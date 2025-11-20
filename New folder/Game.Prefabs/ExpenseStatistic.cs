using System;
using System.Collections.Generic;
using Game.City;

namespace Game.Prefabs;

[ComponentMenu("Statistics/", new Type[] { typeof(StatisticsPrefab) })]
public class ExpenseStatistic : ParametricStatistic
{
	public ExpenseSourceInfo[] m_Expenses;

	public override IEnumerable<StatisticParameterData> GetParameters()
	{
		if (m_Expenses != null)
		{
			int i = 0;
			while (i < m_Expenses.Length)
			{
				yield return new StatisticParameterData((int)m_Expenses[i].m_ExpenseSource, m_Expenses[i].m_Color);
				int num = i + 1;
				i = num;
			}
		}
	}

	public override string GetParameterName(int parameter)
	{
		return Enum.GetName(typeof(ExpenseSource), parameter);
	}
}
