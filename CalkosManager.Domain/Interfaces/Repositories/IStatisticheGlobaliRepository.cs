using CalkosManager.Domain.Entities;

namespace CalkosManager.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Lettura aggregata del fatturato di TUTTI i mandatari (Statistiche Globali).
    /// Contratto nel Domain, implementazione ADO.NET in Infrastructure.
    /// </summary>
    public interface IStatisticheGlobaliRepository
    {
        /// <summary>Totali mensili per mandatario, solo ordini fatturati, anni inclusi.</summary>
        IReadOnlyList<FatturatoMensileMandatario> GetFatturatoMensile(int annoDa, int annoA);

        /// <summary>Anni con almeno un ordine fatturato, dal più recente.</summary>
        IReadOnlyList<int> GetAnniDisponibili();
    }
}