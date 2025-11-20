using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Objects;

[CompilerGenerated]
public class SpawnLocationConnectionSystem : GameSystemBase
{
	[BurstCompile]
	private struct FindUpdatedSpawnLocationsJob : IJobParallelForDefer
	{
		private struct Iterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds2 m_Bounds;

			public ComponentLookup<SpawnLocation> m_SpawnLocationData;

			public NativeQueue<Entity>.ParallelWriter m_ResultQueue;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds) && m_SpawnLocationData.HasComponent(entity))
				{
					m_ResultQueue.Enqueue(entity);
				}
			}
		}

		[ReadOnly]
		public NativeArray<Bounds2> m_Bounds;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_ObjectSearchTree;

		[ReadOnly]
		public ComponentLookup<SpawnLocation> m_SpawnLocationData;

		public NativeQueue<Entity>.ParallelWriter m_ResultQueue;

		public void Execute(int index)
		{
			Iterator iterator = new Iterator
			{
				m_Bounds = MathUtils.Expand(m_Bounds[index], 32f),
				m_SpawnLocationData = m_SpawnLocationData,
				m_ResultQueue = m_ResultQueue
			};
			m_ObjectSearchTree.Iterate(ref iterator);
		}
	}

	[BurstCompile]
	private struct CheckUpdatedSpawnLocationsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<RoadConnectionUpdated> m_RoadConnectionUpdatedType;

		[ReadOnly]
		public BufferLookup<SpawnLocationElement> m_SpawnLocations;

		public NativeQueue<Entity>.ParallelWriter m_ResultQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<RoadConnectionUpdated> nativeArray = chunk.GetNativeArray(ref m_RoadConnectionUpdatedType);
			if (nativeArray.Length != 0)
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity building = nativeArray[i].m_Building;
					DynamicBuffer<SpawnLocationElement> dynamicBuffer = m_SpawnLocations[building];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						if (dynamicBuffer[j].m_Type == SpawnLocationType.SpawnLocation)
						{
							m_ResultQueue.Enqueue(dynamicBuffer[j].m_SpawnLocation);
						}
					}
				}
			}
			else
			{
				NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
				for (int k = 0; k < nativeArray2.Length; k++)
				{
					m_ResultQueue.Enqueue(nativeArray2[k]);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct ListUpdatedSpawnLocationsJob : IJob
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

		public NativeQueue<Entity> m_UpdatedQueue4;

		public NativeList<Entity> m_UpdatedList;

		public void Execute()
		{
			int count = m_UpdatedQueue1.Count;
			int num = count + m_UpdatedQueue2.Count;
			int num2 = num + m_UpdatedQueue3.Count;
			int num3 = num2 + m_UpdatedQueue4.Count;
			m_UpdatedList.ResizeUninitialized(num3);
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
			for (int l = num2; l < num3; l++)
			{
				m_UpdatedList[l] = m_UpdatedQueue4.Dequeue();
			}
			m_UpdatedList.Sort(default(EntityComparer));
			Entity entity = Entity.Null;
			int num4 = 0;
			int num5 = 0;
			while (num4 < m_UpdatedList.Length)
			{
				Entity entity2 = m_UpdatedList[num4++];
				if (entity2 != entity)
				{
					m_UpdatedList[num5++] = entity2;
					entity = entity2;
				}
			}
			if (num5 < m_UpdatedList.Length)
			{
				m_UpdatedList.RemoveRangeSwapBack(num5, m_UpdatedList.Length - num5);
			}
		}
	}

	[BurstCompile]
	private struct FindSpawnLocationConnectionJob : IJobParallelForDefer
	{
		private struct Iterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public Bounds3 m_Bounds;

			public float3 m_Position;

			public float m_MaxDistance;

			public SpawnLocationData m_SpawnLocationData;

			public ComponentLookup<Curve> m_CurveData;

			public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

			public ComponentLookup<SlaveLane> m_SlaveLaneData;

			public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

			public ComponentLookup<Game.Areas.Lot> m_LotData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<NetLaneData> m_PrefabNetLaneData;

			public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

			public ComponentLookup<TrackLaneData> m_PrefabTrackLaneData;

			public BufferLookup<Game.Net.SubLane> m_Lanes;

			public BufferLookup<Game.Areas.Node> m_AreaNodes;

			public BufferLookup<Triangle> m_AreaTriangles;

			public SpawnLocation m_BestLocation;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (MathUtils.Intersect(bounds.m_Bounds, m_Bounds) && TryFindLanes(entity, out var distance, out var spawnLocation) && distance < m_MaxDistance)
				{
					m_Bounds = new Bounds3(m_Position - distance, m_Position + distance);
					m_MaxDistance = distance;
					m_BestLocation = spawnLocation;
				}
			}

			public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem item)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds, m_Bounds) || !m_Lanes.HasBuffer(item.m_Area))
				{
					return;
				}
				DynamicBuffer<Game.Areas.Node> nodes = m_AreaNodes[item.m_Area];
				Triangle triangle = m_AreaTriangles[item.m_Area][item.m_Triangle];
				Triangle3 triangle2 = AreaUtils.GetTriangle3(nodes, triangle);
				float2 t;
				float num = MathUtils.Distance(triangle2, m_Position, out t);
				if (num >= m_MaxDistance)
				{
					return;
				}
				float3 position = MathUtils.Position(triangle2, t);
				if (m_LotData.HasComponent(item.m_Area))
				{
					bool3 @bool = AreaUtils.IsEdge(nodes, triangle);
					@bool &= (math.cmin(triangle.m_Indices.xy) != 0) | (math.cmax(triangle.m_Indices.xy) != 1);
					@bool &= (math.cmin(triangle.m_Indices.yz) != 0) | (math.cmax(triangle.m_Indices.yz) != 1);
					@bool &= (math.cmin(triangle.m_Indices.zx) != 0) | (math.cmax(triangle.m_Indices.zx) != 1);
					if ((@bool.x && MathUtils.Distance(triangle2.ab, position, out var t2) < 0.1f) || (@bool.y && MathUtils.Distance(triangle2.bc, position, out t2) < 0.1f) || (@bool.z && MathUtils.Distance(triangle2.ca, position, out t2) < 0.1f))
					{
						return;
					}
				}
				DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_Lanes[item.m_Area];
				SpawnLocation bestLocation = default(SpawnLocation);
				float num2 = float.MaxValue;
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity subLane = dynamicBuffer[i].m_SubLane;
					if (!m_ConnectionLaneData.HasComponent(subLane) || !CheckLaneType(m_ConnectionLaneData[subLane]))
					{
						continue;
					}
					Curve curve = m_CurveData[subLane];
					float2 t3;
					bool2 x = new bool2(MathUtils.Intersect(triangle2.xz, curve.m_Bezier.a.xz, out t3), MathUtils.Intersect(triangle2.xz, curve.m_Bezier.d.xz, out t3));
					if (math.any(x))
					{
						float t4;
						float num3 = MathUtils.Distance(curve.m_Bezier, position, out t4);
						if (num3 < num2)
						{
							float2 @float = math.select(new float2(0f, 0.49f), math.select(new float2(0.51f, 1f), new float2(0f, 1f), x.x), x.y);
							num2 = num3;
							bestLocation.m_ConnectedLane1 = subLane;
							bestLocation.m_CurvePosition1 = math.clamp(t4, @float.x, @float.y);
						}
					}
				}
				if (bestLocation.m_ConnectedLane1 != Entity.Null)
				{
					m_Bounds = new Bounds3(m_Position - num, m_Position + num);
					m_MaxDistance = num;
					m_BestLocation = bestLocation;
				}
			}

			private bool CheckLaneType(Game.Net.ConnectionLane connectionLane)
			{
				switch (m_SpawnLocationData.m_ConnectionType)
				{
				case RouteConnectionType.Pedestrian:
					if ((connectionLane.m_Flags & ConnectionLaneFlags.Pedestrian) != 0)
					{
						return true;
					}
					return false;
				case RouteConnectionType.Parking:
					if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0)
					{
						if ((connectionLane.m_RoadTypes & m_SpawnLocationData.m_RoadTypes) != RoadTypes.None)
						{
							return true;
						}
					}
					else if ((connectionLane.m_Flags & ConnectionLaneFlags.Pedestrian) != 0 && connectionLane.m_RoadTypes == RoadTypes.Bicycle && m_SpawnLocationData.m_RoadTypes == RoadTypes.Bicycle)
					{
						return true;
					}
					return false;
				case RouteConnectionType.Road:
				case RouteConnectionType.Cargo:
				case RouteConnectionType.Offroad:
					if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0 && (connectionLane.m_RoadTypes & m_SpawnLocationData.m_RoadTypes) != RoadTypes.None)
					{
						return true;
					}
					return false;
				case RouteConnectionType.Track:
					if ((connectionLane.m_Flags & ConnectionLaneFlags.Track) != 0 && (connectionLane.m_TrackTypes & m_SpawnLocationData.m_TrackTypes) != TrackTypes.None)
					{
						return true;
					}
					return false;
				default:
					return false;
				}
			}

			private bool CheckLaneType(Entity lane)
			{
				PrefabRef prefabRef = m_PrefabRefData[lane];
				NetLaneData netLaneData = m_PrefabNetLaneData[prefabRef.m_Prefab];
				switch (m_SpawnLocationData.m_ConnectionType)
				{
				case RouteConnectionType.Pedestrian:
					if ((netLaneData.m_Flags & LaneFlags.Pedestrian) != 0)
					{
						return true;
					}
					return false;
				case RouteConnectionType.Parking:
					if ((netLaneData.m_Flags & LaneFlags.Road) != 0)
					{
						CarLaneData carLaneData = m_PrefabCarLaneData[prefabRef.m_Prefab];
						if (m_CarLaneData.TryGetComponent(lane, out var componentData) && (componentData.m_Flags & CarLaneFlags.Unsafe) != 0)
						{
							return false;
						}
						if ((carLaneData.m_RoadTypes & m_SpawnLocationData.m_RoadTypes) != RoadTypes.None)
						{
							return true;
						}
					}
					else if ((netLaneData.m_Flags & LaneFlags.Pedestrian) != 0 && m_SpawnLocationData.m_RoadTypes == RoadTypes.Bicycle)
					{
						return true;
					}
					return false;
				case RouteConnectionType.Road:
				case RouteConnectionType.Cargo:
				case RouteConnectionType.Offroad:
					if ((netLaneData.m_Flags & LaneFlags.Road) != 0)
					{
						CarLaneData carLaneData2 = m_PrefabCarLaneData[prefabRef.m_Prefab];
						if (m_CarLaneData.TryGetComponent(lane, out var componentData2) && (componentData2.m_Flags & CarLaneFlags.Unsafe) != 0)
						{
							return false;
						}
						if ((carLaneData2.m_RoadTypes & m_SpawnLocationData.m_RoadTypes) != RoadTypes.None)
						{
							return true;
						}
					}
					return false;
				case RouteConnectionType.Track:
					if ((netLaneData.m_Flags & LaneFlags.Track) != 0 && (m_PrefabTrackLaneData[prefabRef.m_Prefab].m_TrackTypes & m_SpawnLocationData.m_TrackTypes) != TrackTypes.None)
					{
						return true;
					}
					return false;
				default:
					return false;
				}
			}

			public bool TryFindLanes(Entity entity, out float distance, out SpawnLocation spawnLocation)
			{
				distance = float.MaxValue;
				spawnLocation = default(SpawnLocation);
				if (!m_Lanes.HasBuffer(entity))
				{
					return false;
				}
				DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_Lanes[entity];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity subLane = dynamicBuffer[i].m_SubLane;
					if (CheckLaneType(subLane))
					{
						float t;
						float num = MathUtils.Distance(m_CurveData[subLane].m_Bezier, m_Position, out t);
						if (num < distance)
						{
							distance = num;
							spawnLocation.m_ConnectedLane1 = subLane;
							spawnLocation.m_CurvePosition1 = t;
						}
					}
				}
				if (m_SlaveLaneData.HasComponent(spawnLocation.m_ConnectedLane1))
				{
					SlaveLane slaveLane = m_SlaveLaneData[spawnLocation.m_ConnectedLane1];
					if (slaveLane.m_MasterIndex < dynamicBuffer.Length)
					{
						spawnLocation.m_ConnectedLane1 = dynamicBuffer[slaveLane.m_MasterIndex].m_SubLane;
					}
				}
				if ((m_SpawnLocationData.m_ConnectionType == RouteConnectionType.Road || m_SpawnLocationData.m_ConnectionType == RouteConnectionType.Cargo || m_SpawnLocationData.m_ConnectionType == RouteConnectionType.Parking || m_SpawnLocationData.m_ConnectionType == RouteConnectionType.Offroad) && m_CarLaneData.TryGetComponent(spawnLocation.m_ConnectedLane1, out var componentData))
				{
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Entity subLane2 = dynamicBuffer[j].m_SubLane;
						if (!(subLane2 == spawnLocation.m_ConnectedLane1) && m_CarLaneData.HasComponent(subLane2) && !m_SlaveLaneData.HasComponent(subLane2))
						{
							Game.Net.CarLane carLane = m_CarLaneData[subLane2];
							if (componentData.m_CarriagewayGroup == carLane.m_CarriagewayGroup)
							{
								spawnLocation.m_ConnectedLane2 = subLane2;
								spawnLocation.m_CurvePosition2 = math.select(spawnLocation.m_CurvePosition1, 1f - spawnLocation.m_CurvePosition1, ((componentData.m_Flags ^ carLane.m_Flags) & CarLaneFlags.Invert) != 0);
								break;
							}
						}
					}
				}
				return spawnLocation.m_ConnectedLane1 != Entity.Null;
			}
		}

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public ComponentLookup<Attached> m_AttachedData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<MovedLocation> m_MovedLocationData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<SlaveLane> m_SlaveLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> m_LotData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_PrefabNetLaneData;

		[ReadOnly]
		public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> m_PrefabTrackLaneData;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> m_PrefabSpawnLocationData;

		[ReadOnly]
		public ComponentLookup<RouteConnectionData> m_PrefabRouteConnectionData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public BufferLookup<SpawnLocationElement> m_SpawnLocations;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_Lanes;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_AreaTriangles;

		[ReadOnly]
		public NativeArray<Entity> m_Entities;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

		[ReadOnly]
		public AirwayHelpers.AirwayData m_AirwayData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(int index)
		{
			Entity entity = m_Entities[index];
			if (!m_SpawnLocationData.HasComponent(entity))
			{
				return;
			}
			if (m_UpdatedData.HasComponent(entity) && m_MovedLocationData.HasComponent(entity))
			{
				m_CommandBuffer.RemoveComponent<MovedLocation>(index, entity);
			}
			Transform transform = m_TransformData[entity];
			PrefabRef prefabRef = m_PrefabRefData[entity];
			if (m_PrefabSpawnLocationData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
			{
				if (componentData.m_ConnectionType == RouteConnectionType.Air)
				{
					SpawnLocation spawnLocation = default(SpawnLocation);
					float distance = float.MaxValue;
					if ((componentData.m_RoadTypes & RoadTypes.Helicopter) != RoadTypes.None)
					{
						m_AirwayData.helicopterMap.FindClosestLane(transform.m_Position, m_CurveData, ref spawnLocation.m_ConnectedLane1, ref spawnLocation.m_CurvePosition1, ref distance);
					}
					if ((componentData.m_RoadTypes & RoadTypes.Airplane) != RoadTypes.None)
					{
						m_AirwayData.airplaneMap.FindClosestLane(transform.m_Position, m_CurveData, ref spawnLocation.m_ConnectedLane1, ref spawnLocation.m_CurvePosition1, ref distance);
					}
					SetSpawnLocation(index, entity, spawnLocation);
					return;
				}
				if (m_PrefabRouteConnectionData.HasComponent(prefabRef.m_Prefab))
				{
					RouteConnectionData routeConnectionData = m_PrefabRouteConnectionData[prefabRef.m_Prefab];
					if (componentData.m_ConnectionType != RouteConnectionType.None && componentData.m_ConnectionType == routeConnectionData.m_AccessConnectionType && componentData.m_ActivityMask.m_Mask == 0)
					{
						SetSpawnLocation(index, entity, default(SpawnLocation));
						return;
					}
				}
				Iterator iterator = new Iterator
				{
					m_Bounds = new Bounds3(transform.m_Position - 32f, transform.m_Position + 32f),
					m_Position = transform.m_Position,
					m_MaxDistance = 32f,
					m_SpawnLocationData = componentData,
					m_CurveData = m_CurveData,
					m_CarLaneData = m_CarLaneData,
					m_SlaveLaneData = m_SlaveLaneData,
					m_ConnectionLaneData = m_ConnectionLaneData,
					m_LotData = m_LotData,
					m_PrefabRefData = m_PrefabRefData,
					m_PrefabNetLaneData = m_PrefabNetLaneData,
					m_PrefabCarLaneData = m_PrefabCarLaneData,
					m_PrefabTrackLaneData = m_PrefabTrackLaneData,
					m_Lanes = m_Lanes,
					m_AreaNodes = m_AreaNodes,
					m_AreaTriangles = m_AreaTriangles
				};
				if (m_AttachedData.HasComponent(entity) && iterator.TryFindLanes(m_AttachedData[entity].m_Parent, out var distance2, out var spawnLocation2))
				{
					SetSpawnLocation(index, entity, spawnLocation2);
					return;
				}
				if (m_OwnerData.HasComponent(entity) && iterator.TryFindLanes(m_OwnerData[entity].m_Owner, out distance2, out var spawnLocation3))
				{
					SetSpawnLocation(index, entity, spawnLocation3);
					return;
				}
				m_NetSearchTree.Iterate(ref iterator);
				m_AreaSearchTree.Iterate(ref iterator);
				if (iterator.m_BestLocation.m_ConnectedLane1 != Entity.Null)
				{
					SetSpawnLocation(index, entity, iterator.m_BestLocation);
					return;
				}
				if (m_OwnerData.HasComponent(entity))
				{
					Owner owner = m_OwnerData[entity];
					if (m_BuildingData.HasComponent(owner.m_Owner) && ShouldConnectToBuildingRoad(componentData, transform.m_Position, entity, owner.m_Owner) && iterator.TryFindLanes(m_BuildingData[owner.m_Owner].m_RoadEdge, out distance2, out var spawnLocation4))
					{
						SetSpawnLocation(index, entity, spawnLocation4);
						return;
					}
				}
			}
			SetSpawnLocation(index, entity, default(SpawnLocation));
		}

		private bool ShouldConnectToBuildingRoad(SpawnLocationData spawnLocationData, float3 position, Entity entity, Entity owner)
		{
			switch (spawnLocationData.m_ConnectionType)
			{
			case RouteConnectionType.Road:
			case RouteConnectionType.Parking:
				if (spawnLocationData.m_RoadTypes != RoadTypes.Bicycle && (spawnLocationData.m_RoadTypes & RoadTypes.Car) == 0)
				{
					return false;
				}
				break;
			case RouteConnectionType.None:
			case RouteConnectionType.Track:
			case RouteConnectionType.Air:
				return false;
			}
			if (spawnLocationData.m_ActivityMask.m_Mask != 0)
			{
				return false;
			}
			if (m_SpawnLocations.HasBuffer(owner))
			{
				DynamicBuffer<SpawnLocationElement> dynamicBuffer = m_SpawnLocations[owner];
				PrefabRef prefabRef = m_PrefabRefData[owner];
				BuildingData buildingData = m_PrefabBuildingData[prefabRef.m_Prefab];
				float3 y = BuildingUtils.CalculateFrontPosition(m_TransformData[owner], buildingData.m_LotSize.y);
				float num = math.distance(position, y);
				bool flag = spawnLocationData.m_ConnectionType == RouteConnectionType.Pedestrian || (spawnLocationData.m_ConnectionType == RouteConnectionType.Parking && spawnLocationData.m_RoadTypes == RoadTypes.Bicycle);
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					if (dynamicBuffer[i].m_Type != SpawnLocationType.SpawnLocation)
					{
						continue;
					}
					Entity spawnLocation = dynamicBuffer[i].m_SpawnLocation;
					if (entity == spawnLocation)
					{
						continue;
					}
					PrefabRef prefabRef2 = m_PrefabRefData[spawnLocation];
					SpawnLocationData spawnLocationData2 = m_PrefabSpawnLocationData[prefabRef2.m_Prefab];
					bool flag2 = spawnLocationData2.m_ConnectionType == RouteConnectionType.Pedestrian || (spawnLocationData2.m_ConnectionType == RouteConnectionType.Parking && spawnLocationData2.m_RoadTypes == RoadTypes.Bicycle);
					if ((flag != flag2 && (spawnLocationData2.m_ConnectionType != spawnLocationData.m_ConnectionType || ((spawnLocationData.m_ConnectionType == RouteConnectionType.Road || spawnLocationData.m_ConnectionType == RouteConnectionType.Parking) && (spawnLocationData2.m_RoadTypes & spawnLocationData.m_RoadTypes) == 0))) || spawnLocationData2.m_ActivityMask.m_Mask != 0)
					{
						continue;
					}
					if (m_PrefabRouteConnectionData.HasComponent(prefabRef2.m_Prefab))
					{
						RouteConnectionData routeConnectionData = m_PrefabRouteConnectionData[prefabRef2.m_Prefab];
						if (spawnLocationData.m_ConnectionType == routeConnectionData.m_AccessConnectionType)
						{
							continue;
						}
					}
					if (m_TransformData.HasComponent(spawnLocation) && math.distance(m_TransformData[spawnLocation].m_Position, y) < num)
					{
						return false;
					}
				}
			}
			return true;
		}

		private void SetSpawnLocation(int jobIndex, Entity entity, SpawnLocation spawnLocation)
		{
			SpawnLocation spawnLocation2 = m_SpawnLocationData[entity];
			if (spawnLocation2.m_ConnectedLane1 != spawnLocation.m_ConnectedLane1 || spawnLocation2.m_ConnectedLane2 != spawnLocation.m_ConnectedLane2 || spawnLocation2.m_CurvePosition1 != spawnLocation.m_CurvePosition1 || spawnLocation2.m_CurvePosition2 != spawnLocation.m_CurvePosition2 || m_UpdatedData.HasComponent(spawnLocation.m_ConnectedLane1) || m_UpdatedData.HasComponent(spawnLocation.m_ConnectedLane2))
			{
				spawnLocation.m_AccessRestriction = spawnLocation2.m_AccessRestriction;
				spawnLocation.m_GroupIndex = spawnLocation2.m_GroupIndex;
				m_SpawnLocationData[entity] = spawnLocation;
				if (!m_UpdatedData.HasComponent(entity))
				{
					m_CommandBuffer.AddComponent(jobIndex, entity, default(PathfindUpdated));
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<RoadConnectionUpdated> __Game_Buildings_RoadConnectionUpdated_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferLookup<SpawnLocationElement> __Game_Buildings_SpawnLocationElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MovedLocation> __Game_Objects_MovedLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> __Game_Areas_Lot_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarLaneData> __Game_Prefabs_CarLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> __Game_Prefabs_TrackLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteConnectionData> __Game_Prefabs_RouteConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		public ComponentLookup<SpawnLocation> __Game_Objects_SpawnLocation_RW_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<SpawnLocation>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_RoadConnectionUpdated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<RoadConnectionUpdated>(isReadOnly: true);
			__Game_Buildings_SpawnLocationElement_RO_BufferLookup = state.GetBufferLookup<SpawnLocationElement>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Objects_MovedLocation_RO_ComponentLookup = state.GetComponentLookup<MovedLocation>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Areas_Lot_RO_ComponentLookup = state.GetComponentLookup<Game.Areas.Lot>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
			__Game_Prefabs_TrackLaneData_RO_ComponentLookup = state.GetComponentLookup<TrackLaneData>(isReadOnly: true);
			__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
			__Game_Prefabs_RouteConnectionData_RO_ComponentLookup = state.GetComponentLookup<RouteConnectionData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RW_ComponentLookup = state.GetComponentLookup<SpawnLocation>();
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
		}
	}

	private Game.Net.SearchSystem m_NetSearchSystem;

	private Game.Net.UpdateCollectSystem m_NetUpdateCollectSystem;

	private AirwaySystem m_AirwaySystem;

	private Game.Areas.SearchSystem m_AreaSearchSystem;

	private Game.Areas.UpdateCollectSystem m_AreaUpdateCollectSystem;

	private SearchSystem m_ObjectSearchSystem;

	private ModificationBarrier5 m_ModificationBarrier;

	private EntityQuery m_UpdatedQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_NetUpdateCollectSystem = base.World.GetOrCreateSystemManaged<Game.Net.UpdateCollectSystem>();
		m_AirwaySystem = base.World.GetOrCreateSystemManaged<AirwaySystem>();
		m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
		m_AreaUpdateCollectSystem = base.World.GetOrCreateSystemManaged<Game.Areas.UpdateCollectSystem>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_UpdatedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<SpawnLocation>(),
				ComponentType.ReadOnly<Updated>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<RoadConnectionUpdated>(),
				ComponentType.ReadOnly<Event>()
			}
		});
	}

	[Preserve]
	protected override void OnUpdate()
	{
		bool flag = !m_UpdatedQuery.IsEmptyIgnoreFilter;
		if (flag || m_NetUpdateCollectSystem.netsUpdated || m_AreaUpdateCollectSystem.lotsUpdated || m_AreaUpdateCollectSystem.spacesUpdated)
		{
			NativeQueue<Entity> updatedQueue = new NativeQueue<Entity>(Allocator.TempJob);
			NativeQueue<Entity> updatedQueue2 = new NativeQueue<Entity>(Allocator.TempJob);
			NativeQueue<Entity> updatedQueue3 = new NativeQueue<Entity>(Allocator.TempJob);
			NativeQueue<Entity> updatedQueue4 = new NativeQueue<Entity>(Allocator.TempJob);
			NativeList<Entity> nativeList = new NativeList<Entity>(Allocator.TempJob);
			JobHandle jobHandle = default(JobHandle);
			if (m_NetUpdateCollectSystem.netsUpdated)
			{
				JobHandle dependencies;
				NativeList<Bounds2> updatedNetBounds = m_NetUpdateCollectSystem.GetUpdatedNetBounds(out dependencies);
				JobHandle dependencies2;
				JobHandle jobHandle2 = new FindUpdatedSpawnLocationsJob
				{
					m_Bounds = updatedNetBounds.AsDeferredJobArray(),
					m_ObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies2),
					m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
					m_ResultQueue = updatedQueue.AsParallelWriter()
				}.Schedule(updatedNetBounds, 1, JobHandle.CombineDependencies(base.Dependency, dependencies, dependencies2));
				m_NetUpdateCollectSystem.AddNetBoundsReader(jobHandle2);
				m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle2);
				jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
			}
			if (m_AreaUpdateCollectSystem.lotsUpdated)
			{
				JobHandle dependencies3;
				NativeList<Bounds2> updatedLotBounds = m_AreaUpdateCollectSystem.GetUpdatedLotBounds(out dependencies3);
				JobHandle dependencies4;
				JobHandle jobHandle3 = new FindUpdatedSpawnLocationsJob
				{
					m_Bounds = updatedLotBounds.AsDeferredJobArray(),
					m_ObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies4),
					m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
					m_ResultQueue = updatedQueue2.AsParallelWriter()
				}.Schedule(updatedLotBounds, 1, JobHandle.CombineDependencies(base.Dependency, dependencies3, dependencies4));
				m_AreaUpdateCollectSystem.AddLotBoundsReader(jobHandle3);
				m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle3);
				jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle3);
			}
			if (m_AreaUpdateCollectSystem.spacesUpdated)
			{
				JobHandle dependencies5;
				NativeList<Bounds2> updatedSpaceBounds = m_AreaUpdateCollectSystem.GetUpdatedSpaceBounds(out dependencies5);
				JobHandle dependencies6;
				JobHandle jobHandle4 = new FindUpdatedSpawnLocationsJob
				{
					m_Bounds = updatedSpaceBounds.AsDeferredJobArray(),
					m_ObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies6),
					m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
					m_ResultQueue = updatedQueue3.AsParallelWriter()
				}.Schedule(updatedSpaceBounds, 1, JobHandle.CombineDependencies(base.Dependency, dependencies5, dependencies6));
				m_AreaUpdateCollectSystem.AddSpaceBoundsReader(jobHandle4);
				m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle4);
				jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle4);
			}
			if (flag)
			{
				JobHandle job = JobChunkExtensions.ScheduleParallel(new CheckUpdatedSpawnLocationsJob
				{
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_RoadConnectionUpdatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_RoadConnectionUpdated_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_SpawnLocations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_SpawnLocationElement_RO_BufferLookup, ref base.CheckedStateRef),
					m_ResultQueue = updatedQueue4.AsParallelWriter()
				}, m_UpdatedQuery, base.Dependency);
				jobHandle = JobHandle.CombineDependencies(jobHandle, job);
			}
			ListUpdatedSpawnLocationsJob jobData = new ListUpdatedSpawnLocationsJob
			{
				m_UpdatedQueue1 = updatedQueue,
				m_UpdatedQueue2 = updatedQueue2,
				m_UpdatedQueue3 = updatedQueue3,
				m_UpdatedQueue4 = updatedQueue4,
				m_UpdatedList = nativeList
			};
			JobHandle dependencies7;
			JobHandle dependencies8;
			FindSpawnLocationConnectionJob jobData2 = new FindSpawnLocationConnectionJob
			{
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AttachedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_MovedLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_MovedLocation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SlaveLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Lot_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabTrackLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrackLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabSpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRouteConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RouteConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RW_ComponentLookup, ref base.CheckedStateRef),
				m_SpawnLocations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_SpawnLocationElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_Lanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
				m_AreaTriangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
				m_Entities = nativeList.AsDeferredJobArray(),
				m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies7),
				m_AreaSearchTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies8),
				m_AirwayData = m_AirwaySystem.GetAirwayData(),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
			};
			JobHandle jobHandle5 = IJobExtensions.Schedule(jobData, jobHandle);
			JobHandle jobHandle6 = jobData2.Schedule(nativeList, 1, JobHandle.CombineDependencies(jobHandle5, dependencies7, dependencies8));
			updatedQueue.Dispose(jobHandle5);
			updatedQueue2.Dispose(jobHandle5);
			updatedQueue3.Dispose(jobHandle5);
			updatedQueue4.Dispose(jobHandle5);
			nativeList.Dispose(jobHandle6);
			m_NetSearchSystem.AddNetSearchTreeReader(jobHandle6);
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
	public SpawnLocationConnectionSystem()
	{
	}
}
