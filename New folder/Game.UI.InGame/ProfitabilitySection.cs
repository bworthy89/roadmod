using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class ProfitabilitySection : InfoSectionBase
{
	public enum Result
	{
		Visible,
		CompanyCount,
		Profitability,
		ResultCount
	}

	[BurstCompile]
	private struct ProfitabilityJob : IJob
	{
		[ReadOnly]
		public Entity m_SelectedEntity;

		[ReadOnly]
		public Entity m_SelectedPrefab;

		[ReadOnly]
		public BufferLookup<Employee> m_EmployeeFromEntity;

		[ReadOnly]
		public BufferLookup<Renter> m_RenterFromEntity;

		[ReadOnly]
		public BufferLookup<ResourceAvailability> m_ResourceAvailabilityFromEntity;

		[ReadOnly]
		public BufferLookup<TradeCost> m_TradeCostFromEntity;

		[ReadOnly]
		public ComponentLookup<Abandoned> m_AbandonedFromEntity;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingFromEntity;

		[ReadOnly]
		public BufferLookup<Efficiency> m_BuildingEfficiencyFromEntity;

		[ReadOnly]
		public ComponentLookup<Citizen> m_CitizenFromEntity;

		[ReadOnly]
		public ComponentLookup<CompanyData> m_CompanyDataFromEntity;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblemFromEntity;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDataFromEntity;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDataFromEntity;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDataFromEntity;

		[ReadOnly]
		public ComponentLookup<OfficeBuilding> m_OfficeBuildingFromEntity;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefFromEntity;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDataFromEntity;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDataFromEntity;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> m_WorkplaceDataFromEntity;

		[ReadOnly]
		public ComponentLookup<ZonePropertiesData> m_ZonePropertyDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Profitability> m_ProfitabilityFromEntity;

		[ReadOnly]
		public ComponentLookup<ServiceAvailable> m_ServiceAvailableFromEntity;

		[ReadOnly]
		public ComponentLookup<ServiceCompanyData> m_ServiceCompanyDataFromEntity;

		[ReadOnly]
		public ComponentLookup<WorkProvider> m_WorkProviderFromEntity;

		public EconomyParameterData m_EconomyParameters;

		public ResourcePrefabs m_ResourcePrefabs;

		public NativeArray<int> m_TaxRates;

		public NativeArray<Entity> m_Processes;

		public NativeArray<int2> m_Factors;

		public NativeArray<int> m_Results;

		public void Execute()
		{
			byte value = 0;
			int num = 0;
			if (m_BuildingFromEntity.HasComponent(m_SelectedEntity) && m_SpawnableBuildingDataFromEntity.HasComponent(m_SelectedPrefab))
			{
				bool flag = m_AbandonedFromEntity.HasComponent(m_SelectedEntity);
				if (CompanyUIUtils.HasCompany(m_SelectedEntity, m_SelectedPrefab, ref m_RenterFromEntity, ref m_BuildingPropertyDataFromEntity, ref m_CompanyDataFromEntity, out var company) && m_ProfitabilityFromEntity.TryGetComponent(company, out var componentData))
				{
					value = componentData.m_Profitability;
					num = 1;
				}
				BuildingHappiness.GetCompanyHappinessFactors(m_SelectedEntity, m_Factors, ref m_PrefabRefFromEntity, ref m_SpawnableBuildingDataFromEntity, ref m_BuildingPropertyDataFromEntity, ref m_BuildingFromEntity, ref m_OfficeBuildingFromEntity, ref m_RenterFromEntity, ref m_BuildingDataFromEntity, ref m_CompanyDataFromEntity, ref m_IndustrialProcessDataFromEntity, ref m_WorkProviderFromEntity, ref m_EmployeeFromEntity, ref m_WorkplaceDataFromEntity, ref m_CitizenFromEntity, ref m_HealthProblemFromEntity, ref m_ServiceAvailableFromEntity, ref m_ResourceDataFromEntity, ref m_ZonePropertyDataFromEntity, ref m_BuildingEfficiencyFromEntity, ref m_ServiceCompanyDataFromEntity, ref m_ResourceAvailabilityFromEntity, ref m_TradeCostFromEntity, m_EconomyParameters, m_TaxRates, m_Processes, m_ResourcePrefabs);
				m_Results[1] = num;
				m_Results[2] = value;
				m_Results[0] = ((num > 0 || flag) ? 1 : 0);
			}
		}
	}

	[BurstCompile]
	public struct DistrictProfitabilityJob : IJobChunk
	{
		[ReadOnly]
		public Entity m_SelectedEntity;

		[ReadOnly]
		public EntityTypeHandle m_EntityHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefHandle;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingFromEntity;

		[ReadOnly]
		public ComponentLookup<CompanyData> m_CompanyDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Abandoned> m_AbandonedFromEntity;

		[ReadOnly]
		public BufferLookup<Renter> m_RenterFromEntity;

		[ReadOnly]
		public ComponentLookup<Citizen> m_CitizenFromEntity;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblemFromEntity;

		[ReadOnly]
		public ComponentLookup<Profitability> m_ProfitabilityFromEntity;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefFromEntity;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDataFromEntity;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> m_BuildingPropertyDataFromEntity;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDataFromEntity;

		[ReadOnly]
		public ComponentLookup<OfficeBuilding> m_OfficeBuildingFromEntity;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDataFromEntity;

		[ReadOnly]
		public ComponentLookup<WorkProvider> m_WorkProviderFromEntity;

		[ReadOnly]
		public ComponentLookup<ServiceAvailable> m_ServiceAvailableFromEntity;

		[ReadOnly]
		public BufferLookup<Efficiency> m_BuildingEfficiencyFromEntity;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> m_WorkplaceDataFromEntity;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDataFromEntity;

		[ReadOnly]
		public ComponentLookup<ZonePropertiesData> m_ZonePropertyDataFromEntity;

		[ReadOnly]
		public ComponentLookup<ServiceCompanyData> m_ServiceCompanyDataFromEntity;

		[ReadOnly]
		public BufferLookup<ResourceAvailability> m_ResourceAvailabilityFromEntity;

		[ReadOnly]
		public BufferLookup<TradeCost> m_TradeCostFromEntity;

		[ReadOnly]
		public BufferLookup<Employee> m_EmployeeFromEntity;

		public EconomyParameterData m_EconomyParameters;

		public ResourcePrefabs m_ResourcePrefabs;

		public NativeArray<int> m_TaxRates;

		public NativeArray<Entity> m_Processes;

		public NativeArray<int2> m_Factors;

		public NativeArray<int> m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityHandle);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefHandle);
			NativeArray<CurrentDistrict> nativeArray3 = chunk.GetNativeArray(ref m_CurrentDistrictHandle);
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Entity prefab = nativeArray2[i].m_Prefab;
				if (!(nativeArray3[i].m_District != m_SelectedEntity) && m_SpawnableBuildingDataFromEntity.HasComponent(prefab))
				{
					if (CompanyUIUtils.HasCompany(entity, prefab, ref m_RenterFromEntity, ref m_BuildingPropertyDataFromEntity, ref m_CompanyDataFromEntity, out var company) && m_ProfitabilityFromEntity.TryGetComponent(company, out var componentData))
					{
						num2 += componentData.m_Profitability;
						num3++;
						num = 1;
					}
					if (m_AbandonedFromEntity.HasComponent(entity))
					{
						num = 1;
					}
					BuildingHappiness.GetCompanyHappinessFactors(entity, m_Factors, ref m_PrefabRefFromEntity, ref m_SpawnableBuildingDataFromEntity, ref m_BuildingPropertyDataFromEntity, ref m_BuildingFromEntity, ref m_OfficeBuildingFromEntity, ref m_RenterFromEntity, ref m_BuildingDataFromEntity, ref m_CompanyDataFromEntity, ref m_IndustrialProcessDataFromEntity, ref m_WorkProviderFromEntity, ref m_EmployeeFromEntity, ref m_WorkplaceDataFromEntity, ref m_CitizenFromEntity, ref m_HealthProblemFromEntity, ref m_ServiceAvailableFromEntity, ref m_ResourceDataFromEntity, ref m_ZonePropertyDataFromEntity, ref m_BuildingEfficiencyFromEntity, ref m_ServiceCompanyDataFromEntity, ref m_ResourceAvailabilityFromEntity, ref m_TradeCostFromEntity, m_EconomyParameters, m_TaxRates, m_Processes, m_ResourcePrefabs);
				}
			}
			m_Results[0] += num;
			m_Results[1] += num3;
			m_Results[2] += num2;
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

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Profitability> __Game_Companies_Profitability_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CompanyData> __Game_Companies_CompanyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OfficeBuilding> __Game_Prefabs_OfficeBuilding_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceAvailable> __Game_Companies_ServiceAvailable_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Efficiency> __Game_Buildings_Efficiency_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<WorkplaceData> __Game_Prefabs_WorkplaceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZonePropertiesData> __Game_Prefabs_ZonePropertiesData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceCompanyData> __Game_Companies_ServiceCompanyData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ResourceAvailability> __Game_Net_ResourceAvailability_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<TradeCost> __Game_Companies_TradeCost_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentDistrict>(isReadOnly: true);
			__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
			__Game_Companies_Profitability_RO_ComponentLookup = state.GetComponentLookup<Profitability>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Companies_CompanyData_RO_ComponentLookup = state.GetComponentLookup<CompanyData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_OfficeBuilding_RO_ComponentLookup = state.GetComponentLookup<OfficeBuilding>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
			__Game_Companies_WorkProvider_RO_ComponentLookup = state.GetComponentLookup<WorkProvider>(isReadOnly: true);
			__Game_Companies_ServiceAvailable_RO_ComponentLookup = state.GetComponentLookup<ServiceAvailable>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferLookup = state.GetBufferLookup<Efficiency>(isReadOnly: true);
			__Game_Prefabs_WorkplaceData_RO_ComponentLookup = state.GetComponentLookup<WorkplaceData>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup = state.GetComponentLookup<ZonePropertiesData>(isReadOnly: true);
			__Game_Companies_ServiceCompanyData_RO_ComponentLookup = state.GetComponentLookup<ServiceCompanyData>(isReadOnly: true);
			__Game_Net_ResourceAvailability_RO_BufferLookup = state.GetBufferLookup<ResourceAvailability>(isReadOnly: true);
			__Game_Companies_TradeCost_RO_BufferLookup = state.GetBufferLookup<TradeCost>(isReadOnly: true);
			__Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(isReadOnly: true);
		}
	}

	private TaxSystem m_TaxSystem;

	private ResourceSystem m_ResourceSystem;

	private EntityQuery m_DistrictBuildingQuery;

	private EntityQuery m_ProcessQuery;

	private EntityQuery m_EconomyParameterQuery;

	public NativeArray<int> m_Results;

	private NativeArray<int2> m_Factors;

	private TypeHandle __TypeHandle;

	protected override string group => "ProfitabilitySection";

	private CompanyProfitability profitability { get; set; }

	private NativeList<FactorInfo> profitabilityFactors { get; set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_DistrictBuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Renter>(), ComponentType.ReadOnly<CurrentDistrict>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_ProcessQuery = GetEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>());
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		m_Factors = new NativeArray<int2>(29, Allocator.Persistent);
		profitabilityFactors = new NativeList<FactorInfo>(10, Allocator.Persistent);
		m_Results = new NativeArray<int>(3, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Results.Dispose();
		m_Factors.Dispose();
		profitabilityFactors.Dispose();
		base.OnDestroy();
	}

	protected override void Reset()
	{
		for (int i = 0; i < m_Factors.Length; i++)
		{
			m_Factors[i] = 0;
		}
		profitability = default(CompanyProfitability);
		profitabilityFactors.Clear();
		m_Results[0] = 0;
		m_Results[1] = 0;
		m_Results[2] = 0;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeArray<Entity> processes = m_ProcessQuery.ToEntityArray(Allocator.TempJob);
		EconomyParameterData singleton = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>();
		NativeArray<int> taxRates = m_TaxSystem.GetTaxRates();
		ResourcePrefabs prefabs = m_ResourceSystem.GetPrefabs();
		if (base.EntityManager.HasComponent<District>(selectedEntity) && base.EntityManager.HasComponent<Area>(selectedEntity))
		{
			JobChunkExtensions.Schedule(new DistrictProfitabilityJob
			{
				m_SelectedEntity = selectedEntity,
				m_EntityHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_PrefabRefHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CitizenFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurrentDistrictHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_AbandonedFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HealthProblemFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ProfitabilityFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_Profitability_RO_ComponentLookup, ref base.CheckedStateRef),
				m_RenterFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabRefFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SpawnableBuildingDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingPropertyDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CompanyDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_CompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OfficeBuildingFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_OfficeBuilding_RO_ComponentLookup, ref base.CheckedStateRef),
				m_IndustrialProcessDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_WorkProviderFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ServiceAvailableFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingEfficiencyFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup, ref base.CheckedStateRef),
				m_WorkplaceDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResourceDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ZonePropertyDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ServiceCompanyDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResourceAvailabilityFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup, ref base.CheckedStateRef),
				m_TradeCostFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_TradeCost_RO_BufferLookup, ref base.CheckedStateRef),
				m_EmployeeFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_Employee_RO_BufferLookup, ref base.CheckedStateRef),
				m_EconomyParameters = singleton,
				m_ResourcePrefabs = prefabs,
				m_TaxRates = taxRates,
				m_Processes = processes,
				m_Factors = m_Factors,
				m_Results = m_Results
			}, m_DistrictBuildingQuery, base.Dependency).Complete();
			base.visible = m_Results[0] > 0;
		}
		else
		{
			IJobExtensions.Schedule(new ProfitabilityJob
			{
				m_SelectedEntity = selectedEntity,
				m_SelectedPrefab = selectedPrefab,
				m_BuildingFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HealthProblemFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ProfitabilityFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_Profitability_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AbandonedFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CitizenFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
				m_RenterFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabRefFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SpawnableBuildingDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingPropertyDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CompanyDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_CompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OfficeBuildingFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_OfficeBuilding_RO_ComponentLookup, ref base.CheckedStateRef),
				m_IndustrialProcessDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_WorkProviderFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ServiceAvailableFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingEfficiencyFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup, ref base.CheckedStateRef),
				m_WorkplaceDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WorkplaceData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResourceDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ZonePropertyDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZonePropertiesData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ServiceCompanyDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResourceAvailabilityFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ResourceAvailability_RO_BufferLookup, ref base.CheckedStateRef),
				m_TradeCostFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_TradeCost_RO_BufferLookup, ref base.CheckedStateRef),
				m_EmployeeFromEntity = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_Employee_RO_BufferLookup, ref base.CheckedStateRef),
				m_EconomyParameters = singleton,
				m_ResourcePrefabs = prefabs,
				m_TaxRates = taxRates,
				m_Processes = processes,
				m_Factors = m_Factors,
				m_Results = m_Results
			}, base.Dependency).Complete();
			base.visible = m_Results[0] > 0;
		}
	}

	protected override void OnProcess()
	{
		int num = m_Results[1];
		int profit = ((num != 0) ? ((int)math.round((float)m_Results[2] / (float)num * 2f - 255f)) : 0);
		profitability = new CompanyProfitability(profit);
		for (int i = 0; i < m_Factors.Length; i++)
		{
			int x = m_Factors[i].x;
			if (x > 0)
			{
				float num2 = math.round((float)m_Factors[i].y / (float)x);
				if (num2 != 0f)
				{
					profitabilityFactors.Add(new FactorInfo(i, (int)num2));
				}
			}
		}
		profitabilityFactors.Sort();
		if (base.EntityManager.HasComponent<Building>(selectedEntity))
		{
			base.tooltipKeys.Add("Company");
		}
		else
		{
			base.tooltipKeys.Add("District");
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("profitability");
		writer.Write(profitability);
		int num = math.min(10, profitabilityFactors.Length);
		writer.PropertyName("profitabilityFactors");
		writer.ArrayBegin(num);
		for (int i = 0; i < num; i++)
		{
			profitabilityFactors[i].WriteBuildingHappinessFactor(writer);
		}
		writer.ArrayEnd();
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
	public ProfitabilitySection()
	{
	}
}
