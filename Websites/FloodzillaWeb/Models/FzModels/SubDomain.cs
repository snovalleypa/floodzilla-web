using System.ComponentModel.DataAnnotations;

namespace FloodzillaWeb.Models.FzModels
{
    public class SubDomain
    {
        [Key]
        public int SubDomainId { get; set; }
        public string SubDomainName { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool? IsActive { get; set; }

    }
}
