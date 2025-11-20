using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
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
public class VehicleOutOfControlSystem : GameSystemBase
{
	[BurstCompile]
	private struct VehicleOutOfControlMoveJob : IJobChunk
	{
		private struct NetIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds3 m_Bounds;

			public Quad3 m_CornerPositions;

			public Quad3 m_CornerPositions2;

			public float4 m_GroundHeights;

			public float4 m_GroundHeights2;

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
				CheckCircle(center, radius, m_CornerPositions2.a, ref m_GroundHeights2.x);
				CheckCircle(center, radius, m_CornerPositions2.b, ref m_GroundHeights2.y);
				CheckCircle(center, radius, m_CornerPositions2.c, ref m_GroundHeights2.z);
				CheckCircle(center, radius, m_CornerPositions2.d, ref m_GroundHeights2.w);
			}

			private void CheckTriangle(Triangle3 triangle)
			{
				CheckTriangle(triangle, m_CornerPositions.a, ref m_GroundHeights.x);
				CheckTriangle(triangle, m_CornerPositions.b, ref m_GroundHeights.y);
				CheckTriangle(triangle, m_CornerPositions.c, ref m_GroundHeights.z);
				CheckTriangle(triangle, m_CornerPositions.d, ref m_GroundHeights.w);
				CheckTriangle(triangle, m_CornerPositions2.a, ref m_GroundHeights2.x);
				CheckTriangle(triangle, m_CornerPositions2.b, ref m_GroundHeights2.y);
				CheckTriangle(triangle, m_CornerPositions2.c, ref m_GroundHeights2.z);
				CheckTriangle(triangle, m_CornerPositions2.d, ref m_GroundHeights2.w);
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
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

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
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public ComponentLookup<CarData> m_PrefabCarData;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetSearchTree;

		[ReadOnly]
		public uint m_TransformFrameIndex;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Moving> nativeArray2 = chunk.GetNativeArray(ref m_MovingType);
			NativeArray<Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
			BufferAccessor<TransformFrame> bufferAccessor = chunk.GetBufferAccessor(ref m_TransformFrameType);
			int num = 4;
			float num2 = 4f / 15f;
			float num3 = num2 / (float)num;
			float num4 = 10f;
			float num5 = math.pow(0.95f, num3);
			Quad3 quad2 = default(Quad3);
			Quad3 quad3 = default(Quad3);
			Quad3 quad4 = default(Quad3);
			Quad3 quad5 = default(Quad3);
			for (int i = 0; i < chunk.Count; i++)
			{
				PrefabRef prefabRef = nativeArray[i];
				Moving moving = nativeArray2[i];
				Transform value = nativeArray3[i];
				CarData carData = m_PrefabCarData[prefabRef.m_Prefab];
				ObjectGeometryData objectGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
				for (int j = 0; j < num; j++)
				{
					float3 momentOfInertia = ObjectUtils.CalculateMomentOfInertia(value.m_Rotation, objectGeometryData.m_Size);
					float3 forward = math.forward(value.m_Rotation);
					float3 @float = value.m_Position + math.mul(value.m_Rotation, new float3(0f, objectGeometryData.m_Bounds.max.y * 0.25f, 0f));
					Quad3 quad = ObjectUtils.CalculateBaseCorners(value.m_Position, value.m_Rotation, objectGeometryData.m_Bounds);
					Quad3 cornerPositions = quad + math.mul(value.m_Rotation, new float3(0f, objectGeometryData.m_Bounds.max.y, 0f));
					GetGroundHeight(quad, cornerPositions, moving.m_Velocity, num2, num4, out var heights, out var heights2);
					quad2.a = CalculatePointVelocityDelta(quad.a, @float, moving, forward, carData.m_Braking, num3, heights.x, num4);
					quad2.b = CalculatePointVelocityDelta(quad.b, @float, moving, forward, carData.m_Braking, num3, heights.y, num4);
					quad2.c = CalculatePointVelocityDelta(quad.c, @float, moving, forward, carData.m_Braking, num3, heights.z, num4);
					quad2.d = CalculatePointVelocityDelta(quad.d, @float, moving, forward, carData.m_Braking, num3, heights.w, num4);
					quad3.a = CalculatePointVelocityDelta(cornerPositions.a, @float, moving, carData.m_Braking, num3, heights2.x, num4);
					quad3.b = CalculatePointVelocityDelta(cornerPositions.b, @float, moving, carData.m_Braking, num3, heights2.y, num4);
					quad3.c = CalculatePointVelocityDelta(cornerPositions.c, @float, moving, carData.m_Braking, num3, heights2.z, num4);
					quad3.d = CalculatePointVelocityDelta(cornerPositions.d, @float, moving, carData.m_Braking, num3, heights2.w, num4);
					quad4.a = CalculatePointAngularVelocityDelta(quad.a, @float, quad2.a, momentOfInertia);
					quad4.b = CalculatePointAngularVelocityDelta(quad.b, @float, quad2.b, momentOfInertia);
					quad4.c = CalculatePointAngularVelocityDelta(quad.c, @float, quad2.c, momentOfInertia);
					quad4.d = CalculatePointAngularVelocityDelta(quad.d, @float, quad2.d, momentOfInertia);
					quad5.a = CalculatePointAngularVelocityDelta(cornerPositions.a, @float, quad3.a, momentOfInertia);
					quad5.b = CalculatePointAngularVelocityDelta(cornerPositions.b, @float, quad3.b, momentOfInertia);
					quad5.c = CalculatePointAngularVelocityDelta(cornerPositions.c, @float, quad3.c, momentOfInertia);
					quad5.d = CalculatePointAngularVelocityDelta(cornerPositions.d, @float, quad3.d, momentOfInertia);
					float3 float2 = (quad2.a + quad2.b + quad2.c + quad2.d + quad3.a + quad3.b + quad3.c + quad3.d) * 0.125f;
					float3 float3 = (quad4.a + quad4.b + quad4.c + quad4.d + quad5.a + quad5.b + quad5.c + quad5.d) * 0.125f;
					float2.y -= num4 * num3;
					moving.m_Velocity *= num5;
					moving.m_AngularVelocity *= num5;
					moving.m_Velocity += float2;
					moving.m_AngularVelocity += float3;
					float num6 = math.length(moving.m_AngularVelocity);
					if (num6 > 1E-05f)
					{
						quaternion a = quaternion.AxisAngle(moving.m_AngularVelocity / num6, num6 * num3);
						value.m_Rotation = math.normalize(math.mul(a, value.m_Rotation));
						float3 float4 = value.m_Position + math.mul(value.m_Rotation, new float3(0f, objectGeometryData.m_Bounds.max.y * 0.25f, 0f));
						value.m_Position += @float - float4;
					}
					value.m_Position += moving.m_Velocity * num3;
				}
				TransformFrame value2 = new TransformFrame
				{
					m_Position = value.m_Position - moving.m_Velocity * (num2 * 0.5f),
					m_Velocity = moving.m_Velocity,
					m_Rotation = value.m_Rotation
				};
				DynamicBuffer<TransformFrame> dynamicBuffer = bufferAccessor[i];
				dynamicBuffer[(int)m_TransformFrameIndex] = value2;
				nativeArray2[i] = moving;
				nativeArray3[i] = value;
			}
		}

		private float3 CalculatePointVelocityDelta(float3 position, float3 origin, Moving moving, float3 forward, float grip, float timeStep, float groundHeight, float gravity)
		{
			float3 @float = ObjectUtils.CalculatePointVelocity(position - origin, moving);
			float num = math.dot(forward, @float);
			float3 float2 = @float - forward * (num * 0.5f);
			float2.y = 0f;
			float num2 = @float.y - gravity * timeStep;
			position.y += num2 * timeStep;
			float num3 = math.max(0f, groundHeight - position.y) / timeStep;
			float3 result = MathUtils.ClampLength(-float2, grip * math.min(timeStep, num3 / gravity));
			result.y += num3;
			return result;
		}

		private float3 CalculatePointVelocityDelta(float3 position, float3 origin, Moving moving, float grip, float timeStep, float groundHeight, float gravity)
		{
			float3 @float = ObjectUtils.CalculatePointVelocity(position - origin, moving);
			float3 float2 = @float * 0.5f;
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

		private void GetGroundHeight(Quad3 cornerPositions, Quad3 cornerPositions2, float3 velocity, float timeStep, float gravity, out float4 heights, out float4 heights2)
		{
			float4 @float = default(float4);
			@float.x = TerrainUtils.SampleHeight(ref m_TerrainHeightData, cornerPositions.a);
			@float.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, cornerPositions.b);
			@float.z = TerrainUtils.SampleHeight(ref m_TerrainHeightData, cornerPositions.c);
			@float.w = TerrainUtils.SampleHeight(ref m_TerrainHeightData, cornerPositions.d);
			float4 float2 = default(float4);
			float2.x = TerrainUtils.SampleHeight(ref m_TerrainHeightData, cornerPositions2.a);
			float2.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, cornerPositions2.b);
			float2.z = TerrainUtils.SampleHeight(ref m_TerrainHeightData, cornerPositions2.c);
			float2.w = TerrainUtils.SampleHeight(ref m_TerrainHeightData, cornerPositions2.d);
			float4 float3 = new float4(cornerPositions.a.y, cornerPositions.b.y, cornerPositions.c.y, cornerPositions.d.y);
			float4 float4 = new float4(cornerPositions2.a.y, cornerPositions2.b.y, cornerPositions2.c.y, cornerPositions2.d.y);
			heights = math.select(float.MinValue, @float, @float < float3 + 4f);
			heights2 = math.select(float.MinValue, float2, float2 < float4 + 4f);
			NetIterator iterator = new NetIterator
			{
				m_Bounds = (MathUtils.Bounds(cornerPositions) | MathUtils.Bounds(cornerPositions2))
			};
			iterator.m_Bounds.min.y += (math.min(0f, velocity.y) - gravity * timeStep) * timeStep;
			iterator.m_Bounds.max.y += math.max(0f, velocity.y) * timeStep;
			iterator.m_CornerPositions = cornerPositions;
			iterator.m_CornerPositions2 = cornerPositions2;
			iterator.m_GroundHeights = heights;
			iterator.m_GroundHeights2 = heights2;
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
			heights2 = iterator.m_GroundHeights2;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

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
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
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
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(isReadOnly: true);
		}
	}

	private SimulationSystem m_SimulationSystem;

	private TerrainSystem m_TerrainSystem;

	private Game.Net.SearchSystem m_NetSearchSystem;

	private EntityQuery m_VehicleQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_NetSearchSystem = base.World.GetOrCreateSystemManaged<Game.Net.SearchSystem>();
		m_VehicleQuery = GetEntityQuery(ComponentType.ReadOnly<OutOfControl>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<Transform>(), ComponentType.ReadWrite<Moving>(), ComponentType.ReadWrite<TransformFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<TripSource>());
		RequireForUpdate(m_VehicleQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint index = m_SimulationSystem.frameIndex & 0xF;
		m_VehicleQuery.ResetFilter();
		m_VehicleQuery.SetSharedComponentFilter(new UpdateFrame(index));
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new VehicleOutOfControlMoveJob
		{
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
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
			m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_NetSearchTree = m_NetSearchSystem.GetNetSearchTree(readOnly: true, out dependencies),
			m_TransformFrameIndex = m_SimulationSystem.frameIndex / 16 % 4
		}, m_VehicleQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		m_TerrainSystem.AddCPUHeightReader(jobHandle);
		m_NetSearchSystem.AddNetSearchTreeReader(jobHandle);
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
	public VehicleOutOfControlSystem()
	{
	}
}
