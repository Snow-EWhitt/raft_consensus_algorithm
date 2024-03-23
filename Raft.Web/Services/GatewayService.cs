using System.Net.Http.Json;
using Raft.Shared;

namespace Raft.Web.Services;

public class GatewayService
{
  private readonly HttpClient _httpClient;
  private readonly ILogger<GatewayService> _logger;

  public GatewayService(HttpClient httpClient, ILogger<GatewayService> logger)
  {
    _httpClient = httpClient;
    _logger = logger;
  }

  public async Task<Data> StrongGet(string key)
  {
    _logger.LogInformation($"Getting data for key: {key}.");

    var result = await _httpClient.GetFromJsonAsync<Data>($"/Gateway/StrongGet?key={key}");

    if (result?.Value == "")
      result.Value = "None";

    if (result != null)
      return result;
    else
      return new Data() { Value = "None", LogIndex = -1 };
  }

  public async Task UpdateData(string key, string expectedValue, string newValue)
  {
    _logger.LogInformation($"Updating data for key: {key}.");

    await _httpClient.PostAsync("/Gateway/CompareVersionAndSwap" +
      $"?key={key}" +
      $"&expectedValue={expectedValue}" +
      $"&newValue={newValue}", null);

    _logger.LogInformation($"Key: {key}, ExpectedValue: {expectedValue} Value: {newValue}.");
  }
}