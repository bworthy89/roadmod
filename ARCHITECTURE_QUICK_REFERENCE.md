# Cities: Skylines II - Architecture Quick Reference

## Quick Stats
- **Total Files:** 4,284 C# files
- **Framework:** .NET 4.0, C# 12.0
- **Architecture:** Unity ECS (Entity Component System)
- **Main Assembly:** Game.dll

## Top 20 Namespaces by Size

| Rank | Namespace | Files | Purpose |
|------|-----------|-------|---------|
| 1 | Game.Prefabs | 1,257 | Object templates & configuration |
| 2 | Game.Simulation | 480 | Core simulation logic |
| 3 | Game.UI.InGame | 218 | In-game user interface |
| 4 | Game.Rendering | 151 | Graphics & visual effects |
| 5 | Game.Net | 148 | Roads & network infrastructure |
| 6 | Game.Buildings | 145 | Building systems & AI |
| 7 | Game.UI.Widgets | 134 | Reusable UI components |
| 8 | Game.Tools | 110 | Editor & building tools |
| 9 | Game.Vehicles | 92 | Vehicle simulation |
| 10 | Game.Prefabs.Modes | 86 | Game mode definitions |
| 11 | Game.Tutorials | 85 | Tutorial system |
| 12 | Game.UI.Editor | 84 | Map editor interface |
| 13 | Game.Objects | 84 | Generic game objects |
| 14 | Game.Pathfind | 80 | Pathfinding algorithms |
| 15 | Game.Settings | 75 | Game configuration |
| 16 | Game.Serialization | 73 | Save/load system |
| 17 | Game.Routes | 70 | Public transport routing |
| 18 | Game.Debug | 64 | Debug utilities |
| 19 | Game.Events | 63 | Event system |
| 20 | Game.Citizens | 62 | Citizen simulation |

## ECS Core Components

### Base Classes
```
GameSystemBase          → Base for all game systems
PrefabBase             → Base for all prefabs
ComponentBase          → Base for components
UpdateSystem           → System update orchestration
```

### Key Interfaces
```
IComponentData         → Simple data component
IBufferElementData     → Dynamic buffer component
ISharedComponentData   → Shared across entities
ISerializable          → Save/load support
IQueryTypeParameter    → ECS query support
```

## System Update Phases (28 Total)

### Main Phases
1. **MainLoop** - Primary game loop
2. **GameSimulation** - Core simulation tick
3. **Rendering** - Visual rendering
4. **UIUpdate** - UI refresh
5. **Cleanup** - Frame cleanup

### Modification Phases
- Modification1 through Modification5
- ModificationBarriers for structural changes
- ModificationEnd for finalization

### Specialized Phases
- PreSimulation / PostSimulation
- PreTool / PostTool / ToolUpdate
- PreCulling / CompleteRendering
- Serialize / Deserialize
- PrefabUpdate / PrefabReferences

## Core Technologies Stack

### Unity Components
- Unity.Entities (ECS Core)
- Unity.Mathematics (Math library)
- Unity.Collections (Native collections)
- Unity.Burst (AOT compiler)
- Unity.RenderPipelines.HighDefinition (HDRP)
- Unity.InputSystem (Input)
- Cinemachine (Cameras)

### Colossal Framework
- Colossal.Core
- Colossal.Entities
- Colossal.IO.AssetDatabase
- Colossal.Mathematics
- Colossal.Serialization.Entities
- Colossal.UI.Binding
- Colossal.Localization

### Third Party
- cohtml.Net (UI rendering)
- PDX.SDK (Paradox services)

## Key System Categories

### Simulation Systems (480 files)
```
Traffic Simulation
├── TransportCarAISystem
├── VehicleSpawnSystem
└── PathfindingIntegration

Economy Simulation
├── CompanyDividendSystem
├── ResourceExporterSystem
├── TaxationSystem
└── DemandSystem

Service Simulation
├── GarbageTruckAISystem
├── PoliceSystem
├── FireSystem
└── HealthcareSystem

Environment
├── PollutionSystem
├── WeatherSystem
└── TimeSystem
```

### Building Systems (145 files)
- Building placement & validation
- Building upgrades
- Service buildings
- Zone buildings (residential, commercial, industrial)

### Network Systems (148 files)
- Road networks
- Public transport networks
- Utility networks (water, power)
- Pathfinding integration

### Citizen Systems (62 files)
- Age simulation
- Employment/unemployment
- Education levels
- Health & happiness
- Leisure activities

## Citizen Data Structure
```csharp
struct Citizen {
    CitizenFlags m_State;        // State flags
    byte m_WellBeing;            // 0-255
    byte m_Health;               // 0-255
    byte m_LeisureCounter;       // Leisure tracking
    int m_UnemploymentCounter;   // Days unemployed
    short m_BirthDay;            // For age calculation

    // Computed
    int Happiness => (m_WellBeing + m_Health) / 2;
}
```

## Common Design Patterns

### 1. Component Pattern
```
Entity (ID only)
  └─> Components (Data)
       └─> Systems (Logic)
```

### 2. Prefab Pattern
```
PrefabBase (Template)
  └─> Components List
       ├─> DataComponent
       ├─> RenderComponent
       └─> AIComponent
```

### 3. System Registration
```csharp
updateSystem.UpdateAt<SystemType>(Phase);
updateSystem.UpdateBefore<SystemType>(Phase);
updateSystem.UpdateAfter<SystemType>(Phase);
```

### 4. Command Buffer Pattern
```
System Update
  └─> Create Commands
       └─> Barrier Executes
            └─> Structural Changes Applied
```

## Performance Optimizations

### Update Intervals
Systems can run at different frequencies:
- Every frame (interval = 1)
- Every 2 frames (interval = 2)
- Every 4 frames (interval = 4)
- etc.

### Job System
- IJobChunk for parallel entity processing
- Burst compilation for critical paths
- Native collections for cache efficiency

### Memory Management
- Struct-based components (no GC)
- Object pooling
- Native memory allocation

## UI Architecture

### Framework
- **cohtml.Net** for rendering
- HTML/CSS-based UI
- C# ↔ JavaScript bindings

### Major UI Systems
```
InGame UI (218 files)
├── City Information
├── Building Panels
├── Resource Overlays
└── Transportation Views

Editor UI (84 files)
├── Map Editor
├── Asset Browser
└── Terrain Tools

Widgets (134 files)
└── Reusable Components
```

## Rendering Pipeline

### Stages
1. PreRender (data preparation)
2. Culling (visibility determination)
3. Batching (draw call optimization)
4. Rendering (GPU execution)
5. Post-processing (effects)
6. CompleteRendering (finalization)

### Features
- HDRP (High Definition Render Pipeline)
- Dynamic lighting & shadows
- LOD system
- Weather effects
- Volumetric effects

## Modding Support

### Components
```
Game.Modding
├── Mod loading system
├── Assembly management
└── API exposure

Game.Modding.Toolchain
├── Build tools
├── Asset pipeline
└── Dependency management
```

## File Locations

### Core Files
```
New folder/
├── Game.csproj (Project file)
├── Game/
│   ├── GameSystemBase.cs (System base)
│   ├── UpdateSystem.cs (Update orchestration)
│   └── SystemUpdatePhase.cs (Phase definitions)
├── Game.Prefabs/
│   └── PrefabBase.cs (Prefab base)
├── Game.Common/
│   └── SystemOrder.cs (System registration)
└── Game.Simulation/
    └── (480 simulation systems)
```

## Development Workflow

### System Creation Pattern
```csharp
1. Create system class extending GameSystemBase
2. Override OnCreate() for initialization
3. Override OnUpdate() for logic
4. Register in SystemOrder.Initialize()
5. Specify update phase and interval
```

### Component Creation Pattern
```csharp
1. Create struct implementing IComponentData
2. Add fields (keep small for cache efficiency)
3. Implement ISerializable if needed
4. Add to archetype via GetPrefabComponents()
```

## Common Component Types

### Standard Components
- **Owner** - Reference to owning entity
- **PrefabRef** - Reference to prefab template
- **Transform** - Position, rotation, scale
- **Updated** - Marks entity as changed

### Citizen Components
- Citizen (main data)
- HouseholdMember
- Worker
- Student
- TravelPurpose

### Building Components
- Building (main data)
- Renter
- PropertySeeker
- ServiceConsumption
- Efficiency

### Vehicle Components
- Vehicle (main data)
- Car, Train, Aircraft, etc.
- PathOwner
- Target
- Odometer

## Query Examples

### Basic Query
```csharp
EntityQuery query = GetEntityQuery(
    ComponentType.ReadOnly<Citizen>(),
    ComponentType.ReadWrite<TravelPurpose>()
);
```

### With Exclusions
```csharp
EntityQuery query = GetEntityQuery(
    ComponentType.ReadOnly<Building>(),
    ComponentType.Exclude<Deleted>()
);
```

## Best Practices

### Performance
- Use Burst-compiled jobs for hot paths
- Minimize component size
- Use SharedComponentData when appropriate
- Batch operations in command buffers

### Architecture
- Keep systems focused on single responsibility
- Use events for cross-system communication
- Leverage prefab system for configuration
- Use proper update phases

### Memory
- Avoid managed allocations in jobs
- Use NativeContainers for temporary data
- Dispose native collections properly
- Prefer struct over class for components

## Debugging Tools

### Available Systems
- DebugWatchSystem (runtime inspection)
- GizmosSystem (visual debugging)
- DebugUISystem (debug overlays)
- Performance profilers

### Debug Namespaces
- Game.Debug (64 files)
- Game.UI.Debug
- Game.Rendering.Debug

---

## Quick Command Reference

### Find System
```bash
find "New folder" -name "*SystemName*System.cs"
```

### Count Namespace Files
```bash
find "New folder/Game.Namespace" -name "*.cs" | wc -l
```

### Search for Component
```bash
grep -r "struct ComponentName" "New folder/"
```

---

**For detailed information, see:** REVERSE_ENGINEERING_DOCUMENTATION.md
