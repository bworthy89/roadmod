using System.Runtime.CompilerServices;
using Game.Common;
using Game.Objects;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class AnimationSystem : GameSystemBase
{
	[BurstCompile]
	private struct AnimateJob : IJobChunk
	{
		[ReadOnly]
		public float m_DeltaTime;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		public ComponentTypeHandle<Animation> m_AnimationType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Game.Objects.Transform> nativeArray = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<Animation> nativeArray2 = chunk.GetNativeArray(ref m_AnimationType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Game.Objects.Transform transform = nativeArray[i];
				Animation value = nativeArray2[i];
				value.m_SwayVelocity.zx += (value.m_TargetPosition.xz - transform.m_Position.xz) * value.m_PushFactor;
				value.m_TargetPosition = transform.m_Position;
				value.m_PushFactor = 0f;
				value.m_SwayVelocity *= math.pow(0.0001f, m_DeltaTime);
				value.m_SwayVelocity -= value.m_SwayPosition * (m_DeltaTime * 100f / (1f + 100f * m_DeltaTime * m_DeltaTime));
				value.m_SwayPosition += value.m_SwayVelocity * m_DeltaTime;
				value.m_SwayPosition = math.atan(value.m_SwayPosition * 0.02f) * 50f;
				float3 xyz = value.m_SwayPosition * 0.005f;
				xyz.z = 0f - xyz.z;
				quaternion a = quaternion.EulerZXY(xyz);
				float3 swayPivot = value.m_SwayPivot;
				value.m_Rotation = math.mul(a, transform.m_Rotation);
				float3 start = transform.m_Position + math.mul(transform.m_Rotation, swayPivot) - math.mul(value.m_Rotation, swayPivot);
				value.m_Position = math.lerp(start, value.m_Position, math.pow(1E-14f, m_DeltaTime));
				nativeArray2[i] = value;
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
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Animation> __Game_Tools_Animation_RW_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Tools_Animation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Animation>();
		}
	}

	private EntityQuery m_AnimatedQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_AnimatedQuery = GetEntityQuery(ComponentType.ReadWrite<Animation>(), ComponentType.ReadOnly<Game.Objects.Transform>(), ComponentType.Exclude<Deleted>());
		RequireForUpdate(m_AnimatedQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependency = JobChunkExtensions.ScheduleParallel(new AnimateJob
		{
			m_DeltaTime = UnityEngine.Time.deltaTime,
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AnimationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Animation_RW_ComponentTypeHandle, ref base.CheckedStateRef)
		}, m_AnimatedQuery, base.Dependency);
		base.Dependency = dependency;
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
	public AnimationSystem()
	{
	}
}
