using System.Security.Claims;

namespace Calkos.Web.Identity
{
    /// <summary>
    /// 07/08/2026 
    //Metodi di comodo per leggere dati custom dall'utente loggato,
    /// riusabili in tutti i controller senza ripetere la logica di parsing.
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Ritorna l'IdAgente dell'utente loggato, oppure null se non è
        /// un Agente (o il claim non è presente).
        /// </summary>
        public static int? GetIdAgente(this ClaimsPrincipal user)
        {
            var value = user.FindFirst("IdAgente")?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }


        /// <summary>
        /// 18/06/2026 Admin/Operatore: true, idAgente null (nessun filtro).
        /// Agente con IdAgente: true, idAgente valorizzato.
        /// Agente SENZA IdAgente: false, da trattare come accesso negato (mai come "nessun filtro").
        /// </summary>
        public static bool TryGetFiltroAgente(this ClaimsPrincipal user, out int? idAgente)
        {
            idAgente = user.GetIdAgente();
            if (!user.IsInRole("Agente"))
                return true;
            return idAgente.HasValue;
        }





    }
}