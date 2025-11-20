using Game.Simulation;
using OrganicNeighborhood.Data;
using OrganicNeighborhood.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace OrganicNeighborhood.Systems
{
    /// <summary>
    /// Burst-compiled job that applies terrain awareness to generated roads
    /// - Snaps roads to terrain height
    /// - Validates slopes and rejects too-steep segments
    /// - Detects and avoids water bodies
    /// - Adjusts positions to follow terrain contours
    /// </summary>
    [BurstCompile]
    public struct ApplyTerrainAwarenessJob : IJob
    {
        // ============ INPUT PARAMETERS ============

        /// <summary>Input roads from GenerateOrganicGridJob (read-only)</summary>
        [ReadOnly] public NativeArray<RoadDefinition> m_InputRoads;

        /// <summary>Terrain awareness configuration</summary>
        [ReadOnly] public TerrainAwareParameters m_TerrainParams;

        /// <summary>Terrain height data from TerrainSystem</summary>
        [ReadOnly] public TerrainHeightData m_TerrainHeightData;

        /// <summary>Water surface data from WaterSystem</summary>
        [ReadOnly] public WaterSurfaceData m_WaterSurfaceData;

        // ============ OUTPUT ============

        /// <summary>Terrain-aware roads (valid roads only, allocated by caller)</summary>
        public NativeList<RoadDefinition> m_OutputRoads;

        /// <summary>Statistics about terrain processing</summary>
        public NativeReference<TerrainStats> m_Stats;

        // ============ EXECUTION ============

        /// <summary>
        /// Execute the job - process all input roads and apply terrain awareness
        /// </summary>
        public void Execute()
        {
            TerrainStats stats = new TerrainStats();

            // Process each input road
            for (int i = 0; i < m_InputRoads.Length; i++)
            {
                RoadDefinition road = m_InputRoads[i];
                stats.m_TotalRoads++;

                // Step 1: Snap to terrain height
                if (m_TerrainParams.m_SnapToTerrain)
                {
                    road.m_Start = TerrainHelpers.SnapToTerrain(
                        road.m_Start, ref m_TerrainHeightData);
                    road.m_End = TerrainHelpers.SnapToTerrain(
                        road.m_End, ref m_TerrainHeightData);
                }

                // Step 2: Validate slope
                if (m_TerrainParams.m_ValidateSlope)
                {
                    bool slopeValid = TerrainHelpers.ValidateSlope(
                        road.m_Start,
                        road.m_End,
                        ref m_TerrainHeightData,
                        m_TerrainParams.m_MaxSlope,
                        samples: 10);

                    if (!slopeValid)
                    {
                        stats.m_RejectedBySlope++;
                        continue; // Skip this road - too steep
                    }
                }

                // Step 3: Check for water crossings
                if (m_TerrainParams.m_AvoidWater)
                {
                    bool crossesWater = CheckWaterCrossing(
                        road.m_Start,
                        road.m_End,
                        m_TerrainParams.m_MaxWaterDepth);

                    if (crossesWater)
                    {
                        stats.m_RejectedByWater++;
                        continue; // Skip this road - crosses deep water
                    }
                }

                // Step 4: Adjust curve to follow terrain (if curvy road)
                if (road.m_CurveAmount > 0.01f && m_TerrainParams.m_SnapToTerrain)
                {
                    // For curved roads, ensure the curve follows terrain elevation changes
                    // This is handled in Phase 5 when we create the actual Bezier curves
                    // For now, we just validate the endpoints
                }

                // Road passed all validation - add to output
                m_OutputRoads.Add(road);
                stats.m_ValidRoads++;

                // Track elevation statistics
                float elevation = (road.m_Start.y + road.m_End.y) * 0.5f;
                stats.m_MinElevation = math.min(stats.m_MinElevation, elevation);
                stats.m_MaxElevation = math.max(stats.m_MaxElevation, elevation);
            }

            // Write statistics
            m_Stats.Value = stats;
        }

        // ============ HELPER METHODS ============

        /// <summary>
        /// Check if a road segment crosses water deeper than the threshold
        /// Samples multiple points along the road path
        /// </summary>
        private bool CheckWaterCrossing(float3 start, float3 end, float maxDepth)
        {
            const int samples = 8; // Check 8 points along the road

            for (int i = 0; i <= samples; i++)
            {
                float t = (float)i / samples;
                float3 position = math.lerp(start, end, t);

                // Get water surface height at this position
                float waterHeight = WaterUtils.SampleHeight(
                    ref m_WaterSurfaceData,
                    ref m_TerrainHeightData,
                    position);

                // Get terrain height at this position
                float terrainHeight = TerrainUtils.SampleHeight(
                    ref m_TerrainHeightData,
                    position);

                // Calculate water depth
                float waterDepth = waterHeight - terrainHeight;

                // If water depth exceeds threshold, this is a water crossing
                if (waterDepth > maxDepth)
                {
                    return true;
                }
            }

            return false; // No significant water crossing
        }
    }

    /// <summary>
    /// Statistics about terrain processing for debugging and logging
    /// </summary>
    public struct TerrainStats
    {
        /// <summary>Total roads processed</summary>
        public int m_TotalRoads;

        /// <summary>Roads that passed all validation</summary>
        public int m_ValidRoads;

        /// <summary>Roads rejected due to excessive slope</summary>
        public int m_RejectedBySlope;

        /// <summary>Roads rejected due to water crossings</summary>
        public int m_RejectedByWater;

        /// <summary>Minimum elevation found (for logging)</summary>
        public float m_MinElevation;

        /// <summary>Maximum elevation found (for logging)</summary>
        public float m_MaxElevation;

        /// <summary>
        /// Initialize statistics with default values
        /// </summary>
        public static TerrainStats Create()
        {
            return new TerrainStats
            {
                m_TotalRoads = 0,
                m_ValidRoads = 0,
                m_RejectedBySlope = 0,
                m_RejectedByWater = 0,
                m_MinElevation = float.MaxValue,
                m_MaxElevation = float.MinValue
            };
        }
    }
}
