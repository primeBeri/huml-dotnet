# NuGet Release Checklist

Repeat this checklist for every release.

## One-time setup (completed for v0.1.0-alpha.1)

- [x] GitHub account renamed to `primeBeri`
- [x] `PackageProjectUrl` and `RepositoryUrl` in `Huml.Net.csproj` point to `https://github.com/primeBeri/huml-dotnet`
- [x] NuGet OIDC Trusted Publisher configured for `primeBeri`
- [x] First package published to https://www.nuget.org/packages/Huml.Net/

## Per-release steps

1. [ ] Update `CHANGELOG.md` — move items from `[Unreleased]` into a new versioned section with today's date.
2. [ ] Ensure all tests pass: `dotnet test`
3. [ ] Pack locally and verify: `dotnet pack src/Huml.Net/Huml.Net.csproj -c Release -o ./out`
4. [ ] Run SourceLink validation: `dotnet tool run sourcelink test ./out/*.nupkg`
5. [ ] Confirm README.md is embedded in the package: `unzip -l ./out/*.nupkg | grep README`
6. [ ] Create and push a version tag to trigger the publish workflow:
   ```bash
   git tag v<version>
   git push origin v<version>
   ```
7. [ ] Verify the package appears on NuGet: https://www.nuget.org/packages/Huml.Net/
8. [ ] Verify CI badge renders: `https://github.com/primeBeri/huml-dotnet/actions/workflows/ci.yml/badge.svg`
