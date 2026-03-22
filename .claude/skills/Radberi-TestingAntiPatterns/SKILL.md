---
name: Radberi-TestingAntiPatterns
description: Use when writing or changing tests, adding mocks, or tempted to add test-only methods to production code - prevents testing mock behaviour, production pollution with test-only methods, and mocking without understanding dependencies
---

# Testing Anti-Patterns (.NET / xUnit 3 / bUnit / NSubstitute)

## Overview

Tests must verify real behaviour, not mock behaviour. Mocks are a means to isolate, not the thing being tested.

**Core principle:** Test what the code does, not what the mocks do.

**Following strict TDD prevents these anti-patterns.**

## The Iron Laws

```text
1. NEVER test mock behaviour
2. NEVER add test-only methods to production classes
3. NEVER mock without understanding dependencies
```

## Anti-Pattern 1: Testing Mock Behaviour

**The violation:**

```csharp
// BAD: Testing that the mock exists
[Fact]
public void RoomList_RendersSidebar()
{
    // Arrange
    using var ctx = new TestContext();
    ctx.Services.AddSingleton(Substitute.For<INavigationService>());

    // Act
    var cut = ctx.RenderComponent<RoomListPage>();

    // Assert - WRONG: asserting on mock existence!
    cut.Find("[data-testid='sidebar-mock']").Should().NotBeNull();
}
```

**Why this is wrong:**

- You're verifying the mock works, not that the component works
- Test passes when mock is present, fails when it's not
- Tells you nothing about real behaviour

**Your human partner's correction:** "Are we testing the behaviour of a mock?"

**The fix:**

```csharp
// GOOD: Test real component or don't mock it
[Fact]
public void RoomList_RendersSidebar()
{
    // Arrange
    using var ctx = new TestContext();
    ctx.Services.AddSyncfusionBlazor();

    // Act
    var cut = ctx.RenderComponent<RoomListPage>();

    // Assert - Test real navigation renders
    cut.Find("nav").Should().NotBeNull();
}

// OR if sidebar must be mocked for isolation:
// Don't assert on the mock - test Page's behaviour with sidebar present
```

### Gate Function

```text
BEFORE asserting on any mock element:
  Ask: "Am I testing real component behaviour or just mock existence?"

  IF testing mock existence:
    STOP - Delete the assertion or unmock the component

  Test real behaviour instead
```

## Anti-Pattern 2: Test-Only Methods in Production

**The violation:**

```csharp
// BAD: ResetForTesting() only used in tests
public class SpaceHubDbContext : DbContext, ISpaceHubDbContext
{
    public void ResetForTesting()  // Looks like production API!
    {
        ChangeTracker.Clear();
        Database.EnsureDeleted();
        Database.EnsureCreated();
    }
}

// In tests
public class RoomHandlerTests : IAsyncLifetime
{
    public async Task DisposeAsync() => _dbContext.ResetForTesting();
}
```

**Why this is wrong:**

- Production class polluted with test-only code
- Dangerous if accidentally called in production
- Violates YAGNI and separation of concerns
- Confuses object lifecycle with entity lifecycle

**The fix:**

```csharp
// GOOD: Test utilities handle test cleanup
// SpaceHubDbContext has no ResetForTesting() - it's a normal DbContext

// In tests/SpaceHub.Application.Tests/TestUtilities/
public static class DatabaseTestHelpers
{
    public static async Task CleanupDatabaseAsync(SpaceHubDbContext dbContext)
    {
        dbContext.ChangeTracker.Clear();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }
}

// In tests
public class RoomHandlerTests : IAsyncLifetime
{
    public async Task DisposeAsync()
        => await DatabaseTestHelpers.CleanupDatabaseAsync(_dbContext);
}
```

### Anti-Pattern 2: Gate Function

```text
BEFORE adding any method to production class:
  Ask: "Is this only used by tests?"

  IF yes:
    STOP - Don't add it
    Put it in test utilities instead

  Ask: "Does this class own this resource's lifecycle?"

  IF no:
    STOP - Wrong class for this method
```

## Anti-Pattern 3: Mocking Without Understanding

**The violation:**

```csharp
// BAD: Mock breaks test logic
[Fact]
public async Task AddRoom_DetectsDuplicateNumber()
{
    // Arrange
    var dbContext = Substitute.For<ISpaceHubDbContext>();
    // Mock prevents actual SaveChanges that test depends on!
    dbContext.SaveChangesAsync(Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(1));

    var handler = new CreateRoomHandler(dbContext);
    var command = new CreateRoomCommand("101", "Meeting Room", _projectId);

    // Act
    await handler.Handle(command, CancellationToken.None);
    await handler.Handle(command, CancellationToken.None); // Should detect duplicate!

    // Assert - Will never fail because mock doesn't track state!
}
```

**Why this is wrong:**

- Mocked method had side effect test depended on (persisting data)
- Over-mocking to "be safe" breaks actual behaviour
- Test passes for wrong reason or fails mysteriously

**The fix:**

```csharp
// GOOD: Use real DbContext with in-memory provider or Testcontainers
[Fact]
public async Task AddRoom_DetectsDuplicateNumber()
{
    // Arrange - Use real DbContext behaviour
    await using var dbContext = CreateTestDbContext(); // In-memory or Testcontainers
    var handler = new CreateRoomHandler(dbContext);
    var command = new CreateRoomCommand("101", "Meeting Room", _projectId);

    // Act
    var firstResult = await handler.Handle(command, CancellationToken.None);
    var duplicateResult = await handler.Handle(command, CancellationToken.None);

    // Assert
    firstResult.IsSuccess.Should().BeTrue();
    duplicateResult.IsSuccess.Should().BeFalse();
    duplicateResult.Errors.Should().Contain(e => e.Contains("duplicate"));
}
```

### Anti-Pattern 3: Gate Function

```text
BEFORE mocking any method:
  STOP - Don't mock yet

  1. Ask: "What side effects does the real method have?"
  2. Ask: "Does this test depend on any of those side effects?"
  3. Ask: "Do I fully understand what this test needs?"

  IF depends on side effects:
    Mock at lower level (the actual slow/external operation)
    OR use test doubles that preserve necessary behaviour
    NOT the high-level method the test depends on

  IF unsure what test depends on:
    Run test with real implementation FIRST
    Observe what actually needs to happen
    THEN add minimal mocking at the right level

  Red flags:
    - "I'll mock this to be safe"
    - "This might be slow, better mock it"
    - Mocking without understanding the dependency chain
```

## Anti-Pattern 4: Incomplete Mocks

**The violation:**

```csharp
// BAD: Partial mock - only fields you think you need
var mockResult = Result<RoomDto>.Success(new RoomDto
{
    Id = Guid.NewGuid(),
    Name = "Meeting Room"
    // Missing: Number, Description, AreaRequired, Notes, CreatedAt, UpdatedAt
    // that downstream code or mapping uses
});

// Later: breaks when component displays room.Number or formats room.AreaRequired
```

**Why this is wrong:**

- **Partial mocks hide structural assumptions** - You only mocked fields you know about
- **Downstream code may depend on fields you didn't include** - Silent failures or NullReferenceException
- **Tests pass but integration fails** - Mock incomplete, real API complete
- **False confidence** - Test proves nothing about real behaviour

**The Iron Rule:** Mock the COMPLETE data structure as it exists in reality, not just fields your immediate test uses.

**The fix:**

```csharp
// GOOD: Mirror real API completeness
var mockResult = Result<RoomDto>.Success(new RoomDto
{
    Id = Guid.NewGuid(),
    Number = "101",
    Name = "Meeting Room",
    Description = "Large meeting room with projector",
    AreaRequired = 45.5m,
    Notes = null,
    CreatedAt = DateTimeOffset.UtcNow,
    UpdatedAt = DateTimeOffset.UtcNow
    // All fields real handler returns
});

// Even better: Use a test data builder
var mockResult = Result<RoomDto>.Success(
    TestData.Room.BuildDto());
```

### Anti-Pattern 4: Gate Function

```text
BEFORE creating mock responses:
  Check: "What fields does the real DTO/Result contain?"

  Actions:
    1. Examine actual DTO class definition
    2. Include ALL fields that might be consumed downstream
    3. Verify mock matches real response schema completely

  Critical:
    If you're creating a mock, you must understand the ENTIRE structure
    Partial mocks fail silently when code depends on omitted fields

  If uncertain: Include all DTO properties with sensible defaults
```

## Anti-Pattern 5: Integration Tests as Afterthought

**The violation:**

```text
Implementation complete
No tests written
"Ready for testing"
```

**Why this is wrong:**

- Testing is part of implementation, not optional follow-up
- TDD would have caught this
- Can't claim complete without tests

**The fix:**

```C#
TDD cycle with xUnit 3:

1. Write failing test
   [Fact]
   public async Task CreateRoom_WithValidData_ReturnsSuccess()
   {
       // Arrange
       // Act
       // Assert - this will fail, handler doesn't exist yet
   }

2. Implement to pass
   public class CreateRoomHandler : ICommandHandler<CreateRoomCommand, Result<RoomDto>>
   {
       // Minimum code to make test pass
   }

3. Refactor while green

4. THEN claim complete
```

## When Mocks Become Too Complex

**Warning signs:**

- Mock setup longer than test logic
- Mocking everything to make test pass
- `Substitute.For<T>()` calls everywhere
- Test breaks when mock changes
- Using `Arg.Any<T>()` excessively

**Your human partner's question:** "Do we need to be using a mock here?"

**Consider:** Integration tests with Testcontainers PostgreSQL often simpler than complex mocks

```csharp
// Complex mock setup - warning sign!
[Fact]
public async Task CreateRoom_Complex_MockHell()
{
    // Arrange - This is getting out of hand
    var dbContext = Substitute.For<ISpaceHubDbContext>();
    var logger = Substitute.For<ILogger<CreateRoomHandler>>();
    var validator = Substitute.For<IValidator<CreateRoomCommand>>();
    var tenantContext = Substitute.For<ITenantContext>();
    var timeProvider = Substitute.For<TimeProvider>();

    dbContext.Projects.Returns(Substitute.For<DbSet<Project>>());
    tenantContext.OrganisationId.Returns(Guid.NewGuid());
    validator.ValidateAsync(Arg.Any<CreateRoomCommand>(), Arg.Any<CancellationToken>())
        .Returns(new ValidationResult());
    // ... 20 more lines of setup
}

// BETTER: Use real dependencies via Testcontainers
[Fact]
public async Task CreateRoom_WithRealDatabase()
{
    // Arrange
    await using var dbContext = await _fixture.CreateDbContextAsync();
    var handler = new CreateRoomHandler(dbContext, _logger);

    // Act & Assert - simple, real behaviour
}
```

## NSubstitute Best Practices

### DO: Use Returns() for simple values

```csharp
var tenantContext = Substitute.For<ITenantContext>();
tenantContext.OrganisationId.Returns(_testOrgId);
```

### DO: Use Received() to verify interactions

```csharp
// Assert
await dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
```

### DO: Use `Arg.Is<T>()` for specific matching

```csharp
await mediator.Received().Send(
    Arg.Is<CreateRoomCommand>(c => c.Number == "101"),
    Arg.Any<CancellationToken>());
```

### DON'T: Mock what you're testing

```csharp
// BAD: Why are you mocking the thing under test?
var handler = Substitute.For<ICommandHandler<CreateRoomCommand, Result<RoomDto>>>();
```

### DON'T: Over-use `Arg.Any<T>()`

```csharp
// BAD: Proves nothing about what was actually passed
mediator.Received().Send(Arg.Any<object>(), Arg.Any<CancellationToken>());

// GOOD: Verify specific values
mediator.Received().Send(
    Arg.Is<CreateRoomCommand>(c => c.ProjectId == expectedProjectId),
    Arg.Any<CancellationToken>());
```

## bUnit Component Testing Patterns

### Testing with Authorization

```csharp
[Fact]
public void RoomList_ShowsCreateButton_WhenUserHasPermission()
{
    // Arrange
    using var ctx = new TestContext();
    var authContext = ctx.AddTestAuthorization();
    authContext.SetAuthorized("testuser");
    authContext.SetClaims(new Claim("permission", "project:rooms:create"));

    // Act
    var cut = ctx.RenderComponent<RoomList>(parameters =>
        parameters.Add(p => p.ProjectId, _testProjectId));

    // Assert
    cut.Find("button.create-room").Should().NotBeNull();
}
```

### Testing Syncfusion Blazor Components

```csharp
[Fact]
public void RoomForm_ValidatesRequiredFields()
{
    // Arrange
    using var ctx = new TestContext();
    ctx.Services.AddSyncfusionBlazor();

    // Act
    var cut = ctx.RenderComponent<RoomForm>();
    var submitButton = cut.Find("button[type='submit']");
    submitButton.Click();

    // Assert
    cut.FindAll(".e-error").Should().HaveCountGreaterThan(0);
}
```

## TDD Prevents These Anti-Patterns

**Why TDD helps:**

1. **Write test first** - Forces you to think about what you're actually testing
2. **Watch it fail** - Confirms test tests real behaviour, not mocks
3. **Minimal implementation** - No test-only methods creep in
4. **Real dependencies** - You see what the test actually needs before mocking

**If you're testing mock behaviour, you violated TDD** - you added mocks without watching test fail against real code first.

## Quick Reference

| Anti-Pattern | Fix |
|--------------|-----|
| Assert on mock elements with `cut.Find("[data-testid='mock']")` | Test real component with `cut.FindComponent<T>()` |
| Test-only methods in production (`ResetForTesting()`) | Move to `tests/TestUtilities/` |
| `Substitute.For<IRepository>()` breaking side effects | Use Testcontainers or in-memory provider |
| Partial DTOs missing required fields | Create complete DTOs or use test data builders |
| Tests as afterthought | TDD - write `[Fact]` first, then implement |
| Complex mock setup (>10 lines of `Substitute.For`) | Consider integration tests |

## Red Flags

- Assertions checking for `*-mock` test IDs
- Methods like `ResetForTesting()`, `ClearForTests()` in production classes
- Mock setup is >50% of test method
- Test fails when you remove a `Substitute.For<T>()`
- Can't explain why mock is needed
- Using `Arg.Any<T>()` for everything
- Mocking "just to be safe"

## The Bottom Line

**Mocks are tools to isolate, not things to test.**

If TDD reveals you're testing mock behaviour, you've gone wrong.

Fix: Test real behaviour or question why you're mocking at all.

## Skill References

| Situation | Skill |
|-----------|-------|
| xUnit/NSubstitute/Testcontainers patterns | `Radberi-UnitTesting` |
| Syncfusion Blazor component testing | `Radberi-BlazorTesting` |
| TDD methodology | `Radberi-TDD` |
