using Raft;
using Raft.Services;

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

builder.Services.AddSingleton<INodeService, NodeService>();
builder.Services.AddSingleton<ITimeProvider, RealTimeProvider>();

var nodes = Environment.GetEnvironmentVariable("NODES")?.Split(',').ToList() ?? [];
// var nodeService = app.Services.GetRequiredService<INodeService>();
// var timeProvider = app.Services.GetRequiredService<ITimeProvider>();

// var node = new Raft.Node(nodeService, nodes, timeProvider, true);

builder.Services.AddSingleton<Raft.Node>(s =>
{
  var nodeUrls = nodes;
  var nodeService = s.GetRequiredService<INodeService>();
  var timeProvider = s.GetRequiredService<ITimeProvider>();

  return new Raft.Node(nodeService, nodeUrls, timeProvider, true);
});

builder.Services.AddHostedService(s =>
{
  return s.GetRequiredService<Raft.Node>();
});

var app = builder.Build();
// app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
