using Backend.DTOs.MultiDevice;
using Backend.DTOs.Passkey;
using Backend.Services.Passkey;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/passkey/device")]
[Authorize]
public class PasskeyDeviceController : ControllerBase
{
    private readonly IPasskeyService _service;

    public PasskeyDeviceController(IPasskeyService service)
    {
        _service = service;
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetDevices()
    {
        var userId = int.Parse(User.FindFirst("id")!.Value);
        var devices = await _service.GetDevicesAsync(userId);
        return Ok(devices);
    }

    [HttpPut("{credentialId}")]
    public async Task<IActionResult> UpdateDeviceName(string credentialId, UpdateDeviceNameRequestDto request)
    {
        var userId = int.Parse(User.FindFirst("id")!.Value);
        var updated = await _service.UpdateDeviceNameAsync(userId, credentialId, request.DeviceName);
        return Ok(updated);
    }

    [HttpDelete("{credentialId}")]
    public async Task<IActionResult> DeleteDevice(string credentialId)
    {
        var userId = int.Parse(User.FindFirst("id")!.Value);

        var result = await _service.DeleteDeviceAsync(userId, credentialId);

        return Ok(result);
    }
    [HttpPost("add/begin")]
    public async Task<IActionResult> AddDeviceBegin()
    {
        var userId = int.Parse(User.FindFirst("id")!.Value);
        var result = await _service.AddDeviceBeginAsync(userId);
        return Ok(result);
    }

    [HttpPost("add/complete")]
    public async Task<IActionResult> AddDeviceComplete(PasskeyRegisterCompleteRequestDto request)
    {
        var userId = int.Parse(User.FindFirst("id")!.Value);
        var result = await _service.AddDeviceCompleteAsync(userId, request);
        return Ok(result);
    }

    [HttpDelete("delete-account")]
    public async Task<IActionResult> DeleteAccount()
    {
        var userId = int.Parse(User.FindFirst("id")!.Value);
        await _service.DeleteAccountAsync(userId);
        return Ok(new { Success = true });
    }
}
