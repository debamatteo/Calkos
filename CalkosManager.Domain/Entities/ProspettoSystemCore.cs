using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// ==== MODIFICA 28/09/2026 - INIZIO ==== Gestione Ordini in Ritardo
using CalkosManager.Domain.Interfaces;
// ==== MODIFICA 28/09/2026 - FINE ====
namespace CalkosManager.Domain.Entities
{
    public class ProspettoSystemCore : IRigaConDate // <-- 28/09/2026 - Implementazione interfaccia per gestione ordini in ritardo
    {
        [Key]
        public int IdProspettoSystemCore { get; set; }

        [Required]
        public int IdMandatario { get; set; }

        [Required]
        public int IdFileImportato { get; set; }

        [Required]
        public int IdImportSystemCore { get; set; }

        [Required]
        public int IdCliente { get; set; }

        [Required]
        public int IdMateriale { get; set; }

        [Required]
        public int IdUnitaMisura { get; set; }

        // --- DOCUMENTO ---
        //08/10/2026 numeroordine no è più obbligatorio, viene assegnto numero temporaneo se manca, quindi rimosso Required
        //[Required(ErrorMessage = "Il Numero Ordine è obbligatorio")]
        [StringLength(100)]
        public string NumeroOrdine { get; set; }

        [StringLength(100)]
        public string? NumeroDDT { get; set; }

        [StringLength(100)]
        public string? NumeroFattura { get; set; }

        [StringLength(200)]
        public string? Misure { get; set; }

        // --- QUANTITÀ E VALORI ---
        [Column(TypeName = "decimal(18,6)")]
        public decimal? Quantita { get; set; }

        //[Column(TypeName = "decimal(18,6)")]
        //public decimal? Kg { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? PrezzoVendita { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? CostoLavorazione { get; set; }

        // --- PROVVIGIONI ---
        [Column(TypeName = "decimal(18,6)")]
        public decimal? PercentualeQuotaFissa { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? ValoreQuotaFissa { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? TotaleProvvigioneVariabile { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? TotaleProvvigione { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? TotaleProvvigioneDaFatturare { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? Delta { get; set; }



        [Column(TypeName = "decimal(18,6)")]
        public decimal? Perc_Delta_Provvigione { get; set; }//26/04/2026

        [Column(TypeName = "decimal(18,6)")]
        public decimal? QuotaCentesimiPerKg { get; set; }//26/04/2026


        // --- AGENTE / PAGAMENTO ---
        public int? IdAgente { get; set; }

        public int? IdTipoPagamento { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? ProvvigioneAgente { get; set; }

        // --- DATE ---
        public DateTime? DataConsegnaIpotetica { get; set; }
        // ==== MODIFICA 28/09/2026 - INIZIO ==== Gestione Ordini in Ritardo
        public DateTime? DataConsegna { get; set; }
        public bool EscludiControlloRitardo { get; set; }
        public bool DaFatturare { get; set; }//30/09/2026 
        // ==== MODIFICA 28/09/2026 - FINE ====


        // ==== 05/10/2026 MISURE STATISTICHE  - INIZIO ====
        [Column(TypeName = "decimal(18,3)")]
        public decimal? SpessoreNum { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal? LarghezzaNum { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal? DiametroNum { get; set; }
        // ==== 05/10/2026 MISURE STATISTICHE  - FINE ====

        public DateTime? Scadenza { get; set; }

        public DateTime? DataFatturazione { get; set; }

        // --- STATO ---
        [Required]
        public int Fatturata { get; set; }

        [Required]
        public bool IsDeleted { get; set; }

        public bool? Fatturare { get; set; }

        [Required]
        public bool IsProcessed { get; set; }

        // --- AUDIT ---
        [Required]
        public DateTime DataInserimento { get; set; }

        public DateTime? DataModifica { get; set; }

        [StringLength(100)]
        public string? Utente { get; set; }

        [StringLength(200)]
        public string? UniqueKey { get; set; }

        public int? Anno { get; set; }

        public int? Mese { get; set; }

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
        public ProspettoSystemCore CloneForInsert(string utente)
        {
            var nuovo = new ProspettoSystemCore();

            nuovo.IdMandatario = this.IdMandatario;
            nuovo.IdFileImportato = 0;
            nuovo.IdImportSystemCore = 0;

            nuovo.NumeroOrdine = this.NumeroOrdine;
            nuovo.NumeroDDT = null;
            nuovo.NumeroFattura = null;

            nuovo.IdCliente = this.IdCliente;
            nuovo.IdMateriale = this.IdMateriale;
            nuovo.IdUnitaMisura = this.IdUnitaMisura;

            nuovo.Misure = this.Misure;
            nuovo.Quantita = this.Quantita;
           
            nuovo.PrezzoVendita = this.PrezzoVendita;
            nuovo.CostoLavorazione = this.CostoLavorazione;

            nuovo.PercentualeQuotaFissa = this.PercentualeQuotaFissa;
            nuovo.ValoreQuotaFissa = this.ValoreQuotaFissa;
            nuovo.TotaleProvvigioneVariabile = this.TotaleProvvigioneVariabile;
            nuovo.TotaleProvvigione = this.TotaleProvvigione;
            nuovo.TotaleProvvigioneDaFatturare = this.TotaleProvvigioneDaFatturare;
            nuovo.Delta = this.Delta;

            
            nuovo.Perc_Delta_Provvigione = this.Perc_Delta_Provvigione;//26/04/2026
            nuovo.QuotaCentesimiPerKg = this.QuotaCentesimiPerKg;//26/04/2026


            nuovo.IdAgente = this.IdAgente;
            nuovo.IdTipoPagamento = this.IdTipoPagamento;
            nuovo.ProvvigioneAgente = this.ProvvigioneAgente;

            nuovo.DataConsegnaIpotetica = this.DataConsegnaIpotetica;
            // ==== MODIFICA 28/09/2026 - INIZIO ====
            nuovo.DataConsegna = this.DataConsegna;
            nuovo.EscludiControlloRitardo = false; // L'ordine duplicato parte senza eccezione
            nuovo.DaFatturare = false; //30/09/2026 NON Mantieni lo stato di fatturazione

            // ==== 05/10/2026 MISURE STATISTICHE  - INIZIO ====
            nuovo.SpessoreNum = this.SpessoreNum;
            nuovo.LarghezzaNum = this.LarghezzaNum;
            nuovo.DiametroNum = this.DiametroNum;
            // ==== 05/10/2026 MISURE STATISTICHE  - FINE ====

            // ==== MODIFICA 28/09/2026 - FINE ====
            nuovo.Scadenza = this.Scadenza;

            nuovo.Anno = this.Anno;
            nuovo.Mese = this.Mese;

            nuovo.Utente = utente;
            nuovo.DataInserimento = DateTime.Now;
            nuovo.DataModifica = DateTime.Now;

            nuovo.Fatturata = 0;
            nuovo.DataFatturazione = null;
            nuovo.IsDeleted = false;
            nuovo.IsProcessed = false;
            nuovo.Fatturare = false;

            return nuovo;
        }
    }
}
