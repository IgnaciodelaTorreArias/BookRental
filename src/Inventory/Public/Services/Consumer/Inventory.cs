using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using Commons.Messages.Resources;
using Inventory.DBContext;
using DBM = Inventory.DBContext.Models;

using Inventory.Public.DBContext;

namespace Inventory.Public.Services.Consumer;

[AllowAnonymous]
public class InventoryConsumer(InventoryContext context) : SInventoryConsumer.SInventoryConsumerBase
{
    private readonly InventoryContext _context = context;

    public override async Task<Books> GetRecentBooks(PaginatedResource request, ServerCallContext context)
    {
        request.Validate();
        List<Book> books = await _context.Books
            .Where(book => book.Visible)
            .OrderByDescending(book => book.BookId)
            .Skip(request.Offset)
            .Take(request.Limit)
            .Select(book => book.ToPublicMessage())
            .ToListAsync();
        return new() { BooksData = { books } };
    }

    public override async Task<Book> GetBook(ResourceIdentifier request, ServerCallContext context)
    {
        request.Validate();
        DBM.Book? book = await _context.Books
            .FirstOrDefaultAsync(book => book.BookId == (int)request.Identifier && book.Visible);
        if (book is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "The book you were searching for was not found"));
        }
        return book.ToPublicMessage();
    }
}
