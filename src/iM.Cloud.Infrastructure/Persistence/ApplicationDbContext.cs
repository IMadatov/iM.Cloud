using iM.Cloud.Application.Common.Interfaces;
using iM.Cloud.Domain.Authorization;
using iM.Cloud.Domain.Entities;
using FileShareEntity = iM.Cloud.Domain.Entities.FileShare;
using iM.Cloud.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace iM.Cloud.Infrastructure.Persistence;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<UserGroup> UserGroups => Set<UserGroup>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<StorageObject> StorageObjects => Set<StorageObject>();
    public DbSet<FileItem> FileItems => Set<FileItem>();
    public DbSet<FileShareEntity> FileShares => Set<FileShareEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.DisplayName).HasMaxLength(200);
            entity.Property(u => u.CreatedBy).HasMaxLength(200);
            entity.Property(u => u.LastModifiedBy).HasMaxLength(200);
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(r => r.Description).HasMaxLength(500);
            entity.Property(r => r.CreatedBy).HasMaxLength(200);
            entity.Property(r => r.LastModifiedBy).HasMaxLength(200);
        });

        builder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permissions");
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Code).IsUnique();
            entity.Property(p => p.Code).HasMaxLength(100);
            entity.Property(p => p.Name).HasMaxLength(200);
            entity.Property(p => p.CreatedBy).HasMaxLength(200);
            entity.Property(p => p.LastModifiedBy).HasMaxLength(200);
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            entity.HasOne<ApplicationRole>()
                .WithMany()
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Permission>()
                .WithMany()
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserPermission>(entity =>
        {
            entity.ToTable("UserPermissions");
            entity.HasKey(up => new { up.UserId, up.PermissionId });
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Permission>()
                .WithMany()
                .HasForeignKey(up => up.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Group>(entity =>
        {
            entity.ToTable("Groups");
            entity.HasKey(g => g.Id);
            entity.HasIndex(g => g.Name).IsUnique();
            entity.Property(g => g.Name).HasMaxLength(200);
            entity.Property(g => g.CreatedBy).HasMaxLength(200);
            entity.Property(g => g.LastModifiedBy).HasMaxLength(200);
        });

        builder.Entity<UserGroup>(entity =>
        {
            entity.ToTable("UserGroups");
            entity.HasKey(ug => ug.Id);
            entity.HasIndex(ug => new { ug.UserId, ug.GroupId }).IsUnique();
            entity.Property(ug => ug.AccessLevel).HasConversion<int>();
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(ug => ug.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Group>()
                .WithMany()
                .HasForeignKey(ug => ug.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        builder.Entity<StorageObject>(entity =>
        {
            entity.ToTable("StorageObjects");
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.StorageKey).IsUnique();
            entity.Property(s => s.StorageKey).HasMaxLength(500);
            entity.Property(s => s.ContentType).HasMaxLength(200);
            entity.Property(s => s.Sha256).HasMaxLength(128);
            entity.Property(s => s.CreatedBy).HasMaxLength(200);
            entity.Property(s => s.LastModifiedBy).HasMaxLength(200);
        });

        builder.Entity<FileItem>(entity =>
        {
            entity.ToTable("FileItems");
            entity.HasKey(f => f.Id);
            entity.HasIndex(f => new { f.OwnerId, f.ParentId, f.Name })
                .IsUnique()
                .HasFilter("Active = 1 AND GroupId IS NULL");
            entity.HasIndex(f => new { f.GroupId, f.ParentId, f.Name })
                .IsUnique()
                .HasFilter("Active = 1 AND GroupId IS NOT NULL");
            entity.HasIndex(f => f.StorageObjectId);
            entity.HasIndex(f => f.GroupId);
            entity.Property(f => f.Name).HasMaxLength(255);
            entity.Property(f => f.CreatedBy).HasMaxLength(200);
            entity.Property(f => f.LastModifiedBy).HasMaxLength(200);
            entity.HasOne(f => f.StorageObject)
                .WithMany()
                .HasForeignKey(f => f.StorageObjectId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<FileItem>()
                .WithMany()
                .HasForeignKey(f => f.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Group>()
                .WithMany()
                .HasForeignKey(f => f.GroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FileShareEntity>(entity =>
        {
            entity.ToTable("FileShares");
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.Token).IsUnique();
            entity.Property(s => s.Token).HasMaxLength(64);
            entity.Property(s => s.AccessLevel).HasConversion<int>();
            entity.Property(s => s.CreatedBy).HasMaxLength(200);
            entity.Property(s => s.LastModifiedBy).HasMaxLength(200);
            entity.HasOne<FileItem>()
                .WithMany()
                .HasForeignKey(s => s.FileItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.TokenHash).IsUnique();
            entity.Property(t => t.TokenHash).HasMaxLength(128);
            entity.Ignore(t => t.IsActive);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
