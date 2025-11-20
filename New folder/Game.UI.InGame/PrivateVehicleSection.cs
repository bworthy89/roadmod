using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Common;
using Game.Vehicles;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class PrivateVehicleSection : VehicleSection
{
	protected override string group => "PrivateVehicleSection";

	private Entity keeperEntity { get; set; }

	private VehicleLocaleKey vehicleKey { get; set; }

	protected override void Reset()
	{
		base.Reset();
		keeperEntity = Entity.Null;
	}

	private bool Visible()
	{
		if (base.EntityManager.HasComponent<Vehicle>(selectedEntity) && (base.EntityManager.HasComponent<Owner>(selectedEntity) || base.EntityManager.HasComponent<Bicycle>(selectedEntity)))
		{
			if (!base.EntityManager.HasComponent<PersonalCar>(selectedEntity))
			{
				return base.EntityManager.HasComponent<Taxi>(selectedEntity);
			}
			return true;
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
		base.stateKey = VehicleUIUtils.GetStateKey(selectedEntity, base.EntityManager);
		keeperEntity = Entity.Null;
		if (base.stateKey != VehicleStateLocaleKey.Parked)
		{
			keeperEntity = (base.EntityManager.TryGetComponent<PersonalCar>(selectedEntity, out var component) ? component.m_Keeper : Entity.Null);
		}
		vehicleKey = (base.EntityManager.HasComponent<Taxi>(selectedEntity) ? VehicleLocaleKey.Taxi : VehicleLocaleKey.HouseholdVehicle);
		base.tooltipKeys.Add(vehicleKey.ToString());
		base.OnProcess();
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		base.OnWriteProperties(writer);
		writer.PropertyName("keeper");
		if (keeperEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			m_NameSystem.BindName(writer, keeperEntity);
		}
		writer.PropertyName("keeperEntity");
		if (keeperEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(keeperEntity);
		}
		writer.PropertyName("vehicleKey");
		writer.Write(Enum.GetName(typeof(VehicleLocaleKey), vehicleKey));
	}

	[Preserve]
	public PrivateVehicleSection()
	{
	}
}
