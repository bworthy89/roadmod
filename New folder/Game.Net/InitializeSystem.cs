using System.Runtime.CompilerServices;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Net;

[CompilerGenerated]
public class InitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeEdgesJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<WaterPipeConnectionData> m_WaterPipeConnectionData;

		public BufferTypeHandle<ServiceCoverage> m_ServiceCoverageType;

		public BufferTypeHandle<ResourceAvailability> m_ResourceAvailabilityType;

		public ComponentTypeHandle<WaterPipeConnection> m_WaterPipeConnectionType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<ServiceCoverage> bufferAccessor = chunk.GetBufferAccessor(ref m_ServiceCoverageType);
			BufferAccessor<ResourceAvailability> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ResourceAvailabilityType);
			NativeArray<WaterPipeConnection> nativeArray2 = chunk.GetNativeArray(ref m_WaterPipeConnectionType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<ServiceCoverage> dynamicBuffer = bufferAccessor[i];
				dynamicBuffer.ResizeUninitialized(9);
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					dynamicBuffer[j] = default(ServiceCoverage);
				}
			}
			for (int k = 0; k < bufferAccessor2.Length; k++)
			{
				DynamicBuffer<ResourceAvailability> dynamicBuffer2 = bufferAccessor2[k];
				dynamicBuffer2.ResizeUninitialized(34);
				for (int l = 0; l < dynamicBuffer2.Length; l++)
				{
					dynamicBuffer2[l] = default(ResourceAvailability);
				}
			}
			for (int m = 0; m < nativeArray2.Length; m++)
			{
				PrefabRef prefabRef = nativeArray[m];
				WaterPipeConnection value = nativeArray2[m];
				value.m_FreshCapacity = m_WaterPipeConnectionData[prefabRef.m_Prefab].m_FreshCapacity;
				value.m_SewageCapacity = m_WaterPipeConnectionData[prefabRef.m_Prefab].m_SewageCapacity;
				value.m_StormCapacity = m_WaterPipeConnectionData[prefabRef.m_Prefab].m_StormCapacity;
				nativeArray2[m] = value;
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<WaterPipeConnectionData> __Game_Prefabs_WaterPipeConnectionData_RO_ComponentLookup;

		public BufferTypeHandle<ServiceCoverage> __Game_Net_ServiceCoverage_RW_BufferTypeHandle;

		public BufferTypeHandle<ResourceAvailability> __Game_Net_ResourceAvailability_RW_BufferTypeHandle;

		public ComponentTypeHandle<WaterPipeConnection> __Game_Net_WaterPipeConnection_RW_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_WaterPipeConnectionData_RO_ComponentLookup = state.GetComponentLookup<WaterPipeConnectionData>(isReadOnly: true);
			__Game_Net_ServiceCoverage_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceCoverage>();
			__Game_Net_ResourceAvailability_RW_BufferTypeHandle = state.GetBufferTypeHandle<ResourceAvailability>();
			__Game_Net_WaterPipeConnection_RW_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPipeConnection>();
		}
	}

	private EntityQuery m_CreatedEdgesQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CreatedEdgesQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Created>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadWrite<ServiceCoverage>(),
				ComponentType.ReadWrite<ResourceAvailability>(),
				ComponentType.ReadWrite<WaterPipeConnection>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		RequireForUpdate(m_CreatedEdgesQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		InitializeEdgesJob jobData = new InitializeEdgesJob
		{
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaterPipeConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WaterPipeConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceCoverageType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ServiceCoverage_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ResourceAvailabilityType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ResourceAvailability_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_WaterPipeConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_WaterPipeConnection_RW_ComponentTypeHandle, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CreatedEdgesQuery, base.Dependency);
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
	public InitializeSystem()
	{
	}
}
