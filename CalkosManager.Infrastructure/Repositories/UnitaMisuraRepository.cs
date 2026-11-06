using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Infrastructure.Helpers;

namespace CalkosManager.Infrastructure.Repositories
{
    public class UnitaMisuraRepository : IUnitaMisuraRepository
    {
        private readonly string _connectionString;

        public UnitaMisuraRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private UnitaMisura Map(SqlDataReader reader)
        {
            // 1. Creazione dell'istanza vuota
            var entity = new UnitaMisura();

            // 2. Assegnazioni dirette (Perfette per il debug riga per riga)
            entity.IdUnitaMisura = reader.GetIntSafe("IdUnitaMisura");
            entity.CodiceUnitaMisura = reader.GetStringSafe("CodiceUnitaMisura");
            entity.UnitaMisuraDescrizione = reader.GetStringSafe("UnitaMisura");

            // --- METADATI ---
            entity.Utente = reader.GetStringSafe("Utente");
            entity.DataModifica = reader.GetNullableDateTime("DataModifica");

            // 3. Gestione protetta delle date (per evitare il crash se la colonna manca nella SELECT)
            try
            {
                int ordIns = reader.GetOrdinal("DataInserimento");
                entity.DataInserimento = reader.GetDateTime(ordIns);
            }
            catch
            {
                /* Colonna non presente nella query SQL: ignoriamo */
            }

            return entity;
        }

        public List<UnitaMisura> GetAll()
        {
            var list = new List<UnitaMisura>();
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spUnitaMisura_GetAll", conn) { CommandType = CommandType.StoredProcedure };

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(Map(reader));
            return list;
        }

        public UnitaMisura? GetByCodice(string codiceUnitaMisura)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spUnitaMisura_GetByCodice", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add("@CodiceUnitaMisura", SqlDbType.VarChar).Value = codiceUnitaMisura;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Map(reader) : null;
        }

        public UnitaMisura? GetByDescrizione(string descrizione)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spUnitaMisura_GetByDescrizione", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add("@UnitaMisura", SqlDbType.VarChar).Value = descrizione;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Map(reader) : null;
        }

        public void InsertManuale(UnitaMisura item)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spUnitaMisura_Insert", conn) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.Add("@CodiceUnitaMisura", SqlDbType.NVarChar).Value = (object?)item.CodiceUnitaMisura ?? DBNull.Value;
            cmd.Parameters.Add("@UnitaMisura", SqlDbType.NVarChar).Value = (object?)item.UnitaMisuraDescrizione ?? DBNull.Value;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = (object?)item.Utente ?? DBNull.Value;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore SQL in UnitaMisuraRepository.InsertManuale\n{SqlDebugHelper.DumpParameters(cmd)}", ex);
            }
        }

        public void Update(UnitaMisura unita)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spUnitaMisura_Update", conn) { CommandType = CommandType.StoredProcedure };

            cmd.Parameters.Add("@IdUnitaMisura", SqlDbType.Int).Value = unita.IdUnitaMisura;
            cmd.Parameters.Add("@CodiceUnitaMisura", SqlDbType.NVarChar).Value = (object?)unita.CodiceUnitaMisura ?? DBNull.Value;
            cmd.Parameters.Add("@UnitaMisura", SqlDbType.NVarChar).Value = (object?)unita.UnitaMisuraDescrizione ?? DBNull.Value;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = (object?)unita.Utente ?? DBNull.Value;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore SQL in UnitaMisuraRepository.Update\n{SqlDebugHelper.DumpParameters(cmd)}", ex);
            }
        }

        public void Delete(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spUnitaMisura_Delete", conn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add("@IdUnitaMisura", SqlDbType.Int).Value = id;

            try { conn.Open(); cmd.ExecuteNonQuery(); }
            catch (Exception ex) { throw new Exception($"Errore SQL in UnitaMisuraRepository.Delete\n{SqlDebugHelper.DumpParameters(cmd)}", ex); }
        }

        public UnitaMisura Insert(UnitaMisura item, SqlConnection conn, SqlTransaction tran)
        {
            const string sql = @"
                INSERT INTO UnitaMisura (CodiceUnitaMisura, UnitaMisura)
                VALUES (@CodiceUnitaMisura, @UnitaMisura);
                SELECT SCOPE_IDENTITY();";

            using var cmd = new SqlCommand(sql, conn, tran);
            cmd.Parameters.Add("@CodiceUnitaMisura", SqlDbType.VarChar).Value = (object?)item.CodiceUnitaMisura ?? DBNull.Value;
            cmd.Parameters.Add("@UnitaMisura", SqlDbType.VarChar).Value = (object?)item.UnitaMisuraDescrizione ?? DBNull.Value;

            try
            {
                item.IdUnitaMisura = Convert.ToInt32(cmd.ExecuteScalar());
                return item;
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore SQL in UnitaMisuraRepository.Insert (Inline)\n{SqlDebugHelper.DumpParameters(cmd)}", ex);
            }
        }
    }
}