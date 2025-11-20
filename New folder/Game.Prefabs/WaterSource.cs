using System;
using System.Collections.Generic;
using Game.Simulation;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[] { typeof(ObjectPrefab) })]
public class WaterSource : ComponentBase
{
	public float m_Radius = 50f;

	public float m_Height = 1f;

	public float m_Polluted;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<WaterSourceData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Simulation.WaterSourceData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		WaterSourceData componentData = default(WaterSourceData);
		componentData.m_Radius = m_Radius;
		componentData.m_height = m_Height;
		componentData.m_InitialPolluted = m_Polluted;
		componentData.m_InitialPolluted = m_Polluted;
		entityManager.SetComponentData(entity, componentData);
	}
}
