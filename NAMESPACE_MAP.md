# Cities: Skylines II - Complete Namespace Map

## Overview
This document provides a comprehensive map of all namespaces in the Cities: Skylines II codebase.

---

## Root Namespace: Game (26 files)

**Core Framework Files:**
- `GameSystemBase.cs` - Base class for all game systems
- `UpdateSystem.cs` - System update orchestration
- `SystemUpdatePhase.cs` - Update phase enumeration
- `GameMode.cs` - Game mode definitions
- `Version.cs` - Version management
- `Stage.cs` - Game stage management
- `CameraController.cs` - Camera control base
- `OrbitCameraController.cs` - Orbit camera
- `CinematicCameraController.cs` - Cinematic camera
- `EndFrameBarrier.cs` - Frame barrier system
- `SafeCommandBufferSystem.cs` - Command buffer safety
- `IGPUSystem.cs` - GPU system interface
- `AutoSaveSystem.cs` - Auto-save functionality
- `AllowBarrier.cs` - Barrier management
- `FormatTags.cs` - Serialization format tags
- `InputExtensions.cs` - Input utilities
- `GizmosExt.cs` - Debug gizmo extensions

---

## Game.Prefabs (1,257 files) 🏗️
**Purpose:** Game object templates and configuration data

### Major Prefab Categories

#### Building Prefabs (250+)
- `BuildingPrefab.cs` - Base building type
- `ResidentialBuilding.cs` - Housing buildings
- `CommercialBuilding.cs` - Commercial buildings
- `IndustrialBuilding.cs` - Industrial facilities
- `OfficeBuilding.cs` - Office buildings
- `CityServiceBuilding.cs` - Service buildings
- `SignatureBuilding.cs` - Unique landmarks
- `SpawnableBuilding.cs` - Growable buildings

#### Service Buildings (50+)
- `School.cs`, `Hospital.cs`, `FireStation.cs`
- `PoliceStation.cs`, `PostOffice.cs`
- `PowerPlant.cs`, `WaterTower.cs`
- `GarbageFacility.cs`, `Prison.cs`
- `EmergencyShelter.cs`

#### Network Prefabs (100+)
- `RoadPrefab.cs` - Road types
- `TrackPrefab.cs` - Railway tracks
- `PathwayPrefab.cs` - Pedestrian paths
- `PowerLinePrefab.cs` - Electrical lines
- `PipelinePrefab.cs` - Water/sewage pipes
- `TaxiwayPrefab.cs` - Airport taxiways
- `NetSectionPrefab.cs` - Network sections
- `NetGeometryPrefab.cs` - Network geometry

#### Vehicle Prefabs (80+)
- `VehiclePrefab.cs` - Base vehicle
- `CarPrefab.cs` - Personal vehicles
- `TrainPrefab.cs` - Train vehicles
- `BusPrefab.cs` - Bus vehicles
- `TramPrefab.cs` - Tram vehicles
- `SubwayPrefab.cs` - Subway trains
- `AircraftPrefab.cs` - Aircraft
- `WatercraftPrefab.cs` - Boats/ships
- `HelicopterPrefab.cs` - Helicopters

#### Service Vehicles (30+)
- `Ambulance.cs`, `FireEngine.cs`
- `PoliceCar.cs`, `GarbageTruck.cs`
- `PostVan.cs`, `Hearse.cs`
- `MaintenanceVehicle.cs`

#### Zone Prefabs (20+)
- `ZonePrefab.cs` - Zone definitions
- `ZoneData.cs` - Zone statistics
- `ZoneBlockData.cs` - Block data
- `ZoneBuiltDataKey.cs` - Build tracking

#### Creature Prefabs (15+)
- `CreaturePrefab.cs` - Base creature
- `AnimalPrefab.cs` - Animals
- `HumanPrefab.cs` - Human characters
- `Domesticated.cs` - Pets
- `WildlifeData.cs` - Wild animals

#### Natural Object Prefabs (30+)
- `TreePrefab.cs` - Trees
- `PlantPrefab.cs` - Plants
- `SurfacePrefab.cs` - Terrain surfaces
- `BrushPrefab.cs` - Terrain brushes

#### Utility Prefabs (50+)
- `TransportStopPrefab.cs` - Transit stops
- `ParkingFacility.cs` - Parking structures
- `TaxiStand.cs` - Taxi stands
- `MailBox.cs` - Mailboxes

#### Configuration Prefabs (100+)
- `ThemePrefab.cs` - Visual themes
- `PolicyPrefab.cs` - City policies
- `NotificationIconPrefab.cs` - UI icons
- `TutorialPrefab.cs` - Tutorials
- `ChirpPrefab.cs` - Social media posts

#### Data Components (500+)
All prefabs can have data components:
- Position, rendering, AI, effects
- Statistics, requirements, modifiers
- Costs, maintenance, capacity
- Sounds, animations, particles

### Prefab Subsystems
- `PrefabSystem.cs` - Core prefab management
- `MeshSystem.cs` - Mesh processing
- `UnlockSystem.cs` - Unlock requirements
- `InstanceCountSystem.cs` - Instance tracking

---

## Game.Simulation (480 files) ⚙️
**Purpose:** Core game simulation logic

### Traffic & Transport (80+)
- `TrafficSimulation.cs` - Traffic flow
- `TransportCarAISystem.cs` - Vehicle AI
- `VehicleSpawnSystem.cs` - Vehicle spawning
- `TrainAISystem.cs` - Train behavior
- `AircraftAISystem.cs` - Aircraft behavior
- `WatercraftAISystem.cs` - Boat behavior
- `TaxiDispatchSystem.cs` - Taxi service

### Economy (60+)
- `EconomySystem.cs` - Economic simulation
- `CompanySystem.cs` - Company management
- `CompanyDividendSystem.cs` - Profit distribution
- `ResourceExporterSystem.cs` - Trade
- `ResourceImporterSystem.cs` - Imports
- `ProductionSystem.cs` - Production chains
- `DemandSystem.cs` - Supply/demand

### Citizens (40+)
- `CitizenBehaviorSystem.cs` - Citizen AI
- `CitizenSpawnSystem.cs` - Population growth
- `HealthcareSystem.cs` - Health simulation
- `EducationSystem.cs` - Education
- `TouristSpawnSystem.cs` - Tourism

### Services (50+)
- `GarbageTruckAISystem.cs` - Garbage collection
- `PoliceCarAISystem.cs` - Police patrols
- `FireEngineAISystem.cs` - Fire fighting
- `AmbulanceAISystem.cs` - Medical response
- `HearseAISystem.cs` - Deathcare
- `PostVanAISystem.cs` - Mail delivery

### Environment (30+)
- `PollutionSystem.cs` - Pollution simulation
- `AirPollution.cs` - Air quality
- `GroundPollution.cs` - Soil contamination
- `NoisePollution.cs` - Noise levels
- `WaterSystem.cs` - Water simulation
- `WeatherSystem.cs` - Weather effects
- `ClimateSystem.cs` - Climate simulation

### Infrastructure (40+)
- `ElectricitySystem.cs` - Power grid
- `WaterPipeSystem.cs` - Water network
- `SewageSystem.cs` - Sewage network
- `TelecomSystem.cs` - Communications

### Building Systems (50+)
- `BuildingUpkeepSystem.cs` - Maintenance
- `BuildingOccupancySystem.cs` - Occupancy
- `ExtractorFacilityAISystem.cs` - Resource extraction
- `ProcessingCompanyAISystem.cs` - Processing
- `StorageCompanyAISystem.cs` - Storage

### Pathfinding Integration (30+)
- `PathfindSetupSystem.cs` - Setup
- `CommonPathfindSetup.cs` - Common paths
- `ResourcePathfindSetup.cs` - Resource paths

### Demand Systems (20+)
- `ResidentialDemandSystem.cs` - Housing demand
- `CommercialDemandSystem.cs` - Commercial demand
- `IndustrialDemandSystem.cs` - Industrial demand
- `OfficeDemandSystem.cs` - Office demand

### Other Systems (80+)
- `TimeSystem.cs` - Time management
- `ZoneAmbienceSystem.cs` - Ambient effects
- `AttractionSystem.cs` - Tourist attractions
- `BudgetApplySystem.cs` - Budget application

---

## Game.UI.InGame (218 files) 🖥️
**Purpose:** In-game user interface

### Information Panels (80+)
- City statistics
- Building information
- Citizen information
- Vehicle information
- Route information
- Economy panels
- Budget panels

### Overlays (40+)
- Traffic overlay
- Zoning overlay
- District overlay
- Resource overlays
- Service coverage overlays
- Pollution overlays

### Menus (30+)
- Construction menus
- Policy menus
- Transportation menus
- Service menus

### HUD Elements (40+)
- Resource indicators
- Time controls
- Camera controls
- Tool indicators

### Notifications (30+)
- Alert system
- Event notifications
- Chirper (social media)

---

## Game.Rendering (151 files) 🎨
**Purpose:** Graphics rendering and visual effects

### Core Rendering (30+)
- `RenderingSystem.cs` - Main rendering
- `MeshRenderSystem.cs` - Mesh rendering
- `AnimatedSystem.cs` - Animation updates
- `BatchUploadSystem.cs` - GPU upload
- `PreRenderSystem.cs` - Pre-render prep
- `CompleteRenderingSystem.cs` - Finalization

### Lighting (20+)
- Dynamic lighting
- Shadow systems
- Volumetric lighting
- Light effects

### Effects (30+)
- Particle systems
- VFX integration
- Weather effects
- Water effects

### Camera Systems (20+)
- Game.Rendering.CinematicCamera
- Camera animation
- Camera effects

### Climate Rendering (10+)
- Game.Rendering.Climate
- Weather visuals
- Seasonal effects

### Debug Rendering (10+)
- Game.Rendering.Debug
- Gizmo rendering
- Debug overlays

### Utilities (30+)
- Game.Rendering.Utilities
- Mesh utilities
- Texture utilities
- Rendering helpers

---

## Game.Net (148 files) 🛣️
**Purpose:** Road and network infrastructure

### Road Systems (40+)
- Road generation
- Road composition
- Road segments
- Road nodes
- Intersections
- Lane management

### Public Transport Networks (30+)
- Train tracks
- Subway lines
- Tram lines
- Bus routes
- Monorail

### Utility Networks (30+)
- Power lines
- Water pipes
- Sewage pipes
- Communication cables

### Network Tools (20+)
- Network placement
- Network editing
- Network upgrading
- Network deletion

### Network Data (30+)
- Network geometry
- Network lanes
- Network connections
- Network aggregation

---

## Game.Buildings (145 files) 🏢
**Purpose:** Building simulation and management

### Building Systems (40+)
- Building initialization
- Building updates
- Building upgrades
- Building efficiency
- Building occupancy

### Building Types (30+)
- Residential buildings
- Commercial buildings
- Industrial buildings
- Office buildings
- Service buildings

### Building Components (40+)
- Renter management
- Property seeking
- Service consumption
- Resource consumption
- Employee management

### Building Effects (20+)
- Building pollution
- Building noise
- Building effects
- Building modifiers

### Building Tools (15+)
- Building placement
- Building upgrades
- Building demolition

---

## Game.UI.Widgets (134 files) 🧩
**Purpose:** Reusable UI components

### Basic Widgets (40+)
- Buttons
- Sliders
- Text inputs
- Dropdowns
- Checkboxes

### Complex Widgets (40+)
- Charts
- Graphs
- Lists
- Tables
- Trees

### Game-Specific Widgets (30+)
- Resource bars
- Building selectors
- Vehicle selectors
- Route planners

### Widget Systems (24+)
- Widget binding
- Widget events
- Widget layouts

---

## Game.Tools (110 files) 🔧
**Purpose:** Editor and construction tools

### Object Tools (30+)
- Object placement
- Object rotation
- Object elevation
- Object snapping

### Network Tools (25+)
- Road tool
- Railway tool
- Pathway tool
- Utility tool

### Terrain Tools (20+)
- Terraform tool
- Brush tools
- Smoothing tools
- Leveling tools

### Area Tools (15+)
- Zone tool
- District tool
- Park tool
- Map tile tool

### Tool Systems (20+)
- Tool base classes
- Tool managers
- Tool indicators
- Tool validation

---

## Game.Vehicles (92 files) 🚗
**Purpose:** Vehicle simulation

### Vehicle Types (30+)
- Personal cars
- Delivery trucks
- Buses
- Trains
- Trams
- Subways
- Aircraft
- Watercraft
- Helicopters

### Service Vehicles (20+)
- Police cars
- Fire engines
- Ambulances
- Garbage trucks
- Hearses
- Post vans
- Maintenance vehicles

### Vehicle Systems (25+)
- Vehicle spawning
- Vehicle AI
- Vehicle pathfinding
- Vehicle maintenance
- Vehicle parking

### Vehicle Components (17+)
- Vehicle data
- Vehicle odometer
- Vehicle passenger
- Vehicle cargo

---

## Game.Pathfind (80 files) 🗺️
**Purpose:** Pathfinding algorithms

### Core Pathfinding (30+)
- A* implementation
- Path queue management
- Path calculation
- Path caching

### Pathfind Types (25+)
- Pedestrian pathfinding
- Vehicle pathfinding
- Public transport pathfinding
- Service vehicle pathfinding
- Cargo pathfinding

### Pathfind Data (15+)
- Path elements
- Path targets
- Path costs
- Path edges

### Pathfind Systems (10+)
- PathfindQueueSystem
- PathfindSetupSystem
- PathfindResultSystem

---

## Game.Citizens (62 files) 👥
**Purpose:** Citizen simulation

### Citizen Data (15+)
- Citizen component
- Household data
- Citizen flags
- Age tracking
- Education tracking

### Citizen Behavior (20+)
- Needs simulation
- Activity selection
- Travel behavior
- Shopping behavior
- Leisure activities

### Citizen Services (15+)
- Healthcare
- Education
- Employment
- Housing
- Transportation

### Citizen Systems (12+)
- Spawn systems
- Behavior systems
- Update systems

---

## Game.Routes (70 files) 🚌
**Purpose:** Public transport routing

### Route Types (20+)
- Bus routes
- Train routes
- Tram routes
- Subway routes
- Ferry routes
- Airplane routes

### Route Components (20+)
- Route definition
- Route waypoints
- Route segments
- Route vehicles
- Route passengers

### Transport Stops (15+)
- Bus stops
- Train stations
- Tram stops
- Subway stations
- Ferry terminals
- Airports

### Route Systems (15+)
- Route pathfinding
- Route initialization
- Route updates
- Boarding systems

---

## Game.Zones (34 files) 🏘️
**Purpose:** Zoning system

### Zone Types
- Residential zones
- Commercial zones
- Industrial zones
- Office zones

### Zone Systems (15+)
- BlockSystem
- CellCheckSystem
- SearchSystem
- UpdateCollectSystem
- LoadSystem

### Zone Data (10+)
- Zone blocks
- Zone cells
- Zone lots
- Build orders

---

## Game.Economy (35 files) 💰
**Purpose:** Economic simulation

### Economic Systems (15+)
- Tax collection
- Resource production
- Resource consumption
- Trade management
- Company management

### Economic Data (10+)
- Resource types
- Production data
- Consumption data
- Trade statistics

### Economic Components (10+)
- Resource producers
- Resource consumers
- Resource storages

---

## Game.City (30 files) 🏛️
**Purpose:** City-level management

### City Systems
- City statistics
- City services
- City policies
- City milestones
- City options

### City Data
- Population tracking
- City modifiers
- City configuration
- District management

---

## Game.Areas (28 files) 🗺️
**Purpose:** Area management

### Area Types
- Districts
- Parks
- Map tiles
- Special areas

### Area Systems
- Area initialization
- Area updates
- Area geometry

---

## Game.Policies (25 files) 📜
**Purpose:** City policy system

### Policy Types
- Tax policies
- Service policies
- Education policies
- Healthcare policies
- Environmental policies

### Policy Systems
- Policy application
- Policy effects
- Policy costs

---

## Game.Companies (40 files) 🏭
**Purpose:** Business simulation

### Company Types
- Commercial companies
- Industrial companies
- Office companies
- Extractors
- Processors
- Storage

### Company Systems
- Company spawning
- Company behavior
- Company employees
- Company resources

---

## Game.Objects (84 files) 📦
**Purpose:** Generic game objects

### Object Systems
- Object initialization
- Object updates
- Object transforms
- Object rendering

### Object Types
- Static objects
- Dynamic objects
- Placeable objects
- Spawnable objects

---

## Game.Events (63 files) 📅
**Purpose:** Event system

### Event Types
- Random events
- Scheduled events
- Disaster events
- Calendar events
- Journal events

### Event Systems
- Event triggering
- Event effects
- Event notifications

---

## Game.Effects (25 files) ✨
**Purpose:** Visual and audio effects

### Effect Types
- Particle effects
- Sound effects
- Light effects
- VFX effects

### Effect Systems
- Effect spawning
- Effect updates
- Effect pooling

---

## Game.Audio (20 files) 🔊
**Purpose:** Audio management

### Audio Systems
- AudioManager
- 3D audio
- Ambient audio
- UI audio

### Game.Audio.Radio (15 files)
- Radio stations
- Music management
- Radio UI

---

## Game.Notifications (18 files) 🔔
**Purpose:** Notification system

### Notification Types
- Info notifications
- Warning notifications
- Error notifications
- Event notifications

---

## Game.Triggers (20 files) 🎯
**Purpose:** Trigger system

### Trigger Types
- Achievement triggers
- Tutorial triggers
- Event triggers
- Statistic triggers

---

## Game.Tutorials (85 files) 📚
**Purpose:** Tutorial system

### Tutorial Types
- Advisor tutorials
- Phase tutorials
- Trigger tutorials
- UI tutorials

### Tutorial Components
- Tutorial steps
- Tutorial conditions
- Tutorial rewards
- Tutorial UI

---

## Game.Settings (75 files) ⚙️
**Purpose:** Game configuration

### Setting Categories
- Graphics settings
- Audio settings
- Gameplay settings
- Control settings
- UI settings

---

## Game.Serialization (73 files) 💾
**Purpose:** Save/load system

### Serialization Systems
- Save game system
- Load game system
- Auto-save system

### Game.Serialization.DataMigration (10+)
- Version migration
- Data compatibility

---

## Game.Debug (64 files) 🐛
**Purpose:** Debug utilities

### Debug Systems
- Debug watch
- Debug UI
- Performance profiling
- Entity inspection

---

## Game.Modding (15 files) 🔧
**Purpose:** Modding support

### Modding Systems
- Mod loading
- Mod lifecycle
- API exposure

### Game.Modding.Toolchain (20+)
- Build tools
- Asset pipeline
- Dependencies

---

## Game.Achievements (12 files) 🏆
**Purpose:** Achievement system

- Achievement tracking
- Achievement triggers
- Achievement UI

---

## Game.Agents (10 files) 🤖
**Purpose:** Agent system

- Agent behavior
- Agent pathfinding

---

## Game.Assets (15 files) 📦
**Purpose:** Asset management

- Asset loading
- Asset databases

---

## Game.AssetPipeline (10 files) 🔄
**Purpose:** Asset processing

- Asset import
- Asset processing
- Asset export

---

## Game.Creatures (20 files) 🦌
**Purpose:** Animal simulation

- Wildlife
- Pets
- Animal AI

---

## Game.Dlc (8 files) 📦
**Purpose:** DLC management

- DLC detection
- DLC content

---

## Game.Input (15 files) 🎮
**Purpose:** Input handling

- Input system
- Input bindings
- Input actions

---

## Game.PSI (Platform Services)
### Game.PSI (10 files)
- Platform integration

### Game.PSI.Internal (5 files)
- Internal PSI systems

### Game.PSI.PdxSdk (8 files)
- Paradox SDK integration

---

## Game.SceneFlow (15 files) 🎬
**Purpose:** Scene management

- Scene loading
- Scene transitions
- Game modes

---

## Game.Common (30 files) 🔧
**Purpose:** Common utilities

### Core Components
- Owner
- Created/Deleted/Destroyed
- Updated
- Overridden
- Native

### Barriers
- ModificationBarrier1-5
- ModificationEndBarrier

### Systems
- ModificationSystem
- CleanUpSystem

---

## Game.UI (Base - 20 files)
**Purpose:** UI framework base

### Game.UI.Debug (10 files)
- Debug UI

### Game.UI.Editor (84 files)
- Map editor UI

### Game.UI.Menu (25 files)
- Main menu
- Settings menu

### Game.UI.Localization (15 files)
- Translation system

### Game.UI.Thumbnails (10 files)
- Thumbnail generation

### Game.UI.Tooltip (15 files)
- Tooltip system

---

## Colossal Namespaces

### Colossal.Atmosphere (5 files)
- Atmosphere simulation

### Colossal.Atmosphere.Internal (3 files)
- Internal atmosphere systems

### Colossal.Rendering (10 files)
- Core rendering utilities

---

## Unity Namespaces

### Unity.Entities.CodeGeneratedRegistry
- Generated ECS code

### Unity.Mathematics
- Math extensions

### System.Runtime.CompilerServices
- C# compiler features

---

## Total File Count by Category

| Category | Files |
|----------|-------|
| Prefabs & Data | 1,343 |
| Simulation | 480 |
| UI Systems | 460 |
| Rendering | 151 |
| Infrastructure | 148 |
| Buildings | 145 |
| Tools | 110 |
| Vehicles | 92 |
| Pathfinding | 80 |
| Configuration | 75 |
| Other Systems | 1,200 |
| **TOTAL** | **4,284** |

---

**Last Updated:** 2025-11-24
