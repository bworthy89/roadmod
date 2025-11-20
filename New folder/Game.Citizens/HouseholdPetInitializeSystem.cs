using System.Runtime.CompilerServices;
using Game.Common;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Citizens;

[CompilerGenerated]
public class HouseholdPetInitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeHouseholdPetJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdPet> m_HouseholdPetType;

		[ReadOnly]
		public ComponentLookup<Household> m_HouseholdData;

		public BufferLookup<HouseholdAnimal> m_HouseholdAnimals;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			NativeParallelMultiHashMap<Entity, Entity> nativeParallelMultiHashMap = default(NativeParallelMultiHashMap<Entity, Entity>);
			NativeList<Entity> nativeList = default(NativeList<Entity>);
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<HouseholdPet> nativeArray2 = archetypeChunk.GetNativeArray(ref m_HouseholdPetType);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					Entity value = nativeArray2[j].m_Household;
					Entity entity = nativeArray[j];
					if (m_HouseholdAnimals.HasBuffer(value))
					{
						m_HouseholdAnimals[value].Add(new HouseholdAnimal(entity));
					}
					else if (m_HouseholdData.HasComponent(value))
					{
						if (!nativeParallelMultiHashMap.IsCreated)
						{
							nativeParallelMultiHashMap = new NativeParallelMultiHashMap<Entity, Entity>(16, Allocator.Temp);
							nativeList = new NativeList<Entity>(16, Allocator.Temp);
						}
						if (!nativeParallelMultiHashMap.ContainsKey(value))
						{
							nativeList.Add(in value);
						}
						nativeParallelMultiHashMap.Add(value, entity);
					}
				}
			}
			if (!nativeParallelMultiHashMap.IsCreated)
			{
				return;
			}
			for (int k = 0; k < nativeList.Length; k++)
			{
				Entity entity2 = nativeList[k];
				if (nativeParallelMultiHashMap.TryGetFirstValue(entity2, out var item, out var it))
				{
					DynamicBuffer<HouseholdAnimal> dynamicBuffer = m_CommandBuffer.AddBuffer<HouseholdAnimal>(entity2);
					do
					{
						dynamicBuffer.Add(new HouseholdAnimal(item));
					}
					while (nativeParallelMultiHashMap.TryGetNextValue(out item, ref it));
				}
			}
			nativeList.Dispose();
			nativeParallelMultiHashMap.Dispose();
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdPet> __Game_Citizens_HouseholdPet_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

		public BufferLookup<HouseholdAnimal> __Game_Citizens_HouseholdAnimal_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_HouseholdPet_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdPet>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
			__Game_Citizens_HouseholdAnimal_RW_BufferLookup = state.GetBufferLookup<HouseholdAnimal>();
		}
	}

	private EntityQuery m_HouseholdPetQuery;

	private ModificationBarrier5 m_ModificationBarrier;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_HouseholdPetQuery = GetEntityQuery(ComponentType.ReadWrite<HouseholdPet>(), ComponentType.ReadOnly<Created>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_HouseholdPetQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		JobHandle jobHandle = IJobExtensions.Schedule(new InitializeHouseholdPetJob
		{
			m_Chunks = m_HouseholdPetQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_HouseholdPetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdPet_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HouseholdData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdAnimals = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdAnimal_RW_BufferLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		}, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
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
	public HouseholdPetInitializeSystem()
	{
	}
}
