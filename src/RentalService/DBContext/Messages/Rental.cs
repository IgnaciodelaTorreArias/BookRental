using UServices = RentalService.Services.User;

namespace RentalService.DBContext.Models;

public partial class Rental
{
    private UServices.RentalRegistry BaseMessage() => new()
    {
        RentalId = (ulong)RentalId,
        BookId = (uint)BookId,
        CopyId = (uint)CopyId,
        StartDate = (uint)StartDate.DayNumber,
        RentalFee = (uint)RentalFee
    };
    public UServices.RentalRegistry Message()
    {
        UServices.RentalRegistry message = BaseMessage();
        if (ReturnDate is not null)
            message.ReturnDate = (uint)ReturnDate.Value.DayNumber;
        if (Total is not null)
            message.Total = (uint)Total;
        return message;
    }
}
