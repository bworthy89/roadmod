using Colossal.Collections;

namespace Game.Simulation.Flow;

public struct PushHeapData : ILessThan<PushHeapData>
{
	public int m_NodeIndex;

	public int m_Height;

	public PushHeapData(int nodeIndex, int height)
	{
		m_NodeIndex = nodeIndex;
		m_Height = height;
	}

	public bool LessThan(PushHeapData other)
	{
		return m_Height > other.m_Height;
	}

	public override string ToString()
	{
		return string.Format("{0}: {1}, {2}: {3}", "m_NodeIndex", m_NodeIndex, "m_Height", m_Height);
	}
}
