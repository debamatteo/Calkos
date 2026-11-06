using Microsoft.AspNetCore.Authorization;//18/10/2026: aggiunto per evitare che utenti non loggati possano accedere a questa pagina.
using Microsoft.AspNetCore.Mvc;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Calkos.Web.Controllers
{
    /// <summary>
    /// Controller responsabile della gestione centralizzata degli errori.
    /// Viene richiamato automaticamente dal middleware configurato in Program.cs.
    /// </summary>
    [AllowAnonymous] // 18/10/2026 sulla classe ErrorController(altrimenti la pagina di errore rimanda al login).
    public class ErrorController : Controller
    {
        /// <summary>
        /// Gestisce gli errori HTTP (404, 500, ecc.)
        /// Il parametro "statusCode" viene passato dal middleware UseStatusCodePagesWithReExecute.
        /// </summary>
        /// <param name="statusCode">Codice HTTP dell'errore</param>
        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            // Passiamo il codice errore alla View, utile per la pagina GenericError
            ViewData["StatusCode"] = statusCode;

            // In base al codice, restituiamo la View corretta
            return statusCode switch
            {
                404 => View("NotFound"),      // Pagina non trovata
                500 => View("ServerError"),   // Errore interno del server
                _ => View("GenericError")   // Qualsiasi altro errore
            };
        }
    }
}
