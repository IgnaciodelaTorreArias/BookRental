using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RentalService.DBContext.Models;

[PrimaryKey("RentalId", "Quarter")]
[Table("rentals", Schema = "rental")]
public partial class Rental
{
    [Key]
    [Column("rental_id")]
    public long RentalId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("book_id")]
    public int BookId { get; set; }

    [Column("copy_id")]
    public int CopyId { get; set; }

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("return_date")]
    public DateOnly? ReturnDate { get; set; }

    [Column("rental_fee")]
    public int RentalFee { get; set; }

    [Column("total")]
    public int? Total { get; set; }

    /// <summary>
    /// FLOOR(extract(YEAR FROM start_date)*10+(extract(MONTH FROM start_date)-1)/3)
    /// </summary>
    [Key]
    [Column("quarter")]
    public int Quarter { get; set; }
}
