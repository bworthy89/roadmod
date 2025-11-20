using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Agents;
using Game.Areas;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Prefabs.Modes;
using Game.Serialization;
using Game.Tools;
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
public class TaxSystem : GameSystemBase, ITaxSystem, IDefaultSerializable, ISerializable, IPostDeserialize
{
	[BurstCompile]
	private struct PayTaxJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<TaxPayer> m_TaxPayerType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		public BufferTypeHandle<Resources> m_ResourceType;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public ComponentLookup<Worker> m_Workers;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_ProcessDatas;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		public uint m_UpdateFrameIndex;

		public IncomeSource m_Type;

		public float m_PaidMultiplier;

		public NativeQueue<StatisticsEvent>.ParallelWriter m_StatisticsEventQueue;

		private void PayTax(ref TaxPayer taxPayer, Entity entity, DynamicBuffer<Resources> resources, IncomeSource taxType, NativeQueue<StatisticsEvent>.ParallelWriter statisticsEventQueue)
		{
			int tax = GetTax(taxPayer);
			int num = (int)math.round(m_PaidMultiplier * (float)tax);
			EconomyUtils.AddResources(Resource.Money, -num, resources);
			if (taxType == IncomeSource.TaxResidential)
			{
				int parameter = 0;
				if (m_HouseholdCitizens.HasBuffer(entity))
				{
					DynamicBuffer<HouseholdCitizen> dynamicBuffer = m_HouseholdCitizens[entity];
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						Entity citizen = dynamicBuffer[i].m_Citizen;
						if (m_Workers.HasComponent(citizen))
						{
							parameter = m_Workers[citizen].m_Level;
							break;
						}
					}
				}
				statisticsEventQueue.Enqueue(new StatisticsEvent
				{
					m_Statistic = StatisticType.ResidentialTaxableIncome,
					m_Change = taxPayer.m_UntaxedIncome * kUpdatesPerDay,
					m_Parameter = parameter
				});
			}
			else
			{
				int parameter2 = 0;
				StatisticType statisticType = ((taxType == IncomeSource.TaxCommercial) ? StatisticType.CommercialTaxableIncome : StatisticType.IndustrialTaxableIncome);
				if (m_Prefabs.HasComponent(entity))
				{
					Entity prefab = m_Prefabs[entity].m_Prefab;
					if (m_ProcessDatas.HasComponent(prefab))
					{
						Resource resource = m_ProcessDatas[prefab].m_Output.m_Resource;
						parameter2 = EconomyUtils.GetResourceIndex(resource);
						if (statisticType == StatisticType.IndustrialTaxableIncome && m_ResourceDatas[m_ResourcePrefabs[resource]].m_Weight == 0f)
						{
							taxType = IncomeSource.TaxOffice;
							statisticType = StatisticType.OfficeTaxableIncome;
						}
					}
				}
				statisticsEventQueue.Enqueue(new StatisticsEvent
				{
					m_Statistic = statisticType,
					m_Change = taxPayer.m_UntaxedIncome * kUpdatesPerDay,
					m_Parameter = parameter2
				});
			}
			taxPayer.m_UntaxedIncome = 0;
			taxPayer.m_AverageTaxPaid = tax * kUpdatesPerDay;
		}

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<TaxPayer> nativeArray2 = chunk.GetNativeArray(ref m_TaxPayerType);
			BufferAccessor<Resources> bufferAccessor = chunk.GetBufferAccessor(ref m_ResourceType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				TaxPayer taxPayer = nativeArray2[i];
				DynamicBuffer<Resources> resources = bufferAccessor[i];
				if (taxPayer.m_UntaxedIncome != 0)
				{
					PayTax(ref taxPayer, nativeArray[i], resources, m_Type, m_StatisticsEventQueue);
					nativeArray2[i] = taxPayer;
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
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public ComponentTypeHandle<TaxPayer> __Game_Agents_TaxPayer_RW_ComponentTypeHandle;

		public BufferTypeHandle<Resources> __Game_Economy_Resources_RW_BufferTypeHandle;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Agents_TaxPayer_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TaxPayer>();
			__Game_Economy_Resources_RW_BufferTypeHandle = state.GetBufferTypeHandle<Resources>();
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
			__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
		}
	}

	public static readonly int kUpdatesPerDay = 32;

	private NativeArray<int> m_TaxRates;

	private EntityQuery m_ResidentialTaxPayerGroup;

	private EntityQuery m_CommercialTaxPayerGroup;

	private EntityQuery m_IndustrialTaxPayerGroup;

	private EntityQuery m_TaxParameterGroup;

	private EntityQuery m_GameModeSettingQuery;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private SimulationSystem m_SimulationSystem;

	private ResourceSystem m_ResourceSystem;

	private TaxParameterData m_TaxParameterData;

	private float3 m_TaxPaidMultiplier;

	private JobHandle m_Readers;

	private TypeHandle __TypeHandle;

	public int TaxRate
	{
		get
		{
			return m_TaxRates[0];
		}
		set
		{
			m_TaxRates[0] = math.min(m_TaxParameterData.m_TotalTaxLimits.y, math.max(m_TaxParameterData.m_TotalTaxLimits.x, value));
			EnsureAreaTaxRateLimits(TaxAreaType.Residential);
			EnsureAreaTaxRateLimits(TaxAreaType.Commercial);
			EnsureAreaTaxRateLimits(TaxAreaType.Industrial);
			EnsureAreaTaxRateLimits(TaxAreaType.Office);
		}
	}

	public JobHandle Readers => m_Readers;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		if (m_GameModeSettingQuery.IsEmptyIgnoreFilter)
		{
			m_TaxPaidMultiplier = new float3(1f, 1f, 1f);
			return;
		}
		ModeSettingData singleton = m_GameModeSettingQuery.GetSingleton<ModeSettingData>();
		if (singleton.m_Enable)
		{
			m_TaxPaidMultiplier = singleton.m_TaxPaidMultiplier;
		}
		else
		{
			m_TaxPaidMultiplier = new float3(1f, 1f, 1f);
		}
	}

	public TaxParameterData GetTaxParameterData()
	{
		if (!m_TaxParameterGroup.IsEmptyIgnoreFilter)
		{
			EnsureTaxParameterData();
			return m_TaxParameterData;
		}
		return default(TaxParameterData);
	}

	public static int GetTax(TaxPayer payer)
	{
		return (int)math.round(0.01f * (float)payer.m_AverageTaxRate * (float)payer.m_UntaxedIncome);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int length = m_TaxRates.Length;
		writer.Write(length);
		NativeArray<int> value = m_TaxRates;
		writer.Write(value);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.averageTaxRate)
		{
			int value;
			if (reader.context.version >= Version.taxRateArrayLength)
			{
				reader.Read(out value);
			}
			else
			{
				value = 53;
			}
			NativeArray<int> nativeArray = new NativeArray<int>(value, Allocator.Temp);
			NativeArray<int> value2 = nativeArray;
			reader.Read(value2);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				m_TaxRates[i] = nativeArray[i];
			}
			nativeArray.Dispose();
		}
		if (!reader.context.format.Has(FormatTags.FishResource))
		{
			for (int num = 90; num > 50; num--)
			{
				m_TaxRates[num] = m_TaxRates[num - 1];
			}
			m_TaxRates[10 + EconomyUtils.GetResourceIndex(Resource.Fish)] = 0;
			m_TaxRates[51 + EconomyUtils.GetResourceIndex(Resource.Fish)] = 0;
		}
	}

	public void SetDefaults(Context context)
	{
		m_TaxRates[0] = 10;
		for (int i = 1; i < m_TaxRates.Length; i++)
		{
			m_TaxRates[i] = 0;
		}
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_TaxRates.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_ResidentialTaxPayerGroup = GetEntityQuery(ComponentType.ReadWrite<TaxPayer>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<Resources>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.ReadOnly<Household>());
		m_CommercialTaxPayerGroup = GetEntityQuery(ComponentType.ReadWrite<TaxPayer>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<Resources>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.ReadOnly<ServiceAvailable>());
		m_IndustrialTaxPayerGroup = GetEntityQuery(ComponentType.ReadWrite<TaxPayer>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<Resources>(), ComponentType.ReadOnly<Game.Companies.ProcessingCompany>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Game.Companies.StorageCompany>(), ComponentType.Exclude<ServiceAvailable>());
		m_TaxParameterGroup = GetEntityQuery(ComponentType.ReadOnly<TaxParameterData>());
		m_GameModeSettingQuery = GetEntityQuery(ComponentType.ReadOnly<ModeSettingData>());
		m_TaxRates = new NativeArray<int>(92, Allocator.Persistent);
		m_TaxRates[0] = 10;
		m_TaxPaidMultiplier = new float3(1f, 1f, 1f);
		RequireForUpdate(m_TaxParameterGroup);
	}

	public void PostDeserialize(Context context)
	{
		if (context.version < Version.averageTaxRate)
		{
			m_TaxRates[0] = 10;
			for (int i = 1; i < m_TaxRates.Length; i++)
			{
				m_TaxRates[i] = 0;
			}
		}
	}

	public NativeArray<int> GetTaxRates()
	{
		return m_TaxRates;
	}

	public void AddReader(JobHandle reader)
	{
		m_Readers = JobHandle.CombineDependencies(m_Readers, reader);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		EnsureTaxParameterData();
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
		m_TaxParameterData = m_TaxParameterGroup.GetSingleton<TaxParameterData>();
		ResourcePrefabs prefabs = m_ResourceSystem.GetPrefabs();
		JobHandle deps;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new PayTaxJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TaxPayerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Agents_TaxPayer_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = GetSharedComponentTypeHandle<UpdateFrame>(),
			m_ResourceType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Workers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourcePrefabs = prefabs,
			m_Type = IncomeSource.TaxResidential,
			m_UpdateFrameIndex = updateFrame,
			m_PaidMultiplier = m_TaxPaidMultiplier.x,
			m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps).AsParallelWriter()
		}, m_ResidentialTaxPayerGroup, JobHandle.CombineDependencies(base.Dependency, deps));
		m_CityStatisticsSystem.AddWriter(jobHandle);
		updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
		JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(new PayTaxJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TaxPayerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Agents_TaxPayer_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = GetSharedComponentTypeHandle<UpdateFrame>(),
			m_ResourceType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Workers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourcePrefabs = prefabs,
			m_Type = IncomeSource.TaxCommercial,
			m_UpdateFrameIndex = updateFrame,
			m_PaidMultiplier = m_TaxPaidMultiplier.y,
			m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps).AsParallelWriter()
		}, m_CommercialTaxPayerGroup, JobHandle.CombineDependencies(base.Dependency, deps));
		m_CityStatisticsSystem.AddWriter(jobHandle2);
		JobHandle jobHandle3 = JobChunkExtensions.ScheduleParallel(new PayTaxJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TaxPayerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Agents_TaxPayer_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = GetSharedComponentTypeHandle<UpdateFrame>(),
			m_ResourceType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Workers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourcePrefabs = prefabs,
			m_Type = IncomeSource.TaxIndustrial,
			m_UpdateFrameIndex = updateFrame,
			m_PaidMultiplier = m_TaxPaidMultiplier.z,
			m_StatisticsEventQueue = m_CityStatisticsSystem.GetStatisticsEventQueue(out deps).AsParallelWriter()
		}, m_IndustrialTaxPayerGroup, JobHandle.CombineDependencies(base.Dependency, deps));
		m_CityStatisticsSystem.AddWriter(jobHandle3);
		base.Dependency = JobHandle.CombineDependencies(jobHandle, jobHandle2, jobHandle3);
	}

	public int GetTaxRate(TaxAreaType areaType)
	{
		return GetTaxRate(areaType, m_TaxRates);
	}

	public static int GetTaxRate(TaxAreaType areaType, NativeArray<int> taxRates)
	{
		return taxRates[0] + taxRates[(int)areaType];
	}

	public int2 GetTaxRateRange(TaxAreaType areaType)
	{
		if (areaType == TaxAreaType.Residential)
		{
			return GetTaxRate(areaType) + GetJobLevelTaxRateRange();
		}
		return GetTaxRate(areaType) + GetResourceTaxRateRange(areaType);
	}

	public int GetModifiedTaxRate(TaxAreaType areaType, Entity district, BufferLookup<DistrictModifier> policies)
	{
		return GetModifiedTaxRate(areaType, m_TaxRates, district, policies);
	}

	public static int GetModifiedTaxRate(TaxAreaType areaType, NativeArray<int> taxRates, Entity district, BufferLookup<DistrictModifier> policies)
	{
		float value = GetTaxRate(areaType, taxRates);
		if (policies.HasBuffer(district))
		{
			DynamicBuffer<DistrictModifier> modifiers = policies[district];
			AreaUtils.ApplyModifier(ref value, modifiers, DistrictModifierType.LowCommercialTax);
		}
		return (int)math.round(value);
	}

	private void EnsureTaxParameterData()
	{
		if (m_TaxParameterData.m_TotalTaxLimits.x == m_TaxParameterData.m_TotalTaxLimits.y)
		{
			m_TaxParameterData = m_TaxParameterGroup.GetSingleton<TaxParameterData>();
		}
	}

	public void SetTaxRate(TaxAreaType areaType, int rate)
	{
		m_TaxRates[(int)areaType] = rate - m_TaxRates[0];
		EnsureAreaTaxRateLimits(areaType);
	}

	private void EnsureAreaTaxRateLimits(TaxAreaType areaType)
	{
		switch (areaType)
		{
		case TaxAreaType.Residential:
		{
			int2 officeTaxLimits = m_TaxParameterData.m_ResidentialTaxLimits;
			m_TaxRates[(int)areaType] = math.min(officeTaxLimits.y, math.max(officeTaxLimits.x, GetTaxRate(areaType))) - m_TaxRates[0];
			for (int i = 0; i < 5; i++)
			{
				EnsureJobLevelTaxRateLimits(i);
			}
			break;
		}
		case TaxAreaType.Commercial:
		{
			int2 officeTaxLimits = m_TaxParameterData.m_CommercialTaxLimits;
			m_TaxRates[(int)areaType] = math.min(officeTaxLimits.y, math.max(officeTaxLimits.x, GetTaxRate(areaType))) - m_TaxRates[0];
			ResourceIterator iterator = ResourceIterator.GetIterator();
			while (iterator.Next())
			{
				if (EconomyUtils.IsCommercialResource(iterator.resource))
				{
					EnsureResourceTaxRateLimits(areaType, iterator.resource);
				}
			}
			break;
		}
		case TaxAreaType.Industrial:
		{
			int2 officeTaxLimits = m_TaxParameterData.m_IndustrialTaxLimits;
			m_TaxRates[(int)areaType] = math.min(officeTaxLimits.y, math.max(officeTaxLimits.x, GetTaxRate(areaType))) - m_TaxRates[0];
			ResourceIterator iterator = ResourceIterator.GetIterator();
			while (iterator.Next())
			{
				if (!base.EntityManager.TryGetComponent<ResourceData>(m_ResourceSystem.GetPrefab(iterator.resource), out var component) || (component.m_IsProduceable && component.m_Weight > 0f))
				{
					EnsureResourceTaxRateLimits(areaType, iterator.resource);
				}
			}
			break;
		}
		case TaxAreaType.Office:
		{
			int2 officeTaxLimits = m_TaxParameterData.m_OfficeTaxLimits;
			m_TaxRates[(int)areaType] = math.min(officeTaxLimits.y, math.max(officeTaxLimits.x, GetTaxRate(areaType))) - m_TaxRates[0];
			ResourceIterator iterator = ResourceIterator.GetIterator();
			while (iterator.Next())
			{
				if (EconomyUtils.IsOfficeResource(iterator.resource))
				{
					EnsureResourceTaxRateLimits(areaType, iterator.resource);
				}
			}
			break;
		}
		}
	}

	private void EnsureJobLevelTaxRateLimits(int jobLevel)
	{
		m_TaxRates[5 + jobLevel] = math.min(m_TaxParameterData.m_JobLevelTaxLimits.y, math.max(m_TaxParameterData.m_JobLevelTaxLimits.x, GetResidentialTaxRate(jobLevel))) - GetTaxRate(TaxAreaType.Residential);
	}

	private void ClampResidentialTaxRates()
	{
		int2 jobLevelTaxRateRange = GetJobLevelTaxRateRange();
		int num = 0;
		if (jobLevelTaxRateRange.x > 0)
		{
			num = jobLevelTaxRateRange.x;
		}
		else if (jobLevelTaxRateRange.y < 0)
		{
			num = jobLevelTaxRateRange.y;
		}
		if (num != 0)
		{
			m_TaxRates[1] += num;
			for (int i = 0; i < 5; i++)
			{
				m_TaxRates[5 + i] -= num;
			}
		}
	}

	private int2 GetJobLevelTaxRateRange()
	{
		int2 result = new int2(int.MaxValue, int.MinValue);
		for (int i = 0; i < 5; i++)
		{
			int y = m_TaxRates[5 + i];
			result.x = math.min(result.x, y);
			result.y = math.max(result.y, y);
		}
		return result;
	}

	private void EnsureResourceTaxRateLimits(TaxAreaType areaType, Resource resource)
	{
		switch (areaType)
		{
		case TaxAreaType.Commercial:
		{
			int taxRate = GetTaxRate(TaxAreaType.Commercial);
			m_TaxRates[10 + EconomyUtils.GetResourceIndex(resource)] = math.min(m_TaxParameterData.m_ResourceTaxLimits.y, math.max(m_TaxParameterData.m_ResourceTaxLimits.x, GetCommercialTaxRate(resource))) - taxRate;
			break;
		}
		case TaxAreaType.Industrial:
		{
			int taxRate = GetTaxRate(TaxAreaType.Industrial);
			m_TaxRates[51 + EconomyUtils.GetResourceIndex(resource)] = math.min(m_TaxParameterData.m_ResourceTaxLimits.y, math.max(m_TaxParameterData.m_ResourceTaxLimits.x, GetIndustrialTaxRate(resource))) - taxRate;
			break;
		}
		case TaxAreaType.Office:
		{
			int taxRate = GetTaxRate(TaxAreaType.Office);
			m_TaxRates[51 + EconomyUtils.GetResourceIndex(resource)] = math.min(m_TaxParameterData.m_ResourceTaxLimits.y, math.max(m_TaxParameterData.m_ResourceTaxLimits.x, GetOfficeTaxRate(resource))) - taxRate;
			break;
		}
		}
	}

	private void ClampResourceTaxRates(TaxAreaType areaType)
	{
		int2 resourceTaxRateRange = GetResourceTaxRateRange(areaType);
		int num = 0;
		if (resourceTaxRateRange.x > 0)
		{
			num = resourceTaxRateRange.x;
		}
		else if (resourceTaxRateRange.y < 0)
		{
			num = resourceTaxRateRange.y;
		}
		if (num == 0)
		{
			return;
		}
		m_TaxRates[(int)areaType] += num;
		int zeroOffset = GetZeroOffset(areaType);
		ResourcePrefabs prefabs = m_ResourceSystem.GetPrefabs();
		ResourceIterator iterator = ResourceIterator.GetIterator();
		while (iterator.Next())
		{
			Entity entity = prefabs[iterator.resource];
			if (base.EntityManager.TryGetComponent<TaxableResourceData>(entity, out var component) && component.Contains(areaType))
			{
				m_TaxRates[zeroOffset + EconomyUtils.GetResourceIndex(iterator.resource)] -= num;
			}
		}
	}

	private int2 GetResourceTaxRateRange(TaxAreaType areaType)
	{
		int2 result = new int2(int.MaxValue, int.MinValue);
		ResourcePrefabs prefabs = m_ResourceSystem.GetPrefabs();
		ResourceIterator iterator = ResourceIterator.GetIterator();
		int zeroOffset = GetZeroOffset(areaType);
		while (iterator.Next())
		{
			Entity entity = prefabs[iterator.resource];
			if (base.EntityManager.TryGetComponent<TaxableResourceData>(entity, out var component) && component.Contains(areaType))
			{
				int y = m_TaxRates[zeroOffset + EconomyUtils.GetResourceIndex(iterator.resource)];
				result.x = math.min(result.x, y);
				result.y = math.max(result.y, y);
			}
		}
		return result;
	}

	private int GetZeroOffset(TaxAreaType areaType)
	{
		switch (areaType)
		{
		case TaxAreaType.Commercial:
			return 10;
		case TaxAreaType.Industrial:
		case TaxAreaType.Office:
			return 51;
		default:
			throw new ArgumentOutOfRangeException("areaType", areaType, null);
		}
	}

	public int GetResidentialTaxRate(int jobLevel)
	{
		return GetResidentialTaxRate(jobLevel, m_TaxRates);
	}

	public static int GetResidentialTaxRate(int jobLevel, NativeArray<int> taxRates)
	{
		return GetTaxRate(TaxAreaType.Residential, taxRates) + taxRates[5 + jobLevel];
	}

	public void SetResidentialTaxRate(int jobLevel, int rate)
	{
		m_TaxRates[5 + jobLevel] = rate - GetTaxRate(TaxAreaType.Residential);
		EnsureJobLevelTaxRateLimits(jobLevel);
		ClampResidentialTaxRates();
	}

	public int GetCommercialTaxRate(Resource resource)
	{
		return GetCommercialTaxRate(resource, m_TaxRates);
	}

	public static int GetCommercialTaxRate(Resource resource, NativeArray<int> taxRates)
	{
		return GetTaxRate(TaxAreaType.Commercial, taxRates) + taxRates[10 + EconomyUtils.GetResourceIndex(resource)];
	}

	public int GetModifiedCommercialTaxRate(Resource resource, Entity district, BufferLookup<DistrictModifier> policies)
	{
		return GetModifiedCommercialTaxRate(resource, m_TaxRates, district, policies);
	}

	public static int GetModifiedCommercialTaxRate(Resource resource, NativeArray<int> taxRates, Entity district, BufferLookup<DistrictModifier> policies)
	{
		return GetModifiedTaxRate(TaxAreaType.Commercial, taxRates, district, policies) + taxRates[10 + EconomyUtils.GetResourceIndex(resource)];
	}

	public void SetCommercialTaxRate(Resource resource, int rate)
	{
		m_TaxRates[10 + EconomyUtils.GetResourceIndex(resource)] = rate - GetTaxRate(TaxAreaType.Commercial);
		EnsureResourceTaxRateLimits(TaxAreaType.Commercial, resource);
		ClampResourceTaxRates(TaxAreaType.Commercial);
	}

	public int GetIndustrialTaxRate(Resource resource)
	{
		return GetIndustrialTaxRate(resource, m_TaxRates);
	}

	public static int GetIndustrialTaxRate(Resource resource, NativeArray<int> taxRates)
	{
		return GetTaxRate(TaxAreaType.Industrial, taxRates) + taxRates[51 + EconomyUtils.GetResourceIndex(resource)];
	}

	public void SetIndustrialTaxRate(Resource resource, int rate)
	{
		m_TaxRates[51 + EconomyUtils.GetResourceIndex(resource)] = rate - GetTaxRate(TaxAreaType.Industrial);
		EnsureResourceTaxRateLimits(TaxAreaType.Industrial, resource);
		ClampResourceTaxRates(TaxAreaType.Industrial);
	}

	public int GetOfficeTaxRate(Resource resource)
	{
		return GetOfficeTaxRate(resource, m_TaxRates);
	}

	public static int GetOfficeTaxRate(Resource resource, NativeArray<int> taxRates)
	{
		return GetTaxRate(TaxAreaType.Office, taxRates) + taxRates[51 + EconomyUtils.GetResourceIndex(resource)];
	}

	public void SetOfficeTaxRate(Resource resource, int rate)
	{
		m_TaxRates[51 + EconomyUtils.GetResourceIndex(resource)] = rate - GetTaxRate(TaxAreaType.Office);
		EnsureResourceTaxRateLimits(TaxAreaType.Office, resource);
		ClampResourceTaxRates(TaxAreaType.Office);
	}

	public int GetTaxRateEffect(TaxAreaType areaType, int taxRate)
	{
		return 0;
	}

	public int GetEstimatedTaxAmount(TaxAreaType areaType, TaxResultType resultType, NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> statisticsLookup, BufferLookup<CityStatistic> stats)
	{
		return GetEstimatedTaxAmount(areaType, resultType, statisticsLookup, stats, m_TaxRates);
	}

	public static int GetEstimatedTaxAmount(TaxAreaType areaType, TaxResultType resultType, NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> statisticsLookup, BufferLookup<CityStatistic> stats, NativeArray<int> taxRates)
	{
		int num = 0;
		switch (areaType)
		{
		case TaxAreaType.Residential:
		{
			for (int i = 0; i < 5; i++)
			{
				int estimatedResidentialTaxIncome = GetEstimatedResidentialTaxIncome(i, statisticsLookup, stats, taxRates);
				if (MatchesResultType(estimatedResidentialTaxIncome, resultType))
				{
					num += estimatedResidentialTaxIncome;
				}
			}
			return num;
		}
		case TaxAreaType.Commercial:
		{
			ResourceIterator iterator = ResourceIterator.GetIterator();
			while (iterator.Next())
			{
				int estimatedCommercialTaxIncome = GetEstimatedCommercialTaxIncome(iterator.resource, statisticsLookup, stats, taxRates);
				if (MatchesResultType(estimatedCommercialTaxIncome, resultType))
				{
					num += estimatedCommercialTaxIncome;
				}
			}
			return num;
		}
		case TaxAreaType.Industrial:
		{
			ResourceIterator iterator = ResourceIterator.GetIterator();
			while (iterator.Next())
			{
				int estimatedIndustrialTaxIncome = GetEstimatedIndustrialTaxIncome(iterator.resource, statisticsLookup, stats, taxRates);
				if (MatchesResultType(estimatedIndustrialTaxIncome, resultType))
				{
					num += estimatedIndustrialTaxIncome;
				}
			}
			return num;
		}
		case TaxAreaType.Office:
		{
			ResourceIterator iterator = ResourceIterator.GetIterator();
			while (iterator.Next())
			{
				int estimatedOfficeTaxIncome = GetEstimatedOfficeTaxIncome(iterator.resource, statisticsLookup, stats, taxRates);
				if (MatchesResultType(estimatedOfficeTaxIncome, resultType))
				{
					num += estimatedOfficeTaxIncome;
				}
			}
			return num;
		}
		default:
			return 0;
		}
	}

	private static bool MatchesResultType(int amount, TaxResultType resultType)
	{
		if (resultType != TaxResultType.Any && (resultType != TaxResultType.Income || amount <= 0))
		{
			if (resultType == TaxResultType.Expense)
			{
				return amount < 0;
			}
			return false;
		}
		return true;
	}

	public int GetEstimatedResidentialTaxIncome(int jobLevel, NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> statisticsLookup, BufferLookup<CityStatistic> stats)
	{
		return GetEstimatedResidentialTaxIncome(jobLevel, statisticsLookup, stats, m_TaxRates);
	}

	public int GetEstimatedCommercialTaxIncome(Resource resource, NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> statisticsLookup, BufferLookup<CityStatistic> stats)
	{
		return GetEstimatedCommercialTaxIncome(resource, statisticsLookup, stats, m_TaxRates);
	}

	public int GetEstimatedIndustrialTaxIncome(Resource resource, NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> statisticsLookup, BufferLookup<CityStatistic> stats)
	{
		return GetEstimatedIndustrialTaxIncome(resource, statisticsLookup, stats, m_TaxRates);
	}

	public int GetEstimatedOfficeTaxIncome(Resource resource, NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> statisticsLookup, BufferLookup<CityStatistic> stats)
	{
		return GetEstimatedOfficeTaxIncome(resource, statisticsLookup, stats, m_TaxRates);
	}

	public static int GetEstimatedResidentialTaxIncome(int jobLevel, NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> statisticsLookup, BufferLookup<CityStatistic> stats, NativeArray<int> taxRates)
	{
		return (int)((long)GetResidentialTaxRate(jobLevel, taxRates) * (long)CityStatisticsSystem.GetStatisticValue(statisticsLookup, stats, StatisticType.ResidentialTaxableIncome, jobLevel) / 100);
	}

	public static int GetEstimatedCommercialTaxIncome(Resource resource, NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> statisticsLookup, BufferLookup<CityStatistic> stats, NativeArray<int> taxRates)
	{
		return (int)((long)GetCommercialTaxRate(resource, taxRates) * (long)CityStatisticsSystem.GetStatisticValue(statisticsLookup, stats, StatisticType.CommercialTaxableIncome, EconomyUtils.GetResourceIndex(resource)) / 100);
	}

	public static int GetEstimatedIndustrialTaxIncome(Resource resource, NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> statisticsLookup, BufferLookup<CityStatistic> stats, NativeArray<int> taxRates)
	{
		return (int)((long)GetIndustrialTaxRate(resource, taxRates) * (long)CityStatisticsSystem.GetStatisticValue(statisticsLookup, stats, StatisticType.IndustrialTaxableIncome, EconomyUtils.GetResourceIndex(resource)) / 100);
	}

	public static int GetEstimatedOfficeTaxIncome(Resource resource, NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> statisticsLookup, BufferLookup<CityStatistic> stats, NativeArray<int> taxRates)
	{
		return (int)((long)GetOfficeTaxRate(resource, taxRates) * (long)CityStatisticsSystem.GetStatisticValue(statisticsLookup, stats, StatisticType.OfficeTaxableIncome, EconomyUtils.GetResourceIndex(resource)) / 100);
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
	public TaxSystem()
	{
	}
}
