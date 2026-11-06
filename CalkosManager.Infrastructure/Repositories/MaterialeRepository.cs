using System.Data;
using Microsoft.Data.SqlClient;
using CalkosManager.Domain.Entities;
using CalkosManager.Domain.Interfaces.Repositories;
using CalkosManager.Infrastructure.Helpers;

namespace CalkosManager.Infrastructure.Repositories
{
    /*
        ============================================================
        MATERIALE REPOSITORY — PROMEMORIA ARCHITETTURALE
        ============================================================

        Questo repository usa STORED PROCEDURE perché:

        ✔ Materiale è un'entità di business modificabile
        ✔ Serve coerenza con Agenti, Clienti, ecc.
        ✔ Insert può essere usato anche in transazione (import COBRAL)
        ✔ Stored evitano duplicazione della logica SQL
        ✔ Parametri tipizzati → niente AddWithValue → niente conversioni implicite

        ============================================================
    */

    public class MaterialeRepository : IMaterialeRepository
    {
        private readonly string _connectionString;

        public MaterialeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Restituisce un materiale tramite il codice univoco.
        /// Serve per recuperare rapidamente un materiale noto.
        /// </summary>
        /// <param name="codiceMateriale">Codice univoco del materiale.</param>
        /// <returns>Entità Materiale se trovata, altrimenti null.</returns>
        public Materiale? GetByCodice(string codiceMateriale)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spMateriale_GetByCodice", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@CodiceMateriale", SqlDbType.VarChar).Value = codiceMateriale;

            conn.Open();
            using var reader = cmd.ExecuteReader();

            return reader.Read() ? Map(reader) : null;
        }
        /// <summary>
        /// Restituisce un materiale tramite ID.
        /// Usa la stored procedure spMateriale_GetById.
        /// </summary>
        public Materiale? GetById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spMateriale_GetById", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdMateriale", SqlDbType.Int).Value = id;

            conn.Open();
            using var reader = cmd.ExecuteReader();

            return reader.Read() ? Map(reader) : null;
        }

        /// <summary>
        /// Restituisce un materiale tramite la descrizione esatta.
        /// Utile per lookup da UI o controlli di esistenza.
        /// </summary>
        /// <param name="descrizioneMateriale">Descrizione completa del materiale.</param>
        /// <returns>Entità Materiale se trovata, altrimenti null.</returns>
        public Materiale? GetByDescrizione(string descrizioneMateriale)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spMateriale_GetByDescrizione", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@DescrizioneMateriale", SqlDbType.VarChar).Value = descrizioneMateriale;

            conn.Open();
            using var reader = cmd.ExecuteReader();

            return reader.Read() ? Map(reader) : null;
        }

        /// <summary>
        /// Restituisce tutti i materiali presenti in tabella.
        /// Serve per popolare liste e dropdown in interfaccia.
        /// </summary>
        /// <returns>Enumerabile di Materiale.</returns>
        public IEnumerable<Materiale> GetAll()
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spMateriali_GetAll", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            conn.Open();
            using var reader = cmd.ExecuteReader();

            var list = new List<Materiale>();

            while (reader.Read())
            {
                list.Add(Map(reader));
            }

            return list;
        }

        /// <summary>
        /// Inserisce un nuovo materiale aprendo una transazione interna.
        /// Serve per inserimenti standard non legati a flussi batch.
        /// </summary>
        /// <param name="materiale">Entità Materiale da inserire.</param>
        /// <returns>Entità Materiale con Id valorizzato.</returns>
        public Materiale Insert(Materiale materiale)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var tran = conn.BeginTransaction();

            try
            {
                var result = Insert(materiale, conn, tran);
                tran.Commit();
                return result;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Inserisce un nuovo materiale utilizzando una connessione e transazione esterne.
        /// Usato nei flussi di importazione (es. COBRAL) per mantenere atomicità.
        /// </summary>
        /// <param name="materiale">Entità Materiale da inserire.</param>
        /// <param name="connection">Connessione SQL esterna.</param>
        /// <param name="transaction">Transazione SQL esterna.</param>
        /// <returns>Entità Materiale con Id valorizzato.</returns>
        public Materiale Insert(Materiale materiale, IDbConnection connection, IDbTransaction transaction)
        {
            var conn = (SqlConnection)connection;
            var tran = (SqlTransaction)transaction;

            using var cmd = new SqlCommand("spMateriale_Insert", conn, tran)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@CodiceMateriale", SqlDbType.VarChar).Value = materiale.CodiceMateriale;
            cmd.Parameters.Add("@DescrizioneMateriale", SqlDbType.VarChar).Value = materiale.DescrizioneMateriale;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = (object?)materiale.Utente ?? DBNull.Value;

            try
            {
                var id = Convert.ToInt32(cmd.ExecuteScalar());
                materiale.IdMateriale = id;
                return materiale;
            }
            catch (Exception ex)
            {
                var dump = SqlDebugHelper.DumpParameters(cmd);
                throw new Exception($"Errore SQL in MaterialeRepository.Insert\n{dump}", ex);
            }
        }

        /// <summary>
        /// Aggiorna i dati di un materiale esistente.
        /// Serve per modifiche da interfaccia amministrativa.
        /// </summary>
        /// <param name="materiale">Entità Materiale con i nuovi valori.</param>
        public void Update(Materiale materiale)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spMateriale_Update", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@IdMateriale", SqlDbType.Int).Value = materiale.IdMateriale;
            cmd.Parameters.Add("@CodiceMateriale", SqlDbType.VarChar).Value = materiale.CodiceMateriale;
            cmd.Parameters.Add("@DescrizioneMateriale", SqlDbType.VarChar).Value = materiale.DescrizioneMateriale;
            cmd.Parameters.Add("@Utente", SqlDbType.NVarChar).Value = (object?)materiale.Utente ?? DBNull.Value;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                var dump = SqlDebugHelper.DumpParameters(cmd);
                throw new Exception($"Errore SQL in MaterialeRepository.Update\n{dump}", ex);
            }
        }

        /// <summary>
        /// Elimina un materiale tramite ID.
        /// Usa la stored di cancellazione logica/fisica prevista per i materiali.
        /// </summary>
        /// <param name="id">ID univoco del materiale da eliminare.</param>
        public void Delete(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("spMateriali_Delete", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;

            try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                var dump = SqlDebugHelper.DumpParameters(cmd);
                throw new Exception($"Errore SQL in MaterialeRepository.Delete\n{dump}", ex);
            }
        }

        /// <summary>
        /// Mappa una riga del DataReader nell'entità Materiale.
        /// Centralizza il mapping per coerenza e riuso.
        /// </summary>
        /// <param name="reader">SqlDataReader già posizionato sulla riga corrente.</param>
        /// <returns>Istanza di Materiale popolata con i dati letti.</returns>
        private Materiale Map(SqlDataReader reader)
        {
            // 1. Istanza vuota
            var materiale = new Materiale();

            // 2. Assegnazioni dirette (per un debug riga per riga preciso)
            materiale.IdMateriale = reader.GetIntSafe("IdMateriale");
            materiale.CodiceMateriale = reader.GetStringSafe("CodiceMateriale");
            materiale.DescrizioneMateriale = reader.GetStringSafe("DescrizioneMateriale");

            // Se la classe Materiale ha anche il campo Utente, aggiungilo qui:
            // materiale.Utente = reader.GetStringSafe("Utente");

            // 3. Gestione sicura e atomica delle date
            try
            {
                int ordIns = reader.GetOrdinal("DataInserimento");
                materiale.DataInserimento = reader.GetDateTime(ordIns);
            }
            catch
            {
                /* Colonna non presente nella query SQL */
            }

            try
            {
                int ordMod = reader.GetOrdinal("DataModifica");
                if (!reader.IsDBNull(ordMod))
                {
                    materiale.DataModifica = reader.GetDateTime(ordMod);
                }
            }
            catch
            {
                /* Colonna non presente nella query SQL */
            }

            return materiale;
        }
    }
}
