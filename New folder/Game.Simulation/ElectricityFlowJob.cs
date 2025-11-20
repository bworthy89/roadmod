#define UNITY_ASSERTIONS
using Colossal.Collections;
using Game.Net;
using Game.Simulation.Flow;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Simulation;

[BurstCompile]
public struct ElectricityFlowJob : IJob
{
	public enum Phase
	{
		Initial,
		Producer,
		PostProducer,
		Battery,
		PostBattery,
		Trade,
		PostTrade,
		Complete
	}

	public struct State
	{
		public Phase m_Phase;

		public int m_LastTotalSteps;

		public int m_StepCounter;

		public bool m_Error;

		public State(int lastTotalSteps)
		{
			this = default(State);
			m_Phase = Phase.Initial;
			m_LastTotalSteps = lastTotalSteps;
		}
	}

	private const int kConnectedNodeLabel = -1;

	private const int kShortageNodeLabel = -2;

	private const int kBeforeBottleneckNodeLabel = -3;

	private const int kBeyondBottleneckNodeLabel = -4;

	public const int kConnectedEdgeLabel = -1;

	public const int kBottleneckEdgeLabel = -2;

	public const int kBeyondBottleneckEdgeLabel = -3;

	public NativeReference<State> m_State;

	public NativeArray<Game.Simulation.Flow.Node> m_Nodes;

	public NativeArray<Game.Simulation.Flow.Edge> m_Edges;

	[ReadOnly]
	public NativeArray<Connection> m_Connections;

	[ReadOnly]
	public NativeReference<NodeIndices> m_NodeIndices;

	[ReadOnly]
	public NativeArray<int> m_ChargeNodes;

	[ReadOnly]
	public NativeArray<int> m_DischargeNodes;

	[ReadOnly]
	public NativeArray<int> m_TradeNodes;

	public NativeReference<MaxFlowSolverState> m_SolverState;

	public NativeList<LayerState> m_LayerStates;

	public NativeList<CutElement> m_LayerElements;

	public NativeList<CutElementRef> m_LayerElementRefs;

	public NativeQueue<int> m_LabelQueue;

	public int m_LayerHeight;

	public int m_FrameCount;

	public bool m_FinalFrame;

	public void Execute()
	{
		ref State reference = ref m_State.ValueAsRef();
		if (reference.m_Error)
		{
			if (m_FinalFrame)
			{
				UnityEngine.Debug.LogError($"Electricity solver error in phase: {reference.m_Phase}");
				Finalize(ref reference);
			}
			return;
		}
		reference.m_Error = true;
		int num = math.max(100, reference.m_LastTotalSteps / 124);
		int num2 = reference.m_StepCounter + m_FrameCount * num;
		while (reference.m_Phase != Phase.Complete && (m_FinalFrame || reference.m_StepCounter < num2))
		{
			ExecutePhase(ref reference, num2);
		}
		reference.m_Error = false;
		if (m_FinalFrame)
		{
			Finalize(ref reference);
		}
	}

	private void Finalize(ref State state)
	{
		state.m_Phase = Phase.Initial;
		state.m_LastTotalSteps = state.m_StepCounter;
		state.m_StepCounter = 0;
		state.m_Error = false;
	}

	private void ExecutePhase(ref State state, int maxSteps)
	{
		if (state.m_Phase == Phase.Initial)
		{
			InitialPhase(ref state);
		}
		else if (state.m_Phase == Phase.Producer)
		{
			MaxFlowPhase(ref state, maxSteps, Phase.PostProducer);
		}
		else if (state.m_Phase == Phase.PostProducer)
		{
			PostProducerPhase(ref state);
		}
		else if (state.m_Phase == Phase.Battery)
		{
			MaxFlowPhase(ref state, maxSteps, Phase.PostBattery);
		}
		else if (state.m_Phase == Phase.PostBattery)
		{
			PostBatteryPhase(ref state);
		}
		else if (state.m_Phase == Phase.Trade)
		{
			MaxFlowPhase(ref state, maxSteps, Phase.PostTrade);
		}
		else if (state.m_Phase == Phase.PostTrade)
		{
			PostTradePhase(ref state);
		}
	}

	private void InitialPhase(ref State state)
	{
		ResetSolverState();
		state.m_Phase = Phase.Producer;
	}

	private void ResetSolverState()
	{
		m_SolverState.Value = default(MaxFlowSolverState);
		m_LayerStates.Length = 0;
		m_LayerElements.Length = 0;
		m_LayerElementRefs.Length = 0;
	}

	private void MaxFlowPhase(ref State state, int maxSteps, Phase phaseAfterCompletion)
	{
		NativeList<Game.Simulation.Flow.Layer> layers = new NativeList<Game.Simulation.Flow.Layer>(m_LayerStates.Capacity, Allocator.Temp);
		NativeArray<UnsafeList<int>> activeQueue = new NativeArray<UnsafeList<int>>(m_LayerHeight, Allocator.Temp);
		for (int i = 0; i < m_LayerHeight; i++)
		{
			activeQueue[i] = new UnsafeList<int>(128, Allocator.Temp);
		}
		NodeIndices value = m_NodeIndices.Value;
		MaxFlowSolver maxFlowSolver = new MaxFlowSolver
		{
			m_LayerHeight = m_LayerHeight,
			m_SourceNode = value.m_SourceNode,
			m_SinkNode = value.m_SinkNode,
			m_Nodes = m_Nodes,
			m_Edges = m_Edges,
			m_Connections = m_Connections,
			m_Layers = layers,
			m_LabelQueue = m_LabelQueue,
			m_ActiveQueue = activeQueue,
			m_StepCounter = state.m_StepCounter
		};
		if (m_SolverState.Value.m_CurrentLabelVersion == 0)
		{
			maxFlowSolver.ResetNetwork();
			maxFlowSolver.InitializeState();
		}
		else
		{
			maxFlowSolver.LoadState(m_SolverState, m_LayerStates, m_LayerElements, m_LayerElementRefs);
		}
		while (!maxFlowSolver.m_Complete && (m_FinalFrame || maxFlowSolver.m_StepCounter < maxSteps))
		{
			maxFlowSolver.SolveNextLayer();
		}
		maxFlowSolver.SaveState(m_SolverState, m_LayerStates, m_LayerElements, m_LayerElementRefs);
		foreach (Game.Simulation.Flow.Layer item in layers)
		{
			item.Dispose();
		}
		layers.Dispose();
		foreach (UnsafeList<int> item2 in activeQueue)
		{
			item2.Dispose();
		}
		activeQueue.Dispose();
		state.m_StepCounter = maxFlowSolver.m_StepCounter;
		if (maxFlowSolver.m_Complete)
		{
			state.m_Phase = phaseAfterCompletion;
		}
	}

	private void PostProducerPhase(ref State state)
	{
		LabelShortages(ref state);
		EnableDischargeConnectionsIfShortage();
		EnableChargeConnectionsIfNoShortage();
		ResetSolverState();
		state.m_Phase = Phase.Battery;
	}

	private void LabelShortages(ref State state)
	{
		NodeIndices value = m_NodeIndices.Value;
		Game.Simulation.Flow.Node node = m_Nodes[value.m_SinkNode];
		for (int i = node.m_FirstConnection; i < node.m_LastConnection; i++)
		{
			Connection connection = m_Connections[i];
			if (connection.m_EndNode != value.m_SourceNode && m_Nodes[connection.m_EndNode].m_CutElementId.m_Version >= 0 && connection.GetIncomingCapacity(m_Edges) > 0 && connection.GetIncomingResidualCapacity(m_Edges) > 0)
			{
				LabelShortageSubGraph(ref state, connection.m_EndNode);
			}
		}
	}

	private void LabelShortageSubGraph(ref State state, int initialNodeIndex)
	{
		NodeIndices value = m_NodeIndices.Value;
		Assert.IsTrue(m_LabelQueue.IsEmpty());
		Identifier identifier = new Identifier(initialNodeIndex, -2);
		m_Nodes.ElementAt(initialNodeIndex).m_CutElementId = identifier;
		m_LabelQueue.Enqueue(initialNodeIndex);
		int item;
		while (m_LabelQueue.TryDequeue(out item))
		{
			state.m_StepCounter++;
			Game.Simulation.Flow.Node node = m_Nodes[item];
			Assert.AreEqual(identifier, node.m_CutElementId);
			for (int i = node.m_FirstConnection; i < node.m_LastConnection; i++)
			{
				Connection connection = m_Connections[i];
				int endNode = connection.m_EndNode;
				if (endNode != value.m_SinkNode && endNode != value.m_SourceNode && connection.GetIncomingCapacity(m_Edges) > 0)
				{
					ref Game.Simulation.Flow.Node reference = ref m_Nodes.ElementAt(endNode);
					if (reference.m_CutElementId.m_Version != -2 && connection.GetIncomingResidualCapacity(m_Edges) > 0)
					{
						reference.m_CutElementId = identifier;
						m_LabelQueue.Enqueue(endNode);
					}
				}
			}
		}
	}

	private void EnableDischargeConnectionsIfShortage()
	{
		NodeIndices value = m_NodeIndices.Value;
		foreach (int dischargeNode in m_DischargeNodes)
		{
			Game.Simulation.Flow.Node node = m_Nodes[dischargeNode];
			for (int i = node.m_FirstConnection; i < node.m_LastConnection; i++)
			{
				Connection connection = m_Connections[i];
				ref Game.Simulation.Flow.Edge reference = ref m_Edges.ElementAt(connection.m_Edge);
				Game.Simulation.Flow.Node node2 = m_Nodes[connection.m_EndNode];
				Assert.IsTrue(reference.m_Direction == FlowDirection.None);
				if (connection.m_EndNode != value.m_SourceNode)
				{
					Assert.IsFalse(connection.m_Backwards);
					if (node2.m_CutElementId.m_Version == -2)
					{
						reference.m_Direction = FlowDirection.Forward;
					}
				}
				else
				{
					Assert.IsTrue(connection.m_Backwards);
					reference.m_Direction = FlowDirection.Forward;
				}
			}
		}
	}

	private void EnableChargeConnectionsIfNoShortage()
	{
		NodeIndices value = m_NodeIndices.Value;
		foreach (int chargeNode in m_ChargeNodes)
		{
			Game.Simulation.Flow.Node node = m_Nodes[chargeNode];
			for (int i = node.m_FirstConnection; i < node.m_LastConnection; i++)
			{
				Connection connection = m_Connections[i];
				ref Game.Simulation.Flow.Edge reference = ref m_Edges.ElementAt(connection.m_Edge);
				Game.Simulation.Flow.Node node2 = m_Nodes[connection.m_EndNode];
				Assert.IsTrue(reference.m_Direction == FlowDirection.None);
				if (connection.m_EndNode != value.m_SinkNode)
				{
					Assert.IsTrue(connection.m_Backwards);
					if (node2.m_CutElementId.m_Version != -2)
					{
						reference.m_Direction = FlowDirection.Forward;
					}
				}
				else
				{
					Assert.IsFalse(connection.m_Backwards);
					reference.m_Direction = FlowDirection.Forward;
				}
			}
		}
	}

	private void PostBatteryPhase(ref State state)
	{
		DisableConnections(m_ChargeNodes);
		DisableConnections(m_DischargeNodes);
		LabelShortages(ref state);
		EnableTradeConnections();
		ResetSolverState();
		state.m_Phase = Phase.Trade;
	}

	private void DisableConnections(NativeArray<int> nodes)
	{
		foreach (int item in nodes)
		{
			Game.Simulation.Flow.Node node = m_Nodes[item];
			for (int i = node.m_FirstConnection; i < node.m_LastConnection; i++)
			{
				m_Edges.ElementAt(m_Connections[i].m_Edge).m_Direction = FlowDirection.None;
			}
		}
	}

	private void EnableTradeConnections()
	{
		NodeIndices value = m_NodeIndices.Value;
		foreach (int tradeNode in m_TradeNodes)
		{
			Game.Simulation.Flow.Node node = m_Nodes[tradeNode];
			bool flag = node.m_CutElementId.m_Version == -2;
			for (int i = node.m_FirstConnection; i < node.m_LastConnection; i++)
			{
				Connection connection = m_Connections[i];
				ref Game.Simulation.Flow.Edge reference = ref m_Edges.ElementAt(connection.m_Edge);
				if (connection.m_EndNode == value.m_SourceNode)
				{
					Assert.IsTrue(connection.m_Backwards);
					reference.m_Direction = (flag ? FlowDirection.Forward : FlowDirection.None);
				}
				else if (connection.m_EndNode == value.m_SinkNode)
				{
					Assert.IsFalse(connection.m_Backwards);
					reference.m_Direction = ((!flag) ? FlowDirection.Forward : FlowDirection.None);
				}
			}
		}
	}

	private void PostTradePhase(ref State state)
	{
		SetTradeConnectionsEnabled(import: true, export: false);
		LabelConnected(ref state);
		LabelBottlenecks(ref state);
		state.m_Phase = Phase.Complete;
	}

	private void SetTradeConnectionsEnabled(bool import, bool export)
	{
		NodeIndices value = m_NodeIndices.Value;
		foreach (int tradeNode in m_TradeNodes)
		{
			Game.Simulation.Flow.Node node = m_Nodes[tradeNode];
			for (int i = node.m_FirstConnection; i < node.m_LastConnection; i++)
			{
				Connection connection = m_Connections[i];
				ref Game.Simulation.Flow.Edge reference = ref m_Edges.ElementAt(connection.m_Edge);
				if (connection.m_EndNode == value.m_SourceNode)
				{
					Assert.IsTrue(connection.m_Backwards);
					reference.m_Direction = (import ? FlowDirection.Forward : FlowDirection.None);
				}
				else if (connection.m_EndNode == value.m_SinkNode)
				{
					Assert.IsFalse(connection.m_Backwards);
					reference.m_Direction = (export ? FlowDirection.Forward : FlowDirection.None);
				}
			}
		}
	}

	private void LabelConnected(ref State state)
	{
		NodeIndices value = m_NodeIndices.Value;
		Game.Simulation.Flow.Node node = m_Nodes[value.m_SourceNode];
		for (int i = node.m_FirstConnection; i < node.m_LastConnection; i++)
		{
			Connection connection = m_Connections[i];
			if (m_Nodes[connection.m_EndNode].m_CutElementId.m_Version == -1)
			{
				m_Edges.ElementAt(connection.m_Edge).m_CutElementId = new Identifier(connection.m_EndNode, -1);
			}
			else if (connection.GetOutgoingCapacity(m_Edges) > 0)
			{
				m_Edges.ElementAt(connection.m_Edge).m_CutElementId = new Identifier(connection.m_EndNode, -1);
				LabelConnectedSubGraph(ref state, connection.m_EndNode);
			}
		}
	}

	private void LabelConnectedSubGraph(ref State state, int initialNodeIndex)
	{
		NodeIndices value = m_NodeIndices.Value;
		Assert.IsTrue(m_LabelQueue.IsEmpty());
		Identifier identifier = new Identifier(initialNodeIndex, -1);
		Identifier cutElementId = new Identifier(initialNodeIndex, -1);
		m_Nodes.ElementAt(initialNodeIndex).m_CutElementId = identifier;
		m_LabelQueue.Enqueue(initialNodeIndex);
		int item;
		while (m_LabelQueue.TryDequeue(out item))
		{
			state.m_StepCounter++;
			Game.Simulation.Flow.Node node = m_Nodes[item];
			Assert.AreEqual(identifier, node.m_CutElementId);
			for (int i = node.m_FirstConnection; i < node.m_LastConnection; i++)
			{
				Connection connection = m_Connections[i];
				int endNode = connection.m_EndNode;
				if (connection.GetOutgoingCapacity(m_Edges) <= 0 && endNode != value.m_SinkNode)
				{
					continue;
				}
				m_Edges.ElementAt(connection.m_Edge).m_CutElementId = cutElementId;
				if (endNode != value.m_SinkNode && endNode != value.m_SourceNode)
				{
					ref Game.Simulation.Flow.Node reference = ref m_Nodes.ElementAt(endNode);
					if (reference.m_CutElementId.m_Version != -1)
					{
						reference.m_CutElementId = identifier;
						m_LabelQueue.Enqueue(endNode);
					}
				}
			}
		}
	}

	private void LabelBottlenecks(ref State state)
	{
		NodeIndices value = m_NodeIndices.Value;
		ref Game.Simulation.Flow.Node reference = ref m_Nodes.ElementAt(value.m_SinkNode);
		reference.m_CutElementId = new Identifier(value.m_SinkNode, -2);
		for (int i = reference.m_FirstConnection; i < reference.m_LastConnection; i++)
		{
			Connection connection = m_Connections[i];
			if (connection.m_EndNode != value.m_SourceNode && m_Nodes[connection.m_EndNode].m_CutElementId.m_Version > -2 && connection.GetIncomingCapacity(m_Edges) > 0 && connection.GetIncomingResidualCapacity(m_Edges) > 0)
			{
				LabelBottleneckSubGraph(ref state, connection.m_EndNode);
			}
		}
	}

	private void LabelBottleneckSubGraph(ref State state, int initialNodeIndex)
	{
		NodeIndices value = m_NodeIndices.Value;
		Assert.IsTrue(m_LabelQueue.IsEmpty());
		int num = 0;
		int num2 = 0;
		bool flag = false;
		Identifier identifier = new Identifier(initialNodeIndex, -2);
		Identifier identifier2 = new Identifier(initialNodeIndex, -2);
		ref Game.Simulation.Flow.Node reference = ref m_Nodes.ElementAt(initialNodeIndex);
		reference.m_CutElementId = identifier;
		m_LabelQueue.Enqueue(initialNodeIndex);
		int item;
		while (m_LabelQueue.TryDequeue(out item))
		{
			state.m_StepCounter++;
			Game.Simulation.Flow.Node node = m_Nodes[item];
			Assert.AreEqual(identifier, node.m_CutElementId);
			for (int i = node.m_FirstConnection; i < node.m_LastConnection; i++)
			{
				Connection connection = m_Connections[i];
				int endNode = connection.m_EndNode;
				ref Game.Simulation.Flow.Edge reference2 = ref m_Edges.ElementAt(connection.m_Edge);
				if (endNode != value.m_SinkNode)
				{
					if (connection.GetIncomingCapacity(m_Edges) <= 0)
					{
						continue;
					}
					if (endNode != value.m_SourceNode)
					{
						ref Game.Simulation.Flow.Node reference3 = ref m_Nodes.ElementAt(endNode);
						if (reference2.m_CutElementId.m_Version == -2)
						{
							Assert.AreEqual(0, connection.GetOutgoingResidualCapacity(m_Edges));
							Assert.IsTrue(connection.GetIncomingResidualCapacity(m_Edges) > 0);
							if (reference2.m_CutElementId.m_Index == initialNodeIndex)
							{
								Assert.AreEqual(initialNodeIndex, reference3.m_CutElementId.m_Index);
								Assert.AreEqual(-2, reference3.m_CutElementId.m_Version);
								reference2.m_CutElementId = Identifier.Null;
								num2--;
							}
							else
							{
								Assert.AreNotEqual(initialNodeIndex, reference3.m_CutElementId.m_Index);
							}
						}
						else
						{
							if (!(reference3.m_CutElementId != identifier))
							{
								continue;
							}
							if (connection.GetIncomingResidualCapacity(m_Edges) > 0)
							{
								if (reference3.m_CutElementId.m_Version >= -1)
								{
									reference3.m_CutElementId = identifier;
									m_LabelQueue.Enqueue(endNode);
								}
							}
							else
							{
								reference2.m_CutElementId = identifier2;
								num2++;
							}
						}
					}
					else
					{
						Assert.AreEqual(0, connection.GetIncomingResidualCapacity(m_Edges));
						reference2.m_CutElementId = identifier2;
						flag = true;
					}
				}
				else
				{
					Assert.AreNotEqual(-2, reference2.m_CutElementId.m_Version);
					num += connection.GetOutgoingResidualCapacity(m_Edges);
				}
			}
		}
		Assert.IsTrue(num > 0);
		if (num2 <= 0 || flag)
		{
			return;
		}
		Identifier identifier3 = new Identifier(initialNodeIndex, -3);
		Identifier identifier4 = new Identifier(initialNodeIndex, -4);
		Identifier cutElementId = new Identifier(initialNodeIndex, -3);
		reference.m_CutElementId = identifier4;
		m_LabelQueue.Enqueue(initialNodeIndex);
		int item2;
		while (m_LabelQueue.TryDequeue(out item2))
		{
			state.m_StepCounter++;
			Game.Simulation.Flow.Node node2 = m_Nodes[item2];
			Assert.IsTrue(node2.m_CutElementId == identifier3 || node2.m_CutElementId == identifier4);
			if (node2.m_CutElementId == identifier4)
			{
				for (int j = node2.m_FirstConnection; j < node2.m_LastConnection; j++)
				{
					Connection connection2 = m_Connections[j];
					int endNode2 = connection2.m_EndNode;
					ref Game.Simulation.Flow.Edge reference4 = ref m_Edges.ElementAt(connection2.m_Edge);
					if (endNode2 != value.m_SinkNode)
					{
						if (connection2.GetIncomingCapacity(m_Edges) <= 0)
						{
							continue;
						}
						Assert.AreNotEqual(value.m_SourceNode, endNode2);
						ref Game.Simulation.Flow.Node reference5 = ref m_Nodes.ElementAt(endNode2);
						if (reference4.m_CutElementId != identifier2)
						{
							reference4.m_CutElementId = cutElementId;
							if (reference5.m_CutElementId == identifier)
							{
								reference5.m_CutElementId = identifier4;
								m_LabelQueue.Enqueue(endNode2);
							}
						}
						else if (endNode2 != value.m_SourceNode)
						{
							Assert.AreNotEqual(identifier, reference5.m_CutElementId);
							reference5.m_CutElementId = identifier3;
							m_LabelQueue.Enqueue(endNode2);
						}
					}
					else if (connection2.GetOutgoingCapacity(m_Edges) > 0)
					{
						reference4.m_CutElementId = cutElementId;
					}
				}
				continue;
			}
			bool flag2 = true;
			for (int k = node2.m_FirstConnection; k < node2.m_LastConnection; k++)
			{
				Connection connection3 = m_Connections[k];
				if (connection3.m_EndNode != value.m_SinkNode && connection3.GetIncomingCapacity(m_Edges) > 0 && m_Edges[connection3.m_Edge].m_CutElementId != identifier2 && connection3.GetIncomingResidualCapacity(m_Edges) > 0)
				{
					flag2 = false;
					break;
				}
			}
			if (!flag2)
			{
				continue;
			}
			for (int l = node2.m_FirstConnection; l < node2.m_LastConnection; l++)
			{
				Connection connection4 = m_Connections[l];
				int endNode3 = connection4.m_EndNode;
				if (endNode3 == value.m_SinkNode || connection4.GetIncomingCapacity(m_Edges) <= 0)
				{
					continue;
				}
				ref Game.Simulation.Flow.Edge reference6 = ref m_Edges.ElementAt(connection4.m_Edge);
				if (!(reference6.m_CutElementId != identifier2))
				{
					continue;
				}
				reference6.m_CutElementId = identifier2;
				if (endNode3 != value.m_SourceNode)
				{
					ref Game.Simulation.Flow.Node reference7 = ref m_Nodes.ElementAt(endNode3);
					if (reference7.m_CutElementId != identifier3)
					{
						reference7.m_CutElementId = identifier3;
						m_LabelQueue.Enqueue(endNode3);
					}
				}
			}
		}
	}
}
