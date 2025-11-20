using System.Runtime.CompilerServices;
using Game.Common;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Areas;

[CompilerGenerated]
public class ServiceDistrictSystem : GameSystemBase
{
	[BurstCompile]
	private struct RemoveServiceDistrictsJob : IJobChunk
	{
		[ReadOnly]
		public NativeList<Entity> m_DeletedDistricts;

		public BufferTypeHandle<ServiceDistrict> m_ServiceDistrictType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<ServiceDistrict> bufferAccessor = chunk.GetBufferAccessor(ref m_ServiceDistrictType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<ServiceDistrict> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Entity district = dynamicBuffer[j].m_District;
					for (int k = 0; k < m_DeletedDistricts.Length; k++)
					{
						if (m_DeletedDistricts[k] == district)
						{
							dynamicBuffer.RemoveAt(j--);
							break;
						}
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
		public BufferTypeHandle<ServiceDistrict> __Game_Areas_ServiceDistrict_RW_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Areas_ServiceDistrict_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceDistrict>();
		}
	}

	private EntityQuery m_DeletedDistrictQuery;

	private EntityQuery m_ServiceDistrictQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_DeletedDistrictQuery = GetEntityQuery(ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<District>(), ComponentType.Exclude<Temp>());
		m_ServiceDistrictQuery = GetEntityQuery(ComponentType.ReadOnly<ServiceDistrict>(), ComponentType.Exclude<Deleted>());
		RequireForUpdate(m_DeletedDistrictQuery);
		RequireForUpdate(m_ServiceDistrictQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<Entity> deletedDistricts = m_DeletedDistrictQuery.ToEntityListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new RemoveServiceDistrictsJob
		{
			m_DeletedDistricts = deletedDistricts,
			m_ServiceDistrictType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_ServiceDistrict_RW_BufferTypeHandle, ref base.CheckedStateRef)
		}, m_ServiceDistrictQuery, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
		deletedDistricts.Dispose(jobHandle);
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
	public ServiceDistrictSystem()
	{
	}
}
