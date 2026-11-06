namespace CalkosManager.Domain.Models.Importazione
{
    public class RigaExcelDeAngeli
    {
        // ---------------------------------------------------------
        // DATI BASE
        // ---------------------------------------------------------
        public int? IdCalcoloBase { get; set; }                   //1 ID_BASE / IdCalcoloBase
        public string? OrdineDDT { get; set; }                    //2 Fatt. n°/Vs. Orc / OrdineDDT
        public string? Cliente { get; set; }                      //3 Cliente / Cliente
        public decimal? Kg { get; set; }                          //4 Kg / Kg
        public decimal? QuantitaOrdine { get; set; }              //5 Ordine < 500 (non compilare se in C/L) / QuantitaOrdine
        public string? Materiale { get; set; }                    //7 Tipologia Materiale / Materiale
        public string? TipologiaMateriale { get; set; }           //8 Tipologia Materiale / Tipologia Materiale

        // ---------------------------------------------------------
        // PREZZI E LAVORAZIONI
        // ---------------------------------------------------------
        public decimal? Prezzo { get; set; }                      //9 Materiale Euro di riferimento / Prezzo
        public decimal? PrezzoConLavorazione { get; set; }        //10 Materiale+Lav con commissione / PrezzoConLavorazione
        public decimal? CostoLavorazione { get; set; }            //6 Lav / CostoLavorazione
        public decimal? PrezzoVendita { get; set; }               //11 Vendita / PrezzoVendita

        // ---------------------------------------------------------
        // PROVVIGIONI (Calcolate o da Excel)
        // ---------------------------------------------------------
        public decimal? PercentualeProvvigioneVariabile { get; set; }   //12 Aliquota variabile / PercentualeProvvigioneVariabile
        public decimal? ValoreProvvigioneVariabile { get; set; }        //13 Commissione con aliquota Variabile / ValoreProvvigioneVariabile
        public decimal? ValoreProvvigioneFissa1 { get; set; }           //14 1,5% fissa / ValoreProvvigioneFissa1
        public decimal? ValoreProvvigioneFissa2 { get; set; }           //15 3% Fissa / ValoreProvvigioneFissa2
        public decimal? ValoreProvvigioneFissa3 { get; set; }           //16 8% Fissa / ValoreProvvigioneFissa3
        public decimal? TotaleFissoProvvigione { get; set; }            //17 Totale Fisso / TotaleFissoProvvigione
        public decimal? TotaleVariabileProvvigione { get; set; }        //20 Totale provv variabile / TotaleVariabileProvvigione
        public decimal? Perc_Delta_Provvigione { get; set; }            //20 letta dalla Formula excel / Perc_Delta_Provvigione
        public decimal? TotaleProvvigioneDaFatturare { get; set; }      //21 TOTALE FATTURA / TotaleProvvigioneDaFatturare

        // ---------------------------------------------------------
        // CONFRONTI E FLAG
        // ---------------------------------------------------------
        public decimal? Differenza { get; set; }                        //19 delta / Differenza
        public bool? FlagIndicatoreMaggiorazione { get; set; }                        //18 FlagIndicatoreMaggiorazione / FlagIndicatoreMaggiorazione
        public string? Fatturare { get; set; }                          // (extra) stringa SI/NO 

        // ---------------------------------------------------------
        // DATE E ALTRO
        // ---------------------------------------------------------
        public DateTime? DataConsegnaIpotetica { get; set; }            //22 Data Consegna / DataConsegnaIpotetica
        public DateTime? Scadenza { get; set; }                         // extra
        public string? Agente { get; set; }                             // extra


    }
}