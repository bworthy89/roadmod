using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Objects;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Tools;

public static class ClearAreaHelpers
{
	public static void FillClearAreas(DynamicBuffer<InstalledUpgrade> installedUpgrades, Entity ignoreUpgradeOrArea, ComponentLookup<Transform> transformData, ComponentLookup<Clear> clearAreaData, ComponentLookup<PrefabRef> prefabRefData, ComponentLookup<ObjectGeometryData> prefabObjectGeometryData, BufferLookup<Game.Areas.SubArea> subAreaBuffers, BufferLookup<Node> nodeBuffers, BufferLookup<Triangle> triangleBuffers, ref NativeList<ClearAreaData> clearAreas)
	{
		for (int i = 0; i < installedUpgrades.Length; i++)
		{
			Entity upgrade = installedUpgrades[i].m_Upgrade;
			if (!(upgrade == ignoreUpgradeOrArea) && subAreaBuffers.TryGetBuffer(upgrade, out var bufferData))
			{
				Transform transform = transformData[upgrade];
				ObjectGeometryData objectGeometryData = prefabObjectGeometryData[prefabRefData[upgrade].m_Prefab];
				FillClearAreas(bufferData, transform, objectGeometryData, ignoreUpgradeOrArea, ref clearAreaData, ref nodeBuffers, ref triangleBuffers, ref clearAreas);
			}
		}
	}

	public static void FillClearAreas(DynamicBuffer<Game.Areas.SubArea> subAreas, Transform transform, ObjectGeometryData objectGeometryData, Entity ignoreArea, ref ComponentLookup<Clear> clearAreaData, ref BufferLookup<Node> nodeBuffers, ref BufferLookup<Triangle> triangleBuffers, ref NativeList<ClearAreaData> clearAreas)
	{
		for (int i = 0; i < subAreas.Length; i++)
		{
			Entity area = subAreas[i].m_Area;
			if (clearAreaData.HasComponent(area) && !(area == ignoreArea))
			{
				if (!clearAreas.IsCreated)
				{
					clearAreas = new NativeList<ClearAreaData>(16, Allocator.Temp);
				}
				DynamicBuffer<Node> nodes = nodeBuffers[area];
				DynamicBuffer<Triangle> dynamicBuffer = triangleBuffers[area];
				float topY = transform.m_Position.y + objectGeometryData.m_Bounds.max.y + 1f;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					clearAreas.Add(new ClearAreaData
					{
						m_Triangle = AreaUtils.GetTriangle3(nodes, dynamicBuffer[j]),
						m_TopY = topY
					});
				}
			}
		}
	}

	public static void FillClearAreas(Entity ownerPrefab, Transform ownerTransform, ComponentLookup<ObjectGeometryData> prefabObjectGeometryData, ComponentLookup<AreaGeometryData> prefabAreaGeometryData, BufferLookup<Game.Prefabs.SubArea> prefabSubAreas, BufferLookup<SubAreaNode> prefabSubAreaNodes, ref NativeList<ClearAreaData> clearAreas)
	{
		if (!prefabSubAreas.TryGetBuffer(ownerPrefab, out var bufferData))
		{
			return;
		}
		for (int i = 0; i < bufferData.Length; i++)
		{
			Game.Prefabs.SubArea subArea = bufferData[i];
			if ((prefabAreaGeometryData[subArea.m_Prefab].m_Flags & Game.Areas.GeometryFlags.ClearArea) != 0)
			{
				if (!clearAreas.IsCreated)
				{
					clearAreas = new NativeList<ClearAreaData>(16, Allocator.Temp);
				}
				int num = subArea.m_NodeRange.y - subArea.m_NodeRange.x;
				DynamicBuffer<SubAreaNode> dynamicBuffer = prefabSubAreaNodes[ownerPrefab];
				NativeArray<SubAreaNode> subArray = dynamicBuffer.AsNativeArray().GetSubArray(subArea.m_NodeRange.x, num);
				NativeArray<float3> nodes = new NativeArray<float3>(num, Allocator.Temp);
				NativeList<Triangle> triangles = new NativeList<Triangle>(Allocator.Temp);
				bool isCounterClockwise = GeometrySystem.Area(subArray) > 0f;
				for (int j = 0; j < num; j++)
				{
					nodes[j] = AreaUtils.GetExpandedNode(subArray, j, -0.1f, isComplete: true, isCounterClockwise);
				}
				GeometrySystem.Triangulate(nodes, triangles, default(NativeArray<Bounds2>), 0, isCounterClockwise);
				GeometrySystem.EqualizeTriangles(nodes, triangles);
				ObjectGeometryData objectGeometryData = prefabObjectGeometryData[ownerPrefab];
				float topY = ownerTransform.m_Position.y + objectGeometryData.m_Bounds.max.y + 1f;
				for (int k = 0; k < triangles.Length; k++)
				{
					int3 @int = triangles[k].m_Indices + subArea.m_NodeRange.x;
					clearAreas.Add(new ClearAreaData
					{
						m_Triangle = new Triangle3(ObjectUtils.LocalToWorld(ownerTransform, dynamicBuffer[@int.x].m_Position), ObjectUtils.LocalToWorld(ownerTransform, dynamicBuffer[@int.y].m_Position), ObjectUtils.LocalToWorld(ownerTransform, dynamicBuffer[@int.z].m_Position)),
						m_TopY = topY
					});
				}
				nodes.Dispose();
				triangles.Dispose();
			}
		}
	}

	public static void FillClearAreas(Entity ownerPrefab, Transform ownerTransform, DynamicBuffer<Node> nodes, bool isComplete, ComponentLookup<ObjectGeometryData> prefabObjectGeometryData, ref NativeList<ClearAreaData> clearAreas)
	{
		int num = nodes.Length;
		if (num >= 3)
		{
			if (num >= 4 && nodes[0].m_Position.Equals(nodes[num - 1].m_Position))
			{
				isComplete = true;
				num--;
			}
			if (!clearAreas.IsCreated)
			{
				clearAreas = new NativeList<ClearAreaData>(16, Allocator.Temp);
			}
			NativeArray<float3> nodes2 = new NativeArray<float3>(num, Allocator.Temp);
			NativeList<Triangle> triangles = new NativeList<Triangle>(Allocator.Temp);
			bool isCounterClockwise = GeometrySystem.Area(nodes) > 0f;
			for (int i = 0; i < num; i++)
			{
				nodes2[i] = AreaUtils.GetExpandedNode(nodes, i, -0.1f, isComplete, isCounterClockwise);
			}
			GeometrySystem.Triangulate(nodes2, triangles, default(NativeArray<Bounds2>), 0, isCounterClockwise);
			GeometrySystem.EqualizeTriangles(nodes2, triangles);
			ObjectGeometryData objectGeometryData = prefabObjectGeometryData[ownerPrefab];
			float topY = ownerTransform.m_Position.y + objectGeometryData.m_Bounds.max.y + 1f;
			for (int j = 0; j < triangles.Length; j++)
			{
				clearAreas.Add(new ClearAreaData
				{
					m_Triangle = AreaUtils.GetTriangle3(nodes, triangles[j]),
					m_TopY = topY
				});
			}
			nodes2.Dispose();
			triangles.Dispose();
		}
	}

	public static void TransformClearAreas(NativeList<ClearAreaData> clearAreas, Transform oldTransform, Transform newTransform)
	{
		if (clearAreas.IsCreated)
		{
			Transform inverseParentTransform = ObjectUtils.InverseTransform(oldTransform);
			for (int i = 0; i < clearAreas.Length; i++)
			{
				ClearAreaData value = clearAreas[i];
				value.m_Triangle.a = ObjectUtils.LocalToWorld(newTransform, ObjectUtils.WorldToLocal(inverseParentTransform, value.m_Triangle.a));
				value.m_Triangle.b = ObjectUtils.LocalToWorld(newTransform, ObjectUtils.WorldToLocal(inverseParentTransform, value.m_Triangle.b));
				value.m_Triangle.c = ObjectUtils.LocalToWorld(newTransform, ObjectUtils.WorldToLocal(inverseParentTransform, value.m_Triangle.c));
				value.m_TopY += newTransform.m_Position.y - oldTransform.m_Position.y;
				clearAreas[i] = value;
			}
		}
	}

	public static void InitClearAreas(NativeList<ClearAreaData> clearAreas, Transform topLevelTransform)
	{
		if (clearAreas.IsCreated)
		{
			for (int i = 0; i < clearAreas.Length; i++)
			{
				ClearAreaData value = clearAreas[i];
				value.m_OnGround = math.any(math.abs(value.m_Triangle.y.abc - topLevelTransform.m_Position.y) <= 1f);
				value.m_Triangle.y -= 1f;
				clearAreas[i] = value;
			}
		}
	}

	public static bool ShouldClear(NativeList<ClearAreaData> clearAreas, float3 position, bool onGround)
	{
		if (clearAreas.IsCreated)
		{
			for (int i = 0; i < clearAreas.Length; i++)
			{
				ClearAreaData clearAreaData = clearAreas[i];
				if (MathUtils.Intersect(clearAreaData.m_Triangle.xz, position.xz, out var t))
				{
					if (clearAreaData.m_OnGround && onGround)
					{
						return true;
					}
					float num = MathUtils.Position(clearAreaData.m_Triangle.y, t);
					float num2 = math.max(clearAreaData.m_TopY, num + 2f);
					if (position.y >= num && position.y <= num2)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public static bool ShouldClear(NativeList<ClearAreaData> clearAreas, Bezier4x3 curve, bool onGround)
	{
		if (clearAreas.IsCreated)
		{
			Bounds3 bounds = MathUtils.Bounds(curve);
			Line3.Segment line = default(Line3.Segment);
			for (int i = 0; i < clearAreas.Length; i++)
			{
				ClearAreaData clearAreaData = clearAreas[i];
				if (!MathUtils.Intersect(MathUtils.Bounds(clearAreaData.m_Triangle.xz), bounds.xz))
				{
					continue;
				}
				line.a = curve.a;
				for (int j = 1; j <= 16; j++)
				{
					line.b = MathUtils.Position(curve, (float)j * 0.0625f);
					if (MathUtils.Intersect(clearAreaData.m_Triangle.xz, line.xz, out var t))
					{
						if (clearAreaData.m_OnGround && onGround)
						{
							return true;
						}
						float3 @float = MathUtils.Position(line, math.csum(t) * 0.5f);
						if (MathUtils.Intersect(clearAreaData.m_Triangle.xz, @float.xz, out t))
						{
							float num = MathUtils.Position(clearAreaData.m_Triangle.y, t);
							float num2 = math.max(clearAreaData.m_TopY, num + 2f);
							if (@float.y >= num && @float.y <= num2)
							{
								return true;
							}
						}
					}
					line.a = line.b;
				}
			}
		}
		return false;
	}

	public static bool ShouldClear(NativeList<ClearAreaData> clearAreas, DynamicBuffer<Node> nodes, DynamicBuffer<Triangle> triangles, Transform ownerTransform)
	{
		if (clearAreas.IsCreated)
		{
			for (int i = 0; i < triangles.Length; i++)
			{
				Triangle3 triangle = AreaUtils.GetTriangle3(nodes, triangles[i]);
				if (ShouldClear(clearAreas, triangle, ownerTransform))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool ShouldClear(NativeList<ClearAreaData> clearAreas, DynamicBuffer<SubAreaNode> subAreaNodes, int2 nodeRange, Transform ownerTransform)
	{
		if (clearAreas.IsCreated)
		{
			int num = nodeRange.y - nodeRange.x;
			NativeArray<SubAreaNode> subArray = subAreaNodes.AsNativeArray().GetSubArray(nodeRange.x, num);
			NativeArray<float3> nodes = new NativeArray<float3>(num, Allocator.Temp);
			NativeList<Triangle> triangles = new NativeList<Triangle>(Allocator.Temp);
			bool isCounterClockwise = GeometrySystem.Area(subArray) > 0f;
			for (int i = 0; i < num; i++)
			{
				nodes[i] = AreaUtils.GetExpandedNode(subArray, i, -0.1f, isComplete: true, isCounterClockwise);
			}
			GeometrySystem.Triangulate(nodes, triangles, default(NativeArray<Bounds2>), 0, isCounterClockwise);
			GeometrySystem.EqualizeTriangles(nodes, triangles);
			for (int j = 0; j < triangles.Length; j++)
			{
				int3 @int = triangles[j].m_Indices + nodeRange.x;
				if (ShouldClear(triangle: new Triangle3(ObjectUtils.LocalToWorld(ownerTransform, subAreaNodes[@int.x].m_Position), ObjectUtils.LocalToWorld(ownerTransform, subAreaNodes[@int.y].m_Position), ObjectUtils.LocalToWorld(ownerTransform, subAreaNodes[@int.z].m_Position)), clearAreas: clearAreas, ownerTransform: ownerTransform))
				{
					nodes.Dispose();
					triangles.Dispose();
					return true;
				}
			}
			nodes.Dispose();
			triangles.Dispose();
		}
		return false;
	}

	private static bool ShouldClear(NativeList<ClearAreaData> clearAreas, Triangle3 triangle, Transform ownerTransform)
	{
		Bounds3 bounds = MathUtils.Bounds(triangle);
		bool flag = math.any(math.abs(triangle.y.abc - ownerTransform.m_Position.y) <= 1f);
		for (int i = 0; i < clearAreas.Length; i++)
		{
			ClearAreaData clearAreaData = clearAreas[i];
			Bounds3 bounds2 = MathUtils.Bounds(clearAreaData.m_Triangle);
			if (MathUtils.Intersect(bounds.xz, bounds2.xz) && MathUtils.Intersect(triangle.xz, clearAreaData.m_Triangle.xz))
			{
				if (clearAreaData.m_OnGround && flag)
				{
					return true;
				}
				float y = bounds2.min.y;
				float num = math.max(clearAreaData.m_TopY, y + 2f);
				if (bounds.max.y >= y && bounds.min.y <= num)
				{
					return true;
				}
			}
		}
		return false;
	}
}
