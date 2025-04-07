using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryService.DBContext.Models;

[Table("acquisitions", Schema = "inventory")]
public partial class Acquisition
{
    [Key]
    [Column("acquisition_id")]
    public int AcquisitionId { get; set; }

    [Column("book_id")]
    public int BookId { get; set; }

    [Column("acquisition_price")]
    public int AcquisitionPrice { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("acquisition_date")]
    public DateOnly AcquisitionDate { get; set; }

    [Column("acquisition_status")]
    public AcquisitionStatus Status { get; set; }

    [Column("copy_id_start")]
    public int CopyIdStart { get; set; }

    [ForeignKey("BookId")]
    [InverseProperty("Acquisitions")]
    public virtual Book Book { get; set; } = null!;
}
