using Game.Net;

namespace Game.Simulation.Flow;

public struct Edge
{
	public int m_Capacity;

	public FlowDirection m_Direction;

	public int m_FinalFlow;

	public int m_TempFlow;

	public Identifier m_CutElementId;

	public int flow => m_FinalFlow + m_TempFlow;

	public Edge(int capacity, FlowDirection direction = FlowDirection.Both)
	{
		m_Capacity = capacity;
		m_Direction = direction;
		m_FinalFlow = 0;
		m_TempFlow = 0;
		m_CutElementId = default(Identifier);
	}

	public int GetCapacity(bool backwards)
	{
		if (backwards)
		{
			if ((m_Direction & FlowDirection.Backward) == 0)
			{
				return 0;
			}
			return m_Capacity;
		}
		if ((m_Direction & FlowDirection.Forward) == 0)
		{
			return 0;
		}
		return m_Capacity;
	}

	public int GetResidualCapacity(bool backwards)
	{
		if (m_Direction != FlowDirection.None)
		{
			if (backwards)
			{
				return (((m_Direction & FlowDirection.Backward) != FlowDirection.None) ? m_Capacity : 0) + flow;
			}
			return (((m_Direction & FlowDirection.Forward) != FlowDirection.None) ? m_Capacity : 0) - flow;
		}
		return 0;
	}

	public int GetFinalFlow(bool backwards)
	{
		if (!backwards)
		{
			return m_FinalFlow;
		}
		return -m_FinalFlow;
	}

	public void FinalizeTempFlow()
	{
		m_FinalFlow += m_TempFlow;
		m_TempFlow = 0;
	}
}
