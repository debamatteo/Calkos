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
        MATERIALI DIMENSIONI REPOSITORY — VERSIONE ALLINEATA
        ============================================================
        ✔ Allineato a ProvvigioniAgentiRepository
        ✔ Gestione campi Audit (Utente, DataInserimento, DataModifica)
        ✔ Mapping completo Entity
        ============================================================
    */

    public class MaterialiDimensioniRepository : IMaterialiDimensioniRepository
    {
        private readonly string _connectionString;

        public MaterialiDimensioniRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<MaterialiDimensioni> GetAll()
        {
            var result = new List<MaterialiDimensioni>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spMaterialiDimensioni_GetAll", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            conn.Open();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                result.Add(MapReaderToEntity(reader));
            }

            return result;
        }

        public MaterialiDimensioni? GetById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spMaterialiDimensioni_GetById", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdDimensione", SqlDbType.Int).Value = id;

            conn.Open();
            using var reader = cmd.ExecuteReader();

            return reader.Read() ? MapReaderToEntity(reader) : null;
        }

        public void Insert(MaterialiDimensioni item)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spMaterialiDimensioni_Insert", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(cmd, item, isUpdate: false);

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                var dump = SqlDebugHelper.DumpParameters(cmd);
                throw new Exception($"Errore SQL in MaterialiDimensioniRepository.Insert\n{dump}", ex);
            }
        }

        public void Update(MaterialiDimensioni item)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spMaterialiDimensioni_Update", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            AddParameters(cmd, item, isUpdate: true);

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                var dump = SqlDebugHelper.DumpParameters(cmd);
                throw new Exception($"Errore SQL in MaterialiDimensioniRepository.Update\n{dump}", ex);
            }
        }

        public void Delete(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spMaterialiDimensioni_Delete", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdDimensione", SqlDbType.Int).Value = id;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                var dump = SqlDebugHelper.DumpParameters(cmd);
                throw new Exception($"Errore SQL in MaterialiDimensioniRepository.Delete\n{dump}", ex);
            }
        }

        // ============================================================
        // HELPER PRIVATI PER EVITARE REPLICAZIONE CODICE
        // ============================================================

        private void AddParameters(SqlCommand cmd, MaterialiDimensioni item, bool isUpdate)
        {
            if (isUpdate)
                cmd.Parameters.Add("@Id", SqlDbType.Int).Value = item.IdDimensione;

            cmd.Parameters.Add("@IdMateriale", SqlDbType.Int).Value = item.IdMateriale;
            cmd.Parameters.Add("@Tipo", SqlDbType.NVarChar).Value = item.Tipo;
            cmd.Parameters.Add("@Valore", SqlDbType.NVarChar).Value = item.Valore;
            cmd.Parameters.Add("@Ordine", SqlDbType.Int).Value = item.Ordine;

            // Campi Audit (fondamentali per coerenza con Service e modello Provvigioni)
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = (object?)item.Utente ?? DBNull.Value;
        }

        private MaterialiDimensioni MapReaderToEntity(SqlDataReader reader)
        {
            return new MaterialiDimensioni
            {
                IdDimensione = reader.GetIntSafe("IdDimensione"),
                IdMateriale = reader.GetIntSafe("IdMateriale"),
                Tipo = reader.GetStringSafe("Tipo"),
                Valore = reader.GetStringSafe("Valore"),
                Ordine = reader.GetIntSafe("Ordine"),
                Utente = reader.GetStringSafe("Utente"),
                DataInserimento = reader.GetDateTimeSafe("DataInserimento"),
                DataModifica = reader.GetNullableDateTime("DataModifica")
            };
        }
    }
}