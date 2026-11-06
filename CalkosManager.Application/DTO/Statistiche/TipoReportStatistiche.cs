using System.ComponentModel.DataAnnotations;

namespace CalkosManager.Application.DTO.Statistiche
{
    /// <summary>Report disponibili nella vista "Statistiche Globali".</summary>
    public enum TipoReportStatistiche
    {
        [Display(Name = "Report Fatturato Mandatari - Singolo Anno")]
        FatturatoAnnuale = 1,

        [Display(Name = "Report Fatturato - Confronto Totali Annuali")]
        ConfrontoAnni = 2
    }
}
