using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Debug;
using Game.Economy;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CityProductionCapacityCalculationSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct UpdateProductionCapacityJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<ServiceAvailable> m_ServiceAvailableType;

		[ReadOnly]
		public ComponentTypeHandle<WorkProvider> m_WorkProviderType;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> m_PropertyRenterType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Companies.ExtractorCompany> m_ExtractorCompanyType;

		[ReadOnly]
		public ComponentTypeHandle<CompanyStatisticData> m_CompanyStatisticType;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> m_Workplaces;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcesses;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildings;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public BufferLookup<Efficiency> m_BuildingEfficiencies;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public EconomyParameterData m_EconomyParameterData;

		public NativeQueue<int2>.ParallelWriter m_CapacityQueueWriter;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<WorkProvider> nativeArray2 = chunk.GetNativeArray(ref m_WorkProviderType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<PropertyRenter> nativeArray4 = chunk.GetNativeArray(ref m_PropertyRenterType);
			NativeArray<CompanyStatisticData> nativeArray5 = chunk.GetNativeArray(ref m_CompanyStatisticType);
			bool flag = chunk.Has(ref m_ServiceAvailableType);
			bool flag2 = chunk.Has(ref m_ExtractorCompanyType);
			for (int i = 0; i < chunk.Count; i++)
			{
				_ = nativeArray[i];
				PrefabRef prefabRef = nativeArray3[i];
				WorkProvider workProvider = nativeArray2[i];
				PropertyRenter propertyRenter = nativeArray4[i];
				Entity property = nativeArray4[i].m_Property;
				if (m_IndustrialProcesses.TryGetComponent(prefabRef.m_Prefab, out var componentData) && m_Workplaces.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
				{
					int level = 1;
					if (m_PrefabRefs.TryGetComponent(propertyRenter.m_Property, out var componentData3) && m_SpawnableBuildings.TryGetComponent(componentData3.m_Prefab, out var componentData4))
					{
						level = componentData4.m_Level;
					}
					float num = 1f;
					if (m_BuildingEfficiencies.TryGetBuffer(property, out var bufferData))
					{
						num = BuildingUtils.GetEfficiency(bufferData);
					}
					float num2 = math.max(num, 1f);
					int maxWorkers = workProvider.m_MaxWorkers;
					int y = ((!flag2) ? EconomyUtils.GetCompanyProductionPerDay(num2, maxWorkers, level, !flag, componentData2, componentData, m_ResourcePrefabs, ref m_ResourceDatas, ref m_EconomyParameterData) : ((int)math.ceil((float)nativeArray5[i].m_LastUpdateProduce / num * num2)));
					Resource resource = componentData.m_Output.m_Resource;
					m_CapacityQueueWriter.Enqueue(new int2(EconomyUtils.GetResourceIndex(resource), y));
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct AggregateProductionCapacityJob : IJob
	{
		public NativeQueue<int2> m_CapacityQueue;

		public NativeArray<int> m_ProductionCapacity;

		public void Execute()
		{
			m_ProductionCapacity.Fill(0);
			int2 item;
			while (m_CapacityQueue.TryDequeue(out item))
			{
				m_ProductionCapacity[item.x] += item.y;
			}
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct TypeHandle
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
		}
	}

	public static readonly int kUpdatesPerDay = 32;

	private ResourceSystem m_ResourceSystem;

	private CityProductionStatisticSystem m_CityProductionStatisticSystem;

	private EntityQuery m_CompanyGroup;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_ProductionCapacity;

	private NativeArray<int> m_ProductionCapacityTemp;

	private NativeQueue<int2> m_CapacityQueue;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_1221345834_0;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatesPerDay;
	}

	public int GetProductionCapacity(Resource resource)
	{
		int resourceIndex = EconomyUtils.GetResourceIndex(resource);
		if (resourceIndex < 0 || resourceIndex >= m_ProductionCapacity.Length)
		{
			return 0;
		}
		int y = m_CityProductionStatisticSystem.GetConsumptionProductions()[resourceIndex].y;
		if (y > m_ProductionCapacity[resourceIndex])
		{
			return y;
		}
		return m_ProductionCapacity[resourceIndex];
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityProductionStatisticSystem = base.World.GetOrCreateSystemManaged<CityProductionStatisticSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_CompanyGroup = GetEntityQuery(ComponentType.ReadOnly<CompanyStatisticData>(), ComponentType.ReadOnly<Game.Companies.ProcessingCompany>(), ComponentType.ReadOnly<PropertyRenter>(), ComponentType.ReadOnly<Resources>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<WorkProvider>(), ComponentType.ReadOnly<Employee>(), ComponentType.Exclude<Deleted>());
		m_ProductionCapacity = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Persistent);
		m_CapacityQueue = new NativeQueue<int2>(Allocator.Persistent);
		m_ProductionCapacityTemp = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Persistent);
		RequireForUpdate(m_CompanyGroup);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_ProductionCapacity.CopyFrom(m_ProductionCapacityTemp);
		JobHandle dependsOn = JobChunkExtensions.ScheduleParallel(new UpdateProductionCapacityJob
		{
			m_EntityType = GetEntityTypeHandle(),
			m_PrefabRefType = GetComponentTypeHandle<PrefabRef>(isReadOnly: true),
			m_ServiceAvailableType = GetComponentTypeHandle<ServiceAvailable>(isReadOnly: true),
			m_WorkProviderType = GetComponentTypeHandle<WorkProvider>(isReadOnly: true),
			m_PropertyRenterType = GetComponentTypeHandle<PropertyRenter>(isReadOnly: true),
			m_ExtractorCompanyType = GetComponentTypeHandle<Game.Companies.ExtractorCompany>(isReadOnly: true),
			m_CompanyStatisticType = GetComponentTypeHandle<CompanyStatisticData>(isReadOnly: true),
			m_Workplaces = GetComponentLookup<WorkplaceData>(isReadOnly: true),
			m_IndustrialProcesses = GetComponentLookup<IndustrialProcessData>(isReadOnly: true),
			m_ResourceDatas = GetComponentLookup<ResourceData>(isReadOnly: true),
			m_SpawnableBuildings = GetComponentLookup<SpawnableBuildingData>(isReadOnly: true),
			m_PrefabRefs = GetComponentLookup<PrefabRef>(isReadOnly: true),
			m_BuildingEfficiencies = GetBufferLookup<Efficiency>(isReadOnly: true),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_EconomyParameterData = __query_1221345834_0.GetSingleton<EconomyParameterData>(),
			m_CapacityQueueWriter = m_CapacityQueue.AsParallelWriter()
		}, m_CompanyGroup, base.Dependency);
		JobHandle dependency = IJobExtensions.Schedule(new AggregateProductionCapacityJob
		{
			m_CapacityQueue = m_CapacityQueue,
			m_ProductionCapacity = m_ProductionCapacityTemp
		}, dependsOn);
		base.Dependency = dependency;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_ProductionCapacity.Dispose();
		m_CapacityQueue.Dispose();
		m_ProductionCapacityTemp.Dispose();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_ProductionCapacity);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(m_ProductionCapacity);
	}

	public void SetDefaults(Context context)
	{
		m_ProductionCapacity.Fill(0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<EconomyParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1221345834_0 = entityQueryBuilder2.Build(ref state);
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
	public CityProductionCapacityCalculationSystem()
	{
	}
}
