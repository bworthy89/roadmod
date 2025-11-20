using System;
using System.Collections.Generic;
using Game.Economy;

namespace Game.Prefabs;

[ComponentMenu("Statistics/", new Type[] { typeof(StatisticsPrefab) })]
public class ResourceStatistic : ParametricStatistic
{
	public ResourcePrefab[] m_Resources;

	public override IEnumerable<StatisticParameterData> GetParameters()
	{
		if (m_Resources != null)
		{
			ResourcePrefab[] resources = m_Resources;
			foreach (ResourcePrefab resourcePrefab in resources)
			{
				int resourceIndex = EconomyUtils.GetResourceIndex(EconomyUtils.GetResource(resourcePrefab.m_Resource));
				yield return new StatisticParameterData(resourceIndex, resourcePrefab.m_Color);
			}
		}
	}

	public override string GetParameterName(int parameter)
	{
		return Enum.GetName(typeof(Resource), EconomyUtils.GetResource(parameter));
	}
}
