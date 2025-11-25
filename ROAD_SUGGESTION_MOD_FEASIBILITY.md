# Road Layout Suggestion Mod - Feasibility Analysis

## Executive Summary

Creating an intelligent **road layout suggestion mod** for Cities: Skylines II is **moderately challenging but highly feasible**. Based on the codebase analysis, the game provides excellent modding infrastructure and access to all necessary terrain, network, and pathfinding systems.

**Difficulty Rating:** ⭐⭐⭐☆☆ (3/5 - Moderate)

**Key Finding:** The game's ECS architecture and comprehensive API access make this very doable, but you'll need to implement custom algorithms for intelligent road suggestions.

---

## What You Want to Build

An intelligent system that:
- Analyzes terrain (slopes, water, obstacles)
- Suggests organic, realistic road layouts
- Adapts to topology (follows contour lines, avoids steep slopes)
- Considers city planning principles (arterials, collectors, local roads)
- Provides visual previews of suggested routes
- Allows easy acceptance/rejection of suggestions

**NOT** a simple grid tool - this needs AI/algorithmic intelligence.

---

## Technical Feasibility Breakdown

### ✅ **EASY Components** (Game Already Provides)

#### 1. **Modding Framework**
**Difficulty:** ⭐☆☆☆☆

```csharp
// The mod interface is extremely simple
public interface IMod
{
    void OnLoad(UpdateSystem updateSystem);
    void OnDispose();
}
```

**What you get:**
- Simple `IMod` interface
- Access to entire `UpdateSystem`
- Can register custom systems
- Full ECS access

**Example Mod Structure:**
```csharp
public class RoadSuggestionMod : IMod
{
    public void OnLoad(UpdateSystem updateSystem)
    {
        // Register your custom systems
        updateSystem.UpdateAt<RoadSuggestionSystem>(SystemUpdatePhase.ToolUpdate);
    }

    public void OnDispose()
    {
        // Cleanup
    }
}
```

#### 2. **Terrain Data Access**
**Difficulty:** ⭐☆☆☆☆

The game provides `TerrainHeightData` with everything you need:

```csharp
public struct TerrainHeightData
{
    public NativeArray<ushort> heights;           // Full resolution heightmap
    public NativeArray<ushort> downscaledHeights; // Lower resolution for fast queries
    public int3 resolution;                        // Grid resolution
    public float3 scale;                          // World space scale
    public float3 offset;                         // World offset
    public bool hasBackdrop;                      // Map edge detection
}
```

**What you can do:**
- Query height at any position
- Calculate slopes
- Detect water bodies
- Find flat areas
- Analyze terrain features

**Access Pattern:**
```csharp
TerrainSystem terrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();
TerrainHeightData heightData = terrainSystem.GetHeightData();
// Now you can query heights, calculate slopes, etc.
```

#### 3. **Network/Road Infrastructure**
**Difficulty:** ⭐☆☆☆☆

The network system is well-exposed:

**Core Components:**
```csharp
// Node: Intersection or endpoint
public struct Node : IComponentData
{
    public float3 m_Position;
    public quaternion m_Rotation;
}

// Edge: Road segment between nodes
public struct Edge : IComponentData
{
    public Entity m_Start;  // Start node
    public Entity m_End;    // End node
}

// Road data from prefabs
public struct RoadData : IComponentData
{
    public Entity m_ZoneBlockPrefab;
    public float m_SpeedLimit;
    public RoadFlags m_Flags;
}
```

**Available Systems:**
- `NetToolSystem` - Create roads programmatically
- `GenerateNodesSystem` - Node generation
- `GenerateEdgesSystem` - Edge generation
- Access to all road prefabs

#### 4. **Tool System Integration**
**Difficulty:** ⭐⭐☆☆☆

You can extend `ToolBaseSystem`:

```csharp
public abstract class ToolBaseSystem : GameSystemBase
{
    public abstract string toolID { get; }

    protected ToolSystem m_ToolSystem;
    protected PrefabSystem m_PrefabSystem;
    protected ToolRaycastSystem m_ToolRaycastSystem;

    // Your tool logic goes here
}
```

**Built-in Tool Features:**
- Raycasting
- Snapping to existing networks
- Visual feedback (temp entities)
- Input handling
- Error/warning system

#### 5. **Existing Building Detection**
**Difficulty:** ⭐☆☆☆☆

Use ECS queries to find existing structures:

```csharp
// Query all buildings
EntityQuery buildingQuery = GetEntityQuery(
    ComponentType.ReadOnly<Building>(),
    ComponentType.ReadOnly<Transform>()
);

// Query all existing roads
EntityQuery roadQuery = GetEntityQuery(
    ComponentType.ReadOnly<Edge>(),
    ComponentType.ReadOnly<Node>()
);
```

---

### ⚠️ **MODERATE Components** (You Need to Implement)

#### 1. **Road Layout Algorithm**
**Difficulty:** ⭐⭐⭐☆☆

This is the core challenge. You'll need to implement intelligent algorithms.

**Suggested Approaches:**

**A. Contour-Based Layout**
```csharp
// Follow contour lines for natural-looking roads
public class ContourRoadGenerator
{
    public List<RoadSegment> GenerateContourRoads(
        TerrainHeightData terrain,
        float3 startPoint,
        float maxSlope = 0.1f // 10% grade
    )
    {
        // 1. Sample terrain heights in a radius
        // 2. Find lines of equal elevation
        // 3. Create roads that follow these lines
        // 4. Connect different elevation levels with switchbacks
    }
}
```

**B. Gradient Descent for Natural Paths**
```csharp
// Roads prefer flat terrain and avoid obstacles
public class NaturalPathfinder
{
    public List<float3> FindOptimalPath(
        float3 start,
        float3 end,
        TerrainHeightData terrain,
        List<Obstacle> obstacles
    )
    {
        // Cost function considering:
        // - Terrain slope (higher cost for steep)
        // - Distance to obstacles
        // - Road length
        // - Existing road connections

        // Use A* or Dijkstra with custom cost function
    }
}
```

**C. Urban Planning Patterns**
```csharp
public enum RoadType
{
    Arterial,    // Main roads, wider, connect districts
    Collector,   // Medium roads, collect from local
    Local        // Small roads, local access
}

public class HierarchicalRoadPlanner
{
    // Create realistic urban layouts
    // 1. Major arterials first (connect zones)
    // 2. Collectors branch off
    // 3. Local roads fill in
}
```

**Complexity Factors:**
- Need to implement custom pathfinding with terrain awareness
- Must consider existing infrastructure
- Should optimize for realistic patterns
- Need to handle edge cases (water, cliffs, existing buildings)

**Time Estimate:** 40-60 hours for good algorithm

#### 2. **Visual Preview System**
**Difficulty:** ⭐⭐⭐☆☆

Show suggested roads before player commits.

**Implementation:**
```csharp
public class RoadPreviewSystem : GameSystemBase
{
    private List<PreviewRoad> m_SuggestedRoads;

    protected override void OnUpdate()
    {
        // Create temporary entities for visualization
        foreach (var road in m_SuggestedRoads)
        {
            // Create temp entity with visual components
            Entity previewEntity = EntityManager.CreateEntity();
            EntityManager.AddComponent<Temp>(previewEntity);
            EntityManager.AddComponent<Updated>(previewEntity);

            // Add rendering components
            // Player can see ghosted roads
        }
    }
}
```

**Challenges:**
- Need to create temporary entities visible in game
- Should show different colors for different road types
- Must handle UI interaction (hover, click to accept)

**Time Estimate:** 20-30 hours

#### 3. **UI Integration**
**Difficulty:** ⭐⭐☆☆☆

The game uses **cohtml.Net** (HTML/CSS/JS UI).

**You'll need:**
```javascript
// JavaScript UI binding
engine.on('roadSuggestion.generated', (roads) => {
    // Display suggestions in UI panel
    // Show accept/reject buttons
    // Display statistics (length, cost, etc.)
});
```

```csharp
// C# side
[Binding]
public void GenerateSuggestion(float3 startPoint)
{
    var suggestions = m_Algorithm.Generate(startPoint);
    // Send to UI
    EventBus.Trigger("roadSuggestion.generated", suggestions);
}
```

**Time Estimate:** 15-25 hours (if new to cohtml)

---

### 🔴 **CHALLENGING Components** (Complex but Doable)

#### 1. **Real-Time Performance**
**Difficulty:** ⭐⭐⭐⭐☆

**Challenge:** Must be fast enough for interactive use.

**Solutions:**

**A. Use Job System for Parallelization**
```csharp
[BurstCompile]
public struct TerrainAnalysisJob : IJob
{
    [ReadOnly]
    public TerrainHeightData heightData;

    public NativeArray<SlopeData> results;

    public void Execute()
    {
        // Parallel terrain analysis
        // Burst-compiled for speed
    }
}

// Schedule job
var job = new TerrainAnalysisJob { ... };
var handle = job.Schedule();
handle.Complete();
```

**B. Spatial Partitioning**
```csharp
// Don't analyze entire map every frame
// Use quadtree or grid for fast queries
public class SpatialPartitionedTerrain
{
    private QuadTree<TerrainChunk> m_Chunks;

    public List<TerrainChunk> GetRelevantChunks(Bounds3 area)
    {
        // Only process nearby terrain
        return m_Chunks.Query(area);
    }
}
```

**C. Caching and Incremental Updates**
```csharp
public class TerrainCache
{
    private Dictionary<int2, CachedTerrainData> m_Cache;

    public CachedTerrainData GetOrCompute(int2 gridPos)
    {
        if (!m_Cache.ContainsKey(gridPos))
        {
            m_Cache[gridPos] = AnalyzeTerrain(gridPos);
        }
        return m_Cache[gridPos];
    }
}
```

**Performance Targets:**
- Analysis: < 100ms for initial scan
- Real-time updates: < 16ms (60 FPS)
- Use Burst compilation for hot paths

**Time Estimate:** 30-40 hours for optimization

#### 2. **Integration with Existing Tools**
**Difficulty:** ⭐⭐⭐☆☆

Need to work alongside existing road tool.

**Approach:**
```csharp
public class RoadSuggestionTool : ToolBaseSystem
{
    private NetToolSystem m_NetTool;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_NetTool = World.GetOrCreateSystemManaged<NetToolSystem>();
    }

    public void ApplySuggestion(RoadSuggestion suggestion)
    {
        // Use NetToolSystem to actually place roads
        foreach (var segment in suggestion.segments)
        {
            m_NetTool.CreateRoadSegment(segment.start, segment.end, segment.prefab);
        }
    }
}
```

**Time Estimate:** 20-30 hours

---

## Complete Implementation Plan

### Phase 1: Foundation (Week 1-2)
**Time:** 40-60 hours

1. **Setup Mod Infrastructure** (5-10h)
   - Create mod project
   - Implement `IMod` interface
   - Setup build pipeline
   - Test basic mod loading

2. **Terrain Analysis System** (15-20h)
   - Access `TerrainHeightData`
   - Calculate slopes
   - Detect flat areas
   - Identify obstacles (water, steep slopes)
   - Create spatial index

3. **Basic Road Placement** (10-15h)
   - Study `NetToolSystem`
   - Create helper functions to place roads
   - Test programmatic road creation

4. **ECS System Setup** (10-15h)
   - Create custom ECS systems
   - Setup update phases
   - Implement data structures

### Phase 2: Core Algorithm (Week 3-5)
**Time:** 80-120 hours

1. **Pathfinding with Terrain Cost** (30-40h)
   - Implement A* with custom cost function
   - Terrain slope penalties
   - Obstacle avoidance
   - Existing road connection bonuses

2. **Layout Generation Algorithms** (30-50h)
   - **Option A:** Contour-following roads
   - **Option B:** Organic sprawl pattern
   - **Option C:** Hierarchical urban planning
   - Implement at least 2 different suggestion modes

3. **Multi-Point Planning** (20-30h)
   - Connect multiple districts
   - Create network hierarchies (arterial, collector, local)
   - Balance road types

### Phase 3: Visualization & UX (Week 6-7)
**Time:** 50-70 hours

1. **Preview System** (20-30h)
   - Create temporary entities for previews
   - Color coding for road types
   - Transparency/ghosting effects

2. **UI Integration** (20-30h)
   - Learn cohtml.Net
   - Create suggestion panel
   - Accept/reject buttons
   - Settings panel (algorithm parameters)

3. **Input Handling** (10-10h)
   - Mouse interaction
   - Keyboard shortcuts
   - Tool activation/deactivation

### Phase 4: Polish & Optimization (Week 8-9)
**Time:** 40-60 hours

1. **Performance Optimization** (20-30h)
   - Profile with Unity Profiler
   - Burst compile hot paths
   - Implement caching
   - Add LOD for preview

2. **Edge Cases & Bug Fixes** (10-15h)
   - Handle map edges
   - Deal with existing infrastructure conflicts
   - Fix visual glitches

3. **User Experience** (10-15h)
   - Tooltips and help text
   - Better visual feedback
   - Undo/redo support

### Phase 5: Testing & Release (Week 10)
**Time:** 20-30 hours

1. **Testing** (10-15h)
   - Test on different maps
   - Test with different terrain types
   - User testing

2. **Documentation** (5-10h)
   - User manual
   - Code documentation
   - Tutorial video

3. **Release** (5-5h)
   - Package mod
   - Upload to Steam Workshop / PDX Mods
   - Community support

---

## Total Effort Estimate

### Realistic Timeline

**Skill Level: Experienced C# Developer**
- **Minimum:** 230 hours (~6 weeks full-time, or 3 months part-time)
- **Realistic:** 320 hours (~8 weeks full-time, or 4-5 months part-time)
- **With Learning:** 400+ hours (if learning ECS, Unity, and game dev concepts)

**Skill Level: Intermediate Developer**
- **Realistic:** 400-500 hours (~5-6 months part-time)

---

## Technical Requirements

### Skills Needed

#### Essential ⭐⭐⭐⭐⭐
- **C# Programming** - Advanced level
- **Unity ECS** - Can learn from docs
- **Algorithms & Data Structures** - Pathfinding, graphs
- **3D Math** - Vector math, quaternions

#### Important ⭐⭐⭐⭐☆
- **Unity Engine** - Basic understanding
- **Job System & Burst** - For performance
- **Spatial Algorithms** - Quadtrees, spatial queries

#### Helpful ⭐⭐⭐☆☆
- **HTML/CSS/JavaScript** - For UI (can be minimal)
- **Urban Planning** - For realistic layouts
- **Game Modding Experience** - Helpful but not required

### Tools & Resources

```bash
# Development Environment
- Visual Studio 2022 or JetBrains Rider
- Cities: Skylines II (obviously)
- Unity Editor (for testing, optional)
- Git for version control

# Libraries (provided by game)
- Unity.Entities
- Unity.Mathematics
- Unity.Collections
- Unity.Burst
```

---

## Key Technical Challenges & Solutions

### Challenge 1: Algorithm Complexity
**Problem:** Creating truly intelligent suggestions is algorithmically complex.

**Solutions:**
1. **Start Simple:** Begin with straight-line A* pathfinding with terrain cost
2. **Iterate:** Add features incrementally (contours, hierarchies, etc.)
3. **Learn from Real Cities:** Analyze real city layouts for patterns
4. **User Feedback:** Let players guide improvements

### Challenge 2: Performance
**Problem:** Analyzing terrain and generating suggestions in real-time.

**Solutions:**
1. **Burst Compilation:** Use `[BurstCompile]` on hot code paths
2. **Job System:** Parallelize terrain analysis
3. **Caching:** Pre-compute expensive operations
4. **Progressive Generation:** Generate suggestions over multiple frames
5. **Spatial Partitioning:** Only analyze relevant areas

### Challenge 3: Integration
**Problem:** Working with existing game systems without conflicts.

**Solutions:**
1. **Use Existing APIs:** Leverage `NetToolSystem` instead of duplicating
2. **Respect Game State:** Check for conflicts before suggesting
3. **Follow Game Patterns:** Use same update phases as built-in tools
4. **Testing:** Extensive testing with other mods

### Challenge 4: User Experience
**Problem:** Making suggestions feel natural and useful, not annoying.

**Solutions:**
1. **Non-Intrusive:** Only show when explicitly requested
2. **Quick to Dismiss:** Easy reject with ESC or click
3. **Visual Clarity:** Clear preview with color coding
4. **Smart Defaults:** Good suggestions out of the box
5. **Customization:** Let users tune algorithm parameters

---

## Comparison with Other Mod Types

### Difficulty Comparison

| Mod Type | Difficulty | Time Estimate |
|----------|-----------|---------------|
| **Simple Asset Pack** | ⭐☆☆☆☆ | 20-40h |
| **UI Skin/Theme** | ⭐⭐☆☆☆ | 40-80h |
| **Building Statistics** | ⭐⭐⭐☆☆ | 80-120h |
| **Road Suggestion (This)** | ⭐⭐⭐☆☆ | 230-400h |
| **Traffic Simulator Overhaul** | ⭐⭐⭐⭐☆ | 400-800h |
| **New Transportation Type** | ⭐⭐⭐⭐⭐ | 800-1200h |

**Your mod is MODERATE difficulty** - harder than simple content mods, easier than core system overhauls.

---

## Recommended Approach: MVP Strategy

### Version 0.1 - Minimal Viable Product (60-80 hours)
**Goal:** Prove the concept works

Features:
- ✅ Single point-to-point suggestion
- ✅ Basic terrain awareness (avoid steep slopes)
- ✅ Simple visual preview
- ✅ Manual accept/reject
- ❌ No UI panel (use console commands)
- ❌ No advanced algorithms

**Algorithm:** Simple A* with terrain cost

### Version 0.5 - Usable (150-200 hours)
**Goal:** Something people will actually use

Additional Features:
- ✅ Multiple suggestion modes
- ✅ UI panel with buttons
- ✅ Better visual feedback
- ✅ Works with existing roads
- ✅ Basic performance optimization

**Algorithm:** A* + contour following

### Version 1.0 - Full Release (300-400 hours)
**Goal:** Polished, feature-complete mod

Additional Features:
- ✅ Hierarchical road planning
- ✅ Multiple layout styles (organic, grid-hybrid, etc.)
- ✅ Advanced settings panel
- ✅ Fully optimized
- ✅ Comprehensive documentation
- ✅ Tutorial/examples

**Algorithm:** Multiple algorithms with user choice

---

## Recommended Starting Point

### Step 1: Proof of Concept (Weekend Project, 10-15h)

```csharp
// RoadSuggestionMod.cs
public class RoadSuggestionMod : IMod
{
    public void OnLoad(UpdateSystem updateSystem)
    {
        Debug.Log("Road Suggestion Mod Loaded!");

        // Just get terrain data and log it
        var terrainSystem = updateSystem.World
            .GetOrCreateSystemManaged<TerrainSystem>();
        var heightData = terrainSystem.GetHeightData();

        Debug.Log($"Terrain resolution: {heightData.resolution}");

        // Success! You can access terrain data.
    }

    public void OnDispose()
    {
        Debug.Log("Road Suggestion Mod Unloaded!");
    }
}
```

**This proves:**
1. ✅ Mod loading works
2. ✅ You can access game systems
3. ✅ Terrain data is accessible

### Step 2: Place a Road Programmatically (20-30h)

```csharp
public class SimpleRoadPlacer : GameSystemBase
{
    private NetToolSystem m_NetTool;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_NetTool = World.GetOrCreateSystemManaged<NetToolSystem>();
    }

    protected override void OnUpdate()
    {
        // On button press, create a simple straight road
        if (Input.GetKeyDown(KeyCode.R))
        {
            CreateStraightRoad(
                new float3(0, 0, 0),
                new float3(100, 0, 0)
            );
        }
    }

    private void CreateStraightRoad(float3 start, float3 end)
    {
        // Implementation here - study NetToolSystem
    }
}
```

**This proves:**
1. ✅ You can create roads via code
2. ✅ Input handling works
3. ✅ Basic mod functionality

### Step 3: Add Terrain Analysis (30-40h)

Implement slope calculation, obstacle detection, etc.

### Step 4: Implement Basic A* (40-60h)

Create pathfinding with terrain cost.

**At this point (100-145h total), you have a working mod!**

---

## Resources & Learning Path

### Official Documentation
1. **Cities: Skylines II Modding Docs**
   - PDX Mods website
   - Official mod examples

2. **Unity ECS Documentation**
   - DOTS manual
   - Job system docs
   - Burst compiler guide

### Code Examples to Study

**From the codebase you already have:**
```bash
# Study these files:
New folder/Game.Tools/NetToolSystem.cs         # How roads are placed
New folder/Game.Simulation/TerrainSystem.cs    # Terrain access
New folder/Game.Pathfind/PathfindSetupSystem.cs # Pathfinding patterns
New folder/Game.Net/                            # Network structures
```

### External Resources
1. **Algorithms**
   - Red Blob Games (pathfinding tutorials)
   - Sebastian Lague YouTube (A* in Unity)

2. **Urban Planning**
   - City planning textbooks
   - Real city analysis (satellite maps)

3. **Unity ECS**
   - Unity DOTS samples on GitHub
   - ECS tutorial videos

---

## Conclusion

### Final Assessment: ⭐⭐⭐☆☆ FEASIBLE

**Yes, you can build this mod!**

### Key Takeaways:

#### ✅ **What Makes It Feasible:**
1. **Excellent modding infrastructure** - Simple IMod interface
2. **Full system access** - Terrain, networks, everything needed
3. **Well-structured codebase** - Easy to understand and extend
4. **ECS architecture** - Performance-friendly
5. **Active modding community** - Support available

#### ⚠️ **What Makes It Challenging:**
1. **Algorithm complexity** - Need smart suggestion logic
2. **Performance requirements** - Must be real-time
3. **UI integration** - cohtml.Net learning curve
4. **Testing scope** - Many edge cases

#### 💡 **Success Factors:**
1. **Start small** - MVP approach
2. **Iterate** - Build features incrementally
3. **Profile early** - Optimize from the start
4. **Community feedback** - Release early, improve based on use
5. **Study the code** - The decompiled code is your best resource

### Realistic Timeline:
- **MVP (usable):** 2-3 months part-time
- **Full-featured:** 4-6 months part-time
- **Polished 1.0:** 6-8 months part-time

### My Recommendation:

**GO FOR IT!** This is an excellent mod project because:
- It's challenging enough to be interesting
- It's achievable with dedicated effort
- There's likely significant user demand
- You have all the technical resources needed

Start with the weekend proof-of-concept, then decide if you want to continue. The hardest part is the algorithm, not the integration.

---

## Next Steps

1. **Study the codebase** (10h)
   - Read NetToolSystem thoroughly
   - Understand terrain access
   - Study existing tools

2. **Create proof of concept** (15h)
   - Get mod loading working
   - Access terrain data
   - Log some info

3. **Build MVP** (60-80h)
   - Basic pathfinding
   - Simple preview
   - Manual accept

4. **Iterate based on feedback** (ongoing)

**Good luck! This mod would be incredibly useful for the community.**

---

**Document Version:** 1.0
**Date:** 2025-11-24
**Based on:** Cities: Skylines II Codebase Analysis
