using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SocialNetworkApp.Models;

namespace SocialNetworkApp.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            //Database.EnsureDeleted();
            //Database.EnsureCreated();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Message>()
                .HasOne<Person>(m => m.Sender)
                .WithMany(p => p.SendedMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Message>()
                .HasOne<Person>(m => m.Receiver)
                .WithMany(p => p.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<FollowerRecord>().HasKey(fr => new { fr.FollowedPersonId, fr.FollowerPersonId });

            modelBuilder.Entity<FollowerRecord>()
            .HasOne<Person>(fr => fr.FollowedPerson)
            .WithMany(p => p.Followers)
            .HasForeignKey(fr => fr.FollowedPersonId)
            .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<FollowerRecord>()
            .HasOne<Person>(fr => fr.FollowerPerson)
            .WithMany(p => p.Followings)
            .HasForeignKey(fr => fr.FollowerPersonId)
            .OnDelete(DeleteBehavior.NoAction);
        }


        public DbSet<SocialNetworkApp.Models.Person>? Person { get; set; }
        public DbSet<SocialNetworkApp.Models.Message>? Message { get; set; }
        public DbSet<SocialNetworkApp.Models.FollowerRecord>? FollowerRecord { get; set; }

    }
}