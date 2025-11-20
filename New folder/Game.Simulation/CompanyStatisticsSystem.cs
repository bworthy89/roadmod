using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.City;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CompanyStatisticsSystem : GameSystemBase
{
	[BurstCompile]
	private struct ProcessCompanyStatisticsJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<WorkProvider> m_WorkProviderType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public BufferTypeHandle<Employee> m_EmployeeType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentTypeHandle<CommercialCompany> m_CommercialCompanyType;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;

		public uint m_UpdateFrameIndex;

		public NativeQueue<StatisticsEvent>.ParallelWriter m_StatisticsEventQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			bool flag = chunk.Has(ref m_CommercialCompanyType);
			NativeArray<WorkProvider> nativeArray = chunk.GetNativeArray(ref m_WorkProviderType);
			BufferAccessor<Employee> bufferAccessor = chunk.GetBufferAccessor(ref m_EmployeeType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
			NativeArray<int> nativeArray3 = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Temp);
			NativeArray<int> nativeArray4 = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Temp);
			NativeArray<int> nativeArray5 = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Temp);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity prefab = nativeArray2[i].m_Prefab;
				WorkProvider workProvider = nativeArray[i];
				DynamicBuffer<Employee> dynamicBuffer = bufferAccessor[i];
				int resourceIndex = EconomyUtils.GetResourceIndex(m_IndustrialProcessDatas[prefab].m_Output.m_Resource);
				nativeArray5[resourceIndex]++;
				nativeArray4[resourceIndex] += workProvider.m_MaxWorkers;
				nativeArray3[resourceIndex] += dynamicBuffer.Length;
			}
			ResourceIterator iterator = ResourceIterator.GetIterator();
			while (iterator.Next())
			{
				int resourceIndex2 = EconomyUtils.GetResourceIndex(iterator.resource);
				if (nativeArray5[resourceIndex2] > 0)
				{
					bool flag2 = EconomyUtils.IsOfficeResource(iterator.resource);
					m_StatisticsEventQueue.Enqueue(new StatisticsEvent
					{
						m_Statistic = (flag ? StatisticType.ServiceCount : (flag2 ? StatisticType.OfficeCount : StatisticType.ProcessingCount)),
						m_Change = nativeArray5[resourceIndex2],
						m_Parameter = resourceIndex2
					});
					m_StatisticsEventQueue.Enqueue(new StatisticsEvent
					{
						m_Statistic = (flag ? StatisticType.ServiceWorkers : (flag2 ? StatisticType.OfficeWorkers : StatisticType.ProcessingWorkers)),
						m_Change = nativeArray3[resourceIndex2],
						m_Parameter = resourceIndex2
					});
					m_StatisticsEventQueue.Enqueue(new StatisticsEvent
					{
						m_Statistic = (flag ? StatisticType.ServiceMaxWorkers : (flag2 ? StatisticType.OfficeMaxWorkers : StatisticType.ProcessingMaxWorkers)),
						m_Change = nativeArray4[resourceIndex2],
						m_Parameter = resourceIndex2
					});
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
		public ComponentTypeHandle<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Employee> __Game_Companies_Employee_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CommercialCompany> __Game_Companies_CommercialCompany_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Companies_WorkProvider_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WorkProvider>(isReadOnly: true);
			__Game_Companies_Employee_RO_BufferTypeHandle = state.GetBufferTypeHandle<Employee>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Companies_CommercialCompany_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CommercialCompany>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
		}
	}

	private EntityQuery m_CompanyGroup;

	private SimulationSystem m_SimulationSystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 512;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CompanyGroup = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<WorkProvider>(),
				ComponentType.ReadOnly<PropertyRenter>(),
				ComponentType.ReadOnly<UpdateFrame>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<IndustrialCompany>(),
				ComponentType.ReadOnly<CommercialCompany>()
			}
		});
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, 32, 16);
		JobHandle deps;
		ProcessCompanyStatisticsJob jobData = new ProcessCompanyStatisticsJob
		{
			m_WorkProviderType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EmployeeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Companies_Employee_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = GetSharedComponentTypeHandle<UpdateFrame>(),
			m_CommercialCompanyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_IndustrialProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps).AsParallelWriter(),
			m_UpdateFrameIndex = updateFrame
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CompanyGroup, JobHandle.CombineDependencies(base.Dependency, deps));
		m_CityStatisticsSystem.AddWriter(base.Dependency);
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
	public CompanyStatisticsSystem()
	{
	}
}
