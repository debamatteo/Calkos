using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CalkosManager.Domain.Entities
{
    public class CalcoloBaseMateriale
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int IdMateriale { get; set; }

        [Required]
        public DateTime DataRiferimento { get; set; }

        [Column(TypeName = "decimal(18, 6)")] public decimal? Lme { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? PremioMiniera { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? Cambio { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? PercFinanziamento { get; set; }
        [Column(TypeName = "decimal(18, 6)")] public decimal? EuroFinanziamento { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 6)")]
        public decimal MaterialeEuroRiferimento { get; set; }

        [Required]
        public DateTime DataInserimento { get; set; }
        public DateTime? DataModifica { get; set; } 

        [StringLength(100)]
        public string? Utente { get; set; }
            
        // --- SOLA LETTURA (join) ---
        [NotMapped] public string? DescrizioneMateriale { get; set; }
    }
}