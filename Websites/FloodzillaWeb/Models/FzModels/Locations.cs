using System.ComponentModel.DataAnnotations.Schema;
using FzCommon;

namespace FloodzillaWeb.Models.FzModels
{
    public class Locations : SensorLocationBase
    {
        public Locations()
        {
            UserLocations = new HashSet<UserLocations>();
        }

        public Locations(Locations source) : base(source)
        {
            this.Devices = source.Devices;
            this.LocationImages = source.LocationImages;
            this.Region = source.Region;
            this.Uploads = source.Uploads;
            this.UserLocations = source.UserLocations;
            this.LocNotifications = source.LocNotifications;
            this.EventsDetail = source.EventsDetail;
            this.DeviceId = source.DeviceId;
            this.DeviceSeaLevelElevation = source.DeviceSeaLevelElevation;
        }

        public Devices? Devices { get; set; }
        public virtual LocationImages? LocationImages { get; set; }
        public virtual Regions? Region { get; set; }
        public ICollection<Uploads>? Uploads { get; set; }
        public ICollection<UserLocations>? UserLocations { get; set; }

        public virtual ICollection<LocNotification>? LocNotifications { get; set; }
        public virtual ICollection<EventsDetail>? EventsDetail { get; set; }

        [NotMapped]
        public int? DeviceId { get; set; }
        [NotMapped]
        public double? DeviceSeaLevelElevation { get; set; }

    }
}
