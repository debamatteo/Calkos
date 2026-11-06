using CalkosManager.Application.DTO.Statistiche;
using CalkosManager.Domain.Entities;
using ClosedXML.Excel;

namespace Calkos.web.Services.Export
{
    /// <summary>
    /// Export Excel del Report Merce Consegnata DDT (05/10/2026, v2 06/10/2026).
    /// Riceve DTO già calcolati dal service: qui NON si accede al database.
    /// Le colonne sono quelle delle dimensioni scelte dall'utente (stesso raggruppamento della tabella a video).
    /// I totali sono calcolati per Unità di Misura: mai sommare unità diverse.
    /// </summary>
    public class ReportMerceConsegnataExcelBuilder
    {
        private const int RigaTitolo = 1;
        private const int RigaFiltri = 2;
        private const int RigaIntestazione = 3;
        private const int PrimaRigaDati = 4;

        // Formato scelto in base al valore: "#,##0.###" su un intero mostrerebbe il separatore decimale finale ("5,")
        private static string FormatoNumero(decimal v) => v == decimal.Truncate(v) ? "#,##0" : "#,##0.###";
        private const string TestoNonDisponibile = "n.d.";

        private static readonly XLColor ColoreIntestazione = XLColor.LightGray;
        private static readonly XLColor ColoreRigaTotaleUnita = XLColor.FromHtml("#C6E0B4");

        private enum Col { Mandatario, Cliente, Materiale, Anno, Mese, Diametro, Spessore, Larghezza, Quantita, UnitaMisura }

        public byte[] CreaReportMerceConsegnata(IReadOnlyList<RigaMerceConsegnataDdtDto> dati, FiltriMerceConsegnataDdtDto filtri)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Merce Consegnata DDT");

            var colonne = CostruisciColonne(filtri);

            ScriviTitolo(ws, "REPORT MERCE CONSEGNATA DDT");
            ScriviRiepilogoFiltri(ws, filtri);
            ScriviIntestazioni(ws, colonne);

            int riga = PrimaRigaDati;
            foreach (var r in dati)
            {
                for (int i = 0; i < colonne.Count; i++)
                    ScriviCella(ws.Cell(riga, i + 1), colonne[i], r);

                ws.Range(riga, 1, riga, colonne.Count).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                riga++;
            }

            int ultimaRigaDati = riga - 1;
            ScriviBloccoTotali(ws, riga, ultimaRigaDati, dati, colonne);

            Rifinisci(ws, colonne);
            return Salva(wb);
        }

        // =====================================================================
        // COLONNE (dipendono dal raggruppamento scelto)
        // =====================================================================
        private static List<Col> CostruisciColonne(FiltriMerceConsegnataDdtDto f)
        {
            var c = new List<Col>();
            if (f.RaggruppaPerMandatario) c.Add(Col.Mandatario);
            if (f.RaggruppaPerCliente) c.Add(Col.Cliente);
            if (f.RaggruppaPerMateriale) c.Add(Col.Materiale);
            if (f.RaggruppaPerAnno) c.Add(Col.Anno);
            if (f.RaggruppaPerMese) c.Add(Col.Mese);
            if (f.RaggruppaPerMisure)
            {
                c.Add(Col.Diametro);
                c.Add(Col.Spessore);
                c.Add(Col.Larghezza);
            }
            c.Add(Col.Quantita);
            c.Add(Col.UnitaMisura);
            return c;
        }

        private static string Intestazione(Col c) => c switch
        {
            Col.Mandatario => "Mandatario",
            Col.Cliente => "Cliente",
            Col.Materiale => "Materiale",
            Col.Anno => "Anno",
            Col.Mese => "Mese",
            Col.Diametro => "Diametro (mm)",
            Col.Spessore => "Spessore (mm)",
            Col.Larghezza => "Larghezza (mm)",
            Col.Quantita => "Quantità Totale",
            _ => "U.M."
        };

        private static void ScriviCella(IXLCell cella, Col col, RigaMerceConsegnataDdtDto r)
        {
            switch (col)
            {
                case Col.Mandatario: cella.Value = r.Mandatario ?? string.Empty; break;
                case Col.Cliente: cella.Value = r.Cliente ?? string.Empty; break;
                case Col.Materiale: cella.Value = r.Materiale ?? string.Empty; break;
                case Col.Anno: cella.Value = r.Anno; break;
                case Col.Mese: cella.Value = r.Mese; break;
                case Col.Diametro: ScriviMisura(cella, r.Diametro); break;
                case Col.Spessore: ScriviMisura(cella, r.Spessore); break;
                case Col.Larghezza: ScriviMisura(cella, r.Larghezza); break;
                case Col.Quantita:
                    cella.Value = r.QuantitaTotale;
                    cella.Style.NumberFormat.Format = FormatoNumero(r.QuantitaTotale);
                    break;
                case Col.UnitaMisura:
                    cella.Value = r.UnitaMisura ?? string.Empty;
                    cella.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    break;
            }
        }

        private static void ScriviMisura(IXLCell cella, decimal? valore)
        {
            if (valore.HasValue)
            {
                cella.Value = valore.Value;
                cella.Style.NumberFormat.Format = FormatoNumero(valore.Value);
            }
            else
            {
                cella.Value = TestoNonDisponibile;
                cella.Style.Font.SetFontColor(XLColor.Gray);
                cella.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
            }
        }

        // =====================================================================
        // METODI DI SCRITTURA CONDIVISI
        // =====================================================================
        private static void ScriviTitolo(IXLWorksheet ws, string titolo)
        {
            ws.Cell(RigaTitolo, 1).Value = titolo;
            ws.Cell(RigaTitolo, 1).Style.Font.SetBold().Font.SetFontSize(14);
        }

        /// <summary>Riga 2: filtri applicati ed elenco delle dimensioni di raggruppamento.</summary>
        private static void ScriviRiepilogoFiltri(IXLWorksheet ws, FiltriMerceConsegnataDdtDto f)
        {
            var parti = new List<string>();
            if (f.AnnoDa.HasValue && f.AnnoA.HasValue)
                parti.Add(f.AnnoDa == f.AnnoA ? $"Anno: {f.AnnoDa}" : $"Anni: {f.AnnoDa}-{f.AnnoA}");
            else if (f.AnnoDa.HasValue)
                parti.Add($"Dall'anno: {f.AnnoDa}");
            else if (f.AnnoA.HasValue)
                parti.Add($"Fino all'anno: {f.AnnoA}");
            if (f.Mese.HasValue) parti.Add($"Mese: {f.Mese}");
            if (f.Diametro.HasValue) parti.Add($"Diametro: {f.Diametro:0.###}");
            if (f.Spessore.HasValue) parti.Add($"Spessore: {f.Spessore:0.###}");
            if (f.Larghezza.HasValue) parti.Add($"Larghezza: {f.Larghezza:0.###}");

            var dimensioni = new List<string>();
            if (f.RaggruppaPerMandatario) dimensioni.Add("Mandatario");
            if (f.RaggruppaPerCliente) dimensioni.Add("Cliente");
            if (f.RaggruppaPerMateriale) dimensioni.Add("Materiale");
            if (f.RaggruppaPerAnno) dimensioni.Add("Anno");
            if (f.RaggruppaPerMese) dimensioni.Add("Mese");
            if (f.RaggruppaPerMisure) dimensioni.Add("Misure");

            string filtriTesto = parti.Count > 0 ? string.Join("  |  ", parti) : "nessun filtro su anno/mese/misure";
            string raggruppaTesto = dimensioni.Count > 0 ? string.Join(", ", dimensioni) : "totale complessivo";

            ws.Cell(RigaFiltri, 1).Value = $"Filtri: {filtriTesto}   —   Raggruppato per: {raggruppaTesto}";
            ws.Cell(RigaFiltri, 1).Style.Font.SetItalic().Font.SetFontColor(XLColor.DimGray);
        }

        private static void ScriviIntestazioni(IXLWorksheet ws, List<Col> colonne)
        {
            for (int i = 0; i < colonne.Count; i++)
            {
                var cella = ws.Cell(RigaIntestazione, i + 1);
                cella.Value = Intestazione(colonne[i]);
                cella.Style
                    .Font.SetBold()
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Fill.SetBackgroundColor(ColoreIntestazione)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }
        }

        private static void ScriviBloccoTotali(IXLWorksheet ws, int rigaInizioTotali, int ultimaRigaDati,
            IReadOnlyList<RigaMerceConsegnataDdtDto> dati, List<Col> colonne)
        {
            var unitaDistinte = dati.Select(d => d.UnitaMisura).Where(u => !string.IsNullOrEmpty(u)).Distinct().ToList();

            // Quantità e U.M. sono sempre le ultime due colonne
            int colQta = colonne.IndexOf(Col.Quantita) + 1;
            int colUm = colonne.IndexOf(Col.UnitaMisura) + 1;

            int rigaCorrente = rigaInizioTotali;

            // Totale per ciascuna Unità di Misura presente
            foreach (var um in unitaDistinte)
            {
                if (colQta > 1)
                {
                    ws.Cell(rigaCorrente, 1).Value = $"TOTALE {um}";
                    var etichetta = colQta > 2 ? ws.Range(rigaCorrente, 1, rigaCorrente, colQta - 1).Merge() : ws.Range(rigaCorrente, 1, rigaCorrente, 1);
                    etichetta.Style
                        .Font.SetBold()
                        .Fill.SetBackgroundColor(ColoreRigaTotaleUnita)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                }

                // Valore calcolato qui (non formula): resta corretto anche in anteprima / vista protetta
                decimal totale = dati.Where(d => d.UnitaMisura == um).Sum(d => d.QuantitaTotale);
                var cQta = ws.Cell(rigaCorrente, colQta);
                cQta.Value = totale;
                cQta.Style.NumberFormat.Format = FormatoNumero(totale);
                cQta.Style.Font.SetBold().Fill.SetBackgroundColor(ColoreRigaTotaleUnita).Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                var cUm = ws.Cell(rigaCorrente, colUm);
                cUm.Value = um;
                cUm.Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                   .Fill.SetBackgroundColor(ColoreRigaTotaleUnita).Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                rigaCorrente++;
            }
        }

        private static void Rifinisci(IXLWorksheet ws, List<Col> colonne)
        {
            // Larghezza calcolata dall'intestazione in giù: il titolo e il riepilogo filtri non allargano la colonna A
            int ultimaRiga = ws.LastRowUsed()?.RowNumber() ?? RigaIntestazione;
            ws.Columns().AdjustToContents(RigaIntestazione, ultimaRiga);

            for (int i = 0; i < colonne.Count; i++)
            {
                if (colonne[i] == Col.Mandatario || colonne[i] == Col.Cliente)
                    ws.Column(i + 1).Width = 25;
                else if (colonne[i] == Col.Materiale)
                    ws.Column(i + 1).Width = 20;
                else if (ws.Column(i + 1).Width < 12)
                    ws.Column(i + 1).Width = 12;
            }

            ws.SheetView.Freeze(RigaIntestazione, 0);
        }

        private static byte[] Salva(XLWorkbook wb)
        {
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }
    }
}
