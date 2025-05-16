using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalService.DBContext.Models;

[Table("books", Schema = "rental")]
public partial class Book
{
    [Key]
    [Column("book_id")]
    public int BookId { get; set; }

    [Column("rental_fee")]
    public int RentalFee { get; set; }

    [Column("visible")]
    public bool Visible { get; set; }
}
