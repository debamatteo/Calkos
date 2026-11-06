namespace CalkosManager.Domain.Entities
{
    public class FiltriMerceConsegnataDdtDto
    {
        // --- FILTRI (restringono i dati) ---
        public int? IdMandatario { get; set; }
        public int? IdCliente { get; set; }
        public int? IdMateriale { get; set; }
        public int? AnnoDa { get; set; }   // intervallo anni (estremi inclusi); uno solo dei due = limite aperto
        public int? AnnoA { get; set; }
        public int? Mese { get; set; }
        public decimal? Diametro { get; set; }
        public decimal? Spessore { get; set; }
        public decimal? Larghezza { get; set; }

        // --- RAGGRUPPAMENTO (sceglie le dimensioni mostrate nel report) ---
        // Default: tutto tranne le misure, che si attivano su richiesta o se si filtra per misura.
        public bool RaggruppaPerMandatario { get; set; } = true;
        public bool RaggruppaPerCliente { get; set; } = true;
        public bool RaggruppaPerMateriale { get; set; } = true;
        public bool RaggruppaPerAnno { get; set; } = true;
        public bool RaggruppaPerMese { get; set; } = true;
        public bool RaggruppaPerMisure { get; set; } = false;
    }
}
