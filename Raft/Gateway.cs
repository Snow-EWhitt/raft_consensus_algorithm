namespace Raft;

public class Gateway
{
  readonly Random rng = new();
  readonly List<Node> nodeList;

  public Gateway(List<Node> nodes)
  {
    nodeList = nodes;
  }

  Node? GetLeaderNode()
  {
    return nodeList.First(n => n.State == NodeState.Leader);
  }

  public int? EventualGet(string key)
  {
    Node randomNode = nodeList[rng.Next(nodeList.Count)];

    if (randomNode.LogData.TryGetValue(key, out var _))
      return randomNode.LogData[key].value;

    return null;
  }

  public int? StrongGet(string key)
  {
    Node? leader = GetLeaderNode();

    if (leader is not null && leader.LogData.TryGetValue(key, out var _))
      return leader.LogData[key].value;

    return null;
  }

  public bool CompareVersionAndSwap(string key, int expectedValue, int newValue)
  {
    Node? leader = GetLeaderNode();

    if (leader != null &&
        leader.LogData.TryGetValue(key, out var value) &&
        value.value == expectedValue)
    {
      int newLogIndex = leader.LogEntries.Max(e => e.LogIndex) + 1;

      leader.AppendEntry(new LogEntry { Key = key, Value = newValue, LogIndex = newLogIndex });

      return true;
    }

    return false;
  }

  public bool Write(string key, int value)
  {
    Node? leader = GetLeaderNode();

    if (leader is not null)
    {
      int newLogIndex = leader.LogEntries.Count != 0 ?
        leader.LogEntries.Max(e => e.LogIndex) + 1 : 0;
      LogEntry newLog = new()
      {
        Key = key,
        Value = value,
        LogIndex = newLogIndex
      };

      leader.AppendEntry(newLog);

      return true;
    }

    return false;
  }
}