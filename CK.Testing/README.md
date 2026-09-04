# CK.Testing: test helpers resolved, not instantiated

A test helper here is an **interface**, and you ask the resolver for it rather than newing a class up.
The point of that indirection is composition: a package can add capabilities to *your* test helper
without you changing anything.

Assertions use [Shouldly](https://docs.shouldly.org/), extended by
[`CKShouldlyExtensions`](CKShouldlyExtensions.cs).

## How an interface becomes an instance.

[`MapType`](Resolver/ResolverImpl.cs) tries two things, in this order:

1. **Convention over emission.** For an interface `IXxx` it looks for a class `Xxx` in the same
   namespace and assembly - and, for an `IXxxCore`, for an `Xxx` walking the namespace up. If such a
   class is found **and is assignable to the interface, it wins**. A hand-written implementation is
   therefore used in preference to anything generated.
2. **Emission, for mixins only.** Failing that, if the interface derives from
   [`IMixinTestHelper`](IMixinTestHelper.cs), [`MixinType`](Resolver/MixinType.cs) emits an
   implementation with `System.Reflection.Emit`
   ([`ILGeneratorExtension`](Resolver/ILGeneratorExtension.cs)).

Anything else throws `Unable to locate an implementation for ...`.

## What a mixin actually forbids.

[`IMixinTestHelper`](IMixinTestHelper.cs) is an empty marker, and its own summary says *"Interfaces
that extends this interface can not be explicitly implemented."* Read that carefully: it is not a
compile-time prohibition - C# has no such mechanism, and step 1 above will happily use a class that
implements a mixin interface.

The rule the code does enforce is a different one:

```csharp
if( t.GetMembers().Length > 0 )
{
    throw new Exception( $"Interface '{t.FullName}' is a Mixin. It can not have members of its own." );
}
```

**A mixin interface must declare nothing of its own.** It is a pure junction of other helper
interfaces, which is what makes it safe to emit: the generated type only has to forward to the
implementations of the interfaces being combined. Declare a member on it and resolution fails at
runtime, not at compile time.

[`ResolveTargetAttribute`](ResolveTargetAttribute.cs) forwards resolution from one type to another,
typically from a core interface to its mixin. It is consulted only at the top of a resolution and on
the mapped class - and no production type in this repository uses it today; the only usages are in the
tests. [`ITestHelperResolvedCallback`](ITestHelperResolvedCallback.cs) lets a helper run code once
resolution is complete.

## What you write for a mixin.

Three declarations, and the resolver does the rest. This is the `A` triplet of
[`ResolverTests`](../Tests/CK.Testing.Tests/ResolverTests.cs), condensed - everything it names comes
from this package:

```csharp
// 1. The core interface: what this helper adds to the TestHelper.
public interface IACore : ITestHelperResolvedCallback
{
    IBasicTestHelper AToBasicRef { get; }
    int CallACount { get; }
    void DoA();
    event EventHandler ADone;
    // ... two more members
}

// 2. The facade: declares nothing, combines everything.
public interface IA : IMixinTestHelper, IBasicTestHelper, IACore
{
}

// 3. The implementation, of the core interface only.
public class A : IACore
{
    readonly IBasicTestHelper _basic;
    int _callCount;

    // Other test helpers are resolved and injected.
    internal A( IBasicTestHelper basic )
    {
        _basic = basic;
    }

    // Explicit implementations, so the facade is what the API exposes.
    int IACore.CallACount => _callCount;
    IBasicTestHelper IACore.AToBasicRef => _basic;
    // ... DoA, ADone, likewise
}
```

Each of the two resolution rules above handles exactly one of these interfaces:

- `IACore` resolves by **rule 1**. Strip the `I` and the `Core`, look for `A` in the same namespace and
  assembly, check it is assignable - it is.
- `IA` resolves by **rule 2**. No class is assignable to it, because `A` implements only the core
  interface and not `IBasicTestHelper`. So it is emitted, and the emitted type forwards to one
  implementation per interface it combines.

That is why the split into three types is not ceremony: interface 1 is what you write, interface 2 is
what you consume, and only class 3 has a body. It is also why the `Core` suffix is load-bearing rather
than stylistic - it drives the name lookup here, and it is read again by
[`MixinType`](Resolver/MixinType.cs) when it decides which interfaces the emitted type must forward to.

The constructor takes an `IBasicTestHelper`: a helper declares what it builds on as constructor
parameters, and the resolver satisfies them.

## Where a test helper knows it is.

[`IBasicTestHelper`](Basic/IBasicTestHelper.cs) exposes the paths a test needs: `SolutionFolder`,
`TestProjectFolder`, `ClosestSUTProjectFolder` (the project under test), `BinFolder`, `PathToBin`,
`LogFolder`, `BuildConfiguration` and `TestProjectName`.

`ClosestSUTProjectFolder` is the one worth knowing. It is **configurable** through the
`TestHelper/ClosestSUTProjectFolder` key, and when it is not configured
[`BasicTestHelper`](Basic/BasicTestHelper.cs) infers it - but only for a folder whose name ends with
`.Tests`, giving priority to a `<Name>.SUT` folder - *"The .SUT always has the priority, wherever it
is"*, searched upward as far as the solution folder, siblings merely tried first - and falling back to
`TestProjectFolder` when it finds nothing. So a fixture can reach the real sources without a relative path hard-coded in
the test, and a project that does not follow the `.Tests` convention configures the key instead.

## Configuration is layered, from the solution down.

[`TestHelperConfiguration`](Configuration/TestHelperConfiguration.cs):

```
/// Simple configuration that reads its content from all "*.TestHelper.config" (in lexicographical 
/// order) and then "TestHelper.config" files in folders from IBasicTestHelper.SolutionFolder 
/// down to the current execution path.
/// Once all these files are applied, environment variables that start with "TestHelper::" prefix are applied.
```

Two things the summary above does not say, both read from the code:

- **Both prefixes work.** `TestHelper::` and `TestHelper__` are accepted (the second is what you use
  where `:` is awkward - a CI variable, a container environment), and key normalization maps both
  separators.
- The per-folder layering means a developer overrides a solution-wide value by dropping a
  `TestHelper.config` next to the test project, and nothing needs to know about it.

Keys are declared, not read blindly: `Declare( key, description, ... )` **must be called once and
only once per key** or it throws `InvalidOperationException`. A description is required. That is what
makes the configuration self-documenting - and what makes a duplicate declaration a startup failure
rather than a silent last-one-wins.
