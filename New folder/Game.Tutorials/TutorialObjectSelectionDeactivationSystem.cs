using System.Runtime.CompilerServices;
using Colossal.Entities;
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
public class TutorialObjectSelectionDeactivationSystem : TutorialDeactivationSystemBase
{
	[BurstCompile]
	private struct CheckTutorialsJob : IJobChunk
	{
		[ReadOnly]
		public BufferTypeHandle<ObjectSelectionActivationData> m_DeactivationDataType;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public Entity m_Selection;

		public bool m_Tool;

		public EntityCommandBuffer.ParallelWriter m_Buffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<ObjectSelectionActivationData> bufferAccessor = chunk.GetBufferAccessor(ref m_DeactivationDataType);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				if (m_Selection == Entity.Null || ShouldDeactivate(bufferAccessor[i]))
				{
					m_Buffer.RemoveComponent<TutorialActivated>(unfilteredChunkIndex, nativeArray[i]);
				}
			}
		}

		private bool ShouldDeactivate(DynamicBuffer<ObjectSelectionActivationData> selections)
		{
			for (int i = 0; i < selections.Length; i++)
			{
				if (selections[i].m_Prefab == m_Selection && (selections[i].m_AllowTool || !m_Tool))
				{
					return false;
				}
			}
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
		public BufferTypeHandle<ObjectSelectionActivationData> __Game_Tutorials_ObjectSelectionActivationData_RO_BufferTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Tutorials_ObjectSelectionActivationData_RO_BufferTypeHandle = state.GetBufferTypeHandle<ObjectSelectionActivationData>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
		}
	}

	private PrefabSystem m_PrefabSystem;

	private ObjectToolSystem m_ObjectToolSystem;

	private NetToolSystem m_NetToolSystem;

	private ToolSystem m_ToolSystem;

	private EntityQuery m_PendingTutorialQuery;

	private EntityQuery m_ActiveTutorialQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PendingTutorialQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialData>(), ComponentType.ReadOnly<ObjectSelectionActivationData>(), ComponentType.ReadOnly<TutorialActivated>(), ComponentType.Exclude<TutorialActive>(), ComponentType.Exclude<TutorialCompleted>(), ComponentType.Exclude<ForceActivation>());
		m_ActiveTutorialQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialData>(), ComponentType.ReadOnly<ObjectSelectionActivationData>(), ComponentType.ReadOnly<TutorialActivated>(), ComponentType.ReadOnly<TutorialActive>(), ComponentType.Exclude<TutorialCompleted>(), ComponentType.Exclude<ForceActivation>());
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_NetToolSystem = base.World.GetOrCreateSystemManaged<NetToolSystem>();
		m_ObjectToolSystem = base.World.GetOrCreateSystemManaged<ObjectToolSystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_PendingTutorialQuery.IsEmptyIgnoreFilter || !m_ActiveTutorialQuery.IsEmptyIgnoreFilter)
		{
			bool tool;
			Entity selection = GetSelection(out tool);
			if (!m_PendingTutorialQuery.IsEmptyIgnoreFilter)
			{
				CheckDeactivate(m_PendingTutorialQuery, selection, tool);
			}
			if (!m_ActiveTutorialQuery.IsEmptyIgnoreFilter && base.phaseCanDeactivate)
			{
				CheckDeactivate(m_ActiveTutorialQuery, selection, tool);
			}
		}
	}

	private Entity GetSelection(out bool tool)
	{
		tool = true;
		if (base.EntityManager.TryGetComponent<PrefabRef>(m_ToolSystem.selected, out var component))
		{
			tool = false;
			return component.m_Prefab;
		}
		if (m_ToolSystem.activeTool == m_ObjectToolSystem && m_ObjectToolSystem.prefab != null)
		{
			return m_PrefabSystem.GetEntity(m_ObjectToolSystem.prefab);
		}
		if (m_ToolSystem.activeTool == m_NetToolSystem && m_NetToolSystem.prefab != null)
		{
			return m_PrefabSystem.GetEntity(m_NetToolSystem.prefab);
		}
		tool = false;
		return Entity.Null;
	}

	private void CheckDeactivate(EntityQuery query, Entity selection, bool tool)
	{
		CheckTutorialsJob jobData = new CheckTutorialsJob
		{
			m_DeactivationDataType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Tutorials_ObjectSelectionActivationData_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_Selection = selection,
			m_Tool = tool,
			m_Buffer = m_BarrierSystem.CreateCommandBuffer().AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, query, base.Dependency);
		m_BarrierSystem.AddJobHandleForProducer(base.Dependency);
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
	public TutorialObjectSelectionDeactivationSystem()
	{
	}
}
