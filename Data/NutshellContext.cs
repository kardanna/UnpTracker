using Microsoft.EntityFrameworkCore;

namespace UnpTracker.Data;

public class NutshellContext : DbContext
{
    public DbSet<Payer> Payers { get; set; }
    public DbSet<Subscriber> Subscribers { get; set; }
    public DbSet<LocalPayer> LocalPayers { get; set; }
    public DbSet<SubscriberPayer> SubscriberPayers { get; set; }
    
    public NutshellContext(DbContextOptions<NutshellContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payer>(entity =>
        {
            entity.ToTable("Payer");
            entity.HasIndex(e => e.Unp).IsUnique();
            entity.Property(e => e.Unp).HasMaxLength(9);
            entity.Property(e => e.IsInLocalDb).HasColumnType("BOOLEAN");
            entity.Property(e => e.IsInStateDb).HasColumnType("BOOLEAN");
        });

        modelBuilder.Entity<Subscriber>(entity =>
        {
            entity.ToTable("Subscriber");
            entity.HasIndex(e => e.Email, "IX_Subscriber_Email").IsUnique();
        });

        modelBuilder.Entity<LocalPayer>(entity =>
        {
            entity.ToTable("LocalPayer");
            entity.HasIndex(e => e.PayerId, "IX_LocalPayer_PayerId").IsUnique();
            entity.HasOne(e => e.Payer).WithOne(e => e.LocalPayer).HasForeignKey<LocalPayer>(e => e.PayerId);
        });

        modelBuilder.Entity<SubscriberPayer>(entity =>
        {
            entity.ToTable("SubscriberPayer");
            entity.HasIndex(e => new { e.SubscriberId, e.PayerId }, "IX_SubscriberPayer_SubscriberId_PayerId").IsUnique();
            entity.HasOne(e => e.Payer).WithMany(e => e.SubscriberPayers).HasForeignKey(e => e.PayerId);
            entity.HasOne(e => e.Subscriber).WithMany(e => e.SubscriberPayers).HasForeignKey(e => e.SubscriberId);
        });
    }
}