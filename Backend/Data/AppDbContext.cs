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
                      .HasColumnName("name")
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(u => u.Email)
                      .HasColumnName("email")
                      .HasMaxLength(255)
                      .IsRequired();

                entity.HasIndex(u => u.Email)
                      .IsUnique();

            // Password column is no longer used
            //  entity.Property(u => u.PasswordHash)
            //        .HasColumnName("password_hash");

                entity.Property(u => u.CreatedAt)
                      .HasColumnName("created_at")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(u => u.UpdatedAt)
                      .HasColumnName("updated_at")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");
                  
                entity.Property(u => u.RecoveryCodeHash)
                      .HasColumnName("recovery_code_hash");

                entity.Property(u => u.RecoveryCodeCreatedAt)
                      .HasColumnName("recovery_code_created_at");

                entity.Property(u => u.RecoveryCodeUsedAt)
                      .HasColumnName("recovery_code_used_at");

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
                      .HasColumnName("credential_id")
                      .IsRequired();

                entity.HasIndex(w => w.CredentialId)
                      .IsUnique();

                entity.Property(w => w.PublicKey)
                      .HasColumnName("public_key")
                      .IsRequired();

                entity.Property(w => w.SignCount)
                      .HasColumnName("sign_count")
                      .HasDefaultValue(0);

                entity.Property(w => w.CreatedAt)
                      .HasColumnName("created_at")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(w => w.UserId)
                      .HasColumnName("user_id");
                // Configure foreign key to users table
                entity.HasOne(w => w.User)
                      .WithMany(u => u.WebAuthnCredentials)
                      .HasForeignKey(w => w.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                  
                  entity.Property(w => w.DeviceName)
                      .HasColumnName("device_name")
                      .HasMaxLength(100)
                      .IsRequired(false);

                  entity.Property(w => w.LastUsedAt)
                      .HasColumnName("last_used_at")
                      .IsRequired(false);
            });
        }
    }
}


