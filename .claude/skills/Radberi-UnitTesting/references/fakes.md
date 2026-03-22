# Fake Implementations

Test doubles for domain services. Use fakes for deterministic, controllable behaviour.

**Why fakes over mocks for domain services?**
- Fakes have realistic behaviour you control
- Mocks verify interactions, fakes verify state
- Fakes are reusable across tests
- Easier to understand test intent

## FakeClock

```csharp
public interface IClock
{
    DateTime UtcNow { get; }
    DateTimeOffset UtcNowOffset { get; }
}

public class FakeClock : IClock
{
    private DateTime _utcNow;

    public FakeClock(DateTime? utcNow = null)
    {
        _utcNow = utcNow ?? new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    }

    public DateTime UtcNow => _utcNow;
    public DateTimeOffset UtcNowOffset => _utcNow;

    public void SetTime(DateTime utcNow)
    {
        _utcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
    }

    public void Advance(TimeSpan duration)
    {
        _utcNow = _utcNow.Add(duration);
    }
}

// Usage
var clock = new FakeClock(new DateTime(2024, 6, 15));
clock.Advance(TimeSpan.FromDays(30)); // Now July 15
```

## FakeUnitOfWork

```csharp
public interface IUnitOfWork
{
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}

public class FakeUnitOfWork : IUnitOfWork
{
    public bool BeginCalled { get; private set; }
    public bool CommitCalled { get; private set; }
    public bool RollbackCalled { get; private set; }

    public Task BeginTransactionAsync(CancellationToken ct = default)
    {
        BeginCalled = true;
        return Task.CompletedTask;
    }

    public Task CommitTransactionAsync(CancellationToken ct = default)
    {
        CommitCalled = true;
        return Task.CompletedTask;
    }

    public Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        RollbackCalled = true;
        return Task.CompletedTask;
    }

    public void Reset()
    {
        BeginCalled = false;
        CommitCalled = false;
        RollbackCalled = false;
    }
}

// Usage in test
var unitOfWork = new FakeUnitOfWork();
// ... run handler ...
unitOfWork.CommitCalled.Should().BeTrue();
```

## External Boundary Mocking (NSubstitute)

For external boundaries (email, blob storage, etc.), NSubstitute is appropriate:

```csharp
// For external boundaries, NSubstitute is appropriate
var emailService = Substitute.For<IEmailService>();
emailService.SendAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
    .Returns(Task.FromResult(true));

// Verify interaction
await emailService.Received(1).SendAsync(
    Arg.Is<Email>(e => e.To == "user@example.com"),
    Arg.Any<CancellationToken>());
```

## Mocking Strategy Quick Reference

| Dependency Type | Approach | Example |
|-----------------|----------|---------|
| **Domain services** | Fakes | `FakeClock`, `FakeUnitOfWork` |
| **External boundaries** | NSubstitute | `IEmailService`, `IBlobStorage` |
| **Repositories** | Real + Testcontainers | `ISpaceHubDbContext` |
