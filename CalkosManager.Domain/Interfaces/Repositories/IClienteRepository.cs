using CalkosManager.Domain.Entities;
using System.Data;

using CalkosManager.Domain;
using CalkosManager.Application.DTOs;


namespace CalkosManager.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Repository per la gestione dei clienti.
    /// </summary>
    public interface IClienteRepository
    {
        /// <summary>
        /// Restituisce un cliente tramite ID.
        /// </summary>
        Cliente? GetById(int id);

        /// <summary>
        /// Restituisce un cliente tramite Partita IVA (campo univoco).
        /// </summary>
        Cliente? GetByPartitaIva(string partitaIva);

        /// <summary>
        /// Restituisce un cliente tramite Codice Fiscale.
        /// </summary>
        Cliente? GetByCodiceFiscale(string codiceFiscale);

        /// <summary>
        /// Lookup per Ragione Sociale (dato proveniente dal file Excel Cobral)
        /// </summary>
        Cliente? GetByRagioneSociale(string ragioneSociale);

        /// <summary>
        /// Inserisce un nuovo cliente dentro una transazione esistente (usato nell'import Excel).
        /// </summary>
        Cliente Insert(Cliente cliente, IDbConnection conn, IDbTransaction tran);

        /// <summary>
        /// Inserimento manuale da interfaccia Web (senza transazione esterna).
        /// </summary>
        void InsertManual(Cliente cliente); // <--- AGGIUNTO: Risolve l'errore CS1061

        /// <summary>
        /// Seleziona tutti i clienti presenti in tabella.
        /// </summary>
        /// <param name="showDeleted">Se true restituisce i clienti archiviati (Cestino).</param>
        /// <param name="idAgente">
        /// 12/10/2026 Se valorizzato, restituisce SOLO i clienti associati a quell'agente
        /// (isolamento dati per il ruolo "Agente"). Se null, nessun filtro (Admin/Operatore).
        /// 30/10/2026 Per i clienti attivi "associati" = agente di default (Clienti.IdAgente) OPPURE
        /// presente in ProvvigioniAgenti; per il Cestino resta solo l'agente di default.
        /// </param>
        IEnumerable<Cliente> GetAll(bool showDeleted, int? idAgente = null); // 12/10/2026 aggiunto idAgente

        /// <summary>
        /// Seleziona tutti i clienti in base al Mandatario a cui sono associati.
        /// </summary>
        //List<Cliente> GetByMandatario(int idMandatario);
        // ==== MODIFICA 30/10/2026 - INIZIO ====
        //List<Cliente> GetByMandatario(int idMandatario, int? idClienteGiaIncluso = null);   // 30/10/2026 sostituita dalla riga sotto
        /// <param name="idAgente">
        /// 30/10/2026 Se valorizzato (ruolo Agente) restituisce solo i clienti dell'agente per quel mandatario:
        /// Clienti.IdAgente = idAgente OPPURE riga in ProvvigioniAgenti (agente + mandatario + cliente).
        /// Il cliente indicato in idClienteGiaIncluso viene comunque incluso. Null = nessun filtro.
        /// </param>
        List<Cliente> GetByMandatario(int idMandatario, int? idClienteGiaIncluso = null, int? idAgente = null);
        // ==== MODIFICA 30/10/2026 - FINE ====
        /// <summary>
        /// Inserisce nella tabella di collegamento MandatariClienti se non esiste già.
        /// </summary>
        void InsertMandatarioClienteIfNotExists(
            int idMandatario,
            int idCliente,
            IDbConnection connection,
            IDbTransaction transaction);

        /// <summary>
        /// Ricerca clienti filtrando per Ragione Sociale (Autocomplete).
        /// </summary>
        /// <param name="idAgente">
        /// 12/10/2026 Se valorizzato, la ricerca è limitata ai clienti di quell'agente. Se null, nessun filtro.
        /// 30/10/2026 "Clienti dell'agente" = agente di default OPPURE ProvvigioniAgenti.
        /// </param>
        IEnumerable<Cliente> SearchByRagioneSociale(string term, int maxResults = 20, int? idAgente = null); // 12/10/2026 aggiunto idAgente

        /// <summary>
        /// Aggiorna i dati di un cliente esistente.
        /// </summary>
        void Update(Cliente cliente);

        /// <summary>
        /// Elimina un cliente tramite ID.
        /// </summary>
        void Delete(int id);

        /// <summary>
        /// Verifica se il cliente ha ordini o relazioni prima della cancellazione.
        /// </summary>
        bool HasOrdersOrRelations(int id);


        /// <summary>
        /// Ripristina un cliente precedentemente rimosso tramite Soft Delete,
        /// impostando il flag IsDeleted a 0 tramite Stored Procedure.
        /// </summary>
        bool Restore(int id);
        Dictionary<int, int> GetMappaAgentiClienti(int idMandatario);//13/05/2026

        /// <summary>
        /// 26/08/2026 Recupera il Tipo Pagamento abituale (di default) di un cliente effettuando la JOIN con TipoPagamento.
        /// </summary>
        /// <param name="idCliente">Identificativo del cliente.</param>
        /// <returns>Il DTO con le informazioni di default oppure null se non presente.</returns>
        TipoPagamentoDefaultDto? GetTipoPagamentoDefault(int idCliente);

        /// <summary>
        /// 12/10/2026 Verifica se il cliente è associato all'agente indicato (Clienti.IdAgente = idAgente).
        /// Considera anche i clienti archiviati (IsDeleted = 1), perché serve anche per il ripristino dal Cestino.
        /// Usato per impedire all'Agente di leggere/modificare/eliminare clienti non propri.
        /// 30/10/2026 Verifica SOLO l'agente di default dell'anagrafica: resta il criterio per eliminare/ripristinare.
        /// Per accedere/lavorare sul cliente (anagrafica e ordini) usare IsClienteAccessibileAdAgente.
        /// </summary>
        bool IsClienteDiAgente(int idCliente, int idAgente);

        // ==== MODIFICA 30/10/2026 - INIZIO ====
        /// <summary>
        /// 30/10/2026 True se l'agente può lavorare sul cliente: Clienti.IdAgente = idAgente (agente di default)
        /// OPPURE esiste una riga in ProvvigioniAgenti per (agente, cliente).
        /// - idMandatario = null: qualsiasi mandatario (anagrafica clienti);
        /// - idMandatario valorizzato (ordini): le provvigioni contano solo per quel mandatario
        ///   e il cliente deve essere collegato al mandatario (MandatariClienti).
        /// Non filtra su IsDeleted. Le righe di ProvvigioniAgenti con IdCliente NULL non danno accesso.
        /// </summary>
        bool IsClienteAccessibileAdAgente(int idCliente, int idAgente, int? idMandatario = null);
        // ==== MODIFICA 30/10/2026 - FINE ====

        /// <summary>14/10/2026 Mandatario associato al cliente in MandatariClienti (null se assente).</summary>
        int? GetIdMandatarioByCliente(int idCliente);

        /// <summary>14/10/2026 Imposta/sostituisce/rimuove (null) il mandatario del cliente in MandatariClienti.</summary>
        void SetMandatarioCliente(int idCliente, int? idMandatario, string? utente);

        // ==== MODIFICA 30/10/2026 - INIZIO ====
        /// <summary>
        /// 30/10/2026 Crea un cliente per conto di un Agente in UNA transazione (spClienti_InsertDaAgente):
        /// controllo duplicati, Clienti (con IdAgenteCreatore), MandatariClienti e ProvvigioniAgenti
        /// con Agenti.PercentualeDefault. Non lancia eccezioni per i casi di business: li restituisce nell'esito.
        /// </summary>
        EsitoCreazioneClienteDto InsertDaAgente(Cliente cliente, int idAgente);

        /// <summary>
        /// 30/10/2026 Nomi simili per l'avviso mentre si digita (spClienti_GetSimili).
        /// idAgente null = Admin/Operatore (vedono tutto); valorizzato = ruolo Agente (dati dei clienti altrui oscurati).
        /// </summary>
        List<ClienteSimileDto> GetSimili(string term, int? idAgente, int max = 5);

        /// <summary>30/10/2026 Agente che ha creato il cliente (Clienti.IdAgenteCreatore), null se creato da Admin/Operatore.</summary>
        int? GetIdAgenteCreatore(int idCliente);
        // ==== MODIFICA 30/10/2026 - FINE ====
    }
}
