using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using Commons.Extensions;
using Commons.Messages.Resources;
using Commons.Auth.BearerToken;
using Inventory.Internal.Services;

using RentalService.DBContext;
using DBM = RentalService.DBContext.Models;

namespace RentalService.Services.Operations;

[Authorize(Roles = "operations")]
public class RentalOperations(
    RentalContext context,
    SInventorySystem.SInventorySystemClient client,
    ILogger<RentalOperations> logger
) : SRentalOperations.SRentalOperationsBase
{
    private readonly RentalContext _context = context;
    private readonly SInventorySystem.SInventorySystemClient _client = client;
    private readonly ILogger<RentalOperations> _logger = logger;

    public override async Task<ConfirmedRentals> GetConfirmedRentals(PaginatedResource request, ServerCallContext context)
    {
        request.Validate();
        var rentals = await _context.ConfirmedRentals
            .OrderByDescending(rental => rental.Identifier)
            .Skip(request.Offset)
            .Take(request.Limit)
            .Select(rental => rental.PrivateMessage())
            .ToArrayAsync();
        return new() { Rentals = { rentals } };
    }

    public override async Task<ConfirmedRental> GetRental(LongResourceIdentifier request, ServerCallContext context)
    {
        request.Validate();
        DBM.ConfirmedRental? rental = await _context.ConfirmedRentals
            .FirstOrDefaultAsync(rental => rental.Identifier == (long)request.Identifier);
        if (rental is null)
            throw new RpcException(new Status(StatusCode.NotFound, "Rental not found"));
        return rental.PrivateMessage();
    }

    public override async Task<Void> PostBookDelivered(LongResourceIdentifier request, ServerCallContext context)
    {
        request.Validate();
        uint confirmer = context.GetUserId();
        long? rental = await _context.Database.SqlQuery<long?>(
                $"SELECT inventory.initiate_rental({(long)request.Identifier},{(int)confirmer}) AS \"Value\""
            ).FirstAsync();
        if (rental is null)
            throw new RpcException(new Status(StatusCode.NotFound, "Rental not found"));
        return new();
    }

    public override async Task<Void> PutRental(RentalBookReasignment request, ServerCallContext context)
    {
        request.Validate();
        DBM.ConfirmedRental? rental;
        if (request.HasNewCopy)
        {
            rental = new DBM.ConfirmedRental() { Identifier = (long)request.RentalId };
            _context.ConfirmedRentals.Attach(rental);
            rental.CopyId = (int)request.NewCopy;
            await _context.SaveChangesAsync();
            _logger.LogInformation("{{\"Event\":\"BookReasignment\",\"RentalId\":{request},\"NewCopy\":{NewCopy},\"Manual\":true,\"User\":{user}}}", request.RentalId, request.NewCopy, context.GetUserId());
            return new();
        }
        rental = await _context.ConfirmedRentals
            .FirstOrDefaultAsync(rental => rental.Identifier == (long)request.RentalId);
        if (rental is null)
            return new();
        ResourceIdentifier response = await _client.ReserveBookAsync(new() { Identifier = (uint)rental.BookId });
        if (response.Identifier == 0)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, "Out of Stock"));
        rental.CopyId = (int)response.Identifier;
        await _context.SaveChangesAsync();
        _logger.LogInformation("{{\"Event\":\"BookReasignment\",\"RentalId\":{request},\"NewCopy\":{NewCopy},\"Manual\":false,\"User\":{user}}}", request.RentalId, response.Identifier, context.GetUserId());
        return new();
    }
}