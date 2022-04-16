using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;

namespace FzCommon
{
    public class OrganizationBase
    {
        public int OrganizationsId { get; set; }

        [Required(ErrorMessage = "Organization name is required")]
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }

        public static OrganizationBase GetOrganization(SqlConnection conn, int id)
        {
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM Organizations WHERE OrganizationsId = '{id}'", conn);
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
                ErrorManager.ReportException(ErrorSeverity.Major, "OrganizationBase.GetOrganization", ex);
            }
            return null;
        }

        public static async Task<List<OrganizationBase>> GetOrganizations(SqlConnection conn)
        {
            SqlCommand cmd = new SqlCommand($"SELECT {GetColumnList()} FROM Organizations", conn);
            List<OrganizationBase> ret = new List<OrganizationBase>();
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
                ErrorManager.ReportException(ErrorSeverity.Major, "OrganizationBase.GetOrganizations", ex);
            }
            return ret;
        }

        public static async Task MarkOrganizationsAsUndeleted(SqlConnection conn, IEnumerable<int> regionIds)
        {
            await SqlHelper.CallIdListProcedure(conn, "MarkOrganizationsAsUndeleted", regionIds, 180);
        }

        private static string GetColumnList()
        {
            return "OrganizationsId, Name, IsActive, IsDeleted";
        }

        private static OrganizationBase InstantiateFromReader(SqlDataReader reader)
        {
            OrganizationBase org = new OrganizationBase()
            {
                OrganizationsId = SqlHelper.Read<int>(reader, "OrganizationsId"),
                Name = SqlHelper.Read<string>(reader, "Name"),
                IsActive = SqlHelper.Read<bool>(reader, "IsActive"),
                IsDeleted = SqlHelper.Read<bool>(reader, "IsDeleted"),
            };
            return org;
        }
    }
}
