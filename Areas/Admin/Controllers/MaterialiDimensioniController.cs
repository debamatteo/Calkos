using CalkosManager.Application.Services;
using CalkosManager.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Calkos.web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class MaterialiDimensioniController : Controller
    {
        private readonly MaterialiDimensioniService _service;

        /// <summary>
        /// Costruttore con Dependency Injection del service.
        /// </summary>
        public MaterialiDimensioniController(MaterialiDimensioniService service)
        {
            _service = service;
        }

        // ============================================================
        // INDEX — Lista Dimensioni
        // ============================================================
        public IActionResult Index()
        {
            var model = _service.GetAll();
            return View(model);
        }

        // ============================================================
        // CREATE — Nuova Dimensione
        // ============================================================
        public IActionResult Create()
        {
            var model = new MaterialiDimensioni();
            return View(model);
        }

        // ============================================================
        // EDIT — Modifica Dimensione
        // ============================================================
        public IActionResult Edit(int id)
        {
            var model = _service.GetById(id);

            if (model == null)
                return NotFound();

            return View(model);
        }

        // ============================================================
        // SAVE — Create + Update
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(MaterialiDimensioni model)
        {
            if (!ModelState.IsValid)
            {
                return View(model.IdDimensione == 0 ? "Create" : "Edit", model);
            }

            var result = _service.Save(model);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                return View(model.IdDimensione == 0 ? "Create" : "Edit", model);
            }

            return RedirectToAction("Index");
        }

        // ============================================================
        // DELETE — Eliminazione via AJAX
        // ============================================================
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var result = _service.Delete(id);
            return Json(result);
        }
    }
}
