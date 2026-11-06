// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable
using Microsoft.AspNetCore.Authorization;//18/10/2026

using System;
using System.Threading.Tasks;

using Calkos.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Calkos.web.Areas.Identity.Pages.Account
{
    [AllowAnonymous] //18/10/2026 Aggiungere l'attributo [AllowAnonymous] sulla classe LogoutModel, altrimenti la pagina di errore rimanda al login.
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(SignInManager<ApplicationUser> signInManager, ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            HttpContext.Session.Clear();      // 18/10/2026  M-03 Aggiungere la pulizia della sessione prima di effettuare il logout effettivo con
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");

            // 19/10/2026 AGGIORNATO: Verifica che l'URL sia locale per evitare errori 500 con URL esterni
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // This needs to be a redirect so that the browser performs a new
                // request and the identity for the user gets updated.
                return RedirectToPage();
            }
        }
    }
}