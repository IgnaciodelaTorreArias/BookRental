using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryService.DBContext.Models;

[Table("stock", Schema = "inventory")]
public partial class Stock
{
    [Key]
    [Column("copy_id")]
    public int CopyId { get; set; }

    [Column("book_id")]
    public int BookId { get; set; }

    [Column("acquisition_id")]
    public int AcquisitionId { get; set; }

    [Column("copy_status")]
    public CopyStatus Status { get; set; }
}
