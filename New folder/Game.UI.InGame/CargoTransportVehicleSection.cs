using System.Runtime.CompilerServices;
using Game.Common;
using Game.Vehicles;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class CargoTransportVehicleSection : VehicleWithLineSection
{
	protected override string group => "CargoTransportVehicleSection";

	private bool Visible()
	{
		if (base.EntityManager.HasComponent<Vehicle>(selectedEntity) && base.EntityManager.HasComponent<CargoTransport>(selectedEntity))
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
		CargoTransport componentData = base.EntityManager.GetComponentData<CargoTransport>(selectedEntity);
		base.stateKey = VehicleUIUtils.GetStateKey(selectedEntity, componentData, base.EntityManager);
		base.OnProcess();
	}

	[Preserve]
	public CargoTransportVehicleSection()
	{
	}
}
