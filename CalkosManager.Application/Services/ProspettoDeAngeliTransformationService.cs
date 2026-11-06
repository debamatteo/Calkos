using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
//Serve per poter usare l’interfaccia IConfiguration.serve proprio per leggere qualsiasi valore dentro appsettings.json.
using Microsoft.Extensions.Configuration;

namespace CalkosManager.Application.Services
{
    /// <summary>
    /// Servizio di dominio responsabile della trasformazione dei dati grezzi provenienti dall'importazione Excel 
    /// (Tabella ImportDeAngeli) nell'entità di business definitiva (ProspettoDeAngeli).
    /// Gestisce la risoluzione delle dipendenze (Lookup), il calcolo dei periodi e il mapping dei dati provvigionali.
    /// </summary>
    public class ProspettoDeAngeliTransformationService
    {
        private readonly IClienteRepository _clienteRepo;
        private readonly IMaterialeRepository _materialeRepo;
        private readonly IUnitaMisuraRepository _unitaRepo;
        private readonly IConfiguration _config;//14/05/2026
        /// <summary>
        /// Inizializza una nuova istanza del servizio con l'iniezione dei repository necessari per il lookup dei dati.
        /// </summary>
        public ProspettoDeAngeliTransformationService(
            IClienteRepository clienteRepo,
            IMaterialeRepository materialeRepo,
            IUnitaMisuraRepository unitaRepo, IConfiguration config)//14/05/2026
        {
            _clienteRepo = clienteRepo;
            _materialeRepo = materialeRepo;
            _unitaRepo = unitaRepo;
            _config = config;//14/05/2026
        }

        /// <summary>
        /// Esegue la trasformazione di una singola riga di importazione in un oggetto ProspettoDeAngeli.
        /// </summary>
        /// <param name="r">L'entità sorgente contenente i dati grezzi dell'importazione.</param>
        /// <param name="errore">Parametro di output valorizzato in caso di fallimento della trasformazione (es. lookup fallito).</param>
        /// <returns>L'istanza di ProspettoDeAngeli popolata o null in caso di errore di validazione.</returns>
        public ProspettoDeAngeli? TrasformaSingola(ImportDeAngeli r, out string? errore)
        {
            errore = null;

            try
            {
                // ------------------------------------------------------------------------------------
                // 1) RISOLUZIONE ENTITÀ CORRELATE (LOOKUP)
                // ------------------------------------------------------------------------------------

                var cliente = r.ClienteEntity ?? (string.IsNullOrEmpty(r.Cliente) ? null : _clienteRepo.GetByRagioneSociale(r.Cliente));
                if (cliente == null)
                {
                    errore = $"Cliente non risolto: '{r.Cliente}'";
                    return null;
                }

                var materiale = r.MaterialeEntity ?? (string.IsNullOrEmpty(r.Materiale) ? null : _materialeRepo.GetByDescrizione(r.Materiale));
                if (materiale == null)
                {
                    errore = $"Materiale non risolto: '{r.Materiale}'";
                    return null;
                }

                var unita = r.UnitaMisuraEntity ?? _unitaRepo.GetByCodice("KG");
                if (unita == null)
                {
                    errore = "Unità di misura 'KG' non trovata a sistema.";
                    return null;
                }


                //// Split Ordine/DDT/ 14/05/2026 DeAngeli
                // Esempio: DeAngeli	2502162/26001943	-     DIVISIONE 	ordine:2502162	ddt: 26001943
                string nomeMandatario = "DeAngeli"; //14/05/2026
                var sep = _config[$"SeparatoreOrdineDDT:{nomeMandatario}"] ?? ""; // case-insensitive


                string? numeroOrdine = null;
                string? numeroDDT = null;


                if (!string.IsNullOrWhiteSpace(r.OrdineDDT))
                {
                    var testo = r.OrdineDDT.Trim();

                    if (!string.IsNullOrEmpty(sep))
                    {
                        int idx = testo.IndexOf(sep);

                        if (idx >= 0)
                        {
                            numeroOrdine = testo.Substring(0, idx);
                            numeroDDT = testo.Substring(idx + sep.Length); // 14/05/2026
                        }
                        else
                        {
                            numeroOrdine = testo;
                        }
                    }
                    else
                    {
                        // Nessuna separazione prevista
                        numeroOrdine = testo;
                    }
                }

                // ------------------------------------------------------------------------------------
                // 3) MAPPING DATI E CHIAVI ESTERNE 
                // ------------------------------------------------------------------------------------
                var p = new ProspettoDeAngeli();

                // Identificativi base
                p.IdImportDeAngeli = r.IdImportDeAngeli;
                p.IdFileImportato = r.IdFileImportato;
                p.IdMandatario = r.IdMandatario;

                // Relazioni entità
                p.IdCliente = cliente.IdCliente;
                p.IdAgente = cliente.IdAgente;
                p.IdMateriale = materiale.IdMateriale;
                p.IdUnitaMisura = unita.IdUnitaMisura;
                p.IdTipoPagamento = null;

                // Documentazione e logistica
                p.NumeroOrdine = numeroOrdine;
                p.NumeroDDT = numeroDDT;
                p.Quantita = r.Kg;
                p.QuantitaOrdine = r.QuantitaOrdine;

                // Dati economici
                p.Prezzo = r.Prezzo;
                p.PrezzoConLavorazione = r.PrezzoConLavorazione;
                p.PrezzoVendita = r.PrezzoVendita;
                p.CostoLavorazione = r.CostoLavorazione;

                // Provvigioni
                p.PercentualeProvvigioneVariabile = r.PercentualeProvvigioneVariabile;
                p.Perc_Delta_Provvigione = r.Perc_Delta_Provvigione;//28/04/2026
                p.ValoreProvvigioneVariabile = r.ValoreProvvigioneVariabile;
                p.ValoreProvvigioneFissa1 = r.ValoreProvvigioneFissa1;
                p.ValoreProvvigioneFissa2 = r.ValoreProvvigioneFissa2;
                p.ValoreProvvigioneFissa3 = r.ValoreProvvigioneFissa3;

                // Totali provvigionali
                p.TotaleFissoProvvigione = r.TotaleFissoProvvigione;
                p.Differenza = r.Differenza;
                p.TotaleVariabileProvvigione = r.TotaleVariabileProvvigione;
                p.TotaleProvvigioneDaFatturare = r.TotaleProvvigioneDaFatturare;

                // Gestione temporale
                p.DataConsegnaIpotetica = r.DataConsegnaIpotetica;
                p.Scadenza = r.Scadenza;

                p.FlagIndicatoreMaggiorazione = r.FlagIndicatoreMaggiorazione; // //28/04/2026



                //// Periodo di competenza basato sulla scadenza
                //if (r.Scadenza.HasValue)
                //{
                //    p.Anno = r.Scadenza.Value.Year;
                //    p.Mese = r.Scadenza.Value.Month;
                //}

                //// Metadati
                //p.DataInserimento = DateTime.Now;
                //p.Utente = r.Utente;
                //p.Fatturata = 0;
                //p.IsDeleted = false;

                return p;
            }
            catch (Exception ex)
            {
                errore = $"Eccezione durante la trasformazione logica: {ex.Message}";
                return null;
            }
        }

    }
}