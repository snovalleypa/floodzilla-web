using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace FzCommon
{
    [DebuggerDisplay("{LocationName} ({Id} / {PublicLocationId})")]
    public class SensorLocationBase : ILogTaggable
    {
        //$ TODO: RegionId
        
        internal class SensorLocationTaggableFactory : ILogBookTaggableFactory
        {
            public async Task<List<ILogTaggable>> GetAvailableTaggables(SqlConnection sqlcn, string category)
            {
                List<SensorLocationBase> locations = await SensorLocationBase.GetLocationsForRegionAsync(sqlcn, Constants.SvpaRegionId);
                List<ILogTaggable> ret = new List<ILogTaggable>();
                foreach (SensorLocationBase loc in locations)
                {
                    if (!loc.IsDeleted && !String.IsNullOrEmpty(loc.PublicLocationId))
                    {
                        ret.Add(loc);
                    }
                }
                return ret;
            }
        }
        
        public const string TagCategory = "loc";

#region ILogTaggable
        public string GetTagCategory() { return SensorLocationBase.TagCategory; }
        public string GetTagId() { return this.Id.ToString(); }
        public string GetTagName() { return "Location: " + this.LocationName; }
#endregion

        public SensorLocationBase()
        {
        }

        public SensorLocationBase(SensorLocationBase source)
        {
            this.Id = source.Id;
            this.LocationName = source.LocationName;
            this.ShortName = source.ShortName;
            this.Description = source.Description;
            this.TimeZone = source.TimeZone;
            this.RegionId = source.RegionId;
            this.Latitude = source.Latitude;
            this.Longitude = source.Longitude;
            this.Address = source.Address;
            this.GroundHeight = source.GroundHeight;
            this.Yellow = source.Yellow;
            this.IsActive = source.IsActive;
            this.IsPublic = source.IsPublic;
            this.IsDeleted = source.IsDeleted;
            this.NearPlaces = source.NearPlaces;
            this.Reason = source.Reason;
            this.ContactInfo = source.ContactInfo;
            this.LocationUpdateMinutes = source.LocationUpdateMinutes;
            this.IsOffline = source.IsOffline;
            this.SeaLevel = source.SeaLevel;
            this.PublicLocationId = source.PublicLocationId;
            this.Rank = source.Rank;
            this.MaxChangeThreshold = source.MaxChangeThreshold;
            this.YMin = source.YMin;
            this.YMax = source.YMax;
            this.BenchmarkElevation = source.BenchmarkElevation;
            this.BenchmarkIsProvisional = source.BenchmarkIsProvisional;
            this.BenchmarkDescription = source.BenchmarkDescription;
            this.RelativeSensorHeight = source.RelativeSensorHeight;
            this.Green = source.Green;
            this.Brown = source.Brown;
            this.RoadSaddleHeight = source.RoadSaddleHeight;
            this.RoadDisplayName = source.RoadDisplayName;
        }

        public int Id { get; set; }
        [Required(ErrorMessage ="Name is required"), StringLength(100,ErrorMessage ="Limit exceed.")]
        public string LocationName { get; set; }
        public string? ShortName { get; set; }
        public string? Description { get; set; }
        [Required(ErrorMessage ="Time zone is required.")]
        public string TimeZone { get; set; }
        [Required(ErrorMessage ="Region is required.")]
        public int RegionId { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Address { get; set; }
        public double? Yellow { get; set; }
        public bool IsActive { get; set; }
        public bool IsOffline { get; set; }
        public bool IsPublic { get; set; }
        public bool IsDeleted { get; set; }
        public string? NearPlaces { get; set; }
        public string? Reason { get; set; }
        public string? ContactInfo { get; set; }
        //[Required(ErrorMessage ="Transmition Interval is required.")]
        public int? LocationUpdateMinutes { get; set; }
        public double? SeaLevel { get; set; }

        //$ TODO(daves): Should this be required once all the new locations are in place?
        public string? PublicLocationId { get; set; }

        //[Required(ErrorMessage ="Rank is required.")]
        public double? Rank { get; set; }

        public double? MaxChangeThreshold { get; set; }

#region Benchmark Info
        // Stored as inches above sea level
        public double? BenchmarkElevation { get; set; }

        public bool BenchmarkIsProvisional { get; set; }
        public string? BenchmarkDescription { get; set; }
#endregion

#region Heights Relative to Benchmark
        // Stored as inches relative to 0; converted to feet-relative-to-100 for editing

        public double? RelativeSensorHeight { get; set; }

        public double? Green { get; set; }
        public double? Brown { get; set; }
        public double? YMin { get; set; }
        public double? YMax { get; set; }
        public double? GroundHeight { get; set; }
        public double? RoadSaddleHeight { get; set; }
        public double? MarkerOneHeight { get; set; }
        public double? MarkerTwoHeight { get; set; }
#endregion

#region Discharge settings
        public double? DischargeMin { get; set; }
        public double? DischargeMax { get; set; }
        public double? DischargeStageOne { get; set; }
        public double? DischargeStageTwo { get; set; }
#endregion

        public string? RoadDisplayName { get; set; }
        public string? MarkerOneDescription { get; set; }
        public string? MarkerTwoDescription { get; set; }

        public static SensorLocationBase GetLocation(SqlConnection conn, int locationId)
        {
            SqlCommand cmd = new SqlCommand( $"SELECT {GetColumnList()}"
                                             +$" FROM Locations WHERE Id={locationId}",
                                             conn); 
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
                ErrorManager.ReportException(ErrorSeverity.Major, "SensorLocationBase.GetLocation", ex);
            }
            return null;
        }

        public static async Task<SensorLocationBase> GetLocationAsync(SqlConnection conn, int locationId)
        {
            SqlCommand cmd = new SqlCommand( $"SELECT {GetColumnList()}"
                                             +$" FROM Locations WHERE Id={locationId}",
                                             conn); 
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
                ErrorManager.ReportException(ErrorSeverity.Major, "SensorLocationBase.GetLocation", ex);
            }
            return null;
        }

        public static SensorLocationBase GetLocationByPublicId(SqlConnection conn, string publicLocationId)
        {
            SqlCommand cmd = new SqlCommand( $"SELECT {GetColumnList()}"
                                            +$" FROM Locations WHERE PublicLocationId='{publicLocationId}'",
                                            conn); 
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
                ErrorManager.ReportException(ErrorSeverity.Major, "SensorLocationBase.GetLocationByPublicId", ex);
            }
            return null;
        }

        public static List<SensorLocationBase> GetLocations(SqlConnection conn)
        {
            List<SensorLocationBase> locations = new List<SensorLocationBase>();
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM LOCATIONS", conn);
            try
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        locations.Add(InstantiateFromReader(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "SensorLocationBase.GetLocations", ex);
            }
            return locations;
        }

        public static async Task<List<SensorLocationBase>> GetLocationsAsync(SqlConnection conn)
        {
            List<SensorLocationBase> locations = new List<SensorLocationBase>();
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM LOCATIONS", conn);
            try
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        locations.Add(InstantiateFromReader(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "SensorLocationBase.GetLocationsAsync", ex);
            }
            return locations;
        }
        
        public static List<SensorLocationBase> GetLocationsForRegion(SqlConnection conn, int regionId)
        {
            List<SensorLocationBase> locations = new List<SensorLocationBase>();
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM LOCATIONS WHERE RegionId={regionId}", conn);
            try
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        locations.Add(InstantiateFromReader(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "SensorLocationBase.GetLocationsForRegion", ex);
            }
            return locations;
        }

        public static async Task<List<SensorLocationBase>> GetLocationsForRegionAsync(SqlConnection conn, int regionId)
        {
            List<SensorLocationBase> locations = new List<SensorLocationBase>();
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM LOCATIONS WHERE RegionId={regionId}", conn);
            try
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        locations.Add(InstantiateFromReader(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Major, "SensorLocationBase.GetLocationsForRegionAsync", ex);
            }
            return locations;
        }

        public static async Task UpdateLocationLatLong(SqlConnection conn, int locationId, double latitude, double longitude)
        {
            try
            {
                SqlCommand cmd = new SqlCommand("UpdateLocationLatLong", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@LocationId", SqlDbType.Int).Value = locationId;
                cmd.Parameters.Add("@Latitude", SqlDbType.Float).Value = latitude;
                cmd.Parameters.Add("@Longitude", SqlDbType.Float).Value = longitude;
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                ErrorManager.ReportException(ErrorSeverity.Minor, "SensorLocationBase.UpdateLocationLatLong", ex);
            }
        }

        // Adjusts the passed-in value to take into account BenchmarkElevation and RelativeSensorHeight,
        // if they're supplied
        //$ TODO: Why does this take sensor height into account?
        public double AdjustToInchesAboveSeaLevel(double val)
        {
            if ((this.BenchmarkElevation ?? 0) != 0 && (this.RelativeSensorHeight ?? 0) != 0)
            {
                val = this.BenchmarkElevation.Value + val;
            }
            return val;
        }

        // Adjusts the passed-in value to take into account BenchmarkElevation.  Assumes
        // ConvertValuesForDisplay has been called (so BenchmarkElevation is in feet).  Assumes
        // the passed-in value is directly relative to benchmark (i.e. is not relative to BenchmarkOffsetFeet).
        public double? AdjustToFeetAboveSeaLevel(double? val)
        {
            if (!val.HasValue || !this.BenchmarkElevation.HasValue)
            {
                return val;
            }
            return val.Value + this.BenchmarkElevation.Value;
        }

        // These fields are presented for editing as feet.  Everything except BenchmarkElevation
        // is relative to FzCommon.Constants.BenchmarkOffsetFeet.
        public void ConvertValuesForEditing()
        {
            this.SeaLevel = FzCommonUtility.GetRoundValue(this.SeaLevel / 12);
            this.BenchmarkElevation = FzCommonUtility.GetRoundValue(this.BenchmarkElevation / 12.0);
            this.RelativeSensorHeight = FzCommonUtility.GetRoundValue((this.RelativeSensorHeight / 12.0) + FzCommon.Constants.BenchmarkOffsetFeet);
            this.Green = FzCommonUtility.GetRoundValue((this.Green / 12.0) + FzCommon.Constants.BenchmarkOffsetFeet);
            this.Brown = FzCommonUtility.GetRoundValue((this.Brown / 12.0) + FzCommon.Constants.BenchmarkOffsetFeet);
            this.YMin = FzCommonUtility.GetRoundValue((this.YMin / 12.0) + FzCommon.Constants.BenchmarkOffsetFeet);
            this.YMax = FzCommonUtility.GetRoundValue((this.YMax / 12.0) + FzCommon.Constants.BenchmarkOffsetFeet);
            this.GroundHeight = FzCommonUtility.GetRoundValue((this.GroundHeight / 12.0) + FzCommon.Constants.BenchmarkOffsetFeet);
            this.RoadSaddleHeight = FzCommonUtility.GetRoundValue((this.RoadSaddleHeight / 12.0) + FzCommon.Constants.BenchmarkOffsetFeet);
            this.MarkerOneHeight = FzCommonUtility.GetRoundValue((this.MarkerOneHeight / 12.0) + FzCommon.Constants.BenchmarkOffsetFeet);
            this.MarkerTwoHeight = FzCommonUtility.GetRoundValue((this.MarkerTwoHeight / 12.0) + FzCommon.Constants.BenchmarkOffsetFeet);
        }

        // These fields are presented for display as feet above sea level
        public void ConvertValuesForDisplay()
        {
            this.SeaLevel = FzCommonUtility.GetRoundValue(this.SeaLevel / 12);
            this.BenchmarkElevation = FzCommonUtility.GetRoundValue(this.BenchmarkElevation / 12.0);
            this.RelativeSensorHeight = FzCommonUtility.GetRoundValue((this.RelativeSensorHeight / 12.0) + this.BenchmarkElevation);
            this.Green = FzCommonUtility.GetRoundValue((this.Green / 12.0) + this.BenchmarkElevation);
            this.Brown = FzCommonUtility.GetRoundValue((this.Brown / 12.0) + this.BenchmarkElevation);
            this.YMin = FzCommonUtility.GetRoundValue((this.YMin / 12.0) + this.BenchmarkElevation);
            this.YMax = FzCommonUtility.GetRoundValue((this.YMax / 12.0) + this.BenchmarkElevation);
            this.GroundHeight = FzCommonUtility.GetRoundValue((this.GroundHeight / 12.0) + this.BenchmarkElevation);
            this.RoadSaddleHeight = FzCommonUtility.GetRoundValue((this.RoadSaddleHeight / 12.0) + this.BenchmarkElevation);
            this.MarkerOneHeight = FzCommonUtility.GetRoundValue((this.MarkerOneHeight / 12.0) + this.BenchmarkElevation);
            this.MarkerTwoHeight = FzCommonUtility.GetRoundValue((this.MarkerTwoHeight / 12.0) + this.BenchmarkElevation);
        }

        public void ConvertValuesForStorage()
        {
            this.SeaLevel = FzCommonUtility.GetRoundValue(this.SeaLevel * 12);
            this.BenchmarkElevation = FzCommonUtility.GetRoundValue(this.BenchmarkElevation * 12.0);
            this.RelativeSensorHeight = FzCommonUtility.GetRoundValue((this.RelativeSensorHeight - FzCommon.Constants.BenchmarkOffsetFeet) * 12.0);
            this.Green = FzCommonUtility.GetRoundValue((this.Green - FzCommon.Constants.BenchmarkOffsetFeet) * 12.0);
            this.Brown = FzCommonUtility.GetRoundValue((this.Brown - FzCommon.Constants.BenchmarkOffsetFeet) * 12.0);
            this.YMin = FzCommonUtility.GetRoundValue((this.YMin - FzCommon.Constants.BenchmarkOffsetFeet) * 12.0);
            this.YMax = FzCommonUtility.GetRoundValue((this.YMax - FzCommon.Constants.BenchmarkOffsetFeet) * 12.0);
            this.GroundHeight = FzCommonUtility.GetRoundValue((this.GroundHeight - FzCommon.Constants.BenchmarkOffsetFeet) * 12.0);
            this.RoadSaddleHeight = FzCommonUtility.GetRoundValue((this.RoadSaddleHeight - FzCommon.Constants.BenchmarkOffsetFeet) * 12.0);
            this.MarkerOneHeight = FzCommonUtility.GetRoundValue((this.MarkerOneHeight - FzCommon.Constants.BenchmarkOffsetFeet) * 12.0);
            this.MarkerTwoHeight = FzCommonUtility.GetRoundValue((this.MarkerTwoHeight - FzCommon.Constants.BenchmarkOffsetFeet) * 12.0);
        }

        private static string GetColumnList()
        {
            return "Id,LocationName,ShortName,Description,TimeZone,RegionId,Latitude,Longitude,"
                    +"Address,GroundHeight,Green,Brown,Yellow,IsActive,IsPublic,IsDeleted,"
                    +"NearPlaces,Reason,ContactInfo,LocationUpdateMinutes,IsOffline,SeaLevel,"
                    +"Rank,MaxChangeThreshold,YMin,YMax,PublicLocationId,BenchmarkElevation,BenchmarkIsProvisional,"
                    +"BenchmarkDescription,RelativeSensorHeight,RoadSaddleHeight,RoadDisplayName,"
                    +"MarkerOneHeight,MarkerOneDescription,MarkerTwoHeight,MarkerTwoDescription,"
                    +"DischargeMin,DischargeMax,DischargeStageOne,DischargeStageTwo";
        }

        private static SensorLocationBase InstantiateFromReader(SqlDataReader reader)
        {
            return new SensorLocationBase()
            {
                Id = SqlHelper.Read<int>(reader, "Id"),
                LocationName = SqlHelper.Read<string>(reader, "LocationName"),
                ShortName = SqlHelper.Read<string>(reader, "ShortName"),
                Description = SqlHelper.Read<string>(reader, "Description"),
                TimeZone = SqlHelper.Read<string>(reader, "TimeZone"),
                RegionId = SqlHelper.Read<int>(reader, "RegionId"),
                Latitude = SqlHelper.Read<double?>(reader, "Latitude"),
                Longitude = SqlHelper.Read<double?>(reader, "Longitude"),
                Address = SqlHelper.Read<string>(reader, "Address"),
                GroundHeight = SqlHelper.Read<double?>(reader, "GroundHeight"),
                Green = SqlHelper.Read<double?>(reader, "Green"),
                Brown = SqlHelper.Read<double?>(reader, "Brown"),
                Yellow = SqlHelper.Read<double?>(reader, "Yellow"),
                IsActive = SqlHelper.Read<bool>(reader, "IsActive"),
                IsPublic = SqlHelper.Read<bool>(reader, "IsPublic"),
                IsDeleted = SqlHelper.Read<bool>(reader, "IsDeleted"),
                NearPlaces = SqlHelper.Read<string>(reader, "NearPlaces"),
                Reason = SqlHelper.Read<string>(reader, "Reason"),
                ContactInfo = SqlHelper.Read<string>(reader, "ContactInfo"),
                LocationUpdateMinutes = SqlHelper.Read<int?>(reader, "LocationUpdateMinutes"),
                IsOffline = SqlHelper.Read<bool>(reader, "IsOffline"),
                SeaLevel = SqlHelper.Read<double?>(reader, "SeaLevel"),
                Rank = SqlHelper.Read<double?>(reader, "Rank"),
                MaxChangeThreshold = SqlHelper.Read<double?>(reader, "MaxChangeThreshold"),
                YMin = SqlHelper.Read<double?>(reader, "YMin"),
                YMax = SqlHelper.Read<double?>(reader, "YMax"),
                PublicLocationId = SqlHelper.Read<string>(reader, "PublicLocationId"),
                BenchmarkElevation = SqlHelper.Read<double?>(reader, "BenchmarkElevation"),
                BenchmarkIsProvisional = SqlHelper.Read<bool>(reader, "BenchmarkIsProvisional"),
                BenchmarkDescription = SqlHelper.Read<string>(reader, "BenchmarkDescription"),
                RelativeSensorHeight = SqlHelper.Read<double?>(reader, "RelativeSensorHeight"),
                RoadSaddleHeight = SqlHelper.Read<double?>(reader, "RoadSaddleHeight"),
                RoadDisplayName = SqlHelper.Read<string>(reader, "RoadDisplayName"),
                MarkerOneHeight = SqlHelper.Read<double?>(reader, "MarkerOneHeight"),
                MarkerOneDescription = SqlHelper.Read<string>(reader, "MarkerOneDescription"),
                MarkerTwoHeight = SqlHelper.Read<double?>(reader, "MarkerTwoHeight"),
                MarkerTwoDescription = SqlHelper.Read<string>(reader, "MarkerTwoDescription"),
                DischargeMin = SqlHelper.Read<double?>(reader, "DischargeMin"),
                DischargeMax = SqlHelper.Read<double?>(reader, "DischargeMax"),
                DischargeStageOne = SqlHelper.Read<double?>(reader, "DischargeStageOne"),
                DischargeStageTwo = SqlHelper.Read<double?>(reader, "DischargeStageTwo"),
            };
        }

        public static async Task MarkLocationsAsUndeleted(SqlConnection conn, IEnumerable<int> locationIds)
        {
            await SqlHelper.CallIdListProcedure(conn, "MarkLocationsAsUndeleted", locationIds, 180);
        }
    }
}
