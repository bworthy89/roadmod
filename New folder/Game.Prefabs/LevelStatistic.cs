using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Statistics/", new Type[] { typeof(StatisticsPrefab) })]
public class LevelStatistic : ParametricStatistic
{
	public LevelInfo[] m_Levels;

	public override IEnumerable<StatisticParameterData> GetParameters()
	{
		if (m_Levels != null)
		{
			LevelInfo[] levels = m_Levels;
			foreach (LevelInfo levelInfo in levels)
			{
				yield return new StatisticParameterData(levelInfo.m_Value, Color.black);
			}
		}
	}

	public override string GetParameterName(int parameter)
	{
		return parameter.ToString();
	}
}
