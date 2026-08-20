using System.Collections.Immutable;
using CommunityToolkit.Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

var redisPassword = builder.AddParameter("redis-password", "daprdemos", secret: true);
var rabbitUser = builder.AddParameter("rabbitmq-username", "guest");
var rabbitPassword = builder.AddParameter("rabbitmq-password", "guest", secret: true);

var postgresPassword = builder.AddParameter("postgres-password", "daprdemos", secret: true);

var redis = builder.AddRedis("redis", port: 6390, password: redisPassword);

var postgres = builder.AddPostgres("postgres", password: postgresPassword, port: 5433)
    .WithDataVolume("daprdemos-postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin();

var rabbitmq = builder
    .AddRabbitMQ("rabbitmq", userName: rabbitUser, password: rabbitPassword, port: 5673)
    .WithManagementPlugin(port: 15672);

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

var demo03Worker = builder.AddProject<Projects.Demo03_StateStore_Worker>("demo03-worker", options => options.ExcludeLaunchProfile = true)
    .WithHttpEndpoint(port: 5301)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithDaprSidecar(sidecar => sidecar
        .WithOptions(SidecarFor("demo03-worker"))
        .WaitFor(redis))
    .WaitFor(redis);

// Demo 04 both publishes (through the outbox) and subscribes, so its sidecar needs Postgres for
// the state store and Redis for the pub/sub component.
var demo04Outbox = builder.AddProject<Projects.Demo04_Outbox_Worker>("demo04-outbox", options => options.ExcludeLaunchProfile = true)
    .WithHttpEndpoint(port: 5401)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithDaprSidecar(sidecar => sidecar
        .WithOptions(SidecarFor("demo04-outbox"))
        .WaitFor(redis)
        .WaitFor(postgres))
    .WaitFor(redis)
    .WaitFor(postgres);

IResourceBuilder<ProjectResource>[] demos =
    [demo01Publisher, demo01Subscriber, demo02Subscriber, demo03Worker, demo04Outbox];

foreach (var demo in demos)
{
    demo.WithEnvironment("ASPNETCORE_HOSTINGSTARTUPASSEMBLIES", string.Empty);

    demo.WithUrlForEndpoint("http", url =>
    {
        url.Url = "/scalar";
        url.DisplayText = "Scalar";
    });
}

builder.Build().Run();
