using Colossal.Collections;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Simulation;

public struct TransportPathfindSetup
{
	[BurstCompile]
	private struct SetupTransportVehiclesJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.TransportDepot> m_TransportDepotType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.CargoTransport> m_CargoTransportType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.PublicTransport> m_PublicTransportType;

		[ReadOnly]
		public ComponentTypeHandle<Controller> m_ControllerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Routes.Color> m_RouteColorType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> m_LayoutElementType;

		[ReadOnly]
		public ComponentLookup<TransportVehicleRequest> m_TransportVehicleRequestData;

		[ReadOnly]
		public ComponentLookup<Game.Routes.Color> m_RouteColorData;

		[ReadOnly]
		public ComponentLookup<TransportDepotData> m_PrefabTransportDepotData;

		[ReadOnly]
		public ComponentLookup<TransportLineData> m_TransportLineData;

		[ReadOnly]
		public ComponentLookup<MultipleUnitTrainData> m_MultipleUnitTrainData;

		[ReadOnly]
		public BufferLookup<VehicleModel> m_VehicleModels;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Buildings.TransportDepot> nativeArray2 = chunk.GetNativeArray(ref m_TransportDepotType);
			Entity entity;
			if (nativeArray2.Length != 0)
			{
				NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
				for (int i = 0; i < m_SetupData.Length; i++)
				{
					m_SetupData.GetItem(i, out entity, out var owner, out var targetSeeker);
					m_TransportVehicleRequestData.TryGetComponent(owner, out var componentData);
					TransportLineData componentData2 = default(TransportLineData);
					if (targetSeeker.m_PrefabRef.TryGetComponent(componentData.m_Route, out var componentData3))
					{
						m_TransportLineData.TryGetComponent(componentData3.m_Prefab, out componentData2);
					}
					for (int j = 0; j < nativeArray2.Length; j++)
					{
						if ((nativeArray2[j].m_Flags & TransportDepotFlags.HasAvailableVehicles) != 0)
						{
							componentData3 = nativeArray3[j];
							if (m_PrefabTransportDepotData.TryGetComponent(componentData3.m_Prefab, out var componentData4) && componentData4.m_TransportType == componentData2.m_TransportType)
							{
								float cost = targetSeeker.m_PathfindParameters.m_Weights.time * 10f;
								Entity entity2 = nativeArray[j];
								targetSeeker.FindTargets(entity2, cost);
							}
						}
					}
				}
			}
			else
			{
				if (!chunk.Has(ref m_OwnerType))
				{
					return;
				}
				NativeArray<Game.Vehicles.CargoTransport> nativeArray4 = chunk.GetNativeArray(ref m_CargoTransportType);
				NativeArray<Game.Vehicles.PublicTransport> nativeArray5 = chunk.GetNativeArray(ref m_PublicTransportType);
				NativeArray<Controller> nativeArray6 = chunk.GetNativeArray(ref m_ControllerType);
				NativeArray<Game.Routes.Color> nativeArray7 = chunk.GetNativeArray(ref m_RouteColorType);
				NativeArray<PrefabRef> nativeArray8 = chunk.GetNativeArray(ref m_PrefabRefType);
				BufferAccessor<LayoutElement> bufferAccessor = chunk.GetBufferAccessor(ref m_LayoutElementType);
				for (int k = 0; k < m_SetupData.Length; k++)
				{
					m_SetupData.GetItem(k, out entity, out var owner2, out var targetSeeker2);
					m_TransportVehicleRequestData.TryGetComponent(owner2, out var componentData5);
					TransportLineData componentData6 = default(TransportLineData);
					if (targetSeeker2.m_PrefabRef.TryGetComponent(componentData5.m_Route, out var componentData7))
					{
						m_TransportLineData.TryGetComponent(componentData7.m_Prefab, out componentData6);
					}
					DynamicBuffer<VehicleModel> bufferData;
					bool flag = m_VehicleModels.TryGetBuffer(componentData5.m_Route, out bufferData);
					Game.Routes.Color componentData8;
					bool flag2 = m_RouteColorData.TryGetComponent(componentData5.m_Route, out componentData8);
					if (nativeArray4.Length != 0 != componentData6.m_CargoTransport || nativeArray5.Length != 0 != componentData6.m_PassengerTransport)
					{
						continue;
					}
					for (int l = 0; l < nativeArray.Length; l++)
					{
						Entity entity3 = nativeArray[l];
						float num = 0f;
						if (nativeArray6.Length != 0)
						{
							Controller controller = nativeArray6[l];
							if (controller.m_Controller != Entity.Null && controller.m_Controller != entity3)
							{
								continue;
							}
						}
						if (nativeArray4.Length != 0)
						{
							Game.Vehicles.CargoTransport cargoTransport = nativeArray4[l];
							if (cargoTransport.m_RequestCount > 0 || (cargoTransport.m_State & (CargoTransportFlags.EnRoute | CargoTransportFlags.RequiresMaintenance | CargoTransportFlags.DummyTraffic | CargoTransportFlags.Disabled)) != 0)
							{
								continue;
							}
						}
						if (nativeArray5.Length != 0)
						{
							Game.Vehicles.PublicTransport publicTransport = nativeArray5[l];
							if (publicTransport.m_RequestCount > 0 || (publicTransport.m_State & (PublicTransportFlags.EnRoute | PublicTransportFlags.Evacuating | PublicTransportFlags.PrisonerTransport | PublicTransportFlags.RequiresMaintenance | PublicTransportFlags.DummyTraffic | PublicTransportFlags.Disabled)) != 0)
							{
								continue;
							}
						}
						if (flag)
						{
							componentData7 = nativeArray8[l];
							DynamicBuffer<LayoutElement> layout = default(DynamicBuffer<LayoutElement>);
							if (bufferAccessor.Length != 0)
							{
								layout = bufferAccessor[l];
							}
							if (!RouteUtils.CheckVehicleModel(bufferData, componentData7, layout, ref targetSeeker2.m_PrefabRef, ref m_MultipleUnitTrainData))
							{
								continue;
							}
						}
						if (CollectionUtils.TryGet(nativeArray7, l, out var value))
						{
							if (flag2 && componentData8.m_Color.r == value.m_Color.r && componentData8.m_Color.g == value.m_Color.g && componentData8.m_Color.b == value.m_Color.b && componentData8.m_Color.a == value.m_Color.a)
							{
								num -= targetSeeker2.m_PathfindParameters.m_Weights.time * 10f;
							}
						}
						else
						{
							num -= targetSeeker2.m_PathfindParameters.m_Weights.time * math.select(10f, 5f, flag2);
						}
						targetSeeker2.FindTargets(entity3, num);
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
	private struct SetupTaxisJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.TransportDepot> m_TransportDepotType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.Taxi> m_TaxiType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.OutsideConnection> m_OutsideConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<PathElement> m_PathElementType;

		[ReadOnly]
		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

		[ReadOnly]
		public ComponentLookup<TaxiRequest> m_TaxiRequestData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.TransportDepot> m_TransportDepotData;

		[ReadOnly]
		public ComponentLookup<TransportDepotData> m_PrefabTransportDepotData;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Buildings.TransportDepot> nativeArray2 = chunk.GetNativeArray(ref m_TransportDepotType);
			Entity entity;
			if (nativeArray2.Length != 0)
			{
				NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
				bool flag = chunk.Has(ref m_OutsideConnectionType);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Game.Buildings.TransportDepot transportDepot = nativeArray2[i];
					if ((transportDepot.m_Flags & TransportDepotFlags.HasAvailableVehicles) == 0)
					{
						continue;
					}
					for (int j = 0; j < m_SetupData.Length; j++)
					{
						m_SetupData.GetItem(j, out entity, out var owner, out var targetSeeker);
						m_TaxiRequestData.TryGetComponent(owner, out var componentData);
						switch (componentData.m_Type)
						{
						case TaxiRequestType.Stand:
							if (flag)
							{
								continue;
							}
							break;
						case TaxiRequestType.Customer:
							if ((transportDepot.m_Flags & TransportDepotFlags.HasDispatchCenter) == 0)
							{
								continue;
							}
							break;
						case TaxiRequestType.Outside:
							if (!flag)
							{
								continue;
							}
							break;
						default:
							continue;
						}
						PrefabRef prefabRef = nativeArray3[i];
						if (m_PrefabTransportDepotData[prefabRef.m_Prefab].m_TransportType == TransportType.Taxi)
						{
							Entity entity2 = nativeArray[i];
							if (AreaUtils.CheckServiceDistrict(componentData.m_District1, componentData.m_District2, entity2, m_ServiceDistricts))
							{
								float cost = targetSeeker.m_PathfindParameters.m_Weights.time * 10f;
								targetSeeker.FindTargets(entity2, cost);
							}
						}
					}
				}
				return;
			}
			NativeArray<Game.Vehicles.Taxi> nativeArray4 = chunk.GetNativeArray(ref m_TaxiType);
			if (nativeArray4.Length == 0)
			{
				return;
			}
			NativeArray<Owner> nativeArray5 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<PathOwner> nativeArray6 = chunk.GetNativeArray(ref m_PathOwnerType);
			BufferAccessor<PathElement> bufferAccessor = chunk.GetBufferAccessor(ref m_PathElementType);
			BufferAccessor<ServiceDispatch> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			for (int k = 0; k < nativeArray4.Length; k++)
			{
				Entity entity3 = nativeArray[k];
				Game.Vehicles.Taxi taxi = nativeArray4[k];
				Owner owner2 = nativeArray5[k];
				if ((taxi.m_State & (TaxiFlags.RequiresMaintenance | TaxiFlags.Dispatched | TaxiFlags.Disabled)) != 0 || !m_TransportDepotData.TryGetComponent(owner2.m_Owner, out var componentData2))
				{
					continue;
				}
				for (int l = 0; l < m_SetupData.Length; l++)
				{
					m_SetupData.GetItem(l, out entity, out var owner3, out var targetSeeker2);
					m_TaxiRequestData.TryGetComponent(owner3, out var componentData3);
					switch (componentData3.m_Type)
					{
					case TaxiRequestType.Stand:
						if (((taxi.m_State & (TaxiFlags.Returning | TaxiFlags.Transporting)) == 0 && nativeArray6.Length != 0) || (taxi.m_State & TaxiFlags.FromOutside) != 0)
						{
							continue;
						}
						break;
					case TaxiRequestType.Customer:
						if ((componentData2.m_Flags & TransportDepotFlags.HasDispatchCenter) == 0)
						{
							continue;
						}
						break;
					case TaxiRequestType.Outside:
						if ((taxi.m_State & TaxiFlags.FromOutside) == 0)
						{
							continue;
						}
						break;
					default:
						continue;
					}
					if (!AreaUtils.CheckServiceDistrict(componentData3.m_District1, componentData3.m_District2, owner2.m_Owner, m_ServiceDistricts))
					{
						continue;
					}
					if (CollectionUtils.TryGet(bufferAccessor2, k, out var value) && value.Length != 0 && (taxi.m_State & TaxiFlags.Requested) != 0)
					{
						Entity request = value[0].m_Request;
						if (m_TaxiRequestData.TryGetComponent(request, out var componentData4) && ((int)componentData3.m_Type < (int)componentData4.m_Type || (componentData3.m_Type == componentData4.m_Type && componentData3.m_Priority <= componentData4.m_Priority)))
						{
							continue;
						}
					}
					if (CollectionUtils.TryGet(nativeArray6, k, out var value2))
					{
						DynamicBuffer<PathElement> dynamicBuffer = bufferAccessor[k];
						int num = dynamicBuffer.Length - taxi.m_ExtraPathElementCount;
						if (num > dynamicBuffer.Length || (taxi.m_State & TaxiFlags.Transporting) == 0)
						{
							targetSeeker2.FindTargets(entity3, 0f);
							continue;
						}
						if (num <= value2.m_ElementIndex)
						{
							targetSeeker2.FindTargets(entity3, entity3, 0f, EdgeFlags.DefaultMask, allowAccessRestriction: true, navigationEnd: true);
							continue;
						}
						float cost2 = (float)(num - value2.m_ElementIndex) * taxi.m_PathElementTime * targetSeeker2.m_PathfindParameters.m_Weights.time;
						PathElement pathElement = dynamicBuffer[num - 1];
						targetSeeker2.m_Buffer.Enqueue(new PathTarget(entity3, pathElement.m_Target, pathElement.m_TargetDelta.y, cost2));
					}
					else
					{
						targetSeeker2.FindTargets(entity3, 0f);
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
	private struct SetupRouteWaypointsJob : IJobParallelFor
	{
		[ReadOnly]
		public BufferLookup<RouteWaypoint> m_Waypoints;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(int index)
		{
			m_SetupData.GetItem(index, out var entity, out var targetSeeker);
			if (!m_Waypoints.HasBuffer(entity))
			{
				return;
			}
			DynamicBuffer<RouteWaypoint> dynamicBuffer = m_Waypoints[entity];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity waypoint = dynamicBuffer[i].m_Waypoint;
				if (targetSeeker.m_RouteLane.HasComponent(waypoint))
				{
					RouteLane routeLane = targetSeeker.m_RouteLane[waypoint];
					if (!(routeLane.m_StartLane == Entity.Null))
					{
						targetSeeker.m_Buffer.Enqueue(new PathTarget(waypoint, routeLane.m_StartLane, routeLane.m_StartCurvePos, 0f));
					}
				}
			}
		}
	}

	[BurstCompile]
	private struct TransportVehicleRequestsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

		[ReadOnly]
		public ComponentTypeHandle<TransportVehicleRequest> m_TransportVehicleRequestType;

		[ReadOnly]
		public ComponentLookup<TransportVehicleRequest> m_TransportVehicleRequestData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PublicTransport> m_PublicTransportData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.CargoTransport> m_CargoTransportData;

		[ReadOnly]
		public ComponentLookup<TransportLineData> m_TransportLineData;

		[ReadOnly]
		public ComponentLookup<TransportDepotData> m_TransportDepotData;

		[ReadOnly]
		public ComponentLookup<MultipleUnitTrainData> m_MultipleUnitTrainData;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> m_Waypoints;

		[ReadOnly]
		public BufferLookup<VehicleModel> m_VehicleModels;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<ServiceRequest> nativeArray2 = chunk.GetNativeArray(ref m_ServiceRequestType);
			NativeArray<TransportVehicleRequest> nativeArray3 = chunk.GetNativeArray(ref m_TransportVehicleRequestType);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var _, out var owner, out var targetSeeker);
				if (!m_TransportVehicleRequestData.TryGetComponent(owner, out var componentData) || !targetSeeker.m_PrefabRef.TryGetComponent(componentData.m_Route, out var componentData2))
				{
					continue;
				}
				bool flag = false;
				bool flag2 = false;
				bool flag3 = false;
				DynamicBuffer<LayoutElement> bufferData = default(DynamicBuffer<LayoutElement>);
				if (m_TransportDepotData.TryGetComponent(componentData2.m_Prefab, out var componentData3))
				{
					flag = true;
				}
				else
				{
					flag2 = m_PublicTransportData.HasComponent(componentData.m_Route);
					flag3 = m_CargoTransportData.HasComponent(componentData.m_Route);
					targetSeeker.m_VehicleLayout.TryGetBuffer(componentData.m_Route, out bufferData);
				}
				if (!flag && !flag2 && !flag3)
				{
					continue;
				}
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					if ((nativeArray2[j].m_Flags & ServiceRequestFlags.Reversed) != 0)
					{
						continue;
					}
					TransportVehicleRequest transportVehicleRequest = nativeArray3[j];
					if (!targetSeeker.m_PrefabRef.TryGetComponent(transportVehicleRequest.m_Route, out var componentData4) || !m_TransportLineData.TryGetComponent(componentData4.m_Prefab, out var componentData5))
					{
						continue;
					}
					DynamicBuffer<VehicleModel> bufferData2;
					if (flag)
					{
						if (componentData5.m_TransportType != componentData3.m_TransportType)
						{
							continue;
						}
					}
					else if (flag3 != componentData5.m_CargoTransport || flag2 != componentData5.m_PassengerTransport || (m_VehicleModels.TryGetBuffer(transportVehicleRequest.m_Route, out bufferData2) && !RouteUtils.CheckVehicleModel(bufferData2, componentData2, bufferData, ref targetSeeker.m_PrefabRef, ref m_MultipleUnitTrainData)))
					{
						continue;
					}
					Entity target = nativeArray[j];
					DynamicBuffer<RouteWaypoint> dynamicBuffer = m_Waypoints[transportVehicleRequest.m_Route];
					for (int k = 0; k < dynamicBuffer.Length; k++)
					{
						Entity waypoint = dynamicBuffer[k].m_Waypoint;
						if (targetSeeker.m_RouteLane.HasComponent(waypoint))
						{
							RouteLane routeLane = targetSeeker.m_RouteLane[waypoint];
							if (!(routeLane.m_StartLane == Entity.Null))
							{
								targetSeeker.m_Buffer.Enqueue(new PathTarget(target, routeLane.m_StartLane, routeLane.m_StartCurvePos, 0f));
							}
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

	[BurstCompile]
	private struct TaxiRequestsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

		[ReadOnly]
		public ComponentTypeHandle<TaxiRequest> m_TaxiRequestType;

		[ReadOnly]
		public ComponentLookup<TaxiRequest> m_TaxiRequestData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.TransportDepot> m_TransportDepotData;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<ServiceRequest> nativeArray2 = chunk.GetNativeArray(ref m_ServiceRequestType);
			NativeArray<TaxiRequest> nativeArray3 = chunk.GetNativeArray(ref m_TaxiRequestType);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var _, out var owner, out var targetSeeker);
				if (!m_TaxiRequestData.TryGetComponent(owner, out var componentData))
				{
					continue;
				}
				bool flag = false;
				Entity service = Entity.Null;
				if (componentData.m_Type != TaxiRequestType.Outside)
				{
					if (m_TransportDepotData.TryGetComponent(componentData.m_Seeker, out var componentData2))
					{
						flag = (componentData2.m_Flags & TransportDepotFlags.HasDispatchCenter) != 0;
						service = componentData.m_Seeker;
					}
					else
					{
						if (!targetSeeker.m_PrefabRef.HasComponent(componentData.m_Seeker))
						{
							continue;
						}
						if (targetSeeker.m_Owner.TryGetComponent(componentData.m_Seeker, out var componentData3) && m_TransportDepotData.TryGetComponent(componentData3.m_Owner, out componentData2))
						{
							flag = (componentData2.m_Flags & TransportDepotFlags.HasDispatchCenter) != 0;
							service = componentData3.m_Owner;
						}
					}
				}
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					if ((nativeArray2[j].m_Flags & ServiceRequestFlags.Reversed) != 0)
					{
						continue;
					}
					TaxiRequest taxiRequest = nativeArray3[j];
					switch (taxiRequest.m_Type)
					{
					case TaxiRequestType.Stand:
						if (componentData.m_Type == TaxiRequestType.Outside)
						{
							continue;
						}
						targetSeeker.m_SetupQueueTarget.m_Methods = PathMethod.Road | PathMethod.MediumRoad;
						break;
					case TaxiRequestType.Customer:
						if (!flag)
						{
							continue;
						}
						targetSeeker.m_SetupQueueTarget.m_Methods = PathMethod.Boarding;
						break;
					case TaxiRequestType.Outside:
						if (componentData.m_Type != TaxiRequestType.Outside)
						{
							continue;
						}
						targetSeeker.m_SetupQueueTarget.m_Methods = PathMethod.Boarding;
						break;
					default:
						continue;
					}
					if (AreaUtils.CheckServiceDistrict(taxiRequest.m_District1, taxiRequest.m_District2, service, m_ServiceDistricts))
					{
						targetSeeker.FindTargets(nativeArray[j], taxiRequest.m_Seeker, 0f, EdgeFlags.DefaultMask, allowAccessRestriction: true, navigationEnd: false);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private EntityQuery m_TransportVehicleQuery;

	private EntityQuery m_TaxiQuery;

	private EntityQuery m_TransportVehicleRequestQuery;

	private EntityQuery m_TaxiRequestQuery;

	private EntityTypeHandle m_EntityType;

	private ComponentTypeHandle<PathOwner> m_PathOwnerType;

	private ComponentTypeHandle<Owner> m_OwnerType;

	private ComponentTypeHandle<Game.Objects.OutsideConnection> m_OutsideConnectionType;

	private ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

	private ComponentTypeHandle<TransportVehicleRequest> m_TransportVehicleRequestType;

	private ComponentTypeHandle<TaxiRequest> m_TaxiRequestType;

	private ComponentTypeHandle<Game.Buildings.TransportDepot> m_TransportDepotType;

	private ComponentTypeHandle<Game.Vehicles.CargoTransport> m_CargoTransportType;

	private ComponentTypeHandle<Game.Vehicles.PublicTransport> m_PublicTransportType;

	private ComponentTypeHandle<Game.Vehicles.Taxi> m_TaxiType;

	private ComponentTypeHandle<Controller> m_ControllerType;

	private ComponentTypeHandle<Game.Routes.Color> m_RouteColorType;

	private ComponentTypeHandle<PrefabRef> m_PrefabRefType;

	private BufferTypeHandle<PathElement> m_PathElementType;

	private BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

	private BufferTypeHandle<LayoutElement> m_LayoutElementType;

	private ComponentLookup<TransportVehicleRequest> m_TransportVehicleRequestData;

	private ComponentLookup<TaxiRequest> m_TaxiRequestData;

	private ComponentLookup<Game.Routes.Color> m_RouteColorData;

	private ComponentLookup<Game.Buildings.TransportDepot> m_TransportDepotData;

	private ComponentLookup<Game.Vehicles.CargoTransport> m_CargoTransportData;

	private ComponentLookup<Game.Vehicles.PublicTransport> m_PublicTransportData;

	private ComponentLookup<TransportLineData> m_TransportLineData;

	private ComponentLookup<TransportDepotData> m_PrefabTransportDepotData;

	private ComponentLookup<MultipleUnitTrainData> m_MultipleUnitTrainData;

	private BufferLookup<ServiceDistrict> m_ServiceDistricts;

	private BufferLookup<RouteWaypoint> m_Waypoints;

	private BufferLookup<VehicleModel> m_VehicleModels;

	public TransportPathfindSetup(PathfindSetupSystem system)
	{
		m_TransportVehicleQuery = system.GetSetupQuery(new EntityQueryDesc
		{
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Buildings.TransportDepot>(),
				ComponentType.ReadOnly<Game.Vehicles.CargoTransport>(),
				ComponentType.ReadOnly<Game.Vehicles.PublicTransport>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Game.Buildings.ServiceUpgrade>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_TaxiQuery = system.GetSetupQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Buildings.TransportDepot>(),
				ComponentType.ReadOnly<Game.Vehicles.Taxi>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Game.Buildings.ServiceUpgrade>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_TransportVehicleRequestQuery = system.GetSetupQuery(ComponentType.ReadOnly<TransportVehicleRequest>(), ComponentType.Exclude<Dispatched>(), ComponentType.Exclude<PathInformation>());
		m_TaxiRequestQuery = system.GetSetupQuery(ComponentType.ReadOnly<TaxiRequest>(), ComponentType.Exclude<Dispatched>(), ComponentType.Exclude<PathInformation>());
		m_EntityType = system.GetEntityTypeHandle();
		m_PathOwnerType = system.GetComponentTypeHandle<PathOwner>(isReadOnly: true);
		m_OwnerType = system.GetComponentTypeHandle<Owner>(isReadOnly: true);
		m_OutsideConnectionType = system.GetComponentTypeHandle<Game.Objects.OutsideConnection>(isReadOnly: true);
		m_ServiceRequestType = system.GetComponentTypeHandle<ServiceRequest>(isReadOnly: true);
		m_TransportVehicleRequestType = system.GetComponentTypeHandle<TransportVehicleRequest>(isReadOnly: true);
		m_TaxiRequestType = system.GetComponentTypeHandle<TaxiRequest>(isReadOnly: true);
		m_TransportDepotType = system.GetComponentTypeHandle<Game.Buildings.TransportDepot>(isReadOnly: true);
		m_CargoTransportType = system.GetComponentTypeHandle<Game.Vehicles.CargoTransport>(isReadOnly: true);
		m_PublicTransportType = system.GetComponentTypeHandle<Game.Vehicles.PublicTransport>(isReadOnly: true);
		m_TaxiType = system.GetComponentTypeHandle<Game.Vehicles.Taxi>(isReadOnly: true);
		m_ControllerType = system.GetComponentTypeHandle<Controller>(isReadOnly: true);
		m_RouteColorType = system.GetComponentTypeHandle<Game.Routes.Color>(isReadOnly: true);
		m_PrefabRefType = system.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
		m_PathElementType = system.GetBufferTypeHandle<PathElement>(isReadOnly: true);
		m_ServiceDispatchType = system.GetBufferTypeHandle<ServiceDispatch>(isReadOnly: true);
		m_LayoutElementType = system.GetBufferTypeHandle<LayoutElement>(isReadOnly: true);
		m_TransportVehicleRequestData = system.GetComponentLookup<TransportVehicleRequest>(isReadOnly: true);
		m_TaxiRequestData = system.GetComponentLookup<TaxiRequest>(isReadOnly: true);
		m_RouteColorData = system.GetComponentLookup<Game.Routes.Color>(isReadOnly: true);
		m_TransportDepotData = system.GetComponentLookup<Game.Buildings.TransportDepot>(isReadOnly: true);
		m_CargoTransportData = system.GetComponentLookup<Game.Vehicles.CargoTransport>(isReadOnly: true);
		m_PublicTransportData = system.GetComponentLookup<Game.Vehicles.PublicTransport>(isReadOnly: true);
		m_TransportLineData = system.GetComponentLookup<TransportLineData>(isReadOnly: true);
		m_PrefabTransportDepotData = system.GetComponentLookup<TransportDepotData>(isReadOnly: true);
		m_MultipleUnitTrainData = system.GetComponentLookup<MultipleUnitTrainData>(isReadOnly: true);
		m_ServiceDistricts = system.GetBufferLookup<ServiceDistrict>(isReadOnly: true);
		m_Waypoints = system.GetBufferLookup<RouteWaypoint>(isReadOnly: true);
		m_VehicleModels = system.GetBufferLookup<VehicleModel>(isReadOnly: true);
	}

	public JobHandle SetupTransportVehicle(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_TransportDepotType.Update(system);
		m_CargoTransportType.Update(system);
		m_PublicTransportType.Update(system);
		m_ControllerType.Update(system);
		m_RouteColorType.Update(system);
		m_OwnerType.Update(system);
		m_PrefabRefType.Update(system);
		m_LayoutElementType.Update(system);
		m_TransportVehicleRequestData.Update(system);
		m_RouteColorData.Update(system);
		m_PrefabTransportDepotData.Update(system);
		m_TransportLineData.Update(system);
		m_MultipleUnitTrainData.Update(system);
		m_VehicleModels.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupTransportVehiclesJob
		{
			m_EntityType = m_EntityType,
			m_TransportDepotType = m_TransportDepotType,
			m_CargoTransportType = m_CargoTransportType,
			m_PublicTransportType = m_PublicTransportType,
			m_ControllerType = m_ControllerType,
			m_RouteColorType = m_RouteColorType,
			m_OwnerType = m_OwnerType,
			m_PrefabRefType = m_PrefabRefType,
			m_LayoutElementType = m_LayoutElementType,
			m_TransportVehicleRequestData = m_TransportVehicleRequestData,
			m_RouteColorData = m_RouteColorData,
			m_PrefabTransportDepotData = m_PrefabTransportDepotData,
			m_TransportLineData = m_TransportLineData,
			m_MultipleUnitTrainData = m_MultipleUnitTrainData,
			m_VehicleModels = m_VehicleModels,
			m_SetupData = setupData
		}, m_TransportVehicleQuery, inputDeps);
	}

	public JobHandle SetupTaxi(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_TransportDepotType.Update(system);
		m_TaxiType.Update(system);
		m_OwnerType.Update(system);
		m_PathOwnerType.Update(system);
		m_OutsideConnectionType.Update(system);
		m_PrefabRefType.Update(system);
		m_PathElementType.Update(system);
		m_ServiceDispatchType.Update(system);
		m_TaxiRequestData.Update(system);
		m_TransportDepotData.Update(system);
		m_PrefabTransportDepotData.Update(system);
		m_ServiceDistricts.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupTaxisJob
		{
			m_EntityType = m_EntityType,
			m_TransportDepotType = m_TransportDepotType,
			m_TaxiType = m_TaxiType,
			m_OwnerType = m_OwnerType,
			m_PathOwnerType = m_PathOwnerType,
			m_OutsideConnectionType = m_OutsideConnectionType,
			m_PrefabRefType = m_PrefabRefType,
			m_PathElementType = m_PathElementType,
			m_ServiceDispatchType = m_ServiceDispatchType,
			m_TaxiRequestData = m_TaxiRequestData,
			m_TransportDepotData = m_TransportDepotData,
			m_PrefabTransportDepotData = m_PrefabTransportDepotData,
			m_ServiceDistricts = m_ServiceDistricts,
			m_SetupData = setupData
		}, m_TaxiQuery, inputDeps);
	}

	public JobHandle SetupRouteWaypoints(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_Waypoints.Update(system);
		return IJobParallelForExtensions.Schedule(new SetupRouteWaypointsJob
		{
			m_Waypoints = m_Waypoints,
			m_SetupData = setupData
		}, setupData.Length, 1, inputDeps);
	}

	public JobHandle SetupTransportVehicleRequest(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_ServiceRequestType.Update(system);
		m_TransportVehicleRequestType.Update(system);
		m_TransportVehicleRequestData.Update(system);
		m_PublicTransportData.Update(system);
		m_CargoTransportData.Update(system);
		m_TransportLineData.Update(system);
		m_PrefabTransportDepotData.Update(system);
		m_MultipleUnitTrainData.Update(system);
		m_Waypoints.Update(system);
		m_VehicleModels.Update(system);
		return JobChunkExtensions.ScheduleParallel(new TransportVehicleRequestsJob
		{
			m_EntityType = m_EntityType,
			m_ServiceRequestType = m_ServiceRequestType,
			m_TransportVehicleRequestType = m_TransportVehicleRequestType,
			m_TransportVehicleRequestData = m_TransportVehicleRequestData,
			m_PublicTransportData = m_PublicTransportData,
			m_CargoTransportData = m_CargoTransportData,
			m_TransportLineData = m_TransportLineData,
			m_TransportDepotData = m_PrefabTransportDepotData,
			m_MultipleUnitTrainData = m_MultipleUnitTrainData,
			m_Waypoints = m_Waypoints,
			m_VehicleModels = m_VehicleModels,
			m_SetupData = setupData
		}, m_TransportVehicleRequestQuery, inputDeps);
	}

	public JobHandle SetupTaxiRequest(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_ServiceRequestType.Update(system);
		m_TaxiRequestType.Update(system);
		m_TaxiRequestData.Update(system);
		m_TransportDepotData.Update(system);
		m_ServiceDistricts.Update(system);
		return JobChunkExtensions.ScheduleParallel(new TaxiRequestsJob
		{
			m_EntityType = m_EntityType,
			m_ServiceRequestType = m_ServiceRequestType,
			m_TaxiRequestType = m_TaxiRequestType,
			m_TaxiRequestData = m_TaxiRequestData,
			m_TransportDepotData = m_TransportDepotData,
			m_ServiceDistricts = m_ServiceDistricts,
			m_SetupData = setupData
		}, m_TaxiRequestQuery, inputDeps);
	}
}
