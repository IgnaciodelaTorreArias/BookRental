using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalService.DBContext.Models;

[Table("confirmed_rentals", Schema = "rental")]
public partial class ConfirmedRental
{
    [Key]
    [Column("identifier")]
    public long Identifier { get; set; }

    [Column("book_id")]
    public int BookId { get; set; }

    [Column("copy_id")]
    public int CopyId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("rental_fee")]
    public int RentalFee { get; set; }

    [Column("confirmed_at", TypeName = "timestamp without time zone")]
    public DateTime ConfirmedAt { get; set; }
}
