using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Game.Zones;
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
public class GenerateEdgesSystem : GameSystemBase
{
	private struct NodeMapKey : IEquatable<NodeMapKey>
	{
		public Entity m_OriginalEntity;

		public float3 m_Position;

		public bool m_IsPermanent;

		public bool m_IsEditor;

		public NodeMapKey(Entity originalEntity)
		{
			m_OriginalEntity = originalEntity;
			m_Position = default(float3);
			m_IsPermanent = false;
			m_IsEditor = false;
		}

		public NodeMapKey(Entity originalEntity, float3 position, bool isPermanent, bool isEditor)
		{
			m_OriginalEntity = originalEntity;
			m_Position = position;
			m_IsPermanent = isPermanent;
			m_IsEditor = isEditor;
		}

		public NodeMapKey(CoursePos coursePos, bool isPermanent, bool isEditor)
		{
			m_OriginalEntity = coursePos.m_Entity;
			m_Position = coursePos.m_Position;
			m_IsPermanent = isPermanent;
			m_IsEditor = isEditor;
		}

		public bool Equals(NodeMapKey other)
		{
			if (m_OriginalEntity != Entity.Null || other.m_OriginalEntity != Entity.Null)
			{
				return m_OriginalEntity.Equals(other.m_OriginalEntity);
			}
			if (m_Position.Equals(other.m_Position) && m_IsPermanent == other.m_IsPermanent)
			{
				return m_IsEditor == other.m_IsEditor;
			}
			return false;
		}

		public override int GetHashCode()
		{
			if (m_OriginalEntity != Entity.Null)
			{
				return m_OriginalEntity.GetHashCode();
			}
			return m_Position.GetHashCode();
		}
	}

	private struct LocalConnectItem
	{
		public Layer m_ConnectLayers;

		public Layer m_LocalConnectLayers;

		public float3 m_Position;

		public float m_Radius;

		public Bounds1 m_HeightRange;

		public Entity m_Node;

		public TempFlags m_TempFlags;

		public bool m_IsPermanent;

		public bool m_IsStandalone;

		public LocalConnectItem(Layer connectLayers, Layer localConnectLayers, float3 position, float radius, Bounds1 heightRange, Entity node, TempFlags tempFlags, bool isPermanent, bool isStandalone)
		{
			m_ConnectLayers = connectLayers;
			m_LocalConnectLayers = localConnectLayers;
			m_Position = position;
			m_Radius = radius;
			m_HeightRange = heightRange;
			m_Node = node;
			m_TempFlags = tempFlags;
			m_IsPermanent = isPermanent;
			m_IsStandalone = isStandalone;
		}
	}

	private struct OldEdgeKey : IEquatable<OldEdgeKey>
	{
		public Entity m_Prefab;

		public Entity m_SubPrefab;

		public Entity m_Original;

		public Entity m_Owner;

		public Entity m_StartNode;

		public Entity m_EndNode;

		public bool Equals(OldEdgeKey other)
		{
			if (m_Prefab.Equals(other.m_Prefab) && m_SubPrefab.Equals(other.m_SubPrefab) && m_Original.Equals(other.m_Original) && m_Owner.Equals(other.m_Owner) && m_StartNode.Equals(other.m_StartNode))
			{
				return m_EndNode.Equals(other.m_EndNode);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((((17 * 31 + m_Prefab.GetHashCode()) * 31 + m_SubPrefab.GetHashCode()) * 31 + m_Original.GetHashCode()) * 31 + m_Owner.GetHashCode()) * 31 + m_StartNode.GetHashCode()) * 31 + m_EndNode.GetHashCode();
		}
	}

	[BurstCompile]
	private struct CheckNodesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<EditorContainer> m_EditorContainerType;

		[ReadOnly]
		public ComponentTypeHandle<LocalConnect> m_LocalConnectType;

		[ReadOnly]
		public ComponentTypeHandle<Elevation> m_ElevationType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<Node> m_NodeType;

		[ReadOnly]
		public ComponentLookup<LocalConnectData> m_LocalConnectData;

		[ReadOnly]
		public ComponentLookup<NetData> m_NetData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_NetGeometryData;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		public NativeParallelMultiHashMap<NodeMapKey, Entity>.ParallelWriter m_NodeMap;

		public NativeQueue<LocalConnectItem>.ParallelWriter m_LocalConnectQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<Node> nativeArray3 = chunk.GetNativeArray(ref m_NodeType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			bool isEditor = chunk.Has(ref m_EditorContainerType);
			chunk.Has(ref m_ElevationType);
			bool flag = !chunk.Has(ref m_OwnerType);
			for (int i = 0; i < nativeArray3.Length; i++)
			{
				Node node = nativeArray3[i];
				_ = nativeArray4[i];
				if (nativeArray2.Length != 0)
				{
					m_NodeMap.Add(new NodeMapKey(nativeArray2[i].m_Original, node.m_Position, isPermanent: false, isEditor), nativeArray[i]);
				}
				else
				{
					m_NodeMap.Add(new NodeMapKey(Entity.Null, node.m_Position, isPermanent: true, isEditor), nativeArray[i]);
				}
			}
			if (!chunk.Has(ref m_LocalConnectType))
			{
				return;
			}
			for (int j = 0; j < nativeArray4.Length; j++)
			{
				Entity node2 = nativeArray[j];
				Node node3 = nativeArray3[j];
				PrefabRef prefabRef = nativeArray4[j];
				LocalConnectData localConnectData = m_LocalConnectData[prefabRef.m_Prefab];
				NetGeometryData netGeometryData = m_NetGeometryData[prefabRef.m_Prefab];
				NetData netData = m_NetData[prefabRef.m_Prefab];
				float radius = math.max(0f, netGeometryData.m_DefaultWidth * 0.5f + localConnectData.m_SearchDistance);
				if (nativeArray2.Length != 0)
				{
					Temp temp = nativeArray2[j];
					m_LocalConnectQueue.Enqueue(new LocalConnectItem(localConnectData.m_Layers, netData.m_ConnectLayers, node3.m_Position, radius, localConnectData.m_HeightRange, node2, temp.m_Flags, isPermanent: false, flag || localConnectData.m_SearchDistance == 0f || (netGeometryData.m_Flags & GeometryFlags.SubOwner) != 0));
				}
				else
				{
					m_LocalConnectQueue.Enqueue(new LocalConnectItem(localConnectData.m_Layers, netData.m_ConnectLayers, node3.m_Position, radius, localConnectData.m_HeightRange, node2, (TempFlags)0u, isPermanent: true, flag || localConnectData.m_SearchDistance == 0f || (netGeometryData.m_Flags & GeometryFlags.SubOwner) != 0));
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FillOldEdgesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Edge> m_EdgeType;

		[ReadOnly]
		public ComponentTypeHandle<EditorContainer> m_EditorContainerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public NativeHashMap<OldEdgeKey, Entity> m_OldEdgeMap;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<Temp> nativeArray3 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<Edge> nativeArray4 = chunk.GetNativeArray(ref m_EdgeType);
			NativeArray<EditorContainer> nativeArray5 = chunk.GetNativeArray(ref m_EditorContainerType);
			NativeArray<PrefabRef> nativeArray6 = chunk.GetNativeArray(ref m_PrefabRefType);
			OldEdgeKey key = default(OldEdgeKey);
			for (int i = 0; i < nativeArray6.Length; i++)
			{
				Edge edge = nativeArray4[i];
				key.m_Prefab = nativeArray6[i].m_Prefab;
				key.m_SubPrefab = Entity.Null;
				key.m_Original = nativeArray3[i].m_Original;
				key.m_Owner = Entity.Null;
				key.m_StartNode = edge.m_Start;
				key.m_EndNode = edge.m_End;
				if (CollectionUtils.TryGet(nativeArray5, i, out var value))
				{
					key.m_SubPrefab = value.m_Prefab;
				}
				if (CollectionUtils.TryGet(nativeArray2, i, out var value2))
				{
					key.m_Owner = value2.m_Owner;
				}
				m_OldEdgeMap.TryAdd(key, nativeArray[i]);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CheckDefinitionsJob : IJobChunk
	{
		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> m_CreationDefinitionType;

		public NativeParallelMultiHashMap<NodeMapKey, Entity>.ParallelWriter m_NodeMap;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<CreationDefinition> nativeArray = chunk.GetNativeArray(ref m_CreationDefinitionType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				CreationDefinition creationDefinition = nativeArray[i];
				if (!m_DeletedData.HasComponent(creationDefinition.m_Owner) && m_EdgeData.HasComponent(creationDefinition.m_Original))
				{
					m_NodeMap.Add(new NodeMapKey(creationDefinition.m_Original), Entity.Null);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CollectLocalConnectItemsJob : IJob
	{
		public NativeQueue<LocalConnectItem> m_LocalConnectQueue;

		public NativeList<LocalConnectItem> m_LocalConnectList;

		public void Execute()
		{
			int count = m_LocalConnectQueue.Count;
			m_LocalConnectList.ResizeUninitialized(count);
			for (int i = 0; i < count; i++)
			{
				m_LocalConnectList[i] = m_LocalConnectQueue.Dequeue();
			}
		}
	}

	[BurstCompile]
	private struct GenerateEdgesJob : IJobChunk
	{
		[ReadOnly]
		[DeallocateOnJobCompletion]
		public NativeArray<int> m_ChunkBaseEntityIndices;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Edge> m_EdgeType;

		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> m_CreationDefinitionType;

		[ReadOnly]
		public ComponentTypeHandle<OwnerDefinition> m_OwnerDefinitionType;

		[ReadOnly]
		public ComponentTypeHandle<NetCourse> m_NetCourseType;

		[ReadOnly]
		public ComponentTypeHandle<LocalCurveCache> m_LocalCurveCacheType;

		[ReadOnly]
		public ComponentTypeHandle<Upgraded> m_UpgradedType;

		[ReadOnly]
		public BufferTypeHandle<SubReplacement> m_SubReplacementType;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Native> m_NativeData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<Upgraded> m_UpgradedData;

		[ReadOnly]
		public ComponentLookup<Game.Net.BuildOrder> m_BuildOrderData;

		[ReadOnly]
		public ComponentLookup<TramTrack> m_TramTrackData;

		[ReadOnly]
		public ComponentLookup<EditorContainer> m_EditorContainerData;

		[ReadOnly]
		public ComponentLookup<Road> m_RoadData;

		[ReadOnly]
		public ComponentLookup<NetCondition> m_ConditionData;

		[ReadOnly]
		public ComponentLookup<Fixed> m_FixedData;

		[ReadOnly]
		public ComponentLookup<Aggregated> m_AggregatedData;

		[ReadOnly]
		public ComponentLookup<Roundabout> m_RoundaboutData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ElectricityConnection> m_ElectricityConnectionData;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> m_ServiceUpgradeData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetData> m_NetData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_NetGeometryData;

		[ReadOnly]
		public ComponentLookup<LocalConnectData> m_LocalConnectData;

		[ReadOnly]
		public ComponentLookup<TrackData> m_PrefabTrackData;

		[ReadOnly]
		public ComponentLookup<RoadData> m_PrefabRoadData;

		[ReadOnly]
		public ComponentLookup<ElectricityConnectionData> m_PrefabElectricityConnectionData;

		[ReadOnly]
		public ComponentLookup<PrefabData> m_PrefabData;

		[ReadOnly]
		public BufferLookup<ConnectedNode> m_ConnectedNodes;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<SubReplacement> m_SubReplacements;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public uint m_BuildOrder;

		[ReadOnly]
		public NativeParallelMultiHashMap<NodeMapKey, Entity> m_NodeMap;

		[ReadOnly]
		public NativeHashMap<OwnerDefinition, Entity> m_ReusedOwnerMap;

		[ReadOnly]
		public NativeHashMap<OldEdgeKey, Entity> m_OldEdgeMap;

		[ReadOnly]
		public NativeArray<LocalConnectItem> m_LocalConnectList;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			int num = m_ChunkBaseEntityIndices[unfilteredChunkIndex];
			NativeArray<CreationDefinition> nativeArray = chunk.GetNativeArray(ref m_CreationDefinitionType);
			if (nativeArray.Length != 0)
			{
				NativeArray<OwnerDefinition> nativeArray2 = chunk.GetNativeArray(ref m_OwnerDefinitionType);
				NativeArray<NetCourse> nativeArray3 = chunk.GetNativeArray(ref m_NetCourseType);
				NativeArray<LocalCurveCache> nativeArray4 = chunk.GetNativeArray(ref m_LocalCurveCacheType);
				NativeArray<Upgraded> nativeArray5 = chunk.GetNativeArray(ref m_UpgradedType);
				BufferAccessor<SubReplacement> bufferAccessor = chunk.GetBufferAccessor(ref m_SubReplacementType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					CreationDefinition definitionData = nativeArray[i];
					if (!m_DeletedData.HasComponent(definitionData.m_Owner))
					{
						NetCourse course = nativeArray3[i];
						CollectionUtils.TryGet(nativeArray2, i, out var value);
						CollectionUtils.TryGet(nativeArray5, i, out var value2);
						CollectionUtils.TryGet(nativeArray4, i, out var value3);
						CollectionUtils.TryGet(bufferAccessor, i, out var value4);
						GenerateEdge(unfilteredChunkIndex, num + i, definitionData, value, course, value2, value3, nativeArray4.Length != 0, value4);
					}
				}
				return;
			}
			NativeArray<Entity> nativeArray6 = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray7 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<Edge> nativeArray8 = chunk.GetNativeArray(ref m_EdgeType);
			if (nativeArray8.Length != 0)
			{
				if (nativeArray7.Length == 0)
				{
					for (int j = 0; j < nativeArray8.Length; j++)
					{
						UpdateNodeConnections(unfilteredChunkIndex, nativeArray6[j], nativeArray8[j]);
					}
				}
				return;
			}
			for (int k = 0; k < nativeArray7.Length; k++)
			{
				Temp temp = nativeArray7[k];
				if (m_ConnectedEdges.TryGetBuffer(temp.m_Original, out var bufferData))
				{
					for (int l = 0; l < bufferData.Length; l++)
					{
						Entity edge = bufferData[l].m_Edge;
						if (ShouldDuplicate(edge, temp.m_Original, temp.m_Flags))
						{
							DuplicateEdge(unfilteredChunkIndex, edge);
						}
					}
				}
				else if (m_EdgeData.HasComponent(temp.m_Original))
				{
					SplitEdge(unfilteredChunkIndex, temp.m_Original, nativeArray6[k]);
				}
			}
		}

		private void UpdateNodeConnections(int jobIndex, Entity edge, Edge edgeData)
		{
			DynamicBuffer<ConnectedNode> dynamicBuffer = m_ConnectedNodes[edge];
			DynamicBuffer<ConnectedNode> nodes = m_CommandBuffer.SetBuffer<ConnectedNode>(jobIndex, edge);
			Curve curveData = m_CurveData[edge];
			PrefabRef prefabRef = m_PrefabRefData[edge];
			NetData netData = m_NetData[prefabRef.m_Prefab];
			NetGeometryData netGeometryData = default(NetGeometryData);
			if (m_NetGeometryData.HasComponent(prefabRef.m_Prefab))
			{
				netGeometryData = m_NetGeometryData[prefabRef.m_Prefab];
			}
			bool isStandalone = true;
			if (m_OwnerData.HasComponent(edge))
			{
				isStandalone = false;
			}
			bool isZoneable = false;
			if (m_PrefabRoadData.HasComponent(prefabRef.m_Prefab))
			{
				isZoneable = (m_PrefabRoadData[prefabRef.m_Prefab].m_Flags & Game.Prefabs.RoadFlags.EnableZoning) != 0;
			}
			FindNodeConnections(nodes, edgeData, curveData, default(Temp), netData, netGeometryData, isPermanent: true, isStandalone, isZoneable);
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				ConnectedNode elem = dynamicBuffer[i];
				float3 position = m_NodeData[elem.m_Node].m_Position;
				if (!m_NodeMap.ContainsKey(new NodeMapKey(Entity.Null, position, isPermanent: true, m_EditorContainerData.HasComponent(elem.m_Node))))
				{
					nodes.Add(elem);
				}
			}
		}

		private bool ShouldDuplicate(Entity edge, Entity fromNode, TempFlags startTempFlags)
		{
			if (m_DeletedData.HasComponent(edge))
			{
				return false;
			}
			if (m_NodeMap.ContainsKey(new NodeMapKey(edge)))
			{
				return false;
			}
			Edge edge2 = m_EdgeData[edge];
			if (edge2.m_Start != fromNode)
			{
				return false;
			}
			if (!m_NodeMap.TryGetFirstValue(new NodeMapKey(edge2.m_End), out var item, out var it))
			{
				return false;
			}
			if ((startTempFlags & (TempFlags.Select | TempFlags.Modify | TempFlags.Regenerate | TempFlags.Upgrade)) != 0)
			{
				return true;
			}
			if (m_TempData.HasComponent(item) && (m_TempData[item].m_Flags & (TempFlags.Select | TempFlags.Modify | TempFlags.Regenerate | TempFlags.Upgrade)) != 0)
			{
				return true;
			}
			DynamicBuffer<ConnectedNode> dynamicBuffer = m_ConnectedNodes[edge];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				if (m_NodeMap.TryGetFirstValue(new NodeMapKey(dynamicBuffer[i].m_Node), out var item2, out it) && m_TempData.HasComponent(item2) && (m_TempData[item2].m_Flags & (TempFlags.Select | TempFlags.Modify | TempFlags.Regenerate | TempFlags.Upgrade)) != 0)
				{
					return true;
				}
			}
			return false;
		}

		private void DuplicateEdge(int jobIndex, Entity edge)
		{
			Edge edge2 = m_EdgeData[edge];
			Curve curve = m_CurveData[edge];
			if (!m_NodeMap.TryGetFirstValue(new NodeMapKey(edge2.m_Start), out edge2.m_Start, out var it) || !m_NodeMap.TryGetFirstValue(new NodeMapKey(edge2.m_End), out edge2.m_End, out it) || edge2.m_Start == edge2.m_End)
			{
				return;
			}
			PrefabRef component = m_PrefabRefData[edge];
			NetGeometryData netGeometryData = default(NetGeometryData);
			bool flag = false;
			bool flag2 = m_PrefabData.IsComponentEnabled(component.m_Prefab);
			if (m_NetGeometryData.HasComponent(component.m_Prefab))
			{
				netGeometryData = m_NetGeometryData[component.m_Prefab];
				flag = true;
			}
			Temp temp = new Temp
			{
				m_Original = edge
			};
			if (m_TempData.TryGetComponent(edge2.m_Start, out var componentData) && ((componentData.m_Flags & (TempFlags.Upgrade | TempFlags.Parent)) == (TempFlags.Upgrade | TempFlags.Parent) || (componentData.m_Flags & TempFlags.Select) != 0))
			{
				temp.m_Flags |= TempFlags.SubDetail;
			}
			if (m_TempData.TryGetComponent(edge2.m_End, out var componentData2) && ((componentData2.m_Flags & (TempFlags.Upgrade | TempFlags.Parent)) == (TempFlags.Upgrade | TempFlags.Parent) || (componentData2.m_Flags & TempFlags.Select) != 0))
			{
				temp.m_Flags |= TempFlags.SubDetail;
			}
			Composition component2 = new Composition
			{
				m_Edge = component.m_Prefab,
				m_StartNode = component.m_Prefab,
				m_EndNode = component.m_Prefab
			};
			NetData netData = m_NetData[component.m_Prefab];
			m_EditorContainerData.TryGetComponent(edge, out var componentData3);
			m_OwnerData.TryGetComponent(edge, out var componentData4);
			OwnerDefinition ownerDefinition = default(OwnerDefinition);
			Entity oldEntity;
			bool flag3 = TryGetOldEntity(edge2, component.m_Prefab, componentData3.m_Prefab, edge, ref ownerDefinition, ref componentData4.m_Owner, out oldEntity);
			if (flag3)
			{
				m_CommandBuffer.RemoveComponent<Deleted>(jobIndex, oldEntity);
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, default(Updated));
			}
			else
			{
				oldEntity = m_CommandBuffer.CreateEntity(jobIndex, netData.m_EdgeArchetype);
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, component);
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, edge2);
				if (!m_ServiceUpgradeData.HasComponent(edge))
				{
					m_CommandBuffer.RemoveComponent<Game.Buildings.ServiceUpgrade>(jobIndex, oldEntity);
				}
			}
			m_CommandBuffer.SetComponent(jobIndex, oldEntity, curve);
			if (flag)
			{
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, component2);
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, m_BuildOrderData[edge]);
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, m_PseudoRandomSeedData[edge]);
			}
			if (m_ElevationData.TryGetComponent(edge, out var componentData5))
			{
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, componentData5);
			}
			if (m_UpgradedData.TryGetComponent(edge, out var componentData6))
			{
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, componentData6);
			}
			else if (flag3 && m_UpgradedData.HasComponent(oldEntity))
			{
				m_CommandBuffer.RemoveComponent<Upgraded>(jobIndex, oldEntity);
			}
			if (m_SubReplacements.TryGetBuffer(edge, out var bufferData))
			{
				m_CommandBuffer.AddBuffer<SubReplacement>(jobIndex, oldEntity).CopyFrom(bufferData);
			}
			else if (flag3 && m_SubReplacements.HasBuffer(oldEntity))
			{
				m_CommandBuffer.RemoveComponent<SubReplacement>(jobIndex, oldEntity);
			}
			bool isStandalone = true;
			if (componentData4.m_Owner != Entity.Null)
			{
				isStandalone = false;
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, componentData4);
				if (m_CurveData.TryGetComponent(componentData4.m_Owner, out var componentData7) && m_PrefabRefData.TryGetComponent(componentData4.m_Owner, out var componentData8))
				{
					m_CommandBuffer.AddComponent(jobIndex, oldEntity, new OwnerDefinition
					{
						m_Prefab = componentData8.m_Prefab,
						m_Position = componentData7.m_Bezier.a,
						m_Rotation = new float4(componentData7.m_Bezier.d, 0f)
					});
				}
			}
			if (m_FixedData.HasComponent(edge))
			{
				Fixed component3 = m_FixedData[edge];
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, component3);
			}
			if (netGeometryData.m_AggregateType != Entity.Null && m_AggregatedData.TryGetComponent(edge, out var componentData9))
			{
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, componentData9);
			}
			if (flag2 && m_ConditionData.HasComponent(edge))
			{
				NetCondition component4 = m_ConditionData[edge];
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, component4);
			}
			if (!m_PrefabTrackData.HasComponent(component.m_Prefab))
			{
				if (m_TramTrackData.HasComponent(edge))
				{
					m_CommandBuffer.AddComponent(jobIndex, oldEntity, default(TramTrack));
				}
				else if (flag3 && m_TramTrackData.HasComponent(oldEntity))
				{
					m_CommandBuffer.RemoveComponent<TramTrack>(jobIndex, oldEntity);
				}
			}
			bool isZoneable = false;
			if (m_PrefabRoadData.HasComponent(component.m_Prefab))
			{
				isZoneable = (m_PrefabRoadData[component.m_Prefab].m_Flags & Game.Prefabs.RoadFlags.EnableZoning) != 0;
				if (flag2 && m_RoadData.HasComponent(edge))
				{
					Road component5 = m_RoadData[edge];
					m_CommandBuffer.SetComponent(jobIndex, oldEntity, component5);
				}
			}
			if (componentData3.m_Prefab != Entity.Null)
			{
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, componentData3);
				if (!flag && m_PseudoRandomSeedData.TryGetComponent(edge, out var componentData10))
				{
					m_CommandBuffer.SetComponent(jobIndex, oldEntity, componentData10);
				}
			}
			if (m_NativeData.HasComponent(edge))
			{
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, default(Native));
			}
			if (m_ElectricityConnectionData.HasComponent(edge))
			{
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, default(Game.Net.ElectricityConnection));
			}
			DynamicBuffer<ConnectedNode> dynamicBuffer = m_ConnectedNodes[edge];
			DynamicBuffer<ConnectedNode> nodes = m_CommandBuffer.SetBuffer<ConnectedNode>(jobIndex, oldEntity);
			FindNodeConnections(nodes, edge2, curve, temp, netData, netGeometryData, isPermanent: false, isStandalone, isZoneable);
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				ConnectedNode elem = dynamicBuffer[i];
				if (!m_NodeMap.TryGetFirstValue(new NodeMapKey(elem.m_Node), out var _, out it))
				{
					nodes.Add(elem);
				}
			}
			m_CommandBuffer.AddComponent(jobIndex, oldEntity, temp);
			m_CommandBuffer.AddComponent(jobIndex, edge, default(Hidden));
			m_CommandBuffer.AddComponent(jobIndex, edge, default(BatchesUpdated));
		}

		private bool TryGetNodes(int jobIndex, Entity edge, Entity middleNode, Edge edgeData, out Entity start, out Entity end, out float3 curveRange)
		{
			start = (end = Entity.Null);
			float2 @float = new float2(float.MinValue, float.MaxValue);
			Temp component = m_TempData[middleNode];
			curveRange = new float3(0f, component.m_CurvePosition, 1f);
			if (m_NodeMap.TryGetFirstValue(new NodeMapKey(edge), out var item, out var it))
			{
				do
				{
					if (!(item != middleNode) || !m_TempData.TryGetComponent(item, out var componentData))
					{
						continue;
					}
					float num = componentData.m_CurvePosition - component.m_CurvePosition;
					if (num < 0f)
					{
						if (num > @float.x)
						{
							start = item;
							curveRange.x = componentData.m_CurvePosition;
							@float.x = num;
						}
					}
					else if (num > 0f && num < @float.y)
					{
						end = item;
						curveRange.z = componentData.m_CurvePosition;
						@float.y = num;
					}
				}
				while (m_NodeMap.TryGetNextValue(out item, ref it));
			}
			NativeParallelMultiHashMapIterator<NodeMapKey> it2;
			if (start == Entity.Null)
			{
				if (!m_NodeMap.TryGetFirstValue(new NodeMapKey(edgeData.m_Start), out start, out it2))
				{
					return false;
				}
			}
			else
			{
				start = Entity.Null;
				component.m_Original = Entity.Null;
				m_CommandBuffer.SetComponent(jobIndex, middleNode, component);
			}
			if (end == Entity.Null && !m_NodeMap.TryGetFirstValue(new NodeMapKey(edgeData.m_End), out end, out it2))
			{
				return false;
			}
			return true;
		}

		private void SplitEdge(int jobIndex, Entity edge, Entity middleNode)
		{
			Edge edgeData = m_EdgeData[edge];
			Curve curve = m_CurveData[edge];
			Edge edge2 = default(Edge);
			Edge edge3 = default(Edge);
			edge2.m_End = middleNode;
			edge3.m_Start = middleNode;
			if (!TryGetNodes(jobIndex, edge, middleNode, edgeData, out edge2.m_Start, out edge3.m_End, out var curveRange) || edge2.m_Start == edge2.m_End || edge3.m_Start == edge3.m_End)
			{
				return;
			}
			PrefabRef prefabRef = m_PrefabRefData[edge];
			NetData netData = m_NetData[prefabRef.m_Prefab];
			NetGeometryData componentData;
			bool flag = m_NetGeometryData.TryGetComponent(prefabRef.m_Prefab, out componentData);
			Curve curveData = default(Curve);
			Curve curveData2 = default(Curve);
			Game.Net.BuildOrder buildOrderData = default(Game.Net.BuildOrder);
			Game.Net.BuildOrder buildOrderData2 = default(Game.Net.BuildOrder);
			if (flag)
			{
				Game.Net.BuildOrder buildOrder = m_BuildOrderData[edge];
				if (curveRange.x > 0f)
				{
					buildOrderData.m_Start = buildOrder.m_Start + (uint)((float)(buildOrder.m_End - buildOrder.m_Start) * curveRange.x) + 1;
				}
				else
				{
					buildOrderData.m_Start = buildOrder.m_Start;
				}
				buildOrderData.m_End = buildOrder.m_Start + (uint)((float)(buildOrder.m_End - buildOrder.m_Start) * curveRange.y);
				buildOrderData2.m_Start = buildOrderData.m_End + 1;
				if (curveRange.z < 1f)
				{
					buildOrderData2.m_End = buildOrder.m_Start + (uint)((float)(buildOrder.m_End - buildOrder.m_Start) * curveRange.z);
				}
				else
				{
					buildOrderData2.m_End = buildOrder.m_End;
				}
				if (buildOrderData.m_Start > buildOrderData.m_End)
				{
					buildOrderData.m_Start = buildOrderData.m_End;
				}
				if (buildOrderData2.m_Start > buildOrderData2.m_End)
				{
					buildOrderData2.m_Start = buildOrderData2.m_End;
				}
			}
			PrefabRef prefabRef2 = m_PrefabRefData[middleNode];
			NetData netData2 = m_NetData[prefabRef2.m_Prefab];
			float3 @float = 0f;
			bool3 @bool = false;
			@bool.y = ((netData.m_RequiredLayers ^ netData2.m_RequiredLayers) & Layer.Waterway) == 0;
			if (@bool.y)
			{
				@float.y = m_NodeData[middleNode].m_Position.y;
			}
			if (edge2.m_Start != Entity.Null && curveRange.x > 0f)
			{
				prefabRef2 = m_PrefabRefData[edge2.m_Start];
				netData2 = m_NetData[prefabRef2.m_Prefab];
				@bool.x = ((netData.m_RequiredLayers ^ netData2.m_RequiredLayers) & Layer.Waterway) == 0;
				if (@bool.x)
				{
					@float.x = m_NodeData[edge2.m_Start].m_Position.y;
				}
			}
			if (edge3.m_End != Entity.Null && curveRange.z < 1f)
			{
				prefabRef2 = m_PrefabRefData[edge3.m_End];
				netData2 = m_NetData[prefabRef2.m_Prefab];
				@bool.z = ((netData.m_RequiredLayers ^ netData2.m_RequiredLayers) & Layer.Waterway) == 0;
				if (@bool.z)
				{
					@float.z = m_NodeData[edge3.m_End].m_Position.y;
				}
			}
			if (!m_FixedData.TryGetComponent(edge, out var componentData2))
			{
				componentData2 = new Fixed
				{
					m_Index = -1
				};
			}
			if ((componentData.m_Flags & GeometryFlags.StraightEdges) != 0 && componentData2.m_Index < 0)
			{
				float3 float2 = MathUtils.Position(curve.m_Bezier, curveRange.y);
				float2.y = math.select(float2.y, @float.y, @bool.y);
				if (edge2.m_Start != Entity.Null)
				{
					if (curveRange.x > 0f)
					{
						float3 startPos = MathUtils.Position(curve.m_Bezier, curveRange.x);
						startPos.y = math.select(startPos.y, @float.x, @bool.x);
						curveData.m_Bezier = NetUtils.StraightCurve(startPos, float2, componentData.m_Hanging);
					}
					else
					{
						curveData.m_Bezier = NetUtils.StraightCurve(curve.m_Bezier.a, float2, componentData.m_Hanging);
					}
				}
				if (edge3.m_End != Entity.Null)
				{
					if (curveRange.z < 1f)
					{
						float3 endPos = MathUtils.Position(curve.m_Bezier, curveRange.z);
						endPos.y = math.select(endPos.y, @float.z, @bool.z);
						curveData2.m_Bezier = NetUtils.StraightCurve(float2, endPos, componentData.m_Hanging);
					}
					else
					{
						curveData2.m_Bezier = NetUtils.StraightCurve(float2, curve.m_Bezier.d, componentData.m_Hanging);
					}
				}
			}
			else
			{
				MathUtils.Divide(curve.m_Bezier, out curveData.m_Bezier, out curveData2.m_Bezier, curveRange.y);
				if (edge2.m_Start != Entity.Null && curveRange.x > 0f)
				{
					curveData.m_Bezier = MathUtils.Cut(curve.m_Bezier, curveRange.xy);
					curveData.m_Bezier.a.y = math.select(curveData.m_Bezier.a.y, @float.x, @bool.x);
				}
				if (edge3.m_End != Entity.Null && curveRange.z < 1f)
				{
					curveData2.m_Bezier = MathUtils.Cut(curve.m_Bezier, curveRange.yz);
					curveData2.m_Bezier.d.y = math.select(curveData2.m_Bezier.d.y, @float.z, @bool.z);
				}
				curveData.m_Bezier.d.y = math.select(curveData.m_Bezier.d.y, @float.y, @bool.y);
				curveData2.m_Bezier.a.y = math.select(curveData2.m_Bezier.a.y, @float.y, @bool.y);
			}
			curveData.m_Length = MathUtils.Length(curveData.m_Bezier);
			curveData2.m_Length = MathUtils.Length(curveData2.m_Bezier);
			DynamicBuffer<ConnectedNode> oldNodes = m_ConnectedNodes[edge];
			m_ElevationData.TryGetComponent(edge, out var componentData3);
			m_UpgradedData.TryGetComponent(edge, out var componentData4);
			m_SubReplacements.TryGetBuffer(edge, out var bufferData);
			m_OwnerData.TryGetComponent(edge, out var componentData5);
			Aggregated componentData6 = default(Aggregated);
			if (componentData.m_AggregateType != Entity.Null)
			{
				m_AggregatedData.TryGetComponent(edge, out componentData6);
			}
			NetCondition condition = default(NetCondition);
			NetCondition condition2 = default(NetCondition);
			if (m_ConditionData.TryGetComponent(edge, out var componentData7))
			{
				if (curveRange.x > 0f)
				{
					condition.m_Wear.x = math.lerp(componentData7.m_Wear.x, componentData7.m_Wear.y, curveRange.x);
				}
				else
				{
					condition.m_Wear.x = componentData7.m_Wear.x;
				}
				condition.m_Wear.y = math.lerp(condition.m_Wear.x, condition.m_Wear.y, curveRange.y);
				condition2.m_Wear.x = condition.m_Wear.y;
				if (curveRange.z < 1f)
				{
					condition2.m_Wear.y = math.lerp(componentData7.m_Wear.x, componentData7.m_Wear.y, curveRange.z);
				}
				else
				{
					condition2.m_Wear.y = componentData7.m_Wear.y;
				}
			}
			bool addTramTrack = m_TramTrackData.HasComponent(edge) && !m_PrefabTrackData.HasComponent(prefabRef.m_Prefab);
			bool addNative = m_NativeData.HasComponent(edge);
			bool addElectricityConnection = m_ElectricityConnectionData.HasComponent(edge);
			bool serviceUpgrade = m_ServiceUpgradeData.HasComponent(edge);
			Road road = default(Road);
			Road road2 = default(Road);
			if (m_RoadData.TryGetComponent(edge, out var componentData8))
			{
				road = componentData8;
				road2 = componentData8;
				if (curveRange.x > 0f)
				{
					road.m_TrafficFlowDistance0 = math.lerp(componentData8.m_TrafficFlowDistance0, componentData8.m_TrafficFlowDistance1, curveRange.x);
					road.m_TrafficFlowDuration0 = math.lerp(componentData8.m_TrafficFlowDuration0, componentData8.m_TrafficFlowDuration1, curveRange.x);
				}
				road.m_TrafficFlowDistance1 = math.lerp(componentData8.m_TrafficFlowDistance0, componentData8.m_TrafficFlowDistance1, curveRange.y);
				road.m_TrafficFlowDuration1 = math.lerp(componentData8.m_TrafficFlowDuration0, componentData8.m_TrafficFlowDuration1, curveRange.y);
				road2.m_TrafficFlowDistance0 = road.m_TrafficFlowDistance1;
				road2.m_TrafficFlowDuration0 = road.m_TrafficFlowDuration1;
				if (curveRange.z < 1f)
				{
					road2.m_TrafficFlowDistance1 = math.lerp(componentData8.m_TrafficFlowDistance0, componentData8.m_TrafficFlowDistance1, curveRange.z);
					road2.m_TrafficFlowDuration1 = math.lerp(componentData8.m_TrafficFlowDuration0, componentData8.m_TrafficFlowDuration1, curveRange.z);
				}
			}
			PseudoRandomSeed pseudoRandomSeed = default(PseudoRandomSeed);
			PseudoRandomSeed pseudoRandomSeed2 = default(PseudoRandomSeed);
			if (m_PseudoRandomSeedData.HasComponent(edge))
			{
				Unity.Mathematics.Random random = m_PseudoRandomSeedData[edge].GetRandom(PseudoRandomSeed.kSplitEdge);
				pseudoRandomSeed = new PseudoRandomSeed(ref random);
				pseudoRandomSeed2 = new PseudoRandomSeed(ref random);
			}
			EditorContainer editorContainer = default(EditorContainer);
			if (m_EditorContainerData.HasComponent(edge))
			{
				editorContainer = m_EditorContainerData[edge];
			}
			if ((componentData.m_Flags & GeometryFlags.SnapCellSize) != 0)
			{
				if (curveRange.x > 0f)
				{
					MathUtils.Divide(curve.m_Bezier, out var output, out var _, curveRange.x);
					if (((int)math.round(MathUtils.Length(output) / 4f) & 1) != 0 != ((componentData8.m_Flags & Game.Net.RoadFlags.StartHalfAligned) != 0))
					{
						road.m_Flags |= Game.Net.RoadFlags.StartHalfAligned;
					}
					else
					{
						road.m_Flags &= ~Game.Net.RoadFlags.StartHalfAligned;
					}
				}
				if (((int)math.round(curveData.m_Length / 4f) & 1) != 0 != ((road.m_Flags & Game.Net.RoadFlags.StartHalfAligned) != 0))
				{
					road.m_Flags |= Game.Net.RoadFlags.EndHalfAligned;
					road2.m_Flags |= Game.Net.RoadFlags.StartHalfAligned;
				}
				else
				{
					road.m_Flags &= ~Game.Net.RoadFlags.EndHalfAligned;
					road2.m_Flags &= ~Game.Net.RoadFlags.StartHalfAligned;
				}
				if (curveRange.z < 1f)
				{
					if (((int)math.round(curveData2.m_Length / 4f) & 1) != 0 != ((road2.m_Flags & Game.Net.RoadFlags.StartHalfAligned) != 0))
					{
						road2.m_Flags |= Game.Net.RoadFlags.EndHalfAligned;
					}
					else
					{
						road2.m_Flags &= ~Game.Net.RoadFlags.EndHalfAligned;
					}
				}
			}
			if (edge2.m_Start != Entity.Null)
			{
				CreateTempEdge(jobIndex, edge2, curveData, componentData3, componentData4, bufferData, componentData5, componentData2, componentData6, condition, addTramTrack, addNative, addElectricityConnection, flag, serviceUpgrade, road, pseudoRandomSeed, prefabRef, componentData, buildOrderData, editorContainer, oldNodes);
			}
			if (edge3.m_End != Entity.Null)
			{
				CreateTempEdge(jobIndex, edge3, curveData2, componentData3, componentData4, bufferData, componentData5, componentData2, componentData6, condition2, addTramTrack, addNative, addElectricityConnection, flag, serviceUpgrade, road2, pseudoRandomSeed2, prefabRef, componentData, buildOrderData2, editorContainer, oldNodes);
			}
		}

		private void CreateTempEdge(int jobIndex, Edge edge, Curve curveData, Elevation elevation, Upgraded upgraded, DynamicBuffer<SubReplacement> subReplacements, Owner owner, Fixed fixedData, Aggregated aggregated, NetCondition condition, bool addTramTrack, bool addNative, bool addElectricityConnection, bool hasGeometry, bool serviceUpgrade, Road road, PseudoRandomSeed pseudoRandomSeed, PrefabRef prefabRef, NetGeometryData netGeometryData, Game.Net.BuildOrder buildOrderData, EditorContainer editorContainer, DynamicBuffer<ConnectedNode> oldNodes)
		{
			bool flag = m_PrefabData.IsComponentEnabled(prefabRef.m_Prefab);
			Composition component = new Composition
			{
				m_Edge = prefabRef.m_Prefab,
				m_StartNode = prefabRef.m_Prefab,
				m_EndNode = prefabRef.m_Prefab
			};
			Temp temp = default(Temp);
			temp.m_Flags |= TempFlags.Essential;
			NetData netData = m_NetData[prefabRef.m_Prefab];
			OwnerDefinition ownerDefinition = default(OwnerDefinition);
			if (TryGetOldEntity(edge, prefabRef.m_Prefab, editorContainer.m_Prefab, Entity.Null, ref ownerDefinition, ref owner.m_Owner, out var oldEntity))
			{
				m_CommandBuffer.RemoveComponent<Deleted>(jobIndex, oldEntity);
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, default(Updated));
			}
			else
			{
				oldEntity = m_CommandBuffer.CreateEntity(jobIndex, netData.m_EdgeArchetype);
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, prefabRef);
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, edge);
				if (!serviceUpgrade)
				{
					m_CommandBuffer.RemoveComponent<Game.Buildings.ServiceUpgrade>(jobIndex, oldEntity);
				}
			}
			m_CommandBuffer.SetComponent(jobIndex, oldEntity, curveData);
			if (hasGeometry)
			{
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, component);
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, buildOrderData);
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, pseudoRandomSeed);
			}
			if (math.any(elevation.m_Elevation != 0f))
			{
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, elevation);
			}
			if (upgraded.m_Flags != default(CompositionFlags))
			{
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, upgraded);
			}
			if (subReplacements.IsCreated)
			{
				m_CommandBuffer.AddBuffer<SubReplacement>(jobIndex, oldEntity).CopyFrom(subReplacements);
			}
			bool flag2 = true;
			if (owner.m_Owner != Entity.Null)
			{
				flag2 = false;
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, owner);
			}
			if (fixedData.m_Index >= 0)
			{
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, fixedData);
			}
			if (aggregated.m_Aggregate != Entity.Null)
			{
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, aggregated);
			}
			if (flag && math.any(condition.m_Wear != 0f))
			{
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, condition);
			}
			if (addTramTrack)
			{
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, default(TramTrack));
			}
			if (addNative)
			{
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, default(Native));
			}
			if (addElectricityConnection)
			{
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, default(Game.Net.ElectricityConnection));
			}
			if (editorContainer.m_Prefab != Entity.Null)
			{
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, editorContainer);
				if (!hasGeometry)
				{
					m_CommandBuffer.SetComponent(jobIndex, oldEntity, pseudoRandomSeed);
				}
			}
			bool flag3 = false;
			if (m_PrefabRoadData.HasComponent(prefabRef.m_Prefab))
			{
				if (flag)
				{
					m_CommandBuffer.SetComponent(jobIndex, oldEntity, road);
				}
				flag3 = (m_PrefabRoadData[prefabRef.m_Prefab].m_Flags & Game.Prefabs.RoadFlags.EnableZoning) != 0;
			}
			DynamicBuffer<ConnectedNode> nodes = m_CommandBuffer.SetBuffer<ConnectedNode>(jobIndex, oldEntity);
			FindNodeConnections(nodes, edge, curveData, temp, netData, netGeometryData, isPermanent: false, flag2, flag3);
			for (int i = 0; i < oldNodes.Length; i++)
			{
				ConnectedNode connectedNode = oldNodes[i];
				if (m_NodeMap.TryGetFirstValue(new NodeMapKey(connectedNode.m_Node), out var item, out var _))
				{
					continue;
				}
				Node node = m_NodeData[item];
				PrefabRef prefabRef2 = m_PrefabRefData[item];
				LocalConnectData localConnectData = m_LocalConnectData[prefabRef2.m_Prefab];
				NetGeometryData netGeometryData2 = m_NetGeometryData[prefabRef2.m_Prefab];
				float num = math.max(0f, netGeometryData2.m_DefaultWidth * 0.5f + localConnectData.m_SearchDistance);
				float t;
				float num2 = MathUtils.Distance(curveData.m_Bezier.xz, node.m_Position.xz, out t);
				if (m_OwnerData.HasComponent(item) && localConnectData.m_SearchDistance != 0f && (netGeometryData2.m_Flags & GeometryFlags.SubOwner) == 0 && flag2 && flag3)
				{
					num2 -= 8f;
				}
				if (num2 <= netGeometryData.m_DefaultWidth * 0.5f + num)
				{
					float position = MathUtils.Position(curveData.m_Bezier, t).y - node.m_Position.y;
					if (MathUtils.Intersect(localConnectData.m_HeightRange, position))
					{
						nodes.Add(new ConnectedNode(item, t));
					}
				}
			}
			m_CommandBuffer.AddComponent(jobIndex, oldEntity, temp);
		}

		private bool TryGetNode(CoursePos coursePos, bool isPermanent, bool isEditor, out Entity node)
		{
			if (isPermanent && m_NodeData.HasComponent(coursePos.m_Entity))
			{
				node = coursePos.m_Entity;
				return true;
			}
			float num = float.MaxValue;
			node = Entity.Null;
			if (m_NodeMap.TryGetFirstValue(new NodeMapKey(coursePos, isPermanent, isEditor), out var item, out var it))
			{
				do
				{
					if (m_TempData.TryGetComponent(item, out var componentData))
					{
						float num2 = math.abs(componentData.m_CurvePosition - coursePos.m_SplitPosition);
						if (num2 < num)
						{
							num = num2;
							node = item;
						}
					}
					else if (item != Entity.Null)
					{
						node = item;
						return true;
					}
				}
				while (m_NodeMap.TryGetNextValue(out item, ref it));
			}
			return node != Entity.Null;
		}

		private void GenerateEdge(int jobIndex, int entityIndex, CreationDefinition definitionData, OwnerDefinition ownerData, NetCourse course, Upgraded upgraded, LocalCurveCache cachedCurve, bool hasCachedCurve, DynamicBuffer<SubReplacement> subReplacements)
		{
			bool flag = (definitionData.m_Flags & CreationFlags.Permanent) != 0;
			bool isEditor = definitionData.m_SubPrefab != Entity.Null;
			Edge edge = default(Edge);
			if (((course.m_StartPosition.m_Flags | course.m_EndPosition.m_Flags) & CoursePosFlags.DontCreate) != 0 || !TryGetNode(course.m_StartPosition, flag, isEditor, out edge.m_Start) || !TryGetNode(course.m_EndPosition, flag, isEditor, out edge.m_End) || edge.m_Start == edge.m_End)
			{
				return;
			}
			if (definitionData.m_Original != Entity.Null && m_NodeMap.TryGetFirstValue(new NodeMapKey(definitionData.m_Original), out var item, out var it))
			{
				do
				{
					if (item != Entity.Null)
					{
						definitionData.m_Original = Entity.Null;
						break;
					}
				}
				while (m_NodeMap.TryGetNextValue(out item, ref it));
			}
			if (course.m_StartPosition.m_Entity != Entity.Null && course.m_EndPosition.m_Entity != Entity.Null && definitionData.m_Original == Entity.Null && ConnectionExists(course.m_StartPosition.m_Entity, course.m_EndPosition.m_Entity))
			{
				return;
			}
			PrefabRef prefabRef = default(PrefabRef);
			PrefabRef prefabRef2 = default(PrefabRef);
			if (definitionData.m_Original != Entity.Null)
			{
				prefabRef2 = m_PrefabRefData[definitionData.m_Original];
			}
			if (definitionData.m_Prefab == Entity.Null)
			{
				if (!(definitionData.m_Original != Entity.Null))
				{
					return;
				}
				prefabRef = prefabRef2;
			}
			else
			{
				prefabRef.m_Prefab = definitionData.m_Prefab;
			}
			bool flag2 = m_PrefabData.IsComponentEnabled(prefabRef.m_Prefab);
			Composition component = new Composition
			{
				m_Edge = prefabRef.m_Prefab,
				m_StartNode = prefabRef.m_Prefab,
				m_EndNode = prefabRef.m_Prefab
			};
			NetData netData = m_NetData[prefabRef.m_Prefab];
			NetGeometryData netGeometryData = default(NetGeometryData);
			bool flag3 = false;
			if (m_NetGeometryData.HasComponent(prefabRef.m_Prefab))
			{
				netGeometryData = m_NetGeometryData[prefabRef.m_Prefab];
				flag3 = true;
			}
			float3 @float;
			float3 float2;
			if ((netGeometryData.m_Flags & GeometryFlags.StrictNodes) != 0 || !flag3)
			{
				@float = m_NodeData[edge.m_Start].m_Position;
				float2 = m_NodeData[edge.m_End].m_Position;
			}
			else
			{
				@float = MathUtils.Position(course.m_Curve, course.m_StartPosition.m_CourseDelta);
				float2 = MathUtils.Position(course.m_Curve, course.m_EndPosition.m_CourseDelta);
				PrefabRef prefabRef3 = m_PrefabRefData[edge.m_Start];
				PrefabRef prefabRef4 = m_PrefabRefData[edge.m_End];
				NetData netData2 = m_NetData[prefabRef3.m_Prefab];
				NetData netData3 = m_NetData[prefabRef4.m_Prefab];
				if (((netData.m_RequiredLayers ^ netData2.m_RequiredLayers) & Layer.Waterway) == 0)
				{
					@float.y = m_NodeData[edge.m_Start].m_Position.y;
				}
				if (((netData.m_RequiredLayers ^ netData3.m_RequiredLayers) & Layer.Waterway) == 0)
				{
					float2.y = m_NodeData[edge.m_End].m_Position.y;
				}
			}
			Curve curve = default(Curve);
			if ((netGeometryData.m_Flags & GeometryFlags.StraightEdges) != 0 && course.m_FixedIndex < 0)
			{
				curve.m_Bezier = NetUtils.StraightCurve(@float, float2, netGeometryData.m_Hanging);
			}
			else
			{
				curve.m_Bezier = MathUtils.Cut(course.m_Curve, new float2(course.m_StartPosition.m_CourseDelta, course.m_EndPosition.m_CourseDelta));
				curve.m_Bezier.a = @float;
				curve.m_Bezier.d = float2;
			}
			curve.m_Length = MathUtils.Length(curve.m_Bezier);
			Temp temp = default(Temp);
			bool flag4 = false;
			if (definitionData.m_Original != Entity.Null)
			{
				temp.m_Original = definitionData.m_Original;
				if ((definitionData.m_Flags & CreationFlags.Delete) != 0)
				{
					temp.m_Flags |= TempFlags.Delete;
				}
				else if ((definitionData.m_Flags & CreationFlags.Select) != 0)
				{
					temp.m_Flags |= TempFlags.Select;
				}
				else if ((definitionData.m_Flags & CreationFlags.Upgrade) != 0)
				{
					temp.m_Flags |= TempFlags.Upgrade;
				}
				else if (prefabRef.m_Prefab != prefabRef2.m_Prefab || (definitionData.m_Flags & CreationFlags.Invert) != 0)
				{
					temp.m_Flags |= TempFlags.Modify;
				}
				if ((definitionData.m_Flags & CreationFlags.Parent) != 0)
				{
					temp.m_Flags |= TempFlags.Parent;
				}
				if ((definitionData.m_Flags & CreationFlags.Upgrade) == 0)
				{
					if (m_UpgradedData.TryGetComponent(definitionData.m_Original, out var componentData))
					{
						upgraded = componentData;
						if ((definitionData.m_Flags & CreationFlags.Invert) != 0)
						{
							upgraded.m_Flags = NetCompositionHelpers.InvertCompositionFlags(upgraded.m_Flags);
						}
					}
					if (m_SubReplacements.TryGetBuffer(definitionData.m_Original, out var bufferData))
					{
						subReplacements = bufferData;
						flag4 = (definitionData.m_Flags & CreationFlags.Invert) != 0;
					}
				}
			}
			else
			{
				temp.m_Flags |= TempFlags.Create;
			}
			if ((definitionData.m_Flags & CreationFlags.Hidden) != 0)
			{
				temp.m_Flags |= TempFlags.Hidden;
			}
			if (definitionData.m_Original != Entity.Null)
			{
				course.m_Elevation = 0f;
				course.m_FixedIndex = -1;
				if (m_ElevationData.HasComponent(definitionData.m_Original))
				{
					course.m_Elevation = m_ElevationData[definitionData.m_Original].m_Elevation;
				}
				if (m_FixedData.HasComponent(definitionData.m_Original))
				{
					course.m_FixedIndex = m_FixedData[definitionData.m_Original].m_Index;
				}
				if ((definitionData.m_Flags & CreationFlags.Invert) != 0)
				{
					course.m_Elevation = course.m_Elevation.yx;
				}
			}
			int num;
			if (!math.any(course.m_Elevation != 0f))
			{
				if (course.m_StartPosition.m_ParentMesh >= 0)
				{
					num = ((course.m_EndPosition.m_ParentMesh >= 0) ? 1 : 0);
					if (num != 0)
					{
						goto IL_0745;
					}
				}
				else
				{
					num = 0;
				}
				if ((netGeometryData.m_Flags & GeometryFlags.FlattenTerrain) == 0 || ownerData.m_Prefab != Entity.Null || definitionData.m_Owner != Entity.Null)
				{
					bool flag5 = m_ElevationData.HasComponent(edge.m_Start);
					bool flag6 = m_ElevationData.HasComponent(edge.m_End);
					Curve curve2 = (((netGeometryData.m_Flags & GeometryFlags.OnWater) == 0) ? NetUtils.AdjustPosition(curve, flag5, flag5 || flag6, flag6, ref m_TerrainHeightData) : NetUtils.AdjustPosition(curve, flag5, flag5 || flag6, flag6, ref m_TerrainHeightData, ref m_WaterSurfaceData));
					if (math.any(math.abs(curve2.m_Bezier.y.abcd - curve.m_Bezier.y.abcd) >= 0.01f))
					{
						curve = curve2;
					}
				}
			}
			else
			{
				num = 1;
			}
			goto IL_0745;
			IL_082c:
			Entity oldEntity;
			m_CommandBuffer.SetComponent(jobIndex, oldEntity, curve);
			if (flag3)
			{
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, component);
				if (m_PseudoRandomSeedData.TryGetComponent(definitionData.m_Original, out var componentData2))
				{
					m_CommandBuffer.SetComponent(jobIndex, oldEntity, componentData2);
				}
				else
				{
					m_CommandBuffer.SetComponent(jobIndex, oldEntity, new PseudoRandomSeed((ushort)definitionData.m_RandomSeed));
				}
			}
			bool flag7;
			if (num != 0)
			{
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, new Elevation(course.m_Elevation));
			}
			else if (flag7 && m_ElevationData.HasComponent(oldEntity))
			{
				m_CommandBuffer.RemoveComponent<Elevation>(jobIndex, oldEntity);
			}
			if (upgraded.m_Flags != default(CompositionFlags))
			{
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, upgraded);
			}
			else if (flag7 && m_UpgradedData.HasComponent(oldEntity))
			{
				m_CommandBuffer.RemoveComponent<Upgraded>(jobIndex, oldEntity);
			}
			if (subReplacements.IsCreated && subReplacements.Length != 0)
			{
				DynamicBuffer<SubReplacement> dynamicBuffer = m_CommandBuffer.AddBuffer<SubReplacement>(jobIndex, oldEntity);
				for (int i = 0; i < subReplacements.Length; i++)
				{
					SubReplacement elem = subReplacements[i];
					if (flag4)
					{
						elem.m_Side = (SubReplacementSide)(0 - elem.m_Side);
					}
					dynamicBuffer.Add(elem);
				}
			}
			else if (flag7 && m_SubReplacements.HasBuffer(oldEntity))
			{
				m_CommandBuffer.RemoveComponent<SubReplacement>(jobIndex, oldEntity);
			}
			if (m_PrefabRoadData.HasComponent(prefabRef.m_Prefab))
			{
				if (((upgraded.m_Flags.m_Left | upgraded.m_Flags.m_Right) & CompositionFlags.Side.PrimaryTrack) != 0)
				{
					m_CommandBuffer.AddComponent(jobIndex, oldEntity, default(TramTrack));
				}
				else if (flag7 && m_TramTrackData.HasComponent(oldEntity))
				{
					m_CommandBuffer.RemoveComponent<TramTrack>(jobIndex, oldEntity);
				}
			}
			if (m_NativeData.HasComponent(definitionData.m_Original) && (temp.m_Flags & (TempFlags.Modify | TempFlags.Upgrade)) == 0)
			{
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, default(Native));
			}
			else if (flag7 && m_NativeData.HasComponent(oldEntity))
			{
				m_CommandBuffer.RemoveComponent<Native>(jobIndex, oldEntity);
			}
			bool flag8 = true;
			if (ownerData.m_Prefab != Entity.Null)
			{
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, default(Owner));
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, ownerData);
				flag8 = false;
			}
			else if (definitionData.m_Owner != Entity.Null)
			{
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, new Owner(definitionData.m_Owner));
				flag8 = false;
			}
			flag8 |= (netGeometryData.m_Flags & GeometryFlags.SubOwner) != 0;
			if ((definitionData.m_Flags & (CreationFlags.SubElevation | CreationFlags.Stamping)) != 0)
			{
				temp.m_Flags |= TempFlags.Essential;
			}
			if (course.m_FixedIndex >= 0)
			{
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, new Fixed
				{
					m_Index = course.m_FixedIndex
				});
			}
			else if (flag7 && m_FixedData.HasComponent(oldEntity))
			{
				m_CommandBuffer.RemoveComponent<Fixed>(jobIndex, oldEntity);
			}
			if (definitionData.m_Original != Entity.Null)
			{
				m_CommandBuffer.AddComponent(jobIndex, definitionData.m_Original, default(Hidden));
				m_CommandBuffer.AddComponent(jobIndex, definitionData.m_Original, default(BatchesUpdated));
				if (flag3)
				{
					m_CommandBuffer.SetComponent(jobIndex, oldEntity, m_BuildOrderData[definitionData.m_Original]);
				}
			}
			else if (flag3)
			{
				Game.Net.BuildOrder component2 = default(Game.Net.BuildOrder);
				component2.m_Start = m_BuildOrder + (uint)(entityIndex * 16);
				component2.m_End = component2.m_Start + 15;
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, component2);
			}
			bool flag9 = false;
			if (m_PrefabRoadData.HasComponent(prefabRef.m_Prefab))
			{
				flag9 = (m_PrefabRoadData[prefabRef.m_Prefab].m_Flags & Game.Prefabs.RoadFlags.EnableZoning) != 0;
				if (flag2)
				{
					Road road = default(Road);
					if (m_RoadData.HasComponent(definitionData.m_Original))
					{
						road = m_RoadData[definitionData.m_Original];
						CheckRoadAlignment(definitionData, prefabRef, prefabRef2, netGeometryData, ref road);
					}
					SetRoadAlignment(course, ref road);
					m_CommandBuffer.SetComponent(jobIndex, oldEntity, road);
				}
			}
			DynamicBuffer<ConnectedNode> nodes = m_CommandBuffer.SetBuffer<ConnectedNode>(jobIndex, oldEntity);
			FindNodeConnections(nodes, edge, curve, temp, netData, netGeometryData, isPermanent: false, flag8, flag9);
			if (m_PrefabElectricityConnectionData.HasComponent(prefabRef.m_Prefab))
			{
				bool flag10 = NetCompositionHelpers.TestEdgeFlags(m_PrefabElectricityConnectionData[prefabRef.m_Prefab], upgraded.m_Flags);
				if (flag10)
				{
					m_CommandBuffer.AddComponent(jobIndex, oldEntity, default(Game.Net.ElectricityConnection));
				}
				else if (flag7 && m_ElectricityConnectionData.HasComponent(oldEntity))
				{
					m_CommandBuffer.RemoveComponent<Game.Net.ElectricityConnection>(jobIndex, oldEntity);
				}
				if (definitionData.m_Original != Entity.Null)
				{
					bool flag11 = m_ElectricityConnectionData.HasComponent(definitionData.m_Original);
					if (flag10 != flag11)
					{
						temp.m_Flags &= ~TempFlags.Upgrade;
						temp.m_Flags |= TempFlags.Replace;
					}
				}
			}
			if (definitionData.m_Original != Entity.Null && prefabRef2.m_Prefab != prefabRef.m_Prefab)
			{
				bool flag12 = false;
				if (m_PrefabRoadData.HasComponent(prefabRef2.m_Prefab))
				{
					flag12 = (m_PrefabRoadData[prefabRef2.m_Prefab].m_Flags & Game.Prefabs.RoadFlags.EnableZoning) != 0;
				}
				if (flag9 != flag12)
				{
					temp.m_Flags &= ~TempFlags.Modify;
					temp.m_Flags |= TempFlags.Replace;
				}
			}
			if (netGeometryData.m_AggregateType != Entity.Null && m_AggregatedData.TryGetComponent(definitionData.m_Original, out var componentData3))
			{
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, componentData3);
			}
			if (flag2 && m_ConditionData.TryGetComponent(definitionData.m_Original, out var componentData4))
			{
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, componentData4);
			}
			if (hasCachedCurve)
			{
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, cachedCurve);
			}
			if (definitionData.m_SubPrefab != Entity.Null)
			{
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, new EditorContainer
				{
					m_Prefab = definitionData.m_SubPrefab
				});
				if (!flag3)
				{
					if (m_PseudoRandomSeedData.TryGetComponent(definitionData.m_Original, out var componentData5))
					{
						m_CommandBuffer.SetComponent(jobIndex, oldEntity, componentData5);
					}
					else
					{
						m_CommandBuffer.SetComponent(jobIndex, oldEntity, new PseudoRandomSeed((ushort)definitionData.m_RandomSeed));
					}
				}
			}
			if (!flag)
			{
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, temp);
			}
			return;
			IL_081e:
			m_CommandBuffer.RemoveComponent<Game.Buildings.ServiceUpgrade>(jobIndex, oldEntity);
			goto IL_082c;
			IL_0745:
			oldEntity = Entity.Null;
			flag7 = !flag && TryGetOldEntity(edge, prefabRef.m_Prefab, definitionData.m_SubPrefab, definitionData.m_Original, ref ownerData, ref definitionData.m_Owner, out oldEntity);
			if (flag7)
			{
				m_CommandBuffer.RemoveComponent<Deleted>(jobIndex, oldEntity);
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, default(Updated));
			}
			else
			{
				oldEntity = m_CommandBuffer.CreateEntity(jobIndex, netData.m_EdgeArchetype);
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, prefabRef);
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, edge);
				bool num2;
				if (!(definitionData.m_Original != Entity.Null))
				{
					if (m_EditorMode)
					{
						goto IL_081e;
					}
					num2 = (definitionData.m_Flags & CreationFlags.SubElevation) == 0;
				}
				else
				{
					num2 = !m_ServiceUpgradeData.HasComponent(definitionData.m_Original);
				}
				if (num2)
				{
					goto IL_081e;
				}
			}
			goto IL_082c;
		}

		private bool TryGetOldEntity(Edge edge, Entity prefab, Entity subPrefab, Entity original, ref OwnerDefinition ownerDefinition, ref Entity owner, out Entity oldEntity)
		{
			if (ownerDefinition.m_Prefab != Entity.Null && m_ReusedOwnerMap.TryGetValue(ownerDefinition, out var item))
			{
				owner = item;
				ownerDefinition = default(OwnerDefinition);
			}
			OldEdgeKey key = default(OldEdgeKey);
			key.m_Prefab = prefab;
			key.m_SubPrefab = subPrefab;
			key.m_Original = original;
			key.m_Owner = owner;
			key.m_StartNode = edge.m_Start;
			key.m_EndNode = edge.m_End;
			if (m_OldEdgeMap.TryGetValue(key, out oldEntity))
			{
				return true;
			}
			oldEntity = Entity.Null;
			return false;
		}

		private void CheckRoadAlignment(CreationDefinition definitionData, PrefabRef prefabRefData, PrefabRef originalPrefabRef, NetGeometryData netGeometryData, ref Road road)
		{
			bool2 @bool = new bool2((road.m_Flags & Game.Net.RoadFlags.StartHalfAligned) != 0, (road.m_Flags & Game.Net.RoadFlags.EndHalfAligned) != 0);
			road.m_Flags &= ~(Game.Net.RoadFlags.StartHalfAligned | Game.Net.RoadFlags.EndHalfAligned);
			if ((definitionData.m_Flags & CreationFlags.Align) != 0)
			{
				@bool = (((definitionData.m_Flags & CreationFlags.Invert) != 0) ? @bool.yx : @bool);
				if (prefabRefData.m_Prefab != originalPrefabRef.m_Prefab)
				{
					NetGeometryData netGeometryData2 = m_NetGeometryData[originalPrefabRef.m_Prefab];
					int cellWidth = ZoneUtils.GetCellWidth(netGeometryData.m_DefaultWidth);
					int cellWidth2 = ZoneUtils.GetCellWidth(netGeometryData2.m_DefaultWidth);
					@bool ^= ((cellWidth ^ cellWidth2) & 1) != 0;
				}
				if (@bool.x)
				{
					road.m_Flags |= Game.Net.RoadFlags.StartHalfAligned;
				}
				if (@bool.y)
				{
					road.m_Flags |= Game.Net.RoadFlags.EndHalfAligned;
				}
			}
		}

		private void SetRoadAlignment(NetCourse course, ref Road road)
		{
			if ((course.m_StartPosition.m_Flags & CoursePosFlags.HalfAlign) != 0)
			{
				road.m_Flags ^= Game.Net.RoadFlags.StartHalfAligned;
			}
			if ((course.m_EndPosition.m_Flags & CoursePosFlags.HalfAlign) != 0)
			{
				road.m_Flags ^= Game.Net.RoadFlags.EndHalfAligned;
			}
		}

		private bool ConnectionExists(Entity node1, Entity node2)
		{
			if (m_ConnectedEdges.TryGetBuffer(node1, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					Entity edge = bufferData[i].m_Edge;
					if (!m_DeletedData.HasComponent(edge))
					{
						Edge edge2 = m_EdgeData[edge];
						if ((edge2.m_Start == node2 && edge2.m_End == node1) || (edge2.m_End == node2 && edge2.m_Start == node1))
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		private void FindNodeConnections(DynamicBuffer<ConnectedNode> nodes, Edge edgeData, Curve curveData, Temp tempData, NetData netData, NetGeometryData netGeometryData, bool isPermanent, bool isStandalone, bool isZoneable)
		{
			float num = netGeometryData.m_DefaultWidth * 0.5f;
			num += math.select(0f, 8f, isStandalone && isZoneable);
			Bounds3 bounds = MathUtils.Expand(MathUtils.Bounds(curveData.m_Bezier), num);
			float3 @float = default(float3);
			if (m_RoundaboutData.TryGetComponent(edgeData.m_Start, out var componentData))
			{
				@float = m_NodeData[edgeData.m_Start].m_Position;
				bounds |= new Bounds3(@float - componentData.m_Radius, @float + componentData.m_Radius);
			}
			float3 float2 = default(float3);
			if (m_RoundaboutData.TryGetComponent(edgeData.m_End, out var componentData2))
			{
				float2 = m_NodeData[edgeData.m_End].m_Position;
				bounds |= new Bounds3(float2 - componentData2.m_Radius, float2 + componentData2.m_Radius);
			}
			for (int i = 0; i < m_LocalConnectList.Length; i++)
			{
				LocalConnectItem localConnectItem = m_LocalConnectList[i];
				if ((localConnectItem.m_ConnectLayers & netData.m_ConnectLayers) == 0 || (localConnectItem.m_LocalConnectLayers & netData.m_LocalConnectLayers) == 0 || localConnectItem.m_Node == edgeData.m_Start || localConnectItem.m_Node == edgeData.m_End || ((tempData.m_Flags ^ localConnectItem.m_TempFlags) & TempFlags.Delete) != 0 || localConnectItem.m_IsPermanent != isPermanent || !MathUtils.Intersect(bounds, new Bounds3(localConnectItem.m_Position - localConnectItem.m_Radius, localConnectItem.m_Position + localConnectItem.m_Radius)
				{
					y = localConnectItem.m_Position.y + localConnectItem.m_HeightRange
				}))
				{
					continue;
				}
				float t;
				float num4;
				if ((netGeometryData.m_Flags & GeometryFlags.NoEdgeConnection) != 0)
				{
					float num2 = math.distance(curveData.m_Bezier.a.xz, localConnectItem.m_Position.xz);
					float num3 = math.distance(curveData.m_Bezier.d.xz, localConnectItem.m_Position.xz);
					num4 = math.select(num2, num3, num3 < num2);
					t = math.select(0f, 1f, num3 < num2);
				}
				else
				{
					num4 = MathUtils.Distance(curveData.m_Bezier.xz, localConnectItem.m_Position.xz, out t);
				}
				num4 -= netGeometryData.m_DefaultWidth * 0.5f;
				if (componentData.m_Radius != 0f)
				{
					float num5 = math.distance(@float.xz, localConnectItem.m_Position.xz) - componentData.m_Radius;
					if (num5 < num4)
					{
						num4 = num5;
						t = 0f;
					}
				}
				if (componentData2.m_Radius != 0f)
				{
					float num6 = math.distance(float2.xz, localConnectItem.m_Position.xz) - componentData2.m_Radius;
					if (num6 < num4)
					{
						num4 = num6;
						t = 1f;
					}
				}
				if (!localConnectItem.m_IsStandalone && isStandalone && isZoneable)
				{
					num4 -= 8f;
				}
				if (num4 <= localConnectItem.m_Radius)
				{
					float position = MathUtils.Position(curveData.m_Bezier, t).y - localConnectItem.m_Position.y;
					if (MathUtils.Intersect(localConnectItem.m_HeightRange, position))
					{
						nodes.Add(new ConnectedNode(localConnectItem.m_Node, t));
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
	private struct UpdateBuildOrderJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.BuildOrder> m_BuildOrderType;

		public NativeValue<uint> m_BuildOrder;

		public void Execute()
		{
			uint num = m_BuildOrder.value;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				NativeArray<Game.Net.BuildOrder> nativeArray = m_Chunks[i].GetNativeArray(ref m_BuildOrderType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Game.Net.BuildOrder buildOrder = nativeArray[j];
					num = math.max(num, math.max(buildOrder.m_Start, buildOrder.m_End) + 1);
				}
			}
			m_BuildOrder.value = num;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EditorContainer> __Game_Tools_EditorContainer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<LocalConnect> __Game_Net_LocalConnect_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Elevation> __Game_Net_Elevation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Node> __Game_Net_Node_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<LocalConnectData> __Game_Prefabs_LocalConnectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

		public ComponentTypeHandle<CreationDefinition> __Game_Tools_CreationDefinition_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> __Game_Tools_CreationDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<OwnerDefinition> __Game_Tools_OwnerDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<NetCourse> __Game_Tools_NetCourse_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<LocalCurveCache> __Game_Tools_LocalCurveCache_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Upgraded> __Game_Net_Upgraded_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubReplacement> __Game_Net_SubReplacement_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Native> __Game_Common_Native_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Elevation> __Game_Net_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Upgraded> __Game_Net_Upgraded_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.BuildOrder> __Game_Net_BuildOrder_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TramTrack> __Game_Net_TramTrack_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EditorContainer> __Game_Tools_EditorContainer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Road> __Game_Net_Road_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCondition> __Game_Net_NetCondition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Fixed> __Game_Net_Fixed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Aggregated> __Game_Net_Aggregated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Roundabout> __Game_Net_Roundabout_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ElectricityConnection> __Game_Net_ElectricityConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> __Game_Buildings_ServiceUpgrade_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrackData> __Game_Prefabs_TrackData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RoadData> __Game_Prefabs_RoadData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ElectricityConnectionData> __Game_Prefabs_ElectricityConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedNode> __Game_Net_ConnectedNode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubReplacement> __Game_Net_SubReplacement_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.BuildOrder> __Game_Net_BuildOrder_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Tools_EditorContainer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EditorContainer>(isReadOnly: true);
			__Game_Net_LocalConnect_RO_ComponentTypeHandle = state.GetComponentTypeHandle<LocalConnect>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Elevation>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Net_Node_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Node>();
			__Game_Prefabs_LocalConnectData_RO_ComponentLookup = state.GetComponentLookup<LocalConnectData>(isReadOnly: true);
			__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(isReadOnly: true);
			__Game_Tools_CreationDefinition_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CreationDefinition>();
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Tools_CreationDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreationDefinition>(isReadOnly: true);
			__Game_Tools_OwnerDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<OwnerDefinition>(isReadOnly: true);
			__Game_Tools_NetCourse_RO_ComponentTypeHandle = state.GetComponentTypeHandle<NetCourse>(isReadOnly: true);
			__Game_Tools_LocalCurveCache_RO_ComponentTypeHandle = state.GetComponentTypeHandle<LocalCurveCache>(isReadOnly: true);
			__Game_Net_Upgraded_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Upgraded>(isReadOnly: true);
			__Game_Net_SubReplacement_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubReplacement>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Common_Native_RO_ComponentLookup = state.GetComponentLookup<Native>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentLookup = state.GetComponentLookup<Elevation>(isReadOnly: true);
			__Game_Net_Upgraded_RO_ComponentLookup = state.GetComponentLookup<Upgraded>(isReadOnly: true);
			__Game_Net_BuildOrder_RO_ComponentLookup = state.GetComponentLookup<Game.Net.BuildOrder>(isReadOnly: true);
			__Game_Net_TramTrack_RO_ComponentLookup = state.GetComponentLookup<TramTrack>(isReadOnly: true);
			__Game_Tools_EditorContainer_RO_ComponentLookup = state.GetComponentLookup<EditorContainer>(isReadOnly: true);
			__Game_Net_Road_RO_ComponentLookup = state.GetComponentLookup<Road>(isReadOnly: true);
			__Game_Net_NetCondition_RO_ComponentLookup = state.GetComponentLookup<NetCondition>(isReadOnly: true);
			__Game_Net_Fixed_RO_ComponentLookup = state.GetComponentLookup<Fixed>(isReadOnly: true);
			__Game_Net_Aggregated_RO_ComponentLookup = state.GetComponentLookup<Aggregated>(isReadOnly: true);
			__Game_Net_Roundabout_RO_ComponentLookup = state.GetComponentLookup<Roundabout>(isReadOnly: true);
			__Game_Net_ElectricityConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ElectricityConnection>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentLookup = state.GetComponentLookup<PseudoRandomSeed>(isReadOnly: true);
			__Game_Buildings_ServiceUpgrade_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.ServiceUpgrade>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_TrackData_RO_ComponentLookup = state.GetComponentLookup<TrackData>(isReadOnly: true);
			__Game_Prefabs_RoadData_RO_ComponentLookup = state.GetComponentLookup<RoadData>(isReadOnly: true);
			__Game_Prefabs_ElectricityConnectionData_RO_ComponentLookup = state.GetComponentLookup<ElectricityConnectionData>(isReadOnly: true);
			__Game_Prefabs_PrefabData_RO_ComponentLookup = state.GetComponentLookup<PrefabData>(isReadOnly: true);
			__Game_Net_ConnectedNode_RO_BufferLookup = state.GetBufferLookup<ConnectedNode>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Net_SubReplacement_RO_BufferLookup = state.GetBufferLookup<SubReplacement>(isReadOnly: true);
			__Game_Net_BuildOrder_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.BuildOrder>(isReadOnly: true);
		}
	}

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private GenerateObjectsSystem m_GenerateObjectsSystem;

	private ToolSystem m_ToolSystem;

	private ModificationBarrier2 m_TempEdgesBarrier;

	private NativeValue<uint> m_BuildOrder;

	private EntityQuery m_CreatedEdgesQuery;

	private EntityQuery m_DefinitionQuery;

	private EntityQuery m_DeletedQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_BuildOrder = new NativeValue<uint>(Allocator.Persistent);
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_GenerateObjectsSystem = base.World.GetOrCreateSystemManaged<GenerateObjectsSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_TempEdgesBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier2>();
		m_CreatedEdgesQuery = GetEntityQuery(ComponentType.ReadOnly<Created>(), ComponentType.ReadOnly<Edge>(), ComponentType.ReadOnly<Game.Net.BuildOrder>(), ComponentType.Exclude<Temp>());
		m_DefinitionQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<CreationDefinition>(),
				ComponentType.ReadOnly<NetCourse>(),
				ComponentType.ReadOnly<Updated>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Updated>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Node>(),
				ComponentType.ReadOnly<Edge>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() }
		});
		m_DeletedQuery = GetEntityQuery(ComponentType.ReadOnly<Edge>(), ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<PrefabRef>());
		RequireAnyForUpdate(m_CreatedEdgesQuery, m_DefinitionQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_BuildOrder.Dispose();
		base.OnDestroy();
	}

	public NativeValue<uint> GetBuildOrder()
	{
		return m_BuildOrder;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = base.Dependency;
		if (!m_DefinitionQuery.IsEmptyIgnoreFilter)
		{
			NativeQueue<LocalConnectItem> localConnectQueue = new NativeQueue<LocalConnectItem>(Allocator.TempJob);
			NativeList<LocalConnectItem> localConnectList = new NativeList<LocalConnectItem>(Allocator.TempJob);
			NativeParallelMultiHashMap<NodeMapKey, Entity> nodeMap = new NativeParallelMultiHashMap<NodeMapKey, Entity>(m_DefinitionQuery.CalculateEntityCount(), Allocator.TempJob);
			NativeHashMap<OldEdgeKey, Entity> oldEdgeMap = new NativeHashMap<OldEdgeKey, Entity>(32, Allocator.TempJob);
			JobHandle dependencies;
			NativeHashMap<OwnerDefinition, Entity> reusedOwnerMap = m_GenerateObjectsSystem.GetReusedOwnerMap(out dependencies);
			TerrainHeightData heightData = m_TerrainSystem.GetHeightData();
			JobHandle deps;
			WaterSurfaceData<SurfaceWater> surfaceData = m_WaterSystem.GetSurfaceData(out deps);
			CheckNodesJob jobData = new CheckNodesJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EditorContainerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_EditorContainer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_LocalConnectType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_LocalConnect_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_ElevationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_NodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Node_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_LocalConnectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LocalConnectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TerrainHeightData = heightData,
				m_NodeMap = nodeMap.AsParallelWriter(),
				m_LocalConnectQueue = localConnectQueue.AsParallelWriter()
			};
			FillOldEdgesJob jobData2 = new FillOldEdgesJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EditorContainerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_EditorContainer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OldEdgeMap = oldEdgeMap
			};
			CheckDefinitionsJob jobData3 = new CheckDefinitionsJob
			{
				m_CreationDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_CreationDefinition_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeMap = nodeMap.AsParallelWriter()
			};
			CollectLocalConnectItemsJob jobData4 = new CollectLocalConnectItemsJob
			{
				m_LocalConnectQueue = localConnectQueue,
				m_LocalConnectList = localConnectList
			};
			JobHandle dependsOn = JobChunkExtensions.ScheduleParallel(jobData, m_DefinitionQuery, base.Dependency);
			JobHandle job = JobChunkExtensions.Schedule(jobData2, m_DeletedQuery, base.Dependency);
			JobHandle job2 = JobChunkExtensions.ScheduleParallel(jobData3, m_DefinitionQuery, dependsOn);
			JobHandle jobHandle2 = IJobExtensions.Schedule(jobData4, dependsOn);
			JobHandle outJobHandle;
			NativeArray<int> chunkBaseEntityIndices = m_DefinitionQuery.CalculateBaseEntityIndexArrayAsync(Allocator.TempJob, JobHandle.CombineDependencies(job2, jobHandle2, deps), out outJobHandle);
			JobHandle jobHandle3 = JobChunkExtensions.ScheduleParallel(new GenerateEdgesJob
			{
				m_ChunkBaseEntityIndices = chunkBaseEntityIndices,
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_CreationDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_CreationDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_OwnerDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_OwnerDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_NetCourseType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_NetCourse_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_LocalCurveCacheType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_LocalCurveCache_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_UpgradedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Upgraded_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_SubReplacementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Net_SubReplacement_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NativeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Native_RO_ComponentLookup, ref base.CheckedStateRef),
				m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_UpgradedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Upgraded_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildOrderData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_BuildOrder_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TramTrackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_TramTrack_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EditorContainerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_EditorContainer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_RoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Road_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConditionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NetCondition_RO_ComponentLookup, ref base.CheckedStateRef),
				m_FixedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Fixed_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AggregatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Aggregated_RO_ComponentLookup, ref base.CheckedStateRef),
				m_RoundaboutData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Roundabout_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElectricityConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ElectricityConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ServiceUpgradeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LocalConnectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LocalConnectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabTrackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrackData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RoadData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabElectricityConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ElectricityConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectedNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedNode_RO_BufferLookup, ref base.CheckedStateRef),
				m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubReplacements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubReplacement_RO_BufferLookup, ref base.CheckedStateRef),
				m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
				m_BuildOrder = m_BuildOrder.value,
				m_NodeMap = nodeMap,
				m_ReusedOwnerMap = reusedOwnerMap,
				m_OldEdgeMap = oldEdgeMap,
				m_LocalConnectList = localConnectList.AsDeferredJobArray(),
				m_TerrainHeightData = heightData,
				m_WaterSurfaceData = surfaceData,
				m_CommandBuffer = m_TempEdgesBarrier.CreateCommandBuffer().AsParallelWriter()
			}, m_DefinitionQuery, JobHandle.CombineDependencies(outJobHandle, job, dependencies));
			localConnectQueue.Dispose(jobHandle2);
			localConnectList.Dispose(jobHandle3);
			nodeMap.Dispose(jobHandle3);
			oldEdgeMap.Dispose(jobHandle3);
			m_TerrainSystem.AddCPUHeightReader(jobHandle3);
			m_WaterSystem.AddSurfaceReader(jobHandle3);
			m_GenerateObjectsSystem.AddOwnerMapReader(jobHandle3);
			m_TempEdgesBarrier.AddJobHandleForProducer(jobHandle3);
			jobHandle = jobHandle3;
		}
		if (!m_CreatedEdgesQuery.IsEmptyIgnoreFilter)
		{
			JobHandle outJobHandle2;
			NativeList<ArchetypeChunk> chunks = m_CreatedEdgesQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle2);
			JobHandle jobHandle4 = IJobExtensions.Schedule(new UpdateBuildOrderJob
			{
				m_Chunks = chunks,
				m_BuildOrderType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_BuildOrder_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_BuildOrder = m_BuildOrder
			}, JobHandle.CombineDependencies(base.Dependency, outJobHandle2));
			chunks.Dispose(jobHandle4);
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle4);
		}
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
	public GenerateEdgesSystem()
	{
	}
}
