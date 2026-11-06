using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
// ==== MODIFICA 28/09/2026 - INIZIO ==== Gestione Ordini in Ritardo
using CalkosManager.Domain.Interfaces;
// ==== MODIFICA 28/09/2026 - FINE ====
namespace CalkosManager.Domain.Entities
{
    public class ProspettoCobral : IRigaConDate // <-- 28/09/2026 - Implementazione interfaccia per gestione ordini in ritardo
    {
        [Key]
        public int IdProspettoCobral { get; set; }

        [Required]
        public int IdMandatario { get; set; }

        [Required]
        public int IdFileImportato { get; set; }

        [Required]
        public int IdImportCobral { get; set; }

        [Required]
        public int IdCliente { get; set; }

        [Required]
        public int IdMateriale { get; set; }

        [Required]
        public int IdUnitaMisura { get; set; }

        // --- DOCUMENTO ---
        //08/10/2026 numeroordine no è più obbligatorio, viene assegnto numero temporaneo se manca, quindi rimosso Required
        //[Required(ErrorMessage = "Il Numero Ordine è obbligatorio")] // Forza l'inserimento
        [StringLength(100)]
        public string NumeroOrdine { get; set; } // Rimosso il '?' per coerenza con Required

        [StringLength(100)]
        public string? NumeroDDT { get; set; }

        [StringLength(100)]
        public string? NumeroFattura { get; set; }

        // --- QUANTITÀ E PREZZI (decimal 18,6) ---
        [Column(TypeName = "decimal(18,6)")]
        public decimal? Quantita { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? Prezzo { get; set; }

        public DateTime? DataRiferimentoPrezzo { get; set; }

        [StringLength(100)]
        public string? Spessore { get; set; }

        [StringLength(100)]
        public string? Larghezza { get; set; }


        // ==== 05/10/2026 MISURE STATISTICHE  - INIZIO ====
        [Column(TypeName = "decimal(18,3)")]
        public decimal? SpessoreNum { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal? LarghezzaNum { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal? DiametroNum { get; set; }
        // ==== 05/10/2026 MISURE STATISTICHE  - FINE ====



        // --- COMPOSIZIONE COSTI E COMMISSIONI (decimal 18,6) ---
        [Column(TypeName = "decimal(18,6)")] public decimal? Provvigione { get; set; }
        [Column(TypeName = "decimal(18,6)")] public decimal? AlluminioSpessore { get; set; }
        [Column(TypeName = "decimal(18,6)")] public decimal? OttoneSpessore { get; set; }
        [Column(TypeName = "decimal(18,6)")] public decimal? RameSpessore { get; set; }
        [Column(TypeName = "decimal(18,6)")] public decimal? AltrePercentuali { get; set; }
        [Column(TypeName = "decimal(18,6)")] public decimal? PrLavSpess { get; set; }
        [Column(TypeName = "decimal(18,6)")] public decimal? AlluminioLarghezza { get; set; }
        [Column(TypeName = "decimal(18,6)")] public decimal? OttoneLarghezza { get; set; }
        [Column(TypeName = "decimal(18,6)")] public decimal? RameLarghezza { get; set; }
        [Column(TypeName = "decimal(18,6)")] public decimal? BronzoLarghezza { get; set; }
        [Column(TypeName = "decimal(18,6)")] public decimal? PrLavLarg { get; set; }
        [Column(TypeName = "decimal(18,6)")] public decimal? ExtraPrezzoKg { get; set; }
        [Column(TypeName = "decimal(18,6)")] public decimal? ExtraPrezzoStagnato { get; set; }
        [Column(TypeName = "decimal(18,6)")] public decimal? PrLavTotale { get; set; }
        [Column(TypeName = "decimal(18,6)")] public decimal? ValoreCommissioni { get; set; }
        [Column(TypeName = "decimal(18,6)")] public decimal? PrezzoVendita { get; set; }
        [Column(TypeName = "decimal(18,6)")] public decimal? Differenza { get; set; }
        [Column(TypeName = "decimal(18,6)")] public decimal? ProvvigioneAgente { get; set; }

        // --- DATE E LOOKUP ---

        public DateTime? DataConsegnaIpotetica { get; set; }
        // ==== MODIFICA 28/09/2026 - INIZIO ==== Gestione Ordini in Ritardo
        public DateTime? DataConsegna { get; set; }
        public bool EscludiControlloRitardo { get; set; }
        // ==== MODIFICA 28/09/2026 - FINE ====

        public bool DaFatturare { get; set; }//30/09/2026 
         public DateTime? Scadenza { get; set; }   

        public int? IdAgente { get; set; }
        public int? IdTipoPagamento { get; set; }

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

        // --- CAMPI DI SOLA LETTURA (Non nel DB ProspettoCobral) ---
        [NotMapped] public string? RagioneSociale { get; set; }
        [NotMapped] public string? DescrizioneMateriale { get; set; }
        [NotMapped] public string? UnitaMisura { get; set; }
        [NotMapped] public string? Agente { get; set; }
        [NotMapped] public decimal? PercentualeDefaultAgente { get; set; }
        [NotMapped] public string? TipoPagamento { get; set; }

        // ==== MODIFICA 28/09/2026 - INIZIO ==== Campi calcolati ritardo
        [NotMapped] public bool InRitardo { get; set; }
        [NotMapped] public int? GiorniRitardo { get; set; }
        // ==== MODIFICA 28/09/2026 - FINE ====
        public ProspettoCobral CloneForInsert(string utente)
        {
            var nuovo = new ProspettoCobral();

            nuovo.IdMandatario = this.IdMandatario;
            nuovo.IdFileImportato = 0;
            nuovo.IdImportCobral = 0;
            nuovo.NumeroOrdine = this.NumeroOrdine;
            nuovo.NumeroDDT = null;
            nuovo.NumeroFattura = null;
            nuovo.IdCliente = this.IdCliente;
            nuovo.IdMateriale = this.IdMateriale;
            nuovo.IdUnitaMisura = this.IdUnitaMisura;
            nuovo.IdAgente = this.IdAgente;
            nuovo.IdTipoPagamento = this.IdTipoPagamento;
            nuovo.Quantita = this.Quantita;
            nuovo.Prezzo = this.Prezzo;
            nuovo.DataRiferimentoPrezzo = this.DataRiferimentoPrezzo;
            nuovo.Spessore = this.Spessore;
            nuovo.Larghezza = this.Larghezza;
            nuovo.AlluminioSpessore = this.AlluminioSpessore;
            nuovo.OttoneSpessore = this.OttoneSpessore;
            nuovo.RameSpessore = this.RameSpessore;
            nuovo.AltrePercentuali = this.AltrePercentuali;
            nuovo.PrLavSpess = this.PrLavSpess;
            nuovo.AlluminioLarghezza = this.AlluminioLarghezza;
            nuovo.OttoneLarghezza = this.OttoneLarghezza;
            nuovo.RameLarghezza = this.RameLarghezza;
            nuovo.BronzoLarghezza = this.BronzoLarghezza;
            nuovo.PrLavLarg = this.PrLavLarg;
            nuovo.ExtraPrezzoKg = this.ExtraPrezzoKg;
            nuovo.ExtraPrezzoStagnato = this.ExtraPrezzoStagnato;
            nuovo.PrLavTotale = this.PrLavTotale;
            nuovo.Provvigione = this.Provvigione;
            nuovo.ValoreCommissioni = this.ValoreCommissioni;
            nuovo.PrezzoVendita = this.PrezzoVendita;
            nuovo.Differenza = this.Differenza;
            nuovo.ProvvigioneAgente = this.ProvvigioneAgente;
            nuovo.DataConsegnaIpotetica = this.DataConsegnaIpotetica;
            // ==== MODIFICA 28/09/2026 - INIZIO ====
            nuovo.DataConsegna = null ;//this.DataConsegna; la DataConsegna viene azzerata per l'ordine duplicato, così da poter essere reinserita correttamente
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
