using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Infrastructure.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CalkosManager.Infrastructure.Repositories
{
    public class ProspettoCSTRepository : IProspettoCSTRepository
    {
        private readonly string _connectionString;

        public ProspettoCSTRepository(string connectionString)
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

        // ---------------------------------------------------------
        // INSERT
        // ---------------------------------------------------------
        public int Insert(ProspettoCST item, string utente)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoCST_Insert", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(cmd, item, utente, TipoOperazione.Insert);

            conn.Open();
            var result = cmd.ExecuteScalar();

            if (result != null && result != DBNull.Value)
            {
                int newId = Convert.ToInt32(result);
                item.IdProspettoCST = newId;
                return newId;
            }

            throw new Exception("Errore nel recupero dell'ID dopo l'inserimento.");
        }

        // ---------------------------------------------------------
        // INSERT (transazionale)
        // ---------------------------------------------------------
        public ProspettoCST Insert(ProspettoCST item, SqlConnection conn, SqlTransaction tran, string utente)
        {
            using var cmd = new SqlCommand("spProspettoCST_Insert_Tran", conn, tran)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(cmd, item, utente, TipoOperazione.InsertTran);

            var result = cmd.ExecuteScalar();
            item.IdProspettoCST = Convert.ToInt32(result);
            return item;
        }

        // ---------------------------------------------------------
        // INSERT RANGE (transazionale)
        // ---------------------------------------------------------
        public void InsertRange(IEnumerable<ProspettoCST> items, SqlConnection conn, SqlTransaction tran, string utente)
        {
            foreach (var item in items)
            {
                Insert(item, conn, tran, utente);
            }
        }

        public void InsertRange(IEnumerable<ProspettoCST> items, string utente)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            foreach (var item in items)
            {
                Insert(item, utente);
            }
        }

        // ---------------------------------------------------------
        // UPDATE
        // ---------------------------------------------------------
        public void Update(ProspettoCST item, string utente)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoCST_Update", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(cmd, item, utente, TipoOperazione.Update);
            cmd.Parameters.Add("@IdProspettoCST", SqlDbType.Int).Value = item.IdProspettoCST;

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        // ---------------------------------------------------------
        // RESET FATTURAZIONE
        // ---------------------------------------------------------
        public bool ResetFatturazione(int idProspettoCST, string utente)
        {


            //13/10/2026 inserito DaFatturare se cancello la fattura devo impostarla come da fatturare 
            string sql = @"
                UPDATE ProspettoCST
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
                WHERE IdProspettoCST = @Id;
                ";


            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = idProspettoCST;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = utente;

            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        // ---------------------------------------------------------
        // DELETE
        // ---------------------------------------------------------
        // ==== MODIFICA 24/10/2026 - INIZIO ==== Eliminazione fisica condizionata (usata dall'Agente)
        // Atomica: possesso, cestino e "non fatturata" sono verificati nella stessa DELETE.
        public bool DeleteSeNonFatturata(int id, int? idAgente)
        {
            const string sql = @"
                DELETE FROM ProspettoCST
                WHERE IdProspettoCST = @Id
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
        public void Delete(int idProspettoCST)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoCST_Delete", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdProspettoCST", SqlDbType.Int).Value = idProspettoCST;

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void Delete(int idProspettoCST, SqlConnection conn, SqlTransaction tran)
        {
            using var cmd = new SqlCommand("spProspettoCST_Delete", conn, tran)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdProspettoCST", SqlDbType.Int).Value = idProspettoCST;
            cmd.ExecuteNonQuery();
        }

        public void SoftDelete(int id, string utente)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoCST_SoftDelete", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdProspettoCST", SqlDbType.Int).Value = id;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = utente;

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void Ripristina(int id, string utente)
        {
            using var conn = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE ProspettoCST
                SET IsDeleted = 0,
                    DataModifica = GETDATE(),
                    Utente = @Utente
                WHERE IdProspettoCST = @Id;
            ";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = utente;

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        // ---------------------------------------------------------
        // GET BY ID
        // ---------------------------------------------------------
        public ProspettoCST? GetById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoCST_GetById", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdProspettoCST", SqlDbType.Int).Value = id;

            conn.Open();
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            return Map(reader);
        }

        // ---------------------------------------------------------
        // GET BY FILE
        // ---------------------------------------------------------
        public IEnumerable<ProspettoCST> GetByFile(int idFileImportato)
        {
            var lista = new List<ProspettoCST>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoCST_GetByFile", conn)
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
        // GET ALL
        // ---------------------------------------------------------
        public IEnumerable<ProspettoCST> GetAll()
        {
            var lista = new List<ProspettoCST>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoCST_GetAll", conn)
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

        // ---------------------------------------------------------
        // GET FILTRATI
        // ---------------------------------------------------------
        // 07/08/2026 Firma aggiornata 
        public IEnumerable<ProspettoCST> GetFiltrati(
            int? idMandatario, int? idFile, int? anno, int? mese, int? fatturata,
            bool mostraEliminati = false, int? idTipoPagamento = null,
            int? idAgente = null, int? daFatturare = null)   // NUOVO 30/09/2026

        {
            var lista = new List<ProspettoCST>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoCST_GetAllFiltri", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = (object?)idMandatario ?? DBNull.Value;
            cmd.Parameters.Add("@IdFileImportato", SqlDbType.Int).Value = (object?)idFile ?? DBNull.Value;
            cmd.Parameters.Add("@Anno", SqlDbType.Int).Value = (object?)anno ?? DBNull.Value;
            cmd.Parameters.Add("@Mese", SqlDbType.Int).Value = (object?)mese ?? DBNull.Value;
            cmd.Parameters.Add("@Fatturata", SqlDbType.Int).Value = (object?)fatturata ?? DBNull.Value;
            cmd.Parameters.Add("@MostraEliminati", SqlDbType.Bit).Value = mostraEliminati;
            cmd.Parameters.Add("@IdTipoPagamento", SqlDbType.Int).Value = (object?)idTipoPagamento ?? DBNull.Value;//22/05/2026 - Aggiunta parametro opzionale per filtro TipoPagamento

            // 07/08/2026
            cmd.Parameters.Add(new SqlParameter("@IdAgente", SqlDbType.Int) { Value = (object?)idAgente ?? DBNull.Value }); // 07/08/2026
            cmd.Parameters.Add(new SqlParameter("@DaFatturare", SqlDbType.Int) { Value = (object?)daFatturare ?? DBNull.Value }); // NUOVO 30/09/2026

            conn.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(Map(reader));
            }

            return lista;
        }

        // ---------------------------------------------------------
        // PARAMETRI
        // ---------------------------------------------------------
        private static void AddParameters(SqlCommand cmd, ProspettoCST item, string utente, TipoOperazione operazione)
        {

            // AddParameters Gestisce Insert e Update, quindi:
            // -OBBLIGATORIO= Ogni cmd.Parameters.Add("@Parametro") della funzione AddParameters DEVE esistere nella Stored.

            // -OBBLIGATORIO = Se la Stored aspetta un parametro deve essere presente in AddParameters, anche se il valore è null (in questo caso va passato DBNull.Value).
            // (A meno che il parametro in SQL non abbia un valore di default tipo = NULL).
            // -----------------------------------------------------------------------------------------


            switch (operazione)
            {
                case TipoOperazione.Insert:
                case TipoOperazione.InsertTran:
                    cmd.Parameters.Add("@IdFileImportato", SqlDbType.Int).Value = item.IdFileImportato;
                    cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = item.IdMandatario;
                    cmd.Parameters.Add("@IdImportCST", SqlDbType.Int).Value = item.IdImportCST;
                    cmd.Parameters.Add("@Anno", SqlDbType.Int).Value = (object?)item.Anno ?? DBNull.Value;
                    cmd.Parameters.Add("@Mese", SqlDbType.Int).Value = (object?)item.Mese ?? DBNull.Value;
                    break;
            }

            cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = item.IdCliente;
            cmd.Parameters.Add("@IdMateriale", SqlDbType.Int).Value = item.IdMateriale;
            cmd.Parameters.Add("@IdUnitaMisura", SqlDbType.Int).Value = item.IdUnitaMisura;

            cmd.Parameters.Add("@NumeroOrdine", SqlDbType.NVarChar).Value = (object?)item.NumeroOrdine ?? DBNull.Value;
            cmd.Parameters.Add("@NumeroDDT", SqlDbType.NVarChar).Value = (object?)item.NumeroDDT ?? DBNull.Value;
            cmd.Parameters.Add("@NumeroFattura", SqlDbType.NVarChar).Value = (object?)item.NumeroFattura ?? DBNull.Value;

            AddDecimal(cmd, "@Quantita", item.Quantita, 18, 6);
            AddDecimal(cmd, "@CostoLavorazione", item.CostoLavorazione, 18, 6);
            AddDecimal(cmd, "@PrezzoBase", item.PrezzoBase, 18, 6);
            AddDecimal(cmd, "@PrezzoVendita", item.PrezzoVendita, 18, 6);
            AddDecimal(cmd, "@PrezzoBaseConLavorazione", item.PrezzoBaseConLavorazione, 18, 6);
            AddDecimal(cmd, "@PrezzoEuroRiferimento", item.PrezzoEuroRiferimento, 18, 6);

            AddDecimal(cmd, "@ProvvigioneFissa01_05", item.ProvvigioneFissa01_05, 18, 6);
            AddDecimal(cmd, "@ProvvigioneFissa03", item.ProvvigioneFissa03, 18, 6);
            AddDecimal(cmd, "@Perc_Delta_Provvigione", item.Perc_Delta_Provvigione, 18, 6);//30/04/2026
            AddDecimal(cmd, "@TotaleProvvigioneFissa", item.TotaleProvvigioneFissa, 18, 6);
            AddDecimal(cmd, "@CIT", item.CIT, 18, 6);
            AddDecimal(cmd, "@Delta", item.Delta, 18, 6);
            AddDecimal(cmd, "@TotaleProvvigioneVariabile", item.TotaleProvvigioneVariabile, 18, 6);
            AddDecimal(cmd, "@TotaleProvvigioneFattura", item.TotaleProvvigioneFattura, 18, 6);

            cmd.Parameters.Add("@DataConsegnaIpotetica", SqlDbType.Date).Value = (object?)item.DataConsegnaIpotetica ?? DBNull.Value;

            // ==== MODIFICA 28/09/2026 - INIZIO ==== Gestione Ordini in Ritardo //2
            cmd.Parameters.Add("@DataConsegna", SqlDbType.Date).Value = (object?)item.DataConsegna ?? DBNull.Value;
            cmd.Parameters.Add("@EscludiControlloRitardo", SqlDbType.Bit).Value = item.EscludiControlloRitardo;
            // ==== MODIFICA 28/09/2026 - FINE ====               //cmd.Parameters.Add("@Fatturare", SqlDbType.Bit).Value = (object?)item.Fatturare ?? DBNull.Value;

            // MODIFICA 05/10/2026 MISURE STATISTICHE (18,3) - INIZIO
            AddDecimal(cmd, "@SpessoreNum", item.SpessoreNum, 18, 3);
            AddDecimal(cmd, "@LarghezzaNum", item.LarghezzaNum, 18, 3);
            AddDecimal(cmd, "@DiametroNum", item.DiametroNum, 18, 3);
            // MODIFICA 05/10/2026 MISURE STATISTICHE (18,3) - FINE


            cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = (object?)item.IdAgente ?? DBNull.Value;
            cmd.Parameters.Add("@IdTipoPagamento", SqlDbType.Int).Value = (object?)item.IdTipoPagamento ?? DBNull.Value;

            AddDecimal(cmd, "@ProvvigioneAgente", item.ProvvigioneAgente, 18, 6);
            cmd.Parameters.Add("@Scadenza", SqlDbType.Date).Value = (object?)item.Scadenza ?? DBNull.Value;
            cmd.Parameters.Add("@DaFatturare", SqlDbType.Bit).Value = item.DaFatturare;//30/09/2026: Aggiunto campo DaFatturare



            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = utente;
        }

        private static void AddDecimal(SqlCommand cmd, string name, decimal? value, byte precision, byte scale)
        {
            var p = cmd.Parameters.Add(name, SqlDbType.Decimal);
            p.Precision = precision;
            p.Scale = scale;
            p.Value = (object?)value ?? DBNull.Value;
        }

        // ---------------------------------------------------------
        // MAP
        // ---------------------------------------------------------
        private static ProspettoCST Map(SqlDataReader reader)
        {
            // Map=Mappatura serve per le SELECT, quindi:
            //Obbligatorio = Tutti i campi della Map devono essere nella Stored altrimenti la Funzione va in errore 

            //Opzionale= I campi della Stored possono anche non essere presenti nella Map,
            //se sono presenti devono essere mappati correttamente.


            var p = new ProspettoCST();

            p.IdProspettoCST = reader.GetIntSafe("IdProspettoCST");
            p.IdFileImportato = reader.GetIntSafe("IdFileImportato");
            p.IdMandatario = reader.GetIntSafe("IdMandatario");
            p.IdImportCST = reader.GetIntSafe("IdImportCST");

            p.IdCliente = reader.GetIntSafe("IdCliente");
            p.RagioneSociale = reader.GetStringSafe("RagioneSociale");

            p.IdMateriale = reader.GetIntSafe("IdMateriale");
            p.DescrizioneMateriale = reader.GetStringSafe("DescrizioneMateriale");

            p.IdUnitaMisura = reader.GetIntSafe("IdUnitaMisura");
            p.UnitaMisura = reader.GetStringSafe("UnitaMisura");

            p.NumeroOrdine = reader.GetStringSafe("NumeroOrdine");
            p.NumeroDDT = reader.GetStringSafe("NumeroDDT");
            p.NumeroFattura = reader.GetStringSafe("NumeroFattura");

            p.Quantita = reader.GetNullableDecimal("Quantita");
            p.CostoLavorazione = reader.GetNullableDecimal("CostoLavorazione");
            p.PrezzoBase = reader.GetNullableDecimal("PrezzoBase");
            p.PrezzoEuroRiferimento = reader.GetNullableDecimal("PrezzoEuroRiferimento");
            p.PrezzoBaseConLavorazione = reader.GetNullableDecimal("PrezzoBaseConLavorazione");

            p.PrezzoVendita = reader.GetNullableDecimal("PrezzoVendita");
            p.ProvvigioneFissa01_05 = reader.GetNullableDecimal("ProvvigioneFissa01_05");
            p.ProvvigioneFissa03 = reader.GetNullableDecimal("ProvvigioneFissa03");
            p.Perc_Delta_Provvigione = reader.GetNullableDecimal("Perc_Delta_Provvigione");//30/04/2026
            p.TotaleProvvigioneFissa = reader.GetNullableDecimal("TotaleProvvigioneFissa");
            p.CIT = reader.GetNullableDecimal("CIT");
            p.Delta = reader.GetNullableDecimal("Delta");
            p.TotaleProvvigioneVariabile = reader.GetNullableDecimal("TotaleProvvigioneVariabile");
            p.TotaleProvvigioneFattura = reader.GetNullableDecimal("TotaleProvvigioneFattura");

            p.DataConsegnaIpotetica = reader.GetNullableDateTime("DataConsegnaIpotetica");
            // ==== MODIFICA 28/09/2026 - INIZIO ==== Gestione Ordini in Ritardo //3
            p.DataConsegna = reader.GetNullableDateTime("DataConsegna");
            p.EscludiControlloRitardo = reader.GetBoolSafe("EscludiControlloRitardo");
            // ==== MODIFICA 28/09/2026 - FINE ====   
            p.DaFatturare = reader.GetBoolSafe("DaFatturare");//30/09/2026: Aggiunto campo DaFatturare

            p.Fatturare = reader.GetNullableBool("Fatturare");//Dismesso

            p.IdAgente = reader.GetNullableInt("IdAgente");
            p.AgenteDescrizione = reader.GetStringSafe("AgenteDescrizione");
            p.PercentualeDefaultAgente = reader.GetNullableDecimal("PercentualeDefault");

            p.IdTipoPagamento = reader.GetNullableInt("IdTipoPagamento");
            p.TipoPagamento = reader.GetStringSafe("TipoPagamento");

            p.ProvvigioneAgente = reader.GetNullableDecimal("ProvvigioneAgente");

            p.Scadenza = reader.GetNullableDateTime("Scadenza");

            p.DataInserimento = reader.GetDateTimeSafe("DataInserimento");
            p.DataModifica = reader.GetNullableDateTime("DataModifica");
            p.Utente = reader.GetStringSafe("Utente");

            p.Anno = reader.GetNullableInt("Anno");
            p.Mese = reader.GetNullableInt("Mese");

            p.Fatturata = reader.GetIntSafe("Fatturata");
            p.DataFatturazione = reader.GetNullableDateTime("DataFatturazione");
            // MODIFICA 05/10/2026 MISURE STATISTICHE (18,3) - INIZIO
            p.SpessoreNum = reader.GetNullableDecimal("SpessoreNum");
            p.LarghezzaNum = reader.GetNullableDecimal("LarghezzaNum");
            p.DiametroNum = reader.GetNullableDecimal("DiametroNum");
            // MODIFICA 05/10/2026 MISURE STATISTICHE (18,3) - FINE

            p.IsDeleted = reader.GetBoolSafe("IsDeleted");
            // ==== MODIFICA 28/09/2026 - INIZIO ==== Calcolo dinamico ritardo //1
            var dataAttivazioneModulo = new DateTime(2026, 9, 28);
            RitardoOrdiniCalculator.Calcola(p, dataAttivazioneModulo);
            // ==== MODIFICA 28/09/2026 - FINE ====
            return p;
        }


        public decimal GetPercentualeAgente(int idAgente, int idCliente, int idMandatario)
        {
            using var conn = new SqlConnection(_connectionString);
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
        public void ImpostaComeFatturate(int idMandatario, int anno, int mese, int idCliente, int idTipoPagamento)//01/04/2026 - Aggiunta filtro per IdTipoPagamento per gestire casi specifici di fatturazione parziale
        {

            //13/10/2026 inserito DaFatturare = 0 
            string SQL_DEBUG = $@"
            UPDATE ProspettoCST
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



    }
}
