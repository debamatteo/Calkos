using CalkosManager.Application.DTOs;
using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Infrastructure.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;
/*
    ============================================================
    CLIENTE REPOSITORY — PROMEMORIA ARCHITETTURALE
    ============================================================

    Questo repository è "ibrido" per scelta architetturale.

    ✔ Le operazioni CRUD standard (GetById, GetByPartitaIva, GetByCodiceFiscale)
      utilizzano STORED PROCEDURE.
      - Motivo: coerenza con il resto del progetto, sicurezza, stabilità,
        nessuna duplicazione della logica SQL, manutenzione più semplice.

    ✔ Alcune operazioni utilizzano SQL INLINE:
        - GetByRagioneSociale()
        - Insert() usato durante l’importazione Excel
        - InsertMandatarioClienteIfNotExists () usato durante l’importazione Excel

      Motivo:
      - Questi metodi vengono utilizzati nel flusso di importazione COBRAL,
        che lavora con UNA SOLA connessione e UNA SOLA transazione.
      - Le stored CRUD NON possono essere usate dentro l’import perché
        aprirebbero connessioni proprie e romperebbero l’atomicità.
      - SQL inline garantisce flessibilità, velocità e controllo totale
        della transazione.

    ✔ NESSUN AddWithValue:
      - Evita conversioni implicite NVARCHAR(4000)
      - Evita index scan e rallentamenti
      - Parametri sempre tipizzati con SqlDbType

    ✔ Lettura dei dati tramite SqlReaderExtensions:
      - GetStringSafe, GetIntSafe, GetNullableInt, ecc.
      - Zero DBNull, zero eccezioni, codice più pulito e coerente.

    ✔ Insert() transazionale:
      - Usa la connessione e la transazione passate dal service
      - NON apre connessioni proprie
      - Fa parte del flusso atomico dell’import Excel

    In sintesi:
    - CRUD → Stored Procedure
    - Import Excel → SQL Inline + Transazione
    - Questo repository è progettato per funzionare correttamente in entrambi i contesti.
    ============================================================
*/

namespace CalkosManager.Infrastructure.Repositories
{
    public class ClienteRepository : IClienteRepository
    {
        private readonly string _connectionString;

        public ClienteRepository(string connectionString)
        {
            _connectionString = connectionString;
            Console.WriteLine(">>> NUOVA ISTANZA ClienteRepository");
        }

        public Dictionary<int, int> GetMappaAgentiClienti(int idMandatario)//13/05/2026
        {
            var dizionario = new Dictionary<int, int>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // Usiamo ProvvigioniAgenti perché è lì che hai il legame Agente-Cliente-Mandatario
            const string sql = "SELECT IdCliente, IdAgente FROM ProvvigioniAgenti WHERE IdMandatario = @id AND IdCliente IS NOT NULL";

            using var cmd = new SqlCommand(sql, conn);
            // ==== MODIFICA 30/10/2026 - INIZIO ====
            //cmd.Parameters.AddWithValue("@id", idMandatario);   // 30/10/2026 sostituito: parametro tipizzato (regola "nessun AddWithValue")
            cmd.Parameters.Add("@id", SqlDbType.Int).Value = idMandatario;
            // ==== MODIFICA 30/10/2026 - FINE ====

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                // Se un cliente ha più agenti, l'ultimo sovrascrive (evita errori di chiave duplicata)
                dizionario[reader.GetInt32(0)] = reader.GetInt32(1);
            }
            return dizionario;
        }




        //lookup filtro autocomplete per ragione sociale, usato nella form di inserimento/modifica cliente
        // 12/10/2026 aggiunto parametro idAgente: se valorizzato (ruolo Agente) la ricerca è limitata ai suoi clienti
        public IEnumerable<Cliente> SearchByRagioneSociale(string term, int maxResults = 20, int? idAgente = null)
        {

            // Aggiunto IsDeleted = 0  -- Mostriamo solo i clienti attivi nell'interfaccia
            // ==== MODIFICA 30/10/2026 - INIZIO ====
            // Il filtro agente ora considera l'agente di default (Clienti.IdAgente) E gli accordi in ProvvigioniAgenti,
            // tramite la funzione dbo.fnClientiDellAgente (mandatario NULL = qualsiasi mandatario).
            // Prima: AND (@IdAgente IS NULL OR IdAgente = @IdAgente)
            const string sql = @"
                SELECT TOP (@MaxResults)
                IdCliente,
                RagioneSociale,
                CodiceFiscale,
                PartitaIva,
                Indirizzo,
                Citta,
                Provincia,
                CAP,
                Email,
                Telefono,
                Note,
                IdAgente
                FROM Clienti
                WHERE RagioneSociale LIKE @Term
                AND IsDeleted = 0
                AND (@IdAgente IS NULL OR IdCliente IN (SELECT f.IdCliente FROM dbo.fnClientiDellAgente(@IdAgente, NULL) f)) -- 30/10/2026 isolamento per Agente (NULL = nessun filtro)
                ORDER BY RagioneSociale;
                ";
            // ==== MODIFICA 30/10/2026 - FINE ====

            var list = new List<Cliente>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(sql, conn);

                // Parametri tipizzati
                cmd.Parameters.Add("@MaxResults", SqlDbType.Int).Value = maxResults;
                cmd.Parameters.Add("@Term", SqlDbType.NVarChar).Value = term + "%";
                // 12/10/2026 filtro per agente (DBNull = Admin/Operatore, vedono tutti i clienti)
                cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = (object?)idAgente ?? DBNull.Value;

                conn.Open();
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(new Cliente
                    {
                        IdCliente = reader.GetIntSafe("IdCliente"),
                        RagioneSociale = reader.GetStringSafe("RagioneSociale"),
                        CodiceFiscale = reader.GetStringSafe("CodiceFiscale"),
                        PartitaIva = reader.GetStringSafe("PartitaIva"),
                        Indirizzo = reader.GetStringSafe("Indirizzo"),
                        Citta = reader.GetStringSafe("Citta"),
                        Provincia = reader.GetStringSafe("Provincia"),
                        CAP = reader.GetStringSafe("CAP"),
                        Email = reader.GetStringSafe("Email"),
                        Telefono = reader.GetStringSafe("Telefono"),
                        Note = reader.GetStringSafe("Note"),
                        IdAgente = reader.GetNullableInt("IdAgente")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore SQL in ClienteRepository.SearchByRagioneSociale('{term}')", ex);
            }

            return list;
        }

        public Cliente? GetById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spClienti_GetById", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = id;

            conn.Open();
            using var reader = cmd.ExecuteReader();

            return reader.Read() ? Map(reader) : null;
        }

        public Cliente? GetByPartitaIva(string partitaIva)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spClienti_GetByPartitaIva", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@PartitaIva", SqlDbType.NVarChar).Value = partitaIva;

            conn.Open();
            using var reader = cmd.ExecuteReader();

            return reader.Read() ? Map(reader) : null;
        }

        public Cliente? GetByCodiceFiscale(string codiceFiscale)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spClienti_GetByCodiceFiscale", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@CodiceFiscale", SqlDbType.NVarChar).Value = codiceFiscale;

            conn.Open();
            using var reader = cmd.ExecuteReader();

            return reader.Read() ? Map(reader) : null;
        }
        private Cliente Map(SqlDataReader reader)
        {
            // Creazione dell'istanza vuota
            var cliente = new Cliente();

            // --- ANAGRAFICA BASE ---
            cliente.IdCliente = reader.GetIntSafe("IdCliente");
            cliente.RagioneSociale = reader.GetStringSafe("RagioneSociale");
            cliente.CodiceFiscale = reader.GetStringSafe("CodiceFiscale");
            cliente.PartitaIva = reader.GetStringSafe("PartitaIva");

            // --- CONTATTI E INDIRIZZO ---// --- SEDE LEGALE ---
            cliente.Indirizzo = reader.GetStringSafe("Indirizzo");
            cliente.Citta = reader.GetStringSafe("Citta");
            cliente.Provincia = reader.GetStringSafe("Provincia");
            cliente.CAP = reader.GetStringSafe("CAP");
            cliente.Email = reader.GetStringSafe("Email");
            cliente.Telefono = reader.GetStringSafe("Telefono");
            cliente.Nazione = reader.GetStringSafe("Nazione"); // <--- 23/09/2026 aggiunto per coerenza con la tabella Clienti

            // --- SEDE OPERATIVA (Nuovi campi) ---
            cliente.SO_Indirizzo = reader.GetStringSafe("SO_Indirizzo"); // <--- 23/09/2026
            cliente.SO_Citta = reader.GetStringSafe("SO_Citta");         // <--- 23/09/2026
            cliente.SO_Provincia = reader.GetStringSafe("SO_Provincia"); // <--- 23/09/2026
            cliente.SO_CAP = reader.GetStringSafe("SO_CAP");             // <--- 23/09/2026
            cliente.SO_Nazione = reader.GetStringSafe("SO_Nazione");     // <--- 23/09/2026

            //21/10/2026 - Referente
            cliente.Referente = reader.GetStringSafe("Referente");
            cliente.TelefonoReferente = reader.GetStringSafe("TelefonoReferente");
            cliente.EmailReferente = reader.GetStringSafe("EmailReferente");
            cliente.NoteReferente = reader.GetStringSafe("NoteReferente");


            cliente.IdTipoPagamentoDefault = reader.GetNullableInt("IdTipoPagamentoDefault"); // 26/08/2026

            // 16/10/2026 Descrizioni (JOIN) presenti solo in spClienti_GetAll / spClienti_GetDeleted.
            // Nelle altre SP la colonna manca: GetStringSafe restituisce string.Empty, nessuna eccezione.
            cliente.AgenteDescrizione = reader.GetStringSafe("AgenteDescrizione");
            cliente.TipoPagamentoDescrizione = reader.GetStringSafe("TipoPagamentoDescrizione");


            // --- LOGICA E RELAZIONI ---
            cliente.Note = reader.GetStringSafe("Note");
            cliente.IdAgente = reader.GetNullableInt("IdAgente");
            cliente.Utente = reader.GetStringSafe("Utente");
            cliente.IsDeleted = reader.GetBoolSafe("IsDeleted");

            // --- GESTIONE DATE (Debug-Friendly) ---
            // Invece di un unico blocco try-catch, qui puoi vedere se crasha su una colonna specifica
            try
            {
                // Se DataInserimento non esiste nella SELECT, GetOrdinal lancia l'errore qui
                int ordIns = reader.GetOrdinal("DataInserimento");
                cliente.DataInserimento = reader.GetDateTime(ordIns);
            }
            catch { /* Colonna non presente nella query SQL */ }

            try
            {
                int ordMod = reader.GetOrdinal("DataModifica");
                if (!reader.IsDBNull(ordMod))
                {
                    cliente.DataModifica = reader.GetDateTime(ordMod);
                }
            }
            catch { /* Colonna non presente nella query SQL */ }

            return cliente;
        }

        public Cliente? GetByRagioneSociale(string ragioneSociale)
        {
            Console.WriteLine($"[DEBUG] THREAD {Environment.CurrentManagedThreadId} - GetByRagioneSociale('{ragioneSociale}')");

            const string sql = @"SELECT * FROM Clienti WHERE RagioneSociale = @RagioneSociale";

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.Add("@RagioneSociale", SqlDbType.NVarChar).Value = ragioneSociale;

                conn.Open();

                using var reader = cmd.ExecuteReader();
                return reader.Read() ? Map(reader) : null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore SQL in ClienteRepository.GetByRagioneSociale('{ragioneSociale}')", ex);
            }
        }


        /// <summary>
        /// Recupera la lista dei clienti.
        /// Per impostazione predefinita restituisce solo i clienti attivi (IsDeleted = 0).
        /// </summary>
        /// <param name="showDeleted">Se impostato a true, restituisce i record archiviati (IsDeleted = 1).</param>
        /// <param name="idAgente">
        /// 12/10/2026 Se valorizzato, restituisce solo i clienti di quell'agente (ruolo Agente).
        /// Se null, nessun filtro (Admin/Operatore). Richiede le SP aggiornate (@IdAgente INT = NULL).
        /// 30/10/2026 spClienti_GetAll: agente di default OPPURE ProvvigioniAgenti; spClienti_GetDeleted (Cestino):
        /// solo agente di default (eliminare/ripristinare resta riservato al proprietario).
        /// </param>
        public IEnumerable<Cliente> GetAll(bool showDeleted = false, int? idAgente = null)
        {
            // Determiniamo quale Stored Procedure chiamare
            string procedureName = showDeleted ? "spClienti_GetDeleted" : "spClienti_GetAll";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(procedureName, conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            // 12/10/2026 Parametro filtro agente passato alla SP (spClienti_GetAll / spClienti_GetDeleted).
            // DBNull = nessun filtro, quindi Admin e Operatore continuano a vedere tutto.
            cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = (object?)idAgente ?? DBNull.Value;

            var list = new List<Cliente>();

            try
            {
                conn.Open();
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    // Utilizziamo il tuo metodo Map() privato per la coerenza dei dati
                    list.Add(Map(reader));
                }
            }
            catch (Exception ex)
            {
                // Log dettagliato dell'errore per facilitare il debug
                throw new Exception($"Errore SQL in ClienteRepository.GetAll(showDeleted: {showDeleted})", ex);
            }

            return list;
        }




        //public Cliente Insert(Cliente cliente, IDbConnection connection, IDbTransaction transaction)
        //{
        //    const string sql = @"
        //        INSERT INTO Clienti (RagioneSociale)
        //        VALUES (@RagioneSociale);
        //        SELECT SCOPE_IDENTITY();
        //    ";

        //    try
        //    {
        //        var conn = (SqlConnection)connection;
        //        var tran = (SqlTransaction)transaction;

        //        using var cmd = new SqlCommand(sql, conn, tran);

        //        cmd.Parameters.Add("@RagioneSociale", SqlDbType.NVarChar).Value = cliente.RagioneSociale;

        //        cliente.IdCliente = Convert.ToInt32(cmd.ExecuteScalar());
        //        return cliente;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("Errore SQL in ClienteRepository.Insert", ex);
        //    }
        //}

        /// <summary>
        /// // 26/08/2026 inserisce un cliente durante l'importazione Excel.
        /// Se esiste già un cliente con la stessa ragione sociale (anche se è stato eliminato),
        /// lo "resuscita" aggiornando IsDeleted a 0 e restituendo l'ID esistente.
        /// Se non esiste, lo crea da zero.
        /// Usa SQL inline per garantire che faccia parte della transazione esterna dell'importazione.
        /// </summary>
        public Cliente Insert(Cliente cliente, IDbConnection connection, IDbTransaction transaction) // 26/08/2026
        {
            // Logica: Se esiste già un cliente con quel nome (anche eliminato),
            // lo resettiamo come attivo (IsDeleted = 0) e prendiamo il suo ID.
            // Se non esiste, lo creiamo da zero.
            const string sql = @"
            DECLARE @ExistingId INT;
            SELECT @ExistingId = IdCliente FROM Clienti WHERE RagioneSociale = @RagioneSociale;

            IF @ExistingId IS NOT NULL
                BEGIN
                    UPDATE Clienti
                    SET IsDeleted = 0,
                        DataModifica = GETDATE()
                    WHERE IdCliente = @ExistingId;

                    SELECT @ExistingId;
                END
            ELSE
                BEGIN
                    INSERT INTO Clienti (RagioneSociale, DataInserimento, IsDeleted)
                    VALUES (@RagioneSociale, GETDATE(), 0);

                    SELECT SCOPE_IDENTITY();
                END";

            try
            {
                var conn = (SqlConnection)connection;
                var tran = (SqlTransaction)transaction;

                using var cmd = new SqlCommand(sql, conn, tran);
                cmd.Parameters.Add("@RagioneSociale", SqlDbType.NVarChar).Value = cliente.RagioneSociale;

                // Eseguiamo e recuperiamo l'ID (nuovo o resuscitato)
                cliente.IdCliente = Convert.ToInt32(cmd.ExecuteScalar());
                cliente.IsDeleted = false; // Forza lo stato a attivo nel modello C#

                return cliente;
            }
            catch (Exception ex)
            {
                throw new Exception("Errore durante l'Upsert (Insert/Restore) del Cliente nell'importazione.", ex);
            }
        }

        ///// <summary>
        ///// Seleziona tutti i clienti in base al Mandatario a cui sono associati (tabella di relazione MandatarioCliente).
        ///// </summary>
        //public List<Cliente> GetByMandatario(int idMandatario)
        //{
        //    using var conn = new SqlConnection(_connectionString);
        //    using var cmd = new SqlCommand("spClienti_GetByMandatario", conn)
        //    {
        //        CommandType = CommandType.StoredProcedure
        //    };

        //    cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = idMandatario;

        //    conn.Open();
        //    using var reader = cmd.ExecuteReader();

        //    var list = new List<Cliente>();
        //    while (reader.Read())
        //    {
        //        list.Add(Map(reader));
        //    }

        //    return list;
        //}
        /// <summary>
        /// Seleziona tutti i clienti vivi + il cliente specifico dell'ordine (se eliminato) 31/03/2026
        /// </summary>
        // ==== MODIFICA 30/10/2026 - INIZIO ====
        // Nuovo parametro idAgente: se valorizzato la SP restituisce solo i clienti dell'agente per il mandatario
        // (Clienti.IdAgente OPPURE ProvvigioniAgenti), più sempre idClienteGiaIncluso.
        //public List<Cliente> GetByMandatario(int idMandatario, int? idClienteGiaIncluso = null)   // 30/10/2026 sostituita dalla riga sotto
        public List<Cliente> GetByMandatario(int idMandatario, int? idClienteGiaIncluso = null, int? idAgente = null)
        // ==== MODIFICA 30/10/2026 - FINE ====
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spClienti_GetByMandatario", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = idMandatario;
            // Passiamo il parametro alla SP, ci pensa lei!
            cmd.Parameters.Add("@IdClienteGiaIncluso", SqlDbType.Int).Value = (object)idClienteGiaIncluso ?? DBNull.Value;
            // ==== MODIFICA 30/10/2026 - INIZIO ====
            // DBNull = nessun filtro (Admin/Operatore); valorizzato = ruolo Agente
            cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = (object?)idAgente ?? DBNull.Value;
            // ==== MODIFICA 30/10/2026 - FINE ====

            conn.Open();
            using var reader = cmd.ExecuteReader();

            var list = new List<Cliente>();
            while (reader.Read())
            {
                list.Add(Map(reader));
            }
            return list; // Niente IF qui, fa tutto la SP
        }

        /// Inserisce un record nella tabella di collegamento MandatariClienti (IdMandatario, IdCliente) solo se non esiste già.
        /// Questo metodo deve usare connessione e transazione esterne, come nell' Insert().
        public void InsertMandatarioClienteIfNotExists(
            int idMandatario,
            int idCliente,
            IDbConnection connection,
            IDbTransaction transaction)
        {
            var conn = (SqlConnection)connection;
            var tran = (SqlTransaction)transaction;

            const string sql = @"
        IF NOT EXISTS (
            SELECT 1
            FROM MandatariClienti
            WHERE IdMandatario = @IdMandatario
              AND IdCliente = @IdCliente
        )
        BEGIN
            INSERT INTO MandatariClienti (IdMandatario, IdCliente)
            VALUES (@IdMandatario, @IdCliente);
        END
    ";

            using var cmd = new SqlCommand(sql, conn, tran);

            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = idMandatario;
            cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = idCliente;

            cmd.ExecuteNonQuery();
        }




        public void Update(Cliente cliente)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spClienti_Update", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = cliente.IdCliente;
            cmd.Parameters.Add("@RagioneSociale", SqlDbType.VarChar).Value = cliente.RagioneSociale;
            cmd.Parameters.Add("@CodiceFiscale", SqlDbType.VarChar).Value = (object?)cliente.CodiceFiscale ?? DBNull.Value;
            cmd.Parameters.Add("@PartitaIva", SqlDbType.VarChar).Value = (object?)cliente.PartitaIva ?? DBNull.Value;
            cmd.Parameters.Add("@Indirizzo", SqlDbType.VarChar).Value = (object?)cliente.Indirizzo ?? DBNull.Value;
            cmd.Parameters.Add("@Citta", SqlDbType.VarChar).Value = (object?)cliente.Citta ?? DBNull.Value;
            cmd.Parameters.Add("@Provincia", SqlDbType.Char).Value = (object?)cliente.Provincia ?? DBNull.Value;
            cmd.Parameters.Add("@CAP", SqlDbType.Char).Value = (object?)cliente.CAP ?? DBNull.Value;
            cmd.Parameters.Add("@Nazione", SqlDbType.VarChar).Value = (object?)cliente.Nazione ?? "Italia";//23/09/2026

            // Sede Operativa //23/09/2026
            cmd.Parameters.Add("@SO_Indirizzo", SqlDbType.VarChar).Value = (object?)cliente.SO_Indirizzo ?? DBNull.Value;
            cmd.Parameters.Add("@SO_Citta", SqlDbType.VarChar).Value = (object?)cliente.SO_Citta ?? DBNull.Value;
            cmd.Parameters.Add("@SO_Provincia", SqlDbType.Char).Value = (object?)cliente.SO_Provincia ?? DBNull.Value;
            cmd.Parameters.Add("@SO_CAP", SqlDbType.Char).Value = (object?)cliente.SO_CAP ?? DBNull.Value;
            cmd.Parameters.Add("@SO_Nazione", SqlDbType.VarChar).Value = (object?)cliente.SO_Nazione ?? "Italia";


            cmd.Parameters.Add("@Email", SqlDbType.VarChar).Value = (object?)cliente.Email ?? DBNull.Value;
            cmd.Parameters.Add("@Telefono", SqlDbType.VarChar).Value = (object?)cliente.Telefono ?? DBNull.Value;
            cmd.Parameters.Add("@Note", SqlDbType.NVarChar).Value = (object?)cliente.Note ?? DBNull.Value;
            cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = (object?)cliente.IdAgente ?? DBNull.Value;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = (object?)cliente.Utente ?? DBNull.Value;
            cmd.Parameters.Add("@IdTipoPagamentoDefault", SqlDbType.Int).Value = (object?)cliente.IdTipoPagamentoDefault ?? DBNull.Value;//26/08/2026
            //21/10/2026
            cmd.Parameters.Add("@Referente", SqlDbType.VarChar, 150).Value = (object?)cliente.Referente ?? DBNull.Value;
            cmd.Parameters.Add("@TelefonoReferente", SqlDbType.VarChar, 20).Value = (object?)cliente.TelefonoReferente ?? DBNull.Value;
            cmd.Parameters.Add("@EmailReferente", SqlDbType.VarChar, 100).Value = (object?)cliente.EmailReferente ?? DBNull.Value;
            cmd.Parameters.Add("@NoteReferente", SqlDbType.NVarChar, 500).Value = (object?)cliente.NoteReferente ?? DBNull.Value;

            // ==== MODIFICA 30/10/2026 - INIZIO ====
            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex) when (IsViolazioneUnicita(ex))
            {
                throw new InvalidOperationException(MessaggioClienteDuplicato);
            }
            // ==== MODIFICA 30/10/2026 - FINE ====
        }


        public void Delete(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spClienti_Delete", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = id;

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        //Verifica in tutte le Tabelle Prospetto  se esistono record collegati prima di procedere con la cancellazione.
        public bool HasOrdersOrRelations(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spClienti_CheckRelazioniDinamiche", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            // Passiamo il parametro alla stored procedure
            cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = id;

            try
            {
                conn.Open();

                // ExecuteScalar recupera la prima colonna della prima riga (l'1 o lo 0 della SP)
                var result = cmd.ExecuteScalar();

                return result != null && Convert.ToInt32(result) == 1;
            }
            catch (Exception ex)
            {
                // Errore durante la chiamata al database
                throw new Exception($"Errore durante l'esecuzione di spClienti_CheckRelazioniDinamiche per l'ID {id}", ex);
            }
        }

        public void InsertManual(Cliente cliente)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spClienti_InsertManual", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Mappatura parametri identica all'Update ma senza IdCliente (è Identity)
            cmd.Parameters.Add("@RagioneSociale", SqlDbType.VarChar, 150).Value = cliente.RagioneSociale;
            cmd.Parameters.Add("@CodiceFiscale", SqlDbType.VarChar, 16).Value = (object?)cliente.CodiceFiscale ?? DBNull.Value;
            cmd.Parameters.Add("@PartitaIva", SqlDbType.VarChar, 11).Value = (object?)cliente.PartitaIva ?? DBNull.Value;

            // Sede Legale
            cmd.Parameters.Add("@Indirizzo", SqlDbType.VarChar, 255).Value = (object?)cliente.Indirizzo ?? DBNull.Value;
            cmd.Parameters.Add("@Citta", SqlDbType.VarChar, 100).Value = (object?)cliente.Citta ?? DBNull.Value;
            cmd.Parameters.Add("@Provincia", SqlDbType.Char, 2).Value = (object?)cliente.Provincia ?? DBNull.Value;
            cmd.Parameters.Add("@CAP", SqlDbType.Char, 5).Value = (object?)cliente.CAP ?? DBNull.Value;
            cmd.Parameters.Add("@Nazione", SqlDbType.VarChar, 100).Value = (object?)cliente.Nazione ?? "Italia";//23/09/2026 default Italia

            // Sede Operativa //23/09/2026
            cmd.Parameters.Add("@SO_Indirizzo", SqlDbType.VarChar, 255).Value = (object?)cliente.SO_Indirizzo ?? DBNull.Value;
            cmd.Parameters.Add("@SO_Citta", SqlDbType.VarChar, 100).Value = (object?)cliente.SO_Citta ?? DBNull.Value;
            cmd.Parameters.Add("@SO_Provincia", SqlDbType.Char, 2).Value = (object?)cliente.SO_Provincia ?? DBNull.Value;
            cmd.Parameters.Add("@SO_CAP", SqlDbType.Char, 5).Value = (object?)cliente.SO_CAP ?? DBNull.Value;
            cmd.Parameters.Add("@SO_Nazione", SqlDbType.VarChar, 100).Value = (object?)cliente.SO_Nazione ?? "Italia";



            cmd.Parameters.Add("@Email", SqlDbType.VarChar, 100).Value = (object?)cliente.Email ?? DBNull.Value;
            cmd.Parameters.Add("@Telefono", SqlDbType.VarChar, 20).Value = (object?)cliente.Telefono ?? DBNull.Value;
            cmd.Parameters.Add("@Note", SqlDbType.NVarChar, -1).Value = (object?)cliente.Note ?? DBNull.Value;
            cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = (object?)cliente.IdAgente ?? DBNull.Value;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar, 100).Value = (object?)cliente.Utente ?? DBNull.Value;
            cmd.Parameters.Add("@IdTipoPagamentoDefault", SqlDbType.Int).Value = (object?)cliente.IdTipoPagamentoDefault ?? DBNull.Value;//26/08/2026

            // ==== MODIFICA 30/10/2026 - INIZIO ====
            // I campi del referente erano previsti dalla SP (default NULL) ma non venivano passati: in inserimento andavano persi.
            cmd.Parameters.Add("@Referente", SqlDbType.VarChar, 150).Value = (object?)cliente.Referente ?? DBNull.Value;
            cmd.Parameters.Add("@TelefonoReferente", SqlDbType.VarChar, 20).Value = (object?)cliente.TelefonoReferente ?? DBNull.Value;
            cmd.Parameters.Add("@EmailReferente", SqlDbType.VarChar, 100).Value = (object?)cliente.EmailReferente ?? DBNull.Value;
            cmd.Parameters.Add("@NoteReferente", SqlDbType.NVarChar, 500).Value = (object?)cliente.NoteReferente ?? DBNull.Value;

            try
            {
                conn.Open();
                var result = cmd.ExecuteScalar();
                if (result != null) cliente.IdCliente = Convert.ToInt32(result);
            }
            catch (SqlException ex) when (IsViolazioneUnicita(ex))
            {
                // Indici unici su nome normalizzato / Partita IVA / Codice Fiscale
                throw new InvalidOperationException(MessaggioClienteDuplicato);
            }
            // ==== MODIFICA 30/10/2026 - FINE ====
        }



        /// <summary>
        /// Ripristina un cliente precedentemente rimosso tramite Soft Delete,
        /// impostando il flag IsDeleted a 0 tramite Stored Procedure.
        /// </summary>
        /// <param name="id">ID univoco del cliente da ripristinare.</param>
        /// <returns>True se l'operazione ha aggiornato correttamente il record.</returns>
        public bool Restore(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spClienti_Restore", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Utilizzo di parametri tipizzati per evitare conversioni implicite e SQL Injection
            cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = id;

            try
            {
                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore SQL in ClienteRepository.Restore(id: {id})", ex);
            }
        }

        /// <summary>
        /// Recupera il Tipo Pagamento di default associato al cliente per la precompilazione del form ordine.
        /// </summary>
        /// <param name="idCliente">Identificativo del cliente.</param>
        /// <returns>Il DTO minimale con ID, Codice e Descrizione oppure null se non presente.</returns>
        public TipoPagamentoDefaultDto? GetTipoPagamentoDefault(int idCliente)
        {
            const string sql = @"
                SELECT
                    tp.IdTipoPagamento,
                    tp.CodiceTipoPagamento,
                    tp.TipoPagamento
                FROM Clienti c
                INNER JOIN TipoPagamento tp ON c.IdTipoPagamentoDefault = tp.IdTipoPagamento
                WHERE c.IdCliente = @IdCliente
                  AND c.IsDeleted = 0;";

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = idCliente;

                conn.Open();
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return new TipoPagamentoDefaultDto
                    {
                        IdTipoPagamento = reader.GetIntSafe("IdTipoPagamento"),
                        CodiceTipoPagamento = reader.GetStringSafe("CodiceTipoPagamento"),
                        TipoPagamento = reader.GetStringSafe("TipoPagamento")
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore SQL in ClienteRepository.GetTipoPagamentoDefault({idCliente})", ex);
            }
        }

        /// <summary>
        /// 12/10/2026 Verifica se il cliente appartiene all'agente indicato (Clienti.IdAgente = @IdAgente).
        /// NON filtra su IsDeleted: serve anche per autorizzare il ripristino di un cliente archiviato.
        /// SQL inline come GetTipoPagamentoDefault: è una semplice verifica di esistenza.
        /// 30/10/2026 Verifica SOLO l'agente di default dell'anagrafica: è il criterio per eliminare/ripristinare.
        /// Per l'accesso in lettura/modifica e per gli ordini usare IsClienteAccessibileAdAgente.
        /// </summary>
        /// <param name="idCliente">Identificativo del cliente.</param>
        /// <param name="idAgente">Identificativo dell'agente collegato all'utente autenticato.</param>
        /// <returns>True se il cliente esiste ed è associato all'agente.</returns>
        public bool IsClienteDiAgente(int idCliente, int idAgente)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM Clienti
                WHERE IdCliente = @IdCliente
                  AND IdAgente = @IdAgente;";

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = idCliente;
                cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = idAgente;

                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore SQL in ClienteRepository.IsClienteDiAgente({idCliente}, {idAgente})", ex);
            }
        }

        // ==== MODIFICA 30/10/2026 - INIZIO ====
        /// <summary>
        /// 30/10/2026 True se l'agente può lavorare sul cliente: agente di default (Clienti.IdAgente)
        /// OPPURE riga in ProvvigioniAgenti. Regola unica nella funzione SQL dbo.fnClientiDellAgente.
        /// - idMandatario = null: qualsiasi mandatario (anagrafica clienti);
        /// - idMandatario valorizzato (ordini): le provvigioni contano solo per quel mandatario
        ///   e il cliente deve essere collegato al mandatario in MandatariClienti.
        /// NON filtra su IsDeleted (un ordine storico può riferirsi a un cliente archiviato).
        /// </summary>
        public bool IsClienteAccessibileAdAgente(int idCliente, int idAgente, int? idMandatario = null)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM dbo.fnClientiDellAgente(@IdAgente, @IdMandatario) f
                WHERE f.IdCliente = @IdCliente
                  AND (@IdMandatario IS NULL OR EXISTS (
                        SELECT 1
                        FROM dbo.MandatariClienti mc
                        WHERE mc.IdMandatario = @IdMandatario
                          AND mc.IdCliente = @IdCliente));";

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = idCliente;
                cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = idAgente;
                cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = (object?)idMandatario ?? DBNull.Value;

                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore SQL in ClienteRepository.IsClienteAccessibileAdAgente({idCliente}, {idAgente}, {idMandatario})", ex);
            }
        }
        // ==== MODIFICA 30/10/2026 - FINE ====

        // ============================================================
        // 14/10/2026 ASSOCIAZIONE CLIENTE <-> MANDATARIO (form anagrafica)
        // SQL inline, NON legato all'import COBRAL: ogni metodo usa la
        // propria connessione (come IsClienteDiAgente).
        // ============================================================

        /// <summary>
        /// 14/10/2026 Restituisce il mandatario associato al cliente in MandatariClienti, o null se assente.
        /// Se per il cliente esistono più righe (es. create dall'import COBRAL o dalla scheda Mandatario)
        /// restituisce la più recente, con ordinamento deterministico: è lo stesso criterio usato per la
        /// pre-selezione nella DDL e per decidere se il valore postato dal form è cambiato.
        /// </summary>
        public int? GetIdMandatarioByCliente(int idCliente)
        {
            const string sql = @"
                SELECT TOP (1) IdMandatario
                FROM MandatariClienti
                WHERE IdCliente = @IdCliente
                ORDER BY DataInserimento DESC, IdMandatario DESC;";

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = idCliente;

                conn.Open();
                var result = cmd.ExecuteScalar();

                return (result == null || result == DBNull.Value) ? null : Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore SQL in ClienteRepository.GetIdMandatarioByCliente({idCliente})", ex);
            }
        }

        /// <summary>
        /// 14/10/2026 Imposta il mandatario del cliente in MandatariClienti (un solo batch, una transazione):
        /// - idMandatario = null          -> rimuove l'associazione;
        /// - idMandatario valorizzato     -> elimina le eventuali associazioni diverse e inserisce quella nuova
        ///   se non esiste già (idempotente).
        /// Le FK di MandatariClienti sono NOCHECK: l'esistenza del mandatario va validata dal chiamante.
        /// </summary>
        public void SetMandatarioCliente(int idCliente, int? idMandatario, string? utente)
        {
            const string sql = @"
                SET XACT_ABORT ON;
                BEGIN TRANSACTION;

                IF @IdMandatario IS NULL
                BEGIN
                    DELETE FROM MandatariClienti
                    WHERE IdCliente = @IdCliente;
                END
                ELSE
                BEGIN
                    DELETE FROM MandatariClienti
                    WHERE IdCliente = @IdCliente
                      AND IdMandatario <> @IdMandatario;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM MandatariClienti WITH (UPDLOCK, HOLDLOCK)
                        WHERE IdMandatario = @IdMandatario
                          AND IdCliente = @IdCliente)
                    BEGIN
                        INSERT INTO MandatariClienti (IdMandatario, IdCliente, DataInserimento, Utente)
                        VALUES (@IdMandatario, @IdCliente, GETDATE(), @Utente);
                    END
                END

                COMMIT TRANSACTION;";

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = idCliente;
                cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = (object?)idMandatario ?? DBNull.Value;
                cmd.Parameters.Add("@Utente", SqlDbType.NVarChar, 100).Value = (object?)utente ?? DBNull.Value;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore SQL in ClienteRepository.SetMandatarioCliente({idCliente}, {idMandatario})", ex);
            }
        }

        // ==== MODIFICA 30/10/2026 - INIZIO ====
        // ============================================================
        // 30/10/2026 CREAZIONE CLIENTE DA PARTE DELL'AGENTE
        // ============================================================

        private const string MessaggioClienteDuplicato =
            "Esiste già un cliente con lo stesso nome, la stessa Partita IVA o lo stesso Codice Fiscale.";

        // 2601 = indice unico violato, 2627 = vincolo UNIQUE/PK violato
        private static bool IsViolazioneUnicita(SqlException ex) => ex.Number is 2601 or 2627;

        /// <summary>
        /// 30/10/2026 Chiama spClienti_InsertDaAgente: tutto (controllo duplicati, Clienti, MandatariClienti,
        /// ProvvigioniAgenti) avviene in UNA transazione dentro la stored procedure.
        /// I casi di business (duplicato, percentuale mancante...) tornano come esito, non come eccezione.
        /// </summary>
        public EsitoCreazioneClienteDto InsertDaAgente(Cliente cliente, int idAgente)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spClienti_InsertDaAgente", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = idAgente;
            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = (object?)cliente.IdMandatario ?? DBNull.Value;
            cmd.Parameters.Add("@RagioneSociale", SqlDbType.VarChar, 150).Value = cliente.RagioneSociale;
            cmd.Parameters.Add("@CodiceFiscale", SqlDbType.VarChar, 16).Value = (object?)cliente.CodiceFiscale ?? DBNull.Value;
            cmd.Parameters.Add("@PartitaIva", SqlDbType.VarChar, 11).Value = (object?)cliente.PartitaIva ?? DBNull.Value;

            // Sede legale
            cmd.Parameters.Add("@Indirizzo", SqlDbType.VarChar, 255).Value = (object?)cliente.Indirizzo ?? DBNull.Value;
            cmd.Parameters.Add("@Citta", SqlDbType.VarChar, 100).Value = (object?)cliente.Citta ?? DBNull.Value;
            cmd.Parameters.Add("@Provincia", SqlDbType.Char, 2).Value = (object?)cliente.Provincia ?? DBNull.Value;
            cmd.Parameters.Add("@CAP", SqlDbType.Char, 5).Value = (object?)cliente.CAP ?? DBNull.Value;
            cmd.Parameters.Add("@Nazione", SqlDbType.VarChar, 100).Value = (object?)cliente.Nazione ?? "Italia";

            // Sede operativa
            cmd.Parameters.Add("@SO_Indirizzo", SqlDbType.VarChar, 255).Value = (object?)cliente.SO_Indirizzo ?? DBNull.Value;
            cmd.Parameters.Add("@SO_Citta", SqlDbType.VarChar, 100).Value = (object?)cliente.SO_Citta ?? DBNull.Value;
            cmd.Parameters.Add("@SO_Provincia", SqlDbType.Char, 2).Value = (object?)cliente.SO_Provincia ?? DBNull.Value;
            cmd.Parameters.Add("@SO_CAP", SqlDbType.Char, 5).Value = (object?)cliente.SO_CAP ?? DBNull.Value;
            cmd.Parameters.Add("@SO_Nazione", SqlDbType.VarChar, 100).Value = (object?)cliente.SO_Nazione ?? "Italia";

            // Contatti e note
            cmd.Parameters.Add("@Email", SqlDbType.VarChar, 100).Value = (object?)cliente.Email ?? DBNull.Value;
            cmd.Parameters.Add("@Telefono", SqlDbType.VarChar, 20).Value = (object?)cliente.Telefono ?? DBNull.Value;
            cmd.Parameters.Add("@Note", SqlDbType.NVarChar, -1).Value = (object?)cliente.Note ?? DBNull.Value;
            cmd.Parameters.Add("@IdTipoPagamentoDefault", SqlDbType.Int).Value = (object?)cliente.IdTipoPagamentoDefault ?? DBNull.Value;

            // Referente
            cmd.Parameters.Add("@Referente", SqlDbType.VarChar, 150).Value = (object?)cliente.Referente ?? DBNull.Value;
            cmd.Parameters.Add("@TelefonoReferente", SqlDbType.VarChar, 20).Value = (object?)cliente.TelefonoReferente ?? DBNull.Value;
            cmd.Parameters.Add("@EmailReferente", SqlDbType.VarChar, 100).Value = (object?)cliente.EmailReferente ?? DBNull.Value;
            cmd.Parameters.Add("@NoteReferente", SqlDbType.NVarChar, 500).Value = (object?)cliente.NoteReferente ?? DBNull.Value;

            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar, 100).Value = (object?)cliente.Utente ?? DBNull.Value;

            try
            {
                conn.Open();
                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                    throw new InvalidOperationException("spClienti_InsertDaAgente non ha restituito alcun esito.");

                return new EsitoCreazioneClienteDto
                {
                    Esito = MappaEsito(reader.GetStringSafe("Esito")),
                    IdCliente = reader.GetNullableInt("IdCliente")
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore SQL in ClienteRepository.InsertDaAgente(agente {idAgente})", ex);
            }
        }

        // Traduce il testo della colonna "Esito" nell'enum; un valore sconosciuto è un errore di programmazione (fail-closed)
        private static EsitoCreazioneCliente MappaEsito(string esito) => esito switch
        {
            "OK" => EsitoCreazioneCliente.Ok,
            "GIA_TUO" => EsitoCreazioneCliente.GiaTuo,
            "ESISTE_NON_VISIBILE" => EsitoCreazioneCliente.EsisteNonVisibile,
            "ARCHIVIATO" => EsitoCreazioneCliente.Archiviato,
            "DUPLICATO" => EsitoCreazioneCliente.Duplicato,
            "NO_PERCENTUALE" => EsitoCreazioneCliente.NoPercentuale,
            "AGENTE_NON_VALIDO" => EsitoCreazioneCliente.AgenteNonValido,
            "MANDATARIO_NON_VALIDO" => EsitoCreazioneCliente.MandatarioNonValido,
            "NOME_NON_VALIDO" => EsitoCreazioneCliente.NomeNonValido,
            _ => throw new InvalidOperationException($"Esito sconosciuto da spClienti_InsertDaAgente: '{esito}'")
        };

        /// <summary>
        /// 30/10/2026 Nomi simili per l'avviso durante la digitazione (spClienti_GetSimili).
        /// La stored procedure oscura nome e id dei clienti non visibili all'agente.
        /// </summary>
        public List<ClienteSimileDto> GetSimili(string term, int? idAgente, int max = 5)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spClienti_GetSimili", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@Term", SqlDbType.VarChar, 150).Value = term.Length > 150 ? term[..150] : term;
            cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = (object?)idAgente ?? DBNull.Value;
            cmd.Parameters.Add("@Max", SqlDbType.Int).Value = max;

            var list = new List<ClienteSimileDto>();

            try
            {
                conn.Open();
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var visibile = reader.GetBoolSafe("Visibile");
                    list.Add(new ClienteSimileDto
                    {
                        Visibile = visibile,
                        Archiviato = reader.GetBoolSafe("Archiviato"),
                        IdCliente = visibile ? reader.GetNullableInt("IdCliente") : null,
                        RagioneSociale = visibile ? reader.GetStringSafe("RagioneSociale") : null
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore SQL in ClienteRepository.GetSimili('{term}')", ex);
            }

            return list;
        }

        /// <summary>30/10/2026 Chi ha creato il cliente (spClienti_GetCreatore). Null = creato da Admin/Operatore o cliente inesistente.</summary>
        public int? GetIdAgenteCreatore(int idCliente)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spClienti_GetCreatore", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = idCliente;

            try
            {
                conn.Open();
                var result = cmd.ExecuteScalar();
                return (result == null || result == DBNull.Value) ? null : Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore SQL in ClienteRepository.GetIdAgenteCreatore({idCliente})", ex);
            }
        }
        // ==== MODIFICA 30/10/2026 - FINE ====

    }
}
