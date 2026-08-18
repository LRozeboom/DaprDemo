using Demo04.Bindings.Application;
using Demo04.Bindings.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddControllers();

var app = builder.Build();

app.MapDefaultEndpoints();

// HTTPS redirection is deliberately absent: it breaks Dapr sidecar communication.

app.MapControllers();

app.Run();
