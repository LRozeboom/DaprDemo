using Demo02.Retries.Subscriber;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddApplication();
builder.Services.AddControllers().AddDapr();

var app = builder.Build();

app.MapDefaultEndpoints();

// HTTPS redirection is deliberately absent: it breaks Dapr sidecar communication.

app.UseCloudEvents();
app.MapSubscribeHandler();
app.MapControllers();

app.Run();
