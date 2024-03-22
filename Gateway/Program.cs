using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowAll", policy =>
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader());
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddLogging();

var nodes = Environment.GetEnvironmentVariable("NODES")?.Split(',')?.ToList() ?? [];

builder.Services.AddSingleton(s =>
{
  // ILogger logger = s.GetRequiredService<ILogger<Raft.Gateway>>();
  
  return new Raft.Gateway(nodes);
});

builder.Logging.AddOpenTelemetry(options =>
{
  options.AddOtlpExporter(options =>
  {
    options.Endpoint = new Uri("http://otel-collector:4317");
  }).SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("RaftGatewayService"));
});

var app = builder.Build();
app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();
app.Run();
