using System.Globalization;
using Calkos.web.Models.Statistiche;
using Calkos.web.Services.Export;                    // StatisticheGlobaliExcelBuilder, ReportMerceConsegnataExcelBuilder
using CalkosManager.Application.DTO.Statistiche;
using CalkosManager.Application.Services;
using CalkosManager.Domain.Entities;                 // FiltriMerceConsegnataDdtDto, RigaMerceConsegnataDdtDto
using CalkosManager.Domain.Interfaces.Repositories;  // IReportMerceConsegnataRepository
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Calkos.web.Areas.Admin.Controllers
{
    /// <summary>
    /// Statistiche Globali (23/09/2026): report aggregati su TUTTI i mandatari.
    /// Flusso: Index (form filtri) -> Esporta (GET) -> file .xlsx.
    /// In caso di filtri non validi la vista viene ri-mostrata con gli errori.
    /// Include anche il report "Analisi Merce Consegnata Tramite DDT" (JSON per DataTables + export Excel).
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin,Operatore")]
    public class StatisticheGlobaliController : Controller
    {
        private const string ContentTypeXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private readonly StatisticheGlobaliService _statisticheService;
        private readonly StatisticheGlobaliExcelBuilder _excelBuilder;
        private readonly ILogger<StatisticheGlobaliController> _logger;

        private readonly IReportMerceConsegnataRepository _reportRepository;
        private readonly ReportMerceConsegnataExcelBuilder _reportDdtExcelBuilder;

        // Lookup per i filtri del report DDT
        private readonly IMandatarioRepository _mandatarioRepository;
        private readonly IClienteRepository _clienteRepository;
        private readonly IMaterialeRepository _materialeRepository;

        public StatisticheGlobaliController(
            StatisticheGlobaliService statisticheService,
            StatisticheGlobaliExcelBuilder excelBuilder,
            ILogger<StatisticheGlobaliController> logger,
            IReportMerceConsegnataRepository reportRepository,
            ReportMerceConsegnataExcelBuilder reportDdtExcelBuilder,
            IMandatarioRepository mandatarioRepository,
            IClienteRepository clienteRepository,
            IMaterialeRepository materialeRepository)
        {
            _statisticheService = statisticheService;
            _excelBuilder = excelBuilder;
            _logger = logger;
            _reportRepository = reportRepository;
            _reportDdtExcelBuilder = reportDdtExcelBuilder;
            _mandatarioRepository = mandatarioRepository;
            _clienteRepository = clienteRepository;
            _materialeRepository = materialeRepository;
        }

        // GET: /Admin/StatisticheGlobali
        [HttpGet]
        public IActionResult Index()
        {
            var anni = _statisticheService.GetAnniDisponibili();   // dal più recente
            int ultimoAnno = anni[0];

            var model = new StatisticheGlobaliViewModel
            {
                TipoReport = TipoReportStatistiche.FatturatoAnnuale,
                Anno = ultimoAnno,
                AnnoDa = Math.Max(anni[^1], ultimoAnno - 2),        // default: ultimi 3 anni
                AnnoA = ultimoAnno,
                AnniDisponibili = anni
            };

            CaricaLookupDdt();
            return View(model);
        }

        // GET: /Admin/StatisticheGlobali/Esporta?TipoReport=1&Anno=2026
        [HttpGet]
        public IActionResult Esporta(StatisticheGlobaliViewModel model)
        {
            if (!ModelState.IsValid)
                return RiMostraForm(model);

            try
            {
                return model.TipoReport switch
                {
                    TipoReportStatistiche.FatturatoAnnuale => File(
                        _excelBuilder.CreaReportFatturatoAnnuale(
                            _statisticheService.GetReportFatturatoAnnuale(model.Anno!.Value)),
                        ContentTypeXlsx,
                        $"Fatturato_Mandatari_{model.Anno}.xlsx"),

                    TipoReportStatistiche.ConfrontoAnni => File(
                        _excelBuilder.CreaReportConfrontoAnni(
                            _statisticheService.GetReportConfrontoAnni(model.AnnoDa!.Value, model.AnnoA!.Value)),
                        ContentTypeXlsx,
                        $"Confronto_Fatturato_{model.AnnoDa}_{model.AnnoA}.xlsx"),

                    _ => BadRequest("Tipo di report non valido.")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore generazione Statistiche Globali {@Filtri}", model);
                ModelState.AddModelError(string.Empty,
                    "Errore durante la generazione del report. Riprovare o contattare l'assistenza.");
                return RiMostraForm(model);
            }
        }

        // =====================================================================
        // ANALISI MERCE CONSEGNATA TRAMITE DDT
        // =====================================================================

        /// <summary>
        /// GET: /Admin/StatisticheGlobali/ReportMerceConsegnataDdtJson?Anno=2026&amp;Mese=3...
        /// Dati per DataTables (formato { data: [...] }, nomi camelCase espliciti).
        /// </summary>
        [HttpGet]
        public IActionResult ReportMerceConsegnataDdtJson()
        {
            try
            {
                var filtri = LeggiFiltriDdt();
                var righe = _reportRepository.GetReport(filtri);

                // Proiezione esplicita: i nomi non dipendono dalla naming policy JSON dell'app.
                var data = righe.Select(r => new
                {
                    mandatario = r.Mandatario,
                    cliente = r.Cliente,
                    materiale = r.Materiale,
                    anno = r.Anno,
                    mese = r.Mese,
                    diametro = r.Diametro,
                    spessore = r.Spessore,
                    larghezza = r.Larghezza,
                    quantitaTotale = r.QuantitaTotale,
                    unitaMisura = r.UnitaMisura
                });

                return Json(new { data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore ReportMerceConsegnataDdtJson");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Errore durante il caricamento del report." });
            }
        }

        /// <summary>
        /// GET: /Admin/StatisticheGlobali/EsportaReportMerceConsegnataDdtExcel?Anno=2026...
        /// </summary>
        [HttpGet]
        public IActionResult EsportaReportMerceConsegnataDdtExcel()
        {
            try
            {
                var filtri = LeggiFiltriDdt();
                var righe = _reportRepository.GetReport(filtri);

                var file = _reportDdtExcelBuilder.CreaReportMerceConsegnata(righe, filtri);

                return File(file, ContentTypeXlsx,
                    $"Merce_Consegnata_DDT_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore EsportaReportMerceConsegnataDdtExcel");
                TempData["ErroreDdt"] = "Errore durante l'esportazione Excel. Riprovare o contattare l'assistenza.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// GET: /Admin/StatisticheGlobali/ClientiPerMandatarioJson?idMandatario=3
        /// Alimenta la select Cliente (a cascata sul Mandatario) del report DDT.
        /// </summary>
        [HttpGet]
        public IActionResult ClientiPerMandatarioJson(int idMandatario)
        {
            try
            {
                if (idMandatario <= 0)
                    return Json(Array.Empty<object>());

                var clienti = _clienteRepository.GetByMandatario(idMandatario, 0)
                    .OrderBy(c => c.RagioneSociale)
                    .Select(c => new
                    {
                        id = c.IdCliente,
                        text = c.IsDeleted ? $"{c.RagioneSociale} (ELIMINATO)" : c.RagioneSociale
                    });

                return Json(clienti);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore ClientiPerMandatarioJson {IdMandatario}", idMandatario);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Errore durante il caricamento dei clienti." });
            }
        }

        /// <summary>Popola le select Mandatario e Materiale del report DDT (Cliente è a cascata via AJAX).</summary>
        private void CaricaLookupDdt()
        {
            ViewBag.Mandatari = _mandatarioRepository.GetAll()
                .OrderBy(m => m.NomeMandatario)
                .Select(m => new SelectListItem { Value = m.IdMandatario.ToString(), Text = m.NomeMandatario })
                .ToList();

            ViewBag.Materiali = _materialeRepository.GetAll()
                .OrderBy(m => m.DescrizioneMateriale)
                .Select(m => new SelectListItem { Value = m.IdMateriale.ToString(), Text = m.DescrizioneMateriale })
                .ToList();
        }

        /// <summary>
        /// Legge i filtri DDT direttamente dalla query string.
        /// Motivo: il model binding dei decimal usa la cultura corrente (it-IT), dove "12.5" verrebbe
        /// letto come 125 o scartato. Qui il parsing è sempre InvariantCulture (accetta anche la virgola).
        /// Valore assente, vuoto o non valido => null (nessun filtro).
        /// </summary>
        private FiltriMerceConsegnataDdtDto LeggiFiltriDdt()
        {
            var q = Request.Query;

            int? mese = ParseInt(q["Mese"]);
            if (mese is < 1 or > 12) mese = null;

            // Intervallo anni: se invertito lo raddrizzo invece di restituire un report vuoto
            int? annoDa = ParseInt(q["AnnoDa"]);
            int? annoA = ParseInt(q["AnnoA"]);
            if (annoDa.HasValue && annoA.HasValue && annoDa > annoA)
                (annoDa, annoA) = (annoA, annoDa);

            bool haFiltroMisure = ParseMisura(q["Diametro"]).HasValue
                               || ParseMisura(q["Spessore"]).HasValue
                               || ParseMisura(q["Larghezza"]).HasValue;

            return new FiltriMerceConsegnataDdtDto
            {
                IdMandatario = ParseInt(q["IdMandatario"]),
                IdCliente = ParseInt(q["IdCliente"]),
                IdMateriale = ParseInt(q["IdMateriale"]),
                AnnoDa = annoDa,
                AnnoA = annoA,
                Mese = mese,
                Diametro = ParseMisura(q["Diametro"]),
                Spessore = ParseMisura(q["Spessore"]),
                Larghezza = ParseMisura(q["Larghezza"]),

                // Raggruppamento: parametro assente => valore di default; filtrare per misura attiva le misure
                RaggruppaPerMandatario = ParseBool(q["RaggruppaPerMandatario"], true),
                RaggruppaPerCliente = ParseBool(q["RaggruppaPerCliente"], true),
                RaggruppaPerMateriale = ParseBool(q["RaggruppaPerMateriale"], true),
                RaggruppaPerAnno = ParseBool(q["RaggruppaPerAnno"], true),
                RaggruppaPerMese = ParseBool(q["RaggruppaPerMese"], true),
                RaggruppaPerMisure = haFiltroMisure || ParseBool(q["RaggruppaPerMisure"], false)
            };
        }

        private static bool ParseBool(string? valore, bool predefinito) =>
            string.IsNullOrWhiteSpace(valore)
                ? predefinito
                : valore == "1" || valore.Equals("true", StringComparison.OrdinalIgnoreCase);

        private static int? ParseInt(string? valore) =>
            int.TryParse(valore, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

        private static decimal? ParseMisura(string? valore)
        {
            if (string.IsNullOrWhiteSpace(valore)) return null;

            var normalizzato = valore.Trim().Replace(',', '.');
            return decimal.TryParse(normalizzato, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var v)
                   && v > 0
                ? Math.Round(v, 3)
                : null;
        }

        private IActionResult RiMostraForm(StatisticheGlobaliViewModel model)
        {
            model.AnniDisponibili = _statisticheService.GetAnniDisponibili();
            CaricaLookupDdt();
            return View(nameof(Index), model);
        }
    }
}
