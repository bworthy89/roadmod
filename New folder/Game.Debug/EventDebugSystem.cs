using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Mathematics;
using Game.Common;
using Game.Events;
using Game.Net;
using Game.Objects;
using Game.Rendering;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Debug;

[CompilerGenerated]
public class EventDebugSystem : BaseDebugSystem
{
	[BurstCompile]
	private struct EventGizmoJob : IJobChunk
	{
		[ReadOnly]
		public uint m_SimulationFrame;

		public GizmoBatcher m_GizmoBatcher;

		[ReadOnly]
		public ComponentTypeHandle<Game.Events.Event> m_EventType;

		[ReadOnly]
		public ComponentTypeHandle<Duration> m_DurationType;

		[ReadOnly]
		public ComponentTypeHandle<WeatherPhenomenon> m_WeatherPhenomenonType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<InterpolatedTransform> m_InterpolatedTransformType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has(ref m_EventType))
			{
				NativeArray<Duration> nativeArray = chunk.GetNativeArray(ref m_DurationType);
				NativeArray<WeatherPhenomenon> nativeArray2 = chunk.GetNativeArray(ref m_WeatherPhenomenonType);
				NativeArray<InterpolatedTransform> nativeArray3 = chunk.GetNativeArray(ref m_InterpolatedTransformType);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Duration duration = nativeArray[i];
					WeatherPhenomenon weatherPhenomenon = nativeArray2[i];
					if (duration.m_EndFrame < m_SimulationFrame && weatherPhenomenon.m_Intensity == 0f)
					{
						continue;
					}
					m_GizmoBatcher.DrawWireArc(weatherPhenomenon.m_PhenomenonPosition, new float3(0f, 1f, 0f), new float3(1f, 0f, 0f), 360f, weatherPhenomenon.m_PhenomenonRadius, UnityEngine.Color.cyan, 72);
					if (duration.m_StartFrame <= m_SimulationFrame || weatherPhenomenon.m_Intensity != 0f)
					{
						m_GizmoBatcher.DrawWireNode(weatherPhenomenon.m_HotspotPosition, 10f, UnityEngine.Color.yellow);
						m_GizmoBatcher.DrawWireArc(weatherPhenomenon.m_HotspotPosition, new float3(0f, 1f, 0f), new float3(1f, 0f, 0f), 360f, weatherPhenomenon.m_HotspotRadius, UnityEngine.Color.yellow, 72);
						if (nativeArray3.Length != 0)
						{
							InterpolatedTransform interpolatedTransform = nativeArray3[i];
							m_GizmoBatcher.DrawWireNode(interpolatedTransform.m_Position, 10f, UnityEngine.Color.green);
							m_GizmoBatcher.DrawWireArc(interpolatedTransform.m_Position, new float3(0f, 1f, 0f), new float3(1f, 0f, 0f), 360f, weatherPhenomenon.m_HotspotRadius, UnityEngine.Color.green, 72);
						}
					}
				}
				return;
			}
			NativeArray<Game.Objects.Transform> nativeArray4 = chunk.GetNativeArray(ref m_TransformType);
			if (nativeArray4.Length != 0)
			{
				for (int j = 0; j < nativeArray4.Length; j++)
				{
					Game.Objects.Transform transform = nativeArray4[j];
					m_GizmoBatcher.DrawArrow(transform.m_Position + new float3(0f, 20f, 0f), transform.m_Position, UnityEngine.Color.red, 5f);
				}
				return;
			}
			NativeArray<Curve> nativeArray5 = chunk.GetNativeArray(ref m_CurveType);
			if (nativeArray5.Length != 0)
			{
				for (int k = 0; k < nativeArray5.Length; k++)
				{
					float3 @float = MathUtils.Position(nativeArray5[k].m_Bezier, 0.5f);
					m_GizmoBatcher.DrawArrow(@float + new float3(0f, 20f, 0f), @float, UnityEngine.Color.red, 5f);
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
		public ComponentTypeHandle<Game.Events.Event> __Game_Events_Event_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Duration> __Game_Events_Duration_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WeatherPhenomenon> __Game_Events_WeatherPhenomenon_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InterpolatedTransform> __Game_Rendering_InterpolatedTransform_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Events_Event_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Events.Event>(isReadOnly: true);
			__Game_Events_Duration_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Duration>(isReadOnly: true);
			__Game_Events_WeatherPhenomenon_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WeatherPhenomenon>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Rendering_InterpolatedTransform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InterpolatedTransform>(isReadOnly: true);
		}
	}

	private EntityQuery m_EventQuery;

	private GizmosSystem m_GizmosSystem;

	private SimulationSystem m_SimulationSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EventQuery = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[7]
			{
				ComponentType.ReadOnly<WeatherPhenomenon>(),
				ComponentType.ReadOnly<OnFire>(),
				ComponentType.ReadOnly<AccidentSite>(),
				ComponentType.ReadOnly<InvolvedInAccident>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<FacingWeather>(),
				ComponentType.ReadOnly<SpectatorSite>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		RequireForUpdate(m_EventQuery);
		base.Enabled = false;
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new EventGizmoJob
		{
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies),
			m_EventType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_Event_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DurationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_Duration_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WeatherPhenomenonType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_WeatherPhenomenon_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InterpolatedTransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RO_ComponentTypeHandle, ref base.CheckedStateRef)
		}, m_EventQuery, JobHandle.CombineDependencies(inputDeps, dependencies));
		m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
		return jobHandle;
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
	public EventDebugSystem()
	{
	}
}
