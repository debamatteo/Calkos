
namespace Calkos.Web.Models.Admin
{
    // Rappresenta un singolo ruolo con info se è assegnato all'utente
    public class ManageUserRoleItemViewModel
    {
        public string RoleId { get; set; }      // Id del ruolo
        public string RoleName { get; set; }    // Nome del ruolo (es. "Admin")
        public bool IsSelected { get; set; }    // True se l'utente ha questo ruolo
    }
}
