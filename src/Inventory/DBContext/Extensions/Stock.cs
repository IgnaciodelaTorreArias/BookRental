namespace Inventory.DBContext.Models;

public partial class Stock
{
    public enum CopyStatus
    {
        Available,
        Unavailable,
        Lost,
        Retired
    }
}
