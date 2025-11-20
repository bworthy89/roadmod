using System;
using System.Collections.Generic;
using Game.Objects;
using Game.Rendering;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[]
{
	typeof(StaticObjectPrefab),
	typeof(MarkerObjectPrefab)
})]
public class FloatingObject : ComponentBase
{
	public float m_FloatingOffset;

	public bool m_FixedToBottom;

	public bool m_AllowDryland;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PlaceableObjectData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (!m_FixedToBottom && base.prefab is StaticObjectPrefab)
		{
			components.Add(ComponentType.ReadWrite<Swaying>());
			components.Add(ComponentType.ReadWrite<InterpolatedTransform>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		PlaceableObjectData componentData = entityManager.GetComponentData<PlaceableObjectData>(entity);
		componentData.m_PlacementOffset.y = m_FloatingOffset;
		if (m_AllowDryland)
		{
			componentData.m_Flags |= PlacementFlags.OnGround | PlacementFlags.Floating;
		}
		else
		{
			componentData.m_Flags &= ~PlacementFlags.OnGround;
			componentData.m_Flags |= PlacementFlags.Floating;
		}
		if (!m_FixedToBottom)
		{
			componentData.m_Flags |= PlacementFlags.Swaying;
		}
		entityManager.SetComponentData(entity, componentData);
	}
}
