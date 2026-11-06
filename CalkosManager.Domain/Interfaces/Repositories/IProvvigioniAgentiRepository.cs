using CalkosManager.Domain.Entities;
using System.Collections.Generic;

namespace CalkosManager.Domain.Interfaces.Repositories
{
    public interface IProvvigioniAgentiRepository
    {
        List<ProvvigioniAgenti> GetAll();
        ProvvigioniAgenti? GetById(int id);

        /// <summary>
        /// 26/10/2026 Cerca una provvigione gia' presente per la combinazione Agente - Mandatario - Cliente.
        /// IdCliente null = provvigione generica del mandatario (vale per tutti i clienti): anche null viene
        /// confrontato come valore (null = null), non ignorato.
        /// </summary>
        /// <param name="escludiIdProvvigione">Id da escludere dal confronto (il record che si sta modificando); 0 = nessuna esclusione.</param>
        /// <returns>Il record esistente (il piu' vecchio, se per errore ce ne fossero piu' d'uno) oppure null.</returns>
        ProvvigioniAgenti? GetByCombinazione(int idAgente, int idMandatario, int? idCliente, int escludiIdProvvigione = 0);

        void Insert(ProvvigioniAgenti provvigione);
        void Update(ProvvigioniAgenti provvigione);
        void Delete(int id);
    }
}
