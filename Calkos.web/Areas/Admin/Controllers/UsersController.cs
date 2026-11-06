using Calkos.web.Helpers;
using Calkos.web.Models.Admin;
using Calkos.Web.Models;
using Calkos.Web.Models.Admin;
using CalkosManager.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
//19/06/2026
namespace Calkos.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private const string RuoloAdmin = "Admin";
        private const string RuoloAgente = "Agente";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AgenteService _agenteService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            AgenteService agenteService,
            ILogger<UsersController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _agenteService = agenteService;
            _logger = logger;
        }

        // ------------------------------------------------------------------
        // Helper
        // ------------------------------------------------------------------

        /// <summary>Traccia le operazioni sensibili (mai password o token).</summary>
        private void Audit(string azione, ApplicationUser bersaglio)
        {
            _logger.LogWarning("AUDIT {Azione}: utente {UserId} ({Email}) eseguita da {Admin}",
                azione, bersaglio.Id, bersaglio.Email, User.Identity?.Name ?? "sconosciuto");
        }

        private bool EAdminCorrente(ApplicationUser utente) =>
            utente.Id == _userManager.GetUserId(User);

        /// <summary>True se, togliendo l'Admin all'utente, non resterebbe nessun altro Admin attivo.</summary>
        private async Task<bool> RimarrebbeSenzaAdminAsync(ApplicationUser utente)
        {
            if (!await _userManager.IsInRoleAsync(utente, RuoloAdmin))
                return false;

            var admin = await _userManager.GetUsersInRoleAsync(RuoloAdmin);
            var altriAttivi = admin.Count(a =>
                a.Id != utente.Id &&
                !(a.LockoutEnd.HasValue && a.LockoutEnd > DateTimeOffset.UtcNow));

            return altriAttivi == 0;
        }

        private void CaricaAgenti(int? selezionato = null)
        {
            ViewBag.Agenti = new SelectList(_agenteService.GetAll(), "IdAgente", "AgenteDescrizione", selezionato);
        }

        // ==== MODIFICA 05/11/2026 - INIZIO ====
        // Elenco di TUTTI i ruoli, con la spunta su quelli indicati (usato da Edit e dalla finestra di Index)
        private List<ManageUserRoleItemViewModel> CostruisciRuoli(IEnumerable<string>? selezionati)
        {
            var scelti = new HashSet<string>(selezionati ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

            return _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToList()
                .Select(r => new ManageUserRoleItemViewModel
                {
                    RoleId = r.Id,
                    RoleName = r.Name!,
                    IsSelected = scelti.Contains(r.Name!)
                })
                .ToList();
        }

        // Riapre la pagina Modifica dopo un errore, mantenendo le spunte scelte dall'utente
        private IActionResult RiproponiEdit(UserEditViewModel model)
        {
            var scelti = (model.Roles ?? new List<ManageUserRoleItemViewModel>())
                .Where(r => r.IsSelected).Select(r => r.RoleName);

            model.Roles = CostruisciRuoli(scelti);
            CaricaAgenti(model.IdAgente);
            return View("Edit", model);
        }
        // ==== MODIFICA 05/11/2026 - FINE ====

        private IActionResult ErroreRuoli(string userId, string messaggio)
        {
            TempData["Errore"] = messaggio;
            return RedirectToAction(nameof(ManageRoles), new { id = userId });
        }

        // ------------------------------------------------------------------
        // Elenco
        // ------------------------------------------------------------------

        // GET: /Admin/Users
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var model = new List<UserWithRolesViewModel>();

            // 05/11/2026 Elenco agenti letto una volta sola, non una query per ogni utente
            var agenti = _agenteService.GetAll().ToDictionary(a => a.IdAgente, a => a.AgenteDescrizione);

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                // ==== MODIFICA 05/11/2026 - INIZIO ====
                string? descrizioneAgente = null;
                if (user.IdAgente.HasValue)
                    descrizioneAgente = agenti.TryGetValue(user.IdAgente.Value, out var d)
                        ? d
                        : $"(agente {user.IdAgente} non trovato)";
                // ==== MODIFICA 05/11/2026 - FINE ====

                model.Add(new UserWithRolesViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Roles = roles.ToList(),
                    IsBloccato = await _userManager.IsLockedOutAsync(user),
                    IdAgente = user.IdAgente,                 // 05/11/2026
                    AgenteDescrizione = descrizioneAgente     // 05/11/2026
                });
            }

            // 05/11/2026 Dati per la finestra "Agente e Ruoli"
            CaricaAgenti();
            ViewBag.Ruoli = CostruisciRuoli(null);

            return View(model);
        }

        // ------------------------------------------------------------------
        // Ruoli
        // ------------------------------------------------------------------

        // GET: Admin/Users/ManageRoles/{id}
        public async Task<IActionResult> ManageRoles(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var allRoles = _roleManager.Roles.ToList();
            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new ManageUserRolesViewModel
            {
                UserId = user.Id,
                Email = user.Email
            };

            foreach (var role in allRoles)
            {
                model.Roles.Add(new ManageUserRoleItemViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                    IsSelected = userRoles.Contains(role.Name)
                });
            }

            return View(model);
        }

        // POST: Admin/Users/ManageRoles
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(ManageUserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
                return NotFound();

            // Solo ruoli che esistono davvero (i nomi arrivano dal client).
            // Confronto senza maiuscole/minuscole, come fa Identity; poi si usa il nome canonico.
            var ruoliEsistenti = _roleManager.Roles.Select(r => r.Name).ToList();

            var richiesti = (model.Roles ?? new List<ManageUserRoleItemViewModel>())
                .Where(r => r.IsSelected && !string.IsNullOrWhiteSpace(r.RoleName))
                .Select(r => r.RoleName.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var sconosciuti = richiesti
                .Where(n => !ruoliEsistenti.Contains(n, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (sconosciuti.Any())
                return ErroreRuoli(model.UserId, "Ruolo non valido: " + string.Join(", ", sconosciuti));

            var selectedRoles = richiesti
                .Select(n => ruoliEsistenti.First(e => string.Equals(e, n, StringComparison.OrdinalIgnoreCase))!)
                .ToList();

            // Coerenza: un utente con ruolo Agente deve avere un IdAgente collegato
            if (selectedRoles.Contains(RuoloAgente, StringComparer.OrdinalIgnoreCase) && user.IdAgente == null)
            {
                // 31/10/2026 Segnala alla view di mostrare i pulsanti "Collega / Crea agente"
                TempData["MostraCollegaAgente"] = true;
                return ErroreRuoli(model.UserId,
                    "Impossibile assegnare il ruolo Agente: all'utente non è collegato nessun agente.");
            }

            var currentUserRoles = await _userManager.GetRolesAsync(user);
            var rolesToAdd = selectedRoles.Except(currentUserRoles, StringComparer.OrdinalIgnoreCase).ToList();
            var rolesToRemove = currentUserRoles.Except(selectedRoles, StringComparer.OrdinalIgnoreCase).ToList();

            // Non si può togliere Admin a se stessi né all'ultimo Admin attivo
            if (rolesToRemove.Contains(RuoloAdmin, StringComparer.OrdinalIgnoreCase))
            {
                if (EAdminCorrente(user))
                    return ErroreRuoli(model.UserId, "Non puoi rimuovere il ruolo Admin dal tuo stesso account.");

                if (await RimarrebbeSenzaAdminAsync(user))
                    return ErroreRuoli(model.UserId, "Impossibile rimuovere l'ultimo Admin attivo.");
            }

            if (rolesToAdd.Any())
            {
                var r = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!r.Succeeded)
                    return ErroreRuoli(model.UserId, "Assegnazione ruoli non riuscita.");
            }

            if (rolesToRemove.Any())
            {
                var r = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!r.Succeeded)
                    return ErroreRuoli(model.UserId, "Rimozione ruoli non riuscita.");
            }

            // Le sessioni già aperte dell'utente si aggiornano/chiudono entro 1 minuto
            await _userManager.UpdateSecurityStampAsync(user);

            Audit($"Ruoli modificati (aggiunti: {string.Join(",", rolesToAdd)}; rimossi: {string.Join(",", rolesToRemove)})", user);

            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------------
        // Dettagli
        // ------------------------------------------------------------------

        // GET: Admin/Users/Details/{id}
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
                Roles = roles.ToList(),
                IsBloccato = await _userManager.IsLockedOutAsync(user)
            };

            return View(model);
        }

        // ------------------------------------------------------------------
        // Creazione
        // ------------------------------------------------------------------

        // GET: Admin/Users/Create
        // GET: Admin/Users/Create
        public IActionResult Create()
        {
            CaricaAgenti();
            // 05/11/2026 Elenco ruoli senza spunte
            return View(new UserCreateViewModel { Roles = CostruisciRuoli(null) });
        }

        // POST: Admin/Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            var errori = new List<string>();

            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                errori.Add("Email e password sono obbligatorie.");

            if (model.IdAgente.HasValue && _agenteService.GetById(model.IdAgente.Value) == null)
                errori.Add("L'agente selezionato non esiste.");

            // ==== MODIFICA 05/11/2026 - INIZIO ====
            // Ruoli da assegnare: solo quelli spuntati e che esistono davvero (i nomi arrivano dal client)
            var ruoliEsistenti = _roleManager.Roles.Select(r => r.Name!).ToList();
            var daAssegnare = new List<string>();

            foreach (var voce in model.Roles ?? new List<ManageUserRoleItemViewModel>())
            {
                if (!voce.IsSelected || string.IsNullOrWhiteSpace(voce.RoleName))
                    continue;

                var nome = ruoliEsistenti.FirstOrDefault(e =>
                    string.Equals(e, voce.RoleName.Trim(), StringComparison.OrdinalIgnoreCase));

                if (nome == null)
                    errori.Add("Ruolo non valido: " + voce.RoleName);
                else if (!daAssegnare.Contains(nome))
                    daAssegnare.Add(nome);
            }

            // Un Agente senza agente collegato vedrebbe dati non suoi
            if (model.IdAgente == null && daAssegnare.Contains(RuoloAgente, StringComparer.OrdinalIgnoreCase))
                errori.Add("Un utente con ruolo Agente deve avere un agente collegato.");
            // ==== MODIFICA 05/11/2026 - FINE ====

            if (errori.Any())
            {
                foreach (var e in errori)
                    ModelState.AddModelError("", e);

                return RiproponiCreate(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email!.Trim(),
                Email = model.Email.Trim(),
                RequirePasswordChange = true,
                IdAgente = model.IdAgente
            };

            var result = await _userManager.CreateAsync(user, model.Password!);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return RiproponiCreate(model);
            }

            Audit("Utente creato", user);

            // ==== MODIFICA 05/11/2026 - INIZIO ====
            // L'utente esiste già: se i ruoli falliscono si apre Modifica con un avviso, senza ricrearlo
            if (daAssegnare.Any())
            {
                var ruoli = await _userManager.AddToRolesAsync(user, daAssegnare);
                if (!ruoli.Succeeded)
                {
                    TempData["Errore"] = "Utente creato, ma l'assegnazione dei ruoli non è riuscita. Controlla i ruoli e riprova.";
                    return RedirectToAction(nameof(Edit), new { id = user.Id });
                }

                Audit($"Ruoli assegnati alla creazione: {string.Join(",", daAssegnare)}", user);
            }
            // ==== MODIFICA 05/11/2026 - FINE ====

            return RedirectToAction(nameof(Index));
        }

        // 05/11/2026 Riapre la pagina dopo un errore: ruoli e agente restano, la password no
        private IActionResult RiproponiCreate(UserCreateViewModel model)
        {
            var scelti = (model.Roles ?? new List<ManageUserRoleItemViewModel>())
                .Where(r => r.IsSelected).Select(r => r.RoleName);

            model.Roles = CostruisciRuoli(scelti);
            model.Password = null;
            CaricaAgenti(model.IdAgente);
            return View("Create", model);
        }
        // ------------------------------------------------------------------
        // Eliminazione
        // ------------------------------------------------------------------

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

            if (EAdminCorrente(user))
            {
                TempData["Errore"] = "Non puoi eliminare il tuo stesso account.";
                return RedirectToAction(nameof(Index));
            }

            if (await RimarrebbeSenzaAdminAsync(user))
            {
                TempData["Errore"] = "Impossibile eliminare l'ultimo Admin attivo.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["Errore"] = "Eliminazione non riuscita.";
                return RedirectToAction(nameof(Index));
            }

            Audit("Utente eliminato", user);
            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------------------------------
        // Modifica
        // ------------------------------------------------------------------

        // GET: Admin/Users/Edit/{id}
        // 05/11/2026 idAgenteCreato arriva da AgentiController.Save dopo "Nuovo agente"
        public async Task<IActionResult> Edit(string id, int? idAgenteCreato = null)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var model = new UserEditViewModel
            {
                Id = user.Id,
                Email = user.Email,
                IdAgente = user.IdAgente,
                Roles = CostruisciRuoli(await _userManager.GetRolesAsync(user))
            };

            // ==== MODIFICA 05/11/2026 - INIZIO ====
            // Agente appena creato: lo si preseleziona (solo se esiste davvero). Il collegamento
            // all'utente si salva comunque con "Salva", dopo tutti i controlli.
            if (idAgenteCreato.HasValue && _agenteService.GetById(idAgenteCreato.Value) != null)
            {
                model.IdAgente = idAgenteCreato;
                ViewBag.AgenteAppenaCreato = true;
            }
            // ==== MODIFICA 05/11/2026 - FINE ====

            CaricaAgenti(model.IdAgente);
            return View(model);
        }

        // POST: Admin/Users/Edit
        // 05/11/2026 Usato sia dalla pagina Modifica sia dalla finestra "Agente e Ruoli" di Index.
        // Dal form si leggono SOLO Id, Email, IdAgente e i ruoli: nessun altro campo dell'utente viene copiato.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
                return NotFound();

            var errori = new List<string>();

            if (string.IsNullOrWhiteSpace(model.Email))
                errori.Add("L'email è obbligatoria.");

            if (model.IdAgente.HasValue && _agenteService.GetById(model.IdAgente.Value) == null)
                errori.Add("L'agente selezionato non esiste.");

            // ==== MODIFICA 05/11/2026 - INIZIO ====
            // Ruoli: si confrontano quelli inviati dal form con quelli attuali dell'utente.
            // Si toccano SOLO i ruoli presenti nel form: un ruolo creato nel frattempo non viene rimosso.
            var ruoliEsistenti = _roleManager.Roles.Select(r => r.Name!).ToList();
            var ruoliAttuali = await _userManager.GetRolesAsync(user);
            var daAggiungere = new List<string>();
            var daRimuovere = new List<string>();

            foreach (var voce in model.Roles ?? new List<ManageUserRoleItemViewModel>())
            {
                if (string.IsNullOrWhiteSpace(voce.RoleName))
                    continue;

                // I nomi arrivano dal client: si accettano solo ruoli che esistono davvero
                var nome = ruoliEsistenti.FirstOrDefault(e =>
                    string.Equals(e, voce.RoleName.Trim(), StringComparison.OrdinalIgnoreCase));

                if (nome == null)
                {
                    if (voce.IsSelected)
                        errori.Add("Ruolo non valido: " + voce.RoleName);
                    continue;
                }

                var presente = ruoliAttuali.Contains(nome, StringComparer.OrdinalIgnoreCase);

                if (voce.IsSelected && !presente && !daAggiungere.Contains(nome))
                    daAggiungere.Add(nome);
                else if (!voce.IsSelected && presente && !daRimuovere.Contains(nome))
                    daRimuovere.Add(nome);
            }

            // Un Agente senza agente collegato vedrebbe dati non suoi. Il controllo vale sui ruoli
            // CHE L'UTENTE AVRÀ dopo il salvataggio, così agente e ruolo si possono impostare insieme.
            var ruoliFinali = ruoliAttuali
                .Except(daRimuovere, StringComparer.OrdinalIgnoreCase)
                .Concat(daAggiungere);

            if (model.IdAgente == null && ruoliFinali.Contains(RuoloAgente, StringComparer.OrdinalIgnoreCase))
                errori.Add("Un utente con ruolo Agente deve avere un agente collegato.");

            // Non si può togliere Admin a se stessi né all'ultimo Admin attivo
            if (daRimuovere.Contains(RuoloAdmin, StringComparer.OrdinalIgnoreCase))
            {
                if (EAdminCorrente(user))
                    errori.Add("Non puoi rimuovere il ruolo Admin dal tuo stesso account.");
                else if (await RimarrebbeSenzaAdminAsync(user))
                    errori.Add("Impossibile rimuovere l'ultimo Admin attivo.");
            }
            // ==== MODIFICA 05/11/2026 - FINE ====

            if (errori.Any())
            {
                foreach (var e in errori)
                    ModelState.AddModelError("", e);

                return RiproponiEdit(model);
            }

            user.Email = model.Email!.Trim();
            user.UserName = user.Email;
            user.IdAgente = model.IdAgente;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return RiproponiEdit(model);
            }

            // ==== MODIFICA 05/11/2026 - INIZIO ====
            // I ruoli si aggiornano dopo l'utente. Identity salva a passi separati (nessuna transazione unica):
            // se i ruoli falliscono si riapre la pagina con i dati reali del database e un avviso.
            if (daAggiungere.Any() && !(await _userManager.AddToRolesAsync(user, daAggiungere)).Succeeded)
                return ErroreRuoliDopoSalvataggio(user);

            if (daRimuovere.Any() && !(await _userManager.RemoveFromRolesAsync(user, daRimuovere)).Succeeded)
                return ErroreRuoliDopoSalvataggio(user);
            // ==== MODIFICA 05/11/2026 - FINE ====

            // Agente e ruoli finiscono nei claim: le sessioni aperte si aggiornano entro 1 minuto
            await _userManager.UpdateSecurityStampAsync(user);

            Audit($"Utente modificato (IdAgente: {user.IdAgente?.ToString() ?? "nessuno"}; " +
                  $"ruoli aggiunti: {string.Join(",", daAggiungere)}; rimossi: {string.Join(",", daRimuovere)})", user);

            return RedirectToAction(nameof(Index));
        }

        // 05/11/2026
        private IActionResult ErroreRuoliDopoSalvataggio(ApplicationUser user)
        {
            TempData["Errore"] = "Dati utente salvati, ma l'aggiornamento dei ruoli non è riuscito. Controlla i ruoli e riprova.";
            return RedirectToAction(nameof(Edit), new { id = user.Id });
        }



















        // ------------------------------------------------------------------
        // Reset password / blocco / sblocco (AJAX)
        // ------------------------------------------------------------------

        // POST: Admin/Users/ResetUserPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetUserPassword([FromBody] UserIdRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserId))
                return BadRequest(new { message = "UserId non specificato." });

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
                return NotFound(new { message = "Utente non trovato." });

            var nuovaPassword = PasswordGenerator.Genera();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, nuovaPassword);

            if (!result.Succeeded)
            {
                var errori = string.Join("; ", result.Errors.Select(e => e.Description));
                return BadRequest(new { message = $"Reset non riuscito: {errori}" });
            }

            user.RequirePasswordChange = true;
            await _userManager.UpdateAsync(user);
            await _userManager.ResetAccessFailedCountAsync(user);

            Audit("Reset password", user);

            // La password temporanea è mostrata una sola volta: la risposta non deve finire in cache
            Response.Headers["Cache-Control"] = "no-store";

            return Ok(new
            {
                success = true,
                tempPassword = nuovaPassword,
                message = "Password temporanea generata con successo."
            });
        }

        // POST: Admin/Users/LockUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockUser([FromBody] UserIdRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserId))
                return BadRequest(new { message = "UserId non specificato." });

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
                return NotFound(new { message = "Utente non trovato." });

            if (EAdminCorrente(user))
                return BadRequest(new { message = "Non puoi bloccare il tuo stesso account." });

            if (await RimarrebbeSenzaAdminAsync(user))
                return BadRequest(new { message = "Impossibile bloccare l'ultimo Admin attivo." });

            if (!user.LockoutEnabled)
                await _userManager.SetLockoutEnabledAsync(user, true);

            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

            // Chiude le sessioni già aperte dell'utente (entro 1 minuto)
            await _userManager.UpdateSecurityStampAsync(user);

            Audit("Utente bloccato", user);

            return Ok(new { message = $"Utente {user.Email} bloccato." });
        }

        // POST: Admin/Users/UnlockUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockUser([FromBody] UserIdRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.UserId))
                return BadRequest(new { message = "UserId non specificato." });

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
                return NotFound(new { message = "Utente non trovato." });

            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(-1));
            await _userManager.ResetAccessFailedCountAsync(user);

            Audit("Utente sbloccato", user);

            return Ok(new { message = $"Utente {user.Email} sbloccato." });
        }
    }
}
