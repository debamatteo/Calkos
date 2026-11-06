using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using System;
using System.Collections.Generic;

namespace CalkosManager.Application.Services
{
    /// <summary>
    /// Servizio applicativo TipoPagamento.
    /// Pulito e allineato al 100% con la logica di AgenteService.
    /// </summary>
    public class TipoPagamentoService
    {
        private readonly ITipoPagamentoRepository _repo;

        public TipoPagamentoService(ITipoPagamentoRepository repo)
        {
            _repo = repo;
        }

        public IEnumerable<TipoPagamento> GetAll()
        {
            return _repo.GetAll();
        }

        public TipoPagamento? GetById(int id)
        {
            return _repo.GetById(id);
        }

        /// <summary>
        /// Sostituisce Insert e Update con un unico punto di ingresso come in Agenti.
        /// </summary>
        public (bool Success, string Message) Save(TipoPagamento entity)
        {
            try
            {
                if (entity.IdTipoPagamento == 0)
                {
                    _repo.Insert(entity);
                }
                else
                {
                    entity.DataModifica = DateTime.Now;
                    _repo.Update(entity);
                }
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        ///// <summary>
        ///// Verifica se il tipo pagamento è usato (es. in Ordini) prima di eliminarlo.
        ///// </summary>
        //public object CheckRelazioni(int id)
        //{
        //    bool hasRelations = _repo.HasRelations(id);

        //    return hasRelations
        //        ? new { canDelete = false, message = "Il tipo di pagamento è già utilizzato e non può essere rimosso." }
        //        : new { canDelete = true };
        //}

        /// <summary>
        /// Eliminazione gestita per chiamate Ajax.
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
    }
}