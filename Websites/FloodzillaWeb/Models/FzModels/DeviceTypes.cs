namespace FloodzillaWeb.Models.FzModels
{
    public partial class DeviceTypes
    {
        public DeviceTypes()
        {
            Devices = new HashSet<Devices>();
        }

        public int DeviceTypeId { get; set; }
        public string DeviceTypeName { get; set; }

        public virtual ICollection<Devices> Devices { get; set; }
    }
}
