using System.Threading.Tasks;

namespace CalkosManager.Application.Interfaces
{
    /// <summary>
    /// Servizio responsabile del backup fisico dei file per i vari mandatari.
    /// Ogni mandatario ha il suo metodo dedicato per garantire l'isolamento dei processi.
    /// </summary>
    public interface IFileBackupService
    {
        /// <summary>
        /// Esegue il backup del file COBRAL (Logica originale).
        /// </summary>
        Task<string> BackupCobralAsync(string sourceFilePath);

        // --- AGGIUNTE NUOVI MANDATARI (11/04/2026) ---

        /// <summary>
        /// Esegue il backup del file DE ANGELI.
        /// </summary>
        Task<string> BackupDeAngeliAsync(string sourceFilePath);

        /// <summary>
        /// Esegue il backup del file ELEKTRA WIRE.
        /// </summary>
        Task<string> BackupElektraWireAsync(string sourceFilePath);

        /// <summary>
        /// Esegue il backup del file CST.
        /// </summary>
        Task<string> BackupCSTAsync(string sourceFilePath);

        /// <summary>
        /// Esegue il backup del file SYSTEMCORE.
        /// </summary>
        Task<string> BackupSystemcoreAsync(string sourceFilePath);

        /// <summary>
        /// Esegue il backup del file SYSTEMP.
        /// </summary>
        Task<string> BackupSystemPAsync(string sourceFilePath);

        /// <summary>
        /// Esegue il backup del file GUERZONI.
        /// </summary>
        Task<string> BackupGuerzoniAsync(string sourceFilePath);

        /// <summary>
        /// Esegue il backup del file TRADING AND CONSULTING.
        /// </summary>
        Task<string> BackupTradingAndConsultingAsync(string sourceFilePath);

        /// <summary>
        /// Esegue il backup del file HITECH.
        /// </summary>
        Task<string> BackupHitechAsync(string sourceFilePath);
    }
}