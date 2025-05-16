using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Grpc.Core;

using RentalService.Services.Operations;

using APIGateway.Dtos.RentalService.SOperations;

namespace APIGateway.Controllers.Administration;

[Route("operations/[controller]")]
[ApiController]
[Consumes("application/json")]
[Authorize(Roles = "operations")]
public class RentalsController(
    SRentalOperations.SRentalOperationsClient client
) : ControllerBase
{
    private readonly SRentalOperations.SRentalOperationsClient _client = client;

#pragma warning disable CS8604 // Possible null reference argument.
    public Metadata Headers => new()
    {
        { "Authorization", Request.Headers.Authorization }
    };
#pragma warning restore CS8604 // Possible null reference argument.

    [HttpGet]
    public async Task<IEnumerable<DtoRentalGET>> Get(uint page = 0, [Range(5, 50)] uint size = 10)
    {
        ConfirmedRentals response = await _client.GetConfirmedRentalsAsync(new() { Page = page, Size = size }, Headers);
        return response.Rentals.Select(rental => new DtoRentalGET(rental));
    }

    [HttpGet("{id}")]
    public async Task<DtoRentalGET> Get([Range(1, uint.MaxValue)] uint id)
    {
        ConfirmedRental response = await _client.GetRentalAsync(new() { Identifier = id }, Headers);
        return new(response);
    }

    [HttpPost("{id}")]
    public async Task<ActionResult> Post([Range(1, ulong.MaxValue)] ulong id)
    {
        await _client.PostBookDeliveredAsync(new() { Identifier = id }, Headers);
        return Ok();
    }

    [HttpPut("{rental_id}/{new_copy?}")]
    public async Task<ActionResult> Put([Range(1, ulong.MaxValue)] ulong rental_id, [Range(1, uint.MaxValue)] uint? new_copy)
    {
        RentalBookReasignment message = new() { RentalId = rental_id };
        if (new_copy.HasValue)
            message.NewCopy = new_copy.Value;
        await _client.PutRentalAsync(message, Headers);
        return Ok();
    }
}
