namespace Calkos.web.Services.Export.Grafici
{
    // ==== NUOVO FILE 29/09/2026 ====
    // LayoutTotali.cs: calcola (codice puro) le righe del BLOCCO TOTALI sotto i mandatari.
    // È condiviso da StatisticheGlobaliExcelBuilder (che scrive le righe) e da
    // GraficiConfrontoAnniFactory (che punta i grafici dei totali su quelle stesse righe):
    // un'unica fonte per le posizioni evita grafici che puntano alla riga sbagliata.
    //
    //   riga N     TOTALE EURO - TUTTI I MANDATARI   (verde: Euro di tutti, quantità vuote)
    //   riga N+1   TOTALE KG                         (solo quantità dei mandatari in KG)
    //   riga N+2   TOTALE PEZZI                      (solo quantità dei mandatari in PZ)
    //   riga N+3   Nota (solo se ci sono quantità non totalizzabili: U.M. mancante o MISTO)

    /// <summary>Riga (1-based) del totale quantità di una unità di misura.</summary>
    public sealed record RigaTotaleUnita(string CodiceUnita, int Riga);

    /// <summary>Posizioni (1-based) delle righe del blocco totali.</summary>
    public sealed record LayoutTotali(int RigaEuro, IReadOnlyList<RigaTotaleUnita> RighePerUnita, int? RigaNota)
    {
        /// <summary>Ultima riga occupata dal blocco (i grafici partono sotto).</summary>
        public int UltimaRiga => RigaNota ?? (RighePerUnita.Count > 0 ? RighePerUnita[^1].Riga : RigaEuro);

        /// <param name="primaRiga">Prima riga libera dopo l'ultimo mandatario.</param>
        /// <param name="unita">Codici U.M. da totalizzare, già ordinati.</param>
        /// <param name="conNota">True se esistono quantità escluse dai totali per unità.</param>
        public static LayoutTotali Calcola(int primaRiga, IReadOnlyList<string> unita, bool conNota)
        {
            var righeUnita = unita
                .Select((u, i) => new RigaTotaleUnita(u, primaRiga + 1 + i))
                .ToList();

            int? rigaNota = conNota ? primaRiga + 1 + righeUnita.Count : null;

            return new LayoutTotali(primaRiga, righeUnita, rigaNota);
        }
    }

    /// <summary>Formule delle righe totali (senza "=", come FormulaA1 di ClosedXML).</summary>
    public static class FormuleTotali
    {
        /// <summary>Totale di TUTTE le righe mandatario: SUM(D5:D14). Usata per gli Euro.</summary>
        public static string Somma(int primaRigaDati, int ultimaRigaDati, int colonna) =>
            $"SUM({ExcelRiferimenti.Cella(primaRigaDati, colonna)}:{ExcelRiferimenti.Cella(ultimaRigaDati, colonna)})";

        /// <summary>
        /// Totale delle sole righe con quella U.M.: SUMIF($B$5:$B$14,"KG",C5:C14).
        /// Il criterio legge la colonna U.M. del foglio: se l'utente corregge una U.M.
        /// o una quantità in Excel, il totale (e il suo grafico) si aggiorna.
        /// </summary>
        public static string SommaPerUnita(int primaRigaDati, int ultimaRigaDati, int colonnaUnitaMisura,
                                           int colonnaValori, string codiceUnita) =>
            $"SUMIF({ExcelRiferimenti.CellaFissa(primaRigaDati, colonnaUnitaMisura)}:{ExcelRiferimenti.CellaFissa(ultimaRigaDati, colonnaUnitaMisura)}," +
            $"{ExcelRiferimenti.TestoFormula(codiceUnita)}," +
            $"{ExcelRiferimenti.Cella(primaRigaDati, colonnaValori)}:{ExcelRiferimenti.Cella(ultimaRigaDati, colonnaValori)})";
    }
}
