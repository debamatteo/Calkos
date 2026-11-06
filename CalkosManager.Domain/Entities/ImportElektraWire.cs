
// ---------------------------------------------------------
// PROMEMORIA DISCREPANZE RISPETTO AL MODELLO IMPORTCOBRAL
// ---------------------------------------------------------
//
// 1) Fatturare:
//    - In Cobral: FatturareStringa (string) + Fatturare (bool?)
//    - In ElektraWire: Fatturare è una stringa singola
//    → Discrepanza di struttura e di tipo.
//
// 2) Navigation Properties:
//    - In Cobral NON sono [NotMapped]
//    - In ElektraWire TUTTE le navigation sono [NotMapped]
//    → Discrepanza strutturale.
//
// 3) Scadenza:
//    - Presente in Cobral
//    - Assente in ElektraWire
//    → Discrepanza funzionale.
//
// 4) Ordine dei blocchi:
//    - Cobral segue blocchi rigidi (chiavi → anagrafica → quantità → materiali → prezzi → provvigioni → consegna → Excel → tecnici → skip → audit)
//    - ElektraWire ha blocchi provvigionali molto estesi e specifici, non presenti in Cobral
//    → Discrepanza logica inevitabile.
//
// 5) Campi provvigionali:
//    - Cobral usa logiche granulari (AlluminioSpessore, OttoneSpessore, ecc.)
//    - ElektraWire usa logiche aggregate e specifiche (ValUnitProvvigioneFissa1_5, DeltaResiduoVariabile, TotaleSommaProvvigioni, ecc.)
//    → Modello concettualmente diverso.
//
// 6) Tipo di Fatturare:
//    - Cobral: bool?
//    - ElektraWire: string?
//    → Discrepanza di tipo.
//

// ---------------------------------------------------------
// FINE PROMEMORIA
// ---------------------------------------------------------



using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CalkosManager.Domain.Entities
{
    public class ImportElektraWire
    {
        /// <summary>
        /// Dati importati dal file Excel ElektraWire.
        /// Copia immutabile.
        /// Tabella: dbo.ImportElektraWire
        /// </summary>

        // ---------------------------------------------------------
        // CHIAVI
        // ---------------------------------------------------------
        public int IdImportElektraWire { get; set; }
        public int IdFileImportato { get; set; }
        public int IdMandatario { get; set; }

        // ---------------------------------------------------------
        // DATI ANAGRAFICI
        // ---------------------------------------------------------
        public string? OrdineDDT { get; set; } //1 Ns. Conferma / OrdineDDT
        public string? Cliente { get; set; }   //2 Cliente / Cliente
        public string? Misure { get; set; }    //3 Misure / Misure

        // ---------------------------------------------------------
        // QUANTITÀ E MATERIALI
        // ---------------------------------------------------------
        public decimal? Kg { get; set; }                 //4 ="Kg" / Kg
        public decimal? CostoLavorazione { get; set; }   //5 Lav / CostoLavorazione
        public string? Materiale { get; set; }           //6 Materiale / Materiale

        // ---------------------------------------------------------
        // PREZZI E MARGINI
        // ---------------------------------------------------------
        public decimal? Prezzo { get; set; }                 //7 Base / Prezzo
        public decimal? PrezzoConLavorazione { get; set; }   //8 Totale Mat+Lav / PrezzoConLavorazione
        public decimal? PrezzoVendita { get; set; }          //9 Vendita / PrezzoVendita
        public decimal? DeltaMargine { get; set; }           //10 Delta / DeltaMargine
        public decimal? Perc_Delta_Provvigione { get; set; }           //23 la percentuale letta dalla formula //23 Totale provv variabile / TotaleVariabileProvvigione

        // ---------------------------------------------------------
        // BLOCCHI PROVVIGIONALI (SPECIFICI ELEKTRA WIRE)
        // ---------------------------------------------------------
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

        // ---------------------------------------------------------
        // CONSEGNA / FATTURAZIONE
        // ---------------------------------------------------------
        public DateTime? DataConsegna { get; set; }                //25 Data Consegna / Data Consegna
        public decimal? TotaleProvvigioneDaFatturare { get; set; } //26 Totale Provvigioni / TotaleProvvigioneDaFatturare
        public bool? Fatturare { get; set; }                       //27 Fatturare / Fatturare

        //public string? Fatturare { get; set; }//19/04/2026
        public string? FatturareStringa { get; set; } // letta da Excel
        // ---------------------------------------------------------
        // TECNICI (UniqueKey / Processed)
        // ---------------------------------------------------------
        public string? UniqueKey { get; set; }
        public bool IsProcessed { get; set; }

        // ---------------------------------------------------------
        // NAVIGATION PROPERTIES 
        // MODIFICA: spostate qui per rispettare la posizione Cobral
        // ---------------------------------------------------------
        public Cliente? ClienteEntity { get; set; }
        public Materiale? MaterialeEntity { get; set; }
        public UnitaMisura? UnitaMisuraEntity { get; set; }

        // ---------------------------------------------------------
        // SKIP (NOT MAPPED)
        // MODIFICA: spostato sotto le navigation properties come Cobral
        // ---------------------------------------------------------
        [NotMapped]
        public bool Skip { get; set; }

        // ---------------------------------------------------------
        // AUDIT
        // MODIFICA: mantenuto in fondo come Cobral
        // ---------------------------------------------------------
        public DateTime DataInserimento { get; set; }
        public DateTime? DataModifica { get; set; }
        public string? Utente { get; set; }
    }
}




