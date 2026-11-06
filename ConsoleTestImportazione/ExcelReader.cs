using ClosedXML.Excel;
using ConsoleTestImportazione.Models;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ConsoleTestImportazione
{
    public static class ExcelReader
    {
        public static List<RigaExcelCobral> Read(string path)
        {
            var lista = new List<RigaExcelCobral>();

            // Apertura del file Excel
            using var workbook = new XLWorkbook(path);

            if (!workbook.TryGetWorksheet("Calkos", out var ws))
                throw new Exception("Foglio 'Calkos' non trovato.");

            int firstRow = 6; // Partiamo dalla riga 6 per saltare l'intestazione

            var errori = new List<string>();

            for (int row = firstRow; ; row++)
            {
                // SENTINELLA: se OrdineDDT è vuoto → fine file
                var ordine = GetSafeString(ws.Cell(row, 4), row, 4, errori);
                if (string.IsNullOrWhiteSpace(ordine))
                    break;

                // Creo l’oggetto PRIMA, poi assegno campo per campo (debug‑friendly)
                var r = new RigaExcelCobral();

                r.OrdineDDT = ordine;
                r.Cliente = GetSafeString(ws.Cell(row, 5), row, 5, errori);
                r.Kg = GetSafeDecimal(ws.Cell(row, 6), row, 6, errori);
                r.Materiale = GetSafeString(ws.Cell(row, 7), row, 7, errori);
                r.Prezzo = GetSafeDecimal(ws.Cell(row, 8), row, 8, errori);

                r.Al = GetSafeDateTime(ws.Cell(row, 9), row, 9, errori);                     // CAMPO AGGIUNTO

                //r.Spessore = GetSafeDecimal(ws.Cell(row, 10), row, 10, errori);
                //r.Larghezza = GetSafeDecimal(ws.Cell(row, 11), row, 11, errori);

                r.Spessore = GetSafeString(ws.Cell(row, 10), row, 10, errori);
                r.Larghezza = GetSafeString(ws.Cell(row, 11), row, 11, errori);


                r.Provvigione = GetSafeDecimal(ws.Cell(row, 12), row, 12, errori);

                r.AlluminioSpessore = GetSafeDecimal(ws.Cell(row, 13), row, 13, errori);
                r.OttoneSpessore = GetSafeDecimal(ws.Cell(row, 14), row, 14, errori);
                r.RameSpessore = GetSafeDecimal(ws.Cell(row, 15), row, 15, errori);
                r.AltrePercentuali = GetSafeDecimal(ws.Cell(row, 16), row, 16, errori);
                r.PrLavSpess = GetSafeDecimal(ws.Cell(row, 17), row, 17, errori);

                r.AlluminioLarghezza = GetSafeDecimal(ws.Cell(row, 18), row, 18, errori);
                r.OttoneLarghezza = GetSafeDecimal(ws.Cell(row, 19), row, 19, errori);
                r.RameLarghezza = GetSafeDecimal(ws.Cell(row, 20), row, 20, errori);
                r.BronzoLarghezza = GetSafeDecimal(ws.Cell(row, 21), row, 21, errori);
                r.PrLavLarg = GetSafeDecimal(ws.Cell(row, 22), row, 22, errori);

                r.ExtraPrezzoKg = GetSafeDecimal(ws.Cell(row, 23), row, 23, errori);
                r.ExtraPrezzoStagnato = GetSafeDecimal(ws.Cell(row, 24), row, 24, errori);

                r.PrLavTotale = GetSafeDecimal(ws.Cell(row, 25), row, 25, errori);           // CAMPO AGGIUNTO
                r.Commissioni = GetSafeDecimal(ws.Cell(row, 26), row, 26, errori);           // CAMPO AGGIUNTO
                r.PrezzoVendita = GetSafeDecimal(ws.Cell(row, 27), row, 27, errori);         // CAMPO AGGIUNTO
                r.Differenza = GetSafeDecimal(ws.Cell(row, 28), row, 28, errori);            // CAMPO AGGIUNTO

                r.DataConsegnaIpotetica = GetSafeDateTime(ws.Cell(row, 29), row, 29, errori);
                r.Agente = GetSafeString(ws.Cell(row, 30), row, 30, errori);
                r.Scadenza = GetSafeDateTime(ws.Cell(row, 31), row, 31, errori);

                r.Fatturare = GetSafeString(ws.Cell(row, 32), row, 32, errori);              // CAMPO AGGIUNTO

                lista.Add(r);
            }

            return lista;
        }


        //--- METODI HELPER PER EVITARE CRASH ---
        private static decimal GetSafeDecimal(IXLCell cell, int row, int col, List<string> errori)
        {
            if (cell.TryGetValue(out decimal value))
                return value;

            errori.Add($"Errore conversione DECIMALE alla riga {row}, colonna {col}, valore='{cell.GetString()}'");
            return 0m;
        }


        private static DateTime? GetSafeDateTime(IXLCell cell, int row, int col, List<string> errori)
        {
            if (cell.TryGetValue(out DateTime value))
                return value;

            if (!string.IsNullOrWhiteSpace(cell.GetString()))
                errori.Add($"Errore conversione DATA alla riga {row}, colonna {col}, valore='{cell.GetString()}'");

            return null;
        }

        private static string? GetSafeString(IXLCell cell, int row, int col, List<string> errori)
        {
            if (cell.TryGetValue(out string value))
                return value;

            if (!string.IsNullOrWhiteSpace(cell.GetString()))
                errori.Add($"Errore conversione STRINGA alla riga {row}, colonna {col}, valore='{cell.GetString()}'");

            return null;
        }


        //private static string GetSafeString(IXLCell cell)
        //{
        //    return cell.GetValue<string>()?.Trim() ?? string.Empty;
        //}





    }
}