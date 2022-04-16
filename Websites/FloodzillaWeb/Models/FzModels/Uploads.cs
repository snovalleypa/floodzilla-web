using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;

using FzCommon;

namespace FloodzillaWeb.Models.FzModels
{
    public partial class Uploads
    {
        public int Id { get; set; }
        [Required(ErrorMessage ="Date is required.")]
        public DateTime? DateOfPicture { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? Altitude { get; set; }
        [Required(ErrorMessage = "Location is required.")]
        public int LocationId { get; set; }
        public string? ResponseString { get; set; }
        public string Image { get; set; }
        public bool IsVarified { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public int? Rank { get; set; }
        public Locations? Location { get; set; }

        public static List<Uploads> GetUploads(SqlConnection conn)
        {
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM Uploads", conn);
            try
            {
                List<Uploads> ret = new List<Uploads>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ret.Add(InstantiateFromReader(reader));
                    }
                }
                return ret;
            }
            catch (Exception)
            {
                //$ TODO: Error handling?
            }
            return null;
        }

        public async static Task<List<Uploads>> GetUploadsAsync(SqlConnection conn)
        {
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM Uploads", conn);
            try
            {
                List<Uploads> ret = new List<Uploads>();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        ret.Add(InstantiateFromReader(reader));
                    }
                }
                return ret;
            }
            catch (Exception)
            {
                //$ TODO: Error handling?
            }
            return null;
        }

        private static Uploads InstantiateFromReader(SqlDataReader reader)
        {
            Uploads u = new Uploads()
            {
                Id = SqlHelper.Read<int>(reader, "Id"),
                DateOfPicture = SqlHelper.Read<DateTime?>(reader, "DateOfPicture"),
                Latitude = SqlHelper.Read<double?>(reader, "Latitude"),
                Longitude = SqlHelper.Read<double?>(reader, "Longitude"),
                Altitude = SqlHelper.Read<double?>(reader, "Altitude"),
                LocationId = SqlHelper.Read<int>(reader, "LocationId"),
                ResponseString = SqlHelper.Read<string>(reader, "ResponseString"),
                Image = SqlHelper.Read<string>(reader, "Image"),
                IsVarified = SqlHelper.Read<bool>(reader, "IsVarified"),
                IsActive = SqlHelper.Read<bool>(reader, "IsActive"),
                IsDeleted = SqlHelper.Read<bool>(reader, "IsDeleted"),
                Rank = SqlHelper.Read<int?>(reader, "Rank"),
            };
            return u;
        }

        private static string GetColumnList()
        {
            return "Id, DateOfPicture, Latitude, Longitude, Altitude, LocationId, ResponseString, Image, IsVarified, IsActive, IsDeleted, Rank";
        }
    }
}
