using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using FloodzillaWeb.Models;
using FloodzillaWeb.Models.FzModels;

namespace FloodzillaWeb.Cache
{
    public enum DeletedCacheOptions
    {
        DeletedDevices, DeletedDeviceConfiguration, DeletedLocations, DeletedRegions, DeletedOrganizations, DeletedUsers,
        DeletedFloodEvents, DeletedUploads, DeletedDataSubscriptions
    }
    public class RecoveryCache
    {
        private FloodzillaContext _context;
        private IMemoryCache _memoryCache;

        public RecoveryCache(FloodzillaContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
        }

        private DateTime GetApplicationCacheExpireTime()
        {
            return DateTime.UtcNow.AddMinutes(30);
        }

        public List<Devices> GetDeletedDevices()
        {
            var devices = new List<Devices>();
            if (!_memoryCache.TryGetValue("DeletedDevices", out devices))
            {
                devices = _context.Devices.AsNoTracking().Include(e => e.Location).ThenInclude(e=>e.Region).ThenInclude(e=>e.Organizations).Where(e => e.IsDeleted == true).ToList();
                _memoryCache.Set("DeletedDevices", devices, GetApplicationCacheExpireTime());
            }
            else
            {
                devices = (List<Devices>)_memoryCache.Get("DeletedDevices");
            }
            return devices;
        }

        public List<Locations> GetDeletedLocations()
        {
            var locations = new List<Locations>();
            if (!_memoryCache.TryGetValue("DeletedLocations", out locations))
            {
                locations = _context.Locations.AsNoTracking().Include(d => d.Devices).Include(l => l.Region).ThenInclude(e=>e.Organizations).Where(e => e.IsDeleted == true).ToList();
                _memoryCache.Set("DeletedLocations", locations, GetApplicationCacheExpireTime());
            }
            else
            {
                locations = (List<Locations>)_memoryCache.Get("DeletedLocations");
            }
            return locations;
        }

        public List<Regions> GetDeletedRegions()
        {
            var regions = new List<Regions>();
            if (!_memoryCache.TryGetValue("DeletedRegions", out regions))
            {
                regions = _context.Regions.AsNoTracking().Include(e=>e.Organizations).Where(e => e.IsDeleted == true).ToList();
                _memoryCache.Set("DeletedRegions", regions, GetApplicationCacheExpireTime());
            }
            else
            {
                regions = (List<Regions>)_memoryCache.Get("DeletedRegions");
            }
            return regions;
        }

        public List<Organizations> GetDeletedOrganizations()
        {
            List<Organizations> organizations = new List<Organizations>();
            if (!_memoryCache.TryGetValue("DeletedOrganizations", out organizations))
            {
                organizations = _context.Organizations.AsNoTracking().Where(e => e.IsDeleted == true).ToList();
                _memoryCache.Set("DeletedOrganizations", organizations, GetApplicationCacheExpireTime());
            }
            else
            {
                organizations = (List<Organizations>)_memoryCache.Get("DeletedOrganizations");
            }
            return organizations;
        }

        public List<Users> GetDeletedUsers()
        {
            List<Users> users = new List<Users>();
            if (!_memoryCache.TryGetValue("DeletedUsers", out users))
            {
                users = _context.Users.AsNoTracking().Include(e => e.Organizations).Include(e => e.AspNetUser).ThenInclude(e => e.AspNetUserRoles).Where(e => e.IsDeleted == true).ToList();
                _memoryCache.Set("DeletedUsers", users, GetApplicationCacheExpireTime());
            }
            else
            {
                users = (List<Users>)_memoryCache.Get("DeletedUsers");
            }
            return users;
        }

        public List<FloodEvents> GetDeletedFloodEvents()
        {
            var floodEvents = new List<FloodEvents>();
            if (!_memoryCache.TryGetValue("DeletedFloodEvents", out floodEvents))
            {
                floodEvents = _context.FloodEvents.AsNoTracking().Include(e => e.Region).ThenInclude(e=>e.Organizations).Include(e=>e.EventsDetail).ThenInclude(e=>e.Location).Where(e => e.IsDeleted == true).ToList();

                _memoryCache.Set("DeletedFloodEvents", floodEvents, GetApplicationCacheExpireTime());
            }
            else
            {
                floodEvents = (List<FloodEvents>)_memoryCache.Get("DeletedFloodEvents");
            }
            return floodEvents;
        }

        public List<Uploads> GetDeletedUploads()
        {
            List<Uploads> uploads = new List<Uploads>();
            if (!_memoryCache.TryGetValue("DeletedUploads", out uploads))
            {
                uploads = _context.Uploads.AsNoTracking().Where(e => e.IsDeleted == true).ToList();
                _memoryCache.Set("DeletedUploads", uploads, GetApplicationCacheExpireTime());
            }
            else
            {
                uploads = (List<Uploads>)_memoryCache.Get("DeletedUploads");
            }
            return uploads;
        }

        public List<DataSubscriptions> GetDeletedDataSubscriptions()
        {
            var dataSubscriptions = new List<DataSubscriptions>();
            if (!_memoryCache.TryGetValue("DeletedDataSubscriptions", out dataSubscriptions))
            {
                dataSubscriptions = _context.DataSubscriptions.AsNoTracking().Include(e => e.User).ThenInclude(e=>e.Organizations).Include(e=>e.User).ThenInclude(e=>e.AspNetUser).Where(e => e.IsDeleted == true).ToList();
                _memoryCache.Set("DeletedDataSubscriptions", dataSubscriptions, GetApplicationCacheExpireTime());
            }
            else
            {
                dataSubscriptions = (List<DataSubscriptions>)_memoryCache.Get("DeletedDataSubscriptions");
            }
            return dataSubscriptions;
        }

        public void RemoveCache(DeletedCacheOptions options)
        {
            switch (options)
            {
                case DeletedCacheOptions.DeletedDevices:
                    _memoryCache.Remove("DeletedDevices");
                    break;
                case DeletedCacheOptions.DeletedDeviceConfiguration:
                    _memoryCache.Remove("DeletedDeviceConfiguration");
                    break;
                case DeletedCacheOptions.DeletedLocations:
                    _memoryCache.Remove("DeletedLocations");
                    break;
                case DeletedCacheOptions.DeletedRegions:
                    _memoryCache.Remove("DeletedRegions");
                    break;
                case DeletedCacheOptions.DeletedOrganizations:
                    _memoryCache.Remove("DeletedOrganizations");
                    break;
                case DeletedCacheOptions.DeletedUsers:
                    _memoryCache.Remove("DeletedUsers");
                    break;
                case DeletedCacheOptions.DeletedFloodEvents:
                    _memoryCache.Remove("DeletedFloodEvents");
                    break;
                case DeletedCacheOptions.DeletedUploads:
                    _memoryCache.Remove("DeletedUploads");
                    break;
                case DeletedCacheOptions.DeletedDataSubscriptions:
                    _memoryCache.Remove("DeletedDataSubscriptions");
                    break;
                default:
                    break;
            }
        }

        public void RemoveAllCaches()
        {
            foreach (DeletedCacheOptions option in Enum.GetValues(typeof(DeletedCacheOptions)))
            {
                _memoryCache.Remove(option.ToString());
            }
        }
    }
}
