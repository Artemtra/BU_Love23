using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace BU_Love.API.NewDb;

public partial class BuLoveDbContext : DbContext
{
    public BuLoveDbContext()
    {
    }

    public BuLoveDbContext(DbContextOptions<BuLoveDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<Orderitem> Orderitems { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=192.168.200.13;database=bu_love_db;user=student;password=student", Microsoft.EntityFrameworkCore.ServerVersion.Parse("10.3.39-mariadb"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_general_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("categories");

            entity.Property(e => e.Id).HasColumnType("int(11)");
            entity.Property(e => e.ImageUrl).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("orders");

            entity.HasIndex(e => e.UserId, "FK_orders_Users_Id");

            entity.Property(e => e.Id).HasColumnType("int(11)");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CustomerName).HasMaxLength(255);
            entity.Property(e => e.OrderDate).HasColumnType("datetime");
            entity.Property(e => e.Phone).HasMaxLength(255);
            entity.Property(e => e.TotalAmount).HasPrecision(10, 2);
            entity.Property(e => e.UserId).HasColumnType("int(11)");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_orders_Users_Id");
        });

        modelBuilder.Entity<Orderitem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("orderitems");

            entity.HasIndex(e => e.OrderId, "FK_orderitems");

            entity.HasIndex(e => e.ProductId, "FK_orderitems2");

            entity.Property(e => e.Id).HasColumnType("int(11)");
            entity.Property(e => e.OrderId).HasColumnType("int(11)");
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.ProductId).HasColumnType("int(11)");
            entity.Property(e => e.Quantity).HasColumnType("int(11)");

            entity.HasOne(d => d.Order).WithMany(p => p.Orderitems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_orderitems");

            entity.HasOne(d => d.Product).WithMany(p => p.Orderitems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_orderitems2");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("products");

            entity.HasIndex(e => e.CategoryId, "FK_products");

            entity.Property(e => e.Id).HasColumnType("int(11)");
            entity.Property(e => e.CategoryId).HasColumnType("int(11)");
            entity.Property(e => e.Condition)
                .HasMaxLength(255)
                .HasDefaultValueSql("'Good'");
            entity.Property(e => e.Description).HasColumnType("int(11)");
            entity.Property(e => e.ImageUrl).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.StockQuantity).HasColumnType("int(11)");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_products");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("users");

            entity.Property(e => e.Id).HasColumnType("int(11)");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.BonusPoints).HasColumnType("int(11)");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(255);
            entity.Property(e => e.Role)
                .HasMaxLength(255)
                .HasDefaultValueSql("'Customer'");
            entity.Property(e => e.Username).HasMaxLength(255);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
