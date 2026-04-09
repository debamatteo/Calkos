using CalkosManager.Application.Services;
using CalkosManager.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Calkos.web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Operatore")]
    public class AgentiController : Controller
    {
        private readonly AgenteService _service;

        /// <summary>
        /// Il controller riceve il service tramite Dependency Injection.
        /// Il controller deve essere snello: delega tutta la logica al service.
        /// </summary>
        public AgentiController(AgenteService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lista di tutti gli agenti.
        /// </summary>
        public IActionResult Index()
        {
            var model = _service.GetAll();
            return View(model);
        }

        /// <summary>
        /// Verifica relazioni prima della cancellazione (AJAX).
        /// </summary>
        [HttpGet]
        public IActionResult CheckRelazioni(int id)
        {
            var result = _service.CheckRelazioni(id);
            return Json(result);
        }

        /// <summary>
        /// Eliminazione definitiva dell'agente.
        /// </summary>
        [HttpPost]
        //[ValidateAntiForgeryToken]
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
            ViewBag.Agenti = new SelectList(_service.GetAll(), "IdAgente", "AgenteDescrizione");
            return View(new Agente());
        }

        /// <summary>
        /// Pagina di modifica.
        /// </summary>
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var agente = _service.GetById(id);
            if (agente == null) return NotFound();

            ViewBag.Agenti = new SelectList(_service.GetAll(), "IdAgente", "AgenteDescrizione", agente.IdAgente);
            return View(agente);
        }

        /// <summary>
        /// Salvataggio (Create + Edit).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(Agente agente)
        {
            agente.Utente = User.Identity?.Name ?? "System";

            var result = _service.Save(agente);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                ViewBag.Agenti = new SelectList(_service.GetAll(), "IdAgente", "AgenteDescrizione", agente.IdAgente);
                return View(agente.IdAgente == 0 ? "Create" : "Edit", agente);
            }

            TempData["Success"] = "Dati salvati con successo.";
            return RedirectToAction(nameof(Index));
        }
    }
}
