# Cities: Skylines II Systems Development Guide

## What are Systems?

In Cities: Skylines II, **systems** are classes that extend `SystemBase`, or ultimately `ComponentSystemBase`. They act as the central driving factor behind all events and actions in the game.

Systems are:
- Created within the **World**
- Assigned to a specific **update phase**
- Responsible for managing and bringing life to entities in the ECS ecosystem

### Examples of System Responsibilities
- **Transaction**: A store sold something → A system ensured the transaction was made
- **Citizen Death**: Someone died of old age → A system's Random probably killed them
- **Pollution Reduction**: Want to reduce air pollution from a building → A system can do that
- **Tree Growth**: Want to accelerate tree growth → A system can do that

---

## System Types

The following are abstract classes - when we say "using a system", it means extending the system to create your own.

### 1. SystemBase

`SystemBase` is the basic system that comes with Unity. All other systems are derived from it.

**Use when:**
- You don't need extra functions from other system types
- Avoiding bloat is important

**Resources:**
- [Unity ECS Documentation](https://docs.unity3d.com/Packages/com.unity.entities@latest)

---

### 2. GameSystemBase

`GameSystemBase` is Colossal Order's generic base system. It extends `COSystemBase` (which only includes verbose logs).

Used almost everywhere in the game, offering several useful override methods:

#### Game Loading Methods
```csharp
// Set up anything your system needs within a save-game
protected override void OnGamePreload(Purpose purpose, GameMode mode) { }

protected override void OnGameLoaded(Context serializationContext) { }

protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode) { }
```

#### Focus Change Method
```csharp
// Notifies when the game window goes in/out of focus
protected override void OnFocusChanged(bool hasFocus) { }
```

#### Game Simulation Interval & Offset
```csharp
// Perfect if your system only needs to run every few ticks during simulation
// IMPORTANT: The returned interval must be a power of 2
// IMPORTANT: The offset must be 0 <= offset < interval
public override int GetUpdateInterval(SystemUpdatePhase phase)
{
    // One day (or month) in-game is '262144' ticks
    return 262144 / updatesPerDay;
}

public override int GetUpdateOffset(SystemUpdatePhase phase)
{
    return -1;
}
```

---

### 3. ToolBaseSystem

`ToolBaseSystem` is used to create Tools in Cities: Skylines II.

**Examples of vanilla tools:**
- Bulldoze tool
- Area tool
- Terrain tool

**Important:** Only one tool system can be active at a time.

See the [Tool System Guide](tool.md) for more information.

---

### 4. TooltipSystemBase

`TooltipSystemBase` is used to add tooltips to the player's cursor.

**Usage:**
Use `AddMouseTooltip()` or `AddGroup()` methods inside your `OnUpdate()` method.

**Available tooltip classes:**
- `StringTooltip`
- `IntTooltip`
- And more based on what you're displaying

**Example:**
```csharp
// From the game's Bulldoze tooltip system
protected override void OnUpdate()
{
    AddMouseTooltip(new StringTooltip { value = "Bulldoze" });
}
```

---

### 5. UISystemBase

`UISystemBase` serves as middleware providing binding between the UI and game systems.

**Capabilities:**
- Send data to the UI
- Receive user input from the UI

**Note:** A dedicated guide for UI modding & UISystemBase will come soon™

---

## Update Phases

Every system exists within a `SystemUpdatePhase`. When creating a system in your mod's `OnCreateWorld()`, you must specify which phase your system will update at.

### Registering Systems at Update Phases

```csharp
public class Mod : IMod
{
    public void OnLoad(UpdateSystem updateSystem)
    {
        // Run before the phase
        updateSystem.UpdateBefore<MySystem1>(SystemUpdatePhase.Modification3);

        // Run at the phase
        updateSystem.UpdateAt<MySystem2>(SystemUpdatePhase.Modification3);

        // Run after the phase
        updateSystem.UpdateAfter<MySystem3>(SystemUpdatePhase.Modification3);
    }
}
```

---

## Choosing an Update Phase

Selecting the appropriate phase can be confusing. It's ultimately a case-by-case decision, but here are guidelines for important phases:

### General Purpose Phases

| Phase | Use Case |
|-------|----------|
| `SystemUpdatePhase.ModificationXX` | Systems that create or change component data of entities, independent of game simulation |
| `SystemUpdatePhase.PreSimulation` | Run something before the game's simulation |
| `SystemUpdatePhase.PostSimulation` | Run something after the game's simulation |

### Simulation-Specific Phases

| Phase | Use Case |
|-------|----------|
| `SystemUpdatePhase.GameSimulation` | Loaded save-game's simulation |
| `SystemUpdatePhase.EditorSimulation` | Loaded editor's simulation |
| `SystemUpdatePhase.LoadSimulation` | While a save-game is loading (executes 8 times in a row) |
| `SystemUpdatePhase.Serialize` | While a save-game or map is being saved |

### System-Type Specific Phases

| System Type | Required Phase |
|-------------|----------------|
| `ToolBaseSystem` | `SystemUpdatePhase.ToolUpdate` |
| `TooltipSystemBase` | `SystemUpdatePhase.UITooltip` |
| `UISystemBase` | `SystemUpdatePhase.UIUpdate` |

---

## Update Phases Order

The update phases execute in a specific order each frame. The exact order and timing is critical for ensuring systems interact correctly.

*(Infographic pending)*

---

## Best Practices

1. **Choose the right system type**: Don't extend `GameSystemBase` if you only need `SystemBase` functionality
2. **Update intervals must be powers of 2**: When using `GetUpdateInterval()`, ensure the value is 2, 4, 8, 16, 32, etc.
3. **Validate offset constraints**: Ensure `0 <= offset < interval` when using `GetUpdateOffset()`
4. **One tool at a time**: Remember that only one `ToolBaseSystem` can be active simultaneously
5. **Match system type to phase**: Always use the correct phase for specialized system types (Tool, Tooltip, UI)
6. **Consider performance**: Use update intervals for systems that don't need to run every frame
7. **Understand ECS**: Systems work with entities and components - familiarize yourself with Entity Component System patterns

---

## Quick Reference

### Game Time
- One in-game day (or month) = **262,144 ticks**
- Use this for calculating `GetUpdateInterval()` values

### Common Patterns
```csharp
// System that runs twice per in-game day
public override int GetUpdateInterval(SystemUpdatePhase phase)
{
    return 262144 / 2; // = 131072
}
```

---

## See Also

- [Tool System Guide](tool.md) - Detailed guide on creating tool systems
- Unity ECS Documentation - For understanding the underlying Entity Component System
- Colossal Order Modding Documentation - Official modding resources
