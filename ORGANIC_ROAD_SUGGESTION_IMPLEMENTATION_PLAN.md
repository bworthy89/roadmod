# Organic Road Suggestion System - Implementation Plan
## Cities: Skylines II Mod

---

## OVERVIEW

**Goal**: Create an intelligent system that analyzes city development patterns and suggests road placements organically based on:
- Building density and population
- Traffic congestion patterns
- Connectivity gaps (dead ends, isolated blocks)
- Zoning grid alignment
- Terrain and natural city growth

**Approach**: Multi-phase ECS-based system that analyzes the network in real-time, generates suggestions, and visualizes them for the player.

---

## SYSTEM ARCHITECTURE

### Three-System Design

```
┌─────────────────────────────────────────────────────────┐
│ 1. OrganicRoadAnalysisSystem (GameSystemBase)          │
│    - Analyzes city development every 60 frames          │
│    - Identifies high-priority areas                     │
│    - Outputs: SuggestionCandidate components            │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ 2. RoadSuggestionGenerationSystem (GameSystemBase)     │
│    - Processes candidates into concrete suggestions     │
│    - Validates placement (terrain, intersections)       │
│    - Scores and prioritizes suggestions                 │
│    - Outputs: RoadSuggestion entities                   │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ 3. RoadSuggestionVisualizationSystem (GameSystemBase)  │
│    - Renders suggestions as overlay gizmos              │
│    - Color-codes by priority                            │
│    - Handles player interaction (click to apply)        │
└─────────────────────────────────────────────────────────┘
```

---

## COMPONENT DEFINITIONS

### 1. SuggestionCandidate (IComponentData)
```csharp
public struct SuggestionCandidate : IComponentData
{
    public float3 m_FromPosition;      // Start point
    public float3 m_ToPosition;        // End point
    public SuggestionReason m_Reason;  // Why this is suggested
    public float m_RawScore;           // Pre-validation score
}

public enum SuggestionReason : byte
{
    ConnectHighDensityBlocks,   // Connect populous blocks
    ReduceCongestion,            // Alleviate traffic bottleneck
    CompleteGrid,                // Logical grid extension
    ImproveConnectivity,         // Connect isolated areas
    DeadEndElimination,          // Convert dead-end to through-road
    ParallelRoute,               // Add capacity to busy road
}
```

### 2. RoadSuggestion (IComponentData)
```csharp
public struct RoadSuggestion : IComponentData
{
    public Entity m_StartNode;         // Snap to existing node or null
    public Entity m_EndNode;           // Snap to existing node or null
    public float3 m_StartPosition;     // World position (snapped)
    public float3 m_EndPosition;       // World position (snapped)
    public Bezier4x3 m_Curve;          // Suggested curve
    public Entity m_RoadPrefab;        // Road type to use
    public float m_Priority;           // Final priority (0-1)
    public SuggestionReason m_Reason;  // Why this is needed
    public SuggestionFlags m_Flags;    // State flags
}

[Flags]
public enum SuggestionFlags : byte
{
    None = 0,
    Validated = 1 << 0,      // Passed validation
    Visible = 1 << 1,        // Currently shown to player
    Applied = 1 << 2,        // Player has applied this
    Rejected = 1 << 3,       // Player rejected this
}
```

### 3. SuggestionMetrics (IComponentData)
```csharp
// Cache analysis data on suggestion entities
public struct SuggestionMetrics : IComponentData
{
    public float m_DensityScore;        // Population density (0-1)
    public float m_ConnectivityScore;   // Gap-filling value (0-1)
    public float m_CongestionScore;     // Traffic relief (0-1)
    public float m_GridAlignmentScore;  // Zoning alignment (0-1)
    public int m_AffectedBuildings;     // Buildings this would serve
    public float m_EstimatedTraffic;    // Expected traffic load
}
```

---

## IMPLEMENTATION PHASES

### Phase 1: Analysis System

**File**: `OrganicRoadAnalysisSystem.cs`

**Update Frequency**: Every 60 frames (approx. 1 second)

**Responsibilities**:
1. Query high-density road edges
2. Find blocks with many buildings
3. Identify dead-end nodes
4. Detect traffic congestion
5. Create SuggestionCandidate entities

**Key Queries**:
```csharp
private EntityQuery m_HighDensityEdgesQuery;
private EntityQuery m_DevelopedBlocksQuery;
private EntityQuery m_DeadEndNodesQuery;
private EntityQuery m_CongestedRoadsQuery;

protected override void OnCreate()
{
    base.OnCreate();

    // High density edges (>10 people per meter)
    m_HighDensityEdgesQuery = GetEntityQuery(new EntityQueryDesc
    {
        All = new[]
        {
            ComponentType.ReadOnly<Edge>(),
            ComponentType.ReadOnly<Curve>(),
            ComponentType.ReadOnly<Density>(),
            ComponentType.ReadOnly<ConnectedBuilding>(),
        },
    });

    // Developed blocks (>15 buildings)
    m_DevelopedBlocksQuery = GetEntityQuery(new EntityQueryDesc
    {
        All = new[]
        {
            ComponentType.ReadOnly<Block>(),
            ComponentType.ReadOnly<ValidArea>(),
        },
    });

    // Dead-end nodes (only 1 connected edge)
    m_DeadEndNodesQuery = GetEntityQuery(new EntityQueryDesc
    {
        All = new[]
        {
            ComponentType.ReadOnly<Node>(),
            ComponentType.ReadOnly<ConnectedEdge>(),
        },
    });

    // Congested roads (high duration/length ratio)
    m_CongestedRoadsQuery = GetEntityQuery(new EntityQueryDesc
    {
        All = new[]
        {
            ComponentType.ReadOnly<Road>(),
            ComponentType.ReadOnly<Curve>(),
        },
    });
}
```

**Core Analysis Jobs**:

```csharp
[BurstCompile]
private struct FindHighDensityConnectionsJob : IJobChunk
{
    // Input
    [ReadOnly] public ComponentTypeHandle<Density> m_DensityType;
    [ReadOnly] public ComponentTypeHandle<Curve> m_CurveType;
    [ReadOnly] public BufferTypeHandle<ConnectedBuilding> m_BuildingType;
    [ReadOnly] public ComponentLookup<Building> m_BuildingLookup;
    [ReadOnly] public ComponentLookup<Block> m_BlockLookup;

    // Output
    public NativeList<SuggestionCandidate>.ParallelWriter m_Candidates;

    public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex,
                        bool useEnabledMask, in v128 chunkEnabledMask)
    {
        var densities = chunk.GetNativeArray(ref m_DensityType);
        var curves = chunk.GetNativeArray(ref m_CurveType);
        var buildingBuffers = chunk.GetBufferAccessor(ref m_BuildingType);

        for (int i = 0; i < chunk.Count; i++)
        {
            float density = densities[i].m_Density;

            // High density threshold
            if (density > 10.0f)
            {
                // Analyze connected buildings
                var buildings = buildingBuffers[i];

                // Check if adjacent blocks need connection
                // (Implementation details...)

                // Create candidate if justified
                m_Candidates.AddNoResize(new SuggestionCandidate
                {
                    m_FromPosition = /* calculated */,
                    m_ToPosition = /* calculated */,
                    m_Reason = SuggestionReason.ConnectHighDensityBlocks,
                    m_RawScore = density / 20.0f,  // Normalize
                });
            }
        }
    }
}
```

**Algorithm: Find Block Connections**

```csharp
private void AnalyzeBlockConnections(
    NativeList<SuggestionCandidate> candidates,
    ComponentLookup<Block> blockLookup,
    ComponentLookup<Node> nodeLookup)
{
    // For each developed block
    foreach (var blockEntity in m_DevelopedBlocksQuery.ToEntityArray(Allocator.Temp))
    {
        Block block = blockLookup[blockEntity];

        // Get block corners
        Quad2 corners = ZoneUtils.CalculateCorners(block);

        // Check each edge of block
        for (int edgeIndex = 0; edgeIndex < 4; edgeIndex++)
        {
            float2 edgeStart = corners[edgeIndex];
            float2 edgeEnd = corners[(edgeIndex + 1) % 4];
            float2 edgeMid = (edgeStart + edgeEnd) * 0.5f;

            // Find if there's a road along this edge
            bool hasRoad = HasRoadNearEdge(edgeMid,
                                           block.m_Direction,
                                           nodeLookup);

            if (!hasRoad)
            {
                // Suggest connecting this edge to nearest road
                // or adjacent block
                candidates.Add(new SuggestionCandidate
                {
                    m_FromPosition = new float3(edgeStart.x, 0, edgeStart.y),
                    m_ToPosition = new float3(edgeEnd.x, 0, edgeEnd.y),
                    m_Reason = SuggestionReason.CompleteGrid,
                    m_RawScore = 0.6f,
                });
            }
        }
    }
}
```

---

### Phase 2: Generation System

**File**: `RoadSuggestionGenerationSystem.cs`

**Update Dependency**: After OrganicRoadAnalysisSystem

**Responsibilities**:
1. Process SuggestionCandidate entities
2. Validate placement (terrain, existing roads, obstacles)
3. Snap to existing nodes
4. Calculate final priority scores
5. Create RoadSuggestion entities

**Validation Pipeline**:

```csharp
private bool ValidateSuggestion(
    in SuggestionCandidate candidate,
    out RoadSuggestion suggestion)
{
    suggestion = default;

    // Step 1: Terrain height validation
    float3 startPos = candidate.m_FromPosition;
    float3 endPos = candidate.m_ToPosition;

    startPos.y = TerrainUtils.SampleHeight(startPos.xz);
    endPos.y = TerrainUtils.SampleHeight(endPos.xz);

    // Check slope
    float distance = math.distance(startPos, endPos);
    float heightDiff = math.abs(startPos.y - endPos.y);
    float slope = heightDiff / distance;

    if (slope > 0.2f)  // Too steep
        return false;

    // Step 2: Snap to existing nodes
    Entity startNode = TrySnapToNode(startPos, SNAP_DISTANCE);
    Entity endNode = TrySnapToNode(endPos, SNAP_DISTANCE);

    if (startNode != Entity.Null)
        startPos = m_NodeLookup[startNode].m_Position;
    if (endNode != Entity.Null)
        endPos = m_NodeLookup[endNode].m_Position;

    // Step 3: Check for obstacles
    if (IntersectsBuilding(startPos, endPos))
        return false;

    // Step 4: Check minimum distance
    if (distance < 20.0f || distance > 500.0f)
        return false;

    // Step 5: Create suggestion
    suggestion = new RoadSuggestion
    {
        m_StartNode = startNode,
        m_EndNode = endNode,
        m_StartPosition = startPos,
        m_EndPosition = endPos,
        m_Curve = NetUtils.StraightCurve(startPos, endPos),
        m_RoadPrefab = GetDefaultRoadPrefab(),
        m_Priority = CalculatePriority(candidate),
        m_Reason = candidate.m_Reason,
        m_Flags = SuggestionFlags.Validated,
    };

    return true;
}
```

**Priority Calculation**:

```csharp
private float CalculatePriority(in SuggestionCandidate candidate)
{
    // Base score from analysis
    float priority = candidate.m_RawScore;

    // Multiply by reason weight
    float reasonWeight = candidate.m_Reason switch
    {
        SuggestionReason.ReduceCongestion => 1.5f,
        SuggestionReason.ConnectHighDensityBlocks => 1.3f,
        SuggestionReason.DeadEndElimination => 1.1f,
        SuggestionReason.ImproveConnectivity => 1.0f,
        SuggestionReason.CompleteGrid => 0.8f,
        SuggestionReason.ParallelRoute => 0.7f,
        _ => 1.0f,
    };

    priority *= reasonWeight;

    // Clamp to 0-1
    return math.clamp(priority, 0.0f, 1.0f);
}
```

---

### Phase 3: Visualization System

**File**: `RoadSuggestionVisualizationSystem.cs`

**Update Frequency**: Every frame (for smooth rendering)

**Responsibilities**:
1. Render suggestion overlays
2. Color-code by priority
3. Show reason icons
4. Handle player interaction

**Rendering Implementation**:

```csharp
[BurstCompile]
private struct VisualizeSuggestionsJob : IJobChunk
{
    [ReadOnly] public ComponentTypeHandle<RoadSuggestion> m_SuggestionType;
    [ReadOnly] public ComponentTypeHandle<SuggestionMetrics> m_MetricsType;

    public GizmoBatcher m_GizmoBatcher;

    public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex,
                        bool useEnabledMask, in v128 chunkEnabledMask)
    {
        var suggestions = chunk.GetNativeArray(ref m_SuggestionType);
        var metrics = chunk.GetNativeArray(ref m_MetricsType);

        for (int i = 0; i < chunk.Count; i++)
        {
            var suggestion = suggestions[i];

            // Skip if not visible
            if ((suggestion.m_Flags & SuggestionFlags.Visible) == 0)
                continue;

            // Color based on priority
            Color color = GetPriorityColor(suggestion.m_Priority);

            // Draw the suggested road path
            DrawRoadSuggestion(suggestion, color);

            // Draw reason marker
            float3 midpoint = math.lerp(
                suggestion.m_StartPosition,
                suggestion.m_EndPosition,
                0.5f
            );
            DrawReasonIcon(midpoint, suggestion.m_Reason);
        }
    }

    private Color GetPriorityColor(float priority)
    {
        // Low priority: yellow
        // High priority: bright green
        return Color.Lerp(
            new Color(1.0f, 1.0f, 0.0f, 0.6f),  // Yellow
            new Color(0.0f, 1.0f, 0.0f, 0.8f),  // Green
            priority
        );
    }

    private void DrawRoadSuggestion(in RoadSuggestion suggestion, Color color)
    {
        // Draw bezier curve with multiple segments for smoothness
        const int segments = 10;
        for (int i = 0; i < segments; i++)
        {
            float t1 = (float)i / segments;
            float t2 = (float)(i + 1) / segments;

            float3 p1 = MathUtils.Position(suggestion.m_Curve, t1);
            float3 p2 = MathUtils.Position(suggestion.m_Curve, t2);

            m_GizmoBatcher.DrawLine(p1, p2, color);
        }
    }

    private void DrawReasonIcon(float3 position, SuggestionReason reason)
    {
        // Draw small sphere or icon at midpoint
        Color iconColor = reason switch
        {
            SuggestionReason.ReduceCongestion => Color.red,
            SuggestionReason.ConnectHighDensityBlocks => Color.blue,
            SuggestionReason.DeadEndElimination => Color.cyan,
            _ => Color.white,
        };

        m_GizmoBatcher.DrawWireSphere(position, 2.0f, iconColor);
    }
}
```

---

## DETAILED ALGORITHMS

### 1. Dead-End Detection

```csharp
[BurstCompile]
private struct FindDeadEndsJob : IJobChunk
{
    [ReadOnly] public EntityTypeHandle m_EntityType;
    [ReadOnly] public ComponentTypeHandle<Node> m_NodeType;
    [ReadOnly] public BufferTypeHandle<ConnectedEdge> m_EdgeType;

    public NativeList<SuggestionCandidate>.ParallelWriter m_Candidates;

    public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex,
                        bool useEnabledMask, in v128 chunkEnabledMask)
    {
        var entities = chunk.GetNativeArray(m_EntityType);
        var nodes = chunk.GetNativeArray(ref m_NodeType);
        var edgeBuffers = chunk.GetBufferAccessor(ref m_EdgeType);

        for (int i = 0; i < chunk.Count; i++)
        {
            var edges = edgeBuffers[i];

            // Dead-end = only 1 connection
            if (edges.Length == 1)
            {
                var node = nodes[i];

                // Suggest extending the road
                // Direction: opposite of existing edge
                float3 extensionDir = CalculateExtensionDirection(
                    node.m_Position,
                    edges[0].m_Edge
                );

                float3 endPos = node.m_Position + extensionDir * 100.0f;

                m_Candidates.AddNoResize(new SuggestionCandidate
                {
                    m_FromPosition = node.m_Position,
                    m_ToPosition = endPos,
                    m_Reason = SuggestionReason.DeadEndElimination,
                    m_RawScore = 0.5f,
                });
            }
        }
    }
}
```

### 2. Congestion Detection

```csharp
private void FindCongestionBottlenecks(NativeList<SuggestionCandidate> candidates)
{
    var entities = m_CongestedRoadsQuery.ToEntityArray(Allocator.Temp);

    foreach (var edgeEntity in entities)
    {
        Road road = m_RoadLookup[edgeEntity];
        Curve curve = m_CurveLookup[edgeEntity];

        // Calculate average traffic flow duration
        float avgDuration = (road.m_TrafficFlowDuration0.x +
                            road.m_TrafficFlowDuration0.y +
                            road.m_TrafficFlowDuration0.z +
                            road.m_TrafficFlowDuration0.w) / 4.0f;

        // Normalize by road length
        float congestionRatio = avgDuration / curve.m_Length;

        // High congestion threshold
        if (congestionRatio > 0.5f)
        {
            // Suggest parallel road
            Edge edge = m_EdgeLookup[edgeEntity];
            Node startNode = m_NodeLookup[edge.m_Start];
            Node endNode = m_NodeLookup[edge.m_End];

            // Offset by 30 meters perpendicular
            float3 direction = math.normalize(endNode.m_Position -
                                             startNode.m_Position);
            float3 perpendicular = new float3(-direction.z, 0, direction.x);

            float3 offsetStart = startNode.m_Position + perpendicular * 30.0f;
            float3 offsetEnd = endNode.m_Position + perpendicular * 30.0f;

            candidates.Add(new SuggestionCandidate
            {
                m_FromPosition = offsetStart,
                m_ToPosition = offsetEnd,
                m_Reason = SuggestionReason.ParallelRoute,
                m_RawScore = math.min(congestionRatio, 1.0f),
            });
        }
    }

    entities.Dispose();
}
```

### 3. Grid Completion

```csharp
private void SuggestGridCompletions(
    NativeList<SuggestionCandidate> candidates,
    ComponentLookup<Block> blockLookup)
{
    var blocks = m_DevelopedBlocksQuery.ToEntityArray(Allocator.Temp);

    for (int i = 0; i < blocks.Length; i++)
    {
        Block blockA = blockLookup[blocks[i]];

        // Check adjacent blocks
        for (int j = i + 1; j < blocks.Length; j++)
        {
            Block blockB = blockLookup[blocks[j]];

            // Are they adjacent and aligned?
            if (AreBlocksAligned(blockA, blockB))
            {
                float distance = math.distance(
                    blockA.m_Position,
                    blockB.m_Position
                );

                // Close enough to connect?
                if (distance < 150.0f)
                {
                    // Check if already connected by road
                    if (!AreBlocksConnectedByRoad(blockA, blockB))
                    {
                        // Suggest connection
                        float3 startPos = GetBlockEdgePosition(blockA, blockB);
                        float3 endPos = GetBlockEdgePosition(blockB, blockA);

                        candidates.Add(new SuggestionCandidate
                        {
                            m_FromPosition = startPos,
                            m_ToPosition = endPos,
                            m_Reason = SuggestionReason.CompleteGrid,
                            m_RawScore = 0.7f,
                        });
                    }
                }
            }
        }
    }

    blocks.Dispose();
}

private bool AreBlocksAligned(in Block a, in Block b)
{
    // Check if block directions are parallel or perpendicular
    float dot = math.abs(math.dot(a.m_Direction, b.m_Direction));
    return dot > 0.9f || dot < 0.1f;  // Parallel or perpendicular
}
```

---

## PLAYER INTERACTION

### Integration with NetToolSystem

**Option 1: Passive Display**
- Suggestions shown as overlays
- Player manually uses NetToolSystem to build
- Suggestions guide placement but don't auto-build

**Option 2: Active Tool Integration**
```csharp
public class OrganicRoadToolSystem : ToolBaseSystem
{
    private EntityQuery m_SuggestionQuery;

    protected override void OnUpdate()
    {
        // Handle input
        if (Input.GetMouseButtonDown(0))
        {
            // Raycast to see if player clicked a suggestion
            RaycastHit hit = GetRaycastHit();

            Entity suggestion = FindNearestSuggestion(hit.m_HitPosition);

            if (suggestion != Entity.Null)
            {
                // Apply the suggestion
                ApplySuggestion(suggestion);
            }
        }
    }

    private void ApplySuggestion(Entity suggestionEntity)
    {
        RoadSuggestion suggestion =
            EntityManager.GetComponentData<RoadSuggestion>(suggestionEntity);

        // Create road using NetToolSystem patterns
        CreateRoadFromSuggestion(suggestion);

        // Mark as applied
        suggestion.m_Flags |= SuggestionFlags.Applied;
        EntityManager.SetComponentData(suggestionEntity, suggestion);
    }
}
```

---

## PERFORMANCE OPTIMIZATION

### 1. Throttling and Intervals

```csharp
public class OrganicRoadAnalysisSystem : GameSystemBase
{
    private int m_UpdateCounter;
    private const int UPDATE_INTERVAL = 60;  // Every 60 frames
    private const int MAX_SUGGESTIONS = 20;   // Limit active suggestions

    protected override void OnUpdate()
    {
        m_UpdateCounter++;

        if (m_UpdateCounter < UPDATE_INTERVAL)
            return;

        m_UpdateCounter = 0;

        // Perform heavy analysis
        RunAnalysisJobs();
    }
}
```

### 2. Spatial Partitioning

```csharp
// Use spatial hashing to quickly find nearby entities
private NativeMultiHashMap<int, Entity> m_SpatialHash;

private int GetSpatialKey(float3 position, float cellSize = 100.0f)
{
    int x = (int)(position.x / cellSize);
    int z = (int)(position.z / cellSize);
    return (x << 16) | (z & 0xFFFF);
}

private void BuildSpatialHash()
{
    m_SpatialHash.Clear();

    // Hash all nodes
    foreach (var nodeEntity in m_NodeQuery)
    {
        Node node = m_NodeLookup[nodeEntity];
        int key = GetSpatialKey(node.m_Position);
        m_SpatialHash.Add(key, nodeEntity);
    }
}
```

### 3. Burst Compilation

```csharp
// Mark all jobs with [BurstCompile] for maximum performance
[BurstCompile]
private struct AnalysisJob : IJobChunk
{
    // All fields must be Burst-compatible
    // No managed objects
    // Use NativeContainers only
}
```

---

## FILE STRUCTURE

```
/OrganicRoadSuggestionMod/
├── Components/
│   ├── SuggestionCandidate.cs
│   ├── RoadSuggestion.cs
│   └── SuggestionMetrics.cs
│
├── Systems/
│   ├── OrganicRoadAnalysisSystem.cs
│   ├── RoadSuggestionGenerationSystem.cs
│   ├── RoadSuggestionVisualizationSystem.cs
│   └── OrganicRoadToolSystem.cs (optional)
│
├── Jobs/
│   ├── FindHighDensityConnectionsJob.cs
│   ├── FindDeadEndsJob.cs
│   ├── FindCongestionBottlenecksJob.cs
│   ├── ValidateSuggestionsJob.cs
│   └── VisualizeSuggestionsJob.cs
│
├── Utils/
│   ├── SuggestionUtils.cs
│   ├── NetworkAnalysisUtils.cs
│   └── TerrainValidationUtils.cs
│
└── ModMain.cs
```

---

## IMPLEMENTATION ROADMAP

### Week 1: Core Components and Analysis
- [ ] Define all component structs
- [ ] Implement OrganicRoadAnalysisSystem
- [ ] Write dead-end detection job
- [ ] Write high-density connection job
- [ ] Test with debug logging

### Week 2: Generation and Validation
- [ ] Implement RoadSuggestionGenerationSystem
- [ ] Write terrain validation logic
- [ ] Write node snapping logic
- [ ] Write priority calculation
- [ ] Test suggestion quality

### Week 3: Visualization
- [ ] Implement RoadSuggestionVisualizationSystem
- [ ] Setup GizmoBatcher rendering
- [ ] Implement color coding
- [ ] Add reason icons
- [ ] Test visual clarity

### Week 4: Integration and Polish
- [ ] Integrate with NetToolSystem (if active tool)
- [ ] Add player settings (max suggestions, priorities)
- [ ] Optimize performance with Burst
- [ ] Add spatial partitioning
- [ ] Final testing and debugging

---

## TESTING STRATEGY

### Unit Tests
1. **Component Creation**: Verify SuggestionCandidate/RoadSuggestion creation
2. **Scoring**: Test priority calculation with known inputs
3. **Validation**: Test terrain slope, obstacle detection
4. **Grid Alignment**: Test block alignment detection

### Integration Tests
1. **Analysis Loop**: Run analysis on test city, verify candidates
2. **Generation Loop**: Verify suggestions are valid and logical
3. **Visualization**: Verify gizmos render correctly
4. **Performance**: Measure frame time impact

### Gameplay Tests
1. Build a small city with dead ends → verify suggestions appear
2. Create high-density block → verify connection suggestions
3. Create traffic jam → verify parallel route suggested
4. Test player interaction (if tool system implemented)

---

## CONFIGURATION OPTIONS

### Suggested Settings UI
```csharp
public struct OrganicRoadSettings : IComponentData
{
    public bool m_Enabled;                    // Master toggle
    public int m_MaxActiveSuggestions;        // Limit displayed
    public float m_MinPriority;               // Filter low-priority
    public bool m_ShowDeadEndSuggestions;     // Toggle reason types
    public bool m_ShowCongestionSuggestions;
    public bool m_ShowGridSuggestions;
    public float m_UpdateInterval;            // Analysis frequency
}
```

---

## EXPECTED OUTCOMES

### Player Experience
- **Organic Growth**: Roads suggested follow natural city development
- **Traffic Relief**: System detects and suggests solutions to congestion
- **Grid Completion**: Aesthetic, organized city layouts
- **Learning Tool**: New players learn good road placement

### Performance
- **Minimal Impact**: Analysis runs every 60 frames, Burst-compiled
- **Scalable**: Spatial partitioning handles large cities
- **Configurable**: Players can adjust suggestion frequency/count

### Metrics to Track
- Number of suggestions generated per minute
- Player acceptance rate (applied vs rejected)
- Traffic flow improvement after applying suggestions
- Frame time impact (target: <0.5ms)

---

## NEXT STEPS

1. **Set up project structure** - Create mod project with proper references
2. **Implement components** - Define all data structures
3. **Start with analysis system** - Get basic candidate generation working
4. **Iterate on algorithms** - Tune thresholds and scoring
5. **Add visualization** - See suggestions in-game
6. **Polish and optimize** - Burst compilation, spatial queries
7. **Player testing** - Get feedback on suggestion quality

---

## REFERENCES

- **Analysis Document**: `CITIES_SKYLINES_2_ROAD_ANALYSIS.md`
- **ECS Reference**: `ecs-reference.md`
- **Systems Guide**: `systems.md`
- **Tool Guide**: `tool.md`
- **Source Code**: `/New folder/` (all namespaces)

---

**Status**: Ready for implementation
**Complexity**: Medium-High (requires ECS knowledge, spatial algorithms)
**Estimated Development Time**: 4 weeks
**Dependencies**: Cities: Skylines II modding SDK, Unity ECS
