using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;

namespace CalkosManager.Application.Services
{
    /// <summary>
    /// Service applicativo per la gestione dei Materiali.
    /// Incapsula la logica di business e delega l'accesso ai dati al repository.
    /// Non utilizza metodi transazionali: questi sono gestiti solo nei flussi di importazione.
    /// </summary>
    public class MaterialeService
    {
        private readonly IMaterialeRepository _materialeRepository;

        /// <summary>
        /// Crea una nuova istanza di <see cref="MaterialeService"/>.
        /// </summary>
        /// <param name="materialeRepository">Repository per l'accesso ai dati dei materiali.</param>
        public MaterialeService(IMaterialeRepository materialeRepository)
        {
            _materialeRepository = materialeRepository;
        }

        /// <summary>
        /// Restituisce l'elenco completo dei materiali.
        /// Usato per popolare liste e dropdown.
        /// </summary>
        public IEnumerable<Materiale> GetAll()
        {
            return _materialeRepository.GetAll();
        }

        /// <summary>
        /// Restituisce un materiale tramite ID.
        /// </summary>
        public Materiale? GetById(int id)
        {
            if (id <= 0)
                return null;

            return _materialeRepository.GetById(id);
        }

        /// <summary>
        /// Restituisce un materiale tramite codice univoco.
        /// Usato per controlli di esistenza e lookup.
        /// </summary>
        public Materiale? GetByCodice(string codice)
        {
            if (string.IsNullOrWhiteSpace(codice))
                return null;

            return _materialeRepository.GetByCodice(codice);
        }

        /// <summary>
        /// Restituisce un materiale tramite descrizione esatta.
        /// </summary>
        public Materiale? GetByDescrizione(string descrizione)
        {
            if (string.IsNullOrWhiteSpace(descrizione))
                return null;

            return _materialeRepository.GetByDescrizione(descrizione);
        }

        /// <summary>
        /// Salva un materiale (inserimento o aggiornamento).
        /// Per il CRUD manuale usa SEMPRE Insert/Update del repository.
        /// </summary>
        public void Save(Materiale materiale)
        {
            if (materiale == null)
                throw new ArgumentNullException(nameof(materiale));

            // --- INSERIMENTO ---
            if (materiale.IdMateriale == 0)
            {
                // Verifica se esiste già un materiale con lo stesso codice
                var esistente = _materialeRepository.GetByCodice(materiale.CodiceMateriale);

                if (esistente != null)
                {
                    // Se esiste già, aggiorniamo i dati
                    MappaDatiMateriale(materiale, esistente);
                    _materialeRepository.Update(esistente);

                    materiale.IdMateriale = esistente.IdMateriale;
                }
                else
                {
                    // Inserimento nuovo
                    _materialeRepository.Insert(materiale);
                }
            }
            // --- AGGIORNAMENTO ---
            else
            {
                _materialeRepository.Update(materiale);
            }
        }

        /// <summary>
        /// Helper privato per copiare i dati dal modello del form al record esistente.
        /// </summary>
        private void MappaDatiMateriale(Materiale sorgente, Materiale destinazione)
        {
            destinazione.CodiceMateriale = sorgente.CodiceMateriale;
            destinazione.DescrizioneMateriale = sorgente.DescrizioneMateriale;
            destinazione.Utente = sorgente.Utente;
        }

        /// <summary>
        /// Elimina un materiale tramite ID.
        /// </summary>
        public void Delete(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Id materiale non valido.", nameof(id));

            _materialeRepository.Delete(id);
        }
    }
}
