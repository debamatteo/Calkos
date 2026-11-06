//using Calkos.Web.Models;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
//using System.Security.Claims;
//// 07/08/2026
//namespace Calkos.Web.Identity
//{
//    /// <summary>
//    /// Estende i claim standard di Identity aggiungendo "IdAgente",
//    /// così da poterlo leggere da User.Claims senza query al DB
//    /// ad ogni richiesta.
//    /// </summary>
//    public class ApplicationUserClaimsPrincipalFactory
//        : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
//    {
//        public ApplicationUserClaimsPrincipalFactory(
//            UserManager<ApplicationUser> userManager,
//            RoleManager<IdentityRole> roleManager,
//            Microsoft.Extensions.Options.IOptions<IdentityOptions> optionsAccessor)
//            : base(userManager, roleManager, optionsAccessor)
//        {
//        }

//        public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
//        {
//            var principal = await base.CreateAsync(user);

//            if (user.IdAgente.HasValue)
//            {
//                ((ClaimsIdentity)principal.Identity!).AddClaim(
//                    new Claim("IdAgente", user.IdAgente.Value.ToString()));
//            }
//            //18/10/2026
//            // Il claim sparisce da solo dopo il cambio password, perché ForceChangePassword chiama RefreshSignInAsync. Chi è già loggato ora non ce l’ha fino al prossimo login.
//            if (user.RequirePasswordChange)
//            {
//                ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim("RequirePasswordChange", "true"));
//            }

//            return principal;
//        }
//    }
//}
using Calkos.Web.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
//19/10/2026
namespace Calkos.Web.Identity
{
    /// <summary>
    /// Aggiunge ai claim standard: "IdAgente" (isolamento dati agente) e
    /// "RequirePasswordChange" (cambio password obbligatorio, letto dal middleware).
    /// </summary>
    public class ApplicationUserClaimsPrincipalFactory
        : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        public ApplicationUserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            Microsoft.Extensions.Options.IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor)
        {
        }

        public override async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
        {
            var principal = await base.CreateAsync(user);
            var identity = (ClaimsIdentity)principal.Identity!;

            if (user.IdAgente.HasValue)
                identity.AddClaim(new Claim("IdAgente", user.IdAgente.Value.ToString()));

            if (user.RequirePasswordChange)//18/10/2026
                                           //            // Il claim sparisce da solo dopo il cambio password, perché ForceChangePassword chiama RefreshSignInAsync. Chi è già loggato ora non ce l’ha fino al prossimo lo
                identity.AddClaim(new Claim("RequirePasswordChange", "1"));

            return principal;
        }
    }
}
