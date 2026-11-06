using System;
using System.ComponentModel.DataAnnotations;

namespace CalkosManager.Domain.Entities
{
    /// <summary>
    /// Rappresenta un materiale presente nella tabella SQL [Materiali].
    /// </summary>
    public class Materiale
    {
        [Key]
        public int IdMateriale { get; set; }

        /// <summary>
        /// Codice univoco del materiale. SQL: varchar(10) NOT NULL
        /// </summary>
        [Required(ErrorMessage = "Il codice materiale è obbligatorio")]
        [StringLength(10, ErrorMessage = "Il codice non può superare i 10 caratteri")]
        public string CodiceMateriale { get; set; } = string.Empty;

        /// <summary>
        /// Descrizione del materiale. SQL: varchar(100) NOT NULL
        /// </summary>
        [Required(ErrorMessage = "La descrizione materiale è obbligatoria")]
        [StringLength(100, ErrorMessage = "La descrizione non può superare i 100 caratteri")]
        public string DescrizioneMateriale { get; set; } = string.Empty;

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