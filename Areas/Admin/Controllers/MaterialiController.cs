using CalkosManager.Application.Services;
using CalkosManager.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Calkos.web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Operatore")]
    public class MaterialiController : Controller
    {
        private readonly MaterialeService _service;

        /// <summary>
        /// Il controller riceve il service tramite Dependency Injection.
        /// Il controller deve essere snello: tutta la logica applicativa risiede nel service.
        /// </summary>
        public MaterialiController(MaterialeService service)
        {
            _service = service;
        }

        // ============================================================
        //                CRUD PRINCIPALE
        // ============================================================

        /// <summary>
        /// Lista completa dei Materiali.
        /// </summary>
        public IActionResult Index()
        {
            var lista = _service.GetAll();
            return View(lista);
        }

        /// <summary>
        /// Pagina di creazione di un nuovo Materiale.
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Materiale());
        }

        /// <summary>
        /// Salvataggio del nuovo Materiale.
        /// </summary>
        [HttpPost]
        public IActionResult Create(Materiale model)
        {
            model.Utente = User.Identity?.Name ?? "System";

            try
            {
                _service.Save(model);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        /// <summary>
        /// Pagina di modifica di un Materiale esistente.
        /// </summary>
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var model = _service.GetById(id);
            if (model == null) return NotFound();

            return View(model);
        }

        /// <summary>
        /// Salvataggio delle modifiche del Materiale.
        /// </summary>
        [HttpPost]
        public IActionResult Edit(Materiale model)
        {
            model.Utente = User.Identity?.Name ?? "System";

            try
            {
                _service.Save(model);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        /// <summary>
        /// Salvataggio Materiale
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(Materiale model)
        {
            model.Utente = User.Identity?.Name ?? "System";

            try
            {
                _service.Save(model);
                TempData["Success"] = "Dati salvati con successo.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model.IdMateriale == 0 ? "Create" : "Edit", model);
            }
        }








        /// <summary>
        /// Eliminazione definitiva del Materiale.
        /// </summary>
        [HttpPost]
        public IActionResult Delete(int id)
        {
            try
            {
                _service.Delete(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
