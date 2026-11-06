
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// ==== MODIFICA 28/09/2026 - INIZIO ==== Gestione Ordini in Ritardo
using CalkosManager.Domain.Interfaces;
// ==== MODIFICA 28/09/2026 - FINE ====
namespace CalkosManager.Domain.Entities
{
    public class ProspettoTradingAndConsulting : IRigaConDate // <-- 28/09/2026 - Implementazione interfaccia per gestione ordini in ritardo
    {
        [Key]
        public int IdProspettoTradingAndConsulting { get; set; }

        [Required]
        public int IdMandatario { get; set; }

        [Required]
        public int IdFileImportato { get; set; }

        [Required]
        public int IdImportTradingAndConsulting { get; set; }

        [Required]
        public int IdCliente { get; set; }

        [Required]
        public int IdMateriale { get; set; }

        [Required]
        public int IdUnitaMisura { get; set; }

        // --- DOCUMENTO ---
        //[Required]
        //08/10/2026 numeroordine no è più obbligatorio, viene assegnto numero temporaneo se manca, quindi rimosso Required
        [StringLength(100)]
        public string NumeroOrdine { get; set; }

        [StringLength(100)]
        public string? NumeroDDT { get; set; }

        [StringLength(100)]
        public string? NumeroFattura { get; set; }

        // --- QUANTITÀ E PREZZI ---
        [Column(TypeName = "decimal(18,6)")]
        public decimal? Quantita { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? CostoLavorazione { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? PrezzoBase { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? PrezzoVendita { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? PrezzoBaseConLavorazione { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? PrezzoEuroRiferimento { get; set; }//20/04/2026

        // --- PROVVIGIONI ---
        [Column(TypeName = "decimal(18,6)")]
        public decimal? ProvvigioneFissa01_05 { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? ProvvigioneFissa03 { get; set; }


        [Column(TypeName = "decimal(18,6)")]
        public decimal? Perc_Delta_Provvigione { get; set; }//30/04/2026

        [Column(TypeName = "decimal(18,6)")]
        public decimal? TotaleProvvigioneFissa { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? CIT { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? Delta { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? TotaleProvvigioneVariabile { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? TotaleProvvigioneFattura { get; set; }


        public bool? Fatturare { get; set; }

        public int? IdAgente { get; set; }

        public int? IdTipoPagamento { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? ProvvigioneAgente { get; set; }
        // --- DATE E LOOKUP ---
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

        // --- AUDIT ---
        [Required]
        public DateTime DataInserimento { get; set; }

        public DateTime? DataModifica { get; set; }

        [StringLength(100)]
        public string? Utente { get; set; }

        public int? Anno { get; set; }

        public int? Mese { get; set; }

        [Required]
        public int Fatturata { get; set; }

        public DateTime? DataFatturazione { get; set; }

        [Required]
        public bool IsDeleted { get; set; }

        // --- CAMPI DI SOLA LETTURA (VIEW) ---
        [NotMapped] public string? RagioneSociale { get; set; }
        [NotMapped] public string? DescrizioneMateriale { get; set; }
        [NotMapped] public string? UnitaMisura { get; set; }
        [NotMapped] public string? AgenteDescrizione { get; set; }
        [NotMapped] public decimal? PercentualeDefaultAgente { get; set; }
        [NotMapped] public string? TipoPagamento { get; set; }
        // ==== MODIFICA 28/09/2026 - INIZIO ==== Campi calcolati ritardo
        [NotMapped] public bool InRitardo { get; set; }
        [NotMapped] public int? GiorniRitardo { get; set; }
        // ==== MODIFICA 28/09/2026 - FINE ====
        // --- CLONE ---
        public ProspettoTradingAndConsulting CloneForInsert(string utente)
        {
            var nuovo = new ProspettoTradingAndConsulting();

            nuovo.IdMandatario = this.IdMandatario;
            nuovo.IdFileImportato = 0;
            nuovo.IdImportTradingAndConsulting = 0;
            nuovo.NumeroOrdine = this.NumeroOrdine;
            nuovo.NumeroDDT = null;
            nuovo.NumeroFattura = null;
            nuovo.IdCliente = this.IdCliente;
            nuovo.IdMateriale = this.IdMateriale;
            nuovo.IdUnitaMisura = this.IdUnitaMisura;
            nuovo.IdAgente = this.IdAgente;
            nuovo.IdTipoPagamento = this.IdTipoPagamento;

            nuovo.Quantita = this.Quantita;
            nuovo.CostoLavorazione = this.CostoLavorazione;
            nuovo.PrezzoBase = this.PrezzoBase;
            nuovo.PrezzoEuroRiferimento = this.PrezzoEuroRiferimento;//20/04/2026
            nuovo.PrezzoBaseConLavorazione = this.PrezzoBaseConLavorazione;

            nuovo.PrezzoVendita = this.PrezzoVendita;

            nuovo.ProvvigioneFissa01_05 = this.ProvvigioneFissa01_05;
            nuovo.ProvvigioneFissa03 = this.ProvvigioneFissa03;
            nuovo.Perc_Delta_Provvigione = this.Perc_Delta_Provvigione;//30/04/2026
            nuovo.TotaleProvvigioneFissa = this.TotaleProvvigioneFissa;
            nuovo.CIT = this.CIT;
            nuovo.Delta = this.Delta;
            nuovo.TotaleProvvigioneVariabile = this.TotaleProvvigioneVariabile;
            nuovo.TotaleProvvigioneFattura = this.TotaleProvvigioneFattura;

            nuovo.DataConsegnaIpotetica = this.DataConsegnaIpotetica;
            // ==== MODIFICA 28/09/2026 - INIZIO ====
            nuovo.DataConsegna = this.DataConsegna;
            nuovo.EscludiControlloRitardo = false; // L'ordine duplicato parte senza eccezione
            nuovo.DaFatturare = false; //30/09/2026 NON Mantieni lo stato di fatturazione
             // ==== MODIFICA 28/09/2026 - FINE ====

            // ==== 05/10/2026 MISURE STATISTICHE  - INIZIO ====
            nuovo.SpessoreNum = this.SpessoreNum;
            nuovo.LarghezzaNum = this.LarghezzaNum;
            nuovo.DiametroNum = this.DiametroNum;
            // ==== 05/10/2026 MISURE STATISTICHE  - FINE ====            

            nuovo.Scadenza = this.Scadenza;

            nuovo.Anno = this.Anno;
            nuovo.Mese = this.Mese;

            nuovo.Utente = utente;
            nuovo.DataInserimento = DateTime.Now;
            nuovo.DataModifica = DateTime.Now;

            nuovo.Fatturata = 0;
            nuovo.DataFatturazione = null;
            nuovo.IsDeleted = false;

            return nuovo;
        }
    }
}
