using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class OfficeAISystem : GameSystemBase
{
	private struct OfficeResourceConsumptionEvent
	{
		public Resource resource;

		public int amount;
	}

	[BurstCompile]
	private struct ResetOfficeConsumptionJob : IJob
	{
		public NativeQueue<OfficeResourceConsumptionEvent> m_OfficeResourceConsumptionQueue;

		public NativeArray<int> m_OfficeResourceConsumeAccumulator;

		public NativeReference<int> m_OfficeConsumedAmount;

		public void Execute()
		{
			m_OfficeConsumedAmount.Value = 0;
			OfficeResourceConsumptionEvent item;
			while (m_OfficeResourceConsumptionQueue.TryDequeue(out item))
			{
				m_OfficeResourceConsumeAccumulator[EconomyUtils.GetResourceIndex(item.resource)] += item.amount;
			}
		}
	}

	[BurstCompile]
	private struct OfficeAIJob : IJobChunk
	{
		[ReadOnly]
		public NativeReference<int> m_OfficeConsumedAmount;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> m_PropertyType;

		public BufferTypeHandle<Resources> m_ResourceType;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		public NativeQueue<OfficeResourceConsumptionEvent>.ParallelWriter m_OfficeResourceConsumptionQueue;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public EconomyParameterData m_EconomyParameters;

		public int m_OfficeEntityCount;

		public uint m_UpdateFrameIndex;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
			BufferAccessor<Resources> bufferAccessor = chunk.GetBufferAccessor(ref m_ResourceType);
			NativeArray<PropertyRenter> nativeArray3 = chunk.GetNativeArray(ref m_PropertyType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity e = nativeArray[i];
				Entity prefab = nativeArray2[i].m_Prefab;
				Entity property = nativeArray3[i].m_Property;
				if (m_Buildings.HasComponent(property) && m_IndustrialProcessDatas.TryGetComponent(prefab, out var componentData))
				{
					DynamicBuffer<Resources> resources = bufferAccessor[i];
					Resource resource = componentData.m_Output.m_Resource;
					int resources2 = EconomyUtils.GetResources(resource, resources);
					if (resources2 <= kMinStorageAllow)
					{
						break;
					}
					int num = math.min(resources2, (int)math.ceil((float)m_OfficeConsumedAmount.Value / (float)m_OfficeEntityCount) * m_EconomyParameters.m_OfficeResourceConsumedPerIndustrialUnit);
					int num2 = EconomyUtils.AddResources(resource, -num, resources);
					int amount = (int)math.ceil((float)num * EconomyUtils.GetIndustrialPrice(resource, m_ResourcePrefabs, ref m_ResourceDatas));
					EconomyUtils.AddResources(Resource.Money, amount, resources);
					m_OfficeResourceConsumptionQueue.Enqueue(new OfficeResourceConsumptionEvent
					{
						resource = resource,
						amount = num
					});
					int num3 = (int)((float)(IndustrialAISystem.kMaxVirtualResourceStorage * 2) / 3f);
					if (num2 > num3 + kMinimumTradeResource)
					{
						int y = num2 - num3;
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, new ResourceExporter
						{
							m_Resource = resource,
							m_Amount = math.max(0, y)
						});
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
		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
		}
	}

	private static readonly int kUpdatesPerDay = 32;

	private static readonly int kMinimumTradeResource = 2000;

	private static readonly int kMinStorageAllow = 30000;

	private NativeReference<int> m_IndustrialCommercialResourceConsumptionAmount;

	private EntityQuery m_OfficeCompanyGroup;

	private ResourceSystem m_ResourceSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private CityProductionStatisticSystem m_CityProductionStatisticSystem;

	private JobHandle m_WriteConsumptionDeps;

	private NativeQueue<OfficeResourceConsumptionEvent> m_OfficeResourceConsumptionQueue;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_2010180795_0;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	public NativeReference<int> GetIndustrialConsumptionAmount(out JobHandle deps)
	{
		deps = m_WriteConsumptionDeps;
		return m_IndustrialCommercialResourceConsumptionAmount;
	}

	public void AddWriteConsumptionDeps(JobHandle deps)
	{
		m_WriteConsumptionDeps = JobHandle.CombineDependencies(m_WriteConsumptionDeps, deps);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CityProductionStatisticSystem = base.World.GetOrCreateSystemManaged<CityProductionStatisticSystem>();
		m_OfficeCompanyGroup = GetEntityQuery(ComponentType.ReadWrite<Resources>(), ComponentType.ReadOnly<Game.Companies.ProcessingCompany>(), ComponentType.ReadOnly<PropertyRenter>(), ComponentType.ReadOnly<OfficeCompany>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Employee>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Game.Companies.ExtractorCompany>());
		m_IndustrialCommercialResourceConsumptionAmount = new NativeReference<int>(Allocator.Persistent);
		m_OfficeResourceConsumptionQueue = new NativeQueue<OfficeResourceConsumptionEvent>(Allocator.Persistent);
		RequireForUpdate(m_OfficeCompanyGroup);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_IndustrialCommercialResourceConsumptionAmount.Dispose();
		m_OfficeResourceConsumptionQueue.Dispose();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
		int officeEntityCount = m_OfficeCompanyGroup.CalculateEntityCount();
		JobHandle job = JobChunkExtensions.ScheduleParallel(new OfficeAIJob
		{
			m_OfficeConsumedAmount = m_IndustrialCommercialResourceConsumptionAmount,
			m_EntityType = GetEntityTypeHandle(),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabType = GetComponentTypeHandle<PrefabRef>(isReadOnly: true),
			m_PropertyType = GetComponentTypeHandle<PropertyRenter>(isReadOnly: true),
			m_ResourceType = GetBufferTypeHandle<Resources>(),
			m_IndustrialProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Buildings = GetComponentLookup<Building>(isReadOnly: true),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_OfficeResourceConsumptionQueue = m_OfficeResourceConsumptionQueue.AsParallelWriter(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_EconomyParameters = __query_2010180795_0.GetSingleton<EconomyParameterData>(),
			m_OfficeEntityCount = officeEntityCount,
			m_UpdateFrameIndex = updateFrame
		}, m_OfficeCompanyGroup, JobHandle.CombineDependencies(m_WriteConsumptionDeps, base.Dependency));
		JobHandle deps;
		ResetOfficeConsumptionJob jobData = new ResetOfficeConsumptionJob
		{
			m_OfficeConsumedAmount = m_IndustrialCommercialResourceConsumptionAmount,
			m_OfficeResourceConsumptionQueue = m_OfficeResourceConsumptionQueue,
			m_OfficeResourceConsumeAccumulator = m_CityProductionStatisticSystem.GetCityResourceUsageAccumulator(CityProductionStatisticSystem.CityResourceUsage.Consumer.Industrial, out deps)
		};
		base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(job, deps));
		AddWriteConsumptionDeps(base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		m_ResourceSystem.AddPrefabsReader(base.Dependency);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<EconomyParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_2010180795_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public OfficeAISystem()
	{
	}
}
