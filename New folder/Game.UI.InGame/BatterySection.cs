using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class BatterySection : InfoSectionBase
{
	protected override string group => "BatterySection";

	private int batteryCharge { get; set; }

	private int batteryCapacity { get; set; }

	private int flow { get; set; }

	private float remainingTime { get; set; }

	protected override void Reset()
	{
		batteryCharge = 0;
		batteryCapacity = 0;
		flow = 0;
		remainingTime = 0f;
	}

	private bool Visible()
	{
		return base.EntityManager.HasComponent<Game.Buildings.Battery>(selectedEntity);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		if (TryGetComponentWithUpgrades<BatteryData>(selectedEntity, selectedPrefab, out var data))
		{
			Game.Buildings.Battery componentData = base.EntityManager.GetComponentData<Game.Buildings.Battery>(selectedEntity);
			batteryCharge = componentData.storedEnergyHours;
			batteryCapacity = data.m_Capacity;
			flow = componentData.m_LastFlow;
			if (flow > 0)
			{
				long num = (data.capacityTicks - componentData.m_StoredEnergy) / flow;
				remainingTime = math.min((float)num / 2048f, 12f);
			}
			else if (flow < 0)
			{
				long num2 = componentData.m_StoredEnergy / -flow;
				remainingTime = math.min((float)num2 / 2048f, 12f);
			}
			else
			{
				remainingTime = 0f;
			}
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("batteryCharge");
		writer.Write(batteryCharge);
		writer.PropertyName("batteryCapacity");
		writer.Write(batteryCapacity);
		writer.PropertyName("flow");
		writer.Write(flow);
		writer.PropertyName("remainingTime");
		writer.Write(remainingTime);
	}

	[Preserve]
	public BatterySection()
	{
	}
}
