# AGENTS.md

This file provides guidance to coding agents working in this repository.

## Project Overview

This is a Unity Action RPG demo built with Unity 6.5.3f1 (6000.5.3f1). The player uses a
hierarchical finite state machine (HFSM) for movement and combat state
orchestration.

## Architecture

### State Machine

- `Assets/Scrips/StateMachine/StateMachine.cs` contains the shared state
  machine implementation.
- `PlayerMovementStateMachine` owns all concrete player movement and combat
  states.
- Concrete states follow the `Player[StateName]State` naming convention.

### Player Composition

- `Assets/Scrips/Characters/Player/Player.cs` is the player composition root.
- The Player GameObject owns the single `PlayerInput` instance.
- `Player` exposes the configured weapon through `IWeaponController`; weapon
  implementations do not subscribe to input independently.
- Player settings are stored in ScriptableObjects under
  `Assets/ScriptableObjects/Characters/Player/`.

### Combat

- `PlayerAttackState` orchestrates combo timing, animation transitions, and
  state changes only.
- `CombatExecutor` owns target acquisition, hit-box queries, combat event
  cursors, and attacker effects.
- Targets implement `IHitReceiver` and receive an immutable `HitContext`
  instead of attacker-owned configuration objects.
- `CombatControllerBase` is the current scene receiver implementation. It
  handles hit animation, target effects, and configured hit movement.
- `CombatEffectSpawner` is stateless and injected into `CombatExecutor`.
  Direct prefab instantiation remains the future object-pooling boundary.
- Do not add a global singleton, event bus, or second input owner without a
  concrete cross-system requirement.

### Data

- Animation data: `Assets/Scrips/Characters/Player/Data/Animation/`
- State data: `Assets/Scrips/Characters/Player/Data/States/`
- Layer data: `Assets/Scrips/Characters/Player/Data/Layers/`
- Combo assets: `Assets/ScriptableObjects/Characters/Player/CombatSO/`

## Development

### Unity Editor

- Use Unity 6.5.3f1 (6000.5.3f1).
- Primary scene: `Assets/Scenes/SampleScene.unity`
- Default target platform: Windows.

### Build Check

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

Unity generates the `.csproj` files. Refresh or regenerate them after adding
or removing scripts.

### Tests

- EditMode architecture tests:
  `Assets/Tests/Editor/CombatArchitectureTests.cs`
- Run them from Unity Test Runner in EditMode.
- The tests guard the combat contract, attack-state boundary, single input
  owner, scene serialization cleanup, and removal of obsolete infrastructure.

### Input

- Input actions are under `Assets/InputActions/`.
- Regenerate the C# class from Unity after changing an `.inputactions` asset.
- Input actions must be enabled and disabled in matching lifecycle methods.

## Conventions

- Data classes follow the `Player[Feature]Data` naming convention.
- ScriptableObjects use the existing `ScriptableObject/` menu paths.
- Keep state `Enter`/`Exit` subscriptions paired.
- Preserve serialized field names and script `.meta` files when moving
  Unity-owned resources.
- Prefer existing project patterns and keep changes scoped to the subsystem
  being modified.

## File Organization

```text
Assets/
|-- Animations/
|-- InputActions/
|-- Scenes/
|-- Scrips/
|   |-- Characters/Player/
|   |   |-- AttackSystem/
|   |   |-- Data/
|   |   |-- Player.cs
|   |   |-- Statemachine/Movement/
|   |   `-- Utilities/
|   |-- StateMachine/
|   `-- Weapons/
|-- ScriptableObjects/
`-- Tests/Editor/
```
