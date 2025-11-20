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
public class TutorialObjectSelectedActivationSystem : GameSystemBase
{
	[BurstCompile]
	private struct ActivateJob : IJobChunk
	{
		[ReadOnly]
		public BufferTypeHandle<ObjectSelectionActivationData> m_ActivationDataType;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public Entity m_Selection;

		public bool m_Tool;

		public EntityCommandBuffer.ParallelWriter m_Writer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<ObjectSelectionActivationData> bufferAccessor = chunk.GetBufferAccessor(ref m_ActivationDataType);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				if (Check(bufferAccessor[i]))
				{
					m_Writer.AddComponent<TutorialActivated>(unfilteredChunkIndex, nativeArray[i]);
				}
			}
		}

		private bool Check(DynamicBuffer<ObjectSelectionActivationData> datas)
		{
			for (int i = 0; i < datas.Length; i++)
			{
				if (datas[i].m_Prefab == m_Selection && (!m_Tool || datas[i].m_AllowTool))
				{
					return true;
				}
			}
			return false;
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

	protected EntityCommandBufferSystem m_BarrierSystem;

	private PrefabSystem m_PrefabSystem;

	private ToolSystem m_ToolSystem;

	private ObjectToolSystem m_ObjectToolSystem;

	private NetToolSystem m_NetToolSystem;

	private AreaToolSystem m_AreaToolSystem;

	private RouteToolSystem m_RouteToolSystem;

	private EntityQuery m_TutorialQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_BarrierSystem = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_ObjectToolSystem = base.World.GetOrCreateSystemManaged<ObjectToolSystem>();
		m_NetToolSystem = base.World.GetOrCreateSystemManaged<NetToolSystem>();
		m_AreaToolSystem = base.World.GetOrCreateSystemManaged<AreaToolSystem>();
		m_RouteToolSystem = base.World.GetOrCreateSystemManaged<RouteToolSystem>();
		m_TutorialQuery = GetEntityQuery(ComponentType.ReadOnly<ObjectSelectionActivationData>(), ComponentType.Exclude<TutorialActivated>(), ComponentType.Exclude<TutorialCompleted>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_TutorialQuery.IsEmptyIgnoreFilter)
		{
			bool tool;
			Entity selection = GetSelection(out tool);
			if (selection != Entity.Null)
			{
				ActivateJob jobData = new ActivateJob
				{
					m_ActivationDataType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Tutorials_ObjectSelectionActivationData_RO_BufferTypeHandle, ref base.CheckedStateRef),
					m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
					m_Selection = selection,
					m_Tool = tool,
					m_Writer = m_BarrierSystem.CreateCommandBuffer().AsParallelWriter()
				};
				base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_TutorialQuery, base.Dependency);
				m_BarrierSystem.AddJobHandleForProducer(base.Dependency);
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
		if (m_ToolSystem.activeTool == m_AreaToolSystem && m_AreaToolSystem.prefab != null)
		{
			return m_PrefabSystem.GetEntity(m_AreaToolSystem.prefab);
		}
		if (m_ToolSystem.activeTool == m_RouteToolSystem && m_RouteToolSystem.prefab != null)
		{
			return m_PrefabSystem.GetEntity(m_RouteToolSystem.prefab);
		}
		tool = false;
		return Entity.Null;
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
	public TutorialObjectSelectedActivationSystem()
	{
	}
}
