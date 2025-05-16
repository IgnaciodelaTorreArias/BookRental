using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Google.Protobuf;

using Commons.Kafka;
using Commons.Messages.Resources;
using Commons.Extensions;
using DBM = Inventory.DBContext.Models;

using Inventory.Public.DBContext;

namespace Inventory.Public.Services.Administration;

public partial class InventoryAdministration
{
    [Authorize(Roles = "acquisitions, operations, admin")]
    public override async Task<BookCopies> GetAcquisitionCopies(PaginatedResource request, ServerCallContext context)
    {
        request.Validate();
        if (!request.HasIdentifier)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`Identifier` is required for this operation"));
        List<BookCopy> copies = await _context.Stocks
            .Where(copy => copy.AcquisitionId == (int)request.Identifier)
            .OrderBy(copy => copy.CopyId)
            .Skip(request.Offset)
            .Take(request.Limit)
            .Select(copy => copy.Message())
            .ToListAsync();
        return new() { Copies = { copies } };
    }

    [Authorize(Roles = "acquisitions, operations, admin")]
    public override async Task<BookCopy> GetBookCopy(ResourceIdentifier request, ServerCallContext context)
    {
        request.Validate();
        DBM.Stock? copy = await _context.Stocks
            .FirstOrDefaultAsync(copy => copy.BookId == (int)request.Identifier);
        if (copy is null)
            throw new RpcException(new Status(StatusCode.NotFound, "The book copy you were searching for was not found"));
        return copy.Message();
    }

    [Authorize(Roles = "operations")]
    public override async Task<Void> PutBookCopy(BookCopy request, ServerCallContext context)
    {
        request.ClearBookId();
        request.ClearAcquisitionId();
        request.Validate();
        DBM.Stock? copy = await _context.Stocks.FirstOrDefaultAsync(copy => copy.CopyId == (int)request.CopyId);
        if (copy is null)
            throw new RpcException(new Status(StatusCode.NotFound, "The book copy you were searching for was not found"));
        if (copy.Status == DBM.Stock.CopyStatus.Retired)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "The book copy is already retired, no modification allowed"));
        if (copy.Status == StockExtension.ConvertMessageStatus(request.Status))
            return new();
        copy.Status = StockExtension.ConvertMessageStatus(request.Status);
        await _context.SaveChangesAsync();
        if (request.Status == CopyStatus.Available)
            await _producer.ProduceAsync("book_available", new KafkaBook() { Identifier = (uint)copy.BookId }.ToByteArray());
        _logger.LogInformation("{{\"Event\":\"BookCopyUpdated\",\"BookCopy\":{request},\"User\":{user}}}", request, context.GetUserId());
        return new();
    }
}