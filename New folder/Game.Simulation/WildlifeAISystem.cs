using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Creatures;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
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
public class WildlifeAISystem : GameSystemBase
{
	[BurstCompile]
	private struct WildlifeGroupTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> m_UnspawnedType;

		[ReadOnly]
		public ComponentTypeHandle<GroupMember> m_GroupMemberType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<AnimalCurrentLane> m_CurrentLaneType;

		public ComponentTypeHandle<AnimalNavigation> m_AnimalNavigationType;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Animal> m_AnimalData;

		[ReadOnly]
		public ComponentLookup<AnimalData> m_PrefabAnimalData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<GroupMember> nativeArray2 = chunk.GetNativeArray(ref m_GroupMemberType);
			NativeArray<AnimalCurrentLane> nativeArray3 = chunk.GetNativeArray(ref m_CurrentLaneType);
			NativeArray<AnimalNavigation> nativeArray4 = chunk.GetNativeArray(ref m_AnimalNavigationType);
			NativeArray<PrefabRef> nativeArray5 = chunk.GetNativeArray(ref m_PrefabRefType);
			bool isUnspawned = chunk.Has(ref m_UnspawnedType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				GroupMember groupMember = nativeArray2[i];
				PrefabRef prefabRef = nativeArray5[i];
				AnimalNavigation navigation = nativeArray4[i];
				if (!CollectionUtils.TryGet(nativeArray3, i, out var value))
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(AnimalCurrentLane));
				}
				Animal animal = m_AnimalData[entity];
				CreatureUtils.CheckUnspawned(unfilteredChunkIndex, entity, value, animal, isUnspawned, m_CommandBuffer);
				TickGroupMemberWalking(unfilteredChunkIndex, entity, groupMember, prefabRef, ref animal, ref value, ref navigation);
				m_AnimalData[entity] = animal;
				CollectionUtils.TrySet(nativeArray3, i, value);
				CollectionUtils.TrySet(nativeArray4, i, navigation);
			}
		}

		private void TickGroupMemberWalking(int jobIndex, Entity entity, GroupMember groupMember, PrefabRef prefabRef, ref Animal animal, ref AnimalCurrentLane currentLane, ref AnimalNavigation navigation)
		{
			if (CreatureUtils.IsStuck(currentLane))
			{
				m_CommandBuffer.AddComponent(jobIndex, entity, default(Deleted));
			}
			else if (m_AnimalData.HasComponent(groupMember.m_Leader))
			{
				Animal animal2 = m_AnimalData[groupMember.m_Leader];
				animal.m_Flags = (AnimalFlags)(((uint)animal.m_Flags & 0xFFFFFFF9u) | (uint)(animal2.m_Flags & (AnimalFlags.SwimmingTarget | AnimalFlags.FlyingTarget)));
				if ((animal.m_Flags & AnimalFlags.FlyingTarget) != 0)
				{
					currentLane.m_Flags = (CreatureLaneFlags)((uint)currentLane.m_Flags & 0xFFFFFFFEu & 0xFFFFFFFDu);
				}
				if ((animal.m_Flags & AnimalFlags.SwimmingTarget) != 0)
				{
					currentLane.m_Flags = (CreatureLaneFlags)((uint)currentLane.m_Flags & 0xFFFFFFFEu & 0xFFFFFFFDu);
				}
				if ((currentLane.m_Flags & CreatureLaneFlags.EndOfPath) == 0 && (currentLane.m_Flags & CreatureLaneFlags.Flying) != 0 && (animal2.m_Flags & AnimalFlags.FlyingTarget) == 0)
				{
					currentLane.m_Flags |= CreatureLaneFlags.EndOfPath;
					Game.Objects.Transform selfPosition = m_TransformData[groupMember.m_Leader];
					Game.Objects.Transform transform = m_TransformData[groupMember.m_Leader];
					ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
					AnimalData animalData = m_PrefabAnimalData[prefabRef.m_Prefab];
					float num = math.max(objectGeometryData.m_Bounds.max.z, (objectGeometryData.m_Bounds.max.x - objectGeometryData.m_Bounds.min.x) * 0.5f);
					float2 @float = MathUtils.RotateLeft(new float2(0f, num * -2f), currentLane.m_LanePosition * (MathF.PI * 2f));
					float3 float2 = transform.m_Position + math.mul(transform.m_Rotation, new float3(@float.x, 0f, @float.y));
					float2.y = selfPosition.m_Position.y;
					selfPosition.m_Rotation = quaternion.LookRotation(float2 - selfPosition.m_Position, math.up());
					SetLandingPosition(ref m_WaterSurfaceData, ref m_TerrainHeightData, animalData.m_LandingOffset, selfPosition, float2, ref navigation);
				}
				if (((animal.m_Flags ^ animal2.m_Flags) & AnimalFlags.Roaming) != 0 && (currentLane.m_Flags & CreatureLaneFlags.EndReached) != 0)
				{
					animal.m_Flags = (AnimalFlags)(((uint)animal.m_Flags & 0xFFFFFFFEu) | (uint)(animal2.m_Flags & AnimalFlags.Roaming));
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct WildlifeTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> m_UnspawnedType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<Animal> m_AnimalType;

		public ComponentTypeHandle<Game.Creatures.Wildlife> m_WildlifeType;

		public ComponentTypeHandle<AnimalCurrentLane> m_CurrentLaneType;

		public ComponentTypeHandle<AnimalNavigation> m_NavigationType;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<AnimalData> m_PrefabAnimalData;

		[ReadOnly]
		public ComponentLookup<WildlifeData> m_PrefabWildlifeData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Animal> nativeArray3 = chunk.GetNativeArray(ref m_AnimalType);
			NativeArray<Game.Creatures.Wildlife> nativeArray4 = chunk.GetNativeArray(ref m_WildlifeType);
			NativeArray<AnimalCurrentLane> nativeArray5 = chunk.GetNativeArray(ref m_CurrentLaneType);
			NativeArray<AnimalNavigation> nativeArray6 = chunk.GetNativeArray(ref m_NavigationType);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			bool isUnspawned = chunk.Has(ref m_UnspawnedType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				Animal animal = nativeArray3[i];
				Game.Creatures.Wildlife wildlife = nativeArray4[i];
				AnimalNavigation navigation = nativeArray6[i];
				if (!CollectionUtils.TryGet(nativeArray5, i, out var value))
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(AnimalCurrentLane));
				}
				CreatureUtils.CheckUnspawned(unfilteredChunkIndex, entity, value, animal, isUnspawned, m_CommandBuffer);
				TickWalking(unfilteredChunkIndex, entity, prefabRef, ref random, ref animal, ref wildlife, ref value, ref navigation);
				nativeArray3[i] = animal;
				nativeArray4[i] = wildlife;
				nativeArray6[i] = navigation;
				CollectionUtils.TrySet(nativeArray5, i, value);
			}
		}

		private void TickWalking(int jobIndex, Entity entity, PrefabRef prefabRef, ref Unity.Mathematics.Random random, ref Animal animal, ref Game.Creatures.Wildlife wildlife, ref AnimalCurrentLane currentLane, ref AnimalNavigation navigation)
		{
			if (CreatureUtils.IsStuck(currentLane))
			{
				m_CommandBuffer.AddComponent(jobIndex, entity, default(Deleted));
				return;
			}
			if (currentLane.m_Lane != Entity.Null)
			{
				animal.m_Flags &= ~AnimalFlags.Roaming;
			}
			else
			{
				animal.m_Flags |= AnimalFlags.Roaming;
			}
			if (CreatureUtils.PathEndReached(currentLane))
			{
				PathEndReached(jobIndex, entity, prefabRef, ref random, ref animal, ref wildlife, ref currentLane, ref navigation);
			}
		}

		private bool PathEndReached(int jobIndex, Entity entity, PrefabRef prefabRef, ref Unity.Mathematics.Random random, ref Animal animal, ref Game.Creatures.Wildlife wildlife, ref AnimalCurrentLane currentLane, ref AnimalNavigation navigation)
		{
			AnimalData animalData = m_PrefabAnimalData[prefabRef.m_Prefab];
			WildlifeData wildlifeData = m_PrefabWildlifeData[prefabRef.m_Prefab];
			if ((wildlife.m_Flags & WildlifeFlags.Idling) != WildlifeFlags.None)
			{
				if (--wildlife.m_StateTime > 0)
				{
					return false;
				}
				wildlife.m_Flags &= ~WildlifeFlags.Idling;
				wildlife.m_Flags |= WildlifeFlags.Wandering;
			}
			else if ((wildlife.m_Flags & WildlifeFlags.Wandering) != WildlifeFlags.None)
			{
				if ((animal.m_Flags & AnimalFlags.FlyingTarget) == 0)
				{
					float num = 3.75f;
					int min = Mathf.RoundToInt(wildlifeData.m_IdleTime.min * num);
					int num2 = Mathf.RoundToInt(wildlifeData.m_IdleTime.max * num);
					wildlife.m_StateTime = (ushort)math.clamp(random.NextInt(min, num2 + 1), 0, 65535);
					if (wildlife.m_StateTime > 0)
					{
						wildlife.m_Flags &= ~WildlifeFlags.Wandering;
						wildlife.m_Flags |= WildlifeFlags.Idling;
						return false;
					}
				}
			}
			else
			{
				wildlife.m_Flags |= WildlifeFlags.Wandering;
			}
			if (m_OwnerData.HasComponent(entity))
			{
				Owner owner = m_OwnerData[entity];
				if (m_TransformData.HasComponent(owner.m_Owner))
				{
					Game.Objects.Transform selfPosition = m_TransformData[entity];
					Game.Objects.Transform transform = m_TransformData[owner.m_Owner];
					bool hasDepth = WaterUtils.SampleDepth(ref m_WaterSurfaceData, selfPosition.m_Position) > 0f;
					bool usingPrimaryTravelMethod;
					bool num3 = SetAnimalFlags(animalData, hasDepth, ref animal, ref random, out usingPrimaryTravelMethod);
					currentLane.m_Flags &= ~(CreatureLaneFlags.EndOfPath | CreatureLaneFlags.EndReached);
					if (num3)
					{
						SetLandingPosition(ref m_WaterSurfaceData, ref m_TerrainHeightData, animalData.m_LandingOffset, selfPosition, selfPosition.m_Position, ref navigation);
						return false;
					}
					float3 targetPosition = math.lerp(selfPosition.m_Position, transform.m_Position, 0.25f);
					GetTripLength(animalData, animal.m_Flags, usingPrimaryTravelMethod, wildlifeData, hasDepth, out var tripMinDistance, out var tripMaxDistance);
					targetPosition.xz += random.NextFloat2Direction() * random.NextFloat(tripMinDistance, tripMaxDistance);
					if ((animal.m_Flags & AnimalFlags.SwimmingTarget) != 0)
					{
						Bounds1 bounds = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, navigation.m_TargetPosition) - MathUtils.Invert(animalData.m_SwimDepth);
						navigation.m_TargetPosition = targetPosition;
						navigation.m_TargetPosition.y = random.NextFloat(bounds.min, bounds.max);
					}
					else if ((animal.m_Flags & AnimalFlags.FlyingTarget) != 0)
					{
						Bounds1 bounds2 = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, navigation.m_TargetPosition) + animalData.m_FlyHeight;
						navigation.m_TargetPosition = targetPosition;
						navigation.m_TargetPosition.y = random.NextFloat(bounds2.min, bounds2.max);
					}
					else if (tripMinDistance > 0f)
					{
						navigation.m_TargetPosition = targetPosition;
						navigation.m_TargetPosition.y = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, navigation.m_TargetPosition, out hasDepth) - (hasDepth ? 0.2f : 0f);
					}
					else
					{
						navigation.m_TargetPosition = selfPosition.m_Position;
						navigation.m_TargetPosition.y = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, selfPosition.m_Position, out hasDepth) - (hasDepth ? 0.2f : 0f);
					}
					return false;
				}
			}
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Deleted));
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
		public ComponentTypeHandle<Unspawned> __Game_Objects_Unspawned_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Animal> __Game_Creatures_Animal_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Creatures.Wildlife> __Game_Creatures_Wildlife_RW_ComponentTypeHandle;

		public ComponentTypeHandle<AnimalCurrentLane> __Game_Creatures_AnimalCurrentLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<AnimalNavigation> __Game_Creatures_AnimalNavigation_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AnimalData> __Game_Prefabs_AnimalData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WildlifeData> __Game_Prefabs_WildlifeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<GroupMember> __Game_Creatures_GroupMember_RO_ComponentTypeHandle;

		public ComponentLookup<Animal> __Game_Creatures_Animal_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Unspawned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Unspawned>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Creatures_Animal_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Animal>();
			__Game_Creatures_Wildlife_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Creatures.Wildlife>();
			__Game_Creatures_AnimalCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AnimalCurrentLane>();
			__Game_Creatures_AnimalNavigation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AnimalNavigation>();
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Prefabs_AnimalData_RO_ComponentLookup = state.GetComponentLookup<AnimalData>(isReadOnly: true);
			__Game_Prefabs_WildlifeData_RO_ComponentLookup = state.GetComponentLookup<WildlifeData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Creatures_GroupMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<GroupMember>(isReadOnly: true);
			__Game_Creatures_Animal_RW_ComponentLookup = state.GetComponentLookup<Animal>();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private EntityQuery m_CreatureQuery;

	private EntityQuery m_GroupCreatureQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 13;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_CreatureQuery = GetEntityQuery(ComponentType.ReadWrite<Game.Creatures.Wildlife>(), ComponentType.ReadWrite<Animal>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<GroupMember>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Stumbling>());
		m_GroupCreatureQuery = GetEntityQuery(ComponentType.ReadWrite<Game.Creatures.Wildlife>(), ComponentType.ReadWrite<Animal>(), ComponentType.ReadOnly<GroupMember>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Stumbling>());
		RequireAnyForUpdate(m_CreatureQuery, m_GroupCreatureQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle deps;
		WaterSurfaceData<SurfaceWater> surfaceData = m_WaterSystem.GetSurfaceData(out deps);
		WildlifeTickJob jobData = new WildlifeTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AnimalType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Animal_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WildlifeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Wildlife_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_AnimalCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NavigationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_AnimalNavigation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabAnimalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AnimalData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabWildlifeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WildlifeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_WaterSurfaceData = surfaceData,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		WildlifeGroupTickJob jobData2 = new WildlifeGroupTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_GroupMemberType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_GroupMember_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_AnimalCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AnimalNavigationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_AnimalNavigation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AnimalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Animal_RW_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabAnimalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AnimalData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_WaterSurfaceData = surfaceData,
			m_CommandBuffer = jobData.m_CommandBuffer
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_CreatureQuery, JobHandle.CombineDependencies(base.Dependency, deps));
		JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(jobData2, m_GroupCreatureQuery, jobHandle);
		m_TerrainSystem.AddCPUHeightReader(jobHandle);
		m_WaterSystem.AddSurfaceReader(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
		base.Dependency = jobHandle2;
	}

	private static bool SetAnimalFlags(AnimalData animalData, bool inWater, ref Animal animal, ref Unity.Mathematics.Random random, out bool usingPrimaryTravelMethod)
	{
		if ((animal.m_Flags & AnimalFlags.FlyingTarget) != 0)
		{
			usingPrimaryTravelMethod = animalData.m_PrimaryTravelMethod == AnimalTravelFlags.Flying;
			if (random.NextInt(5) == 0)
			{
				animal.m_Flags &= ~AnimalFlags.FlyingTarget;
				return true;
			}
			return false;
		}
		if ((animal.m_Flags & AnimalFlags.SwimmingTarget) != 0)
		{
			usingPrimaryTravelMethod = animalData.m_PrimaryTravelMethod == AnimalTravelFlags.Swimming;
			if (animalData.m_PrimaryTravelMethod == AnimalTravelFlags.Swimming || random.NextInt(3) == 0)
			{
				return false;
			}
			animal.m_Flags &= ~AnimalFlags.SwimmingTarget;
			return false;
		}
		if (animalData.m_FlySpeed > 0f)
		{
			if (inWater)
			{
				if ((animalData.m_SwimSpeed > 0f && random.NextInt(2) == 0) || (animalData.m_SwimSpeed == 0f && random.NextInt(3) != 0))
				{
					animal.m_Flags |= AnimalFlags.FlyingTarget;
					usingPrimaryTravelMethod = animalData.m_PrimaryTravelMethod == AnimalTravelFlags.Flying;
					return false;
				}
			}
			else if ((animalData.m_MoveSpeed > 0f && random.NextInt(2) == 0) || (animalData.m_MoveSpeed == 0f && random.NextInt(3) != 0))
			{
				animal.m_Flags |= AnimalFlags.FlyingTarget;
				usingPrimaryTravelMethod = animalData.m_PrimaryTravelMethod == AnimalTravelFlags.Flying;
				return false;
			}
		}
		if (inWater && animalData.m_SwimSpeed > 0f && random.NextInt(2) == 0)
		{
			animal.m_Flags |= AnimalFlags.SwimmingTarget;
			usingPrimaryTravelMethod = animalData.m_PrimaryTravelMethod == AnimalTravelFlags.Swimming;
			return false;
		}
		usingPrimaryTravelMethod = animalData.m_PrimaryTravelMethod == AnimalTravelFlags.None;
		return false;
	}

	private static void GetTripLength(AnimalData animalData, AnimalFlags animalFlags, bool usingPrimaryTravelMethod, WildlifeData wildlifeData, bool inWater, out float tripMinDistance, out float tripMaxDistance)
	{
		tripMinDistance = wildlifeData.m_TripLength.min;
		tripMaxDistance = wildlifeData.m_TripLength.max;
		if (usingPrimaryTravelMethod)
		{
			return;
		}
		float num = 1f;
		switch (animalData.m_PrimaryTravelMethod)
		{
		case AnimalTravelFlags.Flying:
			if ((animalFlags & AnimalFlags.SwimmingTarget) != 0)
			{
				num = animalData.m_SwimSpeed / animalData.m_FlySpeed;
			}
			else if ((animalFlags & AnimalFlags.FlyingTarget) == 0 && !inWater)
			{
				num = animalData.m_MoveSpeed / animalData.m_FlySpeed;
			}
			else if (inWater)
			{
				num = 0f;
			}
			break;
		case AnimalTravelFlags.Swimming:
			if ((animalFlags & AnimalFlags.FlyingTarget) != 0)
			{
				num = animalData.m_SwimSpeed / animalData.m_SwimSpeed;
			}
			else if ((animalFlags & AnimalFlags.SwimmingTarget) == 0)
			{
				num = animalData.m_MoveSpeed / animalData.m_SwimSpeed;
			}
			break;
		default:
			if ((animalFlags & AnimalFlags.FlyingTarget) != 0)
			{
				num = animalData.m_FlySpeed / animalData.m_MoveSpeed;
			}
			else if ((animalFlags & AnimalFlags.SwimmingTarget) != 0)
			{
				num = animalData.m_SwimSpeed / animalData.m_MoveSpeed;
			}
			else if (inWater)
			{
				num = 0f;
			}
			break;
		}
		tripMinDistance = wildlifeData.m_TripLength.min * num;
		tripMaxDistance = wildlifeData.m_TripLength.max * num;
	}

	private static void SetLandingPosition(ref WaterSurfaceData<SurfaceWater> waterSurfaceData, ref TerrainHeightData terrainData, float3 landingOffset, Game.Objects.Transform selfPosition, float3 targetPosition, ref AnimalNavigation navigation)
	{
		float3 @float = math.mul(selfPosition.m_Rotation, landingOffset);
		navigation.m_TargetPosition.y = WaterUtils.SampleHeight(ref waterSurfaceData, ref terrainData, targetPosition - @float, out bool hasDepth);
		navigation.m_TargetPosition.y += landingOffset.y - (hasDepth ? 0.2f : 0f);
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
	public WildlifeAISystem()
	{
	}
}
