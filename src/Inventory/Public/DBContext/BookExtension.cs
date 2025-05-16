using Grpc.Core;

using Inventory.DBContext.Models;

using CServices = Inventory.Public.Services.Consumer;
using AServices = Inventory.Public.Services.Administration;

namespace Inventory.Public.DBContext;

public static class BookExtension
{
    public static CServices.Book ToPublicMessage(this Book book) => new()
    {
        BookId = (uint)book.BookId,
        BookName = book.BookName,
        IsoLanguageCode = book.IsoLanguageCode,
        AuthorName = book.AuthorName,
        PublishedDate = (uint)(book.PublishedDate?.DayNumber ?? 0),
        Description = book.Description,
        RentalFee = (uint)book.RentalFee,
        PublishedDateUnknown = book.PublishedDate == null
    };

    public static AServices.Book ToPrivateMessage(this Book book) => new()
    {
        BookId = (uint)book.BookId,
        BookName = book.BookName,
        IsoLanguageCode = book.IsoLanguageCode,
        AuthorName = book.AuthorName,
        PublishedDate = (uint)(book.PublishedDate?.DayNumber ?? 0),
        Description = book.Description,
        RentalFee = (uint)book.RentalFee,
        Visible = book.Visible,
        PublishedDateUnknown = book.PublishedDate == null
    };

    public static void SetOptionalValues(this Book book, AServices.Book source)
    {
        if (source.HasAuthorName)
            book.AuthorName = source.AuthorName;
        if (source.HasPublishedDate)
            book.PublishedDate = DateOnly.FromDayNumber((int)source.PublishedDate);
        if (source.HasDescription)
            book.Description = source.Description;
        if (source.HasRentalFee)
            book.RentalFee = (int)source.RentalFee;
        if (source.HasVisible)
            book.Visible = source.Visible;
        if (source.HasPublishedDateUnknown)
            book.PublishedDate = null;
    }
    public static void FromMessage(this Book book, AServices.Book source)
    {
        if (!source.HasBookName)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`BookName` is required for this operation"));
        if (!source.HasIsoLanguageCode)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`IsoLanguageCode` is required for this operation"));
        book.BookName = source.BookName;
        book.IsoLanguageCode = source.IsoLanguageCode;
        book.SetOptionalValues(source);
    }
    public static void Update(this Book book, AServices.Book source)
    {
        if (source.HasBookName && source.BookName == book.BookName)
            source.ClearBookName();
        if (source.HasIsoLanguageCode && source.IsoLanguageCode == book.IsoLanguageCode)
            source.ClearIsoLanguageCode();
        if (source.HasAuthorName && source.AuthorName == book.AuthorName)
            source.ClearAuthorName();
        if (source.HasPublishedDate && source.PublishedDate == (uint)(book.PublishedDate?.DayNumber ?? 0) || source.HasPublishedDateUnknown)
            source.ClearPublishedDate();
        if (source.HasDescription && source.Description == book.Description)
            source.ClearDescription();
        if (source.HasRentalFee && source.RentalFee == book.RentalFee)
            source.ClearRentalFee();
        if (source.HasVisible && source.Visible == book.Visible)
            source.ClearVisible();
        if (source.HasPublishedDateUnknown && source.PublishedDateUnknown == (book.PublishedDate == null))
            source.ClearPublishedDateUnknown();
        if (source.HasBookName)
            book.BookName = source.BookName;
        if (source.HasIsoLanguageCode)
            book.IsoLanguageCode = source.IsoLanguageCode;
        book.SetOptionalValues(source);
    }
}