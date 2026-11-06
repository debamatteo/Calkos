using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Calkos.web.Models.Dashboard;

namespace Calkos.web.Services.Export.Templates
{
    /// <summary>
    /// Template PDF per la stampa delle provvigioni agente.
    /// Usa QuestPDF (MIT, gratuito).
    /// </summary>
    public class AgentiPdfTemplate : IDocument
    {
        private readonly List<RigaCliente> _righe;
        private readonly string _titolo;

        /// <summary>
        /// Costruttore: riceve le righe e il titolo da stampare.
        /// </summary>
        public AgentiPdfTemplate(List<RigaCliente> righe, string titolo)
        {
            _righe = righe;
            _titolo = titolo;
        }

        /// <summary>
        /// Metadati PDF (standard).
        /// </summary>
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        /// <summary>
        /// Composizione del documento PDF.
        /// </summary>
        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(30);

                // ============================================================
                // HEADER
                // ============================================================
                page.Header().Column(col =>
                {
                    col.Item().Text(_titolo)
                        .FontSize(18)
                        .Bold();

                    col.Item().Text($"Agente: {_righe.First().AgenteDescrizione}")
                        .FontSize(12);

                    col.Item().Text($"Pagamento: {_righe.First().TipoPagamento}")
                        .FontSize(12);
                });

                // ============================================================
                // CONTENUTO: TABELLA
                // ============================================================
                page.Content().Table(table =>
                {
                    // Definizione colonne
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn();   // Cliente
                        c.ConstantColumn(80); // Quantità
                        c.ConstantColumn(80); // Importo
                        c.ConstantColumn(80); // Provvigione
                    });

                    // Header tabella
                    table.Header(h =>
                    {
                        h.Cell().Text("Cliente").Bold();
                        h.Cell().Text("Quantità").Bold();
                        h.Cell().Text("Importo").Bold();
                        h.Cell().Text("Provvigione").Bold();
                    });

                    // Righe dati
                    foreach (var r in _righe)
                    {
                        table.Cell().Text(r.NomeCliente);
                        table.Cell().Text(r.Quantita.ToString());
                        table.Cell().Text(r.Importo.ToString());
                        table.Cell().Text(r.ProvvigioneAgente.ToString());
                    }

                    // ============================================================
                    // FOOTER: TOTALI
                    // ============================================================
                    table.Footer(f =>
                    {
                        f.Cell().Text("TOTALE").Bold();
                        f.Cell().Text(_righe.Sum(x => x.Quantita).ToString()).Bold();
                        f.Cell().Text(_righe.Sum(x => x.Importo).ToString()).Bold();
                        f.Cell().Text(_righe.Sum(x => x.ProvvigioneAgente).ToString()).Bold();
                    });
                });

                // ============================================================
                // FOOTER PAGINA
                // ============================================================
                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span("Pagina ").FontSize(10);
                    txt.CurrentPageNumber().FontSize(10);
                });
            });
        }
    }
}

