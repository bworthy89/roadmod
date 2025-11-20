using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Common;
using Game.Objects;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[] { typeof(StaticObjectPrefab) })]
public class CraneObject : ComponentBase
{
	public Bounds1 m_DistanceRange = new Bounds1(0f, float.MaxValue);

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<CraneData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Crane>());
		components.Add(ComponentType.ReadWrite<PointOfInterest>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		CraneData componentData = default(CraneData);
		componentData.m_DistanceRange = m_DistanceRange;
		entityManager.SetComponentData(entity, componentData);
	}
}
