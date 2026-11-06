using System.ComponentModel.DataAnnotations;

namespace CalkosManager.Application.DTO
{
    /// <summary>DTO in lettura per lo storico calcoli base (modale "Selezione Calcolo Base").</summary>
    public class CalcoloBaseDto
    {
        public int Id { get; set; }
        public int IdMateriale { get; set; }
        public string? DescrizioneMateriale { get; set; }
        public DateTime DataRiferimento { get; set; }
        public decimal? Lme { get; set; }
        public decimal? PremioMiniera { get; set; }
        public decimal? Cambio { get; set; }
        public decimal? PercFinanziamento { get; set; }
        public decimal? EuroFinanziamento { get; set; }
        public decimal MaterialeEuroRiferimento { get; set; }
    }

    /// <summary>DTO in input per "Nuovo Calcolo Base" (creazione al volo dalla modale).</summary>
    public class CreateCalcoloBaseRequestDto
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Materiale obbligatorio")]
        public int IdMateriale { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Data di riferimento obbligatoria")]
        public DateTime DataRiferimento { get; set; }

        [System.ComponentModel.DataAnnotations.Range(0, 999999999, ErrorMessage = "Valore LME non valido")]
        public decimal? Lme { get; set; }

        [System.ComponentModel.DataAnnotations.Range(0, 999999999, ErrorMessage = "Valore Premio Miniera non valido")]
        public decimal? PremioMiniera { get; set; }

        [System.ComponentModel.DataAnnotations.Range(0, 999999999, ErrorMessage = "Cambio non valido")]
        public decimal? Cambio { get; set; }

        [System.ComponentModel.DataAnnotations.Range(0, 100, ErrorMessage = "Percentuale finanziamento non valida (0-100)")]
        public decimal? PercFinanziamento { get; set; }

        [System.ComponentModel.DataAnnotations.Range(0, 999999999, ErrorMessage = "Euro finanziamento non valido")]
        public decimal? EuroFinanziamento { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Il prezzo finale è obbligatorio")]
        [System.ComponentModel.DataAnnotations.Range(0.000001, 999999999, ErrorMessage = "Il prezzo finale deve essere maggiore di zero")]
        public decimal MaterialeEuroRiferimento { get; set; }
    }

    /// <summary>Risposta restituita alla view dopo selezione/creazione, per applicare subito il prezzo all'ordine.</summary>
    public class ApplicaCalcoloBaseResponseDto
    {
        public int IdCalcoloBase { get; set; }
        public decimal Prezzo { get; set; }
        public string DataRiferimento { get; set; } = string.Empty;
    }
}