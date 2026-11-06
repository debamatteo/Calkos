namespace CalkosManager.Application.DTO.Statistiche
{
    /// <summary>
    /// Report 1 - Fatturato mandatari in un singolo anno, mese per mese.
    /// Struttura "pronta da scrivere": una riga per mandatario, 12 mesi, totali.
    /// </summary>
    public sealed class ReportFatturatoAnnualeDto
    {
        public int Anno { get; init; }
        public IReadOnlyList<RigaFatturatoAnnualeDto> Righe { get; init; } = Array.Empty<RigaFatturatoAnnualeDto>();

        // ==== MODIFICA 29/09/2026 - INIZIO ====
        // Stessa logica del Report 2: totali quantità separati per unità di misura.

        /// <summary>Codici U.M. da totalizzare (es. KG, PZ), in ordine alfabetico.</summary>
        public IReadOnlyList<string> UnitaMisuraTotali { get; init; } = Array.Empty<string>();

        /// <summary>Mandatari con quantità ma U.M. mancante o "MISTO" (esclusi dai totali per unità).</summary>
        public IReadOnlyList<string> MandatariQuantitaNonTotalizzate { get; init; } = Array.Empty<string>();
        // ==== MODIFICA 29/09/2026 - FINE ====
    }

    public sealed class RigaFatturatoAnnualeDto
    {
        public int IdMandatario { get; init; }
        public string NomeMandatario { get; init; } = "";
        public string UnitaMisura { get; init; } = "";

        /// <summary>Indice 0 = gennaio ... 11 = dicembre.</summary>
        public decimal[] QuantitaMensile { get; init; } = new decimal[12];

        /// <summary>Indice 0 = gennaio ... 11 = dicembre.</summary>
        public decimal[] EuroMensile { get; init; } = new decimal[12];

        public decimal TotaleQuantita => QuantitaMensile.Sum();
        public decimal TotaleEuro => EuroMensile.Sum();
    }
}
