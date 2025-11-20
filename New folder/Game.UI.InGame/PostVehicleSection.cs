using System.Runtime.CompilerServices;
using Game.Common;
using Game.Vehicles;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class PostVehicleSection : VehicleSection
{
	protected override string group => "PostVehicleSection";

	private bool Visible()
	{
		if (base.EntityManager.HasComponent<Vehicle>(selectedEntity) && base.EntityManager.HasComponent<PostVan>(selectedEntity))
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
		PostVan componentData = base.EntityManager.GetComponentData<PostVan>(selectedEntity);
		base.stateKey = VehicleUIUtils.GetStateKey(selectedEntity, componentData, base.EntityManager);
		base.OnProcess();
	}

	[Preserve]
	public PostVehicleSection()
	{
	}
}
