# Cities: Skylines II - Comprehensive Reverse Engineering Documentation

## Table of Contents
1. [Executive Summary](#executive-summary)
2. [Project Overview](#project-overview)
3. [Architecture Overview](#architecture-overview)
4. [Core Technologies](#core-technologies)
5. [Codebase Structure](#codebase-structure)
6. [Entity Component System (ECS) Architecture](#entity-component-system-ecs-architecture)
7. [System Update Pipeline](#system-update-pipeline)
8. [Core Game Systems](#core-game-systems)
9. [Prefab System](#prefab-system)
10. [Data Structures](#data-structures)
11. [Simulation Systems](#simulation-systems)
12. [Rendering Pipeline](#rendering-pipeline)
13. [UI Architecture](#ui-architecture)
14. [Modding Framework](#modding-framework)
15. [Key Classes and Components](#key-classes-and-components)
16. [Development Insights](#development-insights)

---

## Executive Summary

This document provides a comprehensive reverse engineering analysis of the **Cities: Skylines II** game codebase, located in the "New folder" directory. The analysis reveals a sophisticated city simulation game built on Unity's Data-Oriented Technology Stack (DOTS) using the Entity Component System (ECS) architecture.

**Key Statistics:**
- **Total C# Files:** 4,284
- **Target Framework:** .NET Framework 4.0
- **Language Version:** C# 12.0
- **Architecture:** Unity ECS (Entity Component System)
- **Primary Engine:** Unity Engine with High Definition Render Pipeline (HDRP)

---

## Project Overview

### Game Information
- **Name:** Cities: Skylines II
- **Developer:** Colossal Order
- **Engine:** Unity Engine (Custom DOTS/ECS Implementation)
- **Genre:** City Building Simulation
- **Assembly Name:** Game.dll

### Project Configuration
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <AssemblyName>Game</AssemblyName>
    <TargetFramework>net40</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <AllowUnsafeBlocks>True</AllowUnsafeBlocks>
  </PropertyGroup>
</Project>
```

---

## Architecture Overview

### High-Level Architecture

Cities: Skylines II is built using a **Data-Oriented Design** approach with Unity's Entity Component System (ECS). This architecture provides:

1. **Performance Optimization:** Cache-friendly data layouts for efficient CPU processing
2. **Parallelization:** Job system integration for multi-threaded execution
3. **Scalability:** Ability to simulate large cities with millions of entities
4. **Modularity:** Clean separation of data and behavior through components and systems

### Architectural Layers

```
┌─────────────────────────────────────────────┐
│          Presentation Layer (UI)            │
│  (Cohtml.Net, UI Bindings, Localization)   │
├─────────────────────────────────────────────┤
│        Rendering Layer (HDRP)               │
│  (Meshes, Materials, Lighting, Effects)    │
├─────────────────────────────────────────────┤
│         Game Logic Layer                    │
│  (Systems, AI, Simulation, Pathfinding)    │
├─────────────────────────────────────────────┤
│      ECS Core (Unity.Entities)              │
│  (Entities, Components, Archetypes)         │
├─────────────────────────────────────────────┤
│         Data & Prefab Layer                 │
│  (Prefabs, Configuration, Serialization)   │
└─────────────────────────────────────────────┘
```

---

## Core Technologies

### Primary Dependencies

1. **Unity.Entities** - Core ECS framework
2. **Unity.Mathematics** - High-performance math library
3. **Unity.Collections** - Native collection types
4. **Unity.Burst** - AOT compiler for high-performance code
5. **Unity.RenderPipelines.HighDefinition** - Advanced rendering
6. **Colossal.* Libraries** - Custom game engine components
7. **cohtml.Net** - UI rendering framework
8. **Cinemachine** - Camera system
9. **PDX.SDK** - Paradox SDK integration

### Colossal Framework Components
- **Colossal.Core** - Core utilities
- **Colossal.IO.AssetDatabase** - Asset management
- **Colossal.Mathematics** - Custom math utilities
- **Colossal.Serialization.Entities** - Save/load system
- **Colossal.Entities** - Extended ECS functionality
- **Colossal.UI.Binding** - UI data binding
- **Colossal.Localization** - Multi-language support

---

## Codebase Structure

### Directory Layout (by file count)

| Namespace | Files | Primary Purpose |
|-----------|-------|----------------|
| **Game.Prefabs** | 1,257 | Prefab definitions and data structures |
| **Game.Simulation** | 480 | Core simulation systems (traffic, economy, etc.) |
| **Game.UI.InGame** | 218 | In-game UI components |
| **Game.Rendering** | 151 | Rendering systems and effects |
| **Game.Net** | 148 | Road/network infrastructure |
| **Game.Buildings** | 145 | Building systems and AI |
| **Game.UI.Widgets** | 134 | Reusable UI widgets |
| **Game.Tools** | 110 | Editor and building tools |
| **Game.Vehicles** | 92 | Vehicle simulation and AI |
| **Game.Prefabs.Modes** | 86 | Game mode definitions |
| **Game.Tutorials** | 85 | Tutorial system |
| **Game.UI.Editor** | 84 | Map editor UI |
| **Game.Objects** | 84 | Generic game objects |
| **Game.Pathfind** | 80 | Pathfinding algorithms |
| **Game.Settings** | 75 | Game configuration |
| **Game.Serialization** | 73 | Save/load functionality |
| **Game.Routes** | 70 | Public transport routes |
| **Game.Debug** | 64 | Debug utilities |
| **Game.Events** | 63 | Event system |
| **Game.Citizens** | 62 | Citizen simulation |

### Additional Namespaces
- **Game.Zones** - Zoning system
- **Game.Economy** - Economic simulation
- **Game.City** - City-level management
- **Game.Areas** - Area management
- **Game.Policies** - Policy system
- **Game.Companies** - Business simulation
- **Game.Audio** - Audio management
- **Game.Effects** - Visual effects
- **Game.Creatures** - Animals and wildlife
- **Game.CinematicCamera** - Cinematic camera system
- **Game.Achievements** - Achievement tracking
- **Game.Modding** - Modding support
- **Game.PSI** - Platform Services Integration

### Root Level Files
- `-BurstDirectCallInitializer.cs` - Burst compilation initializer
- `GameSystemBase.cs` - Base class for all game systems
- `UpdateSystem.cs` - System update orchestrator
- `SystemUpdatePhase.cs` - Update phase definitions
- `GameMode.cs` - Game mode enumeration
- `Version.cs` - Version management
- `CameraController.cs` - Camera control
- `UICursorCollection.cs` - UI cursor definitions
- `UnitySourceGeneratedAssemblyMonoScriptTypes_v1.cs` - Unity generated code

---

## Entity Component System (ECS) Architecture

### Core ECS Concepts

Cities: Skylines II uses Unity's DOTS (Data-Oriented Technology Stack) ECS architecture:

#### 1. **Entities**
Entities are lightweight identifiers that represent game objects. They don't contain data or behavior themselves.

```csharp
// Example: Entity is just an ID
Entity citizenEntity;
Entity buildingEntity;
Entity vehicleEntity;
```

#### 2. **Components**
Components are pure data containers that define what an entity is.

```csharp
// Example: Citizen component (from Game.Citizens/Citizen.cs)
public struct Citizen : IComponentData, IQueryTypeParameter, ISerializable
{
    public ushort m_PseudoRandom;
    public CitizenFlags m_State;
    public byte m_WellBeing;
    public byte m_Health;
    public byte m_LeisureCounter;
    public byte m_PenaltyCounter;
    public int m_UnemploymentCounter;
    public short m_BirthDay;
    public float m_UnemploymentTimeCounter;
    public int m_SicknessPenalty;

    public int Happiness => (m_WellBeing + m_Health) / 2;
}
```

Key component types:
- **IComponentData** - Simple component data
- **IBufferElementData** - Dynamic buffer components
- **ISharedComponentData** - Shared data across entities
- **ICleanupComponentData** - Cleanup components

#### 3. **Systems**
Systems contain the logic that operates on entities with specific components.

```csharp
// Example: GameSystemBase (from Game/GameSystemBase.cs)
public abstract class GameSystemBase : COSystemBase
{
    // Lifecycle methods
    protected virtual void OnWorldReady() { }
    protected virtual void OnGamePreload(Purpose purpose, GameMode mode) { }
    protected virtual void OnGameLoaded(Context serializationContext) { }
    protected virtual void OnGameLoadingComplete(Purpose purpose, GameMode mode) { }
    protected virtual void OnFocusChanged(bool hasFocus) { }

    // Update scheduling
    public virtual int GetUpdateInterval(SystemUpdatePhase phase) => 1;
    public virtual int GetUpdateOffset(SystemUpdatePhase phase) => -1;
}
```

#### 4. **Archetypes**
Archetypes are unique combinations of components. Entities with the same archetype are stored together in memory for cache efficiency.

---

## System Update Pipeline

### Update Phases

The game uses a sophisticated multi-phase update system defined in `SystemUpdatePhase.cs`:

```csharp
public enum SystemUpdatePhase
{
    Invalid = -1,
    MainLoop,              // Main game loop
    LateUpdate,            // Post-update operations
    Modification1,         // Entity modification phase 1
    Modification2,         // Entity modification phase 2
    Modification2B,        // Sub-phase 2B
    Modification3,         // Entity modification phase 3
    Modification4,         // Entity modification phase 4
    Modification4B,        // Sub-phase 4B
    Modification5,         // Entity modification phase 5
    ModificationEnd,       // End of modifications
    PreSimulation,         // Before simulation
    PostSimulation,        // After simulation
    GameSimulation,        // Main simulation
    EditorSimulation,      // Editor-specific simulation
    Rendering,             // Rendering phase
    PreTool,               // Before tool processing
    PostTool,              // After tool processing
    ToolUpdate,            // Tool update
    ClearTool,             // Tool cleanup
    ApplyTool,             // Apply tool changes
    Serialize,             // Serialization
    Deserialize,           // Deserialization
    UIUpdate,              // UI updates
    UITooltip,             // Tooltip updates
    PrefabUpdate,          // Prefab processing
    DebugGizmos,           // Debug visualization
    LoadSimulation,        // Load simulation
    PreCulling,            // Before culling
    CompleteRendering,     // Complete rendering
    Raycast,               // Raycast operations
    PrefabReferences,      // Prefab reference updates
    Cleanup                // Cleanup phase
}
```

### UpdateSystem Architecture

The `UpdateSystem` class orchestrates all system updates:

**Key Features:**
1. **Phase-based Execution:** Systems execute in defined phases
2. **Interval-based Updates:** Systems can update at different frequencies (1, 2, 4, 8, etc. frames)
3. **Dependency Management:** Systems can specify before/after relationships
4. **GPU System Support:** Special handling for GPU-based systems
5. **Error Handling:** Comprehensive exception catching per system

**Example System Registration:**
```csharp
public static void Initialize(UpdateSystem updateSystem)
{
    updateSystem.RegisterGPUSystem<WaterSystem>();
    updateSystem.UpdateAt<PrefabSystem>(SystemUpdatePhase.MainLoop);
    updateSystem.UpdateAt<PathfindSetupSystem>(SystemUpdatePhase.MainLoop);
    updateSystem.UpdateBefore<PathfindQueueSystem>(SystemUpdatePhase.MainLoop);
    updateSystem.UpdateAfter<PrepareCleanUpSystem>(SystemUpdatePhase.MainLoop);
}
```

### Modification Barriers

The system uses Command Buffers and Barriers to manage structural changes:
- **ModificationBarrier1-5** - Sequential modification phases
- **ModificationEndBarrier** - Final modification phase
- **EndFrameBarrier** - Frame cleanup

This prevents race conditions when multiple systems modify entities simultaneously.

---

## Core Game Systems

### 1. **Prefab System** (Game.Prefabs)
**Purpose:** Manages game object templates and configuration

**Key Files:**
- `PrefabSystem.cs` - Core prefab management
- `PrefabBase.cs` - Base prefab class
- `PrefabData.cs` - Prefab data component
- `PrefabRef.cs` - Reference to prefab

**Key Concepts:**
```csharp
public abstract class PrefabBase : ComponentBase, ISerializationCallbackReceiver, IPrefabBase
{
    public List<ComponentBase> components = new List<ComponentBase>();
    public bool isDirty = true;

    // Component management
    public T AddOrGetComponent<T>() where T : ComponentBase
    public T AddComponent<T>() where T : ComponentBase
    public void Remove<T>() where T : ComponentBase
    public bool Has<T>() where T : ComponentBase
    public bool TryGet<T>(out T component) where T : ComponentBase
}
```

### 2. **Simulation Systems** (Game.Simulation - 480 files)

Major simulation systems include:

#### Economic Systems
- `CompanyDividendSystem.cs` - Company profit distribution
- `ResourceExporterSystem.cs` - Resource trading
- `TaxiDispatchSystem.cs` - Taxi service simulation
- `ResidentialDemandSystem.cs` - Housing demand calculation

#### Traffic Systems
- `TransportCarAISystem.cs` - Vehicle AI
- `VehicleSpawnSystem.cs` - Vehicle spawning
- `CommonPathfindSetup.cs` - Pathfinding configuration

#### City Services
- `GarbageTruckAISystem.cs` - Garbage collection
- `ExtractorFacilityAISystem.cs` - Resource extraction
- `AttractionSystem.cs` - Tourist attractions

#### Environmental Systems
- `AirPollution.cs` - Air quality simulation
- `ZoneAmbienceSystem.cs` - Ambient effects
- `WaterSourceCache.cs` - Water resource tracking

### 3. **Building System** (Game.Buildings - 145 files)

**Key Components:**
- Building placement and validation
- Building upgrades and maintenance
- Building-specific AI behaviors
- Service building functionality

### 4. **Network/Road System** (Game.Net - 148 files)

**Purpose:** Manages roads, pipes, power lines, and other networks

**Key Concepts:**
- Node-based network structure
- Lane-based traffic flow
- Network composition and generation
- Underground networks (water, sewage, power)

### 5. **Pathfinding System** (Game.Pathfind - 80 files)

**Features:**
- Multi-modal pathfinding (pedestrian, vehicle, public transport)
- A* algorithm implementation
- Pathfinding job batching
- Real-time path updates

**Key Files:**
- `PathfindQueueSystem.cs` - Queue management
- `PathfindSetupSystem.cs` - Setup and configuration
- `PathfindResultSystem.cs` - Result processing

### 6. **Citizen System** (Game.Citizens - 62 files)

**Citizen Simulation:**
```csharp
public struct Citizen : IComponentData
{
    public CitizenFlags m_State;      // Current state flags
    public byte m_WellBeing;          // Well-being score (0-255)
    public byte m_Health;             // Health score (0-255)
    public byte m_LeisureCounter;     // Leisure activity counter
    public int m_UnemploymentCounter; // Unemployment duration
    public short m_BirthDay;          // Birth day for age calculation

    public int Happiness => (m_WellBeing + m_Health) / 2;

    public CitizenAge GetAge();
    public int GetEducationLevel();
    public void SetEducationLevel(int level);
}
```

**Citizen Features:**
- Age simulation (child, teen, adult, elderly)
- Education system (0-4 levels)
- Employment and unemployment tracking
- Happiness and well-being calculation
- Health and leisure tracking

### 7. **Vehicle System** (Game.Vehicles - 92 files)

**Vehicle Types:**
- Personal cars
- Public transport (buses, trains, trams, subways)
- Service vehicles (police, fire, ambulance, garbage, etc.)
- Cargo vehicles
- Aircraft and watercraft

### 8. **Zone System** (Game.Zones)

**Zone Types:**
- Residential zones
- Commercial zones
- Industrial zones
- Office zones

**Key Files:**
- `BlockSystem.cs` - Zone block management
- `CellCheckSystem.cs` - Zone cell validation
- `BuildOrder.cs` - Construction ordering

---

## Prefab System

### Prefab Architecture

Prefabs are templates that define game objects. They use a component-based composition pattern.

### Prefab Categories

1. **Building Prefabs** (250+ types)
   - `BuildingPrefab.cs` - Base building
   - `ResidentialBuilding.cs` - Housing
   - `CommercialBuilding.cs` - Shops/offices
   - `IndustrialBuilding.cs` - Factories
   - `CityServiceBuilding.cs` - Service buildings

2. **Network Prefabs**
   - `RoadPrefab.cs` - Roads
   - `TrackPrefab.cs` - Railway tracks
   - `PathwayPrefab.cs` - Pedestrian paths
   - `PowerLinePrefab.cs` - Power lines
   - `PipelinePrefab.cs` - Water/sewage pipes

3. **Vehicle Prefabs**
   - `CarPrefab.cs` - Personal vehicles
   - `TrainPrefab.cs` - Trains
   - `BusPrefab.cs` - Buses
   - `AircraftPrefab.cs` - Aircraft

4. **Zone Prefabs**
   - `ZonePrefab.cs` - Zone definitions
   - Theme and style variations

5. **Service Prefabs**
   - Police, fire, healthcare, etc.
   - Service coverage data
   - Service vehicle definitions

### Prefab Component System

Each prefab can have multiple components:
- **Data Components** - Statistical data
- **Rendering Components** - Visual representation
- **AI Components** - Behavior definitions
- **Effect Components** - Sounds, particles, etc.

---

## Data Structures

### Common Component Patterns

#### 1. Owner Component
```csharp
// Reference to owning entity
public struct Owner : IComponentData, ISerializable
{
    public Entity m_Owner;
}
```

#### 2. Position/Transform Components
- Position, rotation, scale data
- Hierarchical transforms
- Interpolation data

#### 3. State Flags
Most entities use bit flags for state:
```csharp
[Flags]
public enum CitizenFlags : ushort
{
    None = 0,
    AgeBit1 = 1,
    AgeBit2 = 2,
    EducationBit1 = 4,
    EducationBit2 = 8,
    EducationBit3 = 16,
    // ... more flags
}
```

#### 4. Buffer Components
Dynamic arrays attached to entities:
```csharp
public struct BufferElement : IBufferElementData
{
    public Entity entity;
    public float3 position;
}
```

---

## Simulation Systems

### Time System

The game uses a sophisticated time simulation:
- Real-time to in-game time conversion
- Day/night cycles
- Season simulation
- Event scheduling based on time

### Economy Simulation

**Key Systems:**
1. **Resource Management**
   - Production, consumption, storage
   - Import/export
   - Resource types (goods, materials, food, etc.)

2. **Company Simulation**
   - Company lifecycle
   - Employment
   - Profit/loss calculation
   - Supply chain simulation

3. **Taxation**
   - Residential, commercial, industrial taxes
   - Tax rates and policies
   - Revenue collection

### Traffic Simulation

**Features:**
- Lane-based traffic flow
- Traffic light simulation
- Parking simulation
- Public transport routing
- Pathfinding integration

### Service Simulation

**Service Types:**
- Police (crime prevention)
- Fire (fire fighting)
- Healthcare (hospitals, clinics)
- Education (schools, universities)
- Deathcare (cemeteries, crematoriums)
- Garbage collection
- Postal service
- Communications

Each service has:
- Coverage area calculation
- Service vehicle dispatching
- Building-specific AI
- Resource consumption

---

## Rendering Pipeline

### Rendering Architecture

Cities: Skylines II uses Unity's High Definition Render Pipeline (HDRP):

**Key Features:**
1. **Dynamic Lighting**
   - Volumetric lighting
   - Global illumination
   - Real-time shadows

2. **Weather Effects**
   - Rain, snow, fog
   - Dynamic sky
   - Cloud simulation

3. **Post-Processing**
   - Depth of field
   - Motion blur
   - Color grading

4. **Level of Detail (LOD)**
   - Automatic LOD switching
   - Impostor rendering for distant objects
   - Mesh simplification

### Rendering Systems

**Key Files (Game.Rendering - 151 files):**
- `RenderingSystem.cs` - Main rendering coordinator
- `MeshSystem.cs` - Mesh management
- `BatchUploadSystem.cs` - GPU data upload
- `AnimatedSystem.cs` - Animation updates
- `PreRenderSystem.cs` - Pre-render preparation
- `CompleteRenderingSystem.cs` - Finalization

### Visual Effects

**Effect Types:**
- Particle systems
- VFX graph integration
- Light effects
- Sound effects integration

---

## UI Architecture

### UI Framework

The game uses **cohtml.Net** for UI rendering, which provides:
- HTML/CSS-based UI
- JavaScript bindings
- Hardware-accelerated rendering
- Responsive design

### UI Systems

**Major UI Modules:**

1. **In-Game UI** (Game.UI.InGame - 218 files)
   - City information panels
   - Building info panels
   - Resource overlays
   - Transportation views
   - Economic indicators

2. **Editor UI** (Game.UI.Editor - 84 files)
   - Map editor interface
   - Asset browser
   - Terrain tools
   - Object placement tools

3. **Menu UI** (Game.UI.Menu)
   - Main menu
   - Save/load interface
   - Settings
   - Mod management

4. **Widgets** (Game.UI.Widgets - 134 files)
   - Reusable UI components
   - Charts and graphs
   - Input controls
   - Notifications

### UI Data Binding

The game uses **Colossal.UI.Binding** for two-way data binding between C# and UI:

```csharp
// Example binding pattern
[Binding]
public int Population => m_CitySystem.GetPopulation();

[Binding]
public void SetTaxRate(int taxRate)
{
    m_EconomySystem.SetTaxRate(taxRate);
}
```

---

## Modding Framework

### Modding Support

The game has extensive modding support via **Game.Modding** namespace:

**Key Features:**
1. **Mod Loading System**
   - Dynamic assembly loading
   - Mod dependencies
   - Mod lifecycle management

2. **Toolchain** (Game.Modding.Toolchain)
   - Build tools
   - Asset pipeline integration
   - Dependency management

3. **API Exposure**
   - Public APIs for common operations
   - Event system for hooking into game logic
   - Data access methods

### Asset Pipeline

**Game.AssetPipeline** provides:
- Asset import/export
- Texture processing
- Mesh processing
- Prefab creation tools

---

## Key Classes and Components

### Critical Base Classes

#### 1. GameSystemBase
```csharp
// Location: Game/GameSystemBase.cs
public abstract class GameSystemBase : COSystemBase
{
    // Base class for all game systems
    // Provides lifecycle hooks and update scheduling
}
```

#### 2. PrefabBase
```csharp
// Location: Game.Prefabs/PrefabBase.cs
public abstract class PrefabBase : ComponentBase
{
    // Base class for all prefabs
    // Component-based composition pattern
}
```

#### 3. UpdateSystem
```csharp
// Location: Game/UpdateSystem.cs
public class UpdateSystem : GameSystemBase
{
    // Orchestrates all system updates
    // Phase-based execution model
}
```

### Common Component Interfaces

```csharp
public interface IComponentData { }           // Simple data component
public interface IBufferElementData { }       // Dynamic buffer component
public interface ISharedComponentData { }     // Shared component
public interface ISerializable { }            // Serialization support
public interface IQueryTypeParameter { }      // Query support
```

---

## Development Insights

### Code Quality Observations

1. **Architecture Maturity**
   - Well-structured ECS implementation
   - Clear separation of concerns
   - Extensive use of Unity's job system for parallelization

2. **Performance Optimizations**
   - Burst compilation enabled
   - Cache-friendly data layouts
   - Efficient memory management with native collections

3. **Maintainability**
   - Consistent naming conventions
   - Component-based architecture allows easy extension
   - Clear module boundaries

### Technical Challenges Addressed

1. **Scale Management**
   - Cities can have millions of entities
   - LOD system for rendering optimization
   - Update interval system to reduce CPU load

2. **Serialization**
   - Custom serialization framework
   - Version management for save compatibility
   - Efficient binary format

3. **Simulation Accuracy**
   - Sophisticated AI systems
   - Realistic traffic simulation
   - Complex economic modeling

### Performance Considerations

1. **Job System Integration**
   - Heavy use of IJobChunk for parallel processing
   - Burst-compiled jobs for critical paths

2. **Memory Management**
   - Native containers for low-level memory control
   - Pooling systems for frequent allocations
   - Struct-based components minimize GC pressure

3. **Update Scheduling**
   - Interval-based updates (systems can run at 1/2, 1/4 frequency)
   - Phase separation prevents conflicts
   - Dependency management ensures correct order

---

## System Interdependencies

### Critical System Relationships

```
PrefabSystem
    ↓ provides
[Building Systems] ← [Zone System] → [Service Systems]
    ↓ uses            ↓ creates         ↓ affects
[Simulation] ← [Traffic System] → [Pathfinding]
    ↓ affects         ↓ uses            ↓ provides
[Citizen System] → [Economy System] ← [Company System]
```

### Data Flow

1. **Prefab Loading** → Entity Creation → Component Initialization
2. **User Input** → Tool System → Modification Commands → Entity Updates
3. **Simulation Tick** → System Updates → State Changes → Event Triggers
4. **Rendering** → Query Entities → Batch Render Data → GPU Upload

---

## Additional Technical Details

### Serialization System

**Key Features:**
- Binary serialization format
- Version-aware deserialization
- Incremental saves for autosave
- Cloud save support (via PSI)

### Networking/PSI

**Platform Services Integration (Game.PSI):**
- Steam integration
- Discord integration
- Paradox SDK integration
- Achievement tracking
- Multiplayer support (planned/limited)

### Audio System

**Game.Audio:**
- 3D spatial audio
- Ambient soundscapes
- Dynamic music system
- Radio station simulation (Game.Audio.Radio)

### Debug Systems

**Game.Debug (64 files):**
- Performance profiling
- Entity inspection
- System visualization
- Gizmo rendering
- Debug UI overlays

---

## Conclusion

Cities: Skylines II represents a sophisticated implementation of modern game development practices:

**Strengths:**
- Excellent use of ECS architecture for performance and maintainability
- Comprehensive simulation systems covering all aspects of city management
- Strong modding support and extensibility
- Professional-grade rendering pipeline

**Architecture Highlights:**
- 4,284 C# files organized into clear functional modules
- Phase-based update system with 28 distinct phases
- Extensive prefab system with 1,257 prefab definitions
- Parallel-ready job-based simulation systems

**Technical Sophistication:**
- Unity DOTS/ECS implementation
- Burst-compiled performance-critical code
- Multi-threaded simulation
- Sophisticated data-oriented design

This codebase demonstrates enterprise-level game development practices and serves as an excellent reference for large-scale simulation game architecture.

---

## File Location Reference

**Main Assembly:** `New folder/Game.csproj`
**Base System:** `New folder/Game/GameSystemBase.cs`
**Update System:** `New folder/Game/UpdateSystem.cs`
**Prefab Base:** `New folder/Game.Prefabs/PrefabBase.cs`
**System Order:** `New folder/Game.Common/SystemOrder.cs`

---

**Document Version:** 1.0
**Date:** 2025-11-24
**Analysis Source:** Decompiled Cities: Skylines II Assembly
