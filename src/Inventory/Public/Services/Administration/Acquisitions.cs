using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Google.Protobuf;

using Commons.Kafka;
using Commons.Extensions;
using Commons.Messages.Resources;
using DBM = Inventory.DBContext.Models;

using Inventory.Public.DBContext;

namespace Inventory.Public.Services.Administration;

public partial class InventoryAdministration
{
    [Authorize(Roles = "acquisitions, operations, admin")]
    public override async Task<Acquisitions> GetRecentAcquisitions(PaginatedResource request, ServerCallContext context)
    {
        request.Validate();
        List<Acquisition> acquisitions = await _context.Acquisitions
            .OrderByDescending(acquisition => acquisition.AcquisitionId)
            .Skip(request.Offset)
            .Take(request.Limit)
            .Select(acquisition => acquisition.Message())
            .ToListAsync();
        return new() { AcquisitionsData = { acquisitions } };
    }

    [Authorize(Roles = "acquisitions, operations, admin")]
    public override async Task<Acquisition> GetAcquisition(ResourceIdentifier request, ServerCallContext context)
    {
        request.Validate();
        DBM.Acquisition? acquisition = await _context.Acquisitions
            .FirstOrDefaultAsync(acquisition => acquisition.AcquisitionId == (int)request.Identifier);
        if (acquisition is null)
            throw new RpcException(new Status(StatusCode.NotFound, "The acquisition you were searching for was not found"));
        return acquisition.Message();
    }

    [Authorize(Roles = "acquisitions")]
    public override async Task<Acquisition> PostAcquisition(Acquisition request, ServerCallContext context)
    {
        request.Validate();

        DBM.Acquisition acquisition = new();
        acquisition.FromMessage(request);
        _context.Acquisitions.Add(acquisition);
        await _context.SaveChangesAsync();
        await _context.Entry(acquisition).ReloadAsync();
        request.AcquisitionId = (uint)acquisition.AcquisitionId;
        Acquisition result = acquisition.Message();
        _logger.LogInformation("{{\"Event\":\"AcquisitionCreated\",\"Acquisition\":{result},\"User\":{user}}}", result, context.GetUserId());
        return result;
    }

    [Authorize(Roles = "operations, admin")]
    public override async Task<Acquisition> PutAcquisition(Acquisition request, ServerCallContext context)
    {
        request.ClearBookId();
        request.ClearCopyIdStart();
        request.Validate();
        string role = context.GetUserRole();
        if (!request.HasAcquisitionId)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`AcquisitionId` is required for this operation"));
        if (role != "operations" && request.HasStatus)
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Only operations can change acquisition`s status"));
        if (role != "admin" && (request.HasAcquisitionPrice || request.HasQuantity || request.HasAcquisitionDate))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Only admins can change [`AcquisitionDate`, `AcquisitionPrice`, `Quantity`]"));
        DBM.Acquisition? acquisition = await _context.Acquisitions.FirstOrDefaultAsync(acquisition => acquisition.AcquisitionId == (int)request.AcquisitionId);
        if (acquisition is null)
            throw new RpcException(new Status(StatusCode.NotFound, "The acquisition you were searching for was not found"));
        acquisition.Update(request);
        await _context.SaveChangesAsync();
        if (request.HasStatus && request.Status == AcquisitionStatus.Confirmed)
            await _producer.ProduceAsync("book_available", new KafkaBook() { Identifier = (uint)acquisition.BookId }.ToByteArray());
        Acquisition result = acquisition.Message();
        _logger.LogInformation("{{\"Event\":\"AcquisitionUpdated\",\"Acquisition\":{request},\"User\":{user}}}", request, context.GetUserId());
        if (request.HasQuantity)
        {
            int new_acquisition = await _context.Database.SqlQuery<int>(
                $"SELECT inventory.update_acquisition_quantity({acquisition.AcquisitionId},{(int)request.Quantity}) AS \"Value\""
            ).FirstAsync();
            acquisition = await _context.Acquisitions.FirstAsync(acquisition => acquisition.AcquisitionId == new_acquisition);
            result = acquisition.Message();
            _logger.LogInformation("{{\"Event\":\"AcquisitionReplaced\",\"OriginalAcquisition\":{request},\"NewAcquisition\":{result},\"User\":{user}}}", request.AcquisitionId, result, context.GetUserId());
        }
        return result;
    }
}
