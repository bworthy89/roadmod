using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Net;

[CompilerGenerated]
public class AggregateSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateAgregatesJob : IJob
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Created> m_CreatedType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public ComponentLookup<AggregateNetData> m_PrefabAggregateData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		public ComponentLookup<Aggregated> m_AggregatedData;

		public BufferLookup<AggregateElement> m_AggregateElements;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			int num = 0;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				num += m_Chunks[i].Count;
			}
			NativeParallelHashSet<Entity> edgeSet = new NativeParallelHashSet<Entity>(num, Allocator.Temp);
			NativeParallelHashSet<Entity> emptySet = new NativeParallelHashSet<Entity>(num, Allocator.Temp);
			NativeParallelHashMap<Entity, Entity> updateMap = new NativeParallelHashMap<Entity, Entity>(num, Allocator.Temp);
			for (int j = 0; j < m_Chunks.Length; j++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[j];
				bool flag = archetypeChunk.Has(ref m_TempType);
				if (archetypeChunk.Has(ref m_CreatedType))
				{
					NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
					for (int k = 0; k < nativeArray.Length; k++)
					{
						Entity entity = nativeArray[k];
						Aggregated aggregated = m_AggregatedData[entity];
						if (aggregated.m_Aggregate != Entity.Null)
						{
							if (flag && !m_TempData.HasComponent(aggregated.m_Aggregate))
							{
								updateMap.TryAdd(aggregated.m_Aggregate, aggregated.m_Aggregate);
								continue;
							}
							m_AggregateElements[aggregated.m_Aggregate].Add(new AggregateElement(entity));
							edgeSet.Add(aggregated.m_Aggregate);
						}
						else
						{
							emptySet.Add(entity);
						}
					}
					continue;
				}
				NativeArray<Entity> nativeArray2 = archetypeChunk.GetNativeArray(m_EntityType);
				for (int l = 0; l < nativeArray2.Length; l++)
				{
					Entity entity2 = nativeArray2[l];
					Aggregated aggregated2 = m_AggregatedData[entity2];
					if (aggregated2.m_Aggregate != Entity.Null)
					{
						if (flag && !m_TempData.HasComponent(aggregated2.m_Aggregate))
						{
							updateMap.TryAdd(aggregated2.m_Aggregate, aggregated2.m_Aggregate);
						}
						else
						{
							edgeSet.Add(aggregated2.m_Aggregate);
						}
					}
				}
			}
			if (!edgeSet.IsEmpty)
			{
				NativeArray<Entity> nativeArray3 = edgeSet.ToNativeArray(Allocator.Temp);
				edgeSet.Clear();
				for (int m = 0; m < nativeArray3.Length; m++)
				{
					ValidateAggregate(nativeArray3[m], edgeSet, emptySet, updateMap);
				}
				for (int n = 0; n < nativeArray3.Length; n++)
				{
					CombineAggregate(nativeArray3[n], updateMap);
				}
				nativeArray3.Dispose();
			}
			if (!emptySet.IsEmpty)
			{
				NativeArray<Entity> nativeArray4 = emptySet.ToNativeArray(Allocator.Temp);
				NativeList<AggregateElement> edgeList = new NativeList<AggregateElement>(32, Allocator.Temp);
				for (int num2 = 0; num2 < nativeArray4.Length; num2++)
				{
					Entity entity3 = nativeArray4[num2];
					if (emptySet.Contains(entity3))
					{
						emptySet.Remove(entity3);
						CreateAggregate(entity3, emptySet, edgeList, updateMap);
					}
				}
				edgeList.Dispose();
				nativeArray4.Dispose();
			}
			if (!updateMap.IsEmpty)
			{
				for (int num3 = 0; num3 < m_Chunks.Length; num3++)
				{
					ArchetypeChunk archetypeChunk2 = m_Chunks[num3];
					if (!archetypeChunk2.Has(ref m_TempType))
					{
						continue;
					}
					NativeArray<Entity> nativeArray5 = archetypeChunk2.GetNativeArray(m_EntityType);
					for (int num4 = 0; num4 < nativeArray5.Length; num4++)
					{
						Entity entity4 = nativeArray5[num4];
						Aggregated value = m_AggregatedData[entity4];
						if (value.m_Aggregate != Entity.Null && updateMap.TryGetValue(value.m_Aggregate, out var item) && item != value.m_Aggregate)
						{
							value.m_Aggregate = item;
							m_AggregatedData[entity4] = value;
						}
					}
				}
				NativeParallelHashMap<Entity, Entity>.Enumerator enumerator = updateMap.GetEnumerator();
				while (enumerator.MoveNext())
				{
					Entity value2 = enumerator.Current.Value;
					if (m_AggregateElements.HasBuffer(value2))
					{
						if (m_DeletedData.HasComponent(value2))
						{
							m_CommandBuffer.RemoveComponent<Deleted>(value2);
							m_CommandBuffer.AddComponent(value2, default(Updated));
						}
						else
						{
							m_CommandBuffer.AddComponent(value2, default(BatchesUpdated));
						}
					}
				}
			}
			edgeSet.Dispose();
			emptySet.Dispose();
			updateMap.Dispose();
		}

		private void CreateAggregate(Entity startEdge, NativeParallelHashSet<Entity> emptySet, NativeList<AggregateElement> edgeList, NativeParallelHashMap<Entity, Entity> updateMap)
		{
			Entity aggregateType = GetAggregateType(startEdge);
			if (aggregateType == Entity.Null)
			{
				return;
			}
			Edge edge = m_EdgeData[startEdge];
			AddElements(startEdge, edge.m_Start, isStartNode: true, aggregateType, emptySet, edgeList);
			CollectionUtils.Reverse(edgeList.AsArray());
			edgeList.Add(new AggregateElement(startEdge));
			AddElements(startEdge, edge.m_End, isStartNode: false, aggregateType, emptySet, edgeList);
			bool flag = m_TempData.HasComponent(startEdge);
			if (!TryCombine(aggregateType, edgeList, flag, updateMap))
			{
				AggregateNetData aggregateNetData = m_PrefabAggregateData[aggregateType];
				Entity entity = m_CommandBuffer.CreateEntity(aggregateNetData.m_Archetype);
				m_CommandBuffer.SetComponent(entity, new PrefabRef(aggregateType));
				DynamicBuffer<AggregateElement> dynamicBuffer = m_CommandBuffer.SetBuffer<AggregateElement>(entity);
				if (flag)
				{
					m_CommandBuffer.AddComponent(entity, new Temp(Entity.Null, TempFlags.Create));
				}
				for (int i = 0; i < edgeList.Length; i++)
				{
					AggregateElement elem = edgeList[i];
					m_CommandBuffer.SetComponent(elem.m_Edge, new Aggregated
					{
						m_Aggregate = entity
					});
					dynamicBuffer.Add(elem);
				}
			}
			edgeList.Clear();
		}

		private bool TryCombine(Entity prefab, NativeList<AggregateElement> edgeList, bool isTemp, NativeParallelHashMap<Entity, Entity> updateMap)
		{
			if (GetStart(edgeList.AsArray(), out var edge, out var node, out var isStart) && ShouldCombine(edge, node, isStart, prefab, Entity.Null, isTemp, out var otherAggregate, out var otherIsStart))
			{
				DynamicBuffer<AggregateElement> dynamicBuffer = m_AggregateElements[otherAggregate];
				int length = dynamicBuffer.Length;
				dynamicBuffer.ResizeUninitialized(dynamicBuffer.Length + edgeList.Length);
				if (otherIsStart)
				{
					for (int num = length - 1; num >= 0; num--)
					{
						dynamicBuffer[edgeList.Length + num] = dynamicBuffer[num];
					}
				}
				for (int i = 0; i < edgeList.Length; i++)
				{
					AggregateElement value = edgeList[i];
					m_AggregatedData[value.m_Edge] = new Aggregated
					{
						m_Aggregate = otherAggregate
					};
					dynamicBuffer[math.select(length, 0, otherIsStart) + edgeList.Length - i - 1] = value;
				}
				m_CommandBuffer.AddComponent<Updated>(otherAggregate);
				if (GetEnd(edgeList.AsArray(), out var edge2, out var node2, out var isStart2) && ShouldCombine(edge2, node2, isStart2, prefab, otherAggregate, isTemp, out var otherAggregate2, out var otherIsStart2))
				{
					DynamicBuffer<AggregateElement> dynamicBuffer2 = m_AggregateElements[otherAggregate2];
					length = dynamicBuffer.Length;
					dynamicBuffer.ResizeUninitialized(dynamicBuffer2.Length + dynamicBuffer.Length);
					if (otherIsStart)
					{
						for (int num2 = length - 1; num2 >= 0; num2--)
						{
							dynamicBuffer[dynamicBuffer2.Length + num2] = dynamicBuffer[num2];
						}
					}
					for (int j = 0; j < dynamicBuffer2.Length; j++)
					{
						AggregateElement value2 = dynamicBuffer2[j];
						m_AggregatedData[value2.m_Edge] = new Aggregated
						{
							m_Aggregate = otherAggregate
						};
						dynamicBuffer[math.select(length, 0, otherIsStart) + math.select(j, dynamicBuffer2.Length - j - 1, otherIsStart2 == otherIsStart)] = value2;
					}
					dynamicBuffer2.Clear();
					m_CommandBuffer.AddComponent<Deleted>(otherAggregate2);
					if (updateMap.ContainsKey(otherAggregate2))
					{
						updateMap[otherAggregate2] = otherAggregate;
					}
				}
				return true;
			}
			if (GetEnd(edgeList.AsArray(), out var edge3, out var node3, out var isStart3) && ShouldCombine(edge3, node3, isStart3, prefab, Entity.Null, isTemp, out var otherAggregate3, out var otherIsStart3))
			{
				DynamicBuffer<AggregateElement> dynamicBuffer3 = m_AggregateElements[otherAggregate3];
				int length2 = dynamicBuffer3.Length;
				dynamicBuffer3.ResizeUninitialized(dynamicBuffer3.Length + edgeList.Length);
				if (otherIsStart3)
				{
					for (int num3 = length2 - 1; num3 >= 0; num3--)
					{
						dynamicBuffer3[edgeList.Length + num3] = dynamicBuffer3[num3];
					}
				}
				for (int k = 0; k < edgeList.Length; k++)
				{
					AggregateElement value3 = edgeList[k];
					m_AggregatedData[value3.m_Edge] = new Aggregated
					{
						m_Aggregate = otherAggregate3
					};
					dynamicBuffer3[math.select(length2, 0, otherIsStart3) + k] = value3;
				}
				m_CommandBuffer.AddComponent<Updated>(otherAggregate3);
				return true;
			}
			return false;
		}

		private bool GetBestConnectionEdge(Entity prefab, Entity prevEdge, Entity prevNode, bool prevIsStart, out Entity nextEdge, out Entity nextNode, out bool nextIsStart)
		{
			Curve curve = m_CurveData[prevEdge];
			float2 @float;
			float2 x;
			float2 xz;
			if (prevIsStart)
			{
				@float = math.normalizesafe(-MathUtils.StartTangent(curve.m_Bezier).xz);
				x = math.normalizesafe(curve.m_Bezier.a.xz - curve.m_Bezier.d.xz);
				xz = curve.m_Bezier.a.xz;
			}
			else
			{
				@float = math.normalizesafe(MathUtils.EndTangent(curve.m_Bezier).xz);
				x = math.normalizesafe(curve.m_Bezier.d.xz - curve.m_Bezier.a.xz);
				xz = curve.m_Bezier.d.xz;
			}
			DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[prevNode];
			float num = 2f;
			nextEdge = Entity.Null;
			nextNode = Entity.Null;
			nextIsStart = false;
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				ConnectedEdge connectedEdge = dynamicBuffer[i];
				if (connectedEdge.m_Edge == prevEdge)
				{
					continue;
				}
				Edge edge = m_EdgeData[connectedEdge.m_Edge];
				if (edge.m_Start == prevNode)
				{
					if (GetAggregateType(connectedEdge.m_Edge) == prefab)
					{
						Curve curve2 = m_CurveData[connectedEdge.m_Edge];
						float2 float2 = math.normalizesafe(-MathUtils.StartTangent(curve2.m_Bezier).xz);
						float2 y = math.normalizesafe(curve2.m_Bezier.a.xz - curve2.m_Bezier.d.xz);
						float2 x2 = curve2.m_Bezier.a.xz - xz;
						float num2 = math.abs(math.dot(x2, MathUtils.Right(@float))) + math.abs(math.dot(x2, MathUtils.Right(float2)));
						num2 = 0.5f - 0.5f / (1f + num2 * 0.1f);
						float num3 = math.dot(@float, float2) + math.dot(x, y) * 0.5f + num2;
						if (num3 < num)
						{
							num = num3;
							nextEdge = connectedEdge.m_Edge;
							nextNode = edge.m_End;
							nextIsStart = false;
						}
					}
				}
				else if (edge.m_End == prevNode && GetAggregateType(connectedEdge.m_Edge) == prefab)
				{
					Curve curve3 = m_CurveData[connectedEdge.m_Edge];
					float2 float3 = math.normalizesafe(MathUtils.EndTangent(curve3.m_Bezier).xz);
					float2 y2 = math.normalizesafe(curve3.m_Bezier.d.xz - curve3.m_Bezier.a.xz);
					float2 x3 = curve3.m_Bezier.d.xz - xz;
					float num4 = math.abs(math.dot(x3, MathUtils.Right(@float))) + math.abs(math.dot(x3, MathUtils.Right(float3)));
					num4 = 0.5f - 0.5f / (1f + num4 * 0.1f);
					float num5 = math.dot(@float, float3) + math.dot(x, y2) * 0.5f + num4;
					if (num5 < num)
					{
						num = num5;
						nextEdge = connectedEdge.m_Edge;
						nextNode = edge.m_Start;
						nextIsStart = true;
					}
				}
			}
			return nextEdge != Entity.Null;
		}

		private void AddElements(Entity startEdge, Entity startNode, bool isStartNode, Entity prefab, NativeParallelHashSet<Entity> emptySet, NativeList<AggregateElement> elements)
		{
			Entity nextEdge;
			Entity nextNode;
			bool nextIsStart;
			Entity nextEdge2;
			Entity nextNode2;
			bool nextIsStart2;
			while (GetBestConnectionEdge(prefab, startEdge, startNode, isStartNode, out nextEdge, out nextNode, out nextIsStart) && GetBestConnectionEdge(prefab, nextEdge, startNode, !nextIsStart, out nextEdge2, out nextNode2, out nextIsStart2) && nextEdge2 == startEdge && emptySet.Contains(nextEdge))
			{
				elements.Add(new AggregateElement(nextEdge));
				emptySet.Remove(nextEdge);
				startEdge = nextEdge;
				startNode = nextNode;
				isStartNode = nextIsStart;
			}
		}

		private void CombineAggregate(Entity aggregate, NativeParallelHashMap<Entity, Entity> updateMap)
		{
			DynamicBuffer<AggregateElement> dynamicBuffer = m_AggregateElements[aggregate];
			Entity prefab = m_PrefabRefData[aggregate].m_Prefab;
			bool isTemp = m_TempData.HasComponent(aggregate);
			Entity edge;
			Entity node;
			bool isStart;
			Entity otherAggregate;
			bool otherIsStart;
			while (GetStart(dynamicBuffer.AsNativeArray(), out edge, out node, out isStart) && ShouldCombine(edge, node, isStart, prefab, aggregate, isTemp, out otherAggregate, out otherIsStart))
			{
				DynamicBuffer<AggregateElement> dynamicBuffer2 = m_AggregateElements[otherAggregate];
				int length = dynamicBuffer.Length;
				dynamicBuffer.ResizeUninitialized(dynamicBuffer2.Length + dynamicBuffer.Length);
				for (int num = length - 1; num >= 0; num--)
				{
					dynamicBuffer[dynamicBuffer2.Length + num] = dynamicBuffer[num];
				}
				for (int i = 0; i < dynamicBuffer2.Length; i++)
				{
					AggregateElement value = dynamicBuffer2[i];
					m_AggregatedData[value.m_Edge] = new Aggregated
					{
						m_Aggregate = aggregate
					};
					dynamicBuffer[math.select(i, dynamicBuffer2.Length - i - 1, otherIsStart)] = value;
				}
				dynamicBuffer2.Clear();
				m_CommandBuffer.AddComponent<Deleted>(otherAggregate);
				if (updateMap.ContainsKey(otherAggregate))
				{
					updateMap[otherAggregate] = aggregate;
				}
			}
			Entity edge2;
			Entity node2;
			bool isStart2;
			Entity otherAggregate2;
			bool otherIsStart2;
			while (GetEnd(dynamicBuffer.AsNativeArray(), out edge2, out node2, out isStart2) && ShouldCombine(edge2, node2, isStart2, prefab, aggregate, isTemp, out otherAggregate2, out otherIsStart2))
			{
				DynamicBuffer<AggregateElement> dynamicBuffer3 = m_AggregateElements[otherAggregate2];
				int length2 = dynamicBuffer.Length;
				dynamicBuffer.ResizeUninitialized(dynamicBuffer3.Length + dynamicBuffer.Length);
				for (int j = 0; j < dynamicBuffer3.Length; j++)
				{
					AggregateElement value2 = dynamicBuffer3[j];
					m_AggregatedData[value2.m_Edge] = new Aggregated
					{
						m_Aggregate = aggregate
					};
					dynamicBuffer[length2 + math.select(j, dynamicBuffer3.Length - j - 1, otherIsStart2)] = value2;
				}
				dynamicBuffer3.Clear();
				m_CommandBuffer.AddComponent<Deleted>(otherAggregate2);
				if (updateMap.ContainsKey(otherAggregate2))
				{
					updateMap[otherAggregate2] = aggregate;
				}
			}
		}

		private bool GetStart(NativeArray<AggregateElement> elements, out Entity edge, out Entity node, out bool isStart)
		{
			if (elements.Length == 0)
			{
				edge = Entity.Null;
				node = Entity.Null;
				isStart = false;
				return false;
			}
			if (elements.Length == 1)
			{
				edge = elements[0].m_Edge;
				node = m_EdgeData[edge].m_Start;
				isStart = true;
				return true;
			}
			edge = elements[0].m_Edge;
			Entity edge2 = elements[1].m_Edge;
			Edge edge3 = m_EdgeData[edge];
			Edge edge4 = m_EdgeData[edge2];
			if (edge3.m_End == edge4.m_Start || edge3.m_End == edge4.m_End)
			{
				node = edge3.m_Start;
				isStart = true;
			}
			else
			{
				node = edge3.m_End;
				isStart = false;
			}
			return true;
		}

		private bool GetEnd(NativeArray<AggregateElement> elements, out Entity edge, out Entity node, out bool isStart)
		{
			if (elements.Length == 0)
			{
				edge = Entity.Null;
				node = Entity.Null;
				isStart = false;
				return false;
			}
			if (elements.Length == 1)
			{
				edge = elements[0].m_Edge;
				node = m_EdgeData[edge].m_End;
				isStart = false;
				return true;
			}
			edge = elements[elements.Length - 1].m_Edge;
			Entity edge2 = elements[elements.Length - 2].m_Edge;
			Edge edge3 = m_EdgeData[edge];
			Edge edge4 = m_EdgeData[edge2];
			if (edge3.m_End == edge4.m_Start || edge3.m_End == edge4.m_End)
			{
				node = edge3.m_Start;
				isStart = true;
			}
			else
			{
				node = edge3.m_End;
				isStart = false;
			}
			return true;
		}

		private void ValidateAggregate(Entity aggregate, NativeParallelHashSet<Entity> edgeSet, NativeParallelHashSet<Entity> emptySet, NativeParallelHashMap<Entity, Entity> updateMap)
		{
			DynamicBuffer<AggregateElement> elements = m_AggregateElements[aggregate];
			Entity entity = Entity.Null;
			Entity prefab = m_PrefabRefData[aggregate].m_Prefab;
			for (int i = 0; i < elements.Length; i++)
			{
				AggregateElement aggregateElement = elements[i];
				if (!m_DeletedData.HasComponent(aggregateElement.m_Edge))
				{
					if (GetAggregateType(aggregateElement.m_Edge) != prefab)
					{
						emptySet.Add(aggregateElement.m_Edge);
						m_AggregatedData[aggregateElement.m_Edge] = default(Aggregated);
					}
					else if (entity == Entity.Null)
					{
						entity = aggregateElement.m_Edge;
					}
					else
					{
						edgeSet.Add(aggregateElement.m_Edge);
					}
				}
			}
			elements.Clear();
			if (entity == Entity.Null)
			{
				m_CommandBuffer.AddComponent<Deleted>(aggregate);
				if (updateMap.ContainsKey(aggregate))
				{
					updateMap[aggregate] = Entity.Null;
				}
			}
			else
			{
				Edge edge = m_EdgeData[entity];
				AddElements(entity, edge.m_Start, isStartNode: true, prefab, edgeSet, elements);
				CollectionUtils.Reverse(elements.AsNativeArray());
				int length = elements.Length;
				elements.Add(new AggregateElement(entity));
				AddElements(entity, edge.m_End, isStartNode: false, prefab, edgeSet, elements);
				if (length > elements.Length - length - 1)
				{
					CollectionUtils.Reverse(elements.AsNativeArray());
				}
				m_CommandBuffer.RemoveComponent<Deleted>(aggregate);
				m_CommandBuffer.AddComponent<Updated>(aggregate);
			}
			if (!edgeSet.IsEmpty)
			{
				NativeArray<Entity> nativeArray = edgeSet.ToNativeArray(Allocator.Temp);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity2 = nativeArray[j];
					emptySet.Add(entity2);
					m_AggregatedData[entity2] = default(Aggregated);
				}
				nativeArray.Dispose();
				edgeSet.Clear();
			}
		}

		private bool ShouldCombine(Entity startEdge, Entity startNode, bool isStartNode, Entity prefab, Entity aggregate, bool isTemp, out Entity otherAggregate, out bool otherIsStart)
		{
			if (GetBestConnectionEdge(prefab, startEdge, startNode, isStartNode, out var nextEdge, out var nextNode, out var nextIsStart) && GetBestConnectionEdge(prefab, nextEdge, startNode, !nextIsStart, out var nextEdge2, out nextNode, out var _) && nextEdge2 == startEdge && m_AggregatedData.HasComponent(nextEdge))
			{
				Aggregated aggregated = m_AggregatedData[nextEdge];
				if (aggregated.m_Aggregate != aggregate && m_AggregateElements.HasBuffer(aggregated.m_Aggregate) && m_TempData.HasComponent(aggregated.m_Aggregate) == isTemp)
				{
					DynamicBuffer<AggregateElement> dynamicBuffer = m_AggregateElements[aggregated.m_Aggregate];
					if (dynamicBuffer[0].m_Edge == nextEdge)
					{
						otherAggregate = aggregated.m_Aggregate;
						otherIsStart = true;
						return true;
					}
					if (dynamicBuffer[dynamicBuffer.Length - 1].m_Edge == nextEdge)
					{
						otherAggregate = aggregated.m_Aggregate;
						otherIsStart = false;
						return true;
					}
				}
			}
			otherAggregate = Entity.Null;
			otherIsStart = false;
			return false;
		}

		private void AddElements(Entity startEdge, Entity startNode, bool isStartNode, Entity prefab, NativeParallelHashSet<Entity> edgeSet, DynamicBuffer<AggregateElement> elements)
		{
			Entity nextEdge;
			Entity nextNode;
			bool nextIsStart;
			Entity nextEdge2;
			Entity nextNode2;
			bool nextIsStart2;
			while (GetBestConnectionEdge(prefab, startEdge, startNode, isStartNode, out nextEdge, out nextNode, out nextIsStart) && GetBestConnectionEdge(prefab, nextEdge, startNode, !nextIsStart, out nextEdge2, out nextNode2, out nextIsStart2) && nextEdge2 == startEdge && edgeSet.Contains(nextEdge))
			{
				elements.Add(new AggregateElement(nextEdge));
				edgeSet.Remove(nextEdge);
				startEdge = nextEdge;
				startNode = nextNode;
				isStartNode = nextIsStart;
			}
		}

		private Entity GetAggregateType(Entity edge)
		{
			PrefabRef prefabRef = m_PrefabRefData[edge];
			return m_PrefabGeometryData[prefabRef.m_Prefab].m_AggregateType;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Created> __Game_Common_Created_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AggregateNetData> __Game_Prefabs_AggregateNetData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		public ComponentLookup<Aggregated> __Game_Net_Aggregated_RW_ComponentLookup;

		public BufferLookup<AggregateElement> __Game_Net_AggregateElement_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Created_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Created>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Prefabs_AggregateNetData_RO_ComponentLookup = state.GetComponentLookup<AggregateNetData>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Net_Aggregated_RW_ComponentLookup = state.GetComponentLookup<Aggregated>();
			__Game_Net_AggregateElement_RW_BufferLookup = state.GetBufferLookup<AggregateElement>();
		}
	}

	private EntityQuery m_ModifiedQuery;

	private ModificationBarrier2B m_ModificationBarrier;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier2B>();
		m_ModifiedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Aggregated>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Deleted>()
			},
			None = new ComponentType[0]
		});
		RequireForUpdate(m_ModifiedQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> chunks = m_ModifiedQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = IJobExtensions.Schedule(new UpdateAgregatesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CreatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Created_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabAggregateData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AggregateNetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_AggregatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Aggregated_RW_ComponentLookup, ref base.CheckedStateRef),
			m_AggregateElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_AggregateElement_RW_BufferLookup, ref base.CheckedStateRef),
			m_Chunks = chunks,
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		}, JobHandle.CombineDependencies(base.Dependency, outJobHandle));
		chunks.Dispose(jobHandle);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
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
	public AggregateSystem()
	{
	}
}
