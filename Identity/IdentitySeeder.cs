using Microsoft.AspNetCore.Identity;

namespace Calkos.Web.Identity
{
    public static class IdentitySeeder
    {
        // Metodo eseguito all'avvio dell'applicazione per garantire
        // che i ruoli fondamentali del sistema esistano nel database.
        // Se il database è nuovo o è stato resettato, questo metodo
        // ricrea automaticamente i ruoli necessari.
        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            // Elenco dei ruoli richiesti dal sistema.
            // In futuro puoi aggiungerne altri semplicemente inserendoli qui.
            string[] roles = { "Admin", "Operatore" };

            foreach (var role in roles)
            {
                // Controlla se il ruolo esiste già nel database.
                // RoleManager interroga la tabella AspNetRoles.
                if (!await roleManager.RoleExistsAsync(role))
                {
                    // Se il ruolo NON esiste, lo crea.
                    // Questo evita errori quando assegni ruoli agli utenti.
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
                // Se esiste, non fa nulla: il seeding è idempotente.
                // Idempotente = puoi eseguirlo infinite volte senza duplicare dati.
            }
        }
    }
}
