using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Prefabs;
using Game.Simulation;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class WaterInfoviewUISystem : InfoviewUISystemBase
{
	private const string kGroup = "waterInfo";

	private WaterStatisticsSystem m_WaterStatisticsSystem;

	private GetterValueBinding<int> m_WaterCapacity;

	private WaterTradeSystem m_WaterTradeSystem;

	private EntityQuery m_OutsideTradeParameterGroup;

	private GetterValueBinding<int> m_WaterConsumption;

	private GetterValueBinding<int> m_SewageCapacity;

	private GetterValueBinding<int> m_SewageConsumption;

	private GetterValueBinding<int> m_WaterExport;

	private GetterValueBinding<IndicatorValue> m_WaterAvailability;

	private GetterValueBinding<int> m_WaterImport;

	private GetterValueBinding<IndicatorValue> m_SewageAvailability;

	private GetterValueBinding<int> m_SewageExport;

	private GetterValueBinding<IndicatorValue> m_WaterTrade;

	protected override bool Active
	{
		get
		{
			if (!base.Active && !m_WaterCapacity.active && !m_WaterConsumption.active && !m_SewageCapacity.active && !m_SewageConsumption.active && !m_WaterExport.active && !m_WaterImport.active && !m_SewageExport.active && !m_WaterAvailability.active && !m_SewageAvailability.active)
			{
				return m_WaterTrade.active;
			}
			return true;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_WaterStatisticsSystem = base.World.GetOrCreateSystemManaged<WaterStatisticsSystem>();
		m_WaterTradeSystem = base.World.GetOrCreateSystemManaged<WaterTradeSystem>();
		m_OutsideTradeParameterGroup = GetEntityQuery(ComponentType.ReadOnly<OutsideTradeParameterData>());
		AddBinding(m_WaterCapacity = new GetterValueBinding<int>("waterInfo", "waterCapacity", () => m_WaterStatisticsSystem.freshCapacity));
		AddBinding(m_WaterConsumption = new GetterValueBinding<int>("waterInfo", "waterConsumption", () => m_WaterStatisticsSystem.freshConsumption));
		AddBinding(m_SewageCapacity = new GetterValueBinding<int>("waterInfo", "sewageCapacity", () => m_WaterStatisticsSystem.sewageCapacity));
		AddBinding(m_SewageConsumption = new GetterValueBinding<int>("waterInfo", "sewageConsumption", () => m_WaterStatisticsSystem.sewageConsumption));
		AddBinding(m_WaterExport = new GetterValueBinding<int>("waterInfo", "waterExport", () => m_WaterTradeSystem.freshExport));
		AddBinding(m_WaterImport = new GetterValueBinding<int>("waterInfo", "waterImport", () => m_WaterTradeSystem.freshImport));
		AddBinding(m_SewageExport = new GetterValueBinding<int>("waterInfo", "sewageExport", () => m_WaterTradeSystem.sewageExport));
		AddBinding(m_WaterAvailability = new GetterValueBinding<IndicatorValue>("waterInfo", "waterAvailability", GetWaterAvailability, new ValueWriter<IndicatorValue>()));
		AddBinding(m_SewageAvailability = new GetterValueBinding<IndicatorValue>("waterInfo", "sewageAvailability", GetSewageAvailability, new ValueWriter<IndicatorValue>()));
		AddBinding(m_WaterTrade = new GetterValueBinding<IndicatorValue>("waterInfo", "waterTrade", GetWaterTrade, new ValueWriter<IndicatorValue>()));
	}

	protected override void PerformUpdate()
	{
		m_WaterCapacity.Update();
		m_WaterConsumption.Update();
		m_SewageCapacity.Update();
		m_SewageConsumption.Update();
		m_WaterExport.Update();
		m_WaterImport.Update();
		m_SewageExport.Update();
		m_WaterAvailability.Update();
		m_SewageAvailability.Update();
		m_WaterTrade.Update();
	}

	private IndicatorValue GetWaterTrade()
	{
		if (!m_OutsideTradeParameterGroup.IsEmptyIgnoreFilter)
		{
			OutsideTradeParameterData singleton = m_OutsideTradeParameterGroup.GetSingleton<OutsideTradeParameterData>();
			float num = (float)m_WaterTradeSystem.freshExport * singleton.m_WaterExportPrice - (float)m_WaterTradeSystem.freshImport * singleton.m_WaterImportPrice;
			float num2 = math.max(0.01f, (float)m_WaterStatisticsSystem.freshConsumption * singleton.m_WaterExportPrice);
			return new IndicatorValue(-1f, 1f, math.clamp(num / num2, -1f, 1f));
		}
		return new IndicatorValue(-1f, 1f, 0f);
	}

	private IndicatorValue GetWaterAvailability()
	{
		return IndicatorValue.Calculate(m_WaterStatisticsSystem.freshCapacity, m_WaterStatisticsSystem.freshConsumption);
	}

	private IndicatorValue GetSewageAvailability()
	{
		return IndicatorValue.Calculate(m_WaterStatisticsSystem.sewageCapacity, m_WaterStatisticsSystem.sewageConsumption);
	}

	[Preserve]
	public WaterInfoviewUISystem()
	{
	}
}
