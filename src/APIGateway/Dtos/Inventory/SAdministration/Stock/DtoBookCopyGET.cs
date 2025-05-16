using Inventory.Public.Services.Administration;

namespace APIGateway.Dtos.Inventory.SAdministration.Stock;

public class DtoBookCopyGET(BookCopy source)
{
    public uint CopyId = source.CopyId;
    public uint BookId = source.BookId;
    public uint AcquisitionId = source.AcquisitionId;
    public Status.CopyStatus CopyStatus = Status.FromMessage(source.Status);
}
