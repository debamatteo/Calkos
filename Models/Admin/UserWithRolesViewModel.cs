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
    }
}
