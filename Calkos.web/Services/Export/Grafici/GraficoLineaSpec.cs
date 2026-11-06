using Calkos.web.Services.Export.Grafici;
using Humanizer;

namespace Calkos.web.Services.Export.Grafici
{
    /// <summary>
    /// Descrizione di un grafico a linee da inserire in un foglio.
    /// Posizione: rettangolo di celle 0-based (convenzione OpenXML delle ancore):
    /// ColonnaDa=0, RigaDa=10 significa "angolo in alto a sinistra sulla cella A11".
    /// </summary>
    /// //GraficoLineaSpec.cs:
    //    DTO Record immutabile che memorizza le specifiche di un singolo grafico:
    //    titolo, serie, riferimenti Excel delle categorie/valori, colore esadecimale
    //    e coordinate di ancoraggio 0-based (ColonnaDa, RigaDa, ColonnaA, RigaA).   
    public sealed record GraficoLineaSpec(
        string Titolo,
        string NomeSerie,
        string RiferimentoCategorie,   // es. ('Grafici'!$C$3,'Grafici'!$E$3)
        string RiferimentoValori,      // es. ('Grafici'!$C$5,'Grafici'!$E$5)
        string ColoreHex,              // es. "4472C4" (senza #)
        string FormatoNumeri,          // es. "#,##0" oppure "#,##0 \"€\""
        int ColonnaDa,
        int RigaDa,
        int ColonnaA,
        int RigaA);
}



