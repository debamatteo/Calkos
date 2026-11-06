using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;

namespace CalkosManager.Application.Services
{
    /// <summary>
    /// Servizio applicativo che gestisce i dati operativi del prospetto De Angeli.
    /// Speculare a Cobral per garantire coerenza nella Dashboard.
    /// </summary>
    public class ProspettoDeAngeliService
    {
        private readonly IProspettoDeAngeliRepository _prospettoRepo;

        public ProspettoDeAngeliService(IProspettoDeAngeliRepository prospettoRepo)
        {
            _prospettoRepo = prospettoRepo;
        }

        public void InserisciProspettoIniziale(IEnumerable<ProspettoDeAngeli> righe, string utente)
        {
            _prospettoRepo.InsertRange(righe, utente);
        }

        public IEnumerable<ProspettoDeAngeli> GetByFile(int idFileImportato)
        {
            return _prospettoRepo.GetByFile(idFileImportato);
        }

        public ProspettoDeAngeli? GetById(int id)
        {
            return _prospettoRepo.GetById(id);
        }

        public void Inserisci(ProspettoDeAngeli item, string utente)
        {
            _prospettoRepo.Insert(item, utente);
        }

        public void Aggiorna(ProspettoDeAngeli item, string utente)
        {
            _prospettoRepo.Update(item, utente);
        }

        public void Elimina(int idProspettoDeAngeli)
        {
            _prospettoRepo.Delete(idProspettoDeAngeli);
        }

        public void EliminaInSicurezza(int id, string utente)
        {
            var riga = _prospettoRepo.GetById(id);
            if (riga == null) throw new Exception("Record non trovato.");

            // BLOCCO FISCALE
            if (riga.Fatturata == 1)
            {
                throw new InvalidOperationException("Blocco Fiscale: Impossibile eliminare una riga già fatturata.");
            }

            _prospettoRepo.SoftDelete(id, utente);
        }

        public void Ripristina(int id, string utente)
        {
            _prospettoRepo.Ripristina(id, utente);
        }

        public void Duplica(int idProspettoDeAngeli, string utente)
        {
            var originale = _prospettoRepo.GetById(idProspettoDeAngeli);
            if (originale == null) return;

            // Assicurati che l'entity ProspettoDeAngeli abbia il metodo CloneForInsert
            var copia = originale.CloneForInsert(utente);
            _prospettoRepo.Insert(copia, utente);
        }

        public IEnumerable<ProspettoDeAngeli> GetFiltrati(int? idMandatario, int? idFile, int? anno, int? mese, int? fatturata, bool mostraEliminati = false)
        {
            return _prospettoRepo.GetFiltrati(idMandatario, idFile, anno, mese, fatturata, mostraEliminati);
        }
    }
}