using System.Runtime.CompilerServices;
using Colossal.Collections;
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
public class SpectateSystem : GameSystemBase
{
	[BurstCompile]
	private struct SpectateJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentTypeHandle<Spectate> m_SpectateType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		public ComponentLookup<SpectatorSite> m_SpectatorSiteData;

		public BufferLookup<TargetElement> m_TargetElements;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			int num = 0;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				num += m_Chunks[i].Count;
			}
			NativeParallelHashMap<Entity, SpectatorSite> nativeParallelHashMap = new NativeParallelHashMap<Entity, SpectatorSite>(num, Allocator.Temp);
			for (int j = 0; j < m_Chunks.Length; j++)
			{
				NativeArray<Spectate> nativeArray = m_Chunks[j].GetNativeArray(ref m_SpectateType);
				for (int k = 0; k < nativeArray.Length; k++)
				{
					Spectate spectate = nativeArray[k];
					if (m_PrefabRefData.HasComponent(spectate.m_Target))
					{
						SpectatorSite item = new SpectatorSite(spectate.m_Event);
						if (!nativeParallelHashMap.TryGetValue(spectate.m_Target, out var _) && !m_SpectatorSiteData.HasComponent(spectate.m_Target))
						{
							nativeParallelHashMap.TryAdd(spectate.m_Target, item);
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
				SpectatorSite component = nativeParallelHashMap[entity];
				if (!m_SpectatorSiteData.HasComponent(entity))
				{
					if (m_TargetElements.HasBuffer(component.m_Event))
					{
						CollectionUtils.TryAddUniqueValue(m_TargetElements[component.m_Event], new TargetElement(entity));
					}
					m_CommandBuffer.AddComponent(entity, component);
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Spectate> __Game_Events_Spectate_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		public ComponentLookup<SpectatorSite> __Game_Events_SpectatorSite_RW_ComponentLookup;

		public BufferLookup<TargetElement> __Game_Events_TargetElement_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Events_Spectate_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Spectate>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Events_SpectatorSite_RW_ComponentLookup = state.GetComponentLookup<SpectatorSite>();
			__Game_Events_TargetElement_RW_BufferLookup = state.GetBufferLookup<TargetElement>();
		}
	}

	private ModificationBarrier4 m_ModificationBarrier;

	private EntityQuery m_SpectateQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_SpectateQuery = GetEntityQuery(ComponentType.ReadOnly<Spectate>(), ComponentType.ReadOnly<Game.Common.Event>());
		RequireForUpdate(m_SpectateQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> chunks = m_SpectateQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = IJobExtensions.Schedule(new SpectateJob
		{
			m_Chunks = chunks,
			m_SpectateType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_Spectate_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpectatorSiteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_SpectatorSite_RW_ComponentLookup, ref base.CheckedStateRef),
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
	public SpectateSystem()
	{
	}
}
