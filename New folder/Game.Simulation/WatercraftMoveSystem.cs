using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
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
public class WatercraftMoveSystem : GameSystemBase
{
	[BurstCompile]
	public struct UpdateTransformDataJob : IJobChunk
	{
		[ReadOnly]
		public uint m_TransformFrameIndex;

		[ReadOnly]
		public ComponentTypeHandle<WatercraftNavigation> m_NavigationType;

		[ReadOnly]
		public ComponentTypeHandle<WatercraftCurrentLane> m_CurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabGeometryData;

		[ReadOnly]
		public ComponentLookup<WatercraftData> m_PrefabWatercraftData;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		public ComponentTypeHandle<Moving> m_MovingType;

		public ComponentTypeHandle<Transform> m_TransformType;

		public BufferTypeHandle<TransformFrame> m_TransformFrameType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<WatercraftNavigation> nativeArray2 = chunk.GetNativeArray(ref m_NavigationType);
			NativeArray<WatercraftCurrentLane> nativeArray3 = chunk.GetNativeArray(ref m_CurrentLaneType);
			NativeArray<Moving> nativeArray4 = chunk.GetNativeArray(ref m_MovingType);
			NativeArray<Transform> nativeArray5 = chunk.GetNativeArray(ref m_TransformType);
			BufferAccessor<TransformFrame> bufferAccessor = chunk.GetBufferAccessor(ref m_TransformFrameType);
			float num = 4f / 15f;
			for (int i = 0; i < chunk.Count; i++)
			{
				PrefabRef prefabRef = nativeArray[i];
				WatercraftNavigation watercraftNavigation = nativeArray2[i];
				WatercraftCurrentLane watercraftCurrentLane = nativeArray3[i];
				Moving value = nativeArray4[i];
				Transform transform = nativeArray5[i];
				ObjectGeometryData prefabGeometryData = m_PrefabGeometryData[prefabRef.m_Prefab];
				WatercraftData watercraftData = m_PrefabWatercraftData[prefabRef.m_Prefab];
				VehicleUtils.CalculateShipNavigationPivots(transform, prefabGeometryData, out var pivot, out var pivot2);
				float3 @float = pivot - pivot2;
				float num2 = math.length(@float.xz);
				float num3 = 1.5f * math.lerp(watercraftData.m_Turning.x, watercraftData.m_Turning.y, watercraftNavigation.m_MaxSpeed / watercraftData.m_MaxSpeed);
				float2 value2 = watercraftNavigation.m_TargetDirection.xz;
				float2 float2;
				float2 float3;
				if (MathUtils.TryNormalize(ref value2, num2 * 0.5f))
				{
					float2 = watercraftNavigation.m_TargetPosition.xz + value2 - pivot.xz;
					float3 = watercraftNavigation.m_TargetPosition.xz - value2 - pivot2.xz;
				}
				else
				{
					float2 = watercraftNavigation.m_TargetPosition.xz - pivot.xz;
					float3 = watercraftNavigation.m_TargetPosition.xz - pivot2.xz;
					value2 = value.m_Velocity.xz;
					num3 = math.min(num3, watercraftNavigation.m_MaxSpeed / (num2 * 0.5f));
				}
				float2 value3 = float2 + float3;
				float2 value4;
				if ((watercraftCurrentLane.m_LaneFlags & (WatercraftLaneFlags.ResetSpeed | WatercraftLaneFlags.Connection)) != 0)
				{
					value4 = value3;
					value.m_AngularVelocity.y = 0f;
					if (MathUtils.TryNormalize(ref value2))
					{
						transform.m_Rotation = quaternion.LookRotationSafe(new float3(value2.x, 0f, value2.y), math.up());
					}
				}
				else
				{
					MathUtils.TryNormalize(ref value3, watercraftNavigation.m_MaxSpeed);
					value4 = value3 * 8f + value.m_Velocity.xz;
					float num4 = 0f;
					float2 value5 = @float.xz;
					if (MathUtils.TryNormalize(ref value5) && MathUtils.TryNormalize(ref value2))
					{
						num4 = math.acos(math.saturate(math.dot(value5, value2)));
						num4 = math.min(num4 * watercraftData.m_AngularAcceleration, num3);
						if (math.dot(MathUtils.Left(value5), value2) > 0f)
						{
							num4 = 0f - num4;
						}
					}
					float valueToClamp = num4 - value.m_AngularVelocity.y;
					valueToClamp = math.clamp(valueToClamp, (0f - watercraftData.m_AngularAcceleration) * num, watercraftData.m_AngularAcceleration * num);
					value.m_AngularVelocity.y += valueToClamp;
					quaternion a = quaternion.LookRotationSafe(new float3(value5.x, 0f, value5.y), math.up());
					transform.m_Rotation = math.mul(a, quaternion.RotateY(value.m_AngularVelocity.y * num));
				}
				MathUtils.TryNormalize(ref value4, watercraftNavigation.m_MaxSpeed);
				value.m_Velocity.xz = value4;
				float3 position = transform.m_Position + value.m_Velocity * num;
				SampleWater(ref position, ref transform.m_Rotation, prefabGeometryData);
				TransformFrame value6 = new TransformFrame
				{
					m_Position = math.lerp(transform.m_Position, position, 0.5f),
					m_Velocity = value.m_Velocity,
					m_Rotation = transform.m_Rotation
				};
				transform.m_Position = position;
				DynamicBuffer<TransformFrame> dynamicBuffer = bufferAccessor[i];
				dynamicBuffer[(int)m_TransformFrameIndex] = value6;
				nativeArray4[i] = value;
				nativeArray5[i] = transform;
			}
		}

		private void SampleWater(ref float3 position, ref quaternion rotation, ObjectGeometryData prefabGeometryData)
		{
			float2 @float = prefabGeometryData.m_Size.xz * 0.4f;
			float3 float2 = position + math.rotate(rotation, new float3(0f - @float.x, 0f, 0f - @float.y));
			float3 float3 = position + math.rotate(rotation, new float3(@float.x, 0f, 0f - @float.y));
			float3 float4 = position + math.rotate(rotation, new float3(0f - @float.x, 0f, @float.y));
			float3 float5 = position + math.rotate(rotation, new float3(@float.x, 0f, @float.y));
			float4 float6 = default(float4);
			float4 float7 = default(float4);
			float4 x = default(float4);
			WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, float2, out float6.x, out x.x, out float7.x);
			WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, float3, out float6.y, out x.y, out float7.y);
			WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, float4, out float6.z, out x.z, out float7.z);
			WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, float5, out float6.w, out x.w, out float7.w);
			float num = math.max(0f, prefabGeometryData.m_Bounds.min.y * -0.75f);
			x = math.max(x, float6 + num);
			float2.y = x.x;
			float3.y = x.y;
			float4.y = x.z;
			float5.y = x.w;
			float3 float8 = math.lerp(float4, float5, 0.5f);
			float3 float9 = math.lerp(float2, float3, 0.5f);
			float3 float10 = math.lerp(float4, float2, 0.5f);
			float3 float11 = math.lerp(float5, float3, 0.5f);
			position.y = math.lerp(float8.y, float9.y, 0.5f);
			float3 float12 = math.normalizesafe(float8 - float9, new float3(0f, 0f, 1f));
			float3 y = float11 - float10;
			float3 up = math.normalizesafe(math.cross(float12, y), new float3(0f, 1f, 0f));
			rotation = quaternion.LookRotationSafe(float12, up);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<WatercraftNavigation> __Game_Vehicles_WatercraftNavigation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WatercraftCurrentLane> __Game_Vehicles_WatercraftCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WatercraftData> __Game_Prefabs_WatercraftData_RO_ComponentLookup;

		public ComponentTypeHandle<Moving> __Game_Objects_Moving_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RW_ComponentTypeHandle;

		public BufferTypeHandle<TransformFrame> __Game_Objects_TransformFrame_RW_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Vehicles_WatercraftNavigation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WatercraftNavigation>(isReadOnly: true);
			__Game_Vehicles_WatercraftCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WatercraftCurrentLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_WatercraftData_RO_ComponentLookup = state.GetComponentLookup<WatercraftData>(isReadOnly: true);
			__Game_Objects_Moving_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Moving>();
			__Game_Objects_Transform_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>();
			__Game_Objects_TransformFrame_RW_BufferTypeHandle = state.GetBufferTypeHandle<TransformFrame>();
		}
	}

	private SimulationSystem m_SimulationSystem;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private WaterRenderSystem m_WaterRenderSystem;

	private EntityQuery m_WatercraftQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 8;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_WaterRenderSystem = base.World.GetOrCreateSystemManaged<WaterRenderSystem>();
		m_WatercraftQuery = GetEntityQuery(ComponentType.ReadOnly<Watercraft>(), ComponentType.ReadOnly<WatercraftNavigation>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<Transform>(), ComponentType.ReadWrite<Moving>(), ComponentType.ReadWrite<TransformFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<TripSource>());
		RequireForUpdate(m_WatercraftQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle deps;
		UpdateTransformDataJob jobData = new UpdateTransformDataJob
		{
			m_TransformFrameIndex = m_SimulationSystem.frameIndex / 16 % 4,
			m_NavigationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_WatercraftNavigation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_WatercraftCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabWatercraftData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WatercraftData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
			m_MovingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Moving_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformFrameType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_TransformFrame_RW_BufferTypeHandle, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_WatercraftQuery, JobHandle.CombineDependencies(base.Dependency, deps));
		m_TerrainSystem.AddCPUHeightReader(base.Dependency);
		m_WaterSystem.AddSurfaceReader(base.Dependency);
		m_WaterRenderSystem.AddHeightReader(base.Dependency);
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
	public WatercraftMoveSystem()
	{
	}
}
