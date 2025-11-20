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

public static class AvailabilityJobs
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

		public NodeAvailability m_Availability;

		public EdgeID m_EdgeID;

		public EdgeID m_NextID;

		public NodeData(int accessRequirement, NodeAvailability availability, EdgeID edgeID, EdgeID nextID, FullNode pathNode)
		{
			m_PathNode = pathNode;
			m_NextNodeIndex = -1;
			m_Processed = 0;
			m_AccessRequirement = accessRequirement;
			m_Availability = availability;
			m_EdgeID = edgeID;
			m_NextID = nextID;
		}
	}

	private struct HeapData : ILessThan<HeapData>, IComparable<HeapData>
	{
		public float m_Availability;

		public int m_NodeIndex;

		public HeapData(float availability, int nodeIndex)
		{
			m_Availability = availability;
			m_NodeIndex = nodeIndex;
		}

		public bool LessThan(HeapData other)
		{
			return m_Availability > other.m_Availability;
		}

		public int CompareTo(HeapData other)
		{
			return m_NodeIndex - other.m_NodeIndex;
		}
	}

	private struct NodeAvailability
	{
		public float m_Availability;

		public int m_Provider;

		public NodeAvailability(float availability, int provider)
		{
			m_Availability = availability;
			m_Provider = provider;
		}
	}

	private struct ProviderItem
	{
		public float m_Capacity;

		public float m_Cost;
	}

	private struct AvailabilityExecutor
	{
		private UnsafePathfindData m_PathfindData;

		private Allocator m_Allocator;

		private AvailabilityParameters m_Parameters;

		private UnsafeParallelMultiHashMap<Entity, PathTarget> m_ProviderTargets;

		private UnsafeList<ProviderItem> m_Providers;

		private UnsafeList<int> m_ProviderIndex;

		private UnsafeList<int> m_NodeIndex;

		private UnsafeList<int> m_NodeIndexBits;

		private UnsafeMinHeap<HeapData> m_Heap;

		private UnsafeList<NodeData> m_NodeData;

		public void Initialize(NativePathfindData pathfindData, Allocator allocator, AvailabilityParameters parameters)
		{
			m_PathfindData = pathfindData.GetReadOnlyData();
			m_Allocator = allocator;
			m_Parameters = parameters;
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
			if (m_ProviderTargets.IsCreated)
			{
				m_ProviderTargets.Dispose();
			}
			if (m_Providers.IsCreated)
			{
				m_Providers.Dispose();
				m_ProviderIndex.Dispose();
			}
		}

		public void AddSources(ref UnsafeQueue<PathTarget> pathTargets)
		{
			int count = pathTargets.Count;
			m_ProviderTargets = new UnsafeParallelMultiHashMap<Entity, PathTarget>(count, m_Allocator);
			PathTarget item;
			while (pathTargets.TryDequeue(out item))
			{
				m_ProviderTargets.Add(item.m_Target, item);
			}
		}

		public void AddProviders(ref UnsafeQueue<AvailabilityProvider> availabilityProviders)
		{
			int count = availabilityProviders.Count;
			m_Providers = new UnsafeList<ProviderItem>(count, m_Allocator);
			m_ProviderIndex = new UnsafeList<int>(count, m_Allocator);
			for (int i = 0; i < count; i++)
			{
				AvailabilityProvider availabilityProvider = availabilityProviders.Dequeue();
				ProviderItem value = new ProviderItem
				{
					m_Capacity = availabilityProvider.m_Capacity,
					m_Cost = availabilityProvider.m_Cost * m_Parameters.m_CostFactor
				};
				m_Providers.Add(in value);
				m_ProviderIndex.Add(in i);
				float num = 0f;
				if (m_ProviderTargets.TryGetFirstValue(availabilityProvider.m_Provider, out var item, out var it))
				{
					do
					{
						if (m_PathfindData.m_PathEdges.TryGetValue(item.m_Entity, out var item2))
						{
							ref Edge edge = ref m_PathfindData.GetEdge(item2);
							bool3 directions = new bool3((edge.m_Specification.m_Flags & EdgeFlags.Forward) != 0 || item.m_Delta == 1f, (edge.m_Specification.m_Flags & EdgeFlags.AllowMiddle) != 0, (edge.m_Specification.m_Flags & EdgeFlags.Backward) != 0 || item.m_Delta == 0f);
							num += AddConnections(item2, in edge, value, i, item.m_Delta, directions);
						}
					}
					while (m_ProviderTargets.TryGetNextValue(out item, ref it));
				}
				value = m_Providers[i];
				value.m_Cost += num;
				m_Providers[i] = value;
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

		public bool FindAvailabilityNodes()
		{
			HeapData heapData;
			while (HeapExtract(out heapData))
			{
				ref NodeData reference = ref m_NodeData.ElementAt(heapData.m_NodeIndex);
				if (reference.m_Processed != 0)
				{
					continue;
				}
				reference.m_Processed = 1;
				int providerIndex = GetProviderIndex(reference.m_Availability.m_Provider);
				ProviderItem providerItem = m_Providers[providerIndex];
				float num = 0f;
				if (reference.m_NextID.m_Index != -1)
				{
					ref Edge edge = ref m_PathfindData.GetEdge(reference.m_NextID);
					num += CheckNextEdge(reference.m_NextID, reference.m_PathNode, in edge, providerItem, providerIndex);
				}
				else
				{
					int connectionCount = m_PathfindData.GetConnectionCount(reference.m_PathNode.m_NodeID);
					int2 @int = new int2(-1, reference.m_AccessRequirement);
					FullNode pathNode = reference.m_PathNode;
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
								num += CheckNextEdge(edgeID2, pathNode, in edge2, providerItem, providerIndex);
							}
						}
					}
				}
				m_Providers.ElementAt(providerIndex).m_Cost += num;
			}
			return m_NodeData.Length != 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private float CheckNextEdge(EdgeID nextID, FullNode pathNode, in Edge edge, ProviderItem providerItem, int providerIndex)
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
					return 0f;
				}
				startDelta = pathNode.m_CurvePos;
				directions = new bool3((edge.m_Specification.m_Flags & EdgeFlags.Forward) != 0, (edge.m_Specification.m_Flags & EdgeFlags.AllowMiddle) != 0, (edge.m_Specification.m_Flags & EdgeFlags.Backward) != 0);
			}
			return AddConnections(nextID, in edge, providerItem, providerIndex, startDelta, directions);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private float AddConnections(EdgeID id, in Edge edge, ProviderItem providerItem, int providerIndex, float startDelta, bool3 directions)
		{
			float num = 0f;
			if (directions.x)
			{
				float num2 = PathUtils.CalculateCost(in edge.m_Specification, in m_Parameters, new float2(startDelta, 1f));
				num += num2;
				AddHeapData(id, in edge, GetAvailability(providerItem, num2), providerIndex, new FullNode(edge.m_EndID, edge.m_EndCurvePos));
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
								float num3 = PathUtils.CalculateCost(in edge.m_Specification, in m_Parameters, new float2(startDelta, startCurvePos));
								num += num3;
								AddHeapData(id, edgeID, in edge, GetAvailability(providerItem, num3), providerIndex, new FullNode(edge.m_StartID, startCurvePos));
							}
						}
						if (edge.m_MiddleID.Equals(edge2.m_EndID) & ((edge2.m_Specification.m_Flags & EdgeFlags.Backward) != 0))
						{
							float endCurvePos = edge2.m_EndCurvePos;
							if ((directions.x && endCurvePos >= startDelta) | (directions.z && endCurvePos <= startDelta))
							{
								float num4 = PathUtils.CalculateCost(in edge.m_Specification, in m_Parameters, new float2(startDelta, endCurvePos));
								num += num4;
								AddHeapData(id, edgeID, in edge, GetAvailability(providerItem, num4), providerIndex, new FullNode(edge.m_EndID, endCurvePos));
							}
						}
					}
				}
			}
			if (directions.z)
			{
				float num5 = PathUtils.CalculateCost(in edge.m_Specification, in m_Parameters, new float2(startDelta, 0f));
				num += num5;
				AddHeapData(id, in edge, GetAvailability(providerItem, num5), providerIndex, new FullNode(edge.m_StartID, edge.m_StartCurvePos));
			}
			return num;
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
		private void AddHeapData(EdgeID id, in Edge edge, float availability, int providerIndex, FullNode pathNode)
		{
			if (GetOrAddNodeIndex(pathNode, out var nodeIndex))
			{
				ref NodeData reference = ref m_NodeData.ElementAt(nodeIndex);
				int providerIndex2 = GetProviderIndex(reference.m_Availability.m_Provider);
				if (providerIndex != providerIndex2)
				{
					MergeProviders(providerIndex, providerIndex2);
				}
				if (reference.m_Processed == 0 && availability < reference.m_Availability.m_Availability)
				{
					reference = new NodeData(edge.m_Specification.m_AccessRequirement, new NodeAvailability(availability, providerIndex), id, new EdgeID
					{
						m_Index = -1
					}, pathNode);
					HeapInsert(new HeapData(availability, nodeIndex));
				}
			}
			else
			{
				m_NodeData.Add(new NodeData(edge.m_Specification.m_AccessRequirement, new NodeAvailability(availability, providerIndex), id, new EdgeID
				{
					m_Index = -1
				}, pathNode));
				HeapInsert(new HeapData(availability, nodeIndex));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void AddHeapData(EdgeID id, EdgeID id2, in Edge edge, float availability, int providerIndex, FullNode pathNode)
		{
			if (GetOrAddNodeIndex(pathNode, out var nodeIndex))
			{
				ref NodeData reference = ref m_NodeData.ElementAt(nodeIndex);
				int providerIndex2 = GetProviderIndex(reference.m_Availability.m_Provider);
				if (providerIndex != providerIndex2)
				{
					MergeProviders(providerIndex, providerIndex2);
				}
				if (reference.m_Processed == 0)
				{
					if (availability < reference.m_Availability.m_Availability)
					{
						reference = new NodeData(edge.m_Specification.m_AccessRequirement, new NodeAvailability(availability, providerIndex), id, id2, pathNode);
						HeapInsert(new HeapData(availability, nodeIndex));
					}
					else if (!id2.Equals(reference.m_NextID))
					{
						reference.m_NextID.m_Index = -1;
					}
				}
			}
			else
			{
				m_NodeData.Add(new NodeData(edge.m_Specification.m_AccessRequirement, new NodeAvailability(availability, providerIndex), id, id2, pathNode));
				HeapInsert(new HeapData(availability, nodeIndex));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool DisallowConnection(PathSpecification newSpec)
		{
			if ((newSpec.m_Methods & PathMethod.Road) == 0)
			{
				return true;
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void MergeProviders(int providerIndex1, int providerIndex2)
		{
			ProviderItem value = m_Providers[providerIndex1];
			ProviderItem providerItem = m_Providers[providerIndex2];
			value.m_Capacity += providerItem.m_Capacity;
			value.m_Cost += providerItem.m_Cost;
			value.m_Capacity *= (1f + value.m_Cost) / (2f + value.m_Cost);
			m_Providers[providerIndex1] = value;
			m_ProviderIndex[providerIndex2] = providerIndex1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private float GetAvailability(ProviderItem providerItem, float cost)
		{
			return providerItem.m_Capacity / (1f + providerItem.m_Cost + cost);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int GetProviderIndex(int storedIndex)
		{
			int num = m_ProviderIndex[storedIndex];
			if (num != storedIndex)
			{
				int index = storedIndex;
				storedIndex = num;
				num = m_ProviderIndex[storedIndex];
				if (num != storedIndex)
				{
					do
					{
						storedIndex = num;
						num = m_ProviderIndex[storedIndex];
					}
					while (num != storedIndex);
					m_ProviderIndex[index] = num;
				}
			}
			return num;
		}

		public void FillResults(ref UnsafeList<AvailabilityResult> results)
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
							if (reference2.m_Processed != 0)
							{
								AvailabilityResult value = new AvailabilityResult
								{
									m_Target = edge.m_Owner,
									m_Availability = NormalizeAvailability(new float2(reference.m_Availability.m_Availability, reference2.m_Availability.m_Availability), m_Parameters)
								};
								results.Add(in value);
							}
						}
					}
					else if (reference.m_PathNode.Equals(new FullNode(edge.m_EndID, edge.m_EndCurvePos)) && (edge.m_Specification.m_Flags & (EdgeFlags.Forward | EdgeFlags.Backward)) == EdgeFlags.Backward && TryGetNodeIndex(new FullNode(edge.m_StartID, edge.m_StartCurvePos), out nodeIndex2))
					{
						ref NodeData reference3 = ref m_NodeData.ElementAt(nodeIndex2);
						if (reference3.m_Processed != 0)
						{
							AvailabilityResult value2 = new AvailabilityResult
							{
								m_Target = edge.m_Owner,
								m_Availability = NormalizeAvailability(new float2(reference3.m_Availability.m_Availability, reference.m_Availability.m_Availability), m_Parameters)
							};
							results.Add(in value2);
						}
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private float2 NormalizeAvailability(float2 availability, AvailabilityParameters availabilityParameters)
		{
			return availability * availabilityParameters.m_ResultFactor;
		}
	}

	[BurstCompile]
	public struct AvailabilityJob : IJob
	{
		[ReadOnly]
		public NativePathfindData m_PathfindData;

		public AvailabilityAction m_Action;

		public void Execute()
		{
			Execute(m_PathfindData, Allocator.Temp, ref m_Action.data);
		}

		public static void Execute(NativePathfindData pathfindData, Allocator allocator, ref AvailabilityActionData actionData)
		{
			if (!actionData.m_Providers.IsEmpty())
			{
				AvailabilityExecutor availabilityExecutor = default(AvailabilityExecutor);
				availabilityExecutor.Initialize(pathfindData, allocator, actionData.m_Parameters);
				availabilityExecutor.AddSources(ref actionData.m_Sources);
				availabilityExecutor.AddProviders(ref actionData.m_Providers);
				if (availabilityExecutor.FindAvailabilityNodes())
				{
					availabilityExecutor.FillResults(ref actionData.m_Results);
				}
				availabilityExecutor.Release();
			}
		}
	}

	public struct ResultItem
	{
		public Entity m_Owner;

		public UnsafeList<AvailabilityResult> m_Results;
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
		public BufferLookup<AvailabilityElement> m_AvailabilityElements;

		public void Execute(int index)
		{
			ResultItem resultItem = m_ResultItems[index];
			if (!m_AvailabilityElements.HasBuffer(resultItem.m_Owner))
			{
				return;
			}
			NativeParallelHashMap<Entity, float2> nativeParallelHashMap = new NativeParallelHashMap<Entity, float2>(1000, Allocator.Temp);
			for (int i = 0; i < resultItem.m_Results.Length; i++)
			{
				AvailabilityResult availabilityResult = resultItem.m_Results[i];
				if (!m_EdgeLaneData.HasComponent(availabilityResult.m_Target) || !m_OwnerData.HasComponent(availabilityResult.m_Target))
				{
					continue;
				}
				EdgeLane edgeLane = m_EdgeLaneData[availabilityResult.m_Target];
				Owner owner = m_OwnerData[availabilityResult.m_Target];
				float2 availability = GetAvailability(availabilityResult.m_Availability, edgeLane.m_EdgeDelta);
				if (nativeParallelHashMap.TryGetValue(owner.m_Owner, out var item))
				{
					availability = math.max(item, availability);
					if (math.any(availability != item))
					{
						nativeParallelHashMap[owner.m_Owner] = availability;
					}
				}
				else
				{
					nativeParallelHashMap.Add(owner.m_Owner, availability);
				}
			}
			DynamicBuffer<AvailabilityElement> dynamicBuffer = m_AvailabilityElements[resultItem.m_Owner];
			dynamicBuffer.Clear();
			NativeParallelHashMap<Entity, float2>.Enumerator enumerator = nativeParallelHashMap.GetEnumerator();
			while (enumerator.MoveNext())
			{
				dynamicBuffer.Add(new AvailabilityElement
				{
					m_Edge = enumerator.Current.Key,
					m_Availability = enumerator.Current.Value
				});
			}
			enumerator.Dispose();
			nativeParallelHashMap.Dispose();
		}

		private static float2 GetAvailability(float2 availability, float2 edgeDelta)
		{
			float2 @float = new float2(0f, 1f);
			return math.select(math.select(0f, availability.yx, edgeDelta.yx == @float), availability, edgeDelta == @float);
		}
	}
}
