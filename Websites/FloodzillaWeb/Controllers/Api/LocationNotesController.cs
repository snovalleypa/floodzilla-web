using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

using FloodzillaWeb.Cache;
using FloodzillaWeb.Models;
using FloodzillaWeb.Models.FzModels;
using FloodzillaWeb.ViewModels.LocationNotes;

namespace FloodzillaWeb.Controllers.Api
{
    [Produces("application/json")]
    [Route("api/LocationNotes")]
    public class LocationNotesController : Controller
    {
        private readonly FloodzillaContext m_FzDbContext;

        private readonly SignInManager<ApplicationUser> m_SignInManager;
        private ApplicationCache _applicationCache;


        public LocationNotesController(FloodzillaContext context, IMemoryCache memoryCache, SignInManager<ApplicationUser> signinmanager)
        {
            m_FzDbContext = context;
            _applicationCache = new ApplicationCache(context, memoryCache);
            m_SignInManager = signinmanager;
        }

        [NonAction]
        private int GetFloodzillaUserId()
        {
            return SecurityHelper.GetFloodzillaUserId(User, _applicationCache);
        }

        [NonAction]
        private string GetUserRole(int userid)
        {
            return (from u in m_FzDbContext.Users
                    join ur in m_FzDbContext.AspNetUserRoles on u.AspNetUserId equals ur.ApplicationUserId
                    join r in m_FzDbContext.AspNetRoles on ur.RoleId equals r.Id
                    where u.Id == userid
                    select r.Name).FirstOrDefault().ToLower().Trim();
        }

        [NonAction]
        private int ValidateUser(int locid = 0)
        {
            int userid = 0;
            try
            {
                string token = string.Empty;
#if SUPPORT_TOKENS
                StringValues headervalues;
                if (Request.Headers.TryGetValue("token", out headervalues))
                {
                    token = headervalues.FirstOrDefault();
                    if (!TokenController.ValidateToken(m_SignInManager, token, out userid)) userid = 0;
                }
#endif

                if (userid == 0 && string.IsNullOrEmpty(token)) userid = GetFloodzillaUserId();

                if (userid > 0 && locid > 0)
                {
                    string role = GetUserRole(userid);

                    Locations loc = (from l in m_FzDbContext.Locations where l.Id == locid && !l.IsDeleted select l).FirstOrDefault();

                    if (role == "guest") userid = (loc != null && loc.IsPublic) ? userid : 0;
                    else if (role == "admin") userid = (loc != null) ? userid : 0;
                    else
                    {
                        Locations userloc = (from u in m_FzDbContext.Users
                                             join r in m_FzDbContext.Regions on u.OrganizationsId equals r.OrganizationsId
                                             join l in m_FzDbContext.Locations on r.RegionId equals l.RegionId
                                             where u.Id == userid && l.Id == locid && !l.IsDeleted
                                             select l).FirstOrDefault();
                        if (userloc == null) userid = 0;
                    }
                }
            }
            catch
            {
                userid = 0;
            }
            return userid;
        }

        [HttpGet("Notes/{locid}")]
        public IActionResult Notes(int locid)
        {
            try
            {
                int userid = ValidateUser(locid);
                if (userid <= 0) return Json(new { Message = "Not Authorized." });

                JsonSerializerSettings jsettings = new JsonSerializerSettings();
                jsettings.NullValueHandling = NullValueHandling.Ignore;

                var queryresult = (from n in m_FzDbContext.LocationNotes
                                   join u in m_FzDbContext.Users on n.UserId equals u.Id

                                   join mu in m_FzDbContext.Users on n.ModifiedBy equals mu.Id into result
                                   from mu2 in result.DefaultIfEmpty()

                                   where n.LocationId == locid && (n.IsDeleted == null || n.IsDeleted == false)
                                   orderby n.Pin descending, n.CreatedOn descending
                                   select new
                                   {
                                       NoteId = n.NoteId,
                                       Note = n.Note,
                                       CreatedOn = n.CreatedOn,
                                       FirstName = u.FirstName,
                                       LastName = u.LastName,
                                       ModifiedOn = n.ModifiedOn,
                                       Pin = n.Pin,
                                       Mod = mu2
                                   });
                List<LocationNoteView> locnotes = new List<LocationNoteView>();
                foreach (var r in queryresult)
                {
                    locnotes.Add(new LocationNoteView
                    {
                        NoteId = r.NoteId,
                        Note = r.Note,
                        CreatedOn = r.CreatedOn,
                        FirstName = r.FirstName,
                        LastName = r.LastName,
                        ModifiedOn = r.ModifiedOn,
                        Pin = r.Pin,
                        ModFirstName = (r.Mod != null) ? r.Mod.FirstName : null,
                        ModLastName = (r.Mod != null) ? r.Mod.LastName : null
                    });
                }
                return Json(locnotes, jsettings);
            }
            catch
            {
                return Json(new { Message = "Some thing went wrong. Please try later or contact the service provider." });
            }
        }

        [HttpGet("Note/{noteid}")]
        public IActionResult Note(int noteid)
        {
            try
            {
                int userid = ValidateUser();
                if (userid <= 0) return Json(new { Message = "Not Authorized." });

                JsonSerializerSettings jsettings = new JsonSerializerSettings();
                jsettings.NullValueHandling = NullValueHandling.Ignore;

                return Json((from n in m_FzDbContext.LocationNotes
                             where n.NoteId == noteid && (n.IsDeleted == null || n.IsDeleted == false)
                             select new
                             {
                                 NoteId = n.NoteId,
                                 Note = n.Note,
                                 Pin = n.Pin
                             }), jsettings);
            }
            catch
            {
                return Json(new { Message = "Some thing went wrong. Please try later or contact the service provider." });
            }
        }

        [HttpPost("AddNote")]
        public async Task<IActionResult> AddNote([FromBody] LocationNoteView note)
        {
            JsonSerializerSettings jsettings = new JsonSerializerSettings();
            jsettings.NullValueHandling = NullValueHandling.Ignore;

            try
            {

                int userid = ValidateUser(note.LocationId ?? 0);
                if (userid <= 0) return Json(new { Succeeded = false, Message = "Not Authorized." }, jsettings);


                m_FzDbContext.LocationNotes.Add(new LocationNote { Note = note.Note, LocationId = note.LocationId ?? 0, Pin = note.Pin, UserId = userid, IsDeleted = false });
                await m_FzDbContext.SaveChangesAsync();
                return Json(new { Succeeded = true, Message = "Note added successfully!" }, jsettings);
            }
            catch
            {
                return Json(new { Succeeded = false, Message = "Some thing went wrong. Please try later or contact the service provider." }, jsettings);
            }
        }


        [HttpPost("UpdateNote")]
        public async Task<IActionResult> UpdateNote([FromBody] LocationNoteView note)
        {
            JsonSerializerSettings jsettings = new JsonSerializerSettings();
            jsettings.NullValueHandling = NullValueHandling.Ignore;

            try
            {
                int userid = ValidateUser(note.LocationId ?? 0);
                if (userid <= 0) return Json(new { Succeeded = false, Message = "Not Authorized." }, jsettings);

                LocationNote ln = (from n in m_FzDbContext.LocationNotes where n.NoteId == note.NoteId && n.IsDeleted != true select n).SingleOrDefault();
                if (ln == null) return Json(new { Succeeded = false, Message = "Note not found." }, jsettings);

                ln.Note = note.Note;
                ln.Pin = note.Pin;
                ln.ModifiedOn = DateTime.UtcNow;
                ln.ModifiedBy = userid;

                //m_FzDbContext.Entry(ln).State = EntityState.Detached;
                m_FzDbContext.LocationNotes.Update(ln);
                await m_FzDbContext.SaveChangesAsync();

                note.CreatedOn = ln.CreatedOn;
                note.ModifiedOn = ln.ModifiedOn;

                Users nu = (from u in m_FzDbContext.Users where u.Id == ln.UserId select u).SingleOrDefault();
                if (nu != null)
                {
                    note.FirstName = nu.FirstName;
                    note.LastName = nu.LastName;
                }

                if (ln.UserId != userid) nu = (from u in m_FzDbContext.Users where u.Id == userid select u).SingleOrDefault();
                if (nu != null)
                {
                    note.ModFirstName = nu.FirstName;
                    note.ModLastName = nu.LastName;
                }

                return Json(new { Succeeded = true, LocNote = note }, jsettings);
            }
            catch 
            {
                return Json(new { Succeeded = false, Message = "Some thing went wrong. Please try later or contact the service provider." }, jsettings);
            }
        }


        [HttpPost("DeleteNote")]
        public async Task<IActionResult> DeleteNote([FromBody] LocationNoteView note)
        {
            try
            {

                int userid = ValidateUser(note.LocationId ?? 0);
                if (userid <= 0) return Json(new { Succeeded = false, Message = "Not Authorized." });

                LocationNote ln = (from n in m_FzDbContext.LocationNotes where n.NoteId == note.NoteId && n.IsDeleted != true select n).SingleOrDefault();
                if (ln == null) return Json(new { Succeeded = false, Message = "Note not found." });

                ln.IsDeleted = true;

                //m_FzDbContext.Entry(ln).State = EntityState.Detached;
                m_FzDbContext.LocationNotes.Update(ln);
                await m_FzDbContext.SaveChangesAsync();
                return Json(new { Succeeded = true, Message = "Note deleted successfully!" });
            }
            catch
            {
                return Json(new { Succeeded = false, Message = "Some thing went wrong. Please try later or contact the service provider." });
            }
        }
    }
}
