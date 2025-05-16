using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Grpc.Core;

using Inventory.Public.Services.Administration;
using APIGateway.Dtos.Inventory.SAdministration.Books;

namespace APIGateway.Controllers.Administration;

[Route("administration/[controller]")]
[ApiController]
[Consumes("application/json")]
public class BooksController : ControllerBase
{
    private readonly SInvenotryAdministration.SInvenotryAdministrationClient _client;

    public BooksController(SInvenotryAdministration.SInvenotryAdministrationClient client)
    {
        _client = client;
    }

#pragma warning disable CS8604 // Possible null reference argument.
    public Metadata Headers => new()
    {
        { "Authorization", Request.Headers.Authorization }
    };
#pragma warning restore CS8604 // Possible null reference argument.

    [HttpGet]
    [Authorize(Roles = "acquisitions, operations, admin")]
    public async Task<IEnumerable<DtoBookGET>> Get(uint page = 0, [Range(5, 50)] uint size = 10)
    {
        Books response = await _client.GetRecentBooksAdministrationAsync(new() { Page = page, Size = size }, Headers);
        return response.BooksData.Select(book => new DtoBookGET(book));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "acquisitions, operations, admin")]
    public async Task<DtoBookGET> Get([Range(1, int.MaxValue)] uint id)
    {
        Book response = await _client.GetBookAdministrationAsync(new() { Identifier = id }, Headers);
        return new DtoBookGET(response);
    }

    [HttpPost]
    [Authorize(Roles = "acquisitions")]
    public async Task<ActionResult> Post(DtoBookPOST book)
    {
        Book response = await _client.PostBookAsync(book.Message(), Headers);
        return CreatedAtAction(nameof(Get), new { id = response.BookId }, new DtoBookGET(response));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "acquisitions, admin")]
    public async Task<ActionResult<DtoBookGET>> Put([Range(1, int.MaxValue)] int id, [FromBody] DtoBookPUT book)
    {
        if (id != book.BookId)
            return BadRequest("ID in URI does not match ID in body");
        Book response = await _client.PutBookAsync(book.Message(), Headers);
        return Ok(new DtoBookGET(response));
    }
}