using System.Runtime.CompilerServices;
using Game.Events;
using Game.Rendering;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class HotspotFrameSystem : GameSystemBase
{
	[BurstCompile]
	private struct HotspotFrameJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<WeatherPhenomenon> m_WeatherPhenomenonType;

		public ComponentTypeHandle<InterpolatedTransform> m_InterpolatedTransformType;

		public BufferTypeHandle<HotspotFrame> m_HotspotFrameType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<WeatherPhenomenon> nativeArray = chunk.GetNativeArray(ref m_WeatherPhenomenonType);
			NativeArray<InterpolatedTransform> nativeArray2 = chunk.GetNativeArray(ref m_InterpolatedTransformType);
			BufferAccessor<HotspotFrame> bufferAccessor = chunk.GetBufferAccessor(ref m_HotspotFrameType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				WeatherPhenomenon weatherPhenomenon = nativeArray[i];
				nativeArray2[i] = new InterpolatedTransform(weatherPhenomenon);
				DynamicBuffer<HotspotFrame> dynamicBuffer = bufferAccessor[i];
				dynamicBuffer.ResizeUninitialized(4);
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					dynamicBuffer[j] = new HotspotFrame(weatherPhenomenon);
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
		public ComponentTypeHandle<WeatherPhenomenon> __Game_Events_WeatherPhenomenon_RO_ComponentTypeHandle;

		public ComponentTypeHandle<InterpolatedTransform> __Game_Rendering_InterpolatedTransform_RW_ComponentTypeHandle;

		public BufferTypeHandle<HotspotFrame> __Game_Events_HotspotFrame_RW_BufferTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Events_WeatherPhenomenon_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WeatherPhenomenon>(isReadOnly: true);
			__Game_Rendering_InterpolatedTransform_RW_ComponentTypeHandle = state.GetComponentTypeHandle<InterpolatedTransform>();
			__Game_Events_HotspotFrame_RW_BufferTypeHandle = state.GetBufferTypeHandle<HotspotFrame>();
		}
	}

	private EntityQuery m_Query;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_Query = GetEntityQuery(ComponentType.ReadWrite<HotspotFrame>(), ComponentType.ReadWrite<InterpolatedTransform>(), ComponentType.ReadOnly<WeatherPhenomenon>());
		RequireForUpdate(m_Query);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		HotspotFrameJob jobData = new HotspotFrameJob
		{
			m_WeatherPhenomenonType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_WeatherPhenomenon_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InterpolatedTransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HotspotFrameType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Events_HotspotFrame_RW_BufferTypeHandle, ref base.CheckedStateRef)
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
	public HotspotFrameSystem()
	{
	}
}
