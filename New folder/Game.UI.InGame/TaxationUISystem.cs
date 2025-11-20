using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.City;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class TaxationUISystem : UISystemBase
{
	private struct TypeHandle
	{
		[ReadOnly]
		public BufferLookup<CityStatistic> __Game_City_CityStatistic_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_City_CityStatistic_RO_BufferLookup = state.GetBufferLookup<CityStatistic>(isReadOnly: true);
		}
	}

	private static readonly string kGroup = "taxation";

	private ITaxSystem m_TaxSystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private PrefabSystem m_PrefabSystem;

	private EntityQuery m_ResourceQuery;

	private EntityQuery m_UnlockedZoneQuery;

	private GetterValueBinding<int> m_TaxRate;

	private GetterValueBinding<int> m_TaxIncome;

	private GetterValueBinding<int> m_TaxEffect;

	private GetterValueBinding<int> m_MinTaxRate;

	private GetterValueBinding<int> m_MaxTaxRate;

	private RawValueBinding m_AreaTypes;

	private GetterMapBinding<int, int> m_AreaTaxRates;

	private GetterMapBinding<int, Bounds1> m_AreaResourceTaxRanges;

	private GetterMapBinding<int, int> m_AreaTaxIncomes;

	private GetterMapBinding<int, int> m_AreaTaxEffects;

	private GetterMapBinding<TaxResource, int> m_ResourceTaxRates;

	private GetterMapBinding<TaxResource, int> m_ResourceTaxIncomes;

	private TaxParameterData m_CachedTaxParameterData;

	private int m_CachedLockedOrderVersion = -1;

	private Dictionary<int, string> m_ResourceIcons = new Dictionary<int, string>();

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ResourceQuery = GetEntityQuery(ComponentType.ReadOnly<ResourceData>(), ComponentType.ReadOnly<TaxableResourceData>());
		m_UnlockedZoneQuery = GetEntityQuery(ComponentType.ReadOnly<ZoneData>(), ComponentType.Exclude<Locked>());
		m_TaxSystem = base.World.GetOrCreateSystemManaged<TaxSystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		AddBinding(m_TaxRate = new GetterValueBinding<int>(kGroup, "taxRate", UpdateTaxRate));
		AddBinding(m_TaxIncome = new GetterValueBinding<int>(kGroup, "taxIncome", UpdateTaxIncome));
		AddBinding(m_TaxEffect = new GetterValueBinding<int>(kGroup, "taxEffect", UpdateTaxEffect));
		AddBinding(m_MinTaxRate = new GetterValueBinding<int>(kGroup, "minTaxRate", UpdateMinTaxRate));
		AddBinding(m_MaxTaxRate = new GetterValueBinding<int>(kGroup, "maxTaxRate", UpdateMaxTaxRate));
		AddBinding(m_AreaTypes = new RawValueBinding(kGroup, "areaTypes", UpdateAreaTypes));
		AddBinding(m_AreaTaxRates = new GetterMapBinding<int, int>(kGroup, "areaTaxRates", UpdateAreaTaxRate));
		AddBinding(m_AreaResourceTaxRanges = new GetterMapBinding<int, Bounds1>(kGroup, "areaResourceTaxRanges", UpdateAreaResourceTaxRange));
		AddBinding(m_AreaTaxIncomes = new GetterMapBinding<int, int>(kGroup, "areaTaxIncomes", UpdateAreaTaxIncome));
		AddBinding(m_AreaTaxEffects = new GetterMapBinding<int, int>(kGroup, "areaTaxEffects", UpdateAreaTaxEffect));
		AddBinding(new RawMapBinding<int>(kGroup, "areaResources", UpdateAreaResources));
		AddBinding(m_ResourceTaxRates = new GetterMapBinding<TaxResource, int>(kGroup, "resourceTaxRates", UpdateResourceTaxRate, new ValueReader<TaxResource>(), new ValueWriter<TaxResource>()));
		AddBinding(m_ResourceTaxIncomes = new GetterMapBinding<TaxResource, int>(kGroup, "resourceTaxIncomes", UpdateResourceTaxIncome, new ValueReader<TaxResource>(), new ValueWriter<TaxResource>()));
		AddBinding(new GetterMapBinding<TaxResource, TaxResourceInfo>(kGroup, "taxResourceInfos", UpdateResourceInfo, new ValueReader<TaxResource>(), new ValueWriter<TaxResource>(), new ValueWriter<TaxResourceInfo>()));
		AddBinding(new TriggerBinding<int>(kGroup, "setTaxRate", SetTaxRate));
		AddBinding(new TriggerBinding<int, int>(kGroup, "setAreaTaxRate", SetAreaTaxRate));
		AddBinding(new TriggerBinding<int, int, int>(kGroup, "setResourceTaxRate", SetResourceTaxRate));
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_AreaTaxIncomes.UpdateAll();
		m_AreaTaxEffects.UpdateAll();
		m_TaxIncome.Update();
		m_TaxEffect.Update();
		m_ResourceTaxIncomes.UpdateAll();
		TaxParameterData taxParameterData = m_TaxSystem.GetTaxParameterData();
		int componentOrderVersion = base.EntityManager.GetComponentOrderVersion<Locked>();
		bool flag = componentOrderVersion != m_CachedLockedOrderVersion;
		m_CachedLockedOrderVersion = componentOrderVersion;
		if (!m_CachedTaxParameterData.Equals(taxParameterData))
		{
			m_AreaTypes.Update();
			m_MinTaxRate.Update();
			m_MaxTaxRate.Update();
		}
		else if (flag)
		{
			m_AreaTypes.Update();
		}
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		m_ResourceIcons.Clear();
		NativeArray<Entity> nativeArray = m_ResourceQuery.ToEntityArray(Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			ResourcePrefab prefab = m_PrefabSystem.GetPrefab<ResourcePrefab>(nativeArray[i]);
			UIObject component = prefab.GetComponent<UIObject>();
			m_ResourceIcons[(int)(prefab.m_Resource - 1)] = (component ? component.m_Icon : string.Empty);
		}
		nativeArray.Dispose();
	}

	private int2 GetLimits(TaxAreaType type, TaxParameterData limits)
	{
		return type switch
		{
			TaxAreaType.Residential => limits.m_ResidentialTaxLimits, 
			TaxAreaType.Commercial => limits.m_CommercialTaxLimits, 
			TaxAreaType.Industrial => limits.m_IndustrialTaxLimits, 
			TaxAreaType.Office => limits.m_OfficeTaxLimits, 
			_ => default(int2), 
		};
	}

	private int2 GetResourceLimits(TaxAreaType type, TaxParameterData limits)
	{
		if (type == TaxAreaType.Residential)
		{
			return limits.m_JobLevelTaxLimits;
		}
		return limits.m_ResourceTaxLimits;
	}

	private int GetResourceTaxRate(TaxAreaType type, int resource)
	{
		return type switch
		{
			TaxAreaType.Residential => m_TaxSystem.GetResidentialTaxRate(resource), 
			TaxAreaType.Commercial => m_TaxSystem.GetCommercialTaxRate(EconomyUtils.GetResource(resource)), 
			TaxAreaType.Industrial => m_TaxSystem.GetIndustrialTaxRate(EconomyUtils.GetResource(resource)), 
			TaxAreaType.Office => m_TaxSystem.GetOfficeTaxRate(EconomyUtils.GetResource(resource)), 
			_ => 0, 
		};
	}

	private int GetEstimatedResourceTaxIncome(TaxAreaType type, int resource)
	{
		NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> lookup = m_CityStatisticsSystem.GetLookup();
		BufferLookup<CityStatistic> bufferLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityStatistic_RO_BufferLookup, ref base.CheckedStateRef);
		return type switch
		{
			TaxAreaType.Residential => m_TaxSystem.GetEstimatedResidentialTaxIncome(resource, lookup, bufferLookup), 
			TaxAreaType.Commercial => m_TaxSystem.GetEstimatedCommercialTaxIncome(EconomyUtils.GetResource(resource), lookup, bufferLookup), 
			TaxAreaType.Industrial => m_TaxSystem.GetEstimatedIndustrialTaxIncome(EconomyUtils.GetResource(resource), lookup, bufferLookup), 
			TaxAreaType.Office => m_TaxSystem.GetEstimatedOfficeTaxIncome(EconomyUtils.GetResource(resource), lookup, bufferLookup), 
			_ => 0, 
		};
	}

	private int UpdateTaxRate()
	{
		return m_TaxSystem.TaxRate;
	}

	private int UpdateTaxIncome()
	{
		NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> lookup = m_CityStatisticsSystem.GetLookup();
		BufferLookup<CityStatistic> bufferLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityStatistic_RO_BufferLookup, ref base.CheckedStateRef);
		return m_TaxSystem.GetEstimatedTaxAmount(TaxAreaType.Residential, TaxResultType.Any, lookup, bufferLookup) + m_TaxSystem.GetEstimatedTaxAmount(TaxAreaType.Commercial, TaxResultType.Any, lookup, bufferLookup) + m_TaxSystem.GetEstimatedTaxAmount(TaxAreaType.Industrial, TaxResultType.Any, lookup, bufferLookup) + m_TaxSystem.GetEstimatedTaxAmount(TaxAreaType.Office, TaxResultType.Any, lookup, bufferLookup);
	}

	private int UpdateTaxEffect()
	{
		return m_TaxSystem.GetTaxRateEffect(TaxAreaType.Residential, m_TaxSystem.GetTaxRate(TaxAreaType.Residential)) + m_TaxSystem.GetTaxRateEffect(TaxAreaType.Commercial, m_TaxSystem.GetTaxRate(TaxAreaType.Commercial)) + m_TaxSystem.GetTaxRateEffect(TaxAreaType.Industrial, m_TaxSystem.GetTaxRate(TaxAreaType.Industrial)) + m_TaxSystem.GetTaxRateEffect(TaxAreaType.Office, m_TaxSystem.GetTaxRate(TaxAreaType.Office));
	}

	private int UpdateMinTaxRate()
	{
		return m_TaxSystem.GetTaxParameterData().m_TotalTaxLimits.x;
	}

	private int UpdateMaxTaxRate()
	{
		return m_TaxSystem.GetTaxParameterData().m_TotalTaxLimits.y;
	}

	private void SetTaxRate(int rate)
	{
		m_TaxSystem.Readers.Complete();
		m_TaxSystem.TaxRate = rate;
		m_TaxRate.Update();
		m_AreaTaxRates.UpdateAll();
		m_ResourceTaxRates.UpdateAll();
		m_AreaResourceTaxRanges.UpdateAll();
	}

	private void SetAreaTaxRate(int areaType, int rate)
	{
		m_TaxSystem.Readers.Complete();
		m_TaxSystem.SetTaxRate((TaxAreaType)areaType, rate);
		m_AreaTaxRates.Update(areaType);
		m_ResourceTaxRates.UpdateAll();
		m_AreaResourceTaxRanges.UpdateAll();
	}

	private void SetResourceTaxRate(int resource, int areaType, int rate)
	{
		m_TaxSystem.Readers.Complete();
		if ((byte)areaType == 1)
		{
			m_TaxSystem.SetResidentialTaxRate(resource, rate);
		}
		if ((byte)areaType == 2)
		{
			m_TaxSystem.SetCommercialTaxRate(EconomyUtils.GetResource(resource), rate);
		}
		else if ((byte)areaType == 3)
		{
			m_TaxSystem.SetIndustrialTaxRate(EconomyUtils.GetResource(resource), rate);
		}
		else if ((byte)areaType == 4)
		{
			m_TaxSystem.SetOfficeTaxRate(EconomyUtils.GetResource(resource), rate);
		}
		m_AreaTaxRates.Update(areaType);
		m_ResourceTaxRates.Update(new TaxResource
		{
			m_AreaType = areaType,
			m_Resource = resource
		});
		m_AreaResourceTaxRanges.UpdateAll();
	}

	private int UpdateAreaTaxRate(int areaType)
	{
		return m_TaxSystem.GetTaxRate((TaxAreaType)areaType);
	}

	private Bounds1 UpdateAreaResourceTaxRange(int area)
	{
		return new Bounds1(m_TaxSystem.GetTaxRateRange((TaxAreaType)area));
	}

	private int UpdateAreaTaxIncome(int areaType)
	{
		NativeParallelHashMap<CityStatisticsSystem.StatisticsKey, Entity> lookup = m_CityStatisticsSystem.GetLookup();
		BufferLookup<CityStatistic> bufferLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityStatistic_RO_BufferLookup, ref base.CheckedStateRef);
		return m_TaxSystem.GetEstimatedTaxAmount((TaxAreaType)areaType, TaxResultType.Any, lookup, bufferLookup);
	}

	private int UpdateAreaTaxEffect(int areaType)
	{
		return m_TaxSystem.GetTaxRateEffect((TaxAreaType)areaType, m_TaxSystem.GetTaxRate((TaxAreaType)areaType));
	}

	private int UpdateResourceTaxRate(TaxResource taxResource)
	{
		return GetResourceTaxRate((TaxAreaType)taxResource.m_AreaType, taxResource.m_Resource);
	}

	private int UpdateResourceTaxIncome(TaxResource taxResource)
	{
		return GetEstimatedResourceTaxIncome((TaxAreaType)taxResource.m_AreaType, taxResource.m_Resource);
	}

	private void UpdateAreaTypes(IJsonWriter binder)
	{
		TaxParameterData limits = (m_CachedTaxParameterData = m_TaxSystem.GetTaxParameterData());
		binder.ArrayBegin(4u);
		TaxAreaType taxAreaType = TaxAreaType.Residential;
		while ((int)taxAreaType <= 4)
		{
			binder.TypeBegin("taxation.TaxAreaType");
			binder.PropertyName("index");
			binder.Write((int)taxAreaType);
			binder.PropertyName("id");
			binder.Write(taxAreaType.ToString());
			binder.PropertyName("icon");
			binder.Write(GetIcon(taxAreaType));
			int2 limits2 = GetLimits(taxAreaType, limits);
			binder.PropertyName("taxRateMin");
			binder.Write(limits2.x);
			binder.PropertyName("taxRateMax");
			binder.Write(limits2.y);
			int2 resourceLimits = GetResourceLimits(taxAreaType, limits);
			binder.PropertyName("resourceTaxRateMin");
			binder.Write(resourceLimits.x);
			binder.PropertyName("resourceTaxRateMax");
			binder.Write(resourceLimits.y);
			binder.PropertyName("locked");
			binder.Write(Locked(taxAreaType));
			binder.TypeEnd();
			taxAreaType++;
		}
		binder.ArrayEnd();
	}

	private bool Locked(TaxAreaType areaType)
	{
		NativeArray<ZoneData> nativeArray = m_UnlockedZoneQuery.ToComponentDataArray<ZoneData>(Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			if ((areaType == TaxAreaType.Residential && nativeArray[i].m_AreaType == AreaType.Residential) || (areaType == TaxAreaType.Commercial && nativeArray[i].m_AreaType == AreaType.Commercial) || (areaType == TaxAreaType.Industrial && nativeArray[i].m_AreaType == AreaType.Industrial) || (areaType == TaxAreaType.Office && (nativeArray[i].m_ZoneFlags & ZoneFlags.Office) != 0))
			{
				nativeArray.Dispose();
				return false;
			}
		}
		nativeArray.Dispose();
		return true;
	}

	private void UpdateAreaResources(IJsonWriter binder, int area)
	{
		if ((byte)area == 1)
		{
			binder.ArrayBegin(5u);
			for (int i = 0; i < 5; i++)
			{
				binder.Write(new TaxResource
				{
					m_Resource = i,
					m_AreaType = 1
				});
			}
			binder.ArrayEnd();
			return;
		}
		int num = 0;
		foreach (ResourcePrefab resource in GetResources(area))
		{
			_ = resource;
			num++;
		}
		binder.ArrayBegin(num);
		foreach (ResourcePrefab resource2 in GetResources(area))
		{
			binder.Write(new TaxResource
			{
				m_Resource = (int)(resource2.m_Resource - 1),
				m_AreaType = area
			});
		}
		binder.ArrayEnd();
	}

	private TaxResourceInfo UpdateResourceInfo(TaxResource resource)
	{
		if (resource.m_AreaType == 1)
		{
			return new TaxResourceInfo
			{
				m_ID = string.Empty,
				m_Icon = "Media/Game/Icons/ZoneResidential.svg"
			};
		}
		return new TaxResourceInfo
		{
			m_ID = EconomyUtils.GetResource(resource.m_Resource).ToString(),
			m_Icon = m_ResourceIcons[resource.m_Resource]
		};
	}

	private IEnumerable<ResourcePrefab> GetResources(int areaType)
	{
		NativeArray<Entity> entities = m_ResourceQuery.ToEntityArray(Allocator.TempJob);
		int i = 0;
		while (i < entities.Length)
		{
			ResourcePrefab prefab = m_PrefabSystem.GetPrefab<ResourcePrefab>(entities[i]);
			TaxableResource component = prefab.GetComponent<TaxableResource>();
			if (MatchArea(component, areaType))
			{
				yield return prefab;
			}
			int num = i + 1;
			i = num;
		}
		entities.Dispose();
	}

	private bool MatchArea(TaxableResource data, int areaType)
	{
		if (data.m_TaxAreas == null || data.m_TaxAreas.Length == 0)
		{
			return true;
		}
		for (int i = 0; i < data.m_TaxAreas.Length; i++)
		{
			if ((int)data.m_TaxAreas[i] == areaType)
			{
				return true;
			}
		}
		return false;
	}

	private string GetIcon(TaxAreaType type)
	{
		return "Media/Game/Icons/Zone" + type.ToString() + ".svg";
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
	public TaxationUISystem()
	{
	}
}
