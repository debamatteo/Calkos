using CalkosManager.Application.Services;
using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;//01//11/2026
namespace Calkos.web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Operatore")]
    public class MandatariController : Controller
    {


        private readonly MandatarioService _service;
        // ==== MODIFICA 01/11/2026 - INIZIO ====
        private readonly UnitaMisuraService _unitaMisuraService;
        /// <summary>
        /// Il controller riceve il service tramite Dependency Injection.
        /// Il controller deve essere snello: tutta la logica applicativa risiede nel service.
        /// </summary>

        public MandatariController(MandatarioService service, UnitaMisuraService unitaMisuraService)
        {
            _service = service;
            _unitaMisuraService = unitaMisuraService;
        }

        /// <summary>Elenco unità di misura per la tendina del default (NULL = nessun default).</summary>
        private void CaricaLookup()
        {
            var lista = _unitaMisuraService.GetAll()
                .Select(u => new SelectListItem { Value = u.IdUnitaMisura.ToString(), Text = u.UnitaMisuraDescrizione })
                .ToList();
            lista.Insert(0, new SelectListItem { Value = "", Text = "— nessun default —" });
            ViewBag.UnitaMisura = lista;
        }
        // ==== MODIFICA 01/11/2026 - FINE ====

        // ============================================================
        //                CRUD PRINCIPALE
        // ============================================================

        /// <summary>
        /// Pagina di creazione di un nuovo Mandatario.
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            CaricaLookup(); // 01/11/2026
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
                CaricaLookup(); // 01/11/2026
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
            CaricaLookup(); // 01/11/2026
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
                CaricaLookup(); // 01/11/2026
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