using Demo01.PubSub.Subscriber;

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
