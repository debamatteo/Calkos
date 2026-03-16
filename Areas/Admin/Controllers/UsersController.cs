using Calkos.web.Models.Admin;
//using Calkos.Web.Identity;
using Calkos.Web.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Calkos.Web.Models;

namespace Calkos.Web.Areas.Admin.Controllers
{
    
    [Area("Admin")]// Indica che questo controller appartiene all'area "Admin"    
    [Authorize(Roles = "Admin")]// Solo gli utenti con ruolo "Admin" possono accedere a questo controller
    public class UsersController : Controller
    {
        // UserManager permette di gestire gli utenti (ApplicationUser)
        private readonly UserManager<ApplicationUser> _userManager;
        // RoleManager permette di gestire i ruoli (IdentityRole)
        private readonly RoleManager<IdentityRole> _roleManager;
        // Il costruttore riceve UserManager e RoleManager tramite Dependency Injection
        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /Admin/Users
        // Mostra la lista di tutti gli utenti registrati
        public async Task<IActionResult> Index()
        {
            // Recupera tutti gli utenti dal database Identity
            var users = _userManager.Users.ToList();

            // Lista del ViewModel che verrà passata alla vista
            var model = new List<UserWithRolesViewModel>();

            // Per ogni utente recuperiamo i ruoli associati
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user); // Ruoli dell'utente

                // Costruzione del ViewModel
                model.Add(new UserWithRolesViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Roles = roles.ToList()
                });
            }

            // Passiamo il modello alla vista
            return View(model);
        }



        // GET: Admin/Users/ManageRoles/{id} Azione GET: mostra i ruoli con toggle
        public async Task<IActionResult> ManageRoles(string id)
        {
            // Controllo parametro
            if (string.IsNullOrEmpty(id))
                return NotFound();

            // Recupera l'utente
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            // Recupera tutti i ruoli esistenti
            var allRoles = _roleManager.Roles.ToList();

            // Recupera i ruoli attualmente assegnati all'utente
            var userRoles = await _userManager.GetRolesAsync(user);

            // Costruisce il ViewModel per la vista
            var model = new ManageUserRolesViewModel
            {
                UserId = user.Id,
                Email = user.Email
            };

            // Per ogni ruolo del sistema, crea un item con info se è selezionato
            foreach (var role in allRoles)
            {
                model.Roles.Add(new ManageUserRoleItemViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                    IsSelected = userRoles.Contains(role.Name) // true se l'utente ha questo ruolo
                });
            }

            // Passa il modello alla vista
            return View(model);
        }



        // POST: Admin/Users/ManageRoles Azione POST: salva i ruoli selezionati
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(ManageUserRolesViewModel model)
        {
            // Recupera l'utente
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
                return NotFound();

            // Ruoli attualmente assegnati all'utente
            var currentUserRoles = await _userManager.GetRolesAsync(user);

            // Ruoli che l'utente dovrebbe avere dopo il salvataggio
            var selectedRoles = model.Roles
                .Where(r => r.IsSelected)
                .Select(r => r.RoleName)
                .ToList();

            // Ruoli da aggiungere: selezionati ora ma non presenti prima
            var rolesToAdd = selectedRoles.Except(currentUserRoles).ToList();

            // Ruoli da rimuovere: presenti prima ma non più selezionati
            var rolesToRemove = currentUserRoles.Except(selectedRoles).ToList();

            // Applica le modifiche
            if (rolesToAdd.Any())
                await _userManager.AddToRolesAsync(user, rolesToAdd);

            if (rolesToRemove.Any())
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

            // Dopo il salvataggio, torna alla lista utenti
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Users/Details/{id} pagina Dettagli
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var model = new UserDetailsViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                Roles = roles.ToList()
            };

            return View(model);
        }

        // GET: Admin/Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Email e password sono obbligatorie.");
                return View();
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                RequirePasswordChange = true
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
                return RedirectToAction(nameof(Index));

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View();
        }
        // GET: Admin/Users/Delete/{id}
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            return View(user);
        }

        // POST: Admin/Users/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            await _userManager.DeleteAsync(user);

            return RedirectToAction(nameof(Index));
        }


        // GET: Admin/Users/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            return View(user);
        }

        // POST: Admin/Users/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ApplicationUser model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
                return NotFound();

            user.Email = model.Email;
            user.UserName = model.Email;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
                return RedirectToAction(nameof(Index));

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

    }
}
