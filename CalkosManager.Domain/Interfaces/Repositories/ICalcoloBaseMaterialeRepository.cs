using CalkosManager.Domain.Entities;

namespace CalkosManager.Domain.Interfaces.Repositories
{
    public interface ICalcoloBaseMaterialeRepository
    {
        /// <summary>Ultime N quotazioni per un materiale (default 4), per lo storico in modale.</summary>
        List<CalcoloBaseMateriale> GetStorico(int idMateriale, int top = 4);

        /// <summary>Ultimo prezzo attivo per un materiale — usato dall'automatismo al cambio Materiale.</summary>
        CalcoloBaseMateriale? GetUltimoAttivo(int idMateriale);

        CalcoloBaseMateriale? GetById(int id);

        /// <summary>Inserisce una nuova quotazione e ne restituisce l'Id generato.</summary>
        int Insert(CalcoloBaseMateriale item);

        /// <summary>
        /// Elimina un CalcoloBaseMateriale SE E SOLO SE non è referenziato da alcun
        /// ProspettoDeAngeli.IdCalcoloBase. Verifica e cancellazione sono atomiche
        /// (vedi spCalcoloBaseMateriale_Delete) per evitare race condition.
        /// </summary>
        (bool Eliminato, string Messaggio) Delete(int id);//28/08/2026

    }
}