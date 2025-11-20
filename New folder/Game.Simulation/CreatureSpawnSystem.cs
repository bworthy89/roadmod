using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Common;
using Game.Creatures;
using Game.Objects;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CreatureSpawnSystem : GameSystemBase
{
	private struct SpawnData : IComparable<SpawnData>
	{
		public Entity m_Source;

		public Entity m_Creature;

		public int m_Priority;

		public int CompareTo(SpawnData other)
		{
			return math.select(m_Priority - other.m_Priority, m_Source.Index - other.m_Source.Index, m_Source.Index != other.m_Source.Index);
		}
	}

	private struct SpawnRange
	{
		public int m_Start;

		public int m_End;
	}

	[BurstCompile]
	private struct GroupSpawnSourcesJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<TripSource> m_TripSourceType;

		public NativeList<SpawnData> m_SpawnData;

		public NativeList<SpawnRange> m_SpawnGroups;

		public void Execute()
		{
			SpawnData value2 = default(SpawnData);
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<TripSource> nativeArray2 = archetypeChunk.GetNativeArray(ref m_TripSourceType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					TripSource value = nativeArray2[j];
					if (value.m_Timer <= 0)
					{
						value2.m_Source = value.m_Source;
						value2.m_Creature = nativeArray[j];
						value2.m_Priority = value.m_Timer;
						m_SpawnData.Add(in value2);
					}
					value.m_Timer -= 16;
					nativeArray2[j] = value;
				}
			}
			if (m_SpawnData.Length == 0)
			{
				return;
			}
			m_SpawnData.Sort();
			SpawnRange value3 = default(SpawnRange);
			value3.m_Start = -1;
			Entity entity = Entity.Null;
			for (int k = 0; k < m_SpawnData.Length; k++)
			{
				Entity entity2 = m_SpawnData[k].m_Source;
				if (entity2 != entity)
				{
					if (value3.m_Start != -1)
					{
						value3.m_End = k;
						m_SpawnGroups.Add(in value3);
					}
					value3.m_Start = k;
					entity = entity2;
				}
			}
			if (value3.m_Start != -1)
			{
				value3.m_End = m_SpawnData.Length;
				m_SpawnGroups.Add(in value3);
			}
		}
	}

	[BurstCompile]
	private struct TrySpawnCreaturesJob : IJobParallelForDefer
	{
		[ReadOnly]
		public NativeArray<SpawnData> m_SpawnData;

		[ReadOnly]
		public NativeArray<SpawnRange> m_SpawnGroups;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		[ReadOnly]
		public ComponentLookup<Resident> m_ResidentData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Patient> m_Patients;

		[NativeDisableParallelForRestriction]
		public BufferLookup<Occupant> m_Occupants;

		public void Execute(int index)
		{
			SpawnRange spawnRange = m_SpawnGroups[index];
			Entity entity = m_SpawnData[spawnRange.m_Start].m_Source;
			DynamicBuffer<Patient> buffer = default(DynamicBuffer<Patient>);
			DynamicBuffer<Occupant> buffer2 = default(DynamicBuffer<Occupant>);
			if (m_Patients.HasBuffer(entity))
			{
				buffer = m_Patients[entity];
			}
			if (m_Occupants.HasBuffer(entity))
			{
				buffer2 = m_Occupants[entity];
			}
			for (int i = spawnRange.m_Start; i < spawnRange.m_End; i++)
			{
				Entity entity2 = m_SpawnData[i].m_Creature;
				m_CommandBuffer.RemoveComponent<TripSource>(index, entity2);
				if (m_ResidentData.HasComponent(entity2))
				{
					Resident resident = m_ResidentData[entity2];
					if (buffer.IsCreated)
					{
						CollectionUtils.RemoveValue(buffer, new Patient(resident.m_Citizen));
					}
					if (buffer2.IsCreated)
					{
						CollectionUtils.RemoveValue(buffer2, new Occupant(resident.m_Citizen));
					}
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public ComponentTypeHandle<TripSource> __Game_Objects_TripSource_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Resident> __Game_Creatures_Resident_RO_ComponentLookup;

		public BufferLookup<Patient> __Game_Buildings_Patient_RW_BufferLookup;

		public BufferLookup<Occupant> __Game_Buildings_Occupant_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_TripSource_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TripSource>();
			__Game_Creatures_Resident_RO_ComponentLookup = state.GetComponentLookup<Resident>(isReadOnly: true);
			__Game_Buildings_Patient_RW_BufferLookup = state.GetBufferLookup<Patient>();
			__Game_Buildings_Occupant_RW_BufferLookup = state.GetBufferLookup<Occupant>();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_CreatureQuery;

	private ComponentTypeSet m_TripSourceRemoveTypes;

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
		m_CreatureQuery = GetEntityQuery(ComponentType.ReadWrite<TripSource>(), ComponentType.ReadOnly<Creature>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_CreatureQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeList<SpawnData> spawnData = new NativeList<SpawnData>(Allocator.TempJob);
		NativeList<SpawnRange> nativeList = new NativeList<SpawnRange>(Allocator.TempJob);
		JobHandle outJobHandle;
		GroupSpawnSourcesJob jobData = new GroupSpawnSourcesJob
		{
			m_Chunks = m_CreatureQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TripSourceType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_TripSource_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SpawnData = spawnData,
			m_SpawnGroups = nativeList
		};
		JobHandle jobHandle = new TrySpawnCreaturesJob
		{
			m_SpawnData = spawnData.AsDeferredJobArray(),
			m_SpawnGroups = nativeList.AsDeferredJobArray(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_ResidentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Resident_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Patients = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Patient_RW_BufferLookup, ref base.CheckedStateRef),
			m_Occupants = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Occupant_RW_BufferLookup, ref base.CheckedStateRef)
		}.Schedule(dependsOn: IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, outJobHandle)), list: nativeList, innerloopBatchCount: 1);
		spawnData.Dispose(jobHandle);
		nativeList.Dispose(jobHandle);
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
	public CreatureSpawnSystem()
	{
	}
}
