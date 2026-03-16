using Microsoft.AspNetCore.Mvc;

namespace Calkos.web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TestController : Controller
    {
        public IActionResult DataTables()
        {
            return View();
        }
    }
}
