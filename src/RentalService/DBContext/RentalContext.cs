using Microsoft.EntityFrameworkCore;
using RentalService.DBContext.Models;

namespace RentalService.DBContext;

public partial class RentalContext : DbContext
{
    public RentalContext()
    {
    }

    public RentalContext(DbContextOptions<RentalContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Book> Books { get; set; }

    public virtual DbSet<ConfirmedRental> ConfirmedRentals { get; set; }

    public virtual DbSet<DeliveryTimeAnalysis> DeliveryTimeAnalyses { get; set; }

    public virtual DbSet<Notified> Notifieds { get; set; }

    public virtual DbSet<Rental> Rentals { get; set; }

    public virtual DbSet<WaitTimeAnalysis> WaitTimeAnalyses { get; set; }

    public virtual DbSet<WaitingList> WaitingLists { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.BookId).HasName("books_pkey");

            entity.Property(e => e.BookId).ValueGeneratedNever();
        });

        modelBuilder.Entity<ConfirmedRental>(entity =>
        {
            entity.HasKey(e => e.Identifier).HasName("confirmed_rentals_pkey");

            entity.Property(e => e.Identifier).UseIdentityAlwaysColumn();
            entity.Property(e => e.ConfirmedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<DeliveryTimeAnalysis>(entity =>
        {
            entity.HasKey(e => e.Identifier).HasName("delivery_time_analysis_pkey");

            entity.Property(e => e.Identifier).UseIdentityAlwaysColumn();
        });

        modelBuilder.Entity<Notified>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("notified_pkey");

            entity.HasIndex(e => new { e.CopyId, e.UserId }, "notified_copy_id_user_id_idx").HasAnnotation("Npgsql:StorageParameter:deduplicate_items", "true");

            entity.Property(e => e.NotificationId).UseIdentityAlwaysColumn();
            entity.Property(e => e.NotifiedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Rental>(entity =>
        {
            entity.HasKey(e => new { e.RentalId, e.Quarter }).HasName("rentals_pkey");

            entity.Property(e => e.RentalId)
                .ValueGeneratedOnAdd()
                .UseIdentityAlwaysColumn();
            entity.Property(e => e.Quarter).HasComment("FLOOR(extract(YEAR FROM start_date)*10+(extract(MONTH FROM start_date)-1)/3)");
        });

        modelBuilder.Entity<WaitTimeAnalysis>(entity =>
        {
            entity.HasKey(e => e.Identifier).HasName("wait_time_analysis_pkey");

            entity.Property(e => e.Identifier).UseIdentityAlwaysColumn();
        });

        modelBuilder.Entity<WaitingList>(entity =>
        {
            entity.HasKey(e => e.WaitingId).HasName("waiting_list_pkey");

            entity.HasIndex(e => e.BookId, "waiting_list_book_id_idx").HasMethod("hash");

            entity.Property(e => e.WaitingId).UseIdentityAlwaysColumn();
            entity.Property(e => e.JoinedAt).HasDefaultValueSql("now()");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
