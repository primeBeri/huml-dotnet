# Skill Reference by Task Type

Use this table to tag each plan task with the appropriate skills.

| Task Type | Skills |
|-----------|--------|
| **Any SpaceHub.Web async work** | `rules-BlazorConfigureAwait` **(MANDATORY)** |
| Implementing feature/bugfix | `Radberi-TDD` |
| Writing/modifying tests | `Radberi-UnitTesting`, `Radberi-TestingAntiPatterns` |
| Blazor component tests | `Radberi-BlazorTesting` |
| Flaky/timing tests | `Radberi-ConditionBasedWaitingCSharp` |
| Debugging failures | `Radberi-SystematicDebugging` |
| Multi-layer validation | `Radberi-DefenceInDepth` |
| CSS in Syncfusion templates | `rules-BlazorCssIsolation` |
| SfDialog components | `rules-DialogDesign` |
| Grid data updates | `rules-GridRefresh` |
| Test data/seeds | `rules-SeedData` |
| localStorage persistence | `rules-StatePersistence` |
| Final validation | `Radberi-VerificationBeforeCompletion` |

**Web Project Rule:** Any task touching `SpaceHub.Web` with async code MUST include `rules-BlazorConfigureAwait` in the Skills line.

Most implementation tasks need only `Radberi-TDD`.
