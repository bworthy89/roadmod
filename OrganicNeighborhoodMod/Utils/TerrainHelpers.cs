using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Game.Simulation;
using Colossal.Mathematics;

namespace OrganicNeighborhood.Utils
{
    /// <summary>
    /// Burst-compatible terrain utilities for organic road generation
    /// </summary>
    [BurstCompile]
    public static class TerrainHelpers
    {
        /// <summary>
        /// Snap a position to terrain height
        /// </summary>
        /// <param name="position">World position (Y will be overwritten)</param>
        /// <param name="terrainData">Terrain height data</param>
        /// <returns>Position snapped to terrain</returns>
        [BurstCompile]
        public static float3 SnapToTerrain(
            float3 position,
            ref TerrainHeightData terrainData)
        {
            float height = TerrainUtils.SampleHeight(ref terrainData, position);
            position.y = height;
            return position;
        }

        /// <summary>
        /// Sample terrain height and normal at position
        /// </summary>
        /// <param name="position">World position</param>
        /// <param name="terrainData">Terrain height data</param>
        /// <param name="normal">Output terrain normal</param>
        /// <returns>Terrain height</returns>
        [BurstCompile]
        public static float SampleTerrainWithNormal(
            float3 position,
            ref TerrainHeightData terrainData,
            out float3 normal)
        {
            return TerrainUtils.SampleHeight(ref terrainData, position, out normal);
        }

        /// <summary>
        /// Calculate slope angle from terrain normal
        /// </summary>
        /// <param name="normal">Terrain normal vector</param>
        /// <returns>Slope angle in degrees</returns>
        [BurstCompile]
        public static float GetSlopeAngle(float3 normal)
        {
            // Clamp to avoid NaN from numerical errors
            float dotProduct = math.clamp(normal.y, 0f, 1f);
            return math.acos(dotProduct) * (180f / math.PI);
        }

        /// <summary>
        /// Validate that slope between two points is acceptable
        /// </summary>
        /// <param name="start">Start position</param>
        /// <param name="end">End position</param>
        /// <param name="terrainData">Terrain height data</param>
        /// <param name="maxSlopeAngle">Maximum acceptable slope in degrees</param>
        /// <param name="samples">Number of samples along path</param>
        /// <returns>True if all slopes are acceptable</returns>
        [BurstCompile]
        public static bool ValidateSlope(
            float3 start,
            float3 end,
            ref TerrainHeightData terrainData,
            float maxSlopeAngle,
            int samples = 10)
        {
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / (samples - 1);
                float3 position = math.lerp(start, end, t);

                // Get terrain normal
                TerrainUtils.SampleHeight(
                    ref terrainData,
                    position,
                    out float3 normal);

                // Calculate slope angle
                float slopeAngle = GetSlopeAngle(normal);

                if (slopeAngle > maxSlopeAngle)
                {
                    return false;  // Too steep
                }
            }

            return true;  // All slopes acceptable
        }

        /// <summary>
        /// Validate slope along a bezier curve
        /// </summary>
        /// <param name="curve">Bezier curve to validate</param>
        /// <param name="terrainData">Terrain height data</param>
        /// <param name="maxSlopeAngle">Maximum acceptable slope in degrees</param>
        /// <param name="samples">Number of samples along curve</param>
        /// <returns>True if all slopes are acceptable</returns>
        [BurstCompile]
        public static bool ValidateCurveSlope(
            Bezier4x3 curve,
            ref TerrainHeightData terrainData,
            float maxSlopeAngle,
            int samples = 20)
        {
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / (samples - 1);
                float3 position = MathUtils.Position(curve, t);

                // Get terrain normal
                TerrainUtils.SampleHeight(
                    ref terrainData,
                    position,
                    out float3 normal);

                // Calculate slope angle
                float slopeAngle = GetSlopeAngle(normal);

                if (slopeAngle > maxSlopeAngle)
                {
                    return false;  // Too steep
                }
            }

            return true;  // All slopes acceptable
        }

        /// <summary>
        /// Calculate average elevation change along path
        /// </summary>
        /// <param name="start">Start position</param>
        /// <param name="end">End position</param>
        /// <param name="terrainData">Terrain height data</param>
        /// <param name="samples">Number of samples</param>
        /// <returns>Average elevation change in meters</returns>
        [BurstCompile]
        public static float GetAverageElevationChange(
            float3 start,
            float3 end,
            ref TerrainHeightData terrainData,
            int samples = 10)
        {
            float totalChange = 0f;

            for (int i = 1; i < samples; i++)
            {
                float t0 = (float)(i - 1) / (samples - 1);
                float t1 = (float)i / (samples - 1);

                float3 pos0 = math.lerp(start, end, t0);
                float3 pos1 = math.lerp(start, end, t1);

                float height0 = TerrainUtils.SampleHeight(ref terrainData, pos0);
                float height1 = TerrainUtils.SampleHeight(ref terrainData, pos1);

                totalChange += math.abs(height1 - height0);
            }

            return totalChange / (samples - 1);
        }

        /// <summary>
        /// Create terrain-following bezier curve
        /// Samples terrain at multiple points and fits curve through them
        /// </summary>
        /// <param name="start">Start position (XZ, Y will be terrain-snapped)</param>
        /// <param name="end">End position (XZ, Y will be terrain-snapped)</param>
        /// <param name="terrainData">Terrain height data</param>
        /// <param name="samples">Number of terrain samples</param>
        /// <returns>Bezier curve following terrain</returns>
        [BurstCompile]
        public static Bezier4x3 CreateTerrainFollowingCurve(
            float3 start,
            float3 end,
            ref TerrainHeightData terrainData,
            int samples = 10)
        {
            // Allocate temporary array for sampled points
            NativeArray<float3> points = new NativeArray<float3>(samples, Allocator.Temp);

            // Sample terrain at multiple points
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / (samples - 1);
                float3 pos = math.lerp(start, end, t);

                // Snap to terrain
                pos.y = TerrainUtils.SampleHeight(ref terrainData, pos);

                points[i] = pos;
            }

            // Fit bezier curve through terrain-snapped points
            Bezier4x3 curve = FitCurveThroughPoints(points);

            points.Dispose();

            return curve;
        }

        /// <summary>
        /// Fit a bezier curve through sampled points
        /// Uses simple approach: endpoints + intermediate control points
        /// </summary>
        /// <param name="points">Sampled points (must have at least 4)</param>
        /// <returns>Fitted bezier curve</returns>
        [BurstCompile]
        private static Bezier4x3 FitCurveThroughPoints(NativeArray<float3> points)
        {
            Bezier4x3 curve;

            // Endpoints are exact
            curve.a = points[0];
            curve.d = points[points.Length - 1];

            // Control points from intermediate samples
            if (points.Length >= 4)
            {
                // Use points near 1/3 and 2/3
                int idx1 = points.Length / 3;
                int idx2 = (points.Length * 2) / 3;
                curve.b = points[idx1];
                curve.c = points[idx2];
            }
            else
            {
                // Fallback: interpolate
                curve.b = math.lerp(curve.a, curve.d, 0.33f);
                curve.c = math.lerp(curve.a, curve.d, 0.67f);
            }

            return curve;
        }

        /// <summary>
        /// Check if terrain is relatively flat in an area
        /// </summary>
        /// <param name="center">Center position</param>
        /// <param name="radius">Radius to check</param>
        /// <param name="terrainData">Terrain height data</param>
        /// <param name="maxSlopeAngle">Maximum slope to consider flat</param>
        /// <param name="samples">Number of samples</param>
        /// <returns>True if area is flat</returns>
        [BurstCompile]
        public static bool IsAreaFlat(
            float3 center,
            float radius,
            ref TerrainHeightData terrainData,
            float maxSlopeAngle = 5f,
            int samples = 8)
        {
            for (int i = 0; i < samples; i++)
            {
                float angle = (float)i / samples * 2f * math.PI;
                float3 offset = new float3(
                    math.cos(angle) * radius,
                    0,
                    math.sin(angle) * radius
                );

                float3 samplePos = center + offset;

                TerrainUtils.SampleHeight(
                    ref terrainData,
                    samplePos,
                    out float3 normal);

                float slope = GetSlopeAngle(normal);

                if (slope > maxSlopeAngle)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get height range in a bounding area
        /// </summary>
        /// <param name="min">Minimum corner</param>
        /// <param name="max">Maximum corner</param>
        /// <param name="terrainData">Terrain height data</param>
        /// <returns>Height range (min, max)</returns>
        [BurstCompile]
        public static float2 GetHeightRange(
            float3 min,
            float3 max,
            ref TerrainHeightData terrainData)
        {
            Bounds3 bounds = new Bounds3(min, max);
            Bounds1 heightRange = TerrainUtils.GetHeightRange(ref terrainData, bounds);
            return new float2(heightRange.min, heightRange.max);
        }
    }
}
