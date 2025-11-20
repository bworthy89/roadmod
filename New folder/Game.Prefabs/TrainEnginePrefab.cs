using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Vehicles/", new Type[] { })]
public class TrainEnginePrefab : TrainPrefab
{
	public int m_MinEngineCount = 2;

	public int m_MaxEngineCount = 2;

	public int m_MinCarriagesPerEngine = 5;

	public int m_MaxCarriagesPerEngine = 5;

	public TrainCarPrefab m_Tender;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<TrainEngineData>());
		components.Add(ComponentType.ReadWrite<VehicleCarriageElement>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		entityManager.SetComponentData(entity, new TrainEngineData(m_MinEngineCount, m_MaxEngineCount));
		DynamicBuffer<VehicleCarriageElement> buffer = entityManager.GetBuffer<VehicleCarriageElement>(entity);
		if (m_Tender != null)
		{
			Entity entity2 = entityManager.World.GetExistingSystemManaged<PrefabSystem>().GetEntity(m_Tender);
			buffer.Add(new VehicleCarriageElement(entity2, 1, 1, VehicleCarriageDirection.Default));
		}
		buffer.Add(new VehicleCarriageElement(Entity.Null, m_MinCarriagesPerEngine, m_MaxCarriagesPerEngine, VehicleCarriageDirection.Random));
	}
}
