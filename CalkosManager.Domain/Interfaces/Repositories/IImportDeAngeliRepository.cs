using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using CalkosManager.Domain.Entities;

namespace CalkosManager.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Repository per la gestione della tabella ImportDeAngeli.
    /// Copia immutabile del file Excel De Angeli.
    /// </summary>
    public interface IImportDeAngeliRepository
    {
        /// <summary>
        /// Inserisce tutte le righe importate usando una transazione esistente.
        /// </summary>
        void InsertRange(IEnumerable<ImportDeAngeli> items, SqlConnection conn, SqlTransaction tran, string utente);

        /// <summary>
        /// Elimina le righe di un file specifico (utile per il ricaricamento) in transazione.
        /// </summary>
        void DeleteByFileImportatoId(int idFileImportato, SqlConnection conn, SqlTransaction tran, string utente);

        /// <summary>
        /// Recupera i dati grezzi associati a un caricamento.
        /// </summary>
        IEnumerable<ImportDeAngeli> GetByFile(int idFileImportato);

        /// <summary>
        /// Inserisce una singola riga in transazione.
        /// </summary>
        ImportDeAngeli Insert(ImportDeAngeli item, SqlConnection conn, SqlTransaction tran, string utente);

        /// <summary>
        /// Verifica se la riga è già stata importata in passato (Anti-duplicazione).
        /// </summary>
        bool ExistsUniqueKey(string uniqueKey, int idMandatario);
        /// <summary>
        /// Aggiorna lo stato isProcessed di elaborazione di una riga specifica. 12/04/2026
        /// </summary>
        void SetProcessedStatus(int idImportDeAngeli, bool isProcessed, SqlConnection conn, SqlTransaction tran, string utente);


    }
}