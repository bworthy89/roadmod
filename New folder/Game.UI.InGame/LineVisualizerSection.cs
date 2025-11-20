using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Colossal.UI.Binding;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Creatures;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Rendering;
using Game.Routes;
using Game.Simulation;
using Game.Vehicles;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class LineVisualizerSection : InfoSectionBase
{
	public enum LineType
	{
		Passenger,
		Cargo,
		Work
	}

	private readonly struct LineStop
	{
		public Entity entity { get; }

		public float position { get; }

		public int cargo { get; }

		public LineType type { get; }

		public bool isOutsideConnection { get; }

		public int capacity { get; }

		public LineStop(Entity entity, float position, int cargo, int capacity, LineType type = LineType.Passenger, bool isOutsideConnection = false)
		{
			this.entity = entity;
			this.position = position;
			this.cargo = cargo;
			this.type = type;
			this.isOutsideConnection = isOutsideConnection;
			this.capacity = capacity;
		}

		public void Bind(IJsonWriter binder, NameSystem nameSystem)
		{
			binder.TypeBegin(GetType().FullName);
			binder.PropertyName("entity");
			binder.Write(entity);
			binder.PropertyName("name");
			nameSystem.BindName(binder, entity);
			binder.PropertyName("position");
			binder.Write(position);
			binder.PropertyName("cargo");
			binder.Write(cargo);
			binder.PropertyName("capacity");
			binder.Write(capacity);
			binder.PropertyName("type");
			binder.Write((int)type);
			binder.PropertyName("isOutsideConnection");
			binder.Write(isOutsideConnection);
			binder.TypeEnd();
		}
	}

	private readonly struct LineVehicle
	{
		public Entity entity { get; }

		public float position { get; }

		public int cargo { get; }

		public int capacity { get; }

		public LineType type { get; }

		public LineVehicle(Entity entity, float position, int cargo, int capacity, LineType type = LineType.Passenger)
		{
			this.entity = entity;
			this.position = position;
			this.cargo = cargo;
			this.capacity = capacity;
			this.type = type;
		}

		public void Bind(IJsonWriter binder, NameSystem nameSystem)
		{
			binder.TypeBegin(GetType().FullName);
			binder.PropertyName("entity");
			binder.Write(entity);
			binder.PropertyName("name");
			nameSystem.BindName(binder, entity);
			binder.PropertyName("cargo");
			binder.Write(cargo);
			binder.PropertyName("capacity");
			binder.Write(capacity);
			binder.PropertyName("position");
			binder.Write(position);
			binder.PropertyName("type");
			binder.Write((int)type);
			binder.TypeEnd();
		}
	}

	private readonly struct LineSegment : IJsonWritable
	{
		public float start { get; }

		public float end { get; }

		public bool broken { get; }

		public LineSegment(float start, float end, bool broken)
		{
			this.start = start;
			this.end = end;
			this.broken = broken;
		}

		public void Write(IJsonWriter binder)
		{
			binder.TypeBegin(GetType().FullName);
			binder.PropertyName("start");
			binder.Write(start);
			binder.PropertyName("end");
			binder.Write(end);
			binder.PropertyName("broken");
			binder.Write(broken);
			binder.TypeEnd();
		}
	}

	private enum Result
	{
		IsVisible = 0,
		ShouldUpdateSelected = 1,
		IsCargo = 2,
		IsWork = 3,
		Count = 4,
		Entity = 0,
		Color = 0,
		StopsCapacity = 0
	}

	[BurstCompile]
	private struct VisibilityJob : IJob
	{
		[ReadOnly]
		public Entity m_SelectedEntity;

		[ReadOnly]
		public Entity m_SelectedRouteEntity;

		[ReadOnly]
		public ComponentLookup<Route> m_Routes;

		[ReadOnly]
		public ComponentLookup<TransportLine> m_TransportLines;

		[ReadOnly]
		public ComponentLookup<WorkRoute> m_WorkRoutes;

		[ReadOnly]
		public ComponentLookup<Game.Routes.TransportStop> m_TransportStops;

		[ReadOnly]
		public ComponentLookup<Game.Routes.WorkStop> m_WorkStops;

		[ReadOnly]
		public ComponentLookup<TaxiStand> m_TaxiStands;

		[ReadOnly]
		public ComponentLookup<Vehicle> m_Vehicles;

		[ReadOnly]
		public ComponentLookup<Owner> m_Owners;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PublicTransport> m_PublicTransports;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.CargoTransport> m_CargoTransports;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.WorkVehicle> m_WorkVehicles;

		[ReadOnly]
		public ComponentLookup<CurrentRoute> m_CurrentRoutes;

		[ReadOnly]
		public ComponentLookup<Attached> m_Attached;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> m_RouteWaypointBuffers;

		[ReadOnly]
		public BufferLookup<RouteSegment> m_RouteSegmentBuffers;

		[ReadOnly]
		public BufferLookup<RouteVehicle> m_RouteVehicleBuffers;

		[ReadOnly]
		public BufferLookup<ConnectedRoute> m_ConnectedRouteBuffers;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjectBuffers;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> m_SubNetBuffers;

		[ReadOnly]
		public BufferLookup<SubRoute> m_SubRouteBuffers;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgradeBuffers;

		public NativeArray<bool> m_BoolResult;

		public NativeArray<Entity> m_EntityResult;

		public void Execute()
		{
			if (IsLine(m_SelectedEntity))
			{
				m_BoolResult[0] = true;
				m_BoolResult[1] = true;
				m_EntityResult[0] = m_SelectedEntity;
				return;
			}
			if (IsVehicle(out var routeEntity))
			{
				m_BoolResult[0] = true;
				m_BoolResult[1] = true;
				m_EntityResult[0] = routeEntity;
				return;
			}
			NativeList<Entity> routes = new NativeList<Entity>(10, Allocator.Temp);
			bool num = TryGetStationRoutes(m_SelectedEntity, routes);
			bool flag = TryGetStopRoutes(m_SelectedEntity, routes);
			if (num || flag)
			{
				bool flag2 = false;
				Entity entity = Entity.Null;
				for (int num2 = routes.Length - 1; num2 >= 0; num2--)
				{
					entity = routes[num2];
					if (entity == m_SelectedRouteEntity)
					{
						flag2 = true;
					}
				}
				if (entity != Entity.Null)
				{
					if (!flag2)
					{
						m_BoolResult[0] = true;
						m_BoolResult[1] = true;
						m_EntityResult[0] = entity;
					}
					else
					{
						m_BoolResult[0] = true;
						m_BoolResult[1] = false;
						m_EntityResult[0] = Entity.Null;
					}
					return;
				}
			}
			m_BoolResult[0] = false;
			m_BoolResult[1] = false;
			m_EntityResult[0] = Entity.Null;
		}

		private bool IsLine(Entity entity)
		{
			if (m_Routes.HasComponent(entity) && m_RouteWaypointBuffers.HasBuffer(entity) && m_RouteSegmentBuffers.HasBuffer(entity) && m_RouteVehicleBuffers.HasBuffer(entity))
			{
				if (!m_TransportLines.HasComponent(entity))
				{
					return m_WorkRoutes.HasComponent(entity);
				}
				return true;
			}
			return false;
		}

		private bool TryGetStationRoutes(Entity entity, NativeList<Entity> routes)
		{
			if (m_Attached.TryGetComponent(entity, out var componentData) && m_SubRouteBuffers.TryGetBuffer(componentData.m_Parent, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					SubRoute subRoute = bufferData[i];
					routes.Add(in subRoute.m_Route);
				}
			}
			if (m_SubObjectBuffers.TryGetBuffer(entity, out var bufferData2))
			{
				for (int j = 0; j < bufferData2.Length; j++)
				{
					TryGetStopRoutes(bufferData2[j].m_SubObject, routes);
				}
			}
			if (m_SubNetBuffers.TryGetBuffer(entity, out var bufferData3))
			{
				for (int k = 0; k < bufferData3.Length; k++)
				{
					if (m_SubObjectBuffers.TryGetBuffer(bufferData3[k].m_SubNet, out var bufferData4))
					{
						for (int l = 0; l < bufferData4.Length; l++)
						{
							TryGetStopRoutes(bufferData4[l].m_SubObject, routes);
						}
					}
				}
			}
			if (m_InstalledUpgradeBuffers.TryGetBuffer(entity, out var bufferData5))
			{
				foreach (InstalledUpgrade item in bufferData5)
				{
					TryGetStationRoutes(item.m_Upgrade, routes);
				}
			}
			return routes.Length > 0;
		}

		private bool TryGetStopRoutes(Entity entity, NativeList<Entity> routes)
		{
			if (m_ConnectedRouteBuffers.TryGetBuffer(entity, out var bufferData) && (m_TransportStops.HasComponent(entity) || m_WorkStops.HasComponent(entity)) && !m_TaxiStands.HasComponent(entity) && bufferData.Length > 0)
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					ConnectedRoute connectedRoute = bufferData[i];
					if (m_Owners.TryGetComponent(connectedRoute.m_Waypoint, out var componentData) && IsLine(componentData.m_Owner))
					{
						routes.Add(in componentData.m_Owner);
					}
				}
				return true;
			}
			return false;
		}

		private bool IsVehicle(out Entity routeEntity)
		{
			if (m_Vehicles.HasComponent(m_SelectedEntity) && m_Owners.HasComponent(m_SelectedEntity) && (m_PublicTransports.HasComponent(m_SelectedEntity) || m_CargoTransports.HasComponent(m_SelectedEntity) || m_WorkVehicles.HasComponent(m_SelectedEntity)) && m_CurrentRoutes.TryGetComponent(m_SelectedEntity, out var componentData) && IsLine(componentData.m_Route))
			{
				routeEntity = componentData.m_Route;
				return true;
			}
			routeEntity = Entity.Null;
			return false;
		}
	}

	[BurstCompile]
	private struct UpdateJob : IJob
	{
		[ReadOnly]
		public bool m_RightHandTraffic;

		[ReadOnly]
		public Entity m_RouteEntity;

		[ReadOnly]
		public uint m_RenderingFrameIndex;

		[ReadOnly]
		public float m_RenderingFrameTime;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrames;

		[ReadOnly]
		public ComponentLookup<Game.Routes.Color> m_Colors;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformation;

		[ReadOnly]
		public ComponentLookup<Connected> m_Connected;

		[ReadOnly]
		public ComponentLookup<WaitingPassengers> m_WaitingPassengers;

		[ReadOnly]
		public ComponentLookup<Position> m_Positions;

		[ReadOnly]
		public ComponentLookup<RouteLane> m_RouteLanes;

		[ReadOnly]
		public ComponentLookup<CurrentRoute> m_CurrentRoutes;

		[ReadOnly]
		public ComponentLookup<Target> m_Targets;

		[ReadOnly]
		public ComponentLookup<PathOwner> m_PathOwners;

		[ReadOnly]
		public ComponentLookup<Owner> m_Owners;

		[ReadOnly]
		public ComponentLookup<Waypoint> m_Waypoints;

		[ReadOnly]
		public ComponentLookup<Area> m_Areas;

		[ReadOnly]
		public ComponentLookup<Train> m_Trains;

		[ReadOnly]
		public ComponentLookup<Curve> m_Curves;

		[ReadOnly]
		public ComponentLookup<SlaveLane> m_SlaveLanes;

		[ReadOnly]
		public ComponentLookup<CarCurrentLane> m_CarCurrentLanes;

		[ReadOnly]
		public ComponentLookup<TrainCurrentLane> m_TrainCurrentLanes;

		[ReadOnly]
		public ComponentLookup<WatercraftCurrentLane> m_WatercraftCurrentLanes;

		[ReadOnly]
		public ComponentLookup<AircraftCurrentLane> m_AircraftCurrentLanes;

		[ReadOnly]
		public ComponentLookup<AreaLane> m_AreaLanes;

		[ReadOnly]
		public ComponentLookup<Game.Creatures.Pet> m_Pets;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<TransportLineData> m_TransportLineData;

		[ReadOnly]
		public ComponentLookup<WorkRouteData> m_WorkRouteData;

		[ReadOnly]
		public ComponentLookup<TrainData> m_TrainDatas;

		[ReadOnly]
		public ComponentLookup<PublicTransportVehicleData> m_PublicTransportVehicleDatas;

		[ReadOnly]
		public ComponentLookup<CargoTransportVehicleData> m_CargoTransportVehicleDatas;

		[ReadOnly]
		public ComponentLookup<Game.Routes.TransportStop> m_TransportStops;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_Transforms;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> m_StorageLimitDatas;

		[ReadOnly]
		public ComponentLookup<RouteData> m_RouteDatas;

		[ReadOnly]
		public ComponentLookup<Attachment> m_Attachments;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public BufferLookup<Game.Economy.Resources> m_EconomyResourcesBuffers;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> m_RouteWaypointBuffers;

		[ReadOnly]
		public BufferLookup<RouteSegment> m_RouteSegmentBuffers;

		[ReadOnly]
		public BufferLookup<RouteVehicle> m_RouteVehicleBuffers;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElementBuffers;

		[ReadOnly]
		public BufferLookup<CarNavigationLane> m_CarNavigationLaneBuffers;

		[ReadOnly]
		public BufferLookup<TrainNavigationLane> m_TrainNavigationLaneBuffers;

		[ReadOnly]
		public BufferLookup<WatercraftNavigationLane> m_WatercraftNavigationLaneBuffers;

		[ReadOnly]
		public BufferLookup<AircraftNavigationLane> m_AircraftNavigationLaneBuffers;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElementBuffers;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLaneBuffers;

		[ReadOnly]
		public BufferLookup<Passenger> m_PassengerBuffers;

		[ReadOnly]
		public BufferLookup<TransformFrame> m_TransformFrames;

		[ReadOnly]
		public BufferLookup<BuildingUpgradeElement> m_BuildingUpgradeElements;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjectBuffers;

		[ReadOnly]
		public BufferLookup<ConnectedRoute> m_ConnectedRoute;

		public NativeList<LineSegment> m_SegmentsResult;

		public NativeList<LineStop> m_StopsResult;

		public NativeList<LineVehicle> m_VehiclesResult;

		public NativeArray<Color32> m_ColorResult;

		public NativeArray<int> m_StopCapacityResult;

		public NativeArray<bool> m_BoolResult;

		public void Execute()
		{
			NativeList<float> nativeList = new NativeList<float>(Allocator.Temp);
			float value = 0f;
			m_BoolResult[2] = false;
			m_BoolResult[3] = false;
			m_ColorResult[0] = m_Colors[m_RouteEntity].m_Color;
			m_StopCapacityResult[0] = 0;
			DynamicBuffer<RouteWaypoint> waypoints = m_RouteWaypointBuffers[m_RouteEntity];
			DynamicBuffer<RouteSegment> routeSegments = m_RouteSegmentBuffers[m_RouteEntity];
			for (int i = 0; i < routeSegments.Length; i++)
			{
				nativeList.Add(in value);
				value += GetSegmentLength(waypoints, routeSegments, i);
			}
			if (value == 0f)
			{
				return;
			}
			for (int j = 0; j < routeSegments.Length; j++)
			{
				if (m_PathInformation.TryGetComponent(routeSegments[j].m_Segment, out var componentData))
				{
					float start = nativeList[j] / value;
					float end = ((j < routeSegments.Length - 1) ? (nativeList[j + 1] / value) : 1f);
					bool broken = componentData.m_Origin == Entity.Null && componentData.m_Destination == Entity.Null;
					m_SegmentsResult.Add(new LineSegment(start, end, broken));
				}
			}
			bool flag = false;
			bool flag2 = false;
			if (m_PrefabRefs.TryGetComponent(m_RouteEntity, out var componentData2))
			{
				if (m_TransportLineData.TryGetComponent(componentData2.m_Prefab, out var componentData3) && componentData3.m_CargoTransport)
				{
					flag = true;
				}
				if (m_WorkRouteData.HasComponent(componentData2.m_Prefab))
				{
					flag2 = true;
				}
			}
			if (m_RouteVehicleBuffers.TryGetBuffer(m_RouteEntity, out var bufferData))
			{
				for (int k = 0; k < bufferData.Length; k++)
				{
					Entity vehicle = bufferData[k].m_Vehicle;
					if (GetVehiclePosition(m_RouteEntity, vehicle, out var prevWaypointIndex, out var distanceFromWaypoint, out var distanceToWaypoint, out var unknownPath))
					{
						int num = prevWaypointIndex;
						float segmentLength = GetSegmentLength(waypoints, routeSegments, num);
						float num2 = nativeList[num];
						num2 = ((!unknownPath) ? (num2 + math.max(segmentLength - distanceToWaypoint, 0f)) : (num2 + segmentLength * distanceFromWaypoint / math.max(1f, distanceFromWaypoint + distanceToWaypoint)));
						(int, int) cargo = GetCargo(vehicle);
						int item = cargo.Item1;
						int item2 = cargo.Item2;
						float num3 = math.frac(num2 / value);
						m_VehiclesResult.Add(new LineVehicle(vehicle, m_RightHandTraffic ? (1f - num3) : num3, item, item2, flag ? LineType.Cargo : (flag2 ? LineType.Work : LineType.Passenger)));
						if (item2 > m_StopCapacityResult[0])
						{
							m_StopCapacityResult[0] = item2;
						}
					}
				}
			}
			for (int l = 0; l < waypoints.Length; l++)
			{
				if (!m_Connected.TryGetComponent(waypoints[l].m_Waypoint, out var componentData4) || !m_TransportStops.HasComponent(componentData4.m_Connected))
				{
					continue;
				}
				float num4 = nativeList[l] / value;
				int num5 = 0;
				int capacity = 80;
				Entity entity = componentData4.m_Connected;
				DynamicBuffer<Game.Economy.Resources> bufferData2;
				Owner componentData6;
				if (!flag && m_WaitingPassengers.TryGetComponent(waypoints[l].m_Waypoint, out var componentData5))
				{
					num5 = componentData5.m_Count;
					capacity = 80;
				}
				else if (m_EconomyResourcesBuffers.TryGetBuffer(componentData4.m_Connected, out bufferData2))
				{
					for (int m = 0; m < bufferData2.Length; m++)
					{
						num5 += bufferData2[m].m_Amount;
					}
				}
				else if (m_Owners.TryGetComponent(componentData4.m_Connected, out componentData6))
				{
					Owner componentData7;
					if (m_EconomyResourcesBuffers.TryGetBuffer(componentData6.m_Owner, out var bufferData3))
					{
						for (int n = 0; n < bufferData3.Length; n++)
						{
							num5 += bufferData3[n].m_Amount;
						}
						entity = componentData6.m_Owner;
					}
					else if (m_Owners.TryGetComponent(componentData6.m_Owner, out componentData7))
					{
						bool flag3 = false;
						if (m_PrefabRefs.TryGetComponent(componentData7.m_Owner, out var componentData8) && m_BuildingUpgradeElements.TryGetBuffer(componentData8.m_Prefab, out var bufferData4))
						{
							for (int num6 = 0; num6 < bufferData4.Length; num6++)
							{
								if (m_RouteDatas.HasComponent(bufferData4[num6].m_Upgrade))
								{
									flag3 = true;
									break;
								}
							}
						}
						Attachment componentData9;
						DynamicBuffer<Game.Economy.Resources> bufferData5;
						if (flag3)
						{
							entity = componentData6.m_Owner;
						}
						else if (m_Attachments.TryGetComponent(componentData7.m_Owner, out componentData9))
						{
							entity = componentData9.m_Attached;
						}
						else if (m_EconomyResourcesBuffers.TryGetBuffer(componentData7.m_Owner, out bufferData5))
						{
							for (int num7 = 0; num7 < bufferData5.Length; num7++)
							{
								num5 += bufferData5[num7].m_Amount;
							}
							entity = componentData7.m_Owner;
						}
					}
				}
				if (flag && m_PrefabRefs.TryGetComponent(entity, out var _))
				{
					(num5, capacity) = GetCargo(entity);
				}
				m_StopsResult.Add(new LineStop(entity, m_RightHandTraffic ? (1f - num4) : num4, num5, capacity, flag ? LineType.Cargo : (flag2 ? LineType.Work : LineType.Passenger), m_OutsideConnections.HasComponent(entity)));
			}
			m_BoolResult[2] = flag;
			m_BoolResult[3] = flag2;
		}

		private float GetSegmentLength(DynamicBuffer<RouteWaypoint> waypoints, DynamicBuffer<RouteSegment> routeSegments, int segmentIndex)
		{
			int index = math.select(segmentIndex + 1, 0, segmentIndex == waypoints.Length - 1);
			if (m_PathInformation.TryGetComponent(routeSegments[segmentIndex].m_Segment, out var componentData) && componentData.m_Destination != Entity.Null)
			{
				float num = componentData.m_Distance;
				Entity waypoint = waypoints[index].m_Waypoint;
				if (m_RouteLanes.TryGetComponent(waypoint, out var componentData2) && m_Curves.TryGetComponent(componentData2.m_StartLane, out var componentData3) && m_Curves.TryGetComponent(componentData2.m_EndLane, out var componentData4))
				{
					num += math.distance(MathUtils.Position(componentData3.m_Bezier, componentData2.m_StartCurvePos), MathUtils.Position(componentData4.m_Bezier, componentData2.m_EndCurvePos));
				}
				return num;
			}
			if (GetWaypointPosition(waypoints[segmentIndex].m_Waypoint, out var position) && GetWaypointPosition(waypoints[index].m_Waypoint, out var position2))
			{
				return math.max(0f, math.distance(position, position2));
			}
			return 0f;
		}

		private bool GetWaypointPosition(Entity waypoint, out float3 position)
		{
			if (m_Positions.TryGetComponent(waypoint, out var componentData))
			{
				if (m_RouteLanes.TryGetComponent(waypoint, out var componentData2) && m_Curves.TryGetComponent(componentData2.m_EndLane, out var componentData3))
				{
					position = MathUtils.Position(componentData3.m_Bezier, componentData2.m_EndCurvePos);
					return true;
				}
				position = componentData.m_Position;
				return true;
			}
			position = default(float3);
			return false;
		}

		private bool GetTargetPosition(Entity target, out float3 position)
		{
			for (int i = 0; i < 10; i++)
			{
				if (m_Areas.HasComponent(target))
				{
					break;
				}
				if (m_Owners.TryGetComponent(target, out var componentData))
				{
					target = componentData.m_Owner;
					continue;
				}
				position = default(float3);
				return false;
			}
			if (m_Areas.HasComponent(target) && m_Owners.TryGetComponent(target, out var componentData2) && m_Transforms.TryGetComponent(componentData2.m_Owner, out var componentData3))
			{
				position = componentData3.m_Position;
				return true;
			}
			position = default(float3);
			return false;
		}

		private int GetStorageLimit(Entity entity, Entity prefab)
		{
			if (m_StorageLimitDatas.HasComponent(prefab))
			{
				StorageLimitData data = m_StorageLimitDatas[prefab];
				if (m_InstalledUpgrades.HasBuffer(entity))
				{
					UpgradeUtils.CombineStats(ref data, m_InstalledUpgrades[entity], ref m_PrefabRefs, ref m_StorageLimitDatas);
				}
				return data.m_Limit;
			}
			return 80;
		}

		private bool TryGetWaypointEntity(Entity target, Entity transportRoute, out Entity waypointEntity)
		{
			waypointEntity = Entity.Null;
			if (m_Waypoints.HasComponent(target))
			{
				waypointEntity = target;
				return true;
			}
			for (int i = 0; i < 10; i++)
			{
				if (!m_Owners.TryGetComponent(target, out var componentData))
				{
					break;
				}
				target = componentData.m_Owner;
				if (!m_SubObjectBuffers.TryGetBuffer(target, out var bufferData))
				{
					continue;
				}
				for (int j = 0; j < bufferData.Length; j++)
				{
					if (!m_ConnectedRoute.TryGetBuffer(bufferData[j].m_SubObject, out var bufferData2))
					{
						continue;
					}
					for (int k = 0; k < bufferData2.Length; k++)
					{
						if (m_Waypoints.HasComponent(bufferData2[k].m_Waypoint) && m_Owners.TryGetComponent(bufferData2[k].m_Waypoint, out var componentData2) && !(componentData2.m_Owner != transportRoute))
						{
							waypointEntity = bufferData2[k].m_Waypoint;
							return true;
						}
					}
				}
			}
			return false;
		}

		private bool GetVehiclePosition(Entity transportRoute, Entity transportVehicle, out int prevWaypointIndex, out float distanceFromWaypoint, out float distanceToWaypoint, out bool unknownPath)
		{
			prevWaypointIndex = 0;
			distanceFromWaypoint = 0f;
			distanceToWaypoint = 0f;
			unknownPath = true;
			if (!m_CurrentRoutes.TryGetComponent(transportVehicle, out var componentData))
			{
				return false;
			}
			if (!m_Targets.TryGetComponent(transportVehicle, out var componentData2))
			{
				return false;
			}
			if (!m_PathOwners.TryGetComponent(transportVehicle, out var componentData3))
			{
				return false;
			}
			if (!TryGetWaypointEntity(componentData2.m_Target, transportRoute, out var waypointEntity))
			{
				return false;
			}
			if (!m_Waypoints.TryGetComponent(waypointEntity, out var componentData4))
			{
				return false;
			}
			if (!GetWaypointPosition(componentData2.m_Target, out var position) && !GetTargetPosition(componentData2.m_Target, out position))
			{
				return false;
			}
			if (!m_RouteWaypointBuffers.TryGetBuffer(transportRoute, out var bufferData))
			{
				return false;
			}
			if (componentData.m_Route != transportRoute)
			{
				return false;
			}
			Entity entity = transportVehicle;
			PrefabRef componentData7;
			TrainData componentData8;
			if (m_LayoutElementBuffers.TryGetBuffer(transportVehicle, out var bufferData2) && bufferData2.Length != 0)
			{
				for (int i = 0; i < bufferData2.Length; i++)
				{
					if (m_PrefabRefs.TryGetComponent(bufferData2[i].m_Vehicle, out var componentData5) && m_TrainDatas.TryGetComponent(componentData5.m_Prefab, out var componentData6))
					{
						float num = math.csum(componentData6.m_AttachOffsets);
						distanceFromWaypoint -= num * 0.5f;
						distanceToWaypoint -= num * 0.5f;
					}
				}
				entity = bufferData2[0].m_Vehicle;
			}
			else if (m_PrefabRefs.TryGetComponent(transportVehicle, out componentData7) && m_TrainDatas.TryGetComponent(componentData7.m_Prefab, out componentData8))
			{
				float num2 = math.csum(componentData8.m_AttachOffsets);
				distanceFromWaypoint -= num2 * 0.5f;
				distanceToWaypoint -= num2 * 0.5f;
			}
			if (m_Trains.TryGetComponent(entity, out var componentData9) && m_PrefabRefs.TryGetComponent(entity, out var componentData10) && m_TrainDatas.TryGetComponent(componentData10.m_Prefab, out var componentData11))
			{
				if ((componentData9.m_Flags & Game.Vehicles.TrainFlags.Reversed) != 0)
				{
					distanceToWaypoint -= componentData11.m_AttachOffsets.y;
				}
				else
				{
					distanceToWaypoint -= componentData11.m_AttachOffsets.x;
				}
			}
			if (!m_EntityLookup.Exists(entity))
			{
				return false;
			}
			ArchetypeChunk chunk = m_EntityLookup[entity].Chunk;
			float3 position2;
			if (m_TransformFrames.TryGetBuffer(entity, out var bufferData3) && chunk.Has(m_UpdateFrames))
			{
				UpdateFrame sharedComponent = chunk.GetSharedComponent(m_UpdateFrames);
				ObjectInterpolateSystem.CalculateUpdateFrames(m_RenderingFrameIndex, m_RenderingFrameTime, sharedComponent.m_Index, out var updateFrame, out var updateFrame2, out var framePosition);
				TransformFrame frame = bufferData3[(int)updateFrame];
				TransformFrame frame2 = bufferData3[(int)updateFrame2];
				position2 = ObjectInterpolateSystem.CalculateTransform(frame, frame2, framePosition).m_Position;
			}
			else
			{
				if (!m_Transforms.TryGetComponent(entity, out var componentData12))
				{
					return false;
				}
				position2 = componentData12.m_Position;
			}
			prevWaypointIndex = math.select(componentData4.m_Index - 1, bufferData.Length - 1, componentData4.m_Index == 0);
			if (prevWaypointIndex >= bufferData.Length)
			{
				return false;
			}
			if (!GetWaypointPosition(bufferData[prevWaypointIndex].m_Waypoint, out var position3))
			{
				return false;
			}
			distanceFromWaypoint += math.distance(position3, position2);
			float3 position4 = position2;
			WatercraftCurrentLane componentData17;
			if ((componentData3.m_State & (PathFlags.Pending | PathFlags.Failed | PathFlags.Obsolete | PathFlags.Updated | PathFlags.DivertObsolete)) == 0 || (componentData3.m_State & (PathFlags.Failed | PathFlags.Append)) == PathFlags.Append)
			{
				unknownPath = false;
				TrainCurrentLane componentData14;
				WatercraftCurrentLane componentData15;
				AircraftCurrentLane componentData16;
				if (m_CarCurrentLanes.TryGetComponent(entity, out var componentData13))
				{
					AddDistance(ref distanceToWaypoint, ref position4, componentData13.m_Lane, componentData13.m_CurvePosition.xz);
				}
				else if (m_TrainCurrentLanes.TryGetComponent(entity, out componentData14))
				{
					AddDistance(ref distanceToWaypoint, ref position4, componentData14.m_Front.m_Lane, componentData14.m_Front.m_CurvePosition.yw);
				}
				else if (m_WatercraftCurrentLanes.TryGetComponent(entity, out componentData15))
				{
					if ((componentData15.m_LaneFlags & WatercraftLaneFlags.Area) == 0)
					{
						AddDistance(ref distanceToWaypoint, ref position4, componentData15.m_Lane, componentData15.m_CurvePosition.xz);
					}
				}
				else if (m_AircraftCurrentLanes.TryGetComponent(entity, out componentData16))
				{
					AddDistance(ref distanceToWaypoint, ref position4, componentData16.m_Lane, componentData16.m_CurvePosition.xz);
				}
				DynamicBuffer<TrainNavigationLane> bufferData5;
				DynamicBuffer<WatercraftNavigationLane> bufferData6;
				DynamicBuffer<AircraftNavigationLane> bufferData7;
				if (m_CarNavigationLaneBuffers.TryGetBuffer(transportVehicle, out var bufferData4))
				{
					for (int j = 0; j < bufferData4.Length; j++)
					{
						CarNavigationLane carNavigationLane = bufferData4[j];
						AddDistance(ref distanceToWaypoint, ref position4, carNavigationLane.m_Lane, carNavigationLane.m_CurvePosition);
					}
				}
				else if (m_TrainNavigationLaneBuffers.TryGetBuffer(transportVehicle, out bufferData5))
				{
					for (int k = 0; k < bufferData5.Length; k++)
					{
						TrainNavigationLane trainNavigationLane = bufferData5[k];
						AddDistance(ref distanceToWaypoint, ref position4, trainNavigationLane.m_Lane, trainNavigationLane.m_CurvePosition);
					}
				}
				else if (m_WatercraftNavigationLaneBuffers.TryGetBuffer(transportVehicle, out bufferData6))
				{
					for (int l = 0; l < bufferData6.Length; l++)
					{
						WatercraftNavigationLane watercraftNavigationLane = bufferData6[l];
						if ((watercraftNavigationLane.m_Flags & WatercraftLaneFlags.Area) == 0)
						{
							AddDistance(ref distanceToWaypoint, ref position4, watercraftNavigationLane.m_Lane, watercraftNavigationLane.m_CurvePosition);
						}
					}
				}
				else if (m_AircraftNavigationLaneBuffers.TryGetBuffer(transportVehicle, out bufferData7))
				{
					for (int m = 0; m < bufferData7.Length; m++)
					{
						AircraftNavigationLane aircraftNavigationLane = bufferData7[m];
						AddDistance(ref distanceToWaypoint, ref position4, aircraftNavigationLane.m_Lane, aircraftNavigationLane.m_CurvePosition);
					}
				}
				if (m_PathElementBuffers.TryGetBuffer(transportVehicle, out var bufferData8))
				{
					for (int n = componentData3.m_ElementIndex; n < bufferData8.Length; n++)
					{
						PathElement pathElement = bufferData8[n];
						if (!m_AreaLanes.HasComponent(pathElement.m_Target))
						{
							AddDistance(ref distanceToWaypoint, ref position4, pathElement.m_Target, pathElement.m_TargetDelta);
						}
					}
				}
				if ((componentData3.m_State & (PathFlags.Pending | PathFlags.Obsolete)) != 0 && (componentData3.m_State & PathFlags.Append) != 0 && m_RouteSegmentBuffers.TryGetBuffer(transportRoute, out var bufferData9) && m_PathElementBuffers.TryGetBuffer(bufferData9[prevWaypointIndex].m_Segment, out bufferData8))
				{
					for (int num3 = 0; num3 < bufferData8.Length; num3++)
					{
						PathElement pathElement2 = bufferData8[num3];
						AddDistance(ref distanceToWaypoint, ref position4, pathElement2.m_Target, pathElement2.m_TargetDelta);
					}
				}
			}
			else if (m_WatercraftCurrentLanes.TryGetComponent(entity, out componentData17) && (componentData17.m_LaneFlags & WatercraftLaneFlags.Area) != 0)
			{
				unknownPath = false;
				distanceToWaypoint = GetSegmentLength(m_RouteWaypointBuffers[m_RouteEntity], m_RouteSegmentBuffers[m_RouteEntity], prevWaypointIndex);
			}
			if (!m_WatercraftCurrentLanes.TryGetComponent(entity, out var componentData18) || ((componentData18.m_LaneFlags & WatercraftLaneFlags.Area) == 0 && !m_Areas.HasComponent(componentData2.m_Target)))
			{
				distanceToWaypoint += math.distance(position4, position);
			}
			distanceFromWaypoint = math.max(0f, distanceFromWaypoint);
			distanceToWaypoint = math.max(0f, distanceToWaypoint);
			return true;
		}

		private void AddDistance(ref float distance, ref float3 position, Entity lane, float2 curveDelta)
		{
			if (m_SlaveLanes.TryGetComponent(lane, out var componentData) && m_Owners.TryGetComponent(lane, out var componentData2) && m_SubLaneBuffers.TryGetBuffer(componentData2.m_Owner, out var bufferData) && componentData.m_MasterIndex < bufferData.Length)
			{
				lane = bufferData[componentData.m_MasterIndex].m_SubLane;
			}
			if (m_Curves.TryGetComponent(lane, out var componentData3))
			{
				distance += math.distance(position, MathUtils.Position(componentData3.m_Bezier, curveDelta.x));
				if ((curveDelta.x == 0f && curveDelta.y == 1f) || (curveDelta.x == 1f && curveDelta.y == 0f))
				{
					distance += componentData3.m_Length;
				}
				else
				{
					distance += MathUtils.Length(componentData3.m_Bezier, new Bounds1(curveDelta));
				}
				position = MathUtils.Position(componentData3.m_Bezier, curveDelta.y);
			}
		}

		private (int, int) GetCargo(Entity entity)
		{
			int num = 0;
			int num2 = 0;
			if (m_PrefabRefs.TryGetComponent(entity, out var componentData))
			{
				if (m_LayoutElementBuffers.TryGetBuffer(entity, out var bufferData))
				{
					for (int i = 0; i < bufferData.Length; i++)
					{
						Entity vehicle = bufferData[i].m_Vehicle;
						DynamicBuffer<Game.Economy.Resources> bufferData3;
						if (m_PassengerBuffers.TryGetBuffer(vehicle, out var bufferData2))
						{
							for (int j = 0; j < bufferData2.Length; j++)
							{
								if (!m_Pets.HasComponent(bufferData2[j].m_Passenger))
								{
									num++;
								}
							}
						}
						else if (m_EconomyResourcesBuffers.TryGetBuffer(vehicle, out bufferData3))
						{
							for (int k = 0; k < bufferData3.Length; k++)
							{
								num += bufferData3[k].m_Amount;
							}
						}
						if (m_PrefabRefs.TryGetComponent(vehicle, out var componentData2))
						{
							Entity prefab = componentData2.m_Prefab;
							CargoTransportVehicleData componentData4;
							if (m_PublicTransportVehicleDatas.TryGetComponent(prefab, out var componentData3))
							{
								num2 += componentData3.m_PassengerCapacity;
							}
							else if (m_CargoTransportVehicleDatas.TryGetComponent(prefab, out componentData4))
							{
								num2 += componentData4.m_CargoCapacity;
							}
						}
					}
				}
				else
				{
					DynamicBuffer<Game.Economy.Resources> bufferData5;
					if (m_PassengerBuffers.TryGetBuffer(entity, out var bufferData4))
					{
						for (int l = 0; l < bufferData4.Length; l++)
						{
							if (!m_Pets.HasComponent(bufferData4[l].m_Passenger))
							{
								num++;
							}
						}
					}
					else if (m_EconomyResourcesBuffers.TryGetBuffer(entity, out bufferData5))
					{
						for (int m = 0; m < bufferData5.Length; m++)
						{
							num += bufferData5[m].m_Amount;
						}
					}
					CargoTransportVehicleData componentData6;
					if (m_PublicTransportVehicleDatas.TryGetComponent(componentData.m_Prefab, out var componentData5))
					{
						num2 = componentData5.m_PassengerCapacity;
					}
					else if (m_CargoTransportVehicleDatas.TryGetComponent(componentData.m_Prefab, out componentData6))
					{
						num2 += componentData6.m_CargoCapacity;
					}
				}
			}
			return (num, num2);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Route> __Game_Routes_Route_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportLine> __Game_Routes_TransportLine_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkRoute> __Game_Routes_WorkRoute_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Routes.TransportStop> __Game_Routes_TransportStop_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Routes.WorkStop> __Game_Routes_WorkStop_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TaxiStand> __Game_Routes_TaxiStand_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Vehicle> __Game_Vehicles_Vehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PublicTransport> __Game_Vehicles_PublicTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.CargoTransport> __Game_Vehicles_CargoTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.WorkVehicle> __Game_Vehicles_WorkVehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentRoute> __Game_Routes_CurrentRoute_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<RouteSegment> __Game_Routes_RouteSegment_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<RouteVehicle> __Game_Routes_RouteVehicle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedRoute> __Game_Routes_ConnectedRoute_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubRoute> __Game_Routes_SubRoute_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Routes.Color> __Game_Routes_Color_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Connected> __Game_Routes_Connected_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaitingPassengers> __Game_Routes_WaitingPassengers_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Position> __Game_Routes_Position_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteLane> __Game_Routes_RouteLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Target> __Game_Common_Target_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathOwner> __Game_Pathfind_PathOwner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Waypoint> __Game_Routes_Waypoint_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Train> __Game_Vehicles_Train_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrainCurrentLane> __Game_Vehicles_TrainCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WatercraftCurrentLane> __Game_Vehicles_WatercraftCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AircraftCurrentLane> __Game_Vehicles_AircraftCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Creatures.Pet> __Game_Creatures_Pet_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportLineData> __Game_Prefabs_TransportLineData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WorkRouteData> __Game_Prefabs_WorkRouteData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrainData> __Game_Prefabs_TrainData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PublicTransportVehicleData> __Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CargoTransportVehicleData> __Game_Prefabs_CargoTransportVehicleData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CarNavigationLane> __Game_Vehicles_CarNavigationLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<TrainNavigationLane> __Game_Vehicles_TrainNavigationLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<WatercraftNavigationLane> __Game_Vehicles_WatercraftNavigationLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<AircraftNavigationLane> __Game_Vehicles_AircraftNavigationLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Passenger> __Game_Vehicles_Passenger_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<TransformFrame> __Game_Objects_TransformFrame_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<BuildingUpgradeElement> __Game_Prefabs_BuildingUpgradeElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> __Game_Companies_StorageLimitData_RO_ComponentLookup;

		public ComponentLookup<RouteData> __Game_Prefabs_RouteData_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attachment> __Game_Objects_Attachment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Area> __Game_Areas_Area_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaLane> __Game_Net_AreaLane_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Routes_Route_RO_ComponentLookup = state.GetComponentLookup<Route>(isReadOnly: true);
			__Game_Routes_TransportLine_RO_ComponentLookup = state.GetComponentLookup<TransportLine>(isReadOnly: true);
			__Game_Routes_WorkRoute_RO_ComponentLookup = state.GetComponentLookup<WorkRoute>(isReadOnly: true);
			__Game_Routes_TransportStop_RO_ComponentLookup = state.GetComponentLookup<Game.Routes.TransportStop>(isReadOnly: true);
			__Game_Routes_WorkStop_RO_ComponentLookup = state.GetComponentLookup<Game.Routes.WorkStop>(isReadOnly: true);
			__Game_Routes_TaxiStand_RO_ComponentLookup = state.GetComponentLookup<TaxiStand>(isReadOnly: true);
			__Game_Vehicles_Vehicle_RO_ComponentLookup = state.GetComponentLookup<Vehicle>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Vehicles_PublicTransport_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PublicTransport>(isReadOnly: true);
			__Game_Vehicles_CargoTransport_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.CargoTransport>(isReadOnly: true);
			__Game_Vehicles_WorkVehicle_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.WorkVehicle>(isReadOnly: true);
			__Game_Routes_CurrentRoute_RO_ComponentLookup = state.GetComponentLookup<CurrentRoute>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Routes_RouteWaypoint_RO_BufferLookup = state.GetBufferLookup<RouteWaypoint>(isReadOnly: true);
			__Game_Routes_RouteSegment_RO_BufferLookup = state.GetBufferLookup<RouteSegment>(isReadOnly: true);
			__Game_Routes_RouteVehicle_RO_BufferLookup = state.GetBufferLookup<RouteVehicle>(isReadOnly: true);
			__Game_Routes_ConnectedRoute_RO_BufferLookup = state.GetBufferLookup<ConnectedRoute>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
			__Game_Routes_SubRoute_RO_BufferLookup = state.GetBufferLookup<SubRoute>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Routes_Color_RO_ComponentLookup = state.GetComponentLookup<Game.Routes.Color>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Routes_Connected_RO_ComponentLookup = state.GetComponentLookup<Connected>(isReadOnly: true);
			__Game_Routes_WaitingPassengers_RO_ComponentLookup = state.GetComponentLookup<WaitingPassengers>(isReadOnly: true);
			__Game_Routes_Position_RO_ComponentLookup = state.GetComponentLookup<Position>(isReadOnly: true);
			__Game_Routes_RouteLane_RO_ComponentLookup = state.GetComponentLookup<RouteLane>(isReadOnly: true);
			__Game_Common_Target_RO_ComponentLookup = state.GetComponentLookup<Target>(isReadOnly: true);
			__Game_Pathfind_PathOwner_RO_ComponentLookup = state.GetComponentLookup<PathOwner>(isReadOnly: true);
			__Game_Routes_Waypoint_RO_ComponentLookup = state.GetComponentLookup<Waypoint>(isReadOnly: true);
			__Game_Vehicles_Train_RO_ComponentLookup = state.GetComponentLookup<Train>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
			__Game_Vehicles_CarCurrentLane_RO_ComponentLookup = state.GetComponentLookup<CarCurrentLane>(isReadOnly: true);
			__Game_Vehicles_TrainCurrentLane_RO_ComponentLookup = state.GetComponentLookup<TrainCurrentLane>(isReadOnly: true);
			__Game_Vehicles_WatercraftCurrentLane_RO_ComponentLookup = state.GetComponentLookup<WatercraftCurrentLane>(isReadOnly: true);
			__Game_Vehicles_AircraftCurrentLane_RO_ComponentLookup = state.GetComponentLookup<AircraftCurrentLane>(isReadOnly: true);
			__Game_Creatures_Pet_RO_ComponentLookup = state.GetComponentLookup<Game.Creatures.Pet>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_TransportLineData_RO_ComponentLookup = state.GetComponentLookup<TransportLineData>(isReadOnly: true);
			__Game_Prefabs_WorkRouteData_RO_ComponentLookup = state.GetComponentLookup<WorkRouteData>(isReadOnly: true);
			__Game_Prefabs_TrainData_RO_ComponentLookup = state.GetComponentLookup<TrainData>(isReadOnly: true);
			__Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup = state.GetComponentLookup<PublicTransportVehicleData>(isReadOnly: true);
			__Game_Prefabs_CargoTransportVehicleData_RO_ComponentLookup = state.GetComponentLookup<CargoTransportVehicleData>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
			__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Vehicles_CarNavigationLane_RO_BufferLookup = state.GetBufferLookup<CarNavigationLane>(isReadOnly: true);
			__Game_Vehicles_TrainNavigationLane_RO_BufferLookup = state.GetBufferLookup<TrainNavigationLane>(isReadOnly: true);
			__Game_Vehicles_WatercraftNavigationLane_RO_BufferLookup = state.GetBufferLookup<WatercraftNavigationLane>(isReadOnly: true);
			__Game_Vehicles_AircraftNavigationLane_RO_BufferLookup = state.GetBufferLookup<AircraftNavigationLane>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferLookup = state.GetBufferLookup<PathElement>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Vehicles_Passenger_RO_BufferLookup = state.GetBufferLookup<Passenger>(isReadOnly: true);
			__Game_Objects_TransformFrame_RO_BufferLookup = state.GetBufferLookup<TransformFrame>(isReadOnly: true);
			__Game_Prefabs_BuildingUpgradeElement_RO_BufferLookup = state.GetBufferLookup<BuildingUpgradeElement>(isReadOnly: true);
			__Game_Companies_StorageLimitData_RO_ComponentLookup = state.GetComponentLookup<StorageLimitData>(isReadOnly: true);
			__Game_Prefabs_RouteData_RW_ComponentLookup = state.GetComponentLookup<RouteData>();
			__Game_Objects_Attachment_RO_ComponentLookup = state.GetComponentLookup<Attachment>(isReadOnly: true);
			__Game_Areas_Area_RO_ComponentLookup = state.GetComponentLookup<Area>(isReadOnly: true);
			__Game_Net_AreaLane_RO_ComponentLookup = state.GetComponentLookup<AreaLane>(isReadOnly: true);
		}
	}

	private RenderingSystem m_RenderingSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private NativeArray<bool> m_BoolResult;

	private NativeArray<Entity> m_EntityResult;

	private NativeList<LineSegment> m_SegmentsResult;

	private NativeList<LineStop> m_StopsResult;

	private NativeList<LineVehicle> m_VehiclesResult;

	private NativeArray<Color32> m_ColorResult;

	private NativeArray<int> m_StopCapacityResult;

	private TypeHandle __TypeHandle;

	protected override string group => "LineVisualizerSection";

	protected override bool displayForDestroyedObjects => true;

	protected override bool displayForOutsideConnections => true;

	protected override bool displayForUpgrades => true;

	private UnityEngine.Color color { get; set; }

	private int stopCapacity { get; set; }

	private NativeList<LineStop> stops { get; set; }

	private NativeList<LineVehicle> vehicles { get; set; }

	private NativeList<LineSegment> segments { get; set; }

	protected override void Reset()
	{
		color = default(UnityEngine.Color);
		stopCapacity = 0;
		stops.Clear();
		vehicles.Clear();
		segments.Clear();
		m_SegmentsResult.Clear();
		m_StopsResult.Clear();
		m_VehiclesResult.Clear();
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		stops = new NativeList<LineStop>(Allocator.Persistent);
		vehicles = new NativeList<LineVehicle>(Allocator.Persistent);
		segments = new NativeList<LineSegment>(Allocator.Persistent);
		m_BoolResult = new NativeArray<bool>(4, Allocator.Persistent);
		m_EntityResult = new NativeArray<Entity>(1, Allocator.Persistent);
		m_ColorResult = new NativeArray<Color32>(1, Allocator.Persistent);
		m_StopCapacityResult = new NativeArray<int>(1, Allocator.Persistent);
		m_SegmentsResult = new NativeList<LineSegment>(Allocator.Persistent);
		m_StopsResult = new NativeList<LineStop>(Allocator.Persistent);
		m_VehiclesResult = new NativeList<LineVehicle>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		stops.Dispose();
		vehicles.Dispose();
		segments.Dispose();
		m_BoolResult.Dispose();
		m_EntityResult.Dispose();
		m_ColorResult.Dispose();
		m_StopCapacityResult.Dispose();
		m_SegmentsResult.Dispose();
		m_StopsResult.Dispose();
		m_VehiclesResult.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		IJobExtensions.Schedule(new VisibilityJob
		{
			m_SelectedEntity = selectedEntity,
			m_SelectedRouteEntity = m_InfoUISystem.selectedRoute,
			m_Routes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Route_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransportLines = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_TransportLine_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WorkRoutes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_WorkRoute_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransportStops = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_TransportStop_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WorkStops = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_WorkStop_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TaxiStands = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_TaxiStand_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Vehicles = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Vehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Owners = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PublicTransports = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PublicTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CargoTransports = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CargoTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WorkVehicles = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_WorkVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentRoutes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_CurrentRoute_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Attached = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RouteWaypointBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferLookup, ref base.CheckedStateRef),
			m_RouteSegmentBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteSegment_RO_BufferLookup, ref base.CheckedStateRef),
			m_RouteVehicleBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteVehicle_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedRouteBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_ConnectedRoute_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubObjectBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubNetBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubRouteBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_SubRoute_RO_BufferLookup, ref base.CheckedStateRef),
			m_InstalledUpgradeBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
			m_BoolResult = m_BoolResult,
			m_EntityResult = m_EntityResult
		}, base.Dependency).Complete();
		base.visible = m_BoolResult[0];
		if (base.visible)
		{
			if (m_BoolResult[1])
			{
				m_InfoUISystem.selectedRoute = m_EntityResult[0];
			}
			IJobExtensions.Schedule(new UpdateJob
			{
				m_RightHandTraffic = !m_CityConfigurationSystem.leftHandTraffic,
				m_RouteEntity = m_InfoUISystem.selectedRoute,
				m_RenderingFrameIndex = m_RenderingSystem.frameIndex,
				m_RenderingFrameTime = m_RenderingSystem.frameTime,
				m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
				m_UpdateFrames = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
				m_Colors = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Color_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PathInformation = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Connected = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Connected_RO_ComponentLookup, ref base.CheckedStateRef),
				m_WaitingPassengers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_WaitingPassengers_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Positions = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Position_RO_ComponentLookup, ref base.CheckedStateRef),
				m_RouteLanes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_RouteLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurrentRoutes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_CurrentRoute_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Targets = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Target_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PathOwners = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathOwner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Owners = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Waypoints = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Waypoint_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Trains = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Train_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Curves = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SlaveLanes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CarCurrentLanes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TrainCurrentLanes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_TrainCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_WatercraftCurrentLanes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_WatercraftCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AircraftCurrentLanes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_AircraftCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Pets = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_Pet_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransportLineData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportLineData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_WorkRouteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WorkRouteData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TrainDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrainData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PublicTransportVehicleDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CargoTransportVehicleDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CargoTransportVehicleData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Transforms = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransportStops = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_TransportStop_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OutsideConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EconomyResourcesBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref base.CheckedStateRef),
				m_RouteWaypointBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferLookup, ref base.CheckedStateRef),
				m_RouteSegmentBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteSegment_RO_BufferLookup, ref base.CheckedStateRef),
				m_RouteVehicleBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteVehicle_RO_BufferLookup, ref base.CheckedStateRef),
				m_LayoutElementBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_CarNavigationLaneBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_CarNavigationLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_TrainNavigationLaneBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_TrainNavigationLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_WatercraftNavigationLaneBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_WatercraftNavigationLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_AircraftNavigationLaneBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_AircraftNavigationLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_PathElementBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubLaneBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_PassengerBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_Passenger_RO_BufferLookup, ref base.CheckedStateRef),
				m_TransformFrames = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_TransformFrame_RO_BufferLookup, ref base.CheckedStateRef),
				m_BuildingUpgradeElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_BuildingUpgradeElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_StorageLimitDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageLimitData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_RouteDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RouteData_RW_ComponentLookup, ref base.CheckedStateRef),
				m_Attachments = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attachment_RO_ComponentLookup, ref base.CheckedStateRef),
				m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubObjectBuffers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
				m_ConnectedRoute = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_ConnectedRoute_RO_BufferLookup, ref base.CheckedStateRef),
				m_Areas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Area_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AreaLanes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_AreaLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SegmentsResult = m_SegmentsResult,
				m_StopsResult = m_StopsResult,
				m_VehiclesResult = m_VehiclesResult,
				m_ColorResult = m_ColorResult,
				m_StopCapacityResult = m_StopCapacityResult,
				m_BoolResult = m_BoolResult
			}, base.Dependency).Complete();
		}
	}

	protected override void OnProcess()
	{
		color = m_ColorResult[0];
		stopCapacity = m_StopCapacityResult[0];
		m_InfoUISystem.tags.Add(m_BoolResult[2] ? SelectedInfoTags.CargoRoute : ((!m_BoolResult[3]) ? SelectedInfoTags.TransportLine : SelectedInfoTags.WorkRoute));
		if (base.EntityManager.HasComponent<Game.Routes.TransportStop>(selectedEntity))
		{
			m_InfoUISystem.tags.Add(SelectedInfoTags.TransportStop);
		}
		for (int i = 0; i < m_SegmentsResult.Length; i++)
		{
			segments.Add(m_SegmentsResult[i]);
		}
		for (int j = 0; j < m_VehiclesResult.Length; j++)
		{
			vehicles.Add(m_VehiclesResult[j]);
		}
		for (int k = 0; k < m_StopsResult.Length; k++)
		{
			stops.Add(m_StopsResult[k]);
		}
		m_Dirty = true;
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("color");
		writer.Write(color);
		writer.PropertyName("stops");
		writer.ArrayBegin(stops.Length);
		for (int i = 0; i < stops.Length; i++)
		{
			stops[i].Bind(writer, m_NameSystem);
		}
		writer.ArrayEnd();
		writer.PropertyName("vehicles");
		writer.ArrayBegin(vehicles.Length);
		for (int j = 0; j < vehicles.Length; j++)
		{
			vehicles[j].Bind(writer, m_NameSystem);
		}
		writer.ArrayEnd();
		writer.PropertyName("segments");
		writer.ArrayBegin(segments.Length);
		for (int k = 0; k < segments.Length; k++)
		{
			writer.Write(segments[k]);
		}
		writer.ArrayEnd();
		writer.PropertyName("stopCapacity");
		writer.Write(stopCapacity);
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
	public LineVisualizerSection()
	{
	}
}
