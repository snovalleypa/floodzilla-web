namespace FloodzillaWeb.Models.FzModels
{
    public class AppsData
    {
        public int AppsDataId { get; set; }
        public string ExternalId { get; set; }
        public int? AppId { get; set; }
        public int? LocationId { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }

        public string AppData { get; set; }
        public bool ShouldSerializeAppData()
        {
            return false;
        }
    }
}
