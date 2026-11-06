using System;
using System.ComponentModel.DataAnnotations;

namespace CalkosManager.Domain.Entities
{
    /// <summary>
    /// Rappresenta un Mandatario (Cobral, De Angeli, ElektraWire, ecc.)
    /// Tabella: dbo.Mandatari
    /// </summary>
    public class Mandatario
    {
        [Key]
        public int IdMandatario { get; set; }

        /// <summary>
        /// Nome del Mandatario (es. "Cobral").
        /// </summary>
        [Required(ErrorMessage = "Il nome del mandatario è obbligatorio")]
        [StringLength(100, ErrorMessage = "Il nome non può superare i 100 caratteri")]
        public string NomeMandatario { get; set; } = string.Empty;

        /// <summary>
        /// CodiceMandatario (es. "COB"). 
        /// Nota: In SQL è NULL, ma se lo vuoi obbligatorio lato UI mantieni Required.
        /// </summary>
        [StringLength(20, ErrorMessage = "Il codice non può superare i 20 caratteri")]
        public string? CodiceMandatario { get; set; }

        /// <summary>
        /// Riferimento al tipo di pagamento predefinito per il mandatario.
        /// </summary>
        [Required(ErrorMessage = "Il tipo di pagamento è obbligatorio")]
        public int IdTipoPagamento { get; set; }

        [Required]
        public DateTime DataInserimento { get; set; }

        public DateTime? DataModifica { get; set; }

        [StringLength(100)]
        public string? Utente { get; set; }


        // ==== MODIFICA 29/10/2026 - INIZIO ==== Perc. Delta Provvigione di default del mandatario
        /// <summary>
        /// Percentuale sul Delta proposta in automatico nei NUOVI ordini del mandatario.
        /// Formato "umano": 40 = 40%. Colonna SQL: decimal(18,6) NULL, DEFAULT 0.
        /// </summary>
        [Range(0, 100, ErrorMessage = "La percentuale deve essere compresa tra 0 e 100")]
        public decimal? Perc_Delta_Provvigione { get; set; }
        // ==== MODIFICA 29/10/2026 - FINE ====



        // ==== MODIFICA 01/11/2026 - INIZIO ==== Default per i NUOVI ordini del mandatario
        /// <summary>Unità di misura proposta nei nuovi ordini (tutti i mandatari). NULL = nessun default.</summary>
        public int? IdUnitaMisuraDefault { get; set; }

        /// <summary>Quota Euro cent/Kg proposta nei nuovi ordini (usata da SystemCore). 0 = nessun valore automatico.</summary>      
        public decimal? QuotaCentesimiPerKg { get; set; }
        // ==== MODIFICA 01/11/2026 - FINE ====


    }
}