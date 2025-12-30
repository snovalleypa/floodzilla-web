using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public class DeviceTypeIds
    {
        public const int Svpa = 1;
        public const int Usgs = 2;
        public const int Virtual = 3;
        public const int Senix = 4;
        public const int UsgsTestingDevice = 5;
        public const int Milesight = 6;
        public const int Dragino = 7;
    }
    
    public class DeviceType
    {
        [Required]
        public int DeviceTypeId { get; set; }
        public string DeviceTypeName { get; set; }

        public static List<DeviceType> GetDeviceTypes(SqlConnection conn)
        {
            SqlCommand cmd = new SqlCommand($"SELECT * FROM DeviceTypes", conn);
            try
            {
                List<DeviceType> ret = new List<DeviceType>();
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
                ErrorManager.ReportException(ErrorSeverity.Major, "DeviceType.GetDeviceTypes", ex);
            }
            return null;
        }

        private static DeviceType InstantiateFromReader(SqlDataReader reader)
        {
            return new DeviceType()
            {
                DeviceTypeId = SqlHelper.Read<int>(reader, "DeviceTypeId"),
                DeviceTypeName = SqlHelper.Read<string>(reader, "DeviceTypeName"),
            };
        }
    }
}

