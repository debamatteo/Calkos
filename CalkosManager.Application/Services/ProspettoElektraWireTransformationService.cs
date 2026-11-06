using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
//Serve per poter usare l’interfaccia IConfiguration.serve proprio per leggere qualsiasi valore dentro appsettings.json.
using Microsoft.Extensions.Configuration;

namespace CalkosManager.Application.Services
{
    public class ProspettoElektraWireTransformationService
    {
        private readonly IClienteRepository _clienteRepo;
        private readonly IMaterialeRepository _materialeRepo;
        private readonly IUnitaMisuraRepository _unitaRepo;
        private readonly IConfiguration _config;//14/05/2026
        public ProspettoElektraWireTransformationService(
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
        /// Trasforma UNA riga. Se fallisce → ritorna null.
        /// </summary>
        public ProspettoElektraWire? TrasformaSingola(ImportElektraWire r, out string? errore)
        {
            errore = null;

            try
            {
                // 1) Cliente
                var cliente = r.ClienteEntity;
                if (cliente == null)
                {
                    errore = $"Cliente non risolto: '{r.Cliente}'";
                    return null;
                }

                // 2) Materiale
                var materiale = r.MaterialeEntity;
                if (materiale == null)
                {
                    errore = $"Materiale non risolto: '{r.Materiale}'";
                    return null;
                }

                // 3) Unità di misura
                var unita = r.UnitaMisuraEntity;
                if (unita == null)
                {
                    errore = "Unità di misura 'KG' non risolta.";
                    return null;
                }


                //// Split Ordine/DDT/ 14/05/2026 ProspettoDeAngeliTransformationService
                // Esempio:   "ElektraWire": "-"  25/01229-8956	 	Ordine 25-01229	DDT 8956
                string nomeMandatario = "ElektraWire"; //14/05/2026
                var sep = _config[$"SeparatoreOrdineDDT:{nomeMandatario}"] ?? ""; // case-insensitive



                // 4) Split Ordine/DDT (solo sul PRIMO slash)
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

                // 5) Creazione oggetto ProspettoElektraWire
                var p = new ProspettoElektraWire();

                // Identificativi
                p.IdImportElektraWire = r.IdImportElektraWire;
                p.IdFileImportato = r.IdFileImportato;
                p.IdMandatario = r.IdMandatario ;

                // Lookup
                p.IdCliente = cliente.IdCliente;
                p.IdAgente = cliente.IdAgente;
                p.IdMateriale = materiale.IdMateriale;
                p.IdUnitaMisura = unita.IdUnitaMisura;

                // Documenti
                p.NumeroOrdine = numeroOrdine;
                p.NumeroDDT = numeroDDT;

                // Quantità e prezzi
                p.Quantita = r.Kg;
                p.PrezzoBase = r.Prezzo;

                // Tecnici ElektraWire
                p.Misure = r.Misure;
                p.CostoLavorazione = r.CostoLavorazione;
                p.PrezzoConLavorazione = r.PrezzoConLavorazione;
                p.PrezzoVendita = r.PrezzoVendita;
                p.DeltaMargine = r.DeltaMargine;
                p.Perc_Delta_Provvigione = r.Perc_Delta_Provvigione;//27/09/2024 - Aggiunto campo % delta provvigione

                // Blocchi Provvigionali
                p.ValUnitProvvigioneFissa1_5 = r.ValUnitProvvigioneFissa1_5;
                p.Delta1ProvvigioneFissa1_5 = r.Delta1ProvvigioneFissa1_5;
                p.Delta2ProvvigioneFissa1_5 = r.Delta2ProvvigioneFissa1_5;
                p.TotProvvigioneFissa1_5 = r.TotProvvigioneFissa1_5;
                p.ValQuotaFissa_1_5 = r.ValQuotaFissa_1_5;

                p.ValUnitProvvigioneFissa_03 = r.ValUnitProvvigioneFissa_03;
                p.Delta1ProvvigioneFissa_03 = r.Delta1ProvvigioneFissa_03;
                p.Delta2ProvvigioneFissa_03 = r.Delta2ProvvigioneFissa_03;
                p.TotProvvigioneFissa_03 = r.TotProvvigioneFissa_03;
                p.ValQuotaFissa_03 = r.ValQuotaFissa_03;

                p.TotaleFissoProvvigione = r.TotaleFissoProvvigione;
                p.DeltaResiduoVariabile = r.DeltaResiduoVariabile;
                p.TotaleVariabileProvvigione = r.TotaleVariabileProvvigione;
                p.TotaleSommaProvvigioni = r.TotaleSommaProvvigioni;
                p.TotaleProvvigioneDaFatturare = r.TotaleProvvigioneDaFatturare;

                // Date
                p.DataConsegna = r.DataConsegna;
                //p.Fatturare = r.Fatturare;
                //p.Anno = r.DataConsegna?.Year;
                //p.Mese = r.DataConsegna?.Month;

                // Metadati
                //p.DataInserimento = DateTime.Now;
                //p.Utente = r.Utente ?? "System";
                //p.IsDeleted = false;
                //p.Fatturata = 0;

                return p;
            }
            catch (Exception ex)
            {
                errore = $"Errore inatteso: {ex.Message}";
                return null;
            }
        }
    }
}