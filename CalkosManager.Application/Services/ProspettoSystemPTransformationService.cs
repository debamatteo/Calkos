using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
//Serve per poter usare l’interfaccia IConfiguration.serve proprio per leggere qualsiasi valore dentro appsettings.json.
using Microsoft.Extensions.Configuration;

public class ProspettoSystemPTransformationService
{
    private readonly IClienteRepository _clienteRepo;
    private readonly IMaterialeRepository _materialeRepo;
    private readonly IUnitaMisuraRepository _unitaRepo;
    private readonly IConfiguration _config;//14/05/2026
    public ProspettoSystemPTransformationService(
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
    public ProspettoSystemP? TrasformaSingola(ImportSystemP r, out string? errore)
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

            //// Split Ordine/DDT/ 14/05/2026  SystemP
            // Esempio:   "SystemP":1/38878-99/822	;     DIVISIONE 	ORDINE 1/38878	  DDT 99/822
            string nomeMandatario = "SystemP"; //14/05/2026
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

            // 5) Creazione oggetto ProspettoSystemP
            var p = new ProspettoSystemP();

            // Identificativi
            p.IdImportSystemP = r.IdImportSystemP;
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

            // Quantità e prezzi
            p.Quantita = r.Kg;
            p.LavorazionePrezzoAcquisto = r.LavorazionePrezzoAcquisto;
            p.BaseAcquisto = r.BaseAcquisto;
            p.CIT = r.CIT;
            p.PrezzoAcquisto = r.PrezzoAcquisto;
            p.DataRiferimentoPrezzoAcquisto = r.DataRiferimentoPrezzoAcquisto;
            p.CIT_EUR_KG = r.CIT_EUR_KG;
            p.CITsuAcquisto = r.CITsuAcquisto;

            p.ProvvigioneUnitaria = r.ProvvigioneUnitaria;
            p.ValoreProvvigioneFissa = r.ValoreProvvigioneFissa;

            p.LavorazionePrezzoVendita = r.LavorazionePrezzoVendita;
            p.BaseVendita = r.BaseVendita;
            p.CITsuVendita = r.CITsuVendita;
            p.PrezzoVendita = r.PrezzoVendita;

            p.Delta = r.Delta;
            p.PercentualeDelta = r.PercentualeDelta;

            p.TotaleVariabileProvvigione = r.TotaleVariabileProvvigione;
            p.TotaleProvvigione = r.TotaleProvvigione;

            // Date
            p.DataConsegnaIpotetica = r.DataConsegnaIpotetica;
            p.Scadenza = r.Scadenza;

            // Quantità e prezzi
            p.Quantita = r.Kg;


            // Date
            p.DataConsegnaIpotetica = r.DataConsegnaIpotetica;
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
