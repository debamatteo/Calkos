/*
    ============================================================
    STATISTICHE GLOBALI REPOSITORY - 23/09/2026
    ============================================================
    ✔ Sola lettura, tramite stored procedure (aggregazione su 9 tabelle).
    ✔ Parametri tipizzati con SqlDbType, niente AddWithValue.
    ✔ Lettura con SqlReaderExtensions (GetIntSafe, GetDecimalSafe, ...).
    ✔ In caso di errore: SqlDebugHelper.DumpParameters(cmd) nel messaggio.
    ============================================================
*/

using CalkosManager.Domain.Interfaces;
using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Domain.Models;
using CalkosManager.Infrastructure.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CalkosManager.Infrastructure.Repositories
{
    public class StatisticheGlobaliRepository : IStatisticheGlobaliRepository
    {
        private readonly string _connectionString;

        public StatisticheGlobaliRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IReadOnlyList<FatturatoMensileMandatario> GetFatturatoMensile(int annoDa, int annoA)
        {
            var risultati = new List<FatturatoMensileMandatario>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.spStatisticheGlobali_Fatturato", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.Add("@AnnoDa", SqlDbType.Int).Value = annoDa;
            cmd.Parameters.Add("@AnnoA", SqlDbType.Int).Value = annoA;

            try
            {
                conn.Open();
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    risultati.Add(new FatturatoMensileMandatario
                    {
                        IdMandatario = reader.GetIntSafe("IdMandatario"),
                        Anno = reader.GetIntSafe("Anno"),
                        Mese = reader.GetIntSafe("Mese"),
                        Quantita = reader.GetDecimalSafe("Quantita"),
                        EuroProvvigione = reader.GetDecimalSafe("EuroProvvigione"),
                        UnitaMisura = reader.GetStringSafe("UnitaMisura")
                    });
                }
            }
            catch (Exception ex)
            {
                var dump = SqlDebugHelper.DumpParameters(cmd);
                throw new Exception($"Errore SQL in StatisticheGlobaliRepository.GetFatturatoMensile\n{dump}", ex);
            }

            return risultati;
        }

        public IReadOnlyList<int> GetAnniDisponibili()
        {
            var anni = new List<int>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.spStatisticheGlobali_AnniDisponibili", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            try
            {
                conn.Open();
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                    anni.Add(reader.GetIntSafe("Anno"));
            }
            catch (Exception ex)
            {
                throw new Exception("Errore SQL in StatisticheGlobaliRepository.GetAnniDisponibili", ex);
            }

            return anni;
        }
    }
}
