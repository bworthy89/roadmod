# Cities: Skylines II Tool Mod Development Guide

## Definition

A **tool** in Cities: Skylines II refers to systems that allow the player to directly manipulate and access attributes of their city for a desired result. Examples include:
- Bulldoze Tool
- Zone Tool
- Selection Tool

## The Tool Lifecycle

The lifecycle of a tool is managed by `Game.Tools.ToolSystem`. The `Game.Tools.DefaultToolSystem` is active on simulation startup and when no other tool is running.

## The ToolBaseSystem

All tools implement the `ToolBaseSystem` abstract class and are run during `SystemUpdatePhase.ToolUpdate`.

---

## Lifecycle Methods

### 1. System Creation

```csharp
protected override void OnCreate()
{
    // IMPORTANT: Set Enabled to false to avoid overwriting the Default tool
    Enabled = false;

    // Input Action for left click/press (may work differently for game pads)
    m_ApplyAction = InputManager.instance.FindAction("Tool", "Apply");

    // Input Action for right click/press (may work differently for game pads)
    m_SecondaryApplyAction = InputManager.instance.FindAction("Tool", "Secondary Apply");

    // Setup a hot key (no standards set yet)
    InputAction hotKey = new ("MyHotKey");
    hotKey.AddCompositeBinding("ButtonWithOneModifier")
        .With("Modifier", "<Keyboard>/ctrl")
        .With("Button", "<Keyboard>/t");
    hotKey.performed += this.OnKeyPressed;
    hotKey.Enable();

    base.OnCreate();
}
```

**Use OnCreate to:**
- Set up keyboard hotkeys
- Get reference to input actions from InputManager
- **IMPORTANT:** Set `Enabled = false` to avoid overwriting the default tool

---

### 2. Starting and Stopping Running

```csharp
// Sets up actions when your tool becomes active
protected override void OnStartRunning()
{
    m_ApplyAction.shouldBeEnabled = true;
    m_SecondaryApplyAction.shouldBeEnabled = true;
}

// Cleans up actions when your tool becomes inactive
protected override void OnStopRunning()
{
    m_ApplyAction.shouldBeEnabled = false;
    m_SecondaryApplyAction.shouldBeEnabled = false;
}
```

---

### 3. Activating Your Tool

The `ToolSystem.ActivatePrefabTool(PrefabBase prefab)` method is responsible for activating tools. When the player selects an object via the game UI, this method cycles through each tool system and activates the first tool that works with the prefab.

#### TrySetPrefab Implementation

```csharp
/**
 * Activates the tool if it's a building prefab
 * Add any other conditions for activation (e.g., progression level)
 **/
public override bool TrySetPrefab(PrefabBase prefab)
{
    BuildingPrefab buildingPrefab = prefab as BuildingPrefab;
    if (buildingPrefab != null)
    {
        this.prefab = buildingPrefab;
        return true;
    }
    return false;
}
```

**Note:** You can use Harmony on a vanilla tool system's `TrySetPrefab` to prevent the UI from activating that tool while your tool is active.

---

### 4. Sharing the Selected Prefab

```csharp
public override PrefabBase GetPrefab()
{
    return this.prefab;
}
```

This method triggers the UI menu for the returned prefab. The UI Toolbar calls this to see which prefab is selected and highlights it.

---

### 5. Letting User Select Objects (Raycasting)

```csharp
public override void InitializeRaycast()
{
    // Configure raycast settings for this frame
    // Cannot change until the next frame
}
```

Raycasting is how the user targets modeled entities and surfaces within the playable area. Configure filters through various `ToolRaycastSystem` properties.

#### Raycast Filter Options

| Field | Enum Type | Purpose |
|-------|-----------|---------|
| `raycastFlags` | `Game.Common.RaycastFlags` | DebugDisable, UIDisable, ToolDisable, FreeCameraDisable, ElevateOffset, SubElements, Placeholders, Markers, NoMainElements, UpgradeIsMain, OutsideConnections, Outside, Cargo, Passenger, Decals, EditorContainers |
| `typeMask` | `Game.Common.TypeMask` | Terrain, StaticObjects, MovingObjects, Net, Zones, Areas, RouteWaypoints, RouteSegments, Labels, Water, Icons, WaterSources, Lanes |
| `collisionMask` | `Game.Common.CollisionMask` | OnGround, Overground, Underground, ExclusiveGround |
| `netLayerMask` | `Game.Net.Layer` | Road, PowerlineLow, PowerlineHigh, WaterPipe, SewagePipe, StormwaterPipe, TrainTrack, Pathway, Waterway, Taxiway, TramTrack, SubwayTrack, Fence, MarkerPathway, MarkerTaxiway, PublicTransportRoad, LaneEditor |
| `areaTypeMask` | `Game.Areas.AreaTypeMask` | Lots, Districts, MapTiles, Spaces, Surfaces |
| `routeType` | `Game.Routes.RouteType` | TransportLine |
| `transportType` | `Game.Prefabs.TransportType` | Bus, Train, Taxi, Tram, Ship, Post, Helicopter, Airplane, Subway, Rocket |
| `iconLayerMask` | `Game.Notifications.IconLayerMask` | Default, Marker, Transaction |
| `utilityTypeMask` | `Game.Net.UtilityTypes` | WaterPipe, SewagePipe, StormwaterPipe, LowVoltageLine, Fence, Catenary, HighVoltageLine |

#### Performing Raycasts

```csharp
bool GetRaycastResult(out Entity entity, out RaycastHit hit)
```

---

### 6. Responding to Hotkey Presses

```csharp
/// <summary>
/// Add keybinding to tool so you can enable tool in game
/// </summary>
private void OnKeyPressed(InputAction.CallbackContext context)
{
    if (m_ToolSystem.activeTool != this && m_ToolSystem.activeTool == m_DefaultToolSystem)
    {
        m_ToolSystem.selected = Entity.Null;
        m_ToolSystem.activeTool = this;
    }
}
```

---

### 7. System Update

```csharp
protected override JobHandle OnUpdate(JobHandle inputDeps)
{
    bool raycastFlag = GetRaycastResult(out Entity e, out RaycastHit hit);

    if (m_ApplyAction.WasPressedThisFrame() && raycastFlag)
    {
        // Do something with entity E
    }

    // Return job handle for any scheduled work
    return inputDeps;
}
```

**Important:** Manage job dependencies and handles properly. Return the final job handle for any work done.

---

### 8. Disable the Tool

```csharp
public void RequestDisable()
{
    m_ToolSystem.activeTool = m_DefaultToolSystem;
}
```

Set the `activeTool` field of `ToolSystem` to the default tool system to disable your tool.

---

## Key Systems and References

- **ToolBaseSystem**: Base class for all tools (already registers itself with ToolSystem)
- **ToolSystem**: Manages tool lifecycle and activation
- **DefaultToolSystem**: Active when no other tool is running
- **InputManager**: Provides access to input actions
- **ToolRaycastSystem**: Handles raycasting configuration and execution

---

## Best Practices

1. Always set `Enabled = false` in `OnCreate()` unless overriding the default tool
2. Configure raycasts in `InitializeRaycast()` - they cannot change mid-frame
3. Manage JobHandle dependencies properly in `OnUpdate()`
4. Use Harmony patches carefully when overriding vanilla tool behavior
5. Only disable vanilla tools while your tool is active
6. Return appropriate job handles from `OnUpdate()`
