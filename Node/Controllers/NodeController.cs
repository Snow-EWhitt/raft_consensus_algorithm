using Microsoft.AspNetCore.Mvc;
using Raft;

namespace Node.NodeController;

[ApiController]
[Route("[controller]")]
public class NodeController : ControllerBase
{
  private Raft.Node _node;

  public NodeController(Raft.Node node)
  {
    _node = node;
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
  public IActionResult RequestVote([FromBody] Guid candidateId, int candidateTerm)
  {
    try
    {
      bool vote = _node.Vote(candidateId, candidateTerm);

      return Ok(vote);
    }
    catch
    {
      return StatusCode(500, "Internal server error while processing vote.");
    }
  }

  [HttpPost("sendHeartbeat")]
  public IActionResult SendHeartbeat([FromBody] int term, Guid leaderId, List<LogEntry> entries)
  {
    try
    {
      _node.ReceiveHeartBeat(term, leaderId, entries);

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
    var (value, logIndex) = _node.EventualGet(key);
    Console.WriteLine($"Key {key} got value {value} {logIndex}");
    if (value.HasValue)
    {
      return Ok(new { value = value.Value, logIndex });
    }
    return NotFound();
  }

  [HttpGet("strongGet")]
  public ActionResult<(int? value, int logIndex)> StrongGet(string key)
  {
    var (value, logIndex) = _node.StrongGet(key);
    Console.WriteLine($"Key {key} got value {value} {logIndex}");
    if (value.HasValue)
    {
      return Ok(new { value = value.Value, logIndex });
    }
    return BadRequest("Not the leader or key does not exist.");
  }

  [HttpPost("compareVersionAndSwap")]
  public ActionResult<bool> CompareVersionAndSwap([FromForm] string key, [FromForm] int expectedValue, [FromForm] int newValue, [FromForm] int expectedLogIndex)
  {
    var success = _node.CompareVersionAndSwap(key, expectedValue, newValue, expectedLogIndex);
    return Ok(success);
  }

  [HttpPost("write")]
  public ActionResult<bool> Write([FromBody] string key, int value)
  {
    var payload = new
    {
      Key = key,
      Value = value
    };

    if (payload == null)
      return BadRequest("Invalid request payload.");

    var success = _node.Write(payload.Key, payload.Value);
    if (success)
    {
      return Ok(true);
    }
    return BadRequest("Not the leader or operation failed.");
  }
}