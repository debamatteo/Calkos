using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Humanizer;
using System.IO;
using System.Text;
using static QuestPDF.Helpers.Colors;

namespace Calkos.web.Services.Export.Grafici
{
    /// <summary>
    /// Aggiunge grafici Excel NATIVI a un file .xlsx già generato (es. da ClosedXML).
    /// Perché serve: ClosedXML non crea grafici. Il file viene quindi riaperto con
    /// l'Open XML SDK (DocumentFormat.OpenXml, già dipendenza di ClosedXML) e si
    /// aggiungono: 1 DrawingsPart per il foglio + 1 ChartPart per grafico.
    /// Va chiamato come ULTIMO passo: se il file venisse riaperto e salvato da
    /// ClosedXML, i grafici andrebbero persi.
    /// </summary>
    /// 

//    OpenXmlChartHelper.cs:
//•	Poiché ClosedXML non supporta la creazione nativa dei grafici, questa classe riapre lo stream del file tramite Open XML SDK e inietta il nodo<drawing> nel foglio, collegandovi la DrawingsPart e le varie ChartPart.
//•	Rispetta il rigoroso ordine di sequenza dello schema XML di OpenXML per prevenire messaggi di file danneggiato all'apertura in Excel. 


    public static class OpenXmlChartHelper
    {
        // Elementi che nello schema di <worksheet> seguono <drawing>:
        // <drawing> va inserito prima del primo di questi, altrimenti Excel segnala il file come danneggiato.
        private static readonly HashSet<string> ElementiSuccessiviADrawing = new()
        {
            "legacyDrawing", "legacyDrawingHF", "drawingHF", "picture", "oleObjects",
            "controls", "webPublishItems", "tableParts", "extLst"
        };

        public static byte[] AggiungiGrafici(byte[] fileXlsx, string nomeFoglio, IReadOnlyList<GraficoLineaSpec> grafici)
        {
            if (grafici.Count == 0) return fileXlsx;

            using var stream = new MemoryStream();
            stream.Write(fileXlsx, 0, fileXlsx.Length);   // stream espandibile: l'SDK deve poterlo riscrivere

            using (var documento = SpreadsheetDocument.Open(stream, true))
            {
                var workbookPart = documento.WorkbookPart
                    ?? throw new InvalidOperationException("File Excel senza WorkbookPart.");

                var foglio = workbookPart.Workbook.Descendants<Sheet>()
                    .FirstOrDefault(s => s.Name?.Value == nomeFoglio)
                    ?? throw new InvalidOperationException($"Foglio '{nomeFoglio}' non trovato.");

                var worksheetPart = (WorksheetPart)workbookPart.GetPartById(foglio.Id!.Value!);

                if (worksheetPart.DrawingsPart != null)
                    throw new InvalidOperationException($"Il foglio '{nomeFoglio}' contiene già oggetti grafici: caso non gestito.");

                // 1) Parte "disegni" del foglio + una parte per ogni grafico (Id relazione scelti da noi)
                var drawingsPart = worksheetPart.AddNewPart<DrawingsPart>();
                var ancore = new StringBuilder();

                for (int i = 0; i < grafici.Count; i++)
                {
                    string chartRelId = $"rIdGrafico{i + 1}";
                    var chartPart = drawingsPart.AddNewPart<ChartPart>(chartRelId);
                    ScriviXml(chartPart, ChartXmlTemplates.GraficoLinea(grafici[i]));
                    ancore.Append(ChartXmlTemplates.Ancora(grafici[i], chartRelId, idForma: i + 2));
                }

                ScriviXml(drawingsPart, ChartXmlTemplates.Drawing(ancore.ToString()));

                // 2) Collegamento foglio -> disegni: elemento <drawing r:id="..."/> nella posizione prevista dallo schema
                var worksheet = worksheetPart.Worksheet;
                var drawing = new Drawing { Id = worksheetPart.GetIdOfPart(drawingsPart) };
                var successivo = worksheet.ChildElements.FirstOrDefault(e => ElementiSuccessiviADrawing.Contains(e.LocalName));

                if (successivo != null) worksheet.InsertBefore(drawing, successivo);
                else worksheet.AppendChild(drawing);

                worksheet.Save();
            } // Dispose: l'SDK scrive il pacchetto aggiornato nello stream

            return stream.ToArray();
        }

        private static void ScriviXml(OpenXmlPart parte, string xml)
        {
            using var contenuto = new MemoryStream(new UTF8Encoding(false).GetBytes(xml));
            parte.FeedData(contenuto);
        }
    }
}
