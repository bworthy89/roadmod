using System;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Pathfind;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Net;

public static class NetUtils
{
	public const float DEFAULT_ELEVATION_STEP = 10f;

	public const float MAX_LANE_WEAR = 10f;

	public const float MAX_LOCAL_CONNECT_DISTANCE = 4f;

	public const float MAX_LOCAL_CONNECT_HEIGHT = 1000f;

	public const float MAX_SNAP_HEIGHT = 50f;

	public const float UTURN_LIMIT_COS = 0.7547096f;

	public const float TURN_LIMIT_COS = -0.4848096f;

	public const float GENTLETURN_LIMIT_COS = -0.9335804f;

	public const float MAX_PASSING_CURVINESS_STREET = MathF.PI / 180f;

	public const float MAX_PASSING_CURVINESS_HIGHWAY = MathF.PI / 360f;

	public const float MIN_VISIBLE_EDGE_LENGTH = 0.1f;

	public const float MIN_VISIBLE_NODE_LENGTH = 0.05f;

	public const float MIN_VISIBLE_LANE_LENGTH = 0.1f;

	public static Bezier4x3 OffsetCurveLeftSmooth(Bezier4x3 curve, float2 offset)
	{
		float3 value = MathUtils.StartTangent(curve);
		float3 value2 = MathUtils.Tangent(curve, 0.5f);
		float3 value3 = MathUtils.EndTangent(curve);
		value = MathUtils.Normalize(value, value.xz);
		value2 = MathUtils.Normalize(value2, value2.xz);
		value3 = MathUtils.Normalize(value3, value3.xz);
		value.y = math.clamp(value.y, -1f, 1f);
		value3.y = math.clamp(value3.y, -1f, 1f);
		float3 a = curve.a;
		float3 middlePos = MathUtils.Position(curve, 0.5f);
		float3 d = curve.d;
		float4 @float = new float4(-offset, offset);
		a.xz += value.zx * @float.xz;
		middlePos.xz += value2.zx * (@float.xz + @float.yw) * 0.5f;
		d.xz += value3.zx * @float.yw;
		return FitCurve(a, value, middlePos, value3, d);
	}

	public static Bezier4x3 CircleCurve(float3 center, float xOffset, float zOffset)
	{
		Bezier4x3 result = new Bezier4x3(center, center, center, center);
		result.a.x += xOffset;
		result.b.x += xOffset;
		result.b.z += zOffset * 0.5522848f;
		result.c.x += xOffset * 0.5522848f;
		result.c.z += zOffset;
		result.d.z += zOffset;
		return result;
	}

	public static Bezier4x3 CircleCurve(float3 center, quaternion rotation, float xOffset, float zOffset)
	{
		float2 xz = math.forward(rotation).xz;
		float2 @float = MathUtils.Right(xz);
		Bezier4x3 result = new Bezier4x3(center, center, center, center);
		result.a.xz += @float * xOffset;
		result.b.xz += @float * xOffset;
		result.b.xz += xz * (zOffset * 0.5522848f);
		result.c.xz += @float * (xOffset * 0.5522848f);
		result.c.xz += xz * zOffset;
		result.d.xz += xz * zOffset;
		return result;
	}

	public static Bezier4x3 FitCurve(float3 startPos, float3 startTangent, float3 middlePos, float3 endTangent, float3 endPos)
	{
		Bezier4x3 bezier4x = FitCurve(startPos, startTangent, endTangent, endPos);
		float3 @float = middlePos - MathUtils.Position(bezier4x, 0.5f);
		float2 float2 = new float2(math.dot(startTangent.xz, @float.xz), math.dot(endTangent.xz, @float.xz));
		float2 float3 = float2 * math.dot(startTangent.xz, endTangent.xz);
		float2 *= math.abs(float2) / math.max(1E-06f, 0.375f * (math.abs(float2) + math.abs(float3.yx)));
		bezier4x.b += startTangent * math.max(float2.x, math.min(0f, 1f - math.distance(bezier4x.a.xz, bezier4x.b.xz)));
		bezier4x.c += endTangent * math.min(float2.y, math.max(0f, math.distance(bezier4x.d.xz, bezier4x.c.xz) - 1f));
		return bezier4x;
	}

	public static Bezier4x3 FitCurve(float3 startPos, float3 startTangent, float3 endTangent, float3 endPos)
	{
		float num = math.distance(startPos.xz, endPos.xz);
		Line3 line = new Line3(startPos, startPos + startTangent);
		Line3 line2 = new Line3(endPos, endPos - endTangent);
		float2 start = ((!MathUtils.Intersect(line.xz, line2.xz, out start)) ? ((float2)(num * 0.75f)) : math.clamp(start, num * 0.01f, num));
		float num2 = math.dot(startTangent.xz, endTangent.xz);
		if (num2 > 0f)
		{
			start = math.lerp(start, num / math.sqrt(2f * num2 + 2f), math.min(1f, num2 * num2));
		}
		else if (num2 < 0f)
		{
			start = math.lerp(start, num * 1.2071068f, math.min(1f, num2 * num2));
		}
		return FitCurve(MathUtils.Cut(line, new float2(0f, start.x)), MathUtils.Cut(line2, new float2(0f, start.y)));
	}

	public static Bezier4x3 FitCurve(Line3.Segment startLine, Line3.Segment endLine)
	{
		float3 @float = MathUtils.Tangent(startLine);
		float3 float2 = MathUtils.Tangent(endLine);
		float num = math.length(@float.xz);
		float num2 = math.length(float2.xz);
		if (num != 0f)
		{
			@float /= num;
		}
		else
		{
			@float.xz = endLine.b.xz - startLine.a.xz;
			num = math.length(@float.xz);
			if (num != 0f)
			{
				@float /= num;
			}
		}
		if (num2 != 0f)
		{
			float2 /= num2;
		}
		else
		{
			float2.xz = startLine.b.xz - endLine.a.xz;
			num2 = math.length(float2.xz);
			if (num2 != 0f)
			{
				float2 /= num2;
			}
		}
		@float.y = math.clamp(@float.y, -1f, 1f);
		float2.y = math.clamp(float2.y, -1f, 1f);
		float num3 = math.acos(math.saturate(0f - math.dot(@float.xz, float2.xz)));
		float num4 = math.tan(num3 / 2f);
		float num5 = (num + num2) * (1f / 6f);
		num5 = ((!(num4 >= 0.0001f)) ? (num5 * 2f) : (num5 * (4f * math.tan(num3 / 4f) / num4)));
		return new Bezier4x3
		{
			a = startLine.a,
			b = startLine.a + @float * math.min(num, num5),
			c = endLine.a + float2 * math.min(num2, num5),
			d = endLine.a
		};
	}

	public static Bezier4x3 StraightCurve(float3 startPos, float3 endPos)
	{
		return new Bezier4x3
		{
			a = startPos,
			b = math.lerp(startPos, endPos, 1f / 3f),
			c = math.lerp(startPos, endPos, 2f / 3f),
			d = endPos
		};
	}

	public static Bezier4x3 StraightCurve(float3 startPos, float3 endPos, float hanging)
	{
		Bezier4x3 result = new Bezier4x3
		{
			a = startPos,
			b = math.lerp(startPos, endPos, 1f / 3f),
			c = math.lerp(startPos, endPos, 2f / 3f),
			d = endPos
		};
		float num = math.distance(result.a.xz, result.d.xz) * hanging * 1.3333334f;
		result.b.y -= num;
		result.c.y -= num;
		return result;
	}

	public static float FindMiddleTangentPos(Bezier4x2 curve, float2 offset)
	{
		float num = math.lerp(offset.x, offset.y, 0.5f);
		float num2 = num;
		float2 value = MathUtils.Tangent(curve, offset.x);
		float2 value2 = MathUtils.Tangent(curve, offset.y);
		if (!MathUtils.TryNormalize(ref value) || !MathUtils.TryNormalize(ref value2))
		{
			return num;
		}
		float2 @float = offset;
		for (int i = 0; i < 24; i++)
		{
			float2 value3 = MathUtils.Tangent(curve, num2);
			if (!MathUtils.TryNormalize(ref value3))
			{
				break;
			}
			float num3 = math.distancesq(value, value3);
			float num4 = math.distancesq(value2, value3);
			if (num3 < num4)
			{
				@float.x = num2;
			}
			else
			{
				if (!(num3 > num4))
				{
					break;
				}
				@float.y = num2;
			}
			num2 = math.lerp(@float.x, @float.y, 0.5f);
		}
		return math.lerp(num2, num, math.saturate(0.5f + math.dot(value, value2) * 0.5f));
	}

	public static float CalculateCurviness(Curve curve, float width)
	{
		if (curve.m_Length > 0.1f)
		{
			float3 tangent = MathUtils.StartTangent(curve.m_Bezier);
			float3 a = curve.m_Bezier.a;
			float3 @float = MathUtils.Tangent(curve.m_Bezier, 0.25f);
			float3 float2 = MathUtils.Position(curve.m_Bezier, 0.25f);
			float3 float3 = MathUtils.Tangent(curve.m_Bezier, 0.5f);
			float3 float4 = MathUtils.Position(curve.m_Bezier, 0.5f);
			float3 float5 = MathUtils.Tangent(curve.m_Bezier, 0.75f);
			float3 float6 = MathUtils.Position(curve.m_Bezier, 0.75f);
			float3 tangent2 = MathUtils.EndTangent(curve.m_Bezier);
			float3 d = curve.m_Bezier.d;
			float4 x = default(float4);
			x.x = CalculateCurviness(a, tangent, float2, @float);
			x.y = CalculateCurviness(float2, @float, float4, float3);
			x.z = CalculateCurviness(float4, float3, float6, float5);
			x.w = CalculateCurviness(float6, float5, d, tangent2);
			float num = math.cmax(x);
			if (curve.m_Length < width * 2f)
			{
				num = math.lerp(math.min(num, CalculateCurviness(a, tangent, d, tangent2)), num, math.smoothstep(width * 0.1f, width * 2f, curve.m_Length));
			}
			return num;
		}
		return 0f;
	}

	public static float CalculateStartCurviness(Curve curve, float width)
	{
		if (curve.m_Length > 0.1f)
		{
			float3 tangent = MathUtils.StartTangent(curve.m_Bezier);
			float3 a = curve.m_Bezier.a;
			float3 @float = MathUtils.Tangent(curve.m_Bezier, 0.25f);
			float3 float2 = MathUtils.Position(curve.m_Bezier, 0.25f);
			float3 tangent2 = MathUtils.Tangent(curve.m_Bezier, 0.5f);
			float3 position = MathUtils.Position(curve.m_Bezier, 0.5f);
			float2 x = default(float2);
			x.x = CalculateCurviness(a, tangent, float2, @float);
			x.y = CalculateCurviness(float2, @float, position, tangent2);
			return math.cmax(x);
		}
		return 0f;
	}

	public static float CalculateEndCurviness(Curve curve, float width)
	{
		if (curve.m_Length > 0.1f)
		{
			float3 tangent = MathUtils.Tangent(curve.m_Bezier, 0.5f);
			float3 position = MathUtils.Position(curve.m_Bezier, 0.5f);
			float3 @float = MathUtils.Tangent(curve.m_Bezier, 0.75f);
			float3 float2 = MathUtils.Position(curve.m_Bezier, 0.75f);
			float3 tangent2 = MathUtils.EndTangent(curve.m_Bezier);
			float3 d = curve.m_Bezier.d;
			float2 x = default(float2);
			x.x = CalculateCurviness(position, tangent, float2, @float);
			x.y = CalculateCurviness(float2, @float, d, tangent2);
			return math.cmax(x);
		}
		return 0f;
	}

	public static float CalculateCurviness(float3 position1, float3 tangent1, float3 position2, float3 tangent2)
	{
		float num = math.distance(position1, position2);
		if (MathUtils.TryNormalize(ref tangent1) && MathUtils.TryNormalize(ref tangent2) && num >= 1E-06f)
		{
			return CalculateCurviness(tangent1, tangent2, num);
		}
		return 0f;
	}

	public static float CalculateCurviness(float3 tangent1, float3 tangent2, float distance)
	{
		float num = math.acos(math.clamp(math.dot(tangent1, tangent2), -1f, 1f));
		return 2f * math.sin(num * 0.5f) / distance;
	}

	public static quaternion GetNodeRotation(float3 tangent)
	{
		return GetNodeRotation(tangent, quaternion.identity);
	}

	public static quaternion GetNodeRotation(float3 tangent, quaternion defaultRotation)
	{
		tangent.y = 0f;
		if (MathUtils.TryNormalize(ref tangent))
		{
			return quaternion.LookRotation(tangent, math.up());
		}
		return defaultRotation;
	}

	public static float ExtendedDistance(Bezier4x2 curve, float2 position, out float t)
	{
		float t2;
		float num = MathUtils.Distance(new Line2(curve.a, curve.a * 2f - curve.b), position, out t2);
		float t3;
		float num2 = MathUtils.Distance(curve, position, out t3);
		float t4;
		float num3 = MathUtils.Distance(new Line2(curve.d, curve.d * 2f - curve.c), position, out t4);
		if (t2 >= 0f && num < num2 && (num < num3 || t4 < 0f))
		{
			t = 0f - t2;
			return num;
		}
		if (t4 >= 0f && num3 < num2)
		{
			t = 1f + t4;
			return num3;
		}
		t = t3;
		return num2;
	}

	public static float ExtendedLength(Bezier4x2 curve, float t)
	{
		if (t <= 0f)
		{
			return math.distance(curve.a, curve.b) * t;
		}
		if (t <= 1f)
		{
			return MathUtils.Length(curve, new Bounds1(0f, t));
		}
		return MathUtils.Length(curve) + math.distance(curve.c, curve.d) * (t - 1f);
	}

	public static float ExtendedClampLength(Bezier4x2 curve, float distance)
	{
		if (distance <= 0f)
		{
			float num = math.distance(curve.a, curve.b);
			return math.select(distance / num, 0f, num == 0f);
		}
		Bounds1 t = new Bounds1(0f, 1f);
		if (MathUtils.ClampLength(curve, ref t, distance))
		{
			return t.max;
		}
		distance -= MathUtils.Length(curve);
		float num2 = math.distance(curve.c, curve.d);
		return math.select(1f + distance / num2, 1f, num2 == 0f);
	}

	public static void ExtendedPositionAndTangent(Bezier4x3 curve, float t, out float3 position, out float3 tangent)
	{
		if (t <= 0f)
		{
			position = MathUtils.Position(new Line3(curve.a, curve.a * 2f - curve.b), 0f - t);
			tangent = curve.b - curve.a;
		}
		else if (t <= 1f)
		{
			position = MathUtils.Position(curve, t);
			tangent = MathUtils.Tangent(curve, t);
		}
		else
		{
			position = MathUtils.Position(new Line3(curve.d, curve.d * 2f - curve.c), t - 1f);
			tangent = curve.d - curve.c;
		}
	}

	public static int ChooseClosestLane(int minIndex, int maxIndex, float3 comparePosition, PathMethod pathMethods, DynamicBuffer<SubLane> lanes, ref ComponentLookup<Curve> curves, float curvePosition)
	{
		float num = float.MaxValue;
		int result = minIndex;
		maxIndex = math.min(maxIndex, lanes.Length - 1);
		for (int i = minIndex; i <= maxIndex; i++)
		{
			SubLane subLane = lanes[i];
			if ((subLane.m_PathMethods & pathMethods) != 0)
			{
				float t;
				float num2 = MathUtils.DistanceSquared(curves[subLane.m_SubLane].m_Bezier, comparePosition, out t);
				if (num2 < num)
				{
					num = num2;
					result = i;
				}
			}
		}
		return result;
	}

	public static float GetAvailability(DynamicBuffer<ResourceAvailability> availabilities, AvailableResource resource, float curvePos)
	{
		if ((int)resource >= availabilities.Length)
		{
			return 0f;
		}
		float2 availability = availabilities[(int)resource].m_Availability;
		return math.lerp(availability.x, availability.y, curvePos);
	}

	public static float GetServiceCoverage(DynamicBuffer<ServiceCoverage> coverages, CoverageService service, float curvePos)
	{
		if ((int)service >= coverages.Length)
		{
			return 0f;
		}
		ServiceCoverage serviceCoverage = coverages[(int)service];
		return math.lerp(serviceCoverage.m_Coverage.x, serviceCoverage.m_Coverage.y, curvePos);
	}

	public static void AddLaneObject(DynamicBuffer<LaneObject> buffer, Entity laneObject, float2 curvePosition)
	{
		for (int i = 0; i < buffer.Length; i++)
		{
			if (buffer[i].m_CurvePosition.y >= curvePosition.y)
			{
				buffer.Insert(i, new LaneObject(laneObject, curvePosition));
				return;
			}
		}
		buffer.Add(new LaneObject(laneObject, curvePosition));
	}

	public static void UpdateLaneObject(DynamicBuffer<LaneObject> buffer, Entity laneObject, float2 curvePosition)
	{
		LaneObject laneObject2 = new LaneObject(laneObject, curvePosition);
		for (int i = 0; i < buffer.Length; i++)
		{
			LaneObject laneObject3 = buffer[i];
			if (laneObject3.m_LaneObject == laneObject)
			{
				for (int j = i + 1; j < buffer.Length; j++)
				{
					laneObject3 = buffer[j];
					if (laneObject3.m_CurvePosition.y >= curvePosition.y)
					{
						buffer[j - 1] = laneObject2;
						return;
					}
					buffer[j - 1] = laneObject3;
				}
				buffer[buffer.Length - 1] = laneObject2;
				return;
			}
			if (!(laneObject3.m_CurvePosition.y >= curvePosition.y))
			{
				continue;
			}
			buffer[i] = laneObject2;
			laneObject2 = laneObject3;
			for (int k = i + 1; k < buffer.Length; k++)
			{
				laneObject3 = buffer[k];
				buffer[k] = laneObject2;
				laneObject2 = laneObject3;
				if (laneObject2.m_LaneObject == laneObject)
				{
					return;
				}
			}
			break;
		}
		buffer.Add(laneObject2);
	}

	public static void RemoveLaneObject(DynamicBuffer<LaneObject> buffer, Entity laneObject)
	{
		CollectionUtils.RemoveValue(buffer, new LaneObject(laneObject));
	}

	public static bool CanConnect(NetData netData1, NetData netData2)
	{
		if ((netData1.m_RequiredLayers & netData2.m_ConnectLayers) != netData1.m_RequiredLayers)
		{
			return (netData2.m_RequiredLayers & netData1.m_ConnectLayers) == netData2.m_RequiredLayers;
		}
		return true;
	}

	public static bool FindConnectedLane(ref Entity laneEntity, ref bool forward, ref ComponentLookup<Lane> laneData, ref ComponentLookup<EdgeLane> edgeLaneData, ref ComponentLookup<Owner> ownerData, ref ComponentLookup<Edge> edgeData, ref BufferLookup<ConnectedEdge> connectedEdges, ref BufferLookup<SubLane> subLanes)
	{
		Lane lane = laneData[laneEntity];
		Entity entity = ownerData[laneEntity].m_Owner;
		Entity entity2 = entity;
		PathNode other = (forward ? lane.m_EndNode : lane.m_StartNode);
		if (edgeLaneData.HasComponent(laneEntity))
		{
			EdgeLane edgeLane = edgeLaneData[laneEntity];
			float num = (forward ? edgeLane.m_EdgeDelta.y : edgeLane.m_EdgeDelta.x);
			if (num == 0f)
			{
				entity = edgeData[entity].m_Start;
			}
			else if (num == 1f)
			{
				entity = edgeData[entity].m_End;
			}
			DynamicBuffer<SubLane> dynamicBuffer = subLanes[entity];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity subLane = dynamicBuffer[i].m_SubLane;
				if (!(subLane == laneEntity))
				{
					Lane lane2 = laneData[subLane];
					if (lane2.m_StartNode.Equals(other))
					{
						laneEntity = subLane;
						forward = true;
						return true;
					}
					if (lane2.m_EndNode.Equals(other))
					{
						laneEntity = subLane;
						forward = false;
						return true;
					}
				}
			}
			if (entity == entity2 || !other.OwnerEquals(new PathNode(entity, 0)))
			{
				return false;
			}
		}
		if (connectedEdges.TryGetBuffer(entity, out var bufferData))
		{
			for (int j = 0; j < bufferData.Length; j++)
			{
				entity = bufferData[j].m_Edge;
				if (entity == entity2)
				{
					continue;
				}
				DynamicBuffer<SubLane> dynamicBuffer2 = subLanes[entity];
				for (int k = 0; k < dynamicBuffer2.Length; k++)
				{
					Entity subLane2 = dynamicBuffer2[k].m_SubLane;
					if (!(subLane2 == laneEntity))
					{
						Lane lane3 = laneData[subLane2];
						if (lane3.m_StartNode.Equals(other))
						{
							laneEntity = subLane2;
							forward = true;
							return true;
						}
						if (lane3.m_EndNode.Equals(other))
						{
							laneEntity = subLane2;
							forward = false;
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	public static CollisionMask GetCollisionMask(NetCompositionData compositionData, bool ignoreMarkers)
	{
		if ((compositionData.m_State & CompositionState.NoSubCollisions) != 0 && ignoreMarkers)
		{
			return (CollisionMask)0;
		}
		CollisionMask collisionMask = (CollisionMask)0;
		if ((compositionData.m_State & CompositionState.ExclusiveGround) != 0)
		{
			collisionMask |= CollisionMask.OnGround | CollisionMask.ExclusiveGround;
		}
		collisionMask = (((compositionData.m_Flags.m_General & CompositionFlags.General.Elevated) != 0) ? (collisionMask | CollisionMask.Overground) : (((compositionData.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0) ? (collisionMask | CollisionMask.Underground) : (((compositionData.m_State & CompositionState.HasSurface) == 0) ? (collisionMask | CollisionMask.Overground) : (collisionMask | (CollisionMask.OnGround | CollisionMask.Overground)))));
		if (((compositionData.m_Flags.m_Left | compositionData.m_Flags.m_Right) & CompositionFlags.Side.Lowered) != 0)
		{
			collisionMask |= CollisionMask.Underground;
		}
		return collisionMask;
	}

	public static CollisionMask GetCollisionMask(LabelPosition labelPosition)
	{
		if (!labelPosition.m_IsUnderground)
		{
			return CollisionMask.Overground;
		}
		return CollisionMask.Underground;
	}

	public static bool IsTurn(float2 startPosition, float2 startDirection, float2 endPosition, float2 endDirection, out bool right, out bool gentle, out bool uturn)
	{
		float2 x = MathUtils.Right(startDirection);
		float4 start = default(float4);
		start.y = math.dot(startDirection, endDirection);
		start.w = math.dot(x, endDirection);
		float num = math.distance(startPosition, endPosition);
		if (num > 0.1f)
		{
			float2 y = (startPosition - endPosition) / num;
			start.x = math.dot(startDirection, y);
			start.z = math.dot(x, y);
		}
		else
		{
			start.x = -1f;
			start.z = 0f;
		}
		start = math.lerp(start, new float4(-1f, -1f, start.wz), math.saturate(new float4(start.z * start.w * new float2(-2f, -4f), start.xy)));
		start = math.select(start, start.yxwz, start.y > start.x);
		right = start.z < 0f;
		gentle = (start.x > -0.9335804f) & (start.x <= -0.4848096f);
		uturn = start.x > 0.7547096f;
		return start.x > -0.9335804f;
	}

	public static int GetConstructionCost(Curve curve, Elevation startElevation, Elevation endElevation, PlaceableNetComposition placeableNetData)
	{
		int num = math.max(1, Mathf.RoundToInt(curve.m_Length / 8f));
		int num2 = math.max(0, Mathf.RoundToInt(math.max(math.cmin(startElevation.m_Elevation), math.cmin(endElevation.m_Elevation)) / 10f));
		return num * ((int)placeableNetData.m_ConstructionCost + num2 * (int)placeableNetData.m_ElevationCost);
	}

	public static int GetUpkeepCost(Curve curve, PlaceableNetComposition placeableNetData)
	{
		float num = math.max(1f, math.round(curve.m_Length / 8f));
		return math.max(1, Mathf.RoundToInt(num * placeableNetData.m_UpkeepCost));
	}

	public static int GetRefundAmount(Recent recent, uint simulationFrame, EconomyParameterData economyParameterData)
	{
		if ((float)simulationFrame < (float)recent.m_ModificationFrame + 262144f * economyParameterData.m_RoadRefundTimeRange.x)
		{
			return (int)((float)recent.m_ModificationCost * economyParameterData.m_RoadRefundPercentage.x);
		}
		if ((float)simulationFrame < (float)recent.m_ModificationFrame + 262144f * economyParameterData.m_RoadRefundTimeRange.y)
		{
			return (int)((float)recent.m_ModificationCost * economyParameterData.m_RoadRefundPercentage.y);
		}
		if ((float)simulationFrame < (float)recent.m_ModificationFrame + 262144f * economyParameterData.m_RoadRefundTimeRange.z)
		{
			return (int)((float)recent.m_ModificationCost * economyParameterData.m_RoadRefundPercentage.z);
		}
		return 0;
	}

	public static int GetUpgradeCost(int newCost, int oldCost)
	{
		return math.max(0, newCost - oldCost);
	}

	public static int GetUpgradeCost(int newCost, int oldCost, Recent recent, uint simulationFrame, EconomyParameterData economyParameterData)
	{
		if (newCost >= oldCost)
		{
			return GetUpgradeCost(newCost, oldCost);
		}
		recent.m_ModificationCost = math.min(recent.m_ModificationCost, oldCost - newCost);
		return -GetRefundAmount(recent, simulationFrame, economyParameterData);
	}

	public static bool FindNextLane(ref Entity entity, ref ComponentLookup<Owner> ownerData, ref ComponentLookup<Lane> laneData, ref BufferLookup<SubLane> subLanes)
	{
		if (!ownerData.TryGetComponent(entity, out var componentData) || !laneData.TryGetComponent(entity, out var componentData2))
		{
			return false;
		}
		if (!subLanes.TryGetBuffer(componentData.m_Owner, out var bufferData))
		{
			return false;
		}
		for (int i = 0; i < bufferData.Length; i++)
		{
			Entity subLane = bufferData[i].m_SubLane;
			Lane lane = laneData[subLane];
			if (componentData2.m_EndNode.Equals(lane.m_StartNode))
			{
				entity = subLane;
				return true;
			}
		}
		return false;
	}

	public static bool FindPrevLane(ref Entity entity, ref ComponentLookup<Owner> ownerData, ref ComponentLookup<Lane> laneData, ref BufferLookup<SubLane> subLanes)
	{
		if (!ownerData.TryGetComponent(entity, out var componentData) || !laneData.TryGetComponent(entity, out var componentData2))
		{
			return false;
		}
		if (!subLanes.TryGetBuffer(componentData.m_Owner, out var bufferData))
		{
			return false;
		}
		for (int i = 0; i < bufferData.Length; i++)
		{
			Entity subLane = bufferData[i].m_SubLane;
			Lane lane = laneData[subLane];
			if (componentData2.m_StartNode.Equals(lane.m_EndNode))
			{
				entity = subLane;
				return true;
			}
		}
		return false;
	}

	public static bool FindEdgeLane(ref Entity entity, ref ComponentLookup<Owner> ownerData, ref ComponentLookup<Lane> laneData, ref BufferLookup<SubLane> subLanes, bool startNode)
	{
		if (!ownerData.TryGetComponent(entity, out var componentData) || !laneData.TryGetComponent(entity, out var componentData2))
		{
			return false;
		}
		if (!subLanes.TryGetBuffer(componentData.m_Owner, out var bufferData))
		{
			return false;
		}
		PathNode pathNode = (startNode ? componentData2.m_StartNode : componentData2.m_EndNode);
		for (int i = 0; i < bufferData.Length; i++)
		{
			Entity subLane = bufferData[i].m_SubLane;
			if (pathNode.EqualsIgnoreCurvePos(laneData[subLane].m_MiddleNode))
			{
				entity = subLane;
				return true;
			}
		}
		return false;
	}

	public static float4 GetTrafficFlowSpeed(Road road)
	{
		return GetTrafficFlowSpeed(road.m_TrafficFlowDuration0 + road.m_TrafficFlowDuration1, road.m_TrafficFlowDistance0 + road.m_TrafficFlowDistance1);
	}

	public static float4 GetTrafficFlowSpeed(float4 duration, float4 distance)
	{
		return math.saturate(distance / duration);
	}

	public static float GetTrafficFlowSpeed(float duration, float distance)
	{
		return math.saturate(distance / duration);
	}

	public static Node AdjustPosition(Node node, ref TerrainHeightData terrainHeightData)
	{
		Node result = node;
		result.m_Position.y = TerrainUtils.SampleHeight(ref terrainHeightData, node.m_Position);
		return result;
	}

	public static Node AdjustPosition(Node node, ref BuildingUtils.LotInfo lotInfo)
	{
		Node result = node;
		BuildingUtils.SampleHeight(ref lotInfo, node.m_Position);
		return result;
	}

	public static Curve AdjustPosition(Curve curve, bool fixedStart, bool linearMiddle, bool fixedEnd, ref TerrainHeightData terrainHeightData)
	{
		Curve result = curve;
		if (!fixedStart)
		{
			result.m_Bezier.a.y = TerrainUtils.SampleHeight(ref terrainHeightData, curve.m_Bezier.a);
		}
		if (!fixedEnd)
		{
			result.m_Bezier.d.y = TerrainUtils.SampleHeight(ref terrainHeightData, curve.m_Bezier.d);
		}
		if (linearMiddle)
		{
			result.m_Bezier.b.y = math.lerp(result.m_Bezier.a.y, result.m_Bezier.d.y, 1f / 3f);
			result.m_Bezier.c.y = math.lerp(result.m_Bezier.a.y, result.m_Bezier.d.y, 2f / 3f);
		}
		else
		{
			result.m_Bezier.b.y = TerrainUtils.SampleHeight(ref terrainHeightData, curve.m_Bezier.b);
			result.m_Bezier.c.y = TerrainUtils.SampleHeight(ref terrainHeightData, curve.m_Bezier.c);
			float num = result.m_Bezier.b.y - MathUtils.Position(result.m_Bezier.y, 1f / 3f);
			float num2 = result.m_Bezier.c.y - MathUtils.Position(result.m_Bezier.y, 2f / 3f);
			result.m_Bezier.b.y += num * 3f - num2 * 1.5f;
			result.m_Bezier.c.y += num2 * 3f - num * 1.5f;
		}
		return result;
	}

	public static Curve AdjustPosition(Curve curve, bool fixedStart, bool linearMiddle, bool fixedEnd, ref TerrainHeightData terrainHeightData, ref WaterSurfaceData<SurfaceWater> waterSurfaceData)
	{
		Curve result = curve;
		if (!fixedStart)
		{
			result.m_Bezier.a.y = WaterUtils.SampleHeight(ref waterSurfaceData, ref terrainHeightData, curve.m_Bezier.a);
		}
		if (!fixedEnd)
		{
			result.m_Bezier.d.y = WaterUtils.SampleHeight(ref waterSurfaceData, ref terrainHeightData, curve.m_Bezier.d);
		}
		if (linearMiddle)
		{
			result.m_Bezier.b.y = math.lerp(result.m_Bezier.a.y, result.m_Bezier.d.y, 1f / 3f);
			result.m_Bezier.c.y = math.lerp(result.m_Bezier.a.y, result.m_Bezier.d.y, 2f / 3f);
		}
		else
		{
			result.m_Bezier.b.y = WaterUtils.SampleHeight(ref waterSurfaceData, ref terrainHeightData, curve.m_Bezier.b);
			result.m_Bezier.c.y = WaterUtils.SampleHeight(ref waterSurfaceData, ref terrainHeightData, curve.m_Bezier.c);
			float num = result.m_Bezier.b.y - MathUtils.Position(result.m_Bezier.y, 1f / 3f);
			float num2 = result.m_Bezier.c.y - MathUtils.Position(result.m_Bezier.y, 2f / 3f);
			result.m_Bezier.b.y += num * 3f - num2 * 1.5f;
			result.m_Bezier.c.y += num2 * 3f - num * 1.5f;
		}
		return result;
	}

	public static Curve AdjustPosition(Curve curve, bool2 fixedStart, bool linearMiddle, bool2 fixedEnd, ref BuildingUtils.LotInfo lotInfo)
	{
		Curve result = curve;
		if (!fixedStart.x)
		{
			result.m_Bezier.a.y = BuildingUtils.SampleHeight(ref lotInfo, curve.m_Bezier.a);
		}
		if (!fixedEnd.x)
		{
			result.m_Bezier.d.y = BuildingUtils.SampleHeight(ref lotInfo, curve.m_Bezier.d);
		}
		if (linearMiddle)
		{
			if (!fixedStart.y)
			{
				result.m_Bezier.b.y = math.lerp(result.m_Bezier.a.y, result.m_Bezier.d.y, 1f / 3f);
			}
			if (!fixedEnd.y)
			{
				result.m_Bezier.c.y = math.lerp(result.m_Bezier.a.y, result.m_Bezier.d.y, 2f / 3f);
			}
		}
		else
		{
			if (!fixedStart.y)
			{
				result.m_Bezier.b.y = BuildingUtils.SampleHeight(ref lotInfo, curve.m_Bezier.b);
			}
			if (!fixedEnd.y)
			{
				result.m_Bezier.c.y = BuildingUtils.SampleHeight(ref lotInfo, curve.m_Bezier.c);
			}
			float num = result.m_Bezier.b.y - MathUtils.Position(result.m_Bezier.y, 1f / 3f);
			float num2 = result.m_Bezier.c.y - MathUtils.Position(result.m_Bezier.y, 2f / 3f);
			if (!fixedStart.y)
			{
				result.m_Bezier.b.y += num * 3f - num2 * 1.5f;
			}
			if (!fixedEnd.y)
			{
				result.m_Bezier.c.y += num2 * 3f - num * 1.5f;
			}
		}
		return result;
	}

	public static bool ShouldInvert(NetInvertMode invertMode, bool lefthandTraffic)
	{
		if (!(invertMode == NetInvertMode.LefthandTraffic && lefthandTraffic) && (invertMode != NetInvertMode.RighthandTraffic || lefthandTraffic))
		{
			return invertMode == NetInvertMode.Always;
		}
		return true;
	}

	public static Game.Prefabs.SubNet GetSubNet(DynamicBuffer<Game.Prefabs.SubNet> subNets, int index, bool lefthandTraffic, ref ComponentLookup<NetGeometryData> netGeometryLookup)
	{
		Game.Prefabs.SubNet result = subNets[index];
		if (ShouldInvert(result.m_InvertMode, lefthandTraffic))
		{
			if (netGeometryLookup.TryGetComponent(result.m_Prefab, out var componentData) && (componentData.m_Flags & GeometryFlags.FlipTrafficHandedness) != 0)
			{
				result.m_Curve = MathUtils.Invert(result.m_Curve);
				result.m_NodeIndex = result.m_NodeIndex.yx;
				result.m_ParentMesh = result.m_ParentMesh.yx;
				FixInvertedUpgradeTrafficHandedness(ref result.m_Upgrades);
			}
			else
			{
				FlipUpgradeTrafficHandedness(ref result.m_Upgrades);
			}
		}
		return result;
	}

	public static void FlipUpgradeTrafficHandedness(ref CompositionFlags flags)
	{
		uint bitMask = (uint)flags.m_Left;
		uint bitMask2 = (uint)flags.m_Right;
		CommonUtils.SwapBits(ref bitMask, 16777216u, 33554432u);
		CommonUtils.SwapBits(ref bitMask2, 16777216u, 33554432u);
		flags.m_Left = (CompositionFlags.Side)bitMask2;
		flags.m_Right = (CompositionFlags.Side)bitMask;
	}

	public static void FixInvertedUpgradeTrafficHandedness(ref CompositionFlags flags)
	{
		uint bitMask = (uint)flags.m_Left;
		uint bitMask2 = (uint)flags.m_Right;
		CommonUtils.SwapBits(ref bitMask, ref bitMask2, 32768u);
		flags.m_Left = (CompositionFlags.Side)bitMask;
		flags.m_Right = (CompositionFlags.Side)bitMask2;
	}

	public static float GetTerrainSmoothingWidth(NetData netData)
	{
		if ((netData.m_RequiredLayers & (Layer.Taxiway | Layer.MarkerTaxiway)) != Layer.None)
		{
			return 100f;
		}
		if ((netData.m_RequiredLayers & Layer.Waterway) != Layer.None)
		{
			return 20f;
		}
		return 8f;
	}

	public static int GetParkingSlotCount(Curve curve, ParkingLane parkingLane, ParkingLaneData prefabParkingLane)
	{
		return (int)math.floor((GetParkingSlotSpace(curve, parkingLane, prefabParkingLane) + 0.01f) / prefabParkingLane.m_SlotInterval);
	}

	public static float GetParkingSlotInterval(Curve curve, ParkingLane parkingLane, ParkingLaneData prefabParkingLane, int slotCount)
	{
		if (slotCount == 0 || (parkingLane.m_Flags & ParkingLaneFlags.FindConnections) != 0)
		{
			return prefabParkingLane.m_SlotInterval;
		}
		return GetParkingSlotSpace(curve, parkingLane, prefabParkingLane) / (float)slotCount;
	}

	private static float GetParkingSlotSpace(Curve curve, ParkingLane parkingLane, ParkingLaneData prefabParkingLane)
	{
		float num = curve.m_Length;
		if ((parkingLane.m_Flags & ParkingLaneFlags.FindConnections) == 0)
		{
			num -= math.select(0f, 0.2f, (parkingLane.m_Flags & ParkingLaneFlags.StartingLane) != 0);
			num -= math.select(0f, 0.2f, (parkingLane.m_Flags & ParkingLaneFlags.EndingLane) != 0);
			if (prefabParkingLane.m_SlotAngle > 0.25f)
			{
				float num2 = math.min(math.dot(y: new float2(math.cos(prefabParkingLane.m_SlotAngle), math.sin(prefabParkingLane.m_SlotAngle)), x: prefabParkingLane.m_SlotSize), prefabParkingLane.m_SlotSize.y);
				switch (parkingLane.m_Flags & (ParkingLaneFlags.StartingLane | ParkingLaneFlags.EndingLane))
				{
				case ParkingLaneFlags.StartingLane:
				case ParkingLaneFlags.EndingLane:
					num -= num2 * 0.5f * math.tan(MathF.PI / 2f - prefabParkingLane.m_SlotAngle);
					break;
				case ParkingLaneFlags.StartingLane | ParkingLaneFlags.EndingLane:
					num -= num2 * math.tan(MathF.PI / 2f - prefabParkingLane.m_SlotAngle);
					break;
				}
			}
		}
		return num;
	}

	public static bool TryGetCombinedSegmentForLanes(EdgeGeometry edgeGeometry, NetGeometryData prefabGeometryData, out Segment segment)
	{
		bool flag = (prefabGeometryData.m_Flags & (GeometryFlags.StraightEdges | GeometryFlags.SmoothSlopes)) == GeometryFlags.StraightEdges;
		segment = edgeGeometry.m_Start;
		segment.m_Left = MathUtils.Join(edgeGeometry.m_Start.m_Left, edgeGeometry.m_End.m_Left);
		segment.m_Right = MathUtils.Join(edgeGeometry.m_Start.m_Right, edgeGeometry.m_End.m_Right);
		segment.m_Length = edgeGeometry.m_Start.m_Length + edgeGeometry.m_End.m_Length;
		if (!flag && MathUtils.Length(MathUtils.Lerp(segment.m_Left.xz, segment.m_Right.xz, 0.5f)) <= prefabGeometryData.m_EdgeLengthRange.max * 0.5f)
		{
			float3 @float = default(float3);
			@float.x = MathUtils.Distance(segment.m_Left, MathUtils.Position(edgeGeometry.m_Start.m_Left, 0.5f), out var t);
			@float.y = MathUtils.Distance(segment.m_Left, edgeGeometry.m_Start.m_Left.d, out t);
			@float.z = MathUtils.Distance(segment.m_Left, MathUtils.Position(edgeGeometry.m_End.m_Left, 0.5f), out t);
			float3 float2 = default(float3);
			float2.x = MathUtils.Distance(segment.m_Right, MathUtils.Position(edgeGeometry.m_Start.m_Right, 0.5f), out t);
			float2.y = MathUtils.Distance(segment.m_Right, edgeGeometry.m_Start.m_Right.d, out t);
			float2.z = MathUtils.Distance(segment.m_Right, MathUtils.Position(edgeGeometry.m_End.m_Right, 0.5f), out t);
			flag = math.all((@float < 0.2f) & (float2 < 0.2f));
		}
		return flag;
	}
}
