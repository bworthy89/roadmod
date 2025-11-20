using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Creatures;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Creatures/", new Type[] { typeof(AnimalPrefab) })]
public class Wildlife : ComponentBase
{
	public Bounds1 m_TripLength = new Bounds1(20f, 200f);

	public Bounds1 m_IdleTime = new Bounds1(10f, 60f);

	public int m_MinGroupMemberCount = 1;

	public int m_MaxGroupMemberCount = 4;

	public AnimalTravelFlags m_PrimaryTravelMethod;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<WildlifeData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Creatures.Wildlife>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		WildlifeData componentData = default(WildlifeData);
		componentData.m_TripLength = m_TripLength;
		componentData.m_IdleTime = m_IdleTime;
		componentData.m_GroupMemberCount.x = m_MinGroupMemberCount;
		componentData.m_GroupMemberCount.y = m_MaxGroupMemberCount;
		entityManager.SetComponentData(entity, componentData);
		AnimalData componentData2 = entityManager.GetComponentData<AnimalData>(entity);
		componentData2.m_PrimaryTravelMethod = m_PrimaryTravelMethod;
		entityManager.SetComponentData(entity, componentData2);
		entityManager.SetComponentData(entity, new UpdateFrameData(13));
	}
}
