using Unity.Entities;
using Unity.Mathematics;

namespace OrganicNeighborhood.Data
{
    /// <summary>
    /// Layout style options
    /// </summary>
    public enum LayoutStyle : byte
    {
        OrganicGrid = 0,          // Traditional grid with natural variations
        Curvilinear = 1,          // Flowing curved streets
        CulDeSacResidential = 2,  // Hierarchical with dead-ends
        MixedDevelopment = 3,     // Combination of patterns
        EuropeanStyle = 4,        // Irregular radial
        Suburban = 5              // Wide lots, gentle curves
    }

    /// <summary>
    /// Parameters for organic neighborhood layout generation
    /// </summary>
    public struct LayoutParameters
    {
        // ===== ROAD TYPES =====
        public Entity m_ArterialPrefab;      // Wide main roads (20m)
        public Entity m_CollectorPrefab;     // Medium connector roads (12m)
        public Entity m_LocalPrefab;         // Narrow local streets (8m)

        // ===== SPACING =====
        public float m_RoadSpacing;          // Base road spacing (meters)
        public float m_ArterialSpacing;      // Arterial spacing (100-200m)
        public float m_CollectorSpacing;     // Collector spacing (50-80m)
        public float m_LocalSpacing;         // Local street spacing (30-50m)

        // ===== VARIATION (ORGANIC FEEL) =====
        public float m_PositionVariation;    // Vertex position variation (0-10m)
        public float m_SpacingVariation;     // Spacing variation percentage (0-0.2)
        public float m_AngleVariation;       // Angle variation in degrees (0-15°)
        public float m_CurveAmount;          // Curve strength (0-1, 0=straight, 1=very curved)

        // ===== LAYOUT STYLE =====
        public LayoutStyle m_Style;          // Layout pattern to use
        public int m_RadialCount;            // Number of radial roads (for radial layouts)
        public float m_CulDeSacProbability;  // Chance of cul-de-sac (0-1)

        // ===== BLOCK SIZE =====
        public float2 m_MinBlockSize;        // Minimum block dimensions (meters)
        public float2 m_MaxBlockSize;        // Maximum block dimensions (meters)

        // ===== DEFAULT VALUES =====
        public static LayoutParameters Default => new LayoutParameters
        {
            // Spacing
            m_RoadSpacing = 50f,
            m_ArterialSpacing = 150f,
            m_CollectorSpacing = 60f,
            m_LocalSpacing = 40f,

            // Variation
            m_PositionVariation = 5f,
            m_SpacingVariation = 0.15f,
            m_AngleVariation = 10f,
            m_CurveAmount = 0.3f,

            // Style
            m_Style = LayoutStyle.OrganicGrid,
            m_RadialCount = 6,
            m_CulDeSacProbability = 0.3f,

            // Block size
            m_MinBlockSize = new float2(30f, 30f),
            m_MaxBlockSize = new float2(100f, 80f),
        };
    }

    /// <summary>
    /// Terrain-aware parameters
    /// </summary>
    public struct TerrainAwareParameters
    {
        // ===== TERRAIN FOLLOWING =====
        public bool m_SnapToTerrain;         // Snap road vertices to terrain
        public int m_TerrainSamples;         // Samples per road segment (5-20)

        // ===== SLOPE VALIDATION =====
        public bool m_ValidateSlope;         // Check slopes
        public float m_MaxSlope;             // Max slope in degrees (typically 15°)
        public float m_PreferredSlope;       // Preferred slope in degrees (typically 5°)

        // ===== WATER HANDLING =====
        public bool m_AvoidWater;            // Avoid water bodies
        public float m_MaxWaterDepth;        // Max depth to cross (meters, typically 2m)
        public bool m_CreateBridges;         // Auto-create bridges over water

        // ===== ELEVATION ADAPTATION =====
        public float m_MaxElevationChange;   // Max elevation per segment (meters)
        public bool m_FollowContours;        // Follow terrain contours (switchbacks)

        // ===== ORGANIC VARIATION =====
        public bool m_UsePerlinVariation;    // Apply Perlin noise variation
        public float m_VariationStrength;    // Variation amount (meters)
        public float m_TerrainInfluence;     // How much terrain affects variation (0-1)

        // ===== DEFAULT VALUES =====
        public static TerrainAwareParameters Default => new TerrainAwareParameters
        {
            // Terrain
            m_SnapToTerrain = true,
            m_TerrainSamples = 10,

            // Slope
            m_ValidateSlope = true,
            m_MaxSlope = 15f,
            m_PreferredSlope = 5f,

            // Water
            m_AvoidWater = true,
            m_MaxWaterDepth = 2f,
            m_CreateBridges = false,

            // Elevation
            m_MaxElevationChange = 10f,
            m_FollowContours = false,

            // Variation
            m_UsePerlinVariation = true,
            m_VariationStrength = 5f,
            m_TerrainInfluence = 0.7f,
        };
    }

    /// <summary>
    /// Road definition for generation
    /// </summary>
    public struct RoadDefinition
    {
        public float3 m_Start;               // Start position (world space)
        public float3 m_End;                 // End position (world space)
        public float m_CurveAmount;          // Curve distortion (0-1)
        public RoadType m_Type;              // Road type (arterial/collector/local)
        public bool m_IsValid;               // Passed validation
    }

    /// <summary>
    /// Road hierarchy type
    /// </summary>
    public enum RoadType : byte
    {
        Local = 0,      // Narrow local streets
        Collector = 1,  // Medium connector roads
        Arterial = 2    // Wide main roads
    }

    /// <summary>
    /// Water crossing detection result
    /// </summary>
    public struct WaterCrossing
    {
        public bool m_HasWater;      // Water detected
        public float m_WaterStartT;  // Start of crossing (0-1 along curve)
        public float m_WaterEndT;    // End of crossing (0-1 along curve)
        public float m_MaxDepth;     // Maximum water depth encountered
        public float m_AvgDepth;     // Average water depth
    }
}
