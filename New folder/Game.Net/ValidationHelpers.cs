using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Common;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Net;

public static class ValidationHelpers
{
	private struct LotData
	{
		public quaternion m_InverseRotation;

		public float3 m_Position;

		public float2 m_Limits;

		public bool m_IsValid;

		public bool m_IsCircular;

		public bool IsPointInside(float3 point)
		{
			if (!m_IsValid)
			{
				return false;
			}
			float3 @float = math.mul(m_InverseRotation, point - m_Position);
			if (m_IsCircular)
			{
				return math.length(@float.xz) <= math.csum(m_Limits) * 0.5f;
			}
			return math.all((@float.xz >= -m_Limits) & (@float.xz <= m_Limits));
		}
	}

	private struct NetIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
	{
		public Entity m_TopLevelEntity;

		public Entity m_EdgeEntity;

		public Entity m_OriginalEntity;

		public Bounds3 m_Bounds;

		public bool m_Essential;

		public bool m_EditorMode;

		public Edge m_Edge;

		public Edge m_OwnerEdge;

		public Edge m_OriginalNodes;

		public Edge m_NodeOwners;

		public NativeArray<ConnectedNode> m_ConnectedNodes;

		public NativeArray<ConnectedNode> m_OriginalConnectedNodes;

		public EdgeGeometry m_EdgeGeometryData;

		public StartNodeGeometry m_StartNodeGeometryData;

		public EndNodeGeometry m_EndNodeGeometryData;

		public NetCompositionData m_EdgeCompositionData;

		public NetCompositionData m_StartCompositionData;

		public NetCompositionData m_EndCompositionData;

		public CollisionMask m_EdgeCollisionMask;

		public CollisionMask m_StartCollisionMask;

		public CollisionMask m_EndCollisionMask;

		public CollisionMask m_CombinedCollisionMask;

		public ValidationSystem.EntityData m_Data;

		public NativeQueue<ErrorData>.ParallelWriter m_ErrorQueue;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			if ((m_CombinedCollisionMask & CollisionMask.OnGround) != 0)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds.xz);
			}
			return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
		}

		public void Iterate(QuadTreeBoundsXZ bounds, Entity edgeEntity2)
		{
			if ((m_CombinedCollisionMask & CollisionMask.OnGround) != 0)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds.xz))
				{
					return;
				}
			}
			else if (!MathUtils.Intersect(bounds.m_Bounds, m_Bounds))
			{
				return;
			}
			if (m_Data.m_Hidden.HasComponent(edgeEntity2) || !m_Data.m_Composition.HasComponent(edgeEntity2))
			{
				return;
			}
			Entity entity = edgeEntity2;
			bool hasOwner = false;
			if (m_Data.m_Owner.TryGetComponent(entity, out var componentData))
			{
				if (m_Data.m_Edge.TryGetComponent(componentData.m_Owner, out var componentData2) && (m_OriginalNodes.m_Start == componentData2.m_Start || m_OriginalNodes.m_Start == componentData2.m_End || m_OriginalNodes.m_End == componentData2.m_Start || m_OriginalNodes.m_End == componentData2.m_End))
				{
					return;
				}
				while (!m_Data.m_Building.HasComponent(entity))
				{
					hasOwner = true;
					if (m_Data.m_AssetStamp.HasComponent(componentData.m_Owner))
					{
						break;
					}
					entity = componentData.m_Owner;
					if (!m_Data.m_Owner.TryGetComponent(entity, out componentData))
					{
						break;
					}
				}
			}
			if (!(edgeEntity2 == m_NodeOwners.m_Start) && !(edgeEntity2 == m_NodeOwners.m_End))
			{
				Edge edgeData = m_Data.m_Edge[edgeEntity2];
				Composition compositionData = m_Data.m_Composition[edgeEntity2];
				EdgeGeometry edgeGeometryData = m_Data.m_EdgeGeometry[edgeEntity2];
				StartNodeGeometry startNodeGeometryData = m_Data.m_StartNodeGeometry[edgeEntity2];
				EndNodeGeometry endNodeGeometryData = m_Data.m_EndNodeGeometry[edgeEntity2];
				if (!(m_OwnerEdge.m_Start == edgeData.m_Start) && !(m_OwnerEdge.m_Start == edgeData.m_End) && !(m_OwnerEdge.m_End == edgeData.m_Start) && !(m_OwnerEdge.m_End == edgeData.m_End) && (!m_Data.m_Owner.TryGetComponent(edgeData.m_Start, out componentData) || !(componentData.m_Owner == m_OriginalEntity)) && (!m_Data.m_Owner.TryGetComponent(edgeData.m_End, out componentData) || !(componentData.m_Owner == m_OriginalEntity)))
				{
					CheckOverlap(entity, edgeEntity2, bounds.m_Bounds, edgeData, compositionData, edgeGeometryData, startNodeGeometryData, endNodeGeometryData, essential: false, hasOwner);
				}
			}
		}

		public void CheckOverlap(Entity topLevelEntity2, Entity edgeEntity2, Bounds3 bounds2, Edge edgeData2, Composition compositionData2, EdgeGeometry edgeGeometryData2, StartNodeGeometry startNodeGeometryData2, EndNodeGeometry endNodeGeometryData2, bool essential, bool hasOwner)
		{
			NetCompositionData netCompositionData = m_Data.m_PrefabComposition[compositionData2.m_Edge];
			NetCompositionData netCompositionData2 = m_Data.m_PrefabComposition[compositionData2.m_StartNode];
			NetCompositionData netCompositionData3 = m_Data.m_PrefabComposition[compositionData2.m_EndNode];
			CollisionMask collisionMask = NetUtils.GetCollisionMask(netCompositionData, !m_EditorMode || hasOwner);
			CollisionMask collisionMask2 = NetUtils.GetCollisionMask(netCompositionData2, !m_EditorMode || hasOwner);
			CollisionMask collisionMask3 = NetUtils.GetCollisionMask(netCompositionData3, !m_EditorMode || hasOwner);
			CollisionMask collisionMask4 = collisionMask | collisionMask2 | collisionMask3;
			if ((m_CombinedCollisionMask & collisionMask4) == 0 || ((m_CombinedCollisionMask & CollisionMask.OnGround) != 0 && !CommonUtils.ExclusiveGroundCollision(m_CombinedCollisionMask, collisionMask4) && !MathUtils.Intersect(bounds2, m_Bounds)))
			{
				return;
			}
			ErrorData value = default(ErrorData);
			Bounds3 intersection = default(Bounds3);
			intersection.min = float.MaxValue;
			intersection.max = float.MinValue;
			Bounds2 intersection2 = default(Bounds2);
			intersection2.min = float.MaxValue;
			intersection2.max = float.MinValue;
			Bounds1 bounds3 = default(Bounds1);
			bounds3.min = float.MaxValue;
			bounds3.max = float.MinValue;
			bool clearanceOnly = true;
			DynamicBuffer<ConnectedNode> dynamicBuffer = m_Data.m_ConnectedNodes[edgeEntity2];
			if ((m_EdgeCollisionMask & collisionMask) != 0)
			{
				if (CommonUtils.ExclusiveGroundCollision(m_EdgeCollisionMask, collisionMask))
				{
					if (ValidationHelpers.Intersect(m_OriginalNodes, edgeData2, m_EdgeGeometryData, edgeGeometryData2, m_EdgeCompositionData, netCompositionData, ref intersection2))
					{
						value.m_ErrorType = ErrorType.OverlapExisting;
						bounds3 |= MathUtils.Center(m_EdgeGeometryData.m_Bounds.y & edgeGeometryData2.m_Bounds.y);
					}
				}
				else
				{
					Check3DCollisionMasks(m_EdgeCollisionMask, collisionMask, m_EdgeCompositionData, netCompositionData, out var outData, out var outData2);
					if (ValidationHelpers.Intersect(m_OriginalNodes, edgeData2, m_EdgeGeometryData, edgeGeometryData2, outData, outData2, ref intersection, ref clearanceOnly))
					{
						value.m_ErrorType = ErrorType.OverlapExisting;
					}
				}
			}
			if ((m_EdgeCollisionMask & collisionMask2) != 0)
			{
				if (CommonUtils.ExclusiveGroundCollision(m_EdgeCollisionMask, collisionMask2))
				{
					if (ValidationHelpers.Intersect(m_Edge, m_OriginalNodes, m_ConnectedNodes, m_OriginalConnectedNodes, edgeData2.m_Start, m_EdgeGeometryData, startNodeGeometryData2.m_Geometry, m_EdgeCompositionData, netCompositionData2, ref intersection2))
					{
						if (!IgnoreCollision(edgeEntity2, edgeData2.m_Start, m_Edge))
						{
							value.m_ErrorType = ErrorType.OverlapExisting;
						}
						bounds3 |= MathUtils.Center(m_EdgeGeometryData.m_Bounds.y & startNodeGeometryData2.m_Geometry.m_Bounds.y);
					}
				}
				else
				{
					Check3DCollisionMasks(m_EdgeCollisionMask, collisionMask2, m_EdgeCompositionData, netCompositionData2, out var outData3, out var outData4);
					if (ValidationHelpers.Intersect(m_Edge, m_OriginalNodes, m_ConnectedNodes, m_OriginalConnectedNodes, edgeData2.m_Start, m_EdgeGeometryData, startNodeGeometryData2.m_Geometry, outData3, outData4, ref intersection, ref clearanceOnly) && !IgnoreCollision(edgeEntity2, edgeData2.m_Start, m_Edge))
					{
						value.m_ErrorType = ErrorType.OverlapExisting;
					}
				}
			}
			if ((m_EdgeCollisionMask & collisionMask3) != 0)
			{
				if (CommonUtils.ExclusiveGroundCollision(m_EdgeCollisionMask, collisionMask3))
				{
					if (ValidationHelpers.Intersect(m_Edge, m_OriginalNodes, m_ConnectedNodes, m_OriginalConnectedNodes, edgeData2.m_End, m_EdgeGeometryData, endNodeGeometryData2.m_Geometry, m_EdgeCompositionData, netCompositionData3, ref intersection2))
					{
						if (!IgnoreCollision(edgeEntity2, edgeData2.m_End, m_Edge))
						{
							value.m_ErrorType = ErrorType.OverlapExisting;
						}
						bounds3 |= MathUtils.Center(m_EdgeGeometryData.m_Bounds.y & endNodeGeometryData2.m_Geometry.m_Bounds.y);
					}
				}
				else
				{
					Check3DCollisionMasks(m_EdgeCollisionMask, collisionMask3, m_EdgeCompositionData, netCompositionData3, out var outData5, out var outData6);
					if (ValidationHelpers.Intersect(m_Edge, m_OriginalNodes, m_ConnectedNodes, m_OriginalConnectedNodes, edgeData2.m_End, m_EdgeGeometryData, endNodeGeometryData2.m_Geometry, outData5, outData6, ref intersection, ref clearanceOnly) && !IgnoreCollision(edgeEntity2, edgeData2.m_End, m_Edge))
					{
						value.m_ErrorType = ErrorType.OverlapExisting;
					}
				}
			}
			if ((collisionMask & m_StartCollisionMask) != 0)
			{
				if (CommonUtils.ExclusiveGroundCollision(collisionMask, m_StartCollisionMask))
				{
					if (ValidationHelpers.Intersect(edgeData2, dynamicBuffer.AsNativeArray(), m_Edge.m_Start, m_OriginalNodes.m_Start, edgeGeometryData2, m_StartNodeGeometryData.m_Geometry, netCompositionData, m_StartCompositionData, ref intersection2))
					{
						if (!IgnoreCollision(m_EdgeEntity, m_Edge.m_Start, edgeData2))
						{
							value.m_ErrorType = ErrorType.OverlapExisting;
						}
						bounds3 |= MathUtils.Center(edgeGeometryData2.m_Bounds.y & m_StartNodeGeometryData.m_Geometry.m_Bounds.y);
					}
				}
				else
				{
					Check3DCollisionMasks(collisionMask, m_StartCollisionMask, netCompositionData, m_StartCompositionData, out var outData7, out var outData8);
					if (ValidationHelpers.Intersect(edgeData2, dynamicBuffer.AsNativeArray(), m_Edge.m_Start, m_OriginalNodes.m_Start, edgeGeometryData2, m_StartNodeGeometryData.m_Geometry, outData7, outData8, ref intersection, ref clearanceOnly) && !IgnoreCollision(m_EdgeEntity, m_Edge.m_Start, edgeData2))
					{
						value.m_ErrorType = ErrorType.OverlapExisting;
					}
				}
			}
			if ((collisionMask & m_EndCollisionMask) != 0)
			{
				if (CommonUtils.ExclusiveGroundCollision(collisionMask, m_EndCollisionMask))
				{
					if (ValidationHelpers.Intersect(edgeData2, dynamicBuffer.AsNativeArray(), m_Edge.m_End, m_OriginalNodes.m_End, edgeGeometryData2, m_EndNodeGeometryData.m_Geometry, netCompositionData, m_EndCompositionData, ref intersection2))
					{
						if (!IgnoreCollision(m_EdgeEntity, m_Edge.m_End, edgeData2))
						{
							value.m_ErrorType = ErrorType.OverlapExisting;
						}
						bounds3 |= MathUtils.Center(edgeGeometryData2.m_Bounds.y & m_EndNodeGeometryData.m_Geometry.m_Bounds.y);
					}
				}
				else
				{
					Check3DCollisionMasks(collisionMask, m_EndCollisionMask, netCompositionData, m_EndCompositionData, out var outData9, out var outData10);
					if (ValidationHelpers.Intersect(edgeData2, dynamicBuffer.AsNativeArray(), m_Edge.m_End, m_OriginalNodes.m_End, edgeGeometryData2, m_EndNodeGeometryData.m_Geometry, outData9, outData10, ref intersection, ref clearanceOnly) && !IgnoreCollision(m_EdgeEntity, m_Edge.m_End, edgeData2))
					{
						value.m_ErrorType = ErrorType.OverlapExisting;
					}
				}
			}
			if ((m_StartCollisionMask & collisionMask2) != 0)
			{
				if (CommonUtils.ExclusiveGroundCollision(m_StartCollisionMask, collisionMask2))
				{
					if (ValidationHelpers.Intersect(m_Edge.m_Start, m_OriginalNodes.m_Start, m_ConnectedNodes, m_OriginalConnectedNodes, edgeData2.m_Start, dynamicBuffer.AsNativeArray(), m_StartNodeGeometryData.m_Geometry, startNodeGeometryData2.m_Geometry, m_StartCompositionData, netCompositionData2, ref intersection2))
					{
						if (!IgnoreCollision(m_EdgeEntity, m_Edge.m_Start, edgeEntity2, edgeData2.m_Start))
						{
							value.m_ErrorType = ErrorType.OverlapExisting;
						}
						bounds3 |= MathUtils.Center(m_StartNodeGeometryData.m_Geometry.m_Bounds.y & startNodeGeometryData2.m_Geometry.m_Bounds.y);
					}
				}
				else
				{
					Check3DCollisionMasks(m_StartCollisionMask, collisionMask2, m_StartCompositionData, netCompositionData2, out var outData11, out var outData12);
					if (ValidationHelpers.Intersect(m_Edge.m_Start, m_OriginalNodes.m_Start, m_ConnectedNodes, m_OriginalConnectedNodes, edgeData2.m_Start, dynamicBuffer.AsNativeArray(), m_StartNodeGeometryData.m_Geometry, startNodeGeometryData2.m_Geometry, outData11, outData12, ref intersection, ref clearanceOnly) && !IgnoreCollision(m_EdgeEntity, m_Edge.m_Start, edgeEntity2, edgeData2.m_Start))
					{
						value.m_ErrorType = ErrorType.OverlapExisting;
					}
				}
			}
			if ((m_StartCollisionMask & collisionMask3) != 0)
			{
				if (CommonUtils.ExclusiveGroundCollision(m_StartCollisionMask, collisionMask3))
				{
					if (ValidationHelpers.Intersect(m_Edge.m_Start, m_OriginalNodes.m_Start, m_ConnectedNodes, m_OriginalConnectedNodes, edgeData2.m_End, dynamicBuffer.AsNativeArray(), m_StartNodeGeometryData.m_Geometry, endNodeGeometryData2.m_Geometry, m_StartCompositionData, netCompositionData3, ref intersection2))
					{
						if (!IgnoreCollision(m_EdgeEntity, m_Edge.m_Start, edgeEntity2, edgeData2.m_End))
						{
							value.m_ErrorType = ErrorType.OverlapExisting;
						}
						bounds3 |= MathUtils.Center(m_StartNodeGeometryData.m_Geometry.m_Bounds.y & endNodeGeometryData2.m_Geometry.m_Bounds.y);
					}
				}
				else
				{
					Check3DCollisionMasks(m_StartCollisionMask, collisionMask3, m_StartCompositionData, netCompositionData3, out var outData13, out var outData14);
					if (ValidationHelpers.Intersect(m_Edge.m_Start, m_OriginalNodes.m_Start, m_ConnectedNodes, m_OriginalConnectedNodes, edgeData2.m_End, dynamicBuffer.AsNativeArray(), m_StartNodeGeometryData.m_Geometry, endNodeGeometryData2.m_Geometry, outData13, outData14, ref intersection, ref clearanceOnly) && !IgnoreCollision(m_EdgeEntity, m_Edge.m_Start, edgeEntity2, edgeData2.m_End))
					{
						value.m_ErrorType = ErrorType.OverlapExisting;
					}
				}
			}
			if ((m_EndCollisionMask & collisionMask2) != 0)
			{
				if (CommonUtils.ExclusiveGroundCollision(m_EndCollisionMask, collisionMask2))
				{
					if (ValidationHelpers.Intersect(m_Edge.m_End, m_OriginalNodes.m_End, m_ConnectedNodes, m_OriginalConnectedNodes, edgeData2.m_Start, dynamicBuffer.AsNativeArray(), m_EndNodeGeometryData.m_Geometry, startNodeGeometryData2.m_Geometry, m_EndCompositionData, netCompositionData2, ref intersection2))
					{
						if (!IgnoreCollision(m_EdgeEntity, m_Edge.m_End, edgeEntity2, edgeData2.m_Start))
						{
							value.m_ErrorType = ErrorType.OverlapExisting;
						}
						bounds3 |= MathUtils.Center(m_EndNodeGeometryData.m_Geometry.m_Bounds.y & startNodeGeometryData2.m_Geometry.m_Bounds.y);
					}
				}
				else
				{
					Check3DCollisionMasks(m_EndCollisionMask, collisionMask2, m_EndCompositionData, netCompositionData2, out var outData15, out var outData16);
					if (ValidationHelpers.Intersect(m_Edge.m_End, m_OriginalNodes.m_End, m_ConnectedNodes, m_OriginalConnectedNodes, edgeData2.m_Start, dynamicBuffer.AsNativeArray(), m_EndNodeGeometryData.m_Geometry, startNodeGeometryData2.m_Geometry, outData15, outData16, ref intersection, ref clearanceOnly) && !IgnoreCollision(m_EdgeEntity, m_Edge.m_End, edgeEntity2, edgeData2.m_Start))
					{
						value.m_ErrorType = ErrorType.OverlapExisting;
					}
				}
			}
			if ((m_EndCollisionMask & collisionMask3) != 0)
			{
				if (CommonUtils.ExclusiveGroundCollision(m_EndCollisionMask, collisionMask3))
				{
					if (ValidationHelpers.Intersect(m_Edge.m_End, m_OriginalNodes.m_End, m_ConnectedNodes, m_OriginalConnectedNodes, edgeData2.m_End, dynamicBuffer.AsNativeArray(), m_EndNodeGeometryData.m_Geometry, endNodeGeometryData2.m_Geometry, m_EndCompositionData, netCompositionData3, ref intersection2))
					{
						if (!IgnoreCollision(m_EdgeEntity, m_Edge.m_End, edgeEntity2, edgeData2.m_End))
						{
							value.m_ErrorType = ErrorType.OverlapExisting;
						}
						bounds3 |= MathUtils.Center(m_EndNodeGeometryData.m_Geometry.m_Bounds.y & endNodeGeometryData2.m_Geometry.m_Bounds.y);
					}
				}
				else
				{
					Check3DCollisionMasks(m_EndCollisionMask, collisionMask3, m_EndCompositionData, netCompositionData3, out var outData17, out var outData18);
					if (ValidationHelpers.Intersect(m_Edge.m_End, m_OriginalNodes.m_End, m_ConnectedNodes, m_OriginalConnectedNodes, edgeData2.m_End, dynamicBuffer.AsNativeArray(), m_EndNodeGeometryData.m_Geometry, endNodeGeometryData2.m_Geometry, outData17, outData18, ref intersection, ref clearanceOnly) && !IgnoreCollision(m_EdgeEntity, m_Edge.m_End, edgeEntity2, edgeData2.m_End))
					{
						value.m_ErrorType = ErrorType.OverlapExisting;
					}
				}
			}
			if (value.m_ErrorType == ErrorType.None)
			{
				return;
			}
			intersection.xz |= intersection2;
			intersection.y |= bounds3;
			value.m_ErrorSeverity = ErrorSeverity.Error;
			value.m_TempEntity = m_EdgeEntity;
			value.m_PermanentEntity = edgeEntity2;
			value.m_Position = MathUtils.Center(intersection);
			if (!essential && topLevelEntity2 != edgeEntity2 && topLevelEntity2 != Entity.Null)
			{
				PrefabRef prefabRef = m_Data.m_PrefabRef[topLevelEntity2];
				if (m_Data.m_PrefabObjectGeometry.HasComponent(prefabRef.m_Prefab) && (m_Data.m_PrefabObjectGeometry[prefabRef.m_Prefab].m_Flags & (Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.DeleteOverridden)) == (Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.DeleteOverridden) && !m_Data.m_Attached.HasComponent(topLevelEntity2) && (!m_Data.m_Temp.HasComponent(topLevelEntity2) || (m_Data.m_Temp[topLevelEntity2].m_Flags & TempFlags.Essential) == 0))
				{
					value.m_ErrorSeverity = ErrorSeverity.Warning;
					value.m_PermanentEntity = topLevelEntity2;
				}
			}
			if (!m_Essential && m_TopLevelEntity != m_EdgeEntity && m_TopLevelEntity != Entity.Null)
			{
				PrefabRef prefabRef2 = m_Data.m_PrefabRef[m_TopLevelEntity];
				if (m_Data.m_PrefabObjectGeometry.HasComponent(prefabRef2.m_Prefab) && (m_Data.m_PrefabObjectGeometry[prefabRef2.m_Prefab].m_Flags & (Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.DeleteOverridden)) == (Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.DeleteOverridden) && !m_Data.m_Attached.HasComponent(m_TopLevelEntity) && (!m_Data.m_Temp.HasComponent(m_TopLevelEntity) || (m_Data.m_Temp[m_TopLevelEntity].m_Flags & TempFlags.Essential) == 0))
				{
					value.m_ErrorSeverity = ErrorSeverity.Warning;
					value.m_TempEntity = edgeEntity2;
					value.m_PermanentEntity = m_TopLevelEntity;
				}
			}
			if (clearanceOnly && bounds3.min > bounds3.max)
			{
				value.m_ErrorType = ErrorType.NotEnoughClearance;
			}
			m_ErrorQueue.Enqueue(value);
		}

		private bool IgnoreCollision(Entity edge1, Entity node1, Edge edge2)
		{
			EdgeIterator edgeIterator = new EdgeIterator(edge1, node1, m_Data.m_ConnectedEdges, m_Data.m_Edge, m_Data.m_Temp, m_Data.m_Hidden, includeMiddleConnections: true);
			EdgeIteratorValue value;
			while (edgeIterator.GetNext(out value))
			{
				if (!value.m_Middle)
				{
					continue;
				}
				Edge edge3 = m_Data.m_Edge[value.m_Edge];
				if (edge3.m_Start == edge2.m_Start || edge3.m_End == edge2.m_Start || edge3.m_Start == edge2.m_End || edge3.m_End == edge2.m_End)
				{
					return true;
				}
				if (m_Data.m_Temp.TryGetComponent(edge3.m_Start, out var componentData) && m_Data.m_Temp.TryGetComponent(edge3.m_End, out var componentData2))
				{
					if (componentData.m_Original == edge2.m_Start || componentData2.m_Original == edge2.m_Start || componentData.m_Original == edge2.m_End || componentData2.m_Original == edge2.m_End)
					{
						return true;
					}
				}
				else if (m_Data.m_Temp.TryGetComponent(edge2.m_Start, out componentData) && m_Data.m_Temp.TryGetComponent(edge2.m_End, out componentData2) && (componentData.m_Original == edge3.m_Start || componentData2.m_Original == edge3.m_Start || componentData.m_Original == edge3.m_End || componentData2.m_Original == edge3.m_End))
				{
					return true;
				}
			}
			return false;
		}

		private bool IgnoreCollision(Entity edge1, Entity node1, Entity edge2, Entity node2)
		{
			EdgeIterator edgeIterator = new EdgeIterator(edge1, node1, m_Data.m_ConnectedEdges, m_Data.m_Edge, m_Data.m_Temp, m_Data.m_Hidden, includeMiddleConnections: true);
			EdgeIteratorValue value;
			while (edgeIterator.GetNext(out value))
			{
				if (!value.m_Middle)
				{
					continue;
				}
				Edge edge3 = m_Data.m_Edge[value.m_Edge];
				if (edge3.m_Start == node2 || edge3.m_End == node2)
				{
					return true;
				}
				if (m_Data.m_Temp.TryGetComponent(node2, out var componentData))
				{
					if (edge3.m_Start == componentData.m_Original || edge3.m_End == componentData.m_Original)
					{
						return true;
					}
					continue;
				}
				if (m_Data.m_Temp.TryGetComponent(edge3.m_Start, out componentData) && componentData.m_Original == node2)
				{
					return true;
				}
				if (m_Data.m_Temp.TryGetComponent(edge3.m_End, out componentData) && componentData.m_Original == node2)
				{
					return true;
				}
			}
			edgeIterator = new EdgeIterator(edge2, node2, m_Data.m_ConnectedEdges, m_Data.m_Edge, m_Data.m_Temp, m_Data.m_Hidden, includeMiddleConnections: true);
			EdgeIteratorValue value2;
			while (edgeIterator.GetNext(out value2))
			{
				if (!value2.m_Middle)
				{
					continue;
				}
				Edge edge4 = m_Data.m_Edge[value2.m_Edge];
				if (edge4.m_Start == node1 || edge4.m_End == node1)
				{
					return true;
				}
				if (m_Data.m_Temp.TryGetComponent(node1, out var componentData2))
				{
					if (edge4.m_Start == componentData2.m_Original || edge4.m_End == componentData2.m_Original)
					{
						return true;
					}
					continue;
				}
				if (m_Data.m_Temp.TryGetComponent(edge4.m_Start, out componentData2) && componentData2.m_Original == node1)
				{
					return true;
				}
				if (m_Data.m_Temp.TryGetComponent(edge4.m_End, out componentData2) && componentData2.m_Original == node1)
				{
					return true;
				}
			}
			return false;
		}
	}

	private struct ObjectIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
	{
		public Entity m_EdgeEntity;

		public Entity m_TopLevelEntity;

		public Entity m_AssetStampEntity;

		public Bounds3 m_Bounds;

		public Edge m_OriginalNodes;

		public Edge m_NodeOwners;

		public Edge m_NodeAssetStamps;

		public Edge m_NodeEdges;

		public Edge m_OwnerEdge;

		public EdgeGeometry m_EdgeGeometryData;

		public StartNodeGeometry m_StartNodeGeometryData;

		public EndNodeGeometry m_EndNodeGeometryData;

		public NetCompositionData m_EdgeCompositionData;

		public NetCompositionData m_StartCompositionData;

		public NetCompositionData m_EndCompositionData;

		public CollisionMask m_EdgeCollisionMask;

		public CollisionMask m_StartCollisionMask;

		public CollisionMask m_EndCollisionMask;

		public CollisionMask m_CombinedCollisionMask;

		public DynamicBuffer<NetCompositionArea> m_EdgeCompositionAreas;

		public DynamicBuffer<NetCompositionArea> m_StartCompositionAreas;

		public DynamicBuffer<NetCompositionArea> m_EndCompositionAreas;

		public ValidationSystem.EntityData m_Data;

		public NativeQueue<ErrorData>.ParallelWriter m_ErrorQueue;

		public bool m_EditorMode;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			if ((bounds.m_Mask & BoundsMask.NotOverridden) == 0)
			{
				return false;
			}
			if ((m_CombinedCollisionMask & CollisionMask.OnGround) != 0)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds.xz);
			}
			return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
		}

		public void Iterate(QuadTreeBoundsXZ bounds, Entity objectEntity2)
		{
			if ((bounds.m_Mask & BoundsMask.NotOverridden) == 0)
			{
				return;
			}
			if ((m_CombinedCollisionMask & CollisionMask.OnGround) != 0)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds.xz))
				{
					return;
				}
			}
			else if (!MathUtils.Intersect(bounds.m_Bounds, m_Bounds))
			{
				return;
			}
			if (m_Data.m_Hidden.HasComponent(objectEntity2) || m_AssetStampEntity == objectEntity2 || m_NodeAssetStamps.m_Start == objectEntity2 || m_NodeAssetStamps.m_End == objectEntity2)
			{
				return;
			}
			bool flag = true;
			bool flag2 = true;
			Entity entity = objectEntity2;
			bool hasOwner = false;
			Owner componentData;
			while (m_Data.m_Owner.TryGetComponent(entity, out componentData) && !m_Data.m_Building.HasComponent(entity))
			{
				Entity owner = componentData.m_Owner;
				hasOwner = true;
				if (m_Data.m_AssetStamp.HasComponent(owner))
				{
					break;
				}
				entity = owner;
				if (m_Data.m_Edge.TryGetComponent(entity, out var componentData2))
				{
					flag &= componentData2.m_Start != m_OriginalNodes.m_Start && componentData2.m_End != m_OriginalNodes.m_Start;
					flag2 &= componentData2.m_Start != m_OriginalNodes.m_End && componentData2.m_End != m_OriginalNodes.m_End;
					if (m_NodeEdges.m_Start == entity || m_NodeEdges.m_End == entity)
					{
						return;
					}
				}
				else if (m_Data.m_Node.HasComponent(entity))
				{
					if (m_NodeEdges.m_Start != Entity.Null)
					{
						Edge edge = m_Data.m_Edge[m_NodeEdges.m_Start];
						if (edge.m_Start == entity || edge.m_End == entity)
						{
							return;
						}
					}
					if (m_NodeEdges.m_End != Entity.Null)
					{
						Edge edge2 = m_Data.m_Edge[m_NodeEdges.m_End];
						if (edge2.m_Start == entity || edge2.m_End == entity)
						{
							return;
						}
					}
				}
				if (owner == m_OwnerEdge.m_Start || owner == m_OwnerEdge.m_End)
				{
					return;
				}
			}
			if (m_TopLevelEntity == entity)
			{
				return;
			}
			flag &= m_NodeOwners.m_Start != entity;
			flag2 &= m_NodeOwners.m_End != entity;
			if (m_Data.m_Attached.TryGetComponent(objectEntity2, out var componentData3))
			{
				if (componentData3.m_Parent == m_OriginalNodes.m_Start)
				{
					flag &= (m_StartCompositionData.m_Flags.m_General & CompositionFlags.General.Roundabout) == 0;
				}
				if (componentData3.m_Parent == m_OriginalNodes.m_End)
				{
					flag2 &= (m_EndCompositionData.m_Flags.m_General & CompositionFlags.General.Roundabout) == 0;
				}
			}
			PrefabRef prefabRef = m_Data.m_PrefabRef[objectEntity2];
			Transform transform = m_Data.m_Transform[objectEntity2];
			CheckOverlap(objectEntity2, entity, bounds.m_Bounds, prefabRef, transform, flag, flag2, hasOwner);
		}

		public void CheckOverlap(Entity objectEntity2, Entity topLevelEntity2, Bounds3 bounds2, PrefabRef prefabRef2, Transform transform2, bool checkStart, bool checkEnd, bool hasOwner)
		{
			if (!m_Data.m_PrefabObjectGeometry.HasComponent(prefabRef2.m_Prefab))
			{
				return;
			}
			ObjectGeometryData objectGeometryData = m_Data.m_PrefabObjectGeometry[prefabRef2.m_Prefab];
			if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.IgnoreSecondaryCollision) != Game.Objects.GeometryFlags.None && m_Data.m_Secondary.HasComponent(objectEntity2))
			{
				return;
			}
			CollisionMask collisionMask = ((!m_Data.m_ObjectElevation.HasComponent(objectEntity2)) ? ObjectUtils.GetCollisionMask(objectGeometryData, !m_EditorMode || hasOwner) : ObjectUtils.GetCollisionMask(objectGeometryData, m_Data.m_ObjectElevation[objectEntity2], !m_EditorMode || hasOwner));
			if ((m_CombinedCollisionMask & collisionMask) == 0)
			{
				return;
			}
			ErrorData error = new ErrorData
			{
				m_ErrorSeverity = ErrorSeverity.Error,
				m_TempEntity = m_EdgeEntity,
				m_PermanentEntity = objectEntity2
			};
			if (topLevelEntity2 != objectEntity2)
			{
				if ((objectGeometryData.m_Flags & (Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.DeleteOverridden)) == Game.Objects.GeometryFlags.Overridable)
				{
					error.m_ErrorSeverity = ErrorSeverity.Override;
				}
				else
				{
					PrefabRef prefabRef3 = m_Data.m_PrefabRef[topLevelEntity2];
					if (m_Data.m_PrefabObjectGeometry.HasComponent(prefabRef3.m_Prefab) && (m_Data.m_PrefabObjectGeometry[prefabRef3.m_Prefab].m_Flags & (Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.DeleteOverridden)) == (Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.DeleteOverridden) && !m_Data.m_Attached.HasComponent(topLevelEntity2))
					{
						error.m_ErrorSeverity = ErrorSeverity.Warning;
						error.m_PermanentEntity = topLevelEntity2;
					}
				}
			}
			else if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Overridable) != Game.Objects.GeometryFlags.None)
			{
				if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.DeleteOverridden) != Game.Objects.GeometryFlags.None)
				{
					if (!m_Data.m_Attached.HasComponent(objectEntity2))
					{
						error.m_ErrorSeverity = ErrorSeverity.Warning;
					}
				}
				else
				{
					error.m_ErrorSeverity = ErrorSeverity.Override;
				}
			}
			float3 origin = MathUtils.Center(bounds2);
			StackData componentData = default(StackData);
			if (m_Data.m_Stack.TryGetComponent(objectEntity2, out var componentData2))
			{
				m_Data.m_PrefabStackData.TryGetComponent(prefabRef2.m_Prefab, out componentData);
			}
			bool flag = (objectGeometryData.m_Flags & (Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.DeleteOverridden)) != Game.Objects.GeometryFlags.Overridable;
			if (flag && m_Data.m_PrefabBuilding.TryGetComponent(prefabRef2.m_Prefab, out var componentData3))
			{
				flag = (componentData3.m_Flags & BuildingFlags.CanBeOnRoadArea) != 0;
			}
			if ((m_CombinedCollisionMask & CollisionMask.OnGround) == 0 || MathUtils.Intersect(bounds2, m_Bounds))
			{
				CheckOverlap3D(ref error, collisionMask, objectGeometryData, componentData, bounds2, transform2, componentData2, topLevelEntity2, origin, checkStart, checkEnd, flag);
			}
			if (error.m_ErrorType == ErrorType.None && CommonUtils.ExclusiveGroundCollision(m_CombinedCollisionMask, collisionMask))
			{
				CheckOverlap2D(ref error, collisionMask, objectGeometryData, bounds2, transform2, topLevelEntity2, origin, checkStart, checkEnd, flag);
			}
			if (error.m_ErrorType != ErrorType.None)
			{
				if ((error.m_ErrorSeverity == ErrorSeverity.Override || error.m_ErrorSeverity == ErrorSeverity.Warning) && error.m_ErrorType == ErrorType.OverlapExisting && m_Data.m_OnFire.HasComponent(error.m_PermanentEntity))
				{
					error.m_ErrorType = ErrorType.OnFire;
					error.m_ErrorSeverity = ErrorSeverity.Error;
				}
				m_ErrorQueue.Enqueue(error);
			}
		}

		private void CheckOverlap3D(ref ErrorData error, CollisionMask collisionMask2, ObjectGeometryData prefabObjectGeometryData2, StackData stackData2, Bounds3 bounds2, Transform transform2, Stack stack2, Entity topLevelEntity2, float3 origin, bool checkStart, bool checkEnd, bool canBeOnRoadArea)
		{
			Bounds3 intersection = default(Bounds3);
			intersection.min = float.MaxValue;
			intersection.max = float.MinValue;
			float3 @float = math.mul(math.inverse(transform2.m_Rotation), transform2.m_Position - origin);
			Bounds3 bounds3 = ObjectUtils.GetBounds(stack2, prefabObjectGeometryData2, stackData2);
			DynamicBuffer<NetCompositionArea> areas = m_EdgeCompositionAreas;
			DynamicBuffer<NetCompositionArea> areas2 = m_StartCompositionAreas;
			DynamicBuffer<NetCompositionArea> areas3 = m_EndCompositionAreas;
			if (!canBeOnRoadArea)
			{
				areas = default(DynamicBuffer<NetCompositionArea>);
				areas2 = default(DynamicBuffer<NetCompositionArea>);
				areas3 = default(DynamicBuffer<NetCompositionArea>);
			}
			if ((prefabObjectGeometryData2.m_Flags & Game.Objects.GeometryFlags.IgnoreBottomCollision) != Game.Objects.GeometryFlags.None)
			{
				bounds3.min.y = math.max(bounds3.min.y, 0f);
			}
			Check3DCollisionMasks(m_EdgeCollisionMask, collisionMask2, m_EdgeCompositionData, out var outData);
			Check3DCollisionMasks(m_StartCollisionMask, collisionMask2, m_StartCompositionData, out var outData2);
			Check3DCollisionMasks(m_EndCollisionMask, collisionMask2, m_EndCompositionData, out var outData3);
			if (ObjectUtils.GetStandingLegCount(prefabObjectGeometryData2, out var legCount))
			{
				for (int i = 0; i < legCount; i++)
				{
					float3 float2 = @float + ObjectUtils.GetStandingLegOffset(prefabObjectGeometryData2, i);
					if ((prefabObjectGeometryData2.m_Flags & (Game.Objects.GeometryFlags.CircularLeg | Game.Objects.GeometryFlags.IgnoreLegCollision)) == Game.Objects.GeometryFlags.CircularLeg)
					{
						Cylinder3 cylinder = new Cylinder3
						{
							circle = new Circle2(prefabObjectGeometryData2.m_LegSize.x * 0.5f, float2.xz),
							height = new Bounds1(bounds3.min.y, prefabObjectGeometryData2.m_LegSize.y) + float2.y,
							rotation = transform2.m_Rotation
						};
						if ((m_EdgeCollisionMask & collisionMask2) != 0 && ValidationHelpers.Intersect(m_NodeOwners, topLevelEntity2, m_EdgeGeometryData, -origin, cylinder, bounds2, outData, areas, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
						}
						if ((m_StartCollisionMask & collisionMask2) != 0 && checkStart && ValidationHelpers.Intersect(m_NodeOwners.m_Start, topLevelEntity2, m_StartNodeGeometryData.m_Geometry, -origin, cylinder, bounds2, outData2, areas2, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
						}
						if ((m_EndCollisionMask & collisionMask2) != 0 && checkEnd && ValidationHelpers.Intersect(m_NodeOwners.m_End, topLevelEntity2, m_EndNodeGeometryData.m_Geometry, -origin, cylinder, bounds2, outData3, areas3, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
						}
					}
					else if ((prefabObjectGeometryData2.m_Flags & Game.Objects.GeometryFlags.IgnoreLegCollision) == 0)
					{
						Box3 box = new Box3(new Bounds3
						{
							min = 
							{
								y = bounds3.min.y,
								xz = prefabObjectGeometryData2.m_LegSize.xz * -0.5f
							},
							max = 
							{
								y = prefabObjectGeometryData2.m_LegSize.y,
								xz = prefabObjectGeometryData2.m_LegSize.xz * 0.5f
							}
						} + float2, transform2.m_Rotation);
						if ((m_EdgeCollisionMask & collisionMask2) != 0 && ValidationHelpers.Intersect(m_NodeOwners, topLevelEntity2, m_EdgeGeometryData, -origin, box, bounds2, outData, areas, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
						}
						if ((m_StartCollisionMask & collisionMask2) != 0 && checkStart && ValidationHelpers.Intersect(m_NodeOwners.m_Start, topLevelEntity2, m_StartNodeGeometryData.m_Geometry, -origin, box, bounds2, outData2, areas2, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
						}
						if ((m_EndCollisionMask & collisionMask2) != 0 && checkEnd && ValidationHelpers.Intersect(m_NodeOwners.m_End, topLevelEntity2, m_EndNodeGeometryData.m_Geometry, -origin, box, bounds2, outData3, areas3, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
						}
					}
				}
				bounds3.min.y = prefabObjectGeometryData2.m_LegSize.y;
			}
			if ((prefabObjectGeometryData2.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
			{
				Cylinder3 cylinder2 = new Cylinder3
				{
					circle = new Circle2(prefabObjectGeometryData2.m_Size.x * 0.5f, @float.xz),
					height = new Bounds1(bounds3.min.y, bounds3.max.y) + @float.y,
					rotation = transform2.m_Rotation
				};
				if ((m_EdgeCollisionMask & collisionMask2) != 0 && ValidationHelpers.Intersect(m_NodeOwners, topLevelEntity2, m_EdgeGeometryData, -origin, cylinder2, bounds2, outData, areas, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
				}
				if ((m_StartCollisionMask & collisionMask2) != 0 && checkStart && ValidationHelpers.Intersect(m_NodeOwners.m_Start, topLevelEntity2, m_StartNodeGeometryData.m_Geometry, -origin, cylinder2, bounds2, outData2, areas2, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
				}
				if ((m_EndCollisionMask & collisionMask2) != 0 && checkEnd && ValidationHelpers.Intersect(m_NodeOwners.m_End, topLevelEntity2, m_EndNodeGeometryData.m_Geometry, -origin, cylinder2, bounds2, outData3, areas3, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
				}
			}
			else
			{
				Box3 box2 = new Box3
				{
					bounds = bounds3 + @float,
					rotation = transform2.m_Rotation
				};
				if ((m_EdgeCollisionMask & collisionMask2) != 0 && ValidationHelpers.Intersect(m_NodeOwners, topLevelEntity2, m_EdgeGeometryData, -origin, box2, bounds2, outData, areas, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
				}
				if ((m_StartCollisionMask & collisionMask2) != 0 && checkStart && ValidationHelpers.Intersect(m_NodeOwners.m_Start, topLevelEntity2, m_StartNodeGeometryData.m_Geometry, -origin, box2, bounds2, outData2, areas2, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
				}
				if ((m_EndCollisionMask & collisionMask2) != 0 && checkEnd && ValidationHelpers.Intersect(m_NodeOwners.m_End, topLevelEntity2, m_EndNodeGeometryData.m_Geometry, -origin, box2, bounds2, outData3, areas3, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
				}
			}
			if (error.m_ErrorType != ErrorType.None)
			{
				error.m_Position = origin + MathUtils.Center(intersection);
			}
		}

		private void CheckOverlap2D(ref ErrorData error, CollisionMask collisionMask2, ObjectGeometryData prefabObjectGeometryData2, Bounds3 bounds2, Transform transformData2, Entity topLevelEntity2, float3 origin, bool checkStart, bool checkEnd, bool canBeOnRoadArea)
		{
			Bounds2 intersection = default(Bounds2);
			intersection.min = float.MaxValue;
			intersection.max = float.MinValue;
			Bounds1 bounds3 = default(Bounds1);
			bounds3.min = float.MaxValue;
			bounds3.max = float.MinValue;
			DynamicBuffer<NetCompositionArea> areas = m_EdgeCompositionAreas;
			DynamicBuffer<NetCompositionArea> areas2 = m_StartCompositionAreas;
			DynamicBuffer<NetCompositionArea> areas3 = m_EndCompositionAreas;
			if (!canBeOnRoadArea)
			{
				areas = default(DynamicBuffer<NetCompositionArea>);
				areas2 = default(DynamicBuffer<NetCompositionArea>);
				areas3 = default(DynamicBuffer<NetCompositionArea>);
			}
			if (ObjectUtils.GetStandingLegCount(prefabObjectGeometryData2, out var legCount))
			{
				for (int i = 0; i < legCount; i++)
				{
					float3 position = ObjectUtils.GetStandingLegPosition(prefabObjectGeometryData2, transformData2, i) - origin;
					if ((prefabObjectGeometryData2.m_Flags & (Game.Objects.GeometryFlags.CircularLeg | Game.Objects.GeometryFlags.IgnoreLegCollision)) == Game.Objects.GeometryFlags.CircularLeg)
					{
						Circle2 circle = new Circle2(prefabObjectGeometryData2.m_LegSize.x * 0.5f, position.xz);
						if (CommonUtils.ExclusiveGroundCollision(m_EdgeCollisionMask, collisionMask2) && ValidationHelpers.Intersect(m_NodeOwners, topLevelEntity2, m_EdgeGeometryData, -origin.xz, circle, bounds2.xz, m_EdgeCompositionData, areas, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
							bounds3 |= MathUtils.Center(m_EdgeGeometryData.m_Bounds.y & bounds2.y);
						}
						if (CommonUtils.ExclusiveGroundCollision(m_StartCollisionMask, collisionMask2) && checkStart && ValidationHelpers.Intersect(m_NodeOwners.m_Start, topLevelEntity2, m_StartNodeGeometryData.m_Geometry, -origin.xz, circle, bounds2.xz, m_StartCompositionData, areas2, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
							bounds3 |= MathUtils.Center(m_StartNodeGeometryData.m_Geometry.m_Bounds.y & bounds2.y);
						}
						if (CommonUtils.ExclusiveGroundCollision(m_EndCollisionMask, collisionMask2) && checkEnd && ValidationHelpers.Intersect(m_NodeOwners.m_End, topLevelEntity2, m_EndNodeGeometryData.m_Geometry, -origin.xz, circle, bounds2.xz, m_EndCompositionData, areas3, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
							bounds3 |= MathUtils.Center(m_EndNodeGeometryData.m_Geometry.m_Bounds.y & bounds2.y);
						}
					}
					else if ((prefabObjectGeometryData2.m_Flags & Game.Objects.GeometryFlags.IgnoreLegCollision) == 0)
					{
						Quad2 xz = ObjectUtils.CalculateBaseCorners(bounds: new Bounds3
						{
							min = 
							{
								xz = prefabObjectGeometryData2.m_LegSize.xz * -0.5f
							},
							max = 
							{
								xz = prefabObjectGeometryData2.m_LegSize.xz * 0.5f
							}
						}, position: position, rotation: transformData2.m_Rotation).xz;
						if (CommonUtils.ExclusiveGroundCollision(m_EdgeCollisionMask, collisionMask2) && ValidationHelpers.Intersect(m_NodeOwners, topLevelEntity2, m_EdgeGeometryData, -origin.xz, xz, bounds2.xz, m_EdgeCompositionData, areas, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
							bounds3 |= MathUtils.Center(m_EdgeGeometryData.m_Bounds.y & bounds2.y);
						}
						if (CommonUtils.ExclusiveGroundCollision(m_StartCollisionMask, collisionMask2) && checkStart && ValidationHelpers.Intersect(m_NodeOwners.m_Start, topLevelEntity2, m_StartNodeGeometryData.m_Geometry, -origin.xz, xz, bounds2.xz, m_StartCompositionData, areas2, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
							bounds3 |= MathUtils.Center(m_StartNodeGeometryData.m_Geometry.m_Bounds.y & bounds2.y);
						}
						if (CommonUtils.ExclusiveGroundCollision(m_EndCollisionMask, collisionMask2) && checkEnd && ValidationHelpers.Intersect(m_NodeOwners.m_End, topLevelEntity2, m_EndNodeGeometryData.m_Geometry, -origin.xz, xz, bounds2.xz, m_EndCompositionData, areas3, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
							bounds3 |= MathUtils.Center(m_EndNodeGeometryData.m_Geometry.m_Bounds.y & bounds2.y);
						}
					}
				}
			}
			else if ((prefabObjectGeometryData2.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
			{
				Circle2 circle2 = new Circle2(prefabObjectGeometryData2.m_Size.x * 0.5f, (transformData2.m_Position - origin).xz);
				if (CommonUtils.ExclusiveGroundCollision(m_EdgeCollisionMask, collisionMask2) && ValidationHelpers.Intersect(m_NodeOwners, topLevelEntity2, m_EdgeGeometryData, -origin.xz, circle2, bounds2.xz, m_EdgeCompositionData, areas, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
					bounds3 |= MathUtils.Center(m_EdgeGeometryData.m_Bounds.y & bounds2.y);
				}
				if (CommonUtils.ExclusiveGroundCollision(m_StartCollisionMask, collisionMask2) && checkStart && ValidationHelpers.Intersect(m_NodeOwners.m_Start, topLevelEntity2, m_StartNodeGeometryData.m_Geometry, -origin.xz, circle2, bounds2.xz, m_StartCompositionData, areas2, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
					bounds3 |= MathUtils.Center(m_StartNodeGeometryData.m_Geometry.m_Bounds.y & bounds2.y);
				}
				if (CommonUtils.ExclusiveGroundCollision(m_EndCollisionMask, collisionMask2) && checkEnd && ValidationHelpers.Intersect(m_NodeOwners.m_End, topLevelEntity2, m_EndNodeGeometryData.m_Geometry, -origin.xz, circle2, bounds2.xz, m_EndCompositionData, areas3, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
					bounds3 |= MathUtils.Center(m_EndNodeGeometryData.m_Geometry.m_Bounds.y & bounds2.y);
				}
			}
			else
			{
				Quad2 xz2 = ObjectUtils.CalculateBaseCorners(transformData2.m_Position - origin, transformData2.m_Rotation, prefabObjectGeometryData2.m_Bounds).xz;
				if (CommonUtils.ExclusiveGroundCollision(m_EdgeCollisionMask, collisionMask2) && ValidationHelpers.Intersect(m_NodeOwners, topLevelEntity2, m_EdgeGeometryData, -origin.xz, xz2, bounds2.xz, m_EdgeCompositionData, areas, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
					bounds3 |= MathUtils.Center(m_EdgeGeometryData.m_Bounds.y & bounds2.y);
				}
				if (CommonUtils.ExclusiveGroundCollision(m_StartCollisionMask, collisionMask2) && checkStart && ValidationHelpers.Intersect(m_NodeOwners.m_Start, topLevelEntity2, m_StartNodeGeometryData.m_Geometry, -origin.xz, xz2, bounds2.xz, m_StartCompositionData, areas2, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
					bounds3 |= MathUtils.Center(m_StartNodeGeometryData.m_Geometry.m_Bounds.y & bounds2.y);
				}
				if (CommonUtils.ExclusiveGroundCollision(m_EndCollisionMask, collisionMask2) && checkEnd && ValidationHelpers.Intersect(m_NodeOwners.m_End, topLevelEntity2, m_EndNodeGeometryData.m_Geometry, -origin.xz, xz2, bounds2.xz, m_EndCompositionData, areas3, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
					bounds3 |= MathUtils.Center(m_EndNodeGeometryData.m_Geometry.m_Bounds.y & bounds2.y);
				}
			}
			if (error.m_ErrorType != ErrorType.None)
			{
				error.m_Position.xz = origin.xz + MathUtils.Center(intersection);
				error.m_Position.y = MathUtils.Center(bounds3);
			}
		}
	}

	private struct AreaIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
	{
		public Entity m_EdgeEntity;

		public Bounds3 m_Bounds;

		public Edge m_NodeOwners;

		public bool m_IgnoreCollisions;

		public bool m_IgnoreProtectedAreas;

		public bool m_EditorMode;

		public EdgeGeometry m_EdgeGeometryData;

		public StartNodeGeometry m_StartNodeGeometryData;

		public EndNodeGeometry m_EndNodeGeometryData;

		public NetCompositionData m_EdgeCompositionData;

		public NetCompositionData m_StartCompositionData;

		public NetCompositionData m_EndCompositionData;

		public CollisionMask m_EdgeCollisionMask;

		public CollisionMask m_StartCollisionMask;

		public CollisionMask m_EndCollisionMask;

		public CollisionMask m_CombinedCollisionMask;

		public ValidationSystem.EntityData m_Data;

		public NativeQueue<ErrorData>.ParallelWriter m_ErrorQueue;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds.xz);
		}

		public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem areaItem2)
		{
			if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds.xz) || m_Data.m_Hidden.HasComponent(areaItem2.m_Area) || (m_Data.m_Area[areaItem2.m_Area].m_Flags & AreaFlags.Slave) != 0)
			{
				return;
			}
			PrefabRef prefabRef = m_Data.m_PrefabRef[areaItem2.m_Area];
			AreaGeometryData areaGeometryData = m_Data.m_PrefabAreaGeometry[prefabRef.m_Prefab];
			AreaUtils.SetCollisionFlags(ref areaGeometryData, !m_EditorMode || m_Data.m_Owner.HasComponent(areaItem2.m_Area));
			if ((areaGeometryData.m_Flags & (Game.Areas.GeometryFlags.PhysicalGeometry | Game.Areas.GeometryFlags.ProtectedArea)) == 0)
			{
				return;
			}
			if ((areaGeometryData.m_Flags & Game.Areas.GeometryFlags.ProtectedArea) != 0)
			{
				if (!m_Data.m_Native.HasComponent(areaItem2.m_Area) || m_IgnoreProtectedAreas)
				{
					return;
				}
			}
			else if (m_IgnoreCollisions)
			{
				return;
			}
			CollisionMask collisionMask = AreaUtils.GetCollisionMask(areaGeometryData);
			if ((m_CombinedCollisionMask & collisionMask) == 0)
			{
				return;
			}
			ErrorType errorType = ErrorType.OverlapExisting;
			if (areaGeometryData.m_Type == AreaType.MapTile)
			{
				errorType = ErrorType.ExceedsCityLimits;
				if ((m_EdgeCompositionData.m_State & CompositionState.Airspace) != 0)
				{
					return;
				}
			}
			DynamicBuffer<Game.Areas.Node> nodes = m_Data.m_AreaNodes[areaItem2.m_Area];
			Triangle triangle = m_Data.m_AreaTriangles[areaItem2.m_Area][areaItem2.m_Triangle];
			Triangle3 triangle2 = AreaUtils.GetTriangle3(nodes, triangle);
			ErrorData value = default(ErrorData);
			Bounds3 intersection = default(Bounds3);
			intersection.min = float.MaxValue;
			intersection.max = float.MinValue;
			if (areaGeometryData.m_Type != AreaType.MapTile && ((m_CombinedCollisionMask & CollisionMask.OnGround) == 0 || MathUtils.Intersect(bounds.m_Bounds, m_Bounds)))
			{
				Bounds1 heightRange = triangle.m_HeightRange;
				heightRange.max += areaGeometryData.m_MaxHeight;
				if ((m_EdgeCollisionMask & collisionMask) != 0 && ValidationHelpers.Intersect(m_NodeOwners, areaItem2.m_Area, m_EdgeGeometryData, triangle2, m_EdgeCompositionData, heightRange, ref intersection))
				{
					value.m_ErrorType = errorType;
				}
				if ((m_StartCollisionMask & collisionMask) != 0 && ValidationHelpers.Intersect(m_NodeOwners.m_Start, areaItem2.m_Area, m_StartNodeGeometryData.m_Geometry, triangle2, m_StartCompositionData, heightRange, ref intersection))
				{
					value.m_ErrorType = errorType;
				}
				if ((m_EndCollisionMask & collisionMask) != 0 && ValidationHelpers.Intersect(m_NodeOwners.m_End, areaItem2.m_Area, m_EndNodeGeometryData.m_Geometry, triangle2, m_EndCompositionData, heightRange, ref intersection))
				{
					value.m_ErrorType = errorType;
				}
			}
			if (areaGeometryData.m_Type == AreaType.MapTile || (value.m_ErrorType == ErrorType.None && CommonUtils.ExclusiveGroundCollision(m_CombinedCollisionMask, collisionMask)))
			{
				if ((m_EdgeCollisionMask & collisionMask) != 0 && ValidationHelpers.Intersect(m_NodeOwners, areaItem2.m_Area, m_EdgeGeometryData, triangle2.xz, bounds.m_Bounds, m_EdgeCompositionData, ref intersection))
				{
					value.m_ErrorType = errorType;
				}
				if ((m_StartCollisionMask & collisionMask) != 0 && ValidationHelpers.Intersect(m_NodeOwners.m_Start, areaItem2.m_Area, m_StartNodeGeometryData.m_Geometry, triangle2.xz, bounds.m_Bounds, m_StartCompositionData, ref intersection))
				{
					value.m_ErrorType = errorType;
				}
				if ((m_EndCollisionMask & collisionMask) != 0 && ValidationHelpers.Intersect(m_NodeOwners.m_End, areaItem2.m_Area, m_EndNodeGeometryData.m_Geometry, triangle2.xz, bounds.m_Bounds, m_EndCompositionData, ref intersection))
				{
					value.m_ErrorType = errorType;
				}
			}
			if (value.m_ErrorType != ErrorType.None)
			{
				value.m_ErrorSeverity = ErrorSeverity.Error;
				value.m_TempEntity = m_EdgeEntity;
				value.m_PermanentEntity = areaItem2.m_Area;
				value.m_Position = MathUtils.Center(intersection);
				value.m_Position.y = MathUtils.Clamp(value.m_Position.y, m_Bounds.y);
				m_ErrorQueue.Enqueue(value);
			}
		}
	}

	public static void ValidateEdge(Entity entity, Temp temp, Owner owner, Fixed _fixed, Edge edge, EdgeGeometry edgeGeometry, StartNodeGeometry startNodeGeometry, EndNodeGeometry endNodeGeometry, Composition composition, PrefabRef prefabRef, bool editorMode, ValidationSystem.EntityData data, NativeList<ValidationSystem.BoundsData> edgeList, NativeQuadTree<Entity, QuadTreeBoundsXZ> objectSearchTree, NativeQuadTree<Entity, QuadTreeBoundsXZ> netSearchTree, NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> areaSearchTree, WaterSurfaceData<SurfaceWater> waterSurfaceData, TerrainHeightData terrainHeightData, NativeQueue<ErrorData>.ParallelWriter errorQueue, NativeList<ConnectedNode> tempNodes)
	{
		Edge originalNodes = default(Edge);
		originalNodes.m_Start = GetNetNode(edge.m_Start, data);
		originalNodes.m_End = GetNetNode(edge.m_End, data);
		DynamicBuffer<ConnectedNode> dynamicBuffer = data.m_ConnectedNodes[entity];
		tempNodes.Clear();
		for (int i = 0; i < dynamicBuffer.Length; i++)
		{
			ConnectedNode value = dynamicBuffer[i];
			value.m_Node = GetNetNode(value.m_Node, data);
			tempNodes.Add(in value);
		}
		bool flag = owner.m_Owner != Entity.Null;
		Bounds3 bounds = edgeGeometry.m_Bounds | startNodeGeometry.m_Geometry.m_Bounds | endNodeGeometry.m_Geometry.m_Bounds;
		NetCompositionData netCompositionData = data.m_PrefabComposition[composition.m_Edge];
		NetCompositionData netCompositionData2 = data.m_PrefabComposition[composition.m_StartNode];
		NetCompositionData netCompositionData3 = data.m_PrefabComposition[composition.m_EndNode];
		CollisionMask collisionMask = NetUtils.GetCollisionMask(netCompositionData, !editorMode || flag);
		CollisionMask collisionMask2 = NetUtils.GetCollisionMask(netCompositionData2, !editorMode || flag);
		CollisionMask collisionMask3 = NetUtils.GetCollisionMask(netCompositionData3, !editorMode || flag);
		CollisionMask collisionMask4 = collisionMask | collisionMask2 | collisionMask3;
		DynamicBuffer<NetCompositionArea> edgeCompositionAreas = data.m_PrefabCompositionAreas[composition.m_Edge];
		DynamicBuffer<NetCompositionArea> startCompositionAreas = data.m_PrefabCompositionAreas[composition.m_StartNode];
		DynamicBuffer<NetCompositionArea> endCompositionAreas = data.m_PrefabCompositionAreas[composition.m_EndNode];
		Entity entity2 = entity;
		if (owner.m_Owner != Entity.Null && !data.m_AssetStamp.HasComponent(owner.m_Owner))
		{
			entity2 = owner.m_Owner;
			while (data.m_Owner.HasComponent(entity2) && !data.m_Building.HasComponent(entity2))
			{
				Entity owner2 = data.m_Owner[entity2].m_Owner;
				if (data.m_AssetStamp.HasComponent(owner2))
				{
					break;
				}
				entity2 = owner2;
			}
		}
		Edge componentData;
		bool flag2 = data.m_Edge.TryGetComponent(owner.m_Owner, out componentData);
		bool flag3 = data.m_ServiceUpgrade.HasComponent(entity) || (flag2 && data.m_ServiceUpgrade.HasComponent(owner.m_Owner));
		Edge ownerEdge = default(Edge);
		if (flag2)
		{
			ownerEdge.m_Start = GetNetNode(componentData.m_Start, data);
			ownerEdge.m_End = GetNetNode(componentData.m_End, data);
		}
		Edge nodeOwners = default(Edge);
		Edge nodeAssetStamps = default(Edge);
		Edge nodeEdges = default(Edge);
		nodeOwners.m_Start = GetOwner(originalNodes.m_Start, data, out nodeAssetStamps.m_Start, out nodeEdges.m_Start);
		nodeOwners.m_End = GetOwner(originalNodes.m_End, data, out nodeAssetStamps.m_End, out nodeEdges.m_End);
		NetIterator iterator = default(NetIterator);
		if ((temp.m_Flags & TempFlags.Delete) == 0)
		{
			iterator = new NetIterator
			{
				m_Edge = edge,
				m_OwnerEdge = ownerEdge,
				m_OriginalNodes = originalNodes,
				m_NodeOwners = nodeOwners,
				m_ConnectedNodes = dynamicBuffer.AsNativeArray(),
				m_OriginalConnectedNodes = tempNodes.AsArray(),
				m_TopLevelEntity = entity2,
				m_Essential = ((temp.m_Flags & TempFlags.Essential) != 0),
				m_EditorMode = editorMode,
				m_EdgeEntity = entity,
				m_OriginalEntity = temp.m_Original,
				m_Bounds = bounds,
				m_EdgeGeometryData = edgeGeometry,
				m_StartNodeGeometryData = startNodeGeometry,
				m_EndNodeGeometryData = endNodeGeometry,
				m_EdgeCompositionData = netCompositionData,
				m_StartCompositionData = netCompositionData2,
				m_EndCompositionData = netCompositionData3,
				m_EdgeCollisionMask = collisionMask,
				m_StartCollisionMask = collisionMask2,
				m_EndCollisionMask = collisionMask3,
				m_CombinedCollisionMask = collisionMask4,
				m_Data = data,
				m_ErrorQueue = errorQueue
			};
			netSearchTree.Iterate(ref iterator);
		}
		ObjectIterator objectIterator = default(ObjectIterator);
		if ((temp.m_Flags & TempFlags.Delete) == 0)
		{
			Entity assetStamp;
			Entity edge2;
			Entity owner3 = GetOwner(entity, data, out assetStamp, out edge2);
			objectIterator = new ObjectIterator
			{
				m_OriginalNodes = originalNodes,
				m_NodeOwners = nodeOwners,
				m_NodeAssetStamps = nodeAssetStamps,
				m_NodeEdges = nodeEdges,
				m_OwnerEdge = ownerEdge,
				m_EdgeEntity = entity,
				m_TopLevelEntity = owner3,
				m_AssetStampEntity = assetStamp,
				m_Bounds = bounds,
				m_EdgeGeometryData = edgeGeometry,
				m_StartNodeGeometryData = startNodeGeometry,
				m_EndNodeGeometryData = endNodeGeometry,
				m_EdgeCompositionData = netCompositionData,
				m_StartCompositionData = netCompositionData2,
				m_EndCompositionData = netCompositionData3,
				m_EdgeCollisionMask = collisionMask,
				m_StartCollisionMask = collisionMask2,
				m_EndCollisionMask = collisionMask3,
				m_CombinedCollisionMask = collisionMask4,
				m_EdgeCompositionAreas = edgeCompositionAreas,
				m_StartCompositionAreas = startCompositionAreas,
				m_EndCompositionAreas = endCompositionAreas,
				m_Data = data,
				m_ErrorQueue = errorQueue,
				m_EditorMode = editorMode
			};
			objectSearchTree.Iterate(ref objectIterator);
		}
		AreaIterator iterator2 = new AreaIterator
		{
			m_NodeOwners = nodeOwners,
			m_EdgeEntity = entity,
			m_Bounds = bounds,
			m_IgnoreCollisions = ((temp.m_Flags & TempFlags.Delete) != 0),
			m_IgnoreProtectedAreas = ((temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Modify | TempFlags.Replace | TempFlags.Upgrade)) == 0 || (temp.m_Flags & TempFlags.Hidden) != 0),
			m_EditorMode = editorMode,
			m_EdgeGeometryData = edgeGeometry,
			m_StartNodeGeometryData = startNodeGeometry,
			m_EndNodeGeometryData = endNodeGeometry,
			m_EdgeCompositionData = netCompositionData,
			m_StartCompositionData = netCompositionData2,
			m_EndCompositionData = netCompositionData3,
			m_EdgeCollisionMask = collisionMask,
			m_StartCollisionMask = collisionMask2,
			m_EndCollisionMask = collisionMask3,
			m_CombinedCollisionMask = collisionMask4,
			m_Data = data,
			m_ErrorQueue = errorQueue
		};
		areaSearchTree.Iterate(ref iterator2);
		if ((temp.m_Flags & TempFlags.Delete) == 0 && edgeList.Length != 0)
		{
			int num = 0;
			int num2 = edgeList.Length;
			float3 @float = edgeList[edgeList.Length - 1].m_Bounds.max - edgeList[0].m_Bounds.min;
			bool flag4 = @float.z > @float.x;
			while (num < num2)
			{
				int num3 = num + num2 >> 1;
				bool2 @bool = edgeList[num3].m_Bounds.min.xz < bounds.min.xz;
				if (flag4 ? @bool.y : @bool.x)
				{
					num = num3 + 1;
				}
				else
				{
					num2 = num3;
				}
			}
			Edge edge3 = default(Edge);
			if (data.m_Owner.TryGetComponent(edge.m_Start, out var componentData2))
			{
				edge3.m_Start = componentData2.m_Owner;
			}
			if (data.m_Owner.TryGetComponent(edge.m_End, out componentData2))
			{
				edge3.m_End = componentData2.m_Owner;
			}
			for (int j = 0; j < edgeList.Length; j++)
			{
				ValidationSystem.BoundsData boundsData = edgeList[j];
				bool2 bool2 = boundsData.m_Bounds.min.xz > bounds.max.xz;
				if (flag4 ? bool2.y : bool2.x)
				{
					break;
				}
				if ((collisionMask4 & CollisionMask.OnGround) != 0)
				{
					if (!MathUtils.Intersect(bounds.xz, boundsData.m_Bounds.xz))
					{
						continue;
					}
				}
				else if (!MathUtils.Intersect(bounds, boundsData.m_Bounds))
				{
					continue;
				}
				if (boundsData.m_Entity == entity || (boundsData.m_Bounds.min.x == bounds.min.x && boundsData.m_Entity.Index < entity.Index))
				{
					continue;
				}
				Entity entity3 = boundsData.m_Entity;
				bool flag5 = data.m_ServiceUpgrade.HasComponent(boundsData.m_Entity);
				if (data.m_Owner.TryGetComponent(boundsData.m_Entity, out var componentData3))
				{
					Entity owner4 = componentData3.m_Owner;
					if (!data.m_AssetStamp.HasComponent(owner4))
					{
						entity3 = owner4;
						while (data.m_Owner.HasComponent(entity3) && !data.m_Building.HasComponent(entity3))
						{
							owner4 = data.m_Owner[entity3].m_Owner;
							if (data.m_AssetStamp.HasComponent(owner4))
							{
								break;
							}
							entity3 = owner4;
						}
					}
					if (data.m_Edge.TryGetComponent(componentData3.m_Owner, out var componentData4))
					{
						if (edge.m_Start == componentData4.m_Start || edge.m_Start == componentData4.m_End || edge.m_End == componentData4.m_Start || edge.m_End == componentData4.m_End)
						{
							continue;
						}
						flag5 |= data.m_ServiceUpgrade.HasComponent(componentData3.m_Owner);
					}
				}
				if (!(entity2 == entity3) || flag3 || flag5)
				{
					Edge edgeData = data.m_Edge[boundsData.m_Entity];
					if (!(edge.m_Start == edgeData.m_Start) && !(edge.m_Start == edgeData.m_End) && !(edge.m_End == edgeData.m_Start) && !(edge.m_End == edgeData.m_End) && (!flag2 || (!(componentData.m_Start == edgeData.m_Start) && !(componentData.m_Start == edgeData.m_End) && !(componentData.m_End == edgeData.m_Start) && !(componentData.m_End == edgeData.m_End))) && !(boundsData.m_Entity == edge3.m_Start) && !(boundsData.m_Entity == edge3.m_End) && (!data.m_Owner.TryGetComponent(edgeData.m_Start, out componentData2) || !(componentData2.m_Owner == entity)) && (!data.m_Owner.TryGetComponent(edgeData.m_End, out componentData2) || !(componentData2.m_Owner == entity)))
					{
						EdgeGeometry edgeGeometryData = data.m_EdgeGeometry[boundsData.m_Entity];
						StartNodeGeometry startNodeGeometryData = data.m_StartNodeGeometry[boundsData.m_Entity];
						EndNodeGeometry endNodeGeometryData = data.m_EndNodeGeometry[boundsData.m_Entity];
						Composition compositionData = data.m_Composition[boundsData.m_Entity];
						Temp temp2 = data.m_Temp[boundsData.m_Entity];
						iterator.CheckOverlap(entity3, boundsData.m_Entity, boundsData.m_Bounds, edgeData, compositionData, edgeGeometryData, startNodeGeometryData, endNodeGeometryData, (temp2.m_Flags & TempFlags.Essential) != 0, componentData3.m_Owner != Entity.Null);
					}
				}
			}
		}
		Bounds3 errorBounds = default(Bounds3);
		errorBounds.min = float.MaxValue;
		errorBounds.max = float.MinValue;
		bool flag6 = (temp.m_Flags & TempFlags.Essential) == 0;
		LotData lotData = default(LotData);
		if ((temp.m_Flags & TempFlags.Delete) == 0)
		{
			bool num4 = entity2 != entity && IsInternal(entity2, edge.m_Start, data.m_ConnectedEdges[edge.m_Start], data);
			bool flag7 = entity2 != entity && IsInternal(entity2, edge.m_End, data.m_ConnectedEdges[edge.m_End], data);
			bool flag8 = false;
			if (!num4 || !flag7)
			{
				flag8 |= CheckGeometryShape(edgeGeometry, ref errorBounds);
			}
			if (!num4)
			{
				flag8 |= CheckGeometryShape(startNodeGeometry.m_Geometry, ref errorBounds);
			}
			if (!flag7)
			{
				flag8 |= CheckGeometryShape(endNodeGeometry.m_Geometry, ref errorBounds);
			}
			if (flag8)
			{
				errorQueue.Enqueue(new ErrorData
				{
					m_ErrorType = ErrorType.InvalidShape,
					m_ErrorSeverity = ErrorSeverity.Error,
					m_Position = MathUtils.Center(errorBounds),
					m_TempEntity = entity
				});
			}
			if (data.m_PrefabNetGeometry.HasComponent(prefabRef.m_Prefab))
			{
				Curve curve = data.m_Curve[entity];
				NetGeometryData netGeometryData = data.m_PrefabNetGeometry[prefabRef.m_Prefab];
				Bounds1 edgeLengthRange = netGeometryData.m_EdgeLengthRange;
				if (_fixed.m_Index >= 0 && data.m_PrefabFixedElements.HasBuffer(prefabRef.m_Prefab))
				{
					DynamicBuffer<FixedNetElement> dynamicBuffer2 = data.m_PrefabFixedElements[prefabRef.m_Prefab];
					if (_fixed.m_Index < dynamicBuffer2.Length)
					{
						Bounds1 lengthRange = dynamicBuffer2[_fixed.m_Index].m_LengthRange;
						edgeLengthRange.min = math.select(lengthRange.min, lengthRange.min * 0.6f, lengthRange.max == lengthRange.min);
						edgeLengthRange.max = lengthRange.max;
					}
				}
				if ((netCompositionData.m_State & CompositionState.HalfLength) != 0)
				{
					edgeLengthRange.min *= 0.1f;
				}
				edgeLengthRange.max *= 1.1f;
				Bezier4x3 curve2 = MathUtils.Lerp(edgeGeometry.m_Start.m_Left, edgeGeometry.m_Start.m_Right, 0.5f);
				Bezier4x3 curve3 = MathUtils.Lerp(edgeGeometry.m_End.m_Left, edgeGeometry.m_End.m_Right, 0.5f);
				float num5 = MathUtils.Length(curve2.xz);
				float num6 = MathUtils.Length(curve3.xz);
				if (num5 + num6 < edgeLengthRange.min)
				{
					errorQueue.Enqueue(new ErrorData
					{
						m_ErrorType = ErrorType.ShortDistance,
						m_ErrorSeverity = ErrorSeverity.Error,
						m_Position = math.lerp(edgeGeometry.m_Start.m_Left.d, edgeGeometry.m_Start.m_Right.d, 0.5f),
						m_TempEntity = entity
					});
				}
				if (num5 + num6 > edgeLengthRange.max)
				{
					errorQueue.Enqueue(new ErrorData
					{
						m_ErrorType = ErrorType.LongDistance,
						m_ErrorSeverity = ErrorSeverity.Error,
						m_Position = math.lerp(edgeGeometry.m_Start.m_Left.d, edgeGeometry.m_Start.m_Right.d, 0.5f),
						m_TempEntity = entity
					});
				}
				if (flag6 && (netGeometryData.m_Flags & GeometryFlags.FlattenTerrain) != 0 && (temp.m_Flags & (TempFlags.Create | TempFlags.Modify)) != 0)
				{
					flag6 = false;
					if (data.m_Transform.TryGetComponent(owner.m_Owner, out var componentData5) && data.m_PrefabRef.TryGetComponent(owner.m_Owner, out var componentData6) && data.m_PrefabObjectGeometry.TryGetComponent(componentData6.m_Prefab, out var componentData7))
					{
						lotData.m_IsValid = true;
						lotData.m_InverseRotation = math.inverse(componentData5.m_Rotation);
						lotData.m_Position = componentData5.m_Position;
						if ((componentData7.m_Flags & Game.Objects.GeometryFlags.Standing) != Game.Objects.GeometryFlags.None)
						{
							lotData.m_IsCircular = (componentData7.m_Flags & Game.Objects.GeometryFlags.CircularLeg) != 0;
							lotData.m_Limits = componentData7.m_LegSize.xz + componentData7.m_LegOffset * 2f + 0.4f;
						}
						else
						{
							lotData.m_IsCircular = (componentData7.m_Flags & Game.Objects.GeometryFlags.Circular) != 0;
							lotData.m_Limits = componentData7.m_Size.xz + 0.4f;
						}
						lotData.m_Limits *= 0.5f;
					}
				}
				if (netGeometryData.m_MaxSlopeSteepness != 0f && !flag6)
				{
					float3 float2 = default(float3);
					float2.x = math.abs(curve2.d.y - curve2.a.y) / math.max(0.1f, num5);
					float2.y = math.abs(curve3.d.y - curve3.a.y) / math.max(0.1f, num6);
					float2.z = math.abs(curve.m_Bezier.d.y - curve.m_Bezier.a.y) / math.max(0.1f, MathUtils.Length(curve.m_Bezier.xz));
					bool3 x = float2 >= new float3(netGeometryData.m_MaxSlopeSteepness * 2f, netGeometryData.m_MaxSlopeSteepness * 2f, netGeometryData.m_MaxSlopeSteepness + 0.0005f);
					x.x &= !lotData.IsPointInside(MathUtils.Position(curve2, 0.5f));
					x.y &= !lotData.IsPointInside(MathUtils.Position(curve3, 0.5f));
					x.z &= !lotData.IsPointInside(MathUtils.Position(curve.m_Bezier, 0.5f));
					if (math.any(x))
					{
						float4 float3 = default(float4);
						if (x.x)
						{
							float3 += new float4(math.lerp(MathUtils.Position(edgeGeometry.m_Start.m_Left, 0.5f), MathUtils.Position(edgeGeometry.m_Start.m_Right, 0.5f), 0.5f), 1f);
						}
						if (x.y)
						{
							float3 += new float4(math.lerp(MathUtils.Position(edgeGeometry.m_End.m_Left, 0.5f), MathUtils.Position(edgeGeometry.m_End.m_Right, 0.5f), 0.5f), 1f);
						}
						if (x.z)
						{
							float3 += new float4(math.lerp(edgeGeometry.m_Start.m_Left.d, edgeGeometry.m_Start.m_Right.d, 0.5f), 1f);
						}
						errorQueue.Enqueue(new ErrorData
						{
							m_ErrorType = ErrorType.SteepSlope,
							m_ErrorSeverity = ErrorSeverity.Error,
							m_Position = float3.xyz / float3.w,
							m_TempEntity = entity
						});
					}
				}
				if ((netGeometryData.m_Flags & GeometryFlags.RequireElevated) != 0)
				{
					data.m_NetElevation.TryGetComponent(edge.m_Start, out var componentData8);
					data.m_NetElevation.TryGetComponent(entity, out var componentData9);
					data.m_NetElevation.TryGetComponent(edge.m_End, out var componentData10);
					if (!math.all(math.max(math.max(math.cmin(componentData8.m_Elevation), math.cmin(componentData10.m_Elevation)), componentData9.m_Elevation) >= netGeometryData.m_ElevationLimit * 2f))
					{
						errorQueue.Enqueue(new ErrorData
						{
							m_ErrorType = ErrorType.LowElevation,
							m_ErrorSeverity = ErrorSeverity.Error,
							m_Position = math.lerp(edgeGeometry.m_Start.m_Left.d, edgeGeometry.m_Start.m_Right.d, 0.5f),
							m_TempEntity = entity
						});
					}
				}
				if ((netCompositionData2.m_Flags.m_General & CompositionFlags.General.FixedNodeSize) != 0)
				{
					Node node = data.m_Node[edge.m_Start];
					if (math.abs(math.dot(math.normalizesafe(MathUtils.StartTangent(curve.m_Bezier).xz), math.forward(node.m_Rotation).xz)) < 0.99999f || (netCompositionData2.m_Flags.m_General & CompositionFlags.General.Intersection) != 0)
					{
						errorQueue.Enqueue(new ErrorData
						{
							m_ErrorType = ErrorType.InvalidShape,
							m_ErrorSeverity = ErrorSeverity.Error,
							m_Position = node.m_Position,
							m_TempEntity = entity
						});
					}
				}
				if ((netCompositionData3.m_Flags.m_General & CompositionFlags.General.FixedNodeSize) != 0)
				{
					Node node2 = data.m_Node[edge.m_End];
					if (math.abs(math.dot(math.normalizesafe(MathUtils.EndTangent(curve.m_Bezier).xz), math.forward(node2.m_Rotation).xz)) < 0.99999f || (netCompositionData3.m_Flags.m_General & CompositionFlags.General.Intersection) != 0)
					{
						errorQueue.Enqueue(new ErrorData
						{
							m_ErrorType = ErrorType.InvalidShape,
							m_ErrorSeverity = ErrorSeverity.Error,
							m_Position = node2.m_Position,
							m_TempEntity = entity
						});
					}
				}
			}
		}
		if ((temp.m_Flags & (TempFlags.Create | TempFlags.Modify)) != 0 && !flag6 && data.m_PlaceableNet.HasComponent(prefabRef.m_Prefab))
		{
			PlaceableNetData placeableNetData = data.m_PlaceableNet[prefabRef.m_Prefab];
			errorBounds.min = float.MaxValue;
			errorBounds.max = float.MinValue;
			if (CheckSurface(waterSurfaceData, terrainHeightData, placeableNetData, netCompositionData, edgeGeometry.m_Start, lotData, ref errorBounds) | CheckSurface(waterSurfaceData, terrainHeightData, placeableNetData, netCompositionData, edgeGeometry.m_End, lotData, ref errorBounds) | CheckSurface(waterSurfaceData, terrainHeightData, placeableNetData, netCompositionData2, startNodeGeometry.m_Geometry.m_Left, lotData, ref errorBounds) | CheckSurface(waterSurfaceData, terrainHeightData, placeableNetData, netCompositionData2, startNodeGeometry.m_Geometry.m_Right, lotData, ref errorBounds) | CheckSurface(waterSurfaceData, terrainHeightData, placeableNetData, netCompositionData3, endNodeGeometry.m_Geometry.m_Left, lotData, ref errorBounds) | CheckSurface(waterSurfaceData, terrainHeightData, placeableNetData, netCompositionData3, endNodeGeometry.m_Geometry.m_Right, lotData, ref errorBounds))
			{
				ErrorData value2 = default(ErrorData);
				if ((placeableNetData.m_PlacementFlags & PlacementFlags.Floating) != PlacementFlags.None)
				{
					value2.m_ErrorType = ErrorType.NoWater;
				}
				else
				{
					value2.m_ErrorType = ErrorType.InWater;
				}
				value2.m_ErrorSeverity = ErrorSeverity.Error;
				value2.m_Position = MathUtils.Center(errorBounds);
				value2.m_TempEntity = entity;
				errorQueue.Enqueue(value2);
			}
		}
		if ((temp.m_Flags & (TempFlags.Create | TempFlags.Modify | TempFlags.Replace | TempFlags.Upgrade)) != 0)
		{
			bounds = edgeGeometry.m_Bounds;
			if (math.any(startNodeGeometry.m_Geometry.m_Left.m_Length > 0.05f) | math.any(startNodeGeometry.m_Geometry.m_Right.m_Length > 0.05f))
			{
				bounds |= startNodeGeometry.m_Geometry.m_Bounds;
			}
			if (math.any(endNodeGeometry.m_Geometry.m_Left.m_Length > 0.05f) | math.any(endNodeGeometry.m_Geometry.m_Right.m_Length > 0.05f))
			{
				bounds |= endNodeGeometry.m_Geometry.m_Bounds;
			}
			Game.Objects.ValidationHelpers.ValidateWorldBounds(entity, owner, bounds, data, terrainHeightData, errorQueue);
		}
	}

	private static bool IsInternal(Entity topLevelEntity, Entity node, DynamicBuffer<ConnectedEdge> connectedEdges, ValidationSystem.EntityData data)
	{
		for (int i = 0; i < connectedEdges.Length; i++)
		{
			Entity entity = connectedEdges[i].m_Edge;
			Edge edge = data.m_Edge[entity];
			if (!(edge.m_Start == node) && !(edge.m_End == node))
			{
				continue;
			}
			while (data.m_Owner.HasComponent(entity) && !data.m_Building.HasComponent(entity))
			{
				Entity owner = data.m_Owner[entity].m_Owner;
				if (data.m_AssetStamp.HasComponent(owner))
				{
					break;
				}
				entity = owner;
			}
			if (topLevelEntity != entity)
			{
				return false;
			}
		}
		return true;
	}

	private static bool CheckSurface(WaterSurfaceData<SurfaceWater> waterSurfaceData, TerrainHeightData terrainHeightData, PlaceableNetData placeableNetData, NetCompositionData compositionData, Segment segment, LotData lotData, ref Bounds3 errorBounds)
	{
		bool result = false;
		bool flag = (placeableNetData.m_PlacementFlags & (PlacementFlags.OnGround | PlacementFlags.Floating)) == PlacementFlags.Floating;
		bool flag2 = (placeableNetData.m_PlacementFlags & (PlacementFlags.OnGround | PlacementFlags.Floating | PlacementFlags.ShoreLine)) == PlacementFlags.OnGround;
		flag2 &= (compositionData.m_Flags.m_General & CompositionFlags.General.Tunnel) == 0;
		if (flag || flag2)
		{
			float sampleInterval = WaterUtils.GetSampleInterval(ref waterSurfaceData);
			int num = (int)math.ceil(segment.middleLength / sampleInterval);
			for (int i = 0; i < num; i++)
			{
				float t = ((float)i + 0.5f) / (float)num;
				float3 @float = MathUtils.Position(segment.m_Left, t);
				float3 float2 = MathUtils.Position(segment.m_Right, t);
				int num2 = (int)math.ceil(math.distance(@float, float2) / sampleInterval);
				for (int j = 0; j < num2; j++)
				{
					float t2 = ((float)j + 0.5f) / (float)num2;
					float3 float3 = math.lerp(@float, float2, t2);
					if (!lotData.IsPointInside(float3))
					{
						float num3 = WaterUtils.SampleDepth(ref waterSurfaceData, float3);
						if (flag2 && num3 >= 0.2f && WaterUtils.SampleHeight(ref waterSurfaceData, ref terrainHeightData, float3) > float3.y + compositionData.m_HeightRange.min)
						{
							errorBounds |= float3;
							result = true;
						}
						if (flag && num3 < 0.2f)
						{
							errorBounds |= float3;
							result = true;
						}
					}
				}
			}
		}
		return result;
	}

	private static Entity GetNetNode(Entity entity, ValidationSystem.EntityData data)
	{
		if (data.m_Temp.TryGetComponent(entity, out var componentData))
		{
			return componentData.m_Original;
		}
		return entity;
	}

	private static Entity GetOwner(Entity entity, ValidationSystem.EntityData data, out Entity assetStamp, out Entity edge)
	{
		assetStamp = Entity.Null;
		edge = Entity.Null;
		Owner componentData;
		while (data.m_Owner.TryGetComponent(entity, out componentData) && !data.m_Building.HasComponent(entity))
		{
			if (data.m_AssetStamp.HasComponent(componentData.m_Owner))
			{
				assetStamp = componentData.m_Owner;
				break;
			}
			if (data.m_Edge.HasComponent(componentData.m_Owner))
			{
				edge = componentData.m_Owner;
			}
			entity = componentData.m_Owner;
			if (data.m_Temp.HasComponent(entity))
			{
				entity = data.m_Temp[entity].m_Original;
			}
		}
		return entity;
	}

	public static void ValidateLane(Entity entity, Owner owner, Lane lane, TrackLane trackLane, Curve curve, EdgeLane edgeLane, PrefabRef prefabRef, ValidationSystem.EntityData data, NativeQueue<ErrorData>.ParallelWriter errorQueue)
	{
		if (data.m_Edge.HasComponent(owner.m_Owner))
		{
			TrackLaneData trackLaneData = data.m_TrackLaneData[prefabRef.m_Prefab];
			if (trackLane.m_Curviness > trackLaneData.m_MaxCurviness && data.m_Temp.TryGetComponent(owner.m_Owner, out var componentData) && (componentData.m_Flags & TempFlags.Essential) != 0)
			{
				errorQueue.Enqueue(new ErrorData
				{
					m_ErrorType = ErrorType.TightCurve,
					m_Position = MathUtils.Position(curve.m_Bezier, 0.5f),
					m_ErrorSeverity = ErrorSeverity.Error,
					m_TempEntity = owner.m_Owner
				});
			}
			Edge edge = data.m_Edge[owner.m_Owner];
			bool flag = (trackLane.m_Flags & TrackLaneFlags.Twoway) != 0;
			bool flag2;
			Entity entity2;
			if (edgeLane.m_EdgeDelta.x < 0.001f)
			{
				flag2 = FindConnectedLane(edge.m_Start, owner.m_Owner, lane.m_StartNode, data) || IsIgnored(owner.m_Owner, edge.m_Start, data, trackLaneData.m_TrackTypes, flag, isTarget: true);
				entity2 = edge.m_Start;
			}
			else if (edgeLane.m_EdgeDelta.x > 0.999f)
			{
				flag2 = FindConnectedLane(edge.m_End, owner.m_Owner, lane.m_StartNode, data) || IsIgnored(owner.m_Owner, edge.m_End, data, trackLaneData.m_TrackTypes, flag, isTarget: true);
				entity2 = edge.m_End;
			}
			else
			{
				flag2 = true;
				entity2 = Entity.Null;
			}
			bool flag3;
			Entity entity3;
			if (edgeLane.m_EdgeDelta.y < 0.001f)
			{
				flag3 = FindConnectedLane(edge.m_Start, owner.m_Owner, lane.m_EndNode, data) || IsIgnored(owner.m_Owner, edge.m_Start, data, trackLaneData.m_TrackTypes, isSource: true, flag);
				entity3 = edge.m_Start;
			}
			else if (edgeLane.m_EdgeDelta.y > 0.999f)
			{
				flag3 = FindConnectedLane(edge.m_End, owner.m_Owner, lane.m_EndNode, data) || IsIgnored(owner.m_Owner, edge.m_End, data, trackLaneData.m_TrackTypes, isSource: true, flag);
				entity3 = edge.m_End;
			}
			else
			{
				flag3 = true;
				entity3 = Entity.Null;
			}
			if (!flag2 && data.m_Temp.TryGetComponent(entity2, out var componentData2) && (componentData2.m_Flags & TempFlags.Essential) != 0)
			{
				errorQueue.Enqueue(new ErrorData
				{
					m_ErrorType = ErrorType.TightCurve,
					m_Position = (curve.m_Bezier.a + data.m_Node[entity2].m_Position) * 0.5f,
					m_ErrorSeverity = ErrorSeverity.Warning,
					m_TempEntity = entity2
				});
			}
			if (!flag3 && data.m_Temp.TryGetComponent(entity3, out var componentData3) && (componentData3.m_Flags & TempFlags.Essential) != 0)
			{
				errorQueue.Enqueue(new ErrorData
				{
					m_ErrorType = ErrorType.TightCurve,
					m_Position = (curve.m_Bezier.d + data.m_Node[entity3].m_Position) * 0.5f,
					m_ErrorSeverity = ErrorSeverity.Warning,
					m_TempEntity = entity3
				});
			}
		}
	}

	private static bool FindConnectedLane(Entity owner, Entity ignore, PathNode node, ValidationSystem.EntityData data)
	{
		if (FindConnectedLane(owner, node, data))
		{
			return true;
		}
		if (node.OwnerEquals(new PathNode(owner, 0)) && data.m_ConnectedEdges.TryGetBuffer(owner, out var bufferData))
		{
			for (int i = 0; i < bufferData.Length; i++)
			{
				ConnectedEdge connectedEdge = bufferData[i];
				if (!(connectedEdge.m_Edge == ignore) && FindConnectedLane(connectedEdge.m_Edge, node, data))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool FindConnectedLane(Entity owner, PathNode node, ValidationSystem.EntityData data)
	{
		DynamicBuffer<SubLane> dynamicBuffer = data.m_Lanes[owner];
		for (int i = 0; i < dynamicBuffer.Length; i++)
		{
			Lane lane = data.m_Lane[dynamicBuffer[i].m_SubLane];
			if (lane.m_StartNode.Equals(node) || lane.m_EndNode.Equals(node))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsIgnored(Entity edge, Entity node, ValidationSystem.EntityData data, TrackTypes trackTypes, bool isSource, bool isTarget)
	{
		EdgeIterator edgeIterator = new EdgeIterator(edge, node, data.m_ConnectedEdges, data.m_Edge, data.m_Temp, data.m_Hidden);
		EdgeIteratorValue value;
		while (edgeIterator.GetNext(out value))
		{
			if (value.m_Edge == edge || !data.m_Lanes.TryGetBuffer(value.m_Edge, out var bufferData))
			{
				continue;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity subLane = bufferData[i].m_SubLane;
				if (!data.m_TrackLane.TryGetComponent(subLane, out var componentData))
				{
					continue;
				}
				PrefabRef prefabRef = data.m_PrefabRef[subLane];
				if (data.m_TrackLaneData[prefabRef.m_Prefab].m_TrackTypes == trackTypes)
				{
					bool num = (componentData.m_Flags & TrackLaneFlags.Twoway) != 0;
					bool flag = (componentData.m_Flags & TrackLaneFlags.Invert) != 0;
					bool flag2 = num | (value.m_End != flag);
					bool flag3 = num | (value.m_End == flag);
					if ((isSource && flag3) || (isTarget && flag2))
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	private static bool CheckGeometryShape(EdgeGeometry geometry, ref Bounds3 errorBounds)
	{
		if (math.any(geometry.m_Start.m_Length + geometry.m_End.m_Length > 0.1f))
		{
			return CheckSegmentShape(geometry.m_Start, ref errorBounds) | CheckSegmentShape(geometry.m_End, ref errorBounds);
		}
		return false;
	}

	private static bool CheckGeometryShape(EdgeNodeGeometry geometry, ref Bounds3 errorBounds)
	{
		if (math.any(geometry.m_Left.m_Length > 0.05f) | math.any(geometry.m_Right.m_Length > 0.05f))
		{
			return CheckSegmentShape(geometry.m_Left, ref errorBounds) | CheckSegmentShape(geometry.m_Right, ref errorBounds);
		}
		return false;
	}

	private static bool CheckSegmentShape(Segment segment, ref Bounds3 errorBounds)
	{
		bool result = false;
		Quad3 quad = default(Quad3);
		quad.a = segment.m_Left.a;
		quad.b = segment.m_Right.a;
		float3 y = quad.b - quad.a;
		for (int i = 1; i <= 8; i++)
		{
			float t = (float)i / 8f;
			quad.d = MathUtils.Position(segment.m_Left, t);
			quad.c = MathUtils.Position(segment.m_Right, t);
			float3 @float = quad.d - quad.a;
			float3 float2 = quad.c - quad.b;
			float3 float3 = quad.c - quad.d;
			@float = math.select(@float, 0f, math.lengthsq(@float) < 0.0001f);
			float2 = math.select(float2, 0f, math.lengthsq(float2) < 0.0001f);
			if ((math.cross(@float, y).y < 0f) | (math.cross(float2, float3).y < 0f))
			{
				errorBounds |= MathUtils.Bounds(quad);
				result = true;
			}
			quad.a = quad.d;
			quad.b = quad.c;
			y = float3;
		}
		return result;
	}

	public static void Check3DCollisionMasks(CollisionMask mask1, CollisionMask mask2, NetCompositionData inData1, NetCompositionData inData2, out NetCompositionData outData1, out NetCompositionData outData2)
	{
		outData1 = inData1;
		outData2 = inData2;
		if ((mask1 & CollisionMask.ExclusiveGround) != 0 && (mask2 & CollisionMask.Overground) != 0)
		{
			outData1.m_HeightRange.min -= 10000f;
		}
		if ((mask2 & CollisionMask.ExclusiveGround) != 0 && (mask1 & CollisionMask.Overground) != 0)
		{
			outData2.m_HeightRange.min -= 10000f;
		}
	}

	public static void Check3DCollisionMasks(CollisionMask mask1, CollisionMask mask2, NetCompositionData inData1, out NetCompositionData outData1)
	{
		outData1 = inData1;
		if ((mask1 & CollisionMask.ExclusiveGround) != 0 && (mask2 & CollisionMask.Overground) != 0)
		{
			outData1.m_HeightRange.min -= 10000f;
		}
	}

	public static bool Intersect(Edge edge1, Entity node2, EdgeGeometry edgeGeometry1, float3 offset1, Box3 box2, Bounds3 bounds2, NetCompositionData prefabCompositionData1, DynamicBuffer<NetCompositionArea> areas1, ref Bounds3 intersection)
	{
		if (!MathUtils.Intersect(edgeGeometry1.m_Bounds, bounds2))
		{
			return false;
		}
		if (math.all(edgeGeometry1.m_Start.m_Length + edgeGeometry1.m_End.m_Length <= 0.1f))
		{
			return false;
		}
		bool flag = false;
		if (edge1.m_Start != node2 || (prefabCompositionData1.m_State & CompositionState.HasSurface) != 0)
		{
			flag |= Intersect(edgeGeometry1.m_Start, new float2(0f, 1f), offset1, box2, bounds2, prefabCompositionData1, areas1, ref intersection);
		}
		if (edge1.m_End != node2 || (prefabCompositionData1.m_State & CompositionState.HasSurface) != 0)
		{
			flag |= Intersect(edgeGeometry1.m_End, new float2(0f, 1f), offset1, box2, bounds2, prefabCompositionData1, areas1, ref intersection);
		}
		return flag;
	}

	public static bool Intersect(Edge edge1, Entity node2, EdgeGeometry edgeGeometry1, float2 offset1, Quad2 quad2, Bounds2 bounds2, NetCompositionData prefabCompositionData1, DynamicBuffer<NetCompositionArea> areas1, ref Bounds2 intersection)
	{
		if (!MathUtils.Intersect(edgeGeometry1.m_Bounds.xz, bounds2))
		{
			return false;
		}
		if (math.all(edgeGeometry1.m_Start.m_Length + edgeGeometry1.m_End.m_Length <= 0.1f))
		{
			return false;
		}
		bool flag = false;
		if (edge1.m_Start != node2 || (prefabCompositionData1.m_State & CompositionState.HasSurface) != 0)
		{
			flag |= Intersect(edgeGeometry1.m_Start, new float2(0f, 1f), offset1, quad2, bounds2, prefabCompositionData1, areas1, ref intersection);
		}
		if (edge1.m_End != node2 || (prefabCompositionData1.m_State & CompositionState.HasSurface) != 0)
		{
			flag |= Intersect(edgeGeometry1.m_End, new float2(0f, 1f), offset1, quad2, bounds2, prefabCompositionData1, areas1, ref intersection);
		}
		return flag;
	}

	public static bool Intersect(Entity node1, Entity node2, EdgeNodeGeometry nodeGeometry1, float3 offset1, Box3 box2, Bounds3 bounds2, NetCompositionData prefabCompositionData1, DynamicBuffer<NetCompositionArea> areas1, ref Bounds3 intersection)
	{
		if (!MathUtils.Intersect(nodeGeometry1.m_Bounds, bounds2))
		{
			return false;
		}
		if (node1 == node2)
		{
			return false;
		}
		if (math.all(nodeGeometry1.m_Left.m_Length <= 0.05f) && math.all(nodeGeometry1.m_Left.m_Length <= 0.05f))
		{
			return false;
		}
		if (nodeGeometry1.m_MiddleRadius > 0f)
		{
			Segment right = nodeGeometry1.m_Right;
			Segment right2 = nodeGeometry1.m_Right;
			right.m_Right = MathUtils.Lerp(nodeGeometry1.m_Right.m_Left, nodeGeometry1.m_Right.m_Right, 0.5f);
			right.m_Right.d = nodeGeometry1.m_Middle.d;
			right2.m_Left = right.m_Right;
			return Intersect(nodeGeometry1.m_Left, new float2(0f, 1f), offset1, box2, bounds2, prefabCompositionData1, areas1, ref intersection) | Intersect(right, new float2(0f, 0.5f), offset1, box2, bounds2, prefabCompositionData1, areas1, ref intersection) | Intersect(right2, new float2(0.5f, 1f), offset1, box2, bounds2, prefabCompositionData1, areas1, ref intersection);
		}
		Segment left = nodeGeometry1.m_Left;
		Segment right3 = nodeGeometry1.m_Right;
		left.m_Right = nodeGeometry1.m_Middle;
		right3.m_Left = nodeGeometry1.m_Middle;
		return Intersect(nodeGeometry1.m_Left, new float2(0f, 0.5f), offset1, box2, bounds2, prefabCompositionData1, areas1, ref intersection) | Intersect(nodeGeometry1.m_Right, new float2(0.5f, 1f), offset1, box2, bounds2, prefabCompositionData1, areas1, ref intersection) | Intersect(left, new float2(0f, 0.5f), offset1, box2, bounds2, prefabCompositionData1, areas1, ref intersection) | Intersect(right3, new float2(0.5f, 1f), offset1, box2, bounds2, prefabCompositionData1, areas1, ref intersection);
	}

	public static bool Intersect(Entity node1, Entity node2, EdgeNodeGeometry nodeGeometry1, float2 offset1, Quad2 quad2, Bounds2 bounds2, NetCompositionData prefabCompositionData1, DynamicBuffer<NetCompositionArea> areas1, ref Bounds2 intersection)
	{
		if (!MathUtils.Intersect(nodeGeometry1.m_Bounds.xz, bounds2))
		{
			return false;
		}
		if (node1 == node2)
		{
			return false;
		}
		if (math.all(nodeGeometry1.m_Left.m_Length <= 0.05f) && math.all(nodeGeometry1.m_Left.m_Length <= 0.05f))
		{
			return false;
		}
		if (nodeGeometry1.m_MiddleRadius > 0f)
		{
			Segment right = nodeGeometry1.m_Right;
			Segment right2 = nodeGeometry1.m_Right;
			right.m_Right = MathUtils.Lerp(nodeGeometry1.m_Right.m_Left, nodeGeometry1.m_Right.m_Right, 0.5f);
			right.m_Right.d = nodeGeometry1.m_Middle.d;
			right2.m_Left = right.m_Right;
			return Intersect(nodeGeometry1.m_Left, new float2(0f, 1f), offset1, quad2, bounds2, prefabCompositionData1, areas1, ref intersection) | Intersect(right, new float2(0f, 0.5f), offset1, quad2, bounds2, prefabCompositionData1, areas1, ref intersection) | Intersect(right2, new float2(0.5f, 1f), offset1, quad2, bounds2, prefabCompositionData1, areas1, ref intersection);
		}
		Segment left = nodeGeometry1.m_Left;
		Segment right3 = nodeGeometry1.m_Right;
		left.m_Right = nodeGeometry1.m_Middle;
		right3.m_Left = nodeGeometry1.m_Middle;
		return Intersect(nodeGeometry1.m_Left, new float2(0f, 0.5f), offset1, quad2, bounds2, prefabCompositionData1, areas1, ref intersection) | Intersect(nodeGeometry1.m_Right, new float2(0.5f, 1f), offset1, quad2, bounds2, prefabCompositionData1, areas1, ref intersection) | Intersect(left, new float2(0f, 0.5f), offset1, quad2, bounds2, prefabCompositionData1, areas1, ref intersection) | Intersect(right3, new float2(0.5f, 1f), offset1, quad2, bounds2, prefabCompositionData1, areas1, ref intersection);
	}

	public static bool Intersect(Segment segment1, float2 segmentSide1, float3 offset1, Box3 box2, Bounds3 bounds2, NetCompositionData prefabCompositionData1, DynamicBuffer<NetCompositionArea> areas1, ref Bounds3 intersection)
	{
		if (!MathUtils.Intersect(SetHeightRange(MathUtils.Bounds(segment1.m_Left) | MathUtils.Bounds(segment1.m_Right), prefabCompositionData1.m_HeightRange), bounds2))
		{
			return false;
		}
		if (areas1.IsCreated)
		{
			for (int i = 0; i < areas1.Length; i++)
			{
				NetCompositionArea netCompositionArea = areas1[i];
				if ((netCompositionArea.m_Flags & (NetAreaFlags.Buildable | NetAreaFlags.Hole)) == 0)
				{
					continue;
				}
				float num = netCompositionArea.m_Width * 0.51f;
				float3 x = MathUtils.Size(box2.bounds) * 0.5f;
				if (math.cmin(x) >= num)
				{
					continue;
				}
				float num2 = netCompositionArea.m_Position.x / prefabCompositionData1.m_Width + 0.5f;
				if (num2 < segmentSide1.x || num2 > segmentSide1.y)
				{
					continue;
				}
				Bezier4x3 curve = MathUtils.Lerp(segment1.m_Left, segment1.m_Right, (num2 - segmentSide1.x) / (segmentSide1.y - segmentSide1.x));
				Bounds3 bounds3 = MathUtils.Bounds(curve);
				bounds3.min.xz -= num;
				bounds3.max.xz += num;
				if (!MathUtils.Intersect(SetHeightRange(bounds3, prefabCompositionData1.m_HeightRange), bounds2))
				{
					continue;
				}
				curve += offset1;
				float3 @float = math.mul(box2.rotation, MathUtils.Center(box2.bounds));
				float2 xz = math.mul(box2.rotation, new float3(x.x, 0f, 0f)).xz;
				float2 xz2 = math.mul(box2.rotation, new float3(0f, x.y, 0f)).xz;
				float2 xz3 = math.mul(box2.rotation, new float3(0f, 0f, x.z)).xz;
				MathUtils.Distance(curve.xz, @float.xz, out var t);
				float3 float2 = MathUtils.Position(curve, t);
				if ((netCompositionArea.m_Flags & NetAreaFlags.Hole) != 0 || !(bounds2.min.y + offset1.y <= float2.y + prefabCompositionData1.m_HeightRange.min))
				{
					float2 y = MathUtils.Right(math.normalizesafe(MathUtils.Tangent(curve, t).xz));
					if (math.abs(math.dot(@float.xz - float2.xz, y)) + math.csum(math.abs(new float3(math.dot(xz, y), math.dot(xz2, y), math.dot(xz3, y)))) < num)
					{
						return false;
					}
				}
			}
		}
		bool result = false;
		Quad3 quad = default(Quad3);
		quad.a = segment1.m_Left.a;
		quad.b = segment1.m_Right.a;
		Bounds3 bounds4 = SetHeightRange(MathUtils.Bounds(quad.a, quad.b), prefabCompositionData1.m_HeightRange);
		for (int j = 1; j <= 8; j++)
		{
			float t2 = (float)j / 8f;
			quad.d = MathUtils.Position(segment1.m_Left, t2);
			quad.c = MathUtils.Position(segment1.m_Right, t2);
			Bounds3 bounds5 = SetHeightRange(MathUtils.Bounds(quad.d, quad.c), prefabCompositionData1.m_HeightRange);
			if (MathUtils.Intersect(bounds4 | bounds5, bounds2))
			{
				Quad3 quad2 = quad;
				float3 float3 = math.normalizesafe(quad2.b - quad2.a) * 0.5f;
				float3 float4 = math.normalizesafe(quad2.d - quad2.c) * 0.5f;
				quad2.a += float3;
				quad2.b -= float3;
				quad2.c += float4;
				quad2.d -= float4;
				TrigonalTrapezohedron3 trapezohedron = new TrigonalTrapezohedron3(quad2, quad2);
				float3 float5 = new float3(offset1.x, offset1.y + prefabCompositionData1.m_HeightRange.min, offset1.z);
				float3 float6 = new float3(offset1.x, offset1.y + prefabCompositionData1.m_HeightRange.max, offset1.z);
				trapezohedron.a.a += float5;
				trapezohedron.a.b += float5;
				trapezohedron.a.c += float5;
				trapezohedron.a.d += float5;
				trapezohedron.b.a += float6;
				trapezohedron.b.b += float6;
				trapezohedron.b.c += float6;
				trapezohedron.b.d += float6;
				if (MathUtils.Intersect(trapezohedron, box2, out var intersection2, out var intersection3))
				{
					Box3 box3 = new Box3(intersection3, box2.rotation);
					result = true;
					intersection |= intersection2 | MathUtils.Bounds(box3);
				}
			}
			quad.a = quad.d;
			quad.b = quad.c;
			bounds4 = bounds5;
		}
		return result;
	}

	public static bool Intersect(Segment segment1, float2 segmentSide1, float2 offset1, Quad2 quad2, Bounds2 bounds2, NetCompositionData prefabCompositionData1, DynamicBuffer<NetCompositionArea> areas1, ref Bounds2 intersection)
	{
		if (!MathUtils.Intersect((MathUtils.Bounds(segment1.m_Left) | MathUtils.Bounds(segment1.m_Right)).xz, bounds2))
		{
			return false;
		}
		if (areas1.IsCreated)
		{
			for (int i = 0; i < areas1.Length; i++)
			{
				NetCompositionArea netCompositionArea = areas1[i];
				if ((netCompositionArea.m_Flags & (NetAreaFlags.Buildable | NetAreaFlags.Hole)) == 0)
				{
					continue;
				}
				float2 x = new float2(math.max(math.distance(quad2.a, quad2.b), math.distance(quad2.c, quad2.d)), math.max(math.distance(quad2.b, quad2.c), math.distance(quad2.d, quad2.a)));
				float num = netCompositionArea.m_Width * 0.51f;
				if (math.cmin(x) * 0.5f >= num)
				{
					continue;
				}
				float num2 = netCompositionArea.m_Position.x / prefabCompositionData1.m_Width + 0.5f;
				if (num2 < segmentSide1.x || num2 > segmentSide1.y)
				{
					continue;
				}
				Bezier4x2 xz = MathUtils.Lerp(segment1.m_Left, segment1.m_Right, (num2 - segmentSide1.x) / (segmentSide1.y - segmentSide1.x)).xz;
				Bounds2 bounds3 = MathUtils.Bounds(xz);
				bounds3.min -= num;
				bounds3.max += num;
				if (MathUtils.Intersect(bounds3, bounds2))
				{
					xz += offset1;
					float2 position = MathUtils.Center(quad2);
					MathUtils.Distance(xz, position, out var t);
					float2 @float = MathUtils.Position(xz, t);
					float2 x2 = MathUtils.Right(math.normalizesafe(MathUtils.Tangent(xz, t)));
					if (math.cmax(math.abs(new float4(math.dot(x2, quad2.a - @float), math.dot(x2, quad2.b - @float), math.dot(x2, quad2.c - @float), math.dot(x2, quad2.d - @float)))) < num)
					{
						return false;
					}
				}
			}
		}
		bool result = false;
		Quad2 quad3 = default(Quad2);
		quad3.a = segment1.m_Left.a.xz;
		quad3.b = segment1.m_Right.a.xz;
		Bounds2 bounds4 = MathUtils.Bounds(quad3.a, quad3.b);
		for (int j = 1; j <= 8; j++)
		{
			float t2 = (float)j / 8f;
			quad3.d = MathUtils.Position(segment1.m_Left, t2).xz;
			quad3.c = MathUtils.Position(segment1.m_Right, t2).xz;
			Bounds2 bounds5 = MathUtils.Bounds(quad3.d, quad3.c);
			if (MathUtils.Intersect(bounds4 | bounds5, bounds2))
			{
				Quad2 quad4 = quad3;
				float2 float2 = math.normalizesafe(quad4.b - quad4.a) * 0.5f;
				float2 float3 = math.normalizesafe(quad4.d - quad4.c) * 0.5f;
				quad4.a += float2;
				quad4.b -= float2;
				quad4.c += float3;
				quad4.d -= float3;
				quad4.a += offset1;
				quad4.b += offset1;
				quad4.c += offset1;
				quad4.d += offset1;
				if (MathUtils.Intersect(quad4, quad2, out var intersection2))
				{
					result = true;
					intersection |= intersection2;
				}
			}
			quad3.a = quad3.d;
			quad3.b = quad3.c;
			bounds4 = bounds5;
		}
		return result;
	}

	public static bool Intersect(Edge edge1, Entity node2, EdgeGeometry edgeGeometry1, Triangle2 triangle2, Bounds3 bounds2, NetCompositionData prefabCompositionData1, ref Bounds3 intersection)
	{
		if (!MathUtils.Intersect(edgeGeometry1.m_Bounds.xz, bounds2.xz))
		{
			return false;
		}
		if (math.all(edgeGeometry1.m_Start.m_Length + edgeGeometry1.m_End.m_Length <= 0.1f))
		{
			return false;
		}
		bool flag = false;
		if (edge1.m_Start != node2)
		{
			flag |= Intersect(edgeGeometry1.m_Start, new float2(0f, 1f), triangle2, bounds2, prefabCompositionData1, ref intersection);
		}
		if (edge1.m_End != node2)
		{
			flag |= Intersect(edgeGeometry1.m_End, new float2(0f, 1f), triangle2, bounds2, prefabCompositionData1, ref intersection);
		}
		return flag;
	}

	public static bool Intersect(Edge edge1, Entity node2, EdgeGeometry edgeGeometry1, Triangle3 triangle2, NetCompositionData prefabCompositionData1, Bounds1 heightRange2, ref Bounds3 intersection)
	{
		Bounds3 bounds = SetHeightRange(MathUtils.Bounds(triangle2), heightRange2);
		if (!MathUtils.Intersect(edgeGeometry1.m_Bounds, bounds))
		{
			return false;
		}
		if (math.all(edgeGeometry1.m_Start.m_Length + edgeGeometry1.m_End.m_Length <= 0.1f))
		{
			return false;
		}
		bool flag = false;
		if (edge1.m_Start != node2)
		{
			flag |= Intersect(edgeGeometry1.m_Start, new float2(0f, 1f), triangle2, prefabCompositionData1, heightRange2, ref intersection);
		}
		if (edge1.m_End != node2)
		{
			flag |= Intersect(edgeGeometry1.m_End, new float2(0f, 1f), triangle2, prefabCompositionData1, heightRange2, ref intersection);
		}
		return flag;
	}

	public static bool Intersect(Edge edge1, Entity node2, EdgeGeometry edgeGeometry1, Line2.Segment line2, Bounds3 bounds2, NetCompositionData prefabCompositionData1, ref Bounds3 intersection)
	{
		if (!MathUtils.Intersect(edgeGeometry1.m_Bounds.xz, bounds2.xz))
		{
			return false;
		}
		if (math.all(edgeGeometry1.m_Start.m_Length + edgeGeometry1.m_End.m_Length <= 0.1f))
		{
			return false;
		}
		bool flag = false;
		if (edge1.m_Start != node2)
		{
			flag |= Intersect(edgeGeometry1.m_Start, new float2(0f, 1f), line2, bounds2, prefabCompositionData1, ref intersection);
		}
		if (edge1.m_End != node2)
		{
			flag |= Intersect(edgeGeometry1.m_End, new float2(0f, 1f), line2, bounds2, prefabCompositionData1, ref intersection);
		}
		return flag;
	}

	public static bool Intersect(Entity node1, Entity node2, EdgeNodeGeometry nodeGeometry1, Triangle2 triangle2, Bounds3 bounds2, NetCompositionData prefabCompositionData1, ref Bounds3 intersection)
	{
		if (!MathUtils.Intersect(nodeGeometry1.m_Bounds.xz, bounds2.xz))
		{
			return false;
		}
		if (node1 == node2)
		{
			return false;
		}
		if (math.all(nodeGeometry1.m_Left.m_Length <= 0.05f) && math.all(nodeGeometry1.m_Left.m_Length <= 0.05f))
		{
			return false;
		}
		if (nodeGeometry1.m_MiddleRadius > 0f)
		{
			Segment right = nodeGeometry1.m_Right;
			Segment right2 = nodeGeometry1.m_Right;
			right.m_Right = MathUtils.Lerp(nodeGeometry1.m_Right.m_Left, nodeGeometry1.m_Right.m_Right, 0.5f);
			right.m_Right.d = nodeGeometry1.m_Middle.d;
			right2.m_Left = right.m_Right;
			return Intersect(nodeGeometry1.m_Left, new float2(0f, 1f), triangle2, bounds2, prefabCompositionData1, ref intersection) | Intersect(right, new float2(0f, 0.5f), triangle2, bounds2, prefabCompositionData1, ref intersection) | Intersect(right2, new float2(0.5f, 1f), triangle2, bounds2, prefabCompositionData1, ref intersection);
		}
		Segment left = nodeGeometry1.m_Left;
		Segment right3 = nodeGeometry1.m_Right;
		left.m_Right = nodeGeometry1.m_Middle;
		right3.m_Left = nodeGeometry1.m_Middle;
		return Intersect(nodeGeometry1.m_Left, new float2(0f, 0.5f), triangle2, bounds2, prefabCompositionData1, ref intersection) | Intersect(nodeGeometry1.m_Right, new float2(0.5f, 1f), triangle2, bounds2, prefabCompositionData1, ref intersection) | Intersect(left, new float2(0f, 0.5f), triangle2, bounds2, prefabCompositionData1, ref intersection) | Intersect(right3, new float2(0.5f, 1f), triangle2, bounds2, prefabCompositionData1, ref intersection);
	}

	public static bool Intersect(Entity node1, Entity node2, EdgeNodeGeometry nodeGeometry1, Triangle3 triangle2, NetCompositionData prefabCompositionData1, Bounds1 heightRange2, ref Bounds3 intersection)
	{
		Bounds3 bounds = SetHeightRange(MathUtils.Bounds(triangle2), heightRange2);
		if (!MathUtils.Intersect(nodeGeometry1.m_Bounds, bounds))
		{
			return false;
		}
		if (node1 == node2)
		{
			return false;
		}
		if (math.all(nodeGeometry1.m_Left.m_Length <= 0.05f) && math.all(nodeGeometry1.m_Left.m_Length <= 0.05f))
		{
			return false;
		}
		if (nodeGeometry1.m_MiddleRadius > 0f)
		{
			Segment right = nodeGeometry1.m_Right;
			Segment right2 = nodeGeometry1.m_Right;
			right.m_Right = MathUtils.Lerp(nodeGeometry1.m_Right.m_Left, nodeGeometry1.m_Right.m_Right, 0.5f);
			right.m_Right.d = nodeGeometry1.m_Middle.d;
			right2.m_Left = right.m_Right;
			return Intersect(nodeGeometry1.m_Left, new float2(0f, 1f), triangle2, prefabCompositionData1, heightRange2, ref intersection) | Intersect(right, new float2(0f, 0.5f), triangle2, prefabCompositionData1, heightRange2, ref intersection) | Intersect(right2, new float2(0.5f, 1f), triangle2, prefabCompositionData1, heightRange2, ref intersection);
		}
		Segment left = nodeGeometry1.m_Left;
		Segment right3 = nodeGeometry1.m_Right;
		left.m_Right = nodeGeometry1.m_Middle;
		right3.m_Left = nodeGeometry1.m_Middle;
		return Intersect(nodeGeometry1.m_Left, new float2(0f, 0.5f), triangle2, prefabCompositionData1, heightRange2, ref intersection) | Intersect(nodeGeometry1.m_Right, new float2(0.5f, 1f), triangle2, prefabCompositionData1, heightRange2, ref intersection) | Intersect(left, new float2(0f, 0.5f), triangle2, prefabCompositionData1, heightRange2, ref intersection) | Intersect(right3, new float2(0.5f, 1f), triangle2, prefabCompositionData1, heightRange2, ref intersection);
	}

	public static bool Intersect(Entity node1, Entity node2, EdgeNodeGeometry nodeGeometry1, Line2.Segment line2, Bounds3 bounds2, NetCompositionData prefabCompositionData1, ref Bounds3 intersection)
	{
		if (!MathUtils.Intersect(nodeGeometry1.m_Bounds.xz, bounds2.xz))
		{
			return false;
		}
		if (node1 == node2)
		{
			return false;
		}
		if (math.all(nodeGeometry1.m_Left.m_Length <= 0.05f) && math.all(nodeGeometry1.m_Left.m_Length <= 0.05f))
		{
			return false;
		}
		if (nodeGeometry1.m_MiddleRadius > 0f)
		{
			Segment right = nodeGeometry1.m_Right;
			Segment right2 = nodeGeometry1.m_Right;
			right.m_Right = MathUtils.Lerp(nodeGeometry1.m_Right.m_Left, nodeGeometry1.m_Right.m_Right, 0.5f);
			right.m_Right.d = nodeGeometry1.m_Middle.d;
			right2.m_Left = right.m_Right;
			return Intersect(nodeGeometry1.m_Left, new float2(0f, 1f), line2, bounds2, prefabCompositionData1, ref intersection) | Intersect(right, new float2(0f, 0.5f), line2, bounds2, prefabCompositionData1, ref intersection) | Intersect(right2, new float2(0.5f, 1f), line2, bounds2, prefabCompositionData1, ref intersection);
		}
		Segment left = nodeGeometry1.m_Left;
		Segment right3 = nodeGeometry1.m_Right;
		left.m_Right = nodeGeometry1.m_Middle;
		right3.m_Left = nodeGeometry1.m_Middle;
		return Intersect(nodeGeometry1.m_Left, new float2(0f, 0.5f), line2, bounds2, prefabCompositionData1, ref intersection) | Intersect(nodeGeometry1.m_Right, new float2(0.5f, 1f), line2, bounds2, prefabCompositionData1, ref intersection) | Intersect(left, new float2(0f, 0.5f), line2, bounds2, prefabCompositionData1, ref intersection) | Intersect(right3, new float2(0.5f, 1f), line2, bounds2, prefabCompositionData1, ref intersection);
	}

	public static bool Intersect(Segment segment1, float2 segmentSide1, Triangle2 triangle2, Bounds3 bounds2, NetCompositionData prefabCompositionData1, ref Bounds3 intersection)
	{
		if (!MathUtils.Intersect(SetHeightRange(MathUtils.Bounds(segment1.m_Left) | MathUtils.Bounds(segment1.m_Right), prefabCompositionData1.m_HeightRange).xz, bounds2.xz))
		{
			return false;
		}
		bool result = false;
		Quad3 quad = default(Quad3);
		quad.a = segment1.m_Left.a;
		quad.b = segment1.m_Right.a;
		Bounds3 bounds3 = SetHeightRange(MathUtils.Bounds(quad.a, quad.b), prefabCompositionData1.m_HeightRange);
		for (int i = 1; i <= 8; i++)
		{
			float t = (float)i / 8f;
			quad.d = MathUtils.Position(segment1.m_Left, t);
			quad.c = MathUtils.Position(segment1.m_Right, t);
			Bounds3 bounds4 = SetHeightRange(MathUtils.Bounds(quad.d, quad.c), prefabCompositionData1.m_HeightRange);
			Bounds3 bounds5 = bounds3 | bounds4;
			if (MathUtils.Intersect(bounds5.xz, bounds2.xz))
			{
				Quad2 xz = quad.xz;
				float2 @float = math.normalizesafe(xz.b - xz.a) * 0.5f;
				float2 float2 = math.normalizesafe(xz.d - xz.c) * 0.5f;
				xz.a += @float;
				xz.b -= @float;
				xz.c += float2;
				xz.d -= float2;
				if (MathUtils.Intersect(xz, triangle2))
				{
					result = true;
					intersection |= bounds5 & bounds2;
				}
			}
			quad.a = quad.d;
			quad.b = quad.c;
			bounds3 = bounds4;
		}
		return result;
	}

	public static bool Intersect(Segment segment1, float2 segmentSide1, Line2.Segment line2, Bounds3 bounds2, NetCompositionData prefabCompositionData1, ref Bounds3 intersection)
	{
		if (!MathUtils.Intersect(SetHeightRange(MathUtils.Bounds(segment1.m_Left) | MathUtils.Bounds(segment1.m_Right), prefabCompositionData1.m_HeightRange).xz, bounds2.xz))
		{
			return false;
		}
		bool result = false;
		Quad3 quad = default(Quad3);
		quad.a = segment1.m_Left.a;
		quad.b = segment1.m_Right.a;
		Bounds3 bounds3 = SetHeightRange(MathUtils.Bounds(quad.a, quad.b), prefabCompositionData1.m_HeightRange);
		for (int i = 1; i <= 8; i++)
		{
			float t = (float)i / 8f;
			quad.d = MathUtils.Position(segment1.m_Left, t);
			quad.c = MathUtils.Position(segment1.m_Right, t);
			Bounds3 bounds4 = SetHeightRange(MathUtils.Bounds(quad.d, quad.c), prefabCompositionData1.m_HeightRange);
			Bounds3 bounds5 = bounds3 | bounds4;
			if (MathUtils.Intersect(bounds5.xz, bounds2.xz))
			{
				Quad2 xz = quad.xz;
				float2 @float = math.normalizesafe(xz.b - xz.a) * 0.5f;
				float2 x = xz.d - xz.c;
				float2 t2 = default(float2);
				float2 float2 = math.normalizesafe(x, t2) * 0.5f;
				xz.a += @float;
				xz.b -= @float;
				xz.c += float2;
				xz.d -= float2;
				if (MathUtils.Intersect(xz, line2, out t2))
				{
					result = true;
					intersection |= bounds5 & bounds2;
				}
			}
			quad.a = quad.d;
			quad.b = quad.c;
			bounds3 = bounds4;
		}
		return result;
	}

	public static bool Intersect(Edge edge1, Entity node2, EdgeGeometry edgeGeometry1, float3 offset1, Cylinder3 cylinder2, Bounds3 bounds2, NetCompositionData prefabCompositionData1, DynamicBuffer<NetCompositionArea> areas1, ref Bounds3 intersection)
	{
		if (!MathUtils.Intersect(edgeGeometry1.m_Bounds, bounds2))
		{
			return false;
		}
		if (math.all(edgeGeometry1.m_Start.m_Length + edgeGeometry1.m_End.m_Length <= 0.1f))
		{
			return false;
		}
		bool flag = false;
		if (edge1.m_Start != node2 || (prefabCompositionData1.m_State & CompositionState.HasSurface) != 0)
		{
			flag |= Intersect(edgeGeometry1.m_Start, new float2(0f, 1f), offset1, cylinder2, bounds2, prefabCompositionData1, areas1, ref intersection);
		}
		if (edge1.m_End != node2 || (prefabCompositionData1.m_State & CompositionState.HasSurface) != 0)
		{
			flag |= Intersect(edgeGeometry1.m_End, new float2(0f, 1f), offset1, cylinder2, bounds2, prefabCompositionData1, areas1, ref intersection);
		}
		return flag;
	}

	public static bool Intersect(Edge edge1, Entity node2, EdgeGeometry edgeGeometry1, float2 offset1, Circle2 circle2, Bounds2 bounds2, NetCompositionData prefabCompositionData1, DynamicBuffer<NetCompositionArea> areas1, ref Bounds2 intersection)
	{
		if (!MathUtils.Intersect(edgeGeometry1.m_Bounds.xz, bounds2))
		{
			return false;
		}
		if (math.all(edgeGeometry1.m_Start.m_Length + edgeGeometry1.m_End.m_Length <= 0.1f))
		{
			return false;
		}
		bool flag = false;
		if (edge1.m_Start != node2 || (prefabCompositionData1.m_State & CompositionState.HasSurface) != 0)
		{
			flag |= Intersect(edgeGeometry1.m_Start, new float2(0f, 1f), offset1, circle2, bounds2, prefabCompositionData1, areas1, ref intersection);
		}
		if (edge1.m_End != node2 || (prefabCompositionData1.m_State & CompositionState.HasSurface) != 0)
		{
			flag |= Intersect(edgeGeometry1.m_End, new float2(0f, 1f), offset1, circle2, bounds2, prefabCompositionData1, areas1, ref intersection);
		}
		return flag;
	}

	public static bool Intersect(Entity node1, Entity node2, EdgeNodeGeometry nodeGeometry1, float3 offset1, Cylinder3 cylinder2, Bounds3 bounds2, NetCompositionData prefabCompositionData1, DynamicBuffer<NetCompositionArea> areas1, ref Bounds3 intersection)
	{
		if (!MathUtils.Intersect(nodeGeometry1.m_Bounds, bounds2))
		{
			return false;
		}
		if (node1 == node2)
		{
			return false;
		}
		if (math.all(nodeGeometry1.m_Left.m_Length <= 0.05f) && math.all(nodeGeometry1.m_Left.m_Length <= 0.05f))
		{
			return false;
		}
		if (nodeGeometry1.m_MiddleRadius > 0f)
		{
			Segment right = nodeGeometry1.m_Right;
			Segment right2 = nodeGeometry1.m_Right;
			right.m_Right = MathUtils.Lerp(nodeGeometry1.m_Right.m_Left, nodeGeometry1.m_Right.m_Right, 0.5f);
			right.m_Right.d = nodeGeometry1.m_Middle.d;
			right2.m_Left = right.m_Right;
			return Intersect(nodeGeometry1.m_Left, new float2(0f, 1f), offset1, cylinder2, bounds2, prefabCompositionData1, areas1, ref intersection) | Intersect(right, new float2(0f, 0.5f), offset1, cylinder2, bounds2, prefabCompositionData1, areas1, ref intersection) | Intersect(right2, new float2(0.5f, 1f), offset1, cylinder2, bounds2, prefabCompositionData1, areas1, ref intersection);
		}
		Segment left = nodeGeometry1.m_Left;
		Segment right3 = nodeGeometry1.m_Right;
		left.m_Right = nodeGeometry1.m_Middle;
		right3.m_Left = nodeGeometry1.m_Middle;
		return Intersect(nodeGeometry1.m_Left, new float2(0f, 0.5f), offset1, cylinder2, bounds2, prefabCompositionData1, areas1, ref intersection) | Intersect(nodeGeometry1.m_Right, new float2(0.5f, 1f), offset1, cylinder2, bounds2, prefabCompositionData1, areas1, ref intersection) | Intersect(left, new float2(0f, 0.5f), offset1, cylinder2, bounds2, prefabCompositionData1, areas1, ref intersection) | Intersect(right3, new float2(0.5f, 1f), offset1, cylinder2, bounds2, prefabCompositionData1, areas1, ref intersection);
	}

	public static bool Intersect(Entity node1, Entity node2, EdgeNodeGeometry nodeGeometry1, float2 offset1, Circle2 circle2, Bounds2 bounds2, NetCompositionData prefabCompositionData1, DynamicBuffer<NetCompositionArea> areas1, ref Bounds2 intersection)
	{
		if (!MathUtils.Intersect(nodeGeometry1.m_Bounds.xz, bounds2))
		{
			return false;
		}
		if (node1 == node2)
		{
			return false;
		}
		if (math.all(nodeGeometry1.m_Left.m_Length <= 0.05f) && math.all(nodeGeometry1.m_Left.m_Length <= 0.05f))
		{
			return false;
		}
		if (nodeGeometry1.m_MiddleRadius > 0f)
		{
			Segment right = nodeGeometry1.m_Right;
			Segment right2 = nodeGeometry1.m_Right;
			right.m_Right = MathUtils.Lerp(nodeGeometry1.m_Right.m_Left, nodeGeometry1.m_Right.m_Right, 0.5f);
			right.m_Right.d = nodeGeometry1.m_Middle.d;
			right2.m_Left = right.m_Right;
			return Intersect(nodeGeometry1.m_Left, new float2(0f, 1f), offset1, circle2, bounds2, prefabCompositionData1, areas1, ref intersection) | Intersect(right, new float2(0f, 0.5f), offset1, circle2, bounds2, prefabCompositionData1, areas1, ref intersection) | Intersect(right2, new float2(0.5f, 1f), offset1, circle2, bounds2, prefabCompositionData1, areas1, ref intersection);
		}
		Segment left = nodeGeometry1.m_Left;
		Segment right3 = nodeGeometry1.m_Right;
		left.m_Right = nodeGeometry1.m_Middle;
		right3.m_Left = nodeGeometry1.m_Middle;
		return Intersect(nodeGeometry1.m_Left, new float2(0f, 0.5f), offset1, circle2, bounds2, prefabCompositionData1, areas1, ref intersection) | Intersect(nodeGeometry1.m_Right, new float2(0.5f, 1f), offset1, circle2, bounds2, prefabCompositionData1, areas1, ref intersection) | Intersect(left, new float2(0f, 0.5f), offset1, circle2, bounds2, prefabCompositionData1, areas1, ref intersection) | Intersect(right3, new float2(0.5f, 1f), offset1, circle2, bounds2, prefabCompositionData1, areas1, ref intersection);
	}

	public static bool Intersect(Segment segment1, float2 segmentSide1, float3 offset1, Cylinder3 cylinder2, Bounds3 bounds2, NetCompositionData prefabCompositionData1, DynamicBuffer<NetCompositionArea> areas1, ref Bounds3 intersection)
	{
		if (!MathUtils.Intersect(SetHeightRange(MathUtils.Bounds(segment1.m_Left) | MathUtils.Bounds(segment1.m_Right), prefabCompositionData1.m_HeightRange), bounds2))
		{
			return false;
		}
		if (areas1.IsCreated)
		{
			for (int i = 0; i < areas1.Length; i++)
			{
				NetCompositionArea netCompositionArea = areas1[i];
				if ((netCompositionArea.m_Flags & (NetAreaFlags.Buildable | NetAreaFlags.Hole)) == 0)
				{
					continue;
				}
				float num = netCompositionArea.m_Width * 0.51f;
				if (cylinder2.circle.radius >= num)
				{
					continue;
				}
				float num2 = netCompositionArea.m_Position.x / prefabCompositionData1.m_Width + 0.5f;
				if (num2 < segmentSide1.x || num2 > segmentSide1.y)
				{
					continue;
				}
				Bezier4x3 curve = MathUtils.Lerp(segment1.m_Left, segment1.m_Right, (num2 - segmentSide1.x) / (segmentSide1.y - segmentSide1.x));
				Bounds3 bounds3 = MathUtils.Bounds(curve);
				bounds3.min.xz -= num;
				bounds3.max.xz += num;
				if (!MathUtils.Intersect(SetHeightRange(bounds3, prefabCompositionData1.m_HeightRange), bounds2))
				{
					continue;
				}
				curve += offset1;
				float3 @float = math.mul(cylinder2.rotation, new float3(cylinder2.circle.position.x, cylinder2.height.min, cylinder2.circle.position.y));
				float t;
				float num3 = MathUtils.Distance(curve.xz, @float.xz, out t);
				if ((netCompositionArea.m_Flags & NetAreaFlags.Hole) == 0)
				{
					float3 float2 = MathUtils.Position(curve, t);
					if (bounds2.min.y + offset1.y <= float2.y + prefabCompositionData1.m_HeightRange.min)
					{
						continue;
					}
				}
				if (t == 0f)
				{
					float num4 = math.dot(math.normalizesafe(MathUtils.StartTangent(curve).xz), curve.a.xz - @float.xz);
					if (!(num4 >= cylinder2.circle.radius) && num * num - 2f * num * math.sqrt(math.max(0f, num3 * num3 - num4 * num4)) + num3 * num3 > cylinder2.circle.radius * cylinder2.circle.radius)
					{
						return false;
					}
				}
				else if (t == 1f)
				{
					float num5 = math.dot(math.normalizesafe(MathUtils.EndTangent(curve).xz), @float.xz - curve.d.xz);
					if (!(num5 >= cylinder2.circle.radius) && num * num - 2f * num * math.sqrt(math.max(0f, num3 * num3 - num5 * num5)) + num3 * num3 > cylinder2.circle.radius * cylinder2.circle.radius)
					{
						return false;
					}
				}
				else if (num3 < num - cylinder2.circle.radius)
				{
					return false;
				}
			}
		}
		bool result = false;
		Quad3 quad = default(Quad3);
		quad.a = segment1.m_Left.a;
		quad.b = segment1.m_Right.a;
		Bounds3 bounds4 = SetHeightRange(MathUtils.Bounds(quad.a, quad.b), prefabCompositionData1.m_HeightRange);
		for (int j = 1; j <= 8; j++)
		{
			float t2 = (float)j / 8f;
			quad.d = MathUtils.Position(segment1.m_Left, t2);
			quad.c = MathUtils.Position(segment1.m_Right, t2);
			Bounds3 bounds5 = SetHeightRange(MathUtils.Bounds(quad.d, quad.c), prefabCompositionData1.m_HeightRange);
			if (MathUtils.Intersect(bounds4 | bounds5, bounds2))
			{
				Quad3 quad2 = quad;
				quad2.a += offset1;
				quad2.b += offset1;
				quad2.c += offset1;
				quad2.d += offset1;
				if (QuadCylinderIntersect(quad2, cylinder2, out var intersection2, out var intersection3))
				{
					intersection2 = SetHeightRange(intersection2, prefabCompositionData1.m_HeightRange);
					if (MathUtils.Intersect(intersection2, intersection3, out var intersection4))
					{
						result = true;
						intersection |= intersection4;
					}
				}
			}
			quad.a = quad.d;
			quad.b = quad.c;
			bounds4 = bounds5;
		}
		return result;
	}

	public static bool Intersect(Segment segment1, float2 segmentSide1, float2 offset1, Circle2 circle2, Bounds2 bounds2, NetCompositionData prefabCompositionData1, DynamicBuffer<NetCompositionArea> areas1, ref Bounds2 intersection)
	{
		if (!MathUtils.Intersect((MathUtils.Bounds(segment1.m_Left) | MathUtils.Bounds(segment1.m_Right)).xz, bounds2))
		{
			return false;
		}
		if (areas1.IsCreated)
		{
			for (int i = 0; i < areas1.Length; i++)
			{
				NetCompositionArea netCompositionArea = areas1[i];
				if ((netCompositionArea.m_Flags & (NetAreaFlags.Buildable | NetAreaFlags.Hole)) == 0)
				{
					continue;
				}
				float num = netCompositionArea.m_Width * 0.51f;
				if (circle2.radius >= num)
				{
					continue;
				}
				float num2 = netCompositionArea.m_Position.x / prefabCompositionData1.m_Width + 0.5f;
				if (num2 < segmentSide1.x || num2 > segmentSide1.y)
				{
					continue;
				}
				Bezier4x2 xz = MathUtils.Lerp(segment1.m_Left, segment1.m_Right, (num2 - segmentSide1.x) / (segmentSide1.y - segmentSide1.x)).xz;
				Bounds2 bounds3 = MathUtils.Bounds(xz);
				bounds3.min -= num;
				bounds3.max += num;
				if (!MathUtils.Intersect(bounds3, bounds2))
				{
					continue;
				}
				xz += offset1;
				float t;
				float num3 = MathUtils.Distance(xz, circle2.position, out t);
				if (t == 0f)
				{
					float num4 = math.dot(math.normalizesafe(MathUtils.StartTangent(xz)), xz.a - circle2.position);
					if (!(num4 >= circle2.radius) && num * num - 2f * num * math.sqrt(math.max(0f, num3 * num3 - num4 * num4)) + num3 * num3 > circle2.radius * circle2.radius)
					{
						return false;
					}
				}
				else if (t == 1f)
				{
					float num5 = math.dot(math.normalizesafe(MathUtils.EndTangent(xz)), circle2.position - xz.d);
					if (!(num5 >= circle2.radius) && num * num - 2f * num * math.sqrt(math.max(0f, num3 * num3 - num5 * num5)) + num3 * num3 > circle2.radius * circle2.radius)
					{
						return false;
					}
				}
				else if (num3 < num - circle2.radius)
				{
					return false;
				}
			}
		}
		bool result = false;
		Quad2 quad = default(Quad2);
		quad.a = segment1.m_Left.a.xz;
		quad.b = segment1.m_Right.a.xz;
		Bounds2 bounds4 = MathUtils.Bounds(quad.a, quad.b);
		for (int j = 1; j <= 8; j++)
		{
			float t2 = (float)j / 8f;
			quad.d = MathUtils.Position(segment1.m_Left, t2).xz;
			quad.c = MathUtils.Position(segment1.m_Right, t2).xz;
			Bounds2 bounds5 = MathUtils.Bounds(quad.d, quad.c);
			if (MathUtils.Intersect(bounds4 | bounds5, bounds2))
			{
				Quad2 quad2 = quad;
				quad2.a += offset1;
				quad2.b += offset1;
				quad2.c += offset1;
				quad2.d += offset1;
				if (MathUtils.Intersect(quad2, circle2, out var intersection2))
				{
					result = true;
					intersection |= intersection2;
				}
			}
			quad.a = quad.d;
			quad.b = quad.c;
			bounds4 = bounds5;
		}
		return result;
	}

	public static bool QuadCylinderIntersect(Quad3 quad1, Cylinder3 cylinder2, out Bounds3 intersection1, out Bounds3 intersection2)
	{
		intersection1.min = float.MaxValue;
		intersection1.max = float.MinValue;
		intersection2.min = float.MaxValue;
		intersection2.max = float.MinValue;
		Line3.Segment line = new Line3.Segment(quad1.a, quad1.b);
		Line3.Segment line2 = new Line3.Segment(quad1.b, quad1.c);
		Line3.Segment line3 = new Line3.Segment(quad1.c, quad1.d);
		Line3.Segment line4 = new Line3.Segment(quad1.d, quad1.a);
		float3 @float = math.mul(cylinder2.rotation, new float3(cylinder2.circle.position.x, cylinder2.height.min, cylinder2.circle.position.y));
		float3 float2 = math.mul(cylinder2.rotation, new float3(cylinder2.circle.position.x, cylinder2.height.max, cylinder2.circle.position.y));
		Circle2 circle = cylinder2.circle;
		circle.position = @float.xz;
		Bounds1 height = MathUtils.Bounds(@float.y, float2.y);
		Line3 line5 = default(Line3);
		line5.a = new float3(circle.position.x, height.min, circle.position.y);
		line5.b = new float3(circle.position.x, height.max, circle.position.y);
		return QuadCylinderIntersectHelper(line, circle, height, ref intersection1, ref intersection2) | QuadCylinderIntersectHelper(line2, circle, height, ref intersection1, ref intersection2) | QuadCylinderIntersectHelper(line3, circle, height, ref intersection1, ref intersection2) | QuadCylinderIntersectHelper(line4, circle, height, ref intersection1, ref intersection2) | QuadCylinderIntersectHelper(quad1, line5, ref intersection1, ref intersection2);
	}

	public static bool TriangleCylinderIntersect(Triangle3 triangle1, Cylinder3 cylinder2, out Bounds3 intersection1, out Bounds3 intersection2)
	{
		intersection1.min = float.MaxValue;
		intersection1.max = float.MinValue;
		intersection2.min = float.MaxValue;
		intersection2.max = float.MinValue;
		Line3.Segment line = new Line3.Segment(triangle1.a, triangle1.b);
		Line3.Segment line2 = new Line3.Segment(triangle1.b, triangle1.c);
		Line3.Segment line3 = new Line3.Segment(triangle1.c, triangle1.a);
		float3 @float = math.mul(cylinder2.rotation, new float3(cylinder2.circle.position.x, cylinder2.height.min, cylinder2.circle.position.y));
		float3 float2 = math.mul(cylinder2.rotation, new float3(cylinder2.circle.position.x, cylinder2.height.max, cylinder2.circle.position.y));
		Circle2 circle = cylinder2.circle;
		circle.position = @float.xz;
		Bounds1 height = MathUtils.Bounds(@float.y, float2.y);
		Line3 line4 = default(Line3);
		line4.a = new float3(circle.position.x, height.min, circle.position.y);
		line4.b = new float3(circle.position.x, height.max, circle.position.y);
		return QuadCylinderIntersectHelper(line, circle, height, ref intersection1, ref intersection2) | QuadCylinderIntersectHelper(line2, circle, height, ref intersection1, ref intersection2) | QuadCylinderIntersectHelper(line3, circle, height, ref intersection1, ref intersection2) | TriangleCylinderIntersectHelper(triangle1, line4, ref intersection1, ref intersection2);
	}

	private static bool QuadCylinderIntersectHelper(Line3.Segment line1, Circle2 circle2, Bounds1 height2, ref Bounds3 intersection1, ref Bounds3 intersection2)
	{
		if (MathUtils.Intersect(circle2, line1.xz, out var t))
		{
			float3 @float = MathUtils.Position(line1, t.x);
			float3 float2 = MathUtils.Position(line1, t.y);
			intersection1 |= @float;
			intersection1 |= float2;
			@float.y = height2.min;
			float2.y = height2.max;
			intersection2 |= @float;
			intersection2 |= float2;
			return true;
		}
		return false;
	}

	private static bool QuadCylinderIntersectHelper(Quad3 quad1, Line3 line2, ref Bounds3 intersection1, ref Bounds3 intersection2)
	{
		if (MathUtils.Intersect(quad1, line2, out var t))
		{
			intersection1 |= MathUtils.Position(line2, t);
			intersection2 |= MathUtils.Position(line2, math.saturate(t));
			return true;
		}
		return false;
	}

	private static bool TriangleCylinderIntersectHelper(Triangle3 triangle1, Line3 line2, ref Bounds3 intersection1, ref Bounds3 intersection2)
	{
		if (MathUtils.Intersect(triangle1, line2, out var t))
		{
			intersection1 |= MathUtils.Position(line2, t.z);
			intersection2 |= MathUtils.Position(line2, math.saturate(t.z));
			return true;
		}
		return false;
	}

	public static bool Intersect(Edge edge1, Edge edge2, EdgeGeometry edgeGeometry1, EdgeGeometry edgeGeometry2, NetCompositionData prefabCompositionData1, NetCompositionData prefabCompositionData2, ref Bounds3 intersection, ref bool clearanceOnly)
	{
		if (!MathUtils.Intersect(edgeGeometry1.m_Bounds, edgeGeometry2.m_Bounds))
		{
			return false;
		}
		if (math.all(edgeGeometry1.m_Start.m_Length + edgeGeometry1.m_End.m_Length <= 0.1f))
		{
			return false;
		}
		if (math.all(edgeGeometry2.m_Start.m_Length + edgeGeometry2.m_End.m_Length <= 0.1f))
		{
			return false;
		}
		bool flag = false;
		if (edge1.m_Start != edge2.m_Start)
		{
			flag |= Intersect(edgeGeometry1.m_Start, edgeGeometry2.m_Start, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly);
		}
		if (edge1.m_Start != edge2.m_End)
		{
			flag |= Intersect(edgeGeometry1.m_Start, edgeGeometry2.m_End, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly);
		}
		if (edge1.m_End != edge2.m_Start)
		{
			flag |= Intersect(edgeGeometry1.m_End, edgeGeometry2.m_Start, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly);
		}
		if (edge1.m_End != edge2.m_End)
		{
			flag |= Intersect(edgeGeometry1.m_End, edgeGeometry2.m_End, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly);
		}
		return flag;
	}

	public static bool Intersect(Edge edge1, Edge edge2, EdgeGeometry edgeGeometry1, EdgeGeometry edgeGeometry2, NetCompositionData prefabCompositionData1, NetCompositionData prefabCompositionData2, ref Bounds2 intersection)
	{
		if (!MathUtils.Intersect(edgeGeometry1.m_Bounds.xz, edgeGeometry2.m_Bounds.xz))
		{
			return false;
		}
		if (math.all(edgeGeometry1.m_Start.m_Length + edgeGeometry1.m_End.m_Length <= 0.1f))
		{
			return false;
		}
		if (math.all(edgeGeometry2.m_Start.m_Length + edgeGeometry2.m_End.m_Length <= 0.1f))
		{
			return false;
		}
		bool flag = false;
		if (edge1.m_Start != edge2.m_Start)
		{
			flag |= Intersect(edgeGeometry1.m_Start, edgeGeometry2.m_Start, prefabCompositionData1, prefabCompositionData2, ref intersection);
		}
		if (edge1.m_Start != edge2.m_End)
		{
			flag |= Intersect(edgeGeometry1.m_Start, edgeGeometry2.m_End, prefabCompositionData1, prefabCompositionData2, ref intersection);
		}
		if (edge1.m_End != edge2.m_Start)
		{
			flag |= Intersect(edgeGeometry1.m_End, edgeGeometry2.m_Start, prefabCompositionData1, prefabCompositionData2, ref intersection);
		}
		if (edge1.m_End != edge2.m_End)
		{
			flag |= Intersect(edgeGeometry1.m_End, edgeGeometry2.m_End, prefabCompositionData1, prefabCompositionData2, ref intersection);
		}
		return flag;
	}

	public static bool Intersect(Edge edge1, Edge originalEdge1, NativeArray<ConnectedNode> nodes1, NativeArray<ConnectedNode> originalNodes1, Entity node2, EdgeGeometry edgeGeometry1, EdgeNodeGeometry nodeGeometry2, NetCompositionData prefabCompositionData1, NetCompositionData prefabCompositionData2, ref Bounds3 intersection, ref bool clearanceOnly)
	{
		if (!MathUtils.Intersect(edgeGeometry1.m_Bounds, nodeGeometry2.m_Bounds))
		{
			return false;
		}
		if (math.all(edgeGeometry1.m_Start.m_Length + edgeGeometry1.m_End.m_Length <= 0.1f))
		{
			return false;
		}
		if (math.all(nodeGeometry2.m_Left.m_Length <= 0.05f) && math.all(nodeGeometry2.m_Left.m_Length <= 0.05f))
		{
			return false;
		}
		for (int i = 0; i < nodes1.Length; i++)
		{
			if (nodes1[i].m_Node == node2)
			{
				return false;
			}
		}
		for (int j = 0; j < originalNodes1.Length; j++)
		{
			if (originalNodes1[j].m_Node == node2)
			{
				return false;
			}
		}
		bool flag = false;
		if (nodeGeometry2.m_MiddleRadius > 0f)
		{
			Segment right = nodeGeometry2.m_Right;
			Segment right2 = nodeGeometry2.m_Right;
			right.m_Right = MathUtils.Lerp(nodeGeometry2.m_Right.m_Left, nodeGeometry2.m_Right.m_Right, 0.5f);
			right.m_Right.d = nodeGeometry2.m_Middle.d;
			right2.m_Left = right.m_Right;
			if (edge1.m_Start != node2 && originalEdge1.m_Start != node2)
			{
				flag |= Intersect(edgeGeometry1.m_Start, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(edgeGeometry1.m_Start, right, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(edgeGeometry1.m_Start, right2, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly);
			}
			if (edge1.m_End != node2 && originalEdge1.m_End != node2)
			{
				flag |= Intersect(edgeGeometry1.m_End, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(edgeGeometry1.m_End, right, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(edgeGeometry1.m_End, right2, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly);
			}
		}
		else
		{
			Segment left = nodeGeometry2.m_Left;
			Segment right3 = nodeGeometry2.m_Right;
			left.m_Right = nodeGeometry2.m_Middle;
			right3.m_Left = nodeGeometry2.m_Middle;
			if (edge1.m_Start != node2 && originalEdge1.m_Start != node2)
			{
				flag |= Intersect(edgeGeometry1.m_Start, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(edgeGeometry1.m_Start, nodeGeometry2.m_Right, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(edgeGeometry1.m_Start, left, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(edgeGeometry1.m_Start, right3, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly);
			}
			if (edge1.m_End != node2 && originalEdge1.m_End != node2)
			{
				flag |= Intersect(edgeGeometry1.m_End, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(edgeGeometry1.m_End, nodeGeometry2.m_Right, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(edgeGeometry1.m_End, left, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(edgeGeometry1.m_End, right3, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly);
			}
		}
		return flag;
	}

	public static bool Intersect(Edge edge1, NativeArray<ConnectedNode> nodes1, Entity node2, Entity originalNode2, EdgeGeometry edgeGeometry1, EdgeNodeGeometry nodeGeometry2, NetCompositionData prefabCompositionData1, NetCompositionData prefabCompositionData2, ref Bounds3 intersection, ref bool clearanceOnly)
	{
		if (!MathUtils.Intersect(edgeGeometry1.m_Bounds, nodeGeometry2.m_Bounds))
		{
			return false;
		}
		if (math.all(edgeGeometry1.m_Start.m_Length + edgeGeometry1.m_End.m_Length <= 0.1f))
		{
			return false;
		}
		if (math.all(nodeGeometry2.m_Left.m_Length <= 0.05f) && math.all(nodeGeometry2.m_Left.m_Length <= 0.05f))
		{
			return false;
		}
		for (int i = 0; i < nodes1.Length; i++)
		{
			Entity node3 = nodes1[i].m_Node;
			if (node3 == node2 || node3 == originalNode2)
			{
				return false;
			}
		}
		bool flag = false;
		if (nodeGeometry2.m_MiddleRadius > 0f)
		{
			Segment right = nodeGeometry2.m_Right;
			Segment right2 = nodeGeometry2.m_Right;
			right.m_Right = MathUtils.Lerp(nodeGeometry2.m_Right.m_Left, nodeGeometry2.m_Right.m_Right, 0.5f);
			right.m_Right.d = nodeGeometry2.m_Middle.d;
			right2.m_Left = right.m_Right;
			if (edge1.m_Start != node2 && edge1.m_Start != originalNode2)
			{
				flag |= Intersect(edgeGeometry1.m_Start, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(edgeGeometry1.m_Start, right, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(edgeGeometry1.m_Start, right2, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly);
			}
			if (edge1.m_End != node2 && edge1.m_End != originalNode2)
			{
				flag |= Intersect(edgeGeometry1.m_End, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(edgeGeometry1.m_End, right, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(edgeGeometry1.m_End, right2, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly);
			}
		}
		else
		{
			Segment left = nodeGeometry2.m_Left;
			Segment right3 = nodeGeometry2.m_Right;
			left.m_Right = nodeGeometry2.m_Middle;
			right3.m_Left = nodeGeometry2.m_Middle;
			if (edge1.m_Start != node2 && edge1.m_Start != originalNode2)
			{
				flag |= Intersect(edgeGeometry1.m_Start, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(edgeGeometry1.m_Start, nodeGeometry2.m_Right, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(edgeGeometry1.m_Start, left, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(edgeGeometry1.m_Start, right3, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly);
			}
			if (edge1.m_End != node2 && edge1.m_End != originalNode2)
			{
				flag |= Intersect(edgeGeometry1.m_End, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(edgeGeometry1.m_End, nodeGeometry2.m_Right, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(edgeGeometry1.m_End, left, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(edgeGeometry1.m_End, right3, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly);
			}
		}
		return flag;
	}

	public static bool Intersect(Edge edge1, Edge originalEdge1, NativeArray<ConnectedNode> nodes1, NativeArray<ConnectedNode> originalNodes1, Entity node2, EdgeGeometry edgeGeometry1, EdgeNodeGeometry nodeGeometry2, NetCompositionData prefabCompositionData1, NetCompositionData prefabCompositionData2, ref Bounds2 intersection)
	{
		if (!MathUtils.Intersect(edgeGeometry1.m_Bounds.xz, nodeGeometry2.m_Bounds.xz))
		{
			return false;
		}
		if (math.all(edgeGeometry1.m_Start.m_Length + edgeGeometry1.m_End.m_Length <= 0.1f))
		{
			return false;
		}
		if (math.all(nodeGeometry2.m_Left.m_Length <= 0.05f) && math.all(nodeGeometry2.m_Left.m_Length <= 0.05f))
		{
			return false;
		}
		for (int i = 0; i < nodes1.Length; i++)
		{
			if (nodes1[i].m_Node == node2)
			{
				return false;
			}
		}
		for (int j = 0; j < originalNodes1.Length; j++)
		{
			if (originalNodes1[j].m_Node == node2)
			{
				return false;
			}
		}
		bool flag = false;
		if (nodeGeometry2.m_MiddleRadius > 0f)
		{
			Segment right = nodeGeometry2.m_Right;
			Segment right2 = nodeGeometry2.m_Right;
			right.m_Right = MathUtils.Lerp(nodeGeometry2.m_Right.m_Left, nodeGeometry2.m_Right.m_Right, 0.5f);
			right.m_Right.d = nodeGeometry2.m_Middle.d;
			right2.m_Left = right.m_Right;
			if (edge1.m_Start != node2 && originalEdge1.m_Start != node2)
			{
				flag |= Intersect(edgeGeometry1.m_Start, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(edgeGeometry1.m_Start, right, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(edgeGeometry1.m_Start, right2, prefabCompositionData1, prefabCompositionData2, ref intersection);
			}
			if (edge1.m_End != node2 && originalEdge1.m_End != node2)
			{
				flag |= Intersect(edgeGeometry1.m_End, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(edgeGeometry1.m_End, right, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(edgeGeometry1.m_End, right2, prefabCompositionData1, prefabCompositionData2, ref intersection);
			}
		}
		else
		{
			Segment left = nodeGeometry2.m_Left;
			Segment right3 = nodeGeometry2.m_Right;
			left.m_Right = nodeGeometry2.m_Middle;
			right3.m_Left = nodeGeometry2.m_Middle;
			if (edge1.m_Start != node2 && originalEdge1.m_Start != node2)
			{
				flag |= Intersect(edgeGeometry1.m_Start, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(edgeGeometry1.m_Start, nodeGeometry2.m_Right, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(edgeGeometry1.m_Start, left, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(edgeGeometry1.m_Start, right3, prefabCompositionData1, prefabCompositionData2, ref intersection);
			}
			if (edge1.m_End != node2 && originalEdge1.m_End != node2)
			{
				flag |= Intersect(edgeGeometry1.m_End, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(edgeGeometry1.m_End, nodeGeometry2.m_Right, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(edgeGeometry1.m_End, left, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(edgeGeometry1.m_End, right3, prefabCompositionData1, prefabCompositionData2, ref intersection);
			}
		}
		return flag;
	}

	public static bool Intersect(Edge edge1, NativeArray<ConnectedNode> nodes1, Entity node2, Entity originalNode2, EdgeGeometry edgeGeometry1, EdgeNodeGeometry nodeGeometry2, NetCompositionData prefabCompositionData1, NetCompositionData prefabCompositionData2, ref Bounds2 intersection)
	{
		if (!MathUtils.Intersect(edgeGeometry1.m_Bounds.xz, nodeGeometry2.m_Bounds.xz))
		{
			return false;
		}
		if (math.all(edgeGeometry1.m_Start.m_Length + edgeGeometry1.m_End.m_Length <= 0.1f))
		{
			return false;
		}
		if (math.all(nodeGeometry2.m_Left.m_Length <= 0.05f) && math.all(nodeGeometry2.m_Left.m_Length <= 0.05f))
		{
			return false;
		}
		for (int i = 0; i < nodes1.Length; i++)
		{
			Entity node3 = nodes1[i].m_Node;
			if (node3 == node2 || node3 == originalNode2)
			{
				return false;
			}
		}
		bool flag = false;
		if (nodeGeometry2.m_MiddleRadius > 0f)
		{
			Segment right = nodeGeometry2.m_Right;
			Segment right2 = nodeGeometry2.m_Right;
			right.m_Right = MathUtils.Lerp(nodeGeometry2.m_Right.m_Left, nodeGeometry2.m_Right.m_Right, 0.5f);
			right.m_Right.d = nodeGeometry2.m_Middle.d;
			right2.m_Left = right.m_Right;
			if (edge1.m_Start != node2 && edge1.m_Start != originalNode2)
			{
				flag |= Intersect(edgeGeometry1.m_Start, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(edgeGeometry1.m_Start, right, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(edgeGeometry1.m_Start, right2, prefabCompositionData1, prefabCompositionData2, ref intersection);
			}
			if (edge1.m_End != node2 && edge1.m_End != originalNode2)
			{
				flag |= Intersect(edgeGeometry1.m_End, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(edgeGeometry1.m_End, right, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(edgeGeometry1.m_End, right2, prefabCompositionData1, prefabCompositionData2, ref intersection);
			}
		}
		else
		{
			Segment left = nodeGeometry2.m_Left;
			Segment right3 = nodeGeometry2.m_Right;
			left.m_Right = nodeGeometry2.m_Middle;
			right3.m_Left = nodeGeometry2.m_Middle;
			if (edge1.m_Start != node2 && edge1.m_Start != originalNode2)
			{
				flag |= Intersect(edgeGeometry1.m_Start, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(edgeGeometry1.m_Start, nodeGeometry2.m_Right, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(edgeGeometry1.m_Start, left, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(edgeGeometry1.m_Start, right3, prefabCompositionData1, prefabCompositionData2, ref intersection);
			}
			if (edge1.m_End != node2 && edge1.m_End != originalNode2)
			{
				flag |= Intersect(edgeGeometry1.m_End, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(edgeGeometry1.m_End, nodeGeometry2.m_Right, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(edgeGeometry1.m_End, left, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(edgeGeometry1.m_End, right3, prefabCompositionData1, prefabCompositionData2, ref intersection);
			}
		}
		return flag;
	}

	public static bool Intersect(Entity node1, Entity originalNode1, NativeArray<ConnectedNode> nodes1, NativeArray<ConnectedNode> originalNodes1, Entity node2, NativeArray<ConnectedNode> nodes2, EdgeNodeGeometry nodeGeometry1, EdgeNodeGeometry nodeGeometry2, NetCompositionData prefabCompositionData1, NetCompositionData prefabCompositionData2, ref Bounds3 intersection, ref bool clearanceOnly)
	{
		if (!MathUtils.Intersect(nodeGeometry1.m_Bounds, nodeGeometry2.m_Bounds))
		{
			return false;
		}
		if (node1 == node2 || originalNode1 == node2)
		{
			return false;
		}
		if (math.all(nodeGeometry1.m_Left.m_Length <= 0.05f) && math.all(nodeGeometry1.m_Left.m_Length <= 0.05f))
		{
			return false;
		}
		if (math.all(nodeGeometry2.m_Left.m_Length <= 0.05f) && math.all(nodeGeometry2.m_Left.m_Length <= 0.05f))
		{
			return false;
		}
		for (int i = 0; i < nodes1.Length; i++)
		{
			if (nodes1[i].m_Node == node2)
			{
				return false;
			}
		}
		for (int j = 0; j < originalNodes1.Length; j++)
		{
			if (originalNodes1[j].m_Node == node2)
			{
				return false;
			}
		}
		for (int k = 0; k < nodes2.Length; k++)
		{
			Entity node3 = nodes2[k].m_Node;
			if (node3 == node1 || node3 == originalNode1)
			{
				return false;
			}
		}
		Segment segment;
		Segment right;
		if (nodeGeometry1.m_MiddleRadius > 0f)
		{
			segment = nodeGeometry1.m_Right;
			right = nodeGeometry1.m_Right;
			segment.m_Right = MathUtils.Lerp(nodeGeometry1.m_Right.m_Left, nodeGeometry1.m_Right.m_Right, 0.5f);
			segment.m_Right.d = nodeGeometry1.m_Middle.d;
			right.m_Left = segment.m_Right;
		}
		else
		{
			segment = nodeGeometry1.m_Left;
			right = nodeGeometry1.m_Right;
			segment.m_Right = nodeGeometry1.m_Middle;
			right.m_Left = nodeGeometry1.m_Middle;
		}
		Segment segment2;
		Segment right2;
		if (nodeGeometry2.m_MiddleRadius > 0f)
		{
			segment2 = nodeGeometry2.m_Right;
			right2 = nodeGeometry2.m_Right;
			segment2.m_Right = MathUtils.Lerp(nodeGeometry2.m_Right.m_Left, nodeGeometry2.m_Right.m_Right, 0.5f);
			segment2.m_Right.d = nodeGeometry2.m_Middle.d;
			right2.m_Left = segment2.m_Right;
		}
		else
		{
			segment2 = nodeGeometry2.m_Left;
			right2 = nodeGeometry2.m_Right;
			segment2.m_Right = nodeGeometry2.m_Middle;
			right2.m_Left = nodeGeometry2.m_Middle;
		}
		bool flag = Intersect(nodeGeometry1.m_Left, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(nodeGeometry1.m_Left, segment2, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(nodeGeometry1.m_Left, right2, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(segment, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(segment, segment2, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(segment, right2, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(right, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(right, segment2, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(right, right2, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly);
		if (nodeGeometry1.m_MiddleRadius <= 0f)
		{
			flag |= Intersect(nodeGeometry1.m_Right, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(nodeGeometry1.m_Right, segment2, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(nodeGeometry1.m_Right, right2, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly);
		}
		if (nodeGeometry2.m_MiddleRadius <= 0f)
		{
			flag |= Intersect(nodeGeometry1.m_Left, nodeGeometry2.m_Right, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(segment, nodeGeometry2.m_Right, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly) | Intersect(right, nodeGeometry2.m_Right, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly);
		}
		if (nodeGeometry1.m_MiddleRadius <= 0f && nodeGeometry2.m_MiddleRadius <= 0f)
		{
			flag |= Intersect(nodeGeometry1.m_Right, nodeGeometry2.m_Right, prefabCompositionData1, prefabCompositionData2, ref intersection, ref clearanceOnly);
		}
		return flag;
	}

	public static bool Intersect(Entity node1, Entity originalNode1, NativeArray<ConnectedNode> nodes1, NativeArray<ConnectedNode> originalNodes1, Entity node2, NativeArray<ConnectedNode> nodes2, EdgeNodeGeometry nodeGeometry1, EdgeNodeGeometry nodeGeometry2, NetCompositionData prefabCompositionData1, NetCompositionData prefabCompositionData2, ref Bounds2 intersection)
	{
		if (!MathUtils.Intersect(nodeGeometry1.m_Bounds.xz, nodeGeometry2.m_Bounds.xz))
		{
			return false;
		}
		if (node1 == node2 || originalNode1 == node2)
		{
			return false;
		}
		if (math.all(nodeGeometry1.m_Left.m_Length <= 0.05f) && math.all(nodeGeometry1.m_Left.m_Length <= 0.05f))
		{
			return false;
		}
		if (math.all(nodeGeometry2.m_Left.m_Length <= 0.05f) && math.all(nodeGeometry2.m_Left.m_Length <= 0.05f))
		{
			return false;
		}
		for (int i = 0; i < nodes1.Length; i++)
		{
			if (nodes1[i].m_Node == node2)
			{
				return false;
			}
		}
		for (int j = 0; j < originalNodes1.Length; j++)
		{
			if (originalNodes1[j].m_Node == node2)
			{
				return false;
			}
		}
		for (int k = 0; k < nodes2.Length; k++)
		{
			Entity node3 = nodes2[k].m_Node;
			if (node3 == node1 || node3 == originalNode1)
			{
				return false;
			}
		}
		Segment segment;
		Segment right;
		if (nodeGeometry1.m_MiddleRadius > 0f)
		{
			segment = nodeGeometry1.m_Right;
			right = nodeGeometry1.m_Right;
			segment.m_Right = MathUtils.Lerp(nodeGeometry1.m_Right.m_Left, nodeGeometry1.m_Right.m_Right, 0.5f);
			segment.m_Right.d = nodeGeometry1.m_Middle.d;
			right.m_Left = segment.m_Right;
		}
		else
		{
			segment = nodeGeometry1.m_Left;
			right = nodeGeometry1.m_Right;
			segment.m_Right = nodeGeometry1.m_Middle;
			right.m_Left = nodeGeometry1.m_Middle;
		}
		Segment segment2;
		Segment right2;
		if (nodeGeometry2.m_MiddleRadius > 0f)
		{
			segment2 = nodeGeometry2.m_Right;
			right2 = nodeGeometry2.m_Right;
			segment2.m_Right = MathUtils.Lerp(nodeGeometry2.m_Right.m_Left, nodeGeometry2.m_Right.m_Right, 0.5f);
			segment2.m_Right.d = nodeGeometry2.m_Middle.d;
			right2.m_Left = segment2.m_Right;
		}
		else
		{
			segment2 = nodeGeometry2.m_Left;
			right2 = nodeGeometry2.m_Right;
			segment2.m_Right = nodeGeometry2.m_Middle;
			right2.m_Left = nodeGeometry2.m_Middle;
		}
		bool flag = Intersect(nodeGeometry1.m_Left, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(nodeGeometry1.m_Left, segment2, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(nodeGeometry1.m_Left, right2, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(segment, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(segment, segment2, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(segment, right2, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(right, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(right, segment2, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(right, right2, prefabCompositionData1, prefabCompositionData2, ref intersection);
		if (nodeGeometry1.m_MiddleRadius <= 0f)
		{
			flag |= Intersect(nodeGeometry1.m_Right, nodeGeometry2.m_Left, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(nodeGeometry1.m_Right, segment2, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(nodeGeometry1.m_Right, right2, prefabCompositionData1, prefabCompositionData2, ref intersection);
		}
		if (nodeGeometry2.m_MiddleRadius <= 0f)
		{
			flag |= Intersect(nodeGeometry1.m_Left, nodeGeometry2.m_Right, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(segment, nodeGeometry2.m_Right, prefabCompositionData1, prefabCompositionData2, ref intersection) | Intersect(right, nodeGeometry2.m_Right, prefabCompositionData1, prefabCompositionData2, ref intersection);
		}
		if (nodeGeometry1.m_MiddleRadius <= 0f && nodeGeometry2.m_MiddleRadius <= 0f)
		{
			flag |= Intersect(nodeGeometry1.m_Right, nodeGeometry2.m_Right, prefabCompositionData1, prefabCompositionData2, ref intersection);
		}
		return flag;
	}

	public static bool Intersect(Segment segment1, float2 segmentSide1, Triangle3 triangle2, NetCompositionData prefabCompositionData1, Bounds1 heightRange2, ref Bounds3 intersection)
	{
		Bounds3 bounds = SetHeightRange(MathUtils.Bounds(segment1.m_Left) | MathUtils.Bounds(segment1.m_Right), prefabCompositionData1.m_HeightRange);
		Bounds3 bounds2 = SetHeightRange(MathUtils.Bounds(triangle2), heightRange2);
		if (!MathUtils.Intersect(bounds, bounds2))
		{
			return false;
		}
		bool result = false;
		Quad3 quad = default(Quad3);
		quad.a = segment1.m_Left.a;
		quad.b = segment1.m_Right.a;
		Bounds3 bounds3 = SetHeightRange(MathUtils.Bounds(quad.a, quad.b), prefabCompositionData1.m_HeightRange);
		for (int i = 1; i <= 8; i++)
		{
			float t = (float)i / 8f;
			quad.d = MathUtils.Position(segment1.m_Left, t);
			quad.c = MathUtils.Position(segment1.m_Right, t);
			Bounds3 bounds4 = SetHeightRange(MathUtils.Bounds(quad.d, quad.c), prefabCompositionData1.m_HeightRange);
			if (MathUtils.Intersect(bounds3 | bounds4, bounds2))
			{
				Quad3 quad2 = quad;
				float3 @float = math.normalizesafe(quad2.b - quad2.a) * 0.5f;
				float3 float2 = math.normalizesafe(quad2.d - quad2.c) * 0.5f;
				quad2.a += @float;
				quad2.b -= @float;
				quad2.c += float2;
				quad2.d -= float2;
				if (QuadTriangleIntersect(quad2, triangle2, out var intersection2, out var intersection3))
				{
					intersection2 = SetHeightRange(intersection2, prefabCompositionData1.m_HeightRange);
					intersection3 = SetHeightRange(intersection3, heightRange2);
					if (MathUtils.Intersect(intersection2, intersection3, out var intersection4))
					{
						result = true;
						intersection |= intersection4;
					}
				}
			}
			quad.a = quad.d;
			quad.b = quad.c;
			bounds3 = bounds4;
		}
		return result;
	}

	public static bool Intersect(Segment segment1, Segment segment2, NetCompositionData prefabCompositionData1, NetCompositionData prefabCompositionData2, ref Bounds3 intersection, ref bool clearanceOnly)
	{
		Bounds3 bounds = SetHeightRange(MathUtils.Bounds(segment1.m_Left) | MathUtils.Bounds(segment1.m_Right), prefabCompositionData1.m_HeightRange);
		Bounds3 bounds2 = SetHeightRange(MathUtils.Bounds(segment2.m_Left) | MathUtils.Bounds(segment2.m_Right), prefabCompositionData2.m_HeightRange);
		if (!MathUtils.Intersect(bounds, bounds2))
		{
			return false;
		}
		bool result = false;
		Quad3 quad = default(Quad3);
		quad.a = segment1.m_Left.a;
		quad.b = segment1.m_Right.a;
		Bounds3 bounds3 = SetHeightRange(MathUtils.Bounds(quad.a, quad.b), prefabCompositionData1.m_HeightRange);
		Quad3 quad2 = default(Quad3);
		for (int i = 1; i <= 8; i++)
		{
			float t = (float)i / 8f;
			quad.d = MathUtils.Position(segment1.m_Left, t);
			quad.c = MathUtils.Position(segment1.m_Right, t);
			Bounds3 bounds4 = SetHeightRange(MathUtils.Bounds(quad.d, quad.c), prefabCompositionData1.m_HeightRange);
			Bounds3 bounds5 = bounds3 | bounds4;
			if (MathUtils.Intersect(bounds5, bounds2))
			{
				quad2.a = segment2.m_Left.a;
				quad2.b = segment2.m_Right.a;
				Bounds3 bounds6 = SetHeightRange(MathUtils.Bounds(quad2.a, quad2.b), prefabCompositionData2.m_HeightRange);
				for (int j = 1; j <= 8; j++)
				{
					float t2 = (float)j / 8f;
					quad2.d = MathUtils.Position(segment2.m_Left, t2);
					quad2.c = MathUtils.Position(segment2.m_Right, t2);
					Bounds3 bounds7 = SetHeightRange(MathUtils.Bounds(quad2.d, quad2.c), prefabCompositionData2.m_HeightRange);
					Bounds3 bounds8 = bounds6 | bounds7;
					if (MathUtils.Intersect(bounds5, bounds8) && QuadIntersect(quad, quad2, out var intersection2, out var intersection3))
					{
						intersection2 = SetHeightRange(intersection2, prefabCompositionData1.m_HeightRange);
						intersection3 = SetHeightRange(intersection3, prefabCompositionData2.m_HeightRange);
						if (MathUtils.Intersect(intersection2, intersection3, out var intersection4))
						{
							result = true;
							intersection |= intersection4;
							clearanceOnly &= (intersection3.min.y >= intersection2.max.y - prefabCompositionData1.m_HeightRange.max && (prefabCompositionData2.m_Flags.m_General & CompositionFlags.General.Elevated) != 0) || (intersection2.min.y >= intersection3.max.y - prefabCompositionData2.m_HeightRange.max && (prefabCompositionData1.m_Flags.m_General & CompositionFlags.General.Elevated) != 0);
						}
					}
					quad2.a = quad2.d;
					quad2.b = quad2.c;
					bounds6 = bounds7;
				}
			}
			quad.a = quad.d;
			quad.b = quad.c;
			bounds3 = bounds4;
		}
		return result;
	}

	public static bool Intersect(Segment segment1, Segment segment2, NetCompositionData prefabCompositionData1, NetCompositionData prefabCompositionData2, ref Bounds2 intersection)
	{
		Bounds2 xz = (MathUtils.Bounds(segment1.m_Left) | MathUtils.Bounds(segment1.m_Right)).xz;
		Bounds2 xz2 = (MathUtils.Bounds(segment2.m_Left) | MathUtils.Bounds(segment2.m_Right)).xz;
		if (!MathUtils.Intersect(xz, xz2))
		{
			return false;
		}
		bool result = false;
		Quad2 quad = default(Quad2);
		quad.a = segment1.m_Left.a.xz;
		quad.b = segment1.m_Right.a.xz;
		Bounds2 bounds = MathUtils.Bounds(quad.a, quad.b);
		Quad2 quad2 = default(Quad2);
		for (int i = 1; i <= 8; i++)
		{
			float t = (float)i / 8f;
			quad.d = MathUtils.Position(segment1.m_Left, t).xz;
			quad.c = MathUtils.Position(segment1.m_Right, t).xz;
			Bounds2 bounds2 = MathUtils.Bounds(quad.d, quad.c);
			Bounds2 bounds3 = bounds | bounds2;
			if (MathUtils.Intersect(bounds3, xz2))
			{
				quad2.a = segment2.m_Left.a.xz;
				quad2.b = segment2.m_Right.a.xz;
				Bounds2 bounds4 = MathUtils.Bounds(quad2.a, quad2.b);
				for (int j = 1; j <= 8; j++)
				{
					float t2 = (float)j / 8f;
					quad2.d = MathUtils.Position(segment2.m_Left, t2).xz;
					quad2.c = MathUtils.Position(segment2.m_Right, t2).xz;
					Bounds2 bounds5 = MathUtils.Bounds(quad2.d, quad2.c);
					Bounds2 bounds6 = bounds4 | bounds5;
					if (MathUtils.Intersect(bounds3, bounds6) && MathUtils.Intersect(quad, quad2, out var intersection2))
					{
						result = true;
						intersection |= intersection2;
					}
					quad2.a = quad2.d;
					quad2.b = quad2.c;
					bounds4 = bounds5;
				}
			}
			quad.a = quad.d;
			quad.b = quad.c;
			bounds = bounds2;
		}
		return result;
	}

	public static Bounds3 SetHeightRange(Bounds3 bounds, Bounds1 heightRange)
	{
		bounds.min.y += heightRange.min;
		bounds.max.y += heightRange.max;
		return bounds;
	}

	public static bool QuadIntersect(Quad3 quad1, Quad3 quad2, out Bounds3 intersection1, out Bounds3 intersection2)
	{
		intersection1.min = float.MaxValue;
		intersection1.max = float.MinValue;
		intersection2.min = float.MaxValue;
		intersection2.max = float.MinValue;
		Triangle3 triangle = new Triangle3(quad1.a, quad1.d, quad1.c);
		Triangle3 triangle2 = new Triangle3(quad1.c, quad1.b, quad1.a);
		Triangle3 triangle3 = new Triangle3(quad2.a, quad2.d, quad2.c);
		Triangle3 triangle4 = new Triangle3(quad2.c, quad2.b, quad2.a);
		Line3.Segment line = new Line3.Segment(quad1.a, quad1.b);
		Line3.Segment line2 = new Line3.Segment(quad1.b, quad1.c);
		Line3.Segment line3 = new Line3.Segment(quad1.c, quad1.d);
		Line3.Segment line4 = new Line3.Segment(quad1.d, quad1.a);
		return QuadIntersectHelper(triangle, quad2, ref intersection1, ref intersection2) | QuadIntersectHelper(triangle2, quad2, ref intersection1, ref intersection2) | QuadIntersectHelper(triangle3, quad1, ref intersection2, ref intersection1) | QuadIntersectHelper(triangle4, quad1, ref intersection2, ref intersection1) | QuadIntersectHelper(line, quad2, ref intersection1, ref intersection2) | QuadIntersectHelper(line2, quad2, ref intersection1, ref intersection2) | QuadIntersectHelper(line3, quad2, ref intersection1, ref intersection2) | QuadIntersectHelper(line4, quad2, ref intersection1, ref intersection2);
	}

	public static bool QuadTriangleIntersect(Quad3 quad1, Triangle3 triangle2, out Bounds3 intersection1, out Bounds3 intersection2)
	{
		intersection1.min = float.MaxValue;
		intersection1.max = float.MinValue;
		intersection2.min = float.MaxValue;
		intersection2.max = float.MinValue;
		Triangle3 triangle3 = new Triangle3(quad1.a, quad1.d, quad1.c);
		Triangle3 triangle4 = new Triangle3(quad1.c, quad1.b, quad1.a);
		Line3.Segment line = new Line3.Segment(quad1.a, quad1.b);
		Line3.Segment line2 = new Line3.Segment(quad1.b, quad1.c);
		Line3.Segment line3 = new Line3.Segment(quad1.c, quad1.d);
		Line3.Segment line4 = new Line3.Segment(quad1.d, quad1.a);
		return QuadTriangleIntersectHelper(triangle3, triangle2, ref intersection1, ref intersection2) | QuadTriangleIntersectHelper(triangle4, triangle2, ref intersection1, ref intersection2) | QuadIntersectHelper(triangle2, quad1, ref intersection2, ref intersection1) | QuadTriangleIntersectHelper(line, triangle2, ref intersection1, ref intersection2) | QuadTriangleIntersectHelper(line2, triangle2, ref intersection1, ref intersection2) | QuadTriangleIntersectHelper(line3, triangle2, ref intersection1, ref intersection2) | QuadTriangleIntersectHelper(line4, triangle2, ref intersection1, ref intersection2);
	}

	private static bool QuadIntersectHelper(Triangle3 triangle1, Quad3 quad2, ref Bounds3 intersection1, ref Bounds3 intersection2)
	{
		Triangle2 xz = triangle1.xz;
		bool result = false;
		if (MathUtils.Intersect(xz, quad2.a.xz, out var t))
		{
			intersection1 |= MathUtils.Position(triangle1, t);
			intersection2 |= quad2.a;
			result = true;
		}
		if (MathUtils.Intersect(xz, quad2.b.xz, out t))
		{
			intersection1 |= MathUtils.Position(triangle1, t);
			intersection2 |= quad2.b;
			result = true;
		}
		if (MathUtils.Intersect(xz, quad2.c.xz, out t))
		{
			intersection1 |= MathUtils.Position(triangle1, t);
			intersection2 |= quad2.c;
			result = true;
		}
		if (MathUtils.Intersect(xz, quad2.d.xz, out t))
		{
			intersection1 |= MathUtils.Position(triangle1, t);
			intersection2 |= quad2.d;
			result = true;
		}
		return result;
	}

	private static bool QuadTriangleIntersectHelper(Triangle3 triangle1, Triangle3 triangle2, ref Bounds3 intersection1, ref Bounds3 intersection2)
	{
		Triangle2 xz = triangle1.xz;
		bool result = false;
		if (MathUtils.Intersect(xz, triangle2.a.xz, out var t))
		{
			intersection1 |= MathUtils.Position(triangle1, t);
			intersection2 |= triangle2.a;
			result = true;
		}
		if (MathUtils.Intersect(xz, triangle2.b.xz, out t))
		{
			intersection1 |= MathUtils.Position(triangle1, t);
			intersection2 |= triangle2.b;
			result = true;
		}
		if (MathUtils.Intersect(xz, triangle2.c.xz, out t))
		{
			intersection1 |= MathUtils.Position(triangle1, t);
			intersection2 |= triangle2.c;
			result = true;
		}
		return result;
	}

	private static bool QuadIntersectHelper(Line3.Segment line1, Quad3 quad2, ref Bounds3 intersection1, ref Bounds3 intersection2)
	{
		Line2.Segment xz = line1.xz;
		bool result = false;
		if (MathUtils.Intersect(xz, new Line2.Segment(quad2.a.xz, quad2.b.xz), out var t))
		{
			intersection1 |= MathUtils.Position(line1, t.x);
			intersection2 |= math.lerp(quad2.a, quad2.b, t.y);
			result = true;
		}
		if (MathUtils.Intersect(xz, new Line2.Segment(quad2.b.xz, quad2.c.xz), out t))
		{
			intersection1 |= MathUtils.Position(line1, t.x);
			intersection2 |= math.lerp(quad2.b, quad2.c, t.y);
			result = true;
		}
		if (MathUtils.Intersect(xz, new Line2.Segment(quad2.c.xz, quad2.d.xz), out t))
		{
			intersection1 |= MathUtils.Position(line1, t.x);
			intersection2 |= math.lerp(quad2.c, quad2.d, t.y);
			result = true;
		}
		if (MathUtils.Intersect(xz, new Line2.Segment(quad2.d.xz, quad2.a.xz), out t))
		{
			intersection1 |= MathUtils.Position(line1, t.x);
			intersection2 |= math.lerp(quad2.d, quad2.a, t.y);
			result = true;
		}
		return result;
	}

	private static bool QuadTriangleIntersectHelper(Line3.Segment line1, Triangle3 triangle2, ref Bounds3 intersection1, ref Bounds3 intersection2)
	{
		Line2.Segment xz = line1.xz;
		bool result = false;
		if (MathUtils.Intersect(xz, new Line2.Segment(triangle2.a.xz, triangle2.b.xz), out var t))
		{
			intersection1 |= MathUtils.Position(line1, t.x);
			intersection2 |= math.lerp(triangle2.a, triangle2.b, t.y);
			result = true;
		}
		if (MathUtils.Intersect(xz, new Line2.Segment(triangle2.b.xz, triangle2.c.xz), out t))
		{
			intersection1 |= MathUtils.Position(line1, t.x);
			intersection2 |= math.lerp(triangle2.b, triangle2.c, t.y);
			result = true;
		}
		if (MathUtils.Intersect(xz, new Line2.Segment(triangle2.c.xz, triangle2.a.xz), out t))
		{
			intersection1 |= MathUtils.Position(line1, t.x);
			intersection2 |= math.lerp(triangle2.c, triangle2.a, t.y);
			result = true;
		}
		return result;
	}
}
