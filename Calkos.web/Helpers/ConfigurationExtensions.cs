using System;
using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Calkos.Web.Helpers
{
    public static class ConfigurationExtensions
    {
        public static DateTime GetDataAttivazioneModuloRitardo(this IConfiguration config, string nomeMandatario)
        {
            // Legge la data dal file di configurazione: es: "DataAttivazioneModuloRitardo:Cobral" = "2026-09-28"
            var dataStr = config[$"DataAttivazioneModuloRitardo:{nomeMandatario}"];

            // Se la chiave non esiste o è vuota → ritorna una data di sicurezza
            if (string.IsNullOrWhiteSpace(dataStr))
                return new DateTime(2026, 1, 1);

            /*
                ============================================================
                1) FORMATO ISO "yyyy-MM-dd" (NON ambiguo)
                   Esempi validi:
                     - "2026-09-28"
                     - "2024-01-05"
                   Questo formato è SEMPRE chiaro: anno-mese-giorno.
                ============================================================
            */
            if (DateTime.TryParseExact(dataStr, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                       DateTimeStyles.None, out var dataIso))
                return dataIso;

            /*
                ============================================================
                2) FORMATO ITALIANO "dd/MM/yyyy" (NON ambiguo)
                   Esempi validi:
                     - "28/09/2026"
                     - "05/01/2024"
                   Qui il separatore è "/", quindi NON può essere confuso
                   con il formato americano.
                ============================================================
            */
            if (DateTime.TryParseExact(dataStr, "dd/MM/yyyy", CultureInfo.GetCultureInfo("it-IT"),
                                       DateTimeStyles.None, out var dataIt))
                return dataIt;

            /*
                ============================================================
                3) FORMATO ITALIANO "dd-MM-yyyy" (leggermente ambiguo)
                   Esempi validi:
                     - "28-09-2026" → 28 settembre
                     - "05-01-2024" → 5 gennaio
                   ATTENZIONE:
                     - "09-11-2026" → 9 novembre (it-IT)
                       MA potrebbe essere interpretato diversamente in altre culture.
                   Qui lo accettiamo comunque, ma è meno sicuro del formato con "/".
                ============================================================
            */
            if (DateTime.TryParseExact(dataStr, "dd-MM-yyyy", CultureInfo.GetCultureInfo("it-IT"),
                                       DateTimeStyles.None, out var dataItDash))
                return dataItDash;

            /*
                ============================================================
                4) PARSING GENERICO (ULTIMO TENTATIVO)
                   Usa InvariantCulture, NON la cultura globale dell'applicazione.
                   Esempi che potrebbero funzionare:
                     - "September 28, 2026"
                     - "2026/09/28"
                     - "09/28/2026" (formato americano MM/dd/yyyy)
                   Questo è il fallback più rischioso, ma utile per evitare errori.
                ============================================================
            */
            if (DateTime.TryParse(dataStr, CultureInfo.InvariantCulture,
                                  DateTimeStyles.None, out var dataInv))
                return dataInv;

            /*
                ============================================================
                5) FALLBACK FINALE
                   Se la data è completamente invalida (es. "pippo", "data sbagliata"),
                   ritorna una data fissa per evitare crash.
                ============================================================
            */
            return new DateTime(2026, 1, 1);
        }

    }
}