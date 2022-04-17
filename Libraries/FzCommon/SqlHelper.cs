using System.Data;
using System.Data.Common;
using System.Text;
using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public static class SqlHelper
    {
        public static T Read<T>(DbDataReader DataReader, string FieldName)
        {
            int FieldIndex;
            try
            {
                FieldIndex = DataReader.GetOrdinal(FieldName);
            }
            catch
            {
                return default(T);
            }

            if (DataReader.IsDBNull(FieldIndex))
            {
                return default(T);
            }
            else
            {
                object readData = DataReader.GetValue(FieldIndex);
                if (readData is T)
                {
                    return (T)readData;
                }
                else
                {
                    try
                    {
                        return (T)Convert.ChangeType(readData, typeof(T));
                    }
                    catch (InvalidCastException)
                    {
                        return default(T);
                    }
                }
            }
        }

        public static async Task CallIdListProcedure(SqlConnection conn, string procName, IEnumerable<int> ids, int maxLength)
        {
            await CallIdListProcedureWithReason(conn, procName, ids, maxLength, null);
        }

        public static async Task CallIdListProcedureWithReason(SqlConnection conn, string procName, IEnumerable<int> ids, int maxLength, string reason)
        {
            StringBuilder sb = new StringBuilder();
            using (SqlCommand cmd = new SqlCommand(procName, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                foreach (int id in ids)
                {
                    if (sb.Length > maxLength)
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@IdList", sb.ToString());
                        if (reason != null)
                        {
                            cmd.Parameters.AddWithValue("@Reason", reason);
                        }
                        await cmd.ExecuteNonQueryAsync();
                        sb.Clear();
                    }
                    if (sb.Length > 0)
                    {
                        sb.Append(",");
                    }
                    sb.Append(id.ToString());
                }
                if (sb.Length > 0)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@IdList", sb.ToString());
                    if (reason != null)
                    {
                        cmd.Parameters.AddWithValue("@Reason", reason);
                    }
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
