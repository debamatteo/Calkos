using Microsoft.AspNetCore.Identity;

namespace Calkos.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Campo che indica se l'utente deve cambiare la password al primo accesso
        public bool RequirePasswordChange { get; set; } = true;

        //07/08/2026 Collegamento opzionale all'anagrafica Agenti (dbo.Agenti.IdAgente).
        // Valorizzato SOLO per utenti con ruolo "Agente": serve a isolare
        // i dati (ordini/prospetti) di competenza di quell'agente.
        public int? IdAgente { get; set; } //07/08/2026



    }
}
