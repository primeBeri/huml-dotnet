# Testcontainers Integration

Real PostgreSQL for integration tests using Docker containers.

## Basic Setup

```csharp
public class IntegrationTestBase : IAsyncLifetime
{
    protected PostgreSqlContainer Postgres { get; } = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("spacehub_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    protected SpaceHubDbContext Context { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Postgres.StartAsync();

        var options = new DbContextOptionsBuilder<SpaceHubDbContext>()
            .UseNpgsql(Postgres.GetConnectionString())
            .Options;

        Context = new SpaceHubDbContext(options);
        await Context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
        await Postgres.DisposeAsync();
    }
}
```

## WebApplicationFactory Integration

```csharp
public class SpaceHubApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<SpaceHubDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add test database
            services.AddDbContext<SpaceHubDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));
        });
    }
}

public class RoomEndpointsTests : IClassFixture<SpaceHubApiFactory>
{
    private readonly HttpClient _client;

    public RoomEndpointsTests(SpaceHubApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRooms_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/v1/projects/{TestData.ProjectId}/rooms");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

## Shared Fixture (Single Container for Test Class)

```csharp
public class DatabaseFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; } = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync() => await Postgres.StartAsync();
    public async Task DisposeAsync() => await Postgres.DisposeAsync();
}

[Collection("Database")]
public class RoomTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public RoomTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Test_WithSharedDatabase()
    {
        // All tests in this class share the same container
    }
}
```
