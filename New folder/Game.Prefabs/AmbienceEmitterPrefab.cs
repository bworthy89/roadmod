using System;
using System.Collections.Generic;
using Game.Creatures;
using Game.Simulation;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Ambience/", new Type[] { })]
public class AmbienceEmitterPrefab : ComponentBase
{
	public GroupAmbienceType m_AmbienceType;

	public float m_Intensity = 1f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<AmbienceEmitterData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<AmbienceEmitter>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new AmbienceEmitterData
		{
			m_AmbienceType = m_AmbienceType,
			m_Intensity = m_Intensity
		});
	}
}
