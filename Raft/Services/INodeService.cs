using Raft.Shared;

namespace Raft.Services;

public interface INodeService
{
  public Task<bool> RequestVoteAsync(string nodeURL, VoteRequest request);
  public Task<int> GetLastLogIndexAsync(string nodeURL);
  public Task SendHeartbeatAsync(string nodeURL, HearbeatRequest heartbeat);
}