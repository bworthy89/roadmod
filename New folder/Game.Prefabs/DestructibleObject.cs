using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[] { typeof(ObjectPrefab) })]
public class DestructibleObject : ComponentBase
{
	public float m_FireHazard = 100f;

	public float m_StructuralIntegrity = 15000f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<DestructibleObjectData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		DestructibleObjectData componentData = default(DestructibleObjectData);
		componentData.m_FireHazard = m_FireHazard;
		componentData.m_StructuralIntegrity = m_StructuralIntegrity;
		entityManager.SetComponentData(entity, componentData);
	}
}
