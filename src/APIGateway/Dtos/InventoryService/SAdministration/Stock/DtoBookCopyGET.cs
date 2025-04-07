using InventoryService.Services.Administration;

namespace APIGateway.Dtos.InventoryService.SAdministration.Stock;

public class DtoBookCopyGET(BookCopy source){
    public uint CopyId = source.CopyId;
    public uint BookId = source.BookId;
    public uint AcquisitionId = source.AcquisitionId;
    public Status.CopyStatus CopyStatus = Status.FromMessage(source.Status);}
