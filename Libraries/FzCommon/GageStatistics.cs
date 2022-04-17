using System.Data;
using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public class GageStatistics
    {
        public int LocationId { get; set; }

        // in UTC, but corresponds to beginning-of-day in region time
        public DateTime Date { get; set; }

        public int? AverageBatteryMillivolts { get; set; }
        public double? PercentReadingsReceived { get; set; }
        public double? AverageRssi { get; set; }

        // NOTE: If sensor update interval changes during the course of the day, PercentReadingsReceived
        // will be unreliable.
        public int? SensorUpdateInterval { get; set; }
        public bool? SensorUpdateIntervalChanged { get; set; }

        public GageStatistics()
        {
        }

        public async Task Save(SqlConnection sqlcn)
        {
            SqlCommand cmd = new SqlCommand("SaveGageStatistics", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@LocationId", SqlDbType.Int).Value = this.LocationId;
            cmd.Parameters.Add("@Date", SqlDbType.DateTime).Value = this.Date;
            cmd.Parameters.Add("@AverageBatteryMillivolts", SqlDbType.Int).Value = this.AverageBatteryMillivolts;
            cmd.Parameters.Add("@PercentReadingsReceived", SqlDbType.Float).Value = this.PercentReadingsReceived;
            cmd.Parameters.Add("@AverageRssi", SqlDbType.Float).Value = this.AverageRssi;
            cmd.Parameters.Add("@SensorUpdateInterval", SqlDbType.Int).Value = this.SensorUpdateInterval;
            cmd.Parameters.Add("@SensorUpdateIntervalChanged", SqlDbType.Bit).Value = this.SensorUpdateIntervalChanged;
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<List<GageStatistics>> GetStatisticsForLocation(int locationId)
        {
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                List<GageStatistics> ret = await GetStatisticsForLocation(sqlcn, locationId);
                sqlcn.Close();
                return ret;
            }
        }

        public static async Task<List<GageStatistics>> GetStatisticsForLocation(SqlConnection sqlcn, int locationId)
        {
            SqlCommand cmd = new SqlCommand("GetGageStatistics", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@LocationId", SqlDbType.Int).Value = locationId;
            List<GageStatistics> ret = new List<GageStatistics>();
            try
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        ret.Add(InstantiateFromReader(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "GageStatistics.GetStatisticsForLocation", ex);
                return null;
            }
            return ret;
        }
        
        public static async Task<List<GageStatistics>> GetStatisticsSummary()
        {
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                await sqlcn.OpenAsync();
                List<GageStatistics> ret = await GetStatisticsSummary(sqlcn);
                sqlcn.Close();
                return ret;
            }
        }

        public static async Task<List<GageStatistics>> GetStatisticsSummary(SqlConnection sqlcn)
        {
            SqlCommand cmd = new SqlCommand("GetGageStatisticsSummary", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            List<GageStatistics> ret = new List<GageStatistics>();
            try
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        ret.Add(InstantiateFromReader(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "GageStatistics.GetStatisticsSummary", ex);
                return null;
            }
            return ret;
        }

        public static async Task<GageStatistics> GetLatestStatistics(SqlConnection sqlcn, int locationId)
        {
            SqlCommand cmd = new SqlCommand("GetLatestGageStatistics", sqlcn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@LocationId", SqlDbType.Int).Value = locationId;
            try
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return InstantiateFromReader(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "GageStatistics.GetLatestStatistics", ex);
            }
            return null;
        }
        
        private static GageStatistics InstantiateFromReader(SqlDataReader reader)
        {
            return new GageStatistics()
            {
                LocationId = SqlHelper.Read<int>(reader, "LocationId"),
                Date = SqlHelper.Read<DateTime>(reader, "Date"),
                AverageBatteryMillivolts = SqlHelper.Read<int?>(reader, "AverageBatteryMillivolts"),
                PercentReadingsReceived = SqlHelper.Read<double?>(reader, "PercentReadingsReceived"),
                AverageRssi = SqlHelper.Read<double?>(reader, "AverageRssi"),
                SensorUpdateInterval = SqlHelper.Read<int?>(reader, "SensorUpdateInterval"),
                SensorUpdateIntervalChanged = SqlHelper.Read<bool?>(reader, "SensorUpdateIntervalChanged"),
            };
        }
    }
}
