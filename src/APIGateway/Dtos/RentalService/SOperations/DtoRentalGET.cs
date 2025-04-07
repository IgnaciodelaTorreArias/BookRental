using RentalService.Services.Operations;

namespace APIGateway.Dtos.RentalService.SOperations;

public class DtoRentalGET(ConfirmedRental source)
{
    public ulong Id = source.Identifier;
    public uint BookId = source.BookId;
    public uint CopyId = source.CopyId;
    public uint UserId = source.UserId;
    public decimal RentalFee = source.RentalFee / 100;
}
