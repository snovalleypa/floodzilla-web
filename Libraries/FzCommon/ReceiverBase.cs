using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public class ReceiverBase : ILogTaggable
    {
        internal class ReceiverTaggableFactory : ILogBookTaggableFactory
        {
            public async Task<List<ILogTaggable>> GetAvailableTaggables(SqlConnection sqlcn, string category)
            {
                List<ReceiverBase> receivers = await ReceiverBase.GetReceiversAsync(sqlcn);
                List<ILogTaggable> ret = new List<ILogTaggable>();
                foreach (ReceiverBase receiver in receivers)
                {
                    if (!receiver.IsDeleted)
                    {
                        ret.Add(receiver);
                    }
                }
                return ret;
            }
        }

        public const string TagCategory = "recv";

#region ILogTaggable
        public string GetTagCategory() { return ReceiverBase.TagCategory; }
        public string GetTagId() { return this.ReceiverId.ToString(); }
        public string GetTagName() { return "Receiver: " + this.Name; }
#endregion

        public int ReceiverId { get; set; }

        public string ExternalReceiverId { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string? ContactInfo { get; set; }
        public string? LatestIPAddress { get; set; }
        public string? ConnectionInfo { get; set; }
        public string? SimId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsDeleted { get; set; }

        public static ReceiverBase GetReceiverByExternalId(SqlConnection conn, string externalId)
        {
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM Receivers WHERE ExternalReceiverId = '{externalId}'", conn);
            try
            {
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return InstantiateFromReader(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "ReceiverBase.GetReceiverByExternalId", ex);
            }
            return null;
        }

        public static ReceiverBase GetReceiver(SqlConnection conn, int id)
        {
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM Receivers WHERE ReceiverId = '{id}'", conn);
            try
            {
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return InstantiateFromReader(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "ReceiverBase.GetReceiver", ex);
            }
            return null;
        }

        public static ReceiverBase EnsureReceiver(SqlConnection conn, string externalId, string clientIP)
        {
            SqlCommand cmd = new SqlCommand("EnsureReceiver", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@ExternalReceiverId", SqlDbType.VarChar, 70).Value = externalId;
            cmd.Parameters.Add("@LatestIPAddress", SqlDbType.VarChar, 70).Value = clientIP;
            try
            {
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return InstantiateFromReader(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "ReceiverBase.EnsureReceiver", ex);
            }
            return null;
        }

        public static List<ReceiverBase> GetReceivers(SqlConnection conn)
        {
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM Receivers WHERE IsDeleted = 0", conn);
            try
            {
                List<ReceiverBase> ret = new List<ReceiverBase>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ret.Add(InstantiateFromReader(reader));
                    }
                }
                return ret;
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "ReceiverBase.GetReceivers", ex);
            }
            return null;
        }

        public static async Task<List<ReceiverBase>> GetReceiversAsync(SqlConnection conn)
        {
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM Receivers WHERE IsDeleted = 0", conn);
            try
            {
                List<ReceiverBase> ret = new List<ReceiverBase>();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        ret.Add(InstantiateFromReader(reader));
                    }
                }
                return ret;
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "ReceiverBase.GetReceiversAsync", ex);
            }
            return null;
        }

        public async Task Save(SqlConnection sqlConnection)
        {
            SqlCommand cmd = new SqlCommand("SaveReceiver", sqlConnection);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@ReceiverId", SqlDbType.Int).Value = this.ReceiverId;
            cmd.Parameters.Add("@ExternalReceiverId", SqlDbType.VarChar, 70).Value = this.ExternalReceiverId;
            cmd.Parameters.Add("@Name", SqlDbType.NVarChar, 200).Value = this.Name;
            cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 200).Value = this.Description;
            cmd.Parameters.Add("@Location", SqlDbType.NVarChar, 200).Value = this.Location;
            cmd.Parameters.Add("@ContactInfo", SqlDbType.NVarChar, 200).Value = this.ContactInfo;
            cmd.Parameters.Add("@ConnectionInfo", SqlDbType.NVarChar, 200).Value = this.ConnectionInfo;
            cmd.Parameters.Add("@SimId", SqlDbType.NVarChar, 200).Value = this.SimId;
            cmd.Parameters.Add("@Latitude", SqlDbType.Float).Value = this.Latitude;
            cmd.Parameters.Add("@Longitude", SqlDbType.Float).Value = this.Longitude;
            cmd.Parameters.Add("@IsDeleted", SqlDbType.Bit).Value = this.IsDeleted;

            await cmd.ExecuteNonQueryAsync();
        }
        
        public static async Task MarkReceiversAsDeleted(IEnumerable<int> receiverIds)
        {
            StringBuilder sb = new StringBuilder();
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                using (SqlCommand cmd = new SqlCommand("MarkReceiversAsDeleted", sqlcn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    foreach (int id in receiverIds)
                    {
                        if (sb.Length > 180)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@IdList", sb.ToString());
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
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }
        
        private static ReceiverBase InstantiateFromReader(SqlDataReader reader)
        {
            ReceiverBase receiver = new ReceiverBase()
            {
                ReceiverId = SqlHelper.Read<int>(reader, "ReceiverId"),
                ExternalReceiverId = SqlHelper.Read<string>(reader, "ExternalReceiverId"),
                Name = SqlHelper.Read<string>(reader, "Name"),
                Description = SqlHelper.Read<string>(reader, "Description"),
                Location = SqlHelper.Read<string>(reader, "Location"),
                ContactInfo = SqlHelper.Read<string>(reader, "ContactInfo"),
                LatestIPAddress = SqlHelper.Read<string>(reader, "LatestIPAddress"),
                ConnectionInfo = SqlHelper.Read<string>(reader, "ConnectionInfo"),
                SimId = SqlHelper.Read<string>(reader, "SimId"),
                Latitude = SqlHelper.Read<double?>(reader, "Latitude"),
                Longitude = SqlHelper.Read<double?>(reader, "Longitude"),
                IsDeleted = SqlHelper.Read<bool>(reader, "IsDeleted"),
            };
            return receiver;
        }            

        private static string GetColumnList()
        {
            return "ReceiverId, ExternalReceiverId, Name, Description, Location, ContactInfo, LatestIPAddress, ConnectionInfo, SimId, Latitude, Longitude, IsDeleted";
        }
    }
}

