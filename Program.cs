using Calkos.web.Services;
using Calkos.web.Services.Export;
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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("CalkosConnection");

// ============================================================
// 1. CONFIGURAZIONE SERVIZI DI SISTEMA (DB, IDENTITY, MVC)
// ============================================================

// --- DATABASE EF CORE ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// --- IDENTITY (Gestione Utenti e Sicurezza) ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

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




builder.Services.AddRazorPages();

// --- GESTIONE SESSIONE ---
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ============================================================
// 2. DEPENDENCY INJECTION - REPOSITORY (DAL)
// ============================================================
// Registrazione dei repository con ciclo di vita Scoped (una istanza per richiesta HTTP).
// Utilizziamo le interfacce per garantire il disaccoppiamento tra Domain e Infrastructure.

builder.Services.AddScoped<IProspettoCobralRepository>(p => new ProspettoCobralRepository(connectionString));
builder.Services.AddScoped<IImportCobralRepository>(p => new ImportCobralRepository(connectionString));
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

// ============================================================
// 3. DEPENDENCY INJECTION - BUSINESS SERVICES (BLL)
// ============================================================

// Servizi di Logica Applicativa
builder.Services.AddScoped<ProspettoCobralTransformationService>();
builder.Services.AddScoped<FileImportatoService>();
builder.Services.AddScoped<MandatarioService>();
builder.Services.AddScoped<ProspettoCobralService>();
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

// Configurazione Libreria PDF
QuestPDF.Settings.License = LicenseType.Community;

// ============================================================
// 4. COSTRUZIONE E PIPELINE MIDDLEWARE (HTTP REQUEST)
// ============================================================
var app = builder.Build();

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
var showDevErrors = builder.Configuration.GetValue<bool>("DebugOptions:ShowDeveloperErrors");
if (showDevErrors)
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
app.UseStaticFiles();
app.UseRouting();

// --- SESSIONE & SICUREZZA ---
app.UseSession(); // Inizializza il supporto alle sessioni (obbligatorio per i filtri di Calkos)
app.UseAuthentication();
app.UseAuthorization();

// ============================================================
// 5. SEEDING DATI (Ruoli e Amministratore)
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await IdentitySeeder.SeedRolesAsync(roleManager);

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    await UserSeeder.SeedAdminAsync(userManager);
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