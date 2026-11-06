using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;

using CalkosManager.Application.DTOs;
namespace CalkosManager.Application.Services
{
    /// <summary>
    /// Service applicativo per la gestione dei Clienti.
    /// Incapsula la logica di business e delega l'accesso ai dati al repository.
    /// NON tocca i metodi transazionali usati dall'import COBRAL.
    /// </summary>
    public class ClienteService
    {
        private readonly IClienteRepository _clienteRepository;

        /// <summary>
        /// Crea una nuova istanza di <see cref="ClienteService"/>.
        /// </summary>
        /// <param name="clienteRepository">Repository per l'accesso ai dati dei clienti.</param>
        public ClienteService(IClienteRepository clienteRepository)
        {
            _clienteRepository = clienteRepository;
        }

        /// <summary>
        /// Restituisce l'elenco dei clienti, con possibilità di filtrare per stato di attività.
        /// </summary>
        /// <param name="showDeleted">Se impostato a true, restituisce esclusivamente i clienti archiviati (Soft Deleted).</param>
        /// <param name="idAgente">
        /// 12/10/2026 Se valorizzato (utente con ruolo Agente) restituisce solo i clienti associati a quell'agente.
        /// Se null (Admin/Operatore) nessun filtro.
        /// 30/10/2026 Clienti attivi: agente di default OPPURE ProvvigioniAgenti (filtro applicato da spClienti_GetAll).
        /// </param>
        /// <returns>Una collezione di entità Cliente.</returns>
        public IEnumerable<Cliente> GetAll(bool showDeleted = false, int? idAgente = null) // 12/10/2026 aggiunto idAgente
        {
            // Delega al repository la scelta della Stored Procedure corretta (spClienti_GetAll o spClienti_GetDeleted)
            // 12/10/2026 il filtro per agente viene applicato direttamente a livello database
            return _clienteRepository.GetAll(showDeleted, idAgente);
        }
        /// <summary>
        /// Ripristina lo stato attivo di un cliente precedentemente disattivato.
        /// </summary>
        /// <param name="id">ID univoco del cliente da ripristinare.</param>
        /// <returns>True se l'operazione è stata completata con successo.</returns>
        public bool Restore(int id)
        {
            // Validazione formale dell'identificativo
            if (id <= 0) return false;

            // Chiamata alla Stored Procedure spClienti_Restore tramite repository
            return _clienteRepository.Restore(id);
        }
        /// <summary>
        /// Restituisce un cliente per Id, oppure null se non trovato.
        /// Usa la stored procedure spClienti_GetById tramite repository.
        /// </summary>
        public Cliente? GetById(int id)
        {
            if (id <= 0)
                return null;

            return _clienteRepository.GetById(id);
        }



        /// <summary>
        /// Recupera il Tipo Pagamento di default di un determinato cliente per la precompilazione del form ordine.
        /// 26/08/2026
        /// </summary>
        /// <param name="idCliente">Identificativo univoco del cliente.</param>
        /// <returns>DTO contenente ID, Codice e Descrizione del pagamento di default, oppure null se non impostato.</returns>
        public TipoPagamentoDefaultDto? GetTipoPagamentoDefault(int idCliente)
        {
            if (idCliente <= 0)
                return null;

            return _clienteRepository.GetTipoPagamentoDefault(idCliente);
        }



        /// <summary>
        /// Ricerca clienti per ragione sociale (autocomplete, Select2, ecc.).
        /// Usa SQL inline nel repository, NON transazionale, pensato per il CRUD/manuale.
        /// </summary>
        /// <param name="term">Testo da cercare (prefisso della ragione sociale).</param>
        /// <param name="maxResults">Numero massimo di risultati.</param>
        /// <param name="idAgente">12/10/2026 Se valorizzato (ruolo Agente) limita la ricerca ai clienti di quell'agente.</param>
        public IEnumerable<Cliente> SearchByRagioneSociale(string term, int maxResults = 20, int? idAgente = null) // 12/10/2026 aggiunto idAgente
        {
            if (string.IsNullOrWhiteSpace(term))
                return Enumerable.Empty<Cliente>();

            return _clienteRepository.SearchByRagioneSociale(term, maxResults, idAgente);
        }

        /// <summary>
        /// 12/10/2026 Verifica se un cliente è associato all'agente indicato (anche se archiviato).
        /// Usato dal controller per negare lettura/modifica/eliminazione/ripristino di clienti non propri.
        /// 30/10/2026 Verifica SOLO l'agente di default (Clienti.IdAgente): ora usato per eliminare/ripristinare.
        /// Per l'accesso in lettura/modifica usare IsClienteAccessibileAdAgente.
        /// </summary>
        public bool IsClienteDiAgente(int idCliente, int idAgente)
        {
            if (idCliente <= 0 || idAgente <= 0)
                return false;

            return _clienteRepository.IsClienteDiAgente(idCliente, idAgente);
        }

        // ==== MODIFICA 30/10/2026 - INIZIO ====
        /// <summary>
        /// 30/10/2026 True se l'agente può lavorare sul cliente: agente di default (Clienti.IdAgente)
        /// OPPURE presente in ProvvigioniAgenti. Con idMandatario valorizzato vale solo per quel mandatario.
        /// Fail-closed: id non validi (es. -1 = agente non collegato) restituiscono sempre false.
        /// </summary>
        public bool IsClienteAccessibileAdAgente(int idCliente, int idAgente, int? idMandatario = null)
        {
            if (idCliente <= 0 || idAgente <= 0)
                return false;

            return _clienteRepository.IsClienteAccessibileAdAgente(idCliente, idAgente, idMandatario);
        }
        // ==== MODIFICA 30/10/2026 - FINE ====

        // ==== MODIFICA 30/10/2026 - INIZIO ====
        /// <summary>
        /// 30/10/2026 Crea un cliente per conto di un Agente (tutto in una transazione, nella stored procedure):
        /// cliente + collegamento al mandatario + accordo di provvigione con la percentuale di default dell'agente.
        /// </summary>
        public EsitoCreazioneClienteDto CreaClienteDaAgente(Cliente cliente, int idAgente)
        {
            if (cliente == null)
                throw new ArgumentNullException(nameof(cliente));

            if (idAgente <= 0)
                return new EsitoCreazioneClienteDto { Esito = EsitoCreazioneCliente.AgenteNonValido };

            // Per l'agente il mandatario è obbligatorio (senza non comparirebbe nel dropdown degli ordini)
            if (!cliente.IdMandatario.HasValue)
                return new EsitoCreazioneClienteDto { Esito = EsitoCreazioneCliente.MandatarioNonValido };

            return _clienteRepository.InsertDaAgente(cliente, idAgente);
        }

        /// <summary>30/10/2026 Nomi simili per l'avviso durante la digitazione (minimo 3 caratteri).</summary>
        public List<ClienteSimileDto> GetSimili(string term, int? idAgente)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 3)
                return new List<ClienteSimileDto>();

            return _clienteRepository.GetSimili(term.Trim(), idAgente);
        }

        /// <summary>
        /// 30/10/2026 True se il cliente è stato creato da QUELL'agente (Clienti.IdAgenteCreatore).
        /// È il criterio per eliminare/ripristinare lato Agente.
        /// </summary>
        public bool IsClienteCreatoDaAgente(int idCliente, int idAgente)
        {
            if (idCliente <= 0 || idAgente <= 0)
                return false;

            return _clienteRepository.GetIdAgenteCreatore(idCliente) == idAgente;
        }

        /// <summary>30/10/2026 True se il cliente ha ordini o altre relazioni (usato per bloccare l'eliminazione dell'agente).</summary>
        public bool HasOrdersOrRelations(int idCliente)
        {
            if (idCliente <= 0)
                return false;

            return _clienteRepository.HasOrdersOrRelations(idCliente);
        }
        // ==== MODIFICA 30/10/2026 - FINE ====

        /// <summary>
        /// 14/10/2026 Restituisce il mandatario associato al cliente (MandatariClienti), o null se assente.
        /// Usato dal controller per pre-selezionare la DDL "Mandatario" in modifica.
        /// </summary>
        public int? GetIdMandatario(int idCliente)
        {
            if (idCliente <= 0)
                return null;

            return _clienteRepository.GetIdMandatarioByCliente(idCliente);
        }

        /// <summary>
        /// 14/10/2026 Allinea MandatariClienti al valore scelto nel form (cliente.IdMandatario).
        /// - Valore invariato rispetto al DB  -> nessuna scrittura (l'associazione si mantiene).
        /// - Valore cambiato                  -> sostituisce l'associazione esistente con la nuova.
        /// - Valore svuotato (null)           -> rimuove l'associazione.
        /// Va chiamato DOPO insert/update, quando cliente.IdCliente è valorizzato.
        /// </summary>
        private void SincronizzaMandatario(Cliente cliente)
        {
            if (cliente.IdCliente <= 0)
                return;

            var attuale = _clienteRepository.GetIdMandatarioByCliente(cliente.IdCliente);

            if (attuale == cliente.IdMandatario)
                return;

            _clienteRepository.SetMandatarioCliente(cliente.IdCliente, cliente.IdMandatario, cliente.Utente);
        }

        /// <summary>
        /// Salva un cliente (inserimento o aggiornamento).
        /// Per il CRUD manuale usa SEMPRE InsertManual/Update del repository.
        /// NON utilizza i metodi transazionali usati dall'import COBRAL.
        /// </summary>
        /// <param name="cliente">Cliente da salvare.</param>
        /// <param name="idAgenteLoggato">
        /// 12/10/2026 Valorizzato solo se l'utente ha ruolo Agente. Serve a impedire la "resurrezione"
        /// di un cliente archiviato che non appartiene all'agente (altrimenti se lo riassegnerebbe).
        /// Null per Admin/Operatore: comportamento invariato.
        /// </param>
        public void SalvaCliente(Cliente cliente, int? idAgenteLoggato = null) // 12/10/2026 aggiunto idAgenteLoggato
        {
            if (cliente == null)
                throw new ArgumentNullException(nameof(cliente));

            // --- LOGICA DI INSERIMENTO ---
            if (cliente.IdCliente == 0)
            {
                // 1. Prima di inserire, verifichiamo se esiste un cliente omonimo "nascosto" (IsDeleted = 1)
                // Usiamo GetByRagioneSociale che abbiamo aggiornato nel repository
                var esistente = _clienteRepository.GetByRagioneSociale(cliente.RagioneSociale);

                if (esistente != null && esistente.IsDeleted)
                {
                    // 12/10/2026 PROTEZIONE AGENTE: se l'utente è un Agente e il cliente archiviato omonimo
                    // NON è suo, blocchiamo l'operazione. Senza questo controllo la riattivazione
                    // (MappaDatiCliente copia IdAgente dal form) gli farebbe "rubare" il cliente di un altro agente,
                    // sovrascrivendone i dati.
                    // 30/10/2026 resta il confronto con l'agente di default: riattivare un archiviato è una
                    // operazione da "proprietario" (coerente con eliminazione/ripristino).
                    // ==== MODIFICA 30/10/2026 - INIZIO ====
                    // 30/10/2026 L'agente crea i clienti da CreaClienteDaAgente (non passa più di qui in inserimento);
                    // per sicurezza, se arrivasse qui, non può mai riattivare un cliente archiviato.
                    //if (idAgenteLoggato.HasValue && esistente.IdAgente != idAgenteLoggato.Value)
                    if (idAgenteLoggato.HasValue)
                    {
                        throw new InvalidOperationException(
                            "Esiste già un cliente archiviato con questa ragione sociale. " +
                            "Contatta l'amministratore.");
                    }
                    // ==== MODIFICA 30/10/2026 - FINE ====

                    // CASO RESURREZIONE: Il cliente esiste ma era stato eliminato.
                    // Lo riattiviamo e aggiorniamo i suoi dati con quelli nuovi del form.
                    esistente.IsDeleted = false;
                    MappaDatiCliente(cliente, esistente); // Copia i campi dal form al record del DB

                    // Usiamo l'Update (stored procedure spClienti_Update) come da tua architettura
                    _clienteRepository.Update(esistente);

                    // Riportiamo l'ID corretto nell'oggetto originale per il controller
                    cliente.IdCliente = esistente.IdCliente;
                }
                else
                {
                    // CASO NUOVO: Inserimento manuale tramite stored procedure spClienti_InsertManual
                    // incapsulata nel metodo InsertManual del repository.
                    _clienteRepository.InsertManual(cliente);
                }
            }
            // --- LOGICA DI AGGIORNAMENTO ---
            else
            {
                // Aggiornamento tramite stored procedure spClienti_Update
                _clienteRepository.Update(cliente);
            }

            // 14/10/2026 Associazione Mandatario: vale per insert, resurrezione e update.
            // IdCliente è già valorizzato in tutti e tre i casi. Se questo passo fallisce l'eccezione risale al
            // controller con cliente.IdCliente != 0: la view viene riproposta come "Edit" e un nuovo salvataggio
            // passa dal ramo update (nessun cliente duplicato).
            SincronizzaMandatario(cliente);
        }

        /// <summary>
        /// Helper privato per copiare i dati dal modello del form al record esistente nel DB.
        /// </summary>
        private void MappaDatiCliente(Cliente sorgente, Cliente destinazione)
        {
            destinazione.PartitaIva = sorgente.PartitaIva;
            destinazione.CodiceFiscale = sorgente.CodiceFiscale;
            destinazione.Indirizzo = sorgente.Indirizzo;
            destinazione.Citta = sorgente.Citta;
            destinazione.Provincia = sorgente.Provincia;
            destinazione.CAP = sorgente.CAP;
            destinazione.Email = sorgente.Email;
            destinazione.Telefono = sorgente.Telefono;
            destinazione.IdAgente = sorgente.IdAgente;
            destinazione.Utente = sorgente.Utente;
            destinazione.Note = sorgente.Note;
            // La RagioneSociale è già identica per via della ricerca

            //21/10/2026 Copia dei campi del referente
            destinazione.Referente = sorgente.Referente;
            destinazione.TelefonoReferente = sorgente.TelefonoReferente;
            destinazione.EmailReferente = sorgente.EmailReferente;
            destinazione.NoteReferente = sorgente.NoteReferente;









        }

        // Con la Soft Delete, il record NON viene rimosso fisicamente.
        //  avvisare se il cliente è "movimentato".

        public (bool canDelete, string message) CanDelete(int id)
        {
            if (id <= 0)
                return (false, "Id cliente non valido.");

            // Con la Soft Delete, il record NON viene rimosso fisicamente.
            // Quindi tecnicamente potremmo sempre farlo.
            // Ma è buona norma avvisare se il cliente è "movimentato".
            if (_clienteRepository.HasOrdersOrRelations(id))
            {
                // Restituiamo true perché la Soft Delete NON spacca i vincoli FK,
                // ma aggiungiamo un messaggio informativo.
                return (true, "Nota: Il cliente ha ordini associati. Verrà disattivato ma lo storico rimarrà salvo.");
            }

            return (true, string.Empty);
        }

        //public (bool canDelete, string message) CanDelete(int id)
        //{
        //    if (id <= 0)
        //        return (false, "Id cliente non valido.");

        //    // Verifichiamo se il cliente è usato in ordini o altre tabelle
        //    if (_clienteRepository.HasOrdersOrRelations(id))
        //        return (false, "Impossibile eliminare il cliente: sono presenti ordini associati.");

        //    return (true, string.Empty);
        //}





        /// <summary>
        /// Disattiva logicamente un cliente (Soft Delete).
        /// Il record rimane nel database per preservare l'integrità dei dati storici.
        /// </summary>
        public void Delete(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Id cliente non valido.", nameof(id));

            _clienteRepository.Delete(id);
        }
    }
}
