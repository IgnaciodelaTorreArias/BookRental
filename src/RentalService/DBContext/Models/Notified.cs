using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalService.DBContext.Models;

[Table("notified", Schema = "rental")]
public partial class Notified
{
    [Key]
    [Column("notification_id")]
    public long NotificationId { get; set; }

    [Column("book_id")]
    public int BookId { get; set; }

    [Column("copy_id")]
    public int CopyId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("rental_fee")]
    public int RentalFee { get; set; }

    [Column("notified_at", TypeName = "timestamp without time zone")]
    public DateTime NotifiedAt { get; set; }
}
