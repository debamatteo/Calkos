using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Infrastructure.Helpers;

namespace CalkosManager.Infrastructure.Repositories
{
    /// <summary>
    /// Implementazione Repository per TipoPagamento
    /// Utilizza gli helper GetIntSafe/GetStringSafe per coerenza con AgenteRepository
    /// </summary>
    public class TipoPagamentoRepository : ITipoPagamentoRepository
    {
        private readonly string _connectionString;

        public TipoPagamentoRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<TipoPagamento> GetAll()
        {
            var result = new List<TipoPagamento>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spTipoPagamento_GetAll", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            conn.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(Map(reader));
            }

            return result;
        }

        public TipoPagamento? GetById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spTipoPagamento_GetById", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdTipoPagamento", SqlDbType.Int).Value = id;

            conn.Open();

            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Map(reader) : null;
        }

        public void Insert(TipoPagamento tipoPagamento)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spTipoPagamento_Insert", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@CodiceTipoPagamento", SqlDbType.VarChar, 10).Value = tipoPagamento.CodiceTipoPagamento;
            cmd.Parameters.Add("@TipoPagamento", SqlDbType.VarChar, 100).Value = tipoPagamento.DescrizioneTipoPagamento;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar, 100).Value = (object?)tipoPagamento.Utente ?? DBNull.Value;

            // Parametri dashboard come da SQL
            cmd.Parameters.Add("@VisualizzaInDashboard", SqlDbType.Bit).Value = tipoPagamento.VisualizzaInDashboard;
            cmd.Parameters.Add("@GruppoDashboard", SqlDbType.NVarChar, 50).Value = (object?)tipoPagamento.GruppoDashboard ?? DBNull.Value;
            cmd.Parameters.Add("@OrdineVisualizzazione", SqlDbType.Int).Value = tipoPagamento.OrdineVisualizzazione;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                var dump = SqlDebugHelper.DumpParameters(cmd);
                throw new Exception($"Errore SQL in TipoPagamentoRepository.Insert\n{dump}", ex);
            }
        }

        public void Update(TipoPagamento tipoPagamento)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spTipoPagamento_Update", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdTipoPagamento", SqlDbType.Int).Value = tipoPagamento.IdTipoPagamento;
            cmd.Parameters.Add("@CodiceTipoPagamento", SqlDbType.VarChar, 10).Value = tipoPagamento.CodiceTipoPagamento;
            cmd.Parameters.Add("@TipoPagamento", SqlDbType.VarChar, 100).Value = tipoPagamento.DescrizioneTipoPagamento;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar, 100).Value = (object?)tipoPagamento.Utente ?? DBNull.Value;

            cmd.Parameters.Add("@VisualizzaInDashboard", SqlDbType.Bit).Value = tipoPagamento.VisualizzaInDashboard;
            cmd.Parameters.Add("@GruppoDashboard", SqlDbType.NVarChar, 50).Value = (object?)tipoPagamento.GruppoDashboard ?? DBNull.Value;
            cmd.Parameters.Add("@OrdineVisualizzazione", SqlDbType.Int).Value = tipoPagamento.OrdineVisualizzazione;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                var dump = SqlDebugHelper.DumpParameters(cmd);
                throw new Exception($"Errore SQL in TipoPagamentoRepository.Update\n{dump}", ex);
            }
        }

        public void Delete(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spTipoPagamento_Delete", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdTipoPagamento", SqlDbType.Int).Value = id;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                var dump = SqlDebugHelper.DumpParameters(cmd);
                throw new Exception($"Errore SQL in TipoPagamentoRepository.Delete\n{dump}", ex);
            }
        }

        /// <summary>
        /// Mapping privato che utilizza gli helper Safe per evitare eccezioni su valori null o colonne mancanti
        /// </summary>
        private TipoPagamento Map(SqlDataReader reader)
        {
            // 1. Istanza vuota
            var entity = new TipoPagamento();

            // 2. Assegnazioni dirette (Anagrafica e Dashboard)
            entity.IdTipoPagamento = reader.GetIntSafe("IdTipoPagamento");
            entity.CodiceTipoPagamento = reader.GetStringSafe("CodiceTipoPagamento");
            entity.DescrizioneTipoPagamento = reader.GetStringSafe("TipoPagamento");

            // Gestione VisualizzaInDashboard: separata per intercettare subito errori di tipo bit/bool
            int ordVisualizza = reader.GetOrdinal("VisualizzaInDashboard");
            entity.VisualizzaInDashboard = reader.GetBoolean(ordVisualizza);

            entity.GruppoDashboard = reader.GetStringSafe("GruppoDashboard");
            entity.OrdineVisualizzazione = reader.GetIntSafe("OrdineVisualizzazione");
            entity.Utente = reader.GetStringSafe("Utente");

            // 3. Gestione sicura delle date (evita crash se le colonne mancano nella query)
            try
            {
                int ordIns = reader.GetOrdinal("DataInserimento");
                entity.DataInserimento = reader.GetDateTime(ordIns);
            }
            catch
            {
                /* Colonna DataInserimento non presente nella SELECT */
            }

            try
            {
                int ordMod = reader.GetOrdinal("DataModifica");
                if (!reader.IsDBNull(ordMod))
                {
                    entity.DataModifica = reader.GetDateTime(ordMod);
                }
            }
            catch
            {
                /* Colonna DataModifica non presente nella SELECT */
            }

            return entity;
        }
    }
}


//using System.Data;
//using System.Data.SqlClient;
//using CalkosManager.Domain.Entities;
//using CalkosManager.Domain.Interfaces.Repositories;
//using Microsoft.Data.SqlClient;

//namespace CalkosManager.Infrastructure.Repositories
//{
//    public class TipoPagamentoRepository : ITipoPagamentoRepository
//    {
//        private readonly string _connectionString;

//        public TipoPagamentoRepository(string connectionString)
//        {
//            _connectionString = connectionString;
//        }

//        public List<TipoPagamento> GetAll()
//        {
//            using var conn = new SqlConnection(_connectionString);
//            using var cmd = new SqlCommand("spTipoPagamento_GetAll", conn)
//            {
//                CommandType = CommandType.StoredProcedure
//            };

//            conn.Open();
//            using var reader = cmd.ExecuteReader();

//            var list = new List<TipoPagamento>();

//            while (reader.Read())
//            {
//                list.Add(Map(reader));
//            }

//            return list;
//        }

//        public TipoPagamento? GetById(int id)
//        {
//            using var conn = new SqlConnection(_connectionString);
//            using var cmd = new SqlCommand("spTipoPagamento_GetById", conn)
//            {
//                CommandType = CommandType.StoredProcedure
//            };

//            cmd.Parameters.Add("@IdTipoPagamento", SqlDbType.Int).Value = id;

//            conn.Open();
//            using var reader = cmd.ExecuteReader();

//            return reader.Read() ? Map(reader) : null;
//        }

//        public void Insert(TipoPagamento tipoPagamento)
//        {
//            using var conn = new SqlConnection(_connectionString);
//            using var cmd = new SqlCommand("spTipoPagamento_Insert", conn)
//            {
//                CommandType = CommandType.StoredProcedure
//            };

//            cmd.Parameters.Add("@CodiceTipoPagamento", SqlDbType.VarChar, 10).Value = tipoPagamento.CodiceTipoPagamento;
//            cmd.Parameters.Add("@TipoPagamento", SqlDbType.VarChar, 100).Value = tipoPagamento.DescrizioneTipoPagamento;

//            conn.Open();
//            cmd.ExecuteNonQuery();
//        }

//        public void Update(TipoPagamento tipoPagamento)
//        {
//            using var conn = new SqlConnection(_connectionString);
//            using var cmd = new SqlCommand("spTipoPagamento_Update", conn)
//            {
//                CommandType = CommandType.StoredProcedure
//            };

//            cmd.Parameters.Add("@IdTipoPagamento", SqlDbType.Int).Value = tipoPagamento.IdTipoPagamento;
//            cmd.Parameters.Add("@CodiceTipoPagamento", SqlDbType.VarChar, 10).Value = tipoPagamento.CodiceTipoPagamento;
//            cmd.Parameters.Add("@TipoPagamento", SqlDbType.VarChar, 100).Value = tipoPagamento.DescrizioneTipoPagamento;

//            conn.Open();
//            cmd.ExecuteNonQuery();
//        }

//        public void Delete(int id)
//        {
//            using var conn = new SqlConnection(_connectionString);
//            using var cmd = new SqlCommand("spTipoPagamento_Delete", conn)
//            {
//                CommandType = CommandType.StoredProcedure
//            };

//            cmd.Parameters.Add("@IdTipoPagamento", SqlDbType.Int).Value = id;

//            conn.Open();
//            cmd.ExecuteNonQuery();
//        }

//        private TipoPagamento Map(SqlDataReader reader)
//        {
//            return new TipoPagamento
//            {
//                IdTipoPagamento = reader.GetInt32(reader.GetOrdinal("IdTipoPagamento")),
//                CodiceTipoPagamento = reader.GetString(reader.GetOrdinal("CodiceTipoPagamento")),
//                DescrizioneTipoPagamento = reader.GetString(reader.GetOrdinal("TipoPagamento"))
//            };
//        }
//    }
//}
