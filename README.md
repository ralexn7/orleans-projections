# Orleans.Projections

Server-side, LINQ-based projections for [Microsoft Orleans](https://learn.microsoft.com/dotnet/orleans/) grains.

Instead of fetching a grain's entire state to the caller and shaping it there, `Orleans.Projections`
lets a client describe *what it wants* as an ordinary LINQ expression. That expression is translated
into a small, serializable **projection plan**, shipped to the grain, evaluated **against the state
inside the grain**, and only the selected/computed values travel back over the wire.

```csharp
// Runs on the grain. Only Name, Age, a formatted location and two computed
// values cross the network — never the whole PersonState.
var summary = await personGrain.Get(state => new
{
    state.Name,
    state.Age,
    Location = $"{state.Address.City}, {state.Address.Country}",
    AgePlusFive = state.Age + 5,
    IsAdult = state.Age >= 18
});
```

## Why

A grain owns its state and is the single point of access to it. The natural way to read part of that
state is to expose a method that returns exactly what a given caller needs — but that means writing a
new grain method (and a new DTO) for every read shape.

`Orleans.Projections` removes that per-shape boilerplate while preserving the encapsulation boundary:

- **Only projected data is transferred.** Computation happens on the grain; the full internal state
  never leaves it.
- **Arbitrary read shapes, no new grain methods.** The projection shape is expressed at the call site.
- **The boundary is enforced.** You can select and compute over fields, but you cannot exfiltrate the
  whole state object — `state => new { Self = state }` is rejected by design (see
  [Design notes](#design-notes)).

## How it works

```
Client                                            Grain
──────                                            ─────
Expression<Func<TState, TProjection>>
        │
        │  ProjectionPlanTranslator.Translate
        ▼
(ProjectionPlan<TState>, constants)  ──serialized──▶  IProjectableGrain<TState>.Get(plan, constants)
                                                              │
                                                              │  plan.BuildLambda()  (compiled + cached)
                                                              │  Func<TState, object?[], object?[]>
                                                              ▼
                                                        projector(state, constants)
                                                              │
        Projection { object?[] Values }  ◀──serialized──────  ┘
        │
        │  Projection.Materialize<TProjection>()
        ▼
   TProjection
```

1. **Translate.** `ProjectionPlanTranslator` walks the client-side expression tree and turns each
   supported node into a serializable `INode`. Literals are pulled out into a separate `constants`
   list so the plan captures only the *shape* of the projection, not the values.
2. **Ship & evaluate.** The `ProjectionPlan<TState>` and the constants are sent to the grain via
   `IProjectableGrain<TState>.Get`. On the grain, `BuildLambda()` rebuilds and compiles the node tree
   into a `Func<TState, object?[], object?[]>` and runs it against the live state.
3. **Return & rehydrate.** The grain returns a `Projection` (a flat `object?[]` of values). On the
   client, `Materialize<TProjection>` reconstructs the requested type from those values.

## Getting started

### 1. Define state and a projectable grain

A grain opts in by implementing `IProjectableGrain<TState>` and delegating to the compiled plan:

```csharp
[GenerateSerializer]
public record Address(string City, string Country);

[GenerateSerializer]
public record PersonState(string Name, int Age, Address Address);

public interface IPersonGrain : IProjectableGrain<PersonState>, IGrainWithStringKey
{
    Task SetStateAsync(PersonState state);
}

public class PersonGrain : Grain, IPersonGrain
{
    private PersonState _state = new("<unset>", 0, new Address("<unset>", "<unset>"));

    public Task SetStateAsync(PersonState state)
    {
        _state = state;
        return Task.CompletedTask;
    }

    public Task<Projection> Get(
        ProjectionPlan<PersonState> plan,
        List<object?> parameters,
        CancellationToken cancellationToken)
    {
        var projector = plan.BuildLambda();
        return Task.FromResult(new Projection(projector(_state, parameters.ToArray())));
    }
}
```

### 2. Project from the client

The `Get<TState, TProjection>` extension method handles translation and rehydration for you:

```csharp
using Orleans.Projections;

// Anonymous type
var summary = await personGrain.Get(state => new
{
    state.Name,
    Location = $"{state.Address.City}, {state.Address.Country}",
    IsAdult = state.Age >= 18
});

// Explicit type (constructor or settable properties)
var person = await personGrain.Get(state => new PersonData
{
    Name = state.Name,
    Age  = state.Age,
});

// A scalar
int age = await personGrain.Get(state => state.Age);

// A computed scalar
int agePlusTwenty = await personGrain.Get(state => state.Age + 20);

// An enum
AgeCategory category = await personGrain.Get(state => state.Age >= 18 ? AgeCategory.Adult : AgeCategory.Child);
```

You can also build a `ProjectionPlan<TState>` by hand from `INode`s and call `grain.Get(plan, ...)`
directly — the expression-based API is a convenience layer on top of that.

## Supported expressions

The translator (`ProjectionPlanTranslator.BuildNode`) currently supports:

| Category            | Examples                                                       |
|---------------------|----------------------------------------------------------------|
| Member access       | `state.Name`, nested `state.Address.City`                      |
| Constants           | `10`, `"literal"` (extracted into the constants list)          |
| Binary arithmetic   | `state.Age + 5`, `(state.Age * 2 + 5) % 4`                     |
| Unary               | negation `-state.Age`, casts `(long) state.Age`, boxing        |
| Comparisons         | `state.Age > 18`, `state.Age >= 18`                            |
| Logical             | `&&`, `\|\|`                                                   |
| Conditional         | ternary `state.Age >= 18 ? "adult" : "minor"`                  |
| Null-coalescing     | `state.Name ?? "unknown"`                                      |
| String operations   | concatenation, interpolation `$"{state.Name} ({state.Age})"`   |
| Method calls        | static (`int.Max(...)`) and instance (`state.Name.ToUpper()`)  |
| Indexers            | `state.Name[0]`                                                |
| Array / list init   | `new int[] { ... }`, `new List<int> { ... }`                   |
| Dictionary init     | `new Dictionary<string, int> { { "age", state.Age } }`         |
| Object construction | `new Address(state.Address.City, "US")`                        |
| Member init         | `new Coordinate { X = state.X, Y = state.Y + 10 }`             |
| Nested projections  | anonymous / constructed objects nested inside the projection   |
| Enums               | enum-valued results; stored and rehydrated by underlying value |

**Projection targets** rebuilt by `Projection.Materialize<T>`:

- Scalars (primitives, `string`, `decimal`, enums)
- Anonymous types
- Types with a matching public constructor
- Types with settable public properties (constructed via parameterless ctor, then assigned)
- Arrays and nested complex objects

> **Nested lambdas** (e.g. `state.Items.Select(i => i.Name)`) are a work in progress. The supporting
> infrastructure — `LambdaExpressionNode`, `ParameterNode`, and lambda scopes in `BuildContext` — is
> in place, but end-to-end collection projections are not fully wired up yet.

## Architecture

| Type                                    | Role                                                                                          |
|-----------------------------------------|-----------------------------------------------------------------------------------------------|
| `IProjectableGrain<TState>`             | Grain-side contract: `Get(plan, parameters, ct)`.                                             |
| `ProjectableGrainExtensions.Get<…>`     | Client-side convenience: expression → plan → call → rehydrated result.                        |
| `ProjectionPlanTranslator`              | Translates a LINQ expression tree into an `INode[]` plan plus an extracted constants list.     |
| `ProjectionPlan<TState>`                | Serializable node tree. `BuildLambda()` compiles it to a delegate and caches by plan identity. |
| `INode` + `Nodes/*`                     | Serializable representations of expression-tree constructs (member, const, binary, call, …).   |
| `BuildContext`                          | Carries the root-state and constants parameters (and lambda scopes) during expression building. |
| `Projection`                            | Result envelope (`object?[] Values`) with `Materialize<T>` for rehydration.              |

## Design notes

- **Constants are parameterized, not baked in.** Literals are hoisted out of the node tree into a
  separate list. This keeps a plan dependent only on the projection's *shape*, which is what makes the
  compiled-delegate cache (`ProjectionPlan.BuildLambda`) effective across calls that differ only in
  their constant values.
- **Enums travel as their underlying value.** The concrete enum type may not exist on the grain/server
  side, so enum constants are stored as their underlying integer and converted back to the enum type
  during rehydration.
- **The root `state` parameter is not projectable as a leaf.** Returning the state object itself
  (`state => new { Self = state }`) is intentionally unsupported — it would defeat the encapsulation
  boundary projections exist to enforce. Inner-lambda parameters are a separate concern and are
  handled via lambda scopes.
- **Compiled-delegate caching.** Each distinct plan compiles its expression once; subsequent
  evaluations reuse the cached `Func<TState, object?[], object?[]>`.

## Building and testing

Requires the **.NET 10** SDK.

```bash
dotnet build
dotnet test
```

The test suite spins up an in-memory Orleans cluster via `Orleans.TestingHost` and exercises the full
client → plan → grain → rehydration flow (`Orleans.Projections.Test`).

## Status

This is an experimental / in-progress project. Several areas are still marked with `todo`s in the
source, including reflection caching in `Projection`, broader member-binding support
(`MemberListBinding` / `MemberMemberBinding`), nullability handling, and full collection/lambda
projections. APIs may change.
