---
name: "auto-test-fix-loop"
description: "Runs a self-test and auto-fix loop for generated code. Invoke after implementing a module or when the user asks to test, debug, and repair code automatically."
---

# Auto Test Fix Loop

## Purpose

This skill defines a closed-loop workflow for code self-testing and automated repair after implementation work is completed.

Use this skill when:

- The user asks to "写完代码后自测"
- The user asks to "测试并修复"
- A full module has just been implemented and should be validated end-to-end

## Goal

After code generation, automatically execute the following loop:

1. Verify functional completeness
2. Run tests and capture failures
3. Analyze root causes
4. Apply targeted fixes
5. Re-run tests
6. Repeat until stable or stop conditions are met

## Execution Workflow

### Step 1: Environment Preparation

- Check whether the project already contains a test framework
- For backend projects, prefer `xUnit` or `NUnit`
- For frontend projects, prefer `Vitest` or `Jest`
- If the test framework is missing, create the smallest viable test setup
- Use an isolated test database when needed, preferably an in-memory database or test container

### Step 2: Test Case Generation

Generate tests according to the implemented feature:

- Unit tests:
  - Normal paths
  - Boundary conditions
  - Invalid input
- Integration tests:
  - API request and response validation
  - Persistence changes
  - Dependency interactions
- Frontend component tests:
  - Rendering
  - Event triggering
  - State updates
- Business flow tests:
  - End-to-end scenario simulation

## Test Generation Rules

- Each controller or service should have at least one corresponding test class
- Each CRUD API should include:
  - Create
  - Read
  - Update
  - Delete
- Complex business flows should simulate a full scenario
- Test data should be created with mocks, builders, or factories

### Step 3: Test Execution

- Run all relevant tests
- Capture:
  - Compiler errors
  - Runtime exceptions
  - Assertion failures
  - Integration failures
- Record:
  - Error type
  - Stack trace
  - Related file
  - Related code location when available

### Step 4: Failure Analysis and Auto Repair

For every failed test, follow this repair loop:

1. Locate root cause
   - Compile errors:
     - Missing imports
     - Type mismatches
     - Unimplemented interfaces
   - Runtime exceptions:
     - Null references
     - Missing validation
     - Database constraint issues
   - Logic errors:
     - Wrong branching
     - Wrong return values
     - Incorrect state transitions

2. Generate a repair plan
   - Only touch the code directly related to the failure
   - Avoid unnecessary refactors

3. Apply the fix
   - Update the relevant source files
   - Re-run the failed test or the smallest relevant scope first

4. Retry limit
   - Retry the same failure at most 3 times
   - If still failing, mark it as not automatically repairable and produce a report

### Step 5: Loop Stop Conditions

Stop the iteration when any of the following is true:

- All tests pass
- Two consecutive repair rounds produce no meaningful change
- Maximum iteration count is reached, default 5 rounds
- A fatal blocker is encountered, such as missing external dependencies

### Step 6: Final Report

The report should include:

- Total number of tests
- Passed count
- Failed count
- Number of automatic fixes applied
- Final status of each failed case
- Suggestions for manual follow-up if unresolved issues remain

## Error Priority

- `P0`: Compile errors, startup failures
- `P1`: Core functionality failures, such as login or key business APIs
- `P2`: Boundary-condition failures
- `P3`: Warnings or performance issues that do not block correctness

Always address higher-priority failures first.

## Typical Fix Strategies

| Error type | Typical fix |
|------|------|
| `NullReferenceException` | Add null checks or fallback values |
| `DbUpdateException` | Validate uniqueness and foreign keys |
| `404 Not Found` | Verify routes and controller registration |
| Missing dependency injection | Register service in `Program.cs` |
| Frontend component not registered | Export or import the component correctly |

## Safety Rules

- Reset or isolate test data before each run
- Do not modify production connection strings or production configuration
- After each repair, ensure no new compile errors are introduced
- Prefer the smallest targeted fix over broad refactoring

## Repository Defaults

When this skill is used in the current HRMS workspace, apply these defaults unless the user explicitly overrides them:

### Tech Stack Defaults

- Backend: `.NET`
- Backend test framework preference: `xUnit`
- Frontend: `Vue 3 + Vite`
- Frontend test framework preference: `Vitest`

### Default Execution Order

Use this order to reduce noise and fix the most blocking issues first:

1. Read the relevant module and test setup
2. Run diagnostics on recently edited files
3. Fix compile or type errors first
4. Run the smallest relevant test scope
5. Fix failing tests
6. Re-run the changed scope
7. Run broader verification only after local issues are stable

### Backend Default Flow

For `.NET` modules, prefer this sequence:

1. Use editor diagnostics to catch obvious compile issues
2. Run focused backend tests with `dotnet test` on the relevant test project if available
3. If no tests exist and the change is substantial, create minimal `xUnit` coverage for the changed controller or service
4. Re-run focused tests after each repair
5. Run a broader backend test pass only after focused tests are green

### Frontend Default Flow

For `Vue 3 + Vite` modules, prefer this sequence:

1. Use editor diagnostics on modified `.vue`, `.js`, or `.ts` files
2. Run focused frontend tests with `Vitest` if configured
3. If tests are missing and the change is substantial, add minimal component or logic tests around the modified behavior
4. Re-run focused tests after each repair
5. Use broader validation only after diagnostics and focused tests are stable

### Diagnostics First Rule

Before running broader test suites, always check diagnostics on the files you changed.

In this workspace, prioritize:

1. `GetDiagnostics` on edited files
2. Focused test execution
3. Broader module-level tests
4. Full-suite tests only when justified

### Retry Policy

Apply these defaults in this repository:

- Maximum repair attempts for the same failure: `3`
- Maximum total repair rounds in one loop: `5`
- Stop early if two consecutive rounds produce no meaningful change

### Reporting Format

For this workspace, the final report should include:

- Scope tested
- Diagnostics status
- Tests run
- Failures found
- Fixes applied
- Remaining blockers

### Guardrails

- Do not add large test suites for trivial edits
- Prefer targeted tests over blanket coverage
- Do not rewrite unrelated modules during repair
- If a failure appears caused by pre-existing unrelated issues, report it clearly instead of forcing risky changes

## User Control Points

The user may:

- Provide custom test data
- Exclude specific test categories
- Change the maximum number of repair iterations
- Interrupt the auto-repair process and switch to manual handling

## Example

1. Implement the TodoCenter API
2. Create a test project such as `TodoCenter.Tests`
3. Generate controller and service tests
4. Run tests
5. Detect failing case `CompleteTask_WhenTaskNotExists_Returns404`
6. Identify missing null handling
7. Add `NotFound()` handling
8. Re-run the failing tests
9. Re-run the full suite
10. Output a final report
