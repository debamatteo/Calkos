using System.Text;

namespace Calkos.web.Services.Export.Grafici
{
    /// <summary>
    /// Costruzione di riferimenti Excel in formato A1 (colonne, celle, unioni).
    /// Codice puro, senza dipendenze: usato sia per le formule sia per i grafici.
    /// Righe e colonne sono 1-based, come in ClosedXML.
    /// </summary>
    ///

    //ExcelRiferimenti.cs:Classe di utilità pura per la manipolazione di stringhe
    //di riferimenti Excel in notazione A1.
    //Converte indici numerici in lettere
    //(es. 1 -> A, 27 -> AA).   Genera riferimenti assoluti con nome foglio
    //(es. 'Grafici'!$C$5) e unioni di celle non contigue per i grafici
    //(es. ('Grafici'!$C$3,'Grafici'!$E$3)).

    public static class ExcelRiferimenti
    {
        /// <summary>1 -> "A", 26 -> "Z", 27 -> "AA", 703 -> "AAA".</summary>
        public static string LetteraColonna(int colonna)
        {
            if (colonna < 1) throw new ArgumentOutOfRangeException(nameof(colonna));

            var sb = new StringBuilder();
            while (colonna > 0)
            {
                int resto = (colonna - 1) % 26;
                sb.Insert(0, (char)('A' + resto));
                colonna = (colonna - 1) / 26;
            }
            return sb.ToString();
        }

        /// <summary>Riferimento relativo, es. "C5" (per le formule sulla stessa riga).</summary>
        public static string Cella(int riga, int colonna) => $"{LetteraColonna(colonna)}{riga}";

        // ==== MODIFICA 29/09/2026 - INIZIO ====
        /// <summary>Riferimento assoluto senza foglio, es. "$B$5" (criterio fisso nelle formule SUMIF).</summary>
        public static string CellaFissa(int riga, int colonna) => $"${LetteraColonna(colonna)}${riga}";

        /// <summary>Testo come costante stringa di formula: KG -> "KG" (virgolette interne raddoppiate).</summary>
        public static string TestoFormula(string testo) => $"\"{(testo ?? "").Replace("\"", "\"\"")}\"";
        // ==== MODIFICA 29/09/2026 - FINE ====

        /// <summary>Riferimento assoluto con foglio, es. 'Grafici'!$C$5 (per le serie dei grafici).</summary>
        public static string CellaAssoluta(string foglio, int riga, int colonna) =>
            $"'{foglio.Replace("'", "''")}'!${LetteraColonna(colonna)}${riga}";

        /// <summary>
        /// Unione di celle non contigue, es. ('Grafici'!$C$5,'Grafici'!$E$5).
        /// Con una sola cella restituisce il riferimento semplice.
        /// È la stessa sintassi usata dal foglio "Grafici" del file Excel del cliente.
        /// </summary>
        public static string Unione(IEnumerable<string> riferimenti)
        {
            var lista = riferimenti.ToList();
            if (lista.Count == 0) throw new ArgumentException("Nessun riferimento.", nameof(riferimenti));
            return lista.Count == 1 ? lista[0] : $"({string.Join(",", lista)})";
        }
    }
}
