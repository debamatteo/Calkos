using Calkos.web.Helpers;
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
using System.ComponentModel;

namespace Calkos.web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProspettiCobralController : Controller
    {
        /*
         * Questo controller gestisce TUTTO ciò che riguarda COBRAL:
         * - Importazione file Excel COBRAL
         * - Conversione righe Excel → ImportCobral
         * - Salvataggio in FileImportato + ProspettoCobral
         * - Visualizzazione lista ordini COBRAL
         * 
         * È la versione SPECIALIZZATA del vecchio ProspettiController,
         * ma dedicata SOLO al Mandatario COBRAL.
         */

        // Il servizio che contiene tutta la pipeline di importazione.
        // Il controller NON fa logica: delega tutto al service.
        private readonly ImportazioneCobralService _ImportazioneCobralService;

        /*
         * Il repository COBRAL.
         * 
         * Il controller NON deve conoscere come il repository funziona internamente.
         * Usa SOLO l’interfaccia IProspettoCobralRepository.
         * 
         * In Program.cs abbiamo:
         * services.AddScoped<IProspettoCobralRepository, ProspettoCobralRepository>();
         * 
         * Questo significa:
         * “Quando qualcuno chiede IProspettoCobralRepository, dagli ProspettoCobralRepository”.
         */
        private readonly IProspettoCobralRepository _prospettoRepository;
        private readonly IAgenteRepository _agenteRepository; // serve per lookup agenti
        private readonly IFileBackupService _fileBackupService; // serve per backup file COBRAL
        private readonly IClienteRepository _clienteRepository; // serve per lookup clienti
        private readonly IMaterialeRepository _materialeRepository; // serve per lookup materiale
        private readonly ITipoPagamentoRepository _tipoPagamentoRepository; // serve per lookup tipo pagamento
        private readonly IUnitaMisuraRepository _unitaMisuraRepository; // serve per lookup unità di misura


        public ProspettiCobralController(
            ImportazioneCobralService ImportazioneCobralService,
            IProspettoCobralRepository prospettoRepository,
            IAgenteRepository agenteRepository,
            IClienteRepository clienteRepository,
            IMaterialeRepository materialeRepository,
            ITipoPagamentoRepository tipoPagamentoRepository,
            IUnitaMisuraRepository unitaMisuraRepository,
            IFileBackupService fileBackupService)
        {
            _ImportazioneCobralService = ImportazioneCobralService;
            _prospettoRepository = prospettoRepository;   
            _agenteRepository = agenteRepository;
            _clienteRepository = clienteRepository;
            _materialeRepository = materialeRepository;
            _tipoPagamentoRepository = tipoPagamentoRepository;
            _unitaMisuraRepository = unitaMisuraRepository;
            _fileBackupService = fileBackupService;
        }



        // ============================================================
        // 1) LISTA ORDINI COBRAL
        // ============================================================
        /*
         * Questa action mostra:
         * - TUTTI gli ordini COBRAL
         * - OPPURE solo quelli relativi a un file importato (idFileImportato)
         * 
         * È la pagina che l’utente vede dopo l’importazione,
         * oppure quando clicca su “Lista Ordini”.
         */
        public IActionResult ListaOrdini(int? idFileImportato)
        {
            var prospetti = _prospettoRepository.GetAll();

            if (idFileImportato.HasValue)
            {
                // Filtriamo SOLO gli ordini del file importato
                prospetti = prospetti
                    .Where(p => p.IdFileImportato == idFileImportato.Value)
                    .ToList();
            }

            ViewBag.IdFileImportato = idFileImportato;

            return View(prospetti);
        }

        // ============================================================
        // 2) GET: PAGINA DI IMPORTAZIONE COBRAL
        // ============================================================

        [HttpGet]
        public IActionResult Importa(int idMandatario)
        {

            // Salvo IdMandatario in sessione per renderlo stabile
            // per tutto il flusso di importazione COBRAL
            HttpContext.Session.SetInt32("IdMandatario", idMandatario);

            // Mantengo anche il ViewBag per la view, se serve
            ViewBag.IdMandatario = idMandatario;

            return View();
        }

        // ============================================================
        // 3) POST: ESECUZIONE IMPORTAZIONE COBRAL
        // ============================================================
        /*
         * Questo metodo:
         * 1. Legge il file Excel
         * 2. Converte ogni riga in ImportCobral
         * 3. Crea un FileImportato
         * 4. Avvia la pipeline di importazione tramite ImportazioneCobralService
         * 5. Mostra una modale di conferma
         */
        [HttpPost]
        [HttpPost]
        public IActionResult Importa(IFormFile fileExcel)//, int idMandatario ora uso la sessione
        {
            // Recupero IdMandatario dalla sessione (stabile, non dipende dal browser)
            int idMandatario = HttpContext.Session.GetInt32("IdMandatario") ?? 0;//uso la sessione
            if (idMandatario == 0)
            {
                TempData["Errore"] = "Si è persa la Sessioel Mandatario .Ritorna alla Pagina Iniziale";
                return View();
            }

            if (fileExcel == null || fileExcel.Length == 0)
            {
                TempData["Errore"] = "Seleziona un file Excel valido.";
                return View();
            }

            // 0. Salvo il file in una cartella temporanea
            var tempFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "tempCobral");
            Directory.CreateDirectory(tempFolder);

            var tempFileName = $"COBRAL_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(fileExcel.FileName)}";
            var tempFilePath = Path.Combine(tempFolder, tempFileName);

            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                fileExcel.CopyTo(stream);
            }


            // 1. BACKUP FISICO DEL FILE COBRAL (con gestione errori elegante)
            try
            {
                _fileBackupService.BackupCobralAsync(tempFilePath).Wait();
            }
            catch (Exception ex)
            {
                // Non blocchiamo l'importazione COBRAL
                // Registriamo l'errore in TempData (o log)
                TempData["BackupWarning"] = "Backup non eseguito: " + ex.Message;

                // Se hai un logger, puoi usare:
                // _logger.LogError(ex, "Errore durante il backup del file COBRAL.");
            }


            // 2. Leggo l’Excel → List<RigaExcelCobral>
            var righe = ExcelHelper.LeggiExcel(fileExcel);

            // 3. Converto ogni riga in ImportCobral
            var righeImportCobral = righe.Select(r => Converti(r)).ToList();

            // 4. Creo FileImportato
            var file = new FileImportato
            {
                IdMandatario = idMandatario,//uso la sessione
                NomeFile = fileExcel.FileName,
                DataImportazione = DateTime.Now
            };

            // 5. Avvio importazione
            int idFile = _ImportazioneCobralService.ImportaFile(file, righeImportCobral);

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
            //return RedirectToAction("Importa", "ProspettiCobral", new { area = "Admin" });
            //Così la pagina Importa si ricarica con il Mandatario corretto
            return RedirectToAction("Importa", "ProspettiCobral",new { area = "Admin", idMandatario = file.IdMandatario });

        }


        // ============================================================
        // 4) CONVERSIONE RIGA EXCEL COBRAL → ImportCobral
        // ============================================================
        /*
         * Questo metodo converte una riga Excel COBRAL
         * nel modello ImportCobral usato dalla pipeline.
         * 
         * È specifico per COBRAL, quindi sta nel controller COBRAL.
         */
        private ImportCobral Converti(RigaExcelCobral r)
        {
            return new ImportCobral
            {
                OrdineDDT = r.OrdineDDT,
                Cliente = r.Cliente,
                Kg = r.Kg,
                Materiale = r.Materiale,
                Prezzo = r.Prezzo,
                Al = r.Al,
                Spessore = r.Spessore,
                Larghezza = r.Larghezza,
                Provvigione = r.Provvigione,
                AlluminioSpessore = r.AlluminioSpessore,
                OttoneSpessore = r.OttoneSpessore,
                RameSpessore = r.RameSpessore,
                AltrePercentuali = r.AltrePercentuali,
                PrLavSpess = r.PrLavSpess,
                AlluminioLarghezza = r.AlluminioLarghezza,
                OttoneLarghezza = r.OttoneLarghezza,
                RameLarghezza = r.RameLarghezza,
                Bronzo = r.Bronzo,
                PrLavLarg = r.PrLavLarg,
                ExtraPrezzoKg = r.ExtraPrezzoKg,
                ExtraPrezzoStagnato = r.ExtraPrezzoStagnato,
                PrLavTotale = r.PrLavTotale,
                Commissioni = r.Commissioni,
                PrezzoVendita = r.PrezzoVendita,
                Differenza = r.Differenza,
                DataConsegnaIpotetica = r.DataConsegnaIpotetica,
                Agente = r.Agente,
                Scadenza = r.Scadenza,
                FatturareStringa = r.Fatturare
            };
        }


        [HttpGet]
        [Authorize(Roles = "Admin,Operatore")]
        public IActionResult DettaglioOrdine(int id)
        {
            // Recupera l’ordine
            //✔ Se non esiste → 404
            //✔ Passa il modello alla view
            //✔ Funziona per Admin e Operatore
            var ordine = _prospettoRepository.GetById(id);

            if (ordine == null)
                return NotFound();

            // 2️⃣ Carico gli agenti dal DB
           
  
            var agenti = _agenteRepository.GetAll(); // dropdown agente
            //var clienti = _clienteRepository.GetAll(); //dropdown cliente
            var clienti = _clienteRepository.GetByMandatario(ordine.IdMandatario); //dropdown cliente
            var materiali = _materialeRepository.GetAll(); //dropdown materiali
            var tipiPagamento = _tipoPagamentoRepository.GetAll(); //dropdown tipo pagamento
            var unitaMisura = _unitaMisuraRepository.GetAll(); //dropdown unità di misura

            //----------------------------------------------
            // 3️⃣ Preparo la lista per la dropdown agenti
            var listaAgenti = agenti.Select(a => new SelectListItem
            {
                Value = a.IdAgente.ToString(),
                Text = a.AgenteDescrizione
            }).ToList();

            // Aggiungo la voce "Nessuno"
            listaAgenti.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "— —"
            });

            ViewBag.Agenti = listaAgenti;
            //----------------------------------------------

            //----------------------------------------------
            // 3️⃣ Preparo la lista per la dropdown clienti
            var listaClienti = clienti.Select(c => new SelectListItem
            {
                Value = c.IdCliente.ToString(),
                Text = c.RagioneSociale
            }).ToList();

            // Aggiungo la voce "Nessuno"
            listaClienti.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "— —"
            });

            ViewBag.Clienti = listaClienti;
            //----------------------------------------------

            //----------------------------------------------
            // 3️⃣ Preparo la lista per la dropdown materiali
            var listaMateriali = materiali.Select(c => new SelectListItem
            {
                Value = c.IdMateriale.ToString(),
                Text = c.DescrizioneMateriale
            }).ToList();

            // Aggiungo la voce "Nessuno"
            listaMateriali.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "— —"
            });

            ViewBag.Materiali = listaMateriali;
            //----------------------------------------------

            //----------------------------------------------
            // 3️⃣ Preparo la lista per la dropdown tipi pagamento
            var listaTipiPagamento = tipiPagamento.Select(t => new SelectListItem
            {
                Value = t.IdTipoPagamento.ToString(),
                Text = t.DescrizioneTipoPagamento
            }).ToList();

            // Aggiungo la voce "Nessuno"
            listaTipiPagamento.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "— —"
            });

            ViewBag.TipiPagamento = listaTipiPagamento;
            //----------------------------------------------

            //----------------------------------------------
            // 3️⃣ Preparo la lista per la dropdown unità di misura
            var listaUnitaMisura = unitaMisura.Select(u => new SelectListItem
            {
                Value = u.IdUnitaMisura.ToString(),
                Text = u.UnitaMisuraDescrizione
            }).ToList();

            // Aggiungo la voce "Nessuno"
            listaUnitaMisura.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "— —"
            });

            ViewBag.UnitaMisura = listaUnitaMisura;
            //----------------------------------------------

            return View(ordine);
        }


        // ============================================================
        // POST: SALVATAGGIO DETTAGLIO ORDINE COBRAL (SOLO ADMIN)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult DettaglioOrdine(ProspettoCobral model)
        {
            /*
             * Questa action salva le modifiche effettuate sul dettaglio ordine.
             * Solo gli ADMIN possono modificare i dati.
             * Gli OPERATORI possono solo visualizzare (action GET).
             */

            // 1) Validazione del modello
            if (!ModelState.IsValid)
            {
                // Se ci sono errori, ritorno la view con i dati inseriti
                return View(model);
            }

            // 2) Aggiornamento nel database tramite repository
            //    Il repository si occupa di chiamare la stored procedure di update.
            _prospettoRepository.Update(model);

            // 3) Messaggio di conferma da mostrare nella view
            TempData["Success"] = "Ordine aggiornato correttamente.";

            // 4) Redirect alla stessa pagina per mostrare i dati aggiornati
            return RedirectToAction("DettaglioOrdine", new { id = model.IdProspettoCobral });
        }



    }
}
