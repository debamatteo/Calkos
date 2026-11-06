namespace CalkosManager.Domain.Models.Importazione
{
    public class RigaExcelCobral
    {
        public string? OrdineDDT { get; set; }
        public string? Cliente { get; set; }
        public decimal? Kg { get; set; }
        public string? Materiale { get; set; }
        public decimal? Prezzo { get; set; }
        public DateTime? Al { get; set; }

        public string? Spessore { get; set; }
        public string? Larghezza { get; set; }
        public decimal? Provvigione { get; set; }
        public decimal? AlluminioSpessore { get; set; }
        public decimal? OttoneSpessore { get; set; }
        public decimal? RameSpessore { get; set; }
        public decimal? AltrePercentuali { get; set; }
        public decimal? PrLavSpess { get; set; }
        public decimal? AlluminioLarghezza { get; set; }
        public decimal? OttoneLarghezza { get; set; }
        public decimal? RameLarghezza { get; set; }
        public decimal? BronzoLarghezza { get; set; }
        public decimal? PrLavLarg { get; set; }
        public decimal? ExtraPrezzoKg { get; set; }
        public decimal? ExtraPrezzoStagnato { get; set; }
        public decimal? PrLavTotale { get; set; }
        public decimal? Commissioni { get; set; }
        public decimal? PrezzoVendita { get; set; }
        public decimal? Differenza { get; set; }
        public DateTime? DataConsegnaIpotetica { get; set; }
        public string? Agente { get; set; }
        public DateTime? Scadenza { get; set; }
        public string? Fatturare { get; set; }
    }
}
