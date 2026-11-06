using CalkosManager.Domain.Entities;

namespace CalkosManager.Domain.Interfaces.Repositories
{
    public interface ITipoPagamentoRepository
    {
        List<TipoPagamento> GetAll();
        TipoPagamento? GetById(int id);
        void Insert(TipoPagamento tipoPagamento);
        void Update(TipoPagamento tipoPagamento);
        void Delete(int id);
    }
}
