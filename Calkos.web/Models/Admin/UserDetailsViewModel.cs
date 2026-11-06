using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Identity;

namespace Calkos.Web.Models.Admin
{
    // ViewModel per la pagina Dettagli Utente
    public class UserDetailsViewModel
    {
        public string UserId { get; set; }      // Id utente
        public string Email { get; set; }       // Email utente
        public List<string> Roles { get; set; } = new(); // Ruoli assegnati

        public bool IsBloccato { get; set; } // ==== MODIFICA 25/08/2026 ====
    }
}
