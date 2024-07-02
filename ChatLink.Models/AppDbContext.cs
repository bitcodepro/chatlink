using ChatLink.Models.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChatLink.Models;

public class AppDbContext : IdentityDbContext<User>
{
    public DbSet<Session> Sessions { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<Message> Messages { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(entity =>
        {
            entity.Property(x => x.Login).HasMaxLength(20).IsRequired();
            entity.HasIndex(x => x.Login).IsUnique();

            entity.Property(x => x.UserName).HasMaxLength(20);
        });

        builder.Entity<Session>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(20).IsRequired();
        });


        builder.Entity<UserSession>(entity =>
        {
            entity.HasKey(us => new { us.UserId, us.SessionId });

            entity.HasOne(us => us.User)
                .WithMany(u => u.UserSessions)
                .HasForeignKey(us => us.UserId);
        });

        builder.Entity<Message>(entity =>
        {
            entity.Property(x => x.SessionId).IsRequired();
            entity.Property(x => x.Email).IsRequired();

            entity.HasOne(us => us.Session)
                .WithMany(u => u.Messages)
                .HasForeignKey(us => us.SessionId);
        });
    }
}
