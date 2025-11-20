using System;
using System.Collections.Generic;
using Colossal.PSI.Common;
using Game.Achievements;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Achievements/", new Type[] { })]
public class ObjectAchievementComponent : ComponentBase
{
	[Serializable]
	public struct ObjectAchievementSetup
	{
		public AchievementId m_ID;

		public bool m_BypassCounter;

		public bool m_AbsoluteCounter;
	}

	public ObjectAchievementSetup[] m_Achievements;

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ObjectAchievement>());
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ObjectAchievementData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		DynamicBuffer<ObjectAchievementData> buffer = entityManager.GetBuffer<ObjectAchievementData>(entity);
		ObjectAchievementSetup[] achievements = m_Achievements;
		for (int i = 0; i < achievements.Length; i++)
		{
			ObjectAchievementSetup objectAchievementSetup = achievements[i];
			buffer.Add(new ObjectAchievementData
			{
				m_ID = objectAchievementSetup.m_ID,
				m_BypassCounter = objectAchievementSetup.m_BypassCounter,
				m_AbsoluteCounter = objectAchievementSetup.m_AbsoluteCounter
			});
		}
	}
}
