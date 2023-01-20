using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

using FloodzillaWeb.Models;
using FloodzillaWeb.ViewModels.Users;
using FloodzillaWeb.Models.FzModels;
using FloodzillaWeb.Cache;
using FzCommon;

namespace FloodzillaWeb.Controllers
{
    [Authorize(Roles ="Admin,Organization Admin")]
    public class UsersController : FloodzillaController
    {
        public UsersController(FloodzillaContext context, IMemoryCache memoryCache, UserPermissions userPermissions,
                               UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
                               IUserValidator<ApplicationUser> userValidator)
                : base(context, memoryCache, userPermissions, userManager, roleManager, userValidator)
        {
        }

        private const string LogBookObjectType = "User";

        //$ TODO: Region
        [NonAction]
        private List<UsersViewModel> GetUserList(bool showDeleted)
        {
            var users = new List<Users>(_applicationCache.GetUsers());
            if (showDeleted)
            {
                users.AddRange(_applicationCache.GetDeletedUsers());
            }
            var roles = _applicationCache.GetRoles();

            List<UsersViewModel> listUsers = new List<UsersViewModel>();

            RegionBase region = null;
            using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
            {
                sqlcn.Open();
                region = RegionBase.GetRegion(sqlcn, FzCommon.Constants.SvpaRegionId);
                sqlcn.Close();
            }

            if (User.IsInRole("Admin"))
            {
                foreach (var item in users)
                {
                    var role = roles.SingleOrDefault(e => e.Id == item.AspNetUser.AspNetUserRoles.First().RoleId);
                    listUsers.Add(new UsersViewModel()
                    {
                        FirstName = item.FirstName,
                        LastName = item.LastName,
                        Id = item.Id,
                        Address = item.Address,
                        AspNetUserId = item.AspNetUserId,
                        Email = item.AspNetUser.Email,
                        UserName = item.AspNetUser.UserName,
                        RoleName = role.Name,
                        OrgName = item.Organizations?.Name,
                        IsDeleted = item.IsDeleted,
                        CreatedOn = region.ToRegionTimeFromUtc(item.CreatedOn),
                        IsEmailVerified = item.AspNetUser.EmailConfirmed,
                        IsPhoneVerified = item.AspNetUser.PhoneNumberConfirmed,
                        HasPassword = !String.IsNullOrEmpty(item.AspNetUser.PasswordHash),
                    });
                }
            }
            else if (User.IsInRole("Organization Admin"))
            {
                string aspNetUserId = SecurityHelper.GetAspNetUserId(User);
                var user = users.SingleOrDefault(e => e.AspNetUserId == aspNetUserId);
                var OrgUsers = users.Where(e => e.OrganizationsId == user.OrganizationsId).ToList();
                foreach (var item in OrgUsers)
                {
                    var role = roles.SingleOrDefault(e => e.Id == item.AspNetUser.AspNetUserRoles.First().RoleId);
                    listUsers.Add(new UsersViewModel()
                    {
                        FirstName = item.FirstName,
                        LastName = item.LastName,
                        Id = item.Id,
                        Address = item.Address,
                        AspNetUserId = item.AspNetUserId,
                        Email = item.AspNetUser.Email,
                        UserName = item.AspNetUser.UserName,
                        RoleName = role.Name,
                        OrgName = item.Organizations?.Name,
                        IsDeleted = item.IsDeleted,
                        CreatedOn = region.ToRegionTimeFromUtc(item.CreatedOn),
                    });
                }
            }
            return listUsers;
        }

        [NonAction]
        private List<SelectListItem> GetRoles()
        {
            var Roles = _applicationCache.GetRoles();

            if (!User.IsInRole("Admin"))
            {
                Roles = Roles.Where(e => e.Name == "Organization Member").ToList();
            }
            var selectListItems = new List<SelectListItem>();
            foreach (var item in Roles)
            {
                selectListItems.Add(new SelectListItem() { Text = item.Name, Value = item.Name });
            }
            selectListItems.Insert(0, new SelectListItem() { Text = "--Select Role--", Value = "" });
            return selectListItems;
        }

        [NonAction]
        private List<SelectListItem> GetOrganizations()
        {
            var orgs = _applicationCache.GetOrganizations().Where(e => e.IsActive == true).ToList();

            List<SelectListItem> selectListItems = new List<SelectListItem>();
            if (!User.IsInRole("Admin"))
            {
                var user = SecurityHelper.GetFloodzillaUser(User, _applicationCache);
                orgs = orgs.Where(e => e.OrganizationsId == user.OrganizationsId).ToList();
            }

            foreach (var item in orgs)
            {
                selectListItems.Add(new SelectListItem() { Text = item.Name, Value = item.OrganizationsId.ToString() });
            }
            selectListItems.Insert(0, new SelectListItem() { Text = "--Select Organization--", Value = "" });
            return selectListItems;
        }

        [NonAction]
        private void SetDropdownList()
        {
            ViewBag.Roles = GetRoles();
            ViewBag.Orgs = GetOrganizations();
        }

        public  IActionResult Index()
        {
            return View();
        }

        public IActionResult Edit(int id)
        {
            var userinfo = _context.Users.SingleOrDefault(e => e.Id == id);
            if (userinfo == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin") && !_userPermissions.CheckPermission(id, GetAspNetUserId(), PermissionOptions.User))
            {
                return Redirect("~/NotAuthorized");
            }

            var user = _userManager.Users.Include(e=>e.Roles).SingleOrDefault(e=>e.Id==userinfo.AspNetUserId.ToString());
            var role = _roleManager.FindByIdAsync(user.Roles.First().RoleId).Result;
            var registerViewModel = new UpdateUserViewModel();
            registerViewModel.FirstName = userinfo.FirstName;
            registerViewModel.LastName = userinfo.LastName;
            //registerViewModel.Address1 = userinfo.Address;
            registerViewModel.AspNetUserId=userinfo.AspNetUserId.ToString();
            registerViewModel.Email = user.Email;
            registerViewModel.OrganizationsID = userinfo.OrganizationsId??0;
            registerViewModel.RoleName = role.Name;
            registerViewModel.Uid = userinfo.Id;
            registerViewModel.oldRole = role.Name;
            SetDropdownList();
            return View(registerViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateUserViewModel model)
        {
            if (!User.IsInRole("Admin") && !_userPermissions.CheckPermission(model.Uid, GetAspNetUserId(), PermissionOptions.User))
            {
                return Redirect("~/NotAuthorized");
            }

            if (ModelState.IsValid)
            {
                if (model.RoleName != "Admin" && model.RoleName != "Guest")
                {
                    if (model.OrganizationsID == null)
                    {
                        TempData["error"] = "Please select organization.";
                        SetDropdownList();
                        return View(model);
                    }
                }

                try
                {
                    var appUser = _userManager.Users.Include(e => e.Roles).SingleOrDefault(e => e.Id == model.AspNetUserId);
                    if (appUser == null)
                    {
                        TempData["error"] = "User not found!";
                        SetDropdownList();
                        return View(model);
                    }
                    appUser.Email = model.Email;
                    appUser.UserName = model.Email;
                    var validEmail = await _userValidator.ValidateAsync(_userManager, appUser);
                    if (!validEmail.Succeeded)
                    {
                        AddErrors(validEmail);
                        SetDropdownList();
                        return View(model);
                    }
                    var userUpdate = await _userManager.UpdateAsync(appUser);
                    if (userUpdate.Succeeded)
                    {
                        if(model.oldRole!=model.RoleName)
                        {
                            await _userManager.RemoveFromRoleAsync(appUser, model.oldRole);
                            var roleAssign = await _userManager.AddToRoleAsync(appUser, model.RoleName);
                            if (!roleAssign.Succeeded)
                            {
                                AddErrors(roleAssign);
                                SetDropdownList();
                                return View(model);
                            }
                        }
                        var userInfo = new Users() { FirstName = model.FirstName, LastName = model.LastName, Id = model.Uid/*, Address = model.Address1*/, OrganizationsId = model.OrganizationsID, AspNetUserId=appUser.Id };
                        _context.Users.Update(userInfo);
                        var result = _context.SaveChanges();
                        if (result > 0)
                        {
                            //$ TODO: Add change reason if necessary
                            LogBook.LogEdit(GetFloodzillaUserId(), GetUserEmail(), LogBookObjectType, model.Email, null);
                            TempData["success"] = "User successfully updated!";
                            _applicationCache.RemoveCache(CacheOptions.Users);
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["error"] = "Something went wrong!";
                        }
                    }
                    else
                    {
                        AddErrors(userUpdate);
                    }
                }
                catch (Exception)
                {
                    TempData["error"] = "Something went wrong!";
                }
            }
            SetDropdownList();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string deleteList)
        {
            try
            {
                var ids = deleteList.Split(',').Select(int.Parse).ToList();
                var users = _applicationCache.GetUsers();
                users = users.Where(e => ids.Contains(e.Id)).ToList();
                if (users.Count > 0)
                {
                    users.ForEach(e => e.AspNetUser.LockoutEnd = DateTimeOffset.MaxValue);
                    users.ForEach(e => e.IsDeleted = true);

                    users.ForEach(e => {
                        LogBook.LogDelete(GetFloodzillaUserId(), GetUserEmail(), LogBookObjectType, e.AspNetUser.Email, null);
                    });

                    _context.Users.UpdateRange(users);

                    var dataSubscription = _applicationCache.GetDataSubscriptions();
                    dataSubscription = dataSubscription.Where(e => ids.Contains(e.UserId)).ToList();
                    if (dataSubscription.Count > 0)
                    {
                        dataSubscription.ForEach(e => e.IsDeleted = true);
                        _context.DataSubscriptions.UpdateRange(dataSubscription);
                        _applicationCache.RemoveCache(CacheOptions.DataSubscriptions, true);
                    }

                    await _context.SaveChangesAsync();
                    _applicationCache.RemoveCache(CacheOptions.Users, true);
                    if (ids.Count == 1)
                    {
                        TempData["success"] = $"User successfully deleted!";
                    }
                    else
                    {
                        TempData["success"] = $"{ids.Count} users successfully deleted!";
                    }
                }
            }
            catch (Exception)
            {
                TempData["error"] = "Something went wrong. Please contact SVPA.";
            }
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin,Organization Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Undelete(string undeleteList)
        {
            IEnumerable<int> undeleteIds = undeleteList.Split(',').Select(int.Parse);
            try
            {
                using (SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]))
                {
                    await sqlcn.OpenAsync();
                    await UserBase.MarkUsersAsUndeleted(sqlcn, undeleteIds);
                    //$ TODO: Add change reason if necessary
                    //$ TODO: If this happens a lot, use email addresses instead of just IDs here
                    LogBook.LogUndeleteObjectList(GetFloodzillaUserId(), GetUserEmail(), LogBookObjectType, undeleteIds, null);
                    sqlcn.Close();
                }
                if (undeleteIds.Count() == 1)
                {
                    TempData["success"] = $"User successfully restored!";
                }
                else
                {
                    TempData["success"] = $"{undeleteIds.Count()} Users successfully restored!";
                }
            }
            catch (Exception)
            {
                TempData["error"] = "Something went wrong. Please contact SVPA.";
            }
            _applicationCache.RemoveCache(CacheOptions.Users, true);
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin")]
        public IActionResult GetUsers(bool showDeleted)
        {
            return Ok(JsonConvert.SerializeObject(new { Data = GetUserList(showDeleted) }));
        }

        [NonAction]
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
