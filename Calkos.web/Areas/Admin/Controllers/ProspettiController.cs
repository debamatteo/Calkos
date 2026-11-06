using CalkosManager.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;//20/06/2026
[Area("Admin")]
[Authorize(Roles = "Admin,Operatore,Agente")]//20/06/2026
public class ProspettiController : Controller
{
    private readonly IMandatarioRepository _mandatarioRepository;

    public ProspettiController(IMandatarioRepository mandatarioRepository)
    {
        _mandatarioRepository = mandatarioRepository;
    }

    public IActionResult Index()
    {
        var mandatari = _mandatarioRepository.GetAll();
        return View(mandatari);
    }
}
