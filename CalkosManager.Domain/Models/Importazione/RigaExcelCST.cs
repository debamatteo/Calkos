using CalkosManager.Domain.Entities;
using System.ComponentModel.DataAnnotations.Schema;


namespace CalkosManager.Domain.Models.Importazione
{
    public class RigaExcelCST
    {
        public int IdImportCST { get; set; }
        public int IdFileImportato { get; set; }

        public string? OrdineDDT { get; set; } //1	OrdineDDT
        public string? Cliente { get; set; } //2	Cliente

        public decimal? Kg { get; set; } //3	Kg
        public decimal? CostoLavorazione { get; set; } //4	Lav
        public string? Materiale { get; set; } //5	Tipologia Materiale

        public decimal? PrezzoBase { get; set; } //6	Materiale Euro di riferimento
        public decimal? PrezzoEuroRiferimento { get; set; } //7	Materiale € di vendita
        public decimal? PrezzoBaseConLavorazione { get; set; } //8	Materiale+Lav con commissione
        public decimal? PrezzoVendita { get; set; } //9	Vendita

        public decimal? ProvvigioneFissa01_05 { get; set; } //10	1,5% fissa
        public decimal? ProvvigioneFissa03 { get; set; } //11	3,0% fissa
        public decimal? Perc_Delta_Provvigione { get; set; } //15	 fissa letta dalla fiormula Excel (30/04/206) di //15	Totale provv variabile
        public decimal? TotaleProvvigioneFissa { get; set; } //12	Totale Fisso

        public decimal? CIT { get; set; } //13	Dedotte Spese di trasporto (CIT)
        public decimal? Delta { get; set; } //14	Delta
        public decimal? TotaleProvvigioneVariabile { get; set; } //15	Totale provv variabile
        public decimal? TotaleProvvigioneFattura { get; set; } //16	Totale fattura

        public DateTime? DataConsegna { get; set; } //17	Data consegna
        public string? Agente { get; set; } //18	Agente

        public string? Fatturare { get; set; } //19	FATTURARE


        //public bool? Fatturare { get; set; }
        //public DateTime? Scadenza { get; set; }


    }
}





