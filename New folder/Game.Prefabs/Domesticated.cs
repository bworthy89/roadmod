using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Creatures;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Creatures/", new Type[] { typeof(AnimalPrefab) })]
public class Domesticated : ComponentBase
{
	public Bounds1 m_IdleTime = new Bounds1(20f, 120f);

	public int m_MinGroupMemberCount = 1;

	public int m_MaxGroupMemberCount = 2;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<DomesticatedData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Creatures.Domesticated>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		DomesticatedData componentData = default(DomesticatedData);
		componentData.m_IdleTime = m_IdleTime;
		componentData.m_GroupMemberCount.x = m_MinGroupMemberCount;
		componentData.m_GroupMemberCount.y = m_MaxGroupMemberCount;
		entityManager.SetComponentData(entity, componentData);
		entityManager.SetComponentData(entity, new UpdateFrameData(9));
	}
}
