using System;
using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Common;
using Game.Vehicles;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class GarbageVehicleSection : VehicleSection
{
	protected override string group => "GarbageVehicleSection";

	private VehicleLocaleKey vehicleKey { get; set; }

	private bool Visible()
	{
		if (base.EntityManager.HasComponent<Vehicle>(selectedEntity) && base.EntityManager.HasComponent<GarbageTruck>(selectedEntity))
		{
			return base.EntityManager.HasComponent<Owner>(selectedEntity);
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		GarbageTruck componentData = base.EntityManager.GetComponentData<GarbageTruck>(selectedEntity);
		base.stateKey = VehicleUIUtils.GetStateKey(selectedEntity, componentData, base.EntityManager);
		vehicleKey = (((componentData.m_State & GarbageTruckFlags.IndustrialWasteOnly) != 0) ? VehicleLocaleKey.IndustrialWasteTruck : VehicleLocaleKey.GarbageTruck);
		base.tooltipKeys.Add(vehicleKey.ToString());
		base.OnProcess();
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		base.OnWriteProperties(writer);
		writer.PropertyName("vehicleKey");
		writer.Write(Enum.GetName(typeof(VehicleLocaleKey), vehicleKey));
	}

	[Preserve]
	public GarbageVehicleSection()
	{
	}
}
