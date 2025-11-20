using System;
using System.Collections.Generic;
using Game.Net;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[] { typeof(AggregateNetPrefab) })]
public class NetArrow : ComponentBase
{
	public Material m_ArrowMaterial;

	public Color m_RoadArrowColor = Color.white;

	public Color m_TrackArrowColor = Color.white;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<NetArrowData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ArrowMaterial>());
		components.Add(ComponentType.ReadWrite<ArrowPosition>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new NetArrowData
		{
			m_RoadColor = m_RoadArrowColor.linear,
			m_TrackColor = m_TrackArrowColor.linear
		});
	}
}
