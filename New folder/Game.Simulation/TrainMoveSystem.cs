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
public class TrainMoveSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateTransformDataJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Train> m_TrainType;

		[ReadOnly]
		public ComponentTypeHandle<TrainCurrentLane> m_CurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<TrainNavigation> m_NavigationType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<Moving> m_MovingType;

		public ComponentTypeHandle<Transform> m_TransformType;

		public BufferTypeHandle<TransformFrame> m_TransformFrameType;

		public BufferTypeHandle<TrainBogieFrame> m_BogieFrameType;

		[ReadOnly]
		public ComponentLookup<TrainData> m_PrefabTrainData;

		[ReadOnly]
		public uint m_TransformFrameIndex;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PrefabRef> nativeArray = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Train> nativeArray2 = chunk.GetNativeArray(ref m_TrainType);
			NativeArray<TrainCurrentLane> nativeArray3 = chunk.GetNativeArray(ref m_CurrentLaneType);
			NativeArray<TrainNavigation> nativeArray4 = chunk.GetNativeArray(ref m_NavigationType);
			NativeArray<Moving> nativeArray5 = chunk.GetNativeArray(ref m_MovingType);
			NativeArray<Transform> nativeArray6 = chunk.GetNativeArray(ref m_TransformType);
			BufferAccessor<TransformFrame> bufferAccessor = chunk.GetBufferAccessor(ref m_TransformFrameType);
			BufferAccessor<TrainBogieFrame> bufferAccessor2 = chunk.GetBufferAccessor(ref m_BogieFrameType);
			for (int i = 0; i < chunk.Count; i++)
			{
				PrefabRef prefabRef = nativeArray[i];
				Train train = nativeArray2[i];
				TrainCurrentLane trainCurrentLane = nativeArray3[i];
				TrainNavigation trainNavigation = nativeArray4[i];
				Moving value = nativeArray5[i];
				Transform transform = nativeArray6[i];
				TrainData prefabTrainData = m_PrefabTrainData[prefabRef.m_Prefab];
				VehicleUtils.CalculateTrainNavigationPivots(transform, prefabTrainData, out var pivot, out var pivot2);
				float3 value2 = trainNavigation.m_Rear.m_Position - trainNavigation.m_Front.m_Position;
				bool flag = (train.m_Flags & Game.Vehicles.TrainFlags.Reversed) != 0;
				if (flag)
				{
					CommonUtils.Swap(ref pivot, ref pivot2);
					prefabTrainData.m_BogieOffsets = prefabTrainData.m_BogieOffsets.yx;
				}
				if (!MathUtils.TryNormalize(ref value2, prefabTrainData.m_BogieOffsets.x))
				{
					value2 = transform.m_Position - pivot;
				}
				transform.m_Position = trainNavigation.m_Front.m_Position + value2;
				float3 value3 = math.select(-value2, value2, flag);
				if (MathUtils.TryNormalize(ref value3))
				{
					transform.m_Rotation = quaternion.LookRotationSafe(value3, math.up());
				}
				value.m_Velocity = trainNavigation.m_Front.m_Direction + trainNavigation.m_Rear.m_Direction;
				MathUtils.TryNormalize(ref value.m_Velocity, trainNavigation.m_Speed);
				TransformFrame value4 = new TransformFrame
				{
					m_Position = transform.m_Position,
					m_Velocity = value.m_Velocity,
					m_Rotation = transform.m_Rotation
				};
				TrainBogieFrame value5 = new TrainBogieFrame
				{
					m_FrontLane = trainCurrentLane.m_Front.m_Lane,
					m_RearLane = trainCurrentLane.m_Rear.m_Lane
				};
				DynamicBuffer<TransformFrame> dynamicBuffer = bufferAccessor[i];
				dynamicBuffer[(int)m_TransformFrameIndex] = value4;
				DynamicBuffer<TrainBogieFrame> dynamicBuffer2 = bufferAccessor2[i];
				dynamicBuffer2[(int)m_TransformFrameIndex] = value5;
				nativeArray5[i] = value;
				nativeArray6[i] = transform;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateLayoutDataJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PseudoRandomSeed> m_PseudoRandomSeedType;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> m_LayoutElementType;

		[ReadOnly]
		public ComponentLookup<Train> m_TrainData;

		[ReadOnly]
		public ComponentLookup<TrainCurrentLane> m_CurrentLaneData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<TransformFrame> m_TransformFrames;

		[ReadOnly]
		public uint m_TransformFrameIndex;

		[ReadOnly]
		public float m_DayLightBrightness;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PseudoRandomSeed> nativeArray = chunk.GetNativeArray(ref m_PseudoRandomSeedType);
			BufferAccessor<LayoutElement> bufferAccessor = chunk.GetBufferAccessor(ref m_LayoutElementType);
			for (int i = 0; i < chunk.Count; i++)
			{
				PseudoRandomSeed pseudoRandomSeed = nativeArray[i];
				DynamicBuffer<LayoutElement> dynamicBuffer = bufferAccessor[i];
				if (dynamicBuffer.Length == 0)
				{
					continue;
				}
				Entity vehicle = dynamicBuffer[0].m_Vehicle;
				Train train = m_TrainData[vehicle];
				DynamicBuffer<TransformFrame> dynamicBuffer2 = m_TransformFrames[vehicle];
				TrainCurrentLane trainCurrentLane = m_CurrentLaneData[vehicle];
				TransformFlags transformFlags = TransformFlags.InteriorLights;
				TransformFlags transformFlags2 = TransformFlags.InteriorLights;
				Random random = pseudoRandomSeed.GetRandom(PseudoRandomSeed.kLightState);
				if (m_DayLightBrightness + random.NextFloat(-0.05f, 0.05f) < 0.25f && (trainCurrentLane.m_Front.m_LaneFlags & TrainLaneFlags.HighBeams) != 0)
				{
					transformFlags |= TransformFlags.MainLights | TransformFlags.ExtraLights;
					transformFlags2 |= TransformFlags.MainLights | TransformFlags.ExtraLights;
				}
				else
				{
					transformFlags |= TransformFlags.MainLights;
					transformFlags2 |= TransformFlags.MainLights;
				}
				if ((trainCurrentLane.m_Front.m_LaneFlags & TrainLaneFlags.TurnLeft) != 0)
				{
					transformFlags |= TransformFlags.TurningLeft;
					transformFlags2 |= TransformFlags.TurningRight;
				}
				if ((trainCurrentLane.m_Front.m_LaneFlags & TrainLaneFlags.TurnRight) != 0)
				{
					transformFlags |= TransformFlags.TurningRight;
					transformFlags2 |= TransformFlags.TurningLeft;
				}
				if ((train.m_Flags & Game.Vehicles.TrainFlags.BoardingLeft) != 0)
				{
					transformFlags |= TransformFlags.BoardingLeft;
					transformFlags2 |= TransformFlags.BoardingRight;
				}
				if ((train.m_Flags & Game.Vehicles.TrainFlags.BoardingRight) != 0)
				{
					transformFlags |= TransformFlags.BoardingRight;
					transformFlags2 |= TransformFlags.BoardingLeft;
				}
				TransformFrame value = dynamicBuffer2[(int)m_TransformFrameIndex];
				TransformFlags flags = value.m_Flags;
				if ((train.m_Flags & Game.Vehicles.TrainFlags.Reversed) != 0)
				{
					value.m_Flags = transformFlags2;
				}
				else
				{
					value.m_Flags = transformFlags;
				}
				if ((train.m_Flags & Game.Vehicles.TrainFlags.Pantograph) != 0)
				{
					value.m_Flags |= TransformFlags.Pantograph;
				}
				if (((flags ^ value.m_Flags) & (TransformFlags.MainLights | TransformFlags.ExtraLights)) != 0)
				{
					TransformFlags transformFlags3 = (TransformFlags)0u;
					TransformFlags transformFlags4 = (TransformFlags)0u;
					for (int j = 0; j < dynamicBuffer2.Length; j++)
					{
						TransformFlags flags2 = dynamicBuffer2[j].m_Flags;
						transformFlags3 |= flags2;
						transformFlags4 |= ((j == (int)m_TransformFrameIndex) ? value.m_Flags : flags2);
					}
					if (((transformFlags3 ^ transformFlags4) & (TransformFlags.MainLights | TransformFlags.ExtraLights)) != 0)
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, vehicle, default(EffectsUpdated));
					}
				}
				dynamicBuffer2[(int)m_TransformFrameIndex] = value;
				transformFlags = (TransformFlags)((uint)transformFlags & 0xFFFFFFFCu);
				transformFlags2 = (TransformFlags)((uint)transformFlags2 & 0xFFFFFFFCu);
				for (int k = 1; k < dynamicBuffer.Length; k++)
				{
					Entity vehicle2 = dynamicBuffer[k].m_Vehicle;
					if (k == dynamicBuffer.Length - 1)
					{
						transformFlags |= TransformFlags.RearLights;
						transformFlags2 |= TransformFlags.RearLights;
					}
					Train train2 = m_TrainData[vehicle2];
					DynamicBuffer<TransformFrame> dynamicBuffer3 = m_TransformFrames[vehicle2];
					TransformFrame value2 = dynamicBuffer3[(int)m_TransformFrameIndex];
					TransformFlags flags3 = value2.m_Flags;
					if ((train2.m_Flags & Game.Vehicles.TrainFlags.Reversed) != 0)
					{
						value2.m_Flags = transformFlags2;
					}
					else
					{
						value2.m_Flags = transformFlags;
					}
					if ((train2.m_Flags & Game.Vehicles.TrainFlags.Pantograph) != 0)
					{
						value2.m_Flags |= TransformFlags.Pantograph;
					}
					if (((flags3 ^ value2.m_Flags) & (TransformFlags.MainLights | TransformFlags.ExtraLights)) != 0)
					{
						TransformFlags transformFlags5 = (TransformFlags)0u;
						TransformFlags transformFlags6 = (TransformFlags)0u;
						for (int l = 0; l < dynamicBuffer3.Length; l++)
						{
							TransformFlags flags4 = dynamicBuffer3[l].m_Flags;
							transformFlags5 |= flags4;
							transformFlags6 |= ((l == (int)m_TransformFrameIndex) ? value2.m_Flags : flags4);
						}
						if (((transformFlags5 ^ transformFlags6) & (TransformFlags.MainLights | TransformFlags.ExtraLights)) != 0)
						{
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, vehicle2, default(EffectsUpdated));
						}
					}
					dynamicBuffer3[(int)m_TransformFrameIndex] = value2;
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
		public ComponentTypeHandle<Train> __Game_Vehicles_Train_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TrainCurrentLane> __Game_Vehicles_TrainCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TrainNavigation> __Game_Vehicles_TrainNavigation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Moving> __Game_Objects_Moving_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RW_ComponentTypeHandle;

		public BufferTypeHandle<TransformFrame> __Game_Objects_TransformFrame_RW_BufferTypeHandle;

		public BufferTypeHandle<TrainBogieFrame> __Game_Vehicles_TrainBogieFrame_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<TrainData> __Game_Prefabs_TrainData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<PseudoRandomSeed> __Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Train> __Game_Vehicles_Train_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrainCurrentLane> __Game_Vehicles_TrainCurrentLane_RO_ComponentLookup;

		public BufferLookup<TransformFrame> __Game_Objects_TransformFrame_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Vehicles_Train_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Train>(isReadOnly: true);
			__Game_Vehicles_TrainCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TrainCurrentLane>(isReadOnly: true);
			__Game_Vehicles_TrainNavigation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TrainNavigation>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Objects_Moving_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Moving>();
			__Game_Objects_Transform_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>();
			__Game_Objects_TransformFrame_RW_BufferTypeHandle = state.GetBufferTypeHandle<TransformFrame>();
			__Game_Vehicles_TrainBogieFrame_RW_BufferTypeHandle = state.GetBufferTypeHandle<TrainBogieFrame>();
			__Game_Prefabs_TrainData_RO_ComponentLookup = state.GetComponentLookup<TrainData>(isReadOnly: true);
			__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PseudoRandomSeed>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<LayoutElement>(isReadOnly: true);
			__Game_Vehicles_Train_RO_ComponentLookup = state.GetComponentLookup<Train>(isReadOnly: true);
			__Game_Vehicles_TrainCurrentLane_RO_ComponentLookup = state.GetComponentLookup<TrainCurrentLane>(isReadOnly: true);
			__Game_Objects_TransformFrame_RW_BufferLookup = state.GetBufferLookup<TransformFrame>();
		}
	}

	private SimulationSystem m_SimulationSystem;

	private LightingSystem m_LightingSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_TrainQuery;

	private EntityQuery m_LayoutQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 3;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_LightingSystem = base.World.GetOrCreateSystemManaged<LightingSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_TrainQuery = GetEntityQuery(ComponentType.ReadOnly<Train>(), ComponentType.ReadOnly<TrainNavigation>(), ComponentType.ReadWrite<Transform>(), ComponentType.ReadWrite<Moving>(), ComponentType.ReadWrite<TransformFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<TripSource>());
		m_LayoutQuery = GetEntityQuery(ComponentType.ReadOnly<Train>(), ComponentType.ReadOnly<TrainNavigation>(), ComponentType.ReadOnly<LayoutElement>(), ComponentType.ReadWrite<TransformFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<TripSource>());
		RequireForUpdate(m_TrainQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint transformFrameIndex = m_SimulationSystem.frameIndex / 16 % 4;
		UpdateTransformDataJob jobData = new UpdateTransformDataJob
		{
			m_TrainType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Train_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_TrainCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NavigationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_TrainNavigation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MovingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Moving_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformFrameType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_TransformFrame_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_BogieFrameType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_TrainBogieFrame_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrainData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformFrameIndex = transformFrameIndex
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new UpdateLayoutDataJob
		{
			m_PseudoRandomSeedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_PseudoRandomSeed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LayoutElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_TrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Train_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_TrainCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformFrames = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_TransformFrame_RW_BufferLookup, ref base.CheckedStateRef),
			m_TransformFrameIndex = transformFrameIndex,
			m_DayLightBrightness = m_LightingSystem.dayLightBrightness,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, dependsOn: JobChunkExtensions.ScheduleParallel(jobData, m_TrainQuery, base.Dependency), query: m_LayoutQuery);
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
	public TrainMoveSystem()
	{
	}
}
