using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Prefabs;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class ElectricitySection : InfoSectionBase
{
	protected override string group => "ElectricitySection";

	private int capacity { get; set; }

	private int production { get; set; }

	protected override void Reset()
	{
		capacity = 0;
		production = 0;
	}

	private bool Visible()
	{
		return base.EntityManager.HasComponent<ElectricityProducer>(selectedEntity);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		ElectricityProducer componentData = base.EntityManager.GetComponentData<ElectricityProducer>(selectedEntity);
		capacity = componentData.m_Capacity;
		production = componentData.m_LastProduction;
		if (TryGetComponentWithUpgrades<SolarPoweredData>(selectedEntity, selectedPrefab, out var _))
		{
			base.tooltipKeys.Add("Solar");
		}
		if (TryGetComponentWithUpgrades<WindPoweredData>(selectedEntity, selectedPrefab, out var _))
		{
			base.tooltipKeys.Add("Wind");
		}
		if (TryGetComponentWithUpgrades<GarbagePoweredData>(selectedEntity, selectedPrefab, out var _))
		{
			base.tooltipKeys.Add("Garbage");
		}
		if (base.EntityManager.HasComponent<Game.Buildings.WaterPowered>(selectedEntity))
		{
			base.tooltipKeys.Add("Water");
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("capacity");
		writer.Write(capacity);
		writer.PropertyName("production");
		writer.Write(production);
	}

	[Preserve]
	public ElectricitySection()
	{
	}
}
