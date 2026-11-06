using System.ComponentModel.DataAnnotations;
using Calkos.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
//19/10/2026
[Authorize]
public class ForceChangePasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ForceChangePasswordModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public class InputModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Nuova password")]
        public string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Conferma nuova password")]
        [Compare(nameof(NewPassword), ErrorMessage = "Le due password non coincidono.")]
        public string ConfermaPassword { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToPage("/Account/Login");

        // La pagina serve SOLO a chi ha una password temporanea
        if (!user.RequirePasswordChange)
            return RedirectToAction("Index", "Home", new { area = "" });

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToPage("/Account/Login");

        // Senza flag la pagina non può essere usata per cambiare password senza conoscere quella attuale
        if (!user.RequirePasswordChange)
            return RedirectToAction("Index", "Home", new { area = "" });

        // La nuova password deve essere diversa da quella temporanea
        if (await _userManager.CheckPasswordAsync(user, Input.NewPassword))
        {
            ModelState.AddModelError("Input.NewPassword", "La nuova password deve essere diversa da quella temporanea.");
            return Page();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, Input.NewPassword);

        if (result.Succeeded)
        {
            user.RequirePasswordChange = false;
            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);

            // Home sceglie la destinazione in base al ruolo
            return RedirectToAction("Index", "Home", new { area = "" });
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return Page();
    }
}
