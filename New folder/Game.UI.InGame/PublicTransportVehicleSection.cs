using System;
using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Common;
using Game.Prefabs;
using Game.Vehicles;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class PublicTransportVehicleSection : VehicleWithLineSection
{
	protected override string group => "PublicTransportVehicleSection";

	private VehicleLocaleKey vehicleKey { get; set; }

	private bool showDestination { get; set; }

	private bool Visible()
	{
		if (base.EntityManager.HasComponent<Vehicle>(selectedEntity) && base.EntityManager.HasComponent<Owner>(selectedEntity))
		{
			return base.EntityManager.HasComponent<Game.Vehicles.PublicTransport>(selectedEntity);
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
		Game.Vehicles.PublicTransport componentData = base.EntityManager.GetComponentData<Game.Vehicles.PublicTransport>(selectedEntity);
		base.stateKey = VehicleUIUtils.GetStateKey(selectedEntity, componentData, base.EntityManager);
		PublicTransportVehicleData componentData2 = base.EntityManager.GetComponentData<PublicTransportVehicleData>(selectedPrefab);
		vehicleKey = (((componentData2.m_PurposeMask & PublicTransportPurpose.PrisonerTransport) != 0) ? VehicleLocaleKey.PrisonVan : (((componentData2.m_PurposeMask & PublicTransportPurpose.Evacuation) != 0 && (componentData.m_State & PublicTransportFlags.Evacuating) != 0) ? VehicleLocaleKey.EvacuationBus : VehicleLocaleKey.PublicTransportVehicle));
		showDestination = !base.EntityManager.HasComponent<Rocket>(selectedEntity);
		base.tooltipKeys.Add(vehicleKey.ToString());
		base.OnProcess();
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		base.OnWriteProperties(writer);
		writer.PropertyName("vehicleKey");
		writer.Write(Enum.GetName(typeof(VehicleLocaleKey), vehicleKey));
		writer.PropertyName("showDestination");
		writer.Write(showDestination);
	}

	[Preserve]
	public PublicTransportVehicleSection()
	{
	}
}
