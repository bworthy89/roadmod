using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/", new Type[] { })]
public class LocalEffectsPrefab : PrefabBase
{
	public LocalEffectInfo[] m_Effects;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<LocalModifierData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		if (m_Effects != null)
		{
			DynamicBuffer<LocalModifierData> buffer = entityManager.GetBuffer<LocalModifierData>(entity);
			for (int i = 0; i < m_Effects.Length; i++)
			{
				LocalEffectInfo localEffectInfo = m_Effects[i];
				buffer.Add(new LocalModifierData(localEffectInfo.m_Type, localEffectInfo.m_Mode, localEffectInfo.m_RadiusCombineMode, new Bounds1(0f, localEffectInfo.m_Delta), new Bounds1(0f, localEffectInfo.m_Radius)));
			}
		}
	}
}
