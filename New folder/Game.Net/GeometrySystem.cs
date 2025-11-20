using System;
using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Net;

[CompilerGenerated]
public class GeometrySystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeNodeGeometryJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Node> m_NodeType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<NodeGeometry> m_NodeGeometryType;

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefDataFromEntity;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_Edges;

		[ReadOnly]
		public bool m_Loaded;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Node> nativeArray2 = chunk.GetNativeArray(ref m_NodeType);
			NativeArray<NodeGeometry> nativeArray3 = chunk.GetNativeArray(ref m_NodeGeometryType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			bool flag = chunk.Has(ref m_TempType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity node = nativeArray[i];
				Node node2 = nativeArray2[i];
				NodeGeometry value = nativeArray3[i];
				PrefabRef prefabRef = nativeArray4[i];
				NetGeometryData netGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
				float2 @float = 0f;
				int num = 0;
				float2 float2 = 0f;
				int num2 = 0;
				bool flag2 = false;
				bool flag3 = false;
				bool flag4 = false;
				bool flag5 = false;
				EdgeIterator edgeIterator = new EdgeIterator(Entity.Null, node, m_Edges, m_EdgeDataFromEntity, m_TempData, m_HiddenData);
				EdgeIteratorValue value2;
				while (edgeIterator.GetNext(out value2))
				{
					PrefabRef prefabRef2 = m_PrefabRefDataFromEntity[value2.m_Edge];
					NetGeometryData netGeometryData2 = m_PrefabGeometryData[prefabRef2.m_Prefab];
					if (!m_Loaded && (!flag || m_TempData.HasComponent(value2.m_Edge)))
					{
						flag5 |= !m_UpdatedData.HasComponent(value2.m_Edge);
					}
					if ((netGeometryData.m_MergeLayers & netGeometryData2.m_MergeLayers) == 0)
					{
						continue;
					}
					Composition composition = m_CompositionDataFromEntity[value2.m_Edge];
					NetCompositionData netCompositionData = m_PrefabCompositionData[value2.m_End ? composition.m_EndNode : composition.m_StartNode];
					flag2 |= (netCompositionData.m_Flags.m_General & CompositionFlags.General.Roundabout) != 0;
					flag3 |= (netCompositionData.m_Flags.m_General & CompositionFlags.General.LevelCrossing) != 0;
					flag4 |= (netCompositionData.m_Flags.m_General & CompositionFlags.General.FixedNodeSize) != 0;
					bool flag6 = false;
					if ((netGeometryData2.m_Flags & GeometryFlags.SmoothElevation) == 0)
					{
						NetCompositionData netCompositionData2 = m_PrefabCompositionData[composition.m_Edge];
						flag6 = (netCompositionData2.m_Flags.m_General & (CompositionFlags.General.Elevated | CompositionFlags.General.Tunnel)) == 0;
						flag6 &= (netCompositionData2.m_Flags.m_Left & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered)) == 0 || (netCompositionData2.m_Flags.m_Right & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered)) == 0;
						flag6 &= !m_OwnerData.HasComponent(value2.m_Edge);
						if (!flag6)
						{
							continue;
						}
					}
					Curve curve = m_CurveDataFromEntity[value2.m_Edge];
					if (value2.m_End)
					{
						curve.m_Bezier = MathUtils.Invert(curve.m_Bezier);
					}
					float num3 = math.distance(curve.m_Bezier.b.xz, curve.m_Bezier.a.xz);
					if (num3 >= 0.1f)
					{
						if (flag6)
						{
							float2 += new float2(curve.m_Bezier.b.y, 1f) / num3;
							num2++;
						}
						else
						{
							@float += new float2(curve.m_Bezier.b.y, 1f) / num3;
							num++;
						}
					}
				}
				if (flag2 || flag3 || flag4)
				{
					value.m_Position = node2.m_Position.y;
					value.m_Flatness = 1f;
					value.m_Offset = 0f;
				}
				else if (num >= 2)
				{
					value.m_Position = math.lerp(node2.m_Position.y, @float.x / @float.y, 0.5f);
					value.m_Flatness = 0f;
					value.m_Offset = 0f;
				}
				else if (num2 >= 2)
				{
					value.m_Position = node2.m_Position.y;
					value.m_Flatness = 0f;
					value.m_Offset = node2.m_Position.y - math.lerp(node2.m_Position.y, float2.x / float2.y, 0.5f);
				}
				else
				{
					value.m_Position = node2.m_Position.y;
					value.m_Flatness = 0f;
					value.m_Offset = 0f;
				}
				value.m_Bounds.min.x = math.select(0f, 1f, flag5);
				nativeArray3[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CalculateEdgeGeometryJob : IJobParallelForDefer
	{
		[ReadOnly]
		public NativeArray<Entity> m_Entities;

		[ReadOnly]
		public Bounds3 m_TerrainBounds;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Node> m_NodeDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionDataFromEntity;

		[ReadOnly]
		public ComponentLookup<OutsideConnection> m_OutsideConnectionData;

		[ReadOnly]
		public ComponentLookup<NodeGeometry> m_NodeGeometryData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public ComponentLookup<PlaceableNetData> m_PlaceableNetData;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PlaceableObjectData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_NetLaneData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_Edges;

		[ReadOnly]
		public BufferLookup<SubNet> m_SubNets;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<NetCompositionLane> m_PrefabCompositionLanes;

		[ReadOnly]
		public BufferLookup<NetCompositionCrosswalk> m_PrefabCompositionCrosswalks;

		[ReadOnly]
		public BufferLookup<NetCompositionPiece> m_PrefabCompositionPieces;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[NativeDisableParallelForRestriction]
		[WriteOnly]
		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

		[NativeDisableParallelForRestriction]
		[WriteOnly]
		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

		public void Execute(int index)
		{
			Entity entity = m_Entities[index];
			CalculateOffsets(entity, out var startOffsets, out var endOffsets, out var startMiddleRadius, out var endMiddleRadius, out var startLeftTarget, out var startRightTarget, out var endRightTarget, out var endLeftTarget, out var leftStartCurve, out var rightStartCurve, out var leftEndCurve, out var rightEndCurve, out var startTangent, out var endTangent, out var edge, out var curve, out var nodeGeometryStart, out var nodeGeometryEnd, out var edgeCompositionData, out var startCompositionData, out var endCompositionData, out var edgePrefabGeometryData);
			Entity entity2 = entity;
			if (m_OwnerData.TryGetComponent(entity, out var componentData) && m_EdgeData.HasComponent(componentData.m_Owner))
			{
				entity2 = componentData.m_Owner;
			}
			PrefabRef prefabRef = m_PrefabRefDataFromEntity[entity2];
			m_PlaceableNetData.TryGetComponent(prefabRef.m_Prefab, out var componentData2);
			if ((componentData2.m_PlacementFlags & PlacementFlags.LinkAuxOffsets) != PlacementFlags.None)
			{
				float startMiddleRadius2;
				float endMiddleRadius2;
				float3 startLeftTarget2;
				float3 startRightTarget2;
				float3 endRightTarget2;
				float3 endLeftTarget2;
				Bezier4x3 leftStartCurve2;
				Bezier4x3 rightStartCurve2;
				Bezier4x3 leftEndCurve2;
				Bezier4x3 rightEndCurve2;
				float3 startTangent2;
				float3 endTangent2;
				Edge edge2;
				NodeGeometry nodeGeometryStart2;
				NodeGeometry nodeGeometryEnd2;
				NetCompositionData edgeCompositionData2;
				NetCompositionData startCompositionData2;
				NetCompositionData endCompositionData2;
				NetGeometryData edgePrefabGeometryData2;
				if (entity2 != entity)
				{
					CalculateOffsets(entity2, out var startOffsets2, out var endOffsets2, out startMiddleRadius2, out endMiddleRadius2, out startLeftTarget2, out startRightTarget2, out endRightTarget2, out endLeftTarget2, out leftStartCurve2, out rightStartCurve2, out leftEndCurve2, out rightEndCurve2, out startTangent2, out endTangent2, out edge2, out var curve2, out nodeGeometryStart2, out nodeGeometryEnd2, out edgeCompositionData2, out startCompositionData2, out endCompositionData2, out edgePrefabGeometryData2);
					if (math.dot(curve.m_Bezier.d.xz - curve.m_Bezier.a.xz, curve2.m_Bezier.d.xz - curve2.m_Bezier.a.xz) < 0f)
					{
						CommonUtils.Swap(ref startOffsets2, ref endOffsets2);
					}
					startOffsets = math.max(startOffsets, startOffsets2);
					endOffsets = math.max(endOffsets, endOffsets2);
				}
				if (m_SubNets.TryGetBuffer(entity2, out var bufferData))
				{
					for (int i = 0; i < bufferData.Length; i++)
					{
						Entity subNet = bufferData[i].m_SubNet;
						if (!(subNet == entity) && m_EdgeData.HasComponent(subNet))
						{
							CalculateOffsets(subNet, out var startOffsets3, out var endOffsets3, out endMiddleRadius2, out startMiddleRadius2, out endTangent2, out startTangent2, out endLeftTarget2, out endRightTarget2, out rightEndCurve2, out leftEndCurve2, out rightStartCurve2, out leftStartCurve2, out startRightTarget2, out startLeftTarget2, out edge2, out var curve3, out nodeGeometryEnd2, out nodeGeometryStart2, out endCompositionData2, out startCompositionData2, out edgeCompositionData2, out edgePrefabGeometryData2);
							if (math.dot(curve.m_Bezier.d.xz - curve.m_Bezier.a.xz, curve3.m_Bezier.d.xz - curve3.m_Bezier.a.xz) < 0f)
							{
								CommonUtils.Swap(ref startOffsets3, ref endOffsets3);
							}
							startOffsets = math.max(startOffsets, startOffsets3);
							endOffsets = math.max(endOffsets, endOffsets3);
						}
					}
				}
			}
			EdgeGeometry value = default(EdgeGeometry);
			StartNodeGeometry value2 = default(StartNodeGeometry);
			EndNodeGeometry value3 = default(EndNodeGeometry);
			value2.m_Geometry.m_MiddleRadius = startMiddleRadius;
			value3.m_Geometry.m_MiddleRadius = endMiddleRadius;
			bool num = math.all(startOffsets == 0f) && (!startLeftTarget.Equals(default(float3)) || !startRightTarget.Equals(default(float3)));
			bool flag = math.all(endOffsets == 0f) && (!endLeftTarget.Equals(default(float3)) || !endRightTarget.Equals(default(float3)));
			if (num)
			{
				leftStartCurve.a = math.lerp(leftStartCurve.a, startLeftTarget, 0.5f);
				rightStartCurve.a = math.lerp(rightStartCurve.a, startRightTarget, 0.5f);
			}
			if (flag)
			{
				leftEndCurve.d = math.lerp(leftEndCurve.d, endLeftTarget, 0.5f);
				rightEndCurve.d = math.lerp(rightEndCurve.d, endRightTarget, 0.5f);
			}
			bool num2 = (startCompositionData.m_Flags.m_General & CompositionFlags.General.FixedNodeSize) != 0;
			flag = (endCompositionData.m_Flags.m_General & CompositionFlags.General.FixedNodeSize) != 0;
			if (num2)
			{
				startOffsets = CalculateFixedOffsets(entity, edge.m_Start, isStart: true, edgePrefabGeometryData, startTangent, leftStartCurve, rightStartCurve, leftEndCurve, rightEndCurve, out startLeftTarget, out startRightTarget);
			}
			if (flag)
			{
				endOffsets = CalculateFixedOffsets(entity, edge.m_End, isStart: false, edgePrefabGeometryData, -endTangent, MathUtils.Invert(rightEndCurve), MathUtils.Invert(leftEndCurve), MathUtils.Invert(rightStartCurve), MathUtils.Invert(leftStartCurve), out endRightTarget, out endLeftTarget);
			}
			float num3 = math.min(1.98f, 2f - 0.4f / math.max(1f, curve.m_Length));
			startOffsets = math.min(startOffsets, num3);
			endOffsets = math.min(endOffsets, num3);
			float2 @float = startOffsets + endOffsets;
			if (@float.x > num3)
			{
				startOffsets.x *= num3 / @float.x;
				endOffsets.x *= num3 / @float.x;
			}
			if (@float.y > num3)
			{
				startOffsets.y *= num3 / @float.y;
				endOffsets.y *= num3 / @float.y;
			}
			float y = startTangent.y;
			float slopeSteepness = 0f - endTangent.y;
			if ((edgePrefabGeometryData.m_Flags & GeometryFlags.SymmetricalEdges) != 0)
			{
				value.m_Start.m_Left = leftStartCurve;
				value.m_Start.m_Right = rightStartCurve;
				value.m_End.m_Left = leftEndCurve;
				value.m_End.m_Right = rightEndCurve;
				ConformLengths(ref value.m_Start.m_Left, ref value.m_End.m_Left, startOffsets.x, endOffsets.x);
				ConformLengths(ref value.m_Start.m_Right, ref value.m_End.m_Right, startOffsets.y, endOffsets.y);
				LimitHeightDelta(ref value.m_Start.m_Left, ref value.m_Start.m_Right, y, leftStartCurve, rightStartCurve, edgePrefabGeometryData, edgeCompositionData, startCompositionData, nodeGeometryStart.m_Flatness);
				leftEndCurve = MathUtils.Invert(leftEndCurve);
				rightEndCurve = MathUtils.Invert(rightEndCurve);
				value.m_End.m_Left = MathUtils.Invert(value.m_End.m_Left);
				value.m_End.m_Right = MathUtils.Invert(value.m_End.m_Right);
				LimitHeightDelta(ref value.m_End.m_Left, ref value.m_End.m_Right, slopeSteepness, leftEndCurve, rightEndCurve, edgePrefabGeometryData, edgeCompositionData, endCompositionData, nodeGeometryEnd.m_Flatness);
				value.m_End.m_Left = MathUtils.Invert(value.m_End.m_Left);
				value.m_End.m_Right = MathUtils.Invert(value.m_End.m_Right);
			}
			else
			{
				float cutOffset = CalculateCutOffset(leftStartCurve, leftEndCurve, startOffsets.x, endOffsets.x, edgeCompositionData.m_Width);
				float cutOffset2 = CalculateCutOffset(rightStartCurve, rightEndCurve, startOffsets.y, endOffsets.y, edgeCompositionData.m_Width);
				value.m_Start.m_Left = Cut(leftStartCurve, leftEndCurve, startOffsets.x, endOffsets.x, cutOffset);
				value.m_Start.m_Right = Cut(rightStartCurve, rightEndCurve, startOffsets.y, endOffsets.y, cutOffset2);
				LimitHeightDelta(ref value.m_Start.m_Left, ref value.m_Start.m_Right, y, leftStartCurve, rightStartCurve, edgePrefabGeometryData, edgeCompositionData, startCompositionData, nodeGeometryStart.m_Flatness);
				leftStartCurve = MathUtils.Invert(leftStartCurve);
				rightStartCurve = MathUtils.Invert(rightStartCurve);
				leftEndCurve = MathUtils.Invert(leftEndCurve);
				rightEndCurve = MathUtils.Invert(rightEndCurve);
				value.m_End.m_Left = Cut(leftEndCurve, leftStartCurve, endOffsets.x, startOffsets.x, cutOffset);
				value.m_End.m_Right = Cut(rightEndCurve, rightStartCurve, endOffsets.y, startOffsets.y, cutOffset2);
				LimitHeightDelta(ref value.m_End.m_Left, ref value.m_End.m_Right, slopeSteepness, leftEndCurve, rightEndCurve, edgePrefabGeometryData, edgeCompositionData, endCompositionData, nodeGeometryEnd.m_Flatness);
				value.m_End.m_Left = MathUtils.Invert(value.m_End.m_Left);
				value.m_End.m_Right = MathUtils.Invert(value.m_End.m_Right);
			}
			if (num2)
			{
				value.m_Start.m_Left.a = startLeftTarget;
				value.m_Start.m_Right.a = startRightTarget;
				value.m_Start.m_Left.b.xz = startLeftTarget.xz + startTangent.xz * math.distance(value.m_Start.m_Left.a.xz, value.m_Start.m_Left.b.xz);
				value.m_Start.m_Right.b.xz = startRightTarget.xz + startTangent.xz * math.distance(value.m_Start.m_Right.a.xz, value.m_Start.m_Right.b.xz);
			}
			if (flag)
			{
				value.m_End.m_Left.d = endLeftTarget;
				value.m_End.m_Right.d = endRightTarget;
				value.m_End.m_Left.c.xz = endLeftTarget.xz - endTangent.xz * math.distance(value.m_End.m_Left.d.xz, value.m_End.m_Left.c.xz);
				value.m_End.m_Right.c.xz = endRightTarget.xz - endTangent.xz * math.distance(value.m_End.m_Right.d.xz, value.m_End.m_Right.c.xz);
			}
			if (nodeGeometryStart.m_Bounds.min.x != 0f || nodeGeometryEnd.m_Bounds.min.x != 0f)
			{
				EdgeGeometry edgeGeometry = m_EdgeGeometryData[entity];
				if (nodeGeometryStart.m_Bounds.min.x != 0f)
				{
					value.m_Start.m_Left.b.y += edgeGeometry.m_Start.m_Left.a.y - value.m_Start.m_Left.a.y;
					value.m_Start.m_Right.b.y += edgeGeometry.m_Start.m_Right.a.y - value.m_Start.m_Right.a.y;
					value.m_Start.m_Left.a.y = edgeGeometry.m_Start.m_Left.a.y;
					value.m_Start.m_Right.a.y = edgeGeometry.m_Start.m_Right.a.y;
				}
				if (nodeGeometryEnd.m_Bounds.min.x != 0f)
				{
					value.m_End.m_Left.c.y += edgeGeometry.m_End.m_Left.d.y - value.m_End.m_Left.d.y;
					value.m_End.m_Right.c.y += edgeGeometry.m_End.m_Right.d.y - value.m_End.m_Right.d.y;
					value.m_End.m_Left.d.y = edgeGeometry.m_End.m_Left.d.y;
					value.m_End.m_Right.d.y = edgeGeometry.m_End.m_Right.d.y;
				}
			}
			m_EdgeGeometryData[entity] = value;
			m_StartNodeGeometryData[entity] = value2;
			m_EndNodeGeometryData[entity] = value3;
		}

		public void CalculateOffsets(Entity entity, out float2 startOffsets, out float2 endOffsets, out float startMiddleRadius, out float endMiddleRadius, out float3 startLeftTarget, out float3 startRightTarget, out float3 endRightTarget, out float3 endLeftTarget, out Bezier4x3 leftStartCurve, out Bezier4x3 rightStartCurve, out Bezier4x3 leftEndCurve, out Bezier4x3 rightEndCurve, out float3 startTangent, out float3 endTangent, out Edge edge, out Curve curve, out NodeGeometry nodeGeometryStart, out NodeGeometry nodeGeometryEnd, out NetCompositionData edgeCompositionData, out NetCompositionData startCompositionData, out NetCompositionData endCompositionData, out NetGeometryData edgePrefabGeometryData)
		{
			edge = m_EdgeData[entity];
			curve = m_CurveDataFromEntity[entity];
			Composition composition = m_CompositionDataFromEntity[entity];
			PrefabRef prefabRef = m_PrefabRefDataFromEntity[entity];
			PrefabRef prefabRef2 = m_PrefabRefDataFromEntity[edge.m_Start];
			PrefabRef prefabRef3 = m_PrefabRefDataFromEntity[edge.m_End];
			edgePrefabGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
			NetGeometryData netGeometryData = m_PrefabGeometryData[prefabRef2.m_Prefab];
			NetGeometryData netGeometryData2 = m_PrefabGeometryData[prefabRef3.m_Prefab];
			edgeCompositionData = m_PrefabCompositionData[composition.m_Edge];
			startCompositionData = m_PrefabCompositionData[composition.m_StartNode];
			endCompositionData = m_PrefabCompositionData[composition.m_EndNode];
			DynamicBuffer<NetCompositionLane> nodeCompositionLanes = m_PrefabCompositionLanes[composition.m_Edge];
			DynamicBuffer<NetCompositionCrosswalk> nodeCompositionCrosswalks = m_PrefabCompositionCrosswalks[composition.m_StartNode];
			DynamicBuffer<NetCompositionCrosswalk> nodeCompositionCrosswalks2 = m_PrefabCompositionCrosswalks[composition.m_EndNode];
			if (m_OwnerData.TryGetComponent(entity, out var componentData) && m_CompositionDataFromEntity.TryGetComponent(componentData.m_Owner, out var componentData2))
			{
				NetCompositionData netCompositionData = m_PrefabCompositionData[componentData2.m_Edge];
				edgeCompositionData.m_NodeOffset = math.max(edgeCompositionData.m_NodeOffset, netCompositionData.m_NodeOffset);
			}
			float roundaboutSize = 0f;
			float roundaboutSize2 = 0f;
			startMiddleRadius = 0f;
			endMiddleRadius = 0f;
			bool flag = ((netGeometryData.m_MergeLayers ^ edgePrefabGeometryData.m_MergeLayers) & Layer.Waterway) == 0;
			bool flag2 = ((netGeometryData2.m_MergeLayers ^ edgePrefabGeometryData.m_MergeLayers) & Layer.Waterway) == 0;
			CutCurve(edge, ref curve, flag, flag2);
			float t = NetUtils.FindMiddleTangentPos(curve.m_Bezier.xz, new float2(0f, 1f));
			MathUtils.Divide(curve.m_Bezier, out var output, out var output2, t);
			nodeGeometryStart = m_NodeGeometryData[edge.m_Start];
			nodeGeometryEnd = m_NodeGeometryData[edge.m_End];
			if ((startCompositionData.m_Flags.m_General & CompositionFlags.General.Roundabout) != 0)
			{
				DynamicBuffer<NetCompositionPiece> pieces = m_PrefabCompositionPieces[composition.m_Edge];
				roundaboutSize = NetCompositionHelpers.CalculateRoundaboutSize(startCompositionData, pieces).x;
				startMiddleRadius = CalculateMiddleRadius(edge.m_Start, edgePrefabGeometryData);
			}
			else if ((startCompositionData.m_Flags.m_General & (CompositionFlags.General.LevelCrossing | CompositionFlags.General.FixedNodeSize)) == 0)
			{
				if (flag)
				{
					curve.m_Bezier.a.y = nodeGeometryStart.m_Position;
				}
				output.a.y = curve.m_Bezier.a.y;
				output.b.y += nodeGeometryStart.m_Offset;
				output.c.y -= nodeGeometryStart.m_Offset * 0.375f;
				output.d.y -= nodeGeometryStart.m_Offset * 0.1875f;
				output2.a.y -= nodeGeometryStart.m_Offset * 0.1875f;
				output2.b.y += nodeGeometryStart.m_Offset * 0.125f;
			}
			if ((endCompositionData.m_Flags.m_General & CompositionFlags.General.Roundabout) != 0)
			{
				DynamicBuffer<NetCompositionPiece> pieces2 = m_PrefabCompositionPieces[composition.m_Edge];
				roundaboutSize2 = NetCompositionHelpers.CalculateRoundaboutSize(endCompositionData, pieces2).y;
				endMiddleRadius = CalculateMiddleRadius(edge.m_End, edgePrefabGeometryData);
			}
			else if ((endCompositionData.m_Flags.m_General & (CompositionFlags.General.LevelCrossing | CompositionFlags.General.FixedNodeSize)) == 0)
			{
				if (flag2)
				{
					curve.m_Bezier.d.y = nodeGeometryEnd.m_Position;
				}
				output2.d.y = curve.m_Bezier.d.y;
				output2.c.y += nodeGeometryEnd.m_Offset;
				output2.b.y -= nodeGeometryEnd.m_Offset * 0.375f;
				output2.a.y -= nodeGeometryEnd.m_Offset * 0.1875f;
				output.d.y -= nodeGeometryEnd.m_Offset * 0.1875f;
				output.c.y += nodeGeometryEnd.m_Offset * 0.125f;
			}
			float num = math.distance(output.c.xz, output.d.xz);
			float num2 = math.distance(output2.b.xz, output2.a.xz);
			float num3 = math.lerp(output.c.y, output2.b.y, num / math.max(0.1f, num + num2)) - output.d.y;
			output.c.y -= num3 * 0.4f;
			output.d.y += num3 * 0.6f;
			output2.a.y += num3 * 0.6f;
			output2.b.y -= num3 * 0.4f;
			float2 @float = edgeCompositionData.m_Width * new float2(0.5f, -0.5f) + edgeCompositionData.m_MiddleOffset;
			leftStartCurve = NetUtils.OffsetCurveLeftSmooth(output, @float.x);
			rightStartCurve = NetUtils.OffsetCurveLeftSmooth(output, @float.y);
			leftEndCurve = NetUtils.OffsetCurveLeftSmooth(output2, @float.x);
			rightEndCurve = NetUtils.OffsetCurveLeftSmooth(output2, @float.y);
			if ((edgeCompositionData.m_State & CompositionState.Airspace) != 0)
			{
				OffsetAirspaceCurves(edge, ref leftStartCurve, ref rightStartCurve, ref leftEndCurve, ref rightEndCurve);
			}
			startTangent = MathUtils.StartTangent(output);
			endTangent = MathUtils.EndTangent(output2);
			startTangent = MathUtils.Normalize(startTangent, startTangent.xz);
			endTangent = MathUtils.Normalize(endTangent, endTangent.xz);
			startTangent.y = math.clamp(startTangent.y, -1f, 1f);
			endTangent.y = math.clamp(endTangent.y, -1f, 1f);
			float y = startTangent.y;
			float slopeSteepness = 0f - endTangent.y;
			float2 float2 = startCompositionData.m_Width * new float2(0.5f, -0.5f) + startCompositionData.m_MiddleOffset;
			float2 float3 = endCompositionData.m_Width * new float2(0.5f, -0.5f) + endCompositionData.m_MiddleOffset;
			bool2 useEdgeWidth = new bool2(@float.x - float2.x > 0.001f, float2.y - @float.y > 0.001f);
			bool2 useEdgeWidth2 = new bool2(@float.x - float3.x > 0.001f, float3.y - @float.y > 0.001f);
			startOffsets = CalculateCornerOffset(entity, edge.m_Start, y, useEdgeWidth, curve, leftStartCurve, rightStartCurve, leftEndCurve, rightEndCurve, prefabRef, edgePrefabGeometryData, edgeCompositionData, startCompositionData, nodeCompositionLanes, nodeCompositionCrosswalks, startMiddleRadius, roundaboutSize, nodeGeometryStart.m_Offset, isEnd: false, out startLeftTarget, out startRightTarget);
			endOffsets = CalculateCornerOffset(entity, edge.m_End, slopeSteepness, useEdgeWidth2, curve, MathUtils.Invert(rightEndCurve), MathUtils.Invert(leftEndCurve), MathUtils.Invert(rightStartCurve), MathUtils.Invert(leftStartCurve), prefabRef, edgePrefabGeometryData, edgeCompositionData, endCompositionData, nodeCompositionLanes, nodeCompositionCrosswalks2, endMiddleRadius, roundaboutSize2, nodeGeometryEnd.m_Offset, isEnd: true, out endRightTarget, out endLeftTarget).yx;
			if (math.any(math.abs(float2 - @float) > 0.001f) || math.any(math.abs(float3 - @float) > 0.001f))
			{
				Bezier4x3 bezier4x = NetUtils.OffsetCurveLeftSmooth(output, float2.x);
				Bezier4x3 bezier4x2 = NetUtils.OffsetCurveLeftSmooth(output, float2.y);
				Bezier4x3 bezier4x3 = NetUtils.OffsetCurveLeftSmooth(output2, float3.x);
				Bezier4x3 bezier4x4 = NetUtils.OffsetCurveLeftSmooth(output2, float3.y);
				startOffsets = math.max(startOffsets, CalculateCornerOffset(entity, edge.m_Start, y, useEdgeWidth, curve, bezier4x, bezier4x2, bezier4x3, bezier4x4, prefabRef, edgePrefabGeometryData, edgeCompositionData, startCompositionData, nodeCompositionLanes, nodeCompositionCrosswalks, 0f, 0f, nodeGeometryStart.m_Offset, isEnd: false, out var leftTarget, out var rightTarget));
				endOffsets = math.max(endOffsets, CalculateCornerOffset(entity, edge.m_End, slopeSteepness, useEdgeWidth2, curve, MathUtils.Invert(bezier4x4), MathUtils.Invert(bezier4x3), MathUtils.Invert(bezier4x2), MathUtils.Invert(bezier4x), prefabRef, edgePrefabGeometryData, edgeCompositionData, endCompositionData, nodeCompositionLanes, nodeCompositionCrosswalks2, 0f, 0f, nodeGeometryEnd.m_Offset, isEnd: true, out rightTarget, out leftTarget).yx);
			}
		}

		private float2 CalculateFixedOffsets(Entity edge, Entity node, bool isStart, NetGeometryData prefabGeometryData, float3 startTangent, Bezier4x3 leftStartCurve, Bezier4x3 rightStartCurve, Bezier4x3 leftEndCurve, Bezier4x3 rightEndCurve, out float3 leftSnapPos, out float3 rightSnapPos)
		{
			float num = GetFixedNodeOffset(node, prefabGeometryData);
			if (m_OwnerData.TryGetComponent(edge, out var componentData) && m_EdgeData.TryGetComponent(componentData.m_Owner, out var componentData2))
			{
				num = math.max(num, GetFixedNodeOffset(isStart ? componentData2.m_Start : componentData2.m_End, prefabGeometryData));
			}
			startTangent *= num;
			leftSnapPos = leftStartCurve.a;
			rightSnapPos = rightStartCurve.a;
			if (m_NodeDataFromEntity.TryGetComponent(node, out var componentData3))
			{
				float3 @float = (leftSnapPos + rightSnapPos) * 0.5f;
				leftSnapPos += componentData3.m_Position - @float;
				rightSnapPos += componentData3.m_Position - @float;
			}
			leftSnapPos.xz += startTangent.xz;
			rightSnapPos.xz += startTangent.xz;
			Line2.Segment line = new Line2.Segment(leftSnapPos.xz + MathUtils.Left(startTangent.xz), rightSnapPos.xz + MathUtils.Right(startTangent.xz));
			float2 result = 1f;
			if (MathUtils.Intersect(leftStartCurve.xz, line, out var t, 4))
			{
				result.x = t.x;
			}
			else if (MathUtils.Intersect(leftEndCurve.xz, line, out t, 4))
			{
				result.x = 1f + t.x;
			}
			if (MathUtils.Intersect(rightStartCurve.xz, line, out t, 4))
			{
				result.y = t.x;
			}
			else if (MathUtils.Intersect(rightEndCurve.xz, line, out t, 4))
			{
				result.y = 1f + t.x;
			}
			return result;
		}

		private void OffsetAirspaceCurves(Edge edge, ref Bezier4x3 leftStartCurve, ref Bezier4x3 rightStartCurve, ref Bezier4x3 leftEndCurve, ref Bezier4x3 rightEndCurve)
		{
			float4 @float = (leftStartCurve.y.abcd + rightStartCurve.y.abcd) * 0.5f;
			float4 float2 = (leftEndCurve.y.abcd + rightEndCurve.y.abcd) * 0.5f;
			float2 float3 = 0f;
			float2 float4 = new float2(@float.x, float2.w);
			if (m_ElevationData.TryGetComponent(edge.m_Start, out var componentData))
			{
				float3.x = math.csum(componentData.m_Elevation) * 0.5f;
			}
			if (m_ElevationData.TryGetComponent(edge.m_End, out var componentData2))
			{
				float3.y = math.csum(componentData2.m_Elevation) * 0.5f;
			}
			float4 float5 = math.lerp(float3.x, float3.y, math.saturate((@float - float4.x) / (float4.y - float4.x)));
			float4 float6 = math.lerp(float3.x, float3.y, math.saturate((float2 - float4.x) / (float4.y - float4.x)));
			float3 float7 = math.normalizesafe(rightStartCurve.a - leftStartCurve.a) * float5.x;
			float3 float8 = math.normalizesafe(rightStartCurve.b - leftStartCurve.b) * float5.y;
			float3 float9 = math.normalizesafe(rightStartCurve.c - leftStartCurve.c) * float5.z;
			float3 float10 = math.normalizesafe(rightStartCurve.d - leftStartCurve.d) * float5.w;
			leftStartCurve.a -= float7;
			leftStartCurve.b -= float8;
			leftStartCurve.c -= float9;
			leftStartCurve.d -= float10;
			rightStartCurve.a += float7;
			rightStartCurve.b += float8;
			rightStartCurve.c += float9;
			rightStartCurve.d += float10;
			float3 float11 = math.normalizesafe(rightEndCurve.a - leftEndCurve.a) * float6.x;
			float3 float12 = math.normalizesafe(rightEndCurve.b - leftEndCurve.b) * float6.y;
			float3 float13 = math.normalizesafe(rightEndCurve.c - leftEndCurve.c) * float6.z;
			float3 float14 = math.normalizesafe(rightEndCurve.d - leftEndCurve.d) * float6.w;
			leftEndCurve.a -= float11;
			leftEndCurve.b -= float12;
			leftEndCurve.c -= float13;
			leftEndCurve.d -= float14;
			rightEndCurve.a += float11;
			rightEndCurve.b += float12;
			rightEndCurve.c += float13;
			rightEndCurve.d += float14;
		}

		private void CutCurve(Edge edge, ref Curve curve, bool useStartNodeHeight, bool useEndNodeHeight)
		{
			Node node = m_NodeDataFromEntity[edge.m_Start];
			Node node2 = m_NodeDataFromEntity[edge.m_End];
			MathUtils.Distance(curve.m_Bezier.xz, node.m_Position.xz, out var t);
			MathUtils.Distance(curve.m_Bezier.xz, node2.m_Position.xz, out var t2);
			if (t < 0.001f)
			{
				t = 0f;
			}
			if (t2 > 0.999f)
			{
				t2 = 1f;
			}
			if (t != 0f || t2 != 1f)
			{
				if (t2 < t + 0.02f)
				{
					t = (t2 = (t + t2) * 0.5f);
					t = math.max(0f, t - 0.01f);
					t2 = math.min(1f, t2 + 0.01f);
				}
				if (!useStartNodeHeight)
				{
					node.m_Position.y = curve.m_Bezier.a.y;
				}
				if (!useEndNodeHeight)
				{
					node2.m_Position.y = curve.m_Bezier.d.y;
				}
				curve.m_Bezier = MathUtils.Cut(curve.m_Bezier, new float2(t, t2));
				curve.m_Bezier.a.y = node.m_Position.y;
				curve.m_Bezier.d.y = node2.m_Position.y;
			}
		}

		private float CalculateMiddleRadius(Entity node, NetGeometryData netGeometryData)
		{
			float num = 0f;
			if (m_SubObjects.TryGetBuffer(node, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					PrefabRef prefabRef = m_PrefabRefDataFromEntity[bufferData[i].m_SubObject];
					if (m_PlaceableObjectData.TryGetComponent(prefabRef.m_Prefab, out var componentData) && m_ObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData2) && (componentData.m_Flags & Game.Objects.PlacementFlags.RoadNode) != Game.Objects.PlacementFlags.None)
					{
						float num2 = math.cmax(componentData2.m_Size.xz) * 0.5f;
						if ((componentData2.m_Flags & Game.Objects.GeometryFlags.Standing) != Game.Objects.GeometryFlags.None)
						{
							num2 = ((!(componentData2.m_LegSize.y >= netGeometryData.m_DefaultHeightRange.max)) ? math.max(num2, math.cmax(componentData2.m_LegSize.xz * 0.5f + componentData2.m_LegOffset)) : math.cmax(componentData2.m_LegSize.xz * 0.5f + componentData2.m_LegOffset));
						}
						num = math.max(num, num2 + 1f);
					}
				}
			}
			return num;
		}

		private float GetFixedNodeOffset(Entity node, NetGeometryData netGeometryData)
		{
			float num = 0f;
			if (m_SubObjects.TryGetBuffer(node, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					PrefabRef prefabRef = m_PrefabRefDataFromEntity[bufferData[i].m_SubObject];
					if (m_PlaceableObjectData.TryGetComponent(prefabRef.m_Prefab, out var componentData) && (componentData.m_Flags & Game.Objects.PlacementFlags.RoadNode) != Game.Objects.PlacementFlags.None)
					{
						num = math.max(num, componentData.m_PlacementOffset.z);
					}
				}
			}
			return num;
		}

		private void LimitHeightDelta(ref Bezier4x3 left, ref Bezier4x3 right, float slopeSteepness, Bezier4x3 originalLeft, Bezier4x3 originalRight, NetGeometryData prefabGeometryData, NetCompositionData edgeCompositionData, NetCompositionData nodeCompositionData, float nodeFlatness)
		{
			if ((nodeCompositionData.m_Flags.m_General & (CompositionFlags.General.LevelCrossing | CompositionFlags.General.FixedNodeSize)) != 0)
			{
				left.a.y = originalLeft.a.y;
				left.b.y = originalLeft.a.y;
				right.a.y = originalRight.a.y;
				right.b.y = originalRight.a.y;
				return;
			}
			float num = math.max(edgeCompositionData.m_NodeOffset, nodeCompositionData.m_NodeOffset) + math.abs(slopeSteepness) * edgeCompositionData.m_Width * 0.25f;
			float num2 = prefabGeometryData.m_MaxSlopeSteepness * num * 1.5f;
			float num3 = math.max(math.min(0f, originalLeft.a.y + num2 - left.a.y), originalLeft.a.y - num2 - left.a.y);
			float num4 = math.max(math.min(0f, originalRight.a.y + num2 - right.a.y), originalRight.a.y - num2 - right.a.y);
			left.a.y += num3 * nodeFlatness;
			left.b.y += num3 * nodeFlatness;
			right.a.y += num4 * nodeFlatness;
			right.b.y += num4 * nodeFlatness;
		}

		private float CalculateCutOffset(Bezier4x3 start, Bezier4x3 end, float startOffset, float endOffset, float width)
		{
			float num = ((startOffset >= 1f) ? MathUtils.Length(end.xz, new Bounds1(startOffset - 1f, 1f - endOffset)) : ((!(endOffset >= 1f)) ? (MathUtils.Length(start.xz, new Bounds1(startOffset, 1f)) + MathUtils.Length(end.xz, new Bounds1(0f, 1f - endOffset))) : MathUtils.Length(start.xz, new Bounds1(startOffset, 2f - endOffset))));
			return 1f - (2f - startOffset - endOffset) * 0.5f / (num / math.max(0.01f, width) + 1f);
		}

		private Bezier4x3 Cut(Bezier4x3 start, Bezier4x3 end, float startOffset, float endOffset, float cutOffset)
		{
			if (startOffset >= 1f)
			{
				return MathUtils.Cut(end, new float2(startOffset - 1f, startOffset - cutOffset));
			}
			if (startOffset > cutOffset)
			{
				float3 startPos = MathUtils.Position(start, startOffset);
				float3 endPos = MathUtils.Position(end, startOffset - cutOffset);
				float3 value = MathUtils.Tangent(start, startOffset);
				float3 value2 = MathUtils.Tangent(end, startOffset - cutOffset);
				value = MathUtils.Normalize(value, value.xz);
				value2 = MathUtils.Normalize(value2, value2.xz);
				value.y = math.clamp(value.y, -1f, 1f);
				value2.y = math.clamp(value2.y, -1f, 1f);
				return NetUtils.FitCurve(startPos, value, value2, endPos);
			}
			return MathUtils.Cut(start, new float2(startOffset, math.min(1f, 1f + cutOffset - endOffset)));
		}

		private void ConformLengths(ref Bezier4x3 start, ref Bezier4x3 end, float startOffset, float endOffset)
		{
			if (startOffset >= 1f)
			{
				Bezier4x2 xz = end.xz;
				Bounds1 t = new Bounds1(startOffset - 1f, 1f - endOffset);
				float num = MathUtils.Length(xz, t);
				MathUtils.ClampLength(xz, ref t, num * 0.5f);
				start = MathUtils.Cut(end, t);
				end = MathUtils.Cut(end, new Bounds1(t.max, 1f - endOffset));
				return;
			}
			if (endOffset >= 1f)
			{
				Bezier4x2 xz2 = start.xz;
				Bounds1 t2 = new Bounds1(startOffset, 2f - endOffset);
				float num2 = MathUtils.Length(xz2, t2);
				MathUtils.ClampLengthInverse(xz2, ref t2, num2 * 0.5f);
				end = MathUtils.Cut(start, t2);
				start = MathUtils.Cut(start, new Bounds1(startOffset, t2.min));
				return;
			}
			Bezier4x2 xz3 = start.xz;
			Bezier4x2 xz4 = end.xz;
			Bounds1 t3 = new Bounds1(startOffset, 1f);
			Bounds1 t4 = new Bounds1(0f, 1f - endOffset);
			float num3 = MathUtils.Length(xz3, t3);
			float num4 = MathUtils.Length(xz4, t4);
			float3 @float;
			float3 value;
			if (num3 > num4)
			{
				MathUtils.ClampLength(xz3, ref t3, math.lerp(num3, num4, 0.5f));
				@float = MathUtils.Position(start, t3.max);
				value = MathUtils.Tangent(start, t3.max);
			}
			else
			{
				MathUtils.ClampLengthInverse(xz4, ref t4, math.lerp(num3, num4, 0.5f));
				@float = MathUtils.Position(end, t4.min);
				value = MathUtils.Tangent(end, t4.min);
			}
			float3 startPos = MathUtils.Position(start, startOffset);
			float3 value2 = MathUtils.Tangent(start, startOffset);
			float3 endPos = MathUtils.Position(end, 1f - endOffset);
			float3 value3 = MathUtils.Tangent(end, 1f - endOffset);
			value2 = MathUtils.Normalize(value2, value2.xz);
			value = MathUtils.Normalize(value, value.xz);
			value3 = MathUtils.Normalize(value3, value3.xz);
			start = NetUtils.FitCurve(startPos, value2, value, @float);
			end = NetUtils.FitCurve(@float, value, value3, endPos);
		}

		private float2 CalculateCornerOffset(Entity edge, Entity node, float slopeSteepness, bool2 useEdgeWidth, Curve curveData, Bezier4x3 leftStartCurve, Bezier4x3 rightStartCurve, Bezier4x3 leftEndCurve, Bezier4x3 rightEndCurve, PrefabRef prefabRef, NetGeometryData prefabGeometryData, NetCompositionData edgeCompositionData, NetCompositionData nodeCompositionData, DynamicBuffer<NetCompositionLane> nodeCompositionLanes, DynamicBuffer<NetCompositionCrosswalk> nodeCompositionCrosswalks, float middleRadius, float roundaboutSize, float bOffset, bool isEnd, out float3 leftTarget, out float3 rightTarget)
		{
			float2 @float = default(float2);
			leftTarget = default(float3);
			rightTarget = default(float3);
			if (isEnd)
			{
				curveData.m_Bezier = MathUtils.Invert(curveData.m_Bezier);
				edgeCompositionData.m_MiddleOffset = 0f - edgeCompositionData.m_MiddleOffset;
			}
			float2 value = MathUtils.StartTangent(leftStartCurve).xz;
			float2 value2 = MathUtils.StartTangent(rightStartCurve).xz;
			float2 value3 = leftStartCurve.a.xz - rightStartCurve.a.xz;
			MathUtils.TryNormalize(ref value);
			MathUtils.TryNormalize(ref value2);
			MathUtils.TryNormalize(ref value3);
			float2 float2 = default(float2);
			float2 float3 = default(float2);
			float2 float4 = float.MinValue;
			float2 float5 = 0f;
			float num = prefabGeometryData.m_MinNodeOffset;
			bool flag = middleRadius > 0f;
			float num2 = math.max(edgeCompositionData.m_NodeOffset, nodeCompositionData.m_NodeOffset);
			float num3 = num2 + math.abs(slopeSteepness) * nodeCompositionData.m_Width * 0.25f;
			bool flag2 = true;
			EdgeIterator edgeIterator = new EdgeIterator(edge, node, m_Edges, m_EdgeData, m_TempData, m_HiddenData);
			EdgeIteratorValue value4;
			float2 float7 = default(float2);
			Bezier4x2 nodeCurve = default(Bezier4x2);
			while (edgeIterator.GetNext(out value4))
			{
				if (!(value4.m_Edge != edge))
				{
					continue;
				}
				PrefabRef prefabRef2 = m_PrefabRefDataFromEntity[value4.m_Edge];
				NetGeometryData netGeometryData = m_PrefabGeometryData[prefabRef2.m_Prefab];
				if ((netGeometryData.m_IntersectLayers & prefabGeometryData.m_IntersectLayers) == 0)
				{
					continue;
				}
				Edge edge2 = m_EdgeData[value4.m_Edge];
				Curve curve = m_CurveDataFromEntity[value4.m_Edge];
				Composition composition = m_CompositionDataFromEntity[value4.m_Edge];
				NetCompositionData edgeCompositionData2 = m_PrefabCompositionData[composition.m_Edge];
				DynamicBuffer<NetCompositionLane> prefabCompositionLanes = m_PrefabCompositionLanes[composition.m_Edge];
				NetCompositionData netCompositionData;
				DynamicBuffer<NetCompositionCrosswalk> prefabCompositionCrosswalks;
				if (value4.m_End)
				{
					netCompositionData = m_PrefabCompositionData[composition.m_EndNode];
					prefabCompositionCrosswalks = m_PrefabCompositionCrosswalks[composition.m_EndNode];
					edgeCompositionData2.m_MiddleOffset = 0f - edgeCompositionData2.m_MiddleOffset;
				}
				else
				{
					netCompositionData = m_PrefabCompositionData[composition.m_StartNode];
					prefabCompositionCrosswalks = m_PrefabCompositionCrosswalks[composition.m_StartNode];
				}
				if (m_OwnerData.TryGetComponent(value4.m_Edge, out var componentData) && m_CompositionDataFromEntity.TryGetComponent(componentData.m_Owner, out var componentData2))
				{
					NetCompositionData netCompositionData2 = m_PrefabCompositionData[componentData2.m_Edge];
					edgeCompositionData2.m_WidthOffset = math.max(edgeCompositionData2.m_WidthOffset, netCompositionData2.m_WidthOffset);
					netCompositionData.m_WidthOffset = math.max(netCompositionData.m_WidthOffset, netCompositionData2.m_WidthOffset);
				}
				CutCurve(edge2, ref curve, useStartNodeHeight: true, useEndNodeHeight: true);
				float t = NetUtils.FindMiddleTangentPos(curve.m_Bezier.xz, new float2(0f, 1f));
				MathUtils.Divide(curve.m_Bezier, out var output, out var output2, t);
				if (value4.m_End)
				{
					curve.m_Bezier = MathUtils.Invert(curve.m_Bezier);
					Bezier4x3 bezier4x = MathUtils.Invert(output);
					output = MathUtils.Invert(output2);
					output2 = bezier4x;
				}
				curve.m_Bezier.a.y = curveData.m_Bezier.a.y;
				output.a.y = curveData.m_Bezier.a.y;
				output.b.y += bOffset;
				bool flag3 = false;
				if ((netGeometryData.m_MergeLayers & prefabGeometryData.m_MergeLayers) != Layer.None)
				{
					num = math.max(num, netGeometryData.m_MinNodeOffset);
					if (middleRadius > 0f)
					{
						DynamicBuffer<NetCompositionPiece> pieces = m_PrefabCompositionPieces[composition.m_Edge];
						float2 float6 = NetCompositionHelpers.CalculateRoundaboutSize(netCompositionData, pieces);
						roundaboutSize = math.max(roundaboutSize, math.select(float6.x, float6.y, value4.m_End));
					}
					float3 value5 = MathUtils.StartTangent(output);
					value5 = MathUtils.Normalize(value5, value5.xz);
					value5.y = math.clamp(value5.y, -1f, 1f);
					float num4 = 0f - value5.y;
					float7.x = math.dot(curve.m_Bezier.a.xz - leftStartCurve.a.xz, value);
					float7.y = math.dot(curve.m_Bezier.a.xz - rightStartCurve.a.xz, value2);
					float4 = math.max(float4, float7 * 0.5f);
					float num5 = math.abs(slopeSteepness - num4) * nodeCompositionData.m_Width;
					bool dontCrossTracks = false;
					if (((edgeCompositionData.m_State | edgeCompositionData2.m_State) & (CompositionState.HasForwardTrackLanes | CompositionState.HasBackwardTrackLanes)) != 0 && ((edgeCompositionData.m_State | edgeCompositionData2.m_State) & (CompositionState.HasForwardRoadLanes | CompositionState.HasBackwardRoadLanes)) == 0 && (nodeCompositionData.m_Flags.m_General & CompositionFlags.General.Intersection) == 0)
					{
						dontCrossTracks = GetTopOwner(edge) == GetTopOwner(value4.m_Edge);
					}
					float4 float8 = (1f - nodeCompositionData.m_SyncVertexOffsetsLeft) * (nodeCompositionData.m_Width * 0.5f + nodeCompositionData.m_MiddleOffset);
					float4 float9 = nodeCompositionData.m_SyncVertexOffsetsRight * (nodeCompositionData.m_Width * 0.5f - nodeCompositionData.m_MiddleOffset);
					float4 float10 = (1f - netCompositionData.m_SyncVertexOffsetsLeft.wzyx) * (netCompositionData.m_Width * 0.5f + netCompositionData.m_MiddleOffset);
					float4 float11 = netCompositionData.m_SyncVertexOffsetsRight.wzyx * (netCompositionData.m_Width * 0.5f - netCompositionData.m_MiddleOffset);
					float2 float12 = edgeCompositionData.m_Width * 0.5f + new float2(edgeCompositionData.m_MiddleOffset, 0f - edgeCompositionData.m_MiddleOffset);
					float2 float13 = edgeCompositionData2.m_Width * 0.5f + new float2(0f - edgeCompositionData2.m_MiddleOffset, edgeCompositionData2.m_MiddleOffset);
					float num6 = math.dot(curveData.m_Bezier.a.xz - curve.m_Bezier.a.xz, value3);
					float2 x = new float2(math.cmax(math.abs(float8 - float11 + num6)), math.cmax(math.abs(float9 - float10 - num6)));
					x = math.max(x, math.abs(float12 - float13 + new float2(num6, 0f - num6)));
					float num7 = math.max(0.1f, math.min(edgeCompositionData.m_Width, edgeCompositionData2.m_Width));
					x *= num7 / (num7 + x * 0.75f);
					x = math.max(x, CompareLanes(nodeCompositionLanes, prefabCompositionLanes, num6, isEnd, value4.m_End, dontCrossTracks));
					if ((nodeCompositionData.m_Flags.m_General & CompositionFlags.General.Crosswalk) != 0)
					{
						x = math.max(x, CheckCrosswalks(nodeCompositionCrosswalks));
					}
					if ((netCompositionData.m_Flags.m_General & CompositionFlags.General.Crosswalk) != 0)
					{
						x = math.max(x, CheckCrosswalks(prefabCompositionCrosswalks));
					}
					x *= math.max(0f, math.dot(value5.xz, -value));
					if (num5 > 0.2f)
					{
						float5 = math.max(float5, num5);
					}
					if (prefabRef2.m_Prefab != prefabRef.m_Prefab)
					{
						float5 = math.max(float5, num2);
					}
					if (math.any(x > 0.1f))
					{
						float5 = math.max(float5, x);
						flag3 = true;
					}
					if (math.distancesq(curve.m_Bezier.a.xz, curveData.m_Bezier.a.xz) > 0.01f || math.dot(value5.xz, -value) < 0.9995f)
					{
						flag3 = true;
						flag = true;
					}
					else if (RequireTransition(nodeCompositionData, netCompositionData, edgeCompositionData, edgeCompositionData2))
					{
						flag3 = true;
					}
					if (flag3)
					{
						netCompositionData.m_Width += num3 * 2f;
						netCompositionData.m_Width += netCompositionData.m_WidthOffset;
					}
					if (!flag2)
					{
						flag = true;
					}
				}
				else
				{
					flag3 = true;
					flag = true;
					netCompositionData.m_Width += num2 * 2f;
					netCompositionData.m_Width += netCompositionData.m_WidthOffset;
				}
				float2 float14 = netCompositionData.m_Width * new float2(0.5f, -0.5f) - netCompositionData.m_MiddleOffset;
				if (math.any(useEdgeWidth))
				{
					if (flag3)
					{
						edgeCompositionData2.m_Width += (((netGeometryData.m_MergeLayers & prefabGeometryData.m_MergeLayers) != Layer.None) ? num3 : num2) * 2f;
						edgeCompositionData2.m_Width += edgeCompositionData2.m_WidthOffset;
					}
					float2 float15 = edgeCompositionData2.m_Width * new float2(0.5f, -0.5f) - edgeCompositionData2.m_MiddleOffset;
					if (useEdgeWidth.y)
					{
						float14.x = math.max(float14.x, float15.x);
					}
					if (useEdgeWidth.x)
					{
						float14.y = math.min(float14.y, float15.y);
					}
				}
				Bezier4x3 curve2 = NetUtils.OffsetCurveLeftSmooth(output, float14.x);
				Bezier4x3 bezier4x2 = NetUtils.OffsetCurveLeftSmooth(output2, float14.x);
				Bezier4x3 curve3 = NetUtils.OffsetCurveLeftSmooth(output, float14.y);
				Bezier4x3 bezier4x3 = NetUtils.OffsetCurveLeftSmooth(output2, float14.y);
				float2 value6 = MathUtils.StartTangent(curve2).xz;
				float2 value7 = MathUtils.StartTangent(curve3).xz;
				MathUtils.TryNormalize(ref value6);
				MathUtils.TryNormalize(ref value7);
				if (flag3)
				{
					float2 float16 = math.max(nodeCompositionData.m_Width, netCompositionData.m_Width * 0.5f);
					float16 = math.max(num3, float16 * math.saturate(new float2(math.dot(value2, value6), math.dot(value, value7)) + 1f));
					nodeCurve.a = curve2.a.xz;
					nodeCurve.b = curve2.a.xz - value6 * (float16.x * 1.3333334f);
					nodeCurve.c = curve3.a.xz - value7 * (float16.y * 1.3333334f);
					nodeCurve.d = curve3.a.xz;
					float2 float17 = Intersect(leftStartCurve.xz, leftEndCurve.xz, nodeCurve, curve3.xz, bezier4x3.xz);
					float2 float18 = Intersect(rightStartCurve.xz, rightEndCurve.xz, nodeCurve, curve2.xz, bezier4x2.xz);
					if (float17.x > 0f)
					{
						if (float17.x > @float.x)
						{
							@float.x = float17.x;
						}
						value7 = Tangent(curve3.xz, bezier4x3.xz, float17.y);
						MathUtils.TryNormalize(ref value7);
					}
					if (float18.x > 0f)
					{
						if (float18.x > @float.y)
						{
							@float.y = float18.x;
						}
						value6 = Tangent(curve2.xz, bezier4x2.xz, float18.y);
						MathUtils.TryNormalize(ref value6);
					}
				}
				else
				{
					leftTarget = curve3.a;
					rightTarget = curve2.a;
				}
				if (flag2)
				{
					float2 = value7;
				}
				else if (math.dot(value7, value3) > 0f)
				{
					if (math.dot(float2, value3) <= 0f || math.dot(value7, value) >= math.dot(float2, value))
					{
						float2 = value7;
					}
				}
				else if (math.dot(float2, value3) <= 0f && math.dot(value7, value) <= math.dot(float2, value))
				{
					float2 = value7;
				}
				if (flag2)
				{
					float3 = value6;
				}
				else if (math.dot(value6, value3) < 0f)
				{
					if (math.dot(float3, value3) >= 0f || math.dot(value6, value2) >= math.dot(float3, value2))
					{
						float3 = value6;
					}
				}
				else if (math.dot(float3, value3) >= 0f && math.dot(value6, value2) <= math.dot(float3, value2))
				{
					float3 = value6;
				}
				flag2 = false;
			}
			if (math.any((float4 > 0.1f) | (float5 > 0.1f)) || flag)
			{
				if (flag)
				{
					float5 = math.max(float5, num);
				}
				float4 += float5;
				if (middleRadius > 0f)
				{
					float num8 = middleRadius + roundaboutSize;
					float3 position = m_NodeDataFromEntity[node].m_Position;
					float2 float19 = default(float2);
					float19.x = math.dot(position.xz - leftStartCurve.a.xz, value);
					float19.y = math.dot(position.xz - rightStartCurve.a.xz, value2);
					float4 = math.max(float4, num8 + num2 + float19);
				}
				if (float4.x > 0f)
				{
					Bounds1 t2 = new Bounds1(0f, 1f);
					if (MathUtils.ClampLength(leftStartCurve, ref t2, float4.x))
					{
						@float.x = math.max(@float.x, t2.max);
					}
					else
					{
						t2 = new Bounds1(0f, 1f);
						float4.x = math.max(0f, float4.x - MathUtils.Length(leftStartCurve));
						MathUtils.ClampLength(leftEndCurve, ref t2, float4.x);
						@float.x = math.max(@float.x, 1f + t2.max);
					}
				}
				if (float4.y > 0f)
				{
					Bounds1 t3 = new Bounds1(0f, 1f);
					if (MathUtils.ClampLength(rightStartCurve, ref t3, float4.y))
					{
						@float.y = math.max(@float.y, t3.max);
					}
					else
					{
						t3 = new Bounds1(0f, 1f);
						float4.y = math.max(0f, float4.y - MathUtils.Length(rightStartCurve));
						MathUtils.ClampLength(rightEndCurve, ref t3, float4.y);
						@float.y = math.max(@float.y, 1f + t3.max);
					}
				}
				if ((prefabGeometryData.m_Flags & GeometryFlags.StraightEnds) != 0)
				{
					@float = math.cmax(@float);
				}
				else if (@float.y > @float.x)
				{
					CheckOppositeSide(leftStartCurve.xz, leftEndCurve.xz, rightStartCurve.xz, rightEndCurve.xz, float2, float3, ref @float.x, @float.y);
				}
				else if (@float.y < @float.x)
				{
					CheckOppositeSide(rightStartCurve.xz, rightEndCurve.xz, leftStartCurve.xz, leftEndCurve.xz, float3, float2, ref @float.y, @float.x);
				}
				leftTarget = default(float3);
				rightTarget = default(float3);
			}
			if (m_OutsideConnectionData.HasComponent(node))
			{
				float2 float20 = default(float2);
				float20.x = IntersectBounds(leftStartCurve, leftEndCurve);
				float20.y = IntersectBounds(rightStartCurve, rightEndCurve);
				if ((prefabGeometryData.m_Flags & GeometryFlags.StraightEnds) != 0)
				{
					float20 = math.cmin(float20);
				}
				@float = math.max(@float, float20);
				leftTarget = default(float3);
				rightTarget = default(float3);
			}
			return @float;
		}

		private Entity GetTopOwner(Entity entity)
		{
			Entity result = Entity.Null;
			Owner componentData;
			while (m_OwnerData.TryGetComponent(entity, out componentData))
			{
				entity = componentData.m_Owner;
				result = entity;
				if (m_TempData.TryGetComponent(entity, out var componentData2) && componentData2.m_Original != Entity.Null)
				{
					entity = componentData2.m_Original;
					result = entity;
				}
			}
			return result;
		}

		private float IntersectBounds(Bezier4x3 startCurve, Bezier4x3 endCurve)
		{
			float num = 0f;
			if (MathUtils.Intersect(startCurve.x, m_TerrainBounds.min.x, out var t, 4))
			{
				num = math.max(num, t);
			}
			if (MathUtils.Intersect(startCurve.x, m_TerrainBounds.max.x, out t, 4))
			{
				num = math.max(num, t);
			}
			if (MathUtils.Intersect(startCurve.z, m_TerrainBounds.min.z, out t, 4))
			{
				num = math.max(num, t);
			}
			if (MathUtils.Intersect(startCurve.z, m_TerrainBounds.max.z, out t, 4))
			{
				num = math.max(num, t);
			}
			if (MathUtils.Intersect(endCurve.x, m_TerrainBounds.min.x, out t, 4))
			{
				num = math.max(num, 1f + t);
			}
			if (MathUtils.Intersect(endCurve.x, m_TerrainBounds.max.x, out t, 4))
			{
				num = math.max(num, 1f + t);
			}
			if (MathUtils.Intersect(endCurve.z, m_TerrainBounds.min.z, out t, 4))
			{
				num = math.max(num, 1f + t);
			}
			if (MathUtils.Intersect(endCurve.z, m_TerrainBounds.max.z, out t, 4))
			{
				num = math.max(num, 1f + t);
			}
			return num;
		}

		private void CheckOppositeSide(Bezier4x2 startCurve, Bezier4x2 endCurve, Bezier4x2 oppositeStartCurve, Bezier4x2 oppositeEndCurve, float2 intersectTangent, float2 oppositeIntersectTangent, ref float t, float oppositeT)
		{
			float2 @float = Position(startCurve, endCurve, t);
			float2 value = Tangent(startCurve, endCurve, t);
			float2 float2 = Position(oppositeStartCurve, oppositeEndCurve, t);
			float2 value2 = @float - float2;
			float2 float3 = Position(oppositeStartCurve, oppositeEndCurve, oppositeT);
			float2 value3 = Tangent(oppositeStartCurve, oppositeEndCurve, oppositeT);
			float2 value4 = float3 - @float;
			MathUtils.TryNormalize(ref intersectTangent);
			MathUtils.TryNormalize(ref oppositeIntersectTangent);
			MathUtils.TryNormalize(ref value);
			MathUtils.TryNormalize(ref value2);
			MathUtils.TryNormalize(ref value3);
			MathUtils.TryNormalize(ref value4);
			float num = math.dot(intersectTangent, value);
			float num2 = math.dot(intersectTangent, value2);
			math.dot(oppositeIntersectTangent, value);
			math.dot(oppositeIntersectTangent, value3);
			Line2.Segment line = new Line2.Segment(float3, float3 - oppositeIntersectTangent * (math.distance(@float, float2) * 3f));
			float num3 = t;
			if (MathUtils.Intersect(startCurve, line, out var t2, 4))
			{
				num3 = math.max(num3, t2.x);
			}
			if (MathUtils.Intersect(endCurve, line, out t2, 4))
			{
				num3 = math.max(num3, t2.x + 1f);
			}
			if (num > 0f && num2 > 0f)
			{
				num2 = 1f;
			}
			float x = 0f;
			float num4 = math.acos(math.saturate(math.dot(value3, value4)));
			float num5 = math.acos(math.saturate(math.dot(value, value4)));
			float num6 = MathF.PI / 2f;
			if (num5 > 0.0001f)
			{
				num6 = math.min(num6, math.sin(num4) / math.tan(num5) * num4);
			}
			if (num6 > num5 && num4 > num5)
			{
				x = math.lerp(t, oppositeT, (num6 - num5) / (MathF.PI / 2f - num5));
			}
			t = math.lerp(num3, t, math.saturate(num2));
			t = math.lerp(t, oppositeT, math.saturate(math.dot(intersectTangent, oppositeIntersectTangent)));
			t = math.min(oppositeT, math.max(x, t));
		}

		private float2 Position(Bezier4x2 startCurve, Bezier4x2 endCurve, float t)
		{
			if (t < 1f)
			{
				return MathUtils.Position(startCurve, math.max(0f, t));
			}
			return MathUtils.Position(endCurve, math.min(1f, t - 1f));
		}

		private float2 Tangent(Bezier4x2 startCurve, Bezier4x2 endCurve, float t)
		{
			if (t < 1f)
			{
				return MathUtils.Tangent(startCurve, math.max(0f, t));
			}
			return MathUtils.Tangent(endCurve, math.min(1f, t - 1f));
		}

		private float2 Intersect(Bezier4x2 startCurve1, Bezier4x2 endCurve1, Bezier4x2 nodeCurve2, Bezier4x2 startCurve2, Bezier4x2 endCurve2)
		{
			float2 t = default(float2);
			Intersect(startCurve1, nodeCurve2, new float2(0f, -1f), ref t);
			Intersect(startCurve1, startCurve2, new float2(0f, 0f), ref t);
			Intersect(startCurve1, endCurve2, new float2(0f, 1f), ref t);
			Intersect(endCurve1, nodeCurve2, new float2(1f, -1f), ref t);
			Intersect(endCurve1, startCurve2, new float2(1f, 0f), ref t);
			Intersect(endCurve1, endCurve2, new float2(1f, 1f), ref t);
			return t;
		}

		private void Intersect(Bezier4x2 curve1, Bezier4x2 curve2, float2 offset, ref float2 t)
		{
			if (MathUtils.Intersect(curve1, curve2, out var t2, 4))
			{
				t2 += offset;
				if (t2.x > t.x)
				{
					t = t2;
				}
			}
		}

		private float CheckCrosswalks(DynamicBuffer<NetCompositionCrosswalk> prefabCompositionCrosswalks)
		{
			float num = 0f;
			for (int i = 0; i < prefabCompositionCrosswalks.Length; i++)
			{
				NetCompositionCrosswalk netCompositionCrosswalk = prefabCompositionCrosswalks[i];
				num = math.max(num, math.max(netCompositionCrosswalk.m_Start.z, netCompositionCrosswalk.m_End.z));
			}
			return num;
		}

		private float CompareLanes(DynamicBuffer<NetCompositionLane> prefabCompositionLanes1, DynamicBuffer<NetCompositionLane> prefabCompositionLanes2, float offset, bool isEnd1, bool isEnd2, bool dontCrossTracks)
		{
			GetLaneLimits(prefabCompositionLanes1, isEnd1, !isEnd1, out var minRoadLimits, out var maxRoadLimits, out var trackLimits, out var masterLanes);
			GetLaneLimits(prefabCompositionLanes2, isEnd2, isEnd2, out var minRoadLimits2, out var maxRoadLimits2, out var trackLimits2, out var masterLanes2);
			float4 x = math.abs(minRoadLimits - minRoadLimits2 + offset);
			float4 y = math.abs(minRoadLimits - maxRoadLimits2 + offset);
			float4 x2 = math.abs(maxRoadLimits - minRoadLimits2 + offset);
			float4 y2 = math.abs(maxRoadLimits - maxRoadLimits2 + offset);
			x = math.max(math.max(x, y), math.max(x2, y2));
			float4 falseValue = math.abs(trackLimits - trackLimits2 + offset);
			float4 falseValue2 = math.abs(trackLimits - trackLimits2.yxwz + offset);
			x = math.select(x, 0f, (math.abs(minRoadLimits) > 100000f) | (math.abs(minRoadLimits2) > 100000f));
			falseValue = math.select(falseValue, 0f, (math.abs(trackLimits) > 100000f) | (math.abs(trackLimits2) > 100000f));
			falseValue2 = math.select(falseValue2, 0f, (math.abs(trackLimits) > 100000f) | (math.abs(trackLimits2.yxwz) > 100000f));
			falseValue2 = math.select(falseValue2, falseValue, dontCrossTracks);
			float2 x3 = new float2(math.cmax(x), math.cmax(math.max(falseValue, falseValue2)));
			float falseValue3 = math.cmax(math.max(masterLanes, masterLanes2)) * 0.5f;
			falseValue3 = math.select(falseValue3, 0f, math.all(masterLanes == 0f == (masterLanes2 == 0f)));
			x3.x = math.max(x3.x, falseValue3);
			return math.cmax(math.sqrt(x3) * new float2(3f, 4f));
		}

		private void GetLaneLimits(DynamicBuffer<NetCompositionLane> prefabCompositionLanes, bool isEnd, bool invert, out float4 minRoadLimits, out float4 maxRoadLimits, out float4 trackLimits, out float2 masterLanes)
		{
			float4 @float = new float4(1000000f, -1000000f, 1000000f, -1000000f);
			minRoadLimits = new float4(1000000f, 1000000f, 1000000f, 1000000f);
			maxRoadLimits = new float4(-1000000f, -1000000f, -1000000f, -1000000f);
			trackLimits = @float;
			masterLanes = 0f;
			float4 float2 = @float;
			int num = -1;
			for (int i = 0; i < prefabCompositionLanes.Length; i++)
			{
				NetCompositionLane netCompositionLane = prefabCompositionLanes[i];
				LaneFlags laneFlags = (((netCompositionLane.m_Flags & LaneFlags.Invert) != 0 != isEnd) ? LaneFlags.DisconnectedEnd : LaneFlags.DisconnectedStart);
				if ((netCompositionLane.m_Flags & laneFlags) != 0)
				{
					continue;
				}
				netCompositionLane.m_Position.x = math.select(netCompositionLane.m_Position.x, 0f - netCompositionLane.m_Position.x, invert);
				if ((netCompositionLane.m_Flags & LaneFlags.Road) != 0)
				{
					if ((netCompositionLane.m_Flags & LaneFlags.Master) != 0)
					{
						if (m_NetLaneData.TryGetComponent(netCompositionLane.m_Lane, out var componentData))
						{
							if ((netCompositionLane.m_Flags & LaneFlags.Twoway) != 0)
							{
								masterLanes = math.max(masterLanes, componentData.m_Width);
							}
							else if ((netCompositionLane.m_Flags & LaneFlags.Invert) != 0 == invert)
							{
								masterLanes.x = math.max(masterLanes.x, componentData.m_Width);
							}
							else
							{
								masterLanes.y = math.max(masterLanes.y, componentData.m_Width);
							}
						}
						continue;
					}
					if (netCompositionLane.m_Carriageway != num)
					{
						minRoadLimits = math.select(minRoadLimits, float2, (float2 < minRoadLimits) & (float2 != @float));
						maxRoadLimits = math.select(maxRoadLimits, float2, (float2 > maxRoadLimits) & (float2 != @float));
						float2 = @float;
						num = netCompositionLane.m_Carriageway;
					}
					if ((netCompositionLane.m_Flags & LaneFlags.Twoway) != 0)
					{
						float2.xz = math.min(float2.xz, netCompositionLane.m_Position.x);
						float2.yw = math.max(float2.yw, netCompositionLane.m_Position.x);
					}
					else if ((netCompositionLane.m_Flags & LaneFlags.Invert) != 0 == invert)
					{
						float2.x = math.min(float2.x, netCompositionLane.m_Position.x);
						float2.y = math.max(float2.y, netCompositionLane.m_Position.x);
					}
					else
					{
						float2.z = math.min(float2.z, netCompositionLane.m_Position.x);
						float2.w = math.max(float2.w, netCompositionLane.m_Position.x);
					}
				}
				else if ((netCompositionLane.m_Flags & LaneFlags.Track) != 0)
				{
					if ((netCompositionLane.m_Flags & LaneFlags.Twoway) != 0)
					{
						trackLimits.xz = math.min(trackLimits.xz, netCompositionLane.m_Position.x);
						trackLimits.yw = math.max(trackLimits.yw, netCompositionLane.m_Position.x);
					}
					else if ((netCompositionLane.m_Flags & LaneFlags.Invert) != 0 == invert)
					{
						trackLimits.x = math.min(trackLimits.x, netCompositionLane.m_Position.x);
						trackLimits.y = math.max(trackLimits.y, netCompositionLane.m_Position.x);
					}
					else
					{
						trackLimits.z = math.min(trackLimits.z, netCompositionLane.m_Position.x);
						trackLimits.w = math.max(trackLimits.w, netCompositionLane.m_Position.x);
					}
				}
			}
			minRoadLimits = math.select(minRoadLimits, float2, (float2 < minRoadLimits) & (float2 != @float));
			maxRoadLimits = math.select(maxRoadLimits, float2, (float2 > maxRoadLimits) & (float2 != @float));
		}

		private bool RequireTransition(NetCompositionData nodeCompositionData, NetCompositionData nodeCompositionData2, NetCompositionData edgeCompositionData, NetCompositionData edgeCompositionData2)
		{
			if (math.abs(nodeCompositionData.m_HeightRange.min - nodeCompositionData2.m_HeightRange.min) > 0.1f || math.abs(nodeCompositionData.m_HeightRange.max - nodeCompositionData2.m_HeightRange.max) > 0.1f)
			{
				return true;
			}
			CompositionFlags compositionFlags = new CompositionFlags(CompositionFlags.General.StyleBreak, CompositionFlags.Side.LowTransition | CompositionFlags.Side.HighTransition, CompositionFlags.Side.LowTransition | CompositionFlags.Side.HighTransition);
			if (((nodeCompositionData.m_Flags | nodeCompositionData2.m_Flags) & compositionFlags) != default(CompositionFlags))
			{
				return true;
			}
			CompositionFlags compositionFlags2 = new CompositionFlags(CompositionFlags.General.Pavement | CompositionFlags.General.Gravel | CompositionFlags.General.Tiles, (CompositionFlags.Side)0u, (CompositionFlags.Side)0u);
			if ((edgeCompositionData.m_Flags & compositionFlags2) != (edgeCompositionData2.m_Flags & compositionFlags2))
			{
				return true;
			}
			return false;
		}
	}

	private struct EdgeData
	{
		public float3 m_Left;

		public float3 m_Right;

		public Layer m_Layers;

		public Entity m_Entity;

		public float2 m_Changes;

		public float m_MaxSlope;

		public bool m_IsEnd;

		public bool m_IsTemp;
	}

	[BurstCompile]
	private struct AllocateBuffersJob : IJob
	{
		[ReadOnly]
		public NativeList<Entity> m_Entities;

		public NativeList<IntersectionData> m_IntersectionData;

		public NativeParallelHashMap<int2, float4> m_EdgeHeightMap;

		public void Execute()
		{
			m_IntersectionData.ResizeUninitialized(m_Entities.Length);
			m_EdgeHeightMap.Capacity = m_Entities.Length * 2;
		}
	}

	[BurstCompile]
	private struct FlattenNodeGeometryJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<NodeGeometry> m_NodeGeometryType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeDataFromEntity;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefDataFromEntity;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_Edges;

		public NativeParallelHashMap<int2, float4>.ParallelWriter m_EdgeHeightMap;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<NodeGeometry> nativeArray2 = chunk.GetNativeArray(ref m_NodeGeometryType);
			NativeList<EdgeData> nativeList = new NativeList<EdgeData>(10, Allocator.Temp);
			bool flag = chunk.Has(ref m_TempType);
			float2 float7 = default(float2);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Entity node = nativeArray[i];
				NodeGeometry nodeGeometry = nativeArray2[i];
				if (nodeGeometry.m_Bounds.min.x != 0f)
				{
					continue;
				}
				bool flag2 = true;
				EdgeIterator edgeIterator = new EdgeIterator(Entity.Null, node, m_Edges, m_EdgeDataFromEntity, m_TempData, m_HiddenData);
				EdgeIteratorValue value;
				while (edgeIterator.GetNext(out value))
				{
					PrefabRef prefabRef = m_PrefabRefDataFromEntity[value.m_Edge];
					NetGeometryData netGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
					if (netGeometryData.m_MergeLayers != Layer.None)
					{
						EdgeGeometry edgeGeometry = m_EdgeGeometryData[value.m_Edge];
						bool flag3 = m_TempData.HasComponent(value.m_Edge);
						flag2 = flag2 && flag3;
						EdgeData value2 = new EdgeData
						{
							m_Left = (value.m_End ? edgeGeometry.m_End.m_Left.d : edgeGeometry.m_Start.m_Right.a),
							m_Right = (value.m_End ? edgeGeometry.m_End.m_Right.d : edgeGeometry.m_Start.m_Left.a),
							m_Layers = netGeometryData.m_MergeLayers,
							m_Entity = value.m_Edge,
							m_MaxSlope = netGeometryData.m_MaxSlopeSteepness,
							m_IsEnd = value.m_End,
							m_IsTemp = flag3
						};
						nativeList.Add(in value2);
					}
				}
				if (flag && !flag2)
				{
					for (int j = 0; j < nativeList.Length; j++)
					{
						EdgeData edgeData = nativeList[j];
						if (!edgeData.m_IsTemp)
						{
							continue;
						}
						Temp temp = m_TempData[edgeData.m_Entity];
						if (m_EdgeGeometryData.TryGetComponent(temp.m_Original, out var componentData))
						{
							EdgeGeometry edgeGeometry2 = m_EdgeGeometryData[edgeData.m_Entity];
							int2 key = new int2(edgeData.m_Entity.Index, math.select(0, 1, edgeData.m_IsEnd));
							float4 item;
							if (edgeData.m_IsEnd)
							{
								item = new float4(componentData.m_End.m_Left.d.yy, componentData.m_End.m_Right.d.yy);
								item.x += edgeGeometry2.m_End.m_Left.c.y - edgeGeometry2.m_End.m_Left.d.y;
								item.z += edgeGeometry2.m_End.m_Right.c.y - edgeGeometry2.m_End.m_Right.d.y;
							}
							else
							{
								item = new float4(componentData.m_Start.m_Right.a.yy, componentData.m_Start.m_Left.a.yy);
								item.x += edgeGeometry2.m_Start.m_Right.b.y - edgeGeometry2.m_Start.m_Right.a.y;
								item.z += edgeGeometry2.m_Start.m_Left.b.y - edgeGeometry2.m_Start.m_Left.a.y;
							}
							m_EdgeHeightMap.TryAdd(key, item);
						}
					}
				}
				else
				{
					bool flag4 = false;
					for (int k = 0; k < 100; k++)
					{
						bool flag5 = false;
						for (int l = 1; l < nativeList.Length; l++)
						{
							ref EdgeData reference = ref nativeList.ElementAt(l);
							for (int m = 0; m < l; m++)
							{
								ref EdgeData reference2 = ref nativeList.ElementAt(m);
								if ((reference.m_Layers & reference2.m_Layers) == 0)
								{
									continue;
								}
								float3 @float = reference2.m_Right - reference.m_Left;
								float3 float2 = reference2.m_Left - reference.m_Right;
								float2 float3 = new float2(math.lengthsq(@float.xz), math.lengthsq(float2.xz));
								float2 float4 = new float2(@float.y, float2.y);
								float num = reference.m_MaxSlope + reference2.m_MaxSlope;
								if (math.any(float4 * float4 > float3 * (num * num * 1.0001f)))
								{
									float3 = math.sqrt(float3);
									float2 float5 = math.abs(float4);
									float2 float6 = math.max(0f, float5 - float3 * num);
									bool2 test = float4 >= 0f;
									float num2;
									if (test.x != test.y)
									{
										float6 = math.select(-float6, float6, test);
										num2 = math.csum(float6) * 0.5f;
									}
									else
									{
										num2 = math.max(float6.x, float6.y);
										num2 = math.select(0f - num2, num2, test.x);
									}
									if (num2 >= 0f)
									{
										float7.x = math.max(reference.m_Left.y, reference.m_Right.y);
										float7.y = math.min(reference2.m_Left.y, reference2.m_Right.y);
										float7 = nodeGeometry.m_Position - float7;
										float7.x = math.max(0f, float7.x);
										float7.y = math.min(0f, float7.y);
									}
									else
									{
										float7.x = math.min(reference.m_Left.y, reference.m_Right.y);
										float7.y = math.max(reference2.m_Left.y, reference2.m_Right.y);
										float7 = nodeGeometry.m_Position - float7;
										float7.x = math.min(0f, float7.x);
										float7.y = math.max(0f, float7.y);
									}
									float7 = math.select(float7, 0f, flag != new bool2(reference.m_IsTemp, reference2.m_IsTemp));
									float6 = float7 * math.min(1f, math.abs(num2) / math.max(0.001f, math.csum(math.abs(float7))));
									reference.m_Changes.x = math.min(reference.m_Changes.x, float6.x);
									reference.m_Changes.y = math.max(reference.m_Changes.y, float6.x);
									reference2.m_Changes.x = math.min(reference2.m_Changes.x, float6.y);
									reference2.m_Changes.y = math.max(reference2.m_Changes.y, float6.y);
									flag5 = true;
								}
							}
						}
						if (!flag5)
						{
							break;
						}
						for (int n = 0; n < nativeList.Length; n++)
						{
							ref EdgeData reference3 = ref nativeList.ElementAt(n);
							float num3 = math.csum(reference3.m_Changes);
							reference3.m_Left.y += num3;
							reference3.m_Right.y += num3;
							reference3.m_Changes = 0f;
						}
						flag4 = true;
					}
					if (flag4)
					{
						for (int num4 = 0; num4 < nativeList.Length; num4++)
						{
							EdgeData edgeData2 = nativeList[num4];
							if (flag == edgeData2.m_IsTemp)
							{
								EdgeGeometry edgeGeometry3 = m_EdgeGeometryData[edgeData2.m_Entity];
								int2 key2 = new int2(edgeData2.m_Entity.Index, math.select(0, 1, edgeData2.m_IsEnd));
								float4 item2 = new float4(edgeData2.m_Left.yy, edgeData2.m_Right.yy);
								if (edgeData2.m_IsEnd)
								{
									item2.x += edgeGeometry3.m_End.m_Left.c.y - edgeGeometry3.m_End.m_Left.d.y;
									item2.z += edgeGeometry3.m_End.m_Right.c.y - edgeGeometry3.m_End.m_Right.d.y;
								}
								else
								{
									item2.x += edgeGeometry3.m_Start.m_Right.b.y - edgeGeometry3.m_Start.m_Right.a.y;
									item2.z += edgeGeometry3.m_Start.m_Left.b.y - edgeGeometry3.m_Start.m_Left.a.y;
								}
								m_EdgeHeightMap.TryAdd(key2, item2);
							}
						}
					}
				}
				nativeList.Clear();
			}
			nativeList.Dispose();
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FinishEdgeGeometryJob : IJobParallelForDefer
	{
		[ReadOnly]
		public NativeArray<Entity> m_Entities;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionDataFromEntity;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public NativeParallelHashMap<int2, float4> m_EdgeHeightMap;

		public void Execute(int index)
		{
			Entity entity = m_Entities[index];
			Composition composition = m_CompositionDataFromEntity[entity];
			PrefabRef prefabRef = m_PrefabRefDataFromEntity[entity];
			NetGeometryData netGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
			NetCompositionData netCompositionData = m_PrefabCompositionData[composition.m_Edge];
			EdgeGeometry value = m_EdgeGeometryData[entity];
			if (m_EdgeHeightMap.TryGetValue(new int2(entity.Index, 0), out var item))
			{
				value.m_Start.m_Right.b.y = item.x;
				value.m_Start.m_Right.a.y = item.y;
				value.m_Start.m_Left.b.y = item.z;
				value.m_Start.m_Left.a.y = item.w;
			}
			if (m_EdgeHeightMap.TryGetValue(new int2(entity.Index, 1), out var item2))
			{
				value.m_End.m_Left.c.y = item2.x;
				value.m_End.m_Left.d.y = item2.y;
				value.m_End.m_Right.c.y = item2.z;
				value.m_End.m_Right.d.y = item2.w;
			}
			if ((netGeometryData.m_Flags & GeometryFlags.SmoothSlopes) == 0)
			{
				if (!(((netCompositionData.m_Flags.m_General & (CompositionFlags.General.Elevated | CompositionFlags.General.Tunnel)) == 0) & ((netCompositionData.m_Flags.m_Left & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered)) == 0 || (netCompositionData.m_Flags.m_Right & (CompositionFlags.Side.Raised | CompositionFlags.Side.Lowered)) == 0) & !m_OwnerData.HasComponent(entity)))
				{
					StraightenMiddleHeights(ref value.m_Start.m_Left, ref value.m_End.m_Left);
					StraightenMiddleHeights(ref value.m_Start.m_Right, ref value.m_End.m_Right);
				}
				else
				{
					LimitMiddleHeights(ref value.m_Start.m_Left, ref value.m_End.m_Left, netGeometryData.m_MaxSlopeSteepness, netCompositionData.m_Width);
					LimitMiddleHeights(ref value.m_Start.m_Right, ref value.m_End.m_Right, netGeometryData.m_MaxSlopeSteepness, netCompositionData.m_Width);
				}
			}
			else
			{
				LimitMiddleHeights(ref value.m_Start.m_Left, ref value.m_End.m_Left, netGeometryData.m_MaxSlopeSteepness, netCompositionData.m_Width);
				LimitMiddleHeights(ref value.m_Start.m_Right, ref value.m_End.m_Right, netGeometryData.m_MaxSlopeSteepness, netCompositionData.m_Width);
			}
			value.m_Start.m_Length.x = MathUtils.Length(value.m_Start.m_Left);
			value.m_Start.m_Length.y = MathUtils.Length(value.m_Start.m_Right);
			value.m_End.m_Length.x = MathUtils.Length(value.m_End.m_Left);
			value.m_End.m_Length.y = MathUtils.Length(value.m_End.m_Right);
			value.m_Bounds = MathUtils.TightBounds(value.m_Start.m_Left) | MathUtils.TightBounds(value.m_Start.m_Right) | MathUtils.TightBounds(value.m_End.m_Left) | MathUtils.TightBounds(value.m_End.m_Right);
			value.m_Bounds.min.y += netCompositionData.m_HeightRange.min;
			value.m_Bounds.max.y += netCompositionData.m_HeightRange.max;
			if ((netCompositionData.m_State & (CompositionState.LowerToTerrain | CompositionState.RaiseToTerrain)) != 0)
			{
				Bounds1 bounds = SampleTerrain(value.m_Start.m_Left) | SampleTerrain(value.m_Start.m_Right) | SampleTerrain(value.m_End.m_Left) | SampleTerrain(value.m_End.m_Right);
				if ((netCompositionData.m_State & CompositionState.LowerToTerrain) != 0)
				{
					value.m_Bounds.min.y = math.min(value.m_Bounds.min.y, bounds.min);
				}
				if ((netCompositionData.m_State & CompositionState.RaiseToTerrain) != 0)
				{
					value.m_Bounds.max.y = math.max(value.m_Bounds.max.y, bounds.max);
				}
			}
			m_EdgeGeometryData[entity] = value;
		}

		private Bounds1 SampleTerrain(Bezier4x3 curve)
		{
			Bounds1 result = new Bounds1(float.MaxValue, float.MinValue);
			for (int i = 0; i <= 8; i++)
			{
				result |= TerrainUtils.SampleHeight(ref m_TerrainHeightData, MathUtils.Position(curve, (float)i * 0.125f));
			}
			return result;
		}

		private void StraightenMiddleHeights(ref Bezier4x3 start, ref Bezier4x3 end)
		{
			float4 @float = default(float4);
			@float.x = math.distance(start.b.xz, start.c.xz);
			@float.y = @float.x + math.distance(start.c.xz, start.d.xz);
			@float.z = @float.y + math.distance(end.a.xz, end.b.xz);
			@float.w = @float.z + math.distance(end.b.xz, end.c.xz);
			@float = math.select(@float / @float.w, 0f, @float.w == 0f);
			float3 float2 = math.lerp(start.b.y, end.c.y, @float.xyz);
			start.c.y = float2.x;
			start.d.y = float2.y;
			end.a.y = float2.y;
			end.b.y = float2.z;
		}

		private void LimitMiddleHeights(ref Bezier4x3 start, ref Bezier4x3 end, float maxSlope, float width)
		{
			float num = MathUtils.Length(start.xz);
			float num2 = MathUtils.Length(end.xz);
			float num3 = num * maxSlope;
			float num4 = num2 * maxSlope;
			Bounds1 bounds = new Bounds1(math.max(start.a.y - num3, end.d.y - num4), math.min(start.a.y + num3, end.d.y + num4));
			if (bounds.max < bounds.min)
			{
				bounds = new Bounds1((bounds.min + bounds.max) * 0.5f);
			}
			else
			{
				float end2 = (bounds.min + bounds.max) * 0.5f;
				float t = 1f / (0.5f * (num + num2) / math.max(0.01f, width) + 1f);
				bounds.min = math.lerp(bounds.min, end2, t);
				bounds.max = math.lerp(bounds.max, end2, t);
			}
			float num5 = MathUtils.Clamp(start.d.y, bounds) - start.d.y;
			start.c.y += num5;
			start.d.y += num5;
			end.a.y += num5;
			end.b.y += num5;
		}
	}

	[BurstCompile]
	private struct CalculateNodeGeometryJob : IJobParallelForDefer
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Node> m_NodeDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionDataFromEntity;

		[ReadOnly]
		public ComponentLookup<OutsideConnection> m_OutsideConnectionData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_GeometryDataFromEntity;

		[ReadOnly]
		public ComponentLookup<NodeGeometry> m_NodeGeometryData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PlaceableObjectData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_Edges;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<NetCompositionPiece> m_PrefabCompositionPieces;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

		[ReadOnly]
		public int m_IterationIndex;

		[ReadOnly]
		public NativeArray<Entity> m_Entities;

		public void Execute(int index)
		{
			Entity entity = m_Entities[index];
			StartNodeGeometry value = m_StartNodeGeometryData[entity];
			EndNodeGeometry value2 = m_EndNodeGeometryData[entity];
			if (m_IterationIndex == 1 && value.m_Geometry.m_Left.m_Length.x >= 0f && value.m_Geometry.m_Right.m_Length.x >= 0f && value2.m_Geometry.m_Left.m_Length.x >= 0f && value2.m_Geometry.m_Right.m_Length.x >= 0f)
			{
				return;
			}
			Edge edge = m_EdgeDataFromEntity[entity];
			Composition composition = m_CompositionDataFromEntity[entity];
			EdgeGeometry edgeGeometry = m_GeometryDataFromEntity[entity];
			PrefabRef prefabRef = m_PrefabRefDataFromEntity[entity];
			NetGeometryData netGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
			NetCompositionData edgeCompositionData = m_PrefabCompositionData[composition.m_Edge];
			NetCompositionData netCompositionData = m_PrefabCompositionData[composition.m_StartNode];
			NetCompositionData netCompositionData2 = m_PrefabCompositionData[composition.m_EndNode];
			NodeGeometry nodeGeometry = m_NodeGeometryData[edge.m_Start];
			NodeGeometry nodeGeometry2 = m_NodeGeometryData[edge.m_End];
			float3 @float = FindMiddleNodePos(entity, edge.m_Start);
			@float.y = nodeGeometry.m_Position;
			float3 float2 = FindMiddleNodePos(entity, edge.m_End);
			float2.y = nodeGeometry2.m_Position;
			float2 offset = StartOffset(edgeGeometry.m_Start, @float.xz);
			float2 offset2 = EndOffset(edgeGeometry.m_End, float2.xz);
			FindTargetSegments(entity, edge.m_Start, offset, @float.xz, netGeometryData, netCompositionData, out var leftSegment, out var rightSegment, out var distances, out var syncVertexOffsetsLeft, out var syncVertexOffsetsRight, out var roundaboutSize, out var sideConnect, out var middleConnect);
			FindTargetSegments(entity, edge.m_End, offset2, float2.xz, netGeometryData, netCompositionData2, out var leftSegment2, out var rightSegment2, out var distances2, out var syncVertexOffsetsLeft2, out var syncVertexOffsetsRight2, out var roundaboutSize2, out var sideConnect2, out var middleConnect2);
			Segment segment = Invert(edgeGeometry.m_Start);
			Segment segment2 = edgeGeometry.m_End;
			AdjustSegmentWidth(ref segment, edgeCompositionData, netCompositionData, invertEdge: true);
			AdjustSegmentWidth(ref segment2, edgeCompositionData, netCompositionData2, invertEdge: false);
			Segment segment3 = segment;
			Segment segment4 = segment2;
			float t = netCompositionData.m_MiddleOffset / math.max(0.01f, netCompositionData.m_Width) + 0.5f;
			float t2 = netCompositionData2.m_MiddleOffset / math.max(0.01f, netCompositionData2.m_Width) + 0.5f;
			segment.m_Right = MathUtils.Lerp(segment.m_Left, segment.m_Right, t);
			segment3.m_Left = MathUtils.Lerp(segment3.m_Left, segment3.m_Right, t);
			segment2.m_Right = MathUtils.Lerp(segment2.m_Left, segment2.m_Right, t2);
			segment4.m_Left = MathUtils.Lerp(segment4.m_Left, segment4.m_Right, t2);
			float leftRadius = 0f;
			float rightRadius = 0f;
			float leftRadius2 = 0f;
			float rightRadius2 = 0f;
			value.m_Geometry.m_Middle = default(Bezier4x3);
			value2.m_Geometry.m_Middle = default(Bezier4x3);
			float2 divPos;
			float2 divPos2;
			if (value.m_Geometry.m_MiddleRadius > 0f)
			{
				float3 position = m_NodeDataFromEntity[edge.m_Start].m_Position;
				position.y = nodeGeometry.m_Position;
				leftRadius = value.m_Geometry.m_MiddleRadius + roundaboutSize;
				rightRadius = leftRadius;
				CalculateSegments(segment, segment3, leftSegment, rightSegment, position, ref leftRadius, ref rightRadius, out value.m_Geometry.m_Left, out value.m_Geometry.m_Right, out divPos, out divPos2);
			}
			else if ((netCompositionData.m_Flags.m_General & CompositionFlags.General.StraightNodeEnd) != 0)
			{
				float straightNodeLength = GetStraightNodeLength(edge.m_Start, netGeometryData);
				value.m_Geometry.m_Left = CalculateStraightSegment(segment, straightNodeLength, out divPos);
				value.m_Geometry.m_Right = CalculateStraightSegment(segment3, straightNodeLength, out divPos2);
				value.m_Geometry.m_Middle.d.xy = 1f;
			}
			else
			{
				if (sideConnect.x)
				{
					value.m_Geometry.m_Left = CalculateSideConnection(segment, leftSegment, rightSegment, edgeCompositionData, netCompositionData, right: false, out divPos);
					value.m_Geometry.m_Middle.d.x = 1f;
				}
				else if (middleConnect.x)
				{
					if (m_IterationIndex == 0)
					{
						value.m_Geometry.m_Left.m_Length = -1f;
						divPos = -1f;
					}
					else
					{
						value.m_Geometry.m_Left = CalculateMiddleConnection(segment, leftSegment, out divPos);
						value.m_Geometry.m_Middle.d.x = 1f;
					}
				}
				else
				{
					value.m_Geometry.m_Left = CalculateMiddleSegment(segment, leftSegment, out divPos);
				}
				if (sideConnect.y)
				{
					value.m_Geometry.m_Right = CalculateSideConnection(segment3, leftSegment, rightSegment, edgeCompositionData, netCompositionData, right: true, out divPos2);
					value.m_Geometry.m_Middle.d.y = 1f;
				}
				else if (middleConnect.y)
				{
					if (m_IterationIndex == 0)
					{
						value.m_Geometry.m_Right.m_Length = -1f;
						divPos2 = -1f;
					}
					else
					{
						value.m_Geometry.m_Right = CalculateMiddleConnection(segment3, rightSegment, out divPos2);
						value.m_Geometry.m_Middle.d.y = 1f;
					}
				}
				else
				{
					value.m_Geometry.m_Right = CalculateMiddleSegment(segment3, rightSegment, out divPos2);
				}
			}
			float2 divPos3;
			float2 divPos4;
			if (value2.m_Geometry.m_MiddleRadius > 0f)
			{
				float3 position2 = m_NodeDataFromEntity[edge.m_End].m_Position;
				position2.y = nodeGeometry2.m_Position;
				leftRadius2 = value2.m_Geometry.m_MiddleRadius + roundaboutSize2;
				rightRadius2 = leftRadius2;
				CalculateSegments(segment2, segment4, leftSegment2, rightSegment2, position2, ref leftRadius2, ref rightRadius2, out value2.m_Geometry.m_Left, out value2.m_Geometry.m_Right, out divPos3, out divPos4);
			}
			else if ((netCompositionData2.m_Flags.m_General & CompositionFlags.General.StraightNodeEnd) != 0)
			{
				float straightNodeLength2 = GetStraightNodeLength(edge.m_End, netGeometryData);
				value2.m_Geometry.m_Left = CalculateStraightSegment(segment2, straightNodeLength2, out divPos3);
				value2.m_Geometry.m_Right = CalculateStraightSegment(segment4, straightNodeLength2, out divPos4);
				value2.m_Geometry.m_Middle.d.xy = 1f;
			}
			else
			{
				if (sideConnect2.x)
				{
					value2.m_Geometry.m_Left = CalculateSideConnection(segment2, leftSegment2, rightSegment2, edgeCompositionData, netCompositionData2, right: false, out divPos3);
					value2.m_Geometry.m_Middle.d.x = 1f;
				}
				else if (middleConnect2.x)
				{
					if (m_IterationIndex == 0)
					{
						value2.m_Geometry.m_Left.m_Length = -1f;
						divPos3 = -1f;
					}
					else
					{
						value2.m_Geometry.m_Left = CalculateMiddleConnection(segment2, leftSegment2, out divPos3);
						value2.m_Geometry.m_Middle.d.x = 1f;
					}
				}
				else
				{
					value2.m_Geometry.m_Left = CalculateMiddleSegment(segment2, leftSegment2, out divPos3);
				}
				if (sideConnect2.y)
				{
					value2.m_Geometry.m_Right = CalculateSideConnection(segment4, leftSegment2, rightSegment2, edgeCompositionData, netCompositionData2, right: true, out divPos4);
					value2.m_Geometry.m_Middle.d.y = 1f;
				}
				else if (middleConnect2.y)
				{
					if (m_IterationIndex == 0)
					{
						value2.m_Geometry.m_Right.m_Length = -1f;
						divPos4 = -1f;
					}
					else
					{
						value2.m_Geometry.m_Right = CalculateMiddleConnection(segment4, rightSegment2, out divPos4);
						value2.m_Geometry.m_Middle.d.y = 1f;
					}
				}
				else
				{
					value2.m_Geometry.m_Right = CalculateMiddleSegment(segment4, rightSegment2, out divPos4);
				}
			}
			float2 float3 = netCompositionData.m_Width * 0.5f + new float2(netCompositionData.m_MiddleOffset, 0f - netCompositionData.m_MiddleOffset);
			float2 float4 = netCompositionData2.m_Width * 0.5f + new float2(netCompositionData2.m_MiddleOffset, 0f - netCompositionData2.m_MiddleOffset);
			if (value.m_Geometry.m_MiddleRadius > 0f)
			{
				float3 position3 = m_NodeDataFromEntity[edge.m_Start].m_Position;
				position3.y = nodeGeometry.m_Position;
				value.m_Geometry.m_SyncVertexTargetsLeft = CalculateVertexSyncTarget(ref value.m_Geometry.m_Right, position3, leftRadius, isRight: false, float3.x, distances.x, netCompositionData.m_SyncVertexOffsetsLeft, syncVertexOffsetsLeft, divPos.y);
				value.m_Geometry.m_SyncVertexTargetsRight = CalculateVertexSyncTarget(ref value.m_Geometry.m_Right, position3, rightRadius, isRight: true, float3.y, distances.y, netCompositionData.m_SyncVertexOffsetsRight, syncVertexOffsetsRight, divPos2.x);
			}
			else if (!math.any(middleConnect) || m_IterationIndex != 0)
			{
				value.m_Geometry.m_SyncVertexTargetsLeft = CalculateVertexSyncTarget(ref value.m_Geometry.m_Left, float3.x, distances.x, netCompositionData.m_SyncVertexOffsetsLeft, syncVertexOffsetsLeft, divPos.y, isRight: false);
				value.m_Geometry.m_SyncVertexTargetsRight = CalculateVertexSyncTarget(ref value.m_Geometry.m_Right, float3.y, distances.y, netCompositionData.m_SyncVertexOffsetsRight, syncVertexOffsetsRight, divPos2.x, isRight: true);
			}
			if (value2.m_Geometry.m_MiddleRadius > 0f)
			{
				float3 position4 = m_NodeDataFromEntity[edge.m_End].m_Position;
				position4.y = nodeGeometry2.m_Position;
				value2.m_Geometry.m_SyncVertexTargetsLeft = CalculateVertexSyncTarget(ref value2.m_Geometry.m_Right, position4, leftRadius2, isRight: false, float4.x, distances2.x, netCompositionData2.m_SyncVertexOffsetsLeft, syncVertexOffsetsLeft2, divPos3.y);
				value2.m_Geometry.m_SyncVertexTargetsRight = CalculateVertexSyncTarget(ref value2.m_Geometry.m_Right, position4, rightRadius2, isRight: true, float4.y, distances2.y, netCompositionData2.m_SyncVertexOffsetsRight, syncVertexOffsetsRight2, divPos4.x);
			}
			else if (!math.any(middleConnect2) || m_IterationIndex != 0)
			{
				value2.m_Geometry.m_SyncVertexTargetsLeft = CalculateVertexSyncTarget(ref value2.m_Geometry.m_Left, float4.x, distances2.x, netCompositionData2.m_SyncVertexOffsetsLeft, syncVertexOffsetsLeft2, divPos3.y, isRight: false);
				value2.m_Geometry.m_SyncVertexTargetsRight = CalculateVertexSyncTarget(ref value2.m_Geometry.m_Right, float4.y, distances2.y, netCompositionData2.m_SyncVertexOffsetsRight, syncVertexOffsetsRight2, divPos4.x, isRight: true);
			}
			m_StartNodeGeometryData[entity] = value;
			m_EndNodeGeometryData[entity] = value2;
		}

		private float GetStraightNodeLength(Entity node, NetGeometryData netGeometryData)
		{
			float num = -1f;
			if (m_SubObjects.TryGetBuffer(node, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					PrefabRef prefabRef = m_PrefabRefDataFromEntity[bufferData[i].m_SubObject];
					if (m_PlaceableObjectData.TryGetComponent(prefabRef.m_Prefab, out var componentData) && (componentData.m_Flags & Game.Objects.PlacementFlags.RoadNode) != Game.Objects.PlacementFlags.None)
					{
						num = math.max(num, componentData.m_PlacementOffset.z);
					}
				}
			}
			return math.select(num, netGeometryData.m_DefaultWidth * 0.5f, num < 0f);
		}

		private Segment CalculateStraightSegment(Segment segment, float length, out float2 divPos)
		{
			float3 value = MathUtils.EndTangent(segment.m_Left);
			float3 value2 = MathUtils.EndTangent(segment.m_Right);
			value = MathUtils.Normalize(value, value.xz) * length;
			value2 = MathUtils.Normalize(value2, value2.xz) * length;
			Segment result = default(Segment);
			result.m_Left = NetUtils.StraightCurve(segment.m_Left.d, segment.m_Left.d + value);
			result.m_Right = NetUtils.StraightCurve(segment.m_Right.d, segment.m_Right.d + value2);
			result.m_Length.x = MathUtils.Length(result.m_Left);
			result.m_Length.y = MathUtils.Length(result.m_Right);
			divPos = 0f;
			return result;
		}

		private Segment Invert(Segment segment)
		{
			Segment result = default(Segment);
			result.m_Left = MathUtils.Invert(segment.m_Right);
			result.m_Right = MathUtils.Invert(segment.m_Left);
			result.m_Length = segment.m_Length.yx;
			return result;
		}

		private float4 CalculateVertexSyncTarget(ref Segment roundaboutSegment, float3 middlePos, float radius, bool isRight, float startDistance, float endDistance, float4 a, float4 b, float t)
		{
			float2 @float;
			float num2;
			if (isRight)
			{
				@float = 1f - new float2(a.w, b.w);
				float3 value = middlePos - roundaboutSegment.m_Right.d;
				float num = (@float.y * endDistance - @float.x * startDistance) * 0.5f;
				value = MathUtils.Normalize(value, value.xz) * num;
				roundaboutSegment.m_Right.c += value * 0.5f;
				roundaboutSegment.m_Right.d += value;
				a = math.saturate(1f - (1f - a - @float.x) / (1f - @float.x));
				b = math.saturate(1f - (1f - b - @float.y) / (1f - @float.y));
				num2 = math.distance(middlePos.xz, roundaboutSegment.m_Right.d.xz);
			}
			else
			{
				@float = new float2(a.x, b.x);
				float3 value2 = middlePos - roundaboutSegment.m_Left.d;
				float num3 = (@float.y * endDistance - @float.x * startDistance) * 0.5f;
				value2 = MathUtils.Normalize(value2, value2.xz) * num3;
				roundaboutSegment.m_Left.c += value2 * 0.5f;
				roundaboutSegment.m_Left.d += value2;
				a = math.saturate((a - @float.x) / (1f - @float.x));
				b = math.saturate((b - @float.y) / (1f - @float.y));
				num2 = math.distance(middlePos.xz, roundaboutSegment.m_Left.d.xz);
			}
			float num4 = @float.x * startDistance / num2;
			startDistance -= @float.x * startDistance;
			endDistance -= @float.y * endDistance;
			float4 float2 = new float4(a.xw, b.xw);
			float4 float3 = new float4(a.yz, b.yz);
			float2 = math.select(float2, float3, (float2 == float3).zwxy);
			a.xw = float2.xy;
			b.xw = float2.zw;
			float num5 = math.lerp(startDistance, endDistance, t);
			float4 float4 = math.select(a, math.lerp(a * startDistance, b * endDistance, t) / num5, num5 >= float.Epsilon);
			if (isRight)
			{
				float4 = math.saturate((1f - float4) * num5 / radius);
				return math.saturate(1f - num4 - float4 * (1f - num4));
			}
			float4 = math.saturate(float4 * num5 / radius);
			return math.saturate(num4 + float4 * (1f - num4));
		}

		private float4 CalculateVertexSyncTarget(ref Segment startSegment, float startDistance, float endDistance, float4 a, float4 b, float t, bool isRight)
		{
			float num = (startDistance + endDistance) * 0.5f;
			float2 @float;
			if (isRight)
			{
				@float = 1f - new float2(a.w, b.w);
				float3 float2 = startSegment.m_Left.d - startSegment.m_Right.d;
				float num2 = (@float.y * endDistance - @float.x * startDistance) * 0.5f;
				num -= num2;
				float2 *= num2 / math.max(0.01f, startDistance);
				startSegment.m_Right.c += float2 * 0.5f;
				startSegment.m_Right.d += float2;
				a = math.saturate(1f - (1f - a - @float.x) / (1f - @float.x));
				b = math.saturate(1f - (1f - b - @float.y) / (1f - @float.y));
			}
			else
			{
				@float = new float2(a.x, b.x);
				float3 float3 = startSegment.m_Right.d - startSegment.m_Left.d;
				float num3 = (@float.y * endDistance - @float.x * startDistance) * 0.5f;
				num -= num3;
				float3 *= num3 / math.max(0.01f, startDistance);
				startSegment.m_Left.c += float3 * 0.5f;
				startSegment.m_Left.d += float3;
				a = math.saturate((a - @float.x) / (1f - @float.x));
				b = math.saturate((b - @float.y) / (1f - @float.y));
			}
			float num4 = @float.x * startDistance / math.max(0.01f, num);
			startDistance -= @float.x * startDistance;
			endDistance -= @float.y * endDistance;
			float4 float4 = new float4(a.xw, b.xw);
			float4 float5 = new float4(a.yz, b.yz);
			float4 = math.select(float4, float5, (float4 == float5).zwxy);
			a.xw = float4.xy;
			b.xw = float4.zw;
			float num5 = math.lerp(startDistance, endDistance, t);
			float4 float6 = math.select(a, math.lerp(a * startDistance, b * endDistance, t) / num5, num5 >= float.Epsilon);
			if (isRight)
			{
				return math.saturate(1f - num4 - (1f - float6) * (1f - num4));
			}
			return math.saturate(num4 + float6 * (1f - num4));
		}

		private float3 FindMiddleNodePos(Entity edge, Entity node)
		{
			float3 @float = new float3(1E+09f, 1E+09f, 1E+09f);
			float3 float2 = new float3(-1E+09f, -1E+09f, -1E+09f);
			EdgeIterator edgeIterator = new EdgeIterator(edge, node, m_Edges, m_EdgeDataFromEntity, m_TempData, m_HiddenData);
			EdgeIteratorValue value;
			while (edgeIterator.GetNext(out value))
			{
				EdgeGeometry edgeGeometry = m_GeometryDataFromEntity[value.m_Edge];
				Composition composition = m_CompositionDataFromEntity[value.m_Edge];
				NetCompositionData netCompositionData = m_PrefabCompositionData[composition.m_Edge];
				float3 y;
				if (value.m_End)
				{
					float t = 0.5f + netCompositionData.m_MiddleOffset / math.max(0.01f, netCompositionData.m_Width);
					y = math.lerp(edgeGeometry.m_End.m_Left.d, edgeGeometry.m_End.m_Right.d, t);
				}
				else
				{
					float t2 = 0.5f + netCompositionData.m_MiddleOffset / math.max(0.01f, netCompositionData.m_Width);
					y = math.lerp(edgeGeometry.m_Start.m_Left.a, edgeGeometry.m_Start.m_Right.a, t2);
				}
				@float = math.min(@float, y);
				float2 = math.max(float2, y);
			}
			return math.lerp(@float, float2, 0.5f);
		}

		private float2 StartOffset(Segment segment, float2 nodePos)
		{
			return math.normalizesafe(math.lerp(segment.m_Left.a.xz, segment.m_Right.a.xz, 0.5f) - nodePos + math.normalizesafe(MathUtils.StartTangent(segment.m_Left).xz + MathUtils.StartTangent(segment.m_Right).xz), new float2(0f, 1f));
		}

		private float2 EndOffset(Segment segment, float2 nodePos)
		{
			return math.normalizesafe(math.lerp(segment.m_Left.d.xz, segment.m_Right.d.xz, 0.5f) - nodePos - math.normalizesafe(MathUtils.EndTangent(segment.m_Left).xz + MathUtils.EndTangent(segment.m_Right).xz), new float2(0f, 1f));
		}

		private void AdjustSegmentWidth(ref Segment segment, NetCompositionData edgeCompositionData, NetCompositionData nodeCompositionData, bool invertEdge)
		{
			Segment segment2 = segment;
			float num = math.select(edgeCompositionData.m_MiddleOffset, 0f - edgeCompositionData.m_MiddleOffset, invertEdge);
			float2 @float = edgeCompositionData.m_Width * 0.5f + num - nodeCompositionData.m_MiddleOffset + nodeCompositionData.m_Width * new float2(-0.5f, 0.5f);
			@float /= math.max(0.01f, edgeCompositionData.m_Width);
			@float.y -= 1f;
			if (math.abs(@float.x) > 0.001f)
			{
				segment.m_Left = MathUtils.Lerp(segment2.m_Left, segment2.m_Right, @float.x);
			}
			if (math.abs(@float.y) > 0.001f)
			{
				segment.m_Right = MathUtils.Lerp(segment2.m_Right, segment2.m_Left, 0f - @float.y);
			}
		}

		private void CheckBestCurve(Bezier4x3 curve, float2 position, bool isRight, bool isEnd, ref Bezier4x3 bestCurve, ref float bestDistance, ref float bestT, ref bool2 bestStartEnd)
		{
			float t;
			float num = MathUtils.Distance(curve.xz, position, out t);
			if (num < bestDistance)
			{
				bestDistance = num;
				bestStartEnd = new bool2(!isEnd, isEnd) & new bool2(t < 0.001f, t > 0.999f);
				if (isRight)
				{
					bestCurve = curve;
					bestT = t;
				}
				else
				{
					bestCurve = MathUtils.Invert(curve);
					bestT = 1f - t;
				}
			}
		}

		private void CheckNodeCurves(Entity node, float2 middlePos, NetGeometryData edgePrefabGeometryData, bool isEnd, ref Bezier4x3 bestCurve, ref float bestDistance, ref float bestT, ref bool2 bestStartEnd)
		{
			Bezier4x3 bestCurve2 = bestCurve;
			float bestDistance2 = bestDistance;
			float bestT2 = bestT;
			bool2 bestStartEnd2 = bestStartEnd;
			EdgeIterator edgeIterator = new EdgeIterator(Entity.Null, node, m_Edges, m_EdgeDataFromEntity, m_TempData, m_HiddenData, includeMiddleConnections: true);
			EdgeIteratorValue value;
			while (edgeIterator.GetNext(out value))
			{
				if (value.m_Middle)
				{
					return;
				}
				PrefabRef prefabRef = m_PrefabRefDataFromEntity[value.m_Edge];
				NetGeometryData netGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
				if ((edgePrefabGeometryData.m_MergeLayers & netGeometryData.m_MergeLayers) == 0)
				{
					continue;
				}
				Composition composition = m_CompositionDataFromEntity[value.m_Edge];
				EdgeNodeGeometry geometry;
				NetCompositionData netCompositionData;
				if (value.m_End)
				{
					geometry = m_EndNodeGeometryData[value.m_Edge].m_Geometry;
					netCompositionData = m_PrefabCompositionData[composition.m_EndNode];
				}
				else
				{
					geometry = m_StartNodeGeometryData[value.m_Edge].m_Geometry;
					netCompositionData = m_PrefabCompositionData[composition.m_StartNode];
				}
				if (!(math.any(geometry.m_Left.m_Length > 0.05f) | math.any(geometry.m_Right.m_Length > 0.05f)))
				{
					continue;
				}
				if (geometry.m_MiddleRadius > 0f)
				{
					if (netCompositionData.m_SideConnectionOffset.x != 0f)
					{
						geometry.m_Left.m_Left = NetUtils.OffsetCurveLeftSmooth(geometry.m_Left.m_Left, netCompositionData.m_SideConnectionOffset.x);
						geometry.m_Right.m_Left = NetUtils.OffsetCurveLeftSmooth(geometry.m_Right.m_Left, netCompositionData.m_SideConnectionOffset.x);
					}
					if (netCompositionData.m_SideConnectionOffset.y != 0f)
					{
						geometry.m_Left.m_Right = NetUtils.OffsetCurveLeftSmooth(geometry.m_Left.m_Right, 0f - netCompositionData.m_SideConnectionOffset.y);
						geometry.m_Right.m_Right = NetUtils.OffsetCurveLeftSmooth(geometry.m_Right.m_Right, 0f - netCompositionData.m_SideConnectionOffset.y);
					}
					OffsetCurveHeight(ref geometry.m_Left.m_Left, netCompositionData.m_EdgeHeights.xz, new float2(0f, 0.5f));
					OffsetCurveHeight(ref geometry.m_Left.m_Right, netCompositionData.m_EdgeHeights.yw, new float2(0f, 0.5f));
					OffsetCurveHeight(ref geometry.m_Right.m_Left, netCompositionData.m_EdgeHeights.xz, new float2(0.5f, 1f));
					OffsetCurveHeight(ref geometry.m_Right.m_Right, netCompositionData.m_EdgeHeights.yw, new float2(0.5f, 1f));
					CheckBestCurve(geometry.m_Left.m_Left, middlePos, isRight: false, isEnd, ref bestCurve2, ref bestDistance2, ref bestT2, ref bestStartEnd2);
					CheckBestCurve(geometry.m_Left.m_Right, middlePos, isRight: true, isEnd, ref bestCurve2, ref bestDistance2, ref bestT2, ref bestStartEnd2);
					CheckBestCurve(geometry.m_Right.m_Left, middlePos, isRight: false, isEnd, ref bestCurve2, ref bestDistance2, ref bestT2, ref bestStartEnd2);
					CheckBestCurve(geometry.m_Right.m_Right, middlePos, isRight: true, isEnd, ref bestCurve2, ref bestDistance2, ref bestT2, ref bestStartEnd2);
				}
				else
				{
					if (netCompositionData.m_SideConnectionOffset.x != 0f)
					{
						geometry.m_Left.m_Left = NetUtils.OffsetCurveLeftSmooth(geometry.m_Left.m_Left, netCompositionData.m_SideConnectionOffset.x);
					}
					if (netCompositionData.m_SideConnectionOffset.y != 0f)
					{
						geometry.m_Right.m_Right = NetUtils.OffsetCurveLeftSmooth(geometry.m_Right.m_Right, 0f - netCompositionData.m_SideConnectionOffset.y);
					}
					OffsetCurveHeight(ref geometry.m_Left.m_Left, netCompositionData.m_EdgeHeights.xz, new float2(0f, 1f));
					OffsetCurveHeight(ref geometry.m_Right.m_Right, netCompositionData.m_EdgeHeights.yw, new float2(0f, 1f));
					CheckBestCurve(geometry.m_Left.m_Left, middlePos, isRight: false, isEnd, ref bestCurve2, ref bestDistance2, ref bestT2, ref bestStartEnd2);
					CheckBestCurve(geometry.m_Right.m_Right, middlePos, isRight: true, isEnd, ref bestCurve2, ref bestDistance2, ref bestT2, ref bestStartEnd2);
				}
			}
			bestCurve = bestCurve2;
			bestDistance = bestDistance2;
			bestT = bestT2;
			bestStartEnd = bestStartEnd2;
		}

		private void OffsetCurveHeight(ref Bezier4x3 curve, float2 heights, float2 heightDelta)
		{
			float4 @float = math.lerp(heights.x, heights.y, new float4(heightDelta.x, math.lerp(heightDelta.x, heightDelta.y, 1f / 3f), math.lerp(heightDelta.x, heightDelta.y, 2f / 3f), heightDelta.y));
			curve.a.y += @float.x;
			curve.b.y += @float.y;
			curve.c.y += @float.z;
			curve.d.y += @float.w;
		}

		private void FindTargetSegments(Entity edge, Entity node, float2 offset, float2 middlePos, NetGeometryData prefabGeometryData, NetCompositionData compositionData, out Segment leftSegment, out Segment rightSegment, out float2 distances, out float4 syncVertexOffsetsLeft, out float4 syncVertexOffsetsRight, out float roundaboutSize, out bool2 sideConnect, out bool2 middleConnect)
		{
			float2 x = new float2(offset.y, 0f - offset.x);
			float num = -2f;
			float num2 = 2f;
			float num3 = float.MaxValue;
			leftSegment = default(Segment);
			rightSegment = default(Segment);
			EdgeIteratorValue edgeIteratorValue = default(EdgeIteratorValue);
			EdgeIteratorValue edgeIteratorValue2 = default(EdgeIteratorValue);
			distances = 0f;
			roundaboutSize = 0f;
			sideConnect = false;
			bool flag = m_OutsideConnectionData.HasComponent(node);
			EdgeIterator edgeIterator = new EdgeIterator(edge, node, m_Edges, m_EdgeDataFromEntity, m_TempData, m_HiddenData, includeMiddleConnections: true);
			EdgeIteratorValue value;
			while (edgeIterator.GetNext(out value))
			{
				if (value.m_Middle)
				{
					if ((prefabGeometryData.m_MergeLayers & (Layer.Pathway | Layer.MarkerPathway)) == 0)
					{
						continue;
					}
					PrefabRef prefabRef = m_PrefabRefDataFromEntity[value.m_Edge];
					NetGeometryData edgePrefabGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
					EdgeGeometry edgeGeometry = m_GeometryDataFromEntity[value.m_Edge];
					Composition composition = m_CompositionDataFromEntity[value.m_Edge];
					NetCompositionData netCompositionData = m_PrefabCompositionData[composition.m_Edge];
					if (netCompositionData.m_SideConnectionOffset.x != 0f)
					{
						edgeGeometry.m_Start.m_Left = NetUtils.OffsetCurveLeftSmooth(edgeGeometry.m_Start.m_Left, netCompositionData.m_SideConnectionOffset.x);
						edgeGeometry.m_End.m_Left = NetUtils.OffsetCurveLeftSmooth(edgeGeometry.m_End.m_Left, netCompositionData.m_SideConnectionOffset.x);
					}
					if (netCompositionData.m_SideConnectionOffset.y != 0f)
					{
						edgeGeometry.m_Start.m_Right = NetUtils.OffsetCurveLeftSmooth(edgeGeometry.m_Start.m_Right, 0f - netCompositionData.m_SideConnectionOffset.y);
						edgeGeometry.m_End.m_Right = NetUtils.OffsetCurveLeftSmooth(edgeGeometry.m_End.m_Right, 0f - netCompositionData.m_SideConnectionOffset.y);
					}
					OffsetCurveHeight(ref edgeGeometry.m_Start.m_Left, netCompositionData.m_EdgeHeights.xz, new float2(0f, 0.5f));
					OffsetCurveHeight(ref edgeGeometry.m_Start.m_Right, netCompositionData.m_EdgeHeights.yw, new float2(0f, 0.5f));
					OffsetCurveHeight(ref edgeGeometry.m_End.m_Left, netCompositionData.m_EdgeHeights.xz, new float2(0.5f, 1f));
					OffsetCurveHeight(ref edgeGeometry.m_End.m_Right, netCompositionData.m_EdgeHeights.yw, new float2(0.5f, 1f));
					Bezier4x3 bestCurve = default(Bezier4x3);
					float bestDistance = float.MaxValue;
					float bestT = 0f;
					bool2 bestStartEnd = false;
					CheckBestCurve(edgeGeometry.m_Start.m_Left, middlePos, isRight: false, isEnd: false, ref bestCurve, ref bestDistance, ref bestT, ref bestStartEnd);
					CheckBestCurve(edgeGeometry.m_Start.m_Right, middlePos, isRight: true, isEnd: false, ref bestCurve, ref bestDistance, ref bestT, ref bestStartEnd);
					CheckBestCurve(edgeGeometry.m_End.m_Left, middlePos, isRight: false, isEnd: true, ref bestCurve, ref bestDistance, ref bestT, ref bestStartEnd);
					CheckBestCurve(edgeGeometry.m_End.m_Right, middlePos, isRight: true, isEnd: true, ref bestCurve, ref bestDistance, ref bestT, ref bestStartEnd);
					if (m_IterationIndex == 1 && math.any(bestStartEnd))
					{
						Edge edge2 = m_EdgeDataFromEntity[value.m_Edge];
						if (bestStartEnd.x)
						{
							CheckNodeCurves(edge2.m_Start, middlePos, edgePrefabGeometryData, isEnd: false, ref bestCurve, ref bestDistance, ref bestT, ref bestStartEnd);
						}
						if (bestStartEnd.y)
						{
							CheckNodeCurves(edge2.m_End, middlePos, edgePrefabGeometryData, isEnd: true, ref bestCurve, ref bestDistance, ref bestT, ref bestStartEnd);
						}
					}
					if (bestDistance < num3)
					{
						num3 = bestDistance;
						float3 @float = MathUtils.Position(bestCurve, bestT);
						float3 float2 = new float3
						{
							xz = math.normalizesafe(MathUtils.Left(MathUtils.Tangent(bestCurve, bestT).xz))
						};
						float3 float3 = @float;
						float3.xz += MathUtils.Left(float2.xz) * (compositionData.m_Width * 0.5f);
						leftSegment.m_Left = NetUtils.StraightCurve(float3, float3 + float2);
						leftSegment.m_Right = NetUtils.StraightCurve(@float, @float + float2);
						edgeIteratorValue = value;
						distances.x = compositionData.m_Width * 0.5f;
						float3 float4 = @float;
						float4.xz += MathUtils.Right(float2.xz) * (compositionData.m_Width * 0.5f);
						rightSegment.m_Left = NetUtils.StraightCurve(@float, @float + float2);
						rightSegment.m_Right = NetUtils.StraightCurve(float4, float4 + float2);
						edgeIteratorValue2 = value;
						distances.y = compositionData.m_Width * 0.5f;
					}
				}
				else
				{
					if (edgeIteratorValue.m_Middle)
					{
						continue;
					}
					Composition composition2 = m_CompositionDataFromEntity[value.m_Edge];
					EdgeGeometry edgeGeometry2 = m_GeometryDataFromEntity[value.m_Edge];
					NetCompositionData edgeCompositionData = m_PrefabCompositionData[composition2.m_Edge];
					Segment segment;
					NetCompositionData netCompositionData2;
					if (value.m_End)
					{
						segment = Invert(edgeGeometry2.m_End);
						netCompositionData2 = m_PrefabCompositionData[composition2.m_EndNode];
					}
					else
					{
						segment = edgeGeometry2.m_Start;
						netCompositionData2 = m_PrefabCompositionData[composition2.m_StartNode];
					}
					netCompositionData2.m_MiddleOffset = 0f - netCompositionData2.m_MiddleOffset;
					AdjustSegmentWidth(ref segment, edgeCompositionData, netCompositionData2, value.m_End);
					float num4 = 0.5f + netCompositionData2.m_MiddleOffset / math.max(0.01f, netCompositionData2.m_Width);
					float2 y = StartOffset(segment, middlePos);
					if (value.m_Edge == edge)
					{
						if (num < -1f)
						{
							if (prefabGeometryData.m_MergeLayers == Layer.None || flag)
							{
								leftSegment.m_Left = MathUtils.StartReflect(segment.m_Right);
								leftSegment.m_Right = MathUtils.StartReflect(segment.m_Left);
								num4 = 1f - num4;
							}
							else
							{
								leftSegment = segment;
							}
							leftSegment.m_Right = MathUtils.Lerp(leftSegment.m_Left, leftSegment.m_Right, num4);
							edgeIteratorValue = value;
							distances.x = netCompositionData2.m_Width * 0.5f + netCompositionData2.m_MiddleOffset;
						}
						if (num2 > 1f)
						{
							if (prefabGeometryData.m_MergeLayers == Layer.None || flag)
							{
								rightSegment.m_Left = MathUtils.StartReflect(segment.m_Right);
								rightSegment.m_Right = MathUtils.StartReflect(segment.m_Left);
								num4 = 1f - num4;
							}
							else
							{
								rightSegment = segment;
							}
							rightSegment.m_Left = MathUtils.Lerp(rightSegment.m_Left, rightSegment.m_Right, num4);
							edgeIteratorValue2 = value;
							distances.y = netCompositionData2.m_Width * 0.5f - netCompositionData2.m_MiddleOffset;
						}
					}
					else
					{
						PrefabRef prefabRef2 = m_PrefabRefDataFromEntity[value.m_Edge];
						NetGeometryData netGeometryData = m_PrefabGeometryData[prefabRef2.m_Prefab];
						if ((netGeometryData.m_MergeLayers & prefabGeometryData.m_MergeLayers) == 0)
						{
							if ((prefabGeometryData.m_MergeLayers & (Layer.Pathway | Layer.MarkerPathway)) == 0 || (netGeometryData.m_MergeLayers & Layer.Road) == 0)
							{
								continue;
							}
							sideConnect = true;
							if (netCompositionData2.m_SideConnectionOffset.y != 0f)
							{
								segment.m_Left = NetUtils.OffsetCurveLeftSmooth(segment.m_Left, netCompositionData2.m_SideConnectionOffset.y);
							}
							if (netCompositionData2.m_SideConnectionOffset.x != 0f)
							{
								segment.m_Right = NetUtils.OffsetCurveLeftSmooth(segment.m_Right, 0f - netCompositionData2.m_SideConnectionOffset.x);
							}
							segment.m_Left.y += netCompositionData2.m_EdgeHeights.y;
							segment.m_Right.y += netCompositionData2.m_EdgeHeights.x;
						}
						float num5;
						if (math.dot(offset, y) < 0f)
						{
							num5 = math.dot(x, y) * 0.5f;
						}
						else
						{
							float num6 = math.dot(x, y);
							num5 = math.select(-1f, 1f, num6 >= 0f) - num6 * 0.5f;
						}
						if (num5 > num)
						{
							num = num5;
							leftSegment = segment;
							leftSegment.m_Right = MathUtils.Lerp(leftSegment.m_Left, leftSegment.m_Right, num4);
							edgeIteratorValue = value;
							distances.x = netCompositionData2.m_Width * 0.5f + netCompositionData2.m_MiddleOffset;
						}
						if (num5 < num2)
						{
							num2 = num5;
							rightSegment = segment;
							rightSegment.m_Left = MathUtils.Lerp(rightSegment.m_Left, rightSegment.m_Right, num4);
							edgeIteratorValue2 = value;
							distances.y = netCompositionData2.m_Width * 0.5f - netCompositionData2.m_MiddleOffset;
						}
					}
					DynamicBuffer<NetCompositionPiece> pieces = m_PrefabCompositionPieces[composition2.m_Edge];
					float2 float5 = NetCompositionHelpers.CalculateRoundaboutSize(netCompositionData2, pieces);
					roundaboutSize = math.max(roundaboutSize, math.select(float5.x, float5.y, value.m_End));
				}
			}
			middleConnect = new bool2(edgeIteratorValue.m_Middle, edgeIteratorValue2.m_Middle);
			if (edgeIteratorValue.m_Edge != Entity.Null && !sideConnect.x && !edgeIteratorValue.m_Middle)
			{
				Composition composition3 = m_CompositionDataFromEntity[edgeIteratorValue.m_Edge];
				Entity entity = (edgeIteratorValue.m_End ? composition3.m_EndNode : composition3.m_StartNode);
				syncVertexOffsetsLeft = 1f - m_PrefabCompositionData[entity].m_SyncVertexOffsetsRight.wzyx;
			}
			else
			{
				syncVertexOffsetsLeft = new float4(0f, 1f / 3f, 2f / 3f, 1f);
			}
			if (edgeIteratorValue2.m_Edge != Entity.Null && !sideConnect.y && !edgeIteratorValue2.m_Middle)
			{
				Composition composition4 = m_CompositionDataFromEntity[edgeIteratorValue2.m_Edge];
				Entity entity2 = (edgeIteratorValue2.m_End ? composition4.m_EndNode : composition4.m_StartNode);
				syncVertexOffsetsRight = 1f - m_PrefabCompositionData[entity2].m_SyncVertexOffsetsLeft.wzyx;
			}
			else
			{
				syncVertexOffsetsRight = new float4(0f, 1f / 3f, 2f / 3f, 1f);
			}
		}

		private void CalculateSegments(Segment startLeftSegment, Segment startRightSegment, Segment endLeftSegment, Segment endRightSegment, float3 nodePosition, ref float leftRadius, ref float rightRadius, out Segment startSegment, out Segment endSegment, out float2 leftDivPos, out float2 rightDivPos)
		{
			CalculateMiddleCurves(startLeftSegment.m_Left, endLeftSegment.m_Left, nodePosition, ref leftRadius, right: false, out startSegment.m_Left, out endSegment.m_Left, out var divPos);
			CalculateMiddleCurves(startRightSegment.m_Right, endRightSegment.m_Right, nodePosition, ref rightRadius, right: true, out startSegment.m_Right, out endSegment.m_Right, out var divPos2);
			if (math.distancesq(endSegment.m_Left.d, endSegment.m_Right.d) < 0.1f)
			{
				endSegment.m_Left.d = math.lerp(endSegment.m_Left.d, endSegment.m_Right.d, 0.5f);
				endSegment.m_Right.d = endSegment.m_Left.d;
			}
			startSegment.m_Length.x = MathUtils.Length(startSegment.m_Left);
			startSegment.m_Length.y = MathUtils.Length(startSegment.m_Right);
			endSegment.m_Length.x = MathUtils.Length(endSegment.m_Left);
			endSegment.m_Length.y = MathUtils.Length(endSegment.m_Right);
			leftDivPos = divPos;
			rightDivPos = divPos2;
		}

		private void CalculateMiddleCurves(Bezier4x3 startCurve, Bezier4x3 endCurve, float3 nodePosition, ref float radius, bool right, out Bezier4x3 startMiddleCurve, out Bezier4x3 endMiddleCurve, out float divPos)
		{
			float2 @float = math.normalizesafe(startCurve.d.xz - nodePosition.xz);
			float2 toVector = math.normalizesafe(endCurve.a.xz - nodePosition.xz);
			float3 endTangent = default(float3);
			float3 float2 = default(float3);
			float num2;
			float2 float3;
			float2 float4;
			if (right)
			{
				float num = MathUtils.RotationAngleLeft(@float, toVector) * 0.5f;
				num2 = math.max(math.min(num * 0.5f, MathF.PI / 8f), num - MathF.PI / 2f);
				float3 = MathUtils.RotateLeft(@float, num);
				float4 = MathUtils.RotateLeft(@float, num2);
				endTangent.xz = MathUtils.Left(float3);
				float2.xz = MathUtils.Left(float4);
			}
			else
			{
				float num3 = MathUtils.RotationAngleRight(@float, toVector) * 0.5f;
				num2 = math.max(math.min(num3 * 0.5f, MathF.PI / 8f), num3 - MathF.PI / 2f);
				float3 = MathUtils.RotateRight(@float, num3);
				float4 = MathUtils.RotateRight(@float, num2);
				endTangent.xz = MathUtils.Right(float3);
				float2.xz = MathUtils.Right(float4);
			}
			float3 value = MathUtils.EndTangent(startCurve);
			value = MathUtils.Normalize(value, value.xz);
			float divPos2;
			float middleDistance;
			Bezier4x3 input = CalculateMiddleCurve(startCurve, endCurve, nodePosition, out middleDistance, out divPos2);
			MathUtils.Divide(input, out var output, out var output2, 0.5f);
			output.c.y = output2.d.y;
			output.d.y = output2.d.y;
			output2.a.y = output2.d.y;
			output2.b.y = output2.d.y;
			output2.c.y = output2.d.y;
			middleDistance = math.select(middleDistance, 0f, math.dot(input.d.xz - nodePosition.xz, float3) <= 0f);
			float t = math.smoothstep(radius, radius * 1.2f, middleDistance);
			float num4 = math.max(radius, middleDistance);
			float3 d = startCurve.d;
			float3 endPos = nodePosition;
			float3 float5 = nodePosition;
			endPos.xz += float3 * num4;
			float5.xz += float4 * num4;
			startMiddleCurve = NetUtils.FitCurve(d, value, float2, float5);
			endMiddleCurve = NetUtils.FitCurve(float5, float2, endTangent, endPos);
			divPos = 0.5f;
			float num5 = math.max(0f, (num2 - MathF.PI / 8f) * 0.84882635f);
			startMiddleCurve.b += (startMiddleCurve.a - startMiddleCurve.b) * (num5 * 0.5f);
			startMiddleCurve.c += (startMiddleCurve.c - startMiddleCurve.d) * num5;
			startMiddleCurve = MathUtils.Lerp(startMiddleCurve, output, t);
			endMiddleCurve = MathUtils.Lerp(endMiddleCurve, output2, t);
			divPos = math.lerp(divPos, divPos2, t);
		}

		private Segment CalculateSideConnection(Segment startSegment, Segment endLeftSegment, Segment endRightSegment, NetCompositionData edgeCompositionData, NetCompositionData nodeCompositionData, bool right, out float2 divPos)
		{
			float3 value = -MathUtils.StartTangent(endLeftSegment.m_Left);
			float3 value2 = MathUtils.StartTangent(endRightSegment.m_Right);
			value = MathUtils.Normalize(value, value.xz);
			value2 = MathUtils.Normalize(value2, value2.xz);
			value.y = math.clamp(value.y, -1f, 1f);
			value2.y = math.clamp(value2.y, -1f, 1f);
			Bezier4x3 curve = NetUtils.FitCurve(endLeftSegment.m_Left.a, value, value2, endRightSegment.m_Right.a);
			MathUtils.Distance(curve, startSegment.m_Left.d, out var t);
			MathUtils.Distance(curve, startSegment.m_Right.d, out var t2);
			float num = math.max(edgeCompositionData.m_NodeOffset, nodeCompositionData.m_NodeOffset);
			if (right)
			{
				Bounds1 t3 = new Bounds1(t2, 1f);
				MathUtils.ClampLength(curve, ref t3, num * 0.5f);
				t2 = t3.min;
				value = MathUtils.Tangent(curve, t);
				value.xz = math.normalizesafe(MathUtils.Left(value.xz));
				value.y = 0f;
				value2 = MathUtils.Tangent(curve, t2);
				value2.xz = math.normalizesafe(MathUtils.Left(value2.xz) + value2.xz);
				value2.y = math.clamp(value2.y * 0.5f, -1f, 1f);
			}
			else
			{
				Bounds1 t4 = new Bounds1(0f, t);
				MathUtils.ClampLengthInverse(curve, ref t4, num * 0.5f);
				t = t4.max;
				value = MathUtils.Tangent(curve, t2);
				value.xz = math.normalizesafe(MathUtils.Left(value.xz) - value.xz);
				value.y = math.clamp(value.y * 0.5f, -1f, 1f);
				value2 = MathUtils.Tangent(curve, t2);
				value2.xz = math.normalizesafe(MathUtils.Left(value2.xz));
				value2.y = 0f;
			}
			float3 endPos = MathUtils.Position(curve, t);
			float3 endPos2 = MathUtils.Position(curve, t2);
			float3 value3 = MathUtils.EndTangent(startSegment.m_Left);
			float3 value4 = MathUtils.EndTangent(startSegment.m_Right);
			value3 = MathUtils.Normalize(value3, value3.xz);
			value4 = MathUtils.Normalize(value4, value4.xz);
			value3.y = math.clamp(value3.y, -1f, 1f);
			value4.y = math.clamp(value4.y, -1f, 1f);
			Segment result = default(Segment);
			result.m_Left = NetUtils.FitCurve(startSegment.m_Left.d, value3, value, endPos);
			result.m_Right = NetUtils.FitCurve(startSegment.m_Right.d, value4, value2, endPos2);
			result.m_Length.x = MathUtils.Length(result.m_Left);
			result.m_Length.y = MathUtils.Length(result.m_Right);
			divPos = 0f;
			return result;
		}

		private Segment CalculateMiddleConnection(Segment startSegment, Segment endSegment, out float2 divPos)
		{
			Segment result = default(Segment);
			result.m_Left = CalculateMiddleConnection(startSegment.m_Left, endSegment.m_Left, out divPos.x);
			result.m_Right = CalculateMiddleConnection(startSegment.m_Right, endSegment.m_Right, out divPos.y);
			result.m_Length.x = MathUtils.Length(result.m_Left);
			result.m_Length.y = MathUtils.Length(result.m_Right);
			return result;
		}

		private Bezier4x3 CalculateMiddleConnection(Bezier4x3 startCurve, Bezier4x3 endCurve, out float divPos)
		{
			float3 value = MathUtils.EndTangent(startCurve);
			float3 value2 = MathUtils.StartTangent(endCurve);
			value = MathUtils.Normalize(value, value.xz);
			value2 = MathUtils.Normalize(value2, value2.xz);
			value.y = math.clamp(value.y, -1f, 1f);
			value2.y = math.clamp(value2.y, -1f, 1f);
			divPos = 0f;
			return NetUtils.FitCurve(startCurve.d, value, value2, endCurve.a);
		}

		private Segment CalculateMiddleSegment(Segment startSegment, Segment endSegment, out float2 divPos)
		{
			Segment result = default(Segment);
			result.m_Left = CalculateMiddleCurve(startSegment.m_Left, endSegment.m_Left, out divPos.x);
			result.m_Right = CalculateMiddleCurve(startSegment.m_Right, endSegment.m_Right, out divPos.y);
			result.m_Length.x = MathUtils.Length(result.m_Left);
			result.m_Length.y = MathUtils.Length(result.m_Right);
			return result;
		}

		private Bezier4x3 CalculateMiddleCurve(Bezier4x3 startCurve, Bezier4x3 endCurve, out float divPos)
		{
			float3 value = MathUtils.EndTangent(startCurve);
			float3 value2 = MathUtils.StartTangent(endCurve);
			value = MathUtils.Normalize(value, value.xz);
			value2 = MathUtils.Normalize(value2, value2.xz);
			value.y = math.clamp(value.y, -1f, 1f);
			value2.y = math.clamp(value2.y, -1f, 1f);
			Bezier4x3 curve = NetUtils.FitCurve(startCurve.d, value, value2, endCurve.a);
			divPos = NetUtils.FindMiddleTangentPos(curve.xz, new float2(0f, 1f));
			float3 endPos = MathUtils.Position(curve, divPos);
			value2 = MathUtils.Tangent(curve, divPos);
			value2 = MathUtils.Normalize(value2, value2.xz);
			value2.y = math.clamp(value2.y, -1f, 1f);
			return NetUtils.FitCurve(startCurve.d, value, value2, endPos);
		}

		private Bezier4x3 CalculateMiddleCurve(Bezier4x3 startCurve, Bezier4x3 endCurve, float3 middlePosition, out float middleDistance, out float divPos)
		{
			float3 value = MathUtils.EndTangent(startCurve);
			float3 value2 = MathUtils.StartTangent(endCurve);
			value = MathUtils.Normalize(value, value.xz);
			value2 = MathUtils.Normalize(value2, value2.xz);
			value.y = math.clamp(value.y, -1f, 1f);
			value2.y = math.clamp(value2.y, -1f, 1f);
			Bezier4x3 curve = NetUtils.FitCurve(startCurve.d, value, value2, endCurve.a);
			middleDistance = MathUtils.Distance(curve.xz, middlePosition.xz, out var _);
			divPos = NetUtils.FindMiddleTangentPos(curve.xz, new float2(0f, 1f));
			float3 endPos = MathUtils.Position(curve, divPos);
			value2 = MathUtils.Tangent(curve, divPos);
			value2 = MathUtils.Normalize(value2, value2.xz);
			value2.y = math.clamp(value2.y, -1f, 1f);
			return NetUtils.FitCurve(startCurve.d, value, value2, endPos);
		}
	}

	private struct IntersectionData
	{
		public Bezier4x3 m_StartMiddle;

		public Bezier4x3 m_EndMiddle;

		public Bounds3 m_StartBounds;

		public Bounds3 m_EndBounds;
	}

	[BurstCompile]
	private struct CalculateIntersectionGeometryJob : IJobParallelForDefer
	{
		[ReadOnly]
		public NativeArray<Entity> m_Entities;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public ComponentLookup<Node> m_NodeDataFromEntity;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeDataFromEntity;

		[ReadOnly]
		public ComponentLookup<NodeGeometry> m_NodeGeometryData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> m_StartGeometryDataFromEntity;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> m_EndGeometryDataFromEntity;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_Edges;

		[NativeDisableParallelForRestriction]
		public NativeList<IntersectionData> m_BufferedData;

		public void Execute(int index)
		{
			Entity entity = m_Entities[index];
			Edge edge = m_EdgeDataFromEntity[entity];
			Composition composition = m_CompositionData[entity];
			StartNodeGeometry startNodeGeometry = m_StartGeometryDataFromEntity[entity];
			EndNodeGeometry endNodeGeometry = m_EndGeometryDataFromEntity[entity];
			PrefabRef prefabRef = m_PrefabRefData[entity];
			NetGeometryData prefabGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
			NetCompositionData netCompositionData = m_PrefabCompositionData[composition.m_StartNode];
			NetCompositionData netCompositionData2 = m_PrefabCompositionData[composition.m_EndNode];
			IntersectionData value = default(IntersectionData);
			NodeGeometry nodeGeometry = m_NodeGeometryData[edge.m_Start];
			NodeGeometry nodeGeometry2 = m_NodeGeometryData[edge.m_End];
			bool flag = (netCompositionData.m_Flags.m_General & (CompositionFlags.General.Roundabout | CompositionFlags.General.LevelCrossing | CompositionFlags.General.FixedNodeSize)) != 0;
			bool flag2 = (netCompositionData2.m_Flags.m_General & (CompositionFlags.General.Roundabout | CompositionFlags.General.LevelCrossing | CompositionFlags.General.FixedNodeSize)) != 0;
			float3 @float;
			if (startNodeGeometry.m_Geometry.m_MiddleRadius > 0f)
			{
				@float = m_NodeDataFromEntity[edge.m_Start].m_Position;
				@float.y = nodeGeometry.m_Position;
			}
			else
			{
				@float = FindIntersectionPos(entity, edge.m_Start, prefabGeometryData);
				if (flag)
				{
					@float.y = nodeGeometry.m_Position;
				}
				Flatten(ref startNodeGeometry.m_Geometry, @float, flag);
			}
			float3 float2;
			if (endNodeGeometry.m_Geometry.m_MiddleRadius > 0f)
			{
				float2 = m_NodeDataFromEntity[edge.m_End].m_Position;
				float2.y = nodeGeometry2.m_Position;
			}
			else
			{
				float2 = FindIntersectionPos(entity, edge.m_End, prefabGeometryData);
				if (flag2)
				{
					float2.y = nodeGeometry2.m_Position;
				}
				Flatten(ref endNodeGeometry.m_Geometry, float2, flag2);
			}
			if (startNodeGeometry.m_Geometry.m_MiddleRadius > 0f)
			{
				float t = netCompositionData.m_MiddleOffset / math.max(0.01f, netCompositionData.m_Width) + 0.5f;
				value.m_StartMiddle = MathUtils.Lerp(startNodeGeometry.m_Geometry.m_Left.m_Left, startNodeGeometry.m_Geometry.m_Left.m_Right, t);
				MoveEndTo(ref value.m_StartMiddle, @float);
			}
			else
			{
				bool num = math.all(startNodeGeometry.m_Geometry.m_Middle.d.xy != 0f);
				value.m_StartMiddle = MathUtils.Lerp(startNodeGeometry.m_Geometry.m_Left.m_Right, startNodeGeometry.m_Geometry.m_Right.m_Left, 0.5f);
				if (!num)
				{
					value.m_StartMiddle.d = @float;
				}
			}
			if (endNodeGeometry.m_Geometry.m_MiddleRadius > 0f)
			{
				float t2 = netCompositionData2.m_MiddleOffset / math.max(0.01f, netCompositionData2.m_Width) + 0.5f;
				value.m_EndMiddle = MathUtils.Lerp(endNodeGeometry.m_Geometry.m_Left.m_Left, endNodeGeometry.m_Geometry.m_Left.m_Right, t2);
				MoveEndTo(ref value.m_EndMiddle, float2);
			}
			else
			{
				bool num2 = math.all(endNodeGeometry.m_Geometry.m_Middle.d.xy != 0f);
				value.m_EndMiddle = MathUtils.Lerp(endNodeGeometry.m_Geometry.m_Left.m_Right, endNodeGeometry.m_Geometry.m_Right.m_Left, 0.5f);
				if (!num2)
				{
					value.m_EndMiddle.d = float2;
				}
			}
			if (prefabGeometryData.m_MergeLayers == Layer.None)
			{
				float num3 = netCompositionData.m_Width * 0.5f;
				float num4 = netCompositionData2.m_Width * 0.5f;
				float3 float3 = math.lerp(startNodeGeometry.m_Geometry.m_Left.m_Left.a, startNodeGeometry.m_Geometry.m_Right.m_Right.a, 0.5f);
				float3 float4 = math.lerp(endNodeGeometry.m_Geometry.m_Left.m_Left.a, endNodeGeometry.m_Geometry.m_Right.m_Right.a, 0.5f);
				value.m_StartBounds = MathUtils.Bounds(float3, float3);
				value.m_EndBounds = MathUtils.Bounds(float4, float4);
				value.m_StartBounds.min.xz -= num3;
				value.m_StartBounds.max.xz += num3;
				value.m_EndBounds.min.xz -= num4;
				value.m_EndBounds.max.xz += num4;
			}
			else
			{
				value.m_StartBounds = MathUtils.TightBounds(startNodeGeometry.m_Geometry.m_Left.m_Left) | MathUtils.TightBounds(startNodeGeometry.m_Geometry.m_Left.m_Right) | MathUtils.TightBounds(startNodeGeometry.m_Geometry.m_Right.m_Left) | MathUtils.TightBounds(startNodeGeometry.m_Geometry.m_Right.m_Right) | MathUtils.TightBounds(value.m_StartMiddle);
				value.m_EndBounds = MathUtils.TightBounds(endNodeGeometry.m_Geometry.m_Left.m_Left) | MathUtils.TightBounds(endNodeGeometry.m_Geometry.m_Left.m_Right) | MathUtils.TightBounds(endNodeGeometry.m_Geometry.m_Right.m_Left) | MathUtils.TightBounds(endNodeGeometry.m_Geometry.m_Right.m_Right) | MathUtils.TightBounds(value.m_EndMiddle);
			}
			value.m_StartBounds.min.y += netCompositionData.m_HeightRange.min;
			value.m_StartBounds.max.y += netCompositionData.m_HeightRange.max;
			value.m_EndBounds.min.y += netCompositionData2.m_HeightRange.min;
			value.m_EndBounds.max.y += netCompositionData2.m_HeightRange.max;
			if ((netCompositionData.m_State & (CompositionState.LowerToTerrain | CompositionState.RaiseToTerrain)) != 0)
			{
				Bounds1 bounds = SampleTerrain(startNodeGeometry.m_Geometry.m_Left.m_Left) | SampleTerrain(startNodeGeometry.m_Geometry.m_Right.m_Right);
				if (startNodeGeometry.m_Geometry.m_MiddleRadius > 0f)
				{
					bounds |= SampleTerrain(startNodeGeometry.m_Geometry.m_Left.m_Right) | SampleTerrain(startNodeGeometry.m_Geometry.m_Right.m_Left);
				}
				if ((netCompositionData.m_State & CompositionState.LowerToTerrain) != 0)
				{
					value.m_StartBounds.min.y = math.min(value.m_StartBounds.min.y, bounds.min);
				}
				if ((netCompositionData.m_State & CompositionState.RaiseToTerrain) != 0)
				{
					value.m_StartBounds.max.y = math.max(value.m_StartBounds.max.y, bounds.max);
				}
			}
			if ((netCompositionData2.m_State & (CompositionState.LowerToTerrain | CompositionState.RaiseToTerrain)) != 0)
			{
				Bounds1 bounds2 = SampleTerrain(endNodeGeometry.m_Geometry.m_Left.m_Left) | SampleTerrain(endNodeGeometry.m_Geometry.m_Right.m_Right);
				if (endNodeGeometry.m_Geometry.m_MiddleRadius > 0f)
				{
					bounds2 |= SampleTerrain(endNodeGeometry.m_Geometry.m_Left.m_Right) | SampleTerrain(endNodeGeometry.m_Geometry.m_Right.m_Left);
				}
				if ((netCompositionData2.m_State & CompositionState.LowerToTerrain) != 0)
				{
					value.m_EndBounds.min.y = math.min(value.m_EndBounds.min.y, bounds2.min);
				}
				if ((netCompositionData2.m_State & CompositionState.RaiseToTerrain) != 0)
				{
					value.m_EndBounds.max.y = math.max(value.m_EndBounds.max.y, bounds2.max);
				}
			}
			m_BufferedData[index] = value;
		}

		private Bounds1 SampleTerrain(Bezier4x3 curve)
		{
			Bounds1 result = new Bounds1(float.MaxValue, float.MinValue);
			for (int i = 0; i <= 8; i++)
			{
				result |= TerrainUtils.SampleHeight(ref m_TerrainHeightData, MathUtils.Position(curve, (float)i * 0.125f));
			}
			return result;
		}

		private void MoveEndTo(ref Bezier4x3 curve, float3 pos)
		{
			float num = math.distance(curve.d, pos);
			curve.b += math.normalizesafe(curve.b - curve.a) * (num * (1f / 3f));
			curve.c = pos + (curve.c - curve.d) + math.normalizesafe(curve.c - curve.d) * (num * (1f / 3f));
			curve.d = pos;
		}

		private float3 FindIntersectionPos(Entity edge, Entity node, NetGeometryData prefabGeometryData)
		{
			float3 result = default(float3);
			float num = 0f;
			EdgeIterator edgeIterator = new EdgeIterator(edge, node, m_Edges, m_EdgeDataFromEntity, m_TempData, m_HiddenData);
			EdgeIteratorValue value;
			while (edgeIterator.GetNext(out value))
			{
				EdgeNodeGeometry edgeNodeGeometry = ((!value.m_End) ? m_StartGeometryDataFromEntity[value.m_Edge].m_Geometry : m_EndGeometryDataFromEntity[value.m_Edge].m_Geometry);
				if (value.m_Edge != edge)
				{
					PrefabRef prefabRef = m_PrefabRefData[value.m_Edge];
					NetGeometryData netGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
					if ((prefabGeometryData.m_MergeLayers & netGeometryData.m_MergeLayers) == 0)
					{
						continue;
					}
				}
				result += math.lerp(edgeNodeGeometry.m_Left.m_Right.d, edgeNodeGeometry.m_Right.m_Left.d, 0.5f);
				num += 1f;
			}
			if (num != 0f)
			{
				result /= num;
			}
			return result;
		}

		private void Flatten(ref EdgeNodeGeometry geometry, float3 middlePos, bool edges)
		{
			if (edges)
			{
				Flatten(ref geometry.m_Left.m_Left, middlePos.y);
				Flatten(ref geometry.m_Left.m_Right, middlePos.y);
				Flatten(ref geometry.m_Right.m_Left, middlePos.y);
				Flatten(ref geometry.m_Right.m_Right, middlePos.y);
				return;
			}
			float num = math.distance(geometry.m_Left.m_Left.d.xz, middlePos.xz);
			float num2 = math.distance(geometry.m_Left.m_Right.d.xz, middlePos.xz);
			float num3 = math.distance(geometry.m_Right.m_Left.d.xz, middlePos.xz);
			float num4 = math.distance(geometry.m_Right.m_Right.d.xz, middlePos.xz);
			float middleHeight = math.lerp(middlePos.y, geometry.m_Left.m_Left.d.y, math.saturate(num2 / num));
			float middleHeight2 = math.lerp(middlePos.y, geometry.m_Right.m_Right.d.y, math.saturate(num3 / num4));
			Flatten(ref geometry.m_Left.m_Right, middleHeight);
			Flatten(ref geometry.m_Right.m_Left, middleHeight2);
		}

		private void Flatten(ref Bezier4x3 curve, float middleHeight)
		{
			curve.c.y += middleHeight - curve.d.y;
			curve.d.y = middleHeight;
		}
	}

	[BurstCompile]
	private struct CopyNodeGeometryJob : IJobParallelForDefer
	{
		[ReadOnly]
		public NativeArray<Entity> m_Entities;

		[ReadOnly]
		public NativeList<IntersectionData> m_BufferedData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

		public void Execute(int index)
		{
			Entity entity = m_Entities[index];
			IntersectionData intersectionData = m_BufferedData[index];
			StartNodeGeometry value = m_StartNodeGeometryData[entity];
			value.m_Geometry.m_Middle = intersectionData.m_StartMiddle;
			value.m_Geometry.m_Bounds = intersectionData.m_StartBounds;
			m_StartNodeGeometryData[entity] = value;
			EndNodeGeometry value2 = m_EndNodeGeometryData[entity];
			value2.m_Geometry.m_Middle = intersectionData.m_EndMiddle;
			value2.m_Geometry.m_Bounds = intersectionData.m_EndBounds;
			m_EndNodeGeometryData[entity] = value2;
		}
	}

	[BurstCompile]
	private struct UpdateNodeGeometryJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public ComponentTypeHandle<Node> m_NodeType;

		[ReadOnly]
		public ComponentTypeHandle<Orphan> m_OrphanType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		public ComponentTypeHandle<NodeGeometry> m_NodeGeometryType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Node> nativeArray2 = chunk.GetNativeArray(ref m_NodeType);
			NativeArray<Orphan> nativeArray3 = chunk.GetNativeArray(ref m_OrphanType);
			NativeArray<NodeGeometry> nativeArray4 = chunk.GetNativeArray(ref m_NodeGeometryType);
			NativeArray<PrefabRef> nativeArray5 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray4.Length; i++)
			{
				Entity node = nativeArray[i];
				Node node2 = nativeArray2[i];
				Bounds3 bounds = default(Bounds3);
				if (nativeArray3.Length != 0)
				{
					Orphan orphan = nativeArray3[i];
					NetCompositionData netCompositionData = m_PrefabCompositionData[orphan.m_Composition];
					float num = netCompositionData.m_Width * 0.5f;
					bounds.xz = new Bounds2(node2.m_Position.xz - num, node2.m_Position.xz + num);
					bounds.y = node2.m_Position.y + netCompositionData.m_HeightRange;
					if ((netCompositionData.m_State & (CompositionState.LowerToTerrain | CompositionState.RaiseToTerrain)) != 0)
					{
						Bounds1 bounds2 = new Bounds1(float.MaxValue, float.MinValue);
						bounds2 |= TerrainUtils.SampleHeight(ref m_TerrainHeightData, new float3(node2.m_Position.xy, bounds.min.z));
						bounds2 |= TerrainUtils.SampleHeight(ref m_TerrainHeightData, new float3(bounds.min.x, node2.m_Position.yz));
						bounds2 |= TerrainUtils.SampleHeight(ref m_TerrainHeightData, new float3(bounds.max.x, node2.m_Position.yz));
						bounds2 |= TerrainUtils.SampleHeight(ref m_TerrainHeightData, new float3(node2.m_Position.xy, bounds.max.z));
						if ((netCompositionData.m_State & CompositionState.LowerToTerrain) != 0)
						{
							bounds.min.y = math.min(bounds.min.y, bounds2.min);
						}
						if ((netCompositionData.m_State & CompositionState.RaiseToTerrain) != 0)
						{
							bounds.max.y = math.max(bounds.max.y, bounds2.max);
						}
					}
				}
				else
				{
					PrefabRef prefabRef = nativeArray5[i];
					NetGeometryData netGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
					float num = netGeometryData.m_DefaultWidth * 0.5f;
					bounds.xz = new Bounds2(node2.m_Position.xz - num, node2.m_Position.xz + num);
					bounds.y = node2.m_Position.y + netGeometryData.m_DefaultHeightRange;
				}
				EdgeIterator edgeIterator = new EdgeIterator(Entity.Null, node, m_ConnectedEdges, m_EdgeData, m_TempData, m_HiddenData);
				EdgeIteratorValue value;
				while (edgeIterator.GetNext(out value))
				{
					bounds |= ((!value.m_End) ? m_StartNodeGeometryData[value.m_Edge].m_Geometry : m_EndNodeGeometryData[value.m_Edge].m_Geometry).m_Bounds;
				}
				NodeGeometry value2 = nativeArray4[i];
				value2.m_Bounds = bounds;
				nativeArray4[i] = value2;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Node> __Game_Net_Node_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<NodeGeometry> __Game_Net_NodeGeometry_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Hidden> __Game_Tools_Hidden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Elevation> __Game_Net_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OutsideConnection> __Game_Net_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NodeGeometry> __Game_Net_NodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableNetData> __Game_Prefabs_PlaceableNetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<NetCompositionLane> __Game_Prefabs_NetCompositionLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<NetCompositionCrosswalk> __Game_Prefabs_NetCompositionCrosswalk_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<NetCompositionPiece> __Game_Prefabs_NetCompositionPiece_RO_BufferLookup;

		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RW_ComponentLookup;

		public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RW_ComponentLookup;

		public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RW_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<NodeGeometry> __Game_Net_NodeGeometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Orphan> __Game_Net_Orphan_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Net_Node_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Node>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Net_NodeGeometry_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NodeGeometry>();
			__Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentLookup = state.GetComponentLookup<Hidden>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentLookup = state.GetComponentLookup<Elevation>(isReadOnly: true);
			__Game_Net_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<OutsideConnection>(isReadOnly: true);
			__Game_Net_NodeGeometry_RO_ComponentLookup = state.GetComponentLookup<NodeGeometry>(isReadOnly: true);
			__Game_Prefabs_PlaceableNetData_RO_ComponentLookup = state.GetComponentLookup<PlaceableNetData>(isReadOnly: true);
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<SubNet>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Prefabs_NetCompositionLane_RO_BufferLookup = state.GetBufferLookup<NetCompositionLane>(isReadOnly: true);
			__Game_Prefabs_NetCompositionCrosswalk_RO_BufferLookup = state.GetBufferLookup<NetCompositionCrosswalk>(isReadOnly: true);
			__Game_Prefabs_NetCompositionPiece_RO_BufferLookup = state.GetBufferLookup<NetCompositionPiece>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RW_ComponentLookup = state.GetComponentLookup<EdgeGeometry>();
			__Game_Net_StartNodeGeometry_RW_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>();
			__Game_Net_EndNodeGeometry_RW_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>();
			__Game_Net_NodeGeometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<NodeGeometry>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
			__Game_Net_Orphan_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Orphan>(isReadOnly: true);
		}
	}

	private TerrainSystem m_TerrainSystem;

	private EntityQuery m_UpdatedEdgesQuery;

	private EntityQuery m_UpdatedNodesQuery;

	private EntityQuery m_AllEdgesQuery;

	private EntityQuery m_AllNodesQuery;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_UpdatedEdgesQuery = GetEntityQuery(ComponentType.ReadWrite<EdgeGeometry>(), ComponentType.ReadOnly<Updated>());
		m_UpdatedNodesQuery = GetEntityQuery(ComponentType.ReadWrite<NodeGeometry>(), ComponentType.ReadOnly<Updated>());
		m_AllEdgesQuery = GetEntityQuery(ComponentType.ReadWrite<EdgeGeometry>());
		m_AllNodesQuery = GetEntityQuery(ComponentType.ReadWrite<NodeGeometry>());
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
		bool loaded = GetLoaded();
		EntityQuery entityQuery = (loaded ? m_AllEdgesQuery : m_UpdatedEdgesQuery);
		EntityQuery query = (loaded ? m_AllNodesQuery : m_UpdatedNodesQuery);
		if (!entityQuery.IsEmptyIgnoreFilter || !query.IsEmptyIgnoreFilter)
		{
			JobHandle outJobHandle;
			NativeList<Entity> nativeList = entityQuery.ToEntityListAsync(Allocator.TempJob, out outJobHandle);
			NativeList<IntersectionData> nativeList2 = new NativeList<IntersectionData>(0, Allocator.TempJob);
			NativeParallelHashMap<int2, float4> edgeHeightMap = new NativeParallelHashMap<int2, float4>(0, Allocator.TempJob);
			TerrainHeightData data = m_TerrainSystem.GetHeightData();
			Bounds3 bounds = TerrainUtils.GetBounds(ref data);
			AllocateBuffersJob jobData = new AllocateBuffersJob
			{
				m_Entities = nativeList,
				m_IntersectionData = nativeList2,
				m_EdgeHeightMap = edgeHeightMap
			};
			InitializeNodeGeometryJob jobData2 = new InitializeNodeGeometryJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_NodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_NodeGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_NodeGeometry_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CompositionDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Edges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_Loaded = loaded
			};
			CalculateEdgeGeometryJob jobData3 = new CalculateEdgeGeometryJob
			{
				m_Entities = nativeList.AsDeferredJobArray(),
				m_TerrainBounds = bounds,
				m_PrefabRefDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CompositionDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OutsideConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PlaceableNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableNetData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PlaceableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NetLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Edges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabCompositionLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabCompositionCrosswalks = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionCrosswalk_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabCompositionPieces = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionPiece_RO_BufferLookup, ref base.CheckedStateRef),
				m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RW_ComponentLookup, ref base.CheckedStateRef),
				m_StartNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RW_ComponentLookup, ref base.CheckedStateRef),
				m_EndNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RW_ComponentLookup, ref base.CheckedStateRef)
			};
			FlattenNodeGeometryJob jobData4 = new FlattenNodeGeometryJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_NodeGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_NodeGeometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EdgeDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Edges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_EdgeHeightMap = edgeHeightMap.AsParallelWriter()
			};
			FinishEdgeGeometryJob jobData5 = new FinishEdgeGeometryJob
			{
				m_Entities = nativeList.AsDeferredJobArray(),
				m_TerrainHeightData = data,
				m_PrefabRefDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CompositionDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RW_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeHeightMap = edgeHeightMap
			};
			CalculateNodeGeometryJob jobData6 = new CalculateNodeGeometryJob
			{
				m_Entities = nativeList.AsDeferredJobArray(),
				m_PrefabRefDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CompositionDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OutsideConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_GeometryDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PlaceableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Edges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
				m_PrefabCompositionPieces = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionPiece_RO_BufferLookup, ref base.CheckedStateRef),
				m_StartNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RW_ComponentLookup, ref base.CheckedStateRef),
				m_EndNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RW_ComponentLookup, ref base.CheckedStateRef)
			};
			CalculateIntersectionGeometryJob jobData7 = new CalculateIntersectionGeometryJob
			{
				m_Entities = nativeList.AsDeferredJobArray(),
				m_TerrainHeightData = data,
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_StartGeometryDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EndGeometryDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Edges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_BufferedData = nativeList2
			};
			CopyNodeGeometryJob jobData8 = new CopyNodeGeometryJob
			{
				m_Entities = nativeList.AsDeferredJobArray(),
				m_BufferedData = nativeList2,
				m_StartNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RW_ComponentLookup, ref base.CheckedStateRef),
				m_EndNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RW_ComponentLookup, ref base.CheckedStateRef)
			};
			UpdateNodeGeometryJob jobData9 = new UpdateNodeGeometryJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_TerrainHeightData = data,
				m_NodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OrphanType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Orphan_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_StartNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EndNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_NodeGeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_NodeGeometry_RW_ComponentTypeHandle, ref base.CheckedStateRef)
			};
			JobHandle jobHandle = IJobParallelForDeferExtensions.Schedule(dependsOn: JobChunkExtensions.ScheduleParallel(dependsOn: JobHandle.CombineDependencies(job1: IJobExtensions.Schedule(jobData, outJobHandle), job0: IJobParallelForDeferExtensions.Schedule(dependsOn: JobHandle.CombineDependencies(JobChunkExtensions.ScheduleParallel(jobData2, query, base.Dependency), outJobHandle), jobData: jobData3, list: nativeList, innerloopBatchCount: 1)), jobData: jobData4, query: query), jobData: jobData5, list: nativeList, innerloopBatchCount: 1);
			JobHandle dependsOn = jobHandle;
			jobData6.m_IterationIndex = 0;
			while (jobData6.m_IterationIndex < 2)
			{
				dependsOn = jobData6.Schedule(nativeList, 1, dependsOn);
				jobData6.m_IterationIndex++;
			}
			JobHandle dependsOn2 = jobData7.Schedule(nativeList, 1, dependsOn);
			JobHandle jobHandle2 = jobData8.Schedule(nativeList, 16, dependsOn2);
			JobHandle jobHandle3 = JobChunkExtensions.ScheduleParallel(jobData9, query, jobHandle2);
			nativeList.Dispose(jobHandle2);
			nativeList2.Dispose(jobHandle2);
			edgeHeightMap.Dispose(jobHandle);
			m_TerrainSystem.AddCPUHeightReader(jobHandle3);
			base.Dependency = jobHandle3;
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
	public GeometrySystem()
	{
	}
}
