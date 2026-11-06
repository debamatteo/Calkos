using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Infrastructure.Helpers;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace CalkosManager.Infrastructure.Repositories
{
    /*
        ============================================================
        PROVVIGIONI AGENTI REPOSITORY — VERSIONE CORRETTA
        ============================================================

        ✔ Infrastructure NON può usare DTO del Web
        ✔ Infrastructure restituisce SOLO ENTITY
        ✔ JOIN e descrizioni NON vanno qui
        ✔ Le descrizioni verranno risolte nel Controller o nella View
        ============================================================
    */

    public class ProvvigioniAgentiRepository : IProvvigioniAgentiRepository
    {
        private readonly string _connectionString;

        public ProvvigioniAgentiRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ============================================================
        // GET ALL — SOLO ENTITY (NO DTO)
        // ============================================================
        public List<ProvvigioniAgenti> GetAll()
        {
            var result = new List<ProvvigioniAgenti>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProvvigioniAgenti_GetAll", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            conn.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                result.Add(new ProvvigioniAgenti
                {
                    IdProvvigione = reader.GetIntSafe("IdProvvigione"),
                    IdAgente = reader.GetIntSafe("IdAgente"),
                    IdMandatario = reader.GetIntSafe("IdMandatario"),
                    IdCliente = reader.GetNullableInt("IdCliente"),
                    Percentuale = reader.GetDecimalSafe("Percentuale"),
                    Utente = reader.GetStringSafe("Utente"),
                    DataInserimento = reader.GetDateTimeSafe("DataInserimento"),
                    DataModifica = reader.GetNullableDateTime("DataModifica")
                });
            }

            return result;
        }

        // ============================================================
        // GET BY ID — SOLO ENTITY
        // ============================================================
        public ProvvigioniAgenti? GetById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProvvigioniAgenti_GetById", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdProvvigione", SqlDbType.Int).Value = id;

            conn.Open();
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return null;

            return new ProvvigioniAgenti
            {
                IdProvvigione = reader.GetIntSafe("IdProvvigione"),
                IdAgente = reader.GetIntSafe("IdAgente"),
                IdMandatario = reader.GetIntSafe("IdMandatario"),
                IdCliente = reader.GetNullableInt("IdCliente"),
                Percentuale = reader.GetDecimalSafe("Percentuale"),
                Utente = reader.GetStringSafe("Utente"),
                DataInserimento = reader.GetDateTimeSafe("DataInserimento"),
                DataModifica = reader.GetNullableDateTime("DataModifica")
            };
        }

        // ============================================================
        // 26/10/2026 GET BY COMBINAZIONE (Agente + Mandatario + Cliente)
        // Usata per impedire provvigioni duplicate. IdCliente null = "tutti i clienti".
        // ============================================================
        public ProvvigioniAgenti? GetByCombinazione(int idAgente, int idMandatario, int? idCliente, int escludiIdProvvigione = 0)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProvvigioniAgenti_GetByCombinazione", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = idAgente;
            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = idMandatario;
            cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = (object?)idCliente ?? DBNull.Value;
            cmd.Parameters.Add("@EscludiIdProvvigione", SqlDbType.Int).Value = escludiIdProvvigione;

            try
            {
                conn.Open();
                using var reader = cmd.ExecuteReader();

                if (!reader.Read())
                    return null;

                return new ProvvigioniAgenti
                {
                    IdProvvigione = reader.GetIntSafe("IdProvvigione"),
                    IdAgente = reader.GetIntSafe("IdAgente"),
                    IdMandatario = reader.GetIntSafe("IdMandatario"),
                    IdCliente = reader.GetNullableInt("IdCliente"),
                    Percentuale = reader.GetDecimalSafe("Percentuale"),
                    Utente = reader.GetStringSafe("Utente"),
                    DataInserimento = reader.GetDateTimeSafe("DataInserimento"),
                    DataModifica = reader.GetNullableDateTime("DataModifica")
                };
            }
            catch (Exception ex)
            {
                var dump = SqlDebugHelper.DumpParameters(cmd);
                throw new Exception($"Errore SQL in ProvvigioniAgentiRepository.GetByCombinazione\n{dump}", ex);
            }
        }

        // ============================================================
        // INSERT
        // ============================================================
        public void Insert(ProvvigioniAgenti provvigione)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProvvigioniAgenti_Insert", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = provvigione.IdAgente;
            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = provvigione.IdMandatario;
            cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = (object?)provvigione.IdCliente ?? DBNull.Value;

            // --- CORREZIONE QUI ---
            SqlParameter pPercentuale = new SqlParameter("@Percentuale", SqlDbType.Decimal)
            {
                Value = provvigione.Percentuale,
                Precision = 5,
                Scale = 2
            };
            cmd.Parameters.Add(pPercentuale);
            // ----------------------

            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = (object?)provvigione.Utente ?? DBNull.Value;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                var dump = SqlDebugHelper.DumpParameters(cmd);
                throw new Exception($"Errore SQL in ProvvigioniAgentiRepository.Insert\n{dump}", ex);
            }
        }

        // ============================================================
        // UPDATE
        // ============================================================
        public void Update(ProvvigioniAgenti provvigione)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProvvigioniAgenti_Update", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = provvigione.IdProvvigione;
            cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = provvigione.IdAgente;
            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = provvigione.IdMandatario;
            cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value =
                (object?)provvigione.IdCliente ?? DBNull.Value;

            // 26/10/2026 Precision/Scale espliciti come in Insert (decimal(5,2) della colonna): la sovrascrittura
            // da "Replica" passa da qui e deve salvare la percentuale esattamente come l'inserimento.
            cmd.Parameters.Add(new SqlParameter("@Percentuale", SqlDbType.Decimal)
            {
                Value = provvigione.Percentuale,
                Precision = 5,
                Scale = 2
            });

            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value =
                (object?)provvigione.Utente ?? DBNull.Value;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                var dump = SqlDebugHelper.DumpParameters(cmd);
                throw new Exception($"Errore SQL in ProvvigioniAgentiRepository.Update\n{dump}", ex);
            }
        }

        // ============================================================
        // DELETE
        // ============================================================
        public void Delete(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spProvvigioniAgenti_Delete", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdProvvigione", SqlDbType.Int).Value = id;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                var dump = SqlDebugHelper.DumpParameters(cmd);
                throw new Exception($"Errore SQL in ProvvigioniAgentiRepository.Delete\n{dump}", ex);
            }
        }
    }
}
