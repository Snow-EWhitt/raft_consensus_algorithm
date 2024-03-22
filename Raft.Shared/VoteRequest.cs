namespace Raft.Shared;

public class VoteRequest
{
  public Guid Id { get; set; }
  public int Term { get; set; }
}