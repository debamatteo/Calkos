using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;

namespace CalkosManager.Application.Services
{
    /// <summary>
    /// Servizio applicativo che gestisce la logica relativa alle Dimensioni Materiali.
    /// Tutta la logica di business risiede qui, non nel controller.
    /// Il controller deve rimanere snello e delegare tutto al service.
    /// </summary>
    public class MaterialiDimensioniService
    {
        private readonly IMaterialiDimensioniRepository _repo;

        /// <summary>
        /// Il service riceve il repository tramite Dependency Injection.
        /// </summary>
        public MaterialiDimensioniService(IMaterialiDimensioniRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Ritorna tutte le dimensioni.
        /// Restituisce ENTITY, non DTO (coerente con l'architettura).
        /// </summary>
        public IEnumerable<MaterialiDimensioni> GetAll()
        {
            return _repo.GetAll();
        }

        /// <summary>
        /// Ritorna una singola dimensione tramite ID.
        /// Usato per la pagina Edit.
        /// </summary>
        public MaterialiDimensioni? GetById(int id)
        {
            return _repo.GetById(id);
        }

        /// <summary>
        /// Eliminazione definitiva.
        /// Restituisce un oggetto anonimo compatibile con AJAX.
        /// </summary>
        public object Delete(int id)
        {
            try
            {
                _repo.Delete(id);
                return new { success = true, message = "" };
            }
            catch (Exception ex)
            {
                return new { success = false, message = ex.Message };
            }
        }

        /// <summary>
        /// Salvataggio (Create + Update).
        /// La logica è identica agli altri moduli del progetto.
        /// </summary>
        public (bool Success, string Message) Save(MaterialiDimensioni model)
        {
            try
            {
                if (model.IdDimensione == 0)
                {
                    // Inserimento
                    _repo.Insert(model);
                }
                else
                {
                    // Aggiornamento
                    // Replica fedele della logica di tracciamento del modello
                    model.DataModifica = DateTime.Now;
                    _repo.Update(model);
                }

                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}