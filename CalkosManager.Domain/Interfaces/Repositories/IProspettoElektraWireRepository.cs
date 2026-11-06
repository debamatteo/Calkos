using CalkosManager.Domain.Entities;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace CalkosManager.Domain.Interfaces.Repositories
{
    public interface IProspettoElektraWireRepository
    {
        // ============================================================
        // 1) VERSIONI STANDARD (senza connessione e transazione)
        // ------------------------------------------------------------
        // Usate da: Controller, UI, Operazioni manuali.
        // Questi metodi APRONO internamente la connessione.
        // ============================================================

        /// <summary>
        /// Inserisce più righe del prospetto (operazioni manuali).
        /// </summary>
        void InsertRange(IEnumerable<ProspettoElektraWire> items, string utente);

        /// <summary>
        /// Inserisce una singola riga del prospetto.
        /// </summary>
        int Insert(ProspettoElektraWire item, string utente);

        /// <summary>
        /// Aggiorna una riga del prospetto ElektraWire.
        /// </summary>
        void Update(ProspettoElektraWire item, string utente);

        /// <summary>
        /// Cancella FISICAMENTE una riga del prospetto (Hard Delete).
        /// </summary>
        void Delete(int idProspettoElektraWire);

        /// <summary>
        /// Esegue la Soft Delete (IsDeleted = 1).
        /// </summary>
        void SoftDelete(int id, string utente);

        /// <summary>
        /// Ripristina una riga dal cestino (IsDeleted = 0).
        /// </summary>
        void Ripristina(int id, string utente);


        // ============================================================
        // 2) LETTURA DATI (sempre senza transazione)
        // ============================================================

        /// <summary>
        /// Ritorna tutte le righe del prospetto per un file importato.
        /// </summary>
        IEnumerable<ProspettoElektraWire> GetByFile(int idFileImportato);

        /// <summary>
        /// Ritorna una singola riga del prospetto.
        /// </summary>
        ProspettoElektraWire? GetById(int id);

        /// <summary>
        /// Ritorna tutte le righe del prospetto filtrate.
        /// </summary>
        IEnumerable<ProspettoElektraWire> GetFiltrati(int? idMandatario, int? idFile, int? anno, int? mese, int? fatturata, 
            bool mostraEliminati = false, int? idTipoPagamento = null, 
            int? idAgente = null
             , int? daFatturare = null);   // NUOVO 30/09/2026);//07/08/2026 idAgente



        // ============================================================
        // 3) VERSIONI TRANSAZIONALI (con connessione e transazione)
        // ------------------------------------------------------------
        // Usate SOLO da: ImportazioneElektraWireService.
        // La connessione viene gestita dal servizio di importazione.
        // ============================================================

        /// <summary>
        /// Inserisce una riga del prospetto all’interno di una transazione.
        /// </summary>
        ProspettoElektraWire Insert(ProspettoElektraWire item, SqlConnection conn, SqlTransaction tran, string utente);

        /// <summary>
        /// Inserisce più righe del prospetto all’interno di una transazione.
        /// </summary>
        void InsertRange(IEnumerable<ProspettoElektraWire> items, SqlConnection conn, SqlTransaction tran, string utente);

        /// <summary>
        /// Cancella una riga del prospetto all’interno di una transazione.
        /// </summary>
        void Delete(int idProspettoElektraWire, SqlConnection conn, SqlTransaction tran);


        // ============================================================
        // 4) LOGICA DI BUSINESS & CALCOLI AGGIUNTIVI
        // ============================================================

        /// <summary>
        /// Imposta come fatturate le righe del prospetto ElektraWire.
        /// </summary>
        void ImpostaComeFatturate(int idMandatario, int anno, int mese, int idCliente, int idTipoPagamento);

        /// <summary>
        /// Recupera la percentuale di provvigione spettante all'agente.
        /// </summary>
        decimal GetPercentualeAgente(int idAgente, int idCliente, int idMandatario);

        /// <summary>
        /// Cancella lo stato di fatturazione.
        /// </summary>
        bool ResetFatturazione(int idProspettoElektraWire, string utente);


        // ==== MODIFICA 24/10/2026 - INIZIO ====
        bool DeleteSeNonFatturata(int id, int? idAgente);
        // ==== MODIFICA 24/10/2026 - FINE ====
    }
}