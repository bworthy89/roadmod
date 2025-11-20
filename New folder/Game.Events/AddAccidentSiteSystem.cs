using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Events;

[CompilerGenerated]
public class AddAccidentSiteSystem : GameSystemBase
{
	[BurstCompile]
	private struct AddAccidentSiteJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentTypeHandle<AddAccidentSite> m_AddAccidentSiteType;

		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		public ComponentLookup<AccidentSite> m_AccidentSiteData;

		public ComponentLookup<CrimeProducer> m_CrimeProducerData;

		public BufferLookup<TargetElement> m_TargetElements;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			int num = 0;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				num += m_Chunks[i].Count;
			}
			NativeParallelHashMap<Entity, AccidentSite> nativeParallelHashMap = new NativeParallelHashMap<Entity, AccidentSite>(num, Allocator.Temp);
			for (int j = 0; j < m_Chunks.Length; j++)
			{
				NativeArray<AddAccidentSite> nativeArray = m_Chunks[j].GetNativeArray(ref m_AddAccidentSiteType);
				for (int k = 0; k < nativeArray.Length; k++)
				{
					AddAccidentSite addAccidentSite = nativeArray[k];
					if (m_PrefabRefData.HasComponent(addAccidentSite.m_Target))
					{
						AccidentSite accidentSite = new AccidentSite(addAccidentSite.m_Event, addAccidentSite.m_Flags, m_SimulationFrame);
						if (nativeParallelHashMap.TryGetValue(addAccidentSite.m_Target, out var item))
						{
							nativeParallelHashMap[addAccidentSite.m_Target] = MergeAccidentSites(item, accidentSite);
						}
						else if (m_AccidentSiteData.HasComponent(addAccidentSite.m_Target))
						{
							item = m_AccidentSiteData[addAccidentSite.m_Target];
							nativeParallelHashMap.TryAdd(addAccidentSite.m_Target, MergeAccidentSites(item, accidentSite));
						}
						else
						{
							nativeParallelHashMap.TryAdd(addAccidentSite.m_Target, accidentSite);
						}
						if ((accidentSite.m_Flags & AccidentSiteFlags.CrimeScene) != 0 && m_CrimeProducerData.HasComponent(addAccidentSite.m_Target))
						{
							CrimeProducer value = m_CrimeProducerData[addAccidentSite.m_Target];
							value.m_Crime *= 0.3f;
							m_CrimeProducerData[addAccidentSite.m_Target] = value;
						}
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
				AccidentSite accidentSite2 = nativeParallelHashMap[entity];
				if (m_AccidentSiteData.HasComponent(entity))
				{
					if (m_AccidentSiteData[entity].m_Event != accidentSite2.m_Event && m_TargetElements.HasBuffer(accidentSite2.m_Event))
					{
						CollectionUtils.TryAddUniqueValue(m_TargetElements[accidentSite2.m_Event], new TargetElement(entity));
					}
					m_AccidentSiteData[entity] = accidentSite2;
				}
				else
				{
					if (m_TargetElements.HasBuffer(accidentSite2.m_Event))
					{
						CollectionUtils.TryAddUniqueValue(m_TargetElements[accidentSite2.m_Event], new TargetElement(entity));
					}
					m_CommandBuffer.AddComponent(entity, accidentSite2);
				}
			}
		}

		private AccidentSite MergeAccidentSites(AccidentSite accidentSite1, AccidentSite accidentSite2)
		{
			AccidentSite result;
			if (accidentSite1.m_Event != Entity.Null != (accidentSite2.m_Event != Entity.Null))
			{
				result = ((accidentSite1.m_Event != Entity.Null) ? accidentSite1 : accidentSite2);
				result.m_Flags |= ((accidentSite1.m_Event != Entity.Null) ? accidentSite2.m_Flags : accidentSite1.m_Flags);
			}
			else
			{
				result = accidentSite1;
				result.m_Flags |= accidentSite2.m_Flags;
			}
			return result;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<AddAccidentSite> __Game_Events_AddAccidentSite_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		public ComponentLookup<AccidentSite> __Game_Events_AccidentSite_RW_ComponentLookup;

		public ComponentLookup<CrimeProducer> __Game_Buildings_CrimeProducer_RW_ComponentLookup;

		public BufferLookup<TargetElement> __Game_Events_TargetElement_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Events_AddAccidentSite_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AddAccidentSite>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Events_AccidentSite_RW_ComponentLookup = state.GetComponentLookup<AccidentSite>();
			__Game_Buildings_CrimeProducer_RW_ComponentLookup = state.GetComponentLookup<CrimeProducer>();
			__Game_Events_TargetElement_RW_BufferLookup = state.GetBufferLookup<TargetElement>();
		}
	}

	private SimulationSystem m_SimulationSystem;

	private ModificationBarrier4 m_ModificationBarrier;

	private EntityQuery m_ImpactQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_ImpactQuery = GetEntityQuery(ComponentType.ReadOnly<AddAccidentSite>(), ComponentType.ReadOnly<Game.Common.Event>());
		RequireForUpdate(m_ImpactQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> chunks = m_ImpactQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = IJobExtensions.Schedule(new AddAccidentSiteJob
		{
			m_Chunks = chunks,
			m_AddAccidentSiteType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_AddAccidentSite_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AccidentSiteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_AccidentSite_RW_ComponentLookup, ref base.CheckedStateRef),
			m_CrimeProducerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_CrimeProducer_RW_ComponentLookup, ref base.CheckedStateRef),
			m_TargetElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Events_TargetElement_RW_BufferLookup, ref base.CheckedStateRef),
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
	public AddAccidentSiteSystem()
	{
	}
}
