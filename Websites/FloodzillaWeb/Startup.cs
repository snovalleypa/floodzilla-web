using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using FloodzillaWeb.Data;
using FloodzillaWeb.Models;
using FzCommon;

namespace FloodzillaWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            FzConfig.Initialize();

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new List<JsonConverter> { new StringEnumConverter() { AllowIntegerValues = false }},
            };

        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //$ TODO: figure out whether we want to enable this globally like this or not. It allows
            //$ local (i.e. http://localhost:3000) development of web apps.
            services.AddCors(options =>
            {
                options.AddPolicy("_CorsPolicy",
                                  builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                });
            });
            
            services.AddControllersWithViews().AddNewtonsoftJson();

            services.AddScoped<IdentityUser, ApplicationUser>();
            services.AddScoped<UserPermissions>();

            // This is to work around the use of UserId in old ASPNET schema vs ApplicationUserId
            // in new schema.
            services.AddScoped<
                    IdentityDbContext<ApplicationUser,
                                      IdentityRole,
                                      string,
                                      IdentityUserClaim<string>,
                                      ApplicationUserRole,
                                      IdentityUserLogin<string>,
                                      IdentityRoleClaim<string>,
                                      IdentityUserToken<string>>,
                    ApplicationDbContext>();

            services.AddDbContext<FloodzillaContext>(options =>
                     options.UseSqlServer(FzConfig.Config[FzConfig.Keys.SqlConnectionString]));

            services.AddDbContext<ApplicationDbContext>(options =>
                     options.UseSqlServer(FzConfig.Config[FzConfig.Keys.SqlConnectionString]));
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = false;
            })
                    .AddEntityFrameworkStores<ApplicationDbContext>()
                    .AddDefaultTokenProviders();

            var key = System.Text.Encoding.ASCII.GetBytes(FzConfig.Config[FzConfig.Keys.JwtTokenKey]);

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie()
                .AddJwtBearer(bearerOptions =>
                {
                    bearerOptions.RequireHttpsMetadata = false;
                    bearerOptions.SaveToken = true;
                    bearerOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                    bearerOptions.Events = new JwtBearerEvents()
                    {
                        OnTokenValidated = async context =>
                        {
                            await JwtManager.OnTokenValidated(context);
                        },
                    };
                });

            services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = "/NotAuthorized";
                options.LoginPath = "/Account/Login";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();

            app.UseCors(x => x
                           .AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader());

            app.UseDefaultFiles();
            app.UseStaticFiles();
            
            app.UseRouting();

            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapFallbackToFile("/index.html");
                endpoints.MapFallbackToFile("Admin/{*path:nonfile}", "/admin-react/index.html");
            });

        }
    }
}
