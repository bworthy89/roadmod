using System;
using Game.Pathfind;

namespace Game.Prefabs;

[Serializable]
public struct PathfindCostInfo
{
	public float m_Time;

	public float m_Behaviour;

	public float m_Money;

	public float m_Comfort;

	public PathfindCostInfo(float time, float behaviour, float money, float comfort)
	{
		m_Time = time;
		m_Behaviour = behaviour;
		m_Money = money;
		m_Comfort = comfort;
	}

	public PathfindCosts ToPathfindCosts()
	{
		return new PathfindCosts(m_Time, m_Behaviour, m_Money, m_Comfort);
	}
}
