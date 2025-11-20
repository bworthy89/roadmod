using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class SewageSection : InfoSectionBase
{
	protected override string group => "SewageSection";

	private float capacity { get; set; }

	private float lastProcessed { get; set; }

	private float lastPurified { get; set; }

	private float purification { get; set; }

	protected override void Reset()
	{
		capacity = 0f;
		lastProcessed = 0f;
		lastPurified = 0f;
		purification = 0f;
	}

	private bool Visible()
	{
		return base.EntityManager.HasComponent<Game.Buildings.SewageOutlet>(selectedEntity);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		Game.Buildings.SewageOutlet componentData = base.EntityManager.GetComponentData<Game.Buildings.SewageOutlet>(selectedEntity);
		capacity = componentData.m_Capacity;
		lastProcessed = componentData.m_LastProcessed;
		lastPurified = componentData.m_LastPurified;
		if (TryGetComponentWithUpgrades<SewageOutletData>(selectedEntity, selectedPrefab, out var data))
		{
			purification = data.m_Purification;
		}
		base.tooltipKeys.Add(HasWaterSource() ? "Outlet" : "Treatment");
		if (purification > 0f)
		{
			if (base.EntityManager.HasComponent<Game.Buildings.WaterPumpingStation>(selectedEntity))
			{
				base.tooltipKeys.Add("TreatmentPurification");
			}
			else
			{
				base.tooltipKeys.Add("OutletPurification");
			}
		}
	}

	private bool HasWaterSource()
	{
		if (base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<Game.Objects.SubObject> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity subObject = buffer[i].m_SubObject;
				if (base.EntityManager.HasComponent<Game.Simulation.WaterSourceData>(subObject))
				{
					return true;
				}
			}
		}
		return false;
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("capacity");
		writer.Write(capacity);
		writer.PropertyName("lastProcessed");
		writer.Write(lastProcessed);
		writer.PropertyName("lastPurified");
		writer.Write(lastPurified);
		writer.PropertyName("purification");
		writer.Write(purification);
	}

	[Preserve]
	public SewageSection()
	{
	}
}
