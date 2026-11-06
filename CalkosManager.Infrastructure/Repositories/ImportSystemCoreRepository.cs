/*
    ============================================================
    IMPORT SYSTEMCORE REPOSITORY — PROMEMORIA ARCHITETTURALE
    ============================================================

    Questo repository gestisce la tabella ImportSystemCore, che contiene
    tutte le righe importate dai file SYSTEMCORE prima della validazione
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
      - Nessuna modifica alla logica di business o al flusso SYSTEMCORE.

    Questo repository è considerato “sensibile”: ogni modifica deve
    essere minima, motivata e verificata con attenzione, per evitare
    impatti sul processo di importazione SYSTEMCORE.
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
    public class ImportSystemCoreRepository : IImportSystemCoreRepository
    {
        private readonly string _connectionString;

        public ImportSystemCoreRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ---------------------------------------------------------
        // INSERT UNA RIGA (TRANSazionale)
        // ---------------------------------------------------------
        public ImportSystemCore Insert(ImportSystemCore item, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        INSERT INTO ImportSystemCore (
            IdFileImportato, OrdineDDT, Cliente, Misure, Kg, CostoLavorazione,
            Materiale, PrezzoVendita, PercentualeQuotaFissa, ValoreQuotaFissa,
            QuotaCentesimiPerKg,Perc_Delta_Provvigione,
            Delta, TotaleProvvigioneVariabile, TotaleProvvigione, DataConsegna,
            TotaleProvvigioneDaFatturare, Fatturare, IdMandatario, UniqueKey,
            IsProcessed, DataInserimento, DataModifica, Utente
        )
        VALUES (
            @IdFileImportato, @OrdineDDT, @Cliente, @Misure, @Kg, @CostoLavorazione,
            @Materiale, @PrezzoVendita, @PercentualeQuotaFissa, @ValoreQuotaFissa, 
            @QuotaCentesimiPerKg,@Perc_Delta_Provvigione,
            @Delta, @TotaleProvvigioneVariabile, @TotaleProvvigione, @DataConsegna,
            @TotaleProvvigioneDaFatturare, @Fatturare, @IdMandatario, @UniqueKey,
            @IsProcessed, @DataInserimento, @DataModifica, @Utente
        );
        SELECT SCOPE_IDENTITY();";

            using var cmd = new SqlCommand(sql, conn, tran);

            PrepareParameters(cmd);
            SetParametersValues(cmd, item, utente);

            item.IdImportSystemCore = Convert.ToInt32(cmd.ExecuteScalar());
            return item;
        }

        // ---------------------------------------------------------
        // INSERT RANGE (TRANSazionale)
        // ---------------------------------------------------------
        public void InsertRange(IEnumerable<ImportSystemCore> items, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        INSERT INTO ImportSystemCore (
            IdFileImportato, OrdineDDT, Cliente, Misure, Kg, CostoLavorazione,
            Materiale, PrezzoVendita, PercentualeQuotaFissa, ValoreQuotaFissa,
            QuotaCentesimiPerKg,Perc_Delta_Provvigione,
            Delta, TotaleProvvigioneVariabile, TotaleProvvigione, DataConsegna,
            TotaleProvvigioneDaFatturare, Fatturare, IdMandatario, UniqueKey,
            IsProcessed, DataInserimento, DataModifica, Utente
        )
        VALUES (
            @IdFileImportato, @OrdineDDT, @Cliente, @Misure, @Kg, @CostoLavorazione,
            @Materiale, @PrezzoVendita, @PercentualeQuotaFissa, @ValoreQuotaFissa,
            @QuotaCentesimiPerKg,@Perc_Delta_Provvigione,
            @Delta, @TotaleProvvigioneVariabile, @TotaleProvvigione, @DataConsegna,
            @TotaleProvvigioneDaFatturare, @Fatturare, @IdMandatario, @UniqueKey,
            @IsProcessed, @DataInserimento, @DataModifica, @Utente
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
            cmd.Parameters.Add("@Misure", SqlDbType.NVarChar);

            AddDecimalParam(cmd, "@Kg", 6);
            AddDecimalParam(cmd, "@CostoLavorazione", 6);

            cmd.Parameters.Add("@Materiale", SqlDbType.NVarChar);

            AddDecimalParam(cmd, "@PrezzoVendita", 6);
            AddDecimalParam(cmd, "@PercentualeQuotaFissa", 6);
            AddDecimalParam(cmd, "@ValoreQuotaFissa", 6);

            AddDecimalParam(cmd, "@QuotaCentesimiPerKg", 6);//26/04/2026 letta dalla formula Excel Colonna (9)   "Quota fissa € 0,10"
            AddDecimalParam(cmd, "@Perc_Delta_Provvigione", 6);//26/04/2026 letta dalla formula Excel Colonna (11) Totale provv variabile

            AddDecimalParam(cmd, "@Delta", 6);
            AddDecimalParam(cmd, "@TotaleProvvigioneVariabile", 6);
            AddDecimalParam(cmd, "@TotaleProvvigione", 6);

            cmd.Parameters.Add("@DataConsegna", SqlDbType.Date);
            AddDecimalParam(cmd, "@TotaleProvvigioneDaFatturare", 6);

            cmd.Parameters.Add("@Fatturare", SqlDbType.Bit);
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
        private void SetParametersValues(SqlCommand cmd, ImportSystemCore item, string utente)
        {
            cmd.Parameters["@IdFileImportato"].Value = item.IdFileImportato;
            cmd.Parameters["@OrdineDDT"].Value = (object?)item.OrdineDDT ?? DBNull.Value;
            cmd.Parameters["@Cliente"].Value = (object?)item.Cliente ?? DBNull.Value;
            cmd.Parameters["@Misure"].Value = (object?)item.Misure ?? DBNull.Value;

            cmd.Parameters["@Kg"].Value = (object?)item.Kg ?? DBNull.Value;
            cmd.Parameters["@CostoLavorazione"].Value = (object?)item.CostoLavorazione ?? DBNull.Value;

            cmd.Parameters["@Materiale"].Value = (object?)item.Materiale ?? DBNull.Value;

            cmd.Parameters["@PrezzoVendita"].Value = (object?)item.PrezzoVendita ?? DBNull.Value;
            cmd.Parameters["@PercentualeQuotaFissa"].Value = (object?)item.PercentualeQuotaFissa ?? DBNull.Value;
            cmd.Parameters["@ValoreQuotaFissa"].Value = (object?)item.ValoreQuotaFissa ?? DBNull.Value;

            cmd.Parameters["@QuotaCentesimiPerKg"].Value = (object?)item.QuotaCentesimiPerKg ?? DBNull.Value;//26/04/2026  letta dalla formula Excel Colonna (9)   "Quota fissa € 0,10"
             cmd.Parameters["@Perc_Delta_Provvigione"].Value = (object?)item.Perc_Delta_Provvigione ?? DBNull.Value;//01/05/2026 letta dalla formula Excel Colonna (11) Totale provv variabile


            cmd.Parameters["@Delta"].Value = (object?)item.Delta ?? DBNull.Value;
            cmd.Parameters["@TotaleProvvigioneVariabile"].Value = (object?)item.TotaleProvvigioneVariabile ?? DBNull.Value;
            cmd.Parameters["@TotaleProvvigione"].Value = (object?)item.TotaleProvvigione ?? DBNull.Value;

            cmd.Parameters["@DataConsegna"].Value =
                (item.DataConsegna == null || item.DataConsegna == DateTime.MinValue)
                ? DBNull.Value : item.DataConsegna;

            cmd.Parameters["@TotaleProvvigioneDaFatturare"].Value =
                (object?)item.TotaleProvvigioneDaFatturare ?? DBNull.Value;

            cmd.Parameters["@Fatturare"].Value =
                item.Fatturare == null ? DBNull.Value : (item.Fatturare == true ? 1 : 0);

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
        public IEnumerable<ImportSystemCore> GetByFile(int idFileImportato)
        {
            const string sql = "SELECT * FROM ImportSystemCore WHERE IdFileImportato = @idFileImportato";

            var lista = new List<ImportSystemCore>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@idFileImportato", SqlDbType.Int).Value = idFileImportato;

            conn.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var r = new ImportSystemCore();

                r.IdImportSystemCore = reader.GetIntSafe("IdImportSystemCore");
                r.IdFileImportato = reader.GetIntSafe("IdFileImportato");

                r.OrdineDDT = reader.GetStringSafe("OrdineDDT");
                r.Cliente = reader.GetStringSafe("Cliente");
                r.Misure = reader.GetStringSafe("Misure");

                r.Kg = reader.GetNullableDecimal("Kg");
                r.CostoLavorazione = reader.GetNullableDecimal("CostoLavorazione");
                r.Materiale = reader.GetStringSafe("Materiale");

                r.PrezzoVendita = reader.GetNullableDecimal("PrezzoVendita");
                r.PercentualeQuotaFissa = reader.GetNullableDecimal("PercentualeQuotaFissa");
                r.ValoreQuotaFissa = reader.GetNullableDecimal("ValoreQuotaFissa"); 
                
                r.QuotaCentesimiPerKg = reader.GetNullableDecimal("QuotaCentesimiPerKg");//26/04/2026  letta dalla formula Excel Colonna (9)   "Quota fissa € 0,10"
                r.Perc_Delta_Provvigione = reader.GetNullableDecimal("Perc_Delta_Provvigione");//26/04/2026 letta dalla formula Excel Colonna (11) Totale provv variabile

                r.Delta = reader.GetNullableDecimal("Delta");
                r.TotaleProvvigioneVariabile = reader.GetNullableDecimal("TotaleProvvigioneVariabile");
                r.TotaleProvvigione = reader.GetNullableDecimal("TotaleProvvigione");

                r.DataConsegna = reader.GetNullableDateTime("DataConsegna");
                r.TotaleProvvigioneDaFatturare = reader.GetNullableDecimal("TotaleProvvigioneDaFatturare");

                r.Fatturare = reader.GetNullableBool("Fatturare");
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
            DELETE FROM ImportSystemCore 
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
            FROM ImportSystemCore
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
        public void SetProcessedStatus(int idImportSystemCore, bool isProcessed, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        UPDATE ImportSystemCore 
        SET IsProcessed = @IsProcessed, 
            DataModifica = GETDATE(), 
            Utente = @Utente 
        WHERE IdImportSystemCore = @IdImportSystemCore";

            using var cmd = new SqlCommand(sql, conn, tran);

            cmd.Parameters.Add("@IdImportSystemCore", SqlDbType.Int).Value = idImportSystemCore;
            cmd.Parameters.Add("@IsProcessed", SqlDbType.Bit).Value = isProcessed;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = utente;

            cmd.ExecuteNonQuery();
        }
    }
}
