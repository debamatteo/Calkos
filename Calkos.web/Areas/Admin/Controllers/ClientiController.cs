using Calkos.Web.Identity;
using CalkosManager.Application.DTOs;
using CalkosManager.Application.Services;
using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Calkos.web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Operatore,Agente")]
    public class ClientiController : Controller
    {
        // Service applicativo per la gestione dei clienti
        private readonly ClienteService _clienteService;
        private readonly TipoPagamentoService _tipoPagamentoService;//26/08/2026


        // 14/10/2026 Elenco mandatari per la DDL (sola lettura, nessun filtro per ruolo)
        private readonly MandatarioService _mandatarioService;

        // ==== MODIFICA 30/10/2026 - INIZIO ====
        // 30/10/2026 Clienti.IdAgente è dismesso (non più visibile né modificabile nelle maschere):
        // la DDL "Agente" non esiste più, quindi il controller non usa più IAgenteRepository.
        //private readonly IAgenteRepository _agenteRepository;
        // ==== MODIFICA 30/10/2026 - FINE ====


        private readonly ILogger<ClientiController> _logger;//21/10/2026

        /// <summary>
        /// Crea una nuova istanza di <see cref="ClientiController"/>.
        /// </summary>
        public ClientiController(
            ClienteService clienteService,
            //IAgenteRepository agenteRepository,   // 30/10/2026 non più usato
            TipoPagamentoService tipoPagamentoService,
            MandatarioService mandatarioService // 14/10/2026
            , ILogger<ClientiController> logger) // <-- Aggiunto qui

        {
            _clienteService = clienteService;
            //_agenteRepository = agenteRepository;   // 30/10/2026 non più usato
            _tipoPagamentoService = tipoPagamentoService; //26/08/2026
            _mandatarioService = mandatarioService; // 14/10/2026
            _logger = logger; // <-- Salvato qui
        }

        // ============================================================
        // 12/10/2026 HELPER ISOLAMENTO PER AGENTE
        // ============================================================

        /// <summary>
        /// 12/10/2026 Restituisce l'IdAgente da usare come filtro sui dati dei clienti.
        /// - Admin / Operatore: null  -> nessun filtro, vedono tutti i clienti.
        /// - Agente collegato (AspNetUsers.IdAgente valorizzato): il suo IdAgente.
        /// - Agente NON collegato a nessun record Agenti: -1. Valore volutamente "impossibile":
        ///   nessun cliente ha IdAgente = -1, quindi non vede e non può toccare nulla (fail-closed).
        ///   Senza questa guardia un Agente senza collegamento avrebbe filtro null e vedrebbe TUTTI i clienti.
        /// </summary>
        private int? GetIdAgenteFiltro()
        {
            if (!User.IsInRole("Agente"))
                return null;

            int? idAgenteLoggato = User.GetIdAgente();

            return (idAgenteLoggato.HasValue && idAgenteLoggato.Value > 0)
                ? idAgenteLoggato.Value
                : -1;
        }

        /// <summary>
        /// 12/10/2026 True se l'utente corrente può accedere al cliente indicato:
        /// sempre per Admin/Operatore, solo se il cliente è associato al proprio agente per il ruolo Agente.
        /// La verifica è fatta sul DATABASE (non sul modello postato dal client), quindi non è manomettibile dal form.
        /// 30/10/2026 "Associato" = agente di default (Clienti.IdAgente) OPPURE riga in ProvvigioniAgenti
        /// (qualsiasi mandatario). Prima contava solo Clienti.IdAgente.
        /// </summary>
        private bool HaAccessoAlCliente(int idCliente, int? idAgenteFiltro)
        {
            if (!idAgenteFiltro.HasValue)
                return true; // Admin / Operatore

            // ==== MODIFICA 30/10/2026 - INIZIO ====
            //return _clienteService.IsClienteDiAgente(idCliente, idAgenteFiltro.Value);   // 30/10/2026 sostituita dalla riga sotto
            return _clienteService.IsClienteAccessibileAdAgente(idCliente, idAgenteFiltro.Value);
            // ==== MODIFICA 30/10/2026 - FINE ====
        }

        // ==== MODIFICA 30/10/2026 - INIZIO ====
        /// <summary>
        /// 30/10/2026 True se l'utente corrente può ELIMINARE / RIPRISTINARE il cliente:
        /// sempre per Admin/Operatore; per il ruolo Agente solo se il cliente è stato CREATO da lui
        /// (Clienti.IdAgenteCreatore). Un cliente assegnato dall'amministratore si può aprire e modificare,
        /// ma non archiviare. Per l'eliminazione vale in più il blocco "niente ordini" (vedi CheckRelazioni/Delete).
        /// </summary>
        private bool PuoGestireArchivio(int idCliente, int? idAgenteFiltro)
        {
            if (!idAgenteFiltro.HasValue)
                return true; // Admin / Operatore

            //return _clienteService.IsClienteDiAgente(idCliente, idAgenteFiltro.Value);   // 30/10/2026 sostituita (prima versione)
            return _clienteService.IsClienteCreatoDaAgente(idCliente, idAgenteFiltro.Value);
        }

        /// <summary>
        /// 30/10/2026 Carica le liste della maschera cliente (Tipo Pagamento e Mandatario) e i flag per la view.
        /// Sostituisce il codice che era duplicato in Create, Edit e nei tre rami di errore di Save.
        /// </summary>
        /// <param name="mandatarioBloccato">True per l'Agente in modifica: il mandatario non si cambia dopo la creazione.</param>
        /// <param name="mandatarioObbligatorio">True per l'Agente in creazione: senza mandatario il cliente non comparirebbe negli ordini.</param>
        private void CaricaLookupCliente(Cliente cliente, bool mandatarioBloccato, bool mandatarioObbligatorio)
        {
            var tipopagamento = _tipoPagamentoService.GetAll() ?? Enumerable.Empty<TipoPagamento>();
            ViewBag.TipoPagamento = new SelectList(tipopagamento, "IdTipoPagamento", "DescrizioneTipoPagamento", cliente.IdTipoPagamentoDefault);

            CaricaDropdownMandatari(cliente.IdMandatario);

            ViewBag.MandatarioBloccato = mandatarioBloccato;
            ViewBag.MandatarioObbligatorio = mandatarioObbligatorio;
        }
        // ==== MODIFICA 30/10/2026 - FINE ====

        // ==== MODIFICA 30/10/2026 - INIZIO ====
        // 30/10/2026 RIMOSSA CaricaDropdownAgenti: la DDL "Agente" (Clienti.IdAgente) non esiste più nelle maschere.
        //private void CaricaDropdownAgenti(int? idAgenteSelezionato, int? idAgenteBloccato) { ... }
        // ==== MODIFICA 30/10/2026 - FINE ====

        /// <summary>
        /// 14/10/2026 Popola ViewBag.Mandatari per la DDL "Mandatario".
        /// NESSUN filtro per ruolo o utente: Admin, Operatore e Agente vedono l'elenco completo.
        /// (Si usa direttamente il MandatarioService: l'[Authorize] di MandatariController non entra in gioco.)
        /// </summary>
        private void CaricaDropdownMandatari(int? idMandatarioSelezionato)
        {
            var mandatari = (_mandatarioService.GetAll() ?? Enumerable.Empty<Mandatario>())
                .OrderBy(m => m.NomeMandatario)
                .Select(m => new
                {
                    m.IdMandatario,
                    Testo = string.IsNullOrWhiteSpace(m.CodiceMandatario)
                        ? m.NomeMandatario
                        : $"{m.NomeMandatario} ({m.CodiceMandatario})"
                });

            ViewBag.Mandatari = new SelectList(mandatari, "IdMandatario", "Testo", idMandatarioSelezionato);
        }

        // --- LISTA CLIENTI ---
        /// <summary>
        /// Visualizza l'elenco dei clienti.
        /// </summary>
        /// <param name="showDeleted">Se true, mostra i clienti eliminati logicamente (Cestino).</param>
        public IActionResult Index(bool showDeleted = false)
        {
            // 12/10/2026 Isolamento per Agente: l'Agente vede (anche nel Cestino) solo i propri clienti
            // 30/10/2026 Elenco attivi: agente di default + ProvvigioniAgenti; Cestino: solo i clienti creati dall'agente
            var idAgenteFiltro = GetIdAgenteFiltro();

            // Passiamo il parametro al service per filtrare i dati
            var model = _clienteService.GetAll(showDeleted, idAgenteFiltro); // 12/10/2026 aggiunto idAgenteFiltro

            // Usiamo il ViewBag per dire alla View se stiamo guardando il cestino o meno
            // utile per cambiare titoli e icone nell'HTML
            ViewBag.IsTrashView = showDeleted;

            return View(model);
        }

        // --- RESTORE ---
        /// <summary>
        /// Ripristina un cliente archiviato. Chiamata via AJAX.
        /// </summary>
        [HttpPost]
        // [ValidateAntiForgeryToken] -> RIMOSSO: Gestito globalmente da Program.cs
        public IActionResult Restore(int id)
        {
            try
            {
                // 12/10/2026 PROTEZIONE AGENTE: non può ripristinare clienti non associati a lui
                // 30/10/2026 solo se l'ha creato lui (PuoGestireArchivio)
                //if (!HaAccessoAlCliente(id, GetIdAgenteFiltro()))
                if (!PuoGestireArchivio(id, GetIdAgenteFiltro()))
                    return Json(new { success = false, message = "Non sei autorizzato a operare su questo cliente." });

                // Chiamata al service per eseguire la procedura di ripristino
                bool success = _clienteService.Restore(id);

                if (success)
                    return Json(new { success = true });
                else
                    return Json(new { success = false, message = "Impossibile trovare il cliente da ripristinare." });
            }
            catch (Exception ex)
            {
                //return Json(new { success = false, message = ex.Message });
                return Json(new { success = false, message = MessaggioErrore(ex, nameof(Restore)) });//21/10/2026 log e messaggio generico per Agente
            }
        }

        // --- CHECK RELAZIONI ---
        // Chiamata via AJAX dalla modale JS prima della cancellazione.
        // Impedisce di spaccare il database se ci sono ordini legati al cliente.
        [HttpGet]
        public IActionResult CheckRelazioni(int id)
        {
            var idAgenteFiltro = GetIdAgenteFiltro();   // 30/10/2026

            // 12/10/2026 PROTEZIONE AGENTE: niente informazioni (né possibilità di procedere) su clienti non propri
            // 30/10/2026 solo se l'ha creato lui (PuoGestireArchivio)
            //if (!HaAccessoAlCliente(id, GetIdAgenteFiltro()))
            if (!PuoGestireArchivio(id, idAgenteFiltro))
            {
                return Json(new
                {
                    canDelete = false,
                    message = "Non sei autorizzato a operare su questo cliente."
                });
            }

            // ==== MODIFICA 30/10/2026 - INIZIO ====
            // L'Agente non può eliminare un cliente che ha ordini (Admin/Operatore: solo avviso, come prima)
            if (idAgenteFiltro.HasValue && _clienteService.HasOrdersOrRelations(id))
            {
                return Json(new
                {
                    canDelete = false,
                    message = "Il cliente ha ordini associati: non puoi eliminarlo. Contatta l'amministratore."
                });
            }
            // ==== MODIFICA 30/10/2026 - FINE ====

            // Recuperiamo la tupla dal Service.
            // Ora 'canDelete' sarà quasi sempre TRUE perché la Soft Delete non rompe i vincoli SQL.
            // 'message' conterrà l'avviso se il cliente ha ordini storici.
            var (canDelete, message) = _clienteService.CanDelete(id);

            // RESTITUIAMO SEMPRE UN OGGETTO JSON
            return Json(new
            {
                // Indica al JavaScript se procedere con la richiesta di conferma
                canDelete = canDelete,

                // Passiamo il messaggio generato dal Service.
                message = !string.IsNullOrWhiteSpace(message)
                          ? message
                          : "Sei sicuro di voler procedere con l'operazione?"
            });
        }

        // --- DELETE ---
        // Eliminazione definitiva. Viene chiamata solo dopo il check positivo della modale.
        [HttpPost]
        // [ValidateAntiForgeryToken] -> RIMOSSO: Gestito globalmente da Program.cs
        public IActionResult Delete(int id)
        {
            try
            {
                var idAgenteFiltro = GetIdAgenteFiltro();   // 30/10/2026

                // 12/10/2026 PROTEZIONE AGENTE: non può eliminare clienti non associati a lui
                // (il controllo in CheckRelazioni è solo lato interfaccia: qui viene ripetuto lato server)
                // 30/10/2026 solo se l'ha creato lui (PuoGestireArchivio)
                //if (!HaAccessoAlCliente(id, GetIdAgenteFiltro()))
                if (!PuoGestireArchivio(id, idAgenteFiltro))
                    return Json(new { success = false, message = "Non sei autorizzato a operare su questo cliente." });

                // ==== MODIFICA 30/10/2026 - INIZIO ====
                // Regola ripetuta lato server: l'Agente non elimina clienti con ordini
                if (idAgenteFiltro.HasValue && _clienteService.HasOrdersOrRelations(id))
                    return Json(new { success = false, message = "Il cliente ha ordini associati: non puoi eliminarlo. Contatta l'amministratore." });
                // ==== MODIFICA 30/10/2026 - FINE ====

                _clienteService.Delete(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // In caso di errore restituiamo un JSON con il messaggio
                //return Json(new { success = false, message = ex.Message });
                return Json(new { success = false, message = MessaggioErrore(ex, nameof(Delete)) });//21/10/2026 log e messaggio generico per Agente
            }
        }

        // --- SEARCH (AUTOCOMPLETE) ---
        // Fornisce i dati per i widget Select2 o ricerche rapide.
        [HttpGet]
        public IActionResult Search(string term)
        {
            // 1) Validazione input (minimo 2 caratteri per non sovraccaricare il SQL)
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return Json(new List<object>());

            // 2) Chiamo il service che a sua volta usa il repository
            // 12/10/2026 Isolamento per Agente: l'autocomplete restituisce solo i suoi clienti
            // 30/10/2026 (agente di default + ProvvigioniAgenti, filtro nel repository)
            var risultati = _clienteService.SearchByRagioneSociale(term, idAgente: GetIdAgenteFiltro());

            // 3) Proietto solo i campi ID e Testo per l'autocomplete
            var response = risultati.Select(c => new
            {
                id = c.IdCliente,
                text = c.RagioneSociale
            });

            return Json(response);
        }

        // ==== MODIFICA 30/10/2026 - INIZIO ====
        // --- NOMI SIMILI (AVVISO MENTRE SI DIGITA, form "Nuovo cliente") ---
        // Per l'Agente i clienti che non gli appartengono arrivano senza nome né id (visibile = false):
        // la SQL li oscura, quindi non si può "scoprire" l'anagrafica degli altri agenti.
        [HttpGet]
        public IActionResult NomiSimili(string term)
        {
            var idAgenteFiltro = GetIdAgenteFiltro();

            // Agente non collegato a nessun record Agenti: nessuna ricerca (AJAX: 403, non Forbid)
            if (idAgenteFiltro.HasValue && idAgenteFiltro.Value <= 0)
                return StatusCode(403);

            var risultati = _clienteService.GetSimili(term, idAgenteFiltro);

            return Json(risultati.Select(r => new
            {
                idCliente = r.IdCliente,
                nome = r.RagioneSociale,
                visibile = r.Visibile,
                archiviato = r.Archiviato
            }));
        }
        // ==== MODIFICA 30/10/2026 - FINE ====

        // --- CREATE (GET) ---
        [HttpGet]
        public IActionResult Create()
        {
            // 12/10/2026 Un Agente non collegato a nessun record Agenti non può creare clienti
            var idAgenteFiltro = GetIdAgenteFiltro();
            if (idAgenteFiltro.HasValue && idAgenteFiltro.Value <= 0)
                return Forbid();

            // Nuovo cliente vuoto
            // ==== MODIFICA 30/10/2026 - INIZIO ====
            // 30/10/2026 Nessun agente da assegnare (Clienti.IdAgente dismesso): per l'Agente l'associazione
            // nasce dall'accordo in ProvvigioniAgenti creato da spClienti_InsertDaAgente.
            //CaricaDropdownAgenti(idAgenteFiltro, idAgenteFiltro);
            var nuovo = new Cliente();
            // Per l'Agente il mandatario è obbligatorio
            CaricaLookupCliente(nuovo, mandatarioBloccato: false, mandatarioObbligatorio: idAgenteFiltro.HasValue);
            return View(nuovo);
            // ==== MODIFICA 30/10/2026 - FINE ====
        }

        // --- EDIT (GET) ---
        // Recupera i dati di un cliente esistente per la modifica
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var cliente = _clienteService.GetById(id);
            if (cliente == null)
                return NotFound();

            // 12/10/2026 PROTEZIONE AGENTE: non può aprire la scheda di un cliente non associato a lui
            var idAgenteFiltro = GetIdAgenteFiltro();
            // ==== MODIFICA 30/10/2026 - INIZIO ====
            // 30/10/2026 accesso anche ai clienti presenti in ProvvigioniAgenti (non solo agente di default)
            //if (idAgenteFiltro.HasValue && cliente.IdAgente != idAgenteFiltro.Value)
            //    return Forbid();
            if (!HaAccessoAlCliente(id, idAgenteFiltro))
                return Forbid();
            // ==== MODIFICA 30/10/2026 - FINE ====

            // 14/10/2026 Pre-selezione del mandatario già associato (MandatariClienti).
            // L'accesso al cliente è già stato verificato sopra (Forbid per Agente non proprietario).
            cliente.IdMandatario = _clienteService.GetIdMandatario(id);

            // ==== MODIFICA 30/10/2026 - INIZIO ====
            // Niente DDL Agente. Per l'Agente il mandatario si vede ma non si cambia dopo la creazione.
            //CaricaDropdownAgenti(cliente.IdAgente, idAgenteFiltro);
            CaricaLookupCliente(cliente, mandatarioBloccato: idAgenteFiltro.HasValue, mandatarioObbligatorio: false);
            // ==== MODIFICA 30/10/2026 - FINE ====

            return View(cliente);
        }


        // --- SAVE (POST) ---
        // Gestisce sia l'inserimento (IdCliente == 0) che l'aggiornamento di un cliente esistente.
        [HttpPost]
        // [ValidateAntiForgeryToken] -> RIMOSSO: Gestito globalmente da Program.cs
        public IActionResult Save(Cliente cliente)
        {
            // ------------------------------------------------------------
            // 12/10/2026 PROTEZIONE AGENTE (lato server, non aggirabile dal client)
            // ------------------------------------------------------------
            var idAgenteFiltro = GetIdAgenteFiltro();
            if (idAgenteFiltro.HasValue)
            {
                // Agente non collegato a nessun record Agenti: nessuna operazione consentita
                if (idAgenteFiltro.Value <= 0)
                    return Forbid();

                // MODIFICA: il cliente esistente deve essere accessibile all'agente. Il controllo usa il DB sull'IdCliente
                // postato, quindi cambiare IdCliente nel form non permette di sovrascrivere clienti altrui.
                // 30/10/2026 accesso anche tramite ProvvigioniAgenti, non solo agente di default
                if (cliente.IdCliente != 0 && !HaAccessoAlCliente(cliente.IdCliente, idAgenteFiltro))
                    return Forbid();
            }

            // ==== MODIFICA 30/10/2026 - INIZIO ====
            // Clienti.IdAgente è dismesso: il form non lo posta più e nessuno lo modifica da qui.
            // - nuovo cliente: resta vuoto (l'associazione agente-cliente è in ProvvigioniAgenti);
            // - modifica: si riporta il valore presente nel DB, così l'UPDATE non lo cancella né lo cambia.
            //   (prima: per l'Agente veniva forzato l'agente loggato)
            if (cliente.IdCliente != 0)
            {
                var attuale = _clienteService.GetById(cliente.IdCliente);
                if (attuale == null)
                    return NotFound();

                cliente.IdAgente = attuale.IdAgente;

                // L'Agente non cambia il mandatario dopo la creazione (una DDL disabilitata non invia il valore,
                // e comunque il form è manomettibile): si riporta quello presente nel DB.
                if (idAgenteFiltro.HasValue)
                {
                    cliente.IdMandatario = _clienteService.GetIdMandatario(cliente.IdCliente);
                    ModelState.Remove(nameof(cliente.IdMandatario));
                }
            }
            else
            {
                cliente.IdAgente = null;

                // L'Agente deve scegliere il mandatario: senza, il cliente non comparirebbe nel dropdown degli ordini
                if (idAgenteFiltro.HasValue && !cliente.IdMandatario.HasValue)
                    ModelState.AddModelError(nameof(cliente.IdMandatario), "Per creare un cliente devi scegliere il mandatario.");
            }

            // Rimuoviamo il valore postato dal ModelState, altrimenti in caso di errore la View
            // mostrerebbe il valore originale inviato dal client invece di quello forzato.
            ModelState.Remove(nameof(cliente.IdAgente));
            // ==== MODIFICA 30/10/2026 - FINE ====

            // Valorizzazione del campo Utente lato server.
            cliente.Utente = User.Identity?.Name ?? "System";

            // Rimozione esplicita di "Utente" dal ModelState.
            ModelState.Remove(nameof(cliente.Utente));

            // 14/10/2026 Il mandatario postato deve esistere: le FK di MandatariClienti sono NOCHECK,
            // senza questo controllo un id manomesso verrebbe scritto comunque.
            if (cliente.IdMandatario.HasValue &&
                !(_mandatarioService.GetAll() ?? Enumerable.Empty<Mandatario>())
                    .Any(m => m.IdMandatario == cliente.IdMandatario.Value))
            {
                ModelState.AddModelError(nameof(cliente.IdMandatario), "Il mandatario selezionato non è valido.");
            }

            // Flag per ricaricare correttamente la maschera in caso di errore
            bool mandatarioBloccato = idAgenteFiltro.HasValue && cliente.IdCliente != 0;
            bool mandatarioObbligatorio = idAgenteFiltro.HasValue && cliente.IdCliente == 0;

            // --- VALIDAZIONE ---
            if (!ModelState.IsValid)
            {
                // ==== MODIFICA 30/10/2026 - INIZIO ====
                // Ricaricamento delle liste (Tipo Pagamento, Mandatario) per non perdere le dropdown nella view
                CaricaLookupCliente(cliente, mandatarioBloccato, mandatarioObbligatorio);
                // ==== MODIFICA 30/10/2026 - FINE ====

                // Restituzione della view corretta (Create o Edit) con il modello e gli errori
                return View(cliente.IdCliente == 0 ? "Create" : "Edit", cliente);
            }

            // --- SALVATAGGIO ---
            try
            {
                // ==== MODIFICA 30/10/2026 - INIZIO ====
                // NUOVO CLIENTE DA PARTE DI UN AGENTE: una sola stored procedure transazionale
                // (controllo duplicati + cliente + mandatario + accordo di provvigione).
                if (idAgenteFiltro.HasValue && cliente.IdCliente == 0)
                {
                    var esito = _clienteService.CreaClienteDaAgente(cliente, idAgenteFiltro.Value);

                    switch (esito.Esito)
                    {
                        case EsitoCreazioneCliente.Ok:
                            TempData["Success"] = "Cliente creato con successo.";
                            return RedirectToAction(nameof(Index));

                        case EsitoCreazioneCliente.GiaTuo when esito.IdCliente.HasValue:
                            // Il cliente esiste già tra i suoi: lo apriamo invece di crearne uno nuovo
                            TempData["Info"] = "Cliente già presente tra i tuoi clienti: l'ho aperto per te."; // 30/10/2026 Info (non Success): la legge la pagina Edit
                            return RedirectToAction(nameof(Edit), new { id = esito.IdCliente.Value });

                        default:
                            ModelState.AddModelError(string.Empty, MessaggioEsitoCreazione(esito.Esito));
                            CaricaLookupCliente(cliente, mandatarioBloccato, mandatarioObbligatorio);
                            return View("Create", cliente);
                    }
                }
                // ==== MODIFICA 30/10/2026 - FINE ====

                // 12/10/2026 passiamo l'agente loggato al service per bloccare la "resurrezione"
                // di clienti archiviati omonimi appartenenti ad altri agenti
                // ==== MODIFICA 30/10/2026 - INIZIO ====
                bool eraNuovo = cliente.IdCliente == 0;   // SalvaCliente valorizza IdCliente sul nuovo inserimento
                // ==== MODIFICA 30/10/2026 - FINE ====
                _clienteService.SalvaCliente(cliente, idAgenteFiltro);

                TempData["Success"] = "Dati salvati con successo.";

                // ==== MODIFICA 30/10/2026 - INIZIO ====
                // Solo Admin e solo per un cliente nuovo: l'Index mostra il pulsante "Collega a un agente"
                if (eraNuovo && User.IsInRole("Admin") && cliente.IdCliente != 0)
                {
                    TempData["Success"] = "Cliente creato con successo.";
                    TempData["IdClienteNuovo"] = cliente.IdCliente.ToString();
                }
                // ==== MODIFICA 30/10/2026 - FINE ====
                return RedirectToAction(nameof(Index));
            }
            // 21/10/2026 Eccezione di business (es. cliente omonimo di altro agente): messaggio leggibile dall'Agente
            // 30/10/2026 include anche la violazione degli indici unici (nome / Partita IVA / Codice Fiscale già presenti)
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);

                CaricaLookupCliente(cliente, mandatarioBloccato, mandatarioObbligatorio);   // 30/10/2026

                return View(cliente.IdCliente == 0 ? "Create" : "Edit", cliente);
            }
            // 21/10/2026 Eccezioni generiche: gestione log e messaggio con helper MessaggioErrore
            catch (Exception ex)
            {
                // Errore durante il salvataggio: aggiunto al ModelState per mostrarlo nella view
                ModelState.AddModelError(string.Empty, "Errore durante il salvataggio: " + MessaggioErrore(ex, nameof(Save)));

                CaricaLookupCliente(cliente, mandatarioBloccato, mandatarioObbligatorio);   // 30/10/2026

                return View(cliente.IdCliente == 0 ? "Create" : "Edit", cliente);
            }
        }

        // ==== MODIFICA 30/10/2026 - INIZIO ====
        /// <summary>
        /// 30/10/2026 Testi mostrati all'Agente per gli esiti della creazione cliente.
        /// Per i clienti che non sono suoi (o archiviati) NON si rivela alcun dato: solo "contatta l'amministratore".
        /// </summary>
        private static string MessaggioEsitoCreazione(EsitoCreazioneCliente esito) => esito switch
        {
            EsitoCreazioneCliente.EsisteNonVisibile =>
                "Cliente già presente in anagrafica: contatta l'amministratore per associarlo al tuo profilo.",
            EsitoCreazioneCliente.Archiviato =>
                "Esiste già un cliente archiviato con questo nome o questi dati: contatta l'amministratore.",
            EsitoCreazioneCliente.Duplicato =>
                "Esiste già un cliente con questo nome o questi dati. Riprova: se è tuo lo trovi nell'elenco clienti.",
            EsitoCreazioneCliente.NoPercentuale =>
                "Per il tuo profilo non è impostata la percentuale di provvigione: contatta l'amministratore.",
            EsitoCreazioneCliente.AgenteNonValido =>
                "Il tuo utente non è collegato a un agente valido: contatta l'amministratore.",
            EsitoCreazioneCliente.MandatarioNonValido =>
                "Il mandatario selezionato non è valido.",
            EsitoCreazioneCliente.NomeNonValido =>
                "La ragione sociale non è valida.",
            _ => "Operazione non riuscita. Contatta l'amministratore."
        };
        // ==== MODIFICA 30/10/2026 - FINE ====


        // 07/08/2026 lookup cliente -> idAgente
        [HttpGet]
        public IActionResult GetAgenteCliente(int idCliente)
        {
            var cliente = _clienteService.GetById(idCliente);
            if (cliente == null)
                return NotFound();

            // 12/10/2026 PROTEZIONE AGENTE: nessun lookup su clienti non associati a lui
            var idAgenteFiltro = GetIdAgenteFiltro();
            // ==== MODIFICA 30/10/2026 - INIZIO ====
            // 30/10/2026 accesso anche tramite ProvvigioniAgenti. Per l'Agente la risposta è sempre il suo
            // IdAgente: nei form ordine serve a sapere "chi è l'agente dell'ordine" e per lui è sé stesso,
            // anche se l'agente di default dell'anagrafica è un altro o è vuoto.
            //if (idAgenteFiltro.HasValue && cliente.IdAgente != idAgenteFiltro.Value)
            //    return StatusCode(403);
            //
            //return Json(new { idAgente = cliente.IdAgente });
            if (idAgenteFiltro.HasValue)
            {
                if (!HaAccessoAlCliente(idCliente, idAgenteFiltro))
                    return StatusCode(403);

                return Json(new { idAgente = idAgenteFiltro.Value });
            }

            return Json(new { idAgente = cliente.IdAgente });
            // ==== MODIFICA 30/10/2026 - FINE ====
        }



        // 07/08/2026 lookup cliente -> idAgente
        [HttpGet]
        public IActionResult GetTipoPagamento(int idCliente)
        {
            var cliente = _clienteService.GetById(idCliente);
            if (cliente == null)
                return NotFound();

            // 12/10/2026 PROTEZIONE AGENTE: nessun lookup su clienti non associati a lui
            var idAgenteFiltro = GetIdAgenteFiltro();
            // ==== MODIFICA 30/10/2026 - INIZIO ====
            //if (idAgenteFiltro.HasValue && cliente.IdAgente != idAgenteFiltro.Value)   // 30/10/2026 sostituita dalla riga sotto
            if (!HaAccessoAlCliente(idCliente, idAgenteFiltro))
                return StatusCode(403);
            // ==== MODIFICA 30/10/2026 - FINE ====

            return Json(new { idTipoPagamento = cliente.IdTipoPagamentoDefault });
        }







        // ============================================================
        // 26/08/2026 recupera TipoPagamento di Default del Cliente
        // ============================================================

        [HttpGet]
        public IActionResult GetTipoPagamentoDefault(int idCliente)
        {
            try
            {
                // 1. Validazione parametro di input
                if (idCliente <= 0)
                {
                    return BadRequest("IdCliente non valido.");
                }

                // 12/10/2026 PROTEZIONE AGENTE: nessun lookup su clienti non associati a lui
                // 30/10/2026 HaAccessoAlCliente considera anche ProvvigioniAgenti
                if (!HaAccessoAlCliente(idCliente, GetIdAgenteFiltro()))
                {
                    return StatusCode(403);
                }

                // 2. Interrogazione del repository per recuperare il DTO / oggetto di default
                var defaultDto = _clienteService.GetTipoPagamentoDefault(idCliente);

                if (defaultDto == null)
                {
                    return Json(null);
                }

                // 3. Ritorno del risultato in formato JSON
                return Json(defaultDto);
            }
            catch (Exception ex)
            {
                // Tracciamento errore in console/log (pattern usato in GetPercentualeProvvigione)
                //Console.WriteLine($"[GetTipoPagamentoDefault] Errore: {ex.Message}");
                _logger.LogError(ex, "GetTipoPagamentoDefault idCliente={IdCliente}", idCliente);//21/10/2026
                return StatusCode(500, "Errore durante il recupero del tipo pagamento predefinito.");
            }
        }

        //DENIS 21/10/2026: aggiunto metodo per loggare gli errori e restituire messaggio generico per Agente
        private string MessaggioErrore(Exception ex, string operazione)
        {
            _logger.LogError(ex, "Errore Clienti/{Operazione} (utente {Utente})", operazione, User.Identity?.Name);
            return User.IsInRole("Agente")
                ? "Operazione non riuscita. Contatta l'amministratore."
                : ex.Message;
        }

    }
}
