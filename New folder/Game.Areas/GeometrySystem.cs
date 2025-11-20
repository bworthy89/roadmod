using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Areas;

[CompilerGenerated]
public class GeometrySystem : GameSystemBase
{
	[BurstCompile]
	private struct TriangulateAreasJob : IJobParallelForDefer
	{
		[ReadOnly]
		public ComponentLookup<Space> m_SpaceData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<TerrainAreaData> m_PrefabTerrainAreaData;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> m_PrefabAreaGeometryData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Area> m_AreaData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Geometry> m_GeometryData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Node> m_Nodes;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Triangle> m_Triangles;

		[ReadOnly]
		public bool m_Loaded;

		[ReadOnly]
		public NativeArray<Entity> m_Entities;

		[ReadOnly]
		public NativeList<Entity> m_Buildings;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		public void Execute(int index)
		{
			Entity entity = m_Entities[index];
			Area area = m_AreaData[entity];
			DynamicBuffer<Node> nodes = m_Nodes[entity];
			DynamicBuffer<Triangle> triangles = m_Triangles[entity];
			if ((area.m_Flags & AreaFlags.Slave) != 0 && m_UpdatedData.HasComponent(entity))
			{
				GenerateSlaveArea(entity, ref area, nodes);
			}
			bool isComplete = (area.m_Flags & AreaFlags.Complete) != 0;
			bool flag = Area(nodes) > 0f;
			NativeArray<float3> nativeArray = new NativeArray<float3>(nodes.Length, Allocator.Temp);
			for (int i = 0; i < nodes.Length; i++)
			{
				nativeArray[i] = AreaUtils.GetExpandedNode(nodes, i, -0.1f, isComplete, flag);
			}
			NativeArray<Bounds2> edgeBounds = default(NativeArray<Bounds2>);
			int totalDepth = 0;
			if (nodes.Length > 20)
			{
				BuildEdgeBounds(nodes, nativeArray, out edgeBounds, out totalDepth);
			}
			Triangulate(nativeArray, triangles, edgeBounds, totalDepth, flag);
			EqualizeTriangles(nativeArray, triangles);
			nativeArray.Dispose();
			if (flag)
			{
				area.m_Flags |= AreaFlags.CounterClockwise;
			}
			else
			{
				area.m_Flags &= ~AreaFlags.CounterClockwise;
			}
			if (triangles.Length == 0)
			{
				area.m_Flags |= AreaFlags.NoTriangles;
			}
			else
			{
				area.m_Flags &= ~AreaFlags.NoTriangles;
			}
			m_AreaData[entity] = area;
			if (m_GeometryData.HasComponent(entity))
			{
				bool flag2 = !m_SpaceData.HasComponent(entity);
				bool useWaterHeight = false;
				bool useTriangleHeight = !flag2 || HasBuildingOwner(entity);
				float heightOffset = 0f;
				float nodeDistance = 0f;
				float lodBias = 0f;
				PrefabRef prefabRef = m_PrefabRefData[entity];
				if (m_PrefabTerrainAreaData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
				{
					heightOffset = componentData.m_HeightOffset;
				}
				if (m_PrefabAreaGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
				{
					useWaterHeight = (componentData2.m_Flags & GeometryFlags.OnWaterSurface) != 0;
					nodeDistance = AreaUtils.GetMinNodeDistance(componentData2.m_Type);
					lodBias = componentData2.m_LodBias;
				}
				UpdateHeightRange(nodes, triangles, flag2, useWaterHeight, useTriangleHeight, heightOffset);
				m_GeometryData[entity] = CalculateGeometry(nodes, triangles, edgeBounds, totalDepth, nodeDistance, lodBias, flag2, useWaterHeight);
			}
			if (edgeBounds.IsCreated)
			{
				edgeBounds.Dispose();
			}
		}

		private void GenerateSlaveArea(Entity entity, ref Area area, DynamicBuffer<Node> nodes)
		{
			nodes.Clear();
			if (!m_OwnerData.TryGetComponent(entity, out var componentData) || !m_AreaData.TryGetComponent(componentData.m_Owner, out var componentData2) || !m_Nodes.TryGetBuffer(componentData.m_Owner, out var bufferData) || bufferData.Length < 3)
			{
				return;
			}
			if ((componentData2.m_Flags & AreaFlags.Complete) != 0)
			{
				area.m_Flags |= AreaFlags.Complete;
			}
			else
			{
				area.m_Flags &= ~AreaFlags.Complete;
			}
			nodes.CopyFrom(bufferData);
			bool isCounterClockwise = Area(nodes) > 0f;
			NativeList<Node> extraNodes = new NativeList<Node>(128, Allocator.Temp);
			NativeList<int2> extraRanges = new NativeList<int2>(32, Allocator.Temp);
			if (m_SubObjects.TryGetBuffer(componentData.m_Owner, out var bufferData2))
			{
				for (int i = 0; i < bufferData2.Length; i++)
				{
					Game.Objects.SubObject subObject = bufferData2[i];
					if (m_BuildingData.HasComponent(subObject.m_SubObject) && !m_DeletedData.HasComponent(subObject.m_SubObject))
					{
						AddObjectHole(subObject.m_SubObject, extraNodes, extraRanges, isCounterClockwise);
					}
				}
			}
			for (int j = 0; j < m_Buildings.Length; j++)
			{
				Entity entity2 = m_Buildings[j];
				if (!(m_OwnerData[entity2].m_Owner != componentData.m_Owner))
				{
					AddObjectHole(entity2, extraNodes, extraRanges, isCounterClockwise);
				}
			}
			for (int num = extraRanges.Length - 1; num >= 0; num--)
			{
				int2 @int = extraRanges[num];
				int3 int2 = -1;
				float num2 = float.MaxValue;
				for (int k = @int.x; k < @int.y; k++)
				{
					Node node = extraNodes[k];
					for (int l = 0; l < nodes.Length; l++)
					{
						Node node2 = nodes[l];
						Line2.Segment newEdge = new Line2.Segment(node.m_Position.xz, node2.m_Position.xz);
						float num3 = math.distancesq(newEdge.a, newEdge.b);
						if (num3 < num2 && CanAddEdge(newEdge, nodes, extraNodes, extraRanges, num, new int4(-1, l, num, k)))
						{
							num2 = num3;
							int2 = new int3(k, -1, l);
						}
					}
					for (int m = 0; m < num; m++)
					{
						int2 int3 = extraRanges[m];
						for (int n = int3.x; n < int3.y; n++)
						{
							Node node3 = extraNodes[n];
							Line2.Segment newEdge2 = new Line2.Segment(node.m_Position.xz, node3.m_Position.xz);
							float num4 = math.distancesq(newEdge2.a, newEdge2.b);
							if (num4 < num2 && CanAddEdge(newEdge2, nodes, extraNodes, extraRanges, num, new int4(m, n, num, k)))
							{
								num2 = num4;
								int2 = new int3(k, m, n);
							}
						}
					}
				}
				if (int2.x != -1)
				{
					int num5 = 2 + @int.y - @int.x;
					if (int2.y == -1)
					{
						Node value = nodes[int2.z];
						int num6 = math.select(int2.z, nodes.Length, int2.z == 0);
						nodes.ResizeUninitialized(nodes.Length + num5);
						for (int num7 = nodes.Length - num5 - 1; num7 >= num6; num7--)
						{
							nodes[num7 + num5] = nodes[num7];
						}
						nodes[num6++] = value;
						for (int num8 = int2.x; num8 < @int.y; num8++)
						{
							nodes[num6++] = extraNodes[num8];
						}
						for (int num9 = @int.x; num9 <= int2.x; num9++)
						{
							nodes[num6++] = extraNodes[num9];
						}
					}
					else
					{
						int2 value2 = extraRanges[int2.y];
						Node value3 = extraNodes[int2.z];
						int num10 = math.select(int2.z, value2.y, int2.z == value2.x);
						int2.x += num5;
						@int += num5;
						value2.y += num5;
						extraRanges[int2.y] = value2;
						extraNodes.ResizeUninitialized(@int.y);
						for (int num11 = @int.y - num5 - 1; num11 >= num10; num11--)
						{
							extraNodes[num11 + num5] = extraNodes[num11];
						}
						for (int num12 = int2.y + 1; num12 < num; num12++)
						{
							extraRanges[num12] += num5;
						}
						extraNodes[num10++] = value3;
						for (int num13 = int2.x; num13 < @int.y; num13++)
						{
							extraNodes[num10++] = extraNodes[num13];
						}
						for (int num14 = @int.x; num14 <= int2.x; num14++)
						{
							extraNodes[num10++] = extraNodes[num14];
						}
					}
				}
			}
			extraNodes.Dispose();
			extraRanges.Dispose();
		}

		private void AddObjectHole(Entity objectEntity, NativeList<Node> extraNodes, NativeList<int2> extraRanges, bool isCounterClockwise)
		{
			Transform transform = m_TransformData[objectEntity];
			PrefabRef prefabRef = m_PrefabRefData[objectEntity];
			ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
			Quad3 quad = ObjectUtils.CalculateBaseCorners(transform.m_Position, transform.m_Rotation, objectGeometryData.m_Bounds);
			int2 value = extraNodes.Length;
			if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
			{
				float num = objectGeometryData.m_Size.x * 0.5f;
				float num2 = MathF.PI / 4f;
				num2 = math.select(num2, 0f - num2, isCounterClockwise);
				for (int i = 0; i < 8; i++)
				{
					float x = (float)i * num2;
					Node value2 = new Node(transform.m_Position, float.MinValue);
					value2.m_Position.x += math.cos(x) * num;
					value2.m_Position.z += math.sin(x) * num;
					extraNodes.Add(in value2);
				}
			}
			else if (isCounterClockwise)
			{
				extraNodes.Add(new Node(quad.a, float.MinValue));
				extraNodes.Add(new Node(quad.b, float.MinValue));
				extraNodes.Add(new Node(quad.c, float.MinValue));
				extraNodes.Add(new Node(quad.d, float.MinValue));
			}
			else
			{
				extraNodes.Add(new Node(quad.b, float.MinValue));
				extraNodes.Add(new Node(quad.a, float.MinValue));
				extraNodes.Add(new Node(quad.d, float.MinValue));
				extraNodes.Add(new Node(quad.c, float.MinValue));
			}
			value.y = extraNodes.Length;
			extraRanges.Add(in value);
		}

		private bool CanAddEdge(Line2.Segment newEdge, DynamicBuffer<Node> nodes, NativeList<Node> extraNodes, NativeList<int2> extraRanges, int extraRangeLimit, int4 ignoreIndex)
		{
			Line2.Segment line = default(Line2.Segment);
			line.a = nodes[nodes.Length - 1].m_Position.xz;
			float2 t;
			for (int i = 0; i < nodes.Length; i++)
			{
				line.b = nodes[i].m_Position.xz;
				if (MathUtils.Intersect(line, newEdge, out t) && ((ignoreIndex.x != -1) | ((i != ignoreIndex.y) & (math.select(i, nodes.Length, i == 0) - 1 != ignoreIndex.y))))
				{
					return false;
				}
				line.a = line.b;
			}
			for (int j = 0; j <= extraRangeLimit; j++)
			{
				int2 @int = extraRanges[j];
				line.a = extraNodes[@int.y - 1].m_Position.xz;
				for (int k = @int.x; k < @int.y; k++)
				{
					line.b = extraNodes[k].m_Position.xz;
					if (MathUtils.Intersect(line, newEdge, out t) && math.all((ignoreIndex.xz != j) | ((k != ignoreIndex.yw) & (math.select(k, @int.y, k == @int.x) - 1 != ignoreIndex.yw))))
					{
						return false;
					}
					line.a = line.b;
				}
			}
			return true;
		}

		private bool HasBuildingOwner(Entity entity)
		{
			while (m_OwnerData.HasComponent(entity))
			{
				entity = m_OwnerData[entity].m_Owner;
				if (m_BuildingData.HasComponent(entity))
				{
					return true;
				}
			}
			return false;
		}

		private void UpdateHeightRange(DynamicBuffer<Node> nodes, DynamicBuffer<Triangle> triangles, bool useTerrainHeight, bool useWaterHeight, bool useTriangleHeight, float heightOffset)
		{
			for (int i = 0; i < triangles.Length; i++)
			{
				Triangle triangle = triangles[i];
				Triangle3 triangle2 = AreaUtils.GetTriangle3(nodes, triangle);
				Bounds1 bounds = new Bounds1(math.min(0f, heightOffset), math.max(0f, heightOffset));
				if (useTerrainHeight)
				{
					triangle.m_HeightRange = GetHeightRange(ref m_TerrainHeightData, triangle2);
					if (triangle.m_HeightRange.min > triangle.m_HeightRange.max)
					{
						triangle.m_HeightRange = bounds;
					}
					else if (useTriangleHeight)
					{
						triangle.m_HeightRange |= bounds;
					}
					if (useWaterHeight)
					{
						triangle.m_HeightRange |= WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, triangle2.a) - triangle2.a.y;
						triangle.m_HeightRange |= WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, triangle2.b) - triangle2.b.y;
						triangle.m_HeightRange |= WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, triangle2.c) - triangle2.c.y;
					}
				}
				else
				{
					triangle.m_HeightRange = bounds;
				}
				triangles[i] = triangle;
			}
		}

		private static Bounds1 GetHeightRange(ref TerrainHeightData data, Triangle3 triangle3)
		{
			triangle3.a = TerrainUtils.ToHeightmapSpace(ref data, triangle3.a);
			triangle3.b = TerrainUtils.ToHeightmapSpace(ref data, triangle3.b);
			triangle3.c = TerrainUtils.ToHeightmapSpace(ref data, triangle3.c);
			if (triangle3.b.z < triangle3.a.z)
			{
				CommonUtils.Swap(ref triangle3.b, ref triangle3.a);
			}
			if (triangle3.c.z < triangle3.a.z)
			{
				CommonUtils.Swap(ref triangle3.c, ref triangle3.a);
			}
			if (triangle3.c.z < triangle3.b.z)
			{
				CommonUtils.Swap(ref triangle3.c, ref triangle3.b);
			}
			int num = math.max(0, (int)math.floor(triangle3.a.z));
			int num2 = math.min(data.resolution.z - 1, (int)math.ceil(triangle3.c.z));
			Bounds1 result = new Bounds1(float.MaxValue, float.MinValue);
			float zFactorAC;
			float zFactorAB;
			float zFactorBC;
			if (num2 >= num)
			{
				zFactorAC = 1f / (triangle3.c.z - triangle3.a.z);
				zFactorAB = 1f / (triangle3.b.z - triangle3.a.z);
				zFactorBC = 1f / (triangle3.c.z - triangle3.b.z);
				(float2 left, float2 right) tuple = GetLeftRight(num);
				float2 @float = tuple.left;
				float2 float2 = tuple.right;
				float2 float3 = @float;
				float2 float4 = float2;
				float2 float5 = float3;
				float2 float6 = float4;
				for (int i = num; i <= num2; i++)
				{
					float2 float7 = @float;
					float2 float8 = float2;
					float2 float9 = new float2(math.min(@float.x, float5.x), math.max(float2.x, float6.x));
					if (i < num2)
					{
						(float2 left, float2 right) tuple2 = GetLeftRight(i + 1);
						float7 = tuple2.left;
						float8 = tuple2.right;
						float9 = new float2(math.min(float9.x, float7.x), math.max(float9.x, float8.x));
					}
					int num3 = math.max(0, (int)math.floor(float9.x));
					int num4 = math.min(data.resolution.x - 1, (int)math.ceil(float9.y));
					float num5 = 1f / (float2.x - @float.x);
					int num6 = i * data.resolution.x;
					for (int j = num3; j <= num4; j++)
					{
						float t = math.saturate(((float)j - @float.x) * num5);
						float num7 = math.lerp(@float.y, float2.y, t);
						float num8 = (int)data.heights[num6 + j];
						result |= num8 - num7;
					}
					float2 float10 = @float;
					float4 = float2;
					float5 = float10;
					float6 = float4;
					float2 float11 = float7;
					float4 = float8;
					@float = float11;
					float2 = float4;
				}
			}
			result.min /= data.scale.y;
			result.max /= data.scale.y;
			return result;
			(float2 left, float2 right) GetLeftRight(float z)
			{
				float t2 = math.saturate((z - triangle3.a.z) * zFactorAC);
				float2 a = math.lerp(triangle3.a.xy, triangle3.c.xy, t2);
				float2 b;
				if (z <= triangle3.b.z)
				{
					float t3 = math.saturate((z - triangle3.a.z) * zFactorAB);
					b = math.lerp(triangle3.a.xy, triangle3.b.xy, t3);
				}
				else
				{
					float t4 = math.saturate((z - triangle3.b.z) * zFactorBC);
					b = math.lerp(triangle3.b.xy, triangle3.c.xy, t4);
				}
				if (b.x < a.x)
				{
					CommonUtils.Swap(ref a, ref b);
				}
				return (left: a, right: b);
			}
		}

		private Geometry CalculateGeometry(DynamicBuffer<Node> nodes, DynamicBuffer<Triangle> triangles, NativeArray<Bounds2> edgeBounds, int totalDepth, float nodeDistance, float lodBias, bool useTerrainHeight, bool useWaterHeight)
		{
			Geometry result = new Geometry
			{
				m_Bounds = 
				{
					min = float.MaxValue,
					max = float.MinValue
				}
			};
			if (triangles.Length != 0)
			{
				float num = -1f;
				for (int i = 0; i < triangles.Length; i++)
				{
					ref Triangle reference = ref triangles.ElementAt(i);
					Triangle3 triangle = AreaUtils.GetTriangle3(nodes, reference);
					result.m_Bounds |= MathUtils.Bounds(triangle);
					result.m_SurfaceArea += MathUtils.Area(triangle.xz);
					int3 @int = math.abs(reference.m_Indices.zxy - reference.m_Indices.yzx);
					bool3 @bool = (@int == 1) | (@int == nodes.Length - 1);
					bool3 bool2 = !@bool;
					float2 bestMinDistance = -1f;
					float3 bestPosition = default(float3);
					if (bool2.x)
					{
						float3 position = math.lerp(triangle.b, triangle.c, 0.5f);
						CheckCenterPositionCandidate(ref bestMinDistance, ref bestPosition, position, triangle, nodes, edgeBounds, totalDepth);
					}
					if (bool2.y)
					{
						float3 position2 = math.lerp(triangle.c, triangle.a, 0.5f);
						CheckCenterPositionCandidate(ref bestMinDistance, ref bestPosition, position2, triangle, nodes, edgeBounds, totalDepth);
					}
					if (bool2.z)
					{
						float3 position3 = math.lerp(triangle.a, triangle.b, 0.5f);
						CheckCenterPositionCandidate(ref bestMinDistance, ref bestPosition, position3, triangle, nodes, edgeBounds, totalDepth);
					}
					if (math.all(bool2.xy) & @bool.z)
					{
						float3 position4 = triangle.c * 0.5f + (triangle.a + triangle.b) * 0.25f;
						CheckCenterPositionCandidate(ref bestMinDistance, ref bestPosition, position4, triangle, nodes, edgeBounds, totalDepth);
					}
					else if (math.all(bool2.yz) & @bool.x)
					{
						float3 position5 = triangle.a * 0.5f + (triangle.b + triangle.c) * 0.25f;
						CheckCenterPositionCandidate(ref bestMinDistance, ref bestPosition, position5, triangle, nodes, edgeBounds, totalDepth);
					}
					else if (math.all(bool2.zx) & @bool.y)
					{
						float3 position6 = triangle.b * 0.5f + (triangle.c + triangle.a) * 0.25f;
						CheckCenterPositionCandidate(ref bestMinDistance, ref bestPosition, position6, triangle, nodes, edgeBounds, totalDepth);
					}
					else
					{
						float3 position7 = (triangle.a + triangle.b + triangle.c) * (1f / 3f);
						CheckCenterPositionCandidate(ref bestMinDistance, ref bestPosition, position7, triangle, nodes, edgeBounds, totalDepth);
					}
					float2 @float = math.sqrt(bestMinDistance) * 4f;
					reference.m_MinLod = RenderingUtils.CalculateLodLimit(RenderingUtils.GetRenderingSize(new float3(@float.x, nodeDistance, @float.y)), lodBias);
					if (bestMinDistance.x > num)
					{
						num = bestMinDistance.x;
						result.m_CenterPosition = bestPosition;
					}
				}
			}
			else if (nodes.Length != 0)
			{
				for (int j = 0; j < nodes.Length; j++)
				{
					float3 position8 = nodes[j].m_Position;
					result.m_Bounds |= position8;
					result.m_CenterPosition += position8;
				}
				result.m_CenterPosition /= (float)nodes.Length;
			}
			if (useTerrainHeight)
			{
				if (useWaterHeight)
				{
					result.m_CenterPosition.y = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, result.m_CenterPosition);
				}
				else
				{
					result.m_CenterPosition.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, result.m_CenterPosition);
				}
			}
			return result;
		}

		private void CheckCenterPositionCandidate(ref float2 bestMinDistance, ref float3 bestPosition, float3 position, Triangle3 triangle, DynamicBuffer<Node> nodes, NativeArray<Bounds2> edgeBounds, int totalDepth)
		{
			float2 @float = float.MaxValue;
			float t;
			if (edgeBounds.IsCreated)
			{
				float num = math.sqrt(math.max(math.distancesq(position.xz, triangle.a.xz), math.max(math.distancesq(position.xz, triangle.b.xz), math.distancesq(position.xz, triangle.c.xz)))) + 0.1f;
				Bounds2 bounds = new Bounds2(position.xz - num, position.xz + num);
				int num2 = 0;
				int num3 = 1;
				int num4 = 0;
				int length = nodes.Length;
				Line2.Segment line = default(Line2.Segment);
				while (num3 > 0)
				{
					if (MathUtils.Intersect(edgeBounds[num2 + num4], bounds))
					{
						if (num3 != totalDepth)
						{
							num4 <<= 1;
							num2 += 1 << num3++;
							continue;
						}
						int num5 = num4 * length >> num3;
						int num6 = (num4 + 1) * length >> num3;
						line.a = nodes[num5++].m_Position.xz;
						for (int i = num5; i <= num6; i++)
						{
							line.b = nodes[math.select(i, 0, i == length)].m_Position.xz;
							float num7 = MathUtils.DistanceSquared(line, position.xz, out t);
							@float.y = math.select(@float.y, num7, num7 < @float.y);
							@float = math.select(@float, new float2(num7, @float.x), num7 < @float.x);
							line.a = line.b;
						}
					}
					while ((num4 & 1) != 0)
					{
						num4 >>= 1;
						num2 -= 1 << --num3;
					}
					num4++;
				}
			}
			else
			{
				Line2.Segment line2 = new Line2.Segment
				{
					a = nodes[nodes.Length - 1].m_Position.xz
				};
				for (int j = 0; j < nodes.Length; j++)
				{
					line2.b = nodes[j].m_Position.xz;
					float num8 = MathUtils.DistanceSquared(line2, position.xz, out t);
					@float.y = math.select(@float.y, num8, num8 < @float.y);
					@float = math.select(@float, new float2(num8, @float.x), num8 < @float.x);
					line2.a = line2.b;
				}
			}
			if (@float.x > bestMinDistance.x)
			{
				bestMinDistance = @float;
				bestPosition = position;
			}
		}
	}

	private struct Index
	{
		public int m_NodeIndex;

		public int m_PrevIndex;

		public int m_NextIndex;

		public int m_SkipIndex;
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Space> __Game_Areas_Space_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TerrainAreaData> __Game_Prefabs_TerrainAreaData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		public ComponentLookup<Area> __Game_Areas_Area_RW_ComponentLookup;

		public ComponentLookup<Geometry> __Game_Areas_Geometry_RW_ComponentLookup;

		public BufferLookup<Node> __Game_Areas_Node_RW_BufferLookup;

		public BufferLookup<Triangle> __Game_Areas_Triangle_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Areas_Space_RO_ComponentLookup = state.GetComponentLookup<Space>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_TerrainAreaData_RO_ComponentLookup = state.GetComponentLookup<TerrainAreaData>(isReadOnly: true);
			__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Areas_Area_RW_ComponentLookup = state.GetComponentLookup<Area>();
			__Game_Areas_Geometry_RW_ComponentLookup = state.GetComponentLookup<Geometry>();
			__Game_Areas_Node_RW_BufferLookup = state.GetBufferLookup<Node>();
			__Game_Areas_Triangle_RW_BufferLookup = state.GetBufferLookup<Triangle>();
		}
	}

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private EntityQuery m_UpdatedAreasQuery;

	private EntityQuery m_AllAreasQuery;

	private EntityQuery m_CreatedBuildingsQuery;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_UpdatedAreasQuery = GetEntityQuery(ComponentType.ReadOnly<Area>(), ComponentType.ReadOnly<Updated>(), ComponentType.ReadOnly<Node>(), ComponentType.ReadWrite<Triangle>());
		m_AllAreasQuery = GetEntityQuery(ComponentType.ReadOnly<Area>(), ComponentType.ReadOnly<Node>(), ComponentType.ReadWrite<Triangle>());
		m_CreatedBuildingsQuery = GetEntityQuery(ComponentType.ReadOnly<Created>(), ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Owner>(), ComponentType.Exclude<Temp>());
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_Loaded = true;
	}

	private bool GetLoaded()
	{
		if (m_Loaded)
		{
			m_Loaded = false;
			return true;
		}
		return false;
	}

	public void TerrainHeightsReadyAfterLoading()
	{
		m_Loaded = true;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		EntityQuery entityQuery = (GetLoaded() ? m_AllAreasQuery : m_UpdatedAreasQuery);
		if (!entityQuery.IsEmptyIgnoreFilter)
		{
			JobHandle outJobHandle;
			NativeList<Entity> list = entityQuery.ToEntityListAsync(Allocator.TempJob, out outJobHandle);
			JobHandle outJobHandle2;
			NativeList<Entity> buildings = m_CreatedBuildingsQuery.ToEntityListAsync(Allocator.TempJob, out outJobHandle2);
			JobHandle deps;
			JobHandle jobHandle = new TriangulateAreasJob
			{
				m_Entities = list.AsDeferredJobArray(),
				m_Buildings = buildings,
				m_SpaceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Space_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
				m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabTerrainAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TerrainAreaData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabAreaGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
				m_TerrainHeightData = m_TerrainSystem.GetHeightData(waitForPending: true),
				m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
				m_AreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Area_RW_ComponentLookup, ref base.CheckedStateRef),
				m_GeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Geometry_RW_ComponentLookup, ref base.CheckedStateRef),
				m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RW_BufferLookup, ref base.CheckedStateRef),
				m_Triangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RW_BufferLookup, ref base.CheckedStateRef)
			}.Schedule(list, 1, JobUtils.CombineDependencies(base.Dependency, outJobHandle2, outJobHandle, deps));
			list.Dispose(jobHandle);
			buildings.Dispose(jobHandle);
			m_TerrainSystem.AddCPUHeightReader(jobHandle);
			m_WaterSystem.AddSurfaceReader(jobHandle);
			base.Dependency = jobHandle;
		}
	}

	public static void BuildEdgeBounds(DynamicBuffer<Node> nodes, NativeArray<float3> expandedNodes, out NativeArray<Bounds2> edgeBounds, out int totalDepth)
	{
		int num = -1;
		int num2 = 0;
		int num3;
		for (num3 = nodes.Length; num3 >= 4; num3 >>= 1)
		{
			num += 1 << num2++;
		}
		edgeBounds = new NativeArray<Bounds2>(num, Allocator.Temp);
		num2 = (totalDepth = num2 - 1);
		int num4 = 1 << num2;
		int num5 = num - num4;
		num3 = nodes.Length;
		for (int i = 0; i < num4; i++)
		{
			int num6 = i * num3 >> num2;
			int num7 = (i + 1) * num3 >> num2;
			Bounds2 value = MathUtils.Bounds(nodes[num6].m_Position.xz, expandedNodes[num6].xz);
			num6++;
			for (int j = num6; j <= num7; j++)
			{
				int index = math.select(j, 0, j == num3);
				value |= nodes[index].m_Position.xz;
				value |= expandedNodes[index].xz;
			}
			edgeBounds[num5 + i] = value;
		}
		while (--num2 > 0)
		{
			int num8 = num5;
			num4 = 1 << num2;
			num5 -= num4;
			for (int k = 0; k < num4; k++)
			{
				edgeBounds[num5 + k] = edgeBounds[num8 + (k << 1)] | edgeBounds[num8 + (k << 1) + 1];
			}
		}
	}

	public static void Triangulate<T>(NativeArray<float3> nodes, T triangles, NativeArray<Bounds2> edgeBounds, int totalDepth, bool isCounterClockwise) where T : INativeList<Triangle>
	{
		if (nodes.Length < 3)
		{
			triangles.Clear();
			return;
		}
		int length = nodes.Length - 2;
		triangles.Length = length;
		int num = 0;
		NativeArray<Index> indexBuffer = new NativeArray<Index>(nodes.Length, Allocator.Temp);
		if (isCounterClockwise)
		{
			for (int i = 0; i < nodes.Length; i++)
			{
				int prevIndex = math.select(i - 1, nodes.Length - 1, i == 0);
				int num2 = math.select(i + 1, 0, i + 1 == nodes.Length);
				indexBuffer[i] = new Index
				{
					m_NodeIndex = i,
					m_PrevIndex = prevIndex,
					m_NextIndex = num2,
					m_SkipIndex = num2
				};
			}
		}
		else
		{
			for (int j = 0; j < nodes.Length; j++)
			{
				int prevIndex2 = math.select(j - 1, nodes.Length - 1, j == 0);
				int num3 = math.select(j + 1, 0, j + 1 == nodes.Length);
				indexBuffer[j] = new Index
				{
					m_NodeIndex = nodes.Length - 1 - j,
					m_PrevIndex = prevIndex2,
					m_NextIndex = num3,
					m_SkipIndex = num3
				};
			}
		}
		int num4 = nodes.Length;
		int num5 = 2 * num4;
		int num6 = num4 - 2;
		int num7 = num4 - 1;
		while (num4 > 2)
		{
			if (0 >= num5--)
			{
				triangles.Clear();
				break;
			}
			Index index = indexBuffer[num7];
			Index index2 = indexBuffer[index.m_NextIndex];
			Index index3 = indexBuffer[index2.m_NextIndex];
			if (Snip(nodes, index, index2, index3, num4, totalDepth, edgeBounds, indexBuffer))
			{
				if (num == triangles.Length)
				{
					triangles.Clear();
					break;
				}
				int index4 = num++;
				Triangle value = new Triangle(index.m_NodeIndex, index2.m_NodeIndex, index3.m_NodeIndex);
				triangles[index4] = value;
				index.m_SkipIndex = math.select(index.m_SkipIndex, index2.m_SkipIndex, index.m_SkipIndex == index.m_NextIndex);
				index.m_NextIndex = index2.m_NextIndex;
				index3.m_PrevIndex = num7;
				indexBuffer[num7] = index;
				indexBuffer[index2.m_NextIndex] = index3;
				if (num6 != index.m_PrevIndex)
				{
					index2 = indexBuffer[num6];
					index3 = indexBuffer[index.m_PrevIndex];
					index2.m_SkipIndex = index.m_PrevIndex;
					index3.m_SkipIndex = num7;
					indexBuffer[num6] = index2;
					indexBuffer[index.m_PrevIndex] = index3;
				}
				num6 = num7;
				num5 = 2 * --num4;
			}
			num7 = index.m_SkipIndex;
		}
		if (num != triangles.Length)
		{
			triangles.Clear();
		}
		indexBuffer.Dispose();
	}

	public static float Area(DynamicBuffer<Node> nodes)
	{
		int length = nodes.Length;
		float num = 0f;
		float3 @float = 0f;
		if (nodes.Length != 0)
		{
			@float = nodes[0].m_Position;
		}
		int index = length - 1;
		int num2 = 0;
		while (num2 < length)
		{
			float2 float2 = (nodes[index].m_Position.xz - @float.xz) * (nodes[num2].m_Position.zx - @float.zx);
			num += float2.x - float2.y;
			index = num2++;
		}
		return num * 0.5f;
	}

	public static float Area(NativeArray<SubAreaNode> nodes)
	{
		int length = nodes.Length;
		float num = 0f;
		float3 @float = 0f;
		if (nodes.Length != 0)
		{
			@float = nodes[0].m_Position;
		}
		int index = length - 1;
		int num2 = 0;
		while (num2 < length)
		{
			float2 float2 = (nodes[index].m_Position.xz - @float.xz) * (nodes[num2].m_Position.zx - @float.zx);
			num += float2.x - float2.y;
			index = num2++;
		}
		return num * 0.5f;
	}

	private static bool Snip(NativeArray<float3> nodes, Index index0, Index index1, Index index2, int nodeCount, int totalDepth, NativeArray<Bounds2> edgeBounds, NativeArray<Index> indexBuffer)
	{
		Triangle2 triangle = new Triangle2(nodes[index0.m_NodeIndex].xz, nodes[index1.m_NodeIndex].xz, nodes[index2.m_NodeIndex].xz);
		float2 @float = (triangle.b - triangle.a) * (triangle.c - triangle.a).yx;
		if (@float.x - @float.y < float.Epsilon)
		{
			return false;
		}
		if (edgeBounds.IsCreated)
		{
			Bounds2 bounds = MathUtils.Bounds(triangle);
			int num = 0;
			int num2 = 1;
			int num3 = 0;
			while (num2 > 0)
			{
				if (MathUtils.Intersect(edgeBounds[num + num3], bounds))
				{
					if (num2 != totalDepth)
					{
						num3 <<= 1;
						num += 1 << num2++;
						continue;
					}
					int num4 = num3 * nodes.Length >> num2;
					int num5 = (num3 + 1) * nodes.Length >> num2;
					for (int i = num4; i < num5; i++)
					{
						if (i != index0.m_NodeIndex && i != index1.m_NodeIndex && i != index2.m_NodeIndex && MathUtils.Intersect(triangle, nodes[i].xz))
						{
							return false;
						}
					}
				}
				while ((num3 & 1) != 0)
				{
					num3 >>= 1;
					num -= 1 << --num2;
				}
				num3++;
			}
		}
		else
		{
			Index index3 = indexBuffer[index2.m_NextIndex];
			for (int j = 3; j < nodeCount; j++)
			{
				if (MathUtils.Intersect(triangle, nodes[index3.m_NodeIndex].xz))
				{
					return false;
				}
				index3 = indexBuffer[index3.m_NextIndex];
			}
		}
		return true;
	}

	public static void EqualizeTriangles<T>(NativeArray<float3> nodes, T triangles) where T : INativeList<Triangle>
	{
		if (triangles.Length < 2)
		{
			return;
		}
		NativeParallelHashMap<int2, int2> edgeMap = new NativeParallelHashMap<int2, int2>(triangles.Length * 3, Allocator.Temp);
		for (int i = 0; i < triangles.Length; i++)
		{
			Triangle triangle = triangles[i];
			bool3 @bool = triangle.m_Indices.yzx - triangle.m_Indices.zxy > 1;
			if (@bool.x)
			{
				edgeMap.TryAdd(triangle.m_Indices.yz, new int2(i, 0));
			}
			if (@bool.y)
			{
				edgeMap.TryAdd(triangle.m_Indices.zx, new int2(i, 1));
			}
			if (@bool.z)
			{
				edgeMap.TryAdd(triangle.m_Indices.xy, new int2(i, 2));
			}
		}
		for (int j = 0; j < 1000; j++)
		{
			bool flag = false;
			int num = 0;
			while (num < triangles.Length)
			{
				Triangle triangle2 = triangles[num];
				bool3 bool2 = triangle2.m_Indices.zxy - triangle2.m_Indices.yzx > 1;
				int2 item2;
				int2 item3;
				if (bool2.x && edgeMap.TryGetValue(triangle2.m_Indices.zy, out var item) && TurnEdgeIfNeeded(nodes, triangles, edgeMap, new int2(num, 0), item))
				{
					flag = true;
				}
				else if (bool2.y && edgeMap.TryGetValue(triangle2.m_Indices.xz, out item2) && TurnEdgeIfNeeded(nodes, triangles, edgeMap, new int2(num, 1), item2))
				{
					flag = true;
				}
				else if (bool2.z && edgeMap.TryGetValue(triangle2.m_Indices.yx, out item3) && TurnEdgeIfNeeded(nodes, triangles, edgeMap, new int2(num, 2), item3))
				{
					flag = true;
				}
				else
				{
					num++;
				}
			}
			if (!flag)
			{
				break;
			}
		}
		edgeMap.Dispose();
	}

	private static bool TurnEdgeIfNeeded<T>(NativeArray<float3> nodes, T triangles, NativeParallelHashMap<int2, int2> edgeMap, int2 index1, int2 index2) where T : INativeList<Triangle>
	{
		int x = index1.x;
		Triangle triangle = triangles[x];
		int x2 = index2.x;
		Triangle triangle2 = triangles[x2];
		int2 @int = new int2(index1.y, index2.y);
		int2 int2 = new int2(triangle.m_Indices[@int.x], triangle2.m_Indices[@int.y]);
		int2 int3 = math.select(@int + 1, 0, @int == 2);
		int3 = new int2(triangle.m_Indices[int3.x], triangle2.m_Indices[int3.y]);
		Triangle triangle3 = new Triangle(int2.x, int3.x, int2.y);
		Triangle triangle4 = new Triangle(int2.y, int3.y, int2.x);
		float num = math.min(GetEqualizationValue(nodes, triangle), GetEqualizationValue(nodes, triangle2));
		if (math.min(GetEqualizationValue(nodes, triangle3), GetEqualizationValue(nodes, triangle4)) > num)
		{
			bool3 @bool = triangle.m_Indices.yzx - triangle.m_Indices.zxy > 1;
			bool3 bool2 = triangle2.m_Indices.yzx - triangle2.m_Indices.zxy > 1;
			bool3 bool3 = triangle3.m_Indices.yzx - triangle3.m_Indices.zxy > 1;
			bool3 bool4 = triangle4.m_Indices.yzx - triangle4.m_Indices.zxy > 1;
			if (@bool.x)
			{
				edgeMap.Remove(triangle.m_Indices.yz);
			}
			if (@bool.y)
			{
				edgeMap.Remove(triangle.m_Indices.zx);
			}
			if (@bool.z)
			{
				edgeMap.Remove(triangle.m_Indices.xy);
			}
			if (bool2.x)
			{
				edgeMap.Remove(triangle2.m_Indices.yz);
			}
			if (bool2.y)
			{
				edgeMap.Remove(triangle2.m_Indices.zx);
			}
			if (bool2.z)
			{
				edgeMap.Remove(triangle2.m_Indices.xy);
			}
			if (bool3.x)
			{
				edgeMap.TryAdd(triangle3.m_Indices.yz, new int2(index1.x, 0));
			}
			if (bool3.y)
			{
				edgeMap.TryAdd(triangle3.m_Indices.zx, new int2(index1.x, 1));
			}
			if (bool3.z)
			{
				edgeMap.TryAdd(triangle3.m_Indices.xy, new int2(index1.x, 2));
			}
			if (bool4.x)
			{
				edgeMap.TryAdd(triangle4.m_Indices.yz, new int2(index2.x, 0));
			}
			if (bool4.y)
			{
				edgeMap.TryAdd(triangle4.m_Indices.zx, new int2(index2.x, 1));
			}
			if (bool4.z)
			{
				edgeMap.TryAdd(triangle4.m_Indices.xy, new int2(index2.x, 2));
			}
			int x3 = index1.x;
			Triangle value = triangle3;
			triangles[x3] = value;
			int x4 = index2.x;
			Triangle value2 = triangle4;
			triangles[x4] = value2;
			return true;
		}
		return false;
	}

	private static float GetEqualizationValue(NativeArray<float3> nodes, Triangle triangle)
	{
		Triangle2 triangle2 = new Triangle2(nodes[triangle.m_Indices.x].xz, nodes[triangle.m_Indices.y].xz, nodes[triangle.m_Indices.z].xz);
		float3 @float = new float3(triangle2.a.x, triangle2.b.x, triangle2.c.x);
		float3 float2 = new float3(triangle2.a.y, triangle2.b.y, triangle2.c.y);
		float3 float3 = @float - @float.yzx;
		float3 float4 = float2 - float2.yzx;
		float num = math.dot(@float, float2.yzx - float2.zxy) * 0.5f;
		float num2 = math.csum(math.sqrt(float3 * float3 + float4 * float4));
		return num / (num2 * num2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public GeometrySystem()
	{
	}
}
