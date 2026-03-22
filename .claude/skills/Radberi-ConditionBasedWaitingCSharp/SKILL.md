---
name: Radberi-ConditionBasedWaitingCSharp
description: Use when tests have race conditions, timing dependencies, or inconsistent pass/fail behaviour - replaces arbitrary Thread.Sleep/Task.Delay with condition polling to wait for actual state changes, eliminating flaky tests from timing guesses
---

# Condition-Based Waiting (C#/.NET)

## Overview

Flaky tests often guess at timing with arbitrary delays. This creates race conditions where tests pass locally but fail in CI or under load.

**Core principle:** Wait for the actual condition you care about, not a guess about how long it takes.

## When to Use

```
┌─────────────────────────────────────┐
│ Test uses Thread.Sleep/Task.Delay? │
└──────────────┬──────────────────────┘
               │ yes
               ▼
┌─────────────────────────────────────┐
│   Testing actual timing behaviour?  │
│   (debounce, throttle, intervals)   │
└──────────────┬──────────────────────┘
        yes    │    no
        ▼      │    ▼
┌─────────────┐│┌──────────────────────┐
│ Document WHY││ Use condition-based   │
│ timeout is  │││ waiting              │
│ needed      │││                      │
└─────────────┘│└──────────────────────┘
```

**Use when:**

- Tests have arbitrary delays (`Thread.Sleep`, `Task.Delay`)
- Tests are flaky (pass sometimes, fail under load or in CI)
- Tests timeout when run in parallel
- Waiting for async operations to complete
- Integration tests with databases, containers, or message queues
- SignalR/WebSocket message handling tests
- Background service (`IHostedService`) tests

**Don't use when:**

- Testing actual timing behaviour (debounce, rate limiting, scheduled intervals)
- Always document WHY if using arbitrary timeout

## Core Pattern

```csharp
// ❌ BEFORE: Guessing at timing
await Task.Delay(500);
var result = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
result.Should().NotBeNull();

// ✅ AFTER: Waiting for condition
var result = await WaitFor.ConditionAsync(
    () => dbContext.Users.FirstOrDefault(u => u.Id == userId),
    "user to be created in database");
result.Should().NotBeNull();
```

## Quick Patterns

| Scenario | Pattern |
|----------|---------|
| Wait for database record | `WaitFor.ConditionAsync(() => db.Users.Find(id), "user exists")` |
| Wait for count | `WaitFor.ConditionAsync(() => items.Count >= 5, "5 items received")` |
| Wait for file | `WaitFor.ConditionAsync(() => File.Exists(path), "file created")` |
| Wait for state | `WaitFor.ConditionAsync(() => service.State == "Ready", "service ready")` |
| Wait for SignalR | `WaitFor.ConditionAsync(() => messages.Any(m => m.Type == "Done"), "done message")` |
| Complex condition | `WaitFor.ConditionAsync(() => obj.Ready && obj.Value > 10, "ready with value")` |

## Implementation

See `WaitForCondition.cs` for the complete implementation. Core method:

```csharp
public static class WaitFor
{
    /// <summary>
    /// Polls a condition until it returns a non-null/non-default value or times out.
    /// </summary>
    /// <typeparam name="T">The type of result to wait for.</typeparam>
    /// <param name="condition">Function that returns the value when ready, null/default otherwise.</param>
    /// <param name="description">Human-readable description for timeout error messages.</param>
    /// <param name="timeout">Maximum time to wait. Defaults to 5 seconds.</param>
    /// <param name="pollInterval">Time between condition checks. Defaults to 10ms.</param>
    /// <param name="cancellationToken">Token to cancel the wait operation.</param>
    /// <returns>The non-null result from the condition function.</returns>
    /// <exception cref="TimeoutException">Thrown when timeout expires before condition is met.</exception>
    public static async Task<T> ConditionAsync<T>(
        Func<T?> condition,
        string description,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var timeoutValue = timeout ?? TimeSpan.FromSeconds(5);
        var pollIntervalValue = pollInterval ?? TimeSpan.FromMilliseconds(10);
        var stopwatch = Stopwatch.StartNew();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = condition();
            if (result is not null)
            {
                return result;
            }

            if (stopwatch.Elapsed > timeoutValue)
            {
                throw new TimeoutException(
                    $"Timeout waiting for {description} after {timeoutValue.TotalMilliseconds:F0}ms");
            }

            await Task.Delay(pollIntervalValue, cancellationToken);
        }
    }
}
```

## Common Testing Scenarios

### xUnit with AwesomeAssertions

```csharp
[Fact]
public async Task Handle_WithValidCommand_CreatesRoom()
{
    // Arrange
    var command = new CreateRoomCommand("Test Room");

    // Act
    await handler.Handle(command, CancellationToken.None);

    // Assert - wait for database to reflect the change
    var room = await WaitFor.ConditionAsync(
        () => dbContext.Rooms.FirstOrDefault(r => r.Name == "Test Room"),
        "room to be created");

    room.Should().NotBeNull();
    room.Name.Should().Be("Test Room");
}
```

### Testcontainers Integration Tests

```csharp
[Fact]
public async Task Database_AfterMigration_HasExpectedSchema()
{
    // Arrange - container already started via fixture

    // Act - run migrations
    await dbContext.Database.MigrateAsync();

    // Assert - wait for tables to exist
    await WaitFor.ConditionAsync(
        () => dbContext.Database.CanConnect(),
        "database connection available",
        timeout: TimeSpan.FromSeconds(30)); // Containers may need longer

    var tableExists = await dbContext.Database
        .ExecuteSqlRawAsync("SELECT 1 FROM information_schema.tables WHERE table_name = 'Rooms'");

    tableExists.Should().BeGreaterThan(0);
}
```

### WebApplicationFactory with Background Services

```csharp
[Fact]
public async Task BackgroundService_ProcessesQueuedItem()
{
    // Arrange
    var client = factory.CreateClient();
    await client.PostAsJsonAsync("/api/queue", new QueueItemDto { Data = "test" });

    // Assert - wait for background processing
    var processed = await WaitFor.ConditionAsync(
        async () => await dbContext.ProcessedItems
            .FirstOrDefaultAsync(i => i.Data == "test"),
        "item to be processed by background service",
        timeout: TimeSpan.FromSeconds(10));

    processed.Should().NotBeNull();
    processed.Status.Should().Be(ProcessingStatus.Completed);
}
```

### SignalR Hub Tests

```csharp
[Fact]
public async Task Hub_WhenRoomCreated_BroadcastsNotification()
{
    // Arrange
    var messages = new List<NotificationDto>();
    hubConnection.On<NotificationDto>("ReceiveNotification", msg => messages.Add(msg));
    await hubConnection.StartAsync();

    // Act
    await client.PostAsJsonAsync("/api/rooms", new CreateRoomDto { Name = "Test" });

    // Assert - wait for SignalR message
    var notification = await WaitFor.ConditionAsync(
        () => messages.FirstOrDefault(m => m.Type == "RoomCreated"),
        "RoomCreated notification via SignalR");

    notification.Should().NotBeNull();
    notification.Payload.Should().Contain("Test");
}
```

### bUnit Component Tests

```csharp
[Fact]
public void DataGrid_WhenLoaded_DisplaysItems()
{
    // Arrange
    var cut = RenderComponent<RoomListComponent>();

    // Act - trigger load
    cut.Find("button.refresh").Click();

    // Assert - wait for async data to load
    cut.WaitForState(() => cut.FindAll("tr.data-row").Count > 0,
        timeout: TimeSpan.FromSeconds(2));

    cut.FindAll("tr.data-row").Count.Should().Be(5);
}
```

## Common Mistakes

### Polling Too Fast

```csharp
// ❌ Wastes CPU cycles
pollInterval: TimeSpan.FromMilliseconds(1)

// ✅ 10ms is efficient for most cases
pollInterval: TimeSpan.FromMilliseconds(10)
```

### No Timeout

```csharp
// ❌ Loops forever if condition never met
while (!condition()) { await Task.Delay(10); }

// ✅ Always include timeout with clear error message
await WaitFor.ConditionAsync(condition, "descriptive message", timeout: TimeSpan.FromSeconds(5));
```

### Stale Data in Loop

```csharp
// ❌ Captures stale reference before loop
var items = service.GetItems();
await WaitFor.ConditionAsync(() => items.Count >= 5, "..."); // items never changes!

// ✅ Call getter inside condition for fresh data
await WaitFor.ConditionAsync(() => service.GetItems().Count >= 5, "...");
```

### Ignoring CancellationToken

```csharp
// ❌ Test hangs if cancelled
await WaitFor.ConditionAsync(() => result, "...");

// ✅ Pass test's cancellation token
await WaitFor.ConditionAsync(() => result, "...", cancellationToken: TestContext.CancellationToken);
```

### Using Thread.Sleep Instead of Task.Delay

```csharp
// ❌ Blocks the thread
Thread.Sleep(100);

// ✅ Yields the thread while waiting
await Task.Delay(100);
```

## When Arbitrary Timeout IS Correct

```csharp
// Service publishes metrics every 100ms - need 2 cycles to verify
await WaitFor.ConditionAsync(
    () => service.State == "Running",
    "service to start");
await Task.Delay(200); // Wait for 2 metric cycles - documented and justified
// 200ms = 2 cycles at 100ms intervals
```

**Requirements:**

1. First wait for triggering condition
2. Based on known timing (not guessing)
3. Comment explaining WHY

## Integration with Test Frameworks

### xUnit Test Timeout

```csharp
[Fact(Timeout = 30000)] // 30 second test timeout
public async Task LongRunningIntegrationTest()
{
    // WaitFor respects CancellationToken, so test timeout works properly
    await WaitFor.ConditionAsync(
        () => service.IsReady,
        "service ready",
        timeout: TimeSpan.FromSeconds(25), // Less than test timeout
        cancellationToken: TestContext.Current.CancellationToken);
}
```

### Shared Test Infrastructure

Place `WaitFor` helper in shared test infrastructure project:

```
tests/
├── SpaceHub.TestInfrastructure/
│   └── Helpers/
│       └── WaitFor.cs          ← Put here
├── SpaceHub.Domain.Tests/
├── SpaceHub.Application.Tests/
├── SpaceHub.Infrastructure.Tests/
└── SpaceHub.Api.Tests/
```

## Real-World Impact

From the original TypeScript implementation (adapted for C# scenarios):

- **Fixed flaky tests**: Race conditions eliminated
- **Pass rate**: 60% → 100% in CI
- **Execution time**: 40% faster (no over-waiting)
- **Maintainability**: Clear intent in test code
