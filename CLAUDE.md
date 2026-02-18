# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Common.DMN.Engine is a .NET rule engine that executes decisions defined in DMN (Decision Model and Notation) models. It evaluates decision tables and expression decisions from OMG-standard DMN XML files (versions 1.1, 1.3, 1.3ext) or programmatically built definitions.

**NuGet package:** `net.adamec.lib.common.dmn.engine`
**Target framework:** .NET Standard 2.0
**Current version:** 1.1.1

## Build and Test Commands

```bash
# Build the solution
dotnet build Common.DMN.Engine.sln

# Run all tests (use a specific test project since the shared project can't run directly)
dotnet test net.adamec.lib.common.dmn.engine.test.netcore6/net.adamec.lib.common.dmn.engine.test.netcore6.csproj

# Run a single test by fully qualified name
dotnet test net.adamec.lib.common.dmn.engine.test.netcore6/net.adamec.lib.common.dmn.engine.test.netcore6.csproj --filter "FullyQualifiedName~TestMethodName"

# Run tests across all target frameworks
dotnet test Common.DMN.Engine.sln
```

Test projects target different frameworks: .NET Core 3.1, .NET 5, .NET 6, .NET Framework 4.6.2, and 4.7.2. The `.netcore6` project is the most convenient for development.

## Solution Structure

- **net.adamec.lib.common.dmn.engine/** - Core library (.NET Standard 2.0)
- **net.adamec.lib.common.dmn.engine.test.shared/** - Shared test code (.shproj, linked into framework-specific test projects)
- **net.adamec.lib.common.dmn.engine.test.netcore6/** (and other targets) - Test runners per framework
- **net.adamec.lib.common.dmn.engine.simulator/** - WPF demo app (.NET 6 Desktop)
- **build/** and **build.tasks/** - Custom MSBuild process for versioning, NuGet packaging, and doc generation (safe to ignore)

## Architecture

### Core Pipeline

```
DMN XML ──DmnParser──▶ DmnModel ──DmnDefinitionFactory──▶ DmnDefinition ──DmnExecutionContextFactory──▶ DmnExecutionContext
                                                                                                            │
Code ──DmnDefinitionBuilder──▶ DmnDefinition ──────────────────────────────────────────────────────────────────┘
```

1. **Parsing** (`parser/`): `DmnParser` deserializes DMN XML into `DmnModel` DTOs. Minimal logic here.
2. **Definition** (`engine/definition/`): `DmnDefinitionFactory` transforms `DmnModel` into `DmnDefinition` — this is where validation, variable type resolution, and dependency tree construction happen. Definitions are "virtually immutable" (exposed via read-only interfaces).
3. **Builder** (`engine/definition/builder/`): `DmnDefinitionBuilder` provides a fluent API to create `DmnDefinition` programmatically without XML.
4. **Decisions** (`engine/decisions/`): Two types — `DmnExpressionDecision` (single expression → output) and `DmnDecisionTable` (rules with inputs, outputs, hit policies).
5. **Execution** (`engine/execution/`): `DmnExecutionContext` manages variables, resolves decision dependencies recursively, evaluates expressions via DynamicExpresso, and returns `DmnDecisionResult`.

### Key Design Patterns

- **Virtual immutability**: Definitions are effectively immutable after creation (hidden behind read-only interfaces like `IDmnVariable`, `IDmnDefinition`). Safe for concurrent access.
- **Factory pattern**: `DmnDefinitionFactory` and `DmnExecutionContextFactory` are the primary creation points. `DmnDefinitionFactory` has virtual protected methods for subclassing.
- **Expression caching**: Parsed DynamicExpresso expressions are cached with configurable scope (None, Execution, Context, Definition, Global) via `ParsedExpressionCacheScopeEnum`.
- **Variable name normalisation**: Spaces/dashes become underscores, special characters are stripped. `NormalizeVariableName` is used throughout.

### Decision Table Hit Policies

Unique, First, Priority, Any, Collect (with aggregations: List, Sum, Min, Max, Count), RuleOrder, OutputOrder.

### Dependencies

- **DynamicExpresso.Core** - Expression evaluation engine
- **NLog** - Logging
- **RadCommons.core** - Common utilities and logging abstractions

## Test Architecture

Tests use **MSTest** with **FluentAssertions**. Test code lives in the shared project and uses an inheritance pattern:

- `DmnTestBase` — abstract base providing `Source` property that determines whether tests run against DMN 1.1, 1.3, 1.3ext XML, or the builder API
- Primary test classes inherit `DmnTestBase` and contain actual test logic (default: DMN 1.1)
- Derived classes override `Source` to reuse the same tests against DMN 1.3, 1.3ext, and builder-based definitions
- `DmnBuilderSamples` — auto-generated class that recreates DMN XML test models using the builder API

Test folders: `builder/` (builder tests), `complex/` (integration/scenario tests), `unit/` (unit tests), `dmn/` (sample DMN XML files), `issue/` (regression tests).

## Versioning

Version is managed centrally in `Version.props` and propagated through `Directory.Build.props`. The custom build system (enabled via `RadUseCustomBuild` env var) handles NuGet packaging and doc generation but is not required for development builds.
