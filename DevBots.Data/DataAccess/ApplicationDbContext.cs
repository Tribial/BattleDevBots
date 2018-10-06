using System;
using System.Collections.Generic;
using System.Text;
using DevBots.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace DevBots.Data.DataAccess
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Robot> Robots { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<Script> Scripts { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<RefreshToken>()
                .HasKey(e => e.Id);

            builder.Entity<User>()
                .HasKey(e => e.Id);

            builder.Entity<Player>()
                .HasKey(e => e.Id);
            builder.Entity<Player>()
                .HasMany(e => e.Matches)
                .WithOne(e => e.Player);
            builder.Entity<Player>()
                .HasMany(e => e.Scripts)
                .WithOne(e => e.Owner);

            builder.Entity<Robot>()
                .HasKey(e => e.Id);
            builder.Entity<Robot>()
                .HasMany(e => e.Skills);

            builder.Entity<Skill>()
                .HasKey(e => e.Id);

            builder.Entity<Script>()
                .HasKey(e => e.Id);
            builder.Entity<Script>()
                .HasOne(e => e.ForRobot);

            builder.Entity<Match>()
                .HasKey(e => e.Id);
            builder.Entity<Match>()
                .HasOne(e => e.Enemy);

        }
    }
}
