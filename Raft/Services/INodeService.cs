namespace Raft.Services;

public interface INodeService
{
  public Task<bool> RequestVoteAsync(string nodeURL, Guid candidateId, int candidateTerm);
  public Task<int> GetLastLogIndexAsync(string nodeURL);
  public Task SendHeartbeatAsync(string nodeURL, int term, Guid leaderId, List<LogEntry> entries);
}