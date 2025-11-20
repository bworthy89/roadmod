using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Debug;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
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
public class CountCompanyDataSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	public struct CommercialCompanyDatas
	{
		public NativeArray<int> m_CurrentServiceWorkers;

		public NativeArray<int> m_MaxServiceWorkers;

		public NativeArray<int> m_ProduceCapacity;

		public NativeArray<int> m_CurrentAvailables;

		public NativeArray<int> m_TotalAvailables;

		public NativeArray<int> m_ServiceCompanies;

		public NativeArray<int> m_ServicePropertyless;
	}

	public struct IndustrialCompanyDatas
	{
		public NativeArray<int> m_CurrentProductionWorkers;

		public NativeArray<int> m_MaxProductionWorkers;

		public NativeArray<int> m_Production;

		public NativeArray<int> m_Demand;

		public NativeArray<int> m_ProductionCompanies;

		public NativeArray<int> m_ProductionPropertyless;
	}

	private struct CompanyDataItem
	{
		public int m_Resource;

		public int m_CurrentProductionWorkers;

		public int m_MaxProductionWorkers;

		public int m_CurrentServiceWorkers;

		public int m_MaxServiceWorkers;

		public int m_Production;

		public int m_SalesCapacities;

		public int m_CurrentAvailables;

		public int m_TotalAvailables;

		public int m_Demand;

		public int m_ProductionCompanies;

		public int m_ServiceCompanies;

		public int m_ProductionPropertyless;

		public int m_ServicePropertyless;

		public int m_TotalSellableInCity;
	}

	[BurstCompile]
	private struct ResetJob : IJob
	{
		public NativeArray<int> m_CurrentProductionWorkers;

		public NativeArray<int> m_MaxProductionWorkers;

		public NativeArray<int> m_CurrentServiceWorkers;

		public NativeArray<int> m_MaxServiceWorkers;

		public NativeArray<int> m_Production;

		public NativeArray<int> m_SalesCapacities;

		public NativeArray<int> m_CurrentAvailables;

		public NativeArray<int> m_TotalAvailables;

		public NativeArray<int> m_Demand;

		public NativeArray<int> m_ProductionCompanies;

		public NativeArray<int> m_ServiceCompanies;

		public NativeArray<int> m_ProductionPropertyless;

		public NativeArray<int> m_ServicePropertyless;

		public NativeArray<int> m_TotalSellableInCity;

		public void Execute()
		{
			m_CurrentProductionWorkers.Fill(0);
			m_MaxProductionWorkers.Fill(0);
			m_CurrentServiceWorkers.Fill(0);
			m_MaxServiceWorkers.Fill(0);
			m_Production.Fill(0);
			m_SalesCapacities.Fill(0);
			m_CurrentAvailables.Fill(0);
			m_TotalAvailables.Fill(0);
			m_Demand.Fill(0);
			m_ProductionCompanies.Fill(0);
			m_ServiceCompanies.Fill(0);
			m_ProductionPropertyless.Fill(0);
			m_ServicePropertyless.Fill(0);
			m_TotalSellableInCity.Fill(0);
		}
	}

	[BurstCompile]
	private struct SumJob : IJob
	{
		public NativeArray<int> m_CurrentProductionWorkers;

		public NativeArray<int> m_MaxProductionWorkers;

		public NativeArray<int> m_CurrentServiceWorkers;

		public NativeArray<int> m_MaxServiceWorkers;

		public NativeArray<int> m_Production;

		public NativeArray<int> m_SalesCapacities;

		public NativeArray<int> m_CurrentAvailables;

		public NativeArray<int> m_TotalAvailables;

		public NativeArray<int> m_Demand;

		public NativeArray<int> m_ProductionCompanies;

		public NativeArray<int> m_ServiceCompanies;

		public NativeArray<int> m_ProductionPropertyless;

		public NativeArray<int> m_ServicePropertyless;

		public NativeArray<int> m_TotalSellableInCity;

		public NativeQueue<CompanyDataItem> m_DataQueue;

		public void Execute()
		{
			m_CurrentProductionWorkers.Fill(0);
			m_MaxProductionWorkers.Fill(0);
			m_CurrentServiceWorkers.Fill(0);
			m_MaxServiceWorkers.Fill(0);
			m_Production.Fill(0);
			m_SalesCapacities.Fill(0);
			m_CurrentAvailables.Fill(0);
			m_TotalAvailables.Fill(0);
			m_Demand.Fill(0);
			m_ProductionCompanies.Fill(0);
			m_ServiceCompanies.Fill(0);
			m_ProductionPropertyless.Fill(0);
			m_ServicePropertyless.Fill(0);
			m_TotalSellableInCity.Fill(0);
			CompanyDataItem item;
			while (m_DataQueue.TryDequeue(out item))
			{
				int index = item.m_Resource;
				m_CurrentProductionWorkers[index] += item.m_CurrentProductionWorkers;
				m_MaxProductionWorkers[index] += item.m_MaxProductionWorkers;
				m_CurrentServiceWorkers[index] += item.m_CurrentServiceWorkers;
				m_MaxServiceWorkers[index] += item.m_MaxServiceWorkers;
				m_Production[index] += item.m_Production;
				m_SalesCapacities[index] += item.m_SalesCapacities;
				m_CurrentAvailables[index] += item.m_CurrentAvailables;
				m_TotalAvailables[index] += item.m_TotalAvailables;
				m_Demand[index] += item.m_Demand;
				m_ProductionCompanies[index] += item.m_ProductionCompanies;
				m_ServiceCompanies[index] += item.m_ServiceCompanies;
				m_ProductionPropertyless[index] += item.m_ProductionPropertyless;
				m_ServicePropertyless[index] += item.m_ServicePropertyless;
				m_TotalSellableInCity[index] += item.m_TotalSellableInCity;
			}
		}
	}

	[BurstCompile]
	private struct CountCompanyDataJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<WorkProvider> m_WorkProviderType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentTypeHandle<ServiceAvailable> m_ServiceAvailableType;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public BufferLookup<Employee> m_Employees;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public BufferLookup<Efficiency> m_BuildingEfficiencyBuf;

		[ReadOnly]
		public BufferLookup<Resources> m_ResourcesBufs;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public ComponentLookup<ServiceCompanyData> m_ServiceCompanyDatas;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> m_StorageCompanyDatas;

		public EconomyParameterData m_EconomyParameters;

		public NativeQueue<CompanyDataItem>.ParallelWriter m_DataQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<WorkProvider> nativeArray2 = chunk.GetNativeArray(ref m_WorkProviderType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabType);
			NativeArray<ServiceAvailable> nativeArray4 = chunk.GetNativeArray(ref m_ServiceAvailableType);
			bool service = chunk.Has(ref m_ServiceAvailableType);
			int resourceCount = EconomyUtils.ResourceCount;
			NativeArray<int> currentProductionWorkers = new NativeArray<int>(resourceCount, Allocator.Temp);
			NativeArray<int> maxProductionWorkers = new NativeArray<int>(resourceCount, Allocator.Temp);
			NativeArray<int> currentServiceWorkers = new NativeArray<int>(resourceCount, Allocator.Temp);
			NativeArray<int> maxServiceWorkers = new NativeArray<int>(resourceCount, Allocator.Temp);
			NativeArray<int> production = new NativeArray<int>(resourceCount, Allocator.Temp);
			NativeArray<int> salesCapacities = new NativeArray<int>(resourceCount, Allocator.Temp);
			NativeArray<int> currentAvailables = new NativeArray<int>(resourceCount, Allocator.Temp);
			NativeArray<int> totalAvailables = new NativeArray<int>(resourceCount, Allocator.Temp);
			NativeArray<int> demand = new NativeArray<int>(resourceCount, Allocator.Temp);
			NativeArray<int> productionCompanies = new NativeArray<int>(resourceCount, Allocator.Temp);
			NativeArray<int> serviceCompanies = new NativeArray<int>(resourceCount, Allocator.Temp);
			NativeArray<int> productionPropertyless = new NativeArray<int>(resourceCount, Allocator.Temp);
			NativeArray<int> servicePropertyless = new NativeArray<int>(resourceCount, Allocator.Temp);
			NativeArray<int> nativeArray5 = new NativeArray<int>(resourceCount, Allocator.Temp);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Entity prefab = nativeArray3[i].m_Prefab;
				DynamicBuffer<Resources> dynamicBuffer = m_ResourcesBufs[entity];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					if (m_PropertyRenters.HasComponent(entity) && !(m_PropertyRenters[entity].m_Property == Entity.Null))
					{
						Resources resources = dynamicBuffer[j];
						if (resources.m_Resource != Resource.Money && (!m_IndustrialProcessDatas.HasComponent(prefab) || (m_IndustrialProcessDatas[prefab].m_Input1.m_Resource != resources.m_Resource && m_IndustrialProcessDatas[prefab].m_Input2.m_Resource != resources.m_Resource)))
						{
							nativeArray5[EconomyUtils.GetResourceIndex(resources.m_Resource)] += resources.m_Amount;
						}
					}
				}
				if (m_IndustrialProcessDatas.HasComponent(prefab) && nativeArray2.Length > 0)
				{
					ProcessCompanies(prefab, entity, nativeArray2[i], service, currentAvailables, nativeArray4, i, totalAvailables, demand, maxServiceWorkers, currentServiceWorkers, salesCapacities, maxProductionWorkers, currentProductionWorkers, production, servicePropertyless, productionPropertyless, serviceCompanies, productionCompanies);
				}
			}
			for (int k = 0; k < production.Length; k++)
			{
				CompanyDataItem value = new CompanyDataItem
				{
					m_Resource = k,
					m_Demand = demand[k],
					m_Production = production[k],
					m_CurrentAvailables = currentAvailables[k],
					m_ProductionCompanies = productionCompanies[k],
					m_ProductionPropertyless = productionPropertyless[k],
					m_SalesCapacities = salesCapacities[k],
					m_ServiceCompanies = serviceCompanies[k],
					m_ServicePropertyless = servicePropertyless[k],
					m_TotalAvailables = totalAvailables[k],
					m_CurrentProductionWorkers = currentProductionWorkers[k],
					m_CurrentServiceWorkers = currentServiceWorkers[k],
					m_MaxProductionWorkers = maxProductionWorkers[k],
					m_MaxServiceWorkers = maxServiceWorkers[k],
					m_TotalSellableInCity = nativeArray5[k]
				};
				m_DataQueue.Enqueue(value);
			}
			currentProductionWorkers.Dispose();
			maxProductionWorkers.Dispose();
			currentServiceWorkers.Dispose();
			maxServiceWorkers.Dispose();
			production.Dispose();
			salesCapacities.Dispose();
			currentAvailables.Dispose();
			totalAvailables.Dispose();
			demand.Dispose();
			productionCompanies.Dispose();
			serviceCompanies.Dispose();
			productionPropertyless.Dispose();
			servicePropertyless.Dispose();
		}

		private void ProcessCompanies(Entity companyPrefab, Entity company, WorkProvider workProvider, bool service, NativeArray<int> currentAvailables, NativeArray<ServiceAvailable> services, int j, NativeArray<int> totalAvailables, NativeArray<int> demand, NativeArray<int> maxServiceWorkers, NativeArray<int> currentServiceWorkers, NativeArray<int> salesCapacities, NativeArray<int> maxProductionWorkers, NativeArray<int> currentProductionWorkers, NativeArray<int> production, NativeArray<int> servicePropertyless, NativeArray<int> productionPropertyless, NativeArray<int> serviceCompanies, NativeArray<int> productionCompanies)
		{
			IndustrialProcessData processData = m_IndustrialProcessDatas[companyPrefab];
			Resource resource = processData.m_Output.m_Resource;
			if (m_PropertyRenters.HasComponent(company))
			{
				if (resource != Resource.NoResource)
				{
					int resourceIndex = EconomyUtils.GetResourceIndex(resource);
					Entity property = m_PropertyRenters[company].m_Property;
					DynamicBuffer<Employee> employees = m_Employees[company];
					int maxWorkers = workProvider.m_MaxWorkers;
					ServiceCompanyData serviceCompanyData = default(ServiceCompanyData);
					ServiceAvailable serviceAvailable = default(ServiceAvailable);
					if (service)
					{
						serviceAvailable = services[j];
						serviceCompanyData = m_ServiceCompanyDatas[companyPrefab];
						currentAvailables[resourceIndex] += math.clamp(services[j].m_ServiceAvailable, 0, serviceCompanyData.m_MaxService);
						totalAvailables[resourceIndex] += serviceCompanyData.m_MaxService;
					}
					float buildingEfficiency = 1f;
					if (m_BuildingEfficiencyBuf.TryGetBuffer(property, out var bufferData))
					{
						buildingEfficiency = BuildingUtils.GetEfficiency(bufferData);
					}
					int companyProductionPerDay = EconomyUtils.GetCompanyProductionPerDay(buildingEfficiency, !service, employees, processData, m_ResourcePrefabs, ref m_ResourceDatas, ref m_Citizens, ref m_EconomyParameters, serviceAvailable, serviceCompanyData);
					if (processData.m_Input1.m_Resource != Resource.NoResource)
					{
						demand[EconomyUtils.GetResourceIndex(processData.m_Input1.m_Resource)] += GetRoundedConsumption(processData.m_Input1.m_Amount, processData.m_Output.m_Amount, companyProductionPerDay);
					}
					if (processData.m_Input2.m_Resource != Resource.NoResource)
					{
						demand[EconomyUtils.GetResourceIndex(processData.m_Input2.m_Resource)] += GetRoundedConsumption(processData.m_Input2.m_Amount, processData.m_Output.m_Amount, companyProductionPerDay);
					}
					if (service)
					{
						maxServiceWorkers[resourceIndex] += maxWorkers;
						currentServiceWorkers[resourceIndex] += employees.Length;
						salesCapacities[resourceIndex] += companyProductionPerDay;
					}
					else
					{
						maxProductionWorkers[resourceIndex] += maxWorkers;
						currentProductionWorkers[resourceIndex] += employees.Length;
						production[resourceIndex] += companyProductionPerDay;
					}
				}
			}
			else if (resource != Resource.NoResource)
			{
				if (service)
				{
					servicePropertyless[EconomyUtils.GetResourceIndex(resource)]++;
				}
				else
				{
					productionPropertyless[EconomyUtils.GetResourceIndex(resource)]++;
				}
			}
			if (resource != Resource.NoResource)
			{
				if (service)
				{
					serviceCompanies[EconomyUtils.GetResourceIndex(resource)]++;
				}
				else
				{
					productionCompanies[EconomyUtils.GetResourceIndex(resource)]++;
				}
			}
		}

		private int GetRoundedConsumption(int inputAmount, int outputAmount, int production)
		{
			return (int)(((long)inputAmount * (long)production + (outputAmount >> 1)) / outputAmount);
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
		public ComponentTypeHandle<WorkProvider> __Game_Companies_WorkProvider_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ServiceAvailable> __Game_Companies_ServiceAvailable_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Resources> __Game_Economy_Resources_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Efficiency> __Game_Buildings_Efficiency_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<ServiceCompanyData> __Game_Companies_ServiceCompanyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> __Game_Prefabs_StorageCompanyData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Companies_WorkProvider_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WorkProvider>(isReadOnly: true);
			__Game_Companies_ServiceAvailable_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceAvailable>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(isReadOnly: true);
			__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Resources>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferLookup = state.GetBufferLookup<Efficiency>(isReadOnly: true);
			__Game_Companies_ServiceCompanyData_RO_ComponentLookup = state.GetComponentLookup<ServiceCompanyData>(isReadOnly: true);
			__Game_Prefabs_StorageCompanyData_RO_ComponentLookup = state.GetComponentLookup<StorageCompanyData>(isReadOnly: true);
		}
	}

	private ResourceSystem m_ResourceSystem;

	private NativeQueue<CompanyDataItem> m_DataQueue;

	private EntityQuery m_EconomyParameterQuery;

	[DebugWatchDeps]
	private JobHandle m_WriteDependencies;

	private JobHandle m_ReadDependencies;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_CurrentProductionWorkers;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_MaxProductionWorkers;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_CurrentServiceWorkers;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_MaxServiceWorkers;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_Production;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_SalesCapacities;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_CurrentAvailables;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_TotalAvailables;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_Demand;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_ProductionCompanies;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_ServiceCompanies;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_ProductionPropertyless;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_ServicePropertyless;

	[ResourceArray]
	[DebugWatchValue]
	private NativeArray<int> m_TotalSellableInCity;

	private EntityQuery m_CompanyQuery;

	private bool m_WasReset;

	private TypeHandle __TypeHandle;

	public CommercialCompanyDatas GetCommercialCompanyDatas(out JobHandle deps)
	{
		deps = m_WriteDependencies;
		return new CommercialCompanyDatas
		{
			m_CurrentAvailables = m_CurrentAvailables,
			m_ProduceCapacity = m_SalesCapacities,
			m_ServiceCompanies = m_ServiceCompanies,
			m_ServicePropertyless = m_ServicePropertyless,
			m_TotalAvailables = m_TotalAvailables,
			m_CurrentServiceWorkers = m_CurrentServiceWorkers,
			m_MaxServiceWorkers = m_MaxServiceWorkers
		};
	}

	public IndustrialCompanyDatas GetIndustrialCompanyDatas(out JobHandle deps)
	{
		deps = m_WriteDependencies;
		return new IndustrialCompanyDatas
		{
			m_Demand = m_Demand,
			m_Production = m_Production,
			m_ProductionCompanies = m_ProductionCompanies,
			m_ProductionPropertyless = m_ProductionPropertyless,
			m_CurrentProductionWorkers = m_CurrentProductionWorkers,
			m_MaxProductionWorkers = m_MaxProductionWorkers
		};
	}

	public NativeArray<int> GetProduction(out JobHandle deps)
	{
		deps = m_WriteDependencies;
		return m_Production;
	}

	public NativeArray<int> GetTotalSellableInCity(out JobHandle deps)
	{
		deps = m_WriteDependencies;
		return m_TotalSellableInCity;
	}

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 1;
	}

	public void AddReader(JobHandle reader)
	{
		m_ReadDependencies = JobHandle.CombineDependencies(m_ReadDependencies, reader);
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_Production.Fill(0);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		m_CompanyQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Resources>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Companies.ProcessingCompany>(),
				ComponentType.ReadOnly<Game.Companies.StorageCompany>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Game.Objects.OutsideConnection>(),
				ComponentType.ReadOnly<Game.Buildings.CargoTransportStation>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		int resourceCount = EconomyUtils.ResourceCount;
		m_CurrentProductionWorkers = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_MaxProductionWorkers = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_CurrentServiceWorkers = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_MaxServiceWorkers = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_Production = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_SalesCapacities = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_CurrentAvailables = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_TotalAvailables = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_Demand = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_ProductionCompanies = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_ServiceCompanies = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_ProductionPropertyless = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_ServicePropertyless = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_TotalSellableInCity = new NativeArray<int>(resourceCount, Allocator.Persistent);
		m_DataQueue = new NativeQueue<CompanyDataItem>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_CurrentProductionWorkers.Dispose();
		m_MaxProductionWorkers.Dispose();
		m_CurrentServiceWorkers.Dispose();
		m_MaxServiceWorkers.Dispose();
		m_Production.Dispose();
		m_SalesCapacities.Dispose();
		m_CurrentAvailables.Dispose();
		m_TotalAvailables.Dispose();
		m_Demand.Dispose();
		m_ProductionCompanies.Dispose();
		m_ServiceCompanies.Dispose();
		m_ProductionPropertyless.Dispose();
		m_ServicePropertyless.Dispose();
		m_TotalSellableInCity.Dispose();
		m_DataQueue.Dispose();
		base.OnDestroy();
	}

	public void SetDefaults(Context context)
	{
		if (!m_WasReset)
		{
			m_CurrentProductionWorkers.Fill(0);
			m_MaxProductionWorkers.Fill(0);
			m_CurrentServiceWorkers.Fill(0);
			m_MaxServiceWorkers.Fill(0);
			m_Production.Fill(0);
			m_SalesCapacities.Fill(0);
			m_CurrentAvailables.Fill(0);
			m_TotalAvailables.Fill(0);
			m_Demand.Fill(0);
			m_ProductionCompanies.Fill(0);
			m_ServiceCompanies.Fill(0);
			m_ProductionPropertyless.Fill(0);
			m_ServicePropertyless.Fill(0);
			m_TotalSellableInCity.Fill(0);
			m_WasReset = true;
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		NativeArray<int> value = m_CurrentProductionWorkers;
		writer.Write(value);
		NativeArray<int> value2 = m_MaxProductionWorkers;
		writer.Write(value2);
		NativeArray<int> value3 = m_CurrentServiceWorkers;
		writer.Write(value3);
		NativeArray<int> value4 = m_MaxServiceWorkers;
		writer.Write(value4);
		NativeArray<int> value5 = m_Production;
		writer.Write(value5);
		NativeArray<int> value6 = m_SalesCapacities;
		writer.Write(value6);
		NativeArray<int> value7 = m_CurrentAvailables;
		writer.Write(value7);
		NativeArray<int> value8 = m_TotalAvailables;
		writer.Write(value8);
		NativeArray<int> value9 = m_Demand;
		writer.Write(value9);
		NativeArray<int> value10 = m_ProductionCompanies;
		writer.Write(value10);
		NativeArray<int> value11 = m_ServiceCompanies;
		writer.Write(value11);
		NativeArray<int> value12 = m_ProductionPropertyless;
		writer.Write(value12);
		NativeArray<int> value13 = m_ServicePropertyless;
		writer.Write(value13);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		int num = EconomyUtils.ResourceCount;
		if (!reader.context.format.Has(FormatTags.FishResource))
		{
			num--;
			m_CurrentProductionWorkers[num] = 0;
			m_MaxProductionWorkers[num] = 0;
			m_CurrentServiceWorkers[num] = 0;
			m_MaxServiceWorkers[num] = 0;
			m_Production[num] = 0;
			m_SalesCapacities[num] = 0;
			m_CurrentAvailables[num] = 0;
			m_TotalAvailables[num] = 0;
			m_Demand[num] = 0;
			m_ProductionCompanies[num] = 0;
			m_ServiceCompanies[num] = 0;
			m_ProductionPropertyless[num] = 0;
			m_ServicePropertyless[num] = 0;
		}
		NativeArray<int> nativeArray = new NativeArray<int>(num, Allocator.Temp);
		NativeArray<int> value = nativeArray;
		reader.Read(value);
		CollectionUtils.CopySafe(nativeArray, m_CurrentProductionWorkers);
		NativeArray<int> value2 = nativeArray;
		reader.Read(value2);
		CollectionUtils.CopySafe(nativeArray, m_MaxProductionWorkers);
		NativeArray<int> value3 = nativeArray;
		reader.Read(value3);
		CollectionUtils.CopySafe(nativeArray, m_CurrentServiceWorkers);
		NativeArray<int> value4 = nativeArray;
		reader.Read(value4);
		CollectionUtils.CopySafe(nativeArray, m_MaxServiceWorkers);
		NativeArray<int> value5 = nativeArray;
		reader.Read(value5);
		CollectionUtils.CopySafe(nativeArray, m_Production);
		NativeArray<int> value6 = nativeArray;
		reader.Read(value6);
		CollectionUtils.CopySafe(nativeArray, m_SalesCapacities);
		NativeArray<int> value7 = nativeArray;
		reader.Read(value7);
		CollectionUtils.CopySafe(nativeArray, m_CurrentAvailables);
		NativeArray<int> value8 = nativeArray;
		reader.Read(value8);
		CollectionUtils.CopySafe(nativeArray, m_TotalAvailables);
		NativeArray<int> value9 = nativeArray;
		reader.Read(value9);
		CollectionUtils.CopySafe(nativeArray, m_Demand);
		NativeArray<int> value10 = nativeArray;
		reader.Read(value10);
		CollectionUtils.CopySafe(nativeArray, m_ProductionCompanies);
		NativeArray<int> value11 = nativeArray;
		reader.Read(value11);
		CollectionUtils.CopySafe(nativeArray, m_ServiceCompanies);
		NativeArray<int> value12 = nativeArray;
		reader.Read(value12);
		CollectionUtils.CopySafe(nativeArray, m_ProductionPropertyless);
		NativeArray<int> value13 = nativeArray;
		reader.Read(value13);
		CollectionUtils.CopySafe(nativeArray, m_ServicePropertyless);
		nativeArray.Dispose();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_CompanyQuery.IsEmptyIgnoreFilter)
		{
			if (!m_WasReset)
			{
				ResetJob jobData = new ResetJob
				{
					m_CurrentProductionWorkers = m_CurrentProductionWorkers,
					m_MaxProductionWorkers = m_MaxProductionWorkers,
					m_CurrentServiceWorkers = m_CurrentServiceWorkers,
					m_MaxServiceWorkers = m_MaxServiceWorkers,
					m_Production = m_Production,
					m_SalesCapacities = m_SalesCapacities,
					m_CurrentAvailables = m_CurrentAvailables,
					m_TotalAvailables = m_TotalAvailables,
					m_Demand = m_Demand,
					m_ProductionCompanies = m_ProductionCompanies,
					m_ServiceCompanies = m_ServiceCompanies,
					m_ProductionPropertyless = m_ProductionPropertyless,
					m_ServicePropertyless = m_ServicePropertyless,
					m_TotalSellableInCity = m_TotalSellableInCity
				};
				m_WriteDependencies = IJobExtensions.Schedule(jobData, base.Dependency);
				m_WasReset = true;
			}
		}
		else
		{
			m_WasReset = false;
			JobHandle dependsOn = JobChunkExtensions.ScheduleParallel(new CountCompanyDataJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_WorkProviderType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_WorkProvider_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ServiceAvailableType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_ServiceAvailable_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_IndustrialProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Employees = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_Employee_RO_BufferLookup, ref base.CheckedStateRef),
				m_ResourcesBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref base.CheckedStateRef),
				m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
				m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingEfficiencyBuf = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup, ref base.CheckedStateRef),
				m_ServiceCompanyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_ServiceCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_StorageCompanyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StorageCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EconomyParameters = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
				m_DataQueue = m_DataQueue.AsParallelWriter()
			}, m_CompanyQuery, JobHandle.CombineDependencies(base.Dependency, m_WriteDependencies, m_ReadDependencies));
			SumJob jobData2 = new SumJob
			{
				m_Demand = m_Demand,
				m_Production = m_Production,
				m_CurrentAvailables = m_CurrentAvailables,
				m_ProductionCompanies = m_ProductionCompanies,
				m_ProductionPropertyless = m_ProductionPropertyless,
				m_SalesCapacities = m_SalesCapacities,
				m_ServiceCompanies = m_ServiceCompanies,
				m_ServicePropertyless = m_ServicePropertyless,
				m_TotalAvailables = m_TotalAvailables,
				m_CurrentProductionWorkers = m_CurrentProductionWorkers,
				m_CurrentServiceWorkers = m_CurrentServiceWorkers,
				m_MaxProductionWorkers = m_MaxProductionWorkers,
				m_MaxServiceWorkers = m_MaxServiceWorkers,
				m_TotalSellableInCity = m_TotalSellableInCity,
				m_DataQueue = m_DataQueue
			};
			base.Dependency = IJobExtensions.Schedule(jobData2, dependsOn);
			m_WriteDependencies = base.Dependency;
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
	public CountCompanyDataSystem()
	{
	}
}
