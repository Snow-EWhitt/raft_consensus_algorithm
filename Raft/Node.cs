using Raft.Services;

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

  readonly object _logLock = new();
  readonly Random _rng = new();
  INodeService _service;
  readonly ITimeProvider _timeProvider;
  readonly string _logFileName;
  DateTime _lastHeartbeatReceived;
  Guid _votedFor;
  Guid _recentLeader;
  int _lastLogIndex;
  int _electionTimeout;
  bool _isHealthy;

  readonly List<string> _nodeList = [];
  public List<LogEntry> LogEntries { get; private set; } = [];
  public Dictionary<string, (int value, int logIndex)> LogData = [];

  public Node(INodeService service, List<string> nodeUrls, ITimeProvider timeProvider, bool isHealthy)
  {
    Id = Guid.NewGuid();
    State = NodeState.Follower;
    CurrentTerm = 0;
    _nodeList = nodeUrls;
    _lastLogIndex = 0;
    _logFileName = $"{Id}.log";
    _isHealthy = isHealthy;
    _service = service;
    _timeProvider = timeProvider;

    LogEntry($"Node {Id} created");
    LogEntry($"Node {Id} is {(isHealthy ? "healthy" : "not healthy")}.");
    SetElectionTimeout();
  }

  public void Initialize()
  {
    while (_isHealthy)
    {
      Act();
    }
  }

  public void Act()
  {
    if (_isHealthy)
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

  public bool IsLeader()
  {
    return State == NodeState.Leader;
  }

  async void StartElection()
  {
    LogEntry("Starting election.");

    List<bool> votes = [];
    _votedFor = Id;
    int numberOfVotes = 1; // Node votes for itself

    foreach (string nodeURL in _nodeList)
    {
      votes.Add(await _service.RequestVoteAsync(nodeURL, Id, CurrentTerm));
    }

    foreach (bool isVotedFor in votes)
    {
      if (isVotedFor)
        numberOfVotes++;
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

  public bool Vote(Guid candidateId, int candidateTerm)
  {
    if (!_isHealthy)
      return false;
    else if (candidateTerm > CurrentTerm || (candidateTerm == CurrentTerm && (_votedFor == Guid.Empty || _votedFor == candidateId)))
    {
      CurrentTerm = candidateTerm;
      _votedFor = candidateId;
      State = NodeState.Follower;

      SetElectionTimeout();
      LogEntry($"Voted for Node {candidateId} on term {candidateTerm}.");

      return true;
    }

    LogEntry($"Denied vote for Node {candidateId} on term {candidateTerm}.");

    return false;
  }

  async void SendHeartbeat()
  {
    foreach (string nodeURL in _nodeList)
    {
      int lastLogIndex = await _service.GetLastLogIndexAsync(nodeURL);
      List<LogEntry> entries = LogEntries.Where(e => e.LogIndex > lastLogIndex).ToList();
      await _service.SendHeartbeatAsync(nodeURL, CurrentTerm, Id, entries);
    }

    LogEntry("Hearbeat sent to all nodes.");
  }

  public void ReceiveHeartBeat(
    int term,
    Guid leaderId,
    List<LogEntry> entries)
  {
    if (term >= CurrentTerm)
    {
      CurrentTerm = term;
      _recentLeader = leaderId;
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
    _lastLogIndex = Math.Max(_lastLogIndex + 1, entry.LogIndex);
    LogEntries.Add(entry);
    LogData[entry.Key] = (entry.Value, entry.LogIndex);
  }

  void SetElectionTimeout()
  {
    _electionTimeout = _rng.Next(150, 300);
    _lastHeartbeatReceived = DateTime.UtcNow;

    LogEntry($"Timer set for {_electionTimeout}ms.");
  }

  bool ElectionTimedOut()
  {
    return _timeProvider.UtcNow - _lastHeartbeatReceived > TimeSpan.FromMilliseconds(_electionTimeout);
  }

  bool NodeHasMajorityVote(int numberOfVotes)
  {
    return numberOfVotes > _nodeList.Count / 2;
  }

  void LogEntry(string message)
  {
    lock (_logLock)
    {
      File.AppendAllText(_logFileName, $"{DateTime.Now.TimeOfDay}: {message}\n");
    }
  }

  public (int? value, int logIndex) EventualGet(string key)
  {
    if (LogData.TryGetValue(key, out var data))
    {
      return (data.value, data.logIndex);
    }
    return (null, 0);
  }

  public (int? value, int logIndex) StrongGet(string key)
  {
    if (!IsLeader()) return (null, 0);

    if (LogData.TryGetValue(key, out var data))
    {
      return (data.value, data.logIndex);
    }
    return (null, 0);
  }

  public bool CompareVersionAndSwap(string key, int expectedValue, int newValue, int expectedLogIndex)
  {
    if (!IsLeader())
      return false;

    if (LogData.TryGetValue(key, out var data) && data.value == expectedValue && data.logIndex == expectedLogIndex)
    {
      _lastLogIndex++;

      var newEntry = new LogEntry
      {
        Key = key,
        Value = newValue,
        LogIndex = _lastLogIndex
      };

      LogEntries.Add(newEntry);
      LogData[key] = (newValue, ++_lastLogIndex);
      return true;
    }
    return false;
  }

  public bool Write(string key, int value)
  {
    if (!IsLeader()) return false;

    _lastLogIndex++;

    var newEntry = new LogEntry
    {
      Key = key,
      Value = value,
      LogIndex = _lastLogIndex
    };

    LogEntries.Add(newEntry);

    if (LogData.ContainsKey(key))
    {
      LogData[key] = (value, LogData[key].logIndex + 1);
    }
    else
    {
      LogData.Add(key, (value, ++_lastLogIndex));
    }
    return true;
  }

  public void Restart()
  {
    Stop();
    Resume();
  }

  public void Stop()
  {
    _isHealthy = false;
  }

  public void Resume()
  {
    State = NodeState.Follower;
    _isHealthy = true;

    SetElectionTimeout();
  }
}