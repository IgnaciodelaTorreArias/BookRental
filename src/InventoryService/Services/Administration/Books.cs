using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Google.Protobuf;

using Commons.Kafka;
using Commons.Messages.Resources;

using DBM = InventoryService.DBContext.Models;
using Commons.Extensions;

namespace InventoryService.Services.Administration;

public partial class InventoryAdministration
{
    [Authorize(Roles = "acquisitions, operations, admin")]
    public override async Task<Books> GetRecentBooksAdministration(PaginatedResource request, ServerCallContext context)
    {
        request.Validate();
        List<Book> books = await _context.Books
            .OrderByDescending(book => book.BookId)
            .Skip(request.Offset)
            .Take(request.Limit)
            .Select(book => book.ToPrivateMessage())
            .ToListAsync();
        return new() { BooksData = { books } };
    }

    [Authorize(Roles = "acquisitions, operations, admin")]
    public override async Task<Book> GetBookAdministration(ResourceIdentifier request, ServerCallContext context)
    {
        request.Validate();
        DBM.Book? book = await _context.Books
            .FirstOrDefaultAsync(book => book.BookId == (int)request.Identifier);
        if (book is null)
            throw new RpcException(new Status(StatusCode.NotFound, "The book you were searching for was not found"));
        return book.ToPrivateMessage();
    }

    [Authorize(Roles = "acquisitions")]
    public override async Task<Book> PostBook(Book request, ServerCallContext context)
    {
        request.Validate();
        DBM.Book book = new();
        book.FromMessage(request);
        _context.Books.Add(book);
        await _context.SaveChangesAsync();
        await _context.Entry(book).ReloadAsync();
        Book result = book.ToPrivateMessage();
        KafkaBook message = result.KafkaBook();
        message.Operation = BookOperation.Created;
        await _producer.ProduceAsync("book_management", message.ToByteArray());
        _logger.LogInformation("{{\"Event\":\"BookCreated\",\"Book\":{result},\"User\":{user}}}", result, context.GetUserId());
        return result;
    }

    [Authorize(Roles = "acquisitions, admin")]
    public override async Task<Book> PutBook(Book request, ServerCallContext context)
    {
        request.Validate();
        if (!request.HasBookId)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`BookId` is required for this operation"));
        DBM.Book? book = await _context.Books.FirstOrDefaultAsync(book => book.BookId == (int)request.BookId);
        if (book is null)
            throw new RpcException(new Status(StatusCode.NotFound, "The book you were searching for was not found"));
        book.Update(request);
        await _context.SaveChangesAsync();
        KafkaBook message = request.KafkaBook();
        message.Operation = BookOperation.Updated;
        if (message.HasDescription || message.HasRentalFee || message.HasVisible)
            await _producer.ProduceAsync("book_management", message.ToByteArray());
        _logger.LogInformation("{{\"Event\":\"BookUpdated\",\"Book\":{request},\"User\":{user}}}", request, context.GetUserId());
        return book.ToPrivateMessage();
    }
}
