using System;
using System.Collections.Generic;
using Colossal.Entities;
using Game.Net;
using Game.Objects;
using Game.Simulation;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[] { typeof(StaticObjectPrefab) })]
public class TreeObject : ComponentBase
{
	public float m_WoodAmount = 3000f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PlantData>());
		components.Add(ComponentType.ReadWrite<TreeData>());
		components.Add(ComponentType.ReadWrite<GrowthScaleData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Plant>());
		components.Add(ComponentType.ReadWrite<Tree>());
		components.Add(ComponentType.ReadWrite<Color>());
		components.Add(ComponentType.ReadWrite<UpdateFrame>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		TreeData componentData = default(TreeData);
		componentData.m_WoodAmount = m_WoodAmount;
		entityManager.SetComponentData(entity, componentData);
		if (entityManager.TryGetComponent<PlaceableObjectData>(entity, out var component) && (component.m_Flags & (Game.Objects.PlacementFlags.RoadNode | Game.Objects.PlacementFlags.RoadEdge)) == 0)
		{
			component.m_SubReplacementType = SubReplacementType.Tree;
			entityManager.SetComponentData(entity, component);
		}
	}
}
