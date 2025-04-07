using System.ComponentModel.DataAnnotations;
using InventoryService.Services.Administration;

namespace APIGateway.Dtos.InventoryService.SAdministration.Acquisitions;

public class DtoAcquisitionPOST : IDtoIN<Acquisition>
{
    [Required]
    [Range(1, uint.MaxValue)]
    public uint BookId { get; set; }
    [Required]
    public uint AcquisitionPrice { get; set; }
    [Required]
    [Range(1, uint.MaxValue)]
    public uint Quantity { get; set; }
    public DateOnly? AcquisitionDate { get; set; }

    public Acquisition Message()
    {
        Acquisition message = new()
        {
            BookId = BookId,
            AcquisitionPrice = AcquisitionPrice,
            Quantity = Quantity,
        };
        if (AcquisitionDate.HasValue)
            message.AcquisitionDate = (uint)AcquisitionDate.Value.DayNumber;
        return message;
    }
}
