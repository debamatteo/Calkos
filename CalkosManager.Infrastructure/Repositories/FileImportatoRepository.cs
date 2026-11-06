using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/*
    ============================================================
    FILE IMPORTATO REPOSITORY — PROMEMORIA ARCHITETTURALE
    ============================================================

    Questo repository gestisce la tabella FileImportato, utilizzata
    nei processi di importazione COBRAL.

    ✔ Logica ibrida:
      - INSERT principale tramite SQL inline (fuori transazione)
      - INSERT transazionale tramite stored procedure
        (spFileImportato_Insert)

    ✔ Motivazioni architetturali:
      - La tabella FileImportato registra i file elaborati e deve
        essere scritta sia in contesti semplici sia in contesti
        transazionali complessi.
      - L’INSERT inline è più semplice e immediata per operazioni
        non transazionali.
      - L’INSERT via stored è necessaria quando l’operazione fa parte
        di un flusso COBRAL che richiede atomicità.

    ✔ Standard tecnici:
      - NIENTE AddWithValue: parametri sempre tipizzati
      - SqlReaderExtensions per lettura sicura (GetIntSafe, GetStringSafe, ecc.)
      - Gestione errori con eccezioni arricchite
      - Nessuna modifica alla logica di business

    Questo repository è considerato “speciale”: non va modificato
    nella logica, ma solo corretto in caso di errori sintattici
    (parametri, mapping, AddWithValue, ecc.).
    ============================================================
*/

// Aggiungo il riferimento alla  classe helper per far funzionare i metodi "Safe"
using CalkosManager.Infrastructure.Helpers;

namespace CalkosManager.Infrastructure.Repositories
{
    public class FileImportatoRepository : IFileImportatoRepository
    {
        private readonly string _connectionString;

        public FileImportatoRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>Inserisce un file importato e ritorna l'ID generato.</summary>
        public int Insert(FileImportato file)
        {
            /* SELECT CAST(SCOPE_IDENTITY() AS INT)  permette di recuperare  l'ID appena generato per il record appena inserito.*/

            const string sql = @"
                INSERT INTO FileImportato (IdMandatario, NomeFile, DataImportazione)
                VALUES (@IdMandatario, @NomeFile, @DataImportazione);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(sql, conn);

                // Parametri tipizzati (più veloci e sicuri)
                cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = file.IdMandatario;
                cmd.Parameters.Add("@NomeFile", SqlDbType.NVarChar, 255).Value = file.NomeFile;
                cmd.Parameters.Add("@DataImportazione", SqlDbType.DateTime).Value = file.DataImportazione;

                conn.Open();

                var result = cmd.ExecuteScalar();
                return (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;
            }
            catch (SqlException ex)
            {
                // Qui andrebbe un logger (Serilog, NLog, ecc.)
                // Log.Error(ex, "Errore SQL durante Insert FileImportato");
                throw new Exception("Errore di database durante il salvataggio del file.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Errore imprevisto nell'operazione di Insert.", ex);
            }
        }


        /// <summary>Ritorna un file importato tramite ID.</summary>
        public FileImportato? GetById(int id)
        {
            const string sql = @"
        SELECT IdFileImportato, IdMandatario, NomeFile, DataImportazione
        FROM FileImportato
        WHERE IdFileImportato = @id";

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;

                conn.Open();
                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                    return null;

                return new FileImportato
                {
                    // Applicazione funzioni helper per mappatura sicura
                    IdFileImportato = reader.GetIntSafe("IdFileImportato"),
                    IdMandatario = reader.GetIntSafe("IdMandatario"),
                    NomeFile = reader.GetStringSafe("NomeFile"),
                    DataImportazione = reader.GetDateTimeSafe("DataImportazione")
                };
            }
            catch (SqlException ex)
            {
                throw new Exception("Errore SQL durante la lettura del file importato.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Errore imprevisto in GetById.", ex);
            }
        }
        
        
        
        /// <summary>
        /// Inserisce un file importato usando una connessione e una transazione già aperte.
        /// Usato nei processi transazionali (importazione completa).
        /// </summary>



        public int Insert(FileImportato file, SqlConnection conn, SqlTransaction tran)
        {
            using var cmd = new SqlCommand("spFileImportato_Insert", conn, tran)
            {
                CommandType = CommandType.StoredProcedure
            };


            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = file.IdMandatario;
            cmd.Parameters.Add("@NomeFile", SqlDbType.NVarChar, 255).Value = file.NomeFile;
            cmd.Parameters.Add("@DataImportazione", SqlDbType.DateTime).Value = file.DataImportazione;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = file.Utente;


            var result = cmd.ExecuteScalar();
            return Convert.ToInt32(result);
        }



        /// <summary>Ritorna tutti i file importati per un mandatario.</summary>
        public IEnumerable<FileImportato> GetByMandatario(int idMandatario)
        {
            const string sql = @"
        SELECT IdFileImportato, IdMandatario, NomeFile, DataImportazione
        FROM FileImportato
        WHERE IdMandatario = @idMandatario
        ORDER BY DataImportazione DESC";

            var lista = new List<FileImportato>();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.Add("@idMandatario", SqlDbType.Int).Value = idMandatario;

                conn.Open();
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    lista.Add(new FileImportato
                    {
                        // Sostituzione indici numerici con funzioni helper basate su nome colonna
                        IdFileImportato = reader.GetIntSafe("IdFileImportato"),
                        IdMandatario = reader.GetIntSafe("IdMandatario"),
                        NomeFile = reader.GetStringSafe("NomeFile"),
                        DataImportazione = reader.GetDateTimeSafe("DataImportazione")
                    });
                }

                return lista;
            }
            catch (SqlException ex)
            {
                throw new Exception("Errore SQL durante la lettura dei file per mandatario.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Errore imprevisto in GetByMandatario.", ex);
            }
        }
        

        // 12/04/2026 Aggiunto per supportare la ri-importazione nello Scenario C
        public void Update(FileImportato file, SqlConnection conn, SqlTransaction tran)
        {
            const string sql = @"
        UPDATE FileImportato 
        SET NomeFile = @NomeFile, 
            DataImportazione = @DataImportazione, 
            Utente = @Utente,
            Anno = @Anno,
            Mese = @Mese,
            IdMandatario = @IdMandatario
        WHERE IdFileImportato = @IdFileImportato";

            using var cmd = new SqlCommand(sql, conn, tran);

            cmd.Parameters.Add("@IdFileImportato", SqlDbType.Int).Value = file.IdFileImportato;
            cmd.Parameters.Add("@NomeFile", SqlDbType.NVarChar).Value = file.NomeFile;
            cmd.Parameters.Add("@DataImportazione", SqlDbType.DateTime).Value = file.DataImportazione;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = file.Utente;
            cmd.Parameters.Add("@Anno", SqlDbType.Int).Value = file.Anno;
            cmd.Parameters.Add("@Mese", SqlDbType.Int).Value = file.Mese;
            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = file.IdMandatario;

            cmd.ExecuteNonQuery();
        }

    }
}