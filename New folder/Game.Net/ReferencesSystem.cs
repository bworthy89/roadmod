using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
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
public class ReferencesSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateNodeReferencesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		public BufferTypeHandle<ConnectedEdge> m_ConnectedEdgeType;

		public BufferLookup<ConnectedNode> m_Nodes;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<ConnectedEdge> bufferAccessor = chunk.GetBufferAccessor(ref m_ConnectedEdgeType);
			if (chunk.Has(ref m_DeletedType))
			{
				for (int i = 0; i < bufferAccessor.Length; i++)
				{
					Entity node = nativeArray[i];
					DynamicBuffer<ConnectedEdge> dynamicBuffer = bufferAccessor[i];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Entity edge = dynamicBuffer[j].m_Edge;
						CollectionUtils.RemoveValue(m_Nodes[edge], new ConnectedNode(node, 0.5f));
					}
				}
				return;
			}
			for (int k = 0; k < bufferAccessor.Length; k++)
			{
				DynamicBuffer<ConnectedEdge> dynamicBuffer2 = bufferAccessor[k];
				for (int l = 0; l < dynamicBuffer2.Length; l++)
				{
					Entity edge2 = dynamicBuffer2[l].m_Edge;
					if (m_UpdatedData.HasComponent(edge2))
					{
						dynamicBuffer2.RemoveAt(l--);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct ValidateConnectedNodesJob : IJobChunk
	{
		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		public BufferTypeHandle<ConnectedNode> m_ConnectedNodeType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<ConnectedNode> bufferAccessor = chunk.GetBufferAccessor(ref m_ConnectedNodeType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<ConnectedNode> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					Entity node = dynamicBuffer[j].m_Node;
					if (m_DeletedData.HasComponent(node))
					{
						dynamicBuffer.RemoveAt(j--);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateEdgeReferencesJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_EdgeChunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Edge> m_EdgeType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public BufferTypeHandle<ConnectedNode> m_ConnectedNodeType;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		public BufferLookup<ConnectedEdge> m_Edges;

		public NativeParallelMultiHashMap<Entity, ConnectedNodeValue> m_ConnectedNodes;

		public void Execute()
		{
			for (int i = 0; i < m_EdgeChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_EdgeChunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<Edge> nativeArray2 = archetypeChunk.GetNativeArray(ref m_EdgeType);
				BufferAccessor<ConnectedNode> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_ConnectedNodeType);
				if (archetypeChunk.Has(ref m_DeletedType))
				{
					for (int j = 0; j < nativeArray.Length; j++)
					{
						Entity edge = nativeArray[j];
						Edge edge2 = nativeArray2[j];
						DynamicBuffer<ConnectedNode> dynamicBuffer = bufferAccessor[j];
						DynamicBuffer<ConnectedEdge> buffer = m_Edges[edge2.m_Start];
						DynamicBuffer<ConnectedEdge> buffer2 = m_Edges[edge2.m_End];
						CollectionUtils.RemoveValue(buffer, new ConnectedEdge(edge));
						CollectionUtils.RemoveValue(buffer2, new ConnectedEdge(edge));
						for (int k = 0; k < dynamicBuffer.Length; k++)
						{
							CollectionUtils.RemoveValue(m_Edges[dynamicBuffer[k].m_Node], new ConnectedEdge(edge));
						}
					}
					continue;
				}
				for (int l = 0; l < nativeArray.Length; l++)
				{
					Entity edge3 = nativeArray[l];
					Edge edge4 = nativeArray2[l];
					DynamicBuffer<ConnectedNode> dynamicBuffer2 = bufferAccessor[l];
					DynamicBuffer<ConnectedEdge> buffer3 = m_Edges[edge4.m_Start];
					DynamicBuffer<ConnectedEdge> buffer4 = m_Edges[edge4.m_End];
					CollectionUtils.TryAddUniqueValue(buffer3, new ConnectedEdge(edge3));
					CollectionUtils.TryAddUniqueValue(buffer4, new ConnectedEdge(edge3));
					for (int m = 0; m < dynamicBuffer2.Length; m++)
					{
						ConnectedNode connectedNode = dynamicBuffer2[m];
						CollectionUtils.RemoveValue(m_Edges[connectedNode.m_Node], new ConnectedEdge(edge3));
						if (!m_DeletedData.HasComponent(connectedNode.m_Node))
						{
							m_ConnectedNodes.Add(connectedNode.m_Node, new ConnectedNodeValue(edge3, connectedNode.m_CurvePosition));
						}
					}
				}
			}
		}
	}

	private struct ConnectedNodeValue
	{
		public Entity m_Edge;

		public float m_CurvePosition;

		public ConnectedNodeValue(Entity edge, float curvePosition)
		{
			m_Edge = edge;
			m_CurvePosition = curvePosition;
		}
	}

	[BurstCompile]
	private struct RationalizeConnectedNodesJob : IJobParallelForDefer
	{
		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct LineComparer : IComparer<Line2.Segment>
		{
			public int Compare(Line2.Segment x, Line2.Segment y)
			{
				return math.csum(math.select(0, math.select(new int4(-8, -4, -2, -1), new int4(8, 4, 2, 1), x.ab > y.ab), x.ab != y.ab));
			}
		}

		[ReadOnly]
		public NativeArray<ArchetypeChunk> m_EdgeChunks;

		[ReadOnly]
		public NativeParallelMultiHashMap<Entity, ConnectedNodeValue> m_ConnectedNodes;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Edge> m_EdgeType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<Standalone> m_StandaloneData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<LocalConnectData> m_PrefabLocalConnectData;

		[ReadOnly]
		public ComponentLookup<NetData> m_PrefabNetData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<ServiceUpgradeData> m_PrefabServiceUpgradeData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_Edges;

		public BufferTypeHandle<ConnectedNode> m_ConnectedNodeType;

		public void Execute(int index)
		{
			ArchetypeChunk archetypeChunk = m_EdgeChunks[index];
			if (archetypeChunk.Has(ref m_DeletedType))
			{
				return;
			}
			NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
			NativeArray<Edge> nativeArray2 = archetypeChunk.GetNativeArray(ref m_EdgeType);
			NativeArray<Curve> nativeArray3 = archetypeChunk.GetNativeArray(ref m_CurveType);
			NativeArray<Owner> nativeArray4 = archetypeChunk.GetNativeArray(ref m_OwnerType);
			NativeArray<Temp> nativeArray5 = archetypeChunk.GetNativeArray(ref m_TempType);
			NativeArray<PrefabRef> nativeArray6 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<ConnectedNode> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_ConnectedNodeType);
			NativeParallelHashMap<Entity, Bounds1> curveBoundsMap = default(NativeParallelHashMap<Entity, Bounds1>);
			NativeParallelHashMap<Entity, float2> nodePosMap = default(NativeParallelHashMap<Entity, float2>);
			NativeList<Line2.Segment> lineBuffer = default(NativeList<Line2.Segment>);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				Entity entity = nativeArray[i];
				Edge edge = nativeArray2[i];
				Curve curve = nativeArray3[i];
				PrefabRef prefabRef = nativeArray6[i];
				DynamicBuffer<ConnectedNode> dynamicBuffer = bufferAccessor[i];
				Entity entity2 = Entity.Null;
				if (nativeArray4.Length != 0)
				{
					entity2 = nativeArray4[i].m_Owner;
				}
				Entity entity3 = Entity.Null;
				if (nativeArray5.Length != 0)
				{
					entity3 = nativeArray5[i].m_Original;
				}
				float num = 0f;
				if (m_PrefabNetGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
				{
					num = componentData.m_DefaultWidth * 0.5f;
				}
				for (int num2 = dynamicBuffer.Length - 1; num2 >= 0; num2--)
				{
					ConnectedNode connectedNode = dynamicBuffer[num2];
					LocalConnectData localConnectData;
					float3 @float;
					float num4;
					Game.Prefabs.BuildingFlags allowDirections;
					float3 allowForward;
					if (!m_DeletedData.HasComponent(connectedNode.m_Node))
					{
						PrefabRef prefabRef2 = m_PrefabRefData[connectedNode.m_Node];
						if (m_PrefabLocalConnectData.HasComponent(prefabRef2.m_Prefab))
						{
							localConnectData = m_PrefabLocalConnectData[prefabRef2.m_Prefab];
							float num3 = 0f;
							if ((localConnectData.m_Flags & LocalConnectFlags.ChooseSides) != 0)
							{
								num3 = m_PrefabNetGeometryData[prefabRef2.m_Prefab].m_DefaultWidth * 0.5f + 0.1f;
							}
							float t = ClampCurvePosition(entity, edge, curve, connectedNode.m_CurvePosition, ref curveBoundsMap, ref nodePosMap, ref lineBuffer);
							@float = MathUtils.Position(curve.m_Bezier, t);
							float3 position = m_NodeData[connectedNode.m_Node].m_Position;
							num4 = math.distance(@float, position);
							Entity entity4 = Entity.Null;
							allowDirections = (Game.Prefabs.BuildingFlags)0u;
							allowForward = default(float3);
							Game.Prefabs.BuildingFlags allowDirections2;
							float3 allowForward2;
							if (m_OwnerData.TryGetComponent(connectedNode.m_Node, out var componentData2))
							{
								entity4 = componentData2.m_Owner;
								if (entity2 != entity4 && (GetElevationFlags(entity, edge, componentData).m_General & CompositionFlags.General.Tunnel) == 0 && (!AllowConnection(entity4, new Line3.Segment(position, @float), out allowDirections, out allowForward) || (entity2 != Entity.Null && !AllowConnection(entity2, new Line3.Segment(@float, position), out allowDirections2, out allowForward2))))
								{
									goto IL_0569;
								}
							}
							if ((localConnectData.m_Flags & LocalConnectFlags.ChooseBest) != 0)
							{
								bool flag = false;
								if (m_ConnectedNodes.TryGetFirstValue(connectedNode.m_Node, out var item, out var it))
								{
									while (true)
									{
										Entity entity5 = Entity.Null;
										if (m_TempData.TryGetComponent(item.m_Edge, out var componentData3))
										{
											entity5 = componentData3.m_Original;
										}
										if (item.m_Edge == entity || item.m_Edge == entity3 || entity5 == entity)
										{
											flag = true;
										}
										else
										{
											Edge edge2 = m_EdgeData[item.m_Edge];
											Curve curve2 = m_CurveData[item.m_Edge];
											PrefabRef prefabRef3 = m_PrefabRefData[item.m_Edge];
											float num5 = 0f;
											if (m_PrefabNetGeometryData.TryGetComponent(prefabRef3.m_Prefab, out var componentData4))
											{
												num5 = componentData4.m_DefaultWidth * 0.5f;
											}
											float t2 = ClampCurvePosition(item.m_Edge, edge2, curve2, item.m_CurvePosition, ref curveBoundsMap, ref nodePosMap, ref lineBuffer);
											float3 float2 = MathUtils.Position(curve2.m_Bezier, t2);
											float num6 = math.distance(float2, position);
											float num7 = num4 - num - (num6 - num5);
											if ((localConnectData.m_Flags & LocalConnectFlags.ChooseSides) != 0 && num7 >= 0f && num7 - num3 <= 0f && (AreNeighbors(edge, edge2) || (GetOriginals(edge, out var originals) && AreNeighbors(originals, edge2)) || (GetOriginals(edge2, out var originals2) && AreNeighbors(edge, originals2))))
											{
												num7 -= math.sqrt(num * num + num3 * num3) - num;
											}
											if ((num7 > 0f || (num7 == 0f && !flag)) && (!(entity4 != Entity.Null) || !(entity2 != entity4) || (GetElevationFlags(item.m_Edge, edge2, componentData4).m_General & CompositionFlags.General.Tunnel) != 0 || (AllowConnection(entity4, new Line3.Segment(position, float2), out allowDirections2, out allowForward2) && (!(entity2 != Entity.Null) || AllowConnection(entity2, new Line3.Segment(float2, position), out allowDirections2, out allowForward2)))) && ValidateConnection(item.m_Edge, connectedNode.m_Node, entity5, edge2, localConnectData, float2, num6, num5, allowDirections, allowForward))
											{
												break;
											}
										}
										if (m_ConnectedNodes.TryGetNextValue(out item, ref it))
										{
											continue;
										}
										goto IL_0548;
									}
									goto IL_0569;
								}
							}
							goto IL_0548;
						}
					}
					goto IL_0569;
					IL_0569:
					dynamicBuffer.RemoveAt(num2);
					continue;
					IL_0548:
					if (ValidateConnection(entity, connectedNode.m_Node, entity3, edge, localConnectData, @float, num4, num, allowDirections, allowForward))
					{
						continue;
					}
					goto IL_0569;
				}
			}
			if (curveBoundsMap.IsCreated)
			{
				curveBoundsMap.Dispose();
			}
			if (nodePosMap.IsCreated)
			{
				nodePosMap.Dispose();
			}
			if (lineBuffer.IsCreated)
			{
				lineBuffer.Dispose();
			}
		}

		private CompositionFlags GetElevationFlags(Entity entity, Edge edge, NetGeometryData prefabGeometryData)
		{
			m_ElevationData.TryGetComponent(edge.m_Start, out var componentData);
			m_ElevationData.TryGetComponent(entity, out var componentData2);
			m_ElevationData.TryGetComponent(edge.m_End, out var componentData3);
			return NetCompositionHelpers.GetElevationFlags(componentData, componentData2, componentData3, prefabGeometryData);
		}

		private float ClampCurvePosition(Entity entity, Edge edge, Curve curve, float curvePos, ref NativeParallelHashMap<Entity, Bounds1> curveBoundsMap, ref NativeParallelHashMap<Entity, float2> nodePosMap, ref NativeList<Line2.Segment> lineBuffer)
		{
			Bounds1 item = default(Bounds1);
			if (curveBoundsMap.IsCreated)
			{
				if (curveBoundsMap.TryGetValue(entity, out item))
				{
					return MathUtils.Clamp(curvePos, item);
				}
			}
			else
			{
				curveBoundsMap = new NativeParallelHashMap<Entity, Bounds1>(100, Allocator.Temp);
			}
			if (curve.m_Length >= 0.2f)
			{
				float2 nodePosition = GetNodePosition(edge.m_Start, ref nodePosMap, ref lineBuffer);
				float2 nodePosition2 = GetNodePosition(edge.m_End, ref nodePosMap, ref lineBuffer);
				MathUtils.Distance(curve.m_Bezier.xz, nodePosition, out item.min);
				MathUtils.Distance(curve.m_Bezier.xz, nodePosition2, out item.max);
				float num = 0.1f / curve.m_Length;
				item.min += num;
				item.max -= num;
				if (item.max < item.min)
				{
					item.min = (item.max = MathUtils.Center(item));
				}
			}
			else
			{
				item = new Bounds1(0.5f, 0.5f);
			}
			curveBoundsMap.Add(entity, item);
			return MathUtils.Clamp(curvePos, item);
		}

		private float2 GetNodePosition(Entity entity, ref NativeParallelHashMap<Entity, float2> nodePosMap, ref NativeList<Line2.Segment> lineBuffer)
		{
			if (nodePosMap.IsCreated)
			{
				if (nodePosMap.TryGetValue(entity, out var item))
				{
					return item;
				}
			}
			else
			{
				nodePosMap = new NativeParallelHashMap<Entity, float2>(100, Allocator.Temp);
			}
			Node node = m_NodeData[entity];
			if (!m_StandaloneData.HasComponent(entity))
			{
				PrefabRef prefabRef = m_PrefabRefData[entity];
				m_PrefabNetData.TryGetComponent(prefabRef.m_Prefab, out var componentData);
				float3 @float = default(float3);
				float3 position = node.m_Position;
				int num = 0;
				if (lineBuffer.IsCreated)
				{
					lineBuffer.Clear();
				}
				EdgeIterator edgeIterator = new EdgeIterator(Entity.Null, entity, m_Edges, m_EdgeData, m_TempData, m_HiddenData);
				EdgeIteratorValue value;
				while (edgeIterator.GetNext(out value))
				{
					Curve curve = m_CurveData[value.m_Edge];
					PrefabRef prefabRef2 = m_PrefabRefData[value.m_Edge];
					m_PrefabNetData.TryGetComponent(prefabRef2.m_Prefab, out var componentData2);
					if ((componentData.m_RequiredLayers & componentData2.m_RequiredLayers) == 0)
					{
						continue;
					}
					float3 float2;
					float2 value2;
					if (value.m_End)
					{
						float2 = curve.m_Bezier.d - position;
						value2 = -MathUtils.EndTangent(curve.m_Bezier).xz;
					}
					else
					{
						float2 = curve.m_Bezier.a - position;
						value2 = MathUtils.StartTangent(curve.m_Bezier).xz;
					}
					@float += float2;
					num++;
					if (MathUtils.TryNormalize(ref value2))
					{
						if (!lineBuffer.IsCreated)
						{
							lineBuffer = new NativeList<Line2.Segment>(10, Allocator.Temp);
						}
						lineBuffer.Add(new Line2.Segment(float2.xz, value2));
					}
				}
				if (num > 0)
				{
					node.m_Position = position + @float / num;
					@float = default(float3);
					num = 0;
				}
				if (lineBuffer.IsCreated && lineBuffer.Length >= 2)
				{
					lineBuffer.Sort(default(LineComparer));
					for (int i = 1; i < lineBuffer.Length; i++)
					{
						Line2.Segment segment = lineBuffer[i];
						for (int j = 0; j < i; j++)
						{
							Line2.Segment segment2 = lineBuffer[j];
							float x = math.dot(segment.b, segment2.b);
							float3 float3 = default(float3);
							if (math.abs(x) > 0.999f)
							{
								float3.xy = segment.a + segment2.a;
								float3.z = 2f;
							}
							else
							{
								float2 float4 = math.distance(segment.a, segment2.a) * new float2(math.abs(x) - 1f, 1f - math.abs(x));
								Line2.Segment segment3 = new Line2.Segment(segment.a - segment.b * float4.x, segment.a - segment.b * float4.y);
								Line2.Segment segment4 = new Line2.Segment(segment2.a - segment2.b * float4.x, segment2.a - segment2.b * float4.y);
								MathUtils.Distance(segment3, segment4, out var t);
								float3.xy = MathUtils.Position(segment3, t.x) + MathUtils.Position(segment4, t.y);
								float3.z = 2f;
							}
							float num2 = 1.01f - math.abs(x);
							@float += float3 * num2;
							num++;
						}
					}
					if (num > 0)
					{
						node.m_Position.xz = position.xz + @float.xy / @float.z;
					}
				}
			}
			nodePosMap.Add(entity, node.m_Position.xz);
			return node.m_Position.xz;
		}

		private bool AreNeighbors(Edge edge1, Edge edge2)
		{
			if (!(edge1.m_Start == edge2.m_Start) && !(edge1.m_End == edge2.m_Start) && !(edge1.m_Start == edge2.m_End))
			{
				return edge1.m_End == edge2.m_End;
			}
			return true;
		}

		private bool GetOriginals(Edge edge, out Edge originals)
		{
			if (m_TempData.TryGetComponent(edge.m_Start, out var componentData) && m_TempData.TryGetComponent(edge.m_End, out var componentData2))
			{
				originals.m_Start = componentData.m_Original;
				originals.m_End = componentData2.m_Original;
				return true;
			}
			originals = default(Edge);
			return false;
		}

		private bool ValidateConnection(Entity entity, Entity node, Entity original, Edge edge, LocalConnectData localConnectData, float3 edgePosition, float nodeDistance, float size, Game.Prefabs.BuildingFlags allowDirections, float3 allowForward)
		{
			EdgeIterator edgeIterator = new EdgeIterator(Entity.Null, node, m_Edges, m_EdgeData, m_TempData, m_HiddenData, (localConnectData.m_Flags & LocalConnectFlags.ChooseBest) != 0);
			int num = 0;
			EdgeIteratorValue value;
			while (edgeIterator.GetNext(out value))
			{
				if (value.m_Middle)
				{
					if (value.m_Edge != entity && value.m_Edge != original)
					{
						if ((localConnectData.m_Flags & LocalConnectFlags.ChooseSides) == 0)
						{
							return false;
						}
						Edge edge2 = m_EdgeData[value.m_Edge];
						if (!AreNeighbors(edge, edge2) && (!GetOriginals(edge, out var originals) || !AreNeighbors(originals, edge2)))
						{
							return false;
						}
					}
					continue;
				}
				Edge edge3 = m_EdgeData[value.m_Edge];
				Entity entity2 = (value.m_End ? edge3.m_Start : edge3.m_End);
				if (entity2 == edge.m_Start || entity2 == edge.m_End)
				{
					return false;
				}
				if (++num >= 2 && (localConnectData.m_Flags & LocalConnectFlags.RequireDeadend) != 0)
				{
					return false;
				}
				float3 position = m_NodeData[entity2].m_Position;
				if (math.distance(edgePosition, position) < nodeDistance)
				{
					return false;
				}
				Curve curve = m_CurveData[value.m_Edge];
				float2 @float = math.normalizesafe(value.m_End ? MathUtils.EndTangent(curve.m_Bezier).xz : (-MathUtils.StartTangent(curve.m_Bezier).xz));
				float2 float2 = (value.m_End ? curve.m_Bezier.d.xz : curve.m_Bezier.a.xz) - @float * size;
				float2 y = math.normalizesafe(edgePosition.xz - float2);
				if (math.dot(@float, y) < 0.70710677f)
				{
					return false;
				}
				if (!allowForward.Equals(default(float3)))
				{
					float2 float3 = math.normalizesafe(allowForward.xz);
					float2 float4 = new float2(math.dot(@float, float3), math.dot(@float, MathUtils.Left(float3)));
					bool4 @bool = new float4(float4, -float4) >= 0.70710677f;
					bool4 bool2 = new bool4(x: true, (allowDirections & Game.Prefabs.BuildingFlags.RightAccess) != 0, (allowDirections & Game.Prefabs.BuildingFlags.BackAccess) != 0, (allowDirections & Game.Prefabs.BuildingFlags.LeftAccess) != 0);
					if (!math.any(@bool & bool2))
					{
						return false;
					}
				}
			}
			PrefabRef prefabRef = m_PrefabRefData[entity];
			PrefabRef prefabRef2 = m_PrefabRefData[node];
			if ((m_PrefabServiceUpgradeData.HasComponent(prefabRef.m_Prefab) || m_PrefabServiceUpgradeData.HasComponent(prefabRef2.m_Prefab)) && GetTopOwner(entity) != GetTopOwner(node))
			{
				return false;
			}
			return true;
		}

		private Entity GetTopOwner(Entity entity)
		{
			while (true)
			{
				if (m_TempData.TryGetComponent(entity, out var componentData) && componentData.m_Original != Entity.Null)
				{
					entity = componentData.m_Original;
				}
				if (!m_OwnerData.TryGetComponent(entity, out var componentData2))
				{
					break;
				}
				entity = componentData2.m_Owner;
			}
			return entity;
		}

		private bool AllowConnection(Entity owner, Line3.Segment line, out Game.Prefabs.BuildingFlags allowDirections, out float3 allowForward)
		{
			allowDirections = (Game.Prefabs.BuildingFlags)0u;
			allowForward = default(float3);
			Owner componentData;
			while (m_OwnerData.TryGetComponent(owner, out componentData) && !m_BuildingData.HasComponent(owner))
			{
				owner = componentData.m_Owner;
			}
			if (!m_PrefabRefData.TryGetComponent(owner, out var componentData2) || !m_TransformData.TryGetComponent(owner, out var componentData3))
			{
				return true;
			}
			if (!m_PrefabBuildingData.TryGetComponent(componentData2.m_Prefab, out var componentData4))
			{
				return true;
			}
			float2 size = (float2)componentData4.m_LotSize * 8f;
			Quad2 xz = ObjectUtils.CalculateBaseCorners(componentData3.m_Position, componentData3.m_Rotation, size).xz;
			if ((componentData4.m_Flags & Game.Prefabs.BuildingFlags.LeftAccess) == 0 && MathUtils.Intersect(line.xz, xz.bc, out var t))
			{
				return false;
			}
			if ((componentData4.m_Flags & Game.Prefabs.BuildingFlags.BackAccess) == 0 && MathUtils.Intersect(line.xz, xz.cd, out t))
			{
				return false;
			}
			if ((componentData4.m_Flags & Game.Prefabs.BuildingFlags.RightAccess) == 0 && MathUtils.Intersect(line.xz, xz.da, out t))
			{
				return false;
			}
			allowDirections = componentData4.m_Flags;
			allowForward = math.forward(componentData3.m_Rotation);
			return true;
		}
	}

	[BurstCompile]
	private struct AddConnectedNodeReferencesJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_EdgeChunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> m_DeletedType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public BufferTypeHandle<ConnectedNode> m_ConnectedNodeType;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		public BufferLookup<ConnectedEdge> m_Edges;

		public void Execute()
		{
			for (int i = 0; i < m_EdgeChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_EdgeChunks[i];
				if (archetypeChunk.Has(ref m_DeletedType))
				{
					continue;
				}
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				BufferAccessor<ConnectedNode> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_ConnectedNodeType);
				bool flag = archetypeChunk.Has(ref m_TempType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity edge = nativeArray[j];
					DynamicBuffer<ConnectedNode> dynamicBuffer = bufferAccessor[j];
					for (int k = 0; k < dynamicBuffer.Length; k++)
					{
						Entity node = dynamicBuffer[k].m_Node;
						if (!flag || m_TempData.HasComponent(node))
						{
							CollectionUtils.TryAddUniqueValue(m_Edges[node], new ConnectedEdge(edge));
						}
					}
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Deleted> __Game_Common_Deleted_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

		public BufferTypeHandle<ConnectedEdge> __Game_Net_ConnectedEdge_RW_BufferTypeHandle;

		public BufferLookup<ConnectedNode> __Game_Net_ConnectedNode_RW_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		public BufferTypeHandle<ConnectedNode> __Game_Net_ConnectedNode_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<ConnectedNode> __Game_Net_ConnectedNode_RO_BufferTypeHandle;

		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RW_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Elevation> __Game_Net_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Standalone> __Game_Net_Standalone_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Hidden> __Game_Tools_Hidden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LocalConnectData> __Game_Prefabs_LocalConnectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceUpgradeData> __Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Deleted_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Deleted>(isReadOnly: true);
			__Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RW_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedEdge>();
			__Game_Net_ConnectedNode_RW_BufferLookup = state.GetBufferLookup<ConnectedNode>();
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Net_ConnectedNode_RW_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedNode>();
			__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(isReadOnly: true);
			__Game_Net_ConnectedNode_RO_BufferTypeHandle = state.GetBufferTypeHandle<ConnectedNode>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RW_BufferLookup = state.GetBufferLookup<ConnectedEdge>();
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentLookup = state.GetComponentLookup<Elevation>(isReadOnly: true);
			__Game_Net_Standalone_RO_ComponentLookup = state.GetComponentLookup<Standalone>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentLookup = state.GetComponentLookup<Hidden>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_LocalConnectData_RO_ComponentLookup = state.GetComponentLookup<LocalConnectData>(isReadOnly: true);
			__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup = state.GetComponentLookup<ServiceUpgradeData>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
		}
	}

	private EntityQuery m_EdgeQuery;

	private EntityQuery m_NodeQuery;

	private EntityQuery m_TempEdgeQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EdgeQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Edge>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_NodeQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Node>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_TempEdgeQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Edge>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_NodeQuery.IsEmptyIgnoreFilter)
		{
			JobHandle dependency = JobChunkExtensions.Schedule(new UpdateNodeReferencesJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectedEdgeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ConnectedEdge_RW_BufferTypeHandle, ref base.CheckedStateRef),
				m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedNode_RW_BufferLookup, ref base.CheckedStateRef)
			}, m_NodeQuery, base.Dependency);
			base.Dependency = dependency;
			if (!m_TempEdgeQuery.IsEmptyIgnoreFilter)
			{
				JobHandle dependency2 = JobChunkExtensions.ScheduleParallel(new ValidateConnectedNodesJob
				{
					m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
					m_ConnectedNodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ConnectedNode_RW_BufferTypeHandle, ref base.CheckedStateRef)
				}, m_TempEdgeQuery, base.Dependency);
				base.Dependency = dependency2;
			}
		}
		if (!m_EdgeQuery.IsEmptyIgnoreFilter)
		{
			JobHandle outJobHandle;
			NativeList<ArchetypeChunk> nativeList = m_EdgeQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
			NativeParallelMultiHashMap<Entity, ConnectedNodeValue> connectedNodes = new NativeParallelMultiHashMap<Entity, ConnectedNodeValue>(32, Allocator.TempJob);
			UpdateEdgeReferencesJob jobData = new UpdateEdgeReferencesJob
			{
				m_EdgeChunks = nativeList,
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ConnectedNodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ConnectedNode_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Edges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RW_BufferLookup, ref base.CheckedStateRef),
				m_ConnectedNodes = connectedNodes
			};
			RationalizeConnectedNodesJob jobData2 = new RationalizeConnectedNodesJob
			{
				m_EdgeChunks = nativeList.AsDeferredJobArray(),
				m_ConnectedNodes = connectedNodes,
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_StandaloneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Standalone_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabLocalConnectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LocalConnectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabServiceUpgradeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Edges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_ConnectedNodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ConnectedNode_RW_BufferTypeHandle, ref base.CheckedStateRef)
			};
			AddConnectedNodeReferencesJob jobData3 = new AddConnectedNodeReferencesJob
			{
				m_EdgeChunks = nativeList,
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_DeletedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ConnectedNodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ConnectedNode_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Edges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RW_BufferLookup, ref base.CheckedStateRef)
			};
			JobHandle jobHandle = IJobParallelForDeferExtensions.Schedule(dependsOn: IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(outJobHandle, base.Dependency)), jobData: jobData2, list: nativeList, innerloopBatchCount: 1);
			JobHandle jobHandle2 = IJobExtensions.Schedule(jobData3, jobHandle);
			nativeList.Dispose(jobHandle2);
			connectedNodes.Dispose(jobHandle);
			base.Dependency = jobHandle2;
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
	public ReferencesSystem()
	{
	}
}
