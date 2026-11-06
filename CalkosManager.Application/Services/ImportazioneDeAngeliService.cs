using CalkosManager.Application.Services;
using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
/*
 Chiusura Ciclo: L'aggiunta di SetProcessedStatus all'interno del ciclo foreach (ma dopo la Insert nel Prospetto) 
garantisce che IsProcessed diventi 1 solo se il record è effettivamente operativo.

Bonifica Idempotente: Chiamando DeleteByFileImportatoId all'inizio, 
permetti all'utente di correggere eventuali errori di trasformazione semplicemente ricaricando il file, 
poiché le righe che non erano arrivate allo stato IsProcessed = 1 vengono rimosse prima del nuovo tentativo.

Ottimizzazione: Il controllo duplicati rimane fuori dalla transazione per evitare lock pesanti sulla tabella ImportCobral.
 */
namespace CalkosManager.Application.Services
{
    public class ImportazioneDeAngeliService
    {
        private readonly string _connectionString;
        private readonly IFileImportatoRepository _fileRepo;
        private readonly IImportDeAngeliRepository _importDeAngeliRepo;
        private readonly IProspettoDeAngeliRepository _prospettoRepo;
        private readonly IClienteRepository _clienteRepo;
        private readonly IMaterialeRepository _materialeRepo;
        private readonly IUnitaMisuraRepository _unitaRepo;
        private readonly ProspettoDeAngeliTransformationService _trasformazioneService;
        private readonly UniqueKeyGenerator _uniqueKeyGenerator;

        public ImportazioneDeAngeliService(
            string connectionString,
            IFileImportatoRepository fileRepo,
            IImportDeAngeliRepository importDeAngeliRepo,
            IProspettoDeAngeliRepository prospettoRepo,
            IClienteRepository clienteRepo,
            IMaterialeRepository materialeRepo,
            IUnitaMisuraRepository unitaRepo,
            ProspettoDeAngeliTransformationService trasformazioneService,
            UniqueKeyGenerator uniqueKeyGenerator)
        {
            _connectionString = connectionString;
            _fileRepo = fileRepo;
            _importDeAngeliRepo = importDeAngeliRepo;
            _prospettoRepo = prospettoRepo;
            _clienteRepo = clienteRepo;
            _materialeRepo = materialeRepo;
            _unitaRepo = unitaRepo;
            _trasformazioneService = trasformazioneService;
            _uniqueKeyGenerator = uniqueKeyGenerator;
        }

        public int ImportaFile(FileImportato file, List<ImportDeAngeli> righe, string utente, int idTipoPagamento = 0)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // ---------------------------------------------------------
            // 12/04/2026 0) FASE DI BONIFICA (Scenario C)
            // Prima di iniziare, puliamo i record non elaborati (IsProcessed = 0) 
            // legati a questo file, permettendo ri-importazioni pulite.
            // ---------------------------------------------------------
            if (file.IdFileImportato > 0)
            {
                _importDeAngeliRepo.DeleteByFileImportatoId(file.IdFileImportato, conn, null, utente);
            }

            // 1) LOOKUP FUORI TRANSAZIONE
            var cacheClienti = new Dictionary<string, Cliente>(StringComparer.OrdinalIgnoreCase);
            var cacheMateriali = new Dictionary<string, Materiale>(StringComparer.OrdinalIgnoreCase);
            var cacheUnita = new Dictionary<string, UnitaMisura>(StringComparer.OrdinalIgnoreCase);

            foreach (var r in righe)
            {
                var keyCliente = r.Cliente?.Trim() ?? "";
                if (!cacheClienti.TryGetValue(keyCliente, out var cliente))
                {
                    cliente = _clienteRepo.GetByRagioneSociale(keyCliente);
                    if (cliente == null)
                    {
                        cliente = new Cliente
                        {
                            RagioneSociale = keyCliente,
                            Utente = utente,
                            DataInserimento = DateTime.Now,
                            IsDeleted = false
                        };
                    }
                    else if (cliente.IsDeleted)
                    {
                        cliente.IsDeleted = false;
                        cliente.DataModifica = DateTime.Now;
                        cliente.Utente = utente;
                        _clienteRepo.Restore(cliente.IdCliente);
                    }
                    cacheClienti[keyCliente] = cliente;
                }
                r.ClienteEntity = cliente;

                var keyMateriale = r.Materiale?.Trim() ?? "";
                if (!cacheMateriali.TryGetValue(keyMateriale, out var materiale))
                {
                    materiale = _materialeRepo.GetByDescrizione(keyMateriale)
                                ?? new Materiale { DescrizioneMateriale = keyMateriale };
                    cacheMateriali[keyMateriale] = materiale;
                }
                r.MaterialeEntity = materiale;

                const string keyUnita = "KG";
                if (!cacheUnita.TryGetValue(keyUnita, out var unita))
                {
                    unita = _unitaRepo.GetByCodice(keyUnita)
                            ?? new UnitaMisura { CodiceUnitaMisura = keyUnita };
                    cacheUnita[keyUnita] = unita;
                }
                r.UnitaMisuraEntity = unita;
            }

            // --- RECUPERO AGENTI --- 13/05/2026 INIZIO
            // 1. Prendi la mappa agenti-clienti (1 sola query veloce)
            //Dictionary<int, int> usato per immagazzinare tuuti i clienti del mandatrio con i suoi agenti
            Dictionary<int, int> mappaAgenti = _clienteRepo.GetMappaAgentiClienti(file.IdMandatario);
            //  13/05/2026 FINE		

            // 1B) PRE-CHECK DUPLICATI (FUORI TRANSAZIONE)
            foreach (var r in righe)
            {
                r.UniqueKey = _uniqueKeyGenerator.Generate(r, "DeAngeli");
                r.DataInserimento = DateTime.Now;
                r.DataModifica = null;
                r.Utente = utente;

                // 12/04/2026 Il controllo ExistsUniqueKey ora ignora i record con IsProcessed = 0
                // perché vengono gestiti dalla bonifica o dalla sovrascrittura.
                if (_importDeAngeliRepo.ExistsUniqueKey(r.UniqueKey, file.IdMandatario))
                {
                    r.Skip = true;
                }
            }

            // 2) TRANSAZIONE
            using var tran = conn.BeginTransaction();
            try
            {
                // 12/04/2026 Gestione idempotente della testata file
                if (file.IdFileImportato == 0)
                    file.IdFileImportato = _fileRepo.Insert(file, conn, (SqlTransaction)tran);
                else
                    _fileRepo.Update(file, conn, (SqlTransaction)tran);

                foreach (var r in righe.Where(x => !x.Skip))
                {
                    if (r.ClienteEntity.IdCliente == 0)
                        r.ClienteEntity = _clienteRepo.Insert(r.ClienteEntity, conn, (SqlTransaction)tran);

                    if (r.MaterialeEntity.IdMateriale == 0)
                        r.MaterialeEntity = _materialeRepo.Insert(r.MaterialeEntity, conn, (SqlTransaction)tran);

                    if (r.UnitaMisuraEntity.IdUnitaMisura == 0)
                        r.UnitaMisuraEntity = _unitaRepo.Insert(r.UnitaMisuraEntity, conn, (SqlTransaction)tran);

                    // 12/04/2026 Stato iniziale riga grezza
                    r.IsProcessed = false;
                    r.IdFileImportato = file.IdFileImportato;
                    r.IdMandatario = file.IdMandatario;
                    /// Inserisce un record nella tabella di collegamento MandatariClienti (IdMandatario, IdCliente) solo se non esiste già.
                    /// Questo metodo deve usare connessione e transazione esterne, come nell' Insert().
                    _clienteRepo.InsertMandatarioClienteIfNotExists(
                        r.IdMandatario,
                        r.ClienteEntity.IdCliente,
                        conn,
                        (SqlTransaction)tran
                    );

                    //'INSERISCE NELLA TABELLA SQL INSERT INTO ImportDeAngeli '
                    var inserted = _importDeAngeliRepo.Insert(r, conn, (SqlTransaction)tran, utente);
                    r.IdImportDeAngeli = inserted.IdImportDeAngeli;

                    // 3) TRASFORMAZIONE -Esegue la trasformazione di una singola riga di importazione in un oggetto ProspettoDeAngeli.
                    var prospetto = _trasformazioneService.TrasformaSingola(r, out var erroreTrasf);

                    if (prospetto != null)//13/05/2026 INIZIO
                    {
                        // mappaAgenti è il dizionario per trovare l'agente del cliente 'r.ClienteEntity.IdCliente'
                        //Prova a cercare nel dizionario l'ID di questo cliente. Se lo trovi, metti il suo ID Agente dentro la variabile idAgenteTrovato
                        //e poi assegnalo al prospetto. Se non lo trovi, non fare nulla (evitando errori).
                        //*TryGetValue, se il cliente non ha un agente, il programma semplicemente salta il blocco if e continua con la riga successiva
                        if (mappaAgenti.TryGetValue(r.ClienteEntity.IdCliente, out var idAgenteTrovato))
                        {
                            // SALVIAMO L'ID AGENTE NEL PROSPETTO
                            prospetto.IdAgente = idAgenteTrovato;
                        }
                    }//


                    if (prospetto == null)
                    {
                        Console.WriteLine($"[ERRORE TRASFORMAZIONE] Riga IdImportDeAngeli={r.IdImportDeAngeli}: {erroreTrasf}");
                        continue;
                    }

                    prospetto.Anno = file.Anno;
                    prospetto.Mese = file.Mese;
                    prospetto.IdTipoPagamento = idTipoPagamento;

                    // Inserimento record operativo procedure spProspettoDeAngeli_Insert_Tran in tabella [ProspettoDeAngeli]
                    _prospettoRepo.Insert(prospetto, conn, (SqlTransaction)tran, utente);

                    // ---------------------------------------------------------
                    // 12/04/2026
                    // 4) CHIUSURA RIGA in tabella ImportDeAngeli (UPDATE campo IsProcessed)
                    // Fondamentale per marcare il record come "elaborato" ed evitare 
                    // che venga incluso in future bonifiche o importazioni duplicate.
                    // ---------------------------------------------------------
                    _importDeAngeliRepo.SetProcessedStatus(r.IdImportDeAngeli, true, conn, (SqlTransaction)tran, utente);
                }

                tran.Commit();
                return file.IdFileImportato;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }
    }
}