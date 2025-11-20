using Unity.Mathematics;
using Unity.Burst;
using Colossal.Mathematics;
using OrganicNeighborhood.Data;

namespace OrganicNeighborhood.Utils
{
    /// <summary>
    /// Burst-compatible utilities for creating organic bezier curves
    /// </summary>
    [BurstCompile]
    public static class CurveUtils
    {
        /// <summary>
        /// Create an organic curved road using sine wave distortion
        /// </summary>
        /// <param name="start">Start position</param>
        /// <param name="end">End position</param>
        /// <param name="curveAmount">Curve strength (0-1)</param>
        /// <param name="seed">Random seed for variation</param>
        /// <returns>Curved bezier</returns>
        [BurstCompile]
        public static Bezier4x3 CreateOrganicCurve(
            float3 start,
            float3 end,
            float curveAmount,
            float seed = 0f)
        {
            float3 direction = math.normalize(end - start);
            float3 perpendicular = new float3(-direction.z, 0, direction.x);

            // Use Perlin to determine curve direction bias
            float curveBias = BurstPerlinNoise.GetCurveBias(start, seed);

            // Amplitude based on curve amount (max 10m deviation)
            float amplitude = curveAmount * 10f;

            Bezier4x3 curve;
            curve.a = start;
            curve.d = end;

            // Control points follow sine-like curve
            float3 midOffset1 = perpendicular * math.sin(0.33f * math.PI) * amplitude * curveBias;
            float3 midOffset2 = perpendicular * math.sin(0.67f * math.PI) * amplitude * curveBias;

            curve.b = math.lerp(start, end, 0.33f) + midOffset1;
            curve.c = math.lerp(start, end, 0.67f) + midOffset2;

            return curve;
        }

        /// <summary>
        /// Create a straight bezier curve (for comparison/fallback)
        /// </summary>
        /// <param name="start">Start position</param>
        /// <param name="end">End position</param>
        /// <returns>Straight bezier</returns>
        [BurstCompile]
        public static Bezier4x3 CreateStraightCurve(float3 start, float3 end)
        {
            Bezier4x3 curve;
            curve.a = start;
            curve.d = end;

            // Control points on straight line
            curve.b = math.lerp(start, end, 0.33f);
            curve.c = math.lerp(start, end, 0.67f);

            return curve;
        }

        /// <summary>
        /// Create a circular arc (for cul-de-sacs, roundabouts)
        /// </summary>
        /// <param name="start">Start position</param>
        /// <param name="end">End position</param>
        /// <param name="center">Arc center</param>
        /// <returns>Arc bezier approximation</returns>
        [BurstCompile]
        public static Bezier4x3 CreateArcCurve(
            float3 start,
            float3 end,
            float3 center)
        {
            // Calculate midpoint and offset for circular arc
            float3 midpoint = (start + end) * 0.5f;
            float3 toMid = math.normalize(midpoint - center);
            float radius = math.distance(start, center);

            // Bezier circle constant (approximate circle with cubic bezier)
            const float k = 0.55228475f;
            float3 controlOffset = toMid * radius * k;

            Bezier4x3 curve;
            curve.a = start;
            curve.b = start + (midpoint - start) * 0.33f + controlOffset * 0.5f;
            curve.c = end - (end - midpoint) * 0.33f + controlOffset * 0.5f;
            curve.d = end;

            return curve;
        }

        /// <summary>
        /// Create a curve with specific tangent constraints
        /// (useful for connecting to existing roads)
        /// </summary>
        /// <param name="start">Start position</param>
        /// <param name="startTangent">Start direction</param>
        /// <param name="endTangent">End direction</param>
        /// <param name="end">End position</param>
        /// <returns>Constrained bezier curve</returns>
        [BurstCompile]
        public static Bezier4x3 CreateConstrainedCurve(
            float3 start,
            float3 startTangent,
            float3 endTangent,
            float3 end)
        {
            float distance = math.distance(start, end);
            float controlPointDistance = distance * 0.33f;

            Bezier4x3 curve;
            curve.a = start;
            curve.b = start + math.normalize(startTangent) * controlPointDistance;
            curve.c = end - math.normalize(endTangent) * controlPointDistance;
            curve.d = end;

            return curve;
        }

        /// <summary>
        /// Apply smoothing to a curve by adjusting control points
        /// </summary>
        /// <param name="curve">Input curve</param>
        /// <param name="smoothFactor">Smoothing strength (0-1)</param>
        /// <returns>Smoothed curve</returns>
        [BurstCompile]
        public static Bezier4x3 SmoothCurve(Bezier4x3 curve, float smoothFactor)
        {
            // Move control points closer to average position
            float3 avgControlPoint = (curve.b + curve.c) * 0.5f;

            curve.b = math.lerp(curve.b, avgControlPoint, smoothFactor);
            curve.c = math.lerp(curve.c, avgControlPoint, smoothFactor);

            return curve;
        }

        /// <summary>
        /// Subdivide a curve into two curves at parameter t
        /// </summary>
        /// <param name="curve">Input curve</param>
        /// <param name="t">Subdivision parameter (0-1)</param>
        /// <param name="curve1">First half</param>
        /// <param name="curve2">Second half</param>
        [BurstCompile]
        public static void SubdivideCurve(
            Bezier4x3 curve,
            float t,
            out Bezier4x3 curve1,
            out Bezier4x3 curve2)
        {
            // De Casteljau's algorithm for curve subdivision
            float3 p01 = math.lerp(curve.a, curve.b, t);
            float3 p12 = math.lerp(curve.b, curve.c, t);
            float3 p23 = math.lerp(curve.c, curve.d, t);

            float3 p012 = math.lerp(p01, p12, t);
            float3 p123 = math.lerp(p12, p23, t);

            float3 p0123 = math.lerp(p012, p123, t);

            // First curve: a to split point
            curve1.a = curve.a;
            curve1.b = p01;
            curve1.c = p012;
            curve1.d = p0123;

            // Second curve: split point to d
            curve2.a = p0123;
            curve2.b = p123;
            curve2.c = p23;
            curve2.d = curve.d;
        }

        /// <summary>
        /// Calculate the approximate length of a bezier curve
        /// </summary>
        /// <param name="curve">Curve to measure</param>
        /// <param name="samples">Number of samples for approximation</param>
        /// <returns>Approximate length</returns>
        [BurstCompile]
        public static float CalculateCurveLength(Bezier4x3 curve, int samples = 20)
        {
            float length = 0f;
            float3 prevPoint = curve.a;

            for (int i = 1; i <= samples; i++)
            {
                float t = (float)i / samples;
                float3 point = MathUtils.Position(curve, t);

                length += math.distance(prevPoint, point);
                prevPoint = point;
            }

            return length;
        }

        /// <summary>
        /// Offset a curve perpendicular to its direction (for parallel roads)
        /// </summary>
        /// <param name="curve">Original curve</param>
        /// <param name="offset">Offset distance (positive = right, negative = left)</param>
        /// <returns>Offset curve</returns>
        [BurstCompile]
        public static Bezier4x3 OffsetCurve(Bezier4x3 curve, float offset)
        {
            // Calculate perpendiculars at control points
            float3 tangentStart = math.normalize(curve.b - curve.a);
            float3 tangentEnd = math.normalize(curve.d - curve.c);

            float3 perpStart = new float3(-tangentStart.z, 0, tangentStart.x);
            float3 perpEnd = new float3(-tangentEnd.z, 0, tangentEnd.x);

            // Offset control points
            Bezier4x3 offsetCurve;
            offsetCurve.a = curve.a + perpStart * offset;
            offsetCurve.b = curve.b + perpStart * offset;
            offsetCurve.c = curve.c + perpEnd * offset;
            offsetCurve.d = curve.d + perpEnd * offset;

            return offsetCurve;
        }

        /// <summary>
        /// Get tangent direction at curve parameter t
        /// </summary>
        /// <param name="curve">Curve</param>
        /// <param name="t">Parameter (0-1)</param>
        /// <returns>Tangent vector (not normalized)</returns>
        [BurstCompile]
        public static float3 GetTangent(Bezier4x3 curve, float t)
        {
            return MathUtils.Tangent(curve, t);
        }

        /// <summary>
        /// Get normalized tangent direction at curve parameter t
        /// </summary>
        /// <param name="curve">Curve</param>
        /// <param name="t">Parameter (0-1)</param>
        /// <returns>Normalized tangent vector</returns>
        [BurstCompile]
        public static float3 GetNormalizedTangent(Bezier4x3 curve, float t)
        {
            float3 tangent = MathUtils.Tangent(curve, t);
            return math.normalize(tangent);
        }

        /// <summary>
        /// Check if a curve is approximately straight
        /// </summary>
        /// <param name="curve">Curve to check</param>
        /// <param name="threshold">Maximum deviation to consider straight (meters)</param>
        /// <returns>True if curve is straight</returns>
        [BurstCompile]
        public static bool IsCurveStraight(Bezier4x3 curve, float threshold = 1f)
        {
            // Check if control points are close to the straight line
            float3 line = curve.d - curve.a;
            float lineLength = math.length(line);

            if (lineLength < 0.001f)
                return true;

            float3 lineDir = line / lineLength;

            // Project control points onto line
            float t1 = math.dot(curve.b - curve.a, lineDir);
            float t2 = math.dot(curve.c - curve.a, lineDir);

            float3 proj1 = curve.a + lineDir * t1;
            float3 proj2 = curve.a + lineDir * t2;

            // Calculate perpendicular distances
            float dist1 = math.distance(curve.b, proj1);
            float dist2 = math.distance(curve.c, proj2);

            return dist1 < threshold && dist2 < threshold;
        }
    }
}
