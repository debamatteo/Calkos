using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CalkosManager.Domain.Entities
{
    public class ImportSystemP
    {

        /// <summary>
        /// Dati importati dal file Excel SystemP.
        /// Copia immutabile.
        /// Tabella: dbo.ImportSystemP
        /// </summary>

        public int IdImportSystemP { get; set; }
        public int IdFileImportato { get; set; }
        public string? OrdineDDT { get; set; } //1  OrdineDDT / Ordine/DDT
        public string? Cliente { get; set; } //2  Cliente / Cliente
        public decimal? Kg { get; set; } //3  KG / KG
        public string? Materiale { get; set; } //4  Materiale / Materiale
        public decimal? LavorazionePrezzoAcquisto { get; set; } //5  LavorazionePrezzoAcquisto / Lavorazione
        public decimal? BaseAcquisto { get; set; } //6  BaseAcquisto / Base
        public decimal? CIT { get; set; } //7  CIT / CIT
        public decimal? PrezzoAcquisto { get; set; } //8  PrezzoAcquisto / Prezzo
        public DateTime? DataRiferimentoPrezzoAcquisto { get; set; } //9  DataRiferimentoPrezzoAcquisto / Al
        public decimal? CIT_EUR_KG { get; set; } //10 CIT_EUR_KG / CIT €/Kg
        public decimal? CITsuAcquisto { get; set; } //11 CITsuAcquisto / CIT
        public decimal? ProvvigioneUnitaria { get; set; } //12 ProvvigioneUnitaria / Provvigione
        public decimal? ValoreProvvigioneFissa { get; set; } //13 ValoreProvvigioneFissa / Commissioni
        public decimal? LavorazionePrezzoVendita { get; set; } //14 LavorazionePrezzoVendita / Lavorazione
        public decimal? BaseVendita { get; set; } //15 BaseVendita / Base
        public decimal? CITsuVendita { get; set; } //16 CITsuVendita / CIT
        public decimal? PrezzoVendita { get; set; } //17 PrezzoVendita / Prezzo di vendita
        public decimal? Delta { get; set; } //18 Delta / Delta
        public decimal? PercentualeDelta { get; set; } //19 PercentualeDelta / Percentuale delta
        public decimal? TotaleVariabileProvvigione { get; set; } //20 TotaleVariabileProvvigione / Totale provv variabile
        public decimal? TotaleProvvigione { get; set; } //21 TotaleProvvigione / Totalone
        public DateTime? DataConsegnaIpotetica { get; set; } //22 DataConsegnaIpotetica / Data Consegna ipotetica
        public string? Agente { get; set; } //23 Agente / Agente
        public DateTime? Scadenza { get; set; } //24 Scadenza / Scadenza
        public bool? Fatturare { get; set; } //25 Fatturare / Fatturare
        public int? IdMandatario { get; set; }
        public string? UniqueKey { get; set; }
        public bool IsProcessed { get; set; }

        // ---------------------------------------------------------
        // Proprietà tecnica usata solo durante l'importazione.
        // Serve per marcare le righe duplicate e saltarle.
        // NON è mappata sul database.
        // ---------------------------------------------------------
        [NotMapped]
        public bool Skip { get; set; }

        // ---------------------------------------------------------
        // Proprietà lette da Excel e convertite successivamente.
        // Replicate dallo stile Cobral.
        // ---------------------------------------------------------
        public string? FatturareStringa { get; set; }

        // ---------------------------------------------------------
        // Proprietà di navigazione replicate da Cobral.
        // ---------------------------------------------------------
        public Cliente? ClienteEntity { get; set; }
        public Materiale? MaterialeEntity { get; set; }
        public UnitaMisura? UnitaMisuraEntity { get; set; }

        public DateTime DataInserimento { get; set; }
        public DateTime? DataModifica { get; set; }
        public string? Utente { get; set; }

    }
}
