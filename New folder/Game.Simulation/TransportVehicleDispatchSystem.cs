using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
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
public class TransportVehicleDispatchSystem : GameSystemBase
{
	private struct VehicleDispatch
	{
		public Entity m_Request;

		public Entity m_Source;

		public VehicleDispatch(Entity request, Entity source)
		{
			m_Request = request;
			m_Source = source;
		}
	}

	[BurstCompile]
	private struct TransportVehicleDispatchJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<TransportVehicleRequest> m_TransportVehicleRequestType;

		[ReadOnly]
		public ComponentTypeHandle<Dispatched> m_DispatchedType;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> m_PathInformationType;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		public ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

		[ReadOnly]
		public ComponentLookup<TransportVehicleRequest> m_TransportVehicleRequestData;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<ParkedTrain> m_ParkedTrainData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<RouteConnectionData> m_PrefabRouteConnectionData;

		[ReadOnly]
		public ComponentLookup<TransportDepotData> m_PrefabTransportDepotData;

		[ReadOnly]
		public ComponentLookup<CarData> m_PrefabCarData;

		[ReadOnly]
		public ComponentLookup<WatercraftData> m_PrefabWatercraftData;

		[ReadOnly]
		public ComponentLookup<AircraftData> m_PrefabAircraftData;

		[ReadOnly]
		public ComponentLookup<AirplaneData> m_PrefabAirplaneData;

		[ReadOnly]
		public ComponentLookup<HelicopterData> m_PrefabHelicopterData;

		[ReadOnly]
		public ComponentLookup<TrainData> m_PrefabTrainData;

		[ReadOnly]
		public BufferLookup<ServiceDispatch> m_ServiceDispatches;

		[ReadOnly]
		public BufferLookup<DispatchedRequest> m_DispatchedRequests;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<TransportLine> m_TransportLineData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Game.Buildings.TransportDepot> m_TransportDepotData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Game.Vehicles.PublicTransport> m_PublicTransportData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Game.Vehicles.CargoTransport> m_CargoTransportData;

		[ReadOnly]
		public uint m_UpdateFrameIndex;

		[ReadOnly]
		public uint m_NextUpdateFrameIndex;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<VehicleDispatch>.ParallelWriter m_VehicleDispatches;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			uint index = chunk.GetSharedComponent(m_UpdateFrameType).m_Index;
			if (index == m_NextUpdateFrameIndex && !chunk.Has(ref m_DispatchedType) && !chunk.Has(ref m_PathInformationType))
			{
				NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
				NativeArray<TransportVehicleRequest> nativeArray2 = chunk.GetNativeArray(ref m_TransportVehicleRequestType);
				NativeArray<ServiceRequest> nativeArray3 = chunk.GetNativeArray(ref m_ServiceRequestType);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Entity entity = nativeArray[i];
					TransportVehicleRequest transportVehicleRequest = nativeArray2[i];
					ServiceRequest serviceRequest = nativeArray3[i];
					if ((serviceRequest.m_Flags & ServiceRequestFlags.Reversed) != 0)
					{
						if (!ValidateReversed(entity, transportVehicleRequest.m_Route))
						{
							m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
							continue;
						}
						if (SimulationUtils.TickServiceRequest(ref serviceRequest))
						{
							FindVehicleTarget(unfilteredChunkIndex, entity, transportVehicleRequest.m_Route);
						}
					}
					else
					{
						if (!ValidateTarget(entity, transportVehicleRequest.m_Route, dispatched: false))
						{
							m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity);
							continue;
						}
						if (SimulationUtils.TickServiceRequest(ref serviceRequest))
						{
							FindVehicleSource(unfilteredChunkIndex, entity, transportVehicleRequest.m_Route);
						}
					}
					nativeArray3[i] = serviceRequest;
				}
			}
			if (index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Dispatched> nativeArray4 = chunk.GetNativeArray(ref m_DispatchedType);
			NativeArray<TransportVehicleRequest> nativeArray5 = chunk.GetNativeArray(ref m_TransportVehicleRequestType);
			NativeArray<ServiceRequest> nativeArray6 = chunk.GetNativeArray(ref m_ServiceRequestType);
			if (nativeArray4.Length != 0)
			{
				NativeArray<Entity> nativeArray7 = chunk.GetNativeArray(m_EntityType);
				for (int j = 0; j < nativeArray4.Length; j++)
				{
					Entity entity2 = nativeArray7[j];
					Dispatched dispatched = nativeArray4[j];
					TransportVehicleRequest transportVehicleRequest2 = nativeArray5[j];
					ServiceRequest serviceRequest2 = nativeArray6[j];
					if (ValidateHandler(entity2, dispatched.m_Handler))
					{
						serviceRequest2.m_Cooldown = 0;
					}
					else if (serviceRequest2.m_Cooldown == 0)
					{
						serviceRequest2.m_Cooldown = 1;
					}
					else
					{
						if (!ValidateTarget(entity2, transportVehicleRequest2.m_Route, dispatched: true))
						{
							m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity2);
							continue;
						}
						ResetFailedRequest(unfilteredChunkIndex, entity2, dispatched: true, ref serviceRequest2);
					}
					nativeArray6[j] = serviceRequest2;
				}
				return;
			}
			NativeArray<PathInformation> nativeArray8 = chunk.GetNativeArray(ref m_PathInformationType);
			if (nativeArray8.Length == 0)
			{
				return;
			}
			NativeArray<Entity> nativeArray9 = chunk.GetNativeArray(m_EntityType);
			for (int k = 0; k < nativeArray5.Length; k++)
			{
				Entity entity3 = nativeArray9[k];
				TransportVehicleRequest transportVehicleRequest3 = nativeArray5[k];
				PathInformation pathInformation = nativeArray8[k];
				ServiceRequest serviceRequest3 = nativeArray6[k];
				if ((serviceRequest3.m_Flags & ServiceRequestFlags.Reversed) != 0)
				{
					if (!ValidateReversed(entity3, transportVehicleRequest3.m_Route))
					{
						m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity3);
						continue;
					}
					if (pathInformation.m_Destination != Entity.Null)
					{
						ResetReverseRequest(unfilteredChunkIndex, entity3, pathInformation, ref serviceRequest3);
					}
					else
					{
						ResetFailedRequest(unfilteredChunkIndex, entity3, dispatched: false, ref serviceRequest3);
					}
				}
				else
				{
					if (!ValidateTarget(entity3, transportVehicleRequest3.m_Route, dispatched: false))
					{
						m_CommandBuffer.DestroyEntity(unfilteredChunkIndex, entity3);
						continue;
					}
					if (pathInformation.m_Origin != Entity.Null)
					{
						DispatchVehicle(unfilteredChunkIndex, entity3, pathInformation);
					}
					else
					{
						ResetFailedRequest(unfilteredChunkIndex, entity3, dispatched: false, ref serviceRequest3);
					}
				}
				nativeArray6[k] = serviceRequest3;
			}
		}

		private bool ValidateReversed(Entity entity, Entity source)
		{
			if (m_TransportDepotData.TryGetComponent(source, out var componentData))
			{
				if ((componentData.m_Flags & TransportDepotFlags.HasAvailableVehicles) == 0)
				{
					return false;
				}
				if (componentData.m_TargetRequest != entity)
				{
					if (m_TransportVehicleRequestData.HasComponent(componentData.m_TargetRequest))
					{
						return false;
					}
					componentData.m_TargetRequest = entity;
					m_TransportDepotData[source] = componentData;
				}
				return true;
			}
			if (m_PublicTransportData.TryGetComponent(source, out var componentData2))
			{
				if ((componentData2.m_State & (PublicTransportFlags.EnRoute | PublicTransportFlags.RequiresMaintenance | PublicTransportFlags.Disabled)) != 0 || componentData2.m_RequestCount > 0 || m_ParkedCarData.HasComponent(source))
				{
					return false;
				}
				if (componentData2.m_TargetRequest != entity)
				{
					if (m_TransportVehicleRequestData.HasComponent(componentData2.m_TargetRequest))
					{
						return false;
					}
					componentData2.m_TargetRequest = entity;
					m_PublicTransportData[source] = componentData2;
				}
				return true;
			}
			if (m_CargoTransportData.TryGetComponent(source, out var componentData3))
			{
				if ((componentData3.m_State & (CargoTransportFlags.EnRoute | CargoTransportFlags.RequiresMaintenance | CargoTransportFlags.Disabled)) != 0 || componentData3.m_RequestCount > 0 || m_ParkedCarData.HasComponent(source) || m_ParkedTrainData.HasComponent(source))
				{
					return false;
				}
				if (componentData3.m_TargetRequest != entity)
				{
					if (m_TransportVehicleRequestData.HasComponent(componentData3.m_TargetRequest))
					{
						return false;
					}
					componentData3.m_TargetRequest = entity;
					m_CargoTransportData[source] = componentData3;
				}
				return true;
			}
			return false;
		}

		private bool ValidateHandler(Entity entity, Entity handler)
		{
			if (m_ServiceDispatches.TryGetBuffer(handler, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					if (bufferData[i].m_Request == entity)
					{
						return true;
					}
				}
			}
			return false;
		}

		private bool ValidateTarget(Entity entity, Entity target, bool dispatched)
		{
			if (!m_TransportLineData.TryGetComponent(target, out var componentData))
			{
				return false;
			}
			if ((componentData.m_Flags & TransportLineFlags.RequireVehicles) == 0)
			{
				return false;
			}
			if (m_DispatchedRequests.TryGetBuffer(target, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					if (bufferData[i].m_VehicleRequest == entity)
					{
						return !dispatched;
					}
				}
			}
			if (componentData.m_VehicleRequest != entity)
			{
				if (m_TransportVehicleRequestData.HasComponent(componentData.m_VehicleRequest))
				{
					return false;
				}
				componentData.m_VehicleRequest = entity;
				m_TransportLineData[target] = componentData;
			}
			return true;
		}

		private void ResetReverseRequest(int jobIndex, Entity entity, PathInformation pathInformation, ref ServiceRequest serviceRequest)
		{
			VehicleDispatch value = new VehicleDispatch(entity, pathInformation.m_Destination);
			m_VehicleDispatches.Enqueue(value);
			SimulationUtils.ResetReverseRequest(ref serviceRequest);
			m_CommandBuffer.RemoveComponent<PathInformation>(jobIndex, entity);
		}

		private void ResetFailedRequest(int jobIndex, Entity entity, bool dispatched, ref ServiceRequest serviceRequest)
		{
			SimulationUtils.ResetFailedRequest(ref serviceRequest);
			m_CommandBuffer.RemoveComponent<PathInformation>(jobIndex, entity);
			m_CommandBuffer.RemoveComponent<PathElement>(jobIndex, entity);
			if (dispatched)
			{
				m_CommandBuffer.RemoveComponent<Dispatched>(jobIndex, entity);
			}
		}

		private void DispatchVehicle(int jobIndex, Entity entity, PathInformation pathInformation)
		{
			Entity entity2 = pathInformation.m_Origin;
			if ((m_ParkedCarData.HasComponent(entity2) || m_ParkedTrainData.HasComponent(entity2)) && m_OwnerData.TryGetComponent(entity2, out var componentData))
			{
				entity2 = componentData.m_Owner;
			}
			VehicleDispatch value = new VehicleDispatch(entity, entity2);
			m_VehicleDispatches.Enqueue(value);
			m_CommandBuffer.AddComponent(jobIndex, entity, new Dispatched(entity2));
		}

		private void FindVehicleSource(int jobIndex, Entity requestEntity, Entity targetRoute)
		{
			PrefabRef prefabRef = m_PrefabRefData[targetRoute];
			RouteConnectionData routeConnectionData = m_PrefabRouteConnectionData[prefabRef.m_Prefab];
			PathMethod pathMethods = RouteUtils.GetPathMethods(routeConnectionData.m_RouteConnectionType, RouteType.TransportLine, routeConnectionData.m_RouteTrackType, routeConnectionData.m_RouteRoadType, routeConnectionData.m_RouteSizeClass);
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = 277.77777f,
				m_WalkSpeed = 5.555556f,
				m_Weights = new PathfindWeights(1f, 1f, 1f, 1f),
				m_Methods = pathMethods,
				m_IgnoredRules = (RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidPrivateTraffic | RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles),
				m_PathfindFlags = PathfindFlags.IgnoreExtraEndAccessRequirements
			};
			SetupQueueTarget origin = new SetupQueueTarget
			{
				m_Type = SetupTargetType.TransportVehicle,
				m_Methods = pathMethods,
				m_TrackTypes = routeConnectionData.m_RouteTrackType,
				m_RoadTypes = routeConnectionData.m_RouteRoadType
			};
			SetupQueueTarget destination = new SetupQueueTarget
			{
				m_Type = SetupTargetType.RouteWaypoints,
				m_Methods = pathMethods,
				m_TrackTypes = routeConnectionData.m_RouteTrackType,
				m_RoadTypes = routeConnectionData.m_RouteRoadType,
				m_Entity = targetRoute
			};
			m_PathfindQueue.Enqueue(new SetupQueueItem(requestEntity, parameters, origin, destination));
			m_CommandBuffer.AddComponent(jobIndex, requestEntity, default(PathInformation));
			m_CommandBuffer.AddBuffer<PathElement>(jobIndex, requestEntity);
		}

		private void FindVehicleTarget(int jobIndex, Entity requestEntity, Entity vehicleSource)
		{
			PrefabRef prefabRef = m_PrefabRefData[vehicleSource];
			PathMethod methods = ~(PathMethod.Pedestrian | PathMethod.Road | PathMethod.Parking | PathMethod.PublicTransportDay | PathMethod.Track | PathMethod.Taxi | PathMethod.CargoTransport | PathMethod.CargoLoading | PathMethod.Flying | PathMethod.PublicTransportNight | PathMethod.Boarding | PathMethod.Offroad | PathMethod.SpecialParking | PathMethod.MediumRoad | PathMethod.Bicycle | PathMethod.BicycleParking);
			TrackTypes trackTypes = TrackTypes.None;
			RoadTypes roadTypes = RoadTypes.None;
			CarData componentData2;
			WatercraftData componentData3;
			TrainData componentData4;
			if (m_PrefabTransportDepotData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
			{
				switch (componentData.m_TransportType)
				{
				case TransportType.Bus:
					methods = VehicleUtils.GetPathMethods(new CarData
					{
						m_SizeClass = componentData.m_SizeClass
					});
					roadTypes = RoadTypes.Car;
					break;
				case TransportType.Train:
					methods = PathMethod.Track;
					trackTypes = TrackTypes.Train;
					break;
				case TransportType.Tram:
					methods = PathMethod.Track;
					trackTypes = TrackTypes.Tram;
					break;
				case TransportType.Subway:
					methods = PathMethod.Track;
					trackTypes = TrackTypes.Subway;
					break;
				case TransportType.Ship:
					methods = VehicleUtils.GetPathMethods(new WatercraftData
					{
						m_SizeClass = componentData.m_SizeClass
					});
					roadTypes = RoadTypes.Watercraft;
					break;
				case TransportType.Helicopter:
					methods = VehicleUtils.GetPathMethods(new AircraftData
					{
						m_SizeClass = componentData.m_SizeClass
					});
					roadTypes = RoadTypes.Helicopter;
					break;
				case TransportType.Airplane:
					methods = VehicleUtils.GetPathMethods(new AircraftData
					{
						m_SizeClass = componentData.m_SizeClass
					});
					roadTypes = RoadTypes.Airplane;
					break;
				case TransportType.Ferry:
					methods = VehicleUtils.GetPathMethods(new WatercraftData
					{
						m_SizeClass = componentData.m_SizeClass
					});
					roadTypes = RoadTypes.Watercraft;
					break;
				}
			}
			else if (m_PrefabCarData.TryGetComponent(prefabRef.m_Prefab, out componentData2))
			{
				methods = VehicleUtils.GetPathMethods(componentData2);
				roadTypes = RoadTypes.Car;
			}
			else if (m_PrefabWatercraftData.TryGetComponent(prefabRef.m_Prefab, out componentData3))
			{
				methods = VehicleUtils.GetPathMethods(componentData3);
				roadTypes = RoadTypes.Watercraft;
			}
			else if (m_PrefabAirplaneData.HasComponent(prefabRef.m_Prefab))
			{
				methods = VehicleUtils.GetPathMethods(m_PrefabAircraftData[prefabRef.m_Prefab]);
				roadTypes = RoadTypes.Airplane;
			}
			else if (m_PrefabHelicopterData.HasComponent(prefabRef.m_Prefab))
			{
				methods = VehicleUtils.GetPathMethods(m_PrefabAircraftData[prefabRef.m_Prefab]);
				roadTypes = RoadTypes.Helicopter;
			}
			else if (m_PrefabTrainData.TryGetComponent(prefabRef.m_Prefab, out componentData4))
			{
				methods = PathMethod.Track;
				trackTypes = componentData4.m_TrackType;
			}
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = 277.77777f,
				m_WalkSpeed = 5.555556f,
				m_Weights = new PathfindWeights(1f, 1f, 1f, 1f),
				m_Methods = methods,
				m_IgnoredRules = (RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidPrivateTraffic | RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
			};
			SetupQueueTarget origin = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = methods,
				m_TrackTypes = trackTypes,
				m_RoadTypes = roadTypes,
				m_Entity = vehicleSource
			};
			SetupQueueTarget destination = new SetupQueueTarget
			{
				m_Type = SetupTargetType.TransportVehicleRequest,
				m_Methods = methods,
				m_TrackTypes = trackTypes,
				m_RoadTypes = roadTypes
			};
			m_PathfindQueue.Enqueue(new SetupQueueItem(requestEntity, parameters, origin, destination));
			m_CommandBuffer.AddComponent(jobIndex, requestEntity, default(PathInformation));
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct DispatchVehiclesJob : IJob
	{
		public NativeQueue<VehicleDispatch> m_VehicleDispatches;

		public ComponentLookup<ServiceRequest> m_ServiceRequestData;

		public BufferLookup<ServiceDispatch> m_ServiceDispatches;

		public void Execute()
		{
			VehicleDispatch item;
			while (m_VehicleDispatches.TryDequeue(out item))
			{
				ServiceRequest componentData;
				if (m_ServiceDispatches.TryGetBuffer(item.m_Source, out var bufferData))
				{
					bufferData.Add(new ServiceDispatch(item.m_Request));
				}
				else if (m_ServiceRequestData.TryGetComponent(item.m_Source, out componentData))
				{
					componentData.m_Flags |= ServiceRequestFlags.SkipCooldown;
					m_ServiceRequestData[item.m_Source] = componentData;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TransportVehicleRequest> __Game_Simulation_TransportVehicleRequest_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Dispatched> __Game_Simulation_Dispatched_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentTypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		public ComponentTypeHandle<ServiceRequest> __Game_Simulation_ServiceRequest_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<TransportVehicleRequest> __Game_Simulation_TransportVehicleRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedTrain> __Game_Vehicles_ParkedTrain_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteConnectionData> __Game_Prefabs_RouteConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportDepotData> __Game_Prefabs_TransportDepotData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WatercraftData> __Game_Prefabs_WatercraftData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AircraftData> __Game_Prefabs_AircraftData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AirplaneData> __Game_Prefabs_AirplaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HelicopterData> __Game_Prefabs_HelicopterData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrainData> __Game_Prefabs_TrainData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ServiceDispatch> __Game_Simulation_ServiceDispatch_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<DispatchedRequest> __Game_Routes_DispatchedRequest_RO_BufferLookup;

		public ComponentLookup<TransportLine> __Game_Routes_TransportLine_RW_ComponentLookup;

		public ComponentLookup<Game.Buildings.TransportDepot> __Game_Buildings_TransportDepot_RW_ComponentLookup;

		public ComponentLookup<Game.Vehicles.PublicTransport> __Game_Vehicles_PublicTransport_RW_ComponentLookup;

		public ComponentLookup<Game.Vehicles.CargoTransport> __Game_Vehicles_CargoTransport_RW_ComponentLookup;

		public ComponentLookup<ServiceRequest> __Game_Simulation_ServiceRequest_RW_ComponentLookup;

		public BufferLookup<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_TransportVehicleRequest_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TransportVehicleRequest>(isReadOnly: true);
			__Game_Simulation_Dispatched_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Dispatched>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathInformation>(isReadOnly: true);
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Simulation_ServiceRequest_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceRequest>();
			__Game_Simulation_TransportVehicleRequest_RO_ComponentLookup = state.GetComponentLookup<TransportVehicleRequest>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Vehicles_ParkedTrain_RO_ComponentLookup = state.GetComponentLookup<ParkedTrain>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_RouteConnectionData_RO_ComponentLookup = state.GetComponentLookup<RouteConnectionData>(isReadOnly: true);
			__Game_Prefabs_TransportDepotData_RO_ComponentLookup = state.GetComponentLookup<TransportDepotData>(isReadOnly: true);
			__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(isReadOnly: true);
			__Game_Prefabs_WatercraftData_RO_ComponentLookup = state.GetComponentLookup<WatercraftData>(isReadOnly: true);
			__Game_Prefabs_AircraftData_RO_ComponentLookup = state.GetComponentLookup<AircraftData>(isReadOnly: true);
			__Game_Prefabs_AirplaneData_RO_ComponentLookup = state.GetComponentLookup<AirplaneData>(isReadOnly: true);
			__Game_Prefabs_HelicopterData_RO_ComponentLookup = state.GetComponentLookup<HelicopterData>(isReadOnly: true);
			__Game_Prefabs_TrainData_RO_ComponentLookup = state.GetComponentLookup<TrainData>(isReadOnly: true);
			__Game_Simulation_ServiceDispatch_RO_BufferLookup = state.GetBufferLookup<ServiceDispatch>(isReadOnly: true);
			__Game_Routes_DispatchedRequest_RO_BufferLookup = state.GetBufferLookup<DispatchedRequest>(isReadOnly: true);
			__Game_Routes_TransportLine_RW_ComponentLookup = state.GetComponentLookup<TransportLine>();
			__Game_Buildings_TransportDepot_RW_ComponentLookup = state.GetComponentLookup<Game.Buildings.TransportDepot>();
			__Game_Vehicles_PublicTransport_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PublicTransport>();
			__Game_Vehicles_CargoTransport_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.CargoTransport>();
			__Game_Simulation_ServiceRequest_RW_ComponentLookup = state.GetComponentLookup<ServiceRequest>();
			__Game_Simulation_ServiceDispatch_RW_BufferLookup = state.GetBufferLookup<ServiceDispatch>();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private EntityQuery m_RequestQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_RequestQuery = GetEntityQuery(ComponentType.ReadOnly<TransportVehicleRequest>(), ComponentType.ReadOnly<UpdateFrame>());
		RequireForUpdate(m_RequestQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint num = (m_SimulationSystem.frameIndex >> 4) & 7;
		uint nextUpdateFrameIndex = (num + 4) & 7;
		NativeQueue<VehicleDispatch> vehicleDispatches = new NativeQueue<VehicleDispatch>(Allocator.TempJob);
		TransportVehicleDispatchJob jobData = new TransportVehicleDispatchJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TransportVehicleRequestType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_TransportVehicleRequest_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DispatchedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_Dispatched_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathInformationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceRequestType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceRequest_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransportVehicleRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_TransportVehicleRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedTrain_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRouteConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RouteConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTransportDepotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportDepotData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabWatercraftData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WatercraftData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabAircraftData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AircraftData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabAirplaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AirplaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabHelicopterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_HelicopterData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrainData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceDispatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RO_BufferLookup, ref base.CheckedStateRef),
			m_DispatchedRequests = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_DispatchedRequest_RO_BufferLookup, ref base.CheckedStateRef),
			m_TransportLineData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_TransportLine_RW_ComponentLookup, ref base.CheckedStateRef),
			m_TransportDepotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_TransportDepot_RW_ComponentLookup, ref base.CheckedStateRef),
			m_PublicTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PublicTransport_RW_ComponentLookup, ref base.CheckedStateRef),
			m_CargoTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CargoTransport_RW_ComponentLookup, ref base.CheckedStateRef),
			m_UpdateFrameIndex = num,
			m_NextUpdateFrameIndex = nextUpdateFrameIndex,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_VehicleDispatches = vehicleDispatches.AsParallelWriter(),
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter()
		};
		DispatchVehiclesJob jobData2 = new DispatchVehiclesJob
		{
			m_VehicleDispatches = vehicleDispatches,
			m_ServiceRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ServiceRequest_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceDispatches = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferLookup, ref base.CheckedStateRef)
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_RequestQuery, base.Dependency);
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
		vehicleDispatches.Dispose(jobHandle2);
		m_PathfindSetupSystem.AddQueueWriter(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle2;
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
	public TransportVehicleDispatchSystem()
	{
	}
}
