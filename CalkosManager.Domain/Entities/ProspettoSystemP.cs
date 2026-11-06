using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// ==== MODIFICA 28/09/2026 - INIZIO ==== Gestione Ordini in Ritardo
using CalkosManager.Domain.Interfaces;
// ==== MODIFICA 28/09/2026 - FINE ====
namespace CalkosManager.Domain.Entities
{
    public class ProspettoSystemP : IRigaConDate // <-- 28/09/2026 - Implementazione interfaccia per gestione ordini in ritardo
    {
        [Key]
        public int IdProspettoSystemP { get; set; }

        [Required]
        public int IdMandatario { get; set; }

        [Required]
        public int IdFileImportato { get; set; }

        [Required]
        public int IdImportSystemP { get; set; }

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
        public decimal? LavorazionePrezzoAcquisto { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? BaseAcquisto { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? CIT { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? PrezzoAcquisto { get; set; }

        public DateTime? DataRiferimentoPrezzoAcquisto { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? CIT_EUR_KG { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? CITsuAcquisto { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? ProvvigioneUnitaria { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? ValoreProvvigioneFissa { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? LavorazionePrezzoVendita { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? BaseVendita { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? CITsuVendita { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? PrezzoVendita { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? Delta { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? PercentualeDelta { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? TotaleVariabileProvvigione { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? TotaleProvvigione { get; set; }

        // --- DATE ---
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
        // --- AGENTE / PAGAMENTO ---
        public int? IdAgente { get; set; }

        public int? IdTipoPagamento { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? ProvvigioneAgente { get; set; }

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
        public ProspettoSystemP CloneForInsert(string utente)
        {
            var nuovo = new ProspettoSystemP();

            nuovo.IdMandatario = this.IdMandatario;
            nuovo.IdFileImportato = 0;
            nuovo.IdImportSystemP = 0;

            nuovo.NumeroOrdine = this.NumeroOrdine;
            nuovo.NumeroDDT = null;
            nuovo.NumeroFattura = null;

            nuovo.IdCliente = this.IdCliente;
            nuovo.IdMateriale = this.IdMateriale;
            nuovo.IdUnitaMisura = this.IdUnitaMisura;

            nuovo.Quantita = this.Quantita;
            nuovo.LavorazionePrezzoAcquisto = this.LavorazionePrezzoAcquisto;
            nuovo.BaseAcquisto = this.BaseAcquisto;
            nuovo.CIT = this.CIT;
            nuovo.PrezzoAcquisto = this.PrezzoAcquisto;
            nuovo.DataRiferimentoPrezzoAcquisto = this.DataRiferimentoPrezzoAcquisto;
            nuovo.CIT_EUR_KG = this.CIT_EUR_KG;
            nuovo.CITsuAcquisto = this.CITsuAcquisto;
            nuovo.ProvvigioneUnitaria = this.ProvvigioneUnitaria;
            nuovo.ValoreProvvigioneFissa = this.ValoreProvvigioneFissa;
            nuovo.LavorazionePrezzoVendita = this.LavorazionePrezzoVendita;
            nuovo.BaseVendita = this.BaseVendita;
            nuovo.CITsuVendita = this.CITsuVendita;
            nuovo.PrezzoVendita = this.PrezzoVendita;
            nuovo.Delta = this.Delta;
            nuovo.PercentualeDelta = this.PercentualeDelta;
            nuovo.TotaleVariabileProvvigione = this.TotaleVariabileProvvigione;
            nuovo.TotaleProvvigione = this.TotaleProvvigione;

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

            nuovo.IdAgente = this.IdAgente;
            nuovo.IdTipoPagamento = this.IdTipoPagamento;
            nuovo.ProvvigioneAgente = this.ProvvigioneAgente;

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
