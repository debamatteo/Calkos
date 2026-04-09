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
        [HttpPost]
        // Il token CSRF viene rimosso: gestito globalmente per le chiamate AJAX
        public IActionResult Delete(int id)
        {
            // Delega la logica di validazione eliminazione al Service
            var result = _service.Delete(id);
            return Json(result);
        }

        /// <summary>
        /// Pagina di inserimento nuova provvigione.
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            LoadDropdowns();
            return View(new ProvvigioniAgenti());
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
        /// </summary>
        /// <param name="model">Entità provvigione mappata dal form.</param>
        [HttpPost]
        // Il token CSRF viene rimosso: gestito globalmente per i form POST
        public IActionResult Save(ProvvigioniAgenti model)
        {
            // Tracciamento dell'utente che esegue l'operazione
            model.Utente = User.Identity?.Name ?? "System";

            var result = _service.Save(model);

            if (!result.Success)
            {
                // In caso di errore di validazione o business logic nel Service
                ModelState.AddModelError(string.Empty, result.Message);
                LoadDropdowns(model);

                // Ritorna alla vista di provenienza (Create o Edit) mantenendo i dati inseriti
                return View(model.IdProvvigione == 0 ? "Create" : "Edit", model);
            }

            // Successo: Notifica l'utente e reindirizza alla lista
            TempData["Success"] = "Configurazione provvigionale salvata correttamente.";
            return RedirectToAction(nameof(Index));
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