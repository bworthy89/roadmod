using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Net;

[CompilerGenerated]
public class LaneConnectionSystem : GameSystemBase
{
	[BurstCompile]
	private struct FindUpdatedLanesJob : IJobParallelForDefer
	{
		private struct Iterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds2 m_Bounds;

			public ComponentLookup<Curve> m_CurveData;

			public BufferLookup<SubLane> m_SubLanes;

			public NativeQueue<Entity>.ParallelWriter m_ResultQueue;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds) || !m_SubLanes.HasBuffer(entity))
				{
					return;
				}
				DynamicBuffer<SubLane> dynamicBuffer = m_SubLanes[entity];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity subLane = dynamicBuffer[i].m_SubLane;
					if (MathUtils.Intersect(MathUtils.Bounds(m_CurveData[subLane].m_Bezier.xz), m_Bounds))
					{
						m_ResultQueue.Enqueue(entity);
					}
				}
			}
		}

		[ReadOnly]
		public NativeArray<Bounds2> m_Bounds;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public BufferLookup<SubLane> m_SubLanes;

		public NativeQueue<Entity>.ParallelWriter m_ResultQueue;

		public void Execute(int index)
		{
			Iterator iterator = new Iterator
			{
				m_Bounds = m_Bounds[index],
				m_CurveData = m_CurveData,
				m_SubLanes = m_SubLanes,
				m_ResultQueue = m_ResultQueue
			};
			m_NetSearchTree.Iterate(ref iterator);
		}
	}

	[BurstCompile]
	private struct CheckUpdatedLanesJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public BufferTypeHandle<SubLane> m_SubLaneType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public BufferLookup<SpawnLocationElement> m_SpawnLocations;

		public NativeQueue<Entity> m_ResultQueue;

		public void Execute()
		{
			NativeHashSet<Entity> nativeHashSet = new NativeHashSet<Entity>(10, Allocator.Temp);
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				NativeArray<Owner> nativeArray = archetypeChunk.GetNativeArray(ref m_OwnerType);
				BufferAccessor<SubLane> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_SubLaneType);
				for (int j = 0; j < bufferAccessor.Length; j++)
				{
					DynamicBuffer<SubLane> dynamicBuffer = bufferAccessor[j];
					for (int k = 0; k < dynamicBuffer.Length; k++)
					{
						m_ResultQueue.Enqueue(dynamicBuffer[k].m_SubLane);
					}
					if (!CollectionUtils.TryGet(nativeArray, j, out var value))
					{
						continue;
					}
					Owner componentData;
					while (m_OwnerData.TryGetComponent(value.m_Owner, out componentData))
					{
						value = componentData;
					}
					if (!m_SpawnLocations.TryGetBuffer(value.m_Owner, out var bufferData) || !nativeHashSet.Add(value.m_Owner))
					{
						continue;
					}
					for (int l = 0; l < bufferData.Length; l++)
					{
						SpawnLocationElement spawnLocationElement = bufferData[l];
						if (spawnLocationElement.m_Type == SpawnLocationType.ParkingLane)
						{
							m_ResultQueue.Enqueue(spawnLocationElement.m_SpawnLocation);
						}
					}
				}
			}
			nativeHashSet.Dispose();
		}
	}

	[BurstCompile]
	private struct ListUpdatedLanesJob : IJob
	{
		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct EntityComparer : IComparer<Entity>
		{
			public int Compare(Entity x, Entity y)
			{
				return x.Index - y.Index;
			}
		}

		public NativeQueue<Entity> m_UpdatedQueue1;

		public NativeQueue<Entity> m_UpdatedQueue2;

		public NativeQueue<Entity> m_UpdatedQueue3;

		public NativeList<Entity> m_UpdatedList;

		public void Execute()
		{
			int count = m_UpdatedQueue1.Count;
			int num = count + m_UpdatedQueue2.Count;
			int num2 = num + m_UpdatedQueue3.Count;
			m_UpdatedList.ResizeUninitialized(num2);
			for (int i = 0; i < count; i++)
			{
				m_UpdatedList[i] = m_UpdatedQueue1.Dequeue();
			}
			for (int j = count; j < num; j++)
			{
				m_UpdatedList[j] = m_UpdatedQueue2.Dequeue();
			}
			for (int k = num; k < num2; k++)
			{
				m_UpdatedList[k] = m_UpdatedQueue3.Dequeue();
			}
			m_UpdatedList.Sort(default(EntityComparer));
			Entity entity = Entity.Null;
			int num3 = 0;
			int num4 = 0;
			while (num3 < m_UpdatedList.Length)
			{
				Entity entity2 = m_UpdatedList[num3++];
				if (entity2 != entity)
				{
					m_UpdatedList[num4++] = entity2;
					entity = entity2;
				}
			}
			if (num4 < m_UpdatedList.Length)
			{
				m_UpdatedList.RemoveRangeSwapBack(num4, m_UpdatedList.Length - num4);
			}
		}
	}

	[BurstCompile]
	private struct FindLaneConnectionJob : IJobParallelForDefer
	{
		private struct Iterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public Curve m_Curve;

			public float2 m_MaxDistance;

			public ConnectionLaneFlags m_LaneFlags;

			public RoadTypes m_RoadTypes;

			public ComponentLookup<Curve> m_CurveData;

			public ComponentLookup<ConnectionLane> m_ConnectionLaneData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<NavigationAreaData> m_PrefabNavigationAreaData;

			public BufferLookup<SubLane> m_Lanes;

			public BufferLookup<Game.Areas.Node> m_AreaNodes;

			public BufferLookup<Triangle> m_AreaTriangles;

			public LaneConnection m_BestConnection;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Curve.m_Bezier.a.xz) | MathUtils.Intersect(bounds.m_Bounds.xz, m_Curve.m_Bezier.d.xz);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem item)
			{
				bool2 x = default(bool2);
				x.x = MathUtils.Intersect(bounds.m_Bounds.xz, m_Curve.m_Bezier.a.xz);
				x.y = MathUtils.Intersect(bounds.m_Bounds.xz, m_Curve.m_Bezier.d.xz);
				if (!math.any(x) || !m_Lanes.HasBuffer(item.m_Area))
				{
					return;
				}
				DynamicBuffer<Game.Areas.Node> nodes = m_AreaNodes[item.m_Area];
				Triangle triangle = m_AreaTriangles[item.m_Area][item.m_Triangle];
				Triangle3 triangle2 = AreaUtils.GetTriangle3(nodes, triangle);
				float2 @float = float.MaxValue;
				if (x.x && MathUtils.Intersect(triangle2.xz, m_Curve.m_Bezier.a.xz, out var t))
				{
					@float.x = math.abs(MathUtils.Position(triangle2, t).y - m_Curve.m_Bezier.a.y);
				}
				if (x.y && MathUtils.Intersect(triangle2.xz, m_Curve.m_Bezier.d.xz, out var t2))
				{
					@float.y = math.abs(MathUtils.Position(triangle2, t2).y - m_Curve.m_Bezier.d.y);
				}
				x = @float < m_MaxDistance;
				if (!math.any(x))
				{
					return;
				}
				DynamicBuffer<SubLane> dynamicBuffer = m_Lanes[item.m_Area];
				float2 float2 = float.MaxValue;
				LaneConnection laneConnection = default(LaneConnection);
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity subLane = dynamicBuffer[i].m_SubLane;
					if (!m_ConnectionLaneData.HasComponent(subLane))
					{
						continue;
					}
					ConnectionLane connectionLane = m_ConnectionLaneData[subLane];
					if ((connectionLane.m_Flags & m_LaneFlags) == 0 || ((m_RoadTypes != RoadTypes.None) & ((connectionLane.m_RoadTypes & m_RoadTypes) == 0)))
					{
						continue;
					}
					Curve curve = m_CurveData[subLane];
					if (!MathUtils.Intersect(triangle2.xz, curve.m_Bezier.a.xz, out var t3) && !MathUtils.Intersect(triangle2.xz, curve.m_Bezier.d.xz, out t3))
					{
						continue;
					}
					if (x.x)
					{
						float t4;
						float num = MathUtils.Distance(curve.m_Bezier, m_Curve.m_Bezier.a, out t4);
						if (num < float2.x)
						{
							float2.x = num;
							laneConnection.m_StartLane = subLane;
							laneConnection.m_StartPosition = t4;
						}
					}
					if (x.y)
					{
						float t5;
						float num2 = MathUtils.Distance(curve.m_Bezier, m_Curve.m_Bezier.d, out t5);
						if (num2 < float2.y)
						{
							float2.y = num2;
							laneConnection.m_EndLane = subLane;
							laneConnection.m_EndPosition = t5;
						}
					}
				}
				if (laneConnection.m_StartLane != Entity.Null)
				{
					m_MaxDistance.x = @float.x;
					m_BestConnection.m_StartLane = laneConnection.m_StartLane;
					m_BestConnection.m_StartPosition = laneConnection.m_StartPosition;
				}
				if (laneConnection.m_EndLane != Entity.Null)
				{
					m_MaxDistance.y = @float.y;
					m_BestConnection.m_EndLane = laneConnection.m_EndLane;
					m_BestConnection.m_EndPosition = laneConnection.m_EndPosition;
				}
			}
		}

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<SlaveLane> m_SlaveLaneData;

		[ReadOnly]
		public ComponentLookup<CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<PedestrianLane> m_PedestrianLaneData;

		[ReadOnly]
		public ComponentLookup<ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<Attached> m_AttachedData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_PrefabNetLaneData;

		[ReadOnly]
		public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> m_PrefabParkingLaneData;

		[ReadOnly]
		public ComponentLookup<NavigationAreaData> m_PrefabNavigationAreaData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<LaneConnection> m_LaneConnectionData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public BufferLookup<SubLane> m_Lanes;

		[ReadOnly]
		public BufferLookup<SubNet> m_SubNets;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_AreaTriangles;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public NativeArray<Entity> m_Entities;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(int index)
		{
			Entity entity = m_Entities[index];
			LaneConnection laneConnection = FindLaneConnection(entity);
			if (laneConnection.m_StartLane == Entity.Null && laneConnection.m_EndLane == Entity.Null)
			{
				if (m_LaneConnectionData.HasComponent(entity))
				{
					if (!m_UpdatedData.HasComponent(entity))
					{
						m_CommandBuffer.AddComponent(index, entity, default(PathfindUpdated));
					}
					m_CommandBuffer.RemoveComponent<LaneConnection>(index, entity);
				}
			}
			else if (m_LaneConnectionData.HasComponent(entity))
			{
				LaneConnection laneConnection2 = m_LaneConnectionData[entity];
				if (laneConnection2.m_StartLane != laneConnection.m_StartLane || laneConnection2.m_EndLane != laneConnection.m_EndLane || laneConnection2.m_StartPosition != laneConnection.m_StartPosition || laneConnection2.m_EndPosition != laneConnection.m_EndPosition)
				{
					m_LaneConnectionData[entity] = laneConnection;
					if (!m_UpdatedData.HasComponent(entity))
					{
						m_CommandBuffer.AddComponent(index, entity, default(PathfindUpdated));
					}
				}
			}
			else
			{
				m_CommandBuffer.AddComponent(index, entity, laneConnection);
				if (!m_UpdatedData.HasComponent(entity))
				{
					m_CommandBuffer.AddComponent(index, entity, default(PathfindUpdated));
				}
			}
		}

		public LaneConnection FindLaneConnection(Entity entity)
		{
			if (m_SlaveLaneData.HasComponent(entity) || m_ConnectionLaneData.HasComponent(entity))
			{
				return default(LaneConnection);
			}
			PrefabRef prefabRef = m_PrefabRefData[entity];
			if (!m_PrefabNetLaneData.HasComponent(prefabRef.m_Prefab))
			{
				return default(LaneConnection);
			}
			NetLaneData netLaneData = m_PrefabNetLaneData[prefabRef.m_Prefab];
			if ((netLaneData.m_Flags & LaneFlags.Parking) != 0 && m_ParkingLaneData.HasComponent(entity))
			{
				ParkingLane parkingLane = m_ParkingLaneData[entity];
				LaneConnection result = default(LaneConnection);
				if ((parkingLane.m_Flags & ParkingLaneFlags.FindConnections) != 0)
				{
					parkingLane.m_Flags &= ~(ParkingLaneFlags.AdditionalStart | ParkingLaneFlags.ParkingInverted | ParkingLaneFlags.SecondaryStart);
					parkingLane.m_AdditionalStartNode = default(PathNode);
					result = FindParkingConnection(entity, ref parkingLane, prefabRef, netLaneData);
					m_ParkingLaneData[entity] = parkingLane;
				}
				return result;
			}
			if ((netLaneData.m_Flags & (LaneFlags.Road | LaneFlags.Pedestrian)) != 0)
			{
				return FindAreaConnection(entity, prefabRef, netLaneData);
			}
			return default(LaneConnection);
		}

		private LaneConnection FindParkingConnection(Entity entity, ref ParkingLane parkingLane, PrefabRef prefabRef, NetLaneData netLaneData)
		{
			Curve curve = m_CurveData[entity];
			Owner owner = m_OwnerData[entity];
			Owner componentData;
			while (m_OwnerData.TryGetComponent(owner.m_Owner, out componentData) && !m_BuildingData.HasComponent(owner.m_Owner))
			{
				owner = componentData;
			}
			if (!m_BuildingData.HasComponent(owner.m_Owner) && m_AttachedData.TryGetComponent(owner.m_Owner, out var componentData2) && m_PrefabRefData.HasComponent(componentData2.m_Parent))
			{
				owner.m_Owner = componentData2.m_Parent;
			}
			LaneConnection result = default(LaneConnection);
			float3 @float = MathUtils.Position(curve.m_Bezier, 0.5f);
			float bestPedestrianDistance = float.MaxValue;
			float bestRoadDistance = float.MaxValue;
			bool bestRoadInverted = false;
			bool bestRoadSecondary = false;
			RoadTypes roadTypes = RoadTypes.Car;
			float3 float2 = @float;
			float3 roadSearchPosition = @float;
			float2 value = MathUtils.Tangent(curve.m_Bezier, 0.5f).xz;
			if (m_PrefabParkingLaneData.TryGetComponent(prefabRef.m_Prefab, out var componentData3))
			{
				roadTypes = componentData3.m_RoadTypes;
				if (MathUtils.TryNormalize(ref value))
				{
					float2 float3 = MathUtils.RotateRight(value, componentData3.m_SlotAngle) * (componentData3.m_SlotSize.y * 0.5f);
					float2.xz -= float3;
					roadSearchPosition.xz = math.select(float2.xz, roadSearchPosition.xz + float3, componentData3.m_SlotAngle < 0.01f);
				}
			}
			FindParkingConnection(owner.m_Owner, @float, float2, roadSearchPosition, roadTypes, ref result, ref bestPedestrianDistance, ref bestRoadDistance, ref bestRoadInverted, ref bestRoadSecondary);
			if (m_InstalledUpgrades.TryGetBuffer(owner.m_Owner, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					FindParkingConnection(bufferData[i].m_Upgrade, @float, float2, roadSearchPosition, roadTypes, ref result, ref bestPedestrianDistance, ref bestRoadDistance, ref bestRoadInverted, ref bestRoadSecondary);
				}
			}
			if ((netLaneData.m_Flags & LaneFlags.Twoway) != 0 && m_CarLaneData.TryGetComponent(result.m_StartLane, out var componentData4) && m_OwnerData.TryGetComponent(result.m_StartLane, out var componentData5) && m_Lanes.TryGetBuffer(componentData5.m_Owner, out var bufferData2))
			{
				PathMethod pathMethod = ((roadTypes == RoadTypes.Bicycle) ? PathMethod.Bicycle : PathMethod.Road);
				for (int j = 0; j < bufferData2.Length; j++)
				{
					if ((bufferData2[j].m_PathMethods & pathMethod) != 0)
					{
						Entity subLane = bufferData2[j].m_SubLane;
						if (!m_SlaveLaneData.HasComponent(subLane) && m_CarLaneData.TryGetComponent(subLane, out var componentData6) && ((componentData4.m_Flags ^ componentData6.m_Flags) & CarLaneFlags.Invert) != 0 && componentData4.m_CarriagewayGroup == componentData6.m_CarriagewayGroup)
						{
							Lane lane = m_LaneData[subLane];
							MathUtils.Distance(m_CurveData[subLane].m_Bezier, float2, out var t);
							parkingLane.m_AdditionalStartNode = new PathNode(lane.m_MiddleNode, t);
							parkingLane.m_Flags |= ParkingLaneFlags.AdditionalStart;
							break;
						}
					}
				}
			}
			if (bestRoadInverted)
			{
				parkingLane.m_Flags |= ParkingLaneFlags.ParkingInverted;
			}
			if (bestRoadSecondary)
			{
				parkingLane.m_Flags |= ParkingLaneFlags.SecondaryStart;
			}
			return result;
		}

		private void FindParkingConnection(Entity owner, float3 pedestrianSearchPosition, float3 roadSearchPosition, float3 roadSearchPosition2, RoadTypes roadTypes, ref LaneConnection result, ref float bestPedestrianDistance, ref float bestRoadDistance, ref bool bestRoadInverted, ref bool bestRoadSecondary)
		{
			PathMethod pathMethod = PathMethod.Pedestrian;
			pathMethod = (PathMethod)((uint)pathMethod | (uint)((roadTypes == RoadTypes.Bicycle) ? 16384 : 2));
			bool canInvertRoad = !roadSearchPosition.Equals(roadSearchPosition2);
			FindParkingConnection(owner, pedestrianSearchPosition, roadSearchPosition, roadSearchPosition2, roadTypes, pathMethod, canInvertRoad, ref result, ref bestPedestrianDistance, ref bestRoadDistance, ref bestRoadInverted, ref bestRoadSecondary);
			if (m_SubNets.TryGetBuffer(owner, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					FindParkingConnection(bufferData[i].m_SubNet, pedestrianSearchPosition, roadSearchPosition, roadSearchPosition2, roadTypes, pathMethod, canInvertRoad, ref result, ref bestPedestrianDistance, ref bestRoadDistance, ref bestRoadInverted, ref bestRoadSecondary);
				}
			}
			if (m_SubAreas.TryGetBuffer(owner, out var bufferData2))
			{
				for (int j = 0; j < bufferData2.Length; j++)
				{
					FindParkingConnection(bufferData2[j].m_Area, pedestrianSearchPosition, roadSearchPosition, roadTypes, pathMethod, ref result, ref bestPedestrianDistance, ref bestRoadDistance, ref bestRoadInverted, ref bestRoadSecondary);
				}
			}
		}

		private void FindParkingConnection(Entity netEntity, float3 pedestrianSearchPosition, float3 roadSearchPosition, float3 roadSearchPosition2, RoadTypes roadTypes, PathMethod pathMethods, bool canInvertRoad, ref LaneConnection result, ref float bestPedestrianDistance, ref float bestRoadDistance, ref bool bestRoadInverted, ref bool bestRoadSecondary)
		{
			if (!m_Lanes.TryGetBuffer(netEntity, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				if ((bufferData[i].m_PathMethods & pathMethods) == 0)
				{
					continue;
				}
				Entity subLane = bufferData[i].m_SubLane;
				if (m_SlaveLaneData.HasComponent(subLane) || m_ConnectionLaneData.HasComponent(subLane))
				{
					continue;
				}
				Curve curve = m_CurveData[subLane];
				PrefabRef prefabRef = m_PrefabRefData[subLane];
				NetLaneData netLaneData = m_PrefabNetLaneData[prefabRef.m_Prefab];
				CarLane componentData2;
				if ((netLaneData.m_Flags & LaneFlags.Pedestrian) != 0)
				{
					if (m_PedestrianLaneData.TryGetComponent(subLane, out var componentData))
					{
						if (roadTypes == RoadTypes.Bicycle && (componentData.m_Flags & PedestrianLaneFlags.AllowBicycle) != 0)
						{
							netLaneData.m_Flags |= LaneFlags.Road;
						}
						if ((componentData.m_Flags & PedestrianLaneFlags.AllowMiddle) == 0)
						{
							continue;
						}
					}
				}
				else if ((netLaneData.m_Flags & LaneFlags.Road) != 0 && ((m_PrefabCarLaneData[prefabRef.m_Prefab].m_RoadTypes & roadTypes) == 0 || (m_CarLaneData.TryGetComponent(subLane, out componentData2) && (componentData2.m_Flags & CarLaneFlags.Unsafe) != 0)))
				{
					continue;
				}
				if ((netLaneData.m_Flags & LaneFlags.Pedestrian) != 0)
				{
					float num = MathUtils.Distance(MathUtils.Bounds(curve.m_Bezier), pedestrianSearchPosition);
					if (num < bestPedestrianDistance)
					{
						num = MathUtils.Distance(curve.m_Bezier, pedestrianSearchPosition, out var t);
						if (num < bestPedestrianDistance)
						{
							bestPedestrianDistance = num;
							result.m_EndLane = subLane;
							result.m_EndPosition = t;
						}
					}
				}
				if ((netLaneData.m_Flags & LaneFlags.Road) == 0)
				{
					continue;
				}
				float num2 = MathUtils.Distance(MathUtils.Bounds(curve.m_Bezier), roadSearchPosition);
				if (num2 < bestRoadDistance)
				{
					num2 = MathUtils.Distance(curve.m_Bezier, roadSearchPosition, out var t2);
					if (canInvertRoad)
					{
						float3 x = MathUtils.Tangent(curve.m_Bezier, t2);
						num2 += math.select(0f, num2, math.dot(x, roadSearchPosition2 - roadSearchPosition) < 0f);
					}
					if (num2 < bestRoadDistance)
					{
						bestRoadDistance = num2;
						bestRoadInverted = false;
						bestRoadSecondary = (netLaneData.m_Flags & LaneFlags.Pedestrian) != 0;
						result.m_StartLane = subLane;
						result.m_StartPosition = t2;
					}
				}
				if (!canInvertRoad)
				{
					continue;
				}
				num2 = MathUtils.Distance(MathUtils.Bounds(curve.m_Bezier), roadSearchPosition2);
				if (num2 < bestRoadDistance)
				{
					num2 = MathUtils.Distance(curve.m_Bezier, roadSearchPosition2, out var t3);
					float3 x2 = MathUtils.Tangent(curve.m_Bezier, t3);
					num2 += math.select(0f, num2, math.dot(x2, roadSearchPosition - roadSearchPosition2) < 0f);
					if (num2 < bestRoadDistance)
					{
						bestRoadDistance = num2;
						bestRoadInverted = true;
						bestRoadSecondary = (netLaneData.m_Flags & LaneFlags.Pedestrian) != 0;
						result.m_StartLane = subLane;
						result.m_StartPosition = t3;
					}
				}
			}
		}

		private void FindParkingConnection(Entity areaEntity, float3 pedestrianSearchPosition, float3 roadSearchPosition, RoadTypes roadTypes, PathMethod pathMethods, ref LaneConnection result, ref float bestPedestrianDistance, ref float bestRoadDistance, ref bool bestRoadInverted, ref bool bestRoadSecondary)
		{
			if (!m_Lanes.TryGetBuffer(areaEntity, out var bufferData))
			{
				return;
			}
			DynamicBuffer<Game.Areas.Node> nodes = m_AreaNodes[areaEntity];
			DynamicBuffer<Triangle> dynamicBuffer = m_AreaTriangles[areaEntity];
			float num = bestPedestrianDistance;
			float num2 = bestRoadDistance;
			Triangle3 triangle = default(Triangle3);
			Triangle3 triangle2 = default(Triangle3);
			float3 position = default(float3);
			float3 position2 = default(float3);
			bool flag = false;
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Triangle triangle3 = dynamicBuffer[i];
				Triangle3 triangle4 = AreaUtils.GetTriangle3(nodes, triangle3);
				float2 t;
				float num3 = MathUtils.Distance(triangle4, pedestrianSearchPosition, out t);
				float2 t2;
				float num4 = MathUtils.Distance(triangle4, roadSearchPosition, out t2);
				if (num3 < num)
				{
					num = num3;
					triangle = triangle4;
					position = MathUtils.Position(triangle4, t);
					flag = true;
				}
				if (num4 < num2)
				{
					num2 = num4;
					triangle2 = triangle4;
					position2 = MathUtils.Position(triangle4, t2);
					flag = true;
				}
			}
			if (!flag)
			{
				return;
			}
			float num5 = float.MaxValue;
			float num6 = float.MaxValue;
			bool flag2 = false;
			for (int j = 0; j < bufferData.Length; j++)
			{
				if ((bufferData[j].m_PathMethods & pathMethods) == 0)
				{
					continue;
				}
				Entity subLane = bufferData[j].m_SubLane;
				if (!m_ConnectionLaneData.TryGetComponent(subLane, out var componentData))
				{
					continue;
				}
				if ((componentData.m_Flags & ConnectionLaneFlags.Pedestrian) != 0)
				{
					if (roadTypes == RoadTypes.Bicycle && (componentData.m_Flags & (ConnectionLaneFlags.SecondaryStart | ConnectionLaneFlags.SecondaryEnd)) != 0)
					{
						componentData.m_Flags |= ConnectionLaneFlags.Road;
					}
				}
				else if ((componentData.m_Flags & ConnectionLaneFlags.Road) != 0 && (componentData.m_RoadTypes & roadTypes) == 0)
				{
					continue;
				}
				Curve curve = m_CurveData[subLane];
				if ((componentData.m_Flags & ConnectionLaneFlags.Pedestrian) != 0 && num < bestPedestrianDistance && (MathUtils.Intersect(triangle.xz, curve.m_Bezier.a.xz, out var t3) || MathUtils.Intersect(triangle.xz, curve.m_Bezier.d.xz, out t3)))
				{
					float t4;
					float num7 = MathUtils.Distance(curve.m_Bezier, position, out t4);
					if (num7 < num5)
					{
						num5 = num7;
						result.m_EndLane = subLane;
						result.m_EndPosition = t4;
					}
				}
				if ((componentData.m_Flags & ConnectionLaneFlags.Road) != 0 && num2 < bestRoadDistance && (MathUtils.Intersect(triangle2.xz, curve.m_Bezier.a.xz, out t3) || MathUtils.Intersect(triangle2.xz, curve.m_Bezier.d.xz, out t3)))
				{
					float t5;
					float num8 = MathUtils.Distance(curve.m_Bezier, position2, out t5);
					if (num8 < num6)
					{
						num6 = num8;
						flag2 = (componentData.m_Flags & ConnectionLaneFlags.Pedestrian) != 0;
						result.m_StartLane = subLane;
						result.m_StartPosition = t5;
					}
				}
			}
			if (num5 != float.MaxValue)
			{
				bestPedestrianDistance = num;
			}
			if (num6 != float.MaxValue)
			{
				bestRoadDistance = num2;
				bestRoadInverted = false;
				bestRoadSecondary = flag2;
			}
		}

		private LaneConnection FindAreaConnection(Entity entity, PrefabRef prefabRef, NetLaneData netLaneData)
		{
			Curve curve = m_CurveData[entity];
			ConnectionLaneFlags connectionLaneFlags = (ConnectionLaneFlags)0;
			RoadTypes roadTypes = RoadTypes.None;
			if ((netLaneData.m_Flags & LaneFlags.Pedestrian) != 0)
			{
				connectionLaneFlags |= ConnectionLaneFlags.Pedestrian;
			}
			if ((netLaneData.m_Flags & LaneFlags.Road) != 0)
			{
				CarLaneData carLaneData = m_PrefabCarLaneData[prefabRef.m_Prefab];
				connectionLaneFlags |= ConnectionLaneFlags.Road;
				roadTypes |= carLaneData.m_RoadTypes;
			}
			Iterator iterator = new Iterator
			{
				m_Curve = curve,
				m_MaxDistance = 2f,
				m_LaneFlags = connectionLaneFlags,
				m_RoadTypes = roadTypes,
				m_CurveData = m_CurveData,
				m_ConnectionLaneData = m_ConnectionLaneData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabNavigationAreaData = m_PrefabNavigationAreaData,
				m_Lanes = m_Lanes,
				m_AreaNodes = m_AreaNodes,
				m_AreaTriangles = m_AreaTriangles
			};
			m_AreaSearchTree.Iterate(ref iterator);
			return iterator.m_BestConnection;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubLane> __Game_Net_SubLane_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SpawnLocationElement> __Game_Buildings_SpawnLocationElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarLaneData> __Game_Prefabs_CarLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> __Game_Prefabs_ParkingLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NavigationAreaData> __Game_Prefabs_NavigationAreaData_RO_ComponentLookup;

		public ComponentLookup<LaneConnection> __Game_Net_LaneConnection_RW_ComponentLookup;

		public ComponentLookup<ParkingLane> __Game_Net_ParkingLane_RW_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<SubLane>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubLane>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Buildings_SpawnLocationElement_RO_BufferLookup = state.GetBufferLookup<SpawnLocationElement>(isReadOnly: true);
			__Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<CarLane>(isReadOnly: true);
			__Game_Net_PedestrianLane_RO_ComponentLookup = state.GetComponentLookup<PedestrianLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<ConnectionLane>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
			__Game_Prefabs_ParkingLaneData_RO_ComponentLookup = state.GetComponentLookup<ParkingLaneData>(isReadOnly: true);
			__Game_Prefabs_NavigationAreaData_RO_ComponentLookup = state.GetComponentLookup<NavigationAreaData>(isReadOnly: true);
			__Game_Net_LaneConnection_RW_ComponentLookup = state.GetComponentLookup<LaneConnection>();
			__Game_Net_ParkingLane_RW_ComponentLookup = state.GetComponentLookup<ParkingLane>();
			__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<SubNet>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
		}
	}

	private ModificationBarrier5 m_ModificationBarrier;

	private SearchSystem m_NetSearchSystem;

	private Game.Areas.SearchSystem m_AreaSearchSystem;

	private Game.Areas.UpdateCollectSystem m_AreaUpdateCollectSystem;

	private EntityQuery m_UpdatedQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
		m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
		m_AreaUpdateCollectSystem = base.World.GetOrCreateSystemManaged<Game.Areas.UpdateCollectSystem>();
		m_UpdatedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<SubLane>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Edge>(),
				ComponentType.ReadOnly<Node>(),
				ComponentType.ReadOnly<Object>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
	}

	[Preserve]
	protected override void OnUpdate()
	{
		bool flag = !m_UpdatedQuery.IsEmptyIgnoreFilter;
		if (flag || m_AreaUpdateCollectSystem.lotsUpdated || m_AreaUpdateCollectSystem.spacesUpdated)
		{
			NativeQueue<Entity> updatedQueue = new NativeQueue<Entity>(Allocator.TempJob);
			NativeQueue<Entity> updatedQueue2 = new NativeQueue<Entity>(Allocator.TempJob);
			NativeQueue<Entity> nativeQueue = new NativeQueue<Entity>(Allocator.TempJob);
			NativeList<Entity> nativeList = new NativeList<Entity>(Allocator.TempJob);
			JobHandle jobHandle = default(JobHandle);
			if (m_AreaUpdateCollectSystem.lotsUpdated)
			{
				JobHandle dependencies;
				NativeList<Bounds2> updatedLotBounds = m_AreaUpdateCollectSystem.GetUpdatedLotBounds(out dependencies);
				JobHandle dependencies2;
				JobHandle jobHandle2 = new FindUpdatedLanesJob
				{
					m_Bounds = updatedLotBounds.AsDeferredJobArray(),
					m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies2),
					m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
					m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
					m_ResultQueue = updatedQueue.AsParallelWriter()
				}.Schedule(updatedLotBounds, 1, JobHandle.CombineDependencies(base.Dependency, dependencies, dependencies2));
				m_AreaUpdateCollectSystem.AddLotBoundsReader(jobHandle2);
				m_NetSearchSystem.AddNetSearchTreeReader(jobHandle2);
				jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
			}
			if (m_AreaUpdateCollectSystem.spacesUpdated)
			{
				JobHandle dependencies3;
				NativeList<Bounds2> updatedSpaceBounds = m_AreaUpdateCollectSystem.GetUpdatedSpaceBounds(out dependencies3);
				JobHandle dependencies4;
				JobHandle jobHandle3 = new FindUpdatedLanesJob
				{
					m_Bounds = updatedSpaceBounds.AsDeferredJobArray(),
					m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies4),
					m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
					m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
					m_ResultQueue = updatedQueue2.AsParallelWriter()
				}.Schedule(updatedSpaceBounds, 1, JobHandle.CombineDependencies(base.Dependency, dependencies3, dependencies4));
				m_AreaUpdateCollectSystem.AddSpaceBoundsReader(jobHandle3);
				m_NetSearchSystem.AddNetSearchTreeReader(jobHandle3);
				jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle3);
			}
			if (flag)
			{
				JobHandle outJobHandle;
				CheckUpdatedLanesJob jobData = new CheckUpdatedLanesJob
				{
					m_Chunks = m_UpdatedQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle),
					m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_SubLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
					m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
					m_SpawnLocations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_SpawnLocationElement_RO_BufferLookup, ref base.CheckedStateRef),
					m_ResultQueue = nativeQueue
				};
				JobHandle jobHandle4 = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
				jobData.m_Chunks.Dispose(jobHandle4);
				jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle4);
			}
			ListUpdatedLanesJob jobData2 = new ListUpdatedLanesJob
			{
				m_UpdatedQueue1 = updatedQueue,
				m_UpdatedQueue2 = updatedQueue2,
				m_UpdatedQueue3 = nativeQueue,
				m_UpdatedList = nativeList
			};
			JobHandle dependencies5;
			FindLaneConnectionJob jobData3 = new FindLaneConnectionJob
			{
				m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SlaveLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PedestrianLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AttachedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkingLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNavigationAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NavigationAreaData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LaneConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneConnection_RW_ComponentLookup, ref base.CheckedStateRef),
				m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RW_ComponentLookup, ref base.CheckedStateRef),
				m_Lanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
				m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
				m_AreaTriangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
				m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
				m_Entities = nativeList.AsDeferredJobArray(),
				m_AreaSearchTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies5),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
			};
			JobHandle jobHandle5 = IJobExtensions.Schedule(jobData2, jobHandle);
			JobHandle jobHandle6 = jobData3.Schedule(nativeList, 1, JobHandle.CombineDependencies(jobHandle5, dependencies5));
			updatedQueue.Dispose(jobHandle5);
			updatedQueue2.Dispose(jobHandle5);
			nativeQueue.Dispose(jobHandle5);
			nativeList.Dispose(jobHandle6);
			m_AreaSearchSystem.AddSearchTreeReader(jobHandle6);
			m_ModificationBarrier.AddJobHandleForProducer(jobHandle6);
			base.Dependency = jobHandle6;
		}
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
	public LaneConnectionSystem()
	{
	}
}
