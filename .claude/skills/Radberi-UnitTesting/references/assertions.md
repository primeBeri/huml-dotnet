# AwesomeAssertions Reference

Fluent assertion patterns for .NET tests.

## Basic Assertions

```csharp
// Equality
result.Value.Name.Should().Be("Meeting Room");
result.Value.Id.Should().NotBe(Guid.Empty);

// Nullability
room.Should().NotBeNull();
room.Description.Should().BeNull();

// Boolean
result.IsSuccess.Should().BeTrue();
result.IsSuccess.Should().BeFalse("because validation failed");

// Numeric
room.Area.Should().BeGreaterThan(0);
room.Area.Should().BeInRange(10, 100);
room.Area.Should().BeApproximately(50.5m, 0.1m);
```

## Collection Assertions

```csharp
// Count
rooms.Should().HaveCount(5);
rooms.Should().BeEmpty();
rooms.Should().NotBeEmpty();
rooms.Should().HaveCountGreaterThan(2);

// Contents
rooms.Should().Contain(r => r.Name == "Meeting Room");
rooms.Should().AllSatisfy(r => r.ProjectId.Should().Be(projectId));
rooms.Should().BeInAscendingOrder(r => r.Number);

// Single item
rooms.Should().ContainSingle(r => r.Number == "R001");
rooms.Single().Name.Should().Be("Meeting Room");
```

## Exception Assertions

```csharp
// Sync
var act = () => new Room("", "Name", projectId);
act.Should().Throw<ArgumentException>()
    .WithMessage("*number*");

// Async
var act = async () => await handler.Handle(invalidCommand, CancellationToken.None);
await act.Should().ThrowAsync<ValidationException>();
```

## Ardalis.Result Assertions

```csharp
// Success
result.IsSuccess.Should().BeTrue();
result.Value.Should().NotBeNull();
result.Value.Id.Should().NotBe(Guid.Empty);

// NotFound
result.IsSuccess.Should().BeFalse();
result.Status.Should().Be(ResultStatus.NotFound);

// Validation errors
result.IsSuccess.Should().BeFalse();
result.Status.Should().Be(ResultStatus.Invalid);
result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("required"));

// General errors
result.Errors.Should().Contain("Room number already exists");

// Forbidden
result.Status.Should().Be(ResultStatus.Forbidden);
```

## Assertion Scope (Multiple Assertions)

```csharp
using (new AssertionScope())
{
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().NotBeNull();
    result.Value.Name.Should().Be("Meeting Room");
    result.Value.Number.Should().Be("R001");
}
// All failures reported together, not just the first
```
