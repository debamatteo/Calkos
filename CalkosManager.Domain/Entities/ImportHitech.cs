using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CalkosManager.Domain.Entities
{
    public class ImportHitech
    {

        /// <summary>
        /// Dati importati dal file Excel Hitech.
        /// Copia immutabile.
        /// Tabella: dbo.ImportHitech
        /// </summary>

        public int IdImportHitech { get; set; }
        public int IdFileImportato { get; set; }
        public string? OrdineDDT { get; set; }              //	1	OrdineDDT	  /	N. Ordine / Fattura
        public DateTime? DataDoc { get; set; }              //  2	DataDoc	  /	Data 20/04/2026
        public string? Cliente { get; set; }                //	3	Cliente	  /	Cliente
        public decimal? Quantita { get; set; }              //  4	Quantita	  /	Nr.Pezzi
        public decimal? PrezzoBase { get; set; }            //	5	PrezzoBase	  /	Prezzo base
        public decimal? PrezzoPraticato { get; set; }       //	6	PrezzoPraticato	  /	Prezzo praticato
        public decimal? ProvvigioneUnitaria { get; set; }   //	7	ProvvigioneUnitaria	  /	Commissione a pezzo
        public decimal? TotaleProvvigione { get; set; }     //	8	TotaleProvvigione	  /	Totale Commissioni Calkos
        public DateTime? DataConsegna { get; set; }         //	9	DataConsegna	  /	Data Consegna
        public string? Agente { get; set; }	                //	10	Agente	  /	Agente
        public bool? Fatturare { get; set; }                //  11	Fatturare	  /	Fatturare
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
