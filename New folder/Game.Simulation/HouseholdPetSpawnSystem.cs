using System.Runtime.CompilerServices;
using Game.Citizens;
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
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class HouseholdPetSpawnSystem : GameSystemBase
{
	[BurstCompile]
	private struct HouseholdPetSpawnJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentTransport> m_CurrentTransportType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;

		[ReadOnly]
		public ComponentTypeHandle<Target> m_TargetType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<PetData> m_PetDataType;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<HouseholdPetData> m_HouseholdPetData;

		[ReadOnly]
		public ComponentLookup<ObjectData> m_ObjectData;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_AnimalPrefabChunks;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EntityArchetype m_ResetTripArchetype;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<CurrentTransport> nativeArray2 = chunk.GetNativeArray(ref m_CurrentTransportType);
			NativeArray<CurrentBuilding> nativeArray3 = chunk.GetNativeArray(ref m_CurrentBuildingType);
			NativeArray<Target> nativeArray4 = chunk.GetNativeArray(ref m_TargetType);
			NativeArray<PrefabRef> nativeArray5 = chunk.GetNativeArray(ref m_PrefabRefType);
			if (nativeArray3.Length == 0)
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					m_CommandBuffer.RemoveComponent<Target>(unfilteredChunkIndex, nativeArray[i]);
				}
				return;
			}
			if (nativeArray2.Length == 0)
			{
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity = nativeArray[j];
					CurrentBuilding currentBuilding = nativeArray3[j];
					Target target = nativeArray4[j];
					PrefabRef prefabRef = nativeArray5[j];
					HouseholdPetData householdPetData = m_HouseholdPetData[prefabRef.m_Prefab];
					Random random = m_RandomSeed.GetRandom(entity.Index);
					PseudoRandomSeed randomSeed;
					Entity entity2 = ObjectEmergeSystem.SelectAnimalPrefab(ref random, householdPetData.m_Type, m_AnimalPrefabChunks, m_EntityType, m_PetDataType, out randomSeed);
					if (entity2 != Entity.Null && m_TransformData.HasComponent(currentBuilding.m_CurrentBuilding))
					{
						Entity transport = SpawnPet(unfilteredChunkIndex, entity, currentBuilding.m_CurrentBuilding, target.m_Target, entity2, randomSeed);
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, new CurrentTransport(transport));
						m_CommandBuffer.RemoveComponent<CurrentBuilding>(unfilteredChunkIndex, entity);
					}
					m_CommandBuffer.RemoveComponent<Target>(unfilteredChunkIndex, entity);
				}
				return;
			}
			for (int k = 0; k < nativeArray.Length; k++)
			{
				Entity e = nativeArray[k];
				CurrentBuilding currentBuilding2 = nativeArray3[k];
				CurrentTransport currentTransport = nativeArray2[k];
				Target target2 = nativeArray4[k];
				if (!m_DeletedData.HasComponent(currentTransport.m_CurrentTransport))
				{
					Entity e2 = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, m_ResetTripArchetype);
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, e2, new ResetTrip
					{
						m_Creature = currentTransport.m_CurrentTransport,
						m_Source = currentBuilding2.m_CurrentBuilding,
						m_Target = target2.m_Target,
						m_Delay = 512u
					});
					m_CommandBuffer.RemoveComponent<CurrentBuilding>(unfilteredChunkIndex, e);
					m_CommandBuffer.RemoveComponent<Target>(unfilteredChunkIndex, e);
				}
			}
		}

		private Entity SpawnPet(int jobIndex, Entity householdPet, Entity source, Entity target, Entity prefab, PseudoRandomSeed randomSeed)
		{
			ObjectData objectData = m_ObjectData[prefab];
			Entity entity = m_CommandBuffer.CreateEntity(jobIndex, objectData.m_Archetype);
			m_CommandBuffer.SetComponent(jobIndex, entity, new PrefabRef(prefab));
			m_CommandBuffer.SetComponent(jobIndex, entity, m_TransformData[source]);
			m_CommandBuffer.SetComponent(jobIndex, entity, new Target(target));
			m_CommandBuffer.SetComponent(jobIndex, entity, new Game.Creatures.Pet(householdPet));
			m_CommandBuffer.SetComponent(jobIndex, entity, randomSeed);
			m_CommandBuffer.AddComponent(jobIndex, entity, new TripSource(source, 512u));
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Unspawned));
			m_CommandBuffer.AddComponent(jobIndex, entity, default(AnimalCurrentLane));
			return entity;
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
		public ComponentTypeHandle<CurrentTransport> __Game_Citizens_CurrentTransport_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Target> __Game_Common_Target_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PetData> __Game_Prefabs_PetData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdPetData> __Game_Prefabs_HouseholdPetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectData> __Game_Prefabs_ObjectData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_CurrentTransport_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentTransport>(isReadOnly: true);
			__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentBuilding>(isReadOnly: true);
			__Game_Common_Target_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Target>();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_PetData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PetData>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Prefabs_HouseholdPetData_RO_ComponentLookup = state.GetComponentLookup<HouseholdPetData>(isReadOnly: true);
			__Game_Prefabs_ObjectData_RO_ComponentLookup = state.GetComponentLookup<ObjectData>(isReadOnly: true);
		}
	}

	private EntityQuery m_HouseholdPetQuery;

	private EntityQuery m_AnimalPrefabQuery;

	private EntityArchetype m_ResetTripArchetype;

	private EndFrameBarrier m_EndFrameBarrier;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_HouseholdPetQuery = GetEntityQuery(ComponentType.ReadOnly<HouseholdPet>(), ComponentType.ReadOnly<Target>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_AnimalPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<AnimalData>(), ComponentType.ReadOnly<PetData>());
		m_ResetTripArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<ResetTrip>());
		RequireForUpdate(m_HouseholdPetQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> animalPrefabChunks = m_AnimalPrefabQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new HouseholdPetSpawnJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CurrentTransportType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CurrentTransport_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PetDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PetData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdPetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_HouseholdPetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AnimalPrefabChunks = animalPrefabChunks,
			m_RandomSeed = RandomSeed.Next(),
			m_ResetTripArchetype = m_ResetTripArchetype,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_HouseholdPetQuery, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
		animalPrefabChunks.Dispose(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
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
	public HouseholdPetSpawnSystem()
	{
	}
}
