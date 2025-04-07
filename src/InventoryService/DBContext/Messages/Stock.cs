using AServices = InventoryService.Services.Administration;

namespace InventoryService.DBContext.Models;

public partial class Stock
{
    public enum CopyStatus
    {
        Available,
        Unavailable,
        Lost,
        Retired
    }
    public static CopyStatus ConvertMessageStatus(AServices.CopyStatus status) => status switch
    {
        AServices.CopyStatus.Available => CopyStatus.Available,
        AServices.CopyStatus.Unavailable => CopyStatus.Unavailable,
        AServices.CopyStatus.Lost => CopyStatus.Lost,
        AServices.CopyStatus.Retired => CopyStatus.Retired,
        _ => throw new NotImplementedException()
    };

    public static AServices.CopyStatus ConvertToMessageStatus(CopyStatus status) => status switch
    {
        CopyStatus.Available => AServices.CopyStatus.Available,
        CopyStatus.Unavailable => AServices.CopyStatus.Unavailable,
        CopyStatus.Lost => AServices.CopyStatus.Lost,
        CopyStatus.Retired => AServices.CopyStatus.Retired,
        _ => throw new NotImplementedException()
    };

    public AServices.BookCopy Message() => new()
    {
        CopyId = (uint)CopyId,
        BookId = (uint)BookId,
        AcquisitionId = (uint)AcquisitionId,
        Status = ConvertToMessageStatus(Status)
    };
}
