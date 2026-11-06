// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;//18/10/2026: aggiunto per evitare che utenti non loggati possano accedere a questa pagina.
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
    [AllowAnonymous]//18/108/2026: aggiunto per evitare che utenti non loggati possano accedere a questa pagina.
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
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)//19/10/2026: sostituito il login standard con quello customizzato per evitare di rivelare se l'account esiste o è bloccato
        {
            if (!ModelState.IsValid)
                return Page();

            // Un solo messaggio per tutti i casi: non rivela se l'account esiste o è bloccato
            const string messaggioGenerico = "Credenziali non valide o account temporaneamente bloccato.";

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // Simula il costo dell'hash per non rendere distinguibile "utente inesistente" dal tempo di risposta
                _userManager.PasswordHasher.HashPassword(new ApplicationUser(), Input.Password);
                ModelState.AddModelError(string.Empty, messaggioGenerico);
                return Page();
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName,
                Input.Password,
                Input.RememberMe,
                lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                    _logger.LogWarning("Accesso negato: account bloccato ({UserId})", user.Id);

                ModelState.AddModelError(string.Empty, messaggioGenerico);
                return Page();
            }

            // 20/10/2026: Utente senza ruolo non deve ricevere il cookie
            var ruoliAmmessi = new[] { "Admin", "Operatore", "Agente" };
            var ruoliUtente = await _userManager.GetRolesAsync(user);
            if (!ruoliUtente.Intersect(ruoliAmmessi).Any())
            {
                await _signInManager.SignOutAsync();   // revoca il cookie appena emesso
                ModelState.AddModelError(string.Empty, "Utente non abilitato. Contatta l'amministratore.");
                return Page();
            }

            // Password temporanea: il middleware (P7) obbliga comunque al cambio
            if (user.RequirePasswordChange)
                return RedirectToPage("/Account/ForceChangePassword");

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return LocalRedirect("/Admin/Dashboard");

            if (await _userManager.IsInRoleAsync(user, "Operatore"))
                return LocalRedirect("/Admin/Prospetti");

            if (await _userManager.IsInRoleAsync(user, "Agente"))
                return LocalRedirect("/Admin/Prospetti");

            // Nessun ruolo riconosciuto: niente sessione aperta
            await _signInManager.SignOutAsync();
            ModelState.AddModelError(string.Empty, "Nessun ruolo assegnato a questo account. Contatta l'amministratore.");
            return Page();
        }
        //public async Task<IActionResult> OnPostAsync(string returnUrl = null)//1810/2026: commentato per sostituire il login con quello customizzato
        //{
        //    // Se non specificato, torna alla home
        //    returnUrl ??= Url.Content("~/");

        //    if (ModelState.IsValid)
        //    {
        //        // 1️⃣ Recupera l'utente tramite email
        //        var user = await _userManager.FindByEmailAsync(Input.Email);

        //        if (user == null)
        //        {
        //            // Utente non trovato → login fallito
        //            ModelState.AddModelError(string.Empty, "Tentativo di login non valido.");
        //            return Page();
        //        }

        //        // 2️⃣ Prova ad effettuare il login
        //        var result = await _signInManager.PasswordSignInAsync(
        //            user.UserName,
        //            Input.Password,
        //            Input.RememberMe,
        //           lockoutOnFailure: true);          // 18/10/2026 era false




        //        if (result.Succeeded)
        //        {
        //            //Aggiungere la pulizia della sessione subito dopo un login riuscito tramite
        //            HttpContext.Session.Clear();      // 18/10/2026 M-03: niente IdMandatario di un utente precedente
        //            // 3️⃣ Controllo password temporanea
        //            // Se l'utente deve cambiare la password → reindirizza
        //            if (user.RequirePasswordChange)
        //            {
        //                //return RedirectToAction("ForceChangePassword", "Account");
        //                return RedirectToPage("/Account/ForceChangePassword");//12/05/2026

        //            }

        //            //// 4️⃣ Login normale
        //            //return LocalRedirect(returnUrl);

        //            // 4️⃣ Redirect in base al ruolo
        //            if (await _userManager.IsInRoleAsync(user, "Admin"))
        //            {
        //                return LocalRedirect("/Admin/Dashboard");
        //            }

        //            if (await _userManager.IsInRoleAsync(user, "Operatore"))
        //            {
        //                return LocalRedirect("/Admin/Prospetti");
        //            }

        //            // NUOVO 07/08/2026: al momento solo Cobral supporta il ruolo Agente
        //            // (Fase 4 completata solo su questo modulo). Quando avremo replicato
        //            // il pattern sugli altri 8, questo redirect andrà spostato sull'hub
        //            // generico "/Admin/Prospetti" come per l'Operatore.
        //            if (await _userManager.IsInRoleAsync(user, "Agente"))
        //                return LocalRedirect("/Admin/Prospetti");


        //        }


        //        if (result.IsLockedOut)//25/08/2026: aggiunto controllo per utente bloccato
        //        {
        //            ModelState.AddModelError(string.Empty, "Account bloccato. Contatta l'amministratore.");
        //            return Page();
        //        }


        //        // Login fallito
        //        ModelState.AddModelError(string.Empty, "Tentativo di login non valido.");
        //        return Page();
        //    }

        //    return Page();
        //}

    }
}
