using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class FireSection : InfoSectionBase
{
	protected override string group => "FireSection";

	private float vehicleEfficiency { get; set; }

	private bool disasterResponder { get; set; }

	protected override void Reset()
	{
		vehicleEfficiency = 0f;
		disasterResponder = false;
	}

	private bool Visible()
	{
		return base.EntityManager.HasComponent<Game.Buildings.FireStation>(selectedEntity);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		if (TryGetComponentWithUpgrades<FireStationData>(selectedEntity, selectedPrefab, out var data) && base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<Efficiency> buffer))
		{
			disasterResponder = data.m_DisasterResponseCapacity > 0;
			float efficiency = BuildingUtils.GetEfficiency(buffer);
			vehicleEfficiency = data.m_VehicleEfficiency * (0.5f + efficiency * 0.5f) * 100f;
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("vehicleEfficiency");
		writer.Write(vehicleEfficiency);
		writer.PropertyName("disasterResponder");
		writer.Write(disasterResponder);
	}

	[Preserve]
	public FireSection()
	{
	}
}
