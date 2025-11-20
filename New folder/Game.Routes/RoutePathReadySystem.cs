using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Common;
using Game.Notifications;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Routes;

[CompilerGenerated]
public class RoutePathReadySystem : GameSystemBase
{
	[BurstCompile]
	private struct RoutePathReadyJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<PathUpdated> m_PathUpdatedType;

		[ReadOnly]
		public BufferTypeHandle<RouteWaypoint> m_RouteWaypointType;

		[ReadOnly]
		public BufferTypeHandle<RouteSegment> m_RouteSegmentType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Position> m_PositionData;

		[ReadOnly]
		public ComponentLookup<Segment> m_SegmentData;

		[ReadOnly]
		public ComponentLookup<HiddenRoute> m_HiddenRouteData;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public ComponentLookup<VerifiedPath> m_VerifiedPathData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> m_RouteWaypoints;

		[ReadOnly]
		public BufferLookup<RouteSegment> m_RouteSegments;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		public ComponentLookup<PathTargets> m_PathTargetsData;

		public BufferLookup<Efficiency> m_BuildingEfficiencies;

		[ReadOnly]
		public RouteConfigurationData m_RouteConfigurationData;

		public EntityCommandBuffer m_CommandBuffer;

		public IconCommandBuffer m_IconCommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PathUpdated> nativeArray = chunk.GetNativeArray(ref m_PathUpdatedType);
			if (nativeArray.Length != 0)
			{
				for (int i = 0; i < nativeArray.Length; i++)
				{
					PathUpdated pathUpdated = nativeArray[i];
					if (m_PathTargetsData.HasComponent(pathUpdated.m_Owner))
					{
						PathTargets value = m_PathTargetsData[pathUpdated.m_Owner];
						value.m_ReadyStartPosition = pathUpdated.m_Data.m_Position1;
						value.m_ReadyEndPosition = pathUpdated.m_Data.m_Position2;
						m_PathTargetsData[pathUpdated.m_Owner] = value;
					}
					if (m_PathInformationData.HasComponent(pathUpdated.m_Owner) && !m_TempData.HasComponent(pathUpdated.m_Owner))
					{
						if (m_VerifiedPathData.HasComponent(pathUpdated.m_Owner))
						{
							UpdateVerifiedPathNotifications(pathUpdated.m_Owner);
						}
						else
						{
							UpdatePathfindNotifications(pathUpdated.m_Owner);
						}
					}
				}
				return;
			}
			BufferAccessor<RouteWaypoint> bufferAccessor = chunk.GetBufferAccessor(ref m_RouteWaypointType);
			BufferAccessor<RouteSegment> bufferAccessor2 = chunk.GetBufferAccessor(ref m_RouteSegmentType);
			for (int j = 0; j < bufferAccessor.Length; j++)
			{
				DynamicBuffer<RouteWaypoint> dynamicBuffer = bufferAccessor[j];
				DynamicBuffer<RouteSegment> dynamicBuffer2 = bufferAccessor2[j];
				for (int k = 0; k < dynamicBuffer2.Length; k++)
				{
					RouteSegment routeSegment = dynamicBuffer2[k];
					if (m_PathTargetsData.HasComponent(routeSegment.m_Segment))
					{
						RouteWaypoint routeWaypoint = dynamicBuffer[k];
						RouteWaypoint routeWaypoint2 = dynamicBuffer[math.select(k + 1, 0, k + 1 >= dynamicBuffer.Length)];
						PathTargets value2 = m_PathTargetsData[routeSegment.m_Segment];
						if (m_PositionData.HasComponent(routeWaypoint.m_Waypoint))
						{
							value2.m_ReadyStartPosition = m_PositionData[routeWaypoint.m_Waypoint].m_Position;
						}
						if (m_PositionData.HasComponent(routeWaypoint2.m_Waypoint))
						{
							value2.m_ReadyEndPosition = m_PositionData[routeWaypoint2.m_Waypoint].m_Position;
						}
						m_PathTargetsData[routeSegment.m_Segment] = value2;
					}
				}
			}
		}

		private void UpdateVerifiedPathNotifications(Entity entity)
		{
			if (!m_SegmentData.TryGetComponent(entity, out var componentData))
			{
				return;
			}
			Entity topOwner = GetTopOwner(entity);
			IconFlags flags = ((componentData.m_Index == 1) ? IconFlags.SecondaryLocation : ((IconFlags)0));
			Entity entity2 = Entity.Null;
			Entity entity3 = Entity.Null;
			if (m_PathInformationData.TryGetComponent(entity, out var componentData2) && componentData2.m_Distance >= 0f)
			{
				if (GetBuildingOwner(componentData2.m_Origin, out var result) && GetTopOwner(result) == topOwner)
				{
					entity2 = result;
					m_IconCommandBuffer.Add(entity2, m_RouteConfigurationData.m_GateBypassNotification, IconPriority.Warning, IconClusterLayer.Default, flags);
				}
				if (GetBuildingOwner(componentData2.m_Destination, out var result2) && GetTopOwner(result2) == topOwner)
				{
					entity3 = result2;
					m_IconCommandBuffer.Add(entity3, m_RouteConfigurationData.m_GateBypassNotification, IconPriority.Warning, IconClusterLayer.Default, flags);
				}
			}
			if (topOwner != entity2 && topOwner != entity3)
			{
				m_IconCommandBuffer.Remove(topOwner, m_RouteConfigurationData.m_GateBypassNotification, default(Entity), flags);
			}
			if (m_InstalledUpgrades.TryGetBuffer(topOwner, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					Entity upgrade = bufferData[i].m_Upgrade;
					if (upgrade != entity2 && upgrade != entity3 && m_BuildingData.HasComponent(upgrade))
					{
						m_IconCommandBuffer.Remove(upgrade, m_RouteConfigurationData.m_GateBypassNotification, default(Entity), flags);
					}
				}
			}
			bool flag = true;
			if (m_OwnerData.TryGetComponent(entity, out var componentData3))
			{
				bool flag2 = m_HiddenRouteData.HasComponent(componentData3.m_Owner);
				if (m_RouteSegments.TryGetBuffer(componentData3.m_Owner, out var bufferData2))
				{
					for (int j = 0; j < bufferData2.Length; j++)
					{
						if (m_PathInformationData.TryGetComponent(bufferData2[j].m_Segment, out componentData2) && componentData2.m_Distance >= 0f)
						{
							flag = false;
							break;
						}
					}
				}
				if (flag2 != flag)
				{
					if (flag)
					{
						m_CommandBuffer.AddComponent<HiddenRoute>(componentData3.m_Owner);
					}
					else
					{
						m_CommandBuffer.RemoveComponent<HiddenRoute>(componentData3.m_Owner);
					}
				}
			}
			if (m_BuildingEfficiencies.TryGetBuffer(topOwner, out var bufferData3))
			{
				float efficiency = math.select(1f + m_RouteConfigurationData.m_GateBypassEfficiency, 1f, flag);
				BuildingUtils.SetEfficiencyFactor(bufferData3, EfficiencyFactor.GateBypass, efficiency);
			}
		}

		private Entity GetTopOwner(Entity entity)
		{
			Owner componentData;
			while (m_OwnerData.TryGetComponent(entity, out componentData))
			{
				entity = componentData.m_Owner;
			}
			return entity;
		}

		private bool GetBuildingOwner(Entity entity, out Entity result)
		{
			result = entity;
			Owner componentData;
			while (m_OwnerData.TryGetComponent(result, out componentData) && !m_BuildingData.HasComponent(result))
			{
				result = componentData.m_Owner;
			}
			return m_BuildingData.HasComponent(result);
		}

		private void UpdatePathfindNotifications(Entity entity)
		{
			if (m_SegmentData.HasComponent(entity) && m_OwnerData.HasComponent(entity))
			{
				Segment segment = m_SegmentData[entity];
				Owner owner = m_OwnerData[entity];
				DynamicBuffer<RouteWaypoint> waypoints = m_RouteWaypoints[owner.m_Owner];
				DynamicBuffer<RouteSegment> segments = m_RouteSegments[owner.m_Owner];
				int index = segment.m_Index;
				int waypointIndex = math.select(segment.m_Index + 1, 0, segment.m_Index == waypoints.Length - 1);
				UpdatePathfindNotification(waypoints, segments, index);
				UpdatePathfindNotification(waypoints, segments, waypointIndex);
			}
		}

		private void UpdatePathfindNotification(DynamicBuffer<RouteWaypoint> waypoints, DynamicBuffer<RouteSegment> segments, int waypointIndex)
		{
			int num = math.select(waypointIndex - 1, waypoints.Length - 1, waypointIndex == 0);
			bool flag = false;
			if (num < segments.Length && m_PathInformationData.TryGetComponent(segments[num].m_Segment, out var componentData))
			{
				flag |= componentData.m_Distance < 0f;
			}
			if (waypointIndex < segments.Length && m_PathInformationData.TryGetComponent(segments[waypointIndex].m_Segment, out var componentData2))
			{
				flag |= componentData2.m_Distance < 0f;
			}
			if (flag)
			{
				m_IconCommandBuffer.Add(waypoints[waypointIndex].m_Waypoint, m_RouteConfigurationData.m_PathfindNotification, IconPriority.Warning);
			}
			else
			{
				m_IconCommandBuffer.Remove(waypoints[waypointIndex].m_Waypoint, m_RouteConfigurationData.m_PathfindNotification);
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
		public ComponentTypeHandle<PathUpdated> __Game_Pathfind_PathUpdated_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<RouteSegment> __Game_Routes_RouteSegment_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Position> __Game_Routes_Position_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Segment> __Game_Routes_Segment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HiddenRoute> __Game_Routes_HiddenRoute_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<VerifiedPath> __Game_Routes_VerifiedPath_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<RouteSegment> __Game_Routes_RouteSegment_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		public ComponentLookup<PathTargets> __Game_Routes_PathTargets_RW_ComponentLookup;

		public BufferLookup<Efficiency> __Game_Buildings_Efficiency_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Pathfind_PathUpdated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathUpdated>(isReadOnly: true);
			__Game_Routes_RouteWaypoint_RO_BufferTypeHandle = state.GetBufferTypeHandle<RouteWaypoint>(isReadOnly: true);
			__Game_Routes_RouteSegment_RO_BufferTypeHandle = state.GetBufferTypeHandle<RouteSegment>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Routes_Position_RO_ComponentLookup = state.GetComponentLookup<Position>(isReadOnly: true);
			__Game_Routes_Segment_RO_ComponentLookup = state.GetComponentLookup<Segment>(isReadOnly: true);
			__Game_Routes_HiddenRoute_RO_ComponentLookup = state.GetComponentLookup<HiddenRoute>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Routes_VerifiedPath_RO_ComponentLookup = state.GetComponentLookup<VerifiedPath>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Routes_RouteWaypoint_RO_BufferLookup = state.GetBufferLookup<RouteWaypoint>(isReadOnly: true);
			__Game_Routes_RouteSegment_RO_BufferLookup = state.GetBufferLookup<RouteSegment>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Routes_PathTargets_RW_ComponentLookup = state.GetComponentLookup<PathTargets>();
			__Game_Buildings_Efficiency_RW_BufferLookup = state.GetBufferLookup<Efficiency>();
		}
	}

	private ModificationBarrier1 m_ModificationBarrier;

	private IconCommandSystem m_IconCommandSystem;

	private EntityQuery m_PathReadyQuery;

	private EntityQuery m_RouteQuery;

	private EntityQuery m_RouteConfigQuery;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier1>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_PathReadyQuery = GetEntityQuery(ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<PathUpdated>());
		m_RouteQuery = GetEntityQuery(ComponentType.ReadOnly<Route>());
		m_RouteConfigQuery = GetEntityQuery(ComponentType.ReadOnly<RouteConfigurationData>());
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_Loaded = true;
	}

	private bool GetLoaded()
	{
		if (m_Loaded)
		{
			m_Loaded = false;
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		EntityQuery query = (GetLoaded() ? m_RouteQuery : m_PathReadyQuery);
		if (!query.IsEmptyIgnoreFilter)
		{
			JobHandle jobHandle = JobChunkExtensions.Schedule(new RoutePathReadyJob
			{
				m_PathUpdatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathUpdated_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_RouteWaypointType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_RouteSegmentType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_RouteSegment_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Position_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SegmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Segment_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HiddenRouteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_HiddenRoute_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PathInformationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_VerifiedPathData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_VerifiedPath_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
				m_RouteWaypoints = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferLookup, ref base.CheckedStateRef),
				m_RouteSegments = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteSegment_RO_BufferLookup, ref base.CheckedStateRef),
				m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
				m_PathTargetsData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_PathTargets_RW_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingEfficiencies = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Efficiency_RW_BufferLookup, ref base.CheckedStateRef),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer(),
				m_RouteConfigurationData = m_RouteConfigQuery.GetSingleton<RouteConfigurationData>(),
				m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer()
			}, query, base.Dependency);
			m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
			m_IconCommandSystem.AddCommandBufferWriter(jobHandle);
			base.Dependency = jobHandle;
		}
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
	public RoutePathReadySystem()
	{
	}
}
