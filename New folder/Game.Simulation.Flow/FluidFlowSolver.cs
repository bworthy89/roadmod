#define UNITY_ASSERTIONS
using Colossal.Collections;
using Unity.Assertions;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Simulation.Flow;

public struct FluidFlowSolver
{
	public int m_SourceNode;

	public int m_SinkNode;

	public NativeArray<Node> m_Nodes;

	public NativeArray<Edge> m_Edges;

	public NativeArray<Connection> m_Connections;

	public NativeMinHeap<LabelHeapData> m_LabelQueue;

	public NativeMinHeap<PushHeapData> m_PushQueue;

	public bool m_Complete;

	public int m_CurrentVersion;

	public int m_StepCounter;

	public void InitializeState()
	{
		m_Complete = false;
		m_CurrentVersion = 0;
	}

	public void Preflow()
	{
		Node node = GetNode(m_SinkNode);
		for (int i = node.m_FirstConnection; i < node.m_LastConnection; i++)
		{
			Connection connection = GetConnection(i);
			int incomingResidualCapacity = connection.GetIncomingResidualCapacity(m_Edges);
			if (incomingResidualCapacity > 0)
			{
				AugmentOutgoingFinalFlow(connection.Reverse(), incomingResidualCapacity);
			}
		}
	}

	public void LoadState(NativeReference<FluidFlowSolverState> solverState)
	{
		FluidFlowSolverState value = solverState.Value;
		m_Complete = value.m_Complete;
		m_CurrentVersion = value.m_CurrentVersion;
	}

	public void SaveState(NativeReference<FluidFlowSolverState> solverState)
	{
		solverState.Value = new FluidFlowSolverState
		{
			m_Complete = m_Complete,
			m_CurrentVersion = m_CurrentVersion
		};
	}

	public void ResetNodes()
	{
		ResetNodes(m_Nodes);
	}

	public static void ResetNodes(NativeArray<Node> nodes)
	{
		for (int i = 0; i < nodes.Length; i++)
		{
			ref Node reference = ref nodes.ElementAt(i);
			reference.m_Height = 0;
			reference.m_Excess = 0;
			reference.m_Version = 0;
			reference.m_Distance = 0;
			reference.m_Predecessor = 0;
			reference.m_Enqueued = false;
		}
	}

	public void ResetFlows()
	{
		ResetFlows(m_Edges);
	}

	public static void ResetFlows(NativeArray<Edge> edges)
	{
		for (int i = 0; i < edges.Length; i++)
		{
			ref Edge reference = ref edges.ElementAt(i);
			reference.m_FinalFlow = 0;
			reference.m_TempFlow = 0;
		}
	}

	public void Solve()
	{
		while (!m_Complete)
		{
			SolveStep();
		}
	}

	public void SolveStep()
	{
		m_CurrentVersion++;
		Label();
		if (m_PushQueue.Length != 0)
		{
			Push();
		}
		else
		{
			m_Complete = true;
		}
	}

	private void Label()
	{
		Assert.AreEqual(0, m_LabelQueue.Length);
		Assert.AreEqual(0, m_PushQueue.Length);
		ref Node node = ref GetNode(m_SourceNode);
		node.m_Distance = 0;
		node.m_Height = 0;
		node.m_Version = m_CurrentVersion;
		node.m_Enqueued = true;
		m_LabelQueue.Insert(new LabelHeapData(m_SourceNode, 0));
		while (m_LabelQueue.Length != 0)
		{
			LabelHeapData labelHeapData = m_LabelQueue.Extract();
			ref Node node2 = ref GetNode(labelHeapData.m_NodeIndex);
			if (node2.m_Distance < labelHeapData.m_Distance)
			{
				continue;
			}
			m_StepCounter++;
			Assert.IsTrue(node2.m_Distance == labelHeapData.m_Distance);
			int num = node2.m_Height + 1;
			for (int i = node2.m_FirstConnection; i < node2.m_LastConnection; i++)
			{
				Connection connection = GetConnection(i);
				if (connection.GetOutgoingResidualCapacity(m_Edges) > 0)
				{
					Assert.IsTrue(connection.m_EndNode != m_SinkNode);
					ref Node node3 = ref GetNode(connection.m_EndNode);
					int num2 = node2.m_Distance + GetLength(connection);
					if (node3.m_Version != m_CurrentVersion)
					{
						node3.m_Enqueued = false;
						node3.m_Height = num;
						node3.m_Version = m_CurrentVersion;
						node3.m_Distance = num2;
						node3.m_Predecessor = i;
						m_LabelQueue.Insert(new LabelHeapData(connection.m_EndNode, num2));
					}
					else if (node3.m_Distance > num2)
					{
						node3.m_Enqueued &= node3.m_Height == num;
						node3.m_Height = num;
						node3.m_Distance = num2;
						node3.m_Predecessor = i;
						m_LabelQueue.Insert(new LabelHeapData(connection.m_EndNode, num2));
					}
				}
			}
			if (!node2.m_Enqueued && node2.m_Excess > 0)
			{
				node2.m_Enqueued = true;
				m_PushQueue.Insert(new PushHeapData(labelHeapData.m_NodeIndex, node2.m_Height));
			}
		}
	}

	private void Push()
	{
		Assert.AreEqual(0, m_LabelQueue.Length);
		Assert.AreNotEqual(0, m_PushQueue.Length);
		while (m_PushQueue.Length != 0)
		{
			PushHeapData pushHeapData = m_PushQueue.Extract();
			ref Node node = ref GetNode(pushHeapData.m_NodeIndex);
			if (node.m_Enqueued && pushHeapData.m_Height == node.m_Height)
			{
				m_StepCounter++;
				Assert.IsTrue(node.m_Excess > 0);
				Assert.AreNotEqual(0, node.m_Predecessor);
				node.m_Enqueued = false;
				Connection connection = GetConnection(node.m_Predecessor);
				Assert.IsTrue(connection.GetOutgoingResidualCapacity(m_Edges) > 0);
				Assert.IsTrue(connection.m_EndNode == pushHeapData.m_NodeIndex);
				int num = math.min(node.m_Excess, GetMaxAdditionalOutgoingFlow(connection));
				Assert.IsTrue(num > 0);
				ref Node node2 = ref GetNode(connection.m_StartNode);
				if (!node2.m_Enqueued)
				{
					node2.m_Enqueued = true;
					m_PushQueue.Insert(new PushHeapData(connection.m_StartNode, node2.m_Height));
				}
				AugmentOutgoingFinalFlow(in connection, num);
			}
		}
	}

	private int GetLength(Connection connection)
	{
		int outgoingFinalFlow = connection.GetOutgoingFinalFlow(m_Edges);
		if (outgoingFinalFlow > 1)
		{
			return 1 + math.ceillog2(outgoingFinalFlow);
		}
		if (outgoingFinalFlow < -1)
		{
			return 1 - math.ceillog2(-outgoingFinalFlow);
		}
		return 1;
	}

	private int GetMaxAdditionalOutgoingFlow(Connection connection)
	{
		int outgoingFinalFlow = connection.GetOutgoingFinalFlow(m_Edges);
		int outgoingResidualCapacity = connection.GetOutgoingResidualCapacity(m_Edges);
		int num = ((outgoingFinalFlow > 1) ? (Mathf.NextPowerOfTwo(outgoingFinalFlow) << 1) : ((outgoingFinalFlow < -2) ? (-(Mathf.NextPowerOfTwo(-outgoingFinalFlow) >> 2) - 1) : ((outgoingFinalFlow == -2) ? 1 : 2)));
		return math.min(num - outgoingFinalFlow, outgoingResidualCapacity);
	}

	private void AugmentOutgoingFinalFlow(in Connection connection, int flow)
	{
		Assert.IsTrue(flow >= 0);
		ref Node node = ref GetNode(connection.m_StartNode);
		ref Node node2 = ref GetNode(connection.m_EndNode);
		ref Edge edge = ref GetEdge(connection.m_Edge);
		node.m_Excess += flow;
		edge.m_FinalFlow += (connection.m_Backwards ? (-flow) : flow);
		node2.m_Excess -= flow;
		int finalFlow = edge.m_FinalFlow;
		Assert.IsFalse(finalFlow < -edge.GetCapacity(backwards: true));
		Assert.IsFalse(finalFlow > edge.GetCapacity(backwards: false));
	}

	private ref Node GetNode(int index)
	{
		return ref m_Nodes.ElementAt(index);
	}

	private ref Edge GetEdge(int index)
	{
		return ref m_Edges.ElementAt(index);
	}

	private Connection GetConnection(int index)
	{
		return m_Connections[index];
	}
}
