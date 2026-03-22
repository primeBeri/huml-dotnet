# LiteBus Handler Testing

Patterns for testing command and query handlers.

## Command Handler Test

```csharp
public class CreateRoomHandlerTests
{
    private readonly ISpaceHubDbContext _context;
    private readonly FakeClock _clock;
    private readonly CreateRoomHandler _handler;

    public CreateRoomHandlerTests()
    {
        _context = Substitute.For<ISpaceHubDbContext>();
        _clock = new FakeClock();
        _handler = new CreateRoomHandler(
            _context,
            _clock,
            Substitute.For<ILogger<CreateRoomHandler>>());
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccessWithDto()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new Project("PRJ001", "Test", Guid.NewGuid());

        _context.Projects.FindAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var rooms = new List<Room>();
        var mockDbSet = rooms.AsQueryable().BuildMockDbSet();
        mockDbSet.When(x => x.Add(Arg.Any<Room>()))
            .Do(x => rooms.Add(x.Arg<Room>()));
        _context.Rooms.Returns(mockDbSet);

        var command = new CreateRoomCommand("R001", "Meeting Room", projectId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Number.Should().Be("R001");
        result.Value.Name.Should().Be("Meeting Room");

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistentProject_ReturnsNotFound()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _context.Projects.FindAsync(projectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var command = new CreateRoomCommand("R001", "Meeting Room", projectId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
    }
}
```

## Query Handler Test

```csharp
public class GetRoomsHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingRooms_ReturnsOrderedList()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var rooms = new List<Room>
        {
            new Room("R003", "Room C", projectId),
            new Room("R001", "Room A", projectId),
            new Room("R002", "Room B", projectId)
        };

        var context = Substitute.For<ISpaceHubDbContext>();
        context.Rooms.Returns(rooms.AsQueryable().BuildMockDbSet());

        var handler = new GetRoomsHandler(context);
        var query = new GetRoomsQuery(projectId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().BeInAscendingOrder(r => r.Number);
    }
}
```

## Multitenancy Testing

```csharp
[Fact]
public async Task GetRoom_FromOtherOrganisation_ReturnsNotFound()
{
    // Arrange
    var orgA = new OrganisationBuilder().Build();
    var orgB = new OrganisationBuilder().Build();

    var projectA = new ProjectBuilder(orgA.Id).Build();
    var roomA = new RoomBuilder(projectA.Id).Build();

    _builder
        .WithOrganisation(orgA)
        .WithOrganisation(orgB)
        .WithProject(projectA)
        .WithRoom(roomA);

    // User from Org B trying to access Org A's room
    var handler = CreateHandlerWithTenant(orgB.Id);
    var query = new GetRoomQuery(roomA.Id);

    // Act
    var result = await handler.Handle(query, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Status.Should().Be(ResultStatus.NotFound);
}

[Fact]
public async Task ListRooms_OnlyReturnsOwnOrganisationRooms()
{
    // Arrange
    var orgA = new OrganisationBuilder().Build();
    var orgB = new OrganisationBuilder().Build();

    var projectA = new ProjectBuilder(orgA.Id).Build();
    var projectB = new ProjectBuilder(orgB.Id).Build();

    var roomA = new RoomBuilder(projectA.Id).Build();
    var roomB = new RoomBuilder(projectB.Id).Build();

    _builder
        .WithOrganisation(orgA)
        .WithOrganisation(orgB)
        .WithProject(projectA)
        .WithProject(projectB)
        .WithRoom(roomA)
        .WithRoom(roomB);

    // User from Org A
    var handler = CreateHandlerWithTenant(orgA.Id);

    // Act
    var result = await handler.Handle(new GetRoomsQuery(projectA.Id), CancellationToken.None);

    // Assert
    result.Value.Should().HaveCount(1);
    result.Value.Should().AllSatisfy(r => r.ProjectId.Should().Be(projectA.Id));
}
```
