using CalkosManager.Application.Services;
using CalkosManager.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;

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
        [Authorize(Roles = "Admin")]//19/10/2026
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
        // 31/10/2026 Accetta un returnUrl opzionale: permette di tornare alla pagina di
        // provenienza (es. Modifica Utente) dopo aver creato l'agente.
        [HttpGet]
        public IActionResult Create(string? returnUrl = null)
        {
            ViewBag.Agenti = new SelectList(_service.GetAll(), "IdAgente", "AgenteDescrizione");
            ViewBag.ReturnUrl = UrlRitornoValido(returnUrl);
            return View(new Agente());
        }

        // 31/10/2026 Anti open-redirect: il returnUrl è accettato SOLO se è un percorso locale
        // (es. "/Admin/Users/Edit/..."). Qualsiasi URL esterno viene scartato (null).
        private string? UrlRitornoValido(string? returnUrl) =>
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null;

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
        public IActionResult Save(Agente agente, string? returnUrl = null)
        {
            // 31/10/2026 Rivalidazione lato server: il valore arriva dal client e non è fidato
            returnUrl = UrlRitornoValido(returnUrl);

            // 05/11/2026 Va letto PRIMA di Save: il service scrive l'Id generato in agente.IdAgente
            var eraNuovo = agente.IdAgente == 0;

            //19/10/2026
            if (!ModelState.IsValid)
            {
                ViewBag.Agenti = new SelectList(_service.GetAll(), "IdAgente", "AgenteDescrizione", agente.IdAgente);
                ViewBag.ReturnUrl = returnUrl; // 31/10/2026 mantiene il ritorno in caso di errore
                return View(agente.IdAgente == 0 ? "Create" : "Edit", agente);
            }

            agente.Utente = User.Identity?.Name ?? "System";

            var result = _service.Save(agente);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                ViewBag.Agenti = new SelectList(_service.GetAll(), "IdAgente", "AgenteDescrizione", agente.IdAgente);
                ViewBag.ReturnUrl = returnUrl; // 31/10/2026 mantiene il ritorno in caso di errore
                return View(agente.IdAgente == 0 ? "Create" : "Edit", agente);
            }

            // 31/10/2026 Se si arriva da un'altra pagina (es. Modifica Utente) si torna lì
            if (returnUrl != null)
            {
                // ==== MODIFICA 05/11/2026 - INIZIO ====
                // Si comunica alla pagina di ritorno quale agente è stato creato, così lo preseleziona.
                // AddQueryString gestisce anche un returnUrl che ha già dei parametri.
                if (eraNuovo && agente.IdAgente > 0)
                    returnUrl = QueryHelpers.AddQueryString(returnUrl, "idAgenteCreato", agente.IdAgente.ToString());
                // ==== MODIFICA 05/11/2026 - FINE ====
                return LocalRedirect(returnUrl);
            }

            TempData["Success"] = "Dati salvati con successo.";
            return RedirectToAction(nameof(Index));
        }
    }
}
