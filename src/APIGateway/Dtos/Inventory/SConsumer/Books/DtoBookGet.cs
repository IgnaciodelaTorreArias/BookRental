using Inventory.Public.Services.Consumer;

namespace APIGateway.Dtos.Inventory.SConsumer.Books;

public class DtoBookGET(Book source)
{
    public uint BookId = source.BookId;
    public string BookName = source.BookName;
    public string IsoLanguageCode = source.IsoLanguageCode;
    public string AuthorName = source.AuthorName;
    public DateOnly? PublishedDate = source.HasPublishedDate ? DateOnly.FromDayNumber((int)source.PublishedDate) : null;
    public string Description = source.Description;
    public decimal RentalFee = source.RentalFee;
}