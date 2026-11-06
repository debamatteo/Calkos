#region USING
using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Infrastructure.Helpers;
using Microsoft.Data.SqlClient;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using static System.Runtime.InteropServices.JavaScript.JSType;
using CalkosManager.Domain.Interfaces;
#endregion

/*
    ============================================================
    IMPORT COBRAL REPOSITORY — PROMEMORIA ARCHITETTURALE
    ============================================================

    Questo repository gestisce la tabella ImportCobral, che contiene
    tutte le righe importate dai file COBRAL prima della validazione
    e della trasformazione nei prospetti definitivi.

    ✔ Repository “speciale”: NON va modificato nella logica
      se non in caso di errori tecnici (AddWithValue, tipi SQL,
      mapping, parametri mancanti, ecc.).

    ✔ Caratteristiche principali:
      - Gestisce oltre 30 colonne, molte delle quali decimal(18,3).
      - Supporta inserimenti massivi tramite InsertRange, ottimizzato
        con parametri riutilizzati.
      - Supporta inserimenti singoli transazionali durante l’import.
      - Usa SQL inline per massima velocità e controllo.
      - Usa SqlReaderExtensions per lettura sicura dei campi.

    ✔ Standard tecnici:
      - NIENTE AddWithValue: parametri sempre tipizzati.
      - Per i DECIMAL è obbligatorio impostare Precision e Scale.
      - Mapping basato su nomi colonna, mai su indici.
      - Gestione errori con dump dei parametri tramite SqlDebugHelper.
      - Nessuna modifica alla logica di business o al flusso COBRAL.

    Questo repository è considerato “sensibile”: ogni modifica deve
    essere minima, motivata e verificata con attenzione, per evitare
    impatti sul processo di importazione COBRAL.
    ============================================================
*/

namespace CalkosManager.Infrastructure.Repositories
{
    public class ProspettoCobralRepository : IProspettoCobralRepository
    {
        #region FIELDS & CONSTRUCTOR

        private readonly string _connectionString;

        public ProspettoCobralRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public enum TipoOperazione
        {
            Insert,
            InsertTran,
            Update,
            delete
        }

        #endregion
        #region INSERT


        public int Insert(ProspettoCobral item, string utente)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoCobral_Insert", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(cmd, item, utente, TipoOperazione.Insert);

            conn.Open();

            // Recupero l'ID e lo assegno sia all'oggetto che come valore di ritorno
            var result = cmd.ExecuteScalar();

            if (result != null && result != DBNull.Value)
            {
                int newId = Convert.ToInt32(result);
                item.IdProspettoCobral = newId;
                return newId;
            }

            throw new Exception("Errore nel recupero dell'ID dopo l'inserimento.");
        }

        // ---------------------------------------------------------
        // INSERT (transazionale)
        // ---------------------------------------------------------
        public ProspettoCobral Insert(ProspettoCobral item, SqlConnection conn, SqlTransaction tran, string utente)

        {
            using var cmd = new SqlCommand("spProspettoCobral_Insert_Tran", conn, tran)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(cmd, item, utente, TipoOperazione.InsertTran);

            var result = cmd.ExecuteScalar();
            item.IdProspettoCobral = Convert.ToInt32(result);
            return item;
        }

        // ---------------------------------------------------------
        // INSERT RANGE (transazionale)
        // ---------------------------------------------------------

        public void InsertRange(IEnumerable<ProspettoCobral> items, SqlConnection conn, SqlTransaction tran, string utente)
        {
            foreach (var item in items)
            {
                Insert(item, conn, tran, utente);
            }
        }


        public void InsertRange(IEnumerable<ProspettoCobral> items, string utente)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            foreach (var item in items)
            {
                Insert(item, utente); // versione standard
            }
        }

        #endregion
        #region UPDATE

        // ---------------------------------------------------------
        // UPDATE
        // ---------------------------------------------------------
        public void Update(ProspettoCobral item, string utente)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoCobral_Update", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(cmd, item, utente, TipoOperazione.Update);
            cmd.Parameters.Add("@IdProspettoCobral", SqlDbType.Int).Value = item.IdProspettoCobral;

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        // ---------------------------------------------------------
        // RESET STATO FATTURAZIONE
        // ---------------------------------------------------------
        /// <summary>
        /// Annulla lo stato di fatturazione di un prospetto specifico.
        /// Ripristina i campi ai valori di default e aggiorna il timestamp di modifica.
        /// </summary>
        /// <param name="idProspettoCobral">Chiave primaria del record da resettare.</param>
        /// <returns>True se l'aggiornamento è avvenuto con successo, False se l'ID non è stato trovato.</returns>
        public bool ResetFatturazione(int idProspettoCobral, string utente)
        {
            //13/10/2026 inserito DaFatturare se cancello la fattura devo impostarla come da fatturare 
            string sql = @"
                UPDATE ProspettoCobral
                SET 
                    Fatturata = CASE 
                    WHEN Fatturata = 1 THEN 0 
                        ELSE 1 
                        END,
                    DaFatturare = CASE
                    WHEN Fatturata = 1 THEN 1
                        ELSE 0
                        END,
                    DataFatturazione = CASE 
                    WHEN Fatturata = 1 THEN NULL 
                        ELSE GETDATE() 
                        END,
        
                Utente = @Utente
                WHERE IdProspettoCobral = @Id;
                    ";

            // Gestione della connessione con blocco using per garantire il rilascio delle risorse (Dispose)
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                // Parametrizzazione della query per prevenire SQL Injection
                cmd.Parameters.AddWithValue("@Id", idProspettoCobral);
                cmd.Parameters.AddWithValue("@Utente", utente);
                conn.Open();

                // Esegue il comando e restituisce true se almeno una riga è stata modificata
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        #endregion
        #region DELETE

        // ---------------------------------------------------------
        // DELETE
        // ---------------------------------------------------------

        // ==== MODIFICA 24/10/2026 - INIZIO ==== Eliminazione fisica condizionata (usata dall'Agente)
        // Atomica: possesso, cestino e "non fatturata" sono verificati nella stessa DELETE.
        public bool DeleteSeNonFatturata(int id, int? idAgente)
        {
            const string sql = @"
            DELETE FROM ProspettoCobral
            WHERE IdProspettoCobral = @Id
            AND IsDeleted = 1
            AND ISNULL(Fatturata, 0) = 0
            AND (@IdAgente IS NULL OR IdAgente = @IdAgente);";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = (object?)idAgente ?? DBNull.Value;

            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }
        // ==== MODIFICA 24/10/2026 - FINE ====


        public void Delete(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoCobral_Delete", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdProspettoCobral", SqlDbType.Int).Value = id;

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void Delete(int id, SqlConnection conn, SqlTransaction tran)
        {
            using var cmd = new SqlCommand("spProspettoCobral_Delete", conn, tran)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdProspettoCobral", SqlDbType.Int).Value = id;
            cmd.ExecuteNonQuery();
        }

        public void SoftDelete(int id, string utente)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoCobral_SoftDelete", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@IdProspettoCobral", id);
            cmd.Parameters.AddWithValue("@Utente", utente);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void Ripristina(int id, string utente)
        {
            // Qui facciamo una UPDATE secca perché non serve una SP complessa, 
            // a meno che tu non voglia loggare chi ha ripristinato.
            using var conn = new SqlConnection(_connectionString);
            string sql = @"UPDATE ProspettoCobral 
                   SET IsDeleted = 0, 
                       DataModifica = GETDATE(), 
                       Utente = @Utente 
                   WHERE IdProspettoCobral = @Id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@Utente", utente);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        #endregion
        #region GET

        // ---------------------------------------------------------
        // GET BY ID
        // ---------------------------------------------------------
        public ProspettoCobral? GetById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoCobral_GetById", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdProspettoCobral", SqlDbType.Int).Value = id;

            conn.Open();
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            return Map(reader);
        }

        // ---------------------------------------------------------
        // GET BY FILE
        // ---------------------------------------------------------
        public IEnumerable<ProspettoCobral> GetByFile(int idFileImportato)
        {
            var lista = new List<ProspettoCobral>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoCobral_GetByFile", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdFileImportato", SqlDbType.Int).Value = idFileImportato;

            conn.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(Map(reader));
            }

            return lista;
        }

        // ---------------------------------------------------------
        //         public IEnumerable<ProspettoCobral> GetAll()
        // ---------------------------------------------------------
        public IEnumerable<ProspettoCobral> GetAll()
        {
            var lista = new List<ProspettoCobral>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("[spProspettoCobral_GetAll]", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            conn.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(Map(reader));
            }

            return lista;
        }

        public List<string> GetDimensioniMateriale(int idMateriale, string tipo)
        {
            var lista = new List<string>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spGetDimensioniMateriale", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Parametri tipizzati
            cmd.Parameters.Add(new SqlParameter("@IdMateriale", SqlDbType.Int) { Value = idMateriale });
            cmd.Parameters.Add(new SqlParameter("@Tipo", SqlDbType.NVarChar, 20) { Value = tipo });

            conn.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(reader.GetString(0)); // Valore
            }

            return lista;
        }

        // 31/03/2026: Aggiunto parametro opzionale mostraEliminati
        // 07/08/2026 Firma aggiornata 
        public IEnumerable<ProspettoCobral> GetFiltrati(
            int? idMandatario, int? idFile, int? anno, int? mese, int? fatturata,
            bool mostraEliminati = false, int? idTipoPagamento = null,
            int? idAgente = null, int? daFatturare = null
           

            )   // NUOVO 30/09/2026
        {
            var lista = new List<ProspettoCobral>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoCobral_GetAllFiltri", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@IdMandatario", SqlDbType.Int) { Value = (object)idMandatario ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@IdFileImportato", SqlDbType.Int) { Value = (object)idFile ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@Anno", SqlDbType.Int) { Value = (object)anno ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@Mese", SqlDbType.Int) { Value = (object)mese ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@Fatturata", SqlDbType.Int) { Value = (object)fatturata ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@MostraEliminati", SqlDbType.Bit) { Value = mostraEliminati });
            cmd.Parameters.Add(new SqlParameter("@IdTipoPagamento", SqlDbType.Int) { Value = (object?)idTipoPagamento ?? DBNull.Value });
            // 07/08/2026
            cmd.Parameters.Add(new SqlParameter("@IdAgente", SqlDbType.Int) { Value = (object?)idAgente ?? DBNull.Value }); // 07/08/2026
            cmd.Parameters.Add(new SqlParameter("@DaFatturare", SqlDbType.Int) { Value = (object?)daFatturare ?? DBNull.Value }); // NUOVO 30/09/2026


            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                lista.Add(Map(reader));

            return lista;
        }

        #endregion
        #region FATTURAZIONE

        public void ImpostaComeFatturate(int idMandatario, int anno, int mese, int idCliente, int idTipoPagamento)//01/04/2026 - Aggiunta filtro per IdTipoPagamento per gestire casi specifici di fatturazione parziale
        {

            //13/10/2026 inserito DaFatturare = 0 
            string SQL_DEBUG = $@"
            UPDATE ProspettoCobral
            SET 
            Fatturata = 1,
            DaFatturare = 0 ,
            DataFatturazione = GETDATE()
            WHERE 
            IdMandatario = {idMandatario}
            AND Anno = {anno}
            AND Mese = {mese}
            AND IdCliente = {idCliente}
            AND IdTipoPagamento = {idTipoPagamento}
            AND (Fatturata = 0 OR Fatturata IS NULL);";


            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(SQL_DEBUG, conn))
            {
                cmd.CommandType = CommandType.Text;

                cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = idMandatario;
                cmd.Parameters.Add("@Anno", SqlDbType.Int).Value = anno;
                cmd.Parameters.Add("@Mese", SqlDbType.Int).Value = mese;
                cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = idCliente;
                cmd.Parameters.Add("@IdTipoPagamento", SqlDbType.Int).Value = idTipoPagamento;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }



        #endregion

        #region PARAMETRI

        // ---------------------------------------------------------
        // PARAMETRI COMUNI
        // ---------------------------------------------------------
        private static void AddParameters(SqlCommand cmd, ProspettoCobral item, string utente, TipoOperazione operazione)
        {

            // AddParameters Gestisce Insert e Update, quindi:
            // -OBBLIGATORIO= Ogni cmd.Parameters.Add("@Parametro") della funzione AddParameters DEVE esistere nella Stored.

            // -OBBLIGATORIO = Se la Stored aspetta un parametro deve essere presente in AddParameters, anche se il valore è null (in questo caso va passato DBNull.Value).
            // (A meno che il parametro in SQL non abbia un valore di default tipo = NULL).
            // -----------------------------------------------------------------------------------------


            // 🔹 PARAMETRI SPECIFICI PER OPERAZIONE
            switch (operazione)
            {
                case TipoOperazione.InsertTran:
                case TipoOperazione.Insert:

                    cmd.Parameters.Add("@IdFileImportato", SqlDbType.Int).Value = item.IdFileImportato;
                    cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = item.IdMandatario;
                    cmd.Parameters.Add("@IdImportCobral", SqlDbType.Int).Value = item.IdImportCobral;
                    cmd.Parameters.Add("@Anno", SqlDbType.Int).Value = (object?)item.Anno ?? DBNull.Value;
                    cmd.Parameters.Add("@Mese", SqlDbType.Int).Value = (object?)item.Mese ?? DBNull.Value;
                    break;

                case TipoOperazione.Update:
                    break;
            }

            // Parametri comuni
            cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = item.IdCliente;
            cmd.Parameters.Add("@IdMateriale", SqlDbType.Int).Value = item.IdMateriale;
            cmd.Parameters.Add("@IdUnitaMisura", SqlDbType.Int).Value = item.IdUnitaMisura;

            cmd.Parameters.Add("@NumeroOrdine", SqlDbType.NVarChar).Value = (object?)item.NumeroOrdine ?? DBNull.Value;
            cmd.Parameters.Add("@NumeroDDT", SqlDbType.NVarChar).Value = (object?)item.NumeroDDT ?? DBNull.Value;
            cmd.Parameters.Add("@NumeroFattura", SqlDbType.NVarChar).Value = (object?)item.NumeroFattura ?? DBNull.Value;

            // DECIMALI (18,3)
            AddDecimal(cmd, "@Quantita", item.Quantita, 18, 6);
            AddDecimal(cmd, "@Prezzo", item.Prezzo, 18, 6);

            cmd.Parameters.Add("@DataRiferimentoPrezzo", SqlDbType.Date).Value = (object?)item.DataRiferimentoPrezzo ?? DBNull.Value;

            cmd.Parameters.Add("@Spessore", SqlDbType.NVarChar).Value = (object?)item.Spessore ?? DBNull.Value;
            cmd.Parameters.Add("@Larghezza", SqlDbType.NVarChar).Value = (object?)item.Larghezza ?? DBNull.Value;

            AddDecimal(cmd, "@Provvigione", item.Provvigione, 18, 6);

            AddDecimal(cmd, "@AlluminioSpessore", item.AlluminioSpessore, 18, 6);
            AddDecimal(cmd, "@OttoneSpessore", item.OttoneSpessore, 18, 6);
            AddDecimal(cmd, "@RameSpessore", item.RameSpessore, 18, 6);
            AddDecimal(cmd, "@AltrePercentuali", item.AltrePercentuali, 18, 6);
            AddDecimal(cmd, "@PrLavSpess", item.PrLavSpess, 18, 6);

            AddDecimal(cmd, "@AlluminioLarghezza", item.AlluminioLarghezza, 18, 6);
            AddDecimal(cmd, "@OttoneLarghezza", item.OttoneLarghezza, 18, 6);
            AddDecimal(cmd, "@RameLarghezza", item.RameLarghezza, 18, 6);
            AddDecimal(cmd, "@BronzoLarghezza", item.BronzoLarghezza, 18, 6);
            AddDecimal(cmd, "@PrLavLarg", item.PrLavLarg, 18, 6);

            AddDecimal(cmd, "@ExtraPrezzoKg", item.ExtraPrezzoKg, 18, 6);
            AddDecimal(cmd, "@ExtraPrezzoStagnato", item.ExtraPrezzoStagnato, 18, 6);

            AddDecimal(cmd, "@PrLavTotale", item.PrLavTotale, 18, 6);
            AddDecimal(cmd, "@ValoreCommissioni", item.ValoreCommissioni, 18, 6);
            AddDecimal(cmd, "@PrezzoVendita", item.PrezzoVendita, 18, 6);
            AddDecimal(cmd, "@Differenza", item.Differenza, 18, 6);

            AddDecimal(cmd, "@ProvvigioneAgente", item.ProvvigioneAgente, 18, 6);

            cmd.Parameters.Add("@DataConsegnaIpotetica", SqlDbType.Date).Value = (object?)item.DataConsegnaIpotetica ?? DBNull.Value;

            // ==== MODIFICA 28/09/2026 - INIZIO ==== Gestione Ordini in Ritardo //2
            cmd.Parameters.Add("@DataConsegna", SqlDbType.Date).Value = (object?)item.DataConsegna ?? DBNull.Value;
            cmd.Parameters.Add("@EscludiControlloRitardo", SqlDbType.Bit).Value = item.EscludiControlloRitardo;
            // ==== MODIFICA 28/09/2026 - FINE ====          

            // MODIFICA 05/10/2026 MISURE STATISTICHE (18,3) - INIZIO
            AddDecimal(cmd, "@SpessoreNum", item.SpessoreNum, 18, 3);
            AddDecimal(cmd, "@LarghezzaNum", item.LarghezzaNum, 18, 3);
            AddDecimal(cmd, "@DiametroNum", item.DiametroNum, 18, 3);
            // MODIFICA 05/10/2026 MISURE STATISTICHE (18,3) - FINE

            cmd.Parameters.Add("@DaFatturare", SqlDbType.Bit).Value = item.DaFatturare;//30/09/2026: Aggiunto campo DaFatturare
            cmd.Parameters.Add("@Scadenza", SqlDbType.Date).Value = (object?)item.Scadenza ?? DBNull.Value;

            cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = (object?)item.IdAgente ?? DBNull.Value;
            cmd.Parameters.Add("@IdTipoPagamento", SqlDbType.Int).Value = (object?)item.IdTipoPagamento ?? DBNull.Value;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = utente;
        }

        // ---------------------------------------------------------
        // HELPER DECIMAL
        // ---------------------------------------------------------
        private static void AddDecimal(SqlCommand cmd, string name, decimal? value, byte precision, byte scale)
        {
            var p = cmd.Parameters.Add(name, SqlDbType.Decimal);
            p.Precision = precision;
            p.Scale = scale;
            p.Value = (object?)value ?? DBNull.Value;
        }


        #endregion
        #region MAPPATURA

        // ---------------------------------------------------------
        // MAPPATURA
        // ---------------------------------------------------------
        private static ProspettoCobral Map(SqlDataReader reader)
        {
            // Map=Mappatura serve per le SELECT, quindi:
            //Obbligatorio = Tutti i campi della Map devono essere nella Stored altrimenti la Funzione va in errore 

            //Opzionale= I campi della Stored possono anche non essere presenti nella Map,
            //se sono presenti devono essere mappati correttamente.

            var p = new ProspettoCobral();

            // Identificativi
            p.IdProspettoCobral = reader.GetIntSafe("IdProspettoCobral");
            p.IdFileImportato = reader.GetIntSafe("IdFileImportato");
            p.IdMandatario = reader.GetIntSafe("IdMandatario");
            p.IdImportCobral = reader.GetIntSafe("IdImportCobral");//16/04/2026
            // Numeri documento
            p.NumeroOrdine = reader.GetStringSafe("NumeroOrdine");
            p.NumeroDDT = reader.GetStringSafe("NumeroDDT");
            p.NumeroFattura = reader.GetStringSafe("NumeroFattura");

            // Cliente
            p.IdCliente = reader.GetIntSafe("IdCliente");
            p.RagioneSociale = reader.GetStringSafe("RagioneSociale");

            // Materiale
            p.IdMateriale = reader.GetIntSafe("IdMateriale");
            p.DescrizioneMateriale = reader.GetStringSafe("DescrizioneMateriale");

            // Unità di misura
            p.IdUnitaMisura = reader.GetIntSafe("IdUnitaMisura");
            p.UnitaMisura = reader.GetStringSafe("UnitaMisura");

            // Quantità e prezzi
            p.Quantita = reader.GetNullableDecimal("Quantita");
            p.Prezzo = reader.GetNullableDecimal("Prezzo");
            p.PrezzoVendita = reader.GetNullableDecimal("PrezzoVendita");
            p.Differenza = reader.GetNullableDecimal("Differenza");

            // Date
            p.DataRiferimentoPrezzo = reader.GetNullableDateTime("DataRiferimentoPrezzo");
            p.DataConsegnaIpotetica = reader.GetNullableDateTime("DataConsegnaIpotetica");

            // ==== MODIFICA 28/09/2026 - INIZIO ==== Gestione Ordini in Ritardo //3
            p.DataConsegna = reader.GetNullableDateTime("DataConsegna");
            p.EscludiControlloRitardo = reader.GetBoolSafe("EscludiControlloRitardo");
            // ==== MODIFICA 28/09/2026 - FINE ====            
            p.DaFatturare = reader.GetBoolSafe("DaFatturare");//30/09/2026: Aggiunto campo DaFatturare
            p.Scadenza = reader.GetNullableDateTime("Scadenza");

            // Caratteristiche tecniche
            p.Spessore = reader.GetStringSafe("Spessore");
            p.Larghezza = reader.GetStringSafe("Larghezza");

            p.Provvigione = reader.GetNullableDecimal("Provvigione");
            p.AlluminioSpessore = reader.GetNullableDecimal("AlluminioSpessore");
            p.OttoneSpessore = reader.GetNullableDecimal("OttoneSpessore");
            p.RameSpessore = reader.GetNullableDecimal("RameSpessore");
            p.AltrePercentuali = reader.GetNullableDecimal("AltrePercentuali");
            p.PrLavSpess = reader.GetNullableDecimal("PrLavSpess");
            p.AlluminioLarghezza = reader.GetNullableDecimal("AlluminioLarghezza");
            p.OttoneLarghezza = reader.GetNullableDecimal("OttoneLarghezza");
            p.RameLarghezza = reader.GetNullableDecimal("RameLarghezza");
            p.BronzoLarghezza = reader.GetNullableDecimal("BronzoLarghezza");
            p.PrLavLarg = reader.GetNullableDecimal("PrLavLarg");
            p.ExtraPrezzoKg = reader.GetNullableDecimal("ExtraPrezzoKg");
            p.ExtraPrezzoStagnato = reader.GetNullableDecimal("ExtraPrezzoStagnato");
            p.PrLavTotale = reader.GetNullableDecimal("PrLavTotale");
            p.ValoreCommissioni = reader.GetNullableDecimal("ValoreCommissioni");

            // Agente e pagamento
            p.IdAgente = reader.GetIntSafe("IdAgente");
            p.ProvvigioneAgente = reader.GetNullableDecimal("ProvvigioneAgente");
            p.IdTipoPagamento = reader.GetIntSafe("IdTipoPagamento");

            // Audit
            p.DataInserimento = reader.GetDateTimeSafe("DataInserimento");
            p.DataModifica = reader.GetNullableDateTime("DataModifica");
            p.Utente = reader.GetStringSafe("Utente");

            // Anno e Mese
            p.Anno = reader.GetNullableInt("Anno");
            p.Mese = reader.GetNullableInt("Mese");



            // Stato fatturazione
            p.Fatturata = reader.GetIntSafe("Fatturata");
           // Debugger.Break();// capire se è necessario che DataFatturazione venga letta //25/04/2026
            p.DataFatturazione = reader.GetNullableDateTime("DataFatturazione");//25/04/2026

            // MODIFICA 05/10/2026 MISURE STATISTICHE (18,3) - INIZIO
            p.SpessoreNum = reader.GetNullableDecimal("SpessoreNum");
            p.LarghezzaNum = reader.GetNullableDecimal("LarghezzaNum");
            p.DiametroNum = reader.GetNullableDecimal("DiametroNum");
            // MODIFICA 05/10/2026 MISURE STATISTICHE (18,3) - FINE

            // Soft delete
            p.IsDeleted = reader.GetBoolSafe("IsDeleted");

            //// ==== MODIFICA 28/09/2026 - INIZIO ==== Calcolo dinamico ritardo//1
            //var dataAttivazioneModulo = new DateTime(2026, 9, 28);
            //RitardoOrdiniCalculator.Calcola(p, dataAttivazioneModulo);
            //// ==== MODIFICA 28/09/2026 - FINE ====

            return p;
        }

        #endregion
        #region SUPPORTO

        // ---------------------------------------------------------
        // RECUPERO PERCENTUALE PROVVIGIONE AGENTE DINAMICA
        // ---------------------------------------------------------
        public decimal GetPercentualeAgente(int idAgente, int idCliente, int idMandatario)
        {
            using var conn = new SqlConnection(_connectionString);

            // FIX 07/08/2026: la funzione SQL ora richiede anche @ProvvigioneManuale
            // (aggiunto in un secondo momento, la chiamata C# non era mai stata aggiornata).
            // Passiamo DBNull: in questo contesto (lookup generico Agente+Cliente) non c'è
            // nessun valore manuale noto sull'ordine — la STEP 0 della funzione SQL lo salterà
            // e proseguirà con la gerarchia normale (accordo specifico → default agente).
            using var cmd = new SqlCommand(
                "SELECT dbo.fn_GetPercentualeProvvigione(@IdAgente, @IdCliente, @IdMandatario, @ProvvigioneManuale)", // 07/08/2026
                conn);

            cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = idAgente;
            cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = idCliente;
            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = idMandatario;
            cmd.Parameters.Add("@ProvvigioneManuale", SqlDbType.Decimal, 5).Value = DBNull.Value; // 07/08/2026 NUOVO

            conn.Open();

            var result = cmd.ExecuteScalar();

            if (result != null && result != DBNull.Value)
                return Convert.ToDecimal(result);

            return 0m;
        }

        #endregion
    }
}
