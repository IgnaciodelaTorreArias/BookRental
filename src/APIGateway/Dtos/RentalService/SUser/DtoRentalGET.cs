using RentalService.Services.User;

namespace APIGateway.Dtos.RentalService.SUser;

public class DtoRentalGET
{
    public uint BookId { get; set; }
    public uint? CopyId { get; set; }
    public decimal? RentalFee { get; set; }
    public DateTime Time { get; set; }

    public DtoRentalGET(WaitingRental source)
    {
        BookId = source.BookId;
        Time = new DateTime((long)source.JoinedAt);
    }
    public DtoRentalGET(NotifiedRental source)
    {
        BookId = source.BookId;
        CopyId = source.CopyId;
        RentalFee = source.RentalFee / 100;
        Time = new DateTime((long)source.NotifiedAt);
    }
    public DtoRentalGET(ConfirmedRental source)
    {
        BookId = source.BookId;
        CopyId = source.CopyId;
        RentalFee = source.RentalFee / 100;
        Time = new DateTime((long)source.ConfirmedAt);
    }
}
