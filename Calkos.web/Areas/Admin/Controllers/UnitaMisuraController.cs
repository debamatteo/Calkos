using CalkosManager.Application.Services;
using CalkosManager.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Calkos.web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Operatore")]
    public class UnitaMisuraController : Controller
    {
        private readonly UnitaMisuraService _service;

        /// <summary>
        /// Il controller riceve il service tramite Dependency Injection.
        /// Replica lo stile snello di AgentiController delegando la logica al service.
        /// </summary>
        /// <param name="service">Service per la gestione unità di misura</param>
        public UnitaMisuraController(UnitaMisuraService service)
        {
            _service = service;
        }

        /// <summary>
        /// Pagina principale: lista di tutte le unità di misura.
        /// </summary>
        /// <returns>View con lista di UnitaMisura</returns>
        public IActionResult Index()
        {
            var model = _service.GetAll();
            return View(model);
        }

        /// <summary>
        /// Eliminazione definitiva dell'unità di misura (chiamata AJAX).
        /// </summary>
        /// <param name="id">ID dell'unità di misura da eliminare</param>
        /// <returns>JsonResult con l'esito dell'operazione</returns>
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var result = _service.Delete(id);
            return Json(result);
        }

        /// <summary>
        /// Pagina di creazione per una nuova unità di misura.
        /// </summary>
        /// <returns>View con oggetto UnitaMisura vuoto</returns>
        [HttpGet]
        public IActionResult Create()
        {
            return View(new UnitaMisura());
        }

        /// <summary>
        /// Pagina di modifica per un'unità di misura esistente.
        /// </summary>
        /// <param name="id">ID dell'unità di misura</param>
        /// <returns>View con i dati dell'unità di misura o NotFound</returns>
        [HttpGet]
        public IActionResult Edit(int id)
        {
            // Nota: Usiamo GetAll().FirstOrDefault() o implementiamo GetById nel service
            // Per coerenza con AgentiController cerchiamo l'elemento specifico
            var lista = _service.GetAll();
            var item = lista.FirstOrDefault(x => x.IdUnitaMisura == id);

            if (item == null) return NotFound();

            return View(item);
        }

        /// <summary>
        /// Salvataggio centralizzato dei dati (Create + Edit).
        /// </summary>
        /// <param name="unita">Oggetto UnitaMisura proveniente dalla form</param>
        /// <returns>Redirect a Index o ritorno alla View in caso di errore</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(UnitaMisura unita)
        {
            // Popolamento dell'utente che esegue l'operazione
            unita.Utente = User.Identity?.Name ?? "System";

            var result = _service.Save(unita);

            if (!result.Success)
            {
                // In caso di errore aggiungiamo l'errore al ModelState per la View
                ModelState.AddModelError(string.Empty, result.Message);
                return View(unita.IdUnitaMisura == 0 ? "Create" : "Edit", unita);
            }

            // Successo: impostiamo il messaggio e torniamo alla lista
            TempData["Success"] = "Unità di misura salvata con successo.";
            return RedirectToAction(nameof(Index));
        }
    }
}