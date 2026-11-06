//namespace Calkos.web.Models
//{
//    public class UserWithRolesViewModel
//    {
//    }
//}
namespace Calkos.web.Models.Admin
{
    public class UserWithRolesViewModel
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool IsBloccato { get; set; } // ==== MODIFICA 25/08/2026 ====

        // ==== MODIFICA 05/11/2026 - INIZIO ====
        public int? IdAgente { get; set; }
        public string? AgenteDescrizione { get; set; }
        // ==== MODIFICA 05/11/2026 - FINE ====

    }
}
