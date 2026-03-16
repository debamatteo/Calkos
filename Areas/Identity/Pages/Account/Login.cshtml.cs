// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
//using Calkos.Web.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Calkos.Web.Models;

namespace Calkos.web.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        // 🔥 Devi aggiungere UserManager<ApplicationUser>
        private readonly UserManager<ApplicationUser> _userManager;

        // 🔥 SignInManager deve usare ApplicationUser
        private readonly SignInManager<ApplicationUser> _signInManager;

        private readonly ILogger<LoginModel> _logger;

        // 🔥 Costruttore corretto: aggiungi userManager
        public LoginModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<LoginModel> logger)
        {
            _userManager = userManager;       // inizializzazione
            _signInManager = signInManager;   // inizializzazione
            _logger = logger;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            // Se non specificato, torna alla home
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                // 1️⃣ Recupera l'utente tramite email
                var user = await _userManager.FindByEmailAsync(Input.Email);

                if (user == null)
                {
                    // Utente non trovato → login fallito
                    ModelState.AddModelError(string.Empty, "Tentativo di login non valido.");
                    return Page();
                }

                // 2️⃣ Prova ad effettuare il login
                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName,
                    Input.Password,
                    Input.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    // 3️⃣ Controllo password temporanea
                    // Se l'utente deve cambiare la password → reindirizza
                    if (user.RequirePasswordChange)
                    {
                        return RedirectToAction("ForceChangePassword", "Account");
                    }

                    //// 4️⃣ Login normale
                    //return LocalRedirect(returnUrl);

                    // 4️⃣ Redirect in base al ruolo
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return LocalRedirect("/Admin/Dashboard");
                    }

                    if (await _userManager.IsInRoleAsync(user, "Operatore"))
                    {
                        return LocalRedirect("/Admin/Prospetti");
                    }


                }

                // Login fallito
                ModelState.AddModelError(string.Empty, "Tentativo di login non valido.");
                return Page();
            }

            return Page();
        }

    }
}
