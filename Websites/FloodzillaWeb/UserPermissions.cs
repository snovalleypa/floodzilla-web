using Microsoft.EntityFrameworkCore;
using FloodzillaWeb.Models;

namespace FloodzillaWeb
{
    public enum PermissionOptions
    {
        Region, Location, FloodEvent, User
    }
    public class UserPermissions
    {
        private readonly FloodzillaContext _context;

        public UserPermissions(FloodzillaContext context)
        {
            _context = context;

        }

        public bool CheckPermission(int id, string userId, PermissionOptions option)
        {
            bool isAuthorized = false;
            int? orgId = _context.Users.AsNoTracking().SingleOrDefault(e => e.AspNetUserId == userId).OrganizationsId;

            switch (option)
            {
                // Case for checking Region restriction to login user.
                case PermissionOptions.Region:
                    var region = _context.Regions.AsNoTracking().SingleOrDefault(e => e.RegionId == id);
                    if (region.OrganizationsId == orgId)
                    {
                        isAuthorized = true;
                    }
                    else
                    {
                        isAuthorized = false;
                    }

                    break;

                case PermissionOptions.Location:
                    var location = _context.Locations.AsNoTracking().Include(e=>e.Region).SingleOrDefault(e => e.Id == id);
                    if (location.Region.OrganizationsId == orgId)
                    {
                        isAuthorized = true;
                    }
                    else
                    {
                        isAuthorized = false;
                    }

                    break;

                case PermissionOptions.FloodEvent:
                    var floodEvent = _context.FloodEvents.AsNoTracking().Include(e=>e.Region).SingleOrDefault(e => e.Id == id);
                    if (floodEvent.Region.OrganizationsId == orgId)
                    {
                        isAuthorized = true;
                    }
                    else
                    {
                        isAuthorized = false;
                    }

                    break;

                case PermissionOptions.User:
                    var user = _context.Users.AsNoTracking().SingleOrDefault(e => e.Id == id);
                    if (user.OrganizationsId == orgId)
                    {
                        isAuthorized = true;
                    }
                    else
                    {
                        isAuthorized = false;
                    }
                    break;

                default:
                    break;
            }

            return isAuthorized;
        }

    }

}
