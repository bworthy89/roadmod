using System.Runtime.CompilerServices;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CompanyEconomyStatisticSystem : GameSystemBase
{
	[BurstCompile]
	private struct CompanyEconomyStatisticJob : IJobChunk
	{
		public ComponentTypeHandle<CompanyStatisticData> m_CompanyStatisticType;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<Resources> m_ResourcesBufType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Companies.ExtractorCompany> m_ExtractorCompanyType;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTrucks;

		[ReadOnly]
		public ComponentLookup<OfficeProperty> m_OfficeProperties;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> m_ElectricityConsumers;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> m_WaterConsumers;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcesses;

		[ReadOnly]
		public ComponentLookup<ServiceAvailable> m_ServiceAvailables;

		[ReadOnly]
		public ComponentLookup<ServiceCompanyData> m_ServiceCompanyDatas;

		[ReadOnly]
		public ComponentLookup<TaxPayer> m_TaxPayers;

		[ReadOnly]
		public ComponentLookup<ConsumptionData> m_ComsumptionDatas;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElementBufs;

		[ReadOnly]
		public BufferLookup<Efficiency> m_EfficiencyBufs;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> m_OwnedVehicleBufs;

		[ReadOnly]
		public BufferLookup<Renter> m_RenterBufs;

		[ReadOnly]
		public BufferLookup<ServiceFee> m_ServiceFeeBufs;

		[ReadOnly]
		public BufferLookup<Employee> m_EmployeeBufs;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public EconomyParameterData m_EconomyParameters;

		[ReadOnly]
		public ServiceFeeParameterData m_ServiceFeeParameterData;

		[ReadOnly]
		public Entity m_CityEntity;

		[ReadOnly]
		public uint m_UpdateFrameIndex;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<CompanyStatisticData> nativeArray = chunk.GetNativeArray(ref m_CompanyStatisticType);
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<Resources> bufferAccessor = chunk.GetBufferAccessor(ref m_ResourcesBufType);
			bool flag = chunk.Has(ref m_ExtractorCompanyType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray2[i];
				CompanyStatisticData value = nativeArray[i];
				value.m_Income = 0;
				value.m_Worth = 0;
				value.m_Profit = 0;
				value.m_RentPaid = 0;
				value.m_WagePaid = 0;
				value.m_ElectricityPaid = 0;
				value.m_WaterPaid = 0;
				value.m_SewagePaid = 0;
				value.m_GarbagePaid = 0;
				value.m_TaxPaid = 0;
				value.m_CostBuyResource = 0;
				bool flag2 = !m_ServiceAvailables.HasComponent(entity);
				bool flag3 = false;
				float buildingEfficiency = 0f;
				IndustrialProcessData industrialProcessData = default(IndustrialProcessData);
				if (m_PropertyRenters.TryGetComponent(entity, out var componentData))
				{
					int rent = componentData.m_Rent;
					buildingEfficiency = (m_EfficiencyBufs.TryGetBuffer(componentData.m_Property, out var bufferData) ? BuildingUtils.GetEfficiency(bufferData) : 0f);
					if (m_RenterBufs.TryGetBuffer(componentData.m_Property, out var bufferData2))
					{
						int num = ((bufferData2.Length <= 0) ? 1 : bufferData2.Length);
						value.m_RentPaid = rent;
						if (m_PrefabRefs.TryGetComponent(componentData.m_Property, out var componentData2) && m_ComsumptionDatas.TryGetComponent(componentData2.m_Prefab, out var componentData3))
						{
							int upkeep = componentData3.m_Upkeep;
							if (upkeep < value.m_Worth)
							{
								value.m_RentPaid += upkeep / num;
							}
						}
					}
					flag3 = m_OfficeProperties.HasComponent(componentData.m_Property);
					if (m_ServiceFeeBufs.TryGetBuffer(m_CityEntity, out var bufferData3))
					{
						if (m_ElectricityConsumers.TryGetComponent(componentData.m_Property, out var componentData4))
						{
							value.m_ElectricityPaid = (int)((float)componentData4.m_FulfilledConsumption * ServiceFeeSystem.GetFee(PlayerResource.Electricity, bufferData3));
						}
						if (m_WaterConsumers.TryGetComponent(componentData.m_Property, out var componentData5))
						{
							value.m_WaterPaid = (int)((float)componentData5.m_FulfilledFresh * ServiceFeeSystem.GetFee(PlayerResource.Water, bufferData3));
							value.m_SewagePaid = (int)((float)componentData5.m_FulfilledSewage * ServiceFeeSystem.GetFee(PlayerResource.Water, bufferData3));
						}
					}
				}
				if (m_EmployeeBufs.TryGetBuffer(entity, out var bufferData4))
				{
					value.m_WagePaid = EconomyUtils.CalculateTotalWage(bufferData4, ref m_EconomyParameters);
					if (m_PrefabRefs.TryGetComponent(entity, out var componentData6) && m_IndustrialProcesses.TryGetComponent(componentData6.m_Prefab, out var componentData7))
					{
						industrialProcessData = componentData7;
						ServiceAvailable serviceAvailable = default(ServiceAvailable);
						ServiceCompanyData serviceCompanyData = default(ServiceCompanyData);
						if (!flag2)
						{
							Resource resource = componentData7.m_Output.m_Resource;
							if (m_ServiceAvailables.TryGetComponent(entity, out var componentData8))
							{
								serviceAvailable = componentData8;
							}
							if (m_ServiceCompanyDatas.TryGetComponent(componentData6.m_Prefab, out var componentData9))
							{
								serviceCompanyData = componentData9;
							}
							int companyProductionPerDay = EconomyUtils.GetCompanyProductionPerDay(buildingEfficiency, isIndustrial: false, bufferData4, componentData7, m_ResourcePrefabs, ref m_ResourceDatas, ref m_Citizens, ref m_EconomyParameters, serviceAvailable, serviceCompanyData);
							value.m_Income = (int)math.ceil((float)companyProductionPerDay * EconomyUtils.GetMarketPrice(resource, m_ResourcePrefabs, ref m_ResourceDatas));
							int num2 = companyProductionPerDay / componentData7.m_Output.m_Amount * componentData7.m_Input1.m_Amount;
							int num3 = companyProductionPerDay / componentData7.m_Output.m_Amount * componentData7.m_Input2.m_Amount;
							float industrialPrice = EconomyUtils.GetIndustrialPrice(componentData7.m_Input1.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas);
							float industrialPrice2 = EconomyUtils.GetIndustrialPrice(componentData7.m_Input2.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas);
							value.m_CostBuyResource = (int)((float)num2 * industrialPrice + (float)num3 * industrialPrice2);
							if (m_TaxPayers.TryGetComponent(entity, out var componentData10))
							{
								int num4 = (int)math.ceil((float)companyProductionPerDay * EconomyUtils.GetServicePrice(resource, m_ResourcePrefabs, ref m_ResourceDatas));
								value.m_TaxPaid = num4 * componentData10.m_AverageTaxRate / 100;
							}
						}
						else
						{
							int num5 = ((!flag) ? EconomyUtils.GetCompanyProductionPerDay(buildingEfficiency, isIndustrial: true, bufferData4, componentData7, m_ResourcePrefabs, ref m_ResourceDatas, ref m_Citizens, ref m_EconomyParameters, serviceAvailable, serviceCompanyData) : value.m_LastUpdateProduce);
							float industrialPrice3 = EconomyUtils.GetIndustrialPrice(componentData7.m_Output.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas);
							value.m_Income = (int)((float)num5 * industrialPrice3);
							int num6 = num5 / componentData7.m_Output.m_Amount * componentData7.m_Input1.m_Amount;
							int num7 = num5 / componentData7.m_Output.m_Amount * componentData7.m_Input2.m_Amount;
							float industrialPrice4 = EconomyUtils.GetIndustrialPrice(componentData7.m_Input1.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas);
							float industrialPrice5 = EconomyUtils.GetIndustrialPrice(componentData7.m_Input2.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas);
							value.m_CostBuyResource = (int)((float)num6 * industrialPrice4 + (float)num7 * industrialPrice5);
							if (m_TaxPayers.TryGetComponent(entity, out var componentData11))
							{
								value.m_TaxPaid = (value.m_Income - value.m_CostBuyResource) * componentData11.m_AverageTaxRate / 100;
							}
						}
					}
				}
				if (flag2)
				{
					value.m_GarbagePaid = (flag3 ? m_ServiceFeeParameterData.m_GarbageFeeRCIO.z : m_ServiceFeeParameterData.m_GarbageFeeRCIO.w);
				}
				else
				{
					value.m_GarbagePaid = m_ServiceFeeParameterData.m_GarbageFeeRCIO.y;
				}
				if (m_OwnedVehicleBufs.TryGetBuffer(entity, out var bufferData5))
				{
					value.m_Worth = EconomyUtils.GetCompanyTotalWorth(flag2, industrialProcessData, bufferAccessor[i], bufferData5, ref m_LayoutElementBufs, ref m_DeliveryTrucks, m_ResourcePrefabs, ref m_ResourceDatas);
				}
				else
				{
					value.m_Worth = EconomyUtils.GetCompanyTotalWorth(flag2, industrialProcessData, bufferAccessor[i], m_ResourcePrefabs, ref m_ResourceDatas);
				}
				int profit = value.m_Income - (value.m_RentPaid + value.m_WagePaid + value.m_ElectricityPaid + value.m_WaterPaid + value.m_SewagePaid + value.m_GarbagePaid + value.m_TaxPaid + value.m_CostBuyResource);
				value.m_LastUpdateWorth = value.m_Worth;
				value.m_Profit = profit;
				nativeArray[i] = value;
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
		public BufferTypeHandle<Resources> __Game_Economy_Resources_RO_BufferTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Companies.ExtractorCompany> __Game_Companies_ExtractorCompany_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OfficeProperty> __Game_Buildings_OfficeProperty_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceAvailable> __Game_Companies_ServiceAvailable_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceCompanyData> __Game_Companies_ServiceCompanyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TaxPayer> __Game_Agents_TaxPayer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ConsumptionData> __Game_Prefabs_ConsumptionData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Efficiency> __Game_Buildings_Efficiency_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ServiceFee> __Game_City_ServiceFee_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Economy_Resources_RO_BufferTypeHandle = state.GetBufferTypeHandle<Resources>(isReadOnly: true);
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Companies_ExtractorCompany_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Companies.ExtractorCompany>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(isReadOnly: true);
			__Game_Buildings_OfficeProperty_RO_ComponentLookup = state.GetComponentLookup<OfficeProperty>(isReadOnly: true);
			__Game_Buildings_ElectricityConsumer_RO_ComponentLookup = state.GetComponentLookup<ElectricityConsumer>(isReadOnly: true);
			__Game_Buildings_WaterConsumer_RO_ComponentLookup = state.GetComponentLookup<WaterConsumer>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
			__Game_Companies_ServiceAvailable_RO_ComponentLookup = state.GetComponentLookup<ServiceAvailable>(isReadOnly: true);
			__Game_Companies_ServiceCompanyData_RO_ComponentLookup = state.GetComponentLookup<ServiceCompanyData>(isReadOnly: true);
			__Game_Agents_TaxPayer_RO_ComponentLookup = state.GetComponentLookup<TaxPayer>(isReadOnly: true);
			__Game_Prefabs_ConsumptionData_RO_ComponentLookup = state.GetComponentLookup<ConsumptionData>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferLookup = state.GetBufferLookup<Efficiency>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_City_ServiceFee_RO_BufferLookup = state.GetBufferLookup<ServiceFee>(isReadOnly: true);
			__Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(isReadOnly: true);
		}
	}

	public static readonly int kUpdatesPerDay = 128;

	private ResourceSystem m_ResourceSystem;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private CitySystem m_CitySystem;

	private EntityQuery m_CompanyQuery;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_227872093_0;

	private EntityQuery __query_227872093_1;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_CompanyQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[4]
			{
				ComponentType.ReadOnly<CompanyData>(),
				ComponentType.ReadOnly<Resources>(),
				ComponentType.ReadOnly<PropertyRenter>(),
				ComponentType.ReadOnly<CompanyStatisticData>()
			},
			None = new ComponentType[2]
			{
				ComponentType.Exclude<Created>(),
				ComponentType.Exclude<Deleted>()
			}
		});
		RequireForUpdate(m_CompanyQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
		CompanyEconomyStatisticJob jobData = new CompanyEconomyStatisticJob
		{
			m_CompanyStatisticType = GetComponentTypeHandle<CompanyStatisticData>(),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_ResourcesBufType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_ExtractorCompanyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_ExtractorCompany_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeliveryTrucks = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OfficeProperties = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_OfficeProperty_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElectricityConsumers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WaterConsumers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_IndustrialProcesses = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceAvailables = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceCompanyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TaxPayers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Agents_TaxPayer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ComsumptionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LayoutElementBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_EfficiencyBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup, ref base.CheckedStateRef),
			m_OwnedVehicleBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref base.CheckedStateRef),
			m_RenterBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
			m_ServiceFeeBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_ServiceFee_RO_BufferLookup, ref base.CheckedStateRef),
			m_EmployeeBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_Employee_RO_BufferLookup, ref base.CheckedStateRef),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_EconomyParameters = __query_227872093_0.GetSingleton<EconomyParameterData>(),
			m_ServiceFeeParameterData = __query_227872093_1.GetSingleton<ServiceFeeParameterData>(),
			m_CityEntity = m_CitySystem.City,
			m_UpdateFrameIndex = updateFrame
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CompanyQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<EconomyParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_227872093_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<ServiceFeeParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_227872093_1 = entityQueryBuilder2.Build(ref state);
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
	public CompanyEconomyStatisticSystem()
	{
	}
}
