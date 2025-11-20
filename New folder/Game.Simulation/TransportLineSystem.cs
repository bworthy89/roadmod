#define UNITY_ASSERTIONS
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.Common;
using Game.Notifications;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class TransportLineSystem : GameSystemBase, IDefaultSerializable, ISerializable
{
	private struct SortedVehicle : IComparable<SortedVehicle>
	{
		public Entity m_Vehicle;

		public float m_Distance;

		public int CompareTo(SortedVehicle other)
		{
			return math.select(0, math.select(1, -1, m_Distance < other.m_Distance), m_Distance != other.m_Distance);
		}
	}

	private struct VehicleAction
	{
		public VehicleActionType m_Type;

		public Entity m_Vehicle;
	}

	private enum VehicleActionType
	{
		AbandonRoute,
		CancelAbandon
	}

	[BurstCompile]
	private struct TransportLineTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Route> m_RouteType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<RouteWaypoint> m_RouteWaypointType;

		[ReadOnly]
		public BufferTypeHandle<RouteSegment> m_RouteSegmentType;

		[ReadOnly]
		public BufferTypeHandle<RouteModifier> m_RouteModifierType;

		[ReadOnly]
		public BufferTypeHandle<VehicleModel> m_VehicleModelType;

		public ComponentTypeHandle<TransportLine> m_TransportLineType;

		public BufferTypeHandle<RouteVehicle> m_RouteVehicleType;

		public BufferTypeHandle<DispatchedRequest> m_DispatchedRequestType;

		[ReadOnly]
		public ComponentLookup<ServiceRequest> m_ServiceRequestData;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public ComponentLookup<VehicleTiming> m_VehicleTimingData;

		[ReadOnly]
		public ComponentLookup<Connected> m_ConnectedData;

		[ReadOnly]
		public ComponentLookup<Game.Routes.TransportStop> m_TransportStopData;

		[ReadOnly]
		public ComponentLookup<CurrentRoute> m_CurrentRouteData;

		[ReadOnly]
		public ComponentLookup<Dispatched> m_DispatchedData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.CargoTransport> m_CargoTransportData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PublicTransport> m_PublicTransportData;

		[ReadOnly]
		public ComponentLookup<Odometer> m_OdometerData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<TransportLineData> m_TransportLineDataData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<MultipleUnitTrainData> m_MultipleUnitTrainData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<RouteInfo> m_RouteInfoData;

		[ReadOnly]
		public EntityArchetype m_VehicleRequestArchetype;

		[ReadOnly]
		public bool m_IsNight;

		[NativeDisableParallelForRestriction]
		public NativeArray<float> m_MaxTransportSpeed;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<VehicleAction>.ParallelWriter m_ActionQueue;

		public IconCommandBuffer m_IconCommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Route> nativeArray2 = chunk.GetNativeArray(ref m_RouteType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<TransportLine> nativeArray4 = chunk.GetNativeArray(ref m_TransportLineType);
			BufferAccessor<RouteWaypoint> bufferAccessor = chunk.GetBufferAccessor(ref m_RouteWaypointType);
			BufferAccessor<RouteSegment> bufferAccessor2 = chunk.GetBufferAccessor(ref m_RouteSegmentType);
			BufferAccessor<RouteModifier> bufferAccessor3 = chunk.GetBufferAccessor(ref m_RouteModifierType);
			BufferAccessor<VehicleModel> bufferAccessor4 = chunk.GetBufferAccessor(ref m_VehicleModelType);
			BufferAccessor<RouteVehicle> bufferAccessor5 = chunk.GetBufferAccessor(ref m_RouteVehicleType);
			BufferAccessor<DispatchedRequest> bufferAccessor6 = chunk.GetBufferAccessor(ref m_DispatchedRequestType);
			NativeList<SortedVehicle> sortBuffer = default(NativeList<SortedVehicle>);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Route route = nativeArray2[i];
				PrefabRef prefabRef = nativeArray3[i];
				TransportLine transportLine = nativeArray4[i];
				DynamicBuffer<RouteWaypoint> waypoints = bufferAccessor[i];
				DynamicBuffer<RouteSegment> segments = bufferAccessor2[i];
				DynamicBuffer<RouteModifier> modifiers = bufferAccessor3[i];
				DynamicBuffer<VehicleModel> vehicleModels = bufferAccessor4[i];
				DynamicBuffer<RouteVehicle> vehicles = bufferAccessor5[i];
				DynamicBuffer<DispatchedRequest> requests = bufferAccessor6[i];
				TransportLineData prefabLineData = m_TransportLineDataData[prefabRef.m_Prefab];
				float value = prefabLineData.m_DefaultVehicleInterval;
				RouteUtils.ApplyModifier(ref value, modifiers, RouteModifierType.VehicleInterval);
				ushort num = 0;
				if (RouteUtils.CheckOption(route, RouteOption.PaidTicket))
				{
					float value2 = 0f;
					RouteUtils.ApplyModifier(ref value2, modifiers, RouteModifierType.TicketPrice);
					num = (ushort)math.clamp(Mathf.RoundToInt(value2), 0, 65535);
				}
				bool3 isActive = (RouteUtils.CheckOption(route, RouteOption.Inactive) ? ((bool3)false) : ((!CheckIfIsThereAnyActiveBuildingsOnTheLine(waypoints)) ? ((bool3)false) : (RouteUtils.CheckOption(route, RouteOption.Day) ? new bool3(!m_IsNight, y: true, z: false) : ((!RouteUtils.CheckOption(route, RouteOption.Night)) ? ((bool3)true) : new bool3(m_IsNight, y: false, z: true)))));
				RefreshLineSegments(unfilteredChunkIndex, prefabLineData, waypoints, segments, isActive, out var lineDuration, out var stableDuration);
				int num2 = CalculateVehicleCount(value, stableDuration);
				float num3 = math.min(value * 10f, CalculateVehicleInterval(lineDuration, num2));
				bool flag = false;
				if (math.abs(num3 - transportLine.m_VehicleInterval) >= 1f)
				{
					transportLine.m_VehicleInterval = num3;
					flag = true;
				}
				if (num != transportLine.m_TicketPrice)
				{
					transportLine.m_TicketPrice = num;
					flag = true;
				}
				if (flag)
				{
					UpdateStopPathfind(unfilteredChunkIndex, waypoints);
				}
				CheckVehicles(entity, vehicleModels, vehicles, out var totalCount, out var continuingCount);
				num2 = math.select(0, num2, isActive.x);
				if (continuingCount < num2 && continuingCount < totalCount)
				{
					int count = num2 - continuingCount;
					CancelAbandon(vehicleModels, vehicles, count, ref sortBuffer);
				}
				else if (continuingCount > num2)
				{
					int count2 = continuingCount - num2;
					AbandonVehicles(vehicleModels, vehicles, count2, ref sortBuffer);
				}
				CheckRequests(ref transportLine, requests);
				bool flag2 = false;
				if (totalCount < num2)
				{
					transportLine.m_Flags |= TransportLineFlags.RequireVehicles;
					totalCount += requests.Length;
					if (totalCount < num2)
					{
						flag2 = !RequestNewVehicleIfNeeded(unfilteredChunkIndex, entity, transportLine, totalCount, num2);
					}
				}
				else
				{
					transportLine.m_Flags &= ~TransportLineFlags.RequireVehicles;
				}
				if (flag2)
				{
					if ((transportLine.m_Flags & TransportLineFlags.NotEnoughVehicles) == 0 && waypoints.Length != 0)
					{
						transportLine.m_Flags |= TransportLineFlags.NotEnoughVehicles;
						m_IconCommandBuffer.Add(entity, prefabLineData.m_VehicleNotification, IconPriority.Problem);
					}
				}
				else if ((transportLine.m_Flags & TransportLineFlags.NotEnoughVehicles) != 0)
				{
					transportLine.m_Flags &= ~TransportLineFlags.NotEnoughVehicles;
					m_IconCommandBuffer.Remove(entity, prefabLineData.m_VehicleNotification);
				}
				nativeArray4[i] = transportLine;
			}
			if (sortBuffer.IsCreated)
			{
				sortBuffer.Dispose();
			}
		}

		private void UpdateStopPathfind(int jobIndex, DynamicBuffer<RouteWaypoint> waypoints)
		{
			for (int i = 0; i < waypoints.Length; i++)
			{
				Entity waypoint = waypoints[i].m_Waypoint;
				if (m_VehicleTimingData.HasComponent(waypoint))
				{
					m_CommandBuffer.AddComponent(jobIndex, waypoint, default(PathfindUpdated));
				}
			}
		}

		private void CheckVehicles(Entity route, DynamicBuffer<VehicleModel> vehicleModels, DynamicBuffer<RouteVehicle> vehicles, out int totalCount, out int continuingCount)
		{
			totalCount = 0;
			continuingCount = 0;
			while (totalCount < vehicles.Length)
			{
				Entity vehicle = vehicles[totalCount].m_Vehicle;
				CurrentRoute currentRoute = default(CurrentRoute);
				if (m_CurrentRouteData.HasComponent(vehicle))
				{
					currentRoute = m_CurrentRouteData[vehicle];
				}
				if (currentRoute.m_Route == route)
				{
					totalCount++;
					Game.Vehicles.CargoTransport cargoTransport = default(Game.Vehicles.CargoTransport);
					if (m_CargoTransportData.HasComponent(vehicle))
					{
						cargoTransport = m_CargoTransportData[vehicle];
					}
					Game.Vehicles.PublicTransport publicTransport = default(Game.Vehicles.PublicTransport);
					if (m_PublicTransportData.HasComponent(vehicle))
					{
						publicTransport = m_PublicTransportData[vehicle];
					}
					if ((cargoTransport.m_State & CargoTransportFlags.AbandonRoute) == 0 && (publicTransport.m_State & PublicTransportFlags.AbandonRoute) == 0)
					{
						PrefabRef prefabRef = m_PrefabRefData[vehicle];
						m_LayoutElements.TryGetBuffer(vehicle, out var bufferData);
						if (RouteUtils.CheckVehicleModel(vehicleModels, prefabRef, bufferData, ref m_PrefabRefData, ref m_MultipleUnitTrainData))
						{
							continuingCount++;
							continue;
						}
						m_ActionQueue.Enqueue(new VehicleAction
						{
							m_Type = VehicleActionType.AbandonRoute,
							m_Vehicle = vehicle
						});
					}
				}
				else
				{
					vehicles.RemoveAt(totalCount);
				}
			}
		}

		private void AbandonVehicles(DynamicBuffer<VehicleModel> vehicleModels, DynamicBuffer<RouteVehicle> vehicles, int count, ref NativeList<SortedVehicle> sortBuffer)
		{
			if (!sortBuffer.IsCreated)
			{
				sortBuffer = new NativeList<SortedVehicle>(vehicles.Length, Allocator.Temp);
			}
			for (int i = 0; i < vehicles.Length; i++)
			{
				Entity vehicle = vehicles[i].m_Vehicle;
				Game.Vehicles.CargoTransport cargoTransport = default(Game.Vehicles.CargoTransport);
				if (m_CargoTransportData.HasComponent(vehicle))
				{
					cargoTransport = m_CargoTransportData[vehicle];
				}
				Game.Vehicles.PublicTransport publicTransport = default(Game.Vehicles.PublicTransport);
				if (m_PublicTransportData.HasComponent(vehicle))
				{
					publicTransport = m_PublicTransportData[vehicle];
				}
				if ((cargoTransport.m_State & CargoTransportFlags.AbandonRoute) == 0 && (publicTransport.m_State & PublicTransportFlags.AbandonRoute) == 0)
				{
					PrefabRef prefabRef = m_PrefabRefData[vehicle];
					m_LayoutElements.TryGetBuffer(vehicle, out var bufferData);
					if (RouteUtils.CheckVehicleModel(vehicleModels, prefabRef, bufferData, ref m_PrefabRefData, ref m_MultipleUnitTrainData))
					{
						SortedVehicle value = new SortedVehicle
						{
							m_Vehicle = vehicle,
							m_Distance = m_OdometerData[vehicle].m_Distance
						};
						sortBuffer.Add(in value);
					}
				}
			}
			sortBuffer.Sort();
			count = math.min(count, sortBuffer.Length);
			for (int j = 0; j < count; j++)
			{
				Entity vehicle2 = sortBuffer[sortBuffer.Length - j - 1].m_Vehicle;
				m_ActionQueue.Enqueue(new VehicleAction
				{
					m_Type = VehicleActionType.AbandonRoute,
					m_Vehicle = vehicle2
				});
			}
			sortBuffer.Clear();
		}

		private void CancelAbandon(DynamicBuffer<VehicleModel> vehicleModels, DynamicBuffer<RouteVehicle> vehicles, int count, ref NativeList<SortedVehicle> sortBuffer)
		{
			if (!sortBuffer.IsCreated)
			{
				sortBuffer = new NativeList<SortedVehicle>(vehicles.Length, Allocator.Temp);
			}
			for (int i = 0; i < vehicles.Length; i++)
			{
				Entity vehicle = vehicles[i].m_Vehicle;
				Game.Vehicles.CargoTransport cargoTransport = default(Game.Vehicles.CargoTransport);
				if (m_CargoTransportData.HasComponent(vehicle))
				{
					cargoTransport = m_CargoTransportData[vehicle];
				}
				Game.Vehicles.PublicTransport publicTransport = default(Game.Vehicles.PublicTransport);
				if (m_PublicTransportData.HasComponent(vehicle))
				{
					publicTransport = m_PublicTransportData[vehicle];
				}
				if ((cargoTransport.m_State & (CargoTransportFlags.AbandonRoute | CargoTransportFlags.Disabled)) == CargoTransportFlags.AbandonRoute || (publicTransport.m_State & (PublicTransportFlags.AbandonRoute | PublicTransportFlags.Disabled)) == PublicTransportFlags.AbandonRoute)
				{
					PrefabRef prefabRef = m_PrefabRefData[vehicle];
					m_LayoutElements.TryGetBuffer(vehicle, out var bufferData);
					if (RouteUtils.CheckVehicleModel(vehicleModels, prefabRef, bufferData, ref m_PrefabRefData, ref m_MultipleUnitTrainData))
					{
						SortedVehicle value = new SortedVehicle
						{
							m_Vehicle = vehicle,
							m_Distance = m_OdometerData[vehicle].m_Distance
						};
						sortBuffer.Add(in value);
					}
				}
			}
			sortBuffer.Sort();
			count = math.min(count, sortBuffer.Length);
			for (int j = 0; j < count; j++)
			{
				Entity vehicle2 = sortBuffer[j].m_Vehicle;
				m_ActionQueue.Enqueue(new VehicleAction
				{
					m_Type = VehicleActionType.CancelAbandon,
					m_Vehicle = vehicle2
				});
			}
			sortBuffer.Clear();
		}

		private unsafe void RefreshLineSegments(int jobIndex, TransportLineData prefabLineData, DynamicBuffer<RouteWaypoint> waypoints, DynamicBuffer<RouteSegment> segments, bool3 isActive, out float lineDuration, out float stableDuration)
		{
			lineDuration = 0f;
			stableDuration = 0f;
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			int num5 = 0;
			int num6 = 0;
			for (int i = 0; i < waypoints.Length; i++)
			{
				if (m_VehicleTimingData.HasComponent(waypoints[i].m_Waypoint))
				{
					num6 = i;
					break;
				}
			}
			RouteInfoFlags routeInfoFlags = (RouteInfoFlags)0;
			if (!isActive.y)
			{
				routeInfoFlags |= RouteInfoFlags.InactiveDay;
			}
			if (!isActive.z)
			{
				routeInfoFlags |= RouteInfoFlags.InactiveNight;
			}
			int num7 = num6;
			for (int j = 0; j < waypoints.Length; j++)
			{
				int2 @int = num6 + j;
				@int.y++;
				@int = math.select(@int, @int - waypoints.Length, @int >= waypoints.Length);
				num5++;
				Entity waypoint = waypoints[@int.y].m_Waypoint;
				Entity segment = segments[@int.x].m_Segment;
				if (m_PathInformationData.HasComponent(segment))
				{
					PathInformation pathInformation = m_PathInformationData[segment];
					num += pathInformation.m_Duration;
					num2 += pathInformation.m_Distance;
					num3 += pathInformation.m_Duration;
					stableDuration += pathInformation.m_Duration;
				}
				if (!m_VehicleTimingData.HasComponent(waypoint))
				{
					continue;
				}
				VehicleTiming vehicleTiming = m_VehicleTimingData[waypoint];
				float stopDuration = prefabLineData.m_StopDuration;
				if (m_ConnectedData.HasComponent(waypoint))
				{
					Connected connected = m_ConnectedData[waypoint];
					if (m_TransportStopData.HasComponent(connected.m_Connected))
					{
						stopDuration = RouteUtils.GetStopDuration(prefabLineData, m_TransportStopData[connected.m_Connected]);
					}
				}
				num = math.max(num, vehicleTiming.m_AverageTravelTime) + stopDuration;
				lineDuration += num;
				stableDuration += prefabLineData.m_StopDuration;
				for (int k = 0; k < num5; k++)
				{
					int num8 = num7 + k;
					num8 = math.select(num8, num8 - waypoints.Length, num8 >= waypoints.Length);
					Entity segment2 = segments[num8].m_Segment;
					if (m_PathInformationData.HasComponent(segment2))
					{
						PathInformation pathInformation2 = m_PathInformationData[segment2];
						RouteInfo routeInfo = m_RouteInfoData[segment2];
						RouteInfo value = routeInfo;
						value.m_Duration = pathInformation2.m_Duration * math.max(1f, num / math.max(1f, num3));
						value.m_Distance = pathInformation2.m_Distance;
						value.m_Flags = routeInfoFlags;
						if (value.m_Distance != routeInfo.m_Distance || math.abs(value.m_Duration - routeInfo.m_Duration) >= 1f || value.m_Flags != routeInfo.m_Flags)
						{
							m_RouteInfoData[segment2] = value;
							m_CommandBuffer.AddComponent(jobIndex, segment2, default(PathfindUpdated));
						}
					}
				}
				num4 = math.max(num4, math.max(1f, num2) / math.max(1f, num));
				num = 0f;
				num2 = 0f;
				num3 = 0f;
				num5 = 0;
				num7 = @int.y;
			}
			if (prefabLineData.m_PassengerTransport)
			{
				float* unsafePtr = (float*)m_MaxTransportSpeed.GetUnsafePtr();
				if (num4 > *unsafePtr)
				{
					float num9;
					do
					{
						num9 = num4;
						num4 = Interlocked.Exchange(ref *unsafePtr, num9);
					}
					while (num4 > num9);
				}
			}
			if (!prefabLineData.m_CargoTransport)
			{
				return;
			}
			float* unsafePtr2 = (float*)m_MaxTransportSpeed.GetUnsafePtr();
			unsafePtr2++;
			if (num4 > *unsafePtr2)
			{
				float num10;
				do
				{
					num10 = num4;
					num4 = Interlocked.Exchange(ref *unsafePtr2, num10);
				}
				while (num4 > num10);
			}
		}

		private void CheckRequests(ref TransportLine transportLine, DynamicBuffer<DispatchedRequest> requests)
		{
			for (int i = 0; i < requests.Length; i++)
			{
				Entity vehicleRequest = requests[i].m_VehicleRequest;
				if (!m_ServiceRequestData.HasComponent(vehicleRequest))
				{
					requests.RemoveAtSwapBack(i--);
				}
			}
			if (m_DispatchedData.HasComponent(transportLine.m_VehicleRequest))
			{
				requests.Add(new DispatchedRequest
				{
					m_VehicleRequest = transportLine.m_VehicleRequest
				});
				transportLine.m_VehicleRequest = Entity.Null;
			}
		}

		private bool RequestNewVehicleIfNeeded(int jobIndex, Entity entity, TransportLine transportLine, int vehicleCount, int targetCount)
		{
			if (m_ServiceRequestData.TryGetComponent(transportLine.m_VehicleRequest, out var componentData))
			{
				return componentData.m_FailCount < 2;
			}
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_VehicleRequestArchetype);
			m_CommandBuffer.SetComponent(jobIndex, e, new TransportVehicleRequest(entity, 1f - (float)vehicleCount / (float)targetCount));
			m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(8u));
			return true;
		}

		private bool CheckIfIsThereAnyActiveBuildingsOnTheLine(DynamicBuffer<RouteWaypoint> waypoints)
		{
			bool flag = false;
			bool flag2 = false;
			for (int i = 0; i < waypoints.Length; i++)
			{
				Entity waypoint = waypoints[i].m_Waypoint;
				if (!m_ConnectedData.TryGetComponent(waypoint, out var componentData))
				{
					continue;
				}
				Entity connected = componentData.m_Connected;
				Entity topOwner = GetTopOwner(connected);
				if (m_BuildingData.TryGetComponent(topOwner, out var componentData2))
				{
					flag2 = true;
					if (!BuildingUtils.CheckOption(componentData2, BuildingOption.Inactive))
					{
						flag = true;
						break;
					}
				}
			}
			return !flag2 || flag;
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

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct VehicleActionJob : IJob
	{
		public ComponentLookup<Game.Vehicles.CargoTransport> m_CargoTransportData;

		public ComponentLookup<Game.Vehicles.PublicTransport> m_PublicTransportData;

		public NativeQueue<VehicleAction> m_ActionQueue;

		public void Execute()
		{
			int count = m_ActionQueue.Count;
			for (int i = 0; i < count; i++)
			{
				VehicleAction vehicleAction = m_ActionQueue.Dequeue();
				switch (vehicleAction.m_Type)
				{
				case VehicleActionType.AbandonRoute:
					if (m_CargoTransportData.HasComponent(vehicleAction.m_Vehicle))
					{
						Game.Vehicles.CargoTransport value3 = m_CargoTransportData[vehicleAction.m_Vehicle];
						value3.m_State |= CargoTransportFlags.AbandonRoute;
						m_CargoTransportData[vehicleAction.m_Vehicle] = value3;
					}
					if (m_PublicTransportData.HasComponent(vehicleAction.m_Vehicle))
					{
						Game.Vehicles.PublicTransport value4 = m_PublicTransportData[vehicleAction.m_Vehicle];
						value4.m_State |= PublicTransportFlags.AbandonRoute;
						m_PublicTransportData[vehicleAction.m_Vehicle] = value4;
					}
					break;
				case VehicleActionType.CancelAbandon:
					if (m_CargoTransportData.HasComponent(vehicleAction.m_Vehicle))
					{
						Game.Vehicles.CargoTransport value = m_CargoTransportData[vehicleAction.m_Vehicle];
						value.m_State &= ~CargoTransportFlags.AbandonRoute;
						m_CargoTransportData[vehicleAction.m_Vehicle] = value;
					}
					if (m_PublicTransportData.HasComponent(vehicleAction.m_Vehicle))
					{
						Game.Vehicles.PublicTransport value2 = m_PublicTransportData[vehicleAction.m_Vehicle];
						value2.m_State &= ~PublicTransportFlags.AbandonRoute;
						m_PublicTransportData[vehicleAction.m_Vehicle] = value2;
					}
					break;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Route> __Game_Routes_Route_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<RouteSegment> __Game_Routes_RouteSegment_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<RouteModifier> __Game_Routes_RouteModifier_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<VehicleModel> __Game_Routes_VehicleModel_RO_BufferTypeHandle;

		public ComponentTypeHandle<TransportLine> __Game_Routes_TransportLine_RW_ComponentTypeHandle;

		public BufferTypeHandle<RouteVehicle> __Game_Routes_RouteVehicle_RW_BufferTypeHandle;

		public BufferTypeHandle<DispatchedRequest> __Game_Routes_DispatchedRequest_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<ServiceRequest> __Game_Simulation_ServiceRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<VehicleTiming> __Game_Routes_VehicleTiming_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Connected> __Game_Routes_Connected_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Routes.TransportStop> __Game_Routes_TransportStop_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentRoute> __Game_Routes_CurrentRoute_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Dispatched> __Game_Simulation_Dispatched_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.CargoTransport> __Game_Vehicles_CargoTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PublicTransport> __Game_Vehicles_PublicTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Odometer> __Game_Vehicles_Odometer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportLineData> __Game_Prefabs_TransportLineData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MultipleUnitTrainData> __Game_Prefabs_MultipleUnitTrainData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		public ComponentLookup<RouteInfo> __Game_Routes_RouteInfo_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		public ComponentLookup<Game.Vehicles.CargoTransport> __Game_Vehicles_CargoTransport_RW_ComponentLookup;

		public ComponentLookup<Game.Vehicles.PublicTransport> __Game_Vehicles_PublicTransport_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Routes_Route_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Route>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Routes_RouteWaypoint_RO_BufferTypeHandle = state.GetBufferTypeHandle<RouteWaypoint>(isReadOnly: true);
			__Game_Routes_RouteSegment_RO_BufferTypeHandle = state.GetBufferTypeHandle<RouteSegment>(isReadOnly: true);
			__Game_Routes_RouteModifier_RO_BufferTypeHandle = state.GetBufferTypeHandle<RouteModifier>(isReadOnly: true);
			__Game_Routes_VehicleModel_RO_BufferTypeHandle = state.GetBufferTypeHandle<VehicleModel>(isReadOnly: true);
			__Game_Routes_TransportLine_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TransportLine>();
			__Game_Routes_RouteVehicle_RW_BufferTypeHandle = state.GetBufferTypeHandle<RouteVehicle>();
			__Game_Routes_DispatchedRequest_RW_BufferTypeHandle = state.GetBufferTypeHandle<DispatchedRequest>();
			__Game_Simulation_ServiceRequest_RO_ComponentLookup = state.GetComponentLookup<ServiceRequest>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Routes_VehicleTiming_RO_ComponentLookup = state.GetComponentLookup<VehicleTiming>(isReadOnly: true);
			__Game_Routes_Connected_RO_ComponentLookup = state.GetComponentLookup<Connected>(isReadOnly: true);
			__Game_Routes_TransportStop_RO_ComponentLookup = state.GetComponentLookup<Game.Routes.TransportStop>(isReadOnly: true);
			__Game_Routes_CurrentRoute_RO_ComponentLookup = state.GetComponentLookup<CurrentRoute>(isReadOnly: true);
			__Game_Simulation_Dispatched_RO_ComponentLookup = state.GetComponentLookup<Dispatched>(isReadOnly: true);
			__Game_Vehicles_CargoTransport_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.CargoTransport>(isReadOnly: true);
			__Game_Vehicles_PublicTransport_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PublicTransport>(isReadOnly: true);
			__Game_Vehicles_Odometer_RO_ComponentLookup = state.GetComponentLookup<Odometer>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_TransportLineData_RO_ComponentLookup = state.GetComponentLookup<TransportLineData>(isReadOnly: true);
			__Game_Prefabs_MultipleUnitTrainData_RO_ComponentLookup = state.GetComponentLookup<MultipleUnitTrainData>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Routes_RouteInfo_RW_ComponentLookup = state.GetComponentLookup<RouteInfo>();
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Vehicles_CargoTransport_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.CargoTransport>();
			__Game_Vehicles_PublicTransport_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PublicTransport>();
		}
	}

	public const uint UPDATE_INTERVAL = 256u;

	private EntityQuery m_LineQuery;

	private EntityArchetype m_VehicleRequestArchetype;

	private NativeArray<float> m_MaxTransportSpeed;

	private JobHandle m_MaxTransportSpeedDeps;

	private TimeSystem m_TimeSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private IconCommandSystem m_IconCommandSystem;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TimeSystem = base.World.GetOrCreateSystemManaged<TimeSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_LineQuery = GetEntityQuery(ComponentType.ReadOnly<Route>(), ComponentType.ReadWrite<TransportLine>(), ComponentType.ReadOnly<RouteWaypoint>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_VehicleRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<TransportVehicleRequest>(), ComponentType.ReadWrite<RequestGroup>());
		m_MaxTransportSpeed = new NativeArray<float>(2, Allocator.Persistent);
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_MaxTransportSpeed.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_MaxTransportSpeed[0] = 0f;
		m_MaxTransportSpeed[1] = 0f;
		if (!m_LineQuery.IsEmptyIgnoreFilter)
		{
			NativeQueue<VehicleAction> actionQueue = new NativeQueue<VehicleAction>(Allocator.TempJob);
			float normalizedTime = m_TimeSystem.normalizedTime;
			bool isNight = normalizedTime < 0.25f || normalizedTime >= 11f / 12f;
			TransportLineTickJob jobData = new TransportLineTickJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_RouteType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_Route_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_RouteWaypointType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_RouteSegmentType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_RouteSegment_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_RouteModifierType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_RouteModifier_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_VehicleModelType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_VehicleModel_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_TransportLineType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_TransportLine_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_RouteVehicleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_RouteVehicle_RW_BufferTypeHandle, ref base.CheckedStateRef),
				m_DispatchedRequestType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Routes_DispatchedRequest_RW_BufferTypeHandle, ref base.CheckedStateRef),
				m_ServiceRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ServiceRequest_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PathInformationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_VehicleTimingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_VehicleTiming_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Connected_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransportStopData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_TransportStop_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurrentRouteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_CurrentRoute_RO_ComponentLookup, ref base.CheckedStateRef),
				m_DispatchedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_Dispatched_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CargoTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CargoTransport_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PublicTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PublicTransport_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OdometerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Odometer_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransportLineDataData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportLineData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_MultipleUnitTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MultipleUnitTrainData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_RouteInfoData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_RouteInfo_RW_ComponentLookup, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
				m_VehicleRequestArchetype = m_VehicleRequestArchetype,
				m_IsNight = isNight,
				m_MaxTransportSpeed = m_MaxTransportSpeed,
				m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
				m_ActionQueue = actionQueue.AsParallelWriter(),
				m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer()
			};
			VehicleActionJob jobData2 = new VehicleActionJob
			{
				m_CargoTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CargoTransport_RW_ComponentLookup, ref base.CheckedStateRef),
				m_PublicTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PublicTransport_RW_ComponentLookup, ref base.CheckedStateRef),
				m_ActionQueue = actionQueue
			};
			JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_LineQuery, base.Dependency);
			JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
			actionQueue.Dispose(jobHandle2);
			m_MaxTransportSpeedDeps = jobHandle;
			m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
			m_IconCommandSystem.AddCommandBufferWriter(jobHandle);
			base.Dependency = jobHandle2;
		}
	}

	public void GetMaxTransportSpeed(out float maxPassengerTransportSpeed, out float maxCargoTransportSpeed)
	{
		m_MaxTransportSpeedDeps.Complete();
		maxPassengerTransportSpeed = m_MaxTransportSpeed[0];
		maxCargoTransportSpeed = m_MaxTransportSpeed[1];
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float value = m_MaxTransportSpeed[0];
		writer.Write(value);
		float value2 = m_MaxTransportSpeed[1];
		writer.Write(value2);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out float value);
		reader.Read(out float value2);
		m_MaxTransportSpeed[0] = value;
		m_MaxTransportSpeed[1] = value2;
	}

	public void SetDefaults(Context context)
	{
		m_MaxTransportSpeed[0] = 277.77777f;
		m_MaxTransportSpeed[1] = 277.77777f;
	}

	public static int CalculateVehicleCount(float vehicleInterval, float lineDuration)
	{
		return math.max(1, (int)math.round(lineDuration / math.max(1f, vehicleInterval)));
	}

	public static float CalculateVehicleInterval(float lineDuration, int vehicleCount)
	{
		return lineDuration / (float)math.max(1, vehicleCount);
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
	public TransportLineSystem()
	{
	}
}
