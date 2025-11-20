# Organic Neighborhood Tool - Implementation Roadmap
## Step-by-Step Development Plan

---

## OVERVIEW

**Goal**: Build the organic neighborhood layout tool incrementally, testing at each stage.

**Strategy**: Start simple, add complexity gradually

**Timeline**: 6 phases, ~2 weeks (can accelerate based on progress)

---

## PHASE 1: Foundation & Utilities (Days 1-2)

### Goals
- Set up project structure
- Create utility classes
- No game integration yet - pure utilities

### Tasks

#### 1.1 Project Structure
```
/OrganicNeighborhoodMod/
├── Utils/
│   ├── BurstPerlinNoise.cs          ← START HERE
│   ├── TerrainHelpers.cs
│   └── CurveUtils.cs
├── Data/
│   └── LayoutParameters.cs
├── Systems/
│   └── (empty for now)
└── ModMain.cs (if needed)
```

#### 1.2 BurstPerlinNoise.cs
**File**: `/Utils/BurstPerlinNoise.cs`

```csharp
using Unity.Mathematics;
using Unity.Burst;

namespace OrganicNeighborhood.Utils
{
    [BurstCompile]
    public static class BurstPerlinNoise
    {
        [BurstCompile]
        public static float Perlin2D(float2 position)
        {
            // Implementation from NOISE_AND_VARIATION_IMPLEMENTATION.md
        }

        [BurstCompile]
        private static float Hash2D(float2 p)
        {
            // Implementation
        }

        [BurstCompile]
        public static float3 ApplyOrganicVariation(
            float3 basePosition,
            float strength,
            float scale = 0.1f)
        {
            // Implementation
        }
    }
}
```

**Test**: Create simple unit test that samples noise

#### 1.3 TerrainHelpers.cs
**File**: `/Utils/TerrainHelpers.cs`

```csharp
using Unity.Mathematics;
using Unity.Burst;
using Game.Simulation;

namespace OrganicNeighborhood.Utils
{
    [BurstCompile]
    public static class TerrainHelpers
    {
        [BurstCompile]
        public static float3 SnapToTerrain(
            float3 position,
            ref TerrainHeightData terrainData)
        {
            float height = TerrainUtils.SampleHeight(ref terrainData, position);
            position.y = height;
            return position;
        }

        [BurstCompile]
        public static bool ValidateSlope(
            float3 start,
            float3 end,
            ref TerrainHeightData terrainData,
            float maxSlopeAngle)
        {
            // Implementation from TERRAIN_AWARE_IMPLEMENTATION.md
        }
    }
}
```

#### 1.4 LayoutParameters.cs
**File**: `/Data/LayoutParameters.cs`

```csharp
using Unity.Entities;
using Unity.Mathematics;

namespace OrganicNeighborhood.Data
{
    public struct LayoutParameters
    {
        // Road spacing
        public float m_RoadSpacing;

        // Variation
        public float m_PositionVariation;
        public float m_CurveAmount;

        // Terrain
        public bool m_SnapToTerrain;
        public bool m_ValidateSlope;
        public float m_MaxSlope;
    }
}
```

**Deliverable**: Three utility files, compile successfully

---

## PHASE 2: Basic Tool System (Days 3-4)

### Goals
- Create minimal tool system
- Implement 3-point area definition
- No road generation yet - just area selection

### Tasks

#### 2.1 OrganicNeighborhoodToolSystem.cs
**File**: `/Systems/OrganicNeighborhoodToolSystem.cs`

```csharp
using Game.Tools;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Game.Simulation;

namespace OrganicNeighborhood.Systems
{
    public partial class OrganicNeighborhoodToolSystem : ToolBaseSystem
    {
        // Dependencies
        private TerrainSystem m_TerrainSystem;

        // State
        private NativeList<ControlPoint> m_ControlPoints;
        private bool m_PreviewActive;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_TerrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();
            m_ControlPoints = new NativeList<ControlPoint>(
                3, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            m_ControlPoints.Dispose();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            // Phase 2: Just handle input
            // Phase 3: Add generation
        }

        private void HandleInput()
        {
            // Get mouse click
            // Add to m_ControlPoints
            // When 3 points, trigger generation
        }
    }
}
```

#### 2.2 Input Handling
Implement:
- Mouse click detection
- Control point creation
- 3-point area definition (like grid tool)
- Debug visualization of control points

**Test**: Click 3 points, see debug spheres at each point

**Deliverable**: Tool that accepts 3-point input

---

## PHASE 3: Grid Generation (Days 5-7)

### Goals
- Generate organic grid layout
- No terrain awareness yet
- Focus on Perlin variation

### Tasks

#### 3.1 GenerateOrganicGridJob
**File**: `/Systems/GenerateOrganicGridJob.cs`

```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using OrganicNeighborhood.Utils;
using OrganicNeighborhood.Data;

namespace OrganicNeighborhood.Systems
{
    [BurstCompile]
    public struct GenerateOrganicGridJob : IJob
    {
        // Input
        [ReadOnly] public float3 m_CornerA;
        [ReadOnly] public float3 m_CornerB;
        [ReadOnly] public float3 m_CornerC;
        [ReadOnly] public LayoutParameters m_Parameters;

        // Output
        public NativeList<RoadDefinition> m_GeneratedRoads;

        public void Execute()
        {
            // Calculate grid dimensions
            float2 dimensions = new float2(
                math.distance(m_CornerA.xz, m_CornerB.xz),
                math.distance(m_CornerA.xz, m_CornerC.xz)
            );

            // Calculate road count
            int2 roadCount = (int2)(dimensions / m_Parameters.m_RoadSpacing);

            // Generate horizontal roads
            for (int row = 0; row <= roadCount.y; row++)
            {
                float tRow = (float)row / roadCount.y;

                // Apply Perlin variation to row position
                float rowVar = BurstPerlinNoise.Perlin2D(
                    new float2(0, row * 10f) * 0.1f);
                rowVar = (rowVar - 0.5f) * 0.2f;
                tRow = math.clamp(tRow + rowVar, 0f, 1f);

                // Calculate endpoints
                float3 startBase = math.lerp(m_CornerA, m_CornerB, tRow);
                float3 endBase = math.lerp(m_CornerA, m_CornerC, tRow);

                // Apply position variation
                float3 start = BurstPerlinNoise.ApplyOrganicVariation(
                    startBase, m_Parameters.m_PositionVariation);
                float3 end = BurstPerlinNoise.ApplyOrganicVariation(
                    endBase, m_Parameters.m_PositionVariation);

                // Create road
                m_GeneratedRoads.Add(new RoadDefinition
                {
                    m_Start = start,
                    m_End = end
                });
            }

            // Generate vertical roads (similar)
            // ...
        }
    }

    public struct RoadDefinition
    {
        public float3 m_Start;
        public float3 m_End;
        public float m_CurveAmount;
    }
}
```

#### 3.2 Integration in Tool System

Update `OrganicNeighborhoodToolSystem.OnUpdate()`:

```csharp
protected override void OnUpdate()
{
    HandleInput();

    if (m_ControlPoints.Length == 3 && !m_PreviewActive)
    {
        GenerateLayout();
    }
}

private void GenerateLayout()
{
    var job = new GenerateOrganicGridJob
    {
        m_CornerA = m_ControlPoints[0].m_Position,
        m_CornerB = m_ControlPoints[1].m_Position,
        m_CornerC = m_ControlPoints[2].m_Position,
        m_Parameters = m_LayoutParameters,
        m_GeneratedRoads = new NativeList<RoadDefinition>(
            100, Allocator.TempJob)
    };

    job.Run();

    // Debug: Draw lines for each road
    foreach (var road in job.m_GeneratedRoads)
    {
        Debug.DrawLine(road.m_Start, road.m_End, Color.green, 10f);
    }

    job.m_GeneratedRoads.Dispose();

    m_PreviewActive = true;
}
```

**Test**:
1. Click 3 points
2. See organic grid of green lines
3. Verify Perlin variation (not perfectly straight)

**Deliverable**: Working organic grid generator

---

## PHASE 4: Terrain Awareness (Days 8-10)

### Goals
- Add terrain snapping
- Add slope validation
- Add water detection

### Tasks

#### 4.1 Update GenerateOrganicGridJob

Add terrain data:
```csharp
[BurstCompile]
public struct GenerateOrganicGridJob : IJob
{
    // Add these
    [ReadOnly] public TerrainHeightData m_TerrainData;
    [ReadOnly] public WaterSurfacesData m_WaterData;

    public void Execute()
    {
        // ... existing code ...

        // SNAP TO TERRAIN
        if (m_Parameters.m_SnapToTerrain)
        {
            start = TerrainHelpers.SnapToTerrain(start, ref m_TerrainData);
            end = TerrainHelpers.SnapToTerrain(end, ref m_TerrainData);
        }

        // VALIDATE SLOPE
        if (m_Parameters.m_ValidateSlope)
        {
            if (!TerrainHelpers.ValidateSlope(
                start, end, ref m_TerrainData, m_Parameters.m_MaxSlope))
            {
                continue;  // Skip this road
            }
        }

        // CREATE TERRAIN-FOLLOWING CURVE
        Bezier4x3 curve = CreateTerrainFollowingCurve(
            start, end, ref m_TerrainData);

        // ... add to output ...
    }
}
```

#### 4.2 Update Tool System

Pass terrain data:
```csharp
private void GenerateLayout()
{
    // GET TERRAIN DATA
    TerrainHeightData terrainData = m_TerrainSystem.GetHeightData();
    WaterSurfacesData waterData = m_WaterSystem.GetSurfaceData();

    var job = new GenerateOrganicGridJob
    {
        // ... existing ...
        m_TerrainData = terrainData,
        m_WaterData = waterData,
        // ...
    };

    job.Run();
    // ...
}
```

**Test**:
1. Click 3 points on hilly terrain
2. Verify roads follow terrain elevation
3. Verify steep slopes are rejected

**Deliverable**: Terrain-aware grid generator

---

## PHASE 5: NetCourse Integration (Days 11-13)

### Goals
- Create actual roads (not just debug lines)
- Use NetToolSystem patterns
- Preview/apply workflow

### Tasks

#### 5.1 Create NetCourse Entities

Replace debug drawing with real road creation:

```csharp
private void GenerateLayout()
{
    var job = new GenerateOrganicGridJob { /* ... */ };
    job.Run();

    // CREATE ACTUAL ROADS
    foreach (var roadDef in job.m_GeneratedRoads)
    {
        CreateNetCourse(roadDef);
    }

    job.m_GeneratedRoads.Dispose();
}

private void CreateNetCourse(RoadDefinition roadDef)
{
    // Get road prefab
    Entity roadPrefab = GetDefaultRoadPrefab();

    // Create entity
    Entity courseEntity = EntityManager.CreateEntity();

    // Add CreationDefinition
    EntityManager.AddComponentData(courseEntity, new CreationDefinition
    {
        m_Prefab = roadPrefab,
        m_RandomSeed = UnityEngine.Random.Range(0, int.MaxValue)
    });

    // Create curve
    Bezier4x3 curve = CreateCurve(roadDef);

    // Add NetCourse
    EntityManager.AddComponentData(courseEntity, new NetCourse
    {
        m_Curve = curve,
        m_StartPosition = new CoursePos
        {
            m_Position = curve.a,
            m_Rotation = CalculateRotation(curve),
            m_Flags = CoursePosFlags.IsGrid,
        },
        m_EndPosition = new CoursePos
        {
            m_Position = curve.d,
            m_Rotation = CalculateRotation(curve),
            m_Flags = CoursePosFlags.IsGrid,
        },
        m_Length = MathUtils.Length(curve),
        m_FixedIndex = -1,
    });

    // Add Temp flag for preview
    EntityManager.AddComponentData(courseEntity, new Temp
    {
        m_Flags = TempFlags.Create
    });

    // Track for later
    m_PreviewEntities.Add(courseEntity);
}
```

#### 5.2 Preview Workflow

```csharp
private void ApplyLayout()
{
    // Remove Temp flags → roads become permanent
    foreach (var entity in m_PreviewEntities)
    {
        EntityManager.RemoveComponent<Temp>(entity);
    }

    m_PreviewEntities.Clear();
    m_PreviewActive = false;
    m_ControlPoints.Clear();
}

private void CancelLayout()
{
    // Delete preview entities
    foreach (var entity in m_PreviewEntities)
    {
        EntityManager.DestroyEntity(entity);
    }

    m_PreviewEntities.Clear();
    m_PreviewActive = false;
    m_ControlPoints.Clear();
}

protected override void OnUpdate()
{
    HandleInput();

    if (m_ControlPoints.Length == 3 && !m_PreviewActive)
    {
        GenerateLayout();
    }

    // HANDLE CONFIRM/CANCEL
    if (m_PreviewActive)
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            ApplyLayout();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelLayout();
        }
    }
}
```

**Test**:
1. Click 3 points
2. See actual road preview
3. Press Enter → roads become permanent
4. OR press Escape → roads disappear

**Deliverable**: Full preview/apply workflow

---

## PHASE 6: UI & Polish (Days 14-15)

### Goals
- Add UI panel
- Parameter adjustment
- Visual polish

### Tasks

#### 6.1 UI Panel (if time permits)
Create simple UI or expose via existing settings

#### 6.2 Parameter Tuning
Test and tune default values:
- Road spacing: 50m
- Position variation: 5m
- Curve amount: 0.3
- Max slope: 15°

#### 6.3 Testing
- Flat terrain
- Hilly terrain
- Mixed terrain with water
- Large areas (500m × 500m)
- Small areas (50m × 50m)

**Deliverable**: Polished, tested tool

---

## DEVELOPMENT WORKFLOW

### Each Phase

1. **Write code** for phase
2. **Compile** - fix errors
3. **Test in-game** - verify functionality
4. **Debug** - fix issues
5. **Commit** - save progress
6. **Move to next phase**

### Testing Strategy

**Phase 1-2**: Unit tests, compilation checks
**Phase 3**: Debug visualization (DrawLine)
**Phase 4**: Test on different terrains
**Phase 5**: Full integration test
**Phase 6**: User experience test

---

## QUICK START - FIRST STEPS

### Step 1: Create Project Structure (Now!)

```bash
cd /home/user/roadmod
mkdir -p OrganicNeighborhoodMod/Utils
mkdir -p OrganicNeighborhoodMod/Data
mkdir -p OrganicNeighborhoodMod/Systems
```

### Step 2: Create BurstPerlinNoise.cs

Copy implementation from `NOISE_AND_VARIATION_IMPLEMENTATION.md`

### Step 3: Test Compilation

Create a simple test script that uses the noise function

### Step 4: Proceed to Phase 2

---

## MILESTONES

- **Week 1 End**: Phases 1-3 complete (basic grid generation)
- **Week 2 Mid**: Phases 4-5 complete (terrain-aware roads)
- **Week 2 End**: Phase 6 complete (polished tool)

---

## RISK MITIGATION

### If Stuck
- **Compilation errors**: Check references to Game.* namespaces
- **Terrain data null**: Verify TerrainSystem exists
- **Roads not appearing**: Check Temp flags and entity creation
- **Performance issues**: Add Burst compilation attributes

### Debugging Tools
- Debug.DrawLine() for visualization
- Unity Console for error messages
- In-game debug overlays
- Entity debugger for ECS inspection

---

## SUCCESS CRITERIA

### Phase 1
✅ BurstPerlinNoise compiles and runs
✅ TerrainHelpers compiles
✅ LayoutParameters defined

### Phase 3
✅ Can click 3 points
✅ Grid appears with variation
✅ Perlin variation visible

### Phase 4
✅ Roads follow terrain
✅ Steep slopes rejected
✅ Water avoided

### Phase 5
✅ Roads appear in game
✅ Preview works
✅ Apply/cancel works

### Phase 6
✅ Tool is usable
✅ Parameters adjustable
✅ Performance acceptable

---

## NEXT ACTION

**START HERE**: Create BurstPerlinNoise.cs

I'm ready to help you write the code for each phase!

What would you like to do first?
1. Create the utility files (Phase 1)?
2. Jump straight to tool system (Phase 2)?
3. Something else?
