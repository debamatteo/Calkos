using Calkos.web.Services;
using Calkos.web.Services.Export;
using Calkos.web.Services.Export.Grafici;
using Calkos.web.Services.Prospetti;
using Calkos.Web.Identity;
using Calkos.Web.Models;
using CalkosManager.Application.Interfaces;
using CalkosManager.Application.Services;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Infrastructure.Helpers;
using CalkosManager.Infrastructure.Repositories;
using CalkosManager.Infrastructure.Services;
using Microsoft.AspNetCore.Antiforgery;
// ==== MODIFICA 18/10/2026 - INIZIO ==== (A-01, A-03)
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Server.IIS;
// ==== MODIFICA 18/10/2026 - FINE ====




var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("CalkosConnection");

// ==== MODIFICA 18/10/2026 - INIZIO ==== (M-04)
// Cookie Secure: in produzione sempre HTTPS; in sviluppo segue la richiesta
// (il profilo "http" di launchSettings continua a funzionare).
var cookieSecurePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;
// ==== MODIFICA 18/10/2026 - FINE ====

// ============================================================
// 1. CONFIGURAZIONE SERVIZI DI SISTEMA (DB, IDENTITY, MVC)
// ============================================================

// --- DATABASE EF CORE ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 05/10/2026: configurazione test - Rilevamento automatico ambiente di TEST basato sul nome del DB (CalkosTest)
bool isTestDb = !string.IsNullOrEmpty(connectionString) &&
    connectionString.Contains("Calkos_TEST", StringComparison.OrdinalIgnoreCase);

builder.Services.AddSingleton(new Calkos.Web.Models.EnvironmentSettings { IsTest = isTestDb });



// --- IDENTITY (Gestione Utenti e Sicurezza) ---
// ==== MODIFICA 18/10/2026 - INIZIO ==== (M-02, A-03)
// Password più robusta (min 12 caratteri, maiuscole/minuscole/cifre), email univoca e lockout
// dopo 5 tentativi falliti per 15 minuti. La policy vale solo per le password create da ora in poi.
// Prima del rilascio verificare sul DB:
//   SELECT NormalizedEmail, COUNT(*) FROM AspNetUsers GROUP BY NormalizedEmail HAVING COUNT(*) > 1;  -- deve dare 0 righe
//   SELECT COUNT(*) FROM AspNetUsers WHERE LockoutEnabled = 0;  -- deve dare 0; altrimenti: UPDATE AspNetUsers SET LockoutEnabled = 1;
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 12;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredUniqueChars = 4;

    options.User.RequireUniqueEmail = true;

    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
// ==== MODIFICA 18/10/2026 - FINE ====
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 07/08/2026: sostituisce la factory di default per includere il claim IdAgente
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();

// ==== MODIFICA 18/10/2026 - INIZIO ==== (A-01, A-03, M-09, M-04)
// M-09: un utente bloccato o con password resettata perde la sessione entro 1 minuto (default 30)
builder.Services.Configure<SecurityStampValidatorOptions>(o =>
    o.ValidationInterval = TimeSpan.FromMinutes(1));

// ==== MODIFICA 20/10/2026 - INIZIO ==== N1.3 
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 20 * 1024 * 1024);
builder.Services.Configure<IISServerOptions>(o => o.MaxRequestBodySize = 20 * 1024 * 1024);





// A-01: tutto ciò che non ha [AllowAnonymous] richiede login
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ==== MODIFICA 19/10/2026 - INIZIO ==== (P3 - HSTS)
// HSTS valido 365 giorni (usato da app.UseHsts() nella pipeline, solo fuori da Development)
builder.Services.AddHsts(o => o.MaxAge = TimeSpan.FromDays(365));
// ==== MODIFICA 19/10/2026 - FINE ====

// A-03: massimo 10 richieste al minuto per IP sulla pagina di login
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "sconosciuto",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

//// M-04: redirect HTTPS su IIS. AMBIENTE DI PRODUZIONE  Se il sito pubblico non è sulla 443, cambia il valore.
//if (!builder.Environment.IsDevelopment())
//    builder.Services.AddHttpsRedirection(o => o.HttpsPort = 443);




//// M-04: redirect HTTPS su IIS.AMBIENTE DI TEST
//if (!builder.Environment.IsDevelopment())
//    builder.Services.AddHttpsRedirection(o => o.HttpsPort = 8443);


// ==== MODIFICA 22/10/2026 - INIZIO ==== (porta HTTPS letta da appsettings.json)
if (!builder.Environment.IsDevelopment())
    builder.Services.AddHttpsRedirection(o =>
        o.HttpsPort = builder.Configuration.GetValue<int?>("HttpsPort") ?? 443);
// ==== MODIFICA 20/10/2026 - FINE ====




// Nota: se un reverse proxy o load balancer sta davanti a IIS, RemoteIpAddress sarà quello del proxy
// e serve UseForwardedHeaders.
// ==== MODIFICA 18/10/2026 - FINE ====

// ==== MODIFICA 15/10/2026 - INIZIO ====
// Le chiavi che cifrano il cookie di login vengono salvate su disco, fuori dalla cartella
// di publish, così sopravvivono a riavvii del pool e a nuovi deploy.
// Cartella e nome applicazione separati per ambiente (isTestDb è definito sopra),
// per evitare che Test e Produzione condividano le stesse chiavi.
// Percorso sovrascrivibile da appsettings: "DataProtection:KeysPath".
var keysPath = builder.Configuration["DataProtection:KeysPath"]
    ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "CalkosManager", "DataProtection-Keys", isTestDb ? "Test" : "Prod");

builder.Services.AddDataProtection()
    .SetApplicationName(isTestDb ? "CalkosManager-Test" : "CalkosManager")
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    // Cifra le chiavi su disco con DPAPI (cifratura di Windows legata alla macchina).
    // Scope macchina: non dipende dall'utente del pool, che è diverso tra Test e Produzione.
    .ProtectKeysWithDpapi(protectToLocalMachine: true);
// ==== MODIFICA 15/10/2026 - FINE ====


//// --- MVC & SERIALIZZAZIONE JSON ---

////builder.Services.AddControllersWithViews()
////    .AddJsonOptions(options =>
////    {
////        options.JsonSerializerOptions.PropertyNamingPolicy = null;
////    });


// --- MVC & SERIALIZZAZIONE JSON ---
// Modificato solo per includere il filtro Antiforgery globale
builder.Services.AddControllersWithViews(options =>
{
    //Antiforgery CENTRALIZZATO 05/ 04/2026
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
})
    .AddJsonOptions(options =>
    {
        // Configurazione critica per DataTables: impostiamo PropertyNamingPolicy = null 
        // per mantenere il PascalCase (es. "IdProspetto") ed evitare la conversione automatica in camelCase.

        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// ==== MODIFICA 25/08/2026 - INIZIO ====
// Necessario perché le chiamate AJAX (fetch/JSON) inviano il token anti-forgery
// via header, non via campo form. Senza questa riga, HeaderName resta null e il
// filtro globale sopra rifiuta con 400 qualunque POST in application/json,
// incluse quelle già presenti in Index.cshtml (reset password) e quelle nuove
// per Blocca/Sblocca utente.
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    // ==== MODIFICA 18/10/2026 - INIZIO ==== (M-04)
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    // ==== MODIFICA 18/10/2026 - FINE ====
});
// ==== MODIFICA 25/08/2026 - FINE ====

//builder.Services.AddRazorPages();

//18/10/2026 rende pubbliche in modo sicuro le pagine di autenticazione (Login, Logout, AccessDenied) sia che si trovino nell'area Identity sia nella cartella standard:
builder.Services.AddRazorPages(options =>
{
    // Pagine che devono restare pubbliche con la regola "login obbligatorio" attiva.
    // Elencate sia con l'area Identity sia senza, così funziona in entrambe le cartelle.
    foreach (var pagina in new[] { "/Account/Login", "/Account/Logout", "/Account/AccessDenied" })
    {
        options.Conventions.AllowAnonymousToAreaPage("Identity", pagina);
        options.Conventions.AllowAnonymousToPage(pagina);
    }
});


// --- GESTIONE SESSIONE ---
builder.Services.AddSession(options =>
{
    // ==== MODIFICA 15/10/2026 - INIZIO ====
    // Allineato alla durata del cookie di login (8 ore): prima 60 min, dopo i quali
    // l'utente restava autenticato ma perdeva IdMandatario.
    options.IdleTimeout = TimeSpan.FromHours(8);
    // ==== MODIFICA 15/10/2026 - FINE ====
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // ==== MODIFICA 18/10/2026 - INIZIO ==== (M-04)
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    // ==== MODIFICA 18/10/2026 - FINE ====
    // ==== MODIFICA 19/10/2026 - INIZIO ==== (P5)
    options.Cookie.SameSite = SameSiteMode.Lax;
    // ==== MODIFICA 19/10/2026 - FINE ====
});

// ============================================================
// 2. DEPENDENCY INJECTION - REPOSITORY (DAL)
// ============================================================
// Registrazione dei repository con ciclo di vita Scoped (una istanza per richiesta HTTP).
// Utilizziamo le interfacce per garantire il disaccoppiamento tra Domain e Infrastructure.


builder.Services.AddScoped<IFileImportatoRepository>(p => new FileImportatoRepository(connectionString));
builder.Services.AddScoped<IClienteRepository>(p => new ClienteRepository(connectionString));
builder.Services.AddScoped<IMaterialeRepository>(p => new MaterialeRepository(connectionString));
builder.Services.AddScoped<IMandatarioRepository>(p => new MandatarioRepository(connectionString));
builder.Services.AddScoped<IAgenteRepository>(p => new AgenteRepository(connectionString));
builder.Services.AddScoped<IUnitaMisuraRepository>(p => new UnitaMisuraRepository(connectionString));
builder.Services.AddScoped<ITipoPagamentoRepository>(p => new TipoPagamentoRepository(connectionString));
builder.Services.AddScoped<IProvvigioniAgentiRepository>(p => new ProvvigioniAgentiRepository(connectionString));
builder.Services.AddScoped<IMaterialeRepository>(p => new MaterialeRepository(connectionString));
builder.Services.AddScoped<IMaterialiDimensioniRepository>(p => new MaterialiDimensioniRepository(connectionString));

builder.Services.AddScoped<ICalcoloBaseMaterialeRepository>(p => new CalcoloBaseMaterialeRepository(connectionString));//29/08/2026: aggiunto per la gestione del CalcoloBaseMateriale
builder.Services.AddScoped<CalcoloBaseMaterialeService>();//29/08/2026: aggiunto per la gestione del CalcoloBaseMateriale

// --- REPOSITORY COBRAL ---



builder.Services.AddScoped<IProspettoCobralRepository>(p => new ProspettoCobralRepository(connectionString));
builder.Services.AddScoped<IImportCobralRepository>(p => new ImportCobralRepository(connectionString));
// --- REPOSITORY DE ANGELI ---
builder.Services.AddScoped<IImportDeAngeliRepository>(p => new ImportDeAngeliRepository(connectionString));
builder.Services.AddScoped<IProspettoDeAngeliRepository>(p => new ProspettoDeAngeliRepository(connectionString));
// --- REPOSITORY ELEKTRAWIRE ---
builder.Services.AddScoped<IImportElektraWireRepository>(p => new ImportElektraWireRepository(connectionString));
builder.Services.AddScoped<IProspettoElektraWireRepository>(p => new ProspettoElektraWireRepository(connectionString));


// --- REPOSITORY CST ---
builder.Services.AddScoped<IImportCSTRepository>(p => new ImportCSTRepository(connectionString));
builder.Services.AddScoped<IProspettoCSTRepository>(p => new ProspettoCSTRepository(connectionString));

// --- REPOSITORY SYSTEMCORE ---
builder.Services.AddScoped<IImportSystemCoreRepository>(p => new ImportSystemCoreRepository(connectionString));
builder.Services.AddScoped<IProspettoSystemCoreRepository>(p => new ProspettoSystemCoreRepository(connectionString));

// --- REPOSITORY SYSTEMP ---
builder.Services.AddScoped<IImportSystemPRepository>(p => new ImportSystemPRepository(connectionString));
builder.Services.AddScoped<IProspettoSystemPRepository>(p => new ProspettoSystemPRepository(connectionString));

// --- REPOSITORY GUERZONI ---
builder.Services.AddScoped<IImportGuerzoniRepository>(p => new ImportGuerzoniRepository(connectionString));
builder.Services.AddScoped<IProspettoGuerzoniRepository>(p => new ProspettoGuerzoniRepository(connectionString));

// --- REPOSITORY TRADING AND CONSULTING ---
builder.Services.AddScoped<IImportTradingAndConsultingRepository>(p => new ImportTradingAndConsultingRepository(connectionString));
builder.Services.AddScoped<IProspettoTradingAndConsultingRepository>(p => new ProspettoTradingAndConsultingRepository(connectionString));

// --- REPOSITORY HITECH ---
builder.Services.AddScoped<IImportHitechRepository>(p => new ImportHitechRepository(connectionString));
builder.Services.AddScoped<IProspettoHitechRepository>(p => new ProspettoHitechRepository(connectionString));

// --- REPOSITORY STATISTICHE GLOBALI ---
builder.Services.AddScoped<IStatisticheGlobaliRepository>(p => new StatisticheGlobaliRepository(connectionString));

//05/10/2026: aggiunto per la gestione del Report Merce Consegnata DDT
builder.Services.AddScoped<IReportMerceConsegnataRepository>(p => new ReportMerceConsegnataRepository(connectionString));




// ============================================================
// 3. DEPENDENCY INJECTION - BUSINESS SERVICES (BLL)
// ============================================================

// Servizi di Logica Applicativa

// --- PROSPETTO COBRAL ---
builder.Services.AddScoped<ProspettoCobralTransformationService>();
builder.Services.AddScoped<ProspettoCobralService>();

// --- PROSPETTO DE ANGELI ---
builder.Services.AddScoped<ProspettoDeAngeliTransformationService>();
builder.Services.AddScoped<ProspettoDeAngeliService>();

// --- PROSPETTO ELEKTRAWIRE ---
builder.Services.AddScoped<ProspettoElektraWireTransformationService>();
builder.Services.AddScoped<ProspettoElektraWireService>();

// --- PROSPETTO CST ---
builder.Services.AddScoped<ProspettoCSTTransformationService>();
builder.Services.AddScoped<ProspettoCSTService>();

// --- PROSPETTO SYSTEMCORE ---
builder.Services.AddScoped<ProspettoSystemCoreTransformationService>();
builder.Services.AddScoped<ProspettoSystemCoreService>();

// --- PROSPETTO SYSTEMP ---
builder.Services.AddScoped<ProspettoSystemPTransformationService>();
builder.Services.AddScoped<ProspettoSystemPService>();

// --- PROSPETTO GUERZONI ---
builder.Services.AddScoped<ProspettoGuerzoniTransformationService>();
builder.Services.AddScoped<ProspettoGuerzoniService>();

// --- PROSPETTO TRADING AND CONSULTING ---
builder.Services.AddScoped<ProspettoTradingAndConsultingTransformationService>();
builder.Services.AddScoped<ProspettoTradingAndConsultingService>();

// --- PROSPETTO HITECH ---
builder.Services.AddScoped<ProspettoHitechTransformationService>();
builder.Services.AddScoped<ProspettoHitechService>();


builder.Services.AddScoped<FileImportatoService>();
builder.Services.AddScoped<MandatarioService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<TipoPagamentoService>();
builder.Services.AddScoped<ProvvigioniAgentiService>();
builder.Services.AddScoped<AgenteService>();
builder.Services.AddScoped<IFileBackupService, FileBackupService>();
builder.Services.AddScoped<MaterialeService>();
builder.Services.AddScoped<MaterialiDimensioniService>();
builder.Services.AddScoped<UnitaMisuraService>();
// Servizi di Esportazione e Dashboard
builder.Services.AddScoped<ExcelExportService>();
builder.Services.AddScoped<PdfExportService>();
builder.Services.AddScoped<DashboardProvvigioniService>(p => new DashboardProvvigioniService(connectionString));

//builder.Services.AddScoped<ReportMerceConsegnataService>();//05/10/2026: aggiunto per la gestione del Report Merce Consegnata DDT
builder.Services.AddScoped<ReportMerceConsegnataExcelBuilder>();//05/10/2026: aggiunto per la gestione del Report Merce Consegnata DDT

// ---- STATISTICHE GLOBALI (23/09/2026) ----
builder.Services.AddScoped<StatisticheGlobaliService>();
// (AddTransient): Crea un'istanza nuova ogni volta che viene richiesta per la generazione ed esportazione del file Excel con grafici.
// 23/09/2026: Transient perché ogni richiesta di Excel deve avere un nuovo builder
// con stato pulito

builder.Services.AddTransient<StatisticheGlobaliExcelBuilder>();

// Helper e Utility (Singleton: istanza unica per l'intera vita dell'app)
builder.Services.AddSingleton<UniqueKeyGenerator>();
builder.Services.AddSingleton<ImportazioneHelper>();
builder.Services.AddSingleton<ProspettoConfigService>();

// Configurazione impostazioni da appsettings.json
builder.Services.Configure<ImportazioneSettings>(builder.Configuration.GetSection("Importazione"));




// Servizio Complesso: Orchestratore Importazione Cobral
builder.Services.AddScoped<ImportazioneCobralService>(provider =>
    new ImportazioneCobralService(
        connectionString,
        provider.GetRequiredService<IFileImportatoRepository>(),
        provider.GetRequiredService<IImportCobralRepository>(),
        provider.GetRequiredService<IProspettoCobralRepository>(),
        provider.GetRequiredService<IClienteRepository>(),
        provider.GetRequiredService<IMaterialeRepository>(),
        provider.GetRequiredService<IUnitaMisuraRepository>(),
        provider.GetRequiredService<ProspettoCobralTransformationService>(),
        provider.GetRequiredService<UniqueKeyGenerator>()
    ));
// Servizio Complesso: Orchestratore Importazione De Angeli
builder.Services.AddScoped<ImportazioneDeAngeliService>(provider =>
    new ImportazioneDeAngeliService(
        connectionString,
        provider.GetRequiredService<IFileImportatoRepository>(),
        provider.GetRequiredService<IImportDeAngeliRepository>(),
        provider.GetRequiredService<IProspettoDeAngeliRepository>(),
        provider.GetRequiredService<IClienteRepository>(),
        provider.GetRequiredService<IMaterialeRepository>(),
        provider.GetRequiredService<IUnitaMisuraRepository>(),
        provider.GetRequiredService<ProspettoDeAngeliTransformationService>(),
        provider.GetRequiredService<UniqueKeyGenerator>()
    ));

// Servizio Complesso: Orchestratore Importazione ElektraWire
builder.Services.AddScoped<ImportazioneElektraWireService>(provider =>
    new ImportazioneElektraWireService(
        connectionString,
        provider.GetRequiredService<IFileImportatoRepository>(),
        provider.GetRequiredService<IImportElektraWireRepository>(),
        provider.GetRequiredService<IProspettoElektraWireRepository>(),
        provider.GetRequiredService<IClienteRepository>(),
        provider.GetRequiredService<IMaterialeRepository>(),
        provider.GetRequiredService<IUnitaMisuraRepository>(),
        provider.GetRequiredService<ProspettoElektraWireTransformationService>(),
        provider.GetRequiredService<UniqueKeyGenerator>()
    ));


// Servizio Complesso: Orchestratore Importazione CST
builder.Services.AddScoped<ImportazioneCSTService>(provider =>
    new ImportazioneCSTService(
        connectionString,
        provider.GetRequiredService<IFileImportatoRepository>(),
        provider.GetRequiredService<IImportCSTRepository>(),
        provider.GetRequiredService<IProspettoCSTRepository>(),
        provider.GetRequiredService<IClienteRepository>(),
        provider.GetRequiredService<IMaterialeRepository>(),
        provider.GetRequiredService<IUnitaMisuraRepository>(),
        provider.GetRequiredService<ProspettoCSTTransformationService>(),
        provider.GetRequiredService<UniqueKeyGenerator>()
    ));

// Servizio Complesso: Orchestratore Importazione SystemCore
builder.Services.AddScoped<ImportazioneSystemCoreService>(provider =>
    new ImportazioneSystemCoreService(
        connectionString,
        provider.GetRequiredService<IFileImportatoRepository>(),
        provider.GetRequiredService<IImportSystemCoreRepository>(),
        provider.GetRequiredService<IProspettoSystemCoreRepository>(),
        provider.GetRequiredService<IClienteRepository>(),
        provider.GetRequiredService<IMaterialeRepository>(),
        provider.GetRequiredService<IUnitaMisuraRepository>(),
        provider.GetRequiredService<ProspettoSystemCoreTransformationService>(),
        provider.GetRequiredService<UniqueKeyGenerator>()
    ));

// Servizio Complesso: Orchestratore Importazione SystemP
builder.Services.AddScoped<ImportazioneSystemPService>(provider =>
    new ImportazioneSystemPService(
        connectionString,
        provider.GetRequiredService<IFileImportatoRepository>(),
        provider.GetRequiredService<IImportSystemPRepository>(),
        provider.GetRequiredService<IProspettoSystemPRepository>(),
        provider.GetRequiredService<IClienteRepository>(),
        provider.GetRequiredService<IMaterialeRepository>(),
        provider.GetRequiredService<IUnitaMisuraRepository>(),
        provider.GetRequiredService<ProspettoSystemPTransformationService>(),
        provider.GetRequiredService<UniqueKeyGenerator>()
    ));


// Servizio Complesso: Orchestratore Importazione Guerzoni
builder.Services.AddScoped<ImportazioneGuerzoniService>(provider =>
    new ImportazioneGuerzoniService(
        connectionString,
        provider.GetRequiredService<IFileImportatoRepository>(),
        provider.GetRequiredService<IImportGuerzoniRepository>(),
        provider.GetRequiredService<IProspettoGuerzoniRepository>(),
        provider.GetRequiredService<IClienteRepository>(),
        provider.GetRequiredService<IMaterialeRepository>(),
        provider.GetRequiredService<IUnitaMisuraRepository>(),
        provider.GetRequiredService<ProspettoGuerzoniTransformationService>(),
        provider.GetRequiredService<UniqueKeyGenerator>()
    ));


// Servizio Complesso: Orchestratore Importazione Trading and Consulting

builder.Services.AddScoped<ImportazioneTradingAndConsultingService>(provider =>
    new ImportazioneTradingAndConsultingService(
        connectionString,
        provider.GetRequiredService<IFileImportatoRepository>(),
        provider.GetRequiredService<IImportTradingAndConsultingRepository>(),
        provider.GetRequiredService<IProspettoTradingAndConsultingRepository>(),
        provider.GetRequiredService<IClienteRepository>(),
        provider.GetRequiredService<IMaterialeRepository>(),
        provider.GetRequiredService<IUnitaMisuraRepository>(),
        provider.GetRequiredService<ProspettoTradingAndConsultingTransformationService>(),
        provider.GetRequiredService<UniqueKeyGenerator>()
    ));

// Servizio Complesso: Orchestratore Importazione Hitech

builder.Services.AddScoped<ImportazioneHitechService>(provider =>
    new ImportazioneHitechService(
        connectionString,
        provider.GetRequiredService<IFileImportatoRepository>(),
        provider.GetRequiredService<IImportHitechRepository>(),
        provider.GetRequiredService<IProspettoHitechRepository>(),
        provider.GetRequiredService<IClienteRepository>(),
        provider.GetRequiredService<IMaterialeRepository>(),
        provider.GetRequiredService<IUnitaMisuraRepository>(),
        provider.GetRequiredService<ProspettoHitechTransformationService>(),
        provider.GetRequiredService<UniqueKeyGenerator>()
    ));


// ==== MODIFICA 15/10/2026 - INIZIO ====
// Riconosce le richieste fatte da script (jQuery o fetch) rispetto alle navigazioni del browser.
static bool IsRichiestaAjax(HttpRequest request) =>
    request.Headers["X-Requested-With"] == "XMLHttpRequest"             // jQuery: $.ajax, $.getJSON, DataTables
    || (request.Headers.TryGetValue("Sec-Fetch-Mode", out var fetchMode) // fetch(): inviato dal browser
        && !string.Equals(fetchMode, "navigate", StringComparison.OrdinalIgnoreCase));
// ==== MODIFICA 15/10/2026 - FINE ====



builder.Services.ConfigureApplicationCookie(options =>
{
    // ==== MODIFICA 19/10/2026 - INIZIO ==== (P6)
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    // (Cookie.SecurePolicy è già impostato più sotto, modifica 18/10/2026 M-04)
    // ==== MODIFICA 19/10/2026 - FINE ====

    options.AccessDeniedPath = "/Identity/Account/AccessDenied"; // CORRETTO 07/08/2026: serve il prefisso "/Identity"

    // ==== MODIFICA 15/10/2026 - INIZIO ====
    options.LoginPath = "/Identity/Account/Login";   // prima: default /Account/Login → 404
    options.LogoutPath = "/Identity/Account/Logout";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);  // durata da te non indicata: ho scelto una giornata lavorativa
    options.SlidingExpiration = true;                // la scadenza si rinnova con l'uso
    // ==== MODIFICA 18/10/2026 - INIZIO ==== (M-04)
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    // ==== MODIFICA 18/10/2026 - FINE ====

    // Cookie scaduto: le chiamate da script ricevono 401 (leggibile dal JS) invece di un 302
    // verso la pagina di login; le navigazioni normali mantengono il redirect.
    options.Events.OnRedirectToLogin = context =>
    {
        if (IsRichiestaAjax(context.Request))
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        else
            context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };

    // Utente senza permesso: 403 per le chiamate da script, redirect per le navigazioni.
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (IsRichiestaAjax(context.Request))
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
        else
            context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    // ==== MODIFICA 15/10/2026 - FINE ====
});

// Configurazione Libreria PDF
QuestPDF.Settings.License = LicenseType.Community;

// ============================================================
// 4. COSTRUZIONE E PIPELINE MIDDLEWARE (HTTP REQUEST)
// ============================================================
var app = builder.Build();

// ==== MODIFICA 18/10/2026 - INIZIO ==== (M-04)
// Header di sicurezza
app.Use(async (context, next) =>
{
    var h = context.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["Referrer-Policy"] = "strict-origin-when-cross-origin";
    // ==== MODIFICA 19/10/2026 - INIZIO ==== (P7)
    h["X-Frame-Options"] = "DENY";
    // ==== MODIFICA 19/10/2026 - FINE ====
    h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    // CSP in sola segnalazione: guarda la console del browser e correggi prima di renderla bloccante
    // ==== MODIFICA 19/10/2026 - INIZIO ==== (P7 - CDN usati dal sito, connect-src, frame-ancestors 'none')
    h["Content-Security-Policy-Report-Only"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://code.jquery.com https://cdn.datatables.net https://cdnjs.cloudflare.com; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdn.datatables.net; " +
        "font-src 'self' https://cdn.jsdelivr.net data:; " +
        "img-src 'self' data:; connect-src 'self'; object-src 'none'; " +
        "frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    // ==== MODIFICA 19/10/2026 - FINE ====
    await next();
});
// ==== MODIFICA 18/10/2026 - FINE ====

// --- LOCALIZZAZIONE ---
// Forza il server a usare il punto (.) come separatore decimale per evitare conflitti numerici SQL/UI.
var defaultCulture = new CultureInfo("en-US");
defaultCulture.NumberFormat.NumberDecimalSeparator = ".";
defaultCulture.NumberFormat.NumberGroupSeparator = "";
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = new List<CultureInfo> { defaultCulture },
    SupportedUICultures = new List<CultureInfo> { defaultCulture }
});

// --- GESTIONE ERRORI ---
// ==== MODIFICA 19/10/2026 - INIZIO ==== (P7)
// La pagina errori da sviluppatore è consentita SOLO in ambiente Development
var showDevErrors = builder.Configuration.GetValue<bool>("DebugOptions:ShowDeveloperErrors");
if (showDevErrors && app.Environment.IsDevelopment())
// ==== MODIFICA 19/10/2026 - FINE ====
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error/500");
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
    app.UseHsts();
}

app.UseHttpsRedirection();

// ==== MODIFICA 19/10/2026 - INIZIO ==== (P7 - Fase 1, C4)
// --- BLOCCO TEMPORANEO CARTELLE DI IMPORT SOTTO wwwroot ---
// Da rimuovere quando i file saranno salvati fuori dalla web root (Pacchetto 3).
// Se dopo la patch un link "temp…" in una vista smette di funzionare, va sostituito con un download passato da un controller.
var cartelleBloccate = new[]
{
    "/tempCobra,/tempCST", "/tempDeAngeli", "/tempElektraWire", "/tempGuerzoni",
    "/tempHitech", "/tempSystemCore", "/tempSystemP", "/tempTradingAndConsulting"
};
app.Use(async (context, next) =>
{
    if (cartelleBloccate.Any(c => context.Request.Path.StartsWithSegments(c, StringComparison.OrdinalIgnoreCase)))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next();
});
// ==== MODIFICA 19/10/2026 - FINE ====

app.UseStaticFiles();
app.UseRouting();

// ==== MODIFICA 18/10/2026 - INIZIO ==== (A-03)
app.UseRateLimiter();
// ==== MODIFICA 18/10/2026 - FINE ====

// --- SESSIONE & SICUREZZA ---
app.UseSession(); // Inizializza il supporto alle sessioni (obbligatorio per i filtri di Calkos)
app.UseAuthentication();

// ==== MODIFICA 18/10/2026 - INIZIO ==== (A-06)
// Chi ha una password temporanea può usare solo ForceChangePassword e Logout.
// L'URL di redirect usa lo stesso prefisso di LoginPath: se la pagina risponde su un altro indirizzo, cambia solo quella stringa.
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true
        // ==== MODIFICA 19/10/2026 - INIZIO ==== (P7 - accetta il claim sia con valore "true" sia "1")
        && (context.User.HasClaim("RequirePasswordChange", "true") || context.User.HasClaim("RequirePasswordChange", "1")))
    // ==== MODIFICA 19/10/2026 - FINE ====
    {
        var path = context.Request.Path.Value ?? "";
        var consentito =
            path.EndsWith("/Account/ForceChangePassword", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith("/Account/Logout", StringComparison.OrdinalIgnoreCase) ||
            // ==== MODIFICA 19/10/2026 - INIZIO ==== (P7 - AccessDenied e pagine errore sempre raggiungibili)
            path.EndsWith("/Account/AccessDenied", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/Error", StringComparison.OrdinalIgnoreCase);
        // ==== MODIFICA 19/10/2026 - FINE ====

        if (!consentito)
        {
            if (IsRichiestaAjax(context.Request))
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
            else
                context.Response.Redirect("/Identity/Account/ForceChangePassword");
            return;
        }
    }
    await next();
});
// ==== MODIFICA 18/10/2026 - FINE ====

app.UseAuthorization();

// ============================================================
// 5. SEEDING DATI (Ruoli e Amministratore)
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await IdentitySeeder.SeedRolesAsync(roleManager);

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    // ==== MODIFICA 18/10/2026 - INIZIO ==== (seeding admin con configurazione e logger)
    var seedLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("UserSeeder");
    await UserSeeder.SeedAdminAsync(userManager, app.Configuration, seedLogger);
    // ==== MODIFICA 18/10/2026 - FINE ====
}

// ============================================================
// 6. ROUTING E AVVIO
// ============================================================
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();