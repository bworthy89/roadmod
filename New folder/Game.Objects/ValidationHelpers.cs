using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Objects;

public static class ValidationHelpers
{
	private struct ObjectIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
	{
		public Entity m_ObjectEntity;

		public Entity m_TopLevelEntity;

		public Entity m_AssetStampEntity;

		public Bounds3 m_ObjectBounds;

		public Transform m_Transform;

		public Stack m_ObjectStack;

		public CollisionMask m_CollisionMask;

		public ObjectGeometryData m_PrefabObjectGeometryData;

		public StackData m_ObjectStackData;

		public bool m_CanOverride;

		public bool m_Optional;

		public bool m_EditorMode;

		public ValidationSystem.EntityData m_Data;

		public NativeQueue<ErrorData>.ParallelWriter m_ErrorQueue;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			if ((bounds.m_Mask & BoundsMask.NotOverridden) == 0)
			{
				return false;
			}
			if ((m_CollisionMask & CollisionMask.OnGround) != 0)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_ObjectBounds.xz);
			}
			return MathUtils.Intersect(bounds.m_Bounds, m_ObjectBounds);
		}

		public void Iterate(QuadTreeBoundsXZ bounds, Entity objectEntity2)
		{
			if ((bounds.m_Mask & BoundsMask.NotOverridden) == 0)
			{
				return;
			}
			if ((m_CollisionMask & CollisionMask.OnGround) != 0)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_ObjectBounds.xz))
				{
					return;
				}
			}
			else if (!MathUtils.Intersect(bounds.m_Bounds, m_ObjectBounds))
			{
				return;
			}
			if (m_Data.m_Hidden.HasComponent(objectEntity2) || objectEntity2 == m_AssetStampEntity)
			{
				return;
			}
			Entity entity = objectEntity2;
			bool hasOwner = false;
			Owner componentData;
			while (m_Data.m_Owner.TryGetComponent(entity, out componentData) && !m_Data.m_Building.HasComponent(entity))
			{
				Entity owner = componentData.m_Owner;
				hasOwner = true;
				if (m_Data.m_AssetStamp.HasComponent(owner))
				{
					if (!(owner == m_ObjectEntity))
					{
						break;
					}
					return;
				}
				entity = owner;
			}
			if (!(m_TopLevelEntity == entity))
			{
				CheckOverlap(entity, objectEntity2, bounds.m_Bounds, essential: false, hasOwner);
			}
		}

		public void CheckOverlap(Entity topLevelEntity2, Entity objectEntity2, Bounds3 bounds2, bool essential, bool hasOwner)
		{
			PrefabRef prefabRef = m_Data.m_PrefabRef[objectEntity2];
			Transform transform = m_Data.m_Transform[objectEntity2];
			if (!m_Data.m_PrefabObjectGeometry.HasComponent(prefabRef.m_Prefab))
			{
				return;
			}
			ObjectGeometryData objectGeometryData = m_Data.m_PrefabObjectGeometry[prefabRef.m_Prefab];
			if ((objectGeometryData.m_Flags & GeometryFlags.IgnoreSecondaryCollision) != GeometryFlags.None && m_Data.m_Secondary.HasComponent(objectEntity2))
			{
				return;
			}
			Elevation componentData;
			CollisionMask collisionMask = ((!m_Data.m_ObjectElevation.TryGetComponent(objectEntity2, out componentData)) ? ObjectUtils.GetCollisionMask(objectGeometryData, !m_EditorMode || hasOwner) : ObjectUtils.GetCollisionMask(objectGeometryData, componentData, !m_EditorMode || hasOwner));
			if ((m_CollisionMask & collisionMask) == 0)
			{
				return;
			}
			ErrorData error = new ErrorData
			{
				m_ErrorSeverity = ErrorSeverity.Error,
				m_TempEntity = m_ObjectEntity,
				m_PermanentEntity = objectEntity2
			};
			if (m_CanOverride)
			{
				error.m_ErrorSeverity = ErrorSeverity.Override;
				error.m_PermanentEntity = Entity.Null;
			}
			else if (!essential)
			{
				if (topLevelEntity2 != objectEntity2)
				{
					if (topLevelEntity2 != Entity.Null)
					{
						if ((objectGeometryData.m_Flags & (GeometryFlags.Overridable | GeometryFlags.DeleteOverridden)) == GeometryFlags.Overridable)
						{
							if ((m_PrefabObjectGeometryData.m_Flags & (GeometryFlags.Overridable | GeometryFlags.DeleteOverridden)) == GeometryFlags.Overridable)
							{
								if (m_Optional)
								{
									error.m_ErrorSeverity = ErrorSeverity.Warning;
								}
							}
							else
							{
								error.m_ErrorSeverity = ErrorSeverity.Override;
							}
						}
						else
						{
							PrefabRef prefabRef2 = m_Data.m_PrefabRef[topLevelEntity2];
							if (m_Data.m_PrefabObjectGeometry.HasComponent(prefabRef2.m_Prefab) && (m_Data.m_PrefabObjectGeometry[prefabRef2.m_Prefab].m_Flags & (GeometryFlags.Overridable | GeometryFlags.DeleteOverridden)) == (GeometryFlags.Overridable | GeometryFlags.DeleteOverridden) && !m_Data.m_Attached.HasComponent(topLevelEntity2) && (!m_Data.m_Temp.HasComponent(topLevelEntity2) || (m_Data.m_Temp[topLevelEntity2].m_Flags & TempFlags.Essential) == 0) && (m_Optional || (m_PrefabObjectGeometryData.m_Flags & (GeometryFlags.Overridable | GeometryFlags.DeleteOverridden)) != GeometryFlags.Overridable))
							{
								error.m_ErrorSeverity = ErrorSeverity.Warning;
								error.m_PermanentEntity = topLevelEntity2;
							}
						}
					}
				}
				else if ((objectGeometryData.m_Flags & GeometryFlags.Overridable) != GeometryFlags.None)
				{
					if ((objectGeometryData.m_Flags & GeometryFlags.DeleteOverridden) != GeometryFlags.None)
					{
						if (!m_Data.m_Attached.HasComponent(objectEntity2) && (m_Optional || (m_PrefabObjectGeometryData.m_Flags & (GeometryFlags.Overridable | GeometryFlags.DeleteOverridden)) != GeometryFlags.Overridable))
						{
							error.m_ErrorSeverity = ErrorSeverity.Warning;
						}
					}
					else if ((m_PrefabObjectGeometryData.m_Flags & (GeometryFlags.Overridable | GeometryFlags.DeleteOverridden)) == GeometryFlags.Overridable)
					{
						if (m_Optional)
						{
							error.m_ErrorSeverity = ErrorSeverity.Warning;
						}
					}
					else
					{
						error.m_ErrorSeverity = ErrorSeverity.Override;
					}
				}
			}
			float3 origin = MathUtils.Center(bounds2);
			StackData componentData2 = default(StackData);
			if (m_Data.m_Stack.TryGetComponent(objectEntity2, out var componentData3))
			{
				m_Data.m_PrefabStackData.TryGetComponent(prefabRef.m_Prefab, out componentData2);
			}
			if ((m_CollisionMask & CollisionMask.OnGround) == 0 || MathUtils.Intersect(bounds2, m_ObjectBounds))
			{
				CheckOverlap3D(ref error, transform, componentData3, objectGeometryData, componentData2, origin);
			}
			if (error.m_ErrorType == ErrorType.None && CommonUtils.ExclusiveGroundCollision(m_CollisionMask, collisionMask))
			{
				CheckOverlap2D(ref error, transform, objectGeometryData, bounds2, origin);
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

		private void CheckOverlap3D(ref ErrorData error, Transform transform2, Stack stack2, ObjectGeometryData prefabObjectGeometryData2, StackData stackData2, float3 origin)
		{
			quaternion q = math.inverse(m_Transform.m_Rotation);
			quaternion q2 = math.inverse(transform2.m_Rotation);
			float3 @float = math.mul(q, m_Transform.m_Position - origin);
			float3 float2 = math.mul(q2, transform2.m_Position - origin);
			Bounds3 bounds = ObjectUtils.GetBounds(m_ObjectStack, m_PrefabObjectGeometryData, m_ObjectStackData);
			if ((m_PrefabObjectGeometryData.m_Flags & GeometryFlags.IgnoreBottomCollision) != GeometryFlags.None)
			{
				bounds.min.y = math.max(bounds.min.y, 0f);
			}
			if (ObjectUtils.GetStandingLegCount(m_PrefabObjectGeometryData, out var legCount))
			{
				for (int i = 0; i < legCount; i++)
				{
					float3 float3 = @float + ObjectUtils.GetStandingLegOffset(m_PrefabObjectGeometryData, i);
					if ((m_PrefabObjectGeometryData.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
					{
						Cylinder3 cylinder = new Cylinder3
						{
							circle = new Circle2(m_PrefabObjectGeometryData.m_LegSize.x * 0.5f - 0.01f, float3.xz),
							height = new Bounds1(bounds.min.y + 0.01f, m_PrefabObjectGeometryData.m_LegSize.y + 0.01f) + float3.y,
							rotation = m_Transform.m_Rotation
						};
						Bounds3 bounds2 = ObjectUtils.GetBounds(stack2, prefabObjectGeometryData2, stackData2);
						if ((prefabObjectGeometryData2.m_Flags & GeometryFlags.IgnoreBottomCollision) != GeometryFlags.None)
						{
							bounds2.min.y = math.max(bounds2.min.y, 0f);
						}
						if (ObjectUtils.GetStandingLegCount(prefabObjectGeometryData2, out var legCount2))
						{
							for (int j = 0; j < legCount2; j++)
							{
								float3 float4 = float2 + ObjectUtils.GetStandingLegOffset(prefabObjectGeometryData2, j);
								if ((prefabObjectGeometryData2.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
								{
									if (ValidationHelpers.Intersect(cylinder2: new Cylinder3
									{
										circle = new Circle2(prefabObjectGeometryData2.m_LegSize.x * 0.5f - 0.01f, float4.xz),
										height = new Bounds1(bounds2.min.y + 0.01f, prefabObjectGeometryData2.m_LegSize.y + 0.01f) + float4.y,
										rotation = transform2.m_Rotation
									}, cylinder1: cylinder, pos: ref error.m_Position))
									{
										error.m_Position += origin;
										error.m_ErrorType = ErrorType.OverlapExisting;
									}
								}
								else if ((prefabObjectGeometryData2.m_Flags & GeometryFlags.IgnoreLegCollision) == 0)
								{
									Box3 box = new Box3
									{
										bounds = 
										{
											min = 
											{
												y = bounds2.min.y + 0.01f,
												xz = prefabObjectGeometryData2.m_LegSize.xz * -0.5f + 0.01f
											},
											max = 
											{
												y = prefabObjectGeometryData2.m_LegSize.y + 0.01f,
												xz = prefabObjectGeometryData2.m_LegSize.xz * 0.5f - 0.01f
											}
										}
									};
									box.bounds += float4;
									box.rotation = transform2.m_Rotation;
									if (MathUtils.Intersect(cylinder, box, out var cylinderIntersection, out var boxIntersection))
									{
										float3 start = math.mul(cylinder.rotation, MathUtils.Center(cylinderIntersection));
										float3 end = math.mul(box.rotation, MathUtils.Center(boxIntersection));
										error.m_Position = origin + math.lerp(start, end, 0.5f);
										error.m_ErrorType = ErrorType.OverlapExisting;
									}
								}
							}
							bounds2.min.y = prefabObjectGeometryData2.m_LegSize.y;
						}
						if ((prefabObjectGeometryData2.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
						{
							if (ValidationHelpers.Intersect(cylinder2: new Cylinder3
							{
								circle = new Circle2(prefabObjectGeometryData2.m_Size.x * 0.5f - 0.01f, float2.xz),
								height = new Bounds1(bounds2.min.y + 0.01f, bounds2.max.y - 0.01f) + float2.y,
								rotation = transform2.m_Rotation
							}, cylinder1: cylinder, pos: ref error.m_Position))
							{
								error.m_Position += origin;
								error.m_ErrorType = ErrorType.OverlapExisting;
							}
							continue;
						}
						Box3 box2 = default(Box3);
						box2.bounds = bounds2 + float2;
						box2.bounds = MathUtils.Expand(box2.bounds, -0.01f);
						box2.rotation = transform2.m_Rotation;
						if (MathUtils.Intersect(cylinder, box2, out var cylinderIntersection2, out var boxIntersection2))
						{
							float3 start2 = math.mul(cylinder.rotation, MathUtils.Center(cylinderIntersection2));
							float3 end2 = math.mul(box2.rotation, MathUtils.Center(boxIntersection2));
							error.m_Position = origin + math.lerp(start2, end2, 0.5f);
							error.m_ErrorType = ErrorType.OverlapExisting;
						}
					}
					else
					{
						if ((m_PrefabObjectGeometryData.m_Flags & GeometryFlags.IgnoreLegCollision) != GeometryFlags.None)
						{
							continue;
						}
						Box3 box3 = new Box3
						{
							bounds = 
							{
								min = 
								{
									y = bounds.min.y + 0.01f,
									xz = m_PrefabObjectGeometryData.m_LegSize.xz * -0.5f + 0.01f
								},
								max = 
								{
									y = m_PrefabObjectGeometryData.m_LegSize.y + 0.01f,
									xz = m_PrefabObjectGeometryData.m_LegSize.xz * 0.5f - 0.01f
								}
							}
						};
						box3.bounds += float3;
						box3.rotation = m_Transform.m_Rotation;
						Bounds3 bounds3 = ObjectUtils.GetBounds(stack2, prefabObjectGeometryData2, stackData2);
						if ((prefabObjectGeometryData2.m_Flags & GeometryFlags.IgnoreBottomCollision) != GeometryFlags.None)
						{
							bounds3.min.y = math.max(bounds3.min.y, 0f);
						}
						if (ObjectUtils.GetStandingLegCount(prefabObjectGeometryData2, out var legCount3))
						{
							for (int k = 0; k < legCount3; k++)
							{
								float3 float5 = float2 + ObjectUtils.GetStandingLegOffset(prefabObjectGeometryData2, k);
								if ((prefabObjectGeometryData2.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
								{
									Cylinder3 cylinder2 = new Cylinder3
									{
										circle = new Circle2(prefabObjectGeometryData2.m_LegSize.x * 0.5f - 0.01f, float5.xz),
										height = new Bounds1(bounds3.min.y + 0.01f, prefabObjectGeometryData2.m_LegSize.y + 0.01f) + float5.y,
										rotation = transform2.m_Rotation
									};
									if (MathUtils.Intersect(cylinder2, box3, out var cylinderIntersection3, out var boxIntersection3))
									{
										float3 start3 = math.mul(box3.rotation, MathUtils.Center(boxIntersection3));
										float3 end3 = math.mul(cylinder2.rotation, MathUtils.Center(cylinderIntersection3));
										error.m_Position = origin + math.lerp(start3, end3, 0.5f);
										error.m_ErrorType = ErrorType.OverlapExisting;
									}
								}
								else if ((prefabObjectGeometryData2.m_Flags & GeometryFlags.IgnoreLegCollision) == 0)
								{
									Box3 box4 = new Box3
									{
										bounds = 
										{
											min = 
											{
												y = bounds3.min.y + 0.01f,
												xz = prefabObjectGeometryData2.m_LegSize.xz * -0.5f + 0.01f
											},
											max = 
											{
												y = prefabObjectGeometryData2.m_LegSize.y + 0.01f,
												xz = prefabObjectGeometryData2.m_LegSize.xz * 0.5f - 0.01f
											}
										}
									};
									box4.bounds += float5;
									box4.rotation = transform2.m_Rotation;
									if (MathUtils.Intersect(box3, box4, out var intersection, out var intersection2))
									{
										float3 start4 = math.mul(box3.rotation, MathUtils.Center(intersection));
										float3 end4 = math.mul(box4.rotation, MathUtils.Center(intersection2));
										error.m_Position = origin + math.lerp(start4, end4, 0.5f);
										error.m_ErrorType = ErrorType.OverlapExisting;
									}
								}
							}
							bounds3.min.y = prefabObjectGeometryData2.m_LegSize.y;
						}
						if ((prefabObjectGeometryData2.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
						{
							Cylinder3 cylinder3 = new Cylinder3
							{
								circle = new Circle2(prefabObjectGeometryData2.m_Size.x * 0.5f - 0.01f, float2.xz),
								height = new Bounds1(bounds3.min.y + 0.01f, bounds3.max.y - 0.01f) + float2.y,
								rotation = transform2.m_Rotation
							};
							if (MathUtils.Intersect(cylinder3, box3, out var cylinderIntersection4, out var boxIntersection4))
							{
								float3 start5 = math.mul(box3.rotation, MathUtils.Center(boxIntersection4));
								float3 end5 = math.mul(cylinder3.rotation, MathUtils.Center(cylinderIntersection4));
								error.m_Position = origin + math.lerp(start5, end5, 0.5f);
								error.m_ErrorType = ErrorType.OverlapExisting;
							}
						}
						else
						{
							Box3 box5 = default(Box3);
							box5.bounds = bounds3 + float2;
							box5.bounds = MathUtils.Expand(box5.bounds, -0.01f);
							box5.rotation = transform2.m_Rotation;
							if (MathUtils.Intersect(box3, box5, out var intersection3, out var intersection4))
							{
								float3 start6 = math.mul(box3.rotation, MathUtils.Center(intersection3));
								float3 end6 = math.mul(box5.rotation, MathUtils.Center(intersection4));
								error.m_Position = origin + math.lerp(start6, end6, 0.5f);
								error.m_ErrorType = ErrorType.OverlapExisting;
							}
						}
					}
				}
				bounds.min.y = m_PrefabObjectGeometryData.m_LegSize.y;
			}
			if ((m_PrefabObjectGeometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
			{
				Cylinder3 cylinder4 = new Cylinder3
				{
					circle = new Circle2(m_PrefabObjectGeometryData.m_Size.x * 0.5f - 0.01f, @float.xz),
					height = new Bounds1(bounds.min.y + 0.01f, bounds.max.y - 0.01f) + @float.y,
					rotation = m_Transform.m_Rotation
				};
				Bounds3 bounds4 = ObjectUtils.GetBounds(stack2, prefabObjectGeometryData2, stackData2);
				if ((prefabObjectGeometryData2.m_Flags & GeometryFlags.IgnoreBottomCollision) != GeometryFlags.None)
				{
					bounds4.min.y = math.max(bounds4.min.y, 0f);
				}
				if (ObjectUtils.GetStandingLegCount(prefabObjectGeometryData2, out var legCount4))
				{
					for (int l = 0; l < legCount4; l++)
					{
						float3 float6 = float2 + ObjectUtils.GetStandingLegOffset(prefabObjectGeometryData2, l);
						if ((prefabObjectGeometryData2.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
						{
							if (ValidationHelpers.Intersect(cylinder2: new Cylinder3
							{
								circle = new Circle2(prefabObjectGeometryData2.m_LegSize.x * 0.5f - 0.01f, float6.xz),
								height = new Bounds1(bounds4.min.y + 0.01f, prefabObjectGeometryData2.m_LegSize.y + 0.01f) + float6.y,
								rotation = transform2.m_Rotation
							}, cylinder1: cylinder4, pos: ref error.m_Position))
							{
								error.m_Position += origin;
								error.m_ErrorType = ErrorType.OverlapExisting;
							}
						}
						else if ((prefabObjectGeometryData2.m_Flags & GeometryFlags.IgnoreLegCollision) == 0)
						{
							Box3 box6 = new Box3
							{
								bounds = 
								{
									min = 
									{
										y = bounds4.min.y + 0.01f,
										xz = prefabObjectGeometryData2.m_LegSize.xz * -0.5f + 0.01f
									},
									max = 
									{
										y = prefabObjectGeometryData2.m_LegSize.y + 0.01f,
										xz = prefabObjectGeometryData2.m_LegSize.xz * 0.5f - 0.01f
									}
								}
							};
							box6.bounds += float6;
							box6.rotation = transform2.m_Rotation;
							if (MathUtils.Intersect(cylinder4, box6, out var cylinderIntersection5, out var boxIntersection5))
							{
								float3 start7 = math.mul(cylinder4.rotation, MathUtils.Center(cylinderIntersection5));
								float3 end7 = math.mul(box6.rotation, MathUtils.Center(boxIntersection5));
								error.m_Position = origin + math.lerp(start7, end7, 0.5f);
								error.m_ErrorType = ErrorType.OverlapExisting;
							}
						}
					}
					bounds4.min.y = prefabObjectGeometryData2.m_LegSize.y;
				}
				if ((prefabObjectGeometryData2.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
				{
					if (ValidationHelpers.Intersect(cylinder2: new Cylinder3
					{
						circle = new Circle2(prefabObjectGeometryData2.m_Size.x * 0.5f - 0.01f, float2.xz),
						height = new Bounds1(bounds4.min.y + 0.01f, bounds4.max.y - 0.01f) + float2.y,
						rotation = transform2.m_Rotation
					}, cylinder1: cylinder4, pos: ref error.m_Position))
					{
						error.m_Position += origin;
						error.m_ErrorType = ErrorType.OverlapExisting;
					}
					return;
				}
				Box3 box7 = default(Box3);
				box7.bounds = bounds4 + float2;
				box7.bounds = MathUtils.Expand(box7.bounds, -0.01f);
				box7.rotation = transform2.m_Rotation;
				if (MathUtils.Intersect(cylinder4, box7, out var cylinderIntersection6, out var boxIntersection6))
				{
					float3 start8 = math.mul(cylinder4.rotation, MathUtils.Center(cylinderIntersection6));
					float3 end8 = math.mul(box7.rotation, MathUtils.Center(boxIntersection6));
					error.m_Position = origin + math.lerp(start8, end8, 0.5f);
					error.m_ErrorType = ErrorType.OverlapExisting;
				}
				return;
			}
			Box3 box8 = default(Box3);
			box8.bounds = bounds + @float;
			box8.bounds = MathUtils.Expand(box8.bounds, -0.01f);
			box8.rotation = m_Transform.m_Rotation;
			Bounds3 bounds5 = ObjectUtils.GetBounds(stack2, prefabObjectGeometryData2, stackData2);
			if ((prefabObjectGeometryData2.m_Flags & GeometryFlags.IgnoreBottomCollision) != GeometryFlags.None)
			{
				bounds5.min.y = math.max(bounds5.min.y, 0f);
			}
			if (ObjectUtils.GetStandingLegCount(prefabObjectGeometryData2, out var legCount5))
			{
				for (int m = 0; m < legCount5; m++)
				{
					float3 float7 = float2 + ObjectUtils.GetStandingLegOffset(prefabObjectGeometryData2, m);
					if ((prefabObjectGeometryData2.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
					{
						Cylinder3 cylinder5 = new Cylinder3
						{
							circle = new Circle2(prefabObjectGeometryData2.m_LegSize.x * 0.5f - 0.01f, float7.xz),
							height = new Bounds1(bounds5.min.y + 0.01f, prefabObjectGeometryData2.m_LegSize.y + 0.01f) + float7.y,
							rotation = transform2.m_Rotation
						};
						if (MathUtils.Intersect(cylinder5, box8, out var cylinderIntersection7, out var boxIntersection7))
						{
							float3 start9 = math.mul(box8.rotation, MathUtils.Center(boxIntersection7));
							float3 end9 = math.mul(cylinder5.rotation, MathUtils.Center(cylinderIntersection7));
							error.m_Position = origin + math.lerp(start9, end9, 0.5f);
							error.m_ErrorType = ErrorType.OverlapExisting;
						}
					}
					else if ((prefabObjectGeometryData2.m_Flags & GeometryFlags.IgnoreLegCollision) == 0)
					{
						Box3 box9 = new Box3
						{
							bounds = 
							{
								min = 
								{
									y = bounds5.min.y + 0.01f,
									xz = prefabObjectGeometryData2.m_LegSize.xz * -0.5f + 0.01f
								},
								max = 
								{
									y = prefabObjectGeometryData2.m_LegSize.y + 0.01f,
									xz = prefabObjectGeometryData2.m_LegSize.xz * 0.5f - 0.01f
								}
							}
						};
						box9.bounds += float7;
						box9.rotation = transform2.m_Rotation;
						if (MathUtils.Intersect(box8, box9, out var intersection5, out var intersection6))
						{
							float3 start10 = math.mul(box8.rotation, MathUtils.Center(intersection5));
							float3 end10 = math.mul(box9.rotation, MathUtils.Center(intersection6));
							error.m_Position = origin + math.lerp(start10, end10, 0.5f);
							error.m_ErrorType = ErrorType.OverlapExisting;
						}
					}
				}
				bounds5.min.y = prefabObjectGeometryData2.m_LegSize.y;
			}
			if ((prefabObjectGeometryData2.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
			{
				Cylinder3 cylinder6 = new Cylinder3
				{
					circle = new Circle2(prefabObjectGeometryData2.m_Size.x * 0.5f - 0.01f, float2.xz),
					height = new Bounds1(bounds5.min.y + 0.01f, bounds5.max.y - 0.01f) + float2.y,
					rotation = transform2.m_Rotation
				};
				if (MathUtils.Intersect(cylinder6, box8, out var cylinderIntersection8, out var boxIntersection8))
				{
					float3 start11 = math.mul(box8.rotation, MathUtils.Center(boxIntersection8));
					float3 end11 = math.mul(cylinder6.rotation, MathUtils.Center(cylinderIntersection8));
					error.m_Position = origin + math.lerp(start11, end11, 0.5f);
					error.m_ErrorType = ErrorType.OverlapExisting;
				}
			}
			else
			{
				Box3 box10 = default(Box3);
				box10.bounds = bounds5 + float2;
				box10.bounds = MathUtils.Expand(box10.bounds, -0.01f);
				box10.rotation = transform2.m_Rotation;
				if (MathUtils.Intersect(box8, box10, out var intersection7, out var intersection8))
				{
					float3 start12 = math.mul(box8.rotation, MathUtils.Center(intersection7));
					float3 end12 = math.mul(box10.rotation, MathUtils.Center(intersection8));
					error.m_Position = origin + math.lerp(start12, end12, 0.5f);
					error.m_ErrorType = ErrorType.OverlapExisting;
				}
			}
		}

		private void CheckOverlap2D(ref ErrorData error, Transform transformData2, ObjectGeometryData prefabObjectGeometryData2, Bounds3 bounds2, float3 origin)
		{
			if (ObjectUtils.GetStandingLegCount(m_PrefabObjectGeometryData, out var legCount))
			{
				for (int i = 0; i < legCount; i++)
				{
					float3 position = ObjectUtils.GetStandingLegPosition(m_PrefabObjectGeometryData, m_Transform, i) - origin;
					if ((m_PrefabObjectGeometryData.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
					{
						Circle2 circle = new Circle2(m_PrefabObjectGeometryData.m_LegSize.x * 0.5f - 0.01f, position.xz);
						Bounds2 intersection2;
						if (ObjectUtils.GetStandingLegCount(prefabObjectGeometryData2, out var legCount2))
						{
							for (int j = 0; j < legCount2; j++)
							{
								float3 position2 = ObjectUtils.GetStandingLegPosition(prefabObjectGeometryData2, transformData2, j) - origin;
								Bounds2 intersection;
								if ((prefabObjectGeometryData2.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
								{
									Circle2 circle2 = new Circle2(prefabObjectGeometryData2.m_LegSize.x * 0.5f - 0.01f, position2.xz);
									if (MathUtils.Intersect(circle, circle2))
									{
										error.m_Position.xz = origin.xz + MathUtils.Center(MathUtils.Bounds(circle) & MathUtils.Bounds(circle2));
										error.m_Position.y = MathUtils.Center(bounds2.y & m_ObjectBounds.y);
										error.m_ErrorType = ErrorType.OverlapExisting;
									}
								}
								else if ((prefabObjectGeometryData2.m_Flags & GeometryFlags.IgnoreLegCollision) == 0 && MathUtils.Intersect(ObjectUtils.CalculateBaseCorners(bounds: MathUtils.Expand(new Bounds3
								{
									min = 
									{
										xz = prefabObjectGeometryData2.m_LegSize.xz * -0.5f
									},
									max = 
									{
										xz = prefabObjectGeometryData2.m_LegSize.xz * 0.5f
									}
								}, -0.01f), position: position2, rotation: transformData2.m_Rotation).xz, circle, out intersection))
								{
									error.m_Position.xz = origin.xz + MathUtils.Center(intersection);
									error.m_Position.y = MathUtils.Center(bounds2.y & m_ObjectBounds.y);
									error.m_ErrorType = ErrorType.OverlapExisting;
								}
							}
						}
						else if ((prefabObjectGeometryData2.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
						{
							Circle2 circle3 = new Circle2(prefabObjectGeometryData2.m_Size.x * 0.5f - 0.01f, (transformData2.m_Position - origin).xz);
							if (MathUtils.Intersect(circle, circle3))
							{
								error.m_Position.xz = origin.xz + MathUtils.Center(MathUtils.Bounds(circle) & MathUtils.Bounds(circle3));
								error.m_Position.y = MathUtils.Center(bounds2.y & m_ObjectBounds.y);
								error.m_ErrorType = ErrorType.OverlapExisting;
							}
						}
						else if (MathUtils.Intersect(ObjectUtils.CalculateBaseCorners(transformData2.m_Position - origin, transformData2.m_Rotation, MathUtils.Expand(prefabObjectGeometryData2.m_Bounds, -0.01f)).xz, circle, out intersection2))
						{
							error.m_Position.xz = origin.xz + MathUtils.Center(intersection2);
							error.m_Position.y = MathUtils.Center(bounds2.y & m_ObjectBounds.y);
							error.m_ErrorType = ErrorType.OverlapExisting;
						}
					}
					else
					{
						if ((m_PrefabObjectGeometryData.m_Flags & GeometryFlags.IgnoreLegCollision) != GeometryFlags.None)
						{
							continue;
						}
						Quad2 xz = ObjectUtils.CalculateBaseCorners(bounds: MathUtils.Expand(new Bounds3
						{
							min = 
							{
								xz = m_PrefabObjectGeometryData.m_LegSize.xz * -0.5f
							},
							max = 
							{
								xz = m_PrefabObjectGeometryData.m_LegSize.xz * 0.5f
							}
						}, -0.01f), position: position, rotation: m_Transform.m_Rotation).xz;
						if (ObjectUtils.GetStandingLegCount(prefabObjectGeometryData2, out var legCount3))
						{
							for (int k = 0; k < legCount3; k++)
							{
								float3 position3 = ObjectUtils.GetStandingLegPosition(prefabObjectGeometryData2, transformData2, k) - origin;
								if ((prefabObjectGeometryData2.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
								{
									Circle2 circle4 = new Circle2(prefabObjectGeometryData2.m_LegSize.x * 0.5f - 0.01f, position3.xz);
									if (MathUtils.Intersect(xz, circle4, out var intersection3))
									{
										error.m_Position.xz = origin.xz + MathUtils.Center(intersection3);
										error.m_Position.y = MathUtils.Center(bounds2.y & m_ObjectBounds.y);
										error.m_ErrorType = ErrorType.OverlapExisting;
									}
								}
								else if ((prefabObjectGeometryData2.m_Flags & GeometryFlags.IgnoreLegCollision) == 0)
								{
									Quad2 xz2 = ObjectUtils.CalculateBaseCorners(bounds: MathUtils.Expand(new Bounds3
									{
										min = 
										{
											xz = prefabObjectGeometryData2.m_LegSize.xz * -0.5f
										},
										max = 
										{
											xz = prefabObjectGeometryData2.m_LegSize.xz * 0.5f
										}
									}, -0.01f), position: position3, rotation: transformData2.m_Rotation).xz;
									if (MathUtils.Intersect(xz, xz2, out var intersection4))
									{
										error.m_Position.xz = origin.xz + MathUtils.Center(intersection4);
										error.m_Position.y = MathUtils.Center(bounds2.y & m_ObjectBounds.y);
										error.m_ErrorType = ErrorType.OverlapExisting;
									}
								}
							}
						}
						else if ((prefabObjectGeometryData2.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
						{
							Circle2 circle5 = new Circle2(prefabObjectGeometryData2.m_Size.x * 0.5f - 0.01f, (transformData2.m_Position - origin).xz);
							if (MathUtils.Intersect(xz, circle5, out var intersection5))
							{
								error.m_Position.xz = origin.xz + MathUtils.Center(intersection5);
								error.m_Position.y = MathUtils.Center(bounds2.y & m_ObjectBounds.y);
								error.m_ErrorType = ErrorType.OverlapExisting;
							}
						}
						else
						{
							Quad2 xz3 = ObjectUtils.CalculateBaseCorners(transformData2.m_Position - origin, transformData2.m_Rotation, MathUtils.Expand(prefabObjectGeometryData2.m_Bounds, -0.01f)).xz;
							if (MathUtils.Intersect(xz, xz3, out var intersection6))
							{
								error.m_Position.xz = origin.xz + MathUtils.Center(intersection6);
								error.m_Position.y = MathUtils.Center(bounds2.y & m_ObjectBounds.y);
								error.m_ErrorType = ErrorType.OverlapExisting;
							}
						}
					}
				}
				return;
			}
			if ((m_PrefabObjectGeometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
			{
				Circle2 circle6 = new Circle2(m_PrefabObjectGeometryData.m_Size.x * 0.5f - 0.01f, (m_Transform.m_Position - origin).xz);
				Bounds2 intersection8;
				if (ObjectUtils.GetStandingLegCount(prefabObjectGeometryData2, out var legCount4))
				{
					for (int l = 0; l < legCount4; l++)
					{
						float3 position4 = ObjectUtils.GetStandingLegPosition(prefabObjectGeometryData2, transformData2, l) - origin;
						Bounds2 intersection7;
						if ((prefabObjectGeometryData2.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
						{
							Circle2 circle7 = new Circle2(prefabObjectGeometryData2.m_LegSize.x * 0.5f - 0.01f, position4.xz);
							if (MathUtils.Intersect(circle6, circle7))
							{
								error.m_Position.xz = origin.xz + MathUtils.Center(MathUtils.Bounds(circle6) & MathUtils.Bounds(circle7));
								error.m_Position.y = MathUtils.Center(bounds2.y & m_ObjectBounds.y);
								error.m_ErrorType = ErrorType.OverlapExisting;
							}
						}
						else if ((prefabObjectGeometryData2.m_Flags & GeometryFlags.IgnoreLegCollision) == 0 && MathUtils.Intersect(ObjectUtils.CalculateBaseCorners(bounds: MathUtils.Expand(new Bounds3
						{
							min = 
							{
								xz = prefabObjectGeometryData2.m_LegSize.xz * -0.5f
							},
							max = 
							{
								xz = prefabObjectGeometryData2.m_LegSize.xz * 0.5f
							}
						}, -0.01f), position: position4, rotation: transformData2.m_Rotation).xz, circle6, out intersection7))
						{
							error.m_Position.xz = origin.xz + MathUtils.Center(intersection7);
							error.m_Position.y = MathUtils.Center(bounds2.y & m_ObjectBounds.y);
							error.m_ErrorType = ErrorType.OverlapExisting;
						}
					}
				}
				else if ((prefabObjectGeometryData2.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
				{
					Circle2 circle8 = new Circle2(prefabObjectGeometryData2.m_Size.x * 0.5f - 0.01f, (transformData2.m_Position - origin).xz);
					if (MathUtils.Intersect(circle6, circle8))
					{
						error.m_Position.xz = origin.xz + MathUtils.Center(MathUtils.Bounds(circle6) & MathUtils.Bounds(circle8));
						error.m_Position.y = MathUtils.Center(bounds2.y & m_ObjectBounds.y);
						error.m_ErrorType = ErrorType.OverlapExisting;
					}
				}
				else if (MathUtils.Intersect(ObjectUtils.CalculateBaseCorners(transformData2.m_Position - origin, transformData2.m_Rotation, MathUtils.Expand(prefabObjectGeometryData2.m_Bounds, -0.01f)).xz, circle6, out intersection8))
				{
					error.m_Position.xz = origin.xz + MathUtils.Center(intersection8);
					error.m_Position.y = MathUtils.Center(bounds2.y & m_ObjectBounds.y);
					error.m_ErrorType = ErrorType.OverlapExisting;
				}
				return;
			}
			Quad2 xz4 = ObjectUtils.CalculateBaseCorners(m_Transform.m_Position - origin, m_Transform.m_Rotation, MathUtils.Expand(m_PrefabObjectGeometryData.m_Bounds, -0.01f)).xz;
			if (ObjectUtils.GetStandingLegCount(prefabObjectGeometryData2, out var legCount5))
			{
				for (int m = 0; m < legCount5; m++)
				{
					float3 position5 = ObjectUtils.GetStandingLegPosition(prefabObjectGeometryData2, transformData2, m) - origin;
					if ((prefabObjectGeometryData2.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
					{
						Circle2 circle9 = new Circle2(prefabObjectGeometryData2.m_LegSize.x * 0.5f - 0.01f, position5.xz);
						if (MathUtils.Intersect(xz4, circle9, out var intersection9))
						{
							error.m_Position.xz = origin.xz + MathUtils.Center(intersection9);
							error.m_Position.y = MathUtils.Center(bounds2.y & m_ObjectBounds.y);
							error.m_ErrorType = ErrorType.OverlapExisting;
						}
					}
					else if ((prefabObjectGeometryData2.m_Flags & GeometryFlags.IgnoreLegCollision) == 0)
					{
						Quad2 xz5 = ObjectUtils.CalculateBaseCorners(bounds: MathUtils.Expand(new Bounds3
						{
							min = 
							{
								xz = prefabObjectGeometryData2.m_LegSize.xz * -0.5f
							},
							max = 
							{
								xz = prefabObjectGeometryData2.m_LegSize.xz * 0.5f
							}
						}, -0.01f), position: position5, rotation: transformData2.m_Rotation).xz;
						if (MathUtils.Intersect(xz4, xz5, out var intersection10))
						{
							error.m_Position.xz = origin.xz + MathUtils.Center(intersection10);
							error.m_Position.y = MathUtils.Center(bounds2.y & m_ObjectBounds.y);
							error.m_ErrorType = ErrorType.OverlapExisting;
						}
					}
				}
			}
			else if ((prefabObjectGeometryData2.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
			{
				Circle2 circle10 = new Circle2(prefabObjectGeometryData2.m_Size.x * 0.5f - 0.01f, (transformData2.m_Position - origin).xz);
				if (MathUtils.Intersect(xz4, circle10, out var intersection11))
				{
					error.m_Position.xz = origin.xz + MathUtils.Center(intersection11);
					error.m_Position.y = MathUtils.Center(bounds2.y & m_ObjectBounds.y);
					error.m_ErrorType = ErrorType.OverlapExisting;
				}
			}
			else
			{
				Quad2 xz6 = ObjectUtils.CalculateBaseCorners(transformData2.m_Position - origin, transformData2.m_Rotation, MathUtils.Expand(prefabObjectGeometryData2.m_Bounds, -0.01f)).xz;
				if (MathUtils.Intersect(xz4, xz6, out var intersection12))
				{
					error.m_Position.xz = origin.xz + MathUtils.Center(intersection12);
					error.m_Position.y = MathUtils.Center(bounds2.y & m_ObjectBounds.y);
					error.m_ErrorType = ErrorType.OverlapExisting;
				}
			}
		}
	}

	private struct NetIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
	{
		public Entity m_ObjectEntity;

		public Entity m_AttachedParent;

		public Entity m_TopLevelEntity;

		public Entity m_EdgeEntity;

		public Entity m_NodeEntity;

		public Entity m_IgnoreNode;

		public Edge m_OwnerNodes;

		public Bounds3 m_ObjectBounds;

		public Transform m_Transform;

		public Stack m_ObjectStack;

		public CollisionMask m_CollisionMask;

		public ObjectGeometryData m_PrefabObjectGeometryData;

		public BuildingData m_PrefabBuildingData;

		public StackData m_ObjectStackData;

		public bool m_Optional;

		public bool m_EditorMode;

		public ValidationSystem.EntityData m_Data;

		public NativeQueue<ErrorData>.ParallelWriter m_ErrorQueue;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			if ((m_CollisionMask & CollisionMask.OnGround) != 0)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_ObjectBounds.xz);
			}
			return MathUtils.Intersect(bounds.m_Bounds, m_ObjectBounds);
		}

		public void Iterate(QuadTreeBoundsXZ bounds, Entity edgeEntity2)
		{
			if ((m_CollisionMask & CollisionMask.OnGround) != 0)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_ObjectBounds.xz))
				{
					return;
				}
			}
			else if (!MathUtils.Intersect(bounds.m_Bounds, m_ObjectBounds))
			{
				return;
			}
			if (m_Data.m_Hidden.HasComponent(edgeEntity2) || !m_Data.m_EdgeGeometry.HasComponent(edgeEntity2))
			{
				return;
			}
			Edge edgeData = m_Data.m_Edge[edgeEntity2];
			bool flag = true;
			bool flag2 = true;
			if (edgeEntity2 == m_AttachedParent || edgeData.m_Start == m_AttachedParent || edgeData.m_End == m_AttachedParent)
			{
				return;
			}
			Entity entity = m_ObjectEntity;
			while (m_Data.m_Owner.HasComponent(entity))
			{
				entity = m_Data.m_Owner[entity].m_Owner;
				if (m_Data.m_Temp.HasComponent(entity))
				{
					Temp temp = m_Data.m_Temp[entity];
					if (temp.m_Original != Entity.Null)
					{
						entity = temp.m_Original;
					}
				}
				if (edgeEntity2 == entity || edgeData.m_Start == entity || edgeData.m_End == entity)
				{
					return;
				}
			}
			Entity entity2 = edgeEntity2;
			bool hasOwner = false;
			Owner componentData;
			while (m_Data.m_Owner.TryGetComponent(entity2, out componentData) && !m_Data.m_Building.HasComponent(entity2))
			{
				Entity owner = componentData.m_Owner;
				hasOwner = true;
				if (m_Data.m_AssetStamp.HasComponent(owner))
				{
					if (!(owner == m_ObjectEntity))
					{
						break;
					}
					return;
				}
				entity2 = owner;
			}
			if (!(m_TopLevelEntity == entity2) && ((!(m_EdgeEntity != Entity.Null) && !(m_NodeEntity != Entity.Null)) || ((!m_Data.m_Owner.TryGetComponent(edgeEntity2, out var componentData2) || !m_Data.m_Edge.TryGetComponent(componentData2.m_Owner, out var componentData3) || (!(m_NodeEntity == componentData3.m_Start) && !(m_NodeEntity == componentData3.m_End))) && (!m_Data.m_Owner.TryGetComponent(edgeData.m_Start, out componentData2) || (!(componentData2.m_Owner == m_EdgeEntity) && (!m_Data.m_Edge.TryGetComponent(componentData2.m_Owner, out componentData3) || (!(m_NodeEntity == componentData3.m_Start) && !(m_NodeEntity == componentData3.m_End))))) && (!m_Data.m_Owner.TryGetComponent(edgeData.m_End, out componentData2) || (!(componentData2.m_Owner == m_EdgeEntity) && (!m_Data.m_Edge.TryGetComponent(componentData2.m_Owner, out componentData3) || (!(m_NodeEntity == componentData3.m_Start) && !(m_NodeEntity == componentData3.m_End))))))))
			{
				Composition compositionData = m_Data.m_Composition[edgeEntity2];
				EdgeGeometry edgeGeometryData = m_Data.m_EdgeGeometry[edgeEntity2];
				StartNodeGeometry startNodeGeometryData = m_Data.m_StartNodeGeometry[edgeEntity2];
				EndNodeGeometry endNodeGeometryData = m_Data.m_EndNodeGeometry[edgeEntity2];
				float3 origin = MathUtils.Center(bounds.m_Bounds);
				flag &= edgeData.m_Start != m_OwnerNodes.m_Start && edgeData.m_Start != m_OwnerNodes.m_End;
				flag2 &= edgeData.m_End != m_OwnerNodes.m_Start && edgeData.m_End != m_OwnerNodes.m_End;
				if (edgeData.m_Start == m_IgnoreNode)
				{
					flag &= (m_Data.m_PrefabComposition[compositionData.m_StartNode].m_Flags.m_General & CompositionFlags.General.Roundabout) == 0;
				}
				if (edgeData.m_End == m_IgnoreNode)
				{
					flag2 &= (m_Data.m_PrefabComposition[compositionData.m_EndNode].m_Flags.m_General & CompositionFlags.General.Roundabout) == 0;
				}
				CheckOverlap(entity2, edgeEntity2, bounds.m_Bounds, edgeData, compositionData, edgeGeometryData, startNodeGeometryData, endNodeGeometryData, origin, flag, flag2, essential: false, hasOwner);
			}
		}

		public void CheckOverlap(Entity topLevelEntity2, Entity edgeEntity2, Bounds3 bounds2, Edge edgeData2, Composition compositionData2, EdgeGeometry edgeGeometryData2, StartNodeGeometry startNodeGeometryData2, EndNodeGeometry endNodeGeometryData2, float3 origin, bool checkStartNode, bool checkEndNode, bool essential, bool hasOwner)
		{
			NetCompositionData netCompositionData = m_Data.m_PrefabComposition[compositionData2.m_Edge];
			NetCompositionData netCompositionData2 = m_Data.m_PrefabComposition[compositionData2.m_StartNode];
			NetCompositionData netCompositionData3 = m_Data.m_PrefabComposition[compositionData2.m_EndNode];
			CollisionMask collisionMask = NetUtils.GetCollisionMask(netCompositionData, !m_EditorMode || hasOwner);
			CollisionMask collisionMask2 = NetUtils.GetCollisionMask(netCompositionData2, !m_EditorMode || hasOwner);
			CollisionMask collisionMask3 = NetUtils.GetCollisionMask(netCompositionData3, !m_EditorMode || hasOwner);
			if (!checkStartNode)
			{
				collisionMask2 = (CollisionMask)0;
			}
			if (!checkEndNode)
			{
				collisionMask3 = (CollisionMask)0;
			}
			CollisionMask collisionMask4 = collisionMask | collisionMask2 | collisionMask3;
			if ((m_CollisionMask & collisionMask4) == 0)
			{
				return;
			}
			DynamicBuffer<NetCompositionArea> edgeCompositionAreas = default(DynamicBuffer<NetCompositionArea>);
			DynamicBuffer<NetCompositionArea> startCompositionAreas = default(DynamicBuffer<NetCompositionArea>);
			DynamicBuffer<NetCompositionArea> endCompositionAreas = default(DynamicBuffer<NetCompositionArea>);
			bool flag = (m_PrefabObjectGeometryData.m_Flags & (GeometryFlags.Overridable | GeometryFlags.DeleteOverridden)) != GeometryFlags.Overridable;
			if (flag && math.any(m_PrefabBuildingData.m_LotSize != 0))
			{
				flag = (m_PrefabBuildingData.m_Flags & Game.Prefabs.BuildingFlags.CanBeOnRoadArea) != 0;
			}
			if (flag)
			{
				edgeCompositionAreas = m_Data.m_PrefabCompositionAreas[compositionData2.m_Edge];
				startCompositionAreas = m_Data.m_PrefabCompositionAreas[compositionData2.m_StartNode];
				endCompositionAreas = m_Data.m_PrefabCompositionAreas[compositionData2.m_EndNode];
			}
			ErrorData error = default(ErrorData);
			if ((m_CollisionMask & CollisionMask.OnGround) == 0 || MathUtils.Intersect(bounds2, m_ObjectBounds))
			{
				CheckOverlap3D(ref error, collisionMask, collisionMask2, collisionMask3, edgeData2, edgeGeometryData2, startNodeGeometryData2, endNodeGeometryData2, netCompositionData, netCompositionData2, netCompositionData3, edgeCompositionAreas, startCompositionAreas, endCompositionAreas, origin);
			}
			if (error.m_ErrorType == ErrorType.None && CommonUtils.ExclusiveGroundCollision(m_CollisionMask, collisionMask4))
			{
				CheckOverlap2D(ref error, collisionMask, collisionMask2, collisionMask3, edgeData2, edgeGeometryData2, startNodeGeometryData2, endNodeGeometryData2, netCompositionData, netCompositionData2, netCompositionData3, edgeCompositionAreas, startCompositionAreas, endCompositionAreas, origin);
			}
			if (error.m_ErrorType == ErrorType.None)
			{
				return;
			}
			if (m_Optional)
			{
				error.m_ErrorSeverity = ErrorSeverity.Override;
				error.m_TempEntity = m_ObjectEntity;
			}
			else
			{
				error.m_ErrorSeverity = ErrorSeverity.Error;
				error.m_TempEntity = m_ObjectEntity;
				error.m_PermanentEntity = edgeEntity2;
				if (!essential && topLevelEntity2 != edgeEntity2 && topLevelEntity2 != Entity.Null)
				{
					PrefabRef prefabRef = m_Data.m_PrefabRef[topLevelEntity2];
					if (m_Data.m_PrefabObjectGeometry.HasComponent(prefabRef.m_Prefab) && (m_Data.m_PrefabObjectGeometry[prefabRef.m_Prefab].m_Flags & (GeometryFlags.Overridable | GeometryFlags.DeleteOverridden)) == (GeometryFlags.Overridable | GeometryFlags.DeleteOverridden) && !m_Data.m_Attached.HasComponent(topLevelEntity2) && (!m_Data.m_Temp.HasComponent(topLevelEntity2) || (m_Data.m_Temp[topLevelEntity2].m_Flags & TempFlags.Essential) == 0))
					{
						error.m_ErrorSeverity = ErrorSeverity.Warning;
						error.m_PermanentEntity = topLevelEntity2;
					}
				}
			}
			m_ErrorQueue.Enqueue(error);
		}

		private void CheckOverlap3D(ref ErrorData error, CollisionMask edgeCollisionMask2, CollisionMask startCollisionMask2, CollisionMask endCollisionMask2, Edge edgeData2, EdgeGeometry edgeGeometryData2, StartNodeGeometry startNodeGeometryData2, EndNodeGeometry endNodeGeometryData2, NetCompositionData edgeCompositionData2, NetCompositionData startCompositionData2, NetCompositionData endCompositionData2, DynamicBuffer<NetCompositionArea> edgeCompositionAreas2, DynamicBuffer<NetCompositionArea> startCompositionAreas2, DynamicBuffer<NetCompositionArea> endCompositionAreas2, float3 origin)
		{
			Bounds3 intersection = default(Bounds3);
			intersection.min = float.MaxValue;
			intersection.max = float.MinValue;
			float3 @float = math.mul(math.inverse(m_Transform.m_Rotation), m_Transform.m_Position - origin);
			Bounds3 bounds = ObjectUtils.GetBounds(m_ObjectStack, m_PrefabObjectGeometryData, m_ObjectStackData);
			if ((m_PrefabObjectGeometryData.m_Flags & GeometryFlags.IgnoreBottomCollision) != GeometryFlags.None)
			{
				bounds.min.y = math.max(bounds.min.y, 0f);
			}
			Game.Net.ValidationHelpers.Check3DCollisionMasks(edgeCollisionMask2, m_CollisionMask, edgeCompositionData2, out var outData);
			Game.Net.ValidationHelpers.Check3DCollisionMasks(startCollisionMask2, m_CollisionMask, startCompositionData2, out var outData2);
			Game.Net.ValidationHelpers.Check3DCollisionMasks(endCollisionMask2, m_CollisionMask, endCompositionData2, out var outData3);
			if (ObjectUtils.GetStandingLegCount(m_PrefabObjectGeometryData, out var legCount))
			{
				for (int i = 0; i < legCount; i++)
				{
					float3 float2 = @float + ObjectUtils.GetStandingLegOffset(m_PrefabObjectGeometryData, i);
					if ((m_PrefabObjectGeometryData.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
					{
						Cylinder3 cylinder = new Cylinder3
						{
							circle = new Circle2(m_PrefabObjectGeometryData.m_LegSize.x * 0.5f, float2.xz),
							height = new Bounds1(bounds.min.y, m_PrefabObjectGeometryData.m_LegSize.y) + float2.y,
							rotation = m_Transform.m_Rotation
						};
						if ((edgeCollisionMask2 & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edgeData2, m_TopLevelEntity, edgeGeometryData2, -origin, cylinder, m_ObjectBounds, outData, edgeCompositionAreas2, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
						}
						if ((startCollisionMask2 & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edgeData2.m_Start, m_TopLevelEntity, startNodeGeometryData2.m_Geometry, -origin, cylinder, m_ObjectBounds, outData2, startCompositionAreas2, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
						}
						if ((endCollisionMask2 & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edgeData2.m_End, m_TopLevelEntity, endNodeGeometryData2.m_Geometry, -origin, cylinder, m_ObjectBounds, outData3, endCompositionAreas2, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
						}
					}
					else if ((m_PrefabObjectGeometryData.m_Flags & GeometryFlags.IgnoreLegCollision) == 0)
					{
						Box3 box = new Box3
						{
							bounds = 
							{
								min = 
								{
									y = bounds.min.y,
									xz = m_PrefabObjectGeometryData.m_LegSize.xz * -0.5f
								},
								max = 
								{
									y = m_PrefabObjectGeometryData.m_LegSize.y,
									xz = m_PrefabObjectGeometryData.m_LegSize.xz * 0.5f
								}
							}
						};
						box.bounds += float2;
						box.rotation = m_Transform.m_Rotation;
						if ((edgeCollisionMask2 & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edgeData2, m_TopLevelEntity, edgeGeometryData2, -origin, box, m_ObjectBounds, outData, edgeCompositionAreas2, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
						}
						if ((startCollisionMask2 & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edgeData2.m_Start, m_TopLevelEntity, startNodeGeometryData2.m_Geometry, -origin, box, m_ObjectBounds, outData2, startCompositionAreas2, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
						}
						if ((endCollisionMask2 & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edgeData2.m_End, m_TopLevelEntity, endNodeGeometryData2.m_Geometry, -origin, box, m_ObjectBounds, outData3, endCompositionAreas2, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
						}
					}
				}
				bounds.min.y = m_PrefabObjectGeometryData.m_LegSize.y;
			}
			if ((m_PrefabObjectGeometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
			{
				Cylinder3 cylinder2 = new Cylinder3
				{
					circle = new Circle2(m_PrefabObjectGeometryData.m_Size.x * 0.5f, @float.xz),
					height = new Bounds1(bounds.min.y, bounds.max.y) + @float.y,
					rotation = m_Transform.m_Rotation
				};
				if ((edgeCollisionMask2 & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edgeData2, m_TopLevelEntity, edgeGeometryData2, -origin, cylinder2, m_ObjectBounds, outData, edgeCompositionAreas2, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
				}
				if ((startCollisionMask2 & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edgeData2.m_Start, m_TopLevelEntity, startNodeGeometryData2.m_Geometry, -origin, cylinder2, m_ObjectBounds, outData2, startCompositionAreas2, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
				}
				if ((endCollisionMask2 & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edgeData2.m_End, m_TopLevelEntity, endNodeGeometryData2.m_Geometry, -origin, cylinder2, m_ObjectBounds, outData3, endCompositionAreas2, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
				}
			}
			else
			{
				Box3 box2 = new Box3
				{
					bounds = bounds + @float,
					rotation = m_Transform.m_Rotation
				};
				if ((edgeCollisionMask2 & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edgeData2, m_TopLevelEntity, edgeGeometryData2, -origin, box2, m_ObjectBounds, outData, edgeCompositionAreas2, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
				}
				if ((startCollisionMask2 & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edgeData2.m_Start, m_TopLevelEntity, startNodeGeometryData2.m_Geometry, -origin, box2, m_ObjectBounds, outData2, startCompositionAreas2, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
				}
				if ((endCollisionMask2 & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edgeData2.m_End, m_TopLevelEntity, endNodeGeometryData2.m_Geometry, -origin, box2, m_ObjectBounds, outData3, endCompositionAreas2, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
				}
			}
			if (error.m_ErrorType != ErrorType.None)
			{
				error.m_Position = origin + MathUtils.Center(intersection);
			}
		}

		private void CheckOverlap2D(ref ErrorData error, CollisionMask edgeCollisionMask2, CollisionMask startCollisionMask2, CollisionMask endCollisionMask2, Edge edgeData2, EdgeGeometry edgeGeometryData2, StartNodeGeometry startNodeGeometryData2, EndNodeGeometry endNodeGeometryData2, NetCompositionData edgeCompositionData2, NetCompositionData startCompositionData2, NetCompositionData endCompositionData2, DynamicBuffer<NetCompositionArea> edgeCompositionAreas2, DynamicBuffer<NetCompositionArea> startCompositionAreas2, DynamicBuffer<NetCompositionArea> endCompositionAreas2, float3 origin)
		{
			Bounds2 intersection = default(Bounds2);
			intersection.min = float.MaxValue;
			intersection.max = float.MinValue;
			Bounds1 bounds = default(Bounds1);
			bounds.min = float.MaxValue;
			bounds.max = float.MinValue;
			if (ObjectUtils.GetStandingLegCount(m_PrefabObjectGeometryData, out var legCount))
			{
				for (int i = 0; i < legCount; i++)
				{
					float3 position = ObjectUtils.GetStandingLegPosition(m_PrefabObjectGeometryData, m_Transform, i) - origin;
					if ((m_PrefabObjectGeometryData.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
					{
						Circle2 circle = new Circle2(m_PrefabObjectGeometryData.m_LegSize.x * 0.5f, position.xz);
						if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, edgeCollisionMask2) && Game.Net.ValidationHelpers.Intersect(edgeData2, m_TopLevelEntity, edgeGeometryData2, -origin.xz, circle, m_ObjectBounds.xz, edgeCompositionData2, edgeCompositionAreas2, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
							bounds |= MathUtils.Center(edgeGeometryData2.m_Bounds.y & m_ObjectBounds.y);
						}
						if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, startCollisionMask2) && Game.Net.ValidationHelpers.Intersect(edgeData2.m_Start, m_TopLevelEntity, startNodeGeometryData2.m_Geometry, -origin.xz, circle, m_ObjectBounds.xz, startCompositionData2, startCompositionAreas2, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
							bounds |= MathUtils.Center(startNodeGeometryData2.m_Geometry.m_Bounds.y & m_ObjectBounds.y);
						}
						if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, endCollisionMask2) && Game.Net.ValidationHelpers.Intersect(edgeData2.m_End, m_TopLevelEntity, endNodeGeometryData2.m_Geometry, -origin.xz, circle, m_ObjectBounds.xz, endCompositionData2, endCompositionAreas2, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
							bounds |= MathUtils.Center(endNodeGeometryData2.m_Geometry.m_Bounds.y & m_ObjectBounds.y);
						}
					}
					else if ((m_PrefabObjectGeometryData.m_Flags & GeometryFlags.IgnoreLegCollision) == 0)
					{
						Quad2 xz = ObjectUtils.CalculateBaseCorners(bounds: new Bounds3
						{
							min = 
							{
								xz = m_PrefabObjectGeometryData.m_LegSize.xz * -0.5f
							},
							max = 
							{
								xz = m_PrefabObjectGeometryData.m_LegSize.xz * 0.5f
							}
						}, position: position, rotation: m_Transform.m_Rotation).xz;
						if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, edgeCollisionMask2) && Game.Net.ValidationHelpers.Intersect(edgeData2, m_TopLevelEntity, edgeGeometryData2, -origin.xz, xz, m_ObjectBounds.xz, edgeCompositionData2, edgeCompositionAreas2, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
							bounds |= MathUtils.Center(edgeGeometryData2.m_Bounds.y & m_ObjectBounds.y);
						}
						if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, startCollisionMask2) && Game.Net.ValidationHelpers.Intersect(edgeData2.m_Start, m_TopLevelEntity, startNodeGeometryData2.m_Geometry, -origin.xz, xz, m_ObjectBounds.xz, startCompositionData2, startCompositionAreas2, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
							bounds |= MathUtils.Center(startNodeGeometryData2.m_Geometry.m_Bounds.y & m_ObjectBounds.y);
						}
						if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, endCollisionMask2) && Game.Net.ValidationHelpers.Intersect(edgeData2.m_End, m_TopLevelEntity, endNodeGeometryData2.m_Geometry, -origin.xz, xz, m_ObjectBounds.xz, endCompositionData2, endCompositionAreas2, ref intersection))
						{
							error.m_ErrorType = ErrorType.OverlapExisting;
							bounds |= MathUtils.Center(endNodeGeometryData2.m_Geometry.m_Bounds.y & m_ObjectBounds.y);
						}
					}
				}
			}
			else if ((m_PrefabObjectGeometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
			{
				Circle2 circle2 = new Circle2(m_PrefabObjectGeometryData.m_Size.x * 0.5f, (m_Transform.m_Position - origin).xz);
				if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, edgeCollisionMask2) && Game.Net.ValidationHelpers.Intersect(edgeData2, m_TopLevelEntity, edgeGeometryData2, -origin.xz, circle2, m_ObjectBounds.xz, edgeCompositionData2, edgeCompositionAreas2, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
					bounds |= MathUtils.Center(edgeGeometryData2.m_Bounds.y & m_ObjectBounds.y);
				}
				if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, startCollisionMask2) && Game.Net.ValidationHelpers.Intersect(edgeData2.m_Start, m_TopLevelEntity, startNodeGeometryData2.m_Geometry, -origin.xz, circle2, m_ObjectBounds.xz, startCompositionData2, startCompositionAreas2, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
					bounds |= MathUtils.Center(startNodeGeometryData2.m_Geometry.m_Bounds.y & m_ObjectBounds.y);
				}
				if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, endCollisionMask2) && Game.Net.ValidationHelpers.Intersect(edgeData2.m_End, m_TopLevelEntity, endNodeGeometryData2.m_Geometry, -origin.xz, circle2, m_ObjectBounds.xz, endCompositionData2, endCompositionAreas2, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
					bounds |= MathUtils.Center(endNodeGeometryData2.m_Geometry.m_Bounds.y & m_ObjectBounds.y);
				}
			}
			else
			{
				Quad2 xz2 = ObjectUtils.CalculateBaseCorners(m_Transform.m_Position - origin, m_Transform.m_Rotation, m_PrefabObjectGeometryData.m_Bounds).xz;
				if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, edgeCollisionMask2) && Game.Net.ValidationHelpers.Intersect(edgeData2, m_TopLevelEntity, edgeGeometryData2, -origin.xz, xz2, m_ObjectBounds.xz, edgeCompositionData2, edgeCompositionAreas2, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
					bounds |= MathUtils.Center(edgeGeometryData2.m_Bounds.y & m_ObjectBounds.y);
				}
				if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, startCollisionMask2) && Game.Net.ValidationHelpers.Intersect(edgeData2.m_Start, m_TopLevelEntity, startNodeGeometryData2.m_Geometry, -origin.xz, xz2, m_ObjectBounds.xz, startCompositionData2, startCompositionAreas2, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
					bounds |= MathUtils.Center(startNodeGeometryData2.m_Geometry.m_Bounds.y & m_ObjectBounds.y);
				}
				if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, endCollisionMask2) && Game.Net.ValidationHelpers.Intersect(edgeData2.m_End, m_TopLevelEntity, endNodeGeometryData2.m_Geometry, -origin.xz, xz2, m_ObjectBounds.xz, endCompositionData2, endCompositionAreas2, ref intersection))
				{
					error.m_ErrorType = ErrorType.OverlapExisting;
					bounds |= MathUtils.Center(endNodeGeometryData2.m_Geometry.m_Bounds.y & m_ObjectBounds.y);
				}
			}
			if (error.m_ErrorType != ErrorType.None)
			{
				error.m_Position.xz = origin.xz + MathUtils.Center(intersection);
				error.m_Position.y = MathUtils.Center(bounds);
			}
		}
	}

	private struct AreaIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
	{
		public Entity m_ObjectEntity;

		public Bounds3 m_ObjectBounds;

		public bool m_IgnoreCollisions;

		public bool m_IgnoreProtectedAreas;

		public bool m_Optional;

		public bool m_EditorMode;

		public Transform m_TransformData;

		public CollisionMask m_CollisionMask;

		public ObjectGeometryData m_PrefabObjectGeometryData;

		public ValidationSystem.EntityData m_Data;

		public NativeQueue<ErrorData>.ParallelWriter m_ErrorQueue;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			return MathUtils.Intersect(bounds.m_Bounds.xz, m_ObjectBounds.xz);
		}

		public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem areaItem2)
		{
			if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_ObjectBounds.xz) || m_Data.m_Hidden.HasComponent(areaItem2.m_Area) || (m_Data.m_Area[areaItem2.m_Area].m_Flags & AreaFlags.Slave) != 0)
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
			if ((m_CollisionMask & collisionMask) == 0)
			{
				return;
			}
			ErrorType errorType = ((areaGeometryData.m_Type != AreaType.MapTile) ? ErrorType.OverlapExisting : ErrorType.ExceedsCityLimits);
			DynamicBuffer<Game.Areas.Node> nodes = m_Data.m_AreaNodes[areaItem2.m_Area];
			Triangle triangle = m_Data.m_AreaTriangles[areaItem2.m_Area][areaItem2.m_Triangle];
			Triangle3 triangle2 = AreaUtils.GetTriangle3(nodes, triangle);
			ErrorData value = default(ErrorData);
			if (areaGeometryData.m_Type != AreaType.MapTile && ((m_CollisionMask & CollisionMask.OnGround) == 0 || MathUtils.Intersect(bounds.m_Bounds, m_ObjectBounds)))
			{
				Bounds1 heightRange = triangle.m_HeightRange;
				heightRange.max += areaGeometryData.m_MaxHeight;
				float3 @float = math.mul(math.inverse(m_TransformData.m_Rotation), m_TransformData.m_Position);
				Bounds3 bounds2 = m_PrefabObjectGeometryData.m_Bounds;
				if ((m_PrefabObjectGeometryData.m_Flags & GeometryFlags.IgnoreBottomCollision) != GeometryFlags.None)
				{
					bounds2.min.y = math.max(bounds2.min.y, 0f);
				}
				if (ObjectUtils.GetStandingLegCount(m_PrefabObjectGeometryData, out var legCount))
				{
					for (int i = 0; i < legCount; i++)
					{
						float3 float2 = @float + ObjectUtils.GetStandingLegOffset(m_PrefabObjectGeometryData, i);
						if ((m_PrefabObjectGeometryData.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
						{
							if (Game.Net.ValidationHelpers.TriangleCylinderIntersect(cylinder2: new Cylinder3
							{
								circle = new Circle2(m_PrefabObjectGeometryData.m_LegSize.x * 0.5f, float2.xz),
								height = new Bounds1(bounds2.min.y, m_PrefabObjectGeometryData.m_LegSize.y) + float2.y,
								rotation = m_TransformData.m_Rotation
							}, triangle1: triangle2, intersection1: out var intersection, intersection2: out var intersection2))
							{
								intersection = Game.Net.ValidationHelpers.SetHeightRange(intersection, heightRange);
								if (MathUtils.Intersect(intersection2, intersection, out var intersection3))
								{
									value.m_Position = MathUtils.Center(intersection3);
									value.m_ErrorType = errorType;
								}
							}
						}
						else
						{
							if ((m_PrefabObjectGeometryData.m_Flags & GeometryFlags.IgnoreLegCollision) != GeometryFlags.None)
							{
								continue;
							}
							float3 standingLegPosition = ObjectUtils.GetStandingLegPosition(m_PrefabObjectGeometryData, m_TransformData, i);
							bounds2.min.xz = m_PrefabObjectGeometryData.m_LegSize.xz * -0.5f;
							bounds2.max.xz = m_PrefabObjectGeometryData.m_LegSize.xz * 0.5f;
							if (Game.Net.ValidationHelpers.QuadTriangleIntersect(ObjectUtils.CalculateBaseCorners(standingLegPosition, m_TransformData.m_Rotation, bounds2), triangle2, out var intersection4, out var intersection5))
							{
								intersection4 = Game.Net.ValidationHelpers.SetHeightRange(intersection4, bounds2.y);
								intersection5 = Game.Net.ValidationHelpers.SetHeightRange(intersection5, heightRange);
								if (MathUtils.Intersect(intersection4, intersection5, out var intersection6))
								{
									value.m_Position = MathUtils.Center(intersection6);
									value.m_ErrorType = errorType;
								}
							}
						}
					}
					bounds2.min.y = m_PrefabObjectGeometryData.m_LegSize.y;
				}
				Bounds3 intersection10;
				Bounds3 intersection11;
				if ((m_PrefabObjectGeometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
				{
					if (Game.Net.ValidationHelpers.TriangleCylinderIntersect(cylinder2: new Cylinder3
					{
						circle = new Circle2(m_PrefabObjectGeometryData.m_Size.x * 0.5f, @float.xz),
						height = new Bounds1(bounds2.min.y, bounds2.max.y) + @float.y,
						rotation = m_TransformData.m_Rotation
					}, triangle1: triangle2, intersection1: out var intersection7, intersection2: out var intersection8))
					{
						intersection7 = Game.Net.ValidationHelpers.SetHeightRange(intersection7, heightRange);
						if (MathUtils.Intersect(intersection8, intersection7, out var intersection9))
						{
							value.m_Position = MathUtils.Center(intersection9);
							value.m_ErrorType = errorType;
						}
					}
				}
				else if (Game.Net.ValidationHelpers.QuadTriangleIntersect(ObjectUtils.CalculateBaseCorners(m_TransformData.m_Position, m_TransformData.m_Rotation, m_PrefabObjectGeometryData.m_Bounds), triangle2, out intersection10, out intersection11))
				{
					intersection10 = Game.Net.ValidationHelpers.SetHeightRange(intersection10, bounds2.y);
					intersection11 = Game.Net.ValidationHelpers.SetHeightRange(intersection11, heightRange);
					if (MathUtils.Intersect(intersection10, intersection11, out var intersection12))
					{
						value.m_Position = MathUtils.Center(intersection12);
						value.m_ErrorType = errorType;
					}
				}
			}
			if (areaGeometryData.m_Type == AreaType.MapTile || (value.m_ErrorType == ErrorType.None && CommonUtils.ExclusiveGroundCollision(m_CollisionMask, collisionMask)))
			{
				if (areaGeometryData.m_Type != AreaType.MapTile && ObjectUtils.GetStandingLegCount(m_PrefabObjectGeometryData, out var legCount2))
				{
					for (int j = 0; j < legCount2; j++)
					{
						float3 standingLegPosition2 = ObjectUtils.GetStandingLegPosition(m_PrefabObjectGeometryData, m_TransformData, j);
						if ((m_PrefabObjectGeometryData.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
						{
							if (MathUtils.Intersect(circle: new Circle2(m_PrefabObjectGeometryData.m_LegSize.x * 0.5f, standingLegPosition2.xz), triangle: triangle2.xz))
							{
								value.m_Position = MathUtils.Center(m_ObjectBounds & bounds.m_Bounds);
								value.m_ErrorType = errorType;
							}
						}
						else if ((m_PrefabObjectGeometryData.m_Flags & GeometryFlags.IgnoreLegCollision) == 0 && MathUtils.Intersect(ObjectUtils.CalculateBaseCorners(bounds: new Bounds3
						{
							min = 
							{
								xz = m_PrefabObjectGeometryData.m_LegSize.xz * -0.5f
							},
							max = 
							{
								xz = m_PrefabObjectGeometryData.m_LegSize.xz * 0.5f
							}
						}, position: standingLegPosition2, rotation: m_TransformData.m_Rotation).xz, triangle2.xz))
						{
							value.m_Position = MathUtils.Center(m_ObjectBounds & bounds.m_Bounds);
							value.m_ErrorType = errorType;
						}
					}
				}
				else if ((m_PrefabObjectGeometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
				{
					if (MathUtils.Intersect(circle: new Circle2(m_PrefabObjectGeometryData.m_Size.x * 0.5f, m_TransformData.m_Position.xz), triangle: triangle2.xz))
					{
						value.m_Position = MathUtils.Center(m_ObjectBounds & bounds.m_Bounds);
						value.m_ErrorType = errorType;
					}
				}
				else if (MathUtils.Intersect(ObjectUtils.CalculateBaseCorners(m_TransformData.m_Position, m_TransformData.m_Rotation, m_PrefabObjectGeometryData.m_Bounds).xz, triangle2.xz))
				{
					value.m_Position = MathUtils.Center(m_ObjectBounds & bounds.m_Bounds);
					value.m_ErrorType = errorType;
				}
			}
			if (value.m_ErrorType != ErrorType.None)
			{
				value.m_Position.y = MathUtils.Clamp(value.m_Position.y, m_ObjectBounds.y);
				if (m_Optional && errorType == ErrorType.OverlapExisting)
				{
					value.m_ErrorSeverity = ErrorSeverity.Override;
					value.m_TempEntity = m_ObjectEntity;
				}
				else
				{
					value.m_ErrorSeverity = ErrorSeverity.Error;
					value.m_TempEntity = m_ObjectEntity;
					value.m_PermanentEntity = areaItem2.m_Area;
				}
				m_ErrorQueue.Enqueue(value);
			}
		}
	}

	public const float COLLISION_TOLERANCE = 0.01f;

	public static void ValidateObject(Entity entity, Temp temp, Owner owner, Transform transform, PrefabRef prefabRef, Attached attached, bool isOutsideConnection, bool editorMode, ValidationSystem.EntityData data, NativeList<ValidationSystem.BoundsData> edgeList, NativeList<ValidationSystem.BoundsData> objectList, NativeQuadTree<Entity, QuadTreeBoundsXZ> objectSearchTree, NativeQuadTree<Entity, QuadTreeBoundsXZ> netSearchTree, NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> areaSearchTree, NativeParallelHashMap<Entity, int> instanceCounts, WaterSurfaceData<SurfaceWater> waterSurfaceData, TerrainHeightData terrainHeightData, NativeQueue<ErrorData>.ParallelWriter errorQueue)
	{
		if (!data.m_PrefabObjectGeometry.TryGetComponent(prefabRef.m_Prefab, out var componentData) || ((componentData.m_Flags & GeometryFlags.IgnoreSecondaryCollision) != GeometryFlags.None && data.m_Secondary.HasComponent(entity)))
		{
			return;
		}
		StackData componentData2 = default(StackData);
		Stack componentData3;
		Bounds3 bounds = ((!data.m_Stack.TryGetComponent(entity, out componentData3) || !data.m_PrefabStackData.TryGetComponent(prefabRef.m_Prefab, out componentData2)) ? ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, componentData) : ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, componentData3, componentData, componentData2));
		data.m_PlaceableObject.TryGetComponent(prefabRef.m_Prefab, out var componentData4);
		bool flag = false;
		if ((componentData.m_Flags & (GeometryFlags.Overridable | GeometryFlags.DeleteOverridden)) == GeometryFlags.Overridable)
		{
			flag = (temp.m_Flags & TempFlags.Essential) == 0;
		}
		CollisionMask collisionMask;
		bool flag2;
		if (data.m_ObjectElevation.TryGetComponent(entity, out var componentData5))
		{
			collisionMask = ObjectUtils.GetCollisionMask(componentData, componentData5, !editorMode || owner.m_Owner != Entity.Null);
			flag2 = (componentData5.m_Flags & ElevationFlags.OnGround) != 0 && flag;
			Owner componentData6 = owner;
			while (flag && !flag2 && componentData6.m_Owner != Entity.Null)
			{
				PrefabRef prefabRef2 = data.m_PrefabRef[componentData6.m_Owner];
				if (!data.m_PrefabObjectGeometry.TryGetComponent(prefabRef2.m_Prefab, out var componentData7) || (componentData7.m_Flags & (GeometryFlags.Overridable | GeometryFlags.DeleteOverridden)) != GeometryFlags.Overridable || !data.m_Temp.TryGetComponent(componentData6.m_Owner, out var componentData8) || (componentData8.m_Flags & TempFlags.Essential) != 0)
				{
					break;
				}
				if (!data.m_ObjectElevation.TryGetComponent(componentData6.m_Owner, out componentData5) || (componentData5.m_Flags & ElevationFlags.OnGround) != 0)
				{
					flag2 = true;
					break;
				}
				if (!data.m_Owner.TryGetComponent(componentData6.m_Owner, out componentData6))
				{
					break;
				}
			}
		}
		else
		{
			collisionMask = ObjectUtils.GetCollisionMask(componentData, !editorMode || owner.m_Owner != Entity.Null);
			flag2 = flag;
		}
		Entity entity2 = Entity.Null;
		Entity ignoreNode = Entity.Null;
		if ((componentData4.m_Flags & PlacementFlags.RoadNode) != PlacementFlags.None)
		{
			if (data.m_Node.HasComponent(attached.m_Parent))
			{
				entity2 = attached.m_Parent;
			}
			if (data.m_Temp.HasComponent(attached.m_Parent))
			{
				Entity original = data.m_Temp[attached.m_Parent].m_Original;
				if (data.m_Node.HasComponent(original))
				{
					ignoreNode = original;
				}
			}
			else
			{
				ignoreNode = entity2;
				entity2 = Entity.Null;
			}
		}
		if (temp.m_Original == Entity.Null && (componentData4.m_Flags & PlacementFlags.Unique) != PlacementFlags.None && instanceCounts.ContainsKey(prefabRef.m_Prefab))
		{
			errorQueue.Enqueue(new ErrorData
			{
				m_ErrorType = ErrorType.AlreadyExists,
				m_ErrorSeverity = ErrorSeverity.Error,
				m_TempEntity = entity,
				m_Position = float.NaN
			});
		}
		ObjectIterator iterator = default(ObjectIterator);
		Entity attachedParent = default(Entity);
		Edge tempNodes = default(Edge);
		Edge ownerNodes = default(Edge);
		Entity edgeOwner = default(Entity);
		Entity nodeOwner = default(Entity);
		if ((temp.m_Flags & TempFlags.Delete) == 0)
		{
			Entity assetStamp;
			Entity owner2 = GetOwner(entity, temp, data, out tempNodes, out ownerNodes, out attachedParent, out assetStamp, out edgeOwner, out nodeOwner);
			iterator = new ObjectIterator
			{
				m_ObjectEntity = entity,
				m_TopLevelEntity = owner2,
				m_AssetStampEntity = assetStamp,
				m_ObjectBounds = bounds,
				m_Transform = transform,
				m_ObjectStack = componentData3,
				m_CollisionMask = collisionMask,
				m_PrefabObjectGeometryData = componentData,
				m_ObjectStackData = componentData2,
				m_CanOverride = flag,
				m_Optional = ((temp.m_Flags & TempFlags.Optional) != 0),
				m_EditorMode = editorMode,
				m_Data = data,
				m_ErrorQueue = errorQueue
			};
			objectSearchTree.Iterate(ref iterator);
		}
		NetIterator iterator2 = default(NetIterator);
		if ((temp.m_Flags & TempFlags.Delete) == 0)
		{
			iterator2 = new NetIterator
			{
				m_ObjectEntity = entity,
				m_AttachedParent = attachedParent,
				m_TopLevelEntity = iterator.m_TopLevelEntity,
				m_EdgeEntity = edgeOwner,
				m_NodeEntity = nodeOwner,
				m_IgnoreNode = ignoreNode,
				m_OwnerNodes = ownerNodes,
				m_ObjectBounds = bounds,
				m_Transform = transform,
				m_ObjectStack = componentData3,
				m_CollisionMask = collisionMask,
				m_PrefabObjectGeometryData = componentData,
				m_ObjectStackData = componentData2,
				m_Optional = flag,
				m_EditorMode = editorMode,
				m_Data = data,
				m_ErrorQueue = errorQueue
			};
			data.m_PrefabBuilding.TryGetComponent(prefabRef.m_Prefab, out iterator2.m_PrefabBuildingData);
			netSearchTree.Iterate(ref iterator2);
		}
		AreaIterator iterator3 = new AreaIterator
		{
			m_ObjectEntity = entity,
			m_ObjectBounds = bounds,
			m_IgnoreCollisions = ((temp.m_Flags & TempFlags.Delete) != 0),
			m_IgnoreProtectedAreas = ((temp.m_Flags & (TempFlags.Create | TempFlags.Delete | TempFlags.Modify | TempFlags.Replace | TempFlags.Upgrade)) == 0),
			m_Optional = flag,
			m_EditorMode = editorMode,
			m_TransformData = transform,
			m_CollisionMask = collisionMask,
			m_PrefabObjectGeometryData = componentData,
			m_Data = data,
			m_ErrorQueue = errorQueue
		};
		areaSearchTree.Iterate(ref iterator3);
		if ((temp.m_Flags & TempFlags.Delete) == 0 && (edgeList.Length != 0 || objectList.Length != 0))
		{
			Entity entity3 = entity;
			Entity entity4 = Entity.Null;
			Entity entity5 = Entity.Null;
			attachedParent = Entity.Null;
			if (owner.m_Owner != Entity.Null && !data.m_Building.HasComponent(entity))
			{
				if (data.m_Node.HasComponent(owner.m_Owner))
				{
					entity5 = owner.m_Owner;
				}
				if (data.m_AssetStamp.HasComponent(owner.m_Owner))
				{
					entity4 = owner.m_Owner;
				}
				else
				{
					if (data.m_Attached.TryGetComponent(owner.m_Owner, out var componentData9))
					{
						attachedParent = componentData9.m_Parent;
					}
					entity3 = owner.m_Owner;
					Owner componentData10;
					while (data.m_Owner.TryGetComponent(entity3, out componentData10) && !data.m_Building.HasComponent(entity3))
					{
						Entity owner3 = componentData10.m_Owner;
						if (data.m_Node.HasComponent(owner3))
						{
							entity5 = owner3;
						}
						if (data.m_AssetStamp.HasComponent(owner3))
						{
							entity4 = owner3;
							break;
						}
						if (data.m_Attached.TryGetComponent(componentData10.m_Owner, out componentData9))
						{
							attachedParent = componentData9.m_Parent;
						}
						entity3 = owner3;
					}
				}
			}
			DynamicBuffer<ConnectedEdge> dynamicBuffer = default(DynamicBuffer<ConnectedEdge>);
			DynamicBuffer<ConnectedNode> dynamicBuffer2 = default(DynamicBuffer<ConnectedNode>);
			Edge edge = default(Edge);
			if (data.m_ConnectedEdges.HasBuffer(entity3))
			{
				dynamicBuffer = data.m_ConnectedEdges[entity3];
			}
			else if (data.m_ConnectedNodes.HasBuffer(entity3))
			{
				dynamicBuffer2 = data.m_ConnectedNodes[entity3];
				edge = data.m_Edge[entity3];
			}
			bool flag3 = false;
			if ((componentData4.m_Flags & PlacementFlags.RoadNode) != PlacementFlags.None && data.m_PrefabNetObject.TryGetComponent(prefabRef.m_Prefab, out var componentData11) && (componentData11.m_CompositionFlags.m_General & CompositionFlags.General.FixedNodeSize) != 0)
			{
				flag3 = true;
			}
			iterator2.m_TopLevelEntity = entity3;
			if (edgeList.Length != 0)
			{
				float3 @float = edgeList[edgeList.Length - 1].m_Bounds.max - edgeList[0].m_Bounds.min;
				bool flag4 = @float.z > @float.x;
				for (int i = 0; i < edgeList.Length; i++)
				{
					ValidationSystem.BoundsData boundsData = edgeList[i];
					bool2 @bool = boundsData.m_Bounds.min.xz > bounds.max.xz;
					if (flag4 ? @bool.y : @bool.x)
					{
						break;
					}
					if ((collisionMask & CollisionMask.OnGround) != 0)
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
					Entity entity6 = boundsData.m_Entity;
					Entity owner4;
					if (data.m_Owner.TryGetComponent(boundsData.m_Entity, out var componentData12))
					{
						owner4 = componentData12.m_Owner;
						if (data.m_AssetStamp.HasComponent(owner4))
						{
							if (owner4 == entity)
							{
								continue;
							}
						}
						else
						{
							entity6 = owner4;
							while (data.m_Owner.HasComponent(entity6) && !data.m_Building.HasComponent(entity6))
							{
								owner4 = data.m_Owner[entity6].m_Owner;
								if (!data.m_AssetStamp.HasComponent(owner4))
								{
									entity6 = owner4;
									continue;
								}
								goto IL_0889;
							}
						}
						goto IL_08bc;
					}
					goto IL_08f9;
					IL_0889:
					if (owner4 == entity)
					{
						continue;
					}
					goto IL_08bc;
					IL_08bc:
					if (data.m_Edge.TryGetComponent(componentData12.m_Owner, out var componentData13) && (entity5 == componentData13.m_Start || entity5 == componentData13.m_End))
					{
						continue;
					}
					goto IL_08f9;
					IL_08f9:
					if (entity3 == entity6)
					{
						continue;
					}
					Edge edgeData = data.m_Edge[boundsData.m_Entity];
					if (boundsData.m_Entity == attachedParent || edgeData.m_Start == attachedParent || edgeData.m_End == attachedParent)
					{
						continue;
					}
					Entity entity7 = edgeData.m_Start;
					Entity entity8 = edgeData.m_End;
					Edge edge2 = default(Edge);
					Edge edge3 = default(Edge);
					while (true)
					{
						if (data.m_Owner.TryGetComponent(entity7, out var componentData14) && !data.m_Building.HasComponent(entity7))
						{
							Entity owner5 = componentData14.m_Owner;
							if (!data.m_AssetStamp.HasComponent(owner5))
							{
								if (data.m_Edge.TryGetComponent(owner5, out var componentData15))
								{
									edge2 = componentData15;
								}
								entity7 = owner5;
								continue;
							}
							if (owner5 == entity)
							{
								break;
							}
						}
						while (true)
						{
							if (data.m_Owner.TryGetComponent(entity8, out var componentData16) && !data.m_Building.HasComponent(entity8))
							{
								Entity owner6 = componentData16.m_Owner;
								if (!data.m_AssetStamp.HasComponent(owner6))
								{
									if (data.m_Edge.TryGetComponent(owner6, out var componentData17))
									{
										edge3 = componentData17;
									}
									entity8 = owner6;
									continue;
								}
								if (owner6 == entity)
								{
									break;
								}
							}
							Composition compositionData = data.m_Composition[boundsData.m_Entity];
							if (flag3)
							{
								if (owner.m_Owner == edgeData.m_Start && (data.m_PrefabComposition[compositionData.m_StartNode].m_Flags.m_General & CompositionFlags.General.FixedNodeSize) == 0)
								{
									edgeData.m_Start = boundsData.m_Entity;
									entity7 = boundsData.m_Entity;
								}
								if (owner.m_Owner == edgeData.m_End && (data.m_PrefabComposition[compositionData.m_EndNode].m_Flags.m_General & CompositionFlags.General.FixedNodeSize) == 0)
								{
									edgeData.m_End = boundsData.m_Entity;
									entity8 = boundsData.m_Entity;
								}
							}
							if (owner.m_Owner != Entity.Null)
							{
								Entity owner7 = owner.m_Owner;
								while (!(owner7 == edgeData.m_Start) && !(owner7 == edgeData.m_End) && !(owner7 == edge2.m_Start) && !(owner7 == edge2.m_End) && !(owner7 == edge3.m_Start) && !(owner7 == edge3.m_End))
								{
									if (data.m_Owner.TryGetComponent(owner7, out var componentData18))
									{
										owner7 = componentData18.m_Owner;
										continue;
									}
									goto IL_0bae;
								}
								break;
							}
							goto IL_0bae;
							IL_0bae:
							EdgeGeometry edgeGeometryData = data.m_EdgeGeometry[boundsData.m_Entity];
							StartNodeGeometry startNodeGeometryData = data.m_StartNodeGeometry[boundsData.m_Entity];
							EndNodeGeometry endNodeGeometryData = data.m_EndNodeGeometry[boundsData.m_Entity];
							bool flag5 = entity7 != entity3 && edgeData.m_Start != tempNodes.m_Start && edgeData.m_Start != tempNodes.m_End;
							bool flag6 = entity8 != entity3 && edgeData.m_End != tempNodes.m_Start && edgeData.m_End != tempNodes.m_End;
							if (flag5 && edgeData.m_Start == entity2)
							{
								flag5 &= (data.m_PrefabComposition[compositionData.m_StartNode].m_Flags.m_General & CompositionFlags.General.Roundabout) == 0;
							}
							if (flag6 && edgeData.m_End == entity2)
							{
								flag6 &= (data.m_PrefabComposition[compositionData.m_EndNode].m_Flags.m_General & CompositionFlags.General.Roundabout) == 0;
							}
							edgeData.m_Start = entity7;
							edgeData.m_End = entity8;
							Temp temp2 = data.m_Temp[boundsData.m_Entity];
							iterator2.CheckOverlap(entity6, boundsData.m_Entity, boundsData.m_Bounds, edgeData, compositionData, edgeGeometryData, startNodeGeometryData, endNodeGeometryData, transform.m_Position, flag5, flag6, (temp2.m_Flags & TempFlags.Essential) != 0, componentData12.m_Owner != Entity.Null);
							break;
						}
						break;
					}
				}
			}
			if (objectList.Length != 0)
			{
				float3 float2 = objectList[objectList.Length - 1].m_Bounds.max - objectList[0].m_Bounds.min;
				bool flag7 = float2.z > float2.x;
				int num = 0;
				int num2 = objectList.Length;
				while (num < num2)
				{
					int num3 = num + num2 >> 1;
					bool2 bool2 = objectList[num3].m_Bounds.min.xz < bounds.min.xz;
					if (flag7 ? bool2.y : bool2.x)
					{
						num = num3 + 1;
					}
					else
					{
						num2 = num3;
					}
				}
				for (int j = num; j < objectList.Length; j++)
				{
					ValidationSystem.BoundsData boundsData2 = objectList[j];
					bool2 bool3 = boundsData2.m_Bounds.min.xz > bounds.max.xz;
					if (flag7 ? bool3.y : bool3.x)
					{
						break;
					}
					if ((collisionMask & CollisionMask.OnGround) != 0)
					{
						if (!MathUtils.Intersect(bounds.xz, boundsData2.m_Bounds.xz))
						{
							continue;
						}
					}
					else if (!MathUtils.Intersect(bounds, boundsData2.m_Bounds))
					{
						continue;
					}
					if (boundsData2.m_Entity == entity || boundsData2.m_Entity == entity4 || (boundsData2.m_Bounds.min.x == bounds.min.x && boundsData2.m_Entity.Index < entity.Index))
					{
						continue;
					}
					Entity entity9 = boundsData2.m_Entity;
					Entity entity10 = Entity.Null;
					Entity owner8;
					if (data.m_Owner.TryGetComponent(boundsData2.m_Entity, out var componentData19) && !data.m_Building.HasComponent(entity9))
					{
						owner8 = componentData19.m_Owner;
						if (data.m_AssetStamp.HasComponent(owner8))
						{
							if (owner8 == entity)
							{
								continue;
							}
						}
						else
						{
							if (data.m_Attached.TryGetComponent(owner8, out var componentData20))
							{
								entity10 = componentData20.m_Parent;
							}
							entity9 = owner8;
							while (data.m_Owner.HasComponent(entity9) && !data.m_Building.HasComponent(entity9))
							{
								owner8 = data.m_Owner[entity9].m_Owner;
								if (!data.m_AssetStamp.HasComponent(owner8))
								{
									if (data.m_Attached.TryGetComponent(owner8, out componentData20))
									{
										entity10 = componentData20.m_Parent;
									}
									entity9 = owner8;
									continue;
								}
								goto IL_0faf;
							}
						}
					}
					goto IL_0ffd;
					IL_0ffd:
					if (entity3 == entity9)
					{
						continue;
					}
					if (dynamicBuffer.IsCreated)
					{
						int num4 = 0;
						while (num4 < dynamicBuffer.Length)
						{
							if (!(dynamicBuffer[num4].m_Edge == entity9))
							{
								num4++;
								continue;
							}
							goto IL_1146;
						}
					}
					else if (dynamicBuffer2.IsCreated)
					{
						int num5 = 0;
						while (num5 < dynamicBuffer2.Length)
						{
							if (!(dynamicBuffer2[num5].m_Node == entity9))
							{
								num5++;
								continue;
							}
							goto IL_1146;
						}
						if (edge.m_Start == entity9 || edge.m_End == entity9)
						{
							continue;
						}
					}
					if (!(attached.m_Parent == boundsData2.m_Entity) && !(attachedParent == boundsData2.m_Entity) && !(entity10 == entity) && (!data.m_Attached.TryGetComponent(boundsData2.m_Entity, out var componentData21) || !(componentData21.m_Parent == entity)))
					{
						Temp temp3 = data.m_Temp[boundsData2.m_Entity];
						iterator.CheckOverlap(entity9, boundsData2.m_Entity, boundsData2.m_Bounds, (temp3.m_Flags & TempFlags.Essential) != 0, componentData19.m_Owner != Entity.Null);
					}
					continue;
					IL_0faf:
					if (owner8 == entity)
					{
						continue;
					}
					goto IL_0ffd;
					IL_1146:;
				}
			}
		}
		if ((temp.m_Flags & (TempFlags.Create | TempFlags.Modify)) != 0 && (componentData4.m_Flags & ~PlacementFlags.HasProbability) != PlacementFlags.None && !flag2)
		{
			CheckSurface(entity, transform, collisionMask, componentData, componentData4, data, waterSurfaceData, terrainHeightData, errorQueue);
		}
		if ((temp.m_Flags & TempFlags.Essential) != 0 && (temp.m_Flags & (TempFlags.Create | TempFlags.Modify)) != 0 && owner.m_Owner != Entity.Null)
		{
			ValidateSubPlacement(entity, owner, transform, prefabRef, componentData, data, errorQueue);
		}
		if ((temp.m_Flags & (TempFlags.Create | TempFlags.Modify | TempFlags.Replace | TempFlags.Upgrade)) != 0 && !isOutsideConnection)
		{
			ValidateWorldBounds(entity, owner, bounds, data, terrainHeightData, errorQueue);
		}
	}

	public static void ValidateWorldBounds(Entity entity, Owner owner, Bounds3 bounds, ValidationSystem.EntityData data, TerrainHeightData terrainHeightData, NativeQueue<ErrorData>.ParallelWriter errorQueue)
	{
		Bounds3 bounds2 = MathUtils.Expand(TerrainUtils.GetBounds(ref terrainHeightData), 0.1f);
		if (bounds.xz.Equals(bounds.xz & bounds2.xz))
		{
			return;
		}
		while (owner.m_Owner != Entity.Null)
		{
			if (data.m_Node.HasComponent(owner.m_Owner) || data.m_Edge.HasComponent(owner.m_Owner))
			{
				return;
			}
			data.m_Owner.TryGetComponent(owner.m_Owner, out var componentData);
			owner = componentData;
		}
		Bounds3 bounds3 = bounds;
		bounds3.min.xz = math.select(bounds.min.xz, bounds2.max.xz, (bounds2.max.xz > bounds.min.xz) & (bounds.min.xz >= bounds2.min.xz) & (bounds.max.xz > bounds2.max.xz));
		bounds3.max.xz = math.select(bounds.max.xz, bounds2.min.xz, (bounds2.min.xz < bounds.max.xz) & (bounds.max.xz <= bounds2.max.xz) & (bounds.min.xz < bounds2.min.xz));
		errorQueue.Enqueue(new ErrorData
		{
			m_Position = MathUtils.Center(bounds3),
			m_ErrorType = ErrorType.ExceedsCityLimits,
			m_ErrorSeverity = ErrorSeverity.Error,
			m_TempEntity = entity
		});
	}

	public static void ValidateSubPlacement(Entity entity, Owner owner, Transform transform, PrefabRef prefabRef, ObjectGeometryData prefabObjectGeometryData, ValidationSystem.EntityData data, NativeQueue<ErrorData>.ParallelWriter errorQueue)
	{
		if (!data.m_Building.HasComponent(owner.m_Owner))
		{
			return;
		}
		Transform transform2 = data.m_Transform[owner.m_Owner];
		PrefabRef prefabRef2 = data.m_PrefabRef[owner.m_Owner];
		BuildingData ownerBuildingData = data.m_PrefabBuilding[prefabRef2.m_Prefab];
		if (data.m_Building.HasComponent(entity))
		{
			if (data.m_PrefabBuilding.TryGetComponent(prefabRef.m_Prefab, out var componentData) && data.m_ServiceUpgradeData.TryGetComponent(prefabRef.m_Prefab, out var componentData2) && componentData2.m_MaxPlacementDistance != 0f)
			{
				BuildingUtils.CalculateUpgradeRangeValues(transform2.m_Rotation, ownerBuildingData, componentData, componentData2, out var forward, out var width, out var length, out var roundness, out var circular);
				float2 halfLotSize = (float2)componentData.m_LotSize * 4f - 0.4f;
				Quad3 quad = BuildingUtils.CalculateCorners(transform.m_Position, transform.m_Rotation, halfLotSize);
				float4 @float = default(float4);
				if (ExceedRange(transform2.m_Position, forward, width, length, roundness, circular, quad.a.xz))
				{
					@float += new float4(quad.a, 1f);
				}
				if (ExceedRange(transform2.m_Position, forward, width, length, roundness, circular, quad.b.xz))
				{
					@float += new float4(quad.b, 1f);
				}
				if (ExceedRange(transform2.m_Position, forward, width, length, roundness, circular, quad.c.xz))
				{
					@float += new float4(quad.c, 1f);
				}
				if (ExceedRange(transform2.m_Position, forward, width, length, roundness, circular, quad.d.xz))
				{
					@float += new float4(quad.d, 1f);
				}
				if (@float.w != 0f)
				{
					errorQueue.Enqueue(new ErrorData
					{
						m_ErrorType = ErrorType.LongDistance,
						m_ErrorSeverity = ErrorSeverity.Error,
						m_TempEntity = entity,
						m_PermanentEntity = owner.m_Owner,
						m_Position = @float.xyz / @float.w
					});
				}
			}
		}
		else
		{
			float2 float2 = ownerBuildingData.m_LotSize;
			float2 *= 4f;
			Bounds2 bounds = new Bounds2(-float2, float2);
			Transform transform3 = ObjectUtils.WorldToLocal(ObjectUtils.InverseTransform(transform2), transform);
			Bounds3 bounds2 = ObjectUtils.CalculateBounds(transform3.m_Position, transform3.m_Rotation, prefabObjectGeometryData);
			if (!bounds2.xz.Equals(bounds2.xz & bounds))
			{
				float3 position = new float3
				{
					xz = (math.select(bounds2.min.xz, bounds.max, (bounds2.min.xz >= bounds.min) & (bounds2.max.xz > bounds.max)) + math.select(bounds2.max.xz, bounds.min, (bounds2.max.xz <= bounds.max) & (bounds2.min.xz < bounds.min))) * 0.5f,
					y = MathUtils.Center(bounds2.y)
				};
				errorQueue.Enqueue(new ErrorData
				{
					m_ErrorType = ErrorType.ExceedsLotLimits,
					m_ErrorSeverity = ErrorSeverity.Warning,
					m_TempEntity = entity,
					m_Position = ObjectUtils.LocalToWorld(transform2, position)
				});
			}
		}
	}

	private static bool ExceedRange(float3 position, float3 forward, float width, float length, float roundness, bool circular, float2 checkPosition)
	{
		float2 x = checkPosition - position.xz;
		if (!circular)
		{
			roundness -= 8f;
			x = math.abs(new float2(math.dot(x, MathUtils.Right(forward.xz)), math.dot(x, forward.xz)));
			x = math.max(0f, x - new float2(width * 0.5f, length * 0.5f) + roundness);
		}
		return math.length(x) > roundness;
	}

	public static void ValidateNetObject(Entity entity, Owner owner, NetObject netObject, Transform transform, PrefabRef prefabRef, Attached attached, ValidationSystem.EntityData data, NativeQueue<ErrorData>.ParallelWriter errorQueue)
	{
		RoadTypes roadTypes = RoadTypes.None;
		bool flag = false;
		if (data.m_PrefabNetObject.TryGetComponent(prefabRef.m_Prefab, out var componentData) && componentData.m_RequireRoad != RoadTypes.None)
		{
			roadTypes = componentData.m_RequireRoad;
			if (data.m_Lanes.TryGetBuffer(attached.m_Parent, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					Game.Net.SubLane subLane = bufferData[i];
					if (!data.m_CarLane.HasComponent(subLane.m_SubLane))
					{
						continue;
					}
					PrefabRef prefabRef2 = data.m_PrefabRef[subLane.m_SubLane];
					if (data.m_CarLaneData.TryGetComponent(prefabRef2.m_Prefab, out var componentData2))
					{
						roadTypes = (RoadTypes)((uint)roadTypes & (uint)(byte)(~(int)componentData2.m_RoadTypes));
						if (roadTypes == RoadTypes.None)
						{
							break;
						}
					}
				}
			}
			if (roadTypes == RoadTypes.Watercraft && componentData.m_RequireRoad == (RoadTypes.Car | RoadTypes.Watercraft))
			{
				roadTypes = RoadTypes.None;
			}
		}
		if (data.m_PlaceableObject.TryGetComponent(prefabRef.m_Prefab, out var componentData3) && (componentData3.m_Flags & PlacementFlags.RequirePedestrian) != PlacementFlags.None)
		{
			flag = true;
			if (data.m_Lanes.TryGetBuffer(attached.m_Parent, out var bufferData2))
			{
				for (int j = 0; j < bufferData2.Length; j++)
				{
					Game.Net.SubLane subLane2 = bufferData2[j];
					if (data.m_PedestrianLane.HasComponent(subLane2.m_SubLane))
					{
						flag = false;
						break;
					}
				}
			}
		}
		if ((roadTypes != RoadTypes.None || flag) && data.m_PrefabObjectGeometry.TryGetComponent(prefabRef.m_Prefab, out var componentData4) && (componentData4.m_Flags & GeometryFlags.Marker) != GeometryFlags.None && data.m_Temp.HasComponent(owner.m_Owner))
		{
			entity = owner.m_Owner;
		}
		if (roadTypes != RoadTypes.None)
		{
			errorQueue.Enqueue(new ErrorData
			{
				m_ErrorType = ((roadTypes == (RoadTypes.Car | RoadTypes.Watercraft)) ? ErrorType.NoPortAccess : ErrorType.NoRoadAccess),
				m_ErrorSeverity = ErrorSeverity.Error,
				m_TempEntity = entity,
				m_Position = transform.m_Position
			});
		}
		else if (flag)
		{
			errorQueue.Enqueue(new ErrorData
			{
				m_ErrorType = ErrorType.NoPedestrianAccess,
				m_ErrorSeverity = ErrorSeverity.Error,
				m_TempEntity = entity,
				m_Position = transform.m_Position
			});
		}
		else if ((netObject.m_Flags & (NetObjectFlags.IsClear | NetObjectFlags.TrackPassThrough)) == 0)
		{
			errorQueue.Enqueue(new ErrorData
			{
				m_ErrorType = ErrorType.OverlapExisting,
				m_ErrorSeverity = ErrorSeverity.Error,
				m_TempEntity = entity,
				m_Position = transform.m_Position
			});
		}
	}

	public static void ValidateOutsideConnection(Entity entity, Transform transform, TerrainHeightData terrainHeightData, NativeQueue<ErrorData>.ParallelWriter errorQueue)
	{
		Bounds3 bounds = TerrainUtils.GetBounds(ref terrainHeightData);
		if (MathUtils.Intersect(MathUtils.Expand(bounds, -0.1f).xz, transform.m_Position.xz))
		{
			errorQueue.Enqueue(new ErrorData
			{
				m_ErrorType = ErrorType.NotOnBorder,
				m_ErrorSeverity = ErrorSeverity.Error,
				m_TempEntity = entity,
				m_Position = transform.m_Position
			});
		}
	}

	public static void ValidateWaterSource(Entity entity, Transform transform, Game.Simulation.WaterSourceData waterSourceData, TerrainHeightData terrainHeightData, Bounds3 worldBounds, NativeQueue<ErrorData>.ParallelWriter errorQueue)
	{
		if (!MathUtils.Intersect(worldBounds.xz, transform.m_Position.xz))
		{
			errorQueue.Enqueue(new ErrorData
			{
				m_ErrorType = ErrorType.ExceedsCityLimits,
				m_ErrorSeverity = ErrorSeverity.Error,
				m_TempEntity = entity,
				m_Position = transform.m_Position
			});
		}
	}

	private static Entity GetOwner(Entity entity, Temp temp, ValidationSystem.EntityData data, out Edge tempNodes, out Edge ownerNodes, out Entity attachedParent, out Entity assetStamp, out Entity edgeOwner, out Entity nodeOwner)
	{
		tempNodes = default(Edge);
		ownerNodes = default(Edge);
		attachedParent = Entity.Null;
		assetStamp = Entity.Null;
		edgeOwner = Entity.Null;
		nodeOwner = Entity.Null;
		if (!data.m_Owner.TryGetComponent(entity, out var componentData) || data.m_Building.HasComponent(entity))
		{
			entity = temp.m_Original;
		}
		else
		{
			do
			{
				if (data.m_AssetStamp.HasComponent(componentData.m_Owner))
				{
					assetStamp = componentData.m_Owner;
					break;
				}
				entity = componentData.m_Owner;
				if (data.m_Edge.TryGetComponent(entity, out var componentData2))
				{
					edgeOwner = entity;
					ownerNodes = componentData2;
					if (data.m_Temp.TryGetComponent(edgeOwner, out var componentData3))
					{
						edgeOwner = componentData3.m_Original;
					}
					if (data.m_Temp.TryGetComponent(ownerNodes.m_Start, out temp))
					{
						tempNodes.m_Start = ownerNodes.m_Start;
						ownerNodes.m_Start = temp.m_Original;
					}
					if (data.m_Temp.TryGetComponent(ownerNodes.m_End, out temp))
					{
						tempNodes.m_End = ownerNodes.m_End;
						ownerNodes.m_End = temp.m_Original;
					}
				}
				else if (data.m_Node.HasComponent(entity))
				{
					nodeOwner = entity;
					if (data.m_Temp.TryGetComponent(nodeOwner, out var componentData4))
					{
						nodeOwner = componentData4.m_Original;
					}
				}
				if (data.m_Temp.TryGetComponent(entity, out temp))
				{
					entity = temp.m_Original;
				}
				if (data.m_Attached.TryGetComponent(entity, out var componentData5))
				{
					attachedParent = componentData5.m_Parent;
				}
			}
			while (data.m_Owner.TryGetComponent(entity, out componentData) && !data.m_Building.HasComponent(entity));
		}
		return entity;
	}

	private static void CheckSurface(Entity entity, Transform transform, CollisionMask collisionMask, ObjectGeometryData prefabObjectGeometryData, PlaceableObjectData placeableObjectData, ValidationSystem.EntityData data, WaterSurfaceData<SurfaceWater> waterSurfaceData, TerrainHeightData terrainHeightData, NativeQueue<ErrorData>.ParallelWriter errorQueue)
	{
		if ((placeableObjectData.m_Flags & PlacementFlags.Shoreline) != PlacementFlags.None && (prefabObjectGeometryData.m_Flags & GeometryFlags.Standing) != GeometryFlags.None)
		{
			float2 @float = prefabObjectGeometryData.m_LegSize.xz * 0.5f + prefabObjectGeometryData.m_LegOffset;
			prefabObjectGeometryData.m_Bounds.xz |= new Bounds2(-@float, @float);
		}
		float sampleInterval = WaterUtils.GetSampleInterval(ref waterSurfaceData);
		int2 @int = (int2)math.ceil((prefabObjectGeometryData.m_Bounds.max.xz - prefabObjectGeometryData.m_Bounds.min.xz) / sampleInterval);
		Quad3 quad = ObjectUtils.CalculateBaseCorners(transform.m_Position, transform.m_Rotation, prefabObjectGeometryData.m_Bounds);
		Bounds3 bounds = default(Bounds3);
		bounds.min = float.MaxValue;
		bounds.max = float.MinValue;
		Bounds3 bounds2 = default(Bounds3);
		bounds2.min = float.MaxValue;
		bounds2.max = float.MinValue;
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < @int.x; i++)
		{
			float t = ((float)i + 0.5f) / (float)@int.x;
			float3 float2 = math.lerp(quad.a, quad.b, t);
			float3 float3 = math.lerp(quad.d, quad.c, t);
			if ((placeableObjectData.m_Flags & PlacementFlags.Shoreline) != PlacementFlags.None)
			{
				float num = WaterUtils.SampleDepth(ref waterSurfaceData, float2);
				float num2 = WaterUtils.SampleDepth(ref waterSurfaceData, float3);
				if (num >= 0.2f)
				{
					bounds |= float2;
					flag = (placeableObjectData.m_Flags & PlacementFlags.Floating) == 0;
				}
				if (num2 < 0.2f)
				{
					bounds2 |= float3;
					flag2 = true;
				}
			}
			else if ((placeableObjectData.m_Flags & (PlacementFlags.Floating | PlacementFlags.Underwater)) != PlacementFlags.None)
			{
				if ((placeableObjectData.m_Flags & PlacementFlags.OnGround) != PlacementFlags.None)
				{
					continue;
				}
				for (int j = 0; j < @int.y; j++)
				{
					float t2 = ((float)j + 0.5f) / (float)@int.y;
					float3 float4 = math.lerp(float2, float3, t2);
					if (WaterUtils.SampleDepth(ref waterSurfaceData, float4) < 0.2f)
					{
						bounds2 |= float4;
						flag2 = true;
					}
				}
			}
			else
			{
				if ((prefabObjectGeometryData.m_Flags & GeometryFlags.CanSubmerge) != GeometryFlags.None)
				{
					continue;
				}
				for (int k = 0; k < @int.y; k++)
				{
					float t3 = ((float)k + 0.5f) / (float)@int.y;
					float3 float5 = math.lerp(float2, float3, t3);
					float waterDepth;
					if ((collisionMask & CollisionMask.ExclusiveGround) != 0)
					{
						waterDepth = WaterUtils.SampleDepth(ref waterSurfaceData, float5);
					}
					else
					{
						float num3 = WaterUtils.SampleHeight(ref waterSurfaceData, ref terrainHeightData, float5, out waterDepth);
						waterDepth = math.min(waterDepth, num3 - transform.m_Position.y);
					}
					if (waterDepth >= 0.2f)
					{
						bounds |= float5;
						flag = true;
					}
				}
			}
		}
		if (flag)
		{
			errorQueue.Enqueue(new ErrorData
			{
				m_ErrorType = ErrorType.InWater,
				m_ErrorSeverity = ErrorSeverity.Error,
				m_TempEntity = entity,
				m_Position = MathUtils.Center(bounds)
			});
		}
		if (flag2)
		{
			ErrorData value = default(ErrorData);
			if ((placeableObjectData.m_Flags & (PlacementFlags.OnGround | PlacementFlags.Shoreline)) == (PlacementFlags.OnGround | PlacementFlags.Shoreline))
			{
				value.m_ErrorType = ErrorType.NotOnShoreline;
			}
			else
			{
				value.m_ErrorType = ErrorType.NoWater;
			}
			if ((placeableObjectData.m_Flags & PlacementFlags.OnGround) == 0)
			{
				value.m_ErrorSeverity = ErrorSeverity.Error;
			}
			else
			{
				value.m_ErrorSeverity = ErrorSeverity.Warning;
			}
			value.m_TempEntity = entity;
			value.m_Position = MathUtils.Center(bounds2);
			errorQueue.Enqueue(value);
		}
	}

	public static bool Intersect(Cylinder3 cylinder1, Cylinder3 cylinder2, ref float3 pos)
	{
		quaternion q = math.mul(cylinder2.rotation, math.inverse(cylinder1.rotation));
		cylinder2.circle.position = math.mul(q, new float3(cylinder2.circle.position.x, 0f, cylinder2.circle.position.y)).xz;
		cylinder2.height.min = math.mul(q, new float3(0f, cylinder2.height.min, 0f)).y;
		cylinder2.height.max = math.mul(q, new float3(0f, cylinder2.height.max, 0f)).y;
		float2 value = cylinder1.circle.position - cylinder2.circle.position;
		float num = cylinder1.circle.radius + cylinder2.circle.radius;
		if (math.lengthsq(value) < num * num && MathUtils.Intersect(cylinder1.height, cylinder2.height))
		{
			MathUtils.TryNormalize(ref value);
			float2 start = cylinder1.circle.position + value * cylinder1.circle.radius;
			float2 end = cylinder2.circle.position - value * cylinder2.circle.radius;
			pos.y = MathUtils.Center(cylinder1.height & cylinder2.height);
			pos.xz = math.lerp(start, end, 0.5f);
			pos = math.mul(cylinder1.rotation, pos);
			return true;
		}
		return false;
	}
}
