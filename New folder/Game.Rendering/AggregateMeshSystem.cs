using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Serialization;
using Game.Tools;
using Game.UI;
using TMPro;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class AggregateMeshSystem : GameSystemBase, IPreDeserialize
{
	private struct MaterialData
	{
		public Material m_Material;

		public bool m_IsUnderground;

		public bool m_HasMesh;
	}

	private class MeshData
	{
		public JobHandle m_DataDependencies;

		public Material m_OriginalMaterial;

		public List<MaterialData> m_Materials;

		public Mesh m_Mesh;

		public Mesh.MeshDataArray m_MeshData;

		public bool m_MeshDirty;

		public bool m_HasMeshData;

		public bool m_HasMesh;
	}

	private struct MaterialUpdate
	{
		public Entity m_Entity;

		public int m_Material;
	}

	[BurstCompile]
	private struct UpdateLabelPositionsJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<LabelExtents> m_LabelExtentsType;

		[ReadOnly]
		public BufferTypeHandle<AggregateElement> m_AggregateElementType;

		public BufferTypeHandle<LabelPosition> m_LabelPositionType;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_NetCompositionData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<LabelExtents> nativeArray = chunk.GetNativeArray(ref m_LabelExtentsType);
			BufferAccessor<AggregateElement> bufferAccessor = chunk.GetBufferAccessor(ref m_AggregateElementType);
			BufferAccessor<LabelPosition> bufferAccessor2 = chunk.GetBufferAccessor(ref m_LabelPositionType);
			NativeList<float> redundancyBuffer = default(NativeList<float>);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				LabelExtents labelExtents = nativeArray[i];
				DynamicBuffer<AggregateElement> aggregateElements = bufferAccessor[i];
				DynamicBuffer<LabelPosition> labelPositions = bufferAccessor2[i];
				labelPositions.Clear();
				if (aggregateElements.Length <= 0)
				{
					continue;
				}
				if (!redundancyBuffer.IsCreated)
				{
					redundancyBuffer = new NativeList<float>(32, Allocator.Temp);
				}
				int startIndex = 0;
				int j = 1;
				Edge edge = m_EdgeData[aggregateElements[0].m_Edge];
				for (; j < aggregateElements.Length; j++)
				{
					Edge edge2 = m_EdgeData[aggregateElements[j].m_Edge];
					if (edge2.m_Start == edge.m_End || edge2.m_Start == edge.m_Start)
					{
						if (m_ConnectedEdges[edge2.m_Start].Length > 2)
						{
							AddLabels(startIndex, j, labelExtents, redundancyBuffer, aggregateElements, labelPositions);
							startIndex = j;
						}
					}
					else if (edge2.m_End == edge.m_End || edge2.m_End == edge.m_Start)
					{
						if (m_ConnectedEdges[edge2.m_End].Length > 2)
						{
							AddLabels(startIndex, j, labelExtents, redundancyBuffer, aggregateElements, labelPositions);
							startIndex = j;
						}
					}
					else
					{
						AddLabels(startIndex, j, labelExtents, redundancyBuffer, aggregateElements, labelPositions);
						startIndex = j;
					}
					edge = edge2;
				}
				AddLabels(startIndex, j, labelExtents, redundancyBuffer, aggregateElements, labelPositions);
				float num = 0f;
				for (int k = 0; k < redundancyBuffer.Length; k++)
				{
					num += redundancyBuffer[k];
				}
				float num2 = (math.max(1f, math.round(num)) - num) * 0.5f;
				float num3 = 0f;
				for (int l = 0; l < redundancyBuffer.Length; l++)
				{
					float num4 = redundancyBuffer[l];
					num2 += num4;
					if (num2 < 0.5f)
					{
						LabelPosition value = labelPositions[l];
						num3 += num4;
						if (num3 < 0.25f)
						{
							value.m_MaxScale *= 0.25f;
						}
						else
						{
							value.m_MaxScale *= 0.5f;
							num3 = ((!(num4 < 0.25f)) ? 0f : (num3 - 0.5f));
						}
						labelPositions[l] = value;
					}
					else
					{
						num2 -= 1f;
						num3 = 0f;
					}
				}
				redundancyBuffer.Clear();
			}
			if (redundancyBuffer.IsCreated)
			{
				redundancyBuffer.Dispose();
			}
		}

		private void AddLabels(int startIndex, int endIndex, LabelExtents labelExtents, NativeList<float> redundancyBuffer, DynamicBuffer<AggregateElement> aggregateElements, DynamicBuffer<LabelPosition> labelPositions)
		{
			float num = 0f;
			for (int i = startIndex; i < endIndex; i++)
			{
				Entity edge = aggregateElements[i].m_Edge;
				Curve curve = m_CurveData[edge];
				Composition composition = m_CompositionData[edge];
				float num2 = math.sqrt(math.max(1f, m_NetCompositionData[composition.m_Edge].m_Width));
				num += curve.m_Length / num2;
			}
			float num3 = 100f;
			int num4 = math.clamp(Mathf.RoundToInt(num / num3), 1, endIndex - startIndex);
			num3 = num / (float)num4;
			float value = num3 / 100f;
			float num5 = 0f;
			float num6 = 0f;
			int num7 = -1;
			LabelPosition elem = default(LabelPosition);
			for (int j = startIndex; j < endIndex; j++)
			{
				Entity edge2 = aggregateElements[j].m_Edge;
				Curve curve2 = m_CurveData[edge2];
				Composition composition2 = m_CompositionData[edge2];
				float num8 = math.sqrt(math.max(1f, m_NetCompositionData[composition2.m_Edge].m_Width));
				float num9 = num5 + curve2.m_Length / num8;
				if (num7 != -1 && num9 - num3 > num3 - num5)
				{
					Entity edge3 = aggregateElements[num7].m_Edge;
					Curve curve3 = m_CurveData[edge3];
					Composition composition3 = m_CompositionData[edge3];
					NetCompositionData netCompositionData = m_NetCompositionData[composition3.m_Edge];
					elem.m_Curve = curve3.m_Bezier;
					elem.m_ElementIndex = j;
					elem.m_HalfLength = curve3.m_Length * 0.5f;
					elem.m_MaxScale = netCompositionData.m_Width * 0.5f / math.max(1f, math.max(0f - labelExtents.m_Bounds.min.y, labelExtents.m_Bounds.max.y));
					elem.m_IsUnderground = (netCompositionData.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0;
					labelPositions.Add(elem);
					redundancyBuffer.Add(in value);
					num5 -= num3;
					num9 -= num3;
					num6 = 0f;
					num7 = -1;
				}
				float num10 = math.lerp(num5, num9, 0.5f);
				float num11 = curve2.m_Length * num8 / math.max(1f, num3 + math.abs(num10 - num3 * 0.5f));
				num5 = num9;
				if (num11 > num6)
				{
					num6 = num11;
					num7 = j;
				}
			}
			if (num7 != -1)
			{
				Entity edge4 = aggregateElements[num7].m_Edge;
				Curve curve4 = m_CurveData[edge4];
				Composition composition4 = m_CompositionData[edge4];
				NetCompositionData netCompositionData2 = m_NetCompositionData[composition4.m_Edge];
				LabelPosition elem2 = default(LabelPosition);
				elem2.m_Curve = curve4.m_Bezier;
				elem2.m_ElementIndex = num7;
				elem2.m_HalfLength = curve4.m_Length * 0.5f;
				elem2.m_MaxScale = netCompositionData2.m_Width * 0.5f / math.max(1f, math.max(0f - labelExtents.m_Bounds.min.y, labelExtents.m_Bounds.max.y));
				elem2.m_IsUnderground = (netCompositionData2.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0;
				labelPositions.Add(elem2);
				redundancyBuffer.Add(in value);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FillTempMapJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Aggregated> m_AggregatedType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		public NativeParallelMultiHashMap<Entity, TempValue> m_TempMap;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Aggregated> nativeArray2 = chunk.GetNativeArray(ref m_AggregatedType);
			NativeArray<Temp> nativeArray3 = chunk.GetNativeArray(ref m_TempType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Aggregated aggregated = nativeArray2[i];
				Temp temp = nativeArray3[i];
				if (aggregated.m_Aggregate != Entity.Null && temp.m_Original != Entity.Null && (temp.m_Flags & (TempFlags.Modify | TempFlags.Replace | TempFlags.Upgrade)) != 0)
				{
					m_TempMap.Add(aggregated.m_Aggregate, new TempValue
					{
						m_TempEntity = nativeArray[i],
						m_OriginalEntity = temp.m_Original
					});
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TempValue
	{
		public Entity m_TempEntity;

		public Entity m_OriginalEntity;
	}

	[BurstCompile]
	private struct UpdateArrowPositionsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<AggregateElement> m_AggregateElementType;

		public BufferTypeHandle<ArrowPosition> m_ArrowPositionType;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_NetCompositionData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<NetCompositionCarriageway> m_NetCompositionCarriageways;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public NativeParallelMultiHashMap<Entity, TempValue> m_TempMap;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<AggregateElement> bufferAccessor = chunk.GetBufferAccessor(ref m_AggregateElementType);
			BufferAccessor<ArrowPosition> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ArrowPositionType);
			NativeParallelHashMap<Entity, Entity> edgeMap = default(NativeParallelHashMap<Entity, Entity>);
			NativeList<AggregateElement> nativeList = default(NativeList<AggregateElement>);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<AggregateElement> dynamicBuffer = bufferAccessor[i];
				DynamicBuffer<ArrowPosition> arrowPositions = bufferAccessor2[i];
				arrowPositions.Clear();
				if (dynamicBuffer.Length <= 0)
				{
					continue;
				}
				UpdateEdgeMap(nativeArray[i], ref edgeMap);
				ProcessElements(dynamicBuffer.AsNativeArray(), arrowPositions, edgeMap);
				if (!edgeMap.IsCreated || edgeMap.IsEmpty)
				{
					continue;
				}
				if (nativeList.IsCreated)
				{
					nativeList.Clear();
				}
				else
				{
					nativeList = new NativeList<AggregateElement>(32, Allocator.Temp);
				}
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					if (edgeMap.TryGetValue(dynamicBuffer[j].m_Edge, out var item))
					{
						nativeList.Add(new AggregateElement(item));
					}
					else if (!nativeList.IsEmpty)
					{
						ProcessElements(nativeList.AsArray(), arrowPositions, default(NativeParallelHashMap<Entity, Entity>));
						nativeList.Clear();
					}
				}
				if (!nativeList.IsEmpty)
				{
					ProcessElements(nativeList.AsArray(), arrowPositions, default(NativeParallelHashMap<Entity, Entity>));
				}
			}
			if (edgeMap.IsCreated)
			{
				edgeMap.Dispose();
			}
			if (nativeList.IsCreated)
			{
				nativeList.Dispose();
			}
		}

		private void UpdateEdgeMap(Entity aggregate, ref NativeParallelHashMap<Entity, Entity> edgeMap)
		{
			if (m_TempMap.TryGetFirstValue(aggregate, out var item, out var it))
			{
				if (edgeMap.IsCreated)
				{
					edgeMap.Clear();
				}
				else
				{
					edgeMap = new NativeParallelHashMap<Entity, Entity>(32, Allocator.Temp);
				}
				do
				{
					edgeMap.TryAdd(item.m_OriginalEntity, item.m_TempEntity);
				}
				while (m_TempMap.TryGetNextValue(out item, ref it));
			}
			else if (edgeMap.IsCreated)
			{
				edgeMap.Clear();
			}
		}

		private void ProcessElements(NativeArray<AggregateElement> aggregateElements, DynamicBuffer<ArrowPosition> arrowPositions, NativeParallelHashMap<Entity, Entity> edgeMap)
		{
			int startIndex = 0;
			int i = 1;
			Edge edge = m_EdgeData[aggregateElements[0].m_Edge];
			for (; i < aggregateElements.Length; i++)
			{
				Edge edge2 = m_EdgeData[aggregateElements[i].m_Edge];
				if (edge2.m_Start == edge.m_End || edge2.m_Start == edge.m_Start)
				{
					DynamicBuffer<ConnectedEdge> connectedEdges = m_ConnectedEdges[edge2.m_Start];
					if (CompositionChange(connectedEdges, edge2.m_Start == edge.m_Start))
					{
						AddArrows(startIndex, i, aggregateElements, arrowPositions, edgeMap);
						startIndex = i;
					}
				}
				else if (edge2.m_End == edge.m_End || edge2.m_End == edge.m_Start)
				{
					DynamicBuffer<ConnectedEdge> connectedEdges2 = m_ConnectedEdges[edge2.m_End];
					if (CompositionChange(connectedEdges2, edge2.m_End == edge.m_End))
					{
						AddArrows(startIndex, i, aggregateElements, arrowPositions, edgeMap);
						startIndex = i;
					}
				}
				else
				{
					AddArrows(startIndex, i, aggregateElements, arrowPositions, edgeMap);
					startIndex = i;
				}
				edge = edge2;
			}
			AddArrows(startIndex, i, aggregateElements, arrowPositions, edgeMap);
		}

		private bool CompositionChange(DynamicBuffer<ConnectedEdge> connectedEdges, bool invert)
		{
			if (connectedEdges.Length != 2)
			{
				return true;
			}
			Entity edge = connectedEdges[0].m_Edge;
			Entity edge2 = connectedEdges[1].m_Edge;
			Composition composition = m_CompositionData[edge];
			Composition composition2 = m_CompositionData[edge2];
			DynamicBuffer<NetCompositionCarriageway> dynamicBuffer = m_NetCompositionCarriageways[composition.m_Edge];
			DynamicBuffer<NetCompositionCarriageway> dynamicBuffer2 = m_NetCompositionCarriageways[composition2.m_Edge];
			if (dynamicBuffer.Length != dynamicBuffer2.Length)
			{
				return true;
			}
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				NetCompositionCarriageway netCompositionCarriageway = dynamicBuffer[i];
				NetCompositionCarriageway netCompositionCarriageway2;
				if (invert)
				{
					netCompositionCarriageway2 = dynamicBuffer2[dynamicBuffer2.Length - i - 1];
					if ((netCompositionCarriageway2.m_Flags & LaneFlags.Twoway) == 0)
					{
						netCompositionCarriageway2.m_Flags ^= LaneFlags.Invert;
					}
				}
				else
				{
					netCompositionCarriageway2 = dynamicBuffer2[i];
				}
				if (((netCompositionCarriageway.m_Flags ^ netCompositionCarriageway2.m_Flags) & (LaneFlags.Invert | LaneFlags.Road | LaneFlags.Track | LaneFlags.Twoway)) != 0)
				{
					return true;
				}
			}
			return false;
		}

		private void AddArrows(int startIndex, int endIndex, NativeArray<AggregateElement> aggregateElements, DynamicBuffer<ArrowPosition> arrowPositions, NativeParallelHashMap<Entity, Entity> edgeMap)
		{
			float num = 0f;
			for (int i = startIndex; i < endIndex; i++)
			{
				Entity edge = aggregateElements[i].m_Edge;
				Curve curve = m_CurveData[edge];
				Composition composition = m_CompositionData[edge];
				NetCompositionData netCompositionData = m_NetCompositionData[composition.m_Edge];
				DynamicBuffer<NetCompositionCarriageway> dynamicBuffer = m_NetCompositionCarriageways[composition.m_Edge];
				float x = 0f;
				int num2 = 0;
				if ((netCompositionData.m_State & CompositionState.Marker) == 0 || m_EditorMode)
				{
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						NetCompositionCarriageway netCompositionCarriageway = dynamicBuffer[j];
						if ((netCompositionCarriageway.m_Flags & LaneFlags.Twoway) == 0)
						{
							x = math.min(x, netCompositionCarriageway.m_Width + 4f);
							num2++;
						}
					}
				}
				x = math.max(x, netCompositionData.m_Width / (float)math.max(1, num2));
				x = math.sqrt(math.max(1f, x));
				num += curve.m_Length / x;
			}
			float num3 = 25f;
			int num4 = math.max(Mathf.RoundToInt(num / num3), 1);
			num3 = math.max(1f, num / (float)num4);
			float num5 = 20f;
			float num6 = num5 * 0.5f;
			num = num3 * -0.5f;
			ArrowPosition elem = default(ArrowPosition);
			for (int k = startIndex; k < endIndex; k++)
			{
				Entity edge2 = aggregateElements[k].m_Edge;
				Curve curve2 = m_CurveData[edge2];
				Composition composition2 = m_CompositionData[edge2];
				NetCompositionData netCompositionData2 = m_NetCompositionData[composition2.m_Edge];
				DynamicBuffer<NetCompositionCarriageway> dynamicBuffer2 = m_NetCompositionCarriageways[composition2.m_Edge];
				float x2 = 0f;
				int num7 = 0;
				if ((netCompositionData2.m_State & CompositionState.Marker) == 0 || m_EditorMode)
				{
					for (int l = 0; l < dynamicBuffer2.Length; l++)
					{
						NetCompositionCarriageway netCompositionCarriageway2 = dynamicBuffer2[l];
						if ((netCompositionCarriageway2.m_Flags & LaneFlags.Twoway) == 0)
						{
							x2 = math.min(x2, netCompositionCarriageway2.m_Width + 4f);
							num7++;
						}
					}
				}
				x2 = math.max(x2, netCompositionData2.m_Width / (float)math.max(1, num7));
				x2 = math.sqrt(math.max(1f, x2));
				float num8 = num + curve2.m_Length / x2;
				bool test = IsInverted(edge2, k, aggregateElements);
				float num9 = math.min(0.25f, num5 / math.max(1f, curve2.m_Length));
				float num10 = 1f - num9;
				if (k > startIndex)
				{
					Curve curve3 = m_CurveData[aggregateElements[k - 1].m_Edge];
					num9 = math.select(num9, 0f, IsContinuous(curve3, curve2));
				}
				if (k < endIndex - 1)
				{
					Curve curve4 = m_CurveData[aggregateElements[k + 1].m_Edge];
					num10 = math.select(num10, 1f, IsContinuous(curve2, curve4));
				}
				bool flag = true;
				if (edgeMap.IsCreated && edgeMap.ContainsKey(edge2))
				{
					flag = false;
				}
				while (num8 >= 0f)
				{
					if (flag)
					{
						float valueToClamp = (0f - num) / math.max(1f, num8 - num);
						valueToClamp = math.clamp(valueToClamp, num9, num10);
						valueToClamp = math.select(valueToClamp, 1f - valueToClamp, test);
						float3 @float = MathUtils.Position(curve2.m_Bezier, valueToClamp);
						float3 float2 = math.normalizesafe(MathUtils.Tangent(curve2.m_Bezier, valueToClamp));
						float3 float3 = math.normalizesafe(new float3(float2.z, 0f, 0f - float2.x));
						if ((netCompositionData2.m_State & CompositionState.Marker) == 0 || m_EditorMode)
						{
							for (int m = 0; m < dynamicBuffer2.Length; m++)
							{
								NetCompositionCarriageway netCompositionCarriageway3 = dynamicBuffer2[m];
								if ((netCompositionCarriageway3.m_Flags & LaneFlags.Twoway) == 0)
								{
									x2 = math.max(netCompositionCarriageway3.m_Width + 4f, netCompositionData2.m_Width / (float)math.max(1, num7));
									elem.m_Direction = math.select(float2, -float2, (netCompositionCarriageway3.m_Flags & LaneFlags.Invert) != 0);
									elem.m_Position = @float + float3 * netCompositionCarriageway3.m_Position.x + elem.m_Direction;
									elem.m_Position.y += netCompositionCarriageway3.m_Position.y;
									elem.m_MaxScale = x2 * 0.5f / num6;
									elem.m_IsTrack = (netCompositionCarriageway3.m_Flags & LaneFlags.Road) == 0;
									elem.m_IsUnderground = (netCompositionData2.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0;
									arrowPositions.Add(elem);
								}
							}
						}
					}
					num -= num3;
					num8 -= num3;
				}
				num = num8;
			}
		}

		private bool IsContinuous(Curve curve1, Curve curve2)
		{
			float4 @float = default(float4);
			@float.x = math.abs(math.distancesq(curve1.m_Bezier.a, curve2.m_Bezier.a));
			@float.y = math.abs(math.distancesq(curve1.m_Bezier.a, curve2.m_Bezier.d));
			@float.z = math.abs(math.distancesq(curve1.m_Bezier.d, curve2.m_Bezier.a));
			@float.w = math.abs(math.distancesq(curve1.m_Bezier.d, curve2.m_Bezier.d));
			if (math.all(@float > 1f))
			{
				return false;
			}
			float3 x = ((!math.any((@float.xy < @float.zw) & (@float.xy < @float.wz))) ? MathUtils.EndTangent(curve1.m_Bezier) : (-MathUtils.StartTangent(curve1.m_Bezier)));
			return math.dot(y: math.normalizesafe((!math.any((@float.xz < @float.yw) & (@float.xz < @float.wy))) ? MathUtils.EndTangent(curve2.m_Bezier) : (-MathUtils.StartTangent(curve2.m_Bezier))), x: math.normalizesafe(x)) < -0.99f;
		}

		private bool IsInverted(Entity edge, int index, NativeArray<AggregateElement> aggregateElements)
		{
			if (index > 0)
			{
				Edge edge2 = m_EdgeData[aggregateElements[index - 1].m_Edge];
				Edge edge3 = m_EdgeData[edge];
				if (!(edge3.m_End == edge2.m_Start))
				{
					return edge3.m_End == edge2.m_End;
				}
				return true;
			}
			if (index < aggregateElements.Length - 1)
			{
				Edge edge4 = m_EdgeData[edge];
				Edge edge5 = m_EdgeData[aggregateElements[index + 1].m_Edge];
				if (!(edge4.m_Start == edge5.m_Start))
				{
					return edge4.m_Start == edge5.m_End;
				}
				return true;
			}
			return false;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct LabelVertexData
	{
		public Vector3 m_Position;

		public Vector3 m_Normal;

		public Color32 m_Color;

		public Vector4 m_UV0;

		public Vector4 m_UV1;

		public Vector4 m_UV2;

		public Vector4 m_UV3;
	}

	private struct SubMeshData
	{
		public int m_VertexCount;

		public Bounds3 m_Bounds;
	}

	[BurstCompile]
	private struct FillNameDataJob : IJob
	{
		[ReadOnly]
		public ComponentTypeHandle<LabelExtents> m_LabelExtentsType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Hidden> m_HiddenType;

		[ReadOnly]
		public BufferTypeHandle<LabelPosition> m_LabelPositionType;

		[ReadOnly]
		public BufferTypeHandle<LabelVertex> m_LabelVertexType;

		[ReadOnly]
		public ComponentLookup<NetNameData> m_NetNameData;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public int m_SubMeshCount;

		public Mesh.MeshDataArray m_NameMeshData;

		public void Execute()
		{
			NativeArray<SubMeshData> array = new NativeArray<SubMeshData>(m_SubMeshCount, Allocator.Temp);
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				if (archetypeChunk.Has(ref m_HiddenType))
				{
					continue;
				}
				BufferAccessor<LabelPosition> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_LabelPositionType);
				BufferAccessor<LabelVertex> bufferAccessor2 = archetypeChunk.GetBufferAccessor(ref m_LabelVertexType);
				for (int j = 0; j < bufferAccessor2.Length; j++)
				{
					DynamicBuffer<LabelPosition> dynamicBuffer = bufferAccessor[j];
					DynamicBuffer<LabelVertex> dynamicBuffer2 = bufferAccessor2[j];
					for (int k = 0; k < dynamicBuffer.Length; k++)
					{
						LabelPosition labelPosition = dynamicBuffer[k];
						for (int l = 0; l < dynamicBuffer2.Length; l += 4)
						{
							int2 material = dynamicBuffer2[l].m_Material;
							int index = math.select(material.x, material.y, labelPosition.m_IsUnderground);
							array.ElementAt(index).m_VertexCount += 4;
						}
					}
				}
			}
			int num = 0;
			for (int m = 0; m < m_SubMeshCount; m++)
			{
				num += array[m].m_VertexCount;
			}
			Mesh.MeshData meshData = m_NameMeshData[0];
			NativeArray<VertexAttributeDescriptor> attributes = new NativeArray<VertexAttributeDescriptor>(7, Allocator.Temp, NativeArrayOptions.UninitializedMemory)
			{
				[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
				[1] = new VertexAttributeDescriptor(VertexAttribute.Normal),
				[2] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
				[3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 4),
				[4] = new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 4),
				[5] = new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.Float32, 4),
				[6] = new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, 4)
			};
			meshData.SetVertexBufferParams(num, attributes);
			meshData.SetIndexBufferParams((num >> 2) * 6, IndexFormat.UInt32);
			attributes.Dispose();
			num = 0;
			meshData.subMeshCount = m_SubMeshCount;
			for (int n = 0; n < m_SubMeshCount; n++)
			{
				ref SubMeshData reference = ref array.ElementAt(n);
				meshData.SetSubMesh(n, new SubMeshDescriptor
				{
					firstVertex = num,
					indexStart = (num >> 2) * 6,
					vertexCount = reference.m_VertexCount,
					indexCount = (reference.m_VertexCount >> 2) * 6,
					topology = MeshTopology.Triangles
				}, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
				num += reference.m_VertexCount;
				reference.m_VertexCount = 0;
				reference.m_Bounds = new Bounds3(float.MaxValue, float.MinValue);
			}
			NativeArray<LabelVertexData> vertexData = meshData.GetVertexData<LabelVertexData>();
			NativeArray<uint> indexData = meshData.GetIndexData<uint>();
			LabelVertexData value = default(LabelVertexData);
			for (int num2 = 0; num2 < m_Chunks.Length; num2++)
			{
				ArchetypeChunk archetypeChunk2 = m_Chunks[num2];
				if (archetypeChunk2.Has(ref m_HiddenType))
				{
					continue;
				}
				NativeArray<LabelExtents> nativeArray = archetypeChunk2.GetNativeArray(ref m_LabelExtentsType);
				NativeArray<PrefabRef> nativeArray2 = archetypeChunk2.GetNativeArray(ref m_PrefabRefType);
				NativeArray<Temp> nativeArray3 = archetypeChunk2.GetNativeArray(ref m_TempType);
				BufferAccessor<LabelPosition> bufferAccessor3 = archetypeChunk2.GetBufferAccessor(ref m_LabelPositionType);
				BufferAccessor<LabelVertex> bufferAccessor4 = archetypeChunk2.GetBufferAccessor(ref m_LabelVertexType);
				for (int num3 = 0; num3 < bufferAccessor4.Length; num3++)
				{
					LabelExtents labelExtents = nativeArray[num3];
					PrefabRef prefabRef = nativeArray2[num3];
					DynamicBuffer<LabelPosition> dynamicBuffer3 = bufferAccessor3[num3];
					DynamicBuffer<LabelVertex> dynamicBuffer4 = bufferAccessor4[num3];
					NetNameData netNameData = m_NetNameData[prefabRef.m_Prefab];
					Color32 color = netNameData.m_Color;
					if (nativeArray3.Length != 0 && (nativeArray3[num3].m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Select | TempFlags.Modify | TempFlags.Replace)) != 0)
					{
						color = netNameData.m_SelectedColor;
					}
					float num4 = math.length(math.max(-labelExtents.m_Bounds.min, labelExtents.m_Bounds.max));
					for (int num5 = 0; num5 < dynamicBuffer3.Length; num5++)
					{
						LabelPosition labelPosition2 = dynamicBuffer3[num5];
						float3 @float = new float3(labelPosition2.m_HalfLength, 0f, 0f);
						float2 xy = labelPosition2.m_Curve.a.xy;
						float2 xy2 = labelPosition2.m_Curve.b.xy;
						float4 float2 = new float4(labelPosition2.m_Curve.c, labelPosition2.m_Curve.a.z);
						float4 float3 = new float4(labelPosition2.m_Curve.d, labelPosition2.m_Curve.b.z);
						float3 float4 = MathUtils.Position(labelPosition2.m_Curve, 0.5f);
						float num6 = num4 * labelPosition2.m_MaxScale;
						Bounds3 bounds = new Bounds3(float4 - num6, float4 + num6);
						SubMeshDescriptor subMeshDescriptor = default(SubMeshDescriptor);
						int num7 = -1;
						for (int num8 = 0; num8 < dynamicBuffer4.Length; num8 += 4)
						{
							int2 material2 = dynamicBuffer4[num8].m_Material;
							int num9 = math.select(material2.x, material2.y, labelPosition2.m_IsUnderground);
							ref SubMeshData reference2 = ref array.ElementAt(num9);
							if (num9 != num7)
							{
								subMeshDescriptor = meshData.GetSubMesh(num9);
								reference2.m_Bounds |= bounds;
								num7 = num9;
							}
							int num10 = subMeshDescriptor.firstVertex + reference2.m_VertexCount;
							int num11 = subMeshDescriptor.indexStart + (reference2.m_VertexCount >> 2) * 6;
							reference2.m_VertexCount += 4;
							indexData[num11] = (uint)num10;
							indexData[num11 + 1] = (uint)(num10 + 1);
							indexData[num11 + 2] = (uint)(num10 + 2);
							indexData[num11 + 3] = (uint)(num10 + 2);
							indexData[num11 + 4] = (uint)(num10 + 3);
							indexData[num11 + 5] = (uint)num10;
							for (int num12 = 0; num12 < 4; num12++)
							{
								LabelVertex labelVertex = dynamicBuffer4[num8 + num12];
								value.m_Position = labelVertex.m_Position;
								value.m_Normal = @float;
								value.m_Color = new Color32((byte)(labelVertex.m_Color.r * color.r >> 8), (byte)(labelVertex.m_Color.g * color.g >> 8), (byte)(labelVertex.m_Color.b * color.b >> 8), (byte)(labelVertex.m_Color.a * color.a >> 8));
								value.m_UV0 = new float4(labelVertex.m_UV0, xy);
								value.m_UV1 = new float4(labelPosition2.m_MaxScale, labelVertex.m_UV1.y, xy2);
								value.m_UV2 = float2;
								value.m_UV3 = float3;
								vertexData[num10 + num12] = value;
							}
						}
					}
				}
			}
			for (int num13 = 0; num13 < m_SubMeshCount; num13++)
			{
				SubMeshDescriptor subMesh = meshData.GetSubMesh(num13);
				subMesh.bounds = RenderingUtils.ToBounds(array[num13].m_Bounds);
				meshData.SetSubMesh(num13, subMesh, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
			}
			array.Dispose();
		}
	}

	private struct ArrowVertexData
	{
		public Vector3 m_Position;

		public Color32 m_Color;

		public Vector2 m_UV0;

		public Vector4 m_UV1;
	}

	[BurstCompile]
	private struct FillArrowDataJob : IJob
	{
		[ReadOnly]
		public ComponentTypeHandle<Hidden> m_HiddenType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<ArrowPosition> m_ArrowPositionType;

		[ReadOnly]
		public ComponentLookup<NetArrowData> m_NetArrowData;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		public Mesh.MeshDataArray m_ArrowMeshData;

		public void Execute()
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				if (archetypeChunk.Has(ref m_HiddenType))
				{
					continue;
				}
				BufferAccessor<ArrowPosition> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_ArrowPositionType);
				for (int j = 0; j < bufferAccessor.Length; j++)
				{
					DynamicBuffer<ArrowPosition> dynamicBuffer = bufferAccessor[j];
					for (int k = 0; k < dynamicBuffer.Length; k++)
					{
						if (dynamicBuffer[k].m_IsUnderground)
						{
							num3 += 4;
							num4 += 6;
						}
						else
						{
							num += 4;
							num2 += 6;
						}
					}
				}
			}
			Mesh.MeshData meshData = m_ArrowMeshData[0];
			NativeArray<VertexAttributeDescriptor> attributes = new NativeArray<VertexAttributeDescriptor>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory)
			{
				[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
				[1] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
				[2] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
				[3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 4)
			};
			meshData.SetVertexBufferParams(num + num3, attributes);
			meshData.SetIndexBufferParams(num2 + num4, IndexFormat.UInt32);
			attributes.Dispose();
			meshData.subMeshCount = 2;
			meshData.SetSubMesh(0, new SubMeshDescriptor
			{
				vertexCount = num,
				indexCount = num2,
				topology = MeshTopology.Triangles
			}, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
			meshData.SetSubMesh(1, new SubMeshDescriptor
			{
				firstVertex = num,
				indexStart = num2,
				vertexCount = num3,
				indexCount = num4,
				topology = MeshTopology.Triangles
			}, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
			NativeArray<ArrowVertexData> vertexData = meshData.GetVertexData<ArrowVertexData>();
			NativeArray<uint> indexData = meshData.GetIndexData<uint>();
			SubMeshDescriptor subMesh = meshData.GetSubMesh(0);
			SubMeshDescriptor subMesh2 = meshData.GetSubMesh(1);
			Bounds3 bounds = new Bounds3(float.MaxValue, float.MinValue);
			Bounds3 bounds2 = new Bounds3(float.MaxValue, float.MinValue);
			int vertexIndex = 0;
			int indexIndex = 0;
			int vertexIndex2 = subMesh2.firstVertex;
			int indexIndex2 = subMesh2.indexStart;
			for (int l = 0; l < m_Chunks.Length; l++)
			{
				ArchetypeChunk archetypeChunk2 = m_Chunks[l];
				if (archetypeChunk2.Has(ref m_HiddenType))
				{
					continue;
				}
				NativeArray<PrefabRef> nativeArray = archetypeChunk2.GetNativeArray(ref m_PrefabRefType);
				BufferAccessor<ArrowPosition> bufferAccessor2 = archetypeChunk2.GetBufferAccessor(ref m_ArrowPositionType);
				for (int m = 0; m < bufferAccessor2.Length; m++)
				{
					PrefabRef prefabRef = nativeArray[m];
					DynamicBuffer<ArrowPosition> dynamicBuffer2 = bufferAccessor2[m];
					NetArrowData netArrowData = m_NetArrowData[prefabRef.m_Prefab];
					float num5 = 20f;
					float num6 = num5 * 0.5f;
					for (int n = 0; n < dynamicBuffer2.Length; n++)
					{
						ArrowPosition arrowPosition = dynamicBuffer2[n];
						Color32 color = (arrowPosition.m_IsTrack ? netArrowData.m_TrackColor : netArrowData.m_RoadColor);
						float4 uv = new float4(arrowPosition.m_Position, arrowPosition.m_MaxScale);
						float3 z = arrowPosition.m_Direction * num5;
						float3 x = math.normalizesafe(new float3(0f - arrowPosition.m_Direction.z, 0f, arrowPosition.m_Direction.x), math.right()) * num6;
						float num7 = num5 * arrowPosition.m_MaxScale;
						if (arrowPosition.m_IsUnderground)
						{
							bounds2 |= new Bounds3(arrowPosition.m_Position - num7, arrowPosition.m_Position + num7);
							AddArrow(vertexData, indexData, color, uv, z, x, ref vertexIndex2, ref indexIndex2);
						}
						else
						{
							bounds |= new Bounds3(arrowPosition.m_Position - num7, arrowPosition.m_Position + num7);
							AddArrow(vertexData, indexData, color, uv, z, x, ref vertexIndex, ref indexIndex);
						}
					}
				}
			}
			subMesh.bounds = RenderingUtils.ToBounds(bounds);
			subMesh2.bounds = RenderingUtils.ToBounds(bounds2);
			meshData.SetSubMesh(0, subMesh, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
			meshData.SetSubMesh(1, subMesh2, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
		}

		private void AddArrow(NativeArray<ArrowVertexData> vertices, NativeArray<uint> indices, Color32 color, float4 uv1, float3 z, float3 x, ref int vertexIndex, ref int indexIndex)
		{
			indices[indexIndex++] = (uint)vertexIndex;
			indices[indexIndex++] = (uint)(vertexIndex + 1);
			indices[indexIndex++] = (uint)(vertexIndex + 2);
			indices[indexIndex++] = (uint)(vertexIndex + 2);
			indices[indexIndex++] = (uint)(vertexIndex + 3);
			indices[indexIndex++] = (uint)vertexIndex;
			ArrowVertexData value = default(ArrowVertexData);
			value.m_Position = -x - z;
			value.m_Color = color;
			value.m_UV0 = new float2(0f, 0f);
			value.m_UV1 = uv1;
			vertices[vertexIndex++] = value;
			value.m_Position = x - z;
			value.m_Color = color;
			value.m_UV0 = new float2(1f, 0f);
			value.m_UV1 = uv1;
			vertices[vertexIndex++] = value;
			value.m_Position = x + z;
			value.m_Color = color;
			value.m_UV0 = new float2(1f, 1f);
			value.m_UV1 = uv1;
			vertices[vertexIndex++] = value;
			value.m_Position = z - x;
			value.m_Color = color;
			value.m_UV0 = new float2(0f, 1f);
			value.m_UV1 = uv1;
			vertices[vertexIndex++] = value;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<NetNameData> __Game_Prefabs_NetNameData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<NetArrowData> __Game_Prefabs_NetArrowData_RW_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Updated> __Game_Common_Updated_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BatchesUpdated> __Game_Common_BatchesUpdated_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<LabelExtents> __Game_Net_LabelExtents_RW_ComponentTypeHandle;

		public SharedComponentTypeHandle<LabelMaterial> __Game_Net_LabelMaterial_SharedComponentTypeHandle;

		public BufferTypeHandle<LabelVertex> __Game_Net_LabelVertex_RW_BufferTypeHandle;

		public SharedComponentTypeHandle<ArrowMaterial> __Game_Net_ArrowMaterial_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<LabelExtents> __Game_Net_LabelExtents_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<AggregateElement> __Game_Net_AggregateElement_RO_BufferTypeHandle;

		public BufferTypeHandle<LabelPosition> __Game_Net_LabelPosition_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Aggregated> __Game_Net_Aggregated_RO_ComponentTypeHandle;

		public BufferTypeHandle<ArrowPosition> __Game_Net_ArrowPosition_RW_BufferTypeHandle;

		[ReadOnly]
		public BufferLookup<NetCompositionCarriageway> __Game_Prefabs_NetCompositionCarriageway_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Hidden> __Game_Tools_Hidden_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LabelPosition> __Game_Net_LabelPosition_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LabelVertex> __Game_Net_LabelVertex_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<NetNameData> __Game_Prefabs_NetNameData_RO_ComponentLookup;

		[ReadOnly]
		public BufferTypeHandle<ArrowPosition> __Game_Net_ArrowPosition_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<NetArrowData> __Game_Prefabs_NetArrowData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
			__Game_Prefabs_NetNameData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NetNameData>();
			__Game_Prefabs_NetArrowData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<NetArrowData>();
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Updated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Updated>(isReadOnly: true);
			__Game_Common_BatchesUpdated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BatchesUpdated>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Net_LabelExtents_RW_ComponentTypeHandle = state.GetComponentTypeHandle<LabelExtents>();
			__Game_Net_LabelMaterial_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<LabelMaterial>();
			__Game_Net_LabelVertex_RW_BufferTypeHandle = state.GetBufferTypeHandle<LabelVertex>();
			__Game_Net_ArrowMaterial_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<ArrowMaterial>();
			__Game_Net_LabelExtents_RO_ComponentTypeHandle = state.GetComponentTypeHandle<LabelExtents>(isReadOnly: true);
			__Game_Net_AggregateElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<AggregateElement>(isReadOnly: true);
			__Game_Net_LabelPosition_RW_BufferTypeHandle = state.GetBufferTypeHandle<LabelPosition>();
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Net_Aggregated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Aggregated>(isReadOnly: true);
			__Game_Net_ArrowPosition_RW_BufferTypeHandle = state.GetBufferTypeHandle<ArrowPosition>();
			__Game_Prefabs_NetCompositionCarriageway_RO_BufferLookup = state.GetBufferLookup<NetCompositionCarriageway>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Hidden>(isReadOnly: true);
			__Game_Net_LabelPosition_RO_BufferTypeHandle = state.GetBufferTypeHandle<LabelPosition>(isReadOnly: true);
			__Game_Net_LabelVertex_RO_BufferTypeHandle = state.GetBufferTypeHandle<LabelVertex>(isReadOnly: true);
			__Game_Prefabs_NetNameData_RO_ComponentLookup = state.GetComponentLookup<NetNameData>(isReadOnly: true);
			__Game_Net_ArrowPosition_RO_BufferTypeHandle = state.GetBufferTypeHandle<ArrowPosition>(isReadOnly: true);
			__Game_Prefabs_NetArrowData_RO_ComponentLookup = state.GetComponentLookup<NetArrowData>(isReadOnly: true);
		}
	}

	private EntityQuery m_CreatedPrefabQuery;

	private EntityQuery m_UpdatedLabelQuery;

	private EntityQuery m_LabelQuery;

	private EntityQuery m_UpdatedArrowQuery;

	private EntityQuery m_ArrowQuery;

	private EntityQuery m_TempAggregatedQuery;

	private OverlayRenderSystem m_OverlayRenderSystem;

	private UndergroundViewSystem m_UndergroundViewSystem;

	private PrefabSystem m_PrefabSystem;

	private NameSystem m_NameSystem;

	private ToolSystem m_ToolSystem;

	private List<MeshData> m_LabelData;

	private List<MeshData> m_ArrowData;

	private Dictionary<Entity, string> m_CachedLabels;

	private int m_FaceColor;

	private bool m_TunnelSelectOn;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_OverlayRenderSystem = base.World.GetOrCreateSystemManaged<OverlayRenderSystem>();
		m_UndergroundViewSystem = base.World.GetOrCreateSystemManaged<UndergroundViewSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_NameSystem = base.World.GetOrCreateSystemManaged<NameSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_CreatedPrefabQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<AggregateNetData>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<NetNameData>(),
				ComponentType.ReadOnly<NetArrowData>()
			}
		});
		m_UpdatedLabelQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Aggregate>(),
				ComponentType.ReadOnly<LabelMaterial>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<BatchesUpdated>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_LabelQuery = GetEntityQuery(ComponentType.ReadOnly<Aggregate>(), ComponentType.ReadOnly<LabelMaterial>(), ComponentType.Exclude<Deleted>());
		m_UpdatedArrowQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Aggregate>(),
				ComponentType.ReadOnly<ArrowMaterial>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<BatchesUpdated>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_ArrowQuery = GetEntityQuery(ComponentType.ReadOnly<Aggregate>(), ComponentType.ReadOnly<ArrowMaterial>(), ComponentType.Exclude<Deleted>());
		m_TempAggregatedQuery = GetEntityQuery(ComponentType.ReadOnly<Aggregated>(), ComponentType.ReadOnly<Temp>(), ComponentType.Exclude<Deleted>());
		m_FaceColor = Shader.PropertyToID("_FaceColor");
		GameManager.instance.localizationManager.onActiveDictionaryChanged += OnDictionaryChanged;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		DestroyMeshData(m_LabelData);
		DestroyMeshData(m_ArrowData);
		GameManager.instance.localizationManager.onActiveDictionaryChanged -= OnDictionaryChanged;
		base.OnDestroy();
	}

	private void DestroyMeshData(List<MeshData> meshData)
	{
		if (meshData == null)
		{
			return;
		}
		for (int i = 0; i < meshData.Count; i++)
		{
			MeshData meshData2 = meshData[i];
			if (meshData2.m_Materials != null)
			{
				for (int j = 0; j < meshData2.m_Materials.Count; j++)
				{
					MaterialData materialData = meshData2.m_Materials[j];
					if (materialData.m_Material != null)
					{
						Object.Destroy(materialData.m_Material);
					}
				}
			}
			if (meshData2.m_Mesh != null)
			{
				Object.Destroy(meshData2.m_Mesh);
			}
			if (meshData2.m_HasMeshData)
			{
				meshData2.m_MeshData.Dispose();
			}
		}
		meshData.Clear();
	}

	public void PreDeserialize(Context context)
	{
		ClearMeshData(m_LabelData);
		ClearMeshData(m_ArrowData);
		if (m_CachedLabels != null)
		{
			m_CachedLabels.Clear();
		}
		m_Loaded = true;
	}

	private void UpdateUndergroundState(List<MeshData> meshData, bool undergroundOn)
	{
		if (meshData == null)
		{
			return;
		}
		for (int i = 0; i < meshData.Count; i++)
		{
			MeshData meshData2 = meshData[i];
			if (meshData2.m_Materials == null)
			{
				continue;
			}
			for (int j = 0; j < meshData2.m_Materials.Count; j++)
			{
				MaterialData materialData = meshData2.m_Materials[j];
				if (!materialData.m_IsUnderground && materialData.m_Material != null)
				{
					materialData.m_Material.SetColor(m_FaceColor, new Color(1f, 1f, 1f, undergroundOn ? 0.25f : 1f));
				}
			}
		}
	}

	private void OnDictionaryChanged()
	{
		base.EntityManager.AddComponent<Updated>(m_LabelQuery);
	}

	private void ClearMeshData(List<MeshData> meshData)
	{
		if (meshData == null)
		{
			return;
		}
		for (int i = 0; i < meshData.Count; i++)
		{
			MeshData meshData2 = meshData[i];
			if (meshData2.m_Materials != null)
			{
				for (int j = 0; j < meshData2.m_Materials.Count; j++)
				{
					MaterialData value = meshData2.m_Materials[j];
					value.m_HasMesh = false;
					meshData2.m_Materials[j] = value;
				}
			}
			if (meshData2.m_Mesh != null)
			{
				Object.Destroy(meshData2.m_Mesh);
				meshData2.m_Mesh = null;
			}
			if (meshData2.m_HasMeshData)
			{
				meshData2.m_MeshData.Dispose();
				meshData2.m_HasMeshData = false;
			}
			meshData2.m_HasMesh = false;
		}
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
		if (!m_CreatedPrefabQuery.IsEmptyIgnoreFilter)
		{
			InitializePrefabs();
		}
		EntityQuery entityQuery = (loaded ? m_LabelQuery : m_UpdatedLabelQuery);
		EntityQuery entityQuery2 = (loaded ? m_ArrowQuery : m_UpdatedArrowQuery);
		bool flag = m_UndergroundViewSystem.undergroundOn && m_UndergroundViewSystem.tunnelsOn;
		bool flag2 = !entityQuery.IsEmptyIgnoreFilter;
		bool flag3 = !entityQuery2.IsEmptyIgnoreFilter;
		if (flag != m_TunnelSelectOn)
		{
			UpdateUndergroundState(m_LabelData, flag);
			UpdateUndergroundState(m_ArrowData, flag);
			m_TunnelSelectOn = flag;
		}
		if (flag2 || flag3)
		{
			JobHandle dependency = base.Dependency;
			JobHandle jobHandle = default(JobHandle);
			if (flag2)
			{
				UpdateLabelVertices(loaded);
				JobHandle inputDeps = UpdateLabelPositions(dependency, loaded);
				jobHandle = JobHandle.CombineDependencies(jobHandle, FillNameMeshData(inputDeps));
			}
			if (flag3)
			{
				UpdateArrowMaterials(loaded);
				JobHandle inputDeps2 = UpdateArrowPositions(dependency, loaded);
				jobHandle = JobHandle.CombineDependencies(jobHandle, FillArrowMeshData(inputDeps2));
			}
			base.Dependency = jobHandle;
		}
	}

	private void InitializePrefabs()
	{
		NativeArray<ArchetypeChunk> nativeArray = m_CreatedPrefabQuery.ToArchetypeChunkArray(Allocator.TempJob);
		try
		{
			bool flag = m_UndergroundViewSystem.undergroundOn && m_UndergroundViewSystem.tunnelsOn;
			ComponentTypeHandle<PrefabData> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<NetNameData> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetNameData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<NetArrowData> typeHandle3 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetArrowData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ArchetypeChunk archetypeChunk = nativeArray[i];
				NativeArray<PrefabData> nativeArray2 = archetypeChunk.GetNativeArray(ref typeHandle);
				NativeArray<NetNameData> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle2);
				NativeArray<NetArrowData> nativeArray4 = archetypeChunk.GetNativeArray(ref typeHandle3);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					AggregateNetPrefab prefab = m_PrefabSystem.GetPrefab<AggregateNetPrefab>(nativeArray2[j]);
					NetLabel component = prefab.GetComponent<NetLabel>();
					NetArrow component2 = prefab.GetComponent<NetArrow>();
					NetNameData value;
					int num;
					if (component != null && component.m_NameMaterial != null)
					{
						value = nativeArray3[j];
						if (m_LabelData != null)
						{
							num = 0;
							while (num < m_LabelData.Count)
							{
								if (!(m_LabelData[num].m_OriginalMaterial == component.m_NameMaterial))
								{
									num++;
									continue;
								}
								goto IL_012f;
							}
						}
						MeshData meshData = new MeshData();
						meshData.m_OriginalMaterial = component.m_NameMaterial;
						meshData.m_Materials = new List<MaterialData>(2);
						if (m_LabelData == null)
						{
							m_LabelData = new List<MeshData>();
						}
						value.m_MaterialIndex = m_LabelData.Count;
						nativeArray3[j] = value;
						m_LabelData.Add(meshData);
					}
					goto IL_01b9;
					IL_01b9:
					if (!(component2 != null) || !(component2.m_ArrowMaterial != null))
					{
						continue;
					}
					NetArrowData value2 = nativeArray4[j];
					int num2;
					if (m_ArrowData != null)
					{
						num2 = 0;
						while (num2 < m_ArrowData.Count)
						{
							if (!(m_ArrowData[num2].m_OriginalMaterial == component2.m_ArrowMaterial))
							{
								num2++;
								continue;
							}
							goto IL_0210;
						}
					}
					MeshData meshData2 = new MeshData();
					meshData2.m_OriginalMaterial = component2.m_ArrowMaterial;
					meshData2.m_Materials = new List<MaterialData>(2);
					MaterialData item = default(MaterialData);
					item.m_Material = new Material(component2.m_ArrowMaterial);
					item.m_Material.name = "Aggregate arrows (" + prefab.name + ")";
					item.m_Material.SetColor(m_FaceColor, new Color(1f, 1f, 1f, flag ? 0.25f : 1f));
					meshData2.m_Materials.Add(item);
					MaterialData item2 = default(MaterialData);
					item2.m_Material = new Material(component2.m_ArrowMaterial);
					item2.m_Material.name = "Aggregate underground arrows (" + prefab.name + ")";
					item2.m_Material.SetColor(m_FaceColor, new Color(1f, 1f, 1f, 1f));
					item2.m_IsUnderground = true;
					meshData2.m_Materials.Add(item2);
					if (m_ArrowData == null)
					{
						m_ArrowData = new List<MeshData>();
					}
					value2.m_MaterialIndex = m_ArrowData.Count;
					nativeArray4[j] = value2;
					m_ArrowData.Add(meshData2);
					continue;
					IL_012f:
					value.m_MaterialIndex = num;
					nativeArray3[j] = value;
					goto IL_01b9;
					IL_0210:
					value2.m_MaterialIndex = num2;
					nativeArray4[j] = value2;
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
	}

	private void UpdateLabelVertices(bool isLoaded)
	{
		List<MaterialUpdate> list = null;
		NativeArray<ArchetypeChunk> nativeArray = (isLoaded ? m_LabelQuery : m_UpdatedLabelQuery).ToArchetypeChunkArray(Allocator.TempJob);
		try
		{
			TextMeshPro textMesh = m_OverlayRenderSystem.GetTextMesh();
			textMesh.rectTransform.sizeDelta = new Vector2(250f, 100f);
			textMesh.fontSize = 200f;
			textMesh.alignment = TextAlignmentOptions.Center;
			bool flag = m_UndergroundViewSystem.undergroundOn && m_UndergroundViewSystem.tunnelsOn;
			EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<Updated> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Updated_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<BatchesUpdated> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_BatchesUpdated_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<Temp> typeHandle3 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PrefabRef> typeHandle4 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<LabelExtents> typeHandle5 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_LabelExtents_RW_ComponentTypeHandle, ref base.CheckedStateRef);
			SharedComponentTypeHandle<LabelMaterial> sharedComponentTypeHandle = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Net_LabelMaterial_SharedComponentTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<LabelVertex> bufferTypeHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_LabelVertex_RW_BufferTypeHandle, ref base.CheckedStateRef);
			LabelVertex value2 = default(LabelVertex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ArchetypeChunk archetypeChunk = nativeArray[i];
				LabelMaterial sharedComponent = archetypeChunk.GetSharedComponent(sharedComponentTypeHandle, base.EntityManager);
				if (isLoaded || archetypeChunk.Has(ref typeHandle) || archetypeChunk.Has(ref typeHandle2))
				{
					NativeArray<Entity> nativeArray2 = archetypeChunk.GetNativeArray(entityTypeHandle);
					NativeArray<Temp> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle3);
					NativeArray<PrefabRef> nativeArray4 = archetypeChunk.GetNativeArray(ref typeHandle4);
					NativeArray<LabelExtents> nativeArray5 = archetypeChunk.GetNativeArray(ref typeHandle5);
					BufferAccessor<LabelVertex> bufferAccessor = archetypeChunk.GetBufferAccessor(ref bufferTypeHandle);
					for (int j = 0; j < nativeArray2.Length; j++)
					{
						Entity entity = nativeArray2[j];
						PrefabRef prefabRef = nativeArray4[j];
						DynamicBuffer<LabelVertex> dynamicBuffer = bufferAccessor[j];
						NetNameData componentData = base.EntityManager.GetComponentData<NetNameData>(prefabRef.m_Prefab);
						MeshData meshData = m_LabelData[componentData.m_MaterialIndex];
						meshData.m_MeshDirty = true;
						if (componentData.m_MaterialIndex != sharedComponent.m_Index)
						{
							if (list == null)
							{
								list = new List<MaterialUpdate>();
							}
							list.Add(new MaterialUpdate
							{
								m_Entity = entity,
								m_Material = componentData.m_MaterialIndex
							});
						}
						string renderedLabelName;
						if (nativeArray3.Length != 0)
						{
							Temp temp = nativeArray3[j];
							if (!(temp.m_Original != Entity.Null))
							{
								if (m_CachedLabels != null && m_CachedLabels.ContainsKey(entity))
								{
									m_CachedLabels.Remove(entity);
								}
								dynamicBuffer.Clear();
								continue;
							}
							renderedLabelName = m_NameSystem.GetRenderedLabelName(temp.m_Original);
						}
						else
						{
							renderedLabelName = m_NameSystem.GetRenderedLabelName(entity);
						}
						if (m_CachedLabels != null)
						{
							if (m_CachedLabels.TryGetValue(entity, out var value))
							{
								if (value == renderedLabelName)
								{
									continue;
								}
								m_CachedLabels[entity] = renderedLabelName;
							}
							else
							{
								m_CachedLabels.Add(entity, renderedLabelName);
							}
						}
						else
						{
							m_CachedLabels = new Dictionary<Entity, string>();
							m_CachedLabels.Add(entity, renderedLabelName);
						}
						TMP_TextInfo textInfo = textMesh.GetTextInfo(renderedLabelName);
						int num = 0;
						for (int k = 0; k < textInfo.meshInfo.Length; k++)
						{
							TMP_MeshInfo tMP_MeshInfo = textInfo.meshInfo[k];
							num += tMP_MeshInfo.vertexCount;
						}
						dynamicBuffer.ResizeUninitialized(num);
						num = 0;
						for (int l = 0; l < textInfo.meshInfo.Length; l++)
						{
							TMP_MeshInfo tMP_MeshInfo2 = textInfo.meshInfo[l];
							if (tMP_MeshInfo2.vertexCount == 0)
							{
								continue;
							}
							Texture mainTexture = tMP_MeshInfo2.material.mainTexture;
							int2 material = -1;
							for (int m = 0; m < meshData.m_Materials.Count; m++)
							{
								MaterialData materialData = meshData.m_Materials[m];
								if (materialData.m_Material.mainTexture == mainTexture)
								{
									if (materialData.m_IsUnderground)
									{
										material.y = m;
									}
									else
									{
										material.x = m;
									}
								}
							}
							if (material.x == -1)
							{
								MaterialData item = default(MaterialData);
								item.m_Material = new Material(meshData.m_OriginalMaterial);
								item.m_Material.SetColor(m_FaceColor, new Color(1f, 1f, 1f, flag ? 0.25f : 1f));
								m_OverlayRenderSystem.CopyFontAtlasParameters(tMP_MeshInfo2.material, item.m_Material);
								material.x = meshData.m_Materials.Count;
								meshData.m_Materials.Add(item);
								item.m_Material.name = $"Aggregate names {material.x} ({meshData.m_OriginalMaterial.name})";
							}
							if (material.y == -1)
							{
								MaterialData item2 = default(MaterialData);
								item2.m_Material = new Material(meshData.m_OriginalMaterial);
								item2.m_Material.SetColor(m_FaceColor, new Color(1f, 1f, 1f, 1f));
								m_OverlayRenderSystem.CopyFontAtlasParameters(tMP_MeshInfo2.material, item2.m_Material);
								item2.m_IsUnderground = true;
								material.y = meshData.m_Materials.Count;
								meshData.m_Materials.Add(item2);
								item2.m_Material.name = $"Aggregate underground names {material.y} ({meshData.m_OriginalMaterial.name})";
							}
							Vector3[] vertices = tMP_MeshInfo2.vertices;
							Vector2[] uvs = tMP_MeshInfo2.uvs0;
							Vector2[] uvs2 = tMP_MeshInfo2.uvs2;
							Color32[] colors = tMP_MeshInfo2.colors32;
							for (int n = 0; n < tMP_MeshInfo2.vertexCount; n++)
							{
								value2.m_Position = vertices[n];
								value2.m_Color = colors[n];
								value2.m_UV0 = uvs[n];
								value2.m_UV1 = uvs2[n];
								value2.m_Material = material;
								dynamicBuffer[num + n] = value2;
							}
							num += tMP_MeshInfo2.vertexCount;
						}
						LabelExtents value3 = default(LabelExtents);
						for (int num2 = 0; num2 < textInfo.lineCount; num2++)
						{
							Extents lineExtents = textInfo.lineInfo[num2].lineExtents;
							value3.m_Bounds |= new Bounds2(lineExtents.min, lineExtents.max);
						}
						nativeArray5[j] = value3;
					}
					continue;
				}
				NativeArray<Entity> nativeArray6 = archetypeChunk.GetNativeArray(entityTypeHandle);
				NativeArray<PrefabRef> nativeArray7 = archetypeChunk.GetNativeArray(ref typeHandle4);
				for (int num3 = 0; num3 < nativeArray6.Length; num3++)
				{
					Entity key = nativeArray6[num3];
					PrefabRef prefabRef2 = nativeArray7[num3];
					NetNameData componentData2 = base.EntityManager.GetComponentData<NetNameData>(prefabRef2.m_Prefab);
					m_LabelData[componentData2.m_MaterialIndex].m_MeshDirty = true;
					if (m_CachedLabels != null)
					{
						m_CachedLabels.Remove(key);
					}
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
		if (list != null)
		{
			for (int num4 = 0; num4 < list.Count; num4++)
			{
				MaterialUpdate materialUpdate = list[num4];
				base.EntityManager.SetSharedComponent(materialUpdate.m_Entity, new LabelMaterial
				{
					m_Index = materialUpdate.m_Material
				});
			}
		}
	}

	private void UpdateArrowMaterials(bool isLoaded)
	{
		List<MaterialUpdate> list = null;
		NativeArray<ArchetypeChunk> nativeArray = (isLoaded ? m_ArrowQuery : m_UpdatedArrowQuery).ToArchetypeChunkArray(Allocator.TempJob);
		try
		{
			EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<Updated> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Updated_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PrefabRef> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			SharedComponentTypeHandle<ArrowMaterial> sharedComponentTypeHandle = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Net_ArrowMaterial_SharedComponentTypeHandle, ref base.CheckedStateRef);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ArchetypeChunk archetypeChunk = nativeArray[i];
				ArrowMaterial sharedComponent = archetypeChunk.GetSharedComponent(sharedComponentTypeHandle, base.EntityManager);
				if (isLoaded || archetypeChunk.Has(ref typeHandle))
				{
					NativeArray<Entity> nativeArray2 = archetypeChunk.GetNativeArray(entityTypeHandle);
					NativeArray<PrefabRef> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle2);
					for (int j = 0; j < nativeArray2.Length; j++)
					{
						Entity entity = nativeArray2[j];
						PrefabRef prefabRef = nativeArray3[j];
						NetArrowData componentData = base.EntityManager.GetComponentData<NetArrowData>(prefabRef.m_Prefab);
						m_ArrowData[componentData.m_MaterialIndex].m_MeshDirty = true;
						if (componentData.m_MaterialIndex != sharedComponent.m_Index)
						{
							if (list == null)
							{
								list = new List<MaterialUpdate>();
							}
							list.Add(new MaterialUpdate
							{
								m_Entity = entity,
								m_Material = componentData.m_MaterialIndex
							});
						}
					}
				}
				else
				{
					NativeArray<Entity> nativeArray4 = archetypeChunk.GetNativeArray(entityTypeHandle);
					NativeArray<PrefabRef> nativeArray5 = archetypeChunk.GetNativeArray(ref typeHandle2);
					for (int k = 0; k < nativeArray4.Length; k++)
					{
						_ = nativeArray4[k];
						PrefabRef prefabRef2 = nativeArray5[k];
						NetArrowData componentData2 = base.EntityManager.GetComponentData<NetArrowData>(prefabRef2.m_Prefab);
						m_ArrowData[componentData2.m_MaterialIndex].m_MeshDirty = true;
					}
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
		if (list != null)
		{
			for (int l = 0; l < list.Count; l++)
			{
				MaterialUpdate materialUpdate = list[l];
				base.EntityManager.SetSharedComponent(materialUpdate.m_Entity, new ArrowMaterial
				{
					m_Index = materialUpdate.m_Material
				});
			}
		}
	}

	private JobHandle UpdateLabelPositions(JobHandle inputDeps, bool isLoaded)
	{
		EntityQuery query = (isLoaded ? m_LabelQuery : m_UpdatedLabelQuery);
		return JobChunkExtensions.ScheduleParallel(new UpdateLabelPositionsJob
		{
			m_LabelExtentsType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_LabelExtents_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AggregateElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_AggregateElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_LabelPositionType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_LabelPosition_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef)
		}, query, inputDeps);
	}

	private JobHandle UpdateArrowPositions(JobHandle inputDeps, bool isLoaded)
	{
		EntityQuery query = (isLoaded ? m_ArrowQuery : m_UpdatedArrowQuery);
		NativeParallelMultiHashMap<Entity, TempValue> tempMap = new NativeParallelMultiHashMap<Entity, TempValue>(32, Allocator.TempJob);
		FillTempMapJob jobData = new FillTempMapJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_AggregatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Aggregated_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempMap = tempMap
		};
		UpdateArrowPositionsJob jobData2 = new UpdateArrowPositionsJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_AggregateElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_AggregateElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ArrowPositionType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ArrowPosition_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_NetCompositionCarriageways = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionCarriageway_RO_BufferLookup, ref base.CheckedStateRef),
			m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
			m_TempMap = tempMap
		};
		JobHandle dependsOn = JobChunkExtensions.Schedule(jobData, m_TempAggregatedQuery, inputDeps);
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData2, query, dependsOn);
		tempMap.Dispose(jobHandle);
		return jobHandle;
	}

	private JobHandle FillNameMeshData(JobHandle inputDeps)
	{
		JobHandle jobHandle = inputDeps;
		if (m_LabelData != null)
		{
			for (int i = 0; i < m_LabelData.Count; i++)
			{
				MeshData meshData = m_LabelData[i];
				if (!meshData.m_MeshDirty)
				{
					continue;
				}
				meshData.m_MeshDirty = false;
				m_LabelQuery.ResetFilter();
				m_LabelQuery.SetSharedComponentFilter(new LabelMaterial
				{
					m_Index = i
				});
				if (m_LabelQuery.IsEmptyIgnoreFilter)
				{
					if (meshData.m_Materials != null)
					{
						for (int j = 0; j < meshData.m_Materials.Count; j++)
						{
							MaterialData value = meshData.m_Materials[j];
							value.m_HasMesh = false;
							meshData.m_Materials[j] = value;
						}
					}
					if (meshData.m_Mesh != null)
					{
						Object.Destroy(meshData.m_Mesh);
						meshData.m_Mesh = null;
					}
					if (meshData.m_HasMeshData)
					{
						meshData.m_HasMeshData = false;
						meshData.m_MeshData.Dispose();
					}
					meshData.m_HasMesh = false;
					continue;
				}
				JobHandle outJobHandle;
				NativeList<ArchetypeChunk> chunks = m_LabelQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
				if (!meshData.m_HasMeshData)
				{
					meshData.m_HasMeshData = true;
					meshData.m_MeshData = Mesh.AllocateWritableMeshData(1);
				}
				JobHandle jobHandle2 = IJobExtensions.Schedule(new FillNameDataJob
				{
					m_LabelExtentsType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_LabelExtents_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_HiddenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentTypeHandle, ref base.CheckedStateRef),
					m_LabelPositionType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_LabelPosition_RO_BufferTypeHandle, ref base.CheckedStateRef),
					m_LabelVertexType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_LabelVertex_RO_BufferTypeHandle, ref base.CheckedStateRef),
					m_NetNameData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetNameData_RO_ComponentLookup, ref base.CheckedStateRef),
					m_Chunks = chunks,
					m_SubMeshCount = meshData.m_Materials.Count,
					m_NameMeshData = meshData.m_MeshData
				}, JobHandle.CombineDependencies(outJobHandle, inputDeps));
				chunks.Dispose(jobHandle2);
				meshData.m_DataDependencies = jobHandle2;
				jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
			}
		}
		return jobHandle;
	}

	private JobHandle FillArrowMeshData(JobHandle inputDeps)
	{
		JobHandle jobHandle = inputDeps;
		if (m_ArrowData != null)
		{
			for (int i = 0; i < m_ArrowData.Count; i++)
			{
				MeshData meshData = m_ArrowData[i];
				if (!meshData.m_MeshDirty)
				{
					continue;
				}
				meshData.m_MeshDirty = false;
				m_ArrowQuery.ResetFilter();
				m_ArrowQuery.SetSharedComponentFilter(new ArrowMaterial
				{
					m_Index = i
				});
				if (m_ArrowQuery.IsEmptyIgnoreFilter)
				{
					if (meshData.m_Materials != null)
					{
						for (int j = 0; j < meshData.m_Materials.Count; j++)
						{
							MaterialData value = meshData.m_Materials[j];
							value.m_HasMesh = false;
							meshData.m_Materials[j] = value;
						}
					}
					if (meshData.m_Mesh != null)
					{
						Object.Destroy(meshData.m_Mesh);
						meshData.m_Mesh = null;
					}
					if (meshData.m_HasMeshData)
					{
						meshData.m_HasMeshData = false;
						meshData.m_MeshData.Dispose();
					}
					meshData.m_HasMesh = false;
				}
				else
				{
					JobHandle outJobHandle;
					NativeList<ArchetypeChunk> chunks = m_ArrowQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
					if (!meshData.m_HasMeshData)
					{
						meshData.m_HasMeshData = true;
						meshData.m_MeshData = Mesh.AllocateWritableMeshData(1);
					}
					JobHandle jobHandle2 = IJobExtensions.Schedule(new FillArrowDataJob
					{
						m_HiddenType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentTypeHandle, ref base.CheckedStateRef),
						m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
						m_ArrowPositionType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_ArrowPosition_RO_BufferTypeHandle, ref base.CheckedStateRef),
						m_NetArrowData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetArrowData_RO_ComponentLookup, ref base.CheckedStateRef),
						m_Chunks = chunks,
						m_ArrowMeshData = meshData.m_MeshData
					}, JobHandle.CombineDependencies(outJobHandle, inputDeps));
					chunks.Dispose(jobHandle2);
					meshData.m_DataDependencies = jobHandle2;
					jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
				}
			}
		}
		return jobHandle;
	}

	public int GetNameMaterialCount()
	{
		if (m_LabelData != null)
		{
			return m_LabelData.Count;
		}
		return 0;
	}

	public int GetArrowMaterialCount()
	{
		if (m_ArrowData != null)
		{
			return m_ArrowData.Count;
		}
		return 0;
	}

	public bool GetNameMesh(int index, out Mesh mesh, out int subMeshCount)
	{
		return GetMeshData(m_LabelData, index, out mesh, out subMeshCount);
	}

	public bool GetNameMaterial(int index, int subMeshIndex, out Material material)
	{
		return GetMaterialData(m_LabelData, index, subMeshIndex, out material);
	}

	public bool GetArrowMesh(int index, out Mesh mesh, out int subMeshCount)
	{
		return GetMeshData(m_ArrowData, index, out mesh, out subMeshCount);
	}

	public bool GetArrowMaterial(int index, int subMeshIndex, out Material material)
	{
		return GetMaterialData(m_ArrowData, index, subMeshIndex, out material);
	}

	private bool GetMeshData(List<MeshData> meshData, int index, out Mesh mesh, out int subMeshCount)
	{
		MeshData meshData2 = meshData[index];
		subMeshCount = meshData2.m_Materials.Count;
		if (meshData2.m_HasMeshData)
		{
			meshData2.m_HasMeshData = false;
			meshData2.m_DataDependencies.Complete();
			meshData2.m_DataDependencies = default(JobHandle);
			if (meshData2.m_Mesh == null)
			{
				meshData2.m_Mesh = new Mesh();
				if (meshData2.m_OriginalMaterial != null)
				{
					meshData2.m_Mesh.name = $"Aggregates ({meshData2.m_OriginalMaterial})";
				}
			}
			Mesh.ApplyAndDisposeWritableMeshData(meshData2.m_MeshData, meshData2.m_Mesh, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);
			Bounds bounds = default(Bounds);
			meshData2.m_HasMesh = false;
			for (int i = 0; i < subMeshCount; i++)
			{
				MaterialData value = meshData2.m_Materials[i];
				SubMeshDescriptor subMesh = meshData2.m_Mesh.GetSubMesh(i);
				value.m_HasMesh = subMesh.vertexCount > 0;
				if (value.m_HasMesh)
				{
					if (meshData2.m_HasMesh)
					{
						bounds.Encapsulate(subMesh.bounds);
					}
					else
					{
						bounds = subMesh.bounds;
						meshData2.m_HasMesh = true;
					}
				}
				meshData2.m_Materials[i] = value;
			}
			meshData2.m_Mesh.bounds = bounds;
		}
		mesh = meshData2.m_Mesh;
		return meshData2.m_HasMesh;
	}

	private bool GetMaterialData(List<MeshData> meshData, int index, int subMeshIndex, out Material material)
	{
		MaterialData materialData = meshData[index].m_Materials[subMeshIndex];
		material = materialData.m_Material;
		return materialData.m_HasMesh;
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
	public AggregateMeshSystem()
	{
	}
}
