using System;
using System.Collections.Generic;
using Game.Areas;
using Game.Buildings;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/CityServices/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab),
	typeof(MarkerObjectPrefab)
})]
public class School : ComponentBase, IServiceUpgrade
{
	public int m_StudentCapacity = 80;

	public SchoolLevel m_Level;

	public float m_GraduationModifier;

	public sbyte m_StudentWellbeing;

	public sbyte m_StudentHealth;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<SchoolData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.School>());
		if (GetComponent<ServiceUpgrade>() == null)
		{
			if (GetComponent<CityServiceBuilding>() != null)
			{
				components.Add(ComponentType.ReadWrite<Efficiency>());
			}
			components.Add(ComponentType.ReadWrite<Student>());
			if (GetComponent<UniqueObject>() == null)
			{
				components.Add(ComponentType.ReadWrite<ServiceDistrict>());
			}
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.School>());
		components.Add(ComponentType.ReadWrite<Student>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		SchoolData componentData = default(SchoolData);
		componentData.m_EducationLevel = (byte)m_Level;
		componentData.m_StudentCapacity = m_StudentCapacity;
		componentData.m_GraduationModifier = m_GraduationModifier;
		componentData.m_StudentWellbeing = m_StudentWellbeing;
		componentData.m_StudentHealth = m_StudentHealth;
		entityManager.SetComponentData(entity, componentData);
		entityManager.SetComponentData(entity, new UpdateFrameData(6));
	}
}
