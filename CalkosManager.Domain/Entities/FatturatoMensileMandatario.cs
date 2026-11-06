namespace CalkosManager.Domain.Entities;

/// <summary>
/// Rappresenta la riga "lunga" restituita da spStatisticheGlobali_Fatturato.
/// Corrisponde all'aggregato mensile di un mandatario per un dato anno/mese.
/// </summary>
public sealed class FatturatoMensileMandatario
{
    public int IdMandatario { get; set; }
    public int Anno { get; set; }
    public int Mese { get; set; }
    public decimal Quantita { get; set; }
    public decimal EuroProvvigione { get; set; }
    public string UnitaMisura { get; set; } = string.Empty;
}