using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Common;
using Game.Economy;
using Game.Vehicles;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class DeliveryVehicleSection : VehicleSection
{
	protected override string group => "DeliveryVehicleSection";

	private Resource resource { get; set; }

	private VehicleLocaleKey vehicleKey { get; set; }

	protected override void Reset()
	{
		base.Reset();
		resource = Resource.NoResource;
	}

	private bool Visible()
	{
		if (base.EntityManager.HasComponent<Vehicle>(selectedEntity) && base.EntityManager.HasComponent<DeliveryTruck>(selectedEntity))
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
		DeliveryTruck componentData = base.EntityManager.GetComponentData<DeliveryTruck>(selectedEntity);
		if (base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<LayoutElement> buffer) && buffer.Length != 0)
		{
			Resource resource = Resource.NoResource;
			for (int i = 0; i < buffer.Length; i++)
			{
				if (base.EntityManager.TryGetComponent<DeliveryTruck>(buffer[i].m_Vehicle, out var component))
				{
					resource |= component.m_Resource;
				}
			}
			this.resource = resource;
		}
		else
		{
			this.resource = componentData.m_Resource;
		}
		base.stateKey = VehicleUIUtils.GetStateKey(selectedEntity, componentData, base.EntityManager);
		vehicleKey = (((this.resource & (Resource)28672uL) != Resource.NoResource) ? VehicleLocaleKey.PostTruck : VehicleLocaleKey.DeliveryTruck);
		base.tooltipKeys.Add(vehicleKey.ToString());
		base.OnProcess();
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		base.OnWriteProperties(writer);
		writer.PropertyName("resourceKey");
		writer.Write(Enum.GetName(typeof(Resource), resource));
		writer.PropertyName("vehicleKey");
		writer.Write(Enum.GetName(typeof(VehicleLocaleKey), vehicleKey));
	}

	[Preserve]
	public DeliveryVehicleSection()
	{
	}
}
