using System;
using System.Collections.Generic;
using Game.Triggers;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Triggers/", new Type[] { })]
public class TriggerPrefab : PrefabBase
{
	public TriggerType m_TriggerType;

	public PrefabBase[] m_TriggerPrefabs;

	[EnumFlag]
	public TargetType m_TargetTypes;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_TriggerPrefabs != null)
		{
			PrefabBase[] triggerPrefabs = m_TriggerPrefabs;
			foreach (PrefabBase item in triggerPrefabs)
			{
				prefabs.Add(item);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<TriggerData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		DynamicBuffer<TriggerData> buffer = entityManager.GetBuffer<TriggerData>(entity);
		if (m_TriggerPrefabs != null && m_TriggerPrefabs.Length != 0)
		{
			PrefabBase[] triggerPrefabs = m_TriggerPrefabs;
			foreach (PrefabBase prefabBase in triggerPrefabs)
			{
				if (prefabBase != null)
				{
					buffer.Add(new TriggerData
					{
						m_TriggerType = m_TriggerType,
						m_TargetTypes = m_TargetTypes,
						m_TriggerPrefab = existingSystemManaged.GetEntity(prefabBase)
					});
				}
			}
		}
		else
		{
			buffer.Add(new TriggerData
			{
				m_TriggerType = m_TriggerType,
				m_TargetTypes = m_TargetTypes
			});
		}
	}
}
