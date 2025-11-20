using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Citizens;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class CompanySection : InfoSectionBase
{
	private enum IndustrialType
	{
		None,
		Storage,
		Extractor,
		Processing
	}

	private enum ResultType
	{
		Income,
		BankBalance,
		Profit,
		WagesPaid,
		RentPaid,
		ElectricityPaid,
		WaterFeePaid,
		SewageFeePaid,
		GarbageFeePaid,
		TaxesPaid,
		ResourcesBoughtPaid,
		DailyCustomers,
		ProductionRate,
		Count
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
		}
	}

	private Entity companyEntity;

	private NativeArray<int> m_Results;

	private ResourceSystem m_ResourceSystem;

	private ResourcePrefabs m_ResourcePrefabs;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_2105106401_0;

	protected override string group => "CompanySection";

	private Resource input1 { get; set; }

	private Resource input2 { get; set; }

	private Resource output { get; set; }

	private Resource sells { get; set; }

	private Resource stores { get; set; }

	private int2 hotelGuests { get; set; }

	private float price { get; set; }

	private bool isRentable { get; set; }

	private bool isCommercial { get; set; }

	private bool isStorage { get; set; }

	private IndustrialType industrialType { get; set; }

	private string outputUnit { get; set; }

	protected override void Reset()
	{
		companyEntity = Entity.Null;
		input1 = Resource.NoResource;
		input2 = Resource.NoResource;
		output = Resource.NoResource;
		sells = Resource.NoResource;
		stores = Resource.NoResource;
		hotelGuests = int2.zero;
		isRentable = false;
		isCommercial = false;
		industrialType = IndustrialType.None;
		outputUnit = "integerPerMonth";
	}

	private bool Visible()
	{
		return CompanyUIUtils.HasCompany(base.EntityManager, selectedEntity, selectedPrefab, out companyEntity);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_ResourcePrefabs = base.World.GetOrCreateSystemManaged<ResourceSystem>().GetPrefabs();
		m_Results = new NativeArray<int>(13, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_Results.Dispose();
	}

	protected override void OnProcess()
	{
		if (companyEntity == Entity.Null && !base.EntityManager.HasComponent<Abandoned>(selectedEntity))
		{
			if (base.EntityManager.TryGetComponent<SpawnableBuildingData>(selectedPrefab, out var component) && base.EntityManager.TryGetComponent<ZoneData>(component.m_ZonePrefab, out var component2))
			{
				switch (component2.m_AreaType)
				{
				case AreaType.Commercial:
					base.tooltipKeys.Add("VacantCommercial");
					break;
				case AreaType.Industrial:
					base.tooltipKeys.Add(component2.IsOffice() ? "VacantOffice" : "VacantIndustrial");
					break;
				}
			}
			if (base.EntityManager.HasComponent<PropertyOnMarket>(selectedEntity))
			{
				isRentable = true;
			}
		}
		if (base.EntityManager.TryGetBuffer(companyEntity, isReadOnly: true, out DynamicBuffer<Resources> _) && base.EntityManager.TryGetComponent<PrefabRef>(companyEntity, out var component3) && base.EntityManager.TryGetComponent<IndustrialProcessData>(component3.m_Prefab, out var component4))
		{
			if (base.EntityManager.HasComponent<ServiceAvailable>(companyEntity))
			{
				Resource resource = component4.m_Input1.m_Resource;
				Resource resource2 = component4.m_Input2.m_Resource;
				Resource resource3 = component4.m_Output.m_Resource;
				if (resource != Resource.NoResource && resource != resource3)
				{
					input1 = resource;
				}
				if (resource2 != Resource.NoResource && resource2 != resource3 && resource2 != resource)
				{
					input2 = resource2;
					base.tooltipKeys.Add("Requires");
				}
				sells = resource3;
				base.tooltipKeys.Add("Sells");
			}
			else if (base.EntityManager.HasComponent<Game.Companies.ProcessingCompany>(companyEntity))
			{
				input1 = component4.m_Input1.m_Resource;
				input2 = component4.m_Input2.m_Resource;
				output = component4.m_Output.m_Resource;
				if (EconomyUtils.GetWeight(base.EntityManager, output, m_ResourcePrefabs) > 0f)
				{
					outputUnit = "weightPerMonth";
				}
				base.tooltipKeys.Add("Requires");
				base.tooltipKeys.Add("Produces");
			}
			else if (base.EntityManager.HasComponent<Game.Companies.ExtractorCompany>(companyEntity))
			{
				output = component4.m_Output.m_Resource;
				if (EconomyUtils.GetWeight(base.EntityManager, output, m_ResourcePrefabs) > 0f)
				{
					outputUnit = "weightPerMonth";
				}
				base.tooltipKeys.Add("Produces");
			}
			else if (base.EntityManager.HasComponent<Game.Companies.StorageCompany>(companyEntity))
			{
				stores = base.EntityManager.GetComponentData<StorageCompanyData>(component3.m_Prefab).m_StoredResources;
				base.tooltipKeys.Add("Stores");
			}
			industrialType = GetIndustrialType();
		}
		if (base.EntityManager.TryGetComponent<LodgingProvider>(companyEntity, out var component5) && base.EntityManager.TryGetBuffer(companyEntity, isReadOnly: true, out DynamicBuffer<Renter> buffer2))
		{
			hotelGuests = new int2(buffer2.Length, buffer2.Length + component5.m_FreeRooms);
			price = component5.m_Price;
		}
		isCommercial = base.EntityManager.HasComponent<ServiceAvailable>(companyEntity);
		isStorage = base.EntityManager.HasComponent<Game.Companies.StorageCompany>(companyEntity);
		QueryCompanyData(companyEntity);
	}

	private IndustrialType GetIndustrialType()
	{
		if (!base.EntityManager.TryGetComponent<PropertyRenter>(companyEntity, out var component) || !base.EntityManager.TryGetComponent<PrefabRef>(component.m_Property, out var component2) || !base.EntityManager.TryGetComponent<SpawnableBuildingData>(component2.m_Prefab, out var component3) || !base.EntityManager.TryGetComponent<ZoneData>(component3.m_ZonePrefab, out var component4) || component4.m_AreaType != AreaType.Industrial || component4.m_ZoneFlags == ZoneFlags.Office)
		{
			return IndustrialType.None;
		}
		if (base.EntityManager.HasComponent<Game.Companies.ExtractorCompany>(companyEntity))
		{
			return IndustrialType.Extractor;
		}
		if (base.EntityManager.HasComponent<Game.Companies.ProcessingCompany>(companyEntity))
		{
			return IndustrialType.Processing;
		}
		if (base.EntityManager.HasComponent<Game.Companies.StorageCompany>(companyEntity))
		{
			return IndustrialType.Storage;
		}
		return IndustrialType.None;
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("companyName");
		if (companyEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			m_NameSystem.BindName(writer, companyEntity);
		}
		writer.PropertyName("isRentable");
		writer.Write(isRentable);
		writer.PropertyName("input1");
		if (input1 == Resource.NoResource)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(Enum.GetName(typeof(Resource), input1));
		}
		writer.PropertyName("input2");
		if (input2 == Resource.NoResource)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(Enum.GetName(typeof(Resource), input2));
		}
		writer.PropertyName("output");
		if (output == Resource.NoResource)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(Enum.GetName(typeof(Resource), output));
		}
		writer.PropertyName("outputUnit");
		if (output == Resource.NoResource)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(outputUnit);
		}
		writer.PropertyName("sells");
		if (sells == Resource.NoResource)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(Enum.GetName(typeof(Resource), sells));
		}
		writer.PropertyName("stores");
		if (stores == Resource.NoResource)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(Enum.GetName(typeof(Resource), stores));
		}
		writer.PropertyName("hotelGuests");
		if (hotelGuests.y == 0)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(hotelGuests);
		}
		writer.PropertyName("isCommercial");
		writer.Write(isCommercial);
		writer.PropertyName("isStorage");
		writer.Write(isStorage);
		writer.PropertyName("industrialType");
		if (industrialType == IndustrialType.None)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(Enum.GetName(typeof(IndustrialType), industrialType));
		}
		for (int i = 0; i < m_Results.Length; i++)
		{
			writer.PropertyName(Enum.GetName(typeof(ResultType), i));
			writer.Write(m_Results[i]);
		}
	}

	private void QueryCompanyData(Entity entity)
	{
		m_Results.Fill(0);
		if (entity == Entity.Null || !base.EntityManager.HasComponent<CompanyData>(entity) || !base.EntityManager.TryGetComponent<CompanyStatisticData>(entity, out var component))
		{
			return;
		}
		int value = 0;
		if (base.EntityManager.TryGetComponent<PropertyRenter>(entity, out var component2) && base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Employee> buffer) && base.EntityManager.TryGetComponent<PrefabRef>(entity, out var component3) && base.EntityManager.TryGetComponent<IndustrialProcessData>(component3.m_Prefab, out var component4))
		{
			DynamicBuffer<Efficiency> buffer2;
			float buildingEfficiency = (base.EntityManager.TryGetBuffer(component2.m_Property, isReadOnly: true, out buffer2) ? BuildingUtils.GetEfficiency(buffer2) : 1f);
			if (base.EntityManager.HasComponent<Game.Companies.ExtractorCompany>(entity))
			{
				value = component.m_LastUpdateProduce;
			}
			else
			{
				ComponentLookup<Citizen> citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef);
				ServiceAvailable serviceAvailable = (base.EntityManager.HasComponent<ServiceAvailable>(entity) ? base.EntityManager.GetComponentData<ServiceAvailable>(entity) : default(ServiceAvailable));
				ServiceCompanyData serviceCompanyData = (base.EntityManager.HasComponent<ServiceCompanyData>(component3.m_Prefab) ? base.EntityManager.GetComponentData<ServiceCompanyData>(component3.m_Prefab) : default(ServiceCompanyData));
				ResourcePrefabs prefabs = m_ResourceSystem.GetPrefabs();
				EconomyParameterData economyParameters = __query_2105106401_0.GetSingleton<EconomyParameterData>();
				ComponentLookup<ResourceData> resourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef);
				bool isIndustrial = !base.EntityManager.HasComponent<ServiceAvailable>(entity);
				value = EconomyUtils.GetCompanyProductionPerDay(buildingEfficiency, isIndustrial, buffer, component4, prefabs, ref resourceDatas, ref citizens, ref economyParameters, serviceAvailable, serviceCompanyData);
			}
		}
		m_Results[11] = math.max(component.m_CurrentNumberOfCustomers, component.m_MonthlyCustomerCount);
		m_Results[4] = component.m_RentPaid;
		m_Results[3] = component.m_WagePaid;
		m_Results[5] = component.m_ElectricityPaid;
		m_Results[6] = component.m_WaterPaid;
		m_Results[7] = component.m_SewagePaid;
		m_Results[8] = component.m_GarbagePaid;
		m_Results[9] = component.m_TaxPaid;
		m_Results[10] = component.m_CostBuyResource;
		m_Results[0] = component.m_Income;
		m_Results[1] = component.m_Worth;
		m_Results[2] = component.m_Profit;
		m_Results[12] = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<EconomyParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_2105106401_0 = entityQueryBuilder2.Build(ref state);
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
	public CompanySection()
	{
	}
}
