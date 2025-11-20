using System;
using System.Linq;
using Colossal.Serialization.Entities;
using Colossal.UI.Binding;
using Game.Common;
using Game.Prefabs;
using Game.Routes;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

public class TransportInfoviewUISystem : InfoviewUISystemBase
{
	public readonly struct PassengerSummary
	{
		private readonly Entity m_Prefab;

		public string id { get; }

		public string icon { get; }

		public bool locked { get; }

		public int lineCount { get; }

		public int touristCount { get; }

		public int citizenCount { get; }

		public PassengerSummary(Entity prefab, string id, string icon, bool locked, int lineCount, int touristCount, int citizenCount)
		{
			m_Prefab = prefab;
			this.id = id;
			this.icon = icon;
			this.locked = locked;
			this.lineCount = lineCount;
			this.touristCount = touristCount;
			this.citizenCount = citizenCount;
		}

		public void Write(PrefabUISystem prefabUISystem, IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("id");
			writer.Write(id);
			writer.PropertyName("icon");
			writer.Write(icon);
			writer.PropertyName("locked");
			writer.Write(locked);
			writer.PropertyName("lineCount");
			writer.Write(lineCount);
			writer.PropertyName("touristCount");
			writer.Write(touristCount);
			writer.PropertyName("citizenCount");
			writer.Write(citizenCount);
			writer.PropertyName("requirements");
			prefabUISystem.BindPrefabRequirements(writer, m_Prefab);
			writer.TypeEnd();
		}
	}

	public readonly struct CargoSummary
	{
		private readonly Entity m_Prefab;

		public string id { get; }

		public string icon { get; }

		public bool locked { get; }

		public int lineCount { get; }

		public int cargoCount { get; }

		public CargoSummary(Entity prefab, string id, string icon, bool locked, int lineCount, int cargoCount)
		{
			m_Prefab = prefab;
			this.id = id;
			this.icon = icon;
			this.locked = locked;
			this.lineCount = lineCount;
			this.cargoCount = cargoCount;
		}

		public void Write(PrefabUISystem prefabUISystem, IJsonWriter writer)
		{
			writer.TypeBegin(GetType().FullName);
			writer.PropertyName("id");
			writer.Write(id);
			writer.PropertyName("icon");
			writer.Write(icon);
			writer.PropertyName("locked");
			writer.Write(locked);
			writer.PropertyName("lineCount");
			writer.Write(lineCount);
			writer.PropertyName("cargoCount");
			writer.Write(cargoCount);
			writer.PropertyName("requirements");
			prefabUISystem.BindPrefabRequirements(writer, m_Prefab);
			writer.TypeEnd();
		}
	}

	private const string kGroup = "transportInfo";

	private UnlockSystem m_UnlockSystem;

	private PrefabSystem m_PrefabSystem;

	private PrefabUISystem m_PrefabUISystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private EntityQuery m_ConfigQuery;

	private EntityQuery m_LineQuery;

	private EntityQuery m_ModifiedLineQuery;

	private EntityQuery m_LinePrefabQuery;

	private RawValueBinding m_Summaries;

	private UITransportConfigurationPrefab m_Config;

	protected override bool Active
	{
		get
		{
			if (!base.Active)
			{
				return m_Summaries.active;
			}
			return true;
		}
	}

	protected override bool Modified => !m_ModifiedLineQuery.IsEmptyIgnoreFilter;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UnlockSystem = base.World.GetOrCreateSystemManaged<UnlockSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_PrefabUISystem = base.World.GetOrCreateSystemManaged<PrefabUISystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_ConfigQuery = GetEntityQuery(ComponentType.ReadOnly<UITransportConfigurationData>());
		m_LineQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[4]
			{
				ComponentType.ReadOnly<Route>(),
				ComponentType.ReadWrite<TransportLine>(),
				ComponentType.ReadOnly<RouteWaypoint>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_LinePrefabQuery = GetEntityQuery(ComponentType.ReadOnly<TransportLineData>());
		m_ModifiedLineQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[4]
			{
				ComponentType.ReadOnly<Route>(),
				ComponentType.ReadWrite<TransportLine>(),
				ComponentType.ReadOnly<RouteWaypoint>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Updated>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		AddBinding(m_Summaries = new RawValueBinding("transportInfo", "summaries", BindSummaries));
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		if (base.Enabled)
		{
			m_Config = m_PrefabSystem.GetSingletonPrefab<UITransportConfigurationPrefab>(m_ConfigQuery);
		}
	}

	protected override void PerformUpdate()
	{
		m_Summaries.Update();
	}

	private void BindSummaries(IJsonWriter writer)
	{
		NativeArray<UITransportLineData> sortedLines = TransportUIUtils.GetSortedLines(m_LineQuery, base.EntityManager, m_PrefabSystem);
		NativeArray<Entity> lineDatas = m_LinePrefabQuery.ToEntityArray(Allocator.Temp);
		int size = m_Config.m_PassengerSummaryItems.Count((UITransportSummaryItem item) => TransportUIUtils.ShouldBindTransportType(base.EntityManager, m_PrefabSystem, item.m_Type, lineDatas));
		writer.TypeBegin(GetType().FullName + "+TransportSummaries");
		writer.PropertyName("passengerSummaries");
		writer.ArrayBegin(size);
		UITransportSummaryItem[] passengerSummaryItems = m_Config.m_PassengerSummaryItems;
		foreach (UITransportSummaryItem uITransportSummaryItem in passengerSummaryItems)
		{
			if (TransportUIUtils.ShouldBindTransportType(base.EntityManager, m_PrefabSystem, uITransportSummaryItem.m_Type, lineDatas))
			{
				new PassengerSummary(m_PrefabSystem.GetEntity(uITransportSummaryItem.m_Unlockable), Enum.GetName(typeof(TransportType), uITransportSummaryItem.m_Type), uITransportSummaryItem.m_Icon, m_UnlockSystem.IsLocked(uITransportSummaryItem.m_Unlockable), uITransportSummaryItem.m_ShowLines ? TransportUIUtils.CountLines(sortedLines, uITransportSummaryItem.m_Type) : 0, m_CityStatisticsSystem.GetStatisticValue(uITransportSummaryItem.m_Statistic, 1), m_CityStatisticsSystem.GetStatisticValue(uITransportSummaryItem.m_Statistic)).Write(m_PrefabUISystem, writer);
			}
		}
		writer.ArrayEnd();
		writer.PropertyName("cargoSummaries");
		writer.ArrayBegin(m_Config.m_CargoSummaryItems.Length);
		passengerSummaryItems = m_Config.m_CargoSummaryItems;
		foreach (UITransportSummaryItem uITransportSummaryItem2 in passengerSummaryItems)
		{
			new CargoSummary(m_PrefabSystem.GetEntity(uITransportSummaryItem2.m_Unlockable), Enum.GetName(typeof(TransportType), uITransportSummaryItem2.m_Type), uITransportSummaryItem2.m_Icon, m_UnlockSystem.IsLocked(uITransportSummaryItem2.m_Unlockable), uITransportSummaryItem2.m_ShowLines ? TransportUIUtils.CountLines(sortedLines, uITransportSummaryItem2.m_Type, cargo: true) : 0, m_CityStatisticsSystem.GetStatisticValue(uITransportSummaryItem2.m_Statistic)).Write(m_PrefabUISystem, writer);
		}
		writer.ArrayEnd();
		writer.TypeEnd();
		sortedLines.Dispose();
		lineDatas.Dispose();
	}

	[Preserve]
	public TransportInfoviewUISystem()
	{
	}
}
