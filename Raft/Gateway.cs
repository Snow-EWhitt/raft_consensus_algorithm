using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Raft.Shared;

namespace Raft;

public class Gateway
{
  HttpClient _httpClient;
  // private readonly ILogger _logger;
  Random rng = new();
  List<string> nodeList;

  public Gateway(List<string> nodes)
  {
    nodeList = nodes;
    _httpClient = new HttpClient();
    // _logger = logger;
  }

  async Task<string?> GetLeaderNode()
  {
    foreach (var nodeURL in nodeList)
    {
      Console.WriteLine("Calling " + nodeURL + "/Node/getLeader");
      try
      {
        var response = await _httpClient.GetAsync($"http://{nodeURL}/Node/getLeader");

        if (response.IsSuccessStatusCode)
        {
          var content = await response.Content.ReadAsStringAsync();
          var isLeader = bool.Parse(content);

          if (isLeader)
          {
            return nodeURL;
          }
        }
      }
      catch
      {

      }
    }

    return null;
  }

  public async Task<(int? value, int logIndex)?> EventualGet(string key)
  {
    var nodeURL = nodeList[rng.Next(nodeList.Count)];
    try
    {
      var response = await _httpClient.GetAsync($"http://{nodeURL}/Node/eventualGet?key={key}");

      if (response.IsSuccessStatusCode)
      {
        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions
        {
          PropertyNameCaseInsensitive = true
        };
        var getResult = JsonSerializer.Deserialize<(int Value, int LogIndex)>(content, options);

        return (getResult.Value, getResult.LogIndex);
      }
    }
    catch
    {

    }
    return null;
  }

  public async Task<Data?> StrongGet(string key)
  {
    // Console.WriteLine("Gateway function called");

    var leaderURL = await GetLeaderNode();

    Console.WriteLine(leaderURL);

    if (leaderURL != null)
    {
      try
      {
        // Console.WriteLine("Calling leader node");

        var response = await _httpClient.GetAsync($"http://{leaderURL}/Node/strongGet?key={key}");

        // Console.WriteLine($"Leader node response: {response}");

        if (response.IsSuccessStatusCode)
        {
          var content = await response.Content.ReadAsStringAsync();
          var options = new JsonSerializerOptions
          {
            PropertyNameCaseInsensitive = true
          };
          var getResult = JsonSerializer.Deserialize<Data>(content, options);

          Console.WriteLine(getResult?.Value);

          if (getResult != null)
            return getResult;
        }
      }
      catch
      {
        Console.WriteLine("A problem occurred in the gateway function.");
      }
    }

    return null;
  }

  public async Task<bool> CompareVersionAndSwap(string key, string expectedValue, string newValue)
  {
    var leaderURL = await GetLeaderNode();

    if (leaderURL != null)
    {
      try
      {
        var content = new FormUrlEncodedContent(
        [
          new KeyValuePair<string, string>("key", key),
          new KeyValuePair<string, string>("expectedValue", expectedValue.ToString()),
          new KeyValuePair<string, string>("newValue", newValue.ToString())
        ]);

        Console.WriteLine($"Content: {content}");

        var response = await _httpClient.PostAsync($"http://{leaderURL}/Node/compareVersionAndSwap", content);

        Console.WriteLine($"Reponse: {response}");

        return response.IsSuccessStatusCode;
      }
      catch
      {
        Console.WriteLine("An error has occurred while doing a compare version and swap operation");
      }
    }

    return false;
  }

  public async Task<bool> Write(string key, int value)
  {
    var leaderURL = await GetLeaderNode();

    if (leaderURL != null)
    {
      try
      {
        var payload = new { Key = key, Value = value };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"http://{leaderURL}/Node/write", content);

        return response.IsSuccessStatusCode;
      }
      catch
      {

      }
    }

    return false;
  }
}