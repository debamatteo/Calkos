using Calkos.web.Helpers;
using Calkos.web.Models.DTO;
using Calkos.web.Models.ViewModels.Prospetti;
using Calkos.web.Services.Export;
using Calkos.web.Services.Prospetti;
using Calkos.Web.Helpers;
using Calkos.Web.Identity;
using CalkosManager.Application.Interfaces;
using CalkosManager.Application.Services;
using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Domain.Models.Importazione;
using CalkosManager.Infrastructure.Repositories;
using CalkosManager.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using NuGet.Protocol.Core.Types;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

//Services\Export\ExcelExportService.cs


namespace Calkos.web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Operatore,Agente")]//20/06/2026
    // Antiforgery 05/04/2026: Aggiunto per centralizzare la validazione Antiforgery su tutte le POST del controller
    [AutoValidateAntiforgeryToken]
    public class ProspettiCSTController : Controller
    {
        /*
         * Questo controller gestisce TUTTO ciò che riguarda CST:
         * - Importazione file Excel CST
         * - Conversione righe Excel → ImportCST
         * - Salvataggio in FileImportato + ProspettoCST
         * - Visualizzazione lista ordini CST
         * 
         * È la versione SPECIALIZZATA del vecchio ProspettiController,
         * ma dedicata SOLO al Mandatario CST.
         */


        /*
         * Il repository CST.
         * 
         * Il controller NON deve conoscere come il repository funziona internamente.
         * Usa SOLO l’interfaccia IProspettoCSTRepository.
         * 
         * In Program.cs abbiamo:
         * services.AddScoped<IProspettoCSTRepository, ProspettoCSTRepository>();
         * 
         * Questo significa:
         * “Quando qualcuno chiede IProspettoCSTRepository, dagli ProspettoCSTRepository”.
         */
        // Il servizio che contiene tutta la pipeline di importazione.
        // Il controller NON fa logica: delega tutto al service.
        private readonly ImportazioneCSTService _ImportazioneCSTService;

        private readonly IProspettoCSTRepository _prospettoRepository;
        private readonly IAgenteRepository _agenteRepository; // serve per lookup agenti
        private readonly IFileBackupService _fileBackupService; // serve per backup file CST
        private readonly IClienteRepository _clienteRepository; // serve per lookup clienti
        private readonly IMaterialeRepository _materialeRepository; // serve per lookup materiale
        private readonly ITipoPagamentoRepository _tipoPagamentoRepository; // serve per lookup tipo pagamento
        private readonly IUnitaMisuraRepository _unitaMisuraRepository; // serve per lookup unità di misura
        private readonly ProspettoConfigService _configService;

        private readonly ExcelExportService _excelExportService;
        private readonly PdfExportService _pdfExportService;
        private readonly IConfiguration _config;//legge i parametri da appsettings.json
        private readonly MandatarioService _mandatarioService;
        private readonly ILogger<ProspettiCSTController> _logger;//20/10/2026
        public ProspettiCSTController(
            ImportazioneCSTService ImportazioneCSTService,
            IProspettoCSTRepository prospettoRepository,
            IAgenteRepository agenteRepository,
            IClienteRepository clienteRepository,
            IMaterialeRepository materialeRepository,
            ITipoPagamentoRepository tipoPagamentoRepository,
            IUnitaMisuraRepository unitaMisuraRepository,
            IFileBackupService fileBackupService,
            ProspettoConfigService configService,
            ExcelExportService excelExportService,
             PdfExportService pdfExportService,
             IConfiguration config,
             MandatarioService mandatarioService,
              ILogger<ProspettiCSTController> logger)//20/10/2026)//legge i parametri da appsettings.json
        {
            _ImportazioneCSTService = ImportazioneCSTService;
            _prospettoRepository = prospettoRepository;
            _agenteRepository = agenteRepository;
            _clienteRepository = clienteRepository;
            _materialeRepository = materialeRepository;
            _tipoPagamentoRepository = tipoPagamentoRepository;
            _unitaMisuraRepository = unitaMisuraRepository;
            _fileBackupService = fileBackupService;
            _configService = configService;   // iniettare il servizio di confiugurazione per la lettura delle colonne da visualizzare in lista ordini
            _excelExportService = excelExportService;
            _pdfExportService = pdfExportService;
            _config = config;//legge i parametri da appsettings.json
            _mandatarioService = mandatarioService;
            _logger = logger;//20/10/2026
        }


        // ============================================================
        // 13/10/2026 1 HELPER ISOLAMENTO ORDINI PER AGENTE -1
        // ============================================================
        /// <summary>
        /// Restituisce l'IdAgente da passare a GetFiltrati / spProspettoCST_GetAllFiltri.
        /// - Admin / Operatore: null  -> nessun filtro, vedono tutti gli ordini.
        /// - Agente collegato (AspNetUsers.IdAgente valorizzato): il suo IdAgente. La SP restituisce
        ///   gli ordini con quell'IdAgente SOLO se l'agente ha almeno un cliente assegnato in Clienti;
        ///   altrimenti restituisce 0 record (nessun fallback su ordini generali o slegati).
        /// - Agente NON collegato a nessun record Agenti: -1. Nessun agente ha Id -1, quindi la SP
        ///   restituisce 0 record. Senza questa guardia null significherebbe "nessun filtro" (fail-open).
        /// </summary>
        private int? GetIdAgenteFiltroOrdini()
        {
            if (!User.IsInRole("Agente"))
                return null;

            int? idAgente = User.GetIdAgente();

            return (idAgente.HasValue && idAgente.Value > 0) ? idAgente.Value : -1;
        }




        // ============================================================
        // 20/10/2026 N5.1 - AGGIUNTO: helper di accesso e valori economici Agente

        // ============================================================
        // ==== MODIFICA 24/10/2026 - INIZIO ==== 16-il mandatario in sessione deve essere davvero CST

        private bool VerificaMandatario(int idMandatario)
        {
            if (idMandatario <= 0) return false;
            var m = _mandatarioService.GetById(idMandatario);   // _mandatarioService è già iniettato
            return m != null && string.Equals(m.CodiceMandatario, "CST", StringComparison.OrdinalIgnoreCase);
        }

        // ==== MODIFICA 29/10/2026 - INIZIO ====
        // Stessa regola di VerificaMandatario(int), ma restituisce anche il Mandatario già letto dal DB
        // (serve a Nuovo() per leggere Perc_Delta_Provvigione senza una seconda GetById).
        private bool VerificaMandatario(int idMandatario, out Mandatario? mandatario)
        {
            mandatario = null;
            if (idMandatario <= 0) return false;
            mandatario = _mandatarioService.GetById(idMandatario);
            return mandatario != null
                && string.Equals(mandatario.CodiceMandatario, "CST", StringComparison.OrdinalIgnoreCase);
        }
        // ==== MODIFICA 29/10/2026 - FINE ====   



        // ==== MODIFICA 24/10/2026 - FINE ==== 17
        /// <summary>Admin/Operatore: sempre. Agente: solo ordini propri; un agente non collegato (-1) non passa mai.</summary>
        private bool PuoAccedereAllOrdine(ProspettoCST ordine)//20/10/2026
        {
            var filtro = GetIdAgenteFiltroOrdini();
            return !filtro.HasValue || (ordine.IdAgente.HasValue && ordine.IdAgente.Value == filtro.Value);
        }
        // ==== MODIFICA 24/10/2026 - INIZIO ==== 18  403 leggibile dalle chiamate AJAX (Forbid() farebbe un redirect HTML)
        private JsonResult ForbidAjax()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return Json(new { success = false, error = "Operazione non consentita su questo ordine." });
        }
        // ==== MODIFICA 24/10/2026 - FINE ==== 19

        /// <summary>
        /// Per l'Agente la percentuale di provvigione agente non e' un dato fidato: la decide solo il server.
        /// - ordine esistente e cliente invariato: resta il valore gia' salvato nel DB (non si ricalcola, quindi
        ///   non si perdono le modifiche manuali dell'Admin e non cambiano retroattivamente gli ordini vecchi);
        /// - ordine nuovo o cliente cambiato: gerarchia dbo.fn_GetPercentualeProvvigione (valore manuale = NULL).
        /// Provvigione/prezzi e totali di questo mandatario restano quelli inviati dalla pagina (nessun calcolo qui).
        /// </summary>
        private void ForzaValoriEconomiciAgente(ProspettoCST model, ProspettoCST? daDb, int idMandatario, int idAgente)//20/10/2026
        {
            model.ProvvigioneAgente = daDb is not null && daDb.IdCliente == model.IdCliente
                ? daDb.ProvvigioneAgente
                : _prospettoRepository.GetPercentualeAgente(idAgente, model.IdCliente, idMandatario);
        }














        // ============================================================
        // INIZIO IMPORTAZIONE CST
        //  GET: PAGINA DI IMPORTAZIONE CST
        // ============================================================


        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Importa(int idMandatario)
        {

            // 20/10/2026 Disabilita completamente l'endpoint restituendo un 404
            return NotFound();
            // Salvo IdMandatario in sessione
            HttpContext.Session.SetInt32("IdMandatario", idMandatario);

            // Recupero il tipo pagamento del mandatario
            int tipoPagamento = _mandatarioService.GetTipoPagamento(idMandatario);

            // Lo salvo in sessione per tutto il flusso CST
            HttpContext.Session.SetInt32("TipoPagamentoMandatario", tipoPagamento);

            // Mantengo anche il ViewBag se serve
            ViewBag.IdMandatario = idMandatario;
            ViewBag.TipoPagamento = tipoPagamento;

            return View();
        }

        // ============================================================
        // POST: ESECUZIONE IMPORTAZIONE CST
        // ============================================================
        /*
         * Questo metodo:
         * 1. Legge il file Excel
         * 2. Converte ogni riga in ImportCST
         * 3. Crea un FileImportato
         * 4. Avvia la pipeline di importazione tramite ImportazioneCSTService
         * 5. Mostra una modale di conferma
         */

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [RequestSizeLimit(20_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 20_000_000)]
        //20/10/2026 Il messaggio d'errore di importazione non mostra più il dettaglio della riga: sta nel log del server.
        public IActionResult Importa(IFormFile fileExcel, int anno, int mese)
        {

            // 20/10/2026 Disabilita completamente l'endpoint restituendo un 404
            //in futuro per riabilitare la funzionalità, rimuovere o commentare quella singola riga.
            return NotFound();

            int idMandatario = HttpContext.Session.GetInt32("IdMandatario") ?? 0;

            if (idMandatario == 0)
            {
                TempData["Errore"] = "Si è persa la Sessione del Mandatario. Ritorna alla Pagina Iniziale.";
                return View();
            }

            if (anno < DateTime.Now.Year - 5 || anno > DateTime.Now.Year + 1 || mese is < 1 or > 12)
            {
                TempData["Errore"] = "Anno o mese non validi.";
                return View();
            }

            if (!UploadExcelTemp.TryCreate(fileExcel, "CST", out var temp, out var erroreUpload))
            {
                TempData["Errore"] = erroreUpload;
                return View();
            }

            using (temp!) // cancella il file temporaneo comunque vada l'importazione
            {
                try
                {
                    _fileBackupService.BackupCSTAsync(temp.Percorso).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Backup CST non eseguito");
                    TempData["BackupWarning"] = "Backup non eseguito. Controlla i log del server.";
                }

                int firstRow = _config.GetValue<int>("ImportazioneCST:FirstRow");

                HashSet<string> valoriAmmessi = (_config.GetSection("ValoriFatturareTrue").Get<List<string>>() ?? new List<string>())
                    .DefaultIfEmpty("OK")
                    .Select(v => v.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                List<RigaExcelCST> righe;

                try
                {
                    righe = ExcelHelper.LeggiExcelCST(fileExcel, firstRow, valoriAmmessi);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lettura Excel CST non riuscita");
                    TempData["Errore"] = "Il file non rispetta il formato atteso. Controlla fogli, colonne e valori.";
                    return View();
                }

                var righeImport = righe.Select(Converti).ToList();

                if (righeImport.Count == 0)
                {
                    TempData["Errore"] = "Non ci sono dati da importare.";
                    return View();
                }

                string utente = User.Identity?.Name ?? "Sistema";

                var file = new FileImportato
                {
                    IdMandatario = idMandatario,
                    NomeFile = Path.GetFileName(fileExcel.FileName),
                    DataImportazione = DateTime.Now,
                    Utente = utente,
                    Anno = anno,
                    Mese = mese
                };

                int idTipoPagamento = HttpContext.Session.GetInt32("TipoPagamentoMandatario") ?? 0;

                int idFile;

                try
                {
                    idFile = _ImportazioneCSTService.ImportaFile(
                        file,
                        righeImport,
                        utente,
                        idTipoPagamento);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Importazione CST non riuscita (mandatario {IdMandatario})",
                        idMandatario);

                    TempData["Errore"] = "Errore durante l'importazione dei dati. Nessun dato è stato salvato.";
                    return View();
                }

                TempData["ImportSuccess"] = true;
                TempData["IdFileImportato"] = idFile;

                return RedirectToAction(
                    "Importa",
                    "ProspettiCST",
                    new
                    {
                        area = "Admin",
                        idMandatario = file.IdMandatario
                    });
            }
        }
        // ============================================================
        // CONVERSIONE RIGA EXCEL CST → ImportCST
        // ============================================================
        /*
         * Questo metodo converte una riga Excel CST
         * nel modello ImportCST usato dalla pipeline.
         * 
         * È specifico per CST, quindi sta nel controller CST.
         */


        private ImportCST Converti(RigaExcelCST r)
        {
            var i = new ImportCST();

            // ============================
            // Campi base
            // ============================
            i.OrdineDDT = r.OrdineDDT;
            i.Cliente = r.Cliente;

            // ============================
            // Quantità e costi
            // ============================
            i.Kg = r.Kg;
            i.CostoLavorazione = r.CostoLavorazione;
            i.Materiale = r.Materiale;

            // ============================
            // Prezzi
            // ============================
            i.PrezzoBase = r.PrezzoBase;
            i.PrezzoEuroRiferimento = r.PrezzoEuroRiferimento;//21/04/2026
            i.PrezzoVendita = r.PrezzoVendita;
            i.PrezzoBaseConLavorazione = r.PrezzoBaseConLavorazione;

            // ============================
            // Provvigioni fisse
            // ============================
            i.ProvvigioneFissa01_05 = r.ProvvigioneFissa01_05;
            i.ProvvigioneFissa03 = r.ProvvigioneFissa03;
            i.Perc_Delta_Provvigione = r.Perc_Delta_Provvigione;//30/04/2026
            i.TotaleProvvigioneFissa = r.TotaleProvvigioneFissa;

            // ============================
            // Variabile
            // ============================
            i.CIT = r.CIT;
            i.Delta = r.Delta;
            i.TotaleProvvigioneVariabile = r.TotaleProvvigioneVariabile;
            i.TotaleProvvigioneFattura = r.TotaleProvvigioneFattura;

            // ============================
            // Date
            // ============================
            i.DataConsegna = r.DataConsegna;

            // ============================
            // Fatturare (stringa Excel)
            // ============================
            i.FatturareStringa = r.Fatturare;

            return i;
        }




        // ============================================================
        // FINE  IMPORTAZIONE CST
        // ============================================================



        //// ============================================================
        //// LISTA ORDINI CST
        //// ============================================================
        ///*
        // * Questa action mostra:
        // * - TUTTI gli ordini CST
        // * - OPPURE quelli filtrati per:
        // * - file importato (idFileImportato)
        // * - anno
        // * - mese
        // * - stato fatturazione (fatturata: 0=No, 1=Si) //25/03/2026
        // *
        // * Dopo l'importazione:
        // * - arrivi con idFileImportato valorizzato → vedi solo le righe di quel file
        // * Dalla pagina:
        // * - puoi togliere il filtro file e filtrare per anno/mese/stato fattura
        // */
        public IActionResult ListaOrdini(int? idMandatario, int? idFileImportato, int? anno,
             int? mese, int? fatturata,
             int? daFatturare,           // NUOVO 30/09/2026
            int? idTipoPagamento,       // <-- NUOVO PARAMETRO 22/05/2026
        string inRitardo,    //05/10/2026  era bool? , valori: aperti | consegnati | tutti_ritardi | false bool? inRitardo, 
            bool mostraEliminati = false)//25/03/2026 aggiunto filtro fatturata
        {

            var sw = Stopwatch.StartNew();

            // ============================================================
            // LOGICA DEFAULT ANNO / MESE
            // Request → rappresenta la richiesta HTTP in arrivo
            // Request.Query →  → rappresenta la query string, cioè i parametri dopo il ? nell’URL
            // ============================================================

            // Se l’utente NON ha cliccato nulla → primo accesso
            if (!Request.Query.ContainsKey("anno"))
            {
                anno = DateTime.Now.Year;
            }

            if (!Request.Query.ContainsKey("mese"))
            {
                mese = DateTime.Now.Month;
            }


            // ============================================================
            // 1) GESTIONE MANDATARIO E SESSIONE
            // ============================================================

            // 1. Lo recuperi dal parametro che arriva dal link
            if (idMandatario.HasValue)
            {
                // 2. Lo salvi in sessione per "ricordarlo" nelle prossime pagine
                HttpContext.Session.SetInt32("IdMandatario", idMandatario.Value);
                // Mantengo anche il ViewBag per la view, se serve
                ViewBag.IdMandatario = idMandatario;
            }
            else
            {
                // 3. Se il parametro è null (magari ricarichi la pagina), prova a prenderlo dalla sessione
                idMandatario = HttpContext.Session.GetInt32("IdMandatario");
                ViewBag.IdMandatario = idMandatario;
            }

            // ============================================================
            //  LOGICA DEFAULT FILTRO FATTURAZIONE
            // ============================================================
            // 1. Recuperiamo il valore dalla query string. 
            // Se l'utente non ha ancora cliccato nulla (primo accesso), 'fatturata' sarà NULL.

            //if (!idFileImportato.HasValue && !anno.HasValue && !mese.HasValue && !fatturata.HasValue)
            //{
            //    // Solo se è il PRIMISSIMO accesso alla pagina (tutti i filtri nulli)
            //    // impostiamo il default a 0 (Non fatturate).
            //    fatturata = 0;
            //}
            //else if (fatturata == -1)
            //{
            //    // Se l'utente sceglie "Tutti" (che imposteremo a -1 nell'HTML), 
            //    // passiamo NULL al database per annullare il filtro SQL.
            //    fatturata = null;
            //}


            // Se l’utente NON ha cliccato nulla e NON è un reset
            //(!Request.Query.ContainsKey("fatturata"))È un modo per capire se nella URL è presente il parametro fatturata
            if (!Request.Query.ContainsKey("fatturata"))
            {
                // fatturata = 0; //Primo accesso →default = NON fatturate
                fatturata = null; // Primo accesso → default = Tutti//mettere 
            }
            else if (fatturata == -1)
            {
                // Utente ha scelto "Tutti"
                fatturata = null;
            }

            //30/09/2026  NUOVO FILTRO DA FATTURARE
            if (!Request.Query.ContainsKey("daFatturare") || daFatturare == -1)
            {
                daFatturare = null;
            }
            // ============================================================
            // 2) RECUPERO DATI FILTRATI (ADO.NET OTTIMIZZATO)
            // ============================================================

            // 22/05/2026: 0 significa "Tutti" → passiamo null al repository così il WHERE nella SP non applica il filtro
            if (idTipoPagamento == 0)
                idTipoPagamento = null;

            //07/08/2026 inizio
            bool isAgente = User.IsInRole("Agente");
            // 13/10/2026 2 FAIL-CLOSED: un Agente senza collegamento (IdAgente nullo) NON deve passare null alla SP,
            // perché null = "nessun filtro" e vedrebbe TUTTI gli ordini. Il helper restituisce -1 (nessun match).
            int? idAgenteLoggato = GetIdAgenteFiltroOrdini();

            //int? idAgenteLoggato = isAgente ? User.GetIdAgente() : null;
            // NUOVO APPROCCIO: Chiamiamo il database chiedendo SOLO le righe che servono.
            //  Ho aggiunto 'fatturata' alla chiamata GetFiltrati.
            var prospetti = _prospettoRepository.GetFiltrati(
                idMandatario, idFileImportato, anno, mese, fatturata,
                mostraEliminati, idTipoPagamento, idAgenteLoggato
                 , daFatturare); // 30/09/2026
                                 // Carico la configurazione colonne dal file JSON
            var config = _configService.Load("CST");
            //Per l'Agente: le colonne con VisibileAgente = false vengono nascoste 
            config.Columns = config.Columns.FiltraPerRuolo(isAgente); // NUOVO
                                                                      //07/08/2026 Fine         

            sw.Stop();
            System.Diagnostics.Debug.WriteLine("TEMPO QUERY: " + sw.ElapsedMilliseconds + " ms");


            // Passiamo i filtri alla view per mantenerli selezionati nelle select del form
            ViewBag.IdFileImportato = idFileImportato;
            ViewBag.Anno = anno;
            ViewBag.Mese = mese;
            ViewBag.Fatturata = fatturata; //  Necessario per far funzionare il 'selected' nella View
                                           // PASSA IL VALORE ALLA VIEW PER MANTENERE LA SELEZIONE NELLA DROPDOWN
            ViewBag.DaFatturare = daFatturare;   // 30/09/2026

            ViewBag.MostraEliminati = mostraEliminati;
            ViewBag.IdTipoPagamento = idTipoPagamento;//22/05/2026

            // -------------------------------------------------------
            // 22/05/2026 POPOLA IL VIEWBAG CON I TIPI PAGAMENTO PER LA SELECT
            // Stesso pattern usato in CaricaLookup per il dettaglio ordine
            // -------------------------------------------------------
            var tipiPagamento = _tipoPagamentoRepository.GetAll();
            ViewBag.TipiPagamento = tipiPagamento
                .Select(t => new SelectListItem
                {
                    Value = t.IdTipoPagamento.ToString(),
                    Text = t.DescrizioneTipoPagamento
                })
                .ToList();


            // ============================================================
            // 3) CONFIGURAZIONE DINAMICA E VIEWMODEL
            // ============================================================

            // Controller
            //    ├── filtra prospetti (ora avviene direttamente in SQL tramite GetFiltrati)
            //    ├── carica JSON
            //    └── crea ViewModel
            //           ├── Righe = prospetti filtrati
            //           └── Colonne = config.Columns
            // → View

            // 2. Costruisco il ViewModel dinamico
            var vm = new ProspettoCSTListaViewModel
            {
                Righe = prospetti, // prospetti = i dati (le righe reali del DB filtrate alla sorgente)
                Colonne = config.Columns // la configurazione colonne (Colonne)
            };

            // 3. Passo il ViewModel alla view
            return View(vm);
        }



        // ============================================================
        // GET:  DETTAGLIO ORDINE CST (SOLO ADMIN)
        // ============================================================


        // ============================================================
        // GET: DETTAGLIO ORDINE CST
        // 20/10/2026 N5.2 - MODIFICATO: lettura ordine + controllo accesso (NotFound prima, poi Forbid)
        // ============================================================
        [HttpGet]
        [Authorize(Roles = "Admin,Operatore,Agente")]
        public IActionResult DettaglioOrdine(int? id)//20/10/2026
        {
            // 1️⃣ Se arrivo da DUPLICA AJAX → uso il modello duplicato
            if (TempData["Duplicato"] != null)
            {
                var duplicato = JsonConvert.DeserializeObject<ProspettoCST>(TempData["Duplicato"].ToString());
                CaricaLookup(duplicato);
                return View(duplicato);
            }

            // 2️⃣ Flusso normale: serve ID
            if (!id.HasValue)
                return NotFound();

            // 20/10/2026 N5.2 INIZIO: prima verifico che l'ordine esista, poi che l'utente possa aprirlo
            var ordine = _prospettoRepository.GetById(id.Value);
            if (ordine is null)
                return NotFound();

            if (!PuoAccedereAllOrdine(ordine))
                return Forbid();
            // 20/10/2026 N5.2 FINE

            // 3️⃣ Carico i lookup (righe preesistenti, lasciate invariate)
            var agenti = _agenteRepository.GetAll();
            var clienti = _clienteRepository.GetByMandatario(ordine.IdMandatario);
            var materiali = _materialeRepository.GetAll();
            var tipiPagamento = _tipoPagamentoRepository.GetAll();
            var unitaMisura = _unitaMisuraRepository.GetAll();

            CaricaLookup(ordine);

            return View(ordine);
        }

        // ============================================================
        // POST: SALVATAGGIO DETTAGLIO ORDINE CST
        // 20/10/2026 N5.3 - MODIFICATO: ownership/fatturato server-side + valori economici dell'Agente
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Admin,Operatore,Agente")]
        public IActionResult DettaglioOrdine(ProspettoCST model)//20/10/2026
        {
            int idMandatario = HttpContext.Session.GetInt32("IdMandatario") ?? 0;
            model.IdMandatario = idMandatario;

            // 20/10/2026 N5.3 INIZIO
            bool isAgente = User.IsInRole("Agente");
            int? idAgenteLoggato = GetIdAgenteFiltroOrdini();   // null Admin/Operatore, -1 se agente non collegato

            // Ordine attuale nel DB (null se nuovo). Riusato anche piu' sotto: NON dichiararlo di nuovo.
            ProspettoCST? valoriDb = model.IdProspettoCST != 0
                ? _prospettoRepository.GetById(model.IdProspettoCST)
                : null;

            if (model.IdProspettoCST != 0 && valoriDb is null)
                return NotFound();


            // ==== MODIFICA 24/10/2026 - INIZIO ====1
            if (valoriDb is not null)
            {
                // ordine esistente: il mandatario è quello salvato, non quello in sessione
                idMandatario = valoriDb.IdMandatario;
                model.IdMandatario = idMandatario;
            }
            else if (!VerificaMandatario(idMandatario))
            {
                // nuovo ordine: la sessione deve puntare a CST
                ModelState.AddModelError("", "Mandatario di lavoro non valido: riapri la Lista Ordini CST dal menu e riprova.");
                CaricaLookup(model);
                return View(model);
            }
            // ==== MODIFICA 24/10/2026 - FINE ====2


            if (valoriDb is not null)
            {
                if (!PuoAccedereAllOrdine(valoriDb))
                    return Forbid();

                // il blocco "ordine fatturato" della pagina e' solo JavaScript: va ripetuto qui
                if (valoriDb.Fatturata == 1)
                {
                    ModelState.AddModelError(string.Empty, "Ordine già fatturato: resetta prima la fatturazione per modificarlo.");
                    CaricaLookup(valoriDb);
                    return View(valoriDb);
                }
            }

            if (isAgente)
            {
                if (idAgenteLoggato is null or <= 0)
                    return Forbid();

                // verifica che il cliente scelto sia realmente associato a questo agente
                // ==== MODIFICA 30/10/2026 - INIZIO ====
                // Un cliente è "dell'agente" se Clienti.IdAgente = agente OPPURE c'è una riga in ProvvigioniAgenti
                // (agente + mandatario dell'ordine + cliente), e il cliente è collegato al mandatario (MandatariClienti).
                // Prima si controllava solo clienteScelto.IdAgente, quindi i clienti con agente solo in ProvvigioniAgenti
                // venivano rifiutati.
                //var clienteScelto = _clienteRepository.GetByMandatario(model.IdMandatario, model.IdCliente)
                //    .FirstOrDefault(c => c.IdCliente == model.IdCliente);
                //
                //if (clienteScelto is null || clienteScelto.IdAgente != idAgenteLoggato)
                if (!_clienteRepository.IsClienteAccessibileAdAgente(model.IdCliente, idAgenteLoggato.Value, model.IdMandatario))
                // ==== MODIFICA 30/10/2026 - FINE ====
                {
                    ModelState.AddModelError(string.Empty, "Cliente non associato al tuo profilo agente.");
                    CaricaLookup(model);
                    return View(model);
                }

                model.IdAgente = idAgenteLoggato;
                ForzaValoriEconomiciAgente(model, valoriDb, model.IdMandatario, idAgenteLoggato.Value);
            }
            // 20/10/2026 N5.3 FINE

            // 26/10/2026 1 Nessun agente (segnaposto "— —" = 0 nella select): salvo NULL, non 0 (che sarebbe un
            // riferimento a un agente inesistente), e la provvigione agente a zero. Per il ruolo Agente il ramo
            // non scatta mai: IdAgente e' gia' stato forzato a quello loggato nel blocco sopra.
            if (model.IdAgente is null or 0)
            {
                model.IdAgente = null;
                model.ProvvigioneAgente = 0m;
            }

            if (idMandatario == 0)
            {
                ModelState.AddModelError("", "Sessione scaduta o Mandatario non identificato");
                CaricaLookup(model);
                return View(model);
            }

            // 08/10/2026 NUMERO ORDINE: se vuoto assegno un numero temporaneo TEMPAAAAMMGG (es. TEMP20261008).
            // Rimuovo l'eventuale errore di validazione sul campo, cosi' il salvataggio non viene bloccato.
            model.NumeroOrdine = model.NumeroOrdine?.Trim();
            if (string.IsNullOrWhiteSpace(model.NumeroOrdine))
            {
                model.NumeroOrdine = GeneraNumeroOrdineTemporaneo();
                ModelState.Remove(nameof(model.NumeroOrdine));
            }

            // 1) Validazione del modello
            // Se il modello non è valido, trasformiamo gli errori generici in qualcosa di leggibile
            if (!ModelState.IsValid)
            {
                // Usiamo ToList() per evitare l'errore "Collection was modified"
                //Dobbiamo creare una copia della lista degli errori prima di modificarla.
                var entriesConErrore = ModelState.ToList();

                foreach (var entry in entriesConErrore)
                {
                    // Creiamo una copia degli errori per poterli modificare senza bloccare il ciclo
                    var errori = entry.Value.Errors.ToList();

                    foreach (var error in errori)
                    {
                        if (error.ErrorMessage.Contains("is invalid") || string.IsNullOrEmpty(error.ErrorMessage))
                        {
                            // Rimuoviamo l'errore vecchio e ne mettiamo uno pulito
                            ModelState.Remove(entry.Key);
                            ModelState.AddModelError(entry.Key, "Campo obbligatorio o formato errato");
                        }
                    }
                }

                CaricaLookup(model);
                return View(model);
            }
            // 2) Recupero l’utente che sta modificando l’ordine
            string utente = User.Identity?.Name ?? "Sistema";

            try
            {
                // 3) Aggiornamento/Inserimento nel database
                if (model.IdProspettoCST != 0)
                {
                    _prospettoRepository.Update(model, utente);//03/04/2026 NUOVA
                    TempData["ModalMessage"] = "Modifiche salvate con successo.";//03/04/2026 NUOVA il messaggio viene salvato in TempData
                    //03/04/2026 NUOVA TempData sopravvive solo per il redirect successivo;
                    return RedirectToAction("ListaOrdini");//03/04/2026 NUOVA dopo la modifica torno alla lista, la pagina che riceve il redirect è ListaOrdini
                }
                else
                {
                    // Se è un nuovo inserimento, recuperiamo l'ID generato per il redirect
                    model.Anno = DateTime.Now.Year;
                    model.Mese = DateTime.Now.Month;

                    int nuovoId = _prospettoRepository.Insert(model, utente);//03/04/2026 NUOVA
                    model.IdProspettoCST = nuovoId;//03/04/2026 NUOVA
                    TempData["ModalMessage"] = "Nuova riga inserita con successo.";//03/04/2026 NUOVA
                    return RedirectToAction("ListaOrdini");//03/04/2026 NUOVA
                }
            }
            catch (Exception ex)
            {
                // ==== MODIFICA 24/10/2026 - INIZIO ====3
                _logger.LogError(ex, "Salvataggio ordine CST {Id} non riuscito", model.IdProspettoCST);
                ModelState.AddModelError("", "Errore durante il salvataggio. Riprova o contatta l'amministratore.");
                // ==== MODIFICA 24/10/2026 - FINE ====4
                CaricaLookup(model);
                return View(model);
            }
        }




        //// ============================================================
        //// CARICAMENTO LOOKUP PER LA VIEW DETTAGLIO ORDINE
        //// ============================================================

        private void CaricaLookup(ProspettoCST ordine)
        {
            // Agenti
            var agenti = _agenteRepository.GetAll();
            var listaAgenti = agenti.Select(a => new AgenteSelectItem
            {
                Id = a.IdAgente,
                Nome = a.AgenteDescrizione,
                PercentualeDefault = a.PercentualeDefault
            }).ToList();

            listaAgenti.Insert(0, new AgenteSelectItem
            {
                Id = 0,
                Nome = "— —",
                PercentualeDefault = 0
            });

            ViewBag.Agenti = listaAgenti;

            // MODIFICA 31/03/2026: Passiamo l'IdCliente dell'ordine corrente
            // per assicurarci che venga caricato anche se è stato eliminato (Soft Delete)
            // ==== MODIFICA 30/10/2026 - INIZIO ====
            // Il filtro per l'Agente è ora nella SP spClienti_GetByMandatario (terzo parametro): restituisce i clienti
            // con Clienti.IdAgente = agente OPPURE ProvvigioniAgenti (agente + mandatario + cliente), più sempre il
            // cliente già presente sull'ordine (idClienteGiaIncluso), anche se eliminato o non più associato,
            // per non rompere la visualizzazione di ordini storici.
            // GetIdAgenteFiltroOrdini(): null per Admin/Operatore (nessun filtro), -1 per Agente non collegato (nessun cliente).
            //var clienti = _clienteRepository.GetByMandatario(ordine.IdMandatario, ordine.IdCliente);   // 30/10/2026 sostituita dalla riga sotto
            var clienti = _clienteRepository.GetByMandatario(ordine.IdMandatario, ordine.IdCliente, GetIdAgenteFiltroOrdini());
            // ==== MODIFICA 30/10/2026 - FINE ====

            // 07/08/2026 6 INIZIO
            //se l'utente è un Agente, mostra SOLO i clienti a lui associati.
            // Preserviamo comunque il cliente già selezionato sull'ordine (se esiste), anche se
            // non risultasse più assegnato a questo agente, per non rompere la visualizzazione
            // di ordini storici/eccezioni — stesso principio già usato qui per i clienti eliminati.
            // ==== MODIFICA 30/10/2026 - INIZIO ==== blocco sostituito dal filtro in SP (vedi sopra)
            //if (User.IsInRole("Agente"))
            //{
            //    int? idAgenteLoggato = User.GetIdAgente();
            //
            //    var clientiFiltrati = clienti
            //        .Where(c => c.IdAgente == idAgenteLoggato)
            //        .ToList();
            //
            //    if (ordine.IdCliente != 0 && !clientiFiltrati.Any(c => c.IdCliente == ordine.IdCliente))
            //    {
            //        var clienteCorrente = clienti.FirstOrDefault(c => c.IdCliente == ordine.IdCliente);
            //        if (clienteCorrente != null)
            //            clientiFiltrati.Add(clienteCorrente);
            //    }
            //
            //    clienti = clientiFiltrati;
            //}
            // ==== MODIFICA 30/10/2026 - FINE ====
            //  07/08/2026 FINE


            var listaClienti = clienti.Select(c => new SelectListItem
            {
                Value = c.IdCliente.ToString(),
                Text = c.IsDeleted ? $"{c.RagioneSociale} (ELIMINATO)" : c.RagioneSociale // Opzionale: aggiunge un'etichetta visiva
            }).ToList();

            listaClienti.Insert(0, new SelectListItem { Value = "", Text = "— —" });
            ViewBag.Clienti = listaClienti;

            // Materiali
            var materiali = _materialeRepository.GetAll();
            var listaMateriali = materiali.Select(m => new SelectListItem
            {
                Value = m.IdMateriale.ToString(),
                Text = m.DescrizioneMateriale
            }).ToList();

            listaMateriali.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "— —"
            });

            ViewBag.Materiali = listaMateriali;

            // Tipi pagamento
            var tipiPagamento = _tipoPagamentoRepository.GetAll();
            var listaTipiPagamento = tipiPagamento.Select(t => new SelectListItem
            {
                Value = t.IdTipoPagamento.ToString(),
                Text = t.DescrizioneTipoPagamento
            }).ToList();

            listaTipiPagamento.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "— —"
            });

            ViewBag.TipiPagamento = listaTipiPagamento;

            // Unità di misura
            var unitaMisura = _unitaMisuraRepository.GetAll();
            var listaUnitaMisura = unitaMisura.Select(u => new SelectListItem
            {
                Value = u.IdUnitaMisura.ToString(),
                Text = u.UnitaMisuraDescrizione
            }).ToList();

            listaUnitaMisura.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "— —"
            });

            ViewBag.UnitaMisura = listaUnitaMisura;
        }


        //// ============================================================
        //// NUOVA RIGA ORDINE CST (SOLO ADMIN)
        //// ============================================================
        public IActionResult Nuovo()
        {
            // 1. Contesto e Sessione
            int idMandatario = HttpContext.Session.GetInt32("IdMandatario") ?? 0;

            // 29/10/2026 era: if (!VerificaMandatario(idMandatario))  -> ora usa l'overload per riusare il Mandatario letto
            if (!VerificaMandatario(idMandatario, out var mandatario))
                return RedirectToAction("Index", "Prospetti");   // 24/10/2026 5- sessione non su CST: si riparte dalla scelta del mandatario
            //if (!VerificaMandatario(idMandatario))
            //    return RedirectToAction("Index", "Prospetti");    // 24/10/2026 5 sessione non su CST: si riparte dalla scelta del mandatario


            var model = new ProspettoCST();

            // --- CHIAVI E METADATI OBBLIGATORI ---
            model.IdMandatario = idMandatario;
            model.IdFileImportato = 0;
            model.IdImportCST = 0;
            model.IdCliente = 0;
            model.IdMateriale = 0;
            //model.IdUnitaMisura = 0;   // 01/11/2026 sostituito dalla riga sotto
            model.IdUnitaMisura = mandatario?.IdUnitaMisuraDefault ?? 0;   // 01/11/2026 default del mandatario

            model.Utente = User.Identity?.Name ?? "Sistema";
            model.DataInserimento = DateTime.Now;

            // --- DATE E PERIODI ---
            model.DataConsegna = DateTime.Today;
            model.Scadenza = DateTime.Today;
            model.Anno = DateTime.Today.Year;
            model.Mese = DateTime.Today.Month;

            // --- DOCUMENTO (EVITA STRINGHE NULL) ---
            model.NumeroOrdine = string.Empty;
            model.NumeroDDT = string.Empty;
            model.NumeroFattura = string.Empty;

            // --- QUANTITÀ E PREZZI (DECIMAL 18,6 PER CALCOLI JS) ---
            model.Quantita = 0m;
            model.CostoLavorazione = 0m;
            model.PrezzoBase = 0m;
            model.PrezzoVendita = 0m;
            model.PrezzoBaseConLavorazione = 0m;
            model.PrezzoEuroRiferimento = 0m; // Aggiunto 20/04/2026

            // --- LOGICA PROVVIGIONI CST (EVITA NaN/NULL) ---
            model.ProvvigioneFissa01_05 = 0m;
            model.ProvvigioneFissa03 = 0m;
            model.Perc_Delta_Provvigione = 0m;//30/04/2026
            model.TotaleProvvigioneFissa = 0m;
            model.CIT = 0m;
            model.Delta = 0m;
            model.TotaleProvvigioneVariabile = 0m;
            model.TotaleProvvigioneFattura = 0m;
            model.ProvvigioneAgente = 0m;

            // --- STATI E FLAG ---
            model.Fatturare = false;
            model.Fatturata = 0;
            model.IsDeleted = false;
            model.EscludiControlloRitardo = false;   // NUOVO 30/09/2026
            model.DaFatturare = false;               // NUOVO 30/09/2026

            // ==== MODIFICA 29/10/2026 - INIZIO ==== valore proposto = default impostato sul Mandatario (NULL => 0)
            //model.Perc_Delta_Provvigione = 0m;   // 29/10/2026 sostituito dalla riga sotto
            model.Perc_Delta_Provvigione = mandatario?.Perc_Delta_Provvigione ?? 0m;
            // ==== MODIFICA 29/10/2026 - FINE ====




            // 3. Lookup tabelle collegate
            if (User.IsInRole("Agente"))
                model.IdAgente = User.GetIdAgente() ?? 0;  // 07/08/2026 l'Agente crea sempre ordini propri



            // --- CARICAMENTO LOOKUP E RITORNO ---
            CaricaLookup(model);

            return View("DettaglioOrdine", model);
        }

        // ============================================================
        // 08/10/2026 NUMERO ORDINE TEMPORANEO: formato TEMPAAAAMMGGHHMMSSFFF (es. TEMP20261008123045123)
        // ============================================================
        private static string GeneraNumeroOrdineTemporaneo()
           => "TEMP" + DateTime.Now.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);

        //// ============================================================
        //// DUPLICA  (SOLO ADMIN)
        //// ============================================================
        [Authorize(Roles = "Admin,Operatore")] //07/08/2026
        public IActionResult Duplica(int id)
        {  //Aprire la pagina Dettaglio invece di salvare subito
            var originale = _prospettoRepository.GetById(id);
            if (originale == null)
                return NotFound();

            var nuovo = originale.CloneForInsert(User.Identity?.Name);

            CaricaLookup(nuovo);

            return View("DettaglioOrdine", nuovo);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Agente")]//24/10/2026 6 era solo Admin. Gli attributi si sommano (AND): il ruolo deve passare sia qui sia a livello di classe
        public IActionResult CancellaAjaxDatabase(int id)
        {
            try
            {
                // Admin: comportamento invariato
                if (!User.IsInRole("Agente"))
                {
                    _prospettoRepository.Delete(id);
                    return Json(new { success = true });
                }

                // ==== MODIFICA 24/10/2026 - INIZIO 7 ==== Agente: solo ordini propri, nel cestino, non fatturati
                var ordine = _prospettoRepository.GetById(id);
                if (ordine == null)
                    return Json(new { success = false, error = "Ordine non trovato." });

                if (!PuoAccedereAllOrdine(ordine))
                    return ForbidAjax();

                if (!ordine.IsDeleted)
                    return Json(new { success = false, error = "Sposta prima l'ordine nel cestino." });

                if (ordine.Fatturata == 1)
                    return Json(new { success = false, error = "Ordine già fatturato: non può essere eliminato." });

                // La DELETE ripete gli stessi controlli: copre il caso "fatturato nel frattempo"
                bool eliminato = _prospettoRepository.DeleteSeNonFatturata(id, GetIdAgenteFiltroOrdini());
                return eliminato
                    ? Json(new { success = true })
                    : Json(new { success = false, error = "Ordine non eliminabile (potrebbe essere stato fatturato)." });
                // ==== MODIFICA 24/10/2026 - FINE ====8
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eliminazione definitiva ordine CST {Id} non riuscita", id);
                return Json(new { success = false, error = "Errore durante l'eliminazione definitiva." });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Operatore,Agente")]//24/10/2026 9 agente elimina i propri ordini non fatturati
        public IActionResult CancellaAjax(int id)
        {
            try
            {
                var ordine = _prospettoRepository.GetById(id);
                if (ordine == null)
                    return Json(new { success = false, error = "Ordine non trovato." });

                if (!PuoAccedereAllOrdine(ordine))
                    return ForbidAjax();

                if (ordine.Fatturata == 1)
                    return Json(new { success = false, error = "Ordine già fatturato: resetta prima la fatturazione." });

                _prospettoRepository.SoftDelete(id, User.Identity?.Name ?? "Admin");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eliminazione logica ordine CST {Id} non riuscita", id);
                return Json(new { success = false, error = "Errore durante l'eliminazione." });
            }
        }



        [HttpPost]
        [Authorize(Roles = "Admin,Operatore")]//07/08/2026
        public IActionResult ResetFatturazioneAjax(int id)
        {
            var ok = _prospettoRepository.ResetFatturazione(id, User.Identity?.Name ?? "sistema");
            return Json(new { success = ok });
        }


        // ==== MODIFICA 02/11/2026 - INIZIO ==== scelta periodo dal popup Duplica
        [HttpPost]
        [Authorize(Roles = "Admin,Operatore,Agente")]//24/10/2026 10-agente puo duplicare i propri ordini

        public IActionResult DuplicaAjax(int id, string? periodo = null, int? anno = null, int? mese = null)

        {
            var originale = _prospettoRepository.GetById(id);
            if (originale == null)
                return Json(new { success = false, error = "Riga non trovata." });

            //24/10/2026 agente puo duplicare i propri ordini - 11
            if (!PuoAccedereAllOrdine(originale))
                return ForbidAjax();   // 24/10/2026 era Forbid() - 12

            // ==== MODIFICA 24/10/2026 - INIZIO ==== 13
            if (originale.IsDeleted)
                return Json(new { success = false, error = "Non puoi duplicare un ordine che si trova nel cestino." });

            int? idAgenteFiltro = GetIdAgenteFiltroOrdini();   // null = Admin/Operatore

            // Stessa regola del salvataggio: l'agente lavora solo su clienti ancora associati a lui
            if (idAgenteFiltro.HasValue)
            {
                // ==== MODIFICA 30/10/2026 - INIZIO ====
                // agente di default OPPURE ProvvigioniAgenti (per il mandatario dell'ordine) + cliente collegato al mandatario
                //bool clienteProprio = _clienteRepository.GetByMandatario(originale.IdMandatario, originale.IdCliente)
                //    .Any(c => c.IdCliente == originale.IdCliente && c.IdAgente == idAgenteFiltro.Value);
                bool clienteProprio = _clienteRepository.IsClienteAccessibileAdAgente(
                    originale.IdCliente, idAgenteFiltro.Value, originale.IdMandatario);
                // ==== MODIFICA 30/10/2026 - FINE ====

                if (!clienteProprio)
                    return Json(new { success = false, error = "Il cliente di questo ordine non è più associato al tuo profilo." });
            }
            // ==== MODIFICA 24/10/2026 - FINE ==== 14

            string utente = User.Identity?.Name ?? "Sistema";
            var nuovo = originale.CloneForInsert(utente);

            if (idAgenteFiltro.HasValue)
                nuovo.IdAgente = idAgenteFiltro.Value;   // 24/10/2026 15-il clone resta sempre dell'agente


            // ==== MODIFICA 02/11/2026 - INIZIO ====
            // Periodo (Anno/Mese) del clone scelto dal popup. Senza parametri resta quello della riga originale.
            if (!TryImpostaPeriodoDuplicato(nuovo, periodo, anno, mese, idAgenteFiltro.HasValue, out var errorePeriodo))
                return Json(new { success = false, error = errorePeriodo });
            // ==== MODIFICA 02/11/2026 - FINE ====           


            // 08/10/2026 se il numero ordine e' vuoto assegno quello temporaneo
            if (string.IsNullOrWhiteSpace(nuovo.NumeroOrdine))
                nuovo.NumeroOrdine = GeneraNumeroOrdineTemporaneo();

            try
            {
                _prospettoRepository.Insert(nuovo, utente);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Duplicazione ordine CST {Id} non riuscita", id);
                return Json(new { success = false, error = "Errore durante la duplicazione." });
            }

            TempData["Success"] = "Riga duplicata con successo.";
            return Json(new { success = true, newId = nuovo.IdProspettoCST });
        }

        // ==== MODIFICA 02/11/2026 - INIZIO ====
        /// <summary>
        /// Imposta Anno/Mese del clone in base alla scelta del popup Duplica.
        /// "originale" (o assente): periodo della riga copiata. "corrente": oggi. "scelta": anno/mese indicati.
        /// Agente: mai periodi passati ("originale" nel passato diventa il mese corrente).
        /// </summary>
        private static bool TryImpostaPeriodoDuplicato(ProspettoCST nuovo, string? periodo,
            int? anno, int? mese, bool isAgente, out string? errore)
        {
            errore = null;
            var oggi = DateTime.Today;
            int indiceCorrente = oggi.Year * 12 + oggi.Month;   // anno*12+mese: confronto tra periodi con un solo numero

            switch ((periodo ?? "originale").Trim().ToLowerInvariant())
            {
                case "originale":
                    // ==== MODIFICA 02/11/2026 ==== periodo mancante (righe vecchie) => mese corrente, per tutti i ruoli
                    if (!nuovo.Anno.HasValue || !nuovo.Mese.HasValue
                        || (isAgente && nuovo.Anno.Value * 12 + nuovo.Mese.Value < indiceCorrente))
                    {
                        nuovo.Anno = oggi.Year;
                        nuovo.Mese = oggi.Month;
                    }
                    return true;

                case "corrente":
                    nuovo.Anno = oggi.Year;
                    nuovo.Mese = oggi.Month;
                    return true;

                case "scelta":
                    // stessi limiti di Importa: da 5 anni fa a +1 anno
                    if (!anno.HasValue || !mese.HasValue || mese.Value is < 1 or > 12
                        || anno.Value < oggi.Year - 5 || anno.Value > oggi.Year + 1)
                    {
                        errore = "Anno o mese non validi.";
                        return false;
                    }
                    if (isAgente && anno.Value * 12 + mese.Value < indiceCorrente)
                    {
                        errore = "Puoi scegliere solo il mese corrente o un mese successivo.";
                        return false;
                    }
                    nuovo.Anno = anno.Value;
                    nuovo.Mese = mese.Value;
                    return true;

                default:
                    errore = "Periodo non valido.";
                    return false;
            }
        }
        // ==== MODIFICA 02/11/2026 - FINE ====





        [HttpPost]
        [Authorize(Roles = "Admin,Operatore")]//07/08/2026
        public IActionResult RipristinaAjax(int id)
        {
            try
            {
                _prospettoRepository.Ripristina(id, User.Identity?.Name ?? "sistema");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }




        //// ============================================================================
        ////  EXPORT EXCEL DINAMICO (ClosedXML)
        ////  - Applica gli stessi filtri della ListaOrdini
        ////  - Carica il JSON (colonne dinamiche)
        ////  - Passa righe + colonne al servizio Excel
        //// ============================================================================
        public IActionResult ExportExcelProspettoListaOrdini(int? idMandatario, int?
            idFileImportato, int? anno, int? mese, int? fatturata,
            int? daFatturare,            // NUOVO 30/09/2026
            int? idTipoPagamento,
        string inRitardo,    //05/10/2026  era bool? , valori: aperti | consegnati | tutti_ritardi | false bool? inRitardo, 
            int? giorniRitardoMin)  // <-- 02/10/2026 -8
        {
            // 1. Recupero i prospetti filtrati (stessa logica della view)
            // 25/03/2026: Ho aggiunto 'fatturata' alla chiamata. 
            // Poiché GetFiltrati ora esegue la SP con tutti i filtri, non serve più fare .Where(...).ToList() dopo.

            // GetFiltrati ora include anche idTipoPagamento           
            // 22/05/2026 Stessa normalizzazione di ListaOrdiniJson: 0 = "Tutti" → null La SP già gestisce 0 come "nessun filtro"
            if (idTipoPagamento == 0)
                idTipoPagamento = null;

            // 07/08/2026 INIZIO
            // 2. Carico la configurazione colonne dal JSON
            bool isAgente = User.IsInRole("Agente");// NUOVO 07/08/2026
            //int? idAgenteLoggato = isAgente ? User.GetIdAgente() : null;// NUOVO 07/08/2026

            // 13/10/2026 3 FAIL-CLOSED: un Agente senza collegamento (IdAgente nullo) NON deve passare null alla SP,
            // perché null = "nessun filtro" e vedrebbe TUTTI gli ordini. Il helper restituisce -1 (nessun match).
            int? idAgenteLoggato = GetIdAgenteFiltroOrdini();
            var prospetti = _prospettoRepository.GetFiltrati(
                idMandatario, idFileImportato, anno, mese, fatturata,
                mostraEliminati: false, idTipoPagamento: idTipoPagamento,
                idAgente: idAgenteLoggato, daFatturare); // 30/09/2026

            var config = _configService.Load("CST");
            config.Columns = config.Columns.FiltraPerRuolo(isAgente); // NUOVO 07/08/2026
            // 07/08/2026 FINE



            if (idMandatario.HasValue)
                HttpContext.Session.SetInt32("IdMandatario", idMandatario.Value);
            else
                idMandatario = HttpContext.Session.GetInt32("IdMandatario");

            // 25/03/2026: I blocchi "if (anno.HasValue) prospetti = ..." sono stati rimossi 
            // perché il filtraggio è già avvenuto a monte nella Stored Procedure.




            // 3. Genero il file Excel tramite servizio dedicato
            var nomeMandatario = "CST"; // oppure recuperalo dal DB
                                                         // Ricavo il nome del mese (es. "Marzo") partendo dal numero
                                                         // Se mese è null o 0, metto stringa vuota o "N/A"


            // ============================================================================
            // 29/09/2026 : CALCOLO DEI RITARDI PRIMA DI GENERARE L'EXCEL 1
            // ============================================================================

            var dataAttivazioneModulo = _config.GetDataAttivazioneModuloRitardo(nomeMandatario);

            foreach (var riga in prospetti)
            {
                RitardoOrdiniCalculator.Calcola(riga, dataAttivazioneModulo);
            }
            // ============================================================================





            // 02/10/2026 -12 INIZIO
            var prospettiFiltrati = FiltraRitardi(prospetti, inRitardo, giorniRitardoMin).ToList();//
            var bytes = _excelExportService.CreaExcelProspettoListaOrdini(prospettiFiltrati, config.Columns, nomeMandatario, mese ?? 0, anno ?? 0);
            // 02/10/2026 -FINE
            // 4. Restituisco il file
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ProspettoCST.xlsx");
        }

        //// ============================================================================
        ////  EXPORT PDF DINAMICO (QuestPDF)
        ////  - Stessi filtri della view
        ////  - Colonne dal JSON
        ////  - PDF dinamico senza colonne hardcoded
        //// ============================================================================
        public IActionResult ExportPdfProspettoListaOrdini(int? idMandatario, int? idFileImportato, int? anno, int? mese,
            int? fatturata,
            int? daFatturare,            // NUOVO 30/09/2026
            int? idTipoPagamento,
        string inRitardo,    //05/10/2026  era bool? , valori: aperti | consegnati | tutti_ritardi | false bool? inRitardo, 
            int? giorniRitardoMin)          // <-- 02/10/2026 -9
        {
            //22/05/2026  Stessa normalizzazione di ListaOrdiniJson: 0 = "Tutti" → null  La SP già gestisce 0 come "nessun filtro"
            if (idTipoPagamento == 0)
                idTipoPagamento = null;

            // 25/03/2026: Anche qui, aggiunto 'fatturata' e rimosso filtraggio manuale post-query.
            // 25/03/2026 filtra direttamente in SQL per evitare il flash di tutte le righe al caricamento della pagina

            //07/08/2026 INIZIO
            bool isAgente = User.IsInRole("Agente");
            //int? idAgenteLoggato = isAgente ? User.GetIdAgente() : null;
            
            // 13/10/2026 4 FAIL-CLOSED: un Agente senza collegamento (IdAgente nullo) NON deve passare null alla SP,
            // perché null = "nessun filtro" e vedrebbe TUTTI gli ordini. Il helper restituisce -1 (nessun match).
            int? idAgenteLoggato = GetIdAgenteFiltroOrdini();

            var prospetti = _prospettoRepository.GetFiltrati(
                idMandatario, idFileImportato, anno, mese, fatturata,
                mostraEliminati: false, idTipoPagamento: idTipoPagamento,
                idAgente: idAgenteLoggato
               , daFatturare); // 30/09/2026
            var config = _configService.Load("CST");
            config.Columns = config.Columns.FiltraPerRuolo(isAgente); // NUOVO 07/08/2026
            //07/08/2026 FINE

            if (idMandatario.HasValue)
                HttpContext.Session.SetInt32("IdMandatario", idMandatario.Value);
            else
                idMandatario = HttpContext.Session.GetInt32("IdMandatario");



            var nomeMandatario = "CST"; // oppure lookup DB

            // ============================================================================
            // 29/09/2026 : CALCOLO DEI RITARDI PRIMA DI GENERARE L'EXCEL 2
            // ============================================================================

            var dataAttivazioneModulo = _config.GetDataAttivazioneModuloRitardo(nomeMandatario);

            foreach (var riga in prospetti)
            {
                RitardoOrdiniCalculator.Calcola(riga, dataAttivazioneModulo);
            }
            // ============================================================================



            var prospettiFiltrati = FiltraRitardi(prospetti, inRitardo, giorniRitardoMin).ToList();//05/10/2026     
            var bytes = _pdfExportService.CreaPdfProspettoListaOrdini(
            prospettiFiltrati,//05/10/2026
            config.Columns,
            "Prospetto CST",
            nomeMandatario,
            mese ?? 0,
            anno ?? 0
            );

            return File(bytes, "application/pdf", "ProspettoCST.pdf");
        }

        // ============================================================================
        /// Recupera la percentuale di provvigione spettante all'agente per uno specifico cliente.
        /// Interroga la funzione scalare SQL [dbo.fn_GetPercentualeProvvigione].
        /// 
        /// La logica di recupero segue la gerarchia definita nel database: 
        /// 1. Cerca l'accordo specifico Agente-Cliente.
        /// 2. Cerca il default dell'Agente se l'accordo specifico non esiste
        // ============================================================================


        // ============================================================================
        // 20/10/2026 N5.4 - RISCRITTO: GetPercentualeProvvigione
        // L'Agente puo' chiedere la percentuale solo per se stesso e per un proprio cliente;
        // errore interno loggato sul server, al client solo un messaggio generico.
        // ============================================================================
        [HttpGet]
        public IActionResult GetPercentualeProvvigione(int idAgente, int idCliente)//20/10/2026
        {
            int idMandatario = HttpContext.Session.GetInt32("IdMandatario") ?? 0;
            if (idMandatario == 0) return BadRequest("Sessione scaduta o Mandatario non identificato.");

            var filtro = GetIdAgenteFiltroOrdini();
            if (filtro.HasValue)
            {
                // ==== MODIFICA 30/10/2026 - INIZIO ====
                // cliente "proprio" = agente di default OPPURE ProvvigioniAgenti per il mandatario di sessione
                //var clienteProprio = _clienteRepository.GetByMandatario(idMandatario, idCliente)
                //    .Any(c => c.IdCliente == idCliente && c.IdAgente == filtro.Value);
                //if (filtro.Value != idAgente || !clienteProprio)
                //    return Forbid();
                var clienteProprio = _clienteRepository.IsClienteAccessibileAdAgente(idCliente, filtro.Value, idMandatario);
                // 403 esplicito (non Forbid()): una chiamata AJAX con Forbid() riceverebbe un redirect HTML
                if (filtro.Value != idAgente || !clienteProprio)
                    return StatusCode(StatusCodes.Status403Forbidden);
                // ==== MODIFICA 30/10/2026 - FINE ====
            }

            try { return Json(_prospettoRepository.GetPercentualeAgente(idAgente, idCliente, idMandatario)); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore GetPercentualeAgente");
                return StatusCode(500, "Errore durante il recupero della percentuale.");
            }
        }



        // ============================================================================
        // 26/10/2026 2 AGENTE DEL CLIENTE NEL CONTESTO DEL MANDATARIO CORRENTE
        // Ordine di risoluzione:
        //   1) Clienti.IdAgente (anagrafica) se valorizzato;
        //   2) altrimenti l'agente indicato in ProvvigioniAgenti per (mandatario in sessione, cliente),
        //      cioe' la stessa fonte che usa fn_GetPercentualeProvvigione per calcolare la percentuale.
        // Prima la vista chiamava Clienti/GetAgenteCliente, che legge SOLO Clienti.IdAgente: un cliente con
        // anagrafica senza agente ma con accordo in ProvvigioniAgenti risultava "senza agente" e la provvigione
        // veniva azzerata.
        // Ruolo Agente: risposta solo se l'agente risolto e' proprio lui (altrimenti 403, come gli altri lookup).
        // 30/10/2026 Ruolo Agente: accesso se il cliente e' suo (default OPPURE ProvvigioniAgenti) e la risposta
        // e' sempre il suo IdAgente (con l'origine reale), anche se l'agente di default dell'anagrafica e' un altro
        // o e' vuoto.
        // ============================================================================
        [HttpGet]
        public IActionResult GetAgenteClienteMandatario(int idCliente)
        {
            int idMandatario = HttpContext.Session.GetInt32("IdMandatario") ?? 0;
            if (!VerificaMandatario(idMandatario))
                return BadRequest("Sessione scaduta o Mandatario non valido.");
            if (idCliente <= 0)
                return BadRequest("IdCliente non valido.");

            try
            {
                var cliente = _clienteRepository.GetById(idCliente);
                if (cliente == null)
                    return NotFound();

                // ==== MODIFICA 30/10/2026 - INIZIO ====
                // Ruolo Agente: il cliente deve essere suo (default OPPURE ProvvigioniAgenti per questo mandatario).
                // Se lo e', l'agente dell'ordine e' lui.
                var filtro = GetIdAgenteFiltroOrdini();
                if (filtro.HasValue)
                {
                    if (!_clienteRepository.IsClienteAccessibileAdAgente(idCliente, filtro.Value, idMandatario))
                        return StatusCode(StatusCodes.Status403Forbidden);

                    return Json(new
                    {
                        idAgente = (int?)filtro.Value,
                        origine = cliente.IdAgente == filtro.Value ? "Clienti" : "ProvvigioniAgenti"
                    });
                }
                // ==== MODIFICA 30/10/2026 - FINE ====

                // Admin / Operatore
                // 1) anagrafica cliente
                int? idAgente = (cliente.IdAgente.HasValue && cliente.IdAgente.Value > 0) ? cliente.IdAgente : null;
                string origine = "Clienti";

                // 2) fallback: accordo specifico cliente+mandatario in ProvvigioniAgenti
                if (idAgente is null
                    && _clienteRepository.GetMappaAgentiClienti(idMandatario).TryGetValue(idCliente, out var idAgenteProvvigione)
                    && idAgenteProvvigione > 0)
                {
                    idAgente = idAgenteProvvigione;
                    origine = "ProvvigioniAgenti";
                }

                // 30/10/2026 il vecchio controllo finale per l'Agente (if (filtro.HasValue && idAgente != filtro.Value) -> 403)
                // e' sostituito dal blocco "Ruolo Agente" sopra.

                return Json(new { idAgente, origine });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore GetAgenteClienteMandatario idCliente={IdCliente}", idCliente);
                return StatusCode(500, "Errore durante il recupero dell'agente del cliente.");
            }
        }













        [HttpGet]
        [Authorize(Roles = "Admin,Operatore")]//07/08/2026
        public IActionResult ReportStatistiche(int idMandatario, string nomeMandatario)
        {
            // Passa i dati alla View per il titolo e i campi hidden
            ViewBag.IdMandatario = idMandatario;
            ViewBag.NomeMandatario = nomeMandatario;

            // Usa la View in Areas/Admin/Views/Shared/ReportStatistiche.cshtml
            return View("ReportStatistiche");
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Operatore")]//20/10/2026 
        public IActionResult EsportaStatisticheExcel(int anno, string mandatario, int IdMandatario, string tipo)
        {
            string connString = _config.GetConnectionString("CalkosConnection");



            byte[] fileContents;

            // PASSA idMandatario AL SERVICE
            if (tipo == "RiepilogoFatture")//PIVOT
            {
                fileContents = _excelExportService.GeneraExcelRiepilogoFatture(anno, mandatario, connString, IdMandatario);
            }
            else if (tipo == "RiepilogoClienti")//RiepilogoClienti
            {
                fileContents = _excelExportService.GeneraExcelRiepilogoClienti(anno, mandatario, connString, IdMandatario);
            }
            else
            {
                return BadRequest("Tipo di report non valido.");
            }

            return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Report_{tipo}_{mandatario}_{anno}.xlsx");
        }



        [HttpGet]
        public IActionResult ListaOrdiniJson(int? idMandatario, int? idFileImportato, int? anno, int? mese,
            int? fatturata,
            int? daFatturare,           // NUOVO 30/09/2026
            int? idTipoPagamento,       // <-- NUOVO PARAMETRO  22/05/026          
            string inRitardo,    //05/10/2026  era bool? , valori: aperti | consegnati | tutti_ritardi | falsebool? inRitardo, 
            int? giorniRitardoMin,             // <-- 02/10/2026 NUOVO PARAMETRO -7        
            bool mostraEliminati = false)

        {
            // ==========================================================================================
            // 1. GESTIONE DEI FILTRI TEMPORALI (DEFAULTING LOGIC)
            // ==========================================================================================
            // Se i parametri 'anno' o 'mese' non sono presenti nella QueryString della richiesta HTTP, 
            // il sistema imposta come fallback il periodo corrente (DateTime.Now). 
            // Questo garantisce che la chiamata AJAX restituisca sempre dati coerenti anche al primo caricamento.

            if (!Request.Query.ContainsKey("anno"))
            {
                anno = DateTime.Now.Year;
            }

            if (!Request.Query.ContainsKey("mese"))
            {
                mese = DateTime.Now.Month;
            }

            // ==========================================================================================
            // 2. PERSISTENZA DELLO STATO DEL MANDATARIO (SESSION MANAGEMENT)
            // ==========================================================================================
            // Logica di sincronizzazione tra parametro di input e sessione utente:
            // - Se idMandatario è fornito (es. cambio selezione in UI), viene aggiornata la Sessione.
            // - Se idMandatario è null, il sistema tenta il recupero dalla Sessione per mantenere 
            //   il contesto operativo dell'utente durante la navigazione tra le pagine.

            // 26/10/2026 3 Uniformato alle altre action protette: il mandatario (da parametro o da sessione)
            // deve essere davvero CST, altrimenti si leggerebbero ordini di altri mandatari. Il controllo
            // avviene PRIMA di scrivere in sessione, cosi' un id non valido non la inquina.
            if (idMandatario.HasValue)
            {
                if (!VerificaMandatario(idMandatario.Value))
                    return BadRequest("Mandatario non valido.");

                HttpContext.Session.SetInt32("IdMandatario", idMandatario.Value);
            }
            else
            {
                idMandatario = HttpContext.Session.GetInt32("IdMandatario");

                if (!VerificaMandatario(idMandatario ?? 0))
                    return BadRequest("Sessione scaduta o Mandatario non valido.");
            }

            // ==========================================================================================
            // 3. NORMALIZZAZIONE FILTRO FATTURAZIONE
            // ==========================================================================================
            // Gestione del tri-stato del filtro fatturazione:
            // - Parametro assente: fatturata = null (mostra tutto).
            // - Valore convenzionale -1: mappato a null per annullare il filtro nel WHERE della Stored Procedure.
            // - Valore 0/1: filtro puntuale su righe "da fatturare" o "già fatturate".

            if (!Request.Query.ContainsKey("fatturata"))
            {
                fatturata = null;
            }
            else if (fatturata == -1)
            {
                fatturata = null;
            }


            // 02/10/2026 Normalizzazione giorni ritardo minimi -10
            //---------------------------------------------------------
            if (giorniRitardoMin.HasValue && giorniRitardoMin.Value <= 0)
            {
                giorniRitardoMin = null;
            }
            inRitardo = NormalizzaFiltroRitardo(inRitardo);//05/10/2026  valori: aperti | consegnati | tutti_ritardi | false
            ViewBag.InRitardo = inRitardo;                 // null / true / false
            ViewBag.GiorniRitardoMin = giorniRitardoMin;
            //---------------------------------------------------------

            // 22/05/2026 NORMALIZZAZIONE: 0 → null (= nessun filtro)
            if (idTipoPagamento == 0)
                idTipoPagamento = null;
            // ==========================================================================================
            // 4. DATA ACCESS LAYER (DAL) - ESECUZIONE QUERY
            // ==========================================================================================
            // Esecuzione della logica di estrazione tramite Repository. 
            // Il metodo GetFiltrati incapsula la chiamata alla Stored Procedure [dbo].[spProspettoCST_GetAllFiltri].
            // Viene passato anche il flag 'mostraEliminati' per la gestione della Soft Delete (Cestino).

            // 07/08/2026 17 inizio
            bool isAgente = User.IsInRole("Agente");
            //int? idAgenteLoggato = isAgente ? User.GetIdAgente() : null;
            
            // 13/10/2026 5 FAIL-CLOSED: un Agente senza collegamento (IdAgente nullo) NON deve passare null alla SP,
            // perché null = "nessun filtro" e vedrebbe TUTTI gli ordini. Il helper restituisce -1 (nessun match).
            int? idAgenteLoggato = GetIdAgenteFiltroOrdini();


            var prospetti = _prospettoRepository.GetFiltrati(
                idMandatario, idFileImportato, anno, mese, fatturata,
                mostraEliminati, idTipoPagamento, idAgenteLoggato
                , daFatturare); // 30/09/2026




            // =========================================================================
            // 29/09/2026      3
            // CALCOLO DINAMICO RITARDI PER OGNI ORDINE PRIMA DELLA SERIALIZZAZIONE JSON
            // =========================================================================

            var dataAttivazioneModulo = _config.GetDataAttivazioneModuloRitardo("CST");

            foreach (var riga in prospetti)
            {
                RitardoOrdiniCalculator.Calcola(riga, dataAttivazioneModulo);
            }
            // =========================================================================

            prospetti = FiltraRitardi(prospetti, inRitardo, giorniRitardoMin).ToList();   // <-- 02/10/2026 -1


            var config = _configService.Load("CST");
            prospetti = ProspettoDataMaskingHelper.MascheraPerAgente(prospetti, config.Columns, isAgente).ToList(); // NUOVO
            //07/08/2026 fine
            // ==========================================================================================
            // 5. SERIALIZZAZIONE JSON E DINAMISMO DATA-DRIVEN
            // ==========================================================================================
            // Il metodo restituisce un oggetto anonimo con proprietà "data", standard richiesto da DataTables.
            // VANTAGGIO ARCHITETTURALE: Restituendo l'intera collezione di oggetti 'ProspettoCST',
            // non vi è accoppiamento forte (Hard Coding) tra le colonne del DB e il backend.
            // Qualsiasi colonna aggiuntiva (es. campi tecnici CST) sarà automaticamente disponibile 
            // nel payload JSON e mappabile tramite la configurazione lato Client.

            return Json(new { data = prospetti });
        }

        //// ============================================================
        //// 26/08/2026 recupera TipoPagamento di Default del Cliente  
        //// ============================================================

        //[HttpGet]
        //public IActionResult GetTipoPagamentoDefault(int idCliente)
        //{
        //    try
        //    {
        //        // 1. Validazione parametro di input
        //        if (idCliente <= 0)
        //        {
        //            return BadRequest("IdCliente non valido.");
        //        }

        //        // 2. Interrogazione del repository per recuperare il DTO / oggetto di default
        //        var defaultDto = _clienteRepository.GetTipoPagamentoDefault(idCliente);

        //        if (defaultDto == null)
        //        {
        //            return Json(null);
        //        }

        //        // 3. Ritorno del risultato in formato JSON
        //        return Json(defaultDto);
        //    }
        //    catch (Exception ex)
        //    {
        //        // Tracciamento errore in console/log (pattern usato in GetPercentualeProvvigione)
        //        Console.WriteLine($"[GetTipoPagamentoDefault] Errore: {ex.Message}");
        //        return StatusCode(500, "Errore durante il recupero del tipo pagamento predefinito.");
        //    }
        //}
        [HttpGet]
        public IActionResult GetTipoPagamentoDefault(int idCliente)
        {
            try
            {
                // 1. Validazione parametro di input
                if (idCliente <= 0)
                {
                    return BadRequest("IdCliente non valido.");
                }

                // 26/10/2026 4 PROTEZIONE AGENTE (stessa regola di GetPercentualeProvvigione): l'Agente puo'
                // leggere il tipo pagamento solo di un cliente associato a lui. Admin/Operatore: nessun filtro.
                // 403 esplicito (non Forbid()): una chiamata AJAX con Forbid() riceverebbe un redirect HTML.
                var filtro = GetIdAgenteFiltroOrdini();
                if (filtro.HasValue)
                {
                    int idMandatario = HttpContext.Session.GetInt32("IdMandatario") ?? 0;
                    if (idMandatario == 0)
                        return BadRequest("Sessione scaduta o Mandatario non identificato.");

                    // ==== MODIFICA 30/10/2026 - INIZIO ====
                    // agente di default OPPURE ProvvigioniAgenti (mandatario di sessione) + cliente collegato al mandatario
                    //bool clienteProprio = _clienteRepository.GetByMandatario(idMandatario, idCliente)
                    //    .Any(c => c.IdCliente == idCliente && c.IdAgente == filtro.Value);
                    bool clienteProprio = _clienteRepository.IsClienteAccessibileAdAgente(idCliente, filtro.Value, idMandatario);
                    // ==== MODIFICA 30/10/2026 - FINE ====

                    if (!clienteProprio)
                        return StatusCode(StatusCodes.Status403Forbidden);
                }

                // 2. Interrogazione del repository per recuperare il DTO / oggetto di default
                var defaultDto = _clienteRepository.GetTipoPagamentoDefault(idCliente);

                if (defaultDto == null)
                {
                    return Json(null);
                }

                // 3. Ritorno del risultato in formato JSON
                return Json(defaultDto);
            }
            catch (Exception ex)
            {
                // 26/10/2026 5 log strutturato (come GetPercentualeProvvigione) invece di Console.WriteLine
                _logger.LogError(ex, "Errore GetTipoPagamentoDefault idCliente={IdCliente}", idCliente);
                return StatusCode(500, "Errore durante il recupero del tipo pagamento predefinito.");
            }
        }
        // ==========================================================================================
        //29/09/2026  RITARDI ORDINI AJAX INIZIO 4
        // ==========================================================================================
        [HttpPost]
        [Authorize(Roles = "Admin,Operatore")]
        public IActionResult AggiornaDataConsegnaAjax(int id, DateTime? dataConsegna)
        {
            try
            {
                string utente = User.Identity?.Name ?? "Sistema";
                var riga = _prospettoRepository.GetById(id);
                if (riga == null)
                    return Json(new { success = false, error = "Ordine non trovato." });

                riga.DataConsegna = dataConsegna;

                // Calcolo dinamico
                var dataAttivazioneModulo = _config.GetDataAttivazioneModuloRitardo("CST");
                RitardoOrdiniCalculator.Calcola(riga, dataAttivazioneModulo);

                _prospettoRepository.Update(riga, utente);

                return Json(new
                {
                    success = true,
                    inRitardo = riga.InRitardo,
                    giorniRitardo = riga.GiorniRitardo
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Operatore")]
        public IActionResult ToggleEscludiRitardoAjax(int id, bool escludi)
        {
            try
            {
                string utente = User.Identity?.Name ?? "Sistema";
                var riga = _prospettoRepository.GetById(id);
                if (riga == null)
                    return Json(new { success = false, error = "Ordine non trovato." });

                riga.EscludiControlloRitardo = escludi;

                // Calcolo dinamico
                var dataAttivazioneModulo = _config.GetDataAttivazioneModuloRitardo("CST");
                RitardoOrdiniCalculator.Calcola(riga, dataAttivazioneModulo);

                _prospettoRepository.Update(riga, utente);

                return Json(new
                {
                    success = true,
                    inRitardo = riga.InRitardo,
                    giorniRitardo = riga.GiorniRitardo
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
        // ==========================================================================================
        //29/09/2026  RITARDI ORDINI AJAX FINE 4
        // ==========================================================================================

        // ============================================================
        // 02/10/2026 Filtro Ritardi (Aperti in ritardo + Consegnati con ritardo) -11
        // ============================================================
        // 05/10/2026 Accetta solo i valori previsti; il vecchio "true" diventa "tutti_ritardi"
        private static string NormalizzaFiltroRitardo(string valore)
        {
            switch ((valore ?? "").Trim().ToLowerInvariant())
            {
                case "aperti": return "aperti";
                case "consegnati": return "consegnati";
                case "tutti_ritardi":
                case "true": return "tutti_ritardi";
                case "false": return "false";
                default: return null;
            }
        }

        // Ha accumulato ritardo (aperto scaduto O consegnato oltre la data ipotetica); gli esclusi non contano mai
        private static bool HaRitardo(ProspettoCST r)
        {
            return !r.EscludiControlloRitardo && r.GiorniRitardo.GetValueOrDefault() > 0;
        }

        private static IEnumerable<ProspettoCST> FiltraRitardi(
            IEnumerable<ProspettoCST> righe, string tipoRitardo, int? giorniRitardoMin)
        {
            switch (NormalizzaFiltroRitardo(tipoRitardo))
            {
                case "aperti":
                    righe = righe.Where(r => r.InRitardo && !r.EscludiControlloRitardo);
                    break;
                case "consegnati":
                    righe = righe.Where(r => r.DataConsegna.HasValue && HaRitardo(r));
                    break;
                case "tutti_ritardi":
                    righe = righe.Where(r => HaRitardo(r));
                    break;
                case "false":
                    righe = righe.Where(r => !HaRitardo(r));
                    break;
            }

            // Giorni minimi: solo ordini con ritardo, mai gli esclusi dal controllo
            if (giorniRitardoMin.HasValue && giorniRitardoMin.Value > 0)
                righe = righe.Where(r => HaRitardo(r) && r.GiorniRitardo.Value >= giorniRitardoMin.Value);

            return righe;
        }

    }
}
