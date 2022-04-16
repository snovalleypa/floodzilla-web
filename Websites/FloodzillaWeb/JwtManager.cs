using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using FloodzillaWeb.Models;
using FloodzillaWeb.Models.FzModels;
using FzCommon;

namespace FloodzillaWeb
{

    //$ do updates need to update anything except token?
    //$ client/session can separately manage all other things except maybe IsAdmin, and that's advisory only

    //$ login provider needs to be in token because server can't generate it when a user returns

    //$ in order to do an update, would like to have access to incoming token expiration date so
    //$ we know if it's necessary to update

    //$ also need to know RememberMe to generate tokens; it needs to be in token?

    public class SessionAuthInfo
    {
        public string Username;
        public string FirstName;
        public string LastName;
        public string Phone;
        public string Token;
        public string LoginProvider;
        public bool IsAdmin;
        public bool HasPassword;
        public bool EmailVerified;
        public bool PhoneVerified;
    }

    public class JwtHeaderFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext.HttpContext.Items[JwtManager.JwtTokenHeader] != null)
            {
                string idToken = (string)actionExecutedContext.HttpContext.Items[JwtManager.JwtTokenHeader];
                actionExecutedContext.HttpContext.Response.Headers.Add(JwtManager.JwtTokenHeader, idToken);
                actionExecutedContext.HttpContext.Response.Headers.Add("Access-Control-Expose-Headers", JwtManager.JwtTokenHeader);
            }

        }
    }

    public class JwtCustomClaims
    {
        public const string LoginProvider = "x-LoginProvider";
        public const string RememberMe = "x-RememberMe";
    }

    public class JwtManager
    {
        // without 'remember me': 1 hour
        public const int ExpirationMinutes = 60;

        // with 'remember me': 3 years
        public const int RememberMeExpirationMinutes = 60 * 24 * 365 * 3;

        // any token older than this will be reissued
        public const int MaxTokenAgeMinutes = 60 * 24;

        // any token about to expire within this many minutes will be reissued
        public const int MinTokenLifetime = 15;

        // custom header used to give the client a refreshed JWT
        public const string JwtTokenHeader = "X-fz-idToken";
        
        public static async Task OnTokenValidated(TokenValidatedContext context)
        {
            // We don't want to put roles straight into the JWT because they can
            // get stale.  So instead we fetch them anew whenever the token is validated.
            ClaimsPrincipal userPrincipal = context.Principal;
            ApplicationUser user = null;
            if (userPrincipal.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
            {
                var uid = userPrincipal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value;
                UserManager<ApplicationUser> userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                user = await userManager.FindByIdAsync(uid);
                if (user != null)
                {
                    foreach (string role in await userManager.GetRolesAsync(user))
                    {
                        ((ClaimsIdentity)userPrincipal.Identity).AddClaim(new Claim(ClaimTypes.Role, role));
                    }
                }
            }

            // We also make sure these claims get pushed into the ASP.NET claims
            // so that they're available to other methods/controllers (like Reauthenticate).
            JwtSecurityToken jwt = (JwtSecurityToken)context.SecurityToken;
            bool rememberMe = false;
            string loginProvider = null;
            foreach (Claim claim in jwt.Claims)
            {
                if (claim.Type == JwtCustomClaims.LoginProvider || claim.Type == JwtCustomClaims.RememberMe)
                {
                    if (claim.Type == JwtCustomClaims.LoginProvider)
                    {
                        loginProvider = claim.Value;
                    }
                    else
                    {
                        if (!Boolean.TryParse(claim.Value, out rememberMe))
                        {
                            rememberMe = false;
                        }
                    }
                    ((ClaimsIdentity)userPrincipal.Identity).AddClaim(claim);
                }
            }

            // Finally, if the token is sufficiently old (for remember-me tokens) or about to expire
            // (for short-term tokens), we issue a new token.
            TimeSpan timeToExpiration = jwt.ValidTo - DateTime.UtcNow;
            TimeSpan tokenAge = DateTime.UtcNow - jwt.ValidFrom;
            if (user != null && tokenAge.TotalMinutes > JwtManager.MaxTokenAgeMinutes || timeToExpiration.TotalMinutes < JwtManager.MinTokenLifetime)
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenDescriptor = JwtManager.CreateIdentityToken(user, rememberMe, loginProvider);
                var token = tokenHandler.CreateToken(tokenDescriptor);
                context.HttpContext.Items[JwtManager.JwtTokenHeader] = tokenHandler.WriteToken(token);
            }
        }

        public static bool GetRememberMeClaim(IEnumerable<Claim> claims)
        {
            Claim c = claims.FirstOrDefault(c => c.Type == JwtCustomClaims.RememberMe);
            if (c == null)
            {
                return false;
            }
            bool ret;
            if (!Boolean.TryParse(c.Value, out ret))
            {
                return false;
            }
            return ret;
        }

        public static string GetLoginProviderClaim(IEnumerable<Claim> claims)
        {
            Claim c = claims.FirstOrDefault(c => c.Type == JwtCustomClaims.LoginProvider);
            if (c == null)
            {
                return null;
            }
            return c.Value;
        }

        public static SecurityTokenDescriptor CreateIdentityToken(ApplicationUser user, bool rememberMe, string loginProvider)
        {
            var key = System.Text.Encoding.ASCII.GetBytes(FzConfig.Config[FzConfig.Keys.JwtTokenKey]);

            var claims = new List<Claim>() 
            {
                new Claim(JwtCustomClaims.RememberMe, rememberMe.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
            };
            if (!String.IsNullOrEmpty(loginProvider))
            {
                claims.Add(new Claim(JwtCustomClaims.LoginProvider, loginProvider));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(rememberMe ? RememberMeExpirationMinutes : ExpirationMinutes),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            return tokenDescriptor;
        }

        //$ TODO: for service separation, consider removing userManager
        //$ TODO: for service separation, consider removing context

        public static async Task<SessionAuthInfo> CreateSessionAuthInfo(UserManager<ApplicationUser> userManager,
                                                                        FloodzillaContext context,
                                                                        ApplicationUser user,
                                                                        bool rememberMe,
                                                                        string loginProvider)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var tokenDescriptor = JwtManager.CreateIdentityToken(user, rememberMe, loginProvider);

            Users userinfo = (from u in context.Users where u.AspNetUserId == user.Id select u).FirstOrDefault();

            bool isAdmin = false;
            foreach (string role in await userManager.GetRolesAsync(user))
            {
                if (role == SecurityRoles.Admin || role == SecurityRoles.OrgAdmin)
                {
                    isAdmin = true;
                }
            }
            bool hasPassword = await userManager.HasPasswordAsync(user);

            var token = tokenHandler.CreateToken(tokenDescriptor);
            SessionAuthInfo sai = new SessionAuthInfo()
            {
                FirstName = userinfo.FirstName,
                LastName = userinfo.LastName,
                Username = user.UserName,
                Phone = user.PhoneNumber,
                Token = tokenHandler.WriteToken(token),
                LoginProvider = loginProvider,
                IsAdmin = isAdmin,
                HasPassword = hasPassword,
                EmailVerified = user.EmailConfirmed,
                PhoneVerified = user.PhoneNumberConfirmed       ,
            };
            return sai;            
        }
    }
}
