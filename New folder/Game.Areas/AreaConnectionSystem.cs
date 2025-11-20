using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Areas;

[CompilerGenerated]
public class AreaConnectionSystem : GameSystemBase
{
	private enum LaneType
	{
		Road,
		Pedestrian,
		Border
	}

	private struct AreaLaneKey : IEquatable<AreaLaneKey>
	{
		private LaneType m_LaneType;

		private float2 m_Position1;

		private float2 m_Position2;

		public AreaLaneKey(LaneType laneType, float2 position1, float2 position2)
		{
			m_LaneType = laneType;
			bool2 @bool = position1 < position2;
			bool flag = position1.x == position2.x;
			if (@bool.x | (flag & @bool.y))
			{
				m_Position1 = position1;
				m_Position2 = position2;
			}
			else
			{
				m_Position1 = position2;
				m_Position2 = position1;
			}
		}

		public bool Equals(AreaLaneKey other)
		{
			if (m_LaneType == other.m_LaneType && m_Position1.Equals(other.m_Position1))
			{
				return m_Position2.Equals(other.m_Position2);
			}
			return false;
		}

		public override int GetHashCode()
		{
			int num = 17 * 31;
			int num2 = (int)m_LaneType;
			return ((num + num2.GetHashCode()) * 31 + m_Position1.GetHashCode()) * 31 + m_Position2.GetHashCode();
		}
	}

	private struct AreaLaneValue
	{
		public Entity m_Lane;

		public float2 m_Heights;

		public AreaLaneValue(Entity lane, float a, float b)
		{
			m_Lane = lane;
			m_Heights = new float2(a, b);
		}
	}

	private struct TriangleSideKey : IEquatable<TriangleSideKey>
	{
		private float3 m_Position1;

		private float3 m_Position2;

		public TriangleSideKey(float3 position1, float3 position2)
		{
			m_Position1 = position1;
			m_Position2 = position2;
		}

		public bool Equals(TriangleSideKey other)
		{
			if (m_Position1.Equals(other.m_Position1))
			{
				return m_Position2.Equals(other.m_Position2);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (17 * 31 + m_Position1.GetHashCode()) * 31 + m_Position2.GetHashCode();
		}
	}

	[BurstCompile]
	private struct UpdateSecondaryLanesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Area> m_AreaType;

		[ReadOnly]
		public ComponentTypeHandle<Lot> m_LotType;

		[ReadOnly]
		public ComponentTypeHandle<PseudoRandomSeed> m_PseudoRandomSeedType;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Overridden> m_OverriddenData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.SecondaryLane> m_SecondaryLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<AreaLane> m_AreaLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Game.Routes.TakeoffLocation> m_TakeoffLocationData;

		[ReadOnly]
		public ComponentLookup<AccessLane> m_AccessLaneData;

		[ReadOnly]
		public ComponentLookup<RouteLane> m_RouteLaneData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_PrefabNetLaneData;

		[ReadOnly]
		public ComponentLookup<NavigationAreaData> m_PrefabNavigationAreaData;

		[ReadOnly]
		public ComponentLookup<EnclosedAreaData> m_PrefabEnclosedAreaData;

		[ReadOnly]
		public ComponentLookup<NetLaneArchetypeData> m_PrefabNetLaneArchetypeData;

		[ReadOnly]
		public BufferLookup<Node> m_Nodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_Triangles;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<CutRange> m_CutRanges;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public NativeList<Entity> m_ConnectionPrefabs;

		[ReadOnly]
		public ComponentTypeSet m_AppliedTypes;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Area> nativeArray2 = chunk.GetNativeArray(ref m_AreaType);
			NativeArray<PseudoRandomSeed> nativeArray3 = chunk.GetNativeArray(ref m_PseudoRandomSeedType);
			bool isLot = chunk.Has(ref m_LotType);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Area area = nativeArray2[i];
				Temp temp = default(Temp);
				Temp subTemp = default(Temp);
				bool isTemp = false;
				bool isDeleted = m_DeletedData.HasComponent(entity);
				bool isCounterClockwise = (area.m_Flags & AreaFlags.CounterClockwise) != 0;
				if (!CollectionUtils.TryGet(nativeArray3, i, out var value))
				{
					value = new PseudoRandomSeed((ushort)random.NextUInt(65536u));
				}
				if (m_TempData.HasComponent(entity))
				{
					temp = m_TempData[entity];
					subTemp.m_Flags = temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Hidden);
					if ((temp.m_Flags & (TempFlags.Replace | TempFlags.Upgrade)) != 0)
					{
						subTemp.m_Flags |= TempFlags.Modify;
					}
					isTemp = true;
				}
				FindOriginalLanes(temp.m_Original, out var originalConnections);
				UpdateLanes(unfilteredChunkIndex, entity, value, isCounterClockwise, isLot, isDeleted, isTemp, subTemp, originalConnections);
				if (originalConnections.IsCreated)
				{
					originalConnections.Dispose();
				}
			}
		}

		private void GetLaneFlags(RouteConnectionType connectionType, RoadTypes areaRoadTypes, ref ConnectionLaneFlags laneFlags, ref RoadTypes roadTypes, ref int indexOffset)
		{
			switch (connectionType)
			{
			case RouteConnectionType.Pedestrian:
				laneFlags |= ConnectionLaneFlags.Pedestrian;
				indexOffset++;
				break;
			case RouteConnectionType.Road:
				laneFlags |= ConnectionLaneFlags.Road;
				roadTypes = areaRoadTypes;
				indexOffset++;
				break;
			}
		}

		private void UpdateLanes(int jobIndex, Entity area, PseudoRandomSeed pseudoRandomSeed, bool isCounterClockwise, bool isLot, bool isDeleted, bool isTemp, Temp subTemp, NativeParallelMultiHashMap<AreaLaneKey, AreaLaneValue> originalLanes)
		{
			NativeParallelMultiHashMap<AreaLaneKey, AreaLaneValue> oldLanes = default(NativeParallelMultiHashMap<AreaLaneKey, AreaLaneValue>);
			NativeParallelHashSet<Entity> updatedSet = default(NativeParallelHashSet<Entity>);
			DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[area];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity subLane = dynamicBuffer[i].m_SubLane;
				if (m_SecondaryLaneData.HasComponent(subLane))
				{
					Curve curve = m_CurveData[subLane];
					if (!oldLanes.IsCreated)
					{
						oldLanes = new NativeParallelMultiHashMap<AreaLaneKey, AreaLaneValue>(dynamicBuffer.Length, Allocator.Temp);
					}
					Game.Net.ConnectionLane componentData;
					LaneType laneType = ((!m_ConnectionLaneData.TryGetComponent(subLane, out componentData)) ? LaneType.Border : (((componentData.m_Flags & ConnectionLaneFlags.Road) == 0) ? LaneType.Pedestrian : LaneType.Road));
					oldLanes.Add(new AreaLaneKey(laneType, curve.m_Bezier.a.xz, curve.m_Bezier.d.xz), new AreaLaneValue(subLane, curve.m_Bezier.a.y, curve.m_Bezier.d.y));
				}
			}
			if (!isDeleted)
			{
				ConnectionLaneFlags laneFlags = (ConnectionLaneFlags)0;
				RoadTypes roadTypes = RoadTypes.None;
				Entity entity = Entity.Null;
				int num = 0;
				int indexOffset = 0;
				bool test = false;
				PrefabRef prefabRef = m_PrefabRefData[area];
				Unity.Mathematics.Random random = pseudoRandomSeed.GetRandom(PseudoRandomSeed.kAreaBorder);
				if (m_PrefabNavigationAreaData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
				{
					GetLaneFlags(componentData2.m_ConnectionType, componentData2.m_RoadTypes, ref laneFlags, ref roadTypes, ref indexOffset);
					GetLaneFlags(componentData2.m_SecondaryType, componentData2.m_RoadTypes, ref laneFlags, ref roadTypes, ref indexOffset);
				}
				if (m_PrefabEnclosedAreaData.TryGetComponent(prefabRef.m_Prefab, out var componentData3))
				{
					entity = componentData3.m_BorderLanePrefab;
					test = componentData3.m_CounterClockWise != isCounterClockwise;
				}
				if (laneFlags != 0)
				{
					DynamicBuffer<Node> nodes = m_Nodes[area];
					DynamicBuffer<Triangle> dynamicBuffer2 = m_Triangles[area];
					if (dynamicBuffer2.Length == 1)
					{
						Triangle triangle = dynamicBuffer2[0];
						float3 trianglePosition = GetTrianglePosition(nodes, triangle);
						float3 trianglePosition2 = GetTrianglePosition(nodes, triangle);
						trianglePosition += (nodes[triangle.m_Indices.x].m_Position - trianglePosition) * 0.5f;
						trianglePosition2 += (nodes[triangle.m_Indices.y].m_Position - trianglePosition2) * 0.25f + (nodes[triangle.m_Indices.z].m_Position - trianglePosition2) * 0.25f;
						float3 middlePosition = (trianglePosition + trianglePosition2) * 0.5f;
						int4 xyyz = triangle.m_Indices.xyyz;
						CheckConnection(jobIndex, area, isTemp, subTemp, 0, 2 * indexOffset, indexOffset, trianglePosition, middlePosition, trianglePosition2, xyyz, laneFlags, roadTypes, originalLanes, oldLanes, ref updatedSet);
						num = 3 * indexOffset;
					}
					else if (dynamicBuffer2.Length >= 2)
					{
						NativeParallelHashMap<TriangleSideKey, int2> nativeParallelHashMap = new NativeParallelHashMap<TriangleSideKey, int2>(dynamicBuffer2.Length * 3, Allocator.Temp);
						for (int j = 0; j < dynamicBuffer2.Length; j++)
						{
							Triangle triangle2 = dynamicBuffer2[j];
							nativeParallelHashMap.TryAdd(new TriangleSideKey(nodes[triangle2.m_Indices.y].m_Position, nodes[triangle2.m_Indices.z].m_Position), new int2(j, triangle2.m_Indices.x));
							nativeParallelHashMap.TryAdd(new TriangleSideKey(nodes[triangle2.m_Indices.z].m_Position, nodes[triangle2.m_Indices.x].m_Position), new int2(j, triangle2.m_Indices.y));
							nativeParallelHashMap.TryAdd(new TriangleSideKey(nodes[triangle2.m_Indices.x].m_Position, nodes[triangle2.m_Indices.y].m_Position), new int2(j, triangle2.m_Indices.z));
						}
						int num2 = dynamicBuffer2.Length * indexOffset;
						for (int k = 0; k < dynamicBuffer2.Length; k++)
						{
							Triangle triangle3 = dynamicBuffer2[k];
							if (nativeParallelHashMap.TryGetValue(new TriangleSideKey(nodes[triangle3.m_Indices.z].m_Position, nodes[triangle3.m_Indices.y].m_Position), out var item) && item.x > k)
							{
								float3 trianglePosition3 = GetTrianglePosition(nodes, triangle3);
								float3 edgePosition = GetEdgePosition(nodes, triangle3.m_Indices.zy);
								float3 trianglePosition4 = GetTrianglePosition(nodes, dynamicBuffer2[item.x]);
								CheckConnection(nodeIndex: new int4(math.select(triangle3.m_Indices.xyz, triangle3.m_Indices.xzy, isCounterClockwise), item.y), jobIndex: jobIndex, area: area, isTemp: isTemp, temp: subTemp, startIndex: k * indexOffset, middleIndex: num2, endIndex: item.x * indexOffset, startPosition: trianglePosition3, middlePosition: edgePosition, endPosition: trianglePosition4, laneFlags: laneFlags, roadTypes: roadTypes, originalLanes: originalLanes, oldLanes: oldLanes, updatedSet: ref updatedSet);
								num2 += indexOffset;
							}
							if (nativeParallelHashMap.TryGetValue(new TriangleSideKey(nodes[triangle3.m_Indices.x].m_Position, nodes[triangle3.m_Indices.z].m_Position), out var item2) && item2.x > k)
							{
								float3 trianglePosition5 = GetTrianglePosition(nodes, triangle3);
								float3 edgePosition2 = GetEdgePosition(nodes, triangle3.m_Indices.xz);
								float3 trianglePosition6 = GetTrianglePosition(nodes, dynamicBuffer2[item2.x]);
								CheckConnection(nodeIndex: new int4(math.select(triangle3.m_Indices.yzx, triangle3.m_Indices.yxz, isCounterClockwise), item2.y), jobIndex: jobIndex, area: area, isTemp: isTemp, temp: subTemp, startIndex: k * indexOffset, middleIndex: num2, endIndex: item2.x * indexOffset, startPosition: trianglePosition5, middlePosition: edgePosition2, endPosition: trianglePosition6, laneFlags: laneFlags, roadTypes: roadTypes, originalLanes: originalLanes, oldLanes: oldLanes, updatedSet: ref updatedSet);
								num2 += indexOffset;
							}
							if (nativeParallelHashMap.TryGetValue(new TriangleSideKey(nodes[triangle3.m_Indices.y].m_Position, nodes[triangle3.m_Indices.x].m_Position), out var item3) && item3.x > k)
							{
								float3 trianglePosition7 = GetTrianglePosition(nodes, triangle3);
								float3 edgePosition3 = GetEdgePosition(nodes, triangle3.m_Indices.yx);
								float3 trianglePosition8 = GetTrianglePosition(nodes, dynamicBuffer2[item3.x]);
								CheckConnection(nodeIndex: new int4(math.select(triangle3.m_Indices.zxy, triangle3.m_Indices.zyx, isCounterClockwise), item3.y), jobIndex: jobIndex, area: area, isTemp: isTemp, temp: subTemp, startIndex: k * indexOffset, middleIndex: num2, endIndex: item3.x * indexOffset, startPosition: trianglePosition7, middlePosition: edgePosition3, endPosition: trianglePosition8, laneFlags: laneFlags, roadTypes: roadTypes, originalLanes: originalLanes, oldLanes: oldLanes, updatedSet: ref updatedSet);
								num2 += indexOffset;
							}
						}
						nativeParallelHashMap.Dispose();
						num = num2;
					}
				}
				if (entity != Entity.Null)
				{
					DynamicBuffer<Node> dynamicBuffer3 = m_Nodes[area];
					int num3 = num + dynamicBuffer3.Length;
					int num4 = math.select(0, 1, isLot);
					PseudoRandomSeed pseudoRandomSeed2 = new PseudoRandomSeed((ushort)random.NextUInt(65536u));
					NativeParallelHashSet<TriangleSideKey> nativeParallelHashSet = new NativeParallelHashSet<TriangleSideKey>(dynamicBuffer3.Length, Allocator.Temp);
					for (int l = num4; l < dynamicBuffer3.Length; l++)
					{
						int2 falseValue = new int2(l, l + 1);
						falseValue.y = math.select(falseValue.y, 0, falseValue.y == dynamicBuffer3.Length);
						falseValue = math.select(falseValue, falseValue.yx, test);
						Node node = dynamicBuffer3[falseValue.x];
						Node node2 = dynamicBuffer3[falseValue.y];
						nativeParallelHashSet.Add(new TriangleSideKey(node.m_Position, node2.m_Position));
					}
					for (int m = num4; m < dynamicBuffer3.Length; m++)
					{
						int2 falseValue2 = new int2(m, m + 1);
						falseValue2.y = math.select(falseValue2.y, 0, falseValue2.y == dynamicBuffer3.Length);
						falseValue2 = math.select(falseValue2, falseValue2.yx, test);
						Node node3 = dynamicBuffer3[falseValue2.x];
						Node node4 = dynamicBuffer3[falseValue2.y];
						if (!nativeParallelHashSet.Contains(new TriangleSideKey(node4.m_Position, node3.m_Position)))
						{
							float3 middlePosition2 = (node3.m_Position + node4.m_Position) * 0.5f;
							falseValue2 += num;
							CheckBorder(jobIndex, area, entity, isTemp, subTemp, pseudoRandomSeed2, falseValue2.x, num3++, falseValue2.y, node3.m_Position, middlePosition2, node4.m_Position, falseValue2.xxyy, originalLanes, oldLanes);
						}
					}
					nativeParallelHashSet.Dispose();
				}
			}
			if (oldLanes.IsCreated)
			{
				int num5 = oldLanes.Count();
				if (num5 != 0)
				{
					NativeArray<AreaLaneValue> valueArray = oldLanes.GetValueArray(Allocator.Temp);
					for (int n = 0; n < valueArray.Length; n++)
					{
						Entity entity2 = valueArray[n].m_Lane;
						m_CommandBuffer.RemoveComponent(jobIndex, entity2, in m_AppliedTypes);
						m_CommandBuffer.AddComponent<Deleted>(jobIndex, entity2);
						if (!updatedSet.IsCreated)
						{
							updatedSet = new NativeParallelHashSet<Entity>(num5, Allocator.Temp);
						}
						updatedSet.Add(entity2);
					}
					valueArray.Dispose();
				}
				oldLanes.Dispose();
			}
			if (!updatedSet.IsCreated)
			{
				return;
			}
			Entity entity3 = area;
			Owner componentData4;
			while (m_OwnerData.TryGetComponent(entity3, out componentData4))
			{
				entity3 = componentData4.m_Owner;
				if (m_UpdatedData.HasComponent(entity3) || m_DeletedData.HasComponent(entity3))
				{
					entity3 = Entity.Null;
					break;
				}
			}
			if (m_SubObjects.TryGetBuffer(entity3, out var bufferData))
			{
				UpdateConnections(jobIndex, updatedSet, bufferData);
			}
			updatedSet.Dispose();
		}

		private void UpdateConnections(int jobIndex, NativeParallelHashSet<Entity> updatedSet, DynamicBuffer<Game.Objects.SubObject> subObjects)
		{
			for (int i = 0; i < subObjects.Length; i++)
			{
				Entity subObject = subObjects[i].m_SubObject;
				if (m_UpdatedData.HasComponent(subObject) || m_DeletedData.HasComponent(subObject))
				{
					continue;
				}
				if (m_SpawnLocationData.HasComponent(subObject))
				{
					Game.Objects.SpawnLocation spawnLocation = m_SpawnLocationData[subObject];
					if (updatedSet.Contains(spawnLocation.m_ConnectedLane1) || updatedSet.Contains(spawnLocation.m_ConnectedLane2))
					{
						m_CommandBuffer.AddComponent(jobIndex, subObject, default(Updated));
					}
				}
				else if (m_TakeoffLocationData.HasComponent(subObject))
				{
					AccessLane accessLane = m_AccessLaneData[subObject];
					RouteLane routeLane = m_RouteLaneData[subObject];
					if (updatedSet.Contains(accessLane.m_Lane) || updatedSet.Contains(routeLane.m_StartLane) || updatedSet.Contains(routeLane.m_EndLane))
					{
						m_CommandBuffer.AddComponent(jobIndex, subObject, default(Updated));
					}
				}
				if (m_SubObjects.TryGetBuffer(subObject, out var bufferData))
				{
					UpdateConnections(jobIndex, updatedSet, bufferData);
				}
			}
		}

		private float3 GetEdgePosition(DynamicBuffer<Node> nodes, int2 indices)
		{
			return (nodes[indices.x].m_Position + nodes[indices.y].m_Position) / 2f;
		}

		private float3 GetTrianglePosition(DynamicBuffer<Node> nodes, Triangle triangle)
		{
			return (nodes[triangle.m_Indices.x].m_Position + nodes[triangle.m_Indices.y].m_Position + nodes[triangle.m_Indices.z].m_Position) / 3f;
		}

		private void CheckConnection(int jobIndex, Entity area, bool isTemp, Temp temp, int startIndex, int middleIndex, int endIndex, float3 startPosition, float3 middlePosition, float3 endPosition, int4 nodeIndex, ConnectionLaneFlags laneFlags, RoadTypes roadTypes, NativeParallelMultiHashMap<AreaLaneKey, AreaLaneValue> originalLanes, NativeParallelMultiHashMap<AreaLaneKey, AreaLaneValue> oldLanes, ref NativeParallelHashSet<Entity> updatedSet)
		{
			Lane component = default(Lane);
			component.m_StartNode = new PathNode(area, (ushort)startIndex);
			component.m_MiddleNode = new PathNode(area, (ushort)middleIndex);
			component.m_EndNode = new PathNode(area, (ushort)endIndex);
			float3 value = middlePosition - startPosition;
			float3 value2 = endPosition - middlePosition;
			value = MathUtils.Normalize(value, value.xz);
			value2 = MathUtils.Normalize(value2, value2.xz);
			Curve component2 = default(Curve);
			component2.m_Bezier = NetUtils.FitCurve(startPosition, value, value2, endPosition);
			component2.m_Length = MathUtils.Length(component2.m_Bezier);
			AreaLane component3 = default(AreaLane);
			component3.m_Nodes = nodeIndex;
			float2 heights = new float2(startPosition.y, endPosition.y);
			while (laneFlags != 0)
			{
				AreaLaneKey laneKey;
				ConnectionLaneFlags connectionLaneFlags;
				if ((laneFlags & ConnectionLaneFlags.Road) != 0)
				{
					connectionLaneFlags = ConnectionLaneFlags.Road;
					laneFlags &= ~ConnectionLaneFlags.Road;
					laneKey = new AreaLaneKey(LaneType.Road, startPosition.xz, endPosition.xz);
				}
				else
				{
					connectionLaneFlags = ConnectionLaneFlags.Pedestrian;
					connectionLaneFlags |= ConnectionLaneFlags.SecondaryStart | ConnectionLaneFlags.SecondaryEnd;
					laneFlags = (ConnectionLaneFlags)0;
					roadTypes = RoadTypes.Bicycle;
					laneKey = new AreaLaneKey(LaneType.Pedestrian, startPosition.xz, endPosition.xz);
				}
				Entity entity = SelectOld(oldLanes, laneKey, heights);
				if (entity != Entity.Null)
				{
					if (m_DeletedData.HasComponent(entity))
					{
						m_CommandBuffer.RemoveComponent<Deleted>(jobIndex, entity);
					}
					Lane lane = m_LaneData[entity];
					Curve curve = m_CurveData[entity];
					AreaLane areaLane = default(AreaLane);
					bool flag = m_AreaLaneData.HasComponent(entity);
					if (flag)
					{
						areaLane = m_AreaLaneData[entity];
					}
					Lane other = lane;
					CommonUtils.Swap(ref other.m_StartNode, ref other.m_EndNode);
					if ((!component.Equals(lane) || !component2.m_Bezier.Equals(curve.m_Bezier) || !component3.m_Nodes.Equals(areaLane.m_Nodes)) && (!component.Equals(other) || !MathUtils.Invert(component2.m_Bezier).Equals(curve.m_Bezier) || !component3.m_Nodes.wzyx.Equals(areaLane.m_Nodes)))
					{
						m_CommandBuffer.SetComponent(jobIndex, entity, component);
						m_CommandBuffer.SetComponent(jobIndex, entity, component2);
						if (flag)
						{
							m_CommandBuffer.SetComponent(jobIndex, entity, component3);
						}
						else
						{
							m_CommandBuffer.AddComponent(jobIndex, entity, component3);
						}
						m_CommandBuffer.AddComponent(jobIndex, entity, default(Updated));
						if (!updatedSet.IsCreated)
						{
							updatedSet = new NativeParallelHashSet<Entity>(oldLanes.Count() + 1, Allocator.Temp);
						}
						updatedSet.Add(entity);
					}
				}
				else
				{
					Entity entity2 = m_ConnectionPrefabs[0];
					NetLaneArchetypeData netLaneArchetypeData = m_PrefabNetLaneArchetypeData[entity2];
					Owner component4 = new Owner(area);
					PrefabRef component5 = new PrefabRef(entity2);
					Game.Net.SecondaryLane component6 = default(Game.Net.SecondaryLane);
					Game.Net.ConnectionLane component7 = new Game.Net.ConnectionLane
					{
						m_Flags = (connectionLaneFlags | (ConnectionLaneFlags.AllowMiddle | ConnectionLaneFlags.Area)),
						m_RoadTypes = roadTypes
					};
					Entity e = m_CommandBuffer.CreateEntity(jobIndex, netLaneArchetypeData.m_AreaLaneArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, component5);
					m_CommandBuffer.SetComponent(jobIndex, e, component);
					m_CommandBuffer.SetComponent(jobIndex, e, component2);
					m_CommandBuffer.SetComponent(jobIndex, e, component3);
					m_CommandBuffer.SetComponent(jobIndex, e, component7);
					m_CommandBuffer.AddComponent(jobIndex, e, component4);
					m_CommandBuffer.AddComponent(jobIndex, e, component6);
					if (isTemp)
					{
						temp.m_Original = SelectOriginal(originalLanes, laneKey, heights);
						m_CommandBuffer.AddComponent(jobIndex, e, temp);
					}
				}
				component.m_StartNode = new PathNode(area, (ushort)(++startIndex));
				component.m_MiddleNode = new PathNode(area, (ushort)(++middleIndex));
				component.m_EndNode = new PathNode(area, (ushort)(++endIndex));
				component2.m_Bezier.y += 0.5f;
				heights += 0.5f;
			}
		}

		private void CheckBorder(int jobIndex, Entity area, Entity lanePrefab, bool isTemp, Temp temp, PseudoRandomSeed pseudoRandomSeed, int startIndex, int middleIndex, int endIndex, float3 startPosition, float3 middlePosition, float3 endPosition, int4 nodeIndex, NativeParallelMultiHashMap<AreaLaneKey, AreaLaneValue> originalLanes, NativeParallelMultiHashMap<AreaLaneKey, AreaLaneValue> oldLanes)
		{
			Lane component = default(Lane);
			component.m_StartNode = new PathNode(area, (ushort)startIndex);
			component.m_MiddleNode = new PathNode(area, (ushort)middleIndex);
			component.m_EndNode = new PathNode(area, (ushort)endIndex);
			float3 value = middlePosition - startPosition;
			float3 value2 = endPosition - middlePosition;
			value = MathUtils.Normalize(value, value.xz);
			value2 = MathUtils.Normalize(value2, value2.xz);
			Curve component2 = default(Curve);
			component2.m_Bezier = NetUtils.FitCurve(startPosition, value, value2, endPosition);
			component2.m_Length = MathUtils.Length(component2.m_Bezier);
			AreaLane component3 = default(AreaLane);
			component3.m_Nodes = nodeIndex;
			AreaLaneKey laneKey = new AreaLaneKey(LaneType.Border, startPosition.xz, endPosition.xz);
			float2 heights = new float2(startPosition.y, endPosition.y);
			Entity entity = SelectOld(oldLanes, laneKey, heights);
			if (entity != Entity.Null)
			{
				if (m_DeletedData.HasComponent(entity))
				{
					m_CommandBuffer.RemoveComponent<Deleted>(jobIndex, entity);
				}
				Lane lane = m_LaneData[entity];
				Curve curve = m_CurveData[entity];
				AreaLane areaLane = default(AreaLane);
				bool flag = m_AreaLaneData.HasComponent(entity);
				if (flag)
				{
					areaLane = m_AreaLaneData[entity];
				}
				Lane other = lane;
				CommonUtils.Swap(ref other.m_StartNode, ref other.m_EndNode);
				if ((!component.Equals(lane) || !component2.m_Bezier.Equals(curve.m_Bezier) || !component3.m_Nodes.Equals(areaLane.m_Nodes)) && (!component.Equals(other) || !MathUtils.Invert(component2.m_Bezier).Equals(curve.m_Bezier) || !component3.m_Nodes.wzyx.Equals(areaLane.m_Nodes)))
				{
					m_CommandBuffer.SetComponent(jobIndex, entity, component);
					m_CommandBuffer.SetComponent(jobIndex, entity, component2);
					if (flag)
					{
						m_CommandBuffer.SetComponent(jobIndex, entity, component3);
					}
					else
					{
						m_CommandBuffer.AddComponent(jobIndex, entity, component3);
					}
					m_CommandBuffer.AddComponent(jobIndex, entity, default(Updated));
				}
				return;
			}
			NetLaneArchetypeData netLaneArchetypeData = m_PrefabNetLaneArchetypeData[lanePrefab];
			Owner component4 = new Owner(area);
			PrefabRef component5 = new PrefabRef(lanePrefab);
			Game.Net.SecondaryLane component6 = default(Game.Net.SecondaryLane);
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, netLaneArchetypeData.m_AreaLaneArchetype);
			m_CommandBuffer.SetComponent(jobIndex, e, component5);
			m_CommandBuffer.SetComponent(jobIndex, e, component);
			m_CommandBuffer.SetComponent(jobIndex, e, component2);
			m_CommandBuffer.SetComponent(jobIndex, e, component3);
			m_CommandBuffer.AddComponent(jobIndex, e, component4);
			m_CommandBuffer.AddComponent(jobIndex, e, component6);
			if (m_PrefabNetLaneData.TryGetComponent(lanePrefab, out var componentData) && (componentData.m_Flags & LaneFlags.PseudoRandom) != 0)
			{
				m_CommandBuffer.SetComponent(jobIndex, e, pseudoRandomSeed);
			}
			if (!isTemp)
			{
				return;
			}
			temp.m_Original = SelectOriginal(originalLanes, laneKey, heights);
			m_CommandBuffer.AddComponent(jobIndex, e, temp);
			if (temp.m_Original != Entity.Null)
			{
				if (m_OverriddenData.HasComponent(temp.m_Original))
				{
					m_CommandBuffer.AddComponent(jobIndex, e, default(Overridden));
				}
				if (m_CutRanges.TryGetBuffer(temp.m_Original, out var bufferData))
				{
					m_CommandBuffer.AddBuffer<CutRange>(jobIndex, e).CopyFrom(bufferData);
				}
			}
		}

		private Entity SelectOld(NativeParallelMultiHashMap<AreaLaneKey, AreaLaneValue> oldLanes, AreaLaneKey laneKey, float2 heights)
		{
			Entity result = Entity.Null;
			if (oldLanes.IsCreated && oldLanes.TryGetFirstValue(laneKey, out var item, out var it))
			{
				NativeParallelMultiHashMapIterator<AreaLaneKey> it2 = it;
				float num = float.MaxValue;
				result = item.m_Lane;
				do
				{
					float num2 = math.csum(math.abs(heights - item.m_Heights));
					if (num2 < num)
					{
						it2 = it;
						num = num2;
						result = item.m_Lane;
					}
				}
				while (oldLanes.TryGetNextValue(out item, ref it));
				oldLanes.Remove(it2);
			}
			return result;
		}

		private Entity SelectOriginal(NativeParallelMultiHashMap<AreaLaneKey, AreaLaneValue> originalLanes, AreaLaneKey laneKey, float2 heights)
		{
			Entity result = Entity.Null;
			if (originalLanes.IsCreated && originalLanes.TryGetFirstValue(laneKey, out var item, out var it))
			{
				NativeParallelMultiHashMapIterator<AreaLaneKey> it2 = it;
				float num = float.MaxValue;
				do
				{
					float num2 = math.csum(math.abs(heights - item.m_Heights));
					if (num2 < num)
					{
						it2 = it;
						num = num2;
						result = item.m_Lane;
					}
				}
				while (originalLanes.TryGetNextValue(out item, ref it));
				originalLanes.Remove(it2);
			}
			return result;
		}

		private void FindOriginalLanes(Entity originalArea, out NativeParallelMultiHashMap<AreaLaneKey, AreaLaneValue> originalConnections)
		{
			originalConnections = default(NativeParallelMultiHashMap<AreaLaneKey, AreaLaneValue>);
			if (!m_SubLanes.HasBuffer(originalArea))
			{
				return;
			}
			DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[originalArea];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity subLane = dynamicBuffer[i].m_SubLane;
				if (m_SecondaryLaneData.HasComponent(subLane))
				{
					Curve curve = m_CurveData[subLane];
					if (!originalConnections.IsCreated)
					{
						originalConnections = new NativeParallelMultiHashMap<AreaLaneKey, AreaLaneValue>(dynamicBuffer.Length, Allocator.Temp);
					}
					Game.Net.ConnectionLane componentData;
					LaneType laneType = ((!m_ConnectionLaneData.TryGetComponent(subLane, out componentData)) ? LaneType.Border : (((componentData.m_Flags & ConnectionLaneFlags.Road) == 0) ? LaneType.Pedestrian : LaneType.Road));
					originalConnections.Add(new AreaLaneKey(laneType, curve.m_Bezier.a.xz, curve.m_Bezier.d.xz), new AreaLaneValue(subLane, curve.m_Bezier.a.y, curve.m_Bezier.d.y));
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Area> __Game_Areas_Area_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Lot> __Game_Areas_Lot_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Overridden> __Game_Common_Overridden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.SecondaryLane> __Game_Net_SecondaryLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaLane> __Game_Net_AreaLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Routes.TakeoffLocation> __Game_Routes_TakeoffLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AccessLane> __Game_Routes_AccessLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteLane> __Game_Routes_RouteLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NavigationAreaData> __Game_Prefabs_NavigationAreaData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EnclosedAreaData> __Game_Prefabs_EnclosedAreaData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneArchetypeData> __Game_Prefabs_NetLaneArchetypeData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CutRange> __Game_Net_CutRange_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Areas_Area_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Area>(isReadOnly: true);
			__Game_Areas_Lot_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Lot>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PseudoRandomSeed>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Common_Overridden_RO_ComponentLookup = state.GetComponentLookup<Overridden>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
			__Game_Net_SecondaryLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.SecondaryLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Net_AreaLane_RO_ComponentLookup = state.GetComponentLookup<AreaLane>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Routes_TakeoffLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Routes.TakeoffLocation>(isReadOnly: true);
			__Game_Routes_AccessLane_RO_ComponentLookup = state.GetComponentLookup<AccessLane>(isReadOnly: true);
			__Game_Routes_RouteLane_RO_ComponentLookup = state.GetComponentLookup<RouteLane>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Prefabs_NavigationAreaData_RO_ComponentLookup = state.GetComponentLookup<NavigationAreaData>(isReadOnly: true);
			__Game_Prefabs_EnclosedAreaData_RO_ComponentLookup = state.GetComponentLookup<EnclosedAreaData>(isReadOnly: true);
			__Game_Prefabs_NetLaneArchetypeData_RO_ComponentLookup = state.GetComponentLookup<NetLaneArchetypeData>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_CutRange_RO_BufferLookup = state.GetBufferLookup<CutRange>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
		}
	}

	private ModificationBarrier4B m_ModificationBarrier;

	private EntityQuery m_ModificationQuery;

	private EntityQuery m_ConnectionQuery;

	private ComponentTypeSet m_AppliedTypes;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4B>();
		m_ModificationQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Area>(),
				ComponentType.ReadOnly<Game.Net.SubLane>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_ConnectionQuery = GetEntityQuery(ComponentType.ReadOnly<ConnectionLaneData>(), ComponentType.ReadOnly<PrefabData>());
		m_AppliedTypes = new ComponentTypeSet(ComponentType.ReadWrite<Applied>(), ComponentType.ReadWrite<Created>(), ComponentType.ReadWrite<Updated>());
		RequireForUpdate(m_ModificationQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<Entity> connectionPrefabs = m_ConnectionQuery.ToEntityListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new UpdateSecondaryLanesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_AreaType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Area_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LotType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Lot_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PseudoRandomSeedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OverriddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Overridden_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SecondaryLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SecondaryLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AreaLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_AreaLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TakeoffLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_TakeoffLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AccessLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_AccessLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RouteLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_RouteLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNavigationAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NavigationAreaData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabEnclosedAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_EnclosedAreaData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetLaneArchetypeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneArchetypeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
			m_Triangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_CutRanges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_CutRange_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_ConnectionPrefabs = connectionPrefabs,
			m_AppliedTypes = m_AppliedTypes,
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_ModificationQuery, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
		connectionPrefabs.Dispose(jobHandle);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle;
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
	public AreaConnectionSystem()
	{
	}
}
