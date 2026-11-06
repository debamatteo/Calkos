using System;
using System.ComponentModel.DataAnnotations;

namespace CalkosManager.Domain.Entities
{
    /// <summary>
    /// Entità TipoPagamento
    /// Mappa la tabella [dbo].[TipoPagamento]
    /// </summary>
    public class TipoPagamento
    {
        [Key]
        public int IdTipoPagamento { get; set; }

        /// <summary>
        /// Codice univoco (es. BON, RIBA). SQL: varchar(10) NOT NULL
        /// </summary>
        [Required(ErrorMessage = "Il codice è obbligatorio")]
        [StringLength(10, ErrorMessage = "Il codice non può superare i 10 caratteri")]
        public string CodiceTipoPagamento { get; set; } = string.Empty;

        /// <summary>
        /// Descrizione estesa. SQL: varchar(100) NOT NULL
        /// Corrisponde alla colonna [TipoPagamento] nel database.
        /// </summary>
        [Required(ErrorMessage = "La descrizione è obbligatoria")]
        [StringLength(100, ErrorMessage = "La descrizione non può superare i 100 caratteri")]
        public string DescrizioneTipoPagamento { get; set; } = string.Empty;

        [Required]
        public bool VisualizzaInDashboard { get; set; }

        /// <summary>
        /// Gruppo per aggregazione dashboard. SQL: nvarchar(50)
        /// </summary>
        [StringLength(50, ErrorMessage = "Il gruppo non può superare i 50 caratteri")]
        public string? GruppoDashboard { get; set; }

        [Required]
        public int OrdineVisualizzazione { get; set; }

        [Required]
        public DateTime DataInserimento { get; set; }

        public DateTime? DataModifica { get; set; }

        /// <summary>
        /// Utente che ha operato sul record. SQL: nvarchar(100)
        /// </summary>
        [StringLength(100)]
        public string? Utente { get; set; }
    }
}