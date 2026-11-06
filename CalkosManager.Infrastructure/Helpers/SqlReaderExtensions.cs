using System.Data;
using Microsoft.Data.SqlClient;

namespace CalkosManager.Infrastructure.Helpers
{
    public static class SqlReaderExtensions
    {
        /// <summary>
        /// Restituisce l'indice della colonna specificata senza generare eccezioni.
        /// Ritorna -1 se la colonna non è presente nel result set.
        /// </summary>
        private static int GetColumnIndexSafe(this SqlDataReader reader, string columnName)
        {
            try
            {
                return reader.GetOrdinal(columnName);
            }
            catch (IndexOutOfRangeException)
            {
                return -1; // Colonna non trovata nella SELECT
            }
        }

        /// <summary>
        /// Legge un intero non nullable. Ritorna 0 se la colonna è assente o NULL.
        /// </summary>
        public static int GetIntSafe(this SqlDataReader reader, string columnName)
        {
            int index = reader.GetColumnIndexSafe(columnName);
            if (index == -1 || reader.IsDBNull(index)) return 0;
            return reader.GetInt32(index);
        }

        /// <summary>
        /// Legge una stringa. Ritorna string.Empty se la colonna è assente o NULL.
        /// </summary>
        public static string GetStringSafe(this SqlDataReader reader, string columnName)
        {
            int index = reader.GetColumnIndexSafe(columnName);
            if (index == -1 || reader.IsDBNull(index)) return string.Empty;
            return reader.GetString(index);
        }

        /// <summary>
        /// Legge un decimal non nullable. Ritorna 0m se la colonna è assente o NULL.
        /// </summary>
        public static decimal GetDecimalSafe(this SqlDataReader reader, string columnName)
        {
            int index = reader.GetColumnIndexSafe(columnName);
            if (index == -1 || reader.IsDBNull(index)) return 0m;
            return reader.GetDecimal(index);
        }

        /// <summary>
        /// Legge un DateTime non nullable. Ritorna DateTime.MinValue se la colonna è assente o NULL.
        /// </summary>
        public static DateTime GetDateTimeSafe(this SqlDataReader reader, string columnName)
        {
            int index = reader.GetColumnIndexSafe(columnName);
            if (index == -1 || reader.IsDBNull(index)) return DateTime.MinValue;
            return reader.GetDateTime(index);
        }

        /// <summary>
        /// Legge un intero nullable. Ritorna null se la colonna è assente o NULL.
        /// </summary>
        public static int? GetNullableInt(this SqlDataReader reader, string columnName)
        {
            int index = reader.GetColumnIndexSafe(columnName);
            if (index == -1 || reader.IsDBNull(index)) return null;
            return reader.GetInt32(index);
        }

        /// <summary>
        /// Legge un DateTime nullable. Ritorna null se la colonna è assente o NULL.
        /// </summary>
        public static DateTime? GetNullableDateTime(this SqlDataReader reader, string columnName)
        {
            int index = reader.GetColumnIndexSafe(columnName);
            if (index == -1 || reader.IsDBNull(index)) return null;
            return reader.GetDateTime(index);
        }

        /// <summary>
        /// Legge un decimal nullable. Ritorna null se la colonna è assente o NULL.
        /// </summary>
        public static decimal? GetNullableDecimal(this SqlDataReader reader, string columnName)
        {
            int index = reader.GetColumnIndexSafe(columnName);
            if (index == -1 || reader.IsDBNull(index)) return null;
            return reader.GetDecimal(index);
        }

        /// <summary>
        /// Legge un boolean non nullable.
        /// Ritorna false se la colonna è assente o NULL.
        /// </summary>
        public static bool GetBoolSafe(this SqlDataReader reader, string columnName) // GetBoolSafe = default false
        {
            int index = reader.GetColumnIndexSafe(columnName);
            if (index == -1 || reader.IsDBNull(index)) return false;
            return reader.GetBoolean(index);
        }

        /// <summary>
        /// Legge un boolean nullable.
        /// Ritorna null se la colonna è assente o NULL.
        /// </summary>
        public static bool? GetNullableBool(this SqlDataReader reader, string columnName) // GetNullableBool = default null
        {
            int index = reader.GetColumnIndexSafe(columnName);
            if (index == -1 || reader.IsDBNull(index)) return null;
            return reader.GetBoolean(index);
        }

        /// <summary>
        /// Legge un double non nullable. Ritorna 0d se la colonna è assente o NULL.
        /// </summary>
        public static double GetDoubleSafe(this SqlDataReader reader, string columnName)
        {
            int index = reader.GetColumnIndexSafe(columnName);
            if (index == -1 || reader.IsDBNull(index)) return 0d;
            return reader.GetDouble(index);
        }

        /// <summary>
        /// Legge un Guid non nullable. Ritorna Guid.Empty se la colonna è assente o NULL.
        /// </summary>
        public static Guid GetGuidSafe(this SqlDataReader reader, string columnName)
        {
            int index = reader.GetColumnIndexSafe(columnName);
            if (index == -1 || reader.IsDBNull(index)) return Guid.Empty;
            return reader.GetGuid(index);
        }
    }
}
