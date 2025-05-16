using SAdmin = Inventory.Public.Services.Administration;

namespace APIGateway.Dtos.Inventory.SAdministration.Acquisitions;

public static class Status
{
    public enum AcquisitionStatus
    {
        Confirmed,
        Unconfirmed,
        Error
    }
    public static AcquisitionStatus FromMessage(SAdmin.AcquisitionStatus source)
    {
        return source switch
        {
            SAdmin.AcquisitionStatus.Confirmed => AcquisitionStatus.Confirmed,
            SAdmin.AcquisitionStatus.Unconfirmed => AcquisitionStatus.Unconfirmed,
            SAdmin.AcquisitionStatus.Error => AcquisitionStatus.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }
    public static SAdmin.AcquisitionStatus ToMessage(AcquisitionStatus source)
    {
        return source switch
        {
            AcquisitionStatus.Confirmed => SAdmin.AcquisitionStatus.Confirmed,
            AcquisitionStatus.Unconfirmed => SAdmin.AcquisitionStatus.Unconfirmed,
            AcquisitionStatus.Error => SAdmin.AcquisitionStatus.Error,
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }
}