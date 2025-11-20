using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public abstract class VehicleSection : InfoSectionBase
{
	protected VehicleStateLocaleKey stateKey { get; set; }

	protected VehicleUIUtils.EntityWrapper owner { get; set; }

	protected bool fromOutside { get; set; }

	protected VehicleUIUtils.EntityWrapper nextStop { get; set; }

	protected override Entity selectedEntity
	{
		get
		{
			if (base.EntityManager.TryGetComponent<Controller>(base.selectedEntity, out var component))
			{
				return component.m_Controller;
			}
			return base.selectedEntity;
		}
	}

	protected override Entity selectedPrefab
	{
		get
		{
			if (base.EntityManager.TryGetComponent<Controller>(base.selectedEntity, out var component) && base.EntityManager.TryGetComponent<PrefabRef>(component.m_Controller, out var component2))
			{
				return component2.m_Prefab;
			}
			return base.selectedPrefab;
		}
	}

	protected override void Reset()
	{
		stateKey = VehicleStateLocaleKey.Unknown;
		owner = new VehicleUIUtils.EntityWrapper(Entity.Null);
		fromOutside = false;
		nextStop = new VehicleUIUtils.EntityWrapper(Entity.Null);
	}

	protected override void OnProcess()
	{
		Entity entity = Entity.Null;
		Game.Vehicles.PersonalCar component2;
		if (base.EntityManager.TryGetComponent<Owner>(selectedEntity, out var component))
		{
			entity = component.m_Owner;
		}
		else if (base.EntityManager.HasComponent<Bicycle>(selectedEntity) && base.EntityManager.TryGetComponent<Game.Vehicles.PersonalCar>(selectedEntity, out component2))
		{
			entity = component2.m_Keeper;
		}
		if (base.EntityManager.HasComponent<Household>(entity))
		{
			owner = new VehicleUIUtils.EntityWrapper(entity);
		}
		else
		{
			owner = (base.EntityManager.TryGetComponent<PropertyRenter>(entity, out var component3) ? new VehicleUIUtils.EntityWrapper(component3.m_Property) : new VehicleUIUtils.EntityWrapper(entity));
		}
		fromOutside = base.EntityManager.HasComponent<Game.Objects.OutsideConnection>(entity);
		VehicleStateLocaleKey vehicleStateLocaleKey = stateKey;
		if (vehicleStateLocaleKey != VehicleStateLocaleKey.Returning && vehicleStateLocaleKey != VehicleStateLocaleKey.Patrolling && vehicleStateLocaleKey != VehicleStateLocaleKey.Collecting && vehicleStateLocaleKey != VehicleStateLocaleKey.Working)
		{
			nextStop = new VehicleUIUtils.EntityWrapper(VehicleUIUtils.GetDestination(base.EntityManager, selectedEntity));
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("stateKey");
		writer.Write(Enum.GetName(typeof(VehicleStateLocaleKey), stateKey));
		writer.PropertyName("owner");
		owner.Write(writer, m_NameSystem);
		writer.PropertyName("fromOutside");
		writer.Write(fromOutside);
		writer.PropertyName("nextStop");
		nextStop.Write(writer, m_NameSystem);
	}

	[Preserve]
	protected VehicleSection()
	{
	}
}
