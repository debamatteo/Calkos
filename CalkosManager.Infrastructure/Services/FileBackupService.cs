using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using CalkosManager.Application.Interfaces;

namespace CalkosManager.Infrastructure.Services
{
    public class FileBackupService : IFileBackupService
    {
        // --- PERCORSI ORIGINALI (NON TOCCARE) ---
        private readonly string _backupCobralBasePath;

        // --- AGGIUNTE NUOVI MANDATARI (11/04/2026) ---
        // Ogni mandatario ha il suo percorso privato caricato da appsettings.json
        private readonly string _backupDeAngeliBasePath;
        private readonly string _backupElektraWireBasePath;
        private readonly string _backupCSTBasePath;
        private readonly string _backupSystemcoreBasePath;
        private readonly string _backupSystemPBasePath;
        private readonly string _backupGuerzoniBasePath;
        private readonly string _backupTradingAndConsultingBasePath;
        private readonly string _backupHitechBasePath;
        private readonly string _backupSystemCoreBasePath;
        public FileBackupService(IConfiguration configuration)
        {
            // LOGICA COBRAL: Invariata
            _backupCobralBasePath = configuration["Percorsi:BackupCobral"]
                ?? throw new InvalidOperationException("Percorso 'Percorsi:BackupCobral' non configurato.");

            // INIZIALIZZAZIONE NUOVI PERCORSI:
            // Se uno di questi manca in appsettings.json, l'applicazione lancia errore all'avvio (Fail-Safe)
            _backupDeAngeliBasePath = configuration["Percorsi:BackupDeAngeli"]
                ?? throw new InvalidOperationException("Percorso 'Percorsi:BackupDeAngeli' non configurato.");

            _backupElektraWireBasePath = configuration["Percorsi:BackupElektraWire"]
                ?? throw new InvalidOperationException("Percorso 'Percorsi:BackupElektraWire' non configurato.");

            _backupCSTBasePath = configuration["Percorsi:BackupCST"]
                ?? throw new InvalidOperationException("Percorso 'Percorsi:BackupCST' non configurato.");

            _backupSystemcoreBasePath = configuration["Percorsi:BackupSystemcore"]
                ?? throw new InvalidOperationException("Percorso 'Percorsi:BackupSystemcore' non configurato.");

            _backupSystemPBasePath = configuration["Percorsi:BackupSystemP"]
                ?? throw new InvalidOperationException("Percorso 'Percorsi:BackupSystemP' non configurato.");

            _backupGuerzoniBasePath = configuration["Percorsi:BackupGuerzoni"]
                ?? throw new InvalidOperationException("Percorso 'Percorsi:BackupGuerzoni' non configurato.");

            _backupTradingAndConsultingBasePath = configuration["Percorsi:BackupTradingAndConsulting"]
                ?? throw new InvalidOperationException("Percorso 'Percorsi:BackupTradingAndConsulting' non configurato.");

            _backupHitechBasePath = configuration["Percorsi:BackupHitech"]
                ?? throw new InvalidOperationException("Percorso 'Percorsi:BackupHitech' non configurato.");
        }

        // ============================================================
        // 1) METODO ORIGINALE COBRAL (NON TOCCATO)
        // ============================================================
        public async Task<string> BackupCobralAsync(string sourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
                throw new FileNotFoundException("File COBRAL non trovato.", sourceFilePath);

            var now = DateTime.Now;
            var yearFolder = Path.Combine(_backupCobralBasePath, now.Year.ToString());
            var monthFolder = Path.Combine(yearFolder, now.Month.ToString("00"));

            Directory.CreateDirectory(monthFolder);

            var timestamp = now.ToString("yyyyMMdd_HHmmss");
            var extension = Path.GetExtension(sourceFilePath);
            var backupFileName = $"COBRAL_{timestamp}{extension}";
            var backupFullPath = Path.Combine(monthFolder, backupFileName);

            using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
            using (var destStream = new FileStream(backupFullPath, FileMode.Create))
            {
                await sourceStream.CopyToAsync(destStream);
            }

            return backupFullPath;
        }

        // ============================================================
        // 2) AGGIUNTE NUOVI MANDATARI (METODI SEPARATI E INDIPENDENTI)
        // ============================================================

        // Metodo specifico DeAngeli
        public async Task<string> BackupDeAngeliAsync(string sourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
                throw new FileNotFoundException("File DE ANGELI non trovato.", sourceFilePath);

            var now = DateTime.Now;
            var monthFolder = Path.Combine(_backupDeAngeliBasePath, now.Year.ToString(), now.Month.ToString("00"));
            Directory.CreateDirectory(monthFolder);

            var backupFullPath = Path.Combine(monthFolder, $"DEANGELI_{now:yyyyMMdd_HHmmss}{Path.GetExtension(sourceFilePath)}");

            using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
            using (var destStream = new FileStream(backupFullPath, FileMode.Create))
            {
                await sourceStream.CopyToAsync(destStream);
            }
            return backupFullPath;
        }

        // Metodo specifico Elektra Wire
        public async Task<string> BackupElektraWireAsync(string sourceFilePath)
        {
            var now = DateTime.Now;
            var monthFolder = Path.Combine(_backupElektraWireBasePath, now.Year.ToString(), now.Month.ToString("00"));
            Directory.CreateDirectory(monthFolder);
            var backupFullPath = Path.Combine(monthFolder, $"ELEKTRAWIRE_{now:yyyyMMdd_HHmmss}{Path.GetExtension(sourceFilePath)}");

            using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
            using (var destStream = new FileStream(backupFullPath, FileMode.Create))
            {
                await sourceStream.CopyToAsync(destStream);
            }
            return backupFullPath;
        }

        // Metodo specifico CST
        public async Task<string> BackupCSTAsync(string sourceFilePath)
        {
            var now = DateTime.Now;
            var monthFolder = Path.Combine(_backupCSTBasePath, now.Year.ToString(), now.Month.ToString("00"));
            Directory.CreateDirectory(monthFolder);
            var backupFullPath = Path.Combine(monthFolder, $"CST_{now:yyyyMMdd_HHmmss}{Path.GetExtension(sourceFilePath)}");

            using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
            using (var destStream = new FileStream(backupFullPath, FileMode.Create))
            {
                await sourceStream.CopyToAsync(destStream);
            }
            return backupFullPath;
        }

        // Metodo specifico Systemcore
        public async Task<string> BackupSystemcoreAsync(string sourceFilePath)
        {
            var now = DateTime.Now;
            var monthFolder = Path.Combine(_backupSystemcoreBasePath, now.Year.ToString(), now.Month.ToString("00"));
            Directory.CreateDirectory(monthFolder);
            var backupFullPath = Path.Combine(monthFolder, $"SYSTEMCORE_{now:yyyyMMdd_HHmmss}{Path.GetExtension(sourceFilePath)}");

            using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
            using (var destStream = new FileStream(backupFullPath, FileMode.Create))
            {
                await sourceStream.CopyToAsync(destStream);
            }
            return backupFullPath;
        }

        // Metodo specifico SystemP
        public async Task<string> BackupSystemPAsync(string sourceFilePath)
        {
            var now = DateTime.Now;
            var monthFolder = Path.Combine(_backupSystemPBasePath, now.Year.ToString(), now.Month.ToString("00"));
            Directory.CreateDirectory(monthFolder);
            var backupFullPath = Path.Combine(monthFolder, $"SYSTEMP_{now:yyyyMMdd_HHmmss}{Path.GetExtension(sourceFilePath)}");

            using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
            using (var destStream = new FileStream(backupFullPath, FileMode.Create))
            {
                await sourceStream.CopyToAsync(destStream);
            }
            return backupFullPath;
        }

        // Metodo specifico Guerzoni
        public async Task<string> BackupGuerzoniAsync(string sourceFilePath)
        {
            var now = DateTime.Now;
            var monthFolder = Path.Combine(_backupGuerzoniBasePath, now.Year.ToString(), now.Month.ToString("00"));
            Directory.CreateDirectory(monthFolder);
            var backupFullPath = Path.Combine(monthFolder, $"GUERZONI_{now:yyyyMMdd_HHmmss}{Path.GetExtension(sourceFilePath)}");

            using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
            using (var destStream = new FileStream(backupFullPath, FileMode.Create))
            {
                await sourceStream.CopyToAsync(destStream);
            }
            return backupFullPath;
        }

        // Metodo specifico Trading And Consulting
        public async Task<string> BackupTradingAndConsultingAsync(string sourceFilePath)
        {
            var now = DateTime.Now;
            var monthFolder = Path.Combine(_backupTradingAndConsultingBasePath, now.Year.ToString(), now.Month.ToString("00"));
            Directory.CreateDirectory(monthFolder);
            var backupFullPath = Path.Combine(monthFolder, $"TRADING_{now:yyyyMMdd_HHmmss}{Path.GetExtension(sourceFilePath)}");

            using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
            using (var destStream = new FileStream(backupFullPath, FileMode.Create))
            {
                await sourceStream.CopyToAsync(destStream);
            }
            return backupFullPath;
        }

        // Metodo specifico Hitech
        public async Task<string> BackupHitechAsync(string sourceFilePath)
        {
            var now = DateTime.Now;
            var monthFolder = Path.Combine(_backupHitechBasePath, now.Year.ToString(), now.Month.ToString("00"));
            Directory.CreateDirectory(monthFolder);
            var backupFullPath = Path.Combine(monthFolder, $"HITECH_{now:yyyyMMdd_HHmmss}{Path.GetExtension(sourceFilePath)}");

            using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
            using (var destStream = new FileStream(backupFullPath, FileMode.Create))
            {
                await sourceStream.CopyToAsync(destStream);
            }
            return backupFullPath;
        }



        // Metodo specifico SystemCore BackupSystemCoreAsync
        public async Task<string> BackupSystemCoreAsync(string sourceFilePath)
        {
            var now = DateTime.Now;
            var monthFolder = Path.Combine(_backupSystemCoreBasePath, now.Year.ToString(), now.Month.ToString("00"));
            Directory.CreateDirectory(monthFolder);
            var backupFullPath = Path.Combine(monthFolder, $"SystemCore_{now:yyyyMMdd_HHmmss}{Path.GetExtension(sourceFilePath)}");

            using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
            using (var destStream = new FileStream(backupFullPath, FileMode.Create))
            {
                await sourceStream.CopyToAsync(destStream);
            }
            return backupFullPath;
        }







    }
}