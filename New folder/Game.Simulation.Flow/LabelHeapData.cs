using Colossal.Collections;

namespace Game.Simulation.Flow;

public struct LabelHeapData : ILessThan<LabelHeapData>
{
	public int m_NodeIndex;

	public int m_Distance;

	public LabelHeapData(int nodeIndex, int distance)
	{
		m_NodeIndex = nodeIndex;
		m_Distance = distance;
	}

	public bool LessThan(LabelHeapData other)
	{
		return m_Distance < other.m_Distance;
	}

	public override string ToString()
	{
		return string.Format("{0}: {1}, {2}: {3}", "m_NodeIndex", m_NodeIndex, "m_Distance", m_Distance);
	}
}
