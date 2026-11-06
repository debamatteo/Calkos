using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;//19/10/2026

namespace CalkosManager.Application.Services
{
    // ============================================================
    // 26/10/2026 ESITO DEL SALVATAGGIO CON CONTROLLO DUPLICATI
    // Il controller non deve dedurre cosa e' successo da bool/stringhe: legge l'esito.
    // ============================================================
    public enum EsitoSalvataggioProvvigione
    {
        /// <summary>Nessun conflitto: inserita o aggiornata normalmente.</summary>
        Salvata,
        /// <summary>Esisteva gia' la stessa combinazione e l'utente ha confermato: aggiornata quella esistente.</summary>
        Sovrascritta,
        /// <summary>Esiste gia' la combinazione e manca la conferma: NIENTE e' stato scritto, serve il popup.</summary>
        DuplicatoDaConfermare,
        /// <summary>Errore (anche: modifica che collide con un altro record).</summary>
        Errore
    }

    public record RisultatoSalvataggioProvvigione(
        EsitoSalvataggioProvvigione Esito,
        string Message = "",
        ProvvigioniAgenti? Duplicato = null);

    /// <summary>
    /// Servizio applicativo che gestisce la logica relativa alle Provvigioni Agenti.
    /// Tutta la logica di business risiede qui, non nel controller.
    /// Il controller deve rimanere snello e delegare tutto al service.
    /// </summary>
    public class ProvvigioniAgentiService
    {
        private readonly IProvvigioniAgentiRepository _repo;
        private readonly ILogger<ProvvigioniAgentiService> _logger;//19/10/2026
        // ==== MODIFICA 04/11/2026 - INIZIO ====
        private readonly IMandatarioRepository _mandatarioRepo;
        // ==== MODIFICA 04/11/2026 - FINE ====

        /// <summary>
        /// Il service riceve il repository tramite Dependency Injection.
        /// </summary>
        // ==== MODIFICA 04/11/2026 - INIZIO ====
        // Aggiunto IMandatarioRepository: serve a creare in automatico il legame Cliente-Mandatario
        public ProvvigioniAgentiService(
            IProvvigioniAgentiRepository repo,
            ILogger<ProvvigioniAgentiService> logger,
            IMandatarioRepository mandatarioRepo)
        {
            _repo = repo;
            _logger = logger;
            _mandatarioRepo = mandatarioRepo;
        }
        // ==== MODIFICA 04/11/2026 - FINE ====
        //public ProvvigioniAgentiService(IProvvigioniAgentiRepository repo, ILogger<ProvvigioniAgentiService> logger)//19/10/2026
        //{
        //    _repo = repo;
        //    _logger = logger;
        //}

        /// <summary>
        /// Ritorna tutte le provvigioni.
        /// Restituisce ENTITY, non DTO (coerente con l'architettura).
        /// </summary>
        public IEnumerable<ProvvigioniAgenti> GetAll()
        {
            return _repo.GetAll();
        }

        /// <summary>
        /// Ritorna una singola provvigione tramite ID.
        /// Usato per la pagina Edit.
        /// </summary>
        public ProvvigioniAgenti? GetById(int id)
        {
            return _repo.GetById(id);
        }

        /// <summary>
        /// Eliminazione definitiva.
        /// Restituisce un oggetto anonimo compatibile con AJAX.
        /// </summary>
        public object Delete(int id)
        {
            try
            {
                _repo.Delete(id);
                return new { success = true, message = "" };
            }
            catch (Exception ex)
            {   //19/10/2026
                _logger.LogError(ex, "Errore eliminazione provvigione {IdProvvigione}", id);
                return new { success = false, message = "Operazione non riuscita. Controlla i dati o contatta l'assistenza." };
            }
        }

        /// <summary>
        /// Salvataggio (Create + Update) SENZA controllo duplicati.
        /// 26/10/2026 Il controller ora usa <see cref="SalvaConControlloDuplicati"/>; resta per compatibilita'.
        /// </summary>
        public (bool Success, string Message) Save(ProvvigioniAgenti model)
        {
            try
            {
                if (model.IdProvvigione == 0)
                {
                    // Inserimento
                    _repo.Insert(model);
                }
                else
                {
                    // Aggiornamento
                    model.DataModifica = DateTime.Now;
                    _repo.Update(model);
                }

                return (true, "");
            }
            catch (Exception ex)
            {   //19/10/2026
                _logger.LogError(ex, "Errore salvataggio provvigione {IdProvvigione}", model.IdProvvigione);
                return (false, "Operazione non riuscita. Controlla i dati o contatta l'assistenza.");
            }
        }

        // ============================================================
        // 26/10/2026 SALVATAGGIO CON CONTROLLO DUPLICATI (Mandatario - Agente - Cliente)
        //
        // Regola: per la stessa combinazione puo' esistere UNA sola provvigione.
        //  - nessun duplicato                      -> Insert/Update normale;
        //  - duplicato + nuovo record (Id = 0)     -> senza conferma: NON scrive e segnala il conflitto
        //                                             (il controller mostra il popup);
        //                                             con conferma: aggiorna il record esistente;
        //  - duplicato + modifica di un record     -> errore: sovrascrivere da una modifica significherebbe
        //                                             fondere due righe, scelta che non spetta a questo flusso.
        //
        // 04/11/2026 Dopo ogni salvataggio riuscito (nuovo, modifica o sovrascrittura) con Cliente indicato,
        //            viene creato il legame Cliente-Mandatario (MandatariClienti) se manca.
        // ============================================================
        public RisultatoSalvataggioProvvigione SalvaConControlloDuplicati(ProvvigioniAgenti model, bool confermaSovrascrittura)
        {
            try
            {
                // IdProvvigione escluso dal confronto: in modifica il record non deve confliggere con se stesso
                var duplicato = _repo.GetByCombinazione(model.IdAgente, model.IdMandatario, model.IdCliente, model.IdProvvigione);

                if (duplicato is null)
                {
                    var esito = Save(model);

                    // ==== MODIFICA 04/11/2026 - INIZIO ====
                    if (esito.Success)
                        AssicuraCollegamentoMandatarioCliente(model);
                    // ==== MODIFICA 04/11/2026 - FINE ====

                    return esito.Success
                        ? new RisultatoSalvataggioProvvigione(EsitoSalvataggioProvvigione.Salvata)
                        : new RisultatoSalvataggioProvvigione(EsitoSalvataggioProvvigione.Errore, esito.Message);
                }

                if (model.IdProvvigione != 0)
                {
                    return new RisultatoSalvataggioProvvigione(
                        EsitoSalvataggioProvvigione.Errore,
                        "Esiste già un'altra provvigione per la stessa combinazione Mandatario - Agente - Cliente. " +
                        "Modifica quella esistente oppure elimina uno dei due record.",
                        duplicato);
                }

                if (!confermaSovrascrittura)
                    return new RisultatoSalvataggioProvvigione(EsitoSalvataggioProvvigione.DuplicatoDaConfermare, Duplicato: duplicato);

                // Conferma ricevuta: aggiorno il record ESISTENTE (stessa combinazione, quindi stessi Id), cambia solo la percentuale
                _logger.LogInformation(
                    "Provvigione {IdProvvigione} sovrascritta: {Vecchia} -> {Nuova} (Agente {IdAgente}, Mandatario {IdMandatario}, Cliente {IdCliente}) da {Utente}",
                    duplicato.IdProvvigione, duplicato.Percentuale, model.Percentuale,
                    model.IdAgente, model.IdMandatario, model.IdCliente, model.Utente);

                duplicato.Percentuale = model.Percentuale;
                duplicato.Utente = model.Utente;
                duplicato.DataModifica = DateTime.Now;
                _repo.Update(duplicato);

                // ==== MODIFICA 04/11/2026 - INIZIO ====
                AssicuraCollegamentoMandatarioCliente(model);
                // ==== MODIFICA 04/11/2026 - FINE ====

                return new RisultatoSalvataggioProvvigione(EsitoSalvataggioProvvigione.Sovrascritta);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore salvataggio provvigione {IdProvvigione} (controllo duplicati)", model.IdProvvigione);
                return new RisultatoSalvataggioProvvigione(
                    EsitoSalvataggioProvvigione.Errore,
                    "Operazione non riuscita. Controlla i dati o contatta l'assistenza.");
            }
        }

        // ==== MODIFICA 04/11/2026 - INIZIO ====
        /// <summary>
        /// Dopo il salvataggio di una provvigione con Cliente, crea il legame Cliente-Mandatario
        /// (tabella MandatariClienti) se manca. Non crea doppioni e non toglie legami esistenti.
        /// Provvigione generica (senza Cliente): non fa nulla.
        /// Se la provvigione viene poi eliminata, il legame RESTA (si toglie da Mandatari > Associazione Clienti).
        /// </summary>
        private void AssicuraCollegamentoMandatarioCliente(ProvvigioniAgenti model)
        {
            if (!model.IdCliente.HasValue)
                return;

            _mandatarioRepo.InsertMandatarioClienteIfNotExists(model.IdMandatario, model.IdCliente.Value);
        }
        // ==== MODIFICA 04/11/2026 - FINE ====
    }
}
