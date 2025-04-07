using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using Commons.Messages.Resources;

using InventoryService.DBContext;

namespace InventoryService.Services.System;

[Authorize(Roles = "system")]
public class BookLend(InventoryContext context) : SBookLend.SBookLendBase
{
    private readonly InventoryContext _context = context;

    public override async Task<ResourceIdentifier> ReserveBook(ResourceIdentifier request, ServerCallContext context)
    {
        request.Validate();
        bool visible = await _context.Books
            .Where(book => book.BookId == (int)request.Identifier)
            .Select(book => book.Visible)
            .FirstOrDefaultAsync();
        if (!visible)
            return new() { Identifier = 0 };
        int? copyId = await _context.Database.SqlQuery<int?>(
                $"SELECT inventory.get_book_lease({(int)request.Identifier}) AS \"Value\""
            ).FirstAsync();
        if (!copyId.HasValue)
            return new() { Identifier = 0 };
        return new() { Identifier = (uint)copyId };
    }
}
