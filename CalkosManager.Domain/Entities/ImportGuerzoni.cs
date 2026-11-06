using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CalkosManager.Domain.Entities
{
    public class ImportGuerzoni
    {

        /// <summary>
        /// Dati importati dal file Excel Guerzoni.
        /// Copia immutabile.
        /// Tabella: dbo.ImportGuerzoni
        /// </summary>

        public int IdImportGuerzoni { get; set; }
        public int IdFileImportato { get; set; }

        public string? OrdineDDT { get; set; }          //1	OrdineDDT	 / 	N. Ordine / Conferma d'ordine
        public string? Cliente { get; set; }            //2	Cliente	 / 	Cliente
        public decimal? Quantita { get; set; }          //3	Quantita	 / 	Quantità
        public decimal? PrezzoUnitario { get; set; }    //4	Prezzo Unitario	 / 	Prezzo Unitario
        public decimal? Imponibile { get; set; }        //5	Imponibile	 / 	Imponibile
                
        public decimal? Perc_Delta_Provvigione { get; set; }//6	[F* 0,03] Forumal letta da Excel [Colonna F	TotaleProvvigione	F* 0,03 ]//29/04/2026

        public decimal? TotaleProvvigione { get; set; }//6	TotaleProvvigione	 / 	Totale
        public DateTime? DataConsegna { get; set; }     //7	Data consegna	 / 	Data consegna
        public bool? Fatturare { get; set; }            //8	FATTURARE	 / 	FATTURARE

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

        public DateTime DataInserimento { get; set; }
        public DateTime? DataModifica { get; set; }
        public string? Utente { get; set; }

    }
}
