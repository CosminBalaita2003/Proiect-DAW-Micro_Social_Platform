using Micro_Social_Platform.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Micro_Social_Platform.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser> //PASUL3 mosteneste IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
         public DbSet<FollowRequest> FollowRequests { get; set; }
        

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //definire primary key compus
            modelBuilder.Entity<UserGroup>()
                .HasKey(ab => new {
                    ab.Id,
                    ab.GroupId,
                    ab.UserId
                });

            //definire relatii cu modelele User si Group (FK)

            modelBuilder.Entity<UserGroup>()
                .HasOne(ab => ab.User)
                .WithMany(ab => ab.UserGroups)
                .HasForeignKey(ab => ab.UserId);

            modelBuilder.Entity<UserGroup>()
                .HasOne(ab => ab.Group)
                .WithMany(ab => ab.UserGroups)
                .HasForeignKey(ab => ab.GroupId);
            //definire primary key compus pt follow
            modelBuilder.Entity<FollowRequest>()
                .HasKey(fr => new
                {
                    fr.Id,
                    fr.SenderId,
                    fr.ReceiverId
                });
            modelBuilder.Entity<FollowRequest>()
            .HasOne(fr => fr.Sender)
            .WithMany(u => u!.SentFollowRequests)
            .HasForeignKey(fr => fr.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FollowRequest>()
                .HasOne(fr => fr.Receiver)
                .WithMany(u => u!.ReceivedFollowRequests)
                .HasForeignKey(fr => fr.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

        }


    }
}