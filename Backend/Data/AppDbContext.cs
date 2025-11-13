using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<WebAuthnCredential> WebAuthnCredentials { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -----------------------------
            // Users table configuration
            // -----------------------------
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                // Columns
                entity.Property(u => u.Name)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(u => u.Email)
                      .HasMaxLength(255)
                      .IsRequired();

                entity.HasIndex(u => u.Email)
                      .IsUnique();

                entity.Property(u => u.PasswordHash)
                      .HasColumnName("password_hash");

                entity.Property(u => u.CreatedAt)
                      .HasColumnName("created_at")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(u => u.UpdatedAt)
                      .HasColumnName("updated_at")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Navigation property configured automatically
            });

            // -----------------------------
            // WebAuthnCredentials table configuration
            // -----------------------------
            modelBuilder.Entity<WebAuthnCredential>(entity =>
            {
                entity.ToTable("webauthn_credentials");

                entity.HasKey(w => w.Id);

                entity.Property(w => w.CredentialId)
                      .IsRequired();

                entity.HasIndex(w => w.CredentialId)
                      .IsUnique();

                entity.Property(w => w.PublicKey)
                      .IsRequired();

                entity.Property(w => w.SignCount)
                      .HasDefaultValue(0);

                entity.Property(w => w.CreatedAt)
                      .HasColumnName("created_at")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Configure foreign key to users table
                entity.HasOne(w => w.User)
                      .WithMany(u => u.WebAuthnCredentials)
                      .HasForeignKey(w => w.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
