using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Effects;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using Game.Vehicles;
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
public class GenerateObjectsSystem : GameSystemBase
{
	private struct CreationData : IComparable<CreationData>
	{
		public CreationDefinition m_CreationDefinition;

		public OwnerDefinition m_OwnerDefinition;

		public ObjectDefinition m_ObjectDefinition;

		public Entity m_OldEntity;

		public bool m_HasDefinition;

		public CreationData(CreationDefinition creationDefinition, OwnerDefinition ownerDefinition, ObjectDefinition objectDefinition, bool hasDefinition)
		{
			m_CreationDefinition = creationDefinition;
			m_OwnerDefinition = ownerDefinition;
			m_ObjectDefinition = objectDefinition;
			m_OldEntity = Entity.Null;
			m_HasDefinition = hasDefinition;
		}

		public int CompareTo(CreationData other)
		{
			return m_CreationDefinition.m_Original.Index - other.m_CreationDefinition.m_Original.Index;
		}
	}

	private struct OldObjectKey : IEquatable<OldObjectKey>
	{
		public Entity m_Prefab;

		public Entity m_SubPrefab;

		public Entity m_Original;

		public Entity m_Owner;

		public bool Equals(OldObjectKey other)
		{
			if (m_Prefab.Equals(other.m_Prefab) && m_SubPrefab.Equals(other.m_SubPrefab) && m_Original.Equals(other.m_Original))
			{
				return m_Owner.Equals(other.m_Owner);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((17 * 31 + m_Prefab.GetHashCode()) * 31 + m_SubPrefab.GetHashCode()) * 31 + m_Original.GetHashCode()) * 31 + m_Owner.GetHashCode();
		}
	}

	private struct OldObjectValue
	{
		public Entity m_Entity;

		public Transform m_Transform;
	}

	[BurstCompile]
	private struct FillOldObjectsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<EditorContainer> m_EditorContainerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public NativeParallelMultiHashMap<OldObjectKey, OldObjectValue> m_OldObjectMap;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<Temp> nativeArray3 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<Transform> nativeArray4 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<EditorContainer> nativeArray5 = chunk.GetNativeArray(ref m_EditorContainerType);
			NativeArray<PrefabRef> nativeArray6 = chunk.GetNativeArray(ref m_PrefabRefType);
			OldObjectKey key = default(OldObjectKey);
			OldObjectValue item = default(OldObjectValue);
			for (int i = 0; i < nativeArray6.Length; i++)
			{
				key.m_Prefab = nativeArray6[i].m_Prefab;
				key.m_SubPrefab = Entity.Null;
				key.m_Original = nativeArray3[i].m_Original;
				key.m_Owner = Entity.Null;
				if (nativeArray5.Length != 0)
				{
					key.m_SubPrefab = nativeArray5[i].m_Prefab;
				}
				if (nativeArray2.Length != 0)
				{
					key.m_Owner = nativeArray2[i].m_Owner;
				}
				item.m_Entity = nativeArray[i];
				item.m_Transform = nativeArray4[i];
				m_OldObjectMap.Add(key, item);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FillCreationListJob : IJobChunk
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

			public FillCreationListJob m_JobData;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds, m_Bounds) || entity == m_IgnoreEntity || !m_JobData.m_CurveData.HasComponent(entity))
				{
					return;
				}
				PrefabRef prefabRef = m_JobData.m_PrefabRefData[entity];
				NetData netData = m_JobData.m_NetData[prefabRef.m_Prefab];
				if ((m_AttachLayers & netData.m_ConnectLayers) == 0 && ((m_ConnectLayers & netData.m_ConnectLayers) == 0 || (m_LocalConnectLayers & netData.m_LocalConnectLayers) == 0))
				{
					return;
				}
				Edge edge = m_JobData.m_EdgeData[entity];
				if (edge.m_Start == m_IgnoreEntity || edge.m_End == m_IgnoreEntity)
				{
					return;
				}
				Curve curve = m_JobData.m_CurveData[entity];
				NetGeometryData netGeometryData = m_JobData.m_NetGeometryData[prefabRef.m_Prefab];
				RoadData roadData = default(RoadData);
				if (m_JobData.m_RoadData.HasComponent(prefabRef.m_Prefab))
				{
					roadData = m_JobData.m_RoadData[prefabRef.m_Prefab];
				}
				float t;
				float num = MathUtils.Distance(curve.m_Bezier.xz, m_Position, out t);
				float num2 = math.select(m_ConnectRadius.x, m_ConnectRadius.y, !m_JobData.m_OwnerData.HasComponent(entity) && (roadData.m_Flags & Game.Prefabs.RoadFlags.EnableZoning) != 0);
				bool flag = num <= netGeometryData.m_DefaultWidth * 0.5f + num2;
				if (!flag)
				{
					if (m_JobData.m_RoundaboutData.TryGetComponent(edge.m_Start, out var componentData) && m_JobData.m_NodeData.TryGetComponent(edge.m_Start, out var componentData2) && math.distance(componentData2.m_Position.xz, m_Position) <= componentData.m_Radius + num2)
					{
						flag = true;
					}
					if (m_JobData.m_RoundaboutData.TryGetComponent(edge.m_End, out componentData) && m_JobData.m_NodeData.TryGetComponent(edge.m_End, out componentData2) && math.distance(componentData2.m_Position.xz, m_Position) <= componentData.m_Radius + num2)
					{
						flag = true;
					}
				}
				if (flag)
				{
					m_JobData.CheckSubObjects(entity);
					m_JobData.CheckSubObjects(edge.m_Start);
					m_JobData.CheckSubObjects(edge.m_End);
					m_JobData.CheckNodeEdges(edge.m_Start, edge.m_End);
					m_JobData.CheckNodeEdges(edge.m_End, edge.m_Start);
				}
			}
		}

		private struct NodeIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds3 m_Bounds;

			public Bezier4x3 m_Curve;

			public float2 m_ConnectRadius;

			public Layer m_ConnectLayers;

			public FillCreationListJob m_JobData;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (MathUtils.Intersect(bounds.m_Bounds, m_Bounds) && m_JobData.m_NodeData.HasComponent(entity))
				{
					CheckNode(entity);
				}
			}

			private void CheckNode(Entity entity)
			{
				if (!m_JobData.m_LocalConnectData.HasComponent(entity))
				{
					return;
				}
				PrefabRef prefabRef = m_JobData.m_PrefabRefData[entity];
				if (!m_JobData.m_PrefabLocalConnectData.HasComponent(prefabRef.m_Prefab))
				{
					return;
				}
				LocalConnectData localConnectData = m_JobData.m_PrefabLocalConnectData[prefabRef.m_Prefab];
				if ((m_ConnectLayers & localConnectData.m_Layers) == 0)
				{
					return;
				}
				NetGeometryData netGeometryData = m_JobData.m_NetGeometryData[prefabRef.m_Prefab];
				float num = math.max(0f, netGeometryData.m_DefaultWidth * 0.5f + localConnectData.m_SearchDistance);
				Game.Net.Node node = m_JobData.m_NodeData[entity];
				if (MathUtils.Intersect(bounds2: new Bounds3(node.m_Position - num, node.m_Position + num)
				{
					y = node.m_Position.y + localConnectData.m_HeightRange
				}, bounds1: m_Bounds))
				{
					float t;
					float num2 = MathUtils.Distance(m_Curve.xz, node.m_Position.xz, out t);
					float num3 = math.select(m_ConnectRadius.x, m_ConnectRadius.y, m_JobData.m_OwnerData.HasComponent(entity) && localConnectData.m_SearchDistance != 0f && (netGeometryData.m_Flags & Game.Net.GeometryFlags.SubOwner) == 0);
					if (num2 <= num + num3)
					{
						m_JobData.CheckSubObjects(entity);
						m_JobData.CheckNodeEdges(entity);
					}
				}
			}
		}

		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> m_CreationDefinitionType;

		[ReadOnly]
		public ComponentTypeHandle<OwnerDefinition> m_OwnerDefinitionType;

		[ReadOnly]
		public ComponentTypeHandle<ObjectDefinition> m_ObjectDefinitionType;

		[ReadOnly]
		public ComponentTypeHandle<NetCourse> m_NetCourseType;

		[ReadOnly]
		public ComponentLookup<Attached> m_AttachedData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectData> m_ObjectData;

		[ReadOnly]
		public ComponentLookup<NetData> m_NetData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_NetGeometryData;

		[ReadOnly]
		public ComponentLookup<LocalConnectData> m_PrefabLocalConnectData;

		[ReadOnly]
		public ComponentLookup<RoadData> m_RoadData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<LocalConnect> m_LocalConnectData;

		[ReadOnly]
		public ComponentLookup<Roundabout> m_RoundaboutData;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<ConnectedNode> m_ConnectedNodes;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		public NativeQueue<CreationData>.ParallelWriter m_CreationQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<CreationDefinition> nativeArray = chunk.GetNativeArray(ref m_CreationDefinitionType);
			NativeArray<OwnerDefinition> nativeArray2 = chunk.GetNativeArray(ref m_OwnerDefinitionType);
			NativeArray<ObjectDefinition> nativeArray3 = chunk.GetNativeArray(ref m_ObjectDefinitionType);
			NativeArray<NetCourse> nativeArray4 = chunk.GetNativeArray(ref m_NetCourseType);
			for (int i = 0; i < nativeArray3.Length; i++)
			{
				ObjectDefinition objectDefinition = nativeArray3[i];
				CreationDefinition creationDefinition = nativeArray[i];
				if (m_DeletedData.HasComponent(creationDefinition.m_Owner))
				{
					continue;
				}
				OwnerDefinition ownerDefinition = default(OwnerDefinition);
				if (nativeArray2.Length != 0)
				{
					ownerDefinition = nativeArray2[i];
				}
				if (m_ObjectData.HasComponent(creationDefinition.m_Prefab))
				{
					m_CreationQueue.Enqueue(new CreationData(creationDefinition, ownerDefinition, objectDefinition, hasDefinition: true));
				}
				else if (m_PrefabRefData.HasComponent(creationDefinition.m_Original))
				{
					PrefabRef prefabRef = m_PrefabRefData[creationDefinition.m_Original];
					if (m_ObjectData.HasComponent(prefabRef.m_Prefab))
					{
						m_CreationQueue.Enqueue(new CreationData(creationDefinition, ownerDefinition, objectDefinition, hasDefinition: true));
					}
				}
			}
			for (int j = 0; j < nativeArray4.Length; j++)
			{
				CreationDefinition creationDefinition2 = nativeArray[j];
				if (m_DeletedData.HasComponent(creationDefinition2.m_Owner) || (creationDefinition2.m_Flags & CreationFlags.Permanent) != 0)
				{
					continue;
				}
				NetCourse netCourse = nativeArray4[j];
				CheckSubObjects(netCourse.m_StartPosition.m_Entity);
				CheckSubObjects(netCourse.m_EndPosition.m_Entity);
				Entity deleteEdge = Entity.Null;
				if ((creationDefinition2.m_Flags & CreationFlags.Delete) != 0)
				{
					deleteEdge = creationDefinition2.m_Original;
				}
				if (netCourse.m_StartPosition.m_Entity != Entity.Null)
				{
					CheckConnectedEdges(netCourse.m_StartPosition.m_Entity, deleteEdge);
				}
				if (netCourse.m_EndPosition.m_Entity != Entity.Null)
				{
					CheckConnectedEdges(netCourse.m_EndPosition.m_Entity, deleteEdge);
				}
				bool isStandalone = nativeArray2.Length == 0;
				if (creationDefinition2.m_Prefab != Entity.Null)
				{
					CheckEdgesForLocalConnectOrAttachment(netCourse.m_StartPosition.m_Flags, netCourse.m_StartPosition.m_Entity, netCourse.m_StartPosition.m_Position, netCourse.m_StartPosition.m_Elevation, creationDefinition2.m_Prefab, isStandalone);
					if (!netCourse.m_StartPosition.m_Position.Equals(netCourse.m_EndPosition.m_Position))
					{
						CheckEdgesForLocalConnectOrAttachment(netCourse.m_EndPosition.m_Flags, netCourse.m_EndPosition.m_Entity, netCourse.m_EndPosition.m_Position, netCourse.m_EndPosition.m_Elevation, creationDefinition2.m_Prefab, isStandalone);
						Bezier4x3 curve = MathUtils.Cut(netCourse.m_Curve, new float2(netCourse.m_StartPosition.m_CourseDelta, netCourse.m_EndPosition.m_CourseDelta));
						CheckNodesForLocalConnect(curve, creationDefinition2.m_Prefab, isStandalone);
					}
				}
				else if (m_PrefabRefData.HasComponent(creationDefinition2.m_Original))
				{
					Entity prefab = m_PrefabRefData[creationDefinition2.m_Original].m_Prefab;
					CheckEdgesForLocalConnectOrAttachment(netCourse.m_StartPosition.m_Flags, netCourse.m_StartPosition.m_Entity, netCourse.m_StartPosition.m_Position, netCourse.m_StartPosition.m_Elevation, prefab, isStandalone);
					if (!netCourse.m_StartPosition.m_Position.Equals(netCourse.m_EndPosition.m_Position))
					{
						CheckEdgesForLocalConnectOrAttachment(netCourse.m_EndPosition.m_Flags, netCourse.m_EndPosition.m_Entity, netCourse.m_EndPosition.m_Position, netCourse.m_EndPosition.m_Elevation, prefab, isStandalone);
					}
				}
			}
		}

		private void CheckConnectedEdges(Entity entity, Entity deleteEdge)
		{
			if (m_EdgeData.HasComponent(entity))
			{
				Edge edge = m_EdgeData[entity];
				CheckSubObjects(edge.m_Start);
				CheckSubObjects(edge.m_End);
				CheckNodeEdges(edge.m_Start, edge.m_End);
				CheckNodeEdges(edge.m_End, edge.m_Start);
				DynamicBuffer<ConnectedNode> dynamicBuffer = m_ConnectedNodes[entity];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity node = dynamicBuffer[i].m_Node;
					CheckSubObjects(node);
					CheckNodeEdges(node);
				}
			}
			else
			{
				if (!m_NodeData.HasComponent(entity))
				{
					return;
				}
				DynamicBuffer<ConnectedEdge> dynamicBuffer2 = m_ConnectedEdges[entity];
				if (deleteEdge != Entity.Null && dynamicBuffer2.Length != 3)
				{
					deleteEdge = Entity.Null;
				}
				for (int j = 0; j < dynamicBuffer2.Length; j++)
				{
					Entity edge2 = dynamicBuffer2[j].m_Edge;
					Edge edge3 = m_EdgeData[edge2];
					if (edge3.m_Start == entity)
					{
						if (deleteEdge != Entity.Null)
						{
							CheckNodeEdges(edge3.m_End, entity);
						}
						CheckSubObjects(edge3.m_End);
						CheckSubObjects(edge2);
					}
					else if (edge3.m_End == entity)
					{
						if (deleteEdge != Entity.Null)
						{
							CheckNodeEdges(edge3.m_Start, entity);
						}
						CheckSubObjects(edge3.m_Start);
						CheckSubObjects(edge2);
					}
					else
					{
						CheckNodeEdges(edge3.m_Start, edge3.m_End);
						CheckNodeEdges(edge3.m_End, edge3.m_Start);
						CheckSubObjects(edge3.m_Start);
						CheckSubObjects(edge3.m_End);
						CheckSubObjects(edge2);
					}
					DynamicBuffer<ConnectedNode> dynamicBuffer3 = m_ConnectedNodes[edge2];
					for (int k = 0; k < dynamicBuffer3.Length; k++)
					{
						Entity node2 = dynamicBuffer3[k].m_Node;
						CheckSubObjects(node2);
						CheckNodeEdges(node2);
					}
				}
			}
		}

		private void CheckNodeEdges(Entity node, Entity otherNode)
		{
			DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[node];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity edge = dynamicBuffer[i].m_Edge;
				Edge edge2 = m_EdgeData[edge];
				if (edge2.m_Start == node)
				{
					if (edge2.m_End != otherNode)
					{
						CheckSubObjects(edge2.m_End);
						CheckSubObjects(edge);
					}
				}
				else if (edge2.m_End == node)
				{
					if (edge2.m_Start != otherNode)
					{
						CheckSubObjects(edge2.m_Start);
						CheckSubObjects(edge);
					}
				}
				else
				{
					CheckSubObjects(edge2.m_Start);
					CheckSubObjects(edge2.m_End);
					CheckSubObjects(edge);
				}
			}
		}

		private void CheckNodeEdges(Entity node)
		{
			DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[node];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity edge = dynamicBuffer[i].m_Edge;
				Edge edge2 = m_EdgeData[edge];
				if (edge2.m_Start == node)
				{
					CheckSubObjects(edge2.m_End);
					CheckSubObjects(edge);
				}
				else if (edge2.m_End == node)
				{
					CheckSubObjects(edge2.m_Start);
					CheckSubObjects(edge);
				}
			}
		}

		private void CheckSubObjects(Entity entity)
		{
			if (!m_SubObjects.HasBuffer(entity))
			{
				return;
			}
			DynamicBuffer<Game.Objects.SubObject> dynamicBuffer = m_SubObjects[entity];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity subObject = dynamicBuffer[i].m_SubObject;
				if (!m_AttachedData.HasComponent(subObject) || !(m_AttachedData[subObject].m_Parent == entity))
				{
					continue;
				}
				CreationDefinition creationDefinition = new CreationDefinition
				{
					m_Original = subObject
				};
				creationDefinition.m_Flags |= CreationFlags.Attach;
				if (m_OwnerData.HasComponent(subObject))
				{
					continue;
				}
				Transform transform = m_TransformData[subObject];
				ObjectDefinition objectDefinition = new ObjectDefinition
				{
					m_Position = transform.m_Position,
					m_Rotation = transform.m_Rotation
				};
				if (m_ElevationData.TryGetComponent(subObject, out var componentData))
				{
					objectDefinition.m_Elevation = componentData.m_Elevation;
					objectDefinition.m_ParentMesh = ObjectUtils.GetSubParentMesh(componentData.m_Flags);
					if ((componentData.m_Flags & ElevationFlags.Lowered) != 0)
					{
						creationDefinition.m_Flags |= CreationFlags.Lowered;
					}
				}
				else
				{
					objectDefinition.m_ParentMesh = -1;
				}
				m_CreationQueue.Enqueue(new CreationData(creationDefinition, default(OwnerDefinition), objectDefinition, hasDefinition: false));
			}
		}

		private void CheckEdgesForLocalConnectOrAttachment(CoursePosFlags flags, Entity ignoreEntity, float3 position, float2 elevation, Entity prefab, bool isStandalone)
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
					m_JobData = this
				};
				m_NetSearchTree.Iterate(ref iterator);
			}
		}

		private void CheckNodesForLocalConnect(Bezier4x3 curve, Entity prefab, bool isStandalone)
		{
			NetData netData = m_NetData[prefab];
			NetGeometryData netGeometryData = default(NetGeometryData);
			if (m_NetGeometryData.HasComponent(prefab))
			{
				netGeometryData = m_NetGeometryData[prefab];
			}
			float2 @float = netGeometryData.m_DefaultWidth * 0.5f;
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
				m_JobData = this
			};
			m_NetSearchTree.Iterate(ref iterator);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct CollectCreationDataJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		public NativeQueue<CreationData> m_CreationQueue;

		public NativeList<CreationData> m_CreationList;

		public NativeParallelMultiHashMap<OldObjectKey, OldObjectValue> m_OldObjectMap;

		public NativeHashMap<OwnerDefinition, Entity> m_ReusedOwnerMap;

		public void Execute()
		{
			m_CreationList.ResizeUninitialized(m_CreationQueue.Count);
			for (int i = 0; i < m_CreationList.Length; i++)
			{
				m_CreationList[i] = m_CreationQueue.Dequeue();
			}
			m_CreationList.Sort();
			CreationData value = default(CreationData);
			int num = 0;
			int num2 = 0;
			bool flag = false;
			while (num < m_CreationList.Length)
			{
				CreationData creationData = m_CreationList[num++];
				if (creationData.m_CreationDefinition.m_Original != value.m_CreationDefinition.m_Original || creationData.m_CreationDefinition.m_Original == Entity.Null)
				{
					if (flag)
					{
						m_CreationList[num2++] = value;
					}
					value = creationData;
					flag = true;
				}
				else if (creationData.m_HasDefinition)
				{
					value = creationData;
				}
			}
			if (flag)
			{
				m_CreationList[num2++] = value;
			}
			if (num2 < m_CreationList.Length)
			{
				m_CreationList.RemoveRange(num2, m_CreationList.Length - num2);
			}
			for (int j = 0; j < m_CreationList.Length; j++)
			{
				CreationData value2 = m_CreationList[j];
				if ((value2.m_CreationDefinition.m_Prefab == Entity.Null || (value2.m_CreationDefinition.m_Flags & CreationFlags.Upgrade) == 0) && m_PrefabRefData.HasComponent(value2.m_CreationDefinition.m_Original))
				{
					value2.m_CreationDefinition.m_Prefab = m_PrefabRefData[value2.m_CreationDefinition.m_Original].m_Prefab;
					m_CreationList[j] = value2;
				}
			}
			OldObjectKey key = default(OldObjectKey);
			OwnerDefinition key2 = default(OwnerDefinition);
			for (int k = 0; k < m_CreationList.Length; k++)
			{
				CreationData value3 = m_CreationList[k];
				if (!(value3.m_OwnerDefinition.m_Prefab == Entity.Null))
				{
					continue;
				}
				key.m_Prefab = value3.m_CreationDefinition.m_Prefab;
				key.m_SubPrefab = value3.m_CreationDefinition.m_SubPrefab;
				key.m_Original = value3.m_CreationDefinition.m_Original;
				key.m_Owner = value3.m_CreationDefinition.m_Owner;
				if (!m_OldObjectMap.TryGetFirstValue(key, out var item, out var it))
				{
					continue;
				}
				float num3 = float.MaxValue;
				Entity entity = item.m_Entity;
				NativeParallelMultiHashMapIterator<OldObjectKey> it2 = it;
				do
				{
					float num4 = math.distancesq(value3.m_ObjectDefinition.m_Position, item.m_Transform.m_Position);
					if (num4 < num3)
					{
						num3 = num4;
						entity = item.m_Entity;
						it2 = it;
					}
				}
				while (m_OldObjectMap.TryGetNextValue(out item, ref it));
				value3.m_OldEntity = entity;
				m_OldObjectMap.Remove(it2);
				m_CreationList[k] = value3;
				key2.m_Prefab = value3.m_CreationDefinition.m_Prefab;
				key2.m_Position = value3.m_ObjectDefinition.m_Position;
				key2.m_Rotation = value3.m_ObjectDefinition.m_Rotation;
				m_ReusedOwnerMap.TryAdd(key2, entity);
			}
			OldObjectKey key3 = default(OldObjectKey);
			OwnerDefinition key4 = default(OwnerDefinition);
			for (int l = 0; l < m_CreationList.Length; l++)
			{
				CreationData value4 = m_CreationList[l];
				if (!(value4.m_OwnerDefinition.m_Prefab != Entity.Null) || !m_ReusedOwnerMap.TryGetValue(value4.m_OwnerDefinition, out var item2))
				{
					continue;
				}
				key3.m_Prefab = value4.m_CreationDefinition.m_Prefab;
				key3.m_SubPrefab = value4.m_CreationDefinition.m_SubPrefab;
				key3.m_Original = value4.m_CreationDefinition.m_Original;
				key3.m_Owner = item2;
				if (!m_OldObjectMap.TryGetFirstValue(key3, out var item3, out var it3))
				{
					continue;
				}
				float num5 = float.MaxValue;
				Entity entity2 = item3.m_Entity;
				NativeParallelMultiHashMapIterator<OldObjectKey> it4 = it3;
				do
				{
					float num6 = math.distancesq(value4.m_ObjectDefinition.m_Position, item3.m_Transform.m_Position);
					if (num6 < num5)
					{
						num5 = num6;
						entity2 = item3.m_Entity;
						it4 = it3;
					}
				}
				while (m_OldObjectMap.TryGetNextValue(out item3, ref it3));
				value4.m_OldEntity = entity2;
				m_OldObjectMap.Remove(it4);
				m_CreationList[l] = value4;
				key4.m_Prefab = value4.m_CreationDefinition.m_Prefab;
				key4.m_Position = value4.m_ObjectDefinition.m_Position;
				key4.m_Rotation = value4.m_ObjectDefinition.m_Rotation;
				m_ReusedOwnerMap.TryAdd(key4, entity2);
			}
		}
	}

	[BurstCompile]
	private struct CreateObjectsJob : IJobParallelForDefer
	{
		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public ComponentTypeSet m_SubTypes;

		[ReadOnly]
		public ComponentTypeSet m_StoppedUpdateFrameTypes;

		[ReadOnly]
		public NativeArray<CreationData> m_CreationList;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		[ReadOnly]
		public ComponentLookup<Native> m_NativeData;

		[ReadOnly]
		public ComponentLookup<Stopped> m_StoppedData;

		[ReadOnly]
		public ComponentLookup<Attached> m_AttachedData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<Relative> m_RelativeData;

		[ReadOnly]
		public ComponentLookup<Recent> m_RecentData;

		[ReadOnly]
		public ComponentLookup<Tree> m_TreeData;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedData;

		[ReadOnly]
		public ComponentLookup<Damaged> m_DamagedData;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> m_PseudoRandomSeedData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Surface> m_SurfaceData;

		[ReadOnly]
		public ComponentLookup<Stack> m_StackData;

		[ReadOnly]
		public ComponentLookup<UnderConstruction> m_UnderConstructionData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectData;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PlaceableObjectData;

		[ReadOnly]
		public ComponentLookup<ServiceUpgradeData> m_ServiceUpgradeData;

		[ReadOnly]
		public ComponentLookup<ObjectData> m_ObjectData;

		[ReadOnly]
		public ComponentLookup<MovingObjectData> m_MovingObjectData;

		[ReadOnly]
		public ComponentLookup<TreeData> m_PrefabTreeData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<EffectData> m_PrefabEffectData;

		[ReadOnly]
		public ComponentLookup<StackData> m_PrefabStackData;

		[ReadOnly]
		public ComponentLookup<PrefabData> m_PrefabData;

		[ReadOnly]
		public ComponentLookup<Extension> m_BuildingExtensionData;

		[ReadOnly]
		public ComponentLookup<GoodsDeliveryVehicle> m_GoodsDeliveryVehicles;

		[ReadOnly]
		public EconomyParameterData m_EconomyParameterData;

		public void Execute(int index)
		{
			CreationData creationData = m_CreationList[index];
			CreateObject(index, creationData.m_CreationDefinition, creationData.m_OwnerDefinition, creationData.m_ObjectDefinition, creationData.m_OldEntity);
		}

		private void CreateObject(int jobIndex, CreationDefinition definitionData, OwnerDefinition ownerDefinition, ObjectDefinition objectDefinition, Entity oldEntity)
		{
			PrefabRef component = new PrefabRef
			{
				m_Prefab = definitionData.m_Prefab
			};
			if (definitionData.m_Original != Entity.Null)
			{
				m_CommandBuffer.AddComponent(jobIndex, definitionData.m_Original, default(Hidden));
				m_CommandBuffer.AddComponent(jobIndex, definitionData.m_Original, default(BatchesUpdated));
			}
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(jobIndex);
			ObjectGeometryData componentData;
			bool flag = m_PrefabObjectData.TryGetComponent(component.m_Prefab, out componentData);
			bool flag2 = (definitionData.m_Flags & CreationFlags.Permanent) != 0 || m_PrefabData.IsComponentEnabled(component.m_Prefab);
			Tree componentData2 = default(Tree);
			bool flag3 = flag2 && m_PrefabTreeData.HasComponent(component.m_Prefab);
			if (flag3 && !m_TreeData.TryGetComponent(definitionData.m_Original, out componentData2))
			{
				componentData2 = ObjectUtils.InitializeTreeState(objectDefinition.m_Age);
			}
			Temp component2 = new Temp
			{
				m_Original = definitionData.m_Original
			};
			component2.m_Flags |= TempFlags.Essential;
			ServiceUpgradeData componentData4;
			if (m_PlaceableObjectData.TryGetComponent(definitionData.m_Prefab, out var componentData3))
			{
				component2.m_Value = (int)componentData3.m_ConstructionCost;
				if (flag3)
				{
					component2.m_Value = ObjectUtils.GetContructionCost(component2.m_Value, componentData2, in m_EconomyParameterData);
				}
			}
			else if (m_ServiceUpgradeData.TryGetComponent(definitionData.m_Prefab, out componentData4))
			{
				component2.m_Value = (int)componentData4.m_UpgradeCost;
			}
			if ((definitionData.m_Flags & CreationFlags.Delete) != 0)
			{
				component2.m_Flags |= TempFlags.Delete;
				if (m_RecentData.TryGetComponent(definitionData.m_Original, out var componentData5))
				{
					component2.m_Cost = -ObjectUtils.GetRefundAmount(componentData5, m_SimulationFrame, m_EconomyParameterData);
				}
			}
			else if ((definitionData.m_Flags & CreationFlags.Select) != 0)
			{
				component2.m_Flags |= TempFlags.Select;
				if ((definitionData.m_Flags & CreationFlags.Dragging) != 0)
				{
					component2.m_Flags |= TempFlags.Dragging;
				}
			}
			else if (definitionData.m_Original != Entity.Null)
			{
				if ((definitionData.m_Flags & CreationFlags.Relocate) != 0)
				{
					component2.m_Flags |= TempFlags.Modify;
					if (!m_EditorMode && IsMoved(definitionData.m_Original, objectDefinition))
					{
						if (m_RecentData.TryGetComponent(definitionData.m_Original, out var componentData6))
						{
							component2.m_Cost = ObjectUtils.GetRelocationCost(component2.m_Value, componentData6, m_SimulationFrame, m_EconomyParameterData);
						}
						else
						{
							component2.m_Cost = ObjectUtils.GetRelocationCost(component2.m_Value, m_EconomyParameterData);
						}
					}
				}
				else
				{
					if ((definitionData.m_Flags & CreationFlags.Upgrade) != 0)
					{
						component2.m_Flags |= TempFlags.Upgrade;
						if (!m_EditorMode && m_PrefabRefData.TryGetComponent(definitionData.m_Original, out var componentData7) && componentData7.m_Prefab != definitionData.m_Prefab)
						{
							int num = 0;
							if (m_PlaceableObjectData.TryGetComponent(componentData7.m_Prefab, out var componentData8))
							{
								num = (int)componentData8.m_ConstructionCost;
								if (flag3)
								{
									num = ObjectUtils.GetContructionCost(num, componentData2, in m_EconomyParameterData);
								}
							}
							if (m_RecentData.TryGetComponent(definitionData.m_Original, out var componentData9))
							{
								component2.m_Cost = ObjectUtils.GetUpgradeCost(component2.m_Value, num, componentData9, m_SimulationFrame, m_EconomyParameterData);
							}
							else
							{
								component2.m_Cost = ObjectUtils.GetUpgradeCost(component2.m_Value, num);
							}
						}
					}
					else if ((definitionData.m_Flags & CreationFlags.Duplicate) != 0)
					{
						component2.m_Flags |= TempFlags.Duplicate;
					}
					if ((definitionData.m_Flags & CreationFlags.Repair) != 0 && !m_EditorMode && m_DestroyedData.HasComponent(definitionData.m_Original))
					{
						if (m_RecentData.TryGetComponent(definitionData.m_Original, out var componentData10))
						{
							component2.m_Cost = ObjectUtils.GetRebuildCost(component2.m_Value, componentData10, m_SimulationFrame, m_EconomyParameterData);
						}
						else
						{
							component2.m_Cost = ObjectUtils.GetRebuildCost(component2.m_Value);
						}
					}
					if ((definitionData.m_Flags & CreationFlags.Parent) != 0)
					{
						component2.m_Flags |= TempFlags.Parent;
					}
				}
			}
			else
			{
				component2.m_Flags |= TempFlags.Create;
				if ((definitionData.m_Flags & CreationFlags.Optional) != 0)
				{
					component2.m_Flags |= TempFlags.Optional;
				}
				if (!m_EditorMode)
				{
					component2.m_Cost = component2.m_Value;
				}
			}
			ElevationFlags elevationFlags = (ElevationFlags)0;
			if (math.abs(objectDefinition.m_ParentMesh) >= 1000)
			{
				elevationFlags |= ElevationFlags.Stacked;
			}
			if (objectDefinition.m_ParentMesh < 0)
			{
				elevationFlags |= ElevationFlags.OnGround;
			}
			if ((definitionData.m_Flags & CreationFlags.Lowered) != 0)
			{
				elevationFlags |= ElevationFlags.Lowered;
			}
			if (oldEntity != Entity.Null)
			{
				m_CommandBuffer.RemoveComponent<Deleted>(jobIndex, oldEntity);
				m_CommandBuffer.AddComponent(jobIndex, oldEntity, default(Updated));
				m_CommandBuffer.SetComponent(jobIndex, oldEntity, new Transform(objectDefinition.m_Position, objectDefinition.m_Rotation));
				if (ownerDefinition.m_Prefab == Entity.Null && definitionData.m_Owner == Entity.Null && (componentData.m_Flags & Game.Objects.GeometryFlags.Physical) != Game.Objects.GeometryFlags.None)
				{
					if (m_SurfaceData.HasComponent(definitionData.m_Original))
					{
						m_CommandBuffer.AddComponent(jobIndex, oldEntity, m_SurfaceData[definitionData.m_Original]);
					}
					else
					{
						m_CommandBuffer.AddComponent(jobIndex, oldEntity, default(Game.Objects.Surface));
					}
				}
				Attached componentData12;
				if ((definitionData.m_Flags & CreationFlags.Attach) != 0 || (componentData3.m_Flags & Game.Objects.PlacementFlags.Attached) != Game.Objects.PlacementFlags.None)
				{
					m_AttachedData.TryGetComponent(oldEntity, out var componentData11);
					m_CommandBuffer.AddComponent(jobIndex, oldEntity, CreateAttached(definitionData.m_Attached, componentData11.m_Parent, objectDefinition.m_Position));
				}
				else if (m_AttachedData.TryGetComponent(oldEntity, out componentData12))
				{
					componentData12.m_OldParent = componentData12.m_Parent;
					componentData12.m_Parent = oldEntity;
					m_CommandBuffer.SetComponent(jobIndex, oldEntity, componentData12);
				}
				if ((definitionData.m_Flags & CreationFlags.Permanent) == 0)
				{
					m_CommandBuffer.SetComponent(jobIndex, oldEntity, component2);
				}
				if (objectDefinition.m_Elevation != 0f || objectDefinition.m_ParentMesh != -1 || definitionData.m_SubPrefab != Entity.Null)
				{
					if (m_ElevationData.HasComponent(oldEntity))
					{
						m_CommandBuffer.SetComponent(jobIndex, oldEntity, new Game.Objects.Elevation(objectDefinition.m_Elevation, elevationFlags));
					}
					else
					{
						m_CommandBuffer.AddComponent(jobIndex, oldEntity, new Game.Objects.Elevation(objectDefinition.m_Elevation, elevationFlags));
					}
				}
				else
				{
					m_CommandBuffer.RemoveComponent<Game.Objects.Elevation>(jobIndex, oldEntity);
				}
				if (flag3)
				{
					m_CommandBuffer.SetComponent(jobIndex, oldEntity, componentData2);
				}
				if (m_EditorMode && (ownerDefinition.m_Prefab != Entity.Null || definitionData.m_Owner != Entity.Null))
				{
					m_CommandBuffer.SetComponent(jobIndex, oldEntity, new LocalTransformCache
					{
						m_Position = objectDefinition.m_LocalPosition,
						m_Rotation = objectDefinition.m_LocalRotation,
						m_ParentMesh = objectDefinition.m_ParentMesh,
						m_GroupIndex = objectDefinition.m_GroupIndex,
						m_Probability = objectDefinition.m_Probability,
						m_PrefabSubIndex = objectDefinition.m_PrefabSubIndex
					});
				}
				if (flag)
				{
					if (m_PseudoRandomSeedData.HasComponent(definitionData.m_Original))
					{
						m_CommandBuffer.SetComponent(jobIndex, oldEntity, m_PseudoRandomSeedData[definitionData.m_Original]);
					}
					else
					{
						m_CommandBuffer.SetComponent(jobIndex, oldEntity, new PseudoRandomSeed((ushort)definitionData.m_RandomSeed));
					}
				}
				if (definitionData.m_SubPrefab != Entity.Null)
				{
					m_CommandBuffer.SetComponent(jobIndex, oldEntity, new EditorContainer
					{
						m_Prefab = definitionData.m_SubPrefab,
						m_Scale = objectDefinition.m_Scale,
						m_Intensity = objectDefinition.m_Intensity,
						m_GroupIndex = objectDefinition.m_GroupIndex
					});
				}
				if (definitionData.m_Original != Entity.Null)
				{
					if (m_UnderConstructionData.TryGetComponent(definitionData.m_Original, out var componentData13))
					{
						m_CommandBuffer.AddComponent(jobIndex, oldEntity, componentData13);
					}
					else if (m_UnderConstructionData.HasComponent(oldEntity))
					{
						m_CommandBuffer.RemoveComponent<UnderConstruction>(jobIndex, oldEntity);
					}
					if (m_BuildingExtensionData.TryGetComponent(definitionData.m_Original, out var componentData14))
					{
						m_CommandBuffer.AddComponent(jobIndex, oldEntity, componentData14);
					}
					if (m_GoodsDeliveryVehicles.TryGetComponent(definitionData.m_Original, out var componentData15))
					{
						m_CommandBuffer.AddComponent(jobIndex, oldEntity, componentData15);
					}
				}
				return;
			}
			Entity e;
			if (m_StoppedData.HasComponent(definitionData.m_Original))
			{
				e = m_CommandBuffer.CreateEntity(jobIndex, m_MovingObjectData[component.m_Prefab].m_StoppedArchetype);
			}
			else if (m_EditorMode && m_MovingObjectData.HasComponent(component.m_Prefab))
			{
				e = m_CommandBuffer.CreateEntity(jobIndex, m_MovingObjectData[component.m_Prefab].m_StoppedArchetype);
				m_CommandBuffer.RemoveComponent(jobIndex, e, in m_StoppedUpdateFrameTypes);
				m_CommandBuffer.AddComponent(jobIndex, e, default(Static));
			}
			else
			{
				e = m_CommandBuffer.CreateEntity(jobIndex, m_ObjectData[component.m_Prefab].m_Archetype);
			}
			m_CommandBuffer.SetComponent(jobIndex, e, new Transform(objectDefinition.m_Position, objectDefinition.m_Rotation));
			m_CommandBuffer.SetComponent(jobIndex, e, component);
			if (ownerDefinition.m_Prefab != Entity.Null)
			{
				m_CommandBuffer.AddComponent(jobIndex, e, default(Owner));
				m_CommandBuffer.AddComponent(jobIndex, e, ownerDefinition);
			}
			else if (definitionData.m_Owner != Entity.Null)
			{
				m_CommandBuffer.AddComponent(jobIndex, e, new Owner(definitionData.m_Owner));
			}
			else if ((componentData.m_Flags & Game.Objects.GeometryFlags.Physical) != Game.Objects.GeometryFlags.None)
			{
				if (m_SurfaceData.HasComponent(definitionData.m_Original))
				{
					m_CommandBuffer.AddComponent(jobIndex, e, m_SurfaceData[definitionData.m_Original]);
				}
				else
				{
					m_CommandBuffer.AddComponent(jobIndex, e, default(Game.Objects.Surface));
				}
			}
			if ((definitionData.m_Flags & CreationFlags.Attach) != 0 || (componentData3.m_Flags & Game.Objects.PlacementFlags.Attached) != Game.Objects.PlacementFlags.None)
			{
				m_CommandBuffer.AddComponent(jobIndex, e, CreateAttached(definitionData.m_Attached, Entity.Null, objectDefinition.m_Position));
			}
			if ((definitionData.m_Flags & CreationFlags.Repair) == 0)
			{
				if (m_DamagedData.HasComponent(definitionData.m_Original))
				{
					m_CommandBuffer.AddComponent(jobIndex, e, m_DamagedData[definitionData.m_Original]);
				}
				if (m_DestroyedData.HasComponent(definitionData.m_Original))
				{
					m_CommandBuffer.AddComponent(jobIndex, e, m_DestroyedData[definitionData.m_Original]);
				}
			}
			if ((definitionData.m_Flags & CreationFlags.Permanent) == 0)
			{
				m_CommandBuffer.AddComponent(jobIndex, e, component2);
				if (flag)
				{
					m_CommandBuffer.AddComponent(jobIndex, e, default(Animation));
					m_CommandBuffer.AddComponent(jobIndex, e, default(InterpolatedTransform));
				}
				if (flag2 && m_PrefabBuildingData.TryGetComponent(component.m_Prefab, out var componentData16) && (componentData16.m_Flags & Game.Prefabs.BuildingFlags.BackAccess) != 0)
				{
					m_CommandBuffer.AddComponent(jobIndex, e, default(BackSide));
				}
			}
			if ((definitionData.m_Flags & CreationFlags.Native) != 0 || (m_NativeData.HasComponent(definitionData.m_Original) && (component2.m_Flags & (TempFlags.Modify | TempFlags.Upgrade)) == 0))
			{
				m_CommandBuffer.AddComponent(jobIndex, e, default(Native));
			}
			if (objectDefinition.m_Elevation != 0f || objectDefinition.m_ParentMesh != -1 || definitionData.m_SubPrefab != Entity.Null)
			{
				m_CommandBuffer.AddComponent(jobIndex, e, new Game.Objects.Elevation(objectDefinition.m_Elevation, elevationFlags));
			}
			if (flag3)
			{
				m_CommandBuffer.SetComponent(jobIndex, e, componentData2);
			}
			if (flag2 && m_PrefabStackData.TryGetComponent(component.m_Prefab, out var componentData17))
			{
				if (m_StackData.TryGetComponent(definitionData.m_Original, out var componentData18))
				{
					m_CommandBuffer.SetComponent(jobIndex, e, componentData18);
				}
				else
				{
					if (componentData17.m_Direction == StackDirection.Up)
					{
						componentData18.m_Range.min = componentData17.m_FirstBounds.min - objectDefinition.m_Elevation;
						componentData18.m_Range.max = componentData17.m_LastBounds.max;
					}
					else
					{
						componentData18.m_Range.min = componentData17.m_FirstBounds.min;
						componentData18.m_Range.max = componentData17.m_FirstBounds.max + MathUtils.Size(componentData17.m_MiddleBounds) * 2f + MathUtils.Size(componentData17.m_LastBounds);
					}
					m_CommandBuffer.SetComponent(jobIndex, e, componentData18);
				}
			}
			if (m_EditorMode)
			{
				bool flag4 = true;
				if (ownerDefinition.m_Prefab != Entity.Null || definitionData.m_Owner != Entity.Null)
				{
					m_CommandBuffer.AddComponent(jobIndex, e, new LocalTransformCache
					{
						m_Position = objectDefinition.m_LocalPosition,
						m_Rotation = objectDefinition.m_LocalRotation,
						m_ParentMesh = objectDefinition.m_ParentMesh,
						m_GroupIndex = objectDefinition.m_GroupIndex,
						m_Probability = objectDefinition.m_Probability,
						m_PrefabSubIndex = objectDefinition.m_PrefabSubIndex
					});
					flag4 = m_ServiceUpgradeData.HasComponent(definitionData.m_Prefab);
				}
				if (flag4)
				{
					m_CommandBuffer.RemoveComponent<EnabledEffect>(jobIndex, e);
					m_CommandBuffer.AddComponent(jobIndex, e, in m_SubTypes);
				}
			}
			if (flag)
			{
				if (m_PseudoRandomSeedData.HasComponent(definitionData.m_Original))
				{
					m_CommandBuffer.SetComponent(jobIndex, e, m_PseudoRandomSeedData[definitionData.m_Original]);
				}
				else
				{
					m_CommandBuffer.SetComponent(jobIndex, e, new PseudoRandomSeed((ushort)definitionData.m_RandomSeed));
				}
			}
			if (definitionData.m_SubPrefab != Entity.Null)
			{
				m_CommandBuffer.SetComponent(jobIndex, e, new EditorContainer
				{
					m_Prefab = definitionData.m_SubPrefab,
					m_Scale = objectDefinition.m_Scale,
					m_Intensity = objectDefinition.m_Intensity,
					m_GroupIndex = objectDefinition.m_GroupIndex
				});
				if (m_PrefabEffectData.HasComponent(definitionData.m_SubPrefab))
				{
					m_CommandBuffer.AddBuffer<EnabledEffect>(jobIndex, e);
				}
			}
			if (definitionData.m_Original != Entity.Null)
			{
				if (m_UnderConstructionData.TryGetComponent(definitionData.m_Original, out var componentData19))
				{
					m_CommandBuffer.AddComponent(jobIndex, e, componentData19);
				}
				if (m_RelativeData.TryGetComponent(definitionData.m_Original, out var componentData20))
				{
					m_CommandBuffer.AddComponent(jobIndex, e, componentData20);
				}
				if (m_BuildingExtensionData.TryGetComponent(definitionData.m_Original, out var componentData21))
				{
					m_CommandBuffer.AddComponent(jobIndex, e, componentData21);
				}
				if (m_GoodsDeliveryVehicles.TryGetComponent(definitionData.m_Original, out var componentData22))
				{
					m_CommandBuffer.AddComponent(jobIndex, e, componentData22);
				}
			}
			else if ((definitionData.m_Flags & CreationFlags.Construction) != 0)
			{
				m_CommandBuffer.AddComponent(jobIndex, e, new UnderConstruction
				{
					m_Speed = (byte)random.NextInt(39, 89)
				});
			}
		}

		private bool IsMoved(Entity original, ObjectDefinition objectDefinition)
		{
			if (m_TransformData.TryGetComponent(original, out var componentData))
			{
				Game.Objects.Elevation componentData2;
				bool test = objectDefinition.m_ParentMesh >= 0 || (m_ElevationData.TryGetComponent(original, out componentData2) && (componentData2.m_Flags & ElevationFlags.OnGround) == 0);
				float3 x = componentData.m_Position - objectDefinition.m_Position;
				x.y = math.select(0f, x.y, test);
				if (math.length(x) > 0.1f)
				{
					return true;
				}
				if (MathUtils.RotationAngle(componentData.m_Rotation, objectDefinition.m_Rotation) > 0.1f)
				{
					return true;
				}
			}
			return false;
		}

		private Attached CreateAttached(Entity parent, Entity oldParent, float3 position)
		{
			Attached result = new Attached(parent, oldParent, 0f);
			if (m_CurveData.HasComponent(parent))
			{
				MathUtils.Distance(m_CurveData[parent].m_Bezier, position, out result.m_CurvePosition);
			}
			return result;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> __Game_Tools_CreationDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<OwnerDefinition> __Game_Tools_OwnerDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ObjectDefinition> __Game_Tools_ObjectDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<NetCourse> __Game_Tools_NetCourse_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> __Game_Objects_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectData> __Game_Prefabs_ObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LocalConnectData> __Game_Prefabs_LocalConnectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RoadData> __Game_Prefabs_RoadData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LocalConnect> __Game_Net_LocalConnect_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Roundabout> __Game_Net_Roundabout_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedNode> __Game_Net_ConnectedNode_RO_BufferLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EditorContainer> __Game_Tools_EditorContainer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Native> __Game_Common_Native_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Stopped> __Game_Objects_Stopped_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Relative> __Game_Objects_Relative_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Recent> __Game_Tools_Recent_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Tree> __Game_Objects_Tree_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Damaged> __Game_Objects_Damaged_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Surface> __Game_Objects_Surface_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Stack> __Game_Objects_Stack_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UnderConstruction> __Game_Objects_UnderConstruction_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceUpgradeData> __Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MovingObjectData> __Game_Prefabs_MovingObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TreeData> __Game_Prefabs_TreeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EffectData> __Game_Prefabs_EffectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StackData> __Game_Prefabs_StackData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Extension> __Game_Buildings_Extension_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GoodsDeliveryVehicle> __Game_Vehicles_GoodsDeliveryVehicle_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Tools_CreationDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreationDefinition>(isReadOnly: true);
			__Game_Tools_OwnerDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<OwnerDefinition>(isReadOnly: true);
			__Game_Tools_ObjectDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ObjectDefinition>(isReadOnly: true);
			__Game_Tools_NetCourse_RO_ComponentTypeHandle = state.GetComponentTypeHandle<NetCourse>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Elevation>(isReadOnly: true);
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ObjectData_RO_ComponentLookup = state.GetComponentLookup<ObjectData>(isReadOnly: true);
			__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Prefabs_LocalConnectData_RO_ComponentLookup = state.GetComponentLookup<LocalConnectData>(isReadOnly: true);
			__Game_Prefabs_RoadData_RO_ComponentLookup = state.GetComponentLookup<RoadData>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Node>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_LocalConnect_RO_ComponentLookup = state.GetComponentLookup<LocalConnect>(isReadOnly: true);
			__Game_Net_Roundabout_RO_ComponentLookup = state.GetComponentLookup<Roundabout>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Net_ConnectedNode_RO_BufferLookup = state.GetBufferLookup<ConnectedNode>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Tools_EditorContainer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EditorContainer>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Common_Native_RO_ComponentLookup = state.GetComponentLookup<Native>(isReadOnly: true);
			__Game_Objects_Stopped_RO_ComponentLookup = state.GetComponentLookup<Stopped>(isReadOnly: true);
			__Game_Objects_Relative_RO_ComponentLookup = state.GetComponentLookup<Relative>(isReadOnly: true);
			__Game_Tools_Recent_RO_ComponentLookup = state.GetComponentLookup<Recent>(isReadOnly: true);
			__Game_Objects_Tree_RO_ComponentLookup = state.GetComponentLookup<Tree>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Objects_Damaged_RO_ComponentLookup = state.GetComponentLookup<Damaged>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentLookup = state.GetComponentLookup<PseudoRandomSeed>(isReadOnly: true);
			__Game_Objects_Surface_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Surface>(isReadOnly: true);
			__Game_Objects_Stack_RO_ComponentLookup = state.GetComponentLookup<Stack>(isReadOnly: true);
			__Game_Objects_UnderConstruction_RO_ComponentLookup = state.GetComponentLookup<UnderConstruction>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
			__Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup = state.GetComponentLookup<ServiceUpgradeData>(isReadOnly: true);
			__Game_Prefabs_MovingObjectData_RO_ComponentLookup = state.GetComponentLookup<MovingObjectData>(isReadOnly: true);
			__Game_Prefabs_TreeData_RO_ComponentLookup = state.GetComponentLookup<TreeData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_EffectData_RO_ComponentLookup = state.GetComponentLookup<EffectData>(isReadOnly: true);
			__Game_Prefabs_StackData_RO_ComponentLookup = state.GetComponentLookup<StackData>(isReadOnly: true);
			__Game_Prefabs_PrefabData_RO_ComponentLookup = state.GetComponentLookup<PrefabData>(isReadOnly: true);
			__Game_Buildings_Extension_RO_ComponentLookup = state.GetComponentLookup<Extension>(isReadOnly: true);
			__Game_Vehicles_GoodsDeliveryVehicle_RO_ComponentLookup = state.GetComponentLookup<GoodsDeliveryVehicle>(isReadOnly: true);
		}
	}

	private ToolSystem m_ToolSystem;

	private SimulationSystem m_SimulationSystem;

	private ModificationBarrier1 m_ModificationBarrier;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private EntityQuery m_DefinitionQuery;

	private EntityQuery m_DeletedQuery;

	private EntityQuery m_EconomyParameterQuery;

	private ComponentTypeSet m_SubTypes;

	private ComponentTypeSet m_StoppedUpdateFrameTypes;

	private NativeHashMap<OwnerDefinition, Entity> m_ReusedOwnerMap;

	private JobHandle m_OwnerMapReadDeps;

	private JobHandle m_OwnerMapWriteDeps;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier1>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_ReusedOwnerMap = new NativeHashMap<OwnerDefinition, Entity>(32, Allocator.Persistent);
		m_DefinitionQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<CreationDefinition>(),
				ComponentType.ReadOnly<Updated>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<ObjectDefinition>(),
				ComponentType.ReadOnly<NetCourse>()
			}
		});
		m_DeletedQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Objects.Object>(), ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<PrefabRef>());
		m_SubTypes = new ComponentTypeSet(ComponentType.ReadWrite<Game.Objects.SubObject>(), ComponentType.ReadWrite<Game.Net.SubNet>(), ComponentType.ReadWrite<Game.Areas.SubArea>());
		m_StoppedUpdateFrameTypes = new ComponentTypeSet(ComponentType.ReadWrite<Stopped>(), ComponentType.ReadWrite<ParkedCar>(), ComponentType.ReadWrite<ParkedTrain>(), ComponentType.ReadWrite<UpdateFrame>());
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		RequireForUpdate(m_DefinitionQuery);
		RequireForUpdate(m_EconomyParameterQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_OwnerMapReadDeps.Complete();
		m_OwnerMapWriteDeps.Complete();
		m_ReusedOwnerMap.Dispose();
		base.OnDestroy();
	}

	public NativeHashMap<OwnerDefinition, Entity> GetReusedOwnerMap(out JobHandle dependencies)
	{
		dependencies = m_OwnerMapWriteDeps;
		return m_ReusedOwnerMap;
	}

	public void AddOwnerMapReader(JobHandle dependencies)
	{
		m_OwnerMapReadDeps = JobHandle.CombineDependencies(m_OwnerMapReadDeps, dependencies);
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		m_OwnerMapReadDeps.Complete();
		m_OwnerMapWriteDeps.Complete();
		m_ReusedOwnerMap.Clear();
		base.OnStopRunning();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeQueue<CreationData> creationQueue = new NativeQueue<CreationData>(Allocator.TempJob);
		NativeList<CreationData> nativeList = new NativeList<CreationData>(Allocator.TempJob);
		NativeParallelMultiHashMap<OldObjectKey, OldObjectValue> oldObjectMap = new NativeParallelMultiHashMap<OldObjectKey, OldObjectValue>(32, Allocator.TempJob);
		m_OwnerMapReadDeps.Complete();
		m_OwnerMapWriteDeps.Complete();
		m_ReusedOwnerMap.Clear();
		JobHandle dependencies;
		FillCreationListJob jobData = new FillCreationListJob
		{
			m_CreationDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_CreationDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_OwnerDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ObjectDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_ObjectDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetCourseType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_NetCourse_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AttachedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabLocalConnectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LocalConnectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RoadData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RoadData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LocalConnectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LocalConnect_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RoundaboutData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Roundabout_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedNode_RO_BufferLookup, ref base.CheckedStateRef),
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
			m_CreationQueue = creationQueue.AsParallelWriter()
		};
		FillOldObjectsJob jobData2 = new FillOldObjectsJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EditorContainerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_EditorContainer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OldObjectMap = oldObjectMap
		};
		CollectCreationDataJob jobData3 = new CollectCreationDataJob
		{
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CreationQueue = creationQueue,
			m_CreationList = nativeList,
			m_OldObjectMap = oldObjectMap,
			m_ReusedOwnerMap = m_ReusedOwnerMap
		};
		CreateObjectsJob jobData4 = new CreateObjectsJob
		{
			m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_RandomSeed = RandomSeed.Next(),
			m_SubTypes = m_SubTypes,
			m_StoppedUpdateFrameTypes = m_StoppedUpdateFrameTypes,
			m_CreationList = nativeList.AsDeferredJobArray(),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_NativeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Native_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StoppedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Stopped_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttachedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RelativeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Relative_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RecentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Recent_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Tree_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DamagedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Damaged_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PseudoRandomSeedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SurfaceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Surface_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Stack_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UnderConstructionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_UnderConstruction_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlaceableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceUpgradeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MovingObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TreeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabEffectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_EffectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabStackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StackData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingExtensionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Extension_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GoodsDeliveryVehicles = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_GoodsDeliveryVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EconomyParameterData = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>()
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_DefinitionQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		JobHandle job = JobChunkExtensions.Schedule(jobData2, m_DeletedQuery, base.Dependency);
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData3, JobHandle.CombineDependencies(jobHandle, job));
		JobHandle jobHandle3 = jobData4.Schedule(nativeList, 1, jobHandle2);
		creationQueue.Dispose(jobHandle2);
		nativeList.Dispose(jobHandle3);
		oldObjectMap.Dispose(jobHandle2);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle3);
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
		m_OwnerMapWriteDeps = jobHandle2;
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
	public GenerateObjectsSystem()
	{
	}
}
