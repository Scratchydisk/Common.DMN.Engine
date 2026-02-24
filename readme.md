# DMN Engine

A .NET rule engine that executes decisions defined in [DMN (Decision Model and Notation)](https://www.omg.org/spec/DMN/1.1/) models. It evaluates decision tables and expression decisions from OMG-standard DMN XML files, or from definitions built programmatically using a fluent API.

> **This is a fork** of [adamecr/Common.DMN.Engine](https://github.com/adamecr/Common.DMN.Engine) (v1.1.1).
> The v2.0.0 release replaces the expression evaluation engine with a full FEEL interpreter built on ANTLR4, adds DMN 1.4/1.5 support, and makes several other substantial changes.
> The original v1.x documentation is archived in [readme-v1.md](readme-v1.md).

**NuGet package ID:** net.adamec.lib.common.dmn.engine
**Target framework:** .NET 10.0

See the latest changes in the [changelog](changelog.md).

## Tools

- **[DMN Testbed](docs/testbed.md)** — A web-based test lab for interactively executing decisions, testing full DRD trees, and managing regression test suites. Start it with `dotnet run --project net.adamec.lib.common.dmn.engine.testbed -- --dmn-dir=/path/to/dmn/files` and open `http://localhost:5000`.
- **[CLI Tool](docs/cli-tool.md)** — `dmnrunner` command-line tool for batch execution and CI integration.

## What Changed from the Original

| Area | Original (v1.x) | This fork (v2.0) |
|------|-----------------|-------------------|
| Expression evaluator | DynamicExpresso (C#-flavoured) | ANTLR4-based FEEL interpreter |
| Expression language | S-FEEL subset + C# syntax | Full FEEL + CLR interop for backward compat |
| DMN versions | 1.1, 1.3, 1.3ext | 1.1, 1.3, 1.3ext, **1.4, 1.5** |
| Target framework | .NET Standard 2.0 | .NET 10.0 |
| `date` type | `DateTime` | `DateOnly` |
| `date and time` type | `DateTime` | `DateTimeOffset` |
| `time` type | N/A | `FeelTime` |
| `years and months duration` | N/A | `FeelYmDuration` |
| Dependencies | DynamicExpresso.Core, RadCommons.core, NLog 4.x | Antlr4.Runtime.Standard, NLog 5.x |

**DMN XML files are fully compatible** — the same `.dmn` files from v1.x will parse and execute. Existing S-FEEL expressions (`>5`, `[1..10]`, `not(5,7)`, `"hello"`, etc.) remain valid FEEL and evaluate identically.

**C# API breaking changes** — the public API shape is largely preserved (`DmnParser`, `DmnDefinitionFactory`, `DmnExecutionContext`, `DmnDefinitionBuilder`) but there are type-level breaking changes documented in the [changelog](changelog.md).

## Architecture

### Core Pipeline

```
DMN XML --> DmnParser --> DmnModel --> DmnDefinitionFactory --> DmnDefinition --> DmnExecutionContextFactory --> DmnExecutionContext
                                                                                                                    |
Code --> DmnDefinitionBuilder --> DmnDefinition ------------------------------------------------------------------>-+
```

1. **Parsing** (`parser/`): `DmnParser` deserialises DMN XML (v1.1, 1.3, 1.3ext, 1.4, 1.5) into `DmnModel` DTOs. Supports auto-detection of DMN version from the XML namespace.
2. **Definition** (`engine/definition/`): `DmnDefinitionFactory` transforms `DmnModel` into `DmnDefinition` — validation, variable type resolution, and dependency tree construction. Definitions are "virtually immutable" (exposed via read-only interfaces).
3. **Builder** (`engine/definition/builder/`): `DmnDefinitionBuilder` provides a fluent API to create `DmnDefinition` programmatically without XML.
4. **Decisions** (`engine/decisions/`): Two types — `DmnExpressionDecision` (single expression to output) and `DmnDecisionTable` (rules with inputs, outputs, hit policies).
5. **Execution** (`engine/execution/`): `DmnExecutionContext` manages variables, resolves decision dependencies recursively, evaluates expressions via the FEEL evaluator, and returns `DmnDecisionResult`.

### FEEL Evaluator Pipeline

```
FEEL expression string
  --> FeelLexer.g4 --> Token stream
  --> FeelNameResolver --> Merged tokens (multi-word names resolved)
  --> FeelParser.g4 --> Parse tree
  --> FeelAstBuilder --> FeelAstNode (AST)
  --> FeelEvaluator --> Result value
```

- **Grammar** (`feel/grammar/`): `FeelLexer.g4` and `FeelParser.g4` — ANTLR4 grammars compiled at build time via `Antlr4BuildTasks`
- **Parsing** (`feel/parsing/`): `FeelScope` (variable/function name registry), `FeelNameResolver` (multi-word identifier merging), `FeelAstBuilder` (parse tree to AST)
- **AST** (`feel/ast/`): `FeelAstNode` hierarchy — literals, operators, control flow, collections, functions, unary tests
- **Evaluation** (`feel/eval/`): `FeelEvaluationContext` (variable scope chain) and `FeelEvaluator` (tree-walking interpreter with three-valued logic)
- **Functions** (`feel/functions/`): ~80 built-in FEEL functions (string, numeric, list, date/time, context, range, boolean, conversion)
- **Types** (`feel/types/`): `FeelTime`, `FeelYmDuration`, `FeelRange`, `FeelContext`, `FeelFunction`, `FeelTypeCoercion`, `FeelValueComparer`
- **Facade** (`feel/FeelEngine.cs`): Public API — `EvaluateExpression()`, `EvaluateSimpleUnaryTests()`, `ParseExpression()`, `ParseSimpleUnaryTests()`

### FEEL Type Mappings

| FEEL Type | .NET Type |
|-----------|-----------|
| `number` | `decimal` |
| `string` | `string` |
| `boolean` | `bool` |
| `date` | `DateOnly` |
| `time` | `FeelTime` |
| `date and time` | `DateTimeOffset` |
| `years and months duration` | `FeelYmDuration` |
| `days and time duration` | `TimeSpan` |
| `list` | `List<object>` |
| `context` | `FeelContext` |
| `range` | `FeelRange` |
| `function` | `FeelFunction` |

### Key Design Patterns

- **Virtual immutability**: Definitions are effectively immutable after creation (hidden behind read-only interfaces like `IDmnVariable`, `IDmnDefinition`). Safe for concurrent access.
- **Factory pattern**: `DmnDefinitionFactory` and `DmnExecutionContextFactory` are the primary creation points. `DmnDefinitionFactory` has virtual protected methods for subclassing.
- **Expression caching**: Parsed FEEL AST nodes (`FeelAstNode`) are cached with configurable scope (None, Execution, Context, Definition, Global) via `ParsedExpressionCacheScopeEnum`. AST nodes are immutable and thread-safe.
- **CLR interop**: The FEEL evaluator supports CLR instance method calls (e.g. `.ToString()`) and static method calls (e.g. `double.Parse()`, `Math.Abs()`) for backward compatibility with v1.x expressions.

### Decision Table Hit Policies

Unique, First, Priority, Any, Collect (with aggregations: List, Sum, Min, Max, Count), RuleOrder, OutputOrder.

### Dependencies

- **Antlr4.Runtime.Standard** 4.13.1 — ANTLR4 runtime for FEEL parser
- **Antlr4BuildTasks** 12.8 — Build-time grammar compilation (private asset)
- **NLog** 5.3.4 — Logging

## Quick Start

The basic use case is:

1. Parse the DMN model from file.
2. Create an engine execution context and load (and validate) the model.
3. Provide the input parameter(s).
4. Execute (and evaluate) the decision and get the result(s).

```csharp
var def = DmnParser.Parse(fileName);
var ctx = DmnExecutionContextFactory.CreateExecutionContext(def);
ctx.WithInputParameter("input name", inputValue);
var result = ctx.ExecuteDecision("decision name");
```

![DMN engine blocks](docs/img/blocks.png)

## Build and Test

The library uses a customised MSBuild process in the `build` and `build.tasks` projects. These can safely be removed from the solution if not needed. Details are in the [build documentation](build/readme.md).

```bash
# Build the solution
dotnet build Common.DMN.Engine.sln

# Run all tests
dotnet test net.adamec.lib.common.dmn.engine.tests/net.adamec.lib.common.dmn.engine.tests.csproj

# Run a single test by name
dotnet test net.adamec.lib.common.dmn.engine.tests/net.adamec.lib.common.dmn.engine.tests.csproj --filter "FullyQualifiedName~TestMethodName"
```

Tests are implemented using [MSTest](https://github.com/microsoft/testfx) with [FluentAssertions](https://fluentassertions.com/). The test code is in a shared project linked into the consolidated test project `net.adamec.lib.common.dmn.engine.tests` targeting .NET 10.0.

`DmnTestBase` provides abstraction for running the same tests against different sources (DMN XML 1.1/1.3/1.3ext/1.4/1.5 or builders). Primary test classes inherit from `DmnTestBase` and contain test logic targeting DMN XML 1.1. Derived classes override the `Source` property to reuse the same tests against other DMN versions and builder-based definitions. `DmnBuilderSamples` is generated from DMN XML test models using the builder API.

> **Note:** Adjust the `LogHome` variable in `nlog.config` of the test project as needed.

## DMN Testbed

The testbed is a web-based test lab for interactively executing DMN decisions and managing test suites. It serves a Nuxt SPA frontend from an ASP.NET Core backend that wraps the engine.

```bash
# Start the testbed, pointing it at a directory of .dmn files
dotnet run --project net.adamec.lib.common.dmn.engine.testbed -- --dmn-dir=/path/to/dmn/files
```

Then open `http://localhost:5000` in a browser. The `--dmn-dir` argument defaults to the current directory if omitted.

To develop the frontend with hot reload, start the backend and the Nuxt dev server separately:

```bash
# Terminal 1: backend on port 5000
dotnet run --project net.adamec.lib.common.dmn.engine.testbed -- --dmn-dir=/path/to/dmn/files

# Terminal 2: frontend dev server (proxies /api to the backend)
cd net.adamec.lib.common.dmn.engine.testbed/client
npm install
npm run dev
```

To build the frontend for production (output goes to `wwwroot/` so the backend serves it directly):

```bash
cd net.adamec.lib.common.dmn.engine.testbed/client
npm run generate
```

## Documentation

| Document | Description |
|----------|-------------|
| [Decision Model](docs/decision-model.md) | Parsing DMN XML, DmnDefinitionBuilder, inputs, decisions, dependency tree |
| [Variables](docs/variables.md) | Variable names, types, normalisation, complex objects |
| [Expressions](docs/expressions.md) | FEEL expressions, simple unary tests, built-in functions |
| [Expression Decisions](docs/expression-decisions.md) | Expression decisions in XML and via builder |
| [Decision Tables](docs/decision-tables.md) | Inputs, outputs, rules, SFeel helper, allowed values, hit policies |
| [Decision Results](docs/decision-results.md) | Working with DmnDecisionResult |
| [Extensions](docs/extensions.md) | Definition extensions and diagram extensions |
| [Advanced Execution](docs/advanced-execution.md) | Execution context options, snapshots, expression cache |
| [CLI Tool](docs/cli-tool.md) | dmnrunner command-line tool for testing DMN files |
| [Testbed](docs/testbed.md) | Web-based test lab for DMN files |

### Code Documentation

The [code documentation](docs/net.adamec.lib.common.dmn.engine.md) is generated during the customised build using [MarkupDoc](https://github.com/adamecr/MarkupDoc).
