using CalkosManager.Application.DTO.Statistiche;
using CalkosManager.Application.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace Calkos.web.Models.Statistiche
{
    /// <summary>
    /// Modello della vista Statistiche Globali: filtri inviati dal form (GET)
    /// + dati di supporto per le dropdown.
    /// La validazione delega a StatisticheGlobaliService.ValidaRichiesta (regole in un solo punto).
    /// </summary>
    public class StatisticheGlobaliViewModel : IValidatableObject
    {
        [Display(Name = "Tipo Report")]
        public TipoReportStatistiche? TipoReport { get; set; }

        // Report 1
        [Display(Name = "Anno")]
        public int? Anno { get; set; }

        // Report 2
        [Display(Name = "Dall'anno")]
        public int? AnnoDa { get; set; }

        [Display(Name = "All'anno")]
        public int? AnnoA { get; set; }

        /// <summary>Anni con dati fatturati (dal più recente). Non arriva dal form.</summary>
        [BindNever]
        public IReadOnlyList<int> AnniDisponibili { get; set; } = Array.Empty<int>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var errori = StatisticheGlobaliService.ValidaRichiesta(
                TipoReport, Anno, AnnoDa, AnnoA, StatisticheGlobaliService.AnnoMassimoConsentito());

            // Errori a livello di modello: compaiono nel riepilogo in cima al form
            return errori.Select(e => new ValidationResult(e));
        }
    }
}
