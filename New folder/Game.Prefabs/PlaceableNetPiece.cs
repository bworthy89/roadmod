using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[] { typeof(NetPiecePrefab) })]
public class PlaceableNetPiece : ComponentBase
{
	public uint m_ConstructionCost;

	public uint m_ElevationCost;

	public float m_UpkeepCost;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PlaceableNetPieceData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new PlaceableNetPieceData
		{
			m_ConstructionCost = m_ConstructionCost,
			m_ElevationCost = m_ElevationCost,
			m_UpkeepCost = m_UpkeepCost
		});
	}
}
