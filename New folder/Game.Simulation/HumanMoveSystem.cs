using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
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
public class HumanMoveSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateTransformDataJob : IJobChunk
	{
		private struct NetIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds3 m_Bounds;

			public Quad3 m_CornerPositions;

			public float4 m_GroundHeights;

			public ComponentLookup<Composition> m_CompositionData;

			public ComponentLookup<Orphan> m_OrphanData;

			public ComponentLookup<Node> m_NodeData;

			public ComponentLookup<NodeGeometry> m_NodeGeometryData;

			public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

			public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

			public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

			public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
			{
				if (MathUtils.Intersect(bounds.m_Bounds, m_Bounds))
				{
					if (m_CompositionData.HasComponent(item))
					{
						CheckEdge(item);
					}
					else if (m_OrphanData.HasComponent(item))
					{
						CheckNode(item);
					}
				}
			}

			private void CheckNode(Entity entity)
			{
				if (MathUtils.Intersect(m_NodeGeometryData[entity].m_Bounds, m_Bounds))
				{
					Node node = m_NodeData[entity];
					Orphan orphan = m_OrphanData[entity];
					NetCompositionData netCompositionData = m_PrefabCompositionData[orphan.m_Composition];
					float3 position = node.m_Position;
					position.y += netCompositionData.m_SurfaceHeight.max;
					CheckCircle(position, netCompositionData.m_Width * 0.5f);
				}
			}

			private void CheckEdge(Entity entity)
			{
				EdgeGeometry edgeGeometry = m_EdgeGeometryData[entity];
				EdgeNodeGeometry geometry = m_StartNodeGeometryData[entity].m_Geometry;
				EdgeNodeGeometry geometry2 = m_EndNodeGeometryData[entity].m_Geometry;
				bool3 x = default(bool3);
				x.x = MathUtils.Intersect(edgeGeometry.m_Bounds, m_Bounds);
				x.y = MathUtils.Intersect(geometry.m_Bounds, m_Bounds);
				x.z = MathUtils.Intersect(geometry2.m_Bounds, m_Bounds);
				if (!math.any(x))
				{
					return;
				}
				Composition composition = m_CompositionData[entity];
				if (x.x)
				{
					NetCompositionData prefabCompositionData = m_PrefabCompositionData[composition.m_Edge];
					CheckSegment(edgeGeometry.m_Start, prefabCompositionData);
					CheckSegment(edgeGeometry.m_End, prefabCompositionData);
				}
				if (x.y)
				{
					NetCompositionData prefabCompositionData2 = m_PrefabCompositionData[composition.m_StartNode];
					if (geometry.m_MiddleRadius > 0f)
					{
						CheckSegment(geometry.m_Left, prefabCompositionData2);
						Segment right = geometry.m_Right;
						Segment right2 = geometry.m_Right;
						right.m_Right = MathUtils.Lerp(geometry.m_Right.m_Left, geometry.m_Right.m_Right, 0.5f);
						right2.m_Left = MathUtils.Lerp(geometry.m_Right.m_Left, geometry.m_Right.m_Right, 0.5f);
						right.m_Right.d = geometry.m_Middle.d;
						right2.m_Left.d = geometry.m_Middle.d;
						CheckSegment(right, prefabCompositionData2);
						CheckSegment(right2, prefabCompositionData2);
					}
					else
					{
						Segment left = geometry.m_Left;
						Segment right3 = geometry.m_Right;
						CheckSegment(left, prefabCompositionData2);
						CheckSegment(right3, prefabCompositionData2);
						left.m_Right = geometry.m_Middle;
						right3.m_Left = geometry.m_Middle;
						CheckSegment(left, prefabCompositionData2);
						CheckSegment(right3, prefabCompositionData2);
					}
				}
				if (x.z)
				{
					NetCompositionData prefabCompositionData3 = m_PrefabCompositionData[composition.m_EndNode];
					if (geometry2.m_MiddleRadius > 0f)
					{
						CheckSegment(geometry2.m_Left, prefabCompositionData3);
						Segment right4 = geometry2.m_Right;
						Segment right5 = geometry2.m_Right;
						right4.m_Right = MathUtils.Lerp(geometry2.m_Right.m_Left, geometry2.m_Right.m_Right, 0.5f);
						right4.m_Right.d = geometry2.m_Middle.d;
						right5.m_Left = right4.m_Right;
						CheckSegment(right4, prefabCompositionData3);
						CheckSegment(right5, prefabCompositionData3);
					}
					else
					{
						Segment left2 = geometry2.m_Left;
						Segment right6 = geometry2.m_Right;
						CheckSegment(left2, prefabCompositionData3);
						CheckSegment(right6, prefabCompositionData3);
						left2.m_Right = geometry2.m_Middle;
						right6.m_Left = geometry2.m_Middle;
						CheckSegment(left2, prefabCompositionData3);
						CheckSegment(right6, prefabCompositionData3);
					}
				}
			}

			private void CheckSegment(Segment segment, NetCompositionData prefabCompositionData)
			{
				float3 a = segment.m_Left.a;
				float3 @float = segment.m_Right.a;
				a.y += prefabCompositionData.m_SurfaceHeight.max;
				@float.y += prefabCompositionData.m_SurfaceHeight.max;
				Bounds3 bounds = MathUtils.Bounds(a, @float);
				for (int i = 1; i <= 8; i++)
				{
					float t = (float)i / 8f;
					float3 float2 = MathUtils.Position(segment.m_Left, t);
					float3 float3 = MathUtils.Position(segment.m_Right, t);
					float2.y += prefabCompositionData.m_SurfaceHeight.max;
					float3.y += prefabCompositionData.m_SurfaceHeight.max;
					Bounds3 bounds2 = MathUtils.Bounds(float2, float3);
					if (MathUtils.Intersect(bounds | bounds2, m_Bounds))
					{
						CheckTriangle(new Triangle3(a, @float, float2));
						CheckTriangle(new Triangle3(float3, float2, @float));
					}
					a = float2;
					@float = float3;
					bounds = bounds2;
				}
			}

			private void CheckCircle(float3 center, float radius)
			{
				CheckCircle(center, radius, m_CornerPositions.a, ref m_GroundHeights.x);
				CheckCircle(center, radius, m_CornerPositions.b, ref m_GroundHeights.y);
				CheckCircle(center, radius, m_CornerPositions.c, ref m_GroundHeights.z);
				CheckCircle(center, radius, m_CornerPositions.d, ref m_GroundHeights.w);
			}

			private void CheckTriangle(Triangle3 triangle)
			{
				CheckTriangle(triangle, m_CornerPositions.a, ref m_GroundHeights.x);
				CheckTriangle(triangle, m_CornerPositions.b, ref m_GroundHeights.y);
				CheckTriangle(triangle, m_CornerPositions.c, ref m_GroundHeights.z);
				CheckTriangle(triangle, m_CornerPositions.d, ref m_GroundHeights.w);
			}

			private void CheckCircle(float3 center, float radius, float3 position, ref float groundHeight)
			{
				if (math.distance(center.xz, position.xz) <= radius)
				{
					float y = center.y;
					groundHeight = math.select(groundHeight, y, (y < position.y + 4f) & (y > groundHeight));
				}
			}

			private void CheckTriangle(Triangle3 triangle, float3 position, ref float groundHeight)
			{
				if (MathUtils.Intersect(triangle.xz, position.xz, out var t))
				{
					float y = MathUtils.Position(triangle, t).y;
					groundHeight = math.select(groundHeight, y, (y < position.y + 4f) & (y > groundHeight));
				}
			}
		}

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Human> m_HumanType;

		[ReadOnly]
		public ComponentTypeHandle<Stumbling> m_StumblingType;

		[ReadOnly]
		public ComponentTypeHandle<Stopped> m_StoppedType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentVehicle> m_CurrentVehicleType;

		[ReadOnly]
		public ComponentTypeHandle<PseudoRandomSeed> m_PseudoRandomSeedType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<MeshGroup> m_MeshGroupType;

		public ComponentTypeHandle<HumanNavigation> m_NavigationType;

		public ComponentTypeHandle<Moving> m_MovingType;

		public ComponentTypeHandle<Transform> m_TransformType;

		public BufferTypeHandle<TransformFrame> m_TransformFrameType;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<Orphan> m_OrphanData;

		[ReadOnly]
		public ComponentLookup<Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<NodeGeometry> m_NodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> m_SubMeshGroups;

		[ReadOnly]
		public BufferLookup<CharacterElement> m_CharacterElements;

		[ReadOnly]
		public BufferLookup<AnimationClip> m_AnimationClips;

		[ReadOnly]
		public BufferLookup<AnimationMotion> m_AnimationMotions;

		[ReadOnly]
		public BufferLookup<SubMesh> m_SubMeshes;

		[ReadOnly]
		public BufferLookup<ActivityLocationElement> m_ActivityLocations;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public uint m_TransformFrameIndex;

		[ReadOnly]
		public EntityArchetype m_SubObjectEventArchetype;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has(ref m_StoppedType))
			{
				StoppedMove(chunk);
			}
			else if (chunk.Has(ref m_StumblingType))
			{
				StumblingMove(chunk);
			}
			else
			{
				NormalMove(chunk, unfilteredChunkIndex);
			}
		}

		private void NormalMove(ArchetypeChunk chunk, int jobIndex)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Human> nativeArray2 = chunk.GetNativeArray(ref m_HumanType);
			NativeArray<CurrentVehicle> nativeArray3 = chunk.GetNativeArray(ref m_CurrentVehicleType);
			NativeArray<PseudoRandomSeed> nativeArray4 = chunk.GetNativeArray(ref m_PseudoRandomSeedType);
			NativeArray<HumanNavigation> nativeArray5 = chunk.GetNativeArray(ref m_NavigationType);
			NativeArray<Moving> nativeArray6 = chunk.GetNativeArray(ref m_MovingType);
			NativeArray<Transform> nativeArray7 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<PrefabRef> nativeArray8 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<TransformFrame> bufferAccessor = chunk.GetBufferAccessor(ref m_TransformFrameType);
			BufferAccessor<MeshGroup> bufferAccessor2 = chunk.GetBufferAccessor(ref m_MeshGroupType);
			float num = 4f / 15f;
			int num2 = (int)m_TransformFrameIndex;
			int index = math.select((int)(m_TransformFrameIndex - 1), 3, m_TransformFrameIndex == 0);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				Human human = nativeArray2[i];
				HumanNavigation value = nativeArray5[i];
				Moving value2 = nativeArray6[i];
				Transform transform = nativeArray7[i];
				PrefabRef prefabRef = nativeArray8[i];
				DynamicBuffer<TransformFrame> dynamicBuffer = bufferAccessor[i];
				TransformFrame transformFrame = dynamicBuffer[index];
				float3 value3 = value.m_TargetPosition - transform.m_Position;
				MathUtils.TryNormalize(ref value3, value.m_MaxSpeed);
				TransformFrame transformFrame2 = default(TransformFrame);
				if (value.m_MaxSpeed >= 0.1f && value.m_TargetActivity == 0)
				{
					transformFrame2.m_State = TransformState.Move;
				}
				else
				{
					transformFrame2.m_State = TransformState.Idle;
					transformFrame2.m_Activity = value.m_TargetActivity;
				}
				float3 targetDirection = new float3(value.m_TargetDirection.x, 0f, value.m_TargetDirection.y);
				if (!targetDirection.Equals(default(float3)))
				{
					transform.m_Rotation = quaternion.LookRotation(targetDirection, math.up());
				}
				CollectionUtils.TryGet(nativeArray3, i, out var value4);
				CollectionUtils.TryGet(nativeArray4, i, out var value5);
				CollectionUtils.TryGet(bufferAccessor2, i, out var value6);
				AnimatedPropID propID = GetPropID(in transformFrame, value4);
				AnimatedPropID propID2 = GetPropID(in transformFrame2, value4);
				ActivityCondition conditions = CreatureUtils.GetConditions(human);
				ObjectUtils.UpdateAnimation(prefabRef.m_Prefab, num, value5, value6, ref m_SubMeshGroups, ref m_CharacterElements, ref m_SubMeshes, ref m_AnimationClips, ref m_AnimationMotions, propID, propID2, conditions, ref value.m_MaxSpeed, ref value.m_TargetActivity, ref value.m_TargetPosition, ref targetDirection, ref transform, ref transformFrame, ref transformFrame2);
				value.m_TransformState = transformFrame2.m_State;
				value.m_LastActivity = transformFrame2.m_Activity;
				if (math.any(targetDirection.xz != value.m_TargetDirection))
				{
					value.m_TargetDirection = math.normalizesafe(targetDirection.xz);
					if (!targetDirection.Equals(default(float3)))
					{
						transform.m_Rotation = quaternion.LookRotation(targetDirection, math.up());
					}
				}
				float3 value7 = value3 * 8f + value2.m_Velocity;
				MathUtils.TryNormalize(ref value7, value.m_MaxSpeed);
				value2.m_Velocity = value7;
				float3 @float = value2.m_Velocity * (num * 0.5f);
				transform.m_Position += @float;
				float2 xz = value2.m_Velocity.xz;
				if (math.length(xz) >= 0.1f)
				{
					float2 float2 = math.normalize(xz);
					transform.m_Rotation = quaternion.LookRotation(new float3(float2.x, 0f, float2.y), math.up());
				}
				transformFrame2.m_Position = transform.m_Position;
				transformFrame2.m_Velocity += value2.m_Velocity;
				transformFrame2.m_Rotation = transform.m_Rotation;
				transform.m_Position += @float;
				transformFrame = dynamicBuffer[num2];
				int2 @int = new int2(transformFrame.m_Activity, transformFrame2.m_Activity);
				dynamicBuffer[num2] = transformFrame2;
				if (@int.x != @int.y)
				{
					bool2 @bool = true;
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						@bool &= (j == num2) | (dynamicBuffer[j].m_Activity != @int);
					}
					if ((@bool.x && GetPropID(prefabRef.m_Prefab, transformFrame, value5, conditions, value6, out var propID3)) || (@bool.y && GetPropID(prefabRef.m_Prefab, transformFrame2, value5, conditions, value6, out propID3)))
					{
						Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_SubObjectEventArchetype);
						m_CommandBuffer.SetComponent(jobIndex, e, new SubObjectsUpdated(entity));
					}
					if (math.any(@bool & (@int == 24)))
					{
						m_CommandBuffer.AddComponent(jobIndex, entity, default(BatchesUpdated));
					}
				}
				nativeArray5[i] = value;
				nativeArray6[i] = value2;
				nativeArray7[i] = transform;
			}
		}

		private bool GetPropID(Entity prefab, TransformFrame transformFrame, PseudoRandomSeed pseudoRandomSeed, ActivityCondition conditions, DynamicBuffer<MeshGroup> meshGroups, out AnimatedPropID propID)
		{
			if (transformFrame.m_Activity == 10 || transformFrame.m_Activity == 11 || transformFrame.m_Activity == 1)
			{
				propID = AnimatedPropID.None;
				return false;
			}
			ObjectUtils.GetStateDuration(prefab, transformFrame.m_State, transformFrame.m_Activity, pseudoRandomSeed, AnimatedPropID.Any, conditions, meshGroups, ref m_SubMeshGroups, ref m_CharacterElements, ref m_SubMeshes, ref m_AnimationClips, out var _, out var animationClip, out var _);
			propID = animationClip.m_PropID;
			return propID != AnimatedPropID.None;
		}

		private AnimatedPropID GetPropID(in TransformFrame transformFrame, CurrentVehicle currentVehicle)
		{
			AnimatedPropID result = AnimatedPropID.Any;
			if (transformFrame.m_Activity == 0)
			{
				result = AnimatedPropID.None;
			}
			else if (transformFrame.m_Activity == 10 || transformFrame.m_Activity == 11 || transformFrame.m_Activity == 1)
			{
				result = AnimatedPropID.None;
				if (m_PrefabRefData.TryGetComponent(currentVehicle.m_Vehicle, out var componentData) && m_ActivityLocations.TryGetBuffer(componentData.m_Prefab, out var bufferData) && bufferData.Length != 0)
				{
					result = bufferData[0].m_PropID;
				}
			}
			return result;
		}

		private void StoppedMove(ArchetypeChunk chunk)
		{
			BufferAccessor<TransformFrame> bufferAccessor = chunk.GetBufferAccessor(ref m_TransformFrameType);
			NativeArray<Transform> nativeArray = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			float timeStep = 4f / 15f;
			Quad3 cornerPositions = default(Quad3);
			for (int i = 0; i < chunk.Count; i++)
			{
				PrefabRef prefabRef = nativeArray2[i];
				Transform value = nativeArray[i];
				ObjectGeometryData objectGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
				cornerPositions.a = value.m_Position + new float3(objectGeometryData.m_Bounds.min.x, 0f, objectGeometryData.m_Bounds.min.z);
				cornerPositions.b = value.m_Position + new float3(objectGeometryData.m_Bounds.min.x, 0f, objectGeometryData.m_Bounds.max.z);
				cornerPositions.c = value.m_Position + new float3(objectGeometryData.m_Bounds.max.x, 0f, objectGeometryData.m_Bounds.min.z);
				cornerPositions.d = value.m_Position + new float3(objectGeometryData.m_Bounds.max.x, 0f, objectGeometryData.m_Bounds.max.z);
				GetGroundHeight(cornerPositions, float3.zero, timeStep, 10f, out var heights);
				heights += 0.1f;
				cornerPositions.a.y = heights.x;
				cornerPositions.b.y = heights.y;
				cornerPositions.c.y = heights.z;
				cornerPositions.d.y = heights.w;
				float3 x = cornerPositions.d - cornerPositions.a;
				float3 y = cornerPositions.c - cornerPositions.a;
				float3 @float = math.normalize(math.cross(x, y));
				value.m_Rotation = quaternion.LookRotationSafe(value.m_Position - @float, @float);
				value.m_Position.y = heights.x;
				TransformFrame value2 = default(TransformFrame);
				DynamicBuffer<TransformFrame> dynamicBuffer = bufferAccessor[i];
				value2.m_Position = value.m_Position;
				value2.m_Velocity = default(float3);
				value2.m_Rotation = value.m_Rotation;
				dynamicBuffer[(int)m_TransformFrameIndex] = value2;
				nativeArray[i] = value;
			}
		}

		private void StumblingMove(ArchetypeChunk chunk)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<HumanNavigation> nativeArray2 = chunk.GetNativeArray(ref m_NavigationType);
			NativeArray<Moving> nativeArray3 = chunk.GetNativeArray(ref m_MovingType);
			NativeArray<Transform> nativeArray4 = chunk.GetNativeArray(ref m_TransformType);
			BufferAccessor<TransformFrame> bufferAccessor = chunk.GetBufferAccessor(ref m_TransformFrameType);
			int num = 4;
			float num2 = 4f / 15f;
			float num3 = num2 / (float)num;
			float grip = 10f;
			float num4 = 10f;
			float num5 = math.pow(0.9f, num3);
			Quad3 cornerPositions = default(Quad3);
			Quad3 quad = default(Quad3);
			Quad3 quad2 = default(Quad3);
			for (int i = 0; i < chunk.Count; i++)
			{
				PrefabRef prefabRef = nativeArray[i];
				_ = nativeArray2[i];
				Moving moving = nativeArray3[i];
				Transform transform = nativeArray4[i];
				ObjectGeometryData objectGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
				for (int j = 0; j < num; j++)
				{
					float3 momentOfInertia = ObjectUtils.CalculateMomentOfInertia(transform.m_Rotation, objectGeometryData.m_Size);
					float3 @float = transform.m_Position + math.mul(transform.m_Rotation, new float3(0f, objectGeometryData.m_Bounds.max.y * 0.5f, 0f));
					cornerPositions.a = ObjectUtils.LocalToWorld(transform, new float3(0f, objectGeometryData.m_Bounds.min.y + 0.2f, 0f));
					cornerPositions.b = ObjectUtils.LocalToWorld(transform, new float3(0f, objectGeometryData.m_Bounds.max.y - 0.2f, 0f));
					cornerPositions.c = ObjectUtils.LocalToWorld(transform, new float3(objectGeometryData.m_Bounds.min.x, objectGeometryData.m_Bounds.max.y * 0.5f, 0f));
					cornerPositions.d = ObjectUtils.LocalToWorld(transform, new float3(objectGeometryData.m_Bounds.max.x, objectGeometryData.m_Bounds.max.y * 0.5f, 0f));
					GetGroundHeight(cornerPositions, moving.m_Velocity, num2, num4, out var heights);
					heights += 0.2f;
					quad.a = CalculatePointVelocityDelta(cornerPositions.a, @float, moving, grip, num3, heights.x, num4);
					quad.b = CalculatePointVelocityDelta(cornerPositions.b, @float, moving, grip, num3, heights.y, num4);
					quad.c = CalculatePointVelocityDelta(cornerPositions.c, @float, moving, grip, num3, heights.z, num4);
					quad.d = CalculatePointVelocityDelta(cornerPositions.d, @float, moving, grip, num3, heights.w, num4);
					quad2.a = CalculatePointAngularVelocityDelta(cornerPositions.a, @float, quad.a, momentOfInertia);
					quad2.b = CalculatePointAngularVelocityDelta(cornerPositions.b, @float, quad.b, momentOfInertia);
					quad2.c = CalculatePointAngularVelocityDelta(cornerPositions.c, @float, quad.c, momentOfInertia);
					quad2.d = CalculatePointAngularVelocityDelta(cornerPositions.d, @float, quad.d, momentOfInertia);
					float3 float2 = (quad.a + quad.b + quad.c + quad.d) * 0.125f;
					float3 float3 = (quad2.a + quad2.b + quad2.c + quad2.d) * 0.125f;
					float2.y -= num4 * num3;
					moving.m_Velocity *= num5;
					moving.m_AngularVelocity *= num5;
					moving.m_Velocity += float2;
					moving.m_AngularVelocity += float3;
					float num6 = math.length(moving.m_AngularVelocity);
					if (num6 > 1E-05f)
					{
						quaternion a = quaternion.AxisAngle(moving.m_AngularVelocity / num6, num6 * num3);
						transform.m_Rotation = math.normalize(math.mul(a, transform.m_Rotation));
						float3 float4 = transform.m_Position + math.mul(transform.m_Rotation, new float3(0f, objectGeometryData.m_Bounds.max.y * 0.5f, 0f));
						transform.m_Position += @float - float4;
					}
					transform.m_Position += moving.m_Velocity * num3;
				}
				TransformFrame value = new TransformFrame
				{
					m_Position = transform.m_Position - moving.m_Velocity * (num2 * 0.5f),
					m_Velocity = moving.m_Velocity,
					m_Rotation = transform.m_Rotation
				};
				DynamicBuffer<TransformFrame> dynamicBuffer = bufferAccessor[i];
				dynamicBuffer[(int)m_TransformFrameIndex] = value;
				nativeArray3[i] = moving;
				nativeArray4[i] = transform;
			}
		}

		private float3 CalculatePointVelocityDelta(float3 position, float3 origin, Moving moving, float grip, float timeStep, float groundHeight, float gravity)
		{
			float3 float2;
			float3 @float = (float2 = ObjectUtils.CalculatePointVelocity(position - origin, moving));
			float2.y = 0f;
			float num = @float.y - gravity * timeStep;
			position.y += num * timeStep;
			float num2 = math.max(0f, groundHeight - position.y) / timeStep;
			float3 result = MathUtils.ClampLength(-float2, grip * math.min(timeStep, num2 / gravity));
			result.y += num2;
			return result;
		}

		private float3 CalculatePointAngularVelocityDelta(float3 position, float3 origin, float3 velocityDelta, float3 momentOfInertia)
		{
			return math.cross(position - origin, velocityDelta) / momentOfInertia;
		}

		private void GetGroundHeight(Quad3 cornerPositions, float3 velocity, float timeStep, float gravity, out float4 heights)
		{
			float4 @float = default(float4);
			@float.x = TerrainUtils.SampleHeight(ref m_TerrainHeightData, cornerPositions.a);
			@float.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, cornerPositions.b);
			@float.z = TerrainUtils.SampleHeight(ref m_TerrainHeightData, cornerPositions.c);
			@float.w = TerrainUtils.SampleHeight(ref m_TerrainHeightData, cornerPositions.d);
			float4 float2 = new float4(cornerPositions.a.y, cornerPositions.b.y, cornerPositions.c.y, cornerPositions.d.y);
			heights = math.select(float.MinValue, @float, @float < float2 + 4f);
			NetIterator iterator = new NetIterator
			{
				m_Bounds = MathUtils.Bounds(cornerPositions)
			};
			iterator.m_Bounds.min.y += (math.min(0f, velocity.y) - gravity * timeStep) * timeStep;
			iterator.m_Bounds.max.y += math.max(0f, velocity.y) * timeStep;
			iterator.m_CornerPositions = cornerPositions;
			iterator.m_GroundHeights = heights;
			iterator.m_CompositionData = m_CompositionData;
			iterator.m_OrphanData = m_OrphanData;
			iterator.m_NodeData = m_NodeData;
			iterator.m_NodeGeometryData = m_NodeGeometryData;
			iterator.m_EdgeGeometryData = m_EdgeGeometryData;
			iterator.m_StartNodeGeometryData = m_StartNodeGeometryData;
			iterator.m_EndNodeGeometryData = m_EndNodeGeometryData;
			iterator.m_PrefabCompositionData = m_PrefabCompositionData;
			m_NetSearchTree.Iterate(ref iterator);
			heights = iterator.m_GroundHeights;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Human> __Game_Creatures_Human_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Stumbling> __Game_Creatures_Stumbling_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Stopped> __Game_Objects_Stopped_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<MeshGroup> __Game_Rendering_MeshGroup_RO_BufferTypeHandle;

		public ComponentTypeHandle<HumanNavigation> __Game_Creatures_HumanNavigation_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Moving> __Game_Objects_Moving_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RW_ComponentTypeHandle;

		public BufferTypeHandle<TransformFrame> __Game_Objects_TransformFrame_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Orphan> __Game_Net_Orphan_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NodeGeometry> __Game_Net_NodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> __Game_Prefabs_SubMeshGroup_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CharacterElement> __Game_Prefabs_CharacterElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<AnimationClip> __Game_Prefabs_AnimationClip_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<AnimationMotion> __Game_Prefabs_AnimationMotion_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMesh> __Game_Prefabs_SubMesh_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ActivityLocationElement> __Game_Prefabs_ActivityLocationElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Creatures_Human_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Human>(isReadOnly: true);
			__Game_Creatures_Stumbling_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Stumbling>(isReadOnly: true);
			__Game_Objects_Stopped_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Stopped>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentVehicle>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PseudoRandomSeed>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Rendering_MeshGroup_RO_BufferTypeHandle = state.GetBufferTypeHandle<MeshGroup>(isReadOnly: true);
			__Game_Creatures_HumanNavigation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<HumanNavigation>();
			__Game_Objects_Moving_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Moving>();
			__Game_Objects_Transform_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>();
			__Game_Objects_TransformFrame_RW_BufferTypeHandle = state.GetBufferTypeHandle<TransformFrame>();
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Net_Orphan_RO_ComponentLookup = state.GetComponentLookup<Orphan>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
			__Game_Net_NodeGeometry_RO_ComponentLookup = state.GetComponentLookup<NodeGeometry>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_SubMeshGroup_RO_BufferLookup = state.GetBufferLookup<SubMeshGroup>(isReadOnly: true);
			__Game_Prefabs_CharacterElement_RO_BufferLookup = state.GetBufferLookup<CharacterElement>(isReadOnly: true);
			__Game_Prefabs_AnimationClip_RO_BufferLookup = state.GetBufferLookup<AnimationClip>(isReadOnly: true);
			__Game_Prefabs_AnimationMotion_RO_BufferLookup = state.GetBufferLookup<AnimationMotion>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = state.GetBufferLookup<SubMesh>(isReadOnly: true);
			__Game_Prefabs_ActivityLocationElement_RO_BufferLookup = state.GetBufferLookup<ActivityLocationElement>(isReadOnly: true);
		}
	}

	private SimulationSystem m_SimulationSystem;

	private TerrainSystem m_TerrainSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_HumanQuery;

	private EntityArchetype m_SubObjectEventArchetype;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_HumanQuery = GetEntityQuery(ComponentType.ReadOnly<Human>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<TransformFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_SubObjectEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<SubObjectsUpdated>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint index = m_SimulationSystem.frameIndex % 16;
		m_HumanQuery.ResetFilter();
		m_HumanQuery.SetSharedComponentFilter(new UpdateFrame(index));
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new UpdateTransformDataJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_HumanType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Human_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StumblingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Stumbling_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StoppedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Stopped_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentVehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PseudoRandomSeedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MeshGroupType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Rendering_MeshGroup_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_NavigationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_HumanNavigation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MovingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Moving_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformFrameType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_TransformFrame_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OrphanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Orphan_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StartNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EndNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubMeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
			m_CharacterElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_CharacterElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_AnimationClips = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AnimationClip_RO_BufferLookup, ref base.CheckedStateRef),
			m_AnimationMotions = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AnimationMotion_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
			m_ActivityLocations = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ActivityLocationElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
			m_TransformFrameIndex = m_SimulationSystem.frameIndex / 16 % 4,
			m_SubObjectEventArchetype = m_SubObjectEventArchetype,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_HumanQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		m_TerrainSystem.AddCPUHeightReader(jobHandle);
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
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
	public HumanMoveSystem()
	{
	}
}
