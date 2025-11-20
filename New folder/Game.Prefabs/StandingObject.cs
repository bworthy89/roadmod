using System;
using System.Collections.Generic;
using Game.Objects;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[] { typeof(StaticObjectPrefab) })]
public class StandingObject : ComponentBase
{
	public float3 m_LegSize = new float3(0.3f, 2.5f, 0.3f);

	public float2 m_LegGap;

	public bool m_CircularLeg = true;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ObjectGeometryData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		ObjectGeometryData componentData = entityManager.GetComponentData<ObjectGeometryData>(entity);
		componentData.m_LegSize = m_LegSize;
		componentData.m_LegOffset = math.select(default(float2), (m_LegGap + m_LegSize.xz) * 0.5f, m_LegGap != 0f);
		componentData.m_Flags |= (GeometryFlags)(m_CircularLeg ? 384 : 128);
		entityManager.SetComponentData(entity, componentData);
	}
}
