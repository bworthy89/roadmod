using System;
using System.Collections.Generic;
using Game.Citizens;

namespace Game.Prefabs;

[ComponentMenu("Statistics/", new Type[] { typeof(StatisticsPrefab) })]
public class AgeStatistic : ParametricStatistic
{
	public PopulationAgeGroupInfo[] m_AgeGroups;

	public override IEnumerable<StatisticParameterData> GetParameters()
	{
		if (m_AgeGroups != null)
		{
			int i = 0;
			while (i < m_AgeGroups.Length)
			{
				yield return new StatisticParameterData((int)m_AgeGroups[i].m_Group, m_AgeGroups[i].m_Color);
				int num = i + 1;
				i = num;
			}
		}
	}

	public override string GetParameterName(int parameter)
	{
		return Enum.GetName(typeof(CitizenAge), parameter);
	}
}
