using System.Runtime.CompilerServices;
using Game.Common;
using Game.Events;
using Game.Net;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class VehicleLaunchSystem : GameSystemBase
{
	[BurstCompile]
	private struct VehicleLaunchJob : IJobChunk
	{
		[ReadOnly]
		public uint m_SimulationFrame;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Duration> m_DurationType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<TargetElement> m_TargetElementType;

		public ComponentTypeHandle<Game.Events.VehicleLaunch> m_VehicleLaunchType;

		[ReadOnly]
		public ComponentLookup<SpectatorSite> m_SpectatorSiteData;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public ComponentLookup<Produced> m_ProducedData;

		[ReadOnly]
		public ComponentLookup<VehicleLaunchData> m_VehicleLaunchData;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> m_OwnedVehicles;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElements;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Duration> nativeArray2 = chunk.GetNativeArray(ref m_DurationType);
			NativeArray<Game.Events.VehicleLaunch> nativeArray3 = chunk.GetNativeArray(ref m_VehicleLaunchType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<TargetElement> bufferAccessor = chunk.GetBufferAccessor(ref m_TargetElementType);
			for (int i = 0; i < nativeArray3.Length; i++)
			{
				Entity eventEntity = nativeArray[i];
				Game.Events.VehicleLaunch value = nativeArray3[i];
				PrefabRef prefabRef = nativeArray4[i];
				if ((value.m_Flags & VehicleLaunchFlags.PathRequested) == 0)
				{
					if (nativeArray2.Length != 0 && nativeArray2[i].m_StartFrame > m_SimulationFrame)
					{
						continue;
					}
					Entity siteEntity = FindSpectatorSite(bufferAccessor[i], eventEntity);
					Entity vehicleEntity = FindProducedVehicle(siteEntity);
					VehicleLaunchData vehicleLaunchData = m_VehicleLaunchData[prefabRef.m_Prefab];
					FindPath(unfilteredChunkIndex, eventEntity, vehicleEntity, vehicleLaunchData);
					value.m_Flags |= VehicleLaunchFlags.PathRequested;
				}
				else if ((value.m_Flags & VehicleLaunchFlags.Launched) == 0)
				{
					Entity siteEntity2 = FindSpectatorSite(bufferAccessor[i], eventEntity);
					Entity vehicleEntity2 = FindProducedVehicle(siteEntity2);
					LaunchVehicle(unfilteredChunkIndex, eventEntity, vehicleEntity2);
					value.m_Flags |= VehicleLaunchFlags.Launched;
				}
				nativeArray3[i] = value;
			}
		}

		private Entity FindSpectatorSite(DynamicBuffer<TargetElement> targetElements, Entity eventEntity)
		{
			for (int i = 0; i < targetElements.Length; i++)
			{
				Entity entity = targetElements[i].m_Entity;
				if (m_SpectatorSiteData.HasComponent(entity) && m_SpectatorSiteData[entity].m_Event == eventEntity)
				{
					return entity;
				}
			}
			return Entity.Null;
		}

		private Entity FindProducedVehicle(Entity siteEntity)
		{
			if (m_OwnedVehicles.HasBuffer(siteEntity))
			{
				DynamicBuffer<OwnedVehicle> dynamicBuffer = m_OwnedVehicles[siteEntity];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity vehicle = dynamicBuffer[i].m_Vehicle;
					if (m_ProducedData.HasComponent(vehicle))
					{
						return vehicle;
					}
				}
			}
			return Entity.Null;
		}

		private void FindPath(int jobIndex, Entity eventEntity, Entity vehicleEntity, VehicleLaunchData vehicleLaunchData)
		{
			if (vehicleEntity != Entity.Null)
			{
				PathfindParameters parameters = new PathfindParameters
				{
					m_MaxSpeed = 277.77777f,
					m_WalkSpeed = 5.555556f,
					m_Weights = new PathfindWeights(1f, 1f, 1f, 1f),
					m_IgnoredRules = (RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidTransitTraffic | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidPrivateTraffic | RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
				};
				SetupQueueTarget origin = new SetupQueueTarget
				{
					m_Type = SetupTargetType.CurrentLocation,
					m_Entity = vehicleEntity
				};
				SetupQueueTarget destination = new SetupQueueTarget
				{
					m_Type = SetupTargetType.OutsideConnection,
					m_Value2 = 1000f
				};
				if (vehicleLaunchData.m_TransportType == TransportType.Rocket)
				{
					parameters.m_Methods = PathMethod.Road | PathMethod.Flying;
					origin.m_Methods = PathMethod.Road;
					origin.m_RoadTypes = RoadTypes.Helicopter;
					destination.m_Methods = PathMethod.Flying;
					destination.m_FlyingTypes = RoadTypes.Helicopter;
					m_PathfindQueue.Enqueue(new SetupQueueItem(eventEntity, parameters, origin, destination));
				}
			}
			m_CommandBuffer.AddComponent(jobIndex, eventEntity, default(PathInformation));
			m_CommandBuffer.AddBuffer<PathElement>(jobIndex, eventEntity);
		}

		private void LaunchVehicle(int jobIndex, Entity eventEntity, Entity vehicleEntity)
		{
			if (vehicleEntity != Entity.Null)
			{
				PathInformation pathInformation = m_PathInformationData[eventEntity];
				DynamicBuffer<PathElement> sourceElements = m_PathElements[eventEntity];
				m_CommandBuffer.RemoveComponent<Produced>(jobIndex, vehicleEntity);
				m_CommandBuffer.SetComponent(jobIndex, vehicleEntity, new Target(pathInformation.m_Destination));
				m_CommandBuffer.SetComponent(jobIndex, vehicleEntity, new PathOwner(PathFlags.Updated));
				m_CommandBuffer.SetComponent(jobIndex, vehicleEntity, new Game.Vehicles.PublicTransport
				{
					m_State = PublicTransportFlags.Launched
				});
				DynamicBuffer<PathElement> targetElements = m_CommandBuffer.SetBuffer<PathElement>(jobIndex, vehicleEntity);
				PathUtils.CopyPath(sourceElements, default(PathOwner), 0, targetElements);
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
		public ComponentTypeHandle<Duration> __Game_Events_Duration_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<TargetElement> __Game_Events_TargetElement_RO_BufferTypeHandle;

		public ComponentTypeHandle<Game.Events.VehicleLaunch> __Game_Events_VehicleLaunch_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<SpectatorSite> __Game_Events_SpectatorSite_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Produced> __Game_Vehicles_Produced_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<VehicleLaunchData> __Game_Prefabs_VehicleLaunchData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Events_Duration_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Duration>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Events_TargetElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<TargetElement>(isReadOnly: true);
			__Game_Events_VehicleLaunch_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Events.VehicleLaunch>();
			__Game_Events_SpectatorSite_RO_ComponentLookup = state.GetComponentLookup<SpectatorSite>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Vehicles_Produced_RO_ComponentLookup = state.GetComponentLookup<Produced>(isReadOnly: true);
			__Game_Prefabs_VehicleLaunchData_RO_ComponentLookup = state.GetComponentLookup<VehicleLaunchData>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferLookup = state.GetBufferLookup<PathElement>(isReadOnly: true);
		}
	}

	private const uint UPDATE_INTERVAL = 64u;

	private SimulationSystem m_SimulationSystem;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_LaunchQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 64;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_LaunchQuery = GetEntityQuery(ComponentType.ReadWrite<Game.Events.VehicleLaunch>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_LaunchQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		VehicleLaunchJob jobData = new VehicleLaunchJob
		{
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter(),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_DurationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_Duration_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TargetElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Events_TargetElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_VehicleLaunchType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Events_VehicleLaunch_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SpectatorSiteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_SpectatorSite_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathInformationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ProducedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Produced_RO_ComponentLookup, ref base.CheckedStateRef),
			m_VehicleLaunchData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_VehicleLaunchData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_LaunchQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
		m_PathfindSetupSystem.AddQueueWriter(base.Dependency);
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
	public VehicleLaunchSystem()
	{
	}
}
