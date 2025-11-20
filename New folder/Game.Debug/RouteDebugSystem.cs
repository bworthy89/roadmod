using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Routes;
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
public class RouteDebugSystem : BaseDebugSystem
{
	[BurstCompile]
	private struct RouteGizmoJob : IJobChunk
	{
		[ReadOnly]
		public bool m_RouteOption;

		[ReadOnly]
		public bool m_LaneConnectionOption;

		[ReadOnly]
		public ComponentTypeHandle<Route> m_RouteType;

		[ReadOnly]
		public ComponentTypeHandle<Position> m_PositionType;

		[ReadOnly]
		public ComponentTypeHandle<AccessLane> m_AccessLaneType;

		[ReadOnly]
		public ComponentTypeHandle<RouteLane> m_RouteLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public BufferTypeHandle<RouteWaypoint> m_WaypointType;

		[ReadOnly]
		public ComponentTypeHandle<Error> m_ErrorType;

		[ReadOnly]
		public ComponentLookup<Position> m_PositionData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		public GizmoBatcher m_GizmoBatcher;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			UnityEngine.Color color = (chunk.Has(ref m_ErrorType) ? UnityEngine.Color.red : ((!chunk.Has(ref m_TempType)) ? UnityEngine.Color.cyan : UnityEngine.Color.blue));
			NativeArray<Route> nativeArray = chunk.GetNativeArray(ref m_RouteType);
			if (nativeArray.Length != 0)
			{
				if (!m_RouteOption)
				{
					return;
				}
				BufferAccessor<RouteWaypoint> bufferAccessor = chunk.GetBufferAccessor(ref m_WaypointType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Route route = nativeArray[i];
					DynamicBuffer<RouteWaypoint> dynamicBuffer = bufferAccessor[i];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						RouteWaypoint routeWaypoint = dynamicBuffer[j];
						float3 position = m_PositionData[routeWaypoint.m_Waypoint].m_Position;
						m_GizmoBatcher.DrawWireNode(position, 2f, color);
						if (j != dynamicBuffer.Length - 1 || (route.m_Flags & RouteFlags.Complete) != 0)
						{
							RouteWaypoint routeWaypoint2 = dynamicBuffer[math.select(j + 1, 0, j == dynamicBuffer.Length - 1)];
							float3 position2 = m_PositionData[routeWaypoint2.m_Waypoint].m_Position;
							m_GizmoBatcher.DrawMiddleArrow(position, position2, color, 4f);
						}
					}
				}
			}
			else
			{
				if (!m_LaneConnectionOption)
				{
					return;
				}
				NativeArray<AccessLane> nativeArray2 = chunk.GetNativeArray(ref m_AccessLaneType);
				NativeArray<RouteLane> nativeArray3 = chunk.GetNativeArray(ref m_RouteLaneType);
				NativeArray<Position> nativeArray4 = chunk.GetNativeArray(ref m_PositionType);
				NativeArray<Game.Objects.Transform> nativeArray5 = chunk.GetNativeArray(ref m_TransformType);
				if (nativeArray4.Length != 0)
				{
					for (int k = 0; k < nativeArray2.Length; k++)
					{
						float3 position3 = nativeArray4[k].m_Position;
						AccessLane accessLane = nativeArray2[k];
						if (m_CurveData.HasComponent(accessLane.m_Lane))
						{
							float3 @float = MathUtils.Position(m_CurveData[accessLane.m_Lane].m_Bezier, accessLane.m_CurvePos);
							m_GizmoBatcher.DrawWireNode(@float, 1f, UnityEngine.Color.green);
							m_GizmoBatcher.DrawLine(position3, @float, UnityEngine.Color.green);
						}
					}
					for (int l = 0; l < nativeArray3.Length; l++)
					{
						float3 position4 = nativeArray4[l].m_Position;
						RouteLane routeLane = nativeArray3[l];
						if (m_CurveData.HasComponent(routeLane.m_StartLane))
						{
							float3 float2 = MathUtils.Position(m_CurveData[routeLane.m_StartLane].m_Bezier, routeLane.m_StartCurvePos);
							m_GizmoBatcher.DrawWireNode(float2, 1f, UnityEngine.Color.magenta);
							m_GizmoBatcher.DrawLine(position4, float2, UnityEngine.Color.magenta);
						}
						if (m_CurveData.HasComponent(routeLane.m_EndLane))
						{
							float3 float3 = MathUtils.Position(m_CurveData[routeLane.m_EndLane].m_Bezier, routeLane.m_EndCurvePos);
							m_GizmoBatcher.DrawWireNode(float3, 1f, UnityEngine.Color.magenta);
							m_GizmoBatcher.DrawLine(position4, float3, UnityEngine.Color.magenta);
						}
					}
				}
				else
				{
					if (nativeArray5.Length == 0)
					{
						return;
					}
					for (int m = 0; m < nativeArray2.Length; m++)
					{
						float3 position5 = nativeArray5[m].m_Position;
						AccessLane accessLane2 = nativeArray2[m];
						if (m_CurveData.HasComponent(accessLane2.m_Lane))
						{
							float3 float4 = MathUtils.Position(m_CurveData[accessLane2.m_Lane].m_Bezier, accessLane2.m_CurvePos);
							m_GizmoBatcher.DrawWireNode(float4, 1f, UnityEngine.Color.green);
							m_GizmoBatcher.DrawLine(position5, float4, UnityEngine.Color.green);
						}
					}
					for (int n = 0; n < nativeArray3.Length; n++)
					{
						float3 position6 = nativeArray5[n].m_Position;
						RouteLane routeLane2 = nativeArray3[n];
						if (m_CurveData.HasComponent(routeLane2.m_StartLane))
						{
							float3 float5 = MathUtils.Position(m_CurveData[routeLane2.m_StartLane].m_Bezier, routeLane2.m_StartCurvePos);
							m_GizmoBatcher.DrawWireNode(float5, 1f, UnityEngine.Color.magenta);
							m_GizmoBatcher.DrawLine(position6, float5, UnityEngine.Color.magenta);
						}
						if (m_CurveData.HasComponent(routeLane2.m_EndLane))
						{
							float3 float6 = MathUtils.Position(m_CurveData[routeLane2.m_EndLane].m_Bezier, routeLane2.m_EndCurvePos);
							m_GizmoBatcher.DrawWireNode(float6, 1f, UnityEngine.Color.magenta);
							m_GizmoBatcher.DrawLine(position6, float6, UnityEngine.Color.magenta);
						}
					}
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
		public ComponentTypeHandle<Route> __Game_Routes_Route_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Position> __Game_Routes_Position_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<AccessLane> __Game_Routes_AccessLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<RouteLane> __Game_Routes_RouteLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Error> __Game_Tools_Error_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Position> __Game_Routes_Position_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Routes_Route_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Route>(isReadOnly: true);
			__Game_Routes_Position_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Position>(isReadOnly: true);
			__Game_Routes_AccessLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AccessLane>(isReadOnly: true);
			__Game_Routes_RouteLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<RouteLane>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Routes_RouteWaypoint_RO_BufferTypeHandle = state.GetBufferTypeHandle<RouteWaypoint>(isReadOnly: true);
			__Game_Tools_Error_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Error>(isReadOnly: true);
			__Game_Routes_Position_RO_ComponentLookup = state.GetComponentLookup<Position>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
		}
	}

	private EntityQuery m_RouteGroup;

	private GizmosSystem m_GizmosSystem;

	private Option m_RouteOption;

	private Option m_LaneConnectionOption;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_RouteGroup = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Route>(),
				ComponentType.ReadOnly<RouteWaypoint>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Hidden>()
			}
		}, new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<AccessLane>(),
				ComponentType.ReadOnly<RouteLane>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Hidden>()
			}
		});
		m_RouteOption = AddOption("Routes", defaultEnabled: true);
		m_LaneConnectionOption = AddOption("Lane Connections", defaultEnabled: true);
		RequireForUpdate(m_RouteGroup);
		base.Enabled = false;
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new RouteGizmoJob
		{
			m_RouteOption = m_RouteOption.enabled,
			m_LaneConnectionOption = m_LaneConnectionOption.enabled,
			m_RouteType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_Route_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_Position_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AccessLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_AccessLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RouteLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_RouteLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaypointType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ErrorType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Error_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Position_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies)
		}, m_RouteGroup, JobHandle.CombineDependencies(inputDeps, dependencies));
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
	public RouteDebugSystem()
	{
	}
}
