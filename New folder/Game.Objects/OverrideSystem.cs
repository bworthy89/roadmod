using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Objects;

[CompilerGenerated]
public class OverrideSystem : GameSystemBase
{
	private struct TreeAction
	{
		public Entity m_Entity;

		public BoundsMask m_Mask;
	}

	private struct OverridableAction : IComparable<OverridableAction>
	{
		public Entity m_Entity;

		public Entity m_Other;

		public BoundsMask m_Mask;

		public sbyte m_Priority;

		public bool m_OtherOverridden;

		public int CompareTo(OverridableAction other)
		{
			return math.select(m_Entity.Index - other.m_Entity.Index, m_Priority - other.m_Priority, m_Priority != other.m_Priority);
		}
	}

	[BurstCompile]
	private struct UpdateObjectOverrideJob : IJob
	{
		[ReadOnly]
		public ComponentTypeSet m_OverriddenUpdatedSet;

		[ReadOnly]
		public BufferLookup<SubObject> m_SubObjects;

		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_ObjectSearchTree;

		public NativeQueue<TreeAction> m_Actions;

		public NativeQueue<OverridableAction> m_OverridableActions;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			NativeArray<OverridableAction> array = m_OverridableActions.ToArray(Allocator.Temp);
			if (array.Length != 0)
			{
				array.Sort();
				NativeHashMap<Entity, bool> overridden = new NativeHashMap<Entity, bool>(array.Length, Allocator.Temp);
				TreeAction item;
				while (m_Actions.TryDequeue(out item))
				{
					if (m_ObjectSearchTree.TryGet(item.m_Entity, out var bounds))
					{
						bounds.m_Mask = (bounds.m_Mask & ~(BoundsMask.AllLayers | BoundsMask.NotOverridden)) | item.m_Mask;
						m_ObjectSearchTree.Update(item.m_Entity, bounds);
					}
					overridden.Add(item.m_Entity, item.m_Mask == (BoundsMask)0);
				}
				for (int i = 0; i < array.Length; i++)
				{
					OverridableAction overridableAction = array[i];
					overridden[overridableAction.m_Entity] = overridableAction.m_Other != Entity.Null;
				}
				OverridableAction action = default(OverridableAction);
				bool flag = false;
				for (int j = 0; j < array.Length; j++)
				{
					OverridableAction overridableAction2 = array[j];
					if (overridableAction2.m_Entity != action.m_Entity)
					{
						if (action.m_Entity != Entity.Null && action.m_Other != Entity.Null)
						{
							overridden[action.m_Entity] = flag;
							UpdateObject(action, overridden, flag);
						}
						action = overridableAction2;
						flag = false;
					}
					if (!flag && overridableAction2.m_Other != Entity.Null)
					{
						flag = ((!overridden.TryGetValue(overridableAction2.m_Other, out var item2)) ? (!overridableAction2.m_OtherOverridden) : (!item2));
					}
				}
				if (action.m_Entity != Entity.Null && action.m_Other != Entity.Null)
				{
					UpdateObject(action, overridden, flag);
				}
				action = default(OverridableAction);
				for (int k = 0; k < array.Length; k++)
				{
					OverridableAction overridableAction3 = array[k];
					if (overridableAction3.m_Entity != action.m_Entity)
					{
						if (action.m_Entity != Entity.Null && action.m_Other == Entity.Null)
						{
							UpdateObject(action, overridden, overridden[action.m_Entity]);
						}
						action = overridableAction3;
					}
				}
				if (action.m_Entity != Entity.Null && action.m_Other == Entity.Null)
				{
					UpdateObject(action, overridden, overridden[action.m_Entity]);
				}
				overridden.Dispose();
				return;
			}
			TreeAction item3;
			while (m_Actions.TryDequeue(out item3))
			{
				if (m_ObjectSearchTree.TryGet(item3.m_Entity, out var bounds2))
				{
					bounds2.m_Mask = (bounds2.m_Mask & ~(BoundsMask.AllLayers | BoundsMask.NotOverridden)) | item3.m_Mask;
					m_ObjectSearchTree.Update(item3.m_Entity, bounds2);
				}
			}
		}

		private void UpdateObject(OverridableAction action, NativeHashMap<Entity, bool> overridden, bool collision)
		{
			bool flag = (action.m_Priority & 2) != 0;
			if (collision != flag)
			{
				if (collision)
				{
					m_CommandBuffer.AddComponent(action.m_Entity, in m_OverriddenUpdatedSet);
					action.m_Mask = (BoundsMask)0;
				}
				else
				{
					m_CommandBuffer.AddComponent(action.m_Entity, default(Updated));
					m_CommandBuffer.RemoveComponent<Overridden>(action.m_Entity);
				}
				if (m_ObjectSearchTree.TryGet(action.m_Entity, out var bounds))
				{
					bounds.m_Mask = (bounds.m_Mask & ~(BoundsMask.AllLayers | BoundsMask.NotOverridden)) | action.m_Mask;
					m_ObjectSearchTree.Update(action.m_Entity, bounds);
				}
			}
			if (!m_SubObjects.TryGetBuffer(action.m_Entity, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity subObject = bufferData[i].m_SubObject;
				if (overridden.ContainsKey(subObject))
				{
					overridden[subObject] = collision;
				}
			}
		}
	}

	[BurstCompile]
	private struct FindUpdatedObjectsJob : IJobParallelForDefer
	{
		private struct Iterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds2 m_Bounds;

			public NativeQueue<Entity>.ParallelWriter m_ResultQueue;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity objectEntity)
			{
				if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds))
				{
					m_ResultQueue.Enqueue(objectEntity);
				}
			}
		}

		[ReadOnly]
		public NativeArray<Bounds2> m_Bounds;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

		public NativeQueue<Entity>.ParallelWriter m_ResultQueue;

		public void Execute(int index)
		{
			Iterator iterator = new Iterator
			{
				m_Bounds = m_Bounds[index],
				m_ResultQueue = m_ResultQueue
			};
			m_SearchTree.Iterate(ref iterator);
		}
	}

	[BurstCompile]
	private struct CollectObjectsJob : IJob
	{
		public NativeQueue<Entity> m_Queue1;

		public NativeQueue<Entity> m_Queue2;

		public NativeQueue<Entity> m_Queue3;

		public NativeList<Entity> m_ResultList;

		public NativeHashSet<Entity> m_ObjectSet;

		public void Execute()
		{
			Entity item;
			while (m_Queue1.TryDequeue(out item))
			{
				if (m_ObjectSet.Add(item))
				{
					m_ResultList.Add(in item);
				}
			}
			Entity item2;
			while (m_Queue2.TryDequeue(out item2))
			{
				if (m_ObjectSet.Add(item2))
				{
					m_ResultList.Add(in item2);
				}
			}
			Entity item3;
			while (m_Queue3.TryDequeue(out item3))
			{
				if (m_ObjectSet.Add(item3))
				{
					m_ResultList.Add(in item3);
				}
			}
		}
	}

	[BurstCompile]
	private struct CheckObjectOverrideJob : IJobParallelForDefer
	{
		private struct ObjectIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Entity m_TopLevelEntity;

			public Entity m_ObjectEntity;

			public Bounds3 m_ObjectBounds;

			public Transform m_ObjectTransform;

			public Stack m_ObjectStack;

			public CollisionMask m_CollisionMask;

			public ObjectGeometryData m_PrefabGeometryData;

			public StackData m_ObjectStackData;

			public DynamicBuffer<ConnectedEdge> m_TopLevelEdges;

			public DynamicBuffer<ConnectedNode> m_TopLevelNodes;

			public Edge m_TopLevelEdge;

			public ComponentLookup<Owner> m_OwnerData;

			public ComponentLookup<Transform> m_TransformData;

			public ComponentLookup<Elevation> m_ElevationData;

			public ComponentLookup<Attachment> m_AttachmentData;

			public ComponentLookup<Stack> m_StackData;

			public ComponentLookup<Building> m_BuildingData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

			public ComponentLookup<StackData> m_PrefabStackData;

			public NativeList<Entity> m_OverridableCollisions;

			public bool m_CollisionFound;

			public bool m_EditorMode;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				if (m_CollisionFound)
				{
					return false;
				}
				if ((m_CollisionMask & CollisionMask.OnGround) != 0)
				{
					return MathUtils.Intersect(bounds.m_Bounds.xz, m_ObjectBounds.xz);
				}
				return MathUtils.Intersect(bounds.m_Bounds, m_ObjectBounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity objectEntity)
			{
				if (m_CollisionFound)
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
				Entity entity = objectEntity;
				while (m_OwnerData.HasComponent(entity) && !m_BuildingData.HasComponent(entity))
				{
					entity = m_OwnerData[entity].m_Owner;
				}
				if (m_TopLevelEntity == entity || (m_AttachmentData.HasComponent(entity) && m_AttachmentData[entity].m_Attached == m_TopLevelEntity))
				{
					return;
				}
				if (m_TopLevelEdges.IsCreated)
				{
					for (int i = 0; i < m_TopLevelEdges.Length; i++)
					{
						if (m_TopLevelEdges[i].m_Edge == entity)
						{
							return;
						}
					}
				}
				else if (m_TopLevelNodes.IsCreated)
				{
					for (int j = 0; j < m_TopLevelNodes.Length; j++)
					{
						if (m_TopLevelNodes[j].m_Node == entity)
						{
							return;
						}
					}
					if (m_TopLevelEdge.m_Start == entity || m_TopLevelEdge.m_End == entity)
					{
						return;
					}
				}
				PrefabRef prefabRef = m_PrefabRefData[objectEntity];
				if (!m_PrefabObjectGeometryData.HasComponent(prefabRef.m_Prefab))
				{
					return;
				}
				bool overridableCollision = false;
				ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
				if ((objectGeometryData.m_Flags & (GeometryFlags.Overridable | GeometryFlags.DeleteOverridden)) == GeometryFlags.Overridable)
				{
					overridableCollision = true;
				}
				Transform transform = m_TransformData[objectEntity];
				bool ignoreMarkers = !m_EditorMode || m_OwnerData.HasComponent(objectEntity);
				Elevation componentData;
				CollisionMask collisionMask = ((!m_ElevationData.TryGetComponent(objectEntity, out componentData)) ? ObjectUtils.GetCollisionMask(objectGeometryData, ignoreMarkers) : ObjectUtils.GetCollisionMask(objectGeometryData, componentData, ignoreMarkers));
				if ((m_CollisionMask & collisionMask) == 0)
				{
					return;
				}
				float3 @float = MathUtils.Center(bounds.m_Bounds);
				float3 pos = default(float3);
				bool flag = false;
				StackData componentData2 = default(StackData);
				if (m_StackData.TryGetComponent(objectEntity, out var componentData3))
				{
					m_PrefabStackData.TryGetComponent(prefabRef.m_Prefab, out componentData2);
				}
				if (((m_CollisionMask & CollisionMask.OnGround) == 0 || MathUtils.Intersect(bounds.m_Bounds, m_ObjectBounds)) && !flag)
				{
					quaternion q = math.inverse(m_ObjectTransform.m_Rotation);
					quaternion q2 = math.inverse(transform.m_Rotation);
					float3 float2 = math.mul(q, m_ObjectTransform.m_Position - @float);
					float3 float3 = math.mul(q2, transform.m_Position - @float);
					Bounds3 bounds2 = ObjectUtils.GetBounds(m_ObjectStack, m_PrefabGeometryData, m_ObjectStackData);
					if ((m_PrefabGeometryData.m_Flags & GeometryFlags.IgnoreBottomCollision) != GeometryFlags.None)
					{
						bounds2.min.y = math.max(bounds2.min.y, 0f);
					}
					if (ObjectUtils.GetStandingLegCount(m_PrefabGeometryData, out var legCount))
					{
						for (int k = 0; k < legCount; k++)
						{
							float3 float4 = float2 + ObjectUtils.GetStandingLegOffset(m_PrefabGeometryData, k);
							if ((m_PrefabGeometryData.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
							{
								Cylinder3 cylinder = new Cylinder3
								{
									circle = new Circle2(m_PrefabGeometryData.m_LegSize.x * 0.5f - 0.01f, float4.xz),
									height = new Bounds1(bounds2.min.y + 0.01f, m_PrefabGeometryData.m_LegSize.y + 0.01f) + float4.y,
									rotation = m_ObjectTransform.m_Rotation
								};
								Bounds3 bounds3 = ObjectUtils.GetBounds(componentData3, objectGeometryData, componentData2);
								if ((objectGeometryData.m_Flags & GeometryFlags.IgnoreBottomCollision) != GeometryFlags.None)
								{
									bounds3.min.y = math.max(bounds3.min.y, 0f);
								}
								if (ObjectUtils.GetStandingLegCount(objectGeometryData, out var legCount2))
								{
									for (int l = 0; l < legCount2; l++)
									{
										float3 float5 = float3 + ObjectUtils.GetStandingLegOffset(objectGeometryData, l);
										if ((objectGeometryData.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
										{
											if (ValidationHelpers.Intersect(cylinder2: new Cylinder3
											{
												circle = new Circle2(objectGeometryData.m_LegSize.x * 0.5f - 0.01f, float5.xz),
												height = new Bounds1(bounds3.min.y + 0.01f, objectGeometryData.m_LegSize.y + 0.01f) + float5.y,
												rotation = transform.m_Rotation
											}, cylinder1: cylinder, pos: ref pos))
											{
												AddCollision(overridableCollision, objectEntity);
												return;
											}
										}
										else if ((objectGeometryData.m_Flags & GeometryFlags.IgnoreLegCollision) == 0)
										{
											Box3 box = new Box3
											{
												bounds = 
												{
													min = 
													{
														y = bounds3.min.y + 0.01f,
														xz = objectGeometryData.m_LegSize.xz * -0.5f + 0.01f
													},
													max = 
													{
														y = objectGeometryData.m_LegSize.y + 0.01f,
														xz = objectGeometryData.m_LegSize.xz * 0.5f - 0.01f
													}
												}
											};
											box.bounds += float5;
											box.rotation = transform.m_Rotation;
											if (MathUtils.Intersect(cylinder, box, out var _, out var _))
											{
												AddCollision(overridableCollision, objectEntity);
												return;
											}
										}
									}
									bounds3.min.y = objectGeometryData.m_LegSize.y;
								}
								if ((objectGeometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
								{
									if (ValidationHelpers.Intersect(cylinder2: new Cylinder3
									{
										circle = new Circle2(objectGeometryData.m_Size.x * 0.5f - 0.01f, float3.xz),
										height = new Bounds1(bounds3.min.y + 0.01f, bounds3.max.y - 0.01f) + float3.y,
										rotation = transform.m_Rotation
									}, cylinder1: cylinder, pos: ref pos))
									{
										AddCollision(overridableCollision, objectEntity);
										return;
									}
									continue;
								}
								Box3 box2 = default(Box3);
								box2.bounds = bounds3 + float3;
								box2.bounds = MathUtils.Expand(box2.bounds, -0.01f);
								box2.rotation = transform.m_Rotation;
								if (MathUtils.Intersect(cylinder, box2, out var _, out var _))
								{
									AddCollision(overridableCollision, objectEntity);
									return;
								}
							}
							else
							{
								if ((m_PrefabGeometryData.m_Flags & GeometryFlags.IgnoreLegCollision) != GeometryFlags.None)
								{
									continue;
								}
								Box3 box3 = new Box3
								{
									bounds = 
									{
										min = 
										{
											y = bounds2.min.y + 0.01f,
											xz = m_PrefabGeometryData.m_LegSize.xz * -0.5f + 0.01f
										},
										max = 
										{
											y = m_PrefabGeometryData.m_LegSize.y + 0.01f,
											xz = m_PrefabGeometryData.m_LegSize.xz * 0.5f - 0.01f
										}
									}
								};
								box3.bounds += float4;
								box3.rotation = m_ObjectTransform.m_Rotation;
								Bounds3 bounds4 = ObjectUtils.GetBounds(componentData3, objectGeometryData, componentData2);
								if ((objectGeometryData.m_Flags & GeometryFlags.IgnoreBottomCollision) != GeometryFlags.None)
								{
									bounds4.min.y = math.max(bounds4.min.y, 0f);
								}
								if (ObjectUtils.GetStandingLegCount(objectGeometryData, out var legCount3))
								{
									for (int m = 0; m < legCount3; m++)
									{
										float3 float6 = float3 + ObjectUtils.GetStandingLegOffset(objectGeometryData, m);
										if ((objectGeometryData.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
										{
											if (MathUtils.Intersect(new Cylinder3
											{
												circle = new Circle2(objectGeometryData.m_LegSize.x * 0.5f - 0.01f, float6.xz),
												height = new Bounds1(bounds4.min.y + 0.01f, objectGeometryData.m_LegSize.y + 0.01f) + float6.y,
												rotation = transform.m_Rotation
											}, box3, out var _, out var _))
											{
												AddCollision(overridableCollision, objectEntity);
												return;
											}
										}
										else if ((objectGeometryData.m_Flags & GeometryFlags.IgnoreLegCollision) == 0)
										{
											Box3 box4 = new Box3
											{
												bounds = 
												{
													min = 
													{
														y = bounds4.min.y + 0.01f,
														xz = objectGeometryData.m_LegSize.xz * -0.5f + 0.01f
													},
													max = 
													{
														y = objectGeometryData.m_LegSize.y + 0.01f,
														xz = objectGeometryData.m_LegSize.xz * 0.5f - 0.01f
													}
												}
											};
											box4.bounds += float6;
											box4.rotation = transform.m_Rotation;
											if (MathUtils.Intersect(box3, box4, out var _, out var _))
											{
												AddCollision(overridableCollision, objectEntity);
												return;
											}
										}
									}
									bounds4.min.y = objectGeometryData.m_LegSize.y;
								}
								if ((objectGeometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
								{
									if (MathUtils.Intersect(new Cylinder3
									{
										circle = new Circle2(objectGeometryData.m_Size.x * 0.5f - 0.01f, float3.xz),
										height = new Bounds1(bounds4.min.y + 0.01f, bounds4.max.y - 0.01f) + float3.y,
										rotation = transform.m_Rotation
									}, box3, out var _, out var _))
									{
										AddCollision(overridableCollision, objectEntity);
										return;
									}
									continue;
								}
								Box3 box5 = default(Box3);
								box5.bounds = bounds4 + float3;
								box5.bounds = MathUtils.Expand(box5.bounds, -0.01f);
								box5.rotation = transform.m_Rotation;
								if (MathUtils.Intersect(box3, box5, out var _, out var _))
								{
									AddCollision(overridableCollision, objectEntity);
									return;
								}
							}
						}
						bounds2.min.y = m_PrefabGeometryData.m_LegSize.y;
					}
					if ((m_PrefabGeometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
					{
						Cylinder3 cylinder2 = new Cylinder3
						{
							circle = new Circle2(m_PrefabGeometryData.m_Size.x * 0.5f - 0.01f, float2.xz),
							height = new Bounds1(bounds2.min.y + 0.01f, bounds2.max.y - 0.01f) + float2.y,
							rotation = m_ObjectTransform.m_Rotation
						};
						Bounds3 bounds5 = ObjectUtils.GetBounds(componentData3, objectGeometryData, componentData2);
						if ((objectGeometryData.m_Flags & GeometryFlags.IgnoreBottomCollision) != GeometryFlags.None)
						{
							bounds5.min.y = math.max(bounds5.min.y, 0f);
						}
						if (ObjectUtils.GetStandingLegCount(objectGeometryData, out var legCount4))
						{
							for (int n = 0; n < legCount4; n++)
							{
								float3 float7 = float3 + ObjectUtils.GetStandingLegOffset(objectGeometryData, n);
								if ((objectGeometryData.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
								{
									if (ValidationHelpers.Intersect(cylinder2: new Cylinder3
									{
										circle = new Circle2(objectGeometryData.m_LegSize.x * 0.5f - 0.01f, float7.xz),
										height = new Bounds1(bounds5.min.y + 0.01f, objectGeometryData.m_LegSize.y + 0.01f) + float7.y,
										rotation = transform.m_Rotation
									}, cylinder1: cylinder2, pos: ref pos))
									{
										AddCollision(overridableCollision, objectEntity);
										return;
									}
								}
								else if ((objectGeometryData.m_Flags & GeometryFlags.IgnoreLegCollision) == 0)
								{
									Box3 box6 = new Box3
									{
										bounds = 
										{
											min = 
											{
												y = bounds5.min.y + 0.01f,
												xz = objectGeometryData.m_LegSize.xz * -0.5f + 0.01f
											},
											max = 
											{
												y = objectGeometryData.m_LegSize.y + 0.01f,
												xz = objectGeometryData.m_LegSize.xz * 0.5f - 0.01f
											}
										}
									};
									box6.bounds += float7;
									box6.rotation = transform.m_Rotation;
									if (MathUtils.Intersect(cylinder2, box6, out var _, out var _))
									{
										AddCollision(overridableCollision, objectEntity);
										return;
									}
								}
							}
							bounds5.min.y = objectGeometryData.m_LegSize.y;
						}
						if ((objectGeometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
						{
							if (ValidationHelpers.Intersect(cylinder2: new Cylinder3
							{
								circle = new Circle2(objectGeometryData.m_Size.x * 0.5f - 0.01f, float3.xz),
								height = new Bounds1(bounds5.min.y + 0.01f, bounds5.max.y - 0.01f) + float3.y,
								rotation = transform.m_Rotation
							}, cylinder1: cylinder2, pos: ref pos))
							{
								AddCollision(overridableCollision, objectEntity);
								return;
							}
						}
						else
						{
							Box3 box7 = default(Box3);
							box7.bounds = bounds5 + float3;
							box7.bounds = MathUtils.Expand(box7.bounds, -0.01f);
							box7.rotation = transform.m_Rotation;
							if (MathUtils.Intersect(cylinder2, box7, out var _, out var _))
							{
								AddCollision(overridableCollision, objectEntity);
								return;
							}
						}
					}
					else
					{
						Box3 box8 = default(Box3);
						box8.bounds = bounds2 + float2;
						box8.bounds = MathUtils.Expand(box8.bounds, -0.01f);
						box8.rotation = m_ObjectTransform.m_Rotation;
						Bounds3 bounds6 = ObjectUtils.GetBounds(componentData3, objectGeometryData, componentData2);
						if ((objectGeometryData.m_Flags & GeometryFlags.IgnoreBottomCollision) != GeometryFlags.None)
						{
							bounds6.min.y = math.max(bounds6.min.y, 0f);
						}
						if (ObjectUtils.GetStandingLegCount(objectGeometryData, out var legCount5))
						{
							for (int num = 0; num < legCount5; num++)
							{
								float3 float8 = float3 + ObjectUtils.GetStandingLegOffset(objectGeometryData, num);
								if ((objectGeometryData.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
								{
									if (MathUtils.Intersect(new Cylinder3
									{
										circle = new Circle2(objectGeometryData.m_LegSize.x * 0.5f - 0.01f, float8.xz),
										height = new Bounds1(bounds6.min.y + 0.01f, objectGeometryData.m_LegSize.y + 0.01f) + float8.y,
										rotation = transform.m_Rotation
									}, box8, out var _, out var _))
									{
										AddCollision(overridableCollision, objectEntity);
										return;
									}
								}
								else if ((objectGeometryData.m_Flags & GeometryFlags.IgnoreLegCollision) == 0)
								{
									Box3 box9 = new Box3
									{
										bounds = 
										{
											min = 
											{
												y = bounds6.min.y + 0.01f,
												xz = objectGeometryData.m_LegSize.xz * -0.5f + 0.01f
											},
											max = 
											{
												y = objectGeometryData.m_LegSize.y + 0.01f,
												xz = objectGeometryData.m_LegSize.xz * 0.5f - 0.01f
											}
										}
									};
									box9.bounds += float8;
									box9.rotation = transform.m_Rotation;
									if (MathUtils.Intersect(box8, box9, out var _, out var _))
									{
										AddCollision(overridableCollision, objectEntity);
										return;
									}
								}
							}
							bounds6.min.y = objectGeometryData.m_LegSize.y;
						}
						if ((objectGeometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
						{
							if (MathUtils.Intersect(new Cylinder3
							{
								circle = new Circle2(objectGeometryData.m_Size.x * 0.5f - 0.01f, float3.xz),
								height = new Bounds1(bounds6.min.y + 0.01f, bounds6.max.y - 0.01f) + float3.y,
								rotation = transform.m_Rotation
							}, box8, out var _, out var _))
							{
								AddCollision(overridableCollision, objectEntity);
								return;
							}
						}
						else
						{
							Box3 box10 = default(Box3);
							box10.bounds = bounds6 + float3;
							box10.bounds = MathUtils.Expand(box10.bounds, -0.01f);
							box10.rotation = transform.m_Rotation;
							if (MathUtils.Intersect(box8, box10, out var _, out var _))
							{
								AddCollision(overridableCollision, objectEntity);
								return;
							}
						}
					}
				}
				if (!CommonUtils.ExclusiveGroundCollision(m_CollisionMask, collisionMask))
				{
					return;
				}
				if (ObjectUtils.GetStandingLegCount(m_PrefabGeometryData, out var legCount6))
				{
					for (int num2 = 0; num2 < legCount6; num2++)
					{
						float3 position = ObjectUtils.GetStandingLegPosition(m_PrefabGeometryData, m_ObjectTransform, num2) - @float;
						if ((m_PrefabGeometryData.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
						{
							Circle2 circle = new Circle2(m_PrefabGeometryData.m_LegSize.x * 0.5f - 0.01f, position.xz);
							Bounds2 intersection10;
							if (ObjectUtils.GetStandingLegCount(objectGeometryData, out var legCount7))
							{
								for (int num3 = 0; num3 < legCount7; num3++)
								{
									float3 position2 = ObjectUtils.GetStandingLegPosition(objectGeometryData, transform, num3) - @float;
									Bounds2 intersection9;
									if ((objectGeometryData.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
									{
										Circle2 circle2 = new Circle2(objectGeometryData.m_LegSize.x * 0.5f - 0.01f, position2.xz);
										if (MathUtils.Intersect(circle, circle2))
										{
											AddCollision(overridableCollision, objectEntity);
											return;
										}
									}
									else if ((objectGeometryData.m_Flags & GeometryFlags.IgnoreLegCollision) == 0 && MathUtils.Intersect(ObjectUtils.CalculateBaseCorners(bounds: MathUtils.Expand(new Bounds3
									{
										min = 
										{
											xz = objectGeometryData.m_LegSize.xz * -0.5f
										},
										max = 
										{
											xz = objectGeometryData.m_LegSize.xz * 0.5f
										}
									}, -0.01f), position: position2, rotation: transform.m_Rotation).xz, circle, out intersection9))
									{
										AddCollision(overridableCollision, objectEntity);
										return;
									}
								}
							}
							else if ((objectGeometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
							{
								Circle2 circle3 = new Circle2(objectGeometryData.m_Size.x * 0.5f - 0.01f, (transform.m_Position - @float).xz);
								if (MathUtils.Intersect(circle, circle3))
								{
									AddCollision(overridableCollision, objectEntity);
									break;
								}
							}
							else if (MathUtils.Intersect(ObjectUtils.CalculateBaseCorners(transform.m_Position - @float, transform.m_Rotation, MathUtils.Expand(objectGeometryData.m_Bounds, -0.01f)).xz, circle, out intersection10))
							{
								AddCollision(overridableCollision, objectEntity);
								break;
							}
						}
						else
						{
							if ((m_PrefabGeometryData.m_Flags & GeometryFlags.IgnoreLegCollision) != GeometryFlags.None)
							{
								continue;
							}
							Quad2 xz = ObjectUtils.CalculateBaseCorners(bounds: MathUtils.Expand(new Bounds3
							{
								min = 
								{
									xz = m_PrefabGeometryData.m_LegSize.xz * -0.5f
								},
								max = 
								{
									xz = m_PrefabGeometryData.m_LegSize.xz * 0.5f
								}
							}, -0.01f), position: position, rotation: m_ObjectTransform.m_Rotation).xz;
							if (ObjectUtils.GetStandingLegCount(objectGeometryData, out var legCount8))
							{
								for (int num4 = 0; num4 < legCount8; num4++)
								{
									float3 position3 = ObjectUtils.GetStandingLegPosition(objectGeometryData, transform, num4) - @float;
									if ((objectGeometryData.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
									{
										Circle2 circle4 = new Circle2(objectGeometryData.m_LegSize.x * 0.5f - 0.01f, position3.xz);
										if (MathUtils.Intersect(xz, circle4, out var _))
										{
											AddCollision(overridableCollision, objectEntity);
											return;
										}
									}
									else if ((objectGeometryData.m_Flags & GeometryFlags.IgnoreLegCollision) == 0)
									{
										Quad2 xz2 = ObjectUtils.CalculateBaseCorners(bounds: MathUtils.Expand(new Bounds3
										{
											min = 
											{
												xz = objectGeometryData.m_LegSize.xz * -0.5f
											},
											max = 
											{
												xz = objectGeometryData.m_LegSize.xz * 0.5f
											}
										}, -0.01f), position: position3, rotation: transform.m_Rotation).xz;
										if (MathUtils.Intersect(xz, xz2, out var _))
										{
											AddCollision(overridableCollision, objectEntity);
											return;
										}
									}
								}
							}
							else if ((objectGeometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
							{
								Circle2 circle5 = new Circle2(objectGeometryData.m_Size.x * 0.5f - 0.01f, (transform.m_Position - @float).xz);
								if (MathUtils.Intersect(xz, circle5, out var _))
								{
									AddCollision(overridableCollision, objectEntity);
									break;
								}
							}
							else
							{
								Quad2 xz3 = ObjectUtils.CalculateBaseCorners(transform.m_Position - @float, transform.m_Rotation, MathUtils.Expand(objectGeometryData.m_Bounds, -0.01f)).xz;
								if (MathUtils.Intersect(xz, xz3, out var _))
								{
									AddCollision(overridableCollision, objectEntity);
									break;
								}
							}
						}
					}
					return;
				}
				if ((m_PrefabGeometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
				{
					Circle2 circle6 = new Circle2(m_PrefabGeometryData.m_Size.x * 0.5f - 0.01f, (m_ObjectTransform.m_Position - @float).xz);
					Bounds2 intersection16;
					if (ObjectUtils.GetStandingLegCount(objectGeometryData, out var legCount9))
					{
						for (int num5 = 0; num5 < legCount9; num5++)
						{
							float3 position4 = ObjectUtils.GetStandingLegPosition(objectGeometryData, transform, num5) - @float;
							Bounds2 intersection15;
							if ((objectGeometryData.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
							{
								if (MathUtils.Intersect(circle2: new Circle2(objectGeometryData.m_LegSize.x * 0.5f - 0.01f, position4.xz), circle1: circle6))
								{
									AddCollision(overridableCollision, objectEntity);
									break;
								}
							}
							else if ((objectGeometryData.m_Flags & GeometryFlags.IgnoreLegCollision) == 0 && MathUtils.Intersect(ObjectUtils.CalculateBaseCorners(bounds: MathUtils.Expand(new Bounds3
							{
								min = 
								{
									xz = objectGeometryData.m_LegSize.xz * -0.5f
								},
								max = 
								{
									xz = objectGeometryData.m_LegSize.xz * 0.5f
								}
							}, -0.01f), position: position4, rotation: transform.m_Rotation).xz, circle6, out intersection15))
							{
								AddCollision(overridableCollision, objectEntity);
								break;
							}
						}
					}
					else if ((objectGeometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
					{
						Circle2 circle7 = new Circle2(objectGeometryData.m_Size.x * 0.5f - 0.01f, (transform.m_Position - @float).xz);
						if (MathUtils.Intersect(circle6, circle7))
						{
							AddCollision(overridableCollision, objectEntity);
						}
					}
					else if (MathUtils.Intersect(ObjectUtils.CalculateBaseCorners(transform.m_Position - @float, transform.m_Rotation, MathUtils.Expand(objectGeometryData.m_Bounds, -0.01f)).xz, circle6, out intersection16))
					{
						AddCollision(overridableCollision, objectEntity);
					}
					return;
				}
				Quad2 xz4 = ObjectUtils.CalculateBaseCorners(m_ObjectTransform.m_Position - @float, m_ObjectTransform.m_Rotation, MathUtils.Expand(m_PrefabGeometryData.m_Bounds, -0.01f)).xz;
				if (ObjectUtils.GetStandingLegCount(objectGeometryData, out var legCount10))
				{
					for (int num6 = 0; num6 < legCount10; num6++)
					{
						float3 position5 = ObjectUtils.GetStandingLegPosition(objectGeometryData, transform, num6) - @float;
						if ((objectGeometryData.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
						{
							Circle2 circle8 = new Circle2(objectGeometryData.m_LegSize.x * 0.5f - 0.01f, position5.xz);
							if (MathUtils.Intersect(xz4, circle8, out var _))
							{
								AddCollision(overridableCollision, objectEntity);
								break;
							}
						}
						else if ((objectGeometryData.m_Flags & GeometryFlags.IgnoreLegCollision) == 0)
						{
							Quad2 xz5 = ObjectUtils.CalculateBaseCorners(bounds: MathUtils.Expand(new Bounds3
							{
								min = 
								{
									xz = objectGeometryData.m_LegSize.xz * -0.5f
								},
								max = 
								{
									xz = objectGeometryData.m_LegSize.xz * 0.5f
								}
							}, -0.01f), position: position5, rotation: transform.m_Rotation).xz;
							if (MathUtils.Intersect(xz4, xz5, out var _))
							{
								AddCollision(overridableCollision, objectEntity);
								break;
							}
						}
					}
				}
				else if ((objectGeometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
				{
					Circle2 circle9 = new Circle2(objectGeometryData.m_Size.x * 0.5f - 0.01f, (transform.m_Position - @float).xz);
					if (MathUtils.Intersect(xz4, circle9, out var _))
					{
						AddCollision(overridableCollision, objectEntity);
					}
				}
				else
				{
					Quad2 xz6 = ObjectUtils.CalculateBaseCorners(transform.m_Position - @float, transform.m_Rotation, MathUtils.Expand(objectGeometryData.m_Bounds, -0.01f)).xz;
					if (MathUtils.Intersect(xz4, xz6, out var _))
					{
						AddCollision(overridableCollision, objectEntity);
					}
				}
			}

			private void AddCollision(bool overridableCollision, Entity other)
			{
				if (overridableCollision)
				{
					if (!m_OverridableCollisions.IsCreated)
					{
						m_OverridableCollisions = new NativeList<Entity>(10, Allocator.Temp);
					}
					m_OverridableCollisions.Add(in other);
				}
				else
				{
					m_CollisionFound = true;
				}
			}
		}

		private struct NetIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Entity m_TopLevelEntity;

			public Entity m_AttachedParent;

			public Entity m_ObjectEntity;

			public Bounds3 m_ObjectBounds;

			public CollisionMask m_CollisionMask;

			public Transform m_ObjectTransform;

			public Stack m_ObjectStack;

			public ObjectGeometryData m_PrefabGeometryData;

			public StackData m_ObjectStackData;

			public ComponentLookup<Owner> m_OwnerData;

			public ComponentLookup<Building> m_BuildingData;

			public ComponentLookup<Edge> m_EdgeData;

			public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

			public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

			public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

			public ComponentLookup<Composition> m_CompositionData;

			public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

			public bool m_CollisionFound;

			public bool m_EditorMode;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				if (m_CollisionFound)
				{
					return false;
				}
				if ((m_CollisionMask & CollisionMask.OnGround) != 0)
				{
					return MathUtils.Intersect(bounds.m_Bounds.xz, m_ObjectBounds.xz);
				}
				return MathUtils.Intersect(bounds.m_Bounds, m_ObjectBounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity edgeEntity)
			{
				if (m_CollisionFound)
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
				if (!m_EdgeGeometryData.HasComponent(edgeEntity))
				{
					return;
				}
				Edge edge = m_EdgeData[edgeEntity];
				Entity entity = m_ObjectEntity;
				if (edgeEntity == m_AttachedParent || edge.m_Start == m_AttachedParent || edge.m_End == m_AttachedParent)
				{
					return;
				}
				Owner componentData;
				while (m_OwnerData.TryGetComponent(entity, out componentData))
				{
					entity = componentData.m_Owner;
					if (edgeEntity == entity || edge.m_Start == entity || edge.m_End == entity)
					{
						return;
					}
				}
				Entity entity2 = edgeEntity;
				bool flag = false;
				Owner componentData2;
				while (m_OwnerData.TryGetComponent(entity2, out componentData2) && !m_BuildingData.HasComponent(entity2))
				{
					flag = true;
					entity2 = componentData2.m_Owner;
				}
				if (m_TopLevelEntity == entity2)
				{
					return;
				}
				Composition composition = m_CompositionData[edgeEntity];
				EdgeGeometry edgeGeometry = m_EdgeGeometryData[edgeEntity];
				StartNodeGeometry startNodeGeometry = m_StartNodeGeometryData[edgeEntity];
				EndNodeGeometry endNodeGeometry = m_EndNodeGeometryData[edgeEntity];
				NetCompositionData netCompositionData = m_PrefabCompositionData[composition.m_Edge];
				NetCompositionData netCompositionData2 = m_PrefabCompositionData[composition.m_StartNode];
				NetCompositionData netCompositionData3 = m_PrefabCompositionData[composition.m_EndNode];
				CollisionMask collisionMask = NetUtils.GetCollisionMask(netCompositionData, !m_EditorMode || flag);
				CollisionMask collisionMask2 = NetUtils.GetCollisionMask(netCompositionData2, !m_EditorMode || flag);
				CollisionMask collisionMask3 = NetUtils.GetCollisionMask(netCompositionData3, !m_EditorMode || flag);
				CollisionMask collisionMask4 = collisionMask | collisionMask2 | collisionMask3;
				if ((m_CollisionMask & collisionMask4) == 0)
				{
					return;
				}
				DynamicBuffer<NetCompositionArea> areas = default(DynamicBuffer<NetCompositionArea>);
				DynamicBuffer<NetCompositionArea> areas2 = default(DynamicBuffer<NetCompositionArea>);
				DynamicBuffer<NetCompositionArea> areas3 = default(DynamicBuffer<NetCompositionArea>);
				float3 @float = MathUtils.Center(bounds.m_Bounds);
				Bounds3 intersection = default(Bounds3);
				Bounds2 intersection2 = default(Bounds2);
				if ((m_CollisionMask & CollisionMask.OnGround) == 0 || MathUtils.Intersect(bounds.m_Bounds, m_ObjectBounds))
				{
					float3 float2 = math.mul(math.inverse(m_ObjectTransform.m_Rotation), m_ObjectTransform.m_Position - @float);
					Bounds3 bounds2 = ObjectUtils.GetBounds(m_ObjectStack, m_PrefabGeometryData, m_ObjectStackData);
					if ((m_PrefabGeometryData.m_Flags & GeometryFlags.IgnoreBottomCollision) != GeometryFlags.None)
					{
						bounds2.min.y = math.max(bounds2.min.y, 0f);
					}
					Game.Net.ValidationHelpers.Check3DCollisionMasks(collisionMask, m_CollisionMask, netCompositionData, out var outData);
					Game.Net.ValidationHelpers.Check3DCollisionMasks(collisionMask2, m_CollisionMask, netCompositionData2, out var outData2);
					Game.Net.ValidationHelpers.Check3DCollisionMasks(collisionMask3, m_CollisionMask, netCompositionData3, out var outData3);
					if (ObjectUtils.GetStandingLegCount(m_PrefabGeometryData, out var legCount))
					{
						for (int i = 0; i < legCount; i++)
						{
							float3 float3 = float2 + ObjectUtils.GetStandingLegOffset(m_PrefabGeometryData, i);
							if ((m_PrefabGeometryData.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
							{
								Cylinder3 cylinder = new Cylinder3
								{
									circle = new Circle2(m_PrefabGeometryData.m_LegSize.x * 0.5f, float3.xz),
									height = new Bounds1(bounds2.min.y, m_PrefabGeometryData.m_LegSize.y) + float3.y,
									rotation = m_ObjectTransform.m_Rotation
								};
								if ((collisionMask & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edge, m_ObjectEntity, edgeGeometry, -@float, cylinder, m_ObjectBounds, outData, areas, ref intersection))
								{
									m_CollisionFound = true;
									return;
								}
								if ((collisionMask2 & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edge.m_Start, m_ObjectEntity, startNodeGeometry.m_Geometry, -@float, cylinder, m_ObjectBounds, outData2, areas2, ref intersection))
								{
									m_CollisionFound = true;
									return;
								}
								if ((collisionMask3 & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edge.m_End, m_ObjectEntity, endNodeGeometry.m_Geometry, -@float, cylinder, m_ObjectBounds, outData3, areas3, ref intersection))
								{
									m_CollisionFound = true;
									return;
								}
							}
							else if ((m_PrefabGeometryData.m_Flags & GeometryFlags.IgnoreLegCollision) == 0)
							{
								Box3 box = new Box3
								{
									bounds = 
									{
										min = 
										{
											y = bounds2.min.y,
											xz = m_PrefabGeometryData.m_LegSize.xz * -0.5f
										},
										max = 
										{
											y = m_PrefabGeometryData.m_LegSize.y,
											xz = m_PrefabGeometryData.m_LegSize.xz * 0.5f
										}
									}
								};
								box.bounds += float3;
								box.rotation = m_ObjectTransform.m_Rotation;
								if ((collisionMask & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edge, m_ObjectEntity, edgeGeometry, -@float, box, m_ObjectBounds, outData, areas, ref intersection))
								{
									m_CollisionFound = true;
									return;
								}
								if ((collisionMask2 & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edge.m_Start, m_ObjectEntity, startNodeGeometry.m_Geometry, -@float, box, m_ObjectBounds, outData2, areas2, ref intersection))
								{
									m_CollisionFound = true;
									return;
								}
								if ((collisionMask3 & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edge.m_End, m_ObjectEntity, endNodeGeometry.m_Geometry, -@float, box, m_ObjectBounds, outData3, areas3, ref intersection))
								{
									m_CollisionFound = true;
									return;
								}
							}
						}
						bounds2.min.y = m_PrefabGeometryData.m_LegSize.y;
					}
					if ((m_PrefabGeometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
					{
						Cylinder3 cylinder2 = new Cylinder3
						{
							circle = new Circle2(m_PrefabGeometryData.m_Size.x * 0.5f, float2.xz),
							height = new Bounds1(bounds2.min.y, bounds2.max.y) + float2.y,
							rotation = m_ObjectTransform.m_Rotation
						};
						if ((collisionMask & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edge, m_ObjectEntity, edgeGeometry, -@float, cylinder2, m_ObjectBounds, outData, areas, ref intersection))
						{
							m_CollisionFound = true;
							return;
						}
						if ((collisionMask2 & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edge.m_Start, m_ObjectEntity, startNodeGeometry.m_Geometry, -@float, cylinder2, m_ObjectBounds, outData2, areas2, ref intersection))
						{
							m_CollisionFound = true;
							return;
						}
						if ((collisionMask3 & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edge.m_End, m_ObjectEntity, endNodeGeometry.m_Geometry, -@float, cylinder2, m_ObjectBounds, outData3, areas3, ref intersection))
						{
							m_CollisionFound = true;
							return;
						}
					}
					else
					{
						Box3 box2 = new Box3
						{
							bounds = bounds2 + float2,
							rotation = m_ObjectTransform.m_Rotation
						};
						if ((collisionMask & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edge, m_ObjectEntity, edgeGeometry, -@float, box2, m_ObjectBounds, outData, areas, ref intersection))
						{
							m_CollisionFound = true;
							return;
						}
						if ((collisionMask2 & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edge.m_Start, m_ObjectEntity, startNodeGeometry.m_Geometry, -@float, box2, m_ObjectBounds, outData2, areas2, ref intersection))
						{
							m_CollisionFound = true;
							return;
						}
						if ((collisionMask3 & m_CollisionMask) != 0 && Game.Net.ValidationHelpers.Intersect(edge.m_End, m_ObjectEntity, endNodeGeometry.m_Geometry, -@float, box2, m_ObjectBounds, outData3, areas3, ref intersection))
						{
							m_CollisionFound = true;
							return;
						}
					}
				}
				if (!CommonUtils.ExclusiveGroundCollision(m_CollisionMask, collisionMask4))
				{
					return;
				}
				if (ObjectUtils.GetStandingLegCount(m_PrefabGeometryData, out var legCount2))
				{
					for (int j = 0; j < legCount2; j++)
					{
						float3 position = ObjectUtils.GetStandingLegPosition(m_PrefabGeometryData, m_ObjectTransform, j) - @float;
						if ((m_PrefabGeometryData.m_Flags & (GeometryFlags.CircularLeg | GeometryFlags.IgnoreLegCollision)) == GeometryFlags.CircularLeg)
						{
							Circle2 circle = new Circle2(m_PrefabGeometryData.m_LegSize.x * 0.5f, position.xz);
							if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, collisionMask) && Game.Net.ValidationHelpers.Intersect(edge, m_ObjectEntity, edgeGeometry, -@float.xz, circle, m_ObjectBounds.xz, netCompositionData, areas, ref intersection2))
							{
								m_CollisionFound = true;
								break;
							}
							if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, collisionMask2) && Game.Net.ValidationHelpers.Intersect(edge.m_Start, m_ObjectEntity, startNodeGeometry.m_Geometry, -@float.xz, circle, m_ObjectBounds.xz, netCompositionData2, areas2, ref intersection2))
							{
								m_CollisionFound = true;
								break;
							}
							if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, collisionMask3) && Game.Net.ValidationHelpers.Intersect(edge.m_End, m_ObjectEntity, endNodeGeometry.m_Geometry, -@float.xz, circle, m_ObjectBounds.xz, netCompositionData3, areas3, ref intersection2))
							{
								m_CollisionFound = true;
								break;
							}
						}
						else if ((m_PrefabGeometryData.m_Flags & GeometryFlags.IgnoreLegCollision) == 0)
						{
							Quad2 xz = ObjectUtils.CalculateBaseCorners(bounds: new Bounds3
							{
								min = 
								{
									xz = m_PrefabGeometryData.m_LegSize.xz * -0.5f
								},
								max = 
								{
									xz = m_PrefabGeometryData.m_LegSize.xz * 0.5f
								}
							}, position: position, rotation: m_ObjectTransform.m_Rotation).xz;
							if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, collisionMask) && Game.Net.ValidationHelpers.Intersect(edge, m_ObjectEntity, edgeGeometry, -@float.xz, xz, m_ObjectBounds.xz, netCompositionData, areas, ref intersection2))
							{
								m_CollisionFound = true;
								break;
							}
							if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, collisionMask2) && Game.Net.ValidationHelpers.Intersect(edge.m_Start, m_ObjectEntity, startNodeGeometry.m_Geometry, -@float.xz, xz, m_ObjectBounds.xz, netCompositionData2, areas2, ref intersection2))
							{
								m_CollisionFound = true;
								break;
							}
							if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, collisionMask3) && Game.Net.ValidationHelpers.Intersect(edge.m_End, m_ObjectEntity, endNodeGeometry.m_Geometry, -@float.xz, xz, m_ObjectBounds.xz, netCompositionData3, areas3, ref intersection2))
							{
								m_CollisionFound = true;
								break;
							}
						}
					}
				}
				else if ((m_PrefabGeometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
				{
					Circle2 circle2 = new Circle2(m_PrefabGeometryData.m_Size.x * 0.5f, (m_ObjectTransform.m_Position - @float).xz);
					if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, collisionMask) && Game.Net.ValidationHelpers.Intersect(edge, m_ObjectEntity, edgeGeometry, -@float.xz, circle2, m_ObjectBounds.xz, netCompositionData, areas, ref intersection2))
					{
						m_CollisionFound = true;
					}
					else if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, collisionMask2) && Game.Net.ValidationHelpers.Intersect(edge.m_Start, m_ObjectEntity, startNodeGeometry.m_Geometry, -@float.xz, circle2, m_ObjectBounds.xz, netCompositionData2, areas2, ref intersection2))
					{
						m_CollisionFound = true;
					}
					else if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, collisionMask3) && Game.Net.ValidationHelpers.Intersect(edge.m_End, m_ObjectEntity, endNodeGeometry.m_Geometry, -@float.xz, circle2, m_ObjectBounds.xz, netCompositionData3, areas3, ref intersection2))
					{
						m_CollisionFound = true;
					}
				}
				else
				{
					Quad2 xz2 = ObjectUtils.CalculateBaseCorners(m_ObjectTransform.m_Position - @float, m_ObjectTransform.m_Rotation, m_PrefabGeometryData.m_Bounds).xz;
					if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, collisionMask) && Game.Net.ValidationHelpers.Intersect(edge, m_ObjectEntity, edgeGeometry, -@float.xz, xz2, m_ObjectBounds.xz, netCompositionData, areas, ref intersection2))
					{
						m_CollisionFound = true;
					}
					else if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, collisionMask2) && Game.Net.ValidationHelpers.Intersect(edge.m_Start, m_ObjectEntity, startNodeGeometry.m_Geometry, -@float.xz, xz2, m_ObjectBounds.xz, netCompositionData2, areas2, ref intersection2))
					{
						m_CollisionFound = true;
					}
					else if (CommonUtils.ExclusiveGroundCollision(m_CollisionMask, collisionMask3) && Game.Net.ValidationHelpers.Intersect(edge.m_End, m_ObjectEntity, endNodeGeometry.m_Geometry, -@float.xz, xz2, m_ObjectBounds.xz, netCompositionData3, areas3, ref intersection2))
					{
						m_CollisionFound = true;
					}
				}
			}
		}

		private struct AreaIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public Entity m_TopLevelEntity;

			public Entity m_TopLevelArea;

			public Entity m_ObjectEntity;

			public Bounds3 m_ObjectBounds;

			public CollisionMask m_CollisionMask;

			public Transform m_ObjectTransform;

			public ObjectGeometryData m_PrefabGeometryData;

			public ComponentLookup<Owner> m_OwnerData;

			public ComponentLookup<Building> m_BuildingData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<AreaGeometryData> m_PrefabAreaGeometryData;

			public BufferLookup<Game.Areas.Node> m_AreaNodes;

			public BufferLookup<Triangle> m_AreaTriangles;

			public bool m_CollisionFound;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				if (m_CollisionFound)
				{
					return false;
				}
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_ObjectBounds.xz);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem areaItem)
			{
				if (m_CollisionFound || !MathUtils.Intersect(bounds.m_Bounds.xz, m_ObjectBounds.xz))
				{
					return;
				}
				Entity entity = areaItem.m_Area;
				if (m_TopLevelArea == entity)
				{
					return;
				}
				Owner componentData;
				while (m_OwnerData.TryGetComponent(entity, out componentData) && !m_BuildingData.HasComponent(entity))
				{
					entity = componentData.m_Owner;
					if (m_TopLevelArea == entity)
					{
						return;
					}
				}
				if (m_TopLevelEntity == entity)
				{
					return;
				}
				PrefabRef prefabRef = m_PrefabRefData[areaItem.m_Area];
				AreaGeometryData areaGeometryData = m_PrefabAreaGeometryData[prefabRef.m_Prefab];
				if ((areaGeometryData.m_Flags & Game.Areas.GeometryFlags.CanOverrideObjects) == 0)
				{
					return;
				}
				CollisionMask collisionMask = AreaUtils.GetCollisionMask(areaGeometryData);
				if ((m_CollisionMask & collisionMask) == 0)
				{
					return;
				}
				Triangle2 xz = AreaUtils.GetTriangle3(m_AreaNodes[areaItem.m_Area], m_AreaTriangles[areaItem.m_Area][areaItem.m_Triangle]).xz;
				if ((m_PrefabGeometryData.m_Flags & GeometryFlags.Circular) != GeometryFlags.None)
				{
					Circle2 circle = new Circle2(m_PrefabGeometryData.m_Size.x * 0.5f, m_ObjectTransform.m_Position.xz);
					if (MathUtils.Intersect(xz, circle))
					{
						m_CollisionFound = true;
					}
				}
				else if (MathUtils.Intersect(ObjectUtils.CalculateBaseCorners(m_ObjectTransform.m_Position, m_ObjectTransform.m_Rotation, m_PrefabGeometryData.m_Bounds).xz, xz))
				{
					m_CollisionFound = true;
				}
			}
		}

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Created> m_CreatedData;

		[ReadOnly]
		public ComponentLookup<Overridden> m_OverriddenData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<Attachment> m_AttachmentData;

		[ReadOnly]
		public ComponentLookup<Attached> m_AttachedData;

		[ReadOnly]
		public ComponentLookup<Stack> m_StackData;

		[ReadOnly]
		public ComponentLookup<Marker> m_MarkerData;

		[ReadOnly]
		public ComponentLookup<OutsideConnection> m_OutsideConnectionData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> m_PrefabAreaGeometryData;

		[ReadOnly]
		public ComponentLookup<NetData> m_PrefabNetData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		[ReadOnly]
		public ComponentLookup<StackData> m_PrefabStackData;

		[ReadOnly]
		public BufferLookup<SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<ConnectedNode> m_ConnectedNodes;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_AreaTriangles;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public NativeArray<Entity> m_ObjectArray;

		[ReadOnly]
		public NativeHashSet<Entity> m_ObjectSet;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_ObjectSearchTree;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaSearchTree;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<TreeAction>.ParallelWriter m_TreeActions;

		public NativeQueue<OverridableAction>.ParallelWriter m_OverridableActions;

		public void Execute(int index)
		{
			Entity entity = m_ObjectArray[index];
			PrefabRef prefabRef = m_PrefabRefData[entity];
			if (!m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData) || (componentData.m_Flags & (GeometryFlags.Overridable | GeometryFlags.DeleteOverridden)) != GeometryFlags.Overridable)
			{
				return;
			}
			NativeList<Entity> overridableCollisions = default(NativeList<Entity>);
			Entity entity2 = entity;
			bool flag = false;
			Owner componentData2;
			while (m_OwnerData.TryGetComponent(entity2, out componentData2))
			{
				entity2 = componentData2.m_Owner;
				if (m_PrefabRefData.HasComponent(componentData2.m_Owner))
				{
					PrefabRef prefabRef2 = m_PrefabRefData[componentData2.m_Owner];
					if (!m_PrefabObjectGeometryData.TryGetComponent(prefabRef2.m_Prefab, out var componentData3) || (componentData3.m_Flags & (GeometryFlags.Overridable | GeometryFlags.DeleteOverridden)) != GeometryFlags.Overridable)
					{
						break;
					}
					if (m_ObjectSet.Contains(entity2))
					{
						return;
					}
					flag |= m_OverriddenData.HasComponent(entity2);
				}
			}
			CheckObject(index, entity, prefabRef, componentData, flag, delayedResolve: false, ref overridableCollisions);
			if (overridableCollisions.IsCreated)
			{
				overridableCollisions.Dispose();
			}
		}

		private void CheckObject(int jobIndex, Entity entity, PrefabRef prefabRef, ObjectGeometryData prefabGeometryData, bool collision, bool delayedResolve, ref NativeList<Entity> overridableCollisions)
		{
			if (!collision)
			{
				Transform objectTransform = m_TransformData[entity];
				bool ignoreMarkers = !m_EditorMode || m_OwnerData.HasComponent(entity);
				StackData componentData = default(StackData);
				Stack componentData2;
				Bounds3 objectBounds = ((!m_StackData.TryGetComponent(entity, out componentData2) || !m_PrefabStackData.TryGetComponent(prefabRef.m_Prefab, out componentData)) ? ObjectUtils.CalculateBounds(objectTransform.m_Position, objectTransform.m_Rotation, prefabGeometryData) : ObjectUtils.CalculateBounds(objectTransform.m_Position, objectTransform.m_Rotation, componentData2, prefabGeometryData, componentData));
				Elevation componentData3;
				CollisionMask collisionMask = ((!m_ElevationData.TryGetComponent(entity, out componentData3)) ? ObjectUtils.GetCollisionMask(prefabGeometryData, ignoreMarkers) : ObjectUtils.GetCollisionMask(prefabGeometryData, componentData3, ignoreMarkers));
				Entity entity2 = entity;
				Entity attachedParent = Entity.Null;
				Owner componentData4;
				while (m_OwnerData.TryGetComponent(entity2, out componentData4) && !m_BuildingData.HasComponent(entity2))
				{
					entity2 = componentData4.m_Owner;
					if (m_AttachedData.TryGetComponent(componentData4.m_Owner, out var componentData5))
					{
						attachedParent = componentData5.m_Parent;
					}
				}
				Entity entity3 = entity2;
				Owner componentData6;
				while (m_OwnerData.TryGetComponent(entity3, out componentData6) && m_AreaNodes.HasBuffer(componentData6.m_Owner))
				{
					entity3 = componentData6.m_Owner;
				}
				if (overridableCollisions.IsCreated)
				{
					overridableCollisions.Clear();
				}
				ObjectIterator iterator = new ObjectIterator
				{
					m_TopLevelEntity = entity2,
					m_ObjectEntity = entity,
					m_ObjectBounds = objectBounds,
					m_CollisionMask = collisionMask,
					m_ObjectTransform = objectTransform,
					m_ObjectStack = componentData2,
					m_PrefabGeometryData = prefabGeometryData,
					m_ObjectStackData = componentData,
					m_OwnerData = m_OwnerData,
					m_TransformData = m_TransformData,
					m_ElevationData = m_ElevationData,
					m_AttachmentData = m_AttachmentData,
					m_StackData = m_StackData,
					m_BuildingData = m_BuildingData,
					m_PrefabRefData = m_PrefabRefData,
					m_PrefabObjectGeometryData = m_PrefabObjectGeometryData,
					m_PrefabStackData = m_PrefabStackData,
					m_OverridableCollisions = overridableCollisions,
					m_EditorMode = m_EditorMode
				};
				NetIterator iterator2 = new NetIterator
				{
					m_TopLevelEntity = entity2,
					m_AttachedParent = attachedParent,
					m_ObjectEntity = entity,
					m_ObjectBounds = objectBounds,
					m_CollisionMask = collisionMask,
					m_ObjectTransform = objectTransform,
					m_ObjectStack = componentData2,
					m_PrefabGeometryData = prefabGeometryData,
					m_ObjectStackData = componentData,
					m_OwnerData = m_OwnerData,
					m_BuildingData = m_BuildingData,
					m_EdgeData = m_EdgeData,
					m_EdgeGeometryData = m_EdgeGeometryData,
					m_StartNodeGeometryData = m_StartNodeGeometryData,
					m_EndNodeGeometryData = m_EndNodeGeometryData,
					m_CompositionData = m_CompositionData,
					m_PrefabCompositionData = m_PrefabCompositionData,
					m_EditorMode = m_EditorMode
				};
				AreaIterator iterator3 = new AreaIterator
				{
					m_TopLevelEntity = entity2,
					m_TopLevelArea = entity3,
					m_ObjectEntity = entity,
					m_ObjectBounds = objectBounds,
					m_CollisionMask = collisionMask,
					m_ObjectTransform = objectTransform,
					m_PrefabGeometryData = prefabGeometryData,
					m_OwnerData = m_OwnerData,
					m_BuildingData = m_BuildingData,
					m_PrefabRefData = m_PrefabRefData,
					m_PrefabAreaGeometryData = m_PrefabAreaGeometryData,
					m_AreaNodes = m_AreaNodes,
					m_AreaTriangles = m_AreaTriangles
				};
				if (m_ConnectedEdges.HasBuffer(entity2))
				{
					iterator.m_TopLevelEdges = m_ConnectedEdges[entity2];
				}
				else if (m_ConnectedNodes.HasBuffer(entity2))
				{
					iterator.m_TopLevelNodes = m_ConnectedNodes[entity2];
					iterator.m_TopLevelEdge = m_EdgeData[entity2];
				}
				m_ObjectSearchTree.Iterate(ref iterator);
				if (!iterator.m_CollisionFound)
				{
					m_NetSearchTree.Iterate(ref iterator2);
					if (!iterator2.m_CollisionFound)
					{
						m_AreaSearchTree.Iterate(ref iterator3);
					}
				}
				collision = iterator.m_CollisionFound || iterator2.m_CollisionFound || iterator3.m_CollisionFound;
				overridableCollisions = iterator.m_OverridableCollisions;
				if (!collision)
				{
					if (overridableCollisions.IsCreated && overridableCollisions.Length != 0)
					{
						OverridableAction value = new OverridableAction
						{
							m_Entity = entity,
							m_Mask = GetBoundsMask(entity, prefabGeometryData)
						};
						if (m_CreatedData.HasComponent(entity))
						{
							value.m_Priority |= 1;
						}
						if (m_OverriddenData.HasComponent(entity))
						{
							value.m_Priority |= 2;
						}
						for (int i = 0; i < iterator.m_OverridableCollisions.Length; i++)
						{
							value.m_Other = iterator.m_OverridableCollisions[i];
							value.m_OtherOverridden = m_OverriddenData.HasComponent(value.m_Other);
							m_OverridableActions.Enqueue(value);
						}
						delayedResolve = true;
					}
					else if (delayedResolve)
					{
						OverridableAction value2 = new OverridableAction
						{
							m_Entity = entity,
							m_Mask = GetBoundsMask(entity, prefabGeometryData)
						};
						if (m_OverriddenData.HasComponent(entity))
						{
							value2.m_Priority |= 2;
						}
						m_OverridableActions.Enqueue(value2);
					}
				}
			}
			if (!collision && delayedResolve)
			{
				if (!m_SubObjects.TryGetBuffer(entity, out var bufferData))
				{
					return;
				}
				for (int j = 0; j < bufferData.Length; j++)
				{
					Entity subObject = bufferData[j].m_SubObject;
					PrefabRef prefabRef2 = m_PrefabRefData[subObject];
					if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef2.m_Prefab, out var componentData7) && (componentData7.m_Flags & (GeometryFlags.Overridable | GeometryFlags.DeleteOverridden)) == GeometryFlags.Overridable)
					{
						CheckObject(jobIndex, subObject, prefabRef, componentData7, collision: false, delayedResolve: true, ref overridableCollisions);
					}
				}
				return;
			}
			bool flag = collision != m_OverriddenData.HasComponent(entity);
			if (flag)
			{
				if (collision)
				{
					m_CommandBuffer.AddComponent(jobIndex, entity, default(Updated));
					m_CommandBuffer.AddComponent(jobIndex, entity, default(Overridden));
					AddTreeAction(entity, prefabGeometryData, isOverridden: true);
				}
				else
				{
					m_CommandBuffer.AddComponent(jobIndex, entity, default(Updated));
					m_CommandBuffer.RemoveComponent<Overridden>(jobIndex, entity);
					AddTreeAction(entity, prefabGeometryData, isOverridden: false);
				}
			}
			if (!m_SubObjects.TryGetBuffer(entity, out var bufferData2))
			{
				return;
			}
			for (int k = 0; k < bufferData2.Length; k++)
			{
				Entity subObject2 = bufferData2[k].m_SubObject;
				PrefabRef prefabRef3 = m_PrefabRefData[subObject2];
				if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef3.m_Prefab, out var componentData8) && (componentData8.m_Flags & (GeometryFlags.Overridable | GeometryFlags.DeleteOverridden)) == GeometryFlags.Overridable && (flag || (!collision && m_ObjectSet.Contains(subObject2))))
				{
					CheckObject(jobIndex, subObject2, prefabRef, componentData8, collision, delayedResolve, ref overridableCollisions);
				}
			}
		}

		private void AddTreeAction(Entity entity, ObjectGeometryData prefabObjectGeometryData, bool isOverridden)
		{
			TreeAction value = new TreeAction
			{
				m_Entity = entity
			};
			if (!isOverridden)
			{
				value.m_Mask = GetBoundsMask(entity, prefabObjectGeometryData);
			}
			m_TreeActions.Enqueue(value);
		}

		private BoundsMask GetBoundsMask(Entity entity, ObjectGeometryData prefabObjectGeometryData)
		{
			BoundsMask boundsMask = BoundsMask.NotOverridden;
			if (m_EditorMode || !m_MarkerData.HasComponent(entity) || m_OutsideConnectionData.HasComponent(entity))
			{
				MeshLayer layers = prefabObjectGeometryData.m_Layers;
				m_OwnerData.TryGetComponent(entity, out var componentData);
				boundsMask |= CommonUtils.GetBoundsMask(Game.Net.SearchSystem.GetLayers(componentData, default(Game.Net.UtilityLane), layers, ref m_PrefabRefData, ref m_PrefabNetData, ref m_PrefabNetGeometryData));
			}
			return boundsMask;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Created> __Game_Common_Created_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Overridden> __Game_Common_Overridden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Elevation> __Game_Objects_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attachment> __Game_Objects_Attachment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Stack> __Game_Objects_Stack_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Marker> __Game_Objects_Marker_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetData> __Game_Prefabs_NetData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StackData> __Game_Prefabs_StackData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedNode> __Game_Net_ConnectedNode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Common_Created_RO_ComponentLookup = state.GetComponentLookup<Created>(isReadOnly: true);
			__Game_Common_Overridden_RO_ComponentLookup = state.GetComponentLookup<Overridden>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Objects_Elevation_RO_ComponentLookup = state.GetComponentLookup<Elevation>(isReadOnly: true);
			__Game_Objects_Attachment_RO_ComponentLookup = state.GetComponentLookup<Attachment>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Objects_Stack_RO_ComponentLookup = state.GetComponentLookup<Stack>(isReadOnly: true);
			__Game_Objects_Marker_RO_ComponentLookup = state.GetComponentLookup<Marker>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<OutsideConnection>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetData_RO_ComponentLookup = state.GetComponentLookup<NetData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Prefabs_StackData_RO_ComponentLookup = state.GetComponentLookup<StackData>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<SubObject>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Net_ConnectedNode_RO_BufferLookup = state.GetBufferLookup<ConnectedNode>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
		}
	}

	private UpdateCollectSystem m_ObjectUpdateCollectSystem;

	private Game.Net.UpdateCollectSystem m_NetUpdateCollectSystem;

	private Game.Areas.UpdateCollectSystem m_AreaUpdateCollectSystem;

	private SearchSystem m_ObjectSearchSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private Game.Areas.SearchSystem m_AreaSearchSystem;

	private ModificationBarrier5 m_ModificationBarrier;

	private ToolSystem m_ToolSystem;

	private ComponentTypeSet m_OverriddenUpdatedSet;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ObjectUpdateCollectSystem = base.World.GetOrCreateSystemManaged<UpdateCollectSystem>();
		m_NetUpdateCollectSystem = base.World.GetOrCreateSystemManaged<Game.Net.UpdateCollectSystem>();
		m_AreaUpdateCollectSystem = base.World.GetOrCreateSystemManaged<Game.Areas.UpdateCollectSystem>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<Game.Areas.SearchSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_OverriddenUpdatedSet = new ComponentTypeSet(ComponentType.ReadWrite<Overridden>(), ComponentType.ReadWrite<Updated>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ObjectUpdateCollectSystem.isUpdated || m_NetUpdateCollectSystem.netsUpdated || m_AreaUpdateCollectSystem.lotsUpdated)
		{
			NativeList<Entity> nativeList = new NativeList<Entity>(Allocator.TempJob);
			NativeHashSet<Entity> objectSet = new NativeHashSet<Entity>(100, Allocator.TempJob);
			NativeQueue<TreeAction> actions = new NativeQueue<TreeAction>(Allocator.TempJob);
			NativeQueue<OverridableAction> overridableActions = new NativeQueue<OverridableAction>(Allocator.TempJob);
			base.Dependency = JobHandle.CombineDependencies(base.Dependency, CollectUpdatedObjects(nativeList, objectSet));
			JobHandle dependencies;
			JobHandle dependencies2;
			JobHandle dependencies3;
			CheckObjectOverrideJob jobData = new CheckObjectOverrideJob
			{
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CreatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Created_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OverriddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Overridden_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AttachmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attachment_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AttachedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
				m_StackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Stack_RO_ComponentLookup, ref base.CheckedStateRef),
				m_MarkerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Marker_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OutsideConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_StartNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EndNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabAreaGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabStackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StackData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
				m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
				m_ConnectedNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedNode_RO_BufferLookup, ref base.CheckedStateRef),
				m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
				m_AreaTriangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
				m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
				m_ObjectArray = nativeList.AsDeferredJobArray(),
				m_ObjectSet = objectSet,
				m_ObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies),
				m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies2),
				m_AreaSearchTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies3),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter(),
				m_TreeActions = actions.AsParallelWriter(),
				m_OverridableActions = overridableActions.AsParallelWriter()
			};
			JobHandle dependencies4;
			UpdateObjectOverrideJob jobData2 = new UpdateObjectOverrideJob
			{
				m_OverriddenUpdatedSet = m_OverriddenUpdatedSet,
				m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
				m_ObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: false, out dependencies4),
				m_Actions = actions,
				m_OverridableActions = overridableActions,
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
			};
			JobHandle jobHandle = jobData.Schedule(nativeList, 1, JobHandle.CombineDependencies(base.Dependency, JobHandle.CombineDependencies(dependencies, dependencies2, dependencies3)));
			JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, JobHandle.CombineDependencies(jobHandle, dependencies4));
			nativeList.Dispose(jobHandle);
			objectSet.Dispose(jobHandle);
			actions.Dispose(jobHandle2);
			overridableActions.Dispose(jobHandle2);
			m_ObjectSearchSystem.AddStaticSearchTreeWriter(jobHandle2);
			m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
			m_AreaSearchSystem.AddSearchTreeReader(jobHandle);
			m_ModificationBarrier.AddJobHandleForProducer(jobHandle2);
			base.Dependency = jobHandle2;
		}
	}

	private JobHandle CollectUpdatedObjects(NativeList<Entity> updateObjectsList, NativeHashSet<Entity> objectSet)
	{
		NativeQueue<Entity> queue = new NativeQueue<Entity>(Allocator.TempJob);
		NativeQueue<Entity> queue2 = new NativeQueue<Entity>(Allocator.TempJob);
		NativeQueue<Entity> queue3 = new NativeQueue<Entity>(Allocator.TempJob);
		JobHandle dependencies;
		NativeQuadTree<Entity, QuadTreeBoundsXZ> staticSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies);
		JobHandle jobHandle = default(JobHandle);
		if (m_ObjectUpdateCollectSystem.isUpdated)
		{
			JobHandle dependencies2;
			NativeList<Bounds2> updatedBounds = m_ObjectUpdateCollectSystem.GetUpdatedBounds(out dependencies2);
			JobHandle jobHandle2 = new FindUpdatedObjectsJob
			{
				m_Bounds = updatedBounds.AsDeferredJobArray(),
				m_SearchTree = staticSearchTree,
				m_ResultQueue = queue.AsParallelWriter()
			}.Schedule(updatedBounds, 1, JobHandle.CombineDependencies(dependencies2, dependencies));
			m_ObjectUpdateCollectSystem.AddBoundsReader(jobHandle2);
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
		}
		if (m_NetUpdateCollectSystem.netsUpdated)
		{
			JobHandle dependencies3;
			NativeList<Bounds2> updatedNetBounds = m_NetUpdateCollectSystem.GetUpdatedNetBounds(out dependencies3);
			JobHandle jobHandle3 = new FindUpdatedObjectsJob
			{
				m_Bounds = updatedNetBounds.AsDeferredJobArray(),
				m_SearchTree = staticSearchTree,
				m_ResultQueue = queue2.AsParallelWriter()
			}.Schedule(updatedNetBounds, 1, JobHandle.CombineDependencies(dependencies3, dependencies));
			m_NetUpdateCollectSystem.AddNetBoundsReader(jobHandle3);
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle3);
		}
		if (m_AreaUpdateCollectSystem.lotsUpdated)
		{
			JobHandle dependencies4;
			NativeList<Bounds2> updatedLotBounds = m_AreaUpdateCollectSystem.GetUpdatedLotBounds(out dependencies4);
			JobHandle jobHandle4 = new FindUpdatedObjectsJob
			{
				m_Bounds = updatedLotBounds.AsDeferredJobArray(),
				m_SearchTree = staticSearchTree,
				m_ResultQueue = queue3.AsParallelWriter()
			}.Schedule(updatedLotBounds, 1, JobHandle.CombineDependencies(dependencies4, dependencies));
			m_AreaUpdateCollectSystem.AddLotBoundsReader(jobHandle4);
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle4);
		}
		JobHandle jobHandle5 = IJobExtensions.Schedule(new CollectObjectsJob
		{
			m_Queue1 = queue,
			m_Queue2 = queue2,
			m_Queue3 = queue3,
			m_ResultList = updateObjectsList,
			m_ObjectSet = objectSet
		}, jobHandle);
		queue.Dispose(jobHandle5);
		queue2.Dispose(jobHandle5);
		queue3.Dispose(jobHandle5);
		m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
		return jobHandle5;
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
	public OverrideSystem()
	{
	}
}
