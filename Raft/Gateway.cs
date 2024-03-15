using System.Text;
using System.Text.Json;

namespace Raft;

public class Gateway
{
  HttpClient _httpClient;
  Random rng = new();
  List<string> nodeList;

  public Gateway(List<string> nodes)
  {
    nodeList = nodes;
    _httpClient = new HttpClient();
  }

  async Task<string?> GetLeaderNode()
  {
    foreach (var nodeURL in nodeList)
    {
      try
      {
        var response = await _httpClient.GetAsync($"{nodeURL}/Node/getLeader");

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
      var response = await _httpClient.GetAsync($"{nodeURL}/Node/eventualGet?key={key}");

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

  public async Task<(int? value, int logIndex)?> StrongGet(string key)
  {
    var leaderURL = await GetLeaderNode();

    if (leaderURL != null)
    {
      try
      {
        var response = await _httpClient.GetAsync($"{leaderURL}/Node/strongGet?key={key}");

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
    }

    return null;
  }

  public async Task<bool> CompareVersionAndSwap(string key, int expectedValue, int newValue)
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

        var response = await _httpClient.PostAsync($"{leaderURL}/Node/compareVersionAndSwap", content);

        return response.IsSuccessStatusCode;
      }
      catch
      {

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

        var response = await _httpClient.PostAsync($"{leaderURL}/Node/write", content);

        return response.IsSuccessStatusCode;
      }
      catch
      {

      }
    }

    return false;
  }
}