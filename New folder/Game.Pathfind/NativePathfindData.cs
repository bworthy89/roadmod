using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Game.Pathfind;

[NativeContainer]
[GenerateTestsForBurstCompatibility]
public struct NativePathfindData : IDisposable
{
	[NativeDisableUnsafePtrRestriction]
	internal unsafe UnsafePathfindData* m_PathfindData;

	internal Allocator m_AllocatorLabel;

	public unsafe bool IsCreated => m_PathfindData != null;

	public unsafe int Size => m_PathfindData->m_Edges.Length - m_PathfindData->m_FreeIDs.Length;

	public NativePathfindData(Allocator allocator)
		: this(allocator, 2)
	{
	}

	private unsafe NativePathfindData(Allocator allocator, int disposeSentinelStackDepth)
	{
		m_PathfindData = (UnsafePathfindData*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<UnsafePathfindData>(), UnsafeUtility.AlignOf<UnsafePathfindData>(), allocator);
		*m_PathfindData = new UnsafePathfindData(allocator);
		m_AllocatorLabel = allocator;
	}

	[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
	private void CheckRead()
	{
	}

	[Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
	private void CheckWrite()
	{
	}

	public unsafe void Dispose()
	{
		m_PathfindData->Dispose();
		UnsafeUtility.Free(m_PathfindData, m_AllocatorLabel);
		m_PathfindData = null;
	}

	public unsafe void Clear()
	{
		m_PathfindData->Clear();
	}

	public unsafe void GetMemoryStats(out uint used, out uint allocated)
	{
		m_PathfindData->GetMemoryStats(out used, out allocated);
	}

	public unsafe EdgeID CreateEdge(PathNode startNode, PathNode middleNode, PathNode endNode, PathSpecification specification, LocationSpecification location)
	{
		return m_PathfindData->CreateEdge(startNode, middleNode, endNode, specification, location);
	}

	public unsafe void UpdateEdge(EdgeID edgeID, PathNode startNode, PathNode middleNode, PathNode endNode, PathSpecification specification, LocationSpecification location)
	{
		m_PathfindData->UpdateEdge(edgeID, startNode, middleNode, endNode, specification, location);
	}

	public unsafe void DestroyEdge(EdgeID edgeID)
	{
		m_PathfindData->DestroyEdge(edgeID);
	}

	public unsafe void AddEdge(Entity owner, EdgeID edgeID)
	{
		m_PathfindData->AddEdge(owner, edgeID);
	}

	public unsafe void AddSecondaryEdge(Entity owner, EdgeID edgeID)
	{
		m_PathfindData->AddSecondaryEdge(owner, edgeID);
	}

	public unsafe bool GetEdge(Entity owner, out EdgeID edgeID)
	{
		return m_PathfindData->GetEdge(owner, out edgeID);
	}

	public unsafe bool GetSecondaryEdge(Entity owner, out EdgeID edgeID)
	{
		return m_PathfindData->GetSecondaryEdge(owner, out edgeID);
	}

	public unsafe bool RemoveEdge(Entity owner, out EdgeID edgeID)
	{
		return m_PathfindData->RemoveEdge(owner, out edgeID);
	}

	public unsafe bool RemoveSecondaryEdge(Entity owner, out EdgeID edgeID)
	{
		return m_PathfindData->RemoveSecondaryEdge(owner, out edgeID);
	}

	public unsafe ref float SetDensity(EdgeID edgeID)
	{
		return ref m_PathfindData->GetEdge(edgeID).m_Specification.m_Density;
	}

	public unsafe ref PathfindCosts SetCosts(EdgeID edgeID)
	{
		return ref m_PathfindData->GetEdge(edgeID).m_Specification.m_Costs;
	}

	public unsafe ref byte SetFlowOffset(EdgeID edgeID)
	{
		return ref m_PathfindData->GetEdge(edgeID).m_Specification.m_FlowOffset;
	}

	public unsafe EdgeFlags GetFlags(EdgeID edgeID)
	{
		return m_PathfindData->GetEdge(edgeID).m_Specification.m_Flags;
	}

	public unsafe void SetEdgeDirections(EdgeID edgeID, PathNode startNode, PathNode endNode, bool enableForward, bool enableBackward)
	{
		m_PathfindData->SetEdgeDirections(edgeID, startNode, endNode, enableForward, enableBackward);
	}

	public unsafe UnsafePathfindData GetReadOnlyData()
	{
		return *m_PathfindData;
	}
}
