---
name: Radberi-DefenceInDepth
description: Use when invalid data causes failures deep in execution, requiring validation at multiple system layers - validates at every layer data passes through to make bugs structurally impossible
---

# Defence-in-Depth Validation (.NET / ASP.NET Core / Clean Architecture)

## Overview

When you fix a bug caused by invalid data, adding validation at one place feels sufficient. But that single check can be bypassed by different code paths, refactoring, or mocks.

**Core principle:** Validate at EVERY layer data passes through. Make the bug structurally impossible.

## Why Multiple Layers

Single validation: "We fixed the bug"
Multiple layers: "We made the bug impossible"

Different layers catch different cases:

- Compile-time catches type errors before running
- Entry validation catches most bugs
- Business logic catches edge cases
- Domain entities enforce invariants
- Infrastructure provides final defence
- Environment guards prevent context-specific dangers
- Observability helps when other layers fail

## The Six Layers

### Layer 0: Compile-Time Defences

**Purpose:** Catch bugs before code runs

```csharp
// Nullable reference types - compiler enforces null checks
#nullable enable
public record CreateRoomCommand(
    string Number,       // Non-nullable = required at compile time
    string Name,
    Guid ProjectId,
    string? Description  // Nullable = explicitly optional
);

// Required keyword (C# 11+) - property must be initialised
public class Room : BaseEntity
{
    public required string Number { get; init; }
    public required string Name { get; init; }
    public required Guid ProjectId { get; init; }
}

// Analysers catch issues at build time
// CA1062: Validate arguments of public methods
// CA2208: Instantiate argument exceptions correctly
// SA1611: Element parameters should be documented
```

**Key C# features:**

- `#nullable enable` - Compiler warns on potential null dereference
- `required` keyword - Must be set during initialisation
- `init` accessors - Immutable after construction
- Code analysers - StyleCop, Meziantou, SonarAnalyzer

### Layer 1: API Boundary Validation

**Purpose:** Reject obviously invalid input at HTTP/SignalR boundary

```csharp
// Minimal API with FluentValidation
app.MapPost("/api/v1/projects/{projectId}/rooms",
    async (
        Guid projectId,
        CreateRoomRequest request,
        IValidator<CreateRoomRequest> validator,
        ICommandMediator mediator) =>
{
    var validation = await validator.ValidateAsync(request);
    if (!validation.IsValid)
    {
        return Results.ValidationProblem(validation.ToDictionary());
    }

    var command = new CreateRoomCommand(
        request.Number,
        request.Name,
        projectId,
        request.Description,
        request.AreaRequired);

    var result = await mediator.SendAsync(command);
    return result.ToMinimalApiResult();
})
.RequireAuthorization("CanCreateRooms")
.WithName("CreateRoom");

// Data Annotations on request DTOs (defence layer 1a)
public record CreateRoomRequest
{
    [Required(ErrorMessage = "Room number is required")]
    [StringLength(50, ErrorMessage = "Room number cannot exceed 50 characters")]
    public required string Number { get; init; }

    [Required(ErrorMessage = "Room name is required")]
    [StringLength(200, ErrorMessage = "Room name cannot exceed 200 characters")]
    public required string Name { get; init; }

    [StringLength(2000)]
    public string? Description { get; init; }

    [Range(0, double.MaxValue, ErrorMessage = "Area must be non-negative")]
    public decimal? AreaRequired { get; init; }
}
```

### Layer 2: Application Layer Validation

**Purpose:** Validate business rules in CQRS handlers

```csharp
// FluentValidation in LiteBus pipeline
public class CreateRoomCommandValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomCommandValidator(ISpaceHubDbContext dbContext)
    {
        RuleFor(x => x.Number)
            .NotEmpty().WithMessage("Room number is required")
            .MaximumLength(50).WithMessage("Room number cannot exceed 50 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Room name is required")
            .MaximumLength(200).WithMessage("Room name cannot exceed 200 characters");

        RuleFor(x => x.AreaRequired)
            .GreaterThanOrEqualTo(0)
            .When(x => x.AreaRequired.HasValue)
            .WithMessage("Area cannot be negative");

        // Business rule: project must exist
        RuleFor(x => x.ProjectId)
            .MustAsync(async (id, ct) =>
                await dbContext.Projects.AnyAsync(p => p.Id == id, ct))
            .WithMessage("Project does not exist");

        // Business rule: room number must be unique within project
        RuleFor(x => x)
            .MustAsync(async (cmd, ct) =>
                !await dbContext.Rooms.AnyAsync(
                    r => r.ProjectId == cmd.ProjectId && r.Number == cmd.Number, ct))
            .WithMessage("Room number already exists in this project");
    }
}

// Guard clauses in handler (defence even if validator is bypassed)
public class CreateRoomHandler : ICommandHandler<CreateRoomCommand, Result<RoomDto>>
{
    public async Task<Result<RoomDto>> Handle(
        CreateRoomCommand command,
        CancellationToken ct)
    {
        // Defensive checks - validator SHOULD catch these, but defence-in-depth
        if (command.ProjectId == Guid.Empty)
        {
            return Result<RoomDto>.Invalid(
                new ValidationError("ProjectId cannot be empty"));
        }

        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == command.ProjectId, ct);

        if (project is null)
        {
            return Result<RoomDto>.NotFound($"Project {command.ProjectId} not found");
        }

        // Tenant check - CRITICAL for multi-tenancy
        if (project.OrganisationId != _tenantContext.OrganisationId)
        {
            // Return NotFound, not Forbidden (don't reveal existence)
            return Result<RoomDto>.NotFound($"Project {command.ProjectId} not found");
        }

        // ... proceed with creation
    }
}
```

### Layer 3: Domain Entity Validation

**Purpose:** Enforce invariants at the domain level - entity can NEVER be invalid

```csharp
public class Room : BaseEntity, IAuditableEntity, ISoftDeletable
{
    private Room() { } // EF Core constructor - bypasses validation

    public Room(string number, string name, Guid projectId)
    {
        // Domain guards - these throw if violated
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("ProjectId cannot be empty", nameof(projectId));
        }

        Id = Uuid.NewDatabaseFriendly(Database.PostgreSql);
        Number = number.Trim();
        Name = name.Trim();
        ProjectId = projectId;
    }

    public string Number { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public Guid ProjectId { get; private set; }
    public decimal? AreaRequired { get; private set; }

    public void SetArea(decimal? area)
    {
        if (area < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(area),
                area,
                "Area cannot be negative");
        }

        AreaRequired = area;
    }

    public void Update(string number, string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number, nameof(number));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        Number = number.Trim();
        Name = name.Trim();
        Description = description?.Trim();
    }
}
```

### Layer 4: Infrastructure/Database Guards

**Purpose:** Final defence at persistence layer

```csharp
// EF Core Fluent API constraints
public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Number)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Description)
            .HasMaxLength(2000);

        builder.Property(r => r.AreaRequired)
            .HasPrecision(18, 3);

        // Database CHECK constraint - final defence
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Room_AreaRequired_NonNegative",
            "\"AreaRequired\" IS NULL OR \"AreaRequired\" >= 0"));

        // Unique constraint on number within project
        builder.HasIndex(r => new { r.ProjectId, r.Number })
            .IsUnique()
            .HasDatabaseName("IX_Room_ProjectId_Number");

        // Global query filter for soft delete
        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}

// Global query filter for multi-tenancy
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Projects filtered by tenant
    modelBuilder.Entity<Project>()
        .HasQueryFilter(p => p.OrganisationId == _tenantContext.OrganisationId);

    // Rooms inherit tenant filter through navigation
    // (EF Core applies filter when joining to Project)
}
```

**PostgreSQL Row-Level Security (ultimate defence):**

```sql
-- Enable RLS on tables
ALTER TABLE "Projects" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Rooms" ENABLE ROW LEVEL SECURITY;

-- Policy: Users can only see their organisation's projects
CREATE POLICY project_tenant_isolation ON "Projects"
    USING ("OrganisationId" = current_setting('app.current_org')::uuid);

-- Policy: Users can only see rooms in their organisation's projects
CREATE POLICY room_tenant_isolation ON "Rooms"
    USING ("ProjectId" IN (
        SELECT "Id" FROM "Projects"
        WHERE "OrganisationId" = current_setting('app.current_org')::uuid
    ));
```

### Layer 5: Environment Guards

**Purpose:** Prevent dangerous operations in specific contexts

```csharp
// Test environment protection
public class FileStorageService : IFileStorageService
{
    private readonly IHostEnvironment _environment;
    private readonly FileStorageOptions _options;

    public async Task DeleteDirectoryAsync(string path)
    {
        var normalizedPath = Path.GetFullPath(path);

        // In tests: NEVER delete outside temp directories
        if (_environment.IsEnvironment("Test"))
        {
            var tempPath = Path.GetFullPath(Path.GetTempPath());
            if (!normalizedPath.StartsWith(tempPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Refusing to delete outside temp directory during tests: {path}");
            }
        }

        // Production: only allow within configured storage root
        var storageRoot = Path.GetFullPath(_options.StorageRoot);
        if (!normalizedPath.StartsWith(storageRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Path is outside storage root: {path}");
        }

        await Task.Run(() => Directory.Delete(normalizedPath, recursive: true));
    }
}

// Multi-tenancy guard service
public class TenantGuard : ITenantGuard
{
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantGuard> _logger;

    public void EnsureSameTenant(Guid resourceOrganisationId)
    {
        if (resourceOrganisationId != _tenantContext.OrganisationId)
        {
            // Log for security audit BEFORE throwing
            _logger.LogWarning(
                "Cross-tenant access attempt detected. " +
                "Resource Org: {ResourceOrg}, Current Org: {CurrentOrg}, User: {UserId}",
                resourceOrganisationId,
                _tenantContext.OrganisationId,
                _tenantContext.UserId);

            // Throw generic exception - don't reveal details
            throw new UnauthorizedAccessException("Access denied");
        }
    }

    public void EnsureSameTenant<T>(T entity) where T : class
    {
        var orgIdProperty = typeof(T).GetProperty("OrganisationId");
        if (orgIdProperty?.GetValue(entity) is Guid orgId)
        {
            EnsureSameTenant(orgId);
        }
    }
}
```

### Layer 6: Observability & Debug

**Purpose:** Capture context for forensics when other layers fail

```csharp
// Structured logging with Serilog enrichment
public class CreateRoomHandler : ICommandHandler<CreateRoomCommand, Result<RoomDto>>
{
    private static readonly ActivitySource ActivitySource = new("SpaceHub.Application");

    public async Task<Result<RoomDto>> Handle(
        CreateRoomCommand command,
        CancellationToken ct)
    {
        // Start distributed trace
        using var activity = ActivitySource.StartActivity("CreateRoom");
        activity?.SetTag("room.number", command.Number);
        activity?.SetTag("room.name", command.Name);
        activity?.SetTag("project.id", command.ProjectId.ToString());
        activity?.SetTag("tenant.id", _tenantContext.OrganisationId.ToString());

        // Structured logging with full context
        _logger.LogDebug(
            "Creating room. Number: {RoomNumber}, Name: {RoomName}, " +
            "ProjectId: {ProjectId}, UserId: {UserId}, TenantId: {TenantId}",
            command.Number,
            command.Name,
            command.ProjectId,
            _currentUser.UserId,
            _tenantContext.OrganisationId);

        try
        {
            // ... implementation
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);

            _logger.LogError(ex,
                "Failed to create room. Number: {RoomNumber}, ProjectId: {ProjectId}",
                command.Number,
                command.ProjectId);

            throw;
        }
    }
}

// Global exception handler captures full context
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An error occurred",
            Instance = httpContext.Request.Path
        };

        // Add trace ID for correlation
        problemDetails.Extensions["traceId"] = Activity.Current?.TraceId.ToString();

        // Log with full context
        Log.Error(exception,
            "Unhandled exception. " +
            "Path: {Path}, Method: {Method}, " +
            "User: {User}, Tenant: {Tenant}, " +
            "TraceId: {TraceId}",
            httpContext.Request.Path,
            httpContext.Request.Method,
            httpContext.User.Identity?.Name ?? "anonymous",
            httpContext.User.FindFirst("org_id")?.Value ?? "none",
            Activity.Current?.TraceId);

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, ct);

        return true;
    }
}
```

## Applying the Pattern

When you find a bug:

1. **Trace the data flow** - Where does bad value originate? Where is it used?
2. **Map all checkpoints** - List every point data passes through
3. **Add validation at each layer** - Compile-time, API, Application, Domain, Infrastructure, Environment, Observability
4. **Test each layer** - Try to bypass layer 1, verify layer 2 catches it

## SpaceHub Multi-Tenancy Example

**Bug:** Room accessed by wrong organisation

**Data flow:**

1. API receives `GET /api/v1/projects/{projectId}/rooms/{roomId}`
2. Handler queries room by ID
3. Room returned without tenant check
4. **BUG:** User from Org A sees Org B's room

**Six layers of defence applied:**

| Layer | Defence | Implementation |
|-------|---------|----------------|
| 0. Compile-time | `OrganisationId` is non-nullable `Guid` | Cannot be null/empty |
| 1. API Boundary | `[Authorize]` attribute | Must be authenticated |
| 2. Application | Handler checks project.OrgId matches | Returns `NotFound` if cross-tenant |
| 3. Domain | `Project.OrganisationId` is required | Invariant enforced in constructor |
| 4. Infrastructure | EF Core Global Query Filter | `.HasQueryFilter(p => p.OrgId == tenant)` |
| 5. Database | PostgreSQL RLS policy | `current_setting('app.current_org')` |
| 6. Observability | Log cross-tenant attempts | Security audit trail |

**Result:** Bug structurally impossible - ALL 6 layers must fail simultaneously.

## Key Insight

All six layers are necessary. During development, each layer catches bugs the others miss:

- Different code paths bypass API validation
- Unit tests with mocks bypass EF Core filters
- Raw SQL queries bypass Global Query Filters
- Edge cases on different environments need environment guards
- Debug logging identifies structural misuse during testing

**Don't stop at one validation point.** Add checks at every layer.

## Quick Reference

| Layer | Purpose | C# Implementation |
|-------|---------|------------------|
| 0. Compile-time | Catch before runtime | `#nullable`, `required`, analysers |
| 1. API Boundary | Reject at HTTP edge | `[Required]`, FluentValidation |
| 2. Application | Business rule checks | Validators, guard clauses, `Result` |
| 3. Domain | Entity invariants | Constructor guards, `ArgumentException` |
| 4. Infrastructure | Database constraints | EF Core config, CHECK, RLS |
| 5. Environment | Context protection | `IHostEnvironment`, path guards |
| 6. Observability | Forensics | Serilog, `Activity`, exception context |

## Red Flags

- "Validation at API level is enough"
- "EF Core will catch that"
- "The database constraint will prevent it"
- "We trust internal code"
- "Only public APIs need validation"
- Skipping domain validation because "it's already checked"
- Removing defence layers during refactoring

## The Bottom Line

**Defence-in-depth isn't paranoia - it's engineering.**

Single points of failure are single points of exploit. When data passes through multiple layers, validate at every layer. The cost is minimal; the protection is multiplicative.

Make bugs structurally impossible.
