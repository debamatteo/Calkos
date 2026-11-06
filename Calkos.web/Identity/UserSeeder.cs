//using Calkos.Web.Models;
//using Microsoft.AspNetCore.Identity;
////18/10/2026 aggiornato
//namespace Calkos.Web.Identity
//{
//    public static class UserSeeder
//    {
//        public static async Task SeedAdminAsync(
//            UserManager<ApplicationUser> userManager,
//            IConfiguration configuration,
//            ILogger logger)
//        {
//            // Esiste già almeno un Admin: non si fa nulla (niente admin "di ritorno").
//            var admins = await userManager.GetUsersInRoleAsync("Admin");
//            if (admins.Count > 0)
//                return;

//            var adminEmail = configuration["Seed:AdminEmail"];
//            var adminPassword = configuration["Seed:AdminPassword"];

//            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
//            {
//                logger.LogWarning("Nessun Admin presente e Seed:AdminEmail/Seed:AdminPassword non configurati: nessun amministratore creato.");
//                return;
//            }

//            if (await userManager.FindByEmailAsync(adminEmail) != null)
//            {
//                logger.LogWarning("L'utente {Email} esiste già senza ruolo Admin: non viene promosso automaticamente.", adminEmail);
//                return;
//            }

//            var user = new ApplicationUser
//            {
//                UserName = adminEmail,
//                Email = adminEmail,
//                EmailConfirmed = true,
//                RequirePasswordChange = true
//            };

//            var create = await userManager.CreateAsync(user, adminPassword);
//            if (!create.Succeeded)
//            {
//                logger.LogError("Creazione admin fallita: {Errori}", string.Join("; ", create.Errors.Select(e => e.Description)));
//                return;
//            }

//            var role = await userManager.AddToRoleAsync(user, "Admin");
//            if (!role.Succeeded)
//                logger.LogError("Assegnazione ruolo Admin fallita: {Errori}", string.Join("; ", role.Errors.Select(e => e.Description)));
//        }
//    }
//}
using Calkos.Web.Models;
using Microsoft.AspNetCore.Identity;
//19/10/2026 aggiornato
namespace Calkos.Web.Identity
{
    public static class UserSeeder
    {
        /// <summary>
        /// Crea il PRIMO amministratore solo se non esiste alcun utente con ruolo Admin.
        /// Le credenziali non sono nel codice: arrivano dalla configurazione
        /// (Seed:AdminEmail e Seed:AdminPassword), da usare solo al primo avvio.
        /// </summary>
        public static async Task SeedAdminAsync(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            ILogger logger)
        {
            // Se esiste già un Admin non si ricrea nulla: eliminare l'account storico è sicuro.
            var admins = await userManager.GetUsersInRoleAsync("Admin");
            if (admins.Count > 0)
                return;

            var email = configuration["Seed:AdminEmail"];
            var password = configuration["Seed:AdminPassword"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("Nessun utente Admin presente e Seed:AdminEmail/Seed:AdminPassword non configurati: creare il primo Admin.");
                return;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                RequirePasswordChange = true
            };

            var create = await userManager.CreateAsync(user, password);
            if (!create.Succeeded)
            {
                logger.LogError("Creazione Admin iniziale fallita: {Errori}",
                    string.Join(", ", create.Errors.Select(e => e.Code)));
                return;
            }

            var role = await userManager.AddToRoleAsync(user, "Admin");
            if (!role.Succeeded)
                logger.LogError("Assegnazione ruolo Admin fallita: {Errori}",
                    string.Join(", ", role.Errors.Select(e => e.Code)));
        }
    }
}