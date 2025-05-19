using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Google.Protobuf.WellKnownTypes;

using Qdrant.Client;
using Qdrant.Client.Grpc;

using Commons.Extensions;
using Commons.Messages.Resources;
using Inventory.DBContext;
using DBM = Inventory.DBContext.Models;

using Inventory.Public.DBContext;

namespace Inventory.Public.Services.Consumer;

public class InventoryConsumer(
    InventoryContext context,
    QdrantClient qdrant
) : SInventoryConsumer.SInventoryConsumerBase
{
    private readonly InventoryContext _context = context;
    private readonly QdrantClient _qdrant = qdrant;

    [AllowAnonymous]
    public override async Task<Books> GetRecentBooks(PaginatedResource request, ServerCallContext context)
    {
        request.Validate();
        var books = await _context.Books
            .Where(book => book.Visible)
            .OrderByDescending(book => book.BookId)
            .Skip(request.Offset)
            .Take(request.Limit)
            .Select(book => book.ToPublicMessage())
            .ToArrayAsync();
        return new() { BooksData = { books } };
    }

    [AllowAnonymous]
    public override async Task<Book> GetBook(ResourceIdentifier request, ServerCallContext context)
    {
        request.Validate();
        DBM.Book? book = await _context.Books
            .FirstOrDefaultAsync(book => book.BookId == (int)request.Identifier && book.Visible);
        if (book is null)
            throw new RpcException(new Status(StatusCode.NotFound, "The book you were searching for was not found"));
        return book.ToPublicMessage();
    }

    [AllowAnonymous]
    public override async Task<Books> GetSimilarBooks(ResourceIdentifier request, ServerCallContext context)
    {
        request.Validate();
        var similar = (await _qdrant.QueryAsync("sentences", query: new Query
        {
            Nearest = new VectorInput { Id = new PointId { Num = request.Identifier } }
        }, payloadSelector: new WithPayloadSelector { Enable = false })).Select(scored => (int)scored.Id.Num).ToHashSet();
        var books = await _context.Books
            .Where(book => book.Visible && similar.Contains(book.BookId))
            .Select(book => book.ToPublicMessage())
            .ToArrayAsync();
        return new() { BooksData = { books } };
    }

    [Authorize(Roles = "user")]
    public override async Task<Books> GetRecommendedBooks(Empty request, ServerCallContext context)
    {
        uint userId = context.GetUserId();
        var user = (await _qdrant.RetrieveAsync("users", userId, withVectors: true)).FirstOrDefault();
        if (user is null)
            return new();
        var f = new Filter();
        var ignored_ids = new HasIdCondition();
        ignored_ids.HasId.AddRange(user.Payload["history"].ListValue.Values.Select(value => new PointId { Num = (ulong)value.IntegerValue }));
        f.MustNot.Add(new Condition { HasId = ignored_ids });
        var similar = (await _qdrant.QueryAsync("books", query: new Query
        {
            Nearest = new VectorInput { Dense = user.Vectors.Vector.Dense }
        },
            payloadSelector: new WithPayloadSelector { Enable = false },
            filter: f
        )).Select(scored => (int)scored.Id.Num).ToHashSet();
        var books = await _context.Books
            .Where(book => book.Visible && similar.Contains(book.BookId))
            .Select(book => book.ToPublicMessage())
            .ToArrayAsync();
        return new() { BooksData = { books } };
    }
}
