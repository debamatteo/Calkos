using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Calkos.Web.Models; // IMPORTANTE
//Questo DbContext ApplicationDbContext è solo per Identity, non contiene tabelle business.
//eredita da IdentityDbContext e quindi contiene automaticamente le tabelle di Identity:
//AspNetUsers/AspNetRoles/AspNetUserRoles/ecc.
namespace Calkos.Web.Identity
{
    public class ApplicationDbContext: IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
    }
}
