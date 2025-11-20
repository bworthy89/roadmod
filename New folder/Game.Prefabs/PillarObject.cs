using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Objects;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[]
{
	typeof(StaticObjectPrefab),
	typeof(MarkerObjectPrefab)
})]
public class PillarObject : ComponentBase
{
	public PillarType m_Type;

	public float m_AnchorOffset;

	public Bounds1 m_VerticalPillarOffsetRange = new Bounds1(-1f, 1f);

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PillarData>());
		if (m_AnchorOffset != 0f)
		{
			components.Add(ComponentType.ReadWrite<PlaceableObjectData>());
		}
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Pillar>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		PillarData componentData = default(PillarData);
		componentData.m_Type = m_Type;
		componentData.m_OffsetRange = m_VerticalPillarOffsetRange;
		entityManager.SetComponentData(entity, componentData);
		if (m_AnchorOffset != 0f)
		{
			PlaceableObjectData componentData2 = entityManager.GetComponentData<PlaceableObjectData>(entity);
			componentData2.m_PlacementOffset.y = m_AnchorOffset;
			entityManager.SetComponentData(entity, componentData2);
		}
	}
}
