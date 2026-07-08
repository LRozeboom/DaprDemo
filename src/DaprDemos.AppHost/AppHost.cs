using System.Collections.Immutable;
using CommunityToolkit.Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

var redisPassword = builder.AddParameter("redis-password", "daprdemos", secret: true);
var rabbitUser = builder.AddParameter("rabbitmq-username", "guest");
var rabbitPassword = builder.AddParameter("rabbitmq-password", "guest", secret: true);

var redis = builder.AddRedis("redis", port: 6390, password: redisPassword);

var rabbitmq = builder
    .AddRabbitMQ("rabbitmq", userName: rabbitUser, password: rabbitPassword, port: 5673)
    .WithManagementPlugin(port: 15672);

var useEtags = builder.AddParameter(
    "use-etags",
    Environment.GetEnvironmentVariable("USE_ETAGS") ?? "false");

var daprResourcesPath = Path.Combine(builder.AppHostDirectory, "dapr");

DaprSidecarOptions SidecarFor(string appId) => new()
{
    AppId = appId,
    ResourcesPaths = ImmutableHashSet.Create(daprResourcesPath),
    EnableAppHealthCheck = true,
    AppHealthCheckPath = "/health",
    AppHealthProbeInterval = 1,
};

var demo01Publisher = builder.AddProject<Projects.Demo01_PubSub_Publisher>("demo01-publisher", options => options.ExcludeLaunchProfile = true)
    .WithHttpEndpoint(port: 5101)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithDaprSidecar(sidecar => sidecar
        .WithOptions(SidecarFor("demo01-publisher"))
        .WaitFor(redis)
        .WaitFor(rabbitmq))
    .WaitFor(redis)
    .WaitFor(rabbitmq);

var demo01Subscriber = builder.AddProject<Projects.Demo01_PubSub_Subscriber>("demo01-subscriber", options => options.ExcludeLaunchProfile = true)
    .WithHttpEndpoint(port: 5102)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithDaprSidecar(sidecar => sidecar
        .WithOptions(SidecarFor("demo01-subscriber"))
        .WaitFor(redis)
        .WaitFor(rabbitmq))
    .WaitFor(redis)
    .WaitFor(rabbitmq);

var demo02Subscriber = builder.AddProject<Projects.Demo02_Retries_Subscriber>("demo02-subscriber", options => options.ExcludeLaunchProfile = true)
    .WithHttpEndpoint(port: 5201)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithDaprSidecar(sidecar => sidecar
        .WithOptions(SidecarFor("demo02-subscriber"))
        .WaitFor(redis)
        .WaitFor(rabbitmq))
    .WaitFor(redis)
    .WaitFor(rabbitmq);

var demo03WorkerA = builder.AddProject<Projects.Demo03_StateStore_Worker>("demo03-worker-a", options => options.ExcludeLaunchProfile = true)
    .WithHttpEndpoint(port: 5301)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("USE_ETAGS", useEtags)
    .WithDaprSidecar(sidecar => sidecar
        .WithOptions(SidecarFor("demo03-worker-a"))
        .WaitFor(redis))
    .WaitFor(redis);

var demo03WorkerB = builder.AddProject<Projects.Demo03_StateStore_Worker>("demo03-worker-b", options => options.ExcludeLaunchProfile = true)
    .WithHttpEndpoint(port: 5302)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("USE_ETAGS", useEtags)
    .WithDaprSidecar(sidecar => sidecar
        .WithOptions(SidecarFor("demo03-worker-b"))
        .WaitFor(redis))
    .WaitFor(redis);

var demo04Api = builder.AddProject<Projects.Demo04_Bindings_Api>("demo04-api", options => options.ExcludeLaunchProfile = true)
    .WithHttpEndpoint(port: 5401)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DISCORD_WEBHOOK_URL", Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL") ?? string.Empty)
    .WithDaprSidecar(sidecar => sidecar
        .WithOptions(SidecarFor("demo04-api")));

if (Environment.GetEnvironmentVariable("DEMO_AUTOSTART") != "true")
{
    IResourceBuilder<ProjectResource>[] demos =
        [demo01Publisher, demo01Subscriber, demo02Subscriber, demo03WorkerA, demo03WorkerB, demo04Api];

    foreach (var demo in demos)
    {
        demo.WithExplicitStart();
    }
}

builder.Build().Run();
