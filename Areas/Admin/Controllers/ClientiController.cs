using CalkosManager.Application.Services;
using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Calkos.web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Operatore")]
    public class ClientiController : Controller
    {
        // Service applicativo per la gestione dei clienti
        private readonly ClienteService _clienteService;

        // Repository per gli agenti (usato solo per popolare la SelectList)
        private readonly IAgenteRepository _agenteRepository;

        /// <summary>
        /// Crea una nuova istanza di <see cref="ClientiController"/>.
        /// </summary>
        public ClientiController(
            ClienteService clienteService,
            IAgenteRepository agenteRepository)
        {
            _clienteService = clienteService;
            _agenteRepository = agenteRepository;
        }

        // --- LISTA CLIENTI ---
        /// <summary>
        /// Visualizza l'elenco dei clienti. 
        /// </summary>
        /// <param name="showDeleted">Se true, mostra i clienti eliminati logicamente (Cestino).</param>
        public IActionResult Index(bool showDeleted = false)
        {
            // Passiamo il parametro al service per filtrare i dati
            var model = _clienteService.GetAll(showDeleted);

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
                // Chiamata al service per eseguire la procedura di ripristino
                bool success = _clienteService.Restore(id);

                if (success)
                    return Json(new { success = true });
                else
                    return Json(new { success = false, message = "Impossibile trovare il cliente da ripristinare." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // --- CHECK RELAZIONI ---
        // Chiamata via AJAX dalla modale JS prima della cancellazione.
        // Impedisce di spaccare il database se ci sono ordini legati al cliente.
        [HttpGet]
        public IActionResult CheckRelazioni(int id)
        {
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
                _clienteService.Delete(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // In caso di errore restituiamo un JSON con il messaggio
                return Json(new { success = false, message = ex.Message });
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
            var risultati = _clienteService.SearchByRagioneSociale(term);

            // 3) Proietto solo i campi ID e Testo per l'autocomplete
            var response = risultati.Select(c => new
            {
                id = c.IdCliente,
                text = c.RagioneSociale
            });

            return Json(response);
        }

        // --- CREATE (GET) ---
        [HttpGet]
        public IActionResult Create()
        {
            // Carichiamo la lista agenti per la dropdown
            var agenti = _agenteRepository.GetAll() ?? Enumerable.Empty<Agente>();
            ViewBag.Agenti = new SelectList(agenti, "IdAgente", "AgenteDescrizione");

            // Nuovo cliente vuoto
            return View(new Cliente());
        }

        // --- EDIT (GET) ---
        // Recupera i dati di un cliente esistente per la modifica
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var cliente = _clienteService.GetById(id);
            if (cliente == null)
                return NotFound();

            var agenti = _agenteRepository.GetAll() ?? Enumerable.Empty<Agente>();
            ViewBag.Agenti = new SelectList(agenti, "IdAgente", "AgenteDescrizione", cliente.IdAgente);

            return View(cliente);
        }

        // --- SAVE (POST) ---
        // Gestisce sia l'inserimento (IdCliente == 0) che l'aggiornamento di un cliente esistente.
        [HttpPost]
        // [ValidateAntiForgeryToken] -> RIMOSSO: Gestito globalmente da Program.cs
        public IActionResult Save(Cliente cliente)
        {
            // Valorizzazione del campo Utente lato server.
            cliente.Utente = User.Identity?.Name ?? "System";

            // Rimozione esplicita di "Utente" dal ModelState.
            ModelState.Remove(nameof(cliente.Utente));

            // --- VALIDAZIONE ---
            if (!ModelState.IsValid)
            {
                // Ricaricamento della SelectList agenti per non perdere il dropdown nella view
                ViewBag.Agenti = new SelectList(
                    _agenteRepository.GetAll() ?? Enumerable.Empty<Agente>(),
                    "IdAgente",
                    "AgenteDescrizione",
                    cliente.IdAgente
                );

                // Restituzione della view corretta (Create o Edit) con il modello e gli errori
                return View(cliente.IdCliente == 0 ? "Create" : "Edit", cliente);
            }

            // --- SALVATAGGIO ---
            try
            {
                _clienteService.SalvaCliente(cliente);

                TempData["Success"] = "Dati salvati con successo.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Errore durante il salvataggio: aggiunto al ModelState per mostrarlo nella view
                ModelState.AddModelError(string.Empty, "Errore durante il salvataggio: " + ex.Message);

                // Ricaricamento della SelectList agenti anche in caso di eccezione
                ViewBag.Agenti = new SelectList(
                    _agenteRepository.GetAll() ?? Enumerable.Empty<Agente>(),
                    "IdAgente",
                    "AgenteDescrizione",
                    cliente.IdAgente
                );

                return View(cliente.IdCliente == 0 ? "Create" : "Edit", cliente);
            }
        }
    }
}