using CalkosManager.Domain.Entities;

namespace CalkosManager.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Repository per la gestione dei Mandatari.
    /// Include:
    /// - CRUD del Mandatario
    /// - Gestione relazione Mandatario ↔ Cliente
    /// </summary>
    public interface IMandatarioRepository
    {
        /// <summary>
        /// Restituisce l'elenco completo dei Mandatari.
        /// Usato nella pagina Index.
        /// </summary>
        IEnumerable<Mandatario> GetAll();

        /// <summary>
        /// Restituisce un singolo Mandatario tramite ID.
        /// Usato nella pagina Edit.
        /// </summary>
        Mandatario? GetById(int id);

        /// <summary>
        /// Inserisce un nuovo Mandatario.
        /// </summary>
        void Insert(Mandatario mandatario);

        /// <summary>
        /// Aggiorna i dati di un Mandatario esistente.
        /// </summary>
        void Update(Mandatario mandatario);

        /// <summary>
        /// Elimina un Mandatario tramite ID.
        /// </summary>
        void Delete(int id);


        // ============================================================
        //      SEZIONE RELAZIONE MANDATARIO ↔ CLIENTE
        // ============================================================

        /// <summary>
        /// Restituisce tutti i Clienti associati a un Mandatario.
        /// Usato nella View Edit per popolare la tabella AJAX.
        /// </summary>
        IEnumerable<Cliente> GetByMandatario(int idMandatario);

        /// <summary>
        /// Aggiunge un Cliente al Mandatario SOLO se non già presente.
        /// Usato dal pulsante "Aggiungi Cliente" (AJAX).
        /// </summary>
        void InsertMandatarioClienteIfNotExists(int idMandatario, int idCliente);

        /// <summary>
        /// Rimuove un Cliente associato al Mandatario.
        /// Usato dal pulsante "Rimuovi" nella tabella (AJAX).
        /// </summary>
        void DeleteMandatarioCliente(int idMandatario, int idCliente);
        /// <summary>
        ///controlla se cancellazione poissibile 
        /// </summary>
        int CountRelazioniClienti(int idMandatario);
        /// <summary>
        ///tipo pagamento default del mandatario
        /// </summary>
        int GetTipoPagamento(int idMandatario);
    }
}
