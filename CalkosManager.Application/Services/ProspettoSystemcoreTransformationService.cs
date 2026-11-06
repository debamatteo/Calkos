using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
//Serve per poter usare l’interfaccia IConfiguration.serve proprio per leggere qualsiasi valore dentro appsettings.json.
using Microsoft.Extensions.Configuration;

public class ProspettoSystemCoreTransformationService
{
    private readonly IClienteRepository _clienteRepo;
    private readonly IMaterialeRepository _materialeRepo;
    private readonly IUnitaMisuraRepository _unitaRepo;
    private readonly IConfiguration _config;//14/05/2026
    public ProspettoSystemCoreTransformationService(
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
    public ProspettoSystemCore? TrasformaSingola(ImportSystemCore r, out string? errore)
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

            //// Split Ordine/DDT/ 14/05/2026  Systemcore
            // Esempio:   "Systemcore":  12609	;	NESSUNA DIVISIONE DDT	; Ordine 12609	DDT NON  PREVISTO
            string nomeMandatario = "Systemcore"; //14/05/2026
            var sep = _config[$"SeparatoreOrdineDDT:{nomeMandatario}"] ?? ""; // case-insensitive

            //12/04/2026
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

            // 5) Creazione oggetto ProspettoSystemCore
            var p = new ProspettoSystemCore();

            // Identificativi
            p.IdImportSystemCore = r.IdImportSystemCore;
            p.IdFileImportato = r.IdFileImportato;
            p.IdMandatario = r.IdMandatario ?? 0;

            // Lookup
            p.IdCliente = cliente.IdCliente;
            p.IdAgente = cliente.IdAgente;//25/03/2026 aggiungo agente default preso dalla tabella clienti
            p.IdMateriale = materiale.IdMateriale;
            p.IdUnitaMisura = unita.IdUnitaMisura;

            // Documenti
            p.NumeroOrdine = numeroOrdine;
            p.NumeroDDT = numeroDDT;

            // Quantità e valori
            p.Quantita = r.Kg;
          
            p.PrezzoVendita = r.PrezzoVendita;
            p.CostoLavorazione = r.CostoLavorazione;

            // Provvigioni
            p.PercentualeQuotaFissa = r.PercentualeQuotaFissa;
            p.ValoreQuotaFissa = r.ValoreQuotaFissa;
            p.TotaleProvvigioneVariabile = r.TotaleProvvigioneVariabile;
            p.TotaleProvvigione = r.TotaleProvvigione;
            p.TotaleProvvigioneDaFatturare = r.TotaleProvvigioneDaFatturare;
            //Perc_Delta_Provvigione,
            //QuotaCentesimiPerKg,
            p.Perc_Delta_Provvigione = r.Perc_Delta_Provvigione;//26/04/2026
            p.QuotaCentesimiPerKg = r.QuotaCentesimiPerKg;//26/04/2026

            p.Delta = r.Delta;

            // Date
            p.DataConsegna = r.DataConsegna;
            p.Scadenza = r.Scadenza;


            return p;
        }
        catch (Exception ex)
        {
            errore = $"Errore inatteso: {ex.Message}";
            return null;
        }
    }



}
