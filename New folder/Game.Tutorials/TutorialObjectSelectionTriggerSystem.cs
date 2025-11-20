using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Tutorials;

[CompilerGenerated]
public class TutorialObjectSelectionTriggerSystem : TutorialTriggerSystemBase
{
	[BurstCompile]
	private struct CheckSelectionJob : IJobChunk
	{
		[ReadOnly]
		public BufferLookup<ForceUIGroupUnlockData> m_ForcedUnlockDataFromEntity;

		[ReadOnly]
		public BufferLookup<UnlockRequirement> m_UnlockRequirementFromEntity;

		[ReadOnly]
		public BufferTypeHandle<ObjectSelectionTriggerData> m_TriggerType;

		[ReadOnly]
		public EntityArchetype m_UnlockEventArchetype;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public Entity m_Selection;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<ObjectSelectionTriggerData> bufferAccessor = chunk.GetBufferAccessor(ref m_TriggerType);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				DynamicBuffer<ObjectSelectionTriggerData> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					ObjectSelectionTriggerData objectSelectionTriggerData = dynamicBuffer[j];
					if (objectSelectionTriggerData.m_Prefab == m_Selection)
					{
						if (objectSelectionTriggerData.m_GoToPhase != Entity.Null)
						{
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, nativeArray[i], new TutorialNextPhase
							{
								m_NextPhase = objectSelectionTriggerData.m_GoToPhase
							});
							m_CommandBuffer.AddComponent<TriggerPreCompleted>(unfilteredChunkIndex, nativeArray[i]);
						}
						else
						{
							m_CommandBuffer.AddComponent<TriggerCompleted>(unfilteredChunkIndex, nativeArray[i]);
							TutorialSystem.ManualUnlock(nativeArray[i], m_UnlockEventArchetype, ref m_ForcedUnlockDataFromEntity, ref m_UnlockRequirementFromEntity, m_CommandBuffer, unfilteredChunkIndex);
						}
					}
					else
					{
						m_CommandBuffer.RemoveComponent<TriggerPreCompleted>(unfilteredChunkIndex, nativeArray[i]);
						m_CommandBuffer.RemoveComponent<TutorialNextPhase>(unfilteredChunkIndex, nativeArray[i]);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public BufferLookup<ForceUIGroupUnlockData> __Game_Prefabs_ForceUIGroupUnlockData_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<UnlockRequirement> __Game_Prefabs_UnlockRequirement_RO_BufferLookup;

		[ReadOnly]
		public BufferTypeHandle<ObjectSelectionTriggerData> __Game_Tutorials_ObjectSelectionTriggerData_RO_BufferTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_ForceUIGroupUnlockData_RO_BufferLookup = state.GetBufferLookup<ForceUIGroupUnlockData>(isReadOnly: true);
			__Game_Prefabs_UnlockRequirement_RO_BufferLookup = state.GetBufferLookup<UnlockRequirement>(isReadOnly: true);
			__Game_Tutorials_ObjectSelectionTriggerData_RO_BufferTypeHandle = state.GetBufferTypeHandle<ObjectSelectionTriggerData>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
		}
	}

	private ToolSystem m_ToolSystem;

	private EntityArchetype m_UnlockEventArchetype;

	private Entity m_LastSelection;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_ActiveTriggerQuery = GetEntityQuery(ComponentType.ReadOnly<ObjectSelectionTriggerData>(), ComponentType.ReadOnly<TriggerActive>(), ComponentType.Exclude<TriggerCompleted>());
		m_UnlockEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Unlock>());
		RequireForUpdate(m_ActiveTriggerQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (base.triggersChanged || m_ToolSystem.selected == Entity.Null)
		{
			m_LastSelection = m_ToolSystem.selected;
		}
		if (m_ToolSystem.selected != Entity.Null && m_ToolSystem.selected != m_LastSelection)
		{
			m_LastSelection = m_ToolSystem.selected;
			if (base.EntityManager.TryGetComponent<PrefabRef>(m_ToolSystem.selected, out var component))
			{
				CheckSelectionJob jobData = new CheckSelectionJob
				{
					m_ForcedUnlockDataFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ForceUIGroupUnlockData_RO_BufferLookup, ref base.CheckedStateRef),
					m_UnlockRequirementFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_UnlockRequirement_RO_BufferLookup, ref base.CheckedStateRef),
					m_TriggerType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Tutorials_ObjectSelectionTriggerData_RO_BufferTypeHandle, ref base.CheckedStateRef),
					m_UnlockEventArchetype = m_UnlockEventArchetype,
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_Selection = component.m_Prefab,
					m_CommandBuffer = m_BarrierSystem.CreateCommandBuffer().AsParallelWriter()
				};
				base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_ActiveTriggerQuery, base.Dependency);
				m_BarrierSystem.AddJobHandleForProducer(base.Dependency);
			}
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
	public TutorialObjectSelectionTriggerSystem()
	{
	}
}
