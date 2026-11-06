using Calkos.web.Services.Export.Grafici;
using CalkosManager.Application.DTO.Statistiche;
using ClosedXML.Excel;

namespace Calkos.web.Services.Export
{
    /// <summary>
    /// Export Excel delle Statistiche Globali (23/09/2026).
    /// Riceve DTO già calcolati dal service: qui NON si accede al database
    /// (a differenza dei metodi GeneraExcelRiepilogo* di ExcelExportService).
    /// Tabelle e stili con ClosedXML; grafici aggiunti alla fine con OpenXmlChartHelper.
    /// </summary>
    public class StatisticheGlobaliExcelBuilder
    {
        public const string NomeFoglioGrafici = "Grafici";

        private static readonly string[] Mesi =
        {
            "GENNAIO", "FEBBRAIO", "MARZO", "APRILE", "MAGGIO", "GIUGNO",
            "LUGLIO", "AGOSTO", "SETTEMBRE", "OTTOBRE", "NOVEMBRE", "DICEMBRE"
        };

        // Layout comune ai due report (righe/colonne 1-based)
        private const int RigaTitolo = 1;
        private const int RigaIntestazione = 3;      // mese / anno (celle unite su 2 colonne)
        private const int RigaSottoIntestazione = 4; // Quantità | Euro
        private const int PrimaRigaDati = 5;
        private const int ColonnaMandatario = 1;
        private const int ColonnaUnitaMisura = 2;
        private const int PrimaColonnaValori = 3;

        private const string FormatoQuantita = "#,##0.00";
        private const string FormatoEuro = "#,##0.00\" €\"";   // stesso formato di ExcelExportService

        private static readonly XLColor ColoreIntestazione = XLColor.LightGray;
        private static readonly XLColor ColoreTotaleAnno = XLColor.LightSteelBlue;
        private static readonly XLColor ColoreRigaTotale = XLColor.FromHtml("#92D050");
        // ==== MODIFICA 29/09/2026 - INIZIO ====
        private static readonly XLColor ColoreRigaTotaleUnita = XLColor.FromHtml("#C6E0B4"); // verde chiaro: stesso blocco
        private static readonly XLColor ColoreNonApplicabile = XLColor.FromHtml("#D9D9D9");  // grigio: cella non pertinente
        // ==== MODIFICA 29/09/2026 - FINE ====

        // =====================================================================
        //  REPORT 1 - FATTURATO MANDATARI, SINGOLO ANNO (struttura foglio "2026")
        // =====================================================================
        public byte[] CreaReportFatturatoAnnuale(ReportFatturatoAnnualeDto report)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(report.Anno.ToString());

            int colonnaTotaleAnno = PrimaColonnaValori + 2 * Mesi.Length;   // dopo i 12 mesi
            int ultimaColonna = colonnaTotaleAnno + 1;

            ScriviTitolo(ws, $"FATTURATO MANDATARI - ANNO {report.Anno} (solo ordini fatturati)");
            ScriviIntestazioniFisse(ws);
            for (int m = 0; m < Mesi.Length; m++)
                ScriviIntestazionePeriodo(ws, PrimaColonnaValori + 2 * m, Mesi[m], ColoreIntestazione);
            ScriviIntestazionePeriodo(ws, colonnaTotaleAnno, "TOTALE ANNO", ColoreTotaleAnno);

            // --- Righe mandatari ---
            int riga = PrimaRigaDati;
            foreach (var r in report.Righe)
            {
                ScriviNomeMandatario(ws, riga, r.NomeMandatario, r.UnitaMisura);

                for (int m = 0; m < Mesi.Length; m++)
                    ScriviCoppiaValori(ws, riga, PrimaColonnaValori + 2 * m, r.QuantitaMensile[m], r.EuroMensile[m]);

                // Totale anno come FORMULA (somma delle 12 colonne del mese), come nel file del cliente
                var colonneQuantita = Enumerable.Range(0, Mesi.Length).Select(m => PrimaColonnaValori + 2 * m);
                ScriviFormulaSomma(ws, riga, colonnaTotaleAnno, colonneQuantita, FormatoQuantita, grassetto: true);
                ScriviFormulaSomma(ws, riga, colonnaTotaleAnno + 1, colonneQuantita.Select(c => c + 1), FormatoEuro, grassetto: true);
                riga++;
            }

            // ==== MODIFICA 29/09/2026 - INIZIO ====
            var totali = LayoutTotali.Calcola(riga, report.UnitaMisuraTotali, report.MandatariQuantitaNonTotalizzate.Count > 0);
            ScriviBloccoTotali(ws, totali, ultimaRigaDati: riga - 1, numeroCoppie: Mesi.Length + 1,
                               report.MandatariQuantitaNonTotalizzate);
            // ==== MODIFICA 29/09/2026 - FINE ====

            Rifinisci(ws, ultimaColonna);
            return Salva(wb);
        }

        // =====================================================================
        //  REPORT 2 - CONFRONTO TOTALI ANNUALI + GRAFICI (struttura foglio "Grafici")
        // =====================================================================
        public byte[] CreaReportConfrontoAnni(ReportConfrontoAnniDto report)
        {
            byte[] file;
            LayoutTotali totali;   // MODIFICA 29/09/2026: sostituisce int rigaTotale

            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add(NomeFoglioGrafici);
                int nAnni = report.Anni.Count;

                ScriviTitolo(ws, $"CONFRONTO FATTURATO MANDATARI {report.AnnoDa} - {report.AnnoA} (solo ordini fatturati)");
                ScriviIntestazioniFisse(ws);
                for (int j = 0; j < nAnni; j++)
                {
                    // L'anno è scritto come NUMERO: il grafico lo usa come etichetta dell'asse X
                    ScriviIntestazionePeriodo(ws, PrimaColonnaValori + 2 * j, report.Anni[j], ColoreIntestazione);
                }

                int riga = PrimaRigaDati;
                foreach (var r in report.Righe)
                {
                    ScriviNomeMandatario(ws, riga, r.NomeMandatario, r.UnitaMisura);
                    for (int j = 0; j < nAnni; j++)
                        ScriviCoppiaValori(ws, riga, PrimaColonnaValori + 2 * j, r.QuantitaPerAnno[j], r.EuroPerAnno[j]);
                    riga++;
                }

                // ==== MODIFICA 29/09/2026 - INIZIO ====
                totali = LayoutTotali.Calcola(riga, report.UnitaMisuraTotali, report.MandatariQuantitaNonTotalizzate.Count > 0);
                ScriviBloccoTotali(ws, totali, ultimaRigaDati: riga - 1, numeroCoppie: nAnni,
                                   report.MandatariQuantitaNonTotalizzate);
                // ==== MODIFICA 29/09/2026 - FINE ====

                Rifinisci(ws, PrimaColonnaValori + 2 * nAnni - 1);
                file = Salva(wb);
            }

            // Grafici: calcolo posizioni/riferimenti (codice puro) + iniezione con Open XML SDK
            var layout = new LayoutTabellaAnni(NomeFoglioGrafici, RigaIntestazione, PrimaRigaDati, PrimaColonnaValori);
            var grafici = GraficiConfrontoAnniFactory.Crea(report, layout, totali);

            return OpenXmlChartHelper.AggiungiGrafici(file, NomeFoglioGrafici, grafici);
        }

        // =====================================================================
        //  METODI DI SCRITTURA CONDIVISI
        // =====================================================================
        private static void ScriviTitolo(IXLWorksheet ws, string titolo)
        {
            ws.Cell(RigaTitolo, 1).Value = titolo;
            ws.Cell(RigaTitolo, 1).Style.Font.SetBold().Font.SetFontSize(14);
        }

        private static void ScriviIntestazioniFisse(IXLWorksheet ws)
        {
            StileIntestazione(ws.Range(RigaIntestazione, ColonnaMandatario, RigaSottoIntestazione, ColonnaMandatario).Merge(), "Mandatario", ColoreIntestazione);
            StileIntestazione(ws.Range(RigaIntestazione, ColonnaUnitaMisura, RigaSottoIntestazione, ColonnaUnitaMisura).Merge(), "U.M.", ColoreIntestazione);
        }

        /// <summary>Intestazione di un periodo (mese o anno) su 2 colonne: Quantità | Euro.</summary>
        private static void ScriviIntestazionePeriodo(IXLWorksheet ws, int colonna, XLCellValue etichetta, XLColor colore)
        {
            StileIntestazione(ws.Range(RigaIntestazione, colonna, RigaIntestazione, colonna + 1).Merge(), etichetta, colore);
            StileIntestazione(ws.Range(RigaSottoIntestazione, colonna, RigaSottoIntestazione, colonna), "Quantità", colore);
            StileIntestazione(ws.Range(RigaSottoIntestazione, colonna + 1, RigaSottoIntestazione, colonna + 1), "Euro", colore);
        }

        private static void StileIntestazione(IXLRange range, XLCellValue valore, XLColor colore)
        {
            range.FirstCell().Value = valore;
            range.Style
                .Font.SetBold()
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Fill.SetBackgroundColor(colore)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        }

        private static void ScriviNomeMandatario(IXLWorksheet ws, int riga, string nome, string unitaMisura)
        {
            ws.Cell(riga, ColonnaMandatario).Value = nome;
            ws.Cell(riga, ColonnaUnitaMisura).Value = unitaMisura;
            ws.Cell(riga, ColonnaUnitaMisura).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Range(riga, ColonnaMandatario, riga, ColonnaUnitaMisura).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        }

        private static void ScriviCoppiaValori(IXLWorksheet ws, int riga, int colonna, decimal quantita, decimal euro)
        {
            var cQuantita = ws.Cell(riga, colonna);
            cQuantita.Value = quantita;
            cQuantita.Style.NumberFormat.Format = FormatoQuantita;

            var cEuro = ws.Cell(riga, colonna + 1);
            cEuro.Value = euro;
            cEuro.Style.NumberFormat.Format = FormatoEuro;

            ws.Range(riga, colonna, riga, colonna + 1).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        }

        /// <summary>Formula =C5+E5+G5... sulla stessa riga (celle non contigue).</summary>
        private static void ScriviFormulaSomma(IXLWorksheet ws, int riga, int colonna, IEnumerable<int> colonneDaSommare,
                                              string formato, bool grassetto)
        {
            var cella = ws.Cell(riga, colonna);
            cella.FormulaA1 = string.Join("+", colonneDaSommare.Select(c => ExcelRiferimenti.Cella(riga, c)));
            cella.Style.NumberFormat.Format = formato;
            if (grassetto) cella.Style.Font.SetBold();
            cella.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        }

        // ==== MODIFICA 29/09/2026 - INIZIO ====
        // Sostituisce ScriviRigaTotale. Richiesta cliente: gli Euro si sommano TUTTI insieme,
        // le quantità restano separate per unità di misura (KG e PZ non si mescolano).
        /// <summary>
        /// Blocco totali sotto i mandatari (posizioni da <see cref="LayoutTotali"/>):
        /// - riga verde "TOTALE EURO": somma degli Euro di tutti i mandatari, quantità in grigio (non pertinente);
        /// - una riga "TOTALE &lt;U.M.&gt;" per unità: SUMIF delle sole quantità con quella U.M., Euro in grigio;
        /// - eventuale nota sui mandatari la cui quantità non entra in alcun totale (U.M. mancante o MISTO).
        /// </summary>
        private static void ScriviBloccoTotali(IXLWorksheet ws, LayoutTotali totali, int ultimaRigaDati,
                                               int numeroCoppie, IReadOnlyList<string> mandatariNonTotalizzati)
        {
            bool ciSonoDati = ultimaRigaDati >= PrimaRigaDati;

            // --- 1) TOTALE EURO (tutte le unità di misura insieme) ---
            ScriviEtichettaTotale(ws, totali.RigaEuro, "TOTALE EURO", "", ColoreRigaTotale);
            for (int p = 0; p < numeroCoppie; p++)
            {
                int colQuantita = PrimaColonnaValori + 2 * p;
                StileCellaTotale(ws.Cell(totali.RigaEuro, colQuantita), ColoreNonApplicabile);

                var cEuro = ws.Cell(totali.RigaEuro, colQuantita + 1);
                if (ciSonoDati)
                {
                    cEuro.FormulaA1 = FormuleTotali.Somma(PrimaRigaDati, ultimaRigaDati, colQuantita + 1);
                    cEuro.Style.NumberFormat.Format = FormatoEuro;
                }
                StileCellaTotale(cEuro, ColoreRigaTotale);
            }

            // --- 2) TOTALE per unità di misura (KG, PZ, ...) ---
            foreach (var rigaUnita in totali.RighePerUnita)
            {
                ScriviEtichettaTotale(ws, rigaUnita.Riga,
                    $"TOTALE {EtichetteUnitaMisura.Estesa(rigaUnita.CodiceUnita)}", rigaUnita.CodiceUnita, ColoreRigaTotaleUnita);

                for (int p = 0; p < numeroCoppie; p++)
                {
                    int colQuantita = PrimaColonnaValori + 2 * p;

                    var cQuantita = ws.Cell(rigaUnita.Riga, colQuantita);
                    if (ciSonoDati)
                    {
                        cQuantita.FormulaA1 = FormuleTotali.SommaPerUnita(
                            PrimaRigaDati, ultimaRigaDati, ColonnaUnitaMisura, colQuantita, rigaUnita.CodiceUnita);
                        cQuantita.Style.NumberFormat.Format = FormatoQuantita;
                    }
                    StileCellaTotale(cQuantita, ColoreRigaTotaleUnita);
                    StileCellaTotale(ws.Cell(rigaUnita.Riga, colQuantita + 1), ColoreNonApplicabile);
                }
            }

            // --- 3) Nota sulle quantità escluse dai totali per unità ---
            if (totali.RigaNota is int rigaNota)
            {
                var nota = ws.Cell(rigaNota, ColonnaMandatario);
                nota.Value = "Nota: quantità non incluse nei totali per unità (U.M. mancante o mista): "
                             + string.Join(", ", mandatariNonTotalizzati);
                nota.Style.Font.SetItalic().Font.SetFontColor(XLColor.DimGray);
            }
        }

        private static void ScriviEtichettaTotale(IXLWorksheet ws, int riga, string etichetta, string codiceUnita, XLColor colore)
        {
            ws.Cell(riga, ColonnaMandatario).Value = etichetta;
            ws.Cell(riga, ColonnaUnitaMisura).Value = codiceUnita;
            ws.Cell(riga, ColonnaUnitaMisura).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Range(riga, ColonnaMandatario, riga, ColonnaUnitaMisura).Style
                .Font.SetBold().Fill.SetBackgroundColor(colore).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        }

        private static void StileCellaTotale(IXLCell cella, XLColor colore)
        {
            cella.Style.Font.SetBold().Fill.SetBackgroundColor(colore).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        }
        // ==== MODIFICA 29/09/2026 - FINE ====

        private static void Rifinisci(IXLWorksheet ws, int ultimaColonna)
        {
            ws.Column(ColonnaMandatario).Width = 26;
            ws.Column(ColonnaUnitaMisura).Width = 7;
            for (int c = PrimaColonnaValori; c <= ultimaColonna; c++)
                ws.Column(c).Width = 15;

            // Blocca intestazioni e colonna mandatario durante lo scorrimento
            ws.SheetView.Freeze(RigaSottoIntestazione, ColonnaUnitaMisura);
        }

        private static byte[] Salva(XLWorkbook wb)
        {
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
