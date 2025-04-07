using Microsoft.EntityFrameworkCore;

using InventoryService.DBContext.Models;

namespace InventoryService.DBContext;

public partial class InventoryContext : DbContext
{
    public InventoryContext()
    {
    }

    public InventoryContext(DbContextOptions<InventoryContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Acquisition> Acquisitions { get; set; }

    public virtual DbSet<Book> Books { get; set; }

    public virtual DbSet<Stock> Stocks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("inventory", "acquisition_status", new[] { "unconfirmed", "confirmed", "error" })
            .HasPostgresEnum("inventory", "copy_status", new[] { "available", "unavailable", "lost", "retired" });

        modelBuilder.Entity<Acquisition>(entity =>
        {
            entity.HasKey(e => e.AcquisitionId).HasName("acquisitions_pkey");

            entity.Property(e => e.AcquisitionId).UseIdentityAlwaysColumn();
            entity.Property(e => e.AcquisitionDate).HasDefaultValueSql("CURRENT_DATE");

            entity.HasOne(d => d.Book).WithMany(p => p.Acquisitions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("acquisitions_book_id_fkey");
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.BookId).HasName("books_pkey");

            entity.Property(e => e.BookId).UseIdentityAlwaysColumn();
            entity.Property(e => e.AuthorName).HasDefaultValueSql("''::character varying");
            entity.Property(e => e.Description).HasDefaultValueSql("''::text");
            entity.Property(e => e.IsoLanguageCode).IsFixedLength();
            entity.Property(e => e.RentalFee).HasDefaultValue(100);
            entity.Property(e => e.Visible).HasDefaultValue(true);
        });

        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasKey(e => e.CopyId).HasName("stock_pkey");

            entity.HasIndex(e => e.AcquisitionId, "stock_acquisition_id").HasMethod("hash");

            entity.Property(e => e.CopyId).UseIdentityAlwaysColumn();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
