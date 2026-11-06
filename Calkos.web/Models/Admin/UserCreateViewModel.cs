namespace Calkos.Web.Models.Admin
{
    // 05/11/2026 Dati della pagina "Nuovo Utente": email, password temporanea, agente e ruoli
    public class UserCreateViewModel
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public int? IdAgente { get; set; }
        public List<ManageUserRoleItemViewModel> Roles { get; set; } = new();
    }
}