using System.Runtime.CompilerServices;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Events;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CitizenEvacuateSystem : GameSystemBase
{
	[BurstCompile]
	private struct CitizenEvacuateJob : IJobChunk
	{
		[ReadOnly]
		public uint m_UpdateFrameIndex;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;

		public BufferTypeHandle<TripNeeded> m_TripNeededType;

		[ReadOnly]
		public ComponentLookup<Dispatched> m_DispatchedData;

		[ReadOnly]
		public ComponentLookup<PublicTransport> m_PublicTransportData;

		[ReadOnly]
		public BufferLookup<ServiceDispatch> m_ServiceDispatches;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<InDanger> m_InDangerData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<CurrentBuilding> nativeArray2 = chunk.GetNativeArray(ref m_CurrentBuildingType);
			BufferAccessor<TripNeeded> bufferAccessor = chunk.GetBufferAccessor(ref m_TripNeededType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				CurrentBuilding currentBuilding = nativeArray2[i];
				if (!m_InDangerData.HasComponent(currentBuilding.m_CurrentBuilding))
				{
					continue;
				}
				InDanger inDanger = m_InDangerData[currentBuilding.m_CurrentBuilding];
				if ((inDanger.m_Flags & DangerFlags.Evacuate) == 0)
				{
					continue;
				}
				if ((inDanger.m_Flags & DangerFlags.UseTransport) != 0)
				{
					if (GetBoardingVehicle(inDanger, out var vehicle))
					{
						DynamicBuffer<TripNeeded> tripNeededs = bufferAccessor[i];
						GoToVehicle(unfilteredChunkIndex, nativeArray[i], tripNeededs, vehicle);
						if ((inDanger.m_Flags & DangerFlags.WaitingCitizens) != 0)
						{
							inDanger.m_Flags &= ~DangerFlags.WaitingCitizens;
							m_InDangerData[currentBuilding.m_CurrentBuilding] = inDanger;
						}
					}
					else if ((inDanger.m_Flags & DangerFlags.WaitingCitizens) == 0)
					{
						inDanger.m_Flags |= DangerFlags.WaitingCitizens;
						m_InDangerData[currentBuilding.m_CurrentBuilding] = inDanger;
					}
				}
				else
				{
					DynamicBuffer<TripNeeded> tripNeededs2 = bufferAccessor[i];
					GoToShelter(unfilteredChunkIndex, nativeArray[i], tripNeededs2);
				}
			}
		}

		private bool GetBoardingVehicle(InDanger inDanger, out Entity vehicle)
		{
			vehicle = Entity.Null;
			if (!m_DispatchedData.HasComponent(inDanger.m_EvacuationRequest))
			{
				return false;
			}
			Dispatched dispatched = m_DispatchedData[inDanger.m_EvacuationRequest];
			if (!m_PublicTransportData.HasComponent(dispatched.m_Handler))
			{
				return false;
			}
			if ((m_PublicTransportData[dispatched.m_Handler].m_State & PublicTransportFlags.Boarding) == 0)
			{
				return false;
			}
			if (!m_ServiceDispatches.HasBuffer(dispatched.m_Handler))
			{
				return false;
			}
			DynamicBuffer<ServiceDispatch> dynamicBuffer = m_ServiceDispatches[dispatched.m_Handler];
			if (dynamicBuffer.Length == 0 || dynamicBuffer[0].m_Request != inDanger.m_EvacuationRequest)
			{
				return false;
			}
			vehicle = dispatched.m_Handler;
			return true;
		}

		private void GoToShelter(int jobIndex, Entity entity, DynamicBuffer<TripNeeded> tripNeededs)
		{
			tripNeededs.Clear();
			tripNeededs.Add(new TripNeeded
			{
				m_Purpose = Purpose.EmergencyShelter
			});
			m_CommandBuffer.RemoveComponent<ResourceBuyer>(jobIndex, entity);
			m_CommandBuffer.RemoveComponent<TravelPurpose>(jobIndex, entity);
			m_CommandBuffer.RemoveComponent<AttendingMeeting>(jobIndex, entity);
		}

		private void GoToVehicle(int jobIndex, Entity entity, DynamicBuffer<TripNeeded> tripNeededs, Entity vehicle)
		{
			tripNeededs.Clear();
			tripNeededs.Add(new TripNeeded
			{
				m_Purpose = Purpose.EmergencyShelter,
				m_TargetAgent = vehicle
			});
			m_CommandBuffer.RemoveComponent<ResourceBuyer>(jobIndex, entity);
			m_CommandBuffer.RemoveComponent<TravelPurpose>(jobIndex, entity);
			m_CommandBuffer.RemoveComponent<AttendingMeeting>(jobIndex, entity);
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

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle;

		public BufferTypeHandle<TripNeeded> __Game_Citizens_TripNeeded_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Dispatched> __Game_Simulation_Dispatched_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PublicTransport> __Game_Vehicles_PublicTransport_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ServiceDispatch> __Game_Simulation_ServiceDispatch_RO_BufferLookup;

		public ComponentLookup<InDanger> __Game_Events_InDanger_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentBuilding>(isReadOnly: true);
			__Game_Citizens_TripNeeded_RW_BufferTypeHandle = state.GetBufferTypeHandle<TripNeeded>();
			__Game_Simulation_Dispatched_RO_ComponentLookup = state.GetComponentLookup<Dispatched>(isReadOnly: true);
			__Game_Vehicles_PublicTransport_RO_ComponentLookup = state.GetComponentLookup<PublicTransport>(isReadOnly: true);
			__Game_Simulation_ServiceDispatch_RO_BufferLookup = state.GetBufferLookup<ServiceDispatch>(isReadOnly: true);
			__Game_Events_InDanger_RW_ComponentLookup = state.GetComponentLookup<InDanger>();
		}
	}

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_InDangerQuery;

	private EntityQuery m_CitizenQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_InDangerQuery = GetEntityQuery(ComponentType.ReadWrite<InDanger>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_CitizenQuery = GetEntityQuery(ComponentType.ReadOnly<Citizen>(), ComponentType.ReadOnly<CurrentBuilding>(), ComponentType.Exclude<HealthProblem>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_InDangerQuery);
		RequireForUpdate(m_CitizenQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrameIndex = (m_SimulationSystem.frameIndex >> 4) & 0xF;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new CitizenEvacuateJob
		{
			m_UpdateFrameIndex = updateFrameIndex,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TripNeededType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_DispatchedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_Dispatched_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PublicTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PublicTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceDispatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RO_BufferLookup, ref base.CheckedStateRef),
			m_InDangerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_InDanger_RW_ComponentLookup, ref base.CheckedStateRef)
		}, m_CitizenQuery, base.Dependency);
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
	public CitizenEvacuateSystem()
	{
	}
}
