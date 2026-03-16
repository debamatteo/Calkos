using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Calkos.web.Controllers
{
    [Authorize(Roles = "Admin,Operatore")]
    public class ClientiController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
