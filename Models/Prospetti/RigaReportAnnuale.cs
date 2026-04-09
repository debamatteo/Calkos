namespace Calkos.web.Models.Prospetti
{
    /// <summary>
    /// Rappresenta la riga restituita dalla stored procedure spGeneraProspettoAnnualeExcel_Cliente.
    /// I nomi delle proprietà devono corrispondere esattamente ai nomi delle colonne della query SQL.
    /// </summary>
    public class RigaReportAnnuale
    {
        public string Cliente { get; set; }
        public decimal NrPezzi { get; set; }
        public decimal PrezzoBase { get; set; }
        public decimal PrezzoPraticato { get; set; }
        public decimal CommissionePezzo { get; set; }
        public decimal TotaleCommissioni { get; set; }
    }
}