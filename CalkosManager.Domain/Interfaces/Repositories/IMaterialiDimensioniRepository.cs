using CalkosManager.Domain.Entities;
using System.Collections.Generic;

namespace CalkosManager.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Interfaccia del repository per la gestione delle Dimensioni dei Materiali.
    /// Definisce le operazioni CRUD esposte all'application layer.
    /// </summary>
    public interface IMaterialiDimensioniRepository
    {
        /// <summary>
        /// Restituisce tutte le dimensioni presenti nel sistema.
        /// </summary>
        List<MaterialiDimensioni> GetAll();

        /// <summary>
        /// Restituisce una singola dimensione tramite ID.
        /// </summary>
        MaterialiDimensioni? GetById(int id);

        /// <summary>
        /// Inserisce una nuova dimensione.
        /// </summary>
        void Insert(MaterialiDimensioni item);

        /// <summary>
        /// Aggiorna una dimensione esistente.
        /// </summary>
        void Update(MaterialiDimensioni item);

        /// <summary>
        /// Elimina definitivamente una dimensione.
        /// </summary>
        void Delete(int id);
    }
}
