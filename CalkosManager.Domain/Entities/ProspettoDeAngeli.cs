using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Mvc;
// ==== MODIFICA 28/09/2026 - INIZIO ==== Gestione Ordini in Ritardo
using CalkosManager.Domain.Interfaces;
// ==== MODIFICA 28/09/2026 - FINE ====

namespace CalkosManager.Domain.Entities
{
    public class ProspettoDeAngeli : IRigaConDate // <-- 28/09/2026 - Implementazione interfaccia per gestione ordini in ritardo
    {
        [Key]
        public int IdProspettoDeAngeli { get; set; }

        [Required]
        public int IdMandatario { get; set; }

        [Required]
        public int IdFileImportato { get; set; }

        [Required]
        public int IdImportDeAngeli { get; set; }

        [Required]
        public int IdCliente { get; set; }

        [Required]
        public int IdMateriale { get; set; }

        [Required]
        public int IdUnitaMisura { get; set; }

        public int? IdAgente { get; set; }
        public int? IdTipoPagamento { get; set; }
        // NUOVO 28/08/2026: riferimento al CalcoloBaseMateriale usato per congelare
        // il prezzo materiale sulla riga d'ordine (storicizzazione — vedi doc DeAngeli_CalcoloBase).
        public int? IdCalcoloBase { get; set; }

        //08/10/2026 numeroordine no è più obbligatorio, viene assegnto numero temporaneo se manca, quindi rimosso Required
        [StringLength(100)]
        public string? NumeroOrdine { get; set; }
        
        [StringLength(100)]
        public string? NumeroDDT { get; set; }


        [StringLength(100)]
        public string? NumeroFattura { get; set; }

        // --- QUANTITÀ E PREZZI (decimal 18, 3) ---
        [Column(TypeName = "decimal(18, 6)")] public decimal? Quantita { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? QuantitaOrdine { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? CostoLavorazione { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? Prezzo { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? PrezzoConLavorazione { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? PrezzoVendita { get; set; }

        // --- PROVVIGIONI ---
        [Column(TypeName = "decimal(18, 6)")] public decimal? PercentualeProvvigioneVariabile { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? ValoreProvvigioneVariabile { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? ValoreProvvigioneFissa1 { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? ValoreProvvigioneFissa2 { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? ValoreProvvigioneFissa3 { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? TotaleFissoProvvigione { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? Differenza { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? TotaleVariabileProvvigione { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? TotaleProvvigioneDaFatturare { get; set; }


        public bool? FlagIndicatoreMaggiorazione { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? Perc_Delta_Provvigione { get; set; }//24/04/2026



        // --- DATE E PERIODI ---
        public DateTime? DataConsegnaIpotetica { get; set; }
        // ==== MODIFICA 28/09/2026 - INIZIO ==== Gestione Ordini in Ritardo
        public DateTime? DataConsegna { get; set; }
        public bool EscludiControlloRitardo { get; set; }
        // ==== MODIFICA 28/09/2026 - FINE ====

        // ==== 05/10/2026 MISURE STATISTICHE  - INIZIO ====
        [Column(TypeName = "decimal(18,3)")]
        public decimal? SpessoreNum { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal? LarghezzaNum { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal? DiametroNum { get; set; }
        // ==== 05/10/2026 MISURE STATISTICHE  - FINE ====
        public bool DaFatturare { get; set; }//30/09/2026 
        public DateTime? Scadenza { get; set; }
        public int? Anno { get; set; }
        public int? Mese { get; set; }

        [Column(TypeName = "decimal(18, 6)")] public decimal? ProvvigioneAgente { get; set; }

        // --- METADATI ---
        [Required]
        public DateTime DataInserimento { get; set; }

        public DateTime? DataModifica { get; set; }

        [StringLength(100)]
        public string? Utente { get; set; }

        [Required]
        public int Fatturata { get; set; }

        public DateTime? DataFatturazione { get; set; }

        [Required]
        public bool IsDeleted { get; set; }

        // --- CAMPI DI SOLA LETTURA (NotMapped) ---
        [NotMapped] public string? RagioneSociale { get; set; }
        [NotMapped] public string? DescrizioneMateriale { get; set; }
        [NotMapped] public string? UnitaMisura { get; set; }
        [NotMapped] public string? Agente { get; set; }
        [NotMapped] public string? TipoPagamento { get; set; }
        // ==== MODIFICA 28/09/2026 - INIZIO ==== Campi calcolati ritardo
        [NotMapped] public bool InRitardo { get; set; }
        [NotMapped] public int? GiorniRitardo { get; set; }
        // ==== MODIFICA 28/09/2026 - FINE ====


        /// <summary>
        /// Crea una copia pulita per l'inserimento manuale (DDT/Ordine extra)
        /// </summary>
        public ProspettoDeAngeli CloneForInsert(string utente)
        {
            return new ProspettoDeAngeli
            {
                IdMandatario = this.IdMandatario,
                IdFileImportato = 0,
                IdImportDeAngeli = 0,
                IdCliente = this.IdCliente,
                IdMateriale = this.IdMateriale,
                IdUnitaMisura = this.IdUnitaMisura,
                IdAgente = this.IdAgente,
                IdTipoPagamento = this.IdTipoPagamento,
                IdCalcoloBase = this.IdCalcoloBase,   // NUOVO 28/08/2026
                NumeroOrdine = this.NumeroOrdine,
                Quantita = this.Quantita,
                QuantitaOrdine = this.QuantitaOrdine,
                FlagIndicatoreMaggiorazione = this.FlagIndicatoreMaggiorazione,
                Perc_Delta_Provvigione = this.Perc_Delta_Provvigione,//24/04/2026
                CostoLavorazione = this.CostoLavorazione,
                Prezzo = this.Prezzo,
                PrezzoConLavorazione = this.PrezzoConLavorazione,
                PrezzoVendita = this.PrezzoVendita,
                PercentualeProvvigioneVariabile = this.PercentualeProvvigioneVariabile,
                ValoreProvvigioneVariabile = this.ValoreProvvigioneVariabile,
                ValoreProvvigioneFissa1 = this.ValoreProvvigioneFissa1,
                ValoreProvvigioneFissa2 = this.ValoreProvvigioneFissa2,
                ValoreProvvigioneFissa3 = this.ValoreProvvigioneFissa3,
                TotaleFissoProvvigione = this.TotaleFissoProvvigione,
                Differenza = this.Differenza,
                TotaleVariabileProvvigione = this.TotaleVariabileProvvigione,
                TotaleProvvigioneDaFatturare = this.TotaleProvvigioneDaFatturare,
                DataConsegnaIpotetica = this.DataConsegnaIpotetica,
                // ==== MODIFICA 28/09/2026 - INIZIO ====
                DataConsegna = this.DataConsegna,
                EscludiControlloRitardo = false, // L'ordine duplicato parte senza eccezione
                DaFatturare = false, //30/09/2026 NON Mantieni lo stato di fatturazione

                // ==== MODIFICA 28/09/2026 - FINE ====

                // ==== 05/10/2026 MISURE STATISTICHE  - INIZIO ====
                SpessoreNum = this.SpessoreNum,
                LarghezzaNum = this.LarghezzaNum,
                DiametroNum = this.DiametroNum,
                // ==== 05/10/2026 MISURE STATISTICHE  - FINE ====


                Scadenza = this.Scadenza,
                Anno = this.Anno,
                Mese = this.Mese,
                Utente = utente,
                DataInserimento = DateTime.Now,
                DataModifica = DateTime.Now,
                Fatturata = 0,
                DataFatturazione = null,
                IsDeleted = false
            };
        }
    }
}