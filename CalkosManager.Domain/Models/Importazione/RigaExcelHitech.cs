using CalkosManager.Domain.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CalkosManager.Domain.Models.Importazione
{
    public class RigaExcelHitech
    {
        public int IdImportHitech { get; set; }
        public int IdFileImportato { get; set; }


        
        public string? OrdineDDT { get; set; }//N.Ordine / Fattura
        public string? Cliente { get; set; }//Cliente


        public DateTime? DataDoc { get; set; }//Data 21/04/2026

        public decimal? Quantita { get; set; }//Nr. Pezzi
        public decimal? PrezzoBase { get; set; }//Prezzo base
        public decimal? PrezzoPraticato { get; set; }//Prezzo praticato
        public decimal? ProvvigioneUnitaria { get; set; }//Commissione a pezzo
        public decimal? TotaleProvvigione { get; set; }//Totale Commissioni Calkos

        public DateTime? DataConsegna { get; set; }//Data Consegna
        public string? Agente { get; set; }//Agente

        public string? Fatturare { get; set; }//FATTURARE
        //public bool? Fatturare { get; set; }
        //public DateTime? Scadenza { get; set; }



    }
}













