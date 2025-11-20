using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Pathfind;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Net;

[CompilerGenerated]
public class EdgeMappingSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateMappingJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Lane> m_LaneType;

		[ReadOnly]
		public ComponentTypeHandle<EdgeLane> m_EdgeLaneType;

		[ReadOnly]
		public ComponentTypeHandle<NodeLane> m_NodeLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		public ComponentTypeHandle<EdgeMapping> m_EdgeMappingType;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<ConnectedNode> m_ConnectedNodes;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Lane> nativeArray = chunk.GetNativeArray(ref m_LaneType);
			NativeArray<Curve> nativeArray2 = chunk.GetNativeArray(ref m_CurveType);
			NativeArray<Owner> nativeArray3 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<EdgeMapping> nativeArray4 = chunk.GetNativeArray(ref m_EdgeMappingType);
			bool flag = chunk.Has(ref m_EdgeLaneType);
			bool flag2 = chunk.Has(ref m_NodeLaneType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Lane lane = nativeArray[i];
				Curve laneCurve = nativeArray2[i];
				EdgeMapping value = default(EdgeMapping);
				if (flag)
				{
					value.m_Parent1 = nativeArray3[i].m_Owner;
					value.m_CurveDelta1 = GetCurveDelta(laneCurve.m_Bezier, value.m_Parent1);
				}
				else if (flag2)
				{
					Owner owner = nativeArray3[i];
					if (m_ConnectedEdges.HasBuffer(owner.m_Owner))
					{
						value = GetNodeEdgeMapping(lane, laneCurve, owner.m_Owner);
					}
					else if (m_ConnectedNodes.HasBuffer(owner.m_Owner))
					{
						value = GetEdgeNodeMapping(lane, owner.m_Owner);
					}
				}
				nativeArray4[i] = value;
			}
		}

		private EdgeMapping GetNodeEdgeMapping(Lane lane, Curve laneCurve, Entity node)
		{
			EdgeMapping result = default(EdgeMapping);
			EdgeIterator edgeIterator = new EdgeIterator(Entity.Null, node, m_ConnectedEdges, m_EdgeData, m_TempData, m_HiddenData);
			bool2 @bool = false;
			bool2 bool2 = false;
			EdgeIteratorValue value;
			while (edgeIterator.GetNext(out value))
			{
				PathNode other = new PathNode(value.m_Edge, 0);
				if (lane.m_StartNode.OwnerEquals(other))
				{
					result.m_Parent1 = value.m_Edge;
					@bool.x = value.m_End;
					bool2.x = false;
				}
				if (lane.m_EndNode.OwnerEquals(other))
				{
					result.m_Parent2 = value.m_Edge;
					@bool.y = value.m_End;
					bool2.y = false;
				}
				DynamicBuffer<ConnectedNode> dynamicBuffer = m_ConnectedNodes[value.m_Edge];
				int num = 0;
				ConnectedNode connectedNode;
				while (num < dynamicBuffer.Length)
				{
					connectedNode = dynamicBuffer[num];
					PathNode other2 = new PathNode(connectedNode.m_Node, 0);
					if (lane.m_StartNode.OwnerEquals(other2))
					{
						goto IL_0107;
					}
					if (!lane.m_EndNode.OwnerEquals(other2))
					{
						EdgeIterator edgeIterator2 = new EdgeIterator(Entity.Null, connectedNode.m_Node, m_ConnectedEdges, m_EdgeData, m_TempData, m_HiddenData);
						EdgeIteratorValue value2;
						while (edgeIterator2.GetNext(out value2))
						{
							if (value2.m_Edge == value.m_Edge)
							{
								continue;
							}
							other = new PathNode(value2.m_Edge, 0);
							if (lane.m_StartNode.OwnerEquals(other))
							{
								goto IL_01f7;
							}
							if (!lane.m_EndNode.OwnerEquals(other))
							{
								continue;
							}
							goto IL_0240;
						}
						num++;
						continue;
					}
					goto IL_0150;
				}
				continue;
				IL_0150:
				result.m_Parent1 = value.m_Edge;
				result.m_Parent2 = connectedNode.m_Node;
				@bool = new bool2(value.m_End, y: true);
				bool2 = new bool2(x: false, y: true);
				break;
				IL_0107:
				result.m_Parent1 = connectedNode.m_Node;
				result.m_Parent2 = value.m_Edge;
				@bool = new bool2(x: false, value.m_End);
				bool2 = new bool2(x: true, y: false);
				break;
				IL_0240:
				result.m_Parent1 = value.m_Edge;
				result.m_Parent2 = connectedNode.m_Node;
				@bool = new bool2(value.m_End, y: true);
				bool2 = new bool2(x: false, y: true);
				break;
				IL_01f7:
				result.m_Parent1 = connectedNode.m_Node;
				result.m_Parent2 = value.m_Edge;
				@bool = new bool2(x: false, value.m_End);
				bool2 = new bool2(x: true, y: false);
				break;
			}
			if (result.m_Parent1 != Entity.Null && result.m_Parent2 != Entity.Null)
			{
				if (bool2.Equals(new bool2(x: false, y: true)))
				{
					CommonUtils.Swap(ref result.m_Parent1, ref result.m_Parent2);
					@bool = @bool.yx;
					bool2 = bool2.yx;
				}
				MathUtils.Divide(laneCurve.m_Bezier, out var output, out var output2, 0.5f);
				if (!bool2.x)
				{
					result.m_CurveDelta1 = GetCurveDelta(output, result.m_Parent1);
				}
				if (!bool2.y)
				{
					result.m_CurveDelta2 = GetCurveDelta(output2, result.m_Parent2);
				}
			}
			else if (result.m_Parent1 != Entity.Null && !bool2.x)
			{
				result.m_CurveDelta1 = GetCurveDelta(laneCurve.m_Bezier, result.m_Parent1);
			}
			else if (result.m_Parent2 != Entity.Null && !bool2.y)
			{
				result.m_CurveDelta2 = GetCurveDelta(laneCurve.m_Bezier, result.m_Parent2);
			}
			if (bool2.x)
			{
				if (@bool.x)
				{
					result.m_CurveDelta1 = new float2(1f, 0f);
				}
				else
				{
					result.m_CurveDelta1 = new float2(0f, 1f);
				}
			}
			else if (@bool.x)
			{
				result.m_CurveDelta1.y = math.cmax(result.m_CurveDelta1);
			}
			else
			{
				result.m_CurveDelta1.y = math.cmin(result.m_CurveDelta1);
			}
			if (bool2.y)
			{
				if (@bool.y)
				{
					result.m_CurveDelta2 = new float2(1f, 0f);
				}
				else
				{
					result.m_CurveDelta2 = new float2(0f, 1f);
				}
			}
			else if (@bool.y)
			{
				result.m_CurveDelta2.x = math.cmax(result.m_CurveDelta2);
			}
			else
			{
				result.m_CurveDelta2.x = math.cmin(result.m_CurveDelta2);
			}
			if (result.m_Parent1 == Entity.Null)
			{
				result.m_Parent1 = result.m_Parent2;
				result.m_CurveDelta1 = result.m_CurveDelta2;
				result.m_Parent2 = Entity.Null;
				result.m_CurveDelta2 = default(float2);
			}
			return result;
		}

		private EdgeMapping GetEdgeNodeMapping(Lane lane, Entity edge)
		{
			EdgeMapping result = default(EdgeMapping);
			DynamicBuffer<ConnectedNode> dynamicBuffer = m_ConnectedNodes[edge];
			float2 @float = default(float2);
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				ConnectedNode connectedNode = dynamicBuffer[i];
				PathNode other = new PathNode(connectedNode.m_Node, 0);
				EdgeIterator edgeIterator = new EdgeIterator(Entity.Null, connectedNode.m_Node, m_ConnectedEdges, m_EdgeData, m_TempData, m_HiddenData);
				if (lane.m_StartNode.OwnerEquals(other))
				{
					result.m_Parent1 = connectedNode.m_Node;
					@float.x = connectedNode.m_CurvePosition;
					break;
				}
				if (lane.m_EndNode.OwnerEquals(other))
				{
					result.m_Parent2 = connectedNode.m_Node;
					@float.y = connectedNode.m_CurvePosition;
					break;
				}
				EdgeIteratorValue value;
				while (edgeIterator.GetNext(out value))
				{
					PathNode other2 = new PathNode(value.m_Edge, 0);
					if (lane.m_StartNode.OwnerEquals(other2))
					{
						goto IL_00f5;
					}
					if (!lane.m_EndNode.OwnerEquals(other2))
					{
						continue;
					}
					goto IL_0123;
				}
				continue;
				IL_0123:
				result.m_Parent2 = connectedNode.m_Node;
				@float.y = connectedNode.m_CurvePosition;
				break;
				IL_00f5:
				result.m_Parent1 = connectedNode.m_Node;
				@float.x = connectedNode.m_CurvePosition;
				break;
			}
			if (result.m_Parent1 != Entity.Null)
			{
				result.m_Parent2 = edge;
				result.m_CurveDelta1 = new float2(0f, 1f);
				result.m_CurveDelta2 = @float.x;
			}
			else if (result.m_Parent2 != Entity.Null)
			{
				result.m_Parent1 = result.m_Parent2;
				result.m_Parent2 = edge;
				result.m_CurveDelta1 = new float2(1f, 0f);
				result.m_CurveDelta2 = @float.y;
			}
			return result;
		}

		private float2 GetCurveDelta(Bezier4x3 laneCurve, Entity edge)
		{
			float2 result = default(float2);
			if (m_CurveData.TryGetComponent(edge, out var componentData))
			{
				MathUtils.Distance(componentData.m_Bezier.xz, laneCurve.a.xz, out result.x);
				MathUtils.Distance(componentData.m_Bezier.xz, laneCurve.d.xz, out result.y);
			}
			return result;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Lane> __Game_Net_Lane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EdgeLane> __Game_Net_EdgeLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<NodeLane> __Game_Net_NodeLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		public ComponentTypeHandle<EdgeMapping> __Game_Net_EdgeMapping_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Hidden> __Game_Tools_Hidden_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedNode> __Game_Net_ConnectedNode_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Net_Lane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Lane>(isReadOnly: true);
			__Game_Net_EdgeLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EdgeLane>(isReadOnly: true);
			__Game_Net_NodeLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<NodeLane>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Net_EdgeMapping_RW_ComponentTypeHandle = state.GetComponentTypeHandle<EdgeMapping>();
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentLookup = state.GetComponentLookup<Hidden>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Net_ConnectedNode_RO_BufferLookup = state.GetBufferLookup<ConnectedNode>(isReadOnly: true);
		}
	}

	private EntityQuery m_UpdatedLanesQuery;

	private EntityQuery m_AllLanesQuery;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UpdatedLanesQuery = GetEntityQuery(ComponentType.ReadOnly<EdgeMapping>(), ComponentType.ReadOnly<Updated>());
		m_AllLanesQuery = GetEntityQuery(ComponentType.ReadOnly<EdgeMapping>());
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_Loaded = true;
	}

	private bool GetLoaded()
	{
		if (m_Loaded)
		{
			m_Loaded = false;
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		EntityQuery query = (GetLoaded() ? m_AllLanesQuery : m_UpdatedLanesQuery);
		if (!query.IsEmptyIgnoreFilter)
		{
			UpdateMappingJob jobData = new UpdateMappingJob
			{
				m_LaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Lane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EdgeLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_NodeLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_NodeLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EdgeMappingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_EdgeMapping_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_ConnectedNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedNode_RO_BufferLookup, ref base.CheckedStateRef)
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, query, base.Dependency);
		}
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
	public EdgeMappingSystem()
	{
	}
}
