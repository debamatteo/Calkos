using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;

namespace CalkosManager.Infrastructure.Helpers
{

    /// <summary>
    /// helper di debug progettato per ispezionare rapidamente tutti i parametri di un SqlCommand quando qualcosa va storto durante un INSERT, UPDATE o DELETE.
    /// Utile soprattutto durante lo sviluppo, per capire se i parametri sono corretti o se c'è qualche valore anomalo (es. NULL, stringhe vuote, tipi errati).
    /// </summary>
    /// 
    // 
    //usarla In ogni repository, dentro il catch:
    //csharp
    //try
    //{
    //    cmd.ExecuteNonQuery();
    //}
    //catch (Exception ex)
    //{
    //    var dump = SqlDebugHelper.DumpParameters(cmd);
    //throw new Exception($"Errore durante InsertRange ImportCobral.\n{dump}", ex);
    //}
    public static class SqlDebugHelper
    {
        public static string DumpParameters(SqlCommand cmd)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Parametri SQL:");

            foreach (SqlParameter p in cmd.Parameters)
            {
                sb.AppendLine(
                    $"{p.ParameterName} = {p.Value ?? "NULL"} " +
                    $"(C#: {p.Value?.GetType().Name ?? "null"}, SQL: {p.SqlDbType})"
                );
            }

            return sb.ToString();
        }


 
    }
}
