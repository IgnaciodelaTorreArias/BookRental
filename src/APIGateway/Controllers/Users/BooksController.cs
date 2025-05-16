using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Grpc.Core;

using RentalService.Services.User;
using Inventory.Public.Services.Consumer;
using APIGateway.Dtos.Inventory.SConsumer.Books;

namespace APIGateway.Controllers.Users;

[Route("[controller]")]
[ApiController]
public class BooksController(
    SInventoryConsumer.SInventoryConsumerClient clientI,
    SRental.SRentalClient clientR
) : ControllerBase
{
    private readonly SInventoryConsumer.SInventoryConsumerClient _clientI = clientI;
    private readonly SRental.SRentalClient _clientR = clientR;

#pragma warning disable CS8604 // Possible null reference argument.
    public Metadata Headers => new()
    {
        { "Authorization", Request.Headers.Authorization }
    };
#pragma warning restore CS8604 // Possible null reference argument.

    [HttpGet]
    [AllowAnonymous]
    public async Task<IEnumerable<DtoBookGET>> Get(uint page = 0, [Range(5, 50)] uint size = 10)
    {
        Books response = await _clientI.GetRecentBooksAsync(new() { Page = page, Size = size });
        return response.BooksData.Select(book => new DtoBookGET(book));
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<DtoBookGET> Get([Range(1, uint.MaxValue)] uint id)
    {
        Book response = await _clientI.GetBookAsync(new() { Identifier = id });
        return new(response);
    }

    [HttpPost("{id}")]
    [Authorize(Roles = "user")]
    public async Task<RentalStatus> Post([Range(1, uint.MaxValue)] uint id)
    {
        RentalResult response = await _clientR.PostBookRentalAsync(new() { Identifier = id }, Headers);
        return response.Status;
    }
}