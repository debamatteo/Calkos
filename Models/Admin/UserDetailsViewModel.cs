namespace Calkos.Web.Models.Admin
{
    // ViewModel per la pagina Dettagli Utente
    public class UserDetailsViewModel
    {
        public string UserId { get; set; }      // Id utente
        public string Email { get; set; }       // Email utente
        public List<string> Roles { get; set; } = new(); // Ruoli assegnati
    }
}
