using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Common;
using Game.Creatures;
using Game.Events;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class PassengersSection : InfoSectionBase
{
	protected override string group => "PassengersSection";

	private int passengers { get; set; }

	private int maxPassengers { get; set; }

	private int pets { get; set; }

	private VehiclePassengerLocaleKey vehiclePassengerKey { get; set; }

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
		passengers = 0;
		maxPassengers = 0;
		pets = 0;
	}

	private bool Visible()
	{
		if (!base.EntityManager.HasComponent<Vehicle>(selectedEntity) || !base.EntityManager.HasComponent<Passenger>(selectedEntity) || base.EntityManager.HasComponent<InvolvedInAccident>(selectedEntity))
		{
			return false;
		}
		if (base.EntityManager.HasComponent<Game.Vehicles.PersonalCar>(selectedEntity) && base.EntityManager.TryGetComponent<PersonalCarData>(selectedPrefab, out var component))
		{
			return component.m_PassengerCapacity > 0;
		}
		if (base.EntityManager.HasComponent<Game.Vehicles.Taxi>(selectedEntity) && base.EntityManager.TryGetComponent<TaxiData>(selectedPrefab, out var component2))
		{
			return component2.m_PassengerCapacity > 0;
		}
		if (base.EntityManager.HasComponent<Game.Vehicles.PoliceCar>(selectedEntity) && base.EntityManager.TryGetComponent<PoliceCarData>(selectedPrefab, out var component3))
		{
			return component3.m_CriminalCapacity > 0;
		}
		int num = 0;
		PublicTransportVehicleData component6;
		if (base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<LayoutElement> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity vehicle = buffer[i].m_Vehicle;
				if (base.EntityManager.TryGetComponent<PrefabRef>(vehicle, out var component4) && base.EntityManager.TryGetComponent<PublicTransportVehicleData>(component4.m_Prefab, out var component5))
				{
					num += component5.m_PassengerCapacity;
				}
			}
		}
		else if (base.EntityManager.TryGetComponent<PublicTransportVehicleData>(selectedPrefab, out component6))
		{
			num = component6.m_PassengerCapacity;
		}
		return num > 0;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		if (base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<LayoutElement> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				Entity vehicle = buffer[i].m_Vehicle;
				if (base.EntityManager.TryGetBuffer(vehicle, isReadOnly: true, out DynamicBuffer<Passenger> buffer2))
				{
					for (int j = 0; j < buffer2.Length; j++)
					{
						if (base.EntityManager.HasComponent<Game.Creatures.Pet>(buffer2[j].m_Passenger))
						{
							pets++;
						}
						else
						{
							passengers++;
						}
					}
				}
				if (base.EntityManager.TryGetComponent<PrefabRef>(vehicle, out var component))
				{
					Entity prefab = component.m_Prefab;
					AddPassengerCapacity(prefab);
				}
			}
		}
		else
		{
			if (base.EntityManager.TryGetBuffer(selectedEntity, isReadOnly: true, out DynamicBuffer<Passenger> buffer3))
			{
				for (int k = 0; k < buffer3.Length; k++)
				{
					if (base.EntityManager.HasComponent<Game.Creatures.Pet>(buffer3[k].m_Passenger))
					{
						pets++;
					}
					else
					{
						passengers++;
					}
				}
			}
			AddPassengerCapacity(selectedPrefab);
		}
		bool flag = false;
		int min = 0;
		vehiclePassengerKey = VehiclePassengerLocaleKey.Passenger;
		Game.Vehicles.PublicTransport component3;
		if (base.EntityManager.TryGetComponent<Game.Vehicles.PersonalCar>(selectedEntity, out var component2))
		{
			flag = (component2.m_State & PersonalCarFlags.DummyTraffic) != 0;
			min = math.min(1, maxPassengers);
		}
		else if (base.EntityManager.TryGetComponent<Game.Vehicles.PublicTransport>(selectedEntity, out component3))
		{
			if ((component3.m_State & PublicTransportFlags.PrisonerTransport) != 0)
			{
				vehiclePassengerKey = VehiclePassengerLocaleKey.Prisoner;
			}
			flag = (component3.m_State & PublicTransportFlags.DummyTraffic) != 0;
		}
		if (flag && base.EntityManager.TryGetComponent<PseudoRandomSeed>(selectedEntity, out var component4))
		{
			Unity.Mathematics.Random random = component4.GetRandom(PseudoRandomSeed.kDummyPassengers);
			passengers = math.max(passengers, random.NextInt(min, maxPassengers + 1));
			if (passengers != 0)
			{
				int num = random.NextInt(0, (passengers + random.NextInt(10)) / 10 + 1);
				for (int l = 0; l < num; l++)
				{
					pets += math.max(1, random.NextInt(0, 4));
				}
			}
		}
		passengers = math.max(0, passengers);
	}

	private void AddPassengerCapacity(Entity prefab)
	{
		TaxiData component2;
		PoliceCarData component3;
		PublicTransportVehicleData component4;
		if (base.EntityManager.TryGetComponent<PersonalCarData>(prefab, out var component))
		{
			maxPassengers += component.m_PassengerCapacity;
		}
		else if (base.EntityManager.TryGetComponent<TaxiData>(prefab, out component2))
		{
			maxPassengers += component2.m_PassengerCapacity;
		}
		else if (base.EntityManager.TryGetComponent<PoliceCarData>(prefab, out component3))
		{
			maxPassengers += component3.m_CriminalCapacity;
		}
		else if (base.EntityManager.TryGetComponent<PublicTransportVehicleData>(prefab, out component4))
		{
			maxPassengers += component4.m_PassengerCapacity;
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("passengers");
		writer.Write(passengers);
		writer.PropertyName("maxPassengers");
		writer.Write(maxPassengers);
		writer.PropertyName("pets");
		writer.Write(pets);
		writer.PropertyName("vehiclePassengerKey");
		writer.Write(Enum.GetName(typeof(VehiclePassengerLocaleKey), vehiclePassengerKey));
	}

	[Preserve]
	public PassengersSection()
	{
	}
}
