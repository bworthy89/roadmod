using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class ProductionCompanyUISystem : UISystemBase
{
	[BurstCompile]
	private struct MapCompanyStatisticsJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<IndustrialCompany> m_IndustrialCompanyType;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> m_PropertyRenterType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<Employee> m_EmployeeType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDatas;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;

		[ReadOnly]
		public Resource m_Resource;

		public NativeQueue<ProductionCompanyInfo>.ParallelWriter m_Queue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			bool industrial = chunk.Has(ref m_IndustrialCompanyType);
			NativeArray<PropertyRenter> nativeArray = chunk.GetNativeArray(ref m_PropertyRenterType);
			BufferAccessor<Employee> bufferAccessor = chunk.GetBufferAccessor(ref m_EmployeeType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (m_IndustrialProcessDatas[nativeArray2[i].m_Prefab].m_Output.m_Resource == m_Resource)
				{
					Entity property = nativeArray[i].m_Property;
					if (m_PrefabRefs.HasComponent(property))
					{
						SpawnableBuildingData spawnableBuildingData = m_SpawnableBuildingDatas[m_PrefabRefs[nativeArray[i].m_Property].m_Prefab];
						m_Queue.Enqueue(new ProductionCompanyInfo
						{
							m_Industrial = industrial,
							m_Level = spawnableBuildingData.m_Level,
							m_Workers = bufferAccessor[i].Length
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

	private struct ProductionCompanyInfo
	{
		public bool m_Industrial;

		public int m_Level;

		public int m_Workers;
	}

	private struct ProductionLevelInfo : IEquatable<ProductionLevelInfo>
	{
		public int m_IndustrialCompanies;

		public int m_IndustrialWorkers;

		public int m_CommercialCompanies;

		public int m_CommercialWorkers;

		public bool Equals(ProductionLevelInfo other)
		{
			if (other.m_IndustrialCompanies == m_IndustrialCompanies && other.m_IndustrialWorkers == m_IndustrialWorkers && other.m_CommercialCompanies == m_CommercialCompanies)
			{
				return other.m_CommercialWorkers == m_CommercialWorkers;
			}
			return false;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<IndustrialCompany> __Game_Companies_IndustrialCompany_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Employee> __Game_Companies_Employee_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Companies_IndustrialCompany_RO_ComponentTypeHandle = state.GetComponentTypeHandle<IndustrialCompany>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PropertyRenter>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Companies_Employee_RO_BufferTypeHandle = state.GetBufferTypeHandle<Employee>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
		}
	}

	private static readonly string kGroup = "production";

	private static readonly int kLevels = 5;

	private IBudgetSystem m_BudgetSystem;

	private ResourceSystem m_ResourceSystem;

	private PrefabSystem m_PrefabSystem;

	private Dictionary<string, Resource> m_ResourceIDMap;

	private RawValueBinding m_ProductionCompanyInfoBinding;

	private ValueBinding<int> m_IndustrialCompanyWealthBinding;

	private ValueBinding<int> m_CommercialCompanyWealthBinding;

	private NativeArray<ProductionLevelInfo> m_CachedValues;

	private NativeArray<ProductionLevelInfo> m_Values;

	private Resource m_SelectedResource;

	private NativeQueue<ProductionCompanyInfo> m_ProductionCompanyInfoQueue;

	private EntityQuery m_CompanyQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_BudgetSystem = base.World.GetOrCreateSystemManaged<BudgetSystem>();
		m_CompanyQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<PropertyRenter>(),
				ComponentType.ReadOnly<Employee>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<IndustrialCompany>(),
				ComponentType.ReadOnly<CommercialCompany>()
			}
		});
		m_SelectedResource = Resource.NoResource;
		m_ResourceIDMap = new Dictionary<string, Resource>();
		m_CachedValues = new NativeArray<ProductionLevelInfo>(kLevels, Allocator.Persistent);
		m_Values = new NativeArray<ProductionLevelInfo>(kLevels, Allocator.Persistent);
		m_ProductionCompanyInfoQueue = new NativeQueue<ProductionCompanyInfo>(Allocator.Persistent);
		AddBinding(m_ProductionCompanyInfoBinding = new RawValueBinding(kGroup, "productionCompanyInfo", UpdateProductionCompanyInfo));
		AddBinding(m_IndustrialCompanyWealthBinding = new ValueBinding<int>(kGroup, "industrialCompanyWealth", 0));
		AddBinding(m_CommercialCompanyWealthBinding = new ValueBinding<int>(kGroup, "commercialCompanyWealth", 0));
		AddBinding(new TriggerBinding<string>(kGroup, "selectResource", OnSelectResource));
		RebuildResourceIDMap();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_CachedValues.Dispose();
		m_Values.Dispose();
		m_ProductionCompanyInfoQueue.Dispose();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ProductionCompanyInfoBinding.active)
		{
			PatchProductionCompanyInfo();
		}
		if (m_IndustrialCompanyWealthBinding.active)
		{
			m_IndustrialCompanyWealthBinding.Update((m_SelectedResource != Resource.NoResource) ? m_BudgetSystem.GetCompanyWealth(service: false, m_SelectedResource) : 0);
		}
		if (m_CommercialCompanyWealthBinding.active)
		{
			m_CommercialCompanyWealthBinding.Update((m_SelectedResource != Resource.NoResource) ? m_BudgetSystem.GetCompanyWealth(service: true, m_SelectedResource) : 0);
		}
	}

	private void UpdateProductionCompanyInfo(IJsonWriter binder)
	{
		binder.ArrayBegin(kLevels);
		for (int i = 0; i < kLevels; i++)
		{
			binder.TypeBegin("production.ProductionCompanyInfo");
			binder.PropertyName("industrialCompanies");
			binder.Write(0);
			binder.PropertyName("industrialWorkers");
			binder.Write(0);
			binder.PropertyName("commercialCompanies");
			binder.Write(0);
			binder.PropertyName("commercialWorkers");
			binder.Write(0);
			binder.TypeEnd();
		}
		binder.ArrayEnd();
	}

	private void PatchProductionCompanyInfo()
	{
		m_ProductionCompanyInfoQueue.Clear();
		for (int i = 0; i < m_Values.Length; i++)
		{
			m_Values[i] = default(ProductionLevelInfo);
		}
		if (m_SelectedResource != Resource.NoResource)
		{
			MapCompanyStatisticsJob jobData = new MapCompanyStatisticsJob
			{
				m_IndustrialCompanyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_IndustrialCompany_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PropertyRenterType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EmployeeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Companies_Employee_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SpawnableBuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_IndustrialProcessDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Resource = m_SelectedResource,
				m_Queue = m_ProductionCompanyInfoQueue.AsParallelWriter()
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_CompanyQuery, base.Dependency);
			base.Dependency.Complete();
			ProductionCompanyInfo item;
			while (m_ProductionCompanyInfoQueue.TryDequeue(out item))
			{
				ProductionLevelInfo value = m_Values[item.m_Level - 1];
				if (item.m_Industrial)
				{
					value.m_IndustrialCompanies++;
					value.m_IndustrialWorkers += item.m_Workers;
				}
				else
				{
					value.m_CommercialCompanies++;
					value.m_CommercialWorkers += item.m_Workers;
				}
				m_Values[item.m_Level - 1] = value;
			}
		}
		for (int j = 0; j < m_CachedValues.Length; j++)
		{
			ProductionLevelInfo productionLevelInfo = m_CachedValues[j];
			ProductionLevelInfo value2 = m_Values[j];
			m_CachedValues[j] = value2;
			if (productionLevelInfo.m_IndustrialCompanies != value2.m_IndustrialCompanies)
			{
				Patch(j, "industrialCompanies", value2.m_IndustrialCompanies);
			}
			if (productionLevelInfo.m_IndustrialWorkers != value2.m_IndustrialWorkers)
			{
				Patch(j, "industrialWorkers", value2.m_IndustrialWorkers);
			}
			if (productionLevelInfo.m_CommercialCompanies != value2.m_CommercialCompanies)
			{
				Patch(j, "commercialCompanies", value2.m_CommercialCompanies);
			}
			if (productionLevelInfo.m_CommercialWorkers != value2.m_CommercialWorkers)
			{
				Patch(j, "commercialWorkers", value2.m_CommercialWorkers);
			}
		}
	}

	private void Patch(int index, string fieldName, int value)
	{
		IJsonWriter jsonWriter = m_ProductionCompanyInfoBinding.PatchBegin();
		jsonWriter.ArrayBegin(2u);
		jsonWriter.Write(index);
		jsonWriter.Write(fieldName);
		jsonWriter.ArrayEnd();
		jsonWriter.Write(value);
		m_ProductionCompanyInfoBinding.PatchEnd();
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		RebuildResourceIDMap();
		for (int i = 0; i < m_CachedValues.Length; i++)
		{
			m_CachedValues[i] = default(ProductionLevelInfo);
		}
		for (int j = 0; j < m_Values.Length; j++)
		{
			m_Values[j] = default(ProductionLevelInfo);
		}
		m_ProductionCompanyInfoQueue.Clear();
	}

	private void RebuildResourceIDMap()
	{
		m_ResourceIDMap.Clear();
		ResourceIterator iterator = ResourceIterator.GetIterator();
		ResourcePrefabs prefabs = m_ResourceSystem.GetPrefabs();
		while (iterator.Next())
		{
			Entity entity = prefabs[iterator.resource];
			if (entity != Entity.Null)
			{
				ResourcePrefab prefab = m_PrefabSystem.GetPrefab<ResourcePrefab>(entity);
				m_ResourceIDMap[prefab.name] = iterator.resource;
			}
		}
	}

	private void OnSelectResource(string resourceID)
	{
		if (m_ResourceIDMap.TryGetValue(resourceID, out var value))
		{
			m_SelectedResource = value;
		}
		else
		{
			m_SelectedResource = Resource.NoResource;
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
	public ProductionCompanyUISystem()
	{
	}
}
