using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class LeisureParametersPrefab : PrefabBase
{
	public EventPrefab m_TravelingEvent;

	public EventPrefab m_AttractionPrefab;

	public EventPrefab m_SightseeingPrefab;

	[Tooltip("Maximum range for leisure randomization. When a citizen's leisure ≤ 128, a random number [0..m_LeisureRandomFactor) is generated. If the result is less than the citizen's leisure value, they will engage in leisure.")]
	public int m_LeisureRandomFactor = 512;

	[Tooltip("Chance for citizen to decrease leisure counter per update")]
	public int m_ChanceCitizenDecreaseLeisureCounter = 2;

	[Tooltip("Chance for tourist to decrease leisure counter per update")]
	public int m_ChanceTouristDecreaseLeisureCounter = 20;

	[Tooltip("The amount to decrease leisure counter when it is decreased")]
	public int m_AmountLeisureCounterDecrease = 1;

	[Tooltip("The lodging resource consuming speed of tourist.")]
	public int m_TouristLodgingConsumePerDay = 100;

	[Tooltip("The service available consuming speed of tourist")]
	public int m_TouristServiceConsumePerDay = 100;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_TravelingEvent);
		prefabs.Add(m_AttractionPrefab);
		prefabs.Add(m_SightseeingPrefab);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<LeisureParametersData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		LeisureParametersData componentData = default(LeisureParametersData);
		componentData.m_TravelingPrefab = orCreateSystemManaged.GetEntity(m_TravelingEvent);
		componentData.m_AttractionPrefab = orCreateSystemManaged.GetEntity(m_AttractionPrefab);
		componentData.m_SightseeingPrefab = orCreateSystemManaged.GetEntity(m_SightseeingPrefab);
		componentData.m_LeisureRandomFactor = m_LeisureRandomFactor;
		componentData.m_ChanceCitizenDecreaseLeisureCounter = m_ChanceCitizenDecreaseLeisureCounter;
		componentData.m_ChanceTouristDecreaseLeisureCounter = m_ChanceTouristDecreaseLeisureCounter;
		componentData.m_AmountLeisureCounterDecrease = m_AmountLeisureCounterDecrease;
		componentData.m_TouristLodgingConsumePerDay = m_TouristLodgingConsumePerDay;
		componentData.m_TouristServiceConsumePerDay = m_TouristServiceConsumePerDay;
		entityManager.SetComponentData(entity, componentData);
	}
}
