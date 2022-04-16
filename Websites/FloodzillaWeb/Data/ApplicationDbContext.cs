using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using FloodzillaWeb.Models;

namespace FloodzillaWeb.Data
{
    public class ApplicationDbContext
            : IdentityDbContext<ApplicationUser,
                                IdentityRole,
                                string,
                                IdentityUserClaim<string>,
                                ApplicationUserRole,
                                IdentityUserLogin<string>,
                                IdentityRoleClaim<string>,
                                IdentityUserToken<string>>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
                : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // This is to work around the use of UserId in old ASPNET schema vs ApplicationUserId
            // in new schema.
            builder.Entity<ApplicationUserRole>(b =>
            {
                b.HasKey(r => new { r.ApplicationUserId, r.RoleId });
                b.ToTable<ApplicationUserRole>("AspNetUserRoles");
            });

            base.OnModelCreating(builder);
        }
    }
}
