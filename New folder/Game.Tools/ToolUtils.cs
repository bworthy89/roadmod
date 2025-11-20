using Colossal.Mathematics;
using Game.Net;
using Unity.Collections;
using Unity.Mathematics;

namespace Game.Tools;

public static class ToolUtils
{
	public const float WATER_DEPTH_LIMIT = 0.2f;

	public const int MAX_ACTIVE_INFOMODES = 100;

	public const int INFOMODE_COLOR_GROUP_COUNT = 3;

	public const int INFOMODE_COLOR_GROUP_SIZE = 4;

	public const int INFOMODE_COLOR_GROUP_TERRAIN = 0;

	public const int INFOMODE_COLOR_GROUP_WATER = 1;

	public const int INFOMODE_COLOR_GROUP_OTHER = 2;

	public static quaternion CalculateRotation(float2 direction)
	{
		if (direction.Equals(default(float2)))
		{
			return quaternion.identity;
		}
		return quaternion.LookRotation(new float3(direction.x, 0f, direction.y), math.up());
	}

	public static float2 CalculateSnapPriority(float level, float priority, float heightWeight, float3 origPos, float3 newPos, float2 direction)
	{
		float3 @float = newPos - origPos;
		@float = new float3(math.dot(@float.xz, direction), @float.y, math.dot(@float.xz, MathUtils.Right(direction)));
		return CalculateSnapPriority(level, priority, heightWeight, @float);
	}

	public static float2 CalculateSnapPriority(float level, float priority, float heightWeight, float3 offset)
	{
		offset /= 8f;
		offset *= offset;
		float num = math.min(1f, offset.x + offset.z);
		float num2 = math.max(offset.x, offset.z) + math.min(offset.x, offset.z) * 0.001f;
		return new float2(level, priority * (2f - num - num2) / (1f + offset.y * heightWeight));
	}

	public static bool CompareSnapPriority(float2 priority, float2 other)
	{
		bool2 @bool = priority > other;
		return @bool.x | (@bool.y & (priority.x == other.x));
	}

	public static void AddSnapLine(ref ControlPoint bestSnapPosition, NativeList<SnapLine> snapLines, SnapLine snapLine)
	{
		for (int i = 0; i < snapLines.Length; i++)
		{
			SnapLine snapLine2 = snapLines[i];
			if (math.abs(math.dot(snapLine.m_ControlPoint.m_Direction, snapLine2.m_ControlPoint.m_Direction)) > 0.999999f)
			{
				continue;
			}
			Line2 line = new Line2(snapLine.m_ControlPoint.m_Position.xz, snapLine.m_ControlPoint.m_Position.xz + snapLine.m_ControlPoint.m_Direction);
			Line2 line2 = new Line2(snapLine2.m_ControlPoint.m_Position.xz, snapLine2.m_ControlPoint.m_Position.xz + snapLine2.m_ControlPoint.m_Direction);
			if (MathUtils.Intersect(line, line2, out var t))
			{
				SnapLine snapLine3;
				if (snapLine.m_ControlPoint.m_SnapPriority.x >= snapLine2.m_ControlPoint.m_SnapPriority.x)
				{
					snapLine3 = snapLine;
					snapLine3.m_ControlPoint.m_Position.xz += snapLine.m_ControlPoint.m_Direction * t.x;
				}
				else
				{
					snapLine3 = snapLine2;
					snapLine3.m_ControlPoint.m_Position.xz += snapLine2.m_ControlPoint.m_Direction * t.y;
				}
				if (snapLine.m_HeightWeight != snapLine2.m_HeightWeight)
				{
					snapLine3.m_ControlPoint.m_Position.y = math.select(snapLine.m_ControlPoint.m_Position.y, snapLine2.m_ControlPoint.m_Position.y, snapLine2.m_HeightWeight > snapLine.m_HeightWeight);
				}
				if ((snapLine3.m_Flags & SnapLineFlags.ExtendedCurve) != 0)
				{
					NetUtils.ExtendedDistance(snapLine3.m_Curve.xz, snapLine3.m_ControlPoint.m_Position.xz, out var t2);
					float value = NetUtils.ExtendedLength(snapLine3.m_Curve.xz, t2);
					value = MathUtils.Snap(value, 4f);
					snapLine3.m_ControlPoint.m_CurvePosition = NetUtils.ExtendedClampLength(snapLine3.m_Curve.xz, value);
				}
				float level = math.max(snapLine.m_ControlPoint.m_SnapPriority.x, snapLine2.m_ControlPoint.m_SnapPriority.x);
				float heightWeight = math.max(snapLine.m_HeightWeight, snapLine2.m_HeightWeight);
				snapLine3.m_ControlPoint.m_SnapPriority = CalculateSnapPriority(level, 2f, heightWeight, snapLine3.m_ControlPoint.m_HitPosition, snapLine3.m_ControlPoint.m_Position, snapLine3.m_ControlPoint.m_Direction);
				AddSnapPosition(ref bestSnapPosition, snapLine3.m_ControlPoint);
			}
		}
		snapLines.Add(in snapLine);
	}

	public static void AddSnapPosition(ref ControlPoint bestSnapPosition, ControlPoint snapPosition)
	{
		if (CompareSnapPriority(snapPosition.m_SnapPriority, bestSnapPosition.m_SnapPriority))
		{
			bestSnapPosition = snapPosition;
		}
	}

	public static void DirectionSnap(ref float bestDirectionDistance, ref float3 resultPos, ref float3 resultDir, float3 refPos, float3 snapOrig, float3 snapDir, float snapDistance)
	{
		float3 @float = default(float3);
		float3 float2 = default(float3);
		@float.xz = snapDir.xz;
		float2.xz = MathUtils.Right(snapDir.xz);
		Line3 line = new Line3(snapOrig, snapOrig + @float);
		Line3 line2 = new Line3(snapOrig, snapOrig + float2);
		float t;
		float num = MathUtils.Distance(line.xz, refPos.xz, out t);
		float t2;
		float num2 = MathUtils.Distance(line2.xz, refPos.xz, out t2);
		if (num < bestDirectionDistance)
		{
			bestDirectionDistance = num;
			if (num < snapDistance)
			{
				resultDir = math.select(@float, -@float, t < 0f);
				resultPos.xz = MathUtils.Position(line.xz, t);
			}
		}
		if (num2 < bestDirectionDistance)
		{
			bestDirectionDistance = num2;
			if (num2 < snapDistance)
			{
				resultDir = math.select(float2, -float2, t2 < 0f);
				resultPos.xz = MathUtils.Position(line2.xz, t2);
			}
		}
	}

	public static Bounds2 GetBounds(Brush brush)
	{
		quaternion q = quaternion.RotateY(brush.m_Angle);
		float2 xz = math.mul(q, new float3(brush.m_Size * 0.5f, 0f, 0f)).xz;
		float2 xz2 = math.mul(q, new float3(0f, 0f, brush.m_Size * 0.5f)).xz;
		float2 @float = math.abs(xz) + math.abs(xz2);
		return new Bounds2(brush.m_Position.xz - @float, brush.m_Position.xz + @float);
	}

	public static float GetRandomAge(ref Random random, AgeMask ageMask)
	{
		int num = random.NextInt(math.countbits((int)ageMask));
		for (int i = 0; i < 4; i++)
		{
			AgeMask ageMask2 = (AgeMask)(1 << i);
			if ((ageMask & ageMask2) != 0 && num-- == 0)
			{
				switch (ageMask2)
				{
				case AgeMask.Sapling:
					return 0f;
				case AgeMask.Young:
					return 0.17500001f;
				case AgeMask.Mature:
					return 0.425f;
				case AgeMask.Elderly:
					return 0.77500004f;
				}
			}
		}
		return 0f;
	}
}
