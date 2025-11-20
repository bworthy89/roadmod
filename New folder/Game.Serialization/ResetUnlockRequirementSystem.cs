using System.Runtime.CompilerServices;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class ResetUnlockRequirementSystem : GameSystemBase
{
	[BurstCompile]
	private struct ResetUnlockRequirementJob : IJobChunk
	{
		public ComponentTypeHandle<UnlockRequirementData> m_UnlockRequirementDataType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<UnlockRequirementData> nativeArray = chunk.GetNativeArray(ref m_UnlockRequirementDataType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				UnlockRequirementData value = nativeArray[i];
				value.m_Progress = 0;
				nativeArray[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		public ComponentTypeHandle<UnlockRequirementData> __Game_Prefabs_UnlockRequirementData_RW_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_UnlockRequirementData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<UnlockRequirementData>();
		}
	}

	private EntityQuery m_RequirementQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_RequirementQuery = GetEntityQuery(ComponentType.ReadOnly<UnlockRequirementData>());
		RequireForUpdate(m_RequirementQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		ResetUnlockRequirementJob jobData = new ResetUnlockRequirementJob
		{
			m_UnlockRequirementDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_UnlockRequirementData_RW_ComponentTypeHandle, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_RequirementQuery, base.Dependency);
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
	public ResetUnlockRequirementSystem()
	{
	}
}
