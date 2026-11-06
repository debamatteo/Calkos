namespace Calkos.web.Services.Export.Grafici
{
    // ==== NUOVO FILE 29/09/2026 ====
    /// <summary>
    /// Nome "leggibile" di un codice U.M. per le etichette del report
    /// ("TOTALE PEZZI" invece di "TOTALE PZ", come richiesto dal cliente).
    /// Un codice non mappato viene mostrato così com'è, in maiuscolo:
    /// una nuova unità funziona subito, la voce qui serve solo a migliorarne il nome.
    /// </summary>
    public static class EtichetteUnitaMisura
    {
        private static readonly Dictionary<string, string> Estese = new(StringComparer.OrdinalIgnoreCase)
        {
            ["KG"] = "KG",
            ["PZ"] = "PEZZI"
        };

        public static string Estesa(string codice)
        {
            string c = (codice ?? "").Trim();
            return Estese.TryGetValue(c, out var estesa) ? estesa : c.ToUpperInvariant();
        }
    }
}
