
//la dashboard NON è un modulo Admin
//la dashboard NON è un modulo gestionale
//la dashboard è un modulo operativo → quindi sta nei controller principali
using Azure.Core;
using Calkos.web.Models.Dashboard;
using Calkos.web.Models.Dashboard;
using Calkos.web.Services;
using Calkos.web.Services;
using Calkos.web.Services.Export;
using Calkos.Web.Helpers;
using CalkosManager.Application.Services; // MandatarioService
using CalkosManager.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        private readonly AgenteService _agenteService;

        /// <summary>
        /// Il controller riceve i servizi tramite Dependency Injection.
        /// </summary>
        public DashboardProvvigioniController(
            DashboardProvvigioniService dashboardService,
            MandatarioService mandatarioService,
            IProspettoCobralRepository prospettoCobralRepository,
            AgenteService agenteService)
        {
            _dashboardService = dashboardService;
            _mandatarioService = mandatarioService;
            _prospettoCobralRepository = prospettoCobralRepository;
            _excel = new ExcelExportService();
            _pdf = new PdfExportService();
            _agenteService = agenteService;
        }

        /// <summary>
        /// Vista principale della dashboard.
        /// Carica filtri, mandatari, agenti, dati Cobral e dati clienti.
        /// Aggiornato per gestire il filtro Stato Fatturazione e caricamento AJAX (Partial View).
        /// </summary>
        public IActionResult Index(int anno = 0, int mese = 0, int idMandatario = 0, int? idAgente = null, int? fatturata = null)
        {
            // ============================================================
            // 0. GESTIONE DEFAULT FILTRI
            // ============================================================

            // Se non arrivano parametri temporali, usa quelli correnti
            if (anno == 0) anno = DateTime.Now.Year;
            if (mese == 0) mese = DateTime.Now.Month;

            // Logica Operativa: Se il filtro fatturata è null (primo accesso), 
            // mostriamo solo le "Non Fatturate" (0) per focus operativo.
            if (!fatturata.HasValue) fatturata = 0;

            // Traduzione per il Database/Service:
            // - Se l'utente sceglie "Tutti" (-1 dalla View), passiamo null alla SP.
            // - Altrimenti passiamo il valore selezionato (0 o 1).
            int? filtroDatabase = (fatturata == -1) ? null : fatturata;

            var vm = new DashboardProvvigioniViewModel
            {
                Anno = anno,
                Mese = mese,
                IdMandatario = idMandatario,
                IdAgente = idAgente,
                Fatturata = fatturata // Manteniamo lo stato scelto dall'utente per la View
            };

            // ============================
            // LISTA MESI (NOME → VALUE NUMERICO)
            // ============================
            var culture = new CultureInfo("it-IT");

            ViewBag.Mesi = Enumerable.Range(1, 12)
                .Select(m => new SelectListItem
                {
                    Value = m.ToString(),
                    Text = culture.DateTimeFormat.GetMonthName(m).ToUpper(),
                    Selected = (m == vm.Mese)
                })
                .ToList();

            // ============================================================
            // 1. POPOLA LISTA MANDATARI DAL DATABASE
            // ============================================================
            vm.Mandatari = _mandatarioService.GetAll()
                .Select(m => new MandatarioSelectItem
                {
                    IdMandatario = m.IdMandatario,
                    Nome = m.NomeMandatario
                })
                .OrderBy(m => m.Nome)
                .ToList();

            if (vm.IdMandatario == 0 && vm.Mandatari.Any())
                vm.IdMandatario = vm.Mandatari.First().IdMandatario;

            // ============================================================
            // 2. CARICO I DATI DEL MANDATARIO (Filtrati per Stato Fattura)
            // ============================================================
            switch (vm.IdMandatario)
            {
                case 2: // Cobral
                    // Passiamo il filtroDatabase (null, 0 o 1) al service
                    vm.DatiMandatario = _dashboardService.GetDatiCobral(anno, mese, filtroDatabase);
                    break;

                default:
                    vm.DatiMandatario = new List<DashboardRigaDTO>();
                    break;
            }

            // ============================================================
            // 3. POPOLA LISTA AGENTI (DINAMICO)
            // ============================================================

            // Se ci sono dati → prendo gli agenti dai dati
            if (vm.DatiMandatario.Any())
            {
                vm.Agenti = vm.DatiMandatario
                    .GroupBy(x => new { x.IdAgente, x.AgenteDescrizione })
                    .Select(g => new AgenteSelectItem
                    {
                        IdAgente = g.Key.IdAgente,
                        AgenteDescrizione = g.Key.AgenteDescrizione
                    })
                    .OrderBy(a => a.AgenteDescrizione)
                    .ToList();
            }
            else
            {
                // Se NON ci sono dati → prendo gli agenti dal DB
                vm.Agenti = _agenteService.GetAll()
                    .Select(a => new AgenteSelectItem
                    {
                        IdAgente = a.IdAgente,
                        AgenteDescrizione = a.AgenteDescrizione
                    })
                    .OrderBy(a => a.AgenteDescrizione)
                    .ToList();
            }

            // ============================================================
            // 4. SE È SELEZIONATO UN AGENTE → FILTRO I DATI IN MEMORIA
            // ============================================================
            if (idAgente.HasValue)
            {
                vm.DatiAgente = vm.DatiMandatario
                    .Where(x => x.IdAgente == idAgente.Value)
                    .ToList();
            }

            // ============================================================
            // 5. CARICO I DATI CLIENTI (Filtrati per Stato Fattura)
            // ============================================================
            // Passiamo filtroDatabase anche qui per coerenza nei totali clienti
            vm.DatiClienti = _dashboardService.GetClienti(anno, mese, idAgente, filtroDatabase);

            // ============================================================
            // 6. CARICO I DATI PROVVIGIONI AGENTE
            // ============================================================
            vm.DatiProvvigioniAgente = vm.DatiClienti
                .Where(x => idAgente == null || x.IdAgente == idAgente.Value)
                .ToList();

            // ============================================================
            // 7. COSTRUISCO I RAGGRUPPAMENTI DINAMICI
            // ============================================================
            vm.CostruisciRaggruppamentiClienti();
            bool isAjax = false;
            // --- MODIFICA AGGRESSIVA: Se arriva isAjax=true, sputa solo la tabella ---
            if (isAjax || Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // Usiamo PartialView per ignorare il _Layout (menu, header, login, ecc.)
                return PartialView("_TabellaProvvigioni", vm);
            }

            // Caricamento normale (primo accesso alla pagina)
            return View(vm);
        }




        // ============================================================================
        //  ESPORTA IN EXCEL LE RIGHE CLIENTI SELEZIONATE NELLA DASHBOARD
        // ============================================================================
        //  Questo metodo viene chiamato dal pulsante "Esporta Excel" della dashboard.
        //  - tableId   = tipo pagamento (30 / 60 / 90)
        //  - righe     = lista di ID cliente selezionati (es: "12,45,88")
        //  - anno/mese = periodo filtrato nella dashboard
        //  
        //  Flusso:
        //  1. Converte la stringa "1,2,3" in lista di interi
        //  2. Recupera SOLO le righe selezionate tramite il service
        //  3. Genera il file Excel tramite ExcelExportService
        //  4. Restituisce il file al browser come download
        // ============================================================================
        public IActionResult EsportaClientiExcel(string tableId, string righe, int anno, int mese, int? fatturata,string nomeSoggetto)
        {
            // ---------------------------------------------------------
            // 1. Converte "1,2,3" → List<int> {1,2,3}
            // ---------------------------------------------------------
            var ids = righe.Split(',').Select(int.Parse).ToList();

            // ---------------------------------------------------------
            // NUOVO: Gestione filtro stato fatturazione per l'export
            // ---------------------------------------------------------
            int? filtroDB = (fatturata == -1) ? null : fatturata;

            // ---------------------------------------------------------
            // 2. Recupera SOLO le righe selezionate per la stampa
            //    Usa il metodo GetRigheClientiPerStampa del service
            //    Passiamo il filtroDB per coerenza con la vista a video
            // ---------------------------------------------------------
            var model = _dashboardService.GetRigheClientiPerStampa(anno, mese, tableId, ids, filtroDB);

            // ---------------------------------------------------------
            // 3. Genera il file Excel in memoria
            //    _excel è un ExcelExportService
            // ---------------------------------------------------------


            // 2.mese
            string parteMese = mese > 0
                ? Utility.NomeMese(mese) + " "
                : "";

            // 3. Recupero l'anno (solo se anno > 0)
            string parteAnno = (anno > 0)
                ? anno.ToString() + "_"
                : "";

            // 4. Componiamo la stringa finale
            // Risultato atteso: 2026_MARZO_FABIO_FABIO_30GG oppure FABIO_FABIO_30GG
            var titolo = $"{parteAnno}{parteMese}";





            var file = _excel.CreaExcelClienti(model, $"{titolo}{tableId} {nomeSoggetto}");

            // ---------------------------------------------------------
            // 4. Restituisce il file al browser come download .xlsx
            // ---------------------------------------------------------
            return File(
                file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Clienti_{tableId}_{nomeSoggetto}.xlsx"
            );
        }

        // ============================================================================
        //  ESPORTA IN PDF LE RIGHE CLIENTI SELEZIONATE NELLA DASHBOARD
        // ============================================================================
        //  Identico a EsportaClientiExcel, ma genera un PDF invece di un Excel.
        // ============================================================================
        public IActionResult EsportaClientiPdf(string tableId, string righe, int anno, int mese, int? fatturata, string nomeSoggetto)
        {
            // 1. Converte "1,2,3" → List<int> {1,2,3}
            var ids = righe.Split(',').Select(int.Parse).ToList();

            // NUOVO: Gestione filtro stato fatturazione per l'export
            int? filtroDB = (fatturata == -1) ? null : fatturata;

            // 2. Recupera SOLO le righe selezionate
            var model = _dashboardService.GetRigheClientiPerStampa(anno, mese, tableId, ids, filtroDB);





            // 2.mese
            string parteMese = mese > 0
                ? Utility.NomeMese(mese).ToUpper() + " "
                : "";

            // 3. Recupero l'anno (solo se anno > 0)
            string parteAnno = (anno > 0)
                ? anno.ToString() + " "
                : "";

            // 4. Componiamo la stringa finale
            
            var titolo = $"{parteMese}{parteAnno}";






            // 3. Genera il PDF
            var file = _pdf.CreaPdfClienti(model, $"Clienti {titolo}{tableId} {nomeSoggetto}");

            // 4. Restituisce il file al browser come download .pdf
            return File(
                file,
                "application/pdf",
                $"Clienti_{tableId}_{nomeSoggetto}.pdf"
            );
        }

        // ============================================================================
        //  ESPORTA IN EXCEL LE RIGHE AGENTI (TUTTE LE RIGHE, NON SOLO SELEZIONATE)
        // ============================================================================
        //  Gli agenti NON hanno selezione riga-per-riga.
        //  Si esportano sempre tutte le righe del tipo pagamento.
        // ============================================================================
        public IActionResult EsportaAgentiExcel(string tableId, int anno, int mese, int? fatturata, string nomeSoggetto)
        {
            // NUOVO: Gestione filtro stato fatturazione per l'export
            int? filtroDB = (fatturata == -1) ? null : fatturata;

            // 1. Recupera TUTTE le righe dell'agente per quel tipo pagamento
            // Passiamo il filtroDB per rispettare la selezione della dashboard
            var model = _dashboardService.GetRigheAgentiPerStampa(anno, mese, tableId, filtroDB);

            // 1. Pulizia iniziale del nome (sostituisce spazi con underscore)
            string nomePulito = nomeSoggetto?.Replace(" ", "_") ?? "-";

            // 2.mese
            string parteMese = mese > 0
                ? Utility.NomeMese(mese).ToUpper() + " "
                : "";

            // 3. Recupero l'anno (solo se anno > 0)
            string parteAnno = (anno > 0)
                ? anno.ToString() + "_"
                : "";

            // 4. Componiamo la stringa finale
            // Risultato atteso: 2026_MARZO_FABIO_FABIO_30GG oppure FABIO_FABIO_30GG
            nomeSoggetto = $"{parteAnno}{parteMese}{nomePulito}";

            // 5. Generazione del file (passando il nome al Service)

            //var file = _excel.CreaExcelAgenti(model, $"Agenti {tableId}");
            var file = _excel.CreaExcelAgenti(model, $"{nomeSoggetto} - Scadenza {tableId}");
            // 3. Restituisce il file al browser come download .xlsx
            return File(
                file,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
               $"Agenti_{nomeSoggetto}_{tableId}.xlsx");
       
        }

        // ============================================================================
        //  ESPORTA IN PDF LE RIGHE AGENTI (TUTTE LE RIGHE, NON SOLO SELEZIONATE)
        // ============================================================================
        public IActionResult EsportaAgentiPdf(string tableId, int anno, int mese, int? fatturata, string nomeSoggetto)
        {
            // NUOVO: Gestione filtro stato fatturazione per l'export
            int? filtroDB = (fatturata == -1) ? null : fatturata;

            // 1. Recupera TUTTE le righe dell'agente
            var model = _dashboardService.GetRigheAgentiPerStampa(anno, mese, tableId, filtroDB);


            // 1. Pulizia iniziale del nome (sostituisce spazi con underscore)
            string nomePulito = nomeSoggetto?.Replace(" ", "_") ?? "Report";

            // 2.mese
            string parteMese = mese > 0
                ? Utility.NomeMese(mese).ToUpper() + " "
                : "";

            // 3. Recupero l'anno (solo se anno > 0)
            string parteAnno = (anno > 0)
                ? anno.ToString() + "_"
                : "";

            // 4. Componiamo la stringa finale
            // Risultato atteso: 2026_MARZO_FABIO_FABIO_30GG oppure FABIO_FABIO_30GG
            nomeSoggetto = $"{parteAnno}{parteMese}{nomePulito}";


            // 2. Genera il PDF
            //var file = _pdf.CreaPdfAgenti(model, $"Agenti {tableId}");
            var file = _pdf.CreaPdfAgenti(model, $"{nomeSoggetto} - Scadenza {tableId}");

            // 3. Restituisce il file al browser come download .pdf
            return File(file, "application/pdf", $"Agente_{nomeSoggetto}_{tableId}.pdf");

        }



        // ============================================================================
        //  CONTROLLER: IMPOSTA COME FATTURATE LE RIGHE DEL PROSPETTO COBRAL
        //  - Riceve i parametri singolarmente dal frontend
        //  - Chiama il repository per aggiornare le righe originali
        //  - Stampa l'Excel con i dati che arrivano dal JS
        // ============================================================================

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult ImpostaComeFatturate([FromBody] EmissioneFatturaRequest request)
        {


            var mandatario = _mandatarioService.GetById(request.IdMandatario);
            string nomeMandatario = mandatario?.NomeMandatario ?? "Mandatario";
            if (request == null || request.RigheSelezionate == null || !request.RigheSelezionate.Any())
            {
                return BadRequest("Dati non validi o nessuna riga selezionata.");
            }

            // 1. Pulizia iniziale del nome (sostituisce spazi con underscore)


            // 2.mese
            string parteMese = request.Mese > 0
                ? Utility.NomeMese(request.Mese).ToUpper() + " "
                : "";

            // 3. Recupero l'anno (solo se anno > 0)
            string parteAnno = (request.Anno > 0)
                ? request.Anno.ToString() + " "
                : "";

            // 4. Componiamo la stringa finale
            // Risultato atteso: 2026_MARZO_FABIO_FABIO_30GG oppure FABIO_FABIO_30GG
            string titolo = $" {parteAnno}{parteMese}";



            // 1. Ciclo ignorante: aggiorna il DB riga per riga
            foreach (var riga in request.RigheSelezionate)
            {
                // Usa il metodo che HAI GIÀ nel repository, senza cambiare nulla
                _prospettoCobralRepository.ImpostaComeFatturate(
                    request.IdMandatario,
                    request.Anno,
                    request.Mese,
                    riga.IdCliente,
                    request.IdTipoPagamento);
            }

            // 2. Stampa l'Excel con i dati che arrivano dal JS
            // (Quelli che l'utente vede a video e che hai messo nel JSON)
            // =================================================================================
            // ATTENZIONE: MAPPATURA INDICI TABELLA (IMPORTANTE PER EXCEL)
            // Se aggiungi o sposti colonne nella Partial View (_TabellaProvvigioni.cshtml),
            //  IN document.querySelectorAll(".btn-emetti-fattura").forEach(btn => {
            // DEVI aggiornare gli indici cells[x] qui sotto:
            // [0] = Checkbox
            // [1] = Cliente (NomeCliente)
            // [2] = Quantità (Nr. Pezzi)
            // [3] = Importo (Commissioni Totali)
            // =================================================================================
            var file = _excel.CreaExcelClienti(request.RigheSelezionate, "Fatture Emesse " + nomeMandatario + titolo);

            return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Emissione.xlsx");
        }


    }
}


//Cosa fa questo controller (spiegato semplice)
//✔️ 1. Riceve i filtri
//anno

//mese

//mandatario

//agente

//✔️ 2. Se non arrivano, usa quelli correnti
//✔️ 3. Crea il ViewModel
//✔️ 4. Chiama il servizio
//csharp
//_dashboardService.GetDatiCobral(anno, mese)
//✔️ 5. Se è selezionato un agente, filtra i dati
//✔️ 6. Restituisce la View
//🔧 IMPORTANTE: Registrazione del servizio in DI
//Per far funzionare il controller, devi registrare il servizio in Program.cs.

//📁 POSIZIONE DEL FILE
//Codice
///Calkos.web/Program.cs
///builder.Services.AddTransient<DashboardProvvigioniService>();
