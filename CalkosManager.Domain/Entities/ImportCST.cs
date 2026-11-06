using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CalkosManager.Domain.Entities
{
    public class ImportCST
    {

        /// <summary>
        /// Dati importati dal file Excel CST.
        /// Copia immutabile.
        /// Tabella: dbo.ImportCST
        /// </summary>

        public int IdImportCST { get; set; }
        public int IdFileImportato { get; set; }

        public string? OrdineDDT { get; set; }
        public string? Cliente { get; set; }

        public decimal? Kg { get; set; }
        public decimal? CostoLavorazione { get; set; }
        public string? Materiale { get; set; }

        public decimal? PrezzoBase { get; set; }

        public decimal? PrezzoEuroRiferimento { get; set; } //7	Materiale € di vendita

        public decimal? PrezzoBaseConLavorazione { get; set; }
        public decimal? PrezzoVendita { get; set; }
        public decimal? ProvvigioneFissa01_05 { get; set; }
        public decimal? ProvvigioneFissa03 { get; set; }
        public decimal? Perc_Delta_Provvigione { get; set; } //30/04/206
        public decimal? TotaleProvvigioneFissa { get; set; }

        public decimal? CIT { get; set; }
        public decimal? Delta { get; set; }
        public decimal? TotaleProvvigioneVariabile { get; set; }
        public decimal? TotaleProvvigioneFattura { get; set; }

        public DateTime? DataConsegna { get; set; }
        public string? Agente { get; set; }

        public bool? Fatturare { get; set; }
        public DateTime? Scadenza { get; set; }

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
        //public Mandatario? MandatarioEntity { get; set; }

        public DateTime DataInserimento { get; set; }
        public DateTime? DataModifica { get; set; }
        public string? Utente { get; set; }

    }
}
