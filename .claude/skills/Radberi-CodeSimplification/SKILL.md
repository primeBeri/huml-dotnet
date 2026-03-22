---
name: Radberi-CodeSimplification
description: >-
  Use when the user asks to modernise C# code, update to C# 14 conventions, simplify code, review code,
  reduce code duplication, extract repeated patterns, find repeated code, improve readability, reduce nesting,
  apply guard clauses, or clean up conditionals. Covers three areas: C# 14 modernisation patterns with semantic
  cautions, de-duplication strategies (shared validators, base handlers, extension methods), and clarity patterns
  (guard clauses, switch expressions, naming). Use this skill even if the user only mentions one of these areas —
  the three concerns are interconnected and should be evaluated together.
---

# C# 14/.NET 10 Modernisation Patterns

This skill provides detailed patterns for modernising C# code to take advantage of C# 14 and .NET 10 features while preserving functionality and avoiding semantic pitfalls.

## When to Use This Skill

Load this skill when:

- Modernising legacy C# code to C# 14 idioms
- Applying primary constructors, records, or collection expressions
- Using the new `field` keyword in auto-properties
- Needing detailed before/after examples with semantic cautions

## Core Principles

### Preserve Semantics

Every modernisation pattern has potential semantic implications. Before applying:

1. **Check mutability** - Primary constructor parameters are mutable unlike `readonly` fields
2. **Check equality** - Records use value-based equality, classes use reference equality
3. **Check naming conflicts** - The `field` keyword may conflict with existing variables
4. **Check type inference** - Collection expressions may infer different types

### When NOT to Modernise

Skip modernisation when:

- Original code relies on `readonly` field semantics for thread safety
- Existing code depends on reference equality
- Performance-critical paths where allocations matter
- Legacy code without test coverage (changes can't be verified)

## Pattern Categories

### 1. Primary Constructors

Convert traditional constructor injection to primary constructors for reduced boilerplate.

**Applicability:**

- DI-heavy service classes
- Simple data carriers
- Classes without complex initialisation logic

**Semantic caution:** Parameters become mutable. Skip if code relies on `readonly`.

See `references/modernisation-patterns.md` for complete examples.

### 2. Collection Expressions

Replace verbose collection initialisers with concise syntax.

**Applicability:**

- Array and list initialisations
- Spread operations (`.ToList()`, `.ToArray()`)
- Empty collection creation

**Semantic caution:** Type inference may differ; verify target type.

See `references/modernisation-patterns.md` for complete examples.

### 3. Records for DTOs

Convert data-only classes to records for built-in equality and immutability.

**Applicability:**

- Pure DTOs with no behaviour
- Classes already implementing `Equals`/`GetHashCode`
- Value objects in DDD patterns

**Semantic caution:** Value-based equality changes behaviour if code depends on reference equality.

See `references/modernisation-patterns.md` for complete examples.

### 4. Field Keyword (C# 14)

Eliminate manual backing fields in auto-property accessors.

**Applicability:**

- Properties with custom getter/setter logic
- Validation in setters
- Lazy initialisation patterns

**Semantic caution:** Conflicts with existing variables named `field`. Use `@field` to disambiguate.

See `references/modernisation-patterns.md` for complete examples.

### 5. Pattern Matching Enhancements

Replace switch statements and complex conditionals with pattern matching.

**Applicability:**

- Type checking chains
- Multi-condition branching
- Tuple deconstruction

**CRITICAL:** Never use nested ternaries. Use switch expressions instead.

See `references/modernisation-patterns.md` for complete examples.

## Quick Decision Matrix

| Pattern | Use When | Avoid When |
|---------|----------|------------|
| Primary constructors | Simple DI, no readonly needs | Thread safety requires readonly |
| Records | Pure DTOs, value semantics | Behaviour present, reference equality needed |
| Collection expressions | Clear target type | Ambiguous type inference |
| Field keyword | Property has accessor logic | Variable named `field` exists |
| Switch expressions | 3+ branches, exhaustive | 2 simple branches |

## De-Duplication Patterns

Beyond language modernisation, apply de-duplication strategies:

- Extract repeated LINQ queries
- Consolidate validation rules
- Create base handlers for common operations

See `references/deduplication-strategies.md` for detailed extraction patterns.

## Clarity Patterns

Apply these patterns to improve readability:

- Guard clauses to reduce nesting
- Early returns for error cases
- Switch expressions instead of nested ternaries
- Meaningful names over abbreviations

See `references/clarity-patterns.md` for detailed examples.

## Process for Applying Patterns

1. **Analyse scope** - Identify files via `git diff main --name-only -- '*.cs'`
2. **Read project rules** - Check `.claude/rules/code-style.md` for conventions
3. **Categorise opportunities** - Group by pattern type
4. **Apply project standards first** - Naming, headers, British English
5. **Apply modernisation patterns** - With semantic checks
6. **Look for de-duplication** - Extract repeated code
7. **Verify functionality** - Recommend running tests

## Output Format

When presenting pattern applications:

```markdown
## Pattern Applied: [Name]

**File:** `path/to/file.cs`

**Before:**
[code block]

**After:**
[code block]

**Semantic consideration:** [Any cautions about this change]
```

## Additional Resources

### Reference Files

For detailed patterns and examples, consult:

- **`references/modernisation-patterns.md`** - Complete C# 14 patterns with before/after examples
- **`references/deduplication-strategies.md`** - Code extraction and consolidation patterns
- **`references/clarity-patterns.md`** - Readability improvements and anti-patterns

Each reference file contains extensive examples suitable for copy-paste adaptation.
