using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Common;
using Game.Events;
using Game.Prefabs;
using Game.Simulation;
using Game.Vehicles;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class FireVehicleSection : VehicleSection
{
	protected override string group => "FireVehicleSection";

	private VehicleLocaleKey vehicleKey { get; set; }

	private bool Visible()
	{
		if (base.EntityManager.HasComponent<Vehicle>(selectedEntity) && base.EntityManager.HasComponent<Game.Vehicles.FireEngine>(selectedEntity))
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
		Game.Vehicles.FireEngine componentData = base.EntityManager.GetComponentData<Game.Vehicles.FireEngine>(selectedEntity);
		base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<ServiceDispatch> buffer);
		vehicleKey = (base.EntityManager.HasComponent<HelicopterData>(selectedPrefab) ? VehicleLocaleKey.FireHelicopter : VehicleLocaleKey.FireEngine);
		base.stateKey = VehicleUIUtils.GetStateKey(selectedEntity, componentData, buffer, base.EntityManager);
		if (base.stateKey == VehicleStateLocaleKey.Dispatched)
		{
			if (!buffer.IsCreated || buffer.Length == 0)
			{
				return;
			}
			ServiceDispatch serviceDispatch = buffer[0];
			if (!base.EntityManager.TryGetComponent<FireRescueRequest>(serviceDispatch.m_Request, out var component))
			{
				return;
			}
			Destroyed component3;
			if (base.EntityManager.TryGetComponent<OnFire>(component.m_Target, out var component2) && component2.m_Event != Entity.Null)
			{
				base.nextStop = new VehicleUIUtils.EntityWrapper(component2.m_Event);
			}
			else if (base.EntityManager.TryGetComponent<Destroyed>(component.m_Target, out component3) && component3.m_Event != Entity.Null)
			{
				base.nextStop = new VehicleUIUtils.EntityWrapper(component3.m_Event);
			}
			else if (component.m_Target != Entity.Null)
			{
				base.nextStop = new VehicleUIUtils.EntityWrapper(component.m_Target);
			}
		}
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
	public FireVehicleSection()
	{
	}
}
