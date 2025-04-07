using System.ComponentModel.DataAnnotations;
using InventoryService.Services.Administration;

namespace APIGateway.Dtos.InventoryService.SAdministration.Acquisitions;

public class DtoAcquisitionPUT : IDtoIN<Acquisition>
{
    [Required]
    [Range(1, uint.MaxValue)]
    public uint AcquisitionId { get; set; }
    public uint? AcquisitionPrice { get; set; }
    public uint? Quantity { get; set; }
    public DateOnly? AcquisitionDate { get; set; }
    public Status.AcquisitionStatus? AcquisitionStatus { get; set; }

    public Acquisition Message()
    {
        Acquisition message = new()
        {
            AcquisitionId = AcquisitionId,
        };
        if (AcquisitionPrice.HasValue)
            message.AcquisitionPrice = AcquisitionPrice.Value;
        if (Quantity.HasValue)
            message.Quantity = Quantity.Value;
        if (AcquisitionDate.HasValue)
            message.AcquisitionDate = (uint)AcquisitionDate.Value.DayNumber;
        if (AcquisitionStatus.HasValue)
            message.Status = Status.ToMessage(AcquisitionStatus.Value);
        return message;
    }
}