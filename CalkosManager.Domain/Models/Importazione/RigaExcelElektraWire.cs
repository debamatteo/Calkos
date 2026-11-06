namespace CalkosManager.Domain.Models.Importazione
{
    public class RigaExcelElektraWire
    {
        public string? OrdineDDT { get; set; }                     //1 Ns. Conferma / OrdineDDT
        public string? Cliente { get; set; }                       //2 Cliente / Cliente
        public string? Misure { get; set; }                        //3 Misure / Misure

        public decimal? Kg { get; set; }                           //4 KG/ Kg
        public decimal? CostoLavorazione { get; set; }             //5 Lav / CostoLavorazione
        public string? Materiale { get; set; }                     //6 Materiale / Materiale

        public decimal? Prezzo { get; set; }                       //7 Base / Prezzo?? ****
        public decimal? PrezzoConLavorazione { get; set; }         //8 Totale Mat+Lav / PrezzoConLavorazione
        public decimal? PrezzoVendita { get; set; }                //9 Vendita / PrezzoVendita
        public decimal? DeltaMargine { get; set; }                 //10 Delta / DeltaMargine
        public decimal? Perc_Delta_Provvigione { get; set; }       //27/04/2026 Perc_Delta_Provvigione LETTA DALLA FORMULA DI //23 Totale provv variabile / TotaleVariabileProvvigione
        public decimal? ValUnitProvvigioneFissa1_5 { get; set; }   //11 1,50% / ValUnitProvvigioneFissa1_5
        public decimal? Delta1ProvvigioneFissa1_5 { get; set; }    //12 1,50%<Delta / Delta1ProvvigioneFissa1_5
        public decimal? Delta2ProvvigioneFissa1_5 { get; set; }    //13 1,5%>Delta / Delta2ProvvigioneFissa1_5
        public decimal? TotProvvigioneFissa1_5 { get; set; }       //14 1,5% fissa / TotProvvigioneFissa1_5

        public decimal? ValUnitProvvigioneFissa_03 { get; set; }   //15 3% / ValUnitProvvigioneFissa_03
        public decimal? Delta1ProvvigioneFissa_03 { get; set; }    //16 3% < Delta / Delta1ProvvigioneFissa_03
        public decimal? Delta2ProvvigioneFissa_03 { get; set; }    //17 3% > Delta / Delta2ProvvigioneFissa_03
        public decimal? TotProvvigioneFissa_03 { get; set; }       //18 3% fissa / TotProvvigioneFissa_03

        public decimal? ValQuotaFissa_1_5 { get; set; }            //19 Quota fissa 1,5% / ValQuotaFissa_1_5
        public decimal? ValQuotaFissa_03 { get; set; }             //20 Quota fissa 3% / ValQuotaFissa_03
        public decimal? TotaleFissoProvvigione { get; set; }       //21 Totale / TotaleFissoProvvigione

        public decimal? DeltaResiduoVariabile { get; set; }        //22 delta / DeltaResiduoVariabile
        public decimal? TotaleVariabileProvvigione { get; set; }   //23 Totale provv variabile / TotaleVariabileProvvigione
        public decimal? TotaleSommaProvvigioni { get; set; }       //24 Tot. Provv. / TotaleSommaProvvigioni

        public DateTime? DataConsegna { get; set; }                //25 Data Consegna / Data Consegna
        public decimal? TotaleProvvigioneDaFatturare { get; set; } //26 Totale Provvigioni / TotaleProvvigioneDaFatturare
        public string? Fatturare { get; set; }                     //27 Fatturare / Fatturare

    }
}
