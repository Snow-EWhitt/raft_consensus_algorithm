namespace Raft.Shared;

public class HearbeatRequest
{
  public int Term { get; set; }
  public Guid LeaderId { get; set; }
  public List<LogEntry> Entries { get; set; }
}