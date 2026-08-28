# CK.Testing.NUnit: every test in the monitor, with no code

Referencing this package makes each NUnit test log itself as a group in the test helper monitor, and
log the failure message and stack trace on error. There is nothing to write in the test project - no
base class, no attribute, no setup.

## How it gets there: MSBuild, not a source generator.

The work is done by an assembly-level attribute that the build generates for you. Two files ship in
`buildTransitive`, so they apply to the project that references the package:

- [`MSBuild/CK.Testing.NUnit.props`](MSBuild/CK.Testing.NUnit.props) sets a property naming the
  attribute:

  ```xml
  <PropertyGroup>
    <CKTestingNUnit>CK.Testing.NUnit.TestHelperMonitorSupport</CKTestingNUnit>
  </PropertyGroup>
  ```

- [`MSBuild/CK.Testing.NUnit.targets`](MSBuild/CK.Testing.NUnit.targets) declares an
  `AddGeneratedFile` target, hooked `BeforeTargets="BeforeCompile;CoreCompile"`, that writes that
  property into `$(IntermediateOutputPath)CK.Testing.NUnit.AutoAttributes.g.cs` and adds the file to
  `@(Compile)`:

  ```xml
  <WriteLinesToFile Lines="[assembly: $(CKTestingNUnit)]" File="$(GeneratedFilePath)"
                    WriteOnlyWhenDifferent="true" Overwrite="true" />
  ```

[`TestHelperMonitorSupportAttribute`](TestHelperMonitorSupportAttribute.cs) is an NUnit `ITestAction`:
it opens a group named after the test in `BeforeTest` and closes it in `AfterTest`, logging the result
message and stack trace when the status is not `Passed`.

## The extension protocol, and its one sharp edge.

`$(CKTestingNUnit)` is a comma-separated list on purpose. Another `CK.Testing.*` package that wants
its own NUnit assembly attribute **appends** to it rather than replacing it - the props file spells the
contract out:

```xml
<Project>
  <PropertyGroup>
    <CKTestingNUnit>$(CKTestingNUnit), CK.Testing.AnotherMagic.BringSomeSupport</CKTestingNUnit>
  </PropertyGroup>
</Project>
```

packaged as `buildTransitive\CK.Testing.AnotherMagic.props`.

The sharp edge is that **this package's own props assigns the property unconditionally** - no
`Condition`, no append. An extension props imported *before* it is therefore silently discarded. Since
nothing in the props, the targets or the csproj constrains the import order, an extension that stops
working after a package upgrade is a plausible failure, and the symptom is quiet: `WriteLinesToFile`
emits exactly one line, so the generated file is always one line long - it is the *content* of that
line that lost an attribute, not the file that got shorter.

This mechanism is not exercised inside this repository: every test project here uses a
`ProjectReference`, so no `buildTransitive` props is imported at all. It is only ever tested by a real
consumer.

## Requires.

- `CK.Testing.Monitoring` (the monitor the groups are written to), NUnit 4.4.0.
