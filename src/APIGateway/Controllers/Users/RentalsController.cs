using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grpc.Core;

using RentalService.Services.User;

using APIGateway.Dtos.RentalService.SUser;

namespace APIGateway.Controllers.Users;

[Route("api/[controller]")]
[ApiController]
[Consumes("application/json")]
[Authorize(Roles = "user")]
public class RentalsController(
    SRental.SRentalClient client
) : ControllerBase
{

    private readonly SRental.SRentalClient _client = client;

#pragma warning disable CS8604 // Possible null reference argument.
    public Metadata Headers => new()
    {
        { "Authorization", Request.Headers.Authorization }
    };
#pragma warning restore CS8604 // Possible null reference argument.

    [HttpGet("waiting")]
    public async Task<IEnumerable<DtoRentalGET>> GetWaiting(uint page = 0, [Range(5, 50)] uint size = 10)
    {
        WaitingRentals response = await _client.GetWaitingRentalsAsync(new() { Page = page, Size = size }, Headers);
        return response.Rentals.Select(rental => new DtoRentalGET(rental));
    }

    [HttpGet("notified")]
    public async Task<IEnumerable<DtoRentalGET>> GetNotified(uint page = 0, [Range(5, 50)] uint size = 10)
    {
        NotifiedRentals response = await _client.GetNotifiedRentalsAsync(new() { Page = page, Size = size }, Headers);
        return response.Rentals.Select(rental => new DtoRentalGET(rental));
    }

    [HttpGet("confirmed")]
    public async Task<IEnumerable<DtoRentalGET>> GetConfirmed(uint page = 0, [Range(5, 50)] uint size = 10)
    {
        ConfirmedRentals response = await _client.GetConfirmedRentalsAsync(new() { Page = page, Size = size }, Headers);
        return response.Rentals.Select(rental => new DtoRentalGET(rental));
    }

    [HttpGet("history")]
    public async Task<IEnumerable<DtoRentalRegistryGET>> GetPast(uint page = 0, [Range(5, 50)] uint size = 10)
    {
        RentalRegistries response = await _client.GetPastRentalsAsync(new() { Page = page, Size = size }, Headers);
        return response.Rentals.Select(rental => new DtoRentalRegistryGET(rental));
    }

    [HttpPost("{id}")]
    public async Task<ConfirmedRentalStatus> Post([Range(1, uint.MaxValue)] uint id)
    {
        ConfirmationResult response = await _client.PostConfirmRentalAsync(new() { Identifier = id }, Headers);
        return response.Status;
    }
}
