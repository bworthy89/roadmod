using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Economy;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class CargoSection : InfoSectionBase
{
	private enum CargoKey
	{
		Cargo
	}

	private ResourcePrefabs m_ResourcePrefabs;

	protected override string group => "CargoSection";

	private int cargo { get; set; }

	private int capacity { get; set; }

	private CargoKey cargoKey { get; set; }

	private NativeList<UIResource> rawMaterials { get; set; }

	private NativeList<UIResource> processedGoods { get; set; }

	private NativeList<UIResource> mail { get; set; }

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
		rawMaterials.Clear();
		processedGoods.Clear();
		mail.Clear();
		cargo = 0;
		capacity = 0;
		cargoKey = CargoKey.Cargo;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		rawMaterials = new NativeList<UIResource>(Allocator.Persistent);
		processedGoods = new NativeList<UIResource>(Allocator.Persistent);
		mail = new NativeList<UIResource>(Allocator.Persistent);
		m_ResourcePrefabs = base.World.GetOrCreateSystemManaged<ResourceSystem>().GetPrefabs();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		rawMaterials.Dispose();
		processedGoods.Dispose();
		mail.Dispose();
		base.OnDestroy();
	}

	private bool Visible()
	{
		if (!base.EntityManager.HasComponent<Vehicle>(selectedEntity))
		{
			return false;
		}
		DeliveryTruckData component4;
		CargoTransportVehicleData component5;
		if (base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<LayoutElement> buffer) && buffer.Length != 0)
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity vehicle = buffer[i].m_Vehicle;
				if (base.EntityManager.TryGetComponent<PrefabRef>(vehicle, out var component))
				{
					CargoTransportVehicleData component3;
					if (base.EntityManager.TryGetComponent<DeliveryTruckData>(component.m_Prefab, out var component2))
					{
						capacity += component2.m_CargoCapacity;
					}
					else if (base.EntityManager.TryGetComponent<CargoTransportVehicleData>(component.m_Prefab, out component3))
					{
						capacity += component3.m_CargoCapacity;
					}
				}
			}
		}
		else if (base.EntityManager.TryGetComponent<DeliveryTruckData>(selectedPrefab, out component4))
		{
			capacity = component4.m_CargoCapacity;
		}
		else if (base.EntityManager.TryGetComponent<CargoTransportVehicleData>(selectedPrefab, out component5))
		{
			capacity = component5.m_CargoCapacity;
		}
		return capacity > 0;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		cargoKey = CargoKey.Cargo;
		if (base.EntityManager.TryGetComponent<Game.Vehicles.DeliveryTruck>(selectedEntity, out var component))
		{
			Resource resource = Resource.NoResource;
			if (base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<LayoutElement> buffer) && buffer.Length != 0)
			{
				int num = 0;
				for (int i = 0; i < buffer.Length; i++)
				{
					Entity vehicle = buffer[i].m_Vehicle;
					if (base.EntityManager.TryGetComponent<Game.Vehicles.DeliveryTruck>(vehicle, out var component2))
					{
						resource |= component2.m_Resource;
						if ((component2.m_State & DeliveryTruckFlags.Loaded) != 0)
						{
							num += component2.m_Amount;
						}
					}
				}
				cargo = num;
			}
			else
			{
				resource = component.m_Resource;
				cargo = (((component.m_State & DeliveryTruckFlags.Loaded) != 0) ? component.m_Amount : 0);
			}
			UIResource.CategorizeResources(resource, cargo, rawMaterials, processedGoods, mail, base.EntityManager, m_ResourcePrefabs);
			return;
		}
		NativeList<Resources> target = new NativeList<Resources>(32, Allocator.Temp);
		DynamicBuffer<Resources> buffer4;
		if (base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<LayoutElement> buffer2))
		{
			for (int j = 0; j < buffer2.Length; j++)
			{
				Entity vehicle2 = buffer2[j].m_Vehicle;
				if (base.EntityManager.TryGetBuffer(vehicle2, isReadOnly: true, out DynamicBuffer<Resources> buffer3))
				{
					AddResources(buffer3, target);
				}
			}
		}
		else if (base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out buffer4))
		{
			AddResources(buffer4, target);
		}
		for (int k = 0; k < target.Length; k++)
		{
			Resources resources = target[k];
			UIResource.CategorizeResources(resources.m_Resource, resources.m_Amount, rawMaterials, processedGoods, mail, base.EntityManager, m_ResourcePrefabs);
			cargo += resources.m_Amount;
		}
		target.Dispose();
	}

	private void AddResources(DynamicBuffer<Resources> source, NativeList<Resources> target)
	{
		for (int i = 0; i < source.Length; i++)
		{
			Resources value = source[i];
			if (value.m_Amount == 0)
			{
				continue;
			}
			int num = 0;
			while (true)
			{
				if (num < target.Length)
				{
					Resources value2 = target[num];
					if (value2.m_Resource == value.m_Resource)
					{
						value2.m_Amount += value.m_Amount;
						target[num] = value2;
						break;
					}
					num++;
					continue;
				}
				target.Add(in value);
				break;
			}
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("cargo");
		writer.Write(cargo);
		writer.PropertyName("capacity");
		writer.Write(capacity);
		rawMaterials.Sort();
		writer.PropertyName("rawMaterials");
		writer.ArrayBegin(rawMaterials.Length);
		for (int i = 0; i < rawMaterials.Length; i++)
		{
			writer.Write(rawMaterials[i]);
		}
		writer.ArrayEnd();
		processedGoods.Sort();
		writer.PropertyName("processedGoods");
		writer.ArrayBegin(processedGoods.Length);
		for (int j = 0; j < processedGoods.Length; j++)
		{
			writer.Write(processedGoods[j]);
		}
		writer.ArrayEnd();
		mail.Sort();
		writer.PropertyName("mail");
		writer.ArrayBegin(mail.Length);
		for (int k = 0; k < mail.Length; k++)
		{
			writer.Write(mail[k]);
		}
		writer.ArrayEnd();
		writer.PropertyName("cargoKey");
		writer.Write(Enum.GetName(typeof(CargoKey), cargoKey));
	}

	[Preserve]
	public CargoSection()
	{
	}
}
