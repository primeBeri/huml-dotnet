---
name: Radberi-UnitTesting
description: Use when writing unit or integration tests - provides comprehensive patterns for xUnit v3, AwesomeAssertions, Testcontainers, handler testing, and test infrastructure setup
---

# Unit Testing with xUnit v3

## Overview

This skill provides patterns for writing unit and integration tests using:

- **xUnit v3** - Test framework
- **AwesomeAssertions** - Fluent assertions
- **Testcontainers** - Real PostgreSQL for integration tests
- **NSubstitute** - Mocking framework (for boundaries)
- **bUnit** - Blazor component testing

**Core principle:** Test handlers (use cases), not infrastructure. Focus on behaviour, not implementation.

## When to Use

- Writing unit tests for command/query handlers
- Writing integration tests with real database
- Setting up test infrastructure for a new project
- Creating test data builders for domain aggregates
- Testing time-dependent business logic
- Testing multitenancy isolation

## Testing Philosophy

### What to Test

- Handler behaviour (commands, queries)
- Business logic in domain aggregates
- Validation and error handling
- Workflow state transitions
- Multitenancy isolation

### What NOT to Test

- Infrastructure implementation details
- Database queries directly (test through handlers)
- Third-party library behaviour

### Mocking Strategy

| Dependency Type | Approach | Example |
|-----------------|----------|---------|
| **Domain services** | Fakes | `FakeClock`, `FakeUnitOfWork` |
| **External boundaries** | NSubstitute | `IEmailService`, `IBlobStorage` |
| **Repositories** | Real + Testcontainers | `ISpaceHubDbContext` |

---

## xUnit v3 Patterns

### Basic Test Structure

```csharp
public class CreateRoomHandlerTests
{
    private readonly ISpaceHubDbContext _context;
    private readonly CreateRoomHandler _handler;

    public CreateRoomHandlerTests()
    {
        // Constructor runs before each test
        _context = Substitute.For<ISpaceHubDbContext>();
        _handler = new CreateRoomHandler(_context, Substitute.For<ILogger<CreateRoomHandler>>());
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = new CreateRoomCommand("R001", "Meeting Room", TestData.ProjectId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
```

### Async Lifecycle (Setup/Teardown)

```csharp
public class RoomIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private SpaceHubDbContext _context = null!;

    public RoomIntegrationTests()
    {
        _postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _context = CreateContext();
        await _context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
```

### Parameterised Tests

```csharp
[Theory]
[InlineData("")]
[InlineData(null)]
[InlineData("   ")]
public async Task Handle_WithInvalidNumber_ReturnsValidationError(string? invalidNumber)
{
    var command = new CreateRoomCommand(invalidNumber!, "Name", TestData.ProjectId);
    var result = await _handler.Handle(command, CancellationToken.None);

    result.IsSuccess.Should().BeFalse();
    result.ValidationErrors.Should().NotBeEmpty();
}

[Theory]
[MemberData(nameof(GetRoomTestCases))]
public async Task Handle_WithVariousInputs_ReturnsExpectedResult(
    string number, string name, bool expectedSuccess) { /* ... */ }

public static IEnumerable<object[]> GetRoomTestCases()
{
    yield return new object[] { "R001", "Valid Room", true };
    yield return new object[] { "", "No Number", false };
}
```

---

## Reference Files

For detailed patterns, see the reference files in this skill's directory:

| Reference | Contents |
|-----------|----------|
| `references/assertions.md` | AwesomeAssertions patterns (equality, collections, exceptions, Result) |
| `references/testcontainers.md` | Testcontainers setup, WebApplicationFactory, shared fixtures |
| `references/builders.md` | TestDataBuilder, RoomBuilder, ProjectBuilder patterns |
| `references/fakes.md` | FakeClock, FakeUnitOfWork, mocking strategy |
| `references/handlers.md` | LiteBus command/query handler testing, multitenancy tests |

For Syncfusion Blazor component testing (SfGrid, SfDialog, EditForm), use the `Radberi-BlazorTesting` skill.

---

## Test Data Management

### Deterministic Test IDs

```csharp
public static class TestData
{
    // Use deterministic UUIDs for predictable test data
    public static readonly Guid OrgAId = Guid.Parse("01945a3b-0001-7000-0000-000000000001");
    public static readonly Guid ProjectId = Guid.Parse("01945a3b-0002-7000-0000-000000000001");
    public static readonly Guid RoomId = Guid.Parse("01945a3b-0003-7000-0000-000000000001");
}
```

---

## Common Gotchas

### "DbContext has been disposed"
Reload from repository or use `AsNoTracking()` for read-only operations.

### Test passes alone but fails with others
Shared state between tests. Ensure fresh setup per test via constructor/`IAsyncLifetime`.

### DateTime comparison failures
Use `FakeClock` for deterministic time, or compare with tolerance:
```csharp
result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
```

### Async test hangs
Deadlock from `.Result` or `.Wait()`. Always use `await` and pass `CancellationToken.None`.

### NSubstitute "Could not find a call to return from"
Ensure interface is mocked (not concrete class), use `Arg.Any<T>()` correctly.

---

## Naming Convention

Follow `Method_Scenario_ExpectedResult`:

```csharp
[Fact] public async Task Handle_WithValidCommand_ReturnsSuccess()
[Fact] public async Task Handle_WithInvalidProject_ReturnsNotFound()
[Fact] public void Create_WithNegativeArea_ThrowsArgumentOutOfRangeException()
```

---

## Quick Reference

| Task | Pattern |
|------|---------|
| Setup per test | Constructor or `IAsyncLifetime.InitializeAsync` |
| Teardown | `IDisposable` or `IAsyncLifetime.DisposeAsync` |
| Single test | `[Fact]` |
| Parameterised | `[Theory]` + `[InlineData]` or `[MemberData]` |
| Shared fixture | `IClassFixture<T>` |
| Mock interface | `Substitute.For<IService>()` |
| Setup return | `.Returns(value)` |
| Verify call | `.Received(1).Method()` |
| Assert equality | `.Should().Be(expected)` |
| Assert null | `.Should().BeNull()` / `.NotBeNull()` |
| Assert collection | `.Should().HaveCount(n)` |
| Assert exception | `.Should().Throw<TException>()` |

---

## Related Skills

| Concern | Skill |
|---------|-------|
| Syncfusion Blazor component testing | `Radberi-BlazorTesting` |
| TDD methodology | `Radberi-TDD` |
| Avoiding testing anti-patterns | `Radberi-TestingAntiPatterns` |
| Flaky test elimination | `Radberi-ConditionBasedWaitingCSharp` |
