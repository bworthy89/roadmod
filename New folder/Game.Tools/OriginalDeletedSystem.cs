using System.Runtime.CompilerServices;
using Game.Common;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class OriginalDeletedSystem : GameSystemBase
{
	[BurstCompile]
	private struct OriginalDeletedJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[NativeDisableParallelForRestriction]
		public NativeArray<bool> m_OriginalDeleted;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Temp> nativeArray = chunk.GetNativeArray(ref m_TempType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Temp temp = nativeArray[i];
				if (temp.m_Original != Entity.Null)
				{
					if (m_DeletedData.HasComponent(temp.m_Original))
					{
						m_OriginalDeleted[1] = true;
					}
					else if (!m_EntityLookup.Exists(temp.m_Original))
					{
						m_OriginalDeleted[0] = true;
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
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
		}
	}

	private EntityQuery m_TempQuery;

	private NativeArray<bool> m_OriginalDeleted;

	private JobHandle m_Dependency;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TempQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.Exclude<Deleted>());
		m_OriginalDeleted = new NativeArray<bool>(2, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_OriginalDeleted.Dispose();
		base.OnDestroy();
	}

	public bool GetOriginalDeletedResult(int delay)
	{
		m_Dependency.Complete();
		for (int num = 1 - delay; num >= 0; num--)
		{
			if (m_OriginalDeleted[num])
			{
				return true;
			}
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_OriginalDeleted[0] = m_OriginalDeleted[1];
		m_OriginalDeleted[1] = false;
		if (!m_TempQuery.IsEmptyIgnoreFilter)
		{
			base.Dependency = (m_Dependency = JobChunkExtensions.ScheduleParallel(new OriginalDeletedJob
			{
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
				m_OriginalDeleted = m_OriginalDeleted
			}, m_TempQuery, base.Dependency));
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
	public OriginalDeletedSystem()
	{
	}
}
