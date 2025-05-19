using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Google.Protobuf;

using Qdrant.Client.Grpc;

using Commons.Kafka;
using Commons.Extensions;
using Commons.Messages.Resources;
using DBM = Inventory.DBContext.Models;

using Inventory.Public.DBContext;

namespace Inventory.Public.Services.Administration;

public partial class InventoryAdministration
{
    async Task UpsertBookEmbeddings(string description, uint id)
    {
        DenseVector vector = new();
        vector.Data.AddRange(_sentencesModel.GetEmbeddings(description));
        await _qdrant.UpsertAsync("books", [
            new PointStruct() { Id = new PointId { Num = id }, Vectors = new Vectors { Vector = new Vector { Dense = vector } } }
        ]);
    }
    [Authorize(Roles = "acquisitions, operations, admin")]
    public override async Task<Books> GetRecentBooksAdministration(PaginatedResource request, ServerCallContext context)
    {
        request.Validate();
        var books = await _context.Books
            .OrderByDescending(book => book.BookId)
            .Skip(request.Offset)
            .Take(request.Limit)
            .Select(book => book.ToPrivateMessage())
            .ToArrayAsync();
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
        await Task.WhenAll(
            UpsertBookEmbeddings(result.Description, result.BookId),
            _producer.ProduceAsync("book_management", message.ToByteArray())
        );
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
        if (request.HasDescription)
            await Task.WhenAll(
                UpsertBookEmbeddings(request.Description, request.BookId),
                _context.SaveChangesAsync()
            );
        else
            await _context.SaveChangesAsync();
        KafkaBook message = request.KafkaBook();
        message.Operation = BookOperation.Updated;
        if (message.HasRentalFee || message.HasVisible)
            await _producer.ProduceAsync("book_management", message.ToByteArray());
        _logger.LogInformation("{{\"Event\":\"BookUpdated\",\"Book\":{request},\"User\":{user}}}", request, context.GetUserId());
        return book.ToPrivateMessage();
    }
}
