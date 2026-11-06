namespace CalkosManager.Domain.Entities
{
    public class RigaMerceConsegnataDdtDto
    {
        public string Mandatario { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public string Materiale { get; set; } = string.Empty;
        public int Anno { get; set; }
        public int Mese { get; set; }
        public decimal? Diametro { get; set; }
        public decimal? Spessore { get; set; }
        public decimal? Larghezza { get; set; }
        public decimal QuantitaTotale { get; set; }
        public string UnitaMisura { get; set; } = string.Empty;
    }
}