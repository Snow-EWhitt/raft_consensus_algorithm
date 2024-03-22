namespace Raft.Shared;

public class WriteRequest
{
  public string Key { get; set; }
  public string Value { get; set; }
}