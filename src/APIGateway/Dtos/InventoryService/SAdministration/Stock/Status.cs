using SAdmin = InventoryService.Services.Administration;

namespace APIGateway.Dtos.InventoryService.SAdministration.Stock;

public class Status
{
    public enum CopyStatus
    {
        Available,
        Unavailable,
        Lost,
        Retired
    }
    public static CopyStatus FromMessage(SAdmin.CopyStatus source)
    {
        return source switch
        {
            SAdmin.CopyStatus.Available => CopyStatus.Available,
            SAdmin.CopyStatus.Unavailable => CopyStatus.Unavailable,
            SAdmin.CopyStatus.Lost => CopyStatus.Lost,
            SAdmin.CopyStatus.Retired => CopyStatus.Retired,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }
    public static SAdmin.CopyStatus ToMessage(CopyStatus source)
    {
        return source switch
        {
            CopyStatus.Available => SAdmin.CopyStatus.Available,
            CopyStatus.Unavailable => SAdmin.CopyStatus.Unavailable,
            CopyStatus.Lost => SAdmin.CopyStatus.Lost,
            CopyStatus.Retired => SAdmin.CopyStatus.Retired,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }
}
