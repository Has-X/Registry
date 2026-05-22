## Summary

-

## Validation

- [ ] `dotnet build Registry.slnx`
- [ ] `dotnet test tests\Registry.Tests\Registry.Tests.csproj --no-build`

## Registry Safety

- [ ] Destructive actions are confirmed.
- [ ] 32-bit/64-bit view behavior is explicit.
- [ ] Tests use disposable HKCU keys only.
