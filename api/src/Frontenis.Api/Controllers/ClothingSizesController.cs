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

    /// <summary>Resumen de tallas de todos los jugadores para quien el administrador ha autorizado.</summary>
    [HttpGet("api/device/sizes/all")]
    public async Task<ActionResult<IReadOnlyList<ClothingSizeDto>>> AllForDevice(
        [FromHeader(Name = DeviceController.TokenHeader)] string? token, CancellationToken ct) =>
        Ok(await _sizes.ListAllForDeviceAsync(token, ct));

    /// <summary>Jugadores con acceso al resumen de tallas (administrador).</summary>
    [HttpGet("api/clothing-sizes/viewers"), AdminOnly]
    public async Task<ActionResult<IReadOnlyList<PlayerRefDto>>> Viewers(CancellationToken ct) =>
        Ok(await _sizes.ListViewersAsync(ct));

    /// <summary>Da acceso al resumen de tallas a un jugador (administrador).</summary>
    [HttpPut("api/clothing-sizes/viewers/{playerId:int}"), AdminOnly]
    public async Task<ActionResult<IReadOnlyList<PlayerRefDto>>> AddViewer(int playerId, CancellationToken ct) =>
        Ok(await _sizes.SetViewerAsync(playerId, true, ct));

    /// <summary>Quita a un jugador el acceso al resumen de tallas (administrador).</summary>
    [HttpDelete("api/clothing-sizes/viewers/{playerId:int}"), AdminOnly]
    public async Task<ActionResult<IReadOnlyList<PlayerRefDto>>> RemoveViewer(int playerId, CancellationToken ct) =>
        Ok(await _sizes.SetViewerAsync(playerId, false, ct));

    /// <summary>Tallas de todos los jugadores (administrador).</summary>
    [HttpGet("api/clothing-sizes"), AdminOnly]
    public async Task<ActionResult<IReadOnlyList<ClothingSizeDto>>> All(CancellationToken ct) =>
        Ok(await _sizes.ListAllAsync(ct));
}
