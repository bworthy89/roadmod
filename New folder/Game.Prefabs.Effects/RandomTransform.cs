using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs.Effects;

[ComponentMenu("Effects/", new Type[] { typeof(EffectPrefab) })]
public class RandomTransform : ComponentBase
{
	public float3 m_MinAngle = new float3(0f, 0f, 0f);

	public float3 m_MaxAngle = new float3(0f, 0f, 360f);

	public float3 m_MinPosition = new float3(0f, 0f, 0f);

	public float3 m_MaxPosition = new float3(0f, 0f, 0f);

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<RandomTransformData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		RandomTransformData componentData = default(RandomTransformData);
		componentData.m_AngleRange.min = math.radians(m_MinAngle);
		componentData.m_AngleRange.max = math.radians(m_MaxAngle);
		componentData.m_PositionRange.min = m_MinPosition;
		componentData.m_PositionRange.max = m_MaxPosition;
		entityManager.SetComponentData(entity, componentData);
	}
}
