using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// ==== MODIFICA 28/09/2026 - INIZIO ==== Gestione Ordini in Ritardo
using CalkosManager.Domain.Interfaces;
// ==== MODIFICA 28/09/2026 - FINE ====
namespace CalkosManager.Domain.Entities
{
    public class ProspettoElektraWire : IRigaConDate // <-- 28/09/2026 - Implementazione interfaccia per gestione ordini in ritardo
    {
        /// <summary>
        /// Prospetto definitivo ElektraWire.
        /// Tabella: dbo.ProspettoElektraWire
        /// </summary>

        [Key]
        public int IdProspettoElektraWire { get; set; }

        [Required] public int IdMandatario { get; set; }
        [Required] public int IdFileImportato { get; set; }
        [Required] public int IdImportElektraWire { get; set; }
        [Required] public int IdCliente { get; set; }
        [Required] public int IdMateriale { get; set; }
        [Required] public int IdUnitaMisura { get; set; }

        // --- DOCUMENTO E MISURE ---
        //08/10/2026 numeroordine no è più obbligatorio, viene assegnto numero temporaneo se manca, quindi rimosso Required
        [StringLength(100)] public string? NumeroOrdine { get; set; }
        [StringLength(100)] public string? NumeroDDT { get; set; }
        [StringLength(100)] public string? NumeroFattura { get; set; }
        public string? Misure { get; set; }

        // --- QUANTITÀ E PREZZI (Precisione 18, 3) ---
        [Column(TypeName = "decimal(18, 6)")] public decimal? Quantita { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? CostoLavorazione { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? PrezzoBase { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? PrezzoConLavorazione { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? PrezzoVendita { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? DeltaMargine { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? Perc_Delta_Provvigione { get; set; }//27/04/2026

        // --- BLOCCHI PROVVIGIONALI ELEKTRAWIRE (Precisione 18, 5) ---
        [Column(TypeName = "decimal(18, 6)")] public decimal? ValUnitProvvigioneFissa1_5 { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? Delta1ProvvigioneFissa1_5 { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? Delta2ProvvigioneFissa1_5 { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? TotProvvigioneFissa1_5 { get; set; }

        [Column(TypeName = "decimal(18, 6)")] public decimal? ValQuotaFissa_1_5 { get; set; }

        [Column(TypeName = "decimal(18, 6)")] public decimal? ValUnitProvvigioneFissa_03 { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? Delta1ProvvigioneFissa_03 { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? Delta2ProvvigioneFissa_03 { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? TotProvvigioneFissa_03 { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? ValQuotaFissa_03 { get; set; }

        [Column(TypeName = "decimal(18, 6)")] public decimal? TotaleFissoProvvigione { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? DeltaResiduoVariabile { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? TotaleVariabileProvvigione { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? TotaleSommaProvvigioni { get; set; }

        // --- TOTALI E GESTIONE AGENTE (Precisione 18, 3 come da SQL) ---
        [Column(TypeName = "decimal(18, 6)")] public decimal? TotaleProvvigioneDaFatturare { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? ProvvigioneAgente { get; set; }

        //[StringLength(50)] public string? Fatturare { get; set; }

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

        public DateTime? Scadenza { get; set; }//18/04/2026
        public int? IdAgente { get; set; }
        public int? IdTipoPagamento { get; set; }

        [Required] public DateTime DataInserimento { get; set; }
        public DateTime? DataModifica { get; set; }
        [StringLength(100)] public string? Utente { get; set; }

        public int? Anno { get; set; }
        public int? Mese { get; set; }
        [Required] public int Fatturata { get; set; }
        public DateTime? DataFatturazione { get; set; }
        [Required] public bool IsDeleted { get; set; }

        // --- PROPRIETÀ NON MAPPATE (Per UI/Business Logic) ---
        [NotMapped] public string? RagioneSociale { get; set; }
        [NotMapped] public string? DescrizioneMateriale { get; set; }
        [NotMapped] public string? UnitaMisura { get; set; }
        // ==== MODIFICA 28/09/2026 - INIZIO ==== Campi calcolati ritardo
        [NotMapped] public bool InRitardo { get; set; }
        [NotMapped] public int? GiorniRitardo { get; set; }
        // ==== MODIFICA 28/09/2026 - FINE ====
        /// <summary>
        /// Crea una copia per inserimento manuale o duplicazione.
        /// </summary>
        public ProspettoElektraWire CloneForInsert(string utente)
        {
            var nuovo = new ProspettoElektraWire();

            nuovo.IdMandatario = this.IdMandatario;
            nuovo.IdFileImportato = 0;
            nuovo.IdImportElektraWire = 0;

            nuovo.IdCliente = this.IdCliente;
            nuovo.IdMateriale = this.IdMateriale;
            nuovo.IdUnitaMisura = this.IdUnitaMisura;
            nuovo.IdAgente = this.IdAgente;
            nuovo.IdTipoPagamento = this.IdTipoPagamento;

            nuovo.NumeroOrdine = this.NumeroOrdine;
            nuovo.NumeroDDT = null;
            nuovo.NumeroFattura = null;
            nuovo.Misure = this.Misure;

            nuovo.Quantita = this.Quantita;
            nuovo.CostoLavorazione = this.CostoLavorazione;
            nuovo.PrezzoBase = this.PrezzoBase;
            nuovo.PrezzoConLavorazione = this.PrezzoConLavorazione;
            nuovo.PrezzoVendita = this.PrezzoVendita;
            nuovo.DeltaMargine = this.DeltaMargine;
            nuovo.Perc_Delta_Provvigione = this.Perc_Delta_Provvigione;//27/04/2026

            nuovo.ValUnitProvvigioneFissa1_5 = this.ValUnitProvvigioneFissa1_5;
            nuovo.Delta1ProvvigioneFissa1_5 = this.Delta1ProvvigioneFissa1_5;
            nuovo.Delta2ProvvigioneFissa1_5 = this.Delta2ProvvigioneFissa1_5;
            nuovo.TotProvvigioneFissa1_5 = this.TotProvvigioneFissa1_5;
            nuovo.ValQuotaFissa_1_5 = this.ValQuotaFissa_1_5;

            nuovo.ValUnitProvvigioneFissa_03 = this.ValUnitProvvigioneFissa_03;
            nuovo.Delta1ProvvigioneFissa_03 = this.Delta1ProvvigioneFissa_03;
            nuovo.Delta2ProvvigioneFissa_03 = this.Delta2ProvvigioneFissa_03;
            nuovo.TotProvvigioneFissa_03 = this.TotProvvigioneFissa_03;
            nuovo.ValQuotaFissa_03 = this.ValQuotaFissa_03;

            nuovo.TotaleFissoProvvigione = this.TotaleFissoProvvigione;
            nuovo.DeltaResiduoVariabile = this.DeltaResiduoVariabile;
            nuovo.TotaleVariabileProvvigione = this.TotaleVariabileProvvigione;
            nuovo.TotaleSommaProvvigioni = this.TotaleSommaProvvigioni;

            nuovo.TotaleProvvigioneDaFatturare = this.TotaleProvvigioneDaFatturare;
            nuovo.ProvvigioneAgente = this.ProvvigioneAgente;

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

            nuovo.Scadenza = this.Scadenza;//18/04/2026
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