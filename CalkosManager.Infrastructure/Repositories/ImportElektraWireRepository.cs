using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Infrastructure.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;

/*
    ============================================================
    IMPORT ELEKTRAWIRE REPOSITORY — PROMEMORIA ARCHITETTURALE
    ============================================================

    Questo repository gestisce la tabella ImportElektraWire, che contiene
    tutte le righe importate dai file ELEKTRAWIRE prima della validazione
    e della trasformazione nei prospetti definitivi.

    ✔ Repository “speciale”: replicato specularmente dal modello Cobral.
    
    ✔ Caratteristiche principali:
      - Gestisce la struttura provvigionale specifica di ElektraWire.
      - Supporta inserimenti massivi tramite InsertRange, ottimizzato 
        con parametri riutilizzati.
      - Usa SQL inline per massima velocità e controllo.
      - Usa SqlReaderExtensions per lettura sicura dei campi.

    ✔ Standard tecnici:
      - NIENTE AddWithValue: parametri sempre tipizzati.
      - Per i DECIMAL è obbligatorio impostare Precision e Scale (18,3).
      - Mapping basato su nomi colonna.
      - Gestione date con protezione MinValue.
    ============================================================
*/

namespace CalkosManager.Infrastructure.Repositories
{
    public class ImportElektraWireRepository : IImportElektraWireRepository
    {
        private readonly string _connectionString;

        public ImportElektraWireRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ---------------------------------------------------------
        // INSERT UNA RIGA (TRANSazionale)
        // ---------------------------------------------------------
        public ImportElektraWire Insert(ImportElektraWire item, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        INSERT INTO ImportElektraWire (
            IdFileImportato, IdMandatario, OrdineDDT, Cliente, Misure, Kg, 
            CostoLavorazione, Materiale, Prezzo, PrezzoConLavorazione, PrezzoVendita, DeltaMargine,Perc_Delta_Provvigione,
            ValUnitProvvigioneFissa1_5, Delta1ProvvigioneFissa1_5, Delta2ProvvigioneFissa1_5, TotProvvigioneFissa1_5,
            ValUnitProvvigioneFissa_03, Delta1ProvvigioneFissa_03, Delta2ProvvigioneFissa_03, TotProvvigioneFissa_03,
            ValQuotaFissa_1_5, ValQuotaFissa_03, TotaleFissoProvvigione, DeltaResiduoVariabile,
            TotaleVariabileProvvigione, TotaleSommaProvvigioni, DataConsegna, TotaleProvvigioneDaFatturare,
            Fatturare, UniqueKey, IsProcessed, DataInserimento, DataModifica, Utente
        )
        VALUES (
            @IdFileImportato, @IdMandatario, @OrdineDDT, @Cliente, @Misure, @Kg, 
            @CostoLavorazione, @Materiale, @Prezzo, @PrezzoConLavorazione, @PrezzoVendita, @DeltaMargine,@Perc_Delta_Provvigione,
            @ValUnitProvvigioneFissa1_5, @Delta1ProvvigioneFissa1_5, @Delta2ProvvigioneFissa1_5, @TotProvvigioneFissa1_5,
            @ValUnitProvvigioneFissa_03, @Delta1ProvvigioneFissa_03, @Delta2ProvvigioneFissa_03, @TotProvvigioneFissa_03,
            @ValQuotaFissa_1_5, @ValQuotaFissa_03, @TotaleFissoProvvigione, @DeltaResiduoVariabile,
            @TotaleVariabileProvvigione, @TotaleSommaProvvigioni, @DataConsegna, @TotaleProvvigioneDaFatturare,
            @Fatturare, @UniqueKey, @IsProcessed, @DataInserimento, @DataModifica, @Utente
        );
        SELECT SCOPE_IDENTITY();";

            using var cmd = new SqlCommand(sql, conn, tran);
            PrepareParameters(cmd);
            SetParametersValues(cmd, item, utente);

            item.IdImportElektraWire = Convert.ToInt32(cmd.ExecuteScalar());
            return item;
        }

        // ---------------------------------------------------------
        // INSERT RANGE (TRANSazionale)
        // ---------------------------------------------------------
        public void InsertRange(IEnumerable<ImportElektraWire> items, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        INSERT INTO ImportElektraWire (
            IdFileImportato, IdMandatario, OrdineDDT, Cliente, Misure, Kg, 
            CostoLavorazione, Materiale, Prezzo, PrezzoConLavorazione, PrezzoVendita, DeltaMargine,Perc_Delta_Provvigione,
            ValUnitProvvigioneFissa1_5, Delta1ProvvigioneFissa1_5, Delta2ProvvigioneFissa1_5, TotProvvigioneFissa1_5,
            ValUnitProvvigioneFissa_03, Delta1ProvvigioneFissa_03, Delta2ProvvigioneFissa_03, TotProvvigioneFissa_03,
            ValQuotaFissa_1_5, ValQuotaFissa_03, TotaleFissoProvvigione, DeltaResiduoVariabile,
            TotaleVariabileProvvigione, TotaleSommaProvvigioni, DataConsegna, TotaleProvvigioneDaFatturare,
            Fatturare, UniqueKey, IsProcessed, DataInserimento, DataModifica, Utente
        )
        VALUES (
            @IdFileImportato, @IdMandatario, @OrdineDDT, @Cliente, @Misure, @Kg, 
            @CostoLavorazione, @Materiale, @Prezzo, @PrezzoConLavorazione, @PrezzoVendita, @DeltaMargine,@Perc_Delta_Provvigione,
            @ValUnitProvvigioneFissa1_5, @Delta1ProvvigioneFissa1_5, @Delta2ProvvigioneFissa1_5, @TotProvvigioneFissa1_5,
            @ValUnitProvvigioneFissa_03, @Delta1ProvvigioneFissa_03, @Delta2ProvvigioneFissa_03, @TotProvvigioneFissa_03,
            @ValQuotaFissa_1_5, @ValQuotaFissa_03, @TotaleFissoProvvigione, @DeltaResiduoVariabile,
            @TotaleVariabileProvvigione, @TotaleSommaProvvigioni, @DataConsegna, @TotaleProvvigioneDaFatturare,
            @Fatturare, @UniqueKey, @IsProcessed, @DataInserimento, @DataModifica, @Utente
        );";

            using var cmd = new SqlCommand(sql, conn, tran);
            PrepareParameters(cmd);

            foreach (var item in items)
            {
                SetParametersValues(cmd, item, utente);
                cmd.ExecuteNonQuery();
            }
        }


        private void SetParametersValues(SqlCommand cmd, ImportElektraWire item, string utente)
        {
            cmd.Parameters["@IdFileImportato"].Value = item.IdFileImportato;
            cmd.Parameters["@IdMandatario"].Value = (object?)item.IdMandatario ?? DBNull.Value;
            cmd.Parameters["@OrdineDDT"].Value = (object?)item.OrdineDDT ?? DBNull.Value;
            cmd.Parameters["@Cliente"].Value = (object?)item.Cliente ?? DBNull.Value;
            cmd.Parameters["@Misure"].Value = (object?)item.Misure ?? DBNull.Value;
            cmd.Parameters["@Materiale"].Value = (object?)item.Materiale ?? DBNull.Value;

            // --- Decimali (18, 3) ---
            cmd.Parameters["@Kg"].Value = (object?)item.Kg ?? DBNull.Value;
            cmd.Parameters["@CostoLavorazione"].Value = (object?)item.CostoLavorazione ?? DBNull.Value;
            cmd.Parameters["@Prezzo"].Value = (object?)item.Prezzo ?? DBNull.Value;
            cmd.Parameters["@PrezzoConLavorazione"].Value = (object?)item.PrezzoConLavorazione ?? DBNull.Value;
            cmd.Parameters["@PrezzoVendita"].Value = (object?)item.PrezzoVendita ?? DBNull.Value;
            cmd.Parameters["@DeltaMargine"].Value = (object?)item.DeltaMargine ?? DBNull.Value;
            cmd.Parameters["@Perc_Delta_Provvigione"].Value = (object?)item.Perc_Delta_Provvigione ?? DBNull.Value;//01/05/2026  


            cmd.Parameters["@TotaleProvvigioneDaFatturare"].Value = (object?)item.TotaleProvvigioneDaFatturare ?? DBNull.Value;

            // --- Blocchi Provvigionali ElektraWire (18, 5) ---
            cmd.Parameters["@ValUnitProvvigioneFissa1_5"].Value = (object?)item.ValUnitProvvigioneFissa1_5 ?? DBNull.Value;
            cmd.Parameters["@Delta1ProvvigioneFissa1_5"].Value = (object?)item.Delta1ProvvigioneFissa1_5 ?? DBNull.Value;
            cmd.Parameters["@Delta2ProvvigioneFissa1_5"].Value = (object?)item.Delta2ProvvigioneFissa1_5 ?? DBNull.Value;
            cmd.Parameters["@TotProvvigioneFissa1_5"].Value = (object?)item.TotProvvigioneFissa1_5 ?? DBNull.Value;
            cmd.Parameters["@ValUnitProvvigioneFissa_03"].Value = (object?)item.ValUnitProvvigioneFissa_03 ?? DBNull.Value;
            cmd.Parameters["@Delta1ProvvigioneFissa_03"].Value = (object?)item.Delta1ProvvigioneFissa_03 ?? DBNull.Value;
            cmd.Parameters["@Delta2ProvvigioneFissa_03"].Value = (object?)item.Delta2ProvvigioneFissa_03 ?? DBNull.Value;
            cmd.Parameters["@TotProvvigioneFissa_03"].Value = (object?)item.TotProvvigioneFissa_03 ?? DBNull.Value;
            cmd.Parameters["@ValQuotaFissa_1_5"].Value = (object?)item.ValQuotaFissa_1_5 ?? DBNull.Value;
            cmd.Parameters["@ValQuotaFissa_03"].Value = (object?)item.ValQuotaFissa_03 ?? DBNull.Value;
            cmd.Parameters["@TotaleFissoProvvigione"].Value = (object?)item.TotaleFissoProvvigione ?? DBNull.Value;
            cmd.Parameters["@DeltaResiduoVariabile"].Value = (object?)item.DeltaResiduoVariabile ?? DBNull.Value;
            cmd.Parameters["@TotaleVariabileProvvigione"].Value = (object?)item.TotaleVariabileProvvigione ?? DBNull.Value;
            cmd.Parameters["@TotaleSommaProvvigioni"].Value = (object?)item.TotaleSommaProvvigioni ?? DBNull.Value;

            // --- Campi Tecnici e Date ---
            cmd.Parameters["@DataConsegna"].Value = (item.DataConsegna == null || item.DataConsegna == DateTime.MinValue) ? DBNull.Value : item.DataConsegna;
            //cmd.Parameters["@Fatturare"].Value = (object?)item.Fatturare ?? DBNull.Value;
            cmd.Parameters["@Fatturare"].Value = item.Fatturare == null ? DBNull.Value : (item.Fatturare == true ? 1 : 0);//19/04/2026
            cmd.Parameters["@UniqueKey"].Value = (object?)item.UniqueKey ?? DBNull.Value;
            cmd.Parameters["@IsProcessed"].Value = item.IsProcessed;
            cmd.Parameters["@DataInserimento"].Value = item.DataInserimento == DateTime.MinValue ? DateTime.Now : item.DataInserimento;
            cmd.Parameters["@DataModifica"].Value = (item.DataModifica == null || item.DataModifica == DateTime.MinValue) ? DBNull.Value : item.DataModifica;
            cmd.Parameters["@Utente"].Value = (object?)utente ?? DBNull.Value;
        }

        private void PrepareParameters(SqlCommand cmd)
        {
            cmd.Parameters.Add("@IdFileImportato", SqlDbType.Int);
            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int);
            cmd.Parameters.Add("@OrdineDDT", SqlDbType.NVarChar);
            cmd.Parameters.Add("@Cliente", SqlDbType.NVarChar);
            cmd.Parameters.Add("@Misure", SqlDbType.NVarChar);
            cmd.Parameters.Add("@Materiale", SqlDbType.NVarChar);

            // --- Decimali (18, 6) ---
            AddDecimalParam(cmd, "@Kg", 6);
            AddDecimalParam(cmd, "@CostoLavorazione", 6);
            AddDecimalParam(cmd, "@Prezzo", 6);
            AddDecimalParam(cmd, "@PrezzoConLavorazione", 6);
            AddDecimalParam(cmd, "@PrezzoVendita", 6);
            AddDecimalParam(cmd, "@DeltaMargine", 6);
            AddDecimalParam(cmd, "@Perc_Delta_Provvigione", 6);//27/04/2026
            AddDecimalParam(cmd, "@TotaleProvvigioneDaFatturare", 6);

            // --- Blocchi Provvigionali ElektraWire (18, 6) ---
            AddDecimalParam(cmd, "@ValUnitProvvigioneFissa1_5", 6);
            AddDecimalParam(cmd, "@Delta1ProvvigioneFissa1_5", 6);
            AddDecimalParam(cmd, "@Delta2ProvvigioneFissa1_5", 6);
            AddDecimalParam(cmd, "@TotProvvigioneFissa1_5", 6);
            AddDecimalParam(cmd, "@ValUnitProvvigioneFissa_03", 6);
            AddDecimalParam(cmd, "@Delta1ProvvigioneFissa_03", 6);
            AddDecimalParam(cmd, "@Delta2ProvvigioneFissa_03", 6);
            AddDecimalParam(cmd, "@TotProvvigioneFissa_03", 6);
            AddDecimalParam(cmd, "@ValQuotaFissa_1_5", 6);
            AddDecimalParam(cmd, "@ValQuotaFissa_03", 6);
            AddDecimalParam(cmd, "@TotaleFissoProvvigione", 6);
            AddDecimalParam(cmd, "@DeltaResiduoVariabile", 6);
            AddDecimalParam(cmd, "@TotaleVariabileProvvigione", 6);
            AddDecimalParam(cmd, "@TotaleSommaProvvigioni", 6);

            // --- Campi Tecnici ---
            cmd.Parameters.Add("@DataConsegna", SqlDbType.Date);
            cmd.Parameters.Add("@Fatturare", SqlDbType.Bit); //19/04/2026//cmd.Parameters.Add("@Fatturare", SqlDbType.NVarChar);
            cmd.Parameters.Add("@UniqueKey", SqlDbType.NVarChar);
            cmd.Parameters.Add("@IsProcessed", SqlDbType.Bit);
            cmd.Parameters.Add("@DataInserimento", SqlDbType.DateTime);
            cmd.Parameters.Add("@DataModifica", SqlDbType.DateTime);
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar);
        }

        private void AddDecimalParam(SqlCommand cmd, string name, byte scale)
        {
            var param = cmd.Parameters.Add(name, SqlDbType.Decimal);
            param.Precision = 18;
            param.Scale = scale;
        }


        // ---------------------------------------------------------
        // GET BY FILE
        // ---------------------------------------------------------
        /* REPLICA PROTOCOLLO: ImportElektraWireRepository 
            FIX: Mapping completo dei campi provvigionali omessi e gestione date.
        */

        public IEnumerable<ImportElektraWire> GetByFile(int idFileImportato)
        {
            const string sql = "SELECT * FROM ImportElektraWire WHERE IdFileImportato = @idFileImportato";
            var lista = new List<ImportElektraWire>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@idFileImportato", SqlDbType.Int).Value = idFileImportato;

            conn.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var r = new ImportElektraWire();

                // Identificativi e Documento
                r.IdImportElektraWire = reader.GetIntSafe("IdImportElektraWire");
                r.IdFileImportato = reader.GetIntSafe("IdFileImportato");
                r.IdMandatario = reader.GetIntSafe("IdMandatario");
                r.OrdineDDT = reader.GetStringSafe("OrdineDDT");
                r.Cliente = reader.GetStringSafe("Cliente");
                r.Misure = reader.GetStringSafe("Misure");
                r.Materiale = reader.GetStringSafe("Materiale");

                // Valori Economici Base
                r.Kg = reader.GetNullableDecimal("Kg");
                r.CostoLavorazione = reader.GetNullableDecimal("CostoLavorazione");
                r.Prezzo = reader.GetNullableDecimal("Prezzo");
                r.PrezzoConLavorazione = reader.GetNullableDecimal("PrezzoConLavorazione");
                r.PrezzoVendita = reader.GetNullableDecimal("PrezzoVendita");
                r.DeltaMargine = reader.GetNullableDecimal("DeltaMargine");
                r.Perc_Delta_Provvigione = reader.GetNullableDecimal("Perc_Delta_Provvigione");  //27/04/2026

                // --- BLOCCO PROVVIGIONALE 1.5 ---
                r.ValUnitProvvigioneFissa1_5 = reader.GetNullableDecimal("ValUnitProvvigioneFissa1_5");
                r.Delta1ProvvigioneFissa1_5 = reader.GetNullableDecimal("Delta1ProvvigioneFissa1_5");
                r.Delta2ProvvigioneFissa1_5 = reader.GetNullableDecimal("Delta2ProvvigioneFissa1_5");
                r.TotProvvigioneFissa1_5 = reader.GetNullableDecimal("TotProvvigioneFissa1_5");
                r.ValQuotaFissa_1_5 = reader.GetNullableDecimal("ValQuotaFissa_1_5");

                // --- BLOCCO PROVVIGIONALE 0.3 ---
                r.ValUnitProvvigioneFissa_03 = reader.GetNullableDecimal("ValUnitProvvigioneFissa_03");
                r.Delta1ProvvigioneFissa_03 = reader.GetNullableDecimal("Delta1ProvvigioneFissa_03");
                r.Delta2ProvvigioneFissa_03 = reader.GetNullableDecimal("Delta2ProvvigioneFissa_03");
                r.TotProvvigioneFissa_03 = reader.GetNullableDecimal("TotProvvigioneFissa_03");
                r.ValQuotaFissa_03 = reader.GetNullableDecimal("ValQuotaFissa_03");

                // --- TOTALI E RESIDUI ---
                r.TotaleFissoProvvigione = reader.GetNullableDecimal("TotaleFissoProvvigione");
                r.DeltaResiduoVariabile = reader.GetNullableDecimal("DeltaResiduoVariabile");
                r.TotaleVariabileProvvigione = reader.GetNullableDecimal("TotaleVariabileProvvigione");
                r.TotaleSommaProvvigioni = reader.GetNullableDecimal("TotaleSommaProvvigioni");
                r.TotaleProvvigioneDaFatturare = reader.GetNullableDecimal("TotaleProvvigioneDaFatturare");

                // Date e Stato
                r.DataConsegna = reader.GetNullableDateTime("DataConsegna");
                r.Fatturare = reader.GetNullableBool("Fatturare"); //19/04/2026//r.Fatturare = reader.GetStringSafe("Fatturare");
                r.UniqueKey = reader.GetStringSafe("UniqueKey");
                r.IsProcessed = reader.GetBoolSafe("IsProcessed");

                // Log tecnici
                r.DataInserimento = reader.GetDateTimeSafe("DataInserimento");
                r.DataModifica = reader.GetNullableDateTime("DataModifica");
                r.Utente = reader.GetStringSafe("Utente");

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
            DELETE FROM ImportElektraWire 
            WHERE IdFileImportato = @IdFileImportato 
            AND IsProcessed = 0";

            using var cmd = new SqlCommand(sql, conn, tran);
            cmd.Parameters.Add("@IdFileImportato", SqlDbType.Int).Value = idFileImportato;
            cmd.ExecuteNonQuery();
        }

        public bool ExistsUniqueKey(string uniqueKey, int idMandatario)
        {
            const string sql = @"
            SELECT COUNT(1) FROM ImportElektraWire
            WHERE UniqueKey = @UniqueKey AND IdMandatario = @IdMandatario AND IsProcessed = 1;";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@UniqueKey", SqlDbType.NVarChar).Value = uniqueKey;
            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = idMandatario;

            conn.Open();
            return (int)cmd.ExecuteScalar() > 0;
        }

        public void SetProcessedStatus(int idImportElektraWire, bool isProcessed, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
            UPDATE ImportElektraWire SET IsProcessed = @IsProcessed, DataModifica = GETDATE(), Utente = @Utente 
            WHERE IdImportElektraWire = @IdImportElektraWire";

            using var cmd = new SqlCommand(sql, conn, tran);
            cmd.Parameters.Add("@IdImportElektraWire", SqlDbType.Int).Value = idImportElektraWire;
            cmd.Parameters.Add("@IsProcessed", SqlDbType.Bit).Value = isProcessed;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = utente;
            cmd.ExecuteNonQuery();
        }
    }
}