using Unity.Collections;

namespace Game.Simulation.Flow;

public struct Connection
{
	public int m_StartNode;

	public int m_EndNode;

	public int m_Edge;

	public bool m_Backwards;

	public Connection Reverse()
	{
		return new Connection
		{
			m_StartNode = m_EndNode,
			m_EndNode = m_StartNode,
			m_Edge = m_Edge,
			m_Backwards = !m_Backwards
		};
	}

	public int GetOutgoingCapacity(NativeArray<Edge> edges)
	{
		return edges[m_Edge].GetCapacity(m_Backwards);
	}

	public int GetIncomingCapacity(NativeArray<Edge> edges)
	{
		return edges[m_Edge].GetCapacity(!m_Backwards);
	}

	public int GetOutgoingFinalFlow(NativeArray<Edge> edges)
	{
		return edges[m_Edge].GetFinalFlow(m_Backwards);
	}

	public int GetIncomingFinalFlow(NativeArray<Edge> edges)
	{
		return edges[m_Edge].GetFinalFlow(!m_Backwards);
	}

	public int GetOutgoingResidualCapacity(NativeArray<Edge> edges)
	{
		return edges[m_Edge].GetResidualCapacity(m_Backwards);
	}

	public int GetIncomingResidualCapacity(NativeArray<Edge> edges)
	{
		return edges[m_Edge].GetResidualCapacity(!m_Backwards);
	}
}
