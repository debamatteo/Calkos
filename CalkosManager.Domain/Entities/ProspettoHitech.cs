using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
// ==== MODIFICA 28/09/2026 - INIZIO ==== Gestione Ordini in Ritardo
using CalkosManager.Domain.Interfaces;
// ==== MODIFICA 28/09/2026 - FINE ====
namespace CalkosManager.Domain.Entities
{
    public class ProspettoHitech : IRigaConDate // <-- 28/09/2026 - Implementazione interfaccia per gestione ordini in ritardo
    {
        [Key]
        public int IdProspettoHitech { get; set; }

        [Required]
        public int IdMandatario { get; set; }

        [Required]
        public int IdFileImportato { get; set; }

        [Required]
        public int IdImportHitech { get; set; }

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

        // --- QUANTITÀ E PREZZI ---
        [Column(TypeName = "decimal(18,6)")]
        public decimal? Quantita { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? PrezzoBase { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? PrezzoPraticato { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? ProvvigioneUnitaria { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? TotaleProvvigione { get; set; }

        // --- DATE E LOOKUP ---

        public DateTime? DataDoc { get; set; } //Data 20/04/2026
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
        public bool? Fatturare { get; set; }

        public int? IdAgente { get; set; }

        public int? IdTipoPagamento { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? ProvvigioneAgente { get; set; }

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
        // 5) Campi NotMapped
        //    Cobral: Agente + PercentualeDefaultAgente
        //    Hitech: AgenteDescrizione + PercentualeDefaultAgente
        //    → OK: la VIEW Hitech restituisce AgenteDescrizione, quindi corretto.
        [NotMapped] public string? AgenteDescrizione { get; set; }
        [NotMapped] public decimal? PercentualeDefaultAgente { get; set; }
        [NotMapped] public string? TipoPagamento { get; set; }

        // ==== MODIFICA 28/09/2026 - INIZIO ==== Campi calcolati ritardo
        [NotMapped] public bool InRitardo { get; set; }
        [NotMapped] public int? GiorniRitardo { get; set; }
        // ==== MODIFICA 28/09/2026 - FINE ====

        // --- CLONE (stesso stile Cobral) ---
        public ProspettoHitech CloneForInsert(string utente)
        {
            var nuovo = new ProspettoHitech();

            nuovo.IdMandatario = this.IdMandatario;
            nuovo.IdFileImportato = 0;
            nuovo.IdImportHitech = 0;
            nuovo.NumeroOrdine = this.NumeroOrdine;
            nuovo.NumeroDDT = null;
            nuovo.NumeroFattura = null;
            nuovo.IdCliente = this.IdCliente;
            nuovo.IdMateriale = this.IdMateriale;
            nuovo.IdUnitaMisura = this.IdUnitaMisura;
            nuovo.IdAgente = this.IdAgente;
            nuovo.IdTipoPagamento = this.IdTipoPagamento;

            nuovo.Quantita = this.Quantita;
            nuovo.PrezzoBase = this.PrezzoBase;
            nuovo.PrezzoPraticato = this.PrezzoPraticato;
            nuovo.ProvvigioneUnitaria = this.ProvvigioneUnitaria;
            nuovo.TotaleProvvigione = this.TotaleProvvigione;
            
            nuovo.DataDoc = this.DataDoc;//21/04/2026
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
