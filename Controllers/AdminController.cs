using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Calkos.Web.Models;

namespace Calkos.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // DTO per mappare correttamente il JSON della fetch
        public class ResetPasswordRequest { public string Email { get; set; } }

        [HttpPost]
        [ValidateAntiForgeryToken] // Necessario perché hai il filtro globale in Program.cs
        public async Task<IActionResult> ResetUserPassword([FromBody] ResetPasswordRequest data)
        {
            if (data == null || string.IsNullOrEmpty(data.Email))
                return Json(new { success = false, message = "Dati non validi." });

            var user = await _userManager.FindByEmailAsync(data.Email);
            if (user == null)
                return Json(new { success = false, message = "Utente non trovato." });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, "Provvisoria123!");

            if (!result.Succeeded)
                return Json(new { success = false, message = "Errore nel reset della password." });

            user.RequirePasswordChange = true;
            await _userManager.UpdateAsync(user);

            return Json(new { success = true, message = "\"Password resettata. Password temporanea: Provvisoria123!" });
        }
    }
}