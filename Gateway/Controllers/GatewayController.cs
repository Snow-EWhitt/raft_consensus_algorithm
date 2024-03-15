using Microsoft.AspNetCore.Mvc;

namespace Gateway.GatewayController;

[ApiController]
[Route("[controller]")]
public class GatewayController : ControllerBase
{
  private Raft.Gateway _gateway;

  public GatewayController(Raft.Gateway gateway)
  {
    _gateway = gateway;
  }

  [HttpGet("EventualGet")]
  public async Task<ActionResult> EventualGet(string key)
  {
    var result = await _gateway.EventualGet(key);
    
    if (result.HasValue)
    {
      var (value, logIndex) = result.Value;

      return Ok(new { value, logIndex });
    }
    else
    {
      return NotFound("No value found for the key.");
    }
  }

  [HttpGet("StrongGet")]
  public async Task<ActionResult> StrongGet(string key)
  {
    var result = await _gateway.StrongGet(key);

    if (result.HasValue)
    {
      var (value, logIndex) = result.Value;

      return Ok(new { value, logIndex });
    }
    else
    {
      return NotFound("No value");
    }
  }

  [HttpPost("CompareVersionAndSwap")]
  public async Task<ActionResult<bool>> CompareVersionAndSwap(string key, int expectedValue, int newValue)
  {
    var result = await _gateway.CompareVersionAndSwap(key, expectedValue, newValue);
    return Ok(result);
  }

  [HttpPost("Write")]
  public async Task<ActionResult<bool>> Write(string key, int value)
  {
    var result = await _gateway.Write(key, value);

    return Ok(result);
  }
}