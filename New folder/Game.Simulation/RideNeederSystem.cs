#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Game.Areas;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class RideNeederSystem : GameSystemBase
{
	[BurstCompile]
	private struct RideNeederTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<RideNeeder> m_RideNeederType;

		[ReadOnly]
		public ComponentTypeHandle<HumanCurrentLane> m_HumanCurrentLaneType;

		[ReadOnly]
		public ComponentLookup<TaxiRequest> m_TaxiRequestData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> m_BorderDistrictData;

		[ReadOnly]
		public ComponentLookup<ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public EntityArchetype m_TaxiRequestArchetype;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<RideNeeder> nativeArray2 = chunk.GetNativeArray(ref m_RideNeederType);
			NativeArray<HumanCurrentLane> nativeArray3 = chunk.GetNativeArray(ref m_HumanCurrentLaneType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				RideNeeder rideNeeder = nativeArray2[i];
				if (nativeArray3.Length == 0)
				{
					m_CommandBuffer.RemoveComponent<RideNeeder>(unfilteredChunkIndex, entity);
					continue;
				}
				HumanCurrentLane humanCurrentLane = nativeArray3[i];
				if ((nativeArray3[i].m_Flags & (CreatureLaneFlags.ParkingSpace | CreatureLaneFlags.Taxi)) != (CreatureLaneFlags.ParkingSpace | CreatureLaneFlags.Taxi))
				{
					m_CommandBuffer.RemoveComponent<RideNeeder>(unfilteredChunkIndex, entity);
					continue;
				}
				TaxiRequestType type = TaxiRequestType.Customer;
				if (m_ConnectionLaneData.HasComponent(humanCurrentLane.m_Lane) && (m_ConnectionLaneData[humanCurrentLane.m_Lane].m_Flags & ConnectionLaneFlags.Outside) != 0)
				{
					type = TaxiRequestType.Outside;
				}
				RequestNewVehicleIfNeeded(unfilteredChunkIndex, entity, humanCurrentLane.m_Lane, rideNeeder, type, 1);
			}
		}

		private void RequestNewVehicleIfNeeded(int jobIndex, Entity entity, Entity lane, RideNeeder rideNeeder, TaxiRequestType type, int priority)
		{
			if (!m_TaxiRequestData.HasComponent(rideNeeder.m_RideRequest))
			{
				GetDistricts(lane, out var district, out var district2);
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_TaxiRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new TaxiRequest(entity, district, district2, type, priority));
				m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(16u));
			}
		}

		private void GetDistricts(Entity entity, out Entity district1, out Entity district2)
		{
			while (true)
			{
				if (m_BorderDistrictData.TryGetComponent(entity, out var componentData))
				{
					district1 = componentData.m_Left;
					district2 = componentData.m_Right;
					return;
				}
				if (m_CurrentDistrictData.TryGetComponent(entity, out var componentData2))
				{
					district1 = componentData2.m_District;
					district2 = componentData2.m_District;
					return;
				}
				if (!m_OwnerData.TryGetComponent(entity, out var componentData3))
				{
					break;
				}
				entity = componentData3.m_Owner;
			}
			district1 = Entity.Null;
			district2 = Entity.Null;
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
		public ComponentTypeHandle<RideNeeder> __Game_Creatures_RideNeeder_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HumanCurrentLane> __Game_Creatures_HumanCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<TaxiRequest> __Game_Simulation_TaxiRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> __Game_Areas_BorderDistrict_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Creatures_RideNeeder_RO_ComponentTypeHandle = state.GetComponentTypeHandle<RideNeeder>(isReadOnly: true);
			__Game_Creatures_HumanCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HumanCurrentLane>(isReadOnly: true);
			__Game_Simulation_TaxiRequest_RO_ComponentLookup = state.GetComponentLookup<TaxiRequest>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RO_ComponentLookup = state.GetComponentLookup<CurrentDistrict>(isReadOnly: true);
			__Game_Areas_BorderDistrict_RO_ComponentLookup = state.GetComponentLookup<BorderDistrict>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<ConnectionLane>(isReadOnly: true);
		}
	}

	private const uint UPDATE_INTERVAL = 256u;

	private EntityQuery m_NeederQuery;

	private EntityArchetype m_VehicleRequestArchetype;

	private EndFrameBarrier m_EndFrameBarrier;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_NeederQuery = GetEntityQuery(ComponentType.ReadOnly<RideNeeder>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_VehicleRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<TaxiRequest>(), ComponentType.ReadWrite<RequestGroup>());
		RequireForUpdate(m_NeederQuery);
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new RideNeederTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_RideNeederType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_RideNeeder_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HumanCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_HumanCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TaxiRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_TaxiRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BorderDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_BorderDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TaxiRequestArchetype = m_VehicleRequestArchetype,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_NeederQuery, base.Dependency);
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
	public RideNeederSystem()
	{
	}
}
