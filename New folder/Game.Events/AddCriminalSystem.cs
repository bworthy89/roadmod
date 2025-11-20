using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Citizens;
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
public class AddCriminalSystem : GameSystemBase
{
	[BurstCompile]
	private struct AddCriminalJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentTypeHandle<AddCriminal> m_AddCriminalType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		public ComponentLookup<Criminal> m_Criminals;

		public BufferLookup<TargetElement> m_TargetElements;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			int num = 0;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				num += m_Chunks[i].Count;
			}
			NativeParallelHashMap<Entity, Criminal> nativeParallelHashMap = new NativeParallelHashMap<Entity, Criminal>(num, Allocator.Temp);
			for (int j = 0; j < m_Chunks.Length; j++)
			{
				NativeArray<AddCriminal> nativeArray = m_Chunks[j].GetNativeArray(ref m_AddCriminalType);
				for (int k = 0; k < nativeArray.Length; k++)
				{
					AddCriminal addCriminal = nativeArray[k];
					if (m_PrefabRefData.HasComponent(addCriminal.m_Target))
					{
						Criminal criminal = new Criminal(addCriminal.m_Event, addCriminal.m_Flags);
						if (nativeParallelHashMap.TryGetValue(addCriminal.m_Target, out var item))
						{
							nativeParallelHashMap[addCriminal.m_Target] = MergeCriminals(item, criminal);
						}
						else if (m_Criminals.HasComponent(addCriminal.m_Target))
						{
							item = m_Criminals[addCriminal.m_Target];
							nativeParallelHashMap.TryAdd(addCriminal.m_Target, MergeCriminals(item, criminal));
						}
						else
						{
							nativeParallelHashMap.TryAdd(addCriminal.m_Target, criminal);
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
				Criminal criminal2 = nativeParallelHashMap[entity];
				if (m_Criminals.HasComponent(entity))
				{
					if (m_Criminals[entity].m_Event != criminal2.m_Event && m_TargetElements.HasBuffer(criminal2.m_Event))
					{
						CollectionUtils.TryAddUniqueValue(m_TargetElements[criminal2.m_Event], new TargetElement(entity));
					}
					m_Criminals[entity] = criminal2;
				}
				else
				{
					if (m_TargetElements.HasBuffer(criminal2.m_Event))
					{
						CollectionUtils.TryAddUniqueValue(m_TargetElements[criminal2.m_Event], new TargetElement(entity));
					}
					m_CommandBuffer.AddComponent(entity, criminal2);
				}
			}
		}

		private Criminal MergeCriminals(Criminal criminal1, Criminal criminal2)
		{
			if (((criminal1.m_Flags ^ criminal2.m_Flags) & CriminalFlags.Prisoner) != 0)
			{
				if ((criminal1.m_Flags & CriminalFlags.Prisoner) == 0)
				{
					return criminal2;
				}
				return criminal1;
			}
			Criminal result;
			if (criminal1.m_Event != Entity.Null != (criminal2.m_Event != Entity.Null))
			{
				result = ((criminal1.m_Event != Entity.Null) ? criminal1 : criminal2);
				result.m_Flags |= ((criminal1.m_Event != Entity.Null) ? criminal2.m_Flags : criminal1.m_Flags);
			}
			else
			{
				result = criminal1;
				result.m_Flags |= criminal2.m_Flags;
			}
			return result;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<AddCriminal> __Game_Events_AddCriminal_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		public ComponentLookup<Criminal> __Game_Citizens_Criminal_RW_ComponentLookup;

		public BufferLookup<TargetElement> __Game_Events_TargetElement_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Events_AddCriminal_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AddCriminal>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Citizens_Criminal_RW_ComponentLookup = state.GetComponentLookup<Criminal>();
			__Game_Events_TargetElement_RW_BufferLookup = state.GetBufferLookup<TargetElement>();
		}
	}

	private ModificationBarrier4 m_ModificationBarrier;

	private EntityQuery m_AddCriminalQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_AddCriminalQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Common.Event>(), ComponentType.ReadOnly<AddCriminal>());
		RequireForUpdate(m_AddCriminalQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> chunks = m_AddCriminalQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = IJobExtensions.Schedule(new AddCriminalJob
		{
			m_Chunks = chunks,
			m_AddCriminalType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_AddCriminal_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Criminals = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Criminal_RW_ComponentLookup, ref base.CheckedStateRef),
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
	public AddCriminalSystem()
	{
	}
}
