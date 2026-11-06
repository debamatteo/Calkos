using CalkosManager.Domain.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace CalkosManager.Domain.Models.Importazione
{
    public class RigaExcelSystemP
    {
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

        public string? Fatturare { get; set; } //25 Fatturare / Fatturare

        //public bool? Fatturare { get; set; }



    }
}
