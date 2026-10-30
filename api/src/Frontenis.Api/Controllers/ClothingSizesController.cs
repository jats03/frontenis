using Frontenis.Api.Authorization;
using Frontenis.Api.RateLimiting;
using Frontenis.Application.Dtos;
using Frontenis.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Frontenis.Api.Controllers;

/// <summary>Tallas de ropa (camiseta y pantalón): las indica cada jugador desde su dispositivo; el administrador las consulta todas.</summary>
[ApiController]
public sealed class ClothingSizesController : ControllerBase
{
    private readonly IClothingSizeService _sizes;
    public ClothingSizesController(IClothingSizeService sizes) { _sizes = sizes; }

    /// <summary>Tallas del titular de este dispositivo y de sus apadrinados.</summary>
    [HttpGet("api/device/sizes")]
    public async Task<ActionResult<IReadOnlyList<ClothingSizeDto>>> Mine(
        [FromHeader(Name = DeviceController.TokenHeader)] string? token, CancellationToken ct) =>
        Ok(await _sizes.ListForDeviceAsync(token, ct));

    /// <summary>Guarda las tallas del titular o de un apadrinado.</summary>
    [HttpPut("api/device/sizes/{playerId:int}"), EnableRateLimiting(RateLimitPolicies.PublicWrite)]
    public async Task<ActionResult<ClothingSizeDto>> Save(int playerId,
        [FromHeader(Name = DeviceController.TokenHeader)] string? token, SaveClothingSizeRequest request, CancellationToken ct) =>
        Ok(await _sizes.SaveForDeviceAsync(token, playerId, request, ct));

    /// <summary>Tallas de todos los jugadores (administrador).</summary>
    [HttpGet("api/clothing-sizes"), AdminOnly]
    public async Task<ActionResult<IReadOnlyList<ClothingSizeDto>>> All(CancellationToken ct) =>
        Ok(await _sizes.ListAllAsync(ct));
}
