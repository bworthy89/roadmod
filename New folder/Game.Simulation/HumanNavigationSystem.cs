using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Creatures;
using Game.Events;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Rendering;
using Game.Routes;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class HumanNavigationSystem : GameSystemBase
{
	[CompilerGenerated]
	public class Groups : GameSystemBase
	{
		private struct TypeHandle
		{
			[ReadOnly]
			public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

			[ReadOnly]
			public ComponentTypeHandle<GroupMember> __Game_Creatures_GroupMember_RO_ComponentTypeHandle;

			[ReadOnly]
			public ComponentLookup<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentLookup;

			[ReadOnly]
			public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

			public ComponentLookup<HumanCurrentLane> __Game_Creatures_HumanCurrentLane_RW_ComponentLookup;

			public ComponentLookup<PathOwner> __Game_Pathfind_PathOwner_RW_ComponentLookup;

			public BufferLookup<PathElement> __Game_Pathfind_PathElement_RW_BufferLookup;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
				__Game_Creatures_GroupMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<GroupMember>(isReadOnly: true);
				__Game_Creatures_CurrentVehicle_RO_ComponentLookup = state.GetComponentLookup<CurrentVehicle>(isReadOnly: true);
				__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
				__Game_Creatures_HumanCurrentLane_RW_ComponentLookup = state.GetComponentLookup<HumanCurrentLane>();
				__Game_Pathfind_PathOwner_RW_ComponentLookup = state.GetComponentLookup<PathOwner>();
				__Game_Pathfind_PathElement_RW_BufferLookup = state.GetBufferLookup<PathElement>();
			}
		}

		private SimulationSystem m_SimulationSystem;

		private EntityQuery m_CreatureQuery;

		private TypeHandle __TypeHandle;

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
			m_CreatureQuery = GetEntityQuery(ComponentType.ReadOnly<Human>(), ComponentType.ReadOnly<GroupMember>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<HumanCurrentLane>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		}

		[Preserve]
		protected override void OnUpdate()
		{
			uint index = m_SimulationSystem.frameIndex % 16;
			m_CreatureQuery.ResetFilter();
			m_CreatureQuery.SetSharedComponentFilter(new UpdateFrame(index));
			JobHandle dependency = JobChunkExtensions.ScheduleParallel(new GroupNavigationJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_GroupMemberType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_GroupMember_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CurrentVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_HumanCurrentLane_RW_ComponentLookup, ref base.CheckedStateRef),
				m_PathOwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentLookup, ref base.CheckedStateRef),
				m_Paths = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RW_BufferLookup, ref base.CheckedStateRef)
			}, m_CreatureQuery, base.Dependency);
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
		public Groups()
		{
		}
	}

	[CompilerGenerated]
	public class Actions : GameSystemBase
	{
		private struct TypeHandle
		{
			public ComponentLookup<LaneSignal> __Game_Net_LaneSignal_RW_ComponentLookup;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				__Game_Net_LaneSignal_RW_ComponentLookup = state.GetComponentLookup<LaneSignal>();
			}
		}

		public LaneObjectUpdater m_LaneObjectUpdater;

		public NativeQueue<HumanNavigationHelpers.LaneSignal> m_LaneSignalQueue;

		public JobHandle m_Dependency;

		private TypeHandle __TypeHandle;

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			m_LaneObjectUpdater = new LaneObjectUpdater(this);
		}

		[Preserve]
		protected override void OnUpdate()
		{
			JobHandle jobHandle = JobHandle.CombineDependencies(base.Dependency, m_Dependency);
			JobHandle jobHandle2 = IJobExtensions.Schedule(new UpdateLaneSignalsJob
			{
				m_LaneSignalQueue = m_LaneSignalQueue,
				m_LaneSignalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneSignal_RW_ComponentLookup, ref base.CheckedStateRef)
			}, jobHandle);
			m_LaneSignalQueue.Dispose(jobHandle2);
			JobHandle job = m_LaneObjectUpdater.Apply(this, jobHandle);
			base.Dependency = JobHandle.CombineDependencies(job, jobHandle2);
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
		public Actions()
		{
		}
	}

	[BurstCompile]
	private struct GroupNavigationJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<GroupMember> m_GroupMemberType;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> m_CurrentVehicleData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<HumanCurrentLane> m_CurrentLaneData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<PathOwner> m_PathOwnerData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<PathElement> m_Paths;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<GroupMember> nativeArray = chunk.GetNativeArray(ref m_GroupMemberType);
			if (nativeArray.Length == 0)
			{
				return;
			}
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity entity = nativeArray2[i];
				GroupMember groupMember = nativeArray[i];
				HumanCurrentLane value = m_CurrentLaneData[entity];
				PathOwner value2 = m_PathOwnerData[entity];
				DynamicBuffer<PathElement> dynamicBuffer = m_Paths[entity];
				if (value2.m_ElementIndex > 0)
				{
					dynamicBuffer.RemoveRange(0, value2.m_ElementIndex);
					value2.m_ElementIndex = 0;
				}
				value.m_Flags &= ~CreatureLaneFlags.Leader;
				value2.m_State &= PathFlags.Stuck;
				if (m_CurrentVehicleData.HasComponent(groupMember.m_Leader))
				{
					if (dynamicBuffer.Length == 0 && (value.m_Flags & (CreatureLaneFlags.ParkingSpace | CreatureLaneFlags.Transport)) == 0)
					{
						value.m_Flags |= CreatureLaneFlags.Transport;
					}
				}
				else if (m_CurrentLaneData.HasComponent(groupMember.m_Leader))
				{
					HumanCurrentLane humanCurrentLane = m_CurrentLaneData[groupMember.m_Leader];
					PathOwner pathOwner = m_PathOwnerData[groupMember.m_Leader];
					DynamicBuffer<PathElement> dynamicBuffer2 = m_Paths[groupMember.m_Leader];
					if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Failed | PathFlags.Obsolete | PathFlags.Updated)) == 0)
					{
						int num = -1;
						if (value.m_Lane == humanCurrentLane.m_Lane && value.m_CurvePosition.y == humanCurrentLane.m_CurvePosition.y && ((value.m_Flags ^ humanCurrentLane.m_Flags) & (CreatureLaneFlags.Taxi | CreatureLaneFlags.WaitPosition)) == 0)
						{
							value.m_Flags |= CreatureLaneFlags.Leader;
							num = 0;
						}
						else
						{
							for (int j = 0; j < dynamicBuffer.Length; j++)
							{
								PathElement value3 = dynamicBuffer[j];
								if (value3.m_Target == humanCurrentLane.m_Lane && value3.m_TargetDelta.y == humanCurrentLane.m_CurvePosition.y)
								{
									value3.m_Flags |= PathElementFlags.Leader;
									dynamicBuffer[j] = value3;
									num = j + 1;
									break;
								}
								value3.m_Flags &= ~PathElementFlags.Leader;
								dynamicBuffer[j] = value3;
							}
						}
						if (num == -1)
						{
							PathElementFlags pathElementFlags = PathElementFlags.Leader;
							if ((humanCurrentLane.m_Flags & CreatureLaneFlags.Taxi) != 0)
							{
								pathElementFlags |= PathElementFlags.Secondary;
							}
							if ((humanCurrentLane.m_Flags & CreatureLaneFlags.WaitPosition) != 0)
							{
								pathElementFlags |= PathElementFlags.WaitPosition;
							}
							dynamicBuffer.Clear();
							dynamicBuffer.Add(new PathElement(humanCurrentLane.m_Lane, humanCurrentLane.m_CurvePosition, pathElementFlags));
						}
						else if (num < dynamicBuffer.Length)
						{
							dynamicBuffer.RemoveRange(num, dynamicBuffer.Length - num);
						}
						if ((humanCurrentLane.m_Flags & CreatureLaneFlags.Area) != 0)
						{
							Entity entity2 = Entity.Null;
							if (m_OwnerData.TryGetComponent(humanCurrentLane.m_Lane, out var componentData))
							{
								entity2 = componentData.m_Owner;
							}
							for (int k = pathOwner.m_ElementIndex; k < dynamicBuffer2.Length; k++)
							{
								PathElement elem = dynamicBuffer2[k];
								dynamicBuffer.Add(elem);
								if (!m_OwnerData.TryGetComponent(elem.m_Target, out componentData) || componentData.m_Owner != entity2)
								{
									break;
								}
							}
						}
						else
						{
							int num2 = math.min(pathOwner.m_ElementIndex + 2, dynamicBuffer2.Length);
							for (int l = pathOwner.m_ElementIndex; l < num2; l++)
							{
								dynamicBuffer.Add(dynamicBuffer2[l]);
							}
						}
					}
				}
				m_CurrentLaneData[entity] = value;
				m_PathOwnerData[entity] = value2;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateNavigationJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Moving> m_MovingType;

		[ReadOnly]
		public ComponentTypeHandle<GroupMember> m_GroupMemberType;

		[ReadOnly]
		public ComponentTypeHandle<Stumbling> m_StumblingType;

		[ReadOnly]
		public ComponentTypeHandle<TripSource> m_TripSourceType;

		[ReadOnly]
		public ComponentTypeHandle<Human> m_HumanType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentVehicle> m_CurrentVehicleType;

		[ReadOnly]
		public ComponentTypeHandle<PseudoRandomSeed> m_PseudoRandomSeedType;

		[ReadOnly]
		public ComponentTypeHandle<InvolvedInAccident> m_InvolvedInAccidentType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<MeshGroup> m_MeshGroupType;

		public ComponentTypeHandle<HumanNavigation> m_NavigationType;

		public ComponentTypeHandle<HumanCurrentLane> m_CurrentLaneType;

		public ComponentTypeHandle<Blocker> m_BlockerType;

		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		public BufferTypeHandle<Queue> m_QueueType;

		public BufferTypeHandle<PathElement> m_PathElementType;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<LaneSignal> m_LaneSignalData;

		[ReadOnly]
		public ComponentLookup<LaneReservation> m_LaneReservationData;

		[ReadOnly]
		public ComponentLookup<AreaLane> m_AreaLaneData;

		[ReadOnly]
		public ComponentLookup<NodeLane> m_NodeLaneData;

		[ReadOnly]
		public ComponentLookup<Waypoint> m_WaypointData;

		[ReadOnly]
		public ComponentLookup<TaxiStand> m_TaxiStandData;

		[ReadOnly]
		public ComponentLookup<Position> m_PositionData;

		[ReadOnly]
		public ComponentLookup<Connected> m_ConnectedData;

		[ReadOnly]
		public ComponentLookup<Game.Routes.TakeoffLocation> m_TakeoffLocationData;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenterData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Moving> m_MovingData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.ActivityLocation> m_ActivityLocationData;

		[ReadOnly]
		public ComponentLookup<Creature> m_CreatureData;

		[ReadOnly]
		public ComponentLookup<GroupMember> m_GroupMemberData;

		[ReadOnly]
		public ComponentLookup<HangaroundLocation> m_HangaroundLocationData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PublicTransport> m_PublicTransportData;

		[ReadOnly]
		public ComponentLookup<Train> m_TrainData;

		[ReadOnly]
		public ComponentLookup<Controller> m_ControllerData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<CreatureData> m_PrefabCreatureData;

		[ReadOnly]
		public ComponentLookup<HumanData> m_PrefabHumanData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_PrefabLaneData;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> m_PrefabSpawnLocationData;

		[ReadOnly]
		public ComponentLookup<CarData> m_PrefabCarData;

		[ReadOnly]
		public ComponentLookup<PedestrianLaneData> m_PrefabPedestrianLaneData;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_Lanes;

		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjects;

		[ReadOnly]
		public BufferLookup<LaneOverlap> m_LaneOverlaps;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_AreaTriangles;

		[ReadOnly]
		public BufferLookup<ActivityLocationElement> m_PrefabActivityLocations;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> m_SubMeshGroups;

		[ReadOnly]
		public BufferLookup<CharacterElement> m_CharacterElements;

		[ReadOnly]
		public BufferLookup<AnimationClip> m_AnimationClips;

		[ReadOnly]
		public BufferLookup<AnimationMotion> m_AnimationMotions;

		[ReadOnly]
		public BufferLookup<SubMesh> m_SubMeshes;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public bool m_LeftHandTraffic;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_StaticObjectSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_MovingObjectSearchTree;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

		public LaneObjectCommandBuffer m_LaneObjectBuffer;

		public NativeQueue<HumanNavigationHelpers.LaneSignal>.ParallelWriter m_LaneSignals;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Objects.Transform> nativeArray2 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<Moving> nativeArray3 = chunk.GetNativeArray(ref m_MovingType);
			NativeArray<GroupMember> nativeArray4 = chunk.GetNativeArray(ref m_GroupMemberType);
			NativeArray<HumanNavigation> nativeArray5 = chunk.GetNativeArray(ref m_NavigationType);
			NativeArray<HumanCurrentLane> nativeArray6 = chunk.GetNativeArray(ref m_CurrentLaneType);
			NativeArray<Blocker> nativeArray7 = chunk.GetNativeArray(ref m_BlockerType);
			NativeArray<PathOwner> nativeArray8 = chunk.GetNativeArray(ref m_PathOwnerType);
			NativeArray<PrefabRef> nativeArray9 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<Queue> bufferAccessor = chunk.GetBufferAccessor(ref m_QueueType);
			BufferAccessor<PathElement> bufferAccessor2 = chunk.GetBufferAccessor(ref m_PathElementType);
			BufferAccessor<MeshGroup> bufferAccessor3 = chunk.GetBufferAccessor(ref m_MeshGroupType);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			if (chunk.Has(ref m_StumblingType))
			{
				for (int i = 0; i < chunk.Count; i++)
				{
					Entity entity = nativeArray[i];
					Game.Objects.Transform transform = nativeArray2[i];
					HumanNavigation navigation = nativeArray5[i];
					HumanCurrentLane currentLane = nativeArray6[i];
					Blocker blocker = nativeArray7[i];
					PathOwner pathOwner = nativeArray8[i];
					PrefabRef prefabRef = nativeArray9[i];
					DynamicBuffer<Queue> dynamicBuffer = bufferAccessor[i];
					DynamicBuffer<PathElement> pathElements = bufferAccessor2[i];
					ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
					CollectionUtils.TryGet(nativeArray3, i, out var value);
					CollectionUtils.TryGet(nativeArray4, i, out var value2);
					HumanNavigationHelpers.CurrentLaneCache currentLaneCache = new HumanNavigationHelpers.CurrentLaneCache(ref currentLane, m_EntityLookup, m_MovingObjectSearchTree);
					dynamicBuffer.Clear();
					UpdateStumbling(entity, transform, value2, objectGeometryData, ref navigation, ref currentLane, ref blocker, ref pathOwner, pathElements);
					currentLaneCache.CheckChanges(entity, ref currentLane, m_LaneObjectBuffer, m_LaneObjects, transform, value, navigation, objectGeometryData);
					nativeArray5[i] = navigation;
					nativeArray6[i] = currentLane;
					nativeArray7[i] = blocker;
					nativeArray8[i] = pathOwner;
				}
				return;
			}
			NativeArray<TripSource> nativeArray10 = chunk.GetNativeArray(ref m_TripSourceType);
			NativeArray<Human> nativeArray11 = chunk.GetNativeArray(ref m_HumanType);
			NativeArray<CurrentVehicle> nativeArray12 = chunk.GetNativeArray(ref m_CurrentVehicleType);
			NativeArray<PseudoRandomSeed> nativeArray13 = chunk.GetNativeArray(ref m_PseudoRandomSeedType);
			bool isInvolvedInAccident = chunk.Has(ref m_InvolvedInAccidentType);
			for (int j = 0; j < chunk.Count; j++)
			{
				Entity entity2 = nativeArray[j];
				Game.Objects.Transform transform2 = nativeArray2[j];
				Moving moving = nativeArray3[j];
				Human human = nativeArray11[j];
				HumanNavigation navigation2 = nativeArray5[j];
				HumanCurrentLane currentLane2 = nativeArray6[j];
				Blocker blocker2 = nativeArray7[j];
				PathOwner pathOwner2 = nativeArray8[j];
				PrefabRef prefabRef2 = nativeArray9[j];
				DynamicBuffer<Queue> queues = bufferAccessor[j];
				DynamicBuffer<PathElement> pathElements2 = bufferAccessor2[j];
				CreatureData prefabCreatureData = m_PrefabCreatureData[prefabRef2.m_Prefab];
				HumanData prefabHumanData = m_PrefabHumanData[prefabRef2.m_Prefab];
				ObjectGeometryData objectGeometryData2 = m_PrefabObjectGeometryData[prefabRef2.m_Prefab];
				CollectionUtils.TryGet(nativeArray4, j, out var value3);
				CollectionUtils.TryGet(nativeArray10, j, out var value4);
				CollectionUtils.TryGet(nativeArray12, j, out var value5);
				CollectionUtils.TryGet(nativeArray13, j, out var value6);
				CollectionUtils.TryGet(bufferAccessor3, j, out var value7);
				HumanNavigationHelpers.CurrentLaneCache currentLaneCache2 = new HumanNavigationHelpers.CurrentLaneCache(ref currentLane2, m_EntityLookup, m_MovingObjectSearchTree);
				if ((currentLane2.m_Lane == Entity.Null || (currentLane2.m_Flags & CreatureLaneFlags.Obsolete) != 0) && (human.m_Flags & HumanFlags.Carried) == 0)
				{
					if ((currentLane2.m_Flags & (CreatureLaneFlags.Obsolete | CreatureLaneFlags.FindLane)) == CreatureLaneFlags.FindLane)
					{
						TryFindCurrentLane(ref currentLane2, transform2);
					}
					else
					{
						TryFindCurrentLane(ref currentLane2, transform2);
						if (value3.m_Leader == Entity.Null)
						{
							pathElements2.Clear();
							pathOwner2.m_ElementIndex = 0;
							pathOwner2.m_State |= PathFlags.Obsolete;
						}
					}
				}
				UpdateQueues(value5, ref pathOwner2, queues);
				UpdateNavigationTarget(ref random, isInvolvedInAccident, entity2, transform2, moving, value4, value5, value6, human, value3, prefabRef2, prefabCreatureData, prefabHumanData, objectGeometryData2, ref navigation2, ref currentLane2, ref blocker2, ref pathOwner2, queues, pathElements2, value7);
				currentLaneCache2.CheckChanges(entity2, ref currentLane2, m_LaneObjectBuffer, m_LaneObjects, transform2, moving, navigation2, objectGeometryData2);
				nativeArray5[j] = navigation2;
				nativeArray6[j] = currentLane2;
				nativeArray7[j] = blocker2;
				nativeArray8[j] = pathOwner2;
			}
		}

		private void UpdateStumbling(Entity entity, Game.Objects.Transform transform, GroupMember groupMember, ObjectGeometryData prefabObjectGeometryData, ref HumanNavigation navigation, ref HumanCurrentLane currentLane, ref Blocker blocker, ref PathOwner pathOwner, DynamicBuffer<PathElement> pathElements)
		{
			TryFindCurrentLane(ref currentLane, transform);
			navigation = new HumanNavigation
			{
				m_TargetPosition = transform.m_Position
			};
			blocker = default(Blocker);
			pathOwner.m_ElementIndex = 0;
			pathElements.Clear();
			if (groupMember.m_Leader == Entity.Null)
			{
				pathOwner.m_State |= PathFlags.Obsolete;
			}
		}

		private void TryFindCurrentLane(ref HumanCurrentLane currentLane, Game.Objects.Transform transform)
		{
			bool flag = (currentLane.m_Flags & CreatureLaneFlags.EmergeUnspawned) != 0;
			currentLane.m_Flags &= ~(CreatureLaneFlags.EndOfPath | CreatureLaneFlags.EndReached | CreatureLaneFlags.TransformTarget | CreatureLaneFlags.ParkingSpace | CreatureLaneFlags.Obsolete | CreatureLaneFlags.Transport | CreatureLaneFlags.Connection | CreatureLaneFlags.Taxi | CreatureLaneFlags.FindLane | CreatureLaneFlags.Area | CreatureLaneFlags.Hangaround | CreatureLaneFlags.WaitPosition | CreatureLaneFlags.EmergeUnspawned);
			currentLane.m_Lane = Entity.Null;
			float3 position = transform.m_Position;
			Bounds3 bounds = new Bounds3(position - 100f, position + 100f);
			HumanNavigationHelpers.FindLaneIterator iterator = new HumanNavigationHelpers.FindLaneIterator
			{
				m_Bounds = bounds,
				m_Position = position,
				m_MinDistance = 1000f,
				m_UnspawnedEmerge = flag,
				m_Result = currentLane,
				m_SubLanes = m_Lanes,
				m_AreaNodes = m_AreaNodes,
				m_AreaTriangles = m_AreaTriangles,
				m_PedestrianLaneData = m_PedestrianLaneData,
				m_ConnectionLaneData = m_ConnectionLaneData,
				m_CurveData = m_CurveData,
				m_HangaroundLocationData = m_HangaroundLocationData
			};
			m_NetSearchTree.Iterate(ref iterator);
			m_StaticObjectSearchTree.Iterate(ref iterator);
			if (!flag)
			{
				m_AreaSearchTree.Iterate(ref iterator);
			}
			currentLane = iterator.m_Result;
		}

		private float GetTargetSpeed(TripSource tripSource, Human human, HumanData prefabHumanData, float leaderDistance, ref HumanCurrentLane currentLane)
		{
			float num = 0f;
			if (tripSource.m_Source == Entity.Null)
			{
				if ((human.m_Flags & HumanFlags.Run) != 0)
				{
					return prefabHumanData.m_RunSpeed;
				}
				num = prefabHumanData.m_WalkSpeed;
			}
			if (m_PedestrianLaneData.HasComponent(currentLane.m_Lane))
			{
				Game.Net.PedestrianLane pedestrianLane = m_PedestrianLaneData[currentLane.m_Lane];
				if ((pedestrianLane.m_Flags & PedestrianLaneFlags.Unsafe) != 0)
				{
					return prefabHumanData.m_RunSpeed;
				}
				if ((pedestrianLane.m_Flags & PedestrianLaneFlags.Crosswalk) != 0)
				{
					num = math.lerp(prefabHumanData.m_WalkSpeed, prefabHumanData.m_RunSpeed, 0.25f);
				}
			}
			if (m_LaneSignalData.HasComponent(currentLane.m_Lane))
			{
				LaneSignal laneSignal = m_LaneSignalData[currentLane.m_Lane];
				if (laneSignal.m_Signal == LaneSignalType.SafeStop || laneSignal.m_Signal == LaneSignalType.Stop)
				{
					return prefabHumanData.m_RunSpeed;
				}
			}
			if (num != 0f)
			{
				num = math.max(num, math.min(prefabHumanData.m_WalkSpeed * (1f + leaderDistance * 0.05f), prefabHumanData.m_RunSpeed));
			}
			return num;
		}

		private void UpdateQueues(CurrentVehicle currentVehicle, ref PathOwner pathOwner, DynamicBuffer<Queue> queues)
		{
			if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Failed | PathFlags.Obsolete | PathFlags.Updated)) != 0 || currentVehicle.m_Vehicle != Entity.Null)
			{
				queues.Clear();
				return;
			}
			for (int i = 0; i < queues.Length; i++)
			{
				Queue value = queues[i];
				if (++value.m_ObsoleteTime >= 500)
				{
					queues.RemoveAt(i--);
				}
				else
				{
					queues[i] = value;
				}
			}
		}

		private void UpdateNavigationTarget(ref Unity.Mathematics.Random random, bool isInvolvedInAccident, Entity entity, Game.Objects.Transform transform, Moving moving, TripSource tripSource, CurrentVehicle currentVehicle, PseudoRandomSeed pseudoRandomSeed, Human human, GroupMember groupMember, PrefabRef prefabRef, CreatureData prefabCreatureData, HumanData prefabHumanData, ObjectGeometryData prefabObjectGeometryData, ref HumanNavigation navigation, ref HumanCurrentLane currentLane, ref Blocker blocker, ref PathOwner pathOwner, DynamicBuffer<Queue> queues, DynamicBuffer<PathElement> pathElements, DynamicBuffer<MeshGroup> meshGroups)
		{
			float num = 4f / 15f;
			float num2 = math.length(moving.m_Velocity);
			float num3 = 0f;
			if (m_TransformData.TryGetComponent(groupMember.m_Leader, out var componentData))
			{
				num3 = (((currentLane.m_Flags & CreatureLaneFlags.Leader) == 0) ? math.distance(componentData.m_Position, transform.m_Position) : math.max(math.dot(math.normalizesafe(navigation.m_TargetPosition - transform.m_Position), componentData.m_Position - transform.m_Position), 0f));
				num3 -= pseudoRandomSeed.GetRandom(PseudoRandomSeed.kFollowDistance).NextFloat(2f);
			}
			if ((currentLane.m_Flags & CreatureLaneFlags.Connection) != 0)
			{
				prefabHumanData.m_WalkSpeed = 277.77777f;
				prefabHumanData.m_RunSpeed = 277.77777f;
				prefabHumanData.m_Acceleration = 277.77777f;
			}
			else
			{
				prefabHumanData.m_RunSpeed *= 1f + num3 * 0.01f;
				prefabHumanData.m_RunSpeed = math.min(prefabHumanData.m_RunSpeed, 277.77777f);
				num2 = math.min(num2, prefabHumanData.m_RunSpeed);
			}
			Bounds1 bounds = new Bounds1(num2 + new float2(0f - prefabHumanData.m_Acceleration, prefabHumanData.m_Acceleration) * num);
			float targetSpeed = GetTargetSpeed(tripSource, human, prefabHumanData, num3, ref currentLane);
			float num4 = prefabHumanData.m_Acceleration * 0.1f;
			if (num2 <= prefabHumanData.m_WalkSpeed)
			{
				targetSpeed = math.min(targetSpeed, math.max(prefabHumanData.m_WalkSpeed, num2 + num4 * num));
				navigation.m_MaxSpeed = MathUtils.Clamp(targetSpeed, bounds);
			}
			else
			{
				Bounds1 bounds2 = new Bounds1(num2 + new float2(0f - num4, num4) * num);
				navigation.m_MaxSpeed = MathUtils.Clamp(targetSpeed, bounds2);
			}
			float num5 = math.max(prefabObjectGeometryData.m_Bounds.max.z, (prefabObjectGeometryData.m_Bounds.max.x - prefabObjectGeometryData.m_Bounds.min.x) * 0.5f);
			float num6;
			if ((currentLane.m_Flags & (CreatureLaneFlags.EndReached | CreatureLaneFlags.TransformTarget | CreatureLaneFlags.Area)) != 0 || currentLane.m_Lane == Entity.Null || ((currentLane.m_Flags & CreatureLaneFlags.Connection) != 0 && (currentLane.m_Flags & (CreatureLaneFlags.ParkingSpace | CreatureLaneFlags.WaitPosition)) != 0))
			{
				num6 = math.distance(transform.m_Position, navigation.m_TargetPosition);
				float distance = math.select(num6, math.max(0f, num6 - num5), (currentLane.m_Flags & CreatureLaneFlags.TransformTarget) == 0);
				float maxBrakingSpeed = CreatureUtils.GetMaxBrakingSpeed(prefabHumanData, distance, num);
				maxBrakingSpeed = MathUtils.Clamp(maxBrakingSpeed, bounds);
				navigation.m_MaxSpeed = math.min(navigation.m_MaxSpeed, maxBrakingSpeed);
			}
			else
			{
				if ((currentLane.m_Flags & CreatureLaneFlags.WaitSignal) != 0)
				{
					navigation.m_TargetPosition = transform.m_Position;
					navigation.m_TargetDirection = default(float2);
					navigation.m_TargetActivity = 0;
					num6 = 0f;
					if (pathOwner.m_ElementIndex < pathElements.Length)
					{
						PathElement pathElement = pathElements[pathOwner.m_ElementIndex];
						if (m_CurveData.HasComponent(pathElement.m_Target))
						{
							float lanePosition = math.select(currentLane.m_LanePosition, 0f - currentLane.m_LanePosition, (currentLane.m_Flags & CreatureLaneFlags.Backward) != 0 != pathElement.m_TargetDelta.y < pathElement.m_TargetDelta.x);
							Line3.Segment segment = CalculateTargetPos(prefabObjectGeometryData, pathElement.m_Target, pathElement.m_TargetDelta, lanePosition);
							navigation.m_TargetPosition = segment.a;
							navigation.m_TargetDirection = math.normalizesafe(segment.b.xz - segment.a.xz);
							num6 = math.distance(transform.m_Position, navigation.m_TargetPosition);
						}
					}
				}
				else
				{
					navigation.m_TargetPosition = CalculateTargetPos(prefabObjectGeometryData, currentLane.m_Lane, currentLane.m_CurvePosition.x, currentLane.m_LanePosition);
					navigation.m_TargetDirection = default(float2);
					navigation.m_TargetActivity = 0;
					num6 = math.distance(transform.m_Position, navigation.m_TargetPosition);
				}
				float brakingDistance = CreatureUtils.GetBrakingDistance(prefabHumanData, navigation.m_MaxSpeed, num);
				float num7 = math.max(0f, num6 - num5);
				if (num7 < brakingDistance)
				{
					float maxBrakingSpeed2 = CreatureUtils.GetMaxBrakingSpeed(prefabHumanData, num7, num);
					maxBrakingSpeed2 = MathUtils.Clamp(maxBrakingSpeed2, bounds);
					navigation.m_MaxSpeed = math.min(navigation.m_MaxSpeed, maxBrakingSpeed2);
				}
			}
			navigation.m_MaxSpeed = math.select(navigation.m_MaxSpeed, 0f, navigation.m_MaxSpeed < 0.1f);
			Entity blocker2 = blocker.m_Blocker;
			float maxSpeed = navigation.m_MaxSpeed;
			blocker.m_Blocker = Entity.Null;
			blocker.m_Type = BlockerType.None;
			currentLane.m_QueueEntity = Entity.Null;
			currentLane.m_QueueArea = default(Sphere3);
			float num8 = num5 + math.max(1f, navigation.m_MaxSpeed * num) + CreatureUtils.GetBrakingDistance(prefabHumanData, navigation.m_MaxSpeed, num);
			if (num2 >= 0.1f)
			{
				float num9 = num2 * num;
				float num10 = random.NextFloat(0f, 1f);
				num10 *= num10;
				num10 = math.select(0.5f - num10, num10 - 0.5f, m_LeftHandTraffic != ((currentLane.m_Flags & CreatureLaneFlags.Backward) != 0));
				currentLane.m_LanePosition = math.lerp(currentLane.m_LanePosition, num10, math.min(1f, num9 * 0.01f));
			}
			if (num6 < num8)
			{
				CreatureTargetIterator targetIterator = new CreatureTargetIterator
				{
					m_MovingData = m_MovingData,
					m_CurveData = m_CurveData,
					m_LaneReservationData = m_LaneReservationData,
					m_LaneOverlaps = m_LaneOverlaps,
					m_LaneObjects = m_LaneObjects,
					m_PrefabObjectGeometry = prefabObjectGeometryData,
					m_Blocker = blocker.m_Blocker,
					m_BlockerType = blocker.m_Type,
					m_QueueEntity = currentLane.m_QueueEntity,
					m_QueueArea = currentLane.m_QueueArea
				};
				while (true)
				{
					byte activity = 0;
					if ((currentLane.m_Flags & (CreatureLaneFlags.EndReached | CreatureLaneFlags.WaitSignal)) == 0 && currentLane.m_Lane != Entity.Null)
					{
						if ((currentLane.m_Flags & CreatureLaneFlags.TransformTarget) != 0)
						{
							if ((currentLane.m_Flags & CreatureLaneFlags.WaitPosition) != 0)
							{
								if (MoveTransformTarget(entity, prefabRef.m_Prefab, meshGroups, ref random, human, currentVehicle, pseudoRandomSeed, transform.m_Position, ref navigation.m_TargetPosition, ref navigation.m_TargetDirection, ref activity, 0f, currentLane.m_Lane, prefabCreatureData.m_SupportedActivities))
								{
									navigation.m_TargetPosition = VehicleUtils.GetConnectionParkingPosition(default(Game.Net.ConnectionLane), new Bezier4x3(navigation.m_TargetPosition, navigation.m_TargetPosition, navigation.m_TargetPosition, navigation.m_TargetPosition), currentLane.m_CurvePosition.y);
									navigation.m_TargetDirection = default(float2);
									navigation.m_TargetActivity = 0;
								}
							}
							else if (MoveTransformTarget(entity, prefabRef.m_Prefab, meshGroups, ref random, human, currentVehicle, pseudoRandomSeed, transform.m_Position, ref navigation.m_TargetPosition, ref navigation.m_TargetDirection, ref activity, num8, currentLane.m_Lane, prefabCreatureData.m_SupportedActivities))
							{
								break;
							}
						}
						else if ((currentLane.m_Flags & CreatureLaneFlags.Connection) != 0 && (currentLane.m_Flags & (CreatureLaneFlags.ParkingSpace | CreatureLaneFlags.WaitPosition)) != 0)
						{
							Curve curve = m_CurveData[currentLane.m_Lane];
							Game.Net.ConnectionLane connectionLane = m_ConnectionLaneData[currentLane.m_Lane];
							navigation.m_TargetPosition = VehicleUtils.GetConnectionParkingPosition(connectionLane, curve.m_Bezier, currentLane.m_CurvePosition.y);
							navigation.m_TargetDirection = default(float2);
							navigation.m_TargetActivity = 0;
						}
						else if ((currentLane.m_Flags & CreatureLaneFlags.Area) != 0)
						{
							navigation.m_TargetActivity = 0;
							float navigationSize = CreatureUtils.GetNavigationSize(prefabObjectGeometryData);
							if (MoveAreaTarget(ref random, transform.m_Position, pathOwner, pathElements, ref navigation.m_TargetPosition, ref navigation.m_TargetDirection, ref activity, num8, currentLane.m_Lane, prefabCreatureData.m_SupportedActivities, ref currentLane.m_CurvePosition, currentLane.m_LanePosition, navigationSize))
							{
								break;
							}
						}
						else
						{
							Curve curve2 = m_CurveData[currentLane.m_Lane];
							PrefabRef prefabRef2 = m_PrefabRefData[currentLane.m_Lane];
							NetLaneData prefabLaneData = m_PrefabLaneData[prefabRef2.m_Prefab];
							navigation.m_TargetDirection = default(float2);
							navigation.m_TargetActivity = 0;
							if ((currentLane.m_Flags & CreatureLaneFlags.Hangaround) != 0)
							{
								Unity.Mathematics.Random random2 = Unity.Mathematics.Random.CreateFromIndex(math.asuint(currentLane.m_CurvePosition.y));
								ActivityType activity2 = ActivityType.Standing;
								CreatureUtils.GetLaneActivity(ref random2, ref activity2, prefabRef2.m_Prefab, prefabCreatureData.m_SupportedActivities, ref m_PrefabPedestrianLaneData);
								if (activity2 != ActivityType.Standing)
								{
									activity = (byte)activity2;
									if (activity2 == ActivityType.Fishing)
									{
										float3 @float = MathUtils.Position(curve2.m_Bezier, currentLane.m_CurvePosition.y);
										float2 value = MathUtils.Tangent(curve2.m_Bezier, currentLane.m_CurvePosition.y).xz;
										if (MathUtils.TryNormalize(ref value) && math.abs(math.dot(transform.m_Position.xz - @float.xz, value)) < num8)
										{
											currentLane.m_LanePosition = math.select(-0.5f, 0.5f, currentLane.m_LanePosition >= 0f);
											navigation.m_TargetDirection = math.select(MathUtils.Left(value), MathUtils.Right(value), currentLane.m_LanePosition >= 0f);
										}
									}
								}
							}
							m_NodeLaneData.TryGetComponent(currentLane.m_Lane, out var componentData2);
							float laneOffset = CreatureUtils.GetLaneOffset(prefabObjectGeometryData, prefabLaneData, componentData2, currentLane.m_CurvePosition.x, currentLane.m_LanePosition);
							if (MoveLaneTarget(ref targetIterator, currentLane.m_Lane, transform.m_Position, ref navigation.m_TargetPosition, num8, curve2.m_Bezier, ref currentLane.m_CurvePosition, laneOffset))
							{
								break;
							}
						}
					}
					if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Failed | PathFlags.Obsolete | PathFlags.Updated)) != 0 || isInvolvedInAccident)
					{
						if ((currentLane.m_Flags & CreatureLaneFlags.Action) == 0)
						{
							break;
						}
						if ((currentLane.m_Flags & CreatureLaneFlags.EndReached) != 0)
						{
							if (navigation.m_TargetActivity == 0 || navigation.m_TransformState == TransformState.Idle)
							{
								navigation.m_TargetActivity = 0;
								currentLane.m_Flags |= CreatureLaneFlags.ActivityDone;
							}
						}
						else
						{
							currentLane.m_Flags &= ~CreatureLaneFlags.Action;
						}
						break;
					}
					if ((currentLane.m_Flags & CreatureLaneFlags.EndOfPath) != 0 || pathOwner.m_ElementIndex >= pathElements.Length)
					{
						if (groupMember.m_Leader != Entity.Null)
						{
							if ((currentLane.m_Flags & CreatureLaneFlags.EndOfPath) == 0)
							{
								targetIterator.m_Blocker = groupMember.m_Leader;
								targetIterator.m_BlockerType = BlockerType.Continuing;
							}
							else if (pathOwner.m_ElementIndex < pathElements.Length)
							{
								currentLane.m_Flags &= ~CreatureLaneFlags.EndOfPath;
							}
						}
						else
						{
							currentLane.m_Flags &= ~(CreatureLaneFlags.ParkingSpace | CreatureLaneFlags.Transport | CreatureLaneFlags.Taxi);
							currentLane.m_Flags |= CreatureLaneFlags.EndOfPath;
						}
						num6 = math.distance(transform.m_Position, navigation.m_TargetPosition);
						float num11 = math.select(0.1f, num5 + 1f, (currentLane.m_Flags & CreatureLaneFlags.TransformTarget) == 0);
						if (!(num6 < num11) || !(num2 < 0.1f))
						{
							break;
						}
						if ((currentLane.m_Flags & CreatureLaneFlags.EndReached) != 0)
						{
							if (navigation.m_TargetActivity == 0)
							{
								currentLane.m_Flags |= CreatureLaneFlags.ActivityDone;
							}
							break;
						}
						navigation.m_TargetActivity = activity;
						currentLane.m_Flags |= CreatureLaneFlags.EndReached;
						if (navigation.m_TargetActivity == 0)
						{
							currentLane.m_Flags |= CreatureLaneFlags.ActivityDone;
						}
						break;
					}
					PathElement pathElement2 = pathElements[pathOwner.m_ElementIndex];
					CreatureLaneFlags creatureLaneFlags = ((pathElement2.m_TargetDelta.y < pathElement2.m_TargetDelta.x) ? CreatureLaneFlags.Backward : ((CreatureLaneFlags)0u));
					if ((pathElement2.m_Flags & PathElementFlags.Leader) != 0)
					{
						creatureLaneFlags |= CreatureLaneFlags.Leader;
					}
					if ((pathElement2.m_Flags & PathElementFlags.Hangaround) != 0)
					{
						creatureLaneFlags |= CreatureLaneFlags.Hangaround;
					}
					currentLane.m_Flags &= ~(CreatureLaneFlags.ParkingSpace | CreatureLaneFlags.Transport | CreatureLaneFlags.Taxi | CreatureLaneFlags.Action);
					if ((pathElement2.m_Flags & PathElementFlags.Action) != 0)
					{
						currentLane.m_Flags |= CreatureLaneFlags.Action;
						num6 = math.distance(transform.m_Position, navigation.m_TargetPosition);
						float num12 = math.select(0.1f, num5 + 1f, (currentLane.m_Flags & CreatureLaneFlags.TransformTarget) == 0);
						if (!(num6 < num12) || !(num2 < 0.1f))
						{
							break;
						}
						if ((currentLane.m_Flags & CreatureLaneFlags.EndReached) != 0)
						{
							if (navigation.m_TargetActivity == 0 || navigation.m_TransformState == TransformState.Idle)
							{
								navigation.m_TargetActivity = 0;
								currentLane.m_Flags |= CreatureLaneFlags.ActivityDone;
							}
						}
						else
						{
							SetActionTarget(ref navigation, transform, human, pathElement2);
							currentLane.m_Flags &= ~CreatureLaneFlags.ActivityDone;
							currentLane.m_Flags |= CreatureLaneFlags.EndReached;
						}
						break;
					}
					LaneSignal componentData7;
					if (!m_PedestrianLaneData.HasComponent(pathElement2.m_Target))
					{
						if (m_ParkingLaneData.TryGetComponent(pathElement2.m_Target, out var componentData3))
						{
							currentLane.m_Flags |= CreatureLaneFlags.ParkingSpace;
							if ((pathElement2.m_Flags & PathElementFlags.Secondary) != 0 && (componentData3.m_Flags & (ParkingLaneFlags.AdditionalStart | ParkingLaneFlags.SecondaryStart)) == 0)
							{
								currentLane.m_Flags |= CreatureLaneFlags.Taxi;
							}
							num6 = math.distance(transform.m_Position, navigation.m_TargetPosition);
							float num13 = math.select(0.1f, num5 + 1f, (currentLane.m_Flags & CreatureLaneFlags.TransformTarget) == 0);
							if (num6 < num13 && num2 < 0.1f)
							{
								currentLane.m_Flags |= CreatureLaneFlags.EndReached;
							}
							break;
						}
						if (m_WaypointData.HasComponent(pathElement2.m_Target) || m_TaxiStandData.HasComponent(pathElement2.m_Target))
						{
							currentLane.m_Flags |= CreatureLaneFlags.Transport;
							if ((currentLane.m_Flags & CreatureLaneFlags.EndReached) != 0)
							{
								break;
							}
							num6 = math.distance(transform.m_Position, navigation.m_TargetPosition);
							float num14 = math.select(0.1f, num5 + 1f, (currentLane.m_Flags & CreatureLaneFlags.TransformTarget) == 0);
							if (!(num6 < num14) || !(num2 < 0.1f))
							{
								break;
							}
							if ((currentLane.m_Flags & CreatureLaneFlags.TransformTarget) == 0)
							{
								Entity entity2 = pathElement2.m_Target;
								if (m_ConnectedData.HasComponent(entity2))
								{
									entity2 = m_ConnectedData[entity2].m_Connected;
								}
								byte activity3 = 0;
								float3 targetPosition = navigation.m_TargetPosition;
								float2 targetDirection = navigation.m_TargetDirection;
								MoveTransformTarget(entity, prefabRef.m_Prefab, meshGroups, ref random, human, currentVehicle, pseudoRandomSeed, transform.m_Position, ref targetPosition, ref targetDirection, ref activity3, num8, entity2, prefabCreatureData.m_SupportedActivities);
								if (activity3 != 0)
								{
									currentLane.m_Lane = entity2;
									currentLane.m_CurvePosition = 0f;
									currentLane.m_Flags = CreatureLaneFlags.TransformTarget;
									navigation.m_TargetPosition = targetPosition;
									navigation.m_TargetDirection = targetDirection;
									continue;
								}
							}
							if (activity == 0)
							{
								navigation.m_TargetPosition = transform.m_Position;
								Game.Objects.Transform componentData5;
								if (m_PositionData.TryGetComponent(pathElement2.m_Target, out var componentData4))
								{
									navigation.m_TargetDirection = math.normalizesafe(componentData4.m_Position.xz - transform.m_Position.xz);
								}
								else if (m_TransformData.TryGetComponent(pathElement2.m_Target, out componentData5))
								{
									navigation.m_TargetDirection = math.normalizesafe(componentData5.m_Position.xz - transform.m_Position.xz);
								}
							}
							navigation.m_TargetActivity = activity;
							currentLane.m_Flags |= CreatureLaneFlags.EndReached;
							break;
						}
						if (m_ConnectionLaneData.HasComponent(pathElement2.m_Target))
						{
							Game.Net.ConnectionLane connectionLane2 = m_ConnectionLaneData[pathElement2.m_Target];
							if ((connectionLane2.m_Flags & ConnectionLaneFlags.Parking) != 0)
							{
								currentLane.m_Flags |= CreatureLaneFlags.ParkingSpace;
								if ((pathElement2.m_Flags & PathElementFlags.Secondary) != 0 && connectionLane2.m_RoadTypes != RoadTypes.Bicycle)
								{
									currentLane.m_Flags |= CreatureLaneFlags.Taxi;
								}
								num6 = math.distance(transform.m_Position, navigation.m_TargetPosition);
								float num15 = math.select(0.1f, num5 + 1f, (currentLane.m_Flags & CreatureLaneFlags.TransformTarget) == 0);
								if (num6 < num15 && num2 < 0.1f)
								{
									currentLane.m_Flags |= CreatureLaneFlags.EndReached;
								}
								break;
							}
							if ((pathElement2.m_Flags & PathElementFlags.WaitPosition) != 0)
							{
								creatureLaneFlags |= CreatureLaneFlags.WaitPosition;
							}
							if ((connectionLane2.m_Flags & ConnectionLaneFlags.Area) != 0)
							{
								if ((currentLane.m_Flags & CreatureLaneFlags.TransformTarget) != 0 && m_SpawnLocationData.HasComponent(currentLane.m_Lane) && !m_ActivityLocationData.HasComponent(currentLane.m_Lane) && (num2 > prefabHumanData.m_WalkSpeed || math.distance(transform.m_Position, navigation.m_TargetPosition) > num5 + 1f))
								{
									break;
								}
								creatureLaneFlags |= CreatureLaneFlags.Area;
								if (m_OwnerData.TryGetComponent(pathElement2.m_Target, out var componentData6) && m_HangaroundLocationData.HasComponent(componentData6.m_Owner))
								{
									creatureLaneFlags |= CreatureLaneFlags.Hangaround;
								}
							}
							else
							{
								creatureLaneFlags |= CreatureLaneFlags.Connection;
							}
						}
						else
						{
							if (m_SpawnLocationData.HasComponent(pathElement2.m_Target))
							{
								pathOwner.m_ElementIndex++;
								currentLane.m_Lane = pathElement2.m_Target;
								currentLane.m_CurvePosition = 0f;
								currentLane.m_Flags = CreatureLaneFlags.TransformTarget;
								if (m_ActivityLocationData.HasComponent(pathElement2.m_Target))
								{
									currentLane.m_Flags |= CreatureLaneFlags.Hangaround;
								}
								if ((pathElement2.m_Flags & PathElementFlags.WaitPosition) != 0)
								{
									currentLane.m_CurvePosition = pathElement2.m_TargetDelta.y;
									currentLane.m_Flags |= CreatureLaneFlags.WaitPosition;
								}
								if (pathOwner.m_ElementIndex >= pathElements.Length)
								{
									currentLane.m_Flags |= CreatureLaneFlags.EndOfPath;
								}
								continue;
							}
							if (m_TakeoffLocationData.HasComponent(pathElement2.m_Target))
							{
								pathOwner.m_ElementIndex++;
								continue;
							}
							if (GetTransformTarget(ref currentLane.m_Lane, pathElement2.m_Target))
							{
								pathOwner.m_ElementIndex++;
								navigation.m_TargetActivity = 0;
								currentLane.m_CurvePosition = 0f;
								currentLane.m_Flags = CreatureLaneFlags.EndOfPath | CreatureLaneFlags.TransformTarget;
								continue;
							}
						}
					}
					else if (pathElement2.m_Target != currentLane.m_Lane && (human.m_Flags & HumanFlags.Emergency) == 0 && m_LaneSignalData.TryGetComponent(pathElement2.m_Target, out componentData7))
					{
						m_LaneSignals.Enqueue(new HumanNavigationHelpers.LaneSignal(entity, pathElement2.m_Target, 100));
						if (componentData7.m_Signal == LaneSignalType.Stop || componentData7.m_Signal == LaneSignalType.SafeStop)
						{
							currentLane.m_Flags |= CreatureLaneFlags.WaitSignal;
							float lanePosition2 = math.select(currentLane.m_LanePosition, 0f - currentLane.m_LanePosition, ((currentLane.m_Flags ^ creatureLaneFlags) & CreatureLaneFlags.Backward) != 0);
							Line3.Segment segment2 = CalculateTargetPos(prefabObjectGeometryData, pathElement2.m_Target, pathElement2.m_TargetDelta, lanePosition2);
							navigation.m_TargetPosition = segment2.a;
							navigation.m_TargetDirection = math.normalizesafe(segment2.b.xz - segment2.a.xz);
							navigation.m_TargetActivity = 0;
							targetIterator.m_Blocker = componentData7.m_Blocker;
							targetIterator.m_BlockerType = BlockerType.Signal;
							targetIterator.m_QueueEntity = pathElement2.m_Target;
							targetIterator.m_QueueArea = CreatureUtils.GetQueueArea(prefabObjectGeometryData, segment2.a, segment2.b);
							break;
						}
					}
					if (((currentLane.m_Flags & ~creatureLaneFlags & CreatureLaneFlags.Connection) != 0 && num6 >= num5 + 1f) || (groupMember.m_Leader != Entity.Null && (currentLane.m_Flags & CreatureLaneFlags.Leader) != 0))
					{
						break;
					}
					pathOwner.m_ElementIndex++;
					if (!m_CurveData.HasComponent(pathElement2.m_Target))
					{
						pathElements.Clear();
						pathOwner.m_ElementIndex = 0;
						if (groupMember.m_Leader == Entity.Null)
						{
							pathOwner.m_State |= PathFlags.Obsolete;
						}
						break;
					}
					for (int i = 0; i < queues.Length; i++)
					{
						if (queues[i].m_TargetEntity == currentLane.m_Lane)
						{
							queues.RemoveAt(i--);
						}
					}
					if (((currentLane.m_Flags ^ creatureLaneFlags) & CreatureLaneFlags.Backward) != 0 && currentLane.m_Lane != pathElement2.m_Target)
					{
						currentLane.m_LanePosition = 0f - currentLane.m_LanePosition;
					}
					currentLane.m_Lane = pathElement2.m_Target;
					currentLane.m_CurvePosition = pathElement2.m_TargetDelta;
					currentLane.m_Flags = creatureLaneFlags;
				}
				blocker.m_Blocker = targetIterator.m_Blocker;
				blocker.m_Type = targetIterator.m_BlockerType;
				currentLane.m_QueueEntity = targetIterator.m_QueueEntity;
				currentLane.m_QueueArea = targetIterator.m_QueueArea;
			}
			if (groupMember.m_Leader != Entity.Null)
			{
				if ((currentLane.m_Flags & CreatureLaneFlags.Leader) != 0 && m_TransformData.TryGetComponent(groupMember.m_Leader, out componentData))
				{
					Line3.Segment line = new Line3.Segment(transform.m_Position, navigation.m_TargetPosition);
					MathUtils.Distance(line, componentData.m_Position, out var t);
					num3 = MathUtils.Length(line) * t;
					float maxBrakingSpeed3 = CreatureUtils.GetMaxBrakingSpeed(prefabHumanData, num3, num);
					maxBrakingSpeed3 = MathUtils.Clamp(maxBrakingSpeed3, bounds);
					if (maxBrakingSpeed3 < navigation.m_MaxSpeed)
					{
						navigation.m_MaxSpeed = math.min(navigation.m_MaxSpeed, maxBrakingSpeed3);
						blocker.m_Blocker = groupMember.m_Leader;
					}
				}
				if (blocker.m_Blocker == groupMember.m_Leader && currentLane.m_QueueArea.radius <= 0f)
				{
					Creature creature = m_CreatureData[groupMember.m_Leader];
					if (creature.m_QueueArea.radius > 0f)
					{
						Sphere3 queueArea = CreatureUtils.GetQueueArea(prefabObjectGeometryData, transform.m_Position, navigation.m_TargetPosition);
						currentLane.m_QueueEntity = creature.m_QueueEntity;
						currentLane.m_QueueArea = MathUtils.Sphere(creature.m_QueueArea, queueArea);
					}
				}
			}
			if (navigation.m_MaxSpeed != 0f || blocker2 != Entity.Null)
			{
				CreatureCollisionIterator creatureCollisionIterator = new CreatureCollisionIterator
				{
					m_OwnerData = m_OwnerData,
					m_TransformData = m_TransformData,
					m_MovingData = m_MovingData,
					m_CreatureData = m_CreatureData,
					m_GroupMemberData = m_GroupMemberData,
					m_WaypointData = m_WaypointData,
					m_TaxiStandData = m_TaxiStandData,
					m_CurveData = m_CurveData,
					m_AreaLaneData = m_AreaLaneData,
					m_NodeLaneData = m_NodeLaneData,
					m_PrefabRefData = m_PrefabRefData,
					m_PrefabObjectGeometryData = m_PrefabObjectGeometryData,
					m_PrefabLaneData = m_PrefabLaneData,
					m_LaneObjects = m_LaneObjects,
					m_AreaNodes = m_AreaNodes,
					m_StaticObjectSearchTree = m_StaticObjectSearchTree,
					m_MovingObjectSearchTree = m_MovingObjectSearchTree,
					m_Entity = entity,
					m_Leader = groupMember.m_Leader,
					m_CurrentLane = currentLane.m_Lane,
					m_CurrentVehicle = currentVehicle.m_Vehicle,
					m_CurvePosition = currentLane.m_CurvePosition.y,
					m_TimeStep = num,
					m_PrefabObjectGeometry = prefabObjectGeometryData,
					m_SpeedRange = bounds,
					m_CurrentPosition = transform.m_Position,
					m_CurrentDirection = math.forward(transform.m_Rotation),
					m_CurrentVelocity = moving.m_Velocity,
					m_TargetDistance = num8,
					m_PathOwner = pathOwner,
					m_PathElements = pathElements,
					m_MinSpeed = random.NextFloat(0.4f, 0.6f),
					m_TargetPosition = navigation.m_TargetPosition,
					m_MaxSpeed = navigation.m_MaxSpeed,
					m_LanePosition = currentLane.m_LanePosition,
					m_Blocker = blocker.m_Blocker,
					m_BlockerType = blocker.m_Type,
					m_QueueEntity = currentLane.m_QueueEntity,
					m_QueueArea = currentLane.m_QueueArea,
					m_Queues = queues
				};
				if (blocker2 != Entity.Null)
				{
					creatureCollisionIterator.IterateBlocker(prefabHumanData, blocker2);
					creatureCollisionIterator.m_MaxSpeed = math.select(creatureCollisionIterator.m_MaxSpeed, 0f, creatureCollisionIterator.m_MaxSpeed < 0.1f);
				}
				if (creatureCollisionIterator.m_MaxSpeed != 0f)
				{
					if ((currentLane.m_Flags & CreatureLaneFlags.Connection) == 0)
					{
						bool isBackward = (currentLane.m_Flags & CreatureLaneFlags.Backward) != 0;
						if ((currentLane.m_Flags & CreatureLaneFlags.WaitSignal) != 0)
						{
							int elementIndex = pathOwner.m_ElementIndex;
							if (elementIndex < pathElements.Length)
							{
								PathElement pathElement3 = pathElements[elementIndex++];
								if (m_CurveData.HasComponent(pathElement3.m_Target) && creatureCollisionIterator.IterateFirstLane(currentLane.m_Lane, pathElement3.m_Target, currentLane.m_CurvePosition, pathElement3.m_TargetDelta, isBackward))
								{
									while (creatureCollisionIterator.IterateNextLane(pathElement3.m_Target, pathElement3.m_TargetDelta) && elementIndex < pathElements.Length)
									{
										pathElement3 = pathElements[elementIndex++];
									}
								}
							}
						}
						else if (creatureCollisionIterator.IterateFirstLane(currentLane.m_Lane, currentLane.m_CurvePosition, isBackward))
						{
							int elementIndex2 = pathOwner.m_ElementIndex;
							if (elementIndex2 < pathElements.Length)
							{
								PathElement pathElement4 = pathElements[elementIndex2++];
								while (creatureCollisionIterator.IterateNextLane(pathElement4.m_Target, pathElement4.m_TargetDelta) && elementIndex2 < pathElements.Length)
								{
									pathElement4 = pathElements[elementIndex2++];
								}
							}
						}
					}
					creatureCollisionIterator.m_MaxSpeed = math.select(creatureCollisionIterator.m_MaxSpeed, 0f, creatureCollisionIterator.m_MaxSpeed < 0.1f);
				}
				navigation.m_TargetPosition = creatureCollisionIterator.m_TargetPosition;
				navigation.m_MaxSpeed = creatureCollisionIterator.m_MaxSpeed;
				currentLane.m_LanePosition = math.lerp(currentLane.m_LanePosition, creatureCollisionIterator.m_LanePosition, 0.5f);
				currentLane.m_QueueEntity = creatureCollisionIterator.m_QueueEntity;
				currentLane.m_QueueArea = creatureCollisionIterator.m_QueueArea;
				blocker.m_Blocker = creatureCollisionIterator.m_Blocker;
				blocker.m_Type = creatureCollisionIterator.m_BlockerType;
				maxSpeed = creatureCollisionIterator.m_MaxSpeed;
			}
			blocker.m_MaxSpeed = (byte)math.clamp(Mathf.RoundToInt(maxSpeed * 45.899998f), 0, 255);
			if ((human.m_Flags & (HumanFlags.Waiting | HumanFlags.Sad | HumanFlags.Happy | HumanFlags.Angry)) != 0 && navigation.m_MaxSpeed < 0.1f && navigation.m_TargetActivity == 0 && random.NextInt(100) == 0)
			{
				navigation.m_TargetActivity = 21;
			}
		}

		private float3 CalculateTargetPos(ObjectGeometryData prefabObjectGeometryData, Entity lane, float curvePosition, float lanePosition)
		{
			Curve curve = m_CurveData[lane];
			PrefabRef prefabRef = m_PrefabRefData[lane];
			NetLaneData prefabLaneData = m_PrefabLaneData[prefabRef.m_Prefab];
			m_NodeLaneData.TryGetComponent(lane, out var componentData);
			return CreatureUtils.GetLanePosition(laneOffset: CreatureUtils.GetLaneOffset(prefabObjectGeometryData, prefabLaneData, componentData, curvePosition, lanePosition), curve: curve.m_Bezier, curvePosition: curvePosition);
		}

		private Line3.Segment CalculateTargetPos(ObjectGeometryData prefabObjectGeometryData, Entity lane, float2 curvePosition, float lanePosition)
		{
			Curve curve = m_CurveData[lane];
			PrefabRef prefabRef = m_PrefabRefData[lane];
			NetLaneData prefabLaneData = m_PrefabLaneData[prefabRef.m_Prefab];
			m_NodeLaneData.TryGetComponent(lane, out var componentData);
			float laneOffset = CreatureUtils.GetLaneOffset(prefabObjectGeometryData, prefabLaneData, componentData, curvePosition.x, lanePosition);
			Line3.Segment result = default(Line3.Segment);
			result.a = CreatureUtils.GetLanePosition(curve.m_Bezier, curvePosition.x, laneOffset);
			result.b = CreatureUtils.GetLanePosition(curve.m_Bezier, curvePosition.y, laneOffset);
			return result;
		}

		private void SetActionTarget(ref HumanNavigation navigation, Game.Objects.Transform transform, Human human, PathElement pathElement)
		{
			bool flag = false;
			if ((human.m_Flags & HumanFlags.Selfies) != 0)
			{
				navigation.m_TargetActivity = 7;
				flag = true;
			}
			if (m_TransformData.HasComponent(pathElement.m_Target))
			{
				Game.Objects.Transform transform2 = m_TransformData[pathElement.m_Target];
				if (flag)
				{
					navigation.m_TargetDirection = math.normalizesafe(transform.m_Position.xz - transform2.m_Position.xz);
				}
				else
				{
					navigation.m_TargetDirection = math.normalizesafe(transform2.m_Position.xz - transform.m_Position.xz);
				}
			}
		}

		private bool MoveAreaTarget(ref Unity.Mathematics.Random random, float3 comparePosition, PathOwner pathOwner, DynamicBuffer<PathElement> pathElements, ref float3 targetPosition, ref float2 targetDirection, ref byte activity, float minDistance, Entity target, ActivityMask activityMask, ref float2 curveDelta, float lanePosition, float navigationSize)
		{
			if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Obsolete | PathFlags.Updated)) != 0)
			{
				return true;
			}
			Entity owner = m_OwnerData[target].m_Owner;
			AreaLane areaLane = m_AreaLaneData[target];
			DynamicBuffer<Game.Areas.Node> nodes = m_AreaNodes[owner];
			bool flag = curveDelta.y < curveDelta.x;
			targetDirection = default(float2);
			activity = 0;
			if (areaLane.m_Nodes.y == areaLane.m_Nodes.z)
			{
				float3 position = nodes[areaLane.m_Nodes.x].m_Position;
				float3 position2 = nodes[areaLane.m_Nodes.y].m_Position;
				float3 position3 = nodes[areaLane.m_Nodes.w].m_Position;
				if (CreatureUtils.SetTriangleTarget(position, position2, position3, comparePosition, default(PathElement), pathOwner.m_ElementIndex, pathElements, ref targetPosition, minDistance, lanePosition, curveDelta.y, navigationSize, isSingle: true, m_TransformData, m_TaxiStandData, m_AreaLaneData, m_CurveData))
				{
					return true;
				}
				curveDelta.x = curveDelta.y;
			}
			else
			{
				bool4 @bool = new bool4(curveDelta < 0.5f, curveDelta > 0.5f);
				int2 @int = math.select(areaLane.m_Nodes.x, areaLane.m_Nodes.w, @bool.zw);
				float3 position4 = nodes[@int.x].m_Position;
				float3 position5 = nodes[areaLane.m_Nodes.y].m_Position;
				float3 position6 = nodes[areaLane.m_Nodes.z].m_Position;
				float3 position7 = nodes[@int.y].m_Position;
				if (math.any(@bool.xy & @bool.wz))
				{
					if (CreatureUtils.SetAreaTarget(position4, position4, position5, position6, position7, owner, nodes, comparePosition, default(PathElement), pathOwner.m_ElementIndex, pathElements, ref targetPosition, minDistance, lanePosition, curveDelta.y, navigationSize, flag, m_TransformData, m_TaxiStandData, m_AreaLaneData, m_CurveData, m_OwnerData))
					{
						return true;
					}
					curveDelta.x = 0.5f;
					@bool.xz = false;
				}
				if (pathElements.Length > pathOwner.m_ElementIndex)
				{
					PathElement pathElement = pathElements[pathOwner.m_ElementIndex];
					if (m_OwnerData.TryGetComponent(pathElement.m_Target, out var componentData) && componentData.m_Owner == owner)
					{
						bool4 bool2 = new bool4(pathElement.m_TargetDelta < 0.5f, pathElement.m_TargetDelta > 0.5f);
						if (math.any(!@bool.xz) & math.any(@bool.yw) & math.any(bool2.xy & bool2.wz))
						{
							AreaLane areaLane2 = m_AreaLaneData[pathElement.m_Target];
							bool flag2 = pathElement.m_TargetDelta.y < pathElement.m_TargetDelta.x;
							lanePosition = math.select(lanePosition, 0f - lanePosition, flag2 != flag);
							@int = math.select(areaLane2.m_Nodes.x, areaLane2.m_Nodes.w, bool2.zw);
							position4 = nodes[@int.x].m_Position;
							if (CreatureUtils.SetAreaTarget(math.select(position5, position6, position4.Equals(position5)), left: nodes[areaLane2.m_Nodes.y].m_Position, right: nodes[areaLane2.m_Nodes.z].m_Position, next: nodes[@int.y].m_Position, prev: position4, areaEntity: owner, nodes: nodes, comparePosition: comparePosition, nextElement: default(PathElement), elementIndex: pathOwner.m_ElementIndex + 1, pathElements: pathElements, targetPosition: ref targetPosition, minDistance: minDistance, lanePosition: lanePosition, curveDelta: pathElement.m_TargetDelta.y, navigationSize: navigationSize, isBackward: flag2, transforms: m_TransformData, taxiStands: m_TaxiStandData, areaLanes: m_AreaLaneData, curves: m_CurveData, owners: m_OwnerData))
							{
								return true;
							}
						}
						curveDelta.x = curveDelta.y;
						return false;
					}
				}
				if (CreatureUtils.SetTriangleTarget(position5, position6, position7, comparePosition, default(PathElement), pathOwner.m_ElementIndex, pathElements, ref targetPosition, minDistance, lanePosition, curveDelta.y, navigationSize, isSingle: false, m_TransformData, m_TaxiStandData, m_AreaLaneData, m_CurveData))
				{
					return true;
				}
				curveDelta.x = curveDelta.y;
			}
			ActivityType activity2 = ActivityType.None;
			CreatureUtils.GetAreaActivity(ref random, ref activity2, target, activityMask, m_OwnerData, m_PrefabRefData, m_PrefabSpawnLocationData);
			if (activity2 != ActivityType.Standing)
			{
				activity = (byte)activity2;
			}
			return math.distance(comparePosition, targetPosition) >= minDistance;
		}

		private bool MoveTransformTarget(Entity creature, Entity creaturePrefab, DynamicBuffer<MeshGroup> meshGroups, ref Unity.Mathematics.Random random, Human human, CurrentVehicle currentVehicle, PseudoRandomSeed pseudoRandomSeed, float3 comparePosition, ref float3 targetPosition, ref float2 targetDirection, ref byte activity, float minDistance, Entity target, ActivityMask activityMask)
		{
			Game.Objects.Transform result = new Game.Objects.Transform
			{
				m_Position = targetPosition
			};
			ActivityType activity2 = ActivityType.None;
			ActivityCondition conditions = CreatureUtils.GetConditions(human);
			if (CreatureUtils.CalculateTransformPosition(creature, creaturePrefab, meshGroups, ref random, ref result, ref activity2, currentVehicle, pseudoRandomSeed, target, m_LeftHandTraffic, activityMask, conditions, m_MovingObjectSearchTree, ref m_TransformData, ref m_PositionData, ref m_PublicTransportData, ref m_TrainData, ref m_ControllerData, ref m_PrefabRefData, ref m_PrefabBuildingData, ref m_PrefabCarData, ref m_PrefabActivityLocations, ref m_SubMeshGroups, ref m_CharacterElements, ref m_SubMeshes, ref m_AnimationClips, ref m_AnimationMotions))
			{
				targetPosition = result.m_Position;
				if (result.m_Rotation.Equals(default(quaternion)))
				{
					targetDirection = default(float2);
				}
				else
				{
					targetDirection = math.normalizesafe(math.forward(result.m_Rotation).xz);
				}
				activity = (byte)activity2;
				return math.distance(comparePosition, targetPosition) >= minDistance;
			}
			return false;
		}

		private bool GetTransformTarget(ref Entity entity, Entity target)
		{
			if (m_PropertyRenterData.HasComponent(target))
			{
				target = m_PropertyRenterData[target].m_Property;
			}
			if (m_TransformData.HasComponent(target))
			{
				entity = target;
				return true;
			}
			if (m_PositionData.HasComponent(target))
			{
				entity = target;
				return true;
			}
			return false;
		}

		private bool MoveLaneTarget(ref CreatureTargetIterator targetIterator, Entity lane, float3 comparePosition, ref float3 targetPosition, float minDistance, Bezier4x3 curve, ref float2 curveDelta, float laneOffset)
		{
			float3 lanePosition = CreatureUtils.GetLanePosition(curve, curveDelta.y, laneOffset);
			if (math.distance(comparePosition, lanePosition) < minDistance)
			{
				if (targetIterator.IterateLane(lane, ref curveDelta.x, curveDelta.y))
				{
					targetPosition = lanePosition;
					return false;
				}
				targetPosition = CreatureUtils.GetLanePosition(curve, curveDelta.x, laneOffset);
				return true;
			}
			float2 @float = curveDelta;
			for (int i = 0; i < 8; i++)
			{
				float num = math.lerp(@float.x, @float.y, 0.5f);
				float3 lanePosition2 = CreatureUtils.GetLanePosition(curve, num, laneOffset);
				if (math.distance(comparePosition, lanePosition2) < minDistance)
				{
					@float.x = num;
				}
				else
				{
					@float.y = num;
				}
			}
			targetIterator.IterateLane(lane, ref curveDelta.x, @float.y);
			targetPosition = CreatureUtils.GetLanePosition(curve, curveDelta.x, laneOffset);
			return true;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateLaneSignalsJob : IJob
	{
		public NativeQueue<HumanNavigationHelpers.LaneSignal> m_LaneSignalQueue;

		public ComponentLookup<LaneSignal> m_LaneSignalData;

		public void Execute()
		{
			HumanNavigationHelpers.LaneSignal item;
			while (m_LaneSignalQueue.TryDequeue(out item))
			{
				LaneSignal value = m_LaneSignalData[item.m_Lane];
				if (item.m_Priority > value.m_Priority)
				{
					value.m_Petitioner = item.m_Petitioner;
					value.m_Priority = item.m_Priority;
					m_LaneSignalData[item.m_Lane] = value;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Moving> __Game_Objects_Moving_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<GroupMember> __Game_Creatures_GroupMember_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Stumbling> __Game_Creatures_Stumbling_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TripSource> __Game_Objects_TripSource_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Human> __Game_Creatures_Human_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InvolvedInAccident> __Game_Events_InvolvedInAccident_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<MeshGroup> __Game_Rendering_MeshGroup_RO_BufferTypeHandle;

		public ComponentTypeHandle<HumanNavigation> __Game_Creatures_HumanNavigation_RW_ComponentTypeHandle;

		public ComponentTypeHandle<HumanCurrentLane> __Game_Creatures_HumanCurrentLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Blocker> __Game_Vehicles_Blocker_RW_ComponentTypeHandle;

		public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RW_ComponentTypeHandle;

		public BufferTypeHandle<Queue> __Game_Creatures_Queue_RW_BufferTypeHandle;

		public BufferTypeHandle<PathElement> __Game_Pathfind_PathElement_RW_BufferTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LaneSignal> __Game_Net_LaneSignal_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LaneReservation> __Game_Net_LaneReservation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaLane> __Game_Net_AreaLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NodeLane> __Game_Net_NodeLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Waypoint> __Game_Routes_Waypoint_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TaxiStand> __Game_Routes_TaxiStand_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Position> __Game_Routes_Position_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Connected> __Game_Routes_Connected_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Routes.TakeoffLocation> __Game_Routes_TakeoffLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Moving> __Game_Objects_Moving_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.ActivityLocation> __Game_Objects_ActivityLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Creature> __Game_Creatures_Creature_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GroupMember> __Game_Creatures_GroupMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HangaroundLocation> __Game_Areas_HangaroundLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PublicTransport> __Game_Vehicles_PublicTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Train> __Game_Vehicles_Train_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Controller> __Game_Vehicles_Controller_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CreatureData> __Game_Prefabs_CreatureData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HumanData> __Game_Prefabs_HumanData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PedestrianLaneData> __Game_Prefabs_PedestrianLaneData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneObject> __Game_Net_LaneObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneOverlap> __Game_Net_LaneOverlap_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ActivityLocationElement> __Game_Prefabs_ActivityLocationElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> __Game_Prefabs_SubMeshGroup_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CharacterElement> __Game_Prefabs_CharacterElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<AnimationClip> __Game_Prefabs_AnimationClip_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<AnimationMotion> __Game_Prefabs_AnimationMotion_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMesh> __Game_Prefabs_SubMesh_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_Moving_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Moving>(isReadOnly: true);
			__Game_Creatures_GroupMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<GroupMember>(isReadOnly: true);
			__Game_Creatures_Stumbling_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Stumbling>(isReadOnly: true);
			__Game_Objects_TripSource_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TripSource>(isReadOnly: true);
			__Game_Creatures_Human_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Human>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentVehicle>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PseudoRandomSeed>(isReadOnly: true);
			__Game_Events_InvolvedInAccident_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InvolvedInAccident>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Rendering_MeshGroup_RO_BufferTypeHandle = state.GetBufferTypeHandle<MeshGroup>(isReadOnly: true);
			__Game_Creatures_HumanNavigation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<HumanNavigation>();
			__Game_Creatures_HumanCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<HumanCurrentLane>();
			__Game_Vehicles_Blocker_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Blocker>();
			__Game_Pathfind_PathOwner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>();
			__Game_Creatures_Queue_RW_BufferTypeHandle = state.GetBufferTypeHandle<Queue>();
			__Game_Pathfind_PathElement_RW_BufferTypeHandle = state.GetBufferTypeHandle<PathElement>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Net_PedestrianLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.PedestrianLane>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_LaneSignal_RO_ComponentLookup = state.GetComponentLookup<LaneSignal>(isReadOnly: true);
			__Game_Net_LaneReservation_RO_ComponentLookup = state.GetComponentLookup<LaneReservation>(isReadOnly: true);
			__Game_Net_AreaLane_RO_ComponentLookup = state.GetComponentLookup<AreaLane>(isReadOnly: true);
			__Game_Net_NodeLane_RO_ComponentLookup = state.GetComponentLookup<NodeLane>(isReadOnly: true);
			__Game_Routes_Waypoint_RO_ComponentLookup = state.GetComponentLookup<Waypoint>(isReadOnly: true);
			__Game_Routes_TaxiStand_RO_ComponentLookup = state.GetComponentLookup<TaxiStand>(isReadOnly: true);
			__Game_Routes_Position_RO_ComponentLookup = state.GetComponentLookup<Position>(isReadOnly: true);
			__Game_Routes_Connected_RO_ComponentLookup = state.GetComponentLookup<Connected>(isReadOnly: true);
			__Game_Routes_TakeoffLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Routes.TakeoffLocation>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_Moving_RO_ComponentLookup = state.GetComponentLookup<Moving>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Objects_ActivityLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.ActivityLocation>(isReadOnly: true);
			__Game_Creatures_Creature_RO_ComponentLookup = state.GetComponentLookup<Creature>(isReadOnly: true);
			__Game_Creatures_GroupMember_RO_ComponentLookup = state.GetComponentLookup<GroupMember>(isReadOnly: true);
			__Game_Areas_HangaroundLocation_RO_ComponentLookup = state.GetComponentLookup<HangaroundLocation>(isReadOnly: true);
			__Game_Vehicles_PublicTransport_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PublicTransport>(isReadOnly: true);
			__Game_Vehicles_Train_RO_ComponentLookup = state.GetComponentLookup<Train>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_CreatureData_RO_ComponentLookup = state.GetComponentLookup<CreatureData>(isReadOnly: true);
			__Game_Prefabs_HumanData_RO_ComponentLookup = state.GetComponentLookup<HumanData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
			__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(isReadOnly: true);
			__Game_Prefabs_PedestrianLaneData_RO_ComponentLookup = state.GetComponentLookup<PedestrianLaneData>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
			__Game_Net_LaneOverlap_RO_BufferLookup = state.GetBufferLookup<LaneOverlap>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
			__Game_Prefabs_ActivityLocationElement_RO_BufferLookup = state.GetBufferLookup<ActivityLocationElement>(isReadOnly: true);
			__Game_Prefabs_SubMeshGroup_RO_BufferLookup = state.GetBufferLookup<SubMeshGroup>(isReadOnly: true);
			__Game_Prefabs_CharacterElement_RO_BufferLookup = state.GetBufferLookup<CharacterElement>(isReadOnly: true);
			__Game_Prefabs_AnimationClip_RO_BufferLookup = state.GetBufferLookup<AnimationClip>(isReadOnly: true);
			__Game_Prefabs_AnimationMotion_RO_BufferLookup = state.GetBufferLookup<AnimationMotion>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = state.GetBufferLookup<SubMesh>(isReadOnly: true);
		}
	}

	private SimulationSystem m_SimulationSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private Game.Areas.SearchSystem m_AreaSearchSystem;

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private Actions m_Actions;

	private EntityQuery m_CreatureQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_Actions = base.World.GetOrCreateSystemManaged<Actions>();
		m_CreatureQuery = GetEntityQuery(ComponentType.ReadOnly<Human>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<HumanCurrentLane>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint index = m_SimulationSystem.frameIndex % 16;
		m_CreatureQuery.ResetFilter();
		m_CreatureQuery.SetSharedComponentFilter(new UpdateFrame(index));
		m_Actions.m_LaneSignalQueue = new NativeQueue<HumanNavigationHelpers.LaneSignal>(Allocator.TempJob);
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle dependencies3;
		JobHandle dependencies4;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new UpdateNavigationJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MovingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Moving_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_GroupMemberType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_GroupMember_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StumblingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Stumbling_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TripSourceType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_TripSource_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HumanType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Human_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentVehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PseudoRandomSeedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InvolvedInAccidentType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_InvolvedInAccident_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MeshGroupType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Rendering_MeshGroup_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_NavigationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_HumanNavigation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_HumanCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BlockerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Blocker_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_QueueType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Creatures_Queue_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PathElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Pathfind_PathElement_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PedestrianLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneSignalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneSignal_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneReservationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneReservation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AreaLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_AreaLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NodeLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaypointData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Waypoint_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TaxiStandData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_TaxiStand_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Position_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Connected_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TakeoffLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_TakeoffLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Moving_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ActivityLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_ActivityLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CreatureData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Creature_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GroupMemberData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_GroupMember_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HangaroundLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_HangaroundLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PublicTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PublicTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Train_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCreatureData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CreatureData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabHumanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_HumanData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPedestrianLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PedestrianLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Lanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneOverlaps = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneOverlap_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaTriangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabActivityLocations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ActivityLocationElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubMeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
			m_CharacterElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_CharacterElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_AnimationClips = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AnimationClip_RO_BufferLookup, ref base.CheckedStateRef),
			m_AnimationMotions = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AnimationMotion_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_LeftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
			m_StaticObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies2),
			m_MovingObjectSearchTree = m_ObjectSearchSystem.GetMovingSearchTree(readOnly: true, out dependencies3),
			m_AreaSearchTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies4),
			m_LaneObjectBuffer = m_Actions.m_LaneObjectUpdater.Begin(Allocator.TempJob),
			m_LaneSignals = m_Actions.m_LaneSignalQueue.AsParallelWriter()
		}, m_CreatureQuery, JobUtils.CombineDependencies(base.Dependency, dependencies, dependencies2, dependencies3, dependencies4));
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
		m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
		m_ObjectSearchSystem.AddMovingSearchTreeReader(jobHandle);
		m_AreaSearchSystem.AddSearchTreeReader(jobHandle);
		m_Actions.m_Dependency = jobHandle;
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
	public HumanNavigationSystem()
	{
	}
}
