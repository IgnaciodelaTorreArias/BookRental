using Grpc.Core;

using UServices = InventoryService.Services.Users;
using AServices = InventoryService.Services.Administration;

namespace InventoryService.DBContext.Models;

public partial class Book
{
    public UServices.Book ToPublicMessage() => new()
    {
        BookId = (uint)BookId,
        BookName = BookName,
        IsoLanguageCode = IsoLanguageCode,
        AuthorName = AuthorName,
        PublishedDate = (uint)(PublishedDate?.DayNumber ?? 0),
        Description = Description,
        RentalFee = (uint)RentalFee,
        PublishedDateUnknown = PublishedDate == null
    };

    public AServices.Book ToPrivateMessage() => new()
    {
        BookId = (uint)BookId,
        BookName = BookName,
        IsoLanguageCode = IsoLanguageCode,
        AuthorName = AuthorName,
        PublishedDate = (uint)(PublishedDate?.DayNumber ?? 0),
        Description = Description,
        RentalFee = (uint)RentalFee,
        Visible = Visible,
        PublishedDateUnknown = PublishedDate == null
    };

    public void SetOptionalValues(AServices.Book source)
    {
        if (source.HasAuthorName)
            AuthorName = source.AuthorName;
        if (source.HasPublishedDate)
            PublishedDate = DateOnly.FromDayNumber((int)source.PublishedDate);
        if (source.HasDescription)
            Description = source.Description;
        if (source.HasRentalFee)
            RentalFee = (int)source.RentalFee;
        if (source.HasVisible)
            Visible = source.Visible;
        if (source.HasPublishedDateUnknown)
            PublishedDate = null;
    }
    public void FromMessage(AServices.Book source)
    {
        if (!source.HasBookName)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`BookName` is required for this operation"));
        if (!source.HasIsoLanguageCode)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`IsoLanguageCode` is required for this operation"));
        BookName = source.BookName;
        IsoLanguageCode = source.IsoLanguageCode;
        SetOptionalValues(source);
    }
    public void Update(AServices.Book source)
    {
        if (source.HasBookName && source.BookName == BookName)
            source.ClearBookName();
        if (source.HasIsoLanguageCode && source.IsoLanguageCode == IsoLanguageCode)
            source.ClearIsoLanguageCode();
        if (source.HasAuthorName && source.AuthorName == AuthorName)
            source.ClearAuthorName();
        if ((source.HasPublishedDate && source.PublishedDate == (uint)(PublishedDate?.DayNumber ?? 0)) || source.HasPublishedDateUnknown)
            source.ClearPublishedDate();
        if (source.HasDescription && source.Description == Description)
            source.ClearDescription();
        if (source.HasRentalFee && source.RentalFee == RentalFee)
            source.ClearRentalFee();
        if (source.HasVisible && source.Visible == Visible)
            source.ClearVisible();
        if (source.HasPublishedDateUnknown && source.PublishedDateUnknown == (PublishedDate == null))
            source.ClearPublishedDateUnknown();
        if (source.HasBookName)
            BookName = source.BookName;
        if (source.HasIsoLanguageCode)
            IsoLanguageCode = source.IsoLanguageCode;
        SetOptionalValues(source);
    }
}