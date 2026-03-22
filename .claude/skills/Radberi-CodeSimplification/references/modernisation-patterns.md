# Modern C# Patterns (C# 8–14 / .NET 10)

Comprehensive before/after examples for modern C# patterns with semantic cautions and edge cases. Covers features from C# 8 (null-coalescing assignment) through C# 14 (field keyword), grouped by pattern rather than language version.

---

## 1. Primary Constructors

### Basic Dependency Injection

**Before:**
```csharp
public class RoomService
{
    private readonly IRoomRepository _repository;
    private readonly ILogger<RoomService> _logger;
    private readonly IMapper _mapper;

    public RoomService(
        IRoomRepository repository,
        ILogger<RoomService> logger,
        IMapper mapper)
    {
        _repository = repository;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<Room?> GetByIdAsync(Guid id)
    {
        _logger.LogDebug("Fetching room {RoomId}", id);
        return await _repository.GetByIdAsync(id);
    }
}
```

**After:**
```csharp
public class RoomService(
    IRoomRepository repository,
    ILogger<RoomService> logger,
    IMapper mapper)
{
    public async Task<Room?> GetByIdAsync(Guid id)
    {
        logger.LogDebug("Fetching room {RoomId}", id);
        return await repository.GetByIdAsync(id);
    }
}
```

**Lines saved:** 12 → 8 (33% reduction)

### SEMANTIC CAUTION: Mutability

Primary constructor parameters are **mutable**. This is safe:

```csharp
// Safe - parameters only read, never reassigned
public class SafeService(IRepository repo)
{
    public void DoWork() => repo.Save();
}
```

This is NOT equivalent:

```csharp
// Original - readonly prevents accidental reassignment
private readonly IRepository _repo;

// Primary constructor - parameter can be reassigned (BAD)
public class UnsafeService(IRepository repo)
{
    public void BadMethod()
    {
        repo = null; // Compiles! Original would not.
    }
}
```

**Decision rule:** If any code path reassigns the field, or thread safety relies on readonly, keep the original pattern.

### With Field Initialisation

**Before:**
```csharp
public class CacheService
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _defaultExpiry;

    public CacheService(IMemoryCache cache, CacheOptions options)
    {
        _cache = cache;
        _defaultExpiry = options.DefaultExpiry;
    }
}
```

**After:**
```csharp
public class CacheService(IMemoryCache cache, CacheOptions options)
{
    private readonly TimeSpan _defaultExpiry = options.DefaultExpiry;

    // 'cache' available directly, '_defaultExpiry' is readonly field
}
```

**Hybrid approach:** Use primary constructor for DI but create readonly fields when immutability is required.

---

## 2. Collection Expressions

### Basic Initialisation

**Before:**
```csharp
var names = new List<string> { "Alice", "Bob", "Charlie" };
var numbers = new int[] { 1, 2, 3, 4, 5 };
var empty = Array.Empty<string>();
var set = new HashSet<int> { 1, 2, 3 };
```

**After:**
```csharp
List<string> names = ["Alice", "Bob", "Charlie"];
int[] numbers = [1, 2, 3, 4, 5];
string[] empty = [];
HashSet<int> set = [1, 2, 3];
```

**Note:** Type must be explicit on the left side for inference.

### Spread Operator

**Before:**
```csharp
var allItems = existingItems.Concat(newItems).ToList();
var combined = first.ToList();
combined.AddRange(second);
combined.AddRange(third);
```

**After:**
```csharp
List<int> allItems = [..existingItems, ..newItems];
List<int> combined = [..first, ..second, ..third];
```

### Replacing ToList/ToArray

**Before:**
```csharp
var filtered = items.Where(x => x.IsActive).ToList();
var mapped = items.Select(x => x.Name).ToArray();
```

**After:**
```csharp
List<Item> filtered = [..items.Where(x => x.IsActive)];
string[] mapped = [..items.Select(x => x.Name)];
```

### SEMANTIC CAUTION: Type Inference

```csharp
// This works - explicit target type
List<object> items = [1, "two", 3.0];

// This may fail or infer unexpectedly
var items = [1, "two", 3.0]; // What type is this?
```

**Decision rule:** Always specify the collection type explicitly when using collection expressions.

---

## 3. Records for DTOs

### Simple DTO Conversion

**Before:**
```csharp
public class RoomDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Area { get; set; }
    public string ProjectNumber { get; set; } = string.Empty;

    public override bool Equals(object? obj)
    {
        return obj is RoomDto other &&
               Id == other.Id &&
               Name == other.Name &&
               Area == other.Area &&
               ProjectNumber == other.ProjectNumber;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, Area, ProjectNumber);
    }
}
```

**After:**
```csharp
public record RoomDto(
    Guid Id,
    string Name,
    decimal Area,
    string ProjectNumber);
```

**Lines saved:** 22 → 4 (82% reduction)

### Record with Optional Properties

**Before:**
```csharp
public class CreateRoomCommand
{
    public string Name { get; init; } = string.Empty;
    public decimal Area { get; init; }
    public string? Description { get; init; }
}
```

**After:**
```csharp
public record CreateRoomCommand(
    string Name,
    decimal Area,
    string? Description = null);
```

### Immutable Record with Mutation

```csharp
public record RoomDto(Guid Id, string Name, decimal Area);

// Create modified copy (original unchanged)
var updated = original with { Name = "New Name" };
```

### SEMANTIC CAUTION: Value Equality

```csharp
// Class - reference equality
var dto1 = new RoomDto { Id = id, Name = "A" };
var dto2 = new RoomDto { Id = id, Name = "A" };
dto1 == dto2 // FALSE (different references)

// Record - value equality
var rec1 = new RoomRecord(id, "A");
var rec2 = new RoomRecord(id, "A");
rec1 == rec2 // TRUE (same values)
```

**Decision rules:**
- Convert if class is pure DTO (no methods)
- Convert if equality by value is desired
- DO NOT convert if code relies on reference equality (e.g., dictionary keys, object tracking)

### Record Class vs Record Struct

```csharp
// Reference type (heap allocated)
public record RoomDto(Guid Id, string Name);

// Value type (stack allocated, copied by value)
public record struct PointDto(int X, int Y);
```

Use `record struct` for small, frequently-created value types.

---

## 4. File-Scoped Namespaces

### Standard Conversion

**Before:**
```csharp
// <copyright file="RoomService.cs" company="Radberi">
// Copyright (c) Radberi. All rights reserved.
// </copyright>

namespace SpaceHub.Application.Services
{
    public class RoomService
    {
        private readonly IRoomRepository _repository;

        public RoomService(IRoomRepository repository)
        {
            _repository = repository;
        }

        public async Task<Room?> GetByIdAsync(Guid id)
        {
            return await _repository.GetByIdAsync(id);
        }
    }
}
```

**After:**
```csharp
// <copyright file="RoomService.cs" company="Radberi">
// Copyright (c) Radberi. All rights reserved.
// </copyright>

namespace SpaceHub.Application.Services;

public class RoomService
{
    private readonly IRoomRepository _repository;

    public RoomService(IRoomRepository repository)
    {
        _repository = repository;
    }

    public async Task<Room?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }
}
```

**Benefit:** Reduces one level of indentation throughout the file.

### Multiple Types in File

File-scoped namespaces work with multiple types:

```csharp
namespace SpaceHub.Domain.Entities;

public class Room { }

public class RoomConfiguration { }

internal record RoomCreatedEvent(Guid RoomId);
```

---

## 5. Field Keyword (C# 14)

### Property with Validation

**Before:**
```csharp
private string _name = string.Empty;

public string Name
{
    get => _name;
    set => _name = value?.Trim() ?? string.Empty;
}
```

**After:**
```csharp
public string Name
{
    get => field;
    set => field = value?.Trim() ?? string.Empty;
} = string.Empty;
```

### Lazy Initialisation

**Before:**
```csharp
private ExpensiveObject? _expensive;

public ExpensiveObject Expensive
{
    get => _expensive ??= new ExpensiveObject();
}
```

**After:**
```csharp
public ExpensiveObject Expensive
{
    get => field ??= new ExpensiveObject();
}
```

### Validation with Notification

**Before:**
```csharp
private decimal _area;

public decimal Area
{
    get => _area;
    set
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value));
        if (_area != value)
        {
            _area = value;
            OnPropertyChanged();
        }
    }
}
```

**After:**
```csharp
public decimal Area
{
    get => field;
    set
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value));
        if (field != value)
        {
            field = value;
            OnPropertyChanged();
        }
    }
}
```

### SEMANTIC CAUTION: Naming Conflicts

```csharp
public class Problematic
{
    public string Field { get; set; } // Property named "Field"

    public string Name
    {
        get => field; // Refers to backing field, not the property
    }

    public void Method()
    {
        var field = "local"; // Local variable named "field"
        Console.WriteLine(field); // Refers to local, not keyword
    }
}
```

**Disambiguation:**
```csharp
@field    // Escape the keyword
this.field // Explicit reference (doesn't work for keyword)
```

---

## 6. Required Properties

### Constructor Validation to Required

**Before:**
```csharp
public class CreateProjectCommand
{
    public string Name { get; init; }
    public string Number { get; init; }
    public Guid OrganisationId { get; init; }

    public CreateProjectCommand(string name, string number, Guid organisationId)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Number = number ?? throw new ArgumentNullException(nameof(number));
        OrganisationId = organisationId;
    }
}
```

**After:**
```csharp
public class CreateProjectCommand
{
    public required string Name { get; init; }
    public required string Number { get; init; }
    public required Guid OrganisationId { get; init; }
}
```

### Usage Difference

```csharp
// Before - must use constructor
var cmd = new CreateProjectCommand("Name", "NUM-001", orgId);

// After - object initializer enforced at compile time
var cmd = new CreateProjectCommand
{
    Name = "Name",
    Number = "NUM-001",
    OrganisationId = orgId
};
```

---

## 7. Pattern Matching Enhancements

### Switch Expression

**Before:**
```csharp
public string GetStatusDisplay(ProjectStatus status)
{
    switch (status)
    {
        case ProjectStatus.Draft:
            return "Draft";
        case ProjectStatus.Active:
            return "Active";
        case ProjectStatus.OnHold:
            return "On Hold";
        case ProjectStatus.Completed:
            return "Completed";
        case ProjectStatus.Archived:
            return "Archived";
        default:
            return "Unknown";
    }
}
```

**After:**
```csharp
public string GetStatusDisplay(ProjectStatus status) => status switch
{
    ProjectStatus.Draft => "Draft",
    ProjectStatus.Active => "Active",
    ProjectStatus.OnHold => "On Hold",
    ProjectStatus.Completed => "Completed",
    ProjectStatus.Archived => "Archived",
    _ => "Unknown"
};
```

### Tuple Pattern

**Before:**
```csharp
public string GetPermissionLevel(bool isAdmin, bool isOwner)
{
    if (isAdmin && isOwner)
        return "Full";
    else if (isAdmin)
        return "Admin";
    else if (isOwner)
        return "Owner";
    else
        return "User";
}
```

**After:**
```csharp
public string GetPermissionLevel(bool isAdmin, bool isOwner) => (isAdmin, isOwner) switch
{
    (true, true) => "Full",
    (true, false) => "Admin",
    (false, true) => "Owner",
    _ => "User"
};
```

### Property Pattern

**Before:**
```csharp
public decimal CalculateDiscount(Order order)
{
    if (order.Customer.IsPremium && order.Total > 1000)
        return 0.20m;
    else if (order.Customer.IsPremium)
        return 0.10m;
    else if (order.Total > 1000)
        return 0.05m;
    else
        return 0m;
}
```

**After:**
```csharp
public decimal CalculateDiscount(Order order) => order switch
{
    { Customer.IsPremium: true, Total: > 1000 } => 0.20m,
    { Customer.IsPremium: true } => 0.10m,
    { Total: > 1000 } => 0.05m,
    _ => 0m
};
```

---

## 8. Null-Conditional and Coalescing

### Null-Conditional Assignment

**Before:**
```csharp
if (_cache == null)
{
    _cache = new Dictionary<string, object>();
}
```

**After:**
```csharp
_cache ??= new Dictionary<string, object>();
```

### Null-Conditional Access Chain

**Before:**
```csharp
string? city = null;
if (person != null && person.Address != null)
{
    city = person.Address.City;
}
```

**After:**
```csharp
var city = person?.Address?.City;
```

### Null-Coalescing with Throw

**Before:**
```csharp
if (name == null)
{
    throw new ArgumentNullException(nameof(name));
}
_name = name;
```

**After:**
```csharp
_name = name ?? throw new ArgumentNullException(nameof(name));
```

---

## 9. Target-Typed New

### Field Declarations

**Before:**
```csharp
private readonly List<Room> _rooms = new List<Room>();
private readonly Dictionary<Guid, Project> _projectCache = new Dictionary<Guid, Project>();
private readonly ConcurrentQueue<Message> _queue = new ConcurrentQueue<Message>();
```

**After:**
```csharp
private readonly List<Room> _rooms = new();
private readonly Dictionary<Guid, Project> _projectCache = new();
private readonly ConcurrentQueue<Message> _queue = new();
```

### In Expressions

**Before:**
```csharp
return new Result<Room>(room);
throw new InvalidOperationException("Not found");
```

**After:**
```csharp
return new(room);
throw new InvalidOperationException("Not found"); // Keep for clarity
```

**Note:** Keep explicit type for exceptions and unclear contexts.

---

## Pattern Application Checklist

Before applying each pattern, verify:

- [ ] **Primary constructors:** No readonly requirements, no field reassignment
- [ ] **Records:** No behaviour, value equality appropriate, no reference equality dependence
- [ ] **Collection expressions:** Target type is explicit
- [ ] **Field keyword:** No existing `field` variable in scope
- [ ] **Required properties:** Object initialiser pattern acceptable
- [ ] **Pattern matching:** Exhaustive matching, no nested ternaries

---

## See Also

- `deduplication-strategies.md` - Code extraction patterns
- `clarity-patterns.md` - Readability improvements
