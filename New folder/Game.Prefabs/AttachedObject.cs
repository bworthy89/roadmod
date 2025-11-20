using System;
using System.Collections.Generic;
using Game.Objects;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[]
{
	typeof(StaticObjectPrefab),
	typeof(MarkerObjectPrefab)
})]
public class AttachedObject : ComponentBase
{
	public AttachedObjectType m_AttachType;

	public float m_AttachOffset;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PlaceableObjectData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		PlaceableObjectData componentData = entityManager.GetComponentData<PlaceableObjectData>(entity);
		switch (m_AttachType)
		{
		case AttachedObjectType.Ground:
			componentData.m_Flags |= PlacementFlags.OnGround;
			componentData.m_PlacementOffset.y = m_AttachOffset;
			break;
		case AttachedObjectType.Wall:
			componentData.m_Flags &= ~PlacementFlags.OnGround;
			componentData.m_Flags |= PlacementFlags.Wall;
			componentData.m_PlacementOffset.z = m_AttachOffset;
			break;
		case AttachedObjectType.Hanging:
			componentData.m_Flags &= ~PlacementFlags.OnGround;
			componentData.m_Flags |= PlacementFlags.Hanging;
			componentData.m_PlacementOffset.y = m_AttachOffset;
			break;
		}
		entityManager.SetComponentData(entity, componentData);
	}
}
