/*
    ============================================================
    IMPORT GUERZONI REPOSITORY — PROMEMORIA ARCHITETTURALE
    ============================================================

    Questo repository gestisce la tabella ImportGuerzoni, che contiene
    tutte le righe importate dai file GUERZONI prima della validazione
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
      - Nessuna modifica alla logica di business o al flusso GUERZONI.

    Questo repository è considerato “sensibile”: ogni modifica deve
    essere minima, motivata e verificata con attenzione, per evitare
    impatti sul processo di importazione GUERZONI.
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
    public class ImportGuerzoniRepository : IImportGuerzoniRepository
    {
        private readonly string _connectionString;

        public ImportGuerzoniRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ---------------------------------------------------------
        // INSERT UNA RIGA (TRANSazionale)
        // ---------------------------------------------------------
        public ImportGuerzoni Insert(ImportGuerzoni item, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        INSERT INTO ImportGuerzoni (
            IdFileImportato, OrdineDDT, Cliente, Quantita, PrezzoUnitario, Imponibile,Perc_Delta_Provvigione,
            TotaleProvvigione, DataConsegna, Fatturare, Scadenza, IdMandatario,
            UniqueKey, IsProcessed, DataInserimento, DataModifica, Utente
        )
        VALUES (
            @IdFileImportato, @OrdineDDT, @Cliente, @Quantita, @PrezzoUnitario, @Imponibile,@Perc_Delta_Provvigione,
            @TotaleProvvigione, @DataConsegna, @Fatturare, @Scadenza, @IdMandatario,
            @UniqueKey, @IsProcessed, @DataInserimento, @DataModifica, @Utente
        );
        SELECT SCOPE_IDENTITY();";

            using var cmd = new SqlCommand(sql, conn, tran);

            PrepareParameters(cmd);
            SetParametersValues(cmd, item, utente);

            item.IdImportGuerzoni = Convert.ToInt32(cmd.ExecuteScalar());
            return item;
        }

        // ---------------------------------------------------------
        // INSERT RANGE (TRANSazionale)
        // ---------------------------------------------------------
        public void InsertRange(IEnumerable<ImportGuerzoni> items, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        INSERT INTO ImportGuerzoni (
            IdFileImportato, OrdineDDT, Cliente, Quantita, PrezzoUnitario, Imponibile,Perc_Delta_Provvigione,
            TotaleProvvigione, DataConsegna, Fatturare, Scadenza, IdMandatario,
            UniqueKey, IsProcessed, DataInserimento, DataModifica, Utente
        )
        VALUES (
            @IdFileImportato, @OrdineDDT, @Cliente, @Quantita, @PrezzoUnitario, @Imponibile,@Perc_Delta_Provvigione,
            @TotaleProvvigione, @DataConsegna, @Fatturare, @Scadenza, @IdMandatario,
            @UniqueKey, @IsProcessed, @DataInserimento, @DataModifica, @Utente
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
            AddDecimalParam(cmd, "@PrezzoUnitario", 6);
            AddDecimalParam(cmd, "@Imponibile", 6);
            AddDecimalParam(cmd, "@Perc_Delta_Provvigione", 6);//29/04/2026

            AddDecimalParam(cmd, "@TotaleProvvigione", 6);

            cmd.Parameters.Add("@DataConsegna", SqlDbType.Date);
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
        private void SetParametersValues(SqlCommand cmd, ImportGuerzoni item, string utente)
        {
                cmd.Parameters["@IdFileImportato"].Value = item.IdFileImportato;
                cmd.Parameters["@OrdineDDT"].Value = (object?)item.OrdineDDT ?? DBNull.Value;
                cmd.Parameters["@Cliente"].Value = (object?)item.Cliente ?? DBNull.Value;

                cmd.Parameters["@Quantita"].Value = (object?)item.Quantita ?? DBNull.Value;
                cmd.Parameters["@PrezzoUnitario"].Value = (object?)item.PrezzoUnitario ?? DBNull.Value;
                cmd.Parameters["@Imponibile"].Value = (object?)item.Imponibile ?? DBNull.Value;
                cmd.Parameters["@Perc_Delta_Provvigione"].Value = (object?)item.Perc_Delta_Provvigione ?? DBNull.Value;//01/05/2026


                cmd.Parameters["@TotaleProvvigione"].Value = (object?)item.TotaleProvvigione ?? DBNull.Value;

                cmd.Parameters["@DataConsegna"].Value =
                (item.DataConsegna == null || item.DataConsegna == DateTime.MinValue)
                ? DBNull.Value : item.DataConsegna;

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
        public IEnumerable<ImportGuerzoni> GetByFile(int idFileImportato)
        {
            const string sql = "SELECT * FROM ImportGuerzoni WHERE IdFileImportato = @idFileImportato";

            var lista = new List<ImportGuerzoni>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@idFileImportato", SqlDbType.Int).Value = idFileImportato;

            conn.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var r = new ImportGuerzoni();

                r.IdImportGuerzoni = reader.GetIntSafe("IdImportGuerzoni");
                r.IdFileImportato = reader.GetIntSafe("IdFileImportato");

                r.OrdineDDT = reader.GetStringSafe("OrdineDDT");
                r.Cliente = reader.GetStringSafe("Cliente");

                r.Quantita = reader.GetNullableDecimal("Quantita");
                r.PrezzoUnitario = reader.GetNullableDecimal("PrezzoUnitario");
                r.Imponibile = reader.GetNullableDecimal("Imponibile");
                r.Perc_Delta_Provvigione = reader.GetNullableDecimal("Perc_Delta_Provvigione");//29/04/2026
                r.TotaleProvvigione = reader.GetNullableDecimal("TotaleProvvigione");

                r.DataConsegna = reader.GetNullableDateTime("DataConsegna");
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
            DELETE FROM ImportGuerzoni 
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
            FROM ImportGuerzoni
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
        public void SetProcessedStatus(int idImportGuerzoni, bool isProcessed, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        UPDATE ImportGuerzoni 
        SET IsProcessed = @IsProcessed, 
            DataModifica = GETDATE(), 
            Utente = @Utente 
        WHERE IdImportGuerzoni = @IdImportGuerzoni";

            using var cmd = new SqlCommand(sql, conn, tran);

            cmd.Parameters.Add("@IdImportGuerzoni", SqlDbType.Int).Value = idImportGuerzoni;
            cmd.Parameters.Add("@IsProcessed", SqlDbType.Bit).Value = isProcessed;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = utente;

            cmd.ExecuteNonQuery();
        }
    }
}
