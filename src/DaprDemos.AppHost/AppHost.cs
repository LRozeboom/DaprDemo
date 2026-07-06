using System.Collections.Immutable;
using CommunityToolkit.Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

// Fixed credentials and host ports: the Dapr component yaml files under dapr/ reach these
// containers at deterministic localhost addresses, so the values must match on both sides.
var redisPassword = builder.AddParameter("redis-password", "daprdemos", secret: true);
var rabbitUser = builder.AddParameter("rabbitmq-username", "guest");
var rabbitPassword = builder.AddParameter("rabbitmq-password", "guest", secret: true);

// Shared infrastructure starts eagerly so the containers are warm before the talk.
// Non-default host ports on purpose: `dapr init` already owns 6379 (dapr_redis) and other
// local projects commonly hold 5672, so the defaults collide on a Dapr-initialized machine.
var redis = builder.AddRedis("redis", port: 6390, password: redisPassword);

var rabbitmq = builder
    .AddRabbitMQ("rabbitmq", userName: rabbitUser, password: rabbitPassword, port: 5673)
    .WithManagementPlugin(port: 15672);

var useEtags = builder.AddParameter(
    "use-etags",
    Environment.GetEnvironmentVariable("USE_ETAGS") ?? "false");

var daprResourcesPath = Path.Combine(builder.AppHostDirectory, "dapr");

// App health checks are load-bearing here: sidecars start eagerly while the apps are
// explicit-start, and daprd only (re)fetches pub/sub subscriptions when the app health
// probe succeeds — i.e. after the presenter clicks Start on the app.
DaprSidecarOptions SidecarFor(string appId) => new()
{
    AppId = appId,
    ResourcesPaths = ImmutableHashSet.Create(daprResourcesPath),
    EnableAppHealthCheck = true,
    AppHealthCheckPath = "/health",
    AppHealthProbeInterval = 1,
};

// The sidecars must also wait for the brokers: daprd fails fatally when a component's
// backing service is unreachable at init, and the DCP proxy accepting early connections
// turns "not up yet" into an instant EOF instead of a retryable refusal.
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

// Two instances of the same worker project compete on one state key. Two named resources
// (instead of WithReplicas) so they can be started one at a time during the talk.
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

// DISCORD_WEBHOOK_URL reaches the sidecar through process-environment inheritance: daprd is a
// child of this AppHost, and its envvar-secrets component reads the variable the presenter
// sets before launch. Mirrored onto the app resource so it is visible in the dashboard.
var demo04Api = builder.AddProject<Projects.Demo04_Bindings_Api>("demo04-api", options => options.ExcludeLaunchProfile = true)
    .WithHttpEndpoint(port: 5401)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DISCORD_WEBHOOK_URL", Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL") ?? string.Empty)
    .WithDaprSidecar(sidecar => sidecar
        .WithOptions(SidecarFor("demo04-api")));

// Explicit start is the talk's "demo remote control": nothing runs until clicked in the
// dashboard. DEMO_AUTOSTART=true flips everything to start immediately for dry-run smoke tests.
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
