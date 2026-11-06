using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Infrastructure.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;
/*
    ============================================================
    IMPORT COBRAL REPOSITORY — PROMEMORIA ARCHITETTURALE
    ============================================================

    Questo repository gestisce la tabella ImportCobral, che contiene
    tutte le righe importate dai file COBRAL prima della validazione
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
      - Nessuna modifica alla logica di business o al flusso COBRAL.

    Questo repository è considerato “sensibile”: ogni modifica deve
    essere minima, motivata e verificata con attenzione, per evitare
    impatti sul processo di importazione COBRAL.
    ============================================================
*/


namespace CalkosManager.Infrastructure.Repositories
{
    public class ImportCobralRepository : IImportCobralRepository
    {
        private readonly string _connectionString;

        public ImportCobralRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Metodo fittizio da rimuovere dopo la prova
        public static string GetBuildTimestamp()
        {
            return $"Build generata il: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }


        // ---------------------------------------------------------
        // INSERT UNA RIGA (TRANSazionale) 16/04/2026
        // ---------------------------------------------------------
        public ImportCobral Insert(ImportCobral item, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        INSERT INTO ImportCobral (
            IdFileImportato, IdMandatario, OrdineDDT, Cliente, Kg, Materiale, Prezzo, Al, Spessore, Larghezza,
            Provvigione, AlluminioSpessore, OttoneSpessore, RameSpessore, AltrePercentuali, PrLavSpess,
            AlluminioLarghezza, OttoneLarghezza, RameLarghezza, BronzoLarghezza, PrLavLarg, 
            ExtraPrezzoKg, ExtraPrezzoStagnato, PrLavTotale, Commissioni, PrezzoVendita, Differenza,
            DataConsegnaIpotetica, Agente, Scadenza, Fatturare, UniqueKey, IsProcessed,
            DataInserimento, DataModifica, Utente
        )
        VALUES (
            @IdFileImportato, @IdMandatario, @OrdineDDT, @Cliente, @Kg, @Materiale, @Prezzo, @Al, @Spessore, @Larghezza,
            @Provvigione, @AlluminioSpessore, @OttoneSpessore, @RameSpessore, @AltrePercentuali, @PrLavSpess,
            @AlluminioLarghezza, @OttoneLarghezza, @RameLarghezza, @BronzoLarghezza, @PrLavLarg, 
            @ExtraPrezzoKg, @ExtraPrezzoStagnato, @PrLavTotale, @Commissioni, @PrezzoVendita, @Differenza,
            @DataConsegnaIpotetica, @Agente, @Scadenza, @Fatturare, @UniqueKey, @IsProcessed,
            @DataInserimento, @DataModifica, @Utente
        );
        SELECT SCOPE_IDENTITY();";

            using var cmd = new SqlCommand(sql, conn, tran);

            PrepareParameters(cmd);
            SetParametersValues(cmd, item, utente);

            item.IdImportCobral = Convert.ToInt32(cmd.ExecuteScalar());
            return item;
        }
        // ---------------------------------------------------------
        // INSERT RANGE (TRANSazionale) 16/04/2026
        // ---------------------------------------------------------
        public void InsertRange(IEnumerable<ImportCobral> items, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        INSERT INTO ImportCobral (
            IdFileImportato, IdMandatario, OrdineDDT, Cliente, Kg, Materiale, Prezzo, Al, Spessore, Larghezza,
            Provvigione, AlluminioSpessore, OttoneSpessore, RameSpessore, AltrePercentuali, PrLavSpess,
            AlluminioLarghezza, OttoneLarghezza, RameLarghezza, BronzoLarghezza, PrLavLarg, 
            ExtraPrezzoKg, ExtraPrezzoStagnato, PrLavTotale, Commissioni, PrezzoVendita, Differenza,
            DataConsegnaIpotetica, Agente, Scadenza, Fatturare, UniqueKey, IsProcessed,
            DataInserimento, DataModifica, Utente
        )
        VALUES (
            @IdFileImportato, @IdMandatario, @OrdineDDT, @Cliente, @Kg, @Materiale, @Prezzo, @Al, @Spessore, @Larghezza,
            @Provvigione, @AlluminioSpessore, @OttoneSpessore, @RameSpessore, @AltrePercentuali, @PrLavSpess,
            @AlluminioLarghezza, @OttoneLarghezza, @RameLarghezza, @BronzoLarghezza, @PrLavLarg, 
            @ExtraPrezzoKg, @ExtraPrezzoStagnato, @PrLavTotale, @Commissioni, @PrezzoVendita, @Differenza,
            @DataConsegnaIpotetica, @Agente, @Scadenza, @Fatturare, @UniqueKey, @IsProcessed,
            @DataInserimento, @DataModifica, @Utente
        );";

            using var cmd = new SqlCommand(sql, conn, tran);

            // 1. Prepara lo schema dei parametri una sola volta
            PrepareParameters(cmd);

            foreach (var item in items)
            {
                // 2. Assegna i valori per l'elemento corrente
                SetParametersValues(cmd, item, utente);

                // 3. Esegue l'insert
                cmd.ExecuteNonQuery();
            }
        }
        //16/04/2026
        private void PrepareParameters(SqlCommand cmd)
        {
            cmd.Parameters.Add("@IdFileImportato", SqlDbType.Int);
            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int);
            cmd.Parameters.Add("@OrdineDDT", SqlDbType.NVarChar);
            cmd.Parameters.Add("@Cliente", SqlDbType.NVarChar);

            AddDecimalParam(cmd, "@Kg", 3);
            cmd.Parameters.Add("@Materiale", SqlDbType.NVarChar);
            AddDecimalParam(cmd, "@Prezzo", 3);

            cmd.Parameters.Add("@Al", SqlDbType.Date);
            cmd.Parameters.Add("@Spessore", SqlDbType.NVarChar);
            cmd.Parameters.Add("@Larghezza", SqlDbType.NVarChar);

            AddDecimalParam(cmd, "@Provvigione", 3);
            AddDecimalParam(cmd, "@AlluminioSpessore", 3);
            AddDecimalParam(cmd, "@OttoneSpessore", 3);
            AddDecimalParam(cmd, "@RameSpessore", 3);
            AddDecimalParam(cmd, "@AltrePercentuali", 3);
            AddDecimalParam(cmd, "@PrLavSpess", 3);

            AddDecimalParam(cmd, "@AlluminioLarghezza", 3);
            AddDecimalParam(cmd, "@OttoneLarghezza", 3);
            AddDecimalParam(cmd, "@RameLarghezza", 3);
            AddDecimalParam(cmd, "@BronzoLarghezza", 3);
            AddDecimalParam(cmd, "@PrLavLarg", 3);

            AddDecimalParam(cmd, "@ExtraPrezzoKg", 3);
            AddDecimalParam(cmd, "@ExtraPrezzoStagnato", 3);

            AddDecimalParam(cmd, "@PrLavTotale", 3);
            AddDecimalParam(cmd, "@Commissioni", 3);
            AddDecimalParam(cmd, "@PrezzoVendita", 3);
            AddDecimalParam(cmd, "@Differenza", 3);

            cmd.Parameters.Add("@DataConsegnaIpotetica", SqlDbType.Date);
            cmd.Parameters.Add("@Agente", SqlDbType.NVarChar);
            cmd.Parameters.Add("@Scadenza", SqlDbType.Date);

            cmd.Parameters.Add("@Fatturare", SqlDbType.Bit);
            cmd.Parameters.Add("@UniqueKey", SqlDbType.NVarChar);
            cmd.Parameters.Add("@IsProcessed", SqlDbType.Bit);

            cmd.Parameters.Add("@DataInserimento", SqlDbType.DateTime);
            cmd.Parameters.Add("@DataModifica", SqlDbType.DateTime);
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar);
        }
        //16/04/2026
        private void SetParametersValues(SqlCommand cmd, ImportCobral item, string utente)
        {
            cmd.Parameters["@IdFileImportato"].Value = item.IdFileImportato;
            cmd.Parameters["@IdMandatario"].Value = item.IdMandatario;
            cmd.Parameters["@OrdineDDT"].Value = (object?)item.OrdineDDT ?? DBNull.Value;
            cmd.Parameters["@Cliente"].Value = (object?)item.Cliente ?? DBNull.Value;
            cmd.Parameters["@Kg"].Value = (object?)item.Kg ?? DBNull.Value;
            cmd.Parameters["@Materiale"].Value = (object?)item.Materiale ?? DBNull.Value;
            cmd.Parameters["@Prezzo"].Value = (object?)item.Prezzo ?? DBNull.Value;

            cmd.Parameters["@Al"].Value = (item.Al == null || item.Al == DateTime.MinValue) ? DBNull.Value : item.Al;

            cmd.Parameters["@Spessore"].Value = (object?)item.Spessore ?? DBNull.Value;
            cmd.Parameters["@Larghezza"].Value = (object?)item.Larghezza ?? DBNull.Value;

            cmd.Parameters["@Provvigione"].Value = (object?)item.Provvigione ?? DBNull.Value;
            cmd.Parameters["@AlluminioSpessore"].Value = (object?)item.AlluminioSpessore ?? DBNull.Value;
            cmd.Parameters["@OttoneSpessore"].Value = (object?)item.OttoneSpessore ?? DBNull.Value;
            cmd.Parameters["@RameSpessore"].Value = (object?)item.RameSpessore ?? DBNull.Value;
            cmd.Parameters["@AltrePercentuali"].Value = (object?)item.AltrePercentuali ?? DBNull.Value;
            cmd.Parameters["@PrLavSpess"].Value = (object?)item.PrLavSpess ?? DBNull.Value;

            cmd.Parameters["@AlluminioLarghezza"].Value = (object?)item.AlluminioLarghezza ?? DBNull.Value;
            cmd.Parameters["@OttoneLarghezza"].Value = (object?)item.OttoneLarghezza ?? DBNull.Value;
            cmd.Parameters["@RameLarghezza"].Value = (object?)item.RameLarghezza ?? DBNull.Value;
            cmd.Parameters["@BronzoLarghezza"].Value = (object?)item.BronzoLarghezza ?? DBNull.Value;
            cmd.Parameters["@PrLavLarg"].Value = (object?)item.PrLavLarg ?? DBNull.Value;

            cmd.Parameters["@ExtraPrezzoKg"].Value = (object?)item.ExtraPrezzoKg ?? DBNull.Value;
            cmd.Parameters["@ExtraPrezzoStagnato"].Value = (object?)item.ExtraPrezzoStagnato ?? DBNull.Value;

            cmd.Parameters["@PrLavTotale"].Value = (object?)item.PrLavTotale ?? DBNull.Value;
            cmd.Parameters["@Commissioni"].Value = (object?)item.Commissioni ?? DBNull.Value;
            cmd.Parameters["@PrezzoVendita"].Value = (object?)item.PrezzoVendita ?? DBNull.Value;
            cmd.Parameters["@Differenza"].Value = (object?)item.Differenza ?? DBNull.Value;

            cmd.Parameters["@DataConsegnaIpotetica"].Value = (item.DataConsegnaIpotetica == null || item.DataConsegnaIpotetica == DateTime.MinValue) ? DBNull.Value : item.DataConsegnaIpotetica;
            cmd.Parameters["@Agente"].Value = (object?)item.Agente ?? DBNull.Value;
            cmd.Parameters["@Scadenza"].Value = (item.Scadenza == null || item.Scadenza == DateTime.MinValue) ? DBNull.Value : item.Scadenza;

            cmd.Parameters["@Fatturare"].Value = item.Fatturare == null ? DBNull.Value : (item.Fatturare == true ? 1 : 0);
            cmd.Parameters["@UniqueKey"].Value = (object?)item.UniqueKey ?? DBNull.Value;
            cmd.Parameters["@IsProcessed"].Value = item.IsProcessed;

            cmd.Parameters["@DataInserimento"].Value = item.DataInserimento == DateTime.MinValue ? DateTime.Now : item.DataInserimento;
            cmd.Parameters["@DataModifica"].Value = (item.DataModifica == null || item.DataModifica == DateTime.MinValue) ? DBNull.Value : item.DataModifica;
            cmd.Parameters["@Utente"].Value = (object?)utente ?? DBNull.Value;
        }
        //16/04/2026
        private void AddDecimalParam(SqlCommand cmd, string name, byte scale)
        {
            var p = cmd.Parameters.Add(name, SqlDbType.Decimal);
            p.Precision = 18;
            p.Scale = scale;
        }





        // ---------------------------------------------------------
        // GET BY FILE
        // ---------------------------------------------------------
        public IEnumerable<ImportCobral> GetByFile(int idFileImportato)
        {
            const string sql = "SELECT * FROM ImportCobral WHERE IdFileImportato = @idFileImportato";

            var lista = new List<ImportCobral>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@idFileImportato", SqlDbType.Int).Value = idFileImportato;

            conn.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var r = new ImportCobral();

                r.IdImportCobral = reader.GetIntSafe("IdImportCobral");
                r.IdMandatario = reader.GetIntSafe("IdMandatario");
                r.IdFileImportato = reader.GetIntSafe("IdFileImportato");

                r.OrdineDDT = reader.GetStringSafe("OrdineDDT");
                r.Cliente = reader.GetStringSafe("Cliente");
                r.Kg = reader.GetNullableDecimal("Kg");
                r.Materiale = reader.GetStringSafe("Materiale");
                r.Prezzo = reader.GetNullableDecimal("Prezzo");

                r.Al = reader.GetNullableDateTime("Al");
                //r.Spessore = reader.GetNullableDecimal("Spessore");
                //r.Larghezza = reader.GetNullableDecimal("Larghezza");

                r.Spessore = reader.GetStringSafe("Spessore");
                r.Larghezza = reader.GetStringSafe("Larghezza");

                r.Provvigione = reader.GetNullableDecimal("Provvigione");

                r.AlluminioSpessore = reader.GetNullableDecimal("AlluminioSpessore");
                r.OttoneSpessore = reader.GetNullableDecimal("OttoneSpessore");
                r.RameSpessore = reader.GetNullableDecimal("RameSpessore");
                r.AltrePercentuali = reader.GetNullableDecimal("AltrePercentuali");
                r.PrLavSpess = reader.GetNullableDecimal("PrLavSpess");

                r.AlluminioLarghezza = reader.GetNullableDecimal("AlluminioLarghezza");
                r.OttoneLarghezza = reader.GetNullableDecimal("OttoneLarghezza");
                r.RameLarghezza = reader.GetNullableDecimal("RameLarghezza");
                r.BronzoLarghezza = reader.GetNullableDecimal("BronzoLarghezza");
                r.PrLavLarg = reader.GetNullableDecimal("PrLavLarg");

                r.ExtraPrezzoKg = reader.GetNullableDecimal("ExtraPrezzoKg");
                r.ExtraPrezzoStagnato = reader.GetNullableDecimal("ExtraPrezzoStagnato");
                r.PrLavTotale = reader.GetNullableDecimal("PrLavTotale");
                r.Commissioni = reader.GetNullableDecimal("Commissioni");
                r.PrezzoVendita = reader.GetNullableDecimal("PrezzoVendita");
                r.Differenza = reader.GetNullableDecimal("Differenza");

                r.DataConsegnaIpotetica = reader.GetNullableDateTime("DataConsegnaIpotetica");
                r.Agente = reader.GetStringSafe("Agente");
                r.Scadenza = reader.GetNullableDateTime("Scadenza");

                r.Fatturare = reader.GetNullableBool("Fatturare");
                r.IsProcessed = reader.GetBoolSafe("IsProcessed");//12/04/2026
                lista.Add(r);
            }

            return lista;
        }



        // ---------------------------------------------------------
        // DELETE
        // ---------------------------------------------------------


        public void DeleteByFileImportatoId(int idFileImportato, SqlConnection conn, SqlTransaction tran, string utente)
        {
            // Modifica Logica12/04/2026
            // Cancelliamo SOLO le righe che non sono ancora state elaborate.
            // Se una riga è IsProcessed = 1, non deve essere toccata da questa operazione di pulizia.
                const string sql = @"
            DELETE FROM ImportCobral 
            WHERE IdFileImportato = @IdFileImportato 
            AND IsProcessed = 0";

            using var cmd = new SqlCommand(sql, conn, tran);
            cmd.Parameters.Add("@IdFileImportato", SqlDbType.Int).Value = idFileImportato;

            // Eseguiamo la cancellazione
            cmd.ExecuteNonQuery();
        }

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
            FROM ImportCobral
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
        public void SetProcessedStatus(int idImportCobral, bool isProcessed, SqlConnection conn, SqlTransaction tran, string utente)
        {
            const string sql = @"
        UPDATE ImportCobral 
        SET IsProcessed = @IsProcessed, 
            DataModifica = GETDATE(), 
            Utente = @Utente 
        WHERE IdImportCobral = @IdImportCobral";

            using var cmd = new SqlCommand(sql, conn, tran);

            cmd.Parameters.Add("@IdImportCobral", SqlDbType.Int).Value = idImportCobral;
            cmd.Parameters.Add("@IsProcessed", SqlDbType.Bit).Value = isProcessed;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = utente;

            cmd.ExecuteNonQuery();
        }



    }
}


