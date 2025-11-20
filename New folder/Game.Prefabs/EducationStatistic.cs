using System;
using System.Collections.Generic;
using Game.Citizens;

namespace Game.Prefabs;

[ComponentMenu("Statistics/", new Type[] { typeof(StatisticsPrefab) })]
public class EducationStatistic : ParametricStatistic
{
	public EducationLevelInfo[] m_Levels;

	public override IEnumerable<StatisticParameterData> GetParameters()
	{
		if (m_Levels != null)
		{
			int i = 0;
			while (i < m_Levels.Length)
			{
				yield return new StatisticParameterData((int)m_Levels[i].m_EducationLevel, m_Levels[i].m_Color);
				int num = i + 1;
				i = num;
			}
		}
	}

	public override string GetParameterName(int parameter)
	{
		return Enum.GetName(typeof(CitizenEducationLevel), parameter);
	}
}
