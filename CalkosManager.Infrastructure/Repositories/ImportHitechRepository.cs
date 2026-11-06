/*
    ============================================================
    IMPORT HITECH REPOSITORY — PROMEMORIA ARCHITETTURALE
    ============================================================

    Questo repository gestisce la tabella ImportHitech, che contiene
    tutte le righe importate dai file HITECH prima della validazione
    e della trasformazione nei prospetti definitivi.

    ✔ Repository “speciale”: NON va modificato nella logica
      se non in caso di errori tecnici (AddWithValue, tipi SQL,
      mapping, parametri mancanti, ecc.).

    ✔ Caratteristiche principali:
      - Gestisce numerose colonne, molte delle quali decimal(18,6).
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
      - Nessuna modifica alla logica di business o al flusso HITECH.

    Questo repository è considerato “sensibile”: ogni modifica deve
    essere minima, motivata e verificata con attenzione, per evitare
    impatti sul processo di importazione HITECH.
    ============================================================
*/

using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Infrastructure.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Collections.Generic;

namespace CalkosManager.Infrastructure.Repositories
{
    public class ImportHitechRepository : IImportHitechRepository
    {
        private readonly string _connectionString;

        public ImportHitechRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ---------------------------------------------------------
        // INSERT UNA RIGA (TRANSazionale)
        // ---------------------------------------------------------
        public ImportHitech Insert(ImportHitech item, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        INSERT INTO ImportHitech (
            IdFileImportato, OrdineDDT, Cliente, Quantita, PrezzoBase, PrezzoPraticato,
            ProvvigioneUnitaria, TotaleProvvigione, DataDoc,DataConsegna, Agente, Fatturare,
            Scadenza, IdMandatario, UniqueKey, IsProcessed, DataInserimento, DataModifica, Utente
        )
        VALUES (
            @IdFileImportato, @OrdineDDT, @Cliente, @Quantita, @PrezzoBase, @PrezzoPraticato,
            @ProvvigioneUnitaria, @TotaleProvvigione,@DataDoc, @DataConsegna, @Agente, @Fatturare,
            @Scadenza, @IdMandatario, @UniqueKey, @IsProcessed, @DataInserimento, @DataModifica, @Utente
        );
        SELECT SCOPE_IDENTITY();";

            using var cmd = new SqlCommand(sql, conn, tran);

            PrepareParameters(cmd);
            SetParametersValues(cmd, item, utente);

            item.IdImportHitech = Convert.ToInt32(cmd.ExecuteScalar());
            return item;
        }

        // ---------------------------------------------------------
        // INSERT RANGE (TRANSazionale)
        // ---------------------------------------------------------
        public void InsertRange(IEnumerable<ImportHitech> items, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        INSERT INTO ImportHitech (
            IdFileImportato, OrdineDDT, Cliente, Quantita, PrezzoBase, PrezzoPraticato,
            ProvvigioneUnitaria, TotaleProvvigione, DataDoc,DataConsegna, Agente, Fatturare,
            Scadenza, IdMandatario, UniqueKey, IsProcessed, DataInserimento, DataModifica, Utente
        )
        VALUES (
            @IdFileImportato, @OrdineDDT, @Cliente, @Quantita, @PrezzoBase, @PrezzoPraticato,
            @ProvvigioneUnitaria, @TotaleProvvigione, @DataDoc, @DataConsegna, @Agente, @Fatturare,
            @Scadenza, @IdMandatario, @UniqueKey, @IsProcessed, @DataInserimento, @DataModifica, @Utente
        );";

            using var cmd = new SqlCommand(sql, conn, tran);

            PrepareParameters(cmd);

            foreach (var item in items)
            {
                SetParametersValues(cmd, item, utente);
                cmd.ExecuteNonQuery();
            }
        }

        // ---------------------------------------------------------
        // PREPARA PARAMETRI
        // ---------------------------------------------------------
        private void PrepareParameters(SqlCommand cmd)
        {
            cmd.Parameters.Add("@IdFileImportato", SqlDbType.Int);
            cmd.Parameters.Add("@OrdineDDT", SqlDbType.NVarChar);
            cmd.Parameters.Add("@Cliente", SqlDbType.NVarChar);

            AddDecimalParam(cmd, "@Quantita", 6);
            AddDecimalParam(cmd, "@PrezzoBase", 6);
            AddDecimalParam(cmd, "@PrezzoPraticato", 6);
            AddDecimalParam(cmd, "@ProvvigioneUnitaria", 6);
            AddDecimalParam(cmd, "@TotaleProvvigione", 6);
            
            cmd.Parameters.Add("@DataDoc", SqlDbType.Date);//21/04/2026
            cmd.Parameters.Add("@DataConsegna", SqlDbType.Date);
            cmd.Parameters.Add("@Agente", SqlDbType.NVarChar);
            cmd.Parameters.Add("@Fatturare", SqlDbType.Bit);
            cmd.Parameters.Add("@Scadenza", SqlDbType.Date);

            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int);

            cmd.Parameters.Add("@UniqueKey", SqlDbType.NVarChar);
            cmd.Parameters.Add("@IsProcessed", SqlDbType.Bit);

            cmd.Parameters.Add("@DataInserimento", SqlDbType.DateTime);
            cmd.Parameters.Add("@DataModifica", SqlDbType.DateTime);
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar);
        }

        // ---------------------------------------------------------
        // SET PARAMETER VALUES
        // ---------------------------------------------------------
        private void SetParametersValues(SqlCommand cmd, ImportHitech item, string utente)
        {
            cmd.Parameters["@IdFileImportato"].Value = item.IdFileImportato;
            cmd.Parameters["@OrdineDDT"].Value = (object?)item.OrdineDDT ?? DBNull.Value;
            cmd.Parameters["@Cliente"].Value = (object?)item.Cliente ?? DBNull.Value;

            cmd.Parameters["@Quantita"].Value = (object?)item.Quantita ?? DBNull.Value;
            cmd.Parameters["@PrezzoBase"].Value = (object?)item.PrezzoBase ?? DBNull.Value;
            cmd.Parameters["@PrezzoPraticato"].Value = (object?)item.PrezzoPraticato ?? DBNull.Value;
            cmd.Parameters["@ProvvigioneUnitaria"].Value = (object?)item.ProvvigioneUnitaria ?? DBNull.Value;
            cmd.Parameters["@TotaleProvvigione"].Value = (object?)item.TotaleProvvigione ?? DBNull.Value;

            cmd.Parameters["@DataDoc"].Value =
                (item.DataDoc == null || item.DataDoc == DateTime.MinValue)
                ? DBNull.Value : item.DataDoc;//21/04/2026

            cmd.Parameters["@DataConsegna"].Value =
                (item.DataConsegna == null || item.DataConsegna == DateTime.MinValue)
                ? DBNull.Value : item.DataConsegna;

            cmd.Parameters["@Agente"].Value = (object?)item.Agente ?? DBNull.Value;

            cmd.Parameters["@Fatturare"].Value =
                item.Fatturare == null ? DBNull.Value : (item.Fatturare == true ? 1 : 0);

            cmd.Parameters["@Scadenza"].Value =
                (item.Scadenza == null || item.Scadenza == DateTime.MinValue)
                ? DBNull.Value : item.Scadenza;

            cmd.Parameters["@IdMandatario"].Value = (object?)item.IdMandatario ?? DBNull.Value;

            cmd.Parameters["@UniqueKey"].Value = (object?)item.UniqueKey ?? DBNull.Value;
            cmd.Parameters["@IsProcessed"].Value = item.IsProcessed;

            cmd.Parameters["@DataInserimento"].Value =
                item.DataInserimento == DateTime.MinValue ? DateTime.Now : item.DataInserimento;

            cmd.Parameters["@DataModifica"].Value =
                (item.DataModifica == null || item.DataModifica == DateTime.MinValue)
                ? DBNull.Value : item.DataModifica;

            cmd.Parameters["@Utente"].Value = (object?)utente ?? DBNull.Value;
        }

        private void AddDecimalParam(SqlCommand cmd, string name, byte scale)
        {
            var p = cmd.Parameters.Add(name, SqlDbType.Decimal);
            p.Precision = 18;
            p.Scale = scale;
        }

        // ---------------------------------------------------------
        // GET BY FILE
        // ---------------------------------------------------------
        public IEnumerable<ImportHitech> GetByFile(int idFileImportato)
        {
            const string sql = "SELECT * FROM ImportHitech WHERE IdFileImportato = @idFileImportato";

            var lista = new List<ImportHitech>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@idFileImportato", SqlDbType.Int).Value = idFileImportato;

            conn.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var r = new ImportHitech();

                r.IdImportHitech = reader.GetIntSafe("IdImportHitech");
                r.IdFileImportato = reader.GetIntSafe("IdFileImportato");

                r.OrdineDDT = reader.GetStringSafe("OrdineDDT");
                r.Cliente = reader.GetStringSafe("Cliente");

                r.Quantita = reader.GetNullableDecimal("Quantita");
                r.PrezzoBase = reader.GetNullableDecimal("PrezzoBase");
                r.PrezzoPraticato = reader.GetNullableDecimal("PrezzoPraticato");
                r.ProvvigioneUnitaria = reader.GetNullableDecimal("ProvvigioneUnitaria");
                r.TotaleProvvigione = reader.GetNullableDecimal("TotaleProvvigione");
                
                r.DataDoc = reader.GetNullableDateTime("DataDoc");//21/04/2026
                r.DataConsegna = reader.GetNullableDateTime("DataConsegna");
                r.Agente = reader.GetStringSafe("Agente");

                r.Fatturare = reader.GetNullableBool("Fatturare");
                r.Scadenza = reader.GetNullableDateTime("Scadenza");

                r.IdMandatario = reader.GetNullableInt("IdMandatario");

                r.UniqueKey = reader.GetStringSafe("UniqueKey");
                r.IsProcessed = reader.GetBoolSafe("IsProcessed");

                lista.Add(r);
            }

            return lista;
        }

        // ---------------------------------------------------------
        // DELETE
        // ---------------------------------------------------------
        public void DeleteByFileImportatoId(int idFileImportato, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
            DELETE FROM ImportHitech 
            WHERE IdFileImportato = @IdFileImportato 
            AND IsProcessed = 0";

            using var cmd = new SqlCommand(sql, conn, tran);
            cmd.Parameters.Add("@IdFileImportato", SqlDbType.Int).Value = idFileImportato;

            cmd.ExecuteNonQuery();
        }

        // ---------------------------------------------------------
        // EXISTS UNIQUE KEY
        // ---------------------------------------------------------
        public bool ExistsUniqueKey(string uniqueKey, int idMandatario)
        {
            const string sql = @"
            SELECT COUNT(1)
            FROM ImportHitech
            WHERE UniqueKey = @UniqueKey
            AND IdMandatario = @IdMandatario 
            AND IsProcessed = 1;";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@UniqueKey", SqlDbType.NVarChar).Value = uniqueKey;
            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = idMandatario;

            conn.Open();

            var count = (int)cmd.ExecuteScalar();
            return count > 0;
        }

        // ---------------------------------------------------------
        // SET PROCESSED STATUS
        // ---------------------------------------------------------
        public void SetProcessedStatus(int idImportHitech, bool isProcessed, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        UPDATE ImportHitech 
        SET IsProcessed = @IsProcessed, 
            DataModifica = GETDATE(), 
            Utente = @Utente 
        WHERE IdImportHitech = @IdImportHitech";

            using var cmd = new SqlCommand(sql, conn, tran);

            cmd.Parameters.Add("@IdImportHitech", SqlDbType.Int).Value = idImportHitech;
            cmd.Parameters.Add("@IsProcessed", SqlDbType.Bit).Value = isProcessed;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = utente;

            cmd.ExecuteNonQuery();
        }
    }
}
