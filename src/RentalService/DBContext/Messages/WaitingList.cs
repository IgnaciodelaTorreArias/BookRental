using UServices = RentalService.Services.User;

namespace RentalService.DBContext.Models;

public partial class WaitingList
{
    public UServices.WaitingRental Message() => new()
    {
        BookId = (uint)BookId,
        JoinedAt = (ulong)JoinedAt.Ticks
    };
}
