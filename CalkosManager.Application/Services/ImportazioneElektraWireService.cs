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

Ottimizzazione: Il controllo duplicati rimane fuori dalla transazione per evitare lock pesanti sulla tabella ImportElektraWire.
 */
public class ImportazioneElektraWireService
{
    private readonly string _connectionString;
    private readonly IFileImportatoRepository _fileRepo;
    private readonly IImportElektraWireRepository _importElektraWireRepo;
    private readonly IProspettoElektraWireRepository _prospettoRepo;
    private readonly IClienteRepository _clienteRepo;
    private readonly IMaterialeRepository _materialeRepo;
    private readonly IUnitaMisuraRepository _unitaRepo;
    private readonly ProspettoElektraWireTransformationService _trasformazioneService;
    private readonly UniqueKeyGenerator _uniqueKeyGenerator;

    public ImportazioneElektraWireService(
        string connectionString,
        IFileImportatoRepository fileRepo,
        IImportElektraWireRepository importElektraWireRepo,
        IProspettoElektraWireRepository prospettoRepo,
        IClienteRepository clienteRepo,
        IMaterialeRepository materialeRepo,
        IUnitaMisuraRepository unitaRepo,
        ProspettoElektraWireTransformationService trasformazioneService,
        UniqueKeyGenerator uniqueKeyGenerator)
    {
        _connectionString = connectionString;
        _fileRepo = fileRepo;
        _importElektraWireRepo = importElektraWireRepo;
        _prospettoRepo = prospettoRepo;
        _clienteRepo = clienteRepo;
        _materialeRepo = materialeRepo;
        _unitaRepo = unitaRepo;
        _trasformazioneService = trasformazioneService;
        _uniqueKeyGenerator = uniqueKeyGenerator;
    }

    public int ImportaFile(FileImportato file, List<ImportElektraWire> righe, string utente, int idTipoPagamento = 0)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        // ---------------------------------------------------------
        // 12/04/2026
        // 0) FASE DI BONIFICA (Scenario C)
        // Se il file è già stato caricato parzialmente in precedenza, 
        // eliminiamo le righe "parcheggiate" (IsProcessed = 0) per evitare sporcizia.
        // ---------------------------------------------------------
        if (file.IdFileImportato > 0)
        {
            // Nota: Se IdFileImportato è 0 significa che è un file totalmente nuovo
            _importElektraWireRepo.DeleteByFileImportatoId(file.IdFileImportato, conn, null, utente);
        }

        // ---------------------------------------------------------
        // 1) LOOKUP FUORI TRANSAZIONE
        //    (Clienti, Materiali, Unità) — nessun lock, nessun rischio
        // ---------------------------------------------------------
        var cacheClienti = new Dictionary<string, Cliente>(StringComparer.OrdinalIgnoreCase);
        var cacheMateriali = new Dictionary<string, Materiale>(StringComparer.OrdinalIgnoreCase);
        var cacheUnita = new Dictionary<string, UnitaMisura>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in righe)
        {
            // CLIENTE 31/03/20126
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

            // MATERIALE
            var keyMateriale = r.Materiale?.Trim() ?? "";
            if (!cacheMateriali.TryGetValue(keyMateriale, out var materiale))
            {
                materiale = _materialeRepo.GetByDescrizione(keyMateriale)
                            ?? new Materiale { DescrizioneMateriale = keyMateriale };
                cacheMateriali[keyMateriale] = materiale;
            }
            r.MaterialeEntity = materiale;

            // UNITÀ (fissa: KG)
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



        // ---------------------------------------------------------
        // 1B) PRE-CHECK DUPLICATI (FUORI TRANSAZIONE)
        // ---------------------------------------------------------
        foreach (var r in righe)
        {
            //12/04/2026 versione senza anno di competenza
            //r.UniqueKey = _uniqueKeyGenerator.Generate(r, "ElektraWire");

            // 12/04/2026 INIEZIONE ANNO DI COMPETENZA
            // ANNO viene fornito (dalla scelta utente nell'interfaccia), lo inseriamo come 
            // primo elemento della chiave. Questo risolve il problema del riciclo dei numeri 
            // d'ordine che avviene tipicamente ad ogni nuovo anno solare.
            r.UniqueKey = _uniqueKeyGenerator.Generate(r, "ElektraWire", file.Anno);
            r.DataModifica = null;
            r.Utente = utente;

            // 12/04/2026 Il metodo ExistsUniqueKey ora controlla solo dove IsProcessed = 1
            if (_importElektraWireRepo.ExistsUniqueKey(r.UniqueKey, file.IdMandatario))
            {
                r.Skip = true;
            }
        }

        // ---------------------------------------------------------
        // 2) TRANSAZIONE SOLO PER INSERT E UPDATE STATO
        // ---------------------------------------------------------
        using var tran = conn.BeginTransaction();

        try
        {
            // Inserimento o aggiornamento testata file
            if (file.IdFileImportato == 0)
                file.IdFileImportato = _fileRepo.Insert(file, conn, tran);
            else
                _fileRepo.Update(file, conn, tran); // Assumendo esista un Update per la testata

            foreach (var r in righe.Where(x => !x.Skip))
            {
                if (r.ClienteEntity.IdCliente == 0)
                    r.ClienteEntity = _clienteRepo.Insert(r.ClienteEntity, conn, tran);

                if (r.MaterialeEntity.IdMateriale == 0)
                    r.MaterialeEntity = _materialeRepo.Insert(r.MaterialeEntity, conn, tran);

                if (r.UnitaMisuraEntity.IdUnitaMisura == 0)
                    r.UnitaMisuraEntity = _unitaRepo.Insert(r.UnitaMisuraEntity, conn, tran);

                // 12/04/2026 Inizializzazione riga: IsProcessed nasce sempre False
                r.IsProcessed = false;
                r.IdFileImportato = file.IdFileImportato;
                r.IdMandatario = file.IdMandatario;

                _clienteRepo.InsertMandatarioClienteIfNotExists(
                    r.IdMandatario,
                    r.ClienteEntity.IdCliente,
                    conn,
                    tran
                );

                // Inserimento ImportElektraWire (Tabella Storica)
                var inserted = _importElektraWireRepo.Insert(r, conn, tran, utente);
                r.IdImportElektraWire = inserted.IdImportElektraWire;

                // ---------------------------------------------------------
                // TRASFORMAZIONE ImportElektraWire → ProspettoElektraWire
                // ---------------------------------------------------------
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
                }//13/05/2026 FINE

                if (prospetto == null)
                {
                    // 12/04/2026 Se fallisce la trasformazione, IsProcessed resta 0.
                    // Verrà pulita alla prossima bonifica o gestita manualmente.
                    Console.WriteLine($"[ERRORE TRASFORMAZIONE] Riga IdImportElektraWire={r.IdImportElektraWire}: {erroreTrasf}");
                    continue;
                }

                prospetto.Anno = file.Anno;
                prospetto.Mese = file.Mese;
                prospetto.IdTipoPagamento = idTipoPagamento;


                // ---------------------------------------------------------
                // INSERIMENTO nella tabella operativa ProspettoElektraWire
                // ---------------------------------------------------------
                _prospettoRepo.Insert(prospetto, conn, tran, utente);



                // ---------------------------------------------------------
                // 12/04/2026 3) CHIUSURA LOGICA
                // Ora che la riga è nel Prospetto, aggiorniamo IsProcessed su ImportElektraWire.
                // Se questa riga fallisce, la transazione farà rollback di tutto (Prospetto incluso).
                // ---------------------------------------------------------
                _importElektraWireRepo.SetProcessedStatus(r.IdImportElektraWire, true, conn, tran, utente);
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

