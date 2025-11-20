using System;
using System.Collections.Generic;
using Colossal.Entities;
using Game.Net;
using Game.Objects;
using Game.Simulation;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[]
{
	typeof(StaticObjectPrefab),
	typeof(NetLaneGeometryPrefab)
})]
public class PlantObject : ComponentBase
{
	public float m_PotCoverage;

	public bool m_TreeReplacement;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PlantData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Plant>());
		components.Add(ComponentType.ReadWrite<UpdateFrame>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		if (m_TreeReplacement && entityManager.TryGetComponent<PlaceableObjectData>(entity, out var component) && (component.m_Flags & (Game.Objects.PlacementFlags.RoadNode | Game.Objects.PlacementFlags.RoadEdge)) == 0)
		{
			component.m_SubReplacementType = SubReplacementType.Tree;
			entityManager.SetComponentData(entity, component);
		}
	}
}
