using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CalkosManager.Domain.Entities
{
    public class ImportCobral
    {

        /// <summary>
        /// Dati importati dal file Excel Cobral.
        /// Copia immutabile.
        /// Tabella: dbo.ImportCobral
        /// </summary>

        public int IdImportCobral { get; set; }
        public int IdFileImportato { get; set; }
        public int IdMandatario { get; set; }

        public string? OrdineDDT { get; set; }
        public string? Cliente { get; set; }

        public decimal? Kg { get; set; }
        public string? Materiale { get; set; }
        public decimal? Prezzo { get; set; }
        public DateTime? Al { get; set; }

        public string? Spessore { get; set; }
        public string? Larghezza { get; set; }

        public decimal? Provvigione { get; set; }
        public decimal? AlluminioSpessore { get; set; }
        public decimal? OttoneSpessore { get; set; }
        public decimal? RameSpessore { get; set; }
        public decimal? AltrePercentuali { get; set; }
        public decimal? PrLavSpess { get; set; }
        public decimal? AlluminioLarghezza { get; set; }
        public decimal? OttoneLarghezza { get; set; }
        public decimal? RameLarghezza { get; set; }
        public decimal? BronzoLarghezza { get; set; }
        public decimal? PrLavLarg { get; set; }
        public decimal? ExtraPrezzoKg { get; set; }
        public decimal? ExtraPrezzoStagnato { get; set; }
        public decimal? PrLavTotale { get; set; }
        public decimal? Commissioni { get; set; }
        public decimal? PrezzoVendita { get; set; }
        public decimal? Differenza { get; set; }

        public DateTime? DataConsegnaIpotetica { get; set; }

        public string? Agente { get; set; }
        public DateTime? Scadenza { get; set; }

        public Cliente? ClienteEntity { get; set; }
        public Materiale? MaterialeEntity { get; set; }
        public UnitaMisura? UnitaMisuraEntity { get; set; }
        public bool? Fatturare { get; set; } // convertita da ImportazioneHelper


        public string? FatturareStringa { get; set; } // letta da Excel
        //filtro antiduplicazione: se UniqueKey è già presente, non importare la riga
        public string? UniqueKey { get; set; }
        public bool IsProcessed { get; set; }

        // ---------------------------------------------------------
        // Proprietà tecnica usata solo durante l'importazione.
        // Serve per marcare le righe duplicate e saltarle.
        // NON è mappata sul database.
        // ---------------------------------------------------------
        [NotMapped]
        public bool Skip { get; set; }

        public DateTime DataInserimento { get; set; }
        public DateTime? DataModifica { get; set; }
        public string Utente { get; set; }

    }
}
