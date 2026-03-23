# NuGet Publish Checklist

This document captures the steps required to publish Huml.Net to NuGet.org for the first time.
Complete each step in order before triggering the publish workflow.

## Before Publishing to NuGet

1. [ ] Rename GitHub account to `primeBeri` at https://github.com/settings/admin
2. [ ] Rename organisation to `Radberi` if applicable
3. [ ] Verify `PackageProjectUrl` and `RepositoryUrl` in `src/Huml.Net/Huml.Net.csproj` point to `https://github.com/primeBeri/huml-dotnet`
4. [ ] Update `CHANGELOG.md` date: replace `YYYY-MM-DD` with actual release date
5. [ ] Create git tag: `git tag v0.1.0`
6. [ ] Run `dotnet pack src/Huml.Net/Huml.Net.csproj -c Release`
7. [ ] Run `dotnet sourcelink test` against the produced `.nupkg`
8. [ ] Verify README.md is embedded in the `.nupkg` (unzip and check root)
9. [ ] Push tag to trigger publish workflow: `git push origin v0.1.0`
10. [ ] Verify package appears on https://www.nuget.org/packages/Huml.Net/

## Badge Verification

After the GitHub rename, verify these URLs resolve:

- CI badge: `https://github.com/primeBeri/huml-dotnet/actions/workflows/ci.yml/badge.svg`
- NuGet badge: `https://img.shields.io/nuget/v/Huml.Net.svg`

## Notes

- The `PackageProjectUrl` and `RepositoryUrl` fields in `Huml.Net.csproj` already reference `primeBeri` URLs.
  They will appear broken in GitHub until the account rename is complete.
- The CI badge in `README.md` also references `primeBeri` and will not render until after the rename.
- Do not push the `v0.1.0` tag before the GitHub rename — the SourceLink embedded PDB will reference the old URL.
