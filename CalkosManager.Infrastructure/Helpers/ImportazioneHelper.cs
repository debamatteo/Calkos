/* Microsoft.Extensions.Options libreria di .NET che serve per leggere valori da appsettings.json  
e metterli dentro una classe C# automaticamente.*/
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CalkosManager.Infrastructure.Helpers
{
    public class ImportazioneHelper
    {
        private readonly ImportazioneSettings _settings;

        // ✔ Costruttore usato da Calkos.Web (DI + IOptions)
        public ImportazioneHelper(IOptions<ImportazioneSettings> settings)
        {
            /* .NET legge appsettings.json
               Trova la sezione "Importazione"
               La converte in un oggetto ImportazioneSettings
               La passa dentro IOptions<ImportazioneSettings>
               Tu la estrai con .Value */
            _settings = settings.Value; //Qui memorizziamo la configurazione letta da appsettings.json.
        }

        // ✔ Costruttore usato dalla Console (senza DI, senza IOptions)
        public ImportazioneHelper(ImportazioneSettings settings)
        {
            _settings = settings; // Qui ricevi direttamente la configurazione già letta dal JSON
        }

        public bool? ConvertiFatturare(string? valore)
        {
            if (string.IsNullOrWhiteSpace(valore))
                return null;

            var v = valore.Trim().ToUpper();

            // Per ogni elemento della lista, chiamato x, restituisci x.Trim().ToUpper().
            // Trim() → toglie spazi, ToUpper() → mette tutto in maiuscolo
            var trueValues = _settings.ValoriFatturareTrue
                .Select(x => x.Trim().ToUpper())
                .ToArray();

            if (trueValues.Contains(v))
                return true; // Se v (il valore Excel normalizzato) è dentro la lista → true
            else
                return false;
        }
    }
}
