# Clarity Patterns

Patterns for improving code readability through reduced nesting, meaningful naming, and clean conditional logic.

---

## 1. Guard Clauses (Early Returns)

### Problem: Deeply Nested Conditionals

**Before (arrow code):**
```csharp
public Result<Room> ProcessRoom(Guid id, string name, decimal? area)
{
    if (id != Guid.Empty)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            if (area.HasValue && area.Value > 0)
            {
                var room = _repository.Find(id);
                if (room != null)
                {
                    if (room.IsActive)
                    {
                        room.Name = name;
                        room.Area = area.Value;
                        _repository.Update(room);
                        return Result.Success(room);
                    }
                    else
                    {
                        return Result.Invalid("Room is not active");
                    }
                }
                else
                {
                    return Result.NotFound("Room not found");
                }
            }
            else
            {
                return Result.Invalid("Area must be positive");
            }
        }
        else
        {
            return Result.Invalid("Name is required");
        }
    }
    else
    {
        return Result.Invalid("Invalid room ID");
    }
}
```

**After (guard clauses):**
```csharp
public Result<Room> ProcessRoom(Guid id, string name, decimal? area)
{
    if (id == Guid.Empty)
        return Result.Invalid("Invalid room ID");

    if (string.IsNullOrWhiteSpace(name))
        return Result.Invalid("Name is required");

    if (!area.HasValue || area.Value <= 0)
        return Result.Invalid("Area must be positive");

    var room = _repository.Find(id);
    if (room is null)
        return Result.NotFound("Room not found");

    if (!room.IsActive)
        return Result.Invalid("Room is not active");

    room.Name = name;
    room.Area = area.Value;
    _repository.Update(room);

    return Result.Success(room);
}
```

**Key improvements:**
- Maximum nesting depth: 5 → 0
- Each validation is independent and clear
- Happy path is obvious (last 4 lines)
- Easy to add new validations

---

## 2. Avoiding Nested Ternaries

### CRITICAL: Never Use Nested Ternaries

**Before (UNACCEPTABLE):**
```csharp
var status = isActive
    ? (isVerified
        ? (hasSubscription
            ? "Premium"
            : "Basic")
        : "Pending")
    : "Inactive";

var discount = order.Total > 1000
    ? (customer.IsPremium ? 0.20m : 0.10m)
    : (customer.IsPremium ? 0.10m : 0.05m);
```

**After (switch expression):**
```csharp
var status = (isActive, isVerified, hasSubscription) switch
{
    (true, true, true) => "Premium",
    (true, true, false) => "Basic",
    (true, false, _) => "Pending",
    (false, _, _) => "Inactive"
};

var discount = (order.Total > 1000, customer.IsPremium) switch
{
    (true, true) => 0.20m,
    (true, false) => 0.10m,
    (false, true) => 0.10m,
    (false, false) => 0.05m
};
```

**Alternative (if/else for complex logic):**
```csharp
decimal discount;
if (order.Total > 1000)
{
    discount = customer.IsPremium ? 0.20m : 0.10m;
}
else
{
    discount = customer.IsPremium ? 0.10m : 0.05m;
}
```

### When Single Ternary is Acceptable

```csharp
// OK - simple, flat, obvious
var label = isActive ? "Active" : "Inactive";
var icon = hasError ? "error-icon" : "success-icon";

// OK - null coalescing alternative
var name = user?.Name ?? "Anonymous";
```

---

## 3. Meaningful Naming

### Variables

**Before:**
```csharp
var x = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id);
var temp = x.Area * 10.764;
var result = new RoomDto { Id = x.Id, Sqft = temp };
```

**After:**
```csharp
var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId);
var areaInSquareFeet = room.Area * SquareMetresToSquareFeetRatio;
var roomDto = new RoomDto { Id = room.Id, SquareFeet = areaInSquareFeet };
```

### Methods

**Before:**
```csharp
public bool Check(Room r) => r.Area > 0 && !r.IsDeleted && r.Status == RoomStatus.Active;
public void Process(List<Room> items) { /* ... */ }
public RoomDto Get(Guid x) { /* ... */ }
```

**After:**
```csharp
public bool IsValidActiveRoom(Room room) =>
    room.Area > 0 && !room.IsDeleted && room.Status == RoomStatus.Active;

public void PublishRoomUpdates(List<Room> modifiedRooms) { /* ... */ }

public RoomDto GetRoomById(Guid roomId) { /* ... */ }
```

### Boolean Variables and Methods

**Before:**
```csharp
bool flag = room.Area > 100;
if (CheckRoom(room)) { /* ... */ }
var x = room.Status == RoomStatus.Active;
```

**After:**
```csharp
bool isLargeRoom = room.Area > 100;
if (room.MeetsMinimumRequirements()) { /* ... */ }
var isActive = room.Status == RoomStatus.Active;
```

**Boolean naming conventions:**
- Prefix with `is`, `has`, `can`, `should`, `was`
- Methods: `IsValid()`, `HasPermission()`, `CanEdit()`
- Variables: `isActive`, `hasChildren`, `canDelete`

---

## 4. Removing Obvious Comments

### Comments That Restate Code

**Before:**
```csharp
// Get the room from the database
var room = await _context.Rooms.FindAsync(id);

// Check if the room exists
if (room == null)
{
    // Return a not found result
    return Result.NotFound();
}

// Update the room name
room.Name = command.Name;

// Save changes to the database
await _context.SaveChangesAsync(ct);

// Return success with the updated room
return Result.Success(_mapper.Map<RoomDto>(room));
```

**After:**
```csharp
var room = await _context.Rooms.FindAsync(id);
if (room is null)
    return Result.NotFound();

room.Name = command.Name;
await _context.SaveChangesAsync(ct);

return Result.Success(_mapper.Map<RoomDto>(room));
```

### Comments That SHOULD Be Kept

```csharp
// Multiply by 10.764 to convert square metres to square feet
// (required for US client reporting)
var areaInSquareFeet = room.AreaSquareMetres * 10.764;

// HACK: EF Core doesn't track changes on owned types properly
// See: https://github.com/dotnet/efcore/issues/12345
_context.Entry(room).State = EntityState.Modified;

// WARNING: This query bypasses RLS - only use for admin reports
var allRooms = await _context.Rooms.IgnoreQueryFilters().ToListAsync();
```

**Keep comments that explain:**
- WHY something is done (not WHAT)
- Business rules or domain knowledge
- Workarounds with issue links
- Security or performance implications

---

## 5. Extracting Complex Conditions

### Problem: Long Boolean Expressions

**Before:**
```csharp
if (user.Role == UserRole.Admin ||
    (user.Role == UserRole.Editor && project.OwnerId == user.Id) ||
    (user.Role == UserRole.Viewer && project.Members.Any(m => m.UserId == user.Id && m.CanEdit)))
{
    // Allow edit
}
```

**After:**
```csharp
bool CanUserEditProject(User user, Project project)
{
    if (user.Role == UserRole.Admin)
        return true;

    if (user.Role == UserRole.Editor && project.OwnerId == user.Id)
        return true;

    if (user.Role == UserRole.Viewer)
    {
        var membership = project.Members.FirstOrDefault(m => m.UserId == user.Id);
        return membership?.CanEdit == true;
    }

    return false;
}

// Usage:
if (CanUserEditProject(user, project))
{
    // Allow edit
}
```

### Using Local Functions

```csharp
public Result<RoomDto> ProcessRoom(ProcessRoomCommand command)
{
    bool IsValidArea() => command.Area > 0 && command.Area < 10000;
    bool IsValidName() => !string.IsNullOrWhiteSpace(command.Name) && command.Name.Length <= 100;
    bool HasRequiredPermission() => _currentUser.CanEditRooms || _currentUser.IsAdmin;

    if (!IsValidArea())
        return Result.Invalid("Area must be between 0 and 10,000");

    if (!IsValidName())
        return Result.Invalid("Name is required and must be <= 100 characters");

    if (!HasRequiredPermission())
        return Result.Forbidden("Insufficient permissions");

    // Process...
}
```

---

## 6. Consistent Null Handling

### Use Pattern Matching

**Before:**
```csharp
if (room != null)
{
    // ...
}

if (room == null)
{
    return null;
}
```

**After:**
```csharp
if (room is not null)
{
    // ...
}

if (room is null)
{
    return null;
}
```

### Null-Conditional Chains

**Before:**
```csharp
string? city = null;
if (person != null)
{
    if (person.Address != null)
    {
        city = person.Address.City;
    }
}
```

**After:**
```csharp
var city = person?.Address?.City;
```

### Null-Coalescing for Defaults

**Before:**
```csharp
string name;
if (user.DisplayName != null)
{
    name = user.DisplayName;
}
else
{
    name = user.Email;
}
```

**After:**
```csharp
var name = user.DisplayName ?? user.Email;
```

---

## 7. Method Extraction for Readability

### Problem: Long Methods

**Before:**
```csharp
public async Task<Result<OrderDto>> ProcessOrder(ProcessOrderCommand command, CancellationToken ct)
{
    // Validate order
    if (command.Items.Count == 0)
        return Result.Invalid("Order must have at least one item");

    foreach (var item in command.Items)
    {
        if (item.Quantity <= 0)
            return Result.Invalid($"Invalid quantity for item {item.ProductId}");
    }

    // Check inventory
    foreach (var item in command.Items)
    {
        var product = await _context.Products.FindAsync(item.ProductId, ct);
        if (product is null)
            return Result.NotFound($"Product {item.ProductId} not found");

        if (product.StockQuantity < item.Quantity)
            return Result.Invalid($"Insufficient stock for {product.Name}");
    }

    // Calculate totals
    decimal subtotal = 0;
    foreach (var item in command.Items)
    {
        var product = await _context.Products.FindAsync(item.ProductId, ct);
        subtotal += product!.Price * item.Quantity;
    }

    var tax = subtotal * 0.20m;
    var total = subtotal + tax;

    // Apply discount
    if (command.DiscountCode != null)
    {
        var discount = await _context.Discounts.FirstOrDefaultAsync(d => d.Code == command.DiscountCode, ct);
        if (discount is null)
            return Result.Invalid("Invalid discount code");

        total -= total * discount.Percentage;
    }

    // Create order...
    // 50 more lines...
}
```

**After:**
```csharp
public async Task<Result<OrderDto>> ProcessOrder(ProcessOrderCommand command, CancellationToken ct)
{
    var validationResult = ValidateOrderItems(command.Items);
    if (!validationResult.IsSuccess)
        return validationResult.Map<OrderDto>();

    var inventoryResult = await CheckInventoryAsync(command.Items, ct);
    if (!inventoryResult.IsSuccess)
        return inventoryResult.Map<OrderDto>();

    var totals = await CalculateTotalsAsync(command.Items, ct);

    var discountResult = await ApplyDiscountAsync(totals, command.DiscountCode, ct);
    if (!discountResult.IsSuccess)
        return discountResult.Map<OrderDto>();

    var finalTotal = discountResult.Value;

    return await CreateOrderAsync(command, finalTotal, ct);
}

private Result ValidateOrderItems(IReadOnlyList<OrderItem> items)
{
    if (items.Count == 0)
        return Result.Invalid("Order must have at least one item");

    var invalidItem = items.FirstOrDefault(i => i.Quantity <= 0);
    if (invalidItem is not null)
        return Result.Invalid($"Invalid quantity for item {invalidItem.ProductId}");

    return Result.Success();
}

private async Task<Result> CheckInventoryAsync(IReadOnlyList<OrderItem> items, CancellationToken ct)
{
    foreach (var item in items)
    {
        var product = await _context.Products.FindAsync(item.ProductId, ct);
        if (product is null)
            return Result.NotFound($"Product {item.ProductId} not found");

        if (product.StockQuantity < item.Quantity)
            return Result.Invalid($"Insufficient stock for {product.Name}");
    }

    return Result.Success();
}

// Additional helper methods...
```

---

## 8. LINQ Clarity

### Prefer Method Syntax for Simple Queries

**Both acceptable:**
```csharp
// Query syntax - good for complex joins
var result = from r in rooms
             join p in projects on r.ProjectId equals p.Id
             where r.IsActive
             select new { r.Name, p.Number };

// Method syntax - good for simple chains
var activeRooms = rooms
    .Where(r => r.IsActive)
    .OrderBy(r => r.Name)
    .ToList();
```

### Avoid Over-Chaining

**Before (hard to debug):**
```csharp
var result = rooms
    .Where(r => r.ProjectId == projectId)
    .Where(r => !r.IsDeleted)
    .Select(r => new { r.Id, r.Name, r.Area })
    .Where(x => x.Area > 50)
    .GroupBy(x => x.Name.Substring(0, 1))
    .Select(g => new { Letter = g.Key, Count = g.Count(), TotalArea = g.Sum(x => x.Area) })
    .Where(x => x.Count > 5)
    .OrderByDescending(x => x.TotalArea)
    .Take(10)
    .ToList();
```

**After (clearer with intermediate variables):**
```csharp
var activeRooms = rooms
    .Where(r => r.ProjectId == projectId && !r.IsDeleted)
    .Where(r => r.Area > 50)
    .ToList();

var roomsByFirstLetter = activeRooms
    .GroupBy(r => r.Name[0])
    .Select(g => new
    {
        Letter = g.Key,
        Count = g.Count(),
        TotalArea = g.Sum(r => r.Area)
    })
    .Where(x => x.Count > 5);

var topLetters = roomsByFirstLetter
    .OrderByDescending(x => x.TotalArea)
    .Take(10)
    .ToList();
```

---

## Clarity Checklist

When reviewing code for clarity improvements:

- [ ] **Nesting depth** - Maximum 2-3 levels; use guard clauses
- [ ] **Ternaries** - No nesting; switch for 3+ cases
- [ ] **Naming** - Meaningful, consistent, appropriate length
- [ ] **Comments** - Remove obvious ones; keep WHY explanations
- [ ] **Conditions** - Extract complex boolean logic
- [ ] **Null handling** - Use `is null`/`is not null` pattern
- [ ] **Method length** - Extract logical sections
- [ ] **LINQ chains** - Use intermediate variables for debugging

---

## See Also

- `modernisation-patterns.md` - C# 14 language patterns
- `deduplication-strategies.md` - Code extraction patterns
