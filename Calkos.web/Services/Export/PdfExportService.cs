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
        private string FormatValue(object value, string format)//24/05/2026
        {
            if (value == null)
                return "";

            if (string.IsNullOrEmpty(format))
                return value.ToString();

            format = format.ToLower();

            // ============================
            // 0) BOLEANI E TESTO PURO
            // ============================
            if (value is bool b)
                return b ? "SI" : "NO";

            // ==== MODIFICA 01/11/2026 - INIZIO ====
            // Flag 0/1 (es. Fatturata) con format "boolean" nel JSON: SI / NO come per i veri bool
            if (format == "boolean" && value is int flag)
                return flag == 1 ? "SI" : "NO";
            // ==== MODIFICA 01/11/2026 - FINE ====



            if (string.IsNullOrEmpty(format))
                return value.ToString();

            format = format.ToLower();

            if (format == "text")
                return value.ToString();
            // ============================
            // 1) INTERI
            // ============================
            if (format == "int")
            {
                if (decimal.TryParse(value.ToString(), out decimal d))
                    return d.ToString("#,##0", CultureInfo.GetCultureInfo("it-IT"));
                return value.ToString();
            }

            // ============================
            // 2) DECIMALI (2 decimali fissi)
            // ============================
            if (format.Contains("decimal"))
            {
                if (decimal.TryParse(value.ToString(), out decimal d))
                    return d.ToString("#,##0.00", CultureInfo.GetCultureInfo("it-IT"));
                return value.ToString();
            }

            // ============================
            // 3) VALUTA (€ + 2 decimali)
            // ============================
            if (format.Contains("currency"))
            {
                if (decimal.TryParse(value.ToString(), out decimal d))
                    return d.ToString("€ #,##0.00", CultureInfo.GetCultureInfo("it-IT"));
                return value.ToString();
            }

            // ============================
            // 4) PERCENTUALI (5 → 5%)
            // ============================
            if (format.Contains("percent"))
            {
                if (decimal.TryParse(value.ToString(), out decimal perc))
                {
                    // Conversione come Excel: 5 → 0,05
                    decimal matematico = perc / 100m;
                    return matematico.ToString("0.00%", CultureInfo.GetCultureInfo("it-IT"));
                }
                return value.ToString();
            }

            // ============================
            // 5) DATE (dd/MM/yyyy)
            // ============================
            if (format.Contains("date"))
            {
                if (DateTime.TryParse(value.ToString(), out DateTime dt))
                    return dt.ToString("dd/MM/yyyy");
                return value.ToString();
            }

            // Default
            return value.ToString();
        }





        /// <summary>
        /// Genera un documento PDF in formato Landscape (Orizzontale) con layout dinamico.
        /// Utilizza QuestPDF per la composizione fluida della tabella e degli header.
        /// 22/10/2026 Layout adattivo: dimensione pagina, margini, font e padding dipendono dal numero di colonne esportate.
        /// </summary>
        public byte[] CreaPdfProspettoListaOrdini(IEnumerable<object> righe, List<ProspettoColumnConfig> colonne, string titolo, string nomeMandatario, int mese, int anno)
        {
            // 22/10/2026 Colonne effettivamente esportate (calcolate una sola volta e riusate in header e righe)
            var colonneExport = colonne.Where(x => x.Export).ToList();
            int numeroColonne = colonneExport.Count;

            // 22/10/2026 Profilo di impaginazione in base al numero di colonne:
            // più colonne => margini, font e padding più piccoli; oltre 12 colonne si passa ad A3 orizzontale
            // (più spazio utile: in stampa il PDF si riduce su A4 mantenendo il testo nitido).
            var profilo = numeroColonne switch
            {
                <= 8 => (Pagina: PageSizes.A4.Landscape(), Margine: 25f, FontCorpo: 8f, FontHeader: 8f, Padding: 4f),
                <= 12 => (Pagina: PageSizes.A4.Landscape(), Margine: 20f, FontCorpo: 7f, FontHeader: 7f, Padding: 3f),
                <= 16 => (Pagina: PageSizes.A3.Landscape(), Margine: 20f, FontCorpo: 7.5f, FontHeader: 7.5f, Padding: 3f),
                _ => (Pagina: PageSizes.A3.Landscape(), Margine: 15f, FontCorpo: 6.5f, FontHeader: 6.5f, Padding: 2f)
            };

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // 22/10/2026 Dimensione e margini dal profilo adattivo (prima: A4 Landscape fisso, margine 30)
                    page.Size(profilo.Pagina);
                    page.Margin(profilo.Margine);

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
                            foreach (var c in colonneExport)
                                cols.RelativeColumn(c.Width);
                        });

                        // Intestazioni di colonna (Header) - 22/10/2026 font e padding dal profilo adattivo
                        table.Header(header =>
                        {
                            foreach (var c in colonneExport)
                            {
                                header.Cell()
                                      .Element(x => CellHeaderStyle(x, profilo.Padding))
                                      .Text(c.Label).Bold().FontSize(profilo.FontHeader);
                            }
                        });

                        // =================================================================================
                        // ESTRAZIONE E ORDINAMENTO DEI DATI BASATO SULLA CONFIGURAZIONE JSON 27/05/2026
                        // =================================================================================

                        // 1. Filtriamo solo le colonne che hanno un valore in 'SortOrder' (richiesto l'ordinamento)
                        //    e le ordiniamo in base a quel valore per stabilire la priorità (es. prima 1, poi 2, poi 3).
                        var criteriOrdinamento = colonne
                            .Where(c => c.SortOrder.HasValue)
                            .OrderBy(c => c.SortOrder.Value)
                            .ToList();

                        // Questa variabile conterrà la sequenza ordinata. Usiamo 'IOrderedEnumerable' perché
                        // è l'unico tipo che permette di concatenare gli ordinamenti successivi tramite 'ThenBy'.
                        IOrderedEnumerable<object> righeOrdinate = null;

                        // Procediamo all'ordinamento solo se ci sono dati da elaborare e se sono stati definiti criteri nel JSON
                        if (righe != null && righe.Any() && criteriOrdinamento.Any())
                        {
                            // -----------------------------------------------------------------------------
                            // PRIMO LIVELLO DI ORDINAMENTO (Obbligatorio usare OrderBy / OrderByDescending)
                            // -----------------------------------------------------------------------------
                            var primoCriterio = criteriOrdinamento.First();

                            // Tramite Reflection, recuperiamo le informazioni della proprietà dell'oggetto usando il nome scritto nel JSON
                            var primoProp = righe.First().GetType().GetProperty(primoCriterio.Name);

                            // Se non è espressamente "desc", di default ordiniamo in modo ascendente (A-Z, 0-9)
                            bool isAsc = primoCriterio.SortDirection?.ToLower() != "desc";

                            if (isAsc)
                                righeOrdinate = righe.OrderBy(r => primoProp?.GetValue(r));
                            else
                                righeOrdinate = righe.OrderByDescending(r => primoProp?.GetValue(r));

                            // -----------------------------------------------------------------------------
                            // LIVELLI DI ORDINAMENTO SUCCESSIVI (Obbligatorio usare ThenBy / ThenByDescending)
                            // -----------------------------------------------------------------------------
                            // 'ThenBy' ordina i "duplicati" del livello precedente senza distruggere l'ordinamento già calcolato.
                            for (int i = 1; i < criteriOrdinamento.Count; i++)
                            {
                                var criterio = criteriOrdinamento[i];

                                var prop = righe.First().GetType().GetProperty(criterio.Name);
                                bool isNextAsc = criterio.SortDirection?.ToLower() != "desc";

                                if (isNextAsc)
                                    righeOrdinate = righeOrdinate.ThenBy(r => prop?.GetValue(r));
                                else
                                    righeOrdinate = righeOrdinate.ThenByDescending(r => prop?.GetValue(r));
                            }
                        }

                        // -----------------------------------------------------------------------------
                        // APPLICAZIONE DEL RISULTATO
                        // -----------------------------------------------------------------------------
                        // Se 'righeOrdinate' è popolato (c'erano criteri) usiamo la lista ordinata, altrimenti l'originale.
                        var listaFinale = righeOrdinate ?? righe;

                        // Ciclo sulle righe di dati: Reflection per mappare proprietà dinamiche
                        //19/09/2026
                        foreach (var r in listaFinale)
                        {
                            foreach (var c in colonneExport)
                            {
                                var prop = r.GetType().GetProperty(c.Name);
                                var value = prop?.GetValue(r);
                                var formatted = FormatValue(value, c.Format);

                                // 22/10/2026 Padding dal profilo adattivo
                                var cell = table.Cell().Element(x => CellStyle(x, profilo.Padding));

                                string fmt = c.Format?.ToLower() ?? "";

                                //// Centra i Booleani (SI / NO)
                                //if (value is bool)
                                //{
                                //    cell.AlignCenter().Text(formatted).FontSize(profilo.FontCorpo);
                                //}

                                // Centra i Booleani (SI / NO)
                                // 01/11/2026 era: if (value is bool)  -> include i flag 0/1 con format "boolean" (es. Fatturata)
                                if (value is bool || (fmt == "boolean" && value is int))
                                {
                                    cell.AlignCenter().Text(formatted).FontSize(profilo.FontCorpo);
                                }


                                // Allinea a destra numeri, valute, percentuali e interi
                                else if (fmt == "int" || fmt.Contains("decimal") || fmt.Contains("currency") || fmt.Contains("percent"))
                                {
                                    cell.AlignRight().Text(formatted).FontSize(profilo.FontCorpo);
                                }
                                // Allinea a sinistra il resto (Testo, Date, ecc.)
                                else
                                {
                                    cell.AlignLeft().Text(formatted).FontSize(profilo.FontCorpo);
                                }
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

        // 22/10/2026 Overload con padding variabile, usati da CreaPdfProspettoListaOrdini (layout adattivo)
        private static IContainer CellHeaderStyle(IContainer container, float padding)
        {
            return container
                .Border(1)
                .BorderColor("#CCCCCC")
                .Background("#F2F2F2")
                .Padding(padding)
                .AlignLeft()
                .AlignMiddle();
        }

        private static IContainer CellStyle(IContainer container, float padding)
        {
            return container
                .Border(1)
                .BorderColor("#CCCCCC")
                .Padding(padding)
                .AlignMiddle(); // L'allineamento orizzontale è gestito dinamicamente nel loop dei dati
        }



    }
}
