using System;
using System.ComponentModel.DataAnnotations;

namespace CalkosManager.Domain.Entities
{
    /// <summary>
    /// Rappresenta l'entità UnitaMisura mappata sulla tabella dbo.UnitaMisura
    /// </summary>
    public class UnitaMisura
    {
        [Key]
        public int IdUnitaMisura { get; set; }

        /// <summary>
        /// Codice abbreviato (es. KG, MT). SQL: varchar(10) NOT NULL
        /// </summary>
        [Required(ErrorMessage = "Il codice unità di misura è obbligatorio")]
        [StringLength(10, ErrorMessage = "Il codice non può superare i 10 caratteri")]
        public string CodiceUnitaMisura { get; set; } = string.Empty;

        /// <summary>
        /// Descrizione dell'unità di misura. SQL: varchar(100) NOT NULL
        /// Corrisponde alla colonna [UnitaMisura] nel database.
        /// </summary>
        [Required(ErrorMessage = "La descrizione è obbligatoria")]
        [StringLength(100, ErrorMessage = "La descrizione non può superare i 100 caratteri")]
        public string UnitaMisuraDescrizione { get; set; } = string.Empty;

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