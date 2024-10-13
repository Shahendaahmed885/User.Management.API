

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace User.Management.API.Models
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }
      public DbSet<UserProfile> UserProfiles { get; set; }
      public DbSet<MedicalHistory> MedicalHistories { get; set; }
      public DbSet<UploadImages>uploadImages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
           SeedRoles(modelBuilder);
            modelBuilder.Entity<UserProfile>()
            .HasMany(u => u.MedicalHistories)
            .WithOne(m => m.UserProfile)
            .HasForeignKey(m => m.Id);

            modelBuilder.Entity<UserProfile>()
                .HasMany(u => u.UploadImages)
                .WithOne(ui => ui.UserProfile)
                .HasForeignKey(ui => ui.UserProfileId);


        }
        private void SeedRoles(ModelBuilder builder)
        {
            builder.Entity<IdentityRole>().HasData
                (
                new IdentityRole() { Name = "Admin", ConcurrencyStamp = "1", NormalizedName = "Admin" },
                new IdentityRole() { Name = "User", ConcurrencyStamp = "2", NormalizedName = "User" }



                );
        }




    }   
}
