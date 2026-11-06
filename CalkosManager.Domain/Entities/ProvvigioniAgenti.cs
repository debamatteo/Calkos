using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CalkosManager.Domain.Entities
{
    public class ProvvigioniAgenti
    {
        [Key]
        public int IdProvvigione { get; set; }

        [Required(ErrorMessage = "L'agente è obbligatorio")]
        public int IdAgente { get; set; }

        [Required(ErrorMessage = "Il mandatario è obbligatorio")]
        public int IdMandatario { get; set; }

        public int? IdCliente { get; set; }

        /// <summary>
        /// Percentuale provvigionale. SQL: decimal(5, 2)
        /// </summary>
        [Required(ErrorMessage = "La percentuale è obbligatoria")]
        [Range(0, 100, ErrorMessage = "La percentuale deve essere compresa tra 0 e 100")]
        [Column(TypeName = "decimal(5, 2)")]
        public decimal Percentuale { get; set; }

        [Required]
        public DateTime DataInserimento { get; set; }

        public DateTime? DataModifica { get; set; }

        /// <summary>
        /// Utente che ha inserito o modificato il record. SQL: nvarchar(100)
        /// </summary>
        [StringLength(100)]
        public string? Utente { get; set; }
    }
}