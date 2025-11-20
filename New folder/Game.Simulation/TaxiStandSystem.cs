#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Game.Areas;
using Game.City;
using Game.Common;
using Game.Prefabs;
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
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class TaxiStandSystem : GameSystemBase
{
	[BurstCompile]
	private struct TaxiStandTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<RouteLane> m_RouteLaneType;

		[ReadOnly]
		public ComponentTypeHandle<WaitingPassengers> m_WaitingPassengersType;

		public ComponentTypeHandle<TaxiStand> m_TaxiStandType;

		public BufferTypeHandle<RouteVehicle> m_RouteVehicleType;

		public BufferTypeHandle<DispatchedRequest> m_DispatchedRequestType;

		[ReadOnly]
		public ComponentLookup<TaxiRequest> m_TaxiRequestData;

		[ReadOnly]
		public ComponentLookup<CurrentRoute> m_CurrentRouteData;

		[ReadOnly]
		public ComponentLookup<Dispatched> m_DispatchedData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> m_BorderDistrictData;

		[ReadOnly]
		public ComponentLookup<Game.City.City> m_CityData;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public Entity m_City;

		[ReadOnly]
		public EntityArchetype m_TaxiRequestArchetype;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<TaxiStand> nativeArray3 = chunk.GetNativeArray(ref m_TaxiStandType);
			NativeArray<RouteLane> nativeArray4 = chunk.GetNativeArray(ref m_RouteLaneType);
			NativeArray<WaitingPassengers> nativeArray5 = chunk.GetNativeArray(ref m_WaitingPassengersType);
			BufferAccessor<RouteVehicle> bufferAccessor = chunk.GetBufferAccessor(ref m_RouteVehicleType);
			BufferAccessor<DispatchedRequest> bufferAccessor2 = chunk.GetBufferAccessor(ref m_DispatchedRequestType);
			ushort num = 0;
			if (CityUtils.CheckOption(m_CityData[m_City], CityOption.PaidTaxiStart))
			{
				float value = 0f;
				CityUtils.ApplyModifier(ref value, m_CityModifiers[m_City], CityModifierType.TaxiStartingFee);
				num = (ushort)math.clamp(Mathf.RoundToInt(value), 0, 65535);
			}
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				_ = nativeArray2[i];
				TaxiStand taxiStand = nativeArray3[i];
				WaitingPassengers waitingPassengers = nativeArray5[i];
				DynamicBuffer<RouteVehicle> vehicles = bufferAccessor[i];
				DynamicBuffer<DispatchedRequest> requests = bufferAccessor2[i];
				int maxTaxiCount = RouteUtils.GetMaxTaxiCount(waitingPassengers);
				CheckVehicles(entity, vehicles, out var count);
				CheckRequests(ref taxiStand, requests);
				if (count < maxTaxiCount)
				{
					taxiStand.m_Flags |= TaxiStandFlags.RequireVehicles;
					count += requests.Length;
					if (count < maxTaxiCount)
					{
						Entity lane = Entity.Null;
						if (nativeArray4.Length != 0)
						{
							lane = nativeArray4[i].m_EndLane;
						}
						RequestNewVehicleIfNeeded(unfilteredChunkIndex, entity, lane, taxiStand, maxTaxiCount - count);
					}
				}
				else
				{
					taxiStand.m_Flags &= ~TaxiStandFlags.RequireVehicles;
				}
				if (taxiStand.m_StartingFee != num)
				{
					taxiStand.m_StartingFee = num;
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(PathfindUpdated));
				}
				nativeArray3[i] = taxiStand;
			}
		}

		private void CheckVehicles(Entity route, DynamicBuffer<RouteVehicle> vehicles, out int count)
		{
			count = 0;
			while (count < vehicles.Length)
			{
				Entity vehicle = vehicles[count].m_Vehicle;
				CurrentRoute currentRoute = default(CurrentRoute);
				if (m_CurrentRouteData.HasComponent(vehicle))
				{
					currentRoute = m_CurrentRouteData[vehicle];
				}
				if (currentRoute.m_Route == route)
				{
					count++;
				}
				else
				{
					vehicles.RemoveAt(count);
				}
			}
		}

		private void CheckRequests(ref TaxiStand taxiStand, DynamicBuffer<DispatchedRequest> requests)
		{
			for (int i = 0; i < requests.Length; i++)
			{
				Entity vehicleRequest = requests[i].m_VehicleRequest;
				if (!m_TaxiRequestData.HasComponent(vehicleRequest))
				{
					requests.RemoveAtSwapBack(i--);
				}
			}
			if (m_DispatchedData.HasComponent(taxiStand.m_TaxiRequest))
			{
				requests.Add(new DispatchedRequest
				{
					m_VehicleRequest = taxiStand.m_TaxiRequest
				});
				taxiStand.m_TaxiRequest = Entity.Null;
			}
		}

		private void RequestNewVehicleIfNeeded(int jobIndex, Entity entity, Entity lane, TaxiStand taxiStand, int priority)
		{
			if (!m_TaxiRequestData.HasComponent(taxiStand.m_TaxiRequest))
			{
				GetDistricts(lane, out var district, out var district2);
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_TaxiRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new TaxiRequest(entity, district, district2, TaxiRequestType.Stand, priority));
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<RouteLane> __Game_Routes_RouteLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaitingPassengers> __Game_Routes_WaitingPassengers_RO_ComponentTypeHandle;

		public ComponentTypeHandle<TaxiStand> __Game_Routes_TaxiStand_RW_ComponentTypeHandle;

		public BufferTypeHandle<RouteVehicle> __Game_Routes_RouteVehicle_RW_BufferTypeHandle;

		public BufferTypeHandle<DispatchedRequest> __Game_Routes_DispatchedRequest_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<TaxiRequest> __Game_Simulation_TaxiRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentRoute> __Game_Routes_CurrentRoute_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Dispatched> __Game_Simulation_Dispatched_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BorderDistrict> __Game_Areas_BorderDistrict_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.City.City> __Game_City_City_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Routes_RouteLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<RouteLane>(isReadOnly: true);
			__Game_Routes_WaitingPassengers_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaitingPassengers>(isReadOnly: true);
			__Game_Routes_TaxiStand_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TaxiStand>();
			__Game_Routes_RouteVehicle_RW_BufferTypeHandle = state.GetBufferTypeHandle<RouteVehicle>();
			__Game_Routes_DispatchedRequest_RW_BufferTypeHandle = state.GetBufferTypeHandle<DispatchedRequest>();
			__Game_Simulation_TaxiRequest_RO_ComponentLookup = state.GetComponentLookup<TaxiRequest>(isReadOnly: true);
			__Game_Routes_CurrentRoute_RO_ComponentLookup = state.GetComponentLookup<CurrentRoute>(isReadOnly: true);
			__Game_Simulation_Dispatched_RO_ComponentLookup = state.GetComponentLookup<Dispatched>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RO_ComponentLookup = state.GetComponentLookup<CurrentDistrict>(isReadOnly: true);
			__Game_Areas_BorderDistrict_RO_ComponentLookup = state.GetComponentLookup<BorderDistrict>(isReadOnly: true);
			__Game_City_City_RO_ComponentLookup = state.GetComponentLookup<Game.City.City>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
		}
	}

	public const uint UPDATE_INTERVAL = 256u;

	private EntityQuery m_StandQuery;

	private EntityArchetype m_VehicleRequestArchetype;

	private CitySystem m_CitySystem;

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
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_StandQuery = GetEntityQuery(ComponentType.ReadWrite<TaxiStand>(), ComponentType.ReadOnly<Game.Routes.TransportStop>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_VehicleRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<TaxiRequest>(), ComponentType.ReadWrite<RequestGroup>());
		RequireForUpdate(m_StandQuery);
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new TaxiStandTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RouteLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_RouteLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaitingPassengersType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_WaitingPassengers_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TaxiStandType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_TaxiStand_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RouteVehicleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_RouteVehicle_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_DispatchedRequestType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_DispatchedRequest_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_TaxiRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_TaxiRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentRouteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_CurrentRoute_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DispatchedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_Dispatched_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BorderDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_BorderDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_City_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_City = m_CitySystem.City,
			m_TaxiRequestArchetype = m_VehicleRequestArchetype,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_StandQuery, base.Dependency);
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
	public TaxiStandSystem()
	{
	}
}
