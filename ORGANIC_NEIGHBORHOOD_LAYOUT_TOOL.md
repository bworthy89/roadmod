# Organic Neighborhood Layout Tool - Implementation Plan
## Cities: Skylines II Mod

---

## OVERVIEW

**Goal**: Create a tool that generates realistic, organic neighborhood road layouts similar to the built-in grid tool but with natural variations, curved streets, cul-de-sacs, and hierarchical road networks.

**Philosophy**: Real neighborhoods aren't perfect grids. They have:
- Natural curves following terrain
- Irregular block sizes
- Cul-de-sacs and dead-ends in residential areas
- Hierarchical networks (arterial → collector → local streets)
- Subtle variations in spacing and alignment
- Character and uniqueness

**Approach**: Extend the existing grid tool architecture with procedural generation algorithms that create organic, realistic layouts.

---

## SYSTEM ARCHITECTURE

### Tool Structure

```
OrganicNeighborhoodToolSystem (ToolBaseSystem)
├── Input: 3-point area definition (like grid tool)
├── Layout Generator: Creates NetCourse entities with variation
├── Pattern Selector: Choose layout style
└── Visualization: Preview before applying
```

### Layout Styles

The tool supports multiple neighborhood patterns:

1. **Organic Grid** - Traditional grid with natural variations
2. **Curvilinear** - Flowing curved streets
3. **Cul-de-Sac Residential** - Hierarchical with dead-ends
4. **Mixed Development** - Combination of patterns
5. **European Style** - Irregular blocks, radial streets
6. **Suburban** - Wide lots, curved collectors

---

## CORE ALGORITHM: ORGANIC GRID LAYOUT

### Base Grid with Variation

**Step 1: Create Base Grid** (like existing tool)
```csharp
// Use same 3-point input as grid tool
ControlPoint cornerA = m_ControlPoints[0];
ControlPoint cornerB = m_ControlPoints[1];
ControlPoint cornerC = m_ControlPoints[2];

// Calculate dimensions
float2 dimensions = new float2(
    math.distance(cornerA.m_Position.xz, cornerB.m_Position.xz),
    math.distance(cornerB.m_Position.xz, cornerC.m_Position.xz)
);

// Determine number of roads (with variation)
int2 roadCount = CalculateOrganicRoadCount(dimensions, m_LayoutStyle);
```

**Step 2: Apply Perlin Noise Variation**
```csharp
private float3 ApplyOrganicVariation(float3 basePosition, float variationStrength)
{
    // Use Perlin noise for smooth, natural variation
    float noiseX = PerlinNoise2D(basePosition.xz * 0.1f) * variationStrength;
    float noiseZ = PerlinNoise2D(basePosition.xz * 0.1f + 100f) * variationStrength;

    return basePosition + new float3(noiseX, 0, noiseZ);
}

// Perlin noise implementation (standard 2D)
private float PerlinNoise2D(float2 position)
{
    // Hash-based Perlin noise
    float2 i = math.floor(position);
    float2 f = math.frac(position);

    // Cubic interpolation
    float2 u = f * f * (3.0f - 2.0f * f);

    // Sample corners and interpolate
    float a = Hash2D(i);
    float b = Hash2D(i + new float2(1, 0));
    float c = Hash2D(i + new float2(0, 1));
    float d = Hash2D(i + new float2(1, 1));

    return math.lerp(math.lerp(a, b, u.x), math.lerp(c, d, u.x), u.y);
}

private float Hash2D(float2 p)
{
    // Simple hash function
    p = math.frac(p * new float2(234.34f, 435.345f));
    p += math.dot(p, p + 34.23f);
    return math.frac(p.x * p.y);
}
```

**Step 3: Create Roads with Variation**
```csharp
private void CreateOrganicGridLayout(
    ControlPoint cornerA,
    ControlPoint cornerB,
    ControlPoint cornerC,
    LayoutParameters parameters)
{
    // Calculate base spacing
    float2 baseSpacing = CalculateSpacing(dimensions, roadCount);

    // Create horizontal roads
    for (int row = 0; row <= roadCount.y; row++)
    {
        float tRow = (float)row / roadCount.y;

        // Apply row-level variation
        float rowVariation = (PerlinNoise2D(new float2(0, row * 10)) - 0.5f) * 2f;
        tRow += rowVariation * parameters.m_SpacingVariation;
        tRow = math.clamp(tRow, 0f, 1f);

        // Calculate endpoints
        float3 startPos = math.lerp(cornerA.m_Position, cornerB.m_Position, tRow);
        float3 endPos = math.lerp(cornerA.m_Position, cornerC.m_Position, tRow);

        // Apply per-vertex variation
        startPos = ApplyOrganicVariation(startPos, parameters.m_PositionVariation);
        endPos = ApplyOrganicVariation(endPos, parameters.m_PositionVariation);

        // Create curved road instead of straight
        Bezier4x3 curve = CreateOrganicCurve(startPos, endPos, parameters.m_CurveAmount);

        CreateNetCourse(curve, parameters.m_RoadPrefab);
    }

    // Create vertical roads (similar process)
    for (int col = 0; col <= roadCount.x; col++)
    {
        // ... same pattern
    }
}
```

---

## ADVANCED PATTERN: CURVILINEAR LAYOUT

### Flowing Curved Streets

**Algorithm: Sine Wave Distortion**
```csharp
private Bezier4x3 CreateOrganicCurve(float3 start, float3 end, float curveStrength)
{
    float distance = math.distance(start, end);
    float3 direction = math.normalize(end - start);
    float3 perpendicular = new float3(-direction.z, 0, direction.x);

    // Create control points with sine wave offset
    float wavelength = distance / 2f;  // One full wave per road
    float amplitude = curveStrength * 10f;  // Max 10m deviation

    Bezier4x3 curve;
    curve.a = start;
    curve.d = end;

    // Intermediate control points follow sine wave
    float t1 = 0.33f;
    float offset1 = math.sin(t1 * math.PI) * amplitude;
    curve.b = math.lerp(start, end, t1) + perpendicular * offset1;

    float t2 = 0.67f;
    float offset2 = math.sin(t2 * math.PI) * amplitude;
    curve.c = math.lerp(start, end, t2) + perpendicular * offset2;

    return curve;
}
```

**Algorithm: Natural Flow Following Terrain**
```csharp
private Bezier4x3 CreateTerrainFollowingCurve(
    float3 start,
    float3 end,
    TerrainHeightData terrain)
{
    // Sample terrain at multiple points
    const int samples = 5;
    NativeArray<float3> points = new NativeArray<float3>(samples, Allocator.Temp);

    for (int i = 0; i < samples; i++)
    {
        float t = (float)i / (samples - 1);
        float3 pos = math.lerp(start, end, t);

        // Get terrain height
        pos.y = terrain.SampleHeight(pos.xz);

        // Add slight lateral variation based on slope
        float2 gradient = terrain.SampleGradient(pos.xz);
        float3 slopeOffset = new float3(gradient.x, 0, gradient.y) * 5f;
        pos += slopeOffset;

        points[i] = pos;
    }

    // Fit Bezier curve through points
    Bezier4x3 curve = FitCurveThroughPoints(points);
    points.Dispose();

    return curve;
}

private Bezier4x3 FitCurveThroughPoints(NativeArray<float3> points)
{
    // Least-squares curve fitting
    Bezier4x3 curve;
    curve.a = points[0];
    curve.d = points[points.Length - 1];

    // Use middle points to determine control points
    curve.b = points[1];
    curve.c = points[points.Length - 2];

    return curve;
}
```

---

## ADVANCED PATTERN: CUL-DE-SAC RESIDENTIAL

### Hierarchical Network with Dead-Ends

**Layout Structure**:
```
Arterial Road (perimeter)
├── Collector Road 1 (perpendicular)
│   ├── Local Street 1A
│   │   ├── Cul-de-sac 1A1
│   │   └── Cul-de-sac 1A2
│   └── Local Street 1B
│       └── Cul-de-sac 1B1
├── Collector Road 2
│   └── ...
└── Collector Road 3
```

**Implementation**:
```csharp
private void CreateCulDeSacLayout(
    Quad2 boundary,
    LayoutParameters parameters)
{
    // Step 1: Create perimeter arterial road
    Entity perimeterRoad = CreatePerimeterRoad(boundary, parameters.m_ArterialPrefab);

    // Step 2: Create collector roads (perpendicular to longest edge)
    float2 longestEdge = GetLongestEdge(boundary);
    float2 perpendicular = MathUtils.Right(math.normalize(longestEdge));

    int collectorCount = (int)(math.length(longestEdge) / 100f);  // Every 100m

    for (int i = 0; i < collectorCount; i++)
    {
        float t = (float)(i + 1) / (collectorCount + 1);
        float3 startPos = math.lerp(boundary.a, boundary.b, t);
        float3 endPos = startPos + new float3(perpendicular.x, 0, perpendicular.y) * 60f;

        // Create collector with slight curve
        Bezier4x3 collectorCurve = CreateOrganicCurve(
            startPos, endPos, parameters.m_CurveAmount);
        Entity collectorRoad = CreateNetCourse(collectorCurve, parameters.m_CollectorPrefab);

        // Step 3: Branch local streets from collector
        CreateLocalStreetBranches(collectorCurve, parameters);
    }
}

private void CreateLocalStreetBranches(
    Bezier4x3 collectorCurve,
    LayoutParameters parameters)
{
    // Sample points along collector
    int branchCount = 3;  // 3 branches per collector

    for (int i = 0; i < branchCount; i++)
    {
        float t = (float)(i + 1) / (branchCount + 1);
        float3 branchStart = MathUtils.Position(collectorCurve, t);
        float3 tangent = MathUtils.Tangent(collectorCurve, t);
        float3 perpendicular = new float3(-tangent.z, 0, tangent.x);

        // Alternate sides
        float side = (i % 2 == 0) ? 1f : -1f;
        float3 branchEnd = branchStart + perpendicular * side * 40f;

        // Create local street
        Bezier4x3 localCurve = NetUtils.StraightCurve(branchStart, branchEnd);
        Entity localStreet = CreateNetCourse(localCurve, parameters.m_LocalPrefab);

        // Step 4: Add cul-de-sac at end
        CreateCulDeSac(branchEnd, perpendicular * side, parameters);
    }
}

private void CreateCulDeSac(
    float3 center,
    float3 approach,
    LayoutParameters parameters)
{
    // Create circular cul-de-sac (radius 15-20m)
    float radius = 18f;
    int segments = 8;  // Octagonal approximation

    NativeList<float3> circlePoints = new NativeList<float3>(segments, Allocator.Temp);

    for (int i = 0; i < segments; i++)
    {
        float angle = (float)i / segments * 2f * math.PI;
        float3 offset = new float3(
            math.cos(angle) * radius,
            0,
            math.sin(angle) * radius
        );
        circlePoints.Add(center + offset);
    }

    // Create arc roads
    for (int i = 0; i < segments; i++)
    {
        float3 start = circlePoints[i];
        float3 end = circlePoints[(i + 1) % segments];

        // Create curved segment
        Bezier4x3 arc = CreateArcCurve(start, end, center);
        CreateNetCourse(arc, parameters.m_LocalPrefab);
    }

    circlePoints.Dispose();
}

private Bezier4x3 CreateArcCurve(float3 start, float3 end, float3 center)
{
    // Create circular arc using bezier approximation
    float3 midpoint = (start + end) * 0.5f;
    float3 toMid = math.normalize(midpoint - center);
    float radius = math.distance(start, center);

    // Control points pulled toward arc
    float3 controlOffset = toMid * radius * 0.55f;  // Bezier circle constant

    Bezier4x3 curve;
    curve.a = start;
    curve.b = start + (midpoint - start) * 0.33f + controlOffset * 0.5f;
    curve.c = end - (end - midpoint) * 0.33f + controlOffset * 0.5f;
    curve.d = end;

    return curve;
}
```

---

## ADVANCED PATTERN: EUROPEAN STYLE

### Irregular Blocks with Radial Streets

**Characteristics**:
- Non-orthogonal intersections
- Varying block sizes (30m to 100m)
- Radial streets from central point
- Irregular angles (not just 90°)

**Implementation**:
```csharp
private void CreateEuropeanStyleLayout(
    float3 centerPoint,
    float radius,
    LayoutParameters parameters)
{
    // Step 1: Create radial arterial roads
    int radialCount = parameters.m_RadialCount;  // 6-8 radials

    for (int i = 0; i < radialCount; i++)
    {
        float angle = (float)i / radialCount * 2f * math.PI;

        // Add angular variation
        angle += (PerlinNoise2D(new float2(i * 10, 0)) - 0.5f) * 0.3f;

        float3 direction = new float3(math.cos(angle), 0, math.sin(angle));
        float3 endPoint = centerPoint + direction * radius;

        // Create radial road with curve
        Bezier4x3 radialCurve = CreateOrganicCurve(
            centerPoint, endPoint, parameters.m_CurveAmount);
        CreateNetCourse(radialCurve, parameters.m_ArterialPrefab);
    }

    // Step 2: Create concentric ring roads
    int ringCount = 3;
    for (int ring = 1; ring <= ringCount; ring++)
    {
        float ringRadius = radius * (float)ring / (ringCount + 1);

        // Add radial variation
        ringRadius += (PerlinNoise2D(new float2(0, ring * 10)) - 0.5f) * 10f;

        CreateConcentricRing(centerPoint, ringRadius, radialCount, parameters);
    }

    // Step 3: Fill gaps with connector streets
    FillIrregularGaps(centerPoint, radius, parameters);
}

private void CreateConcentricRing(
    float3 center,
    float radius,
    int segments,
    LayoutParameters parameters)
{
    for (int i = 0; i < segments; i++)
    {
        float angle1 = (float)i / segments * 2f * math.PI;
        float angle2 = (float)(i + 1) / segments * 2f * math.PI;

        // Add per-vertex variation
        float r1 = radius + (PerlinNoise2D(new float2(i * 5, 0)) - 0.5f) * 5f;
        float r2 = radius + (PerlinNoise2D(new float2((i+1) * 5, 0)) - 0.5f) * 5f;

        float3 point1 = center + new float3(math.cos(angle1) * r1, 0, math.sin(angle1) * r1);
        float3 point2 = center + new float3(math.cos(angle2) * r2, 0, math.sin(angle2) * r2);

        // Create arc segment
        Bezier4x3 arc = CreateArcCurve(point1, point2, center);
        CreateNetCourse(arc, parameters.m_CollectorPrefab);
    }
}
```

---

## LAYOUT PARAMETERS

### Configuration Structure

```csharp
public struct LayoutParameters
{
    // Road Types
    public Entity m_ArterialPrefab;      // Wide main roads
    public Entity m_CollectorPrefab;     // Medium connector roads
    public Entity m_LocalPrefab;         // Narrow local streets

    // Spacing
    public float m_ArterialSpacing;      // 100-200m
    public float m_CollectorSpacing;     // 50-80m
    public float m_LocalSpacing;         // 30-50m

    // Variation
    public float m_PositionVariation;    // 0-10m per vertex
    public float m_SpacingVariation;     // 0-0.2 (20% variation)
    public float m_AngleVariation;       // 0-15° variation
    public float m_CurveAmount;          // 0-1 (0=straight, 1=very curved)

    // Layout Style
    public LayoutStyle m_Style;
    public int m_RadialCount;            // For radial layouts
    public float m_CulDeSacProbability;  // 0-1 chance of cul-de-sac

    // Block Size
    public float2 m_MinBlockSize;        // Minimum block dimensions
    public float2 m_MaxBlockSize;        // Maximum block dimensions
}

public enum LayoutStyle
{
    OrganicGrid,          // Traditional grid + variation
    Curvilinear,          // Flowing curves
    CulDeSacResidential,  // Hierarchical with dead-ends
    MixedDevelopment,     // Combination
    EuropeanStyle,        // Irregular radial
    Suburban,             // Wide lots, curves
}
```

---

## USER INTERFACE

### Tool Activation

```csharp
public class OrganicNeighborhoodToolSystem : ToolBaseSystem
{
    // Tool state
    private LayoutStyle m_CurrentStyle = LayoutStyle.OrganicGrid;
    private LayoutParameters m_Parameters;
    private NativeList<ControlPoint> m_ControlPoints;
    private bool m_PreviewActive;

    // Preview entities
    private NativeList<Entity> m_PreviewRoads;

    protected override void OnCreate()
    {
        base.OnCreate();

        // Default parameters
        m_Parameters = new LayoutParameters
        {
            m_ArterialSpacing = 150f,
            m_CollectorSpacing = 60f,
            m_LocalSpacing = 40f,
            m_PositionVariation = 5f,
            m_SpacingVariation = 0.15f,
            m_AngleVariation = 10f,
            m_CurveAmount = 0.3f,
            m_Style = LayoutStyle.OrganicGrid,
            m_RadialCount = 6,
            m_CulDeSacProbability = 0.3f,
            m_MinBlockSize = new float2(30f, 30f),
            m_MaxBlockSize = new float2(100f, 80f),
        };
    }
}
```

### Input Flow

**Same 3-point system as grid tool**:

1. **Click 1**: Define area origin
2. **Click 2**: Define first dimension
3. **Click 3**: Define second dimension
4. **Preview**: Show generated layout (all roads as Temp)
5. **Confirm/Cancel**: Apply or discard

### UI Controls

```csharp
// Exposed to UI system
public class NeighborhoodToolUI : UISystemBase
{
    // Style selector
    public void SetLayoutStyle(LayoutStyle style) { /* ... */ }

    // Sliders
    public void SetVariationAmount(float amount) { /* 0-1 */ }
    public void SetCurveAmount(float amount) { /* 0-1 */ }
    public void SetDensity(float density) { /* 0-1, affects spacing */ }

    // Toggles
    public void SetIncludeCulDeSacs(bool include) { /* ... */ }
    public void SetFollowTerrain(bool follow) { /* ... */ }

    // Buttons
    public void Regenerate() { /* New random seed */ }
    public void Apply() { /* Convert Temp → permanent */ }
    public void Cancel() { /* Delete Temp entities */ }
}
```

---

## IMPLEMENTATION ROADMAP

### Phase 1: Core Tool Structure (Week 1)

- [x] Study grid tool implementation
- [ ] Create `OrganicNeighborhoodToolSystem` inheriting `ToolBaseSystem`
- [ ] Implement 3-point area definition
- [ ] Add control point system
- [ ] Implement basic preview rendering
- [ ] Test with single straight road

### Phase 2: Organic Grid Pattern (Week 2)

- [ ] Implement Perlin noise utility
- [ ] Create `ApplyOrganicVariation()` method
- [ ] Implement organic grid generation
- [ ] Add curve creation (sine wave)
- [ ] Test variation parameters
- [ ] Tune variation amounts

### Phase 3: Curvilinear Pattern (Week 3)

- [ ] Implement terrain-following curves
- [ ] Create bezier curve fitting
- [ ] Add advanced curve distortion
- [ ] Test on various terrain types
- [ ] Optimize curve smoothness

### Phase 4: Hierarchical Patterns (Week 4)

- [ ] Implement cul-de-sac generator
- [ ] Create hierarchical road network
- [ ] Add collector/local street branching
- [ ] Test connectivity
- [ ] Validate block generation

### Phase 5: Advanced Patterns (Week 5)

- [ ] Implement European radial layout
- [ ] Create concentric ring system
- [ ] Add irregular gap filling
- [ ] Test non-orthogonal intersections

### Phase 6: UI Integration (Week 6)

- [ ] Create tool UI panel
- [ ] Add style selector dropdown
- [ ] Implement parameter sliders
- [ ] Add regenerate button
- [ ] Test user workflow

### Phase 7: Polish & Optimization (Week 7)

- [ ] Burst compile all jobs
- [ ] Add spatial queries for collision
- [ ] Implement undo/redo
- [ ] Add tooltips and help
- [ ] Performance profiling

### Phase 8: Testing & Release (Week 8)

- [ ] Test all layout styles
- [ ] Test on various terrain types
- [ ] Community testing
- [ ] Bug fixes
- [ ] Documentation
- [ ] Release v1.0

---

## TECHNICAL IMPLEMENTATION DETAILS

### NetCourse Creation Pattern

```csharp
private Entity CreateNetCourse(Bezier4x3 curve, Entity roadPrefab)
{
    // Create entity
    Entity courseEntity = m_CommandBuffer.CreateEntity();

    // Add definition
    CreationDefinition definition = new CreationDefinition
    {
        m_Prefab = roadPrefab,
        m_SubPrefab = Entity.Null,
        m_RandomSeed = m_Random.NextInt(),
        m_Flags = CreationFlags.Permanent,
    };
    m_CommandBuffer.AddComponent(courseEntity, definition);

    // Create course
    NetCourse course = new NetCourse
    {
        m_Curve = curve,
        m_StartPosition = new CoursePos
        {
            m_Position = curve.a,
            m_Rotation = quaternion.LookRotationSafe(
                math.normalize(curve.b - curve.a),
                new float3(0, 1, 0)),
            m_Elevation = new float2(curve.a.y, curve.a.y),
            m_Flags = CoursePosFlags.IsGrid,
        },
        m_EndPosition = new CoursePos
        {
            m_Position = curve.d,
            m_Rotation = quaternion.LookRotationSafe(
                math.normalize(curve.d - curve.c),
                new float3(0, 1, 0)),
            m_Elevation = new float2(curve.d.y, curve.d.y),
            m_Flags = CoursePosFlags.IsGrid,
        },
        m_Length = MathUtils.Length(curve),
        m_FixedIndex = -1,
    };
    m_CommandBuffer.AddComponent(courseEntity, course);

    // Add to preview list
    m_PreviewRoads.Add(courseEntity);

    return courseEntity;
}
```

### Node Deduplication

```csharp
// Use hash map to merge nearby nodes
private NativeParallelHashMap<int, float3> m_NodePositions;

private float3 SnapToNearbyNode(float3 position, float snapDistance = 2f)
{
    // Create spatial hash key
    int key = GetSpatialHashKey(position, snapDistance);

    if (m_NodePositions.TryGetValue(key, out float3 existingPos))
    {
        float distance = math.distance(position, existingPos);
        if (distance < snapDistance)
        {
            return existingPos;  // Snap to existing
        }
    }

    // Add new position
    m_NodePositions.TryAdd(key, position);
    return position;
}

private int GetSpatialHashKey(float3 position, float cellSize)
{
    int x = (int)(position.x / cellSize);
    int z = (int)(position.z / cellSize);
    return (x << 16) | (z & 0xFFFF);
}
```

### Preview System

```csharp
protected override void OnUpdate()
{
    // Check input
    if (GetRaycastResult(out RaycastHit hit))
    {
        if (m_ControlPoints.Length < 3)
        {
            // Still defining area
            AddControlPoint(hit.m_HitPosition);
        }
        else if (!m_PreviewActive)
        {
            // Generate preview
            GeneratePreviewLayout();
            m_PreviewActive = true;
        }
    }

    // Confirm/cancel input
    if (Input.GetKeyDown(KeyCode.Return) && m_PreviewActive)
    {
        ApplyLayout();  // Convert Temp → permanent
    }
    else if (Input.GetKeyDown(KeyCode.Escape))
    {
        CancelLayout();  // Delete Temp entities
    }
}

private void GeneratePreviewLayout()
{
    // Clear previous preview
    ClearPreview();

    // Generate layout based on style
    switch (m_Parameters.m_Style)
    {
        case LayoutStyle.OrganicGrid:
            CreateOrganicGridLayout(
                m_ControlPoints[0],
                m_ControlPoints[1],
                m_ControlPoints[2],
                m_Parameters);
            break;

        case LayoutStyle.Curvilinear:
            CreateCurvilinearLayout(/* ... */);
            break;

        case LayoutStyle.CulDeSacResidential:
            CreateCulDeSacLayout(/* ... */);
            break;

        // ... other styles
    }

    // Mark all as Temp for preview
    foreach (var roadEntity in m_PreviewRoads)
    {
        m_CommandBuffer.AddComponent(roadEntity, new Temp
        {
            m_Flags = TempFlags.Create,
        });
    }
}

private void ApplyLayout()
{
    // Remove Temp flags → roads become permanent
    foreach (var roadEntity in m_PreviewRoads)
    {
        EntityManager.RemoveComponent<Temp>(roadEntity);
    }

    m_PreviewRoads.Clear();
    m_PreviewActive = false;
}

private void CancelLayout()
{
    // Delete all preview entities
    foreach (var roadEntity in m_PreviewRoads)
    {
        EntityManager.DestroyEntity(roadEntity);
    }

    m_PreviewRoads.Clear();
    m_PreviewActive = false;
}
```

---

## VALIDATION & CONSTRAINTS

### Ensure Valid Layouts

```csharp
private bool ValidateLayout(Bezier4x3 curve)
{
    // 1. Check minimum road length
    if (MathUtils.Length(curve) < 20f)
        return false;

    // 2. Check maximum slope
    float heightDiff = math.abs(curve.a.y - curve.d.y);
    float horizontalDist = math.distance(curve.a.xz, curve.d.xz);
    float slope = heightDiff / horizontalDist;

    if (slope > 0.15f)  // 15% max slope
        return false;

    // 3. Check for building collisions
    if (IntersectsBuilding(curve))
        return false;

    // 4. Check minimum separation from existing roads
    if (TooCloseToExistingRoad(curve, minDistance: 15f))
        return false;

    return true;
}

private bool IntersectsBuilding(Bezier4x3 curve)
{
    // Sample curve at intervals
    const int samples = 10;
    for (int i = 0; i < samples; i++)
    {
        float t = (float)i / (samples - 1);
        float3 pos = MathUtils.Position(curve, t);

        // Query buildings in radius
        if (m_BuildingQuery.GetNearestBuilding(pos, radius: 10f, out Entity building))
        {
            return true;  // Collision detected
        }
    }

    return false;
}
```

---

## PERFORMANCE CONSIDERATIONS

### Burst Compilation

```csharp
[BurstCompile]
private struct GenerateOrganicRoadsJob : IJob
{
    // Input
    [ReadOnly] public LayoutParameters m_Parameters;
    [ReadOnly] public NativeArray<ControlPoint> m_ControlPoints;

    // Output
    public NativeList<Bezier4x3> m_GeneratedCurves;

    public void Execute()
    {
        // All logic Burst-compatible
        // No managed objects
        // Use only NativeContainers
    }
}
```

### Memory Management

```csharp
protected override void OnCreate()
{
    base.OnCreate();

    // Pre-allocate containers
    m_PreviewRoads = new NativeList<Entity>(100, Allocator.Persistent);
    m_NodePositions = new NativeParallelHashMap<int, float3>(200, Allocator.Persistent);
}

protected override void OnDestroy()
{
    // Clean up
    m_PreviewRoads.Dispose();
    m_NodePositions.Dispose();

    base.OnDestroy();
}
```

---

## EXAMPLE LAYOUTS

### Example 1: Organic Grid (Small Neighborhood)

**Parameters**:
- Style: OrganicGrid
- Area: 200m × 200m
- Variation: 0.5 (medium)
- Curve: 0.2 (subtle)

**Result**:
- 5×5 grid (25 blocks)
- Each block: 35-45m (varied)
- Roads: Slightly curved
- Intersections: Not perfectly aligned
- Natural, hand-drawn appearance

### Example 2: Cul-de-Sac Residential

**Parameters**:
- Style: CulDeSacResidential
- Area: 300m × 200m
- CulDeSac Probability: 0.4

**Result**:
- 1 arterial perimeter
- 3 collector roads
- 9 local streets
- 6 cul-de-sacs
- Total: 40-50 building lots

### Example 3: European Radial

**Parameters**:
- Style: EuropeanStyle
- Center: Market square
- Radius: 150m
- Radials: 7

**Result**:
- 7 radial arterials
- 3 concentric rings
- Irregular block sizes
- 30-60° intersection angles
- Old-world character

---

## FUTURE ENHANCEMENTS

### Planned Features

1. **Terrain Integration**
   - Follow contour lines
   - Avoid steep slopes
   - Bridge generation

2. **Zoning Integration**
   - Pre-zone generated blocks
   - Mix residential/commercial
   - Density-based spacing

3. **POI Integration**
   - Roads connect to landmarks
   - Park integration
   - School/services placement

4. **Templates**
   - Save/load custom layouts
   - Community sharing
   - Procedural variations

5. **Advanced Algorithms**
   - L-system generation
   - Genetic algorithms
   - Real-world city pattern matching

---

## FILE STRUCTURE

```
/OrganicNeighborhoodMod/
├── Systems/
│   ├── OrganicNeighborhoodToolSystem.cs
│   ├── NeighborhoodGeneratorSystem.cs
│   └── NeighborhoodPreviewSystem.cs
│
├── Generators/
│   ├── OrganicGridGenerator.cs
│   ├── CurvilinearGenerator.cs
│   ├── CulDeSacGenerator.cs
│   ├── EuropeanStyleGenerator.cs
│   └── SuburbanGenerator.cs
│
├── Utils/
│   ├── PerlinNoise.cs
│   ├── CurveUtils.cs
│   ├── ValidationUtils.cs
│   └── LayoutUtils.cs
│
├── UI/
│   ├── NeighborhoodToolUI.cs
│   └── ParameterPanel.cs
│
├── Data/
│   └── LayoutParameters.cs
│
└── ModMain.cs
```

---

## TESTING STRATEGY

### Unit Tests

1. **Perlin Noise**: Verify smooth gradients
2. **Curve Generation**: Check bezier validity
3. **Validation**: Test slope/collision checks
4. **Node Snapping**: Verify deduplication

### Integration Tests

1. **Grid Generation**: Create 10×10 grid, verify counts
2. **Cul-de-Sac**: Generate layout, check connectivity
3. **Preview System**: Create/apply/cancel workflow
4. **Performance**: Measure generation time

### Gameplay Tests

1. **Flat Terrain**: Test all patterns
2. **Hilly Terrain**: Test terrain following
3. **Large Area**: 500m × 500m performance
4. **Small Area**: 50m × 50m edge cases
5. **User Workflow**: Full tool experience

---

## SUCCESS METRICS

### Quality Metrics

- **Variety**: No two layouts look identical
- **Realism**: Looks hand-placed, not algorithmic
- **Usability**: Fast, intuitive tool
- **Performance**: <100ms generation time
- **Playability**: Blocks zone properly, traffic flows

### Player Feedback

- "Feels like a real neighborhood"
- "Saves so much time vs. manual placement"
- "Love the variety and randomness"
- "Wish vanilla game had this"

---

## CONCLUSION

This organic neighborhood layout tool provides Cities: Skylines II players with a powerful, flexible system for creating realistic road networks. By combining the efficiency of the built-in grid tool with procedural variation algorithms, players can quickly generate unique, natural-looking neighborhoods that enhance gameplay and city aesthetics.

**Next Step**: Begin implementation Phase 1 - Core Tool Structure

---

**References**:
- `DETAILED_GRID_LAYOUT_ANALYSIS.md` - Grid tool deep dive
- `CITIES_SKYLINES_2_ROAD_ANALYSIS.md` - Network architecture
- `ecs-reference.md` - ECS patterns
- `tool.md` - Tool lifecycle
