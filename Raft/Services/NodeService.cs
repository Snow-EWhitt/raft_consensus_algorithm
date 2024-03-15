using System.Text;
using System.Text.Json;

namespace Raft.Services;

public class NodeService : INodeService
{
  HttpClient _httpClient;

  public NodeService()
  {
    _httpClient = new HttpClient();
  }

  public async Task<bool> RequestVoteAsync(string nodeURL, Guid candidateId, int candidateTerm)
  {
    var payload = new
    {
      Id = candidateId,
      Term = candidateTerm
    };

    var json = JsonSerializer.Serialize(payload);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    try
    {
      var response = await _httpClient.PostAsync($"{nodeURL}/Node/vote", content);
      
      if (response.IsSuccessStatusCode)
      {
        var responseString = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
          PropertyNameCaseInsensitive = true
        };
        var voteResponse = JsonSerializer.Deserialize<bool>(responseString, options);
        
        return voteResponse;
      }
    }
    catch
    {

    }

    return false;
  }

  public async Task<int> GetLastLogIndexAsync(string nodeURL)
  {
    try
    {
      var response = await _httpClient.GetAsync($"{nodeURL}/Node/getLastLogIndex");

      if (response.IsSuccessStatusCode)
      {
        var responseString = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
          PropertyNameCaseInsensitive = true
        };
        var lastLogIndex = JsonSerializer.Deserialize<int>(responseString, options);

        return lastLogIndex;
      }
    }
    catch
    {

    }

    return -1;
  }

  public async Task SendHeartbeatAsync(string nodeURL, int term, Guid leaderId, List<LogEntry> entries)
  {
    var payload = new
    {
      Id = leaderId,
      Term = term,
      Entries = entries
    };

    var json = JsonSerializer.Serialize(payload);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    try
    {
      var response = await _httpClient.PostAsync($"{nodeURL}/Node/sendHeartbeat", content);
    }
    catch
    {

    }
  }
}