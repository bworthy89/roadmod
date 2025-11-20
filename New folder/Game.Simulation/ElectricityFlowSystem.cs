#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Serialization;
using Game.Simulation.Flow;
using Game.Tools;
using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ElectricityFlowSystem : GameSystemBase, IDefaultSerializable, ISerializable, IPostDeserialize
{
	private enum Phase
	{
		Prepare,
		Flow,
		Apply
	}

	[BurstCompile]
	private struct PrepareNetworkJob : IJob
	{
		public NativeList<Game.Simulation.Flow.Node> m_Nodes;

		public NativeList<Game.Simulation.Flow.Edge> m_Edges;

		public NativeList<Connection> m_Connections;

		public NativeList<int> m_ChargeNodes;

		public NativeList<int> m_DischargeNodes;

		public NativeList<int> m_TradeNodes;

		public int m_NodeCount;

		public int m_EdgeCount;

		public void Execute()
		{
			m_Nodes.ResizeUninitialized(m_NodeCount + 1);
			m_Nodes[0] = default(Game.Simulation.Flow.Node);
			m_Edges.ResizeUninitialized(m_EdgeCount + 1);
			m_Edges[0] = default(Game.Simulation.Flow.Edge);
			m_Connections.Clear();
			m_Connections.Capacity = 2 * m_EdgeCount + 1;
			m_Connections.Add(default(Connection));
			m_ChargeNodes.Clear();
			m_DischargeNodes.Clear();
			m_TradeNodes.Clear();
		}
	}

	[BurstCompile]
	private struct PrepareNodesJob : IJobChunk
	{
		public ComponentTypeHandle<ElectricityFlowNode> m_FlowNodeType;

		public int m_MaxChunkCapacity;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			int num = unfilteredChunkIndex * m_MaxChunkCapacity;
			NativeArray<ElectricityFlowNode> nativeArray = chunk.GetNativeArray(ref m_FlowNodeType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				nativeArray.ElementAt(i).m_Index = num + i + 1;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct PrepareEdgesJob : IJobChunk
	{
		public ComponentTypeHandle<ElectricityFlowEdge> m_FlowEdgeType;

		[NativeDisableParallelForRestriction]
		public NativeArray<Game.Simulation.Flow.Edge> m_Edges;

		public int m_MaxChunkCapacity;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			int num = unfilteredChunkIndex * m_MaxChunkCapacity;
			NativeArray<ElectricityFlowEdge> nativeArray = chunk.GetNativeArray(ref m_FlowEdgeType);
			for (int i = 0; i < chunk.Count; i++)
			{
				ref ElectricityFlowEdge reference = ref nativeArray.ElementAt(i);
				int index = num + i + 1;
				m_Edges[index] = new Game.Simulation.Flow.Edge
				{
					m_Capacity = reference.m_Capacity,
					m_Direction = reference.direction
				};
				reference.m_Index = index;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct PrepareConnectionsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<ConnectedFlowEdge> m_ConnectedFlowEdgeType;

		[ReadOnly]
		public ComponentTypeHandle<BatteryChargeNode> m_ChargeNodeType;

		[ReadOnly]
		public ComponentTypeHandle<BatteryDischargeNode> m_DischargeNodeType;

		[ReadOnly]
		public ComponentTypeHandle<TradeNode> m_TradeNodeType;

		[ReadOnly]
		public ComponentLookup<ElectricityFlowNode> m_FlowNodes;

		[ReadOnly]
		public ComponentLookup<ElectricityFlowEdge> m_FlowEdges;

		public NativeArray<Game.Simulation.Flow.Node> m_Nodes;

		public NativeList<Connection> m_Connections;

		public NativeList<int> m_ChargeNodes;

		public NativeList<int> m_DischargeNodes;

		public NativeList<int> m_TradeNodes;

		public int m_MaxChunkCapacity;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			int num = unfilteredChunkIndex * m_MaxChunkCapacity;
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<ConnectedFlowEdge> bufferAccessor = chunk.GetBufferAccessor(ref m_ConnectedFlowEdgeType);
			bool flag = chunk.Has(ref m_ChargeNodeType);
			bool flag2 = chunk.Has(ref m_DischargeNodeType);
			bool flag3 = chunk.Has(ref m_TradeNodeType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				DynamicBuffer<ConnectedFlowEdge> dynamicBuffer = bufferAccessor[i];
				int value = num + i + 1;
				ref Game.Simulation.Flow.Node reference = ref m_Nodes.ElementAt(value);
				reference.m_FirstConnection = m_Connections.Length;
				reference.m_LastConnection = m_Connections.Length + dynamicBuffer.Length;
				foreach (ConnectedFlowEdge item in dynamicBuffer)
				{
					ElectricityFlowEdge electricityFlowEdge = m_FlowEdges[item.m_Edge];
					bool flag4 = electricityFlowEdge.m_End == entity;
					ElectricityFlowNode electricityFlowNode = m_FlowNodes[flag4 ? electricityFlowEdge.m_Start : electricityFlowEdge.m_End];
					m_Connections.Add(new Connection
					{
						m_StartNode = value,
						m_EndNode = electricityFlowNode.m_Index,
						m_Edge = electricityFlowEdge.m_Index,
						m_Backwards = flag4
					});
				}
				if (flag)
				{
					m_ChargeNodes.Add(in value);
				}
				if (flag2)
				{
					m_DischargeNodes.Add(in value);
				}
				if (flag3)
				{
					m_TradeNodes.Add(in value);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct PopulateNodeIndicesJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<ElectricityFlowNode> m_FlowNodes;

		public NativeReference<NodeIndices> m_NodeIndices;

		public Entity m_SourceNode;

		public Entity m_SinkNode;

		public void Execute()
		{
			m_NodeIndices.Value = new NodeIndices
			{
				m_SourceNode = m_FlowNodes[m_SourceNode].m_Index,
				m_SinkNode = m_FlowNodes[m_SinkNode].m_Index
			};
		}
	}

	[BurstCompile]
	private struct ApplyEdgesJob : IJobChunk
	{
		public ComponentTypeHandle<ElectricityFlowEdge> m_FlowEdgeType;

		[ReadOnly]
		public NativeArray<Game.Simulation.Flow.Edge> m_Edges;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<ElectricityFlowEdge> nativeArray = chunk.GetNativeArray(ref m_FlowEdgeType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ref ElectricityFlowEdge reference = ref nativeArray.ElementAt(i);
				Game.Simulation.Flow.Edge edge = m_Edges[reference.m_Index];
				reference.m_Flow = edge.flow;
				reference.m_Flags &= ElectricityFlowEdgeFlags.ForwardBackward;
				reference.m_Flags |= GetBottleneckFlag(edge.m_CutElementId.m_Version);
			}
		}

		private ElectricityFlowEdgeFlags GetBottleneckFlag(int label)
		{
			return label switch
			{
				-2 => ElectricityFlowEdgeFlags.Bottleneck, 
				-3 => ElectricityFlowEdgeFlags.BeyondBottleneck, 
				-1 => ElectricityFlowEdgeFlags.None, 
				_ => ElectricityFlowEdgeFlags.Disconnected, 
			};
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		public ComponentTypeHandle<ElectricityFlowNode> __Game_Simulation_ElectricityFlowNode_RW_ComponentTypeHandle;

		public ComponentTypeHandle<ElectricityFlowEdge> __Game_Simulation_ElectricityFlowEdge_RW_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public BufferTypeHandle<ConnectedFlowEdge> __Game_Simulation_ConnectedFlowEdge_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BatteryChargeNode> __Game_Simulation_BatteryChargeNode_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BatteryDischargeNode> __Game_Simulation_BatteryDischargeNode_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TradeNode> __Game_Simulation_TradeNode_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<ElectricityFlowNode> __Game_Simulation_ElectricityFlowNode_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityFlowEdge> __Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Simulation_ElectricityFlowNode_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityFlowNode>();
			__Game_Simulation_ElectricityFlowEdge_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityFlowEdge>();
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_ConnectedFlowEdge_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedFlowEdge>(isReadOnly: true);
			__Game_Simulation_BatteryChargeNode_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BatteryChargeNode>(isReadOnly: true);
			__Game_Simulation_BatteryDischargeNode_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BatteryDischargeNode>(isReadOnly: true);
			__Game_Simulation_TradeNode_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TradeNode>(isReadOnly: true);
			__Game_Simulation_ElectricityFlowNode_RO_ComponentLookup = state.GetComponentLookup<ElectricityFlowNode>(isReadOnly: true);
			__Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup = state.GetComponentLookup<ElectricityFlowEdge>(isReadOnly: true);
		}
	}

	public const int kUpdateInterval = 128;

	public const int kUpdatesPerDay = 2048;

	public const int kUpdatesPerHour = 85;

	public const int kStartFrames = 2;

	public const int kAdjustFrame = 0;

	public const int kPrepareFrame = 1;

	public const int kFlowFrames = 124;

	public const int kFlowCompletionFrame = 125;

	public const int kEndFrames = 2;

	public const int kApplyFrame = 126;

	public const int kStatusFrame = 127;

	public const int kMaxEdgeCapacity = 1073741823;

	private const int kLayerHeight = 20;

	private SimulationSystem m_SimulationSystem;

	private EntityQuery m_NodeGroup;

	private EntityQuery m_EdgeGroup;

	private EntityArchetype m_NodeArchetype;

	private EntityArchetype m_ChargeNodeArchetype;

	private EntityArchetype m_DischargeNodeArchetype;

	private EntityArchetype m_EdgeArchetype;

	private NativeList<Game.Simulation.Flow.Node> m_Nodes;

	private NativeList<Game.Simulation.Flow.Edge> m_Edges;

	private NativeList<Connection> m_Connections;

	private NativeReference<NodeIndices> m_NodeIndices;

	private NativeList<int> m_ChargeNodes;

	private NativeList<int> m_DischargeNodes;

	private NativeList<int> m_TradeNodes;

	private NativeReference<ElectricityFlowJob.State> m_FlowJobState;

	private NativeReference<MaxFlowSolverState> m_SolverState;

	private NativeList<LayerState> m_LayerStates;

	private NativeList<CutElement> m_LayerElements;

	private NativeList<CutElementRef> m_LayerElementRefs;

	private Entity m_SourceNode;

	private Entity m_SinkNode;

	private Entity m_LegacyOutsideSourceNode;

	private Entity m_LegacyOutsideSinkNode;

	private Phase m_NextPhase;

	private JobHandle m_DataDependency;

	private TypeHandle __TypeHandle;

	public bool ready { get; private set; }

	public EntityArchetype nodeArchetype => m_NodeArchetype;

	public EntityArchetype chargeNodeArchetype => m_ChargeNodeArchetype;

	public EntityArchetype dischargeNodeArchetype => m_DischargeNodeArchetype;

	public EntityArchetype edgeArchetype => m_EdgeArchetype;

	public Entity sourceNode => m_SourceNode;

	public Entity sinkNode => m_SinkNode;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_NodeGroup = GetEntityQuery(ComponentType.ReadWrite<ElectricityFlowNode>(), ComponentType.ReadOnly<ConnectedFlowEdge>(), ComponentType.Exclude<Deleted>());
		m_EdgeGroup = GetEntityQuery(ComponentType.ReadWrite<ElectricityFlowEdge>(), ComponentType.Exclude<Deleted>());
		m_NodeArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadOnly<ElectricityFlowNode>(), ComponentType.ReadOnly<ConnectedFlowEdge>());
		m_ChargeNodeArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadOnly<ElectricityFlowNode>(), ComponentType.ReadOnly<ConnectedFlowEdge>(), ComponentType.ReadOnly<BatteryChargeNode>());
		m_DischargeNodeArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadOnly<ElectricityFlowNode>(), ComponentType.ReadOnly<ConnectedFlowEdge>(), ComponentType.ReadOnly<BatteryDischargeNode>());
		m_EdgeArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadOnly<ElectricityFlowEdge>());
		m_Nodes = new NativeList<Game.Simulation.Flow.Node>(Allocator.Persistent);
		m_Edges = new NativeList<Game.Simulation.Flow.Edge>(Allocator.Persistent);
		m_Connections = new NativeList<Connection>(Allocator.Persistent);
		m_NodeIndices = new NativeReference<NodeIndices>(Allocator.Persistent);
		m_ChargeNodes = new NativeList<int>(Allocator.Persistent);
		m_DischargeNodes = new NativeList<int>(Allocator.Persistent);
		m_TradeNodes = new NativeList<int>(Allocator.Persistent);
		m_FlowJobState = new NativeReference<ElectricityFlowJob.State>(new ElectricityFlowJob.State(20000), Allocator.Persistent);
		m_SolverState = new NativeReference<MaxFlowSolverState>(default(MaxFlowSolverState), Allocator.Persistent);
		m_LayerStates = new NativeList<LayerState>(Allocator.Persistent);
		m_LayerElements = new NativeList<CutElement>(Allocator.Persistent);
		m_LayerElementRefs = new NativeList<CutElementRef>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_DataDependency.Complete();
		m_Nodes.Dispose();
		m_Edges.Dispose();
		m_Connections.Dispose();
		m_NodeIndices.Dispose();
		m_ChargeNodes.Dispose();
		m_DischargeNodes.Dispose();
		m_TradeNodes.Dispose();
		m_FlowJobState.Dispose();
		m_SolverState.Dispose();
		m_LayerStates.Dispose();
		m_LayerElements.Dispose();
		m_LayerElementRefs.Dispose();
		base.OnDestroy();
	}

	public void Reset()
	{
		m_DataDependency.Complete();
		m_NextPhase = Phase.Prepare;
		m_FlowJobState.Value = new ElectricityFlowJob.State(20000);
		ready = false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_NextPhase == Phase.Prepare)
		{
			PreparePhase();
		}
		else if (m_NextPhase == Phase.Flow)
		{
			FlowPhase();
		}
		else if (m_NextPhase == Phase.Apply)
		{
			ApplyPhase();
		}
	}

	private void PreparePhase()
	{
		if (m_SimulationSystem.frameIndex % 128 == 1)
		{
			int chunkCapacity = m_NodeArchetype.ChunkCapacity;
			int chunkCapacity2 = m_EdgeArchetype.ChunkCapacity;
			int nodeCount = chunkCapacity * m_NodeGroup.CalculateChunkCountWithoutFiltering();
			int edgeCount = chunkCapacity2 * m_EdgeGroup.CalculateChunkCountWithoutFiltering();
			JobHandle dependsOn = IJobExtensions.Schedule(new PrepareNetworkJob
			{
				m_Nodes = m_Nodes,
				m_Edges = m_Edges,
				m_Connections = m_Connections,
				m_ChargeNodes = m_ChargeNodes,
				m_DischargeNodes = m_DischargeNodes,
				m_TradeNodes = m_TradeNodes,
				m_NodeCount = nodeCount,
				m_EdgeCount = edgeCount
			}, JobHandle.CombineDependencies(base.Dependency, m_DataDependency));
			JobHandle job = JobChunkExtensions.ScheduleParallel(new PrepareNodesJob
			{
				m_FlowNodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_ElectricityFlowNode_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_MaxChunkCapacity = chunkCapacity
			}, m_NodeGroup, base.Dependency);
			JobHandle job2 = JobChunkExtensions.ScheduleParallel(new PrepareEdgesJob
			{
				m_FlowEdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_ElectricityFlowEdge_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_Edges = m_Edges.AsDeferredJobArray(),
				m_MaxChunkCapacity = chunkCapacity2
			}, m_EdgeGroup, dependsOn);
			JobHandle job3 = JobChunkExtensions.Schedule(new PrepareConnectionsJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_ConnectedFlowEdgeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ConnectedFlowEdge_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_ChargeNodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_BatteryChargeNode_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DischargeNodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_BatteryDischargeNode_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TradeNodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_TradeNode_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_FlowNodes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityFlowNode_RO_ComponentLookup, ref base.CheckedStateRef),
				m_FlowEdges = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityFlowEdge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Nodes = m_Nodes.AsDeferredJobArray(),
				m_Connections = m_Connections,
				m_ChargeNodes = m_ChargeNodes,
				m_DischargeNodes = m_DischargeNodes,
				m_TradeNodes = m_TradeNodes,
				m_MaxChunkCapacity = chunkCapacity
			}, m_NodeGroup, JobHandle.CombineDependencies(job, job2));
			JobHandle job4 = IJobExtensions.Schedule(new PopulateNodeIndicesJob
			{
				m_FlowNodes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ElectricityFlowNode_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeIndices = m_NodeIndices,
				m_SourceNode = m_SourceNode,
				m_SinkNode = m_SinkNode
			}, JobHandle.CombineDependencies(job, m_DataDependency));
			base.Dependency = (m_DataDependency = JobHandle.CombineDependencies(job3, job4));
			m_NextPhase = Phase.Flow;
		}
	}

	private void FlowPhase()
	{
		uint num = m_SimulationSystem.frameIndex % 128;
		Assert.IsTrue(num != 0 && num != 1 && num != 126 && num != 127);
		bool flag = num >= 125;
		ElectricityFlowJob jobData = new ElectricityFlowJob
		{
			m_State = m_FlowJobState,
			m_Nodes = m_Nodes.AsDeferredJobArray(),
			m_Edges = m_Edges.AsDeferredJobArray(),
			m_Connections = m_Connections.AsDeferredJobArray(),
			m_NodeIndices = m_NodeIndices,
			m_ChargeNodes = m_ChargeNodes.AsDeferredJobArray(),
			m_DischargeNodes = m_DischargeNodes.AsDeferredJobArray(),
			m_TradeNodes = m_TradeNodes.AsDeferredJobArray(),
			m_SolverState = m_SolverState,
			m_LayerStates = m_LayerStates,
			m_LayerElements = m_LayerElements,
			m_LayerElementRefs = m_LayerElementRefs,
			m_LabelQueue = new NativeQueue<int>(Allocator.TempJob),
			m_LayerHeight = 20,
			m_FrameCount = 1,
			m_FinalFrame = flag
		};
		JobHandle jobHandle = IJobExtensions.Schedule(jobData, m_DataDependency);
		jobData.m_LabelQueue.Dispose(jobHandle);
		m_DataDependency = jobHandle;
		if (flag)
		{
			m_NextPhase = Phase.Apply;
		}
	}

	private void ApplyPhase()
	{
		Assert.IsFalse(m_SimulationSystem.frameIndex % 128 > 126);
		if (m_SimulationSystem.frameIndex % 128 == 126)
		{
			JobHandle dataDependency = JobChunkExtensions.ScheduleParallel(new ApplyEdgesJob
			{
				m_FlowEdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_ElectricityFlowEdge_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_Edges = m_Edges.AsDeferredJobArray()
			}, m_EdgeGroup, JobHandle.CombineDependencies(base.Dependency, m_DataDependency));
			base.Dependency = (m_DataDependency = dataDependency);
			m_NextPhase = Phase.Prepare;
			ready = true;
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		m_DataDependency.Complete();
		Entity value = m_SourceNode;
		writer.Write(value);
		Entity value2 = m_SinkNode;
		writer.Write(value2);
		int lastTotalSteps = m_FlowJobState.Value.m_LastTotalSteps;
		writer.Write(lastTotalSteps);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		m_DataDependency.Complete();
		ref Entity value = ref m_SourceNode;
		reader.Read(out value);
		ref Entity value2 = ref m_SinkNode;
		reader.Read(out value2);
		if (reader.context.version >= Version.electricityTrading && reader.context.version < Version.batteryRework2)
		{
			ref Entity value3 = ref m_LegacyOutsideSourceNode;
			reader.Read(out value3);
			ref Entity value4 = ref m_LegacyOutsideSinkNode;
			reader.Read(out value4);
		}
		else
		{
			m_LegacyOutsideSourceNode = Entity.Null;
			m_LegacyOutsideSinkNode = Entity.Null;
		}
		if (reader.context.version >= Version.waterElectricityID && reader.context.version < Version.electricityImprovements)
		{
			reader.Read(out int value5);
			for (int i = 0; i < value5; i++)
			{
				reader.Read(out Entity _);
			}
			NativeList<int> value7 = new NativeList<int>(Allocator.Temp);
			reader.Read(value7);
		}
		if (reader.context.version >= Version.electricityImprovements && reader.context.version < Version.electricityImprovements2)
		{
			reader.Read(out int _);
		}
		if (reader.context.version >= Version.electricityImprovements2 && reader.context.version < Version.flowJobImprovements)
		{
			reader.Read(out int _);
			reader.Read(out int _);
			reader.Read(out int _);
		}
		if (reader.context.version > Version.flowJobImprovements)
		{
			ref int lastTotalSteps = ref m_FlowJobState.ValueAsRef().m_LastTotalSteps;
			reader.Read(out lastTotalSteps);
		}
	}

	public void SetDefaults(Context context)
	{
		Reset();
		m_SourceNode = Entity.Null;
		m_SinkNode = Entity.Null;
	}

	public void PostDeserialize(Context context)
	{
		if (context.purpose == Purpose.NewMap || (context.purpose == Purpose.NewGame && context.version < Version.timoSerializationFlow) || (context.purpose == Purpose.LoadMap && m_SourceNode == Entity.Null))
		{
			m_SourceNode = base.EntityManager.CreateEntity(m_NodeArchetype);
			m_SinkNode = base.EntityManager.CreateEntity(m_NodeArchetype);
		}
		if (m_LegacyOutsideSourceNode != Entity.Null || m_LegacyOutsideSinkNode != Entity.Null)
		{
			if (m_LegacyOutsideSourceNode != Entity.Null)
			{
				ElectricityGraphUtils.DeleteFlowNode(base.EntityManager, m_LegacyOutsideSourceNode);
			}
			if (m_LegacyOutsideSinkNode != Entity.Null)
			{
				ElectricityGraphUtils.DeleteFlowNode(base.EntityManager, m_LegacyOutsideSinkNode);
			}
			NativeArray<Entity> nativeArray = m_EdgeGroup.ToEntityArray(Allocator.TempJob);
			NativeArray<ElectricityFlowEdge> nativeArray2 = m_EdgeGroup.ToComponentDataArray<ElectricityFlowEdge>(Allocator.TempJob);
			try
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity entity = nativeArray[i];
					ElectricityFlowEdge electricityFlowEdge = nativeArray2[i];
					if (electricityFlowEdge.m_Start == m_LegacyOutsideSourceNode || electricityFlowEdge.m_End == m_LegacyOutsideSourceNode)
					{
						base.EntityManager.AddComponent<Deleted>(entity);
					}
					else if (electricityFlowEdge.m_Start == m_LegacyOutsideSinkNode || electricityFlowEdge.m_End == m_LegacyOutsideSinkNode)
					{
						base.EntityManager.AddComponent<Deleted>(entity);
					}
				}
			}
			finally
			{
				nativeArray.Dispose();
				nativeArray2.Dispose();
			}
			m_LegacyOutsideSourceNode = Entity.Null;
			m_LegacyOutsideSinkNode = Entity.Null;
			EntityQuery entityQuery = base.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ElectricityOutsideConnection>(), ComponentType.ReadOnly<Owner>(), ComponentType.Exclude<Temp>());
			NativeArray<Owner> nativeArray3 = entityQuery.ToComponentDataArray<Owner>(Allocator.TempJob);
			try
			{
				foreach (Owner item in nativeArray3)
				{
					if (base.EntityManager.TryGetComponent<ElectricityNodeConnection>(item.m_Owner, out var component))
					{
						base.EntityManager.AddComponent<TradeNode>(component.m_ElectricityNode);
						ElectricityGraphUtils.CreateFlowEdge(base.EntityManager, m_EdgeArchetype, m_SourceNode, component.m_ElectricityNode, FlowDirection.None, 1073741823);
						ElectricityGraphUtils.CreateFlowEdge(base.EntityManager, m_EdgeArchetype, component.m_ElectricityNode, m_SinkNode, FlowDirection.None, 1073741823);
					}
				}
			}
			finally
			{
				entityQuery.Dispose();
				nativeArray3.Dispose();
			}
		}
		Reset();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public ElectricityFlowSystem()
	{
	}
}
