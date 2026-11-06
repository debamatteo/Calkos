using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CalkosManager.Domain.Entities
{
    public class ImportSystemCore
    {

        /// <summary>
        /// Dati importati dal file Excel SystemCore.
        /// Copia immutabile.
        /// Tabella: dbo.ImportSystemCore
        /// </summary>

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
        public decimal? ValoreQuotaFissa { get; set; } //9  ValoreQuotaFissa / Quota fissa € 0,10
        public decimal? QuotaCentesimiPerKg { get; set; } //26/04/2026  letta dalla formula Excel Colonna (9) Quota fissa € 0,10

        public decimal? Delta { get; set; } //10  Delta / delta

        public decimal? TotaleProvvigioneVariabile { get; set; } //11  TotaleProvvigioneVariabile / Totale provv variabile
        public decimal? Perc_Delta_Provvigione { get; set; } //26/04/2026 letta dalla formula Excel Colonna (11) Totale provv variabile	
        public decimal? TotaleProvvigione { get; set; } //12  TotaleProvvigione / Totalone

        public DateTime? DataConsegna { get; set; } //13  DataConsegna / Data Consegna
        public decimal? TotaleProvvigioneDaFatturare { get; set; } //14  TotaleProvvigioneDaFatturare / Totale Provvigioni

        public bool? Fatturare { get; set; } //15  Fatturare / Fatturare



        public DateTime? Scadenza { get; set; } //(compatibilità altre classi)


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
