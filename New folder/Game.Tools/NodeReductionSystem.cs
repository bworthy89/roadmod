using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class NodeReductionSystem : GameSystemBase
{
	[BurstCompile]
	private struct FindCandidatesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<OwnerDefinition> m_OwnerDefinitionData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Upgraded> m_UpgradedData;

		[ReadOnly]
		public ComponentLookup<Fixed> m_FixedData;

		[ReadOnly]
		public ComponentLookup<Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public bool m_EditorMode;

		public NativeQueue<ReductionData>.ParallelWriter m_ReductionQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity node = nativeArray[i];
				if (CanMove(node, out var data))
				{
					m_ReductionQueue.Enqueue(data);
				}
			}
		}

		private bool CanMove(Entity node, out ReductionData data)
		{
			data = new ReductionData
			{
				m_Node = node
			};
			Temp temp = m_TempData[node];
			if (temp.m_Original == Entity.Null)
			{
				return false;
			}
			if ((temp.m_Flags & (TempFlags.Delete | TempFlags.Replace | TempFlags.Upgrade)) != 0)
			{
				return false;
			}
			Entity entity = Entity.Null;
			Entity entity2 = Entity.Null;
			EdgeIterator edgeIterator = new EdgeIterator(Entity.Null, node, m_ConnectedEdges, m_EdgeData, m_TempData, m_HiddenData, includeMiddleConnections: true);
			EdgeIteratorValue value;
			while (edgeIterator.GetNext(out value))
			{
				if (value.m_Middle)
				{
					return false;
				}
				if (!m_TempData.HasComponent(value.m_Edge))
				{
					return false;
				}
				if (m_FixedData.HasComponent(value.m_Edge))
				{
					return false;
				}
				if (entity == Entity.Null)
				{
					entity = value.m_Edge;
					continue;
				}
				if (entity2 == Entity.Null)
				{
					entity2 = value.m_Edge;
					continue;
				}
				return false;
			}
			if (entity2 == Entity.Null)
			{
				return false;
			}
			Edge edge = m_EdgeData[entity];
			Edge edge2 = m_EdgeData[entity2];
			bool2 @bool = new bool2(edge.m_Start == node, edge2.m_Start == node);
			bool2 bool2 = new bool2(edge.m_End == node, edge2.m_End == node);
			if (math.any(@bool == bool2))
			{
				return false;
			}
			PrefabRef prefabRef = m_PrefabRefData[entity];
			PrefabRef prefabRef2 = m_PrefabRefData[entity2];
			if (prefabRef.m_Prefab != prefabRef2.m_Prefab)
			{
				return false;
			}
			NetGeometryData prefabGeometryData = default(NetGeometryData);
			if (m_PrefabGeometryData.HasComponent(prefabRef.m_Prefab))
			{
				prefabGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
				if (@bool.x == @bool.y && (prefabGeometryData.m_Flags & GeometryFlags.Asymmetric) != 0)
				{
					return false;
				}
				data.m_EdgeLengthRange = prefabGeometryData.m_EdgeLengthRange;
				data.m_SnapCellSize = (prefabGeometryData.m_Flags & GeometryFlags.SnapCellSize) != 0;
				data.m_ForbidMove = (prefabGeometryData.m_Flags & GeometryFlags.StraightEdges) != 0;
				data.m_NoEdgeConnection = (prefabGeometryData.m_Flags & GeometryFlags.NoEdgeConnection) != 0;
			}
			CompositionFlags compositionFlags = GetElevationFlags(entity, edge.m_Start, edge.m_End, prefabGeometryData);
			CompositionFlags compositionFlags2 = GetElevationFlags(entity2, edge2.m_Start, edge2.m_End, prefabGeometryData);
			if (@bool.x)
			{
				compositionFlags = NetCompositionHelpers.InvertCompositionFlags(compositionFlags);
			}
			if (bool2.y)
			{
				compositionFlags2 = NetCompositionHelpers.InvertCompositionFlags(compositionFlags2);
			}
			CompositionFlags compositionFlags3 = new CompositionFlags(CompositionFlags.General.Elevated | CompositionFlags.General.Tunnel, CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered, CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered);
			if (((compositionFlags ^ compositionFlags2) & compositionFlags3) != default(CompositionFlags))
			{
				return false;
			}
			data.m_EdgeLengthRange.max = math.select(data.m_EdgeLengthRange.max, prefabGeometryData.m_ElevatedLength, (compositionFlags.m_General & CompositionFlags.General.Elevated) != 0);
			data.m_ForbidMove |= (compositionFlags.m_General & CompositionFlags.General.Elevated) != 0;
			data.m_CheckHeight |= (compositionFlags.m_General & compositionFlags3.m_General) != 0;
			data.m_CheckHeight |= (compositionFlags.m_Left & compositionFlags3.m_Left) != 0 && (compositionFlags.m_Right & compositionFlags3.m_Right) != 0;
			Upgraded upgraded = default(Upgraded);
			Upgraded upgraded2 = default(Upgraded);
			if (m_UpgradedData.HasComponent(entity))
			{
				upgraded = m_UpgradedData[entity];
				if (@bool.x)
				{
					upgraded.m_Flags = NetCompositionHelpers.InvertCompositionFlags(upgraded.m_Flags);
				}
			}
			if (m_UpgradedData.HasComponent(entity2))
			{
				upgraded2 = m_UpgradedData[entity2];
				if (bool2.y)
				{
					upgraded2.m_Flags = NetCompositionHelpers.InvertCompositionFlags(upgraded2.m_Flags);
				}
			}
			if (upgraded.m_Flags != upgraded2.m_Flags)
			{
				return false;
			}
			Owner owner = default(Owner);
			Owner owner2 = default(Owner);
			Owner owner3 = default(Owner);
			if (m_OwnerData.HasComponent(node))
			{
				owner = m_OwnerData[node];
			}
			if (m_OwnerData.HasComponent(entity))
			{
				owner2 = m_OwnerData[entity];
			}
			if (m_OwnerData.HasComponent(entity2))
			{
				owner3 = m_OwnerData[entity2];
			}
			if (m_EditorMode)
			{
				if (owner2.m_Owner != owner.m_Owner || owner3.m_Owner != owner.m_Owner)
				{
					return false;
				}
			}
			else if (owner.m_Owner != Entity.Null || owner2.m_Owner != Entity.Null || owner3.m_Owner != Entity.Null)
			{
				return false;
			}
			OwnerDefinition other = default(OwnerDefinition);
			OwnerDefinition ownerDefinition = default(OwnerDefinition);
			OwnerDefinition ownerDefinition2 = default(OwnerDefinition);
			if (m_OwnerDefinitionData.HasComponent(node))
			{
				other = m_OwnerDefinitionData[node];
			}
			if (m_OwnerDefinitionData.HasComponent(entity))
			{
				ownerDefinition = m_OwnerDefinitionData[entity];
			}
			if (m_OwnerDefinitionData.HasComponent(entity2))
			{
				ownerDefinition2 = m_OwnerDefinitionData[entity2];
			}
			if (m_EditorMode)
			{
				if (!ownerDefinition.Equals(other) || !ownerDefinition2.Equals(other))
				{
					return false;
				}
			}
			else if (other.m_Prefab != Entity.Null || ownerDefinition.m_Prefab != Entity.Null || ownerDefinition2.m_Prefab != Entity.Null)
			{
				return false;
			}
			Curve curve = m_CurveData[entity];
			Curve curve2 = m_CurveData[entity2];
			if (@bool.x)
			{
				curve.m_Bezier = MathUtils.Invert(curve.m_Bezier);
			}
			if (bool2.y)
			{
				curve2.m_Bezier = MathUtils.Invert(curve2.m_Bezier);
			}
			if (data.m_CheckHeight)
			{
				float3 x = math.normalizesafe(MathUtils.EndTangent(curve.m_Bezier));
				float3 y = math.normalizesafe(MathUtils.StartTangent(curve2.m_Bezier));
				return math.dot(x, y) >= 0.9995f;
			}
			float2 x2 = math.normalizesafe(MathUtils.EndTangent(curve.m_Bezier).xz);
			float2 y2 = math.normalizesafe(MathUtils.StartTangent(curve2.m_Bezier).xz);
			return math.dot(x2, y2) >= 0.9995f;
		}

		private CompositionFlags GetElevationFlags(Entity edge, Entity startNode, Entity endNode, NetGeometryData prefabGeometryData)
		{
			Elevation startElevation = default(Elevation);
			Elevation middleElevation = default(Elevation);
			Elevation endElevation = default(Elevation);
			if (m_ElevationData.HasComponent(startNode))
			{
				startElevation = m_ElevationData[startNode];
			}
			if (m_ElevationData.HasComponent(edge))
			{
				middleElevation = m_ElevationData[edge];
			}
			if (m_ElevationData.HasComponent(endNode))
			{
				endElevation = m_ElevationData[endNode];
			}
			return NetCompositionHelpers.GetElevationFlags(startElevation, middleElevation, endElevation, prefabGeometryData);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct ReductionData
	{
		public Entity m_Node;

		public Bounds1 m_EdgeLengthRange;

		public bool m_SnapCellSize;

		public bool m_ForbidMove;

		public bool m_CheckHeight;

		public bool m_NoEdgeConnection;
	}

	[BurstCompile]
	private struct NodeReductionJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public ComponentLookup<LocalConnectData> m_LocalConnectData;

		public ComponentLookup<Node> m_NodeData;

		public ComponentLookup<Edge> m_EdgeData;

		public ComponentLookup<Curve> m_CurveData;

		public ComponentLookup<Temp> m_TempData;

		public ComponentLookup<BuildOrder> m_BuildOrderData;

		public ComponentLookup<Road> m_RoadData;

		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		public BufferLookup<ConnectedNode> m_ConnectedNodes;

		public NativeQueue<ReductionData> m_ReductionQueue;

		public void Execute()
		{
			int count = m_ReductionQueue.Count;
			if (count != 0)
			{
				for (int i = 0; i < count; i++)
				{
					ReductionData data = m_ReductionQueue.Dequeue();
					TryReduceOrMove(data);
				}
			}
		}

		private void TryReduceOrMove(ReductionData data)
		{
			Entity entity = Entity.Null;
			Entity entity2 = Entity.Null;
			DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[data.m_Node];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity edge = dynamicBuffer[i].m_Edge;
				if ((m_TempData[edge].m_Flags & TempFlags.Delete) != 0)
				{
					continue;
				}
				if (entity == Entity.Null)
				{
					entity = edge;
					continue;
				}
				if (entity2 == Entity.Null)
				{
					entity2 = edge;
					continue;
				}
				return;
			}
			if (entity2 == Entity.Null)
			{
				return;
			}
			Temp value = m_TempData[data.m_Node];
			Temp value2 = m_TempData[entity];
			Temp value3 = m_TempData[entity2];
			Edge value4 = m_EdgeData[entity];
			Edge value5 = m_EdgeData[entity2];
			bool2 @bool = new bool2(value4.m_Start == data.m_Node, value5.m_Start == data.m_Node);
			bool2 bool2 = new bool2(value4.m_End == data.m_Node, value5.m_End == data.m_Node);
			Curve curve = m_CurveData[entity];
			Curve curve2 = m_CurveData[entity2];
			if (@bool.x)
			{
				curve.m_Bezier = MathUtils.Invert(curve.m_Bezier);
			}
			if (bool2.y)
			{
				curve2.m_Bezier = MathUtils.Invert(curve2.m_Bezier);
			}
			float num = MathUtils.Length(curve.m_Bezier.xz);
			float num2 = MathUtils.Length(curve2.m_Bezier.xz);
			if (data.m_EdgeLengthRange.max == 0f || num + num2 <= data.m_EdgeLengthRange.max)
			{
				bool flag = ((value2.m_Original != Entity.Null == (value3.m_Original != Entity.Null)) ? (num < num2) : (value2.m_Original != Entity.Null));
				if (!TryJoinCurve(curve.m_Bezier, curve2.m_Bezier, data.m_CheckHeight, out var curve3))
				{
					return;
				}
				if (value2.m_Original != Entity.Null)
				{
					FixStartSlope(curve.m_Bezier, ref curve3);
				}
				if (value3.m_Original != Entity.Null)
				{
					FixEndSlope(curve2.m_Bezier, ref curve3);
				}
				value.m_Flags = TempFlags.Delete | TempFlags.Hidden;
				m_TempData[data.m_Node] = value;
				if (flag)
				{
					if ((value2.m_Flags & (TempFlags.Create | TempFlags.Modify | TempFlags.Replace)) != 0)
					{
						value3.m_Flags |= TempFlags.Modify;
					}
					value3.m_Flags |= value2.m_Flags & (TempFlags.Essential | TempFlags.Upgrade | TempFlags.Parent);
					if (value3.m_Original != Entity.Null)
					{
						value3.m_Flags |= TempFlags.Combine;
					}
					value2.m_Flags = (value3.m_Flags & TempFlags.Essential) | (TempFlags.Delete | TempFlags.Hidden);
					if ((value3.m_Flags & TempFlags.Essential) != 0 && (value3.m_Flags & (TempFlags.Create | TempFlags.Modify | TempFlags.Replace | TempFlags.Upgrade)) != 0)
					{
						value2.m_Flags |= TempFlags.RemoveCost;
					}
					curve2.m_Bezier = curve3;
					curve2.m_Length = MathUtils.Length(curve3);
					if (bool2.y)
					{
						curve2.m_Bezier = MathUtils.Invert(curve2.m_Bezier);
						ReplaceEdgeConnection(ref value5.m_End, entity2, @bool.x ? value4.m_End : value4.m_Start);
					}
					else
					{
						ReplaceEdgeConnection(ref value5.m_Start, entity2, @bool.x ? value4.m_End : value4.m_Start);
					}
					ReplaceEdgeData(entity, entity2, bool2.x, @bool.y);
					MoveConnectedNodes(entity, entity2, curve2, data.m_NoEdgeConnection);
					m_CurveData[entity2] = curve2;
					m_EdgeData[entity2] = value5;
				}
				else
				{
					if ((value3.m_Flags & (TempFlags.Create | TempFlags.Modify | TempFlags.Replace)) != 0)
					{
						value2.m_Flags |= TempFlags.Modify;
					}
					value2.m_Flags |= value3.m_Flags & (TempFlags.Essential | TempFlags.Upgrade | TempFlags.Parent);
					if (value2.m_Original != Entity.Null)
					{
						value2.m_Flags |= TempFlags.Combine;
					}
					value3.m_Flags = (value2.m_Flags & TempFlags.Essential) | (TempFlags.Delete | TempFlags.Hidden);
					if ((value2.m_Flags & TempFlags.Essential) != 0 && (value2.m_Flags & (TempFlags.Create | TempFlags.Modify | TempFlags.Replace | TempFlags.Upgrade)) != 0)
					{
						value3.m_Flags |= TempFlags.RemoveCost;
					}
					curve.m_Bezier = curve3;
					curve.m_Length = MathUtils.Length(curve3);
					if (@bool.x)
					{
						curve.m_Bezier = MathUtils.Invert(curve.m_Bezier);
						ReplaceEdgeConnection(ref value4.m_Start, entity, bool2.y ? value5.m_Start : value5.m_End);
					}
					else
					{
						ReplaceEdgeConnection(ref value4.m_End, entity, bool2.y ? value5.m_Start : value5.m_End);
					}
					ReplaceEdgeData(entity2, entity, bool2.y, @bool.x);
					MoveConnectedNodes(entity2, entity, curve, data.m_NoEdgeConnection);
					m_CurveData[entity] = curve;
					m_EdgeData[entity] = value4;
				}
				m_TempData[entity] = value2;
				m_TempData[entity2] = value3;
			}
			else
			{
				if (data.m_ForbidMove || (!(num < data.m_EdgeLengthRange.max * 0.5f) && !(num2 < data.m_EdgeLengthRange.max * 0.5f)))
				{
					return;
				}
				float num3 = math.abs(num - num2) * 0.5f;
				if (data.m_SnapCellSize)
				{
					num3 = MathUtils.Snap(num3 - 1f, 8f);
				}
				if (num3 < 1f)
				{
					return;
				}
				if (num >= num2)
				{
					Bounds1 t = new Bounds1(0f, 1f);
					MathUtils.ClampLengthInverse(curve.m_Bezier.xz, ref t, num3);
					MathUtils.Divide(curve.m_Bezier, out var output, out var output2, t.min);
					if (!TryJoinCurve(output2, curve2.m_Bezier, data.m_CheckHeight, out var curve4))
					{
						return;
					}
					if (value3.m_Original != Entity.Null)
					{
						FixEndSlope(curve2.m_Bezier, ref curve4);
					}
					curve.m_Bezier = output;
					curve2.m_Bezier = curve4;
					if ((value3.m_Flags & (TempFlags.Create | TempFlags.Modify | TempFlags.Replace | TempFlags.Parent)) == 0 && (value2.m_Flags & (TempFlags.Create | TempFlags.Modify | TempFlags.Replace | TempFlags.Parent)) != 0)
					{
						value3.m_Flags |= (TempFlags)(((value2.m_Flags & TempFlags.Parent) != 0) ? 2048 : 64);
					}
				}
				else
				{
					Bounds1 t2 = new Bounds1(0f, 1f);
					MathUtils.ClampLength(curve2.m_Bezier.xz, ref t2, num3);
					MathUtils.Divide(curve2.m_Bezier, out var output3, out var output4, t2.max);
					if (!TryJoinCurve(curve.m_Bezier, output3, data.m_CheckHeight, out var curve5))
					{
						return;
					}
					if (value2.m_Original != Entity.Null)
					{
						FixStartSlope(curve.m_Bezier, ref curve5);
					}
					curve2.m_Bezier = output4;
					curve.m_Bezier = curve5;
					if ((value2.m_Flags & (TempFlags.Create | TempFlags.Modify | TempFlags.Replace | TempFlags.Parent)) == 0 && (value3.m_Flags & (TempFlags.Create | TempFlags.Modify | TempFlags.Replace | TempFlags.Parent)) != 0)
					{
						value2.m_Flags |= (TempFlags)(((value3.m_Flags & TempFlags.Parent) != 0) ? 2048 : 64);
					}
				}
				bool flag2 = (value2.m_Flags & (TempFlags.Create | TempFlags.Modify | TempFlags.Replace | TempFlags.Upgrade)) != 0;
				bool flag3 = (value3.m_Flags & (TempFlags.Create | TempFlags.Modify | TempFlags.Replace | TempFlags.Upgrade)) != 0;
				if ((value2.m_Flags & TempFlags.Essential) != 0 && flag2 && !flag3)
				{
					value3.m_Flags |= TempFlags.RemoveCost;
				}
				if ((value3.m_Flags & TempFlags.Essential) != 0 && flag3 && !flag2)
				{
					value2.m_Flags |= TempFlags.RemoveCost;
				}
				if (@bool.x)
				{
					curve.m_Bezier = MathUtils.Invert(curve.m_Bezier);
				}
				if (bool2.y)
				{
					curve2.m_Bezier = MathUtils.Invert(curve2.m_Bezier);
				}
				curve.m_Length = MathUtils.Length(curve.m_Bezier);
				curve2.m_Length = MathUtils.Length(curve2.m_Bezier);
				MoveConnectedNodes(entity, entity2, curve, curve2, data.m_NoEdgeConnection);
				m_CurveData[entity] = curve;
				m_CurveData[entity2] = curve2;
				value2.m_Flags |= value3.m_Flags & TempFlags.Essential;
				value3.m_Flags |= value2.m_Flags & TempFlags.Essential;
				m_TempData[entity] = value2;
				m_TempData[entity2] = value3;
			}
		}

		private bool TryJoinCurve(Bezier4x3 curve1, Bezier4x3 curve2, bool checkHeight, out Bezier4x3 curve)
		{
			curve = MathUtils.Join(curve1, curve2);
			float4 @float = default(float4);
			float t;
			if (checkHeight)
			{
				@float.x = MathUtils.Distance(curve, MathUtils.Position(curve1, 0.5f), out t);
				@float.y = MathUtils.Distance(curve, curve1.d, out t);
				@float.z = MathUtils.Distance(curve, curve2.a, out t);
				@float.w = MathUtils.Distance(curve, MathUtils.Position(curve2, 0.5f), out t);
			}
			else
			{
				@float.x = MathUtils.Distance(curve.xz, MathUtils.Position(curve1, 0.5f).xz, out t);
				@float.y = MathUtils.Distance(curve.xz, curve1.d.xz, out t);
				@float.z = MathUtils.Distance(curve.xz, curve2.a.xz, out t);
				@float.w = MathUtils.Distance(curve.xz, MathUtils.Position(curve2, 0.5f).xz, out t);
				float num = FindHeightOffset(curve1, curve2, MathUtils.Position(curve, 1f / 3f));
				float num2 = FindHeightOffset(curve1, curve2, MathUtils.Position(curve, 2f / 3f));
				curve.b.y += num * 3f - num2 * 1.5f;
				curve.c.y += num2 * 3f - num * 1.5f;
			}
			return math.all(@float < 0.1f);
		}

		private void FixStartSlope(Bezier4x3 originalCurve, ref Bezier4x3 newCurve)
		{
			newCurve.b.y = originalCurve.b.y;
		}

		private void FixEndSlope(Bezier4x3 originalCurve, ref Bezier4x3 newCurve)
		{
			newCurve.c.y = originalCurve.c.y;
		}

		private float FindHeightOffset(Bezier4x3 curve1, Bezier4x3 curve2, float3 position)
		{
			float t;
			float num = MathUtils.Distance(curve1.xz, position.xz, out t);
			float t2;
			float num2 = MathUtils.Distance(curve2.xz, position.xz, out t2);
			if (num < num2)
			{
				return MathUtils.Position(curve1, t).y - position.y;
			}
			return MathUtils.Position(curve2, t2).y - position.y;
		}

		private void ReplaceEdgeConnection(ref Entity node, Entity edge, Entity newNode)
		{
			if (m_TempData.HasComponent(node))
			{
				CollectionUtils.RemoveValue(m_ConnectedEdges[node], new ConnectedEdge(edge));
			}
			node = newNode;
			if (m_TempData.HasComponent(node))
			{
				CollectionUtils.TryAddUniqueValue(m_ConnectedEdges[node], new ConnectedEdge(edge));
			}
		}

		private void ReplaceEdgeData(Entity source, Entity target, bool sourceStart, bool targetStart)
		{
			if (m_RoadData.HasComponent(source) && m_RoadData.HasComponent(target))
			{
				Road road = m_RoadData[source];
				Road value = m_RoadData[target];
				if (((uint)road.m_Flags & (uint)(sourceStart ? 1 : 2)) != 0)
				{
					value.m_Flags |= (Game.Net.RoadFlags)(targetStart ? 1 : 2);
				}
				else
				{
					value.m_Flags &= (Game.Net.RoadFlags)(byte)(~(targetStart ? 1 : 2));
				}
				m_RoadData[target] = value;
			}
			if (m_BuildOrderData.HasComponent(source) && m_BuildOrderData.HasComponent(target))
			{
				BuildOrder buildOrder = m_BuildOrderData[source];
				BuildOrder value2 = m_BuildOrderData[target];
				if (targetStart)
				{
					value2.m_Start = (sourceStart ? buildOrder.m_Start : buildOrder.m_End);
				}
				else
				{
					value2.m_End = (sourceStart ? buildOrder.m_Start : buildOrder.m_End);
				}
				m_BuildOrderData[target] = value2;
			}
		}

		private void MoveConnectedNodes(Entity source, Entity target, Curve curve, bool noEdgeConnection)
		{
			DynamicBuffer<ConnectedNode> dynamicBuffer = m_ConnectedNodes[source];
			DynamicBuffer<ConnectedNode> buffer = m_ConnectedNodes[target];
			for (int i = 0; i < buffer.Length; i++)
			{
				ConnectedNode value = buffer[i];
				Node node = m_NodeData[value.m_Node];
				GetDistance(curve, node, noEdgeConnection, out value.m_CurvePosition);
				buffer[i] = value;
			}
			for (int j = 0; j < dynamicBuffer.Length; j++)
			{
				ConnectedNode connectedNode = dynamicBuffer[j];
				if (CollectionUtils.ContainsValue(buffer, connectedNode))
				{
					RemoveConnectedEdge(connectedNode.m_Node, source);
					continue;
				}
				Node node2 = m_NodeData[connectedNode.m_Node];
				GetDistance(curve, node2, noEdgeConnection, out connectedNode.m_CurvePosition);
				buffer.Add(connectedNode);
				SwitchConnectedEdge(connectedNode.m_Node, source, target);
			}
			dynamicBuffer.Clear();
		}

		private void MoveConnectedNodes(Entity edge1, Entity edge2, Curve curve1, Curve curve2, bool noEdgeConnection)
		{
			DynamicBuffer<ConnectedNode> buffer = m_ConnectedNodes[edge1];
			DynamicBuffer<ConnectedNode> buffer2 = m_ConnectedNodes[edge2];
			int num = buffer.Length;
			int num2 = buffer2.Length;
			for (int i = 0; i < num; i++)
			{
				ConnectedNode connectedNode = buffer[i];
				Node node = m_NodeData[connectedNode.m_Node];
				PrefabRef prefabRef = m_PrefabRefData[connectedNode.m_Node];
				if (m_LocalConnectData.TryGetComponent(prefabRef.m_Prefab, out var componentData) && (componentData.m_Flags & LocalConnectFlags.ChooseSides) != 0)
				{
					PrefabRef prefabRef2 = m_PrefabRefData[edge1];
					float num3 = m_PrefabGeometryData[prefabRef.m_Prefab].m_DefaultWidth * 0.5f + 0.1f;
					float num4 = m_PrefabGeometryData[prefabRef2.m_Prefab].m_DefaultWidth * 0.5f;
					float clampedDistance = GetClampedDistance(curve1, node, out var curvePosition);
					float clampedDistance2 = GetClampedDistance(curve2, node, out var curvePosition2);
					if (clampedDistance <= clampedDistance2)
					{
						connectedNode.m_CurvePosition = curvePosition;
						buffer[i] = connectedNode;
						clampedDistance2 -= math.sqrt(num4 * num4 + num3 * num3) - num4;
						if (clampedDistance2 <= clampedDistance && !CollectionUtils.ContainsValue(buffer2, connectedNode))
						{
							connectedNode.m_CurvePosition = curvePosition2;
							buffer2.Add(connectedNode);
							AddConnectedEdge(connectedNode.m_Node, edge2);
						}
						continue;
					}
					clampedDistance -= math.sqrt(num4 * num4 + num3 * num3) - num4;
					if (clampedDistance <= clampedDistance2)
					{
						connectedNode.m_CurvePosition = curvePosition;
						buffer[i] = connectedNode;
						continue;
					}
					buffer.RemoveAt(i--);
					num--;
					if (CollectionUtils.ContainsValue(buffer2, connectedNode))
					{
						RemoveConnectedEdge(connectedNode.m_Node, edge1);
						continue;
					}
					connectedNode.m_CurvePosition = curvePosition2;
					buffer2.Add(connectedNode);
					SwitchConnectedEdge(connectedNode.m_Node, edge1, edge2);
				}
				else
				{
					float curvePosition3;
					float distance = GetDistance(curve1, node, noEdgeConnection, out curvePosition3);
					float curvePosition4;
					float distance2 = GetDistance(curve2, node, noEdgeConnection, out curvePosition4);
					if (distance <= distance2 || CollectionUtils.ContainsValue(buffer2, connectedNode))
					{
						connectedNode.m_CurvePosition = curvePosition3;
						buffer[i] = connectedNode;
						continue;
					}
					connectedNode.m_CurvePosition = curvePosition4;
					buffer2.Add(connectedNode);
					buffer.RemoveAt(i--);
					SwitchConnectedEdge(connectedNode.m_Node, edge1, edge2);
					num--;
				}
			}
			for (int j = 0; j < num2; j++)
			{
				ConnectedNode connectedNode2 = buffer2[j];
				Node node2 = m_NodeData[connectedNode2.m_Node];
				PrefabRef prefabRef3 = m_PrefabRefData[connectedNode2.m_Node];
				if (m_LocalConnectData.TryGetComponent(prefabRef3.m_Prefab, out var componentData2) && (componentData2.m_Flags & LocalConnectFlags.ChooseSides) != 0)
				{
					PrefabRef prefabRef4 = m_PrefabRefData[edge1];
					float num5 = m_PrefabGeometryData[prefabRef3.m_Prefab].m_DefaultWidth * 0.5f + 0.1f;
					float num6 = m_PrefabGeometryData[prefabRef4.m_Prefab].m_DefaultWidth * 0.5f;
					float clampedDistance3 = GetClampedDistance(curve1, node2, out var curvePosition5);
					float clampedDistance4 = GetClampedDistance(curve2, node2, out var curvePosition6);
					if (clampedDistance4 <= clampedDistance3)
					{
						connectedNode2.m_CurvePosition = curvePosition6;
						buffer2[j] = connectedNode2;
						clampedDistance3 -= math.sqrt(num6 * num6 + num5 * num5) - num6;
						if (clampedDistance3 <= clampedDistance4 && !CollectionUtils.ContainsValue(buffer, connectedNode2))
						{
							connectedNode2.m_CurvePosition = curvePosition5;
							buffer.Add(connectedNode2);
							AddConnectedEdge(connectedNode2.m_Node, edge1);
						}
						continue;
					}
					clampedDistance4 -= math.sqrt(num6 * num6 + num5 * num5) - num6;
					if (clampedDistance4 <= clampedDistance3)
					{
						connectedNode2.m_CurvePosition = curvePosition6;
						buffer2[j] = connectedNode2;
						continue;
					}
					buffer2.RemoveAt(j--);
					num2--;
					if (CollectionUtils.ContainsValue(buffer, connectedNode2))
					{
						RemoveConnectedEdge(connectedNode2.m_Node, edge2);
						continue;
					}
					connectedNode2.m_CurvePosition = curvePosition5;
					buffer.Add(connectedNode2);
					SwitchConnectedEdge(connectedNode2.m_Node, edge2, edge1);
				}
				else
				{
					float curvePosition7;
					float distance3 = GetDistance(curve1, node2, noEdgeConnection, out curvePosition7);
					if (GetDistance(curve2, node2, noEdgeConnection, out var curvePosition8) <= distance3 || CollectionUtils.ContainsValue(buffer, connectedNode2))
					{
						connectedNode2.m_CurvePosition = curvePosition8;
						buffer2[j] = connectedNode2;
						continue;
					}
					connectedNode2.m_CurvePosition = curvePosition7;
					buffer.Add(connectedNode2);
					buffer2.RemoveAt(j--);
					SwitchConnectedEdge(connectedNode2.m_Node, edge2, edge1);
					num2--;
				}
			}
		}

		private void RemoveConnectedEdge(Entity node, Entity source)
		{
			if (!m_TempData.HasComponent(node))
			{
				return;
			}
			DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[node];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				if (dynamicBuffer[i].m_Edge == source)
				{
					dynamicBuffer.RemoveAt(i);
					break;
				}
			}
		}

		private void AddConnectedEdge(Entity node, Entity target)
		{
			if (m_TempData.HasComponent(node))
			{
				m_ConnectedEdges[node].Add(new ConnectedEdge(target));
			}
		}

		private void SwitchConnectedEdge(Entity node, Entity source, Entity target)
		{
			if (!m_TempData.HasComponent(node))
			{
				return;
			}
			DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[node];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				ConnectedEdge value = dynamicBuffer[i];
				if (value.m_Edge == source)
				{
					value.m_Edge = target;
					dynamicBuffer[i] = value;
				}
			}
		}

		private float GetClampedDistance(Curve curve, Node node, out float curvePosition)
		{
			MathUtils.Distance(curve.m_Bezier.xz, node.m_Position.xz, out curvePosition);
			float t;
			if (curve.m_Length >= 0.2f)
			{
				float num = 0.1f / curve.m_Length;
				t = math.clamp(curvePosition, num, 1f - num);
			}
			else
			{
				t = 0.5f;
			}
			return math.distance(MathUtils.Position(curve.m_Bezier, t), node.m_Position);
		}

		private float GetDistance(Curve curve, Node node, bool noEdgeConnection, out float curvePosition)
		{
			if (noEdgeConnection)
			{
				float num = math.distance(curve.m_Bezier.a.xz, node.m_Position.xz);
				float num2 = math.distance(curve.m_Bezier.d.xz, node.m_Position.xz);
				curvePosition = math.select(0f, 1f, num2 < num);
				return math.select(num, num2, num2 < num);
			}
			return MathUtils.Distance(curve.m_Bezier.xz, node.m_Position.xz, out curvePosition);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Hidden> __Game_Tools_Hidden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OwnerDefinition> __Game_Tools_OwnerDefinition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Upgraded> __Game_Net_Upgraded_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Fixed> __Game_Net_Fixed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Elevation> __Game_Net_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<LocalConnectData> __Game_Prefabs_LocalConnectData_RO_ComponentLookup;

		public ComponentLookup<Node> __Game_Net_Node_RW_ComponentLookup;

		public ComponentLookup<Edge> __Game_Net_Edge_RW_ComponentLookup;

		public ComponentLookup<Curve> __Game_Net_Curve_RW_ComponentLookup;

		public ComponentLookup<Temp> __Game_Tools_Temp_RW_ComponentLookup;

		public ComponentLookup<BuildOrder> __Game_Net_BuildOrder_RW_ComponentLookup;

		public ComponentLookup<Road> __Game_Net_Road_RW_ComponentLookup;

		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RW_BufferLookup;

		public BufferLookup<ConnectedNode> __Game_Net_ConnectedNode_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentLookup = state.GetComponentLookup<Hidden>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Tools_OwnerDefinition_RO_ComponentLookup = state.GetComponentLookup<OwnerDefinition>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Upgraded_RO_ComponentLookup = state.GetComponentLookup<Upgraded>(isReadOnly: true);
			__Game_Net_Fixed_RO_ComponentLookup = state.GetComponentLookup<Fixed>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentLookup = state.GetComponentLookup<Elevation>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Prefabs_LocalConnectData_RO_ComponentLookup = state.GetComponentLookup<LocalConnectData>(isReadOnly: true);
			__Game_Net_Node_RW_ComponentLookup = state.GetComponentLookup<Node>();
			__Game_Net_Edge_RW_ComponentLookup = state.GetComponentLookup<Edge>();
			__Game_Net_Curve_RW_ComponentLookup = state.GetComponentLookup<Curve>();
			__Game_Tools_Temp_RW_ComponentLookup = state.GetComponentLookup<Temp>();
			__Game_Net_BuildOrder_RW_ComponentLookup = state.GetComponentLookup<BuildOrder>();
			__Game_Net_Road_RW_ComponentLookup = state.GetComponentLookup<Road>();
			__Game_Net_ConnectedEdge_RW_BufferLookup = state.GetBufferLookup<ConnectedEdge>();
			__Game_Net_ConnectedNode_RW_BufferLookup = state.GetBufferLookup<ConnectedNode>();
		}
	}

	private ToolSystem m_ToolSystem;

	private EntityQuery m_TempNodeQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_TempNodeQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Node>(), ComponentType.ReadOnly<Updated>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Fixed>());
		RequireForUpdate(m_TempNodeQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeQueue<ReductionData> reductionQueue = new NativeQueue<ReductionData>(Allocator.TempJob);
		FindCandidatesJob jobData = new FindCandidatesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerDefinitionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_OwnerDefinition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UpgradedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Upgraded_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FixedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Fixed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
			m_ReductionQueue = reductionQueue.AsParallelWriter()
		};
		NodeReductionJob jobData2 = new NodeReductionJob
		{
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LocalConnectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LocalConnectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RW_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RW_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RW_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RW_ComponentLookup, ref base.CheckedStateRef),
			m_BuildOrderData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_BuildOrder_RW_ComponentLookup, ref base.CheckedStateRef),
			m_RoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Road_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RW_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedNode_RW_BufferLookup, ref base.CheckedStateRef),
			m_ReductionQueue = reductionQueue
		};
		JobHandle dependsOn = JobChunkExtensions.ScheduleParallel(jobData, m_TempNodeQuery, base.Dependency);
		JobHandle jobHandle = IJobExtensions.Schedule(jobData2, dependsOn);
		reductionQueue.Dispose(jobHandle);
		base.Dependency = jobHandle;
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
	public NodeReductionSystem()
	{
	}
}
