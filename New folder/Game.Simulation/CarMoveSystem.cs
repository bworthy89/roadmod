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
public class CarMoveSystem : GameSystemBase
{
	[BurstCompile]
	private struct CarMoveJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Car> m_CarType;

		[ReadOnly]
		public ComponentTypeHandle<CarNavigation> m_NavigationType;

		[ReadOnly]
		public ComponentTypeHandle<CarCurrentLane> m_CurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<PseudoRandomSeed> m_PseudoRandomSeedType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<Moving> m_MovingType;

		public ComponentTypeHandle<Transform> m_TransformType;

		public BufferTypeHandle<TransformFrame> m_TransformFrameType;

		[ReadOnly]
		public ComponentLookup<CarData> m_PrefabCarData;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_Layouts;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public uint m_TransformFrameIndex;

		[ReadOnly]
		public float m_DayLightBrightness;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Car> nativeArray3 = chunk.GetNativeArray(ref m_CarType);
			NativeArray<CarNavigation> nativeArray4 = chunk.GetNativeArray(ref m_NavigationType);
			NativeArray<CarCurrentLane> nativeArray5 = chunk.GetNativeArray(ref m_CurrentLaneType);
			NativeArray<PseudoRandomSeed> nativeArray6 = chunk.GetNativeArray(ref m_PseudoRandomSeedType);
			NativeArray<Moving> nativeArray7 = chunk.GetNativeArray(ref m_MovingType);
			NativeArray<Transform> nativeArray8 = chunk.GetNativeArray(ref m_TransformType);
			BufferAccessor<TransformFrame> bufferAccessor = chunk.GetBufferAccessor(ref m_TransformFrameType);
			float num = 4f / 15f;
			float num2 = num * 0.5f;
			for (int i = 0; i < chunk.Count; i++)
			{
				PrefabRef prefabRef = nativeArray2[i];
				Car car = nativeArray3[i];
				CarNavigation carNavigation = nativeArray4[i];
				CarCurrentLane carCurrentLane = nativeArray5[i];
				PseudoRandomSeed pseudoRandomSeed = nativeArray6[i];
				Moving value = nativeArray7[i];
				Transform value2 = nativeArray8[i];
				CarData carData = m_PrefabCarData[prefabRef.m_Prefab];
				Random random = pseudoRandomSeed.GetRandom(PseudoRandomSeed.kLightState);
				float3 value3 = carNavigation.m_TargetPosition - value2.m_Position;
				bool flag = math.asuint(carNavigation.m_MaxSpeed) >> 31 != 0;
				bool flag2 = !carNavigation.m_TargetRotation.Equals(default(quaternion));
				float num3 = math.abs(carNavigation.m_MaxSpeed);
				if (flag && (car.m_Flags & CarFlags.CannotReverse) != 0)
				{
					flag = false;
					carNavigation.m_MaxSpeed = 0f;
					ForceFullUpdate(unfilteredChunkIndex, nativeArray[i]);
				}
				if ((carCurrentLane.m_LaneFlags & CarLaneFlags.Area) != 0)
				{
					if (!flag)
					{
						float y = math.select(carData.m_PivotOffset, 0f - carData.m_PivotOffset, flag);
						value3.xz = MathUtils.ClampLength(value3.xz, math.max(1f, num3) + math.max(0f, y));
						carNavigation.m_TargetPosition.xz = value2.m_Position.xz + value3.xz;
					}
					carNavigation.m_TargetPosition.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, carNavigation.m_TargetPosition);
					value3.y = carNavigation.m_TargetPosition.y - value2.m_Position.y;
					MathUtils.TryNormalize(ref value3, num3);
				}
				else
				{
					MathUtils.TryNormalize(ref value3, num3);
				}
				float3 value4;
				if ((carCurrentLane.m_LaneFlags & (CarLaneFlags.Connection | CarLaneFlags.ResetSpeed)) != 0 || flag2)
				{
					value4 = value3;
				}
				else
				{
					float3 @float = math.rotate(value2.m_Rotation, math.right());
					float3 float2 = math.rotate(value2.m_Rotation, math.up());
					float3 float3 = math.forward(value2.m_Rotation);
					float3 value5 = new float3(math.dot(@float, value3), math.dot(float2, value3), math.dot(float3, value3));
					float num4 = math.min(1f, math.abs(carData.m_PivotOffset));
					float num5 = math.saturate(math.distance(carNavigation.m_TargetPosition, value2.m_Position) - num4);
					if (flag)
					{
						float falseValue = math.smoothstep(-1f, -0.9f, math.dot(float3, math.normalizesafe(value3)));
						falseValue = math.select(falseValue, 1f, (carCurrentLane.m_LaneFlags & CarLaneFlags.CanReverse) == 0);
						float3 float4 = MathUtils.Normalize(value5, value5.xz);
						value5.x = 1.5f * falseValue * num5 * value5.x;
						value5.z = num5 * (math.max(0f, value5.z) + 1f + math.max(1f, carData.m_PivotOffset * -4f));
						value5.y = float4.y * math.dot(value5.xz, float4.xz);
						value3 -= @float * value5.x + float2 * value5.y + float3 * value5.z;
						if ((carCurrentLane.m_LaneFlags & CarLaneFlags.Area) != 0)
						{
							carNavigation.m_TargetPosition.xz = value2.m_Position.xz + value3.xz;
							carNavigation.m_TargetPosition.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, carNavigation.m_TargetPosition);
							value3.y = carNavigation.m_TargetPosition.y - value2.m_Position.y;
						}
						value4 = value3 * 8f + value.m_Velocity;
						if (math.dot(value3, float3) < 0f)
						{
							value3 = -value3;
						}
						carNavigation.m_TargetPosition = value2.m_Position + value3;
					}
					else
					{
						float num6 = math.acos(math.normalizesafe(value5.xz).y);
						float num7 = num3 * num / math.max(1f, 0f - carData.m_PivotOffset);
						if (num6 > num7)
						{
							float3 float5 = MathUtils.Normalize(value5, value5.xz);
							value5.xz = new float2(math.sin(num7 * math.sign(value5.x)), math.cos(num7)) - value5.xz;
							value5.xz *= num5;
							value5.y = float5.y * math.dot(value5.xz, float5.xz);
							value3 += @float * value5.x + float2 * value5.y + float3 * value5.z;
							if ((carCurrentLane.m_LaneFlags & CarLaneFlags.Area) != 0)
							{
								carNavigation.m_TargetPosition.xz = value2.m_Position.xz + value3.xz;
								carNavigation.m_TargetPosition.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, carNavigation.m_TargetPosition);
								value3.y = carNavigation.m_TargetPosition.y - value2.m_Position.y;
							}
						}
						value4 = value3 * 8f + value.m_Velocity;
						carNavigation.m_TargetPosition = value2.m_Position + value3;
					}
				}
				MathUtils.TryNormalize(ref value4, num3);
				float num8 = math.length(value.m_Velocity);
				float num9 = math.length(value4);
				value.m_Velocity = value4;
				float3 float6 = value.m_Velocity * num2;
				float3 float7 = value2.m_Position + float6;
				quaternion quaternion = value2.m_Rotation;
				quaternion rotation = value2.m_Rotation;
				if (flag2)
				{
					quaternion = carNavigation.m_TargetRotation;
					rotation = quaternion;
				}
				else if (num9 > 0.01f)
				{
					float z = math.min(0f, carData.m_PivotOffset);
					float3 float8 = value2.m_Position + math.rotate(value2.m_Rotation, new float3(0f, 0f, z));
					float3 value6 = carNavigation.m_TargetPosition - float8;
					if (MathUtils.TryNormalize(ref value6))
					{
						quaternion = quaternion.LookRotationSafe(value6, math.up());
						rotation = quaternion;
					}
					float8 = float7 + math.rotate(quaternion, new float3(0f, 0f, z));
					value6 = carNavigation.m_TargetPosition - float8;
					if (MathUtils.TryNormalize(ref value6))
					{
						rotation = quaternion.LookRotationSafe(value6, math.up());
					}
				}
				TransformFrame value7 = new TransformFrame
				{
					m_Position = float7,
					m_Velocity = value.m_Velocity,
					m_Rotation = quaternion
				};
				value7.m_Flags |= TransformFlags.RearLights;
				float num10 = m_DayLightBrightness + random.NextFloat(-0.05f, 0.05f);
				if (num10 < 0.5f)
				{
					if (num10 < 0.25f && (carCurrentLane.m_LaneFlags & CarLaneFlags.HighBeams) != 0)
					{
						value7.m_Flags |= TransformFlags.ExtraLights;
					}
					else
					{
						value7.m_Flags |= TransformFlags.MainLights;
					}
				}
				if (num9 <= num8 * (1f - num / (1f + num8)) + 0.1f * num)
				{
					value7.m_Flags |= TransformFlags.Braking;
				}
				if (flag)
				{
					value7.m_Flags |= TransformFlags.Reversing;
				}
				else
				{
					if ((carCurrentLane.m_LaneFlags & CarLaneFlags.TurnLeft) != 0)
					{
						value7.m_Flags |= TransformFlags.TurningLeft;
					}
					if ((carCurrentLane.m_LaneFlags & CarLaneFlags.TurnRight) != 0)
					{
						value7.m_Flags |= TransformFlags.TurningRight;
					}
				}
				if ((car.m_Flags & (CarFlags.Emergency | CarFlags.Warning)) != 0)
				{
					value7.m_Flags |= TransformFlags.WarningLights;
				}
				if ((car.m_Flags & (CarFlags.Sign | CarFlags.Working)) != 0)
				{
					value7.m_Flags |= TransformFlags.WorkLights;
				}
				if ((car.m_Flags & CarFlags.Interior) != 0)
				{
					value7.m_Flags |= TransformFlags.InteriorLights;
				}
				if ((car.m_Flags & (CarFlags.SignalAnimation1 | CarFlags.SignalAnimation2)) != 0)
				{
					value7.m_Flags |= (TransformFlags)(((car.m_Flags & CarFlags.SignalAnimation1) != 0) ? 8192 : 0);
					value7.m_Flags |= (TransformFlags)(((car.m_Flags & CarFlags.SignalAnimation2) != 0) ? 16384 : 0);
				}
				value2.m_Position = float7 + float6;
				value2.m_Rotation = rotation;
				DynamicBuffer<TransformFrame> dynamicBuffer = bufferAccessor[i];
				if (((dynamicBuffer[(int)m_TransformFrameIndex].m_Flags ^ value7.m_Flags) & (TransformFlags.MainLights | TransformFlags.ExtraLights | TransformFlags.WarningLights | TransformFlags.WorkLights)) != 0)
				{
					TransformFlags transformFlags = (TransformFlags)0u;
					TransformFlags transformFlags2 = (TransformFlags)0u;
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						TransformFlags flags = dynamicBuffer[j].m_Flags;
						transformFlags |= flags;
						transformFlags2 |= ((j == (int)m_TransformFrameIndex) ? value7.m_Flags : flags);
					}
					if (((transformFlags ^ transformFlags2) & (TransformFlags.MainLights | TransformFlags.ExtraLights | TransformFlags.WarningLights | TransformFlags.WorkLights)) != 0)
					{
						EffectsUpdated(unfilteredChunkIndex, nativeArray[i]);
					}
				}
				dynamicBuffer[(int)m_TransformFrameIndex] = value7;
				nativeArray7[i] = value;
				nativeArray8[i] = value2;
			}
		}

		private void EffectsUpdated(int jobIndex, Entity entity)
		{
			if (m_Layouts.TryGetBuffer(entity, out var bufferData) && bufferData.Length != 0)
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					m_CommandBuffer.AddComponent(jobIndex, bufferData[i].m_Vehicle, default(EffectsUpdated));
				}
			}
			else
			{
				m_CommandBuffer.AddComponent(jobIndex, entity, default(EffectsUpdated));
			}
		}

		private void ForceFullUpdate(int jobIndex, Entity entity)
		{
			if (m_Layouts.TryGetBuffer(entity, out var bufferData) && bufferData.Length != 0)
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					UpdateVehicle(jobIndex, bufferData[i].m_Vehicle);
				}
			}
			else
			{
				UpdateVehicle(jobIndex, entity);
			}
		}

		private void UpdateVehicle(int jobIndex, Entity entity)
		{
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Updated));
			UpdateSubObjectBatches(jobIndex, entity);
		}

		private void UpdateSubObjectBatches(int jobIndex, Entity entity)
		{
			if (m_SubObjects.TryGetBuffer(entity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					Entity subObject = bufferData[i].m_SubObject;
					m_CommandBuffer.AddComponent(jobIndex, subObject, default(BatchesUpdated));
					UpdateSubObjectBatches(jobIndex, subObject);
				}
			}
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
		public ComponentTypeHandle<Car> __Game_Vehicles_Car_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CarNavigation> __Game_Vehicles_CarNavigation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Moving> __Game_Objects_Moving_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RW_ComponentTypeHandle;

		public BufferTypeHandle<TransformFrame> __Game_Objects_TransformFrame_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Vehicles_Car_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Car>(isReadOnly: true);
			__Game_Vehicles_CarNavigation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CarNavigation>(isReadOnly: true);
			__Game_Vehicles_CarCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CarCurrentLane>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PseudoRandomSeed>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Objects_Moving_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Moving>();
			__Game_Objects_Transform_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>();
			__Game_Objects_TransformFrame_RW_BufferTypeHandle = state.GetBufferTypeHandle<TransformFrame>();
			__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
		}
	}

	private SimulationSystem m_SimulationSystem;

	private TerrainSystem m_TerrainSystem;

	private LightingSystem m_LightingSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_VehicleQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_LightingSystem = base.World.GetOrCreateSystemManaged<LightingSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_VehicleQuery = GetEntityQuery(ComponentType.ReadOnly<Car>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<TransformFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<TripSource>(), ComponentType.Exclude<OutOfControl>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint index = m_SimulationSystem.frameIndex & 0xF;
		uint transformFrameIndex = (m_SimulationSystem.frameIndex >> 4) & 3;
		m_VehicleQuery.ResetFilter();
		m_VehicleQuery.SetSharedComponentFilter(new UpdateFrame(index));
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new CarMoveJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CarType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Car_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NavigationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_CarNavigation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PseudoRandomSeedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MovingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Moving_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformFrameType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_TransformFrame_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Layouts = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_TransformFrameIndex = transformFrameIndex,
			m_DayLightBrightness = m_LightingSystem.dayLightBrightness,
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_VehicleQuery, base.Dependency);
		m_TerrainSystem.AddCPUHeightReader(jobHandle);
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
	public CarMoveSystem()
	{
	}
}
