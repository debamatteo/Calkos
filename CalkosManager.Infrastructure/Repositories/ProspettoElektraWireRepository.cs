#region USING
using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Infrastructure.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;
#endregion

/*
    ============================================================
    IMPORT ElektraWire REPOSITORY — PROMEMORIA ARCHITETTURALE
    ============================================================

    Questo repository gestisce la tabella ImportElektraWire, che contiene
    tutte le righe importate dai file ElektraWire prima della validazione
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
      - Nessuna modifica alla logica di business o al flusso ElektraWire.

    Questo repository è considerato “sensibile”: ogni modifica deve
    essere minima, motivata e verificata con attenzione, per evitare
    impatti sul processo di importazione ElektraWire.
    ============================================================
*/

namespace CalkosManager.Infrastructure.Repositories
{
    public class ProspettoElektraWireRepository : IProspettoElektraWireRepository
    {
        #region FIELDS & CONSTRUCTOR

        private readonly string _connectionString;

        public ProspettoElektraWireRepository(string connectionString)
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


        public int Insert(ProspettoElektraWire item, string utente)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoElektraWire_Insert", conn)
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
                item.IdProspettoElektraWire = newId;
                return newId;
            }

            throw new Exception("Errore nel recupero dell'ID dopo l'inserimento.");
        }

        // ---------------------------------------------------------
        // INSERT (transazionale)
        // ---------------------------------------------------------
        public ProspettoElektraWire Insert(ProspettoElektraWire item, SqlConnection conn, SqlTransaction tran, string utente)

        {
            
            using var cmd = new SqlCommand("spProspettoElektraWire_Insert_Tran", conn, tran)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(cmd, item, utente, TipoOperazione.InsertTran);

            var result = cmd.ExecuteScalar();
            item.IdProspettoElektraWire = Convert.ToInt32(result);
            return item;
        }

        // ---------------------------------------------------------
        // INSERT RANGE (transazionale)
        // ---------------------------------------------------------

        public void InsertRange(IEnumerable<ProspettoElektraWire> items, SqlConnection conn, SqlTransaction tran, string utente)
        {
            foreach (var item in items)
            {
                Insert(item, conn, tran, utente);
            }
        }


        public void InsertRange(IEnumerable<ProspettoElektraWire> items, string utente)
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
        public void Update(ProspettoElektraWire item, string utente)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoElektraWire_Update", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(cmd, item, utente, TipoOperazione.Update);
            cmd.Parameters.Add("@IdProspettoElektraWire", SqlDbType.Int).Value = item.IdProspettoElektraWire;

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
        /// <param name="idProspettoElektraWire">Chiave primaria del record da resettare.</param>
        /// <returns>True se l'aggiornamento è avvenuto con successo, False se l'ID non è stato trovato.</returns>
        public bool ResetFatturazione(int idProspettoElektraWire, string utente)
        {

            //13/10/2026 inserito DaFatturare se cancello la fattura devo impostarla come da fatturare 
            string sql = @"
                UPDATE ProspettoElektraWire
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
                DataModifica = GETDATE(),
                Utente = @Utente
                WHERE IdProspettoElektraWire = @Id;
                ";


            // Gestione della connessione con blocco using per garantire il rilascio delle risorse (Dispose)
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                // Parametrizzazione della query per prevenire SQL Injection
                cmd.Parameters.AddWithValue("@Id", idProspettoElektraWire);
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
            DELETE FROM ProspettoElektraWire
            WHERE IdProspettoElektraWire = @Id
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
            using var cmd = new SqlCommand("spProspettoElektraWire_Delete", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdProspettoElektraWire", SqlDbType.Int).Value = id;

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void Delete(int id, SqlConnection conn, SqlTransaction tran)
        {
            using var cmd = new SqlCommand("spProspettoElektraWire_Delete", conn, tran)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdProspettoElektraWire", SqlDbType.Int).Value = id;
            cmd.ExecuteNonQuery();
        }

        public void SoftDelete(int id, string utente)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoElektraWire_SoftDelete", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@IdProspettoElektraWire", id);
            cmd.Parameters.AddWithValue("@Utente", utente);

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void Ripristina(int id, string utente)
        {
            // Qui facciamo una UPDATE secca perché non serve una SP complessa, 
            // a meno che tu non voglia loggare chi ha ripristinato.
            using var conn = new SqlConnection(_connectionString);
            string sql = @"UPDATE ProspettoElektraWire 
                   SET IsDeleted = 0, 
                       DataModifica = GETDATE(), 
                       Utente = @Utente 
                   WHERE IdProspettoElektraWire = @Id";

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
        public ProspettoElektraWire? GetById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoElektraWire_GetById", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdProspettoElektraWire", SqlDbType.Int).Value = id;

            conn.Open();
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            return Map(reader);
        }

        // ---------------------------------------------------------
        // GET BY FILE
        // ---------------------------------------------------------
        public IEnumerable<ProspettoElektraWire> GetByFile(int idFileImportato)
        {
            var lista = new List<ProspettoElektraWire>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoElektraWire_GetByFile", conn)
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
        //         public IEnumerable<ProspettoElektraWire> GetAll()
        // ---------------------------------------------------------
        public IEnumerable<ProspettoElektraWire> GetAll()
        {
            var lista = new List<ProspettoElektraWire>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("[spProspettoElektraWire_GetAll]", conn)
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
        public IEnumerable<ProspettoElektraWire> GetFiltrati(int? idMandatario, int? idFile, int? anno, int? mese, int? fatturata, 
            bool mostraEliminati = false, int? idTipoPagamento = null,
            int? idAgente = null, int? daFatturare = null)   // NUOVO 30/09/2026
        {
            var lista = new List<ProspettoElektraWire>();

            using var conn = new SqlConnection(_connectionString);

            using var cmd = new SqlCommand("spProspettoElektraWire_GetAllFiltri", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            // --- PARAMETRI ---
            cmd.Parameters.Add(new SqlParameter("@IdMandatario", SqlDbType.Int) { Value = (object)idMandatario ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@IdFileImportato", SqlDbType.Int) { Value = (object)idFile ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@Anno", SqlDbType.Int) { Value = (object)anno ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@Mese", SqlDbType.Int) { Value = (object)mese ?? DBNull.Value });

            // 25/03/2026: Filtro Fatturazione
            cmd.Parameters.Add(new SqlParameter("@Fatturata", SqlDbType.Int) { Value = (object)fatturata ?? DBNull.Value });

            // 31/03/2026: Filtro Soft Delete (Cestino)
            cmd.Parameters.Add(new SqlParameter("@MostraEliminati", SqlDbType.Bit) { Value = mostraEliminati });
            cmd.Parameters.Add("@IdTipoPagamento", SqlDbType.Int).Value = (object?)idTipoPagamento ?? DBNull.Value;//22/05/2026 - Aggiunta parametro opzionale per filtro TipoPagamento
            cmd.Parameters.Add(new SqlParameter("@IdAgente", SqlDbType.Int) { Value = (object?)idAgente ?? DBNull.Value }); // NUOVO 07/08/2026
            cmd.Parameters.Add(new SqlParameter("@DaFatturare", SqlDbType.Int) { Value = (object?)daFatturare ?? DBNull.Value }); // NUOVO 30/09/2026

            conn.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(Map(reader));
            }

            return lista;
        }

        #endregion
        #region FATTURAZIONE

        public void ImpostaComeFatturate(int idMandatario, int anno, int mese, int idCliente, int idTipoPagamento)//01/04/2026 - Aggiunta filtro per IdTipoPagamento per gestire casi specifici di fatturazione parziale
        {

            //13/10/2026 inserito DaFatturare = 0 
            string SQL_DEBUG = $@"
            UPDATE ProspettoElektraWire
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
        private static void AddParameters(SqlCommand cmd, ProspettoElektraWire item, string utente, TipoOperazione operazione)
        {

            // AddParameters Gestisce Insert e Update, quindi:
            // -OBBLIGATORIO= Ogni cmd.Parameters.Add("@Parametro") della funzione AddParameters DEVE esistere nella Stored.

            // -OBBLIGATORIO = Se la Stored aspetta un parametro deve essere presente in AddParameters, anche se il valore è null (in questo caso va passato DBNull.Value).
            // (A meno che il parametro in SQL non abbia un valore di default tipo = NULL).
            // -            
            // 🔹 PARAMETRI SPECIFICI PER OPERAZIONE
            switch (operazione)
            {
                case TipoOperazione.InsertTran:
                case TipoOperazione.Insert:
                    cmd.Parameters.Add("@IdFileImportato", SqlDbType.Int).Value = item.IdFileImportato;
                    cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = item.IdMandatario;
                    cmd.Parameters.Add("@IdImportElektraWire", SqlDbType.Int).Value = item.IdImportElektraWire;
                    cmd.Parameters.Add("@Anno", SqlDbType.Int).Value = (object?)item.Anno ?? DBNull.Value;
                    cmd.Parameters.Add("@Mese", SqlDbType.Int).Value = (object?)item.Mese ?? DBNull.Value;
                    break;
                case TipoOperazione.Update:
                    break;
            }

            // Identificativi base
            cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = item.IdCliente;
            cmd.Parameters.Add("@IdMateriale", SqlDbType.Int).Value = item.IdMateriale;
            cmd.Parameters.Add("@IdUnitaMisura", SqlDbType.Int).Value = item.IdUnitaMisura;

            // Documento
            cmd.Parameters.Add("@NumeroOrdine", SqlDbType.NVarChar).Value = (object?)item.NumeroOrdine ?? DBNull.Value;
            cmd.Parameters.Add("@NumeroDDT", SqlDbType.NVarChar).Value = (object?)item.NumeroDDT ?? DBNull.Value;
            cmd.Parameters.Add("@NumeroFattura", SqlDbType.NVarChar).Value = (object?)item.NumeroFattura ?? DBNull.Value;
            cmd.Parameters.Add("@Misure", SqlDbType.NVarChar).Value = (object?)item.Misure ?? DBNull.Value;

            // Quantità e prezzi (18,3)
            AddDecimal(cmd, "@Quantita", item.Quantita, 18, 6);
            AddDecimal(cmd, "@CostoLavorazione", item.CostoLavorazione, 18, 6);
            AddDecimal(cmd, "@PrezzoBase", item.PrezzoBase, 18, 6);
            AddDecimal(cmd, "@PrezzoConLavorazione", item.PrezzoConLavorazione, 18, 6);
            AddDecimal(cmd, "@PrezzoVendita", item.PrezzoVendita, 18, 6);
            AddDecimal(cmd, "@DeltaMargine", item.DeltaMargine, 18, 6);
            AddDecimal(cmd, "@Perc_Delta_Provvigione", item.Perc_Delta_Provvigione, 18, 6);//27/04/2026: Aggiunta Percentuale Delta Provvigione

            // Blocchi provvigionali (18,5)
            AddDecimal(cmd, "@ValUnitProvvigioneFissa1_5", item.ValUnitProvvigioneFissa1_5, 18, 6);
            AddDecimal(cmd, "@Delta1ProvvigioneFissa1_5", item.Delta1ProvvigioneFissa1_5, 18, 6);
            AddDecimal(cmd, "@Delta2ProvvigioneFissa1_5", item.Delta2ProvvigioneFissa1_5, 18, 6);
            AddDecimal(cmd, "@TotProvvigioneFissa1_5", item.TotProvvigioneFissa1_5, 18, 6);
            AddDecimal(cmd, "@ValQuotaFissa_1_5", item.ValQuotaFissa_1_5, 18, 6);

            AddDecimal(cmd, "@ValUnitProvvigioneFissa_03", item.ValUnitProvvigioneFissa_03, 18, 6);
            AddDecimal(cmd, "@Delta1ProvvigioneFissa_03", item.Delta1ProvvigioneFissa_03, 18, 6);
            AddDecimal(cmd, "@Delta2ProvvigioneFissa_03", item.Delta2ProvvigioneFissa_03, 18, 6);
            AddDecimal(cmd, "@TotProvvigioneFissa_03", item.TotProvvigioneFissa_03, 18, 6);
            AddDecimal(cmd, "@ValQuotaFissa_03", item.ValQuotaFissa_03, 18, 6);

            AddDecimal(cmd, "@TotaleFissoProvvigione", item.TotaleFissoProvvigione, 18, 6);
            AddDecimal(cmd, "@DeltaResiduoVariabile", item.DeltaResiduoVariabile, 18, 6);
            AddDecimal(cmd, "@TotaleVariabileProvvigione", item.TotaleVariabileProvvigione, 18, 6);
            AddDecimal(cmd, "@TotaleSommaProvvigioni", item.TotaleSommaProvvigioni, 18, 6);

            // Totali e agente
            AddDecimal(cmd, "@TotaleProvvigioneDaFatturare", item.TotaleProvvigioneDaFatturare, 18, 6);
            AddDecimal(cmd, "@ProvvigioneAgente", item.ProvvigioneAgente, 18, 6);

            // Date e lookup
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

            // 
            cmd.Parameters.Add("@DaFatturare", SqlDbType.Bit).Value = item.DaFatturare;//30/09/2026: Aggiunto campo DaFatturare

            cmd.Parameters.Add("@Scadenza", SqlDbType.Date).Value = (object?)item.Scadenza ?? DBNull.Value;//18/04/2026: Aggiunta DataScadenza
            cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = (object?)item.IdAgente ?? DBNull.Value;
            cmd.Parameters.Add("@IdTipoPagamento", SqlDbType.Int).Value = (object?)item.IdTipoPagamento ?? DBNull.Value;

            // Audit
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
        private static ProspettoElektraWire Map(SqlDataReader reader)
        {

            // Map=Mappatura serve per le SELECT, quindi:
            //Obbligatorio = Tutti i campi della Map devono essere nella Stored altrimenti la Funzione va in errore 

            //Opzionale= I campi della Stored possono anche non essere presenti nella Map,
            //se sono presenti devono essere mappati correttamente.


            var p = new ProspettoElektraWire();

            // Identificativi
            p.IdProspettoElektraWire = reader.GetIntSafe("IdProspettoElektraWire");
            p.IdFileImportato = reader.GetIntSafe("IdFileImportato");
            p.IdMandatario = reader.GetIntSafe("IdMandatario");            
            p.IdImportElektraWire = reader.GetIntSafe("IdImportElektraWire");

            // Documento
            p.NumeroOrdine = reader.GetStringSafe("NumeroOrdine");
            p.NumeroDDT = reader.GetStringSafe("NumeroDDT");
            p.NumeroFattura = reader.GetStringSafe("NumeroFattura");
            // Cliente
            p.IdCliente = reader.GetIntSafe("IdCliente");
            p.RagioneSociale = reader.GetStringSafe("RagioneSociale");


            // Materiale
            p.IdMateriale = reader.GetIntSafe("IdMateriale");
            p.IdUnitaMisura = reader.GetIntSafe("IdUnitaMisura");
            p.DescrizioneMateriale = reader.GetStringSafe("DescrizioneMateriale");
            p.Misure = reader.GetStringSafe("Misure");

            // Quantità e prezzi
            p.Quantita = reader.GetNullableDecimal("Quantita");
            p.CostoLavorazione = reader.GetNullableDecimal("CostoLavorazione");
            p.PrezzoBase = reader.GetNullableDecimal("PrezzoBase");
            p.PrezzoConLavorazione = reader.GetNullableDecimal("PrezzoConLavorazione");
            p.PrezzoVendita = reader.GetNullableDecimal("PrezzoVendita");
            p.DeltaMargine = reader.GetNullableDecimal("DeltaMargine");
            p.Perc_Delta_Provvigione = reader.GetNullableDecimal("Perc_Delta_Provvigione");//27/04/2026: Aggiunta Percentuale Delta Provvigione

            // Blocchi provvigionali (18,5)
            p.ValUnitProvvigioneFissa1_5 = reader.GetNullableDecimal("ValUnitProvvigioneFissa1_5");
            p.Delta1ProvvigioneFissa1_5 = reader.GetNullableDecimal("Delta1ProvvigioneFissa1_5");
            p.Delta2ProvvigioneFissa1_5 = reader.GetNullableDecimal("Delta2ProvvigioneFissa1_5");
            p.TotProvvigioneFissa1_5 = reader.GetNullableDecimal("TotProvvigioneFissa1_5");
            p.ValQuotaFissa_1_5 = reader.GetNullableDecimal("ValQuotaFissa_1_5");

            p.ValUnitProvvigioneFissa_03 = reader.GetNullableDecimal("ValUnitProvvigioneFissa_03");
            p.Delta1ProvvigioneFissa_03 = reader.GetNullableDecimal("Delta1ProvvigioneFissa_03");
            p.Delta2ProvvigioneFissa_03 = reader.GetNullableDecimal("Delta2ProvvigioneFissa_03");
            p.TotProvvigioneFissa_03 = reader.GetNullableDecimal("TotProvvigioneFissa_03");
            p.ValQuotaFissa_03 = reader.GetNullableDecimal("ValQuotaFissa_03");

            p.TotaleFissoProvvigione = reader.GetNullableDecimal("TotaleFissoProvvigione");
            p.DeltaResiduoVariabile = reader.GetNullableDecimal("DeltaResiduoVariabile");
            p.TotaleVariabileProvvigione = reader.GetNullableDecimal("TotaleVariabileProvvigione");
            p.TotaleSommaProvvigioni = reader.GetNullableDecimal("TotaleSommaProvvigioni");
            p.TotaleProvvigioneDaFatturare = reader.GetNullableDecimal("TotaleProvvigioneDaFatturare");

            // agente
            p.ProvvigioneAgente = reader.GetNullableDecimal("ProvvigioneAgente");
            p.IdAgente = reader.GetNullableInt("IdAgente");
            // Date e lookup
            p.DataConsegnaIpotetica = reader.GetNullableDateTime("DataConsegnaIpotetica");

            // ==== MODIFICA 28/09/2026 - INIZIO ==== Gestione Ordini in Ritardo //3
            p.DataConsegna = reader.GetNullableDateTime("DataConsegna");
            p.EscludiControlloRitardo = reader.GetBoolSafe("EscludiControlloRitardo");
            // ==== MODIFICA 28/09/2026 - FINE ====  
            p.DaFatturare = reader.GetBoolSafe("DaFatturare");//30/09/2026: Aggiunto campo DaFatturare
            p.Scadenza = reader.GetNullableDateTime("Scadenza");//18/04/2026
            p.IdTipoPagamento = reader.GetNullableInt("IdTipoPagamento");
            // Audit
            p.DataInserimento = reader.GetDateTimeSafe("DataInserimento");
            p.DataModifica = reader.GetNullableDateTime("DataModifica");
            p.Utente = reader.GetStringSafe("Utente");
            // Anno e Mese
            p.Anno = reader.GetNullableInt("Anno");
            p.Mese = reader.GetNullableInt("Mese");
            // Stato fatturazione
            p.Fatturata = reader.GetIntSafe("Fatturata");

            // MODIFICA 05/10/2026 MISURE STATISTICHE (18,3) - INIZIO
            p.SpessoreNum = reader.GetNullableDecimal("SpessoreNum");
            p.LarghezzaNum = reader.GetNullableDecimal("LarghezzaNum");
            p.DiametroNum = reader.GetNullableDecimal("DiametroNum");
            // MODIFICA 05/10/2026 MISURE STATISTICHE (18,3) - FINE

            p.DataFatturazione = reader.GetNullableDateTime("DataFatturazione");
            // Soft delete
            p.IsDeleted = reader.GetBoolSafe("IsDeleted");

            // Non mappati (UI)
            p.UnitaMisura = reader.GetStringSafe("UnitaMisura");
            // ==== MODIFICA 28/09/2026 - INIZIO ==== Calcolo dinamico ritardo//1
            var dataAttivazioneModulo = new DateTime(2026, 9, 28);
            RitardoOrdiniCalculator.Calcola(p, dataAttivazioneModulo);
            // ==== MODIFICA 28/09/2026 - FINE ====
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

            // Interroghiamo la funzione scalare SQL
            using var cmd = new SqlCommand(
                "SELECT dbo.fn_GetPercentualeProvvigione(@IdAgente, @IdCliente, @IdMandatario, @ProvvigioneManuale)", // 07/08/2026
                conn);

            // Aggiunta parametri con tipo esplicito (Standard Calkos Manager)
            cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = idAgente;
            cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = idCliente;
            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = idMandatario;
            cmd.Parameters.Add("@ProvvigioneManuale", SqlDbType.Decimal, 5).Value = DBNull.Value; // 07/08/2026 NUOVO
            conn.Open();

            // Eseguiamo Scalar e gestiamo eventuali ritorni nulli (anche se la funzione SQL ha un default)
            var result = cmd.ExecuteScalar();

            if (result != null && result != DBNull.Value)
            {
                return Convert.ToDecimal(result);
            }

            return 0m;
        }

        #endregion
    }
}
