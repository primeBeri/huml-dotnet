# Test Data Builders

Fluent builders for creating test data.

## TestDataBuilder Pattern

```csharp
public class TestDataBuilder
{
    private readonly SpaceHubDbContext _context;

    public TestDataBuilder(SpaceHubDbContext context)
    {
        _context = context;
    }

    // Fluent methods for adding test data
    public TestDataBuilder WithOrganisation(Organisation organisation)
    {
        _context.Organisations.Add(organisation);
        _context.SaveChanges();
        return this;
    }

    public TestDataBuilder WithProject(Project project)
    {
        _context.Projects.Add(project);
        _context.SaveChanges();
        return this;
    }

    public TestDataBuilder WithRoom(Room room)
    {
        _context.Rooms.Add(room);
        _context.SaveChanges();
        return this;
    }

    public TestDataBuilder WithRooms(params Room[] rooms)
    {
        _context.Rooms.AddRange(rooms);
        _context.SaveChanges();
        return this;
    }

    // Query methods
    public T? Get<T>(Guid id) where T : class
        => _context.Set<T>().Find(id);

    public IReadOnlyList<T> GetAll<T>() where T : class
        => _context.Set<T>().ToList();

    public int Count<T>() where T : class
        => _context.Set<T>().Count();
}
```

## Room Builder

```csharp
public class RoomBuilder
{
    private readonly Guid _projectId;
    private string _number = "R001";
    private string _name = "Test Room";
    private string? _description;
    private decimal? _areaRequired;
    private string? _notes;

    public RoomBuilder(Guid projectId)
    {
        _projectId = projectId;
    }

    public RoomBuilder WithNumber(string number)
    {
        _number = number;
        return this;
    }

    public RoomBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public RoomBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public RoomBuilder WithArea(decimal area)
    {
        _areaRequired = area;
        return this;
    }

    public RoomBuilder WithNotes(string notes)
    {
        _notes = notes;
        return this;
    }

    public Room Build()
    {
        var room = new Room(_number, _name, _projectId);

        if (_description is not null)
            room.Description = _description;

        if (_areaRequired.HasValue)
            room.SetArea(_areaRequired.Value);

        if (_notes is not null)
            room.Notes = _notes;

        return room;
    }
}

// Usage
var room = new RoomBuilder(projectId)
    .WithNumber("R001")
    .WithName("Meeting Room")
    .WithArea(50.5m)
    .Build();
```

## Project Builder

```csharp
public class ProjectBuilder
{
    private readonly Guid _organisationId;
    private string _code = "PRJ001";
    private string _name = "Test Project";
    private string? _description;

    public ProjectBuilder(Guid organisationId)
    {
        _organisationId = organisationId;
    }

    public ProjectBuilder WithCode(string code)
    {
        _code = code;
        return this;
    }

    public ProjectBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ProjectBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public Project Build()
    {
        var project = new Project(_code, _name, _organisationId);

        if (_description is not null)
            project.Description = _description;

        return project;
    }
}
```

## Usage in Tests

```csharp
public class UpdateRoomHandlerTests : IntegrationTestBase
{
    private TestDataBuilder _builder = null!;
    private UpdateRoomHandler _handler = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _builder = new TestDataBuilder(Context);
        _handler = new UpdateRoomHandler(Context, new FakeClock());
    }

    [Fact]
    public async Task Handle_WithExistingRoom_UpdatesSuccessfully()
    {
        // Arrange
        var org = new OrganisationBuilder().Build();
        var project = new ProjectBuilder(org.Id).Build();
        var room = new RoomBuilder(project.Id)
            .WithNumber("R001")
            .WithName("Old Name")
            .Build();

        _builder
            .WithOrganisation(org)
            .WithProject(project)
            .WithRoom(room);

        var command = new UpdateRoomCommand(room.Id, "New Name");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var updated = _builder.Get<Room>(room.Id);
        updated!.Name.Should().Be("New Name");
    }
}
```
