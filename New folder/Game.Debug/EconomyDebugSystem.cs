using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Entities;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Debug;

[CompilerGenerated]
public class EconomyDebugSystem : BaseDebugSystem
{
	private struct EconomyGizmoJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<Game.Economy.Resources> m_ResourceType;

		[ReadOnly]
		public BufferTypeHandle<TradeCost> m_TradeCostBufType;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> m_RenterType;

		[ReadOnly]
		public ComponentTypeHandle<Household> m_HouseholdType;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdNeed> m_HouseholdNeedType;

		[ReadOnly]
		public ComponentTypeHandle<ServiceAvailable> m_ServiceType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Companies.ProcessingCompany> m_ProcessingType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Companies.StorageCompany> m_StorageType;

		[ReadOnly]
		public ComponentTypeHandle<Profitability> m_ProfitabilityType;

		[ReadOnly]
		public ComponentTypeHandle<TaxPayer> m_TaxPayerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.CargoTransportStation> m_CargoTransportstationType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_ProcessDatas;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_Trucks;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> m_OwnedVehicles;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_Transforms;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> m_StorageDatas;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDatas;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDatas;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> m_StorageLimitDatas;

		public EconomyParameterData m_EconomyParameters;

		public GizmoBatcher m_GizmoBatcher;

		public bool m_ResidentialOption;

		public bool m_CommercialOption;

		public bool m_IndustrialOption;

		public bool m_UntaxedIncomeOption;

		public bool m_StorageUsedOption;

		public bool m_HouseholdNeedOption;

		public bool m_ProfitabilityOption;

		public bool m_TradeCostOption;

		public bool m_CommercialStorageOption;

		private void Draw(Entity building, float value, int offset)
		{
			value /= 500f;
			if (m_Transforms.HasComponent(building))
			{
				Game.Objects.Transform transform = m_Transforms[building];
				float3 position = transform.m_Position;
				position.y += value / 2f + 10f;
				position += 5f * (float)offset * math.rotate(transform.m_Rotation.value, new float3(1f, 0f, 0f));
				UnityEngine.Color color = ((value > 0f) ? UnityEngine.Color.white : UnityEngine.Color.red);
				m_GizmoBatcher.DrawWireCube(position, new float3(5f, value, 5f), color);
			}
		}

		private void Draw(Entity building, float value, int offset, UnityEngine.Color color)
		{
			value /= 500f;
			if (m_Transforms.HasComponent(building))
			{
				Game.Objects.Transform transform = m_Transforms[building];
				float3 position = transform.m_Position;
				position.y += value / 2f + 10f;
				position += 5f * (float)offset * math.rotate(transform.m_Rotation.value, new float3(1f, 0f, 0f));
				m_GizmoBatcher.DrawWireCube(position, new float3(5f, value, 5f), color);
			}
		}

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<Game.Economy.Resources> bufferAccessor = chunk.GetBufferAccessor(ref m_ResourceType);
			BufferAccessor<TradeCost> bufferAccessor2 = chunk.GetBufferAccessor(ref m_TradeCostBufType);
			NativeArray<PropertyRenter> nativeArray2 = chunk.GetNativeArray(ref m_RenterType);
			NativeArray<Household> nativeArray3 = chunk.GetNativeArray(ref m_HouseholdType);
			if (chunk.Has(ref m_HouseholdType))
			{
				if (m_ResidentialOption)
				{
					for (int i = 0; i < nativeArray2.Length; i++)
					{
						Entity property = nativeArray2[i].m_Property;
						int householdTotalWealth = EconomyUtils.GetHouseholdTotalWealth(nativeArray3[i], bufferAccessor[i]);
						Draw(property, householdTotalWealth, 0);
					}
				}
				else if (m_HouseholdNeedOption)
				{
					NativeArray<HouseholdNeed> nativeArray4 = chunk.GetNativeArray(ref m_HouseholdNeedType);
					for (int j = 0; j < nativeArray2.Length; j++)
					{
						Entity property2 = nativeArray2[j].m_Property;
						Draw(property2, nativeArray4[j].m_Amount, 0);
					}
				}
			}
			if (m_CommercialOption && chunk.Has(ref m_ServiceType))
			{
				NativeArray<ServiceAvailable> nativeArray5 = chunk.GetNativeArray(ref m_ServiceType);
				for (int k = 0; k < nativeArray2.Length; k++)
				{
					Entity entity = nativeArray[k];
					Entity property3 = nativeArray2[k].m_Property;
					Entity prefab = m_PrefabRefs[entity].m_Prefab;
					IndustrialProcessData industrialProcessData = m_ProcessDatas[prefab];
					float value;
					if (m_OwnedVehicles.HasBuffer(entity))
					{
						DynamicBuffer<OwnedVehicle> vehicles = m_OwnedVehicles[entity];
						value = EconomyUtils.GetCompanyTotalWorth(isIndustrial: false, industrialProcessData, bufferAccessor[k], vehicles, ref m_LayoutElements, ref m_Trucks, m_ResourcePrefabs, ref m_ResourceDatas);
					}
					else
					{
						value = EconomyUtils.GetCompanyTotalWorth(isIndustrial: false, industrialProcessData, bufferAccessor[k], m_ResourcePrefabs, ref m_ResourceDatas);
					}
					float num = EconomyUtils.GetResources(industrialProcessData.m_Output.m_Resource, bufferAccessor[k]);
					float num2 = nativeArray5[k].m_ServiceAvailable;
					float marketPrice = EconomyUtils.GetMarketPrice(industrialProcessData.m_Output.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas);
					num *= marketPrice;
					num2 *= marketPrice;
					Draw(property3, value, -1);
					Draw(property3, num, 0);
					Draw(property3, num2, 1);
				}
			}
			if (m_IndustrialOption && !chunk.Has(ref m_ServiceType) && chunk.Has(ref m_ProcessingType))
			{
				for (int l = 0; l < nativeArray2.Length; l++)
				{
					Entity entity2 = nativeArray[l];
					Entity property4 = nativeArray2[l].m_Property;
					Entity prefab2 = m_PrefabRefs[entity2].m_Prefab;
					IndustrialProcessData industrialProcessData2 = m_ProcessDatas[prefab2];
					float value2;
					if (m_OwnedVehicles.HasBuffer(entity2))
					{
						DynamicBuffer<OwnedVehicle> vehicles2 = m_OwnedVehicles[entity2];
						value2 = EconomyUtils.GetCompanyTotalWorth(isIndustrial: true, industrialProcessData2, bufferAccessor[l], vehicles2, ref m_LayoutElements, ref m_Trucks, m_ResourcePrefabs, ref m_ResourceDatas);
					}
					else
					{
						value2 = EconomyUtils.GetCompanyTotalWorth(isIndustrial: true, industrialProcessData2, bufferAccessor[l], m_ResourcePrefabs, ref m_ResourceDatas);
					}
					float num3 = EconomyUtils.GetResources(industrialProcessData2.m_Input1.m_Resource, bufferAccessor[l]);
					num3 *= EconomyUtils.GetMarketPrice(industrialProcessData2.m_Input1.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas);
					float num4 = EconomyUtils.GetResources(industrialProcessData2.m_Output.m_Resource, bufferAccessor[l]);
					num4 *= EconomyUtils.GetMarketPrice(industrialProcessData2.m_Output.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas);
					Draw(property4, value2, -1);
					Draw(property4, num3, 0);
					Draw(property4, num4, 1);
				}
			}
			else if (m_UntaxedIncomeOption && chunk.Has(ref m_TaxPayerType))
			{
				NativeArray<TaxPayer> nativeArray6 = chunk.GetNativeArray(ref m_TaxPayerType);
				for (int m = 0; m < nativeArray2.Length; m++)
				{
					Entity property5 = nativeArray2[m].m_Property;
					Draw(property5, nativeArray6[m].m_UntaxedIncome, 0);
				}
			}
			else if (m_StorageUsedOption && chunk.Has(ref m_StorageType) && !chunk.Has(ref m_CargoTransportstationType))
			{
				for (int n = 0; n < nativeArray2.Length; n++)
				{
					Entity entity3 = nativeArray[n];
					Entity property6 = nativeArray2[n].m_Property;
					Entity prefab3 = m_PrefabRefs[entity3].m_Prefab;
					Entity prefab4 = m_PrefabRefs[property6].m_Prefab;
					StorageCompanyData storageCompanyData = m_StorageDatas[prefab3];
					StorageLimitData storageLimitData = m_StorageLimitDatas[prefab3];
					int resources = EconomyUtils.GetResources(storageCompanyData.m_StoredResources, bufferAccessor[n]);
					int adjustedLimitForWarehouse = storageLimitData.GetAdjustedLimitForWarehouse(m_SpawnableBuildingDatas[prefab4], m_BuildingDatas[prefab4]);
					EconomyUtils.GetResourceIndex(storageCompanyData.m_StoredResources);
					UnityEngine.Color resourceColor = EconomyUtils.GetResourceColor(storageCompanyData.m_StoredResources);
					Draw(property6, resources, -1, resourceColor);
					Draw(property6, adjustedLimitForWarehouse, 0, resourceColor);
				}
			}
			else if (m_ProfitabilityOption && chunk.Has(ref m_ProfitabilityType))
			{
				NativeArray<Profitability> nativeArray7 = chunk.GetNativeArray(ref m_ProfitabilityType);
				for (int num5 = 0; num5 < nativeArray7.Length; num5++)
				{
					Profitability profitability = nativeArray7[num5];
					Entity property7 = nativeArray2[num5].m_Property;
					Draw(property7, (int)profitability.m_Profitability, 0);
				}
			}
			else if (m_TradeCostOption && chunk.Has(ref m_TradeCostBufType))
			{
				for (int num6 = 0; num6 < bufferAccessor2.Length; num6++)
				{
					Entity property8 = nativeArray2[num6].m_Property;
					DynamicBuffer<TradeCost> dynamicBuffer = bufferAccessor2[num6];
					for (int num7 = 0; num7 < dynamicBuffer.Length; num7++)
					{
						Draw(property8, dynamicBuffer[num7].m_BuyCost * 100f, num7 * 2, (dynamicBuffer[num7].m_BuyCost > 5f) ? UnityEngine.Color.red : UnityEngine.Color.white);
						Draw(property8, dynamicBuffer[num7].m_SellCost * 100f, num7 * 2 + 1, UnityEngine.Color.green);
					}
				}
			}
			else
			{
				if (!m_CommercialStorageOption || !chunk.Has(ref m_ServiceType))
				{
					return;
				}
				for (int num8 = 0; num8 < bufferAccessor.Length; num8++)
				{
					Entity property9 = nativeArray2[num8].m_Property;
					int num9 = 0;
					for (int num10 = 0; num10 < bufferAccessor[num8].Length; num10++)
					{
						if (EconomyUtils.IsResourceHasWeight(bufferAccessor[num8][num10].m_Resource, m_ResourcePrefabs, ref m_ResourceDatas))
						{
							num9 += bufferAccessor[num8][num10].m_Amount;
						}
					}
					if (num9 > 8000)
					{
						Draw(property9, num9, 0, UnityEngine.Color.red);
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
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Economy.Resources> __Game_Economy_Resources_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<TradeCost> __Game_Companies_TradeCost_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Household> __Game_Citizens_Household_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ServiceAvailable> __Game_Companies_ServiceAvailable_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Companies.ProcessingCompany> __Game_Companies_ProcessingCompany_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Companies.StorageCompany> __Game_Companies_StorageCompany_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Profitability> __Game_Companies_Profitability_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TaxPayer> __Game_Agents_TaxPayer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.CargoTransportStation> __Game_Buildings_CargoTransportStation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdNeed> __Game_Citizens_HouseholdNeed_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> __Game_Prefabs_StorageCompanyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> __Game_Companies_StorageLimitData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Economy_Resources_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Economy.Resources>(isReadOnly: true);
			__Game_Companies_TradeCost_RO_BufferTypeHandle = state.GetBufferTypeHandle<TradeCost>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Household>(isReadOnly: true);
			__Game_Companies_ServiceAvailable_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceAvailable>(isReadOnly: true);
			__Game_Companies_ProcessingCompany_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Companies.ProcessingCompany>(isReadOnly: true);
			__Game_Companies_StorageCompany_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Companies.StorageCompany>(isReadOnly: true);
			__Game_Companies_Profitability_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Profitability>(isReadOnly: true);
			__Game_Agents_TaxPayer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TaxPayer>(isReadOnly: true);
			__Game_Buildings_CargoTransportStation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.CargoTransportStation>(isReadOnly: true);
			__Game_Citizens_HouseholdNeed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdNeed>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
			__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Prefabs_StorageCompanyData_RO_ComponentLookup = state.GetComponentLookup<StorageCompanyData>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Companies_StorageLimitData_RO_ComponentLookup = state.GetComponentLookup<StorageLimitData>(isReadOnly: true);
		}
	}

	private EntityQuery m_AgentQuery;

	private EntityQuery m_EconomyParameterQuery;

	private GizmosSystem m_GizmosSystem;

	private ResourceSystem m_ResourceSystem;

	private Option m_ResidentialOption;

	private Option m_CommercialOption;

	private Option m_CommercialStorageOption;

	private Option m_IndustrialOption;

	private Option m_UntaxedIncomeOption;

	private Option m_StorageUsedOption;

	private Option m_HouseholdNeedOption;

	private Option m_ProfitabilityOption;

	private Option m_TradeCostOption;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_AgentQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Economy.Resources>(),
				ComponentType.ReadOnly<PrefabRef>(),
				ComponentType.ReadOnly<PropertyRenter>()
			},
			Any = new ComponentType[5]
			{
				ComponentType.ReadOnly<Household>(),
				ComponentType.ReadOnly<ServiceAvailable>(),
				ComponentType.ReadOnly<Game.Companies.ProcessingCompany>(),
				ComponentType.ReadOnly<Game.Companies.StorageCompany>(),
				ComponentType.ReadOnly<Profitability>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Hidden>()
			}
		});
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		base.Enabled = false;
		RequireForUpdate(m_AgentQuery);
		RequireForUpdate(m_EconomyParameterQuery);
		m_ResidentialOption = AddOption("Residential worth", defaultEnabled: false);
		m_CommercialOption = AddOption("Commercial worth", defaultEnabled: false);
		m_IndustrialOption = AddOption("Industrial worth", defaultEnabled: false);
		m_UntaxedIncomeOption = AddOption("Untaxed income", defaultEnabled: false);
		m_StorageUsedOption = AddOption("Storage used", defaultEnabled: false);
		m_HouseholdNeedOption = AddOption("Household need", defaultEnabled: false);
		m_ProfitabilityOption = AddOption("Company Profitability", defaultEnabled: false);
		m_TradeCostOption = AddOption("Trade Cost Profitability", defaultEnabled: false);
		m_CommercialStorageOption = AddOption("Commercial storage", defaultEnabled: false);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_AgentQuery.IsEmptyIgnoreFilter)
		{
			JobHandle dependencies;
			EconomyGizmoJob jobData = new EconomyGizmoJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_ResourceType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_TradeCostBufType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Companies_TradeCost_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_RenterType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_HouseholdType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ServiceType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ProcessingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_ProcessingCompany_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_StorageType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_StorageCompany_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ProfitabilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_Profitability_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TaxPayerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Agents_TaxPayer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CargoTransportstationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_CargoTransportStation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_HouseholdNeedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdNeed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Trucks = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref base.CheckedStateRef),
				m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_Transforms = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
				m_StorageDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StorageCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SpawnableBuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_StorageLimitDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageLimitData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ResidentialOption = m_ResidentialOption.enabled,
				m_CommercialOption = m_CommercialOption.enabled,
				m_IndustrialOption = m_IndustrialOption.enabled,
				m_UntaxedIncomeOption = m_UntaxedIncomeOption.enabled,
				m_StorageUsedOption = m_StorageUsedOption.enabled,
				m_HouseholdNeedOption = m_HouseholdNeedOption.enabled,
				m_ProfitabilityOption = m_ProfitabilityOption.enabled,
				m_TradeCostOption = m_TradeCostOption.enabled,
				m_CommercialStorageOption = m_CommercialStorageOption.enabled,
				m_EconomyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
				m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies)
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_AgentQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
			m_ResourceSystem.AddPrefabsReader(base.Dependency);
			m_GizmosSystem.AddGizmosBatcherWriter(base.Dependency);
		}
	}

	public static void RemoveExtraCompanies()
	{
		EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
		EntityQuery entityQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<CompanyData>(), ComponentType.Exclude<PropertyRenter>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		entityManager.AddComponent<Deleted>(entityQuery);
	}

	public static void PrintTradeDebug(ITradeSystem tradeSystem, DynamicBuffer<CityModifier> cityEffects)
	{
		ResourceIterator iterator = ResourceIterator.GetIterator();
		while (iterator.Next())
		{
			UnityEngine.Debug.Log(EconomyUtils.GetName(iterator.resource) + ":");
			UnityEngine.Debug.Log("Road: " + tradeSystem.GetBestTradePriceAmongTypes(iterator.resource, OutsideConnectionTransferType.Road, import: true, cityEffects) + " / " + tradeSystem.GetBestTradePriceAmongTypes(iterator.resource, OutsideConnectionTransferType.Road, import: false, cityEffects));
			UnityEngine.Debug.Log("Air: " + tradeSystem.GetBestTradePriceAmongTypes(iterator.resource, OutsideConnectionTransferType.Air, import: true, cityEffects) + " / " + tradeSystem.GetBestTradePriceAmongTypes(iterator.resource, OutsideConnectionTransferType.Air, import: false, cityEffects));
			UnityEngine.Debug.Log("Rail: " + tradeSystem.GetBestTradePriceAmongTypes(iterator.resource, OutsideConnectionTransferType.Train, import: true, cityEffects) + " / " + tradeSystem.GetBestTradePriceAmongTypes(iterator.resource, OutsideConnectionTransferType.Train, import: false, cityEffects));
			UnityEngine.Debug.Log("Ship: " + tradeSystem.GetBestTradePriceAmongTypes(iterator.resource, OutsideConnectionTransferType.Ship, import: true, cityEffects) + " / " + tradeSystem.GetBestTradePriceAmongTypes(iterator.resource, OutsideConnectionTransferType.Ship, import: false, cityEffects));
		}
	}

	public static void PrintAgeDebug()
	{
		EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
		EntityQuery entityQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<Household>(), ComponentType.ReadOnly<MovingAway>(), ComponentType.Exclude<CommuterHousehold>(), ComponentType.Exclude<TouristHousehold>(), ComponentType.Exclude<Deleted>());
		EntityQuery entityQuery2 = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeSettingsData>());
		TimeData singleton = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TimeData>()).GetSingleton<TimeData>();
		entityQuery2.GetSingleton<TimeSettingsData>();
		NativeArray<Entity> nativeArray = entityQuery.ToEntityArray(Allocator.TempJob);
		int num = 0;
		int[] array = new int[240];
		int day = TimeSystem.GetDay(World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<SimulationSystem>().frameIndex, singleton);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			DynamicBuffer<HouseholdCitizen> buffer = entityManager.GetBuffer<HouseholdCitizen>(nativeArray[i], isReadOnly: true);
			for (int j = 0; j < buffer.Length; j++)
			{
				Entity citizen = buffer[j].m_Citizen;
				int num2 = day - entityManager.GetComponentData<Citizen>(citizen).m_BirthDay;
				array[num2]++;
				num = math.max(num, num2);
			}
		}
		nativeArray.Dispose();
		for (int k = 0; k < num; k++)
		{
			UnityEngine.Debug.Log(k + ": " + array[k]);
		}
	}

	public static void PrintSchoolDebug()
	{
		EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
		EntityQuery entityQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<Citizen>());
		int num = 0;
		int num2 = 0;
		NativeArray<Entity> nativeArray = entityQuery.ToEntityArray(Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			if (entityManager.TryGetComponent<Citizen>(nativeArray[i], out var component) && component.GetAge() == CitizenAge.Child)
			{
				num++;
				if (entityManager.GetComponentData<Citizen>(nativeArray[i]).GetEducationLevel() > 1)
				{
					UnityEngine.Debug.Log($"{nativeArray[i].Index} level ");
				}
				else
				{
					num2++;
				}
			}
		}
		UnityEngine.Debug.Log($"Processed {num} children, {num2} ok");
	}

	public static void PrintCompanyDebug(ComponentLookup<ResourceData> resourceDatas)
	{
		EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
		EntityQuery entityQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>(), ComponentType.ReadOnly<ServiceCompanyData>(), ComponentType.ReadOnly<WorkplaceData>());
		EntityQuery entityQuery2 = entityManager.CreateEntityQuery(ComponentType.ReadOnly<IndustrialProcessData>(), ComponentType.ReadOnly<IndustrialCompanyData>(), ComponentType.ReadOnly<WorkplaceData>(), ComponentType.Exclude<StorageCompanyData>());
		ResourcePrefabs prefabs = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<ResourceSystem>().GetPrefabs();
		NativeArray<ServiceCompanyData> nativeArray = entityQuery.ToComponentDataArray<ServiceCompanyData>(Allocator.TempJob);
		NativeArray<IndustrialProcessData> nativeArray2 = entityQuery.ToComponentDataArray<IndustrialProcessData>(Allocator.TempJob);
		NativeArray<WorkplaceData> nativeArray3 = entityQuery.ToComponentDataArray<WorkplaceData>(Allocator.TempJob);
		NativeArray<IndustrialProcessData> nativeArray4 = entityQuery2.ToComponentDataArray<IndustrialProcessData>(Allocator.TempJob);
		NativeArray<WorkplaceData> nativeArray5 = entityQuery2.ToComponentDataArray<WorkplaceData>(Allocator.TempJob);
		NativeArray<Entity> nativeArray6 = entityQuery2.ToEntityArray(Allocator.TempJob);
		NativeArray<EconomyParameterData> nativeArray7 = entityManager.CreateEntityQuery(ComponentType.ReadOnly<EconomyParameterData>()).ToComponentDataArray<EconomyParameterData>(Allocator.TempJob);
		EconomyParameterData economyParameters = nativeArray7[0];
		UnityEngine.Debug.Log("Company data per cell");
		for (int i = 0; i < nativeArray.Length; i++)
		{
			ServiceCompanyData serviceCompanyData = nativeArray[i];
			IndustrialProcessData industrialProcessData = nativeArray2[i];
			BuildingData buildingData = new BuildingData
			{
				m_LotSize = new int2(100, 10)
			};
			ServiceAvailable serviceAvailable = new ServiceAvailable
			{
				m_MeanPriority = 0.5f
			};
			WorkplaceData workplaceData = nativeArray3[i];
			SpawnableBuildingData spawnableBuildingData = new SpawnableBuildingData
			{
				m_Level = 1
			};
			SpawnableBuildingData spawnableBuildingData2 = new SpawnableBuildingData
			{
				m_Level = 5
			};
			EconomyUtils.BuildPseudoTradeCost(5000f, industrialProcessData, ref resourceDatas, prefabs);
			string text = "C " + EconomyUtils.GetName(industrialProcessData.m_Output.m_Resource) + ": ";
			int workerAmount = Mathf.RoundToInt(serviceCompanyData.m_MaxWorkersPerCell * 1000f);
			int companyProductionPerDay = EconomyUtils.GetCompanyProductionPerDay(1f, workerAmount, spawnableBuildingData.m_Level, isIndustrial: true, workplaceData, industrialProcessData, prefabs, ref resourceDatas, ref economyParameters);
			int companyProductionPerDay2 = EconomyUtils.GetCompanyProductionPerDay(1f, workerAmount, spawnableBuildingData2.m_Level, isIndustrial: true, workplaceData, industrialProcessData, prefabs, ref resourceDatas, ref economyParameters);
			text = text + "Production " + (float)companyProductionPerDay / 1000f + "|" + (float)companyProductionPerDay2 / 1000f + ")";
			UnityEngine.Debug.Log(text);
		}
		for (int j = 0; j < nativeArray4.Length; j++)
		{
			IndustrialProcessData process = nativeArray4[j];
			BuildingData buildingData = new BuildingData
			{
				m_LotSize = new int2(100, 10)
			};
			EconomyUtils.BuildPseudoTradeCost(5000f, process, ref resourceDatas, prefabs);
			_ = nativeArray5[j];
			SpawnableBuildingData spawnableBuildingData3 = new SpawnableBuildingData
			{
				m_Level = 1
			};
			spawnableBuildingData3 = new SpawnableBuildingData
			{
				m_Level = 5
			};
			UnityEngine.Debug.Log("I " + EconomyUtils.GetName(process.m_Input1.m_Resource) + " => " + EconomyUtils.GetName(process.m_Output.m_Resource) + ": ");
		}
		nativeArray.Dispose();
		nativeArray2.Dispose();
		nativeArray3.Dispose();
		nativeArray6.Dispose();
		nativeArray4.Dispose();
		nativeArray5.Dispose();
		nativeArray7.Dispose();
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
	public EconomyDebugSystem()
	{
	}
}
