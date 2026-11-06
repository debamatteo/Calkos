using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Infrastructure.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;
/*
    ============================================================
    IMPORT DeAngeli REPOSITORY — PROMEMORIA ARCHITETTURALE
    ============================================================

    Questo repository gestisce la tabella ImportDeAngeli, che contiene
    tutte le righe importate dai file DeAngeli prima della validazione
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
      - Nessuna modifica alla logica di business o al flusso DeAngeli.

    Questo repository è considerato “sensibile”: ogni modifica deve
    essere minima, motivata e verificata con attenzione, per evitare
    impatti sul processo di importazione DeAngeli.
    ============================================================
*/
namespace CalkosManager.Infrastructure.Repositories
{
    public class ImportDeAngeliRepository : IImportDeAngeliRepository
    {
        private readonly string _connectionString;

        public ImportDeAngeliRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        //16/04/2026
        public ImportDeAngeli Insert(ImportDeAngeli item, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        INSERT INTO ImportDeAngeli (
            IdFileImportato, IdMandatario, IdCalcoloBase, OrdineDDT, Cliente, Kg, QuantitaOrdine, CostoLavorazione,
            Materiale, TipologiaMateriale, Prezzo, PrezzoConLavorazione, PrezzoVendita, 
            PercentualeProvvigioneVariabile, ValoreProvvigioneVariabile, ValoreProvvigioneFissa1, 
            ValoreProvvigioneFissa2, ValoreProvvigioneFissa3, TotaleFissoProvvigione, 
            FlagIndicatoreMaggiorazione,Perc_Delta_Provvigione,
            Differenza, TotaleVariabileProvvigione, TotaleProvvigioneDaFatturare, 
            DataConsegnaIpotetica, Fatturare, Scadenza, UniqueKey, IsProcessed, 
            DataInserimento, DataModifica, Utente
        )
        VALUES (
            @IdFileImportato, @IdMandatario, @IdCalcoloBase, @OrdineDDT, @Cliente, @Kg, @QuantitaOrdine, @CostoLavorazione,
            @Materiale, @TipologiaMateriale, @Prezzo, @PrezzoConLavorazione, @PrezzoVendita, 
            @PercentualeProvvigioneVariabile, @ValoreProvvigioneVariabile, @ValoreProvvigioneFissa1, 
            @ValoreProvvigioneFissa2, @ValoreProvvigioneFissa3, @TotaleFissoProvvigione, 
            @FlagIndicatoreMaggiorazione, @Perc_Delta_Provvigione,
            @Differenza, @TotaleVariabileProvvigione, @TotaleProvvigioneDaFatturare, 
            @DataConsegnaIpotetica, @Fatturare, @Scadenza, @UniqueKey, @IsProcessed, 
            @DataInserimento, @DataModifica, @Utente
        );
        SELECT SCOPE_IDENTITY();";

            using var cmd = new SqlCommand(sql, conn, tran);
            PrepareParameters(cmd);
            SetParametersValues(cmd, item, utente);

            item.IdImportDeAngeli = Convert.ToInt32(cmd.ExecuteScalar());
            return item;
        }
        //16/04/2026
        public void InsertRange(IEnumerable<ImportDeAngeli> items, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        INSERT INTO ImportDeAngeli (
            IdFileImportato, IdMandatario, IdCalcoloBase, OrdineDDT, Cliente, Kg, QuantitaOrdine, CostoLavorazione, 
            Materiale, TipologiaMateriale, Prezzo, PrezzoConLavorazione, PrezzoVendita, 
            PercentualeProvvigioneVariabile, ValoreProvvigioneVariabile, ValoreProvvigioneFissa1, 
            ValoreProvvigioneFissa2, ValoreProvvigioneFissa3, TotaleFissoProvvigione, 
            FlagIndicatoreMaggiorazione, Perc_Delta_Provvigione,
            Differenza, TotaleVariabileProvvigione, TotaleProvvigioneDaFatturare, 
            DataConsegnaIpotetica, Fatturare, Scadenza, UniqueKey, IsProcessed, 
            DataInserimento, DataModifica, Utente
        )
        VALUES (
            @IdFileImportato, @IdMandatario, @IdCalcoloBase, @OrdineDDT, @Cliente, @Kg, @QuantitaOrdine, @CostoLavorazione, 
            @Materiale, @TipologiaMateriale, @Prezzo, @PrezzoConLavorazione, @PrezzoVendita, 
            @PercentualeProvvigioneVariabile, @ValoreProvvigioneVariabile, @ValoreProvvigioneFissa1, 
            @ValoreProvvigioneFissa2, @ValoreProvvigioneFissa3, @TotaleFissoProvvigione, 
            @FlagIndicatoreMaggiorazione, @Perc_Delta_Provvigione, 
            @Differenza, @TotaleVariabileProvvigione, @TotaleProvvigioneDaFatturare, 
            @DataConsegnaIpotetica, @Fatturare, @Scadenza, @UniqueKey, @IsProcessed, 
            @DataInserimento, @DataModifica, @Utente
        );";

            using var cmd = new SqlCommand(sql, conn, tran);
            PrepareParameters(cmd);

            foreach (var item in items)
            {
                SetParametersValues(cmd, item, utente);
                cmd.ExecuteNonQuery();
            }
        }
        //16/04/2026
        private void PrepareParameters(SqlCommand cmd)
        {
            cmd.Parameters.Add("@IdFileImportato", SqlDbType.Int);
            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int);
            cmd.Parameters.Add("@IdCalcoloBase", SqlDbType.Int);
            cmd.Parameters.Add("@OrdineDDT", SqlDbType.NVarChar);
            cmd.Parameters.Add("@Cliente", SqlDbType.NVarChar);
            cmd.Parameters.Add("@Materiale", SqlDbType.NVarChar);
            cmd.Parameters.Add("@TipologiaMateriale", SqlDbType.NVarChar);
            cmd.Parameters.Add("@DataConsegnaIpotetica", SqlDbType.Date);
            cmd.Parameters.Add("@Scadenza", SqlDbType.Date);
            cmd.Parameters.Add("@UniqueKey", SqlDbType.NVarChar);
            cmd.Parameters.Add("@IsProcessed", SqlDbType.Bit);
            cmd.Parameters.Add("@Fatturare", SqlDbType.Bit);

            cmd.Parameters.Add("@DataInserimento", SqlDbType.DateTime);
            cmd.Parameters.Add("@DataModifica", SqlDbType.DateTime);
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar);

            cmd.Parameters.Add("@FlagIndicatoreMaggiorazione", SqlDbType.Bit);
            AddDecimalParam(cmd, "@Perc_Delta_Provvigione", 6);//24/04/2026
            // --- Decimali Allineati alla tua logica ---
            AddDecimalParam(cmd, "@Kg", 6);
            AddDecimalParam(cmd, "@QuantitaOrdine", 6);
            AddDecimalParam(cmd, "@CostoLavorazione", 6);
            AddDecimalParam(cmd, "@Prezzo", 6);
            AddDecimalParam(cmd, "@PrezzoConLavorazione", 6);
            AddDecimalParam(cmd, "@PrezzoVendita", 6);
            AddDecimalParam(cmd, "@PercentualeProvvigioneVariabile", 6);
            AddDecimalParam(cmd, "@ValoreProvvigioneVariabile", 6);
            AddDecimalParam(cmd, "@ValoreProvvigioneFissa1", 6);
            AddDecimalParam(cmd, "@ValoreProvvigioneFissa2", 6);
            AddDecimalParam(cmd, "@ValoreProvvigioneFissa3", 6);
            AddDecimalParam(cmd, "@TotaleFissoProvvigione", 6);
            AddDecimalParam(cmd, "@Differenza", 6);
            AddDecimalParam(cmd, "@TotaleVariabileProvvigione", 6);
            AddDecimalParam(cmd, "@TotaleProvvigioneDaFatturare", 6);
        }
        //16/04/2026



        private void SetParametersValues(SqlCommand cmd, ImportDeAngeli item, string utente)
        {
            cmd.Parameters["@IdFileImportato"].Value = item.IdFileImportato;
            cmd.Parameters["@IdMandatario"].Value = item.IdMandatario;
            cmd.Parameters["@IdCalcoloBase"].Value = (object?)item.IdCalcoloBase ?? DBNull.Value;
            cmd.Parameters["@OrdineDDT"].Value = (object?)item.OrdineDDT ?? DBNull.Value;
            cmd.Parameters["@Cliente"].Value = (object?)item.Cliente ?? DBNull.Value;
            cmd.Parameters["@Kg"].Value = (object?)item.Kg ?? DBNull.Value;
            
            cmd.Parameters["@FlagIndicatoreMaggiorazione"].Value = item.FlagIndicatoreMaggiorazione == null ? DBNull.Value : (item.FlagIndicatoreMaggiorazione == true ? 1 : 0);
            cmd.Parameters["@Perc_Delta_Provvigione"].Value = (object?)item.Perc_Delta_Provvigione ?? DBNull.Value;//01/05/2026	

            cmd.Parameters["@QuantitaOrdine"].Value = (object?)item.QuantitaOrdine ?? DBNull.Value;
            cmd.Parameters["@CostoLavorazione"].Value = (object?)item.CostoLavorazione ?? DBNull.Value;
            cmd.Parameters["@Materiale"].Value = (object?)item.Materiale ?? DBNull.Value;
            cmd.Parameters["@TipologiaMateriale"].Value = (object?)item.TipologiaMateriale ?? DBNull.Value;
            cmd.Parameters["@Prezzo"].Value = (object?)item.Prezzo ?? DBNull.Value;
            cmd.Parameters["@PrezzoConLavorazione"].Value = (object?)item.PrezzoConLavorazione ?? DBNull.Value;
            cmd.Parameters["@PrezzoVendita"].Value = (object?)item.PrezzoVendita ?? DBNull.Value;
            cmd.Parameters["@PercentualeProvvigioneVariabile"].Value = (object?)item.PercentualeProvvigioneVariabile ?? DBNull.Value;
            cmd.Parameters["@ValoreProvvigioneVariabile"].Value = (object?)item.ValoreProvvigioneVariabile ?? DBNull.Value;
            cmd.Parameters["@ValoreProvvigioneFissa1"].Value = (object?)item.ValoreProvvigioneFissa1 ?? DBNull.Value;
            cmd.Parameters["@ValoreProvvigioneFissa2"].Value = (object?)item.ValoreProvvigioneFissa2 ?? DBNull.Value;
            cmd.Parameters["@ValoreProvvigioneFissa3"].Value = (object?)item.ValoreProvvigioneFissa3 ?? DBNull.Value;
            cmd.Parameters["@TotaleFissoProvvigione"].Value = (object?)item.TotaleFissoProvvigione ?? DBNull.Value;

            cmd.Parameters["@Differenza"].Value = (object?)item.Differenza ?? DBNull.Value;
            cmd.Parameters["@TotaleVariabileProvvigione"].Value = (object?)item.TotaleVariabileProvvigione ?? DBNull.Value;
            cmd.Parameters["@TotaleProvvigioneDaFatturare"].Value = (object?)item.TotaleProvvigioneDaFatturare ?? DBNull.Value;

            // Protezione Date
            cmd.Parameters["@DataConsegnaIpotetica"].Value = (item.DataConsegnaIpotetica == null || item.DataConsegnaIpotetica == DateTime.MinValue) ? DBNull.Value : item.DataConsegnaIpotetica;
            cmd.Parameters["@Scadenza"].Value = (item.Scadenza == null || item.Scadenza == DateTime.MinValue) ? DBNull.Value : item.Scadenza;

            cmd.Parameters["@Fatturare"].Value = item.Fatturare == null ? DBNull.Value : (item.Fatturare == true ? 1 : 0);
            cmd.Parameters["@UniqueKey"].Value = (object?)item.UniqueKey ?? DBNull.Value;
            cmd.Parameters["@IsProcessed"].Value = item.IsProcessed;

            cmd.Parameters["@DataInserimento"].Value = item.DataInserimento == DateTime.MinValue ? DateTime.Now : item.DataInserimento;
            cmd.Parameters["@DataModifica"].Value = (item.DataModifica == null || item.DataModifica == DateTime.MinValue) ? DBNull.Value : item.DataModifica;

            cmd.Parameters["@Utente"].Value = utente;
        }
        //16/04/2026
        private void AddDecimalParam(SqlCommand cmd, string name, byte scale)
        {
            var param = cmd.Parameters.Add(name, SqlDbType.Decimal);
            param.Precision = 18;
            param.Scale = scale;
        }

        public IEnumerable<ImportDeAngeli> GetByFile(int idFileImportato)
        {
            const string sql = "SELECT * FROM ImportDeAngeli WHERE IdFileImportato = @idFileImportato";
            var lista = new List<ImportDeAngeli>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@idFileImportato", SqlDbType.Int).Value = idFileImportato;

            conn.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var r = new ImportDeAngeli();
                r.IdImportDeAngeli = reader.GetIntSafe("IdImportDeAngeli");
                r.IdFileImportato = reader.GetIntSafe("IdFileImportato");
                r.IdMandatario = reader.GetIntSafe("IdMandatario");
                r.IdCalcoloBase = reader.GetNullableInt("IdCalcoloBase");
                r.OrdineDDT = reader.GetStringSafe("OrdineDDT");
                r.Cliente = reader.GetStringSafe("Cliente");
                r.Kg = reader.GetNullableDecimal("Kg");
                r.QuantitaOrdine = reader.GetNullableDecimal("QuantitaOrdine");
                r.CostoLavorazione = reader.GetNullableDecimal("CostoLavorazione");
                r.Materiale = reader.GetStringSafe("Materiale");
                r.TipologiaMateriale = reader.GetStringSafe("TipologiaMateriale");
                r.Prezzo = reader.GetNullableDecimal("Prezzo");
                r.PrezzoConLavorazione = reader.GetNullableDecimal("PrezzoConLavorazione");
                r.PrezzoVendita = reader.GetNullableDecimal("PrezzoVendita");
                r.PercentualeProvvigioneVariabile = reader.GetNullableDecimal("PercentualeProvvigioneVariabile");
                r.ValoreProvvigioneVariabile = reader.GetNullableDecimal("ValoreProvvigioneVariabile");
                r.ValoreProvvigioneFissa1 = reader.GetNullableDecimal("ValoreProvvigioneFissa1");
                r.ValoreProvvigioneFissa2 = reader.GetNullableDecimal("ValoreProvvigioneFissa2");
                r.ValoreProvvigioneFissa3 = reader.GetNullableDecimal("ValoreProvvigioneFissa3");
                r.TotaleFissoProvvigione = reader.GetNullableDecimal("TotaleFissoProvvigione");
                r.FlagIndicatoreMaggiorazione = reader.GetNullableBool("FlagIndicatoreMaggiorazione");
                r.Perc_Delta_Provvigione = reader.GetNullableDecimal("Perc_Delta_Provvigione");//24/04/2026
                r.Differenza = reader.GetNullableDecimal("Differenza");
                r.TotaleVariabileProvvigione = reader.GetNullableDecimal("TotaleVariabileProvvigione");
                r.TotaleProvvigioneDaFatturare = reader.GetNullableDecimal("TotaleProvvigioneDaFatturare");
                r.DataConsegnaIpotetica = reader.GetNullableDateTime("DataConsegnaIpotetica");
                r.Fatturare = reader.GetNullableBool("Fatturare");
                r.Scadenza = reader.GetNullableDateTime("Scadenza");
                r.UniqueKey = reader.GetStringSafe("UniqueKey");
                //r.IsProcessed = reader.GetBoolean(reader.GetOrdinal("IsProcessed"));
                r.DataInserimento = reader.GetDateTime(reader.GetOrdinal("DataInserimento"));
                r.DataModifica = reader.GetNullableDateTime("DataModifica");
                r.Utente = reader.GetStringSafe("Utente");
                r.IsProcessed = reader.GetBoolSafe("IsProcessed");//12/04/2026
                lista.Add(r);
            }
            return lista;
        }

        public void DeleteByFileImportatoId(int idFileImportato, SqlConnection conn, SqlTransaction tran, string utente)
        {
            // Modifica Logica12/04/2026
            // Cancelliamo SOLO le righe che non sono ancora state elaborate.
            // Se una riga è IsProcessed = 1, non deve essere toccata da questa operazione di pulizia.
            const string sql = @"
            DELETE FROM ImportDeAngeli 
            WHERE IdFileImportato = @IdFileImportato 
            AND IsProcessed = 0";

            using var cmd = new SqlCommand(sql, conn, tran);
            cmd.Parameters.Add("@IdFileImportato", SqlDbType.Int).Value = idFileImportato;

            // Eseguiamo la cancellazione
            cmd.ExecuteNonQuery();
        }

        //ImportDeAngeli
        /// <summary>
        /// Verifica se esiste già una chiave univoca per un determinato mandatario nel database.
        /// Utilizzato per evitare duplicati durante l'importazione dei file (es. Prospetti).
        /// </summary>
        /// <param name="uniqueKey">La stringa univoca da cercare (es. combinazione di N. Ordine e DDT).</param>
        /// <param name="idMandatario">L'ID dell'agente o mandatario di riferimento.</param>
        /// <returns>True se la chiave esiste già, altrimenti False.</returns>
        public bool ExistsUniqueKey(string uniqueKey, int idMandatario)
        {
            // Definizione della query SQL: usiamo COUNT(1) per ottimizzare le prestazioni
            const string sql = @"
            SELECT COUNT(1)
            FROM ImportDeAngeli
            WHERE UniqueKey = @UniqueKey
            AND IdMandatario = @IdMandatario 
            AND IsProcessed = 1;
            "; //12/04/2026 aggiunto IsProcessed = 1

            // 'using var' garantisce la chiusura e lo smaltimento (Dispose) della connessione 
            // e del comando anche in caso di eccezioni, evitando leak di memoria.
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);

            // Definizione dei parametri per prevenire SQL Injection e gestire correttamente i tipi di dato
            cmd.Parameters.Add("@UniqueKey", SqlDbType.NVarChar).Value = uniqueKey;
            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = idMandatario;

            // Apertura della connessione al database
            conn.Open();

            // ExecuteScalar restituisce la prima colonna della prima riga del set di risultati.
            // Facciamo il cast a (int) perché COUNT restituisce un intero.
            var count = (int)cmd.ExecuteScalar();

            // Se il conteggio è maggiore di 0, la chiave esiste già nel sistema
            return count > 0;
        }


        /// <summary>
        /// Aggiorna lo stato isProcessed di elaborazione di una riga specifica. 12/04/2026
        /// </summary>
        public void SetProcessedStatus(int idImportDeAngeli, bool isProcessed, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        UPDATE ImportDeAngeli 
        SET IsProcessed = @IsProcessed, 
            DataModifica = GETDATE(), 
            Utente = @Utente 
        WHERE idImportDeAngeli = @idImportDeAngeli";

            using var cmd = new SqlCommand(sql, conn, tran);

            cmd.Parameters.Add("@idImportDeAngeli", SqlDbType.Int).Value = idImportDeAngeli;
            cmd.Parameters.Add("@IsProcessed", SqlDbType.Bit).Value = isProcessed;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = utente;

            cmd.ExecuteNonQuery();
        }


    }
}