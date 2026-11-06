
using CalkosManager.Domain.Entities;
using Microsoft.Data.SqlClient;

namespace CalkosManager.Domain.Interfaces.Repositories
{
    public interface IProspettoCobralRepository
    {
        // ============================================================
        // 1) VERSIONI STANDARD (senza connessione e transazione)
        // ------------------------------------------------------------
        // Usate da:
        // - Controller
        // - ProspettoCobralService
        // - UI
        // - Operazioni manuali (inserimento, modifica, duplicazione)
        //
        // Questi metodi APRONO internamente la connessione.
        // Sono semplici, comodi e identici a come funzionavano PRIMA.
        // ============================================================

        /// <summary>
        /// Inserisce più righe del prospetto (operazioni manuali).
        /// La connessione viene aperta internamente.
        /// </summary>
        void InsertRange(IEnumerable<ProspettoCobral> items, string utente);

        /// <summary>
        /// Inserisce una singola riga del prospetto (DDT manuale).
        /// </summary>
        int Insert(ProspettoCobral item, string utente);

        /// <summary>
        /// Aggiorna una riga del prospetto.
        /// </summary>
        void Update(ProspettoCobral item, string utente);

        /// <summary>
        /// Cancella FISICAMENTE una riga del prospetto (Hard Delete).
        /// Da usare con cautela.
        /// </summary>
        void Delete(int idProspettoCobral);

        /// <summary>
        /// [31/03/2026] Esegue la Soft Delete (IsDeleted = 1).
        /// Sposta la riga nel cestino rispettando i vincoli fiscali.
        /// </summary>
        void SoftDelete(int id, string utente);

        /// <summary>
        /// [31/03/2026] Ripristina una riga dal cestino (IsDeleted = 0).
        /// </summary>
        void Ripristina(int id, string utente);


        // ============================================================
        // 2) LETTURA DATI (sempre senza transazione)
        // ------------------------------------------------------------
        // Questi metodi NON modificano nulla, quindi non servono transazioni.
        // ============================================================

        /// <summary>
        /// Ritorna tutte le righe del prospetto per un file importato.
        /// </summary>
        IEnumerable<ProspettoCobral> GetByFile(int idFileImportato);

        /// <summary>
        /// Ritorna una singola riga del prospetto.
        /// </summary>
        ProspettoCobral? GetById(int id);

        /// <summary>
        /// Ritorna tutte le righe del prospetto.
        /// </summary>
        IEnumerable<ProspettoCobral> GetAll();

        /// <summary>
        /// Ritorna tutte le righe del prospetto filtrate.
        /// [31/03/2026] Aggiunto parametro mostraEliminati per la gestione del Cestino.
        /// </summary>
        IEnumerable<ProspettoCobral> GetFiltrati(int? idMandatario, int? idFile, int? anno, int? mese, int? fatturata,  
            bool mostraEliminati = false, int? idTipoPagamento = null, int? idAgente = null,
            int? daFatturare = null
            
            );   // NUOVO 30/09/2026)
                 // ;//07/08/2026 idAgente

        /// <summary>
        /// GetDimensioniMateriale
        /// </summary>
        List<string> GetDimensioniMateriale(int idMateriale, string tipo);


        // ============================================================
        // 3) VERSIONI TRANSAZIONALI (con connessione e transazione)
        // ------------------------------------------------------------
        // Usate  da:
        // - ImportazioneCobralService
        //
        // Perché l’import COBRAL deve:
        // - registrare il file
        // - salvare ImportCobral
        // - trasformare
        // - salvare ProspettoCobral
        // - fare tutto in UNA SOLA transazione
        //
        // Questi metodi NON aprono la connessione.
        // La connessione viene gestita dal servizio di importazione.
        // ============================================================

        /// <summary>
        /// Inserisce una riga del prospetto all’interno di una transazione.
        /// </summary>
        ProspettoCobral Insert(ProspettoCobral item, SqlConnection conn, SqlTransaction tran, string utente);

        /// <summary>
        /// Inserisce più righe del prospetto all’interno di una transazione.
        /// </summary>
        void InsertRange(IEnumerable<ProspettoCobral> items, SqlConnection conn, SqlTransaction tran, string utente);

        /// <summary>
        /// Cancella una riga del prospetto all’interno di una transazione.
        /// </summary>
        void Delete(int idProspettoCobral, SqlConnection conn, SqlTransaction tran);


        // ============================================================
        // 4) LOGICA DI BUSINESS & CALCOLI AGGIUNTIVI
        // ============================================================

        /// <summary>
        /// IMPOSTA COME FATTURATE LE RIGHE ORIGINALI DEL PROSPETTO COBRAL
        /// </summary>
        void ImpostaComeFatturate(int idMandatario, int anno, int mese, int idCliente, int idTipoPagamento);

        /// <summary>
        /// Recupera la percentuale di provvigione spettante all'agente per uno specifico cliente.
        /// </summary>
        decimal GetPercentualeAgente(int idAgente, int idCliente, int idMandatario);

        /// <summary>
        /// Cancella lo stato di fatturazione di tutte le righe del prospetto che corrispondono ai parametri specificati.
        /// </summary>
        bool ResetFatturazione(int idProspettoCobral, string utente);



        // ==== MODIFICA 24/10/2026 - INIZIO ====
        bool DeleteSeNonFatturata(int id, int? idAgente);
        // ==== MODIFICA 24/10/2026 - FINE ====

    }
}