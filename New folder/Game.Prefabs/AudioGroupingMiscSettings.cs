using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { typeof(AudioGroupingSettingsPrefab) })]
public class AudioGroupingMiscSettings : ComponentBase
{
	public float m_ForestFireDistance = 100f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<AudioGroupingMiscSetting>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		AudioGroupingMiscSetting componentData = new AudioGroupingMiscSetting
		{
			m_ForestFireDistance = m_ForestFireDistance
		};
		entityManager.SetComponentData(entity, componentData);
	}
}
