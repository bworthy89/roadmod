using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Vehicles/", new Type[] { })]
public class MultipleUnitTrainFrontPrefab : TrainPrefab
{
	public int m_MinMultipleUnitCount = 1;

	public int m_MaxMultipleUnitCount = 1;

	public MultipleUnitTrainCarriageInfo[] m_Carriages;

	public bool m_AddReversedEndCarriage = true;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_Carriages != null)
		{
			for (int i = 0; i < m_Carriages.Length; i++)
			{
				prefabs.Add(m_Carriages[i].m_Carriage);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<TrainEngineData>());
		components.Add(ComponentType.ReadWrite<MultipleUnitTrainData>());
		components.Add(ComponentType.ReadWrite<VehicleCarriageElement>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		entityManager.SetComponentData(entity, new TrainEngineData(m_MinMultipleUnitCount, m_MaxMultipleUnitCount));
		DynamicBuffer<VehicleCarriageElement> buffer = entityManager.GetBuffer<VehicleCarriageElement>(entity);
		if (m_Carriages != null)
		{
			PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
			for (int i = 0; i < m_Carriages.Length; i++)
			{
				MultipleUnitTrainCarriageInfo multipleUnitTrainCarriageInfo = m_Carriages[i];
				Entity entity2 = existingSystemManaged.GetEntity(multipleUnitTrainCarriageInfo.m_Carriage);
				buffer.Add(new VehicleCarriageElement(entity2, multipleUnitTrainCarriageInfo.m_MinCount, multipleUnitTrainCarriageInfo.m_MaxCount, multipleUnitTrainCarriageInfo.m_Direction));
			}
		}
		if (m_AddReversedEndCarriage)
		{
			buffer.Add(new VehicleCarriageElement(entity, 1, 1, VehicleCarriageDirection.Reversed));
		}
	}
}
