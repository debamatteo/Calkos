using System.Reflection;
using System.Text;
using System.Globalization;
using Microsoft.Extensions.Configuration;
/*
 * UNIQUE KEY GENERATOR – COME FUNZIONA E COSA DEVI SAPERE
 * --------------------------------------------------------
 * Questo servizio crea una “chiave univoca” per ogni riga importata da Excel.
 * La chiave serve per riconoscere i duplicati ed evitare di importare due volte
 * la stessa riga.
 *
 * ⚠️ IMPORTANTE: I nomi delle proprietà usate per la chiave NON sono i nomi
 * delle colonne Excel, ma i nomi delle proprietà della classe C# (es. ImportCobral).
 *
 * --------------------------------------------------------
 * CONFIGURAZIONE (appsettings.json)
 * --------------------------------------------------------
 * Per ogni mandatario si definiscono le proprietà che identificano una riga.
 * Esempio:
 *
 * "MandatariUniqueKeys": {
 *   "Cobral": [ "OrdineDDT", "Cliente", "Kg", "Materiale" ]
 * }
 *
 * Questi nomi devono corrispondere esattamente alle proprietà della classe
 * ImportCobral (o della classe T passata al metodo Generate<T>).
 *
 * --------------------------------------------------------
 * COME VIENE GENERATA LA CHIAVE
 * --------------------------------------------------------
 * 1. Legge dal file di configurazione la lista delle proprietà per il mandatario.
 * 2. Se la configurazione è vuota, usa un fallback: "OrdineDDT" e "Cliente".
 * 3. Per ogni proprietà indicata:
 *      - legge il valore tramite Reflection
 *      - normalizza il valore:
 *          • Date → "yyyy-MM-dd"
 *          • Decimal → formato invariant con punto decimale
 *          • Altri tipi → ToString()
 * 4. Costruisce la chiave nel formato:
 *
 *      <Mandatario>|Anno=XXXX|NomeProprietà=Valore|NomeProprietà=Valore|...
 *
 * 5. L’anno (se fornito dall’utente) viene aggiunto per evitare duplicati
 *    tra anni diversi quando i numeri d’ordine si ripetono.
 *
 * --------------------------------------------------------
 * ESEMPIO DI RISULTATO
 * --------------------------------------------------------
 *   Cobral|Anno=2026|OrdineDDT=12345|Cliente=Rossi|Kg=100|Materiale=Ottone|
 *
 * --------------------------------------------------------
 * SCOPO DELLA CHIAVE
 * --------------------------------------------------------
 * - Evitare importazioni duplicate.
 * - Permettere configurazioni diverse per ogni mandatario senza modificare il codice.
 * - Rendere il sistema flessibile e adattabile a nuovi formati Excel.
 */


namespace CalkosManager.Application.Services
{
    /// <summary>
    /// Servizio preposto alla generazione di identificativi univoci (UniqueKey) per le righe di importazione.
    /// Utilizza la Reflection per estrarre dinamicamente i valori dalle entità in base alla configurazione.
    /// </summary>
    public class UniqueKeyGenerator
    {
        private readonly IConfiguration _configuration;

        public UniqueKeyGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        /// Genera una stringa hash-like basata sulle proprietà configurate per lo specifico mandatario.
        /// <typeparam name="T">Il tipo dell'entità di import (es. ImportCobral, ImportDeAngeli).</typeparam>
        /// <param name="riga">L'istanza della riga da processare.</param>
        /// <param name="nomeMandatarioLogico">Il nome del mandatario usato come chiave nel file di configurazione.</param>
        /// <returns>Una stringa formattata contenente i valori chiave per il controllo dei duplicati.</returns>
        /// Il metodo risolve i nomi delle proprietà definiti in appsettings.json sotto la sezione 'MandatariUniqueKeys'.
        /// Se la configurazione è assente, applica un fallback sui campi standard 'OrdineDDT' e 'Cliente'.
        public string Generate<T>(T riga, string nomeMandatarioLogico, int? anno = null) where T : class
        {
            // 1. Recupero dei nomi delle proprietà configurate per il mandatario specifico
            // Esempio JSON: "MandatariUniqueKeys": { "DeAngeli": ["OrdineDDT", "Cliente", "Kg"] }
            var section = _configuration.GetSection($"MandatariUniqueKeys:{nomeMandatarioLogico}");
            var propertyNames = section.Get<string[]>() ?? Array.Empty<string>();

            // 2. Fallback di sicurezza nel caso di configurazione mancante o errata
            if (propertyNames.Length == 0)
            {
                propertyNames = new[] { "OrdineDDT", "Cliente" };
            }

            var sb = new StringBuilder();

            // Prefisso per distinguere chiavi tra diversi mandatari che potrebbero avere dati simili
            sb.Append(nomeMandatarioLogico);
            sb.Append("|");

            // --------------------------------------------------------------------------------
            // 12/04/2026 INIEZIONE ANNO DI COMPETENZA
            // Se l'anno viene fornito (dalla scelta utente nell'interfaccia), lo inseriamo come 
            // primo elemento della chiave. Questo risolve il problema del riciclo dei numeri 
            // d'ordine che avviene tipicamente ad ogni nuovo anno solare.
            // --------------------------------------------------------------------------------
            if (anno.HasValue)
            {
                sb.Append("Anno=");
                sb.Append(anno.Value);
                sb.Append("|");
            }

            // Acquisizione del tipo a runtime per l'ispezione tramite Reflection
            var type = typeof(T);

            foreach (var propName in propertyNames)
            {
                // Ricerca della proprietà con case-insensitivity per tolleranza agli errori di configurazione
                var prop = type.GetProperty(
                    propName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
                );

                // Se la proprietà configurata non esiste nell'oggetto corrente, viene ignorata
                if (prop == null)
                    continue;

                var value = prop.GetValue(riga);

                // 3. Normalizzazione del valore per garantire determinismo nella chiave
                // - Date: Formato ISO (yyyy-MM-dd) per evitare problemi di localizzazione
                // - Decimal: Formato Invariant (punto decimale) e arrotondamento a 3 decimali
                string valueString = value switch
                {
                    null => "",
                    DateTime dt => dt.ToString("yyyy-MM-dd"),
                    decimal dec => dec.ToString("0.###", CultureInfo.InvariantCulture),
                    _ => value.ToString() ?? ""
                };

                // Composizione della chiave: NomeProprietà=Valore|
                sb.Append(propName);
                sb.Append("=");
                sb.Append(valueString.Trim());
                sb.Append("|");
            }

            // [GEMINI] Esempio Risultato: "Cobral|Anno=2026|NOrdineFattura=123|Cliente=Rossi|"
            return sb.ToString();
        }

        // VERSIO NE SENZA ANNO 
        //public string Generate<T>(T riga, string nomeMandatarioLogico) where T : class
        //{
        //    // 1. Recupero dei nomi delle proprietà configurate per il mandatario specifico
        //    // Esempio JSON: "MandatariUniqueKeys": { "DeAngeli": ["OrdineDDT", "Cliente", "Kg"] }
        //    var section = _configuration.GetSection($"MandatariUniqueKeys:{nomeMandatarioLogico}");
        //    var propertyNames = section.Get<string[]>() ?? Array.Empty<string>();

        //    // 2. Fallback di sicurezza nel caso di configurazione mancante o errata
        //    if (propertyNames.Length == 0)
        //    {
        //        propertyNames = new[] { "OrdineDDT", "Cliente" };
        //    }

        //    var sb = new StringBuilder();

        //    // Prefisso per distinguere chiavi tra diversi mandatari che potrebbero avere dati simili
        //    sb.Append(nomeMandatarioLogico);
        //    sb.Append("|");

        //    // Acquisizione del tipo a runtime per l'ispezione tramite Reflection
        //    var type = typeof(T);

        //    foreach (var propName in propertyNames)
        //    {
        //        // Ricerca della proprietà con case-insensitivity per tolleranza agli errori di configurazione
        //        var prop = type.GetProperty(
        //            propName,
        //            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase
        //        );

        //        // Se la proprietà configurata non esiste nell'oggetto corrente, viene ignorata
        //        if (prop == null)
        //            continue;

        //        var value = prop.GetValue(riga);

        //        // 3. Normalizzazione del valore per garantire determinismo nella chiave
        //        // - Date: Formato ISO (yyyy-MM-dd) per evitare problemi di localizzazione
        //        // - Decimal: Formato Invariant (punto decimale) e arrotondamento a 3 decimali
        //        string valueString = value switch
        //        {
        //            null => "",
        //            DateTime dt => dt.ToString("yyyy-MM-dd"),
        //            decimal dec => dec.ToString("0.###", CultureInfo.InvariantCulture),
        //            _ => value.ToString() ?? ""
        //        };

        //        // Composizione della chiave: NomeProprietà=Valore|
        //        sb.Append(propName);
        //        sb.Append("=");
        //        sb.Append(valueString.Trim());
        //        sb.Append("|");
        //    }

        //    return sb.ToString();
        //}
    }
}

