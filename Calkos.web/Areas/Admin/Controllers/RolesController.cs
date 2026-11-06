using Calkos.Web.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Calkos.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]// Solo gli utenti con ruolo "Admin" possono accedere a questo controller
    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RolesController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        // ==== MODIFICA 19/10/2026 - INIZIO (M3 ruoli di sistema) ====
        private static readonly string[] RuoliDiSistema = { "Admin", "Operatore", "Agente" };

        private static bool EDiSistema(string? nome) =>
            RuoliDiSistema.Contains(nome, StringComparer.OrdinalIgnoreCase);
        // ==== MODIFICA 19/10/2026 - FINE ====

        // GET: Admin/Roles
        public IActionResult Index()
        {
            var roles = _roleManager.Roles.ToList();
            return View(roles);
        }

        // GET: Admin/Roles/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Roles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                ModelState.AddModelError("", "Il nome del ruolo è obbligatorio.");
                return View();
            }

            roleName = roleName.Trim(); // 19/10/2026

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));

            if (result.Succeeded)
                return RedirectToAction(nameof(Index));

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View();
        }

        // GET: Admin/Roles/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return NotFound();

            return View(role);
        }

        // POST: Admin/Roles/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, string name)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return NotFound();

            // ==== MODIFICA 19/10/2026 - INIZIO ====
            if (EDiSistema(role.Name))
            {
                ModelState.AddModelError("", "I ruoli di sistema non possono essere rinominati.");
                return View(role);
            }
            // ==== MODIFICA 19/10/2026 - FINE ====

            role.Name = name;

            var result = await _roleManager.UpdateAsync(role);

            if (result.Succeeded)
                return RedirectToAction(nameof(Index));

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(role);
        }

        // GET: Admin/Roles/Delete/{id}
        public async Task<IActionResult> Delete(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return NotFound();

            return View(role);
        }

        // POST: Admin/Roles/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return NotFound();

            // ==== MODIFICA 19/10/2026 - INIZIO ====
            if (EDiSistema(role.Name))
            {
                TempData["Errore"] = "I ruoli di sistema non possono essere eliminati.";
                return RedirectToAction(nameof(Index));
            }
            // ==== MODIFICA 19/10/2026 - FINE ====

            await _roleManager.DeleteAsync(role);

            return RedirectToAction(nameof(Index));
        }
    }
}