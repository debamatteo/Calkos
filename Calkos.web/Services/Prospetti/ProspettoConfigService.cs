using Calkos.web.Models.Prospetti;
//using Microsoft.Extensions.Hosting;private readonly IHostEnvironment _hostEnvironment; public ProspettoConfigService(IHostEnvironment hostEnvironment)
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Text.Json;

namespace Calkos.web.Services.Prospetti
{
    /// <summary>
    /// Servizio che legge i file di configurazione JSON dei prospetti
    /// (es. Config\Prospetti\cobral.json) e li deserializza in oggetti C#.
    /// </summary>
    public class ProspettoConfigService
    {
        private readonly IWebHostEnvironment _env;
        //private readonly IHostEnvironment _hostEnvironment;

        /// <summary>
        /// Il costruttore riceve l'ambiente host per poter calcolare il percorso fisico
        /// della cartella Config\Prospetti a partire dalla root dell'applicazione.
        /// </summary>
        //public ProspettoConfigService(IHostEnvironment hostEnvironment)
        //{
        //    _hostEnvironment = hostEnvironment;
        //}
        public ProspettoConfigService(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Carica la configurazione di un prospetto a partire dal suo nome logico
        /// (es. "cobral" → Config\Prospetti\cobral.json).
        /// </summary>
        /// <param name="nomeProspetto">
        /// Nome del prospetto, usato come nome file (senza estensione).
        /// Esempio: "cobral", "deangeli", "guerzoni".
        /// </param>
        /// <returns>
        /// Oggetto ProspettoConfig con l'elenco delle colonne configurate.
        /// </returns>
        public ProspettoConfig Load(string nomeProspetto)
        {
            if (string.IsNullOrWhiteSpace(nomeProspetto))
                throw new ArgumentException("Il nome del prospetto non può essere vuoto.", nameof(nomeProspetto));

            // 1. Costruisco il percorso fisico del file JSON.
            //    BasePath = root dell'app (Calkos.web)
            //    Config\Prospetti\<nome>.json
            //var basePath = _hostEnvironment.ContentRootPath;
            var basePath = _env.ContentRootPath;


            var configFolder = Path.Combine(basePath, "Config", "Prospetti");
            var filePath = Path.Combine(configFolder, $"{nomeProspetto}.json");

            if (!File.Exists(filePath))
            {
                // Qui NON invento fallback: se il file non esiste, lo segnalo chiaramente.
                throw new FileNotFoundException(
                    $"File di configurazione del prospetto non trovato: {filePath}");
            }

            // 2. Leggo il contenuto del file JSON.
            var json = File.ReadAllText(filePath);

            // 3. Opzioni di deserializzazione:
            //    - PropertyNameCaseInsensitive: true → accetta "columns" o "Columns"
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // 4. Deserializzo il JSON in ProspettoConfig.
            var config = JsonSerializer.Deserialize<ProspettoConfig>(json, options);

            if (config == null)
            {
                throw new InvalidOperationException(
                    $"Impossibile deserializzare la configurazione del prospetto: {filePath}");
            }

            return config;
        }
    }
}
