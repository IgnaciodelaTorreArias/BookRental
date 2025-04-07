using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Grpc.Core;

using InventoryService.Services.Administration;

using APIGateway.Dtos.InventoryService.SAdministration.Acquisitions;

namespace APIGateway.Controllers.Administration;

[Route("administration/[controller]")]
[ApiController]
[Consumes("application/json")]
public class AcquisitionsController(
    SInvenotryAdministration.SInvenotryAdministrationClient client
) : ControllerBase
{
    private readonly SInvenotryAdministration.SInvenotryAdministrationClient _client = client;

#pragma warning disable CS8604 // Possible null reference argument.
    public Metadata Headers => new()
    {
        { "Authorization", Request.Headers.Authorization }
    };
#pragma warning restore CS8604 // Possible null reference argument.

    [HttpGet]
    [Authorize(Roles = "acquisitions, operations, admin")]
    public async Task<IEnumerable<DtoAcquisitionGET>> Get(uint page = 0, [Range(5, 50)] uint size = 10)
    {
        Acquisitions response = await _client.GetRecentAcquisitionsAsync(new() { Page = page, Size = size }, Headers);
        return response.AcquisitionsData.Select(acquisition => new DtoAcquisitionGET(acquisition));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "acquisitions, operations, admin")]
    public async Task<DtoAcquisitionGET> Get([Range(1, uint.MaxValue)] uint id)
    {
        Acquisition response = await _client.GetAcquisitionAsync(new() { Identifier = id }, Headers);
        return new(response);
    }

    [HttpPost]
    [Authorize(Roles = "acquisitions")]
    public async Task<ActionResult<int>> Post(DtoAcquisitionPOST acquisition)
    {
        Acquisition response = await _client.PostAcquisitionAsync(acquisition.Message(), Headers);
        return CreatedAtAction(nameof(Get), new { id = response.AcquisitionId }, new DtoAcquisitionGET(response));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "operations, admin")]
    public async Task<ActionResult<DtoAcquisitionGET>> Put([Range(1, uint.MaxValue)] uint id, [FromBody] DtoAcquisitionPUT acquisition)
    {
        if (id != acquisition.AcquisitionId)
            return BadRequest("ID in URI does not match ID in body");
        string? role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role != "operations" && acquisition.AcquisitionStatus.HasValue)
            return Forbid("Only operations can change acquisition`s status");
        if (role != "admin" && (acquisition.AcquisitionPrice.HasValue || acquisition.Quantity.HasValue || acquisition.AcquisitionDate.HasValue))
            return Forbid("Only admins can change [`AcquisitionDate`, `AcquisitionPrice`, `Quantity`]");
        Acquisition response = await _client.PutAcquisitionAsync(acquisition.Message(), Headers);
        return Ok(new DtoAcquisitionGET(response));
    }
}
