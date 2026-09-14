# Repository Guidelines

## Project Overview

This is a Unity 6.3 project using URP and the new Input System.

Unity version:

- Unity 6000.3.8f1
- URP 17.3.0
- Input System 1.18.0
- Unity Test Framework 1.6.0

The project is currently minimal and should be developed according to the game specification contained in `Docs/GAME_SPEC.md` when that file exists.

Codex is responsible for implementing, testing, debugging, and maintaining the project while preserving existing working behavior.

Do not stop after merely writing code.

For meaningful gameplay changes, validate the Unity project, inspect errors, fix problems, and repeat until the implementation is in a usable state.

---

# Project Structure

The current project contains a minimal Unity 6.3 URP scene.

`Assets/Scenes/SampleScene.unity` is currently the main enabled scene.

Current structure:

- `Assets/Scenes/`: Unity scenes.
- `Assets/Settings/`: URP render pipelines, renderers, and volume profiles.
- `Assets/InputSystem_Actions.inputactions`: shared Player and UI action maps.
- `Packages/`: dependency manifest and lockfile.
- `ProjectSettings/`: shared Unity project settings.

Use the following structure for new content:

```text
Assets/
├── Scripts/
│   ├── Player/
│   ├── Enemy/
│   ├── Game/
│   ├── UI/
│   └── Systems/
│
├── Prefabs/
│
├── Scenes/
│
├── Editor/
│   └── Codex/
│
├── Tests/
│   ├── EditMode/
│   └── PlayMode/
│
└── ScriptableObjects/
```

Do not create unnecessary folders.

Keep files grouped by responsibility rather than placing all scripts in one directory.

---

# Primary Development Goal

Implement the game described by `Docs/GAME_SPEC.md`.

When the specification is incomplete or ambiguous:

1. Preserve the core game concept.
2. Prefer the simplest playable implementation.
3. Avoid unnecessary architecture.
4. Make reasonable implementation decisions instead of blocking on minor ambiguity.
5. Record important assumptions in the final report.

Do not invent large new gameplay systems that are not supported by the specification.

---

# Development Workflow

For meaningful implementation tasks, follow this general loop:

1. Read the relevant existing files.
2. Understand the current architecture.
3. Inspect Git status when available.
4. Implement the smallest coherent change.
5. Allow Unity to compile the project.
6. Inspect compilation errors and Unity logs.
7. Run relevant automated tests.
8. Fix failures.
9. Repeat validation after fixes.
10. Report what changed and what remains unresolved.

Do not report a task as complete when known compilation errors caused by the change remain.

Do not assume code works merely because it compiles.

---

# Unity Editor

Use this Unity executable on macOS:

```sh
UNITY_EDITOR="/Applications/Unity/Hub/Editor/6000.3.8f1/Unity.app/Contents/MacOS/Unity"
```

Open the project with:

```sh
"$UNITY_EDITOR" -projectPath "$PWD"
```

Do not open multiple Unity Editor instances for the same project unless there is a specific reason.

Before running Unity in batch mode, ensure another Unity Editor instance is not locking the project when that would interfere with the command.

---

# Compilation Validation

After meaningful C# changes, validate that Unity can load and compile the project.

When appropriate, use batch mode and save the output to a readable log.

Example:

```sh
mkdir -p Logs

"$UNITY_EDITOR" \
  -batchmode \
  -quit \
  -projectPath "$PWD" \
  -logFile "$PWD/Logs/codex-unity.log"
```

After running Unity:

- inspect the exit status;
- inspect the generated log;
- search for compilation errors;
- investigate exceptions relevant to the change.

Do not ignore errors merely because Unity exits successfully.

Warnings should be investigated when they indicate incorrect behavior, deprecated APIs, broken references, or unsafe code.

---

# Automated Testing

Unity Test Framework 1.6.0 is installed.

Add automated tests when the behavior can reasonably be tested without excessive complexity.

Prefer:

## Edit Mode Tests

Use Edit Mode tests for:

- game rules;
- calculations;
- scoring;
- state transitions;
- data transformations;
- pure C# logic;
- deterministic systems.

## Play Mode Tests

Use Play Mode tests for:

- GameObject interactions;
- MonoBehaviour lifecycle behavior;
- physics;
- spawning;
- destruction;
- scene behavior;
- gameplay interactions;
- systems that depend on Unity runtime behavior.

Use the naming convention:

```text
Method_Condition_ExpectedResult
```

Example Edit Mode test command:

```sh
"$UNITY_EDITOR" \
  -batchmode \
  -projectPath "$PWD" \
  -runTests \
  -testPlatform EditMode \
  -testResults /tmp/unity-editmode.xml \
  -logFile /tmp/unity-editmode.log
```

Example Play Mode test command:

```sh
"$UNITY_EDITOR" \
  -batchmode \
  -projectPath "$PWD" \
  -runTests \
  -testPlatform PlayMode \
  -testResults /tmp/unity-playmode.xml \
  -logFile /tmp/unity-playmode.log
```

Inspect both the XML results and Unity logs when a test fails.

Do not repeatedly run the entire test suite when a smaller relevant test can diagnose the issue more efficiently.

---

# Gameplay Validation

When implementing gameplay, verify more than compilation.

Check relevant cases such as:

- the feature can actually be triggered;
- required input is recognized;
- important game states are reachable;
- spawning works;
- destruction works;
- scoring works;
- game-over conditions work;
- scene transitions work;
- object references are assigned correctly;
- null references are handled;
- repeated actions do not create unintended duplicate objects;
- objects do not remain active after they should be destroyed or disabled;
- Input System behavior is correct;
- physics interactions behave consistently.

Add automated tests for deterministic behavior when practical.

Use Play Mode validation for behavior that cannot be reliably verified in Edit Mode.

---

# Scene and Prefab Automation

Avoid fragile manual Unity Editor work when the same operation can reliably be implemented through Unity Editor scripting.

Editor-only automation belongs in:

```text
Assets/Editor/Codex/
```

Use Unity Editor APIs when useful for:

- creating scenes;
- creating GameObjects;
- configuring components;
- creating Prefabs;
- creating ScriptableObjects;
- assigning references;
- configuring project content;
- validating required objects;
- generating prototype scenes.

Editor scripts must not be referenced by runtime assemblies.

When creating or modifying scenes programmatically, save changes through Unity Editor APIs.

When creating Prefabs programmatically, use the appropriate `PrefabUtility` APIs.

Do not modify `.unity`, `.prefab`, `.asset`, or `.meta` YAML manually unless there is a strong reason and the change is understood.

Prefer Unity APIs for serialized Unity assets.

---

# Runtime Code Architecture

Prefer small components with clear responsibilities.

Separate concerns such as:

- input;
- gameplay logic;
- movement;
- presentation;
- UI;
- game state;
- spawning;
- data.

Avoid creating a single large `GameManager` containing unrelated systems.

Use plain C# classes for logic that does not require `MonoBehaviour`.

Use `ScriptableObject` when data should be shared, configurable, or edited in the Inspector.

Avoid unnecessary singleton patterns and global mutable state.

Prefer explicit references and clearly defined dependencies.

---

# Coding Style

Use four-space indentation.

Use:

- PascalCase for types;
- PascalCase for methods;
- PascalCase for properties;
- camelCase for local variables;
- camelCase for parameters;
- camelCase for private fields.

For Inspector configuration, prefer:

```csharp
[SerializeField] private float moveSpeed;
```

instead of unnecessarily public fields.

Match `MonoBehaviour` filenames to class names.

Example:

```text
BallController.cs
```

contains:

```csharp
public class BallController : MonoBehaviour
```

Use descriptive names.

Avoid abbreviated names unless the abbreviation is obvious.

Do not add comments that merely repeat what the code already says.

Comment behavior, assumptions, and non-obvious decisions instead.

---

# Input

Use the new Unity Input System.

Legacy `UnityEngine.Input` APIs should not be introduced unless explicitly required.

Existing input configuration is stored in:

```text
Assets/InputSystem_Actions.inputactions
```

Before changing input configuration, inspect existing action maps and bindings.

Do not remove existing bindings unless they are intentionally being replaced.

---

# Asset Safety

Preserve `.meta` files and GUIDs.

Do not unnecessarily delete and recreate Unity assets because doing so may change GUIDs and break references.

Do not manually modify generated Unity directories.

Never treat these directories as source code:

```text
Library/
Temp/
Logs/
Obj/
```

Do not commit them.

Avoid unrelated changes to:

- render pipeline assets;
- package versions;
- ProjectSettings;
- URP settings;
- graphics settings;
- volume profiles.

Investigate unresolved references before replacing or deleting assets.

Do not upgrade Unity packages unless the task specifically requires it.

---

# Existing Work Safety

Before making large changes, inspect the existing project.

Do not overwrite unrelated user work.

Do not delete existing scripts, scenes, Prefabs, assets, or gameplay systems merely because a cleaner implementation is possible.

Prefer incremental changes unless restructuring is specifically required.

If an existing system appears broken, investigate its purpose before replacing it.

When uncertain whether a file is user-created or generated, preserve it until its role is understood.

---

# Git

When Git is available, inspect:

```sh
git status
```

before large modifications.

Do not overwrite unrelated uncommitted changes.

Keep changes focused on the requested task.

Do not commit:

```text
Library/
Temp/
Logs/
Obj/
```

Preserve Unity `.meta` files.

Use concise imperative commit messages when commits are requested.

Examples:

```text
Add enemy spawning system
Implement keyboard attack input
Fix score reset behavior
```

Do not create commits unless requested or clearly part of the assigned workflow.

---

# Visual Changes

For meaningful visual changes, verify the resulting Unity scene when practical.

Check:

- camera framing;
- object visibility;
- UI readability;
- sorting layers;
- Canvas configuration;
- missing sprites or materials;
- incorrect scaling;
- obvious layout problems.

Screenshots may be used when useful for validating visual work.

Do not treat automated code tests as sufficient validation for visual presentation.

---

# Performance

For prototype development, prioritize correctness and playability over premature optimization.

However, avoid obviously expensive patterns such as:

- repeated `FindObjectOfType`-style searches every frame;
- unnecessary allocations inside `Update`;
- uncontrolled object spawning;
- repeated asset loading;
- unnecessary Instantiate/Destroy loops when they create obvious performance problems.

Only introduce pooling or advanced optimization when the project needs it.

---

# Error Handling

Investigate:

- compilation errors;
- `NullReferenceException`;
- missing component errors;
- missing asset references;
- scene loading failures;
- Input System exceptions;
- physics setup problems;
- serialization errors.

Fix the root cause when possible instead of suppressing errors.

Do not hide exceptions solely to make the Console appear clean.

---

# Completion Criteria

A gameplay task should normally be considered complete only when:

- the requested functionality is implemented;
- Unity compiles without new errors;
- relevant automated tests pass;
- relevant gameplay behavior has been validated;
- no new significant Console errors remain;
- existing unrelated functionality has not knowingly been broken.

For prototype tasks, prioritize reaching a functioning playable game loop.

---

# Final Report

After completing a substantial task, report:

## Implemented

Describe the gameplay or technical behavior added.

## Changed Files

List important files created or modified.

## Validation

Report:

- Unity compilation status;
- Edit Mode tests;
- Play Mode tests;
- manual or automated gameplay validation.

## Remaining Issues

Clearly identify anything that could not be verified or remains incomplete.

Do not claim a feature was manually tested if it was not actually tested.

Do not claim Unity was successfully launched if it was not actually launched.
