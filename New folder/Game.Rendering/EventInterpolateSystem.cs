using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Common;
using Game.Events;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class EventInterpolateSystem : GameSystemBase
{
	[BurstCompile]
	private struct UpdateTransformDataJob : IJobChunk
	{
		[ReadOnly]
		public uint m_FrameIndex;

		[ReadOnly]
		public float m_FrameTime;

		[ReadOnly]
		public BufferTypeHandle<HotspotFrame> m_HotspotFrameType;

		public ComponentTypeHandle<InterpolatedTransform> m_InterpolatedTransformType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			uint num = 0u;
			uint num2 = m_FrameIndex - num - 32;
			uint num3 = num2 / 16 % 4;
			uint index = (num3 + 1) % 4;
			float t = ((float)(num2 % 16) + m_FrameTime) / 16f;
			NativeArray<InterpolatedTransform> nativeArray = chunk.GetNativeArray(ref m_InterpolatedTransformType);
			BufferAccessor<HotspotFrame> bufferAccessor = chunk.GetBufferAccessor(ref m_HotspotFrameType);
			for (int i = 0; i < chunk.Count; i++)
			{
				InterpolatedTransform value = nativeArray[i];
				DynamicBuffer<HotspotFrame> dynamicBuffer = bufferAccessor[i];
				HotspotFrame hotspotFrame = dynamicBuffer[(int)num3];
				HotspotFrame hotspotFrame2 = dynamicBuffer[(int)index];
				float num4 = 4f / 45f;
				Bezier4x3 curve = new Bezier4x3(hotspotFrame.m_Position, hotspotFrame.m_Position + hotspotFrame.m_Velocity * num4, hotspotFrame2.m_Position - hotspotFrame2.m_Velocity * num4, hotspotFrame2.m_Position);
				value.m_Position = MathUtils.Position(curve, t);
				value.m_Rotation = quaternion.identity;
				nativeArray[i] = value;
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
		public BufferTypeHandle<HotspotFrame> __Game_Events_HotspotFrame_RO_BufferTypeHandle;

		public ComponentTypeHandle<InterpolatedTransform> __Game_Rendering_InterpolatedTransform_RW_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Events_HotspotFrame_RO_BufferTypeHandle = state.GetBufferTypeHandle<HotspotFrame>(isReadOnly: true);
			__Game_Rendering_InterpolatedTransform_RW_ComponentTypeHandle = state.GetComponentTypeHandle<InterpolatedTransform>();
		}
	}

	private RenderingSystem m_RenderingSystem;

	private EntityQuery m_EventQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_EventQuery = GetEntityQuery(ComponentType.ReadOnly<WeatherPhenomenon>(), ComponentType.ReadOnly<HotspotFrame>(), ComponentType.ReadWrite<InterpolatedTransform>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_EventQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		UpdateTransformDataJob jobData = new UpdateTransformDataJob
		{
			m_FrameIndex = m_RenderingSystem.frameIndex,
			m_FrameTime = m_RenderingSystem.frameTime,
			m_HotspotFrameType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Events_HotspotFrame_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_InterpolatedTransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RW_ComponentTypeHandle, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_EventQuery, base.Dependency);
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
	public EventInterpolateSystem()
	{
	}
}
