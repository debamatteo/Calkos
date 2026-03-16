using Calkos.Web.Identity;
using Calkos.Web.Models;
using CalkosManager.Application.Interfaces;
using CalkosManager.Application.Services;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Infrastructure.Helpers;
using CalkosManager.Infrastructure.Repositories;
using CalkosManager.Infrastructure.Services;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------
// CONFIGURAZIONE DATABASE (EF Core)
// -----------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("CalkosConnection")));
//
// -----------------------------
// CONFIGURAZIONE IDENTITY (utenti + ruoli)
// -----------------------------
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

// -----------------------------
// MVC (Controller + Views)
// -----------------------------
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();   // NECESSARIO per Identity e tutte le Razor Pages


// -----------------------------
// Aggiungi il servizio di Sessione per gestirle(ahhiungere app.UseSession(); dopo app.UseRouting())
// -----------------------------
builder.Services.AddSession();


// -----------------------------
// REPOSITORY
// (Domain → Infrastructure)
// Registrazione DI di tutti i repository. Ogni interfaccia deve essere collegata alla sua implementazione.
// AddScoped = una nuova istanza per ogni richiesta HTTP.
// -----------------------------
//registrare un servizio solo se:
//Una classe viene richiesta tramite costruttore  //(es. un controller o un altro servizio la richiede)
//E quella classe ha un costruttore con parametri  che il DI non può creare automaticamente.
var connectionString = builder.Configuration.GetConnectionString("CalkosConnection");
builder.Services.AddScoped<IProspettoCobralRepository>(provider =>  new ProspettoCobralRepository(connectionString));

builder.Services.AddScoped<IImportCobralRepository>(provider => new ImportCobralRepository(connectionString));

builder.Services.AddScoped<IFileImportatoRepository>(provider => new FileImportatoRepository(connectionString));

builder.Services.AddScoped<IClienteRepository>(provider => new ClienteRepository(connectionString));

builder.Services.AddScoped<IMaterialeRepository>(provider => new MaterialeRepository(connectionString));

builder.Services.AddScoped<IMandatarioRepository>(provider => new MandatarioRepository(connectionString));
builder.Services.AddScoped<IAgenteRepository>(provider =>    new AgenteRepository(connectionString));
builder.Services.AddScoped<IUnitaMisuraRepository>(provider => new UnitaMisuraRepository(connectionString));
builder.Services.AddScoped<ITipoPagamentoRepository>(provider => new TipoPagamentoRepository(connectionString));
// Registrazione del servizio applicativo che gestisce l'intera pipeline di importazione.
// Il controller richiede ImportazioneService nel costruttore, quindi deve essere registrato nel DI (Dependency Injection).
// AddScoped crea un'istanza per ogni richiesta HTTP.
builder.Services.AddScoped<ProspettoCobralTransformationService>();
builder.Services.AddScoped<FileImportatoService>();
builder.Services.AddScoped<MandatarioService>();
builder.Services.AddScoped<ProspettoCobralService>();

builder.Services.AddScoped<ImportazioneCobralService>(provider =>
    new ImportazioneCobralService(
        connectionString,
        provider.GetRequiredService<IFileImportatoRepository>(),
        provider.GetRequiredService<IImportCobralRepository>(),
        provider.GetRequiredService<IProspettoCobralRepository>(),
        provider.GetRequiredService<IClienteRepository>(),
        provider.GetRequiredService<IMaterialeRepository>(),
        provider.GetRequiredService<IUnitaMisuraRepository>(),
        provider.GetRequiredService<ProspettoCobralTransformationService>()
    ));
/*ogni volta che un servizio o controller chiede IFileBackupService
.NET crea una nuova istanza di FileBackupService e la passa automaticamente al costruttore*/
builder.Services.AddScoped<IFileBackupService, FileBackupService>();


//Serve per caricare una sezione del file appsettings.json dentro una classe di configurazione.
builder.Services.Configure<ImportazioneSettings>( builder.Configuration.GetSection("Importazione"));

//Registra ImportazioneHelper come servizio Singleton; viene creata una sola istanza di ImportazioneHelper; e viene riutilizzata per tutta la durata dell’applicazione
builder.Services.AddSingleton<ImportazioneHelper>();


var app = builder.Build();

// -----------------------------
// SEEDING RUOLI E ADMIN-//Creazione utente Admin iniziale
// Garantisce che i ruoli “Admin” e “Operatore” esistano SEMPRE.
// Anche se cancelli il database, al riavvio vengono ricreati.
// -----------------------------
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await IdentitySeeder.SeedRolesAsync(roleManager);

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    await UserSeeder.SeedAdminAsync(userManager);
}

// -----------------------------
// PIPELINE HTTP
// -----------------------------
//vedi appsettings.json=Se ShowDeveloperErrors = false → usa le pagine di errore personalizzate
var showDevErrors = builder.Configuration.GetValue<bool>("DebugOptions:ShowDeveloperErrors");


if (showDevErrors)
{
    // Mostra errori dettagliati SEMPRE, anche in produzione
    app.UseDeveloperExceptionPage();
}
else
{
    // Usa le pagine di errore personalizzate
    app.UseExceptionHandler("/Error/500");
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
    app.UseHsts();
}



app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

//Abilitare la SessioneSenza UseSession() la sessione non viene inizializzata per ogni richiesta.
app.UseSession();



// Autenticazione → Autorizzazione (ordine obbligatorio)
app.UseAuthentication();
app.UseAuthorization();

// -----------------------------
// ROUTING MVC
// -----------------------------
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
//Le Aree MVC richiedono una route dedicata 
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); //NECESSARIO per Identity e tutte le Razor Pages
                     //Mappa tutte le pagine Razor, incluse quelle in Areas/Identity
// -----------------------------
// AVVIO APPLICAZIONE
// -----------------------------
app.Run();
