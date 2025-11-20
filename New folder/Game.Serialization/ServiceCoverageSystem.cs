using System.Runtime.CompilerServices;
using Game.Net;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class ServiceCoverageSystem : GameSystemBase
{
	[BurstCompile]
	private struct ServiceCoverageJob : IJobChunk
	{
		public BufferTypeHandle<ServiceCoverage> m_CoverageType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<ServiceCoverage> bufferAccessor = chunk.GetBufferAccessor(ref m_CoverageType);
			for (int i = 0; i < chunk.Count; i++)
			{
				DynamicBuffer<ServiceCoverage> dynamicBuffer = bufferAccessor[i];
				int num = 9 - dynamicBuffer.Length;
				if (num > 0)
				{
					dynamicBuffer.Capacity = 9;
					for (int j = 0; j < num; j++)
					{
						dynamicBuffer.Add(default(ServiceCoverage));
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
		public BufferTypeHandle<ServiceCoverage> __Game_Net_ServiceCoverage_RW_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_ServiceCoverage_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceCoverage>();
		}
	}

	private EntityQuery m_Query;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_Query = GetEntityQuery(ComponentType.ReadOnly<ServiceCoverage>());
		RequireForUpdate(m_Query);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		ServiceCoverageJob jobData = new ServiceCoverageJob
		{
			m_CoverageType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ServiceCoverage_RW_BufferTypeHandle, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_Query, base.Dependency);
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
	public ServiceCoverageSystem()
	{
	}
}
