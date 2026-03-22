---
name: Radberi-TDD
description: Use when implementing any feature or bugfix, before writing implementation code - write the test first, watch it fail, write minimal code to pass; ensures tests actually verify behaviour by requiring failure first
---

# Test-Driven Development (TDD) - .NET / xUnit 3 / AwesomeAssertions

## Overview

Write the test first. Watch it fail. Write minimal code to pass.

**Core principle:** If you didn't watch the test fail, you don't know if it tests the right thing.

**Violating the letter of the rules is violating the spirit of the rules.**

## When to Use

**Always:**

- New features
- Bug fixes
- Refactoring
- Behaviour changes

**Exceptions (ask your human partner):**

- Throwaway prototypes
- Generated code
- Configuration files

Thinking "skip TDD just this once"? Stop. That's rationalisation.

## The Iron Law

```text
NO PRODUCTION CODE WITHOUT A FAILING TEST FIRST
```

Write code before the test? Delete it. Start over.

**No exceptions:**

- Don't keep it as "reference"
- Don't "adapt" it while writing tests
- Don't look at it
- Delete means delete

Implement fresh from tests. Period.

## Red-Green-Refactor

```text
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│      RED        │────▶│     GREEN       │────▶│    REFACTOR     │
│ Write failing   │     │ Minimal code    │     │ Clean up        │
│ test            │     │ to pass         │     │ (stay green)    │
└─────────────────┘     └─────────────────┘     └────────┬────────┘
        ▲                                                │
        └────────────────────────────────────────────────┘
                            Next test
```

### RED - Write Failing Test

Write one minimal test showing what should happen.

**Good:**

```csharp
[Fact]
public async Task RetryOperation_RetriesThreeTimes_BeforeSuccess()
{
    // Arrange
    var attempts = 0;
    var operation = () =>
    {
        attempts++;
        if (attempts < 3)
        {
            throw new InvalidOperationException("fail");
        }

        return Task.FromResult("success");
    };

    // Act
    var result = await RetryService.ExecuteAsync(operation);

    // Assert
    result.Should().Be("success");
    attempts.Should().Be(3);
}
```

Clear name, tests real behaviour, one thing.

**Bad:**

```csharp
[Fact]
public async Task RetryWorks()
{
    // Arrange
    var mock = Substitute.For<IOperation>();
    mock.ExecuteAsync()
        .Returns(
            x => throw new Exception(),
            x => throw new Exception(),
            x => Task.FromResult("success"));

    // Act
    await _sut.ExecuteAsync(mock.ExecuteAsync);

    // Assert
    await mock.Received(3).ExecuteAsync();
}
```

Vague name, tests mock not code.

**Requirements:**

- One behaviour
- Clear name (MethodName_Scenario_ExpectedBehavior)
- Real code (no mocks unless unavoidable)
- AAA pattern (Arrange-Act-Assert)

### Verify RED - Watch It Fail

**MANDATORY. Never skip.**

```bash
dotnet test --filter "FullyQualifiedName~RetryOperation_RetriesThreeTimes"
```

Confirm:

- Test fails (not errors)
- Failure message is expected
- Fails because feature missing (not typos)

**Test passes?** You're testing existing behaviour. Fix test.

**Test errors?** Fix error, re-run until it fails correctly.

### GREEN - Minimal Code

Write simplest code to pass the test.

**Good:**

```csharp
public static class RetryService
{
    public static async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        for (var i = 0; i < 3; i++)
        {
            try
            {
                return await operation();
            }
            catch
            {
                if (i == 2)
                {
                    throw;
                }
            }
        }

        throw new InvalidOperationException("Unreachable");
    }
}
```

Just enough to pass.

**Bad:**

```csharp
public static async Task<T> ExecuteAsync<T>(
    Func<Task<T>> operation,
    RetryOptions? options = null)
{
    options ??= RetryOptions.Default;

    for (var i = 0; i < options.MaxRetries; i++)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex) when (options.ShouldRetry(ex))
        {
            await Task.Delay(options.GetDelay(i));
            options.OnRetry?.Invoke(i, ex);
        }
    }
    // YAGNI - over-engineered!
}
```

Over-engineered.

Don't add features, refactor other code, or "improve" beyond the test.

### Verify GREEN - Watch It Pass

**MANDATORY.**

```bash
dotnet test --filter "FullyQualifiedName~RetryOperation_RetriesThreeTimes"
```

Confirm:

- Test passes
- Other tests still pass
- Output pristine (no errors, warnings)

**Test fails?** Fix code, not test.

**Other tests fail?** Fix now.

### REFACTOR - Clean Up

After green only:

- Remove duplication
- Improve names
- Extract helpers

Keep tests green. Don't add behaviour.

### Repeat

Next failing test for next feature.

## Good Tests

| Quality | Good | Bad |
|---------|------|-----|
| **Minimal** | One thing. "And" in name? Split it. | `ValidatesEmailAndDomainAndWhitespace` |
| **Clear** | Name describes behaviour | `Test1`, `MyTest` |
| **Shows intent** | Demonstrates desired API | Obscures what code should do |
| **AAA Pattern** | Clear Arrange/Act/Assert sections | Mixed setup and assertions |

## xUnit 3 Test Naming Convention

```csharp
// Pattern: MethodName_Scenario_ExpectedBehavior
[Fact]
public async Task Handle_WithValidCommand_ReturnsSuccess()

[Fact]
public async Task Handle_WithDuplicateNumber_ReturnsInvalidResult()

[Fact]
public void Email_WithEmptyValue_ThrowsArgumentException()

// For Theory with multiple cases
[Theory]
[InlineData("")]
[InlineData(" ")]
[InlineData(null)]
public void Email_WithInvalidValue_ThrowsArgumentException(string? email)
```

## Why Order Matters

**"I'll write tests after to verify it works"**

Tests written after code pass immediately. Passing immediately proves nothing:

- Might test wrong thing
- Might test implementation, not behaviour
- Might miss edge cases you forgot
- You never saw it catch the bug

Test-first forces you to see the test fail, proving it actually tests something.

**"I already manually tested all the edge cases"**

Manual testing is ad-hoc. You think you tested everything but:

- No record of what you tested
- Can't re-run when code changes
- Easy to forget cases under pressure
- "It worked when I tried it" ≠ comprehensive

Automated tests are systematic. They run the same way every time.

**"Deleting X hours of work is wasteful"**

Sunk cost fallacy. The time is already gone. Your choice now:

- Delete and rewrite with TDD (X more hours, high confidence)
- Keep it and add tests after (30 min, low confidence, likely bugs)

The "waste" is keeping code you can't trust. Working code without real tests is technical debt.

**"TDD is dogmatic, being pragmatic means adapting"**

TDD IS pragmatic:

- Finds bugs before commit (faster than debugging after)
- Prevents regressions (tests catch breaks immediately)
- Documents behaviour (tests show how to use code)
- Enables refactoring (change freely, tests catch breaks)

"Pragmatic" shortcuts = debugging in production = slower.

**"Tests after achieve the same goals - it's spirit not ritual"**

No. Tests-after answer "What does this do?" Tests-first answer "What should this do?"

Tests-after are biased by your implementation. You test what you built, not what's required. You verify remembered edge cases, not discovered ones.

Tests-first force edge case discovery before implementing. Tests-after verify you remembered everything (you didn't).

30 minutes of tests after ≠ TDD. You get coverage, lose proof tests work.

## Common Rationalizations

| Excuse | Reality |
|--------|---------|
| "Too simple to test" | Simple code breaks. Test takes 30 seconds. |
| "I'll test after" | Tests passing immediately prove nothing. |
| "Tests after achieve same goals" | Tests-after = "what does this do?" Tests-first = "what should this do?" |
| "Already manually tested" | Ad-hoc ≠ systematic. No record, can't re-run. |
| "Deleting X hours is wasteful" | Sunk cost fallacy. Keeping unverified code is technical debt. |
| "Keep as reference, write tests first" | You'll adapt it. That's testing after. Delete means delete. |
| "Need to explore first" | Fine. Throw away exploration, start with TDD. |
| "Test hard = design unclear" | Listen to test. Hard to test = hard to use. |
| "TDD will slow me down" | TDD faster than debugging. Pragmatic = test-first. |
| "Manual test faster" | Manual doesn't prove edge cases. You'll re-test every change. |
| "Existing code has no tests" | You're improving it. Add tests for existing code. |

## Red Flags - STOP and Start Over

- Code before test
- Test after implementation
- Test passes immediately
- Can't explain why test failed
- Tests added "later"
- Rationalising "just this once"
- "I already manually tested it"
- "Tests after achieve the same purpose"
- "It's about spirit not ritual"
- "Keep as reference" or "adapt existing code"
- "Already spent X hours, deleting is wasteful"
- "TDD is dogmatic, I'm being pragmatic"
- "This is different because..."

**All of these mean: Delete code. Start over with TDD.**

## Example: Bug Fix (Email Validation)

**Bug:** Empty email accepted in CreateRoomCommand

**RED:**

```csharp
[Fact]
public async Task Handle_WithEmptyEmail_ReturnsValidationError()
{
    // Arrange
    var command = new InviteMemberCommand(
        Email: string.Empty,
        RoleId: _orgUserRoleId);
    var handler = new InviteMemberHandler(_dbContext, _emailService);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("Email"));
}
```

**Verify RED:**

```bash
$ dotnet test --filter "Handle_WithEmptyEmail"
Failed Handle_WithEmptyEmail_ReturnsValidationError
  Expected result.IsSuccess to be False, but found True.
```

**GREEN:**

```csharp
public class InviteMemberHandler : ICommandHandler<InviteMemberCommand, Result>
{
    public async Task<Result> Handle(InviteMemberCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return Result.Invalid(new ValidationError("Email is required"));
        }

        // ... rest of implementation
    }
}
```

**Verify GREEN:**

```bash
$ dotnet test --filter "Handle_WithEmptyEmail"
Passed Handle_WithEmptyEmail_ReturnsValidationError
```

**REFACTOR:**
Extract validation to FluentValidation validator for consistency.

```csharp
public class InviteMemberCommandValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required");
    }
}
```

## Example: Domain Entity TDD

**Feature:** Room must have positive area

**RED:**

```csharp
[Fact]
public void SetArea_WithNegativeValue_ThrowsArgumentOutOfRangeException()
{
    // Arrange
    var room = new Room("101", "Meeting Room", _projectId);

    // Act
    var act = () => room.SetArea(-5.0m);

    // Assert
    act.Should().Throw<ArgumentOutOfRangeException>()
        .WithMessage("*Area*cannot be negative*");
}
```

**GREEN:**

```csharp
public void SetArea(decimal? area)
{
    if (area < 0)
    {
        throw new ArgumentOutOfRangeException(nameof(area), "Area cannot be negative");
    }

    AreaRequired = area;
}
```

## Example: Blazor Component TDD with bUnit

**Feature:** Room list shows "No rooms" when empty

**RED:**

```csharp
[Fact]
public void RoomList_WithNoRooms_ShowsEmptyMessage()
{
    // Arrange
    using var ctx = new TestContext();
    ctx.Services.AddSyncfusionBlazor();

    var queryMediator = Substitute.For<IQueryMediator>();
    queryMediator.Send(Arg.Any<GetRoomsQuery>(), Arg.Any<CancellationToken>())
        .Returns(Result<IReadOnlyList<RoomDto>>.Success(new List<RoomDto>()));
    ctx.Services.AddSingleton(queryMediator);

    // Act
    var cut = ctx.RenderComponent<RoomList>(parameters =>
        parameters.Add(p => p.ProjectId, Guid.NewGuid()));

    // Assert
    cut.Find(".empty-state").TextContent.Should().Contain("No rooms found");
}
```

**GREEN:**

```razor
@if (!_rooms.Any())
{
    <span class="empty-state">No rooms found</span>
}
else
{
    <SfGrid TValue="RoomDto" DataSource="_rooms">
        @* ... *@
    </SfGrid>
}
```

## Verification Checklist

Before marking work complete:

- [ ] Every new function/method has a test
- [ ] Watched each test fail before implementing
- [ ] Each test failed for expected reason (feature missing, not typo)
- [ ] Wrote minimal code to pass each test
- [ ] All tests pass (`dotnet test`)
- [ ] Output pristine (no errors, warnings)
- [ ] Tests use real code (mocks only if unavoidable)
- [ ] Edge cases and errors covered
- [ ] Follows AAA pattern (Arrange-Act-Assert)

Can't check all boxes? You skipped TDD. Start over.

## When Stuck

| Problem | Solution |
|---------|----------|
| Don't know how to test | Write wished-for API. Write assertion first. Ask your human partner. |
| Test too complicated | Design too complicated. Simplify interface. |
| Must mock everything | Code too coupled. Use dependency injection via constructor. |
| Test setup huge | Extract to test fixtures. Still complex? Simplify design. |
| EF Core queries hard to test | Use Testcontainers PostgreSQL or in-memory provider. |

## Debugging Integration

Bug found? Write failing test reproducing it. Follow TDD cycle. Test proves fix and prevents regression.

Never fix bugs without a test.

## Final Rule

```text
Production code → test exists and failed first
Otherwise → not TDD
```

No exceptions without your human partner's permission.

## Skill References

| Situation | Skill |
|-----------|-------|
| xUnit/NSubstitute/Testcontainers patterns | `Radberi-UnitTesting` |
| Syncfusion Blazor component testing | `Radberi-BlazorTesting` |
| Avoiding testing anti-patterns | `Radberi-TestingAntiPatterns` |
| Flaky test elimination | `Radberi-ConditionBasedWaitingCSharp` |
