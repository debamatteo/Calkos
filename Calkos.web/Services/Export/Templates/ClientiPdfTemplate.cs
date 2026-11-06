using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Calkos.web.Models.Dashboard;

namespace Calkos.web.Services.Export.Templates
{
    public class ClientiPdfTemplate : IDocument
    {
        private readonly List<RigaCliente> _righe;
        private readonly string _titolo;

        public ClientiPdfTemplate(List<RigaCliente> righe, string titolo)
        {
            _righe = righe;
            _titolo = titolo;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header().Text(_titolo).FontSize(18).Bold();

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn();
                        c.ConstantColumn(80);
                        c.ConstantColumn(80);
                        c.ConstantColumn(80);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Text("Cliente").Bold();
                        h.Cell().Text("Quantità").Bold();
                        h.Cell().Text("Importo").Bold();
                        h.Cell().Text("Provvigione").Bold();
                    });

                    foreach (var r in _righe)
                    {
                        table.Cell().Text(r.NomeCliente);
                        table.Cell().Text(r.Quantita.ToString());
                        table.Cell().Text(r.Importo.ToString());
                        table.Cell().Text(r.ProvvigioneAgente.ToString());
                    }

                    table.Footer(f =>
                    {
                        f.Cell().Text("TOTALE").Bold();
                        f.Cell().Text(_righe.Sum(x => x.Quantita).ToString()).Bold();
                        f.Cell().Text(_righe.Sum(x => x.Importo).ToString()).Bold();
                        f.Cell().Text(_righe.Sum(x => x.ProvvigioneAgente).ToString()).Bold();
                    });
                });
            });
        }
    }
}
