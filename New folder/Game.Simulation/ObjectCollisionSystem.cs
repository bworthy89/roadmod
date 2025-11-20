using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Events;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ObjectCollisionSystem : GameSystemBase
{
	private struct Collision : IComparable<Collision>
	{
		public quaternion m_Rotation1;

		public quaternion m_Rotation2;

		public float3 m_Position1;

		public float3 m_Position2;

		public float3 m_ImpactLocation;

		public Entity m_Entity1;

		public Entity m_Entity2;

		public float m_Time;

		public int m_CurrentIndex1;

		public int m_CurrentIndex2;

		public int CompareTo(Collision other)
		{
			return (int)math.sign(m_Time - other.m_Time);
		}
	}

	[BurstCompile]
	private struct FindCollisionsJob : IJobChunk
	{
		private struct NetIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds3 m_Bounds;

			public Entity m_Ignore;

			public NativeList<Entity> m_ObjectList;

			public ComponentLookup<Unspawned> m_UnspawnedData;

			public ComponentLookup<Transform> m_TransformData;

			public ComponentLookup<Controller> m_ControllerData;

			public ComponentLookup<Edge> m_EdgeData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<ObjectGeometryData> m_PrefabGeometryData;

			public BufferLookup<TransformFrame> m_TransformFrames;

			public BufferLookup<ConnectedEdge> m_ConnectedEdges;

			public BufferLookup<Game.Net.SubLane> m_SubLanes;

			public BufferLookup<LaneObject> m_LaneObjects;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(m_Bounds, bounds.m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (!MathUtils.Intersect(m_Bounds, bounds.m_Bounds))
				{
					return;
				}
				if (m_EdgeData.HasComponent(entity))
				{
					Edge edge = m_EdgeData[entity];
					CheckLanes(entity);
					CheckLanes(edge.m_Start);
					CheckLanes(edge.m_End);
				}
				else if (m_ConnectedEdges.HasBuffer(entity))
				{
					DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[entity];
					CheckLanes(entity);
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						CheckLanes(dynamicBuffer[i].m_Edge);
					}
				}
			}

			private void CheckLanes(Entity entity)
			{
				if (!m_SubLanes.HasBuffer(entity))
				{
					return;
				}
				DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[entity];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity subLane = dynamicBuffer[i].m_SubLane;
					if (m_LaneObjects.HasBuffer(subLane))
					{
						DynamicBuffer<LaneObject> dynamicBuffer2 = m_LaneObjects[subLane];
						for (int j = 0; j < dynamicBuffer2.Length; j++)
						{
							CheckObject(dynamicBuffer2[j].m_LaneObject);
						}
					}
				}
			}

			private void CheckObject(Entity entity)
			{
				if (entity == m_Ignore || (m_ControllerData.TryGetComponent(entity, out var componentData) && componentData.m_Controller == m_Ignore) || m_UnspawnedData.HasComponent(entity))
				{
					return;
				}
				if (m_TransformFrames.HasBuffer(entity))
				{
					PrefabRef prefabRef = m_PrefabRefData[entity];
					Transform transform = m_TransformData[entity];
					DynamicBuffer<TransformFrame> transformFrames = m_TransformFrames[entity];
					ObjectGeometryData geometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
					int num = CurrentFrameIndex(transform, transformFrames);
					int index = math.select(num - 1, 3, num == 0);
					TransformFrame transformFrame = transformFrames[num];
					TransformFrame transformFrame2 = transformFrames[index];
					Bounds3 bounds = ObjectUtils.CalculateBounds(transformFrame.m_Position, transformFrame.m_Rotation, geometryData);
					Bounds3 bounds2 = ObjectUtils.CalculateBounds(transformFrame2.m_Position, transformFrame2.m_Rotation, geometryData);
					if (MathUtils.Intersect(m_Bounds, bounds | bounds2))
					{
						CollectionUtils.TryAddUniqueValue(m_ObjectList, entity);
					}
				}
				else
				{
					PrefabRef prefabRef2 = m_PrefabRefData[entity];
					Transform transform2 = m_TransformData[entity];
					ObjectGeometryData geometryData2 = m_PrefabGeometryData[prefabRef2.m_Prefab];
					Bounds3 bounds3 = ObjectUtils.CalculateBounds(transform2.m_Position, transform2.m_Rotation, geometryData2);
					if (MathUtils.Intersect(m_Bounds, bounds3))
					{
						CollectionUtils.TryAddUniqueValue(m_ObjectList, entity);
					}
				}
			}
		}

		[ReadOnly]
		public uint m_PreviousTransformFrameIndex;

		[ReadOnly]
		public uint m_CurrentTransformFrameIndex;

		[ReadOnly]
		public uint m_UpdateFrameIndex;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		public NativeQueue<Collision>.ParallelWriter m_CollisionQueue;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<TransformFrame> m_TransformFrameType;

		[ReadOnly]
		public ComponentLookup<Unspawned> m_UnspawnedData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Controller> m_ControllerData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public BufferLookup<TransformFrame> m_TransformFrames;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjects;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeList<Entity> objectList = new NativeList<Entity>(16, Allocator.Temp);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<TransformFrame> bufferAccessor = chunk.GetBufferAccessor(ref m_TransformFrameType);
			NetIterator iterator = new NetIterator
			{
				m_ObjectList = objectList,
				m_UnspawnedData = m_UnspawnedData,
				m_TransformData = m_TransformData,
				m_ControllerData = m_ControllerData,
				m_EdgeData = m_EdgeData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabGeometryData = m_PrefabGeometryData,
				m_TransformFrames = m_TransformFrames,
				m_ConnectedEdges = m_ConnectedEdges,
				m_SubLanes = m_SubLanes,
				m_LaneObjects = m_LaneObjects
			};
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				DynamicBuffer<TransformFrame> dynamicBuffer = bufferAccessor[i];
				ObjectGeometryData objectGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
				TransformFrame previousFrame = dynamicBuffer[(int)m_PreviousTransformFrameIndex];
				TransformFrame currentFrame = dynamicBuffer[(int)m_CurrentTransformFrameIndex];
				float3 position = previousFrame.m_Position;
				float3 @float = math.mul(math.inverse(currentFrame.m_Rotation), currentFrame.m_Position - position);
				Bounds3 bounds = objectGeometryData.m_Bounds;
				Box3 box = new Box3(bounds, previousFrame.m_Rotation);
				Box3 box2 = new Box3(bounds + @float, currentFrame.m_Rotation);
				Bounds3 bounds2 = position + (MathUtils.Bounds(box) | MathUtils.Bounds(box2));
				iterator.m_Bounds = bounds2;
				iterator.m_Ignore = entity;
				m_NetSearchTree.Iterate(ref iterator);
				for (int j = 0; j < objectList.Length; j++)
				{
					TestCollision(position, previousFrame, currentFrame, box, box2, entity, objectList[j]);
				}
				objectList.Clear();
			}
		}

		private void TestCollision(float3 origin, TransformFrame previousFrame1, TransformFrame currentFrame1, Box3 previousBox1, Box3 currentBox1, Entity entity1, Entity entity2)
		{
			PrefabRef prefabRef = m_PrefabRefData[entity2];
			Transform transform = m_TransformData[entity2];
			ObjectGeometryData objectGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
			if (m_TransformFrames.HasBuffer(entity2))
			{
				DynamicBuffer<TransformFrame> transformFrames = m_TransformFrames[entity2];
				int num = CurrentFrameIndex(transform, transformFrames);
				int index = math.select(num - 1, 3, num == 0);
				TransformFrame transformFrame = transformFrames[index];
				TransformFrame transformFrame2 = transformFrames[num];
				quaternion q = math.inverse(transformFrame.m_Rotation);
				quaternion q2 = math.inverse(transformFrame2.m_Rotation);
				float3 @float = math.mul(q, transformFrame.m_Position - origin);
				float3 float2 = math.mul(q2, transformFrame2.m_Position - origin);
				Box3 box = new Box3(objectGeometryData.m_Bounds + @float, transformFrame.m_Rotation);
				Box3 box2 = new Box3(objectGeometryData.m_Bounds + float2, transformFrame2.m_Rotation);
				for (int i = 1; i <= 16; i++)
				{
					float num2 = (float)i * 0.0625f;
					Box3 box3 = MathUtils.Lerp(previousBox1, currentBox1, num2);
					Box3 box4 = MathUtils.Lerp(box, box2, num2);
					Bounds3 bounds = MathUtils.Bounds(box3);
					Bounds3 bounds2 = MathUtils.Bounds(box4);
					if (MathUtils.Intersect(bounds, bounds2) && MathUtils.Intersect(box3, box4, out var intersection, out var intersection2))
					{
						float3 start = math.rotate(box3.rotation, MathUtils.Center(intersection)) + origin;
						float3 end = math.rotate(box4.rotation, MathUtils.Center(intersection2)) + origin;
						m_CollisionQueue.Enqueue(new Collision
						{
							m_Rotation1 = box3.rotation,
							m_Rotation2 = box4.rotation,
							m_Position1 = math.lerp(previousFrame1.m_Position, currentFrame1.m_Position, num2),
							m_Position2 = math.lerp(transformFrame.m_Position, transformFrame2.m_Position, num2),
							m_ImpactLocation = math.lerp(start, end, 0.5f),
							m_Entity1 = entity1,
							m_Entity2 = entity2,
							m_Time = num2,
							m_CurrentIndex1 = (int)m_CurrentTransformFrameIndex,
							m_CurrentIndex2 = num
						});
						break;
					}
				}
				return;
			}
			float3 float3 = math.mul(math.inverse(transform.m_Rotation), transform.m_Position - origin);
			Box3 box5 = new Box3(objectGeometryData.m_Bounds + float3, transform.m_Rotation);
			Bounds3 bounds3 = MathUtils.Bounds(box5);
			for (int j = 1; j <= 16; j++)
			{
				float num3 = (float)j * 0.0625f;
				Box3 box6 = MathUtils.Lerp(previousBox1, currentBox1, num3);
				if (MathUtils.Intersect(MathUtils.Bounds(box6), bounds3) && MathUtils.Intersect(box6, box5, out var intersection3, out var intersection4))
				{
					float3 start2 = math.rotate(box6.rotation, MathUtils.Center(intersection3)) + origin;
					float3 end2 = math.rotate(box5.rotation, MathUtils.Center(intersection4)) + origin;
					m_CollisionQueue.Enqueue(new Collision
					{
						m_Rotation1 = box6.rotation,
						m_Rotation2 = box5.rotation,
						m_Position1 = math.lerp(previousFrame1.m_Position, currentFrame1.m_Position, num3),
						m_Position2 = transform.m_Position,
						m_ImpactLocation = math.lerp(start2, end2, 0.5f),
						m_Entity1 = entity1,
						m_Entity2 = entity2,
						m_Time = num3,
						m_CurrentIndex1 = (int)m_CurrentTransformFrameIndex,
						m_CurrentIndex2 = -1
					});
					break;
				}
			}
		}

		private static int CurrentFrameIndex(Transform transform, DynamicBuffer<TransformFrame> transformFrames)
		{
			float num = float.MaxValue;
			int result = -1;
			for (int i = 0; i < transformFrames.Length; i++)
			{
				float num2 = math.distancesq(transformFrames[i].m_Position, transform.m_Position);
				if (num2 < num)
				{
					num = num2;
					result = i;
				}
			}
			return result;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct ResolveCollisionsJob : IJob
	{
		[ReadOnly]
		public ComponentLookup<InvolvedInAccident> m_InvolvedInAccidentData;

		[ReadOnly]
		public ComponentLookup<Vehicle> m_VehicleData;

		[ReadOnly]
		public ComponentLookup<Bicycle> m_BicycleData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabGeometryData;

		public ComponentLookup<Transform> m_TransformData;

		public ComponentLookup<Moving> m_MovingData;

		public ComponentLookup<Damaged> m_DamagedData;

		public BufferLookup<TransformFrame> m_TransformFrames;

		[ReadOnly]
		public EntityArchetype m_EventImpactArchetype;

		[ReadOnly]
		public EntityArchetype m_DamageEventArchetype;

		[ReadOnly]
		public EntityArchetype m_DestroyEventArchetype;

		[ReadOnly]
		public EventHelpers.StructuralIntegrityData m_StructuralIntegrityData;

		public NativeQueue<Collision> m_CollisionQueue;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			if (m_CollisionQueue.Count == 0)
			{
				return;
			}
			NativeArray<Collision> array = CollectionUtils.ToArray(m_CollisionQueue, Allocator.Temp);
			NativeParallelHashSet<Entity> nativeParallelHashSet = new NativeParallelHashSet<Entity>(array.Length * 2, Allocator.Temp);
			array.Sort();
			float2 float3 = default(float2);
			for (int i = 0; i < array.Length; i++)
			{
				Collision collision = array[i];
				if (nativeParallelHashSet.Contains(collision.m_Entity1) || nativeParallelHashSet.Contains(collision.m_Entity2))
				{
					continue;
				}
				Moving componentData;
				bool flag = m_MovingData.TryGetComponent(collision.m_Entity1, out componentData);
				Moving componentData2;
				bool flag2 = m_MovingData.TryGetComponent(collision.m_Entity2, out componentData2);
				PrefabRef prefabRef = m_PrefabRefData[collision.m_Entity1];
				PrefabRef prefabRef2 = m_PrefabRefData[collision.m_Entity2];
				ObjectGeometryData objectGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
				ObjectGeometryData objectGeometryData2 = m_PrefabGeometryData[prefabRef2.m_Prefab];
				m_InvolvedInAccidentData.TryGetComponent(collision.m_Entity1, out var componentData3);
				m_InvolvedInAccidentData.TryGetComponent(collision.m_Entity2, out var componentData4);
				if ((!flag || !flag2) && componentData3.m_Event != Entity.Null && componentData3.m_Event == componentData4.m_Event)
				{
					continue;
				}
				float3 momentOfInertia = ObjectUtils.CalculateMomentOfInertia(collision.m_Rotation1, objectGeometryData.m_Size);
				float3 momentOfInertia2 = ObjectUtils.CalculateMomentOfInertia(collision.m_Rotation2, objectGeometryData2.m_Size);
				float3 @float = ObjectUtils.CalculatePointVelocity(collision.m_ImpactLocation - collision.m_Position1, componentData);
				float3 float2 = ObjectUtils.CalculatePointVelocity(collision.m_ImpactLocation - collision.m_Position2, componentData2);
				if (m_VehicleData.HasComponent(collision.m_Entity2))
				{
					float3.x = objectGeometryData.m_Size.x * objectGeometryData.m_Size.y * objectGeometryData.m_Size.z;
					float3.y = objectGeometryData2.m_Size.x * objectGeometryData2.m_Size.y * objectGeometryData2.m_Size.z;
					float3 = float3 / math.csum(float3) * 1.2f;
					float3 float4 = (float2 - @float) * float3.y;
					float3 float5 = (@float - float2) * float3.x;
					if (!(math.dot(float4, collision.m_Position1 - collision.m_Position2) < 0f))
					{
						float4 += math.normalizesafe(collision.m_Position1 - collision.m_Position2) * (math.length(@float) * 0.1f);
						float5 += math.normalizesafe(collision.m_Position2 - collision.m_Position1) * (math.length(float2) * 0.1f);
						float3 float6 = CalculatePointAngularVelocityDelta(collision.m_ImpactLocation, collision.m_Position1, float4, momentOfInertia);
						float3 float7 = CalculatePointAngularVelocityDelta(collision.m_ImpactLocation, collision.m_Position2, float5, momentOfInertia2);
						Impact component = new Impact
						{
							m_Event = ((componentData4.m_Event != Entity.Null) ? componentData4.m_Event : componentData3.m_Event),
							m_Target = collision.m_Entity1,
							m_Severity = 10f,
							m_CheckStoppedEvent = true
						};
						Impact component2 = new Impact
						{
							m_Event = ((componentData3.m_Event != Entity.Null) ? componentData3.m_Event : componentData4.m_Event),
							m_Target = collision.m_Entity2,
							m_Severity = 10f,
							m_CheckStoppedEvent = true
						};
						if (flag)
						{
							componentData.m_Velocity += float4;
							componentData.m_AngularVelocity += float6;
							m_MovingData[collision.m_Entity1] = componentData;
							m_TransformData[collision.m_Entity1] = new Transform(collision.m_Position1, collision.m_Rotation1);
							DynamicBuffer<TransformFrame> dynamicBuffer = m_TransformFrames[collision.m_Entity1];
							dynamicBuffer[collision.m_CurrentIndex1] = new TransformFrame(collision.m_Position1, collision.m_Rotation1, componentData.m_Velocity);
						}
						else
						{
							component.m_VelocityDelta = float4;
							component.m_AngularVelocityDelta = float6;
						}
						if (flag2)
						{
							componentData2.m_Velocity += float5;
							componentData2.m_AngularVelocity += float7;
							m_MovingData[collision.m_Entity2] = componentData2;
							m_TransformData[collision.m_Entity2] = new Transform(collision.m_Position2, collision.m_Rotation2);
							DynamicBuffer<TransformFrame> dynamicBuffer2 = m_TransformFrames[collision.m_Entity2];
							dynamicBuffer2[collision.m_CurrentIndex2] = new TransformFrame(collision.m_Position2, collision.m_Rotation2, componentData2.m_Velocity);
						}
						else
						{
							component2.m_VelocityDelta = float5;
							component2.m_AngularVelocityDelta = float7;
						}
						Entity e = m_CommandBuffer.CreateEntity(m_EventImpactArchetype);
						Entity e2 = m_CommandBuffer.CreateEntity(m_EventImpactArchetype);
						m_CommandBuffer.SetComponent(e, component);
						m_CommandBuffer.SetComponent(e2, component2);
						nativeParallelHashSet.Add(collision.m_Entity1);
						nativeParallelHashSet.Add(collision.m_Entity2);
						AddDamage(collision.m_Entity1, component.m_Event, prefabRef, float4);
						AddDamage(collision.m_Entity2, component2.m_Event, prefabRef2, float5);
					}
					continue;
				}
				float3 float8 = (@float - float2) * 0.5f;
				if (!(math.dot(float8, collision.m_Position2 - collision.m_Position1) < 0f))
				{
					float8 += math.normalizesafe(collision.m_Position2 - collision.m_Position1) * (math.length(float2) * 0.1f);
					float3 float9 = CalculatePointAngularVelocityDelta(collision.m_ImpactLocation, collision.m_Position2, float8, momentOfInertia2);
					Impact component3 = new Impact
					{
						m_Event = ((componentData3.m_Event != Entity.Null) ? componentData3.m_Event : componentData4.m_Event),
						m_Target = collision.m_Entity2,
						m_Severity = 10f
					};
					if (m_MovingData.HasComponent(collision.m_Entity2))
					{
						componentData2.m_Velocity += float8;
						componentData2.m_AngularVelocity += float9;
						m_MovingData[collision.m_Entity2] = componentData2;
						m_TransformData[collision.m_Entity2] = new Transform(collision.m_Position2, collision.m_Rotation2);
						DynamicBuffer<TransformFrame> dynamicBuffer3 = m_TransformFrames[collision.m_Entity2];
						dynamicBuffer3[collision.m_CurrentIndex2] = new TransformFrame(collision.m_Position2, collision.m_Rotation2, componentData2.m_Velocity);
					}
					else
					{
						component3.m_VelocityDelta = float8;
						component3.m_AngularVelocityDelta = float9;
					}
					Entity e3 = m_CommandBuffer.CreateEntity(m_EventImpactArchetype);
					m_CommandBuffer.SetComponent(e3, component3);
					nativeParallelHashSet.Add(collision.m_Entity2);
				}
			}
		}

		private float3 CalculatePointAngularVelocityDelta(float3 position, float3 origin, float3 velocityDelta, float3 momentOfInertia)
		{
			return math.cross(position - origin, velocityDelta) / momentOfInertia;
		}

		private void AddDamage(Entity entity, Entity _event, PrefabRef prefabRef, float3 velocityDelta)
		{
			float structuralIntegrity = m_StructuralIntegrityData.GetStructuralIntegrity(prefabRef.m_Prefab, isBuilding: false);
			float num = math.min(1f, math.length(velocityDelta) * 100f / structuralIntegrity);
			if (!(num < 0.01f))
			{
				if (m_BicycleData.HasComponent(entity))
				{
					num *= 2f;
				}
				else if (m_DamagedData.HasComponent(entity))
				{
					Damaged damaged = m_DamagedData[entity];
					damaged.m_Damage.x = math.min(1f, damaged.m_Damage.x + num);
					m_DamagedData[entity] = damaged;
					num = ObjectUtils.GetTotalDamage(damaged);
				}
				else
				{
					Entity e = m_CommandBuffer.CreateEntity(m_DamageEventArchetype);
					m_CommandBuffer.SetComponent(e, new Damage(entity, new float3(num, 0f, 0f)));
				}
				if (num >= 1f)
				{
					Entity e2 = m_CommandBuffer.CreateEntity(m_DestroyEventArchetype);
					m_CommandBuffer.SetComponent(e2, new Destroy(entity, _event));
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<TransformFrame> __Game_Objects_TransformFrame_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Unspawned> __Game_Objects_Unspawned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Controller> __Game_Vehicles_Controller_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<TransformFrame> __Game_Objects_TransformFrame_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneObject> __Game_Net_LaneObject_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<InvolvedInAccident> __Game_Events_InvolvedInAccident_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Vehicle> __Game_Vehicles_Vehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Bicycle> __Game_Vehicles_Bicycle_RO_ComponentLookup;

		public ComponentLookup<Transform> __Game_Objects_Transform_RW_ComponentLookup;

		public ComponentLookup<Moving> __Game_Objects_Moving_RW_ComponentLookup;

		public ComponentLookup<Damaged> __Game_Objects_Damaged_RW_ComponentLookup;

		public BufferLookup<TransformFrame> __Game_Objects_TransformFrame_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Objects_TransformFrame_RO_BufferTypeHandle = state.GetBufferTypeHandle<TransformFrame>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentLookup = state.GetComponentLookup<Unspawned>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Objects_TransformFrame_RO_BufferLookup = state.GetBufferLookup<TransformFrame>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
			__Game_Events_InvolvedInAccident_RO_ComponentLookup = state.GetComponentLookup<InvolvedInAccident>(isReadOnly: true);
			__Game_Vehicles_Vehicle_RO_ComponentLookup = state.GetComponentLookup<Vehicle>(isReadOnly: true);
			__Game_Vehicles_Bicycle_RO_ComponentLookup = state.GetComponentLookup<Bicycle>(isReadOnly: true);
			__Game_Objects_Transform_RW_ComponentLookup = state.GetComponentLookup<Transform>();
			__Game_Objects_Moving_RW_ComponentLookup = state.GetComponentLookup<Moving>();
			__Game_Objects_Damaged_RW_ComponentLookup = state.GetComponentLookup<Damaged>();
			__Game_Objects_TransformFrame_RW_BufferLookup = state.GetBufferLookup<TransformFrame>();
		}
	}

	private SimulationSystem m_SimulationSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_ObjectQuery;

	private EntityQuery m_ConfigQuery;

	private EntityArchetype m_EventImpactArchetype;

	private EntityArchetype m_DamageEventArchetype;

	private EntityArchetype m_DestroyEventArchetype;

	private EventHelpers.StructuralIntegrityData m_StructuralIntegrityData;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_ObjectQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[4]
			{
				ComponentType.ReadOnly<UpdateFrame>(),
				ComponentType.ReadWrite<Transform>(),
				ComponentType.ReadWrite<Moving>(),
				ComponentType.ReadWrite<TransformFrame>()
			},
			Any = new ComponentType[1] { ComponentType.ReadOnly<OutOfControl>() },
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Unspawned>()
			}
		});
		m_ConfigQuery = GetEntityQuery(ComponentType.ReadOnly<FireConfigurationData>());
		m_EventImpactArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Impact>());
		m_DamageEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Damage>());
		m_DestroyEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Destroy>());
		m_StructuralIntegrityData = new EventHelpers.StructuralIntegrityData(this);
		RequireForUpdate(m_ObjectQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrameIndex = m_SimulationSystem.frameIndex % 16;
		uint num = m_SimulationSystem.frameIndex / 16 % 4;
		NativeQueue<Collision> collisionQueue = new NativeQueue<Collision>(Allocator.TempJob);
		FireConfigurationData singleton = m_ConfigQuery.GetSingleton<FireConfigurationData>();
		m_StructuralIntegrityData.Update(this, singleton);
		JobHandle dependencies;
		FindCollisionsJob jobData = new FindCollisionsJob
		{
			m_PreviousTransformFrameIndex = (num - 1) % 4,
			m_CurrentTransformFrameIndex = num,
			m_UpdateFrameIndex = updateFrameIndex,
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
			m_CollisionQueue = collisionQueue.AsParallelWriter(),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformFrameType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_TransformFrame_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_UnspawnedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformFrames = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_TransformFrame_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef)
		};
		ResolveCollisionsJob jobData2 = new ResolveCollisionsJob
		{
			m_InvolvedInAccidentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_InvolvedInAccident_RO_ComponentLookup, ref base.CheckedStateRef),
			m_VehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Vehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BicycleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Bicycle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RW_ComponentLookup, ref base.CheckedStateRef),
			m_MovingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Moving_RW_ComponentLookup, ref base.CheckedStateRef),
			m_DamagedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Damaged_RW_ComponentLookup, ref base.CheckedStateRef),
			m_TransformFrames = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_TransformFrame_RW_BufferLookup, ref base.CheckedStateRef),
			m_EventImpactArchetype = m_EventImpactArchetype,
			m_DamageEventArchetype = m_DamageEventArchetype,
			m_DestroyEventArchetype = m_DestroyEventArchetype,
			m_StructuralIntegrityData = m_StructuralIntegrityData,
			m_CollisionQueue = collisionQueue,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer()
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_ObjectQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
		collisionQueue.Dispose(jobHandle2);
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
		base.Dependency = jobHandle2;
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
	public ObjectCollisionSystem()
	{
	}
}
