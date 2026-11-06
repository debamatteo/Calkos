using System.Collections.Generic;
using CalkosManager.Domain.Entities;

namespace CalkosManager.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Lettura aggregata della merce consegnata tramite DDT per tutti i mandatari.
    /// Contratto nel Domain, implementazione ADO.NET in Infrastructure.
    /// </summary>
    public interface IReportMerceConsegnataRepository
    {
        /// <summary>
        /// Restituisce l'elenco aggregato della merce consegnata in base ai filtri specificati.
        /// </summary>
        IReadOnlyList<RigaMerceConsegnataDdtDto> GetReport(FiltriMerceConsegnataDdtDto filtri);
    }
}