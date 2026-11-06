using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;

namespace CalkosManager.Application.Services
{
    /// <summary>
    /// Servizio applicativo che gestisce la logica relativa ai Mandatari.
    /// Non contiene SQL: usa il repository tramite interfaccia.
    /// Contiene la logica di business, validazioni e gestione errori.
    /// </summary>
    public class MandatarioService
    {
        private readonly IMandatarioRepository _mandatarioRepository;

        /// <summary>
        /// Il servizio riceve il repository tramite Dependency Injection.
        /// </summary>
        public MandatarioService(IMandatarioRepository mandatarioRepository)
        {
            _mandatarioRepository = mandatarioRepository;
        }

        // ============================================================
        //                      CRUD PRINCIPALE
        // ============================================================

        /// <summary>
        /// Restituisce l'elenco completo dei Mandatari.
        /// </summary>
        public IEnumerable<Mandatario> GetAll()
        {
            return _mandatarioRepository.GetAll();
        }

        /// <summary>
        /// Restituisce un Mandatario tramite ID.
        /// </summary>
        public Mandatario? GetById(int id)
        {
            return _mandatarioRepository.GetById(id);
        }

        /// <summary>
        /// Inserisce un nuovo Mandatario.
        /// Restituisce un oggetto risultato con Success/Message.
        /// </summary>
        public (bool Success, string Message) Insert(Mandatario model)
        {

            // ==== MODIFICA 29/10/2026 - INIZIO ====
            var validazione = ValidaPercDelta(model);
            if (!validazione.Success) return validazione;
            // ==== MODIFICA 29/10/2026 - FINE ====


            // ==== MODIFICA 01/11/2026 - INIZIO ====
            validazione = ValidaQuotaCentesimiPerKg(model);
            if (!validazione.Success) return validazione;
            // ==== MODIFICA 01/11/2026 - FINE ====

            try
            {
                _mandatarioRepository.Insert(model);
                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }


        /// <summary>
        /// GTipoPagamento di default del  un Mandatario esistente.
        /// </summary>
        public int GetTipoPagamento(int idMandatario)
        {
            return _mandatarioRepository.GetTipoPagamento(idMandatario);
        }



        /// <summary>
        /// Aggiorna un Mandatario esistente.
        /// Restituisce un oggetto risultato con Success/Message.
        /// </summary>
        public (bool Success, string Message) Update(Mandatario model)
        {
            // ==== MODIFICA 29/10/2026 - INIZIO ====
            var validazione = ValidaPercDelta(model);
            if (!validazione.Success) return validazione;
            // ==== MODIFICA 29/10/2026 - FINE ====

            // ==== MODIFICA 01/11/2026 - INIZIO ====
            validazione = ValidaQuotaCentesimiPerKg(model);
            if (!validazione.Success) return validazione;
            // ==== MODIFICA 01/11/2026 - FINE ====
            try
            {
                model.DataModifica = DateTime.Now;
                _mandatarioRepository.Update(model);
                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Elimina un Mandatario tramite ID.
        /// Restituisce un oggetto risultato con Success/Message.
        /// </summary>
        public (bool Success, string Message) Delete(int id)
        {
            try
            {
                _mandatarioRepository.Delete(id);
                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        // ============================================================
        //      SEZIONE RELAZIONE MANDATARIO ↔ CLIENTE
        // ============================================================
        // Come richiesto, la logica AJAX rimane nel controller.
        // Qui forniamo solo metodi di supporto per evitare duplicazioni.

        /// <summary>
        /// Restituisce i clienti associati a un Mandatario.
        /// </summary>
        public IEnumerable<Cliente> GetClienti(int idMandatario)
        {
            return _mandatarioRepository.GetByMandatario(idMandatario);
        }

        /// <summary>
        /// Aggiunge un cliente al Mandatario solo se non già presente.
        /// </summary>
        public void AddCliente(int idMandatario, int idCliente)
        {
            _mandatarioRepository.InsertMandatarioClienteIfNotExists(idMandatario, idCliente);
        }

        /// <summary>
        /// Rimuove un cliente associato al Mandatario.
        /// </summary>
        public void RemoveCliente(int idMandatario, int idCliente)
        {
            _mandatarioRepository.DeleteMandatarioCliente(idMandatario, idCliente);
        }

        /// <summary>
        /// Verifica se il Mandatario ha clienti associati prima della cancellazione.
        /// </summary>
        public (bool CanDelete, string Message) CheckRelazioni(int idMandatario)
        {
            int count = _mandatarioRepository.CountRelazioniClienti(idMandatario);

            if (count > 0)
            {
                return (false, $"Impossibile eliminare: il mandatario ha {count} clienti associati.");
            }

            return (true, "");
        }

        // ==== MODIFICA 29/10/2026 - INIZIO ====
        private static (bool Success, string Message) ValidaPercDelta(Mandatario model) =>
            model.Perc_Delta_Provvigione is < 0 or > 100
                ? (false, "Perc. Delta Provvigione non valida: inserire un valore tra 0 e 100 (es. 40 = 40%).")
                : (true, "");
        // ==== MODIFICA 29/10/2026 - FINE ====

        // ==== MODIFICA 01/11/2026 - INIZIO ====
        private static (bool Success, string Message) ValidaQuotaCentesimiPerKg(Mandatario model) =>
            model.QuotaCentesimiPerKg is < 0
                ? (false, "Quota Euro cent/Kg non valida: inserire un valore maggiore o uguale a 0.")
                : (true, "");
        // ==== MODIFICA 01/11  /2026 - FINE ====
    }
}
