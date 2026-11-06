using CalkosManager.Domain.Entities;

using CalkosManager.Domain.Interfaces.Repositories;

namespace CalkosManager.Application.Services
{
    /// <summary>
    /// Servizio applicativo che gestisce i dati operativi del prospetto CST.
    /// Qui NON c'è SQL: tutta la logica di accesso ai dati è delegata al repository.
    /// </summary>
    public class ProspettoCSTService
    {
        private readonly IProspettoCSTRepository _prospettoRepo;

        /// <summary>
        /// Il servizio riceve il repository tramite Dependency Injection.
        /// Questo permette di sostituire facilmente l'implementazione (es. mock nei test).
        /// </summary>
        public ProspettoCSTService(IProspettoCSTRepository prospettoRepo)
        {
            _prospettoRepo = prospettoRepo;
        }

        /// <summary>
        /// Inserisce tutte le righe iniziali del prospetto (copiate dal file Excel).
        /// </summary>
        //public void InserisciProspettoIniziale(IEnumerable<ProspettoCST> righe)
        //{
        //    //_prospettoRepo.InsertRange(righe);
        //    _prospettoRepo.InsertRange(righe, conn, tran);

        //}
        public void InserisciProspettoIniziale(IEnumerable<ProspettoCST> righe, string utente)
        {
            _prospettoRepo.InsertRange(righe,utente);
        }

        /// <summary>
        /// Ritorna tutte le righe del prospetto per un file importato.
        /// </summary>
        public IEnumerable<ProspettoCST> GetByFile(int idFileImportato)
        {
            return _prospettoRepo.GetByFile(idFileImportato);
        }

        /// <summary>
        /// Ritorna una singola riga del prospetto tramite ID.
        /// Utile per la pagina di modifica.
        /// </summary>
        public ProspettoCST? GetById(int id)
        {
            return _prospettoRepo.GetById(id);
        }

        /// <summary>
        /// Inserisce una nuova riga (es. un DDT aggiunto manualmente).
        /// </summary>
        public void Inserisci(ProspettoCST item, string utente)
        {
            _prospettoRepo.Insert(item,  utente);
        }

        /// <summary>
        /// Aggiorna una riga esistente del prospetto.
        /// </summary>
        public void Aggiorna(ProspettoCST item, string utente)
        {
            _prospettoRepo.Update(item,  utente);
        }

        /// <summary>
        /// Cancella una riga del prospetto.
        /// </summary>
        public void Elimina(int idProspettoCST)
        {
            _prospettoRepo.Delete(idProspettoCST);
        }
        public void EliminaInSicurezza(int id, string utente)
        {
            // 1. Recupero la riga per controllare lo stato
            var riga = _prospettoRepo.GetById(id);
            if (riga == null) throw new Exception("Record non trovato.");

            // 2. BLOCCO FISCALE: Controllo se è già stata fatturata
            if (riga.Fatturata == 1)
            {
                throw new InvalidOperationException("Blocco Fiscale: Impossibile eliminare una riga già fatturata. Rimuovere prima lo stato di fatturazione.");
            }

            // 3. Eseguo la Soft Delete tramite Repository
            _prospettoRepo.SoftDelete(id, utente);
        }

        public void Ripristina(int id, string utente)
        {
            // Qui non serve il controllo sulla fatturazione perché 
            // riportare in vita un dato non rompe i vincoli fiscali.
            _prospettoRepo.Ripristina(id, utente);
        }
        /// <summary>
        /// Duplica una riga del prospetto.
        /// La logica è applicativa: il repository non deve sapere cosa significa "duplicare".
        /// </summary>
        /// 

        public void Duplica(int idProspettoCST, string utente)
        {
            var originale = _prospettoRepo.GetById(idProspettoCST);

            if (originale == null)
                return;

            // Usiamo il metodo centralizzato della Entity che resetta anche 
            // IsDeleted, Fatturata e i riferimenti ai file importati.
            var copia = originale.CloneForInsert(utente);

            // Inseriamo la copia
            _prospettoRepo.Insert(copia, utente);
        }
        
    }
}
