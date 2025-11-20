using System;
using System.Collections.Generic;
using Game.Citizens;
using Unity.Entities;

namespace Game.Prefabs.Effects;

[ComponentMenu("Effects/", new Type[] { typeof(CitizenPrefab) })]
public class CitizenSelectedSound : ComponentBase
{
	public CitizenSelectedSoundInfo[] m_CitizenSelectedSounds;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<CitizenSelectedSoundData>());
	}

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		for (int i = 0; i < m_CitizenSelectedSounds.Length; i++)
		{
			prefabs.Add(m_CitizenSelectedSounds[i].m_SelectedSound);
		}
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		if (m_CitizenSelectedSounds != null)
		{
			DynamicBuffer<CitizenSelectedSoundData> buffer = entityManager.GetBuffer<CitizenSelectedSoundData>(entity);
			for (int i = 0; i < m_CitizenSelectedSounds.Length; i++)
			{
				CitizenSelectedSoundInfo citizenSelectedSoundInfo = m_CitizenSelectedSounds[i];
				buffer.Add(new CitizenSelectedSoundData(citizenSelectedSoundInfo.m_IsSickOrInjured, citizenSelectedSoundInfo.m_Age, citizenSelectedSoundInfo.m_Happiness, orCreateSystemManaged.GetEntity(citizenSelectedSoundInfo.m_SelectedSound)));
			}
		}
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}
}
