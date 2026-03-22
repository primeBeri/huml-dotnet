# De-Duplication Strategies

Patterns for identifying and extracting repeated code across C# codebases, with focus on CQRS handlers, validation, and common operations.

---

## 1. Handler Base Classes

### Problem: Repeated Entity Loading

**Before (repeated in every handler):**
```csharp
public class CreateRoomHandler : ICommandHandler<CreateRoomCommand, Result<RoomDto>>
{
    private readonly ISpaceHubDbContext _context;
    private readonly IMapper _mapper;

    public async Task<Result<RoomDto>> Handle(CreateRoomCommand command, CancellationToken ct)
    {
        var organisation = await _context.Organisations
            .FirstOrDefaultAsync(o => o.Id == command.OrganisationId, ct);

        if (organisation is null)
            return Result<RoomDto>.NotFound($"Organisation {command.OrganisationId} not found");

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == command.ProjectId, ct);

        if (project is null)
            return Result<RoomDto>.NotFound($"Project {command.ProjectId} not found");

        // Actual logic...
    }
}

public class UpdateRoomHandler : ICommandHandler<UpdateRoomCommand, Result<RoomDto>>
{
    // Same entity loading code repeated...
}

public class DeleteRoomHandler : ICommandHandler<DeleteRoomCommand, Result>
{
    // Same entity loading code repeated...
}
```

**After (extracted to base class):**
```csharp
public abstract class ProjectScopedHandler<TCommand, TResult>(
    ISpaceHubDbContext context,
    IMapper mapper)
{
    protected ISpaceHubDbContext Context => context;
    protected IMapper Mapper => mapper;

    protected async Task<Result<Organisation>> GetOrganisationAsync(
        Guid organisationId,
        CancellationToken ct)
    {
        var organisation = await Context.Organisations
            .FirstOrDefaultAsync(o => o.Id == organisationId, ct);

        return organisation is null
            ? Result<Organisation>.NotFound($"Organisation {organisationId} not found")
            : Result.Success(organisation);
    }

    protected async Task<Result<Project>> GetProjectAsync(
        Guid projectId,
        CancellationToken ct)
    {
        var project = await Context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId, ct);

        return project is null
            ? Result<Project>.NotFound($"Project {projectId} not found")
            : Result.Success(project);
    }
}

public class CreateRoomHandler(ISpaceHubDbContext context, IMapper mapper)
    : ProjectScopedHandler<CreateRoomCommand, Result<RoomDto>>(context, mapper),
      ICommandHandler<CreateRoomCommand, Result<RoomDto>>
{
    public async Task<Result<RoomDto>> Handle(CreateRoomCommand command, CancellationToken ct)
    {
        var orgResult = await GetOrganisationAsync(command.OrganisationId, ct);
        if (!orgResult.IsSuccess)
            return orgResult.Map<RoomDto>();

        var projectResult = await GetProjectAsync(command.ProjectId, ct);
        if (!projectResult.IsSuccess)
            return projectResult.Map<RoomDto>();

        // Actual logic with orgResult.Value and projectResult.Value...
    }
}
```

---

## 2. Extension Methods for Common Operations

### Problem: Repeated LINQ Patterns

**Before:**
```csharp
// In QueryHandler1
var rooms = await _context.Rooms
    .Where(r => r.ProjectId == projectId)
    .Where(r => !r.IsDeleted)
    .OrderBy(r => r.Number)
    .ToListAsync(ct);

// In QueryHandler2
var rooms = await _context.Rooms
    .Where(r => r.ProjectId == projectId)
    .Where(r => !r.IsDeleted)
    .OrderBy(r => r.Number)
    .Take(10)
    .ToListAsync(ct);

// In QueryHandler3
var count = await _context.Rooms
    .Where(r => r.ProjectId == projectId)
    .Where(r => !r.IsDeleted)
    .CountAsync(ct);
```

**After (extension methods):**
```csharp
public static class RoomQueryExtensions
{
    public static IQueryable<Room> ForProject(this IQueryable<Room> query, Guid projectId)
        => query.Where(r => r.ProjectId == projectId);

    public static IQueryable<Room> Active(this IQueryable<Room> query)
        => query.Where(r => !r.IsDeleted);

    public static IQueryable<Room> Ordered(this IQueryable<Room> query)
        => query.OrderBy(r => r.Number);
}

// Usage becomes:
var rooms = await _context.Rooms
    .ForProject(projectId)
    .Active()
    .Ordered()
    .ToListAsync(ct);

var count = await _context.Rooms
    .ForProject(projectId)
    .Active()
    .CountAsync(ct);
```

---

## 3. Shared Validators

### Problem: Repeated Validation Rules

**Before:**
```csharp
public class CreateRoomValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\s\-]+$").WithMessage("Name contains invalid characters");

        RuleFor(x => x.Number)
            .NotEmpty().WithMessage("Number is required")
            .MaximumLength(20).WithMessage("Number cannot exceed 20 characters")
            .Matches(@"^[A-Z]{1,3}-\d{3}$").WithMessage("Number must be in format XXX-000");
    }
}

public class UpdateRoomValidator : AbstractValidator<UpdateRoomCommand>
{
    public UpdateRoomValidator()
    {
        // Same rules repeated...
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\s\-]+$").WithMessage("Name contains invalid characters");
    }
}
```

**After (shared rules):**
```csharp
public static class RoomValidationRules
{
    public static IRuleBuilderOptions<T, string> ValidRoomName<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9\s\-]+$").WithMessage("Name contains invalid characters");
    }

    public static IRuleBuilderOptions<T, string> ValidRoomNumber<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Number is required")
            .MaximumLength(20).WithMessage("Number cannot exceed 20 characters")
            .Matches(@"^[A-Z]{1,3}-\d{3}$").WithMessage("Number must be in format XXX-000");
    }
}

public class CreateRoomValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomValidator()
    {
        RuleFor(x => x.Name).ValidRoomName();
        RuleFor(x => x.Number).ValidRoomNumber();
    }
}

public class UpdateRoomValidator : AbstractValidator<UpdateRoomCommand>
{
    public UpdateRoomValidator()
    {
        RuleFor(x => x.Name).ValidRoomName();
    }
}
```

---

## 4. Result Mapping Helpers

### Problem: Repeated Result Transformation

**Before:**
```csharp
public async Task<Result<RoomDto>> Handle(GetRoomQuery query, CancellationToken ct)
{
    var room = await _context.Rooms.FindAsync(query.Id, ct);

    if (room is null)
        return Result<RoomDto>.NotFound($"Room {query.Id} not found");

    var dto = _mapper.Map<RoomDto>(room);
    return Result.Success(dto);
}

public async Task<Result<ProjectDto>> Handle(GetProjectQuery query, CancellationToken ct)
{
    var project = await _context.Projects.FindAsync(query.Id, ct);

    if (project is null)
        return Result<ProjectDto>.NotFound($"Project {query.Id} not found");

    var dto = _mapper.Map<ProjectDto>(project);
    return Result.Success(dto);
}
```

**After (extension method):**
```csharp
public static class ResultExtensions
{
    public static async Task<Result<TDto>> FindAndMapAsync<TEntity, TDto>(
        this DbSet<TEntity> dbSet,
        Guid id,
        IMapper mapper,
        CancellationToken ct)
        where TEntity : class
    {
        var entity = await dbSet.FindAsync([id], ct);

        if (entity is null)
            return Result<TDto>.NotFound($"{typeof(TEntity).Name} {id} not found");

        var dto = mapper.Map<TDto>(entity);
        return Result.Success(dto);
    }
}

// Usage:
public async Task<Result<RoomDto>> Handle(GetRoomQuery query, CancellationToken ct)
    => await _context.Rooms.FindAndMapAsync<Room, RoomDto>(query.Id, _mapper, ct);

public async Task<Result<ProjectDto>> Handle(GetProjectQuery query, CancellationToken ct)
    => await _context.Projects.FindAndMapAsync<Project, ProjectDto>(query.Id, _mapper, ct);
```

---

## 5. Specification Pattern for Complex Queries

### Problem: Repeated Complex Filter Logic

**Before:**
```csharp
// Handler 1
var rooms = await _context.Rooms
    .Where(r => r.ProjectId == projectId)
    .Where(r => !r.IsDeleted)
    .Where(r => r.Area >= minArea)
    .Where(r => r.Status == RoomStatus.Active)
    .ToListAsync(ct);

// Handler 2 - slightly different
var rooms = await _context.Rooms
    .Where(r => r.ProjectId == projectId)
    .Where(r => !r.IsDeleted)
    .Where(r => r.Status == RoomStatus.Active)
    .Where(r => r.CreatedAt >= since)
    .ToListAsync(ct);
```

**After (specification pattern):**
```csharp
public abstract class Specification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public static Specification<T> operator &(Specification<T> left, Specification<T> right)
        => new AndSpecification<T>(left, right);
}

public class ActiveRoomsSpec : Specification<Room>
{
    public override Expression<Func<Room, bool>> ToExpression()
        => room => !room.IsDeleted && room.Status == RoomStatus.Active;
}

public class RoomsInProjectSpec(Guid projectId) : Specification<Room>
{
    public override Expression<Func<Room, bool>> ToExpression()
        => room => room.ProjectId == projectId;
}

public class RoomsWithMinAreaSpec(decimal minArea) : Specification<Room>
{
    public override Expression<Func<Room, bool>> ToExpression()
        => room => room.Area >= minArea;
}

// Usage:
var spec = new RoomsInProjectSpec(projectId) & new ActiveRoomsSpec();
var rooms = await _context.Rooms
    .Where(spec.ToExpression())
    .ToListAsync(ct);
```

---

## 6. Mapping Profile Consolidation

### Problem: Repeated Mapping Configurations

**Before:**
```csharp
public class RoomMappingProfile : Profile
{
    public RoomMappingProfile()
    {
        CreateMap<Room, RoomDto>()
            .ForMember(d => d.ProjectNumber, o => o.MapFrom(s => s.Project.Number));

        CreateMap<Room, RoomSummaryDto>()
            .ForMember(d => d.ProjectNumber, o => o.MapFrom(s => s.Project.Number));

        CreateMap<Room, RoomListItemDto>()
            .ForMember(d => d.ProjectNumber, o => o.MapFrom(s => s.Project.Number));
    }
}
```

**After (base mapping):**
```csharp
public class RoomMappingProfile : Profile
{
    public RoomMappingProfile()
    {
        // Base mapping
        CreateMap<Room, RoomBaseDto>()
            .ForMember(d => d.ProjectNumber, o => o.MapFrom(s => s.Project.Number))
            .IncludeAllDerived();

        // Derived mappings inherit base configuration
        CreateMap<Room, RoomDto>();
        CreateMap<Room, RoomSummaryDto>();
        CreateMap<Room, RoomListItemDto>();
    }
}

public record RoomBaseDto
{
    public Guid Id { get; init; }
    public string ProjectNumber { get; init; } = string.Empty;
}

public record RoomDto : RoomBaseDto { /* additional properties */ }
public record RoomSummaryDto : RoomBaseDto { /* subset */ }
public record RoomListItemDto : RoomBaseDto { /* list view */ }
```

---

## 7. Error Handling Consolidation

### Problem: Repeated Try-Catch Patterns

**Before:**
```csharp
public async Task<Result<RoomDto>> Handle(CreateRoomCommand command, CancellationToken ct)
{
    try
    {
        // Logic
        await _context.SaveChangesAsync(ct);
        return Result.Success(dto);
    }
    catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
    {
        if (pgEx.SqlState == "23505") // Unique violation
            return Result<RoomDto>.Conflict("Room with this number already exists");
        throw;
    }
}

// Same pattern in UpdateRoomHandler, CreateProjectHandler, etc.
```

**After (result extension):**
```csharp
public static class DbContextExtensions
{
    public static async Task<Result<T>> SaveChangesAndWrapAsync<T>(
        this ISpaceHubDbContext context,
        T successValue,
        CancellationToken ct)
    {
        try
        {
            await context.SaveChangesAsync(ct);
            return Result.Success(successValue);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
        {
            return pgEx.SqlState switch
            {
                "23505" => Result<T>.Conflict("A record with this identifier already exists"),
                "23503" => Result<T>.Invalid("Referenced record does not exist"),
                _ => throw
            };
        }
    }
}

// Usage:
return await _context.SaveChangesAndWrapAsync(dto, ct);
```

---

## Identification Checklist

When reviewing code for de-duplication opportunities, look for:

- [ ] **Entity loading** - Same FindAsync/FirstOrDefaultAsync patterns
- [ ] **LINQ queries** - Similar Where/OrderBy chains
- [ ] **Validation rules** - Same NotEmpty/MaxLength/Regex patterns
- [ ] **Result wrapping** - Same success/failure patterns
- [ ] **Error handling** - Same try-catch structures
- [ ] **Mapping** - Same ForMember configurations
- [ ] **Null checks** - Same is null / NotFound patterns

## When NOT to De-Duplicate

- **2 occurrences** - Wait for the third before extracting
- **Different contexts** - Similar code that may evolve differently
- **Forced abstraction** - Extraction that requires complex generics
- **Test code** - Some repetition in tests is acceptable for clarity

---

## See Also

- `modernisation-patterns.md` - C# 14 language patterns
- `clarity-patterns.md` - Readability improvements
