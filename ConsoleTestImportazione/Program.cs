// Microsoft.Extensions.Configuration permette al codice C# di "leggere" i file esterni
// (come appsettings.json) per recuperare impostazioni senza doverle scrivere a mano nel codice (hard-coded).

using CalkosManager.Application.Services;
using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Infrastructure.Helpers;        // ✔ ImportazioneSettings + ImportazioneHelper
using CalkosManager.Infrastructure.Repositories;
using ClosedXML.Excel;
using ConsoleTestImportazione;                    // ✔ ExcelReader
using Microsoft.Extensions.Configuration;



var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var uniqueKeyGenerator = new UniqueKeyGenerator(configuration);
// INIZIALIZZAZIONE CORRETTA
List<ImportCobral> righe = new();
List<ImportCobral> righeImport = new();

// 0. Carica configurazione
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory()) //// Dove cercare il file
    .AddJsonFile("appsettings.json", optional: false) //// Carica il file JSON
    .Build();

var settings = config.GetSection("Importazione").Get<ImportazioneSettings>(); // Mappa la sezione "Importazione" del file JSON alla classe ImportazioneSettings
// Crea l'helper per convertire i valori di Fatturare
var helper = new ImportazioneHelper(settings);
// 1. Crea i repository
var cn = "Server=localhost;Database=Calkos;Trusted_Connection=True;TrustServerCertificate=True;";

var fileRepo = new FileImportatoRepository(cn);
var importCobralRepo = new ImportCobralRepository(cn);
var prospettoRepo = new ProspettoCobralRepository(cn);

var clienteRepo = new ClienteRepository(cn);
var materialeRepo = new MaterialeRepository(cn);
var unitaRepo = new UnitaMisuraRepository(cn);

var trasformazioneService = new ProspettoCobralTransformationService(
    clienteRepo,
    materialeRepo,
    unitaRepo
);

// 2. Crea il servizio di importazione
var importService = new ImportazioneCobralService(
    cn,
    fileRepo,
    importCobralRepo,
    prospettoRepo,
    clienteRepo,
    materialeRepo,
    unitaRepo,
    trasformazioneService, uniqueKeyGenerator    // ← NUOVO PARAMETRO
);

// 3. Simula un file importato
var file = new FileImportato
{
    IdMandatario = 2, // COBRAL
    NomeFile = "TestCobral.xlsx"
};

// 4. Scegli modalità
Console.WriteLine("Seleziona modalità:");
Console.WriteLine("1 = Riga fissa");
Console.WriteLine("2 = Lettura Excel");
Console.Write("Scelta: ");
var scelta = Console.ReadLine();

// -----------------------------
// MODALITÀ 1: RIGA FISSA
// -----------------------------
if (scelta == "1")
{
    righe = new List<ImportCobral>
    {
        new ImportCobral
        {
            OrdineDDT = "12345/67890",
            Cliente = "COBRAL",
            Kg = 100.5m,
            Materiale = "OTTONE",
            Prezzo = 3.5m,
            Al = DateTime.Today,
            Spessore = "1.2m",
            Larghezza = "500m",
            Provvigione = 0.15m,
            AlluminioSpessore = 0.1m,
            OttoneSpessore = 0.2m,
            RameSpessore = 0.3m,
            AltrePercentuali = 0.05m,
            AlluminioLarghezza = 0.1m,
            OttoneLarghezza = 0.2m,
            RameLarghezza = 0.3m,
            BronzoLarghezza = 0.1m,
            ExtraPrezzoKg = 0.05m,
            ExtraPrezzoStagnato = 0.02m,
            DataConsegnaIpotetica = DateTime.Today.AddDays(10),
            Agente = "Mario Rossi",
            Scadenza = DateTime.Today.AddDays(30),
            Fatturare = true
        }
    };
}
else
{
    // -----------------------------
    // MODALITÀ 2: LETTURA EXCEL
    // -----------------------------
    Console.WriteLine("Modalità Excel selezionata.");

    var path = "C:\\Percorso\\TuoFile.xlsm";

    // Leggi Excel → RigaExcelCobral
    var righeExcel = ExcelReader.Read(path);

    // Normalizza valori ammessi .Serve per creare un elenco di valori validi che significano “Sì, questa riga va importata”.Legge i valori dal tuo appsettings.json
    var valoriAmmessi = new HashSet<string>(
        settings.ValoriFatturareTrue.Select(v => v.Trim()),
        StringComparer.OrdinalIgnoreCase
    );

    //// Filtra solo righe con Fatturare = Sì
    var righeDaImportare = righeExcel
        .Where(r => valoriAmmessi.Contains(r.Fatturare?.Trim() ?? ""))
        .ToList();

    // Mappa Excel → ImportCobral
    righeImport = righeDaImportare.Select(r => new ImportCobral
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
        AlluminioLarghezza = r.AlluminioLarghezza,
        OttoneLarghezza = r.OttoneLarghezza,
        RameLarghezza = r.RameLarghezza,
        BronzoLarghezza = r.BronzoLarghezza,
        ExtraPrezzoKg = r.ExtraPrezzoKg,
        ExtraPrezzoStagnato = r.ExtraPrezzoStagnato,
        DataConsegnaIpotetica = r.DataConsegnaIpotetica,
        Agente = r.Agente,
        Scadenza = r.Scadenza,

        // 🔥 CORREZIONE DEFINITIVA
        FatturareStringa = r.Fatturare, // stringa letta da Excel
        Fatturare = helper.ConvertiFatturare(r.Fatturare) // bool?
    }).ToList();

    righe = righeImport;
}

// 5. Esegui l’importazione
string utente = "ConsoleTest";
int idFile = importService.ImportaFile(file, righe,utente);

Console.WriteLine($"Importazione completata. IdFileImportato = {idFile}");
