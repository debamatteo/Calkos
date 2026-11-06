using DocumentFormat.OpenXml.Drawing.Spreadsheet;
using Humanizer;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace Calkos.web.Services.Export.Grafici
{
    /// <summary>
    /// XML DrawingML dei grafici (chartSpace) e delle ancore (wsDr).
    /// Tenuto separato dall'SDK: sono stringhe pure, verificabili e testabili da sole.
    /// Ogni testo variabile passa da SecurityElement.Escape, che converte anche le
    /// virgolette: il formato "#,##0 "€"" senza escape renderebbe il file illeggibile.
    /// </summary>
    /// 
//      ChartXmlTemplates.cs:Contiene i template XML string-based del
//      markup DrawingML/OpenXML dei grafici Excel(c:chartSpace e xdr:twoCellAnchor).  
//      Sicurezza ed Escape: Ogni testo variabile passa da SecurityElement.
//      Escape per garantire che caratteri speciali
//      (come virgolette nei formati valuta) non danneggino la struttura XML.


    public static class ChartXmlTemplates
    {
        private const string NsC = "http://schemas.openxmlformats.org/drawingml/2006/chart";
        private const string NsA = "http://schemas.openxmlformats.org/drawingml/2006/main";
        private const string NsR = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private const string NsXdr = "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing";

        private static string E(string testo) => SecurityElement.Escape(testo) ?? "";

        /// <summary>Contenuto completo di una ChartPart: grafico a linee con una serie, etichette dati e titolo.</summary>
        public static string GraficoLinea(GraficoLineaSpec g) => $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<c:chartSpace xmlns:c=""{NsC}"" xmlns:a=""{NsA}"" xmlns:r=""{NsR}"">
  <c:roundedCorners val=""0""/>
  <c:chart>
    <c:title>
      <c:tx><c:rich><a:bodyPr/><a:lstStyle/><a:p><a:pPr><a:defRPr sz=""1200"" b=""1""/></a:pPr><a:r><a:rPr lang=""it-IT"" sz=""1200"" b=""1""><a:solidFill><a:srgbClr val=""{E(g.ColoreHex)}""/></a:solidFill></a:rPr><a:t>{E(g.Titolo)}</a:t></a:r></a:p></c:rich></c:tx>
      <c:overlay val=""0""/>
    </c:title>
    <c:autoTitleDeleted val=""0""/>
    <c:plotArea>
      <c:layout/>
      <c:lineChart>
        <c:grouping val=""standard""/>
        <c:varyColors val=""0""/>
        <c:ser>
          <c:idx val=""0""/>
          <c:order val=""0""/>
          <c:tx><c:v>{E(g.NomeSerie)}</c:v></c:tx>
          <c:spPr><a:ln w=""28575"" cap=""rnd""><a:solidFill><a:srgbClr val=""{E(g.ColoreHex)}""/></a:solidFill><a:round/></a:ln></c:spPr>
          <c:marker><c:symbol val=""circle""/><c:size val=""6""/><c:spPr><a:solidFill><a:srgbClr val=""{E(g.ColoreHex)}""/></a:solidFill></c:spPr></c:marker>
          <c:dLbls>
            <c:numFmt formatCode=""{E(g.FormatoNumeri)}"" sourceLinked=""0""/>
            <c:spPr><a:noFill/><a:ln><a:noFill/></a:ln></c:spPr>
            <c:txPr><a:bodyPr/><a:lstStyle/><a:p><a:pPr><a:defRPr sz=""800""/></a:pPr><a:endParaRPr lang=""it-IT""/></a:p></c:txPr>
            <c:dLblPos val=""t""/>
            <c:showLegendKey val=""0""/><c:showVal val=""1""/><c:showCatName val=""0""/><c:showSerName val=""0""/><c:showPercent val=""0""/><c:showBubbleSize val=""0""/>
          </c:dLbls>
          <c:cat><c:numRef><c:f>{E(g.RiferimentoCategorie)}</c:f></c:numRef></c:cat>
          <c:val><c:numRef><c:f>{E(g.RiferimentoValori)}</c:f></c:numRef></c:val>
          <c:smooth val=""0""/>
        </c:ser>
        <c:marker val=""1""/>
        <c:axId val=""500000001""/>
        <c:axId val=""500000002""/>
      </c:lineChart>
      <c:catAx>
        <c:axId val=""500000001""/>
        <c:scaling><c:orientation val=""minMax""/></c:scaling>
        <c:delete val=""0""/>
        <c:axPos val=""b""/>
        <c:numFmt formatCode=""0"" sourceLinked=""0""/>
        <c:majorTickMark val=""out""/>
        <c:minorTickMark val=""none""/>
        <c:tickLblPos val=""low""/>
        <c:crossAx val=""500000002""/>
        <c:crosses val=""autoZero""/>
        <c:auto val=""1""/>
        <c:lblAlgn val=""ctr""/>
        <c:lblOffset val=""100""/>
        <c:noMultiLvlLbl val=""0""/>
      </c:catAx>
      <c:valAx>
        <c:axId val=""500000002""/>
        <c:scaling><c:orientation val=""minMax""/></c:scaling>
        <c:delete val=""0""/>
        <c:axPos val=""l""/>
        <c:majorGridlines><c:spPr><a:ln w=""6350""><a:solidFill><a:srgbClr val=""D9D9D9""/></a:solidFill></a:ln></c:spPr></c:majorGridlines>
        <c:numFmt formatCode=""{E(g.FormatoNumeri)}"" sourceLinked=""0""/>
        <c:majorTickMark val=""out""/>
        <c:minorTickMark val=""none""/>
        <c:tickLblPos val=""nextTo""/>
        <c:crossAx val=""500000001""/>
        <c:crosses val=""autoZero""/>
        <c:crossBetween val=""between""/>
      </c:valAx>
    </c:plotArea>
    <c:plotVisOnly val=""1""/>
    <c:dispBlanksAs val=""gap""/>
  </c:chart>
</c:chartSpace>";

        /// <summary>Ancora (posizione nel foglio) di un grafico; chartRelId è l'Id della relazione DrawingsPart -> ChartPart.</summary>
        public static string Ancora(GraficoLineaSpec g, string chartRelId, int idForma) => $@"  <xdr:twoCellAnchor editAs=""oneCell"">
    <xdr:from><xdr:col>{g.ColonnaDa}</xdr:col><xdr:colOff>0</xdr:colOff><xdr:row>{g.RigaDa}</xdr:row><xdr:rowOff>0</xdr:rowOff></xdr:from>
    <xdr:to><xdr:col>{g.ColonnaA}</xdr:col><xdr:colOff>0</xdr:colOff><xdr:row>{g.RigaA}</xdr:row><xdr:rowOff>0</xdr:rowOff></xdr:to>
    <xdr:graphicFrame macro="""">
      <xdr:nvGraphicFramePr><xdr:cNvPr id=""{idForma}"" name=""Grafico {idForma}""/><xdr:cNvGraphicFramePr/></xdr:nvGraphicFramePr>
      <xdr:xfrm><a:off x=""0"" y=""0""/><a:ext cx=""0"" cy=""0""/></xdr:xfrm>
      <a:graphic><a:graphicData uri=""{NsC}""><c:chart r:id=""{E(chartRelId)}""/></a:graphicData></a:graphic>
    </xdr:graphicFrame>
    <xdr:clientData/>
  </xdr:twoCellAnchor>
";

        /// <summary>Contenuto completo della DrawingsPart del foglio (contenitore delle ancore).</summary>
        public static string Drawing(string ancore) => $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<xdr:wsDr xmlns:xdr=""{NsXdr}"" xmlns:a=""{NsA}"" xmlns:r=""{NsR}"" xmlns:c=""{NsC}"">
{ancore}</xdr:wsDr>";
    }
}
