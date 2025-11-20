using OrganicNeighborhood.Data;
using OrganicNeighborhood.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace OrganicNeighborhood.Systems
{
    /// <summary>
    /// Burst-compiled job that generates organic neighborhood road layouts
    /// Applies Perlin noise for natural variation
    /// Supports multiple layout styles (OrganicGrid, Curvilinear, CulDeSac, etc.)
    /// </summary>
    [BurstCompile]
    public struct GenerateOrganicGridJob : IJob
    {
        // ============ INPUT PARAMETERS ============

        /// <summary>First corner of the parallelogram area (bottom-left typically)</summary>
        [ReadOnly] public float3 m_PointA;

        /// <summary>Second corner defining width direction (bottom-right typically)</summary>
        [ReadOnly] public float3 m_PointB;

        /// <summary>Third corner defining height direction (top-left typically)</summary>
        [ReadOnly] public float3 m_PointC;

        /// <summary>Layout configuration parameters</summary>
        [ReadOnly] public LayoutParameters m_Parameters;

        // ============ OUTPUT ============

        /// <summary>Generated road segments (allocated by caller)</summary>
        public NativeList<RoadDefinition> m_GeneratedRoads;

        // ============ EXECUTION ============

        /// <summary>
        /// Execute the job - generates all roads for the defined area
        /// </summary>
        public void Execute()
        {
            // Calculate area dimensions and vectors
            float3 vectorAB = m_PointB - m_PointA;  // Width vector
            float3 vectorAC = m_PointC - m_PointA;  // Height vector
            float width = math.length(vectorAB);
            float height = math.length(vectorAC);

            // Normalize direction vectors
            float3 directionX = math.normalize(vectorAB);
            float3 directionY = math.normalize(vectorAC);

            // Calculate fourth corner to complete parallelogram
            float3 pointD = m_PointA + vectorAB + vectorAC;

            // Dispatch to appropriate layout generator based on style
            switch (m_Parameters.m_Style)
            {
                case LayoutStyle.OrganicGrid:
                    GenerateOrganicGrid(width, height, directionX, directionY);
                    break;

                case LayoutStyle.Curvilinear:
                    GenerateCurvilinear(width, height, directionX, directionY);
                    break;

                case LayoutStyle.CulDeSacResidential:
                    GenerateCulDeSac(width, height, directionX, directionY);
                    break;

                case LayoutStyle.EuropeanStyle:
                    GenerateEuropean(width, height, directionX, directionY);
                    break;

                case LayoutStyle.Suburban:
                    GenerateSuburban(width, height, directionX, directionY);
                    break;

                case LayoutStyle.MixedDevelopment:
                    GenerateMixed(width, height, directionX, directionY);
                    break;

                default:
                    GenerateOrganicGrid(width, height, directionX, directionY);
                    break;
            }
        }

        // ============ LAYOUT GENERATORS ============

        /// <summary>
        /// Generate organic grid layout - standard grid with Perlin variation
        /// Creates intersecting horizontal and vertical roads with organic offsets
        /// </summary>
        private void GenerateOrganicGrid(float width, float height, float3 dirX, float3 dirY)
        {
            // Calculate road counts
            int roadsX = (int)(width / m_Parameters.m_RoadSpacing);
            int roadsY = (int)(height / m_Parameters.m_RoadSpacing);

            // Ensure at least 2 roads in each direction
            roadsX = math.max(2, roadsX);
            roadsY = math.max(2, roadsY);

            // Generate horizontal roads (running along X direction)
            for (int y = 0; y <= roadsY; y++)
            {
                // Base position along Y axis
                float tY = (float)y / roadsY;

                // Apply Perlin variation to Y position
                float noiseY = BurstPerlinNoise.Perlin2D(new float2(0f, y * 10f) * 0.1f);
                float offsetY = (noiseY - 0.5f) * 2f * m_Parameters.m_PositionVariation;

                // Clamp to keep roads within bounds
                offsetY = math.clamp(offsetY, -m_Parameters.m_RoadSpacing * 0.3f, m_Parameters.m_RoadSpacing * 0.3f);

                // Calculate actual Y position
                float actualY = tY * height + offsetY;
                actualY = math.clamp(actualY, 0f, height);

                // Calculate start and end points
                float3 start = m_PointA + dirY * actualY;
                float3 end = m_PointA + dirX * width + dirY * actualY;

                // Apply position variation to endpoints
                start = ApplyPositionVariation(start, y * 1000f);
                end = ApplyPositionVariation(end, y * 1000f + 500f);

                // Create road definition
                float seed = y * 123.456f;
                m_GeneratedRoads.Add(RoadDefinition.CreateOrganic(
                    start,
                    end,
                    DetermineRoadType(y, roadsY, true),
                    m_Parameters.m_CurveAmount,
                    seed
                ));
            }

            // Generate vertical roads (running along Y direction)
            for (int x = 0; x <= roadsX; x++)
            {
                // Base position along X axis
                float tX = (float)x / roadsX;

                // Apply Perlin variation to X position
                float noiseX = BurstPerlinNoise.Perlin2D(new float2(x * 10f, 0f) * 0.1f);
                float offsetX = (noiseX - 0.5f) * 2f * m_Parameters.m_PositionVariation;

                // Clamp to keep roads within bounds
                offsetX = math.clamp(offsetX, -m_Parameters.m_RoadSpacing * 0.3f, m_Parameters.m_RoadSpacing * 0.3f);

                // Calculate actual X position
                float actualX = tX * width + offsetX;
                actualX = math.clamp(actualX, 0f, width);

                // Calculate start and end points
                float3 start = m_PointA + dirX * actualX;
                float3 end = m_PointA + dirX * actualX + dirY * height;

                // Apply position variation to endpoints
                start = ApplyPositionVariation(start, x * 2000f);
                end = ApplyPositionVariation(end, x * 2000f + 500f);

                // Create road definition
                float seed = x * 234.567f;
                m_GeneratedRoads.Add(RoadDefinition.CreateOrganic(
                    start,
                    end,
                    DetermineRoadType(x, roadsX, false),
                    m_Parameters.m_CurveAmount,
                    seed
                ));
            }
        }

        /// <summary>
        /// Generate curvilinear layout - flowing curved roads
        /// Creates organic curved paths that follow terrain naturally
        /// </summary>
        private void GenerateCurvilinear(float width, float height, float3 dirX, float3 dirY)
        {
            int roadCount = (int)((width + height) / (m_Parameters.m_RoadSpacing * 2f));
            roadCount = math.max(3, roadCount);

            // Generate main curved arterials
            for (int i = 0; i <= roadCount; i++)
            {
                float t = (float)i / roadCount;

                // Curve across the area diagonally with organic variation
                float3 start = m_PointA + dirY * (t * height);
                float3 end = m_PointA + dirX * width + dirY * (t * height * 0.7f + height * 0.3f);

                // Add significant curve for curvilinear style
                float curveAmount = m_Parameters.m_CurveAmount * 2f; // Double curve for this style

                // Apply heavy position variation
                start = ApplyPositionVariation(start, i * 3000f, m_Parameters.m_PositionVariation * 1.5f);
                end = ApplyPositionVariation(end, i * 3000f + 1000f, m_Parameters.m_PositionVariation * 1.5f);

                m_GeneratedRoads.Add(RoadDefinition.CreateOrganic(
                    start,
                    end,
                    i == 0 || i == roadCount ? RoadType.Arterial : RoadType.Collector,
                    curveAmount,
                    i * 345.678f
                ));
            }

            // Add perpendicular connectors
            int connectorCount = roadCount / 2;
            for (int i = 1; i < connectorCount; i++)
            {
                float t = (float)i / connectorCount;

                float3 start = m_PointA + dirX * (t * width);
                float3 end = m_PointA + dirX * (t * width * 0.8f) + dirY * height;

                start = ApplyPositionVariation(start, i * 4000f, m_Parameters.m_PositionVariation);
                end = ApplyPositionVariation(end, i * 4000f + 1000f, m_Parameters.m_PositionVariation);

                m_GeneratedRoads.Add(RoadDefinition.CreateOrganic(
                    start,
                    end,
                    RoadType.Local,
                    m_Parameters.m_CurveAmount * 1.5f,
                    i * 456.789f
                ));
            }
        }

        /// <summary>
        /// Generate cul-de-sac layout - hierarchical with dead-end streets
        /// Creates main collector roads with branching local roads ending in cul-de-sacs
        /// </summary>
        private void GenerateCulDeSac(float width, float height, float3 dirX, float3 dirY)
        {
            // Main spine roads (collectors)
            int spineCount = math.max(2, (int)(width / (m_Parameters.m_RoadSpacing * 3f)));

            for (int i = 0; i <= spineCount; i++)
            {
                float t = (float)i / spineCount;
                float xPos = t * width;

                float3 start = m_PointA + dirX * xPos;
                float3 end = m_PointA + dirX * xPos + dirY * height;

                start = ApplyPositionVariation(start, i * 5000f);
                end = ApplyPositionVariation(end, i * 5000f + 1000f);

                m_GeneratedRoads.Add(RoadDefinition.CreateOrganic(
                    start,
                    end,
                    i == 0 || i == spineCount ? RoadType.Arterial : RoadType.Collector,
                    m_Parameters.m_CurveAmount * 0.5f,
                    i * 567.891f
                ));

                // Add branching cul-de-sac roads
                if (i < spineCount)
                {
                    int branchCount = 4;
                    for (int b = 0; b < branchCount; b++)
                    {
                        float branchT = (float)(b + 1) / (branchCount + 1);
                        float3 branchStart = start + dirY * (branchT * height);

                        // Branch length (shorter than main roads)
                        float branchLength = m_Parameters.m_RoadSpacing * 1.5f;

                        // Alternate left and right
                        float direction = (b % 2 == 0) ? 1f : -1f;
                        float3 branchEnd = branchStart + dirX * (branchLength * direction);

                        branchStart = ApplyPositionVariation(branchStart, (i * 100 + b) * 6000f);
                        branchEnd = ApplyPositionVariation(branchEnd, (i * 100 + b) * 6000f + 500f);

                        m_GeneratedRoads.Add(RoadDefinition.CreateOrganic(
                            branchStart,
                            branchEnd,
                            RoadType.CulDeSac,
                            m_Parameters.m_CurveAmount * 0.7f,
                            (i * 100 + b) * 678.912f
                        ));
                    }
                }
            }
        }

        /// <summary>
        /// Generate European-style layout - radial/irregular with plaza spaces
        /// Creates organic road network with varied angles and some curved segments
        /// </summary>
        private void GenerateEuropean(float width, float height, float3 dirX, float3 dirY)
        {
            // Create center point (plaza area)
            float3 center = m_PointA + dirX * (width * 0.5f) + dirY * (height * 0.5f);
            center = ApplyPositionVariation(center, 9999f);

            // Radial roads from center
            int radialCount = 6;
            for (int i = 0; i < radialCount; i++)
            {
                float angle = (float)i / radialCount * math.PI * 2f;

                // Calculate direction
                float3 radialDir = math.normalize(
                    dirX * math.cos(angle) + dirY * math.sin(angle));

                // Varying lengths for irregular pattern
                float noiseLength = BurstPerlinNoise.Perlin2D(new float2(i * 5f, 0f) * 0.2f);
                float length = math.min(width, height) * 0.4f * (0.7f + noiseLength * 0.6f);

                float3 end = center + radialDir * length;
                end = ApplyPositionVariation(end, i * 7000f);

                m_GeneratedRoads.Add(RoadDefinition.CreateOrganic(
                    center,
                    end,
                    RoadType.Arterial,
                    m_Parameters.m_CurveAmount * 0.8f,
                    i * 789.123f
                ));
            }

            // Concentric rings (partial, irregular)
            int ringCount = 2;
            for (int ring = 1; ring <= ringCount; ring++)
            {
                float ringRadius = (float)ring / (ringCount + 1) * math.min(width, height) * 0.4f;

                // Create segments around the ring (not complete circles)
                int segmentCount = 8;
                for (int seg = 0; seg < segmentCount; seg++)
                {
                    float angle1 = (float)seg / segmentCount * math.PI * 2f;
                    float angle2 = (float)(seg + 1) / segmentCount * math.PI * 2f;

                    float3 dir1 = math.normalize(dirX * math.cos(angle1) + dirY * math.sin(angle1));
                    float3 dir2 = math.normalize(dirX * math.cos(angle2) + dirY * math.sin(angle2));

                    float3 p1 = center + dir1 * ringRadius;
                    float3 p2 = center + dir2 * ringRadius;

                    p1 = ApplyPositionVariation(p1, (ring * 100 + seg) * 8000f);
                    p2 = ApplyPositionVariation(p2, (ring * 100 + seg) * 8000f + 500f);

                    // Skip some segments for irregular pattern
                    if (seg % 3 != 0)
                    {
                        m_GeneratedRoads.Add(RoadDefinition.CreateOrganic(
                            p1,
                            p2,
                            RoadType.Collector,
                            m_Parameters.m_CurveAmount * 1.2f,
                            (ring * 100 + seg) * 891.234f
                        ));
                    }
                }
            }
        }

        /// <summary>
        /// Generate suburban layout - wider spacing, gentle curves
        /// Creates spacious grid with wide roads and gentle organic variation
        /// </summary>
        private void GenerateSuburban(float width, float height, float3 dirX, float3 dirY)
        {
            // Use wider spacing for suburban feel
            float suburbanSpacing = m_Parameters.m_RoadSpacing * 1.5f;

            int roadsX = math.max(2, (int)(width / suburbanSpacing));
            int roadsY = math.max(2, (int)(height / suburbanSpacing));

            // Horizontal roads with gentle curves
            for (int y = 0; y <= roadsY; y++)
            {
                float tY = (float)y / roadsY;
                float actualY = tY * height;

                float3 start = m_PointA + dirY * actualY;
                float3 end = m_PointA + dirX * width + dirY * actualY;

                // Gentle variation
                start = ApplyPositionVariation(start, y * 10000f, m_Parameters.m_PositionVariation * 0.7f);
                end = ApplyPositionVariation(end, y * 10000f + 1000f, m_Parameters.m_PositionVariation * 0.7f);

                m_GeneratedRoads.Add(RoadDefinition.CreateOrganic(
                    start,
                    end,
                    y == 0 || y == roadsY ? RoadType.Arterial : RoadType.Collector,
                    m_Parameters.m_CurveAmount * 0.6f, // Gentle curves
                    y * 912.345f
                ));
            }

            // Vertical roads
            for (int x = 0; x <= roadsX; x++)
            {
                float tX = (float)x / roadsX;
                float actualX = tX * width;

                float3 start = m_PointA + dirX * actualX;
                float3 end = m_PointA + dirX * actualX + dirY * height;

                start = ApplyPositionVariation(start, x * 11000f, m_Parameters.m_PositionVariation * 0.7f);
                end = ApplyPositionVariation(end, x * 11000f + 1000f, m_Parameters.m_PositionVariation * 0.7f);

                m_GeneratedRoads.Add(RoadDefinition.CreateOrganic(
                    start,
                    end,
                    x == 0 || x == roadsX ? RoadType.Arterial : RoadType.Collector,
                    m_Parameters.m_CurveAmount * 0.6f,
                    x * 123.456f
                ));
            }
        }

        /// <summary>
        /// Generate mixed development layout - combines grid + organic elements
        /// Blends regular grid structure with curvilinear features
        /// </summary>
        private void GenerateMixed(float width, float height, float3 dirX, float3 dirY)
        {
            // Start with a basic grid (60% of roads)
            int roadsX = (int)(width / m_Parameters.m_RoadSpacing);
            int roadsY = (int)(height / m_Parameters.m_RoadSpacing);

            roadsX = math.max(2, roadsX);
            roadsY = math.max(2, roadsY);

            // Main grid roads (every other line)
            for (int y = 0; y <= roadsY; y += 2)
            {
                float tY = (float)y / roadsY;
                float3 start = m_PointA + dirY * (tY * height);
                float3 end = m_PointA + dirX * width + dirY * (tY * height);

                start = ApplyPositionVariation(start, y * 12000f);
                end = ApplyPositionVariation(end, y * 12000f + 1000f);

                m_GeneratedRoads.Add(RoadDefinition.CreateOrganic(
                    start,
                    end,
                    RoadType.Arterial,
                    m_Parameters.m_CurveAmount * 0.5f,
                    y * 234.567f
                ));
            }

            for (int x = 0; x <= roadsX; x += 2)
            {
                float tX = (float)x / roadsX;
                float3 start = m_PointA + dirX * (tX * width);
                float3 end = m_PointA + dirX * (tX * width) + dirY * height;

                start = ApplyPositionVariation(start, x * 13000f);
                end = ApplyPositionVariation(end, x * 13000f + 1000f);

                m_GeneratedRoads.Add(RoadDefinition.CreateOrganic(
                    start,
                    end,
                    RoadType.Arterial,
                    m_Parameters.m_CurveAmount * 0.5f,
                    x * 345.678f
                ));
            }

            // Add organic curved connectors between grid
            int curvedCount = roadsX + roadsY;
            for (int i = 0; i < curvedCount; i++)
            {
                float t = (float)i / curvedCount;

                // Diagonal curved roads
                float3 start = m_PointA + dirX * (t * width * 0.8f);
                float3 end = m_PointA + dirX * ((t + 0.3f) * width) + dirY * (height * 0.7f);

                start = ApplyPositionVariation(start, i * 14000f, m_Parameters.m_PositionVariation * 1.2f);
                end = ApplyPositionVariation(end, i * 14000f + 1000f, m_Parameters.m_PositionVariation * 1.2f);

                m_GeneratedRoads.Add(RoadDefinition.CreateOrganic(
                    start,
                    end,
                    RoadType.Local,
                    m_Parameters.m_CurveAmount * 1.5f, // More curved
                    i * 456.789f
                ));
            }
        }

        // ============ HELPER METHODS ============

        /// <summary>
        /// Apply Perlin-based position variation to a point
        /// </summary>
        private float3 ApplyPositionVariation(float3 position, float seed, float strength = -1f)
        {
            if (strength < 0f)
            {
                strength = m_Parameters.m_PositionVariation;
            }

            if (strength <= 0.001f)
            {
                return position; // No variation
            }

            return BurstPerlinNoise.ApplyOrganicVariation(
                position,
                strength,
                0.1f, // Scale
                seed
            );
        }

        /// <summary>
        /// Determine road type based on position in grid (edges = arterial, middle = local)
        /// </summary>
        private RoadType DetermineRoadType(int index, int total, bool isHorizontal)
        {
            // Edges are arterials
            if (index == 0 || index == total)
            {
                return RoadType.Arterial;
            }

            // Middle roads are collectors or local
            // Every 3rd road is a collector
            if (index % 3 == 0)
            {
                return RoadType.Collector;
            }

            return RoadType.Local;
        }
    }
}
