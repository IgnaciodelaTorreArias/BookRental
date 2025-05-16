namespace Inventory.DBContext.Models;

public partial class Acquisition
{
    public enum AcquisitionStatus
    {
        Unconfirmed,
        Confirmed,
        Error
    }
}
