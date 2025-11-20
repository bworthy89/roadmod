# Cities: Skylines II Road and Network Systems Analysis
## Complete Guide for Building an Organic Road Suggestion System

---

## EXECUTIVE SUMMARY

The Cities: Skylines II road system is built on a hierarchical ECS architecture with nodes, edges, curves, and lanes. Roads are created through a sophisticated tool system that integrates with entity archetypes, prefabs, and composition systems. An organic road suggestion system should leverage density analysis, building patterns, zoning blocks, and traffic flow data to recommend road placements that naturally follow city development patterns.

---

## 1. CORE NETWORK/ROAD ARCHITECTURE

### 1.1 Fundamental Components

#### **Node Component** (`Game.Net.Node`)
- **Purpose**: Represents intersections/junctions in the road network
- **Properties**:
  - `m_Position` (float3): 3D world position of the node
  - `m_Rotation` (quaternion): Orientation of the node
- **Key Feature**: Nodes define where roads meet and allow connections

```csharp
public struct Node : IComponentData, IQueryTypeParameter
{
    public float3 m_Position;
    public quaternion m_Rotation;
}
```

#### **Edge Component** (`Game.Net.Edge`)
- **Purpose**: Represents road segments connecting two nodes
- **Properties**:
  - `m_Start` (Entity): Reference to starting node
  - `m_End` (Entity): Reference to ending node
- **Key Feature**: Edges form the actual road path between intersections

```csharp
public struct Edge : IComponentData, IQueryTypeParameter
{
    public Entity m_Start;
    public Entity m_End;
}
```

#### **Curve Component** (`Game.Net.Curve`)
- **Purpose**: Defines the bezier curve geometry between nodes
- **Properties**:
  - `m_Bezier` (Bezier4x3): 4-control point cubic bezier curve
  - `m_Length` (float): Precomputed curve length
- **Key Feature**: Smooth road paths with calculated length for pathfinding

```csharp
public struct Curve : IComponentData, IQueryTypeParameter
{
    public Bezier4x3 m_Bezier;  // Cubic bezier spline
    public float m_Length;       // Cached for performance
}
```

#### **Elevation Component** (`Game.Net.Elevation`)
- **Purpose**: Tracks elevation changes on edges
- **Properties**:
  - `m_Elevation` (float2): Start and end elevation values
- **Key Feature**: Handles elevated roads, bridges, and tunnels

```csharp
public struct Elevation : IComponentData, IQueryTypeParameter
{
    public float2 m_Elevation;   // [start, end] heights
}
```

#### **Composition Component** (`Game.Net.Composition`)
- **Purpose**: Links edges to their geometric representation
- **Properties**:
  - `m_Edge` (Entity): Reference to the edge entity
  - `m_StartNode` (Entity): Visual start node reference
  - `m_EndNode` (Entity): Visual end node reference
- **Key Feature**: Separates logical road structure from visual rendering

```csharp
public struct Composition : IComponentData
{
    public Entity m_Edge;
    public Entity m_StartNode;
    public Entity m_EndNode;
}
```

#### **Road Component** (`Game.Net.Road`)
- **Purpose**: Stores traffic flow and operational data
- **Properties**:
  - `m_TrafficFlowDuration0/1` (float4 x2): Traffic duration metrics
  - `m_TrafficFlowDistance0/1` (float4 x2): Traffic distance metrics
  - `m_Flags` (RoadFlags): Lighting and alignment flags
- **Key Feature**: Real-time traffic analytics for organic suggestions

```csharp
public struct Road : IComponentData
{
    public float4 m_TrafficFlowDuration0;
    public float4 m_TrafficFlowDuration1;
    public float4 m_TrafficFlowDistance0;
    public float4 m_TrafficFlowDistance1;
    public RoadFlags m_Flags;
}
```

### 1.2 Connection Components

#### **ConnectedNode** (`Game.Net.ConnectedNode`)
- **IBufferElementData**: Dynamic buffer on edges
- **Properties**:
  - `m_Node` (Entity): Node on the curve
  - `m_CurvePosition` (float): Position along the bezier (0-1)
- **Use Case**: Stores intermediate nodes along a curved road

```csharp
[InternalBufferCapacity(0)]
public struct ConnectedNode : IBufferElementData
{
    public Entity m_Node;
    public float m_CurvePosition;  // Normalized position on curve
}
```

#### **ConnectedEdge** (`Game.Net.ConnectedEdge`)
- **IBufferElementData**: Dynamic buffer on nodes
- **Capacity**: 4 edges per node (typical intersection)
- **Properties**:
  - `m_Edge` (Entity): Connected edge entity
- **Use Case**: Quick lookup of edges connected to a node

```csharp
[InternalBufferCapacity(4)]
public struct ConnectedEdge : IBufferElementData
{
    public Entity m_Edge;  // Max 4 typical, can be more
}
```

### 1.3 Lane System Architecture

#### **Lane Component** (`Game.Net.Lane`)
- **Purpose**: Defines traffic lanes on roads
- **Properties**:
  - `m_StartNode` (PathNode): Lane start in pathfinding space
  - `m_MiddleNode` (PathNode): Lane middle point
  - `m_EndNode` (PathNode): Lane end in pathfinding space
- **Key Feature**: Integrated with pathfinding system for vehicle routing

#### **SubLane** (`Game.Net.SubLane`)
- **IBufferElementData**: Dynamic buffer on edges
- **Purpose**: Individual movement lanes under a road composition
- **Properties**:
  - `m_SubLane` (Entity): Reference to the actual lane entity
  - `m_PathMethods` (PathMethod): Pathfinding types (e.g., Car, Pedestrian, Delivery)

#### **CarLane** (`Game.Net.CarLane`)
- **Purpose**: Vehicle traffic lane specific data
- **Properties**: Lane-specific vehicle behavior flags

#### **Density Component** (`Game.Net.Density`)
- **Purpose**: Population/activity density on roads
- **Properties**:
  - `m_Density` (float): Calculated density value
- **Key Feature**: Normalized by road length for organic suggestions

```csharp
public struct Density : IComponentData
{
    public float m_Density;  // Residents/employees per road length
}
```

### 1.4 Prefab and Geometric Data

#### **NetData** (`Game.Prefabs.NetData`)
- **Purpose**: Road type definition from prefabs
- **Properties**:
  - `m_NodeArchetype` (EntityArchetype): Template for node entities
  - `m_EdgeArchetype` (EntityArchetype): Template for edge entities
  - `m_RequiredLayers` (Layer): Network layer requirements
  - `m_ConnectLayers` (Layer): Layers this road can connect to
  - `m_LocalConnectLayers` (Layer): Local junction connections
  - `m_NodePriority` (float): Priority in junction resolution

#### **NetGeometryData** (`Game.Prefabs.NetGeometryData`)
- **Purpose**: Visual and physical properties
- **Properties**:
  - `m_NodeCompositionArchetype`: Visual node template
  - `m_EdgeCompositionArchetype`: Visual edge template
  - `m_DefaultWidth` (float): Road width in meters
  - `m_ElevatedWidth` (float): Elevated section width
  - `m_DefaultHeightRange` (Bounds1): Normal road height
  - `m_ElevatedHeightRange` (Bounds1): Bridge/elevated height
  - `m_EdgeLengthRange` (Bounds1): Min/max segment length
  - `m_MaxSlopeSteepness` (float): Slope limits
  - `m_MergeLayers` (Layer): Road types to merge with
  - `m_IntersectLayers` (Layer): Road types that can intersect

---

## 2. TOOL SYSTEMS FOR ROAD CREATION

### 2.1 NetToolSystem Architecture

**Location**: `/Game.Tools/NetToolSystem.cs`

#### **Road Placement Modes**
```csharp
public enum Mode
{
    Straight,        // Direct line between two points
    SimpleCurve,     // Single curve control point
    ComplexCurve,    // Multiple control points for bezier
    Continuous,      // Chain multiple segments
    Grid,            // Organized grid layout
    Replace,         // Replace existing road type
    Point            // Place single junction point
}
```

#### **Snap Modes**
The system defines snapping behavior to assist organic placement:
```
Snap.ExistingGeometry  - Snap to existing roads
Snap.NearbyGeometry    - Snap to close roads
Snap.ObjectSide        - Snap to building sides
Snap.Zones             - Snap to zoning blocks
Snap.Grid              - Snap to terrain grid
```

#### **Key NetToolSystem Fields**
- `mode`: Current placement mode
- `elevation`: Current road elevation
- `elevationStep`: Elevation increment
- `parallelCount`: Number of parallel roads
- `parallelOffset`: Distance between parallel roads
- `underground`: Whether placing underground

### 2.2 Road Creation Pipeline

#### **Step 1: GenerateNodesSystem** (`Game.Tools.GenerateNodesSystem`)
```
Input: CreationDefinition (placement intent)
↓
Creates temporary node entities at specified positions
Validates node placement against terrain and existing structures
Sets up node archetype with proper components
Output: Node entities with Temp flag for validation
```

#### **Step 2: GenerateEdgesSystem** (`Game.Tools.GenerateEdgesSystem`)
```
Input: Generated nodes + connection specifications
↓
Creates temporary edge entities connecting nodes
Computes bezier curves between nodes
Validates edge placement and length
Checks for intersections with obstacles
Output: Edge entities with curves and connections
```

#### **Step 3: GenerateAggregatesSystem**
```
Input: Edges + composition data
↓
Creates visual representations
Generates lane structures
Sets up decoration and lighting
Output: Complete road with all lanes
```

#### **Step 4: ApplyNetSystem** (`Game.Tools.ApplyNetSystem`)
```
Input: Temporary validated entities
↓
Converts Temp entities to permanent entities
Updates node/edge connections
Integrates into existing network
Triggers pathfinding recalculation
Output: Live road in game world
```

### 2.3 Entity Creation Pattern

**Core Pattern from GenerateNodesSystem:**
```csharp
// Create node entity
Entity nodeEntity = m_CommandBuffer.CreateEntity(netData.m_NodeArchetype);
m_CommandBuffer.SetComponent(nodeEntity, new Node
{
    m_Position = position,
    m_Rotation = rotation
});
m_CommandBuffer.SetComponent(nodeEntity, new Temp
{
    m_Flags = TempFlags.Create
});

// Create edge entity
Entity edgeEntity = m_CommandBuffer.CreateEntity(netData.m_EdgeArchetype);
m_CommandBuffer.SetComponent(edgeEntity, new Edge
{
    m_Start = startNodeEntity,
    m_End = endNodeEntity
});
m_CommandBuffer.SetComponent(edgeEntity, new Curve
{
    m_Bezier = bezierCurve,
    m_Length = MathUtils.Length(bezierCurve)
});
```

### 2.4 Snapping Logic

**Building Side Snapping (from NetToolSystem):**
```csharp
// Snaps to building perimeters for organic placement
private void SnapObjectSide(Entity buildingEntity)
{
    // Get building geometry
    ObjectGeometryData geometry = geometryLookup[prefabRef];
    
    // Calculate building bounds in rotation space
    Quad3 buildingCorners = ObjectUtils.CalculateBaseCorners(
        transform.m_Position, 
        transform.m_Rotation,
        geometry.m_Bounds
    );
    
    // Check distance to each building edge
    // Return snap point if within snap distance
}
```

**Zone Block Snapping:**
The system also snaps to zone block cells, aligning roads with city district layouts.

---

## 3. TERRAIN AND CITY ANALYSIS SYSTEMS

### 3.1 Building and Zoning Components

#### **Building Component** (`Game.Buildings.Building`)
```csharp
public struct Building : IComponentData
{
    public Entity m_RoadEdge;           // Connected road
    public float m_CurvePosition;       // Position on road (0-1)
    public uint m_OptionMask;           // Building options
    public BuildingFlags m_Flags;       // State flags
}
```

**Use for organic suggestions:**
- Query buildings near proposed roads
- Check building density and development level
- Analyze demand signals from building types

#### **Block Component** (`Game.Zones.Block`)
```csharp
public struct Block : IComponentData
{
    public float3 m_Position;    // Block center
    public float2 m_Direction;   // Block orientation (normalized)
    public int2 m_Size;          // Block dimensions in cells (8m each)
}
```

**Use for organic suggestions:**
- Grid-aligned road suggestions following zoning patterns
- Analyze block density to find high-demand areas
- Ensure roads connect major blocks logically

#### **ZoneUtils Helper Functions**
```csharp
// Key helper methods for block analysis
public static float3 GetCellPosition(Block block, int2 cellIndex);
public static int2 GetCellIndex(Block block, float2 position);
public static Bounds2 CalculateBounds(Block block);
public static Quad2 CalculateCorners(Block block);
```

### 3.2 Density Analysis System

#### **NetEdgeDensitySystem** (`Game.Simulation.NetEdgeDensitySystem`)

**Calculates resident/worker density on roads:**
```csharp
private struct CalculateDensityJob : IJobChunk
{
    // Input: Buildings connected to edges
    public BufferTypeHandle<ConnectedBuilding> m_BuildingType;
    
    // Output: Density metric
    public ComponentTypeHandle<Density> m_DensityType;
    
    // Execution:
    // For each building connected to edge:
    //   - Count residents (via HouseholdCitizen buffer)
    //   - Count employees (via WorkProvider component)
    //   - Divide by road length for normalized density
    // Result: float value proportional to demand
}
```

**Density calculation formula:**
```
density = (total_residents + total_employees) / road_length
```

### 3.3 Connected Buildings

#### **ConnectedBuilding** (`Game.Buildings.ConnectedBuilding`)
- **IBufferElementData**: Dynamic buffer on edges
- **Purpose**: Track which buildings use each road
- **Property**:
  - `m_Building` (Entity): Building entity reference

**Use for organic suggestions:**
```csharp
// Query buildings on edge
var buildings = m_BuildingBuffer[edgeEntity];
foreach (var connectedBuilding in buildings)
{
    Entity building = connectedBuilding.m_Building;
    // Analyze building type, efficiency, happiness
    // Suggest additional roads if density high
}
```

### 3.4 Population and Workforce Data

#### **WorkProvider** (`Game.Buildings.WorkProvider`)
- **Property**: `m_MaxWorkers` (float): Workplace capacity
- **Use**: Gauge commercial/industrial demand

#### **Renter Component** (`Game.Buildings.Renter`)
- **IBufferElementData**: Residents/companies renting the building
- **Use**: Analyze who occupies each building

#### **HouseholdCitizen** (`Game.Citizens.HouseholdCitizen`)
- **Property**: Buffer length = household size
- **Use**: Count actual population

---

## 4. PATHFINDING AND TRAFFIC FLOW

### 4.1 Pathfinding Integration

#### **Lane and PathNode System**
- Roads integrate with the pathfinding system via `PathNode` references
- Each lane in `Lane.m_StartNode`, `Lane.m_MiddleNode`, `Lane.m_EndNode` references pathfinding nodes
- The pathfinding system uses these to find routes for vehicles

**Relevant for organic roads:**
- New roads must be integrated into pathfinding
- Can analyze existing pathfinding congestion
- Suggest roads where pathfinding shows bottlenecks

### 4.2 Traffic Flow Data

#### **Road Traffic Metrics**
```csharp
public struct Road : IComponentData
{
    // Traffic flow tracked in 8 time buckets (day/night, direction)
    public float4 m_TrafficFlowDuration0;   // Time spent on road
    public float4 m_TrafficFlowDuration1;
    public float4 m_TrafficFlowDistance0;   // Distance traveled
    public float4 m_TrafficFlowDistance1;
}
```

**Suggests where roads are needed:**
- High duration on short roads = congestion
- Uneven load distribution = need alternate routes
- Dead-end roads with traffic = need connectivity

### 4.3 Game.Pathfind Systems

**Key pathfinding files for analysis:**
- `DensityAction.cs`: Track demand density for routing
- `FlowAction.cs`: Analyze traffic flow patterns
- `LaneDataSystem.cs`: Manage lane pathfinding info

---

## 5. ORGANIC ROAD SUGGESTION ARCHITECTURE

### 5.1 Recommended System Base Class

```csharp
using Game;
using Game.Net;
using Game.Zones;
using Game.Buildings;
using Game.Simulation;
using Game.Prefabs;
using Game.Tools;
using Game.Common;
using Game.Debug;
using Colossal.Mathematics;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Colossal.Collections;

namespace Game.Tools;

[CompilerGenerated]
public class OrganicRoadSuggestionSystem : GameSystemBase
{
    // Inherits:
    // - EntityManager (m_EntityManager)
    // - SystemState (m_SystemState)
    // - World access
    // - Update scheduling
}
```

### 5.2 Data Analysis Phase

**Query high-demand areas:**
```csharp
// Find edges with high density
var highDensityEdges = m_DensityQuery;  // Filter by Density > threshold

// Find blocks with many buildings
var populousBlocks = GetEntityQuery(
    ComponentType.ReadOnly<Block>(),
    ComponentType.ReadOnly<BuildOrder>()
);

// Calculate building density per block
// Aggregate connected building counts
```

**Analyze existing road patterns:**
```csharp
// Find road gaps between populated blocks
foreach (var block in blocks)
{
    // Get buildings in block
    var cellBounds = ZoneUtils.CalculateBounds(block);
    var buildingsInBlock = quadTree.Query(cellBounds);
    
    if (buildingsInBlock.Count > threshold)
    {
        // This block is developed
        // Check if it has sufficient road connections
        // Suggest connecting to adjacent blocks
    }
}
```

### 5.3 Suggestion Generation Phase

**Core suggestion algorithm:**
```csharp
struct RoadSuggestion
{
    public float3 m_StartPosition;
    public float3 m_EndPosition;
    public Entity m_RoadPrefab;
    public float m_Priority;  // 0-1, based on need
    public SuggestionReason m_Reason;
}

enum SuggestionReason
{
    ConnectPopulousBlocks,      // High density block connection
    ReduceCongestion,            // Traffic bottleneck
    CompleteGrid,                // Logical grid extension
    ImproveConnectivity,         // Cut-off area connection
    DeadEndElimination,          // Convert dead-end to through-road
}
```

### 5.4 Visualization System

**Using GizmoBatcher for debug overlay:**
```csharp
[BurstCompile]
private struct SuggestionVisualizationJob : IJob
{
    [ReadOnly]
    public NativeList<RoadSuggestion> m_Suggestions;
    
    public GizmoBatcher m_GizmoBatcher;
    
    public void Execute()
    {
        foreach (var suggestion in m_Suggestions)
        {
            // Draw suggested road as semi-transparent line
            Color color = Color.Lerp(
                Color.yellow,  // Low priority
                Color.green,   // High priority
                suggestion.m_Priority
            );
            
            m_GizmoBatcher.DrawLine(
                suggestion.m_StartPosition,
                suggestion.m_EndPosition,
                color
            );
            
            // Draw reason indicator as circle at midpoint
            float3 midpoint = (suggestion.m_StartPosition + 
                              suggestion.m_EndPosition) * 0.5f;
            DrawReasonMarker(midpoint, suggestion.m_Reason);
        }
    }
}
```

---

## 6. IMPLEMENTATION EXAMPLES

### 6.1 Creating Road Programmatically

**Minimal example - create straight road:**
```csharp
// Step 1: Get road prefab from collection
Entity roadPrefab = prefabLookup[roadPrefabHandle];
NetData netData = netDataLookup[roadPrefab];
NetGeometryData geometryData = geometryLookup[roadPrefab];

// Step 2: Create start node
Entity startNode = EntityManager.CreateEntity(netData.m_NodeArchetype);
EntityManager.SetComponentData(startNode, new Node
{
    m_Position = startPosition,
    m_Rotation = quaternion.identity
});
EntityManager.SetComponentData(startNode, new Temp
{
    m_Flags = TempFlags.Create
});

// Step 3: Create end node
Entity endNode = EntityManager.CreateEntity(netData.m_NodeArchetype);
EntityManager.SetComponentData(endNode, new Node
{
    m_Position = endPosition,
    m_Rotation = quaternion.identity
});
EntityManager.SetComponentData(endNode, new Temp
{
    m_Flags = TempFlags.Create
});

// Step 4: Create edge
Entity edge = EntityManager.CreateEntity(netData.m_EdgeArchetype);
Bezier4x3 curve = NetUtils.StraightCurve(startPosition, endPosition);
EntityManager.SetComponentData(edge, new Edge
{
    m_Start = startNode,
    m_End = endNode
});
EntityManager.SetComponentData(edge, new Curve
{
    m_Bezier = curve,
    m_Length = MathUtils.Length(curve)
});
EntityManager.SetComponentData(edge, new Temp
{
    m_Flags = TempFlags.Create
});

// Step 5: Add PrefabRef so system knows road type
EntityManager.SetComponentData(edge, new PrefabRef(roadPrefab));
EntityManager.SetComponentData(startNode, new PrefabRef(roadPrefab));
EntityManager.SetComponentData(endNode, new PrefabRef(roadPrefab));

// Step 6: Connect nodes and edges
var startEdges = EntityManager.GetBuffer<ConnectedEdge>(startNode);
startEdges.Add(new ConnectedEdge(edge));

var endEdges = EntityManager.GetBuffer<ConnectedEdge>(endNode);
endEdges.Add(new ConnectedEdge(edge));

// Step 7: Create composition and lanes
// (Handled by GenerateAggregatesSystem automatically)
```

### 6.2 Querying Network Connectivity

```csharp
// Find all roads connected to a node
public static void GetConnectedEdges(
    Entity nodeEntity,
    BufferLookup<ConnectedEdge> edgeLookup,
    NativeList<Entity> outEdges)
{
    if (!edgeLookup.HasBuffer(nodeEntity))
        return;
    
    var connectedEdges = edgeLookup[nodeEntity];
    foreach (var connected in connectedEdges)
    {
        outEdges.Add(connected.m_Edge);
    }
}

// Find distance to nearest road
public static bool FindNearestRoadNode(
    float3 position,
    ComponentLookup<Node> nodeLookup,
    ComponentLookup<Owner> ownerLookup,
    float searchRadius,
    out Entity nearestNode,
    out float distance)
{
    nearestNode = Entity.Null;
    distance = float.MaxValue;
    
    // Use spatial queries with road layer
    foreach (var nodeEntity in roadNodeQuery)
    {
        var node = nodeLookup[nodeEntity];
        float d = math.distance(node.m_Position.xz, position.xz);
        
        if (d < distance && d <= searchRadius)
        {
            distance = d;
            nearestNode = nodeEntity;
        }
    }
    
    return nearestNode != Entity.Null;
}
```

### 6.3 Analyzing Building Density

```csharp
// Calculate density in area
public struct DensityAnalysis
{
    public float m_ResidentCount;
    public float m_EmployeeCount;
    public int m_BuildingCount;
    public float m_AverageDensity;
}

public static DensityAnalysis AnalyzeBlockDensity(
    Entity blockEntity,
    in Block block,
    BufferLookup<Cell> cellLookup,
    ComponentLookup<Building> buildingLookup,
    BufferLookup<ConnectedBuilding> connectedBuildingLookup,
    ComponentLookup<WorkProvider> workProviderLookup,
    BufferLookup<HouseholdCitizen> householdLookup)
{
    DensityAnalysis analysis = default;
    
    if (!connectedBuildingLookup.HasBuffer(blockEntity))
        return analysis;
    
    var buildings = connectedBuildingLookup[blockEntity];
    analysis.m_BuildingCount = buildings.Length;
    
    foreach (var connectedBuilding in buildings)
    {
        var building = connectedBuilding.m_Building;
        
        // Count residents
        if (householdLookup.HasBuffer(building))
        {
            analysis.m_ResidentCount += householdLookup[building].Length;
        }
        
        // Count employees
        if (workProviderLookup.HasComponent(building))
        {
            analysis.m_EmployeeCount += 
                workProviderLookup[building].m_MaxWorkers;
        }
    }
    
    analysis.m_AverageDensity = 
        (analysis.m_ResidentCount + analysis.m_EmployeeCount) / 
        (ZoneUtils.CELL_AREA * block.m_Size.x * block.m_Size.y);
    
    return analysis;
}
```

### 6.4 Snapping Suggestions to Terrain

```csharp
// Snap suggested road to actual terrain height
public static void SnapToTerrain(
    ref float3 position,
    in TerrainHeightSystem terrainSystem)
{
    // Get terrain height at position
    float terrainHeight = terrainSystem.GetHeight(position.xz);
    position.y = terrainHeight;
}

// Snap endpoints to existing nodes/buildings
public static bool TrySnapToExistingRoad(
    float3 position,
    float snapDistance,
    ComponentLookup<Node> nodeLookup,
    ComponentLookup<Owner> ownerLookup,
    EntityQuery roadNodeQuery,
    out float3 snappedPosition,
    out Entity snappedNode)
{
    snappedPosition = position;
    snappedNode = Entity.Null;
    
    foreach (var nodeEntity in roadNodeQuery)
    {
        var node = nodeLookup[nodeEntity];
        float distance = math.distance(
            node.m_Position, position);
        
        if (distance < snapDistance)
        {
            snappedPosition = node.m_Position;
            snappedNode = nodeEntity;
            return true;
        }
    }
    
    return false;
}
```

---

## 7. SUGGESTED SYSTEM UPDATE PHASES

### 7.1 System Update Order

```
PreSimulation (every tick):
├── RoadSuggestionAnalysisSystem
│   └── Analyze city development patterns
│       ├── Query high-density areas
│       ├── Identify connectivity gaps
│       └── Calculate demand signals
│
├── SuggestionGenerationSystem
│   └── Create suggestions from analysis
│       ├── Find block connections
│       ├── Identify congestion points
│       └── Rate suggestions by priority
│
└── SuggestionVisualizationSystem (debug only)
    └── Render suggestions as gizmos
        ├── Draw suggestion lines
        ├── Color-code by priority
        └── Show reason markers

Simulation:
└── (Standard road systems)
    ├── LaneSystem
    ├── LaneConnectionSystem
    ├── NetEdgeDensitySystem
    └── TrafficSystem
```

### 7.2 Update Frequency Strategy

```csharp
public class OrganicRoadSuggestionSystem : GameSystemBase
{
    private int m_TickCounter;
    private const int ANALYSIS_INTERVAL = 60;  // Every 60 frames
    
    protected override void OnUpdate()
    {
        m_TickCounter++;
        
        if (m_TickCounter >= ANALYSIS_INTERVAL)
        {
            m_TickCounter = 0;
            
            // Heavy analysis only every N frames
            AnalyzeNetworkDemand();
            GenerateSuggestions();
        }
        
        // Every frame
        UpdateVisualization();
    }
}
```

---

## 8. KEY INTEGRATION POINTS

### 8.1 With Tool System
- **Integration**: Suggestions feed into NetToolSystem
- **Handoff**: Store suggestions as tool "guides"
- **UX**: Player can click to apply suggestion via tool

### 8.2 With Pathfinding
- **Integration**: Monitor pathfinding congestion
- **Feedback**: Use pathfinding-detected bottlenecks
- **Validation**: Ensure suggested roads improve pathfinding

### 8.3 With Zoning
- **Integration**: Query block information
- **Analysis**: Calculate development potential
- **Alignment**: Suggest roads aligned with zones

### 8.4 With Economy
- **Integration**: Monitor commercial/industrial demand
- **Signals**: High employment demand = more roads needed
- **Balanced**: Avoid over-suggesting in low-demand areas

---

## 9. PERFORMANCE CONSIDERATIONS

### 9.1 Query Optimization

```csharp
// Create cached queries, not temporary ones
private EntityQuery m_HighDensityEdgesQuery;
private EntityQuery m_DevelopedBlocksQuery;

protected override void OnCreate()
{
    base.OnCreate();
    
    m_HighDensityEdgesQuery = GetEntityQuery(
        ComponentType.ReadOnly<Density>(),
        ComponentType.ReadOnly<Curve>()
    );
    
    m_DevelopedBlocksQuery = GetEntityQuery(
        ComponentType.ReadOnly<Block>(),
        ComponentType.ReadOnly<BuildOrder>()
    );
}
```

### 9.2 Burst Compilation

```csharp
[BurstCompile]
private struct AnalysisDensityJob : IJobChunk
{
    // Only include necessary data
    [ReadOnly]
    public ComponentTypeHandle<Density> m_DensityType;
    
    // Avoid dynamic allocations in Burst
    public NativeList<float> m_DensityValues;
    
    public void Execute(in ArchetypeChunk chunk, ...)
    {
        // Burst-compatible code only
    }
}
```

### 9.3 Caching Strategies

```csharp
// Cache expensive lookups
private ComponentLookup<Node> m_NodeLookup;
private ComponentLookup<Density> m_DensityLookup;
private BufferLookup<ConnectedBuilding> m_BuildingLookup;

protected override void OnUpdate()
{
    // Update caches once per frame
    m_NodeLookup.Update(ref SystemAPI.GetComponentLookupState());
    m_DensityLookup.Update(ref SystemAPI.GetComponentLookupState());
    // ... use caches in jobs
}
```

---

## 10. RECOMMENDED ROAD SUGGESTION CRITERIA

### Priority Scoring Formula

```
suggestion_priority = (
    connectivity_score * 0.3 +      // Fill network gaps
    density_score * 0.4 +           // High development
    congestion_score * 0.2 +        // Reduce traffic
    grid_alignment_score * 0.1      // Aesthetic alignment
)
```

### Key Metrics

| Metric | Calculation | Use |
|--------|-------------|-----|
| Block Density | Buildings per 64m² | Identify high development |
| Edge Congestion | Duration on length ratio | Find bottlenecks |
| Connectivity Gap | Min distance to next node | Suggest connections |
| Grid Alignment | Dot product with block direction | Organic placement |
| Residents/Employees | Count on connected buildings | Demand signal |

---

## 11. EXAMPLE: COMPLETE SUGGESTION SYSTEM

See implementation guide in next section.

---

## SUMMARY: KEY FILES TO IMPLEMENT

### Essential Reading (in order):

1. **Core Components**:
   - `/Game.Net/Node.cs` - Node structure
   - `/Game.Net/Edge.cs` - Edge structure
   - `/Game.Net/Curve.cs` - Curve geometry
   - `/Game.Net/Road.cs` - Traffic data

2. **Creation Pipeline**:
   - `/Game.Tools/GenerateNodesSystem.cs` - Node creation
   - `/Game.Tools/GenerateEdgesSystem.cs` - Edge creation
   - `/Game.Tools/ApplyNetSystem.cs` - Finalization

3. **Analysis**:
   - `/Game.Simulation/NetEdgeDensitySystem.cs` - Density calculation
   - `/Game.Zones/ZoneUtils.cs` - Block helpers
   - `/Game.Buildings/Building.cs` - Building references

4. **Visualization**:
   - `/Game.Debug/DensityDebugSystem.cs` - Gizmo drawing pattern
   - `/Game.Debug/LaneDebugSystem.cs` - Debug visualization

5. **Prefab Data**:
   - `/Game.Prefabs/NetData.cs` - Road type definition
   - `/Game.Prefabs/NetGeometryData.cs` - Road properties

