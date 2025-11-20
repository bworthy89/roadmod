using Unity.Mathematics;
using Unity.Burst;

namespace OrganicNeighborhood.Utils
{
    /// <summary>
    /// Burst-compatible Perlin noise implementation for organic variation
    /// </summary>
    [BurstCompile]
    public static class BurstPerlinNoise
    {
        /// <summary>
        /// 2D Perlin noise - returns value between 0 and 1
        /// </summary>
        /// <param name="position">Input position (scaled by caller)</param>
        /// <returns>Noise value [0, 1]</returns>
        [BurstCompile]
        public static float Perlin2D(float2 position)
        {
            // Integer and fractional parts
            float2 i = math.floor(position);
            float2 f = math.frac(position);

            // Smooth interpolation (smoothstep)
            float2 u = f * f * (3.0f - 2.0f * f);

            // Hash corners
            float a = Hash2D(i);
            float b = Hash2D(i + new float2(1, 0));
            float c = Hash2D(i + new float2(0, 1));
            float d = Hash2D(i + new float2(1, 1));

            // Bilinear interpolation
            return math.lerp(
                math.lerp(a, b, u.x),
                math.lerp(c, d, u.x),
                u.y
            );
        }

        /// <summary>
        /// Simple 2D hash function for Perlin noise
        /// Based on: https://www.shadertoy.com/view/4djSRW
        /// </summary>
        /// <param name="p">Input position</param>
        /// <returns>Hash value [0, 1]</returns>
        [BurstCompile]
        private static float Hash2D(float2 p)
        {
            // Hash constants (prime-like numbers for good distribution)
            const float k1 = 0.1031f;
            const float k2 = 0.1030f;
            const float k3 = 0.0973f;

            float3 p3 = math.frac(new float3(p.x, p.y, p.x) * k1);
            p3 += math.dot(p3, p3.yzx + 33.33f);

            return math.frac((p3.x + p3.y) * p3.z);
        }

        /// <summary>
        /// Fractal Perlin (multiple octaves for more detail)
        /// </summary>
        /// <param name="position">Input position</param>
        /// <param name="octaves">Number of octaves (detail levels)</param>
        /// <returns>Fractal noise value [0, 1]</returns>
        [BurstCompile]
        public static float FractalPerlin2D(float2 position, int octaves = 3)
        {
            float result = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxValue = 0f;

            for (int i = 0; i < octaves; i++)
            {
                result += Perlin2D(position * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }

            // Normalize to 0-1
            return result / maxValue;
        }

        /// <summary>
        /// Apply organic variation to a 3D position
        /// Uses Perlin noise to offset position in XZ plane
        /// </summary>
        /// <param name="basePosition">Original position</param>
        /// <param name="strength">Variation strength in meters</param>
        /// <param name="scale">Noise frequency (lower = smoother)</param>
        /// <returns>Varied position</returns>
        [BurstCompile]
        public static float3 ApplyOrganicVariation(
            float3 basePosition,
            float strength,
            float scale = 0.1f)
        {
            float2 noiseInput = basePosition.xz * scale;

            // Sample noise for X and Z offsets
            float noiseX = Perlin2D(noiseInput);
            float noiseZ = Perlin2D(noiseInput + new float2(100f, 100f));

            // Center around 0 (noise is 0-1, so subtract 0.5 and multiply by 2)
            noiseX = (noiseX - 0.5f) * 2f;
            noiseZ = (noiseZ - 0.5f) * 2f;

            // Apply variation
            return basePosition + new float3(noiseX, 0, noiseZ) * strength;
        }

        /// <summary>
        /// Apply organic variation with terrain influence
        /// More variation on slopes, less on flat areas
        /// </summary>
        /// <param name="basePosition">Original position</param>
        /// <param name="terrainNormal">Terrain normal at position</param>
        /// <param name="strength">Base variation strength</param>
        /// <param name="terrainInfluence">How much terrain affects variation [0-1]</param>
        /// <param name="scale">Noise frequency</param>
        /// <returns>Varied position</returns>
        [BurstCompile]
        public static float3 ApplyTerrainInfluencedVariation(
            float3 basePosition,
            float3 terrainNormal,
            float strength,
            float terrainInfluence,
            float scale = 0.05f)
        {
            // Calculate slope (0 = flat, 1 = vertical)
            float slope = 1f - terrainNormal.y;

            // Adjust strength based on slope
            // More variation on slopes (follow terrain)
            // Less variation on flat areas (stay straight)
            float adjustedStrength = math.lerp(
                strength,
                strength * (1f + slope * 2f),
                terrainInfluence
            );

            // Apply Perlin variation
            float2 noiseInput = basePosition.xz * scale;
            float noiseX = (Perlin2D(noiseInput) - 0.5f) * 2f;
            float noiseZ = (Perlin2D(noiseInput + 100f) - 0.5f) * 2f;

            // Calculate slope direction
            float3 slopeDirection = math.normalize(new float3(terrainNormal.x, 0, terrainNormal.z));
            float3 perpendicular = new float3(-slopeDirection.z, 0, slopeDirection.x);

            // Blend Perlin with slope-aware offset
            float3 variation = new float3(noiseX, 0, noiseZ) * adjustedStrength;
            variation += perpendicular * slope * strength * terrainInfluence;

            return basePosition + variation;
        }

        /// <summary>
        /// Create organic curve amount based on position
        /// Returns a value suitable for curve distortion
        /// </summary>
        /// <param name="position">Road position</param>
        /// <param name="seed">Random seed for variation</param>
        /// <returns>Curve bias [-1, 1]</returns>
        [BurstCompile]
        public static float GetCurveBias(float3 position, float seed = 0f)
        {
            float noise = Perlin2D(position.xz * 0.01f + seed);
            return (noise - 0.5f) * 2f;  // Convert to [-1, 1]
        }
    }
}
