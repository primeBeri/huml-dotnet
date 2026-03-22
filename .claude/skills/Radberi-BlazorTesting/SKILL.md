---
name: Radberi-BlazorTesting
description: Use when writing unit or integration tests for Blazor components - provides patterns for Syncfusion Blazor testing with bUnit, SfGrid, SfDialog, EditForm, and authorisation testing
---

# Blazor Component Testing with bUnit and Syncfusion

## Overview

Testing Syncfusion Blazor components requires specific setup. This skill covers:

- bUnit test setup with Syncfusion services
- SfGrid data grid testing
- EditForm validation testing
- SfDialog interaction testing
- ISnackbar notification verification
- Authorisation testing with `AuthorizeView`

## bUnit Setup for Syncfusion

### Base Test Class (Key Patterns)

```csharp
public abstract class BunitTestBase : BunitContext
{
    protected IQueryMediator QueryMediator { get; private set; } = null!;
    protected ICommandMediator CommandMediator { get; private set; } = null!;
    protected ISnackbar Snackbar { get; private set; } = null!;

    protected BunitTestBase()
    {
        // CRITICAL: Syncfusion components make heavy JS interop calls
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Register Syncfusion services (replaces AddMudServices)
        Services.AddSyncfusionBlazor();

        // Mock mediators and services
        QueryMediator = Substitute.For<IQueryMediator>();
        CommandMediator = Substitute.For<ICommandMediator>();
        Snackbar = Substitute.For<ISnackbar>();

        Services.AddSingleton(QueryMediator);
        Services.AddSingleton(CommandMediator);
        Services.AddSingleton(Snackbar);
    }

    protected void SetupAuthorization(params string[] policies)
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("test-user");
        foreach (var policy in policies)
            authContext.SetPolicies(policy);
    }

    protected async Task WaitForStateAsync(IRenderedComponent<dynamic> cut, int ms = 100)
    {
        await Task.Delay(ms);
        cut.Render();
    }
}
```

### InternalsVisibleTo for NSubstitute

When mocking internal interfaces (like `ISnackbar`), add to your Web project's csproj:

```xml
<ItemGroup>
  <InternalsVisibleTo Include="SpaceHub.Web.Tests" />
  <InternalsVisibleTo Include="DynamicProxyGenAssembly2" />
</ItemGroup>
```

## SfGrid Testing

### Component Lookup and Data Verification

```csharp
[Fact]
public async Task RoomList_DisplaysAllRooms()
{
    // Arrange
    QueryMediator.SendAsync(Arg.Any<GetRoomsQuery>(), Arg.Any<CancellationToken>())
        .Returns(Result<IReadOnlyList<RoomDto>>.Success(_testRooms));
    SetupAuthorization(PolicyNames.Rooms.CanView);

    // Act
    var cut = RenderWithProviders<RoomList>(p => p.Add(x => x.ProjectId, TestProjectId));
    await WaitForStateAsync(cut);

    // Assert - find SfGrid component directly
    var grid = cut.FindComponent<SfGrid<RoomDto>>();
    grid.Should().NotBeNull();

    // Verify data via markup (Syncfusion renders differently than MudBlazor)
    cut.Markup.Should().Contain("R001");
    cut.Markup.Should().Contain("Meeting Room");
}
```

## EditForm Validation Testing

Use standard `EditForm` with `DataAnnotationsValidator` (not MudForm):

```csharp
[Fact]
public async Task RoomForm_Submit_CallsCommandMediator()
{
    // Arrange - fill form fields
    var cut = RenderWithProviders<RoomForm>(p => p.Add(x => x.Model, _model));

    cut.Find("input[aria-label='Room Number']").Change("R001");
    cut.Find("input[aria-label='Room Name']").Change("Meeting Room");

    // Act - submit form
    await cut.Instance.SubmitAsync();

    // Assert
    await CommandMediator.Received(1).SendAsync(
        Arg.Is<CreateRoomCommand>(c => c.Number == "R001"),
        Arg.Any<CancellationToken>());
}
```

**Validation pattern:** Use `EditContext.Validate()` or form submission to trigger validation, then check markup for error messages.

## SfDialog Testing

Syncfusion dialogs expose instance properties directly - simpler than MudDialog's cascading parameters:

```csharp
[Fact]
public void ConfirmDialog_Renders_WithCorrectProperties()
{
    var cut = Render<SfConfirmDialog>(p => p
        .Add(x => x.Title, "Delete Room")
        .Add(x => x.Message, "Are you sure?"));

    var dialog = cut.FindComponent<SfDialog>();

    // Test via Instance properties (no DialogResult pattern)
    dialog.Instance.IsModal.Should().BeTrue();
    dialog.Instance.ShowCloseIcon.Should().BeTrue();
    dialog.Instance.Width.Should().Be("400px");
}

[Fact]
public async Task ConfirmDialog_Confirm_InvokesCallback()
{
    bool? result = null;
    var cut = Render<SfConfirmDialog>(p => p
        .Add(x => x.OnClose, EventCallback.Factory.Create<bool>(this, r => result = r)));

    // Find button by text content (Syncfusion pattern)
    var confirmBtn = cut.FindAll("button")
        .First(b => b.TextContent.Contains("Confirm"));
    await confirmBtn.ClickAsync(new MouseEventArgs());

    result.Should().BeTrue();
}
```

**Dialog control:**
- Use `dialog.Instance.Visible` for visibility testing
- Use `await dialog.Instance.ShowAsync()` / `HideAsync()` for control
- Use `EventCallback<bool>` for results (not `DialogResult`)

## ISnackbar Testing

SpaceHub uses a custom `ISnackbar` abstraction (internal):

```csharp
[Fact]
public async Task CreateRoom_Success_ShowsSnackbar()
{
    CommandMediator.SendAsync(Arg.Any<CreateRoomCommand>(), Arg.Any<CancellationToken>())
        .Returns(Result<RoomDto>.Success(_createdRoom));

    var cut = RenderWithProviders<RoomForm>(p => p.Add(x => x.Model, _model));
    await cut.Instance.SubmitAsync();

    // Verify snackbar was called (NSubstitute syntax)
    Snackbar.Received(1).Show(
        Arg.Is<string>(msg => msg.Contains("success")),
        Arg.Any<Severity>());
}
```

## Authorisation Testing

Authorisation patterns are unchanged from MudBlazor:

```csharp
[Fact]
public async Task RoomList_WithCreatePermission_ShowsAddButton()
{
    SetupAuthorization(PolicyNames.Rooms.CanView, PolicyNames.Rooms.CanCreate);
    var cut = RenderWithProviders<RoomList>(p => p.Add(x => x.ProjectId, TestProjectId));
    await WaitForStateAsync(cut);

    cut.Markup.Should().Contain("Add Room");
}

[Fact]
public async Task RoomList_WithoutCreatePermission_HidesAddButton()
{
    SetupAuthorization(PolicyNames.Rooms.CanView);  // No CanCreate
    var cut = RenderWithProviders<RoomList>(p => p.Add(x => x.ProjectId, TestProjectId));
    await WaitForStateAsync(cut);

    cut.Markup.Should().NotContain("Add Room");
}
```

## Syncfusion Testing Quirks

### 1. JSInterop Loose Mode (Critical)

```csharp
JSInterop.Mode = JSRuntimeMode.Loose;
```

Syncfusion components make heavy JS interop calls. Without loose mode, tests fail with unregistered JS calls.

### 2. SfMenu Collapsed Submenus

SfMenu renders submenus collapsed by default:
- Parent menu items are visible with `aria-label`
- Submenus have `aria-expanded="false"`
- **Test parent visibility only**, not collapsed submenu text

```csharp
// ✅ Correct - test parent menu item
cut.Markup.Should().Contain("Organisation");
cut.Markup.Should().Contain("aria-label=\"Organisation\"");

// ❌ Wrong - submenu text not in DOM when collapsed
cut.Markup.Should().Contain("Settings");  // Fails
```

### 3. Button Interactions

Find buttons by text content (Syncfusion doesn't always use aria-label):

```csharp
var deleteBtn = cut.FindAll("button")
    .First(b => b.TextContent.Contains("Delete"));
```

Or use `title` attribute for icon buttons:
```csharp
cut.Markup.Should().Contain("title=\"Delete\"");
```

## Quick Reference

| Scenario | Test Pattern |
|----------|--------------|
| Grid display | `cut.FindComponent<SfGrid<T>>()`, verify markup |
| Form validation | `EditContext.Validate()`, check markup for errors |
| Dialog visibility | `dialog.Instance.Visible`, `ShowAsync()`/`HideAsync()` |
| Snackbar | Mock `ISnackbar`, verify `Show()` received |
| Authorisation | `SetupAuthorization()`, check element presence |
| Button click | `FindAll("button").First(b => b.TextContent.Contains(...))` |
| Menu testing | Use `aria-label` attributes for collapsed menus |

## Skill References

| Situation | Skill |
|-----------|-------|
| xUnit patterns and Testcontainers | `unit-testing-xunit` |
| Avoiding mocking anti-patterns | `testing-anti-patterns` |
| Flaky test elimination | `condition-based-waiting-csharp` |
| TDD methodology | `test-driven-development` |
