using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Common;
using Game.Creatures;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Net;

[CompilerGenerated]
public class FixLaneObjectsSystem : GameSystemBase
{
	[BurstCompile]
	private struct CollectLaneObjectsJob : IJobChunk
	{
		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<ParkedTrain> m_ParkedTrainData;

		[ReadOnly]
		public ComponentLookup<CarCurrentLane> m_CarCurrentLaneData;

		[ReadOnly]
		public ComponentLookup<CarTrailerLane> m_CarTrailerLaneData;

		[ReadOnly]
		public ComponentLookup<HumanCurrentLane> m_HumanCurrentLane;

		[ReadOnly]
		public ComponentLookup<AnimalCurrentLane> m_AnimalCurrentLane;

		[ReadOnly]
		public ComponentLookup<TrainCurrentLane> m_TrainCurrentLane;

		[ReadOnly]
		public ComponentLookup<WatercraftCurrentLane> m_WatercraftCurrentLane;

		[ReadOnly]
		public ComponentLookup<AircraftCurrentLane> m_AircraftCurrentLane;

		public BufferTypeHandle<LaneObject> m_LaneObjectType;

		public NativeQueue<Entity>.ParallelWriter m_LaneObjectQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<LaneObject> bufferAccessor = chunk.GetBufferAccessor(ref m_LaneObjectType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<LaneObject> dynamicBuffer = bufferAccessor[i];
				int num = 0;
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					LaneObject value = dynamicBuffer[j];
					if (m_ParkedCarData.HasComponent(value.m_LaneObject) || m_ParkedTrainData.HasComponent(value.m_LaneObject))
					{
						dynamicBuffer[num++] = value;
					}
					else if (m_CarCurrentLaneData.HasComponent(value.m_LaneObject) || m_CarTrailerLaneData.HasComponent(value.m_LaneObject) || m_HumanCurrentLane.HasComponent(value.m_LaneObject) || m_AnimalCurrentLane.HasComponent(value.m_LaneObject) || m_TrainCurrentLane.HasComponent(value.m_LaneObject) || m_WatercraftCurrentLane.HasComponent(value.m_LaneObject) || m_AircraftCurrentLane.HasComponent(value.m_LaneObject))
					{
						dynamicBuffer[num++] = value;
						m_LaneObjectQueue.Enqueue(value.m_LaneObject);
					}
				}
				if (num != 0)
				{
					if (num < dynamicBuffer.Length)
					{
						dynamicBuffer.RemoveRange(num, dynamicBuffer.Length - num);
					}
				}
				else
				{
					dynamicBuffer.Clear();
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FixLaneObjectsJob : IJob
	{
		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Moving> m_MovingData;

		[ReadOnly]
		public ComponentLookup<CarNavigation> m_CarNavigationData;

		[ReadOnly]
		public ComponentLookup<HumanNavigation> m_HumanNavigationData;

		[ReadOnly]
		public ComponentLookup<AnimalNavigation> m_AnimalNavigationData;

		[ReadOnly]
		public ComponentLookup<Train> m_TrainData;

		[ReadOnly]
		public ComponentLookup<WatercraftNavigation> m_WatercraftNavigationData;

		[ReadOnly]
		public ComponentLookup<AircraftNavigation> m_AircraftNavigationData;

		[ReadOnly]
		public ComponentLookup<OutOfControl> m_OutOfControlData;

		[ReadOnly]
		public ComponentLookup<Helicopter> m_HelicopterData;

		[ReadOnly]
		public ComponentLookup<Controller> m_ControllerData;

		[ReadOnly]
		public ComponentLookup<Bicycle> m_BicycleData;

		[ReadOnly]
		public ComponentLookup<Lane> m_LaneData;

		[ReadOnly]
		public ComponentLookup<MasterLane> m_MasterLaneData;

		[ReadOnly]
		public ComponentLookup<CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<TrackLane> m_TrackLaneData;

		[ReadOnly]
		public ComponentLookup<PedestrianLane> m_PedestrianLaneData;

		[ReadOnly]
		public ComponentLookup<ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<HangaroundLocation> m_HangaroundLocationData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_PrefabLaneData;

		[ReadOnly]
		public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> m_PrefabTrackLaneData;

		[ReadOnly]
		public ComponentLookup<TrainData> m_PrefabTrainData;

		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjects;

		[ReadOnly]
		public BufferLookup<SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_AreaTriangles;

		public ComponentLookup<CarCurrentLane> m_CarCurrentLaneData;

		public ComponentLookup<CarTrailerLane> m_CarTrailerLaneData;

		public ComponentLookup<HumanCurrentLane> m_HumanCurrentLane;

		public ComponentLookup<AnimalCurrentLane> m_AnimalCurrentLane;

		public ComponentLookup<TrainCurrentLane> m_TrainCurrentLane;

		public ComponentLookup<WatercraftCurrentLane> m_WatercraftCurrentLane;

		public ComponentLookup<AircraftCurrentLane> m_AircraftCurrentLane;

		public BufferLookup<BlockedLane> m_BlockedLanes;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_StaticObjectSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_MovingObjectSearchTree;

		public NativeQueue<Entity> m_LaneObjectQueue;

		public LaneObjectCommandBuffer m_LaneObjectBuffer;

		public void Execute()
		{
			if (m_LaneObjectQueue.Count == 0)
			{
				return;
			}
			NativeParallelHashSet<Entity> nativeParallelHashSet = new NativeParallelHashSet<Entity>(m_LaneObjectQueue.Count, Allocator.Temp);
			NativeList<BlockedLane> tempBlockedLanes = new NativeList<BlockedLane>(16, Allocator.Temp);
			Entity item;
			while (m_LaneObjectQueue.TryDequeue(out item))
			{
				if (nativeParallelHashSet.Add(item))
				{
					if (m_CarCurrentLaneData.HasComponent(item))
					{
						UpdateCar(item, tempBlockedLanes);
					}
					else if (m_CarTrailerLaneData.HasComponent(item))
					{
						UpdateCarTrailer(item, tempBlockedLanes);
					}
					else if (m_HumanCurrentLane.HasComponent(item))
					{
						UpdateHuman(item);
					}
					else if (m_AnimalCurrentLane.HasComponent(item))
					{
						UpdateAnimal(item);
					}
					else if (m_TrainCurrentLane.HasComponent(item))
					{
						UpdateTrain(item);
					}
					else if (m_WatercraftCurrentLane.HasComponent(item))
					{
						UpdateWatercraft(item);
					}
					else if (m_AircraftCurrentLane.HasComponent(item))
					{
						UpdateAircraft(item);
					}
				}
			}
			tempBlockedLanes.Dispose();
			nativeParallelHashSet.Dispose();
		}

		private void UpdateAircraft(Entity entity)
		{
			Transform transform = m_TransformData[entity];
			AircraftNavigation navigation = m_AircraftNavigationData[entity];
			AircraftCurrentLane currentLane = m_AircraftCurrentLane[entity];
			PrefabRef prefabRef = m_PrefabRefData[entity];
			ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
			Moving moving = default(Moving);
			if (m_MovingData.HasComponent(entity))
			{
				moving = m_MovingData[entity];
			}
			AircraftNavigationHelpers.CurrentLaneCache currentLaneCache = new AircraftNavigationHelpers.CurrentLaneCache(ref currentLane, m_PrefabRefData, m_MovingObjectSearchTree);
			float num = 4f / 15f;
			bool flag = m_HelicopterData.HasComponent(entity);
			AircraftCurrentLane aircraftCurrentLane = currentLane;
			currentLane.m_Lane = Entity.Null;
			float3 @float = transform.m_Position + moving.m_Velocity * (num * 2f);
			float num2 = 100f;
			Bounds3 bounds = new Bounds3(@float - num2, @float + num2);
			AircraftNavigationHelpers.FindLaneIterator iterator = new AircraftNavigationHelpers.FindLaneIterator
			{
				m_Bounds = bounds,
				m_Position = @float,
				m_MinDistance = num2,
				m_Result = currentLane,
				m_CarType = (flag ? RoadTypes.Helicopter : RoadTypes.Airplane),
				m_SubLanes = m_SubLanes,
				m_CarLaneData = m_CarLaneData,
				m_ConnectionLaneData = m_ConnectionLaneData,
				m_CurveData = m_CurveData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabCarLaneData = m_PrefabCarLaneData
			};
			m_NetSearchTree.Iterate(ref iterator);
			currentLane = iterator.m_Result;
			if (aircraftCurrentLane.m_Lane == currentLane.m_Lane)
			{
				currentLane.m_CurvePosition.yz = aircraftCurrentLane.m_CurvePosition.yz;
				currentLane.m_LaneFlags = aircraftCurrentLane.m_LaneFlags;
			}
			else
			{
				currentLane.m_LaneFlags |= AircraftLaneFlags.Obsolete;
			}
			currentLaneCache.CheckChanges(entity, ref currentLane, m_LaneObjectBuffer, m_LaneObjects, transform, moving, navigation, objectGeometryData);
			m_AircraftCurrentLane[entity] = currentLane;
		}

		private void UpdateWatercraft(Entity entity)
		{
			Transform transform = m_TransformData[entity];
			WatercraftNavigation navigation = m_WatercraftNavigationData[entity];
			WatercraftCurrentLane currentLane = m_WatercraftCurrentLane[entity];
			PrefabRef prefabRef = m_PrefabRefData[entity];
			ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
			Moving moving = default(Moving);
			if (m_MovingData.HasComponent(entity))
			{
				moving = m_MovingData[entity];
			}
			WatercraftNavigationHelpers.CurrentLaneCache currentLaneCache = new WatercraftNavigationHelpers.CurrentLaneCache(ref currentLane, m_EntityLookup, m_MovingObjectSearchTree);
			float num = 4f / 15f;
			WatercraftCurrentLane watercraftCurrentLane = currentLane;
			currentLane.m_Lane = Entity.Null;
			currentLane.m_ChangeLane = Entity.Null;
			float3 @float = transform.m_Position + moving.m_Velocity * (num * 2f);
			float num2 = 100f;
			Bounds3 bounds = new Bounds3(@float - num2, @float + num2);
			WatercraftNavigationHelpers.FindLaneIterator iterator = new WatercraftNavigationHelpers.FindLaneIterator
			{
				m_Bounds = bounds,
				m_Position = @float,
				m_MinDistance = num2,
				m_Result = currentLane,
				m_CarType = RoadTypes.Watercraft,
				m_SubLanes = m_SubLanes,
				m_AreaNodes = m_AreaNodes,
				m_AreaTriangles = m_AreaTriangles,
				m_CarLaneData = m_CarLaneData,
				m_ConnectionLaneData = m_ConnectionLaneData,
				m_MasterLaneData = m_MasterLaneData,
				m_CurveData = m_CurveData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabCarLaneData = m_PrefabCarLaneData
			};
			m_NetSearchTree.Iterate(ref iterator);
			m_AreaSearchTree.Iterate(ref iterator);
			currentLane = iterator.m_Result;
			if (watercraftCurrentLane.m_Lane == currentLane.m_Lane)
			{
				if (m_PrefabRefData.HasComponent(watercraftCurrentLane.m_ChangeLane) && !m_DeletedData.HasComponent(watercraftCurrentLane.m_ChangeLane))
				{
					currentLane.m_ChangeLane = watercraftCurrentLane.m_ChangeLane;
				}
				currentLane.m_CurvePosition.yz = watercraftCurrentLane.m_CurvePosition.yz;
				currentLane.m_LaneFlags = watercraftCurrentLane.m_LaneFlags;
			}
			else
			{
				currentLane.m_LaneFlags |= WatercraftLaneFlags.Obsolete;
			}
			currentLaneCache.CheckChanges(entity, ref currentLane, m_LaneObjectBuffer, m_LaneObjects, transform, moving, navigation, objectGeometryData);
			m_WatercraftCurrentLane[entity] = currentLane;
		}

		private void UpdateTrain(Entity entity)
		{
			Transform transform = m_TransformData[entity];
			Train train = m_TrainData[entity];
			TrainCurrentLane currentLane = m_TrainCurrentLane[entity];
			PrefabRef prefabRef = m_PrefabRefData[entity];
			TrainData prefabTrainData = m_PrefabTrainData[prefabRef.m_Prefab];
			Moving moving = default(Moving);
			if (m_MovingData.HasComponent(entity))
			{
				moving = m_MovingData[entity];
			}
			TrainNavigationHelpers.CurrentLaneCache currentLaneCache = new TrainNavigationHelpers.CurrentLaneCache(ref currentLane, m_LaneData);
			float num = 4f / 15f;
			transform.m_Position += moving.m_Velocity * (num * 2f);
			TrainCurrentLane trainCurrentLane = currentLane;
			currentLane.m_Front.m_Lane = Entity.Null;
			currentLane.m_Rear.m_Lane = Entity.Null;
			currentLane.m_FrontCache.m_Lane = Entity.Null;
			currentLane.m_RearCache.m_Lane = Entity.Null;
			VehicleUtils.CalculateTrainNavigationPivots(transform, prefabTrainData, out var pivot, out var pivot2);
			if ((train.m_Flags & Game.Vehicles.TrainFlags.Reversed) != 0)
			{
				CommonUtils.Swap(ref pivot, ref pivot2);
			}
			float num2 = 100f;
			Bounds3 bounds = MathUtils.Expand(MathUtils.Bounds(pivot, pivot2), num2);
			TrainNavigationHelpers.FindLaneIterator iterator = new TrainNavigationHelpers.FindLaneIterator
			{
				m_Bounds = bounds,
				m_FrontPivot = pivot,
				m_RearPivot = pivot2,
				m_MinDistance = num2,
				m_Result = currentLane,
				m_TrackType = prefabTrainData.m_TrackType,
				m_SubLanes = m_SubLanes,
				m_TrackLaneData = m_TrackLaneData,
				m_CurveData = m_CurveData,
				m_ConnectionLaneData = m_ConnectionLaneData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabTrackLaneData = m_PrefabTrackLaneData
			};
			m_NetSearchTree.Iterate(ref iterator);
			currentLane = iterator.m_Result;
			if (trainCurrentLane.m_Front.m_Lane == currentLane.m_Front.m_Lane)
			{
				if (m_PrefabRefData.HasComponent(trainCurrentLane.m_FrontCache.m_Lane) && !m_DeletedData.HasComponent(trainCurrentLane.m_FrontCache.m_Lane))
				{
					currentLane.m_FrontCache.m_Lane = trainCurrentLane.m_FrontCache.m_Lane;
					currentLane.m_FrontCache.m_CurvePosition = trainCurrentLane.m_FrontCache.m_CurvePosition;
					currentLane.m_FrontCache.m_LaneFlags = trainCurrentLane.m_FrontCache.m_LaneFlags;
				}
				currentLane.m_Front.m_CurvePosition.xzw = trainCurrentLane.m_Front.m_CurvePosition.xzw;
				currentLane.m_Front.m_LaneFlags = trainCurrentLane.m_Front.m_LaneFlags;
			}
			else
			{
				currentLane.m_Front.m_LaneFlags |= TrainLaneFlags.Obsolete;
			}
			if (trainCurrentLane.m_Rear.m_Lane == currentLane.m_Rear.m_Lane)
			{
				if (m_PrefabRefData.HasComponent(trainCurrentLane.m_RearCache.m_Lane) && !m_DeletedData.HasComponent(trainCurrentLane.m_RearCache.m_Lane))
				{
					currentLane.m_RearCache.m_Lane = trainCurrentLane.m_RearCache.m_Lane;
					currentLane.m_RearCache.m_CurvePosition = trainCurrentLane.m_RearCache.m_CurvePosition;
					currentLane.m_RearCache.m_LaneFlags = trainCurrentLane.m_RearCache.m_LaneFlags;
				}
				currentLane.m_Rear.m_CurvePosition.xzw = trainCurrentLane.m_Rear.m_CurvePosition.xzw;
				currentLane.m_Rear.m_LaneFlags = trainCurrentLane.m_Rear.m_LaneFlags;
			}
			else
			{
				currentLane.m_Rear.m_LaneFlags |= TrainLaneFlags.Obsolete;
			}
			currentLaneCache.CheckChanges(entity, currentLane, m_LaneObjectBuffer);
			m_TrainCurrentLane[entity] = currentLane;
		}

		private void UpdateHuman(Entity entity)
		{
			Transform transform = m_TransformData[entity];
			HumanNavigation navigation = m_HumanNavigationData[entity];
			HumanCurrentLane currentLane = m_HumanCurrentLane[entity];
			PrefabRef prefabRef = m_PrefabRefData[entity];
			ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
			Moving moving = default(Moving);
			if (m_MovingData.HasComponent(entity))
			{
				moving = m_MovingData[entity];
			}
			HumanNavigationHelpers.CurrentLaneCache currentLaneCache = new HumanNavigationHelpers.CurrentLaneCache(ref currentLane, m_EntityLookup, m_MovingObjectSearchTree);
			HumanCurrentLane humanCurrentLane = currentLane;
			float3 position = transform.m_Position;
			Bounds3 bounds = new Bounds3(position - 100f, position + 100f);
			HumanNavigationHelpers.FindLaneIterator iterator = new HumanNavigationHelpers.FindLaneIterator
			{
				m_Bounds = bounds,
				m_Position = position,
				m_MinDistance = 1000f,
				m_Result = currentLane,
				m_SubLanes = m_SubLanes,
				m_AreaNodes = m_AreaNodes,
				m_AreaTriangles = m_AreaTriangles,
				m_PedestrianLaneData = m_PedestrianLaneData,
				m_ConnectionLaneData = m_ConnectionLaneData,
				m_CurveData = m_CurveData,
				m_HangaroundLocationData = m_HangaroundLocationData
			};
			m_NetSearchTree.Iterate(ref iterator);
			m_StaticObjectSearchTree.Iterate(ref iterator);
			m_AreaSearchTree.Iterate(ref iterator);
			currentLane = iterator.m_Result;
			if (humanCurrentLane.m_Lane == currentLane.m_Lane)
			{
				currentLane.m_CurvePosition.y = humanCurrentLane.m_CurvePosition.y;
				currentLane.m_Flags = humanCurrentLane.m_Flags;
			}
			else
			{
				currentLane.m_Flags |= CreatureLaneFlags.Obsolete;
			}
			currentLaneCache.CheckChanges(entity, ref currentLane, m_LaneObjectBuffer, m_LaneObjects, transform, moving, navigation, objectGeometryData);
			m_HumanCurrentLane[entity] = currentLane;
		}

		private void UpdateAnimal(Entity entity)
		{
			Transform transform = m_TransformData[entity];
			AnimalNavigation navigation = m_AnimalNavigationData[entity];
			AnimalCurrentLane currentLane = m_AnimalCurrentLane[entity];
			PrefabRef prefabRef = m_PrefabRefData[entity];
			ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
			Moving moving = default(Moving);
			if (m_MovingData.HasComponent(entity))
			{
				moving = m_MovingData[entity];
			}
			AnimalNavigationHelpers.CurrentLaneCache currentLaneCache = new AnimalNavigationHelpers.CurrentLaneCache(ref currentLane, m_PrefabRefData, m_MovingObjectSearchTree);
			AnimalCurrentLane animalCurrentLane = currentLane;
			float3 position = transform.m_Position;
			Bounds3 bounds = new Bounds3(position - 100f, position + 100f);
			AnimalNavigationHelpers.FindLaneIterator iterator = new AnimalNavigationHelpers.FindLaneIterator
			{
				m_Bounds = bounds,
				m_Position = position,
				m_MinDistance = 1000f,
				m_Result = currentLane,
				m_SubLanes = m_SubLanes,
				m_AreaNodes = m_AreaNodes,
				m_AreaTriangles = m_AreaTriangles,
				m_PedestrianLaneData = m_PedestrianLaneData,
				m_ConnectionLaneData = m_ConnectionLaneData,
				m_CurveData = m_CurveData,
				m_HangaroundLocationData = m_HangaroundLocationData
			};
			m_NetSearchTree.Iterate(ref iterator);
			m_StaticObjectSearchTree.Iterate(ref iterator);
			m_AreaSearchTree.Iterate(ref iterator);
			currentLane = iterator.m_Result;
			if (animalCurrentLane.m_Lane == currentLane.m_Lane)
			{
				if (m_PrefabRefData.HasComponent(animalCurrentLane.m_NextLane) && !m_DeletedData.HasComponent(animalCurrentLane.m_NextLane))
				{
					currentLane.m_NextLane = animalCurrentLane.m_NextLane;
					currentLane.m_NextPosition = animalCurrentLane.m_NextPosition;
					currentLane.m_NextFlags = animalCurrentLane.m_NextFlags;
				}
				currentLane.m_CurvePosition.y = animalCurrentLane.m_CurvePosition.y;
				currentLane.m_Flags = animalCurrentLane.m_Flags;
			}
			else
			{
				currentLane.m_Flags |= CreatureLaneFlags.Obsolete;
			}
			currentLaneCache.CheckChanges(entity, ref currentLane, m_LaneObjectBuffer, m_LaneObjects, transform, moving, navigation, objectGeometryData);
			m_AnimalCurrentLane[entity] = currentLane;
		}

		private void UpdateCar(Entity entity, NativeList<BlockedLane> tempBlockedLanes)
		{
			Transform transform = m_TransformData[entity];
			CarNavigation navigation = m_CarNavigationData[entity];
			CarCurrentLane currentLane = m_CarCurrentLaneData[entity];
			PrefabRef prefabRef = m_PrefabRefData[entity];
			DynamicBuffer<BlockedLane> blockedLanes = m_BlockedLanes[entity];
			ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
			Moving moving = default(Moving);
			if (m_MovingData.HasComponent(entity))
			{
				moving = m_MovingData[entity];
			}
			CarNavigationHelpers.CurrentLaneCache currentLaneCache = new CarNavigationHelpers.CurrentLaneCache(ref currentLane, blockedLanes, m_EntityLookup, m_MovingObjectSearchTree);
			if (m_OutOfControlData.HasComponent(entity))
			{
				float3 position = transform.m_Position;
				float3 @float = math.forward(transform.m_Rotation);
				Line3.Segment line = new Line3.Segment(position - @float * math.max(0.1f, 0f - objectGeometryData.m_Bounds.min.z - objectGeometryData.m_Size.x * 0.5f), position + @float * math.max(0.1f, objectGeometryData.m_Bounds.max.z - objectGeometryData.m_Size.x * 0.5f));
				float num = objectGeometryData.m_Size.x * 0.5f;
				Bounds3 bounds = MathUtils.Expand(MathUtils.Bounds(line), num);
				CarNavigationHelpers.FindBlockedLanesIterator iterator = new CarNavigationHelpers.FindBlockedLanesIterator
				{
					m_Bounds = bounds,
					m_Line = line,
					m_Radius = num,
					m_BlockedLanes = tempBlockedLanes,
					m_SubLanes = m_SubLanes,
					m_MasterLaneData = m_MasterLaneData,
					m_CurveData = m_CurveData,
					m_PrefabRefData = m_PrefabRefData,
					m_PrefabLaneData = m_PrefabLaneData
				};
				m_NetSearchTree.Iterate(ref iterator);
			}
			else
			{
				float num2 = 4f / 15f;
				CarCurrentLane carCurrentLane = currentLane;
				currentLane.m_Lane = Entity.Null;
				currentLane.m_ChangeLane = Entity.Null;
				float3 float2 = transform.m_Position + moving.m_Velocity * (num2 * 2f);
				float num3 = 100f;
				Bounds3 bounds2 = new Bounds3(float2 - num3, float2 + num3);
				bool flag = m_BicycleData.HasComponent(entity);
				CarNavigationHelpers.FindLaneIterator iterator2 = new CarNavigationHelpers.FindLaneIterator
				{
					m_Bounds = bounds2,
					m_Position = float2,
					m_MinDistance = num3,
					m_Result = currentLane,
					m_CarType = ((!flag) ? RoadTypes.Car : RoadTypes.Bicycle),
					m_SubLanes = m_SubLanes,
					m_AreaNodes = m_AreaNodes,
					m_AreaTriangles = m_AreaTriangles,
					m_CarLaneData = m_CarLaneData,
					m_PedestrianLaneData = m_PedestrianLaneData,
					m_MasterLaneData = m_MasterLaneData,
					m_ConnectionLaneData = m_ConnectionLaneData,
					m_CurveData = m_CurveData,
					m_PrefabRefData = m_PrefabRefData,
					m_PrefabCarLaneData = m_PrefabCarLaneData
				};
				m_NetSearchTree.Iterate(ref iterator2);
				m_StaticObjectSearchTree.Iterate(ref iterator2);
				m_AreaSearchTree.Iterate(ref iterator2);
				currentLane = iterator2.m_Result;
				if (carCurrentLane.m_Lane == currentLane.m_Lane)
				{
					if (m_PrefabRefData.HasComponent(carCurrentLane.m_ChangeLane) && !m_DeletedData.HasComponent(carCurrentLane.m_ChangeLane))
					{
						currentLane.m_ChangeLane = carCurrentLane.m_ChangeLane;
					}
					currentLane.m_CurvePosition.yz = carCurrentLane.m_CurvePosition.yz;
					currentLane.m_LaneFlags = carCurrentLane.m_LaneFlags;
				}
				else
				{
					currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.Obsolete;
				}
			}
			currentLaneCache.CheckChanges(entity, ref currentLane, tempBlockedLanes, m_LaneObjectBuffer, m_LaneObjects, transform, moving, navigation, objectGeometryData);
			m_CarCurrentLaneData[entity] = currentLane;
			tempBlockedLanes.Clear();
		}

		private void UpdateCarTrailer(Entity entity, NativeList<BlockedLane> tempBlockedLanes)
		{
			Transform transform = m_TransformData[entity];
			Controller controller = m_ControllerData[entity];
			CarTrailerLane trailerLane = m_CarTrailerLaneData[entity];
			PrefabRef prefabRef = m_PrefabRefData[entity];
			DynamicBuffer<BlockedLane> blockedLanes = m_BlockedLanes[entity];
			ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
			CarNavigation tractorNavigation = default(CarNavigation);
			if (m_CarNavigationData.HasComponent(controller.m_Controller))
			{
				tractorNavigation = m_CarNavigationData[controller.m_Controller];
			}
			Moving moving = default(Moving);
			if (m_MovingData.HasComponent(entity))
			{
				moving = m_MovingData[entity];
			}
			CarNavigationHelpers.TrailerLaneCache trailerLaneCache = new CarNavigationHelpers.TrailerLaneCache(ref trailerLane, blockedLanes, m_PrefabRefData, m_MovingObjectSearchTree);
			if (m_OutOfControlData.HasComponent(entity))
			{
				float3 position = transform.m_Position;
				float3 @float = math.forward(transform.m_Rotation);
				Line3.Segment line = new Line3.Segment(position - @float * math.max(0.1f, 0f - objectGeometryData.m_Bounds.min.z - objectGeometryData.m_Size.x * 0.5f), position + @float * math.max(0.1f, objectGeometryData.m_Bounds.max.z - objectGeometryData.m_Size.x * 0.5f));
				float num = objectGeometryData.m_Size.x * 0.5f;
				Bounds3 bounds = MathUtils.Expand(MathUtils.Bounds(line), num);
				CarNavigationHelpers.FindBlockedLanesIterator iterator = new CarNavigationHelpers.FindBlockedLanesIterator
				{
					m_Bounds = bounds,
					m_Line = line,
					m_Radius = num,
					m_BlockedLanes = tempBlockedLanes,
					m_SubLanes = m_SubLanes,
					m_MasterLaneData = m_MasterLaneData,
					m_CurveData = m_CurveData,
					m_PrefabRefData = m_PrefabRefData,
					m_PrefabLaneData = m_PrefabLaneData
				};
				m_NetSearchTree.Iterate(ref iterator);
			}
			else
			{
				float num2 = 4f / 15f;
				float3 float2 = transform.m_Position + moving.m_Velocity * (num2 * 2f);
				float num3 = 100f;
				Bounds3 bounds2 = new Bounds3(float2 - num3, float2 + num3);
				bool flag = m_BicycleData.HasComponent(entity);
				CarNavigationHelpers.FindLaneIterator iterator2 = new CarNavigationHelpers.FindLaneIterator
				{
					m_Bounds = bounds2,
					m_Position = float2,
					m_MinDistance = num3,
					m_CarType = ((!flag) ? RoadTypes.Car : RoadTypes.Bicycle),
					m_SubLanes = m_SubLanes,
					m_AreaNodes = m_AreaNodes,
					m_AreaTriangles = m_AreaTriangles,
					m_CarLaneData = m_CarLaneData,
					m_PedestrianLaneData = m_PedestrianLaneData,
					m_MasterLaneData = m_MasterLaneData,
					m_ConnectionLaneData = m_ConnectionLaneData,
					m_CurveData = m_CurveData,
					m_PrefabRefData = m_PrefabRefData,
					m_PrefabCarLaneData = m_PrefabCarLaneData
				};
				m_NetSearchTree.Iterate(ref iterator2);
				m_StaticObjectSearchTree.Iterate(ref iterator2);
				m_AreaSearchTree.Iterate(ref iterator2);
				if (iterator2.m_Result.m_Lane != trailerLane.m_Lane)
				{
					trailerLane.m_Lane = iterator2.m_Result.m_Lane;
					trailerLane.m_CurvePosition = iterator2.m_Result.m_CurvePosition.xy;
					trailerLane.m_NextLane = Entity.Null;
					trailerLane.m_NextPosition = default(float2);
				}
				else
				{
					trailerLane.m_CurvePosition.x = iterator2.m_Result.m_CurvePosition.x;
					if (!m_PrefabRefData.HasComponent(trailerLane.m_NextLane) || m_DeletedData.HasComponent(trailerLane.m_NextLane))
					{
						trailerLane.m_NextLane = Entity.Null;
						trailerLane.m_NextPosition = default(float2);
					}
				}
			}
			trailerLaneCache.CheckChanges(entity, ref trailerLane, tempBlockedLanes, m_LaneObjectBuffer, m_LaneObjects, transform, moving, tractorNavigation, objectGeometryData);
			m_CarTrailerLaneData[entity] = trailerLane;
			tempBlockedLanes.Clear();
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedTrain> __Game_Vehicles_ParkedTrain_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarTrailerLane> __Game_Vehicles_CarTrailerLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HumanCurrentLane> __Game_Creatures_HumanCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AnimalCurrentLane> __Game_Creatures_AnimalCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrainCurrentLane> __Game_Vehicles_TrainCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WatercraftCurrentLane> __Game_Vehicles_WatercraftCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AircraftCurrentLane> __Game_Vehicles_AircraftCurrentLane_RO_ComponentLookup;

		public BufferTypeHandle<LaneObject> __Game_Net_LaneObject_RW_BufferTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Moving> __Game_Objects_Moving_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarNavigation> __Game_Vehicles_CarNavigation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HumanNavigation> __Game_Creatures_HumanNavigation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AnimalNavigation> __Game_Creatures_AnimalNavigation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Train> __Game_Vehicles_Train_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WatercraftNavigation> __Game_Vehicles_WatercraftNavigation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AircraftNavigation> __Game_Vehicles_AircraftNavigation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OutOfControl> __Game_Vehicles_OutOfControl_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Helicopter> __Game_Vehicles_Helicopter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Controller> __Game_Vehicles_Controller_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Bicycle> __Game_Vehicles_Bicycle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Lane> __Game_Net_Lane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MasterLane> __Game_Net_MasterLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrackLane> __Game_Net_TrackLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HangaroundLocation> __Game_Areas_HangaroundLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarLaneData> __Game_Prefabs_CarLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> __Game_Prefabs_TrackLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrainData> __Game_Prefabs_TrainData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LaneObject> __Game_Net_LaneObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		public ComponentLookup<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RW_ComponentLookup;

		public ComponentLookup<CarTrailerLane> __Game_Vehicles_CarTrailerLane_RW_ComponentLookup;

		public ComponentLookup<HumanCurrentLane> __Game_Creatures_HumanCurrentLane_RW_ComponentLookup;

		public ComponentLookup<AnimalCurrentLane> __Game_Creatures_AnimalCurrentLane_RW_ComponentLookup;

		public ComponentLookup<TrainCurrentLane> __Game_Vehicles_TrainCurrentLane_RW_ComponentLookup;

		public ComponentLookup<WatercraftCurrentLane> __Game_Vehicles_WatercraftCurrentLane_RW_ComponentLookup;

		public ComponentLookup<AircraftCurrentLane> __Game_Vehicles_AircraftCurrentLane_RW_ComponentLookup;

		public BufferLookup<BlockedLane> __Game_Objects_BlockedLane_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Vehicles_ParkedTrain_RO_ComponentLookup = state.GetComponentLookup<ParkedTrain>(isReadOnly: true);
			__Game_Vehicles_CarCurrentLane_RO_ComponentLookup = state.GetComponentLookup<CarCurrentLane>(isReadOnly: true);
			__Game_Vehicles_CarTrailerLane_RO_ComponentLookup = state.GetComponentLookup<CarTrailerLane>(isReadOnly: true);
			__Game_Creatures_HumanCurrentLane_RO_ComponentLookup = state.GetComponentLookup<HumanCurrentLane>(isReadOnly: true);
			__Game_Creatures_AnimalCurrentLane_RO_ComponentLookup = state.GetComponentLookup<AnimalCurrentLane>(isReadOnly: true);
			__Game_Vehicles_TrainCurrentLane_RO_ComponentLookup = state.GetComponentLookup<TrainCurrentLane>(isReadOnly: true);
			__Game_Vehicles_WatercraftCurrentLane_RO_ComponentLookup = state.GetComponentLookup<WatercraftCurrentLane>(isReadOnly: true);
			__Game_Vehicles_AircraftCurrentLane_RO_ComponentLookup = state.GetComponentLookup<AircraftCurrentLane>(isReadOnly: true);
			__Game_Net_LaneObject_RW_BufferTypeHandle = state.GetBufferTypeHandle<LaneObject>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Objects_Moving_RO_ComponentLookup = state.GetComponentLookup<Moving>(isReadOnly: true);
			__Game_Vehicles_CarNavigation_RO_ComponentLookup = state.GetComponentLookup<CarNavigation>(isReadOnly: true);
			__Game_Creatures_HumanNavigation_RO_ComponentLookup = state.GetComponentLookup<HumanNavigation>(isReadOnly: true);
			__Game_Creatures_AnimalNavigation_RO_ComponentLookup = state.GetComponentLookup<AnimalNavigation>(isReadOnly: true);
			__Game_Vehicles_Train_RO_ComponentLookup = state.GetComponentLookup<Train>(isReadOnly: true);
			__Game_Vehicles_WatercraftNavigation_RO_ComponentLookup = state.GetComponentLookup<WatercraftNavigation>(isReadOnly: true);
			__Game_Vehicles_AircraftNavigation_RO_ComponentLookup = state.GetComponentLookup<AircraftNavigation>(isReadOnly: true);
			__Game_Vehicles_OutOfControl_RO_ComponentLookup = state.GetComponentLookup<OutOfControl>(isReadOnly: true);
			__Game_Vehicles_Helicopter_RO_ComponentLookup = state.GetComponentLookup<Helicopter>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
			__Game_Vehicles_Bicycle_RO_ComponentLookup = state.GetComponentLookup<Bicycle>(isReadOnly: true);
			__Game_Net_Lane_RO_ComponentLookup = state.GetComponentLookup<Lane>(isReadOnly: true);
			__Game_Net_MasterLane_RO_ComponentLookup = state.GetComponentLookup<MasterLane>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<CarLane>(isReadOnly: true);
			__Game_Net_TrackLane_RO_ComponentLookup = state.GetComponentLookup<TrackLane>(isReadOnly: true);
			__Game_Net_PedestrianLane_RO_ComponentLookup = state.GetComponentLookup<PedestrianLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<ConnectionLane>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Areas_HangaroundLocation_RO_ComponentLookup = state.GetComponentLookup<HangaroundLocation>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
			__Game_Prefabs_TrackLaneData_RO_ComponentLookup = state.GetComponentLookup<TrackLaneData>(isReadOnly: true);
			__Game_Prefabs_TrainData_RO_ComponentLookup = state.GetComponentLookup<TrainData>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<SubLane>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
			__Game_Vehicles_CarCurrentLane_RW_ComponentLookup = state.GetComponentLookup<CarCurrentLane>();
			__Game_Vehicles_CarTrailerLane_RW_ComponentLookup = state.GetComponentLookup<CarTrailerLane>();
			__Game_Creatures_HumanCurrentLane_RW_ComponentLookup = state.GetComponentLookup<HumanCurrentLane>();
			__Game_Creatures_AnimalCurrentLane_RW_ComponentLookup = state.GetComponentLookup<AnimalCurrentLane>();
			__Game_Vehicles_TrainCurrentLane_RW_ComponentLookup = state.GetComponentLookup<TrainCurrentLane>();
			__Game_Vehicles_WatercraftCurrentLane_RW_ComponentLookup = state.GetComponentLookup<WatercraftCurrentLane>();
			__Game_Vehicles_AircraftCurrentLane_RW_ComponentLookup = state.GetComponentLookup<AircraftCurrentLane>();
			__Game_Objects_BlockedLane_RW_BufferLookup = state.GetBufferLookup<BlockedLane>();
		}
	}

	private ModificationBarrier5 m_ModificationBarrier;

	private SearchSystem m_NetSearchSystem;

	private Game.Areas.SearchSystem m_AreaSearchSystem;

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private EntityQuery m_LaneQuery;

	private LaneObjectUpdater m_LaneObjectUpdater;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
		m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_LaneQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<LaneObject>(),
				ComponentType.ReadOnly<Lane>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<ParkingLane>()
			}
		});
		m_LaneObjectUpdater = new LaneObjectUpdater(this);
		RequireForUpdate(m_LaneQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeQueue<Entity> laneObjectQueue = new NativeQueue<Entity>(Allocator.TempJob);
		CollectLaneObjectsJob jobData = new CollectLaneObjectsJob
		{
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedTrain_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarTrailerLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CarTrailerLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HumanCurrentLane = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_HumanCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AnimalCurrentLane = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_AnimalCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrainCurrentLane = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_TrainCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WatercraftCurrentLane = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_WatercraftCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AircraftCurrentLane = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_AircraftCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_LaneObject_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_LaneObjectQueue = laneObjectQueue.AsParallelWriter()
		};
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle dependencies3;
		JobHandle dependencies4;
		FixLaneObjectsJob jobData2 = new FixLaneObjectsJob
		{
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Moving_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarNavigationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CarNavigation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HumanNavigationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_HumanNavigation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AnimalNavigationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_AnimalNavigation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Train_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WatercraftNavigationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_WatercraftNavigation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AircraftNavigationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_AircraftNavigation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutOfControlData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_OutOfControl_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HelicopterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Helicopter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BicycleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Bicycle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Lane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MasterLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_MasterLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrackLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_TrackLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PedestrianLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HangaroundLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_HangaroundLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTrackLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrackLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrainData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaTriangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
			m_CarCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RW_ComponentLookup, ref base.CheckedStateRef),
			m_CarTrailerLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CarTrailerLane_RW_ComponentLookup, ref base.CheckedStateRef),
			m_HumanCurrentLane = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_HumanCurrentLane_RW_ComponentLookup, ref base.CheckedStateRef),
			m_AnimalCurrentLane = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_AnimalCurrentLane_RW_ComponentLookup, ref base.CheckedStateRef),
			m_TrainCurrentLane = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_TrainCurrentLane_RW_ComponentLookup, ref base.CheckedStateRef),
			m_WatercraftCurrentLane = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_WatercraftCurrentLane_RW_ComponentLookup, ref base.CheckedStateRef),
			m_AircraftCurrentLane = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_AircraftCurrentLane_RW_ComponentLookup, ref base.CheckedStateRef),
			m_BlockedLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_BlockedLane_RW_BufferLookup, ref base.CheckedStateRef),
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
			m_AreaSearchTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies2),
			m_StaticObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies3),
			m_MovingObjectSearchTree = m_ObjectSearchSystem.GetMovingSearchTree(readOnly: true, out dependencies4),
			m_LaneObjectQueue = laneObjectQueue,
			m_LaneObjectBuffer = m_LaneObjectUpdater.Begin(Allocator.TempJob)
		};
		JobHandle job = JobChunkExtensions.ScheduleParallel(jobData, m_LaneQuery, base.Dependency);
		JobHandle jobHandle = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(job, dependencies2, JobHandle.CombineDependencies(dependencies, dependencies3, dependencies4)));
		laneObjectQueue.Dispose(jobHandle);
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
		m_AreaSearchSystem.AddSearchTreeReader(jobHandle);
		m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
		m_ObjectSearchSystem.AddMovingSearchTreeReader(jobHandle);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
		JobHandle dependency = m_LaneObjectUpdater.Apply(this, jobHandle);
		base.Dependency = dependency;
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
	public FixLaneObjectsSystem()
	{
	}
}
