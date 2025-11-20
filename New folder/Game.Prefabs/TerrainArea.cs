using System;
using System.Collections.Generic;
using Game.Areas;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Areas/", new Type[] { typeof(LotPrefab) })]
public class TerrainArea : ComponentBase
{
	public float m_HeightOffset = 20f;

	public float m_SlopeWidth = 20f;

	public float m_NoiseScale = 100f;

	public float m_NoiseFactor = 1f;

	public bool m_AbsoluteHeight;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<TerrainAreaData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Terrain>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		TerrainAreaData componentData = default(TerrainAreaData);
		componentData.m_HeightOffset = m_HeightOffset;
		componentData.m_SlopeWidth = m_SlopeWidth;
		componentData.m_NoiseScale = 1f / math.max(0.001f, m_NoiseScale);
		componentData.m_NoiseFactor = m_NoiseFactor;
		componentData.m_AbsoluteHeight = (m_AbsoluteHeight ? 1f : 0f);
		entityManager.SetComponentData(entity, componentData);
	}
}
