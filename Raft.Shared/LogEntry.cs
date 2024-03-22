namespace Raft;

public class LogEntry
{
  public required string Key { get; set; }
  public string Value { get; set; }
  public int LogIndex { get; set; }
}