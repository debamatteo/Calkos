using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
//Serve per poter usare l’interfaccia IConfiguration.serve proprio per leggere qualsiasi valore dentro appsettings.json.
using Microsoft.Extensions.Configuration;

public class ProspettoCobralTransformationService
{
    private readonly IClienteRepository _clienteRepo;
    private readonly IMaterialeRepository _materialeRepo;
    private readonly IUnitaMisuraRepository _unitaRepo;
    
    private readonly IConfiguration _config;//14/05/2026

    public ProspettoCobralTransformationService(
        IClienteRepository clienteRepo,
        IMaterialeRepository materialeRepo,
        IUnitaMisuraRepository unitaRepo,
        IConfiguration config)//14/05/2026
    {
        _clienteRepo = clienteRepo;
        _materialeRepo = materialeRepo;
        _unitaRepo = unitaRepo;       
        _config = config;//14/05/2026

    }

    /// <summary>
    /// Trasforma UNA riga. Se fallisce → ritorna null.
    /// </summary>
    public ProspettoCobral? TrasformaSingola(ImportCobral r, out string? errore)
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

            //// 4) Split Ordine/DDT/ Cobral
            // Esempio:  // "1505/26OC-1763"	Carattere "-"	 Ordine= 	1505/26OC	DDT=1763
            string nomeMandatario = "Cobral"; //14/05/2026
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
                        numeroDDT = testo.Substring(idx + sep.Length); // 
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


            // 5) Creazione oggetto ProspettoCobral
            var p = new ProspettoCobral();

            // Identificativi
            p.IdImportCobral = r.IdImportCobral;
            p.IdFileImportato = r.IdFileImportato;
            p.IdMandatario = r.IdMandatario;

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
            p.Prezzo = r.Prezzo;

            // Tecnici
            p.DataRiferimentoPrezzo = r.Al;                     // CAMPO AGGIUNTO (Al → DataRiferimentoPrezzo)
            p.Spessore = r.Spessore;
            p.Larghezza = r.Larghezza;
            p.Provvigione = r.Provvigione;

            p.AlluminioSpessore = r.AlluminioSpessore;
            p.OttoneSpessore = r.OttoneSpessore;
            p.RameSpessore = r.RameSpessore;
            p.AltrePercentuali = r.AltrePercentuali;
            p.PrLavSpess = r.PrLavSpess;

            p.AlluminioLarghezza = r.AlluminioLarghezza;
            p.OttoneLarghezza = r.OttoneLarghezza;
            p.RameLarghezza = r.RameLarghezza;
            p.BronzoLarghezza = r.BronzoLarghezza;

            p.PrLavLarg = r.PrLavLarg;
            p.ExtraPrezzoKg = r.ExtraPrezzoKg;
            p.ExtraPrezzoStagnato = r.ExtraPrezzoStagnato;

            p.PrLavTotale = r.PrLavTotale;                     // CAMPO AGGIUNTO
            p.ValoreCommissioni = r.Commissioni;               // CAMPO AGGIUNTO (Commissioni → ValoreCommissioni)
            p.PrezzoVendita = r.PrezzoVendita;                 // CAMPO AGGIUNTO
            p.Differenza = r.Differenza;                       // CAMPO AGGIUNTO

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
