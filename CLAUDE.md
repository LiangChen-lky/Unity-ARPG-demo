# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity Action RPG (ARPG) demo project built with Unity 2022.3.62f1c1. The project implements a character controller with hierarchical finite state machine (HFSM) architecture for movement and combat systems.

## Architecture

### Core Patterns
- **Singleton Pattern**: `Assets/Scrips/Core/Patterns/Singleton/Singleton.cs` - Generic singleton implementation
- **MonoSingleton**: `Assets/Scrips/Core/Patterns/Singleton/MonoSingleton.cs` - MonoBehaviour-based singleton base class
- **State Machine**: `Assets/Scrips/StateMachine/StateMachine.cs` - Base state machine class used for player movement states
- **Hierarchical FSM**: Player movement uses a hierarchical state machine with parent `PlayerMovementStateMachine` and concrete state implementations

### Player Character System
- **Main Player Controller**: `Assets/Scrips/Characters/Player/Player.cs` - Main entry point, initializes state machine
- **State Machine**: `Assets/Scrips/Characters/Player/Statemachine/Movement/PlayerMovementStateMachine.cs` - Manages player movement states
- **Input System**: `Assets/Scrips/Characters/Player/Utilities/Input/PlayerInput.cs` - Wrapper for Unity Input System actions
- **Scriptable Object Data**: Player configuration uses ScriptableObjects (`.asset` files) for data-driven design

### Attack System
- **Combat Controller**: `Assets/Scrips/Characters/Player/AttackSystem/PlayerCombatController.cs` - Handles attack combos
- **Base Class**: `Assets/Scrips/Characters/Player/AttackSystem/CombatControllerBase.cs` - Shared combat functionality
- **Effect System**: `Assets/Scrips/Characters/Player/AttackSystem/Effect/` - Visual effects and hit FX management
- **Tool Manager**: `Assets/Scrips/Characters/Player/AttackSystem/Effect/ToolManager.cs` - Global utility for spawning effects (currently direct instantiation, marked for optimization with object pooling)

### Data Organization
- **Animation Data**: `Assets/Scrips/Characters/Player/Data/Animation/` - Animation configuration
- **State Data**: `Assets/Scrips/Characters/Player/Data/States/` - Per-state configuration (Idle, Run, Sprint, Dash, Jump, Attack, etc.)
- **ScriptableObjects**: `Assets/ScriptableObjects/Characters/Player/` - Player configuration assets
- **Layer Data**: `Assets/Scrips/Characters/Player/Data/Layers/PlayerLayerData.cs` - Layer mask utilities

### Key Dependencies (from manifest.json)
- Unity Input System (`com.unity.inputsystem`) - Modern input handling
- Cinemachine (`com.unity.cinemachine`) - Camera system
- Animation Rigging (`com.unity.animation.rigging`) - Animation constraints
- URP (`com.unity.render-pipelines.universal`) - Render pipeline
- MagicaCloth - Physics-based cloth simulation (third-party)

## Development Commands

### Unity Editor
- Open project in Unity 2022.3.62f1c1
- Primary scene: `Assets/Scenes/SampleScene.unity`

### Building
- Use Unity Build Settings (File → Build Settings)
- Target platform: Windows (default)

### Input System
- Input actions are defined in `Assets/InputActions/`
- Regenerate C# code after modifying input actions: Right-click `.inputactions` file → "Generate C# Class"

### Testing
- No automated test framework currently configured
- Manual testing through Unity Editor play mode

## Code Conventions

### Naming Patterns
- State classes follow `Player[StateName]State` convention (e.g., `PlayerIdlingState`, `PlayerRunningState`)
- Data classes follow `Player[Feature]Data` convention (e.g., `PlayerReusableData`, `PlayerAnimationData`)
- ScriptableObjects use `[CreateAssetMenu]` attribute with `ScriptableObject/` menu path

### State Implementation
- States implement `IState` interface (check `StateMachine.cs` for required methods)
- Movement states are in `Assets/Scrips/Characters/Player/Statemachine/Movement/State/`
- Attack states integrate with the movement state machine

### Extension Methods
- `ExpandClass.cs` provides extension methods like `GetMoveOffsetDirection()` for Transform

## Important Notes

### Recent Changes
- Attack system was recently combined into HFSM (see commit `cc6bd87`)
- Singleton pattern was moved to `Assets/Scrips/Core/Patterns/Singleton/` (see git status for moved files)

### Areas Marked for Optimization
- `ToolManager.PlayOneFX()` currently uses `Object.Instantiate()` - TODO: implement object pooling
- Effect system uses coroutines for lifetime management

### Scene Setup
- Player character uses capsule collider with utility class `CapsuleColliderUtility`
- Camera follows player via Cinemachine
- Input actions must be enabled/disabled properly (see `PlayerInput.cs`)

## File Organization

```
Assets/
├── Scrips/
│   ├── Characters/Player/           # Player character implementation
│   │   ├── Player.cs               # Main controller
│   │   ├── Statemachine/Movement/  # Movement state machine
│   │   ├── AttackSystem/           # Combat system
│   │   ├── Data/                   # Configuration data
│   │   └── Utilities/Input/        # Input handling
│   ├── StateMachine/               # Base state machine classes
│   └── Core/Patterns/              # Design patterns (Singleton, etc.)
├── ScriptableObjects/              # Data assets
├── Animations/                     # Animation clips
├── InputActions/                   # Input System definitions
└── Scenes/                         # Unity scenes
```