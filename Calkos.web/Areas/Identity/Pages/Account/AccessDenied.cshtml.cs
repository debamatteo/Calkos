// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;//18/10/2026 Aggiungere l'attributo [AllowAnonymous] sulla classe AccessDeniedModel, altrimenti la pagina di errore rimanda al login.
namespace Calkos.web.Areas.Identity.Pages.Account
{
    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [AllowAnonymous]//18/10/2026 Aggiungere l'attributo [AllowAnonymous] sulla classe AccessDeniedModel, altrimenti la pagina di errore rimanda al login.
    public class AccessDeniedModel : PageModel
    {
        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public void OnGet()
        {
        }
    }
}
//using Microsoft.AspNetCore.Mvc.RazorPages;

//namespace Calkos.Web.Areas.Identity.Pages.Account
//{
//    public class AccessDeniedModel : PageModel
//    {
//        public void OnGet()
//        {
//            // Nessuna logica necessaria: pagina puramente informativa.
//        }
//    }
//}