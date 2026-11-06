using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Infrastructure.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;
/*
    ============================================================
    IMPORT CST REPOSITORY — PROMEMORIA ARCHITETTURALE
    ============================================================

    Questo repository gestisce la tabella ImportCST, che contiene
    tutte le righe importate dai file CST prima della validazione
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
      - Nessuna modifica alla logica di business o al flusso CST.

    Questo repository è considerato “sensibile”: ogni modifica deve
    essere minima, motivata e verificata con attenzione, per evitare
    impatti sul processo di importazione CST.
    ============================================================
*/

namespace CalkosManager.Infrastructure.Repositories
{
    public class ImportCSTRepository : Domain.Interfaces.Repositories.IImportCSTRepository
    {
        private readonly string _connectionString;

        public ImportCSTRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ---------------------------------------------------------
        // INSERT UNA RIGA (TRANSazionale)
        // ---------------------------------------------------------
        public ImportCST Insert(ImportCST item, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        INSERT INTO ImportCST (
            IdFileImportato, OrdineDDT, Cliente, Kg, CostoLavorazione, Materiale,
            PrezzoBase, PrezzoEuroRiferimento,PrezzoVendita, PrezzoBaseConLavorazione,
            ProvvigioneFissa01_05, ProvvigioneFissa03, TotaleProvvigioneFissa,Perc_Delta_Provvigione,
            CIT, Delta, TotaleProvvigioneVariabile, TotaleProvvigioneFattura,
            DataConsegna, Agente, Fatturare, Scadenza, IdMandatario,
            UniqueKey, IsProcessed, DataInserimento, DataModifica, Utente
        )
        VALUES (
            @IdFileImportato, @OrdineDDT, @Cliente, @Kg, @CostoLavorazione, @Materiale,
            @PrezzoBase, @PrezzoEuroRiferimento,@PrezzoVendita, @PrezzoBaseConLavorazione,
            @ProvvigioneFissa01_05, @ProvvigioneFissa03, @TotaleProvvigioneFissa,@Perc_Delta_Provvigione,
            @CIT, @Delta, @TotaleProvvigioneVariabile, @TotaleProvvigioneFattura,
            @DataConsegna, @Agente, @Fatturare, @Scadenza, @IdMandatario,
            @UniqueKey, @IsProcessed, @DataInserimento, @DataModifica, @Utente
        );
        SELECT SCOPE_IDENTITY();";

            using var cmd = new SqlCommand(sql, conn, tran);

            PrepareParameters(cmd);
            SetParametersValues(cmd, item, utente);

            item.IdImportCST = Convert.ToInt32(cmd.ExecuteScalar());
            return item;
        }

        // ---------------------------------------------------------
        // INSERT RANGE (TRANSazionale)
        // ---------------------------------------------------------
        public void InsertRange(IEnumerable<ImportCST> items, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        INSERT INTO ImportCST (
            IdFileImportato, OrdineDDT, Cliente, Kg, CostoLavorazione, Materiale,
            PrezzoBase, PrezzoEuroRiferimento,PrezzoVendita, PrezzoBaseConLavorazione,
            ProvvigioneFissa01_05, ProvvigioneFissa03, TotaleProvvigioneFissa,Perc_Delta_Provvigione,
            CIT, Delta, TotaleProvvigioneVariabile, TotaleProvvigioneFattura,
            DataConsegna, Agente, Fatturare, Scadenza, IdMandatario,
            UniqueKey, IsProcessed, DataInserimento, DataModifica, Utente
        )
        VALUES (
            @IdFileImportato, @OrdineDDT, @Cliente, @Kg, @CostoLavorazione, @Materiale,
            @PrezzoBase, @PrezzoEuroRiferimento,@PrezzoVendita, @PrezzoBaseConLavorazione,
            @ProvvigioneFissa01_05, @ProvvigioneFissa03, @TotaleProvvigioneFissa,@Perc_Delta_Provvigione,
            @CIT, @Delta, @TotaleProvvigioneVariabile, @TotaleProvvigioneFattura,
            @DataConsegna, @Agente, @Fatturare, @Scadenza, @IdMandatario,
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

            AddDecimalParam(cmd, "@Kg", 6);
            AddDecimalParam(cmd, "@CostoLavorazione", 6);
            cmd.Parameters.Add("@Materiale", SqlDbType.NVarChar);

            AddDecimalParam(cmd, "@PrezzoBase", 6);

            
            AddDecimalParam(cmd, "@PrezzoEuroRiferimento", 6);
            AddDecimalParam(cmd, "@PrezzoVendita", 6);
            AddDecimalParam(cmd, "@PrezzoBaseConLavorazione", 6);

            AddDecimalParam(cmd, "@ProvvigioneFissa01_05", 6);
            AddDecimalParam(cmd, "@ProvvigioneFissa03", 6);
            AddDecimalParam(cmd, "@Perc_Delta_Provvigione", 6);//30/04/2026
            AddDecimalParam(cmd, "@TotaleProvvigioneFissa", 6);

            AddDecimalParam(cmd, "@CIT", 6);
            AddDecimalParam(cmd, "@Delta", 6);
            AddDecimalParam(cmd, "@TotaleProvvigioneVariabile", 6);
            AddDecimalParam(cmd, "@TotaleProvvigioneFattura", 6);

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
        private void SetParametersValues(SqlCommand cmd, ImportCST item, string utente)
        {
            cmd.Parameters["@IdFileImportato"].Value = item.IdFileImportato;
            cmd.Parameters["@OrdineDDT"].Value = (object?)item.OrdineDDT ?? DBNull.Value;
            cmd.Parameters["@Cliente"].Value = (object?)item.Cliente ?? DBNull.Value;

            cmd.Parameters["@Kg"].Value = (object?)item.Kg ?? DBNull.Value;
            cmd.Parameters["@CostoLavorazione"].Value = (object?)item.CostoLavorazione ?? DBNull.Value;
            cmd.Parameters["@Materiale"].Value = (object?)item.Materiale ?? DBNull.Value;

            cmd.Parameters["@PrezzoBase"].Value = (object?)item.PrezzoBase ?? DBNull.Value;

            

            cmd.Parameters["@PrezzoEuroRiferimento"].Value = (object?)item.PrezzoEuroRiferimento ?? DBNull.Value;
            cmd.Parameters["@PrezzoVendita"].Value = (object?)item.PrezzoVendita ?? DBNull.Value;
            cmd.Parameters["@PrezzoBaseConLavorazione"].Value = (object?)item.PrezzoBaseConLavorazione ?? DBNull.Value;

            cmd.Parameters["@ProvvigioneFissa01_05"].Value = (object?)item.ProvvigioneFissa01_05 ?? DBNull.Value;
            cmd.Parameters["@ProvvigioneFissa03"].Value = (object?)item.ProvvigioneFissa03 ?? DBNull.Value;
            cmd.Parameters["@Perc_Delta_Provvigione"].Value = (object?)item.Perc_Delta_Provvigione ?? DBNull.Value;////01/05/2026


            cmd.Parameters["@TotaleProvvigioneFissa"].Value = (object?)item.TotaleProvvigioneFissa ?? DBNull.Value;

            cmd.Parameters["@CIT"].Value = (object?)item.CIT ?? DBNull.Value;
            cmd.Parameters["@Delta"].Value = (object?)item.Delta ?? DBNull.Value;
            cmd.Parameters["@TotaleProvvigioneVariabile"].Value = (object?)item.TotaleProvvigioneVariabile ?? DBNull.Value;
            cmd.Parameters["@TotaleProvvigioneFattura"].Value = (object?)item.TotaleProvvigioneFattura ?? DBNull.Value;

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
        public IEnumerable<ImportCST> GetByFile(int idFileImportato)
        {
            const string sql = "SELECT * FROM ImportCST WHERE IdFileImportato = @idFileImportato";

            var lista = new List<ImportCST>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@idFileImportato", SqlDbType.Int).Value = idFileImportato;

            conn.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var r = new ImportCST();

                r.IdImportCST = reader.GetIntSafe("IdImportCST");
                r.IdFileImportato = reader.GetIntSafe("IdFileImportato");

                r.OrdineDDT = reader.GetStringSafe("OrdineDDT");
                r.Cliente = reader.GetStringSafe("Cliente");

                r.Kg = reader.GetNullableDecimal("Kg");
                r.CostoLavorazione = reader.GetNullableDecimal("CostoLavorazione");
                r.Materiale = reader.GetStringSafe("Materiale");

                r.PrezzoBase = reader.GetNullableDecimal("PrezzoBase");
                
                 r.PrezzoEuroRiferimento = reader.GetNullableDecimal("PrezzoEuroRiferimento");
                r.PrezzoVendita = reader.GetNullableDecimal("PrezzoVendita");
                r.PrezzoBaseConLavorazione = reader.GetNullableDecimal("PrezzoBaseConLavorazione");

                r.ProvvigioneFissa01_05 = reader.GetNullableDecimal("ProvvigioneFissa01_05");
                r.ProvvigioneFissa03 = reader.GetNullableDecimal("ProvvigioneFissa03");
                r.Perc_Delta_Provvigione = reader.GetNullableDecimal("Perc_Delta_Provvigione");   //30/04/2026
                r.TotaleProvvigioneFissa = reader.GetNullableDecimal("TotaleProvvigioneFissa");

                r.CIT = reader.GetNullableDecimal("CIT");
                r.Delta = reader.GetNullableDecimal("Delta");
                r.TotaleProvvigioneVariabile = reader.GetNullableDecimal("TotaleProvvigioneVariabile");
                r.TotaleProvvigioneFattura = reader.GetNullableDecimal("TotaleProvvigioneFattura");

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
            DELETE FROM ImportCST 
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
            FROM ImportCST
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
        public void SetProcessedStatus(int idImportCST, bool isProcessed, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        UPDATE ImportCST 
        SET IsProcessed = @IsProcessed, 
            DataModifica = GETDATE(), 
            Utente = @Utente 
        WHERE IdImportCST = @IdImportCST";

            using var cmd = new SqlCommand(sql, conn, tran);

            cmd.Parameters.Add("@IdImportCST", SqlDbType.Int).Value = idImportCST;
            cmd.Parameters.Add("@IsProcessed", SqlDbType.Bit).Value = isProcessed;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = utente;

            cmd.ExecuteNonQuery();
        }
    }
}
