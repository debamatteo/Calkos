using Microsoft.AspNetCore.Identity;

namespace Calkos.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Campo che indica se l'utente deve cambiare la password al primo accesso
        public bool RequirePasswordChange { get; set; } = true;
    }
}
