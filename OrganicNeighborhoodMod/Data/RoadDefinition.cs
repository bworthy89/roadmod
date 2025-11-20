using Unity.Entities;
using Unity.Mathematics;

namespace OrganicNeighborhood.Data
{
    /// <summary>
    /// Represents a single road segment to be created
    /// Output from GenerateOrganicGridJob, input to NetCourse creation (Phase 5)
    /// </summary>
    public struct RoadDefinition
    {
        /// <summary>Start position of the road in world space</summary>
        public float3 m_Start;

        /// <summary>End position of the road in world space</summary>
        public float3 m_End;

        /// <summary>Road type/hierarchy for prefab selection</summary>
        public RoadType m_Type;

        /// <summary>Curve amount for organic paths (0 = straight, 1 = very curved)</summary>
        public float m_CurveAmount;

        /// <summary>Unique seed for this road's variation (for consistent Perlin noise)</summary>
        public float m_Seed;

        /// <summary>Optional: Road width override (negative = use prefab default)</summary>
        public float m_Width;

        /// <summary>
        /// Create a straight road definition
        /// </summary>
        public static RoadDefinition CreateStraight(float3 start, float3 end, RoadType type)
        {
            return new RoadDefinition
            {
                m_Start = start,
                m_End = end,
                m_Type = type,
                m_CurveAmount = 0f,
                m_Seed = 0f,
                m_Width = -1f
            };
        }

        /// <summary>
        /// Create an organic/curved road definition
        /// </summary>
        public static RoadDefinition CreateOrganic(
            float3 start,
            float3 end,
            RoadType type,
            float curveAmount,
            float seed)
        {
            return new RoadDefinition
            {
                m_Start = start,
                m_End = end,
                m_Type = type,
                m_CurveAmount = curveAmount,
                m_Seed = seed,
                m_Width = -1f
            };
        }

        /// <summary>
        /// Get the length of this road segment
        /// </summary>
        public float GetLength()
        {
            return math.distance(m_Start, m_End);
        }

        /// <summary>
        /// Get the direction vector (normalized) from start to end
        /// </summary>
        public float3 GetDirection()
        {
            return math.normalize(m_End - m_Start);
        }

        /// <summary>
        /// Get the midpoint of this road segment
        /// </summary>
        public float3 GetMidpoint()
        {
            return (m_Start + m_End) * 0.5f;
        }
    }

    /// <summary>
    /// Road hierarchy type for prefab selection
    /// Matches typical road network hierarchy in urban planning
    /// </summary>
    public enum RoadType : byte
    {
        /// <summary>Main roads connecting different areas (widest)</summary>
        Arterial = 0,

        /// <summary>Medium roads distributing traffic within an area</summary>
        Collector = 1,

        /// <summary>Small local/residential roads (narrowest)</summary>
        Local = 2,

        /// <summary>Dead-end roads in cul-de-sac developments</summary>
        CulDeSac = 3
    }
}
