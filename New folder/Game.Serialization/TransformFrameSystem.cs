using System.Runtime.CompilerServices;
using Game.Creatures;
using Game.Objects;
using Game.Rendering;
using Game.Simulation;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class TransformFrameSystem : GameSystemBase
{
	[BurstCompile]
	private struct TransformFrameJob : IJobChunk
	{
		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Moving> m_MovingType;

		[ReadOnly]
		public ComponentTypeHandle<HumanNavigation> m_HumanNavigationType;

		[ReadOnly]
		public ComponentTypeHandle<AnimalNavigation> m_AnimalNavigationType;

		[ReadOnly]
		public ComponentTypeHandle<Train> m_TrainType;

		public ComponentTypeHandle<InterpolatedTransform> m_InterpolatedTransformType;

		public BufferTypeHandle<TransformFrame> m_TransformFrameType;

		[ReadOnly]
		public uint m_SimulationFrameIndex;

		private const float SECONDS_PER_FRAME = 4f / 15f;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Transform> nativeArray = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<Moving> nativeArray2 = chunk.GetNativeArray(ref m_MovingType);
			NativeArray<HumanNavigation> nativeArray3 = chunk.GetNativeArray(ref m_HumanNavigationType);
			NativeArray<AnimalNavigation> nativeArray4 = chunk.GetNativeArray(ref m_AnimalNavigationType);
			NativeArray<InterpolatedTransform> nativeArray5 = chunk.GetNativeArray(ref m_InterpolatedTransformType);
			NativeArray<Train> nativeArray6 = chunk.GetNativeArray(ref m_TrainType);
			BufferAccessor<TransformFrame> bufferAccessor = chunk.GetBufferAccessor(ref m_TransformFrameType);
			uint index = chunk.GetSharedComponent(m_UpdateFrameType).m_Index;
			uint updateFrameOffset = (m_SimulationFrameIndex - index) / 16;
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Transform transform = nativeArray[i];
				nativeArray5[i] = new InterpolatedTransform(transform);
				DynamicBuffer<TransformFrame> transformFrames = bufferAccessor[i];
				bool flag = transformFrames.Length != 4;
				if (flag)
				{
					transformFrames.ResizeUninitialized(4);
				}
				TransformFlags transformFlags = (TransformFlags)0u;
				if (nativeArray6.Length != 0 && (nativeArray6[i].m_Flags & TrainFlags.Pantograph) != 0)
				{
					transformFlags |= TransformFlags.Pantograph;
				}
				if (nativeArray2.Length != 0)
				{
					Moving moving = nativeArray2[i];
					if (nativeArray3.Length != 0)
					{
						HumanNavigation humanNavigation = nativeArray3[i];
						InitTransformFrames(transform, moving, humanNavigation.m_TransformState, transformFlags, humanNavigation.m_LastActivity, transformFrames, updateFrameOffset, flag);
					}
					else if (nativeArray4.Length != 0)
					{
						AnimalNavigation animalNavigation = nativeArray4[i];
						InitTransformFrames(transform, moving, animalNavigation.m_TransformState, transformFlags, animalNavigation.m_LastActivity, transformFrames, updateFrameOffset, flag);
					}
					else
					{
						InitTransformFrames(transform, moving, TransformState.Default, transformFlags, 0, transformFrames, updateFrameOffset, flag);
					}
				}
				else
				{
					for (int j = 0; j < transformFrames.Length; j++)
					{
						transformFrames[j] = new TransformFrame(transform)
						{
							m_Flags = transformFlags
						};
					}
				}
			}
		}

		private void InitTransformFrames(Transform transform, Moving moving, TransformState state, TransformFlags flags, byte activity, DynamicBuffer<TransformFrame> transformFrames, uint updateFrameOffset, bool fullReset)
		{
			for (int i = 0; i < transformFrames.Length; i++)
			{
				TransformFrame value;
				if (fullReset)
				{
					value = new TransformFrame(transform, moving)
					{
						m_State = state,
						m_Flags = flags,
						m_Activity = activity
					};
				}
				else
				{
					value = transformFrames[i];
					value.m_Position = transform.m_Position;
					value.m_Velocity = moving.m_Velocity;
					value.m_Rotation = transform.m_Rotation;
				}
				float num = (float)((uint)((int)updateFrameOffset - i) % 4u) * (4f / 15f) + 2f / 15f;
				value.m_Position -= moving.m_Velocity * num;
				transformFrames[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Moving> __Game_Objects_Moving_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HumanNavigation> __Game_Creatures_HumanNavigation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<AnimalNavigation> __Game_Creatures_AnimalNavigation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Train> __Game_Vehicles_Train_RO_ComponentTypeHandle;

		public ComponentTypeHandle<InterpolatedTransform> __Game_Rendering_InterpolatedTransform_RW_ComponentTypeHandle;

		public BufferTypeHandle<TransformFrame> __Game_Objects_TransformFrame_RW_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Objects_Moving_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Moving>(isReadOnly: true);
			__Game_Creatures_HumanNavigation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HumanNavigation>(isReadOnly: true);
			__Game_Creatures_AnimalNavigation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AnimalNavigation>(isReadOnly: true);
			__Game_Vehicles_Train_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Train>(isReadOnly: true);
			__Game_Rendering_InterpolatedTransform_RW_ComponentTypeHandle = state.GetComponentTypeHandle<InterpolatedTransform>();
			__Game_Objects_TransformFrame_RW_BufferTypeHandle = state.GetBufferTypeHandle<TransformFrame>();
		}
	}

	private SimulationSystem m_SimulationSystem;

	private EntityQuery m_Query;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_Query = GetEntityQuery(ComponentType.ReadWrite<TransformFrame>(), ComponentType.ReadWrite<InterpolatedTransform>(), ComponentType.ReadOnly<Transform>());
		RequireForUpdate(m_Query);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		TransformFrameJob jobData = new TransformFrameJob
		{
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MovingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Moving_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HumanNavigationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_HumanNavigation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AnimalNavigationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_AnimalNavigation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TrainType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Train_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InterpolatedTransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformFrameType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_TransformFrame_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_SimulationFrameIndex = m_SimulationSystem.frameIndex
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_Query, base.Dependency);
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
	public TransformFrameSystem()
	{
	}
}
