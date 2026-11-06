namespace Calkos.Web.Models.Admin
{
    // 05/11/2026 Dati della pagina "Modifica Utente" e della finestra "Agente e Ruoli" in Index.
    // Entrambe inviano questo modello a POST Admin/Users/Edit.
    public class UserEditViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int? IdAgente { get; set; }
        public List<ManageUserRoleItemViewModel> Roles { get; set; } = new();
    }
}