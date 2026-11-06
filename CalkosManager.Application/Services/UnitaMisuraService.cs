using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;

namespace CalkosManager.Application.Services
{
    /// <summary>
    /// Servizio applicativo che gestisce la logica relativa alle Unità di Misura.
    /// Tutta la logica di business risiede qui, replicando esattamente lo stile di AgenteService.
    /// </summary>
    public class UnitaMisuraService
    {
        private readonly IUnitaMisuraRepository _repo;

        public UnitaMisuraService(IUnitaMisuraRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Ritorna tutte le unità di misura dal repository.
        /// </summary>
        public IEnumerable<UnitaMisura> GetAll()
        {
            return _repo.GetAll();
        }

        /// <summary>
        /// Ritorna un'unità di misura tramite il codice (es. "KG").
        /// </summary>
        public UnitaMisura? GetByCodice(string codice)
        {
            return _repo.GetByCodice(codice);
        }

        /// <summary>
        /// Eliminazione definitiva dell'unità di misura.
        /// Ritorna un oggetto anonimo per la gestione della risposta AJAX.
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
        /// Salvataggio centralizzato (Inserimento o Aggiornamento).
        /// </summary>
        public (bool Success, string Message) Save(UnitaMisura unita)
        {
            try
            {
                if (unita.IdUnitaMisura == 0)
                {
                    // Logica per nuovo inserimento
                    _repo.InsertManuale(unita);
                }
                else
                {
                    // Logica per aggiornamento record esistente
                    unita.DataModifica = DateTime.Now;
                    _repo.Update(unita);
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