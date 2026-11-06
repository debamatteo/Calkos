using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Infrastructure.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CalkosManager.Infrastructure.Repositories
{
    /*
        ============================================================
        CALCOLO BASE MATERIALE REPOSITORY
        ============================================================
        ✔ Allineato a MaterialiDimensioniRepository / ProspettoDeAngeliRepository
        ✔ NIENTE AddWithValue — parametri sempre tipizzati esplicitamente
        ✔ Precision/Scale espliciti su tutti i decimali (18,6)
        ============================================================
    */
    public class CalcoloBaseMaterialeRepository : ICalcoloBaseMaterialeRepository
    {
        private readonly string _connectionString;

        public CalcoloBaseMaterialeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<CalcoloBaseMateriale> GetStorico(int idMateriale, int top = 4)
        {
            var result = new List<CalcoloBaseMateriale>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spCalcoloBaseMateriale_GetStorico", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Add("@IdMateriale", SqlDbType.Int).Value = idMateriale;
            cmd.Parameters.Add("@Top", SqlDbType.Int).Value = top;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) result.Add(Map(reader));
            return result;
        }

        public CalcoloBaseMateriale? GetUltimoAttivo(int idMateriale)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spCalcoloBaseMateriale_GetUltimoAttivo", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Add("@IdMateriale", SqlDbType.Int).Value = idMateriale;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Map(reader) : null;
        }

        public CalcoloBaseMateriale? GetById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spCalcoloBaseMateriale_GetById", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            conn.Open();
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Map(reader) : null;
        }

        public int Insert(CalcoloBaseMateriale item)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spCalcoloBaseMateriale_Insert", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdMateriale", SqlDbType.Int).Value = item.IdMateriale;
            cmd.Parameters.Add("@DataRiferimento", SqlDbType.Date).Value = item.DataRiferimento;
            AddDecimal(cmd, "@Lme", item.Lme);
            AddDecimal(cmd, "@PremioMiniera", item.PremioMiniera);
            AddDecimal(cmd, "@Cambio", item.Cambio);
            AddDecimal(cmd, "@PercFinanziamento", item.PercFinanziamento);
            AddDecimal(cmd, "@EuroFinanziamento", item.EuroFinanziamento);
            AddDecimal(cmd, "@MaterialeEuroRiferimento", item.MaterialeEuroRiferimento);
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar, 100).Value = (object?)item.Utente ?? DBNull.Value;

            try
            {
                conn.Open();
                var result = cmd.ExecuteScalar();
                int newId = Convert.ToInt32(result);
                item.Id = newId;
                return newId;
            }
            catch (Exception ex)
            {
                var dump = SqlDebugHelper.DumpParameters(cmd);
                throw new Exception($"Errore SQL in CalcoloBaseMaterialeRepository.Insert\n{dump}", ex);
            }
        }

        private static void AddDecimal(SqlCommand cmd, string name, decimal? value)
        {
            var p = cmd.Parameters.Add(name, SqlDbType.Decimal);
            p.Precision = 18;
            p.Scale = 6;
            p.Value = (object?)value ?? DBNull.Value;
        }

        private static CalcoloBaseMateriale Map(SqlDataReader reader)
        {
            return new CalcoloBaseMateriale
            {
                Id = reader.GetIntSafe("Id"),
                IdMateriale = reader.GetIntSafe("IdMateriale"),
                DataRiferimento = reader.GetDateTimeSafe("DataRiferimento"),
                Lme = reader.GetNullableDecimal("Lme"),
                PremioMiniera = reader.GetNullableDecimal("PremioMiniera"),
                Cambio = reader.GetNullableDecimal("Cambio"),
                PercFinanziamento = reader.GetNullableDecimal("PercFinanziamento"),
                EuroFinanziamento = reader.GetNullableDecimal("EuroFinanziamento"),
                MaterialeEuroRiferimento = reader.GetNullableDecimal("MaterialeEuroRiferimento") ?? 0,
                DescrizioneMateriale = reader.GetStringSafe("DescrizioneMateriale")
            };
        }



        public (bool Eliminato, string Messaggio) Delete(int id)//28/08/2026
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spCalcoloBaseMateriale_Delete", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            conn.Open();
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                bool eliminato = reader.GetBoolean(reader.GetOrdinal("Eliminato"));
                string messaggio = reader.IsDBNull(reader.GetOrdinal("Messaggio"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("Messaggio"));
                return (eliminato, messaggio);
            }

            return (false, "Errore imprevisto durante la cancellazione.");
        }

    }
}