using CalkosManager.Application.DTO.Statistiche;

namespace Calkos.web.Services.Export.Grafici
{
    /// <summary>
    /// Posizione della tabella "anni" nel foglio (1-based, come ClosedXML).
    /// Ogni anno occupa 2 colonne: Quantità, Euro.
    /// </summary>
    ///

    //    GraficiConfrontoAnniFactory.cs:
    //•	Calcola COSA e DOVE disegnare per ogni mandatario nel report comparativo.
    //•	Genera due grafici affiancati per mandatario(uno per la Quantità ed uno per gli Euro Provvigione), alternando i colori da una palette definita.
    //•	Genera in fondo il grafico globale col totale in Euro di tutti i mandatari.
    //•	29/09/2026: sotto il totale Euro, un grafico per ogni unità di misura (TOTALE KG, TOTALE PEZZI, ...).
    //•	I riferimenti puntano direttamente alle celle: se l'utente cambia un valore su Excel, il grafico si aggiorna autonomamente.

    public sealed record LayoutTabellaAnni(
        string Foglio,
        int RigaIntestazioneAnni,  // riga con l'anno (celle unite su 2 colonne)
        int PrimaRigaDati,         // prima riga mandatario
        int PrimaColonnaValori)    // colonna "Quantità" del primo anno
    {
        public int ColonnaQuantita(int indiceAnno) => PrimaColonnaValori + 2 * indiceAnno;
        public int ColonnaEuro(int indiceAnno) => PrimaColonnaValori + 2 * indiceAnno + 1;
        public int ColonnaAnno(int indiceAnno) => ColonnaQuantita(indiceAnno); // l'anno sta sulla cella unita
    }

    /// <summary>
    /// Calcola COSA disegnare e DOVE per il Report 2 (nessuna dipendenza da ClosedXML/SDK):
    /// per ogni mandatario con movimenti un grafico Quantità (se ha quantità) e uno Euro, affiancati,
    /// poi il grafico del totale Euro e un grafico per ogni unità di misura (totali separati).
    /// Le serie puntano alle celle della tabella, quindi se l'utente modifica un valore il grafico si aggiorna.
    /// </summary>
    public static class GraficiConfrontoAnniFactory
    {
        public const int AltezzaGraficoRighe = 16;
        public const int LarghezzaGraficoColonne = 6;
        private const string FormatoQuantita = "#,##0";
        private const string FormatoEuro = "#,##0 \"€\"";
        private const string ColoreTotale = "375623";

        private static readonly string[] Palette =
            { "4472C4", "C55A11", "70AD47", "7F6000", "5B9BD5", "7030A0", "264478", "9E480E", "636363" };

        // ==== MODIFICA 29/09/2026 - INIZIO ====
        private static readonly string[] PaletteTotaliUnita = { "1F4E79", "843C0C", "3A3838", "7F6000" };
        // ==== MODIFICA 29/09/2026 - FINE ====

        /// <param name="totali">Posizioni del blocco totali (stesse usate dal builder per scriverlo).</param>
        public static IReadOnlyList<GraficoLineaSpec> Crea(ReportConfrontoAnniDto report, LayoutTabellaAnni layout, LayoutTotali totali)
        {
            var grafici = new List<GraficoLineaSpec>();
            int nAnni = report.Anni.Count;

            string categorie = ExcelRiferimenti.Unione(Enumerable.Range(0, nAnni)
                .Select(j => ExcelRiferimenti.CellaAssoluta(layout.Foglio, layout.RigaIntestazioneAnni, layout.ColonnaAnno(j))));

            string Serie(int riga, Func<int, int> colonna) => ExcelRiferimenti.Unione(Enumerable.Range(0, nAnni)
                .Select(j => ExcelRiferimenti.CellaAssoluta(layout.Foglio, riga, colonna(j))));

            // Ancore 0-based: si parte due righe sotto l'ULTIMA riga del blocco totali
            int rigaAncora = totali.UltimaRiga + 2;
            int colSinistraDa = 0, colSinistraA = LarghezzaGraficoColonne;
            int colDestraDa = LarghezzaGraficoColonne + 1, colDestraA = 2 * LarghezzaGraficoColonne + 1;

            for (int k = 0; k < report.Righe.Count; k++)
            {
                var riga = report.Righe[k];
                int rigaExcel = layout.PrimaRigaDati + k;

                // Mandatario senza alcun movimento nel periodo: resta in tabella (a zero) ma senza grafici
                if (!riga.HaQuantita && !riga.HaEuro) continue;

                string colore = Palette[k % Palette.Length];
                string nome = riga.NomeMandatario.ToUpperInvariant();

                if (riga.HaQuantita)
                {
                    string um = string.IsNullOrWhiteSpace(riga.UnitaMisura) ? "" : $" ({riga.UnitaMisura})";
                    grafici.Add(new GraficoLineaSpec(
                        $"{nome} - QUANTITÀ{um}", "Quantità", categorie, Serie(rigaExcel, layout.ColonnaQuantita),
                        colore, FormatoQuantita,
                        colSinistraDa, rigaAncora, colSinistraA, rigaAncora + AltezzaGraficoRighe));
                }

                grafici.Add(new GraficoLineaSpec(
                    $"{nome} - EURO PROVVIGIONE", "Euro", categorie, Serie(rigaExcel, layout.ColonnaEuro),
                    colore, FormatoEuro,
                    colDestraDa, rigaAncora, colDestraA, rigaAncora + AltezzaGraficoRighe));

                rigaAncora += AltezzaGraficoRighe + 1;
            }

            // Totale Euro di tutti i mandatari (tutte le U.M. insieme): larghezza piena
            grafici.Add(new GraficoLineaSpec(
                "TOTALE EURO PROVVIGIONE - TUTTI I MANDATARI", "Euro", categorie, Serie(totali.RigaEuro, layout.ColonnaEuro),
                ColoreTotale, FormatoEuro,
                colSinistraDa, rigaAncora, colDestraA, rigaAncora + AltezzaGraficoRighe));
            rigaAncora += AltezzaGraficoRighe + 1;

            // ==== MODIFICA 29/09/2026 - INIZIO ====
            // Un grafico per ogni unità di misura (TOTALE KG, TOTALE PEZZI, ...), a coppie affiancate.
            // Le quantità NON si mescolano: ogni serie punta alla propria riga "TOTALE <U.M.>".
            for (int i = 0; i < totali.RighePerUnita.Count; i++)
            {
                var rigaUnita = totali.RighePerUnita[i];
                bool sinistra = i % 2 == 0;
                string etichetta = EtichetteUnitaMisura.Estesa(rigaUnita.CodiceUnita);

                grafici.Add(new GraficoLineaSpec(
                    $"TOTALE {etichetta} - TUTTI I MANDATARI", $"Totale {etichetta}", categorie,
                    Serie(rigaUnita.Riga, layout.ColonnaQuantita),
                    PaletteTotaliUnita[i % PaletteTotaliUnita.Length], FormatoQuantita,
                    sinistra ? colSinistraDa : colDestraDa, rigaAncora,
                    sinistra ? colSinistraA : colDestraA, rigaAncora + AltezzaGraficoRighe));

                if (!sinistra) rigaAncora += AltezzaGraficoRighe + 1; // coppia completata: nuova fila
            }
            // ==== MODIFICA 29/09/2026 - FINE ====

            return grafici;
        }
    }
}
