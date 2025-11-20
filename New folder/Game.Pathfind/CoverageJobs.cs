using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Game.Net;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Pathfind;

public static class CoverageJobs
{
	private struct FullNode : IEquatable<FullNode>
	{
		public NodeID m_NodeID;

		public float m_CurvePos;

		public FullNode(NodeID nodeID, float curvePos)
		{
			m_NodeID = nodeID;
			m_CurvePos = curvePos;
		}

		public bool Equals(FullNode other)
		{
			if (m_NodeID.Equals(other.m_NodeID))
			{
				return m_CurvePos == other.m_CurvePos;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (m_NodeID.m_Index >> 2) ^ math.asint(m_CurvePos);
		}
	}

	private struct NodeData
	{
		public FullNode m_PathNode;

		public int m_NextNodeIndex;

		public int m_Processed;

		public int m_AccessRequirement;

		public float2 m_Costs;

		public EdgeID m_EdgeID;

		public EdgeID m_NextID;

		public NodeData(int accessRequirement, float cost, float distance, EdgeID edgeID, EdgeID nextID, FullNode pathNode)
		{
			m_PathNode = pathNode;
			m_NextNodeIndex = -1;
			m_Processed = 0;
			m_AccessRequirement = accessRequirement;
			m_Costs = new float2(cost, distance);
			m_EdgeID = edgeID;
			m_NextID = nextID;
		}
	}

	private struct HeapData : ILessThan<HeapData>, IComparable<HeapData>
	{
		public float m_Cost;

		public int m_NodeIndex;

		public HeapData(float cost, int nodeIndex)
		{
			m_Cost = cost;
			m_NodeIndex = nodeIndex;
		}

		public bool LessThan(HeapData other)
		{
			return m_Cost < other.m_Cost;
		}

		public int CompareTo(HeapData other)
		{
			return m_NodeIndex - other.m_NodeIndex;
		}
	}

	private struct CoverageExecutor
	{
		private UnsafePathfindData m_PathfindData;

		private CoverageParameters m_Parameters;

		private float4 m_MinDistance;

		private float4 m_MaxDistance;

		private UnsafeList<int> m_NodeIndex;

		private UnsafeList<int> m_NodeIndexBits;

		private UnsafeMinHeap<HeapData> m_Heap;

		private UnsafeList<NodeData> m_NodeData;

		public void Initialize(NativePathfindData pathfindData, Allocator allocator, CoverageParameters parameters)
		{
			m_PathfindData = pathfindData.GetReadOnlyData();
			m_Parameters = parameters;
			m_MinDistance = parameters.m_Range * new float4(0f, 0.6f, 0f, 0.6f);
			m_MaxDistance = parameters.m_Range * new float4(2f, 1.2f, 2f, 1.2f);
			int num = (math.max(m_PathfindData.GetNodeIDSize(), m_PathfindData.m_Edges.Length) >> 5) + 1;
			int num2 = (num >> 5) + 1;
			m_NodeIndex = new UnsafeList<int>(num, allocator);
			m_NodeIndexBits = new UnsafeList<int>(num2, allocator);
			m_Heap = new UnsafeMinHeap<HeapData>(1000, allocator);
			m_NodeData = new UnsafeList<NodeData>(10000, allocator);
			m_NodeIndex.Resize(num);
			m_NodeIndexBits.Resize(num2, NativeArrayOptions.ClearMemory);
		}

		public void Release()
		{
			if (m_NodeData.IsCreated)
			{
				m_NodeIndex.Dispose();
				m_NodeIndexBits.Dispose();
				m_Heap.Dispose();
				m_NodeData.Dispose();
			}
		}

		public void AddSources(ref UnsafeQueue<PathTarget> pathTargets)
		{
			PathTarget item;
			while (pathTargets.TryDequeue(out item))
			{
				if (m_PathfindData.m_PathEdges.TryGetValue(item.m_Entity, out var item2))
				{
					ref Edge edge = ref m_PathfindData.GetEdge(item2);
					bool3 directions = new bool3((edge.m_Specification.m_Flags & EdgeFlags.Forward) != 0 || item.m_Delta == 1f, (edge.m_Specification.m_Flags & EdgeFlags.AllowMiddle) != 0, (edge.m_Specification.m_Flags & EdgeFlags.Backward) != 0 || item.m_Delta == 0f);
					AddConnections(item2, in edge, new float2(item.m_Cost, 0f), item.m_Delta, directions);
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool HeapExtract(out HeapData heapData)
		{
			if (m_Heap.Length != 0)
			{
				heapData = m_Heap.Extract();
				return true;
			}
			heapData = default(HeapData);
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void HeapInsert(HeapData heapData)
		{
			m_Heap.Insert(heapData);
		}

		public bool FindCoveredNodes()
		{
			bool result = false;
			HeapData heapData;
			while (HeapExtract(out heapData))
			{
				ref NodeData reference = ref m_NodeData.ElementAt(heapData.m_NodeIndex);
				if (reference.m_Processed != 0)
				{
					continue;
				}
				reference.m_Processed = 1;
				result = true;
				if (!(reference.m_Costs.y < m_MaxDistance.y))
				{
					continue;
				}
				if (reference.m_NextID.m_Index != -1)
				{
					ref Edge edge = ref m_PathfindData.GetEdge(reference.m_NextID);
					CheckNextEdge(reference.m_NextID, reference.m_PathNode, reference.m_Costs, in edge);
					continue;
				}
				int connectionCount = m_PathfindData.GetConnectionCount(reference.m_PathNode.m_NodeID);
				int2 @int = new int2(-1, reference.m_AccessRequirement);
				FullNode pathNode = reference.m_PathNode;
				float2 costs = reference.m_Costs;
				EdgeID edgeID = reference.m_EdgeID;
				for (int i = 0; i < connectionCount; i++)
				{
					EdgeID edgeID2 = new EdgeID
					{
						m_Index = m_PathfindData.GetConnection(pathNode.m_NodeID, i)
					};
					int accessRequirement = m_PathfindData.GetAccessRequirement(pathNode.m_NodeID, i);
					if (!edgeID.Equals(edgeID2) && !math.all(@int != accessRequirement))
					{
						ref Edge edge2 = ref m_PathfindData.GetEdge(edgeID2);
						if (!DisallowConnection(edge2.m_Specification))
						{
							CheckNextEdge(edgeID2, pathNode, costs, in edge2);
						}
					}
				}
			}
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void CheckNextEdge(EdgeID nextID, FullNode pathNode, float2 baseCosts, in Edge edge)
		{
			float startDelta;
			bool3 directions;
			if (pathNode.Equals(new FullNode(edge.m_StartID, edge.m_StartCurvePos)))
			{
				startDelta = 0f;
				directions = new bool3((edge.m_Specification.m_Flags & EdgeFlags.Forward) != 0, (edge.m_Specification.m_Flags & (EdgeFlags.Forward | EdgeFlags.AllowMiddle)) == (EdgeFlags.Forward | EdgeFlags.AllowMiddle), z: false);
			}
			else if (pathNode.Equals(new FullNode(edge.m_EndID, edge.m_EndCurvePos)))
			{
				startDelta = 1f;
				directions = new bool3(x: false, (edge.m_Specification.m_Flags & (EdgeFlags.Backward | EdgeFlags.AllowMiddle)) == (EdgeFlags.Backward | EdgeFlags.AllowMiddle), (edge.m_Specification.m_Flags & EdgeFlags.Backward) != 0);
			}
			else
			{
				if (!pathNode.m_NodeID.Equals(edge.m_MiddleID))
				{
					return;
				}
				startDelta = pathNode.m_CurvePos;
				directions = new bool3((edge.m_Specification.m_Flags & EdgeFlags.Forward) != 0, (edge.m_Specification.m_Flags & EdgeFlags.AllowMiddle) != 0, (edge.m_Specification.m_Flags & EdgeFlags.Backward) != 0);
			}
			AddConnections(nextID, in edge, baseCosts, startDelta, directions);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void AddConnections(EdgeID id, in Edge edge, float2 baseCosts, float startDelta, bool3 directions)
		{
			if (directions.x)
			{
				AddHeapData(pathNode: new FullNode(edge.m_EndID, edge.m_EndCurvePos), edgeDelta: new float2(startDelta, 1f), id: id, edge: in edge, baseCosts: baseCosts);
			}
			if (directions.y)
			{
				int connectionCount = m_PathfindData.GetConnectionCount(edge.m_MiddleID);
				if (connectionCount != 0)
				{
					int2 @int = new int2(-1, edge.m_Specification.m_AccessRequirement);
					for (int i = 0; i < connectionCount; i++)
					{
						EdgeID edgeID = new EdgeID
						{
							m_Index = m_PathfindData.GetConnection(edge.m_MiddleID, i)
						};
						int accessRequirement = m_PathfindData.GetAccessRequirement(edge.m_MiddleID, i);
						if (id.Equals(edgeID) || math.all(@int != accessRequirement))
						{
							continue;
						}
						ref Edge edge2 = ref m_PathfindData.GetEdge(edgeID);
						if (DisallowConnection(edge2.m_Specification))
						{
							continue;
						}
						if (edge.m_MiddleID.Equals(edge2.m_StartID) & ((edge2.m_Specification.m_Flags & EdgeFlags.Forward) != 0))
						{
							float startCurvePos = edge2.m_StartCurvePos;
							if ((directions.x && startCurvePos >= startDelta) | (directions.z && startCurvePos <= startDelta))
							{
								AddHeapData(pathNode: new FullNode(edge2.m_StartID, startCurvePos), edgeDelta: new float2(startDelta, startCurvePos), id: id, id2: edgeID, edge: in edge, baseCosts: baseCosts);
							}
						}
						if (edge.m_MiddleID.Equals(edge2.m_EndID) & ((edge2.m_Specification.m_Flags & EdgeFlags.Backward) != 0))
						{
							float endCurvePos = edge2.m_EndCurvePos;
							if ((directions.x && endCurvePos >= startDelta) | (directions.z && endCurvePos <= startDelta))
							{
								AddHeapData(pathNode: new FullNode(edge2.m_EndID, endCurvePos), edgeDelta: new float2(startDelta, endCurvePos), id: id, id2: edgeID, edge: in edge, baseCosts: baseCosts);
							}
						}
					}
				}
			}
			if (directions.z)
			{
				AddHeapData(pathNode: new FullNode(edge.m_StartID, edge.m_StartCurvePos), edgeDelta: new float2(startDelta, 0f), id: id, edge: in edge, baseCosts: baseCosts);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool GetOrAddNodeIndex(FullNode pathNode, out int nodeIndex)
		{
			int num = math.abs(pathNode.m_NodeID.m_Index) >> 5;
			int index = num >> 5;
			int num2 = 1 << (num & 0x1F);
			if ((m_NodeIndexBits[index] & num2) != 0)
			{
				nodeIndex = m_NodeIndex[num];
				ref NodeData reference = ref m_NodeData.ElementAt(nodeIndex);
				while (true)
				{
					if (reference.m_PathNode.Equals(pathNode))
					{
						return true;
					}
					nodeIndex = reference.m_NextNodeIndex;
					if (nodeIndex == -1)
					{
						break;
					}
					reference = ref m_NodeData.ElementAt(nodeIndex);
				}
				nodeIndex = m_NodeData.Length;
				reference.m_NextNodeIndex = nodeIndex;
			}
			else
			{
				m_NodeIndexBits[index] |= num2;
				nodeIndex = m_NodeData.Length;
				m_NodeIndex[num] = nodeIndex;
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool TryGetNodeIndex(FullNode pathNode, out int nodeIndex)
		{
			int num = math.abs(pathNode.m_NodeID.m_Index) >> 5;
			int index = num >> 5;
			int num2 = 1 << (num & 0x1F);
			if ((m_NodeIndexBits[index] & num2) != 0)
			{
				nodeIndex = m_NodeIndex[num];
				ref NodeData reference = ref m_NodeData.ElementAt(nodeIndex);
				while (true)
				{
					if (reference.m_PathNode.Equals(pathNode))
					{
						return true;
					}
					nodeIndex = reference.m_NextNodeIndex;
					if (nodeIndex == -1)
					{
						break;
					}
					reference = ref m_NodeData.ElementAt(nodeIndex);
				}
			}
			else
			{
				nodeIndex = -1;
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void AddHeapData(EdgeID id, in Edge edge, float2 baseCosts, FullNode pathNode, float2 edgeDelta)
		{
			if (GetOrAddNodeIndex(pathNode, out var nodeIndex))
			{
				ref NodeData reference = ref m_NodeData.ElementAt(nodeIndex);
				if (reference.m_Processed == 0)
				{
					float num = baseCosts.x + PathUtils.CalculateCost(in edge.m_Specification, in m_Parameters, edgeDelta);
					if (num < reference.m_Costs.x)
					{
						float distance = baseCosts.y + edge.m_Specification.m_Length * math.abs(edgeDelta.x - edgeDelta.y);
						reference = new NodeData(edge.m_Specification.m_AccessRequirement, num, distance, id, new EdgeID
						{
							m_Index = -1
						}, pathNode);
						HeapInsert(new HeapData(num, nodeIndex));
					}
				}
			}
			else
			{
				float cost = baseCosts.x + PathUtils.CalculateCost(in edge.m_Specification, in m_Parameters, edgeDelta);
				float distance2 = baseCosts.y + edge.m_Specification.m_Length * math.abs(edgeDelta.x - edgeDelta.y);
				m_NodeData.Add(new NodeData(edge.m_Specification.m_AccessRequirement, cost, distance2, id, new EdgeID
				{
					m_Index = -1
				}, pathNode));
				HeapInsert(new HeapData(cost, nodeIndex));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void AddHeapData(EdgeID id, EdgeID id2, in Edge edge, float2 baseCosts, FullNode pathNode, float2 edgeDelta)
		{
			if (GetOrAddNodeIndex(pathNode, out var nodeIndex))
			{
				ref NodeData reference = ref m_NodeData.ElementAt(nodeIndex);
				if (reference.m_Processed == 0)
				{
					float num = baseCosts.x + PathUtils.CalculateCost(in edge.m_Specification, in m_Parameters, edgeDelta);
					if (num < reference.m_Costs.x)
					{
						float distance = baseCosts.y + edge.m_Specification.m_Length * math.abs(edgeDelta.x - edgeDelta.y);
						reference = new NodeData(edge.m_Specification.m_AccessRequirement, num, distance, id, id2, pathNode);
						HeapInsert(new HeapData(num, nodeIndex));
					}
					else if (!id2.Equals(reference.m_NextID))
					{
						reference.m_NextID.m_Index = -1;
					}
				}
			}
			else
			{
				float cost = baseCosts.x + PathUtils.CalculateCost(in edge.m_Specification, in m_Parameters, edgeDelta);
				float distance2 = baseCosts.y + edge.m_Specification.m_Length * math.abs(edgeDelta.x - edgeDelta.y);
				m_NodeData.Add(new NodeData(edge.m_Specification.m_AccessRequirement, cost, distance2, id, id2, pathNode));
				HeapInsert(new HeapData(cost, nodeIndex));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool DisallowConnection(PathSpecification newSpec)
		{
			if ((newSpec.m_Methods & m_Parameters.m_Methods) == 0)
			{
				return true;
			}
			return false;
		}

		public void FillResults(ref UnsafeList<CoverageResult> results)
		{
			for (int i = 0; i < m_NodeData.Length; i++)
			{
				ref NodeData reference = ref m_NodeData.ElementAt(i);
				if (reference.m_Processed == 0)
				{
					continue;
				}
				int connectionCount = m_PathfindData.GetConnectionCount(reference.m_PathNode.m_NodeID);
				for (int j = 0; j < connectionCount; j++)
				{
					EdgeID edgeID = new EdgeID
					{
						m_Index = m_PathfindData.GetConnection(reference.m_PathNode.m_NodeID, j)
					};
					ref Edge edge = ref m_PathfindData.GetEdge(edgeID);
					int nodeIndex2;
					if (reference.m_PathNode.Equals(new FullNode(edge.m_StartID, edge.m_StartCurvePos)) && (edge.m_Specification.m_Flags & EdgeFlags.Forward) != 0)
					{
						if (TryGetNodeIndex(new FullNode(edge.m_EndID, edge.m_EndCurvePos), out var nodeIndex))
						{
							ref NodeData reference2 = ref m_NodeData.ElementAt(nodeIndex);
							if (reference2.m_Processed != 0 && math.min(reference.m_Costs.y, reference2.m_Costs.y) < m_MaxDistance.y)
							{
								float4 @float = (new float4(reference.m_Costs, reference2.m_Costs) - m_MinDistance) / (m_MaxDistance - m_MinDistance);
								CoverageResult value = new CoverageResult
								{
									m_Target = edge.m_Owner,
									m_TargetCost = math.saturate(math.max(@float.xz, @float.yw))
								};
								results.Add(in value);
							}
						}
					}
					else if (reference.m_PathNode.Equals(new FullNode(edge.m_EndID, edge.m_EndCurvePos)) && (edge.m_Specification.m_Flags & (EdgeFlags.Forward | EdgeFlags.Backward)) == EdgeFlags.Backward && TryGetNodeIndex(new FullNode(edge.m_StartID, edge.m_StartCurvePos), out nodeIndex2))
					{
						ref NodeData reference3 = ref m_NodeData.ElementAt(nodeIndex2);
						if (reference3.m_Processed != 0 && math.min(reference.m_Costs.y, reference3.m_Costs.y) < m_MaxDistance.y)
						{
							float4 float2 = (new float4(reference3.m_Costs, reference.m_Costs) - m_MinDistance) / (m_MaxDistance - m_MinDistance);
							CoverageResult value2 = new CoverageResult
							{
								m_Target = edge.m_Owner,
								m_TargetCost = math.saturate(math.max(float2.xz, float2.yw))
							};
							results.Add(in value2);
						}
					}
				}
			}
		}
	}

	[BurstCompile]
	public struct CoverageJob : IJob
	{
		[ReadOnly]
		public NativePathfindData m_PathfindData;

		public CoverageAction m_Action;

		public void Execute()
		{
			Execute(m_PathfindData, Allocator.Temp, ref m_Action.data);
		}

		public static void Execute(NativePathfindData pathfindData, Allocator allocator, ref CoverageActionData actionData)
		{
			if (!actionData.m_Sources.IsEmpty())
			{
				CoverageExecutor coverageExecutor = default(CoverageExecutor);
				coverageExecutor.Initialize(pathfindData, allocator, actionData.m_Parameters);
				coverageExecutor.AddSources(ref actionData.m_Sources);
				if (coverageExecutor.FindCoveredNodes())
				{
					coverageExecutor.FillResults(ref actionData.m_Results);
				}
				coverageExecutor.Release();
			}
		}
	}

	public struct ResultItem
	{
		public Entity m_Owner;

		public UnsafeList<CoverageResult> m_Results;
	}

	[BurstCompile]
	public struct ProcessResultsJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeList<ResultItem> m_ResultItems;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<EdgeLane> m_EdgeLaneData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<CoverageElement> m_CoverageElements;

		public void Execute(int index)
		{
			ResultItem resultItem = m_ResultItems[index];
			if (!m_CoverageElements.HasBuffer(resultItem.m_Owner))
			{
				return;
			}
			NativeParallelHashMap<Entity, float2> nativeParallelHashMap = new NativeParallelHashMap<Entity, float2>(100, Allocator.Temp);
			NativeList<Entity> nativeList = new NativeList<Entity>(Allocator.Temp);
			for (int i = 0; i < resultItem.m_Results.Length; i++)
			{
				CoverageResult coverageResult = resultItem.m_Results[i];
				if (!m_EdgeLaneData.HasComponent(coverageResult.m_Target))
				{
					continue;
				}
				EdgeLane edgeLane = m_EdgeLaneData[coverageResult.m_Target];
				Owner owner = m_OwnerData[coverageResult.m_Target];
				float2 cost = GetCost(coverageResult.m_TargetCost, edgeLane.m_EdgeDelta);
				if (nativeParallelHashMap.TryGetValue(owner.m_Owner, out var item))
				{
					if (math.any(cost < item))
					{
						cost = math.min(item, cost);
						nativeParallelHashMap.Remove(owner.m_Owner);
						nativeParallelHashMap.TryAdd(owner.m_Owner, cost);
					}
				}
				else
				{
					nativeParallelHashMap.TryAdd(owner.m_Owner, cost);
					nativeList.Add(in owner.m_Owner);
				}
			}
			DynamicBuffer<CoverageElement> dynamicBuffer = m_CoverageElements[resultItem.m_Owner];
			dynamicBuffer.Clear();
			for (int j = 0; j < nativeList.Length; j++)
			{
				CoverageElement elem = new CoverageElement
				{
					m_Edge = nativeList[j]
				};
				if (nativeParallelHashMap.TryGetValue(elem.m_Edge, out elem.m_Cost))
				{
					dynamicBuffer.Add(elem);
				}
			}
			nativeParallelHashMap.Dispose();
			nativeList.Dispose();
		}

		private static float2 GetCost(float2 cost, float2 edgeDelta)
		{
			float2 @float = new float2(0f, 1f);
			return math.select(math.select(float.MaxValue, cost.yx, edgeDelta.yx == @float), cost, edgeDelta == @float);
		}
	}
}
