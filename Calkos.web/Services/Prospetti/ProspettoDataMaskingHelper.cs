//Problema(Vulnerabilità di Sicurezza)
//Immagina di avere una tabella nell'interfaccia web che mostra delle vendite. Se l'utente collegato è un Agente,
//la pagina HTML nasconde visivamente la colonna "Commissioni Calkos" tramite CSS (es. display: none) o tramite la vista Razor.

//Tuttavia, quando il browser fa una richiesta AJAX per caricare i dati, il controller C# invia indietro un 
//oggetto JSON completo. Un utente Agente un po' smanettato potrebbe aprire i DevTools del browser (tasto F12 -> scheda Network) 
//e leggere i dati riservati direttamente nel payload JSON grezzo.

//Helper riservatezza dati — nuovo servizio condiviso
//Dato che il problema (JSON che espone tutto) è identico sui 9 moduli, meglio scriverlo
//una volta sola in modo generico via reflection, riusabile ovunque:
//Invece di duplicare la logica di pulizia dei dati nei Controller di tutti i 9 moduli della tua applicazione, questo metodo cancella fisicamente i dati sensibili in C# (Backend) prima che il server invii la risposta JSON al browser.
//Se l'utente è un Agente, i campi riservati vengono impostati a null o 0. In questo modo, anche se l'Agente apre F12, i dati riservati non esistono proprio nel pacchetto inviato via ret

using Calkos.web.Models.Prospetti;
using System.Reflection;

namespace Calkos.web.Services.Prospetti
{
    /// <summary>
    /// Oscura (imposta a null/default) le proprietà riservate all'Agente
    /// prima della serializzazione JSON, così la risposta AJAX non contiene
    /// dati che l'interfaccia nasconde solo visivamente.
    /// </summary>
    public static class ProspettoDataMaskingHelper
    {
        public static IEnumerable<T> MascheraPerAgente<T>(
            IEnumerable<T> righe, List<ProspettoColumnConfig> colonne, bool isAgente) where T : class
        {
            if (!isAgente) return righe;

            var campiDaNascondere = colonne
                .Where(c => !c.VisibileAgente)
                .Select(c => c.Name)
                .ToList();

            if (!campiDaNascondere.Any()) return righe;

            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => campiDaNascondere.Contains(p.Name) && p.CanWrite)
                .ToList();

            foreach (var riga in righe)
            {
                foreach (var prop in props)
                {
                    var defaultValue = prop.PropertyType.IsValueType
                        ? Activator.CreateInstance(prop.PropertyType)
                        : null;
                    prop.SetValue(riga, defaultValue);
                }
            }

            return righe;
        }
    }
}