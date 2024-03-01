namespace Raft;

public enum NodeState
{
  Follower,
  Candidate,
  Leader
}

public class Node
{
  public Guid Id { get; private set; }
  public NodeState State { get; set; }
  int CurrentTerm { get; set; }

  private readonly object logLock = new();
  readonly Random rng = new();
  readonly List<Node> NodeList = [];
  ITimeProvider timeProvider;
  readonly string LogFileName;
  DateTime lastHeartbeatReceived;
  Guid votedFor;
  Guid recentLeader;
  int electionTimeout;
  bool isHealthy;

  public List<LogEntry> LogEntries { get; private set; } = [];
  public Dictionary<string, (int value, int logIndex)> LogData = [];

  public Node(List<Node> allNodes, ITimeProvider timeProvider, bool isHealthy)
  {
    Id = Guid.NewGuid();
    State = NodeState.Follower;
    CurrentTerm = 0;
    NodeList = allNodes;
    LogFileName = $"{Id}.log";
    this.isHealthy = isHealthy;
    this.timeProvider = timeProvider;

    LogEntry($"Node {Id} created");
    LogEntry($"Node {Id} is {(isHealthy ? "healthy" : "not healthy")}.");
    SetElectionTimeout();
  }

  public void Initialize()
  {
    while (isHealthy)
    {
      Act();
    }
  }

  public void Act()
  {
    if (isHealthy)
    {
      if (State == NodeState.Leader)
      {
        SendHeartbeat();
      }
      else if (ElectionTimedOut())
      {
        LogEntry("Timer timed out.");

        if (State == NodeState.Follower)
        {
          State = NodeState.Candidate;
          CurrentTerm++;

          SetElectionTimeout();
          StartElection();
        }
        else if (State == NodeState.Candidate)
        {
          CurrentTerm++;

          StartElection();
        }
      }
    }
  }

  void StartElection()
  {
    LogEntry("Starting election.");

    votedFor = Id;
    int numberOfVotes = 1; // Node votes for itself

    foreach (Node node in NodeList)
    {
      if (node.Id == Id)
        continue;

      numberOfVotes += RequestVote(node);
    }

    CalculateElectionResults(numberOfVotes);
  }

  private void CalculateElectionResults(int numberOfVotes)
  {
    if (NodeHasMajorityVote(numberOfVotes))
    {
      State = NodeState.Leader;

      LogEntry($"Node {Id} won the election.");
      SendHeartbeat();
    }
    else
    {
      State = NodeState.Follower;

      LogEntry($"Node {Id} lost the election.");
    }
  }

  int RequestVote(Node node)
  {
    if (node.Vote(Id, CurrentTerm))
    {
      LogEntry($"Received vote from Node {node.Id}.");

      return 1;
    }
    else
    {
      LogEntry($"Did not receive vote from Node {node.Id}.");

      return 0;
    }
  }

  bool Vote(Guid candidateId, int candidateTerm)
  {
    if (!isHealthy)
      return false;
    else if (candidateTerm > CurrentTerm || (candidateTerm == CurrentTerm && (votedFor == Guid.Empty || votedFor == candidateId)))
    {
      CurrentTerm = candidateTerm;
      votedFor = candidateId;
      State = NodeState.Follower;

      SetElectionTimeout();
      LogEntry($"Voted for Node {candidateId} on term {candidateTerm}.");

      return true;
    }
    else
    {
      LogEntry($"Denied vote for Node {candidateId} on term {candidateTerm}.");

      return false;
    }
  }

  public void SendHeartbeat()
  {
    foreach (Node node in NodeList)
    {
      if (node.Id == Id)
        continue;

      List<LogEntry> entries = LogEntries.Where(e => !node.LogEntries.Select(le => le.LogIndex).Contains(e.LogIndex)).ToList();

      node.ReceiveHeartBeat(CurrentTerm, Id, entries);
    }

    LogEntry("Hearbeat sent to all nodes.");
  }

  void ReceiveHeartBeat(
    int term,
    Guid leaderId,
    List<LogEntry> entries)
  {
    if (term >= CurrentTerm)
    {
      CurrentTerm = term;
      recentLeader = leaderId;
      State = NodeState.Follower;

      foreach (LogEntry entry in entries)
      {
        AppendEntry(entry);
      }

      SetElectionTimeout();
      LogEntry("Received heartbeat.");
    }
  }

  public void AppendEntry(LogEntry entry)
  {
    LogEntries.Add(entry);
    LogData[entry.Key] = (entry.Value, entry.LogIndex);
  }

  void SetElectionTimeout()
  {
    electionTimeout = rng.Next(150, 300);
    lastHeartbeatReceived = DateTime.UtcNow;

    LogEntry($"Timer set for {electionTimeout}ms.");
  }

  private bool ElectionTimedOut()
  {
    return timeProvider.UtcNow - lastHeartbeatReceived > TimeSpan.FromMilliseconds(electionTimeout);
  }

  bool NodeHasMajorityVote(int numberOfVotes)
  {
    return numberOfVotes > NodeList.Count / 2;
  }

  void LogEntry(string message)
  {
    lock (logLock)
    {
      File.AppendAllText(LogFileName, $"{DateTime.Now.TimeOfDay}: {message}\n");
    }
  }

  public void Restart()
  {
    Stop();
    Resume();
  }

  public void Stop()
  {
    isHealthy = false;
  }

  public void Resume()
  {
    State = NodeState.Follower;
    isHealthy = true;

    SetElectionTimeout();
  }
}