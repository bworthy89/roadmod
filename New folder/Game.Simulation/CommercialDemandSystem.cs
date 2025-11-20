using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Debug;
using Game.Economy;
using Game.Prefabs;
using Game.Prefabs.Modes;
using Game.Reflection;
using Game.Tools;
using Game.Zones;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CommercialDemandSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	[BurstCompile]
	private struct UpdateCommercialDemandJob : IJob
	{
		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<ZoneData> m_UnlockedZoneDatas;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_CommercialPropertyChunks;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public BufferTypeHandle<Renter> m_RenterType;

		[ReadOnly]
		public ComponentTypeHandle<PropertyOnMarket> m_PropertyOnMarketType;

		[ReadOnly]
		public ComponentLookup<Population> m_Populations;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDatas;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<CommercialCompany> m_CommercialCompanies;

		[ReadOnly]
		public ComponentLookup<Tourism> m_Tourisms;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public DemandParameterData m_DemandParameters;

		[ReadOnly]
		public Entity m_City;

		[ReadOnly]
		public NativeArray<int> m_TaxRates;

		public NativeValue<int> m_CompanyDemand;

		public NativeValue<int> m_BuildingDemand;

		public NativeArray<int> m_DemandFactors;

		public NativeArray<int> m_FreeProperties;

		public NativeArray<int> m_ResourceDemands;

		public NativeArray<int> m_BuildingDemands;

		[ReadOnly]
		public NativeArray<int> m_ProduceCapacity;

		[ReadOnly]
		public NativeArray<int> m_CurrentAvailables;

		[ReadOnly]
		public NativeArray<int> m_Propertyless;

		public float m_CommercialTaxEffectDemandOffset;

		public bool m_UnlimitedDemand;

		public void Execute()
		{
			bool flag = false;
			for (int i = 0; i < m_UnlockedZoneDatas.Length; i++)
			{
				if (m_UnlockedZoneDatas[i].m_AreaType == AreaType.Commercial)
				{
					flag = true;
					break;
				}
			}
			ResourceIterator iterator = ResourceIterator.GetIterator();
			while (iterator.Next())
			{
				int resourceIndex = EconomyUtils.GetResourceIndex(iterator.resource);
				m_FreeProperties[resourceIndex] = 0;
				m_BuildingDemands[resourceIndex] = 0;
				m_ResourceDemands[resourceIndex] = 0;
			}
			for (int j = 0; j < m_DemandFactors.Length; j++)
			{
				m_DemandFactors[j] = 0;
			}
			for (int k = 0; k < m_CommercialPropertyChunks.Length; k++)
			{
				ArchetypeChunk archetypeChunk = m_CommercialPropertyChunks[k];
				if (!archetypeChunk.Has(ref m_PropertyOnMarketType))
				{
					continue;
				}
				NativeArray<PrefabRef> nativeArray = archetypeChunk.GetNativeArray(ref m_PrefabType);
				BufferAccessor<Renter> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_RenterType);
				for (int l = 0; l < nativeArray.Length; l++)
				{
					Entity prefab = nativeArray[l].m_Prefab;
					if (!m_BuildingPropertyDatas.HasComponent(prefab))
					{
						continue;
					}
					bool flag2 = false;
					DynamicBuffer<Renter> dynamicBuffer = bufferAccessor[l];
					for (int m = 0; m < dynamicBuffer.Length; m++)
					{
						if (m_CommercialCompanies.HasComponent(dynamicBuffer[m].m_Renter))
						{
							flag2 = true;
							break;
						}
					}
					if (flag2)
					{
						continue;
					}
					BuildingPropertyData buildingPropertyData = m_BuildingPropertyDatas[prefab];
					ResourceIterator iterator2 = ResourceIterator.GetIterator();
					while (iterator2.Next())
					{
						if ((buildingPropertyData.m_AllowedSold & iterator2.resource) != Resource.NoResource)
						{
							m_FreeProperties[EconomyUtils.GetResourceIndex(iterator2.resource)]++;
						}
					}
				}
			}
			m_CompanyDemand.value = 0;
			m_BuildingDemand.value = 0;
			int population = m_Populations[m_City].m_Population;
			iterator = ResourceIterator.GetIterator();
			int num = 0;
			while (iterator.Next())
			{
				int resourceIndex2 = EconomyUtils.GetResourceIndex(iterator.resource);
				if (!EconomyUtils.IsCommercialResource(iterator.resource) || !m_ResourceDatas.HasComponent(m_ResourcePrefabs[iterator.resource]))
				{
					continue;
				}
				float num2 = -0.05f * ((float)TaxSystem.GetCommercialTaxRate(iterator.resource, m_TaxRates) - 10f) * m_DemandParameters.m_TaxEffect.y;
				num2 += m_CommercialTaxEffectDemandOffset;
				if (iterator.resource != Resource.Lodging)
				{
					int num3 = ((population <= 1000) ? 2500 : (2500 * (int)Mathf.Log10(0.01f * (float)population)));
					m_ResourceDemands[resourceIndex2] = math.clamp(100 - (m_CurrentAvailables[resourceIndex2] - num3) / 25, 0, 100);
				}
				else if (math.max((int)((float)m_Tourisms[m_City].m_CurrentTourists * m_DemandParameters.m_HotelRoomPercentRequirement) - m_Tourisms[m_City].m_Lodging.y, 0) > 0)
				{
					m_ResourceDemands[resourceIndex2] = 100;
				}
				m_ResourceDemands[resourceIndex2] = Mathf.RoundToInt((1f + num2) * (float)m_ResourceDemands[resourceIndex2]);
				int num4 = Mathf.RoundToInt(100f * num2);
				m_DemandFactors[11] += num4;
				if (m_ResourceDemands[resourceIndex2] > 0)
				{
					m_CompanyDemand.value += m_ResourceDemands[resourceIndex2];
					m_BuildingDemands[resourceIndex2] = ((m_FreeProperties[resourceIndex2] - m_Propertyless[resourceIndex2] <= 0) ? m_ResourceDemands[resourceIndex2] : 0);
					if (m_BuildingDemands[resourceIndex2] > 0)
					{
						m_BuildingDemand.value += m_BuildingDemands[resourceIndex2];
					}
					int num5 = ((m_BuildingDemands[resourceIndex2] > 0) ? m_ResourceDemands[resourceIndex2] : 0);
					int num6 = m_ResourceDemands[resourceIndex2];
					int num7 = num6 + num4;
					if (iterator.resource == Resource.Lodging)
					{
						m_DemandFactors[9] += num6;
					}
					else if (iterator.resource == Resource.Petrochemicals)
					{
						m_DemandFactors[16] += num6;
					}
					else
					{
						m_DemandFactors[4] += num6;
					}
					m_DemandFactors[13] += math.min(0, num5 - num7);
					num++;
				}
			}
			m_DemandFactors[4] = ((m_DemandFactors[4] == 0) ? (-1) : m_DemandFactors[4]);
			if (population <= 0)
			{
				m_DemandFactors[4] = 0;
				m_DemandFactors[18] = m_BuildingDemand.value;
				m_DemandFactors[16] = 0;
			}
			if (m_CommercialPropertyChunks.Length == 0)
			{
				m_DemandFactors[13] = 0;
			}
			m_CompanyDemand.value = ((num != 0) ? math.clamp(m_CompanyDemand.value / num, 0, 100) : 0);
			m_BuildingDemand.value = ((num != 0 && flag) ? math.clamp(m_BuildingDemand.value / num, 0, 100) : 0);
			if (m_UnlimitedDemand)
			{
				m_BuildingDemand.value = 100;
				m_CompanyDemand.value = 100;
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PropertyOnMarket> __Game_Buildings_PropertyOnMarket_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Population> __Game_City_Population_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CommercialCompany> __Game_Companies_CommercialCompany_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Tourism> __Game_City_Tourism_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(isReadOnly: true);
			__Game_Buildings_PropertyOnMarket_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyOnMarket>(isReadOnly: true);
			__Game_City_Population_RO_ComponentLookup = state.GetComponentLookup<Population>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Companies_CommercialCompany_RO_ComponentLookup = state.GetComponentLookup<CommercialCompany>(isReadOnly: true);
			__Game_City_Tourism_RO_ComponentLookup = state.GetComponentLookup<Tourism>(isReadOnly: true);
		}
	}

	private ResourceSystem m_ResourceSystem;

	private TaxSystem m_TaxSystem;

	private CountCompanyDataSystem m_CountCompanyDataSystem;

	private CountHouseholdDataSystem m_CountHouseholdDataSystem;

	private CitySystem m_CitySystem;

	private EntityQuery m_EconomyParameterQuery;

	private EntityQuery m_DemandParameterQuery;

	private EntityQuery m_CommercialQuery;

	private EntityQuery m_CommercialProcessDataQuery;

	private EntityQuery m_UnlockedZoneDataQuery;

	private EntityQuery m_GameModeSettingQuery;

	private NativeValue<int> m_CompanyDemand;

	private NativeValue<int> m_BuildingDemand;

	[EnumArray(typeof(DemandFactor))]
	[DebugWatchValue]
	private NativeArray<int> m_DemandFactors;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_ResourceDemands;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_BuildingDemands;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_Consumption;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_FreeProperties;

	[DebugWatchDeps]
	private JobHandle m_WriteDependencies;

	private JobHandle m_ReadDependencies;

	private int m_LastCompanyDemand;

	private int m_LastBuildingDemand;

	private float m_CommercialTaxEffectDemandOffset;

	private bool m_UnlimitedDemand;

	private TypeHandle __TypeHandle;

	[DebugWatchValue(color = "#008fff")]
	public int companyDemand => m_LastCompanyDemand;

	[DebugWatchValue(color = "#2b6795")]
	public int buildingDemand => m_LastBuildingDemand;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 4;
	}

	public void SetUnlimitedDemand(bool unlimited)
	{
		m_UnlimitedDemand = unlimited;
	}

	public NativeArray<int> GetDemandFactors(out JobHandle deps)
	{
		deps = m_WriteDependencies;
		return m_DemandFactors;
	}

	public NativeArray<int> GetResourceDemands(out JobHandle deps)
	{
		deps = m_WriteDependencies;
		return m_ResourceDemands;
	}

	public NativeArray<int> GetBuildingDemands(out JobHandle deps)
	{
		deps = m_WriteDependencies;
		return m_BuildingDemands;
	}

	public NativeArray<int> GetConsumption(out JobHandle deps)
	{
		deps = m_WriteDependencies;
		return m_Consumption;
	}

	public void AddReader(JobHandle reader)
	{
		m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, reader);
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		m_Consumption.Fill(0);
		if (m_GameModeSettingQuery.IsEmptyIgnoreFilter)
		{
			m_CommercialTaxEffectDemandOffset = 0f;
			return;
		}
		ModeSettingData singleton = m_GameModeSettingQuery.GetSingleton<ModeSettingData>();
		if (singleton.m_Enable)
		{
			m_CommercialTaxEffectDemandOffset = singleton.m_CommercialTaxEffectDemandOffset;
		}
		else
		{
			m_CommercialTaxEffectDemandOffset = 0f;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
		m_CountCompanyDataSystem = base.World.GetOrCreateSystemManaged<CountCompanyDataSystem>();
		m_CountHouseholdDataSystem = base.World.GetOrCreateSystemManaged<CountHouseholdDataSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		m_DemandParameterQuery = GetEntityQuery(ComponentType.ReadOnly<DemandParameterData>());
		m_CommercialQuery = GetEntityQuery(ComponentType.ReadOnly<CommercialProperty>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Abandoned>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Condemned>(), ComponentType.Exclude<Temp>());
		m_CommercialProcessDataQuery = GetEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>(), ComponentType.ReadOnly<ServiceCompanyData>());
		m_UnlockedZoneDataQuery = GetEntityQuery(ComponentType.ReadOnly<ZoneData>(), ComponentType.Exclude<Locked>());
		m_GameModeSettingQuery = GetEntityQuery(ComponentType.ReadOnly<ModeSettingData>());
		m_CompanyDemand = new NativeValue<int>(Allocator.Persistent);
		m_BuildingDemand = new NativeValue<int>(Allocator.Persistent);
		m_DemandFactors = new NativeArray<int>(19, Allocator.Persistent);
		int resourceCount = EconomyUtils.ResourceCount;
		m_ResourceDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_BuildingDemands = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_Consumption = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_FreeProperties = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_CommercialTaxEffectDemandOffset = 0f;
		RequireForUpdate(m_EconomyParameterQuery);
		RequireForUpdate(m_DemandParameterQuery);
		RequireForUpdate(m_CommercialProcessDataQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_CompanyDemand.Dispose();
		m_BuildingDemand.Dispose();
		m_DemandFactors.Dispose();
		m_ResourceDemands.Dispose();
		m_BuildingDemands.Dispose();
		m_Consumption.Dispose();
		m_FreeProperties.Dispose();
		base.OnDestroy();
	}

	public void SetDefaults(Context context)
	{
		m_CompanyDemand.value = 0;
		m_BuildingDemand.value = 0;
		m_DemandFactors.Fill(0);
		m_ResourceDemands.Fill(0);
		m_BuildingDemands.Fill(0);
		m_Consumption.Fill(0);
		m_FreeProperties.Fill(0);
		m_LastCompanyDemand = 0;
		m_LastBuildingDemand = 0;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int value = m_CompanyDemand.value;
		writer.Write(value);
		int value2 = m_BuildingDemand.value;
		writer.Write(value2);
		int length = m_DemandFactors.Length;
		writer.Write(length);
		NativeArray<int> value3 = m_DemandFactors;
		writer.Write(value3);
		NativeArray<int> value4 = m_ResourceDemands;
		writer.Write(value4);
		NativeArray<int> value5 = m_BuildingDemands;
		writer.Write(value5);
		NativeArray<int> value6 = m_Consumption;
		writer.Write(value6);
		NativeArray<int> value7 = m_FreeProperties;
		writer.Write(value7);
		int value8 = m_LastCompanyDemand;
		writer.Write(value8);
		int value9 = m_LastBuildingDemand;
		writer.Write(value9);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out int value);
		m_CompanyDemand.value = value;
		reader.Read(out int value2);
		m_BuildingDemand.value = value2;
		if (reader.context.version < Version.demandFactorCountSerialization)
		{
			NativeArray<int> nativeArray = new NativeArray<int>(13, Allocator.Temp);
			NativeArray<int> value3 = nativeArray;
			reader.Read(value3);
			CollectionUtils.CopySafe(nativeArray, m_DemandFactors);
			nativeArray.Dispose();
		}
		else
		{
			reader.Read(out int value4);
			if (value4 == m_DemandFactors.Length)
			{
				NativeArray<int> value5 = m_DemandFactors;
				reader.Read(value5);
			}
			else
			{
				NativeArray<int> nativeArray2 = new NativeArray<int>(value4, Allocator.Temp);
				NativeArray<int> value6 = nativeArray2;
				reader.Read(value6);
				CollectionUtils.CopySafe(nativeArray2, m_DemandFactors);
				nativeArray2.Dispose();
			}
		}
		if (reader.context.format.Has(FormatTags.FishResource))
		{
			NativeArray<int> value7 = m_ResourceDemands;
			reader.Read(value7);
			NativeArray<int> value8 = m_BuildingDemands;
			reader.Read(value8);
		}
		else
		{
			NativeArray<int> subArray = m_ResourceDemands.GetSubArray(0, 40);
			reader.Read(subArray);
			NativeArray<int> subArray2 = m_BuildingDemands.GetSubArray(0, 40);
			reader.Read(subArray2);
			m_ResourceDemands[40] = 0;
			m_BuildingDemands[40] = 0;
		}
		NativeArray<int> nativeArray3 = default(NativeArray<int>);
		if (reader.context.version < Version.companyDemandOptimization)
		{
			nativeArray3 = new NativeArray<int>(EconomyUtils.ResourceCount, Allocator.Temp);
			NativeArray<int> value9 = nativeArray3;
			reader.Read(value9);
		}
		if (reader.context.format.Has(FormatTags.FishResource))
		{
			NativeArray<int> value10 = m_Consumption;
			reader.Read(value10);
		}
		else
		{
			NativeArray<int> subArray3 = m_Consumption.GetSubArray(0, 40);
			reader.Read(subArray3);
			m_Consumption[40] = 0;
		}
		if (reader.context.version < Version.companyDemandOptimization)
		{
			NativeArray<int> value11 = nativeArray3;
			reader.Read(value11);
			NativeArray<int> value12 = nativeArray3;
			reader.Read(value12);
			NativeArray<int> value13 = nativeArray3;
			reader.Read(value13);
		}
		if (reader.context.format.Has(FormatTags.FishResource))
		{
			NativeArray<int> value14 = m_FreeProperties;
			reader.Read(value14);
		}
		else
		{
			NativeArray<int> subArray4 = m_FreeProperties.GetSubArray(0, 40);
			reader.Read(subArray4);
			m_FreeProperties[40] = 0;
		}
		if (reader.context.version < Version.companyDemandOptimization)
		{
			NativeArray<int> value15 = nativeArray3;
			reader.Read(value15);
			nativeArray3.Dispose();
		}
		ref int value16 = ref m_LastCompanyDemand;
		reader.Read(out value16);
		ref int value17 = ref m_LastBuildingDemand;
		reader.Read(out value17);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_DemandParameterQuery.IsEmptyIgnoreFilter && !m_EconomyParameterQuery.IsEmptyIgnoreFilter)
		{
			m_LastCompanyDemand = m_CompanyDemand.value;
			m_LastBuildingDemand = m_BuildingDemand.value;
			JobHandle deps;
			CountCompanyDataSystem.CommercialCompanyDatas commercialCompanyDatas = m_CountCompanyDataSystem.GetCommercialCompanyDatas(out deps);
			JobHandle outJobHandle;
			UpdateCommercialDemandJob jobData = new UpdateCommercialDemandJob
			{
				m_CommercialPropertyChunks = m_CommercialQuery.ToArchetypeChunkListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
				m_UnlockedZoneDatas = m_UnlockedZoneDataQuery.ToComponentDataArray<ZoneData>(Allocator.TempJob),
				m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_RenterType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_PropertyOnMarketType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PropertyOnMarket_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_Populations = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_Population_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingPropertyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CommercialCompanies = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_CommercialCompany_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
				m_DemandParameters = m_DemandParameterQuery.GetSingleton<DemandParameterData>(),
				m_TaxRates = m_TaxSystem.GetTaxRates(),
				m_CompanyDemand = m_CompanyDemand,
				m_BuildingDemand = m_BuildingDemand,
				m_DemandFactors = m_DemandFactors,
				m_City = m_CitySystem.City,
				m_ResourceDemands = m_ResourceDemands,
				m_BuildingDemands = m_BuildingDemands,
				m_ProduceCapacity = commercialCompanyDatas.m_ProduceCapacity,
				m_CurrentAvailables = commercialCompanyDatas.m_CurrentAvailables,
				m_FreeProperties = m_FreeProperties,
				m_Propertyless = commercialCompanyDatas.m_ServicePropertyless,
				m_Tourisms = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_Tourism_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CommercialTaxEffectDemandOffset = m_CommercialTaxEffectDemandOffset,
				m_UnlimitedDemand = m_UnlimitedDemand
			};
			base.Dependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(base.Dependency, m_ReadDependencies, outJobHandle, deps));
			m_WriteDependencies = base.Dependency;
			m_CountHouseholdDataSystem.AddHouseholdDataReader(base.Dependency);
			m_ResourceSystem.AddPrefabsReader(base.Dependency);
			m_TaxSystem.AddReader(base.Dependency);
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
	public CommercialDemandSystem()
	{
	}
}
