using System.Numerics.Tensors;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Google.Protobuf;

using Qdrant.Client;
using Qdrant.Client.Grpc;

using Commons.Kafka;
using Commons.Extensions;
using Commons.Messages.Resources;
using Inventory.Internal.Services;

using RentalService.DBContext;

namespace RentalService.Services.User;

[Authorize(Roles = "user")]
public class Rental(
    RentalContext context,
    SInventorySystem.SInventorySystemClient client,
    IKafkaProducer producer,
    QdrantClient qdrant
) : SRental.SRentalBase
{
    private readonly RentalContext _context = context;
    private readonly SInventorySystem.SInventorySystemClient _client = client;
    private readonly IKafkaProducer _producer = producer;
    private readonly QdrantClient _qdrant = qdrant;

    public async Task BookRented(uint bookId, uint userId)
    {
        RetrievedPoint book;
        RetrievedPoint? user;
        var result = await Task.WhenAll(
            _qdrant.RetrieveAsync("books", bookId, withVectors: true),
            _qdrant.RetrieveAsync("users", userId, withVectors: true)
        );
        book = result[0][0];
        user = result[1].FirstOrDefault();
        DenseVector dense = new();
        if (user is null)
        {
            dense.Data.AddRange(book.Vectors.Vector.Dense.Data);
            PointStruct userAdd = new()
            {
                Id = new PointId { Num = (ulong)userId },
                Vectors = new Vectors { Vector = new Vector { Dense = dense } }
            };
            userAdd.Payload["history"].ListValue.Values.Add(bookId);
            await _qdrant.UpsertAsync("users", [userAdd]);
            return;
        }
        user.Payload["history"].ListValue.Values.Add(bookId);
        var count = user.Payload["history"].ListValue.Values.Count;
        float[] averageVector = user.Vectors.Vector.Dense.Data.ToArray();
        float[] newVector = book.Vectors.Vector.Dense.Data.ToArray();
        TensorPrimitives.Subtract(newVector, averageVector, newVector);
        TensorPrimitives.Divide(newVector, count, newVector);
        TensorPrimitives.Add(averageVector, newVector, averageVector);
        dense.Data.AddRange(averageVector);
        PointStruct userUpdate = new()
        {
            Id = new PointId { Num = (ulong)userId },
            Vectors = new Vectors { Vector = new Vector { Dense = dense } }
        };
        userUpdate.Payload.Add(user.Payload);
        await _qdrant.UpsertAsync("users", [userUpdate]);
    }

    public override async Task<RentalResult> PostBookRental(ResourceIdentifier request, ServerCallContext context)
    {
        request.Validate();
        DBContext.Models.Book? book = await _context.Books.FirstOrDefaultAsync(book => book.BookId == (int)request.Identifier && book.Visible == true);
        if (book is null)
            return new() { Status = RentalStatus.UnavailableBook };
        DBContext.Models.WaitingList? waiting = await _context.WaitingLists.FirstOrDefaultAsync(wait => wait.BookId == request.Identifier);
        uint userId = context.GetUserId();
        if (waiting != null)
        {
            _context.WaitingLists.Add(new() { BookId = (int)request.Identifier, UserId = (int)userId });
            await _context.SaveChangesAsync();
            return new() { Status = RentalStatus.JoinedWaitingList };
        }
        ResourceIdentifier response = await _client.ReserveBookAsync(new() { Identifier = request.Identifier });
        if (response.Identifier != 0)
        {
            _context.ConfirmedRentals.Add(new() { BookId = (int)request.Identifier, CopyId = (int)response.Identifier, UserId = (int)userId, RentalFee = book.RentalFee });
            await Task.WhenAll(
                _context.SaveChangesAsync(),
                _producer.ProduceAsync("confirmed_rental", new KafkaRental() { CopyId = response.Identifier, UserId = (uint)userId }.ToByteArray()),
                BookRented(request.Identifier, userId)
            );
            return new() { Status = RentalStatus.ReservedBook };
        }
        else
        {
            _context.WaitingLists.Add(new() { BookId = (int)request.Identifier, UserId = (int)userId });
            await _context.SaveChangesAsync();
            return new() { Status = RentalStatus.JoinedWaitingList };
        }
    }

    public override async Task<WaitingRentals> GetWaitingRentals(PaginatedResource request, ServerCallContext context)
    {
        request.Validate();
        uint userId = context.GetUserId();
        var waitingRentals = await _context.WaitingLists
            .Where(rental => rental.UserId == userId)
            .OrderByDescending(rental => rental.WaitingId)
            .Skip(request.Offset)
            .Take(request.Limit)
            .Select(rental => rental.Message())
            .ToArrayAsync();
        return new() { Rentals = { waitingRentals } };
    }

    public override async Task<NotifiedRentals> GetNotifiedRentals(PaginatedResource request, ServerCallContext context)
    {
        request.Validate();
        uint userId = context.GetUserId();
        var notifiedRentals = await _context.Notifieds
            .Where(rental => rental.UserId == userId)
            .OrderBy(rental => rental.NotificationId)
            .Skip(request.Offset)
            .Take(request.Limit)
            .Select(rental => rental.Message())
            .ToArrayAsync();
        return new() { Rentals = { notifiedRentals } };
    }

    public override async Task<ConfirmedRentals> GetConfirmedRentals(PaginatedResource request, ServerCallContext context)
    {
        request.Validate();
        uint userId = context.GetUserId();
        var confirmedRentals = await _context.ConfirmedRentals
            .Where(rental => rental.UserId == userId)
            .OrderBy(rental => rental.Identifier)
            .Skip(request.Offset)
            .Take(request.Limit)
            .Select(rental => rental.PublicMessage())
            .ToArrayAsync();
        return new() { Rentals = { confirmedRentals } };
    }

    public override async Task<RentalRegistries> GetPastRentals(PaginatedResource request, ServerCallContext context)
    {
        request.Validate();
        uint userId = context.GetUserId();
        var pastRentals = await _context.Rentals
            .Where(rental => rental.UserId == userId)
            .OrderByDescending(rental => rental.RentalId)
            .Skip(request.Offset)
            .Take(request.Limit)
            .Select(rental => rental.Message())
            .ToArrayAsync();
        return new() { Rentals = { pastRentals } };
    }

    public override async Task<ConfirmationResult> PostConfirmRental(ResourceIdentifier request, ServerCallContext context)
    {
        request.Validate();
        uint userId = context.GetUserId();
        int? bookId = await _context.Database.SqlQuery<int?>(
                $"SELECT inventory.user_confirmed_rental({(int)request.Identifier},{userId}) AS \"Value\""
            ).FirstAsync();
        if (bookId is null)
            return new() { Status = ConfirmedRentalStatus.Late };
        await Task.WhenAll(
            _producer.ProduceAsync("confirmed_rental", new KafkaRental() { CopyId = request.Identifier, UserId = (uint)userId }.ToByteArray()),
            BookRented((uint)bookId, userId)
        );
        return new() { Status = ConfirmedRentalStatus.Confirmed };
    }
}