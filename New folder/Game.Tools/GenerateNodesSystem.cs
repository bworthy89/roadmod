using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
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
public class GenerateNodesSystem : GameSystemBase
{
	private struct UpdateData : IEquatable<UpdateData>
	{
		public bool m_OnCourse;

		public bool m_Regenerate;

		public bool m_HasCachedPosition;

		public bool m_AddEdge;

		public bool m_UpdateOnly;

		public bool m_Valid;

		public float3 m_Position;

		public float3 m_CachedPosition;

		public quaternion m_Rotation;

		public Entity m_Prefab;

		public Entity m_Original;

		public Entity m_Owner;

		public Entity m_Lane;

		public OwnerDefinition m_OwnerData;

		public float m_CurvePosition;

		public Bounds1 m_CurveBounds;

		public float2 m_Elevation;

		public CoursePosFlags m_Flags;

		public CreationFlags m_CreationFlags;

		public CompositionFlags m_UpgradeFlags;

		public int m_FixedIndex;

		public int m_RandomSeed;

		public int m_ParentMesh;

		public UpdateData(Node node, Entity original, bool regenerate, bool updateOnly)
		{
			m_OnCourse = false;
			m_Regenerate = regenerate;
			m_HasCachedPosition = false;
			m_AddEdge = false;
			m_UpdateOnly = updateOnly;
			m_Valid = true;
			m_Position = node.m_Position;
			m_CachedPosition = default(float3);
			m_Rotation = node.m_Rotation;
			m_Prefab = Entity.Null;
			m_Original = original;
			m_Owner = Entity.Null;
			m_Lane = Entity.Null;
			m_OwnerData = default(OwnerDefinition);
			m_CurvePosition = 0f;
			m_CurveBounds = new Bounds1(0f, 1f);
			m_Elevation = default(float2);
			m_Flags = (CoursePosFlags)0u;
			m_CreationFlags = (CreationFlags)0u;
			m_UpgradeFlags = default(CompositionFlags);
			m_FixedIndex = 0;
			m_RandomSeed = 0;
			m_ParentMesh = -1;
		}

		public UpdateData(CreationDefinition definitionData, OwnerDefinition ownerData, CoursePos coursePos, Upgraded upgraded, int fixedIndex, float3 cachedPosition, Bounds1 curveBounds, bool hasCachedPosition, bool addEdge)
		{
			m_OnCourse = true;
			m_Regenerate = true;
			m_HasCachedPosition = hasCachedPosition;
			m_AddEdge = addEdge;
			m_UpdateOnly = false;
			m_Valid = true;
			m_Position = coursePos.m_Position;
			m_CachedPosition = cachedPosition;
			m_Rotation = coursePos.m_Rotation;
			m_Prefab = definitionData.m_Prefab;
			m_Original = coursePos.m_Entity;
			m_Owner = definitionData.m_Owner;
			m_Lane = definitionData.m_SubPrefab;
			m_OwnerData = ownerData;
			m_CurvePosition = coursePos.m_SplitPosition;
			m_CurveBounds = curveBounds;
			m_Elevation = coursePos.m_Elevation;
			m_Flags = coursePos.m_Flags;
			m_CreationFlags = definitionData.m_Flags;
			m_UpgradeFlags = upgraded.m_Flags;
			m_FixedIndex = fixedIndex;
			m_RandomSeed = definitionData.m_RandomSeed;
			m_ParentMesh = coursePos.m_ParentMesh;
		}

		public bool Equals(UpdateData other)
		{
			if (m_Original != Entity.Null || other.m_Original != Entity.Null)
			{
				return m_Original.Equals(other.m_Original);
			}
			return m_Position.Equals(other.m_Position);
		}

		public override int GetHashCode()
		{
			if (m_Original != Entity.Null)
			{
				return m_Original.GetHashCode();
			}
			return m_Position.GetHashCode();
		}
	}

	private struct NodeKey : IEquatable<NodeKey>
	{
		public Entity m_Original;

		public float3 m_Position;

		public bool m_IsEditor;

		public NodeKey(UpdateData data)
		{
			m_Original = data.m_Original;
			m_Position = data.m_Position;
			m_IsEditor = data.m_Lane != Entity.Null;
		}

		public bool Equals(NodeKey other)
		{
			if (m_Original != Entity.Null || other.m_Original != Entity.Null)
			{
				return m_Original.Equals(other.m_Original);
			}
			if (m_Position.Equals(other.m_Position))
			{
				return m_IsEditor == other.m_IsEditor;
			}
			return false;
		}

		public override int GetHashCode()
		{
			if (m_Original != Entity.Null)
			{
				return m_Original.GetHashCode();
			}
			return m_Position.GetHashCode();
		}
	}

	private struct DefinitionData
	{
		public Entity m_Prefab;

		public Entity m_Lane;

		public CreationFlags m_Flags;

		public DefinitionData(Entity prefab, Entity lane, CreationFlags flags)
		{
			m_Prefab = prefab;
			m_Lane = lane;
			m_Flags = flags;
		}
	}

	private struct OldNodeKey : IEquatable<OldNodeKey>
	{
		public Entity m_Prefab;

		public Entity m_SubPrefab;

		public Entity m_Original;

		public Entity m_Owner;

		public bool m_OutsideConnection;

		public bool Equals(OldNodeKey other)
		{
			if (m_Prefab.Equals(other.m_Prefab) && m_SubPrefab.Equals(other.m_SubPrefab) && m_Original.Equals(other.m_Original) && m_Owner.Equals(other.m_Owner))
			{
				return m_OutsideConnection == other.m_OutsideConnection;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((17 * 31 + m_Prefab.GetHashCode()) * 31 + m_SubPrefab.GetHashCode()) * 31 + m_Original.GetHashCode()) * 31 + m_Owner.GetHashCode();
		}
	}

	private struct OldNodeValue
	{
		public Entity m_Entity;

		public float3 m_Position;
	}

	[BurstCompile]
	private struct FillOldNodesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Node> m_NodeType;

		[ReadOnly]
		public ComponentTypeHandle<EditorContainer> m_EditorContainerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.OutsideConnection> m_OutsideConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		public NativeParallelMultiHashMap<OldNodeKey, OldNodeValue> m_OldNodeMap;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<Temp> nativeArray3 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<Node> nativeArray4 = chunk.GetNativeArray(ref m_NodeType);
			NativeArray<EditorContainer> nativeArray5 = chunk.GetNativeArray(ref m_EditorContainerType);
			NativeArray<PrefabRef> nativeArray6 = chunk.GetNativeArray(ref m_PrefabRefType);
			bool outsideConnection = chunk.Has(ref m_OutsideConnectionType);
			OldNodeKey key = default(OldNodeKey);
			OldNodeValue item = default(OldNodeValue);
			for (int i = 0; i < nativeArray6.Length; i++)
			{
				key.m_Prefab = nativeArray6[i].m_Prefab;
				key.m_SubPrefab = Entity.Null;
				key.m_Original = nativeArray3[i].m_Original;
				key.m_Owner = Entity.Null;
				key.m_OutsideConnection = outsideConnection;
				item.m_Entity = nativeArray[i];
				item.m_Position = nativeArray4[i].m_Position;
				if (CollectionUtils.TryGet(nativeArray5, i, out var value))
				{
					key.m_SubPrefab = value.m_Prefab;
				}
				if (CollectionUtils.TryGet(nativeArray2, i, out var value2))
				{
					key.m_Owner = value2.m_Owner;
					if (m_TransformData.TryGetComponent(value2.m_Owner, out var componentData))
					{
						Transform inverseParentTransform = ObjectUtils.InverseTransform(componentData);
						item.m_Position = ObjectUtils.WorldToLocal(inverseParentTransform, item.m_Position);
					}
				}
				m_OldNodeMap.Add(key, item);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FillNodeMapJob : IJobChunk
	{
		private struct EdgeIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds3 m_Bounds;

			public float2 m_Position;

			public float2 m_ConnectRadius;

			public Layer m_AttachLayers;

			public Layer m_ConnectLayers;

			public Layer m_LocalConnectLayers;

			public Entity m_IgnoreEntity;

			public bool m_IsPermanent;

			public ComponentLookup<Edge> m_EdgeData;

			public ComponentLookup<Node> m_NodeData;

			public ComponentLookup<Curve> m_CurveData;

			public ComponentLookup<Deleted> m_DeletedData;

			public ComponentLookup<Owner> m_OwnerData;

			public ComponentLookup<Roundabout> m_RoundaboutData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<NetData> m_NetData;

			public ComponentLookup<NetGeometryData> m_NetGeometryData;

			public ComponentLookup<RoadData> m_RoadData;

			public BufferLookup<ConnectedEdge> m_Edges;

			public NativeQueue<UpdateData>.ParallelWriter m_NodeQueue;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds, m_Bounds) || entity == m_IgnoreEntity || !m_CurveData.HasComponent(entity))
				{
					return;
				}
				PrefabRef prefabRef = m_PrefabRefData[entity];
				NetData netData = m_NetData[prefabRef.m_Prefab];
				if ((m_AttachLayers & netData.m_ConnectLayers) == 0 && ((m_ConnectLayers & netData.m_ConnectLayers) == 0 || (m_LocalConnectLayers & netData.m_LocalConnectLayers) == 0))
				{
					return;
				}
				Edge edge = m_EdgeData[entity];
				if (edge.m_Start == m_IgnoreEntity || edge.m_End == m_IgnoreEntity)
				{
					return;
				}
				Curve curve = m_CurveData[entity];
				NetGeometryData netGeometryData = m_NetGeometryData[prefabRef.m_Prefab];
				RoadData roadData = default(RoadData);
				if (m_RoadData.HasComponent(prefabRef.m_Prefab))
				{
					roadData = m_RoadData[prefabRef.m_Prefab];
				}
				float t;
				float num = MathUtils.Distance(curve.m_Bezier.xz, m_Position, out t);
				float num2 = math.select(m_ConnectRadius.x, m_ConnectRadius.y, !m_OwnerData.HasComponent(entity) && (roadData.m_Flags & Game.Prefabs.RoadFlags.EnableZoning) != 0);
				bool flag = num <= netGeometryData.m_DefaultWidth * 0.5f + num2;
				if (!flag)
				{
					if (m_RoundaboutData.TryGetComponent(edge.m_Start, out var componentData) && m_NodeData.TryGetComponent(edge.m_Start, out var componentData2) && math.distance(componentData2.m_Position.xz, m_Position) <= componentData.m_Radius + num2)
					{
						flag = true;
					}
					if (m_RoundaboutData.TryGetComponent(edge.m_End, out componentData) && m_NodeData.TryGetComponent(edge.m_End, out componentData2) && math.distance(componentData2.m_Position.xz, m_Position) <= componentData.m_Radius + num2)
					{
						flag = true;
					}
				}
				if (flag)
				{
					if (m_IsPermanent)
					{
						m_NodeQueue.Enqueue(new UpdateData(default(Node), entity, regenerate: false, updateOnly: true));
						m_NodeQueue.Enqueue(new UpdateData(default(Node), edge.m_Start, regenerate: false, updateOnly: true));
						m_NodeQueue.Enqueue(new UpdateData(default(Node), edge.m_End, regenerate: false, updateOnly: true));
						return;
					}
					Node node = m_NodeData[edge.m_Start];
					Node node2 = m_NodeData[edge.m_End];
					m_NodeQueue.Enqueue(new UpdateData(node, edge.m_Start, regenerate: true, updateOnly: false));
					m_NodeQueue.Enqueue(new UpdateData(node2, edge.m_End, regenerate: true, updateOnly: false));
					AddConnectedEdges(edge.m_Start, edge.m_End);
					AddConnectedEdges(edge.m_End, edge.m_Start);
				}
			}

			private void AddConnectedEdges(Entity node, Entity otherNode)
			{
				DynamicBuffer<ConnectedEdge> dynamicBuffer = m_Edges[node];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity edge = dynamicBuffer[i].m_Edge;
					if (!m_DeletedData.HasComponent(edge))
					{
						Edge edge2 = m_EdgeData[edge];
						if (edge2.m_Start != node && edge2.m_Start != otherNode)
						{
							Node node2 = m_NodeData[edge2.m_Start];
							m_NodeQueue.Enqueue(new UpdateData(node2, edge2.m_Start, regenerate: false, updateOnly: false));
						}
						if (edge2.m_End != node && edge2.m_End != otherNode)
						{
							Node node3 = m_NodeData[edge2.m_End];
							m_NodeQueue.Enqueue(new UpdateData(node3, edge2.m_End, regenerate: false, updateOnly: false));
						}
					}
				}
			}
		}

		private struct NodeIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds3 m_Bounds;

			public Bezier4x3 m_Curve;

			public float2 m_ConnectRadius;

			public Layer m_ConnectLayers;

			public bool m_IsPermanent;

			public ComponentLookup<Edge> m_EdgeData;

			public ComponentLookup<Node> m_NodeData;

			public ComponentLookup<Owner> m_OwnerData;

			public ComponentLookup<LocalConnect> m_LocalConnectData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<LocalConnectData> m_PrefabLocalConnectData;

			public ComponentLookup<NetGeometryData> m_NetGeometryData;

			public BufferLookup<ConnectedEdge> m_Edges;

			public NativeQueue<UpdateData>.ParallelWriter m_NodeQueue;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (MathUtils.Intersect(bounds.m_Bounds, m_Bounds) && m_NodeData.HasComponent(entity))
				{
					CheckNode(entity);
				}
			}

			private void CheckNode(Entity entity)
			{
				if (!m_LocalConnectData.HasComponent(entity))
				{
					return;
				}
				PrefabRef prefabRef = m_PrefabRefData[entity];
				if (!m_PrefabLocalConnectData.HasComponent(prefabRef.m_Prefab))
				{
					return;
				}
				LocalConnectData localConnectData = m_PrefabLocalConnectData[prefabRef.m_Prefab];
				if ((m_ConnectLayers & localConnectData.m_Layers) == 0)
				{
					return;
				}
				NetGeometryData netGeometryData = m_NetGeometryData[prefabRef.m_Prefab];
				float num = math.max(0f, netGeometryData.m_DefaultWidth * 0.5f + localConnectData.m_SearchDistance);
				Node node = m_NodeData[entity];
				if (!MathUtils.Intersect(bounds2: new Bounds3(node.m_Position - num, node.m_Position + num)
				{
					y = node.m_Position.y + localConnectData.m_HeightRange
				}, bounds1: m_Bounds))
				{
					return;
				}
				float t;
				float num2 = MathUtils.Distance(m_Curve.xz, node.m_Position.xz, out t);
				float num3 = math.select(m_ConnectRadius.x, m_ConnectRadius.y, m_OwnerData.HasComponent(entity) && localConnectData.m_SearchDistance != 0f && (netGeometryData.m_Flags & Game.Net.GeometryFlags.SubOwner) == 0);
				if (!(num2 <= num + num3))
				{
					return;
				}
				if (m_IsPermanent)
				{
					m_NodeQueue.Enqueue(new UpdateData(default(Node), entity, regenerate: false, updateOnly: true));
					return;
				}
				m_NodeQueue.Enqueue(new UpdateData(node, entity, regenerate: true, updateOnly: false));
				DynamicBuffer<ConnectedEdge> dynamicBuffer = m_Edges[entity];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity edge = dynamicBuffer[i].m_Edge;
					Edge edge2 = m_EdgeData[edge];
					if (edge2.m_Start == entity)
					{
						Node node2 = m_NodeData[edge2.m_End];
						m_NodeQueue.Enqueue(new UpdateData(node2, edge2.m_End, regenerate: false, updateOnly: false));
					}
					else if (edge2.m_End == entity)
					{
						Node node3 = m_NodeData[edge2.m_Start];
						m_NodeQueue.Enqueue(new UpdateData(node3, edge2.m_Start, regenerate: false, updateOnly: false));
					}
				}
			}
		}

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
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<LocalConnect> m_LocalConnectData;

		[ReadOnly]
		public ComponentLookup<Roundabout> m_RoundaboutData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetData> m_NetData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_NetGeometryData;

		[ReadOnly]
		public ComponentLookup<LocalConnectData> m_PrefabLocalConnectData;

		[ReadOnly]
		public ComponentLookup<RoadData> m_RoadData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_Edges;

		[ReadOnly]
		public BufferLookup<ConnectedNode> m_Nodes;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> m_SubNets;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		public NativeQueue<UpdateData>.ParallelWriter m_NodeQueue;

		public NativeParallelHashMap<Entity, DefinitionData>.ParallelWriter m_DefinitionMap;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<CreationDefinition> nativeArray = chunk.GetNativeArray(ref m_CreationDefinitionType);
			NativeArray<NetCourse> nativeArray2 = chunk.GetNativeArray(ref m_NetCourseType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				CreationDefinition creationDefinition = nativeArray[i];
				if (creationDefinition.m_Original != Entity.Null)
				{
					m_DefinitionMap.TryAdd(creationDefinition.m_Original, new DefinitionData(creationDefinition.m_Prefab, creationDefinition.m_SubPrefab, creationDefinition.m_Flags));
				}
			}
			if (nativeArray2.Length == 0)
			{
				return;
			}
			NativeArray<OwnerDefinition> nativeArray3 = chunk.GetNativeArray(ref m_OwnerDefinitionType);
			NativeArray<LocalCurveCache> nativeArray4 = chunk.GetNativeArray(ref m_LocalCurveCacheType);
			NativeArray<Upgraded> nativeArray5 = chunk.GetNativeArray(ref m_UpgradedType);
			for (int j = 0; j < nativeArray2.Length; j++)
			{
				CreationDefinition definitionData = nativeArray[j];
				if (m_DeletedData.HasComponent(definitionData.m_Owner))
				{
					continue;
				}
				OwnerDefinition ownerData = default(OwnerDefinition);
				NetCourse netCourse = nativeArray2[j];
				LocalCurveCache localCurveCache = default(LocalCurveCache);
				Upgraded upgraded = default(Upgraded);
				bool isStandalone = true;
				if (nativeArray3.Length != 0)
				{
					ownerData = nativeArray3[j];
					isStandalone = false;
				}
				if (nativeArray4.Length != 0)
				{
					localCurveCache = nativeArray4[j];
				}
				if (nativeArray5.Length != 0)
				{
					upgraded = nativeArray5[j];
				}
				int2 @int = new PseudoRandomSeed((ushort)definitionData.m_RandomSeed).GetRandom(PseudoRandomSeed.kEdgeNodes).NextInt2();
				if (((netCourse.m_StartPosition.m_Flags | netCourse.m_EndPosition.m_Flags) & CoursePosFlags.DontCreate) == 0)
				{
					if (netCourse.m_StartPosition.m_Position.Equals(netCourse.m_EndPosition.m_Position))
					{
						if (netCourse.m_StartPosition.m_Entity != Entity.Null && netCourse.m_EndPosition.m_Entity == Entity.Null)
						{
							definitionData.m_RandomSeed = @int.x;
							AddNode(definitionData, ownerData, netCourse.m_StartPosition, upgraded, netCourse.m_FixedIndex, localCurveCache.m_Curve.a, nativeArray4.Length != 0, addEdge: false);
						}
						else
						{
							definitionData.m_RandomSeed = @int.y;
							AddNode(definitionData, ownerData, netCourse.m_EndPosition, upgraded, netCourse.m_FixedIndex, localCurveCache.m_Curve.d, nativeArray4.Length != 0, addEdge: false);
						}
					}
					else
					{
						definitionData.m_Flags &= ~(CreationFlags.Select | CreationFlags.Upgrade);
						upgraded.m_Flags = default(CompositionFlags);
						definitionData.m_RandomSeed = @int.x;
						AddNode(definitionData, ownerData, netCourse.m_StartPosition, upgraded, netCourse.m_FixedIndex, localCurveCache.m_Curve.a, nativeArray4.Length != 0, (definitionData.m_Flags & CreationFlags.Delete) == 0);
						definitionData.m_RandomSeed = @int.y;
						AddNode(definitionData, ownerData, netCourse.m_EndPosition, upgraded, netCourse.m_FixedIndex, localCurveCache.m_Curve.d, nativeArray4.Length != 0, (definitionData.m_Flags & CreationFlags.Delete) == 0);
					}
				}
				bool flag = (definitionData.m_Flags & CreationFlags.Permanent) != 0;
				Entity deleteEdge = Entity.Null;
				if (!flag && (definitionData.m_Flags & CreationFlags.Delete) != 0)
				{
					deleteEdge = definitionData.m_Original;
				}
				if (netCourse.m_StartPosition.m_Entity != Entity.Null)
				{
					AddConnectedNodes(netCourse.m_StartPosition.m_Entity, deleteEdge, flag);
				}
				if (netCourse.m_EndPosition.m_Entity != Entity.Null)
				{
					AddConnectedNodes(netCourse.m_EndPosition.m_Entity, deleteEdge, flag);
				}
				if (definitionData.m_Prefab != Entity.Null)
				{
					AddEdgesForLocalConnectOrAttachment(netCourse.m_StartPosition.m_Flags, netCourse.m_StartPosition.m_Entity, netCourse.m_StartPosition.m_Position, netCourse.m_StartPosition.m_Elevation, definitionData.m_Prefab, flag, isStandalone);
					if (!netCourse.m_StartPosition.m_Position.Equals(netCourse.m_EndPosition.m_Position))
					{
						AddEdgesForLocalConnectOrAttachment(netCourse.m_EndPosition.m_Flags, netCourse.m_EndPosition.m_Entity, netCourse.m_EndPosition.m_Position, netCourse.m_EndPosition.m_Elevation, definitionData.m_Prefab, flag, isStandalone);
						Bezier4x3 curve = MathUtils.Cut(netCourse.m_Curve, new float2(netCourse.m_StartPosition.m_CourseDelta, netCourse.m_EndPosition.m_CourseDelta));
						AddNodesForLocalConnect(curve, definitionData.m_Prefab, flag, isStandalone);
					}
				}
				else if (m_PrefabRefData.HasComponent(definitionData.m_Original))
				{
					Entity prefab = m_PrefabRefData[definitionData.m_Original].m_Prefab;
					AddEdgesForLocalConnectOrAttachment(netCourse.m_StartPosition.m_Flags, netCourse.m_StartPosition.m_Entity, netCourse.m_StartPosition.m_Position, netCourse.m_StartPosition.m_Elevation, prefab, flag, isStandalone);
					if (!netCourse.m_StartPosition.m_Position.Equals(netCourse.m_EndPosition.m_Position))
					{
						AddEdgesForLocalConnectOrAttachment(netCourse.m_EndPosition.m_Flags, netCourse.m_EndPosition.m_Entity, netCourse.m_EndPosition.m_Position, netCourse.m_EndPosition.m_Elevation, prefab, flag, isStandalone);
					}
				}
			}
		}

		private void AddConnectedNodes(Entity original, Entity deleteEdge, bool isPermanent)
		{
			if (m_EdgeData.HasComponent(original))
			{
				Edge edge = m_EdgeData[original];
				AddNode(edge.m_Start, regenerate: true, isPermanent);
				AddNode(edge.m_End, regenerate: true, isPermanent);
				AddConnectedEdges(edge.m_Start, edge.m_End, isPermanent);
				AddConnectedEdges(edge.m_End, edge.m_Start, isPermanent);
				DynamicBuffer<ConnectedNode> dynamicBuffer = m_Nodes[original];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity node = dynamicBuffer[i].m_Node;
					AddNode(node, regenerate: true, isPermanent);
					AddConnectedEdges(node, isPermanent);
				}
			}
			else
			{
				if (!m_NodeData.HasComponent(original))
				{
					return;
				}
				DynamicBuffer<ConnectedEdge> dynamicBuffer2 = m_Edges[original];
				if (deleteEdge != Entity.Null && dynamicBuffer2.Length != 3)
				{
					deleteEdge = Entity.Null;
				}
				for (int j = 0; j < dynamicBuffer2.Length; j++)
				{
					Entity edge2 = dynamicBuffer2[j].m_Edge;
					if (m_DeletedData.HasComponent(edge2))
					{
						continue;
					}
					if (isPermanent)
					{
						m_NodeQueue.Enqueue(new UpdateData(default(Node), edge2, regenerate: false, updateOnly: true));
					}
					Edge componentData = m_EdgeData[edge2];
					bool flag = componentData.m_Start != original;
					bool flag2 = componentData.m_End != original;
					if (flag && flag2)
					{
						AddNode(componentData.m_Start, regenerate: true, isPermanent);
						AddNode(componentData.m_End, regenerate: true, isPermanent);
						AddConnectedEdges(componentData.m_Start, componentData.m_End, isPermanent);
						AddConnectedEdges(componentData.m_End, componentData.m_Start, isPermanent);
					}
					else
					{
						if (flag)
						{
							if (deleteEdge != Entity.Null)
							{
								AddNode(componentData.m_Start, regenerate: true, isPermanent);
								AddConnectedEdges(componentData.m_Start, original, isPermanent);
							}
							else
							{
								AddNode(componentData.m_Start, regenerate: false, isPermanent);
							}
						}
						if (flag2)
						{
							if (deleteEdge != Entity.Null)
							{
								AddNode(componentData.m_End, regenerate: true, isPermanent);
								AddConnectedEdges(componentData.m_End, original, isPermanent);
							}
							else
							{
								AddNode(componentData.m_End, regenerate: false, isPermanent);
							}
						}
					}
					DynamicBuffer<ConnectedNode> dynamicBuffer3 = m_Nodes[edge2];
					for (int k = 0; k < dynamicBuffer3.Length; k++)
					{
						Entity node2 = dynamicBuffer3[k].m_Node;
						AddNode(node2, regenerate: true, isPermanent);
						AddConnectedEdges(node2, isPermanent);
					}
					if (flag == flag2)
					{
						continue;
					}
					Curve curve = m_CurveData[edge2];
					Entity entity = edge2;
					if (m_OwnerData.TryGetComponent(edge2, out var componentData2) && m_EdgeData.TryGetComponent(componentData2.m_Owner, out componentData))
					{
						entity = componentData2.m_Owner;
						if (isPermanent)
						{
							m_NodeQueue.Enqueue(new UpdateData(default(Node), entity, regenerate: false, updateOnly: true));
						}
						Curve curve2 = m_CurveData[entity];
						bool flag3 = math.dot(curve.m_Bezier.d.xz - curve.m_Bezier.a.xz, curve2.m_Bezier.d.xz - curve2.m_Bezier.a.xz) < 0f;
						AddNode(componentData.m_Start, flag3 ? flag : flag2, isPermanent);
						AddNode(componentData.m_End, flag3 ? flag2 : flag, isPermanent);
						if (flag3 ? flag : flag2)
						{
							AddConnectedEdges(componentData.m_Start, componentData.m_End, isPermanent);
						}
						if (flag3 ? flag2 : flag)
						{
							AddConnectedEdges(componentData.m_End, componentData.m_Start, isPermanent);
						}
					}
					if (!m_SubNets.TryGetBuffer(entity, out var bufferData))
					{
						continue;
					}
					for (int l = 0; l < bufferData.Length; l++)
					{
						Entity subNet = bufferData[l].m_SubNet;
						if (!(subNet == edge2) && m_EdgeData.TryGetComponent(subNet, out componentData))
						{
							if (isPermanent)
							{
								m_NodeQueue.Enqueue(new UpdateData(default(Node), subNet, regenerate: false, updateOnly: true));
							}
							Curve curve3 = m_CurveData[subNet];
							bool flag4 = math.dot(curve.m_Bezier.d.xz - curve.m_Bezier.a.xz, curve3.m_Bezier.d.xz - curve3.m_Bezier.a.xz) < 0f;
							AddNode(componentData.m_Start, flag4 ? flag : flag2, isPermanent);
							AddNode(componentData.m_End, flag4 ? flag2 : flag, isPermanent);
							if (flag4 ? flag : flag2)
							{
								AddConnectedEdges(componentData.m_Start, componentData.m_End, isPermanent);
							}
							if (flag4 ? flag2 : flag)
							{
								AddConnectedEdges(componentData.m_End, componentData.m_Start, isPermanent);
							}
						}
					}
				}
			}
		}

		private void AddConnectedEdges(Entity node, Entity otherNode, bool isPermanent)
		{
			DynamicBuffer<ConnectedEdge> dynamicBuffer = m_Edges[node];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity edge = dynamicBuffer[i].m_Edge;
				if (!m_DeletedData.HasComponent(edge))
				{
					if (isPermanent)
					{
						m_NodeQueue.Enqueue(new UpdateData(default(Node), edge, regenerate: false, updateOnly: true));
					}
					Edge edge2 = m_EdgeData[edge];
					if (edge2.m_Start != node && edge2.m_Start != otherNode)
					{
						AddNode(edge2.m_Start, regenerate: false, isPermanent);
					}
					if (edge2.m_End != node && edge2.m_End != otherNode)
					{
						AddNode(edge2.m_End, regenerate: false, isPermanent);
					}
				}
			}
		}

		private void AddConnectedEdges(Entity node, bool isPermanent)
		{
			DynamicBuffer<ConnectedEdge> dynamicBuffer = m_Edges[node];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity edge = dynamicBuffer[i].m_Edge;
				if (!m_DeletedData.HasComponent(edge))
				{
					if (isPermanent)
					{
						m_NodeQueue.Enqueue(new UpdateData(default(Node), edge, regenerate: false, updateOnly: true));
					}
					Edge edge2 = m_EdgeData[edge];
					if (edge2.m_End == node)
					{
						AddNode(edge2.m_Start, regenerate: false, isPermanent);
					}
					if (edge2.m_Start == node)
					{
						AddNode(edge2.m_End, regenerate: false, isPermanent);
					}
				}
			}
		}

		private void AddEdgesForLocalConnectOrAttachment(CoursePosFlags flags, Entity ignoreEntity, float3 position, float2 elevation, Entity prefab, bool isPermanent, bool isStandalone)
		{
			Bounds1 y = new Bounds1(float.MaxValue, float.MinValue);
			float2 @float = 0f;
			Layer layer = Layer.None;
			Layer layer2 = Layer.None;
			Layer layer3 = Layer.None;
			NetData netData = m_NetData[prefab];
			NetGeometryData netGeometryData = default(NetGeometryData);
			if (m_NetGeometryData.HasComponent(prefab))
			{
				netGeometryData = m_NetGeometryData[prefab];
			}
			if (math.all(elevation >= netGeometryData.m_ElevationLimit * 2f) || (!math.all(elevation < 0f) && (netData.m_RequiredLayers & (Layer.PowerlineLow | Layer.PowerlineHigh)) != Layer.None))
			{
				float num = netGeometryData.m_DefaultWidth * 0.5f;
				float num2 = position.y - math.cmin(elevation);
				y |= new Bounds1(num2 - num, num2 + num);
				@float = math.max(@float, num);
				layer |= Layer.Road;
			}
			if (m_PrefabLocalConnectData.HasComponent(prefab))
			{
				LocalConnectData localConnectData = m_PrefabLocalConnectData[prefab];
				if ((localConnectData.m_Flags & LocalConnectFlags.ExplicitNodes) == 0 || (flags & (CoursePosFlags.IsFirst | CoursePosFlags.IsLast)) != 0)
				{
					float2 y2 = netGeometryData.m_DefaultWidth * 0.5f + localConnectData.m_SearchDistance;
					y2.y += math.select(0f, 8f, !isStandalone && localConnectData.m_SearchDistance != 0f && (netGeometryData.m_Flags & Game.Net.GeometryFlags.SubOwner) == 0);
					y |= position.y + localConnectData.m_HeightRange;
					@float = math.max(@float, y2);
					layer2 |= localConnectData.m_Layers;
					layer3 |= netData.m_ConnectLayers;
				}
			}
			if (layer != Layer.None || layer2 != Layer.None || layer3 != Layer.None)
			{
				float num3 = math.cmax(@float);
				Bounds3 bounds = new Bounds3(position - num3, position + num3)
				{
					y = y
				};
				EdgeIterator iterator = new EdgeIterator
				{
					m_Bounds = bounds,
					m_Position = position.xz,
					m_ConnectRadius = @float,
					m_AttachLayers = layer,
					m_ConnectLayers = layer2,
					m_LocalConnectLayers = layer3,
					m_IgnoreEntity = ignoreEntity,
					m_IsPermanent = isPermanent,
					m_EdgeData = m_EdgeData,
					m_NodeData = m_NodeData,
					m_CurveData = m_CurveData,
					m_DeletedData = m_DeletedData,
					m_OwnerData = m_OwnerData,
					m_RoundaboutData = m_RoundaboutData,
					m_PrefabRefData = m_PrefabRefData,
					m_NetData = m_NetData,
					m_NetGeometryData = m_NetGeometryData,
					m_RoadData = m_RoadData,
					m_Edges = m_Edges,
					m_NodeQueue = m_NodeQueue
				};
				m_NetSearchTree.Iterate(ref iterator);
			}
		}

		private void AddNodesForLocalConnect(Bezier4x3 curve, Entity prefab, bool isPermanent, bool isStandalone)
		{
			NetData netData = m_NetData[prefab];
			m_NetGeometryData.TryGetComponent(prefab, out var componentData);
			float2 @float = componentData.m_DefaultWidth * 0.5f;
			if (m_RoadData.HasComponent(prefab))
			{
				@float.y += math.select(0f, 8f, isStandalone && (m_RoadData[prefab].m_Flags & Game.Prefabs.RoadFlags.EnableZoning) != 0);
			}
			float num = math.cmax(@float) + 4f;
			NodeIterator iterator = new NodeIterator
			{
				m_Bounds = MathUtils.Expand(MathUtils.Bounds(curve), new float3(num, 1000f, num)),
				m_Curve = curve,
				m_ConnectRadius = @float,
				m_ConnectLayers = netData.m_ConnectLayers,
				m_IsPermanent = isPermanent,
				m_EdgeData = m_EdgeData,
				m_NodeData = m_NodeData,
				m_OwnerData = m_OwnerData,
				m_LocalConnectData = m_LocalConnectData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabLocalConnectData = m_PrefabLocalConnectData,
				m_NetGeometryData = m_NetGeometryData,
				m_Edges = m_Edges,
				m_NodeQueue = m_NodeQueue
			};
			m_NetSearchTree.Iterate(ref iterator);
		}

		private void AddNode(CreationDefinition definitionData, OwnerDefinition ownerData, CoursePos coursePos, Upgraded upgraded, int fixedIndex, float3 cachedPosition, bool hasCachedPosition, bool addEdge)
		{
			if (definitionData.m_Prefab == Entity.Null && coursePos.m_Entity == Entity.Null)
			{
				return;
			}
			Bounds1 curveBounds = new Bounds1(0f, 1f);
			if (m_CurveData.TryGetComponent(coursePos.m_Entity, out var componentData))
			{
				curveBounds = new Bounds1(coursePos.m_SplitPosition);
				if (m_NetGeometryData.TryGetComponent(definitionData.m_Prefab, out var componentData2))
				{
					float length = componentData2.m_DefaultWidth * 0.5f;
					MathUtils.Distance(componentData.m_Bezier.xz, coursePos.m_Position.xz, out var t);
					Bounds1 t2 = new Bounds1(0f, t);
					Bounds1 t3 = new Bounds1(t, 1f);
					MathUtils.ClampLengthInverse(componentData.m_Bezier.xz, ref t2, length);
					MathUtils.ClampLength(componentData.m_Bezier.xz, ref t3, length);
					curveBounds |= new Bounds1(t2.min, t3.max);
				}
			}
			else
			{
				coursePos.m_SplitPosition = 0f;
				if ((definitionData.m_Flags & CreationFlags.Permanent) != 0 && m_NodeData.TryGetComponent(coursePos.m_Entity, out var componentData3))
				{
					m_NodeQueue.Enqueue(new UpdateData(componentData3, coursePos.m_Entity, regenerate: false, updateOnly: true));
					return;
				}
			}
			m_NodeQueue.Enqueue(new UpdateData(definitionData, ownerData, coursePos, upgraded, fixedIndex, cachedPosition, curveBounds, hasCachedPosition, addEdge));
		}

		private void AddNode(Entity original, bool regenerate, bool isPermanent)
		{
			Node node = m_NodeData[original];
			m_NodeQueue.Enqueue(new UpdateData(node, original, regenerate && !isPermanent, isPermanent));
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CollectUpdatesJob : IJob
	{
		public NativeQueue<UpdateData> m_UpdateQueue;

		public NativeList<UpdateData> m_UpdateList;

		public void Execute()
		{
			int count = m_UpdateQueue.Count;
			NativeParallelMultiHashMap<NodeKey, int> nativeParallelMultiHashMap = new NativeParallelMultiHashMap<NodeKey, int>(count, Allocator.Temp);
			for (int i = 0; i < count; i++)
			{
				UpdateData value = m_UpdateQueue.Dequeue();
				NodeKey key = new NodeKey(value);
				int num = -1;
				if (nativeParallelMultiHashMap.TryGetFirstValue(key, out var item, out var it))
				{
					do
					{
						UpdateData updateData = m_UpdateList[item];
						if (!updateData.m_Valid || !MathUtils.Intersect(value.m_CurveBounds, updateData.m_CurveBounds))
						{
							continue;
						}
						if (value.m_OnCourse)
						{
							if (updateData.m_OnCourse)
							{
								value.m_AddEdge |= updateData.m_AddEdge;
								value.m_FixedIndex = math.min(value.m_FixedIndex, updateData.m_FixedIndex);
								value.m_CreationFlags |= updateData.m_CreationFlags;
								value.m_UpgradeFlags |= updateData.m_UpgradeFlags;
								value.m_RandomSeed ^= updateData.m_RandomSeed;
							}
							value.m_CurveBounds |= updateData.m_CurveBounds;
						}
						else if (!value.m_Regenerate || updateData.m_OnCourse)
						{
							updateData.m_CurveBounds |= value.m_CurveBounds;
							value = updateData;
						}
						if (num == -1)
						{
							num = item;
						}
						else
						{
							m_UpdateList[item] = default(UpdateData);
						}
					}
					while (nativeParallelMultiHashMap.TryGetNextValue(out item, ref it));
				}
				if (num == -1)
				{
					nativeParallelMultiHashMap.Add(key, m_UpdateList.Length);
					m_UpdateList.Add(in value);
				}
				else
				{
					m_UpdateList[num] = value;
				}
			}
		}
	}

	[BurstCompile]
	private struct CreateNodesJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_NetGeometryData;

		[ReadOnly]
		public ComponentLookup<LocalConnectData> m_PrefabLocalConnectData;

		[ReadOnly]
		public ComponentLookup<NetData> m_NetData;

		[ReadOnly]
		public ComponentLookup<RoadData> m_PrefabRoadData;

		[ReadOnly]
		public ComponentLookup<PrefabData> m_PrefabData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<LocalConnect> m_LocalConnectData;

		[ReadOnly]
		public ComponentLookup<EditorContainer> m_EditorContainerData;

		[ReadOnly]
		public ComponentLookup<Standalone> m_StandaloneData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Native> m_NativeData;

		[ReadOnly]
		public ComponentLookup<Fixed> m_FixedData;

		[ReadOnly]
		public ComponentLookup<Upgraded> m_UpgradedData;

		[ReadOnly]
		public ComponentLookup<NetCondition> m_ConditionData;

		[ReadOnly]
		public ComponentLookup<Road> m_RoadData;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> m_ServiceUpgradeData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_Edges;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public Bounds3 m_TerrainBounds;

		[ReadOnly]
		public NativeList<UpdateData> m_UpdateList;

		[ReadOnly]
		public NativeParallelHashMap<Entity, DefinitionData> m_DefinitionMap;

		[ReadOnly]
		public NativeHashMap<OwnerDefinition, Entity> m_ReusedOwnerMap;

		public NativeParallelMultiHashMap<OldNodeKey, OldNodeValue> m_OldNodeMap;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			for (int i = 0; i < m_UpdateList.Length; i++)
			{
				Execute(i);
			}
		}

		public void Execute(int index)
		{
			UpdateData updateData = m_UpdateList[index];
			if (!updateData.m_Valid)
			{
				return;
			}
			if (updateData.m_UpdateOnly)
			{
				m_CommandBuffer.AddComponent(updateData.m_Original, default(Updated));
				return;
			}
			Node node = new Node
			{
				m_Position = updateData.m_Position,
				m_Rotation = updateData.m_Rotation
			};
			Temp component = new Temp
			{
				m_Original = updateData.m_Original
			};
			if ((updateData.m_CreationFlags & CreationFlags.SubElevation) != 0 && updateData.m_OnCourse)
			{
				component.m_Flags |= TempFlags.Essential;
			}
			if ((updateData.m_Flags & (CoursePosFlags.IsLast | CoursePosFlags.IsParallel)) == CoursePosFlags.IsLast)
			{
				component.m_Flags |= TempFlags.IsLast;
			}
			Upgraded component2 = new Upgraded
			{
				m_Flags = updateData.m_UpgradeFlags
			};
			bool flag = false;
			if (m_NodeData.HasComponent(updateData.m_Original))
			{
				if (updateData.m_OnCourse && ((updateData.m_Owner == Entity.Null && updateData.m_OwnerData.m_Prefab == Entity.Null) || (m_OwnerData.TryGetComponent(updateData.m_Original, out var componentData) && !TryingToDelete(componentData.m_Owner))))
				{
					node = m_NodeData[updateData.m_Original];
				}
				bool alreadyOrphan = false;
				flag = !updateData.m_AddEdge && WillBeOrphan(updateData.m_Original, out alreadyOrphan);
				if (flag && CanDelete(updateData.m_Original) && (!alreadyOrphan || (updateData.m_CreationFlags & CreationFlags.Delete) != 0))
				{
					flag = alreadyOrphan;
					component.m_Flags |= TempFlags.Delete;
				}
				else if (updateData.m_OnCourse)
				{
					if ((updateData.m_CreationFlags & CreationFlags.Upgrade) != 0)
					{
						component.m_Flags |= TempFlags.Upgrade;
					}
					else if ((updateData.m_CreationFlags & CreationFlags.Select) != 0)
					{
						component.m_Flags |= TempFlags.Select;
					}
					else
					{
						component.m_Flags |= TempFlags.Regenerate;
					}
					if ((updateData.m_CreationFlags & CreationFlags.Parent) != 0)
					{
						component.m_Flags |= TempFlags.Parent;
					}
				}
				else if (updateData.m_Regenerate)
				{
					component.m_Flags |= TempFlags.Regenerate;
				}
				if ((updateData.m_CreationFlags & CreationFlags.Upgrade) == 0 && m_UpgradedData.HasComponent(updateData.m_Original))
				{
					component2 = m_UpgradedData[updateData.m_Original];
				}
			}
			else if (m_EdgeData.HasComponent(updateData.m_Original))
			{
				component.m_Flags |= TempFlags.Replace;
				component.m_CurvePosition = updateData.m_CurvePosition;
			}
			else
			{
				flag = !updateData.m_AddEdge;
				component.m_Flags |= TempFlags.Create;
			}
			if ((updateData.m_CreationFlags & CreationFlags.Hidden) != 0 && ((updateData.m_CreationFlags & CreationFlags.Delete) == 0 || (component.m_Flags & TempFlags.Delete) != 0))
			{
				component.m_Flags |= TempFlags.Hidden;
			}
			PrefabRef component3 = new PrefabRef
			{
				m_Prefab = updateData.m_Prefab
			};
			bool flag2 = false;
			bool hasNativeEdges = false;
			if (updateData.m_Original != Entity.Null)
			{
				m_CommandBuffer.AddComponent(updateData.m_Original, default(Hidden));
				m_CommandBuffer.AddComponent(updateData.m_Original, default(BatchesUpdated));
				if (m_StandaloneData.HasComponent(updateData.m_Original))
				{
					flag2 = true;
					if (!flag && (component.m_Flags & TempFlags.Delete) == 0 && m_OwnerData.TryGetComponent(updateData.m_Original, out var componentData2) && (componentData2.m_Owner == Entity.Null || TryingToDelete(componentData2.m_Owner)))
					{
						flag2 = false;
					}
				}
				if (!flag2 && updateData.m_Prefab != Entity.Null && (updateData.m_Owner != Entity.Null || updateData.m_OwnerData.m_Prefab != Entity.Null) && (updateData.m_Flags & (CoursePosFlags.IsFirst | CoursePosFlags.IsLast)) == (CoursePosFlags.IsFirst | CoursePosFlags.IsLast))
				{
					flag2 = true;
					component3.m_Prefab = updateData.m_Prefab;
				}
				else if (flag2)
				{
					component3 = m_PrefabRefData[updateData.m_Original];
					updateData.m_Lane = GetEditorLane(updateData.m_Original);
				}
				else
				{
					FindNodePrefab(updateData.m_Original, updateData.m_Prefab, updateData.m_Lane, out component3.m_Prefab, out var lanePrefab, out hasNativeEdges);
					updateData.m_Lane = lanePrefab;
				}
			}
			else if (flag)
			{
				flag2 = true;
			}
			NetData netData = m_NetData[component3.m_Prefab];
			NetGeometryData netGeometryData = default(NetGeometryData);
			bool flag3 = false;
			if (m_NetGeometryData.HasComponent(component3.m_Prefab))
			{
				netGeometryData = m_NetGeometryData[component3.m_Prefab];
				flag3 = true;
			}
			if (((netGeometryData.m_Flags & Game.Net.GeometryFlags.StrictNodes) != 0 || !flag3) && updateData.m_Prefab != Entity.Null && updateData.m_Original != Entity.Null && (!flag2 || m_StandaloneData.HasComponent(updateData.m_Original)))
			{
				if (m_NodeData.HasComponent(updateData.m_Original))
				{
					Node node2 = m_NodeData[updateData.m_Original];
					node.m_Position = node2.m_Position;
					node.m_Rotation = node2.m_Rotation;
				}
				else if (m_CurveData.HasComponent(updateData.m_Original))
				{
					Curve curve = m_CurveData[updateData.m_Original];
					node.m_Position = MathUtils.Position(curve.m_Bezier, updateData.m_CurvePosition);
					float2 value = MathUtils.Tangent(curve.m_Bezier, updateData.m_CurvePosition).xz;
					if (MathUtils.TryNormalize(ref value))
					{
						node.m_Rotation = quaternion.LookRotation(new float3(value.x, 0f, value.y), math.up());
					}
				}
			}
			if (m_PrefabRefData.TryGetComponent(updateData.m_Original, out var componentData3) && ((m_NetData[componentData3.m_Prefab].m_RequiredLayers ^ netData.m_RequiredLayers) & Layer.Waterway) != Layer.None)
			{
				node.m_Position.y = updateData.m_Position.y;
			}
			bool flag4 = false;
			Owner componentData4;
			if (updateData.m_OwnerData.m_Prefab != Entity.Null)
			{
				if ((component.m_Flags & TempFlags.Delete) == 0 && m_OwnerData.HasComponent(updateData.m_Original))
				{
					Entity owner = m_OwnerData[updateData.m_Original].m_Owner;
					if (owner != Entity.Null && TryingToDelete(owner))
					{
						updateData.m_OwnerData = default(OwnerDefinition);
						updateData.m_Owner = Entity.Null;
					}
				}
			}
			else if (updateData.m_Owner == Entity.Null && m_OwnerData.TryGetComponent(updateData.m_Original, out componentData4) && componentData4.m_Owner != Entity.Null && !TryingToDelete(componentData4.m_Owner) && !TryingToDelete(updateData.m_Original))
			{
				updateData.m_Owner = componentData4.m_Owner;
				if (m_CurveData.TryGetComponent(componentData4.m_Owner, out var componentData5) && m_PrefabRefData.TryGetComponent(componentData4.m_Owner, out var componentData6))
				{
					flag4 = true;
					updateData.m_OwnerData = new OwnerDefinition
					{
						m_Prefab = componentData6.m_Prefab,
						m_Position = componentData5.m_Bezier.a,
						m_Rotation = new float4(componentData5.m_Bezier.d, 0f)
					};
				}
			}
			float num = 0.1f;
			bool flag5 = !MathUtils.Intersect(MathUtils.Expand(m_TerrainBounds.xz, 0f - num), node.m_Position.xz);
			Entity oldEntity = Entity.Null;
			bool flag6 = (updateData.m_CreationFlags & CreationFlags.Permanent) == 0 && TryGetOldEntity(node, component3.m_Prefab, updateData.m_Lane, component.m_Original, flag5, ref updateData.m_OwnerData, ref updateData.m_Owner, out oldEntity);
			if (flag6)
			{
				m_CommandBuffer.RemoveComponent<Deleted>(oldEntity);
				m_CommandBuffer.AddComponent(oldEntity, default(Updated));
				m_CommandBuffer.SetBuffer<ConnectedEdge>(oldEntity);
			}
			else
			{
				oldEntity = m_CommandBuffer.CreateEntity(netData.m_NodeArchetype);
				m_CommandBuffer.SetComponent(oldEntity, component3);
				bool num2;
				if (!(component.m_Original != Entity.Null))
				{
					if (m_EditorMode)
					{
						goto IL_08ad;
					}
					num2 = (updateData.m_CreationFlags & CreationFlags.SubElevation) == 0;
				}
				else
				{
					num2 = !m_ServiceUpgradeData.HasComponent(component.m_Original);
				}
				if (num2)
				{
					goto IL_08ad;
				}
			}
			goto IL_08ba;
			IL_08ad:
			m_CommandBuffer.RemoveComponent<Game.Buildings.ServiceUpgrade>(oldEntity);
			goto IL_08ba;
			IL_08ba:
			m_CommandBuffer.SetComponent(oldEntity, node);
			if (flag && flag3)
			{
				m_CommandBuffer.AddComponent(oldEntity, default(Orphan));
			}
			else if (flag6)
			{
				m_CommandBuffer.RemoveComponent<Orphan>(oldEntity);
			}
			if (flag3)
			{
				if (m_PseudoRandomSeedData.HasComponent(updateData.m_Original))
				{
					m_CommandBuffer.SetComponent(oldEntity, m_PseudoRandomSeedData[updateData.m_Original]);
				}
				else
				{
					m_CommandBuffer.SetComponent(oldEntity, new PseudoRandomSeed((ushort)updateData.m_RandomSeed));
				}
			}
			if (flag2)
			{
				m_CommandBuffer.AddComponent(oldEntity, default(Standalone));
			}
			else if (flag6 && m_StandaloneData.HasComponent(oldEntity))
			{
				m_CommandBuffer.RemoveComponent<Standalone>(oldEntity);
			}
			if (updateData.m_Lane != Entity.Null)
			{
				m_CommandBuffer.SetComponent(oldEntity, new EditorContainer
				{
					m_Prefab = updateData.m_Lane
				});
			}
			component2.m_Flags &= CompositionFlags.nodeMask;
			if (component2.m_Flags != default(CompositionFlags))
			{
				m_CommandBuffer.AddComponent(oldEntity, component2);
			}
			else if (flag6 && m_UpgradedData.HasComponent(oldEntity))
			{
				m_CommandBuffer.RemoveComponent<Upgraded>(oldEntity);
			}
			bool flag7 = true;
			bool flag8 = true;
			bool flag9 = true;
			bool num3 = m_NodeData.HasComponent(updateData.m_Original);
			bool flag10 = !num3 && m_EdgeData.HasComponent(updateData.m_Original);
			if (num3 || flag10)
			{
				if (m_ElevationData.TryGetComponent(updateData.m_Original, out var componentData7))
				{
					if (math.any(componentData7.m_Elevation != 0f) || updateData.m_ParentMesh >= 0 || m_OwnerData.HasComponent(updateData.m_Original))
					{
						m_CommandBuffer.AddComponent(oldEntity, componentData7);
						flag7 = false;
					}
				}
				else if (updateData.m_ParentMesh >= 0)
				{
					m_CommandBuffer.AddComponent(oldEntity, default(Game.Net.Elevation));
					flag7 = false;
				}
			}
			else if (math.any(updateData.m_Elevation != 0f) || updateData.m_ParentMesh >= 0)
			{
				m_CommandBuffer.AddComponent(oldEntity, new Game.Net.Elevation(updateData.m_Elevation));
				flag7 = false;
			}
			if (num3)
			{
				if (m_NativeData.HasComponent(updateData.m_Original) && (flag2 || hasNativeEdges))
				{
					m_CommandBuffer.AddComponent(oldEntity, default(Native));
				}
				else if (flag6 && m_NativeData.HasComponent(oldEntity))
				{
					m_CommandBuffer.RemoveComponent<Native>(oldEntity);
				}
				if (m_FixedData.HasComponent(updateData.m_Original))
				{
					m_CommandBuffer.AddComponent(oldEntity, m_FixedData[updateData.m_Original]);
					flag8 = false;
				}
				if (m_PrefabRoadData.HasComponent(component3.m_Prefab) && m_PrefabData.IsComponentEnabled(component3.m_Prefab))
				{
					if (m_ConditionData.TryGetComponent(updateData.m_Original, out var componentData8))
					{
						m_CommandBuffer.SetComponent(oldEntity, componentData8);
					}
					if (m_RoadData.TryGetComponent(updateData.m_Original, out var componentData9))
					{
						m_CommandBuffer.SetComponent(oldEntity, componentData9);
					}
				}
				if (m_PrefabLocalConnectData.HasComponent(component3.m_Prefab))
				{
					LocalConnectData localConnectData = m_PrefabLocalConnectData[component3.m_Prefab];
					if ((localConnectData.m_Flags & LocalConnectFlags.ExplicitNodes) == 0 || flag)
					{
						m_CommandBuffer.AddComponent(oldEntity, default(LocalConnect));
						flag9 = false;
					}
					else if (m_LocalConnectData.HasComponent(updateData.m_Original) && ((localConnectData.m_Flags & LocalConnectFlags.KeepOpen) != 0 || HasLocalConnections(updateData.m_Original)))
					{
						m_CommandBuffer.AddComponent(oldEntity, default(LocalConnect));
						flag9 = false;
					}
				}
			}
			else
			{
				if ((updateData.m_Flags & CoursePosFlags.IsFixed) != 0)
				{
					m_CommandBuffer.AddComponent(oldEntity, new Fixed
					{
						m_Index = updateData.m_FixedIndex
					});
					flag8 = false;
				}
				if (m_PrefabRoadData.HasComponent(component3.m_Prefab) && m_PrefabData.IsComponentEnabled(component3.m_Prefab))
				{
					if (m_ConditionData.TryGetComponent(updateData.m_Original, out var componentData10))
					{
						componentData10.m_Wear = math.lerp(componentData10.m_Wear.x, componentData10.m_Wear.y, updateData.m_CurvePosition);
						m_CommandBuffer.SetComponent(oldEntity, componentData10);
					}
					if (m_RoadData.TryGetComponent(updateData.m_Original, out var componentData11))
					{
						componentData11.m_TrafficFlowDistance0 = (componentData11.m_TrafficFlowDistance0 + componentData11.m_TrafficFlowDistance1) * 0.5f;
						componentData11.m_TrafficFlowDuration0 = (componentData11.m_TrafficFlowDuration0 + componentData11.m_TrafficFlowDuration1) * 0.5f;
						componentData11.m_TrafficFlowDistance1 = componentData11.m_TrafficFlowDistance0;
						componentData11.m_TrafficFlowDuration1 = componentData11.m_TrafficFlowDuration0;
						m_CommandBuffer.SetComponent(oldEntity, componentData11);
					}
				}
				if (m_PrefabLocalConnectData.HasComponent(component3.m_Prefab) && ((m_PrefabLocalConnectData[component3.m_Prefab].m_Flags & LocalConnectFlags.ExplicitNodes) == 0 || (updateData.m_Flags & (CoursePosFlags.IsFirst | CoursePosFlags.IsLast)) != 0))
				{
					m_CommandBuffer.AddComponent(oldEntity, default(LocalConnect));
					flag9 = false;
				}
			}
			if (flag6)
			{
				if (flag7 && m_ElevationData.HasComponent(oldEntity))
				{
					m_CommandBuffer.RemoveComponent<Game.Net.Elevation>(oldEntity);
				}
				if (flag8 && m_FixedData.HasComponent(oldEntity))
				{
					m_CommandBuffer.RemoveComponent<Fixed>(oldEntity);
				}
				if (flag9 && m_LocalConnectData.HasComponent(oldEntity))
				{
					m_CommandBuffer.RemoveComponent<LocalConnect>(oldEntity);
				}
			}
			if (updateData.m_OwnerData.m_Prefab != Entity.Null)
			{
				m_CommandBuffer.AddComponent(oldEntity, new Owner(flag4 ? updateData.m_Owner : Entity.Null));
				m_CommandBuffer.AddComponent(oldEntity, updateData.m_OwnerData);
			}
			else if (updateData.m_Owner != Entity.Null)
			{
				m_CommandBuffer.AddComponent(oldEntity, new Owner(updateData.m_Owner));
			}
			if (flag5)
			{
				m_CommandBuffer.AddComponent(oldEntity, default(Game.Net.OutsideConnection));
			}
			if ((updateData.m_CreationFlags & CreationFlags.Permanent) == 0)
			{
				m_CommandBuffer.AddComponent(oldEntity, component);
			}
			if (updateData.m_HasCachedPosition)
			{
				LocalTransformCache component4 = default(LocalTransformCache);
				component4.m_Position = updateData.m_CachedPosition;
				component4.m_Rotation = quaternion.identity;
				component4.m_ParentMesh = updateData.m_ParentMesh;
				component4.m_GroupIndex = 0;
				component4.m_Probability = 100;
				component4.m_PrefabSubIndex = -1;
				m_CommandBuffer.AddComponent(oldEntity, component4);
			}
		}

		private bool TryGetOldEntity(Node node, Entity prefab, Entity subPrefab, Entity original, bool isOutsideConnection, ref OwnerDefinition ownerDefinition, ref Entity owner, out Entity oldEntity)
		{
			Transform transform = default(Transform);
			bool flag = false;
			Transform componentData;
			if (ownerDefinition.m_Prefab != Entity.Null && m_ReusedOwnerMap.TryGetValue(ownerDefinition, out var item))
			{
				transform.m_Position = ownerDefinition.m_Position;
				transform.m_Rotation = ownerDefinition.m_Rotation;
				owner = item;
				ownerDefinition = default(OwnerDefinition);
				flag = true;
			}
			else if (m_TransformData.TryGetComponent(owner, out componentData))
			{
				transform = componentData;
				flag = true;
			}
			OldNodeKey key = default(OldNodeKey);
			key.m_Prefab = prefab;
			key.m_SubPrefab = subPrefab;
			key.m_Original = original;
			key.m_Owner = owner;
			key.m_OutsideConnection = isOutsideConnection;
			if (m_OldNodeMap.TryGetFirstValue(key, out var item2, out var it))
			{
				float3 @float = node.m_Position;
				float num = float.MaxValue;
				Entity entity = item2.m_Entity;
				NativeParallelMultiHashMapIterator<OldNodeKey> it2 = it;
				if (flag)
				{
					@float = ObjectUtils.WorldToLocal(ObjectUtils.InverseTransform(transform), @float);
				}
				do
				{
					float num2 = math.distancesq(@float, item2.m_Position);
					if (num2 < num)
					{
						num = num2;
						entity = item2.m_Entity;
						it2 = it;
					}
				}
				while (m_OldNodeMap.TryGetNextValue(out item2, ref it));
				oldEntity = entity;
				m_OldNodeMap.Remove(it2);
				return true;
			}
			oldEntity = Entity.Null;
			return false;
		}

		private bool HasLocalConnections(Entity node)
		{
			if (m_Edges.HasBuffer(node))
			{
				DynamicBuffer<ConnectedEdge> dynamicBuffer = m_Edges[node];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity edge = dynamicBuffer[i].m_Edge;
					Edge edge2 = m_EdgeData[edge];
					if (edge2.m_Start != node && edge2.m_End != node)
					{
						return true;
					}
				}
			}
			return false;
		}

		private void FindNodePrefab(Entity original, Entity newPrefab, Entity newLane, out Entity netPrefab, out Entity lanePrefab, out bool hasNativeEdges)
		{
			netPrefab = Entity.Null;
			lanePrefab = Entity.Null;
			float num = float.MinValue;
			hasNativeEdges = false;
			if (m_EdgeData.HasComponent(original))
			{
				hasNativeEdges = m_NativeData.HasComponent(original);
				PrefabRef prefabRef = m_PrefabRefData[original];
				NetData netData = m_NetData[prefabRef.m_Prefab];
				if (netData.m_NodePriority > num)
				{
					netPrefab = prefabRef.m_Prefab;
					lanePrefab = GetEditorLane(original);
					num = netData.m_NodePriority;
				}
			}
			else if (m_Edges.HasBuffer(original))
			{
				DynamicBuffer<ConnectedEdge> dynamicBuffer = m_Edges[original];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity edge = dynamicBuffer[i].m_Edge;
					Edge edge2 = m_EdgeData[edge];
					if (edge2.m_Start != original && edge2.m_End != original)
					{
						continue;
					}
					if (m_DefinitionMap.TryGetValue(edge, out var item))
					{
						if ((item.m_Flags & CreationFlags.Delete) != 0)
						{
							continue;
						}
						if (item.m_Prefab != Entity.Null)
						{
							hasNativeEdges |= m_NativeData.HasComponent(edge);
							NetData netData2 = m_NetData[item.m_Prefab];
							if (netData2.m_NodePriority > num)
							{
								netPrefab = item.m_Prefab;
								lanePrefab = item.m_Lane;
								num = netData2.m_NodePriority;
							}
							continue;
						}
					}
					hasNativeEdges |= m_NativeData.HasComponent(edge);
					PrefabRef prefabRef2 = m_PrefabRefData[edge];
					NetData netData3 = m_NetData[prefabRef2.m_Prefab];
					if (netData3.m_NodePriority > num)
					{
						netPrefab = prefabRef2.m_Prefab;
						lanePrefab = GetEditorLane(edge);
						num = netData3.m_NodePriority;
					}
				}
			}
			if (newPrefab != Entity.Null)
			{
				NetData netData4 = m_NetData[newPrefab];
				if (netData4.m_NodePriority > num)
				{
					netPrefab = newPrefab;
					lanePrefab = newLane;
					num = netData4.m_NodePriority;
				}
			}
			if (netPrefab == Entity.Null && m_PrefabRefData.HasComponent(original))
			{
				netPrefab = m_PrefabRefData[original].m_Prefab;
				lanePrefab = GetEditorLane(original);
			}
		}

		private Entity GetEditorLane(Entity entity)
		{
			if (m_EditorContainerData.HasComponent(entity))
			{
				return m_EditorContainerData[entity].m_Prefab;
			}
			return Entity.Null;
		}

		private bool TryingToDelete(Entity entity)
		{
			if (m_DefinitionMap.TryGetValue(entity, out var item))
			{
				return (item.m_Flags & (CreationFlags.Delete | CreationFlags.Relocate)) != 0;
			}
			return false;
		}

		private bool WillBeOrphan(Entity node, out bool alreadyOrphan)
		{
			DynamicBuffer<ConnectedEdge> dynamicBuffer = m_Edges[node];
			alreadyOrphan = true;
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity edge = dynamicBuffer[i].m_Edge;
				Edge edge2 = m_EdgeData[edge];
				if (edge2.m_Start == node || edge2.m_End == node)
				{
					alreadyOrphan = false;
					if (!m_DefinitionMap.TryGetValue(edge, out var item))
					{
						return false;
					}
					if ((item.m_Flags & CreationFlags.Delete) == 0)
					{
						return false;
					}
				}
			}
			return true;
		}

		private bool CanDelete(Entity node)
		{
			if (TryingToDelete(node))
			{
				return true;
			}
			if (m_OwnerData.HasComponent(node))
			{
				Entity owner = m_OwnerData[node].m_Owner;
				if (owner != Entity.Null && !m_EditorMode && !TryingToDelete(owner))
				{
					DynamicBuffer<ConnectedEdge> dynamicBuffer = m_Edges[node];
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						Entity edge = dynamicBuffer[i].m_Edge;
						if (TryingToDelete(edge) && m_OwnerData.HasComponent(edge))
						{
							Edge edge2 = m_EdgeData[edge];
							Entity owner2 = m_OwnerData[edge].m_Owner;
							if ((edge2.m_Start == node || edge2.m_End == node) && owner2 == owner)
							{
								return true;
							}
						}
					}
					return false;
				}
			}
			return true;
		}
	}

	private struct TypeHandle
	{
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
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LocalConnect> __Game_Net_LocalConnect_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Roundabout> __Game_Net_Roundabout_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LocalConnectData> __Game_Prefabs_LocalConnectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RoadData> __Game_Prefabs_RoadData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedNode> __Game_Net_ConnectedNode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Node> __Game_Net_Node_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EditorContainer> __Game_Tools_EditorContainer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Net.OutsideConnection> __Game_Net_OutsideConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> __Game_Net_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EditorContainer> __Game_Tools_EditorContainer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Standalone> __Game_Net_Standalone_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Native> __Game_Common_Native_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Fixed> __Game_Net_Fixed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Upgraded> __Game_Net_Upgraded_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCondition> __Game_Net_NetCondition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Road> __Game_Net_Road_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> __Game_Buildings_ServiceUpgrade_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Tools_CreationDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreationDefinition>(isReadOnly: true);
			__Game_Tools_OwnerDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<OwnerDefinition>(isReadOnly: true);
			__Game_Tools_NetCourse_RO_ComponentTypeHandle = state.GetComponentTypeHandle<NetCourse>(isReadOnly: true);
			__Game_Tools_LocalCurveCache_RO_ComponentTypeHandle = state.GetComponentTypeHandle<LocalCurveCache>(isReadOnly: true);
			__Game_Net_Upgraded_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Upgraded>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Net_LocalConnect_RO_ComponentLookup = state.GetComponentLookup<LocalConnect>(isReadOnly: true);
			__Game_Net_Roundabout_RO_ComponentLookup = state.GetComponentLookup<Roundabout>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Prefabs_LocalConnectData_RO_ComponentLookup = state.GetComponentLookup<LocalConnectData>(isReadOnly: true);
			__Game_Prefabs_RoadData_RO_ComponentLookup = state.GetComponentLookup<RoadData>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Net_ConnectedNode_RO_BufferLookup = state.GetBufferLookup<ConnectedNode>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Node>(isReadOnly: true);
			__Game_Tools_EditorContainer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EditorContainer>(isReadOnly: true);
			__Game_Net_OutsideConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Net.OutsideConnection>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Prefabs_PrefabData_RO_ComponentLookup = state.GetComponentLookup<PrefabData>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Elevation>(isReadOnly: true);
			__Game_Tools_EditorContainer_RO_ComponentLookup = state.GetComponentLookup<EditorContainer>(isReadOnly: true);
			__Game_Net_Standalone_RO_ComponentLookup = state.GetComponentLookup<Standalone>(isReadOnly: true);
			__Game_Common_Native_RO_ComponentLookup = state.GetComponentLookup<Native>(isReadOnly: true);
			__Game_Net_Fixed_RO_ComponentLookup = state.GetComponentLookup<Fixed>(isReadOnly: true);
			__Game_Net_Upgraded_RO_ComponentLookup = state.GetComponentLookup<Upgraded>(isReadOnly: true);
			__Game_Net_NetCondition_RO_ComponentLookup = state.GetComponentLookup<NetCondition>(isReadOnly: true);
			__Game_Net_Road_RO_ComponentLookup = state.GetComponentLookup<Road>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentLookup = state.GetComponentLookup<PseudoRandomSeed>(isReadOnly: true);
			__Game_Buildings_ServiceUpgrade_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.ServiceUpgrade>(isReadOnly: true);
		}
	}

	private ToolSystem m_ToolSystem;

	private Game.Net.SearchSystem m_SearchSystem;

	private TerrainSystem m_TerrainSystem;

	private GenerateObjectsSystem m_GenerateObjectsSystem;

	private ModificationBarrier1 m_ModificationBarrier;

	private EntityQuery m_DefinitionQuery;

	private EntityQuery m_DeletedQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_SearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_GenerateObjectsSystem = base.World.GetOrCreateSystemManaged<GenerateObjectsSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier1>();
		m_DefinitionQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<CreationDefinition>(),
				ComponentType.ReadOnly<Updated>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<NetCourse>(),
				ComponentType.ReadOnly<ObjectDefinition>()
			}
		});
		m_DeletedQuery = GetEntityQuery(ComponentType.ReadOnly<Node>(), ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<PrefabRef>());
		RequireForUpdate(m_DefinitionQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeQueue<UpdateData> updateQueue = new NativeQueue<UpdateData>(Allocator.TempJob);
		NativeList<UpdateData> updateList = new NativeList<UpdateData>(Allocator.TempJob);
		NativeParallelHashMap<Entity, DefinitionData> definitionMap = new NativeParallelHashMap<Entity, DefinitionData>(m_DefinitionQuery.CalculateEntityCount(), Allocator.TempJob);
		NativeParallelMultiHashMap<OldNodeKey, OldNodeValue> oldNodeMap = new NativeParallelMultiHashMap<OldNodeKey, OldNodeValue>(32, Allocator.TempJob);
		JobHandle dependencies;
		NativeHashMap<OwnerDefinition, Entity> reusedOwnerMap = m_GenerateObjectsSystem.GetReusedOwnerMap(out dependencies);
		TerrainHeightData data = m_TerrainSystem.GetHeightData();
		Bounds3 bounds = TerrainUtils.GetBounds(ref data);
		JobHandle dependencies2;
		FillNodeMapJob jobData = new FillNodeMapJob
		{
			m_CreationDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_CreationDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_OwnerDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetCourseType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_NetCourse_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LocalCurveCacheType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_LocalCurveCache_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpgradedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Upgraded_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LocalConnectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LocalConnect_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RoundaboutData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Roundabout_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabLocalConnectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LocalConnectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RoadData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Edges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedNode_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_NodeQueue = updateQueue.AsParallelWriter(),
			m_DefinitionMap = definitionMap.AsParallelWriter(),
			m_NetSearchTree = m_SearchSystem.GetNetSearchTree(readOnly: true, out dependencies2)
		};
		FillOldNodesJob jobData2 = new FillOldNodesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NodeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Node_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EditorContainerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_EditorContainer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OutsideConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_OutsideConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OldNodeMap = oldNodeMap
		};
		CollectUpdatesJob jobData3 = new CollectUpdatesJob
		{
			m_UpdateQueue = updateQueue,
			m_UpdateList = updateList
		};
		CreateNodesJob jobData4 = new CreateNodesJob
		{
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabLocalConnectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LocalConnectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RoadData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LocalConnectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LocalConnect_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EditorContainerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_EditorContainer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StandaloneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Standalone_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NativeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Native_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FixedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Fixed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UpgradedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Upgraded_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConditionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NetCondition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Road_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceUpgradeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_ServiceUpgrade_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Edges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
			m_TerrainBounds = bounds,
			m_UpdateList = updateList,
			m_DefinitionMap = definitionMap,
			m_ReusedOwnerMap = reusedOwnerMap,
			m_OldNodeMap = oldNodeMap,
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_DefinitionQuery, JobHandle.CombineDependencies(base.Dependency, dependencies2));
		JobHandle job = JobChunkExtensions.Schedule(jobData2, m_DeletedQuery, base.Dependency);
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData3, jobHandle);
		JobHandle jobHandle3 = IJobExtensions.Schedule(jobData4, JobHandle.CombineDependencies(jobHandle2, job, dependencies));
		updateQueue.Dispose(jobHandle2);
		updateList.Dispose(jobHandle3);
		definitionMap.Dispose(jobHandle3);
		oldNodeMap.Dispose(jobHandle3);
		m_SearchSystem.AddNetSearchTreeReader(jobHandle);
		m_GenerateObjectsSystem.AddOwnerMapReader(jobHandle3);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle3);
		base.Dependency = jobHandle3;
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
	public GenerateNodesSystem()
	{
	}
}
