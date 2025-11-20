using System.Runtime.CompilerServices;
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
public class TutorialInfoviewDeactivationSystem : TutorialDeactivationSystemBase
{
	[BurstCompile]
	private struct CheckDeactivationJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<InfoviewActivationData> m_ActivationType;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public Entity m_Infoview;

		public EntityCommandBuffer.ParallelWriter m_Buffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<InfoviewActivationData> nativeArray = chunk.GetNativeArray(ref m_ActivationType);
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (nativeArray[i].m_Infoview != m_Infoview)
				{
					m_Buffer.RemoveComponent<TutorialActivated>(unfilteredChunkIndex, nativeArray2[i]);
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
		public ComponentTypeHandle<InfoviewActivationData> __Game_Tutorials_InfoviewActivationData_RO_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Tutorials_InfoviewActivationData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewActivationData>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
		}
	}

	private PrefabSystem m_PrefabSystem;

	private ToolSystem m_ToolSystem;

	private EntityQuery m_PendingTutorialQuery;

	private EntityQuery m_ActiveTutorialQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_PendingTutorialQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialData>(), ComponentType.ReadOnly<InfoviewActivationData>(), ComponentType.ReadOnly<TutorialActivated>(), ComponentType.Exclude<TutorialActive>(), ComponentType.Exclude<TutorialCompleted>(), ComponentType.Exclude<ForceActivation>());
		m_ActiveTutorialQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialData>(), ComponentType.ReadOnly<InfoviewActivationData>(), ComponentType.ReadOnly<TutorialActivated>(), ComponentType.ReadOnly<TutorialActive>(), ComponentType.Exclude<TutorialCompleted>(), ComponentType.Exclude<ForceActivation>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_PendingTutorialQuery.IsEmptyIgnoreFilter)
		{
			CheckDeactivation(m_PendingTutorialQuery);
		}
		if (!m_ActiveTutorialQuery.IsEmptyIgnoreFilter && base.phaseCanDeactivate)
		{
			CheckDeactivation(m_ActiveTutorialQuery);
		}
	}

	private void CheckDeactivation(EntityQuery query)
	{
		CheckDeactivationJob jobData = new CheckDeactivationJob
		{
			m_ActivationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tutorials_InfoviewActivationData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_Infoview = GetActiveInfoview(),
			m_Buffer = m_BarrierSystem.CreateCommandBuffer().AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, query, base.Dependency);
		m_BarrierSystem.AddJobHandleForProducer(base.Dependency);
	}

	private Entity GetActiveInfoview()
	{
		if (m_ToolSystem.activeInfoview == null)
		{
			return Entity.Null;
		}
		return m_PrefabSystem.GetEntity(m_ToolSystem.activeInfoview);
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
	public TutorialInfoviewDeactivationSystem()
	{
	}
}
