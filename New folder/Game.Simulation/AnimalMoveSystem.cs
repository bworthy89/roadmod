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
public class AnimalMoveSystem : GameSystemBase
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
		public ComponentTypeHandle<Animal> m_AnimalType;

		[ReadOnly]
		public ComponentTypeHandle<AnimalNavigation> m_NavigationType;

		[ReadOnly]
		public ComponentTypeHandle<Stumbling> m_StumblingType;

		[ReadOnly]
		public ComponentTypeHandle<PseudoRandomSeed> m_PseudoRandomSeedType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<MeshGroup> m_MeshGroupType;

		public ComponentTypeHandle<Moving> m_MovingType;

		public ComponentTypeHandle<Transform> m_TransformType;

		public BufferTypeHandle<TransformFrame> m_TransformFrameType;

		[ReadOnly]
		public ComponentTypeHandle<AnimalCurrentLane> m_AnimalCurrentLaneType;

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
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public ComponentLookup<AnimalData> m_PrefabAnimalData;

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
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public uint m_TransformFrameIndex;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has(ref m_StumblingType))
			{
				StumblingMove(chunk);
			}
			else
			{
				NormalMove(chunk);
			}
		}

		private void NormalMove(ArchetypeChunk chunk)
		{
			NativeArray<Animal> nativeArray = chunk.GetNativeArray(ref m_AnimalType);
			NativeArray<AnimalNavigation> nativeArray2 = chunk.GetNativeArray(ref m_NavigationType);
			NativeArray<AnimalCurrentLane> nativeArray3 = chunk.GetNativeArray(ref m_AnimalCurrentLaneType);
			NativeArray<Moving> nativeArray4 = chunk.GetNativeArray(ref m_MovingType);
			NativeArray<Transform> nativeArray5 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<PseudoRandomSeed> nativeArray6 = chunk.GetNativeArray(ref m_PseudoRandomSeedType);
			NativeArray<PrefabRef> nativeArray7 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<TransformFrame> bufferAccessor = chunk.GetBufferAccessor(ref m_TransformFrameType);
			BufferAccessor<MeshGroup> bufferAccessor2 = chunk.GetBufferAccessor(ref m_MeshGroupType);
			float num = 4f / 15f;
			int index = (int)m_TransformFrameIndex;
			int index2 = math.select((int)(m_TransformFrameIndex - 1), 3, m_TransformFrameIndex == 0);
			for (int i = 0; i < chunk.Count; i++)
			{
				Animal animal = nativeArray[i];
				AnimalNavigation animalNavigation = nativeArray2[i];
				AnimalCurrentLane animalCurrentLane = nativeArray3[i];
				Moving value = nativeArray4[i];
				Transform transform = nativeArray5[i];
				PrefabRef prefabRef = nativeArray7[i];
				DynamicBuffer<TransformFrame> dynamicBuffer = bufferAccessor[i];
				TransformFrame oldFrameData = dynamicBuffer[index2];
				if ((animal.m_Flags & AnimalFlags.Roaming) != 0)
				{
					float2 value2 = animalNavigation.m_TargetPosition.xz - transform.m_Position.xz;
					MathUtils.TryNormalize(ref value2, animalNavigation.m_MaxSpeed * num);
					animalNavigation.m_TargetPosition.xz = transform.m_Position.xz + value2;
					bool hasDepth;
					float num2 = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, animalNavigation.m_TargetPosition, out hasDepth);
					if (animalNavigation.m_TargetActivity == 8)
					{
						AnimalData animalData = m_PrefabAnimalData[prefabRef.m_Prefab];
						if ((animal.m_Flags & AnimalFlags.SwimmingTarget) == 0)
						{
							animalData.m_SwimDepth.min = 0f;
						}
						Bounds1 bounds = num2 - MathUtils.Invert(animalData.m_SwimDepth);
						animalNavigation.m_TargetPosition.y = MathUtils.Clamp(animalNavigation.m_TargetPosition.y, bounds);
					}
					else if (animalNavigation.m_TargetActivity == 9)
					{
						AnimalData animalData2 = m_PrefabAnimalData[prefabRef.m_Prefab];
						if ((animal.m_Flags & AnimalFlags.FlyingTarget) == 0)
						{
							animalData2.m_FlyHeight.min = 0f;
						}
						Bounds1 bounds2 = num2 + animalData2.m_FlyHeight;
						animalNavigation.m_TargetPosition.y = MathUtils.Clamp(animalNavigation.m_TargetPosition.y, bounds2);
					}
					else
					{
						animalNavigation.m_TargetPosition.y = num2 - (hasDepth ? 0.2f : 0f);
					}
				}
				float3 value3 = animalNavigation.m_TargetPosition - transform.m_Position;
				MathUtils.TryNormalize(ref value3, animalNavigation.m_MaxSpeed);
				TransformFrame newFrameData = default(TransformFrame);
				switch ((ActivityType)animalNavigation.m_TargetActivity)
				{
				case ActivityType.Walking:
				case ActivityType.Running:
				case ActivityType.Flying:
					newFrameData.m_State = TransformState.Move;
					newFrameData.m_Activity = animalNavigation.m_TargetActivity;
					break;
				case ActivityType.Swimming:
					newFrameData.m_State = ((m_PrefabAnimalData[prefabRef.m_Prefab].m_PrimaryTravelMethod != AnimalTravelFlags.Swimming && !(animalNavigation.m_MaxSpeed >= 0.1f)) ? TransformState.Idle : TransformState.Move);
					newFrameData.m_Activity = animalNavigation.m_TargetActivity;
					break;
				case ActivityType.None:
					newFrameData.m_State = ((!(animalNavigation.m_MaxSpeed >= 0.1f)) ? TransformState.Idle : TransformState.Move);
					break;
				default:
					newFrameData.m_State = TransformState.Idle;
					newFrameData.m_Activity = animalNavigation.m_TargetActivity;
					break;
				}
				CollectionUtils.TryGet(nativeArray6, i, out var value4);
				CollectionUtils.TryGet(bufferAccessor2, i, out var value5);
				ObjectUtils.UpdateAnimation(oldPropID: new AnimatedPropID(-1), newPropID: new AnimatedPropID(-1), prefab: prefabRef.m_Prefab, timeStep: num, pseudoRandomSeed: value4, meshGroups: value5, subMeshGroupBuffers: ref m_SubMeshGroups, characterElementBuffers: ref m_CharacterElements, subMeshBuffers: ref m_SubMeshes, animationClipBuffers: ref m_AnimationClips, animationMotionBuffers: ref m_AnimationMotions, conditions: (ActivityCondition)0u, maxSpeed: ref animalNavigation.m_MaxSpeed, activity: ref animalNavigation.m_TargetActivity, targetPosition: ref animalNavigation.m_TargetPosition, targetDirection: ref animalNavigation.m_TargetDirection, transform: ref transform, oldFrameData: ref oldFrameData, newFrameData: ref newFrameData);
				animalNavigation.m_TransformState = newFrameData.m_State;
				animalNavigation.m_LastActivity = newFrameData.m_Activity;
				newFrameData.m_Position = transform.m_Position;
				newFrameData.m_Velocity = value.m_Velocity;
				newFrameData.m_Rotation = transform.m_Rotation;
				float3 value6 = value3 * 8f + value.m_Velocity;
				MathUtils.TryNormalize(ref value6, animalNavigation.m_MaxSpeed);
				float num3 = 1f;
				if ((animalCurrentLane.m_Flags & CreatureLaneFlags.EndOfPath) != 0 && (animal.m_Flags & AnimalFlags.FlyingTarget) != 0 && math.dot(math.normalize(value6), math.normalize(value.m_Velocity)) < -0.98f)
				{
					num3 *= 0.8f;
				}
				value.m_Velocity = value6 * num3;
				float3 @float = value.m_Velocity * (num * 0.5f);
				transform.m_Position += @float;
				if (math.length(value.m_Velocity.xz) >= 0.1f && (animalNavigation.m_TargetActivity != 9 || (animal.m_Flags & AnimalFlags.FlyingTarget) != 0))
				{
					transform.m_Rotation = quaternion.LookRotationSafe(value6, math.up());
				}
				transform.m_Position += @float;
				dynamicBuffer[index] = newFrameData;
				nativeArray4[i] = value;
				nativeArray5[i] = transform;
			}
		}

		private void StumblingMove(ArchetypeChunk chunk)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<AnimalNavigation> nativeArray2 = chunk.GetNativeArray(ref m_NavigationType);
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
					cornerPositions.a = ObjectUtils.LocalToWorld(transform, new float3(0f, objectGeometryData.m_Bounds.min.y + 0.2f, objectGeometryData.m_Bounds.min.z + 0.2f));
					cornerPositions.b = ObjectUtils.LocalToWorld(transform, new float3(0f, objectGeometryData.m_Bounds.min.y + 0.2f, objectGeometryData.m_Bounds.max.z - 0.2f));
					cornerPositions.c = ObjectUtils.LocalToWorld(transform, new float3(0f, objectGeometryData.m_Bounds.max.y - 0.2f, objectGeometryData.m_Bounds.min.z + 0.2f));
					cornerPositions.d = ObjectUtils.LocalToWorld(transform, new float3(0f, objectGeometryData.m_Bounds.max.y - 0.2f, objectGeometryData.m_Bounds.max.z - 0.2f));
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
		public ComponentTypeHandle<Animal> __Game_Creatures_Animal_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<AnimalNavigation> __Game_Creatures_AnimalNavigation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Stumbling> __Game_Creatures_Stumbling_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<MeshGroup> __Game_Rendering_MeshGroup_RO_BufferTypeHandle;

		public ComponentTypeHandle<Moving> __Game_Objects_Moving_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RW_ComponentTypeHandle;

		public BufferTypeHandle<TransformFrame> __Game_Objects_TransformFrame_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<AnimalCurrentLane> __Game_Creatures_AnimalCurrentLane_RO_ComponentTypeHandle;

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
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AnimalData> __Game_Prefabs_AnimalData_RO_ComponentLookup;

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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Creatures_Animal_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Animal>(isReadOnly: true);
			__Game_Creatures_AnimalNavigation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AnimalNavigation>(isReadOnly: true);
			__Game_Creatures_Stumbling_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Stumbling>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PseudoRandomSeed>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Rendering_MeshGroup_RO_BufferTypeHandle = state.GetBufferTypeHandle<MeshGroup>(isReadOnly: true);
			__Game_Objects_Moving_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Moving>();
			__Game_Objects_Transform_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>();
			__Game_Objects_TransformFrame_RW_BufferTypeHandle = state.GetBufferTypeHandle<TransformFrame>();
			__Game_Creatures_AnimalCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AnimalCurrentLane>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Net_Orphan_RO_ComponentLookup = state.GetComponentLookup<Orphan>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
			__Game_Net_NodeGeometry_RO_ComponentLookup = state.GetComponentLookup<NodeGeometry>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_AnimalData_RO_ComponentLookup = state.GetComponentLookup<AnimalData>(isReadOnly: true);
			__Game_Prefabs_SubMeshGroup_RO_BufferLookup = state.GetBufferLookup<SubMeshGroup>(isReadOnly: true);
			__Game_Prefabs_CharacterElement_RO_BufferLookup = state.GetBufferLookup<CharacterElement>(isReadOnly: true);
			__Game_Prefabs_AnimationClip_RO_BufferLookup = state.GetBufferLookup<AnimationClip>(isReadOnly: true);
			__Game_Prefabs_AnimationMotion_RO_BufferLookup = state.GetBufferLookup<AnimationMotion>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = state.GetBufferLookup<SubMesh>(isReadOnly: true);
		}
	}

	private SimulationSystem m_SimulationSystem;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private EntityQuery m_AnimalQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_AnimalQuery = GetEntityQuery(ComponentType.ReadOnly<Animal>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<TransformFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint num = m_SimulationSystem.frameIndex % 16;
		if (num == 5 || num == 9 || num == 13)
		{
			m_AnimalQuery.ResetFilter();
			m_AnimalQuery.SetSharedComponentFilter(new UpdateFrame(num));
			JobHandle deps;
			JobHandle dependencies;
			JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new UpdateTransformDataJob
			{
				m_AnimalType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Animal_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_NavigationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_AnimalNavigation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_StumblingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_Stumbling_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PseudoRandomSeedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_MeshGroupType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Rendering_MeshGroup_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_MovingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Moving_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TransformFrameType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_TransformFrame_RW_BufferTypeHandle, ref base.CheckedStateRef),
				m_AnimalCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_AnimalCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OrphanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Orphan_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_StartNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EndNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabAnimalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AnimalData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SubMeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
				m_CharacterElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_CharacterElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_AnimationClips = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AnimationClip_RO_BufferLookup, ref base.CheckedStateRef),
				m_AnimationMotions = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_AnimationMotion_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubMeshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
				m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
				m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
				m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
				m_TransformFrameIndex = m_SimulationSystem.frameIndex / 16 % 4
			}, m_AnimalQuery, JobHandle.CombineDependencies(base.Dependency, deps, dependencies));
			m_TerrainSystem.AddCPUHeightReader(jobHandle);
			m_WaterSystem.AddSurfaceReader(jobHandle);
			m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
			base.Dependency = jobHandle;
		}
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
	public AnimalMoveSystem()
	{
	}
}
