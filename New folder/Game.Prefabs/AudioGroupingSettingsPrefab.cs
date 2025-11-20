using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class AudioGroupingSettingsPrefab : PrefabBase
{
	public AudioGroupSettings[] m_Settings;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<AudioGroupingSettingsData>());
	}

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		if (m_Settings != null)
		{
			for (int i = 0; i < m_Settings.Length; i++)
			{
				if (m_Settings[i].m_GroupSoundFar != null)
				{
					prefabs.Add(m_Settings[i].m_GroupSoundFar);
				}
				if (m_Settings[i].m_GroupSoundNear != null)
				{
					prefabs.Add(m_Settings[i].m_GroupSoundNear);
				}
			}
		}
		base.GetDependencies(prefabs);
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		DynamicBuffer<AudioGroupingSettingsData> buffer = entityManager.GetBuffer<AudioGroupingSettingsData>(entity);
		if (m_Settings != null)
		{
			for (int i = 0; i < m_Settings.Length; i++)
			{
				AudioGroupSettings audioGroupSettings = m_Settings[i];
				buffer.Add(new AudioGroupingSettingsData
				{
					m_Type = audioGroupSettings.m_Type,
					m_FadeSpeed = audioGroupSettings.m_FadeSpeed,
					m_Scale = audioGroupSettings.m_Scale,
					m_GroupSoundFar = orCreateSystemManaged.GetEntity(audioGroupSettings.m_GroupSoundFar),
					m_GroupSoundNear = ((audioGroupSettings.m_GroupSoundNear != null) ? orCreateSystemManaged.GetEntity(audioGroupSettings.m_GroupSoundNear) : Entity.Null),
					m_Height = audioGroupSettings.m_Height,
					m_NearHeight = audioGroupSettings.m_NearHeight,
					m_NearWeight = audioGroupSettings.m_NearWeight
				});
			}
		}
	}
}
