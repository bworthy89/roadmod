using System;
using System.Collections.Generic;
using Game.Common;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[] { typeof(StaticObjectPrefab) })]
public class MoveableBridge : ComponentBase
{
	public float3 m_LiftOffsets;

	public float m_MovingTime;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<MoveableBridgeData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PointOfInterest>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		MoveableBridgeData componentData = default(MoveableBridgeData);
		componentData.m_LiftOffsets = m_LiftOffsets;
		componentData.m_MovingTime = m_MovingTime;
		entityManager.SetComponentData(entity, componentData);
	}
}
