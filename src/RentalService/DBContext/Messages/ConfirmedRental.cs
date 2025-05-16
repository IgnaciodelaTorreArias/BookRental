using OServices = RentalService.Services.Operations;
using UServices = RentalService.Services.User;

namespace RentalService.DBContext.Models;

public partial class ConfirmedRental
{
    public OServices.ConfirmedRental PrivateMessage() => new()
    {
        Identifier = (uint)Identifier,
        BookId = (uint)BookId,
        CopyId = (uint)CopyId,
        UserId = (uint)UserId,
        RentalFee = (uint)RentalFee,
    };

    public UServices.ConfirmedRental PublicMessage() => new()
    {
        BookId = (uint)BookId,
        CopyId = (uint)CopyId,
        RentalFee = (uint)RentalFee,
        ConfirmedAt = (ulong)ConfirmedAt.Ticks
    };
}
