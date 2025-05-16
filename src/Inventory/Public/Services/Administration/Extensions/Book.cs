using Grpc.Core;

using Commons.Kafka;

namespace Inventory.Public.Services.Administration;

public partial class Book
{
    public void Validate()
    {
        if (HasBookId && BookId < 1)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`BookId` cannot be less than 1"));
        if (HasBookName && BookName.Length > 255)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`BookName` is too long, max is 255"));
        if (HasIsoLanguageCode && IsoLanguageCode.Length != 3)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`IsoLanguageCode` has an invalid value, use a 3 characters code"));
        if (HasAuthorName && AuthorName.Length > 255)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`AuthorName` is too long, max is 255"));
        if (HasPublishedDateUnknown)
        {
            if (PublishedDateUnknown && !HasPublishedDate)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "`PublishedDateUnknow` and `PublishedDate` must be consistent"));
            if (HasPublishedDate && (PublishedDate < DateOnly.MinValue.DayNumber || PublishedDate > DateOnly.MaxValue.DayNumber))
                throw new RpcException(new Status(StatusCode.InvalidArgument, $"`PublishedDate` has an invalid value, valid range is [{DateOnly.MinValue.DayNumber}, {DateOnly.MaxValue.DayNumber}]"));
        }
        if (HasDescription && Description.Length > 5000)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`Description` is too long, max is 5000"));
    }

    public KafkaBook KafkaBook()
    {
        KafkaBook message = new()
        {
            Identifier = BookId
        };
        if (HasDescription)
            message.Description = Description;
        if (HasRentalFee)
            message.RentalFee = RentalFee;
        if (HasVisible)
            message.Visible = Visible;
        return message;
    }
}
