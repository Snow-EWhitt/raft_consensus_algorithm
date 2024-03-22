using Microsoft.Extensions.Hosting;
using Raft.Services;
using Raft.Shared;

namespace Raft;

public enum NodeState
{
  Follower,
  Candidate,
  Leader
}

public class Node : BackgroundService
{
  public Guid Id { get; private set; }
  public NodeState State { get; set; }
  int CurrentTerm { get; set; }

  object _logLock = new();
  Random _rng = new();
  INodeService _service;
  ITimeProvider _timeProvider;
  string _logFileName;
  DateTime _lastHeartbeatReceived;
  Guid _votedFor;
  Guid _recentLeader;
  int _lastLogIndex;
  int _electionTimeout;
  bool _isHealthy;

  List<string> _nodeList = [];
  public List<LogEntry> LogEntries { get; private set; } = [];
  public Dictionary<string, Data> LogData = [];

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

  public async Task Initialize()
  {
    while (_isHealthy)
    {
      await Act();
    }
  }

  public async Task Act()
  {
    if (_isHealthy)
    {
      if (State == NodeState.Leader)
      {
        await SendHeartbeat();
      }
      else if (ElectionTimedOut())
      {
        LogEntry("Timer timed out.");

        if (State == NodeState.Follower)
        {
          State = NodeState.Candidate;
          CurrentTerm++;

          SetElectionTimeout();
          await StartElection();
        }
        else if (State == NodeState.Candidate)
        {
          CurrentTerm++;

          await StartElection();
        }
      }
    }
  }

  public bool IsLeader()
  {
    return State == NodeState.Leader;
  }

  async Task StartElection()
  {
    LogEntry("Starting election.");

    List<bool> votes = [];
    _votedFor = Id;
    int numberOfVotes = 1; // Node votes for itself

    VoteRequest payload = new()
    {
      Id = Id,
      Term = CurrentTerm
    };

    foreach (string nodeURL in _nodeList)
    {
      votes.Add(await _service.RequestVoteAsync(nodeURL, payload));
    }

    foreach (bool isVotedFor in votes)
    {
      if (isVotedFor)
        numberOfVotes++;
    }

    CalculateElectionResults(numberOfVotes);
  }

  private async void CalculateElectionResults(int numberOfVotes)
  {
    if (NodeHasMajorityVote(numberOfVotes))
    {
      State = NodeState.Leader;

      LogEntry($"Node {Id} won the election.");
      await SendHeartbeat();
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

  async Task SendHeartbeat()
  {
    foreach (string nodeURL in _nodeList)
    {
      int lastLogIndex = await _service.GetLastLogIndexAsync(nodeURL);
      List<LogEntry> entries = LogEntries.Where(e => e.LogIndex > lastLogIndex).ToList();

      HearbeatRequest payload = new()
      {
        LeaderId = Id,
        Term = CurrentTerm,
        Entries = entries
      };

      await _service.SendHeartbeatAsync(nodeURL, payload);
    }

    LogEntry("Hearbeat sent to all nodes.");
  }

  public void ReceiveHeartBeat(HearbeatRequest heartbeat)
  {
    if (heartbeat.Term >= CurrentTerm)
    {
      CurrentTerm = heartbeat.Term;
      _recentLeader = heartbeat.LeaderId;
      State = NodeState.Follower;

      foreach (LogEntry entry in heartbeat.Entries)
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
    LogData[entry.Key] = new Data { Value = entry.Value, LogIndex = entry.LogIndex };
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

    // Console.WriteLine($"{DateTime.Now.TimeOfDay}: {message}");
  }

  public Data EventualGet(string key)
  {
    if (LogData.TryGetValue(key, out var data))
    {
      return new Data { Value = data.Value, LogIndex = data.LogIndex };
    }

    return new Data { Value = "None", LogIndex = -1 };
  }

  public Data StrongGet(string key)
  {
    if (!IsLeader()) return new Data { Value = "None", LogIndex = -1 };

    if (LogData.TryGetValue(key, out var data))
      return new Data { Value = data.Value, LogIndex = data.LogIndex };

    return new Data { Value = "None", LogIndex = -1 };
  }

  public bool CompareVersionAndSwap(string key, string expectedValue, string newValue)
  {
    if (!IsLeader())
      return false;

    if (LogData.TryGetValue(key, out var data))
    {
      if (data.Value == expectedValue)
        return Write(key, newValue);

      return false;
    }
    else
    {
      return Write(key, newValue);
    }
  }

  public bool Write(string key, string value)
  {
    if (!IsLeader()) return false;

    _lastLogIndex++;

    LogEntry newEntry = new()
    {
      Key = key,
      Value = value,
      LogIndex = _lastLogIndex
    };

    LogEntries.Add(newEntry);

    if (LogData.ContainsKey(key))
    {
      LogData[key] = new Data { Value = value, LogIndex = LogData[key].LogIndex + 1 };
    }
    else
    {
      LogData.Add(key, new Data { Value = value, LogIndex = _lastLogIndex });
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

  protected override async Task ExecuteAsync(CancellationToken token)
  {
    await Initialize();
  }
}