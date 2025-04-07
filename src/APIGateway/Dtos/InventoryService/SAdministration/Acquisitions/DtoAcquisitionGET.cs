using InventoryService.Services.Administration;

namespace APIGateway.Dtos.InventoryService.SAdministration.Acquisitions;

public class DtoAcquisitionGET(Acquisition source){
    public uint AcquisitionId = source.AcquisitionId;
    public uint BookId = source.BookId;
    public decimal AcquisitionPrice = source.AcquisitionPrice / 100;
    public uint Quantity = source.Quantity;
    public DateOnly AcquisitionDate = DateOnly.FromDayNumber((int)source.AcquisitionDate);
    public Status.AcquisitionStatus AcquisitionStatus = Status.FromMessage(source.Status);
    public uint CopyIdStart = source.CopyIdStart;}