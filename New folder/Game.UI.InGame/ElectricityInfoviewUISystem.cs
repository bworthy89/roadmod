using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Prefabs;
using Game.Simulation;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class ElectricityInfoviewUISystem : InfoviewUISystemBase
{
	private const string kGroup = "electricityInfo";

	private ElectricityStatisticsSystem m_ElectricityStatisticsSystem;

	private ElectricityTradeSystem m_ElectricityTradeSystem;

	private EntityQuery m_OutsideTradeParameterGroup;

	private GetterValueBinding<int> m_ElectricityProduction;

	private GetterValueBinding<int> m_ElectricityConsumption;

	private GetterValueBinding<int> m_ElectricityTransmitted;

	private GetterValueBinding<int> m_ElectricityExport;

	private GetterValueBinding<int> m_ElectricityImport;

	private GetterValueBinding<IndicatorValue> m_ElectricityAvailability;

	private GetterValueBinding<IndicatorValue> m_ElectricityTransmission;

	private GetterValueBinding<IndicatorValue> m_ElectricityTrade;

	private GetterValueBinding<IndicatorValue> m_BatteryCharge;

	protected override bool Active
	{
		get
		{
			if (!base.Active && !m_ElectricityProduction.active && !m_ElectricityConsumption.active && !m_ElectricityTransmitted.active && !m_ElectricityExport.active && !m_ElectricityImport.active && !m_ElectricityAvailability.active && !m_ElectricityTransmission.active && !m_ElectricityTrade.active)
			{
				return m_BatteryCharge.active;
			}
			return true;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ElectricityStatisticsSystem = base.World.GetOrCreateSystemManaged<ElectricityStatisticsSystem>();
		m_ElectricityTradeSystem = base.World.GetOrCreateSystemManaged<ElectricityTradeSystem>();
		m_OutsideTradeParameterGroup = GetEntityQuery(ComponentType.ReadOnly<OutsideTradeParameterData>());
		AddBinding(m_ElectricityProduction = new GetterValueBinding<int>("electricityInfo", "electricityProduction", () => m_ElectricityStatisticsSystem.production));
		AddBinding(m_ElectricityConsumption = new GetterValueBinding<int>("electricityInfo", "electricityConsumption", () => m_ElectricityStatisticsSystem.consumption));
		AddBinding(m_ElectricityTransmitted = new GetterValueBinding<int>("electricityInfo", "electricityTransmitted", () => m_ElectricityStatisticsSystem.fulfilledConsumption));
		AddBinding(m_ElectricityExport = new GetterValueBinding<int>("electricityInfo", "electricityExport", () => m_ElectricityTradeSystem.export));
		AddBinding(m_ElectricityImport = new GetterValueBinding<int>("electricityInfo", "electricityImport", () => m_ElectricityTradeSystem.import));
		AddBinding(m_ElectricityAvailability = new GetterValueBinding<IndicatorValue>("electricityInfo", "electricityAvailability", GetElectricityAvailability, new ValueWriter<IndicatorValue>()));
		AddBinding(m_ElectricityTransmission = new GetterValueBinding<IndicatorValue>("electricityInfo", "electricityTransmission", GetElectricityTransmission, new ValueWriter<IndicatorValue>()));
		AddBinding(m_ElectricityTrade = new GetterValueBinding<IndicatorValue>("electricityInfo", "electricityTrade", GetElectricityTrade, new ValueWriter<IndicatorValue>()));
		AddBinding(m_BatteryCharge = new GetterValueBinding<IndicatorValue>("electricityInfo", "batteryCharge", GetBatteryCharge, new ValueWriter<IndicatorValue>()));
	}

	protected override void PerformUpdate()
	{
		m_ElectricityProduction.Update();
		m_ElectricityConsumption.Update();
		m_ElectricityTransmitted.Update();
		m_ElectricityExport.Update();
		m_ElectricityImport.Update();
		m_ElectricityAvailability.Update();
		m_ElectricityTransmission.Update();
		m_ElectricityTrade.Update();
		m_BatteryCharge.Update();
	}

	private IndicatorValue GetElectricityTransmission()
	{
		float max = m_ElectricityStatisticsSystem.consumption;
		float current = m_ElectricityStatisticsSystem.fulfilledConsumption;
		return new IndicatorValue(0f, max, current);
	}

	private IndicatorValue GetElectricityAvailability()
	{
		return IndicatorValue.Calculate(m_ElectricityStatisticsSystem.production, m_ElectricityStatisticsSystem.consumption);
	}

	private IndicatorValue GetElectricityTrade()
	{
		if (!m_OutsideTradeParameterGroup.IsEmptyIgnoreFilter)
		{
			OutsideTradeParameterData singleton = m_OutsideTradeParameterGroup.GetSingleton<OutsideTradeParameterData>();
			float num = (float)m_ElectricityTradeSystem.export * singleton.m_ElectricityExportPrice - (float)m_ElectricityTradeSystem.import * singleton.m_ElectricityImportPrice;
			float num2 = math.max(0.01f, (float)m_ElectricityStatisticsSystem.consumption * singleton.m_ElectricityExportPrice);
			return new IndicatorValue(-1f, 1f, math.clamp(num / num2, -1f, 1f));
		}
		return new IndicatorValue(-1f, 1f, 0f);
	}

	private IndicatorValue GetBatteryCharge()
	{
		return new IndicatorValue(0f, m_ElectricityStatisticsSystem.batteryCapacity, m_ElectricityStatisticsSystem.batteryCharge);
	}

	[Preserve]
	public ElectricityInfoviewUISystem()
	{
	}
}
