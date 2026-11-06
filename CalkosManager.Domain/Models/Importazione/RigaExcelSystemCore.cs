using CalkosManager.Domain.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace CalkosManager.Domain.Models.Importazione
{
    public class RigaExcelSystemCore
    {
        public int IdImportSystemCore { get; set; }
        public int IdFileImportato { get; set; }
        public string? OrdineDDT { get; set; } //1  OrdineDDT / Ns. Conferma
        public string? Cliente { get; set; } //2  Cliente / Cliente
        public string? Misure { get; set; } //3  Misure / Misure
        public decimal? Kg { get; set; } //4  Kg / Kg
        public decimal? CostoLavorazione { get; set; } //5  CostoLavorazione / Lav
        public string? Materiale { get; set; } //6  Materiale / Materiale

        public decimal? PrezzoVendita { get; set; } //7  PrezzoVendita / Vendita
        public decimal? PercentualeQuotaFissa { get; set; } //8  PercentualeQuotaFissa / % Quota fissa
        public decimal? QuotaCentesimiPerKg { get; set; } //8  letta dalla formula Excel Colonna (8) % Quota fissa

        public decimal? ValoreQuotaFissa { get; set; } //9  ValoreQuotaFissa / Quota fissa € 0,10
        public decimal? Delta { get; set; } //10  Delta / delta
        public decimal? TotaleProvvigioneVariabile { get; set; } //11  TotaleProvvigioneVariabile / Totale provv variabile
        public decimal? TotaleProvvigione { get; set; } //12  TotaleProvvigione / Totalone

        public decimal? Perc_Delta_Provvigione { get; set; } //11  letta dalla formula Excel TotaleProvvigioneVariabile / Totale provv variabile


        public DateTime? DataConsegna { get; set; } //13  DataConsegna / Data Consegna
        public decimal? TotaleProvvigioneDaFatturare { get; set; } //14  TotaleProvvigioneDaFatturare / Totale Provvigioni

        public string? Fatturare { get; set; } //15  Fatturare / Fatturare

        //public bool? Fatturare { get; set; }

    }
}
