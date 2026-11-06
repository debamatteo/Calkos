using CalkosManager.Domain.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace CalkosManager.Domain.Models.Importazione
{
    public class RigaExcelGuerzoni

    {
        public int IdImportGuerzoni { get; set; }
        public int IdFileImportato { get; set; }
        public string? OrdineDDT { get; set; }//1	OrdineDDT	 / 	N. Ordine / Conferma d'ordine
        public string? Cliente { get; set; }//2	Cliente	 / 	Cliente
        public decimal? Quantita { get; set; }//3	Quantita	 / 	Quantità
        public decimal? PrezzoUnitario { get; set; }//4	Prezzo Unitario	 / 	Prezzo Unitario
        public decimal? Imponibile { get; set; }//5	Imponibile	 / 	Imponibile
        public decimal? Perc_Delta_Provvigione   { get; set; }//6	Perc_Delta_Provvigione	 //29/04/2026 valore letto da Excel [Colonna F	TotaleProvvigione	F* 0,03 ]
        public decimal? TotaleProvvigione { get; set; }//6	TotaleProvvigione	 / 	Totale
        public DateTime? DataConsegna { get; set; }//7	Data consegna	 / 	Data consegna
        public string? Fatturare { get; set; }//8	Fatturare	 / 	Fatturare

        //public bool? Fatturare { get; set; }
        //public DateTime? Scadenza { get; set; }

    }
}
