using RentalService.Services.User;

namespace APIGateway.Dtos.RentalService.SUser;

public class DtoRentalRegistryGET(RentalRegistry source)
{
    public uint BookId = source.BookId;
    public uint CopyId = source.CopyId;
    public DateOnly StartDate = DateOnly.FromDayNumber((int)source.StartDate);
    public DateOnly? ReturnDate = source.HasReturnDate ? DateOnly.FromDayNumber((int)source.ReturnDate) : null;
    public decimal RentalFee = source.RentalFee / 100;
    public decimal? Total = source.HasTotal ? source.Total / 100 : null;
}
