using CalkosManager.Application.Services;
using CalkosManager.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Calkos.web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Operatore")]
    public class TipoPagamentoController : Controller
    {
        private readonly TipoPagamentoService _service;

        /// <summary>
        /// Il controller riceve il service tramite Dependency Injection.
        /// Struttura speculare a AgentiController.
        /// </summary>
        public TipoPagamentoController(TipoPagamentoService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lista di tutti i tipi di pagamento.
        /// </summary>
        public IActionResult Index()
        {
            var model = _service.GetAll();
            return View(model);
        }

        /// <summary>
        /// Eliminazione definitiva del tipo pagamento (AJAX).
        /// Replicato dal pattern di Agenti.
        /// </summary>
        [HttpPost]
        // [ValidateAntiForgeryToken] // Decommenta se il JS invia il token
        public IActionResult Delete(int id)
        {
            var result = _service.Delete(id);
            return Json(result);
        }

        /// <summary>
        /// Pagina di creazione.
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            return View(new TipoPagamento());
        }

        /// <summary>
        /// Pagina di modifica.
        /// </summary>
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var entity = _service.GetById(id);
            if (entity == null) return NotFound();

            return View(entity);
        }

        /// <summary>
        /// Salvataggio centralizzato (Create + Edit).
        /// Delega la logica di inserimento/aggiornamento al Service.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(TipoPagamento tipoPagamento)
        {
            // Valorizzazione dell'utente corrente
            tipoPagamento.Utente = User.Identity?.Name ?? "System";

            // Se hai validazioni nel ModelState, le controlliamo qui
            if (!ModelState.IsValid)
            {
                return View(tipoPagamento.IdTipoPagamento == 0 ? "Create" : "Edit", tipoPagamento);
            }

            // Chiamata al metodo Save del Service (che gestisce internamente Insert/Update)
            var result = _service.Save(tipoPagamento);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(tipoPagamento.IdTipoPagamento == 0 ? "Create" : "Edit", tipoPagamento);
            }

            TempData["Success"] = "Dati salvati con successo.";
            return RedirectToAction(nameof(Index));
        }
    }
}