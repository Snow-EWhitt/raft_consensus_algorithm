namespace Raft;

public class LogEntry
{
  public required string Key { get; set; }
  public int Value { get; set; }
  public int LogIndex { get; set; }
}