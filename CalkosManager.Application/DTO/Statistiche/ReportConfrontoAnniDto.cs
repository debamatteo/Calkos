namespace CalkosManager.Application.DTO.Statistiche
{
    /// <summary>
    /// Report 2 - Confronto dei totali annuali di tutti i mandatari in un intervallo di anni.
    /// Gli array per anno sono allineati a <see cref="Anni"/> (stesso indice).
    /// </summary>
    public sealed class ReportConfrontoAnniDto
    {
        public int AnnoDa { get; init; }
        public int AnnoA { get; init; }

        /// <summary>Anni in ordine crescente, da AnnoDa ad AnnoA inclusi.</summary>
        public IReadOnlyList<int> Anni { get; init; } = Array.Empty<int>();

        public IReadOnlyList<RigaConfrontoAnniDto> Righe { get; init; } = Array.Empty<RigaConfrontoAnniDto>();

        // ==== MODIFICA 29/09/2026 - INIZIO ====
        // Sostituisce il vecchio flag UnitaMisuraOmogenea: le quantità si totalizzano
        // SEPARATAMENTE per ogni unità di misura (una riga TOTALE KG, una TOTALE PZ, ...).

        /// <summary>Codici U.M. da totalizzare (es. KG, PZ), in ordine alfabetico.</summary>
        public IReadOnlyList<string> UnitaMisuraTotali { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Mandatari con quantità ma U.M. mancante o "MISTO": le loro quantità
        /// non entrano in nessun totale per unità (lo segnala una nota nel file).
        /// </summary>
        public IReadOnlyList<string> MandatariQuantitaNonTotalizzate { get; init; } = Array.Empty<string>();
        // ==== MODIFICA 29/09/2026 - FINE ====
    }

    public sealed class RigaConfrontoAnniDto
    {
        public int IdMandatario { get; init; }
        public string NomeMandatario { get; init; } = "";
        public string UnitaMisura { get; init; } = "";

        /// <summary>Totale quantità per anno, allineato a ReportConfrontoAnniDto.Anni.</summary>
        public decimal[] QuantitaPerAnno { get; init; } = Array.Empty<decimal>();

        /// <summary>Totale euro provvigione per anno, allineato a ReportConfrontoAnniDto.Anni.</summary>
        public decimal[] EuroPerAnno { get; init; } = Array.Empty<decimal>();

        /// <summary>False se il mandatario non ha quantità nel periodo: il grafico quantità si salta.</summary>
        public bool HaQuantita => QuantitaPerAnno.Any(q => q != 0);

        /// <summary>False se il mandatario non ha provvigioni nel periodo.</summary>
        public bool HaEuro => EuroPerAnno.Any(e => e != 0);
    }
}
