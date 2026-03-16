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
        public static List<RigaExcelCobral> LeggiExcel(IFormFile fileExcel)
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

            int firstRow = 6; // Come nella console: si parte dalla riga 6

            for (int row = firstRow; ; row++)
            {
                // SENTINELLA: se OrdineDDT (colonna 4) è vuoto → fine file
                var ordine = GetSafeString(ws.Cell(row, 4), row, 4, errori);
                if (string.IsNullOrWhiteSpace(ordine))
                    break;

                var r = new RigaExcelCobral
                {
                    OrdineDDT = ordine,
                    Cliente = GetSafeString(ws.Cell(row, 5), row, 5, errori),
                    Kg = GetSafeDecimal(ws.Cell(row, 6), row, 6, errori),
                    Materiale = GetSafeString(ws.Cell(row, 7), row, 7, errori),
                    Prezzo = GetSafeDecimal(ws.Cell(row, 8), row, 8, errori),
                    Al = GetSafeDateTime(ws.Cell(row, 9), row, 9, errori),
                    Spessore = GetSafeDecimal(ws.Cell(row, 10), row, 10, errori),
                    Larghezza = GetSafeDecimal(ws.Cell(row, 11), row, 11, errori),
                    Provvigione = GetSafeDecimal(ws.Cell(row, 12), row, 12, errori),
                    AlluminioSpessore = GetSafeDecimal(ws.Cell(row, 13), row, 13, errori),
                    OttoneSpessore = GetSafeDecimal(ws.Cell(row, 14), row, 14, errori),
                    RameSpessore = GetSafeDecimal(ws.Cell(row, 15), row, 15, errori),
                    AltrePercentuali = GetSafeDecimal(ws.Cell(row, 16), row, 16, errori),
                    PrLavSpess = GetSafeDecimal(ws.Cell(row, 17), row, 17, errori),
                    AlluminioLarghezza = GetSafeDecimal(ws.Cell(row, 18), row, 18, errori),
                    OttoneLarghezza = GetSafeDecimal(ws.Cell(row, 19), row, 19, errori),
                    RameLarghezza = GetSafeDecimal(ws.Cell(row, 20), row, 20, errori),
                    Bronzo = GetSafeDecimal(ws.Cell(row, 21), row, 21, errori),
                    PrLavLarg = GetSafeDecimal(ws.Cell(row, 22), row, 22, errori),
                    ExtraPrezzoKg = GetSafeDecimal(ws.Cell(row, 23), row, 23, errori),
                    ExtraPrezzoStagnato = GetSafeDecimal(ws.Cell(row, 24), row, 24, errori),
                    PrLavTotale = GetSafeDecimal(ws.Cell(row, 25), row, 25, errori),
                    Commissioni = GetSafeDecimal(ws.Cell(row, 26), row, 26, errori),
                    PrezzoVendita = GetSafeDecimal(ws.Cell(row, 27), row, 27, errori),
                    Differenza = GetSafeDecimal(ws.Cell(row, 28), row, 28, errori),
                    DataConsegnaIpotetica = GetSafeDateTime(ws.Cell(row, 29), row, 29, errori),
                    Agente = GetSafeString(ws.Cell(row, 30), row, 30, errori),
                    Scadenza = GetSafeDateTime(ws.Cell(row, 31), row, 31, errori),
                    Fatturare = GetSafeString(ws.Cell(row, 32), row, 32, errori)
                };

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
    }
}
