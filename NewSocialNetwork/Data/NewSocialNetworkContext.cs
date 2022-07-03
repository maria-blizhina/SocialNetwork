using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NewSocialNetwork.Models;

namespace NewSocialNetwork.Data
{
    public class NewSocialNetworkContext : DbContext
    {
        public NewSocialNetworkContext (DbContextOptions<NewSocialNetworkContext> options)
            : base(options)
        {
            //Database.EnsureDeleted();
            //Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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
        }

        public DbSet<NewSocialNetwork.Models.Person>? Person { get; set; }
        public DbSet<NewSocialNetwork.Models.Follower>? Follower { get; set; }
        public DbSet<NewSocialNetwork.Models.Message>? Message { get; set; }

    }
}
