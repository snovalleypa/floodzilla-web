namespace FloodzillaWeb.ViewModels.Uploads
{
    public class NearestLocationViewModel
    {
        public int Id { get; set; }
        public string LocationName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double DistanceKm { get; set; }
    }
}
