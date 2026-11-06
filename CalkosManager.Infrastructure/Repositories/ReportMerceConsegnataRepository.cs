/*
    ============================================================
    REPORT MERCE CONSEGNATA DDT REPOSITORY - 05/10/2026
    ============================================================
    ✔ Sola lettura, tramite stored procedure spReportMerceConsegnataDdt.
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
    public class ReportMerceConsegnataRepository : IReportMerceConsegnataRepository
    {
        private readonly string _connectionString;

        public ReportMerceConsegnataRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IReadOnlyList<RigaMerceConsegnataDdtDto> GetReport(FiltriMerceConsegnataDdtDto filtri)
        {
            var risultati = new List<RigaMerceConsegnataDdtDto>();

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.spReportMerceConsegnataDdt", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdMandatario", SqlDbType.Int).Value = (object?)filtri.IdMandatario ?? DBNull.Value;
            cmd.Parameters.Add("@IdCliente", SqlDbType.Int).Value = (object?)filtri.IdCliente ?? DBNull.Value;
            cmd.Parameters.Add("@IdMateriale", SqlDbType.Int).Value = (object?)filtri.IdMateriale ?? DBNull.Value;
            cmd.Parameters.Add("@AnnoDa", SqlDbType.Int).Value = (object?)filtri.AnnoDa ?? DBNull.Value;
            cmd.Parameters.Add("@AnnoA", SqlDbType.Int).Value = (object?)filtri.AnnoA ?? DBNull.Value;
            cmd.Parameters.Add("@Mese", SqlDbType.Int).Value = (object?)filtri.Mese ?? DBNull.Value;

            SqlParameter pDiametro = cmd.Parameters.Add("@Diametro", SqlDbType.Decimal);
            pDiametro.Precision = 18;
            pDiametro.Scale = 3;
            pDiametro.Value = (object?)filtri.Diametro ?? DBNull.Value;

            SqlParameter pSpessore = cmd.Parameters.Add("@Spessore", SqlDbType.Decimal);
            pSpessore.Precision = 18;
            pSpessore.Scale = 3;
            pSpessore.Value = (object?)filtri.Spessore ?? DBNull.Value;

            SqlParameter pLarghezza = cmd.Parameters.Add("@Larghezza", SqlDbType.Decimal);
            pLarghezza.Precision = 18;
            pLarghezza.Scale = 3;
            pLarghezza.Value = (object?)filtri.Larghezza ?? DBNull.Value;

            // Raggruppamento scelto dall'utente (le dimensioni non selezionate restano neutre nel risultato)
            cmd.Parameters.Add("@RaggruppaMandatario", SqlDbType.Bit).Value = filtri.RaggruppaPerMandatario;
            cmd.Parameters.Add("@RaggruppaCliente", SqlDbType.Bit).Value = filtri.RaggruppaPerCliente;
            cmd.Parameters.Add("@RaggruppaMateriale", SqlDbType.Bit).Value = filtri.RaggruppaPerMateriale;
            cmd.Parameters.Add("@RaggruppaAnno", SqlDbType.Bit).Value = filtri.RaggruppaPerAnno;
            cmd.Parameters.Add("@RaggruppaMese", SqlDbType.Bit).Value = filtri.RaggruppaPerMese;
            cmd.Parameters.Add("@RaggruppaMisure", SqlDbType.Bit).Value = filtri.RaggruppaPerMisure;

            try
            {
                conn.Open();
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    risultati.Add(new RigaMerceConsegnataDdtDto
                    {
                        Mandatario = reader.GetStringSafe("Mandatario"),
                        Cliente = reader.GetStringSafe("Cliente"),
                        Materiale = reader.GetStringSafe("Materiale"),
                        Anno = reader.GetIntSafe("Anno"),
                        Mese = reader.GetIntSafe("Mese"),
                        Diametro = reader.IsDBNull(reader.GetOrdinal("Diametro")) ? null : reader.GetDecimalSafe("Diametro"),
                        Spessore = reader.IsDBNull(reader.GetOrdinal("Spessore")) ? null : reader.GetDecimalSafe("Spessore"),
                        Larghezza = reader.IsDBNull(reader.GetOrdinal("Larghezza")) ? null : reader.GetDecimalSafe("Larghezza"),
                        QuantitaTotale = reader.GetDecimalSafe("QuantitaTotale"),
                        UnitaMisura = reader.GetStringSafe("UnitaMisura")
                    });
                }
            }
            catch (Exception ex)
            {
                var dump = SqlDebugHelper.DumpParameters(cmd);
                throw new Exception($"Errore SQL in ReportMerceConsegnataRepository.GetReport\n{dump}", ex);
            }

            return risultati;
        }
    }
}