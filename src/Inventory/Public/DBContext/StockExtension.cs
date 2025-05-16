using Inventory.DBContext.Models;

using AServices = Inventory.Public.Services.Administration;

namespace Inventory.Public.DBContext;

public static class StockExtension
{
    public static Stock.CopyStatus ConvertMessageStatus(AServices.CopyStatus status) => status switch
    {
        AServices.CopyStatus.Available => Stock.CopyStatus.Available,
        AServices.CopyStatus.Unavailable => Stock.CopyStatus.Unavailable,
        AServices.CopyStatus.Lost => Stock.CopyStatus.Lost,
        AServices.CopyStatus.Retired => Stock.CopyStatus.Retired,
        _ => throw new NotImplementedException()
    };

    public static AServices.CopyStatus ConvertToMessageStatus(Stock.CopyStatus status) => status switch
    {
        Stock.CopyStatus.Available => AServices.CopyStatus.Available,
        Stock.CopyStatus.Unavailable => AServices.CopyStatus.Unavailable,
        Stock.CopyStatus.Lost => AServices.CopyStatus.Lost,
        Stock.CopyStatus.Retired => AServices.CopyStatus.Retired,
        _ => throw new NotImplementedException()
    };

    public static AServices.BookCopy Message(this Stock stock) => new()
    {
        CopyId = (uint)stock.CopyId,
        BookId = (uint)stock.BookId,
        AcquisitionId = (uint)stock.AcquisitionId,
        Status = ConvertToMessageStatus(stock.Status)
    };
}
