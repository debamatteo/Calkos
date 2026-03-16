using CalkosManager.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

[Area("Admin")]
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
