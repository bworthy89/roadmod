using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Prefabs;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class WaterSection : InfoSectionBase
{
	protected override string group => "WaterSection";

	private float pollution { get; set; }

	private int capacity { get; set; }

	private int lastProduction { get; set; }

	protected override void Reset()
	{
		pollution = 0f;
		capacity = 0;
		lastProduction = 0;
	}

	private bool Visible()
	{
		return base.EntityManager.HasComponent<Game.Buildings.WaterPumpingStation>(selectedEntity);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		if (base.EntityManager.TryGetComponent<Game.Buildings.WaterPumpingStation>(selectedEntity, out var component))
		{
			pollution = component.m_Pollution;
			capacity = component.m_Capacity;
			lastProduction = component.m_LastProduction;
		}
		if (TryGetComponentWithUpgrades<WaterPumpingStationData>(selectedEntity, selectedPrefab, out var data) && data.m_Capacity > 0 && data.m_Types != AllowedWaterTypes.None)
		{
			base.tooltipKeys.Add("Pumping");
		}
		if ((double)pollution > 0.01)
		{
			base.tooltipKeys.Add("Pollution");
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("pollution");
		writer.Write(pollution);
		writer.PropertyName("capacity");
		writer.Write(capacity);
		writer.PropertyName("lastProduction");
		writer.Write(lastProduction);
	}

	[Preserve]
	public WaterSection()
	{
	}
}
