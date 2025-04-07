using UServices = RentalService.Services.User;

namespace RentalService.DBContext.Models;

public partial class Notified
{
    public UServices.NotifiedRental Message() => new()
    {
        BookId = (uint)BookId,
        CopyId = (uint)CopyId,
        RentalFee = (uint)RentalFee,
        NotifiedAt = (ulong)NotifiedAt.Ticks
    };
}
