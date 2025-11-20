using System;
using System.Collections.Generic;
using Colossal;
using Colossal.Logging;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class PopulationVictoryConfigurationPrefab : PrefabBase
{
	[Serializable]
	public struct GoalPerPlatform
	{
		public Platform Platform;

		public int Goal;
	}

	[Tooltip("Population to reach to show game end popup (for consoles)")]
	public List<GoalPerPlatform> m_populationGoalPerPlatform;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<PopulationVictoryConfigurationData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		int num = -1;
		if (Platform.Consoles.IsPlatformSet(Application.platform))
		{
			foreach (GoalPerPlatform item in m_populationGoalPerPlatform)
			{
				if (item.Platform.IsPlatformSet(Application.platform))
				{
					num = item.Goal;
					break;
				}
			}
			if (num == -1)
			{
				LogManager.GetLogger("Platforms").Error($"Population goal is not set for platform {Application.platform}");
			}
		}
		entityManager.SetComponentData(entity, new PopulationVictoryConfigurationData
		{
			m_populationGoal = num
		});
	}
}
