using Microsoft.EntityFrameworkCore;
using EOS.Models;

namespace EOS.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Token> Tokens { get; set; }
    public DbSet<PurchaseTransaction> PurchaseTransactions { get; set; }
    public DbSet<FundsTransaction> FundsTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User config
        modelBuilder.Entity<User>().HasKey(u => u.Id);
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();

        // Token config
        modelBuilder.Entity<Token>().HasKey(t => t.Id);
        modelBuilder.Entity<Token>()
            .HasOne(t => t.User)
            .WithMany(u => u.Tokens)
            .HasForeignKey(t => t.UserId);

        // PurchaseTransaction config
        modelBuilder.Entity<PurchaseTransaction>().HasKey(pt => pt.Id);
        modelBuilder.Entity<PurchaseTransaction>()
            .HasOne(pt => pt.User)
            .WithMany(u => u.PurchaseTransactions)
            .HasForeignKey(pt => pt.UserId);

        // FundsTransaction config
        modelBuilder.Entity<FundsTransaction>().HasKey(ft => ft.Id);
        modelBuilder.Entity<FundsTransaction>()
            .HasOne(ft => ft.User)
            .WithMany(u => u.FundsTransactions)
            .HasForeignKey(ft => ft.UserId);
    }
}
