using System;
using System.Collections.Generic;
using Game.Buildings;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Tools/Infomode/", new Type[] { })]
public class EffectRangeInfomodePrefab : ColorInfomodeBasePrefab
{
	public LocalModifierType m_Type;

	public override string infomodeTypeLocaleKey => "Radius";

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<InfoviewLocalEffectData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new InfoviewLocalEffectData
		{
			m_Type = m_Type,
			m_Color = new float4(m_Color.r, m_Color.g, m_Color.b, m_Color.a)
		});
	}
}
