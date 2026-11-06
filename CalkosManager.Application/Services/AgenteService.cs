using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;//19/10/2026

namespace CalkosManager.Application.Services
{ //19/10/2026
    public class AgenteService
    {
        private const string ErroreGenerico = "Operazione non riuscita. Controlla i dati o contatta l'assistenza.";

        private readonly IAgenteRepository _repo;
        private readonly ILogger<AgenteService> _logger;//19/10/2026

        public AgenteService(IAgenteRepository repo, ILogger<AgenteService> logger)//19/10/2026
        {
            _repo = repo;
            _logger = logger;
        }

        public IEnumerable<Agente> GetAll() => _repo.GetAll();

        public Agente? GetById(int id) => _repo.GetById(id);

        public object CheckRelazioni(int id)
        {
            bool hasRelations = _repo.HasOrdersOrRelations(id);

            return hasRelations
                ? new { canDelete = false, message = "L'agente ha ordini in corso e non può essere rimosso." }
                : new { canDelete = true };
        }

        /// <summary>Eliminazione definitiva. Il controllo delle relazioni è fatto QUI, non solo dalla pagina.</summary>
        public object Delete(int id)
        {
            try
            {
                if (_repo.HasOrdersOrRelations(id))
                    return new { success = false, message = "L'agente ha ordini in corso e non può essere rimosso." };

                _repo.Delete(id);
                return new { success = true, message = "" };
            }
            catch (Exception ex)
            {   //19/10/2026
                // Il dettaglio resta nei log; all'utente non arrivano nomi di tabelle o vincoli
                _logger.LogError(ex, "Errore eliminazione agente {IdAgente}", id);
                return new { success = false, message = ErroreGenerico };
            }
        }

        public (bool Success, string Message) Save(Agente agente)
        {
            try
            {
                if (agente.IdAgente == 0)
                {
                    // 05/11/2026 L'Id generato dal database viene scritto nell'oggetto: il controller lo legge da lì
                    agente.IdAgente = _repo.Insert(agente);
                }
                else
                {
                    agente.DataModifica = DateTime.Now;
                    _repo.Update(agente);
                }

                return (true, "");
            }
            catch (Exception ex)
            {   //19/10/2026
                _logger.LogError(ex, "Errore salvataggio agente {IdAgente}", agente.IdAgente);
                return (false, ErroreGenerico);
            }
        }
    }
}
