using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class StatisticsUISystem : UISystemBase
{
	public struct StatCategory : IComparable<StatCategory>
	{
		public Entity m_Entity;

		public PrefabData m_PrefabData;

		public UIObjectData m_ObjectData;

		public StatCategory(Entity entity, UIObjectData objectData, PrefabData prefabData)
		{
			m_Entity = entity;
			m_PrefabData = prefabData;
			m_ObjectData = objectData;
		}

		public int CompareTo(StatCategory other)
		{
			return m_ObjectData.m_Priority.CompareTo(other.m_ObjectData.m_Priority);
		}
	}

	public struct DataPoint : IJsonWritable
	{
		public long x;

		public long y;

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("x");
			writer.Write(x);
			writer.PropertyName("y");
			writer.Write(y);
			writer.TypeEnd();
		}
	}

	public struct StatItem : IJsonReadable, IJsonWritable, IComparable<StatItem>
	{
		public Entity category;

		public Entity group;

		public Entity entity;

		public int statisticType;

		public int unitType;

		public int parameterIndex;

		public string key;

		public Color color;

		public bool locked;

		public bool isGroup;

		public bool isSubgroup;

		public bool stacked;

		public int priority;

		public StatItem(int priority, Entity category, Entity group, Entity entity, int statisticType, StatisticUnitType unitType, int parameterIndex, string key, Color color, bool locked, bool isGroup = false, bool isSubgroup = false, bool stacked = true)
		{
			this.category = category;
			this.group = group;
			this.entity = entity;
			this.statisticType = statisticType;
			this.unitType = (int)unitType;
			this.parameterIndex = parameterIndex;
			this.key = key;
			this.color = color;
			this.locked = locked;
			this.isGroup = isGroup;
			this.isSubgroup = isSubgroup;
			this.stacked = stacked;
			this.priority = priority;
		}

		public void Read(IJsonReader reader)
		{
			reader.ReadMapBegin();
			reader.ReadProperty("category");
			reader.Read(out category);
			reader.ReadProperty("group");
			reader.Read(out group);
			reader.ReadProperty("entity");
			reader.Read(out entity);
			reader.ReadProperty("statisticType");
			reader.Read(out statisticType);
			reader.ReadProperty("unitType");
			reader.Read(out unitType);
			reader.ReadProperty("parameterIndex");
			reader.Read(out parameterIndex);
			reader.ReadProperty("key");
			reader.Read(out key);
			reader.ReadProperty("color");
			reader.Read(out color);
			reader.ReadProperty("locked");
			reader.Read(out locked);
			reader.ReadProperty("isGroup");
			reader.Read(out isGroup);
			reader.ReadProperty("isSubgroup");
			reader.Read(out isSubgroup);
			reader.ReadProperty("stacked");
			reader.Read(out stacked);
			reader.ReadProperty("priority");
			reader.Read(out priority);
			reader.ReadMapEnd();
		}

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin("statistics.StatItem");
			writer.PropertyName("category");
			writer.Write(category);
			writer.PropertyName("group");
			writer.Write(group);
			writer.PropertyName("entity");
			writer.Write(entity);
			writer.PropertyName("statisticType");
			writer.Write(statisticType);
			writer.PropertyName("unitType");
			writer.Write(unitType);
			writer.PropertyName("parameterIndex");
			writer.Write(parameterIndex);
			writer.PropertyName("key");
			writer.Write(key);
			writer.PropertyName("color");
			writer.Write(color);
			writer.PropertyName("locked");
			writer.Write(locked);
			writer.PropertyName("isGroup");
			writer.Write(isGroup);
			writer.PropertyName("isSubgroup");
			writer.Write(isSubgroup);
			writer.PropertyName("stacked");
			writer.Write(stacked);
			writer.PropertyName("priority");
			writer.Write(priority);
			writer.TypeEnd();
		}

		public int CompareTo(StatItem other)
		{
			if (isSubgroup != other.isSubgroup)
			{
				return isGroup.CompareTo(other.isGroup);
			}
			if (entity != other.entity)
			{
				return entity.CompareTo(other.entity);
			}
			return priority.CompareTo(other.priority);
		}
	}

	private const string kGroup = "statistics";

	private PrefabUISystem m_PrefabUISystem;

	private PrefabSystem m_PrefabSystem;

	private ResourceSystem m_ResourceSystem;

	private ICityStatisticsSystem m_CityStatisticsSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private GameModeGovernmentSubsidiesSystem m_GameModeGovernmentSubsidiesSystem;

	private MapTilePurchaseSystem m_MapTilePurchaseSystem;

	private TimeUISystem m_TimeUISystem;

	private EntityQuery m_StatisticsCategoryQuery;

	private EntityQuery m_TimeDataQuery;

	private EntityQuery m_UnlockedPrefabQuery;

	private EntityQuery m_LinePrefabQuery;

	private List<StatItem> m_GroupCache;

	private List<StatItem> m_SubGroupCache;

	private List<StatItem> m_SelectedStatistics;

	private List<StatItem> m_SelectedStatisticsTracker;

	private Entity m_ActiveCategory;

	private Entity m_ActiveGroup;

	private int m_SampleRange;

	private bool m_Stacked;

	private RawMapBinding<Entity> m_GroupsMapBinding;

	private ValueBinding<int> m_SampleRangeBinding;

	private ValueBinding<int> m_SampleCountBinding;

	private GetterValueBinding<Entity> m_ActiveGroupBinding;

	private GetterValueBinding<Entity> m_ActiveCategoryBinding;

	private GetterValueBinding<bool> m_StackedBinding;

	private RawValueBinding m_SelectedStatisticsBinding;

	private RawValueBinding m_CategoriesBinding;

	private RawValueBinding m_DataBinding;

	private RawMapBinding<Entity> m_UnlockingRequirementsBinding;

	private bool m_ClearActive = true;

	private int m_UnlockRequirementVersion;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_StatisticsCategoryQuery = GetEntityQuery(ComponentType.ReadOnly<UIObjectData>(), ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<UIStatisticsCategoryData>());
		m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<TimeData>());
		m_UnlockedPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<Unlock>());
		m_LinePrefabQuery = GetEntityQuery(ComponentType.ReadOnly<TransportLineData>());
		m_GroupCache = new List<StatItem>();
		m_SubGroupCache = new List<StatItem>();
		m_SelectedStatistics = new List<StatItem>();
		m_SelectedStatisticsTracker = new List<StatItem>();
		m_PrefabUISystem = base.World.GetOrCreateSystemManaged<PrefabUISystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		ICityStatisticsSystem cityStatisticsSystem = m_CityStatisticsSystem;
		cityStatisticsSystem.eventStatisticsUpdated = (Action)Delegate.Combine(cityStatisticsSystem.eventStatisticsUpdated, new Action(OnStatisticsUpdated));
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_TimeUISystem = base.World.GetOrCreateSystemManaged<TimeUISystem>();
		m_MapTilePurchaseSystem = base.World.GetOrCreateSystemManaged<MapTilePurchaseSystem>();
		m_GameModeGovernmentSubsidiesSystem = base.World.GetOrCreateSystemManaged<GameModeGovernmentSubsidiesSystem>();
		AddBinding(m_GroupsMapBinding = new RawMapBinding<Entity>("statistics", "groups", BindGroups));
		AddBinding(m_SampleRangeBinding = new ValueBinding<int>("statistics", "sampleRange", m_SampleRange));
		AddBinding(m_SampleCountBinding = new ValueBinding<int>("statistics", "sampleCount", m_CityStatisticsSystem.sampleCount));
		AddBinding(m_ActiveGroupBinding = new GetterValueBinding<Entity>("statistics", "activeGroup", () => m_ActiveGroup));
		AddBinding(m_ActiveCategoryBinding = new GetterValueBinding<Entity>("statistics", "activeCategory", () => m_ActiveCategory));
		AddBinding(m_StackedBinding = new GetterValueBinding<bool>("statistics", "stacked", () => m_Stacked));
		AddBinding(m_CategoriesBinding = new RawValueBinding("statistics", "categories", BindCategories));
		AddBinding(m_DataBinding = new RawValueBinding("statistics", "data", BindData));
		AddBinding(m_SelectedStatisticsBinding = new RawValueBinding("statistics", "selectedStatistics", BindSelectedStatistics));
		AddBinding(m_UnlockingRequirementsBinding = new RawMapBinding<Entity>("statistics", "unlockingRequirements", BindUnlockingRequirements));
		AddBinding(new GetterValueBinding<int>("statistics", "updatesPerDay", () => 32));
		AddBinding(new TriggerBinding<StatItem>("statistics", "addStat", ProcessAddStat, new ValueReader<StatItem>()));
		AddBinding(new TriggerBinding<StatItem>("statistics", "addStatChildren", ProcessAddStatChildren, new ValueReader<StatItem>()));
		AddBinding(new TriggerBinding<StatItem>("statistics", "removeStat", DeepRemoveStat, new ValueReader<StatItem>()));
		AddBinding(new TriggerBinding("statistics", "clearStats", ClearStats));
		AddBinding(new TriggerBinding<int>("statistics", "setSampleRange", SetSampleRange));
	}

	private void BindUnlockingRequirements(IJsonWriter writer, Entity prefabEntity)
	{
		m_PrefabUISystem.BindPrefabRequirements(writer, prefabEntity);
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_SelectedStatistics.Clear();
		m_SampleRange = 32;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_SampleCountBinding.Update(m_CityStatisticsSystem.sampleCount);
		m_SampleRangeBinding.Update(m_SampleRange);
		int componentOrderVersion = base.EntityManager.GetComponentOrderVersion<UnlockRequirementData>();
		if (PrefabUtils.HasUnlockedPrefab<UIObjectData>(base.EntityManager, m_UnlockedPrefabQuery) || m_UnlockRequirementVersion != componentOrderVersion)
		{
			m_UnlockingRequirementsBinding.UpdateAll();
			m_GroupsMapBinding.UpdateAll();
			m_CategoriesBinding.Update();
		}
		m_UnlockRequirementVersion = componentOrderVersion;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		ICityStatisticsSystem cityStatisticsSystem = m_CityStatisticsSystem;
		cityStatisticsSystem.eventStatisticsUpdated = (Action)Delegate.Remove(cityStatisticsSystem.eventStatisticsUpdated, new Action(OnStatisticsUpdated));
		base.OnDestroy();
	}

	private void OnStatisticsUpdated()
	{
		m_DataBinding.Update();
	}

	private void BindSelectedStatistics(IJsonWriter binder)
	{
		binder.ArrayBegin(m_SelectedStatistics.Count);
		for (int i = 0; i < m_SelectedStatistics.Count; i++)
		{
			StatItem value = m_SelectedStatistics[i];
			binder.Write(value);
		}
		binder.ArrayEnd();
	}

	private void BindCategories(IJsonWriter binder)
	{
		NativeList<StatCategory> sortedCategories = GetSortedCategories();
		binder.ArrayBegin(sortedCategories.Length);
		for (int i = 0; i < sortedCategories.Length; i++)
		{
			StatCategory statCategory = sortedCategories[i];
			PrefabBase prefab = m_PrefabSystem.GetPrefab<PrefabBase>(statCategory.m_PrefabData);
			bool value = base.EntityManager.HasEnabledComponent<Locked>(statCategory.m_Entity);
			binder.TypeBegin("statistics.StatCategory");
			binder.PropertyName("entity");
			binder.Write(statCategory.m_Entity);
			binder.PropertyName("key");
			binder.Write(prefab.name);
			binder.PropertyName("locked");
			binder.Write(value);
			binder.TypeEnd();
		}
		binder.ArrayEnd();
	}

	private NativeList<StatCategory> GetSortedCategories()
	{
		NativeArray<Entity> nativeArray = m_StatisticsCategoryQuery.ToEntityArray(Allocator.TempJob);
		NativeArray<UIObjectData> nativeArray2 = m_StatisticsCategoryQuery.ToComponentDataArray<UIObjectData>(Allocator.TempJob);
		NativeArray<PrefabData> nativeArray3 = m_StatisticsCategoryQuery.ToComponentDataArray<PrefabData>(Allocator.TempJob);
		NativeList<StatCategory> nativeList = new NativeList<StatCategory>(nativeArray.Length, Allocator.Temp);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			nativeList.Add(new StatCategory(nativeArray[i], nativeArray2[i], nativeArray3[i]));
		}
		nativeArray.Dispose();
		nativeArray2.Dispose();
		nativeArray3.Dispose();
		nativeList.Sort();
		return nativeList;
	}

	private void CacheChildren(Entity parentEntity, List<StatItem> cache)
	{
		cache.Clear();
		bool flag = base.EntityManager.HasComponent<UIStatisticsCategoryData>(parentEntity);
		StatisticsData component4;
		PrefabData component5;
		if (base.EntityManager.TryGetBuffer(parentEntity, isReadOnly: true, out DynamicBuffer<UIGroupElement> buffer))
		{
			NativeList<UIObjectInfo> sortedObjects = UIObjectInfo.GetSortedObjects(base.EntityManager, buffer, Allocator.TempJob);
			for (int i = 0; i < sortedObjects.Length; i++)
			{
				Entity category = Entity.Null;
				Entity entity = Entity.Null;
				Entity entity2 = sortedObjects[i].entity;
				PrefabBase prefab = m_PrefabSystem.GetPrefab<PrefabBase>(entity2);
				StatisticUnitType unitType = StatisticUnitType.None;
				StatisticType statisticType = StatisticType.Invalid;
				bool locked = base.EntityManager.HasEnabledComponent<Locked>(entity2);
				bool isSubgroup = (!flag && base.EntityManager.HasComponent<UIStatisticsGroupData>(entity2)) || (prefab is ParametricStatistic parametricStatistic && parametricStatistic.GetParameters().Count() > 1);
				bool stacked = true;
				Color color = new Color(0f, 0f, 0f, 0f);
				if (!m_MapTilePurchaseSystem.GetMapTileUpkeepEnabled() && prefab.name == "MapTileUpkeep")
				{
					continue;
				}
				if (base.EntityManager.TryGetComponent<StatisticsData>(entity2, out var component))
				{
					if ((m_CityConfigurationSystem.unlimitedMoney && (component.m_StatisticType == StatisticType.Money || prefab.name == "LoanInterest")) || (!m_GameModeGovernmentSubsidiesSystem.GetGovernmentSubsidiesEnabled() && component.m_StatisticType == StatisticType.Income && prefab.name == "GovernmentSubsidy"))
					{
						continue;
					}
					if (component.m_StatisticType == StatisticType.PassengerCountFerry)
					{
						NativeArray<Entity> lineDatas = m_LinePrefabQuery.ToEntityArray(Allocator.TempJob);
						bool num = !TransportUIUtils.ShouldBindTransportType(base.EntityManager, m_PrefabSystem, TransportType.Ferry, lineDatas);
						lineDatas.Dispose();
						if (num)
						{
							continue;
						}
					}
					unitType = component.m_UnitType;
					statisticType = component.m_StatisticType;
					entity = component.m_Group;
					category = component.m_Category;
					color = component.m_Color;
					stacked = component.m_Stacked;
				}
				if (base.EntityManager.TryGetComponent<UIStatisticsGroupData>(entity2, out var component2) && base.EntityManager.TryGetComponent<UIObjectData>(entity2, out var component3))
				{
					entity = ((component3.m_Group == component2.m_Category) ? entity2 : component3.m_Group);
					unitType = component2.m_UnitType;
					category = component2.m_Category;
					color = component2.m_Color;
					stacked = component2.m_Stacked;
				}
				cache.Add(new StatItem(i, category, entity, entity2, (int)statisticType, unitType, 0, prefab.name, color, locked, flag, isSubgroup, stacked));
			}
			sortedObjects.Dispose();
		}
		else if (base.EntityManager.TryGetComponent<StatisticsData>(parentEntity, out component4) && base.EntityManager.TryGetComponent<PrefabData>(parentEntity, out component5))
		{
			bool locked2 = base.EntityManager.HasEnabledComponent<Locked>(parentEntity);
			CacheParameterChildren(parentEntity, locked2, component4, component5, cache);
		}
	}

	private void CacheParameterChildren(Entity parent, bool locked, StatisticsData statisticsData, PrefabData prefabData, List<StatItem> cache)
	{
		ParametricStatistic prefab = m_PrefabSystem.GetPrefab<ParametricStatistic>(prefabData);
		if (base.EntityManager.TryGetBuffer(parent, isReadOnly: true, out DynamicBuffer<StatisticParameterData> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				cache.Add(new StatItem(i, statisticsData.m_Category, (statisticsData.m_Group == Entity.Null) ? parent : statisticsData.m_Group, parent, (int)prefab.m_StatisticsType, prefab.m_UnitType, i, prefab.name + prefab.GetParameterName(buffer[i].m_Value), buffer[i].m_Color, locked, isGroup: false, isSubgroup: false, statisticsData.m_Stacked));
			}
		}
	}

	private void BindGroups(IJsonWriter binder, Entity parent)
	{
		CacheChildren(parent, m_GroupCache);
		binder.ArrayBegin(m_GroupCache.Count);
		for (int i = 0; i < m_GroupCache.Count; i++)
		{
			binder.Write(m_GroupCache[i]);
		}
		binder.ArrayEnd();
	}

	private void BindData(IJsonWriter binder)
	{
		binder.ArrayBegin(m_SelectedStatistics.Count);
		for (int num = m_SelectedStatistics.Count - 1; num >= 0; num--)
		{
			BindData(binder, m_SelectedStatistics[num]);
		}
		binder.ArrayEnd();
	}

	private void BindData(IJsonWriter binder, StatItem stat)
	{
		binder.TypeBegin("statistics.ChartDataSets");
		binder.PropertyName("label");
		binder.Write(stat.key);
		binder.PropertyName("data");
		NativeList<DataPoint> statisticData = GetStatisticData(stat);
		binder.ArrayBegin(statisticData.Length);
		for (int i = 0; i < statisticData.Length; i++)
		{
			binder.Write(statisticData[i]);
		}
		binder.ArrayEnd();
		binder.PropertyName("borderColor");
		binder.Write(stat.color.ToHexCode());
		binder.PropertyName("backgroundColor");
		binder.Write($"rgba({Mathf.RoundToInt(stat.color.r * 255f)}, {Mathf.RoundToInt(stat.color.g * 255f)}, {Mathf.RoundToInt(stat.color.b * 255f)}, 0.5)");
		binder.PropertyName("fill");
		if (m_Stacked)
		{
			binder.Write("origin");
		}
		else
		{
			binder.Write("false");
		}
		binder.TypeEnd();
	}

	private NativeList<DataPoint> GetStatisticData(StatItem stat)
	{
		m_CityStatisticsSystem.CompleteWriters();
		StatisticsPrefab prefab = m_PrefabSystem.GetPrefab<StatisticsPrefab>(stat.entity);
		ResourcePrefabs prefabs = m_ResourceSystem.GetPrefabs();
		TimeData singleton = TimeData.GetSingleton(m_TimeDataQuery);
		int sampleCount = m_CityStatisticsSystem.sampleCount;
		int num = math.min(m_SampleRange + 1, sampleCount);
		if (sampleCount <= 1)
		{
			NativeList<DataPoint> result = new NativeList<DataPoint>(1, Allocator.Temp);
			DataPoint value = new DataPoint
			{
				x = singleton.m_FirstFrame,
				y = 0L
			};
			result.Add(in value);
			return result;
		}
		NativeArray<long> data = CollectionHelper.CreateNativeArray<long>(num, Allocator.Temp);
		StatisticParameterData[] array = ((prefab is ParametricStatistic parametricStatistic) ? parametricStatistic.GetParameters().ToArray() : new StatisticParameterData[1]
		{
			new StatisticParameterData
			{
				m_Value = 0
			}
		});
		if (stat.isSubgroup)
		{
			for (int i = 0; i < array.Length; i++)
			{
				int value2 = array[i].m_Value;
				NativeArray<long> statisticDataArrayLong = m_CityStatisticsSystem.GetStatisticDataArrayLong((StatisticType)stat.statisticType, value2);
				statisticDataArrayLong = EnsureDataSize(statisticDataArrayLong);
				for (int j = 0; j < num; j++)
				{
					long num2 = statisticDataArrayLong[statisticDataArrayLong.Length - num + j];
					if (stat.statisticType == 4 && prefab is ResourceStatistic resourceStatistic)
					{
						Resource resource = EconomyUtils.GetResource(resourceStatistic.m_Resources[i].m_Resource);
						Entity entity = prefabs[resource];
						ResourceData componentData = base.EntityManager.GetComponentData<ResourceData>(entity);
						num2 *= (int)EconomyUtils.GetMarketPrice(componentData);
					}
					data[j] += num2;
				}
			}
		}
		else
		{
			int value3 = array[stat.parameterIndex].m_Value;
			NativeArray<long> statisticDataArrayLong2 = m_CityStatisticsSystem.GetStatisticDataArrayLong((StatisticType)stat.statisticType, value3);
			NativeArray<long> nativeArray = CollectionHelper.CreateNativeArray<long>(0, Allocator.Temp);
			if (stat.statisticType == 16 || stat.statisticType == 15)
			{
				nativeArray = m_CityStatisticsSystem.GetStatisticDataArrayLong(StatisticType.Population);
				nativeArray = EnsureDataSize(nativeArray);
			}
			statisticDataArrayLong2 = EnsureDataSize(statisticDataArrayLong2);
			for (int k = 0; k < num; k++)
			{
				long num3 = statisticDataArrayLong2[statisticDataArrayLong2.Length - num + k];
				if (stat.statisticType == 4 && prefab is ResourceStatistic resourceStatistic2)
				{
					Resource resource2 = EconomyUtils.GetResource(resourceStatistic2.m_Resources[stat.parameterIndex].m_Resource);
					Entity entity2 = prefabs[resource2];
					ResourceData componentData2 = base.EntityManager.GetComponentData<ResourceData>(entity2);
					num3 *= (int)EconomyUtils.GetMarketPrice(componentData2);
				}
				if (nativeArray.Length > 0 && (stat.statisticType == 16 || stat.statisticType == 15))
				{
					long num4 = nativeArray[nativeArray.Length - num + k];
					if (num4 > 0)
					{
						num3 /= num4;
					}
				}
				data[k] += num3;
			}
		}
		return GetDataPoints(num, sampleCount, data, singleton);
	}

	private NativeArray<long> EnsureDataSize(NativeArray<long> data)
	{
		if (data.Length < m_CityStatisticsSystem.sampleCount)
		{
			NativeArray<long> result = CollectionHelper.CreateNativeArray<long>(m_CityStatisticsSystem.sampleCount, Allocator.Temp);
			int num = 0;
			for (int i = 0; i < result.Length; i++)
			{
				if (i < result.Length - data.Length)
				{
					result[i] = 0L;
				}
				else
				{
					result[i] = data[num++];
				}
			}
			return result;
		}
		return data;
	}

	private NativeList<DataPoint> GetDataPoints(int range, int samples, NativeArray<long> data, TimeData timeData)
	{
		int sampleInterval = GetSampleInterval(range);
		NativeList<DataPoint> result = new NativeList<DataPoint>(data.Length / sampleInterval, Allocator.Temp);
		int num = 0;
		uint num2 = (uint)math.max((int)(m_CityStatisticsSystem.GetSampleFrameIndex(samples - range) - timeData.m_FirstFrame), 0);
		DataPoint value = new DataPoint
		{
			x = (uint)math.max(num2, m_TimeUISystem.GetTicks() - 8192 * m_SampleRange),
			y = data[0]
		};
		result.Add(in value);
		if (data.Length > 2)
		{
			for (int i = 1; i < data.Length - 1; i++)
			{
				if (num % sampleInterval == 0)
				{
					uint sampleFrameIndex = m_CityStatisticsSystem.GetSampleFrameIndex(samples - range + i);
					value = new DataPoint
					{
						x = sampleFrameIndex - timeData.m_FirstFrame,
						y = data[i]
					};
					result.Add(in value);
				}
				num++;
			}
		}
		m_CityStatisticsSystem.GetSampleFrameIndex(samples);
		value = new DataPoint
		{
			x = (uint)(m_TimeUISystem.GetTicks() + 182 + 1)
		};
		value.y = data[data.Length - 1];
		result.Add(in value);
		return result;
	}

	private int GetSampleInterval(int range)
	{
		int num = 32;
		if (range <= num)
		{
			return 1;
		}
		int num2 = num - 2;
		return Math.Max(1, (range - 2) / num2);
	}

	private void CheckActiveCategory(StatItem stat)
	{
		if (stat.category != m_ActiveCategory)
		{
			m_SelectedStatistics.Clear();
			m_SelectedStatisticsTracker.Clear();
			m_ActiveCategory = stat.category;
			m_ActiveCategoryBinding.Update();
		}
	}

	private void CheckActiveGroup(StatItem stat)
	{
		if (m_ActiveGroup == Entity.Null || stat.isGroup || stat.group != m_ActiveGroup)
		{
			m_SelectedStatistics.Clear();
			m_SelectedStatisticsTracker.Clear();
			m_ActiveGroup = (stat.isGroup ? stat.entity : stat.group);
			m_ActiveGroupBinding.Update();
		}
	}

	private void ProcessAddStat(StatItem stat)
	{
		if (stat.locked)
		{
			return;
		}
		CheckActiveCategory(stat);
		CheckActiveGroup(stat);
		if (stat.isGroup)
		{
			AddStat(stat, onlyTracker: true);
			if (!TryAddChildren(stat, m_GroupCache))
			{
				m_SelectedStatistics.Add(stat);
			}
		}
		else if (stat.isSubgroup)
		{
			RemoveStatChildren(stat);
			AddStat(stat, onlyTracker: false);
		}
		else
		{
			RemoveStatParent(stat);
			AddStat(stat, onlyTracker: false);
		}
		UpdateStackedStatus();
		UpdateStats();
	}

	private void ProcessAddStatChildren(StatItem stat)
	{
		if (!stat.locked)
		{
			CheckActiveCategory(stat);
			CheckActiveGroup(stat);
			if (stat.isSubgroup)
			{
				RemoveStat(stat, keepTracker: true);
				TryAddChildren(stat, m_SubGroupCache);
			}
		}
	}

	private void UpdateStackedStatus()
	{
		if (m_SelectedStatisticsTracker.Count((StatItem stat) => stat.isSubgroup && stat.group == m_ActiveGroup) > 1 && base.EntityManager.TryGetComponent<UIStatisticsGroupData>(m_ActiveGroup, out var component))
		{
			m_Stacked = component.m_Stacked;
		}
		else if (m_SelectedStatisticsTracker.Count > 0)
		{
			m_Stacked = false;
			for (int num = 0; num < m_SelectedStatisticsTracker.Count; num++)
			{
				if (m_SelectedStatisticsTracker[num].stacked)
				{
					m_Stacked = true;
					break;
				}
			}
		}
		else
		{
			m_Stacked = false;
		}
		m_StackedBinding.Update();
	}

	private void AddStat(StatItem stat, bool onlyTracker)
	{
		if (!m_SelectedStatisticsTracker.Contains(stat))
		{
			m_SelectedStatisticsTracker.Add(stat);
		}
		if (!onlyTracker && !m_SelectedStatistics.Contains(stat))
		{
			m_SelectedStatistics.Add(stat);
		}
	}

	private bool TryAddChildren(StatItem stat, List<StatItem> cache)
	{
		CacheChildren(stat.entity, cache);
		for (int i = 0; i < cache.Count; i++)
		{
			ProcessAddStat(cache[i]);
		}
		return cache.Count > 0;
	}

	private void DeepRemoveStat(StatItem stat)
	{
		if (!m_SelectedStatisticsTracker.Contains(stat))
		{
			int num = m_SelectedStatisticsTracker.FindIndex((StatItem s) => s.entity == stat.group);
			int num2 = m_SelectedStatisticsTracker.FindIndex((StatItem s) => s.entity == stat.entity && s.isSubgroup);
			if (num >= 0)
			{
				StatItem stat2 = m_SelectedStatisticsTracker[num];
				if (num2 >= 0)
				{
					StatItem stat3 = m_SelectedStatisticsTracker[num2];
					DeepRemoveStat(stat2);
					ProcessAddStat(stat3);
				}
				else
				{
					DeepRemoveStat(stat2);
				}
			}
		}
		int num3 = m_SelectedStatisticsTracker.Count((StatItem s) => s.isSubgroup);
		RemoveStat(stat, keepTracker: false);
		RemoveStatChildren(stat);
		int num4 = m_SelectedStatisticsTracker.Count((StatItem s) => s.isSubgroup);
		if (num3 > 1 && num4 == 1)
		{
			StatItem stat4 = m_SelectedStatisticsTracker.First((StatItem s) => s.isSubgroup);
			RemoveStat(stat4, keepTracker: false);
			ProcessAddStat(stat4);
		}
		if (m_ClearActive && m_SelectedStatistics.Count == 0 && m_SelectedStatisticsTracker.Count <= 1)
		{
			ClearStats();
		}
		else
		{
			UpdateStats();
		}
		m_ClearActive = true;
		UpdateStackedStatus();
	}

	private void RemoveStatParent(StatItem stat)
	{
		int num = m_SelectedStatisticsTracker.FindIndex((StatItem s) => s.entity == stat.entity && s.isSubgroup);
		if (num != -1)
		{
			StatItem stat2 = m_SelectedStatisticsTracker[num];
			RemoveStat(stat2, keepTracker: true);
		}
	}

	private void RemoveStatChildren(StatItem stat)
	{
		if (stat.isGroup)
		{
			for (int num = m_SelectedStatistics.Count - 1; num >= 0; num--)
			{
				if (m_SelectedStatistics[num].group == stat.entity)
				{
					m_SelectedStatistics.RemoveAt(num);
				}
			}
			for (int num2 = m_SelectedStatisticsTracker.Count - 1; num2 >= 0; num2--)
			{
				if (m_SelectedStatisticsTracker[num2].group == stat.entity)
				{
					m_SelectedStatisticsTracker.RemoveAt(num2);
				}
			}
		}
		else
		{
			if (!stat.isSubgroup)
			{
				return;
			}
			for (int num3 = m_SelectedStatistics.Count - 1; num3 >= 0; num3--)
			{
				if (m_SelectedStatistics[num3].entity == stat.entity)
				{
					m_SelectedStatistics.RemoveAt(num3);
				}
			}
			for (int num4 = m_SelectedStatisticsTracker.Count - 1; num4 >= 0; num4--)
			{
				if (m_SelectedStatisticsTracker[num4].entity == stat.entity)
				{
					m_SelectedStatisticsTracker.RemoveAt(num4);
				}
			}
		}
	}

	private void RemoveStat(StatItem stat, bool keepTracker)
	{
		m_SelectedStatistics.Remove(stat);
		if (!keepTracker)
		{
			m_SelectedStatisticsTracker.Remove(stat);
		}
	}

	private void ClearStats()
	{
		m_SelectedStatistics.Clear();
		m_SelectedStatisticsTracker.Clear();
		UpdateStats();
		ClearActive();
	}

	private void UpdateStats()
	{
		m_SelectedStatistics.Sort();
		m_SelectedStatisticsBinding.Update();
		m_DataBinding.Update();
	}

	private void ClearActive()
	{
		m_ActiveGroup = Entity.Null;
		m_ActiveGroupBinding.Update();
		m_ActiveCategory = Entity.Null;
		m_ActiveCategoryBinding.Update();
	}

	private void SetSampleRange(int range)
	{
		m_SampleRange = range;
		m_SampleRangeBinding.Update(m_SampleRange);
		UpdateStats();
	}

	[Preserve]
	public StatisticsUISystem()
	{
	}
}
