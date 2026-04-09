using Calkos.web.Models.Dashboard;
using Calkos.web.Models.Prospetti;
using Calkos.web.Services.Export.Templates;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization; // Necessario per la conversione del mese

namespace Calkos.web.Services.Export
{
    public class PdfExportService
    {
        public byte[] CreaPdfClienti(List<RigaCliente> righe, string titolo)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Header().Element(header =>
                    {
                        header.PaddingBottom(20).AlignCenter().Text(titolo).FontSize(20).Bold();
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Cliente").Bold();
                            header.Cell().Element(CellStyle).AlignRight().Text("Quantità").Bold();
                            header.Cell().Element(CellStyle).AlignRight().Text("Importo").Bold();
                        });

                        foreach (var r in righe)
                        {
                            table.Cell().Element(CellStyle).Text(r.NomeCliente);
                            table.Cell().Element(CellStyle).AlignRight().Text(FormatValue(r.Quantita, "decimal"));
                            table.Cell().Element(CellStyle).AlignRight().Text(FormatValue(r.CommissioniTotali, "decimal"));
                        }

                        table.Cell().Element(CellStyle).Text("TOTALE").Bold();
                        table.Cell().Element(CellStyle).AlignRight().Text(FormatValue(righe.Sum(x => x.Quantita), "decimal")).Bold();
                        table.Cell().Element(CellStyle).AlignRight().Text(FormatValue(righe.Sum(x => x.CommissioniTotali), "decimal")).Bold();
                    });

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.Border(1).BorderColor("#CCCCCC").Padding(5).AlignMiddle();
                    }
                });
            });

            return document.GeneratePdf();
        }
        public byte[] CreaPdfAgenti(List<RigaCliente> righe, string titolo)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Header().Element(header =>
                    {
                        header.PaddingBottom(20).AlignCenter().Text(titolo).FontSize(20).Bold();
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Cliente").Bold();
                            header.Cell().Element(CellStyle).AlignRight().Text("Quantità").Bold();
                            header.Cell().Element(CellStyle).AlignRight().Text("Importo").Bold();
                            header.Cell().Element(CellStyle).AlignRight().Text("Provvigione").Bold();
                        });

                        foreach (var r in righe)
                        {
                            table.Cell().Element(CellStyle).Text(r.NomeCliente);
                            table.Cell().Element(CellStyle).AlignRight().Text(FormatValue(r.Quantita, "decimal"));
                            table.Cell().Element(CellStyle).AlignRight().Text(FormatValue(r.CommissioniTotali, "decimal"));
                            table.Cell().Element(CellStyle).AlignRight().Text(FormatValue(r.ValoreProvvigioneAgente, "decimal"));
                        }

                        table.Cell().Element(CellStyle).Text("TOTALE").Bold();
                        table.Cell().Element(CellStyle).AlignRight().Text(FormatValue(righe.Sum(x => x.Quantita), "decimal")).Bold();
                        table.Cell().Element(CellStyle).AlignRight().Text(FormatValue(righe.Sum(x => x.CommissioniTotali), "decimal")).Bold();
                        table.Cell().Element(CellStyle).AlignRight().Text(FormatValue(righe.Sum(x => x.ValoreProvvigioneAgente), "decimal")).Bold();
                    });

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.Border(1).BorderColor("#CCCCCC").Padding(5).AlignMiddle();
                    }
                });
            });

            return document.GeneratePdf();
        }

        // Spostato fuori per essere riutilizzato o modificato globalmente
        //private static IContainer CellStyle(IContainer container)
        //{
        //    return container
        //        .Border(1)
        //        .BorderColor("#CCCCCC")
        //        .Padding(5)
        //        .AlignMiddle();
        //}

        /// <summary>
        /// Formatta i valori grezzi in stringhe leggibili basandosi sulla configurazione delle colonne.
        /// Garantisce la coerenza visiva (2 decimali, date italiane, simboli valuta).
        /// </summary>
        private string FormatValue(object value, string format)
        {
            if (value == null)
                return "";

            if (string.IsNullOrEmpty(format))
                return value.ToString();

            format = format.ToLower();

            // Formattazione Numeri Interi: Separatore migliaia senza decimali (es. 1.250)
            if (format == "int")
                return Convert.ToInt32(value).ToString("N0");

            // Formattazione Decimali e Valuta: Forza sempre i 2 decimali come richiesto
            if (format.Contains("decimal") || format == "currency")
            {
                decimal val = Convert.ToDecimal(value);
                // Se 'currency', applica il formato monetario locale (it-IT) con simbolo €
                return format == "currency"
                    ? val.ToString("C2", CultureInfo.GetCultureInfo("it-IT"))
                    : val.ToString("N2", CultureInfo.GetCultureInfo("it-IT")); // Aggiungi la cultura qui!
            }

            // Formattazione Date: Converte in formato standard italiano dd/MM/yyyy
            if (format.Contains("date"))
            {
                return Convert.ToDateTime(value).ToString("dd/MM/yyyy");
            }

            return value.ToString();
        }

        /// <summary>
        /// Genera un documento PDF in formato Landscape (Orizzontale) con layout dinamico.
        /// Utilizza QuestPDF per la composizione fluida della tabella e degli header.
        /// </summary>
        public byte[] CreaPdfProspettoListaOrdini(IEnumerable<object> righe, List<ProspettoColumnConfig> colonne, string titolo, string nomeMandatario, int mese, int anno)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Configurazione Layout: A4 Orizzontale per ospitare più colonne
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);

                    // ============================================================
                    // 1) HEADER: Titolo e Metadati del Prospetto
                    // ============================================================
                    page.Header().Column(col =>
                    {
                        string descrizioneMese = (mese >= 1 && mese <= 12)
                            ? CultureInfo.GetCultureInfo("it-IT").DateTimeFormat.GetMonthName(mese).ToUpper()
                            : "N/A";

                        col.Item().Text(titolo).FontSize(18).Bold().AlignCenter();

                        // Riga sottotitolo con dettagli Mandatario, Mese e Anno
                        col.Item().PaddingBottom(10).Row(row =>
                        {
                            row.RelativeItem().Text($"Mandatario: {nomeMandatario}").Bold().FontSize(10);
                            row.RelativeItem().Text($"Mese: {descrizioneMese}").Bold().FontSize(10);
                            row.RelativeItem().Text($"Anno: {anno}").Bold().FontSize(10);
                        });
                    });

                    // ============================================================
                    // 2) TABELLA: Rendering dinamico dei dati
                    // ============================================================
                    page.Content().PaddingTop(10).Table(table =>
                    {
                        // Definizione delle proporzioni delle colonne (RelativeColumn) basata sul JSON
                        table.ColumnsDefinition(cols =>
                        {
                            foreach (var c in colonne.Where(x => x.Export))
                                cols.RelativeColumn(c.Width);
                        });

                        // Intestazioni di colonna (Header)
                        table.Header(header =>
                        {
                            foreach (var c in colonne.Where(x => x.Export))
                            {
                                header.Cell().Element(CellHeaderStyle).Text(c.Label).Bold().FontSize(8);
                            }
                        });

                        // Ciclo sulle righe di dati: Reflection per mappare proprietà dinamiche
                        foreach (var r in righe)
                        {
                            foreach (var c in colonne.Where(x => x.Export))
                            {
                                var prop = r.GetType().GetProperty(c.Name);
                                var value = prop?.GetValue(r);
                                var formatted = FormatValue(value, c.Format);

                                // Applica l'allineamento a destra per valori numerici/monetari
                                var cell = table.Cell().Element(CellStyle);

                                if (c.Format.Contains("decimal") || c.Format.Contains("currency") || c.Format == "int")
                                    cell.AlignRight().Text(formatted).FontSize(7);
                                else
                                    cell.AlignLeft().Text(formatted).FontSize(7);
                            }
                        }
                    });

                    // ============================================================
                    // 3) FOOTER: Numerazione pagine
                    // ============================================================
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Pagina ");
                        x.CurrentPageNumber();
                        x.Span(" di ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        // ============================================================
        // STILI DI CELLA: Metodi helper per la coerenza grafica
        // ============================================================

        private static IContainer CellHeaderStyle(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor("#CCCCCC")
                .Background("#F2F2F2") // Sfondo grigio chiaro per l'intestazione
                .Padding(5)
                .AlignLeft()
                .AlignMiddle();
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor("#CCCCCC")
                .Padding(5)
                .AlignMiddle(); // L'allineamento orizzontale è gestito dinamicamente nel loop dei dati
        }





    }
}
