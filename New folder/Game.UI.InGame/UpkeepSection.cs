using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class UpkeepSection : InfoSectionBase
{
	private readonly struct UIUpkeepItem : IJsonWritable, IComparable<UIUpkeepItem>
	{
		public int count { get; }

		public int amount { get; }

		public int price { get; }

		public Resource localeKey { get; }

		public string titleId { get; }

		public UIUpkeepItem(int amount, int price, Resource localeKey, string titleId)
		{
			count = 1;
			this.amount = amount;
			this.price = price;
			this.localeKey = localeKey;
			this.titleId = titleId;
		}

		private UIUpkeepItem(int count, int amount, int price, Resource localeKey, string titleId)
		{
			this.count = count;
			this.amount = amount;
			this.price = price;
			this.localeKey = localeKey;
			this.titleId = titleId;
		}

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(typeof(UIUpkeepItem).FullName);
			writer.PropertyName("count");
			writer.Write(count);
			writer.PropertyName("amount");
			writer.Write(amount);
			writer.PropertyName("price");
			writer.Write(price);
			writer.PropertyName("localeKey");
			writer.Write(Enum.GetName(typeof(Resource), localeKey));
			writer.PropertyName("titleId");
			writer.Write(titleId);
			writer.PropertyName("localeKey");
			writer.Write(Enum.GetName(typeof(Resource), localeKey));
			writer.TypeEnd();
		}

		public int CompareTo(UIUpkeepItem other)
		{
			return amount.CompareTo(other.amount);
		}

		public static UIUpkeepItem operator +(UIUpkeepItem a, UIUpkeepItem b)
		{
			return new UIUpkeepItem(a.count + 1, a.amount + b.amount, a.price + b.price, b.localeKey, b.titleId);
		}
	}

	private struct TypeHandle
	{
		public BufferLookup<Employee> __Game_Companies_Employee_RW_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<UpkeepModifierData> __Game_Prefabs_UpkeepModifierData_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Companies_Employee_RW_BufferLookup = state.GetBufferLookup<Employee>();
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_UpkeepModifierData_RO_BufferLookup = state.GetBufferLookup<UpkeepModifierData>(isReadOnly: true);
		}
	}

	private ResourceSystem m_ResourceSystem;

	private PrefabUISystem m_PrefabUISystem;

	private CitySystem m_CitySystem;

	private EntityQuery m_BudgetDataQuery;

	private EntityQuery m_EconomyParameterQuery;

	private TypeHandle __TypeHandle;

	protected override string group => "UpkeepSection";

	private Dictionary<string, UIUpkeepItem> moneyUpkeep { get; set; }

	private Dictionary<Resource, UIUpkeepItem> resourceUpkeep { get; set; }

	private List<UIUpkeepItem> upkeeps { get; set; }

	private int total { get; set; }

	private bool inactive { get; set; }

	protected override bool displayForUpgrades => true;

	protected override void Reset()
	{
		moneyUpkeep.Clear();
		resourceUpkeep.Clear();
		upkeeps.Clear();
		total = 0;
		inactive = false;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_PrefabUISystem = base.World.GetOrCreateSystemManaged<PrefabUISystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_BudgetDataQuery = GetEntityQuery(ComponentType.ReadOnly<ServiceBudgetData>());
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		resourceUpkeep = new Dictionary<Resource, UIUpkeepItem>(5);
		moneyUpkeep = new Dictionary<string, UIUpkeepItem>(5);
		upkeeps = new List<UIUpkeepItem>(10);
	}

	private bool Visible()
	{
		if (!base.EntityManager.HasComponent<ServiceUpkeepData>(selectedPrefab))
		{
			if (base.EntityManager.HasComponent<ServiceObjectData>(selectedPrefab))
			{
				return base.EntityManager.HasComponent<WorkplaceData>(selectedPrefab);
			}
			return false;
		}
		return true;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	private void CalculateServiceUpkeepDatas(Entity entity, Entity prefabEntity, Entity buildingOwnerEntity, DynamicBuffer<ServiceUpkeepData> serviceUpkeepDatas, bool inactiveBuilding, bool inactiveUpgrade, DynamicBuffer<CityModifier> cityModifiers, NativeList<UpkeepModifierData> upkeepModifiers)
	{
		string prefabName = m_PrefabSystem.GetPrefabName(prefabEntity);
		for (int i = 0; i < serviceUpkeepDatas.Length; i++)
		{
			ServiceUpkeepData serviceUpkeepData = serviceUpkeepDatas[i];
			Resource resource = serviceUpkeepData.m_Upkeep.m_Resource;
			int amount = serviceUpkeepData.m_Upkeep.m_Amount;
			m_PrefabUISystem.GetTitleAndDescription(prefabEntity, out var titleId, out var descriptionId);
			if (!m_BudgetDataQuery.IsEmptyIgnoreFilter && resource == Resource.Money)
			{
				float value = CityServiceUpkeepSystem.CalculateUpkeep(amount, selectedPrefab, m_BudgetDataQuery.GetSingletonEntity(), base.EntityManager);
				CityUtils.ApplyModifier(ref value, cityModifiers, CityModifierType.CityServiceBuildingBaseUpkeepCost);
				if (inactiveBuilding || inactiveUpgrade)
				{
					value *= 0.1f;
				}
				int price = Mathf.RoundToInt(value);
				if (!moneyUpkeep.TryGetValue(prefabName, out var value2))
				{
					moneyUpkeep[prefabName] = value2;
				}
				Dictionary<string, UIUpkeepItem> dictionary = moneyUpkeep;
				descriptionId = prefabName;
				dictionary[descriptionId] += new UIUpkeepItem(amount, price, Resource.Money, titleId);
			}
			else
			{
				if (inactiveUpgrade)
				{
					continue;
				}
				amount = Mathf.RoundToInt(CityServiceUpkeepSystem.GetUpkeepModifier(resource, upkeepModifiers).Transform(amount));
				int num = Mathf.RoundToInt((float)amount * EconomyUtils.GetMarketPrice(resource, m_ResourceSystem.GetPrefabs(), base.EntityManager));
				if (serviceUpkeepData.m_ScaleWithUsage && base.EntityManager.TryGetComponent<ServiceUsage>(buildingOwnerEntity, out var component))
				{
					amount = (int)((float)amount * component.m_Usage);
					num = (int)((float)num * component.m_Usage);
				}
				if (amount != 0 && num != 0)
				{
					if (!resourceUpkeep.TryGetValue(resource, out var value3))
					{
						resourceUpkeep[resource] = value3;
					}
					resourceUpkeep[resource] += new UIUpkeepItem(amount, num, resource, string.Empty);
					if (!base.tooltipKeys.Contains(resource.ToString()))
					{
						base.tooltipKeys.Add(resource.ToString());
					}
				}
			}
		}
		if (!m_EconomyParameterQuery.IsEmptyIgnoreFilter && entity == buildingOwnerEntity && base.EntityManager.HasComponent<WorkplaceData>(selectedPrefab))
		{
			int upkeepOfEmployeeWage = CityServiceUpkeepSystem.GetUpkeepOfEmployeeWage(InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_Employee_RW_BufferLookup, ref base.CheckedStateRef), buildingOwnerEntity, m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(), inactiveBuilding);
			if (!moneyUpkeep.TryGetValue(Resource.Money.ToString(), out var value4))
			{
				moneyUpkeep[Resource.Money.ToString()] = value4;
			}
			moneyUpkeep[Resource.Money.ToString()] += new UIUpkeepItem(upkeepOfEmployeeWage, upkeepOfEmployeeWage, Resource.Money, string.Empty);
		}
	}

	protected override void OnProcess()
	{
		BufferLookup<InstalledUpgrade> bufferLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef);
		ComponentLookup<PrefabRef> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef);
		BufferLookup<UpkeepModifierData> bufferLookup2 = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_UpkeepModifierData_RO_BufferLookup, ref base.CheckedStateRef);
		inactive = (base.EntityManager.TryGetComponent<Building>(selectedEntity, out var component) && BuildingUtils.CheckOption(component, BuildingOption.Inactive)) || (base.EntityManager.TryGetComponent<Extension>(selectedEntity, out var component2) && (component2.m_Flags & ExtensionFlags.Disabled) != 0);
		if (base.EntityManager.TryGetBuffer(selectedPrefab, isReadOnly: true, out DynamicBuffer<ServiceUpkeepData> buffer))
		{
			Entity owner = selectedEntity;
			bool inactiveBuilding = inactive;
			bool inactiveUpgrade = false;
			if (base.EntityManager.HasComponent<Game.Buildings.ServiceUpgrade>(selectedEntity) && base.EntityManager.TryGetComponent<Owner>(selectedEntity, out var component3) && base.EntityManager.TryGetComponent<Building>(component3.m_Owner, out component))
			{
				owner = component3.m_Owner;
				inactiveUpgrade = inactive;
				inactiveBuilding = BuildingUtils.CheckOption(component, BuildingOption.Inactive);
			}
			DynamicBuffer<CityModifier> buffer2 = base.EntityManager.GetBuffer<CityModifier>(m_CitySystem.City, isReadOnly: true);
			NativeList<UpkeepModifierData> nativeList = new NativeList<UpkeepModifierData>(4, Allocator.Temp);
			CityServiceUpkeepSystem.GetUpkeepModifierData(nativeList, bufferLookup, componentLookup, bufferLookup2, selectedEntity);
			CalculateServiceUpkeepDatas(owner, selectedPrefab, owner, buffer, inactiveBuilding, inactiveUpgrade, buffer2, nativeList);
			if (bufferLookup.TryGetBuffer(selectedEntity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					InstalledUpgrade installedUpgrade = bufferData[i];
					if (componentLookup.TryGetComponent(installedUpgrade.m_Upgrade, out var componentData) && base.EntityManager.TryGetBuffer(componentData.m_Prefab, isReadOnly: true, out DynamicBuffer<ServiceUpkeepData> buffer3))
					{
						CalculateServiceUpkeepDatas(installedUpgrade.m_Upgrade, componentData.m_Prefab, owner, buffer3, inactiveBuilding, BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive), buffer2, nativeList);
					}
				}
			}
			nativeList.Dispose();
		}
		foreach (KeyValuePair<string, UIUpkeepItem> item in moneyUpkeep)
		{
			upkeeps.Add(item.Value);
			total += item.Value.price;
		}
		foreach (KeyValuePair<Resource, UIUpkeepItem> item2 in resourceUpkeep)
		{
			upkeeps.Add(item2.Value);
			total += item2.Value.price;
		}
		upkeeps.Sort();
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("upkeeps");
		writer.ArrayBegin(upkeeps.Count);
		for (int i = 0; i < upkeeps.Count; i++)
		{
			writer.Write(upkeeps[i]);
		}
		writer.ArrayEnd();
		writer.PropertyName("total");
		writer.Write(total);
		writer.PropertyName("inactive");
		writer.Write(inactive);
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
	public UpkeepSection()
	{
	}
}
