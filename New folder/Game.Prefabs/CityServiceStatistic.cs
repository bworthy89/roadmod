using System;
using System.Collections.Generic;
using Game.City;

namespace Game.Prefabs;

[ComponentMenu("Statistics/", new Type[] { typeof(StatisticsPrefab) })]
public class CityServiceStatistic : ParametricStatistic
{
	public CityServiceInfo[] m_CityServices;

	public override IEnumerable<StatisticParameterData> GetParameters()
	{
		if (m_CityServices != null)
		{
			int i = 0;
			while (i < m_CityServices.Length)
			{
				yield return new StatisticParameterData((int)m_CityServices[i].m_Service, m_CityServices[i].m_Color);
				int num = i + 1;
				i = num;
			}
		}
	}

	public override string GetParameterName(int parameter)
	{
		return Enum.GetName(typeof(CityService), parameter);
	}
}
