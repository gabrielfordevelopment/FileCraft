# Copilot Instructions for FileCraft

## Scope

These instructions apply to the entire repository.

## Project context

- FileCraft is a Windows desktop application built with WPF and .NET 10.
- The project follows MVVM with dependency injection through `Microsoft.Extensions.Hosting`.
- Keep UI work aligned with existing XAML resource dictionaries, shared controls, and Material Icons usage.
- Persistence uses JSON save files plus LiteDB-backed presets; preserve backward-compatible behavior for existing save data and preset data.

## Core principles

- Keep changes focused on the requested task.
- Prefer long-term maintainable solutions over quick patches.
- Avoid unrelated refactors.
- Reuse existing patterns and naming from the codebase.
- Keep module responsibilities clear (`Services`, `ViewModels`, `Models`, `Shared`).

## Before coding

- Review relevant files around the target behavior before editing.
- Validate dependencies and call flows before introducing new abstractions.
- Prefer the existing architecture and conventions over parallel patterns.
- For non-trivial changes, review nearby View, ViewModel, Service, Model, and interface files before editing.

## Code quality expectations

- Do not swallow exceptions silently.
- Add diagnostics for failure paths that can affect user data or app stability.
- Keep async flows predictable and cancellation-aware where applicable.
- Avoid magic strings when stable identifiers/constants are repeatedly used.
- Minimize duplication; centralize shared logic/constants.
- Keep user-facing text in `Resources/Locals/Strings.en-US.xaml` and reference it from XAML or via `ResourceHelper` instead of hardcoding new UI strings in C#.
- Preserve UTF-8 encoding and do not run bulk encoding or repository-wide search/replace rewrites unless explicitly requested.
- If mojibake or garbled text is found in files being edited, normalize only the touched text while preserving meaning.
- Keep comments documentation-oriented and current-state focused; do not add change-history comments.

## Testing and validation

- Add or update tests for changed business logic.
- Run `dotnet build FileCraft.sln` and relevant tests after modifications when the environment supports it.
- Keep changes compile-safe and behaviorally consistent.

## Pull request discipline

- Keep commits small and task-oriented.
- Include only files related to the active task.
- Summarize what changed and why, focusing on final behavior.
- Use `main` only for production-ready release state.
- Use `development` as the integration branch for upcoming work.
- Start new feature branches from `development`, not from `main`.
- Do not commit directly to `main` or `development`; put all changes on feature branches and merge them back through pull requests.
- Target feature pull requests to `development` by default.
- Create or switch branches, create commits, and publish branches only when explicitly requested.
- Use Conventional Commit style for commit messages when a commit is requested.
