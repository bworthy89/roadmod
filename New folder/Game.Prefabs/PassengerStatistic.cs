using System;
using System.Collections.Generic;
using Game.City;

namespace Game.Prefabs;

[ComponentMenu("Statistics/", new Type[] { typeof(StatisticsPrefab) })]
public class PassengerStatistic : ParametricStatistic
{
	public PassengerTypeInfo[] m_PassengerTypes;

	public override IEnumerable<StatisticParameterData> GetParameters()
	{
		if (m_PassengerTypes != null)
		{
			int i = 0;
			while (i < m_PassengerTypes.Length)
			{
				yield return new StatisticParameterData((int)m_PassengerTypes[i].m_PassengerType, m_PassengerTypes[i].m_Color);
				int num = i + 1;
				i = num;
			}
		}
	}

	public override string GetParameterName(int parameter)
	{
		return Enum.GetName(typeof(PassengerType), parameter);
	}
}
