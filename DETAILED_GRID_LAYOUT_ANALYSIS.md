# Cities: Skylines II Grid Tool and Layout Generation - Detailed Technical Analysis

## EXECUTIVE SUMMARY

This document provides an in-depth technical analysis of the Cities: Skylines II built-in grid tool system and how it creates multiple roads in one operation. The system uses a rectangular grid algorithm with Bezier curve interpolation, sophisticated parallel road generation, and a hierarchical course-based architecture.

---

## 1. GRID TOOL SYSTEM ARCHITECTURE

### 1.1 Grid Mode Overview

**Location**: `Game.Tools.NetToolSystem.cs` (7807 lines)

The grid tool is one of 7 placement modes:
```csharp
public enum Mode
{
    Straight,      // Single road
    SimpleCurve,   // One control point
    ComplexCurve,  // Multiple control points  
    Continuous,    // Chain segments
    Grid,          // **FOCUS: Rectangular grid layout**
    Replace,       // Upgrade existing
    Point          // Single junction
}
```

### 1.2 Grid Input Structure

The grid tool takes 3 control points that define the grid rectangle:

```csharp
// From CreateGrid() method (line 4035)
ControlPoint controlPoint = m_ControlPoints[0];      // Corner A
ControlPoint controlPoint2 = m_ControlPoints[1];     // Corner B (intermediate)
ControlPoint controlPoint3 = m_ControlPoints[m_ControlPoints.Length - 1];  // Corner C

// These define two perpendicular lines that form the grid
// Point A = grid origin
// Point B = defines one axis (adjusted to be perpendicular projection)
// Point C = defines other axis
```

**Geometric Interpretation**:
- User draws line from A to B (first axis)
- User draws line from B to C (second axis)
- Grid is generated orthogonal to these lines

### 1.3 Grid Calculation Algorithm

**Step 1: Validate Perpendicularity**
```csharp
// Line 4052-4055: Adjust B to be perpendicular projection of C onto A-B line
bool flag = math.dot(controlPoint3.m_Position.xz - controlPoint2.m_Position.xz, 
                     MathUtils.Right(controlPoint2.m_Direction)) > 0f;
flag ^= math.dot(controlPoint3.m_Position.xz - controlPoint.m_Position.xz, 
                 controlPoint2.m_Direction) < 0f;
float3 @float = new float3(controlPoint2.m_Direction.x, 0f, controlPoint2.m_Direction.y);
controlPoint2.m_Position = controlPoint.m_Position + 
    @float * math.dot(controlPoint3.m_Position - controlPoint.m_Position, @float);
```

**Step 2: Calculate Grid Dimensions**
```csharp
// Line 4056-4061: Determine number of rows and columns
float2 float2 = new float2(
    math.distance(controlPoint.m_Position.xz, controlPoint2.m_Position.xz),  // Width
    math.distance(controlPoint2.m_Position.xz, controlPoint3.m_Position.xz)  // Height
);

// Calculate cell spacing based on road width
float2 float3 = netGeometryData.m_DefaultWidth * new float2(16f, 8f);

// Calculate number of cells needed
float2 float4 = math.max(1f, math.ceil((float2 - 0.16f) / float3));

// Adjust spacing to fit exact number of roads
float3 = float2 / float4;

// Final grid dimensions
int2 @int = new int2(
    Mathf.RoundToInt(float4.x),  // Number of horizontal roads
    Mathf.RoundToInt(float4.y)   // Number of vertical roads
);
```

**Key Calculation Details**:
- `float2` = actual width/height in meters
- `float3` = cell spacing (calculated from road width)
- `float4` = number of cells (rounded)
- `@int` = final grid dimensions

### 1.4 Multiple Road Creation Loop

**Core Grid Generation (Line 4091-4310)**:

```csharp
// Nested loops create every road in the grid
while (int2.y <= @int.y)  // For each horizontal line of roads
{
    // Calculate positions along the vertical axis
    float cutPosition = GetCutPosition(netGeometryData, length2, (float)int2.y / (float)@int.y);
    float cutPosition2 = GetCutPosition(netGeometryData, length2, (float)(int2.y + 1) / (float)@int.y);
    
    // Get segment endpoints for this row
    line5.a = MathUtils.Position(line, cutPosition);    // Start of row
    line5.b = MathUtils.Position(line2, cutPosition);
    line6.a = MathUtils.Position(line, cutPosition2);   // End of row
    line6.b = MathUtils.Position(line2, cutPosition2);
    
    // Create horizontal roads
    while (int2.x < @int.x)  // For each vertical line of roads
    {
        Entity e = m_CommandBuffer.CreateEntity();
        
        // Create road definition
        CreationDefinition component = new CreationDefinition
        {
            m_Prefab = m_NetPrefab,
            m_SubPrefab = m_LanePrefab,
            m_RandomSeed = random.NextInt()
        };
        m_CommandBuffer.AddComponent(e, component);
        
        // Determine start/end points for this road segment
        ControlPoint controlPoint4 = (int2.x == 0) 
            ? controlPoint 
            : new ControlPoint { /* interpolated position */ };
        
        ControlPoint controlPoint5 = ((int2.x + 1) == @int.x) 
            ? controlPoint3 
            : new ControlPoint { /* interpolated position */ };
        
        // Create course (road definition)
        NetCourse netCourse = default(NetCourse);
        netCourse.m_Curve = NetUtils.StraightCurve(controlPoint4.m_Position, controlPoint5.m_Position);
        netCourse.m_StartPosition = GetCoursePos(netCourse.m_Curve, controlPoint4, 0f);
        netCourse.m_EndPosition = GetCoursePos(netCourse.m_Curve, controlPoint5, 1f);
        
        // Mark as grid road
        netCourse.m_StartPosition.m_Flags |= CoursePosFlags.IsGrid;
        netCourse.m_EndPosition.m_Flags |= CoursePosFlags.IsGrid;
        
        // Mark parallel roads appropriately
        if (int2.y != 0)
        {
            netCourse.m_StartPosition.m_Flags |= CoursePosFlags.IsParallel;
            netCourse.m_EndPosition.m_Flags |= CoursePosFlags.IsParallel;
        }
        
        netCourse.m_Length = MathUtils.Length(netCourse.m_Curve);
        netCourse.m_FixedIndex = -1;
        
        m_CommandBuffer.AddComponent(e, netCourse);
        int2.x++;
    }
    
    // Create vertical roads (perpendicular set)
    // ... similar structure, creates @int.x vertical segments
    
    int2.y++;
}
```

**Critical Features**:
1. **Double Grid**: Creates both horizontal AND vertical roads
2. **Alternating Direction**: Odd/even rows alternate direction for proper connectivity
3. **Bezier Interpolation**: Uses curve interpolation for smooth alignment
4. **Parallel Road Marking**: Sets `CoursePosFlags.IsParallel` for proper node connection
5. **Random Seeds**: Each road gets unique random seed for variation

### 1.5 Edge Cases Handled

```csharp
// Line 4062-4070: Degenerate cases
if (@int.y == 0)
{
    // One-dimensional grid (single column)
    CreateStraightLine(ref ownerDefinitions, new int2(0, 1));
    return;
}

if (@int.x == 0)
{
    // One-dimensional grid (single row)
    CreateStraightLine(ref ownerDefinitions, new int2(1, m_ControlPoints.Length - 1));
    return;
}
```

---

## 2. MULTIPLE ROAD CREATION MECHANISM

### 2.1 NetCourse Structure

Each road is represented by a `NetCourse` - a complete road definition:

```csharp
public struct NetCourse : IComponentData, IQueryTypeParameter
{
    public CoursePos m_StartPosition;    // Start node definition
    public CoursePos m_EndPosition;      // End node definition
    public Bezier4x3 m_Curve;           // 3D cubic bezier curve
    public float2 m_Elevation;          // Elevation metadata
    public float m_Length;              // Cached curve length
    public int m_FixedIndex;            // Link to fixed geometry
}

public struct CoursePos
{
    public Entity m_Entity;             // Connected node entity
    public float3 m_Position;           // 3D position
    public quaternion m_Rotation;       // Direction/rotation
    public float2 m_Elevation;          // Elevation info
    public float m_CourseDelta;         // Position along curve (0-1)
    public float m_SplitPosition;       // Position within segment
    public CoursePosFlags m_Flags;      // Parallel, First, Last, Grid, etc.
    public int m_ParentMesh;            // Parent mesh reference
}
```

### 2.2 Parallel Road System

The grid tool leverages the powerful parallel road system to create multiple roads from one base:

**From CreateParallelCourses() (Line 3764-3816)**:

```csharp
private void CreateParallelCourses(CreationDefinition definitionData, OwnerDefinition ownerDefinition, 
                                   NetCourse courseData, NativeParallelHashMap<float4, float3> nodeMap)
{
    float num = m_ParallelOffset;
    
    // Add road width to calculate actual spacing
    if (m_NetGeometryData.HasComponent(m_NetPrefab))
    {
        NetGeometryData netGeometryData = m_NetGeometryData[m_NetPrefab];
        num += netGeometryData.m_DefaultWidth;  // Total spacing
    }
    
    // Create left-side parallel roads
    for (int i = 1; i <= m_ParallelCount.x; i++)
    {
        // Offset curve to the left
        NetCourse netCourse3 = netCourse;
        netCourse3.m_Curve = NetUtils.OffsetCurveLeftSmooth(netCourse.m_Curve, num);
        
        // Snap endpoints to existing nodes (node deduplication)
        float4 key = new float4(netCourse.m_Curve.a, -i);
        if (!nodeMap.TryAdd(key, netCourse3.m_Curve.a))
        {
            netCourse3.m_Curve.a = nodeMap[key];  // Reuse existing node
        }
        
        // Recursively subdivide if needed
        CreateParallelCourse(definitionData, ownerDefinition, netCourse, netCourse3, 
                           num, elevationLimit, (i & 1) != 0, 
                           i == m_ParallelCount.x, isRight: false, 0, ref random);
        
        netCourse = netCourse3;
    }
    
    // Create right-side parallel roads (same process, negative offset)
    // ...
}
```

**Key Features**:
- **OffsetCurveLeftSmooth()**: Uses bezier curve offset algorithm
- **Node Deduplication**: NativeParallelHashMap ensures roads share intersection nodes
- **Recursive Subdivision**: Handles curve splits if parallel distance exceeds tolerance

### 2.3 Offset Curve Algorithm

The system uses `NetUtils.OffsetCurveLeftSmooth()` to create parallel bezier curves:

```csharp
// Pseudo-code for curve offsetting
private Bezier4x3 OffsetCurveLeftSmooth(Bezier4x3 original, float offset)
{
    // Offset each control point perpendicular to curve
    Bezier4x3 result = original;
    
    // At parameter t=0
    float2 tangent0 = StartTangent(original).xz;
    float2 perpendicular0 = Right(tangent0);  // 90 degree rotation
    result.a.xz += perpendicular0 * offset;
    result.b.xz += perpendicular0 * offset;
    
    // At parameter t=1
    float2 tangent1 = EndTangent(original).xz;
    float2 perpendicular1 = Right(tangent1);
    result.c.xz += perpendicular1 * offset;
    result.d.xz += perpendicular1 * offset;
    
    return result;
}
```

### 2.4 Batch Creation Pipeline

The complete pipeline for grid creation:

```
CreateGrid()
├── Validate grid dimensions and spacing
├── Create NetCourse for each road segment
│   └── Use Bezier curve interpolation for positioning
├── Add CreationDefinition component
│   └── Marks road type and prefab
├── For each road:
│   ├── Set StartPosition and EndPosition
│   ├── Mark as IsGrid flag
│   ├── Set IsParallel for non-border roads
│   └── Add to command buffer
├── System barrier triggers
├── GenerateNodesSystem processes roads
│   └── Creates node entities at curve endpoints
├── GenerateEdgesSystem
│   └── Creates edge entities and connections
└── GenerateAggregatesSystem
    └── Creates lanes and visual geometry
```

---

## 3. ZONE AND BLOCK GENERATION SYSTEM

### 3.1 Block Structure

A block represents a rectangular area bounded by roads:

```csharp
public struct Block : IComponentData, IQueryTypeParameter
{
    public float3 m_Position;    // Center position
    public float2 m_Direction;   // Normal vector (normalized)
    public int2 m_Size;          // Width x Height in cells
                                 // Each cell = 8 meters
}
```

**Size Interpretation**:
- `m_Size.x` = width in 8m cells
- `m_Size.y` = depth in 8m cells
- Actual dimensions: `m_Size * 8` meters

### 3.2 Cell Grid System

Blocks use a fine-grained cell system for zoning:

```csharp
// From ZoneUtils.cs
public const float CELL_SIZE = 8f;       // Each cell is 8m x 8m
public const float CELL_AREA = 64f;      // 8m * 8m = 64m²
public const int MAX_ZONE_WIDTH = 10;    // Max 10 cells wide
public const int MAX_ZONE_DEPTH = 6;     // Max 6 cells deep

// Cell coordinates within a block
public struct Cell : IBufferElementData
{
    public CellFlags m_State;    // Visible, Occupied, Selected
    public ZoneType m_Zone;      // Residential, Commercial, etc.
}
```

### 3.3 Block Positioning Helpers

```csharp
// From ZoneUtils.cs
public static float3 GetCellPosition(Block block, int2 cellIndex)
{
    // Convert cell index to world position
    float2 @float = (float2)(block.m_Size - (cellIndex << 1) - 1) * 4f;
    float3 position = block.m_Position;
    position.xz += block.m_Direction * @float.y;
    position.xz += MathUtils.Right(block.m_Direction) * @float.x;
    return position;
}

public static int2 GetCellIndex(Block block, float2 position)
{
    // Inverse: convert world position to cell index
    float2 y = MathUtils.Right(block.m_Direction);
    float2 x = block.m_Position.xz - position;
    return (int2)math.floor(
        (new float2(math.dot(x, y), math.dot(x, block.m_Direction)) + 
         (float2)block.m_Size * 4f) / 8f
    );
}

public static Quad2 CalculateCorners(Block block)
{
    // Get the four corners of the block
    float2 @float = (float2)block.m_Size * 4f;
    float2 float2 = block.m_Direction * @float.y;
    float2 float3 = MathUtils.Right(block.m_Direction) * @float.x;
    float2 float4 = block.m_Position.xz + float2;
    float2 float5 = block.m_Position.xz - float2;
    return new Quad2(float4 + float3, float4 - float3, float5 - float3, float5 + float3);
}
```

### 3.4 Block Creation from Roads

**From GenerateZonesSystem.cs**:

The system automatically creates blocks from road layouts:

```csharp
// Flow:
// 1. Road network placement (via NetToolSystem)
// 2. GenerateZonesSystem processes road segments
// 3. Creates blocks in areas bounded by roads
// 4. Populates block cell data
// 5. Stores in DynamicBuffer<Cell>

// Block queries:
var blockQuery = GetEntityQuery(
    ComponentType.ReadOnly<Block>(),
    ComponentType.ReadOnly<BuildOrder>()
);
```

### 3.5 Zoning Application

**From GenerateZonesSystem (Lines 267-277)**:

```csharp
// Three zoning application modes:

// 1. Paint - single point zoning
if ((zoningData.m_Flags & ZoningFlags.Paint) != 0)
{
    // Find nearest cell in block
    // Apply zone type
}

// 2. Marquee - area zoning
if ((zoningData.m_Flags & ZoningFlags.Marquee) != 0)
{
    // Find all cells within polygon
    // Apply zone type to all
    // Uses MarqueeIterator to find intersecting blocks
}

// 3. FloodFill - connected zoning
if ((zoningData.m_Flags & ZoningFlags.FloodFill) != 0)
{
    // BFS algorithm through adjacent cells
    // Fills continuous regions of same type
    // Respects block boundaries
}
```

---

## 4. TOOL UI AND INTERACTION PATTERNS

### 4.1 Control Point System

All tools use a control point array for user interaction:

```csharp
public struct ControlPoint
{
    public float3 m_Position;          // Snapped world position
    public float3 m_HitPosition;       // Raw raycast hit
    public float2 m_Direction;         // Normalized direction
    public float3 m_HitDirection;      // Raw raycast direction
    public quaternion m_Rotation;      // Orientation
    public Entity m_OriginalEntity;    // Snapped-to entity (road/building/zone)
    public float2 m_SnapPriority;      // Priority scoring (x=distance, y=type)
    public int2 m_ElementIndex;        // Index within snapped entity
    public float m_CurvePosition;      // Position along curve (0-1)
    public float m_Elevation;          // Height
}

private NativeList<ControlPoint> m_ControlPoints;  // Current path
```

### 4.2 Snapping Priority System

The system evaluates snap points using a priority scoring system:

```csharp
// From ToolUtils.CalculateSnapPriority()
public static float SnapPriority(
    float level,           // Base priority level (0-3)
    float typeWeight,      // Weight for snap type
    float heightWeight,    // Weight for height snapping
    float3 hitPosition,
    float3 snapPosition,
    float2 direction)
{
    // Formula:
    // priority = (level * 1000) 
    //          - (distance * 10) 
    //          + (typeWeight * 100)
    //          + (heightWeight * 50)
    
    // Lower distance = higher priority
    // Higher level = higher priority (e.g., exact node > edge > free space)
}
```

**Snap Levels** (higher = better):
- Level 3: Existing geometry (roads, buildings)
- Level 2: Zone grid alignment
- Level 1: Generic snapping (distance-based)
- Level 0: Free placement

### 4.3 Area Definition Methods

**Grid Tool Input** (3-point system):
```
Click 1: Origin corner (A)
         ↓
Click 2: First axis direction (B)
         ↓ 
Click 3: Second axis direction (C)
         ↓
System creates rectangular grid with A-B as width, B-C as height
```

**Other Tool Methods** (from AreaToolSystem.cs):
```csharp
public enum Mode
{
    Edit,      // Polygon editing - click to add vertices
    Generate   // Area generation from constraints
}

// Polygon tool: Click to define arbitrary polygon
// Each click adds a vertex
// System snaps to existing geometry for alignment
```

### 4.4 Preview and Validation System

The system provides real-time feedback through preview entities:

```csharp
// All created roads are marked as Temp initially
EntityManager.SetComponentData(entity, new Temp
{
    m_Flags = TempFlags.Create,
    m_Original = Entity.Null
});

// Validation systems check:
// - Terrain conflicts
// - Existing road overlaps
// - Elevation violations
// - Building intersections

// If valid → Temp flag remains, awaiting user confirmation
// If invalid → Error feedback, no creation allowed
// User presses OK → Temp flags cleared, entities become permanent
// User presses Cancel → Temp entities deleted
```

### 4.5 Parameter Adjustment UI

Grid tool parameters exposed via UI:

```csharp
public int m_ParallelCount;      // Additional parallel roads
public float m_ParallelOffset;   // Distance between parallel roads
public float m_Elevation;        // Road elevation
public float m_ElevationStep;    // Elevation increment
public Snap m_Snap;              // Snapping modes
public bool m_Underground;       // Create underground
```

Users adjust these before/during tool placement to customize grid generation.

---

## 5. REALISTIC LAYOUT PATTERNS AND PROCEDURAL SYSTEMS

### 5.1 Curve Generation Utilities

The system provides sophisticated curve utilities for organic layouts:

**From NetUtils.cs**:
```csharp
// 1. Straight curves
public static Bezier4x3 StraightCurve(float3 start, float3 end)
{
    // All control points collinear with start/end
    // Results in perfectly straight line
}

// 2. Fitted curves
public static Bezier4x3 FitCurve(float3 start, float3 tangentStart, 
                                 float3 tangentEnd, float3 end)
{
    // Bezier curve constrained by tangent directions
    // Used for smooth transitions between segments
    // Exact tangent matching ensures G1 continuity
}

// 3. Offset curves
public static Bezier4x3 OffsetCurveLeftSmooth(Bezier4x3 original, float offset)
{
    // Parallel curve at perpendicular distance
    // Maintains curve smoothness
    // Used for parallel road generation
}

// 4. Length calculation
public static float Length(Bezier4x3 curve)
{
    // Arc length approximation via Gaussian quadrature
    // Cached for performance
}

// 5. Position/Tangent sampling
public static float3 Position(Bezier4x3 curve, float t)  // t in [0,1]
public static float3 Tangent(Bezier4x3 curve, float t)
public static float3 StartTangent(Bezier4x3 curve)
public static float3 EndTangent(Bezier4x3 curve)
```

### 5.2 Curve Path Algorithms

The system supports sophisticated path algorithms:

**From CreateParallelCourse() (Line 3818-3862)**:

```csharp
// Adaptive subdivision for parallel roads
private void CreateParallelCourse(CreationDefinition definitionData, 
                                 OwnerDefinition ownerDefinition, 
                                 NetCourse courseData, 
                                 NetCourse courseData2, 
                                 float parallelOffset, 
                                 float elevationLimit, 
                                 bool invert, 
                                 bool isLeft, 
                                 bool isRight, 
                                 int level, 
                                 ref Unity.Mathematics.Random random)
{
    // Check if curve needs subdivision
    float num = math.abs(parallelOffset);
    if (++level >= 10 || 
        math.distance(courseData2.m_Curve.a.xz, courseData2.m_Curve.d.xz) < num * 2f)
    {
        // Curve too short or too deep → finalize
        CreateParallelCourse(definitionData, ownerDefinition, courseData2, 
                           elevationLimit, invert, isLeft, isRight, ref random);
        return;
    }
    
    // Find point on original curve closest to parallel midpoint
    float3 @float = MathUtils.Position(courseData2.m_Curve, 0.5f);
    float t;
    float num2 = MathUtils.Distance(courseData.m_Curve.xz, @float.xz, out t);
    
    // If deviation from target distance exceeds tolerance
    if (math.abs(num2 - num) > num * 0.02f || 
        math.dot(float3, @float.xz - float2.xz) < 0f)
    {
        // Subdivide curve
        float3 value2 = MathUtils.StartTangent(courseData2.m_Curve);
        float3 value3 = MathUtils.EndTangent(courseData2.m_Curve);
        
        // Split at midpoint and recursively process each half
        NetCourse courseData3 = courseData2;
        courseData3.m_Curve = NetUtils.FitCurve(
            courseData2.m_Curve.a, value2, value, float2);
        
        NetCourse courseData4 = courseData2;
        courseData4.m_Curve = NetUtils.FitCurve(
            float2, value, value3, courseData2.m_Curve.d);
        
        // Recurse on both segments
        CreateParallelCourse(..., courseData3, ..., level+1, ...);
        CreateParallelCourse(..., courseData4, ..., level+1, ...);
    }
    else
    {
        // Acceptable curve → finalize
        CreateParallelCourse(..., courseData2, ..., ...);
    }
}
```

### 5.3 Intersection and Connection Patterns

**Automatic Intersection Handling**:

The GenerateEdgesSystem automatically creates intersections where roads cross:

```csharp
// From GenerateEdgesSystem (Line 145-238)
// CheckNodesJob identifies intersection points

// Process:
// 1. For each new edge being created
// 2. Sample curve at regular intervals
// 3. Check intersection with existing roads
// 4. At intersections: create new node entity
// 5. Split edge at intersection point
// 6. Connect both road edges to new intersection node
```

### 5.4 Suggested Layout Generation Algorithms

**For Organic Cul-de-Sacs**:
```
1. Identify dead-ends (T-shaped intersections)
2. Generate circular arc connecting to nearby parallel street
3. Create intermediate nodes on curve
4. Apply randomization to start angle/radius
5. Limit curve radius based on terrain/blocks
```

**For Curved Streets**:
```
1. Start with two parallel roads
2. Apply sine wave function along length
3. Sample at discrete intervals
4. Create bezier curve through samples
5. Apply smooth tangent constraints
```

**For Irregular Blocks**:
```
1. Analyze existing block sizes/orientations
2. Generate "keystone" irregular blocks at intersections
3. Add chamfered corners to sharp angles
4. Use curve offsetting for smooth transitions
```

**For Natural Variations**:
```
1. Create base grid layout
2. Apply Perlin/Simplex noise to node positions
3. Clamp displacement to minimum separation distance
4. Regenerate curves with adjusted endpoints
5. Validate no self-intersections
```

**For Hierarchical Networks**:
```
Arterial Roads (tier 1):
- 20m width minimum
- 100m minimum spacing
- Connect major districts

Collector Roads (tier 2):
- 12m width typical
- 60m typical spacing
- Connect neighborhoods

Local Streets (tier 3):
- 8m width typical
- 30m typical spacing
- Access individual plots

Cul-de-sacs (dead-ends):
- 8m width
- ~40m length maximum
- Connect to local streets
```

---

## 6. IMPLEMENTATION RECOMMENDATIONS FOR ORGANIC LAYOUTS

### 6.1 Analysis Phase Architecture

```csharp
[CompilerGenerated]
public class OrganicRoadAnalysisSystem : GameSystemBase
{
    // Query high-demand areas
    private EntityQuery m_HighDensityEdges;
    private EntityQuery m_PopulousBlocks;
    
    // Identify existing gaps
    private EntityQuery m_DisconnectedBuildings;
    
    // Analyze traffic patterns
    private EntityQuery m_CongestedRoads;
    
    protected override void OnUpdate()
    {
        // Step 1: Scan building density
        AnalyzeBuildingDensity();
        
        // Step 2: Identify block connectivity
        AnalyzeBlockGraph();
        
        // Step 3: Detect traffic bottlenecks
        AnalyzeTrafficFlow();
        
        // Step 4: Generate suggestions
        GenerateSuggestions();
    }
}
```

### 6.2 Suggestion Prioritization

```csharp
public struct RoadSuggestion
{
    public float3 m_StartPosition;
    public float3 m_EndPosition;
    public Entity m_RoadPrefab;
    public float m_Priority;           // 0-1 normalized
    public SuggestionType m_Type;
    public float m_ExpectedDensity;    // Projected residents on road
    public float m_CongestionReduction; // % traffic reduction
}

public enum SuggestionType
{
    ConnectPopulousBlocks,     // High density + separated
    CompleteCulDeSac,          // Dead-end to through-road
    ReduceBottleneck,          // Traffic convergence point
    FillNetworkGap,            // Large unpaved area between blocks
    ImproveConnectivity,       // Isolated building cluster
    HierarchicalUpgrade        // Upgrade local street to collector
}
```

### 6.3 Curve Generation for Natural Feel

```csharp
// Instead of perfect grid, use noise-based curves
private Bezier4x3 GenerateOrganicCurve(float3 start, float3 end, 
                                      float noiseScale, int seed)
{
    var random = new Unity.Mathematics.Random((uint)seed);
    
    // Midpoint with perpendicular displacement
    float3 midpoint = (start + end) * 0.5f;
    float2 direction = math.normalize((end - start).xz);
    float2 perpendicular = MathUtils.Right(direction);
    
    // Add displacement based on noise
    float displacement = noise.perlin(midpoint.xz * noiseScale) * 10f;
    midpoint.xz += perpendicular * displacement;
    
    // Create curve with natural tangents
    float3 tangentStart = math.normalize(
        (midpoint - start).xz * new float2(1, 0.5f));  // Slightly upward curve
    float3 tangentEnd = math.normalize(
        (end - midpoint).xz * new float2(1, 0.5f));
    
    return NetUtils.FitCurve(start, 
        new float3(tangentStart, 0), 
        new float3(tangentEnd, 0), 
        end);
}
```

### 6.4 Multi-Pass Road Generation

```csharp
// Phase 1: Arterial roads (main corridors)
GenerateArterialNetwork(district);

// Phase 2: Collector roads (neighborhood distribution)
GenerateCollectorNetwork(district);

// Phase 3: Local streets (fine-grained access)
GenerateLocalStreetNetwork(district);

// Phase 4: Cul-de-sacs (dead-end terminations)
GenerateCulDeSacs(district);

// Phase 5: Validation & cleanup
ValidateConnectivity();
RemoveRedundantRoads();
OptimizeIntersectionAngles();
```

### 6.5 Validation and Constraints

```csharp
// Check road doesn't pass through blocked areas
bool IsValidPlacement(Bezier4x3 curve)
{
    // Sample at 5m intervals
    int samples = (int)(MathUtils.Length(curve) / 5f);
    
    for (int i = 0; i <= samples; i++)
    {
        float t = (float)i / samples;
        float3 point = MathUtils.Position(curve, t);
        
        // Check building collision
        if (CheckBuildingCollision(point)) return false;
        
        // Check elevation slope
        if (CheckElevationSlope(curve, t)) return false;
        
        // Check water overlap
        if (CheckWaterCollision(point)) return false;
        
        // Check existing road proximity (minimum separation)
        if (CheckRoadProximity(point, 8f)) return false;
    }
    
    return true;
}
```

---

## 7. ALGORITHM COMPLEXITY AND PERFORMANCE CONSIDERATIONS

### 7.1 Grid Creation Complexity

- Time: **O(n × m)** where n = width cells, m = height cells
- Space: **O(n × m)** for course entities
- For 10×6 grid = 60 roads = sub-millisecond on modern hardware

### 7.2 Parallel Road Generation Complexity

- Time: **O(k log d)** where k = number of parallel roads, d = recursion depth
- Recursive subdivision ensures accuracy without excessive geometry
- Typical depth: 2-4 levels for 50m offset curves

### 7.3 Zone Generation Complexity

- Block creation: **O(1)** per block (cells are pre-allocated)
- Cell updates: **O(cells)** = O(60) per block maximum
- Zoning application: **O(n)** where n = affected blocks

### 7.4 Optimization Techniques

```csharp
// 1. Burst compilation for hot loops
[BurstCompile]
private struct GenerateGridJob : IJob { ... }

// 2. Job scheduling with proper dependencies
JobHandle nodeGeneration = nodesJob.Schedule(dependency);
JobHandle edgeGeneration = edgesJob.Schedule(nodeGeneration);

// 3. Allocation pooling
NativeParallelHashMap<float4, float3> nodeMap = 
    new NativeParallelHashMap<float4, float3>(100, Allocator.TempJob);

// 4. Early exit conditions
if (distance < tolerance && recursion > maxDepth) return;

// 5. Spatial hashing for collision detection
NativeQuadTree<Entity, QuadTreeBoundsXZ> spatialIndex = 
    m_SearchSystem.GetSearchTree();
```

---

## 8. SUMMARY: KEY TAKEAWAYS

### Grid Tool Implementation
1. Takes 3 control points defining rectangular grid corners
2. Calculates grid dimensions from spacing constraints
3. Creates all roads in nested loops (horizontal then vertical)
4. Uses Bezier curve interpolation for positioning
5. Leverages parallel road system for offset generation
6. Handles edge cases and degenerate grids gracefully

### Multiple Road Creation
1. Each road defined by NetCourse structure
2. Roads grouped in batches for entity creation
3. Node deduplication via hashmap ensures intersection sharing
4. Recursive curve subdivision handles complex parallel offsets
5. Flags system (IsGrid, IsParallel, IsFirst/Last) controls connectivity

### Zone and Block System
1. Blocks are rectangular containers aligned with road grid
2. Cells provide fine 8m×8m granularity for zoning
3. Automatic block creation from road networks
4. Multiple zoning application methods (Paint, Marquee, FloodFill)

### Tool Interaction
1. Control points accumulate user clicks
2. Snapping system evaluates multiple snap targets with priority scoring
3. Real-time preview via Temp entities
4. Validation before confirmation
5. Parameter adjustment allows customization

### For Organic Layouts
1. Use curve offset algorithms for natural parallel spacing
2. Apply noise-based displacement for organic feel
3. Hierarchical generation (arterial → collector → local)
4. Cul-de-sac generation with adaptive curves
5. Validation ensures practical constraints (slope, collision, separation)

---

## APPENDIX: KEY FILES AND FUNCTIONS

| File | Key Functions | Purpose |
|------|---|---|
| NetToolSystem.cs | CreateGrid() | Grid generation |
| NetToolSystem.cs | CreateParallelCourses() | Parallel road generation |
| NetToolSystem.cs | CreateStraightLine() | Single road creation |
| GenerateZonesSystem.cs | MarqueeBlocks() | Area zoning |
| GenerateZonesSystem.cs | FloodFillBlocks() | Connected zoning |
| ZoneUtils.cs | GetCellPosition() | Cell lookup |
| ZoneUtils.cs | CalculateCorners() | Block geometry |
| AreaToolSystem.cs | SnapJob | Area snapping |
| NetUtils.cs | StraightCurve() | Straight line creation |
| NetUtils.cs | OffsetCurveLeftSmooth() | Parallel curves |
| NetUtils.cs | FitCurve() | Constrained curves |

