using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations.Schema;

namespace CalkosManager.Domain.Entities
{
    public class ImportDeAngeli
    {
        /// <summary>
        /// Dati importati dal file Excel De Angeli.
        /// Tabella: dbo.ImportDeAngeli
        /// </summary>

        // ---------------------------------------------------------
        // CHIAVI
        // ---------------------------------------------------------
        public int IdImportDeAngeli { get; set; }
        public int? IdCalcoloBase { get; set; }

        // ---------------------------------------------------------
        // DATI ANAGRAFICI
        // ---------------------------------------------------------
        public string? OrdineDDT { get; set; }                 //2 Fatt. n°/Vs. Orc / OrdineDDT
        public string? Cliente { get; set; }                   //3 Cliente / Cliente

        // ---------------------------------------------------------
        // QUANTITÀ E MATERIALI
        // ---------------------------------------------------------
        public decimal? Kg { get; set; }                       //4 Kg / Kg
        public decimal? QuantitaOrdine { get; set; }           //5 Ordine < 500 (non compilare se in C/L) / QuantitaOrdine
        public decimal? CostoLavorazione { get; set; }         //6 Lav / CostoLavorazione
        public string? Materiale { get; set; }                 //7 Tipologia Materiale / Materiale
        public string? TipologiaMateriale { get; set; }        //8 Tipologia Materiale / Tipologia Materiale

        // ---------------------------------------------------------
        // PREZZI
        // ---------------------------------------------------------
        public decimal? Prezzo { get; set; }                   //9 Materiale Euro di riferimento / Prezzo
        public decimal? PrezzoConLavorazione { get; set; }     //10 Materiale+Lav con commissione / PrezzoConLavorazione
        public decimal? PrezzoVendita { get; set; }            //11 Vendita / PrezzoVendita

        // ---------------------------------------------------------
        // PROVVIGIONI
        // ---------------------------------------------------------
        public decimal? PercentualeProvvigioneVariabile { get; set; }   //12 Aliquota variabile / PercentualeProvvigioneVariabile
        public decimal? ValoreProvvigioneVariabile { get; set; }        //13 Commissione con aliquota Variabile / ValoreProvvigioneVariabile
        public decimal? ValoreProvvigioneFissa1 { get; set; }           //14 1,5% fissa / ValoreProvvigioneFissa1
        public decimal? ValoreProvvigioneFissa2 { get; set; }           //15 3% Fissa / ValoreProvvigioneFissa2
        public decimal? ValoreProvvigioneFissa3 { get; set; }           //16 8% Fissa / ValoreProvvigioneFissa3
        public decimal? TotaleFissoProvvigione { get; set; }            //17 Totale Fisso / TotaleFissoProvvigione
        public bool? FlagIndicatoreMaggiorazione { get; set; }                          //18 FlagIndicatoreMaggiorazione / FlagIndicatoreMaggiorazione
        public decimal? Differenza { get; set; }                        //19 delta / Differenza
        public decimal? TotaleVariabileProvvigione { get; set; }        //20 Totale provv variabile / TotaleVariabileProvvigione
        public decimal? Perc_Delta_Provvigione { get; set; }            //20 letta dalla Formula excel / Perc_Delta_Provvigione //24/04/2026	

        public decimal? TotaleProvvigioneDaFatturare { get; set; }      //21 TOTALE FATTURA / TotaleProvvigioneDaFatturare

        // ---------------------------------------------------------
        // CONSEGNA
        // ---------------------------------------------------------
        public DateTime? DataConsegnaIpotetica { get; set; }            //22 Data Consegna / DataConsegnaIpotetica

        // ---------------------------------------------------------
        // CAMPI EXTRA (non presenti nei 22 commenti ma richiesti dal modello)
        // ---------------------------------------------------------
        public string? FatturareStringa { get; set; }                   // extra 
        public bool? Fatturare { get; set; }                            // extra 
        public DateTime? Scadenza { get; set; }                         // extra 

        // ---------------------------------------------------------
        // COLONNE STANDARD (Allineate a Cobral)
        // ---------------------------------------------------------
        public int IdFileImportato { get; set; }
        public int IdMandatario { get; set; }


        // ---------------------------------------------------------
        // NAVIGATION PROPERTIES
        // ---------------------------------------------------------
        public Cliente? ClienteEntity { get; set; }
        public Materiale? MaterialeEntity { get; set; }
        public UnitaMisura? UnitaMisuraEntity { get; set; }

        // ---------------------------------------------------------
        // TECNICI
        // ---------------------------------------------------------
        public string? UniqueKey { get; set; }
        public bool IsProcessed { get; set; }

        // ---------------------------------------------------------
        // SKIP (NOT MAPPED)
        // ---------------------------------------------------------
        [NotMapped]
        public bool Skip { get; set; }

        // ---------------------------------------------------------
        // AUDIT
        // ---------------------------------------------------------
        public DateTime DataInserimento { get; set; }
        public DateTime? DataModifica { get; set; }
        public string ?Utente { get; set; }
    }
}


// ---------------------------------------------------------
// DISCREPANZE DA CORREGGERE (ImportDeAngeli vs ImportCobral)
// ---------------------------------------------------------
//
// 1) Manca FatturareStringa (Cobral ce l'ha).
//

//
// ---------------------------------------------------------
