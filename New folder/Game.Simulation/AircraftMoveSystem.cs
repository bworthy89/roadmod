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
public class AircraftMoveSystem : GameSystemBase
{
	[BurstCompile]
	private struct AircraftMoveJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Aircraft> m_AircraftType;

		[ReadOnly]
		public ComponentTypeHandle<Helicopter> m_HelicopterType;

		[ReadOnly]
		public ComponentTypeHandle<AircraftNavigation> m_NavigationType;

		[ReadOnly]
		public ComponentTypeHandle<AircraftCurrentLane> m_CurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<PseudoRandomSeed> m_PseudoRandomSeedType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<Moving> m_MovingType;

		public ComponentTypeHandle<Transform> m_TransformType;

		public BufferTypeHandle<TransformFrame> m_TransformFrameType;

		[ReadOnly]
		public uint m_TransformFrameIndex;

		[ReadOnly]
		public float m_DayLightBrightness;

		[ReadOnly]
		public ComponentLookup<HelicopterData> m_PrefabHelicopterData;

		[ReadOnly]
		public ComponentLookup<AirplaneData> m_PrefabAirplaneData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Aircraft> nativeArray3 = chunk.GetNativeArray(ref m_AircraftType);
			NativeArray<AircraftNavigation> nativeArray4 = chunk.GetNativeArray(ref m_NavigationType);
			NativeArray<AircraftCurrentLane> nativeArray5 = chunk.GetNativeArray(ref m_CurrentLaneType);
			NativeArray<PseudoRandomSeed> nativeArray6 = chunk.GetNativeArray(ref m_PseudoRandomSeedType);
			NativeArray<Moving> nativeArray7 = chunk.GetNativeArray(ref m_MovingType);
			NativeArray<Transform> nativeArray8 = chunk.GetNativeArray(ref m_TransformType);
			BufferAccessor<TransformFrame> bufferAccessor = chunk.GetBufferAccessor(ref m_TransformFrameType);
			float num = 4f / 15f;
			if (chunk.Has(ref m_HelicopterType))
			{
				for (int i = 0; i < chunk.Count; i++)
				{
					PrefabRef prefabRef = nativeArray2[i];
					Aircraft aircraft = nativeArray3[i];
					AircraftNavigation aircraftNavigation = nativeArray4[i];
					AircraftCurrentLane aircraftCurrentLane = nativeArray5[i];
					PseudoRandomSeed pseudoRandomSeed = nativeArray6[i];
					Moving value = nativeArray7[i];
					Transform value2 = nativeArray8[i];
					Random random = pseudoRandomSeed.GetRandom(PseudoRandomSeed.kLightState);
					HelicopterData helicopterData = m_PrefabHelicopterData[prefabRef.m_Prefab];
					TransformFrame value3 = default(TransformFrame);
					if ((aircraftCurrentLane.m_LaneFlags & AircraftLaneFlags.Flying) != 0)
					{
						float3 x = aircraftNavigation.m_TargetPosition - value2.m_Position;
						x = math.normalizesafe(x);
						float num2 = math.asin(math.saturate(x.y));
						if (aircraftNavigation.m_MinClimbAngle > num2)
						{
							x.y = math.sin(aircraftNavigation.m_MinClimbAngle);
							x.xz = math.normalizesafe(x.xz) * math.cos(aircraftNavigation.m_MinClimbAngle);
						}
						x *= aircraftNavigation.m_MaxSpeed;
						float3 value4 = x - value.m_Velocity;
						value4 = MathUtils.ClampLength(value4, helicopterData.m_FlyingAcceleration * num);
						value.m_Velocity += value4;
						float3 x2 = new float3(0f, 0f, 1f);
						if (math.lengthsq(value.m_Velocity.xz) >= 1f)
						{
							x2.xz = value.m_Velocity.xz;
						}
						else
						{
							x2.xz = math.forward(value2.m_Rotation).xz;
						}
						float3 x3 = value.m_Velocity * helicopterData.m_VelocitySwayFactor * num + value4 * helicopterData.m_AccelerationSwayFactor;
						x3.y = math.max(x3.y, 0f) + 9.81f * num;
						x3 = math.normalize(x3);
						float3 y = math.cross(x2, x3);
						x2 = math.normalizesafe(math.cross(x3, y), new float3(0f, 0f, 1f));
						quaternion b = quaternion.LookRotationSafe(x2, x3);
						quaternion q = math.mul(math.inverse(value2.m_Rotation), b);
						MathUtils.AxisAngle(q, out var axis, out var angle);
						float3 valueToClamp = axis * angle * helicopterData.m_FlyingAngularAcceleration - value.m_AngularVelocity;
						valueToClamp = math.clamp(valueToClamp, (0f - helicopterData.m_FlyingAngularAcceleration) * num, helicopterData.m_FlyingAngularAcceleration * num);
						value.m_AngularVelocity += valueToClamp;
						float num3 = math.length(value.m_AngularVelocity);
						if (num3 > 1E-05f)
						{
							q = quaternion.AxisAngle(value.m_AngularVelocity / num3, num3 * num);
							value2.m_Rotation = math.normalize(math.mul(value2.m_Rotation, q));
						}
						float3 @float = value2.m_Position + value.m_Velocity * num;
						value3.m_Position = math.lerp(value2.m_Position, @float, 0.5f);
						value3.m_Velocity = value.m_Velocity;
						value3.m_Rotation = value2.m_Rotation;
						float num4 = m_DayLightBrightness + random.NextFloat(-0.05f, 0.05f);
						value3.m_Flags = TransformFlags.InteriorLights | TransformFlags.Flying;
						if ((aircraftCurrentLane.m_LaneFlags & AircraftLaneFlags.Landing) != 0)
						{
							value3.m_Flags |= TransformFlags.WarningLights | TransformFlags.Landing;
							if (num4 < 0.5f)
							{
								value3.m_Flags |= TransformFlags.ExtraLights;
							}
						}
						else if ((aircraftCurrentLane.m_LaneFlags & AircraftLaneFlags.TakingOff) != 0)
						{
							value3.m_Flags |= TransformFlags.WarningLights | TransformFlags.TakingOff;
						}
						if (num4 < 0.5f)
						{
							value3.m_Flags |= TransformFlags.MainLights;
						}
						if ((aircraft.m_Flags & AircraftFlags.Working) != 0)
						{
							value3.m_Flags |= TransformFlags.WorkLights;
						}
						value2.m_Position = @float;
					}
					else
					{
						float3 value5 = aircraftNavigation.m_TargetPosition - value2.m_Position;
						MathUtils.TryNormalize(ref value5, aircraftNavigation.m_MaxSpeed);
						float3 value6 = value5 * 8f + value.m_Velocity;
						MathUtils.TryNormalize(ref value6, aircraftNavigation.m_MaxSpeed);
						value.m_Velocity = value6;
						float3 float2 = value.m_Velocity * (num * 0.5f);
						float3 float3 = value2.m_Position + float2;
						float3 value7 = aircraftNavigation.m_TargetDirection;
						quaternion rotation = value2.m_Rotation;
						if (MathUtils.TryNormalize(ref value7))
						{
							rotation = quaternion.LookRotationSafe(value7, math.up());
						}
						else
						{
							float3 float4 = aircraftNavigation.m_TargetPosition - value2.m_Position;
							float num5 = math.length(float4);
							if (num5 >= 1f)
							{
								rotation = quaternion.LookRotationSafe(float4 / num5, math.up());
							}
						}
						value2.m_Rotation = rotation;
						value.m_AngularVelocity = default(float3);
						value3.m_Position = float3;
						value3.m_Velocity = value.m_Velocity;
						value3.m_Rotation = value2.m_Rotation;
						value3.m_Flags = TransformFlags.InteriorLights | TransformFlags.WarningLights;
						value2.m_Position = float3 + float2;
					}
					DynamicBuffer<TransformFrame> dynamicBuffer = bufferAccessor[i];
					if (((dynamicBuffer[(int)m_TransformFrameIndex].m_Flags ^ value3.m_Flags) & (TransformFlags.MainLights | TransformFlags.ExtraLights | TransformFlags.WorkLights | TransformFlags.TakingOff | TransformFlags.Landing | TransformFlags.Flying)) != 0)
					{
						TransformFlags transformFlags = (TransformFlags)0u;
						TransformFlags transformFlags2 = (TransformFlags)0u;
						for (int j = 0; j < dynamicBuffer.Length; j++)
						{
							TransformFlags flags = dynamicBuffer[j].m_Flags;
							transformFlags |= flags;
							transformFlags2 |= ((j == (int)m_TransformFrameIndex) ? value3.m_Flags : flags);
						}
						if (((transformFlags ^ transformFlags2) & (TransformFlags.MainLights | TransformFlags.ExtraLights | TransformFlags.WorkLights | TransformFlags.TakingOff | TransformFlags.Landing | TransformFlags.Flying)) != 0)
						{
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, nativeArray[i], default(EffectsUpdated));
						}
					}
					dynamicBuffer[(int)m_TransformFrameIndex] = value3;
					nativeArray7[i] = value;
					nativeArray8[i] = value2;
				}
				return;
			}
			for (int k = 0; k < chunk.Count; k++)
			{
				PrefabRef prefabRef2 = nativeArray2[k];
				AircraftNavigation aircraftNavigation2 = nativeArray4[k];
				AircraftCurrentLane aircraftCurrentLane2 = nativeArray5[k];
				PseudoRandomSeed pseudoRandomSeed2 = nativeArray6[k];
				Moving value8 = nativeArray7[k];
				Transform value9 = nativeArray8[k];
				Random random2 = pseudoRandomSeed2.GetRandom(PseudoRandomSeed.kLightState);
				AirplaneData airplaneData = m_PrefabAirplaneData[prefabRef2.m_Prefab];
				TransformFrame value10 = default(TransformFrame);
				if ((aircraftCurrentLane2.m_LaneFlags & AircraftLaneFlags.Flying) != 0)
				{
					float3 float5 = math.normalizesafe(value8.m_Velocity, new float3(0f, 0f, 1f));
					float2 x4 = math.normalizesafe(value8.m_Velocity.xz, new float2(0f, 1f));
					float2 value11 = aircraftNavigation2.m_TargetDirection.xz;
					float3 value12 = aircraftNavigation2.m_TargetPosition - value9.m_Position;
					float num6 = aircraftNavigation2.m_MaxSpeed / airplaneData.m_FlyingTurning;
					if (MathUtils.TryNormalize(ref value11))
					{
						float num7 = math.dot(x4, value11);
						float2 float6 = MathUtils.Right(value11);
						float num8 = math.dot(float6, value12.xz);
						if (math.dot(value12.xz, value11) >= 0f)
						{
							num7 = math.max(num7, 0f);
							if (math.abs(num8) <= num6)
							{
								num7 = math.max(num7, 1f);
							}
						}
						float6 = math.select(float6, -float6, num8 > 0f);
						value12.xz += float6 * (num6 * (1f - num7));
						value12.xz -= value11 * (num6 * (1f - math.abs(num7)));
					}
					if (MathUtils.TryNormalize(ref value12))
					{
						float num9 = math.asin(math.saturate(value12.y));
						if (aircraftNavigation2.m_MinClimbAngle > num9)
						{
							value12.y = math.sin(aircraftNavigation2.m_MinClimbAngle);
							value12.xz = math.normalizesafe(value12.xz) * math.cos(aircraftNavigation2.m_MinClimbAngle);
						}
					}
					float x5 = math.acos(math.clamp(math.dot(value12, float5), -1f, 1f));
					x5 = math.min(x5, airplaneData.m_FlyingTurning * num);
					float3 float7 = math.normalizesafe(value12 - float5 * math.dot(float5, value12), new float3(float5.zy, 0f - float5.x));
					float3 float8 = float5 * math.cos(x5) + float7 * math.sin(x5);
					float3 velocity = value8.m_Velocity;
					value8.m_Velocity = float8 * aircraftNavigation2.m_MaxSpeed;
					float num10 = (aircraftNavigation2.m_MaxSpeed - airplaneData.m_FlyingSpeed.x) / (airplaneData.m_FlyingSpeed.y - airplaneData.m_FlyingSpeed.x);
					num10 = math.saturate(1f - num10);
					num10 *= num10;
					float x6 = math.lerp(0f - airplaneData.m_ClimbAngle, airplaneData.m_SlowPitchAngle, num10);
					float num11 = math.sin(x6);
					float3 float9 = float8;
					if (float9.y < num11)
					{
						float9.y = num11;
						float9.xz = math.normalizesafe(float9.xz, new float2(0f, 1f)) * math.cos(x6);
					}
					float3 float10 = new float3
					{
						xz = math.normalizesafe(MathUtils.Right(float9.xz))
					};
					float3 x7 = float10 * (math.dot(value8.m_Velocity - velocity, float10) * airplaneData.m_TurningRollFactor);
					x7.y = math.max(x7.y, 0f) + 9.81f * num;
					x7 = math.normalize(x7);
					float10 = math.cross(float9, x7);
					x7 = math.normalizesafe(math.cross(float10, float9), new float3(0f, 1f, 0f));
					quaternion b2 = quaternion.LookRotationSafe(float9, x7);
					quaternion q2 = math.mul(math.inverse(value9.m_Rotation), b2);
					MathUtils.AxisAngle(q2, out var axis2, out var angle2);
					float3 valueToClamp2 = axis2 * angle2 * airplaneData.m_FlyingAngularAcceleration - value8.m_AngularVelocity;
					valueToClamp2 = math.clamp(valueToClamp2, (0f - airplaneData.m_FlyingAngularAcceleration) * num, airplaneData.m_FlyingAngularAcceleration * num);
					value8.m_AngularVelocity += valueToClamp2;
					float num12 = math.length(value8.m_AngularVelocity);
					if (num12 > 1E-05f)
					{
						q2 = quaternion.AxisAngle(value8.m_AngularVelocity / num12, num12 * num);
						value9.m_Rotation = math.normalize(math.mul(value9.m_Rotation, q2));
					}
					float3 float11 = value9.m_Position + value8.m_Velocity * num;
					value10.m_Position = math.lerp(value9.m_Position, float11, 0.5f);
					value10.m_Velocity = value8.m_Velocity;
					value10.m_Rotation = value9.m_Rotation;
					value10.m_Flags = TransformFlags.InteriorLights | TransformFlags.Flying;
					if ((aircraftCurrentLane2.m_LaneFlags & AircraftLaneFlags.Landing) != 0)
					{
						value10.m_Flags |= TransformFlags.WarningLights | TransformFlags.Landing;
						if (m_DayLightBrightness + random2.NextFloat(-0.05f, 0.05f) < 0.5f)
						{
							value10.m_Flags |= TransformFlags.ExtraLights;
						}
					}
					else if ((aircraftCurrentLane2.m_LaneFlags & AircraftLaneFlags.TakingOff) != 0)
					{
						value10.m_Flags |= TransformFlags.WarningLights | TransformFlags.TakingOff;
					}
					value9.m_Position = float11;
				}
				else
				{
					float3 value13 = aircraftNavigation2.m_TargetPosition - value9.m_Position;
					MathUtils.TryNormalize(ref value13, aircraftNavigation2.m_MaxSpeed);
					float3 value14 = value13 * 8f + value8.m_Velocity;
					MathUtils.TryNormalize(ref value14, aircraftNavigation2.m_MaxSpeed);
					value8.m_Velocity = value14;
					float3 float12 = value8.m_Velocity * (num * 0.5f);
					float3 float13 = value9.m_Position + float12;
					float3 value15 = aircraftNavigation2.m_TargetDirection;
					quaternion quaternion = value9.m_Rotation;
					if (MathUtils.TryNormalize(ref value15))
					{
						quaternion = quaternion.LookRotationSafe(value15, math.up());
					}
					else
					{
						float3 value16 = aircraftNavigation2.m_TargetPosition - value9.m_Position;
						if (MathUtils.TryNormalize(ref value16))
						{
							quaternion = quaternion.LookRotationSafe(value16, math.up());
						}
					}
					if (aircraftNavigation2.m_MaxSpeed > airplaneData.m_FlyingSpeed.x * 0.9f)
					{
						quaternion q3 = math.mul(math.inverse(value9.m_Rotation), quaternion);
						MathUtils.AxisAngle(q3, out var axis3, out var angle3);
						float3 valueToClamp3 = axis3 * angle3 * airplaneData.m_FlyingAngularAcceleration - value8.m_AngularVelocity;
						valueToClamp3 = math.clamp(valueToClamp3, (0f - airplaneData.m_FlyingAngularAcceleration) * num, airplaneData.m_FlyingAngularAcceleration * num);
						value8.m_AngularVelocity += valueToClamp3;
						float num13 = math.length(value8.m_AngularVelocity);
						if (num13 > 1E-05f)
						{
							q3 = quaternion.AxisAngle(value8.m_AngularVelocity / num13, num13 * num);
							value9.m_Rotation = math.normalize(math.mul(value9.m_Rotation, q3));
						}
					}
					else
					{
						value9.m_Rotation = quaternion;
						value8.m_AngularVelocity = default(float3);
					}
					value10.m_Position = float13;
					value10.m_Velocity = value8.m_Velocity;
					value10.m_Rotation = value9.m_Rotation;
					value10.m_Flags = TransformFlags.InteriorLights | TransformFlags.WarningLights;
					if ((aircraftCurrentLane2.m_LaneFlags & AircraftLaneFlags.Landing) != 0)
					{
						value10.m_Flags |= TransformFlags.Landing;
					}
					else if ((aircraftCurrentLane2.m_LaneFlags & AircraftLaneFlags.TakingOff) != 0)
					{
						value10.m_Flags |= TransformFlags.TakingOff;
					}
					if (m_DayLightBrightness + random2.NextFloat(-0.05f, 0.05f) < 0.5f)
					{
						value10.m_Flags |= TransformFlags.MainLights;
					}
					value9.m_Position = float13 + float12;
				}
				DynamicBuffer<TransformFrame> dynamicBuffer2 = bufferAccessor[k];
				if (((dynamicBuffer2[(int)m_TransformFrameIndex].m_Flags ^ value10.m_Flags) & (TransformFlags.MainLights | TransformFlags.ExtraLights | TransformFlags.TakingOff | TransformFlags.Landing | TransformFlags.Flying)) != 0)
				{
					TransformFlags transformFlags3 = (TransformFlags)0u;
					TransformFlags transformFlags4 = (TransformFlags)0u;
					for (int l = 0; l < dynamicBuffer2.Length; l++)
					{
						TransformFlags flags2 = dynamicBuffer2[l].m_Flags;
						transformFlags3 |= flags2;
						transformFlags4 |= ((l == (int)m_TransformFrameIndex) ? value10.m_Flags : flags2);
					}
					if (((transformFlags3 ^ transformFlags4) & (TransformFlags.MainLights | TransformFlags.ExtraLights | TransformFlags.TakingOff | TransformFlags.Landing | TransformFlags.Flying)) != 0)
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, nativeArray[k], default(EffectsUpdated));
					}
				}
				dynamicBuffer2[(int)m_TransformFrameIndex] = value10;
				nativeArray7[k] = value8;
				nativeArray8[k] = value9;
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
		public ComponentTypeHandle<Aircraft> __Game_Vehicles_Aircraft_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Helicopter> __Game_Vehicles_Helicopter_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<AircraftNavigation> __Game_Vehicles_AircraftNavigation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<AircraftCurrentLane> __Game_Vehicles_AircraftCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RW_ComponentTypeHandle;

		public BufferTypeHandle<TransformFrame> __Game_Objects_TransformFrame_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<HelicopterData> __Game_Prefabs_HelicopterData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AirplaneData> __Game_Prefabs_AirplaneData_RO_ComponentLookup;

		public ComponentTypeHandle<Moving> __Game_Objects_Moving_RW_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Vehicles_Aircraft_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Aircraft>(isReadOnly: true);
			__Game_Vehicles_Helicopter_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Helicopter>(isReadOnly: true);
			__Game_Vehicles_AircraftNavigation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AircraftNavigation>(isReadOnly: true);
			__Game_Vehicles_AircraftCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AircraftCurrentLane>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PseudoRandomSeed>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Objects_Transform_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>();
			__Game_Objects_TransformFrame_RW_BufferTypeHandle = state.GetBufferTypeHandle<TransformFrame>();
			__Game_Prefabs_HelicopterData_RO_ComponentLookup = state.GetComponentLookup<HelicopterData>(isReadOnly: true);
			__Game_Prefabs_AirplaneData_RO_ComponentLookup = state.GetComponentLookup<AirplaneData>(isReadOnly: true);
			__Game_Objects_Moving_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Moving>();
		}
	}

	private SimulationSystem m_SimulationSystem;

	private LightingSystem m_LightingSystem;

	private EntityQuery m_AircraftQuery;

	private EndFrameBarrier m_EndFrameBarrier;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 10;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_LightingSystem = base.World.GetOrCreateSystemManaged<LightingSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_AircraftQuery = GetEntityQuery(ComponentType.ReadOnly<Aircraft>(), ComponentType.ReadOnly<AircraftNavigation>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<Transform>(), ComponentType.ReadWrite<Moving>(), ComponentType.ReadWrite<TransformFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<TripSource>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new AircraftMoveJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_AircraftType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Aircraft_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HelicopterType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Helicopter_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NavigationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_AircraftNavigation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_AircraftCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PseudoRandomSeedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformFrameType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_TransformFrame_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabHelicopterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_HelicopterData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabAirplaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AirplaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MovingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Moving_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformFrameIndex = m_SimulationSystem.frameIndex / 16 % 4,
			m_DayLightBrightness = m_LightingSystem.dayLightBrightness,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_AircraftQuery, base.Dependency);
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
	public AircraftMoveSystem()
	{
	}
}
