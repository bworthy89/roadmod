using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Creatures;
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
public class AnimalNavigationSystem : GameSystemBase
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
			public ComponentLookup<HumanCurrentLane> __Game_Creatures_HumanCurrentLane_RO_ComponentLookup;

			public ComponentLookup<AnimalCurrentLane> __Game_Creatures_AnimalCurrentLane_RW_ComponentLookup;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void __AssignHandles(ref SystemState state)
			{
				__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
				__Game_Creatures_GroupMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<GroupMember>(isReadOnly: true);
				__Game_Creatures_HumanCurrentLane_RO_ComponentLookup = state.GetComponentLookup<HumanCurrentLane>(isReadOnly: true);
				__Game_Creatures_AnimalCurrentLane_RW_ComponentLookup = state.GetComponentLookup<AnimalCurrentLane>();
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
			m_CreatureQuery = GetEntityQuery(ComponentType.ReadOnly<Animal>(), ComponentType.ReadOnly<GroupMember>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<AnimalCurrentLane>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		}

		[Preserve]
		protected override void OnUpdate()
		{
			uint num = m_SimulationSystem.frameIndex % 16;
			if (num == 5 || num == 9 || num == 13)
			{
				m_CreatureQuery.ResetFilter();
				m_CreatureQuery.SetSharedComponentFilter(new UpdateFrame(num));
				JobHandle dependency = JobChunkExtensions.ScheduleParallel(new GroupNavigationJob
				{
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_GroupMemberType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_GroupMember_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_HumanCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_HumanCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
					m_AnimalCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_AnimalCurrentLane_RW_ComponentLookup, ref base.CheckedStateRef)
				}, m_CreatureQuery, base.Dependency);
				base.Dependency = dependency;
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
		public Groups()
		{
		}
	}

	public class Actions : GameSystemBase
	{
		private SimulationSystem m_SimulationSystem;

		public LaneObjectUpdater m_LaneObjectUpdater;

		public JobHandle m_Dependency;

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
			m_LaneObjectUpdater = new LaneObjectUpdater(this);
		}

		[Preserve]
		protected override void OnUpdate()
		{
			uint num = m_SimulationSystem.frameIndex % 16;
			if (num == 5 || num == 9 || num == 13)
			{
				JobHandle dependencies = JobHandle.CombineDependencies(base.Dependency, m_Dependency);
				JobHandle dependency = m_LaneObjectUpdater.Apply(this, dependencies);
				base.Dependency = dependency;
			}
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
		public ComponentLookup<HumanCurrentLane> m_HumanCurrentLaneData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<AnimalCurrentLane> m_AnimalCurrentLaneData;

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
				AnimalCurrentLane value = m_AnimalCurrentLaneData[entity];
				AnimalCurrentLane componentData2;
				if (m_HumanCurrentLaneData.TryGetComponent(groupMember.m_Leader, out var componentData))
				{
					if (componentData.m_Lane != value.m_Lane)
					{
						if (componentData.m_Lane != value.m_NextLane)
						{
							value.m_NextLane = componentData.m_Lane;
							value.m_NextPosition = componentData.m_CurvePosition;
						}
						else
						{
							value.m_NextPosition.y = componentData.m_CurvePosition.y;
						}
						value.m_NextFlags = (CreatureLaneFlags)((uint)componentData.m_Flags & 0xFFFFFDECu);
					}
					else
					{
						if (value.m_CurvePosition.y != componentData.m_CurvePosition.y)
						{
							value.m_CurvePosition.y = componentData.m_CurvePosition.y;
							value.m_Flags = (CreatureLaneFlags)((uint)componentData.m_Flags & 0xFFFFFDECu);
						}
						value.m_NextLane = Entity.Null;
					}
				}
				else if (m_AnimalCurrentLaneData.TryGetComponent(groupMember.m_Leader, out componentData2))
				{
					if (componentData2.m_Lane != value.m_Lane)
					{
						if (componentData2.m_Lane != value.m_NextLane)
						{
							value.m_NextLane = componentData2.m_Lane;
							value.m_NextPosition = componentData2.m_CurvePosition;
						}
						else
						{
							value.m_NextPosition.y = componentData2.m_CurvePosition.y;
						}
						value.m_NextFlags = (CreatureLaneFlags)((uint)componentData2.m_Flags & 0xFFFFFDECu);
					}
					else
					{
						if (value.m_CurvePosition.y != componentData2.m_CurvePosition.y)
						{
							value.m_CurvePosition.y = componentData2.m_CurvePosition.y;
							value.m_Flags = (CreatureLaneFlags)((uint)componentData2.m_Flags & 0xFFFFFDECu);
						}
						value.m_NextLane = Entity.Null;
					}
				}
				m_AnimalCurrentLaneData[entity] = value;
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
		public ComponentTypeHandle<Animal> m_AnimalType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<MeshGroup> m_MeshGroupType;

		public ComponentTypeHandle<AnimalNavigation> m_NavigationType;

		public ComponentTypeHandle<Blocker> m_BlockerType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<HumanCurrentLane> m_HumanCurrentLaneData;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> m_CurrentVehicleData;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<LaneReservation> m_LaneReservationData;

		[ReadOnly]
		public ComponentLookup<AreaLane> m_AreaLaneData;

		[ReadOnly]
		public ComponentLookup<NodeLane> m_NodeLaneData;

		[ReadOnly]
		public ComponentLookup<Position> m_PositionData;

		[ReadOnly]
		public ComponentLookup<TaxiStand> m_TaxiStandData;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenterData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Moving> m_MovingData;

		[ReadOnly]
		public ComponentLookup<Creature> m_CreatureData;

		[ReadOnly]
		public ComponentLookup<Animal> m_AnimalData;

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
		public ComponentLookup<PathOwner> m_PathOwnerData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<CreatureData> m_PrefabCreatureData;

		[ReadOnly]
		public ComponentLookup<AnimalData> m_PrefabAnimalData;

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
		public BufferLookup<PathElement> m_PathElements;

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

		[NativeDisableParallelForRestriction]
		public ComponentLookup<AnimalCurrentLane> m_AnimalCurrentLaneData;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public bool m_LeftHandTraffic;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_StaticObjectSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_MovingObjectSearchTree;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

		public LaneObjectCommandBuffer m_LaneObjectBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Objects.Transform> nativeArray2 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<Moving> nativeArray3 = chunk.GetNativeArray(ref m_MovingType);
			NativeArray<GroupMember> nativeArray4 = chunk.GetNativeArray(ref m_GroupMemberType);
			NativeArray<Animal> nativeArray5 = chunk.GetNativeArray(ref m_AnimalType);
			NativeArray<AnimalNavigation> nativeArray6 = chunk.GetNativeArray(ref m_NavigationType);
			NativeArray<Blocker> nativeArray7 = chunk.GetNativeArray(ref m_BlockerType);
			NativeArray<PrefabRef> nativeArray8 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<MeshGroup> bufferAccessor = chunk.GetBufferAccessor(ref m_MeshGroupType);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			if (chunk.Has(ref m_StumblingType))
			{
				for (int i = 0; i < chunk.Count; i++)
				{
					Entity entity = nativeArray[i];
					Game.Objects.Transform transform = nativeArray2[i];
					Animal animal = nativeArray5[i];
					AnimalNavigation navigation = nativeArray6[i];
					Blocker blocker = nativeArray7[i];
					PrefabRef prefabRef = nativeArray8[i];
					AnimalCurrentLane currentLane = m_AnimalCurrentLaneData[entity];
					ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
					CollectionUtils.TryGet(nativeArray3, i, out var value);
					CollectionUtils.TryGet(nativeArray4, i, out var value2);
					AnimalNavigationHelpers.CurrentLaneCache currentLaneCache = new AnimalNavigationHelpers.CurrentLaneCache(ref currentLane, m_PrefabRefData, m_MovingObjectSearchTree);
					UpdateStumbling(entity, transform, value2, animal, objectGeometryData, ref navigation, ref currentLane, ref blocker);
					currentLaneCache.CheckChanges(entity, ref currentLane, m_LaneObjectBuffer, m_LaneObjects, transform, value, navigation, objectGeometryData);
					nativeArray6[i] = navigation;
					nativeArray7[i] = blocker;
					m_AnimalCurrentLaneData[entity] = currentLane;
				}
				return;
			}
			NativeArray<TripSource> nativeArray9 = chunk.GetNativeArray(ref m_TripSourceType);
			for (int j = 0; j < chunk.Count; j++)
			{
				Entity entity2 = nativeArray[j];
				Game.Objects.Transform transform2 = nativeArray2[j];
				Moving moving = nativeArray3[j];
				Animal animal2 = nativeArray5[j];
				AnimalNavigation navigation2 = nativeArray6[j];
				Blocker blocker2 = nativeArray7[j];
				PrefabRef prefabRef2 = nativeArray8[j];
				AnimalCurrentLane currentLane2 = m_AnimalCurrentLaneData[entity2];
				CreatureData prefabCreatureData = m_PrefabCreatureData[prefabRef2.m_Prefab];
				AnimalData prefabAnimalData = m_PrefabAnimalData[prefabRef2.m_Prefab];
				ObjectGeometryData objectGeometryData2 = m_PrefabObjectGeometryData[prefabRef2.m_Prefab];
				CollectionUtils.TryGet(nativeArray4, j, out var value3);
				CollectionUtils.TryGet(nativeArray9, j, out var value4);
				CollectionUtils.TryGet(bufferAccessor, j, out var value5);
				AnimalNavigationHelpers.CurrentLaneCache currentLaneCache2 = new AnimalNavigationHelpers.CurrentLaneCache(ref currentLane2, m_PrefabRefData, m_MovingObjectSearchTree);
				if (currentLane2.m_Lane == Entity.Null || (currentLane2.m_Flags & CreatureLaneFlags.Obsolete) != 0)
				{
					TryFindCurrentLane(ref currentLane2, transform2, animal2);
				}
				UpdateNavigationTarget(ref random, entity2, transform2, moving, value4, value3, animal2, prefabRef2, prefabCreatureData, prefabAnimalData, objectGeometryData2, ref navigation2, ref currentLane2, ref blocker2, value5);
				currentLaneCache2.CheckChanges(entity2, ref currentLane2, m_LaneObjectBuffer, m_LaneObjects, transform2, moving, navigation2, objectGeometryData2);
				nativeArray6[j] = navigation2;
				nativeArray7[j] = blocker2;
				m_AnimalCurrentLaneData[entity2] = currentLane2;
			}
		}

		private void UpdateStumbling(Entity entity, Game.Objects.Transform transform, GroupMember groupMember, Animal animal, ObjectGeometryData prefabObjectGeometryData, ref AnimalNavigation navigation, ref AnimalCurrentLane currentLane, ref Blocker blocker)
		{
			TryFindCurrentLane(ref currentLane, transform, animal);
			navigation = new AnimalNavigation
			{
				m_TargetPosition = transform.m_Position
			};
			blocker = default(Blocker);
		}

		private void TryFindCurrentLane(ref AnimalCurrentLane currentLane, Game.Objects.Transform transformData, Animal animal)
		{
			currentLane.m_Flags &= ~CreatureLaneFlags.Obsolete;
			currentLane.m_Lane = Entity.Null;
			currentLane.m_NextLane = Entity.Null;
			if ((animal.m_Flags & AnimalFlags.Roaming) == 0)
			{
				bool flag = (currentLane.m_Flags & CreatureLaneFlags.EmergeUnspawned) != 0;
				currentLane.m_Flags &= ~(CreatureLaneFlags.EndOfPath | CreatureLaneFlags.EndReached | CreatureLaneFlags.TransformTarget | CreatureLaneFlags.ParkingSpace | CreatureLaneFlags.Transport | CreatureLaneFlags.Connection | CreatureLaneFlags.Taxi | CreatureLaneFlags.FindLane | CreatureLaneFlags.Area | CreatureLaneFlags.Hangaround | CreatureLaneFlags.WaitPosition | CreatureLaneFlags.EmergeUnspawned);
				float3 position = transformData.m_Position;
				Bounds3 bounds = new Bounds3(position - 100f, position + 100f);
				AnimalNavigationHelpers.FindLaneIterator iterator = new AnimalNavigationHelpers.FindLaneIterator
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
		}

		private void UpdateNavigationTarget(ref Unity.Mathematics.Random random, Entity entity, Game.Objects.Transform transform, Moving moving, TripSource tripSource, GroupMember groupMember, Animal animal, PrefabRef prefabRef, CreatureData prefabCreatureData, AnimalData prefabAnimalData, ObjectGeometryData prefabObjectGeometryData, ref AnimalNavigation navigation, ref AnimalCurrentLane currentLane, ref Blocker blocker, DynamicBuffer<MeshGroup> meshGroups)
		{
			float num = 4f / 15f;
			float num2 = math.length(moving.m_Velocity);
			if ((animal.m_Flags & AnimalFlags.SwimmingTarget) != 0)
			{
				currentLane.m_Flags |= CreatureLaneFlags.Swimming;
			}
			else
			{
				prefabAnimalData.m_SwimDepth.min = 0f;
			}
			if ((animal.m_Flags & AnimalFlags.FlyingTarget) != 0)
			{
				currentLane.m_Flags |= CreatureLaneFlags.Flying;
			}
			else
			{
				prefabAnimalData.m_FlyHeight.min = 0f;
			}
			if ((currentLane.m_Flags & CreatureLaneFlags.Connection) != 0)
			{
				prefabAnimalData.m_MoveSpeed = 277.77777f;
				prefabAnimalData.m_Acceleration = 277.77777f;
			}
			else
			{
				if ((currentLane.m_Flags & CreatureLaneFlags.Swimming) != 0)
				{
					prefabAnimalData.m_MoveSpeed = prefabAnimalData.m_SwimSpeed;
				}
				else if ((currentLane.m_Flags & CreatureLaneFlags.Flying) != 0)
				{
					prefabAnimalData.m_MoveSpeed = prefabAnimalData.m_FlySpeed;
				}
				num2 = math.min(num2, prefabAnimalData.m_MoveSpeed);
			}
			Bounds1 bounds = new Bounds1(num2 + new float2(0f - prefabAnimalData.m_Acceleration, prefabAnimalData.m_Acceleration) * num);
			float position = math.select(prefabAnimalData.m_MoveSpeed * random.NextFloat(0.9f, 1f), 0f, tripSource.m_Source != Entity.Null);
			navigation.m_MaxSpeed = MathUtils.Clamp(position, bounds);
			float num3 = math.max(prefabObjectGeometryData.m_Bounds.max.z, (prefabObjectGeometryData.m_Bounds.max.x - prefabObjectGeometryData.m_Bounds.min.x) * 0.5f);
			float currentDistance;
			if ((currentLane.m_Flags & (CreatureLaneFlags.EndReached | CreatureLaneFlags.TransformTarget | CreatureLaneFlags.Area)) != 0 || currentLane.m_Lane == Entity.Null || ((currentLane.m_Flags & CreatureLaneFlags.Connection) != 0 && (currentLane.m_Flags & (CreatureLaneFlags.ParkingSpace | CreatureLaneFlags.WaitPosition)) != 0))
			{
				if ((animal.m_Flags & AnimalFlags.Roaming) != 0 && math.distance(transform.m_Position.xz, navigation.m_TargetPosition.xz) < num3 + 1f)
				{
					if ((currentLane.m_Flags & CreatureLaneFlags.Swimming) != 0)
					{
						Bounds1 bounds2 = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, navigation.m_TargetPosition) - MathUtils.Invert(prefabAnimalData.m_SwimDepth);
						navigation.m_TargetPosition.y = MathUtils.Clamp(navigation.m_TargetPosition.y, bounds2);
					}
					else if ((currentLane.m_Flags & CreatureLaneFlags.Flying) != 0)
					{
						Bounds1 bounds3 = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, navigation.m_TargetPosition) + prefabAnimalData.m_FlyHeight;
						navigation.m_TargetPosition.y = MathUtils.Clamp(navigation.m_TargetPosition.y, bounds3);
					}
					else
					{
						navigation.m_TargetPosition.y = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, navigation.m_TargetPosition, out bool hasDepth);
						navigation.m_TargetPosition.y -= (hasDepth ? 0.2f : 0f);
					}
				}
				currentDistance = math.distance(transform.m_Position, navigation.m_TargetPosition);
				float distance = math.select(currentDistance, math.max(0f, currentDistance - num3), (currentLane.m_Flags & (CreatureLaneFlags.TransformTarget | CreatureLaneFlags.Swimming | CreatureLaneFlags.Flying)) == 0);
				float maxBrakingSpeed = CreatureUtils.GetMaxBrakingSpeed(prefabAnimalData, distance, num);
				maxBrakingSpeed = MathUtils.Clamp(maxBrakingSpeed, bounds);
				navigation.m_MaxSpeed = math.min(navigation.m_MaxSpeed, maxBrakingSpeed);
			}
			else
			{
				if ((currentLane.m_Flags & CreatureLaneFlags.WaitSignal) != 0)
				{
					navigation.m_TargetPosition = transform.m_Position;
					navigation.m_TargetDirection = default(float3);
					navigation.m_TargetActivity = 0;
					currentDistance = 0f;
					if (m_PathOwnerData.HasComponent(groupMember.m_Leader))
					{
						PathOwner pathOwner = m_PathOwnerData[groupMember.m_Leader];
						DynamicBuffer<PathElement> dynamicBuffer = m_PathElements[groupMember.m_Leader];
						if (pathOwner.m_ElementIndex < dynamicBuffer.Length)
						{
							PathElement pathElement = dynamicBuffer[pathOwner.m_ElementIndex];
							if (m_CurveData.HasComponent(pathElement.m_Target))
							{
								float lanePosition = math.select(currentLane.m_LanePosition, 0f - currentLane.m_LanePosition, (currentLane.m_Flags & CreatureLaneFlags.Backward) != 0 != pathElement.m_TargetDelta.y < pathElement.m_TargetDelta.x);
								Line3.Segment segment = CalculateTargetPos(prefabObjectGeometryData, pathElement.m_Target, pathElement.m_TargetDelta, lanePosition);
								navigation.m_TargetPosition = segment.a;
								navigation.m_TargetDirection = math.normalizesafe(segment.b - segment.a);
								currentDistance = math.distance(transform.m_Position, navigation.m_TargetPosition);
							}
						}
					}
				}
				else
				{
					navigation.m_TargetPosition = CalculateTargetPos(prefabObjectGeometryData, currentLane.m_Lane, currentLane.m_CurvePosition.x, currentLane.m_LanePosition);
					navigation.m_TargetDirection = default(float3);
					navigation.m_TargetActivity = 0;
					currentDistance = math.distance(transform.m_Position, navigation.m_TargetPosition);
				}
				float brakingDistance = CreatureUtils.GetBrakingDistance(prefabAnimalData, navigation.m_MaxSpeed, num);
				float num4 = math.max(0f, currentDistance - num3);
				if (num4 < brakingDistance)
				{
					float maxBrakingSpeed2 = CreatureUtils.GetMaxBrakingSpeed(prefabAnimalData, num4, num);
					maxBrakingSpeed2 = MathUtils.Clamp(maxBrakingSpeed2, bounds);
					navigation.m_MaxSpeed = math.min(navigation.m_MaxSpeed, maxBrakingSpeed2);
				}
			}
			navigation.m_MaxSpeed = math.select(navigation.m_MaxSpeed, 0f, navigation.m_MaxSpeed < 0.1f);
			Entity blocker2 = blocker.m_Blocker;
			float num5 = navigation.m_MaxSpeed;
			blocker.m_Blocker = Entity.Null;
			blocker.m_Type = BlockerType.None;
			currentLane.m_QueueEntity = Entity.Null;
			currentLane.m_QueueArea = default(Sphere3);
			if (m_HumanCurrentLaneData.HasComponent(groupMember.m_Leader) && m_HumanCurrentLaneData[groupMember.m_Leader].m_Lane == currentLane.m_Lane)
			{
				Game.Objects.Transform transform2 = m_TransformData[groupMember.m_Leader];
				Moving moving2 = default(Moving);
				if (m_MovingData.HasComponent(groupMember.m_Leader))
				{
					moving2 = m_MovingData[groupMember.m_Leader];
				}
				float3 @float = math.normalizesafe(navigation.m_TargetPosition - transform.m_Position);
				float3 x = transform2.m_Position - transform.m_Position;
				float num6 = math.dot(x, @float);
				if (num6 < 0f)
				{
					float num7 = MathUtils.Radians(@float, math.normalizesafe(x));
					float distance2 = math.max(0f, math.lerp(0.4f, 3f, (num7 - 1.5f) / 1.5f) - math.abs(num6));
					float maxResultSpeed = math.max(0f, math.dot(@float, moving2.m_Velocity));
					float maxBrakingSpeed3 = CreatureUtils.GetMaxBrakingSpeed(prefabAnimalData, distance2, maxResultSpeed, num);
					maxBrakingSpeed3 = MathUtils.Clamp(maxBrakingSpeed3, bounds);
					if (maxBrakingSpeed3 < navigation.m_MaxSpeed)
					{
						navigation.m_MaxSpeed = maxBrakingSpeed3;
						num5 = maxBrakingSpeed3;
						blocker.m_Blocker = groupMember.m_Leader;
						blocker.m_Type = BlockerType.Continuing;
					}
				}
			}
			float num8 = num3 + math.max(1f, navigation.m_MaxSpeed * num) + CreatureUtils.GetBrakingDistance(prefabAnimalData, navigation.m_MaxSpeed, num);
			float num9 = num3 + 1f;
			if (num2 > 0.01f && (animal.m_Flags & AnimalFlags.Roaming) == 0)
			{
				float num10 = num2 * num;
				float num11 = random.NextFloat(0f, 1f);
				num11 *= num11;
				num11 = math.select(0.5f - num11, num11 - 0.5f, m_LeftHandTraffic != ((currentLane.m_Flags & CreatureLaneFlags.Backward) != 0));
				currentLane.m_LanePosition = math.lerp(currentLane.m_LanePosition, num11, math.min(1f, num10 * 0.01f));
			}
			if (currentDistance < num8)
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
					if ((currentLane.m_Flags & (CreatureLaneFlags.EndReached | CreatureLaneFlags.WaitSignal)) == 0 && (animal.m_Flags & AnimalFlags.Roaming) == 0 && currentLane.m_Lane != Entity.Null)
					{
						if ((currentLane.m_Flags & CreatureLaneFlags.TransformTarget) != 0)
						{
							CurrentVehicle currentVehicle = default(CurrentVehicle);
							if (m_CurrentVehicleData.HasComponent(groupMember.m_Leader))
							{
								currentVehicle = m_CurrentVehicleData[groupMember.m_Leader];
							}
							if ((currentLane.m_Flags & CreatureLaneFlags.WaitPosition) != 0)
							{
								if (MoveTransformTarget(entity, prefabRef.m_Prefab, meshGroups, ref random, currentVehicle, transform.m_Position, ref navigation.m_TargetPosition, ref navigation.m_TargetDirection, ref activity, 0f, currentLane.m_Lane, prefabCreatureData.m_SupportedActivities))
								{
									navigation.m_TargetPosition = VehicleUtils.GetConnectionParkingPosition(default(Game.Net.ConnectionLane), new Bezier4x3(navigation.m_TargetPosition, navigation.m_TargetPosition, navigation.m_TargetPosition, navigation.m_TargetPosition), currentLane.m_CurvePosition.y);
									navigation.m_TargetDirection = default(float3);
									navigation.m_TargetActivity = 0;
								}
							}
							else if (MoveTransformTarget(entity, prefabRef.m_Prefab, meshGroups, ref random, currentVehicle, transform.m_Position, ref navigation.m_TargetPosition, ref navigation.m_TargetDirection, ref activity, num8, currentLane.m_Lane, prefabCreatureData.m_SupportedActivities))
							{
								break;
							}
						}
						else if ((currentLane.m_Flags & CreatureLaneFlags.Connection) != 0 && (currentLane.m_Flags & (CreatureLaneFlags.ParkingSpace | CreatureLaneFlags.WaitPosition)) != 0)
						{
							Curve curve = m_CurveData[currentLane.m_Lane];
							Game.Net.ConnectionLane connectionLane = m_ConnectionLaneData[currentLane.m_Lane];
							navigation.m_TargetPosition = VehicleUtils.GetConnectionParkingPosition(connectionLane, curve.m_Bezier, currentLane.m_CurvePosition.y);
							navigation.m_TargetDirection = default(float3);
							navigation.m_TargetActivity = 0;
						}
						else if ((currentLane.m_Flags & CreatureLaneFlags.Area) != 0)
						{
							navigation.m_TargetActivity = 0;
							float navigationSize = CreatureUtils.GetNavigationSize(prefabObjectGeometryData);
							PathOwner pathOwner2 = default(PathOwner);
							DynamicBuffer<PathElement> pathElements = default(DynamicBuffer<PathElement>);
							if (m_HumanCurrentLaneData.HasComponent(groupMember.m_Leader) && m_HumanCurrentLaneData[groupMember.m_Leader].m_Lane == currentLane.m_Lane)
							{
								pathOwner2 = m_PathOwnerData[groupMember.m_Leader];
								pathElements = m_PathElements[groupMember.m_Leader];
							}
							if (MoveAreaTarget(ref random, transform.m_Position, pathOwner2, pathElements, ref navigation.m_TargetPosition, ref navigation.m_TargetDirection, ref activity, num8, currentLane.m_Lane, currentLane.m_NextLane, prefabCreatureData.m_SupportedActivities, ref currentLane.m_CurvePosition, currentLane.m_NextPosition, currentLane.m_LanePosition, navigationSize))
							{
								break;
							}
						}
						else
						{
							Curve curve2 = m_CurveData[currentLane.m_Lane];
							PrefabRef prefabRef2 = m_PrefabRefData[currentLane.m_Lane];
							NetLaneData prefabLaneData = m_PrefabLaneData[prefabRef2.m_Prefab];
							m_NodeLaneData.TryGetComponent(currentLane.m_Lane, out var componentData);
							float laneOffset = CreatureUtils.GetLaneOffset(prefabObjectGeometryData, prefabLaneData, componentData, currentLane.m_CurvePosition.x, currentLane.m_LanePosition);
							navigation.m_TargetDirection = default(float3);
							navigation.m_TargetActivity = 0;
							if (MoveLaneTarget(ref targetIterator, currentLane.m_Lane, transform.m_Position, ref navigation.m_TargetPosition, num8, curve2.m_Bezier, ref currentLane.m_CurvePosition, laneOffset))
							{
								break;
							}
						}
					}
					float3 targetPosition = navigation.m_TargetPosition;
					if ((animal.m_Flags & AnimalFlags.FlyingTarget) != 0)
					{
						Bounds1 bounds4 = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, navigation.m_TargetPosition) + prefabAnimalData.m_FlyHeight;
						targetPosition.y = MathUtils.Clamp(navigation.m_TargetPosition.y, bounds4);
					}
					else if ((animal.m_Flags & AnimalFlags.SwimmingTarget) != 0)
					{
						Bounds1 bounds5 = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, navigation.m_TargetPosition) - MathUtils.Invert(prefabAnimalData.m_SwimDepth);
						targetPosition.y = MathUtils.Clamp(navigation.m_TargetPosition.y, bounds5);
					}
					if ((currentLane.m_Flags & CreatureLaneFlags.EndOfPath) != 0)
					{
						currentDistance = math.distance(transform.m_Position, targetPosition);
						if ((currentLane.m_Flags & CreatureLaneFlags.EndReached) == 0 && currentDistance < num9 && num2 < 0.1f)
						{
							navigation.m_TargetActivity = activity;
							navigation.m_TargetPosition = targetPosition;
							currentLane.m_Flags |= CreatureLaneFlags.EndReached;
							if ((animal.m_Flags & AnimalFlags.SwimmingTarget) == 0)
							{
								currentLane.m_Flags &= ~CreatureLaneFlags.Swimming;
							}
							if ((animal.m_Flags & AnimalFlags.FlyingTarget) == 0)
							{
								currentLane.m_Flags &= ~CreatureLaneFlags.Flying;
							}
						}
						break;
					}
					if ((animal.m_Flags & AnimalFlags.Roaming) == 0 && currentLane.m_NextLane != Entity.Null)
					{
						if (((currentLane.m_Flags ^ currentLane.m_NextFlags) & CreatureLaneFlags.Backward) != 0)
						{
							currentLane.m_LanePosition = 0f - currentLane.m_LanePosition;
						}
						currentLane.m_Lane = currentLane.m_NextLane;
						currentLane.m_Flags = currentLane.m_NextFlags;
						currentLane.m_CurvePosition = currentLane.m_NextPosition;
						currentLane.m_NextLane = Entity.Null;
						if ((currentLane.m_Flags & CreatureLaneFlags.Area) == 0 && m_CurveData.HasComponent(currentLane.m_Lane))
						{
							MathUtils.Distance(m_CurveData[currentLane.m_Lane].m_Bezier, transform.m_Position, out currentLane.m_CurvePosition.x);
						}
						continue;
					}
					if (groupMember.m_Leader != Entity.Null)
					{
						if (m_HumanCurrentLaneData.HasComponent(groupMember.m_Leader))
						{
							if ((m_HumanCurrentLaneData[groupMember.m_Leader].m_Flags & CreatureLaneFlags.WaitSignal) != 0)
							{
								currentLane.m_Flags |= CreatureLaneFlags.WaitSignal;
								if (m_PathOwnerData.HasComponent(groupMember.m_Leader))
								{
									PathOwner pathOwner3 = m_PathOwnerData[groupMember.m_Leader];
									DynamicBuffer<PathElement> dynamicBuffer2 = m_PathElements[groupMember.m_Leader];
									if (pathOwner3.m_ElementIndex < dynamicBuffer2.Length)
									{
										PathElement pathElement2 = dynamicBuffer2[pathOwner3.m_ElementIndex];
										if (m_CurveData.HasComponent(pathElement2.m_Target))
										{
											float lanePosition2 = math.select(currentLane.m_LanePosition, 0f - currentLane.m_LanePosition, (currentLane.m_Flags & CreatureLaneFlags.Backward) != 0 != pathElement2.m_TargetDelta.y < pathElement2.m_TargetDelta.x);
											Line3.Segment segment2 = CalculateTargetPos(prefabObjectGeometryData, pathElement2.m_Target, pathElement2.m_TargetDelta, lanePosition2);
											navigation.m_TargetPosition = segment2.a;
											navigation.m_TargetDirection = math.normalizesafe(segment2.b - segment2.a);
											navigation.m_TargetActivity = 0;
										}
									}
								}
							}
							else
							{
								currentDistance = math.distance(transform.m_Position, navigation.m_TargetPosition);
								if (currentDistance < num9 && num2 < 0.1f)
								{
									currentLane.m_Flags |= CreatureLaneFlags.EndReached;
								}
							}
							targetIterator.m_Blocker = groupMember.m_Leader;
							targetIterator.m_BlockerType = BlockerType.Continuing;
							break;
						}
						if (m_AnimalCurrentLaneData.HasComponent(groupMember.m_Leader) && m_AnimalData.TryGetComponent(groupMember.m_Leader, out var componentData2))
						{
							if (((animal.m_Flags ^ componentData2.m_Flags) & AnimalFlags.Roaming) != 0)
							{
								currentLane.m_Flags |= CreatureLaneFlags.EndReached;
							}
							else if ((componentData2.m_Flags & AnimalFlags.Roaming) != 0)
							{
								currentLane.m_Lane = Entity.Null;
								Game.Objects.Transform transform3 = m_TransformData[groupMember.m_Leader];
								float2 float2 = MathUtils.RotateLeft(new float2(0f, num3 * -2f), currentLane.m_LanePosition * (MathF.PI * 2f));
								float3 followPosition = transform3.m_Position + math.mul(transform3.m_Rotation, new float3(float2.x, 0f, float2.y));
								if ((currentLane.m_Flags & CreatureLaneFlags.Swimming) != 0)
								{
									Bounds1 bounds6 = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, followPosition) - MathUtils.Invert(prefabAnimalData.m_SwimDepth);
									followPosition.y = MathUtils.Clamp(followPosition.y, bounds6);
								}
								else if ((currentLane.m_Flags & CreatureLaneFlags.Flying) != 0)
								{
									CalculateFlyingTargetPos(navigation, transform, componentData2, prefabAnimalData, ref currentLane, ref followPosition, ref currentDistance);
								}
								else
								{
									followPosition.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, followPosition);
								}
								float3 float3 = followPosition - navigation.m_TargetPosition;
								if (math.length(float3) >= 0.1f)
								{
									navigation.m_TargetPosition += MathUtils.ClampLength(float3, num8);
									break;
								}
								targetIterator.m_Blocker = groupMember.m_Leader;
								targetIterator.m_BlockerType = BlockerType.Continuing;
								if (currentDistance < num9 && num2 < 0.1f)
								{
									navigation.m_TargetActivity = 0;
									if ((animal.m_Flags & AnimalFlags.SwimmingTarget) == 0)
									{
										currentLane.m_Flags &= ~CreatureLaneFlags.Swimming;
									}
									if ((animal.m_Flags & AnimalFlags.FlyingTarget) == 0 && (currentLane.m_Flags & CreatureLaneFlags.EndOfPath) != 0)
									{
										currentLane.m_Flags &= ~CreatureLaneFlags.Flying;
										currentLane.m_Flags |= CreatureLaneFlags.EndReached;
									}
								}
							}
							else
							{
								currentDistance = math.distance(transform.m_Position, navigation.m_TargetPosition);
								if (currentDistance < num9 && num2 < 0.1f)
								{
									currentLane.m_Flags |= CreatureLaneFlags.EndReached;
								}
								targetIterator.m_Blocker = groupMember.m_Leader;
								targetIterator.m_BlockerType = BlockerType.Continuing;
							}
							break;
						}
						if (m_CurrentVehicleData.HasComponent(groupMember.m_Leader))
						{
							currentLane.m_Lane = m_CurrentVehicleData[groupMember.m_Leader].m_Vehicle;
							currentLane.m_CurvePosition = 0f;
							currentLane.m_Flags = CreatureLaneFlags.EndOfPath | CreatureLaneFlags.TransformTarget;
							continue;
						}
					}
					if (tripSource.m_Source != Entity.Null)
					{
						break;
					}
					currentLane.m_Flags |= CreatureLaneFlags.EndOfPath;
				}
				blocker.m_Blocker = targetIterator.m_Blocker;
				blocker.m_Type = targetIterator.m_BlockerType;
			}
			if (navigation.m_TargetActivity == 0)
			{
				if ((currentLane.m_Flags & CreatureLaneFlags.Swimming) != 0)
				{
					navigation.m_TargetActivity = 8;
				}
				else if ((currentLane.m_Flags & CreatureLaneFlags.Flying) != 0)
				{
					navigation.m_TargetActivity = 9;
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
					m_TimeStep = num,
					m_PrefabObjectGeometry = prefabObjectGeometryData,
					m_SpeedRange = bounds,
					m_CurrentPosition = transform.m_Position,
					m_CurrentDirection = math.forward(transform.m_Rotation),
					m_CurrentVelocity = moving.m_Velocity,
					m_TargetDistance = num8,
					m_MinSpeed = random.NextFloat(0.4f, 0.6f),
					m_TargetPosition = navigation.m_TargetPosition,
					m_MaxSpeed = navigation.m_MaxSpeed,
					m_LanePosition = currentLane.m_LanePosition,
					m_Blocker = blocker.m_Blocker,
					m_BlockerType = blocker.m_Type,
					m_QueueEntity = currentLane.m_QueueEntity,
					m_QueueArea = currentLane.m_QueueArea
				};
				if (blocker2 != Entity.Null)
				{
					creatureCollisionIterator.IterateBlocker(prefabAnimalData, blocker2);
					creatureCollisionIterator.m_MaxSpeed = math.select(creatureCollisionIterator.m_MaxSpeed, 0f, creatureCollisionIterator.m_MaxSpeed < 0.1f);
				}
				if (creatureCollisionIterator.m_MaxSpeed != 0f && (currentLane.m_Flags & CreatureLaneFlags.Connection) == 0)
				{
					bool isBackward = (currentLane.m_Flags & CreatureLaneFlags.Backward) != 0;
					if ((currentLane.m_Flags & CreatureLaneFlags.WaitSignal) != 0)
					{
						if (m_PathOwnerData.HasComponent(groupMember.m_Leader))
						{
							PathOwner pathOwner4 = m_PathOwnerData[groupMember.m_Leader];
							DynamicBuffer<PathElement> dynamicBuffer3 = m_PathElements[groupMember.m_Leader];
							int elementIndex = pathOwner4.m_ElementIndex;
							if (elementIndex < dynamicBuffer3.Length)
							{
								PathElement pathElement3 = dynamicBuffer3[elementIndex++];
								if (m_CurveData.HasComponent(pathElement3.m_Target) && creatureCollisionIterator.IterateFirstLane(currentLane.m_Lane, pathElement3.m_Target, currentLane.m_CurvePosition, pathElement3.m_TargetDelta, isBackward))
								{
									while (creatureCollisionIterator.IterateNextLane(pathElement3.m_Target, pathElement3.m_TargetDelta) && elementIndex < dynamicBuffer3.Length)
									{
										pathElement3 = dynamicBuffer3[elementIndex++];
									}
								}
							}
						}
					}
					else if (creatureCollisionIterator.IterateFirstLane(currentLane.m_Lane, currentLane.m_CurvePosition, isBackward) && m_PathOwnerData.HasComponent(groupMember.m_Leader))
					{
						PathOwner pathOwner5 = m_PathOwnerData[groupMember.m_Leader];
						DynamicBuffer<PathElement> dynamicBuffer4 = m_PathElements[groupMember.m_Leader];
						int elementIndex2 = pathOwner5.m_ElementIndex;
						if (elementIndex2 < dynamicBuffer4.Length)
						{
							PathElement pathElement4 = dynamicBuffer4[elementIndex2++];
							while (creatureCollisionIterator.IterateNextLane(pathElement4.m_Target, pathElement4.m_TargetDelta) && elementIndex2 < dynamicBuffer4.Length)
							{
								pathElement4 = dynamicBuffer4[elementIndex2++];
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
				num5 = creatureCollisionIterator.m_MaxSpeed;
			}
			blocker.m_MaxSpeed = (byte)math.clamp(Mathf.RoundToInt(num5 * 45.899998f), 0, 255);
		}

		private float3 CalculateTargetPos(ObjectGeometryData prefabObjectGeometryData, Entity lane, float curvePosition, float lanePosition)
		{
			Curve curve = m_CurveData[lane];
			PrefabRef prefabRef = m_PrefabRefData[lane];
			NetLaneData prefabLaneData = m_PrefabLaneData[prefabRef.m_Prefab];
			m_NodeLaneData.TryGetComponent(lane, out var componentData);
			return CreatureUtils.GetLanePosition(laneOffset: CreatureUtils.GetLaneOffset(prefabObjectGeometryData, prefabLaneData, componentData, curvePosition, lanePosition), curve: curve.m_Bezier, curvePosition: curvePosition);
		}

		private void CalculateFlyingTargetPos(AnimalNavigation navigation, Game.Objects.Transform transform, Animal leaderAnimal, AnimalData prefabAnimalData, ref AnimalCurrentLane currentLane, ref float3 followPosition, ref float currentDistance)
		{
			if ((leaderAnimal.m_Flags & AnimalFlags.FlyingTarget) != 0)
			{
				currentLane.m_Flags &= (CreatureLaneFlags)4294967295u;
				Bounds1 bounds = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, followPosition) + prefabAnimalData.m_FlyHeight;
				followPosition.y = MathUtils.Clamp(followPosition.y, bounds);
			}
			else if ((leaderAnimal.m_Flags & AnimalFlags.FlyingTarget) == 0 && (currentLane.m_Flags & CreatureLaneFlags.EndOfPath) == 0)
			{
				currentLane.m_Flags |= CreatureLaneFlags.EndOfPath;
				float3 @float = math.mul(transform.m_Rotation, prefabAnimalData.m_LandingOffset);
				navigation.m_TargetPosition.y = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, navigation.m_TargetPosition - @float, out bool hasDepth);
				navigation.m_TargetPosition.y += prefabAnimalData.m_LandingOffset.y - (hasDepth ? 0.2f : 0f);
				currentDistance = math.distance(transform.m_Position, navigation.m_TargetPosition);
				followPosition = navigation.m_TargetPosition;
			}
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

		private bool MoveAreaTarget(ref Unity.Mathematics.Random random, float3 comparePosition, PathOwner pathOwner, DynamicBuffer<PathElement> pathElements, ref float3 targetPosition, ref float3 targetDirection, ref byte activity, float minDistance, Entity target, Entity nextTarget, ActivityMask activityMask, ref float2 curveDelta, float2 nextCurveDelta, float lanePosition, float navigationSize)
		{
			if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Obsolete | PathFlags.Updated)) != 0)
			{
				return true;
			}
			Entity owner = m_OwnerData[target].m_Owner;
			AreaLane areaLane = m_AreaLaneData[target];
			DynamicBuffer<Game.Areas.Node> nodes = m_AreaNodes[owner];
			bool flag = curveDelta.y < curveDelta.x;
			PathElement nextElement = new PathElement(nextTarget, nextCurveDelta);
			targetDirection = default(float3);
			activity = 0;
			if (areaLane.m_Nodes.y == areaLane.m_Nodes.z)
			{
				float3 position = nodes[areaLane.m_Nodes.x].m_Position;
				float3 position2 = nodes[areaLane.m_Nodes.y].m_Position;
				float3 position3 = nodes[areaLane.m_Nodes.w].m_Position;
				if (CreatureUtils.SetTriangleTarget(position, position2, position3, comparePosition, nextElement, pathOwner.m_ElementIndex, pathElements, ref targetPosition, minDistance, lanePosition, curveDelta.y, navigationSize, isSingle: true, m_TransformData, m_TaxiStandData, m_AreaLaneData, m_CurveData))
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
					if (CreatureUtils.SetAreaTarget(position4, position4, position5, position6, position7, owner, nodes, comparePosition, nextElement, pathOwner.m_ElementIndex, pathElements, ref targetPosition, minDistance, lanePosition, curveDelta.y, navigationSize, flag, m_TransformData, m_TaxiStandData, m_AreaLaneData, m_CurveData, m_OwnerData))
					{
						return true;
					}
					curveDelta.x = 0.5f;
					@bool.xz = false;
				}
				if (nextElement.m_Target == Entity.Null && pathElements.IsCreated && pathOwner.m_ElementIndex < pathElements.Length)
				{
					nextElement = pathElements[pathOwner.m_ElementIndex++];
				}
				if (nextElement.m_Target != Entity.Null && m_OwnerData.TryGetComponent(nextElement.m_Target, out var componentData) && componentData.m_Owner == owner)
				{
					bool4 bool2 = new bool4(nextElement.m_TargetDelta < 0.5f, nextElement.m_TargetDelta > 0.5f);
					if (math.any(!@bool.xz) & math.any(@bool.yw) & math.any(bool2.xy & bool2.wz))
					{
						AreaLane areaLane2 = m_AreaLaneData[nextElement.m_Target];
						bool flag2 = nextElement.m_TargetDelta.y < nextElement.m_TargetDelta.x;
						lanePosition = math.select(lanePosition, 0f - lanePosition, flag2 != flag);
						@int = math.select(areaLane2.m_Nodes.x, areaLane2.m_Nodes.w, bool2.zw);
						position4 = nodes[@int.x].m_Position;
						if (CreatureUtils.SetAreaTarget(math.select(position5, position6, position4.Equals(position5)), left: nodes[areaLane2.m_Nodes.y].m_Position, right: nodes[areaLane2.m_Nodes.z].m_Position, next: nodes[@int.y].m_Position, prev: position4, areaEntity: owner, nodes: nodes, comparePosition: comparePosition, nextElement: default(PathElement), elementIndex: pathOwner.m_ElementIndex, pathElements: pathElements, targetPosition: ref targetPosition, minDistance: minDistance, lanePosition: lanePosition, curveDelta: nextElement.m_TargetDelta.y, navigationSize: navigationSize, isBackward: flag2, transforms: m_TransformData, taxiStands: m_TaxiStandData, areaLanes: m_AreaLaneData, curves: m_CurveData, owners: m_OwnerData))
						{
							return true;
						}
					}
					curveDelta.x = curveDelta.y;
					return false;
				}
				if (CreatureUtils.SetTriangleTarget(position5, position6, position7, comparePosition, nextElement, pathOwner.m_ElementIndex, pathElements, ref targetPosition, minDistance, lanePosition, curveDelta.y, navigationSize, isSingle: false, m_TransformData, m_TaxiStandData, m_AreaLaneData, m_CurveData))
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

		private bool MoveTransformTarget(Entity creature, Entity creaturePrefab, DynamicBuffer<MeshGroup> meshGroups, ref Unity.Mathematics.Random random, CurrentVehicle currentVehicle, float3 comparePosition, ref float3 targetPosition, ref float3 targetDirection, ref byte activity, float minDistance, Entity target, ActivityMask activityMask)
		{
			Game.Objects.Transform result = new Game.Objects.Transform
			{
				m_Position = targetPosition
			};
			ActivityType activity2 = ActivityType.None;
			m_PseudoRandomSeedData.TryGetComponent(creature, out var componentData);
			if (CreatureUtils.CalculateTransformPosition(creature, creaturePrefab, meshGroups, ref random, ref result, ref activity2, currentVehicle, componentData, target, m_LeftHandTraffic, activityMask, (ActivityCondition)0u, m_MovingObjectSearchTree, ref m_TransformData, ref m_PositionData, ref m_PublicTransportData, ref m_TrainData, ref m_ControllerData, ref m_PrefabRefData, ref m_PrefabBuildingData, ref m_PrefabCarData, ref m_PrefabActivityLocations, ref m_SubMeshGroups, ref m_CharacterElements, ref m_SubMeshes, ref m_AnimationClips, ref m_AnimationMotions))
			{
				targetPosition = result.m_Position;
				if (result.m_Rotation.Equals(default(quaternion)))
				{
					targetDirection = default(float3);
				}
				else
				{
					targetDirection = math.forward(result.m_Rotation);
				}
				activity = (byte)activity2;
				return math.distance(comparePosition, targetPosition) >= minDistance;
			}
			return false;
		}

		private bool GetTransformTarget(ref Entity entity, Game.Objects.Transform transform, Entity target, Entity prevLane, float prevCurvePosition, float prevLanePosition, ObjectGeometryData prefabObjectGeometryData)
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
		public ComponentTypeHandle<Animal> __Game_Creatures_Animal_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<MeshGroup> __Game_Rendering_MeshGroup_RO_BufferTypeHandle;

		public ComponentTypeHandle<AnimalNavigation> __Game_Creatures_AnimalNavigation_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Blocker> __Game_Vehicles_Blocker_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HumanCurrentLane> __Game_Creatures_HumanCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LaneReservation> __Game_Net_LaneReservation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaLane> __Game_Net_AreaLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NodeLane> __Game_Net_NodeLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Position> __Game_Routes_Position_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TaxiStand> __Game_Routes_TaxiStand_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Moving> __Game_Objects_Moving_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Creature> __Game_Creatures_Creature_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Animal> __Game_Creatures_Animal_RO_ComponentLookup;

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
		public ComponentLookup<PathOwner> __Game_Pathfind_PathOwner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CreatureData> __Game_Prefabs_CreatureData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AnimalData> __Game_Prefabs_AnimalData_RO_ComponentLookup;

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
		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RO_BufferLookup;

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

		public ComponentLookup<AnimalCurrentLane> __Game_Creatures_AnimalCurrentLane_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_Moving_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Moving>(isReadOnly: true);
			__Game_Creatures_GroupMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<GroupMember>(isReadOnly: true);
			__Game_Creatures_Stumbling_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Stumbling>(isReadOnly: true);
			__Game_Objects_TripSource_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TripSource>(isReadOnly: true);
			__Game_Creatures_Animal_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Animal>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Rendering_MeshGroup_RO_BufferTypeHandle = state.GetBufferTypeHandle<MeshGroup>(isReadOnly: true);
			__Game_Creatures_AnimalNavigation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AnimalNavigation>();
			__Game_Vehicles_Blocker_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Blocker>();
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentLookup = state.GetComponentLookup<PseudoRandomSeed>(isReadOnly: true);
			__Game_Creatures_HumanCurrentLane_RO_ComponentLookup = state.GetComponentLookup<HumanCurrentLane>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentLookup = state.GetComponentLookup<CurrentVehicle>(isReadOnly: true);
			__Game_Net_PedestrianLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.PedestrianLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_LaneReservation_RO_ComponentLookup = state.GetComponentLookup<LaneReservation>(isReadOnly: true);
			__Game_Net_AreaLane_RO_ComponentLookup = state.GetComponentLookup<AreaLane>(isReadOnly: true);
			__Game_Net_NodeLane_RO_ComponentLookup = state.GetComponentLookup<NodeLane>(isReadOnly: true);
			__Game_Routes_Position_RO_ComponentLookup = state.GetComponentLookup<Position>(isReadOnly: true);
			__Game_Routes_TaxiStand_RO_ComponentLookup = state.GetComponentLookup<TaxiStand>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_Moving_RO_ComponentLookup = state.GetComponentLookup<Moving>(isReadOnly: true);
			__Game_Creatures_Creature_RO_ComponentLookup = state.GetComponentLookup<Creature>(isReadOnly: true);
			__Game_Creatures_Animal_RO_ComponentLookup = state.GetComponentLookup<Animal>(isReadOnly: true);
			__Game_Creatures_GroupMember_RO_ComponentLookup = state.GetComponentLookup<GroupMember>(isReadOnly: true);
			__Game_Areas_HangaroundLocation_RO_ComponentLookup = state.GetComponentLookup<HangaroundLocation>(isReadOnly: true);
			__Game_Vehicles_PublicTransport_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PublicTransport>(isReadOnly: true);
			__Game_Vehicles_Train_RO_ComponentLookup = state.GetComponentLookup<Train>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
			__Game_Pathfind_PathOwner_RO_ComponentLookup = state.GetComponentLookup<PathOwner>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_CreatureData_RO_ComponentLookup = state.GetComponentLookup<CreatureData>(isReadOnly: true);
			__Game_Prefabs_AnimalData_RO_ComponentLookup = state.GetComponentLookup<AnimalData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
			__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
			__Game_Net_LaneOverlap_RO_BufferLookup = state.GetBufferLookup<LaneOverlap>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferLookup = state.GetBufferLookup<PathElement>(isReadOnly: true);
			__Game_Prefabs_ActivityLocationElement_RO_BufferLookup = state.GetBufferLookup<ActivityLocationElement>(isReadOnly: true);
			__Game_Prefabs_SubMeshGroup_RO_BufferLookup = state.GetBufferLookup<SubMeshGroup>(isReadOnly: true);
			__Game_Prefabs_CharacterElement_RO_BufferLookup = state.GetBufferLookup<CharacterElement>(isReadOnly: true);
			__Game_Prefabs_AnimationClip_RO_BufferLookup = state.GetBufferLookup<AnimationClip>(isReadOnly: true);
			__Game_Prefabs_AnimationMotion_RO_BufferLookup = state.GetBufferLookup<AnimationMotion>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = state.GetBufferLookup<SubMesh>(isReadOnly: true);
			__Game_Creatures_AnimalCurrentLane_RW_ComponentLookup = state.GetComponentLookup<AnimalCurrentLane>();
		}
	}

	private SimulationSystem m_SimulationSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private Game.Areas.SearchSystem m_AreaSearchSystem;

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

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
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_Actions = base.World.GetOrCreateSystemManaged<Actions>();
		m_CreatureQuery = GetEntityQuery(ComponentType.ReadOnly<Animal>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<AnimalCurrentLane>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint num = m_SimulationSystem.frameIndex % 16;
		if (num == 5 || num == 9 || num == 13)
		{
			m_CreatureQuery.ResetFilter();
			m_CreatureQuery.SetSharedComponentFilter(new UpdateFrame(num));
			JobHandle deps;
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
				m_AnimalType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Animal_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_MeshGroupType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Rendering_MeshGroup_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_NavigationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_AnimalNavigation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BlockerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Blocker_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HumanCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_HumanCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurrentVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PedestrianLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LaneReservationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneReservation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AreaLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_AreaLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NodeLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Position_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TaxiStandData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_TaxiStand_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PropertyRenterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_MovingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Moving_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CreatureData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Creature_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AnimalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Animal_RO_ComponentLookup, ref base.CheckedStateRef),
				m_GroupMemberData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_GroupMember_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HangaroundLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_HangaroundLocation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PublicTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PublicTransport_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Train_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PathOwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathOwner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCreatureData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CreatureData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabAnimalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AnimalData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabSpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Lanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
				m_LaneOverlaps = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneOverlap_RO_BufferLookup, ref base.CheckedStateRef),
				m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
				m_AreaTriangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
				m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabActivityLocations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ActivityLocationElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubMeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
				m_CharacterElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_CharacterElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_AnimationClips = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AnimationClip_RO_BufferLookup, ref base.CheckedStateRef),
				m_AnimationMotions = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AnimationMotion_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
				m_AnimalCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_AnimalCurrentLane_RW_ComponentLookup, ref base.CheckedStateRef),
				m_RandomSeed = RandomSeed.Next(),
				m_LeftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
				m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
				m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
				m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
				m_StaticObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies2),
				m_MovingObjectSearchTree = m_ObjectSearchSystem.GetMovingSearchTree(readOnly: true, out dependencies3),
				m_AreaSearchTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies4),
				m_LaneObjectBuffer = m_Actions.m_LaneObjectUpdater.Begin(Allocator.TempJob)
			}, m_CreatureQuery, JobUtils.CombineDependencies(base.Dependency, dependencies, dependencies2, dependencies3, dependencies4, deps));
			m_TerrainSystem.AddCPUHeightReader(jobHandle);
			m_WaterSystem.AddSurfaceReader(jobHandle);
			m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
			m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
			m_ObjectSearchSystem.AddMovingSearchTreeReader(jobHandle);
			m_AreaSearchSystem.AddSearchTreeReader(jobHandle);
			m_Actions.m_Dependency = jobHandle;
			base.Dependency = jobHandle;
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
	public AnimalNavigationSystem()
	{
	}
}
