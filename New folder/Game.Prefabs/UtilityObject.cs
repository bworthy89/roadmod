using System;
using System.Collections.Generic;
using Game.Net;
using Game.Objects;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[]
{
	typeof(StaticObjectPrefab),
	typeof(MarkerObjectPrefab)
})]
public class UtilityObject : ComponentBase
{
	public UtilityTypes m_UtilityType = UtilityTypes.WaterPipe;

	public float3 m_UtilityPosition;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<UtilityObjectData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Objects.UtilityObject>());
		components.Add(ComponentType.ReadWrite<Color>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		UtilityObjectData componentData = default(UtilityObjectData);
		componentData.m_UtilityTypes = m_UtilityType;
		componentData.m_UtilityPosition = m_UtilityPosition;
		entityManager.SetComponentData(entity, componentData);
	}
}
