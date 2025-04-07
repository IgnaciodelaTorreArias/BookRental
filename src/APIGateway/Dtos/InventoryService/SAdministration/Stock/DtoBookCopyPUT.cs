using System.ComponentModel.DataAnnotations;
using InventoryService.Services.Administration;

namespace APIGateway.Dtos.InventoryService.SAdministration.Stock;

public class DtoBookCopyPUT : IDtoIN<BookCopy>
{
    [Required]
    [Range(1, uint.MaxValue)]
    public uint CopyId { get; set; }
    [Required]
    public Status.CopyStatus CopyStatus { get; set; }
    public BookCopy Message() => new()
    {
        CopyId = CopyId,
        Status = Status.ToMessage(CopyStatus)
    };
}
