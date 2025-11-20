using System.Collections.Generic;
using Game.Common;
using Game.Effects;
using Unity.Entities;

namespace Game.Prefabs;

public class EffectPrefab : TransformPrefab
{
	public EffectCondition m_Conditions;

	public bool m_DisableDistanceCulling;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<EffectInstance>());
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<EffectData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		entityManager.SetComponentData(entity, new EffectData
		{
			m_Archetype = GetArchetype(entityManager, entity),
			m_Flags = new EffectCondition
			{
				m_RequiredFlags = m_Conditions.m_RequiredFlags,
				m_ForbiddenFlags = m_Conditions.m_ForbiddenFlags,
				m_IntensityFlags = m_Conditions.m_IntensityFlags
			},
			m_OwnerCulling = !m_DisableDistanceCulling
		});
	}

	private EntityArchetype GetArchetype(EntityManager entityManager, Entity entity)
	{
		List<ComponentBase> list = new List<ComponentBase>();
		GetComponents(list);
		HashSet<ComponentType> hashSet = new HashSet<ComponentType>();
		for (int i = 0; i < list.Count; i++)
		{
			list[i].GetArchetypeComponents(hashSet);
		}
		hashSet.Add(ComponentType.ReadWrite<Created>());
		hashSet.Add(ComponentType.ReadWrite<Updated>());
		return entityManager.CreateArchetype(PrefabUtils.ToArray(hashSet));
	}
}
