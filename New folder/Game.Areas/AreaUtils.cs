using System;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Areas;

public static class AreaUtils
{
	public struct ObjectItem
	{
		public Circle2 m_Circle;

		public Entity m_Entity;

		public ObjectItem(float radius, float2 position, Entity entity)
		{
			m_Circle = new Circle2(radius, position);
			m_Entity = entity;
		}
	}

	private struct FixPathItem : ILessThan<FixPathItem>
	{
		public PathNode m_Node;

		public PathElement m_PathElement;

		public float m_Cost;

		public FixPathItem(PathNode node, PathElement pathElement, float cost)
		{
			m_Node = node;
			m_PathElement = pathElement;
			m_Cost = cost;
		}

		public bool LessThan(FixPathItem other)
		{
			return m_Cost < other.m_Cost;
		}
	}

	public const float NODE_DISTANCE_TOLERANCE = 0.1f;

	public static float GetMinNodeDistance(AreaGeometryData areaData)
	{
		return areaData.m_SnapDistance * 0.5f;
	}

	public static Triangle3 GetTriangle3(DynamicBuffer<Node> nodes, Triangle triangle)
	{
		return new Triangle3(nodes[triangle.m_Indices.x].m_Position, nodes[triangle.m_Indices.y].m_Position, nodes[triangle.m_Indices.z].m_Position);
	}

	public static float3 GetElevations(DynamicBuffer<Node> nodes, Triangle triangle)
	{
		return new float3(nodes[triangle.m_Indices.x].m_Elevation, nodes[triangle.m_Indices.y].m_Elevation, nodes[triangle.m_Indices.z].m_Elevation);
	}

	public static Bounds3 GetBounds(Triangle triangle, Triangle3 triangle3, AreaGeometryData areaData)
	{
		Bounds3 result = MathUtils.Bounds(triangle3);
		result.min.y += triangle.m_HeightRange.min;
		result.max.y += triangle.m_HeightRange.max + areaData.m_MaxHeight;
		return result;
	}

	public static int CalculateStorageCapacity(Geometry geometry, StorageAreaData prefabStorageData)
	{
		return Mathf.RoundToInt(geometry.m_SurfaceArea * (1f / 64f) * (float)prefabStorageData.m_Capacity);
	}

	public static float CalculateStorageObjectArea(Geometry geometry, Storage storage, StorageAreaData prefabStorageData)
	{
		float y = geometry.m_SurfaceArea * (1f / 64f) * (float)prefabStorageData.m_Capacity;
		float x = (float)storage.m_Amount / math.max(1f, y);
		return math.min(0.25f, math.sqrt(x)) * geometry.m_SurfaceArea;
	}

	public static float CalculateExtractorObjectArea(Geometry geometry, Extractor extractor, ExtractorAreaData extractorAreaData)
	{
		return math.min(extractor.m_TotalExtracted * extractorAreaData.m_ObjectSpawnFactor, geometry.m_SurfaceArea * extractorAreaData.m_MaxObjectArea);
	}

	public static Triangle2 GetTriangle2(DynamicBuffer<Node> nodes, Triangle triangle)
	{
		return new Triangle2(nodes[triangle.m_Indices.x].m_Position.xz, nodes[triangle.m_Indices.y].m_Position.xz, nodes[triangle.m_Indices.z].m_Position.xz);
	}

	public static Triangle2 GetTriangle2(DynamicBuffer<Node> nodes, Triangle triangle, float expandAmount, bool isCounterClockwise)
	{
		Triangle2 result = default(Triangle2);
		result.a = GetExpandedNode(nodes, triangle.m_Indices.x, expandAmount, isComplete: true, isCounterClockwise).xz;
		result.b = GetExpandedNode(nodes, triangle.m_Indices.y, expandAmount, isComplete: true, isCounterClockwise).xz;
		result.c = GetExpandedNode(nodes, triangle.m_Indices.z, expandAmount, isComplete: true, isCounterClockwise).xz;
		return result;
	}

	public static bool3 IsEdge(DynamicBuffer<Node> nodes, Triangle triangle)
	{
		int3 @int = math.abs(triangle.m_Indices - triangle.m_Indices.yzx);
		return (@int == 1) | (@int == nodes.Length - 1);
	}

	public static quaternion CalculateLabelRotation(float3 cameraRight)
	{
		float3 up = math.cross(cameraRight, math.up());
		return quaternion.LookRotation(new float3(0f, -1f, 0f), up);
	}

	public static float3 CalculateLabelPosition(Geometry geometry)
	{
		return geometry.m_CenterPosition;
	}

	public static float CalculateLabelScale(float3 cameraPosition, float3 labelPosition)
	{
		return math.max(0.01f, math.sqrt(math.distance(cameraPosition, labelPosition) * 0.001f));
	}

	public static float4x4 CalculateLabelMatrix(float3 cameraPosition, float3 labelPosition, quaternion labelRotation)
	{
		float num = CalculateLabelScale(cameraPosition, labelPosition);
		return float4x4.TRS(labelPosition, labelRotation, num);
	}

	public static bool CheckOption(District district, DistrictOption option)
	{
		return (district.m_OptionMask & (uint)(1 << (int)option)) != 0;
	}

	public static void ApplyModifier(ref float value, DynamicBuffer<DistrictModifier> modifiers, DistrictModifierType type)
	{
		if (modifiers.Length > (int)type)
		{
			float2 delta = modifiers[(int)type].m_Delta;
			value += delta.x;
			value += value * delta.y;
		}
	}

	public static bool HasOption(DistrictOptionData optionData, DistrictOption option)
	{
		return (optionData.m_OptionMask & (uint)(1 << (int)option)) != 0;
	}

	public static bool CheckServiceDistrict(Entity district, Entity service, BufferLookup<ServiceDistrict> serviceDistricts)
	{
		if (!serviceDistricts.TryGetBuffer(service, out var bufferData))
		{
			return true;
		}
		if (bufferData.Length == 0)
		{
			return true;
		}
		if (district == Entity.Null)
		{
			return false;
		}
		return CollectionUtils.ContainsValue(bufferData, new ServiceDistrict(district));
	}

	public static bool CheckServiceDistrict(Entity district1, Entity district2, Entity service, BufferLookup<ServiceDistrict> serviceDistricts)
	{
		if (!serviceDistricts.HasBuffer(service))
		{
			return true;
		}
		DynamicBuffer<ServiceDistrict> buffer = serviceDistricts[service];
		if (buffer.Length == 0)
		{
			return true;
		}
		if (district1 == Entity.Null && district2 == Entity.Null)
		{
			return false;
		}
		if (!CollectionUtils.ContainsValue(buffer, new ServiceDistrict(district1)))
		{
			return CollectionUtils.ContainsValue(buffer, new ServiceDistrict(district2));
		}
		return true;
	}

	public static bool CheckServiceDistrict(Entity building, DynamicBuffer<ServiceDistrict> serviceDistricts, ref ComponentLookup<CurrentDistrict> currentDistricts)
	{
		if (serviceDistricts.IsCreated && serviceDistricts.Length != 0 && currentDistricts.TryGetComponent(building, out var componentData))
		{
			return CollectionUtils.ContainsValue(serviceDistricts, new ServiceDistrict(componentData.m_District));
		}
		return true;
	}

	public static CollisionMask GetCollisionMask(AreaGeometryData areaGeometryData)
	{
		CollisionMask collisionMask = CollisionMask.OnGround | CollisionMask.Overground | CollisionMask.ExclusiveGround;
		if (areaGeometryData.m_Type != AreaType.Lot)
		{
			collisionMask |= CollisionMask.Underground;
		}
		return collisionMask;
	}

	public static bool TryGetRandomObjectLocation(ref Unity.Mathematics.Random random, ObjectGeometryData objectGeometryData, Area area, Geometry geometry, float extraRadius, DynamicBuffer<Node> nodes, DynamicBuffer<Triangle> triangles, NativeList<ObjectItem> objects, out Game.Objects.Transform transform)
	{
		transform.m_Position = GetRandomPosition(ref random, geometry, nodes, triangles);
		float num = (((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) == 0) ? (math.length(MathUtils.Size(objectGeometryData.m_Bounds.xz)) * 0.5f) : (objectGeometryData.m_Size.x * 0.5f));
		bool result = TryFitInside(ref transform.m_Position, num, extraRadius, area, nodes, objects, canOverride: true);
		if (objects.IsCreated)
		{
			float num2 = (num + extraRadius) * 0.5f;
			int num3 = 0;
			for (int i = 0; i < objects.Length; i++)
			{
				ObjectItem value = objects[i];
				if (value.m_Circle.radius < num2)
				{
					float num4 = num + value.m_Circle.radius;
					if (math.distancesq(transform.m_Position.xz, value.m_Circle.position) < num4 * num4)
					{
						objects[num3++] = value;
					}
				}
			}
			if (num3 < objects.Length)
			{
				objects.RemoveRange(num3, objects.Length - num3);
			}
		}
		transform.m_Rotation = GetRandomRotation(ref random, transform.m_Position, nodes);
		if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) == 0)
		{
			transform.m_Position.xz -= math.rotate(transform.m_Rotation, MathUtils.Center(objectGeometryData.m_Bounds)).xz;
		}
		return result;
	}

	public static float3 GetRandomPosition(ref Unity.Mathematics.Random random, Geometry geometry, DynamicBuffer<Node> nodes, DynamicBuffer<Triangle> triangles)
	{
		float num = random.NextFloat(geometry.m_SurfaceArea);
		for (int i = 0; i < triangles.Length; i++)
		{
			Triangle3 triangle = GetTriangle3(nodes, triangles[i]);
			num -= MathUtils.Area(triangle.xz);
			if (num <= 0f)
			{
				float2 @float = random.NextFloat2(1f);
				@float = math.select(@float, 1f - @float, math.csum(@float) > 1f);
				return MathUtils.Position(triangle, @float);
			}
		}
		if (nodes.Length >= 2)
		{
			return math.lerp(nodes[0].m_Position, nodes[1].m_Position, random.NextFloat(1f));
		}
		if (nodes.Length == 1)
		{
			return nodes[0].m_Position;
		}
		return default(float3);
	}

	public static quaternion GetRandomRotation(ref Unity.Mathematics.Random random, float3 position, DynamicBuffer<Node> nodes)
	{
		float2 value = default(float2);
		float num = float.MaxValue;
		Line2.Segment line = default(Line2.Segment);
		line.a = nodes[nodes.Length - 1].m_Position.xz;
		for (int i = 0; i < nodes.Length; i++)
		{
			line.b = nodes[i].m_Position.xz;
			float t;
			float num2 = MathUtils.DistanceSquared(line, position.xz, out t);
			if (num2 < num)
			{
				value = line.b - line.a;
				num = num2;
			}
			line.a = line.b;
		}
		float num3;
		if (MathUtils.TryNormalize(ref value))
		{
			num3 = math.atan2(value.x, value.y);
			num3 += (float)random.NextInt(4) * (MathF.PI / 2f);
		}
		else
		{
			num3 = random.NextFloat(MathF.PI * 2f);
		}
		return quaternion.RotateY(num3);
	}

	public static bool TryFitInside(ref float3 position, float radius, float extraRadius, Area area, DynamicBuffer<Node> nodes, NativeList<ObjectItem> objects, bool canOverride = false)
	{
		float num = radius + extraRadius;
		num *= num;
		bool flag = false;
		Line2.Segment line = default(Line2.Segment);
		line.a = nodes[nodes.Length - 1].m_Position.xz;
		for (int i = 0; i < nodes.Length; i++)
		{
			line.b = nodes[i].m_Position.xz;
			if (MathUtils.DistanceSquared(line, position.xz, out var t) < num)
			{
				float2 @float = math.normalizesafe(line.b - line.a);
				float2 y = position.xz - MathUtils.Position(line, t);
				float num2 = math.dot(@float, y);
				@float = (((area.m_Flags & AreaFlags.CounterClockwise) == 0) ? MathUtils.Right(@float) : MathUtils.Left(@float));
				@float *= math.sqrt(num - num2 * num2) - math.dot(@float, y) + 0.01f;
				position.xz += @float;
				flag = true;
			}
			line.a = line.b;
		}
		if (objects.IsCreated)
		{
			float num3 = (radius + extraRadius) * 0.5f;
			for (int j = 0; j < objects.Length; j++)
			{
				ObjectItem objectItem = objects[j];
				if (!canOverride || !(objectItem.m_Circle.radius < num3))
				{
					float num4 = radius + objectItem.m_Circle.radius;
					float num5 = math.distancesq(position.xz, objectItem.m_Circle.position);
					if (num5 < num4 * num4)
					{
						float x = math.sqrt(num5);
						float2 float2 = (objectItem.m_Circle.position - position.xz) * (num4 / math.max(x, 0.01f) - 1f);
						position.xz += float2;
						flag = true;
					}
				}
			}
		}
		if (flag)
		{
			if (!IntersectEdges(position, radius, extraRadius, nodes))
			{
				return !IntersectObjects(position, radius, extraRadius, objects, canOverride);
			}
			return false;
		}
		return true;
	}

	public static bool IntersectArea(float3 position, float radius, DynamicBuffer<Node> nodes, DynamicBuffer<Triangle> triangles)
	{
		Circle2 circle = new Circle2(radius, position.xz);
		for (int i = 0; i < triangles.Length; i++)
		{
			if (MathUtils.Intersect(GetTriangle2(nodes, triangles[i]), circle))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IntersectEdges(float3 position, float radius, float extraRadius, DynamicBuffer<Node> nodes)
	{
		float num = radius + extraRadius;
		num *= num;
		Line2.Segment line = default(Line2.Segment);
		line.a = nodes[nodes.Length - 1].m_Position.xz;
		for (int i = 0; i < nodes.Length; i++)
		{
			line.b = nodes[i].m_Position.xz;
			if (MathUtils.DistanceSquared(line, position.xz, out var _) < num)
			{
				return true;
			}
			line.a = line.b;
		}
		return false;
	}

	public static bool IntersectObjects(float3 position, float radius, float extraRadius, NativeList<ObjectItem> objects, bool canOverride = false)
	{
		if (objects.IsCreated)
		{
			float num = (radius + extraRadius) * 0.5f;
			for (int i = 0; i < objects.Length; i++)
			{
				ObjectItem objectItem = objects[i];
				if (!canOverride || !(objectItem.m_Circle.radius < num))
				{
					float num2 = radius + objectItem.m_Circle.radius;
					if (math.distancesq(position.xz, objectItem.m_Circle.position) < num2 * num2)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public static float GetMinNodeDistance(AreaType areaType)
	{
		return areaType switch
		{
			AreaType.Lot => 8f, 
			AreaType.District => 32f, 
			AreaType.MapTile => 64f, 
			AreaType.Space => 1f, 
			AreaType.Surface => 0.75f, 
			_ => 1f, 
		};
	}

	public static AreaTypeMask GetTypeMask(AreaType type)
	{
		if (type != AreaType.None)
		{
			return (AreaTypeMask)(1 << (int)type);
		}
		return AreaTypeMask.None;
	}

	public static float3 GetExpandedNode(DynamicBuffer<Node> nodes, int index, float expandAmount, bool isComplete, bool isCounterClockwise)
	{
		if (!isComplete)
		{
			if (nodes.Length == 1)
			{
				return nodes[index].m_Position;
			}
			if (index == 0)
			{
				float2 xz = nodes[math.select(index + 1, 0, index == nodes.Length - 1)].m_Position.xz;
				float3 position = nodes[index].m_Position;
				float2 @float = math.normalizesafe(xz - position.xz);
				float2 float2 = math.select(MathUtils.Left(@float), MathUtils.Right(@float), isCounterClockwise) - @float;
				position.xz += float2 * expandAmount;
				return position;
			}
			if (index == nodes.Length - 1)
			{
				float2 xz2 = nodes[math.select(index - 1, nodes.Length - 1, index == 0)].m_Position.xz;
				float3 position2 = nodes[index].m_Position;
				float2 float3 = math.normalizesafe(xz2 - position2.xz);
				float2 float4 = math.select(MathUtils.Right(float3), MathUtils.Left(float3), isCounterClockwise) - float3;
				position2.xz += float4 * expandAmount;
				return position2;
			}
		}
		float2 xz3 = nodes[math.select(index - 1, nodes.Length - 1, index == 0)].m_Position.xz;
		float2 xz4 = nodes[math.select(index + 1, 0, index == nodes.Length - 1)].m_Position.xz;
		float3 position3 = nodes[index].m_Position;
		float2 float5 = math.normalizesafe(xz3 - position3.xz);
		float2 y = math.normalizesafe(xz4 - position3.xz);
		float2 float6 = math.select(MathUtils.Right(float5), MathUtils.Left(float5), isCounterClockwise);
		float num = math.acos(math.clamp(math.dot(float5, y), -1f, 1f));
		float num2 = math.sign(math.dot(float6, y));
		float num3 = math.tan(num * 0.5f);
		float6 += float5 * math.select(num2 / num3, 0f, num3 < 0.001f);
		position3.xz += float6 * expandAmount;
		return position3;
	}

	public static float3 GetExpandedNode(NativeArray<SubAreaNode> nodes, int index, float expandAmount, bool isComplete, bool isCounterClockwise)
	{
		if (!isComplete)
		{
			if (nodes.Length == 1)
			{
				return nodes[index].m_Position;
			}
			if (index == 0)
			{
				float2 xz = nodes[math.select(index + 1, 0, index == nodes.Length - 1)].m_Position.xz;
				float3 position = nodes[index].m_Position;
				float2 @float = math.normalizesafe(xz - position.xz);
				float2 float2 = math.select(MathUtils.Left(@float), MathUtils.Right(@float), isCounterClockwise) - @float;
				position.xz += float2 * expandAmount;
				return position;
			}
			if (index == nodes.Length - 1)
			{
				float2 xz2 = nodes[math.select(index - 1, nodes.Length - 1, index == 0)].m_Position.xz;
				float3 position2 = nodes[index].m_Position;
				float2 float3 = math.normalizesafe(xz2 - position2.xz);
				float2 float4 = math.select(MathUtils.Right(float3), MathUtils.Left(float3), isCounterClockwise) - float3;
				position2.xz += float4 * expandAmount;
				return position2;
			}
		}
		float2 xz3 = nodes[math.select(index - 1, nodes.Length - 1, index == 0)].m_Position.xz;
		float2 xz4 = nodes[math.select(index + 1, 0, index == nodes.Length - 1)].m_Position.xz;
		float3 position3 = nodes[index].m_Position;
		float2 float5 = math.normalizesafe(xz3 - position3.xz);
		float2 y = math.normalizesafe(xz4 - position3.xz);
		float2 float6 = math.select(MathUtils.Right(float5), MathUtils.Left(float5), isCounterClockwise);
		float num = math.acos(math.clamp(math.dot(float5, y), -1f, 1f));
		float num2 = math.sign(math.dot(float6, y));
		float num3 = math.tan(num * 0.5f);
		float6 += float5 * math.select(num2 / num3, 0f, num3 < 0.001f);
		position3.xz += float6 * expandAmount;
		return position3;
	}

	public static float3 GetExpandedNode<TNodeList>(TNodeList nodes, int index, int prevIndex, int nextIndex, float expandAmount, bool isCounterClockwise) where TNodeList : INativeList<Node>
	{
		float2 xz = nodes[prevIndex].m_Position.xz;
		float2 xz2 = nodes[nextIndex].m_Position.xz;
		float3 position = nodes[index].m_Position;
		float2 @float = math.normalizesafe(xz - position.xz);
		float2 y = math.normalizesafe(xz2 - position.xz);
		float2 float2 = math.select(MathUtils.Right(@float), MathUtils.Left(@float), isCounterClockwise);
		float num = math.acos(math.clamp(math.dot(@float, y), -1f, 1f));
		float num2 = math.sign(math.dot(float2, y));
		float num3 = math.tan(num * 0.5f);
		float2 += @float * math.select(num2 / num3, 0f, num3 < 0.001f);
		position.xz += float2 * expandAmount;
		return position;
	}

	public static bool SelectAreaPrefab(DynamicBuffer<PlaceholderObjectElement> placeholderElements, ComponentLookup<SpawnableObjectData> spawnableDatas, NativeParallelHashMap<Entity, int> selectedSpawnables, ref Unity.Mathematics.Random random, out Entity result, out int seed)
	{
		int num = 0;
		bool flag = false;
		result = Entity.Null;
		seed = 0;
		for (int i = 0; i < placeholderElements.Length; i++)
		{
			PlaceholderObjectElement placeholderObjectElement = placeholderElements[i];
			SpawnableObjectData spawnableObjectData = spawnableDatas[placeholderObjectElement.m_Object];
			int item = 0;
			if (selectedSpawnables.IsCreated && selectedSpawnables.TryGetValue(placeholderObjectElement.m_Object, out item))
			{
				if (!flag)
				{
					num = 0;
					flag = true;
				}
			}
			else if (flag)
			{
				continue;
			}
			num += spawnableObjectData.m_Probability;
			if (random.NextInt(num) < spawnableObjectData.m_Probability)
			{
				result = placeholderObjectElement.m_Object;
				seed = (flag ? item : random.NextInt());
			}
		}
		if (result != Entity.Null)
		{
			if (!flag && selectedSpawnables.IsCreated)
			{
				selectedSpawnables.Add(result, seed);
			}
			return true;
		}
		return false;
	}

	public static void FindAreaPath(ref Unity.Mathematics.Random random, NativeList<PathElement> path, DynamicBuffer<Game.Net.SubLane> lanes, Entity startEntity, float startCurvePos, Entity endEntity, float endCurvePos, ComponentLookup<Lane> laneData, ComponentLookup<Curve> curveData)
	{
		if (startEntity == endEntity)
		{
			path.Add(new PathElement(startEntity, new float2(startCurvePos, endCurvePos)));
			return;
		}
		NativeParallelMultiHashMap<PathNode, Entity> nativeParallelMultiHashMap = new NativeParallelMultiHashMap<PathNode, Entity>(lanes.Length * 2, Allocator.Temp);
		NativeParallelHashMap<PathNode, PathElement> nativeParallelHashMap = new NativeParallelHashMap<PathNode, PathElement>(lanes.Length + 1, Allocator.Temp);
		NativeMinHeap<FixPathItem> nativeMinHeap = new NativeMinHeap<FixPathItem>(lanes.Length, Allocator.Temp);
		for (int i = 0; i < lanes.Length; i++)
		{
			Entity subLane = lanes[i].m_SubLane;
			Lane lane = laneData[subLane];
			nativeParallelMultiHashMap.Add(lane.m_StartNode, subLane);
			nativeParallelMultiHashMap.Add(lane.m_EndNode, subLane);
		}
		Lane lane2 = laneData[endEntity];
		Curve curve = curveData[endEntity];
		float cost = random.NextFloat(0.5f, 1f) * curve.m_Length * endCurvePos;
		float cost2 = random.NextFloat(0.5f, 1f) * curve.m_Length * (1f - endCurvePos);
		nativeMinHeap.Insert(new FixPathItem(lane2.m_StartNode, new PathElement(endEntity, new float2(0f, endCurvePos)), cost));
		nativeMinHeap.Insert(new FixPathItem(lane2.m_EndNode, new PathElement(endEntity, new float2(1f, endCurvePos)), cost2));
		while (nativeMinHeap.Length != 0)
		{
			FixPathItem fixPathItem = nativeMinHeap.Extract();
			if (!nativeParallelHashMap.TryAdd(fixPathItem.m_Node, fixPathItem.m_PathElement))
			{
				continue;
			}
			if (fixPathItem.m_PathElement.m_Target == startEntity)
			{
				path.Add(in fixPathItem.m_PathElement);
				Lane lane3 = laneData[startEntity];
				PathNode key = ((fixPathItem.m_PathElement.m_TargetDelta.y == 0f) ? lane3.m_StartNode : lane3.m_EndNode);
				PathElement item;
				while (nativeParallelHashMap.TryGetValue(key, out item))
				{
					path.Add(in item);
					if (item.m_Target == endEntity)
					{
						break;
					}
					lane3 = laneData[item.m_Target];
					key = ((item.m_TargetDelta.y == 0f) ? lane3.m_StartNode : lane3.m_EndNode);
				}
				break;
			}
			if (!nativeParallelMultiHashMap.TryGetFirstValue(fixPathItem.m_Node, out var item2, out var it))
			{
				continue;
			}
			do
			{
				if (item2 == fixPathItem.m_PathElement.m_Target)
				{
					continue;
				}
				Lane lane4 = laneData[item2];
				Curve curve2 = curveData[item2];
				if (lane4.m_EndNode.Equals(fixPathItem.m_Node))
				{
					if (item2 == startEntity)
					{
						float num = random.NextFloat(0.5f, 1f) * curve2.m_Length * (1f - startCurvePos);
						nativeMinHeap.Insert(new FixPathItem(lane4.m_MiddleNode, new PathElement(startEntity, new float2(startCurvePos, 1f)), fixPathItem.m_Cost + num));
					}
					else if (!nativeParallelHashMap.ContainsKey(lane4.m_StartNode))
					{
						float num2 = random.NextFloat(0.5f, 1f) * curve2.m_Length;
						nativeMinHeap.Insert(new FixPathItem(lane4.m_StartNode, new PathElement(item2, new float2(0f, 1f)), fixPathItem.m_Cost + num2));
					}
				}
				else if (lane4.m_StartNode.Equals(fixPathItem.m_Node))
				{
					if (item2 == startEntity)
					{
						float num3 = random.NextFloat(0.5f, 1f) * curve2.m_Length * startCurvePos;
						nativeMinHeap.Insert(new FixPathItem(lane4.m_MiddleNode, new PathElement(startEntity, new float2(startCurvePos, 0f)), fixPathItem.m_Cost + num3));
					}
					else if (!nativeParallelHashMap.ContainsKey(lane4.m_EndNode))
					{
						float num4 = random.NextFloat(0.5f, 1f) * curve2.m_Length;
						nativeMinHeap.Insert(new FixPathItem(lane4.m_EndNode, new PathElement(item2, new float2(1f, 0f)), fixPathItem.m_Cost + num4));
					}
				}
			}
			while (nativeParallelMultiHashMap.TryGetNextValue(out item2, ref it));
		}
		nativeParallelMultiHashMap.Dispose();
		nativeParallelHashMap.Dispose();
		nativeMinHeap.Dispose();
	}

	public static Node AdjustPosition(Node node, ref TerrainHeightData terrainHeightData, ref WaterSurfaceData<SurfaceWater> waterSurfaceData)
	{
		Node result = node;
		result.m_Position.y = WaterUtils.SampleHeight(ref waterSurfaceData, ref terrainHeightData, node.m_Position);
		return result;
	}

	public static Node AdjustPosition(Node node, ref TerrainHeightData terrainHeightData)
	{
		Node result = node;
		result.m_Position.y = TerrainUtils.SampleHeight(ref terrainHeightData, node.m_Position);
		return result;
	}

	public static void SetCollisionFlags(ref AreaGeometryData areaGeometryData, bool ignoreMarkers)
	{
		if (!ignoreMarkers)
		{
			AreaType type = areaGeometryData.m_Type;
			if ((uint)(type - 3) <= 1u)
			{
				areaGeometryData.m_Flags |= GeometryFlags.PhysicalGeometry;
			}
		}
	}

	public static bool CheckOption(BorderDistrict borderDistrict, DistrictOption districtOption, ref ComponentLookup<District> districtData)
	{
		bool2 @bool = false;
		if (districtData.TryGetComponent(borderDistrict.m_Left, out var componentData))
		{
			if (CheckOption(componentData, districtOption))
			{
				@bool.x = true;
			}
			else
			{
				@bool.y = true;
			}
		}
		if (districtData.TryGetComponent(borderDistrict.m_Right, out var componentData2))
		{
			if (CheckOption(componentData2, districtOption))
			{
				@bool.x = true;
			}
			else
			{
				@bool.y = true;
			}
		}
		return @bool.x & !@bool.y;
	}
}
