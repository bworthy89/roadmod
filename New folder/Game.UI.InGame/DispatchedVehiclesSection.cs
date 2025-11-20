using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Citizens;
using Game.Common;
using Game.Events;
using Game.Simulation;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class DispatchedVehiclesSection : InfoSectionBase
{
	[BurstCompile]
	private struct CollectDispatchedVehiclesJob : IJobChunk
	{
		[ReadOnly]
		public Entity m_SelectedEntity;

		[ReadOnly]
		public EntityTypeHandle m_EntityHandle;

		[ReadOnly]
		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchHandle;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> m_CurrentBuildingFromEntity;

		[ReadOnly]
		public ComponentLookup<FireRescueRequest> m_FireRequestFromEntity;

		[ReadOnly]
		public ComponentLookup<PoliceEmergencyRequest> m_PoliceRequestFromEntity;

		[ReadOnly]
		public ComponentLookup<HealthcareRequest> m_HealthcareRequestFromEntity;

		[ReadOnly]
		public ComponentLookup<EvacuationRequest> m_EvacuationRequestFromEntity;

		[ReadOnly]
		public ComponentLookup<GarbageCollectionRequest> m_GarbageCollectionRequest;

		[ReadOnly]
		public ComponentLookup<MaintenanceRequest> m_MaintenanceRequest;

		[ReadOnly]
		public ComponentLookup<OnFire> m_OnFireFromEntity;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedFromEntity;

		[ReadOnly]
		public ComponentLookup<AccidentSite> m_AccidentSiteFromEntity;

		[ReadOnly]
		public ComponentLookup<InDanger> m_InDangerFromEntity;

		[ReadOnly]
		public ComponentLookup<FireEngine> m_FireEngineFromEntity;

		[ReadOnly]
		public ComponentLookup<PoliceCar> m_PoliceCarFromEntity;

		[ReadOnly]
		public ComponentLookup<Ambulance> m_AmbulanceFromEntity;

		[ReadOnly]
		public ComponentLookup<PublicTransport> m_PublicTransportFromEntity;

		[ReadOnly]
		public ComponentLookup<Hearse> m_HearseFromEntity;

		[ReadOnly]
		public ComponentLookup<MaintenanceVehicle> m_MaintenanceVehicleFromEntity;

		public NativeList<Entity> m_VehiclesResult;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<ServiceDispatch> bufferAccessor = chunk.GetBufferAccessor(ref m_ServiceDispatchHandle);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityHandle);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity value = nativeArray[i];
				if (m_VehiclesResult.Contains(value))
				{
					continue;
				}
				int y = 0;
				if (m_FireEngineFromEntity.TryGetComponent(value, out var componentData))
				{
					y = componentData.m_RequestCount;
				}
				if (m_PoliceCarFromEntity.TryGetComponent(value, out var componentData2))
				{
					y = componentData2.m_RequestCount;
				}
				if (m_AmbulanceFromEntity.TryGetComponent(value, out var componentData3) && (componentData3.m_State & AmbulanceFlags.Dispatched) != 0)
				{
					y = 1;
				}
				if (m_PublicTransportFromEntity.TryGetComponent(value, out var componentData4))
				{
					y = componentData4.m_RequestCount;
				}
				if (m_HearseFromEntity.TryGetComponent(value, out var componentData5) && (componentData5.m_State & HearseFlags.Dispatched) != 0)
				{
					y = 1;
				}
				if (m_MaintenanceVehicleFromEntity.TryGetComponent(value, out var componentData6))
				{
					y = componentData6.m_RequestCount;
				}
				DynamicBuffer<ServiceDispatch> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < math.min(dynamicBuffer.Length, y); j++)
				{
					ServiceDispatch serviceDispatch = dynamicBuffer[j];
					if (m_FireRequestFromEntity.TryGetComponent(serviceDispatch.m_Request, out var componentData7) && (componentData7.m_Target == m_SelectedEntity || (m_OnFireFromEntity.TryGetComponent(componentData7.m_Target, out var componentData8) && componentData8.m_Event == m_SelectedEntity) || (m_DestroyedFromEntity.TryGetComponent(componentData7.m_Target, out var componentData9) && componentData9.m_Event == m_SelectedEntity)))
					{
						m_VehiclesResult.Add(in value);
					}
					if (m_PoliceRequestFromEntity.TryGetComponent(serviceDispatch.m_Request, out var componentData10) && (componentData10.m_Target == m_SelectedEntity || (m_AccidentSiteFromEntity.TryGetComponent(componentData10.m_Target, out var componentData11) && componentData11.m_Event == m_SelectedEntity)))
					{
						m_VehiclesResult.Add(in value);
					}
					if (m_HealthcareRequestFromEntity.TryGetComponent(serviceDispatch.m_Request, out var componentData12) && (componentData12.m_Citizen == m_SelectedEntity || (m_CurrentBuildingFromEntity.TryGetComponent(componentData12.m_Citizen, out var componentData13) && componentData13.m_CurrentBuilding == m_SelectedEntity)))
					{
						m_VehiclesResult.Add(in value);
					}
					if (m_EvacuationRequestFromEntity.TryGetComponent(serviceDispatch.m_Request, out var componentData14) && (componentData14.m_Target == m_SelectedEntity || (m_InDangerFromEntity.TryGetComponent(componentData14.m_Target, out var componentData15) && componentData15.m_Event == m_SelectedEntity)))
					{
						m_VehiclesResult.Add(in value);
					}
					if (m_GarbageCollectionRequest.TryGetComponent(serviceDispatch.m_Request, out var componentData16) && componentData16.m_Target == m_SelectedEntity)
					{
						m_VehiclesResult.Add(in value);
					}
					if (m_MaintenanceRequest.TryGetComponent(serviceDispatch.m_Request, out var componentData17) && componentData17.m_Target == m_SelectedEntity)
					{
						m_VehiclesResult.Add(in value);
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
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public BufferTypeHandle<ServiceDispatch> __Game_Simulation_ServiceDispatch_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<FireRescueRequest> __Game_Simulation_FireRescueRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PoliceEmergencyRequest> __Game_Simulation_PoliceEmergencyRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthcareRequest> __Game_Simulation_HealthcareRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EvacuationRequest> __Game_Simulation_EvacuationRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarbageCollectionRequest> __Game_Simulation_GarbageCollectionRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OnFire> __Game_Events_OnFire_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AccidentSite> __Game_Events_AccidentSite_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<InDanger> __Game_Events_InDanger_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<FireEngine> __Game_Vehicles_FireEngine_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Ambulance> __Game_Vehicles_Ambulance_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PoliceCar> __Game_Vehicles_PoliceCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PublicTransport> __Game_Vehicles_PublicTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Hearse> __Game_Vehicles_Hearse_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MaintenanceVehicle> __Game_Vehicles_MaintenanceVehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MaintenanceRequest> __Game_Simulation_MaintenanceRequest_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_ServiceDispatch_RO_BufferTypeHandle = state.GetBufferTypeHandle<ServiceDispatch>(isReadOnly: true);
			__Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(isReadOnly: true);
			__Game_Simulation_FireRescueRequest_RO_ComponentLookup = state.GetComponentLookup<FireRescueRequest>(isReadOnly: true);
			__Game_Simulation_PoliceEmergencyRequest_RO_ComponentLookup = state.GetComponentLookup<PoliceEmergencyRequest>(isReadOnly: true);
			__Game_Simulation_HealthcareRequest_RO_ComponentLookup = state.GetComponentLookup<HealthcareRequest>(isReadOnly: true);
			__Game_Simulation_EvacuationRequest_RO_ComponentLookup = state.GetComponentLookup<EvacuationRequest>(isReadOnly: true);
			__Game_Simulation_GarbageCollectionRequest_RO_ComponentLookup = state.GetComponentLookup<GarbageCollectionRequest>(isReadOnly: true);
			__Game_Events_OnFire_RO_ComponentLookup = state.GetComponentLookup<OnFire>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Events_AccidentSite_RO_ComponentLookup = state.GetComponentLookup<AccidentSite>(isReadOnly: true);
			__Game_Events_InDanger_RO_ComponentLookup = state.GetComponentLookup<InDanger>(isReadOnly: true);
			__Game_Vehicles_FireEngine_RO_ComponentLookup = state.GetComponentLookup<FireEngine>(isReadOnly: true);
			__Game_Vehicles_Ambulance_RO_ComponentLookup = state.GetComponentLookup<Ambulance>(isReadOnly: true);
			__Game_Vehicles_PoliceCar_RO_ComponentLookup = state.GetComponentLookup<PoliceCar>(isReadOnly: true);
			__Game_Vehicles_PublicTransport_RO_ComponentLookup = state.GetComponentLookup<PublicTransport>(isReadOnly: true);
			__Game_Vehicles_Hearse_RO_ComponentLookup = state.GetComponentLookup<Hearse>(isReadOnly: true);
			__Game_Vehicles_MaintenanceVehicle_RO_ComponentLookup = state.GetComponentLookup<MaintenanceVehicle>(isReadOnly: true);
			__Game_Simulation_MaintenanceRequest_RO_ComponentLookup = state.GetComponentLookup<MaintenanceRequest>(isReadOnly: true);
		}
	}

	private EntityQuery m_ServiceDispatchQuery;

	private NativeList<Entity> m_VehiclesResult;

	private TypeHandle __TypeHandle;

	protected override string group => "DispatchedVehiclesSection";

	protected override bool displayForDestroyedObjects => true;

	protected override bool displayForUpgrades => true;

	private NativeList<VehiclesSection.UIVehicle> vehicleList { get; set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ServiceDispatchQuery = GetEntityQuery(ComponentType.ReadOnly<Vehicle>(), ComponentType.ReadOnly<ServiceDispatch>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_VehiclesResult = new NativeList<Entity>(5, Allocator.Persistent);
		vehicleList = new NativeList<VehiclesSection.UIVehicle>(5, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		vehicleList.Dispose();
		m_VehiclesResult.Dispose();
		base.OnDestroy();
	}

	protected override void Reset()
	{
		vehicleList.Clear();
		m_VehiclesResult.Clear();
	}

	private bool Visible()
	{
		return m_VehiclesResult.Length > 0;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobChunkExtensions.Schedule(new CollectDispatchedVehiclesJob
		{
			m_SelectedEntity = selectedEntity,
			m_EntityHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_ServiceDispatchHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_CurrentBuildingFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FireRequestFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_FireRescueRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PoliceRequestFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_PoliceEmergencyRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HealthcareRequestFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_HealthcareRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EvacuationRequestFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_EvacuationRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarbageCollectionRequest = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_GarbageCollectionRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OnFireFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_OnFire_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DestroyedFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AccidentSiteFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_AccidentSite_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InDangerFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_InDanger_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FireEngineFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_FireEngine_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AmbulanceFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Ambulance_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PoliceCarFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PoliceCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PublicTransportFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PublicTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HearseFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Hearse_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MaintenanceVehicleFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_MaintenanceVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MaintenanceRequest = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_MaintenanceRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_VehiclesResult = m_VehiclesResult
		}, m_ServiceDispatchQuery, base.Dependency).Complete();
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		for (int i = 0; i < m_VehiclesResult.Length; i++)
		{
			Entity vehicle = m_VehiclesResult[i];
			VehiclesSection.AddVehicle(base.EntityManager, vehicle, vehicleList);
		}
		vehicleList.Sort();
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("vehicleList");
		writer.ArrayBegin(vehicleList.Length);
		for (int i = 0; i < vehicleList.Length; i++)
		{
			VehiclesSection.BindVehicle(m_NameSystem, writer, vehicleList[i]);
		}
		writer.ArrayEnd();
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
	public DispatchedVehiclesSection()
	{
	}
}
