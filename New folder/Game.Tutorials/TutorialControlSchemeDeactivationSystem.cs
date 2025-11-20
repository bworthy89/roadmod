using System.Runtime.CompilerServices;
using Game.Input;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Tutorials;

[CompilerGenerated]
public class TutorialControlSchemeDeactivationSystem : TutorialDeactivationSystemBase
{
	[BurstCompile]
	private struct DeactivateJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<ControlSchemeActivationData> m_ControlSchemeDeactivationType;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public InputManager.ControlScheme m_ControlScheme;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<ControlSchemeActivationData> nativeArray = chunk.GetNativeArray(ref m_ControlSchemeDeactivationType);
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (nativeArray[i].m_ControlScheme != m_ControlScheme)
				{
					m_CommandBuffer.RemoveComponent<TutorialActivated>(unfilteredChunkIndex, nativeArray2[i]);
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
		public ComponentTypeHandle<ControlSchemeActivationData> __Game_Tutorials_ControlSchemeActivationData_RO_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Tutorials_ControlSchemeActivationData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ControlSchemeActivationData>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
		}
	}

	private EntityQuery m_PendingTutorialQuery;

	private EntityQuery m_ActiveTutorialQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PendingTutorialQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialData>(), ComponentType.ReadOnly<TutorialActivated>(), ComponentType.ReadOnly<ControlSchemeActivationData>(), ComponentType.Exclude<TutorialActive>(), ComponentType.Exclude<TutorialCompleted>(), ComponentType.Exclude<ForceActivation>());
		m_ActiveTutorialQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialData>(), ComponentType.ReadOnly<TutorialActivated>(), ComponentType.ReadOnly<ControlSchemeActivationData>(), ComponentType.ReadOnly<TutorialActive>(), ComponentType.Exclude<TutorialCompleted>(), ComponentType.Exclude<ForceActivation>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (InputManager.instance != null)
		{
			if (!m_PendingTutorialQuery.IsEmptyIgnoreFilter)
			{
				CheckDeactivate(m_PendingTutorialQuery);
			}
			if (!m_ActiveTutorialQuery.IsEmptyIgnoreFilter && base.phaseCanDeactivate)
			{
				CheckDeactivate(m_ActiveTutorialQuery);
			}
		}
	}

	private void CheckDeactivate(EntityQuery query)
	{
		if (!query.IsEmptyIgnoreFilter)
		{
			DeactivateJob jobData = new DeactivateJob
			{
				m_ControlSchemeDeactivationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tutorials_ControlSchemeActivationData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_ControlScheme = InputManager.instance.activeControlScheme,
				m_CommandBuffer = m_BarrierSystem.CreateCommandBuffer().AsParallelWriter()
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, query, base.Dependency);
			m_BarrierSystem.AddJobHandleForProducer(base.Dependency);
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
	public TutorialControlSchemeDeactivationSystem()
	{
	}
}
