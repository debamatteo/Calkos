using CalkosManager.Application.Services;
using CalkosManager.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Calkos.web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Operatore")]
    public class ProvvigioniAgentiController : Controller
    {
        private readonly ProvvigioniAgentiService _service;
        private readonly AgenteService _agenteService;
        private readonly MandatarioService _mandatarioService;
        private readonly ClienteService _clienteService;

        /// <summary>
        /// Costruttore con Dependency Injection dei servizi necessari.
        /// Segue il principio di delega: il controller gestisce il flusso, il service la logica.
        /// </summary>
        public ProvvigioniAgentiController(
            ProvvigioniAgentiService service,
            AgenteService agenteService,
            MandatarioService mandatarioService,
            ClienteService clienteService)
        {
            _service = service;
            _agenteService = agenteService;
            _mandatarioService = mandatarioService;
            _clienteService = clienteService;
        }

        /// <summary>
        /// Visualizza l'elenco completo delle provvigioni configurate.
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            var model = _service.GetAll();
            return View(model);
        }

        /// <summary>
        /// Gestisce l'eliminazione asincrona (AJAX) di una configurazione provvigionale.
        /// </summary>
        /// <param name="id">ID della provvigione da rimuovere.</param>
        /// <returns>JSON con esito dell'operazione (Success/Message).</returns>
        ///
        [Authorize(Roles = "Admin")]//19/10/2026
        [HttpPost]
        // Il token CSRF viene rimosso: gestito globalmente per le chiamate AJAX
        public IActionResult Delete(int id)
        {
            // Delega la logica di validazione eliminazione al Service
            var result = _service.Delete(id);
            return Json(result);
        }

        // ==== MODIFICA 30/10/2026 - INIZIO ====
        /// <summary>
        /// Pagina di inserimento nuova provvigione.
        /// 30/10/2026 Parametri opzionali (arrivano dall'indirizzo, es. dal pulsante "Collega a un agente"
        /// della scheda cliente o da "Salva e aggiungi un altro"): precompilano il form.
        ///  - idCliente: seleziona il Cliente e, se idMandatario non e' indicato, il Mandatario del cliente;
        ///  - idMandatario / idAgente: selezionano Mandatario / Agente.
        /// Sono solo valori di partenza: l'utente puo' cambiarli e il Save controlla tutto come prima.
        /// </summary>
        [HttpGet]
        public IActionResult Create(int? idCliente = null, int? idMandatario = null, int? idAgente = null)
        {
            var model = new ProvvigioniAgenti();

            // Cliente: lo preseleziono solo se esiste davvero
            if (idCliente.HasValue && _clienteService.GetById(idCliente.Value) != null)
            {
                model.IdCliente = idCliente.Value;

                // Se il Mandatario non e' indicato uso quello collegato al cliente (se presente)
                if (!idMandatario.HasValue)
                    idMandatario = _clienteService.GetIdMandatario(idCliente.Value);
            }

            if (idMandatario.HasValue) model.IdMandatario = idMandatario.Value;
            if (idAgente.HasValue) model.IdAgente = idAgente.Value;

            LoadDropdowns(model);
            return View(model);
        }
        // ==== MODIFICA 30/10/2026 - FINE ====
        //[HttpGet]
        //public IActionResult Create()   // 30/10/2026 sostituita dalla versione con i parametri sopra
        //{
        //    LoadDropdowns();
        //    return View(new ProvvigioniAgenti());
        //}

        // ============================================================
        // 26/10/2026 REPLICA di una provvigione esistente.
        // Apre il form di inserimento (vista Create) precompilato con i valori della riga di origine
        // e IdProvvigione = 0: l'utente cambia Mandatario / Agente / Cliente / Percentuale e salva.
        // Il salvataggio passa da Save, che controlla se la combinazione esiste gia'.
        // Solo Admin: e' una scrittura, come Save (l'Operatore non ha il pulsante).
        // ============================================================
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Replica(int id)
        {
            var origine = _service.GetById(id);
            if (origine == null)
                return NotFound();

            // Copia dei SOLI campi di business: Id, date e utente non si replicano
            var copia = new ProvvigioniAgenti
            {
                IdProvvigione = 0,
                IdAgente = origine.IdAgente,
                IdMandatario = origine.IdMandatario,
                IdCliente = origine.IdCliente,
                Percentuale = origine.Percentuale
            };

            LoadDropdowns(copia);
            ViewData["Title"] = "Replica Provvigione";
            ViewBag.IdOrigineReplica = origine.IdProvvigione;
            return View("Create", copia);
        }

        /// <summary>
        /// Pagina di modifica di una provvigione esistente.
        /// </summary>
        /// <param name="id">ID record.</param>
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var item = _service.GetById(id);
            if (item == null)
                return NotFound();

            LoadDropdowns(item);
            return View(item);
        }

        /// <summary>
        /// Azione unificata per il salvataggio dei dati (Inserimento o Aggiornamento).
        /// 26/10/2026 Controllo duplicati sulla combinazione Mandatario - Agente - Cliente:
        /// se esiste gia', la prima richiesta NON scrive nulla e rimanda il form con il popup di conferma;
        /// la seconda (confermaSovrascrittura = true, inviata dal pulsante "Sovrascrivi") aggiorna il record esistente.
        /// 30/10/2026 azione = "altro" (pulsante "Salva e aggiungi un altro"): dopo il salvataggio si torna al form
        /// di inserimento con Agente e Mandatario gia' scelti, invece che all'elenco.
        /// </summary>
        /// <param name="model">Entità provvigione mappata dal form.</param>
        /// <param name="confermaSovrascrittura">true solo se l'utente ha confermato nel popup.</param>
        /// <param name="azione">"altro" = salva e aggiungi un altro; vuoto = salva e torna all'elenco.</param>
        [HttpPost]
        // Il token CSRF viene rimosso: gestito globalmente per i form POST
        [Authorize(Roles = "Admin")]//19/10/2026
        public IActionResult Save(ProvvigioniAgenti model, bool confermaSovrascrittura = false, string? azione = null) // 30/10/2026 aggiunto azione
        {
            // ==== MODIFICA 30/10/2026 - INIZIO ====
            // La vista rimanda il valore nel campo nascosto "azione" anche dopo il popup "Sovrascrivi"
            ViewBag.Azione = azione;
            bool aggiungiAltro = string.Equals(azione, "altro", StringComparison.OrdinalIgnoreCase);
            // ==== MODIFICA 30/10/2026 - FINE ====

            if (!ModelState.IsValid)//19/10/2026
            {
                LoadDropdowns(model);
                return View(model.IdProvvigione == 0 ? "Create" : "Edit", model);
            }
            // Tracciamento dell'utente che esegue l'operazione
            model.Utente = User.Identity?.Name ?? "System";

            var result = _service.SalvaConControlloDuplicati(model, confermaSovrascrittura);

            switch (result.Esito)
            {
                case EsitoSalvataggioProvvigione.Salvata:
                    TempData["Success"] = "Configurazione provvigionale salvata correttamente.";
                    // ==== MODIFICA 30/10/2026 - INIZIO ====
                    if (aggiungiAltro && model.IdProvvigione == 0)
                        return RedirectToAction(nameof(Create), new { idAgente = model.IdAgente, idMandatario = model.IdMandatario });
                    // ==== MODIFICA 30/10/2026 - FINE ====
                    return RedirectToAction(nameof(Index));

                case EsitoSalvataggioProvvigione.Sovrascritta:
                    TempData["Success"] = "Provvigione esistente sovrascritta correttamente.";
                    // ==== MODIFICA 30/10/2026 - INIZIO ====
                    if (aggiungiAltro && model.IdProvvigione == 0)
                        return RedirectToAction(nameof(Create), new { idAgente = model.IdAgente, idMandatario = model.IdMandatario });
                    // ==== MODIFICA 30/10/2026 - FINE ====
                    return RedirectToAction(nameof(Index));

                case EsitoSalvataggioProvvigione.DuplicatoDaConfermare:
                    // Nulla e' stato scritto: ripresento il form con i dati inseriti e i dati del record
                    // esistente, che la vista usa per il popup "Provvigione gia' presente".
                    LoadDropdowns(model);
                    ViewBag.Duplicato = result.Duplicato;
                    PreparaDescrizioniCombinazione(model);
                    return View("Create", model);

                default:
                    // In caso di errore di validazione o business logic nel Service
                    ModelState.AddModelError(string.Empty, result.Message);
                    LoadDropdowns(model);

                    // Ritorna alla vista di provenienza (Create o Edit) mantenendo i dati inseriti
                    return View(model.IdProvvigione == 0 ? "Create" : "Edit", model);
            }
        }

        /// <summary>
        /// 26/10/2026 Testi leggibili (Mandatario / Agente / Cliente) per il popup di conferma.
        /// Le stringhe vanno nella vista come testo: Razor le codifica in HTML (nessun rischio XSS).
        /// </summary>
        private void PreparaDescrizioniCombinazione(ProvvigioniAgenti model)
        {
            ViewBag.DescMandatario = _mandatarioService.GetAll()
                .FirstOrDefault(m => m.IdMandatario == model.IdMandatario)?.NomeMandatario ?? "N/D";

            ViewBag.DescAgente = _agenteService.GetAll()
                .FirstOrDefault(a => a.IdAgente == model.IdAgente)?.AgenteDescrizione ?? "N/D";

            ViewBag.DescCliente = model.IdCliente.HasValue
                ? _clienteService.GetAll().FirstOrDefault(c => c.IdCliente == model.IdCliente.Value)?.RagioneSociale ?? "N/D"
                : "TUTTI I CLIENTI";
        }

        /// <summary>
        /// Carica i dati necessari per popolare le liste di selezione (Select2/Dropdown).
        /// Centralizza il caricamento di Agenti, Mandatari e Clienti.
        /// </summary>
        /// <param name="item">Eventuale oggetto esistente per pre-selezionare i valori.</param>
        private void LoadDropdowns(ProvvigioniAgenti? item = null)
        {
            // Caricamento Agenti: utilizza la descrizione calcolata (Nome + Cognome)
            ViewBag.Agenti = new SelectList(
                _agenteService.GetAll().OrderBy(a => a.AgenteDescrizione),
                "IdAgente",
                "AgenteDescrizione",
                item?.IdAgente
            );

            // Caricamento Mandatari: attenzione alla proprietà NomeMandatario
            ViewBag.Mandatari = new SelectList(
                _mandatarioService.GetAll().OrderBy(m => m.NomeMandatario),
                "IdMandatario",
                "NomeMandatario",
                item?.IdMandatario
            );

            // Caricamento Clienti
            ViewBag.Clienti = new SelectList(
                _clienteService.GetAll().OrderBy(c => c.RagioneSociale),
                "IdCliente",
                "RagioneSociale",
                item?.IdCliente
            );
        }
    }
}
