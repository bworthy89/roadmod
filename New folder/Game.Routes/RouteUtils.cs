using Colossal.Mathematics;
using Game.Areas;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Pathfind;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Routes;

public static class RouteUtils
{
	public interface ITransportEstimateBuffer
	{
		void AddWaitEstimate(Entity waypoint, int seconds);
	}

	public const float WAYPOINT_CONNECTION_DISTANCE = 10f;

	public const float ROUTE_VISIBLE_THROUGH_DISTANCE = 100f;

	public const float TRANSPORT_DAY_START_TIME = 0.25f;

	public const float TRANSPORT_DAY_END_TIME = 11f / 12f;

	public const float DEFAULT_TRAVEL_TIME = 1f / 48f;

	public const float TAXI_DISTANCE_FEE = 0.03f;

	public static float GetMinWaypointDistance(RouteData routeData)
	{
		return routeData.m_SnapDistance * 0.5f;
	}

	public static Bounds3 CalculateBounds(Position waypointPosition, RouteData routeData)
	{
		float snapDistance = routeData.m_SnapDistance;
		return new Bounds3(waypointPosition.m_Position - snapDistance, waypointPosition.m_Position + snapDistance);
	}

	public static Bounds3 CalculateBounds(CurveElement curveElement, RouteData routeData)
	{
		float snapDistance = routeData.m_SnapDistance;
		return MathUtils.Expand(MathUtils.Bounds(curveElement.m_Curve), snapDistance);
	}

	public static void StripTransportSegments<TTransportEstimateBuffer>(ref Unity.Mathematics.Random random, int length, DynamicBuffer<PathElement> path, ComponentLookup<Connected> connectedData, ComponentLookup<BoardingVehicle> boardingVehicleData, ComponentLookup<Owner> ownerData, ComponentLookup<Lane> laneData, ComponentLookup<Game.Net.ConnectionLane> connectionLaneData, ComponentLookup<Curve> curveData, ComponentLookup<PrefabRef> prefabRefData, ComponentLookup<TransportStopData> prefabTransportStopData, BufferLookup<Game.Net.SubLane> subLanes, BufferLookup<Game.Areas.Node> areaNodes, BufferLookup<Triangle> areaTriangles, TTransportEstimateBuffer transportEstimateBuffer) where TTransportEstimateBuffer : unmanaged, ITransportEstimateBuffer
	{
		int num = 0;
		while (num < length)
		{
			PathElement pathElement = path[num++];
			Entity entity = Entity.Null;
			int num2 = -1;
			if (connectedData.HasComponent(pathElement.m_Target))
			{
				Connected connected = connectedData[pathElement.m_Target];
				if (boardingVehicleData.HasComponent(connected.m_Connected))
				{
					entity = connected.m_Connected;
					num2 = num - 2;
				}
				int i;
				for (i = num; i < length && !connectedData.HasComponent(path[i].m_Target); i++)
				{
				}
				if (i > num)
				{
					path.RemoveRange(num, i - num);
					length -= i - num;
				}
				num = i;
			}
			else if (boardingVehicleData.HasComponent(pathElement.m_Target))
			{
				entity = pathElement.m_Target;
				num2 = num - 2;
			}
			if (!(entity != Entity.Null) || !prefabTransportStopData.TryGetComponent(prefabRefData[entity].m_Prefab, out var componentData))
			{
				continue;
			}
			if (num2 >= 0 && componentData.m_AccessDistance > 0f)
			{
				PathElement pathElement2 = path[num2];
				int length2 = path.Length;
				if (connectionLaneData.TryGetComponent(pathElement2.m_Target, out var componentData2))
				{
					if ((componentData2.m_Flags & ConnectionLaneFlags.Area) != 0)
					{
						OffsetPathTarget_AreaLane(ref random, componentData.m_AccessDistance, num2, path, ownerData, curveData, laneData, connectionLaneData, subLanes, areaNodes, areaTriangles);
					}
				}
				else if (curveData.HasComponent(pathElement2.m_Target))
				{
					OffsetPathTarget_EdgeLane(ref random, componentData.m_AccessDistance, num2, path, ownerData, laneData, curveData, subLanes);
				}
				num += path.Length - length2;
				length += path.Length - length2;
			}
			if (componentData.m_BoardingTime > 0f)
			{
				int num3 = MathUtils.RoundToIntRandom(ref random, componentData.m_BoardingTime);
				if (num3 > 0)
				{
					transportEstimateBuffer.AddWaitEstimate(pathElement.m_Target, num3);
				}
			}
		}
	}

	private static void OffsetPathTarget_AreaLane(ref Unity.Mathematics.Random random, float distance, int elementIndex, DynamicBuffer<PathElement> path, ComponentLookup<Owner> ownerData, ComponentLookup<Curve> curveData, ComponentLookup<Lane> laneData, ComponentLookup<Game.Net.ConnectionLane> connectionLaneData, BufferLookup<Game.Net.SubLane> subLanes, BufferLookup<Game.Areas.Node> areaNodes, BufferLookup<Triangle> areaTriangles)
	{
		PathElement pathElement = path[elementIndex];
		Curve curve = curveData[pathElement.m_Target];
		Entity owner = ownerData[pathElement.m_Target].m_Owner;
		float3 position = MathUtils.Position(curve.m_Bezier, pathElement.m_TargetDelta.y);
		DynamicBuffer<Game.Areas.Node> nodes = areaNodes[owner];
		DynamicBuffer<Triangle> dynamicBuffer = areaTriangles[owner];
		int num = -1;
		float num2 = 0f;
		float2 t;
		for (int i = 0; i < dynamicBuffer.Length; i++)
		{
			Triangle3 triangle = AreaUtils.GetTriangle3(nodes, dynamicBuffer[i]);
			if (!(MathUtils.Distance(triangle, position, out t) >= distance))
			{
				float num3 = MathUtils.Area(triangle.xz);
				num2 += num3;
				if (random.NextFloat(num2) < num3)
				{
					num = i;
				}
			}
		}
		if (num == -1)
		{
			return;
		}
		DynamicBuffer<Game.Net.SubLane> lanes = subLanes[owner];
		float2 @float = random.NextFloat2(1f);
		@float = math.select(@float, 1f - @float, math.csum(@float) > 1f);
		Triangle3 triangle2 = AreaUtils.GetTriangle3(nodes, dynamicBuffer[num]);
		float3 position2 = MathUtils.Position(triangle2, @float);
		float num4 = float.MaxValue;
		Entity entity = Entity.Null;
		float endCurvePos = 0f;
		for (int j = 0; j < lanes.Length; j++)
		{
			Entity subLane = lanes[j].m_SubLane;
			if (!connectionLaneData.HasComponent(subLane) || (connectionLaneData[subLane].m_Flags & ConnectionLaneFlags.Pedestrian) == 0)
			{
				continue;
			}
			curve = curveData[subLane];
			bool2 x = new bool2(MathUtils.Intersect(triangle2.xz, curve.m_Bezier.a.xz, out t), MathUtils.Intersect(triangle2.xz, curve.m_Bezier.d.xz, out t));
			if (math.any(x))
			{
				float t2;
				float num5 = MathUtils.Distance(curve.m_Bezier, position2, out t2);
				if (num5 < num4)
				{
					float2 float2 = math.select(new float2(0f, 0.49f), math.select(new float2(0.51f, 1f), new float2(0f, 1f), x.x), x.y);
					num4 = num5;
					entity = subLane;
					endCurvePos = random.NextFloat(float2.x, float2.y);
				}
			}
		}
		if (entity == Entity.Null)
		{
			UnityEngine.Debug.Log($"Target path lane not found ({position2.x}, {position2.y}, {position2.z})");
			return;
		}
		int num6 = elementIndex;
		Owner componentData;
		while (num6 > 0 && ownerData.TryGetComponent(path[num6 - 1].m_Target, out componentData) && !(componentData.m_Owner != owner))
		{
			num6--;
		}
		NativeList<PathElement> path2 = new NativeList<PathElement>(lanes.Length, Allocator.Temp);
		PathElement pathElement2 = path[num6];
		AreaUtils.FindAreaPath(ref random, path2, lanes, pathElement2.m_Target, pathElement2.m_TargetDelta.x, entity, endCurvePos, laneData, curveData);
		if (path2.Length != 0)
		{
			int num7 = elementIndex - num6 + 1;
			int num8 = math.min(num7, path2.Length);
			for (int k = 0; k < num8; k++)
			{
				path[num6 + k] = path2[k];
			}
			if (path2.Length < num7)
			{
				path.RemoveRange(num6 + path2.Length, num7 - path2.Length);
			}
			else
			{
				for (int l = num7; l < path2.Length; l++)
				{
					path.Insert(num6 + l, path2[l]);
				}
			}
		}
		path2.Dispose();
	}

	private static void OffsetPathTarget_EdgeLane(ref Unity.Mathematics.Random random, float distance, int elementIndex, DynamicBuffer<PathElement> path, ComponentLookup<Owner> ownerData, ComponentLookup<Lane> laneData, ComponentLookup<Curve> curveData, BufferLookup<Game.Net.SubLane> subLanes)
	{
		PathElement value = path[elementIndex];
		Curve curve = curveData[value.m_Target];
		float num = random.NextFloat(0f - distance, distance);
		if (num >= 0f)
		{
			Bounds1 t = new Bounds1(value.m_TargetDelta.y, 1f);
			float length = num;
			if (MathUtils.ClampLength(curve.m_Bezier.xz, ref t, ref length))
			{
				value.m_TargetDelta.y = t.max;
				path[elementIndex] = value;
				return;
			}
			Entity entity = value.m_Target;
			if (NetUtils.FindNextLane(ref entity, ref ownerData, ref laneData, ref subLanes))
			{
				num = math.max(0f, num - length);
				t = new Bounds1(0f, 1f);
				MathUtils.ClampLength(curveData[entity].m_Bezier.xz, ref t, num);
				if (elementIndex > 0 && path[elementIndex - 1].m_Target == entity)
				{
					path.RemoveAt(elementIndex--);
					value = path[elementIndex];
					value.m_TargetDelta.y = t.max;
					path[elementIndex] = value;
				}
				else
				{
					path.Insert(elem: new PathElement
					{
						m_Target = value.m_Target,
						m_TargetDelta = new float2(value.m_TargetDelta.x, 1f)
					}, index: elementIndex++);
					value.m_Target = entity;
					value.m_TargetDelta = new float2(0f, t.max);
					path[elementIndex] = value;
				}
			}
			else
			{
				value.m_TargetDelta.y = math.saturate(value.m_TargetDelta.y + (1f - value.m_TargetDelta.y) * num / distance);
				path[elementIndex] = value;
			}
			return;
		}
		num = 0f - num;
		Bounds1 t2 = new Bounds1(0f, value.m_TargetDelta.y);
		float length2 = num;
		if (MathUtils.ClampLengthInverse(curve.m_Bezier.xz, ref t2, ref length2))
		{
			value.m_TargetDelta.y = t2.min;
			path[elementIndex] = value;
			return;
		}
		Entity entity2 = value.m_Target;
		if (NetUtils.FindPrevLane(ref entity2, ref ownerData, ref laneData, ref subLanes))
		{
			num = math.max(0f, num - length2);
			t2 = new Bounds1(0f, 1f);
			MathUtils.ClampLengthInverse(curveData[entity2].m_Bezier.xz, ref t2, num);
			if (elementIndex > 0 && path[elementIndex - 1].m_Target == entity2)
			{
				path.RemoveAt(elementIndex--);
				value = path[elementIndex];
				value.m_TargetDelta.y = t2.min;
				path[elementIndex] = value;
			}
			else
			{
				path.Insert(elem: new PathElement
				{
					m_Target = value.m_Target,
					m_TargetDelta = new float2(value.m_TargetDelta.x, 0f)
				}, index: elementIndex++);
				value.m_Target = entity2;
				value.m_TargetDelta = new float2(1f, t2.min);
				path[elementIndex] = value;
			}
		}
		else
		{
			value.m_TargetDelta.y = math.saturate(value.m_TargetDelta.y - value.m_TargetDelta.y * num / distance);
			path[elementIndex] = value;
		}
	}

	public static bool GetBoardingVehicle(Entity currentLane, Entity currentWaypoint, Entity targetWaypoint, uint minDeparture, ref ComponentLookup<Owner> ownerData, ref ComponentLookup<Target> targetData, ref ComponentLookup<Connected> connectedData, ref ComponentLookup<BoardingVehicle> boardingVehicleData, ref ComponentLookup<CurrentRoute> currentRouteData, ref ComponentLookup<AccessLane> accessLaneData, ref ComponentLookup<Game.Vehicles.PublicTransport> publicTransportData, ref ComponentLookup<Game.Vehicles.Taxi> taxiData, ref BufferLookup<ConnectedRoute> connectedRoutes, ref BufferLookup<RouteWaypoint> routeWaypoints, out Entity vehicle, out bool testing, out bool obsolete)
	{
		if (currentLane != currentWaypoint && accessLaneData.TryGetComponent(currentWaypoint, out var componentData))
		{
			Entity entity = Entity.Null;
			Entity entity2 = Entity.Null;
			if (ownerData.TryGetComponent(currentLane, out var componentData2))
			{
				entity = componentData2.m_Owner;
			}
			if (ownerData.TryGetComponent(componentData.m_Lane, out var componentData3))
			{
				entity2 = componentData3.m_Owner;
			}
			if (entity != entity2 && (!connectedData.TryGetComponent(currentWaypoint, out var componentData4) || componentData4.m_Connected != currentLane))
			{
				vehicle = Entity.Null;
				testing = false;
				obsolete = true;
				return false;
			}
		}
		if (boardingVehicleData.TryGetComponent(currentWaypoint, out var componentData5))
		{
			if (componentData5.m_Vehicle != Entity.Null && taxiData.TryGetComponent(componentData5.m_Vehicle, out var componentData6) && (componentData6.m_State & TaxiFlags.Boarding) != 0)
			{
				vehicle = componentData5.m_Vehicle;
				testing = false;
				obsolete = false;
				return true;
			}
			vehicle = Entity.Null;
			testing = false;
			obsolete = false;
			return false;
		}
		if (connectedData.TryGetComponent(currentWaypoint, out var componentData7) && connectedData.TryGetComponent(targetWaypoint, out var componentData8))
		{
			Entity connected = componentData7.m_Connected;
			Entity connected2 = componentData8.m_Connected;
			if (boardingVehicleData.TryGetComponent(connected, out componentData5) && connectedRoutes.TryGetBuffer(connected2, out var bufferData))
			{
				if (currentRouteData.TryGetComponent(componentData5.m_Vehicle, out var componentData9) && (!publicTransportData.TryGetComponent(componentData5.m_Vehicle, out var componentData10) || ((componentData10.m_State & (PublicTransportFlags.EnRoute | PublicTransportFlags.Boarding)) == (PublicTransportFlags.EnRoute | PublicTransportFlags.Boarding) && (componentData10.m_DepartureFrame >= minDeparture || componentData10.m_MaxBoardingDistance != float.MaxValue))))
				{
					for (int i = 0; i < bufferData.Length; i++)
					{
						if (ownerData[bufferData[i].m_Waypoint].m_Owner == componentData9.m_Route)
						{
							if (!CheckVehicleRoute(connected, connected2, componentData5.m_Vehicle, componentData9.m_Route, ref targetData, ref connectedData, ref routeWaypoints))
							{
								break;
							}
							vehicle = componentData5.m_Vehicle;
							testing = false;
							obsolete = false;
							return true;
						}
					}
				}
				if (currentRouteData.TryGetComponent(componentData5.m_Testing, out componentData9) && (!publicTransportData.TryGetComponent(componentData5.m_Testing, out var componentData11) || (componentData11.m_State & (PublicTransportFlags.EnRoute | PublicTransportFlags.Testing | PublicTransportFlags.RequireStop)) == (PublicTransportFlags.EnRoute | PublicTransportFlags.Testing)))
				{
					for (int j = 0; j < bufferData.Length; j++)
					{
						if (ownerData[bufferData[j].m_Waypoint].m_Owner == componentData9.m_Route)
						{
							if (!CheckVehicleRoute(connected, connected2, componentData5.m_Testing, componentData9.m_Route, ref targetData, ref connectedData, ref routeWaypoints))
							{
								break;
							}
							vehicle = componentData5.m_Testing;
							testing = true;
							obsolete = false;
							return false;
						}
					}
				}
				vehicle = Entity.Null;
				testing = false;
				obsolete = false;
				return false;
			}
		}
		vehicle = Entity.Null;
		testing = false;
		obsolete = true;
		return false;
	}

	private static bool CheckVehicleRoute(Entity transportStop1, Entity transportStop2, Entity vehicle, Entity route, ref ComponentLookup<Target> targetData, ref ComponentLookup<Connected> connectedData, ref BufferLookup<RouteWaypoint> routeWaypointData)
	{
		if (targetData.TryGetComponent(vehicle, out var componentData) && routeWaypointData.TryGetBuffer(route, out var bufferData))
		{
			int num = -1;
			int num2 = -1;
			for (int i = 0; i < bufferData.Length; i++)
			{
				Connected componentData2;
				if (bufferData[i].m_Waypoint == componentData.m_Target)
				{
					num = 0;
				}
				else if (connectedData.TryGetComponent(bufferData[i].m_Waypoint, out componentData2))
				{
					if (componentData2.m_Connected == transportStop2)
					{
						if (num == 0)
						{
							return true;
						}
						num = 2;
					}
					else if (componentData2.m_Connected == transportStop1)
					{
						num = 1;
					}
				}
				num2 = math.select(num2, num, num2 == -1);
			}
			if (num == 0 && num2 == 2)
			{
				return true;
			}
		}
		return false;
	}

	public static bool ShouldExitVehicle(Entity nextLane, Entity targetWaypoint, Entity currentVehicle, ref ComponentLookup<Owner> ownerData, ref ComponentLookup<Connected> connectedData, ref ComponentLookup<BoardingVehicle> boardingVehicleData, ref ComponentLookup<CurrentRoute> currentRouteData, ref ComponentLookup<AccessLane> accessLaneData, ref ComponentLookup<Game.Vehicles.PublicTransport> publicTransportData, ref BufferLookup<ConnectedRoute> connectedRoutes, bool testing, out bool obsolete)
	{
		if (connectedData.TryGetComponent(targetWaypoint, out var componentData) && currentRouteData.TryGetComponent(currentVehicle, out var componentData2))
		{
			Entity connected = componentData.m_Connected;
			if (boardingVehicleData.TryGetComponent(connected, out var componentData3) && connectedRoutes.TryGetBuffer(connected, out var bufferData))
			{
				if ((testing ? componentData3.m_Testing : componentData3.m_Vehicle) == currentVehicle)
				{
					obsolete = false;
					if (nextLane != Entity.Null && accessLaneData.TryGetComponent(targetWaypoint, out var componentData4))
					{
						Entity entity = Entity.Null;
						Entity entity2 = Entity.Null;
						if (ownerData.TryGetComponent(nextLane, out var componentData5))
						{
							entity = componentData5.m_Owner;
						}
						if (ownerData.TryGetComponent(componentData4.m_Lane, out var componentData6))
						{
							entity2 = componentData6.m_Owner;
						}
						if (entity != entity2)
						{
							obsolete = true;
						}
					}
					return true;
				}
				if (publicTransportData.TryGetComponent(currentVehicle, out var componentData7) && (componentData7.m_State & PublicTransportFlags.EnRoute) == 0)
				{
					obsolete = true;
					return true;
				}
				for (int i = 0; i < bufferData.Length; i++)
				{
					if (ownerData[bufferData[i].m_Waypoint].m_Owner == componentData2.m_Route)
					{
						obsolete = false;
						return false;
					}
				}
			}
		}
		obsolete = true;
		return true;
	}

	public static float UpdateAverageTravelTime(float oldTravelTime, uint departureFrame, uint arrivalFrame)
	{
		if (departureFrame == 0)
		{
			return oldTravelTime;
		}
		float num = (float)(arrivalFrame - departureFrame) / 60f;
		if (oldTravelTime == 0f)
		{
			return num;
		}
		return math.lerp(oldTravelTime, num, 0.5f);
	}

	public static float GetStopDuration(TransportLineData prefabLineData, TransportStop transportStop)
	{
		return prefabLineData.m_StopDuration / math.max(0.25f, transportStop.m_LoadingFactor);
	}

	public static uint CalculateDepartureFrame(TransportLine transportLine, TransportLineData prefabLineData, DynamicBuffer<RouteModifier> routeModifiers, float targetStopTime, uint lastDepartureFrame, uint simulationFrame)
	{
		float num = (float)(simulationFrame - lastDepartureFrame) / 60f;
		if (num >= 0f)
		{
			float value = prefabLineData.m_DefaultVehicleInterval;
			ApplyModifier(ref value, routeModifiers, RouteModifierType.VehicleInterval);
			float vehicleInterval = transportLine.m_VehicleInterval;
			float unbunchingFactor = transportLine.m_UnbunchingFactor;
			float num2 = math.min(value, 2f * vehicleInterval * vehicleInterval / (num + vehicleInterval) - vehicleInterval) * unbunchingFactor;
			num2 = math.max(num2 + targetStopTime, 1f);
			return simulationFrame + (uint)(num2 * 60f);
		}
		return simulationFrame;
	}

	public static PathMethod GetPathMethods(RouteConnectionType routeConnectionType, RouteType routeType, TrackTypes trackTypes, RoadTypes roadTypes, SizeClass sizeClass)
	{
		switch (routeConnectionType)
		{
		case RouteConnectionType.Pedestrian:
			return PathMethod.Pedestrian;
		case RouteConnectionType.Road:
		case RouteConnectionType.Air:
		{
			PathMethod pathMethod = PathMethod.Road;
			if (routeType == RouteType.WorkRoute)
			{
				pathMethod |= PathMethod.Offroad;
			}
			if ((int)sizeClass <= 1)
			{
				pathMethod |= PathMethod.MediumRoad;
			}
			if ((roadTypes & (RoadTypes.Helicopter | RoadTypes.Airplane)) != RoadTypes.None)
			{
				pathMethod |= PathMethod.Flying;
			}
			return pathMethod;
		}
		case RouteConnectionType.Track:
			return PathMethod.Track;
		default:
			return ~(PathMethod.Pedestrian | PathMethod.Road | PathMethod.Parking | PathMethod.PublicTransportDay | PathMethod.Track | PathMethod.Taxi | PathMethod.CargoTransport | PathMethod.CargoLoading | PathMethod.Flying | PathMethod.PublicTransportNight | PathMethod.Boarding | PathMethod.Offroad | PathMethod.SpecialParking | PathMethod.MediumRoad | PathMethod.Bicycle | PathMethod.BicycleParking);
		}
	}

	public static bool CheckOption(Route route, RouteOption option)
	{
		return (route.m_OptionMask & (uint)(1 << (int)option)) != 0;
	}

	public static bool HasOption(RouteOptionData optionData, RouteOption option)
	{
		return (optionData.m_OptionMask & (uint)(1 << (int)option)) != 0;
	}

	public static void ApplyModifier(ref float value, DynamicBuffer<RouteModifier> modifiers, RouteModifierType type)
	{
		if (modifiers.Length > (int)type)
		{
			float2 delta = modifiers[(int)type].m_Delta;
			value += delta.x;
			value += value * delta.y;
		}
	}

	public static PathMethod GetTaxiMethods(Game.Creatures.Resident resident)
	{
		if ((resident.m_Flags & ResidentFlags.IgnoreTaxi) != ResidentFlags.None)
		{
			return ~(PathMethod.Pedestrian | PathMethod.Road | PathMethod.Parking | PathMethod.PublicTransportDay | PathMethod.Track | PathMethod.Taxi | PathMethod.CargoTransport | PathMethod.CargoLoading | PathMethod.Flying | PathMethod.PublicTransportNight | PathMethod.Boarding | PathMethod.Offroad | PathMethod.SpecialParking | PathMethod.MediumRoad | PathMethod.Bicycle | PathMethod.BicycleParking);
		}
		return PathMethod.Taxi;
	}

	public static PathMethod GetPublicTransportMethods(float timeOfDay, float predictionOffset = 1f / 48f)
	{
		timeOfDay = math.frac(timeOfDay + predictionOffset);
		if (!(timeOfDay >= 0.25f) || !(timeOfDay < 11f / 12f))
		{
			return PathMethod.PublicTransportNight;
		}
		return PathMethod.PublicTransportDay;
	}

	public static PathMethod GetPublicTransportMethods(Game.Creatures.Resident resident, float timeOfDay, float predictionOffset = 1f / 48f)
	{
		if ((resident.m_Flags & ResidentFlags.IgnoreTransport) != ResidentFlags.None)
		{
			return ~(PathMethod.Pedestrian | PathMethod.Road | PathMethod.Parking | PathMethod.PublicTransportDay | PathMethod.Track | PathMethod.Taxi | PathMethod.CargoTransport | PathMethod.CargoLoading | PathMethod.Flying | PathMethod.PublicTransportNight | PathMethod.Boarding | PathMethod.Offroad | PathMethod.SpecialParking | PathMethod.MediumRoad | PathMethod.Bicycle | PathMethod.BicycleParking);
		}
		timeOfDay = math.frac(timeOfDay + predictionOffset);
		if (!(timeOfDay >= 0.25f) || !(timeOfDay < 11f / 12f))
		{
			return PathMethod.PublicTransportNight;
		}
		return PathMethod.PublicTransportDay;
	}

	public static bool CheckVehicleModel(DynamicBuffer<VehicleModel> vehicleModels, PrefabRef prefabRef)
	{
		bool2 x = false;
		for (int i = 0; i < vehicleModels.Length; i++)
		{
			VehicleModel vehicleModel = vehicleModels[i];
			x |= new bool2(vehicleModel.m_PrimaryPrefab != Entity.Null, vehicleModel.m_SecondaryPrefab != Entity.Null);
		}
		if (x.x)
		{
			for (int j = 0; j < vehicleModels.Length; j++)
			{
				VehicleModel vehicleModel2 = vehicleModels[j];
				x.x &= prefabRef.m_Prefab != vehicleModel2.m_PrimaryPrefab;
			}
		}
		return !math.any(x);
	}

	public static bool CheckVehicleModel(DynamicBuffer<VehicleModel> vehicleModels, PrefabRef prefabRef, DynamicBuffer<LayoutElement> layout, ref ComponentLookup<PrefabRef> prefabRefData, ref ComponentLookup<MultipleUnitTrainData> prefabMultipleUnitTrainData)
	{
		bool2 x = false;
		for (int i = 0; i < vehicleModels.Length; i++)
		{
			VehicleModel vehicleModel = vehicleModels[i];
			x |= new bool2(vehicleModel.m_PrimaryPrefab != Entity.Null, vehicleModel.m_SecondaryPrefab != Entity.Null);
		}
		if (x.y)
		{
			x.y &= !prefabMultipleUnitTrainData.HasComponent(prefabRef.m_Prefab);
		}
		if (x.x)
		{
			for (int j = 0; j < vehicleModels.Length; j++)
			{
				VehicleModel vehicleModel2 = vehicleModels[j];
				x.x &= prefabRef.m_Prefab != vehicleModel2.m_PrimaryPrefab;
			}
		}
		if (x.y && layout.IsCreated)
		{
			for (int k = 0; k < layout.Length; k++)
			{
				if (!x.y)
				{
					break;
				}
				PrefabRef prefabRef2 = prefabRefData[layout[k].m_Vehicle];
				for (int l = 0; l < vehicleModels.Length; l++)
				{
					VehicleModel vehicleModel3 = vehicleModels[l];
					x.y &= prefabRef2.m_Prefab != vehicleModel3.m_SecondaryPrefab;
				}
			}
		}
		return !math.any(x);
	}

	public static int GetMaxTaxiCount(WaitingPassengers waitingPassengers)
	{
		return 3 + (waitingPassengers.m_Count + 3 >> 2);
	}
}
