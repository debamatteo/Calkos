/*
    ============================================================
    IMPORT SYSTEMP REPOSITORY — PROMEMORIA ARCHITETTURALE
    ============================================================

    Questo repository gestisce la tabella ImportSystemP, che contiene
    tutte le righe importate dai file SYSTEMP prima della validazione
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
      - Nessuna modifica alla logica di business o al flusso SYSTEMP.

    Questo repository è considerato “sensibile”: ogni modifica deve
    essere minima, motivata e verificata con attenzione, per evitare
    impatti sul processo di importazione SYSTEMP.
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
    public class ImportSystemPRepository : IImportSystemPRepository
    {
        private readonly string _connectionString;

        public ImportSystemPRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ---------------------------------------------------------
        // INSERT UNA RIGA (TRANSazionale)
        // ---------------------------------------------------------
        public ImportSystemP Insert(ImportSystemP item, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        INSERT INTO ImportSystemP (
            IdFileImportato, OrdineDDT, Cliente, Kg, Materiale,
            LavorazionePrezzoAcquisto, BaseAcquisto, CIT, PrezzoAcquisto,
            DataRiferimentoPrezzoAcquisto, CIT_EUR_KG, CITsuAcquisto,
            ProvvigioneUnitaria, ValoreProvvigioneFissa,
            LavorazionePrezzoVendita, BaseVendita, CITsuVendita, PrezzoVendita,
            Delta, PercentualeDelta, TotaleVariabileProvvigione, TotaleProvvigione,
            DataConsegnaIpotetica, Agente, Scadenza, Fatturare, IdMandatario,
            UniqueKey, IsProcessed, DataInserimento, DataModifica, Utente
        )
        VALUES (
            @IdFileImportato, @OrdineDDT, @Cliente, @Kg, @Materiale,
            @LavorazionePrezzoAcquisto, @BaseAcquisto, @CIT, @PrezzoAcquisto,
            @DataRiferimentoPrezzoAcquisto, @CIT_EUR_KG, @CITsuAcquisto,
            @ProvvigioneUnitaria, @ValoreProvvigioneFissa,
            @LavorazionePrezzoVendita, @BaseVendita, @CITsuVendita, @PrezzoVendita,
            @Delta, @PercentualeDelta, @TotaleVariabileProvvigione, @TotaleProvvigione,
            @DataConsegnaIpotetica, @Agente, @Scadenza, @Fatturare, @IdMandatario,
            @UniqueKey, @IsProcessed, @DataInserimento, @DataModifica, @Utente
        );
        SELECT SCOPE_IDENTITY();";

            using var cmd = new SqlCommand(sql, conn, tran);

            PrepareParameters(cmd);
            SetParametersValues(cmd, item, utente);

            item.IdImportSystemP = Convert.ToInt32(cmd.ExecuteScalar());
            return item;
        }

        // ---------------------------------------------------------
        // INSERT RANGE (TRANSazionale)
        // ---------------------------------------------------------
        public void InsertRange(IEnumerable<ImportSystemP> items, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        INSERT INTO ImportSystemP (
            IdFileImportato, OrdineDDT, Cliente, Kg, Materiale,
            LavorazionePrezzoAcquisto, BaseAcquisto, CIT, PrezzoAcquisto,
            DataRiferimentoPrezzoAcquisto, CIT_EUR_KG, CITsuAcquisto,
            ProvvigioneUnitaria, ValoreProvvigioneFissa,
            LavorazionePrezzoVendita, BaseVendita, CITsuVendita, PrezzoVendita,
            Delta, PercentualeDelta, TotaleVariabileProvvigione, TotaleProvvigione,
            DataConsegnaIpotetica, Agente, Scadenza, Fatturare, IdMandatario,
            UniqueKey, IsProcessed, DataInserimento, DataModifica, Utente
        )
        VALUES (
            @IdFileImportato, @OrdineDDT, @Cliente, @Kg, @Materiale,
            @LavorazionePrezzoAcquisto, @BaseAcquisto, @CIT, @PrezzoAcquisto,
            @DataRiferimentoPrezzoAcquisto, @CIT_EUR_KG, @CITsuAcquisto,
            @ProvvigioneUnitaria, @ValoreProvvigioneFissa,
            @LavorazionePrezzoVendita, @BaseVendita, @CITsuVendita, @PrezzoVendita,
            @Delta, @PercentualeDelta, @TotaleVariabileProvvigione, @TotaleProvvigione,
            @DataConsegnaIpotetica, @Agente, @Scadenza, @Fatturare, @IdMandatario,
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

            AddDecimal(cmd, "@Kg");
            cmd.Parameters.Add("@Materiale", SqlDbType.NVarChar);

            AddDecimal(cmd, "@LavorazionePrezzoAcquisto");
            AddDecimal(cmd, "@BaseAcquisto");
            AddDecimal(cmd, "@CIT");
            AddDecimal(cmd, "@PrezzoAcquisto");

            cmd.Parameters.Add("@DataRiferimentoPrezzoAcquisto", SqlDbType.Date);

            AddDecimal(cmd, "@CIT_EUR_KG");
            AddDecimal(cmd, "@CITsuAcquisto");

            AddDecimal(cmd, "@ProvvigioneUnitaria");
            AddDecimal(cmd, "@ValoreProvvigioneFissa");

            AddDecimal(cmd, "@LavorazionePrezzoVendita");
            AddDecimal(cmd, "@BaseVendita");
            AddDecimal(cmd, "@CITsuVendita");
            AddDecimal(cmd, "@PrezzoVendita");

            AddDecimal(cmd, "@Delta");
            AddDecimal(cmd, "@PercentualeDelta");
            AddDecimal(cmd, "@TotaleVariabileProvvigione");
            AddDecimal(cmd, "@TotaleProvvigione");

            cmd.Parameters.Add("@DataConsegnaIpotetica", SqlDbType.Date);

            cmd.Parameters.Add("@Agente", SqlDbType.NVarChar);
            cmd.Parameters.Add("@Scadenza", SqlDbType.Date);

            cmd.Parameters.Add("@Fatturare", SqlDbType.Bit);
            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int);

            cmd.Parameters.Add("@UniqueKey", SqlDbType.NVarChar);
            cmd.Parameters.Add("@IsProcessed", SqlDbType.Bit);

            cmd.Parameters.Add("@DataInserimento", SqlDbType.DateTime);
            cmd.Parameters.Add("@DataModifica", SqlDbType.DateTime);
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar);
        }

        private void AddDecimal(SqlCommand cmd, string name)
        {
            var p = cmd.Parameters.Add(name, SqlDbType.Decimal);
            p.Precision = 18;
            p.Scale = 6;
        }

        // ---------------------------------------------------------
        // SET PARAMETER VALUES
        // ---------------------------------------------------------
        private void SetParametersValues(SqlCommand cmd, ImportSystemP item, string utente)
        {
            cmd.Parameters["@IdFileImportato"].Value = item.IdFileImportato;
            cmd.Parameters["@OrdineDDT"].Value = (object?)item.OrdineDDT ?? DBNull.Value;
            cmd.Parameters["@Cliente"].Value = (object?)item.Cliente ?? DBNull.Value;

            cmd.Parameters["@Kg"].Value = (object?)item.Kg ?? DBNull.Value;
            cmd.Parameters["@Materiale"].Value = (object?)item.Materiale ?? DBNull.Value;

            cmd.Parameters["@LavorazionePrezzoAcquisto"].Value = (object?)item.LavorazionePrezzoAcquisto ?? DBNull.Value;
            cmd.Parameters["@BaseAcquisto"].Value = (object?)item.BaseAcquisto ?? DBNull.Value;
            cmd.Parameters["@CIT"].Value = (object?)item.CIT ?? DBNull.Value;
            cmd.Parameters["@PrezzoAcquisto"].Value = (object?)item.PrezzoAcquisto ?? DBNull.Value;

            cmd.Parameters["@DataRiferimentoPrezzoAcquisto"].Value =
                item.DataRiferimentoPrezzoAcquisto == null ? DBNull.Value : item.DataRiferimentoPrezzoAcquisto;

            cmd.Parameters["@CIT_EUR_KG"].Value = (object?)item.CIT_EUR_KG ?? DBNull.Value;
            cmd.Parameters["@CITsuAcquisto"].Value = (object?)item.CITsuAcquisto ?? DBNull.Value;

            cmd.Parameters["@ProvvigioneUnitaria"].Value = (object?)item.ProvvigioneUnitaria ?? DBNull.Value;
            cmd.Parameters["@ValoreProvvigioneFissa"].Value = (object?)item.ValoreProvvigioneFissa ?? DBNull.Value;

            cmd.Parameters["@LavorazionePrezzoVendita"].Value = (object?)item.LavorazionePrezzoVendita ?? DBNull.Value;
            cmd.Parameters["@BaseVendita"].Value = (object?)item.BaseVendita ?? DBNull.Value;
            cmd.Parameters["@CITsuVendita"].Value = (object?)item.CITsuVendita ?? DBNull.Value;
            cmd.Parameters["@PrezzoVendita"].Value = (object?)item.PrezzoVendita ?? DBNull.Value;

            cmd.Parameters["@Delta"].Value = (object?)item.Delta ?? DBNull.Value;
            cmd.Parameters["@PercentualeDelta"].Value = (object?)item.PercentualeDelta ?? DBNull.Value;//01/05/2026

            cmd.Parameters["@TotaleVariabileProvvigione"].Value = (object?)item.TotaleVariabileProvvigione ?? DBNull.Value;
            cmd.Parameters["@TotaleProvvigione"].Value = (object?)item.TotaleProvvigione ?? DBNull.Value;

            cmd.Parameters["@DataConsegnaIpotetica"].Value =
                item.DataConsegnaIpotetica == null ? DBNull.Value : item.DataConsegnaIpotetica;

            cmd.Parameters["@Agente"].Value = (object?)item.Agente ?? DBNull.Value;

            cmd.Parameters["@Scadenza"].Value =
                item.Scadenza == null ? DBNull.Value : item.Scadenza;

            cmd.Parameters["@Fatturare"].Value =
                item.Fatturare == null ? DBNull.Value : (item.Fatturare == true ? 1 : 0);

            cmd.Parameters["@IdMandatario"].Value = (object?)item.IdMandatario ?? DBNull.Value;

            cmd.Parameters["@UniqueKey"].Value = (object?)item.UniqueKey ?? DBNull.Value;
            cmd.Parameters["@IsProcessed"].Value = item.IsProcessed;

            cmd.Parameters["@DataInserimento"].Value =
                item.DataInserimento == DateTime.MinValue ? DateTime.Now : item.DataInserimento;

            cmd.Parameters["@DataModifica"].Value =
                item.DataModifica == null ? DBNull.Value : item.DataModifica;

            cmd.Parameters["@Utente"].Value = (object?)utente ?? DBNull.Value;
        }

        // ---------------------------------------------------------
        // GET BY FILE
        // ---------------------------------------------------------
        public IEnumerable<ImportSystemP> GetByFile(int idFileImportato)
        {
            const string sql = "SELECT * FROM ImportSystemP WHERE IdFileImportato = @idFileImportato";

            var lista = new List<ImportSystemP>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@idFileImportato", SqlDbType.Int).Value = idFileImportato;

            conn.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var r = new ImportSystemP();

                r.IdImportSystemP = reader.GetIntSafe("IdImportSystemP");
                r.IdFileImportato = reader.GetIntSafe("IdFileImportato");

                r.OrdineDDT = reader.GetStringSafe("OrdineDDT");
                r.Cliente = reader.GetStringSafe("Cliente");

                r.Kg = reader.GetNullableDecimal("Kg");
                r.Materiale = reader.GetStringSafe("Materiale");

                r.LavorazionePrezzoAcquisto = reader.GetNullableDecimal("LavorazionePrezzoAcquisto");
                r.BaseAcquisto = reader.GetNullableDecimal("BaseAcquisto");
                r.CIT = reader.GetNullableDecimal("CIT");
                r.PrezzoAcquisto = reader.GetNullableDecimal("PrezzoAcquisto");

                r.DataRiferimentoPrezzoAcquisto = reader.GetNullableDateTime("DataRiferimentoPrezzoAcquisto");

                r.CIT_EUR_KG = reader.GetNullableDecimal("CIT_EUR_KG");
                r.CITsuAcquisto = reader.GetNullableDecimal("CITsuAcquisto");

                r.ProvvigioneUnitaria = reader.GetNullableDecimal("ProvvigioneUnitaria");
                r.ValoreProvvigioneFissa = reader.GetNullableDecimal("ValoreProvvigioneFissa");

                r.LavorazionePrezzoVendita = reader.GetNullableDecimal("LavorazionePrezzoVendita");
                r.BaseVendita = reader.GetNullableDecimal("BaseVendita");
                r.CITsuVendita = reader.GetNullableDecimal("CITsuVendita");
                r.PrezzoVendita = reader.GetNullableDecimal("PrezzoVendita");

                r.Delta = reader.GetNullableDecimal("Delta");
                r.PercentualeDelta = reader.GetNullableDecimal("PercentualeDelta");
                r.TotaleVariabileProvvigione = reader.GetNullableDecimal("TotaleVariabileProvvigione");
                r.TotaleProvvigione = reader.GetNullableDecimal("TotaleProvvigione");

                r.DataConsegnaIpotetica = reader.GetNullableDateTime("DataConsegnaIpotetica");
                r.Agente = reader.GetStringSafe("Agente");
                r.Scadenza = reader.GetNullableDateTime("Scadenza");

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
            DELETE FROM ImportSystemP 
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
            FROM ImportSystemP
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
        public void SetProcessedStatus(int idImportSystemP, bool isProcessed, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        UPDATE ImportSystemP 
        SET IsProcessed = @IsProcessed, 
            DataModifica = GETDATE(), 
            Utente = @Utente 
        WHERE IdImportSystemP = @IdImportSystemP";

            using var cmd = new SqlCommand(sql, conn, tran);

            cmd.Parameters.Add("@IdImportSystemP", SqlDbType.Int).Value = idImportSystemP;
            cmd.Parameters.Add("@IsProcessed", SqlDbType.Bit).Value = isProcessed;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = utente;

            cmd.ExecuteNonQuery();
        }
    }
}
