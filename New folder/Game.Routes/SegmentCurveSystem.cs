using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.City;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Routes;

[CompilerGenerated]
public class SegmentCurveSystem : GameSystemBase
{
	[BurstCompile]
	private struct FindUpdatedSegmentsJob : IJob
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PathUpdated> m_PathUpdatedType;

		[ReadOnly]
		public BufferLookup<CurveElement> m_CurveElements;

		public NativeList<Entity> m_SegmentList;

		public void Execute()
		{
			int num = 0;
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				num += m_Chunks[i].Count;
			}
			NativeHashSet<Entity> nativeHashSet = new NativeHashSet<Entity>(num, Allocator.Temp);
			m_SegmentList.Capacity = num;
			for (int j = 0; j < m_Chunks.Length; j++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[j];
				NativeArray<PathUpdated> nativeArray = archetypeChunk.GetNativeArray(ref m_PathUpdatedType);
				if (nativeArray.Length != 0)
				{
					for (int k = 0; k < nativeArray.Length; k++)
					{
						PathUpdated pathUpdated = nativeArray[k];
						if (m_CurveElements.HasBuffer(pathUpdated.m_Owner) && nativeHashSet.Add(pathUpdated.m_Owner))
						{
							m_SegmentList.Add(in pathUpdated.m_Owner);
						}
					}
					continue;
				}
				NativeArray<Entity> nativeArray2 = archetypeChunk.GetNativeArray(m_EntityType);
				for (int l = 0; l < nativeArray2.Length; l++)
				{
					Entity value = nativeArray2[l];
					if (nativeHashSet.Add(value))
					{
						m_SegmentList.Add(in value);
					}
				}
			}
			nativeHashSet.Dispose();
		}
	}

	[BurstCompile]
	private struct UpdateSegmentCurvesJob : IJobParallelForDefer
	{
		[ReadOnly]
		public bool m_LeftHandTraffic;

		[ReadOnly]
		public NativeArray<Entity> m_SegmentList;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public ComponentLookup<Segment> m_SegmentData;

		[ReadOnly]
		public ComponentLookup<Position> m_PositionData;

		[ReadOnly]
		public ComponentLookup<TransportStop> m_TransportStopData;

		[ReadOnly]
		public ComponentLookup<PathTargets> m_PathTargetsData;

		[ReadOnly]
		public ComponentLookup<PathSource> m_PathSourceData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<SlaveLane> m_SlaveLaneData;

		[ReadOnly]
		public ComponentLookup<MasterLane> m_MasterLaneData;

		[ReadOnly]
		public ComponentLookup<NodeLane> m_NodeLaneData;

		[ReadOnly]
		public ComponentLookup<PathOwner> m_PathOwnerData;

		[ReadOnly]
		public ComponentLookup<Controller> m_ControllerData;

		[ReadOnly]
		public ComponentLookup<CarCurrentLane> m_CarCurrentLaneData;

		[ReadOnly]
		public ComponentLookup<WatercraftCurrentLane> m_WatercraftCurrentLaneData;

		[ReadOnly]
		public ComponentLookup<AircraftCurrentLane> m_AircraftCurrentLaneData;

		[ReadOnly]
		public ComponentLookup<TrainCurrentLane> m_TrainCurrentLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PublicTransport> m_PublicTransportData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.Taxi> m_TaxiData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PersonalCar> m_PersonalCarData;

		[ReadOnly]
		public ComponentLookup<Aircraft> m_AircraftData;

		[ReadOnly]
		public ComponentLookup<Bicycle> m_BicycleData;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> m_CurrentVehicleData;

		[ReadOnly]
		public ComponentLookup<GroupMember> m_GroupMemberData;

		[ReadOnly]
		public ComponentLookup<HumanCurrentLane> m_HumanCurrentLaneData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<RouteData> m_PrefabRouteData;

		[ReadOnly]
		public ComponentLookup<CarLaneData> m_PrefabCarLaneData;

		[ReadOnly]
		public ComponentLookup<NetLaneData> m_PrefabNetLaneData;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> m_RouteWaypoints;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public BufferLookup<CarNavigationLane> m_CarNavigationLanes;

		[ReadOnly]
		public BufferLookup<WatercraftNavigationLane> m_WatercraftNavigationLanes;

		[ReadOnly]
		public BufferLookup<AircraftNavigationLane> m_AircraftNavigationLanes;

		[ReadOnly]
		public BufferLookup<TrainNavigationLane> m_TrainNavigationLanes;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[NativeDisableParallelForRestriction]
		public BufferLookup<CurveElement> m_CurveElements;

		[NativeDisableParallelForRestriction]
		public BufferLookup<CurveSource> m_CurveSources;

		public void Execute(int index)
		{
			Entity entity = m_SegmentList[index];
			DynamicBuffer<CurveElement> curveElements = m_CurveElements[entity];
			m_CurveSources.TryGetBuffer(entity, out var bufferData);
			if (!m_OwnerData.TryGetComponent(entity, out var componentData))
			{
				curveElements.Clear();
				if (bufferData.IsCreated)
				{
					bufferData.Clear();
				}
				return;
			}
			Entity owner = componentData.m_Owner;
			Segment segment = m_SegmentData[entity];
			PrefabRef prefabRef = m_PrefabRefData[owner];
			DynamicBuffer<RouteWaypoint> dynamicBuffer = m_RouteWaypoints[owner];
			RouteData routeData = m_PrefabRouteData[prefabRef.m_Prefab];
			float nodeDistance = routeData.m_Width * 2.5f;
			float segmentLength = routeData.m_SegmentLength;
			float3 lastPosition = default(float3);
			float3 @float = default(float3);
			float3 lastTangent = default(float3);
			bool isFirst = true;
			bool airway = false;
			bool area = false;
			bool hasLastPos = false;
			bool hasNextPos = false;
			int connectionCount = 0;
			if (m_PathSourceData.TryGetComponent(entity, out var componentData2))
			{
				bool flag = false;
				bool flag2 = false;
				bool isPedestrian = false;
				bool isBicycle = false;
				bool skipAirway = true;
				bool skipArea = true;
				bool stayMidAir = false;
				m_UpdatedData.HasComponent(entity);
				curveElements.Clear();
				bufferData.Clear();
				m_GroupMemberData.TryGetComponent(componentData2.m_Entity, out var componentData3);
				WatercraftCurrentLane componentData5;
				DynamicBuffer<WatercraftNavigationLane> bufferData3;
				AircraftCurrentLane componentData6;
				DynamicBuffer<AircraftNavigationLane> bufferData4;
				DynamicBuffer<TrainNavigationLane> bufferData5;
				DynamicBuffer<LayoutElement> bufferData6;
				TrainCurrentLane componentData7;
				HumanCurrentLane componentData8;
				CurrentVehicle componentData9;
				if (m_CarCurrentLaneData.TryGetComponent(componentData2.m_Entity, out var componentData4) && m_CarNavigationLanes.TryGetBuffer(componentData2.m_Entity, out var bufferData2))
				{
					skipArea = false;
					isBicycle = m_BicycleData.HasComponent(componentData2.m_Entity);
					flag = TryAddSegments(curveElements, bufferData, componentData4, bufferData2, lastPosition, @float, nodeDistance, segmentLength, isBicycle, ref lastPosition, ref lastTangent, ref isFirst, ref airway, ref area, ref hasLastPos, ref hasNextPos, ref connectionCount);
				}
				else if (m_WatercraftCurrentLaneData.TryGetComponent(componentData2.m_Entity, out componentData5) && m_WatercraftNavigationLanes.TryGetBuffer(componentData2.m_Entity, out bufferData3))
				{
					skipArea = false;
					flag = TryAddSegments(curveElements, bufferData, componentData5, bufferData3, lastPosition, @float, nodeDistance, segmentLength, ref lastPosition, ref lastTangent, ref isFirst, ref airway, ref area, ref hasLastPos, ref hasNextPos, ref connectionCount);
				}
				else if (m_AircraftCurrentLaneData.TryGetComponent(componentData2.m_Entity, out componentData6) && m_AircraftNavigationLanes.TryGetBuffer(componentData2.m_Entity, out bufferData4))
				{
					airway = (componentData6.m_LaneFlags & AircraftLaneFlags.Flying) != 0;
					skipAirway = false;
					skipArea = false;
					stayMidAir = (m_AircraftData[componentData2.m_Entity].m_Flags & AircraftFlags.StayMidAir) != 0;
					flag = TryAddSegments(curveElements, bufferData, componentData6, bufferData4, lastPosition, @float, nodeDistance, segmentLength, stayMidAir, ref lastPosition, ref lastTangent, ref isFirst, ref airway, ref area, ref hasLastPos, ref hasNextPos, ref connectionCount);
				}
				else if (m_TrainNavigationLanes.TryGetBuffer(componentData2.m_Entity, out bufferData5) && m_LayoutElements.TryGetBuffer(componentData2.m_Entity, out bufferData6) && bufferData6.Length != 0 && m_TrainCurrentLaneData.TryGetComponent(bufferData6[0].m_Vehicle, out componentData7))
				{
					flag = TryAddSegments(curveElements, bufferData, componentData7, bufferData5, lastPosition, @float, nodeDistance, segmentLength, ref lastPosition, ref lastTangent, ref isFirst, ref airway, ref area, ref hasLastPos, ref hasNextPos, ref connectionCount);
				}
				else if (m_HumanCurrentLaneData.TryGetComponent(componentData2.m_Entity, out componentData8))
				{
					skipArea = false;
					flag = TryAddSegments(curveElements, bufferData, componentData8, lastPosition, @float, nodeDistance, segmentLength, ref lastPosition, ref lastTangent, ref isFirst, ref airway, ref area, ref hasLastPos, ref hasNextPos, ref connectionCount);
					isPedestrian = true;
				}
				else if (m_CurrentVehicleData.TryGetComponent(componentData2.m_Entity, out componentData9))
				{
					if (m_ControllerData.TryGetComponent(componentData9.m_Vehicle, out var componentData10) && componentData10.m_Controller != Entity.Null)
					{
						componentData9.m_Vehicle = componentData10.m_Controller;
					}
					if (m_TaxiData.HasComponent(componentData9.m_Vehicle) || m_PersonalCarData.HasComponent(componentData9.m_Vehicle))
					{
						componentData2.m_Entity = componentData9.m_Vehicle;
						flag2 = true;
					}
					else if (m_PublicTransportData.HasComponent(componentData9.m_Vehicle))
					{
						flag2 = true;
					}
					else
					{
						flag = true;
					}
					isPedestrian = true;
				}
				PathElement pathElement = default(PathElement);
				if (!flag && m_PathElements.TryGetBuffer(componentData2.m_Entity, out var bufferData7))
				{
					m_PathOwnerData.TryGetComponent(componentData2.m_Entity, out var componentData11);
					if (flag2)
					{
						while (componentData11.m_ElementIndex < bufferData7.Length)
						{
							PathElement pathElement2 = bufferData7[componentData11.m_ElementIndex];
							if (IsPedestrianTarget(pathElement2))
							{
								break;
							}
							componentData11.m_ElementIndex++;
						}
					}
					else if (componentData11.m_ElementIndex < bufferData7.Length)
					{
						pathElement = bufferData7[bufferData7.Length - 1];
					}
					TryAddSegments(curveElements, bufferData, bufferData7, componentData11, lastPosition, @float, nodeDistance, segmentLength, ref lastPosition, ref lastTangent, isPedestrian, isBicycle, skipAirway, skipArea, stayMidAir, ref isFirst, ref airway, ref area, ref hasLastPos, ref hasNextPos, ref connectionCount);
				}
				if (!flag && m_PathElements.TryGetBuffer(componentData3.m_Leader, out bufferData7))
				{
					m_PathOwnerData.TryGetComponent(componentData3.m_Leader, out var componentData12);
					if (flag2)
					{
						while (componentData12.m_ElementIndex < bufferData7.Length)
						{
							PathElement pathElement3 = bufferData7[componentData12.m_ElementIndex];
							if (IsPedestrianTarget(pathElement3))
							{
								break;
							}
							componentData12.m_ElementIndex++;
						}
					}
					else if (pathElement.m_Target != Entity.Null)
					{
						for (int i = componentData12.m_ElementIndex; i < bufferData7.Length; i++)
						{
							PathElement pathElement4 = bufferData7[i];
							if (pathElement4.m_Target == pathElement.m_Target && pathElement4.m_TargetDelta.Equals(pathElement.m_TargetDelta))
							{
								componentData12.m_ElementIndex = i + 1;
								break;
							}
						}
					}
					TryAddSegments(curveElements, bufferData, bufferData7, componentData12, lastPosition, @float, nodeDistance, segmentLength, ref lastPosition, ref lastTangent, isPedestrian, isBicycle, skipAirway, skipArea, stayMidAir, ref isFirst, ref airway, ref area, ref hasLastPos, ref hasNextPos, ref connectionCount);
				}
				if (connectionCount != 0)
				{
					ProcessConnectionSegments(curveElements, bufferData, nodeDistance, segmentLength, ref lastPosition, ref lastTangent, ref airway, ref area, ref hasLastPos, ref hasNextPos, ref connectionCount);
				}
				if (airway && hasLastPos && curveElements.Length == 0)
				{
					curveElements.Add(new CurveElement
					{
						m_Curve = new Bezier4x3(lastPosition, lastPosition, lastPosition, lastPosition)
					});
					if (bufferData.IsCreated)
					{
						bufferData.Add(default(CurveSource));
					}
				}
				return;
			}
			if (dynamicBuffer.Length >= 2)
			{
				Entity waypoint = dynamicBuffer[segment.m_Index].m_Waypoint;
				Entity waypoint2 = dynamicBuffer[math.select(segment.m_Index + 1, 0, segment.m_Index + 1 >= dynamicBuffer.Length)].m_Waypoint;
				lastPosition = m_PositionData[waypoint].m_Position;
				@float = m_PositionData[waypoint2].m_Position;
				if (m_PathTargetsData.TryGetComponent(entity, out var componentData13))
				{
					lastPosition = componentData13.m_ReadyStartPosition;
					@float = componentData13.m_ReadyEndPosition;
				}
				hasLastPos = true;
				hasNextPos = true;
			}
			curveElements.Clear();
			if (m_PathElements.TryGetBuffer(entity, out var bufferData8))
			{
				TryAddSegments(curveElements, bufferData, bufferData8, default(PathOwner), lastPosition, @float, nodeDistance, segmentLength, ref lastPosition, ref lastTangent, isPedestrian: false, isBicycle: false, skipAirway: true, skipArea: true, stayMidAir: false, ref isFirst, ref airway, ref area, ref hasLastPos, ref hasNextPos, ref connectionCount);
			}
			if (hasNextPos)
			{
				TryAddSegments(curveElements, bufferData, @float, default(float3), nodeDistance, segmentLength, ref lastPosition, ref lastTangent, ref airway, ref area);
			}
		}

		private bool TryAddSegments(DynamicBuffer<CurveElement> curveElements, DynamicBuffer<CurveSource> curveSources, CarCurrentLane carCurrentLane, DynamicBuffer<CarNavigationLane> navLanes, float3 lastNodePos, float3 nextNodePos, float nodeDistance, float segmentLength, bool isBicycle, ref float3 lastPosition, ref float3 lastTangent, ref bool isFirst, ref bool airway, ref bool area, ref bool hasLastPos, ref bool hasNextPos, ref int connectionCount)
		{
			int i = 0;
			bool airway2 = false;
			bool area2 = false;
			bool result = false;
			if (hasNextPos)
			{
				i = navLanes.Length;
			}
			else
			{
				if (ShouldEndPath(carCurrentLane.m_Lane))
				{
					return true;
				}
				for (; i < navLanes.Length; i++)
				{
					if (ShouldEndPath(navLanes[i].m_Lane))
					{
						result = true;
						break;
					}
				}
			}
			for (int num = --i; num >= 0; num--)
			{
				CarNavigationLane carNavigationLane = navLanes[num];
				GetMasterLane(ref carNavigationLane.m_Lane);
				if (IsLast(carNavigationLane.m_Lane, carNavigationLane.m_CurvePosition, nextNodePos, nodeDistance, hasNextPos, skipAirway: true, skipArea: false, ref airway2, ref area2))
				{
					i = num;
					break;
				}
			}
			if (i == -1 && !IsLast(carCurrentLane.m_Lane, carCurrentLane.m_CurvePosition.xz, nextNodePos, nodeDistance, hasNextPos, skipAirway: true, skipArea: false, ref airway2, ref area2))
			{
				i = -2;
			}
			if (i >= -1)
			{
				GetMasterLane(ref carCurrentLane.m_Lane);
				TryAddSegments(curveElements, curveSources, lastNodePos, nextNodePos, nodeDistance, segmentLength, carCurrentLane.m_Lane, carCurrentLane.m_CurvePosition.xz, i == -1, isBicycle, skipAirway: true, skipArea: false, stayMidAir: false, ref isFirst, ref lastPosition, ref lastTangent, ref airway, ref area, ref hasLastPos, ref hasNextPos, ref connectionCount);
			}
			for (int j = 0; j <= i; j++)
			{
				CarNavigationLane carNavigationLane2 = navLanes[j];
				GetMasterLane(ref carNavigationLane2.m_Lane);
				TryAddSegments(curveElements, curveSources, lastNodePos, nextNodePos, nodeDistance, segmentLength, carNavigationLane2.m_Lane, carNavigationLane2.m_CurvePosition, j == i, isBicycle, skipAirway: true, skipArea: false, stayMidAir: false, ref isFirst, ref lastPosition, ref lastTangent, ref airway, ref area, ref hasLastPos, ref hasNextPos, ref connectionCount);
			}
			airway |= airway2;
			area |= area2;
			return result;
		}

		private bool TryAddSegments(DynamicBuffer<CurveElement> curveElements, DynamicBuffer<CurveSource> curveSources, WatercraftCurrentLane watercraftCurrentLane, DynamicBuffer<WatercraftNavigationLane> navLanes, float3 lastNodePos, float3 nextNodePos, float nodeDistance, float segmentLength, ref float3 lastPosition, ref float3 lastTangent, ref bool isFirst, ref bool airway, ref bool area, ref bool hasLastPos, ref bool hasNextPos, ref int connectionCount)
		{
			int i = 0;
			bool airway2 = false;
			bool area2 = false;
			bool result = false;
			if (hasNextPos)
			{
				i = navLanes.Length;
			}
			else
			{
				if (ShouldEndPath(watercraftCurrentLane.m_Lane))
				{
					return true;
				}
				for (; i < navLanes.Length; i++)
				{
					if (ShouldEndPath(navLanes[i].m_Lane))
					{
						result = true;
						break;
					}
				}
			}
			for (int num = --i; num >= 0; num--)
			{
				WatercraftNavigationLane watercraftNavigationLane = navLanes[num];
				GetMasterLane(ref watercraftNavigationLane.m_Lane);
				if (IsLast(watercraftNavigationLane.m_Lane, watercraftNavigationLane.m_CurvePosition, nextNodePos, nodeDistance, hasNextPos, skipAirway: true, skipArea: false, ref airway2, ref area2))
				{
					i = num;
					break;
				}
			}
			if (i == -1 && !IsLast(watercraftCurrentLane.m_Lane, watercraftCurrentLane.m_CurvePosition.xz, nextNodePos, nodeDistance, hasNextPos, skipAirway: true, skipArea: false, ref airway2, ref area2))
			{
				i = -2;
			}
			if (i >= -1)
			{
				GetMasterLane(ref watercraftCurrentLane.m_Lane);
				TryAddSegments(curveElements, curveSources, lastNodePos, nextNodePos, nodeDistance, segmentLength, watercraftCurrentLane.m_Lane, watercraftCurrentLane.m_CurvePosition.xz, i == -1, isBicycle: false, skipAirway: true, skipArea: false, stayMidAir: false, ref isFirst, ref lastPosition, ref lastTangent, ref airway, ref area, ref hasLastPos, ref hasNextPos, ref connectionCount);
			}
			for (int j = 0; j <= i; j++)
			{
				WatercraftNavigationLane watercraftNavigationLane2 = navLanes[j];
				GetMasterLane(ref watercraftNavigationLane2.m_Lane);
				TryAddSegments(curveElements, curveSources, lastNodePos, nextNodePos, nodeDistance, segmentLength, watercraftNavigationLane2.m_Lane, watercraftNavigationLane2.m_CurvePosition, j == i, isBicycle: false, skipAirway: true, skipArea: false, stayMidAir: false, ref isFirst, ref lastPosition, ref lastTangent, ref airway, ref area, ref hasLastPos, ref hasNextPos, ref connectionCount);
			}
			airway |= airway2;
			area |= area2;
			return result;
		}

		private bool TryAddSegments(DynamicBuffer<CurveElement> curveElements, DynamicBuffer<CurveSource> curveSources, AircraftCurrentLane aircraftCurrentLane, DynamicBuffer<AircraftNavigationLane> navLanes, float3 lastNodePos, float3 nextNodePos, float nodeDistance, float segmentLength, bool stayMidAir, ref float3 lastPosition, ref float3 lastTangent, ref bool isFirst, ref bool airway, ref bool area, ref bool hasLastPos, ref bool hasNextPos, ref int connectionCount)
		{
			int i = 0;
			bool flag = false;
			bool area2 = false;
			bool airway2 = false;
			bool result = false;
			if (hasNextPos)
			{
				i = navLanes.Length;
			}
			else
			{
				if (ShouldEndPath(aircraftCurrentLane.m_Lane))
				{
					return true;
				}
				for (; i < navLanes.Length; i++)
				{
					if (ShouldEndPath(navLanes[i].m_Lane))
					{
						result = true;
						break;
					}
				}
			}
			for (int num = --i; num >= 0; num--)
			{
				AircraftNavigationLane aircraftNavigationLane = navLanes[num];
				airway2 = false;
				if (IsLast(aircraftNavigationLane.m_Lane, aircraftNavigationLane.m_CurvePosition, nextNodePos, nodeDistance, hasNextPos, skipAirway: false, skipArea: false, ref airway2, ref area2))
				{
					i = num;
					break;
				}
				flag = flag || airway2;
			}
			if (i == -1)
			{
				airway2 = false;
				if (!IsLast(aircraftCurrentLane.m_Lane, aircraftCurrentLane.m_CurvePosition.xz, nextNodePos, nodeDistance, hasNextPos, skipAirway: false, skipArea: false, ref airway2, ref area2))
				{
					i = -2;
					flag = flag || airway2;
				}
			}
			if (i == -2)
			{
				airway2 = airway;
			}
			if (airway2 && i + 1 < navLanes.Length)
			{
				i++;
			}
			if (i >= -1)
			{
				TryAddSegments(curveElements, curveSources, lastNodePos, nextNodePos, nodeDistance, segmentLength, aircraftCurrentLane.m_Lane, aircraftCurrentLane.m_CurvePosition.xz, i == -1, isBicycle: false, skipAirway: false, skipArea: false, stayMidAir, ref isFirst, ref lastPosition, ref lastTangent, ref airway, ref area, ref hasLastPos, ref hasNextPos, ref connectionCount);
			}
			for (int j = 0; j <= i; j++)
			{
				AircraftNavigationLane aircraftNavigationLane2 = navLanes[j];
				TryAddSegments(curveElements, curveSources, lastNodePos, nextNodePos, nodeDistance, segmentLength, aircraftNavigationLane2.m_Lane, aircraftNavigationLane2.m_CurvePosition, j == i, isBicycle: false, skipAirway: false, skipArea: false, stayMidAir, ref isFirst, ref lastPosition, ref lastTangent, ref airway, ref area, ref hasLastPos, ref hasNextPos, ref connectionCount);
			}
			airway |= flag;
			area |= area2;
			return result;
		}

		private bool TryAddSegments(DynamicBuffer<CurveElement> curveElements, DynamicBuffer<CurveSource> curveSources, TrainCurrentLane trainCurrentLane, DynamicBuffer<TrainNavigationLane> navLanes, float3 lastNodePos, float3 nextNodePos, float nodeDistance, float segmentLength, ref float3 lastPosition, ref float3 lastTangent, ref bool isFirst, ref bool airway, ref bool area, ref bool hasLastPos, ref bool hasNextPos, ref int connectionCount)
		{
			int i = 0;
			bool airway2 = false;
			bool area2 = false;
			bool result = false;
			if (hasNextPos)
			{
				i = navLanes.Length;
			}
			else
			{
				if (ShouldEndPath(trainCurrentLane.m_Front.m_Lane))
				{
					return true;
				}
				for (; i < navLanes.Length; i++)
				{
					if (ShouldEndPath(navLanes[i].m_Lane))
					{
						result = true;
						break;
					}
				}
			}
			for (int num = --i; num >= 0; num--)
			{
				TrainNavigationLane trainNavigationLane = navLanes[num];
				if (IsLast(trainNavigationLane.m_Lane, trainNavigationLane.m_CurvePosition, nextNodePos, nodeDistance, hasNextPos, skipAirway: true, skipArea: true, ref airway2, ref area2))
				{
					i = num;
					break;
				}
			}
			if (i == -1 && !IsLast(trainCurrentLane.m_Front.m_Lane, trainCurrentLane.m_Front.m_CurvePosition.yw, nextNodePos, nodeDistance, hasNextPos, skipAirway: true, skipArea: true, ref airway2, ref area2))
			{
				i = -2;
			}
			if (i >= -1)
			{
				TryAddSegments(curveElements, curveSources, lastNodePos, nextNodePos, nodeDistance, segmentLength, trainCurrentLane.m_Front.m_Lane, trainCurrentLane.m_Front.m_CurvePosition.yw, i == -1, isBicycle: false, skipAirway: true, skipArea: true, stayMidAir: false, ref isFirst, ref lastPosition, ref lastTangent, ref airway, ref area, ref hasLastPos, ref hasNextPos, ref connectionCount);
			}
			for (int j = 0; j <= i; j++)
			{
				TrainNavigationLane trainNavigationLane2 = navLanes[j];
				TryAddSegments(curveElements, curveSources, lastNodePos, nextNodePos, nodeDistance, segmentLength, trainNavigationLane2.m_Lane, trainNavigationLane2.m_CurvePosition, j == i, isBicycle: false, skipAirway: true, skipArea: true, stayMidAir: false, ref isFirst, ref lastPosition, ref lastTangent, ref airway, ref area, ref hasLastPos, ref hasNextPos, ref connectionCount);
			}
			airway |= airway2;
			area |= area2;
			return result;
		}

		private bool TryAddSegments(DynamicBuffer<CurveElement> curveElements, DynamicBuffer<CurveSource> curveSources, HumanCurrentLane humanCurrentLane, float3 lastNodePos, float3 nextNodePos, float nodeDistance, float segmentLength, ref float3 lastPosition, ref float3 lastTangent, ref bool isFirst, ref bool airway, ref bool area, ref bool hasLastPos, ref bool hasNextPos, ref int connectionCount)
		{
			if (!hasNextPos && ShouldEndPath(humanCurrentLane.m_Lane))
			{
				return true;
			}
			TryAddSegments(curveElements, curveSources, lastNodePos, nextNodePos, nodeDistance, segmentLength, humanCurrentLane.m_Lane, humanCurrentLane.m_CurvePosition, isLast: true, isBicycle: false, skipAirway: true, skipArea: false, stayMidAir: false, ref isFirst, ref lastPosition, ref lastTangent, ref airway, ref area, ref hasLastPos, ref hasNextPos, ref connectionCount);
			return false;
		}

		private void TryAddSegments(DynamicBuffer<CurveElement> curveElements, DynamicBuffer<CurveSource> curveSources, DynamicBuffer<PathElement> path, PathOwner pathOwner, float3 lastNodePos, float3 nextNodePos, float nodeDistance, float segmentLength, ref float3 lastPosition, ref float3 lastTangent, bool isPedestrian, bool isBicycle, bool skipAirway, bool skipArea, bool stayMidAir, ref bool isFirst, ref bool airway, ref bool area, ref bool hasLastPos, ref bool hasNextPos, ref int connectionCount)
		{
			int i = pathOwner.m_ElementIndex;
			bool flag = false;
			bool area2 = false;
			bool airway2 = false;
			if (hasNextPos)
			{
				i = path.Length;
			}
			else
			{
				for (; i < path.Length; i++)
				{
					PathElement pathElement = path[i];
					if (ShouldEndPath(pathElement, isPedestrian))
					{
						break;
					}
				}
			}
			for (int num = --i; num >= pathOwner.m_ElementIndex; num--)
			{
				PathElement pathElement2 = path[num];
				airway2 = false;
				if (IsLast(pathElement2.m_Target, pathElement2.m_TargetDelta, nextNodePos, nodeDistance, hasNextPos, skipAirway, skipArea, ref airway2, ref area2))
				{
					i = num;
					break;
				}
				flag = flag || airway2;
			}
			if (i == pathOwner.m_ElementIndex - 1)
			{
				airway2 = airway;
			}
			if (!skipAirway && airway2 && i + 1 < path.Length)
			{
				i++;
			}
			for (int j = pathOwner.m_ElementIndex; j <= i; j++)
			{
				PathElement pathElement3 = path[j];
				TryAddSegments(curveElements, curveSources, lastNodePos, nextNodePos, nodeDistance, segmentLength, pathElement3.m_Target, pathElement3.m_TargetDelta, j == i, isBicycle, skipAirway, skipArea, stayMidAir, ref isFirst, ref lastPosition, ref lastTangent, ref airway, ref area, ref hasLastPos, ref hasNextPos, ref connectionCount);
			}
			airway |= flag;
			area |= area2;
		}

		private bool IsLast(Entity target, float2 targetDelta, float3 nextNodePos, float nodeDistance, bool hasNextPos, bool skipAirway, bool skipArea, ref bool airway, ref bool area)
		{
			if (ShouldSkipTarget(target, skipAirway, skipArea, ref airway, ref area))
			{
				return false;
			}
			Bezier4x3 bezier4x = MathUtils.Cut(m_CurveData[target].m_Bezier, targetDelta);
			if (!hasNextPos || math.distance(nextNodePos, bezier4x.a) > nodeDistance)
			{
				return true;
			}
			return false;
		}

		private void TryAddSegments(DynamicBuffer<CurveElement> curveElements, DynamicBuffer<CurveSource> curveSources, float3 lastNodePos, float3 nextNodePos, float nodeDistance, float segmentLength, Entity target, float2 targetDelta, bool isLast, bool isBicycle, bool skipAirway, bool skipArea, bool stayMidAir, ref bool isFirst, ref float3 lastPosition, ref float3 lastTangent, ref bool airway, ref bool area, ref bool hasLastPos, ref bool hasNextPos, ref int connectionCount)
		{
			bool airway2 = false;
			bool area2 = false;
			if (ShouldSkipTarget(target, skipAirway, skipArea, ref airway2, ref area2))
			{
				if (!skipAirway & airway)
				{
					if (m_ConnectionLaneData.TryGetComponent(target, out var componentData) && (componentData.m_Flags & (ConnectionLaneFlags.Start | ConnectionLaneFlags.Outside)) == (ConnectionLaneFlags.Start | ConnectionLaneFlags.Outside) && m_OwnerData.TryGetComponent(target, out var componentData2))
					{
						target = componentData2.m_Owner;
					}
					if (m_TransformData.TryGetComponent(target, out var componentData3))
					{
						if (connectionCount != 0)
						{
							ProcessConnectionSegments(curveElements, curveSources, nodeDistance, segmentLength, ref lastPosition, ref lastTangent, ref airway, ref area, ref hasLastPos, ref hasNextPos, ref connectionCount);
						}
						if (stayMidAir)
						{
							componentData3.m_Position.y += 100f;
						}
						if (hasLastPos)
						{
							airway = false;
							area = false;
							TryAddSegments(curveElements, curveSources, componentData3.m_Position, default(float3), nodeDistance, segmentLength, ref lastPosition, ref lastTangent, ref airway, ref area);
						}
						else
						{
							lastPosition = componentData3.m_Position;
							lastTangent = default(float3);
							hasLastPos = true;
						}
					}
				}
				airway |= airway2;
				area |= area2;
				return;
			}
			Curve curve = m_CurveData[target];
			float2 @float = 0f;
			if (isBicycle && m_CarLaneData.TryGetComponent(target, out var componentData4))
			{
				if (m_MasterLaneData.TryGetComponent(target, out var componentData5) && m_OwnerData.TryGetComponent(target, out var componentData6) && m_SubLanes.TryGetBuffer(componentData6.m_Owner, out var bufferData))
				{
					int num = math.select((int)componentData5.m_MinIndex, (int)componentData5.m_MaxIndex, (componentData4.m_Flags & Game.Net.CarLaneFlags.Invert) != 0 == m_LeftHandTraffic);
					if (num >= 0 && num < bufferData.Length)
					{
						target = bufferData[num].m_SubLane;
						curve = m_CurveData[target];
					}
				}
				if (m_PrefabRefData.TryGetComponent(target, out var componentData7) && m_PrefabNetLaneData.TryGetComponent(componentData7.m_Prefab, out var componentData8) && m_PrefabCarLaneData.TryGetComponent(componentData7.m_Prefab, out var componentData9))
				{
					@float = componentData8.m_Width;
					bool flag = (componentData4.m_Flags & Game.Net.CarLaneFlags.Twoway) != 0 && targetDelta.y <= targetDelta.x;
					if (m_NodeLaneData.TryGetComponent(target, out var componentData10))
					{
						@float += componentData10.m_WidthOffset;
						@float = math.select(@float, 0f, new bool2((componentData10.m_Flags & NodeLaneFlags.StartBicycleOnly) != 0, (componentData10.m_Flags & NodeLaneFlags.EndBicycleOnly) != 0));
					}
					else
					{
						@float = math.select(@float, 0f, componentData9.m_RoadTypes == RoadTypes.Bicycle);
					}
					if (math.any(@float != 0f))
					{
						@float = math.select(@float * -0.5f, @float * 0.5f, m_LeftHandTraffic != flag);
						curve.m_Bezier = NetUtils.OffsetCurveLeftSmooth(curve.m_Bezier, @float);
					}
				}
			}
			Bezier4x3 curve2 = MathUtils.Cut(curve.m_Bezier, targetDelta);
			if (airway2 || area2)
			{
				if (airway2 && !hasLastPos && connectionCount == 0)
				{
					curve2 = ((!(targetDelta.y < targetDelta.x) && targetDelta.y != 0f) ? curve.m_Bezier : MathUtils.Invert(curve.m_Bezier));
				}
				curveElements.Add(new CurveElement
				{
					m_Curve = curve2
				});
				if (curveSources.IsCreated)
				{
					curveSources.Add(default(CurveSource));
				}
				connectionCount++;
				airway |= airway2;
				area |= area2;
				return;
			}
			if (connectionCount != 0)
			{
				ProcessConnectionSegments(curveElements, curveSources, nodeDistance, segmentLength, ref lastPosition, ref lastTangent, ref airway, ref area, ref hasLastPos, ref hasNextPos, ref connectionCount);
				airway = false;
				area = false;
			}
			if (isFirst)
			{
				if (hasLastPos && math.distance(lastNodePos, curve2.a) < nodeDistance)
				{
					if (math.distance(lastNodePos, curve2.d) <= nodeDistance)
					{
						return;
					}
					float num2 = MoveCurvePosition(lastNodePos, nodeDistance, curve2);
					curve2 = MathUtils.Cut(curve2, new float2(num2, 1f));
					targetDelta.x = math.lerp(targetDelta.x, targetDelta.y, num2);
				}
				isFirst = false;
			}
			if (isLast && hasNextPos && math.distance(nextNodePos, curve2.d) < nodeDistance)
			{
				if (math.distance(nextNodePos, curve2.a) <= nodeDistance)
				{
					return;
				}
				float num3 = MoveCurvePosition(nextNodePos, nodeDistance, MathUtils.Invert(curve2));
				curve2 = MathUtils.Cut(curve2, new float2(0f, 1f - num3));
				targetDelta.y = math.lerp(targetDelta.y, targetDelta.x, num3);
			}
			TryAddSegments(curveElements, curveSources, target, targetDelta, @float, curve2, nodeDistance, segmentLength, ref lastPosition, ref lastTangent, ref airway, ref area, ref hasLastPos);
		}

		private void ProcessConnectionSegments(DynamicBuffer<CurveElement> curveElements, DynamicBuffer<CurveSource> curveSources, float nodeDistance, float segmentLength, ref float3 lastPosition, ref float3 lastTangent, ref bool airway, ref bool area, ref bool hasLastPos, ref bool hasNextPos, ref int connectionCount)
		{
			int num = curveElements.Length - connectionCount;
			bool airway2 = false;
			bool area2 = false;
			if (area && connectionCount == 1)
			{
				CurveElement curveElement = curveElements[num];
				float3 @float = MathUtils.Position(curveElement.m_Curve, 0.5f);
				float3 float2 = MathUtils.Tangent(curveElement.m_Curve, 0.5f);
				if (hasLastPos)
				{
					TryAddSegments(curveElements, curveSources, @float, float2, nodeDistance, segmentLength, ref lastPosition, ref lastTangent, ref airway2, ref area2);
				}
				else
				{
					lastPosition = @float;
					lastTangent = float2;
					hasLastPos = true;
				}
			}
			for (int i = 1; i < connectionCount; i++)
			{
				CurveElement curveElement2 = curveElements[num + i - 1];
				CurveElement curveElement3 = curveElements[num + i];
				float3 float3 = (curveElement2.m_Curve.a + curveElement2.m_Curve.d + curveElement3.m_Curve.a + curveElement3.m_Curve.d) * 0.25f;
				float3 float4 = curveElement3.m_Curve.d - curveElement2.m_Curve.a;
				if (hasLastPos)
				{
					TryAddSegments(curveElements, curveSources, float3, float4, nodeDistance, segmentLength, ref lastPosition, ref lastTangent, ref airway2, ref area2);
					continue;
				}
				lastPosition = float3;
				lastTangent = float4;
				hasLastPos = true;
			}
			curveElements.RemoveRange(num, connectionCount);
			if (curveSources.IsCreated)
			{
				curveSources.RemoveRange(num, connectionCount);
			}
			connectionCount = 0;
		}

		private void GetMasterLane(ref Entity lane)
		{
			if (m_SlaveLaneData.TryGetComponent(lane, out var componentData) && m_OwnerData.TryGetComponent(lane, out var componentData2) && m_SubLanes.TryGetBuffer(componentData2.m_Owner, out var bufferData) && bufferData.Length > componentData.m_MasterIndex)
			{
				lane = bufferData[componentData.m_MasterIndex].m_SubLane;
			}
		}

		private bool ShouldEndPath(Entity target)
		{
			if (!m_ParkingLaneData.HasComponent(target) && (!m_ConnectionLaneData.TryGetComponent(target, out var componentData) || (componentData.m_Flags & ConnectionLaneFlags.Parking) == 0) && !m_PositionData.HasComponent(target))
			{
				return m_TransportStopData.HasComponent(target);
			}
			return true;
		}

		private bool ShouldEndPath(PathElement pathElement, bool isPedestrian)
		{
			if (m_ParkingLaneData.HasComponent(pathElement.m_Target) || m_PositionData.HasComponent(pathElement.m_Target) || m_TransportStopData.HasComponent(pathElement.m_Target))
			{
				return true;
			}
			bool flag = (pathElement.m_Flags & PathElementFlags.Secondary) != 0;
			if (isPedestrian)
			{
				if (!flag && !m_CarLaneData.HasComponent(pathElement.m_Target))
				{
					if (m_ConnectionLaneData.TryGetComponent(pathElement.m_Target, out var componentData))
					{
						return (componentData.m_Flags & (ConnectionLaneFlags.Road | ConnectionLaneFlags.Parking)) != 0;
					}
					return false;
				}
				return true;
			}
			if (flag || !m_PedestrianLaneData.HasComponent(pathElement.m_Target))
			{
				if (m_ConnectionLaneData.TryGetComponent(pathElement.m_Target, out var componentData2))
				{
					return ((uint)componentData2.m_Flags & (uint)(flag ? 256 : 384)) != 0;
				}
				return false;
			}
			return true;
		}

		private bool IsPedestrianTarget(PathElement pathElement)
		{
			if ((pathElement.m_Flags & PathElementFlags.Secondary) == 0)
			{
				if (!m_PedestrianLaneData.HasComponent(pathElement.m_Target))
				{
					if (m_ConnectionLaneData.TryGetComponent(pathElement.m_Target, out var componentData))
					{
						return (componentData.m_Flags & ConnectionLaneFlags.Pedestrian) != 0;
					}
					return false;
				}
				return true;
			}
			return false;
		}

		private bool ShouldSkipTarget(Entity target, bool skipAirway, bool skipArea, ref bool airway, ref bool area)
		{
			if (!m_CurveData.HasComponent(target))
			{
				return true;
			}
			if (m_CarLaneData.TryGetComponent(target, out var componentData) && (componentData.m_Flags & Game.Net.CarLaneFlags.Runway) != 0)
			{
				airway = true;
				return skipAirway;
			}
			if (m_ConnectionLaneData.TryGetComponent(target, out var componentData2))
			{
				if ((componentData2.m_Flags & ConnectionLaneFlags.Airway) != 0)
				{
					airway = true;
					return skipAirway;
				}
				if ((componentData2.m_Flags & ConnectionLaneFlags.Area) != 0)
				{
					area = true;
					return skipArea;
				}
				return true;
			}
			return false;
		}

		private float MoveCurvePosition(float3 comparePosition, float minDistance, Bezier4x3 curve)
		{
			float2 @float = new float2(0f, 1f);
			for (int i = 0; i < 8; i++)
			{
				float num = math.lerp(@float.x, @float.y, 0.5f);
				float3 y = MathUtils.Position(curve, num);
				if (math.distance(comparePosition, y) < minDistance)
				{
					@float.x = num;
				}
				else
				{
					@float.y = num;
				}
			}
			return math.lerp(@float.x, @float.y, 0.5f);
		}

		private void TryAddSegments(DynamicBuffer<CurveElement> curveElements, DynamicBuffer<CurveSource> curveSources, Entity target, float2 targetDelta, float2 sideOffset, Bezier4x3 curve, float nodeDistance, float segmentLength, ref float3 lastPosition, ref float3 lastTangent, ref bool airway, ref bool area, ref bool hasLastPos)
		{
			float3 @float = MathUtils.StartTangent(curve);
			if (hasLastPos)
			{
				TryAddSegments(curveElements, curveSources, curve.a, @float, nodeDistance, segmentLength, ref lastPosition, ref lastTangent, ref airway, ref area);
			}
			else
			{
				lastPosition = curve.a;
				lastTangent = @float;
				hasLastPos = true;
			}
			if (math.distance(curve.a, curve.d) > 0.01f)
			{
				curveElements.Add(new CurveElement
				{
					m_Curve = curve
				});
				if (curveSources.IsCreated)
				{
					curveSources.Add(new CurveSource
					{
						m_Entity = target,
						m_Range = targetDelta,
						m_Offset = sideOffset
					});
				}
				lastPosition = curve.d;
				lastTangent = MathUtils.EndTangent(curve);
				airway = false;
			}
		}

		private void TryAddSegments(DynamicBuffer<CurveElement> curveElements, DynamicBuffer<CurveSource> curveSources, float3 position, float3 nextTangent, float nodeDistance, float segmentLength, ref float3 lastPosition, ref float3 lastTangent, ref bool airway, ref bool area)
		{
			float num = math.distance(lastPosition, position);
			if (!(num > nodeDistance * 0.25f))
			{
				return;
			}
			Bezier4x3 curve;
			if (airway)
			{
				lastTangent = position - lastPosition;
				nextTangent = position - lastPosition;
				lastTangent.y += num;
				nextTangent.y -= num;
				lastTangent = MathUtils.Normalize(lastTangent, lastTangent.xz);
				nextTangent = MathUtils.Normalize(nextTangent, nextTangent.xz);
				curve = NetUtils.FitCurve(lastPosition, lastTangent, nextTangent, position);
				num = MathUtils.Length(curve);
				nextTangent = default(float3);
			}
			else
			{
				bool2 x = new bool2(!lastTangent.Equals(default(float3)), !nextTangent.Equals(default(float3)));
				if (math.any(x))
				{
					float3 @float = position - lastPosition;
					bool2 x2 = false;
					bool2 @bool = false;
					if (math.all(x))
					{
						lastTangent = MathUtils.Normalize(lastTangent, lastTangent.xz);
						nextTangent = MathUtils.Normalize(nextTangent, nextTangent.xz);
						x2.x = math.dot(lastTangent, @float) < nodeDistance * 0.2f;
						x2.y = math.dot(nextTangent, @float) < nodeDistance * 0.2f;
						@bool.x = math.dot(lastTangent, @float) < nodeDistance * 0.05f;
						@bool.y = math.dot(nextTangent, @float) < nodeDistance * 0.05f;
					}
					else if (x.x)
					{
						float3 float2 = @float / num;
						lastTangent = MathUtils.Normalize(lastTangent, lastTangent.xz);
						nextTangent = float2 * (math.dot(lastTangent, float2) * 2f) - lastTangent;
						x2 = math.dot(lastTangent, @float) < nodeDistance * 0.2f;
						@bool = math.dot(lastTangent, @float) < nodeDistance * 0.05f;
					}
					else if (x.y)
					{
						float3 float3 = @float / num;
						nextTangent = MathUtils.Normalize(nextTangent, nextTangent.xz);
						lastTangent = float3 * (math.dot(nextTangent, float3) * 2f) - nextTangent;
						x2 = math.dot(nextTangent, @float) < nodeDistance * 0.2f;
						@bool = math.dot(nextTangent, @float) < nodeDistance * 0.05f;
					}
					if (math.any(x2))
					{
						float3 float4 = lastPosition;
						float3 float5 = position;
						if (!@bool.x)
						{
							num = math.dot(lastTangent, @float);
							float4 = lastPosition + lastTangent * num;
							curve = NetUtils.StraightCurve(lastPosition, float4);
							int num2 = Mathf.RoundToInt(num / segmentLength);
							if (num2 > 1)
							{
								for (int i = 0; i < num2; i++)
								{
									float2 t = new float2(i, i + 1) / num2;
									curveElements.Add(new CurveElement
									{
										m_Curve = MathUtils.Cut(curve, t)
									});
									if (curveSources.IsCreated)
									{
										curveSources.Add(default(CurveSource));
									}
								}
							}
							else
							{
								curveElements.Add(new CurveElement
								{
									m_Curve = curve
								});
								if (curveSources.IsCreated)
								{
									curveSources.Add(default(CurveSource));
								}
							}
						}
						if (!@bool.y)
						{
							num = math.dot(nextTangent, @float);
							float5 = position - nextTangent * num;
						}
						num = math.distance(float4, float5);
						if (num >= nodeDistance * 0.5f)
						{
							curve = NetUtils.StraightCurve(float4, float5);
							int num3 = Mathf.RoundToInt(num / segmentLength);
							if (num3 > 1)
							{
								for (int j = 0; j < num3; j++)
								{
									float2 t2 = new float2(j, j + 1) / num3;
									curveElements.Add(new CurveElement
									{
										m_Curve = MathUtils.Cut(curve, t2)
									});
									if (curveSources.IsCreated)
									{
										curveSources.Add(default(CurveSource));
									}
								}
							}
							else
							{
								curveElements.Add(new CurveElement
								{
									m_Curve = curve
								});
								if (curveSources.IsCreated)
								{
									curveSources.Add(default(CurveSource));
								}
							}
						}
						if (!@bool.y)
						{
							num = math.dot(nextTangent, @float);
							curve = NetUtils.StraightCurve(float5, position);
							int num4 = Mathf.RoundToInt(num / segmentLength);
							if (num4 > 1)
							{
								for (int k = 0; k < num4; k++)
								{
									float2 t3 = new float2(k, k + 1) / num4;
									curveElements.Add(new CurveElement
									{
										m_Curve = MathUtils.Cut(curve, t3)
									});
									if (curveSources.IsCreated)
									{
										curveSources.Add(default(CurveSource));
									}
								}
							}
							else
							{
								curveElements.Add(new CurveElement
								{
									m_Curve = curve
								});
								if (curveSources.IsCreated)
								{
									curveSources.Add(default(CurveSource));
								}
							}
						}
						lastPosition = position;
						lastTangent = nextTangent;
						airway = false;
						area = false;
						return;
					}
					curve = NetUtils.FitCurve(lastPosition, lastTangent, nextTangent, position);
					num = MathUtils.Length(curve);
				}
				else
				{
					curve = NetUtils.StraightCurve(lastPosition, position);
					nextTangent = position - lastPosition;
				}
			}
			int num5 = Mathf.RoundToInt(num / segmentLength);
			if (num5 > 1)
			{
				for (int l = 0; l < num5; l++)
				{
					float2 t4 = new float2(l, l + 1) / num5;
					curveElements.Add(new CurveElement
					{
						m_Curve = MathUtils.Cut(curve, t4)
					});
					if (curveSources.IsCreated)
					{
						curveSources.Add(default(CurveSource));
					}
				}
			}
			else
			{
				curveElements.Add(new CurveElement
				{
					m_Curve = curve
				});
				if (curveSources.IsCreated)
				{
					curveSources.Add(default(CurveSource));
				}
			}
			lastPosition = position;
			lastTangent = nextTangent;
			airway = false;
			area = false;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathUpdated> __Game_Pathfind_PathUpdated_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferLookup<CurveElement> __Game_Routes_CurveElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Segment> __Game_Routes_Segment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Position> __Game_Routes_Position_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportStop> __Game_Routes_TransportStop_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathTargets> __Game_Routes_PathTargets_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathSource> __Game_Routes_PathSource_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MasterLane> __Game_Net_MasterLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NodeLane> __Game_Net_NodeLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathOwner> __Game_Pathfind_PathOwner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Controller> __Game_Vehicles_Controller_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WatercraftCurrentLane> __Game_Vehicles_WatercraftCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AircraftCurrentLane> __Game_Vehicles_AircraftCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TrainCurrentLane> __Game_Vehicles_TrainCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PublicTransport> __Game_Vehicles_PublicTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.Taxi> __Game_Vehicles_Taxi_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PersonalCar> __Game_Vehicles_PersonalCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Aircraft> __Game_Vehicles_Aircraft_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Bicycle> __Game_Vehicles_Bicycle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GroupMember> __Game_Creatures_GroupMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HumanCurrentLane> __Game_Creatures_HumanCurrentLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RouteData> __Game_Prefabs_RouteData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarLaneData> __Game_Prefabs_CarLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetLaneData> __Game_Prefabs_NetLaneData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CarNavigationLane> __Game_Vehicles_CarNavigationLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<WatercraftNavigationLane> __Game_Vehicles_WatercraftNavigationLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<AircraftNavigationLane> __Game_Vehicles_AircraftNavigationLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<TrainNavigationLane> __Game_Vehicles_TrainNavigationLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		public BufferLookup<CurveElement> __Game_Routes_CurveElement_RW_BufferLookup;

		public BufferLookup<CurveSource> __Game_Routes_CurveSource_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Pathfind_PathUpdated_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathUpdated>(isReadOnly: true);
			__Game_Routes_CurveElement_RO_BufferLookup = state.GetBufferLookup<CurveElement>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
			__Game_Routes_Segment_RO_ComponentLookup = state.GetComponentLookup<Segment>(isReadOnly: true);
			__Game_Routes_Position_RO_ComponentLookup = state.GetComponentLookup<Position>(isReadOnly: true);
			__Game_Routes_TransportStop_RO_ComponentLookup = state.GetComponentLookup<TransportStop>(isReadOnly: true);
			__Game_Routes_PathTargets_RO_ComponentLookup = state.GetComponentLookup<PathTargets>(isReadOnly: true);
			__Game_Routes_PathSource_RO_ComponentLookup = state.GetComponentLookup<PathSource>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
			__Game_Net_PedestrianLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.PedestrianLane>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
			__Game_Net_MasterLane_RO_ComponentLookup = state.GetComponentLookup<MasterLane>(isReadOnly: true);
			__Game_Net_NodeLane_RO_ComponentLookup = state.GetComponentLookup<NodeLane>(isReadOnly: true);
			__Game_Pathfind_PathOwner_RO_ComponentLookup = state.GetComponentLookup<PathOwner>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
			__Game_Vehicles_CarCurrentLane_RO_ComponentLookup = state.GetComponentLookup<CarCurrentLane>(isReadOnly: true);
			__Game_Vehicles_WatercraftCurrentLane_RO_ComponentLookup = state.GetComponentLookup<WatercraftCurrentLane>(isReadOnly: true);
			__Game_Vehicles_AircraftCurrentLane_RO_ComponentLookup = state.GetComponentLookup<AircraftCurrentLane>(isReadOnly: true);
			__Game_Vehicles_TrainCurrentLane_RO_ComponentLookup = state.GetComponentLookup<TrainCurrentLane>(isReadOnly: true);
			__Game_Vehicles_PublicTransport_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PublicTransport>(isReadOnly: true);
			__Game_Vehicles_Taxi_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.Taxi>(isReadOnly: true);
			__Game_Vehicles_PersonalCar_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PersonalCar>(isReadOnly: true);
			__Game_Vehicles_Aircraft_RO_ComponentLookup = state.GetComponentLookup<Aircraft>(isReadOnly: true);
			__Game_Vehicles_Bicycle_RO_ComponentLookup = state.GetComponentLookup<Bicycle>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentLookup = state.GetComponentLookup<CurrentVehicle>(isReadOnly: true);
			__Game_Creatures_GroupMember_RO_ComponentLookup = state.GetComponentLookup<GroupMember>(isReadOnly: true);
			__Game_Creatures_HumanCurrentLane_RO_ComponentLookup = state.GetComponentLookup<HumanCurrentLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_RouteData_RO_ComponentLookup = state.GetComponentLookup<RouteData>(isReadOnly: true);
			__Game_Prefabs_CarLaneData_RO_ComponentLookup = state.GetComponentLookup<CarLaneData>(isReadOnly: true);
			__Game_Prefabs_NetLaneData_RO_ComponentLookup = state.GetComponentLookup<NetLaneData>(isReadOnly: true);
			__Game_Routes_RouteWaypoint_RO_BufferLookup = state.GetBufferLookup<RouteWaypoint>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferLookup = state.GetBufferLookup<PathElement>(isReadOnly: true);
			__Game_Vehicles_CarNavigationLane_RO_BufferLookup = state.GetBufferLookup<CarNavigationLane>(isReadOnly: true);
			__Game_Vehicles_WatercraftNavigationLane_RO_BufferLookup = state.GetBufferLookup<WatercraftNavigationLane>(isReadOnly: true);
			__Game_Vehicles_AircraftNavigationLane_RO_BufferLookup = state.GetBufferLookup<AircraftNavigationLane>(isReadOnly: true);
			__Game_Vehicles_TrainNavigationLane_RO_BufferLookup = state.GetBufferLookup<TrainNavigationLane>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Routes_CurveElement_RW_BufferLookup = state.GetBufferLookup<CurveElement>();
			__Game_Routes_CurveSource_RW_BufferLookup = state.GetBufferLookup<CurveSource>();
		}
	}

	private CityConfigurationSystem m_CityConfigurationSystem;

	private EntityQuery m_UpdatedRoutesQuery;

	private EntityQuery m_AllRoutesQuery;

	private bool m_Loaded;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_UpdatedRoutesQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Segment>(),
				ComponentType.ReadOnly<CurveElement>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<LivePath>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Common.Event>(),
				ComponentType.ReadOnly<PathUpdated>()
			}
		});
		m_AllRoutesQuery = GetEntityQuery(ComponentType.ReadOnly<Segment>(), ComponentType.ReadOnly<CurveElement>());
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		m_Loaded = true;
	}

	private bool GetLoaded()
	{
		if (m_Loaded)
		{
			m_Loaded = false;
			return true;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		EntityQuery entityQuery = (GetLoaded() ? m_AllRoutesQuery : m_UpdatedRoutesQuery);
		if (!entityQuery.IsEmptyIgnoreFilter)
		{
			NativeList<Entity> nativeList = new NativeList<Entity>(Allocator.TempJob);
			JobHandle outJobHandle;
			NativeList<ArchetypeChunk> chunks = entityQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
			FindUpdatedSegmentsJob jobData = new FindUpdatedSegmentsJob
			{
				m_Chunks = chunks,
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_PathUpdatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathUpdated_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CurveElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_CurveElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_SegmentList = nativeList
			};
			JobHandle jobHandle = new UpdateSegmentCurvesJob
			{
				m_LeftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
				m_SegmentList = nativeList.AsDeferredJobArray(),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SegmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Segment_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Position_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransportStopData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_TransportStop_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PathTargetsData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_PathTargets_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PathSourceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_PathSource_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PedestrianLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SlaveLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_MasterLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_MasterLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_NodeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NodeLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PathOwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathOwner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CarCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_WatercraftCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_WatercraftCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AircraftCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_AircraftCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TrainCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_TrainCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PublicTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PublicTransport_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TaxiData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Taxi_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PersonalCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PersonalCar_RO_ComponentLookup, ref base.CheckedStateRef),
				m_AircraftData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Aircraft_RO_ComponentLookup, ref base.CheckedStateRef),
				m_BicycleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Bicycle_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CurrentVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
				m_GroupMemberData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_GroupMember_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HumanCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_HumanCurrentLane_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRouteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_RouteData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabCarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabNetLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_RouteWaypoints = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_CarNavigationLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_CarNavigationLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_WatercraftNavigationLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_WatercraftNavigationLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_AircraftNavigationLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_AircraftNavigationLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_TrainNavigationLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_TrainNavigationLane_RO_BufferLookup, ref base.CheckedStateRef),
				m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
				m_CurveElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_CurveElement_RW_BufferLookup, ref base.CheckedStateRef),
				m_CurveSources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_CurveSource_RW_BufferLookup, ref base.CheckedStateRef)
			}.Schedule(dependsOn: IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, outJobHandle)), list: nativeList, innerloopBatchCount: 1);
			nativeList.Dispose(jobHandle);
			chunks.Dispose(jobHandle);
			base.Dependency = jobHandle;
		}
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
	public SegmentCurveSystem()
	{
	}
}
