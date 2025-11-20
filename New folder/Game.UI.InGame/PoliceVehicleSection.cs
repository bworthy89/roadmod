using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Citizens;
using Game.Common;
using Game.Creatures;
using Game.Events;
using Game.Prefabs;
using Game.Simulation;
using Game.Vehicles;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class PoliceVehicleSection : VehicleSection
{
	protected override string group => "PoliceVehicleSection";

	private Entity criminalEntity { get; set; }

	private VehicleLocaleKey vehicleKey { get; set; }

	protected override void Reset()
	{
		base.Reset();
		criminalEntity = Entity.Null;
	}

	private bool Visible()
	{
		if (base.EntityManager.HasComponent<Vehicle>(selectedEntity) && base.EntityManager.HasComponent<Game.Vehicles.PoliceCar>(selectedEntity))
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
		Game.Vehicles.PoliceCar componentData = base.EntityManager.GetComponentData<Game.Vehicles.PoliceCar>(selectedEntity);
		PoliceCarData componentData2 = base.EntityManager.GetComponentData<PoliceCarData>(selectedPrefab);
		base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<ServiceDispatch> buffer);
		if (base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<Passenger> buffer2))
		{
			for (int i = 0; i < buffer2.Length; i++)
			{
				Entity entity = buffer2[i].m_Passenger;
				if (base.EntityManager.TryGetComponent<Game.Creatures.Resident>(entity, out var component))
				{
					entity = component.m_Citizen;
				}
				if (base.EntityManager.HasComponent<Citizen>(entity))
				{
					criminalEntity = entity;
				}
			}
		}
		vehicleKey = (base.EntityManager.HasComponent<HelicopterData>(selectedPrefab) ? VehicleLocaleKey.PoliceHelicopter : VehicleUIUtils.GetPoliceVehicleLocaleKey(componentData2.m_PurposeMask));
		base.stateKey = VehicleUIUtils.GetStateKey(selectedEntity, componentData, buffer, base.EntityManager);
		if ((componentData.m_State & PoliceCarFlags.AccidentTarget) != 0 && (componentData.m_State & PoliceCarFlags.AtTarget) == 0)
		{
			if (componentData.m_RequestCount <= 0 || !buffer.IsCreated || buffer.Length <= 0)
			{
				return;
			}
			ServiceDispatch serviceDispatch = buffer[0];
			if (!base.EntityManager.TryGetComponent<PoliceEmergencyRequest>(serviceDispatch.m_Request, out var component2))
			{
				return;
			}
			if (base.EntityManager.TryGetComponent<AccidentSite>(component2.m_Site, out var component3) && component3.m_Event != Entity.Null)
			{
				base.nextStop = new VehicleUIUtils.EntityWrapper(component3.m_Event);
			}
			else
			{
				base.nextStop = new VehicleUIUtils.EntityWrapper(component2.m_Target);
			}
		}
		base.tooltipKeys.Add(vehicleKey.ToString());
		base.OnProcess();
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		base.OnWriteProperties(writer);
		writer.PropertyName("criminal");
		if (criminalEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			m_NameSystem.BindName(writer, criminalEntity);
		}
		writer.PropertyName("criminalEntity");
		if (criminalEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(criminalEntity);
		}
		writer.PropertyName("vehicleKey");
		writer.Write(Enum.GetName(typeof(VehicleLocaleKey), vehicleKey));
	}

	[Preserve]
	public PoliceVehicleSection()
	{
	}
}
