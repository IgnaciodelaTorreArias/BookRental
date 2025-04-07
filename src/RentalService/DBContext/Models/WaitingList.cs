using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalService.DBContext.Models;

[Table("waiting_list", Schema = "rental")]
public partial class WaitingList
{
    [Key]
    [Column("waiting_id")]
    public long WaitingId { get; set; }

    [Column("book_id")]
    public int BookId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("joined_at", TypeName = "timestamp without time zone")]
    public DateTime JoinedAt { get; set; }
}
