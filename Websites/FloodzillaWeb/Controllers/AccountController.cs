using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using FloodzillaWeb.Cache;
using FloodzillaWeb.Models;
using FloodzillaWeb.Models.FzModels;
using FloodzillaWeb.Services;
using FloodzillaWeb.ViewModels.Account;
using FzCommon;

namespace FloodzillaWeb.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly FloodzillaContext _context;
        private readonly ApplicationCache _applicationCache;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            FloodzillaContext context,
            IMemoryCache memoryCache,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILoggerFactory loggerFactory, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _applicationCache = new ApplicationCache(_context, memoryCache);
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        //
        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = "/Admin")
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewBag.GoogleCaptchaSiteKey = FzConfig.Config[FzConfig.Keys.GoogleCaptchaSiteKey];
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewBag.GoogleCaptchaSiteKey = FzConfig.Config[FzConfig.Keys.GoogleCaptchaSiteKey];
            if (ModelState.IsValid)
            {
                GoogleReCaptcha grc = GoogleReCaptcha.GetResponse(FzConfig.Config[FzConfig.Keys.GoogleCaptchaSecretKey], Request.Form[GoogleReCaptcha.USER_RESPONSE_FORM_FIELD]);
                if (grc == null) return View("Error");
                if (!grc.Success)
                {
                    TempData["error"] = "Captcha not verified. Please try again.";
                    return View(model);
                }

                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return Redirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    //$ TODO: Two Factor auth?
//$                    return RedirectToAction(nameof(SendCode), new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    return View("Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [AllowAnonymous]
        public IActionResult Guest()
        {
            ViewBag.GoogleCaptchaSiteKey = FzConfig.Config[FzConfig.Keys.GoogleCaptchaSiteKey];
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guest(GuestRegisterViewModel vm)
        {
            if (ModelState.IsValid)
            {
                GoogleReCaptcha grc = GoogleReCaptcha.GetResponse(FzConfig.Config[FzConfig.Keys.GoogleCaptchaSecretKey], Request.Form[GoogleReCaptcha.USER_RESPONSE_FORM_FIELD]);
                if (grc == null) return View("Error");
                if (!grc.Success)
                {
                    TempData["error"] = "Captcha not verified. Please try again.";
                    ViewBag.GoogleCaptchaSiteKey = FzConfig.Config[FzConfig.Keys.GoogleCaptchaSiteKey];
                    return View(vm);
                }

                CreateUserResult cur = await this.CreateUserAsync(vm.Email, vm.Password, vm.FirstName, vm.LastName, null, false);
                if (cur.Succeeded)
                {
                    return View("GuestAccountSuccess");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "An error occurred when creating this user.");
                }

            }
            ViewBag.GoogleCaptchaSiteKey = FzConfig.Config[FzConfig.Keys.GoogleCaptchaSiteKey];
            return View(vm);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            return Redirect("/Admin");
        }

        //
        // GET: /Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            ViewBag.GoogleCaptchaSiteKey = FzConfig.Config[FzConfig.Keys.GoogleCaptchaSiteKey];
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                GoogleReCaptcha grc = GoogleReCaptcha.GetResponse(FzConfig.Config[FzConfig.Keys.GoogleCaptchaSecretKey], Request.Form[GoogleReCaptcha.USER_RESPONSE_FORM_FIELD]);
                if (grc == null) return View("Error");
                if (!grc.Success)
                {
                    TempData["error"] = "Captcha not verified. Please try again.";
                    ViewBag.GoogleCaptchaSiteKey = FzConfig.Config[FzConfig.Keys.GoogleCaptchaSiteKey];
                    return View(model);
                }

                var user = await _userManager.FindByNameAsync(model.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
                Users userinfo = (from u in _context.Users where u.AspNetUserId == user.Id select u).FirstOrDefault();

                ResetPasswordEmailModel rpm = new ResetPasswordEmailModel()
                {
                    FirstName = userinfo.FirstName,
                    LastName = userinfo.LastName,
                    CallbackUrl = callbackUrl,
                };
                using SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]);
                await sqlcn.OpenAsync();
                NotificationManager nm = new();
                await nm.SendEmailModelToRecipientList(sqlcn,
                                                       rpm,
                                                       FzConfig.Config[FzConfig.Keys.EmailFromAddress],
                                                       model.Email);
                return View("ForgotPasswordConfirmation");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(AccountController.ResetPasswordConfirmation), "Account");
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(AccountController.ResetPasswordConfirmation), "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/SendCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl = null, bool rememberMe = false)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            var userFactors = await _userManager.GetValidTwoFactorProvidersAsync(user);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

#if LATER
        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }

            // Generate the token and send it
            var code = await _userManager.GenerateTwoFactorTokenAsync(user, model.SelectedProvider);
            if (string.IsNullOrWhiteSpace(code))
            {
                return View("Error");
            }

            var message = "Your security code is: " + code;
            if (model.SelectedProvider == "Email")
            {
                await _emailSender.SendEmailAsync(m_AppConfig.FromEmail, await _userManager.GetEmailAsync(user), "Security Code", message, m_AppConfig.SendGridApiKey);
            }
            else if (model.SelectedProvider == "Phone")
            {
                await _smsSender.SendSmsAsync(await _userManager.GetPhoneNumberAsync(user), message);
            }

            return RedirectToAction(nameof(VerifyCode), new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }
#endif
        
        //
        // GET: /Account/VerifyCode
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyCode(string provider, bool rememberMe, string returnUrl = null)
        {
            // Require that the user has already logged in via username/password or external login
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes.
            // If a user enters incorrect codes for a specified amount of time then the user account
            // will be locked out for a specified amount of time.
            var result = await _signInManager.TwoFactorSignInAsync(model.Provider, model.Code, model.RememberMe, model.RememberBrowser);
            if (result.Succeeded)
            {
                return Redirect(model.ReturnUrl);
            }
            if (result.IsLockedOut)
            {
                //$ TODO: How should we notify about this kind of thing?
                return View("Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid code.");
                return View(model);
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        public class AuthenticateModel
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public bool RememberMe { get; set; }
            public string CaptchaToken { get; set; }
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Authenticate([FromBody]AuthenticateModel model)
        {
            try
            {
                //$ hack to allow development of mobile version while we work on figuring out captcha
                if (model.CaptchaToken != "mobile login")
                {
                    GoogleReCaptcha grc = GoogleReCaptcha.GetResponse(FzConfig.Config[FzConfig.Keys.GoogleInvisibleCaptchaSecretKey], model.CaptchaToken);
                    if (grc == null || !grc.Success)
                    {
                        return Unauthorized("Captcha not verified. Please try again.");
                    }
                }

                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, true, lockoutOnFailure: false);
                if (!result.Succeeded)
                {
                    return Unauthorized("The username or password was incorrect.");
                }

                ApplicationUser user = await _userManager.FindByNameAsync(model.Username);

                SessionAuthInfo sai = await JwtManager.CreateSessionAuthInfo(_userManager, _context, user, model.RememberMe, null);
                return Ok(sai);
            }
            catch
            {
                //$ TODO: Log this exception somewhere?
                return BadRequest("An error occurred while processing this request.");
            }
        }

        public class CreateAccountModel
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string Phone { get; set; }
            public bool RememberMe { get; set; }
            public string CaptchaToken { get; set; }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateAccount([FromBody]CreateAccountModel model)
        {
            try
            {
                //$ hack to allow development of mobile version while we work on figuring out captcha
                if (model.CaptchaToken != "mobile login")
                {
                    GoogleReCaptcha grc = GoogleReCaptcha.GetResponse(FzConfig.Config[FzConfig.Keys.GoogleInvisibleCaptchaSecretKey], model.CaptchaToken);
                    if (grc == null || !grc.Success)
                    {
                        return Unauthorized("Captcha not verified. Please try again.");
                    }
                }

                CreateUserResult cur = await this.CreateUserAsync(model.Username, model.Password, model.FirstName, model.LastName, model.Phone, false);
                if (!cur.Succeeded)
                {
                    if (cur.UserExists)
                    {
                        return Conflict();
                    }
                }
                
                SessionAuthInfo sai = await JwtManager.CreateSessionAuthInfo(_userManager, _context, cur.NewUser, model.RememberMe, null);
                return Ok(sai);
            }
            catch
            {
                //$ TODO: Log this exception somewhere?
                return BadRequest("An error occurred while processing this request.");
            }
        }

        public class ForgotPasswordModel
        {
            public string Email { get; set; }
            public string CaptchaToken { get; set; }
        }

        //$ TODO: Rename this once we remove all the old account-management code...
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> APIForgotPassword([FromBody]ForgotPasswordModel model)
        {
            try
            {
                //$ hack to allow development of mobile version while we work on figuring out captcha
                if (model.CaptchaToken != "mobile login")
                {
                    GoogleReCaptcha grc = GoogleReCaptcha.GetResponse(FzConfig.Config[FzConfig.Keys.GoogleInvisibleCaptchaSecretKey], model.CaptchaToken);
                    if (grc == null || !grc.Success)
                    {
                        return Unauthorized("Captcha not verified. Please try again.");
                    }
                }

                var user = await _userManager.FindByNameAsync(model.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return Ok(new { success = true });
                }

                var code = await _userManager.GeneratePasswordResetTokenAsync(user);

                //$ TODO: This assumes that the API and the website are running at
                //$ the same server/port/scheme.  If that changes, then we'll need
                //$ to put a target server/port/scheme into config...
                string callbackUrl = String.Format("{0}://{1}/user/resetpassword?userId={2}&code={3}",
                                                   Request.Scheme, Request.Host, user.Id, Uri.EscapeDataString(code));
                Users userinfo = (from u in _context.Users where u.AspNetUserId == user.Id select u).FirstOrDefault();

                ResetPasswordEmailModel rpm = new ResetPasswordEmailModel()
                {
                    FirstName = userinfo.FirstName,
                    LastName = userinfo.LastName,
                    CallbackUrl = callbackUrl,
                };
                using SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]);
                await sqlcn.OpenAsync();
                NotificationManager nm = new();
                await nm.SendEmailModelToRecipientList(sqlcn,
                                                       rpm,
                                                       FzConfig.Config[FzConfig.Keys.EmailFromAddress],
                                                       model.Email);
                return Ok(new { success = true });
            }
            catch
            {
                //$ TODO: Log this exception somewhere?
                return BadRequest("An error occurred while processing this request.");
            }
        }

        public class SetPasswordModel
        {
            // These two are only used for Reset mode
            public string UserId { get; set; }
            public string Code { get; set; }

            // This is only used for Set mode
            public string OldPassword { get; set; }

            public string NewPassword { get; set; }
        }

        //$ TODO: Rename this once we remove all the old account-management code...
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> APISetPassword([FromBody]SetPasswordModel model)
        {
            try
            {
                ApplicationUser user = await SecurityHelper.GetApplicationUser(User, _userManager);
                if (user != null)
                {
                    var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        return Ok(new { Success = true });
                    }
                    else if (result.Errors.Any(e => e.Code == "PasswordMismatch"))
                    {
                        return Unauthorized("The username or password was incorrect.");
                    }
                }
                return BadRequest("An error occurred while processing this request.");
            }
            catch
            {
                //$ TODO: Log this exception somewhere?
                return BadRequest("An error occurred while processing this request.");
            }
        }

        //$ TODO: Rename this once we remove all the old account-management code...
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> APICreatePassword([FromBody]SetPasswordModel model)
        {
            try
            {
                ApplicationUser user = await SecurityHelper.GetApplicationUser(User, _userManager);
                if (user != null)
                {
                    var result = await _userManager.AddPasswordAsync(user, model.NewPassword);
                    if (result.Succeeded)
                    {
                        //$ TODO: RememberMe?
                        SessionAuthInfo sai = await JwtManager.CreateSessionAuthInfo(_userManager, _context, user, false, null);
                        return Ok(sai);
                    }
                    else if (result.Errors.Any(e => e.Code == "UserAlreadyHasPassword"))
                    {
                        return Conflict();
                    }
                }
                return BadRequest("An error occurred while processing this request.");
            }
            catch
            {
                //$ TODO: Log this exception somewhere?
                return BadRequest("An error occurred while processing this request.");
            }
        }

        //$ TODO: Rename this once we remove all the old account-management code...
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> APIResetPassword([FromBody]SetPasswordModel model)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user != null)
                {
                    var result = await _userManager.ResetPasswordAsync(user, model.Code, model.NewPassword);
                    if (result.Succeeded)
                    {
                        return Ok(new { Success = true });
                    }
                }
                return BadRequest("An error occurred while processing this request.");
            }
            catch
            {
                //$ TODO: Log this exception somewhere?
                return BadRequest("An error occurred while processing this request.");
            }
        }

        public class SetDevicePushTokenModel
        {
            public string? DeviceId { get; set; }
            public string? Language { get; set; }
            public string Token { get; set; }
            public string Platform { get; set; }
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> SetDevicePushToken([FromBody]SetDevicePushTokenModel model)
        {
            if (model == null)
            {
                return BadRequest("One or more parameters is invalid.");
            }
            if (String.IsNullOrWhiteSpace(model.Token))
            {
                return BadRequest("Token is required.");
            }
            try
            {
                ApplicationUser aspUser = await SecurityHelper.GetApplicationUser(User, _userManager);
                if (aspUser != null)
                {
                    using SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]);
                    await sqlcn.OpenAsync();
                    UserBase user = await UserBase.GetUserForAspNetUserAsync(sqlcn, aspUser.Id);
                    try
                    {
                        await PushDeviceLog.Create(sqlcn,
                                                   DateTime.Now,
                                                   Environment.MachineName,
                                                   PushDeviceLog.EntryType_Registered,
                                                   model.Token,
                                                   user.Id,
                                                   model.Platform,
                                                   model.Language,
                                                   "Registered via API");
                    }
                    catch
                    {
                        // We can just eat this -- if we don't log it, we'll survive...
                    }
                    await UserDevicePushToken.EnsureToken(sqlcn, user.Id, model.Token, model.Platform, DateTime.UtcNow, model.Language, model.DeviceId);
                    return Ok();
                }
                else
                {
                    return BadRequest("An error occurred while processing this request.");
                }
            }
            catch
            {
                //$ TODO: Log this exception somewhere?
                return BadRequest("An error occurred while processing this request.");
            }
        }
        
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> SendVerificationEmail()
        {
            try
            {
                ApplicationUser user = await SecurityHelper.GetApplicationUser(User, _userManager);
                if (user != null)
                {
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                    //$ TODO: This assumes that the API and the website are running at
                    //$ the same server/port/scheme.  If that changes, then we'll need
                    //$ to put a target server/port/scheme into config...
                    string callbackUrl = String.Format("{0}://{1}/user/verifyemail?userId={2}&token={3}",
                                                       Request.Scheme, Request.Host, user.Id, Uri.EscapeDataString(token));
                    Users userinfo = (from u in _context.Users where u.AspNetUserId == user.Id select u).FirstOrDefault();
                    VerifyEmailEmailModel vem = new VerifyEmailEmailModel()
                    {
                        FirstName = userinfo.FirstName,
                        LastName = userinfo.LastName,
                        CallbackUrl = callbackUrl,
                    };

                    using SqlConnection sqlcn = new SqlConnection(FzConfig.Config[FzConfig.Keys.SqlConnectionString]);
                    await sqlcn.OpenAsync();
                    NotificationManager nm = new();
                    await nm.SendEmailModelToRecipientList(sqlcn,
                                                           vem,
                                                           FzConfig.Config[FzConfig.Keys.EmailFromAddress],
                                                           user.UserName);
                    return Ok(new { success = true });
                }
                return BadRequest("An error occurred while processing this request.");
            }
            catch
            {
                //$ TODO: Log this exception somewhere?
                return BadRequest("An error occurred while processing this request.");
            }
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> VerifyEmail([FromBody]string token)
        {
            try
            {
                ApplicationUser user = await SecurityHelper.GetApplicationUser(User, _userManager);
                if (user != null)
                {
                    IdentityResult res = await _userManager.ConfirmEmailAsync(user, token);
                    if (res.Succeeded)
                    {
                        return Ok(new { success = true });
                    }
                    else
                    {
                        return BadRequest(res.Errors.First().Description);
                    }
                }
                return BadRequest("An error occurred while processing this request.");
            }
            catch
            {
                //$ TODO: Log this exception somewhere?
                return BadRequest("An error occurred while processing this request.");
            }
        }
        
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> SendPhoneVerificationSms([FromBody]string phone)
        {
            try
            {
                ApplicationUser user = await SecurityHelper.GetApplicationUser(User, _userManager);
                if (user != null)
                {
                    string token = await _userManager.GenerateChangePhoneNumberTokenAsync(user, phone);

                    Users userinfo = (from u in _context.Users where u.AspNetUserId == user.Id select u).FirstOrDefault();
                    VerifyPhoneSmsEmailModel vpsm = new VerifyPhoneSmsEmailModel()
                    {
                        Code = token,
                    };
                    SmsClient client = new SmsClient();
                    SmsSendResult result = await client.SendSms(phone, user.UserName, vpsm);
                    switch (result)
                    {
                        case SmsSendResult.Success:
                            return Ok(new { success = true });
                        case SmsSendResult.InvalidNumber:
                            return BadRequest("Invalid Phone Number.");
                        case SmsSendResult.Failure:
                        case SmsSendResult.NotSending:
                            return StatusCode(500, "An error occurred while processing this request.");
                    }
                }
                return StatusCode(500, "An error occurred while processing this request.");
            }
            catch
            {
                //$ TODO: Log this exception somewhere?
                return StatusCode(500, "An error occurred while processing this request.");
            }
        }

        public class VerifyPhoneModel
        {
            public string Phone { get; set; }
            public string Code { get; set; }
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> VerifyPhone([FromBody]VerifyPhoneModel model)
        {
            try
            {
                ApplicationUser user = await SecurityHelper.GetApplicationUser(User, _userManager);
                if (user != null)
                {
                    IdentityResult res = await _userManager.ChangePhoneNumberAsync(user, model.Phone, model.Code);
                    if (res.Succeeded)
                    {
                        return Ok(new { success = true });
                    }
                    else
                    {
                        return BadRequest(res.Errors.First().Description);
                    }
                }
                return BadRequest("An error occurred while processing this request.");
            }
            catch
            {
                //$ TODO: Log this exception somewhere?
                return BadRequest("An error occurred while processing this request.");
            }
        }
        
        [HttpGet]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Reauthenticate()
        {
            try
            {
                ApplicationUser user = await SecurityHelper.GetApplicationUser(User, _userManager);
                if (user != null)
                {
                    bool rememberMe = JwtManager.GetRememberMeClaim(User.Claims);
                    string loginProvider = JwtManager.GetLoginProviderClaim(User.Claims);
                    SessionAuthInfo sai = await JwtManager.CreateSessionAuthInfo(_userManager, _context, user, rememberMe, loginProvider);
                    return Ok(sai);
                }
                return BadRequest("An error occurred while processing this request.");
            }
            catch
            {
                //$ TODO: Log this exception somewhere?
                return BadRequest("An error occurred while processing this request.");
            }
        }

        // Only updates CreateAccountModel fields that aren't null.  Only handles
        // FirstName, LastName, and Username.
        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> UpdateAccount([FromBody]CreateAccountModel model)
        {
            try
            {
                ApplicationUser user = await SecurityHelper.GetApplicationUser(User, _userManager);
                if (user != null)
                {
                    Users userinfo = (from u in _context.Users where u.AspNetUserId == user.Id select u).FirstOrDefault();
                    if (!String.IsNullOrEmpty(model.FirstName) && !String.IsNullOrEmpty(model.LastName))
                    {
                        userinfo.FirstName = model.FirstName;
                        userinfo.LastName = model.LastName;
                        await _context.SaveChangesAsync();
                    }

                    if (!String.IsNullOrEmpty(model.Username) && (model.Username != user.UserName))
                    {
                        var result = await _userManager.SetUserNameAsync(user, model.Username);
                        if (!result.Succeeded)
                        {
                            if (result.Errors.Any(e => e.Code == "DuplicateUserName"))
                            {
                                return Conflict();
                            }
                            return BadRequest("An error occurred while processing this request.");
                        }
                        result = await _userManager.SetEmailAsync(user, model.Username);
                        if (!result.Succeeded)
                        {
                            if (result.Errors.Any(e => e.Code == "DuplicateUserName"))
                            {
                                return Conflict();
                            }
                            return BadRequest("An error occurred while processing this request.");
                        }
                    }

                    // NOTE: Ignore password changes here...

                    bool rememberMe = JwtManager.GetRememberMeClaim(User.Claims);
                    SessionAuthInfo sai = await JwtManager.CreateSessionAuthInfo(_userManager, _context, user, rememberMe, null);
                    return Ok(sai);
                }
                return BadRequest("An error occurred while processing this request.");
            }
            catch
            {
                //$ TODO: Log this exception somewhere?
                return BadRequest("An error occurred while processing this request.");
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> AuthenticateWithGoogle([FromBody]string idToken)
        {
            try
            {
                JwtSecurityToken jwt = new JwtSecurityToken(idToken);

                string email = GetJwtClaim(jwt, JwtRegisteredClaimNames.Email);
                string firstName = GetJwtClaim(jwt, JwtRegisteredClaimNames.GivenName);
                string lastName = GetJwtClaim(jwt, JwtRegisteredClaimNames.FamilyName);
                string googleUserId = GetJwtClaim(jwt, JwtRegisteredClaimNames.Sub);

                if (!String.IsNullOrEmpty(email))
                {
                    ApplicationUser user = await _userManager.FindByNameAsync(email);
                    if (user == null)
                    {
                        CreateUserResult cur = await this.CreateUserAsync(email, null, firstName, lastName, null, true);
                        if (cur.Succeeded)
                        {
                            user = cur.NewUser;

                            // Associate the new user with the Google userid.
                            UserLoginInfo uli = new UserLoginInfo(Constants.GoogleLoginProviderName,
                                                                  googleUserId,
                                                                  email);
                            await _userManager.AddLoginAsync(user, uli);
                        }
                    }
                    if (user != null)
                    {
                        SessionAuthInfo sai = await JwtManager.CreateSessionAuthInfo(_userManager, _context, user, true, Constants.GoogleLoginProviderName);
                        return Ok(sai);
                    }
                }
                return BadRequest("An error occurred while processing this request.");
            }
            catch
            {
                //$ TODO: Log this exception somewhere?
                return BadRequest("An error occurred while processing this request.");
            }
        }

        public class FacebookAuthModel
        {
            public string UserId { get; set; }
            public string Token { get; set; }
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> AuthenticateWithFacebook([FromBody] FacebookAuthModel model)
        {
            try
            {
                string url = String.Format(FzConfig.Config[FzConfig.Keys.FacebookUserQueryEndpointFormat],
                                           model.UserId,
                                           model.Token);
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    Dictionary<string, string> payload = JsonConvert.DeserializeObject<Dictionary<string, string>>(await response.Content.ReadAsStringAsync());
                    if (!payload.ContainsKey("email"))
                    {
                        // Apparently Facebook accounts aren't required to have an email address;
                        // if this happens, we have to punt.
                        return BadRequest("There is no email address associated with this account.  Please choose another account.");
                    }
                    string email = payload["email"];

                    // If these aren't in the response, we'll just fail out and throw;
                    // if necessary we can try to fake them from 'name' and/or email...
                    string firstName = payload["first_name"];
                    string lastName = payload["last_name"];

                    ApplicationUser user = await _userManager.FindByNameAsync(email);
                    if (user == null)
                    {
                        CreateUserResult cur = await this.CreateUserAsync(email, null, firstName, lastName, null, true);
                        if (cur.Succeeded)
                        {
                            user = cur.NewUser;

                            // Associate the new user with the Facebook userid.
                            UserLoginInfo uli = new UserLoginInfo(Constants.FacebookLoginProviderName,
                                                                  model.UserId,
                                                                  email);
                            await _userManager.AddLoginAsync(user, uli);
                        }
                    }
                    if (user != null)
                    {
                        SessionAuthInfo sai = await JwtManager.CreateSessionAuthInfo(_userManager, _context, user, true, Constants.FacebookLoginProviderName);
                        return Ok(sai);
                    }
                }
                return BadRequest("An error occurred while processing this request.");
            }
            catch
            {
                //$ TODO: Log this exception somewhere?
                return BadRequest("An error occurred while processing this request.");
            }
        }

        private string GetJwtClaim(JwtSecurityToken jwt, string claimType)
        {
            Claim c = jwt.Claims.FirstOrDefault(c => c.Type == claimType);
            if (c == null)
            {
                return null;
            }
            return c.Value;
        }
        
        //$ TODO: Model errors may need to be set here to handle user-exists, etc?
        internal class CreateUserResult
        {
            internal bool Succeeded { get; set; }
            internal bool UserExists { get; set; }
            internal ApplicationUser NewUser { get; set; }
        }   
        
        private async Task<CreateUserResult> CreateUserAsync(string email,
                                                             string password,
                                                             string firstName,
                                                             string lastName,
                                                             string phone,
                                                             bool confirmEmail)
        {
            var user = new ApplicationUser() { Email = email, UserName = email, PhoneNumber = phone, EmailConfirmed = confirmEmail };
            IdentityResult createUserResult;
            if (String.IsNullOrEmpty(password))
            {
                createUserResult = await _userManager.CreateAsync(user);
            }
            else
            {
                createUserResult = await _userManager.CreateAsync(user, password);
            }
            
            if (createUserResult.Succeeded)
            {
                var roleAssign = await _userManager.AddToRoleAsync(user, "Guest");
                if (roleAssign.Succeeded)
                {
                    Users uinfo = new Users();
                    uinfo.FirstName = firstName;
                    uinfo.LastName = lastName;
                    uinfo.AspNetUserId = user.Id;
                    uinfo.NotifyViaEmail = true;
                    uinfo.CreatedOn = DateTime.UtcNow;
                    
                    _context.Users.Add(uinfo);
                    int res = _context.SaveChanges();
                    if (res > 0)
                    {
                        _applicationCache.RemoveCache(CacheOptions.Users);
                    }

                    return new CreateUserResult()
                    {
                        Succeeded = true,
                        NewUser = user,
                    };
                }
            }
            return new CreateUserResult()
            {
                Succeeded = false,
                UserExists = createUserResult.Errors.Any(e => e.Code == "DuplicateUserName"),
            };
        }
    }
}
