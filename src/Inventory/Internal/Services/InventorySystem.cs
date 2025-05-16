using Grpc.Core;
using Microsoft.EntityFrameworkCore;

using Commons.Messages.Resources;
using Inventory.DBContext;

namespace Inventory.Internal.Services;

public class InventorySystem(InventoryContext context) : SInventorySystem.SInventorySystemBase
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
