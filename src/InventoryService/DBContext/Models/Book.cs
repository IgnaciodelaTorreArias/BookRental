using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryService.DBContext.Models;

[Table("books", Schema = "inventory")]
public partial class Book
{
    [Key]
    [Column("book_id")]
    public int BookId { get; set; }

    [Column("book_name")]
    [StringLength(255)]
    public string BookName { get; set; } = null!;

    [Column("iso_language_code")]
    [StringLength(3)]
    public string IsoLanguageCode { get; set; } = null!;

    [Column("author_name")]
    [StringLength(255)]
    public string AuthorName { get; set; } = null!;

    [Column("published_date")]
    public DateOnly? PublishedDate { get; set; }

    [Column("description")]
    public string Description { get; set; } = null!;

    [Column("rental_fee")]
    public int RentalFee { get; set; }

    [Column("visible")]
    public bool Visible { get; set; }

    [InverseProperty("Book")]
    public virtual ICollection<Acquisition> Acquisitions { get; set; } = new List<Acquisition>();
}
