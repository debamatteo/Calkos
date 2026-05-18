//la dashboard NON è un modulo Admin
//la dashboard NON è un modulo gestionale
//la dashboard è un modulo operativo → quindi sta nei controller principali
using Calkos.web.Models.Dashboard;
using Calkos.web.Services;
using Calkos.web.Services.Export;
using Calkos.Web.Helpers;
using CalkosManager.Application.Services; // MandatarioService
using CalkosManager.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;

namespace Calkos.web.Controllers
{
    public class DashboardProvvigioniController : Controller
    {
        private readonly DashboardProvvigioniService _dashboardService;
        private readonly MandatarioService _mandatarioService;
        private readonly ExcelExportService _excel;
        private readonly PdfExportService _pdf;
        private readonly IProspettoCobralRepository _prospettoCobralRepository;
        private readonly IProspettoDeAngeliRepository _prospettoDeAngeliRepository;
        private readonly IProspettoElektraWireRepository _prospettoElektraWireRepository;
        private readonly IProspettoCSTRepository _prospettoCSTRepository;
        private readonly IProspettoSystemCoreRepository _prospettoSystemCoreRepository;
        private readonly IProspettoSystemPRepository _prospettoSystemPRepository;
        private readonly IProspettoGuerzoniRepository _prospettoGuerzoniRepository;
        private readonly IProspettoTradingAndConsultingRepository _prospettoTradingRepository;
        private readonly IProspettoHitechRepository _prospettoHitechRepository;





        private readonly AgenteService _agenteService;

        /// <summary>
        /// Il controller riceve i servizi tramite Dependency Injection.
        /// </summary>
        //public DashboardProvvigioniController(
        //    DashboardProvvigioniService dashboardService,
        //    MandatarioService mandatarioService,
        //    IProspettoCobralRepository prospettoCobralRepository,
        //    AgenteService agenteService)
        //{
        //    _dashboardService = dashboardService;
        //    _mandatarioService = mandatarioService;
        //    _prospettoCobralRepository = prospettoCobralRepository;
        //    _excel = new ExcelExportService();
        //    _pdf = new PdfExportService();
        //    _agenteService = agenteService;
        //}
        public DashboardProvvigioniController(
            DashboardProvvigioniService dashboardService,
            MandatarioService mandatarioService,
            IProspettoCobralRepository prospettoCobralRepository,
            IProspettoDeAngeliRepository prospettoDeAngeliRepository,
            IProspettoElektraWireRepository prospettoElektraWireRepository,
            IProspettoCSTRepository prospettoCSTRepository,
            IProspettoSystemCoreRepository prospettoSystemCoreRepository,
            IProspettoSystemPRepository prospettoSystemPRepository,
            IProspettoGuerzoniRepository prospettoGuerzoniRepository,
            IProspettoTradingAndConsultingRepository prospettoTradingRepository,
            IProspettoHitechRepository prospettoHitechRepository,
            AgenteService agenteService)
        {
            _dashboardService = dashboardService;
            _mandatarioService = mandatarioService;

            _prospettoCobralRepository = prospettoCobralRepository;
            _prospettoDeAngeliRepository = prospettoDeAngeliRepository;
            _prospettoElektraWireRepository = prospettoElektraWireRepository;
            _prospettoCSTRepository = prospettoCSTRepository;
            _prospettoSystemCoreRepository = prospettoSystemCoreRepository;
            _prospettoSystemPRepository = prospettoSystemPRepository;
            _prospettoGuerzoniRepository = prospettoGuerzoniRepository;
            _prospettoTradingRepository = prospettoTradingRepository;
            _prospettoHitechRepository = prospettoHitechRepository;

            _excel = new ExcelExportService();
            _pdf = new PdfExportService();
            _agenteService = agenteService;
        }

        // ============================================================================
        // METODO DI SUPPORTO: RECUPERO NOME STORED PROCEDURE DINAMICA
        // ============================================================================
        /// <summary>
        /// Identifica il nome della Stored Procedure SQL da eseguire in base al Mandatario.
        /// Centralizzare questa logica previene errori di disallineamento (mismatch) 
        /// tra la visualizzazione a video (Index) e i file generati (Export Excel/PDF).
        /// </summary>
        /// <param name="idMandatario">ID univoco del mandatario selezionato</param>
        /// <returns>Stringa contenente il nome della SP o stringa vuota se non trovato</returns>
        private string GetNomeSpPerMandatario(int idMandatario)
        {
            // 1. Recupero l'oggetto mandatario dal database tramite il service
            var mandatario = _mandatarioService.GetById(idMandatario);

            // 2. Validazione: se il mandatario non esiste, restituisco stringa vuota per evitare errori a valle
            if (mandatario == null)
            {
                return string.Empty;
            }

            // 3. Mappatura Codice Mandatario -> Nome Stored Procedure
            switch (mandatario.CodiceMandatario?.ToLower())
            {
                case "cobral":
                    return "spDashboardProvvigioni_Cobral";

                case "deangeli":
                    return "spDashboardProvvigioni_DeAngeli";

                case "systemcore":
                    return "spDashboardProvvigioni_SystemCore";

                case "cst":
                    return "spDashboardProvvigioni_CST";

                case "elektrawire":
                    return "spDashboardProvvigioni_ElektraWire";

                case "systemp":
                    return "spDashboardProvvigioni_SystemP";

                case "guerzoni":
                    return "spDashboardProvvigioni_Guerzoni";

                case "hitech":
                    return "spDashboardProvvigioni_Hitech";

                case "tradingandconsulting":
                    return "spDashboardProvvigioni_TradingAndConsulting";

                default:
                    return "";
            }

        }

        /// <summary>
        /// Vista principale della dashboard.
        /// Carica filtri, mandatari, agenti, dati specifici del mandatario e dati clienti.
        /// </summary>
        //public IActionResult Index(int anno = 0, int mese = 0, int idMandatario = 0, int? idAgente = null, int? fatturata = null)
        //{
        //    // 0. GESTIONE DEFAULT FILTRI
        //    if (anno == 0) anno = DateTime.Now.Year;
        //    if (mese == 0) mese = DateTime.Now.Month;
        //    if (!fatturata.HasValue) fatturata = 0;

        //    int? filtroDatabase = (fatturata == -1) ? null : fatturata;

        //    var vm = new DashboardProvvigioniViewModel
        //    {
        //        Anno = anno,
        //        Mese = mese,
        //        IdMandatario = idMandatario,
        //        IdAgente = idAgente,
        //        Fatturata = fatturata
        //    };

        //    // LISTA MESI
        //    var culture = new CultureInfo("it-IT");
        //    ViewBag.Mesi = Enumerable.Range(1, 12)
        //        .Select(m => new SelectListItem
        //        {
        //            Value = m.ToString(),
        //            Text = culture.DateTimeFormat.GetMonthName(m).ToUpper(),
        //            Selected = (m == vm.Mese)
        //        }).ToList();

        //    // 1. POPOLA LISTA MANDATARI
        //    var tuttiIMandatari = _mandatarioService.GetAll();
        //    vm.Mandatari = tuttiIMandatari.Select(m => new MandatarioSelectItem
        //    {
        //        IdMandatario = m.IdMandatario,
        //        Nome = m.NomeMandatario
        //    }).OrderBy(m => m.Nome).ToList();

        //    if (vm.IdMandatario == 0 && vm.Mandatari.Any())
        //        vm.IdMandatario = vm.Mandatari.First().IdMandatario;

        //    // 2.1 RECUPERO CODICE E SP
        //    var mandatarioCorrente = tuttiIMandatari.FirstOrDefault(m => m.IdMandatario == vm.IdMandatario);
        //    if (mandatarioCorrente == null) return View(vm);

        //    string nomeSpClienti = GetNomeSpPerMandatario(vm.IdMandatario);

        //    // 2.2 CARICO I DATI PROSPETTO (Switch per i dati testata/mandatario)

        //    vm.DatiMandatario = _dashboardService.GetDatiDashBoard(mandatarioCorrente.CodiceMandatario.ToString(), anno, mese, filtroDatabase);

        //    //switch (mandatarioCorrente.CodiceMandatario)
        //    //{
        //    //    case "Cobral":
        //    //        vm.DatiMandatario = _dashboardService.GetDatiCobral(anno, mese, filtroDatabase);

        //    //        break;
        //    //    // Aggiungere altri case man mano che i Service specifici sono pronti
        //    //    default:
        //    //        vm.DatiMandatario = new List<DashboardRigaDTO>();
        //    //        break;
        //    //}

        //    // 2.3 USCITA ANTICIPATA SE VUOTO
        //    if (vm.DatiMandatario == null || !vm.DatiMandatario.Any())
        //    {
        //        vm.Agenti = new List<AgenteSelectItem>();
        //        vm.DatiClienti = new List<RigaCliente>();
        //        return View(vm);
        //    }

        //    // 3. POPOLA AGENTI (DINAMICO)
        //    vm.Agenti = vm.DatiMandatario
        //        .GroupBy(x => new { x.IdAgente, x.AgenteDescrizione })
        //        .Select(g => new AgenteSelectItem
        //        {
        //            IdAgente = g.Key.IdAgente,
        //            AgenteDescrizione = g.Key.AgenteDescrizione
        //        }).OrderBy(a => a.AgenteDescrizione).ToList();

        //    // 4. FILTRO AGENTE IN MEMORIA
        //    if (idAgente.HasValue)
        //        vm.DatiAgente = vm.DatiMandatario.Where(x => x.IdAgente == idAgente.Value).ToList();

        //    // 5. CARICO DATI CLIENTI (SP DINAMICA)
        //    vm.DatiClienti = _dashboardService.GetClienti(nomeSpClienti, anno, mese, idAgente, filtroDatabase);

        //    // 6. PROVVIGIONI AGENTE
        //    vm.DatiProvvigioniAgente = vm.DatiClienti.Where(x => idAgente == null || x.IdAgente == idAgente.Value).ToList();

        //    // 7. RAGGRUPPAMENTI E AJAX
        //    vm.CostruisciRaggruppamentiClienti();

        //    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        //        return PartialView("_TabellaProvvigioni", vm);

        //    return View(vm);
        //}
        public IActionResult Index(int anno = 0, int mese = 0, int idMandatario = 0, int? idAgente = null, int? fatturata = null)
        {
            // 0. GESTIONE DEFAULT FILTRI
            if (anno == 0) anno = DateTime.Now.Year;
            if (mese == 0) mese = DateTime.Now.Month;
            if (!fatturata.HasValue) fatturata = 0;

            int? filtroDatabase = (fatturata == -1) ? null : fatturata;

            var vm = new DashboardProvvigioniViewModel
            {
                Anno = anno,
                Mese = mese,
                IdMandatario = idMandatario,
                IdAgente = idAgente,
                Fatturata = fatturata
            };

            // LISTA MESI
            var culture = new CultureInfo("it-IT");
            ViewBag.Mesi = Enumerable.Range(1, 12)
                .Select(m => new SelectListItem
                {
                    Value = m.ToString(),
                    Text = culture.DateTimeFormat.GetMonthName(m).ToUpper(),
                    Selected = (m == vm.Mese)
                }).ToList();

            // 1. POPOLA LISTA MANDATARI
            var tuttiIMandatari = _mandatarioService.GetAll();
            vm.Mandatari = tuttiIMandatari.Select(m => new MandatarioSelectItem
            {
                IdMandatario = m.IdMandatario,
                Nome = m.NomeMandatario
            }).OrderBy(m => m.Nome).ToList();

            if (vm.IdMandatario == 0 && vm.Mandatari.Any())
                vm.IdMandatario = vm.Mandatari.First().IdMandatario;

            // 2.1 RECUPERO CODICE E SP
            var mandatarioCorrente = tuttiIMandatari.FirstOrDefault(m => m.IdMandatario == vm.IdMandatario);
            if (mandatarioCorrente == null) return View(vm);

            string nomeSpClienti = GetNomeSpPerMandatario(vm.IdMandatario);

            // 2.2 CARICO I DATI PROSPETTO
            vm.DatiMandatario = _dashboardService.GetDatiDashBoard(mandatarioCorrente.CodiceMandatario.ToString(), anno, mese, filtroDatabase, idAgente);

            // ============================================================
            // 2.3 USCITA ANTICIPATA SE VUOTO (PUNTO CRITICO RISOLTO)
            // ============================================================
            if (vm.DatiMandatario == null || !vm.DatiMandatario.Any())
            {
                vm.Agenti = new List<AgenteSelectItem>();
                vm.DatiClienti = new List<RigaCliente>();

                // Se la richiesta è AJAX, dobbiamo restituire solo la Partial anche se è vuota!
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return PartialView("_TabellaProvvigioni", vm);

                return View(vm);
            }

            // 3. POPOLA AGENTI (DINAMICO)
            vm.Agenti = vm.DatiMandatario
                .GroupBy(x => new { x.IdAgente, x.AgenteDescrizione })
                .Select(g => new AgenteSelectItem
                {
                    IdAgente = g.Key.IdAgente,
                    AgenteDescrizione = g.Key.AgenteDescrizione
                }).OrderBy(a => a.AgenteDescrizione).ToList();

            // 4. FILTRO AGENTE IN MEMORIA
            if (idAgente.HasValue)
                vm.DatiAgente = vm.DatiMandatario.Where(x => x.IdAgente == idAgente.Value).ToList();

            // 5. CARICO DATI CLIENTI (SP DINAMICA)
            vm.DatiClienti = _dashboardService.GetClienti(nomeSpClienti, anno, mese, idAgente, filtroDatabase);

            // 6. PROVVIGIONI AGENTE
            //vm.DatiProvvigioniAgente = vm.DatiClienti.Where(x => idAgente == null || x.IdAgente == idAgente.Value).ToList();

            vm.DatiProvvigioniAgente = vm.DatiClienti
                .Where(x => x.IdAgente != null) // esclude i null
                .Where(x => !idAgente.HasValue || x.IdAgente == idAgente)
                .ToList();



            // 7. RAGGRUPPAMENTI E INVIO RISPOSTA
            vm.CostruisciRaggruppamentiClienti();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("_TabellaProvvigioni", vm);

            return View(vm);
        }
        public IActionResult EsportaClientiExcel(string tableId, string righe, int anno, int mese, int? fatturata, string nomeSoggetto, int idMandatario)
        {
            var ids = righe.Split(',').Select(int.Parse).ToList();
            int? filtroDB = (fatturata == -1) ? null : fatturata;

            // Recupero SP Dinamica per evitare errore CS1503
            string nomeSp = GetNomeSpPerMandatario(idMandatario);
            var model = _dashboardService.GetRigheClientiPerStampa(nomeSp, anno, mese, tableId, ids, filtroDB);

            string parteMese = mese > 0 ? Utility.NomeMese(mese) + " " : "";
            string parteAnno = anno > 0 ? anno.ToString() + "_" : "";
            var titolo = $"{parteAnno}{parteMese}";

            var file = _excel.CreaExcelClienti(model, $"{titolo}{tableId} {nomeSoggetto}");

            return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Clienti_{tableId}_{nomeSoggetto}.xlsx");
        }

        public IActionResult EsportaClientiPdf(string tableId, string righe, int anno, int mese, int? fatturata, string nomeSoggetto, int idMandatario)
        {
            var ids = righe.Split(',').Select(int.Parse).ToList();
            int? filtroDB = (fatturata == -1) ? null : fatturata;

            // Recupero SP Dinamica
            string nomeSp = GetNomeSpPerMandatario(idMandatario);
            var model = _dashboardService.GetRigheClientiPerStampa(nomeSp, anno, mese, tableId, ids, filtroDB);

            string parteMese = mese > 0 ? Utility.NomeMese(mese).ToUpper() + " " : "";
            string parteAnno = anno > 0 ? anno.ToString() + " " : "";
            var titolo = $"{parteMese}{parteAnno}";

            var file = _pdf.CreaPdfClienti(model, $"Clienti {titolo}{tableId} {nomeSoggetto}");

            return File(file, "application/pdf", $"Clienti_{tableId}_{nomeSoggetto}.pdf");
        }

        public IActionResult EsportaAgentiExcel(string tableId, int anno, int mese, int? fatturata, string nomeSoggetto, int idMandatario, int? idAgente)//AGGIUNTO idAgente
        {
            int? filtroDB = (fatturata == -1) ? null : fatturata;

            // Recupero SP Dinamica
            string nomeSp = GetNomeSpPerMandatario(idMandatario);
            var model = _dashboardService.GetRigheAgentiPerStampa(nomeSp, anno, mese, tableId, filtroDB, idAgente);

            string nomePulito = nomeSoggetto?.Replace(" ", "_") ?? "-";
            string parteMese = mese > 0 ? Utility.NomeMese(mese).ToUpper() + " " : "";
            string parteAnno = anno > 0 ? anno.ToString() + "_" : "";
            nomeSoggetto = $"{parteAnno}{parteMese}{nomePulito}";

            var file = _excel.CreaExcelAgenti(model, $"{nomeSoggetto} - Scadenza {tableId}");

            return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Agenti_{nomeSoggetto}_{tableId}.xlsx");
        }

        public IActionResult EsportaAgentiPdf(string tableId, int anno, int mese, int? fatturata, string nomeSoggetto, int idMandatario , int? idAgente)   // <--- AGGIUNTO QUI
        {
            int? filtroDB = (fatturata == -1) ? null : fatturata;

            // Recupero SP Dinamica
            string nomeSp = GetNomeSpPerMandatario(idMandatario);
            var model = _dashboardService.GetRigheAgentiPerStampa(nomeSp, anno, mese, tableId, filtroDB, idAgente);

            string nomePulito = nomeSoggetto?.Replace(" ", "_") ?? "Report";
            string parteMese = mese > 0 ? Utility.NomeMese(mese).ToUpper() + " " : "";
            string parteAnno = anno > 0 ? anno.ToString() + "_" : "";
            nomeSoggetto = $"{parteAnno}{parteMese}{nomePulito}";

            var file = _pdf.CreaPdfAgenti(model, $"{nomeSoggetto} - Scadenza {tableId}");

            return File(file, "application/pdf", $"Agente_{nomeSoggetto}_{tableId}.pdf");
        }


        private void ChiamaRepositoryFatturazione(int idMandatario, int anno, int mese, int idCliente, int idTipoPagamento)
        {
            var mandatario = _mandatarioService.GetById(idMandatario);
            if (mandatario == null)
                throw new Exception("Mandatario non valido.");

            switch (mandatario.CodiceMandatario?.ToLower())
            {
                case "cobral":
                    _prospettoCobralRepository.ImpostaComeFatturate(idMandatario, anno, mese, idCliente, idTipoPagamento);
                    break;

                case "deangeli":
                    _prospettoDeAngeliRepository.ImpostaComeFatturate(idMandatario, anno, mese, idCliente, idTipoPagamento);
                    break;

                case "elektrawire":
                    _prospettoElektraWireRepository.ImpostaComeFatturate(idMandatario, anno, mese, idCliente, idTipoPagamento);
                    break;

                case "cst":
                    _prospettoCSTRepository.ImpostaComeFatturate(idMandatario, anno, mese, idCliente, idTipoPagamento);
                    break;

                case "systemcore":
                    _prospettoSystemCoreRepository.ImpostaComeFatturate(idMandatario, anno, mese, idCliente, idTipoPagamento);
                    break;

                case "systemp":
                    _prospettoSystemPRepository.ImpostaComeFatturate(idMandatario, anno, mese, idCliente, idTipoPagamento);
                    break;

                case "guerzoni":
                    _prospettoGuerzoniRepository.ImpostaComeFatturate(idMandatario, anno, mese, idCliente, idTipoPagamento);
                    break;

                case "tradingandconsulting":
                    _prospettoTradingRepository.ImpostaComeFatturate(idMandatario, anno, mese, idCliente, idTipoPagamento);
                    break;

                case "hitech":
                    _prospettoHitechRepository.ImpostaComeFatturate(idMandatario, anno, mese, idCliente, idTipoPagamento);
                    break;

                default:
                    throw new Exception($"Mandatario non supportato: {mandatario.CodiceMandatario}");
            }

        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult ImpostaComeFatturate([FromBody] EmissioneFatturaRequest request)
        {
            var mandatario = _mandatarioService.GetById(request.IdMandatario);
            string nomeMandatario = mandatario?.NomeMandatario ?? "Mandatario";

            if (request.RigheSelezionate == null || !request.RigheSelezionate.Any())
                return BadRequest("Nessuna riga selezionata.");

            foreach (var riga in request.RigheSelezionate)
            {
                ChiamaRepositoryFatturazione(
                    request.IdMandatario,
                    request.Anno,
                    request.Mese,
                    riga.IdCliente,
                    riga.IdTipoPagamento
                );
            }

            string parteMese = request.Mese > 0 ? Utility.NomeMese(request.Mese).ToUpper() + " " : "";
            string parteAnno = request.Anno > 0 ? request.Anno.ToString() + " " : "";
            string titolo = $" {parteAnno}{parteMese}";

            var file = _excel.CreaExcelClienti(request.RigheSelezionate, "Fatture Emesse " + nomeMandatario + titolo);
            return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Emissione.xlsx");
        }



        //[HttpPost]
        //[Authorize(Roles = "Admin")]
        //public IActionResult ImpostaComeFatturate([FromBody] EmissioneFatturaRequest request)
        //{
        //    var mandatario = _mandatarioService.GetById(request.IdMandatario);
        //    string nomeMandatario = mandatario?.NomeMandatario ?? "Mandatario";

        //    if (request == null || request.RigheSelezionate == null || !request.RigheSelezionate.Any())
        //        return BadRequest("Dati non validi o nessuna riga selezionata.");

        //    string parteMese = request.Mese > 0 ? Utility.NomeMese(request.Mese).ToUpper() + " " : "";
        //    string parteAnno = request.Anno > 0 ? request.Anno.ToString() + " " : "";
        //    string titolo = $" {parteAnno}{parteMese}";

        //    foreach (var riga in request.RigheSelezionate)
        //    {
        //        _prospettoCobralRepository.ImpostaComeFatturate(
        //            request.IdMandatario, request.Anno, request.Mese, riga.IdCliente,
        //            riga.IdTipoPagamento);   // ✔ CORRETTOrequest.IdTipoPagamento);
        //    }

        //    var file = _excel.CreaExcelClienti(request.RigheSelezionate, "Fatture Emesse " + nomeMandatario + titolo);
        //    return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Emissione.xlsx");
        //}
    }
}