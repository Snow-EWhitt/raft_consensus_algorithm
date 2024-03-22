using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Raft.Web;
using Raft.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddLogging();

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var baseAddress = "http://localhost:5000";

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseAddress) });
builder.Services.AddScoped<GatewayService>();

await builder.Build().RunAsync();
