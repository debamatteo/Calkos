namespace Calkos.Web.Models.Admin
{
    // ViewModel usato dalla pagina "Assegnazione Ruoli" per un singolo utente
    public class ManageUserRolesViewModel
    {
        public string UserId { get; set; }                  // Id dell'utente
        public string Email { get; set; }                   // Email dell'utente
        public List<ManageUserRoleItemViewModel> Roles { get; set; } = new(); // Elenco ruoli disponibili
    }
}
