using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Google.Protobuf;

using Commons.Kafka;
using Commons.Auth.BearerToken;
using Commons.Extensions;
using Commons.Messages.Resources;
using InventoryService.Services.System;

using RentalService.DBContext;

namespace RentalService.Services.User;

[Authorize(Roles = "user")]
public class Rental(
    RentalContext context,
    SBookLend.SBookLendClient client,
    ITokenService token,
    IKafkaProducer producer
) : SRental.SRentalBase
{
    private readonly RentalContext _context = context;
    private readonly SBookLend.SBookLendClient _client = client;
    private readonly ITokenService _tokenService = token;
    private readonly IKafkaProducer _producer = producer;

    public override async Task<RentalResult> PostBookRental(ResourceIdentifier request, ServerCallContext context)
    {
        request.Validate();
        DBContext.Models.Book? book = await _context.Books.FirstOrDefaultAsync(book => book.BookId == (int)request.Identifier && book.Visible == true);
        if (book is null)
            return new() { Status = RentalStatus.UnavailableBook };
        DBContext.Models.WaitingList? waiting = await _context.WaitingLists.FirstOrDefaultAsync(wait => wait.BookId == (int)request.Identifier);
        int userId = context.GetUserId();
        if (waiting != null)
        {
            _context.WaitingLists.Add(new() { BookId = (int)request.Identifier, UserId = userId });
            await _context.SaveChangesAsync();
            return new() { Status = RentalStatus.JoinedWaitingList };
        }
        string token = await _tokenService.GetTokenAsync();
        ResourceIdentifier response = await _client.ReserveBookAsync(new() { Identifier = request.Identifier }, new Metadata()
        {
            {"Authorization", $"Bearer {token}" }
        });
        if (response.Identifier != 0)
        {
            _context.ConfirmedRentals.Add(new() { BookId = (int)request.Identifier, CopyId = (int)response.Identifier, UserId = userId, RentalFee = book.RentalFee });
            await _context.SaveChangesAsync();
            await _producer.ProduceAsync("confirmed_rental", new KafkaRental() { CopyId = response.Identifier, UserId = (uint)userId }.ToByteArray());
            return new() { Status = RentalStatus.ReservedBook };
        }
        else
        {
            _context.WaitingLists.Add(new() { BookId = (int)request.Identifier, UserId = userId });
            await _context.SaveChangesAsync();
            return new() { Status = RentalStatus.JoinedWaitingList };
        }
    }

    public override async Task<WaitingRentals> GetWaitingRentals(PaginatedResource request, ServerCallContext context)
    {
        request.Validate();
        List<WaitingRental> waitingRentals = await _context.WaitingLists
            .OrderByDescending(rental => rental.WaitingId)
            .Skip(request.Offset)
            .Take(request.Limit)
            .Select(rental => rental.Message())
            .ToListAsync();
        return new() { Rentals = { waitingRentals } };
    }

    public override async Task<NotifiedRentals> GetNotifiedRentals(PaginatedResource request, ServerCallContext context)
    {
        request.Validate();
        List<NotifiedRental> notifiedRentals = await _context.Notifieds
            .OrderBy(rental => rental.NotificationId)
            .Skip(request.Offset)
            .Take(request.Limit)
            .Select(rental => rental.Message())
            .ToListAsync();
        return new() { Rentals = { notifiedRentals } };
    }

    public override async Task<ConfirmedRentals> GetConfirmedRentals(PaginatedResource request, ServerCallContext context)
    {
        request.Validate();
        List<ConfirmedRental> confirmedRentals = await _context.ConfirmedRentals
            .OrderBy(rental => rental.Identifier)
            .Skip(request.Offset)
            .Take(request.Limit)
            .Select(rental => rental.PublicMessage())
            .ToListAsync();
        return new() { Rentals = { confirmedRentals } };
    }

    public override async Task<RentalRegistries> GetPastRentals(PaginatedResource request, ServerCallContext context)
    {
        request.Validate();
        List<RentalRegistry> pastRentals = await _context.Rentals
            .OrderByDescending(rental => rental.RentalId)
            .Skip(request.Offset)
            .Take(request.Limit)
            .Select(rental => rental.Message())
            .ToListAsync();
        return new() { Rentals = { pastRentals } };
    }

    public override async Task<ConfirmationResult> PostConfirmRental(ResourceIdentifier request, ServerCallContext context)
    {
        request.Validate();
        int userId = context.GetUserId();
        bool confirmed = await _context.Database.SqlQuery<bool>(
                $"SELECT inventory.user_confirmed_rental({(int)request.Identifier},{userId}) AS \"Value\""
            ).FirstAsync();
        if (!confirmed)
            return new() { Status = ConfirmedRentalStatus.Late };
        await _producer.ProduceAsync("confirmed_rental", new KafkaRental() { CopyId = request.Identifier, UserId = (uint)userId }.ToByteArray());
        return new() { Status = ConfirmedRentalStatus.Confirmed };
    }
}