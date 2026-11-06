using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using CalkosManager.Domain.Entities;

namespace CalkosManager.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Repository per la gestione della tabella ImportHitech.
    /// Questa tabella rappresenta la copia immutabile del file Excel importato.
    /// Nessuna logica, nessun calcolo, nessun lookup.
    /// </summary>
    public interface IImportHitechRepository
    {
        //// -----------------------------
        //// Versione standard
        //// -----------------------------

        ///// <summary>
        ///// Inserisce tutte le righe importate dal file Excel.
        ///// </summary>
        //void InsertRange(IEnumerable<ImportHitech> items);

        ///// <summary>
        ///// Elimina tutte le righe associate a un file importato.
        ///// </summary>
        //void DeleteByFileImportatoId(int idFileImportato);

        // -----------------------------
        // Versione transazionale
        // -----------------------------

        /// <summary>
        /// Inserisce tutte le righe usando una connessione e una transazione esistenti.
        /// </summary>
        void InsertRange(IEnumerable<ImportHitech> items, SqlConnection conn, SqlTransaction tran, string utente);

        /// <summary>
        /// Elimina tutte le righe usando una connessione e una transazione esistenti.
        /// </summary>
        void DeleteByFileImportatoId(int idFileImportato, SqlConnection conn, SqlTransaction tran, string utente);

        /// <summary>
        /// Restituisce tutte le righe associate a un file importato.
        /// </summary>
        IEnumerable<ImportHitech> GetByFile(int idFileImportato);

        /// <summary>
        /// Inserisce una sola riga usando una connessione e una transazione esistenti.
        /// </summary>
        ImportHitech Insert(ImportHitech item, SqlConnection conn, SqlTransaction tran, string utente);

        /// <summary>
        /// metodo per verificare se esiste già una riga con la stessa chiave unica (UniqueKey) per lo stesso mandatario (idMandatario).
        /// </summary>
        bool ExistsUniqueKey(string uniqueKey, int idMandatario);

        /// <summary>
        /// Aggiorna lo stato isProcessed di elaborazione di una riga specifica. 12/04/2026
        /// </summary>
        void SetProcessedStatus(int idImportHitech, bool isProcessed, SqlConnection conn, SqlTransaction tran, string utente);

    }
}
