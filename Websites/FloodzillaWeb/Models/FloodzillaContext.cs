using Microsoft.EntityFrameworkCore;

using FloodzillaWeb.Models.FzModels;

namespace FloodzillaWeb.Models
{
    public partial class FloodzillaContext : DbContext
    {
        public FloodzillaContext(DbContextOptions<FloodzillaContext> opt) : base(opt)
        {
            Database.SetCommandTimeout(9000);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AspNetRoleClaims>(entity =>
            {
                entity.HasIndex(e => e.RoleId)
                    .HasName("IX_AspNetRoleClaims_RoleId");

                entity.Property(e => e.RoleId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.AspNetRoleClaims)
                    .HasForeignKey(d => d.RoleId);
            });

            modelBuilder.Entity<AspNetRoles>(entity =>
            {
                entity.HasIndex(e => e.NormalizedName)
                    .HasName("RoleNameIndex");

                entity.Property(e => e.Id).HasMaxLength(450);

                entity.Property(e => e.Name).HasMaxLength(256);

                entity.Property(e => e.NormalizedName).HasMaxLength(256);
            });

            modelBuilder.Entity<AspNetUserClaims>(entity =>
            {
                entity.HasIndex(e => e.UserId)
                    .HasName("IX_AspNetUserClaims_UserId");

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserClaims)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<AspNetUserLogins>(entity =>
            {
                entity.HasKey(e => new { e.LoginProvider, e.ProviderKey })
                    .HasName("PK_AspNetUserLogins");

                entity.HasIndex(e => e.UserId)
                    .HasName("IX_AspNetUserLogins_UserId");

                entity.Property(e => e.LoginProvider).HasMaxLength(450);

                entity.Property(e => e.ProviderKey).HasMaxLength(450);

                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserLogins)
                    .HasForeignKey(d => d.UserId);
            });

            modelBuilder.Entity<AspNetUserRoles>(entity =>
            {
                entity.HasKey(e => new { e.ApplicationUserId, e.RoleId })
                    .HasName("PK_AspNetUserRoles");

//                entity.HasIndex(e => e.RoleId)
//                    .HasName("IX_AspNetUserRoles_RoleId");

//                entity.HasIndex(e => e.UserId)
//                    .HasName("IX_AspNetUserRoles_UserId");

//                entity.Property(e => e.UserId).HasMaxLength(450);

                entity.Property(e => e.RoleId).HasMaxLength(450);

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.AspNetUserRoles)
                    .HasForeignKey(d => d.RoleId);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AspNetUserRoles)
                    .HasForeignKey(d => d.ApplicationUserId);
            });

            modelBuilder.Entity<AspNetUserTokens>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name })
                    .HasName("PK_AspNetUserTokens");

                entity.Property(e => e.UserId).HasMaxLength(450);

                entity.Property(e => e.LoginProvider).HasMaxLength(450);

                entity.Property(e => e.Name).HasMaxLength(450);
            });

            modelBuilder.Entity<AspNetUsers>(entity =>
            {
                entity.HasIndex(e => e.NormalizedEmail)
                    .HasName("EmailIndex");

                entity.HasIndex(e => e.NormalizedUserName)
                    .HasName("UserNameIndex")
                    .IsUnique();

                entity.Property(e => e.Id).HasMaxLength(450);

                entity.Property(e => e.Email).HasMaxLength(256);

                entity.Property(e => e.NormalizedEmail).HasMaxLength(256);

                entity.Property(e => e.NormalizedUserName)
                    .IsRequired()
                    .HasMaxLength(256);

                entity.Property(e => e.UserName).HasMaxLength(256);
            });

            modelBuilder.Entity<DeviceTypes>(entity =>
            {
                entity.HasKey(e => e.DeviceTypeId)
                    .HasName("PK_DeviceTypes");

                entity.Property(e => e.DeviceTypeId).ValueGeneratedNever();

                entity.Property(e => e.DeviceTypeName).HasMaxLength(50);
            });

            modelBuilder.Entity<Devices>(entity =>
            {
                entity.HasKey(e => e.DeviceId);

                entity.HasIndex(e => e.LocationId)
                    .HasName("LocationID_unique")
                    .IsUnique();

                entity.Property(e => e.DeviceId).ValueGeneratedNever();

                entity.Property(e => e.Imei)
                    .HasColumnName("IMEI")
                    .HasMaxLength(16);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasOne(d => d.Location)
                    .WithOne(p => p.Devices)
                    .HasForeignKey<Devices>(d => d.LocationId)
                    .HasConstraintName("FK_Devices_Locations");

                entity.HasOne(d => d.DeviceType)
                    .WithMany(p => p.Devices)
                    .HasForeignKey(d => d.DeviceTypeId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Devices_DeviceTypes");

            });

            modelBuilder.Entity<DevicesConfiguration>(entity =>
            {
                entity.HasKey(e => e.DeviceId)
                    .HasName("PK_DevicesConfiguration");

                entity.Property(e => e.DeviceId).ValueGeneratedNever();

                entity.Property(e => e.AdctestsCount).HasColumnName("ADCTestsCount");

                entity.Property(e => e.SecBetweenAdcsense).HasColumnName("SecBetweenADCSense");

                entity.HasOne(d => d.Device)
                    .WithOne(p => p.DevicesConfiguration)
                    .HasForeignKey<DevicesConfiguration>(d => d.DeviceId)
                    .HasConstraintName("FK_DevicesConfiguration_Devices");
            });

            modelBuilder.Entity<FloodEvents>(entity =>
            {
                entity.Property(e => e.EventName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.FromDate).HasColumnType("date");

                entity.Property(e => e.LocationIds)
                    .IsRequired();

                entity.Property(e => e.ToDate).HasColumnType("date");

                entity.HasOne(d => d.Region)
                    .WithMany(p => p.FloodEvents)
                    .HasForeignKey(d => d.RegionId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK__FloodEven__Regio__4F47C5E3");

            });

            modelBuilder.Entity<DataSubscriptions>(entity =>
            {
                entity.HasIndex(e => e.UserId)
                    .HasName("IX_UserId")
                    .IsUnique();

                entity.Property(e => e.FzPostUrl)
                    .IsRequired()
                    .HasColumnType("varchar(max)");

                entity.Property(e => e.IsDeleted).HasDefaultValueSql("0");

                entity.Property(e => e.IsSubscribe).HasDefaultValueSql("1");

                entity.HasOne(d => d.User)
                    .WithOne(p => p.DataSubscriptions)
                    .HasForeignKey<DataSubscriptions>(d => d.UserId)
                    .HasConstraintName("FK_DataSubscriptions_Users");
            });

            modelBuilder.Entity<ElevationTypes>(entity =>
            {
                entity.HasKey(e => e.ElevationTypeId)
                    .HasName("PK_ElevationTypes");

                entity.Property(e => e.ElevationTypeName)
                    .IsRequired()
                    .HasColumnType("varchar(100)");
            });

            modelBuilder.Entity<Elevations>(entity =>
            {
                entity.HasKey(e => e.ElevationId)
                    .HasName("PK_Elevations");

                entity.Property(e => e.ElevationName)
                    .IsRequired()
                    .HasColumnType("varchar(max)");

                entity.HasOne(d => d.ElevationType)
                    .WithMany(p => p.Elevations)
                    .HasForeignKey(d => d.ElevationTypeId)
                    .HasConstraintName("FK_Elevations_ElevationTypes");
            });

            modelBuilder.Entity<FzLevel>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.CreatedOn)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("getutcdate()");

                entity.Property(e => e.L1).HasColumnName("l1");

                entity.Property(e => e.L10).HasColumnName("l10");

                entity.Property(e => e.L11).HasColumnName("l11");

                entity.Property(e => e.L12).HasColumnName("l12");

                entity.Property(e => e.L13).HasColumnName("l13");

                entity.Property(e => e.L14).HasColumnName("l14");

                entity.Property(e => e.L15).HasColumnName("l15");

                entity.Property(e => e.L16).HasColumnName("l16");

                entity.Property(e => e.L17).HasColumnName("l17");

                entity.Property(e => e.L18).HasColumnName("l18");

                entity.Property(e => e.L19).HasColumnName("l19");

                entity.Property(e => e.L2).HasColumnName("l2");

                entity.Property(e => e.L20).HasColumnName("l20");

                entity.Property(e => e.L21).HasColumnName("l21");

                entity.Property(e => e.L22).HasColumnName("l22");

                entity.Property(e => e.L23).HasColumnName("l23");

                entity.Property(e => e.L24).HasColumnName("l24");

                entity.Property(e => e.L25).HasColumnName("l25");

                entity.Property(e => e.L26).HasColumnName("l26");

                entity.Property(e => e.L27).HasColumnName("l27");

                entity.Property(e => e.L28).HasColumnName("l28");

                entity.Property(e => e.L29).HasColumnName("l29");

                entity.Property(e => e.L3).HasColumnName("l3");

                entity.Property(e => e.L30).HasColumnName("l30");

                entity.Property(e => e.L31).HasColumnName("l31");

                entity.Property(e => e.L32).HasColumnName("l32");

                entity.Property(e => e.L33).HasColumnName("l33");

                entity.Property(e => e.L34).HasColumnName("l34");

                entity.Property(e => e.L35).HasColumnName("l35");

                entity.Property(e => e.L36).HasColumnName("l36");

                entity.Property(e => e.L37).HasColumnName("l37");

                entity.Property(e => e.L38).HasColumnName("l38");

                entity.Property(e => e.L39).HasColumnName("l39");

                entity.Property(e => e.L4).HasColumnName("l4");

                entity.Property(e => e.L40).HasColumnName("l40");

                entity.Property(e => e.L41).HasColumnName("l41");

                entity.Property(e => e.L42).HasColumnName("l42");

                entity.Property(e => e.L43).HasColumnName("l43");

                entity.Property(e => e.L44).HasColumnName("l44");

                entity.Property(e => e.L45).HasColumnName("l45");

                entity.Property(e => e.L46).HasColumnName("l46");

                entity.Property(e => e.L47).HasColumnName("l47");

                entity.Property(e => e.L48).HasColumnName("l48");

                entity.Property(e => e.L49).HasColumnName("l49");

                entity.Property(e => e.L5).HasColumnName("l5");

                entity.Property(e => e.L50).HasColumnName("l50");

                entity.Property(e => e.L51).HasColumnName("l51");

                entity.Property(e => e.L52).HasColumnName("l52");

                entity.Property(e => e.L53).HasColumnName("l53");

                entity.Property(e => e.L54).HasColumnName("l54");

                entity.Property(e => e.L55).HasColumnName("l55");

                entity.Property(e => e.L56).HasColumnName("l56");

                entity.Property(e => e.L57).HasColumnName("l57");

                entity.Property(e => e.L58).HasColumnName("l58");

                entity.Property(e => e.L59).HasColumnName("l59");

                entity.Property(e => e.L6).HasColumnName("l6");

                entity.Property(e => e.L60).HasColumnName("l60");

                entity.Property(e => e.L61).HasColumnName("l61");

                entity.Property(e => e.L62).HasColumnName("l62");

                entity.Property(e => e.L63).HasColumnName("l63");

                entity.Property(e => e.L64).HasColumnName("l64");

                entity.Property(e => e.L65).HasColumnName("l65");

                entity.Property(e => e.L66).HasColumnName("l66");

                entity.Property(e => e.L67).HasColumnName("l67");

                entity.Property(e => e.L68).HasColumnName("l68");

                entity.Property(e => e.L69).HasColumnName("l69");

                entity.Property(e => e.L7).HasColumnName("l7");

                entity.Property(e => e.L70).HasColumnName("l70");

                entity.Property(e => e.L71).HasColumnName("l71");

                entity.Property(e => e.L72).HasColumnName("l72");

                entity.Property(e => e.L73).HasColumnName("l73");

                entity.Property(e => e.L74).HasColumnName("l74");

                entity.Property(e => e.L75).HasColumnName("l75");

                entity.Property(e => e.L76).HasColumnName("l76");

                entity.Property(e => e.L77).HasColumnName("l77");

                entity.Property(e => e.L78).HasColumnName("l78");

                entity.Property(e => e.L79).HasColumnName("l79");

                entity.Property(e => e.L8).HasColumnName("l8");

                entity.Property(e => e.L80).HasColumnName("l80");

                entity.Property(e => e.L81).HasColumnName("l81");

                entity.Property(e => e.L82).HasColumnName("l82");

                entity.Property(e => e.L83).HasColumnName("l83");

                entity.Property(e => e.L84).HasColumnName("l84");

                entity.Property(e => e.L85).HasColumnName("l85");

                entity.Property(e => e.L86).HasColumnName("l86");

                entity.Property(e => e.L87).HasColumnName("l87");

                entity.Property(e => e.L88).HasColumnName("l88");

                entity.Property(e => e.L89).HasColumnName("l89");

                entity.Property(e => e.L9).HasColumnName("l9");

                entity.Property(e => e.L90).HasColumnName("l90");
            });

            modelBuilder.Entity<Header>(entity =>
            {
                entity.Property(e => e.AggNote).HasMaxLength(512);

                entity.Property(e => e.DeviceId).HasDefaultValueSql("1");

                entity.Property(e => e.Note).HasMaxLength(512);

                entity.Property(e => e.TimeBetweenAdc).HasColumnName("TimeBetweenADC");

                entity.Property(e => e.Timestamp)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("getutcdate()");
            });

            modelBuilder.Entity<LocationImages>(entity =>
            {
                entity.HasKey(e => e.LocationId)
                    .HasName("PK_LocationImages");

                entity.Property(e => e.LocationId).ValueGeneratedNever();

                entity.Property(e => e.Image1).HasColumnType("varchar(max)");

                entity.Property(e => e.Image2).HasColumnType("varchar(max)");

                entity.Property(e => e.Image3).HasColumnType("varchar(max)");

                entity.Property(e => e.Image4).HasColumnType("varchar(max)");

                entity.HasOne(d => d.Location)
                    .WithOne(p => p.LocationImages)
                    .HasForeignKey<LocationImages>(d => d.LocationId)
                    .HasConstraintName("FK_LocationImages_Locations");
            });

            modelBuilder.Entity<Locations>(entity =>
            {
                entity.Property(e => e.Description)
                    .HasMaxLength(300);

                entity.Property(e => e.GroundHeight).HasDefaultValueSql("((0))");

                entity.Property(e => e.LocationName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.TimeZone)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasOne(d => d.Region)
                    .WithMany(p => p.Locations)
                    .HasForeignKey(d => d.RegionId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Locations_Regions");
            });

            modelBuilder.Entity<Organizations>(entity =>
            {
                entity.Property(e => e.OrganizationsId).HasColumnName("OrganizationsID");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnType("varchar(30)");
            });

            modelBuilder.Entity<Regions>(entity =>
            {
                entity.HasKey(e => e.RegionId)
                    .HasName("PK__Regions__3214EC07A4B42177");

                entity.Property(e => e.Address).HasMaxLength(256);


                entity.Property(e => e.RegionName).HasMaxLength(150);

                entity.Property(e => e.OrganizationsId).HasColumnName("OrganizationsId");

                entity.HasOne(d => d.Organizations)
                     .WithMany(p => p.Regions)
                     .HasForeignKey(d => d.OrganizationsId)
                     .OnDelete(DeleteBehavior.SetNull)
                     .HasConstraintName("FK_Regions_Organizations");
            });

            modelBuilder.Entity<TempString>(entity =>
            {
                entity.Property(e => e.CreatedOn)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("getutcdate()");

                entity.Property(e => e.Data)
                    .HasColumnName("data")
                    .HasColumnType("text");
            });

            modelBuilder.Entity<Uploads>(entity =>
            {
                entity.Property(e => e.DateOfPicture)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getdate())");

                entity.HasOne(d => d.Location)
                    .WithMany(p => p.Uploads)
                    .HasForeignKey(d => d.LocationId)
                    .HasConstraintName("FK__Uploads__Locatio__65370702");
            });

            modelBuilder.Entity<UserDevices>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.DeviceId, e.OrganizationsId })
                    .HasName("PK_CompositeKeys");

                entity.Property(e => e.OrganizationsId).HasColumnName("OrganizationsID");
            });

            modelBuilder.Entity<UserLocations>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.LocationId, e.RegionId });

                entity.Property(e => e.RegionId).HasColumnName("RegionId");

                entity.HasOne(d => d.Location)
                    .WithMany(p => p.UserLocations)
                    .HasForeignKey(d => d.LocationId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("UserLocation_Locations");

                entity.HasOne(d => d.Regions)
                    .WithMany(p => p.UserLocations)
                    .HasForeignKey(d => d.RegionId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("UserLocation_Organizations");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserLocations)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("UserLocation_Users");
            });

            modelBuilder.Entity<Users>(entity =>
            {
                entity.HasIndex(e => e.AspNetUserId)
                    .HasName("aspuseruD")
                    .IsUnique();

                entity.Property(e => e.Address).HasColumnType("varchar(250)");

                entity.Property(e => e.AspNetUserId)
                    .IsRequired()
                    .HasMaxLength(450);

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasColumnType("varchar(250)");

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasColumnType("varchar(250)");

                entity.Property(e => e.OrganizationsId).HasColumnName("OrganizationsID");

                entity.HasOne(d => d.AspNetUser)
                    .WithOne(p => p.Users)
                    .HasForeignKey<Users>(d => d.AspNetUserId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Users");

                entity.HasOne(d => d.Organizations)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.OrganizationsId)
                    .HasConstraintName("FK_Organization");
            });

            modelBuilder.Entity<Weathers>(entity =>
            {
                entity.HasKey(e => e.WeatherId)
                    .HasName("PK_Weathers");

                entity.Property(e => e.Precip1HourMm).HasColumnName("Precip1HourMM");

                entity.Property(e => e.Timestamp)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("getutcdate()");

                entity.Property(e => e.WeatherStatus).HasMaxLength(150);
            });

            modelBuilder.Entity<vw_HeadersWithWeatherInfo>(entity => { entity.HasKey(e => e.Id); });

            modelBuilder.Entity<UserNotification>().HasKey(u => new { u.UserId, u.ChannelTypeId, u.NotifyTypeId });

            modelBuilder.Entity<SubDomainConfig>().HasKey(c => new { c.SubDomainId, c.ConfigName });

            modelBuilder.Entity<App>(entity =>
            {
                entity.HasKey(e => e.AppId).HasName("PK_Apps");

                entity.Property(e => e.RegisteredOn)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("getutcdate()");
            });

            modelBuilder.Entity<AppsData>(entity =>
            {
                entity.HasKey(e => e.AppsDataId).HasName("PK_AppsData");

                entity.Property(e => e.CreatedOn)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("getutcdate()");

                entity.Property(e => e.ModifiedOn)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("getutcdate()");
            });

            modelBuilder.Entity<LocNotification>(entity =>
           {
               entity.HasKey(e => new { e.UserId, e.ChannelTypeId, e.NotifyTypeId, e.LocationId })
                   .HasName("PK_LocNotifications");

               entity.Property(e => e.Level1SentOn).HasColumnType("datetime");

               entity.Property(e => e.Level2SentOn).HasColumnType("datetime");

               entity.Property(e => e.Level3SentOn).HasColumnType("datetime");

               entity.HasOne(d => d.Location)
                   .WithMany(p => p.LocNotifications)
                   .HasForeignKey(d => d.LocationId)
                   .OnDelete(DeleteBehavior.Cascade);

               entity.HasOne(d => d.Notify)
                   .WithMany(p => p.LocNotifications)
                   .HasForeignKey(d => d.NotifyId)
                   .OnDelete(DeleteBehavior.Cascade);

           });

            modelBuilder.Entity<UserNotification>(entity =>
            {
                entity.HasKey(e => e.NotifyId)
                    .HasName("PK_UserNotifications");

                entity.HasOne(d => d.ChannelType)
                    .WithMany(p => p.UserNotifications)
                    .HasForeignKey(d => d.ChannelTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.NotifyType)
                    .WithMany(p => p.UserNotifications)
                    .HasForeignKey(d => d.NotifyTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.UserNotifications)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(e => e.LocNotifications).WithOne(e => e.Notify)
                .HasForeignKey(d => d.NotifyId).OnDelete(DeleteBehavior.Cascade);

            });

            modelBuilder.Entity<EventsDetail>(entity => {
                entity.HasKey(e => new { e.EventId, e.LocationId });
                entity.HasOne(e => e.Floodevent).WithMany(e => e.EventsDetail).HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Location).WithMany(e => e.EventsDetail).HasForeignKey(e => e.LocationId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<LocationNote>(entity => {
                entity.HasKey(e => e.NoteId);
                entity.Property(e => e.CreatedOn)
                        .HasColumnType("datetime")
                        .HasDefaultValueSql("getutcdate()");
            });
        }

        public virtual DbSet<AspNetRoleClaims> AspNetRoleClaims { get; set; }
        public virtual DbSet<AspNetRoles> AspNetRoles { get; set; }
        public virtual DbSet<AspNetUserClaims> AspNetUserClaims { get; set; }
        public virtual DbSet<AspNetUserLogins> AspNetUserLogins { get; set; }
        public virtual DbSet<AspNetUserRoles> AspNetUserRoles { get; set; }
        public virtual DbSet<AspNetUserTokens> AspNetUserTokens { get; set; }
        public virtual DbSet<AspNetUsers> AspNetUsers { get; set; }
        public virtual DbSet<DataSubscriptions> DataSubscriptions { get; set; }
        public virtual DbSet<DeviceTypes> DeviceTypes { get; set; }
        public virtual DbSet<Devices> Devices { get; set; }
        public virtual DbSet<ElevationTypes> ElevationTypes { get; set; }
        public virtual DbSet<Elevations> Elevations { get; set; }
        public virtual DbSet<LocationImages> LocationImages { get; set; }
        public virtual DbSet<DevicesConfiguration> DevicesConfiguration { get; set; }
        public virtual DbSet<FloodEvents> FloodEvents { get; set; }
        public virtual DbSet<FzLevel> FzLevel { get; set; }
        public virtual DbSet<Header> Headers { get; set; }
        public virtual DbSet<Locations> Locations { get; set; }
        public virtual DbSet<Organizations> Organizations { get; set; }
        public virtual DbSet<Regions> Regions { get; set; }
        public virtual DbSet<TempString> TempString { get; set; }
        public virtual DbSet<Uploads> Uploads { get; set; }
        public virtual DbSet<UserLocations> UserLocations { get; set; }
        public virtual DbSet<UserDevices> UserDevices { get; set; }
        public virtual DbSet<Users> Users { get; set; }
        public virtual DbSet<Weathers> Weathers { get; set; }
        public virtual DbSet<vw_HeadersWithWeatherInfo> vw_HeadersWithWeatherInfo { get; set; }
        public virtual DbSet<LocNotification> LocNotifications { get; set; }
        public virtual DbSet<UserNotification> UserNotifications { get; set; }
        public virtual DbSet<SubDomain> SubDomains { get; set; }
        public virtual DbSet<SubDomainConfig> SubDomainsConfig { get; set; }
        public virtual DbSet<App> Apps { get; set; }
        public virtual DbSet<AppsData> AppsData { get; set; }
        public virtual DbSet<ChannelType> ChannelTypes { get; set; }
        public virtual DbSet<NotifyType> NotifyTypes { get; set; }
        public virtual DbSet<EventsDetail> EventsDetail { get; set; }
        public virtual DbSet<LocationNote> LocationNotes { get; set; }
    }
}