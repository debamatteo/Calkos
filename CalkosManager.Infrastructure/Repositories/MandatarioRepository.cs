/*
    ============================================================
    MANDATARIO REPOSITORY — PROMEMORIA ARCHITETTURALE
    ============================================================

    Questo repository è un CRUD semplice e NON è coinvolto
    nell’importazione COBRAL.

    ✔ Usa SQL INLINE perché:
      - non richiede transazioni
      - non è parte di flussi atomici
      - non ha lookup complessi
      - non ha logica duplicata da centralizzare in stored
      - è più veloce da mantenere e modificare

    ✔ NIENTE AddWithValue:
      - parametri sempre tipizzati con SqlDbType
      - evita conversioni implicite NVARCHAR(4000)
      - evita index scan e rallentamenti

    ✔ Lettura dei dati tramite SqlReaderExtensions:
      - GetStringSafe, GetIntSafe, ecc.
      - zero DBNull, zero eccezioni, codice più pulito

    ✔ Debug intelligente:
      - nei catch puoi usare SqlDebugHelper.DumpParameters(cmd)
        se necessario per tracciare i parametri SQL

    Questo repository è progettato per essere semplice, pulito
    e coerente con lo standard ADO.NET del progetto.
    ============================================================
*/

using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using Microsoft.Data.SqlClient;
using System.Data;
using CalkosManager.Infrastructure.Helpers;

namespace CalkosManager.Infrastructure.Repositories
{
    public class MandatarioRepository : IMandatarioRepository
    {
        private readonly string _connectionString;

        public MandatarioRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<Mandatario> GetAll()
        {
            var lista = new List<Mandatario>();
            // ==== MODIFICA 01/11/2026 - INIZIO ====
            const string sql = @"SELECT IdMandatario, NomeMandatario, CodiceMandatario, Perc_Delta_Provvigione,
                                        IdUnitaMisuraDefault, QuotaCentesimiPerKg
                                 FROM Mandatari
                                 ";
            // ==== MODIFICA 01/11/2026 - FINE ====

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(sql, conn);

                conn.Open();
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    lista.Add(new Mandatario
                    {
                        IdMandatario = reader.GetIntSafe("IdMandatario"),
                        NomeMandatario = reader.GetStringSafe("NomeMandatario"),
                        CodiceMandatario = reader.GetStringSafe("CodiceMandatario"),
                        Perc_Delta_Provvigione = reader.GetNullableDecimal("Perc_Delta_Provvigione"),
                        // ==== MODIFICA 01/11/2026 - INIZIO ====
                        IdUnitaMisuraDefault = reader.GetNullableInt("IdUnitaMisuraDefault"),
                        QuotaCentesimiPerKg = reader.GetNullableDecimal("QuotaCentesimiPerKg")
                        // ==== MODIFICA 01/11/2026 - FINE ====



                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Errore SQL durante la lettura della tabella Mandatario.", ex);
            }

            return lista;
        }
        public int GetTipoPagamento(int idMandatario)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spMandatari_GetTipoPagamento", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = idMandatario;

            conn.Open();
            var result = cmd.ExecuteScalar();

            return result != null ? Convert.ToInt32(result) : 0;
        }

        public Mandatario? GetById(int id)
        {
            // ==== MODIFICA 01/11/2026 - INIZIO ====
            const string sql = @"SELECT IdMandatario, NomeMandatario, CodiceMandatario, Perc_Delta_Provvigione,
                                        IdUnitaMisuraDefault, QuotaCentesimiPerKg
                                 FROM Mandatari
                                 WHERE IdMandatario = @IdMandatario";
            // ==== MODIFICA 01/11/2026 - FINE ====

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = id;

                conn.Open();
                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return new Mandatario
                    {
                        IdMandatario = reader.GetIntSafe("IdMandatario"),
                        NomeMandatario = reader.GetStringSafe("NomeMandatario"),
                        CodiceMandatario = reader.GetStringSafe("CodiceMandatario"),
                        // ==== MODIFICA 29/10/2026 - INIZIO ====
                        Perc_Delta_Provvigione = reader.GetNullableDecimal("Perc_Delta_Provvigione"),
                        // ==== MODIFICA 01/11/2026 - INIZIO ====
                        IdUnitaMisuraDefault = reader.GetNullableInt("IdUnitaMisuraDefault"),
                        QuotaCentesimiPerKg = reader.GetNullableDecimal("QuotaCentesimiPerKg")
                        // ==== MODIFICA 01/11/2026 - FINE ====

                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore SQL durante la lettura del Mandatario ID: {id}", ex);
            }
        }








        public void Insert(Mandatario mandatario)
        {
            // ==== MODIFICA 01/11/2026 - INIZIO ====
            const string sql = @"
                INSERT INTO Mandatari (NomeMandatario, CodiceMandatario, Perc_Delta_Provvigione,
                IdUnitaMisuraDefault, QuotaCentesimiPerKg, DataInserimento, Utente)
                VALUES (@NomeMandatario, @CodiceMandatario, @Perc_Delta_Provvigione,
                @IdUnitaMisuraDefault, @QuotaCentesimiPerKg, GETDATE(), @Utente);
                ";
            // ==== MODIFICA 01/11/2026 - FINE ====

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.Add("@NomeMandatario", SqlDbType.NVarChar).Value = mandatario.NomeMandatario;
                cmd.Parameters.Add("@CodiceMandatario", SqlDbType.NVarChar).Value = mandatario.CodiceMandatario;
                // ==== MODIFICA 29/10/2026 - INIZIO ==== campo vuoto in maschera => 0 (coerente col DEFAULT)
                AddDecimal(cmd, "@Perc_Delta_Provvigione", mandatario.Perc_Delta_Provvigione ?? 0m, 18, 6);
                // ==== MODIFICA 29/10/2026 - FINE ====                
                cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = mandatario.Utente;
                // ==== MODIFICA 01/11/2026 - INIZIO ====
                cmd.Parameters.Add("@IdUnitaMisuraDefault", SqlDbType.Int).Value = (object?)mandatario.IdUnitaMisuraDefault ?? DBNull.Value;
                AddDecimal(cmd, "@QuotaCentesimiPerKg", mandatario.QuotaCentesimiPerKg ?? 0m, 18, 6); // vuoto => 0
                // ==== MODIFICA 01/11/2026 - FINE ====
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Errore SQL in MandatarioRepository.Insert", ex);
            }
        }


        public void Update(Mandatario mandatario)
        {
            // ==== MODIFICA 01/11/2026 - INIZIO ====
            const string sql = @"
                UPDATE Mandatari
                SET NomeMandatario = @NomeMandatario,
                CodiceMandatario = @CodiceMandatario,
                Perc_Delta_Provvigione = @Perc_Delta_Provvigione,
                IdUnitaMisuraDefault = @IdUnitaMisuraDefault,
                QuotaCentesimiPerKg = @QuotaCentesimiPerKg,
                DataModifica = GETDATE(),
                Utente = @Utente
                WHERE IdMandatario = @IdMandatario;
                ";
            // ==== MODIFICA 01/11/2026 - FINE ====

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(sql, conn);

                cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = mandatario.IdMandatario;
                cmd.Parameters.Add("@NomeMandatario", SqlDbType.NVarChar).Value = mandatario.NomeMandatario;
                cmd.Parameters.Add("@CodiceMandatario", SqlDbType.NVarChar).Value = mandatario.CodiceMandatario;
                cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = mandatario.Utente;
                // ==== MODIFICA 29/10/2026 - INIZIO ==== campo vuoto in maschera => 0 (coerente col DEFAULT)
                AddDecimal(cmd, "@Perc_Delta_Provvigione", mandatario.Perc_Delta_Provvigione ?? 0m, 18, 6);
                // ==== MODIFICA 29/10/2026 - FINE ====
                // 
                // ==== MODIFICA 01/11/2026 - INIZIO ====
                cmd.Parameters.Add("@IdUnitaMisuraDefault", SqlDbType.Int).Value = (object?)mandatario.IdUnitaMisuraDefault ?? DBNull.Value;
                AddDecimal(cmd, "@QuotaCentesimiPerKg", mandatario.QuotaCentesimiPerKg ?? 0m, 18, 6); // vuoto => 0
                // ==== MODIFICA 01/11/2026  - FINE ====
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Errore SQL in MandatarioRepository.Update", ex);
            }
        }



        public void Delete(int id)
        {
            const string sql = @"DELETE FROM Mandatari WHERE IdMandatario = @IdMandatario";
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = id;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                var dump = SqlDebugHelper.DumpParameters(cmd);
                throw new Exception($"Errore SQL in MandatarioRepository.Delete\n{dump}", ex);
            }
        }

        public int CountRelazioniClienti(int idMandatario)
        {
            const string sql = @"SELECT COUNT(*) FROM MandatariClienti WHERE IdMandatario = @IdMandatario";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);

            // Parametro tipizzato come da standard del progetto
            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = idMandatario;

            try
            {
                conn.Open();
                // ExecuteScalar restituisce object, facciamo il cast a int
                return (int)cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                var dump = SqlDebugHelper.DumpParameters(cmd);
                throw new Exception($"Errore SQL in MandatarioRepository.CountRelazioniClienti\n{dump}", ex);
            }
        }
        public IEnumerable<Cliente> GetByMandatario(int idMandatario)
        {
            var lista = new List<Cliente>();
            //AGGIUNTO: AND c.IsDeleted = 0  Filtro per mostrare solo i clienti attivi 31/03/2026
            const string sql = @"
                SELECT c.IdCliente, c.RagioneSociale
                FROM MandatariClienti mc
                INNER JOIN Clienti c ON c.IdCliente = mc.IdCliente
                WHERE mc.IdMandatario = @IdMandatario
                AND c.IsDeleted = 0 
                ORDER BY c.RagioneSociale;
                  ";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = idMandatario;

            conn.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new Cliente
                {
                    IdCliente = reader.GetInt32(reader.GetOrdinal("IdCliente")),
                    RagioneSociale = reader.GetString(reader.GetOrdinal("RagioneSociale"))
                });
            }

            return lista;
        }

        public void InsertMandatarioClienteIfNotExists(int idMandatario, int idCliente)
        {
            const string sql = @"
        IF NOT EXISTS (
            SELECT 1 FROM MandatariClienti
            WHERE IdMandatario = @IdMandatario AND IdCliente = @IdCliente
        )
        INSERT INTO MandatariClienti (IdMandatario, IdCliente)
        VALUES (@IdMandatario, @IdCliente);
    ";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = idMandatario;
            cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = idCliente;

            conn.Open();
            cmd.ExecuteNonQuery();
        }


        public void DeleteMandatarioCliente(int idMandatario, int idCliente)
        {
            const string sql = @"
        DELETE FROM MandatariClienti
        WHERE IdMandatario = @IdMandatario AND IdCliente = @IdCliente;
    ";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = idMandatario;
            cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = idCliente;

            conn.Open();
            cmd.ExecuteNonQuery();
        }



        // ==== MODIFICA 29/10/2026 - INIZIO ====
        private static void AddDecimal(SqlCommand cmd, string name, decimal? value, byte precision, byte scale)
        {
            var p = cmd.Parameters.Add(name, SqlDbType.Decimal);
            p.Precision = precision;
            p.Scale = scale;
            p.Value = (object?)value ?? DBNull.Value;
        }
        // ==== MODIFICA 29/10/2026 - FINE ====






    }

}
