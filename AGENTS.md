# AGENTS.md

This file provides guidance to coding agents working in this repository.

## Project Overview

This is a Unity Action RPG demo built with Unity 6.5.3f1 (6000.5.3f1). The player uses a
hierarchical finite state machine (HFSM) for movement and combat state
orchestration.

`CLAUDE.md` holds the same guidance in more detail; keep both in sync when the
architecture changes.

Note: the scripts folder is `Assets/Scrips` (no 't'). This spelling is
intentional and used throughout the project.

Code identifiers are English; inline comments are written in Chinese. Match the
surrounding comment language when editing.

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
- `HitReceiverBase` is the default hit-reaction MonoBehaviour (faces the
  attacker, plays a force-indexed effect). It is not an Enemy base class; both
  player and enemy receivers subclass it. `EnemyHitReceiver` is the scene
  implementation `CombatExecutor` resolves via `GetComponentInParent`.
- `HitContext` carries only confirmed-hit facts (source position, forward,
  `AttackForce`, damage). Do not add hit-animation, weapon, or movement fields
  to it or to `ComboInteractionConfig` — those are receiver-owned.
- `CombatEffectSpawner` is stateless and injected into `CombatExecutor`.
  Direct prefab instantiation remains the future object-pooling boundary.
- `ComboConfig.Validate` throws on inconsistent authoring. Extend that
  validation rather than adding silent runtime fallbacks.
- Do not add a global singleton, event bus, or second input owner without a
  concrete cross-system requirement. The old `Singleton`/`MonoSingleton`
  infrastructure was removed; do not reintroduce it.

### Animation Motion

Horizontal movement for stop and attack animations is baked from Root Motion
offline and replayed at runtime. The only interface between the editor and
runtime halves is the serialized `AnimationMotionData`.

- `Data/Animation/AnimationMotionData.cs` is the serialized payload
  (`SpeedCurve`, `RotationCurve`, `BakedDuration`), read-only at runtime.
- `Player/Editor/AnimationMotionBaker/` is the bake pipeline: scanner →
  sampler → validator → writer, driven by `AnimationMotionBakerWindow`
  (menu: Tools → Animation → Animation Motion Baker). Settings persist to
  `ProjectSettings/AnimationMotionBakerSettings.asset`.
- `Player/Utilities/Motion/MotionDriver.cs` replays a curve onto the
  Rigidbody's horizontal velocity, preserving vertical velocity. `Stop()`
  intentionally does not zero velocity.
- Runtime code must not author `AnimationMotionData`, and bake code must not
  touch scene or runtime objects.

### Work In Progress

The branch is mid-migration from force-based deceleration to baked motion
curves. Superseded code is commented out behind
`TODO：MotionDriver PlayMode 验证通过后…` markers, and `PlayerStopData`'s
`*DecelerationForce` properties are `[Obsolete]` but still serialized. Do not
delete these as an incidental cleanup — the serialized values in `Player.asset`
must be cleared first, after PlayMode validation.

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

EditMode tests live in `Assets/Tests/Editor/` and run from the Unity Test
Runner in EditMode:

- `CombatArchitectureTests.cs` guards the combat contract, attack-state
  boundary, single input owner, scene serialization cleanup, and removal of
  obsolete infrastructure.
- `AnimationMotionDataTests.cs`, `AnimationMotionBakerTests.cs`, and
  `MotionDriverTests.cs` cover the motion bake pipeline and runtime driver.
- `Unity6MigrationBehaviorTests.cs` and `ForwardPlusToonShaderTests.cs` cover
  the Unity 6 migration (`Unity6Migration` category).

To run a single test or class from the CLI:

```powershell
& "<UnityEditorPath>\Unity.exe" -batchmode -runTests -projectPath . `
    -testPlatform EditMode -testFilter MotionDriverTests -logFile -
```

These are architectural guardrails, not behavior tests. Run them after touching
combat, input, motion, or scene structure.

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
|   |-- Characters/
|   |   |-- Combat/                     # HitContract, HitReceiverBase, spawner
|   |   |-- Enemy/
|   |   `-- Player/
|   |       |-- AttackSystem/           # CombatExecutor
|   |       |-- Data/
|   |       |-- Editor/                 # AnimationMotionBaker pipeline
|   |       |-- Player.cs
|   |       |-- Statemachine/Movement/
|   |       `-- Utilities/              # Input, Cameras, Colliders, Motion
|   |-- StateMachine/
|   `-- Weapons/
|-- ScriptableObjects/
`-- Tests/Editor/
```
