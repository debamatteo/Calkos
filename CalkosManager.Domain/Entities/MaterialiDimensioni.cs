using System;
using System.ComponentModel.DataAnnotations;

namespace CalkosManager.Domain.Entities
{
    public class MaterialiDimensioni
    {
        [Key]
        public int IdDimensione { get; set; }

        [Required(ErrorMessage = "Il materiale è obbligatorio")]
        public int IdMateriale { get; set; }

        /// <summary>
        /// Tipo di dimensione (es. Spessore, Diametro). SQL suggerito: nvarchar(20)
        /// </summary>
        [Required(ErrorMessage = "Il tipo è obbligatorio")]
        [StringLength(20, ErrorMessage = "Il tipo non può superare i 20 caratteri")]
        public string Tipo { get; set; } = string.Empty;

        /// <summary>
        /// Valore della dimensione. SQL suggerito: nvarchar(100)
        /// </summary>
        [Required(ErrorMessage = "Il valore è obbligatorio")]
        [StringLength(100, ErrorMessage = "Il valore non può superare i 100 caratteri")]
        public string Valore { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'ordine di visualizzazione è obbligatorio")]
        public int Ordine { get; set; }

        // --- CAMPI AUDIT ---

        [Required]
        public DateTime DataInserimento { get; set; }

        public DateTime? DataModifica { get; set; }

        [StringLength(100)]
        public string? Utente { get; set; }
    }
}