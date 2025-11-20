using System;
using System.Collections.Generic;
using Colossal.PSI.Common;
using Game.Achievements;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Achievements/", new Type[] { typeof(EventPrefab) })]
public class EventAchievementComponent : ComponentBase
{
	[Serializable]
	public struct EventAchievementSetup
	{
		public AchievementId m_ID;

		public uint m_FrameDelay;

		public bool m_BypassCounter;
	}

	public EventAchievementSetup[] m_Achievements;

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<EventAchievement>());
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<EventAchievementData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		DynamicBuffer<EventAchievementData> buffer = entityManager.GetBuffer<EventAchievementData>(entity);
		for (int i = 0; i < m_Achievements.Length; i++)
		{
			buffer.Add(new EventAchievementData
			{
				m_ID = m_Achievements[i].m_ID,
				m_FrameDelay = m_Achievements[i].m_FrameDelay,
				m_BypassCounter = m_Achievements[i].m_BypassCounter
			});
		}
	}
}
