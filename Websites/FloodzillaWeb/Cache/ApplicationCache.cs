using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using FloodzillaWeb.Models;
using FloodzillaWeb.Models.FzModels;

namespace FloodzillaWeb.Cache
{
    public enum CacheOptions
    {
        Devices, DeviceConfiguration, Locations, Regions, Organizations, Users, Roles, UserLocations,
        FloodEvents, EventsDetail, Uploads, DataSubscriptions, UserNotifications, ChannelTypes, NotifyTypes,
        ElevationTypes, Elevations, DeviceTypes
    }
    public class ApplicationCache
    {
        private readonly FloodzillaContext _context;
        private IMemoryCache _memoryCache;
        private readonly RoleManager<IdentityRole> _roleManager;
        private RecoveryCache _recoveryCache;

        public ApplicationCache(FloodzillaContext context, IMemoryCache memoryCache)
        {
            _context = context;
            _memoryCache = memoryCache;
            _recoveryCache = new RecoveryCache(_context, _memoryCache);
        }

        public ApplicationCache(RoleManager<IdentityRole> roleManager, FloodzillaContext context, IMemoryCache memoryCache) :this(context,memoryCache)
        {
            _roleManager = roleManager;
        }

        private DateTime GetApplicationCacheExpireTime()
        {
            return DateTime.UtcNow.AddMinutes(30);
        }
        
        public List<Organizations> GetOrganizations()
        {
            List<Organizations> organizations = new List<Organizations>();
            if (!_memoryCache.TryGetValue("Organizations", out organizations))
            {
                organizations = _context.Organizations.AsNoTracking().Where(e => e.IsDeleted == false).ToList();
                _memoryCache.Set("Organizations", organizations, GetApplicationCacheExpireTime());
            }
            else
            {
                organizations = (List<Organizations>)_memoryCache.Get("Organizations");
            }
            return organizations;
        }

        public List<Users> GetUsers()
        {
            List<Users> users = new List<Users>();
            if (!_memoryCache.TryGetValue("Users", out users))
            {
                users = _context.Users.AsNoTracking().Include(e => e.Organizations).Include(e => e.AspNetUser).ThenInclude(e => e.AspNetUserRoles).Where(e => e.IsDeleted == false).ToList();
                _memoryCache.Set("Users", users, GetApplicationCacheExpireTime());
            }
            else
            {
                users = (List<Users>)_memoryCache.Get("Users");
            }
            return users;
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

        public List<IdentityRole> GetRoles()
        {
            List<IdentityRole> Roles = new List<IdentityRole>();
            if (!_memoryCache.TryGetValue("Roles", out Roles))
            {
                Roles = _roleManager.Roles.AsNoTracking().ToList();
                _memoryCache.Set("Roles", Roles, GetApplicationCacheExpireTime());
            }
            else
            {
                Roles = (List<IdentityRole>)_memoryCache.Get("Roles");
            }
            return Roles;
        }

        public List<Regions> GetRegions()
        {
            var regions = new List<Regions>();
            if (!_memoryCache.TryGetValue("Regions", out regions))
            {
                regions = _context.Regions.AsNoTracking().Include(e => e.Organizations).Where(e => e.IsDeleted == false).ToList();
                _memoryCache.Set("Regions", regions, GetApplicationCacheExpireTime());
            }
            else
            {
                regions = (List<Regions>)_memoryCache.Get("Regions");
            }
            return regions;
        }

        public List<FloodEvents> GetFloodEvents()
        {
            var floodEvents = new List<FloodEvents>();
            if (!_memoryCache.TryGetValue("FloodEvents", out floodEvents))
            {
                floodEvents = _context.FloodEvents.AsNoTracking().Include(e => e.Region).Include(e=>e.EventsDetail).ThenInclude(e=>e.Location).Where(e => e.IsDeleted == false).ToList();
                _memoryCache.Set("FloodEvents", floodEvents, GetApplicationCacheExpireTime());
            }
            else
            {
                floodEvents = (List<FloodEvents>)_memoryCache.Get("FloodEvents");
            }
            return floodEvents;
        }

        public List<EventsDetail> GetEventsDetail()
        {
            var eventsDetail = new List<EventsDetail>();
            if (!_memoryCache.TryGetValue("EventsDetail", out eventsDetail))
            {
                eventsDetail = _context.EventsDetail.AsNoTracking().Include(e => e.Floodevent).Include(e => e.Location).Where(e => e.Floodevent.IsDeleted == false && e.Location.IsDeleted == false).ToList();
                _memoryCache.Set("EventsDetail", eventsDetail, GetApplicationCacheExpireTime());
            }
            else
            {
                eventsDetail = (List<EventsDetail>)_memoryCache.Get("EventsDetail");
            }
            return eventsDetail;
        }

        public List<Locations> GetLocations()
        {
            var locations = new List<Locations>();
            if (!_memoryCache.TryGetValue("Locations", out locations))
            {
                locations = _context.Locations.AsNoTracking().Include(l => l.Region).Include(l => l.Devices).ThenInclude(e=>e.DeviceType).Where(e => e.IsDeleted == false).ToList();
                _memoryCache.Set("Locations", locations, GetApplicationCacheExpireTime());
            }
            else
            {
                locations = (List<Locations>)_memoryCache.Get("Locations");
            }
            return locations;
        }

        public List<Devices> GetDevices()
        {

            var devices = new List<Devices>();
            if (!_memoryCache.TryGetValue("Devices", out devices))
            {
                devices = _context.Devices.AsNoTracking().Include(e => e.Location).Include(e=>e.DeviceType).Where(e=>e.IsDeleted==false).ToList();
                _memoryCache.Set("Devices", devices, GetApplicationCacheExpireTime());
            }
            else
            {
                devices = (List<Devices>)_memoryCache.Get("Devices");
            }
            return devices;
        }

        public List<DevicesConfiguration> GetDevicesConfiguration()
        {
            List<DevicesConfiguration> devicesConfiguration = new List<DevicesConfiguration>();
            if (!_memoryCache.TryGetValue("DeviceConfiguration", out devicesConfiguration))
            {
                devicesConfiguration = _context.DevicesConfiguration.AsNoTracking().Include(e => e.Device).ThenInclude(e => e.Location).Where(e=>e.Device.IsDeleted==false).ToList();
                _memoryCache.Set("DeviceConfiguration", devicesConfiguration, GetApplicationCacheExpireTime());
            }
            else
            {
                devicesConfiguration = (List<DevicesConfiguration>)_memoryCache.Get("DeviceConfiguration");
            }
            return devicesConfiguration;
        }

        public List<UserLocations> GetAssignedLocations()
        {
            List<UserLocations> userLocations = new List<UserLocations>();
            if (!_memoryCache.TryGetValue("UserLocations", out userLocations))
            {
                userLocations = _context.UserLocations.AsNoTracking().Include(e => e.Regions).Include(e => e.User).Include(e => e.Location).ToList();
                _memoryCache.Set("UserLocations", userLocations, GetApplicationCacheExpireTime());
            }
            else
            {
                userLocations = (List<UserLocations>)_memoryCache.Get("UserLocations");
            }
            return userLocations;
        }
        
        public List<Uploads> GetUploads()
        {
            List<Uploads> uploads = new List<Uploads>();
            if (!_memoryCache.TryGetValue("Uploads", out uploads))
            {
                uploads = _context.Uploads.AsNoTracking().Include(e=>e.Location).Where(e => e.IsDeleted == false).ToList();
                _memoryCache.Set("Uploads", uploads, GetApplicationCacheExpireTime());
            }
            else
            {
                uploads = (List<Uploads>)_memoryCache.Get("Uploads");
            }
            return uploads;
        }
        
        public List<DataSubscriptions> GetDataSubscriptions()
        {
            var dataSubscriptions = new List<DataSubscriptions>();
            if(!_memoryCache.TryGetValue("DataSubscriptions",out dataSubscriptions))
            {
                dataSubscriptions = _context.DataSubscriptions.AsNoTracking().Include(e => e.User).Where(e => e.IsDeleted == false).ToList();
                _memoryCache.Set("DataSubscriptions", dataSubscriptions, GetApplicationCacheExpireTime());
            }
            else
            {
                dataSubscriptions = (List<DataSubscriptions>)_memoryCache.Get("DataSubscriptions");
            }
            return dataSubscriptions;
        }

        public List<ElevationTypes> GetElevationTypes()
        {
            var elevationTypes = new List<ElevationTypes>();
            if (!_memoryCache.TryGetValue("ElevationTypes", out elevationTypes))
            {
                elevationTypes = _context.ElevationTypes.AsNoTracking().ToList();
                _memoryCache.Set("ElevationTypes", elevationTypes, GetApplicationCacheExpireTime());
            }
            else
            {
                elevationTypes = (List<ElevationTypes>)_memoryCache.Get("ElevationTypes");
            }
            return elevationTypes;
        }

        public List<Elevations> GetElevations()
        {
            var elevations = new List<Elevations>();
            if (!_memoryCache.TryGetValue("Elevations", out elevations))
            {
                elevations = _context.Elevations.AsNoTracking().Include(e=>e.ElevationType).ToList();
                _memoryCache.Set("Elevations", elevations, GetApplicationCacheExpireTime());
            }
            else
            {
                elevations = (List<Elevations>)_memoryCache.Get("Elevations");
            }
            return elevations;
        }

        public List<DeviceTypes> GetDeviceTypes()
        {
            var deviceTypes = new List<DeviceTypes>();
            if (!_memoryCache.TryGetValue("DeviceTypes", out deviceTypes))
            {
                deviceTypes = _context.DeviceTypes.AsNoTracking().ToList();
                _memoryCache.Set("DeviceTypes", deviceTypes, GetApplicationCacheExpireTime());
            }
            else
            {
                deviceTypes = (List<DeviceTypes>)_memoryCache.Get("DeviceTypes");
            }
            return deviceTypes;
        }

        public List<UserNotification> GetUserNotifications()
        {
            var userNotifications = new List<UserNotification>();
            if (!_memoryCache.TryGetValue("UserNotifications", out userNotifications))
            {
                userNotifications = _context.UserNotifications.AsNoTracking().Include(e => e.NotifyType).Include(e => e.ChannelType)
                        .Include(e => e.LocNotifications).ThenInclude(e => e.Location).ToList();
                _memoryCache.Set("UserNotifications", userNotifications, GetApplicationCacheExpireTime());
            }
            else
            {
                userNotifications = (List<UserNotification>)_memoryCache.Get("UserNotifications");
            }
            return userNotifications;
        }

        public List<ChannelType> GetChannelTypes()
        {
            var channelTypes = new List<ChannelType>();
            if (!_memoryCache.TryGetValue("ChannelTypes", out channelTypes))
            {
                channelTypes = _context.ChannelTypes.AsNoTracking().ToList();
                _memoryCache.Set("ChannelTypes", channelTypes, GetApplicationCacheExpireTime());
            }
            else
            {
                channelTypes = (List<ChannelType>)_memoryCache.Get("ChannelTypes");
            }
            return channelTypes;
        }

        public List<NotifyType> GetNotifyTypes()
        {
            var notifyTypes = new List<NotifyType>();
            if (!_memoryCache.TryGetValue("NotifyTypes", out notifyTypes))
            {
                notifyTypes = _context.NotifyTypes.AsNoTracking().ToList();
                _memoryCache.Set("NotifyTypes", notifyTypes, GetApplicationCacheExpireTime());
            }
            else
            {
                notifyTypes = (List<NotifyType>)_memoryCache.Get("NotifyTypes");
            }
            return notifyTypes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="refreshDeletedCache"></param> Will indicate, if some delete opteration is perform on UI. if flag is on, 
        /// then we must clear the corresponding DeletedCache in RecoveryCache class.
        public void RemoveCache(CacheOptions options, bool refreshDeletedCache=false)
        {
            switch (options)
            {
                // Removing Devices Cache
                case CacheOptions.Devices:
                    if (refreshDeletedCache)
                    {
                        // Refreshing the Deleted Cache records of the deleted devices.
                        _recoveryCache.RemoveCache(DeletedCacheOptions.DeletedDevices);
                    }
                    _memoryCache.Remove("Devices");
                    break;

                // Removing Devices Configuration Cache
                case CacheOptions.DeviceConfiguration:
                    _memoryCache.Remove("DeviceConfiguration");
                    break;

                // Removing Locations Cache
                case CacheOptions.Locations:
                    if (refreshDeletedCache)
                    {
                        // Refreshing the Deleted Cache records of the deleted locations.
                        _recoveryCache.RemoveCache(DeletedCacheOptions.DeletedLocations);
                    }
                    _memoryCache.Remove("Locations");
                    break;

                // Removing Regions Cache
                case CacheOptions.Regions:
                    if (refreshDeletedCache)
                    {
                        // Refreshing the Deleted Cache records of the deleted devices.
                        _recoveryCache.RemoveCache(DeletedCacheOptions.DeletedRegions);
                    }
                    _memoryCache.Remove("Regions");
                    break;

                // Removing Organizations Cache
                case CacheOptions.Organizations:
                    if (refreshDeletedCache)
                    {
                        // Refreshing the Deleted Cache records of the deleted orgs.
                        _recoveryCache.RemoveCache(DeletedCacheOptions.DeletedOrganizations);
                    }
                    _memoryCache.Remove("Organizations");
                    break;

                // Removing Users Cache
                case CacheOptions.Users:
                    if (refreshDeletedCache)
                    {
                        // Refreshing the Deleted Cache records of the deleted users.
                        _recoveryCache.RemoveCache(DeletedCacheOptions.DeletedUsers);
                    }
                    _memoryCache.Remove("Users");
                    _memoryCache.Remove("DeletedUsers");
                    break;

                // Removing Roles Cache
                case CacheOptions.Roles:
                    _memoryCache.Remove("Roles");
                    break;

                // Removing User Locations Cache
                case CacheOptions.UserLocations:
                    _memoryCache.Remove("UserLocations");
                    break;

                // Removing FloodEvents Cache
                case CacheOptions.FloodEvents:
                    if (refreshDeletedCache)
                    {
                        // Refreshing the Deleted Cache records of the deleted events.
                        _recoveryCache.RemoveCache(DeletedCacheOptions.DeletedFloodEvents);
                    }
                    _memoryCache.Remove("FloodEvents");
                    break;

                // Removing EventsDetail Cache
                case CacheOptions.EventsDetail:
                    if (refreshDeletedCache)
                    {
                        // Refreshing the Deleted Cache records of the deleted events.
                        _recoveryCache.RemoveCache(DeletedCacheOptions.DeletedFloodEvents);
                    }
                    _memoryCache.Remove("EventsDetail");
                    break;

                // Removing Uploads Cache
                case CacheOptions.Uploads:
                    _memoryCache.Remove("Uploads");
                    break;

                // Removing DataSubscription Cache
                case CacheOptions.DataSubscriptions:
                    if (refreshDeletedCache)
                    {
                        _recoveryCache.RemoveCache(DeletedCacheOptions.DeletedDataSubscriptions);
                    }
                    _memoryCache.Remove("DataSubscriptions");
                    break;

                // Removing UserNotifications Cache
                case CacheOptions.UserNotifications:
                    _memoryCache.Remove("UserNotifications");
                    break;

                // Removing channel types Cache
                case CacheOptions.ChannelTypes:
                    _memoryCache.Remove("ChannelTypes");
                    break;

                // Removing notify types Cache
                case CacheOptions.NotifyTypes:
                    _memoryCache.Remove("NotifyTypes");
                    break;

                // Removing Elevations cache
                case CacheOptions.Elevations:
                    _memoryCache.Remove("Elevations");
                    break;

                default:
                    break;
            }
        }

        public void RemoveAllCaches()
        {
            foreach (CacheOptions option in Enum.GetValues(typeof(CacheOptions)))
            {
                _memoryCache.Remove(option.ToString());
            }

            // refreshing cache of the deleted records
            _recoveryCache.RemoveAllCaches();
        }

    }
}
