using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Common;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class MaintenanceVehicleSection : VehicleSection
{
	protected override string group => "MaintenanceVehicleSection";

	private int workShift { get; set; }

	protected override void Reset()
	{
		base.Reset();
		workShift = 0;
	}

	protected bool Visible()
	{
		if (base.EntityManager.HasComponent<Owner>(selectedEntity) && base.EntityManager.HasComponent<Vehicle>(selectedEntity) && base.EntityManager.HasComponent<Game.Vehicles.MaintenanceVehicle>(selectedEntity))
		{
			return base.EntityManager.HasComponent<MaintenanceVehicleData>(selectedPrefab);
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
		Game.Vehicles.MaintenanceVehicle componentData = base.EntityManager.GetComponentData<Game.Vehicles.MaintenanceVehicle>(selectedEntity);
		MaintenanceVehicleData componentData2 = base.EntityManager.GetComponentData<MaintenanceVehicleData>(selectedPrefab);
		componentData2.m_MaintenanceCapacity = Mathf.CeilToInt((float)componentData2.m_MaintenanceCapacity * componentData.m_Efficiency);
		workShift = Mathf.CeilToInt((1f - math.select((float)componentData.m_Maintained / (float)componentData2.m_MaintenanceCapacity, 0f, componentData2.m_MaintenanceCapacity == 0)) * 100f);
		base.stateKey = VehicleUIUtils.GetStateKey(selectedEntity, componentData, base.EntityManager);
		base.OnProcess();
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		base.OnWriteProperties(writer);
		writer.PropertyName("workShift");
		writer.Write(workShift);
	}

	[Preserve]
	public MaintenanceVehicleSection()
	{
	}
}
