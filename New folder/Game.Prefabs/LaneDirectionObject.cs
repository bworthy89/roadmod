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
public class LaneDirectionObject : ComponentBase
{
	public LaneDirectionType m_Left = LaneDirectionType.None;

	public LaneDirectionType m_Forward;

	public LaneDirectionType m_Right = LaneDirectionType.None;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<LaneDirectionData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Objects.NetObject>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		LaneDirectionData componentData = default(LaneDirectionData);
		componentData.m_Left = m_Left;
		componentData.m_Forward = m_Forward;
		componentData.m_Right = m_Right;
		entityManager.SetComponentData(entity, componentData);
	}
}
