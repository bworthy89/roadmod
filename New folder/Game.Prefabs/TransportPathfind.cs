using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Pathfind/", new Type[] { typeof(PathfindPrefab) })]
public class TransportPathfind : ComponentBase
{
	public PathfindCostInfo m_OrderingCost = new PathfindCostInfo(5f, 0f, 0f, 5f);

	public PathfindCostInfo m_StartingCost = new PathfindCostInfo(5f, 0f, 10f, 5f);

	public PathfindCostInfo m_TravelCost = new PathfindCostInfo(0f, 0f, 0.02f, 0f);

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PathfindTransportData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new PathfindTransportData
		{
			m_OrderingCost = m_OrderingCost.ToPathfindCosts(),
			m_StartingCost = m_StartingCost.ToPathfindCosts(),
			m_TravelCost = m_TravelCost.ToPathfindCosts()
		});
	}
}
