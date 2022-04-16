using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using FzCommon;

namespace FloodzillaJob
{
    public class UsgsGageReading
    {
        public long id { get; set; }
        public double? AvgL { get; set; }
        public double? GroundHeight { get; set; }
        public double? WaterHeight{ get; set; }
        public double? UsgsGageHeight { get; set; }

        public int HeaderId { get; set; }
        public int LocationId { get; set; }
        public int DeviceId { get; set; }
        public int? RegionId { get; set; }
        public DateTime CreatedOn { get; set; }

        public int? CalibrationId { get; set; }
        public double? CalcWaterDischarge { get; set; }
        public double? ReportedWaterDischarge { get; set; }
    }

    public class UsgsDataSource
    {

        //$ TODO: remove this
        private const short TOP_CALC_ROWS = 500;

        public static async Task CalculateUsgsLevels(ILogger log)
        {
            JobRunLog runLog = new JobRunLog("FloodzillaJob.UsgsDataSource.CalculateUsgsLevels");

            try
            {
                List<UsgsGageReading> usgsReadings = GetUsgsLevelData();
                if (usgsReadings.Count > 0)
                {
                    await UpdateUsgsLevels(usgsReadings, true);
                }
                
                string summary = String.Format("Saved {0} USGS readings", usgsReadings.Count);
                
                runLog.Summary = summary;
                runLog.ReportJobRunSuccess();
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "UsgsDataSource.CalculateUsgsLevels", ex);
                runLog.ReportJobRunException(ex);
                throw;
            }
        }

#if DEBUG
        public static async Task TestUsgsCalculate(ILogger log)
        {
            //$ TODO: Make these configurable and/or move into an actual test framework

            List<UsgsGageReading> readings = new List<UsgsGageReading>();

            UsgsGageReading reading = new UsgsGageReading()
            {
                id = 999999,
                GroundHeight = 528,
                UsgsGageHeight = 47.3,
                ReportedWaterDischarge = 6540,
                HeaderId = 888888,
                LocationId = 99,
                CreatedOn = DateTime.UtcNow,
                DeviceId = 50,
                CalibrationId = 6,
                RegionId = 1,
            };

            reading.AvgL = reading.GroundHeight - (reading.UsgsGageHeight * 12);
            reading.WaterHeight = reading.GroundHeight - reading.AvgL;
            if (reading.CalibrationId != 0)
            {
                double[] discharge = CurveFitting.GetDisharges((int)reading.CalibrationId, new double[] { ((double)reading.WaterHeight) / 12.0 });
                if (discharge != null && discharge.Length > 0) reading.CalcWaterDischarge = discharge[0];
            }
            readings.Add(reading);
            try
            {
                await UpdateUsgsLevels(readings, false);
            }
            catch (Exception /*ex*/)
            {
                //$
            }
        }
#endif

        private static List<UsgsGageReading> GetUsgsLevelData()
        {
            List<UsgsGageReading> usgsReadings = new List<UsgsGageReading>();
            try
            {
                using (SqlCommand cmd = new SqlCommand( $"SELECT TOP {TOP_CALC_ROWS} f.id, f.DeviceId, f.CreatedOn, f.HeaderId, f.LocationId, l.GroundHeight, l.RegionId, ud.GageHeight, ud.SteamFlow, c.CalibrationId FROM FzLevel f "
                                                       +$"INNER JOIN Locations l ON f.LocationId = l.Id "
                                                       +$"INNER JOIN Headers h ON f.HeaderId = h.Id "
                                                       +$"INNER JOIN UsgsData ud ON h.UsgsDataId = ud.UsgsDataId "
                                                       +$"OUTER APPLY (SELECT TOP 1 CalibrationId FROM Calibrations WHERE LocationId = h.LocationId AND IsDefault = 1) c "
                                                       +$"WHERE f.Iteration = 6 AND l.GroundHeight >= 0 AND f.id > (SELECT CAST(PValue AS bigint) FROM Properties WHERE PName = 'LastCalcId6') ORDER BY f.id ASC",
                                                        new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString])))
                {
                    cmd.Connection.Open();
                    using (SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (dr.Read())
                        {
                            UsgsGageReading f = new UsgsGageReading
                            {
                                id = (long)dr["id"],
                                GroundHeight = (dr["GroundHeight"] != null && dr["GroundHeight"] != DBNull.Value) ? (double?)dr["GroundHeight"] : null,
                                UsgsGageHeight = (dr["GageHeight"] != null && dr["GageHeight"] != DBNull.Value) ? (double?)dr["GageHeight"] : null,
                                ReportedWaterDischarge = (dr["SteamFlow"] != null && dr["SteamFlow"] != DBNull.Value) ? (double?)dr["SteamFlow"] : null,
                                HeaderId = (int)dr["HeaderId"],
                                LocationId = (int)dr["LocationId"],
                                CreatedOn = (DateTime)dr["CreatedOn"],
                                DeviceId = (int)dr["DeviceId"],
                                CalibrationId = (dr["CalibrationId"] != null && dr["CalibrationId"] != DBNull.Value) ? (int?)dr["CalibrationId"] : null,
                                RegionId = (dr["RegionId"] != null && dr["RegionId"] != DBNull.Value) ? (int?)dr["RegionId"] : null
                            };

                            f.AvgL = (f.GroundHeight != null && f.UsgsGageHeight != null) ? f.GroundHeight - (f.UsgsGageHeight * 12.0) : null;

                            f.WaterHeight = (f.GroundHeight != null && f.AvgL != null) ? f.GroundHeight - f.AvgL : null;

                            if (f.CalibrationId != null && f.WaterHeight != null)
                            {
                                double[] discharge = CurveFitting.GetDisharges((int)f.CalibrationId, new double[] { ((double)f.WaterHeight) / 12.0 });
                                if (discharge != null && discharge.Length > 0) f.CalcWaterDischarge = discharge[0];
                            }

                            usgsReadings.Add(f);
                        }
                        dr.Close();
                    }
                }
            }
            catch (Exception /*ex*/)
            {
                //$
            }
            return usgsReadings;
        }

        private static async Task UpdateUsgsLevels(List<UsgsGageReading> fhlevels, bool updateLastCalcId)
        {
            try
            {
                using (SqlConnection sqlcn =  new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
                {
                    SqlCommand cmd = new SqlCommand(string.Empty, sqlcn);
                    sqlcn.Open();

                    foreach (UsgsGageReading fl in fhlevels)
                    {
                        try
                        {
                            if (fl.AvgL != null && fl.GroundHeight != null && fl.WaterHeight != null)
                            {
                                if (updateLastCalcId)
                                {
                                    //$ TODO: Remove this and set LastCalcId6 separately at the end
                                    //$ TODO: Also, remove LastCalcId6 and do this whole thing differently
                                    cmd.CommandText = $"UPDATE FzLevel SET AvgL = {fl.AvgL}, WaterHeight = {fl.WaterHeight} {((fl.AvgL == 0) ? ", IsDeleted = 1" : string.Empty)} {((fl.CalcWaterDischarge != null) ? $", WaterDischarge = {fl.CalcWaterDischarge}" : string.Empty)} {((fl.CalibrationId != null) ? $", CalibrationId = {fl.CalibrationId}" : string.Empty)} , ModifiedOn = getutcdate() WHERE id = {fl.id}; "
                                        + $"UPDATE Headers SET GroundHeight = {fl.GroundHeight}, ModifiedOn = getutcdate() WHERE Id = {fl.HeaderId}; "
                                        + $"UPDATE Properties SET PValue = {fl.id} WHERE PName = 'LastCalcId6';";
                                    cmd.ExecuteNonQuery();
                                }

                                SensorReading reading = CreateUsgsSensorReading(fl);
                                await reading.Save(sqlcn);
                            }
                        }
                        catch (Exception /*ex2*/)
                        {
                            //$
                            if (cmd.Connection.State == ConnectionState.Closed || cmd.Connection.State == ConnectionState.Broken)
                            {
                                throw;
                            }
                        }
                    }

                    sqlcn.Close();
                }
            }
            catch (Exception /*ex*/)
            {
                //$
            }
        }

        private static SensorReading CreateUsgsSensorReading(UsgsGageReading fl)
        {
            SensorReading reading = new SensorReading()
            {
                ListenerInfo = "FloodzillaJob.UsgsDataSource, 12/2019, " + Environment.MachineName,
                Timestamp = fl.CreatedOn,
                LocationId = fl.LocationId,
                DeviceId = fl.DeviceId,
                GroundHeight = fl.GroundHeight,
                DistanceReading = fl.AvgL,
                WaterHeight = fl.WaterHeight,
                CalcWaterDischarge = fl.CalcWaterDischarge,
                WaterDischarge = fl.ReportedWaterDischarge
            };

            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                sqlcn.Open();
                if (reading.LocationId.HasValue)
                {
                    reading.AdjustReadingForLocation(sqlcn, reading.LocationId.Value);
                }
                if (reading.DeviceId.HasValue)
                {
                    reading.AdjustReadingForDevice(sqlcn, reading.DeviceId.Value);
                }
                sqlcn.Close();
            }

            reading.RawSensorData = new UsgsRawSensorData()
            {
                CalibrationId = fl.CalibrationId
            };

            return reading;
        }
    }
}
