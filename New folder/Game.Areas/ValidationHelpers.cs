using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Areas;

public static class ValidationHelpers
{
	private struct OriginalAreaIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
	{
		public Entity m_AreaEntity;

		public Bounds2 m_Bounds;

		public float2 m_Position;

		public float m_Offset;

		public bool m_Result;

		public DynamicBuffer<Node> m_Nodes;

		public DynamicBuffer<Triangle> m_Triangles;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			if (!m_Result)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}
			return false;
		}

		public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem areaItem2)
		{
			if (!m_Result && !(areaItem2.m_Area != m_AreaEntity) && MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds))
			{
				Triangle2 triangle = AreaUtils.GetTriangle2(m_Nodes, m_Triangles[areaItem2.m_Triangle]);
				m_Result = MathUtils.Intersect(triangle, new Circle2(m_Offset, m_Position));
			}
		}
	}

	private struct ObjectIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
	{
		public Entity m_AreaEntity;

		public Entity m_OriginalAreaEntity;

		public Entity m_IgnoreEntity;

		public Entity m_IgnoreEntity2;

		public Bounds3 m_TriangleBounds;

		public Bounds1 m_HeightRange;

		public Triangle3 m_Triangle;

		public ErrorSeverity m_ErrorSeverity;

		public CollisionMask m_CollisionMask;

		public AreaGeometryData m_PrefabAreaData;

		public ValidationSystem.EntityData m_Data;

		public NativeQueue<ErrorData>.ParallelWriter m_ErrorQueue;

		public bool m_EditorMode;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			if ((bounds.m_Mask & BoundsMask.NotOverridden) == 0)
			{
				return false;
			}
			return MathUtils.Intersect(bounds.m_Bounds.xz, m_TriangleBounds.xz);
		}

		public void Iterate(QuadTreeBoundsXZ bounds, Entity objectEntity2)
		{
			if ((bounds.m_Mask & BoundsMask.NotOverridden) == 0 || !MathUtils.Intersect(bounds.m_Bounds.xz, m_TriangleBounds.xz) || m_Data.m_Hidden.HasComponent(objectEntity2) || objectEntity2 == m_IgnoreEntity)
			{
				return;
			}
			Entity entity = objectEntity2;
			bool flag = false;
			Owner componentData;
			while (m_Data.m_Owner.TryGetComponent(entity, out componentData))
			{
				entity = componentData.m_Owner;
				flag = true;
				if (entity == m_AreaEntity || entity == m_OriginalAreaEntity)
				{
					return;
				}
			}
			if (entity == m_IgnoreEntity || entity == m_IgnoreEntity2)
			{
				return;
			}
			PrefabRef prefabRef = m_Data.m_PrefabRef[objectEntity2];
			Transform transform = m_Data.m_Transform[objectEntity2];
			if (!m_Data.m_PrefabObjectGeometry.HasComponent(prefabRef.m_Prefab))
			{
				return;
			}
			ObjectGeometryData objectGeometryData = m_Data.m_PrefabObjectGeometry[prefabRef.m_Prefab];
			CollisionMask collisionMask = ((!m_Data.m_ObjectElevation.HasComponent(objectEntity2)) ? ObjectUtils.GetCollisionMask(objectGeometryData, !m_EditorMode || flag) : ObjectUtils.GetCollisionMask(objectGeometryData, m_Data.m_ObjectElevation[objectEntity2], !m_EditorMode || flag));
			if ((m_CollisionMask & collisionMask) == 0)
			{
				return;
			}
			ErrorData value = new ErrorData
			{
				m_ErrorSeverity = m_ErrorSeverity,
				m_TempEntity = m_AreaEntity,
				m_PermanentEntity = objectEntity2
			};
			if (entity != objectEntity2)
			{
				if ((objectGeometryData.m_Flags & (Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.DeleteOverridden)) == Game.Objects.GeometryFlags.Overridable)
				{
					if ((m_PrefabAreaData.m_Flags & GeometryFlags.CanOverrideObjects) == 0)
					{
						return;
					}
					value.m_ErrorSeverity = ErrorSeverity.Override;
				}
				else
				{
					PrefabRef prefabRef2 = m_Data.m_PrefabRef[entity];
					if (m_Data.m_PrefabObjectGeometry.HasComponent(prefabRef2.m_Prefab))
					{
						ObjectGeometryData objectGeometryData2 = m_Data.m_PrefabObjectGeometry[prefabRef2.m_Prefab];
						if ((objectGeometryData2.m_Flags & Game.Objects.GeometryFlags.Overridable) != Game.Objects.GeometryFlags.None)
						{
							if ((objectGeometryData2.m_Flags & Game.Objects.GeometryFlags.DeleteOverridden) == 0)
							{
								return;
							}
							if (!m_Data.m_Attached.HasComponent(entity))
							{
								value.m_ErrorSeverity = ErrorSeverity.Warning;
								value.m_PermanentEntity = entity;
							}
						}
					}
				}
			}
			else if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Overridable) != Game.Objects.GeometryFlags.None)
			{
				if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.DeleteOverridden) != Game.Objects.GeometryFlags.None)
				{
					if (!m_Data.m_Attached.HasComponent(objectEntity2))
					{
						value.m_ErrorSeverity = ErrorSeverity.Warning;
					}
				}
				else
				{
					if ((m_PrefabAreaData.m_Flags & GeometryFlags.CanOverrideObjects) == 0)
					{
						return;
					}
					value.m_ErrorSeverity = ErrorSeverity.Override;
				}
			}
			if ((collisionMask & CollisionMask.OnGround) == 0 || MathUtils.Intersect(m_TriangleBounds, bounds.m_Bounds))
			{
				float3 @float = math.mul(math.inverse(transform.m_Rotation), transform.m_Position);
				Bounds3 bounds2 = objectGeometryData.m_Bounds;
				if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.IgnoreBottomCollision) != Game.Objects.GeometryFlags.None)
				{
					bounds2.min.y = math.max(bounds2.min.y, 0f);
				}
				if (ObjectUtils.GetStandingLegCount(objectGeometryData, out var legCount))
				{
					for (int i = 0; i < legCount; i++)
					{
						if ((objectGeometryData.m_Flags & (Game.Objects.GeometryFlags.CircularLeg | Game.Objects.GeometryFlags.IgnoreLegCollision)) == Game.Objects.GeometryFlags.CircularLeg)
						{
							float3 float2 = @float + ObjectUtils.GetStandingLegOffset(objectGeometryData, i);
							if (Game.Net.ValidationHelpers.TriangleCylinderIntersect(cylinder2: new Cylinder3
							{
								circle = new Circle2(objectGeometryData.m_LegSize.x * 0.5f, float2.xz),
								height = new Bounds1(bounds2.min.y, objectGeometryData.m_LegSize.y) + float2.y,
								rotation = transform.m_Rotation
							}, triangle1: m_Triangle, intersection1: out var intersection, intersection2: out var intersection2))
							{
								intersection = Game.Net.ValidationHelpers.SetHeightRange(intersection, m_HeightRange);
								if (MathUtils.Intersect(intersection, intersection2, out var intersection3))
								{
									value.m_Position = MathUtils.Center(intersection3);
									value.m_ErrorType = ErrorType.OverlapExisting;
								}
							}
						}
						else
						{
							if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.IgnoreLegCollision) != Game.Objects.GeometryFlags.None)
							{
								continue;
							}
							float3 standingLegPosition = ObjectUtils.GetStandingLegPosition(objectGeometryData, transform, i);
							bounds2.min.xz = objectGeometryData.m_LegSize.xz * -0.5f;
							bounds2.max.xz = objectGeometryData.m_LegSize.xz * 0.5f;
							if (Game.Net.ValidationHelpers.QuadTriangleIntersect(ObjectUtils.CalculateBaseCorners(standingLegPosition, transform.m_Rotation, bounds2), m_Triangle, out var intersection4, out var intersection5))
							{
								intersection5 = Game.Net.ValidationHelpers.SetHeightRange(intersection5, m_HeightRange);
								intersection4 = Game.Net.ValidationHelpers.SetHeightRange(intersection4, bounds2.y);
								if (MathUtils.Intersect(intersection5, intersection4, out var intersection6))
								{
									value.m_Position = MathUtils.Center(intersection6);
									value.m_ErrorType = ErrorType.OverlapExisting;
								}
							}
						}
					}
					bounds2.min.y = objectGeometryData.m_LegSize.y;
				}
				Bounds3 intersection11;
				Bounds3 intersection10;
				if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
				{
					if (Game.Net.ValidationHelpers.TriangleCylinderIntersect(cylinder2: new Cylinder3
					{
						circle = new Circle2(objectGeometryData.m_Size.x * 0.5f, @float.xz),
						height = new Bounds1(bounds2.min.y, bounds2.max.y) + @float.y,
						rotation = transform.m_Rotation
					}, triangle1: m_Triangle, intersection1: out var intersection7, intersection2: out var intersection8))
					{
						intersection7 = Game.Net.ValidationHelpers.SetHeightRange(intersection7, m_HeightRange);
						if (MathUtils.Intersect(intersection7, intersection8, out var intersection9))
						{
							value.m_Position = MathUtils.Center(intersection9);
							value.m_ErrorType = ErrorType.OverlapExisting;
						}
					}
				}
				else if (Game.Net.ValidationHelpers.QuadTriangleIntersect(ObjectUtils.CalculateBaseCorners(transform.m_Position, transform.m_Rotation, objectGeometryData.m_Bounds), m_Triangle, out intersection10, out intersection11))
				{
					intersection11 = Game.Net.ValidationHelpers.SetHeightRange(intersection11, m_HeightRange);
					intersection10 = Game.Net.ValidationHelpers.SetHeightRange(intersection10, bounds2.y);
					if (MathUtils.Intersect(intersection11, intersection10, out var intersection12))
					{
						value.m_Position = MathUtils.Center(intersection12);
						value.m_ErrorType = ErrorType.OverlapExisting;
					}
				}
			}
			if (value.m_ErrorType == ErrorType.None && CommonUtils.ExclusiveGroundCollision(collisionMask, m_CollisionMask))
			{
				float2 t;
				if (ObjectUtils.GetStandingLegCount(objectGeometryData, out var legCount2))
				{
					for (int j = 0; j < legCount2; j++)
					{
						float3 standingLegPosition2 = ObjectUtils.GetStandingLegPosition(objectGeometryData, transform, j);
						if ((objectGeometryData.m_Flags & (Game.Objects.GeometryFlags.CircularLeg | Game.Objects.GeometryFlags.IgnoreLegCollision)) == Game.Objects.GeometryFlags.CircularLeg)
						{
							Circle2 circle = new Circle2(objectGeometryData.m_LegSize.x * 0.5f, standingLegPosition2.xz);
							if ((!m_Triangle.c.Equals(m_Triangle.b)) ? MathUtils.Intersect(m_Triangle.xz, circle) : MathUtils.Intersect(circle, m_Triangle.xz.ab, out t))
							{
								value.m_Position = MathUtils.Center(bounds.m_Bounds & m_TriangleBounds);
								value.m_ErrorType = ErrorType.OverlapExisting;
							}
						}
						else if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.IgnoreLegCollision) == 0)
						{
							Quad2 xz = ObjectUtils.CalculateBaseCorners(bounds: new Bounds3
							{
								min = 
								{
									xz = objectGeometryData.m_LegSize.xz * -0.5f
								},
								max = 
								{
									xz = objectGeometryData.m_LegSize.xz * 0.5f
								}
							}, position: standingLegPosition2, rotation: transform.m_Rotation).xz;
							if ((!m_Triangle.c.Equals(m_Triangle.b)) ? MathUtils.Intersect(xz, m_Triangle.xz) : MathUtils.Intersect(xz, m_Triangle.xz.ab, out t))
							{
								value.m_Position = MathUtils.Center(bounds.m_Bounds & m_TriangleBounds);
								value.m_ErrorType = ErrorType.OverlapExisting;
							}
						}
					}
				}
				else if ((objectGeometryData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
				{
					Circle2 circle2 = new Circle2(objectGeometryData.m_Size.x * 0.5f, transform.m_Position.xz);
					if ((!m_Triangle.c.Equals(m_Triangle.b)) ? MathUtils.Intersect(m_Triangle.xz, circle2) : MathUtils.Intersect(circle2, m_Triangle.xz.ab, out t))
					{
						value.m_Position = MathUtils.Center(bounds.m_Bounds & m_TriangleBounds);
						value.m_ErrorType = ErrorType.OverlapExisting;
					}
				}
				else
				{
					Quad2 xz2 = ObjectUtils.CalculateBaseCorners(transform.m_Position, transform.m_Rotation, objectGeometryData.m_Bounds).xz;
					if ((!m_Triangle.c.Equals(m_Triangle.b)) ? MathUtils.Intersect(xz2, m_Triangle.xz) : MathUtils.Intersect(xz2, m_Triangle.xz.ab, out t))
					{
						value.m_Position = MathUtils.Center(bounds.m_Bounds & m_TriangleBounds);
						value.m_ErrorType = ErrorType.OverlapExisting;
					}
				}
			}
			if (value.m_ErrorType != ErrorType.None)
			{
				if ((value.m_ErrorSeverity == ErrorSeverity.Override || value.m_ErrorSeverity == ErrorSeverity.Warning) && value.m_ErrorType == ErrorType.OverlapExisting && m_Data.m_OnFire.HasComponent(value.m_PermanentEntity))
				{
					value.m_ErrorType = ErrorType.OnFire;
					value.m_ErrorSeverity = ErrorSeverity.Error;
				}
				m_ErrorQueue.Enqueue(value);
			}
		}
	}

	private struct NetIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
	{
		public Entity m_AreaEntity;

		public Entity m_OriginalAreaEntity;

		public Entity m_IgnoreEntity;

		public Entity m_IgnoreEntity2;

		public Bounds3 m_TriangleBounds;

		public Bounds1 m_HeightRange;

		public Triangle3 m_Triangle;

		public ErrorSeverity m_ErrorSeverity;

		public CollisionMask m_CollisionMask;

		public ValidationSystem.EntityData m_Data;

		public NativeQueue<ErrorData>.ParallelWriter m_ErrorQueue;

		public bool m_EditorMode;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			return MathUtils.Intersect(bounds.m_Bounds.xz, m_TriangleBounds.xz);
		}

		public void Iterate(QuadTreeBoundsXZ bounds, Entity edgeEntity2)
		{
			if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_TriangleBounds.xz) || m_Data.m_Hidden.HasComponent(edgeEntity2))
			{
				return;
			}
			Entity entity = edgeEntity2;
			bool flag = false;
			Owner componentData;
			while (m_Data.m_Owner.TryGetComponent(entity, out componentData))
			{
				entity = componentData.m_Owner;
				flag = true;
				if (entity == m_AreaEntity || entity == m_OriginalAreaEntity)
				{
					return;
				}
			}
			if (entity == m_IgnoreEntity || entity == m_IgnoreEntity2 || !m_Data.m_Composition.HasComponent(edgeEntity2))
			{
				return;
			}
			Edge edge = m_Data.m_Edge[edgeEntity2];
			Composition composition = m_Data.m_Composition[edgeEntity2];
			EdgeGeometry edgeGeometry = m_Data.m_EdgeGeometry[edgeEntity2];
			StartNodeGeometry startNodeGeometry = m_Data.m_StartNodeGeometry[edgeEntity2];
			EndNodeGeometry endNodeGeometry = m_Data.m_EndNodeGeometry[edgeEntity2];
			NetCompositionData netCompositionData = m_Data.m_PrefabComposition[composition.m_Edge];
			NetCompositionData netCompositionData2 = m_Data.m_PrefabComposition[composition.m_StartNode];
			NetCompositionData netCompositionData3 = m_Data.m_PrefabComposition[composition.m_EndNode];
			CollisionMask collisionMask = NetUtils.GetCollisionMask(netCompositionData, !m_EditorMode || flag);
			CollisionMask collisionMask2 = NetUtils.GetCollisionMask(netCompositionData2, !m_EditorMode || flag);
			CollisionMask collisionMask3 = NetUtils.GetCollisionMask(netCompositionData3, !m_EditorMode || flag);
			CollisionMask collisionMask4 = collisionMask | collisionMask2 | collisionMask3;
			if ((m_CollisionMask & collisionMask4) == 0)
			{
				return;
			}
			ErrorData value = default(ErrorData);
			Bounds3 intersection = default(Bounds3);
			intersection.min = float.MaxValue;
			intersection.max = float.MinValue;
			bool flag2 = m_Triangle.c.Equals(m_Triangle.b);
			if ((collisionMask4 & CollisionMask.OnGround) == 0 || MathUtils.Intersect(m_TriangleBounds, bounds.m_Bounds))
			{
				if ((collisionMask & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edge, m_AreaEntity, edgeGeometry, m_Triangle, netCompositionData, m_HeightRange, ref intersection))
				{
					value.m_ErrorType = ErrorType.OverlapExisting;
				}
				if ((collisionMask2 & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edge.m_Start, m_AreaEntity, startNodeGeometry.m_Geometry, m_Triangle, netCompositionData2, m_HeightRange, ref intersection))
				{
					value.m_ErrorType = ErrorType.OverlapExisting;
				}
				if ((collisionMask3 & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edge.m_End, m_AreaEntity, endNodeGeometry.m_Geometry, m_Triangle, netCompositionData3, m_HeightRange, ref intersection))
				{
					value.m_ErrorType = ErrorType.OverlapExisting;
				}
			}
			if (value.m_ErrorType == ErrorType.None && CommonUtils.ExclusiveGroundCollision(collisionMask4, m_CollisionMask))
			{
				if ((collisionMask & m_CollisionMask) != 0)
				{
					if (flag2)
					{
						if (Game.Net.ValidationHelpers.Intersect(edge, m_AreaEntity, edgeGeometry, m_Triangle.xz.ab, m_TriangleBounds, netCompositionData, ref intersection))
						{
							value.m_ErrorType = ErrorType.OverlapExisting;
						}
					}
					else if (Game.Net.ValidationHelpers.Intersect(edge, m_AreaEntity, edgeGeometry, m_Triangle.xz, m_TriangleBounds, netCompositionData, ref intersection))
					{
						value.m_ErrorType = ErrorType.OverlapExisting;
					}
				}
				if ((collisionMask2 & m_CollisionMask) != 0)
				{
					if (flag2)
					{
						if (Game.Net.ValidationHelpers.Intersect(edge.m_Start, m_AreaEntity, startNodeGeometry.m_Geometry, m_Triangle.xz.ab, m_TriangleBounds, netCompositionData2, ref intersection))
						{
							value.m_ErrorType = ErrorType.OverlapExisting;
						}
					}
					else if (Game.Net.ValidationHelpers.Intersect(edge.m_Start, m_AreaEntity, startNodeGeometry.m_Geometry, m_Triangle.xz, m_TriangleBounds, netCompositionData2, ref intersection))
					{
						value.m_ErrorType = ErrorType.OverlapExisting;
					}
				}
				if ((collisionMask3 & m_CollisionMask) != 0)
				{
					if (flag2)
					{
						if (Game.Net.ValidationHelpers.Intersect(edge.m_End, m_AreaEntity, endNodeGeometry.m_Geometry, m_Triangle.xz.ab, m_TriangleBounds, netCompositionData3, ref intersection))
						{
							value.m_ErrorType = ErrorType.OverlapExisting;
						}
					}
					else if (Game.Net.ValidationHelpers.Intersect(edge.m_End, m_AreaEntity, endNodeGeometry.m_Geometry, m_Triangle.xz, m_TriangleBounds, netCompositionData3, ref intersection))
					{
						value.m_ErrorType = ErrorType.OverlapExisting;
					}
				}
			}
			if (value.m_ErrorType != ErrorType.None)
			{
				value.m_ErrorSeverity = m_ErrorSeverity;
				value.m_TempEntity = m_AreaEntity;
				value.m_PermanentEntity = edgeEntity2;
				value.m_Position = MathUtils.Center(intersection);
				m_ErrorQueue.Enqueue(value);
			}
		}
	}

	private struct AreaIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
	{
		public Entity m_AreaEntity;

		public Entity m_IgnoreEntity;

		public Entity m_IgnoreEntity2;

		public Entity m_TopLevelEntity;

		public Bounds3 m_TriangleBounds;

		public Triangle2 m_Triangle;

		public bool m_IgnoreCollisions;

		public bool m_EditorMode;

		public bool m_Essential;

		public AreaGeometryData m_PrefabAreaData;

		public ErrorSeverity m_ErrorSeverity;

		public ValidationSystem.EntityData m_Data;

		public NativeQueue<ErrorData>.ParallelWriter m_ErrorQueue;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			return MathUtils.Intersect(bounds.m_Bounds.xz, m_TriangleBounds.xz);
		}

		public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem areaItem2)
		{
			if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_TriangleBounds.xz) || m_Data.m_Hidden.HasComponent(areaItem2.m_Area))
			{
				return;
			}
			Area area = m_Data.m_Area[areaItem2.m_Area];
			if ((area.m_Flags & AreaFlags.Slave) != 0)
			{
				return;
			}
			Entity entity = areaItem2.m_Area;
			bool flag = false;
			Owner componentData;
			while (m_Data.m_Owner.TryGetComponent(entity, out componentData) && !m_Data.m_Building.HasComponent(entity))
			{
				Entity owner = componentData.m_Owner;
				flag = true;
				if (m_Data.m_AssetStamp.HasComponent(owner))
				{
					break;
				}
				entity = owner;
			}
			if (entity == m_TopLevelEntity)
			{
				return;
			}
			if (m_IgnoreEntity != Entity.Null)
			{
				Entity entity2 = entity;
				while (m_Data.m_Owner.HasComponent(entity2))
				{
					entity2 = m_Data.m_Owner[entity2].m_Owner;
				}
				if (entity2 == m_IgnoreEntity || entity2 == m_IgnoreEntity2)
				{
					return;
				}
			}
			PrefabRef prefabRef = m_Data.m_PrefabRef[areaItem2.m_Area];
			AreaGeometryData areaGeometryData = m_Data.m_PrefabAreaGeometry[prefabRef.m_Prefab];
			AreaUtils.SetCollisionFlags(ref areaGeometryData, !m_EditorMode || flag);
			if (areaGeometryData.m_Type != m_PrefabAreaData.m_Type)
			{
				if ((areaGeometryData.m_Flags & (GeometryFlags.PhysicalGeometry | GeometryFlags.ProtectedArea)) == 0)
				{
					return;
				}
				if ((areaGeometryData.m_Flags & GeometryFlags.ProtectedArea) != 0)
				{
					if (!m_Data.m_Native.HasComponent(areaItem2.m_Area))
					{
						return;
					}
				}
				else if (m_IgnoreCollisions || ((areaGeometryData.m_Flags & GeometryFlags.PhysicalGeometry) != 0 && (m_PrefabAreaData.m_Flags & GeometryFlags.PhysicalGeometry) == 0))
				{
					return;
				}
			}
			else if ((areaGeometryData.m_Flags & (GeometryFlags.PhysicalGeometry | GeometryFlags.ProtectedArea)) == 0 && entity != areaItem2.m_Area && m_TopLevelEntity != m_AreaEntity && (m_EditorMode || m_IgnoreCollisions || !m_Essential))
			{
				return;
			}
			DynamicBuffer<Node> nodes = m_Data.m_AreaNodes[areaItem2.m_Area];
			DynamicBuffer<Triangle> dynamicBuffer = m_Data.m_AreaTriangles[areaItem2.m_Area];
			Triangle2 triangle = AreaUtils.GetTriangle2(isCounterClockwise: (area.m_Flags & AreaFlags.CounterClockwise) != 0, nodes: nodes, triangle: dynamicBuffer[areaItem2.m_Triangle], expandAmount: -0.1f);
			if (!((!m_Triangle.c.Equals(m_Triangle.b)) ? MathUtils.Intersect(m_Triangle, triangle) : MathUtils.Intersect(triangle, m_Triangle.ab, out var _)))
			{
				return;
			}
			ErrorData value = default(ErrorData);
			value.m_ErrorSeverity = m_ErrorSeverity;
			value.m_ErrorType = ((areaGeometryData.m_Type != AreaType.MapTile || m_EditorMode) ? ErrorType.OverlapExisting : ErrorType.ExceedsCityLimits);
			value.m_TempEntity = m_AreaEntity;
			value.m_PermanentEntity = areaItem2.m_Area;
			value.m_Position = MathUtils.Center(bounds.m_Bounds & m_TriangleBounds);
			value.m_Position.y = MathUtils.Clamp(value.m_Position.y, m_TriangleBounds.y);
			if (value.m_ErrorType == ErrorType.OverlapExisting)
			{
				if (entity != areaItem2.m_Area && entity != Entity.Null)
				{
					PrefabRef prefabRef2 = m_Data.m_PrefabRef[entity];
					if (m_Data.m_PrefabObjectGeometry.HasComponent(prefabRef2.m_Prefab) && (m_Data.m_PrefabObjectGeometry[prefabRef2.m_Prefab].m_Flags & (Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.DeleteOverridden)) == (Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.DeleteOverridden) && !m_Data.m_Attached.HasComponent(entity) && (!m_Data.m_Temp.HasComponent(entity) || (m_Data.m_Temp[entity].m_Flags & TempFlags.Essential) == 0))
					{
						value.m_ErrorSeverity = ErrorSeverity.Warning;
						value.m_PermanentEntity = entity;
					}
				}
				if (!m_Essential && m_TopLevelEntity != m_AreaEntity && m_TopLevelEntity != Entity.Null)
				{
					PrefabRef prefabRef3 = m_Data.m_PrefabRef[m_TopLevelEntity];
					if (m_Data.m_PrefabObjectGeometry.HasComponent(prefabRef3.m_Prefab) && (m_Data.m_PrefabObjectGeometry[prefabRef3.m_Prefab].m_Flags & (Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.DeleteOverridden)) == (Game.Objects.GeometryFlags.Overridable | Game.Objects.GeometryFlags.DeleteOverridden) && !m_Data.m_Attached.HasComponent(m_TopLevelEntity) && (!m_Data.m_Temp.HasComponent(m_TopLevelEntity) || (m_Data.m_Temp[m_TopLevelEntity].m_Flags & TempFlags.Essential) == 0))
					{
						value.m_ErrorSeverity = ErrorSeverity.Warning;
						value.m_TempEntity = areaItem2.m_Area;
						value.m_PermanentEntity = m_TopLevelEntity;
					}
				}
			}
			m_ErrorQueue.Enqueue(value);
		}
	}

	public struct BrushAreaIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
	{
		public Entity m_BrushEntity;

		public Brush m_Brush;

		public Bounds3 m_BrushBounds;

		public ValidationSystem.EntityData m_Data;

		public NativeQueue<ErrorData>.ParallelWriter m_ErrorQueue;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			return MathUtils.Intersect(bounds.m_Bounds.xz, m_BrushBounds.xz);
		}

		public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem areaItem2)
		{
			if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_BrushBounds.xz) || m_Data.m_Hidden.HasComponent(areaItem2.m_Area) || (m_Data.m_Area[areaItem2.m_Area].m_Flags & AreaFlags.Slave) != 0)
			{
				return;
			}
			PrefabRef prefabRef = m_Data.m_PrefabRef[areaItem2.m_Area];
			AreaGeometryData areaGeometryData = m_Data.m_PrefabAreaGeometry[prefabRef.m_Prefab];
			if ((areaGeometryData.m_Flags & GeometryFlags.ProtectedArea) != 0 && m_Data.m_Native.HasComponent(areaItem2.m_Area))
			{
				DynamicBuffer<Node> nodes = m_Data.m_AreaNodes[areaItem2.m_Area];
				Triangle triangle = m_Data.m_AreaTriangles[areaItem2.m_Area][areaItem2.m_Triangle];
				Triangle3 triangle2 = AreaUtils.GetTriangle3(nodes, triangle);
				ErrorData value = default(ErrorData);
				if (areaGeometryData.m_Type == AreaType.MapTile && MathUtils.Intersect(circle: new Circle2(m_Brush.m_Size * 0.4f, m_Brush.m_Position.xz), triangle: triangle2.xz))
				{
					value.m_Position = MathUtils.Center(m_BrushBounds & bounds.m_Bounds);
					value.m_ErrorType = ErrorType.ExceedsCityLimits;
				}
				if (value.m_ErrorType != ErrorType.None)
				{
					value.m_Position.y = MathUtils.Clamp(value.m_Position.y, m_BrushBounds.y);
					value.m_ErrorSeverity = ErrorSeverity.Error;
					value.m_TempEntity = m_BrushEntity;
					value.m_PermanentEntity = areaItem2.m_Area;
					m_ErrorQueue.Enqueue(value);
				}
			}
		}
	}

	public static void ValidateArea(bool editorMode, Entity entity, Temp temp, Owner owner, Area area, Geometry geometry, Storage storage, DynamicBuffer<Node> nodes, PrefabRef prefabRef, ValidationSystem.EntityData data, NativeQuadTree<Entity, QuadTreeBoundsXZ> objectSearchTree, NativeQuadTree<Entity, QuadTreeBoundsXZ> netSearchTree, NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> areaSearchTree, WaterSurfaceData<SurfaceWater> waterSurfaceData, TerrainHeightData terrainHeightData, NativeQueue<ErrorData>.ParallelWriter errorQueue)
	{
		if ((area.m_Flags & AreaFlags.Slave) != 0)
		{
			return;
		}
		float minNodeDistance = AreaUtils.GetMinNodeDistance(data.m_PrefabAreaGeometry[prefabRef.m_Prefab]);
		bool flag = true;
		bool flag2 = (area.m_Flags & AreaFlags.Complete) != 0;
		bool isCounterClockwise = (area.m_Flags & AreaFlags.CounterClockwise) != 0;
		if (nodes.Length == 2)
		{
			ValidateTriangle(editorMode, noErrors: false, isCounterClockwise, entity, temp, owner, new Triangle(0, 1, 1), data, objectSearchTree, netSearchTree, areaSearchTree, waterSurfaceData, terrainHeightData, errorQueue);
		}
		else if (nodes.Length == 3)
		{
			if ((temp.m_Flags & TempFlags.Delete) == 0)
			{
				Line3.Segment line = new Line3.Segment(nodes[0].m_Position, nodes[1].m_Position);
				Line3.Segment line2 = new Line3.Segment(nodes[1].m_Position, nodes[2].m_Position);
				flag &= CheckShape(line, nodes[2].m_Position, entity, minNodeDistance, errorQueue);
				flag &= CheckShape(line2, nodes[0].m_Position, entity, minNodeDistance, errorQueue);
				if (flag2)
				{
					Line3.Segment line3 = new Line3.Segment(nodes[2].m_Position, nodes[0].m_Position);
					flag &= CheckShape(line3, nodes[1].m_Position, entity, minNodeDistance, errorQueue);
				}
			}
		}
		else if (nodes.Length > 3 && (temp.m_Flags & TempFlags.Delete) == 0)
		{
			int num = 0;
			int num2 = math.select(nodes.Length - 1, nodes.Length, flag2);
			NativeArray<Bounds2> nativeArray = default(NativeArray<Bounds2>);
			int num3 = num2 - num - 2;
			int num4 = 0;
			float2 t;
			if (num3 > 10)
			{
				int num5 = -1;
				int num6 = 0;
				while (num3 >= 2)
				{
					num5 += 1 << num6++;
					num3 >>= 1;
				}
				nativeArray = new NativeArray<Bounds2>(num5, Allocator.Temp);
				num4 = --num6;
				int num7 = 1 << num6;
				int num8 = num5 - num7;
				num3 = num2 - num - 2;
				Bounds2 bounds = default(Bounds2);
				for (int i = 0; i < num7; i++)
				{
					int num9 = i * num3 >> num6;
					int num10 = (i + 1) * num3 >> num6;
					t = (bounds.min = (bounds.max = nodes[num9++].m_Position.xz));
					for (int j = num9; j <= num10; j++)
					{
						bounds |= nodes[j].m_Position.xz;
					}
					nativeArray[num8 + i] = MathUtils.Expand(bounds, minNodeDistance);
				}
				while (--num6 > 0)
				{
					int num11 = num8;
					num7 = 1 << num6;
					num8 -= num7;
					for (int k = 0; k < num7; k++)
					{
						nativeArray[num8 + k] = nativeArray[num11 + (k << 1)] | nativeArray[num11 + (k << 1) + 1];
					}
				}
			}
			Line3.Segment line4 = new Line3.Segment
			{
				a = nodes[num++].m_Position
			};
			for (int l = num; l <= num2; l++)
			{
				int num12 = math.select(l, 0, l == nodes.Length);
				line4.b = nodes[num12].m_Position;
				int num13 = math.select(0, 1, l == nodes.Length);
				int num14 = l - 2;
				if (nativeArray.IsCreated)
				{
					int num15 = 0;
					int num16 = 1;
					int num17 = 0;
					while (num16 > 0)
					{
						if (MathUtils.Intersect(nativeArray[num15 + num17], line4.xz, out t))
						{
							if (num16 != num4)
							{
								num17 <<= 1;
								num15 += 1 << num16++;
								continue;
							}
							int num18 = math.max(num13, num17 * num3 >> num16);
							int num19 = math.min(num14, (num17 + 1) * num3 >> num16);
							if (num19 > num18)
							{
								Line3.Segment line5 = new Line3.Segment
								{
									a = nodes[num18++].m_Position
								};
								for (int m = num18; m <= num19; m++)
								{
									line5.b = nodes[m].m_Position;
									flag &= CheckShape(line4, line5, entity, minNodeDistance, errorQueue, nodes, num12, m, flag2, isCounterClockwise);
									line5.a = line5.b;
								}
							}
						}
						while ((num17 & 1) != 0)
						{
							num17 >>= 1;
							num15 -= 1 << --num16;
						}
						num17++;
					}
				}
				else
				{
					Line3.Segment line6 = new Line3.Segment
					{
						a = nodes[num13++].m_Position
					};
					for (int n = num13; n <= num14; n++)
					{
						line6.b = nodes[n].m_Position;
						flag &= CheckShape(line4, line6, entity, minNodeDistance, errorQueue, nodes, num12, n, flag2, isCounterClockwise);
						line6.a = line6.b;
					}
				}
				if (l > num || flag2)
				{
					int num20 = l - 2;
					num20 += math.select(0, nodes.Length, num20 < 0);
					flag &= CheckShape(line4, nodes[num20].m_Position, entity, minNodeDistance, errorQueue);
				}
				if (l < num2 || flag2)
				{
					int num21 = l + 1;
					num21 -= math.select(0, nodes.Length, num21 >= nodes.Length);
					flag &= CheckShape(line4, nodes[num21].m_Position, entity, minNodeDistance, errorQueue);
				}
				if (!flag2)
				{
					if (l > num)
					{
						flag &= CheckShape(line4, nodes[0].m_Position, entity, minNodeDistance, errorQueue, nodes, num12, 0, flag2, isCounterClockwise);
					}
					if (l < num2)
					{
						flag &= CheckShape(line4, nodes[nodes.Length - 1].m_Position, entity, minNodeDistance, errorQueue, nodes, num12, nodes.Length - 1, flag2, isCounterClockwise);
					}
				}
				line4.a = line4.b;
			}
			if (nativeArray.IsCreated)
			{
				nativeArray.Dispose();
			}
		}
		if (!flag2 && nodes.Length >= 3)
		{
			ValidateTriangle(editorMode, noErrors: false, isCounterClockwise, entity, temp, owner, new Triangle(nodes.Length - 2, nodes.Length - 1, nodes.Length - 1), data, objectSearchTree, netSearchTree, areaSearchTree, waterSurfaceData, terrainHeightData, errorQueue);
		}
		if (flag && flag2 && (area.m_Flags & AreaFlags.NoTriangles) != 0 && nodes.Length >= 3)
		{
			float3 position = 0;
			for (int num22 = 0; num22 < nodes.Length; num22++)
			{
				position += nodes[num22].m_Position;
			}
			position /= (float)nodes.Length;
			errorQueue.Enqueue(new ErrorData
			{
				m_ErrorSeverity = ErrorSeverity.Error,
				m_ErrorType = ErrorType.InvalidShape,
				m_TempEntity = entity,
				m_Position = position
			});
			flag = false;
		}
		if ((temp.m_Flags & TempFlags.Delete) == 0 && data.m_Transform.HasComponent(owner.m_Owner) && data.m_PrefabLotData.HasComponent(prefabRef.m_Prefab))
		{
			float2 xz = data.m_Transform[owner.m_Owner].m_Position.xz;
			float maxRadius = data.m_PrefabLotData[prefabRef.m_Prefab].m_MaxRadius;
			if (maxRadius > 0f)
			{
				for (int num23 = 0; num23 < nodes.Length; num23++)
				{
					if (math.distance(xz, nodes[num23].m_Position.xz) > maxRadius)
					{
						errorQueue.Enqueue(new ErrorData
						{
							m_ErrorSeverity = ErrorSeverity.Error,
							m_ErrorType = ErrorType.LongDistance,
							m_TempEntity = entity,
							m_PermanentEntity = owner.m_Owner,
							m_Position = nodes[num23].m_Position
						});
					}
				}
			}
		}
		if ((temp.m_Flags & TempFlags.Delete) == 0 && flag && flag2 && data.m_PrefabStorageArea.HasComponent(prefabRef.m_Prefab))
		{
			StorageAreaData prefabStorageData = data.m_PrefabStorageArea[prefabRef.m_Prefab];
			int num24 = AreaUtils.CalculateStorageCapacity(geometry, prefabStorageData);
			if (storage.m_Amount > num24)
			{
				errorQueue.Enqueue(new ErrorData
				{
					m_ErrorSeverity = ErrorSeverity.Error,
					m_ErrorType = ErrorType.SmallArea,
					m_TempEntity = entity,
					m_Position = geometry.m_CenterPosition
				});
			}
		}
	}

	private static bool CheckShape(Line3.Segment line1, float3 node2, Entity entity, float minNodeDistance, NativeQueue<ErrorData>.ParallelWriter errorQueue)
	{
		if (MathUtils.Distance(line1.xz, node2.xz, out var t) < minNodeDistance)
		{
			errorQueue.Enqueue(new ErrorData
			{
				m_ErrorSeverity = ErrorSeverity.Error,
				m_ErrorType = ErrorType.InvalidShape,
				m_TempEntity = entity,
				m_Position = math.lerp(MathUtils.Position(line1, t), node2, 0.5f)
			});
			return false;
		}
		return true;
	}

	private static bool CheckShape(Line3.Segment line1, float3 node2, Entity entity, float minNodeDistance, NativeQueue<ErrorData>.ParallelWriter errorQueue, DynamicBuffer<Node> nodes, int index1, int index2, bool isComplete, bool isCounterClockwise)
	{
		if (MathUtils.Distance(line1.xz, node2.xz, out var t) < minNodeDistance)
		{
			Quad2 edgeQuad = GetEdgeQuad(minNodeDistance, nodes, index1, isComplete, isCounterClockwise);
			Line2.Segment edgeLine = GetEdgeLine(minNodeDistance, nodes, index2, isComplete, isCounterClockwise);
			if (MathUtils.Intersect(edgeQuad, node2.xz) || MathUtils.Intersect(edgeLine, line1.xz, out var _))
			{
				errorQueue.Enqueue(new ErrorData
				{
					m_ErrorSeverity = ErrorSeverity.Error,
					m_ErrorType = ErrorType.InvalidShape,
					m_TempEntity = entity,
					m_Position = math.lerp(MathUtils.Position(line1, t), node2, 0.5f)
				});
				return false;
			}
		}
		return true;
	}

	private static bool CheckShape(Line3.Segment line1, Line3.Segment line2, Entity entity, float minNodeDistance, NativeQueue<ErrorData>.ParallelWriter errorQueue, DynamicBuffer<Node> nodes, int index1, int index2, bool isComplete, bool isCounterClockwise)
	{
		if (MathUtils.Distance(line1.xz, line2.xz, out var t) < minNodeDistance)
		{
			Quad2 edgeQuad = GetEdgeQuad(minNodeDistance, nodes, index1, isComplete, isCounterClockwise);
			Quad2 edgeQuad2 = GetEdgeQuad(minNodeDistance, nodes, index2, isComplete, isCounterClockwise);
			if (MathUtils.Intersect(edgeQuad, line2.xz, out var t2) || MathUtils.Intersect(edgeQuad2, line1.xz, out t2))
			{
				errorQueue.Enqueue(new ErrorData
				{
					m_ErrorSeverity = ErrorSeverity.Error,
					m_ErrorType = ErrorType.InvalidShape,
					m_TempEntity = entity,
					m_Position = math.lerp(MathUtils.Position(line1, t.x), MathUtils.Position(line2, t.y), 0.5f)
				});
				return false;
			}
		}
		return true;
	}

	private static Line2.Segment GetEdgeLine(float minNodeDistance, DynamicBuffer<Node> nodes, int index, bool isComplete, bool isCounterClockwise)
	{
		Line2.Segment result = default(Line2.Segment);
		result.a = AreaUtils.GetExpandedNode(nodes, index, -0.1f, isComplete, isCounterClockwise).xz;
		result.b = AreaUtils.GetExpandedNode(nodes, index, 0f - minNodeDistance, isComplete, isCounterClockwise).xz;
		return result;
	}

	private static Quad2 GetEdgeQuad(float minNodeDistance, DynamicBuffer<Node> nodes, int index, bool isComplete, bool isCounterClockwise)
	{
		int index2 = math.select(index - 1, index + nodes.Length - 1, index == 0);
		Quad2 result = default(Quad2);
		result.a = AreaUtils.GetExpandedNode(nodes, index2, 0f - minNodeDistance, isComplete, isCounterClockwise).xz;
		result.b = AreaUtils.GetExpandedNode(nodes, index2, -0.1f, isComplete, isCounterClockwise).xz;
		result.c = AreaUtils.GetExpandedNode(nodes, index, -0.1f, isComplete, isCounterClockwise).xz;
		result.d = AreaUtils.GetExpandedNode(nodes, index, 0f - minNodeDistance, isComplete, isCounterClockwise).xz;
		return result;
	}

	public static void ValidateTriangle(bool editorMode, bool noErrors, bool isCounterClockwise, Entity entity, Temp temp, Owner owner, Triangle triangle, ValidationSystem.EntityData data, NativeQuadTree<Entity, QuadTreeBoundsXZ> objectSearchTree, NativeQuadTree<Entity, QuadTreeBoundsXZ> netSearchTree, NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> areaSearchTree, WaterSurfaceData<SurfaceWater> waterSurfaceData, TerrainHeightData terrainHeightData, NativeQueue<ErrorData>.ParallelWriter errorQueue)
	{
		DynamicBuffer<Node> nodes = data.m_AreaNodes[entity];
		PrefabRef prefabRef = data.m_PrefabRef[entity];
		AreaGeometryData areaGeometryData = data.m_PrefabAreaGeometry[prefabRef.m_Prefab];
		Triangle3 triangle2 = AreaUtils.GetTriangle3(nodes, triangle);
		Bounds3 bounds = AreaUtils.GetBounds(triangle, triangle2, areaGeometryData);
		Bounds1 heightRange = triangle.m_HeightRange;
		heightRange.max += areaGeometryData.m_MaxHeight;
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
		Entity entity3 = Entity.Null;
		Entity ignoreEntity = Entity.Null;
		ErrorSeverity errorSeverity = ErrorSeverity.Error;
		if (noErrors)
		{
			entity3 = entity2;
			while (data.m_Owner.HasComponent(entity3))
			{
				entity3 = data.m_Owner[entity3].m_Owner;
			}
			if (data.m_Attachment.TryGetComponent(entity3, out var componentData))
			{
				ignoreEntity = componentData.m_Attached;
			}
			errorSeverity = ErrorSeverity.Warning;
		}
		AreaUtils.SetCollisionFlags(ref areaGeometryData, !editorMode || owner.m_Owner != Entity.Null);
		if ((areaGeometryData.m_Flags & GeometryFlags.PhysicalGeometry) != 0)
		{
			CollisionMask collisionMask = AreaUtils.GetCollisionMask(areaGeometryData);
			if ((temp.m_Flags & TempFlags.Delete) == 0)
			{
				ObjectIterator iterator = new ObjectIterator
				{
					m_AreaEntity = entity,
					m_OriginalAreaEntity = temp.m_Original,
					m_IgnoreEntity = entity3,
					m_IgnoreEntity2 = ignoreEntity,
					m_TriangleBounds = bounds,
					m_HeightRange = heightRange,
					m_Triangle = triangle2,
					m_ErrorSeverity = errorSeverity,
					m_CollisionMask = collisionMask,
					m_PrefabAreaData = areaGeometryData,
					m_Data = data,
					m_ErrorQueue = errorQueue,
					m_EditorMode = editorMode
				};
				objectSearchTree.Iterate(ref iterator);
			}
			if ((temp.m_Flags & TempFlags.Delete) == 0)
			{
				NetIterator iterator2 = new NetIterator
				{
					m_AreaEntity = entity,
					m_OriginalAreaEntity = temp.m_Original,
					m_IgnoreEntity = entity3,
					m_IgnoreEntity2 = ignoreEntity,
					m_TriangleBounds = bounds,
					m_HeightRange = heightRange,
					m_Triangle = triangle2,
					m_ErrorSeverity = errorSeverity,
					m_CollisionMask = collisionMask,
					m_Data = data,
					m_ErrorQueue = errorQueue,
					m_EditorMode = editorMode
				};
				netSearchTree.Iterate(ref iterator2);
			}
		}
		if ((areaGeometryData.m_Flags & (GeometryFlags.PhysicalGeometry | GeometryFlags.ProtectedArea)) != 0 || entity2 == entity || (!editorMode && ((temp.m_Flags & (TempFlags.Delete | TempFlags.Essential)) == TempFlags.Essential || (temp.m_Flags & TempFlags.Create) != 0)))
		{
			AreaIterator iterator3 = new AreaIterator
			{
				m_AreaEntity = entity,
				m_IgnoreEntity = entity3,
				m_IgnoreEntity2 = ignoreEntity,
				m_TopLevelEntity = entity2,
				m_TriangleBounds = bounds,
				m_IgnoreCollisions = ((temp.m_Flags & TempFlags.Delete) != 0),
				m_EditorMode = editorMode,
				m_Essential = ((temp.m_Flags & TempFlags.Essential) != 0),
				m_PrefabAreaData = areaGeometryData,
				m_ErrorSeverity = errorSeverity,
				m_Data = data,
				m_ErrorQueue = errorQueue
			};
			if (triangle.m_Indices.y == triangle.m_Indices.z)
			{
				iterator3.m_Triangle = AreaUtils.GetTriangle2(nodes, triangle);
			}
			else
			{
				iterator3.m_Triangle = AreaUtils.GetTriangle2(nodes, triangle, -0.1f, isCounterClockwise);
			}
			areaSearchTree.Iterate(ref iterator3);
		}
		if ((areaGeometryData.m_Flags & GeometryFlags.PhysicalGeometry) == 0 || (areaGeometryData.m_Flags & (GeometryFlags.OnWaterSurface | GeometryFlags.RequireWater)) == GeometryFlags.OnWaterSurface)
		{
			return;
		}
		float sampleInterval = WaterUtils.GetSampleInterval(ref waterSurfaceData);
		int2 @int = (int2)math.ceil(new float2(math.distance(triangle2.a.xz, triangle2.b.xz), math.distance(triangle2.a.xz, triangle2.c.xz)) / sampleInterval);
		float num = 1f / (float)math.max(1, @int.x);
		float num2 = areaGeometryData.m_SnapDistance * 0.01f;
		Bounds3 bounds2 = default(Bounds3);
		bounds2.min = float.MaxValue;
		bounds2.max = float.MinValue;
		bool flag = false;
		bool flag2 = false;
		OriginalAreaIterator iterator4 = default(OriginalAreaIterator);
		if (data.m_AreaNodes.TryGetBuffer(temp.m_Original, out var bufferData))
		{
			iterator4 = new OriginalAreaIterator
			{
				m_AreaEntity = temp.m_Original,
				m_Offset = num2,
				m_Nodes = bufferData,
				m_Triangles = data.m_AreaTriangles[temp.m_Original]
			};
		}
		for (int i = 0; i <= @int.x; i++)
		{
			float2 t = new float2
			{
				x = (float)i * num
			};
			int num3 = ((@int.x - i) * @int.y + (@int.x >> 1)) / math.max(1, @int.x);
			float num4 = (1f - t.x) / (float)math.max(1, num3);
			if ((areaGeometryData.m_Flags & GeometryFlags.RequireWater) != 0)
			{
				for (int j = 0; j <= num3; j++)
				{
					t.y = (float)j * num4;
					float3 @float = MathUtils.Position(triangle2, t);
					if (!(WaterUtils.SampleDepth(ref waterSurfaceData, @float) < 0.2f))
					{
						continue;
					}
					if (iterator4.m_Nodes.IsCreated)
					{
						iterator4.m_Bounds = new Bounds2(@float.xz - num2, @float.xz + num2);
						iterator4.m_Position = @float.xz;
						iterator4.m_Result = false;
						areaSearchTree.Iterate(ref iterator4);
						if (iterator4.m_Result)
						{
							continue;
						}
					}
					bounds2 |= @float;
					flag2 = true;
				}
				continue;
			}
			for (int k = 0; k <= num3; k++)
			{
				t.y = (float)k * num4;
				float3 float2 = MathUtils.Position(triangle2, t);
				if (!(WaterUtils.SampleDepth(ref waterSurfaceData, float2) >= 0.2f))
				{
					continue;
				}
				if (iterator4.m_Nodes.IsCreated)
				{
					iterator4.m_Bounds = new Bounds2(float2.xz - num2, float2.xz + num2);
					iterator4.m_Position = float2.xz;
					iterator4.m_Result = false;
					areaSearchTree.Iterate(ref iterator4);
					if (iterator4.m_Result)
					{
						continue;
					}
				}
				bounds2 |= float2;
				flag = true;
			}
		}
		if (flag)
		{
			errorQueue.Enqueue(new ErrorData
			{
				m_ErrorType = ErrorType.InWater,
				m_ErrorSeverity = ErrorSeverity.Error,
				m_TempEntity = entity,
				m_Position = MathUtils.Center(bounds2)
			});
		}
		if (flag2)
		{
			errorQueue.Enqueue(new ErrorData
			{
				m_ErrorType = ErrorType.NoWater,
				m_ErrorSeverity = ErrorSeverity.Error,
				m_TempEntity = entity,
				m_Position = MathUtils.Center(bounds2)
			});
		}
	}
}
