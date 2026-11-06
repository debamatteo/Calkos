using CalkosManager.Domain.Entities;

using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Infrastructure.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;

public class AgenteRepository : IAgenteRepository
{
    private readonly string _connectionString;

    public AgenteRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public List<Agente> GetAll()
    {
        var result = new List<Agente>();

        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("spAgenti_GetAll", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        conn.Open();

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Agente
            {
                IdAgente = reader.GetIntSafe("IdAgente"),
                Nome = reader.GetStringSafe("Nome"),
                Cognome = reader.GetStringSafe("Cognome"),
                AgenteDescrizione = reader.GetStringSafe("AgenteDescrizione"),
               // CodiceAgente = reader.GetStringSafe("CodiceAgente"),
                PartitaIva = reader.GetStringSafe("PartitaIva"),
                CodiceFiscale = reader.GetStringSafe("CodiceFiscale"),
                PercentualeDefault = reader.GetNullableDecimal("PercentualeDefault"),
                Email = reader.GetStringSafe("Email"),
                Telefono1 = reader.GetStringSafe("Telefono1"),
                Telefono2 = reader.GetStringSafe("Telefono2")
            });
        }

        return result;
    }

    public Agente? GetById(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("spAgenti_GetById", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = id;

        conn.Open();

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;

        return new Agente
        {
            IdAgente = reader.GetIntSafe("IdAgente"),
            Nome = reader.GetStringSafe("Nome"),
            Cognome = reader.GetStringSafe("Cognome"),
            AgenteDescrizione = reader.GetStringSafe("AgenteDescrizione"),
           // CodiceAgente = reader.GetStringSafe("CodiceAgente"),
            PartitaIva = reader.GetStringSafe("PartitaIva"),
            CodiceFiscale = reader.GetStringSafe("CodiceFiscale"),
            PercentualeDefault = reader.GetNullableDecimal("PercentualeDefault"),
            Email = reader.GetStringSafe("Email"),
            Telefono1 = reader.GetStringSafe("Telefono1"),
            Telefono2 = reader.GetStringSafe("Telefono2")
        };
    }

    public int Insert(Agente agente)   // 05/11/2026 era void
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("spAgenti_Insert", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.Add("@Nome", SqlDbType.NVarChar).Value = agente.Nome;
        cmd.Parameters.Add("@Cognome", SqlDbType.NVarChar).Value = agente.Cognome;

        cmd.Parameters.Add("@PartitaIva", SqlDbType.NVarChar).Value =
            (object?)agente.PartitaIva ?? DBNull.Value;

        cmd.Parameters.Add("@CodiceFiscale", SqlDbType.NVarChar).Value =
            (object?)agente.CodiceFiscale ?? DBNull.Value;

        cmd.Parameters.Add("@PercentualeDefault", SqlDbType.Decimal).Value =
            (object?)agente.PercentualeDefault ?? DBNull.Value;

        cmd.Parameters.Add("@Email", SqlDbType.NVarChar).Value =
            (object?)agente.Email ?? DBNull.Value;

        cmd.Parameters.Add("@Telefono1", SqlDbType.NVarChar).Value =
            (object?)agente.Telefono1 ?? DBNull.Value;

        cmd.Parameters.Add("@Telefono2", SqlDbType.NVarChar).Value =
            (object?)agente.Telefono2 ?? DBNull.Value;

        
        cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value =
            (object?)agente.Utente ?? DBNull.Value;

        try
        {
            conn.Open();

            // ==== MODIFICA 05/11/2026 - INIZIO ====
            // ExecuteScalar legge la prima colonna della prima riga: l'Id restituito dalla stored.
            // Con la stored vecchia il risultato è null: si restituisce 0 e il resto funziona come prima.
            var nuovoId = cmd.ExecuteScalar();
            return nuovoId == null || nuovoId == DBNull.Value ? 0 : Convert.ToInt32(nuovoId);
            // ==== MODIFICA 05/11/2026 - FINE ====
        }
        catch (Exception ex)
        {
            var dump = SqlDebugHelper.DumpParameters(cmd);
            throw new Exception($"Errore SQL in AgenteRepository.Insert\n{dump}", ex);
        }
    }


    public void Update(Agente agente)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("spAgenti_Update", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = agente.IdAgente;

        cmd.Parameters.Add("@Nome", SqlDbType.NVarChar).Value = agente.Nome;
        cmd.Parameters.Add("@Cognome", SqlDbType.NVarChar).Value = agente.Cognome;

        cmd.Parameters.Add("@PartitaIva", SqlDbType.NVarChar).Value =
            (object?)agente.PartitaIva ?? DBNull.Value;

        cmd.Parameters.Add("@CodiceFiscale", SqlDbType.NVarChar).Value =
            (object?)agente.CodiceFiscale ?? DBNull.Value;

        cmd.Parameters.Add("@PercentualeDefault", SqlDbType.Decimal).Value =
            (object?)agente.PercentualeDefault ?? DBNull.Value;

        cmd.Parameters.Add("@Email", SqlDbType.NVarChar).Value =
            (object?)agente.Email ?? DBNull.Value;

        cmd.Parameters.Add("@Telefono1", SqlDbType.NVarChar).Value =
            (object?)agente.Telefono1 ?? DBNull.Value;

        cmd.Parameters.Add("@Telefono2", SqlDbType.NVarChar).Value =
            (object?)agente.Telefono2 ?? DBNull.Value;

        
        cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value =
            (object?)agente.Utente ?? DBNull.Value;

        try
        {
            conn.Open();
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            var dump = SqlDebugHelper.DumpParameters(cmd);
            throw new Exception($"Errore SQL in AgenteRepository.Update\n{dump}", ex);
        }
    }


    public void Delete(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("spAgenti_Delete", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.Add("@IdAgente", SqlDbType.Int).Value = id;

        try
        {
            conn.Open();
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            var dump = SqlDebugHelper.DumpParameters(cmd);
            throw new Exception($"Errore SQL in AgenteRepository.Delete\n{dump}", ex);
        }
    }




    

    public bool HasOrdersOrRelations(int id)//23/10/2026
    {
        const string sql = "SELECT COUNT(*) FROM vAgenteInProspetto WHERE IdAgente = @id";
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@id", SqlDbType.Int).Value = id;

        conn.Open();
        int count = Convert.ToInt32(cmd.ExecuteScalar());
        return count > 0;
    }


}
