using Calkos.web.Models;
using Calkos.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Calkos.web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // 1️⃣ Se NON è autenticato → mostra la Home pubblica
            if (!User.Identity.IsAuthenticated)
                return View();

            // 2️⃣ Recupera l'utente loggato
            var user = await _userManager.GetUserAsync(User);

            // 3️⃣ Se Admin → vai al pannello
            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

            // 4️⃣ Se Operatore → vai a Gestione Prospetti
            if (await _userManager.IsInRoleAsync(user, "Operatore"))
                return RedirectToAction("Index", "Prospetti", new { area = "Admin" });

            // 5️⃣ Default (ruoli futuri)
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
