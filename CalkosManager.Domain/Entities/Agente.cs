using System;
using System.ComponentModel.DataAnnotations;

namespace CalkosManager.Domain.Entities
{
    public class Agente
    {
        [Key]
        public int IdAgente { get; set; }

        [Required(ErrorMessage = "Il Nome è obbligatorio")]
        [StringLength(100, ErrorMessage = "Il Nome non può superare i 100 caratteri")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Il Cognome è obbligatorio")]
        [StringLength(100, ErrorMessage = "Il Cognome non può superare i 100 caratteri")]
        public string Cognome { get; set; } = string.Empty;

        // Campo calcolato lato SQL (sola lettura in C#)
        public string? AgenteDescrizione { get; set; }

        [StringLength(11, ErrorMessage = "La Partita IVA deve essere di massimo 11 caratteri")]
        public string? PartitaIva { get; set; }

        [StringLength(16, ErrorMessage = "Il Codice Fiscale deve essere di massimo 16 caratteri")]
        public string? CodiceFiscale { get; set; }

        [Range(0, 100, ErrorMessage = "La percentuale deve essere compresa tra 0 e 100")]//19/10/2026
        public decimal? PercentualeDefault { get; set; }

        [EmailAddress(ErrorMessage = "Formato Email non valido")]
        [StringLength(100, ErrorMessage = "L'Email non può superare i 100 caratteri")]
        public string? Email { get; set; }

        [StringLength(30, ErrorMessage = "Il telefono non può superare i 30 caratteri")]
        public string? Telefono1 { get; set; }

        [StringLength(30, ErrorMessage = "Il telefono non può superare i 30 caratteri")]
        public string? Telefono2 { get; set; }

        [StringLength(100)]
        public string? Utente { get; set; }

        [Required]
        public DateTime DataInserimento { get; set; }

        public DateTime? DataModifica { get; set; }
    }
}