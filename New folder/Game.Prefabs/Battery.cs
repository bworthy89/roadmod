using System;
using System.Collections.Generic;
using Game.Buildings;
using Game.Notifications;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab)
})]
public class Battery : ComponentBase, IServiceUpgrade
{
	public int m_PowerOutput;

	public int m_Capacity;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<BatteryData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (GetComponent<ServiceUpgrade>() == null)
		{
			if (GetComponent<CityServiceBuilding>() != null)
			{
				components.Add(ComponentType.ReadWrite<Efficiency>());
			}
			components.Add(ComponentType.ReadWrite<Game.Buildings.Battery>());
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.Battery>());
		components.Add(ComponentType.ReadWrite<Efficiency>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		entityManager.SetComponentData(entity, new BatteryData
		{
			m_Capacity = m_Capacity,
			m_PowerOutput = m_PowerOutput
		});
	}

	public void DoActionWithOwnerAfterRemove(EntityManager entityManager, Entity ownerEntity)
	{
		World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<IconCommandSystem>().CreateCommandBuffer().Remove(ownerEntity, entityManager.CreateEntityQuery(ComponentType.ReadOnly<ElectricityParameterData>()).GetSingleton<ElectricityParameterData>().m_BatteryEmptyNotificationPrefab);
	}
}
