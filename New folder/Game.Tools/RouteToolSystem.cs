using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Audio;
using Game.Common;
using Game.Input;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class RouteToolSystem : ToolBaseSystem
{
	public enum State
	{
		Default,
		Create,
		Modify,
		Remove
	}

	public enum Tooltip
	{
		None,
		CreateRoute,
		ModifyWaypoint,
		ModifySegment,
		CreateOrModify,
		AddWaypoint,
		InsertWaypoint,
		MoveWaypoint,
		MergeWaypoints,
		CompleteRoute,
		DeleteRoute,
		RemoveWaypoint
	}

	[BurstCompile]
	private struct SnapJob : IJob
	{
		[ReadOnly]
		public Snap m_Snap;

		[ReadOnly]
		public State m_State;

		[ReadOnly]
		public Entity m_Prefab;

		[ReadOnly]
		public Entity m_ApplyTempRoute;

		[ReadOnly]
		public Entity m_ServiceUpgradeOwner;

		[ReadOnly]
		public ControlPoint m_MoveStartPosition;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<RouteData> m_PrefabRouteData;

		[ReadOnly]
		public ComponentLookup<TransportLineData> m_PrefabTransportLineData;

		[ReadOnly]
		public ComponentLookup<RouteConnectionData> m_PrefabRouteConnectionData;

		[ReadOnly]
		public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> m_PrefabTrackLaneData;

		[ReadOnly]
		public ComponentLookup<TransportStopData> m_PrefabTransportStopData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Position> m_PositionData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<SlaveLane> m_SlaveLaneData;

		[ReadOnly]
		public BufferLookup<ConnectedRoute> m_ConnectedRoutes;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> m_Waypoints;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		public NativeList<ControlPoint> m_ControlPoints;

		public void Execute()
		{
			Entity entity = m_Prefab;
			int index = m_ControlPoints.Length - 1;
			ControlPoint currentPoint = m_ControlPoints[index];
			if ((m_State == State.Modify || m_State == State.Remove) && m_Waypoints.HasBuffer(m_MoveStartPosition.m_OriginalEntity))
			{
				entity = m_PrefabRefData[m_MoveStartPosition.m_OriginalEntity].m_Prefab;
			}
			RouteData routeData = m_PrefabRouteData[entity];
			m_PrefabTransportLineData.TryGetComponent(entity, out var componentData);
			m_PrefabRouteConnectionData.TryGetComponent(entity, out var componentData2);
			if (routeData.m_Type == RouteType.WorkRoute)
			{
				componentData.m_TransportType = TransportType.Work;
			}
			switch (m_State)
			{
			case State.Default:
			{
				if (!FindWaypointLocation(routeData, componentData, componentData2, ref currentPoint) || !(m_ApplyTempRoute != Entity.Null) || m_ConnectedRoutes.HasBuffer(currentPoint.m_OriginalEntity))
				{
					break;
				}
				Temp temp = m_TempData[m_ApplyTempRoute];
				if ((temp.m_Flags & TempFlags.Delete) != 0)
				{
					if (temp.m_Original != Entity.Null && currentPoint.m_OriginalEntity == temp.m_Original)
					{
						currentPoint.m_OriginalEntity = Entity.Null;
					}
					break;
				}
				if (temp.m_Original != Entity.Null && currentPoint.m_OriginalEntity == temp.m_Original)
				{
					if (currentPoint.m_ElementIndex.x < 0)
					{
						break;
					}
					DynamicBuffer<RouteWaypoint> dynamicBuffer2 = m_Waypoints[temp.m_Original];
					DynamicBuffer<RouteWaypoint> dynamicBuffer3 = m_Waypoints[m_ApplyTempRoute];
					float3 position3 = m_PositionData[dynamicBuffer2[currentPoint.m_ElementIndex.x].m_Waypoint].m_Position;
					currentPoint.m_ElementIndex.x = -1;
					for (int i = 0; i < dynamicBuffer3.Length; i++)
					{
						if (m_PositionData[dynamicBuffer3[i].m_Waypoint].m_Position.Equals(position3))
						{
							currentPoint.m_ElementIndex.x = i;
							break;
						}
					}
					break;
				}
				DynamicBuffer<RouteWaypoint> dynamicBuffer4 = m_Waypoints[m_ApplyTempRoute];
				for (int j = 0; j < dynamicBuffer4.Length; j++)
				{
					float3 position4 = m_PositionData[dynamicBuffer4[j].m_Waypoint].m_Position;
					if (math.distance(position4, currentPoint.m_Position) < routeData.m_SnapDistance)
					{
						currentPoint.m_Position = position4;
						currentPoint.m_OriginalEntity = ((temp.m_Original != Entity.Null) ? temp.m_Original : m_ApplyTempRoute);
						currentPoint.m_ElementIndex = new int2(j, -1);
						break;
					}
				}
				break;
			}
			case State.Create:
				if (FindWaypointLocation(routeData, componentData, componentData2, ref currentPoint) && m_ControlPoints.Length >= 3)
				{
					ControlPoint controlPoint = m_ControlPoints[0];
					if (math.distance(controlPoint.m_Position, currentPoint.m_Position) < routeData.m_SnapDistance)
					{
						currentPoint.m_Position = controlPoint.m_Position;
					}
				}
				break;
			case State.Modify:
				if (FindWaypointLocation(routeData, componentData, componentData2, ref currentPoint) && m_Waypoints.HasBuffer(m_MoveStartPosition.m_OriginalEntity) && m_MoveStartPosition.m_ElementIndex.x >= 0)
				{
					DynamicBuffer<RouteWaypoint> dynamicBuffer = m_Waypoints[m_MoveStartPosition.m_OriginalEntity];
					int index2 = math.select(m_MoveStartPosition.m_ElementIndex.x - 1, dynamicBuffer.Length - 1, m_MoveStartPosition.m_ElementIndex.x == 0);
					int index3 = math.select(m_MoveStartPosition.m_ElementIndex.x + 1, 0, m_MoveStartPosition.m_ElementIndex.x == dynamicBuffer.Length - 1);
					float3 position = m_PositionData[dynamicBuffer[index2].m_Waypoint].m_Position;
					float3 position2 = m_PositionData[dynamicBuffer[index3].m_Waypoint].m_Position;
					float num = math.distance(currentPoint.m_Position, position);
					float num2 = math.distance(currentPoint.m_Position, position2);
					if (num < routeData.m_SnapDistance && num <= num2)
					{
						currentPoint.m_Position = position;
					}
					else if (num2 < routeData.m_SnapDistance)
					{
						currentPoint.m_Position = position2;
					}
				}
				break;
			case State.Remove:
				if (FindWaypointLocation(routeData, componentData, componentData2, ref currentPoint) && (currentPoint.m_OriginalEntity != m_MoveStartPosition.m_OriginalEntity || math.any(currentPoint.m_ElementIndex != m_MoveStartPosition.m_ElementIndex)))
				{
					currentPoint = m_MoveStartPosition;
					currentPoint.m_OriginalEntity = Entity.Null;
				}
				break;
			}
			currentPoint.m_HitPosition = currentPoint.m_Position;
			currentPoint.m_CurvePosition = 0f;
			m_ControlPoints[index] = currentPoint;
		}

		private bool ValidateStop(TransportLineData transportLineData, Entity stopEntity)
		{
			if (m_PrefabRefData.TryGetComponent(stopEntity, out var componentData))
			{
				if (m_ServiceUpgradeOwner != Entity.Null && !HasOwner(stopEntity, m_ServiceUpgradeOwner))
				{
					return false;
				}
				if (m_PrefabTransportStopData.TryGetComponent(componentData.m_Prefab, out var componentData2))
				{
					return componentData2.m_TransportType == transportLineData.m_TransportType;
				}
			}
			return false;
		}

		private bool HasOwner(Entity entity, Entity owner)
		{
			Owner componentData;
			while (m_OwnerData.TryGetComponent(entity, out componentData))
			{
				if (componentData.m_Owner == owner)
				{
					return true;
				}
				entity = componentData.m_Owner;
			}
			return false;
		}

		private bool FindWaypointLocation(RouteData routeData, TransportLineData transportLineData, RouteConnectionData routeConnectionData, ref ControlPoint currentPoint)
		{
			ControlPoint controlPoint = default(ControlPoint);
			bool flag = false;
			while (currentPoint.m_OriginalEntity != Entity.Null)
			{
				if (m_ConnectedRoutes.HasBuffer(currentPoint.m_OriginalEntity) && ValidateStop(transportLineData, currentPoint.m_OriginalEntity))
				{
					return true;
				}
				if (m_Waypoints.HasBuffer(currentPoint.m_OriginalEntity) && math.any(currentPoint.m_ElementIndex >= 0))
				{
					if (currentPoint.m_ElementIndex.y >= 0)
					{
						DynamicBuffer<RouteWaypoint> dynamicBuffer = m_Waypoints[currentPoint.m_OriginalEntity];
						int y = currentPoint.m_ElementIndex.y;
						int num = math.select(currentPoint.m_ElementIndex.y + 1, 0, currentPoint.m_ElementIndex.y == dynamicBuffer.Length - 1);
						float3 position = m_PositionData[dynamicBuffer[y].m_Waypoint].m_Position;
						float3 position2 = m_PositionData[dynamicBuffer[num].m_Waypoint].m_Position;
						float num2 = math.distance(currentPoint.m_Position, position);
						float num3 = math.distance(currentPoint.m_Position, position2);
						if (num2 < routeData.m_SnapDistance && num2 <= num3)
						{
							currentPoint.m_ElementIndex = new int2(y, -1);
							currentPoint.m_Position = position;
						}
						else if (num3 < routeData.m_SnapDistance)
						{
							currentPoint.m_ElementIndex = new int2(num, -1);
							currentPoint.m_Position = position2;
						}
					}
					if (currentPoint.m_ElementIndex.x >= 0 || m_State == State.Default)
					{
						return true;
					}
				}
				if (m_SubObjects.HasBuffer(currentPoint.m_OriginalEntity))
				{
					DynamicBuffer<Game.Objects.SubObject> dynamicBuffer2 = m_SubObjects[currentPoint.m_OriginalEntity];
					float num4 = routeData.m_SnapDistance;
					for (int i = 0; i < dynamicBuffer2.Length; i++)
					{
						Entity subObject = dynamicBuffer2[i].m_SubObject;
						if (m_ConnectedRoutes.HasBuffer(subObject) && ValidateStop(transportLineData, currentPoint.m_OriginalEntity))
						{
							Game.Objects.Transform transform = m_TransformData[subObject];
							float num5 = math.distance(transform.m_Position, currentPoint.m_HitPosition);
							if (num5 < num4)
							{
								num4 = num5;
								currentPoint.m_Position = transform.m_Position;
								currentPoint.m_OriginalEntity = subObject;
							}
						}
					}
					if (num4 < routeData.m_SnapDistance)
					{
						return true;
					}
				}
				if (!flag && m_State != State.Default && m_SubLanes.HasBuffer(currentPoint.m_OriginalEntity))
				{
					DynamicBuffer<Game.Net.SubLane> dynamicBuffer3 = m_SubLanes[currentPoint.m_OriginalEntity];
					float num6 = routeData.m_SnapDistance;
					for (int j = 0; j < dynamicBuffer3.Length; j++)
					{
						Entity subLane = dynamicBuffer3[j].m_SubLane;
						if (CheckLaneType(routeData, transportLineData, routeConnectionData, subLane))
						{
							Curve curve = m_CurveData[subLane];
							float t;
							float num7 = MathUtils.Distance(curve.m_Bezier, currentPoint.m_HitPosition, out t);
							if (num7 < num6)
							{
								num6 = num7;
								controlPoint = currentPoint;
								controlPoint.m_OriginalEntity = Entity.Null;
								controlPoint.m_Position = MathUtils.Position(curve.m_Bezier, t);
								flag = true;
							}
						}
					}
				}
				if (m_OwnerData.HasComponent(currentPoint.m_OriginalEntity))
				{
					currentPoint.m_OriginalEntity = m_OwnerData[currentPoint.m_OriginalEntity].m_Owner;
					if (m_TransformData.HasComponent(currentPoint.m_OriginalEntity))
					{
						currentPoint.m_Position = m_TransformData[currentPoint.m_OriginalEntity].m_Position;
					}
				}
				else
				{
					currentPoint.m_OriginalEntity = Entity.Null;
				}
			}
			if (flag)
			{
				currentPoint = controlPoint;
				return true;
			}
			if (m_State == State.Default && m_ControlPoints.Length == 1)
			{
				currentPoint = default(ControlPoint);
			}
			else if (m_State == State.Modify && m_ControlPoints.Length >= 1)
			{
				currentPoint = m_MoveStartPosition;
			}
			else if (m_State == State.Remove && m_ControlPoints.Length >= 1)
			{
				currentPoint = m_MoveStartPosition;
				currentPoint.m_OriginalEntity = Entity.Null;
			}
			else if (m_ControlPoints.Length >= 2)
			{
				currentPoint = m_ControlPoints[m_ControlPoints.Length - 2];
			}
			return false;
		}

		private bool CheckLaneType(RouteData routeData, TransportLineData transportLineData, RouteConnectionData routeConnectionData, Entity lane)
		{
			if (m_SlaveLaneData.HasComponent(lane))
			{
				return false;
			}
			PrefabRef prefabRef = m_PrefabRefData[lane];
			if (routeData.m_Type == RouteType.TransportLine)
			{
				switch (transportLineData.m_TransportType)
				{
				case TransportType.Bus:
					if (m_PrefabCarLaneData.HasComponent(prefabRef.m_Prefab))
					{
						CarLaneData carLaneData2 = m_PrefabCarLaneData[prefabRef.m_Prefab];
						if ((carLaneData2.m_RoadTypes & RoadTypes.Car) != RoadTypes.None)
						{
							return (int)carLaneData2.m_MaxSize >= (int)transportLineData.m_SizeClass;
						}
						return false;
					}
					return false;
				case TransportType.Tram:
					if (m_PrefabTrackLaneData.HasComponent(prefabRef.m_Prefab))
					{
						return (m_PrefabTrackLaneData[prefabRef.m_Prefab].m_TrackTypes & TrackTypes.Tram) != 0;
					}
					return false;
				case TransportType.Train:
					if (m_PrefabTrackLaneData.HasComponent(prefabRef.m_Prefab))
					{
						return (m_PrefabTrackLaneData[prefabRef.m_Prefab].m_TrackTypes & TrackTypes.Train) != 0;
					}
					return false;
				case TransportType.Subway:
					if (m_PrefabTrackLaneData.HasComponent(prefabRef.m_Prefab))
					{
						return (m_PrefabTrackLaneData[prefabRef.m_Prefab].m_TrackTypes & TrackTypes.Subway) != 0;
					}
					return false;
				case TransportType.Ship:
					if (m_PrefabCarLaneData.HasComponent(prefabRef.m_Prefab))
					{
						CarLaneData carLaneData3 = m_PrefabCarLaneData[prefabRef.m_Prefab];
						if ((carLaneData3.m_RoadTypes & RoadTypes.Watercraft) != RoadTypes.None)
						{
							return (int)carLaneData3.m_MaxSize >= (int)transportLineData.m_SizeClass;
						}
						return false;
					}
					return false;
				case TransportType.Airplane:
					if (m_PrefabCarLaneData.HasComponent(prefabRef.m_Prefab))
					{
						CarLaneData carLaneData4 = m_PrefabCarLaneData[prefabRef.m_Prefab];
						if ((carLaneData4.m_RoadTypes & RoadTypes.Airplane) != RoadTypes.None)
						{
							return (int)carLaneData4.m_MaxSize >= (int)transportLineData.m_SizeClass;
						}
						return false;
					}
					return false;
				case TransportType.Ferry:
					if (m_PrefabCarLaneData.HasComponent(prefabRef.m_Prefab))
					{
						CarLaneData carLaneData = m_PrefabCarLaneData[prefabRef.m_Prefab];
						if ((carLaneData.m_RoadTypes & RoadTypes.Watercraft) != RoadTypes.None)
						{
							return (int)carLaneData.m_MaxSize >= (int)transportLineData.m_SizeClass;
						}
						return false;
					}
					return false;
				}
			}
			else if (routeData.m_Type == RouteType.WorkRoute && m_PrefabCarLaneData.HasComponent(prefabRef.m_Prefab))
			{
				CarLaneData carLaneData5 = m_PrefabCarLaneData[prefabRef.m_Prefab];
				if ((carLaneData5.m_RoadTypes & routeConnectionData.m_RouteRoadType) != RoadTypes.None)
				{
					return (int)carLaneData5.m_MaxSize >= (int)routeConnectionData.m_RouteSizeClass;
				}
				return false;
			}
			return false;
		}
	}

	[BurstCompile]
	private struct CreateDefinitionsJob : IJob
	{
		[ReadOnly]
		public State m_State;

		[ReadOnly]
		public Entity m_Prefab;

		[ReadOnly]
		public Entity m_ApplyTempRoute;

		[ReadOnly]
		public Entity m_ServiceUpgradeOwner;

		[ReadOnly]
		public ControlPoint m_MoveStartPosition;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<RouteData> m_PrefabRouteData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Position> m_PositionData;

		[ReadOnly]
		public ComponentLookup<Connected> m_ConnectedData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<Attached> m_AttachedData;

		[ReadOnly]
		public BufferLookup<ConnectedRoute> m_ConnectedRoutes;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> m_Waypoints;

		[ReadOnly]
		public NativeList<ControlPoint> m_ControlPoints;

		public Color32 m_Color;

		public NativeValue<Tooltip> m_Tooltip;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			m_Tooltip.value = Tooltip.None;
			int length = m_ControlPoints.Length;
			ControlPoint controlPoint = m_ControlPoints[0];
			if (length == 1 && controlPoint.Equals(default(ControlPoint)))
			{
				return;
			}
			if (m_State != State.Default)
			{
				controlPoint = m_MoveStartPosition;
			}
			Entity entity = m_ControlPoints[length - 1].m_OriginalEntity;
			if (m_Waypoints.HasBuffer(entity) && m_ControlPoints[length - 1].m_ElementIndex.x >= 0)
			{
				DynamicBuffer<RouteWaypoint> dynamicBuffer = ((!(m_ApplyTempRoute != Entity.Null)) ? m_Waypoints[entity] : ((!(entity == m_TempData[m_ApplyTempRoute].m_Original)) ? m_Waypoints[entity] : m_Waypoints[m_ApplyTempRoute]));
				if (m_ControlPoints[length - 1].m_ElementIndex.x < dynamicBuffer.Length)
				{
					entity = dynamicBuffer[m_ControlPoints[length - 1].m_ElementIndex.x].m_Waypoint;
					if (m_ConnectedData.TryGetComponent(entity, out var componentData))
					{
						entity = componentData.m_Connected;
					}
				}
			}
			if (m_TransformData.HasComponent(entity))
			{
				CreateTempWaypointObject(entity);
			}
			Entity e = m_CommandBuffer.CreateEntity();
			CreationDefinition component = new CreationDefinition
			{
				m_Prefab = m_Prefab,
				m_Owner = m_ServiceUpgradeOwner
			};
			ColorDefinition component2 = new ColorDefinition
			{
				m_Color = m_Color
			};
			if (m_Waypoints.HasBuffer(controlPoint.m_OriginalEntity))
			{
				component.m_Prefab = m_PrefabRefData[controlPoint.m_OriginalEntity].m_Prefab;
			}
			float minWaypointDistance = RouteUtils.GetMinWaypointDistance(m_PrefabRouteData[component.m_Prefab]);
			if (m_Waypoints.HasBuffer(controlPoint.m_OriginalEntity) && math.any(controlPoint.m_ElementIndex >= 0))
			{
				component.m_Original = controlPoint.m_OriginalEntity;
				DynamicBuffer<RouteWaypoint> dynamicBuffer2;
				if (m_ApplyTempRoute != Entity.Null)
				{
					Temp temp = m_TempData[m_ApplyTempRoute];
					dynamicBuffer2 = ((!(controlPoint.m_OriginalEntity == temp.m_Original)) ? m_Waypoints[controlPoint.m_OriginalEntity] : m_Waypoints[m_ApplyTempRoute]);
				}
				else
				{
					dynamicBuffer2 = m_Waypoints[controlPoint.m_OriginalEntity];
				}
				if (controlPoint.m_ElementIndex.y >= 0)
				{
					int y = controlPoint.m_ElementIndex.y;
					int index = math.select(controlPoint.m_ElementIndex.y + 1, 0, controlPoint.m_ElementIndex.y == dynamicBuffer2.Length - 1);
					float3 position = m_ControlPoints[length - 1].m_Position;
					bool flag = math.any(new float2(math.distance(position, m_PositionData[dynamicBuffer2[y].m_Waypoint].m_Position), math.distance(position, m_PositionData[dynamicBuffer2[index].m_Waypoint].m_Position)) < minWaypointDistance);
					bool flag2 = !m_MoveStartPosition.Equals(m_ControlPoints[length - 1]);
					bool test = m_State == State.Default || flag || !flag2;
					int num = math.select(length, length - 1, test);
					int length2 = dynamicBuffer2.Length + num;
					DynamicBuffer<WaypointDefinition> dynamicBuffer3 = m_CommandBuffer.AddBuffer<WaypointDefinition>(e);
					dynamicBuffer3.ResizeUninitialized(length2);
					int num2 = 0;
					for (int i = 0; i <= controlPoint.m_ElementIndex.y; i++)
					{
						dynamicBuffer3[num2++] = GetWaypointDefinition(dynamicBuffer2[i].m_Waypoint);
					}
					for (int j = 0; j < num; j++)
					{
						dynamicBuffer3[num2++] = GetWaypointDefinition(m_ControlPoints[j]);
					}
					for (int k = controlPoint.m_ElementIndex.y + 1; k < dynamicBuffer2.Length; k++)
					{
						dynamicBuffer3[num2++] = GetWaypointDefinition(dynamicBuffer2[k].m_Waypoint);
					}
					switch (m_State)
					{
					case State.Default:
						m_Tooltip.value = Tooltip.ModifySegment;
						break;
					case State.Create:
					case State.Remove:
						m_Tooltip.value = Tooltip.None;
						break;
					case State.Modify:
						if (flag || !flag2)
						{
							m_Tooltip.value = Tooltip.None;
						}
						else
						{
							m_Tooltip.value = Tooltip.InsertWaypoint;
						}
						break;
					}
				}
				else
				{
					float3 position2 = m_ControlPoints[length - 1].m_Position;
					bool flag3;
					bool flag4;
					if (m_State == State.Remove)
					{
						flag3 = !(m_ControlPoints[length - 1].m_OriginalEntity != m_MoveStartPosition.m_OriginalEntity) && math.distance(position2, m_PositionData[dynamicBuffer2[controlPoint.m_ElementIndex.x].m_Waypoint].m_Position) < minWaypointDistance;
						flag4 = false;
					}
					else
					{
						int index2 = math.select(controlPoint.m_ElementIndex.x - 1, dynamicBuffer2.Length - 1, controlPoint.m_ElementIndex.x == 0);
						int index3 = math.select(controlPoint.m_ElementIndex.x + 1, 0, controlPoint.m_ElementIndex.x == dynamicBuffer2.Length - 1);
						flag3 = math.any(new float2(math.distance(position2, m_PositionData[dynamicBuffer2[index2].m_Waypoint].m_Position), math.distance(position2, m_PositionData[dynamicBuffer2[index3].m_Waypoint].m_Position)) < minWaypointDistance);
						flag4 = !m_MoveStartPosition.Equals(m_ControlPoints[length - 1]);
					}
					bool test2 = flag3;
					int num3 = math.select(length, length - 1, test2);
					int num4 = dynamicBuffer2.Length + num3 - 1;
					DynamicBuffer<WaypointDefinition> dynamicBuffer4 = m_CommandBuffer.AddBuffer<WaypointDefinition>(e);
					dynamicBuffer4.ResizeUninitialized(num4);
					int num5 = 0;
					for (int l = 0; l < controlPoint.m_ElementIndex.x; l++)
					{
						dynamicBuffer4[num5++] = GetWaypointDefinition(dynamicBuffer2[l].m_Waypoint);
					}
					for (int m = 0; m < num3; m++)
					{
						ControlPoint controlPoint2 = m_ControlPoints[m];
						if (m_State == State.Remove && m == length - 1)
						{
							controlPoint2.m_OriginalEntity = m_MoveStartPosition.m_OriginalEntity;
						}
						WaypointDefinition waypointDefinition = GetWaypointDefinition(controlPoint2);
						waypointDefinition.m_Original = dynamicBuffer2[controlPoint.m_ElementIndex.x].m_Waypoint;
						dynamicBuffer4[num5++] = waypointDefinition;
					}
					for (int n = controlPoint.m_ElementIndex.x + 1; n < dynamicBuffer2.Length; n++)
					{
						dynamicBuffer4[num5++] = GetWaypointDefinition(dynamicBuffer2[n].m_Waypoint);
					}
					if (num4 <= 1)
					{
						component.m_Flags |= CreationFlags.Delete;
					}
					switch (m_State)
					{
					case State.Default:
					{
						Entity waypoint = dynamicBuffer2[controlPoint.m_ElementIndex.x].m_Waypoint;
						if (m_ConnectedData.HasComponent(waypoint) && m_ConnectedData[waypoint].m_Connected != Entity.Null)
						{
							m_Tooltip.value = Tooltip.CreateOrModify;
						}
						else
						{
							m_Tooltip.value = Tooltip.ModifyWaypoint;
						}
						break;
					}
					case State.Create:
						m_Tooltip.value = Tooltip.None;
						break;
					case State.Modify:
						if (num4 <= 1)
						{
							m_Tooltip.value = Tooltip.DeleteRoute;
						}
						else if (flag3)
						{
							m_Tooltip.value = Tooltip.MergeWaypoints;
						}
						else if (flag4)
						{
							m_Tooltip.value = Tooltip.MoveWaypoint;
						}
						else
						{
							m_Tooltip.value = Tooltip.None;
						}
						break;
					case State.Remove:
						if (num4 <= 1)
						{
							m_Tooltip.value = Tooltip.DeleteRoute;
						}
						else if (flag3)
						{
							m_Tooltip.value = Tooltip.RemoveWaypoint;
						}
						else
						{
							m_Tooltip.value = Tooltip.None;
						}
						break;
					}
				}
			}
			else
			{
				bool flag5 = false;
				if (length >= 2)
				{
					flag5 = math.distance(m_ControlPoints[length - 2].m_Position, m_ControlPoints[length - 1].m_Position) < minWaypointDistance;
				}
				int num6 = math.select(length, length - 1, flag5);
				DynamicBuffer<WaypointDefinition> dynamicBuffer5 = m_CommandBuffer.AddBuffer<WaypointDefinition>(e);
				dynamicBuffer5.ResizeUninitialized(num6);
				for (int num7 = 0; num7 < num6; num7++)
				{
					dynamicBuffer5[num7] = GetWaypointDefinition(m_ControlPoints[num7]);
				}
				switch (m_State)
				{
				case State.Default:
					if (length == 1)
					{
						m_Tooltip.value = Tooltip.CreateRoute;
					}
					else
					{
						m_Tooltip.value = Tooltip.None;
					}
					break;
				case State.Create:
					if (flag5)
					{
						m_Tooltip.value = Tooltip.None;
					}
					else if (length >= 3 && m_ControlPoints[0].m_Position.Equals(m_ControlPoints[length - 1].m_Position))
					{
						m_Tooltip.value = Tooltip.CompleteRoute;
					}
					else
					{
						m_Tooltip.value = Tooltip.AddWaypoint;
					}
					break;
				case State.Modify:
				case State.Remove:
					m_Tooltip.value = Tooltip.None;
					break;
				}
			}
			m_CommandBuffer.AddComponent(e, component);
			m_CommandBuffer.AddComponent(e, component2);
			m_CommandBuffer.AddComponent(e, default(Updated));
		}

		private void CreateTempWaypointObject(Entity entity)
		{
			Game.Objects.Transform transform = m_TransformData[entity];
			CreationDefinition component = new CreationDefinition
			{
				m_Original = entity
			};
			component.m_Flags |= CreationFlags.Select;
			ObjectDefinition component2 = new ObjectDefinition
			{
				m_Position = transform.m_Position,
				m_Rotation = transform.m_Rotation
			};
			if (m_ElevationData.TryGetComponent(entity, out var componentData))
			{
				component2.m_Elevation = componentData.m_Elevation;
				component2.m_ParentMesh = ObjectUtils.GetSubParentMesh(componentData.m_Flags);
			}
			else
			{
				component2.m_ParentMesh = -1;
			}
			if (m_AttachedData.HasComponent(entity))
			{
				component.m_Attached = m_AttachedData[entity].m_Parent;
				component.m_Flags |= CreationFlags.Attach;
			}
			component2.m_Probability = 100;
			component2.m_PrefabSubIndex = -1;
			component2.m_LocalPosition = transform.m_Position;
			component2.m_LocalRotation = transform.m_Rotation;
			Entity e = m_CommandBuffer.CreateEntity();
			m_CommandBuffer.AddComponent(e, component);
			m_CommandBuffer.AddComponent(e, component2);
			m_CommandBuffer.AddComponent(e, default(Updated));
		}

		private WaypointDefinition GetWaypointDefinition(Entity original)
		{
			WaypointDefinition result = new WaypointDefinition(m_PositionData[original].m_Position)
			{
				m_Original = original
			};
			if (m_ConnectedData.HasComponent(original))
			{
				result.m_Connection = m_ConnectedData[original].m_Connected;
			}
			return result;
		}

		private WaypointDefinition GetWaypointDefinition(ControlPoint controlPoint)
		{
			WaypointDefinition result = new WaypointDefinition(controlPoint.m_Position);
			if (m_ConnectedData.HasComponent(controlPoint.m_OriginalEntity))
			{
				result.m_Connection = m_ConnectedData[controlPoint.m_OriginalEntity].m_Connected;
			}
			else if (m_ConnectedRoutes.HasBuffer(controlPoint.m_OriginalEntity))
			{
				result.m_Connection = controlPoint.m_OriginalEntity;
			}
			else if (m_Waypoints.HasBuffer(controlPoint.m_OriginalEntity) && controlPoint.m_ElementIndex.x >= 0)
			{
				DynamicBuffer<RouteWaypoint> dynamicBuffer;
				if (m_ApplyTempRoute != Entity.Null)
				{
					Temp temp = m_TempData[m_ApplyTempRoute];
					dynamicBuffer = ((!(controlPoint.m_OriginalEntity == temp.m_Original)) ? m_Waypoints[controlPoint.m_OriginalEntity] : m_Waypoints[m_ApplyTempRoute]);
				}
				else
				{
					dynamicBuffer = m_Waypoints[controlPoint.m_OriginalEntity];
				}
				if (controlPoint.m_ElementIndex.x < dynamicBuffer.Length)
				{
					Entity waypoint = dynamicBuffer[controlPoint.m_ElementIndex.x].m_Waypoint;
					if (m_ConnectedData.HasComponent(waypoint))
					{
						result.m_Connection = m_ConnectedData[waypoint].m_Connected;
					}
				}
			}
			return result;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Route> __Game_Routes_Route_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathUpdated> __Game_Pathfind_PathUpdated_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteData> __Game_Prefabs_RouteData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportLineData> __Game_Prefabs_TransportLineData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteConnectionData> __Game_Prefabs_RouteConnectionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarLaneData> __Game_Prefabs_CarLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrackLaneData> __Game_Prefabs_TrackLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportStopData> __Game_Prefabs_TransportStopData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Position> __Game_Routes_Position_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedRoute> __Game_Routes_ConnectedRoute_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Connected> __Game_Routes_Connected_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> __Game_Objects_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Routes_Route_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Route>(isReadOnly: true);
			__Game_Pathfind_PathUpdated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathUpdated>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_RouteData_RO_ComponentLookup = state.GetComponentLookup<RouteData>(isReadOnly: true);
			__Game_Prefabs_TransportLineData_RO_ComponentLookup = state.GetComponentLookup<TransportLineData>(isReadOnly: true);
			__Game_Prefabs_RouteConnectionData_RO_ComponentLookup = state.GetComponentLookup<RouteConnectionData>(isReadOnly: true);
			__Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
			__Game_Prefabs_TrackLaneData_RO_ComponentLookup = state.GetComponentLookup<TrackLaneData>(isReadOnly: true);
			__Game_Prefabs_TransportStopData_RO_ComponentLookup = state.GetComponentLookup<TransportStopData>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Routes_Position_RO_ComponentLookup = state.GetComponentLookup<Position>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
			__Game_Routes_ConnectedRoute_RO_BufferLookup = state.GetBufferLookup<ConnectedRoute>(isReadOnly: true);
			__Game_Routes_RouteWaypoint_RO_BufferLookup = state.GetBufferLookup<RouteWaypoint>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Routes_Connected_RO_ComponentLookup = state.GetComponentLookup<Connected>(isReadOnly: true);
			__Game_Objects_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Elevation>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
		}
	}

	public const string kToolID = "Route Tool";

	private AudioManager m_AudioManager;

	private EntityQuery m_DefinitionQuery;

	private EntityQuery m_TempRouteQuery;

	private EntityQuery m_EventQuery;

	private EntityQuery m_SoundQuery;

	private IProxyAction m_AddWaypoint;

	private IProxyAction m_InsertWaypoint;

	private IProxyAction m_MoveWaypoint;

	private IProxyAction m_MergeWaypoint;

	private IProxyAction m_RemoveWaypoint;

	private IProxyAction m_UndoWaypoint;

	private IProxyAction m_CreateRoute;

	private IProxyAction m_CompleteRoute;

	private IProxyAction m_DeleteRoute;

	private IProxyAction m_DiscardInsertWaypoint;

	private IProxyAction m_DiscardMoveWaypoint;

	private IProxyAction m_DiscardMergeWaypoint;

	private bool m_ApplyBlocked;

	private ControlPoint m_LastRaycastPoint;

	private NativeList<ControlPoint> m_ControlPoints;

	private NativeValue<Tooltip> m_Tooltip;

	private State m_State;

	private bool m_ControlPointsMoved;

	private bool m_ForceApply;

	private bool m_ForceCancel;

	private bool m_CanApplyModify;

	private ControlPoint m_MoveStartPosition;

	private ToolOutputBarrier m_ToolOutputBarrier;

	private RoutePrefab m_SelectedPrefab;

	private TypeHandle __TypeHandle;

	public override string toolID => "Route Tool";

	public RoutePrefab prefab
	{
		get
		{
			return m_SelectedPrefab;
		}
		set
		{
			if (value != m_SelectedPrefab)
			{
				m_SelectedPrefab = value;
				m_ForceUpdate = true;
				color = m_SelectedPrefab.m_Color;
				serviceUpgrade = m_PrefabSystem.HasComponent<ServiceUpgradeData>(m_SelectedPrefab);
				m_ToolSystem.EventPrefabChanged?.Invoke(value);
			}
		}
	}

	public State state => m_State;

	public ControlPoint moveStartPosition => m_MoveStartPosition;

	public Tooltip tooltip => m_Tooltip.value;

	public bool underground { get; set; }

	public bool serviceUpgrade { get; private set; }

	private protected override IEnumerable<IProxyAction> toolActions
	{
		get
		{
			yield return m_AddWaypoint;
			yield return m_InsertWaypoint;
			yield return m_MoveWaypoint;
			yield return m_MergeWaypoint;
			yield return m_RemoveWaypoint;
			yield return m_UndoWaypoint;
			yield return m_CreateRoute;
			yield return m_CompleteRoute;
			yield return m_DeleteRoute;
			yield return m_DiscardInsertWaypoint;
			yield return m_DiscardMoveWaypoint;
			yield return m_DiscardMergeWaypoint;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
		m_AudioManager = base.World.GetOrCreateSystemManaged<AudioManager>();
		m_DefinitionQuery = GetDefinitionQuery();
		m_TempRouteQuery = GetEntityQuery(ComponentType.ReadOnly<Route>(), ComponentType.ReadOnly<Temp>());
		m_EventQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Common.Event>(), ComponentType.ReadOnly<PathUpdated>());
		m_SoundQuery = GetEntityQuery(ComponentType.ReadOnly<ToolUXSoundSettingsData>());
		m_AddWaypoint = InputManager.instance.toolActionCollection.GetActionState("Add Waypoint", "RouteToolSystem");
		m_InsertWaypoint = InputManager.instance.toolActionCollection.GetActionState("Insert Waypoint", "RouteToolSystem");
		m_MoveWaypoint = InputManager.instance.toolActionCollection.GetActionState("Move Waypoint", "RouteToolSystem");
		m_MergeWaypoint = InputManager.instance.toolActionCollection.GetActionState("Merge Waypoint", "RouteToolSystem");
		m_RemoveWaypoint = InputManager.instance.toolActionCollection.GetActionState("Remove Waypoint", "RouteToolSystem");
		m_UndoWaypoint = InputManager.instance.toolActionCollection.GetActionState("Undo Waypoint", "RouteToolSystem");
		m_CreateRoute = InputManager.instance.toolActionCollection.GetActionState("Create Route", "RouteToolSystem");
		m_CompleteRoute = InputManager.instance.toolActionCollection.GetActionState("Complete Route", "RouteToolSystem");
		m_DeleteRoute = InputManager.instance.toolActionCollection.GetActionState("Delete Route", "RouteToolSystem");
		m_DiscardInsertWaypoint = InputManager.instance.toolActionCollection.GetActionState("Discard Insert Waypoint", "RouteToolSystem");
		m_DiscardMoveWaypoint = InputManager.instance.toolActionCollection.GetActionState("Discard Move Waypoint", "RouteToolSystem");
		m_DiscardMergeWaypoint = InputManager.instance.toolActionCollection.GetActionState("Discard Merge Waypoint", "RouteToolSystem");
		m_ControlPoints = new NativeList<ControlPoint>(20, Allocator.Persistent);
		m_Tooltip = new NativeValue<Tooltip>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_ControlPoints.Dispose();
		m_Tooltip.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnStartRunning()
	{
		base.OnStartRunning();
		m_ControlPoints.Clear();
		m_LastRaycastPoint = default(ControlPoint);
		m_State = State.Default;
		m_Tooltip.value = Tooltip.None;
		m_ForceApply = false;
		m_ForceCancel = false;
		m_ApplyBlocked = false;
		base.requireUnderground = false;
		base.requireNetArrows = false;
		base.requireRoutes = RouteType.None;
		base.requireNet = Layer.None;
	}

	private protected override void UpdateActions()
	{
		using (ProxyAction.DeferStateUpdating())
		{
			UpdateApplyAction();
			UpdateSecondaryApplyAction();
			UpdateCancelAction();
		}
	}

	private void UpdateApplyAction()
	{
		switch (state)
		{
		case State.Default:
		{
			Route component3;
			DynamicBuffer<RouteWaypoint> buffer2;
			if (m_ControlPoints.Length < 1 || m_ControlPoints[0].Equals(default(ControlPoint)))
			{
				base.applyAction.shouldBeEnabled = base.actionsEnabled;
				base.applyActionOverride = null;
			}
			else if (m_ControlPoints.Length == 1 && base.EntityManager.TryGetComponent<Route>(m_ControlPoints[0].m_OriginalEntity, out component3) && component3.m_Flags == RouteFlags.Complete && base.EntityManager.TryGetBuffer(m_ControlPoints[0].m_OriginalEntity, isReadOnly: true, out buffer2))
			{
				for (int j = 0; j < buffer2.Length; j++)
				{
					Entity waypoint2 = buffer2[j].m_Waypoint;
					if (base.EntityManager.TryGetComponent<Position>(waypoint2, out var component4) && component4.m_Position.Equals(m_ControlPoints[0].m_Position))
					{
						base.applyAction.shouldBeEnabled = base.actionsEnabled;
						base.applyActionOverride = m_MoveWaypoint;
						return;
					}
				}
				base.applyAction.shouldBeEnabled = base.actionsEnabled;
				base.applyActionOverride = m_InsertWaypoint;
			}
			else if (!base.EntityManager.HasComponent<RouteWaypoint>(m_ControlPoints[0].m_OriginalEntity) || !math.any(m_ControlPoints[0].m_ElementIndex >= 0))
			{
				base.applyAction.shouldBeEnabled = base.actionsEnabled;
				base.applyActionOverride = m_CreateRoute;
			}
			else
			{
				base.applyAction.shouldBeEnabled = false;
				base.applyActionOverride = null;
			}
			break;
		}
		case State.Create:
		{
			ref NativeList<ControlPoint> reference = ref m_ControlPoints;
			if (reference[reference.Length - 1].Equals(default(ControlPoint)))
			{
				base.applyAction.shouldBeEnabled = base.actionsEnabled;
				base.applyActionOverride = null;
				break;
			}
			ref NativeList<ControlPoint> reference2 = ref m_ControlPoints;
			ControlPoint controlPoint = reference2[reference2.Length - 1];
			ref float3 position = ref controlPoint.m_Position;
			ref NativeList<ControlPoint> reference3 = ref m_ControlPoints;
			if (!position.Equals(reference3[reference3.Length - 2].m_Position))
			{
				ref NativeList<ControlPoint> reference4 = ref m_ControlPoints;
				if (!reference4[reference4.Length - 1].m_Position.Equals(m_ControlPoints[0].m_Position))
				{
					base.applyAction.shouldBeEnabled = base.actionsEnabled;
					base.applyActionOverride = (GetAllowApply() ? m_AddWaypoint : null);
					break;
				}
			}
			if (m_ControlPoints.Length >= 3)
			{
				ref NativeList<ControlPoint> reference5 = ref m_ControlPoints;
				if (reference5[reference5.Length - 1].m_Position.Equals(m_ControlPoints[0].m_Position))
				{
					base.applyAction.shouldBeEnabled = base.actionsEnabled;
					base.applyActionOverride = (GetAllowApply() ? m_CompleteRoute : null);
					break;
				}
			}
			base.applyAction.shouldBeEnabled = base.actionsEnabled;
			base.applyActionOverride = null;
			break;
		}
		case State.Modify:
		{
			if (base.EntityManager.TryGetComponent<Route>(m_MoveStartPosition.m_OriginalEntity, out var component) && component.m_Flags == RouteFlags.Complete && base.EntityManager.TryGetBuffer(m_MoveStartPosition.m_OriginalEntity, isReadOnly: true, out DynamicBuffer<RouteWaypoint> buffer))
			{
				for (int i = 0; i < buffer.Length; i++)
				{
					Entity waypoint = buffer[i].m_Waypoint;
					if (base.EntityManager.TryGetComponent<Position>(waypoint, out var component2))
					{
						if (!component2.m_Position.Equals(m_MoveStartPosition.m_Position) && component2.m_Position.Equals(m_ControlPoints[0].m_Position))
						{
							base.applyAction.shouldBeEnabled = base.actionsEnabled;
							base.applyActionOverride = m_MergeWaypoint;
							return;
						}
						if (component2.m_Position.Equals(m_MoveStartPosition.m_Position))
						{
							base.applyAction.shouldBeEnabled = base.actionsEnabled;
							base.applyActionOverride = m_MoveWaypoint;
							return;
						}
					}
				}
				base.applyAction.shouldBeEnabled = base.actionsEnabled;
				base.applyActionOverride = m_InsertWaypoint;
			}
			else
			{
				base.applyAction.shouldBeEnabled = false;
				base.applyActionOverride = null;
			}
			break;
		}
		case State.Remove:
			base.applyAction.shouldBeEnabled = base.actionsEnabled;
			base.applyActionOverride = null;
			break;
		default:
			base.applyAction.shouldBeEnabled = false;
			base.applyActionOverride = null;
			break;
		}
	}

	private void UpdateSecondaryApplyAction()
	{
		switch (state)
		{
		case State.Default:
		{
			if (m_ControlPoints.Length == 1 && base.EntityManager.TryGetComponent<Route>(m_ControlPoints[0].m_OriginalEntity, out var component) && component.m_Flags == RouteFlags.Complete && base.EntityManager.TryGetBuffer(m_ControlPoints[0].m_OriginalEntity, isReadOnly: true, out DynamicBuffer<RouteWaypoint> buffer))
			{
				for (int i = 0; i < buffer.Length; i++)
				{
					Entity waypoint = buffer[i].m_Waypoint;
					if (base.EntityManager.TryGetComponent<Position>(waypoint, out var component2) && component2.m_Position.Equals(m_ControlPoints[0].m_Position))
					{
						base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled;
						base.secondaryApplyActionOverride = ((buffer.Length >= 3) ? m_RemoveWaypoint : m_DeleteRoute);
						return;
					}
				}
			}
			base.secondaryApplyAction.shouldBeEnabled = false;
			base.secondaryApplyActionOverride = null;
			break;
		}
		case State.Create:
			if (m_ControlPoints.Length > 1)
			{
				base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled;
				base.secondaryApplyActionOverride = m_UndoWaypoint;
			}
			else
			{
				base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled;
				base.secondaryApplyActionOverride = null;
			}
			break;
		case State.Remove:
			base.secondaryApplyAction.shouldBeEnabled = base.actionsEnabled;
			base.secondaryApplyActionOverride = m_RemoveWaypoint;
			break;
		default:
			base.secondaryApplyAction.shouldBeEnabled = false;
			base.secondaryApplyActionOverride = null;
			break;
		}
	}

	private void UpdateCancelAction()
	{
		if (state == State.Modify)
		{
			if (base.EntityManager.TryGetComponent<Route>(m_MoveStartPosition.m_OriginalEntity, out var component) && component.m_Flags == RouteFlags.Complete && base.EntityManager.TryGetBuffer(m_MoveStartPosition.m_OriginalEntity, isReadOnly: true, out DynamicBuffer<RouteWaypoint> buffer))
			{
				for (int i = 0; i < buffer.Length; i++)
				{
					Entity waypoint = buffer[i].m_Waypoint;
					if (base.EntityManager.TryGetComponent<Position>(waypoint, out var component2))
					{
						if (!component2.m_Position.Equals(m_MoveStartPosition.m_Position) && component2.m_Position.Equals(m_ControlPoints[0].m_Position))
						{
							base.cancelAction.shouldBeEnabled = base.actionsEnabled;
							base.cancelActionOverride = m_DiscardMergeWaypoint;
							return;
						}
						if (component2.m_Position.Equals(m_MoveStartPosition.m_Position))
						{
							base.cancelAction.shouldBeEnabled = base.actionsEnabled;
							base.cancelActionOverride = m_DiscardMoveWaypoint;
							return;
						}
					}
				}
				base.cancelAction.shouldBeEnabled = base.actionsEnabled;
				base.cancelActionOverride = m_DiscardInsertWaypoint;
			}
			else
			{
				base.cancelAction.shouldBeEnabled = false;
				base.cancelActionOverride = null;
			}
		}
		else
		{
			base.cancelAction.shouldBeEnabled = false;
			base.cancelActionOverride = null;
		}
	}

	public NativeList<ControlPoint> GetControlPoints(out JobHandle dependencies)
	{
		dependencies = base.Dependency;
		return m_ControlPoints;
	}

	public override PrefabBase GetPrefab()
	{
		return prefab;
	}

	public override bool TrySetPrefab(PrefabBase prefab)
	{
		if (prefab is RoutePrefab routePrefab)
		{
			this.prefab = routePrefab;
			return true;
		}
		return false;
	}

	public override void SetUnderground(bool underground)
	{
		this.underground = underground;
	}

	public override void ElevationUp()
	{
		underground = false;
	}

	public override void ElevationDown()
	{
		underground = true;
	}

	public override void ElevationScroll()
	{
		underground = !underground;
	}

	public override void InitializeRaycast()
	{
		base.InitializeRaycast();
		if (prefab != null)
		{
			bool flag = false;
			RouteData componentData = m_PrefabSystem.GetComponentData<RouteData>(prefab);
			Entity entity = m_PrefabSystem.GetEntity(prefab);
			if ((m_State == State.Modify || m_State == State.Remove) && base.EntityManager.HasComponent<Route>(m_MoveStartPosition.m_OriginalEntity))
			{
				entity = base.EntityManager.GetComponentData<PrefabRef>(m_MoveStartPosition.m_OriginalEntity).m_Prefab;
			}
			RouteConnectionData component2;
			if (base.EntityManager.TryGetComponent<TransportLineData>(entity, out var component))
			{
				m_ToolRaycastSystem.typeMask = TypeMask.StaticObjects | TypeMask.Net | TypeMask.RouteWaypoints;
				m_ToolRaycastSystem.transportType = component.m_TransportType;
				m_ToolRaycastSystem.raycastFlags |= RaycastFlags.BuildingLots;
				if (component.m_PassengerTransport)
				{
					m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Passenger;
				}
				if (component.m_CargoTransport)
				{
					m_ToolRaycastSystem.raycastFlags |= RaycastFlags.Cargo;
				}
				switch (component.m_TransportType)
				{
				case TransportType.Bus:
					m_ToolRaycastSystem.netLayerMask = Layer.Road | Layer.Pathway | Layer.MarkerPathway | Layer.PublicTransportRoad;
					flag = true;
					break;
				case TransportType.Tram:
					m_ToolRaycastSystem.netLayerMask = Layer.Road | Layer.TramTrack | Layer.PublicTransportRoad;
					flag = true;
					break;
				case TransportType.Train:
					m_ToolRaycastSystem.netLayerMask = Layer.TrainTrack;
					flag = true;
					break;
				case TransportType.Subway:
					m_ToolRaycastSystem.netLayerMask = Layer.SubwayTrack;
					flag = true;
					break;
				case TransportType.Ship:
					m_ToolRaycastSystem.netLayerMask = Layer.Waterway;
					break;
				case TransportType.Airplane:
					m_ToolRaycastSystem.netLayerMask = Layer.Taxiway | Layer.MarkerTaxiway;
					break;
				case TransportType.Ferry:
					m_ToolRaycastSystem.netLayerMask = Layer.Waterway;
					break;
				default:
					m_ToolRaycastSystem.netLayerMask = Layer.None;
					break;
				}
			}
			else if (base.EntityManager.TryGetComponent<RouteConnectionData>(entity, out component2))
			{
				m_ToolRaycastSystem.typeMask = TypeMask.StaticObjects | TypeMask.Net | TypeMask.RouteWaypoints;
				m_ToolRaycastSystem.transportType = TransportType.Work;
				m_ToolRaycastSystem.raycastFlags |= RaycastFlags.BuildingLots;
				if ((component2.m_RouteRoadType & RoadTypes.Car) != RoadTypes.None)
				{
					m_ToolRaycastSystem.netLayerMask |= Layer.Road | Layer.Pathway | Layer.MarkerPathway;
				}
				if ((component2.m_RouteRoadType & RoadTypes.Watercraft) != RoadTypes.None)
				{
					m_ToolRaycastSystem.netLayerMask |= Layer.Waterway;
				}
			}
			else
			{
				m_ToolRaycastSystem.typeMask = TypeMask.Terrain | TypeMask.RouteWaypoints;
				m_ToolRaycastSystem.netLayerMask = Layer.None;
			}
			if (serviceUpgrade)
			{
				m_ToolRaycastSystem.owner = GetUpgradable(m_ToolSystem.selected);
			}
			if (flag && underground)
			{
				m_ToolRaycastSystem.collisionMask = CollisionMask.Underground;
			}
			else
			{
				m_ToolRaycastSystem.collisionMask = CollisionMask.OnGround | CollisionMask.Overground;
			}
			if (m_State == State.Default)
			{
				m_ToolRaycastSystem.typeMask |= TypeMask.RouteSegments;
			}
			m_ToolRaycastSystem.routeType = componentData.m_Type;
		}
		else
		{
			m_ToolRaycastSystem.typeMask = TypeMask.None;
			m_ToolRaycastSystem.netLayerMask = Layer.None;
			m_ToolRaycastSystem.routeType = RouteType.None;
		}
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		UpdateActions();
		bool flag = m_ForceApply;
		bool flag2 = m_ForceCancel;
		m_ForceApply = false;
		m_ForceCancel = false;
		if (prefab != null)
		{
			allowUnderground = false;
			base.requireUnderground = false;
			base.requireNetArrows = false;
			base.requireNet = Layer.None;
			base.requireStops = TransportType.None;
			RouteData componentData = m_PrefabSystem.GetComponentData<RouteData>(prefab);
			base.requireRoutes = componentData.m_Type;
			if (componentData.m_Type == RouteType.TransportLine)
			{
				TransportLineData componentData2 = m_PrefabSystem.GetComponentData<TransportLineData>(prefab);
				base.requireNetArrows = true;
				switch (componentData2.m_TransportType)
				{
				case TransportType.Bus:
					base.requireNet |= Layer.Road | Layer.Pathway | Layer.MarkerPathway | Layer.PublicTransportRoad;
					allowUnderground = true;
					break;
				case TransportType.Tram:
					base.requireNet |= Layer.Road | Layer.TramTrack | Layer.PublicTransportRoad;
					allowUnderground = true;
					break;
				case TransportType.Train:
					base.requireNet |= Layer.TrainTrack;
					allowUnderground = true;
					break;
				case TransportType.Subway:
					base.requireNet |= Layer.SubwayTrack;
					allowUnderground = true;
					break;
				case TransportType.Ship:
					base.requireNet |= Layer.Waterway;
					break;
				case TransportType.Airplane:
					base.requireNet |= Layer.Taxiway | Layer.MarkerTaxiway;
					break;
				case TransportType.Ferry:
					base.requireNet |= Layer.Waterway;
					break;
				}
				base.requireStops = componentData2.m_TransportType;
			}
			else if (componentData.m_Type == RouteType.WorkRoute)
			{
				RouteConnectionData componentData3 = m_PrefabSystem.GetComponentData<RouteConnectionData>(prefab);
				if ((componentData3.m_RouteRoadType & RoadTypes.Car) != RoadTypes.None)
				{
					base.requireNet |= Layer.Road | Layer.Pathway | Layer.MarkerPathway;
				}
				if ((componentData3.m_RouteRoadType & RoadTypes.Watercraft) != RoadTypes.None)
				{
					base.requireNet |= Layer.Waterway;
				}
				base.requireStops = TransportType.Work;
			}
			if (allowUnderground)
			{
				base.requireUnderground = underground;
			}
			UpdateInfoview(m_PrefabSystem.GetEntity(prefab));
			if ((m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable | RaycastFlags.UIDisable)) == 0)
			{
				switch (m_State)
				{
				case State.Default:
				case State.Create:
					if (m_ApplyBlocked)
					{
						if (base.applyAction.WasReleasedThisFrame() || base.secondaryApplyAction.WasReleasedThisFrame())
						{
							m_ApplyBlocked = false;
						}
						return Update(inputDeps, fullUpdate: false);
					}
					if (base.applyAction.WasPressedThisFrame())
					{
						return Apply(inputDeps, base.applyAction.WasReleasedThisFrame());
					}
					if (base.secondaryApplyAction.WasPressedThisFrame())
					{
						return Cancel(inputDeps, base.secondaryApplyAction.WasReleasedThisFrame());
					}
					break;
				case State.Modify:
					if (base.cancelAction.WasPressedThisFrame())
					{
						m_ApplyBlocked = true;
						m_State = State.Default;
						return Update(inputDeps, fullUpdate: true);
					}
					if (flag || base.applyAction.WasReleasedThisFrame())
					{
						return Apply(inputDeps);
					}
					break;
				case State.Remove:
					if (base.cancelAction.WasPressedThisFrame())
					{
						m_ApplyBlocked = true;
						m_State = State.Default;
						return Update(inputDeps, fullUpdate: true);
					}
					if (flag2 || base.secondaryApplyAction.WasReleasedThisFrame())
					{
						return Cancel(inputDeps);
					}
					break;
				}
				return Update(inputDeps, fullUpdate: false);
			}
		}
		else
		{
			base.requireUnderground = false;
			base.requireNetArrows = false;
			base.requireRoutes = RouteType.None;
			base.requireNet = Layer.None;
			UpdateInfoview(Entity.Null);
			m_Tooltip.value = Tooltip.None;
		}
		if (m_State == State.Modify && (!base.applyAction.enabled || base.applyAction.WasReleasedThisFrame()))
		{
			if ((m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable | RaycastFlags.UIDisable)) == 0)
			{
				return Cancel(inputDeps);
			}
			m_ControlPoints.Clear();
			m_State = State.Default;
		}
		else if (m_State == State.Remove && (!base.secondaryApplyAction.enabled || base.secondaryApplyAction.WasReleasedThisFrame()))
		{
			if ((m_ToolRaycastSystem.raycastFlags & (RaycastFlags.DebugDisable | RaycastFlags.UIDisable)) == 0)
			{
				return Apply(inputDeps);
			}
			m_ControlPoints.Clear();
			m_State = State.Default;
		}
		if (m_State != State.Default)
		{
			return inputDeps;
		}
		return Clear(inputDeps);
	}

	protected override bool GetRaycastResult(out ControlPoint controlPoint)
	{
		if (GetRaycastResult(out Entity entity, out RaycastHit hit))
		{
			if (base.EntityManager.HasComponent<ConnectedRoute>(hit.m_HitEntity))
			{
				entity = hit.m_HitEntity;
			}
			controlPoint = new ControlPoint(entity, hit);
			return true;
		}
		controlPoint = default(ControlPoint);
		return false;
	}

	protected override bool GetRaycastResult(out ControlPoint controlPoint, out bool forceUpdate)
	{
		if (GetRaycastResult(out var entity, out var hit, out forceUpdate))
		{
			if (base.EntityManager.HasComponent<ConnectedRoute>(hit.m_HitEntity))
			{
				entity = hit.m_HitEntity;
			}
			controlPoint = new ControlPoint(entity, hit);
			return true;
		}
		controlPoint = default(ControlPoint);
		return false;
	}

	private JobHandle Clear(JobHandle inputDeps)
	{
		base.applyMode = ApplyMode.Clear;
		return inputDeps;
	}

	private JobHandle Cancel(JobHandle inputDeps, bool singleFrameOnly = false)
	{
		m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_TransportLineRemoveSound);
		switch (m_State)
		{
		case State.Default:
			if (GetAllowApply() && m_ControlPoints.Length > 0)
			{
				base.applyMode = ApplyMode.Clear;
				ControlPoint controlPoint2 = m_ControlPoints[0];
				if (base.EntityManager.HasComponent<Route>(controlPoint2.m_OriginalEntity) && controlPoint2.m_ElementIndex.x >= 0)
				{
					m_State = State.Remove;
					m_MoveStartPosition = controlPoint2;
					m_ForceCancel = singleFrameOnly;
					if (GetRaycastResult(out var controlPoint3))
					{
						m_LastRaycastPoint = controlPoint3;
						m_ControlPoints[0] = controlPoint3;
						inputDeps = SnapControlPoints(inputDeps, Entity.Null);
						inputDeps = UpdateDefinitions(inputDeps, Entity.Null);
					}
					else
					{
						m_Tooltip.value = Tooltip.None;
					}
					return inputDeps;
				}
				return Update(inputDeps, fullUpdate: false);
			}
			return Update(inputDeps, fullUpdate: false);
		case State.Create:
		{
			m_ControlPoints.RemoveAtSwapBack(m_ControlPoints.Length - 1);
			base.applyMode = ApplyMode.Clear;
			if (m_ControlPoints.Length <= 1)
			{
				m_State = State.Default;
			}
			if (GetRaycastResult(out var controlPoint5))
			{
				m_LastRaycastPoint = controlPoint5;
				m_ControlPoints[m_ControlPoints.Length - 1] = controlPoint5;
				inputDeps = SnapControlPoints(inputDeps, Entity.Null);
				inputDeps = UpdateDefinitions(inputDeps, Entity.Null);
			}
			else if (m_ControlPoints.Length >= 2)
			{
				m_ControlPoints[m_ControlPoints.Length - 1] = m_ControlPoints[m_ControlPoints.Length - 2];
				inputDeps = UpdateDefinitions(inputDeps, Entity.Null);
			}
			else
			{
				m_Tooltip.value = Tooltip.None;
			}
			return inputDeps;
		}
		case State.Modify:
		{
			m_ControlPoints.Clear();
			base.applyMode = ApplyMode.Clear;
			m_State = State.Default;
			if (GetRaycastResult(out var controlPoint4))
			{
				m_LastRaycastPoint = controlPoint4;
				m_ControlPoints.Add(in controlPoint4);
				inputDeps = SnapControlPoints(inputDeps, Entity.Null);
				inputDeps = UpdateDefinitions(inputDeps, Entity.Null);
			}
			else
			{
				m_Tooltip.value = Tooltip.None;
			}
			return inputDeps;
		}
		case State.Remove:
		{
			Entity applyTempRoute = Entity.Null;
			if (GetAllowApply() && !m_TempRouteQuery.IsEmptyIgnoreFilter)
			{
				base.applyMode = ApplyMode.Apply;
				NativeArray<ArchetypeChunk> nativeArray = m_TempRouteQuery.ToArchetypeChunkArray(Allocator.TempJob);
				applyTempRoute = nativeArray[0].GetNativeArray(GetEntityTypeHandle())[0];
				nativeArray.Dispose();
			}
			else
			{
				base.applyMode = ApplyMode.Clear;
			}
			m_State = State.Default;
			m_ControlPoints.Clear();
			if (GetRaycastResult(out var controlPoint))
			{
				m_LastRaycastPoint = controlPoint;
				m_ControlPoints.Add(in controlPoint);
				inputDeps = SnapControlPoints(inputDeps, applyTempRoute);
				inputDeps = UpdateDefinitions(inputDeps, applyTempRoute);
			}
			else
			{
				m_Tooltip.value = Tooltip.None;
			}
			return inputDeps;
		}
		default:
			return Update(inputDeps, fullUpdate: false);
		}
	}

	private JobHandle Apply(JobHandle inputDeps, bool singleFrameOnly = false)
	{
		switch (m_State)
		{
		case State.Default:
			if (GetAllowApply() && m_ControlPoints.Length > 0)
			{
				base.applyMode = ApplyMode.Clear;
				ControlPoint value = m_ControlPoints[0];
				m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_TransportLineStartSound);
				if (base.EntityManager.HasComponent<Route>(value.m_OriginalEntity) && math.any(value.m_ElementIndex >= 0))
				{
					m_State = State.Modify;
					m_ControlPointsMoved = value.m_ElementIndex.y >= 0;
					m_MoveStartPosition = value;
					m_ForceApply = singleFrameOnly;
					m_CanApplyModify = false;
					if (GetRaycastResult(out var controlPoint2))
					{
						m_LastRaycastPoint = controlPoint2;
						m_ControlPoints[0] = controlPoint2;
						inputDeps = SnapControlPoints(inputDeps, Entity.Null);
						JobHandle.ScheduleBatchedJobs();
						inputDeps.Complete();
						ControlPoint other = m_ControlPoints[0];
						m_ControlPointsMoved |= !m_MoveStartPosition.Equals(other);
						inputDeps = UpdateDefinitions(inputDeps, Entity.Null);
					}
					else
					{
						m_Tooltip.value = Tooltip.None;
					}
					return inputDeps;
				}
				if (!value.Equals(default(ControlPoint)))
				{
					m_State = State.Create;
					m_MoveStartPosition = default(ControlPoint);
					if (GetRaycastResult(out var controlPoint3))
					{
						m_LastRaycastPoint = controlPoint3;
						m_ControlPoints.Add(in controlPoint3);
						inputDeps = SnapControlPoints(inputDeps, Entity.Null);
						inputDeps = UpdateDefinitions(inputDeps, Entity.Null);
					}
					else
					{
						m_ControlPoints.Add(in value);
						m_Tooltip.value = Tooltip.None;
					}
					return inputDeps;
				}
				return Update(inputDeps, fullUpdate: false);
			}
			return Update(inputDeps, fullUpdate: false);
		case State.Create:
			if (GetAllowApply() && !m_TempRouteQuery.IsEmptyIgnoreFilter && GetPathfindCompleted())
			{
				RouteData componentData = m_PrefabSystem.GetComponentData<RouteData>(prefab);
				float num = math.distance(m_ControlPoints[m_ControlPoints.Length - 2].m_Position, m_ControlPoints[m_ControlPoints.Length - 1].m_Position);
				float minWaypointDistance = RouteUtils.GetMinWaypointDistance(componentData);
				if (num >= minWaypointDistance)
				{
					Entity applyTempRoute = Entity.Null;
					NativeArray<ArchetypeChunk> nativeArray = m_TempRouteQuery.ToArchetypeChunkArray(Allocator.TempJob);
					ComponentTypeHandle<Route> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Routes_Route_RO_ComponentTypeHandle, ref base.CheckedStateRef);
					if ((nativeArray[0].GetNativeArray(ref typeHandle)[0].m_Flags & RouteFlags.Complete) != 0)
					{
						base.applyMode = ApplyMode.Apply;
						m_State = State.Default;
						m_ControlPoints.Clear();
						applyTempRoute = nativeArray[0].GetNativeArray(GetEntityTypeHandle())[0];
						m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_TransportLineCompleteSound);
					}
					else
					{
						m_AudioManager.PlayUISound(m_SoundQuery.GetSingleton<ToolUXSoundSettingsData>().m_TransportLineBuildSound);
						base.applyMode = ApplyMode.Clear;
					}
					nativeArray.Dispose();
					if (GetRaycastResult(out var controlPoint4))
					{
						m_LastRaycastPoint = controlPoint4;
						m_ControlPoints.Add(in controlPoint4);
						inputDeps = SnapControlPoints(inputDeps, applyTempRoute);
						inputDeps = UpdateDefinitions(inputDeps, applyTempRoute);
					}
					else
					{
						m_Tooltip.value = Tooltip.None;
					}
					return inputDeps;
				}
			}
			return Update(inputDeps, fullUpdate: false);
		case State.Modify:
		{
			bool allowApply = GetAllowApply();
			if (!m_ControlPointsMoved && allowApply && m_ControlPoints.Length > 0)
			{
				base.applyMode = ApplyMode.Clear;
				m_State = State.Create;
				m_MoveStartPosition = default(ControlPoint);
				if (GetRaycastResult(out var controlPoint5))
				{
					m_LastRaycastPoint = controlPoint5;
					m_ControlPoints.Add(in controlPoint5);
					inputDeps = SnapControlPoints(inputDeps, Entity.Null);
					inputDeps = UpdateDefinitions(inputDeps, Entity.Null);
				}
				else
				{
					m_ControlPoints.Add(m_ControlPoints[0]);
					m_Tooltip.value = Tooltip.None;
				}
				return inputDeps;
			}
			if (m_CanApplyModify)
			{
				Entity applyTempRoute2 = Entity.Null;
				if (allowApply && !m_TempRouteQuery.IsEmptyIgnoreFilter)
				{
					base.applyMode = ApplyMode.Apply;
					NativeArray<ArchetypeChunk> nativeArray2 = m_TempRouteQuery.ToArchetypeChunkArray(Allocator.TempJob);
					applyTempRoute2 = nativeArray2[0].GetNativeArray(GetEntityTypeHandle())[0];
					nativeArray2.Dispose();
				}
				else
				{
					base.applyMode = ApplyMode.Clear;
				}
				m_State = State.Default;
				m_ControlPoints.Clear();
				if (GetRaycastResult(out var controlPoint6))
				{
					m_LastRaycastPoint = controlPoint6;
					m_ControlPoints.Add(in controlPoint6);
					inputDeps = SnapControlPoints(inputDeps, applyTempRoute2);
					inputDeps = UpdateDefinitions(inputDeps, applyTempRoute2);
				}
				else
				{
					m_Tooltip.value = Tooltip.None;
				}
				return inputDeps;
			}
			m_ForceApply = true;
			return Update(inputDeps, fullUpdate: false);
		}
		case State.Remove:
		{
			m_ControlPoints.Clear();
			base.applyMode = ApplyMode.Clear;
			m_State = State.Default;
			if (GetRaycastResult(out var controlPoint))
			{
				m_LastRaycastPoint = controlPoint;
				m_ControlPoints.Add(in controlPoint);
				inputDeps = SnapControlPoints(inputDeps, Entity.Null);
				inputDeps = UpdateDefinitions(inputDeps, Entity.Null);
			}
			else
			{
				m_Tooltip.value = Tooltip.None;
			}
			return inputDeps;
		}
		default:
			return Update(inputDeps, fullUpdate: false);
		}
	}

	private bool GetPathfindCompleted()
	{
		NativeArray<Entity> nativeArray = m_TempRouteQuery.ToEntityArray(Allocator.TempJob);
		try
		{
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				DynamicBuffer<RouteWaypoint> buffer = base.EntityManager.GetBuffer<RouteWaypoint>(entity, isReadOnly: true);
				DynamicBuffer<RouteSegment> buffer2 = base.EntityManager.GetBuffer<RouteSegment>(entity, isReadOnly: true);
				for (int j = 0; j < buffer2.Length; j++)
				{
					Entity segment = buffer2[j].m_Segment;
					if (base.EntityManager.TryGetComponent<PathTargets>(segment, out var component))
					{
						RouteWaypoint routeWaypoint = buffer[j];
						RouteWaypoint routeWaypoint2 = buffer[math.select(j + 1, 0, j + 1 >= buffer.Length)];
						if (base.EntityManager.TryGetComponent<Position>(routeWaypoint.m_Waypoint, out var component2) && math.distancesq(component.m_ReadyStartPosition, component2.m_Position) >= 1f)
						{
							return false;
						}
						if (base.EntityManager.TryGetComponent<Position>(routeWaypoint2.m_Waypoint, out var component3) && math.distancesq(component.m_ReadyEndPosition, component3.m_Position) >= 1f)
						{
							return false;
						}
					}
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
		return true;
	}

	private bool CheckPathUpdates()
	{
		if (m_EventQuery.IsEmptyIgnoreFilter)
		{
			return false;
		}
		NativeArray<ArchetypeChunk> nativeArray = m_EventQuery.ToArchetypeChunkArray(Allocator.TempJob);
		try
		{
			ComponentTypeHandle<PathUpdated> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathUpdated_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			ComponentLookup<Temp> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				NativeArray<PathUpdated> nativeArray2 = nativeArray[i].GetNativeArray(ref typeHandle);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					if (componentLookup.HasComponent(nativeArray2[j].m_Owner))
					{
						return true;
					}
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
		return false;
	}

	private JobHandle Update(JobHandle inputDeps, bool fullUpdate)
	{
		bool flag = CheckPathUpdates();
		if (m_State == State.Modify)
		{
			m_CanApplyModify = true;
		}
		if (GetRaycastResult(out ControlPoint controlPoint, out bool forceUpdate))
		{
			forceUpdate = forceUpdate || fullUpdate;
			if (m_ControlPoints.Length == 0)
			{
				m_LastRaycastPoint = controlPoint;
				m_ControlPoints.Add(in controlPoint);
				inputDeps = SnapControlPoints(inputDeps, Entity.Null);
				base.applyMode = ApplyMode.Clear;
				return UpdateDefinitions(inputDeps, Entity.Null);
			}
			if (m_LastRaycastPoint.Equals(controlPoint) && !flag && !forceUpdate)
			{
				base.applyMode = ApplyMode.None;
				return inputDeps;
			}
			m_LastRaycastPoint = controlPoint;
			ControlPoint controlPoint2 = m_ControlPoints[m_ControlPoints.Length - 1];
			m_ControlPoints[m_ControlPoints.Length - 1] = controlPoint;
			inputDeps = SnapControlPoints(inputDeps, Entity.Null);
			JobHandle.ScheduleBatchedJobs();
			inputDeps.Complete();
			ControlPoint other = m_ControlPoints[m_ControlPoints.Length - 1];
			if (controlPoint2.EqualsIgnoreHit(other) && !flag && !forceUpdate)
			{
				base.applyMode = ApplyMode.None;
			}
			else
			{
				m_ControlPointsMoved = true;
				base.applyMode = ApplyMode.Clear;
				inputDeps = UpdateDefinitions(inputDeps, Entity.Null);
			}
			return inputDeps;
		}
		if (m_LastRaycastPoint.Equals(controlPoint))
		{
			forceUpdate = forceUpdate || fullUpdate;
			if (flag || forceUpdate)
			{
				base.applyMode = ApplyMode.Clear;
				if (m_ControlPoints.Length > 0)
				{
					return UpdateDefinitions(inputDeps, Entity.Null);
				}
				return inputDeps;
			}
			base.applyMode = ApplyMode.None;
			return inputDeps;
		}
		m_LastRaycastPoint = controlPoint;
		if (m_State == State.Default && m_ControlPoints.Length == 1)
		{
			base.applyMode = ApplyMode.Clear;
			m_ControlPoints[m_ControlPoints.Length - 1] = default(ControlPoint);
			return UpdateDefinitions(inputDeps, Entity.Null);
		}
		if (m_State == State.Modify && m_ControlPoints.Length >= 1)
		{
			m_ControlPointsMoved = true;
			base.applyMode = ApplyMode.Clear;
			m_ControlPoints[m_ControlPoints.Length - 1] = m_MoveStartPosition;
			return UpdateDefinitions(inputDeps, Entity.Null);
		}
		if (m_State == State.Remove && m_ControlPoints.Length >= 1)
		{
			m_ControlPointsMoved = true;
			base.applyMode = ApplyMode.Clear;
			ControlPoint value = m_MoveStartPosition;
			value.m_OriginalEntity = Entity.Null;
			m_ControlPoints[m_ControlPoints.Length - 1] = value;
			return UpdateDefinitions(inputDeps, Entity.Null);
		}
		if (m_ControlPoints.Length >= 2)
		{
			m_ControlPointsMoved = true;
			base.applyMode = ApplyMode.Clear;
			m_ControlPoints[m_ControlPoints.Length - 1] = m_ControlPoints[m_ControlPoints.Length - 2];
			return UpdateDefinitions(inputDeps, Entity.Null);
		}
		m_Tooltip.value = Tooltip.None;
		return inputDeps;
	}

	private Entity GetUpgradable(Entity entity)
	{
		if (base.EntityManager.TryGetComponent<Attached>(entity, out var component))
		{
			return component.m_Parent;
		}
		return entity;
	}

	private JobHandle SnapControlPoints(JobHandle inputDeps, Entity applyTempRoute)
	{
		SnapJob jobData = new SnapJob
		{
			m_Snap = GetActualSnap(),
			m_State = m_State,
			m_Prefab = m_PrefabSystem.GetEntity(prefab),
			m_ApplyTempRoute = applyTempRoute,
			m_MoveStartPosition = m_MoveStartPosition,
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRouteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RouteData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTransportLineData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportLineData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRouteConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RouteConnectionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTrackLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TrackLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTransportStopData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportStopData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Position_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SlaveLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedRoutes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_ConnectedRoute_RO_BufferLookup, ref base.CheckedStateRef),
			m_Waypoints = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_ControlPoints = m_ControlPoints
		};
		if (serviceUpgrade)
		{
			jobData.m_ServiceUpgradeOwner = GetUpgradable(m_ToolSystem.selected);
		}
		return IJobExtensions.Schedule(jobData, inputDeps);
	}

	private JobHandle UpdateDefinitions(JobHandle inputDeps, Entity applyTempRoute)
	{
		JobHandle jobHandle = DestroyDefinitions(m_DefinitionQuery, m_ToolOutputBarrier, inputDeps);
		if (prefab != null)
		{
			CreateDefinitionsJob jobData = new CreateDefinitionsJob
			{
				m_State = m_State,
				m_Prefab = m_PrefabSystem.GetEntity(prefab),
				m_ApplyTempRoute = applyTempRoute,
				m_MoveStartPosition = m_MoveStartPosition,
				m_PrefabRouteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RouteData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Position_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Connected_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AttachedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectedRoutes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_ConnectedRoute_RO_BufferLookup, ref base.CheckedStateRef),
				m_Waypoints = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferLookup, ref base.CheckedStateRef),
				m_ControlPoints = m_ControlPoints,
				m_Tooltip = m_Tooltip,
				m_Color = color,
				m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer()
			};
			if (serviceUpgrade)
			{
				jobData.m_ServiceUpgradeOwner = GetUpgradable(m_ToolSystem.selected);
			}
			JobHandle jobHandle2 = IJobExtensions.Schedule(jobData, inputDeps);
			m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle2);
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
		}
		return jobHandle;
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
	public RouteToolSystem()
	{
	}
}
