using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class CompleteCullingSystem : GameSystemBase
{
	[BurstCompile]
	private struct CullingCleanupJob : IJob
	{
		public ComponentLookup<CullingInfo> m_CullingInfo;

		public NativeList<PreCullingData> m_CullingData;

		public void Execute()
		{
			for (int i = 0; i < m_CullingData.Length; i++)
			{
				ref PreCullingData reference = ref m_CullingData.ElementAt(i);
				if ((reference.m_Flags & (PreCullingFlags.NearCameraUpdated | PreCullingFlags.Updated | PreCullingFlags.Created | PreCullingFlags.Applied | PreCullingFlags.BatchesUpdated | PreCullingFlags.ColorsUpdated)) == 0)
				{
					continue;
				}
				if ((reference.m_Flags & PreCullingFlags.NearCamera) == 0)
				{
					if ((reference.m_Flags & PreCullingFlags.Deleted) == 0)
					{
						m_CullingInfo.GetRefRW(reference.m_Entity).ValueRW.m_CullingIndex = 0;
					}
					m_CullingData.RemoveAtSwapBack(i);
					if (i < m_CullingData.Length)
					{
						ref PreCullingData reference2 = ref m_CullingData.ElementAt(i);
						if ((reference2.m_Flags & PreCullingFlags.Deleted) == 0)
						{
							m_CullingInfo.GetRefRW(reference2.m_Entity).ValueRW.m_CullingIndex = i;
						}
					}
					i--;
				}
				else
				{
					reference.m_Flags &= ~(PreCullingFlags.NearCameraUpdated | PreCullingFlags.Updated | PreCullingFlags.Created | PreCullingFlags.Applied | PreCullingFlags.BatchesUpdated | PreCullingFlags.ColorsUpdated);
				}
			}
		}
	}

	private struct TypeHandle
	{
		public ComponentLookup<CullingInfo> __Game_Rendering_CullingInfo_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Rendering_CullingInfo_RW_ComponentLookup = state.GetComponentLookup<CullingInfo>();
		}
	}

	private PreCullingSystem m_CullingSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CullingSystem = base.World.GetOrCreateSystemManaged<PreCullingSystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		JobHandle jobHandle = IJobExtensions.Schedule(new CullingCleanupJob
		{
			m_CullingInfo = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_CullingInfo_RW_ComponentLookup, ref base.CheckedStateRef),
			m_CullingData = m_CullingSystem.GetCullingData(readOnly: false, out dependencies)
		}, JobHandle.CombineDependencies(base.Dependency, dependencies));
		m_CullingSystem.AddCullingDataWriter(jobHandle);
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
	public CompleteCullingSystem()
	{
	}
}
