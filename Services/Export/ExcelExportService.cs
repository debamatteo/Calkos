using Calkos.web.Helpers;
using Calkos.web.Models.Dashboard;
using Calkos.web.Models.Prospetti;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;
using Humanizer;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;

namespace Calkos.web.Services.Export
{
    public class ExcelExportService
    {
        // ============================================================
        //  CREA FILE EXCEL IN MEMORIA
        // ============================================================
        // ============================================================================
        //  CREA FILE EXCEL PER I CLIENTI
        //  - Genera una tabella formattata con header, bordi e numeri corretti
        //  - Formattazione professionale: colonne auto-fit, totali in grassetto
        // ============================================================================
        public byte[] CreaExcelClienti(List<RigaCliente> righe, string titolo)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Clienti");

            // 1) TITOLO DEL REPORT
            // Usiamo 3 come ultima colonna perché la quarta è commentata
            ExcelHelper.SetTitolo(ws, titolo, 1, 3);

            // 2) HEADER DELLA TABELLA (Riga 3)
            ws.Cell(3, 1).Value = "Cliente";
            ws.Cell(3, 2).Value = "Quantità";
            ws.Cell(3, 3).Value = "Importo";

            // Applichiamo lo stile solo alle 3 colonne effettive
            ws.Range(3, 1, 3, 3).Style
                .Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.LightGray)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin);

            int row = 4;

            // 3) RIGHE DELLA TABELLA
            foreach (var r in righe)
            {
                ws.Cell(row, 1).Value = r.NomeCliente;
                ws.Cell(row, 2).Value = r.Quantita;
                ws.Cell(row, 3).Value = r.CommissioniTotali;

                // Formattazione celle numeriche 
                ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00"; //"#,##0";Quantità Quantità come intero //"#,##0"; Con decimali  "#,##0.00"
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";//Importo con 2 decimali

                // Bordo solo per le 3 colonne popolate
                ws.Range(row, 1, row, 3).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                row++;
            }

            // 4) RIGA TOTALI
            ws.Cell(row, 1).Value = "TOTALE";
            ws.Cell(row, 1).Style.Font.SetBold();

            ws.Cell(row, 2).FormulaA1 = $"=SUM(B4:B{row - 1})";
            ws.Cell(row, 3).FormulaA1 = $"=SUM(C4:C{row - 1})";

            // Stile riga totali (solo colonne 1-3)
            ws.Range(row, 1, row, 3).Style
                .Font.SetBold()
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin);

            ws.Cell(row, 2).Style.NumberFormat.Format =  "#,##0.00"; // Quantità come intero = "#,##0" // Quantità Con decimali = "#,##0.00"
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";//Importo con 2 decimali

            // 5) AUTO-FIT E SALVATAGGIO
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return stream.ToArray();
        }

        // ============================================================================
        //  CREA FILE EXCEL PER GLI AGENTI
        //  - Stessa formattazione dei clienti
        //  - Cambia solo la prima colonna (Agente)
        // ============================================================================
        public byte[] CreaExcelAgenti(List<RigaCliente> righe, string titolo)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Agenti");

            // ============================================================
            // 1) TITOLO DEL REPORT
            // ============================================================
            // Utilizziamo le 4 colonne (Cliente, Quantità, Importo, Provvigione)
            ExcelHelper.SetTitolo(ws, titolo, 1, 4);

            // ============================================================
            // 2) HEADER DELLA TABELLA (Riga 3)
            // ============================================================
            ws.Cell(3, 1).Value = "Cliente";
            ws.Cell(3, 2).Value = "Quantità";
            ws.Cell(3, 3).Value = "Importo";
            ws.Cell(3, 4).Value = "Provvigione";

            ws.Range(3, 1, 3, 4).Style
                .Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.LightGray)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin);

            int row = 4;

            // ============================================================
            // 3) RIGHE DELLA TABELLA
            // ============================================================
            foreach (var r in righe)
            {
                ws.Cell(row, 1).Value = r.NomeCliente;
                ws.Cell(row, 2).Value = r.Quantita;
                ws.Cell(row, 3).Value = r.CommissioniTotali;
                ws.Cell(row, 4).Value = r.ValoreProvvigioneAgente;

                // Applicazione formattazione a 2 decimali su tutte le colonne numeriche
                ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00"; // Quantità con decimali
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00"; // Importo
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00"; // Provvigione

                ws.Range(row, 1, row, 4).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                row++;
            }

            // ============================================================
            // 4) RIGA TOTALI
            // ============================================================
            ws.Cell(row, 1).Value = "TOTALE";
            ws.Cell(row, 1).Style.Font.SetBold();

            // Formule per i totali dinamici
            ws.Cell(row, 2).FormulaA1 = $"=SUM(B4:B{row - 1})";
            ws.Cell(row, 3).FormulaA1 = $"=SUM(C4:C{row - 1})";
            ws.Cell(row, 4).FormulaA1 = $"=SUM(D4:D{row - 1})";

            ws.Range(row, 1, row, 4).Style
                .Font.SetBold()
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetInsideBorder(XLBorderStyleValues.Thin);

            // Mantenimento della formattazione a 2 decimali anche nei totali
            ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";

            // ============================================================
            // 5) AUTO-FIT E GENERAZIONE STREAM
            // ============================================================
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return stream.ToArray();
        }


        // ============================================================================
        //  CREA FILE EXCEL LA STAMPA DEL PROSPETTO
        //  - Stessa formattazione dei clienti
        //  - Cambia solo la prima colonna (Agente)
        // ============================================================================

        public byte[] CreaExcelProspettoListaOrdini(IEnumerable<object> righe, List<ProspettoColumnConfig> colonne, string nomeMandatario, int mese, int anno)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Dati");

            // ============================================================
            // 1) INTESTAZIONE DOCUMENTO (Informazioni di contesto)
            // ============================================================

            string descrizioneMese = (mese >= 1 && mese <= 12)
                ? CultureInfo.GetCultureInfo("it-IT").DateTimeFormat.GetMonthName(mese).ToUpper()
                : "N/A";

            // TITOLO DINAMICO
            string titolo = $"{nomeMandatario} - {descrizioneMese} {anno}";

            // Richiamo del tuo helper
            // Calcolo del numero di colonne esportate per centrare correttamente il titolo
            int colonneEsportate = colonne.Count(x => x.Export);
            ExcelHelper.SetTitolo(ws, titolo, 1, colonneEsportate);

            // ============================================================
            // 2) HEADER DELLA TABELLA (Riga 3)
            // ============================================================
            int row = 3;
            int col = 1;

            foreach (var c in colonne.Where(x => x.Export))
            {
                var headerCell = ws.Cell(row, col);
                headerCell.Value = c.Label;

                headerCell.Style.Font.SetBold();
                headerCell.Style.Fill.SetBackgroundColor(XLColor.LightGray);
                headerCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                col++;
            }

            row++;

            // ============================================================
            // 3) INSERIMENTO DATI DINAMICI
            // ============================================================
            foreach (var r in righe)
            {
                col = 1;
                foreach (var c in colonne.Where(x => x.Export))
                {
                    var prop = r.GetType().GetProperty(c.Name);
                    var value = prop?.GetValue(r);
                    var cell = ws.Cell(row, col);

                    // Gestione tipi di dato per ClosedXML
                    if (value == null)
                    {
                        cell.Value = Blank.Value;
                    }
                    else if (value is DateTime dt)
                    {
                        cell.Value = dt;
                    }
                    else if (decimal.TryParse(value.ToString(), out decimal decimalValue))
                    {
                        // Inserimento del valore come numero per permettere calcoli in Excel
                        cell.Value = decimalValue;
                    }
                    else
                    {
                        cell.Value = value.ToString();
                    }

                    // Applicazione formattazione dinamica basata sulla configurazione JSON
                    if (!string.IsNullOrEmpty(c.Format))
                    {
                        var f = c.Format.ToLower();

                        if (f == "int")
                        {
                            cell.Style.NumberFormat.Format = "#,##0";
                        }
                        // Correzione: impostato a 2 decimali (#,##0.00) invece di 3
                        else if (f.Contains("decimal"))
                        {
                            cell.Style.NumberFormat.Format = "#,##0.00";
                        }
                        // Gestione specifica formato valuta con simbolo Euro
                        else if (f.Contains("currency"))
                        {
                            cell.Style.NumberFormat.Format = "€ #,##0.00";
                        }
                        else if (f.Contains("date"))
                        {
                            // Recupero formato data specifico se presente, altrimenti default
                            cell.Style.DateFormat.Format = f.Contains(":") ? f.Split(':')[1] : "dd/MM/yyyy";
                        }
                    }

                    col++;
                }
                row++;
            }

            // ============================================================
            // 4) FINALIZZAZIONE FILE
            // ============================================================
            // Auto-dimensionamento delle colonne in base al contenuto generato
            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            return stream.ToArray();
        }

        // --- REPORT 1: DETTAGLIO ANNUALE PER FATTURE CLIENTE ---
        /// <summary>
        /// Genera un file Excel multi-colonna rappresentante il prospetto annuale delle fatture.
        /// Organizza i dati in blocchi mensili da 4 colonne (Nr. Fatt, Cliente, Kg, Importo) 
        /// con formattazione numerica rigorosa e stili visivi conformi al layout aziendale.
        /// </summary>
        /// <summary>
        /// Genera il prospetto annuale delle fatture in formato Excel.
        /// Il report organizza i dati in blocchi mensili da 4 colonne con formattazione numerica specifica,
        /// separatori tratteggiati per le righe di dettaglio e bordi esterni spessi per i totali a fondo pagina.
        /// </summary>
        public byte[] GeneraExcelRiepilogoFatture(int anno, string mandatario, string connectionString, int idMandatario)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Prospetto Annuale Fatture");

            // Mapping dei nomi dei mesi per le testate e dei prefissi per il recupero dati dal database
            string[] mesiEstesi = { "GENNAIO", "FEBBRAIO", "MARZO", "APRILE", "MAGGIO", "GIUGNO", "LUGLIO", "AGOSTO", "SETTEMBRE", "OTTOBRE", "NOVEMBRE", "DICEMBRE" };
            string[] prefissi = { "Gen", "Feb", "Mar", "Apr", "Mag", "Giu", "Lug", "Ago", "Set", "Ott", "Nov", "Dic" };

            // --- FASE 1: DEFINIZIONE STRUTTURA E STILE DELLE INTESTAZIONI ---
            for (int i = 0; i < 12; i++)
            {
                int c = (i * 4) + 1; // Calcolo offset colonna per ogni mese

                // Formattazione riga mese (Cella unita con bordo spesso)
                var monthRange = ws.Range(1, c, 1, c + 3);
                monthRange.Merge().Value = mesiEstesi[i];
                monthRange.Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                monthRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thick);

                // Definizione label colonne di dettaglio
                ws.Cell(2, c).Value = "Nr. Fatt";
                ws.Cell(2, c + 1).Value = "Cliente";
                ws.Cell(2, c + 2).Value = "Kg.";
                ws.Cell(2, c + 3).Value = "Importo";

                // Styling testata colonne (Allineamento centrato e bordo perimetrale spesso)
                var headerRange = ws.Range(2, c, 2, c + 3);
                headerRange.Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                headerRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thick);
            }

            // --- FASE 2: ELABORAZIONE DATI E POPOLAMENTO GRIGLIA ---
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand("spGeneraProspettoAnnualeExcel_Fatture", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Anno", SqlDbType.Int).Value = anno;
                cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = idMandatario;

                conn.Open();

                int rigaInizioDati = 3;
                int rigaMassimaRaggiunta = 3;

                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    int rigaCorrente = rigaInizioDati;
                    while (rdr.Read())
                    {
                        for (int i = 0; i < 12; i++)
                        {
                            int col = (i * 4) + 1;
                            string p = prefissi[i];
                            var clienteVal = rdr[$"{p}_Cliente"]?.ToString();

                            // Inserimento dati solo in presenza di record validi per il mese corrente
                            if (!string.IsNullOrEmpty(clienteVal))
                            {
                                // Nr. Fattura: Formato numerico intero allineato a destra
                                var cellFatt = ws.Cell(rigaCorrente, col);
                                cellFatt.Value = rdr[$"{p}_NrFatt"] == DBNull.Value ? 0 : Convert.ToInt32(rdr[$"{p}_NrFatt"]);
                                cellFatt.Style.NumberFormat.Format = "0";
                                cellFatt.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

                                // Ragione Sociale Cliente
                                ws.Cell(rigaCorrente, col + 1).Value = clienteVal;

                                // Quantità KG: Formato decimale (2 cifre) con separatore migliaia e bordo tratteggiato
                                var cellKg = ws.Cell(rigaCorrente, col + 2);
                                cellKg.Value = rdr[$"{p}_Kg"] == DBNull.Value ? 0 : Convert.ToDouble(rdr[$"{p}_Kg"]);
                                cellKg.Style.NumberFormat.Format = "#,##0.00";
                                cellKg.Style.Border.SetLeftBorder(XLBorderStyleValues.Dotted);

                                // Importo Valuta: Formato Euro con bordo tratteggiato a sinistra
                                var cellImp = ws.Cell(rigaCorrente, col + 3);
                                cellImp.Value = rdr[$"{p}_Importo"] == DBNull.Value ? 0 : Convert.ToDouble(rdr[$"{p}_Importo"]);
                                cellImp.Style.NumberFormat.Format = "#,##0.00\" €\"";
                                cellImp.Style.Border.SetLeftBorder(XLBorderStyleValues.Dotted);
                            }
                        }
                        rigaCorrente++;
                        rigaMassimaRaggiunta = rigaCorrente;
                    }
                }

                // --- FASE 3: CALCOLO TOTALI E APPLICAZIONE BORDATURE FINALI ---
                // Si imposta il totale con una riga di distacco rispetto all'ultimo record inserito
                int rigaTotale = rigaMassimaRaggiunta + 1;

                for (int i = 0; i < 12; i++)
                {
                    int col = (i * 4) + 1;

                    // Bordo perimetrale spesso per l'intero blocco dati del mese
                    ws.Range(1, col, rigaMassimaRaggiunta, col + 3).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thick);

                    // Totale KG: Inserimento formula SUM e bordatura Thick completa (Outside)
                    var cellTotKg = ws.Cell(rigaTotale, col + 2);
                    cellTotKg.FormulaA1 = $"SUM({ws.Cell(rigaInizioDati, col + 2).Address}:{ws.Cell(rigaMassimaRaggiunta - 1, col + 2).Address})";
                    cellTotKg.Style.Font.SetBold().NumberFormat.Format = "#,##0.00";
                    cellTotKg.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                    cellTotKg.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thick);

                    // Totale Importo: Inserimento formula SUM e bordatura Thick completa (Outside)
                    var cellTotImp = ws.Cell(rigaTotale, col + 3);
                    cellTotImp.FormulaA1 = $"SUM({ws.Cell(rigaInizioDati, col + 3).Address}:{ws.Cell(rigaMassimaRaggiunta - 1, col + 3).Address})";
                    cellTotImp.Style.Font.SetBold().NumberFormat.Format = "#,##0.00\" €\"";
                    cellTotImp.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                    cellTotImp.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thick);
                }
            }

            // Ottimizzazione automatica della larghezza delle colonne
            ws.Columns().AdjustToContents();

            // Restituzione del file tramite finalizzazione dello stream (metodo helper)
            return Finalizza(wb);
        }




        //public byte[] GeneraExcelRiepilogoFatture(int anno, string mandatario, string connectionString, int idMandatario)
        //{
        //    using var wb = new XLWorkbook();
        //    var ws = wb.Worksheets.Add("Prospetto Annuale Fatture");
        //    string[] prefissi = { "Gen", "Feb", "Mar", "Apr", "Mag", "Giu", "Lug", "Ago", "Set", "Ott", "Nov", "Dic" };

        //    // Header
        //    for (int i = 0; i < 12; i++)
        //    {
        //        int c = (i * 4) + 1;
        //        ws.Cell(1, c).Value = prefissi[i];
        //        ws.Cell(2, c).Value = "Nr.Fatt";
        //        ws.Cell(2, c + 1).Value = "Cliente";
        //        ws.Cell(2, c + 2).Value = "Kg";
        //        ws.Cell(2, c + 3).Value = "Importo";
        //        ws.Range(1, c, 1, c + 3).Merge().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        //    }

        //    using (SqlConnection conn = new SqlConnection(connectionString))
        //    {
        //        SqlCommand cmd = new SqlCommand("spGeneraProspettoAnnualeExcel_Fatture", conn);
        //        cmd.CommandType = CommandType.StoredProcedure;
        //        cmd.Parameters.Add("@Anno", SqlDbType.Int).Value = anno;
        //        cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = idMandatario;

        //        conn.Open();
        //        using (SqlDataReader rdr = cmd.ExecuteReader())
        //        {
        //            int riga = 3;
        //            while (rdr.Read())
        //            {
        //                for (int i = 0; i < 12; i++)
        //                {
        //                    int col = (i * 4) + 1;
        //                    string p = prefissi[i];

        //                    ws.Cell(riga, col).Value = rdr[$"{p}_NrFatt"]?.ToString();
        //                    ws.Cell(riga, col + 1).Value = rdr[$"{p}_Cliente"]?.ToString();

        //                    // --- CORREZIONE FORMATI ---

        //                    // KG: Quantità intera (usiamo double o int per sicurezza calcoli)
        //                    var cellKg = ws.Cell(riga, col + 2);
        //                    cellKg.Value = Convert.ToDouble(rdr[$"{p}_Kg"] == DBNull.Value ? 0 : rdr[$"{p}_Kg"]);
        //                    cellKg.Style.NumberFormat.Format = "#,##0.00"; // Quantità con decimali// "#,##0"; // Formato numero intero

        //                    // EURO: Decimale con valuta
        //                    var cellEuro = ws.Cell(riga, col + 3);
        //                    cellEuro.Value = Convert.ToDouble(rdr[$"{p}_Importo"] == DBNull.Value ? 0 : rdr[$"{p}_Importo"]);
        //                    cellEuro.Style.NumberFormat.Format = "#,##0.00 €"; // Formato valuta
        //                }
        //                riga++;
        //            }
        //        }
        //    }
        //    return Finalizza(wb);
        //}


        // --- REPORT 2: DETTAGLIO ANNUALE PER CLIENTE ---
        /// <summary>
        /// Genera il prospetto annuale clienti con riga dei totali evidenziata in verde,
        /// incluse le colonne percentuali, e ottimizzazione della visibilità dei mesi.
        /// </summary>
        /// <summary>
        /// Genera un file Excel contenente il prospetto annuale dei clienti.
        /// Include KG e Euro per ogni mese, totali annuali e incidenza percentuale.
        /// </summary>
        /// <param name="anno">Anno di riferimento per il report.</param>
        /// <param name="mandatario">Nome del mandatario (opzionale per intestazioni).</param>
        /// <param name="connectionString">Stringa di connessione al database SQL Server.</param>
        /// <param name="idMandatario">ID univoco del mandatario per il filtro dati.</param>
        /// <returns>Array di byte rappresentante il file Excel generato.</returns>
        public byte[] GeneraExcelRiepilogoClienti(int anno, string mandatario, string connectionString, int idMandatario)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Prospetto Annuale Clienti");

            // Array per la gestione dinamica delle colonne mensili
            string[] mesiEstesi = {
        "Gennaio", "Febbraio", "Marzo", "Aprile", "Maggio", "Giugno",
        "Luglio", "Agosto", "Settembre", "Ottobre", "Novembre", "Dicembre"
    };

            // Definizione dei colori istituzionali per il report
            var coloreVerdeTotali = XLColor.FromHtml("#92D050");
            var coloreBluHeader = XLColor.LightSteelBlue;
            var coloreGrigioHeader = XLColor.LightGray;

            // --- 1. DEFINIZIONE STRUTTURA INTESTAZIONI (HEADER) ---

            // Colonna "CLIENTE" (Unione di due righe verticali)
            ws.Cell(1, 1).Value = "CLIENTE";
            ws.Range(1, 1, 2, 1).Merge().Style
                .Font.SetBold()
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Fill.SetBackgroundColor(coloreGrigioHeader)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            // Generazione dinamica delle colonne per i 12 mesi
            for (int i = 0; i < 12; i++)
            {
                // Ogni mese occupa 2 colonne (Kg e Euro), partendo dalla colonna 2
                int colBase = 2 + (i * 2);

                // Header Mese (Sopra KG ed Euro)
                var rangeMese = ws.Range(1, colBase, 1, colBase + 1);
                rangeMese.Merge().Value = mesiEstesi[i].ToUpper();
                rangeMese.Style
                    .Font.SetBold()
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Fill.SetBackgroundColor(coloreGrigioHeader)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                // Sub-Header: KG e Euro
                ws.Cell(2, colBase).Value = "Kg";
                ws.Cell(2, colBase + 1).Value = "Euro";
                ws.Range(2, colBase, 2, colBase + 1).Style
                    .Font.SetBold()
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }

            // Header Sezione Totali Annuali
            int colTotAnnuale = 2 + (12 * 2);
            var rangeTitoloAnnuale = ws.Range(1, colTotAnnuale, 1, colTotAnnuale + 1);
            rangeTitoloAnnuale.Merge().Value = "ANNUALE";
            rangeTitoloAnnuale.Style
                .Font.SetBold()
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Fill.SetBackgroundColor(coloreBluHeader)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thick);

            ws.Cell(2, colTotAnnuale).Value = "KG";
            ws.Cell(2, colTotAnnuale + 1).Value = "Euro";
            ws.Range(2, colTotAnnuale, 2, colTotAnnuale + 1).Style.Font.SetBold().Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            // Header Sezione Incidenza Percentuale
            int colPerc = colTotAnnuale + 2;
            var rangeTitoloPerc = ws.Range(1, colPerc, 1, colPerc + 1);
            rangeTitoloPerc.Merge().Value = "ANNUALE %";
            rangeTitoloPerc.Style
                .Font.SetBold()
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Fill.SetBackgroundColor(coloreBluHeader)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thick);

            ws.Cell(2, colPerc).Value = "Kg %";
            ws.Cell(2, colPerc + 1).Value = "Euro %";
            ws.Range(2, colPerc, 2, colPerc + 1).Style.Font.SetBold().Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            // --- 2. RECUPERO DATI E POPOLAMENTO RIGHE ---

            int rigaInizioDati = 3;
            int rigaCorrente = rigaInizioDati;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                //SqlCommand cmd = new SqlCommand("spGeneraProspettoAnnualeExcel_Cliente", conn);//SOLO CLIENTE CHE HANNO UNA RIGA IN PROSPETTO
                SqlCommand cmd = new SqlCommand("[spGeneraProspettoAnnualeExcel_Cliente_Completo]", conn);//TUTTI I CLIENTI ANCHE QUELLI CHE NON HANNO RIGHE IN PROSPETTO
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Anno", SqlDbType.Int).Value = anno;
                cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = idMandatario;

                conn.Open();
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        // Scrittura Ragione Sociale Cliente
                        ws.Cell(rigaCorrente, 1).Value = rdr["Cliente"]?.ToString();

                        // Ciclo popolamento valori mensili
                        for (int i = 0; i < 12; i++)
                        {
                            int colBase = 2 + (i * 2);
                            string nomeMese = mesiEstesi[i];

                            ws.Cell(rigaCorrente, colBase).Value = Convert.ToDouble(rdr[$"{nomeMese}_Kg"] == DBNull.Value ? 0 : rdr[$"{nomeMese}_Kg"]);
                            ws.Cell(rigaCorrente, colBase + 1).Value = Convert.ToDouble(rdr[$"{nomeMese}_Euro"] == DBNull.Value ? 0 : rdr[$"{nomeMese}_Euro"]);

                            // Formattazione numerica standard
                            ws.Range(rigaCorrente, colBase, rigaCorrente, colBase + 1).Style.NumberFormat.Format = "#,##0.00";
                        }

                        // Scrittura Totali Annuali recuperati da DB
                        ws.Cell(rigaCorrente, colTotAnnuale).Value = Convert.ToDouble(rdr["Totale_Annuale_Kg"] == DBNull.Value ? 0 : rdr["Totale_Annuale_Kg"]);
                        ws.Cell(rigaCorrente, colTotAnnuale + 1).Value = Convert.ToDouble(rdr["Totale_Annuale_Euro"] == DBNull.Value ? 0 : rdr["Totale_Annuale_Euro"]);
                        ws.Range(rigaCorrente, colTotAnnuale, rigaCorrente, colTotAnnuale + 1).Style.NumberFormat.Format = "#,##0.00";

                        rigaCorrente++;
                    }
                }
            }

            // --- 3. GESTIONE RIGA TOTALI E FORMULE EXCEL ---

            int rigaTotaleFisico = rigaCorrente;

            // Calcolo Somme Verticali (Mesi e Totale Annuale)
            for (int c = 2; c <= colTotAnnuale + 1; c++)
            {
                var cellaTotale = ws.Cell(rigaTotaleFisico, c);
                string letteraColonna = ws.Cell(rigaInizioDati, c).WorksheetColumn().ColumnLetter();

                // Inserimento formula SUM dinamica
                cellaTotale.FormulaA1 = $"SUM({letteraColonna}{rigaInizioDati}:{letteraColonna}{rigaTotaleFisico - 1})";

                // Formattazione estetica riga totale (Verde)
                cellaTotale.Style.Font.SetBold().Fill.SetBackgroundColor(coloreVerdeTotali);
                cellaTotale.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thick);
                cellaTotale.Style.NumberFormat.Format = "#,##0.00";
            }

            // Calcolo Formule Percentuali (Incidenza riga su totale generale)
            string refTotaleKg = ws.Cell(rigaTotaleFisico, colTotAnnuale).Address.ToString();
            string refTotaleEuro = ws.Cell(rigaTotaleFisico, colTotAnnuale + 1).Address.ToString();

            for (int r = rigaInizioDati; r <= rigaTotaleFisico; r++)
            {
                string refKgCliente = ws.Cell(r, colTotAnnuale).Address.ToString();
                string refEuroCliente = ws.Cell(r, colTotAnnuale + 1).Address.ToString();

                // Formula % Kg
                var cellaPercKg = ws.Cell(r, colPerc);
                cellaPercKg.FormulaA1 = $"=IF({refTotaleKg}<>0, {refKgCliente}/{refTotaleKg}, 0)";
                cellaPercKg.Style.NumberFormat.Format = "0.00%";

                // Formula % Euro
                var cellaPercEuro = ws.Cell(r, colPerc + 1);
                cellaPercEuro.FormulaA1 = $"=IF({refTotaleEuro}<>0, {refEuroCliente}/{refTotaleEuro}, 0)";
                cellaPercEuro.Style.NumberFormat.Format = "0.00%";

                // Evidenziazione verde anche per le percentuali nella riga finale (100%)
                if (r == rigaTotaleFisico)
                {
                    ws.Range(r, colPerc, r, colPerc + 1).Style
                        .Font.SetBold()
                        .Fill.SetBackgroundColor(coloreVerdeTotali)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }
            }

            // --- 4. RIFINITURE LAYOUT E VISIBILITÀ ---

            // Adattamento automatico del contenuto
            ws.Columns().AdjustToContents();

            // Incremento manuale della larghezza per garantire la leggibilità dei mesi
            for (int c = 2; c <= colPerc + 1; c++)
            {
                ws.Column(c).Width += 3; // Padding di sicurezza
            }

            // --- CORREZIONE ERRORE CS1061: APPLICAZIONE BORDI SEPARATORI ---

            // Sezione ANNUALE (Col Tot Ann e Col Tot Ann + 1)
            var rangeAnnuale = ws.Range(1, colTotAnnuale, rigaTotaleFisico, colTotAnnuale + 1);
            rangeAnnuale.Style.Border.LeftBorder = XLBorderStyleValues.Thick;
            rangeAnnuale.Style.Border.RightBorder = XLBorderStyleValues.Thick;

            // Sezione PERCENTUALI (Col Perc e Col Perc + 1)
            var rangePercentuali = ws.Range(1, colPerc, rigaTotaleFisico, colPerc + 1);
            rangePercentuali.Style.Border.RightBorder = XLBorderStyleValues.Thick;

            // Esportazione finale del workbook
            return Finalizza(wb);
        }


        //public byte[] GeneraExcelRiepilogoClienti(int anno, string mandatario, string connectionString, int idMandatario)
        //{
        //    using var wb = new XLWorkbook();
        //    var ws = wb.Worksheets.Add("Prospetto Annuale Clienti");
        //    string[] mesiEstesi = { "Gennaio", "Febbraio", "Marzo", "Aprile", "Maggio", "Giugno", "Luglio", "Agosto", "Settembre", "Ottobre", "Novembre", "Dicembre" };

        //    // --- 1. COSTRUZIONE INTESTAZIONI (Righe 1 e 2) ---

        //    // Cella Cliente (Unita verticalmente su 2 righe)
        //    ws.Cell(1, 1).Value = "CLIENTE";
        //    ws.Range(1, 1, 2, 1).Merge().Style
        //        .Font.SetBold()
        //        .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
        //        .Fill.SetBackgroundColor(XLColor.LightGray);

        //    // Ciclo per creare le intestazioni dei 12 mesi
        //    for (int i = 0; i < 12; i++)
        //    {
        //        int col = 2 + (i * 3); // Colonna di partenza per il mese (2, 5, 8...)

        //        // Riga 1: Nome Mese (Unito su 3 colonne: Kg, Euro, Diff)
        //        var meseRange = ws.Range(1, col, 1, col + 2);
        //        meseRange.Merge().Value = mesiEstesi[i].ToUpper();
        //        meseRange.Style
        //            .Font.SetBold()
        //            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
        //            .Fill.SetBackgroundColor(XLColor.LightGray)
        //            .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

        //        // Riga 2: Sottotitoli
        //        ws.Cell(2, col).Value = "Kg";
        //        ws.Cell(2, col + 1).Value = "Euro";
        //        ws.Cell(2, col + 2).Value = "Diff";

        //        ws.Range(2, col, 2, col + 2).Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        //    }

        //    // Header Totali Annuali (alla fine delle 36 colonne dei mesi)
        //    int colTot = 2 + (12 * 3);
        //    var totRange = ws.Range(1, colTot, 1, colTot + 2);
        //    totRange.Merge().Value = "TOTALI ANNUALI";
        //    totRange.Style
        //        .Font.SetBold()
        //        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
        //        .Fill.SetBackgroundColor(XLColor.LightSteelBlue);

        //    ws.Cell(2, colTot).Value = "Tot Kg";
        //    ws.Cell(2, colTot + 1).Value = "Tot Euro";
        //    ws.Cell(2, colTot + 2).Value = "Tot Diff";
        //    ws.Range(2, colTot, 2, colTot + 2).Style.Font.SetBold();

        //    // --- 2. POPOLAMENTO DATI ---

        //    using (SqlConnection conn = new SqlConnection(connectionString))
        //    {
        //        SqlCommand cmd = new SqlCommand("spGeneraProspettoAnnualeExcel_Cliente", conn);
        //        cmd.CommandType = CommandType.StoredProcedure;
        //        cmd.Parameters.Add("@Anno", SqlDbType.Int).Value = anno;
        //        cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = idMandatario;

        //        conn.Open();
        //        using (SqlDataReader rdr = cmd.ExecuteReader())
        //        {
        //            int riga = 3; // I dati partono dalla riga 3
        //            while (rdr.Read())
        //            {
        //                // Nome Cliente
        //                ws.Cell(riga, 1).Value = rdr["Cliente"]?.ToString();

        //                for (int i = 0; i < 12; i++)
        //                {
        //                    int col = 2 + (i * 3);
        //                    string m = mesiEstesi[i];

        //                    // KG (Mese) -> Numero Intero
        //                    var cKg = ws.Cell(riga, col);
        //                    cKg.Value = Convert.ToDouble(rdr[$"{m}_Kg"] == DBNull.Value ? 0 : rdr[$"{m}_Kg"]);
        //                    cKg.Style.NumberFormat.Format = "#,##0.00"; // Quantità con decimali//"#,##0";

        //                    // EURO (Mese) -> Decimale con simbolo
        //                    var cEu = ws.Cell(riga, col + 1);
        //                    cEu.Value = Convert.ToDouble(rdr[$"{m}_Euro"] == DBNull.Value ? 0 : rdr[$"{m}_Euro"]);
        //                    cEu.Style.NumberFormat.Format = "#,##0.00 €";

        //                    // DIFF (Mese) -> Decimale con simbolo
        //                    var cDi = ws.Cell(riga, col + 2);
        //                    cDi.Value = Convert.ToDouble(rdr[$"{m}_Diff"] == DBNull.Value ? 0 : rdr[$"{m}_Diff"]);
        //                    cDi.Style.NumberFormat.Format = "#,##0.00 €";
        //                }

        //                // TOTALI ANNUALI (Fine riga)
        //                var tcKg = ws.Cell(riga, colTot);
        //                tcKg.Value = Convert.ToDouble(rdr["Totale_Annuale_Kg"] == DBNull.Value ? 0 : rdr["Totale_Annuale_Kg"]);
        //                tcKg.Style.NumberFormat.Format = "#,##0.00"; // Quantità con decimali//"#,##0";

        //                var tcEu = ws.Cell(riga, colTot + 1);
        //                tcEu.Value = Convert.ToDouble(rdr["Totale_Annuale_Euro"] == DBNull.Value ? 0 : rdr["Totale_Annuale_Euro"]);
        //                tcEu.Style.NumberFormat.Format = "#,##0.00 €";

        //                var tcDi = ws.Cell(riga, colTot + 2);
        //                tcDi.Value = Convert.ToDouble(rdr["Totale_Annuale_Diff"] == DBNull.Value ? 0 : rdr["Totale_Annuale_Diff"]);
        //                tcDi.Style.NumberFormat.Format = "#,##0.00 €";

        //                riga++;
        //            }
        //        }
        //    }

        //    return Finalizza(wb);
        //}

        private byte[] Finalizza(XLWorkbook wb)
        {
            foreach (var w in wb.Worksheets)
            {
                w.Columns().AdjustToContents();
                // Formattazione aggiuntiva mia: rendiamo gli header grassetto
                w.Row(1).Style.Font.SetBold();
                w.Row(2).Style.Font.SetBold();
            }

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

    }
}
