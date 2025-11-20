using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Events;

[CompilerGenerated]
public class SubmergeSystem : GameSystemBase
{
	[BurstCompile]
	private struct SubmergeJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentTypeHandle<Submerge> m_SubmergeType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		public ComponentLookup<Flooded> m_FloodedData;

		public BufferLookup<TargetElement> m_TargetElements;

		public EntityArchetype m_JournalDataArchetype;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			int num = 0;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				num += m_Chunks[i].Count;
			}
			NativeParallelHashMap<Entity, Flooded> nativeParallelHashMap = new NativeParallelHashMap<Entity, Flooded>(num, Allocator.Temp);
			for (int j = 0; j < m_Chunks.Length; j++)
			{
				NativeArray<Submerge> nativeArray = m_Chunks[j].GetNativeArray(ref m_SubmergeType);
				for (int k = 0; k < nativeArray.Length; k++)
				{
					Submerge submerge = nativeArray[k];
					if (!m_PrefabRefData.HasComponent(submerge.m_Target))
					{
						continue;
					}
					Flooded flooded = new Flooded(submerge.m_Event, submerge.m_Depth);
					if (nativeParallelHashMap.TryGetValue(submerge.m_Target, out var item))
					{
						if (flooded.m_Depth > item.m_Depth)
						{
							nativeParallelHashMap[submerge.m_Target] = flooded;
						}
					}
					else if (m_FloodedData.HasComponent(submerge.m_Target))
					{
						item = m_FloodedData[submerge.m_Target];
						if (flooded.m_Depth > item.m_Depth)
						{
							nativeParallelHashMap.TryAdd(submerge.m_Target, flooded);
						}
					}
					else
					{
						nativeParallelHashMap.TryAdd(submerge.m_Target, flooded);
					}
				}
			}
			if (nativeParallelHashMap.Count() == 0)
			{
				return;
			}
			NativeArray<Entity> keyArray = nativeParallelHashMap.GetKeyArray(Allocator.Temp);
			for (int l = 0; l < keyArray.Length; l++)
			{
				Entity entity = keyArray[l];
				Flooded flooded2 = nativeParallelHashMap[entity];
				if (m_FloodedData.HasComponent(entity))
				{
					if (m_FloodedData[entity].m_Event != flooded2.m_Event)
					{
						if (m_TargetElements.HasBuffer(flooded2.m_Event))
						{
							CollectionUtils.TryAddUniqueValue(m_TargetElements[flooded2.m_Event], new TargetElement(entity));
						}
						AddJournalData(entity, flooded2);
					}
					m_FloodedData[entity] = flooded2;
				}
				else
				{
					if (m_TargetElements.HasBuffer(flooded2.m_Event))
					{
						CollectionUtils.TryAddUniqueValue(m_TargetElements[flooded2.m_Event], new TargetElement(entity));
					}
					m_CommandBuffer.AddComponent(entity, flooded2);
					AddJournalData(entity, flooded2);
				}
			}
		}

		private void AddJournalData(Entity target, Flooded flooded)
		{
			if (m_BuildingData.HasComponent(target))
			{
				Entity e = m_CommandBuffer.CreateEntity(m_JournalDataArchetype);
				m_CommandBuffer.SetComponent(e, new AddEventJournalData(flooded.m_Event, EventDataTrackingType.Damages));
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Submerge> __Game_Events_Submerge_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		public ComponentLookup<Flooded> __Game_Events_Flooded_RW_ComponentLookup;

		public BufferLookup<TargetElement> __Game_Events_TargetElement_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Events_Submerge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Submerge>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Events_Flooded_RW_ComponentLookup = state.GetComponentLookup<Flooded>();
			__Game_Events_TargetElement_RW_BufferLookup = state.GetBufferLookup<TargetElement>();
		}
	}

	private ModificationBarrier4 m_ModificationBarrier;

	private EntityQuery m_FaceWeatherQuery;

	private EntityArchetype m_JournalDataArchetype;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_FaceWeatherQuery = GetEntityQuery(ComponentType.ReadOnly<Submerge>(), ComponentType.ReadOnly<Game.Common.Event>());
		m_JournalDataArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<AddEventJournalData>(), ComponentType.ReadWrite<Game.Common.Event>());
		RequireForUpdate(m_FaceWeatherQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> chunks = m_FaceWeatherQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = IJobExtensions.Schedule(new SubmergeJob
		{
			m_Chunks = chunks,
			m_SubmergeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_Submerge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FloodedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_Flooded_RW_ComponentLookup, ref base.CheckedStateRef),
			m_TargetElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Events_TargetElement_RW_BufferLookup, ref base.CheckedStateRef),
			m_JournalDataArchetype = m_JournalDataArchetype,
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		}, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
		chunks.Dispose(jobHandle);
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
	public SubmergeSystem()
	{
	}
}
