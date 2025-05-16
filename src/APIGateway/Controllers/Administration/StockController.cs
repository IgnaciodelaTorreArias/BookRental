using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using Grpc.Core;

using Inventory.Public.Services.Administration;
using APIGateway.Dtos.Inventory.SAdministration.Stock;

namespace APIGateway.Controllers.Administration;

[Route("[controller]")]
[ApiController]
[Consumes("application/json")]
public class StockController(
    SInvenotryAdministration.SInvenotryAdministrationClient client
) : ControllerBase
{
    private readonly SInvenotryAdministration.SInvenotryAdministrationClient _client = client;

#pragma warning disable CS8604 // Possible null reference argument.
    public Metadata headers => new()
    {
        { "Authorization", Request.Headers.Authorization }
    };
#pragma warning restore CS8604 // Possible null reference argument.

    [HttpGet("acquisitions/{id}")]
    [Authorize(Roles = "acquisitions, operations, admin")]
    public async Task<IEnumerable<DtoBookCopyGET>> Get([Range(1, uint.MaxValue)] uint id, uint page = 0, [Range(5, 50)] uint size = 10)
    {
        BookCopies response = await _client.GetAcquisitionCopiesAsync(new() { Page = page, Size = size, Identifier = id }, headers);
        return response.Copies.Select(response => new DtoBookCopyGET(response));
    }

    [HttpGet("books/{id}")]
    [Authorize(Roles = "acquisitions, operations, admin")]
    public async Task<DtoBookCopyGET> Get([Range(1, uint.MaxValue)] uint id)
    {
        BookCopy response = await _client.GetBookCopyAsync(new() { Identifier = id }, headers);
        return new DtoBookCopyGET(response);
    }

    [HttpPut("books/{id}")]
    [Authorize(Roles = "operations")]
    public async Task<ActionResult> Put([Range(1, int.MaxValue)] int id, [FromBody] DtoBookCopyPUT copy)
    {
        await _client.PutBookCopyAsync(copy.Message(), headers);
        return NoContent();
    }
}