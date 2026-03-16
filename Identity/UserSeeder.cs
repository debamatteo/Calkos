using Microsoft.AspNetCore.Identity;
using Calkos.Web.Models;

namespace Calkos.Web.Identity
{
    public static class UserSeeder
    {
        // Metodo eseguito all'avvio dell'applicazione per garantire
        // che esista almeno un utente amministratore.
        // Questo evita di rimanere senza accesso al sistema.
        public static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager)
        {
            // Credenziali dell'utente admin iniziale.
            // Puoi cambiarle o leggerle da configurazione.
            string adminEmail = "admin@calkos.local";
            string adminPassword = "Admin123!";

            // Verifica se esiste già un utente con questa email.
            // UserManager interroga la tabella AspNetUsers.
            var user = await userManager.FindByEmailAsync(adminEmail);

            if (user == null)
            {
                // Se l'utente non esiste, lo crea.
                // EmailConfirmed = true evita la conferma email.
                user = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                // Crea l'utente con la password specificata.
                await userManager.CreateAsync(user, adminPassword);

                // Assegna il ruolo Admin all'utente appena creato.
                // Questo è fondamentale per accedere alle aree protette.
                await userManager.AddToRoleAsync(user, "Admin");
            }

            // Se l'utente esiste già, non viene ricreato.
            // Il seeding è idempotente: puoi riavviare l'app infinite volte.
        }
    }
}
