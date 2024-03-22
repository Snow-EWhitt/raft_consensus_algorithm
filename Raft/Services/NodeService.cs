using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Raft.Shared;

namespace Raft.Services;

public class NodeService : INodeService
{
  private HttpClient _httpClient;
  private readonly ILogger _logger;

  public NodeService(ILogger<NodeService> logger)
  {
    _httpClient = new HttpClient();
    _logger = logger;
  }

  public async Task<bool> RequestVoteAsync(string nodeURL, VoteRequest request)
  {
    _logger.LogInformation($"Requesting vote from {nodeURL}.");

    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    try
    {
      var response = await _httpClient.PostAsync($"http://{nodeURL}/Node/requestVote", content);
      
      if (response.IsSuccessStatusCode)
      {
        var responseString = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
          PropertyNameCaseInsensitive = true
        };
        var voteResponse = JsonSerializer.Deserialize<bool>(responseString, options);

        // _logger.LogInformation($"Received vote from {nodeURL}.");
        
        return voteResponse;
      }
      else
      {
        _logger.LogInformation($"An error occurred while requesting vote from {nodeURL}.");
      }
    }
    catch (Exception e)
    {
      _logger.LogError(e.ToString());
    }

    _logger.LogInformation($"Did not receive vote from {nodeURL}.");

    return false;
  }

  public async Task<int> GetLastLogIndexAsync(string nodeURL)
  {
    // _logger.LogInformation($"Getting latest log index from {nodeURL}.");

    try
    {
      var response = await _httpClient.GetAsync($"http://{nodeURL}/Node/getLastLogIndex");

      if (response.IsSuccessStatusCode)
      {
        var responseString = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
          PropertyNameCaseInsensitive = true
        };
        var lastLogIndex = JsonSerializer.Deserialize<int>(responseString, options);

        // _logger.LogInformation($"Received latest log index from {nodeURL}.");

        return lastLogIndex;
      }
    }
    catch (Exception e)
    {
      _logger.LogError(e.ToString());
    }

    return -1;
  }

  public async Task SendHeartbeatAsync(string nodeURL, HearbeatRequest heartbeat)
  {
    // _logger.LogInformation($"Sending hearbeat to {nodeURL}.");

    var json = JsonSerializer.Serialize(heartbeat);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    try
    {
      var response = await _httpClient.PostAsync($"http://{nodeURL}/Node/sendHeartbeat", content);

      // _logger.LogInformation("Hearbeat successful.");
    }
    catch (Exception e)
    {
      _logger.LogError(e.ToString());
    }
  }
}