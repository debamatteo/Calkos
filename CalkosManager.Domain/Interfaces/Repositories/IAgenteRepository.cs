using CalkosManager.Domain.Entities;
using System.Data;

namespace CalkosManager.Domain.Interfaces.Repositories
{
    public interface IAgenteRepository
    {
        List<Agente> GetAll();
        Agente GetById(int id);
        //void Insert(Agente agente);
        // ==== MODIFICA 05/11/2026 - INIZIO ====
        int Insert(Agente agente);   // prima: void. Restituisce l'IdAgente creato (0 se non disponibile)
          // ==== MODIFICA 05/11/2026 - FINE ====
        void Update(Agente agente);
        void Delete(int id);

        /// <summary>
        /// Verifica se Agente ha ordini o relazioni prima della cancellazione.
        /// </summary>
        bool HasOrdersOrRelations(int id);




    }

}


