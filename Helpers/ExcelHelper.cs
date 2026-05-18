using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Models.Importazione;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office.CustomUI;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Calkos.web.Helpers
{
    public static class ExcelHelper
    {
        // ============================================================
        // 1) METODO  COBRAL 
        // ============================================================
        public static List<RigaExcelCobral> LeggiExcelCobral(IFormFile fileExcel, int firstRow, 
            HashSet<string> valoriAmmessi)//14/04/2026
        {
            var lista = new List<RigaExcelCobral>();
            var errori = new List<string>();

            using var stream = new MemoryStream();
            fileExcel.CopyTo(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);

            if (!workbook.TryGetWorksheet("Calkos", out var ws))
                throw new Exception("Foglio 'Calkos' non trovato nel file Excel.");

            for (int row = firstRow; ; row++)
            {
                // 15/04/2026  colonna 2 e 3 ordine e cliente se sono vuote entrambe il ciclo si interrompe
                var ordine = ws.Cell(row, 2).GetFormattedString().Trim();
                var cliente = ws.Cell(row, 3).GetFormattedString().Trim();

                // 15/04/2026  : interrompi solo se entrambe vuote
                if (string.IsNullOrWhiteSpace(ordine) && string.IsNullOrWhiteSpace(cliente))
                    break;

                var r = new RigaExcelCobral();
                r.OrdineDDT = ordine;
                r.Cliente = cliente;// GetSafeString(ws.Cell(row, 3), row, 3, errori);
                r.Kg = GetSafeDecimal(ws.Cell(row, 4), row, 4, errori);
                r.Materiale = GetSafeString(ws.Cell(row, 5), row, 5, errori);
                r.Prezzo = GetSafeDecimal(ws.Cell(row, 6), row, 6, errori);
                r.Al = GetSafeDateTime(ws.Cell(row, 7), row, 7, errori);
                r.Spessore = GetSafeString(ws.Cell(row, 8), row, 8, errori);
                r.Larghezza = GetSafeString(ws.Cell(row, 9), row, 9, errori);
                r.Provvigione = GetSafeDecimal(ws.Cell(row, 10), row, 10, errori);
                r.AlluminioSpessore = GetSafeDecimal(ws.Cell(row, 11), row, 11, errori);
                r.OttoneSpessore = GetSafeDecimal(ws.Cell(row, 12), row, 12, errori);
                r.RameSpessore = GetSafeDecimal(ws.Cell(row, 13), row, 13, errori);
                r.AltrePercentuali = GetSafeDecimal(ws.Cell(row, 14), row, 14, errori);
                r.PrLavSpess = GetSafeDecimal(ws.Cell(row, 15), row, 15, errori);
                r.AlluminioLarghezza = GetSafeDecimal(ws.Cell(row, 16), row, 16, errori);
                r.OttoneLarghezza = GetSafeDecimal(ws.Cell(row, 17), row, 17, errori);
                r.RameLarghezza = GetSafeDecimal(ws.Cell(row, 18), row, 18, errori);
                r.BronzoLarghezza = GetSafeDecimal(ws.Cell(row, 19), row, 19, errori);
                r.PrLavLarg = GetSafeDecimal(ws.Cell(row, 20), row, 20, errori);
                r.ExtraPrezzoKg = GetSafeDecimal(ws.Cell(row, 21), row, 21, errori);
                r.ExtraPrezzoStagnato = GetSafeDecimal(ws.Cell(row, 22), row, 22, errori);
                r.PrLavTotale = GetSafeDecimal(ws.Cell(row, 23), row, 23, errori);
                r.Commissioni = GetSafeDecimal(ws.Cell(row, 24), row, 24, errori);
                r.PrezzoVendita = GetSafeDecimal(ws.Cell(row, 25), row, 25, errori);
                r.Differenza = GetSafeDecimal(ws.Cell(row, 26), row, 26, errori);
                r.DataConsegnaIpotetica = GetSafeDateTime(ws.Cell(row, 27), row, 27, errori);
                r.Agente = GetSafeString(ws.Cell(row, 28), row, 28, errori);
                r.Scadenza = GetSafeDateTime(ws.Cell(row, 29), row, 29, errori);
                r.Fatturare = GetSafeString(ws.Cell(row, 30), row, 30, errori);
                // 14/04/2026  IMPORTA SOLO LE RIGHE CON FATTURARE = OK
                if (!valoriAmmessi.Contains(r.Fatturare?.Trim() ?? ""))
                    continue;

                lista.Add(r);
            }

            //15/04/2026
            if (errori.Any())
                throw new Exception("Errori nel file Excel:\n" + string.Join("\n", errori));
            return lista;
        }

        // ============================================================
        // 2)  DEANGELI (11/04/2026)
        // ============================================================
        /// <summary>
        /// Lettura specifica per DE ANGELI. 
        /// Utilizza l'oggetto RigaExcelDeAngeli (o quello corrispondente).
        /// </summary>
        public static List<RigaExcelDeAngeli> LeggiExcelDeAngeli(
           IFormFile fileExcel,  int firstRow, HashSet<string> valoriAmmessi) 
        {
            var lista = new List<RigaExcelDeAngeli>();
            var errori = new List<string>();

            using var stream = new MemoryStream();
            fileExcel.CopyTo(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);

            if (!workbook.TryGetWorksheet("Commissioni De Angeli", out var ws))
                throw new Exception("Foglio 'Commissioni De Angeli' non trovato nel file Excel DeAngeli.");

            for (int row = firstRow; ; row++)
            {
                // 15/04/2026  colonna 2 e 3 ordine e cliente se sono vuote entrambe il ciclo si interrompe
                var ordine = ws.Cell(row, 2).GetFormattedString().Trim();
                var cliente = ws.Cell(row, 3).GetFormattedString().Trim();

                // 15/04/2026  : interrompi solo se entrambe vuote
                if (string.IsNullOrWhiteSpace(ordine) && string.IsNullOrWhiteSpace(cliente))
                    break;

                var r = new RigaExcelDeAngeli();

                r.IdCalcoloBase = GetSafeInt(ws.Cell(row, 1), row, 1, errori);//ID_BASE
                r.OrdineDDT = ordine;
                r.Cliente = cliente;// GetSafeString(ws.Cell(row, 3), row, 3, errori);
                //4 Kg  Kg
                r.Kg = GetSafeDecimal(ws.Cell(row, 4), row, 4, errori);
                r.QuantitaOrdine = GetSafeDecimal(ws.Cell(row, 5), row, 5, errori);//Ordine < 500 (non compilare se in C/L)
               
                r.CostoLavorazione = GetSafeDecimal(ws.Cell(row, 6), row, 6, errori);//Lav
                //7   Tipologia Materiale Materiale
                r.Materiale = GetSafeString(ws.Cell(row, 7), row, 7, errori);//7    8 Tipologia Materiale   Tipologia Materiale               
                r.TipologiaMateriale = GetSafeString(ws.Cell(row, 8), row, 8, errori);//7   8 Tipologia Materiale   Tipologia Materiale
                //9  Materiale Euro di riferimento   Prezzo
                r.Prezzo = GetSafeDecimal(ws.Cell(row, 9), row, 9, errori);//Materiale Euro di riferimento
                //10  Materiale + Lav con commissione   PrezzoConLavorazione
                r.PrezzoConLavorazione = GetSafeDecimal(ws.Cell(row, 10), row, 10, errori);//Materiale+Lav con commissione
                //11    Vendita PrezzoVendita
                r.PrezzoVendita = GetSafeDecimal(ws.Cell(row, 11), row, 11, errori);
                //12  Aliquota variabile  PercentualeProvvigioneVariabile
                r.PercentualeProvvigioneVariabile = GetSafeDecimal(ws.Cell(row, 12), row, 12, errori);//Aliquota variabile
                r.PercentualeProvvigioneVariabile = NormalizePercent(r.PercentualeProvvigioneVariabile);//05/05/2026	


                //13  Commissione con aliquota Variabile  ValoreProvvigioneVariabile
                r.ValoreProvvigioneVariabile = GetSafeDecimal(ws.Cell(row, 13), row, 13, errori);//Commissione con aliquota Variabile
                r.ValoreProvvigioneFissa1 = GetSafeDecimal(ws.Cell(row, 14), row, 14, errori);//1,5 % fissa
                r.ValoreProvvigioneFissa2 = GetSafeDecimal(ws.Cell(row, 15), row, 15, errori);//3 % Fissa 
                r.ValoreProvvigioneFissa3 = GetSafeDecimal(ws.Cell(row, 16), row, 16, errori);//8 % Fissa
                //17  Totale Fisso    TotaleFissoProvvigione
                r.TotaleFissoProvvigione = GetSafeDecimal(ws.Cell(row, 17), row, 17, errori);// Totale Fisso
                r.FlagIndicatoreMaggiorazione = GetSafeBool(ws.Cell(row, 18), row, 18, errori);

                //19  delta Differenza
                r.Differenza = GetSafeDecimal(ws.Cell(row, 19), row, 19, errori);//delta Differenza
                //20  Totale provv variabile TotaleVariabileProvvigione
                r.TotaleVariabileProvvigione = GetSafeDecimal(ws.Cell(row, 20), row, 20, errori);// Totale provv variabile 
                //20  Perc_Delta_Provvigione la devo leggere dalla Formula
                
                //r.Perc_Delta_Provvigione = EstraiPercentualeDaFormula(ws.Cell(row, 20).FormulaA1);
                var perc = EstraiPercentualeDaFormula(ws.Cell(row, 20).FormulaA1);//02/05/2026 11  Perc_Delta_Provvigione la devo leggere dalla Formula
                r.Perc_Delta_Provvigione = NormalizePercent(perc);//02/05/2026			 



                //21  TOTALE FATTURA  TotaleProvvigioneDaFatturare
                r.TotaleProvvigioneDaFatturare = GetSafeDecimal(ws.Cell(row, 21), row, 21, errori);//TOTALE FATTURA
                //22  Data Consegna   DataConsegnaIpotetica
                r.DataConsegnaIpotetica = GetSafeDateTime(ws.Cell(row, 22), row, 22, errori);
                //23  Fatturare   Fatturare
                r.Fatturare = GetSafeString(ws.Cell(row, 23), row, 23, errori);
                ////GetSafeString() funziona bene per testo puro ma NON funziona con celle che hanno un elenco a discesa(dropdown)
                ////ClosedXML in quel caso può restituire "" anche se vedi “OK” in Excel
                //r.Fatturare = ws.Cell(row, 23).GetFormattedString().Trim();

                //var cell = ws.Cell(row, 23);
                //var raw = cell.Value.ToString().Trim();
                //r.Fatturare = raw;



                // ============================================================
                // 14/04/2026 — IMPORTA SOLO LE RIGHE CON FLAG  AMMESSO
                // ============================================================
                if (!valoriAmmessi.Contains(r.Fatturare?.Trim() ?? ""))
                    continue;

                lista.Add(r);
            }
            //15/04/2026
            if (errori.Any())
                throw new Exception("Errori nel file Excel:\n" + string.Join("\n", errori));

            return lista;

        }

        // ============================================================
        // 3)  ElektraWire (11/04/2026)
        // ============================================================
        /// <summary>
        /// Lettura specifica per ElektraWire. 
        /// Utilizza l'oggetto RigaExcelElektraWire (o quello corrispondente).
        /// </summary>

        public static List<RigaExcelElektraWire> LeggiExcelElektraWire(
            IFormFile fileExcel, int firstRow, HashSet<string> valoriAmmessi)
        {
            var lista = new List<RigaExcelElektraWire>();
            var errori = new List<string>();

            using var stream = new MemoryStream();
            fileExcel.CopyTo(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);

            if (!workbook.TryGetWorksheet("calcolo comm.", out var ws))
                throw new Exception("Foglio 'calcolo comm.' non trovato nel file Excel.");

            for (int row = firstRow; ; row++)
            {
                // Interrompi solo se Ordine + Cliente sono entrambi vuoti
                var ordine = ws.Cell(row, 1).GetFormattedString().Trim();
                var cliente = ws.Cell(row, 2).GetFormattedString().Trim();

                if (string.IsNullOrWhiteSpace(ordine) && string.IsNullOrWhiteSpace(cliente))
                    break;

                var r = new RigaExcelElektraWire();

                r.OrdineDDT = ordine;
                r.Cliente = cliente;// GetSafeString(ws.Cell(row, 2), row, 2, errori);
                r.Misure = GetSafeString(ws.Cell(row, 3), row, 3, errori);

                r.Kg = GetSafeDecimal(ws.Cell(row, 4), row, 4, errori);
                r.CostoLavorazione = GetSafeDecimal(ws.Cell(row, 5), row, 5, errori);
                r.Materiale = GetSafeString(ws.Cell(row, 6), row, 6, errori);

                r.Prezzo = GetSafeDecimal(ws.Cell(row, 7), row, 7, errori);
                r.PrezzoConLavorazione = GetSafeDecimal(ws.Cell(row, 8), row, 8, errori);
                r.PrezzoVendita = GetSafeDecimal(ws.Cell(row, 9), row, 9, errori);
                r.DeltaMargine = GetSafeDecimal(ws.Cell(row, 10), row, 10, errori);

                //r.Perc_Delta_Provvigione = EstraiPercentualeDaFormula(ws.Cell(row, 23).FormulaA1);////27/04/2026    estraggo dalla formula di TotaleVariabileProvvigione
                var perc = EstraiPercentualeDaFormula(ws.Cell(row, 23).FormulaA1);//02/05/2026 23    estraggo dalla formula di TotaleVariabileProvvigione
                r.Perc_Delta_Provvigione = NormalizePercent(perc);//02/05/2026			 

                r.ValUnitProvvigioneFissa1_5 = GetSafeDecimal(ws.Cell(row, 11), row, 11, errori);
                r.Delta1ProvvigioneFissa1_5 = GetSafeDecimal(ws.Cell(row, 12), row, 12, errori);
                r.Delta2ProvvigioneFissa1_5 = GetSafeDecimal(ws.Cell(row, 13), row, 13, errori);
                r.TotProvvigioneFissa1_5 = GetSafeDecimal(ws.Cell(row, 14), row, 14, errori);

                r.ValUnitProvvigioneFissa_03 = GetSafeDecimal(ws.Cell(row, 15), row, 15, errori);
                r.Delta1ProvvigioneFissa_03 = GetSafeDecimal(ws.Cell(row, 16), row, 16, errori);
                r.Delta2ProvvigioneFissa_03 = GetSafeDecimal(ws.Cell(row, 17), row, 17, errori);
                r.TotProvvigioneFissa_03 = GetSafeDecimal(ws.Cell(row, 18), row, 18, errori);

                r.ValQuotaFissa_1_5 = GetSafeDecimal(ws.Cell(row, 19), row, 19, errori);
                r.ValQuotaFissa_03 = GetSafeDecimal(ws.Cell(row, 20), row, 20, errori);
                r.TotaleFissoProvvigione = GetSafeDecimal(ws.Cell(row, 21), row, 21, errori);

                r.DeltaResiduoVariabile = GetSafeDecimal(ws.Cell(row, 22), row, 22, errori);
                r.TotaleVariabileProvvigione = GetSafeDecimal(ws.Cell(row, 23), row, 23, errori);
                r.TotaleSommaProvvigioni = GetSafeDecimal(ws.Cell(row, 24), row, 24, errori);

                r.DataConsegna = GetSafeDateTime(ws.Cell(row, 25), row, 25, errori);
                r.TotaleProvvigioneDaFatturare = GetSafeDecimal(ws.Cell(row, 26), row, 26, errori);

                // Celle con dropdown → GetFormattedString() non serve
                r.Fatturare = GetSafeString(ws.Cell(row, 27), row, 27, errori); //ws.Cell(row, 27).GetFormattedString().Trim();

                // Importa solo se Fatturare è ammesso
                if (!valoriAmmessi.Contains(r.Fatturare?.Trim() ?? ""))
                    continue;

                lista.Add(r);
            }

            if (errori.Any())
                throw new Exception("Errori nel file Excel:\n" + string.Join("\n", errori));

            return lista;
        }

        // ============================================================
        //4 )  CST (20/04/2026)
        // ============================================================
        /// <summary>
        /// Lettura specifica per CST. 
        /// Utilizza l'oggetto RigaExcelCST (o quello corrispondente).
        /// </summary>
        public static List<RigaExcelCST> LeggiExcelCST(
            IFormFile fileExcel,
            int firstRow,
            HashSet<string> valoriAmmessi)
        {
            var lista = new List<RigaExcelCST>();
            var errori = new List<string>();

            using var stream = new MemoryStream();
            fileExcel.CopyTo(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);

            if (!workbook.TryGetWorksheet("Commissioni Metal Invest", out var ws))
                throw new Exception("Foglio 'Commissioni Metal Invest' non trovato nel file Excel.");

            for (int row = firstRow; ; row++)
            {
                // 1 & 2: Ordine e Cliente (usati per il check di interruzione)
                var ordine = ws.Cell(row, 1).GetFormattedString().Trim();
                var cliente = ws.Cell(row, 2).GetFormattedString().Trim();

                // Interrompi se entrambi vuoti
                if (string.IsNullOrWhiteSpace(ordine) && string.IsNullOrWhiteSpace(cliente))
                    break;

                var r = new RigaExcelCST();

                // Mappatura secondo sequenza fornita:
                r.OrdineDDT = ordine;                                             // Colonna 1
                r.Cliente = cliente;// GetSafeString(ws.Cell(row, 2), row, 2, errori);        // Colonna 2
                r.Kg = GetSafeDecimal(ws.Cell(row, 3), row, 3, errori);             // Colonna 3
                r.CostoLavorazione = GetSafeDecimal(ws.Cell(row, 4), row, 4, errori); // Colonna 4
                r.Materiale = GetSafeString(ws.Cell(row, 5), row, 5, errori);      // Colonna 5
                r.PrezzoBase = GetSafeDecimal(ws.Cell(row, 6), row, 6, errori);     // Colonna 6
                r.PrezzoEuroRiferimento = GetSafeDecimal(ws.Cell(row, 7), row, 7, errori);  // Colonna 7 ****
                r.PrezzoBaseConLavorazione = GetSafeDecimal(ws.Cell(row, 8), row, 8, errori); // Colonna 8

                r.PrezzoVendita = GetSafeDecimal(ws.Cell(row, 9), row, 9, errori);  // Colonna 9

                r.ProvvigioneFissa01_05 = GetSafeDecimal(ws.Cell(row, 10), row, 10, errori); // Colonna 10
                r.ProvvigioneFissa03 = GetSafeDecimal(ws.Cell(row, 11), row, 11, errori);    // Colonna 11

                //r.Perc_Delta_Provvigione = EstraiPercentualeDaFormula(ws.Cell(row, 15).FormulaA1);//30/04/2026 letto da formula (O)	Totale provv variabile	=(N)*C*50%	TotaleProvvigioneVariabile  
                var perc = EstraiPercentualeDaFormula(ws.Cell(row, 15).FormulaA1);//02/05/2026  letto da formula (O)	Totale provv variabile	=(N)*C*50%	TotaleProvvigioneVariabile  
                r.Perc_Delta_Provvigione = NormalizePercent(perc);//02/05/2026			 




                r.TotaleProvvigioneFissa = GetSafeDecimal(ws.Cell(row, 12), row, 12, errori); // Colonna 12
                r.CIT = GetSafeDecimal(ws.Cell(row, 13), row, 13, errori);                  // Colonna 13
                r.Delta = GetSafeDecimal(ws.Cell(row, 14), row, 14, errori);                // Colonna 14
                r.TotaleProvvigioneVariabile = GetSafeDecimal(ws.Cell(row, 15), row, 15, errori); // Colonna 15
                r.TotaleProvvigioneFattura = GetSafeDecimal(ws.Cell(row, 16), row, 16, errori);   // Colonna 16
                r.DataConsegna = GetSafeDateTime(ws.Cell(row, 17), row, 17, errori);        // Colonna 17
                r.Agente = GetSafeString(ws.Cell(row, 18), row, 18, errori);                // Colonna 18
                r.Fatturare = GetSafeString(ws.Cell(row, 19), row, 19, errori);             // Colonna 19

                // Filtro basato sulla colonna 19
                if (!valoriAmmessi.Contains(r.Fatturare?.Trim() ?? ""))
                    continue;

                lista.Add(r);
            }

            if (errori.Any())
                throw new Exception("Errori nel file Excel:\n" + string.Join("\n", errori));

            return lista;
        }


        // ============================================================
        //5 )  Guerzoni (20/04/2026)
        // ============================================================
        /// <summary>
        /// Lettura specifica per Guerzoni. 
        /// Utilizza l'oggetto RigaExcelGuerzoni (o quello corrispondente).
        /// </summary>

        public static List<RigaExcelGuerzoni> LeggiExcelGuerzoni(
            IFormFile fileExcel,
            int firstRow,
            HashSet<string> valoriAmmessi)
        {
            var lista = new List<RigaExcelGuerzoni>();
            var errori = new List<string>();

            using var stream = new MemoryStream();
            fileExcel.CopyTo(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);

            if (!workbook.TryGetWorksheet("Calkos", out var ws))
                throw new Exception("Foglio 'Calkos' non trovato nel file Excel.");

            for (int row = firstRow; ; row++)
            {
                // Colonne 2 e 3: Ordine e Cliente
                var ordine = ws.Cell(row, 2).GetFormattedString().Trim();
                var cliente = ws.Cell(row, 3).GetFormattedString().Trim();

                // Interrompi solo se ENTRAMBE vuote
                if (string.IsNullOrWhiteSpace(ordine) && string.IsNullOrWhiteSpace(cliente))
                    break;

                var r = new RigaExcelGuerzoni();

                // ============================
                // Campi base
                // ============================
                r.OrdineDDT = ordine;
                r.Cliente = cliente;// GetSafeString(ws.Cell(row, 3), row, 3, errori);

                // ============================
                // Quantità e prezzi
                // ============================
                r.Quantita = GetSafeDecimal(ws.Cell(row, 4), row, 4, errori);
                r.PrezzoUnitario = GetSafeDecimal(ws.Cell(row, 5), row, 5, errori);
                r.Imponibile = GetSafeDecimal(ws.Cell(row, 6), row, 6, errori);

                //r.Perc_Delta_Provvigione = EstraiPercentualeDaFormula(ws.Cell(row, 7).FormulaA1);//29/04/2026 letto da formula F	TotaleProvvigione	F5*0,03
                var perc = EstraiPercentualeDaFormula(ws.Cell(row, 7).FormulaA1);//02/05/2026
                r.Perc_Delta_Provvigione = NormalizePercent(perc);//02/05/2026
                r.TotaleProvvigione = GetSafeDecimal(ws.Cell(row, 7), row, 7, errori);

                // ============================
                // Date
                // ============================
                r.DataConsegna = GetSafeDateTime(ws.Cell(row, 8), row, 8, errori);

                // ============================
                // Fatturare (stringa Excel)
                // ============================
                r.Fatturare = GetSafeString(ws.Cell(row, 9), row, 9, errori);

                // IMPORTA SOLO LE RIGHE CON FATTURARE AMMESSO
                if (!valoriAmmessi.Contains(r.Fatturare?.Trim() ?? ""))
                    continue;

                lista.Add(r);
            }

            if (errori.Any())
                throw new Exception("Errori nel file Excel:\n" + string.Join("\n", errori));

            return lista;
        }



        public static decimal NormalizePercent(decimal? value)
        {

            //Logica applicata per normalizzare le percentuali, basata su osservazioni empiriche:
            // 50 → resta 50
            // 0.5 → diventa 50
            // 0.03 → diventa 3
            // null → diventa 0


            // ---------------------------------------------------------
            // Se il valore è NULL (Excel vuoto o colonna mancante)
            // restituisco 0 per evitare errori nei calcoli
            // ---------------------------------------------------------
            if (value == null)
                return 0;

            decimal v = value.Value;


            // Protezione contro valori microscopici tipo 0.0000001
            if (v > 0 && v < 0.0001m)
                return 0;

            // ---------------------------------------------------------
            // CASO 1: già percentuale "umana" (1 - 100)
            // esempio: 50 = 50%
            // ---------------------------------------------------------
            if (v >= 1 && v <= 100)
                return v;

            // ---------------------------------------------------------
            // CASO 2: formato Excel frazione (0 - 1)
            // esempio: 0.5 = 50%, 0.03 = 3%
            // ---------------------------------------------------------
            if (v > 0 && v < 1)
                return v * 100;

            // ---------------------------------------------------------
            // fallback
            // ---------------------------------------------------------
            return v;
        }



        // ============================================================
        //6 )  Hitech (20/04/2026)
        // ============================================================
        /// <summary>
        /// Lettura specifica per Hitech. 
        /// Utilizza l'oggetto RigaExcelHitech (o quello corrispondente).
        /// </summary>
        public static List<RigaExcelHitech> LeggiExcelHitech(
            IFormFile fileExcel,
            int firstRow,
            HashSet<string> valoriAmmessi)
        {
            var lista = new List<RigaExcelHitech>();
            var errori = new List<string>();

            using var stream = new MemoryStream();
            fileExcel.CopyTo(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);

            if (!workbook.TryGetWorksheet("Calcolo commissioni", out var ws))
                throw new Exception("Foglio 'Calcolo commissioni' non trovato nel file Excel.");

            for (int row = firstRow; ; row++)
            {
                // 1 → OrdineDDT
                var ordine = ws.Cell(row, 1).GetFormattedString().Trim();

                // 3 → Cliente
                var cliente = ws.Cell(row, 3).GetFormattedString().Trim();

                // Stop solo se ENTRAMBE vuote
                if (string.IsNullOrWhiteSpace(ordine) && string.IsNullOrWhiteSpace(cliente))
                    break;

                var r = new RigaExcelHitech();

                // 1 - OrdineDDT
                r.OrdineDDT = ordine;
                // 2 - DataDoc (CAMBIO: prima era colonna 9)
                r.DataDoc = GetSafeDateTime(ws.Cell(row, 2), row, 2, errori);//21/04/2026
                // 3 - Cliente
                r.Cliente = cliente;// GetSafeString(ws.Cell(row, 3), row, 3, errori);
                // 4 - Quantita
                r.Quantita = GetSafeDecimal(ws.Cell(row, 4), row, 4, errori);
                // 5 - PrezzoBase
                r.PrezzoBase = GetSafeDecimal(ws.Cell(row, 5), row, 5, errori);
                // 6 - PrezzoPraticato
                r.PrezzoPraticato = GetSafeDecimal(ws.Cell(row, 6), row, 6, errori);
                // 7 - ProvvigioneUnitaria
                r.ProvvigioneUnitaria = GetSafeDecimal(ws.Cell(row, 7), row, 7, errori);
                // 8 - TotaleProvvigione
                r.TotaleProvvigione = GetSafeDecimal(ws.Cell(row, 8), row, 8, errori);
                // 9 - DataConsegna
                r.DataConsegna = GetSafeDateTime(ws.Cell(row, 9), row, 9, errori);
                // 10 - Agente
                r.Agente = GetSafeString(ws.Cell(row, 10), row, 10, errori);
                // 11 - Fatturare
                r.Fatturare = GetSafeString(ws.Cell(row, 11), row, 11, errori);
                // Importa solo se ammesso
                if (!valoriAmmessi.Contains(r.Fatturare?.Trim() ?? ""))
                    continue;

                lista.Add(r);
            }

            if (errori.Any())
                throw new Exception("Errori nel file Excel:\n" + string.Join("\n", errori));

            return lista;
        }



        // ============================================================
        //7 )  SystemCore (20/04/2026)
        // ============================================================
        /// <summary>
        /// Lettura specifica per SystemCore. 
        /// Utilizza l'oggetto RigaExcelSystemCore (o quello corrispondente).
        /// </summary>

        public static List<RigaExcelSystemCore> LeggiExcelSystemCore(
            IFormFile fileExcel,
            int firstRow,
            HashSet<string> valoriAmmessi)
        {
            var lista = new List<RigaExcelSystemCore>();
            var errori = new List<string>();

            using var stream = new MemoryStream();
            fileExcel.CopyTo(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);

            if (!workbook.TryGetWorksheet("calcolo comm.", out var ws))
                throw new Exception("Foglio 'calcolo comm.' non trovato nel file Excel.");

            for (int row = firstRow; ; row++)
            {
                // Colonne 1 e 2: Ordine e Cliente
                var ordine = ws.Cell(row, 1).GetFormattedString().Trim();   //1 OrdineDDT
                var cliente = ws.Cell(row, 2).GetFormattedString().Trim();  //2 Cliente

                // Interrompi solo se ENTRAMBE vuote
                if (string.IsNullOrWhiteSpace(ordine) && string.IsNullOrWhiteSpace(cliente))
                    break;

                var r = new RigaExcelSystemCore();

                // ============================
                // Campi base
                // ============================
                r.OrdineDDT = ordine;                                       //1 OrdineDDT
                r.Cliente = cliente;                                        //2 Cliente
                r.Misure = GetSafeString(ws.Cell(row, 3), row, 3, errori);  //3 Misure

                // ============================
                // Quantità e costi
                // ============================
                r.Kg = GetSafeDecimal(ws.Cell(row, 4), row, 4, errori);                     //4 Kg
                r.CostoLavorazione = GetSafeDecimal(ws.Cell(row, 5), row, 5, errori);      //5 CostoLavorazione
                r.Materiale = GetSafeString(ws.Cell(row, 6), row, 6, errori);              //6 Materiale

                // ============================
                // Prezzi e quote fisse
                // ============================
                r.PrezzoVendita = GetSafeDecimal(ws.Cell(row, 7), row, 7, errori);         //7 PrezzoVendita
                r.PercentualeQuotaFissa = GetSafeDecimal(ws.Cell(row, 8), row, 8, errori); //8 PercentualeQuotaFissa                                                                                           


                r.ValoreQuotaFissa = GetSafeDecimal(ws.Cell(row, 9), row, 9, errori);      //9 ValoreQuotaFissa
                r.QuotaCentesimiPerKg = EstraiPercentualeDaFormula(ws.Cell(row, 9).FormulaA1);//9  Perc_Delta_Provvigione la devo leggere dalla Formula colonna  "Quota fissa € 0,10"


                // ============================
                // Provvigioni variabili e totali
                // ============================
                r.Delta = GetSafeDecimal(ws.Cell(row, 10), row, 10, errori);                       //10 Delta
                r.TotaleProvvigioneVariabile = GetSafeDecimal(ws.Cell(row, 11), row, 11, errori);  //11 TotaleProvvigioneVariabile                
                //r.Perc_Delta_Provvigione = EstraiPercentualeDaFormula(ws.Cell(row, 11).FormulaA1);//11  Perc_Delta_Provvigione la devo leggere dalla Formula
                var perc = EstraiPercentualeDaFormula(ws.Cell(row, 11).FormulaA1);//02/05/2026 11  Perc_Delta_Provvigione la devo leggere dalla Formula
                r.Perc_Delta_Provvigione = NormalizePercent(perc);//02/05/2026





                r.TotaleProvvigione = GetSafeDecimal(ws.Cell(row, 12), row, 12, errori);           //12 TotaleProvvigione

                // ============================
                // Date e totali
                // ============================
                r.DataConsegna = GetSafeDateTime(ws.Cell(row, 13), row, 13, errori);               //13 DataConsegna
                r.TotaleProvvigioneDaFatturare = GetSafeDecimal(ws.Cell(row, 14), row, 14, errori);//14 TotaleProvvigioneDaFatturare

                // ============================
                // Fatturare (stringa Excel)
                // ============================
                r.Fatturare = GetSafeString(ws.Cell(row, 15), row, 15, errori);                    //15 Fatturare


                // IMPORTA SOLO LE RIGHE CON FATTURARE AMMESSO
                if (!valoriAmmessi.Contains(r.Fatturare?.Trim() ?? ""))
                    continue;

                lista.Add(r);
            }

            if (errori.Any())
                throw new Exception("Errori nel file Excel:\n" + string.Join("\n", errori));

            return lista;
        }

        // ============================================================
        //8 )  SystemP (20/04/2026)
        // ============================================================
        /// <summary>
        /// Lettura specifica per SystemP. 
        /// Utilizza l'oggetto RigaExcelSystemP (o quello corrispondente).
        /// </summary>
        public static List<RigaExcelSystemP> LeggiExcelSystemP(
            IFormFile fileExcel,
            int firstRow,
            HashSet<string> valoriAmmessi)
        {
            var lista = new List<RigaExcelSystemP>();
            var errori = new List<string>();

            using var stream = new MemoryStream();
            fileExcel.CopyTo(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);

            if (!workbook.TryGetWorksheet("Calkos", out var ws))
                throw new Exception("Foglio 'Calkos' non trovato nel file Excel.");

            for (int row = firstRow; ; row++)
            {
                // Colonne 1 e 2: Ordine e Cliente
                var ordine = ws.Cell(row, 1).GetFormattedString().Trim();   //1 OrdineDDT
                var cliente = ws.Cell(row, 2).GetFormattedString().Trim();  //2 Cliente

                // Interrompi solo se ENTRAMBE vuote
                if (string.IsNullOrWhiteSpace(ordine) && string.IsNullOrWhiteSpace(cliente))
                    break;

                var r = new RigaExcelSystemP();

                //1 OrdineDDT
                r.OrdineDDT = ordine;

                //2 Cliente
                r.Cliente = cliente;//GetSafeString(ws.Cell(row, 2), row, 2, errori);

                //3 KG
                r.Kg = GetSafeDecimal(ws.Cell(row, 3), row, 3, errori);

                //4 Materiale
                r.Materiale = GetSafeString(ws.Cell(row, 4), row, 4, errori);

                //5 LavorazionePrezzoAcquisto
                r.LavorazionePrezzoAcquisto = GetSafeDecimal(ws.Cell(row, 5), row, 5, errori);

                //6 BaseAcquisto
                r.BaseAcquisto = GetSafeDecimal(ws.Cell(row, 6), row, 6, errori);

                //7 CIT
                r.CIT = GetSafeDecimal(ws.Cell(row, 7), row, 7, errori);

                //8 PrezzoAcquisto
                r.PrezzoAcquisto = GetSafeDecimal(ws.Cell(row, 8), row, 8, errori);

                //9 DataRiferimentoPrezzoAcquisto
                r.DataRiferimentoPrezzoAcquisto = GetSafeDateTime(ws.Cell(row, 9), row, 9, errori);

                //10 CIT_EUR_KG
                r.CIT_EUR_KG = GetSafeDecimal(ws.Cell(row, 10), row, 10, errori);

                //11 CITsuAcquisto
                r.CITsuAcquisto = GetSafeDecimal(ws.Cell(row, 11), row, 11, errori);

                //12 ProvvigioneUnitaria
                r.ProvvigioneUnitaria = GetSafeDecimal(ws.Cell(row, 12), row, 12, errori);

                //13 ValoreProvvigioneFissa
                r.ValoreProvvigioneFissa = GetSafeDecimal(ws.Cell(row, 13), row, 13, errori);

                //14 LavorazionePrezzoVendita
                r.LavorazionePrezzoVendita = GetSafeDecimal(ws.Cell(row, 14), row, 14, errori);

                //15 BaseVendita
                r.BaseVendita = GetSafeDecimal(ws.Cell(row, 15), row, 15, errori);

                //16 CITsuVendita
                r.CITsuVendita = GetSafeDecimal(ws.Cell(row, 16), row, 16, errori);

                //17 PrezzoVendita
                r.PrezzoVendita = GetSafeDecimal(ws.Cell(row, 17), row, 17, errori);

                //18 Delta
                r.Delta = GetSafeDecimal(ws.Cell(row, 18), row, 18, errori);

                //19 PercentualeDelta
                //r.PercentualeDelta = GetSafeDecimal(ws.Cell(row, 19), row, 19, errori);
                var perc = GetSafeDecimal(ws.Cell(row, 19), row, 19, errori); ;//02/05/2026 PercentualeDelta 
                r.PercentualeDelta = NormalizePercent(perc);//02/05/2026			 




                //20 TotaleVariabileProvvigione
                r.TotaleVariabileProvvigione = GetSafeDecimal(ws.Cell(row, 20), row, 20, errori);

                //21 TotaleProvvigione
                r.TotaleProvvigione = GetSafeDecimal(ws.Cell(row, 21), row, 21, errori);

                //22 DataConsegnaIpotetica
                r.DataConsegnaIpotetica = GetSafeDateTime(ws.Cell(row, 22), row, 22, errori);

                //23 Agente
                r.Agente = GetSafeString(ws.Cell(row, 23), row, 23, errori);

                //24 Scadenza
                r.Scadenza = GetSafeDateTime(ws.Cell(row, 24), row, 24, errori);

                //25 Fatturare
                r.Fatturare = GetSafeString(ws.Cell(row, 25), row, 25, errori);


                // IMPORTA SOLO LE RIGHE CON FATTURARE AMMESSO
                if (!valoriAmmessi.Contains(r.Fatturare?.Trim() ?? ""))
                    continue;

                lista.Add(r);
            }

            if (errori.Any())
                throw new Exception("Errori nel file Excel:\n" + string.Join("\n", errori));

            return lista;
        }

        // ============================================================
        //9 )  TradingAndConsulting (20/04/2026)
        // ============================================================
        /// <summary>
        /// Lettura specifica per TradingAndConsulting. 
        /// Utilizza l'oggetto RigaExcelTradingAndConsulting (o quello corrispondente).
        /// </summary>

        public static List<RigaExcelTradingAndConsulting> LeggiExcelTradingAndConsulting(
            IFormFile fileExcel,
            int firstRow,
            HashSet<string> valoriAmmessi)
        {
            var lista = new List<RigaExcelTradingAndConsulting>();
            var errori = new List<string>();

            using var stream = new MemoryStream();
            fileExcel.CopyTo(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);

            if (!workbook.TryGetWorksheet("Commissioni Metal Invest", out var ws))
                throw new Exception("Foglio 'Commissioni Metal Invest' non trovato nel file Excel.");

            for (int row = firstRow; ; row++)
            {
                // 1 & 2: Ordine e Cliente (usati per il check di interruzione)
                var ordine = ws.Cell(row, 1).GetFormattedString().Trim();
                var cliente = ws.Cell(row, 2).GetFormattedString().Trim();

                // Interrompi se entrambi vuoti
                if (string.IsNullOrWhiteSpace(ordine) && string.IsNullOrWhiteSpace(cliente))
                    break;

                var r = new RigaExcelTradingAndConsulting();

                // Mappatura secondo sequenza fornita:
                r.OrdineDDT = ordine;                                             // Colonna 1
                r.Cliente = cliente;// GetSafeString(ws.Cell(row, 2), row, 2, errori);        // Colonna 2
                r.Kg = GetSafeDecimal(ws.Cell(row, 3), row, 3, errori);             // Colonna 3
                r.CostoLavorazione = GetSafeDecimal(ws.Cell(row, 4), row, 4, errori); // Colonna 4
                r.Materiale = GetSafeString(ws.Cell(row, 5), row, 5, errori);      // Colonna 5
                r.PrezzoBase = GetSafeDecimal(ws.Cell(row, 6), row, 6, errori);     // Colonna 6
                r.PrezzoEuroRiferimento = GetSafeDecimal(ws.Cell(row, 7), row, 7, errori);  // Colonna 7 ****
                r.PrezzoBaseConLavorazione = GetSafeDecimal(ws.Cell(row, 8), row, 8, errori); // Colonna 8

                r.PrezzoVendita = GetSafeDecimal(ws.Cell(row, 9), row, 9, errori);  // Colonna 9

                r.ProvvigioneFissa01_05 = GetSafeDecimal(ws.Cell(row, 10), row, 10, errori); // Colonna 10
                r.ProvvigioneFissa03 = GetSafeDecimal(ws.Cell(row, 11), row, 11, errori);    // Colonna 11
                //r.Perc_Delta_Provvigione = EstraiPercentualeDaFormula(ws.Cell(row, 15).FormulaA1);//30/04/2026 letto da formula (O)	Totale provv variabile	=(N)*C*50%	TotaleProvvigioneVariabile  
                var perc = EstraiPercentualeDaFormula(ws.Cell(row, 15).FormulaA1);//02/05/2026  letto da formula (O)	Totale provv variabile	=(N)*C*50%	TotaleProvvigioneVariabile  
                r.Perc_Delta_Provvigione = NormalizePercent(perc);//02/05/2026			 


                r.TotaleProvvigioneFissa = GetSafeDecimal(ws.Cell(row, 12), row, 12, errori); // Colonna 12
                r.CIT = GetSafeDecimal(ws.Cell(row, 13), row, 13, errori);                  // Colonna 13
                r.Delta = GetSafeDecimal(ws.Cell(row, 14), row, 14, errori);                // Colonna 14
                r.TotaleProvvigioneVariabile = GetSafeDecimal(ws.Cell(row, 15), row, 15, errori); // Colonna 15
                r.TotaleProvvigioneFattura = GetSafeDecimal(ws.Cell(row, 16), row, 16, errori);   // Colonna 16
                r.DataConsegna = GetSafeDateTime(ws.Cell(row, 17), row, 17, errori);        // Colonna 17
                r.Agente = GetSafeString(ws.Cell(row, 18), row, 18, errori);                // Colonna 18
                r.Fatturare = GetSafeString(ws.Cell(row, 19), row, 19, errori);             // Colonna 19

                // Filtro basato sulla colonna 19
                if (!valoriAmmessi.Contains(r.Fatturare?.Trim() ?? ""))
                    continue;

                lista.Add(r);
            }

            if (errori.Any())
            {
                var dettagli = string.Join("\n - ", errori);
                throw new Exception($"Errori nel file Excel (vedi righe specifiche):\n - {dettagli}");
            }


            return lista;
        }


        // ------------------------------------------------------------
        // METODI ROBUSTI (PRIVATI - CONDIVISI MA SOLA LETTURA)
        // ------------------------------------------------------------


        private static int? GetSafeInt(IXLCell cell, int row, int col, List<string> errori)//13/04/2026
        {
            // Tentativo diretto ClosedXML
            if (cell.TryGetValue(out int value))
                return value;

            // Lettura stringa grezza (gestisce dropdown, formattazioni, ecc.)
            var raw = cell.GetFormattedString().Trim();

            if (!string.IsNullOrWhiteSpace(raw))
                errori.Add($"Errore INTERO riga {row}, col {col}, valore='{raw}'");

            return null;
        }


        /// <summary>
        /// Legge in modo sicuro il valore numerico di una cella Excel.
        ///
        /// Excel memorizza i numeri come DOUBLE (floating point).
        /// Questo metodo legge il valore interno reale usando TryGetValue<double>,
        /// mantenendo il comportamento il più fedele possibile a Excel.
        ///
        /// Il valore viene poi convertito in DECIMAL per un utilizzo più sicuro in .NET.
        /// 
        /// NOTA:
        /// - La conversione DOUBLE → DECIMAL può introdurre minime differenze
        ///   rispetto ai calcoli interni di Excel.
        /// - Celle con formule restituiscono il valore calcolato (se aggiornato).
        /// - Celle vuote restituiscono null.
        /// - Celle non numeriche vengono segnalate nella lista errori.
        /// </summary>
        //private static decimal? GetSafeDecimal(IXLCell cell, int row, int col, List<string> errori)
        //{
        //    if (cell == null || cell.IsEmpty())
        //        return null;

        //    if (cell.TryGetValue(out double dblValue))
        //        return (decimal)dblValue;

        //    var raw = cell.GetString();
        //    if (!string.IsNullOrWhiteSpace(raw))
        //        errori.Add($"Errore DECIMALE riga {row}, col {col}, valore='{raw}'");

        //    return null;
        //}

        private static decimal GetSafeDecimal(IXLCell cell, int row, int col, List<string> errori)
        {
            // 1) Celle nulle o vuote → 0
            if (cell == null || cell.IsEmpty())
                return 0m;

            // 2) Prova lettura numerica diretta (Excel memorizza DOUBLE)
            if (cell.TryGetValue(out double dblValue))
                return (decimal)dblValue;

            // 3) Lettura stringa grezza
            var raw = cell.GetFormattedString().Trim();

            // Vuoto → 0
            if (string.IsNullOrWhiteSpace(raw))
                return 0m;

            // 4) Normalizza virgole/punti
            raw = raw.Replace(",", ".");

            // 5) Prova conversione manuale
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                return val;

            // 6) Se arrivi qui → valore non valido
            errori.Add($"Errore DECIMALE riga {row}, col {col}, valore='{raw}'");
            return 0m;
        }


        //private static decimal? GetSafeDecimal(IXLCell cell, int row, int col, List<string> errori)
        //{
        //    if (cell.TryGetValue(out decimal value)) return value;
        //    if (!string.IsNullOrWhiteSpace(cell.GetString()))
        //        errori.Add($"Errore DECIMALE riga {row}, col {col}, valore='{cell.GetString()}'");
        //    return null;
        //}
        private static DateTime? GetSafeDateTime(IXLCell cell, int row, int col, List<string> errori)
        {
            // Tentativo diretto ClosedXML
            if (cell.TryGetValue(out DateTime value))
                return value;

            // Lettura stringa grezza
            var raw = cell.GetFormattedString().Trim();

            if (!string.IsNullOrWhiteSpace(raw))
                errori.Add($"Errore DATA riga {row}, col {col}, valore='{raw}'");

            return null;
        }


        private static string? GetSafeString(IXLCell cell, int row, int col, List<string> errori)
        {
            // Tentativo diretto ClosedXML
            if (cell.TryGetValue(out string value))
                return value;

            // Gestisce dropdown, formattazioni, ecc.
            var raw = cell.GetFormattedString().Trim();

            if (!string.IsNullOrWhiteSpace(raw))
                return raw;

            // Se c'è qualcosa ma non è stringa valida → errore
            var raw2 = cell.GetString();
            if (!string.IsNullOrWhiteSpace(raw2))
                errori.Add($"Errore STRINGA riga {row}, col {col}, valore='{raw2}'");

            return null;
        }

        private static bool? GetSafeBool(IXLCell cell, int row, int col, List<string> errori)
        {
            if (cell == null || cell.IsEmpty())
                return null;

            // 1) Tentativo diretto ClosedXML
            if (cell.TryGetValue(out bool boolValue))
                return boolValue;

            // 2) Lettura stringa grezza
            var raw = cell.GetFormattedString()?.Trim();

            if (string.IsNullOrWhiteSpace(raw))
                return null;

            // 3) Normalizzazioni tipiche Excel
            switch (raw.ToUpperInvariant())
            {
                case "SI":
                case "SÌ":
                case "YES":
                case "Y":
                case "1":
                case "TRUE":
                case "VERO":
                case "OK":
                    return true;

                case "NO":
                case "N":
                case "0":
                case "FALSE":
                    return false;
            }

            // 4) Se arrivi qui → valore non valido
            errori.Add($"Errore BOOLEANO riga {row}, col {col}, valore='{raw}'");
            return null;
        }

        public static void SetTitolo(IXLWorksheet ws, string titolo, int colStart, int colEnd)
        {
            ws.Cell(1, colStart).Value = titolo;
            ws.Cell(1, colStart).Style
                .Font.SetBold()
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            int colonne = colEnd - colStart + 1;
            ws.Column(colStart).Width = 40 * colonne;
        }




        public static decimal EstraiPercentualeDaFormula(string formula)//24/04/2026
        {
            if (string.IsNullOrWhiteSpace(formula))
                return 0;

            // 1) Cerca numeri con il simbolo %
            var matchPercent = Regex.Match(formula, @"(\d+[.,]?\d*)\s*%");
            if (matchPercent.Success)
            {
                var raw = matchPercent.Groups[1].Value.Replace(",", ".");
                if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var valore))
                    return valore;
            }

            // 2) Se non c’è %, cerca numeri decimali (es: 0,5)
            var matchDecimal = Regex.Match(formula, @"(\d+[.,]\d+)");
            if (matchDecimal.Success)
            {
                var raw = matchDecimal.Groups[1].Value.Replace(",", ".");
                if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var valore))
                    return valore;
            }

            return 0;
        }





    }
}