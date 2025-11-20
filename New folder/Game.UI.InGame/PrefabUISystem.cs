using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Colossal.Annotations;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Agents;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.Tutorials;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class PrefabUISystem : UISystemBase
{
	public interface IPrefabEffectBinder
	{
		bool Matches(EntityManager entityManager, Entity entity);

		void Bind(IJsonWriter binder, EntityManager entityManager, Entity entity);
	}

	public class CityModifierBinder : IPrefabEffectBinder
	{
		public bool Matches(EntityManager entityManager, Entity entity)
		{
			return entityManager.HasComponent<CityModifierData>(entity);
		}

		public void Bind(IJsonWriter binder, EntityManager entityManager, Entity entity)
		{
			DynamicBuffer<CityModifierData> buffer = entityManager.GetBuffer<CityModifierData>(entity, isReadOnly: true);
			Bind(binder, buffer);
		}

		public static void Bind<T>(IJsonWriter binder, T cityModifiers) where T : INativeList<CityModifierData>
		{
			int num = 0;
			for (int i = 0; i < cityModifiers.Length; i++)
			{
				if (IsValidModifierType(cityModifiers[i].m_Type))
				{
					num++;
				}
			}
			binder.TypeBegin("prefabs.CityModifierEffect");
			binder.PropertyName("modifiers");
			binder.ArrayBegin(num);
			for (int j = 0; j < cityModifiers.Length; j++)
			{
				CityModifierData data = cityModifiers[j];
				if (IsValidModifierType(data.m_Type))
				{
					binder.TypeBegin("prefabs.CityModifier");
					binder.PropertyName("type");
					binder.Write(Enum.GetName(typeof(CityModifierType), data.m_Type));
					binder.PropertyName("delta");
					binder.Write(ModifierUIUtils.GetModifierDelta(data.m_Mode, data.m_Range.max));
					binder.PropertyName("unit");
					binder.Write(GetModifierUnit(data));
					binder.TypeEnd();
				}
			}
			binder.ArrayEnd();
			binder.TypeEnd();
		}

		public static string GetModifierUnit(CityModifierData data)
		{
			if (data.m_Mode == ModifierValueMode.Absolute)
			{
				switch (data.m_Type)
				{
				case CityModifierType.DiseaseProbability:
				case CityModifierType.OfficeSoftwareEfficiency:
				case CityModifierType.IndustrialElectronicsEfficiency:
				case CityModifierType.CollegeGraduation:
				case CityModifierType.UniversityGraduation:
				case CityModifierType.IndustrialEfficiency:
				case CityModifierType.OfficeEfficiency:
				case CityModifierType.HospitalEfficiency:
				case CityModifierType.IndustrialFishInputEfficiency:
				case CityModifierType.IndustrialFishHubEfficiency:
					return "percentage";
				default:
					return ModifierUIUtils.GetModifierUnit(data.m_Mode);
				}
			}
			return ModifierUIUtils.GetModifierUnit(data.m_Mode);
		}

		private static bool IsValidModifierType(CityModifierType type)
		{
			return type != CityModifierType.CriminalMonitorProbability;
		}
	}

	public class LocalModifierBinder : IPrefabEffectBinder
	{
		public bool Matches(EntityManager entityManager, Entity entity)
		{
			return entityManager.HasComponent<LocalModifierData>(entity);
		}

		public void Bind(IJsonWriter binder, EntityManager entityManager, Entity entity)
		{
			DynamicBuffer<LocalModifierData> buffer = entityManager.GetBuffer<LocalModifierData>(entity, isReadOnly: true);
			Bind(binder, buffer);
		}

		public static void Bind<T>(IJsonWriter binder, T localModifiers) where T : INativeList<LocalModifierData>
		{
			binder.TypeBegin("prefabs.LocalModifierEffect");
			binder.PropertyName("modifiers");
			binder.ArrayBegin(localModifiers.Length);
			for (int i = 0; i < localModifiers.Length; i++)
			{
				LocalModifierData localModifierData = localModifiers[i];
				binder.TypeBegin("prefabs.LocalModifier");
				binder.PropertyName("type");
				binder.Write(Enum.GetName(typeof(LocalModifierType), localModifierData.m_Type));
				binder.PropertyName("delta");
				binder.Write(ModifierUIUtils.GetModifierDelta(localModifierData.m_Mode, localModifierData.m_Delta.max));
				binder.PropertyName("unit");
				binder.Write(ModifierUIUtils.GetModifierUnit(localModifierData.m_Mode));
				binder.PropertyName("radius");
				binder.Write(localModifierData.m_Radius.max);
				binder.TypeEnd();
			}
			binder.ArrayEnd();
			binder.TypeEnd();
		}
	}

	public class LeisureProviderBinder : IPrefabEffectBinder
	{
		public bool Matches(EntityManager entityManager, Entity entity)
		{
			if (entityManager.TryGetComponent<LeisureProviderData>(entity, out var component))
			{
				return component.m_Efficiency > 0;
			}
			return false;
		}

		public void Bind(IJsonWriter binder, EntityManager entityManager, Entity entity)
		{
			LeisureProviderData value = entityManager.GetComponentData<LeisureProviderData>(entity);
			Bind(binder, new NativeList<LeisureProviderData>(1, Allocator.Temp) { in value });
		}

		public static void Bind<T>(IJsonWriter binder, T providers) where T : INativeList<LeisureProviderData>
		{
			binder.TypeBegin("prefabs.LeisureProviderEffect");
			binder.PropertyName("providers");
			binder.ArrayBegin(providers.Length);
			for (int i = 0; i < providers.Length; i++)
			{
				LeisureProviderData leisureProviderData = providers[i];
				binder.TypeBegin("prefabs.LeisureProvider");
				binder.PropertyName("type");
				binder.Write(Enum.GetName(typeof(LeisureType), leisureProviderData.m_LeisureType));
				binder.PropertyName("efficiency");
				binder.Write(leisureProviderData.m_Efficiency);
				binder.TypeEnd();
			}
			binder.ArrayEnd();
			binder.TypeEnd();
		}
	}

	public interface IPrefabPropertyBinder
	{
		bool Matches(EntityManager entityManager, Entity entity);

		void Bind(IJsonWriter binder, EntityManager entityManager, Entity entity);
	}

	public abstract class IntPropertyBinder : IPrefabPropertyBinder
	{
		public readonly string m_LabelId;

		public readonly string m_Unit;

		public readonly bool m_Signed;

		public readonly string m_Icon;

		public readonly string m_ValueIcon;

		protected IntPropertyBinder(string labelId, string unit, bool signed = false, string icon = null, string valueIcon = null)
		{
			m_LabelId = labelId;
			m_Unit = unit;
			m_Signed = signed;
			m_Icon = icon;
			m_ValueIcon = valueIcon;
		}

		public abstract bool Matches(EntityManager entityManager, Entity entity);

		public abstract int GetValue(EntityManager entityManager, Entity entity);

		public void Bind(IJsonWriter binder, EntityManager entityManager, Entity entity)
		{
			binder.Write(new IntProperty
			{
				labelId = m_LabelId,
				unit = m_Unit,
				value = GetValue(entityManager, entity),
				signed = m_Signed,
				icon = m_Icon,
				valueIcon = m_ValueIcon
			});
		}
	}

	public abstract class IntRangePropertyBinder : IPrefabPropertyBinder
	{
		public readonly string m_LabelId;

		public readonly string m_Unit;

		public readonly bool m_Signed;

		public readonly string m_Icon;

		public readonly string m_ValueIcon;

		protected IntRangePropertyBinder(string labelId, string unit, bool signed = false, string icon = null, string valueIcon = null)
		{
			m_LabelId = labelId;
			m_Unit = unit;
			m_Signed = signed;
			m_Icon = icon;
			m_ValueIcon = valueIcon;
		}

		public abstract bool Matches(EntityManager entityManager, Entity entity);

		public abstract int GetMinValue(EntityManager entityManager, Entity entity);

		public abstract int GetMaxValue(EntityManager entityManager, Entity entity);

		public void Bind(IJsonWriter binder, EntityManager entityManager, Entity entity)
		{
			binder.Write(new IntRangeProperty
			{
				labelId = m_LabelId,
				unit = m_Unit,
				minValue = GetMinValue(entityManager, entity),
				maxValue = GetMaxValue(entityManager, entity),
				signed = m_Signed,
				icon = m_Icon,
				valueIcon = m_ValueIcon
			});
		}
	}

	public abstract class Int2PropertyBinder : IPrefabPropertyBinder
	{
		public readonly string m_LabelId;

		public readonly string m_Unit;

		public readonly bool m_Signed;

		public readonly string m_Icon;

		public readonly string m_ValueIcon;

		protected Int2PropertyBinder(string labelId, string unit, bool signed = false, string icon = null, string valueIcon = null)
		{
			m_LabelId = labelId;
			m_Unit = unit;
			m_Signed = signed;
			m_Icon = icon;
			m_ValueIcon = valueIcon;
		}

		public abstract bool Matches(EntityManager entityManager, Entity entity);

		public abstract int2 GetValue(EntityManager entityManager, Entity entity);

		public void Bind(IJsonWriter binder, EntityManager entityManager, Entity entity)
		{
			binder.Write(new Int2Property
			{
				labelId = m_LabelId,
				unit = m_Unit,
				value = GetValue(entityManager, entity),
				signed = m_Signed,
				icon = m_Icon,
				valueIcon = m_ValueIcon
			});
		}
	}

	public class ComponentIntPropertyBinder<T> : IntPropertyBinder where T : unmanaged, IComponentData
	{
		private readonly Func<T, int> m_Getter;

		private readonly bool m_OmitZero;

		public ComponentIntPropertyBinder(string labelId, string unit, Func<T, int> getter, bool omitZero = true, bool signed = false, string icon = null, string valueIcon = null)
			: base(labelId, unit, signed, icon, valueIcon)
		{
			m_Getter = getter;
			m_OmitZero = omitZero;
		}

		public override bool Matches(EntityManager entityManager, Entity entity)
		{
			if (entityManager.TryGetComponent<T>(entity, out var component))
			{
				if (m_OmitZero)
				{
					return m_Getter(component) != 0;
				}
				return true;
			}
			return false;
		}

		public override int GetValue(EntityManager entityManager, Entity entity)
		{
			return m_Getter(entityManager.GetComponentData<T>(entity));
		}
	}

	public class ComponentIntRangePropertyBinder<T> : IntRangePropertyBinder where T : unmanaged, IComponentData
	{
		private readonly Func<T, int> m_MinGetter;

		private readonly Func<T, int> m_MaxGetter;

		public ComponentIntRangePropertyBinder(string labelId, string unit, Func<T, int> minGetter, Func<T, int> maxGetter, bool signed = false, string icon = null, string valueIcon = null)
			: base(labelId, unit, signed, icon, valueIcon)
		{
			m_MinGetter = minGetter;
			m_MaxGetter = maxGetter;
		}

		public override bool Matches(EntityManager entityManager, Entity entity)
		{
			T component;
			return entityManager.TryGetComponent<T>(entity, out component);
		}

		public override int GetMinValue(EntityManager entityManager, Entity entity)
		{
			return m_MinGetter(entityManager.GetComponentData<T>(entity));
		}

		public override int GetMaxValue(EntityManager entityManager, Entity entity)
		{
			return m_MaxGetter(entityManager.GetComponentData<T>(entity));
		}
	}

	public class ComponentInt2PropertyBinder<T> : Int2PropertyBinder where T : unmanaged, IComponentData
	{
		private readonly Func<T, int2> m_Getter;

		private readonly bool m_OmitZero;

		public ComponentInt2PropertyBinder(string labelId, string unit, Func<T, int2> getter, bool omitZero = true, bool signed = false, string icon = null, string valueIcon = null)
			: base(labelId, unit, signed, icon, valueIcon)
		{
			m_Getter = getter;
			m_OmitZero = omitZero;
		}

		public override bool Matches(EntityManager entityManager, Entity entity)
		{
			if (entityManager.TryGetComponent<T>(entity, out var component))
			{
				if (m_OmitZero)
				{
					return math.all(m_Getter(component) != int2.zero);
				}
				return true;
			}
			return false;
		}

		public override int2 GetValue(EntityManager entityManager, Entity entity)
		{
			return m_Getter(entityManager.GetComponentData<T>(entity));
		}
	}

	public abstract class StringPropertyBinder : IPrefabPropertyBinder
	{
		public readonly string m_LabelId;

		public readonly string m_Icon;

		public readonly string m_ValueIcon;

		protected StringPropertyBinder(string labelId, string icon = null, string valueIcon = null)
		{
			m_LabelId = labelId;
			m_Icon = icon;
			m_ValueIcon = icon;
		}

		public abstract bool Matches(EntityManager entityManager, Entity entity);

		public abstract string GetValueId(EntityManager entityManager, Entity entity);

		public void Bind(IJsonWriter binder, EntityManager entityManager, Entity entity)
		{
			binder.Write(new StringProperty
			{
				labelId = m_LabelId,
				valueId = GetValueId(entityManager, entity),
				icon = m_Icon,
				valueIcon = m_ValueIcon
			});
		}
	}

	public class ConstructionCostBinder : ComponentIntRangePropertyBinder<PlaceableObjectData>
	{
		public ConstructionCostBinder()
			: base("Properties.CONSTRUCTION_COST", "money", (Func<PlaceableObjectData, int>)((PlaceableObjectData data) => (int)data.m_ConstructionCost), (Func<PlaceableObjectData, int>)((PlaceableObjectData data) => (int)data.m_ConstructionCost), signed: false, (string)null, (string)null)
		{
		}

		public override int GetMinValue(EntityManager entityManager, Entity entity)
		{
			if (entityManager.TryGetComponent<TreeData>(entity, out var _))
			{
				PlaceableObjectData componentData = entityManager.GetComponentData<PlaceableObjectData>(entity);
				ObjectToolSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<ObjectToolSystem>();
				EntityQuery entityQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
				EconomyParameterData singleton = entityQuery.GetSingleton<EconomyParameterData>();
				Game.Tools.AgeMask actualAgeMask = orCreateSystemManaged.actualAgeMask;
				int num = int.MaxValue;
				if ((actualAgeMask & Game.Tools.AgeMask.Sapling) != 0)
				{
					num = Math.Min(num, (int)componentData.m_ConstructionCost);
				}
				if ((actualAgeMask & Game.Tools.AgeMask.Young) != 0)
				{
					num = Math.Min(num, (int)(componentData.m_ConstructionCost * singleton.m_TreeCostMultipliers.x));
				}
				if ((actualAgeMask & Game.Tools.AgeMask.Mature) != 0)
				{
					num = Math.Min(num, (int)(componentData.m_ConstructionCost * singleton.m_TreeCostMultipliers.y));
				}
				if ((actualAgeMask & Game.Tools.AgeMask.Elderly) != 0)
				{
					num = Math.Min(num, (int)(componentData.m_ConstructionCost * singleton.m_TreeCostMultipliers.z));
				}
				entityQuery.Dispose();
				return num;
			}
			return base.GetMinValue(entityManager, entity);
		}

		public override int GetMaxValue(EntityManager entityManager, Entity entity)
		{
			if (entityManager.TryGetComponent<TreeData>(entity, out var _))
			{
				PlaceableObjectData componentData = entityManager.GetComponentData<PlaceableObjectData>(entity);
				ObjectToolSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<ObjectToolSystem>();
				EntityQuery entityQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
				EconomyParameterData singleton = entityQuery.GetSingleton<EconomyParameterData>();
				Game.Tools.AgeMask actualAgeMask = orCreateSystemManaged.actualAgeMask;
				int num = 0;
				if ((actualAgeMask & Game.Tools.AgeMask.Sapling) != 0)
				{
					num = Math.Max(num, (int)componentData.m_ConstructionCost);
				}
				if ((actualAgeMask & Game.Tools.AgeMask.Young) != 0)
				{
					num = Math.Max(num, (int)(componentData.m_ConstructionCost * singleton.m_TreeCostMultipliers.x));
				}
				if ((actualAgeMask & Game.Tools.AgeMask.Mature) != 0)
				{
					num = Math.Max(num, (int)(componentData.m_ConstructionCost * singleton.m_TreeCostMultipliers.y));
				}
				if ((actualAgeMask & Game.Tools.AgeMask.Elderly) != 0)
				{
					num = Math.Max(num, (int)(componentData.m_ConstructionCost * singleton.m_TreeCostMultipliers.z));
				}
				entityQuery.Dispose();
				return num;
			}
			return base.GetMaxValue(entityManager, entity);
		}
	}

	public class PlaceableNetCostBinder : ComponentInt2PropertyBinder<PlaceableNetData>
	{
		public PlaceableNetCostBinder()
			: base("Common.ASSET_CONSTRUCTION_COST", "moneyPerDistance", (Func<PlaceableNetData, int2>)((PlaceableNetData data) => new int2(Convert.ToInt32(data.m_DefaultConstructionCost), Convert.ToInt32(data.m_DefaultConstructionCost) * 125)), omitZero: true, signed: false, (string)null, (string)null)
		{
		}

		public override int2 GetValue(EntityManager entityManager, Entity entity)
		{
			int2 result = new int2(0, 0);
			if (entityManager.TryGetComponent<PlaceableNetData>(entity, out var component))
			{
				result += new int2(Convert.ToInt32(component.m_DefaultConstructionCost), Convert.ToInt32(component.m_DefaultConstructionCost) * 125);
			}
			if (entityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<AuxiliaryNet> buffer))
			{
				foreach (AuxiliaryNet item in buffer)
				{
					result += new int2(new float2(GetValue(entityManager, item.m_Prefab)) * ((1000f - item.m_Position.z * 2f) / 1000f));
				}
			}
			return result;
		}
	}

	public class ConsumptionBinder : IPrefabPropertyBinder
	{
		public bool Matches(EntityManager entityManager, Entity entity)
		{
			if (entityManager.TryGetComponent<ConsumptionData>(entity, out var component))
			{
				return component.m_WaterConsumption + component.m_ElectricityConsumption + component.m_GarbageAccumulation > 0f;
			}
			return false;
		}

		public void Bind(IJsonWriter binder, EntityManager entityManager, Entity entity)
		{
			ConsumptionData componentData = entityManager.GetComponentData<ConsumptionData>(entity);
			binder.TypeBegin("prefabs.ConsumptionProperty");
			binder.PropertyName("electricityConsumption");
			binder.Write(Mathf.CeilToInt(componentData.m_ElectricityConsumption));
			binder.PropertyName("waterConsumption");
			binder.Write(Mathf.CeilToInt(componentData.m_WaterConsumption));
			binder.PropertyName("garbageAccumulation");
			binder.Write(Mathf.CeilToInt(componentData.m_GarbageAccumulation));
			binder.TypeEnd();
		}
	}

	public class PollutionBinder : IPrefabPropertyBinder
	{
		private UIPollutionConfigurationPrefab m_ConfigData;

		public PollutionBinder(UIPollutionConfigurationPrefab data)
		{
			m_ConfigData = data;
		}

		public bool Matches(EntityManager entityManager, Entity entity)
		{
			if (entityManager.TryGetComponent<PollutionData>(entity, out var component))
			{
				return component.m_AirPollution + component.m_GroundPollution + component.m_NoisePollution != 0f;
			}
			return false;
		}

		public void Bind(IJsonWriter binder, EntityManager entityManager, Entity entity)
		{
			PollutionData componentData = entityManager.GetComponentData<PollutionData>(entity);
			binder.TypeBegin("prefabs.PollutionProperty");
			binder.PropertyName("groundPollution");
			binder.Write((int)PollutionUIUtils.GetPollutionKey(m_ConfigData.m_GroundPollution, componentData.m_GroundPollution));
			binder.PropertyName("airPollution");
			binder.Write((int)PollutionUIUtils.GetPollutionKey(m_ConfigData.m_AirPollution, componentData.m_AirPollution));
			binder.PropertyName("noisePollution");
			binder.Write((int)PollutionUIUtils.GetPollutionKey(m_ConfigData.m_NoisePollution, componentData.m_NoisePollution));
			binder.TypeEnd();
		}
	}

	public abstract class ElectricityPropertyBinder : IPrefabPropertyBinder
	{
		public readonly string m_LabelId;

		protected ElectricityPropertyBinder(string labelId)
		{
			m_LabelId = labelId;
		}

		public abstract bool Matches(EntityManager entityManager, Entity entity);

		public abstract void GetValue(EntityManager entityManager, Entity entity, out int minCapacity, out int maxCapacity, out Layer voltageLayers);

		public void Bind(IJsonWriter binder, EntityManager entityManager, Entity entity)
		{
			GetValue(entityManager, entity, out var minCapacity, out var maxCapacity, out var voltageLayers);
			binder.TypeBegin("prefabs.ElectricityProperty");
			binder.PropertyName("labelId");
			binder.Write(m_LabelId);
			binder.PropertyName("minCapacity");
			binder.Write(minCapacity);
			binder.PropertyName("maxCapacity");
			binder.Write(maxCapacity);
			binder.PropertyName("voltage");
			binder.Write((int)ElectricityUIUtils.GetVoltage(voltageLayers));
			binder.TypeEnd();
		}
	}

	[CompilerGenerated]
	public class UpkeepPropertyBinderSystem : GameSystemBase, IPrefabPropertyBinder
	{
		private ResourceSystem m_ResourceSystem;

		private EntityQuery m_BudgetDataQuery;

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
			m_BudgetDataQuery = GetEntityQuery(ComponentType.ReadOnly<ServiceBudgetData>());
		}

		[Preserve]
		protected override void OnUpdate()
		{
		}

		public bool Matches(EntityManager entityManager, Entity entity)
		{
			return base.EntityManager.HasComponent<ServiceUpkeepData>(entity);
		}

		public void Bind(IJsonWriter binder, EntityManager entityManager, Entity entity)
		{
			int2 value = GetValue(entity);
			if (value.x == value.y)
			{
				binder.Write(new UpkeepIntProperty
				{
					labelId = "Properties.UPKEEP",
					unit = "moneyPerMonth",
					value = value.x
				});
			}
			else
			{
				binder.Write(new UpkeepInt2Property
				{
					labelId = "Properties.UPKEEP",
					unit = "moneyPerMonth",
					value = value
				});
			}
		}

		private int2 GetValue(Entity entity)
		{
			int2 result = 0;
			if (base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<ServiceUpkeepData> buffer))
			{
				for (int i = 0; i < buffer.Length; i++)
				{
					ServiceUpkeepData serviceUpkeepData = buffer[i];
					Resource resource = serviceUpkeepData.m_Upkeep.m_Resource;
					int amount = serviceUpkeepData.m_Upkeep.m_Amount;
					if (!m_BudgetDataQuery.IsEmptyIgnoreFilter && resource == Resource.Money)
					{
						result.x += CityServiceUpkeepSystem.CalculateUpkeep(amount, entity, m_BudgetDataQuery.GetSingletonEntity(), base.EntityManager);
						continue;
					}
					float f = (float)amount * EconomyUtils.GetMarketPrice(resource, m_ResourceSystem.GetPrefabs(), base.EntityManager);
					result.y += Mathf.RoundToInt(f);
				}
			}
			result.y += result.x;
			return result;
		}

		[Preserve]
		public UpkeepPropertyBinderSystem()
		{
		}
	}

	public struct UpkeepIntProperty : IJsonWritable
	{
		public string labelId;

		public int value;

		public string unit;

		public bool signed;

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin("prefabs.UpkeepIntProperty");
			writer.PropertyName("labelId");
			writer.Write(labelId);
			writer.PropertyName("value");
			writer.Write(value);
			writer.PropertyName("unit");
			writer.Write(unit);
			writer.PropertyName("signed");
			writer.Write(signed);
			writer.TypeEnd();
		}
	}

	public struct UpkeepInt2Property : IJsonWritable
	{
		public string labelId;

		public int2 value;

		public string unit;

		public bool signed;

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin("prefabs.UpkeepInt2Property");
			writer.PropertyName("labelId");
			writer.Write(labelId);
			writer.PropertyName("value");
			writer.Write(value);
			writer.PropertyName("unit");
			writer.Write(unit);
			writer.PropertyName("signed");
			writer.Write(signed);
			writer.TypeEnd();
		}
	}

	public class StorageLimitBinder : IntPropertyBinder
	{
		public StorageLimitBinder()
			: base("Properties.CARGO_CAPACITY", "weight")
		{
		}

		public override int GetValue(EntityManager entityManager, Entity entity)
		{
			if (entityManager.TryGetComponent<StorageLimitData>(entity, out var component))
			{
				if (entityManager.TryGetComponent<PropertyRenter>(entity, out var _) && entityManager.TryGetComponent<PrefabRef>(entity, out var component3))
				{
					Entity prefab = component3.m_Prefab;
					if (entityManager.TryGetComponent<BuildingPropertyData>(prefab, out var component4) && component4.m_AllowedStored != Resource.NoResource && entityManager.TryGetComponent<SpawnableBuildingData>(prefab, out var component5) && entityManager.TryGetComponent<BuildingData>(prefab, out var component6))
					{
						return component.GetAdjustedLimitForWarehouse(component5, component6);
					}
				}
				return component.m_Limit;
			}
			return 0;
		}

		public override bool Matches(EntityManager entityManager, Entity entity)
		{
			StorageLimitData component;
			return entityManager.TryGetComponent<StorageLimitData>(entity, out component);
		}
	}

	public class PowerProductionBinder : ElectricityPropertyBinder
	{
		public PowerProductionBinder()
			: base("Properties.POWER_PLANT_OUTPUT")
		{
		}

		public override bool Matches(EntityManager entityManager, Entity entity)
		{
			if (entityManager.HasComponent<PowerPlantData>(entity))
			{
				return true;
			}
			if (entityManager.HasComponent<EmergencyGeneratorData>(entity))
			{
				return true;
			}
			if (entityManager.HasComponent<NetData>(entity) && entityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<SubObject> buffer))
			{
				for (int i = 0; i < buffer.Length; i++)
				{
					if (entityManager.HasComponent<PowerPlantData>(buffer[i].m_Prefab))
					{
						return true;
					}
				}
			}
			return false;
		}

		public override void GetValue(EntityManager entityManager, Entity entity, out int minCapacity, out int maxCapacity, out Layer voltageLayers)
		{
			minCapacity = 0;
			maxCapacity = 0;
			voltageLayers = Layer.None;
			AddValues(entityManager, entity, ref minCapacity, ref maxCapacity, ref voltageLayers);
			if (entityManager.HasComponent<NetData>(entity) && entityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<SubObject> buffer))
			{
				for (int i = 0; i < buffer.Length; i++)
				{
					AddValues(entityManager, buffer[i].m_Prefab, ref minCapacity, ref maxCapacity, ref voltageLayers);
				}
			}
		}

		public static void AddValues(EntityManager entityManager, Entity entity, ref int minCapacity, ref int maxCapacity, ref Layer voltageLayers)
		{
			if (entityManager.TryGetComponent<PowerPlantData>(entity, out var component))
			{
				minCapacity += component.m_ElectricityProduction;
				maxCapacity += component.m_ElectricityProduction;
			}
			if (entityManager.TryGetComponent<WindPoweredData>(entity, out var component2))
			{
				maxCapacity += component2.m_Production;
			}
			if (entityManager.TryGetComponent<SolarPoweredData>(entity, out var component3))
			{
				maxCapacity += component3.m_Production;
			}
			if (entityManager.TryGetComponent<GarbagePoweredData>(entity, out var component4))
			{
				maxCapacity += component4.m_Capacity;
			}
			if (entityManager.TryGetComponent<WaterPoweredData>(entity, out var component5))
			{
				maxCapacity += (int)(1000000f * component5.m_CapacityFactor);
			}
			if (entityManager.TryGetComponent<GroundWaterPoweredData>(entity, out var component6))
			{
				maxCapacity += component6.m_Production;
			}
			if (entityManager.TryGetComponent<EmergencyGeneratorData>(entity, out var component7))
			{
				maxCapacity += component7.m_ElectricityProduction;
			}
			voltageLayers |= ElectricityUIUtils.GetPowerLineLayers(entityManager, entity);
		}
	}

	public class TransformerCapacityBinder : IntPropertyBinder
	{
		public TransformerCapacityBinder()
			: base("Properties.TRANSFORMER_CAPACITY", "power")
		{
		}

		public override bool Matches(EntityManager entityManager, Entity entity)
		{
			if (entityManager.HasComponent<Game.Prefabs.TransformerData>(entity))
			{
				return !entityManager.HasComponent<PowerPlantData>(entity);
			}
			return false;
		}

		public override int GetValue(EntityManager entityManager, Entity entity)
		{
			int num = 0;
			int num2 = 0;
			if (entityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Game.Prefabs.SubNet> buffer))
			{
				foreach (Game.Prefabs.SubNet item in buffer)
				{
					if (item.m_NodeIndex.x == item.m_NodeIndex.y && entityManager.TryGetComponent<ElectricityConnectionData>(item.m_Prefab, out var component))
					{
						if (component.m_Voltage == Game.Prefabs.ElectricityConnection.Voltage.Low)
						{
							num += component.m_Capacity;
						}
						else
						{
							num2 += component.m_Capacity;
						}
					}
				}
			}
			return math.min(num, num2);
		}
	}

	public class TransformerInputBinder : StringPropertyBinder
	{
		public TransformerInputBinder()
			: base("Properties.TRANSFORMER_INPUT")
		{
		}

		public override bool Matches(EntityManager entityManager, Entity entity)
		{
			if (entityManager.HasComponent<Game.Prefabs.TransformerData>(entity))
			{
				return !entityManager.HasComponent<PowerPlantData>(entity);
			}
			return false;
		}

		public override string GetValueId(EntityManager entityManager, Entity entity)
		{
			return "Properties.VOLTAGE:1";
		}
	}

	public class TransformerOutputBinder : StringPropertyBinder
	{
		public TransformerOutputBinder()
			: base("Properties.TRANSFORMER_OUTPUT")
		{
		}

		public override bool Matches(EntityManager entityManager, Entity entity)
		{
			return entityManager.HasComponent<Game.Prefabs.TransformerData>(entity);
		}

		public override string GetValueId(EntityManager entityManager, Entity entity)
		{
			return "Properties.VOLTAGE:0";
		}
	}

	public class ElectricityConnectionBinder : ElectricityPropertyBinder
	{
		public ElectricityConnectionBinder()
			: base("Properties.POWER_LINE_CAPACITY")
		{
		}

		public override bool Matches(EntityManager entityManager, Entity entity)
		{
			if (entityManager.TryGetComponent<ElectricityConnectionData>(entity, out var component) && component.m_Capacity > 0 && (component.m_CompositionAll.m_General & CompositionFlags.General.Lighting) == 0 && entityManager.TryGetComponent<NetData>(entity, out var component2))
			{
				return ElectricityUIUtils.HasVoltageLayers(component2.m_LocalConnectLayers);
			}
			return false;
		}

		public override void GetValue(EntityManager entityManager, Entity entity, out int minCapacity, out int maxCapacity, out Layer voltageLayers)
		{
			minCapacity = entityManager.GetComponentData<ElectricityConnectionData>(entity).m_Capacity;
			maxCapacity = minCapacity;
			voltageLayers = entityManager.GetComponentData<NetData>(entity).m_LocalConnectLayers;
		}
	}

	public class WaterConnectionBinder : StringPropertyBinder
	{
		public WaterConnectionBinder()
			: base("Properties.WATER_PIPES")
		{
		}

		public override bool Matches(EntityManager entityManager, Entity entity)
		{
			if (entityManager.TryGetComponent<WaterPipeConnectionData>(entity, out var component) && (component.m_FreshCapacity > 0 || component.m_SewageCapacity > 0) && entityManager.TryGetComponent<NetData>(entity, out var component2) && !entityManager.HasComponent<PipelineData>(entity))
			{
				return (component2.m_LocalConnectLayers & (Layer.WaterPipe | Layer.SewagePipe)) != 0;
			}
			return false;
		}

		public override string GetValueId(EntityManager entityManager, Entity entity)
		{
			WaterPipeConnectionData componentData = entityManager.GetComponentData<WaterPipeConnectionData>(entity);
			if (componentData.m_FreshCapacity > 0 && componentData.m_SewageCapacity > 0)
			{
				return "Properties.WATER_PIPE_TYPE[Combined]";
			}
			if (componentData.m_FreshCapacity > 0)
			{
				return "Properties.WATER_PIPE_TYPE[Fresh]";
			}
			return "Properties.WATER_PIPE_TYPE[Sewage]";
		}
	}

	public class JailCapacityBinder : IntPropertyBinder
	{
		public JailCapacityBinder()
			: base("Properties.JAIL_CAPACITY", "integer")
		{
		}

		public override bool Matches(EntityManager entityManager, Entity entity)
		{
			if (!entityManager.TryGetComponent<PoliceStationData>(entity, out var component) || component.m_JailCapacity <= 0)
			{
				if (entityManager.TryGetComponent<PrisonData>(entity, out var component2))
				{
					return component2.m_PrisonerCapacity > 0;
				}
				return false;
			}
			return true;
		}

		public override int GetValue(EntityManager entityManager, Entity entity)
		{
			int num = 0;
			if (entityManager.TryGetComponent<PoliceStationData>(entity, out var component))
			{
				num += component.m_JailCapacity;
			}
			if (entityManager.TryGetComponent<PrisonData>(entity, out var component2))
			{
				num += component2.m_PrisonerCapacity;
			}
			return num;
		}
	}

	public class TransportStopBinder : IPrefabPropertyBinder
	{
		public bool Matches(EntityManager entityManager, Entity entity)
		{
			if (entityManager.HasComponent<NetData>(entity))
			{
				return false;
			}
			if (entityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<SubObject> buffer))
			{
				for (int i = 0; i < buffer.Length; i++)
				{
					if (entityManager.TryGetComponent<TransportStopData>(buffer[i].m_Prefab, out var component))
					{
						if (component.m_PassengerTransport)
						{
							return IsValidTransportType(component.m_TransportType);
						}
						return false;
					}
				}
			}
			return false;
		}

		public void Bind(IJsonWriter binder, EntityManager entityManager, Entity entity)
		{
			DynamicBuffer<SubObject> buffer = entityManager.GetBuffer<SubObject>(entity, isReadOnly: true);
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = 0;
			int num7 = 0;
			for (int i = 0; i < buffer.Length; i++)
			{
				if (entityManager.TryGetComponent<TransportStopData>(buffer[i].m_Prefab, out var component) && component.m_PassengerTransport)
				{
					if (component.m_TransportType == TransportType.Bus)
					{
						num++;
					}
					else if (component.m_TransportType == TransportType.Train)
					{
						num2++;
					}
					else if (component.m_TransportType == TransportType.Tram)
					{
						num3++;
					}
					else if (component.m_TransportType == TransportType.Ship)
					{
						num4++;
					}
					else if (component.m_TransportType == TransportType.Helicopter)
					{
						num5++;
					}
					else if (component.m_TransportType == TransportType.Airplane)
					{
						num6++;
					}
					else if (component.m_TransportType == TransportType.Subway)
					{
						num7++;
					}
				}
			}
			binder.TypeBegin("prefabs.TransportStopProperty");
			binder.PropertyName("stops");
			binder.MapBegin(7u);
			binder.Write("Airplane");
			binder.Write(num6);
			binder.Write("Helicopter");
			binder.Write(num5);
			binder.Write("Ship");
			binder.Write(num4);
			binder.Write("Subway");
			binder.Write(num7);
			binder.Write("Tram");
			binder.Write(num3);
			binder.Write("Train");
			binder.Write(num2);
			binder.Write("Bus");
			binder.Write(num);
			binder.MapEnd();
			binder.TypeEnd();
		}

		private static bool IsValidTransportType(TransportType type)
		{
			switch (type)
			{
			case TransportType.Bus:
			case TransportType.Train:
			case TransportType.Tram:
			case TransportType.Ship:
			case TransportType.Helicopter:
			case TransportType.Airplane:
			case TransportType.Subway:
				return true;
			default:
				return false;
			}
		}
	}

	public class RequiredResourceBinder : StringPropertyBinder
	{
		private ResourceSystem m_ResourceSystem;

		public RequiredResourceBinder(ResourceSystem resourceSystem)
			: base("Properties.REQUIRED_RESOURCE")
		{
			m_ResourceSystem = resourceSystem;
		}

		public override bool Matches(EntityManager entityManager, Entity entity)
		{
			if (!RequiresWater(entityManager, entity, out var _))
			{
				return GetExtractorType(entityManager, entity) != MapFeature.None;
			}
			return true;
		}

		public override string GetValueId(EntityManager entityManager, Entity entity)
		{
			if (RequiresWater(entityManager, entity, out var types))
			{
				if ((types & AllowedWaterTypes.Groundwater) != AllowedWaterTypes.None)
				{
					return "Properties.MAP_RESOURCE[GroundWater]";
				}
				return "Properties.MAP_RESOURCE[SurfaceWater]";
			}
			return $"Properties.MAP_RESOURCE[{GetExtractorType(entityManager, entity):G}]";
		}

		private bool RequiresWater(EntityManager entityManager, Entity entity, out AllowedWaterTypes types)
		{
			if (entityManager.HasComponent<GroundWaterPoweredData>(entity))
			{
				types = AllowedWaterTypes.Groundwater;
				return true;
			}
			if (entityManager.TryGetComponent<WaterPumpingStationData>(entity, out var component) && component.m_Types != AllowedWaterTypes.None)
			{
				types = component.m_Types;
				return true;
			}
			types = AllowedWaterTypes.None;
			return false;
		}

		private MapFeature GetExtractorType(EntityManager entityManager, Entity entity)
		{
			Entity entity2 = entity;
			if (entityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<ServiceUpgradeBuilding> buffer) && buffer.Length >= 1)
			{
				entity2 = buffer[0].m_Building;
			}
			if (!entityManager.TryGetComponent<PlaceholderBuildingData>(entity2, out var component) || component.m_Type != BuildingType.ExtractorBuilding)
			{
				return MapFeature.None;
			}
			if (!entityManager.TryGetComponent<BuildingPropertyData>(entity2, out var component2))
			{
				return MapFeature.None;
			}
			if (!entityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Game.Prefabs.SubArea> buffer2))
			{
				return MapFeature.None;
			}
			ResourcePrefabs prefabs = m_ResourceSystem.GetPrefabs();
			Resource allowedManufactured = component2.m_AllowedManufactured;
			if (!entityManager.TryGetComponent<ResourceData>(prefabs[allowedManufactured], out var component3) || !component3.m_RequireNaturalResource)
			{
				return MapFeature.None;
			}
			foreach (Game.Prefabs.SubArea item in buffer2)
			{
				if (entityManager.TryGetComponent<ExtractorAreaData>(item.m_Prefab, out var component4) && component4.m_RequireNaturalResource)
				{
					return component4.m_MapFeature;
				}
			}
			return MapFeature.None;
		}
	}

	public class UpkeepModifierBinder : IntPropertyBinder
	{
		public UpkeepModifierBinder()
			: base("Properties.RESOURCE_CONSUMPTION", "percentage", signed: true)
		{
		}

		public override bool Matches(EntityManager entityManager, Entity entity)
		{
			if (entityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<UpkeepModifierData> buffer))
			{
				foreach (UpkeepModifierData item in buffer)
				{
					if (item.m_Multiplier != 1f)
					{
						return true;
					}
				}
			}
			return false;
		}

		public override int GetValue(EntityManager entityManager, Entity entity)
		{
			DynamicBuffer<UpkeepModifierData> buffer = entityManager.GetBuffer<UpkeepModifierData>(entity, isReadOnly: true);
			float num = 0f;
			foreach (UpkeepModifierData item in buffer)
			{
				num = math.max(num, item.m_Multiplier);
			}
			return (int)math.round(100f * (num - 1f));
		}
	}

	private const string kGroup = "prefabs";

	private PrefabSystem m_PrefabSystem;

	private UniqueAssetTrackingSystem m_UniqueAssetTrackingSystem;

	private ImageSystem m_ImageSystem;

	private Entity m_RequirementEntity;

	private Entity m_TutorialRequirementEntity;

	private EntityQuery m_ThemeQuery;

	private EntityQuery m_ModifiedThemeQuery;

	private EntityQuery m_UnlockedPrefabQuery;

	private EntityQuery m_PollutionConfigQuery;

	private EntityQuery m_ManualUITagsConfigQuery;

	private GetterValueBinding<Dictionary<string, string>> m_UITagsBinding;

	private RawValueBinding m_ThemesBinding;

	private RawMapBinding<Entity> m_PrefabDetailsBinding;

	private int m_UnlockRequirementVersion;

	private int m_UITagVersion;

	private bool m_Initialized;

	public override GameMode gameMode => GameMode.Game;

	public List<IPrefabEffectBinder> effectBinders { get; private set; }

	public List<IPrefabPropertyBinder> constructionCostBinders { get; private set; }

	public List<IPrefabPropertyBinder> propertyBinders { get; private set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_UniqueAssetTrackingSystem = base.World.GetOrCreateSystemManaged<UniqueAssetTrackingSystem>();
		m_ImageSystem = base.World.GetOrCreateSystemManaged<ImageSystem>();
		m_RequirementEntity = base.EntityManager.CreateEntity();
		base.EntityManager.AddBuffer<UnlockRequirement>(m_RequirementEntity);
		m_TutorialRequirementEntity = base.EntityManager.CreateEntity();
		m_ThemeQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<UIObjectData>(), ComponentType.ReadOnly<ThemeData>());
		m_ModifiedThemeQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<PrefabData>(),
				ComponentType.ReadOnly<ThemeData>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_UnlockedPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<Unlock>());
		m_PollutionConfigQuery = GetEntityQuery(ComponentType.ReadOnly<UIPollutionConfigurationData>());
		m_ManualUITagsConfigQuery = GetEntityQuery(ComponentType.ReadOnly<ManualUITagsConfigurationData>());
		AddBinding(m_ThemesBinding = new RawValueBinding("prefabs", "themes", BindThemes));
		AddBinding(m_PrefabDetailsBinding = new RawMapBinding<Entity>("prefabs", "prefabDetails", BindPrefabDetails));
		AddBinding(m_UITagsBinding = new GetterValueBinding<Dictionary<string, string>>("prefabs", "manualUITags", BindManualUITags, ValueWriters.Nullable(new DictionaryWriter<string, string>())));
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		if (base.Enabled)
		{
			m_Initialized = true;
			constructionCostBinders = BuildDefaultConstructionCostBinders();
			propertyBinders = BuildDefaultPropertyBinders();
			effectBinders = BuildDefaultEffectBinders();
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_ModifiedThemeQuery.IsEmptyIgnoreFilter)
		{
			m_ThemesBinding.Update();
		}
		int componentOrderVersion = base.EntityManager.GetComponentOrderVersion<UnlockRequirementData>();
		int componentOrderVersion2 = base.EntityManager.GetComponentOrderVersion<ManualUITagsConfigurationData>();
		if (PrefabUtils.HasUnlockedPrefab<UIObjectData>(base.EntityManager, m_UnlockedPrefabQuery) || m_UnlockRequirementVersion != componentOrderVersion)
		{
			m_PrefabDetailsBinding.UpdateAll();
		}
		if (!m_ManualUITagsConfigQuery.IsEmptyIgnoreFilter && componentOrderVersion2 != m_UITagVersion)
		{
			m_UITagsBinding.Update();
		}
		m_UnlockRequirementVersion = componentOrderVersion;
		m_UITagVersion = componentOrderVersion2;
	}

	private Dictionary<string, string> BindManualUITags()
	{
		if (m_ManualUITagsConfigQuery.IsEmptyIgnoreFilter)
		{
			return null;
		}
		Entity singletonEntity = m_ManualUITagsConfigQuery.GetSingletonEntity();
		ManualUITagsConfiguration prefab = m_PrefabSystem.GetPrefab<ManualUITagsConfiguration>(singletonEntity);
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		FieldInfo[] fields = typeof(ManualUITagsConfiguration).GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			UITagPrefab uITagPrefab = fieldInfo.GetValue(prefab) as UITagPrefab;
			if (uITagPrefab != null)
			{
				string key = fieldInfo.Name[2].ToString().ToLower() + fieldInfo.Name.Remove(0, 3);
				dictionary[key] = uITagPrefab.uiTag;
			}
		}
		return dictionary;
	}

	private void BindThemes(IJsonWriter writer)
	{
		NativeList<UIObjectInfo> sortedObjects = UIObjectInfo.GetSortedObjects(m_ThemeQuery, Allocator.TempJob);
		writer.ArrayBegin(sortedObjects.Length);
		for (int i = 0; i < sortedObjects.Length; i++)
		{
			ThemePrefab prefab = m_PrefabSystem.GetPrefab<ThemePrefab>(sortedObjects[i].prefabData);
			writer.TypeBegin("prefabs.Theme");
			writer.PropertyName("entity");
			writer.Write(sortedObjects[i].entity);
			writer.PropertyName("name");
			writer.Write(prefab.name);
			writer.PropertyName("icon");
			writer.Write(ImageSystem.GetIcon(prefab) ?? m_ImageSystem.placeholderIcon);
			writer.TypeEnd();
		}
		writer.ArrayEnd();
		sortedObjects.Dispose();
	}

	private void BindPrefabDetails(IJsonWriter writer, Entity entity)
	{
		bool unique = m_UniqueAssetTrackingSystem.IsUniqueAsset(entity);
		bool placed = m_UniqueAssetTrackingSystem.IsPlacedUniqueAsset(entity);
		BindPrefabDetails(writer, entity, unique, placed);
	}

	public void BindPrefabDetails(IJsonWriter writer, Entity entity, bool unique, bool placed)
	{
		if (!m_Initialized)
		{
			writer.WriteNull();
			return;
		}
		Entity entity2 = entity;
		if (base.EntityManager.HasComponent<NetData>(entity2) && base.EntityManager.TryGetBuffer(entity2, isReadOnly: true, out DynamicBuffer<SubObject> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				SubObject subObject = buffer[i];
				if ((subObject.m_Flags & SubObjectFlags.MakeOwner) != 0)
				{
					entity2 = subObject.m_Prefab;
					break;
				}
			}
		}
		if (base.EntityManager.Exists(entity) && base.EntityManager.TryGetEnabledComponent<PrefabData>(entity2, out var component) && base.EntityManager.TryGetEnabledComponent<PrefabData>(entity, out var component2))
		{
			PrefabBase prefab = m_PrefabSystem.GetPrefab<PrefabBase>(component2);
			PrefabBase prefab2 = m_PrefabSystem.GetPrefab<PrefabBase>(component);
			GetTitleAndDescription(entity2, out var titleId, out var descriptionId);
			string contentPrerequisite = PrefabUtils.GetContentPrerequisite(prefab);
			writer.TypeBegin("prefabs.PrefabDetails");
			writer.PropertyName("entity");
			writer.Write(entity);
			writer.PropertyName("name");
			writer.Write(prefab.name);
			writer.PropertyName("uiTag");
			writer.Write(prefab.uiTag);
			writer.PropertyName("icon");
			writer.Write(ImageSystem.GetThumbnail(prefab) ?? m_ImageSystem.placeholderIcon);
			writer.PropertyName("dlc");
			if (contentPrerequisite != null)
			{
				writer.Write("Media/DLC/" + contentPrerequisite + ".svg");
			}
			else
			{
				writer.WriteNull();
			}
			writer.PropertyName("preview");
			writer.Write(prefab2.TryGet<SignatureBuilding>(out var component3) ? component3.m_UnlockEventImage : null);
			writer.PropertyName("titleId");
			writer.Write(titleId);
			writer.PropertyName("descriptionId");
			writer.Write(descriptionId);
			writer.PropertyName("locked");
			writer.Write(base.EntityManager.HasEnabledComponent<Locked>(entity));
			writer.PropertyName("unique");
			writer.Write(unique);
			writer.PropertyName("placed");
			writer.Write(placed);
			writer.PropertyName("constructionCost");
			BindConstructionCost(writer, entity2);
			writer.PropertyName("effects");
			BindEffects(writer, entity2);
			writer.PropertyName("properties");
			BindProperties(writer, entity2);
			writer.PropertyName("requirements");
			BindPrefabRequirements(writer, entity);
			writer.TypeEnd();
		}
		else
		{
			writer.WriteNull();
		}
	}

	public void GetTitleAndDescription(Entity prefabEntity, out string titleId, [CanBeNull] out string descriptionId)
	{
		if (m_PrefabSystem.TryGetPrefab<PrefabBase>(prefabEntity, out var prefab))
		{
			if (prefab is UIAssetMenuPrefab || prefab is ServicePrefab)
			{
				titleId = "Services.NAME[" + prefab.name + "]";
				descriptionId = "Services.DESCRIPTION[" + prefab.name + "]";
			}
			else if (prefab is UIAssetCategoryPrefab)
			{
				titleId = "SubServices.NAME[" + prefab.name + "]";
				descriptionId = "Assets.SUB_SERVICE_DESCRIPTION[" + prefab.name + "]";
			}
			else if (prefab.Has<Game.Prefabs.ServiceUpgrade>())
			{
				titleId = "Assets.UPGRADE_NAME[" + prefab.name + "]";
				descriptionId = "Assets.UPGRADE_DESCRIPTION[" + prefab.name + "]";
			}
			else
			{
				titleId = "Assets.NAME[" + prefab.name + "]";
				descriptionId = "Assets.DESCRIPTION[" + prefab.name + "]";
			}
		}
		else
		{
			titleId = m_PrefabSystem.GetObsoleteID(prefabEntity).GetName();
			descriptionId = "Assets.MISSING_PREFAB_DESCRIPTION";
		}
	}

	private static List<IPrefabEffectBinder> BuildDefaultEffectBinders()
	{
		return new List<IPrefabEffectBinder>
		{
			new CityModifierBinder(),
			new LocalModifierBinder(),
			new LeisureProviderBinder()
		};
	}

	public void BindEffects(IJsonWriter binder, Entity prefabEntity)
	{
		int num = 0;
		foreach (IPrefabEffectBinder effectBinder in effectBinders)
		{
			if (effectBinder.Matches(base.EntityManager, prefabEntity))
			{
				num++;
			}
		}
		binder.ArrayBegin(num);
		foreach (IPrefabEffectBinder effectBinder2 in effectBinders)
		{
			if (effectBinder2.Matches(base.EntityManager, prefabEntity))
			{
				effectBinder2.Bind(binder, base.EntityManager, prefabEntity);
			}
		}
		binder.ArrayEnd();
	}

	private static List<IPrefabPropertyBinder> BuildDefaultConstructionCostBinders()
	{
		return new List<IPrefabPropertyBinder>
		{
			new ConstructionCostBinder(),
			new PlaceableNetCostBinder(),
			new ComponentIntPropertyBinder<ServiceUpgradeData>("Properties.CONSTRUCTION_COST", "money", (ServiceUpgradeData data) => Convert.ToInt32(data.m_UpgradeCost))
		};
	}

	public void BindConstructionCost(IJsonWriter binder, Entity prefabEntity)
	{
		if (m_Initialized)
		{
			foreach (IPrefabPropertyBinder constructionCostBinder in constructionCostBinders)
			{
				if (constructionCostBinder.Matches(base.EntityManager, prefabEntity))
				{
					constructionCostBinder.Bind(binder, base.EntityManager, prefabEntity);
					return;
				}
			}
		}
		binder.WriteNull();
	}

	private List<IPrefabPropertyBinder> BuildDefaultPropertyBinders()
	{
		return new List<IPrefabPropertyBinder>
		{
			new RequiredResourceBinder(base.World.GetOrCreateSystemManaged<ResourceSystem>()),
			base.World.GetOrCreateSystemManaged<UpkeepPropertyBinderSystem>(),
			new ComponentIntPropertyBinder<AssetStampData>("Properties.UPKEEP", "moneyPerMonth", (AssetStampData data) => (int)data.m_UpKeepCost),
			new ComponentIntPropertyBinder<PlaceableNetData>("Properties.UPKEEP", "moneyPerDistancePerMonth", (PlaceableNetData data) => Convert.ToInt32(data.m_DefaultUpkeepCost) * 125),
			new PowerProductionBinder(),
			new ComponentIntPropertyBinder<BatteryData>("Properties.BATTERY_CAPACITY", "energy", (BatteryData data) => data.m_Capacity),
			new ComponentIntPropertyBinder<BatteryData>("Properties.BATTERY_POWER_OUTPUT", "power", (BatteryData data) => data.m_PowerOutput),
			new TransformerCapacityBinder(),
			new TransformerInputBinder(),
			new TransformerOutputBinder(),
			new ElectricityConnectionBinder(),
			new WaterConnectionBinder(),
			new ComponentIntPropertyBinder<SewageOutletData>("Properties.SEWAGE_CAPACITY", "volumePerMonth", (SewageOutletData data) => data.m_Capacity),
			new ComponentIntPropertyBinder<SewageOutletData>("Properties.SEWAGE_PURIFICATION_RATE", "percentage", (SewageOutletData data) => Mathf.RoundToInt(100f * data.m_Purification)),
			new ComponentIntPropertyBinder<WaterPumpingStationData>("Properties.WATER_CAPACITY", "volumePerMonth", (WaterPumpingStationData data) => data.m_Capacity),
			new ComponentIntPropertyBinder<WaterPumpingStationData>("Properties.WATER_PURIFICATION_RATE", "percentage", (WaterPumpingStationData data) => Mathf.RoundToInt(100f * data.m_Purification)),
			new ComponentIntPropertyBinder<HospitalData>("Properties.PATIENT_CAPACITY", "integer", (HospitalData data) => data.m_PatientCapacity),
			new ComponentIntPropertyBinder<HospitalData>("Properties.AMBULANCE_COUNT", "integer", (HospitalData data) => data.m_AmbulanceCapacity),
			new ComponentIntPropertyBinder<HospitalData>("Properties.MEDICAL_HELICOPTER_COUNT", "integer", (HospitalData data) => data.m_MedicalHelicopterCapacity),
			new ComponentIntPropertyBinder<DeathcareFacilityData>("Properties.DECEASED_PROCESSING_CAPACITY", "integerPerMonth", (DeathcareFacilityData data) => Mathf.CeilToInt(data.m_ProcessingRate)),
			new ComponentIntPropertyBinder<DeathcareFacilityData>("Properties.DECEASED_STORAGE", "integer", (DeathcareFacilityData data) => data.m_StorageCapacity),
			new ComponentIntPropertyBinder<DeathcareFacilityData>("Properties.HEARSE_COUNT", "integer", (DeathcareFacilityData data) => data.m_HearseCapacity),
			new ComponentIntPropertyBinder<GarbageFacilityData>("Properties.GARBAGE_PROCESSING_CAPACITY", "weightPerMonth", (GarbageFacilityData data) => data.m_ProcessingSpeed),
			new ComponentIntPropertyBinder<GarbageFacilityData>("Properties.GARBAGE_STORAGE", "weight", (GarbageFacilityData data) => data.m_GarbageCapacity),
			new ComponentIntPropertyBinder<StorageAreaData>("Properties.GARBAGE_STORAGE", "weightPerCell", (StorageAreaData data) => data.m_Capacity * 1000),
			new ComponentIntPropertyBinder<GarbageFacilityData>("Properties.GARBAGE_TRUCK_COUNT", "integer", (GarbageFacilityData data) => data.m_VehicleCapacity),
			new ComponentIntPropertyBinder<FireStationData>("Properties.FIRE_ENGINE_COUNT", "integer", (FireStationData data) => data.m_FireEngineCapacity),
			new ComponentIntPropertyBinder<FireStationData>("Properties.FIRE_HELICOPTER_COUNT", "integer", (FireStationData data) => data.m_FireHelicopterCapacity),
			new ComponentIntPropertyBinder<EmergencyShelterData>("Properties.SHELTER_CAPACITY", "integer", (EmergencyShelterData data) => data.m_ShelterCapacity),
			new ComponentIntPropertyBinder<EmergencyShelterData>("Properties.EVACUATION_BUS_COUNT", "integer", (EmergencyShelterData data) => data.m_VehicleCapacity),
			new JailCapacityBinder(),
			new ComponentIntPropertyBinder<PoliceStationData>("Properties.PATROL_CAR_COUNT", "integer", (PoliceStationData data) => data.m_PatrolCarCapacity),
			new ComponentIntPropertyBinder<PoliceStationData>("Properties.POLICE_HELICOPTER_COUNT", "integer", (PoliceStationData data) => data.m_PoliceHelicopterCapacity),
			new ComponentIntPropertyBinder<PrisonData>("Properties.PRISON_VAN_COUNT", "integer", (PrisonData data) => data.m_PrisonVanCapacity),
			new ComponentIntPropertyBinder<SchoolData>("Properties.STUDENT_CAPACITY", "integer", (SchoolData data) => data.m_StudentCapacity),
			new ComponentIntPropertyBinder<TransportDepotData>("Properties.TRANSPORT_VEHICLE_COUNT", "integer", (TransportDepotData data) => data.m_VehicleCapacity),
			new TransportStopBinder(),
			new ComponentIntPropertyBinder<MaintenanceDepotData>("Properties.MAINTENANCE_VEHICLES", "integer", (MaintenanceDepotData data) => data.m_VehicleCapacity),
			new ComponentIntPropertyBinder<PostFacilityData>("Properties.MAIL_SORTING_RATE", "integerPerMonth", (PostFacilityData data) => data.m_SortingRate),
			new ComponentIntPropertyBinder<PostFacilityData>("Properties.MAIL_STORAGE_CAPACITY", "integer", (PostFacilityData data) => data.m_MailCapacity),
			new ComponentIntPropertyBinder<MailBoxData>("Properties.MAIL_BOX_CAPACITY", "integer", (MailBoxData data) => data.m_MailCapacity),
			new ComponentIntPropertyBinder<PostFacilityData>("Properties.POST_VAN_COUNT", "integer", (PostFacilityData data) => data.m_PostVanCapacity),
			new ComponentIntPropertyBinder<PostFacilityData>("Properties.POST_TRUCK_COUNT", "integer", (PostFacilityData data) => data.m_PostTruckCapacity),
			new ComponentIntPropertyBinder<TelecomFacilityData>("Properties.NETWORK_RANGE", "length", (TelecomFacilityData data) => Mathf.CeilToInt(data.m_Range)),
			new ComponentIntPropertyBinder<TelecomFacilityData>("Properties.NETWORK_CAPACITY", "dataRate", (TelecomFacilityData data) => Mathf.CeilToInt(data.m_NetworkCapacity)),
			new ComponentIntPropertyBinder<AttractionData>("Properties.ATTRACTIVENESS", "integer", (AttractionData data) => data.m_Attractiveness),
			new UpkeepModifierBinder(),
			new StorageLimitBinder(),
			new PollutionBinder(m_PrefabSystem.GetSingletonPrefab<UIPollutionConfigurationPrefab>(m_PollutionConfigQuery)),
			new ComponentIntPropertyBinder<PollutionModifierData>("SelectedInfoPanel.POLLUTION_LEVELS_GROUND", "percentage", (PollutionModifierData data) => Mathf.RoundToInt(data.m_GroundPollutionMultiplier * 100f), omitZero: true, signed: true, null, "Media/Game/Icons/GroundPollution.svg"),
			new ComponentIntPropertyBinder<PollutionModifierData>("SelectedInfoPanel.POLLUTION_LEVELS_AIR", "percentage", (PollutionModifierData data) => Mathf.RoundToInt(data.m_AirPollutionMultiplier * 100f), omitZero: true, signed: true, null, "Media/Game/Icons/AirPollution.svg"),
			new ComponentIntPropertyBinder<PollutionModifierData>("SelectedInfoPanel.POLLUTION_LEVELS_NOISE", "percentage", (PollutionModifierData data) => Mathf.RoundToInt(data.m_NoisePollutionMultiplier * 100f), omitZero: true, signed: true, null, "Media/Game/Icons/NoisePollution.svg"),
			new ComponentIntPropertyBinder<ParkingFacilityData>("Properties.COMFORT", "integer", (ParkingFacilityData data) => (int)math.round(100f * data.m_ComfortFactor)),
			new ComponentIntPropertyBinder<TransportStopData>("Properties.COMFORT", "integer", (TransportStopData data) => (int)math.round(100f * data.m_ComfortFactor)),
			new ComponentIntPropertyBinder<TransportStationData>("Properties.COMFORT", "integer", (TransportStationData data) => (int)math.round(100f * data.m_ComfortFactor))
		};
	}

	public void BindProperties(IJsonWriter binder, Entity prefabEntity)
	{
		int num = 0;
		foreach (IPrefabPropertyBinder propertyBinder in propertyBinders)
		{
			if (propertyBinder.Matches(base.EntityManager, prefabEntity))
			{
				num++;
			}
		}
		binder.ArrayBegin(num);
		foreach (IPrefabPropertyBinder propertyBinder2 in propertyBinders)
		{
			if (propertyBinder2.Matches(base.EntityManager, prefabEntity))
			{
				propertyBinder2.Bind(binder, base.EntityManager, prefabEntity);
			}
		}
		binder.ArrayEnd();
	}

	public void BindPrefabRequirements(IJsonWriter writer, Entity prefabEntity)
	{
		if (base.EntityManager.HasComponent<UIGroupElement>(prefabEntity))
		{
			BindUIGroupRequirements(writer, prefabEntity);
		}
		else
		{
			BindRequirements(writer, prefabEntity);
		}
	}

	private void BindUIGroupRequirements(IJsonWriter writer, Entity prefabEntity)
	{
		if (base.EntityManager.TryGetComponent<ForceUnlockRequirementData>(prefabEntity, out var component))
		{
			BindRequirements(writer, component.m_Prefab);
			return;
		}
		NativeList<Entity> requirements = new NativeList<Entity>(4, Allocator.TempJob);
		NativeList<UnlockRequirement> list = new NativeList<UnlockRequirement>(4, Allocator.TempJob);
		FindLowestRequirements(prefabEntity, requirements);
		if (requirements.Length > 1)
		{
			DynamicBuffer<UnlockRequirement> buffer = base.EntityManager.GetBuffer<UnlockRequirement>(m_RequirementEntity);
			for (int i = 0; i < requirements.Length; i++)
			{
				Entity entity = requirements[i];
				if (!base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<UnlockRequirement> buffer2))
				{
					continue;
				}
				for (int j = 0; j < buffer2.Length; j++)
				{
					if (buffer2[j].m_Prefab != entity && !list.Contains(buffer2[j]))
					{
						list.Add(buffer2[j]);
						buffer.Add(new UnlockRequirement
						{
							m_Prefab = buffer2[j].m_Prefab,
							m_Flags = UnlockFlags.RequireAny
						});
					}
				}
			}
			BindRequirements(writer, m_RequirementEntity);
			buffer.Clear();
		}
		else if (requirements.Length > 0)
		{
			BindRequirements(writer, requirements[0]);
		}
		else
		{
			BindRequirements(writer, m_RequirementEntity);
		}
		requirements.Dispose();
		list.Dispose();
	}

	private int FindLowestRequirements(Entity prefabEntity, NativeList<Entity> requirements, int score = -1)
	{
		NativeList<UIObjectInfo> sortedObjects = UIObjectInfo.GetSortedObjects(base.EntityManager, base.EntityManager.GetBuffer<UIGroupElement>(prefabEntity, isReadOnly: true), Allocator.TempJob);
		NativeParallelHashMap<Entity, UnlockFlags> devTreeNodes = new NativeParallelHashMap<Entity, UnlockFlags>(10, Allocator.TempJob);
		NativeParallelHashMap<Entity, UnlockFlags> unlockRequirements = new NativeParallelHashMap<Entity, UnlockFlags>(10, Allocator.TempJob);
		foreach (UIObjectInfo item in sortedObjects)
		{
			if (base.EntityManager.HasComponent<UIGroupElement>(item.entity))
			{
				int num = FindLowestRequirements(item.entity, requirements, score);
				if ((requirements.Length > 0 && score == -1) || num < score)
				{
					score = num;
				}
				continue;
			}
			GetRequirements(item.entity, out var milestone, devTreeNodes, unlockRequirements);
			int num2 = 0;
			if (milestone != Entity.Null)
			{
				num2 += base.EntityManager.GetComponentData<MilestoneData>(milestone).m_Index * 10000;
			}
			foreach (KeyValue<Entity, UnlockFlags> item2 in devTreeNodes)
			{
				DevTreeNodePrefab prefab = m_PrefabSystem.GetPrefab<DevTreeNodePrefab>(item2.Key);
				num2 += prefab.m_HorizontalPosition * 100;
			}
			num2 += unlockRequirements.Count() * 10;
			foreach (KeyValue<Entity, UnlockFlags> item3 in unlockRequirements)
			{
				if (base.EntityManager.HasComponent<UnlockRequirementData>(item3.Key))
				{
					UnlockRequirementPrefab prefab2 = m_PrefabSystem.GetPrefab<UnlockRequirementPrefab>(item3.Key);
					num2 = ((prefab2 is ZoneBuiltRequirementPrefab zoneBuiltRequirementPrefab) ? (num2 + (zoneBuiltRequirementPrefab.m_MinimumLevel * zoneBuiltRequirementPrefab.m_MinimumCount + zoneBuiltRequirementPrefab.m_MinimumSquares / 100)) : ((prefab2 is CitizenRequirementPrefab citizenRequirementPrefab) ? (num2 + (citizenRequirementPrefab.m_MinimumPopulation / 10 + citizenRequirementPrefab.m_MinimumHappiness * 100)) : ((!(prefab2 is ProcessingRequirementPrefab processingRequirementPrefab)) ? (num2 + 100) : (num2 + processingRequirementPrefab.m_MinimumProducedAmount / 10))));
				}
				else
				{
					num2 += 10;
				}
			}
			if ((item.entity != Entity.Null && !requirements.Contains(item.entity) && score == -1) || num2 <= score)
			{
				if (num2 < score)
				{
					requirements.Clear();
				}
				requirements.Add(item.entity);
				score = num2;
			}
		}
		sortedObjects.Dispose();
		devTreeNodes.Dispose();
		unlockRequirements.Dispose();
		return score;
	}

	private void BindRequirements(IJsonWriter writer, Entity prefabEntity)
	{
		NativeParallelHashMap<Entity, UnlockFlags> devTreeNodes = new NativeParallelHashMap<Entity, UnlockFlags>(10, Allocator.TempJob);
		NativeParallelHashMap<Entity, UnlockFlags> unlockRequirements = new NativeParallelHashMap<Entity, UnlockFlags>(10, Allocator.TempJob);
		writer.TypeBegin("Game.UI.Game.PrefabUISystem.UnlockingRequirements");
		GetRequirements(prefabEntity, out var milestone, devTreeNodes, unlockRequirements);
		VerifyRequirements(devTreeNodes, unlockRequirements);
		BindRequirements(writer, UnlockFlags.RequireAll, milestone, devTreeNodes, unlockRequirements);
		BindRequirements(writer, UnlockFlags.RequireAny, Entity.Null, devTreeNodes, unlockRequirements);
		writer.TypeEnd();
		devTreeNodes.Dispose();
		unlockRequirements.Dispose();
	}

	private void VerifyRequirements(NativeParallelHashMap<Entity, UnlockFlags> devTreeNodes, NativeParallelHashMap<Entity, UnlockFlags> unlockRequirements)
	{
		Entity entity = Entity.Null;
		Entity entity2 = Entity.Null;
		int num = 0;
		foreach (KeyValue<Entity, UnlockFlags> item in devTreeNodes)
		{
			if ((item.Value & UnlockFlags.RequireAny) != 0)
			{
				entity = item.Key;
				num++;
			}
		}
		foreach (KeyValue<Entity, UnlockFlags> item2 in unlockRequirements)
		{
			if ((item2.Value & UnlockFlags.RequireAny) != 0)
			{
				entity2 = item2.Key;
				num++;
			}
		}
		if (num == 1)
		{
			if (entity != Entity.Null)
			{
				devTreeNodes[entity] = UnlockFlags.RequireAll;
			}
			if (entity2 != Entity.Null)
			{
				unlockRequirements[entity2] = UnlockFlags.RequireAll;
			}
		}
	}

	private void BindRequirements(IJsonWriter writer, UnlockFlags flag, Entity milestone, NativeParallelHashMap<Entity, UnlockFlags> devTreeNodes, NativeParallelHashMap<Entity, UnlockFlags> unlockRequirements)
	{
		NativeList<Entity> nativeList = new NativeList<Entity>(2, Allocator.TempJob);
		NativeList<Entity> nativeList2 = new NativeList<Entity>(4, Allocator.TempJob);
		foreach (KeyValue<Entity, UnlockFlags> item in devTreeNodes)
		{
			if ((item.Value & flag) != 0)
			{
				nativeList.Add(item.Key);
			}
		}
		for (int i = 0; i < nativeList.Length; i++)
		{
			devTreeNodes.Remove(nativeList[i]);
		}
		foreach (KeyValue<Entity, UnlockFlags> item2 in unlockRequirements)
		{
			if ((item2.Value & flag) != 0)
			{
				nativeList2.Add(item2.Key);
			}
		}
		for (int j = 0; j < nativeList2.Length; j++)
		{
			unlockRequirements.Remove(nativeList2[j]);
		}
		writer.PropertyName((flag == UnlockFlags.RequireAll) ? "requireAll" : "requireAny");
		writer.ArrayBegin(((milestone != Entity.Null) ? 1 : 0) + nativeList.Length + nativeList2.Length);
		if (milestone != Entity.Null)
		{
			BindMilestoneRequirement(writer, milestone);
		}
		for (int k = 0; k < nativeList.Length; k++)
		{
			BindDevTreeNodeRequirement(writer, nativeList[k]);
		}
		for (int l = 0; l < nativeList2.Length; l++)
		{
			BindUnlockRequirement(writer, nativeList2[l]);
		}
		writer.ArrayEnd();
		nativeList.Dispose();
		nativeList2.Dispose();
	}

	private void GetRequirements(Entity prefabEntity, out Entity milestone, NativeParallelHashMap<Entity, UnlockFlags> devTreeNodes, NativeParallelHashMap<Entity, UnlockFlags> unlockRequirements)
	{
		NativeParallelHashMap<Entity, UnlockFlags> requiredPrefabs = new NativeParallelHashMap<Entity, UnlockFlags>(10, Allocator.TempJob);
		ProgressionUtils.CollectSubRequirements(base.EntityManager, prefabEntity, requiredPrefabs);
		milestone = Entity.Null;
		int num = -1;
		devTreeNodes.Clear();
		unlockRequirements.Clear();
		foreach (KeyValue<Entity, UnlockFlags> item in requiredPrefabs)
		{
			if (base.EntityManager.TryGetComponent<MilestoneData>(item.Key, out var component) && component.m_Index > num)
			{
				milestone = item.Key;
				num = component.m_Index;
			}
			if (base.EntityManager.HasComponent<DevTreeNodeData>(item.Key))
			{
				if (devTreeNodes.ContainsKey(item.Key))
				{
					devTreeNodes[item.Key] |= item.Value;
				}
				else
				{
					devTreeNodes.Add(item.Key, item.Value);
				}
			}
			if (base.EntityManager.HasComponent<UnlockRequirementData>(item.Key))
			{
				if (unlockRequirements.ContainsKey(item.Key))
				{
					unlockRequirements[item.Key] |= item.Value;
				}
				else
				{
					unlockRequirements.Add(item.Key, item.Value);
				}
			}
			if ((base.EntityManager.HasComponent<TutorialData>(item.Key) || base.EntityManager.HasComponent<TutorialPhaseData>(item.Key) || base.EntityManager.HasComponent<TutorialTriggerData>(item.Key) || base.EntityManager.HasComponent<TutorialListData>(item.Key)) && base.EntityManager.HasEnabledComponent<Locked>(item.Key))
			{
				if (unlockRequirements.ContainsKey(m_TutorialRequirementEntity))
				{
					unlockRequirements[m_TutorialRequirementEntity] |= item.Value;
				}
				else
				{
					unlockRequirements.Add(m_TutorialRequirementEntity, item.Value);
				}
			}
		}
		requiredPrefabs.Dispose();
	}

	private void BindMilestoneRequirement(IJsonWriter binder, Entity entity)
	{
		MilestoneData componentData = base.EntityManager.GetComponentData<MilestoneData>(entity);
		bool value = base.EntityManager.HasEnabledComponent<Locked>(entity);
		binder.TypeBegin("prefabs.MilestoneRequirement");
		binder.PropertyName("entity");
		binder.Write(entity);
		binder.PropertyName("index");
		binder.Write(componentData.m_Index);
		binder.PropertyName("locked");
		binder.Write(value);
		binder.TypeEnd();
	}

	private void BindDevTreeNodeRequirement(IJsonWriter binder, Entity entity)
	{
		DevTreeNodePrefab prefab = m_PrefabSystem.GetPrefab<DevTreeNodePrefab>(entity);
		bool value = base.EntityManager.HasEnabledComponent<Locked>(entity);
		binder.TypeBegin("prefabs.DevTreeNodeRequirement");
		binder.PropertyName("entity");
		binder.Write(entity);
		binder.PropertyName("name");
		binder.Write(prefab.name);
		binder.PropertyName("locked");
		binder.Write(value);
		binder.TypeEnd();
	}

	private void BindUnlockRequirement(IJsonWriter binder, Entity entity)
	{
		if (entity == m_TutorialRequirementEntity)
		{
			BindTutorialRequirement(binder, entity);
			return;
		}
		PrefabBase prefab = m_PrefabSystem.GetPrefab<PrefabBase>(entity);
		if (prefab is StrictObjectBuiltRequirementPrefab prefab2)
		{
			BindObjectBuiltRequirement(binder, entity, prefab2);
		}
		else if (prefab is ZoneBuiltRequirementPrefab prefab3)
		{
			BindZoneBuiltRequirement(binder, entity, prefab3);
		}
		else if (prefab is CitizenRequirementPrefab cr)
		{
			BindCitizenRequirement(binder, entity, cr);
		}
		else if (prefab is ProcessingRequirementPrefab prefab4)
		{
			BindProcessingRequirement(binder, entity, prefab4);
		}
		else if (prefab is TransportRequirementPrefab prefab5)
		{
			BindTransportRequirement(binder, entity, prefab5);
		}
		else if (prefab is ObjectBuiltRequirementPrefab prefab6)
		{
			BindOnBuildRequirement(binder, entity, prefab6);
		}
		else if (prefab is PrefabUnlockedRequirementPrefab prefab7)
		{
			BindPrefabUnlockedRequirement(binder, entity, prefab7);
		}
		else if (prefab is UnlockRequirementPrefab prefab8)
		{
			BindUnknownUnlockRequirement(binder, entity, prefab8);
		}
	}

	private void BindTutorialRequirement(IJsonWriter binder, Entity entity)
	{
		binder.TypeBegin("prefabs.TutorialRequirement");
		binder.PropertyName("entity");
		binder.Write(entity);
		binder.PropertyName("locked");
		binder.Write(value: true);
		binder.TypeEnd();
	}

	private void BindObjectBuiltRequirement(IJsonWriter binder, Entity entity, StrictObjectBuiltRequirementPrefab prefab)
	{
		binder.TypeBegin("prefabs.StrictObjectBuiltRequirement");
		BindUnlockRequirementProperties(binder, entity, prefab);
		binder.PropertyName("icon");
		binder.Write(ImageSystem.GetThumbnail(prefab.m_Requirement) ?? m_ImageSystem.placeholderIcon);
		binder.PropertyName("requirement");
		binder.Write(prefab.m_Requirement.name);
		binder.PropertyName("minimumCount");
		binder.Write(prefab.m_MinimumCount);
		binder.PropertyName("isUpgrade");
		binder.Write(prefab.m_Requirement.Has<Game.Prefabs.ServiceUpgrade>());
		binder.TypeEnd();
	}

	private void BindZoneBuiltRequirement(IJsonWriter binder, Entity entity, ZoneBuiltRequirementPrefab prefab)
	{
		binder.TypeBegin("prefabs.ZoneBuiltRequirement");
		BindUnlockRequirementProperties(binder, entity, prefab);
		binder.PropertyName("icon");
		bool flag = m_PrefabSystem.HasComponent<UIObjectData>(prefab.m_RequiredZone);
		binder.Write(ImageSystem.GetIcon((PrefabBase)(flag ? (((object)prefab.m_RequiredZone) ?? ((object)prefab)) : prefab)) ?? m_ImageSystem.placeholderIcon);
		binder.PropertyName("requiredTheme");
		binder.Write(prefab.m_RequiredTheme?.name);
		binder.PropertyName("requiredZone");
		binder.Write(prefab.m_RequiredZone?.name);
		binder.PropertyName("requiredType");
		binder.Write((int)prefab.m_RequiredType);
		binder.PropertyName("minimumSquares");
		binder.Write(prefab.m_MinimumSquares);
		binder.PropertyName("minimumCount");
		binder.Write(prefab.m_MinimumCount);
		binder.PropertyName("minimumLevel");
		binder.Write(prefab.m_MinimumLevel);
		binder.TypeEnd();
	}

	private void BindCitizenRequirement(IJsonWriter binder, Entity entity, CitizenRequirementPrefab cr)
	{
		binder.TypeBegin("prefabs.CitizenRequirement");
		BindUnlockRequirementProperties(binder, entity, cr);
		binder.PropertyName("minimumPopulation");
		binder.Write(cr.m_MinimumPopulation);
		binder.PropertyName("minimumHappiness");
		binder.Write(cr.m_MinimumHappiness);
		binder.TypeEnd();
	}

	private void BindProcessingRequirement(IJsonWriter binder, Entity entity, ProcessingRequirementPrefab prefab)
	{
		binder.TypeBegin("prefabs.ProcessingRequirement");
		BindUnlockRequirementProperties(binder, entity, prefab);
		binder.PropertyName("icon");
		binder.Write(ImageSystem.GetIcon(prefab) ?? m_ImageSystem.placeholderIcon);
		binder.PropertyName("resourceType");
		binder.Write(prefab.m_ResourceType.ToString());
		binder.PropertyName("minimumProducedAmount");
		binder.Write(prefab.m_MinimumProducedAmount);
		binder.TypeEnd();
	}

	private void BindTransportRequirement(IJsonWriter binder, Entity entity, TransportRequirementPrefab prefab)
	{
		binder.TypeBegin("prefabs.TransportRequirement");
		BindUnlockRequirementProperties(binder, entity, prefab);
		binder.PropertyName("icon");
		binder.Write(ImageSystem.GetIcon(prefab) ?? m_ImageSystem.placeholderIcon);
		binder.PropertyName("name");
		binder.Write((prefab.m_BuildingPrefab != null) ? prefab.m_BuildingPrefab.name : null);
		binder.PropertyName("filterID");
		binder.Write(prefab.m_FilterID);
		binder.PropertyName("transportType");
		binder.Write(Enum.GetName(typeof(TransportType), prefab.m_TransportType));
		binder.PropertyName("minimumTransportedCargo");
		binder.Write(prefab.m_MinimumTransportedCargo);
		binder.PropertyName("minimumTransportedPassenger");
		binder.Write(prefab.m_MinimumTransportedPassenger);
		binder.TypeEnd();
	}

	private void BindOnBuildRequirement(IJsonWriter binder, Entity entity, ObjectBuiltRequirementPrefab prefab)
	{
		binder.TypeBegin("prefabs.ObjectBuiltRequirement");
		BindUnlockRequirementProperties(binder, entity, prefab);
		binder.PropertyName("name");
		binder.Write(prefab.name);
		binder.PropertyName("minimumCount");
		binder.Write(prefab.m_MinimumCount);
		binder.TypeEnd();
	}

	private void BindPrefabUnlockedRequirement(IJsonWriter binder, Entity entity, PrefabUnlockedRequirementPrefab prefab)
	{
		binder.TypeBegin("prefabs.PrefabUnlockedRequirement");
		BindUnlockRequirementProperties(binder, entity, prefab);
		binder.TypeEnd();
	}

	private void BindUnknownUnlockRequirement(IJsonWriter binder, Entity entity, UnlockRequirementPrefab prefab)
	{
		binder.TypeBegin("prefabs.UnlockRequirement");
		BindUnlockRequirementProperties(binder, entity, prefab);
		binder.TypeEnd();
	}

	private void BindUnlockRequirementProperties(IJsonWriter binder, Entity entity, UnlockRequirementPrefab prefab)
	{
		UnlockRequirementData componentData = base.EntityManager.GetComponentData<UnlockRequirementData>(entity);
		bool value = base.EntityManager.HasEnabledComponent<Locked>(entity);
		binder.PropertyName("entity");
		binder.Write(entity);
		binder.PropertyName("labelId");
		binder.Write((!string.IsNullOrEmpty(prefab.m_LabelID)) ? prefab.m_LabelID : null);
		binder.PropertyName("progress");
		binder.Write(componentData.m_Progress);
		binder.PropertyName("locked");
		binder.Write(value);
	}

	[Preserve]
	public PrefabUISystem()
	{
	}
}
