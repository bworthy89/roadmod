using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Game.Pathfind;
using Game.Routes;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class ApplyRoutesSystem : GameSystemBase
{
	[BurstCompile]
	private struct PatchTempReferencesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Waypoint> m_WaypointType;

		[ReadOnly]
		public ComponentLookup<Connected> m_WaypointConnectionData;

		public BufferLookup<ConnectedRoute> m_Routes;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Waypoint> nativeArray = chunk.GetNativeArray(ref m_WaypointType);
			if (nativeArray.Length == 0)
			{
				return;
			}
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray3 = chunk.GetNativeArray(ref m_TempType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray2[i];
				Temp temp = nativeArray3[i];
				if (!(temp.m_Original != Entity.Null))
				{
					continue;
				}
				Connected connected = default(Connected);
				Connected connected2 = default(Connected);
				if (m_WaypointConnectionData.HasComponent(entity))
				{
					connected = m_WaypointConnectionData[entity];
				}
				if (m_WaypointConnectionData.HasComponent(temp.m_Original))
				{
					connected2 = m_WaypointConnectionData[temp.m_Original];
				}
				if (connected.m_Connected != connected2.m_Connected)
				{
					if (m_Routes.HasBuffer(connected2.m_Connected))
					{
						CollectionUtils.RemoveValue(m_Routes[connected2.m_Connected], new ConnectedRoute(temp.m_Original));
					}
					if (m_Routes.HasBuffer(connected.m_Connected))
					{
						CollectionUtils.TryAddUniqueValue(m_Routes[connected.m_Connected], new ConnectedRoute(temp.m_Original));
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct HandleTempEntitiesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Waypoint> m_WaypointType;

		[ReadOnly]
		public ComponentTypeHandle<Segment> m_SegmentType;

		[ReadOnly]
		public ComponentTypeHandle<Position> m_RoutePositionType;

		[ReadOnly]
		public ComponentTypeHandle<Connected> m_RouteConnectedType;

		[ReadOnly]
		public ComponentTypeHandle<PathTargets> m_RoutePathTargetsType;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> m_PathInformationType;

		[ReadOnly]
		public BufferTypeHandle<RouteWaypoint> m_RouteWaypointType;

		[ReadOnly]
		public BufferTypeHandle<RouteSegment> m_RouteSegmentType;

		[ReadOnly]
		public BufferTypeHandle<PathElement> m_PathElementType;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Position> m_RoutePositionData;

		[ReadOnly]
		public ComponentLookup<Connected> m_RouteConnectedData;

		[ReadOnly]
		public ComponentLookup<VehicleTiming> m_VehicleTimingData;

		[ReadOnly]
		public ComponentLookup<RouteInfo> m_RouteInfoData;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> m_Waypoints;

		[ReadOnly]
		public BufferLookup<RouteSegment> m_Segments;

		[ReadOnly]
		public EntityArchetype m_PathTargetEventArchetype;

		[ReadOnly]
		public ComponentTypeSet m_AppliedTypes;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<Waypoint> nativeArray3 = chunk.GetNativeArray(ref m_WaypointType);
			if (nativeArray3.Length != 0)
			{
				NativeArray<Position> nativeArray4 = chunk.GetNativeArray(ref m_RoutePositionType);
				NativeArray<Connected> nativeArray5 = chunk.GetNativeArray(ref m_RouteConnectedType);
				for (int i = 0; i < nativeArray.Length; i++)
				{
					Entity entity = nativeArray[i];
					Temp temp = nativeArray2[i];
					if ((temp.m_Flags & TempFlags.Delete) != 0)
					{
						Delete(unfilteredChunkIndex, entity, temp);
					}
					else if (temp.m_Original != Entity.Null)
					{
						if (nativeArray5.Length != 0)
						{
							Update(unfilteredChunkIndex, entity, temp, nativeArray3[i], nativeArray4[i], nativeArray5[i]);
						}
						else
						{
							Update(unfilteredChunkIndex, entity, temp, nativeArray3[i], nativeArray4[i], default(Connected));
						}
						UpdateComponent(unfilteredChunkIndex, entity, temp.m_Original, m_VehicleTimingData, updateValue: false);
					}
					else
					{
						Create(unfilteredChunkIndex, entity);
					}
				}
				return;
			}
			NativeArray<Segment> nativeArray6 = chunk.GetNativeArray(ref m_SegmentType);
			if (nativeArray6.Length != 0)
			{
				NativeArray<PathTargets> nativeArray7 = chunk.GetNativeArray(ref m_RoutePathTargetsType);
				NativeArray<PathInformation> nativeArray8 = chunk.GetNativeArray(ref m_PathInformationType);
				BufferAccessor<PathElement> bufferAccessor = chunk.GetBufferAccessor(ref m_PathElementType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity2 = nativeArray[j];
					Temp temp2 = nativeArray2[j];
					if ((temp2.m_Flags & TempFlags.Delete) != 0)
					{
						Delete(unfilteredChunkIndex, entity2, temp2);
					}
					else if (temp2.m_Original != Entity.Null)
					{
						if (nativeArray7.Length != 0)
						{
							CopyToOriginal(unfilteredChunkIndex, temp2, nativeArray7[j]);
						}
						if (nativeArray8.Length != 0)
						{
							UpdatePathInfo(unfilteredChunkIndex, temp2, nativeArray8[j]);
						}
						if (bufferAccessor.Length != 0)
						{
							CopyToOriginal(unfilteredChunkIndex, temp2, bufferAccessor[j]);
						}
						Update(unfilteredChunkIndex, entity2, temp2, nativeArray6[j]);
					}
					else
					{
						Create(unfilteredChunkIndex, entity2);
					}
				}
				return;
			}
			BufferAccessor<RouteWaypoint> bufferAccessor2 = chunk.GetBufferAccessor(ref m_RouteWaypointType);
			if (bufferAccessor2.Length != 0)
			{
				BufferAccessor<RouteSegment> bufferAccessor3 = chunk.GetBufferAccessor(ref m_RouteSegmentType);
				for (int k = 0; k < nativeArray.Length; k++)
				{
					Entity entity3 = nativeArray[k];
					Temp temp3 = nativeArray2[k];
					if ((temp3.m_Flags & TempFlags.Delete) != 0)
					{
						if (temp3.m_Original != Entity.Null)
						{
							Delete(unfilteredChunkIndex, entity3, temp3, bufferAccessor2[k], bufferAccessor3[k]);
						}
						else
						{
							Delete(unfilteredChunkIndex, entity3, temp3);
						}
					}
					else if (temp3.m_Original != Entity.Null)
					{
						Update(unfilteredChunkIndex, entity3, temp3, bufferAccessor2[k], bufferAccessor3[k]);
					}
					else
					{
						Create(unfilteredChunkIndex, entity3);
					}
				}
				return;
			}
			for (int l = 0; l < nativeArray.Length; l++)
			{
				Entity entity4 = nativeArray[l];
				Temp temp4 = nativeArray2[l];
				if ((temp4.m_Flags & TempFlags.Delete) != 0)
				{
					Delete(unfilteredChunkIndex, entity4, temp4);
				}
				else if (temp4.m_Original != Entity.Null)
				{
					Update(unfilteredChunkIndex, entity4, temp4);
				}
				else
				{
					Create(unfilteredChunkIndex, entity4);
				}
			}
		}

		private void Delete(int chunkIndex, Entity entity, Temp temp)
		{
			if (temp.m_Original != Entity.Null)
			{
				m_CommandBuffer.AddComponent(chunkIndex, temp.m_Original, default(Deleted));
			}
			m_CommandBuffer.AddComponent(chunkIndex, entity, default(Deleted));
		}

		private void UpdateComponent<T>(int chunkIndex, Entity entity, Entity original, ComponentLookup<T> data, bool updateValue) where T : unmanaged, IComponentData
		{
			if (data.HasComponent(entity))
			{
				if (data.HasComponent(original))
				{
					if (updateValue)
					{
						m_CommandBuffer.SetComponent(chunkIndex, original, data[entity]);
					}
				}
				else if (updateValue)
				{
					m_CommandBuffer.AddComponent(chunkIndex, original, data[entity]);
				}
				else
				{
					m_CommandBuffer.AddComponent(chunkIndex, original, default(T));
				}
			}
			else if (data.HasComponent(original))
			{
				m_CommandBuffer.RemoveComponent<T>(chunkIndex, original);
			}
		}

		private void Update(int chunkIndex, Entity entity, Temp temp, bool updateOriginal = true)
		{
			if (m_HiddenData.HasComponent(temp.m_Original))
			{
				m_CommandBuffer.RemoveComponent<Hidden>(chunkIndex, temp.m_Original);
			}
			if (updateOriginal)
			{
				m_CommandBuffer.AddComponent(chunkIndex, temp.m_Original, default(Updated));
			}
			m_CommandBuffer.AddComponent(chunkIndex, entity, default(Deleted));
		}

		private void Update(int chunkIndex, Entity entity, Temp temp, Waypoint waypoint, Position position, Connected connected)
		{
			m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, waypoint);
			Position position2 = m_RoutePositionData[temp.m_Original];
			if (!position2.m_Position.Equals(position.m_Position))
			{
				Entity e = m_CommandBuffer.CreateEntity(chunkIndex, m_PathTargetEventArchetype);
				m_CommandBuffer.SetComponent(chunkIndex, e, new PathTargetMoved(temp.m_Original, position2.m_Position, position.m_Position));
				m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, position);
			}
			if (connected.m_Connected != Entity.Null)
			{
				if (m_RouteConnectedData.HasComponent(temp.m_Original))
				{
					m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, connected);
				}
				else
				{
					m_CommandBuffer.AddComponent(chunkIndex, temp.m_Original, connected);
				}
			}
			else if (m_RouteConnectedData.HasComponent(temp.m_Original))
			{
				m_CommandBuffer.RemoveComponent<Connected>(chunkIndex, temp.m_Original);
			}
			Update(chunkIndex, entity, temp);
		}

		private void CopyToOriginal<T>(int chunkIndex, Temp temp, T data) where T : unmanaged, IComponentData
		{
			m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, data);
		}

		private void CopyToOriginal<T>(int chunkIndex, Temp temp, DynamicBuffer<T> data) where T : unmanaged, IBufferElementData
		{
			m_CommandBuffer.SetBuffer<T>(chunkIndex, temp.m_Original).CopyFrom(data.AsNativeArray());
		}

		private void UpdatePathInfo(int chunkIndex, Temp temp, PathInformation data)
		{
			m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, data);
			if (m_RouteInfoData.HasComponent(temp.m_Original))
			{
				RouteInfo component = m_RouteInfoData[temp.m_Original];
				component.m_Distance = math.max(component.m_Distance, data.m_Distance);
				component.m_Duration = math.max(component.m_Duration, data.m_Duration);
				m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, component);
			}
		}

		private void Update(int chunkIndex, Entity entity, Temp temp, Segment segment)
		{
			m_CommandBuffer.SetComponent(chunkIndex, temp.m_Original, segment);
			Update(chunkIndex, entity, temp);
		}

		private void Delete(int chunkIndex, Entity entity, Temp temp, DynamicBuffer<RouteWaypoint> waypoints, DynamicBuffer<RouteSegment> segments)
		{
			DynamicBuffer<RouteWaypoint> dynamicBuffer = m_Waypoints[temp.m_Original];
			DynamicBuffer<RouteSegment> dynamicBuffer2 = m_Segments[temp.m_Original];
			NativeParallelHashMap<Entity, int> nativeParallelHashMap = new NativeParallelHashMap<Entity, int>(dynamicBuffer.Length, Allocator.Temp);
			for (int i = 0; i < waypoints.Length; i++)
			{
				RouteWaypoint routeWaypoint = waypoints[i];
				if (routeWaypoint.m_Waypoint != Entity.Null)
				{
					Temp temp2 = m_TempData[routeWaypoint.m_Waypoint];
					if (temp2.m_Original != Entity.Null)
					{
						nativeParallelHashMap.TryAdd(temp2.m_Original, 1);
					}
				}
			}
			for (int j = 0; j < dynamicBuffer.Length; j++)
			{
				RouteWaypoint routeWaypoint2 = dynamicBuffer[j];
				if (routeWaypoint2.m_Waypoint != Entity.Null && !nativeParallelHashMap.TryGetValue(routeWaypoint2.m_Waypoint, out var _))
				{
					m_CommandBuffer.AddComponent(chunkIndex, routeWaypoint2.m_Waypoint, default(Deleted));
				}
			}
			nativeParallelHashMap.Clear();
			for (int k = 0; k < segments.Length; k++)
			{
				RouteSegment routeSegment = segments[k];
				if (routeSegment.m_Segment != Entity.Null)
				{
					Temp temp3 = m_TempData[routeSegment.m_Segment];
					if (temp3.m_Original != Entity.Null)
					{
						nativeParallelHashMap.TryAdd(temp3.m_Original, 1);
					}
				}
			}
			for (int l = 0; l < dynamicBuffer2.Length; l++)
			{
				RouteSegment routeSegment2 = dynamicBuffer2[l];
				if (routeSegment2.m_Segment != Entity.Null && !nativeParallelHashMap.TryGetValue(routeSegment2.m_Segment, out var _))
				{
					m_CommandBuffer.AddComponent(chunkIndex, routeSegment2.m_Segment, default(Deleted));
				}
			}
			nativeParallelHashMap.Dispose();
			m_CommandBuffer.AddComponent(chunkIndex, temp.m_Original, default(Deleted));
			m_CommandBuffer.AddComponent(chunkIndex, entity, default(Deleted));
		}

		private void Update(int chunkIndex, Entity entity, Temp temp, DynamicBuffer<RouteWaypoint> waypoints, DynamicBuffer<RouteSegment> segments)
		{
			DynamicBuffer<RouteWaypoint> dynamicBuffer = m_Waypoints[temp.m_Original];
			DynamicBuffer<RouteSegment> dynamicBuffer2 = m_Segments[temp.m_Original];
			DynamicBuffer<RouteWaypoint> dynamicBuffer3 = m_CommandBuffer.SetBuffer<RouteWaypoint>(chunkIndex, temp.m_Original);
			DynamicBuffer<RouteSegment> dynamicBuffer4 = m_CommandBuffer.SetBuffer<RouteSegment>(chunkIndex, temp.m_Original);
			dynamicBuffer3.ResizeUninitialized(waypoints.Length);
			dynamicBuffer4.ResizeUninitialized(segments.Length);
			NativeParallelHashMap<Entity, int> nativeParallelHashMap = new NativeParallelHashMap<Entity, int>(dynamicBuffer.Length, Allocator.Temp);
			for (int i = 0; i < waypoints.Length; i++)
			{
				RouteWaypoint value = waypoints[i];
				if (value.m_Waypoint != Entity.Null)
				{
					Temp temp2 = m_TempData[value.m_Waypoint];
					if (temp2.m_Original != Entity.Null)
					{
						nativeParallelHashMap.TryAdd(temp2.m_Original, 1);
						value.m_Waypoint = temp2.m_Original;
					}
					else
					{
						m_CommandBuffer.SetComponent(chunkIndex, value.m_Waypoint, new Owner(temp.m_Original));
					}
				}
				dynamicBuffer3[i] = value;
			}
			for (int j = 0; j < dynamicBuffer.Length; j++)
			{
				RouteWaypoint routeWaypoint = dynamicBuffer[j];
				if (routeWaypoint.m_Waypoint != Entity.Null && !nativeParallelHashMap.TryGetValue(routeWaypoint.m_Waypoint, out var _))
				{
					m_CommandBuffer.AddComponent(chunkIndex, routeWaypoint.m_Waypoint, default(Deleted));
				}
			}
			nativeParallelHashMap.Clear();
			for (int k = 0; k < segments.Length; k++)
			{
				RouteSegment value2 = segments[k];
				if (value2.m_Segment != Entity.Null)
				{
					Temp temp3 = m_TempData[value2.m_Segment];
					if (temp3.m_Original != Entity.Null)
					{
						nativeParallelHashMap.TryAdd(temp3.m_Original, 1);
						value2.m_Segment = temp3.m_Original;
					}
					else
					{
						m_CommandBuffer.SetComponent(chunkIndex, value2.m_Segment, new Owner(temp.m_Original));
					}
				}
				dynamicBuffer4[k] = value2;
			}
			for (int l = 0; l < dynamicBuffer2.Length; l++)
			{
				RouteSegment routeSegment = dynamicBuffer2[l];
				if (routeSegment.m_Segment != Entity.Null && !nativeParallelHashMap.TryGetValue(routeSegment.m_Segment, out var _))
				{
					m_CommandBuffer.AddComponent(chunkIndex, routeSegment.m_Segment, default(Deleted));
				}
			}
			nativeParallelHashMap.Dispose();
			Update(chunkIndex, entity, temp);
		}

		private void Create(int chunkIndex, Entity entity)
		{
			m_CommandBuffer.RemoveComponent<Temp>(chunkIndex, entity);
			m_CommandBuffer.AddComponent(chunkIndex, entity, in m_AppliedTypes);
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
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Waypoint> __Game_Routes_Waypoint_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Connected> __Game_Routes_Connected_RO_ComponentLookup;

		public BufferLookup<ConnectedRoute> __Game_Routes_ConnectedRoute_RW_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Segment> __Game_Routes_Segment_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Position> __Game_Routes_Position_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Connected> __Game_Routes_Connected_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathTargets> __Game_Routes_PathTargets_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<RouteSegment> __Game_Routes_RouteSegment_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<PathElement> __Game_Pathfind_PathElement_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Hidden> __Game_Tools_Hidden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Position> __Game_Routes_Position_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<VehicleTiming> __Game_Routes_VehicleTiming_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteInfo> __Game_Routes_RouteInfo_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<RouteSegment> __Game_Routes_RouteSegment_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Routes_Waypoint_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Waypoint>(isReadOnly: true);
			__Game_Routes_Connected_RO_ComponentLookup = state.GetComponentLookup<Connected>(isReadOnly: true);
			__Game_Routes_ConnectedRoute_RW_BufferLookup = state.GetBufferLookup<ConnectedRoute>();
			__Game_Routes_Segment_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Segment>(isReadOnly: true);
			__Game_Routes_Position_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Position>(isReadOnly: true);
			__Game_Routes_Connected_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Connected>(isReadOnly: true);
			__Game_Routes_PathTargets_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathTargets>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathInformation>(isReadOnly: true);
			__Game_Routes_RouteWaypoint_RO_BufferTypeHandle = state.GetBufferTypeHandle<RouteWaypoint>(isReadOnly: true);
			__Game_Routes_RouteSegment_RO_BufferTypeHandle = state.GetBufferTypeHandle<RouteSegment>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<PathElement>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentLookup = state.GetComponentLookup<Hidden>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Routes_Position_RO_ComponentLookup = state.GetComponentLookup<Position>(isReadOnly: true);
			__Game_Routes_VehicleTiming_RO_ComponentLookup = state.GetComponentLookup<VehicleTiming>(isReadOnly: true);
			__Game_Routes_RouteInfo_RO_ComponentLookup = state.GetComponentLookup<RouteInfo>(isReadOnly: true);
			__Game_Routes_RouteWaypoint_RO_BufferLookup = state.GetBufferLookup<RouteWaypoint>(isReadOnly: true);
			__Game_Routes_RouteSegment_RO_BufferLookup = state.GetBufferLookup<RouteSegment>(isReadOnly: true);
		}
	}

	private ToolOutputBarrier m_ToolOutputBarrier;

	private EntityQuery m_TempQuery;

	private EntityArchetype m_PathTargetEventArchetype;

	private ComponentTypeSet m_AppliedTypes;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
		m_TempQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Temp>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Route>(),
				ComponentType.ReadOnly<Waypoint>(),
				ComponentType.ReadOnly<Segment>()
			}
		});
		m_PathTargetEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<PathTargetMoved>());
		m_AppliedTypes = new ComponentTypeSet(ComponentType.ReadWrite<Applied>(), ComponentType.ReadWrite<Created>(), ComponentType.ReadWrite<Updated>());
		RequireForUpdate(m_TempQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		PatchTempReferencesJob jobData = new PatchTempReferencesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaypointType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_Waypoint_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaypointConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Connected_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Routes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_ConnectedRoute_RW_BufferLookup, ref base.CheckedStateRef)
		};
		HandleTempEntitiesJob jobData2 = new HandleTempEntitiesJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaypointType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_Waypoint_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SegmentType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_Segment_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RoutePositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_Position_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RouteConnectedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_Connected_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RoutePathTargetsType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_PathTargets_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathInformationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RouteWaypointType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_RouteSegmentType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_RouteSegment_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PathElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RoutePositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Position_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RouteConnectedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Connected_RO_ComponentLookup, ref base.CheckedStateRef),
			m_VehicleTimingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_VehicleTiming_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RouteInfoData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_RouteInfo_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Waypoints = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferLookup, ref base.CheckedStateRef),
			m_Segments = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteSegment_RO_BufferLookup, ref base.CheckedStateRef),
			m_PathTargetEventArchetype = m_PathTargetEventArchetype,
			m_AppliedTypes = m_AppliedTypes,
			m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		JobHandle job = JobChunkExtensions.Schedule(jobData, m_TempQuery, base.Dependency);
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData2, m_TempQuery, base.Dependency);
		base.Dependency = JobHandle.CombineDependencies(job, jobHandle);
		m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
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
	public ApplyRoutesSystem()
	{
	}
}
