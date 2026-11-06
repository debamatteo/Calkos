using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Infrastructure.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;




/*
    ============================================================
    PROSPETTO DE ANGELI REPOSITORY — AGGIORNAMENTO 2026
    ============================================================

    Questo repository gestisce la tabella ProspettoDeAngeli, che contiene
    i dati trasformati e pronti per la fatturazione provvigioni.
    Deriva dalla logica di Cobral ma adatta i calcoli alla struttura
    specifica di De Angeli (Provvigioni Fisse 1-2-3 e Variabili).

    ✔ Repository "Speculare": Segue fedelmente il pattern Cobral
      per garantire coerenza nelle procedure di Dashboard e Cestino.

    ✔ Standard tecnici:
      - NIENTE AddWithValue: parametri sempre tipizzati esplicitamente.
      - Helper AddDecimal obbligatorio per garantire Precision(18) e Scale(XXX) in base al campo.
      - Mapping tramite SqlReaderExtensions (GetIntSafe, GetNullableDecimal).
      - Gestione transazionale per trasformazioni massive.

    ✔ Novità: Gestione colonna [NumeroDDT] per tracciabilità logistica.

    Ogni modifica ai campi decimali deve riflettersi sia nel metodo 
    AddParameters che nel metodo Map per evitare disallineamenti UI/DB.
    ============================================================
*/

namespace CalkosManager.Infrastructure.Repositories
{
    public class ProspettoDeAngeliRepository : IProspettoDeAngeliRepository
    {
        private readonly string _connectionString;

        public ProspettoDeAngeliRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public enum TipoOperazione
        {
            Insert,
            InsertTran,
            Update
        }

        // ---------------------------------------------------------
        // INSERT (Standard & Transazionale)
        // ---------------------------------------------------------

        public int Insert(ProspettoDeAngeli item, string utente)
        {
            
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoDeAngeli_Insert", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(cmd, item, utente, TipoOperazione.Insert);

            conn.Open();
            var result = cmd.ExecuteScalar();

            if (result != null && result != DBNull.Value)
            {
                int newId = Convert.ToInt32(result);
                item.IdProspettoDeAngeli = newId;
                return newId;
            }

            throw new Exception("Errore nel recupero dell'ID dopo l'inserimento De Angeli.");
        }

        public ProspettoDeAngeli Insert(ProspettoDeAngeli item, SqlConnection conn, SqlTransaction tran, string utente)
        {
            using var cmd = new SqlCommand("spProspettoDeAngeli_Insert_Tran", conn, tran)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(cmd, item, utente, TipoOperazione.InsertTran);

            var result = cmd.ExecuteScalar();
            item.IdProspettoDeAngeli = Convert.ToInt32(result);
            return item;
        }

        public void InsertRange(IEnumerable<ProspettoDeAngeli> items, SqlConnection conn, SqlTransaction tran, string utente)
        {
            foreach (var item in items)
            {
                Insert(item, conn, tran, utente);
            }
        }

        public void InsertRange(IEnumerable<ProspettoDeAngeli> items, string utente)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var tran = conn.BeginTransaction();
            try
            {
                InsertRange(items, conn, tran, utente);
                tran.Commit();
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        // ---------------------------------------------------------
        // UPDATE & DELETE LOGICA
        // ---------------------------------------------------------

        public void Update(ProspettoDeAngeli item, string utente)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoDeAngeli_Update", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(cmd, item, utente, TipoOperazione.Update);
            cmd.Parameters.Add("@IdProspettoDeAngeli", SqlDbType.Int).Value = item.IdProspettoDeAngeli;

            conn.Open();
            cmd.ExecuteNonQuery();
        }

        public void SoftDelete(int id, string utente)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoDeAngeli_SoftDelete", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdProspettoDeAngeli", SqlDbType.Int).Value = id;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar, 100).Value = utente;

            conn.Open();
            cmd.ExecuteNonQuery();
        }


        // ==== MODIFICA 24/10/2026 - INIZIO ==== Eliminazione fisica condizionata (usata dall'Agente)
        // Atomica: possesso, cestino e "non fatturata" sono verificati nella stessa DELETE.
        public bool DeleteSeNonFatturata(int id, int? idAgente)
        {
            const string sql = @"
            DELETE FROM ProspettoDeAngeli
            WHERE IdProspettoDeAngeli = @Id
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


        //public void Delete(int id)//02/11/2026
        //{
        //    // Chiamiamo la SoftDelete passando un utente di sistema
        //    SoftDelete(id, "System");
        //}

        public void Delete(int idProspettoDeAngeli)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoDeAngeli_Delete", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdProspettoDeAngeli", SqlDbType.Int).Value = idProspettoDeAngeli;

            conn.Open();
            cmd.ExecuteNonQuery();
        }


        public void Delete(int idFile, SqlConnection conn, SqlTransaction tran)
        {
            // Cancellazione fisica per re-importazione (tipico stile Calkos)
            // Impedisce la cancellazione se le righe sono già state fatturate
            string sql = "DELETE FROM ProspettoDeAngeli WHERE IdFileImportato = @IdFile AND Fatturata = 0";
            using var cmd = new SqlCommand(sql, conn, tran);
            cmd.Parameters.Add("@IdFile", SqlDbType.Int).Value = idFile;
            cmd.ExecuteNonQuery();
        }

        public bool ResetFatturazione(int id, string utente)
        {

            //13/10/2026 inserito DaFatturare se cancello la fattura devo impostarla come da fatturare 
            string sql = @"
                UPDATE ProspettoDeAngeli
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
                WHERE IdProspettoDeAngeli = @Id;
                ";


            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar, 100).Value = utente;

            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public void Ripristina(int id, string utente)
        {
            string sql = "UPDATE ProspettoDeAngeli SET IsDeleted = 0, DataModifica = GETDATE(), Utente = @Utente WHERE IdProspettoDeAngeli = @Id";
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar, 100).Value = utente;
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        // ---------------------------------------------------------
        // QUERY DI LETTURA
        // ---------------------------------------------------------

        public ProspettoDeAngeli? GetById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoDeAngeli_GetById", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Add("@IdProspettoDeAngeli", SqlDbType.Int).Value = id;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Map(reader) : null;
        }

        public IEnumerable<ProspettoDeAngeli> GetFiltrati(int? idMandatario, int? idFile, int? anno, int? mese, int? fatturata, 
            bool mostraEliminati = false, int? idTipoPagamento = null,
            int? idAgente = null, int? daFatturare = null)   // NUOVO 30/09/2026
        {
            var lista = new List<ProspettoDeAngeli>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProspettoDeAngeli_GetAllFiltri", conn)
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
            cmd.Parameters.Add(new SqlParameter("@IdAgente", SqlDbType.Int) { Value = (object?)idAgente ?? DBNull.Value });  // NUOVO 07/08/2026 idAgente
            cmd.Parameters.Add(new SqlParameter("@DaFatturare", SqlDbType.Int) { Value = (object?)daFatturare ?? DBNull.Value }); // NUOVO 30/09/2026


            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) lista.Add(Map(reader));
            return lista;
        }

        public IEnumerable<ProspettoDeAngeli> GetByFile(int idFile)
        {
            return GetFiltrati(null, idFile, null, null, null, false);
        }

        //public void ImpostaComeFatturate(int idMandatario, int anno, int mese, int idAgente, int idUtente)
        //{
        //    string sql = @"UPDATE ProspettoDeAngeli 
        //                   SET Fatturata = 1, DataFatturazione = GETDATE() 
        //                   WHERE IdMandatario = @IdM AND Anno = @Anno AND Mese = @Mese 
        //                   AND (@IdA = 0 OR IdAgente = @IdA) AND IsDeleted = 0";

        //    using var conn = new SqlConnection(_connectionString);
        //    using var cmd = new SqlCommand(sql, conn);
        //    cmd.Parameters.Add("@IdM", SqlDbType.Int).Value = idMandatario;
        //    cmd.Parameters.Add("@Anno", SqlDbType.Int).Value = anno;
        //    cmd.Parameters.Add("@Mese", SqlDbType.Int).Value = mese;
        //    cmd.Parameters.Add("@IdA", SqlDbType.Int).Value = idAgente;

        //    conn.Open();
        //    cmd.ExecuteNonQuery();
        //}
        public void ImpostaComeFatturate(int idMandatario, int anno, int mese, int idCliente, int idTipoPagamento)//01/04/2026 - Aggiunta filtro per IdTipoPagamento per gestire casi specifici di fatturazione parziale
        {
            //13/10/2026 inserito DaFatturare = 0 
            string SQL_DEBUG = $@"
            UPDATE ProspettoDeAngeli
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
        // ---------------------------------------------------------
        // HELPERS MAPPING & PARAMETERS
        // ---------------------------------------------------------
        private static void AddParameters(SqlCommand cmd, ProspettoDeAngeli item, string utente, TipoOperazione operazione)
        {

            // AddParameters Gestisce Insert e Update, quindi:
            // -OBBLIGATORIO= Ogni cmd.Parameters.Add("@Parametro") della funzione AddParameters DEVE esistere nella Stored.

            // -OBBLIGATORIO = Se la Stored aspetta un parametro deve essere presente in AddParameters, anche se il valore è null (in questo caso va passato DBNull.Value).
            // (A meno che il parametro in SQL non abbia un valore di default tipo = NULL).
            // -----------------------------------------------------------------------------------------

            // 🔹 PARAMETRI SPECIFICI PER OPERAZIONE //25/04/2026
            switch (operazione)
            {
                case TipoOperazione.InsertTran:
                case TipoOperazione.Insert:

                    cmd.Parameters.Add("@IdFileImportato", SqlDbType.Int).Value = item.IdFileImportato;
                   
                    cmd.Parameters.Add("@IdImportDeAngeli", SqlDbType.Int).Value = item.IdImportDeAngeli;
                    cmd.Parameters.Add("@Anno", SqlDbType.Int).Value = (object?)item.Anno ?? DBNull.Value;
                    cmd.Parameters.Add("@Mese", SqlDbType.Int).Value = (object?)item.Mese ?? DBNull.Value;
                    break;

                case TipoOperazione.Update:
                    break;
            }



            // 🔹 2. PARAMETRI COMUNI (Presenti in tutte le Stored Procedure: Insert e Update)
            // Riferimenti
            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = item.IdMandatario;
            cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = item.IdCliente;
            cmd.Parameters.Add("@IdMateriale", SqlDbType.Int).Value = item.IdMateriale;
            cmd.Parameters.Add("@IdUnitaMisura", SqlDbType.Int).Value = item.IdUnitaMisura;



            //cmd.Parameters.Add("@Fatturata", SqlDbType.Int).Value = item.Fatturata;

            // Stringhe
            cmd.Parameters.Add("@NumeroOrdine", SqlDbType.NVarChar, 100).Value = (object?)item.NumeroOrdine ?? DBNull.Value;
            cmd.Parameters.Add("@NumeroDDT", SqlDbType.NVarChar, 100).Value = (object?)item.NumeroDDT ?? DBNull.Value;
            cmd.Parameters.Add("@NumeroFattura", SqlDbType.NVarChar, 100).Value = (object?)item.NumeroFattura ?? DBNull.Value;

            // 🔹 3. DECIMALI (Precisione 18,6)
            AddDecimal(cmd, "@Quantita", item.Quantita, 18, 6);
            AddDecimal(cmd, "@QuantitaOrdine", item.QuantitaOrdine, 18, 6);
            AddDecimal(cmd, "@CostoLavorazione", item.CostoLavorazione, 18, 6);
            AddDecimal(cmd, "@Prezzo", item.Prezzo, 18, 6);
            AddDecimal(cmd, "@PrezzoConLavorazione", item.PrezzoConLavorazione, 18, 6);
            AddDecimal(cmd, "@PrezzoVendita", item.PrezzoVendita, 18, 6);

            AddDecimal(cmd, "@PercentualeProvvigioneVariabile", item.PercentualeProvvigioneVariabile, 18, 6);
            AddDecimal(cmd, "@ValoreProvvigioneVariabile", item.ValoreProvvigioneVariabile, 18, 6);
            AddDecimal(cmd, "@ValoreProvvigioneFissa1", item.ValoreProvvigioneFissa1, 18, 6);
            AddDecimal(cmd, "@ValoreProvvigioneFissa2", item.ValoreProvvigioneFissa2, 18, 6);
            AddDecimal(cmd, "@ValoreProvvigioneFissa3", item.ValoreProvvigioneFissa3, 18, 6);
            AddDecimal(cmd, "@TotaleFissoProvvigione", item.TotaleFissoProvvigione, 18, 6);
            AddDecimal(cmd, "@Differenza", item.Differenza, 18, 6);
            AddDecimal(cmd, "@TotaleVariabileProvvigione", item.TotaleVariabileProvvigione, 18, 6);
            AddDecimal(cmd, "@TotaleProvvigioneDaFatturare", item.TotaleProvvigioneDaFatturare, 18, 6);
            AddDecimal(cmd, "@ProvvigioneAgente", item.ProvvigioneAgente, 18, 6);
            AddDecimal(cmd, "@Perc_Delta_Provvigione", item.Perc_Delta_Provvigione, 18, 6);
         
            // 🔹 4. ALTRI CAMPI E DATE
            cmd.Parameters.Add("@FlagIndicatoreMaggiorazione", SqlDbType.Bit).Value = item.FlagIndicatoreMaggiorazione.HasValue ? (object)item.FlagIndicatoreMaggiorazione.Value : DBNull.Value;
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

            // NUOVO 28/08/2026 — storicizzazione CalcoloBaseMateriale
            cmd.Parameters.Add("@IdCalcoloBase", SqlDbType.Int).Value = (object?)item.IdCalcoloBase ?? DBNull.Value;


            // Audit
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar, 100).Value = utente;
        }


        private static void AddDecimal(SqlCommand cmd, string name, decimal? value, byte precision, byte scale)
        {
            var p = cmd.Parameters.Add(name, SqlDbType.Decimal);
            p.Precision = precision;
            p.Scale = scale;
            p.Value = (object?)value ?? DBNull.Value;
        }
        private static ProspettoDeAngeli Map(SqlDataReader reader)
        {

            // Map=Mappatura serve per le SELECT, quindi:
            //Obbligatorio = Tutti i campi della Map devono essere nella Stored altrimenti la Funzione va in errore 

            //Opzionale= I campi della Stored possono anche non essere presenti nella Map,
            //se sono presenti devono essere mappati correttamente.


            var p = new ProspettoDeAngeli();
            
            // ID e Riferimenti Base
            p.IdProspettoDeAngeli = reader.GetIntSafe("IdProspettoDeAngeli");
            p.IdFileImportato = reader.GetIntSafe("IdFileImportato");
            p.IdMandatario = reader.GetIntSafe("IdMandatario");
            p.IdImportDeAngeli = reader.GetIntSafe("IdImportDeAngeli");
            p.IdCliente = reader.GetIntSafe("IdCliente");
            p.IdMateriale = reader.GetIntSafe("IdMateriale");
            p.IdUnitaMisura = reader.GetIntSafe("IdUnitaMisura");
            p.IdAgente = reader.GetNullableInt("IdAgente");
            p.IdTipoPagamento = reader.GetNullableInt("IdTipoPagamento");

            // Stringhe
            p.NumeroOrdine = reader.GetStringSafe("NumeroOrdine");
            p.NumeroDDT = reader.GetStringSafe("NumeroDDT");
            p.NumeroFattura = reader.GetStringSafe("NumeroFattura");

            // Decimali
            p.Quantita = reader.GetNullableDecimal("Quantita");
            p.QuantitaOrdine = reader.GetNullableDecimal("QuantitaOrdine");
            p.CostoLavorazione = reader.GetNullableDecimal("CostoLavorazione");
            p.Prezzo = reader.GetNullableDecimal("Prezzo");
            p.PrezzoConLavorazione = reader.GetNullableDecimal("PrezzoConLavorazione");
            p.PrezzoVendita = reader.GetNullableDecimal("PrezzoVendita");

            // Provvigioni
            p.Perc_Delta_Provvigione = reader.GetNullableDecimal("Perc_Delta_Provvigione");
    

            p.PercentualeProvvigioneVariabile = reader.GetNullableDecimal("PercentualeProvvigioneVariabile");
            p.ValoreProvvigioneVariabile = reader.GetNullableDecimal("ValoreProvvigioneVariabile");
            p.ValoreProvvigioneFissa1 = reader.GetNullableDecimal("ValoreProvvigioneFissa1");
            p.ValoreProvvigioneFissa2 = reader.GetNullableDecimal("ValoreProvvigioneFissa2");
            p.ValoreProvvigioneFissa3 = reader.GetNullableDecimal("ValoreProvvigioneFissa3");
            p.TotaleFissoProvvigione = reader.GetNullableDecimal("TotaleFissoProvvigione");
            p.Differenza = reader.GetNullableDecimal("Differenza");
            p.TotaleVariabileProvvigione = reader.GetNullableDecimal("TotaleVariabileProvvigione");
            p.TotaleProvvigioneDaFatturare = reader.GetNullableDecimal("TotaleProvvigioneDaFatturare");
            p.ProvvigioneAgente = reader.GetNullableDecimal("ProvvigioneAgente");

            // Date e Audit
            p.DataConsegnaIpotetica = reader.GetNullableDateTime("DataConsegnaIpotetica");

            // ==== MODIFICA 28/09/2026 - INIZIO ==== Gestione Ordini in Ritardo //3
            p.DataConsegna = reader.GetNullableDateTime("DataConsegna");
            p.EscludiControlloRitardo = reader.GetBoolSafe("EscludiControlloRitardo");
            // ==== MODIFICA 28/09/2026 - FINE ====              
            p.DaFatturare = reader.GetBoolSafe("DaFatturare");//30/09/2026: Aggiunto campo DaFatturare
            p.Scadenza = reader.GetNullableDateTime("Scadenza");

            p.DataInserimento = reader.GetDateTimeSafe("DataInserimento");
            p.DataModifica = reader.GetNullableDateTime("DataModifica");
            p.Utente = reader.GetStringSafe("Utente");
            p.Fatturata = reader.GetIntSafe("Fatturata");

            // MODIFICA 05/10/2026 MISURE STATISTICHE (18,3) - INIZIO
            p.SpessoreNum = reader.GetNullableDecimal("SpessoreNum");
            p.LarghezzaNum = reader.GetNullableDecimal("LarghezzaNum");
            p.DiametroNum = reader.GetNullableDecimal("DiametroNum");
            // MODIFICA 05/10/2026 MISURE STATISTICHE (18,3) - FINE


            p.Anno = reader.GetNullableInt("Anno");
            p.Mese = reader.GetNullableInt("Mese");


            p.DataFatturazione = reader.GetNullableDateTime("DataFatturazione");
            p.IsDeleted = reader.GetBoolSafe("IsDeleted");

            // Campi Bit e Descrizioni esterne (Join)
            p.FlagIndicatoreMaggiorazione = reader.GetNullableBool("FlagIndicatoreMaggiorazione");
            p.RagioneSociale = reader.GetStringSafe("RagioneSociale");
            p.DescrizioneMateriale = reader.GetStringSafe("DescrizioneMateriale");
            p.UnitaMisura = reader.GetStringSafe("UnitaMisura");
            p.Agente = reader.GetStringSafe("AgenteDescrizione");
            p.TipoPagamento = reader.GetStringSafe("TipoPagamento");

            p.IdCalcoloBase = reader.GetNullableInt("IdCalcoloBase");   // NUOVO 28/08/2026
            
            // ==== MODIFICA 28/09/2026 - INIZIO ==== Calcolo dinamico ritardo//1
            var dataAttivazioneModulo = new DateTime(2026, 9, 28);
            RitardoOrdiniCalculator.Calcola(p, dataAttivazioneModulo);
            // ==== MODIFICA 28/09/2026 - FINE ====

            return p;
        }




        // ---------------------------------------------------------
        // RECUPERO PERCENTUALE PROVVIGIONE AGENTE DINAMICA 30/04/2026
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











    }
}