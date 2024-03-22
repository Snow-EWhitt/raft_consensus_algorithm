using Microsoft.AspNetCore.Mvc;
using Raft.Shared;

namespace Node.NodeController;

[ApiController]
[Route("[controller]")]
public class NodeController : ControllerBase
{
  private Raft.Node _node;
  private readonly ILogger<NodeController> _logger;

  public NodeController(Raft.Node node, ILogger<NodeController> logger)
  {
    _node = node;
    _logger = logger;
  }

  [HttpGet("getLeader")]
  public ActionResult<string> GetLeader()
  {
    var isLeader = _node.IsLeader();

    return Ok(isLeader);
  }

  [HttpGet("getLastLogIndex")]
  public IActionResult GetLastLogIndex()
  {
    try
    {
      int lastLogIndex = _node.LogEntries.Count - 1;

      return Ok(lastLogIndex);
    }
    catch
    {
      return StatusCode(500, "Internal server error while fetching index.");
    }
  }

  [HttpPost("requestVote")]
  public IActionResult RequestVote([FromBody] VoteRequest ballot)
  {
    // _logger.LogInformation("Received vote request.");

    try
    {
      bool vote = _node.Vote(ballot.Id, ballot.Term);

      return Ok(vote);
    }
    catch (Exception e)
    {
      _logger.LogError(e, "Error processing vote.");

      return StatusCode(500, "Internal server error while processing vote.");
    }
  }

  [HttpPost("sendHeartbeat")]
  public IActionResult SendHeartbeat([FromBody] HearbeatRequest heartbeat)
  {
    try
    {
      _node.ReceiveHeartBeat(heartbeat);

      return Ok();
    }
    catch
    {
      return StatusCode(500, "Internal server error while appending entries.");
    }
  }

  [HttpGet("eventualGet")]
  public ActionResult<(int? value, int logIndex)> EventualGet(string key)
  {
    Data result = _node.EventualGet(key);

    if (result != null)
    {
      return Ok(new { value = result.Value, result.LogIndex });
    }
    
    return NotFound();
  }

  [HttpGet("strongGet")]
  public ActionResult<(int? value, int logIndex)> StrongGet(string key)
  {
    Data result = _node.StrongGet(key);

    if (result != null)
    {
      return Ok(new { value = result.Value, result.LogIndex });
    }

    return BadRequest("Not the leader or key does not exist.");
  }

  [HttpPost("compareVersionAndSwap")]
  public ActionResult<bool> CompareVersionAndSwap([FromForm] string key, [FromForm] string expectedValue, [FromForm] string newValue)
  {
    var success = _node.CompareVersionAndSwap(key, expectedValue, newValue);
    
    return Ok(success);
  }

  [HttpPost("write")]
  public ActionResult<bool> Write([FromBody] WriteRequest write)
  {
    if (write == null)
      return BadRequest("Invalid request payload.");

    var success = _node.Write(write.Key, write.Value);

    if (success)
    {
      return Ok(true);
    }

    return BadRequest("Not the leader or operation failed.");
  }
}