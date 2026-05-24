using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;
using SWP391_AutoWashPro_BE.Service.Vehicles;
using SWP391_AutoWashPro_BE.Service.Vehicles;
using Request = SWP391_AutoWashPro_BE.Service.Vehicles.Request;

namespace SWP391_AutoWashPro_BE.Api.Controllers;

[ApiController]
[Route("api/v1/vehicles")]
[Authorize]
public class VehicleController:ControllerBase
{
    private readonly IService _service;
    public VehicleController(IService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetUserVehicles([FromQuery] int page, int pageSize)
    {
        var result = await _service.GetVehicles(page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetVehicleById(Guid id)
    {
        var result = await _service.GetVehicleById(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateVehicle(Request.CreateVehicleRequest request)
    {
        var result = await _service.CreateVehicle(request);
        return Ok(result);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateVehicle(Guid id, Request.UpdateVehicleRequest request)
    {
        var result = await _service.UpdateVehicle(id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVehicle(Guid id)
    {
        var result = await _service.DeleteVehicle(id);
        return Ok(result);
    }
}