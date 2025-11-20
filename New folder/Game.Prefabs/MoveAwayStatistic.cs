using System;
using System.Collections.Generic;
using Game.Agents;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Statistics/", new Type[] { typeof(StatisticsPrefab) })]
public class MoveAwayStatistic : ParametricStatistic
{
	public MoveAwayReason[] m_MoveAwayReasons;

	public override IEnumerable<StatisticParameterData> GetParameters()
	{
		if (m_MoveAwayReasons != null)
		{
			int i = 0;
			while (i < m_MoveAwayReasons.Length)
			{
				yield return new StatisticParameterData((int)m_MoveAwayReasons[i], Color.black);
				int num = i + 1;
				i = num;
			}
		}
	}

	public override string GetParameterName(int parameter)
	{
		return Enum.GetName(typeof(MoveAwayReason), parameter);
	}
}
