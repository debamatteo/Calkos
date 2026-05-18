using Calkos.web.Helpers;
using Calkos.web.Models.DTO;
using Calkos.web.Models.ViewModels.Prospetti;
using Calkos.web.Services.Export;
using Calkos.web.Services.Prospetti;
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
    [Authorize]
    // Antiforgery 05/04/2026: Aggiunto per centralizzare la validazione Antiforgery su tutte le POST del controller
    [AutoValidateAntiforgeryToken]
    public class ProspettiSystemPController : Controller
    {
        /*
         * Questo controller gestisce TUTTO ciò che riguarda SystemP:
         * - Importazione file Excel SystemP
         * - Conversione righe Excel → ImportSystemP
         * - Salvataggio in FileImportato + ProspettoSystemP
         * - Visualizzazione lista ordini SystemP
         * 
         * È la versione SPECIALIZZATA del vecchio ProspettiController,
         * ma dedicata SOLO al Mandatario SystemP.
         */


        /*
         * Il repository SystemP.
         * 
         * Il controller NON deve conoscere come il repository funziona internamente.
         * Usa SOLO l’interfaccia IProspettoSystemPRepository.
         * 
         * In Program.cs abbiamo:
         * services.AddScoped<IProspettoSystemPRepository, ProspettoSystemPRepository>();
         * 
         * Questo significa:
         * “Quando qualcuno chiede IProspettoSystemPRepository, dagli ProspettoSystemPRepository”.
         */
        // Il servizio che contiene tutta la pipeline di importazione.
        // Il controller NON fa logica: delega tutto al service.
        private readonly ImportazioneSystemPService _ImportazioneSystemPService;

        private readonly IProspettoSystemPRepository _prospettoRepository;
        private readonly IAgenteRepository _agenteRepository; // serve per lookup agenti
        private readonly IFileBackupService _fileBackupService; // serve per backup file SystemP
        private readonly IClienteRepository _clienteRepository; // serve per lookup clienti
        private readonly IMaterialeRepository _materialeRepository; // serve per lookup materiale
        private readonly ITipoPagamentoRepository _tipoPagamentoRepository; // serve per lookup tipo pagamento
        private readonly IUnitaMisuraRepository _unitaMisuraRepository; // serve per lookup unità di misura
        private readonly ProspettoConfigService _configService;

        private readonly ExcelExportService _excelExportService;
        private readonly PdfExportService _pdfExportService;
        private readonly IConfiguration _config;//legge i parametri da appsettings.json
        private readonly MandatarioService _mandatarioService;

        public ProspettiSystemPController(
            ImportazioneSystemPService ImportazioneSystemPService,
            IProspettoSystemPRepository prospettoRepository,
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
             MandatarioService mandatarioService)//legge i parametri da appsettings.json
        {
            _ImportazioneSystemPService = ImportazioneSystemPService;
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
        }



        // ============================================================
        // INIZIO IMPORTAZIONE SystemP
        //  GET: PAGINA DI IMPORTAZIONE SystemP
        // ============================================================


        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Importa(int idMandatario)
        {
            // Salvo IdMandatario in sessione
            HttpContext.Session.SetInt32("IdMandatario", idMandatario);

            // Recupero il tipo pagamento del mandatario
            int tipoPagamento = _mandatarioService.GetTipoPagamento(idMandatario);

            // Lo salvo in sessione per tutto il flusso SystemP
            HttpContext.Session.SetInt32("TipoPagamentoMandatario", tipoPagamento);

            // Mantengo anche il ViewBag se serve
            ViewBag.IdMandatario = idMandatario;
            ViewBag.TipoPagamento = tipoPagamento;

            return View();
        }

        // ============================================================
        // POST: ESECUZIONE IMPORTAZIONE SystemP
        // ============================================================
        /*
         * Questo metodo:
         * 1. Legge il file Excel
         * 2. Converte ogni riga in ImportSystemP
         * 3. Crea un FileImportato
         * 4. Avvia la pipeline di importazione tramite ImportazioneSystemPService
         * 5. Mostra una modale di conferma
         */

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Importa(IFormFile fileExcel)//, int idMandatario ora uso la sessione
        {
            // Recupero IdMandatario dalla sessione (stabile, non dipende dal browser)
            int idMandatario = HttpContext.Session.GetInt32("IdMandatario") ?? 0;//uso la sessione
            if (idMandatario == 0)
            {
                TempData["Errore"] = "Si è persa la Sessione del  Mandatario .Ritorna alla Pagina Iniziale";
                return View();
            }

            if (fileExcel == null || fileExcel.Length == 0)
            {
                TempData["Errore"] = "Seleziona un file Excel valido.";
                return View();
            }


            // 0. Salvo il file in una cartella temporanea
            var tempFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "tempSystemP");
            Directory.CreateDirectory(tempFolder);

            var tempFileName = $"SystemP_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(fileExcel.FileName)}";
            var tempFilePath = Path.Combine(tempFolder, tempFileName);

            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                fileExcel.CopyTo(stream);
            }


            // 1. BACKUP FISICO DEL FILE SystemP (con gestione errori elegante)
            try
            {
                _fileBackupService.BackupSystemPAsync(tempFilePath).Wait();
            }
            catch (Exception ex)
            {
                // Non blocchiamo l'importazione SystemP
                // Registriamo l'errore in TempData (o log)
                TempData["BackupWarning"] = "Backup non eseguito: " + ex.Message;

                // Se hai un logger, puoi usare:
                // _logger.LogError(ex, "Errore durante il backup del file SystemP.");
            }



            // 2. Leggo l’Excel → List<RigaExcelSystemP>
            int firstRow = _config.GetValue<int>("ImportazioneSystemP:FirstRow");
            // CREA LA LISTA DEI VALORI AMMESSI (OK, SI, TRUE, ecc.)
            //14/04/2026  operatore ?? per dare una lista vuota se manca la sezione
            // Se la lista è null o vuota, inizializzo una nuova lista con "OK"
            HashSet<string> valoriAmmessi = (_config.GetSection("ValoriFatturareTrue").Get<List<string>>() ?? new List<string>())
                .DefaultIfEmpty("OK") // Se la lista è vuota, aggiunge "OK" come elemento predefinito
                .Select(v => v.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // PASSA I VALORI AMMESSI//14/04/2026
            //var righe = ExcelHelper.LeggiExcelSystemP(fileExcel, firstRow, valoriAmmessi);
            //15/04/2026
            List<RigaExcelSystemP> righe;
            try
            {
                righe = ExcelHelper.LeggiExcelSystemP(fileExcel, firstRow, valoriAmmessi);
            }
            catch (Exception ex)
            {
                TempData["Errore"] = "Errore durante la lettura del file Excel: " + ex.Message;
                return View();
            }
            // 3. Converto ogni riga in ImportSystemP
            //Select = trasforma ogni elemento della collezione;
            //r => Converti(r) = funzione lambda= Per ogni elemento r in righe, applica il metodo Converti(r);
            var righeImportSystemP = righe.Select(r => Converti(r)).ToList();
            //15/04/2026
            if (righeImportSystemP.Count == 0)
            {
                TempData["Errore"] = "Non ci sono dati  da importare.";
                return View();
            }




            // 4. Creo FileImportato
            string utente = User.Identity?.Name ?? "Sistema";

            int anno = int.Parse(Request.Form["Anno"]);
            int mese = int.Parse(Request.Form["Mese"]);

            var file = new FileImportato
            {
                IdMandatario = idMandatario,//uso la sessione
                NomeFile = fileExcel.FileName,
                DataImportazione = DateTime.Now,
                Utente = utente,
                Anno = anno,
                Mese = mese

            };

            // 5. Avvio importazione ImportazioneSystemPService

            //gestione del tipopagamento di default del mandatario tramite sessione, così non devo passarlo come parametro da form o URL
            int IdTipoPagamento = HttpContext.Session.GetInt32("TipoPagamentoMandatario") ?? 0;
            //15/04/2026
            //int idFile = _ImportazioneSystemPService.ImportaFile(file, righeImportSystemP, utente, IdTipoPagamento); 
            int idFile;
            try
            {
                idFile = _ImportazioneSystemPService.ImportaFile(file, righeImportSystemP, utente, IdTipoPagamento);
            }
            catch (Exception ex)
            {
                TempData["Errore"] = "Errore durante l'importazione dei dati: " + ex.Message;
                return View();
            }

            // 6. Mostra modale di conferma
            TempData["ImportSuccess"] = true;
            TempData["IdFileImportato"] = idFile;
            // 7. Pulizia del file temporaneo (non blocca l'importazione)
            try
            {
                if (System.IO.File.Exists(tempFilePath))
                    System.IO.File.Delete(tempFilePath);
            }
            catch
            {
                // Non blocchiamo nulla se la cancellazione fallisce
            }
            //return RedirectToAction("Importa", "ProspettiSystemP", new { area = "Admin" });
            //Così la pagina Importa si ricarica con il Mandatario corretto
            return RedirectToAction("Importa", "ProspettiSystemP", new { area = "Admin", idMandatario = file.IdMandatario });

        }


        // ============================================================
        // CONVERSIONE RIGA EXCEL SystemP → ImportSystemP
        // ============================================================
        /*
         * Questo metodo converte una riga Excel SystemP
         * nel modello ImportSystemP usato dalla pipeline.
         * 
         * È specifico per SystemP, quindi sta nel controller SystemP.
         */

        private ImportSystemP Converti(RigaExcelSystemP r)
        {
            var i = new ImportSystemP();

            // ============================
            // Campi base
            // ============================
            i.OrdineDDT = r.OrdineDDT;
            i.Cliente = r.Cliente;

            // ============================
            // Quantità e materiale
            // ============================
            i.Kg = r.Kg;
            i.Materiale = r.Materiale;

            // ============================
            // Prezzi di acquisto
            // ============================
            i.LavorazionePrezzoAcquisto = r.LavorazionePrezzoAcquisto;
            i.BaseAcquisto = r.BaseAcquisto;
            i.CIT = r.CIT;
            i.PrezzoAcquisto = r.PrezzoAcquisto;
            i.DataRiferimentoPrezzoAcquisto = r.DataRiferimentoPrezzoAcquisto;
            i.CIT_EUR_KG = r.CIT_EUR_KG;
            i.CITsuAcquisto = r.CITsuAcquisto;

            // ============================
            // Provvigioni fisse e unitarie
            // ============================
            i.ProvvigioneUnitaria = r.ProvvigioneUnitaria;
            i.ValoreProvvigioneFissa = r.ValoreProvvigioneFissa;

            // ============================
            // Prezzi di vendita
            // ============================
            i.LavorazionePrezzoVendita = r.LavorazionePrezzoVendita;
            i.BaseVendita = r.BaseVendita;
            i.CITsuVendita = r.CITsuVendita;
            i.PrezzoVendita = r.PrezzoVendita;

            // ============================
            // Delta e percentuali
            // ============================
            i.Delta = r.Delta;
            i.PercentualeDelta = r.PercentualeDelta;

            // ============================
            // Provvigioni variabili e totali
            // ============================
            i.TotaleVariabileProvvigione = r.TotaleVariabileProvvigione;
            i.TotaleProvvigione = r.TotaleProvvigione;

            // ============================
            // Date
            // ============================
            i.DataConsegnaIpotetica = r.DataConsegnaIpotetica;
            i.Agente = r.Agente;
            i.Scadenza = r.Scadenza;

            // ============================
            // Fatturare (stringa Excel)
            // ============================
            i.FatturareStringa = r.Fatturare;

            return i;
        }





        // ============================================================
        // FINE  IMPORTAZIONE SystemP
        // ============================================================



        //// ============================================================
        //// LISTA ORDINI SystemP
        //// ============================================================
        ///*
        // * Questa action mostra:
        // * - TUTTI gli ordini SystemP
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

        public IActionResult ListaOrdini(int? idMandatario, int? idFileImportato, int? anno, int? mese, int? fatturata, bool mostraEliminati = false)//25/03/2026 aggiunto filtro fatturata
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

            // ============================================================
            // 2) RECUPERO DATI FILTRATI (ADO.NET OTTIMIZZATO)
            // ============================================================

            // NUOVO APPROCCIO: Chiamiamo il database chiedendo SOLO le righe che servono.
            //  Ho aggiunto 'fatturata' alla chiamata GetFiltrati. 
            // Dovrai aggiornare la firma del metodo nel tuo Repository per accettare questo nuovo int?.
            var prospetti = _prospettoRepository.GetFiltrati(idMandatario, idFileImportato, anno, mese, fatturata, mostraEliminati);

            sw.Stop();
            System.Diagnostics.Debug.WriteLine("TEMPO QUERY: " + sw.ElapsedMilliseconds + " ms");


            // Passiamo i filtri alla view per mantenerli selezionati nelle select del form
            ViewBag.IdFileImportato = idFileImportato;
            ViewBag.Anno = anno;
            ViewBag.Mese = mese;
            ViewBag.Fatturata = fatturata; //  Necessario per far funzionare il 'selected' nella View
                                           // PASSA IL VALORE ALLA VIEW PER MANTENERE LA SELEZIONE NELLA DROPDOWN
            ViewBag.MostraEliminati = mostraEliminati;

            // ============================================================
            // 3) CONFIGURAZIONE DINAMICA E VIEWMODEL
            // ============================================================

            // 1. Carico la configurazione colonne dal file JSON
            var config = _configService.Load("systemp");

            // Controller
            //    ├── filtra prospetti (ora avviene direttamente in SQL tramite GetFiltrati)
            //    ├── carica JSON
            //    └── crea ViewModel
            //           ├── Righe = prospetti filtrati
            //           └── Colonne = config.Columns
            // → View

            // 2. Costruisco il ViewModel dinamico
            var vm = new ProspettoSystemPListaViewModel
            {
                Righe = prospetti, // prospetti = i dati (le righe reali del DB filtrate alla sorgente)
                Colonne = config.Columns // la configurazione colonne (Colonne)
            };

            // 3. Passo il ViewModel alla view
            return View(vm);
        }



        // ============================================================
        // GET:  DETTAGLIO ORDINE SystemP (SOLO ADMIN)
        // ============================================================



        [HttpGet]
        [Authorize(Roles = "Admin,Operatore")]
        public IActionResult DettaglioOrdine(int? id)
        {
            // 1️⃣ Se arrivo da DUPLICA AJAX → uso il modello duplicato
            if (TempData["Duplicato"] != null)
            {
                var duplicato = JsonConvert.DeserializeObject<ProspettoSystemP>(TempData["Duplicato"].ToString());
                CaricaLookup(duplicato);
                return View(duplicato);
            }

            // 2️⃣ Flusso normale: serve ID
            if (!id.HasValue)
                return NotFound();

            var ordine = _prospettoRepository.GetById(id.Value);

            if (ordine == null)
                return NotFound();

            // 3️⃣ Carico i lookup
            var agenti = _agenteRepository.GetAll();
            var clienti = _clienteRepository.GetByMandatario(ordine.IdMandatario);
            var materiali = _materialeRepository.GetAll();
            var tipiPagamento = _tipoPagamentoRepository.GetAll();
            var unitaMisura = _unitaMisuraRepository.GetAll();

            CaricaLookup(ordine);


            return View(ordine);
        }



        // ============================================================
        // POST: SALVATAGGIO DETTAGLIO ORDINE SystemP (SOLO ADMIN)
        // ============================================================

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult DettaglioOrdine(ProspettoSystemP model)
        {
            /*
             * Questa action salva le modifiche effettuate sul dettaglio ordine.
             * Solo gli ADMIN possono modificare i dati.
             */
            int idMandatario = HttpContext.Session.GetInt32("IdMandatario") ?? 0;
            model.IdMandatario = idMandatario;


            if (idMandatario == 0)
            {
                ModelState.AddModelError("", "Sessione scaduta o Mandatario non identificato");
                CaricaLookup(model);
                return View(model);
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
                if (model.IdProspettoSystemP != 0)
                {
                    //_prospettoRepository.Update(model, utente);//03/04/2026 REMMATA
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
                    model.IdProspettoSystemP = nuovoId;//03/04/2026 NUOVA
                    TempData["ModalMessage"] = "Nuova riga inserita con successo.";//03/04/2026 NUOVA
                    return RedirectToAction("ListaOrdini");//03/04/2026 NUOVA

                    //model.IdProspettoSystemP = nuovoId;//03/04/2026 REMMATA

                }

                //// 4) Messaggio di conferma//03/04/2026 REMMATA
                //TempData["Success"] = "Ordine salvato con successo.";//03/04/2026 REMMATA
            }
            catch (Exception ex)
            {
                // Gestione errori database (es. stored procedure fallita)
                ModelState.AddModelError("", "Errore durante il salvataggio: " + ex.Message);
                CaricaLookup(model);
                return View(model);
            }

            // 5) Redirect alla stessa pagina //03/04/2026 REMMATA
            // Usiamo l'ID del modello per assicurarci di tornare sull'ordine corretto
            //return RedirectToAction("DettaglioOrdine", new { id = model.IdProspettoSystemP });//03/04/2026 resto nel dettaglio
            //return RedirectToAction("ListaOrdini", new { id = model.IdProspettoSystemP });//03/04/2026 remmata
        }
        public IActionResult GetDimensioniMateriale(int idMateriale)
        {
            var spessori = _prospettoRepository.GetDimensioniMateriale(idMateriale, "Spessore");
            var larghezze = _prospettoRepository.GetDimensioniMateriale(idMateriale, "Larghezza");

            return Json(new
            {
                spessori,
                larghezze
            });
        }
        //// ============================================================
        //// CARICAMENTO LOOKUP PER LA VIEW DETTAGLIO ORDINE
        //// ============================================================

        private void CaricaLookup(ProspettoSystemP ordine)
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

            // Clienti (filtrati per Mandatario)
            //var clienti = _clienteRepository.GetByMandatario(ordine.IdMandatario);
            //var listaClienti = clienti.Select(c => new SelectListItem
            //{
            //    Value = c.IdCliente.ToString(),
            //    Text = c.RagioneSociale
            //}).ToList();

            //listaClienti.Insert(0, new SelectListItem
            //{
            //    Value = "",
            //    Text = "— —"
            //});

            //ViewBag.Clienti = listaClienti;


            // MODIFICA 31/03/2026: Passiamo l'IdCliente dell'ordine corrente 
            // per assicurarci che venga caricato anche se è stato eliminato (Soft Delete)
            var clienti = _clienteRepository.GetByMandatario(ordine.IdMandatario, ordine.IdCliente);

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
        //// NUOVA RIGA ORDINE SystemP (SOLO ADMIN)
        //// ============================================================
        public IActionResult Nuovo()
        {
            int idMandatario = HttpContext.Session.GetInt32("IdMandatario") ?? 0;

            var model = new ProspettoSystemP();

            // --- CHIAVI E METADATI OBBLIGATORI ---
            model.IdMandatario = idMandatario;
            model.IdFileImportato = 0;
            model.IdImportSystemP = 0;
            model.IdCliente = 0;
            model.IdMateriale = 0;
            model.IdUnitaMisura = 0;
            model.Utente = User.Identity?.Name ?? "Sistema";
            model.DataInserimento = DateTime.Now;

            // --- DATE E PERIODI ---
            model.DataConsegnaIpotetica = DateTime.Today;
            model.Scadenza = DateTime.Today;
            model.DataRiferimentoPrezzoAcquisto = DateTime.Today;
            model.Anno = DateTime.Today.Year;
            model.Mese = DateTime.Today.Month;

            // --- DOCUMENTO (EVITA STRINGHE NULL) ---
            model.NumeroOrdine = string.Empty;
            model.NumeroDDT = string.Empty;
            model.NumeroFattura = string.Empty;

            // --- LOGICA PREZZI E CIT (DECIMAL 18,6) ---
            model.Quantita = 0m;
            model.LavorazionePrezzoAcquisto = 0m;
            model.BaseAcquisto = 0m;
            model.CIT = 0m;
            model.PrezzoAcquisto = 0m;
            model.CIT_EUR_KG = 0m;
            model.CITsuAcquisto = 0m;

            // --- LOGICA VENDITA E PROVVIGIONI ---
            model.LavorazionePrezzoVendita = 0m;
            model.BaseVendita = 0m;
            model.CITsuVendita = 0m;
            model.PrezzoVendita = 0m;
            model.ProvvigioneUnitaria = 0m;
            model.ValoreProvvigioneFissa = 0m;
            model.Delta = 0m;
            model.PercentualeDelta = 0m;
            model.TotaleVariabileProvvigione = 0m;
            model.TotaleProvvigione = 0m;
            model.ProvvigioneAgente = 0m;

            // --- STATI E FLAG ---
            model.Fatturata = 0;
            model.IsDeleted = false;

            // --- CARICAMENTO LOOKUP E RITORNO ---
            CaricaLookup(model);

            return View("DettaglioOrdine", model);
        }

        //// ============================================================
        //// DUPLICA  (SOLO ADMIN)
        //// ============================================================

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
        public IActionResult CancellaAjaxDatabase(int id)
        {
            try
            {
                _prospettoRepository.Delete(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
        [HttpPost]
        public IActionResult CancellaAjax(int id)
        {
            try
            {
                // RECUPERA L'UTENTE DALLA SESSIONE O DAL CONTESTO
                string utenteLoggato = User.Identity?.Name ?? "Admin";

                // USA IL METODO CORRETTO!
                _prospettoRepository.SoftDelete(id, utenteLoggato);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
        [HttpPost]
        public IActionResult ResetFatturazioneAjax(int id)
        {
            var ok = _prospettoRepository.ResetFatturazione(id, User.Identity?.Name ?? "sistema");
            return Json(new { success = ok });
        }

        [HttpPost]
        public IActionResult DuplicaAjax(int id)//03/04/2026 modificata
        {
            var originale = _prospettoRepository.GetById(id);
            if (originale == null)
                return Json(new { success = false, error = "Riga non trovata." });

            // Duplica
            var nuovo = originale.CloneForInsert(User.Identity?.Name);

            // Salva nel DB
            _prospettoRepository.Insert(nuovo, User.Identity?.Name);

            // Imposto il messaggio che verrà mostrato DOPO il redirect al dettaglio
            TempData["Success"] = "Riga duplicata con successo.";

            // Restituisco l'ID nuovo
            return Json(new { success = true, newId = nuovo.IdProspettoSystemP });
        }



        [HttpPost]
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




        // ============================================================================
        //  EXPORT EXCEL DINAMICO (ClosedXML)
        //  - Applica gli stessi filtri della ListaOrdini
        //  - Carica il JSON (colonne dinamiche)
        //  - Passa righe + colonne al servizio Excel
        // ============================================================================
        public IActionResult ExportExcelProspettoListaOrdini(int? idMandatario, int? idFileImportato, int? anno, int? mese, int? fatturata)
        {
            // 1. Recupero i prospetti filtrati (stessa logica della view)
            // 25/03/2026: Ho aggiunto 'fatturata' alla chiamata. 
            // Poiché GetFiltrati ora esegue la SP con tutti i filtri, non serve più fare .Where(...).ToList() dopo.
            var prospetti = _prospettoRepository.GetFiltrati(idMandatario, idFileImportato, anno, mese, fatturata);

            if (idMandatario.HasValue)
                HttpContext.Session.SetInt32("IdMandatario", idMandatario.Value);
            else
                idMandatario = HttpContext.Session.GetInt32("IdMandatario");

            // 25/03/2026: I blocchi "if (anno.HasValue) prospetti = ..." sono stati rimossi 
            // perché il filtraggio è già avvenuto a monte nella Stored Procedure.

            // 2. Carico la configurazione colonne dal JSON
            var config = _configService.Load("systemp");

            // 3. Genero il file Excel tramite servizio dedicato
            var nomeMandatario = "SystemP"; // oppure recuperalo dal DB
                                            // Ricavo il nome del mese (es. "Marzo") partendo dal numero
                                            // Se mese è null o 0, metto stringa vuota o "N/A"

            var bytes = _excelExportService.CreaExcelProspettoListaOrdini(prospetti, config.Columns, nomeMandatario, mese ?? 0, anno ?? 0);

            // 4. Restituisco il file
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ProspettoSystemP.xlsx");
        }

        // ============================================================================
        //  EXPORT PDF DINAMICO (QuestPDF)
        //  - Stessi filtri della view
        //  - Colonne dal JSON
        //  - PDF dinamico senza colonne hardcoded
        // ============================================================================
        public IActionResult ExportPdfProspettoListaOrdini(int? idMandatario, int? idFileImportato, int? anno, int? mese, int? fatturata)
        {
            // 25/03/2026: Anche qui, aggiunto 'fatturata' e rimosso filtraggio manuale post-query.
            // 25/03/2026 filtra direttamente in SQL per evitare il flash di tutte le righe al caricamento della pagina
            var prospetti = _prospettoRepository.GetFiltrati(idMandatario, idFileImportato, anno, mese, fatturata);

            if (idMandatario.HasValue)
                HttpContext.Session.SetInt32("IdMandatario", idMandatario.Value);
            else
                idMandatario = HttpContext.Session.GetInt32("IdMandatario");

            var config = _configService.Load("systemp");

            var nomeMandatario = "SystemP"; // oppure lookup DB

            var bytes = _pdfExportService.CreaPdfProspettoListaOrdini(
                prospetti,
                config.Columns,
                "Prospetto SystemP",
                nomeMandatario,
                mese ?? 0,
                anno ?? 0
            );

            return File(bytes, "application/pdf", "ProspettoSystemP.pdf");
        }

        // ============================================================================
        /// Recupera la percentuale di provvigione spettante all'agente per uno specifico cliente.
        /// Interroga la funzione scalare SQL [dbo.fn_GetPercentualeProvvigione].
        /// 
        /// La logica di recupero segue la gerarchia definita nel database: 
        /// 1. Cerca l'accordo specifico Agente-Cliente.
        /// 2. Cerca il default dell'Agente se l'accordo specifico non esiste
        // ============================================================================
        [HttpGet]
        public IActionResult GetPercentualeProvvigione(int idAgente, int idCliente)
        {
            try
            {
                // 1. Recupero l'IdMandatario dalla Sessione (standard del tuo progetto)
                int idMandatario = HttpContext.Session.GetInt32("IdMandatario") ?? 0;

                if (idMandatario == 0)
                {
                    return BadRequest("Sessione scaduta o Mandatario non identificato.");
                }

                // 2. Interrogo il Repository (che a sua volta chiama la funzione SQL)
                decimal percentuale = _prospettoRepository.GetPercentualeAgente(idAgente, idCliente, idMandatario);

                // 3. Rispondo allo script con il numero puro (es: 3.50)
                return Json(percentuale);
            }
            catch (Exception ex)
            {
                // Log dell'errore (opzionale, utile per il debug)
                return StatusCode(500, "Errore durante il recupero della percentuale.");
            }
        }


        [HttpGet]
        public IActionResult ReportStatistiche(int idMandatario, string nomeMandatario)
        {
            // Passa i dati alla View per il titolo e i campi hidden
            ViewBag.IdMandatario = idMandatario;
            ViewBag.NomeMandatario = nomeMandatario;

            // Usa la View in Areas/Admin/Views/Shared/ReportStatistiche.cshtml
            return View("ReportStatistiche");
        }

        [HttpGet]
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
        public IActionResult ListaOrdiniJson(int? idMandatario, int? idFileImportato, int? anno, int? mese, int? fatturata, bool mostraEliminati = false)
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

            if (idMandatario.HasValue)
            {
                HttpContext.Session.SetInt32("IdMandatario", idMandatario.Value);
            }
            else
            {
                idMandatario = HttpContext.Session.GetInt32("IdMandatario");
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

            // ==========================================================================================
            // 4. DATA ACCESS LAYER (DAL) - ESECUZIONE QUERY
            // ==========================================================================================
            // Esecuzione della logica di estrazione tramite Repository. 
            // Il metodo GetFiltrati incapsula la chiamata alla Stored Procedure [dbo].[spProspettoSystemP_GetAllFiltri].
            // Viene passato anche il flag 'mostraEliminati' per la gestione della Soft Delete (Cestino).

            var prospetti = _prospettoRepository.GetFiltrati(idMandatario, idFileImportato, anno, mese, fatturata, mostraEliminati);

            // ==========================================================================================
            // 5. SERIALIZZAZIONE JSON E DINAMISMO DATA-DRIVEN
            // ==========================================================================================
            // Il metodo restituisce un oggetto anonimo con proprietà "data", standard richiesto da DataTables.
            // VANTAGGIO ARCHITETTURALE: Restituendo l'intera collezione di oggetti 'ProspettoSystemP',
            // non vi è accoppiamento forte (Hard Coding) tra le colonne del DB e il backend.
            // Qualsiasi colonna aggiuntiva (es. campi tecnici SystemP) sarà automaticamente disponibile 
            // nel payload JSON e mappabile tramite la configurazione lato Client.

            return Json(new { data = prospetti });
        }

    }
}
