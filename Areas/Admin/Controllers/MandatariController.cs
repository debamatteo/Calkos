using CalkosManager.Application.Services;
using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Calkos.web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Operatore")]
    public class MandatariController : Controller
    {
        private readonly MandatarioService _service;

        /// <summary>
        /// Il controller riceve il service tramite Dependency Injection.
        /// Il controller deve essere snello: tutta la logica applicativa risiede nel service.
        /// </summary>
        public MandatariController(MandatarioService service)
        {
            _service = service;
        }

        // ============================================================
        //                CRUD PRINCIPALE
        // ============================================================

        /// <summary>
        /// Pagina di creazione di un nuovo Mandatario.
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Mandatario());
        }

        /// <summary>
        /// Salvataggio del nuovo Mandatario.
        /// </summary>
        [HttpPost]
        public IActionResult Create(Mandatario model)
        {
            model.Utente = User.Identity?.Name ?? "System";

            var result = _service.Insert(model);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Lista completa dei Mandatari.
        /// </summary>
        public IActionResult Index()
        {
            var lista = _service.GetAll();
            return View(lista);
        }

        /// <summary>
        /// Pagina di modifica di un Mandatario esistente.
        /// </summary>
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = _service.GetById(id);
            if (model == null) return NotFound();

            return View(model);
        }

        /// <summary>
        /// Salvataggio delle modifiche del Mandatario.
        /// </summary>
        [HttpPost]
        public IActionResult Edit(Mandatario model)
        {
            model.Utente = User.Identity?.Name ?? "System";

            var result = _service.Update(model);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Eliminazione definitiva del Mandatario.
        /// </summary>
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var result = _service.Delete(id);

            return Json(new
            {
                success = result.Success,
                message = result.Message
            });
        }

        // ============================================================
        //       SEZIONE RELAZIONE MANDATARIO ↔ CLIENTE (AJAX)
        // ============================================================
        // Come richiesto, NON viene spostata nel service per evitare regressioni.

        /// <summary>
        /// Restituisce i clienti associati al Mandatario (AJAX).
        /// </summary>
        [HttpGet]
        public IActionResult GetClienti(int idMandatario)
        {
            var clienti = _service.GetClienti(idMandatario);

            var response = clienti.Select(c => new
            {
                idCliente = c.IdCliente,
                ragioneSociale = c.RagioneSociale
            });

            return Json(response);
        }

        /// <summary>
        /// Aggiunge un cliente al Mandatario (AJAX).
        /// </summary>
        [HttpPost]
        public IActionResult AddCliente(int idMandatario, int idCliente)
        {
            _service.AddCliente(idMandatario, idCliente);
            return Ok();
        }

        /// <summary>
        /// Rimuove un cliente associato al Mandatario (AJAX).
        /// </summary>
        [HttpPost]
        public IActionResult RemoveCliente(int idMandatario, int idCliente)
        {
            _service.RemoveCliente(idMandatario, idCliente);
            return Json(new { success = true });
        }

        /// <summary>
        /// Verifica se il Mandatario ha clienti associati prima della cancellazione (AJAX).
        /// </summary>
        [HttpGet]
        public IActionResult CheckRelazioni(int id)
        {
            var result = _service.CheckRelazioni(id);

            return Json(new
            {
                canDelete = result.CanDelete,
                message = result.Message
            });
        }
    }
}