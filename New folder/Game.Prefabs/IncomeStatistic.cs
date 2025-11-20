using System;
using System.Collections.Generic;
using Game.City;

namespace Game.Prefabs;

[ComponentMenu("Statistics/", new Type[] { typeof(StatisticsPrefab) })]
public class IncomeStatistic : ParametricStatistic
{
	public IncomeSourceInfo[] m_Incomes;

	public override IEnumerable<StatisticParameterData> GetParameters()
	{
		if (m_Incomes != null)
		{
			int i = 0;
			while (i < m_Incomes.Length)
			{
				yield return new StatisticParameterData((int)m_Incomes[i].m_IncomeSource, m_Incomes[i].m_Color);
				int num = i + 1;
				i = num;
			}
		}
	}

	public override string GetParameterName(int parameter)
	{
		return Enum.GetName(typeof(IncomeSource), parameter);
	}
}
