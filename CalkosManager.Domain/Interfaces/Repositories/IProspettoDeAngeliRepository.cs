using CalkosManager.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace CalkosManager.Domain.Interfaces.Repositories
{
    public interface IProspettoDeAngeliRepository
    {
        // ============================================================
        // 1) VERSIONI STANDARD (senza connessione e transazione)
        // ------------------------------------------------------------
        // Usate da: Controller, UI, Operazioni manuali.
        // Aprono internamente la connessione.
        // ============================================================

        /// <summary>
        /// Inserisce più righe del prospetto De Angeli (operazioni manuali).
        /// </summary>
        void InsertRange(IEnumerable<ProspettoDeAngeli> items, string utente);

        /// <summary>
        /// Inserisce una singola riga del prospetto (DDT manuale).
        /// </summary>
        int Insert(ProspettoDeAngeli item, string utente);

        /// <summary>
        /// Aggiorna una riga del prospetto De Angeli.
        /// </summary>
        void Update(ProspettoDeAngeli item, string utente);

        /// <summary>
        /// Cancella FISICAMENTE una riga (Hard Delete).
        /// </summary>
        void Delete(int idProspettoDeAngeli);

        /// <summary>
        /// Esegue la Soft Delete (IsDeleted = 1) spostando la riga nel cestino.
        /// </summary>
        void SoftDelete(int id, string utente);

        /// <summary>
        /// Ripristina una riga dal cestino (IsDeleted = 0).
        /// </summary>
        void Ripristina(int id, string utente);


        // ============================================================
        // 2) LETTURA DATI
        // ============================================================

        /// <summary>
        /// Ritorna tutte le righe del prospetto per un file importato.
        /// </summary>
        IEnumerable<ProspettoDeAngeli> GetByFile(int idFileImportato);

        /// <summary>
        /// Ritorna una singola riga del prospetto De Angeli.
        /// </summary>
        ProspettoDeAngeli? GetById(int id);

        /// <summary>
        /// Ritorna tutte le righe del prospetto De Angeli filtrate per la visualizzazione.
        /// </summary>
        IEnumerable<ProspettoDeAngeli> GetFiltrati(int? idMandatario, int? idFile, int? anno, int? mese, int? fatturata,
            bool mostraEliminati = false, int? idTipoPagamento = null,
             int? idAgente = null
           , int? daFatturare = null);   // NUOVO 30/09/2026);//07/08/2026 idAgente


        // ============================================================
        // 3) VERSIONI TRANSAZIONALI (con connessione e transazione)
        // ------------------------------------------------------------
        // Usate SOLO da: ImportazioneDeAngeliService.
        // La connessione NON viene aperta internamente.
        // ============================================================

        /// <summary>
        /// Inserisce una riga del prospetto all’interno di una transazione esistente.
        /// </summary>
        ProspettoDeAngeli Insert(ProspettoDeAngeli item, SqlConnection conn, SqlTransaction tran, string utente);

        /// <summary>
        /// Inserisce più righe del prospetto all’interno di una transazione esistente.
        /// </summary>
        void InsertRange(IEnumerable<ProspettoDeAngeli> items, SqlConnection conn, SqlTransaction tran, string utente);

        /// <summary>
        /// Cancella una riga del prospetto all’interno di una transazione esistente.
        /// </summary>
        void Delete(int idProspettoDeAngeli, SqlConnection conn, SqlTransaction tran);


        // ============================================================
        // 4) LOGICA DI BUSINESS & FATTURAZIONE
        // ============================================================

        /// <summary>
        /// Imposta come fatturate le righe del prospetto De Angeli.
        /// </summary>
        void ImpostaComeFatturate(int idMandatario, int anno, int mese, int idCliente, int idTipoPagamento);

        /// <summary>
        /// Annulla lo stato di fatturazione di una riga specifica.
        /// </summary>
        bool ResetFatturazione(int idProspettoDeAngeli, string utente);

        /// <summary>
        /// Recupera la percentuale di provvigione spettante all'agente per uno specifico cliente.
        /// </summary>
        decimal GetPercentualeAgente(int idAgente, int idCliente, int idMandatario);


        // ==== MODIFICA 24/10/2026 - INIZIO ====
        bool DeleteSeNonFatturata(int id, int? idAgente);
        // ==== MODIFICA 24/10/2026 - FINE ====

    }
}