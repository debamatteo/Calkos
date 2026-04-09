using ClosedXML.Excel;
using CalkosManager.Domain.Models.Importazione;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;

namespace Calkos.web.Helpers
{
    /// <summary>
    /// Lettura robusta del file Excel COBRAL tramite ClosedXML.
    /// Versione PRO: identica alla console, ma adattata a IFormFile.
    /// </summary>
    public static class ExcelHelper
    {
        

        public static List<RigaExcelCobral> LeggiExcel(IFormFile fileExcel, int firstRow)
        {
            var lista = new List<RigaExcelCobral>();
            var errori = new List<string>();

            using var stream = new MemoryStream();
            fileExcel.CopyTo(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);

            // Foglio "Calkos" come nella console
            if (!workbook.TryGetWorksheet("Calkos", out var ws))
                throw new Exception("Foglio 'Calkos' non trovato nel file Excel.");

            //int firstRow = 6; // Come nella console: si parte dalla riga 6
         

            for (int row = firstRow; ; row++)
            {
                // SENTINELLA: se OrdineDDT (colonna 4) è vuoto → fine file
                var ordine = GetSafeString(ws.Cell(row, 4), row, 4, errori);
                if (string.IsNullOrWhiteSpace(ordine))
                    break;
                var r = new RigaExcelCobral();
                //PrLavSpess
                //PrLavLarg
                // Colonna 4 – Sentinella
                r.OrdineDDT = ordine;

                // Colonna 5
                r.Cliente = GetSafeString(ws.Cell(row, 5), row, 5, errori);

                // Colonna 6
                r.Kg = GetSafeDecimal(ws.Cell(row, 6), row, 6, errori);

                // Colonna 7
                r.Materiale = GetSafeString(ws.Cell(row, 7), row, 7, errori);

                // Colonna 8
                r.Prezzo = GetSafeDecimal(ws.Cell(row, 8), row, 8, errori);

                // Colonna 9
                r.Al = GetSafeDateTime(ws.Cell(row, 9), row, 9, errori);

                // Colonna 10
                //r.Spessore = GetSafeDecimal(ws.Cell(row, 10), row, 10, errori);
                r.Spessore = GetSafeString(ws.Cell(row, 10), row, 10, errori);
                // Colonna 11
                //r.Larghezza = GetSafeDecimal(ws.Cell(row, 11), row, 11, errori);
                r.Larghezza = GetSafeString(ws.Cell(row, 11), row, 11, errori);
                // Colonna 12
                r.Provvigione = GetSafeDecimal(ws.Cell(row, 12), row, 12, errori);

                // Colonna 13–21
                r.AlluminioSpessore = GetSafeDecimal(ws.Cell(row, 13), row, 13, errori);
                r.OttoneSpessore = GetSafeDecimal(ws.Cell(row, 14), row, 14, errori);
                r.RameSpessore = GetSafeDecimal(ws.Cell(row, 15), row, 15, errori);
                r.AltrePercentuali = GetSafeDecimal(ws.Cell(row, 16), row, 16, errori);
                r.PrLavSpess = GetSafeDecimal(ws.Cell(row, 17), row, 17, errori);
                r.AlluminioLarghezza = GetSafeDecimal(ws.Cell(row, 18), row, 18, errori);
                r.OttoneLarghezza = GetSafeDecimal(ws.Cell(row, 19), row, 19, errori);
                r.RameLarghezza = GetSafeDecimal(ws.Cell(row, 20), row, 20, errori);
                r.BronzoLarghezza = GetSafeDecimal(ws.Cell(row, 21), row, 21, errori);

                // Colonna 22–28
                r.PrLavLarg = GetSafeDecimal(ws.Cell(row, 22), row, 22, errori);
                r.ExtraPrezzoKg = GetSafeDecimal(ws.Cell(row, 23), row, 23, errori);
                r.ExtraPrezzoStagnato = GetSafeDecimal(ws.Cell(row, 24), row, 24, errori);
                r.PrLavTotale = GetSafeDecimal(ws.Cell(row, 25), row, 25, errori);
                r.Commissioni = GetSafeDecimal(ws.Cell(row, 26), row, 26, errori);
                r.PrezzoVendita = GetSafeDecimal(ws.Cell(row, 27), row, 27, errori);
                r.Differenza = GetSafeDecimal(ws.Cell(row, 28), row, 28, errori);

                // Colonna 29–32
                r.DataConsegnaIpotetica = GetSafeDateTime(ws.Cell(row, 29), row, 29, errori);
                r.Agente = GetSafeString(ws.Cell(row, 30), row, 30, errori);
                r.Scadenza = GetSafeDateTime(ws.Cell(row, 31), row, 31, errori);
                r.Fatturare = GetSafeString(ws.Cell(row, 32), row, 32, errori);

                lista.Add(r);

            }

            return lista;
        }

        // -----------------------------
        // METODI ROBUSTI (identici alla console)
        // -----------------------------
        private static decimal? GetSafeDecimal(IXLCell cell, int row, int col, List<string> errori)
        {
            if (cell.TryGetValue(out decimal value))
                return value;

            if (!string.IsNullOrWhiteSpace(cell.GetString()))
                errori.Add($"Errore DECIMALE riga {row}, col {col}, valore='{cell.GetString()}'");

            return null;
        }

        private static DateTime? GetSafeDateTime(IXLCell cell, int row, int col, List<string> errori)
        {
            if (cell.TryGetValue(out DateTime value))
                return value;

            if (!string.IsNullOrWhiteSpace(cell.GetString()))
                errori.Add($"Errore DATA riga {row}, col {col}, valore='{cell.GetString()}'");

            return null;
        }

        private static string? GetSafeString(IXLCell cell, int row, int col, List<string> errori)
        {
            if (cell.TryGetValue(out string value))
                return value;

            if (!string.IsNullOrWhiteSpace(cell.GetString()))
                errori.Add($"Errore STRINGA riga {row}, col {col}, valore='{cell.GetString()}'");

            return null;
        }


        /// <summary>
        /// Imposta il titolo del report Excel:
        /// - Inserisce il testo nella riga 1
        /// - Unisce le colonne specificate
        /// - Applica stile (grassetto, font grande, centrato)
        /// - Imposta larghezza fissa (40) per evitare tagli del testo
        /// </summary>
        public static void SetTitolo(IXLWorksheet ws, string titolo, int colStart, int colEnd)
        {
            // Inserisce il titolo nella cella iniziale
            ws.Cell(1, colStart).Value = titolo;

            // Applica stile del titolo
            ws.Cell(1, colStart).Style
                .Font.SetBold()
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // Calcola quante colonne coprire visivamente
            int colonne = colEnd - colStart + 1;

            // Imposta una larghezza molto ampia sulla PRIMA colonna
            // così il titolo non viene mai tagliato
            ws.Column(colStart).Width = 40 * colonne;

            // NON facciamo merge → Excel non si rompe più
        }

        /// <summary>
        /// Imposta il titolo del report Excel:
        /// - Inserisce il testo nella riga 1
        /// - Unisce le colonne specificate
        /// - Applica stile (grassetto, font grande, centrato)
        /// - Imposta larghezza fissa (40) per evitare tagli del testo
        /// </summary>
        //public static void SetTitolo(IXLWorksheet ws, string titolo, int colStart, int colEnd)
        //{
        //    // Inserisce il titolo nella cella iniziale (es. A1)
        //    ws.Cell(1, colStart).Value = titolo;

        //    // Unisce le celle della prima riga da colStart a colEnd
        //    ws.Range(1, colStart, 1, colEnd).Merge()
        //        .Style.Font.SetBold()
        //        .Font.SetFontSize(16)
        //        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        //    // Imposta larghezza fissa e generosa per ogni colonna del titolo
        //    for (int c = colStart; c <= colEnd; c++)
        //        ws.Column(c).Width = 40;
        //}
        /// <summary>
        /// Imposta il titolo del report Excel:
        /// - Inserisce il testo nella riga 1
        /// - Unisce le colonne specificate
        /// - Applica stile (grassetto, font grande, centrato)
        /// - Imposta una larghezza colonne stabile e sufficiente
        /// </summary>
        /// /// <summary>
        //public static void SetTitolo(IXLWorksheet ws, string titolo, int colStart, int colEnd)
        //{
        //    // Inserisce il titolo nella cella iniziale (es. A1)
        //    ws.Cell(1, colStart).Value = titolo;

        //    // Unisce le celle della prima riga da colStart a colEnd
        //    ws.Range(1, colStart, 1, colEnd).Merge()
        //        .Style.Font.SetBold()
        //        .Font.SetFontSize(16)
        //        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        //    // Numero di colonne coinvolte
        //    int colonne = colEnd - colStart + 1;

        //    // Calcolo dinamico della larghezza totale necessaria
        //    // 1.2 = fattore più realistico per font non monospaziati
        //    double larghezzaStimata = titolo.Length * 1.2;

        //    // Imposta una larghezza minima per evitare titoli corti troppo stretti
        //    double larghezzaMinima = 40;

        //    // Sceglie la larghezza maggiore tra stimata e minima
        //    double larghezzaTotale = Math.Max(larghezzaStimata, larghezzaMinima);

        //    // Divide la larghezza totale sulle colonne mergiate
        //    double larghezzaPerColonna = larghezzaTotale / colonne;

        //    for (int c = colStart; c <= colEnd; c++)
        //        ws.Column(c).Width = larghezzaPerColonna;
        //}



    }
}
