using System;
using Colossal.Mathematics;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Simulation;

public struct CarLaneSpeedIterator
{
	public ComponentLookup<Transform> m_TransformData;

	public ComponentLookup<Moving> m_MovingData;

	public ComponentLookup<Car> m_CarData;

	public ComponentLookup<Bicycle> m_BicycleData;

	public ComponentLookup<Train> m_TrainData;

	public ComponentLookup<Controller> m_ControllerData;

	public ComponentLookup<LaneReservation> m_LaneReservationData;

	public ComponentLookup<LaneCondition> m_LaneConditionData;

	public ComponentLookup<LaneSignal> m_LaneSignalData;

	public ComponentLookup<Curve> m_CurveData;

	public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

	public ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLaneData;

	public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

	public ComponentLookup<Unspawned> m_UnspawnedData;

	public ComponentLookup<Creature> m_CreatureData;

	public ComponentLookup<PrefabRef> m_PrefabRefData;

	public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

	public ComponentLookup<CarData> m_PrefabCarData;

	public ComponentLookup<TrainData> m_PrefabTrainData;

	public ComponentLookup<ParkingLaneData> m_PrefabParkingLaneData;

	public BufferLookup<LaneOverlap> m_LaneOverlapData;

	public BufferLookup<LaneObject> m_LaneObjectData;

	public Entity m_Entity;

	public Entity m_Ignore;

	public NativeList<Entity> m_TempBuffer;

	public int m_Priority;

	public float m_TimeStep;

	public float m_SafeTimeStep;

	public float m_DistanceOffset;

	public float m_SpeedLimitFactor;

	public float m_CurrentSpeed;

	public CarData m_PrefabCar;

	public ObjectGeometryData m_PrefabObjectGeometry;

	public Bounds1 m_SpeedRange;

	public bool m_PushBlockers;

	public bool m_IsBicycle;

	public float m_MaxSpeed;

	public float m_CanChangeLane;

	public float3 m_CurrentPosition;

	public float m_Oncoming;

	public Entity m_Blocker;

	public BlockerType m_BlockerType;

	public float2 m_LaneOffsetPush;

	private Entity m_Lane;

	private Entity m_NextLane;

	private Curve m_Curve;

	private float2 m_CurveOffset;

	private float2 m_NextOffset;

	private float3 m_PrevPosition;

	private float m_PrevDistance;

	private float m_Distance;

	public bool IterateFirstLane(Entity lane, float3 curveOffset, Entity nextLane, float2 nextOffset, float laneOffset, bool requestSpace, out Game.Net.CarLaneFlags laneFlags)
	{
		Curve curve = m_CurveData[lane];
		bool flag = curveOffset.z < curveOffset.x;
		laneOffset = math.select(laneOffset, 0f - laneOffset, flag);
		laneFlags = ~(Game.Net.CarLaneFlags.Unsafe | Game.Net.CarLaneFlags.UTurnLeft | Game.Net.CarLaneFlags.Invert | Game.Net.CarLaneFlags.SideConnection | Game.Net.CarLaneFlags.TurnLeft | Game.Net.CarLaneFlags.TurnRight | Game.Net.CarLaneFlags.LevelCrossing | Game.Net.CarLaneFlags.Twoway | Game.Net.CarLaneFlags.IsSecured | Game.Net.CarLaneFlags.Runway | Game.Net.CarLaneFlags.Yield | Game.Net.CarLaneFlags.Stop | Game.Net.CarLaneFlags.SecondaryStart | Game.Net.CarLaneFlags.SecondaryEnd | Game.Net.CarLaneFlags.ForbidBicycles | Game.Net.CarLaneFlags.PublicOnly | Game.Net.CarLaneFlags.Highway | Game.Net.CarLaneFlags.UTurnRight | Game.Net.CarLaneFlags.GentleTurnLeft | Game.Net.CarLaneFlags.GentleTurnRight | Game.Net.CarLaneFlags.Forward | Game.Net.CarLaneFlags.Approach | Game.Net.CarLaneFlags.Roundabout | Game.Net.CarLaneFlags.RightLimit | Game.Net.CarLaneFlags.LeftLimit | Game.Net.CarLaneFlags.ForbidPassing | Game.Net.CarLaneFlags.RightOfWay | Game.Net.CarLaneFlags.TrafficLights | Game.Net.CarLaneFlags.ParkingLeft | Game.Net.CarLaneFlags.ParkingRight | Game.Net.CarLaneFlags.Forbidden | Game.Net.CarLaneFlags.AllowEnter);
		float3 @float = MathUtils.Position(curve.m_Bezier, curveOffset.x);
		float3 lanePosition = VehicleUtils.GetLanePosition(curve.m_Bezier, curveOffset.x, laneOffset);
		m_PrevPosition = m_CurrentPosition;
		m_Distance = math.distance(m_CurrentPosition, lanePosition);
		if (m_CarLaneData.TryGetComponent(lane, out var componentData))
		{
			componentData.m_SpeedLimit *= m_SpeedLimitFactor;
			laneFlags = componentData.m_Flags;
			float driveSpeed = VehicleUtils.GetMaxDriveSpeed(m_PrefabCar, componentData);
			int yieldOverride = 0;
			bool isRoundabout = false;
			if ((componentData.m_Flags & Game.Net.CarLaneFlags.Approach) == 0)
			{
				isRoundabout = (componentData.m_Flags & Game.Net.CarLaneFlags.Roundabout) != 0;
				if ((componentData.m_Flags & (Game.Net.CarLaneFlags.LevelCrossing | Game.Net.CarLaneFlags.TrafficLights)) != 0 && m_LaneSignalData.TryGetComponent(lane, out var componentData2))
				{
					switch (componentData2.m_Signal)
					{
					case LaneSignalType.Stop:
						yieldOverride = -1;
						break;
					case LaneSignalType.Yield:
						yieldOverride = 1;
						break;
					}
				}
			}
			if (m_LaneConditionData.HasComponent(lane))
			{
				VehicleUtils.ModifyDriveSpeed(ref driveSpeed, m_LaneConditionData[lane]);
			}
			if (m_Priority < 102 && m_LaneReservationData.TryGetComponent(lane, out var componentData3) && componentData3.GetPriority() == 102)
			{
				driveSpeed *= 0.5f;
			}
			if (driveSpeed < m_MaxSpeed)
			{
				m_MaxSpeed = MathUtils.Clamp(driveSpeed, m_SpeedRange);
				m_Blocker = Entity.Null;
				m_BlockerType = BlockerType.Limit;
			}
			float2 xy = curveOffset.xy;
			float num = 0f - m_PrefabObjectGeometry.m_Bounds.max.z;
			float num2 = m_Distance + num + m_DistanceOffset;
			if (componentData.m_CautionEnd >= componentData.m_CautionStart)
			{
				Bounds1 cautionBounds = componentData.cautionBounds;
				float2 float2 = math.select(curveOffset.xz, curveOffset.zx, flag);
				if (cautionBounds.max > float2.x && cautionBounds.min < float2.y)
				{
					float distance = num2 + curve.m_Length * math.max(0f, math.select(cautionBounds.min - float2.x, float2.y - cautionBounds.max, flag));
					float num3 = componentData.m_SpeedLimit * math.select(0.5f, 0.8f, (componentData.m_Flags & Game.Net.CarLaneFlags.IsSecured) != 0);
					driveSpeed = math.max(num3, VehicleUtils.GetMaxBrakingSpeed(m_PrefabCar, distance, num3, m_SafeTimeStep));
					if (driveSpeed < m_MaxSpeed)
					{
						m_MaxSpeed = MathUtils.Clamp(driveSpeed, m_SpeedRange);
						m_Blocker = Entity.Null;
						m_BlockerType = BlockerType.Caution;
					}
				}
			}
			m_Lane = lane;
			m_NextLane = nextLane;
			m_Curve = curve;
			m_CurveOffset = curveOffset.xz;
			m_NextOffset = nextOffset;
			m_CurrentPosition = @float;
			CheckCurrentLane(num2, laneOffset, xy, flag, inverseLaneOffset: false);
			CheckOverlappingLanes(num2, xy.y, laneOffset, yieldOverride, componentData.m_SpeedLimit, isRoundabout, flag, requestSpace, inverseLaneOffset: false);
		}
		else if (m_PedestrianLaneData.HasComponent(lane))
		{
			float num4 = math.min(m_PrefabCar.m_MaxSpeed, 5.555556f);
			int yieldOverride2 = 0;
			if (m_LaneSignalData.TryGetComponent(lane, out var componentData4))
			{
				switch (componentData4.m_Signal)
				{
				case LaneSignalType.Stop:
					yieldOverride2 = -1;
					break;
				case LaneSignalType.Yield:
					yieldOverride2 = 1;
					break;
				}
			}
			if (num4 < m_MaxSpeed)
			{
				m_MaxSpeed = MathUtils.Clamp(num4, m_SpeedRange);
				m_Blocker = Entity.Null;
				m_BlockerType = BlockerType.Limit;
			}
			float2 xy2 = curveOffset.xy;
			float num5 = 0f - m_PrefabObjectGeometry.m_Bounds.max.z;
			float num6 = m_Distance + num5 + m_DistanceOffset;
			m_Lane = lane;
			m_NextLane = nextLane;
			m_Curve = curve;
			m_CurveOffset = curveOffset.xz;
			m_NextOffset = nextOffset;
			m_CurrentPosition = @float;
			CheckCurrentLane(num6, laneOffset, xy2, flag, inverseLaneOffset: false);
			CheckOverlappingLanes(num6, xy2.y, laneOffset, yieldOverride2, num4, isRoundabout: false, flag, requestSpace, inverseLaneOffset: false);
		}
		float3 float3 = MathUtils.Position(curve.m_Bezier, curveOffset.z);
		float num7 = math.abs(curveOffset.z - curveOffset.x);
		float num8 = math.max(0.001f, math.lerp(math.distance(@float, float3), curve.m_Length * num7, num7));
		if (num8 > 1f)
		{
			m_PrevPosition = m_CurrentPosition;
			m_PrevDistance = m_Distance;
		}
		m_CurrentPosition = float3;
		m_Distance += num8;
		float brakingDistance = VehicleUtils.GetBrakingDistance(m_PrefabCar, m_MaxSpeed, m_SafeTimeStep);
		return (m_Distance + m_DistanceOffset - 20f >= brakingDistance) | (m_MaxSpeed == m_SpeedRange.min);
	}

	public bool IterateFirstLane(Entity lane1, Entity lane2, float3 curveOffset, Entity nextLane, float2 nextOffset, float laneDelta, float laneOffset1, float laneOffset2, bool requestSpace, out Game.Net.CarLaneFlags laneFlags)
	{
		laneDelta = math.saturate(laneDelta);
		laneFlags = ~(Game.Net.CarLaneFlags.Unsafe | Game.Net.CarLaneFlags.UTurnLeft | Game.Net.CarLaneFlags.Invert | Game.Net.CarLaneFlags.SideConnection | Game.Net.CarLaneFlags.TurnLeft | Game.Net.CarLaneFlags.TurnRight | Game.Net.CarLaneFlags.LevelCrossing | Game.Net.CarLaneFlags.Twoway | Game.Net.CarLaneFlags.IsSecured | Game.Net.CarLaneFlags.Runway | Game.Net.CarLaneFlags.Yield | Game.Net.CarLaneFlags.Stop | Game.Net.CarLaneFlags.SecondaryStart | Game.Net.CarLaneFlags.SecondaryEnd | Game.Net.CarLaneFlags.ForbidBicycles | Game.Net.CarLaneFlags.PublicOnly | Game.Net.CarLaneFlags.Highway | Game.Net.CarLaneFlags.UTurnRight | Game.Net.CarLaneFlags.GentleTurnLeft | Game.Net.CarLaneFlags.GentleTurnRight | Game.Net.CarLaneFlags.Forward | Game.Net.CarLaneFlags.Approach | Game.Net.CarLaneFlags.Roundabout | Game.Net.CarLaneFlags.RightLimit | Game.Net.CarLaneFlags.LeftLimit | Game.Net.CarLaneFlags.ForbidPassing | Game.Net.CarLaneFlags.RightOfWay | Game.Net.CarLaneFlags.TrafficLights | Game.Net.CarLaneFlags.ParkingLeft | Game.Net.CarLaneFlags.ParkingRight | Game.Net.CarLaneFlags.Forbidden | Game.Net.CarLaneFlags.AllowEnter);
		Curve curve = m_CurveData[lane1];
		Curve curve2 = m_CurveData[lane2];
		float3 @float = MathUtils.Position(curve.m_Bezier, curveOffset.x);
		float3 float2 = MathUtils.Position(curve2.m_Bezier, curveOffset.x);
		float3 x = math.lerp(@float, float2, laneDelta);
		float3 lanePosition = VehicleUtils.GetLanePosition(curve.m_Bezier, curveOffset.x, laneOffset1);
		float3 lanePosition2 = VehicleUtils.GetLanePosition(curve2.m_Bezier, curveOffset.x, laneOffset2);
		float3 y = math.lerp(lanePosition, lanePosition2, laneDelta);
		m_PrevPosition = m_CurrentPosition;
		m_Distance = math.distance(m_CurrentPosition, y);
		if (m_CarLaneData.TryGetComponent(lane1, out var componentData))
		{
			componentData.m_SpeedLimit *= m_SpeedLimitFactor;
			laneFlags = componentData.m_Flags;
			float driveSpeed = VehicleUtils.GetMaxDriveSpeed(m_PrefabCar, componentData);
			int yieldOverride = 0;
			bool isRoundabout = false;
			bool flag = curveOffset.z < curveOffset.x;
			if ((componentData.m_Flags & Game.Net.CarLaneFlags.Approach) == 0)
			{
				isRoundabout = (componentData.m_Flags & Game.Net.CarLaneFlags.Roundabout) != 0;
				if ((componentData.m_Flags & (Game.Net.CarLaneFlags.LevelCrossing | Game.Net.CarLaneFlags.TrafficLights)) != 0 && m_LaneSignalData.HasComponent(lane1))
				{
					switch (m_LaneSignalData[lane1].m_Signal)
					{
					case LaneSignalType.Stop:
						yieldOverride = -1;
						break;
					case LaneSignalType.Yield:
						yieldOverride = 1;
						break;
					}
				}
			}
			if (m_LaneConditionData.HasComponent(lane1))
			{
				VehicleUtils.ModifyDriveSpeed(ref driveSpeed, m_LaneConditionData[lane1]);
			}
			if (m_Priority < 102 && m_LaneReservationData.HasComponent(lane1) && m_LaneReservationData.HasComponent(lane2))
			{
				if (laneDelta < 0.9f)
				{
					LaneReservation laneReservation = m_LaneReservationData[lane1];
					LaneReservation laneReservation2 = m_LaneReservationData[lane2];
					if (math.any(new int2(laneReservation.GetPriority(), laneReservation2.GetPriority()) == 102))
					{
						driveSpeed *= 0.5f;
					}
				}
				else if (m_LaneReservationData[lane2].GetPriority() == 102)
				{
					driveSpeed *= 0.5f;
				}
			}
			if (driveSpeed < m_MaxSpeed)
			{
				m_MaxSpeed = MathUtils.Clamp(driveSpeed, m_SpeedRange);
				m_Blocker = Entity.Null;
				m_BlockerType = BlockerType.Limit;
			}
			float2 xy = curveOffset.xy;
			float num = 0f - m_PrefabObjectGeometry.m_Bounds.max.z;
			float num2 = m_Distance + num + m_DistanceOffset;
			if (componentData.m_CautionEnd >= componentData.m_CautionStart)
			{
				Bounds1 cautionBounds = componentData.cautionBounds;
				float2 float3 = math.select(curveOffset.xz, curveOffset.zx, flag);
				if (cautionBounds.max > float3.x && cautionBounds.min < float3.y)
				{
					float distance = num2 + curve.m_Length * math.max(0f, math.select(cautionBounds.min - float3.x, float3.y - cautionBounds.max, flag));
					float num3 = componentData.m_SpeedLimit * math.select(0.5f, 0.8f, (componentData.m_Flags & Game.Net.CarLaneFlags.IsSecured) != 0);
					driveSpeed = math.max(num3, VehicleUtils.GetMaxBrakingSpeed(m_PrefabCar, distance, num3, m_SafeTimeStep));
					if (driveSpeed < m_MaxSpeed)
					{
						m_MaxSpeed = MathUtils.Clamp(driveSpeed, m_SpeedRange);
						m_Blocker = Entity.Null;
						m_BlockerType = BlockerType.Caution;
					}
				}
			}
			if (laneDelta < 0.9f)
			{
				m_Lane = lane1;
				m_Curve = curve;
				m_CurveOffset = curveOffset.xz;
				m_CurrentPosition = @float;
				CheckCurrentLane(num2, laneOffset1, xy, flag, inverseLaneOffset: false);
				CheckOverlappingLanes(num2, xy.y, laneOffset1, yieldOverride, componentData.m_SpeedLimit, isRoundabout, flag, requestSpace, inverseLaneOffset: false);
			}
			m_Lane = lane2;
			m_NextLane = nextLane;
			m_Curve = curve2;
			m_CurveOffset = curveOffset.xz;
			m_NextOffset = nextOffset;
			m_CurrentPosition = float2;
			if (laneDelta == 0f)
			{
				CheckCurrentLane(num2, laneOffset2, xy, flag, inverseLaneOffset: true, ref m_CanChangeLane);
			}
			else
			{
				CheckCurrentLane(num2, laneOffset2, xy, flag, inverseLaneOffset: true);
			}
			CheckOverlappingLanes(num2, xy.y, laneOffset2, 0, componentData.m_SpeedLimit, isRoundabout, flag, requestSpace, inverseLaneOffset: true);
		}
		float3 float4 = MathUtils.Position(curve2.m_Bezier, curveOffset.z);
		float num4 = math.lerp(curve.m_Length, curve2.m_Length, laneDelta);
		float num5 = math.abs(curveOffset.z - curveOffset.x);
		float num6 = math.max(0.001f, math.lerp(math.distance(x, float4), num4 * num5, num5));
		if (num6 > 1f)
		{
			m_PrevPosition = m_CurrentPosition;
			m_PrevDistance = m_Distance;
		}
		m_CurrentPosition = float4;
		m_Distance += num6;
		float brakingDistance = VehicleUtils.GetBrakingDistance(m_PrefabCar, m_MaxSpeed, m_SafeTimeStep);
		return (m_Distance + m_DistanceOffset - 20f >= brakingDistance) | (m_MaxSpeed == m_SpeedRange.min);
	}

	public bool IterateNextLane(Entity lane, float2 curveOffset, float laneOffset, float minOffset, NativeArray<CarNavigationLane> nextLanes, bool requestSpace, ref Game.Net.CarLaneFlags laneFlags, out bool needSignal)
	{
		needSignal = false;
		Game.Net.CarLaneFlags carLaneFlags = laneFlags;
		laneFlags = ~(Game.Net.CarLaneFlags.Unsafe | Game.Net.CarLaneFlags.UTurnLeft | Game.Net.CarLaneFlags.Invert | Game.Net.CarLaneFlags.SideConnection | Game.Net.CarLaneFlags.TurnLeft | Game.Net.CarLaneFlags.TurnRight | Game.Net.CarLaneFlags.LevelCrossing | Game.Net.CarLaneFlags.Twoway | Game.Net.CarLaneFlags.IsSecured | Game.Net.CarLaneFlags.Runway | Game.Net.CarLaneFlags.Yield | Game.Net.CarLaneFlags.Stop | Game.Net.CarLaneFlags.SecondaryStart | Game.Net.CarLaneFlags.SecondaryEnd | Game.Net.CarLaneFlags.ForbidBicycles | Game.Net.CarLaneFlags.PublicOnly | Game.Net.CarLaneFlags.Highway | Game.Net.CarLaneFlags.UTurnRight | Game.Net.CarLaneFlags.GentleTurnLeft | Game.Net.CarLaneFlags.GentleTurnRight | Game.Net.CarLaneFlags.Forward | Game.Net.CarLaneFlags.Approach | Game.Net.CarLaneFlags.Roundabout | Game.Net.CarLaneFlags.RightLimit | Game.Net.CarLaneFlags.LeftLimit | Game.Net.CarLaneFlags.ForbidPassing | Game.Net.CarLaneFlags.RightOfWay | Game.Net.CarLaneFlags.TrafficLights | Game.Net.CarLaneFlags.ParkingLeft | Game.Net.CarLaneFlags.ParkingRight | Game.Net.CarLaneFlags.Forbidden | Game.Net.CarLaneFlags.AllowEnter);
		if (!m_CurveData.TryGetComponent(lane, out var componentData))
		{
			return false;
		}
		if (m_CarLaneData.TryGetComponent(lane, out var componentData2))
		{
			componentData2.m_SpeedLimit *= m_SpeedLimitFactor;
			laneFlags = componentData2.m_Flags;
			float driveSpeed = VehicleUtils.GetMaxDriveSpeed(m_PrefabCar, componentData2);
			float num = 0f - m_PrefabObjectGeometry.m_Bounds.max.z;
			float num2 = m_Distance + num;
			int yieldOverride = 0;
			bool flag = false;
			bool flag2 = curveOffset.y < curveOffset.x;
			Entity blocker = Entity.Null;
			BlockerType blockerType = BlockerType.Limit;
			if ((componentData2.m_Flags & Game.Net.CarLaneFlags.Approach) == 0)
			{
				if ((carLaneFlags & Game.Net.CarLaneFlags.Approach) == 0)
				{
					componentData2.m_Flags &= ~(Game.Net.CarLaneFlags.LevelCrossing | Game.Net.CarLaneFlags.Stop | Game.Net.CarLaneFlags.TrafficLights);
					if ((componentData2.m_Flags & Game.Net.CarLaneFlags.SideConnection) == 0)
					{
						componentData2.m_Flags &= ~(Game.Net.CarLaneFlags.UTurnLeft | Game.Net.CarLaneFlags.TurnLeft | Game.Net.CarLaneFlags.TurnRight | Game.Net.CarLaneFlags.UTurnRight | Game.Net.CarLaneFlags.GentleTurnLeft | Game.Net.CarLaneFlags.GentleTurnRight | Game.Net.CarLaneFlags.Forward);
					}
				}
				flag = (componentData2.m_Flags & Game.Net.CarLaneFlags.Roundabout) != 0;
				if ((componentData2.m_Flags & (Game.Net.CarLaneFlags.LevelCrossing | Game.Net.CarLaneFlags.TrafficLights)) != 0 && m_LaneSignalData.TryGetComponent(lane, out var componentData3))
				{
					float brakingDistance = VehicleUtils.GetBrakingDistance(m_PrefabCar, m_CurrentSpeed, 0f);
					if (!flag && brakingDistance <= num2 && !CheckSpace(lane, curveOffset, nextLanes, out blocker))
					{
						driveSpeed = 0f;
						blockerType = BlockerType.Continuing;
					}
					else
					{
						needSignal = true;
						switch (componentData3.m_Signal)
						{
						case LaneSignalType.Stop:
							if ((m_Priority < 108 || (componentData3.m_Flags & LaneSignalFlags.Physical) != 0) && brakingDistance <= num2 + 1f)
							{
								driveSpeed = 0f;
								blocker = componentData3.m_Blocker;
								blockerType = BlockerType.Signal;
								yieldOverride = 1;
							}
							else
							{
								yieldOverride = -1;
							}
							break;
						case LaneSignalType.SafeStop:
							if ((m_Priority < 108 || (componentData3.m_Flags & LaneSignalFlags.Physical) != 0) && brakingDistance <= num2)
							{
								driveSpeed = 0f;
								blocker = componentData3.m_Blocker;
								blockerType = BlockerType.Signal;
							}
							break;
						case LaneSignalType.Yield:
							yieldOverride = 1;
							break;
						}
					}
				}
				else if ((componentData2.m_Flags & Game.Net.CarLaneFlags.Stop) != 0)
				{
					if (m_Priority < 108 && num2 >= 1.1f)
					{
						driveSpeed = 0f;
						blockerType = BlockerType.Limit;
					}
					else if (!flag && VehicleUtils.GetBrakingDistance(m_PrefabCar, m_CurrentSpeed, 0f) <= num2 && !CheckSpace(lane, curveOffset, nextLanes, out blocker))
					{
						driveSpeed = 0f;
						blockerType = BlockerType.Continuing;
					}
					yieldOverride = 1;
				}
				else if ((componentData2.m_Flags & (Game.Net.CarLaneFlags.UTurnLeft | Game.Net.CarLaneFlags.TurnLeft | Game.Net.CarLaneFlags.TurnRight | Game.Net.CarLaneFlags.UTurnRight | Game.Net.CarLaneFlags.GentleTurnLeft | Game.Net.CarLaneFlags.GentleTurnRight | Game.Net.CarLaneFlags.Forward)) != 0 && !flag && VehicleUtils.GetBrakingDistance(m_PrefabCar, m_CurrentSpeed, 0f) <= num2 && !CheckSpace(lane, curveOffset, nextLanes, out blocker))
				{
					driveSpeed = 0f;
					blockerType = BlockerType.Continuing;
				}
			}
			num2 += m_DistanceOffset;
			float num3;
			if (driveSpeed == 0f)
			{
				float trueValue = math.clamp(1f - num, 0.2f, 0.5f);
				float distance = math.max(0f, num2 - math.select(0.5f, trueValue, m_IsBicycle));
				num3 = VehicleUtils.GetMaxBrakingSpeed(m_PrefabCar, distance, m_SafeTimeStep);
			}
			else
			{
				if (m_LaneConditionData.HasComponent(lane))
				{
					VehicleUtils.ModifyDriveSpeed(ref driveSpeed, m_LaneConditionData[lane]);
				}
				num3 = math.max(driveSpeed, VehicleUtils.GetMaxBrakingSpeed(m_PrefabCar, m_Distance, driveSpeed, m_TimeStep));
			}
			if (num3 < m_MaxSpeed)
			{
				m_MaxSpeed = MathUtils.Clamp(num3, m_SpeedRange);
				m_Blocker = blocker;
				m_BlockerType = blockerType;
			}
			if (componentData2.m_CautionEnd >= componentData2.m_CautionStart)
			{
				Bounds1 cautionBounds = componentData2.cautionBounds;
				float2 @float = math.select(curveOffset, curveOffset.yx, flag2);
				if (cautionBounds.max > @float.x && cautionBounds.min < @float.y)
				{
					float distance2 = num2 + componentData.m_Length * math.max(0f, math.select(cautionBounds.min - @float.x, @float.y - cautionBounds.max, flag2));
					float num4 = componentData2.m_SpeedLimit * math.select(0.5f, 0.8f, (componentData2.m_Flags & Game.Net.CarLaneFlags.IsSecured) != 0);
					num3 = math.max(num4, VehicleUtils.GetMaxBrakingSpeed(m_PrefabCar, distance2, num4, m_SafeTimeStep));
					if (num3 < m_MaxSpeed)
					{
						m_MaxSpeed = MathUtils.Clamp(num3, m_SpeedRange);
						m_Blocker = Entity.Null;
						m_BlockerType = BlockerType.Caution;
					}
				}
			}
			m_Curve = componentData;
			m_CurveOffset = curveOffset;
			m_Lane = lane;
			if (nextLanes.Length != 0)
			{
				CarNavigationLane carNavigationLane = nextLanes[0];
				m_NextOffset = carNavigationLane.m_CurvePosition;
				m_NextLane = carNavigationLane.m_Lane;
			}
			else
			{
				m_NextOffset = 0f;
				m_NextLane = Entity.Null;
			}
			minOffset = math.select(minOffset, curveOffset.x, flag2 ? (curveOffset.x < 1f) : (curveOffset.x > 0f));
			CheckCurrentLane(num2, laneOffset, minOffset, flag2, inverseLaneOffset: false);
			CheckOverlappingLanes(num2, minOffset, laneOffset, yieldOverride, componentData2.m_SpeedLimit, flag, flag2, requestSpace, inverseLaneOffset: false);
		}
		else if (m_ParkingLaneData.HasComponent(lane))
		{
			float num5 = 0f - m_PrefabObjectGeometry.m_Bounds.max.z;
			float distance3 = m_Distance + num5 + m_DistanceOffset;
			m_Curve = componentData;
			m_CurveOffset = curveOffset;
			m_Lane = lane;
			CheckParkingLane(distance3, ignoreTooFar: false);
		}
		else if (m_PedestrianLaneData.HasComponent(lane))
		{
			float num6 = math.min(m_PrefabCar.m_MaxSpeed, 5.555556f);
			float num7 = 0f - m_PrefabObjectGeometry.m_Bounds.max.z;
			float num8 = m_Distance + num7;
			int yieldOverride2 = 0;
			bool flag3 = curveOffset.y < curveOffset.x;
			Entity blocker2 = Entity.Null;
			BlockerType blockerType2 = BlockerType.Limit;
			if (m_LaneSignalData.TryGetComponent(lane, out var componentData4))
			{
				float brakingDistance2 = VehicleUtils.GetBrakingDistance(m_PrefabCar, m_CurrentSpeed, 0f);
				needSignal = true;
				switch (componentData4.m_Signal)
				{
				case LaneSignalType.Stop:
					if ((m_Priority < 108 || (componentData4.m_Flags & LaneSignalFlags.Physical) != 0) && brakingDistance2 <= num8 + 1f)
					{
						num6 = 0f;
						blocker2 = componentData4.m_Blocker;
						blockerType2 = BlockerType.Signal;
						yieldOverride2 = 1;
					}
					else
					{
						yieldOverride2 = -1;
					}
					break;
				case LaneSignalType.SafeStop:
					if ((m_Priority < 108 || (componentData4.m_Flags & LaneSignalFlags.Physical) != 0) && brakingDistance2 <= num8)
					{
						num6 = 0f;
						blocker2 = componentData4.m_Blocker;
						blockerType2 = BlockerType.Signal;
					}
					break;
				case LaneSignalType.Yield:
					yieldOverride2 = 1;
					break;
				}
			}
			else if ((componentData2.m_Flags & Game.Net.CarLaneFlags.Stop) != 0)
			{
				if (m_Priority < 108 && num8 >= 1.1f)
				{
					num6 = 0f;
					blockerType2 = BlockerType.Limit;
				}
				yieldOverride2 = 1;
			}
			num8 += m_DistanceOffset;
			float num10;
			if (num6 == 0f)
			{
				float num9 = math.clamp(1f - num7, 0.2f, 0.5f);
				num10 = VehicleUtils.GetMaxBrakingSpeed(m_PrefabCar, math.max(0f, num8 - num9), m_SafeTimeStep);
			}
			else
			{
				num10 = math.max(num6, VehicleUtils.GetMaxBrakingSpeed(m_PrefabCar, m_Distance, num6, m_TimeStep));
			}
			if (num10 < m_MaxSpeed)
			{
				m_MaxSpeed = MathUtils.Clamp(num10, m_SpeedRange);
				m_Blocker = blocker2;
				m_BlockerType = blockerType2;
			}
			m_Curve = componentData;
			m_CurveOffset = curveOffset;
			m_Lane = lane;
			if (nextLanes.Length != 0)
			{
				CarNavigationLane carNavigationLane2 = nextLanes[0];
				m_NextOffset = carNavigationLane2.m_CurvePosition;
				m_NextLane = carNavigationLane2.m_Lane;
			}
			else
			{
				m_NextOffset = 0f;
				m_NextLane = Entity.Null;
			}
			minOffset = math.select(minOffset, curveOffset.x, flag3 ? (curveOffset.x < 1f) : (curveOffset.x > 0f));
			CheckCurrentLane(num8, laneOffset, minOffset, flag3, inverseLaneOffset: false);
			CheckOverlappingLanes(num8, minOffset, laneOffset, yieldOverride2, num10, isRoundabout: false, flag3, requestSpace, inverseLaneOffset: false);
		}
		float3 float2 = MathUtils.Position(componentData.m_Bezier, curveOffset.y);
		float num11 = math.abs(curveOffset.y - curveOffset.x);
		float num12 = math.max(0.001f, math.lerp(math.distance(m_CurrentPosition, float2), componentData.m_Length * num11, num11));
		if (num12 > 1f)
		{
			m_PrevPosition = m_CurrentPosition;
			m_PrevDistance = m_Distance;
		}
		m_CurrentPosition = float2;
		m_Distance += num12;
		float brakingDistance3 = VehicleUtils.GetBrakingDistance(m_PrefabCar, m_MaxSpeed, m_SafeTimeStep);
		return (m_Distance + m_DistanceOffset - 20f >= brakingDistance3) | (m_MaxSpeed == m_SpeedRange.min);
	}

	public void IterateTarget(float3 targetPosition)
	{
		float maxDriveSpeed = VehicleUtils.GetMaxDriveSpeed(m_PrefabCar, 11.111112f, MathF.PI / 12f);
		IterateTarget(targetPosition, maxDriveSpeed);
	}

	public void IterateTarget(float3 targetPosition, float maxLaneSpeed)
	{
		float maxBrakingSpeed = VehicleUtils.GetMaxBrakingSpeed(m_PrefabCar, m_Distance, maxLaneSpeed, m_TimeStep);
		m_Distance += math.distance(m_CurrentPosition, targetPosition);
		maxBrakingSpeed = math.min(maxBrakingSpeed, VehicleUtils.GetMaxBrakingSpeed(m_PrefabCar, m_Distance, m_TimeStep));
		if (maxBrakingSpeed < m_MaxSpeed)
		{
			m_MaxSpeed = MathUtils.Clamp(maxBrakingSpeed, m_SpeedRange);
			m_Blocker = Entity.Null;
			m_BlockerType = BlockerType.None;
		}
	}

	private bool CheckSpace(Entity currentLane, float2 curveOffset, NativeArray<CarNavigationLane> nextLanes, out Entity blocker)
	{
		blocker = Entity.Null;
		if (nextLanes.Length == 0)
		{
			return true;
		}
		CarNavigationLane carNavigationLane = nextLanes[0];
		bool flag = carNavigationLane.m_CurvePosition.y < carNavigationLane.m_CurvePosition.x;
		if (carNavigationLane.m_CurvePosition.x != math.select(0f, 1f, flag) || !m_CarLaneData.TryGetComponent(carNavigationLane.m_Lane, out var componentData))
		{
			return true;
		}
		if ((componentData.m_Flags & (Game.Net.CarLaneFlags.UTurnLeft | Game.Net.CarLaneFlags.TurnLeft | Game.Net.CarLaneFlags.TurnRight | Game.Net.CarLaneFlags.LevelCrossing | Game.Net.CarLaneFlags.Yield | Game.Net.CarLaneFlags.Stop | Game.Net.CarLaneFlags.UTurnRight | Game.Net.CarLaneFlags.GentleTurnLeft | Game.Net.CarLaneFlags.GentleTurnRight | Game.Net.CarLaneFlags.Forward | Game.Net.CarLaneFlags.Roundabout | Game.Net.CarLaneFlags.RightOfWay | Game.Net.CarLaneFlags.TrafficLights)) != 0 && (componentData.m_Flags & Game.Net.CarLaneFlags.Approach) == 0)
		{
			return true;
		}
		if (!m_LaneOverlapData.TryGetBuffer(currentLane, out var bufferData))
		{
			return true;
		}
		Curve curve = m_CurveData[carNavigationLane.m_Lane];
		bool num = curveOffset.y < curveOffset.x;
		bool flag2 = false;
		bool flag3 = m_IsBicycle;
		float num2 = float.MaxValue;
		float num3 = MathUtils.Size(m_PrefabObjectGeometry.m_Bounds.z);
		float num4 = num3;
		int num5 = 1;
		OverlapFlags overlapFlags = (num ? OverlapFlags.MergeStart : OverlapFlags.MergeEnd);
		float3 @float = math.normalizesafe(MathUtils.Tangent(curve.m_Bezier, carNavigationLane.m_CurvePosition.x));
		float3 float2 = MathUtils.Position(curve.m_Bezier, carNavigationLane.m_CurvePosition.x);
		@float = math.select(@float, -@float, flag);
		DynamicBuffer<LaneObject> bufferData2;
		for (int i = 0; i < bufferData.Length; i++)
		{
			LaneOverlap laneOverlap = bufferData[i];
			if ((laneOverlap.m_Flags & (OverlapFlags.MergeStart | OverlapFlags.MergeEnd | OverlapFlags.MergeMiddleStart | OverlapFlags.MergeMiddleEnd | OverlapFlags.Unsafe | OverlapFlags.Water)) == 0)
			{
				flag2 = true;
			}
			else
			{
				if ((laneOverlap.m_Flags & overlapFlags) == 0 || !(laneOverlap.m_Other != carNavigationLane.m_Lane) || !m_LaneObjectData.TryGetBuffer(laneOverlap.m_Other, out bufferData2) || bufferData2.Length == 0)
				{
					continue;
				}
				float2 float3 = new float2((int)laneOverlap.m_OtherStart, (int)laneOverlap.m_OtherEnd) * 0.003921569f;
				for (int j = 0; j < bufferData2.Length; j++)
				{
					LaneObject laneObject = bufferData2[j];
					if (laneObject.m_LaneObject == m_Entity)
					{
						continue;
					}
					Entity entity = laneObject.m_LaneObject;
					if (m_ControllerData.TryGetComponent(laneObject.m_LaneObject, out var componentData2))
					{
						if (componentData2.m_Controller == m_Entity)
						{
							continue;
						}
						entity = componentData2.m_Controller;
					}
					float2 curvePosition = laneObject.m_CurvePosition;
					if ((curvePosition.y < curvePosition.x) ? (curvePosition.y >= float3.y) : (curvePosition.y <= float3.x))
					{
						int num6;
						if (m_CarData.TryGetComponent(entity, out var componentData3))
						{
							num6 = VehicleUtils.GetPriority(componentData3);
						}
						else if (m_TrainData.HasComponent(laneObject.m_LaneObject))
						{
							PrefabRef prefabRef = m_PrefabRefData[laneObject.m_LaneObject];
							num6 = VehicleUtils.GetPriority(m_PrefabTrainData[prefabRef.m_Prefab]);
						}
						else
						{
							num6 = 0;
						}
						if (num6 < m_Priority)
						{
							continue;
						}
					}
					if (!m_MovingData.TryGetComponent(laneObject.m_LaneObject, out var componentData4))
					{
						continue;
					}
					PrefabRef prefabRef2 = m_PrefabRefData[laneObject.m_LaneObject];
					if (!m_PrefabObjectGeometryData.TryGetComponent(prefabRef2.m_Prefab, out var componentData5))
					{
						continue;
					}
					bool flag4 = m_BicycleData.HasComponent(laneObject.m_LaneObject);
					float num7 = MathUtils.Size(componentData5.m_Bounds.z);
					num4 += math.select(num7 + math.select(1f, 0.5f, flag4), num7 * 0.5f, flag4 && flag3);
					flag3 = flag4;
					num5++;
					blocker = laneObject.m_LaneObject;
					if (!(num2 >= num3))
					{
						continue;
					}
					float num8 = 0f - componentData5.m_Bounds.min.z;
					bool flag5 = false;
					if (m_PrefabTrainData.TryGetComponent(prefabRef2.m_Prefab, out var componentData6))
					{
						Train train = m_TrainData[laneObject.m_LaneObject];
						float2 float4 = componentData6.m_AttachOffsets - componentData6.m_BogieOffsets;
						num8 = math.select(float4.y, float4.x, (train.m_Flags & Game.Vehicles.TrainFlags.Reversed) != 0);
						flag5 = true;
					}
					num2 = math.dot(m_TransformData[laneObject.m_LaneObject].m_Position - float2, @float) - num8;
					float num9 = math.dot(componentData4.m_Velocity, @float);
					if (num9 > 0.001f)
					{
						if (m_PrefabCarData.TryGetComponent(prefabRef2.m_Prefab, out var componentData7))
						{
							num2 += VehicleUtils.GetBrakingDistance(componentData7, num9, m_SafeTimeStep);
						}
						else if (flag5)
						{
							num2 += VehicleUtils.GetBrakingDistance(componentData6, num9, m_SafeTimeStep);
						}
					}
				}
			}
		}
		if (!flag2)
		{
			return true;
		}
		if (m_LaneObjectData.TryGetBuffer(currentLane, out bufferData2))
		{
			for (int k = 0; k < bufferData2.Length; k++)
			{
				LaneObject laneObject2 = bufferData2[k];
				if (laneObject2.m_LaneObject == m_Entity || (m_ControllerData.TryGetComponent(laneObject2.m_LaneObject, out var componentData8) && componentData8.m_Controller == m_Entity) || !m_MovingData.TryGetComponent(laneObject2.m_LaneObject, out var componentData9))
				{
					continue;
				}
				PrefabRef prefabRef3 = m_PrefabRefData[laneObject2.m_LaneObject];
				if (!m_PrefabObjectGeometryData.TryGetComponent(prefabRef3.m_Prefab, out var componentData10))
				{
					continue;
				}
				bool flag6 = m_BicycleData.HasComponent(laneObject2.m_LaneObject);
				float num10 = MathUtils.Size(componentData10.m_Bounds.z);
				num4 += math.select(num10 + math.select(1f, 0.5f, flag6), num10 * 0.5f, flag6 && flag3);
				flag3 = flag6;
				num5++;
				blocker = laneObject2.m_LaneObject;
				if (!(num2 >= num3))
				{
					continue;
				}
				float num11 = 0f - componentData10.m_Bounds.min.z;
				bool flag7 = false;
				if (m_PrefabTrainData.TryGetComponent(prefabRef3.m_Prefab, out var componentData11))
				{
					Train train2 = m_TrainData[laneObject2.m_LaneObject];
					float2 float5 = componentData11.m_AttachOffsets - componentData11.m_BogieOffsets;
					num11 = math.select(float5.y, float5.x, (train2.m_Flags & Game.Vehicles.TrainFlags.Reversed) != 0);
					flag7 = true;
				}
				num2 = math.dot(m_TransformData[laneObject2.m_LaneObject].m_Position - float2, @float) - num11;
				float num12 = math.dot(componentData9.m_Velocity, @float);
				if (num12 > 0.001f)
				{
					if (m_PrefabCarData.TryGetComponent(prefabRef3.m_Prefab, out var componentData12))
					{
						num2 += VehicleUtils.GetBrakingDistance(componentData12, num12, m_SafeTimeStep);
					}
					else if (flag7)
					{
						num2 += VehicleUtils.GetBrakingDistance(componentData11, num12, m_SafeTimeStep);
					}
				}
			}
		}
		if (num2 != float.MaxValue && num2 >= num3)
		{
			blocker = Entity.Null;
			return true;
		}
		num2 = 0f;
		int num13 = 1;
		while (true)
		{
			if (m_LaneObjectData.TryGetBuffer(carNavigationLane.m_Lane, out bufferData2))
			{
				for (int l = 0; l < bufferData2.Length; l++)
				{
					LaneObject laneObject3 = bufferData2[math.select(l, bufferData2.Length - 1 - l, flag)];
					bool flag8 = laneObject3.m_CurvePosition.y < laneObject3.m_CurvePosition.x;
					if (flag != flag8)
					{
						continue;
					}
					if (flag)
					{
						if (laneObject3.m_CurvePosition.x > carNavigationLane.m_CurvePosition.x)
						{
							continue;
						}
					}
					else if (laneObject3.m_CurvePosition.x < carNavigationLane.m_CurvePosition.x)
					{
						continue;
					}
					if (laneObject3.m_LaneObject == m_Entity || (m_ControllerData.TryGetComponent(laneObject3.m_LaneObject, out var componentData13) && componentData13.m_Controller == m_Entity) || !m_MovingData.TryGetComponent(laneObject3.m_LaneObject, out var componentData14))
					{
						continue;
					}
					PrefabRef prefabRef4 = m_PrefabRefData[laneObject3.m_LaneObject];
					m_PrefabObjectGeometryData.TryGetComponent(prefabRef4.m_Prefab, out var componentData15);
					float num14 = 0f - componentData15.m_Bounds.min.z;
					bool flag9 = false;
					if (m_PrefabTrainData.TryGetComponent(prefabRef4.m_Prefab, out var componentData16))
					{
						Train train3 = m_TrainData[laneObject3.m_LaneObject];
						float2 float6 = componentData16.m_AttachOffsets - componentData16.m_BogieOffsets;
						num14 = math.select(float6.y, float6.x, (train3.m_Flags & Game.Vehicles.TrainFlags.Reversed) != 0);
						flag9 = true;
					}
					float num15 = num2 + math.dot(m_TransformData[laneObject3.m_LaneObject].m_Position - float2, @float) - num14;
					float num16 = math.dot(componentData14.m_Velocity, @float);
					if (num16 > 0.001f)
					{
						if (m_PrefabCarData.TryGetComponent(prefabRef4.m_Prefab, out var componentData17))
						{
							num15 += VehicleUtils.GetBrakingDistance(componentData17, num16, m_SafeTimeStep);
						}
						else if (flag9)
						{
							num15 += VehicleUtils.GetBrakingDistance(componentData16, num16, m_SafeTimeStep);
						}
					}
					if (num15 >= num4)
					{
						blocker = Entity.Null;
						return true;
					}
					blocker = laneObject3.m_LaneObject;
					if (--num5 == 0)
					{
						return false;
					}
					bool flag10 = m_BicycleData.HasComponent(laneObject3.m_LaneObject);
					float num17 = MathUtils.Size(componentData15.m_Bounds.z);
					num4 += math.select(num17 + math.select(1f, 0.5f, flag10), num17 * 0.5f, flag10 && flag3);
					flag3 = flag10;
				}
			}
			num2 += curve.m_Length;
			if (math.max(num2, num3) >= num4)
			{
				blocker = Entity.Null;
				return true;
			}
			if (num13 >= nextLanes.Length)
			{
				return false;
			}
			carNavigationLane = nextLanes[num13++];
			flag = carNavigationLane.m_CurvePosition.y < carNavigationLane.m_CurvePosition.x;
			if (carNavigationLane.m_CurvePosition.x != math.select(0f, 1f, flag) || !m_CarLaneData.TryGetComponent(carNavigationLane.m_Lane, out componentData))
			{
				return false;
			}
			if ((componentData.m_Flags & (Game.Net.CarLaneFlags.UTurnLeft | Game.Net.CarLaneFlags.TurnLeft | Game.Net.CarLaneFlags.TurnRight | Game.Net.CarLaneFlags.LevelCrossing | Game.Net.CarLaneFlags.Yield | Game.Net.CarLaneFlags.Stop | Game.Net.CarLaneFlags.UTurnRight | Game.Net.CarLaneFlags.GentleTurnLeft | Game.Net.CarLaneFlags.GentleTurnRight | Game.Net.CarLaneFlags.Forward | Game.Net.CarLaneFlags.Roundabout | Game.Net.CarLaneFlags.RightOfWay | Game.Net.CarLaneFlags.TrafficLights)) != 0 && (componentData.m_Flags & Game.Net.CarLaneFlags.Approach) == 0)
			{
				break;
			}
			curve = m_CurveData[carNavigationLane.m_Lane];
			@float = math.normalizesafe(MathUtils.Tangent(curve.m_Bezier, carNavigationLane.m_CurvePosition.x));
			float2 = MathUtils.Position(curve.m_Bezier, carNavigationLane.m_CurvePosition.x);
			@float = math.select(@float, -@float, flag);
		}
		return false;
	}

	private bool CheckOverlapSpace(Entity currentLane, float2 curCurvePos, Entity nextLane, float2 nextCurvePos, float2 overlapPos, out Entity blocker)
	{
		blocker = Entity.Null;
		Entity entity = Entity.Null;
		Curve curve = m_CurveData[currentLane];
		float num = curve.m_Length * (1f - curCurvePos.x);
		float num2 = math.select(1f, 0.5f, m_IsBicycle);
		float num3 = MathUtils.Size(m_PrefabObjectGeometry.m_Bounds.z) + num2;
		float num4 = num3;
		bool flag = m_IsBicycle;
		if (m_LaneObjectData.TryGetBuffer(currentLane, out var bufferData))
		{
			for (int i = 0; i < bufferData.Length; i++)
			{
				LaneObject laneObject = bufferData[i];
				if (laneObject.m_CurvePosition.x < curCurvePos.x || laneObject.m_LaneObject == m_Entity || (m_ControllerData.TryGetComponent(laneObject.m_LaneObject, out var componentData) && componentData.m_Controller == m_Entity))
				{
					continue;
				}
				if (m_MovingData.HasComponent(laneObject.m_LaneObject))
				{
					PrefabRef prefabRef = m_PrefabRefData[laneObject.m_LaneObject];
					if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
					{
						bool flag2 = m_BicycleData.HasComponent(laneObject.m_LaneObject);
						float num5 = MathUtils.Size(componentData2.m_Bounds.z);
						num4 += math.select(num5 + math.select(1f, 0.5f, flag2), num5 * 0.5f, flag2 && flag);
						flag = flag2;
						blocker = laneObject.m_LaneObject;
					}
				}
				if (laneObject.m_CurvePosition.y >= overlapPos.y)
				{
					entity = laneObject.m_LaneObject;
					break;
				}
			}
		}
		if (entity == Entity.Null && m_CarLaneData.HasComponent(nextLane))
		{
			num += m_CurveData[nextLane].m_Length;
			if (m_LaneObjectData.TryGetBuffer(nextLane, out bufferData))
			{
				for (int j = 0; j < bufferData.Length; j++)
				{
					LaneObject laneObject2 = bufferData[j];
					if (!(laneObject2.m_CurvePosition.x < nextCurvePos.x) && !(laneObject2.m_LaneObject == m_Entity) && (!m_ControllerData.TryGetComponent(laneObject2.m_LaneObject, out var componentData3) || !(componentData3.m_Controller == m_Entity)))
					{
						entity = laneObject2.m_LaneObject;
						break;
					}
				}
			}
		}
		num = math.max(num, num3);
		if (entity != Entity.Null)
		{
			if (m_MovingData.TryGetComponent(entity, out var componentData4))
			{
				PrefabRef prefabRef2 = m_PrefabRefData[entity];
				float num6 = 0f;
				bool flag3 = false;
				ObjectGeometryData componentData6;
				if (m_PrefabTrainData.TryGetComponent(prefabRef2.m_Prefab, out var componentData5))
				{
					Train train = m_TrainData[entity];
					float2 @float = componentData5.m_AttachOffsets - componentData5.m_BogieOffsets;
					num6 = math.select(@float.y, @float.x, (train.m_Flags & Game.Vehicles.TrainFlags.Reversed) != 0);
					flag3 = true;
				}
				else if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef2.m_Prefab, out componentData6))
				{
					num6 = 0f - componentData6.m_Bounds.min.z;
				}
				Transform transform = m_TransformData[entity];
				float3 y = math.normalizesafe(MathUtils.Tangent(curve.m_Bezier, overlapPos.y));
				num = math.dot(transform.m_Position - MathUtils.Position(curve.m_Bezier, overlapPos.y), y) - num6;
				float num7 = math.dot(componentData4.m_Velocity, y);
				if (num7 > 0.001f)
				{
					if (m_PrefabCarData.TryGetComponent(prefabRef2.m_Prefab, out var componentData7))
					{
						num += VehicleUtils.GetBrakingDistance(componentData7, num7, m_SafeTimeStep);
					}
					else if (flag3)
					{
						num += VehicleUtils.GetBrakingDistance(componentData5, num7, m_SafeTimeStep);
					}
				}
			}
			blocker = entity;
		}
		if (num >= num4)
		{
			blocker = Entity.Null;
			return true;
		}
		return false;
	}

	public void IterateParkingTarget(Entity lane, float2 curveOffset)
	{
		if (m_ParkingLaneData.HasComponent(lane) && m_CurveData.TryGetComponent(lane, out var componentData))
		{
			float num = 0f - m_PrefabObjectGeometry.m_Bounds.max.z;
			float distance = m_Distance + num + m_DistanceOffset;
			m_Curve = componentData;
			m_CurveOffset = curveOffset;
			m_Lane = lane;
			CheckParkingLane(distance, ignoreTooFar: true);
		}
	}

	private void CheckParkingLane(float distance, bool ignoreTooFar)
	{
		if (!m_LaneObjectData.TryGetBuffer(m_Lane, out var bufferData) || bufferData.Length == 0)
		{
			return;
		}
		PrefabRef prefabRef = m_PrefabRefData[m_Lane];
		ParkingLaneData parkingLaneData = m_PrefabParkingLaneData[prefabRef.m_Prefab];
		float3 @float = MathUtils.Position(m_Curve.m_Bezier, m_CurveOffset.x);
		float2 float2;
		if (parkingLaneData.m_SlotInterval == 0f)
		{
			float2 = VehicleUtils.GetParkingSize(m_PrefabObjectGeometry, out var offset).y * 0.5f;
			float2.x += 0.9f + offset;
			float2.y += 0.9f - offset;
		}
		else
		{
			float2 = 0.1f;
		}
		for (int i = 0; i < bufferData.Length; i++)
		{
			LaneObject laneObject = bufferData[i];
			if (laneObject.m_LaneObject == m_Entity || (m_ControllerData.TryGetComponent(laneObject.m_LaneObject, out var componentData) && componentData.m_Controller == m_Entity) || m_UnspawnedData.HasComponent(laneObject.m_LaneObject))
			{
				continue;
			}
			bool test = laneObject.m_CurvePosition.y >= m_CurveOffset.x;
			float3 y = MathUtils.Position(m_Curve.m_Bezier, laneObject.m_CurvePosition.y);
			float num = math.select(float2.x, float2.y, test);
			if (parkingLaneData.m_SlotInterval == 0f)
			{
				float2 parkingOffsets = VehicleUtils.GetParkingOffsets(laneObject.m_LaneObject, ref m_PrefabRefData, ref m_PrefabObjectGeometryData);
				num += math.select(parkingOffsets.y, parkingOffsets.x, test);
			}
			if (!(math.distance(@float, y) < num))
			{
				continue;
			}
			if (ignoreTooFar)
			{
				float num2 = math.distance(m_CurrentPosition, @float);
				float num3 = float.MaxValue;
				if (m_TransformData.TryGetComponent(laneObject.m_LaneObject, out var componentData2))
				{
					num3 = math.distance(componentData2.m_Position, y);
				}
				if (num3 >= num2)
				{
					continue;
				}
			}
			float maxBrakingSpeed = VehicleUtils.GetMaxBrakingSpeed(m_PrefabCar, distance, m_SafeTimeStep);
			maxBrakingSpeed = math.select(maxBrakingSpeed, 3f, laneObject.m_LaneObject == m_Ignore && maxBrakingSpeed < 1f);
			maxBrakingSpeed = MathUtils.Clamp(maxBrakingSpeed, m_SpeedRange);
			if (maxBrakingSpeed < m_MaxSpeed)
			{
				m_MaxSpeed = maxBrakingSpeed;
				m_Blocker = laneObject.m_LaneObject;
				m_BlockerType = BlockerType.Continuing;
			}
		}
	}

	private void CheckCurrentLane(float distance, float laneOffset, float2 minOffset, bool inverse, bool inverseLaneOffset)
	{
		if (!m_LaneObjectData.TryGetBuffer(m_Lane, out var bufferData) || bufferData.Length == 0)
		{
			return;
		}
		float num = math.select(0.9f, 0.4f, m_IsBicycle);
		distance -= num;
		for (int i = 0; i < bufferData.Length; i++)
		{
			LaneObject laneObject = bufferData[i];
			if (!(laneObject.m_LaneObject == m_Entity))
			{
				float2 curvePosition = laneObject.m_CurvePosition;
				bool flag = curvePosition.y < curvePosition.x;
				bool flag2 = false;
				flag2 = (inverse ? ((!flag) ? (curvePosition.x >= minOffset.x) : (curvePosition.y >= minOffset.y && (curvePosition.y > 0f || curvePosition.x >= minOffset.x))) : ((!flag) ? (curvePosition.y <= minOffset.y && (curvePosition.y < 1f || curvePosition.x <= minOffset.x)) : (curvePosition.x <= minOffset.x)));
				if (!flag2 && (!m_ControllerData.TryGetComponent(laneObject.m_LaneObject, out var componentData) || !(componentData.m_Controller == m_Entity)))
				{
					float objectSpeed = GetObjectSpeed(laneObject.m_LaneObject, curvePosition.x);
					objectSpeed = math.select(objectSpeed, 0f - objectSpeed, inverse);
					BlockerType blockerType = ((inverse == flag) ? BlockerType.Continuing : BlockerType.Oncoming);
					UpdateMaxSpeed(laneObject.m_LaneObject, blockerType, objectSpeed, curvePosition.x, 1f, distance, 0f, laneOffset, laneObject.m_LaneObject == m_Ignore, inverse, flag, inverseLaneOffset, m_CurrentPosition);
				}
			}
		}
	}

	private void CheckCurrentLane(float distance, float laneOffset, float2 minOffset, bool inverse, bool inverseLaneOffset, ref float canUseLane)
	{
		if (!m_LaneObjectData.TryGetBuffer(m_Lane, out var bufferData) || bufferData.Length == 0)
		{
			return;
		}
		float num = math.select(0.9f, 0.4f, m_IsBicycle);
		distance -= num;
		for (int i = 0; i < bufferData.Length; i++)
		{
			LaneObject laneObject = bufferData[i];
			if (laneObject.m_LaneObject == m_Entity || (m_ControllerData.TryGetComponent(laneObject.m_LaneObject, out var componentData) && componentData.m_Controller == m_Entity))
			{
				continue;
			}
			float2 curvePosition = laneObject.m_CurvePosition;
			bool flag = curvePosition.y < curvePosition.x;
			bool flag2 = false;
			if (inverse ? ((!flag) ? (curvePosition.x >= minOffset.x) : (curvePosition.y >= minOffset.y && (curvePosition.y > 0f || curvePosition.x >= minOffset.x))) : ((!flag) ? (curvePosition.y <= minOffset.y && (curvePosition.y < 1f || curvePosition.x <= minOffset.x)) : (curvePosition.x <= minOffset.x)))
			{
				PrefabRef prefabRef = m_PrefabRefData[laneObject.m_LaneObject];
				float num2 = 0f;
				if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
				{
					num2 = 0f - componentData2.m_Bounds.max.z;
				}
				if ((curvePosition.x - minOffset.x) * m_Curve.m_Length > num2)
				{
					canUseLane = 0f;
				}
			}
			else
			{
				float objectSpeed = GetObjectSpeed(laneObject.m_LaneObject, curvePosition.x);
				objectSpeed = math.select(objectSpeed, 0f - objectSpeed, inverse);
				BlockerType blockerType = ((inverse == flag) ? BlockerType.Continuing : BlockerType.Oncoming);
				UpdateMaxSpeed(laneObject.m_LaneObject, blockerType, objectSpeed, curvePosition.x, 1f, distance, 0f, laneOffset, laneObject.m_LaneObject == m_Ignore, inverse, flag, inverseLaneOffset, m_CurrentPosition);
			}
		}
	}

	private void CheckOverlappingLanes(float origDistance, float origMinOffset, float laneOffset, int yieldOverride, float speedLimit, bool isRoundabout, bool inverse, bool requestSpace, bool inverseLaneOffset)
	{
		if (!m_LaneOverlapData.TryGetBuffer(m_Lane, out var bufferData) || bufferData.Length == 0)
		{
			return;
		}
		float num = math.select(0.9f, 0.4f, m_IsBicycle);
		origDistance -= num;
		Entity lane = m_Lane;
		Bezier4x3 bezier = m_Curve.m_Bezier;
		float2 curveOffset = m_CurveOffset;
		float length = m_Curve.m_Length;
		float x = 1f;
		int num2 = m_Priority;
		if (m_LaneReservationData.TryGetComponent(m_Lane, out var componentData))
		{
			int priority = componentData.GetPriority();
			num2 = math.select(num2, 106, priority >= 108 && 106 > num2);
		}
		for (int i = 0; i < bufferData.Length; i++)
		{
			LaneOverlap laneOverlap = bufferData[i];
			if ((laneOverlap.m_Flags & OverlapFlags.Water) != 0)
			{
				continue;
			}
			float4 @float = new float4((int)laneOverlap.m_ThisStart, (int)laneOverlap.m_ThisEnd, (int)laneOverlap.m_OtherStart, (int)laneOverlap.m_OtherEnd) * 0.003921569f;
			if (inverse)
			{
				if (@float.x >= curveOffset.x)
				{
					continue;
				}
			}
			else if (@float.y <= curveOffset.x)
			{
				continue;
			}
			m_Lane = laneOverlap.m_Other;
			m_Curve = m_CurveData[m_Lane];
			m_CurveOffset = math.select(@float.zw, @float.wz, inverse);
			Line3.Segment overlapLine = MathUtils.Line(bezier, @float.xy);
			float num3;
			OverlapFlags overlapFlags;
			OverlapFlags overlapFlags2;
			if (inverse)
			{
				num3 = math.max(0f, @float.y - origMinOffset);
				overlapFlags = OverlapFlags.MergeEnd | OverlapFlags.MergeMiddleEnd;
				overlapFlags2 = OverlapFlags.MergeStart | OverlapFlags.MergeMiddleStart;
			}
			else
			{
				num3 = math.max(0f, origMinOffset - @float.x);
				overlapFlags = OverlapFlags.MergeStart | OverlapFlags.MergeMiddleStart;
				overlapFlags2 = OverlapFlags.MergeEnd | OverlapFlags.MergeMiddleEnd;
			}
			if (isRoundabout && laneOverlap.m_PriorityDelta > 0 && (laneOverlap.m_Flags & OverlapFlags.Road) != 0 && @float.x >= curveOffset.x)
			{
				x = (@float.x = math.min(x, @float.x));
			}
			float num4 = length * math.select(@float.x - curveOffset.x, curveOffset.x - @float.y, inverse);
			float num5 = origDistance + num4;
			float distanceFactor = (float)(int)laneOverlap.m_Parallelism * (1f / 128f);
			bool flag = VehicleUtils.GetBrakingDistance(m_PrefabCar, m_CurrentSpeed, m_TimeStep) <= num5 + num;
			int num6 = num2;
			BlockerType blockerType = (((laneOverlap.m_Flags & overlapFlags2) != 0) ? BlockerType.Continuing : BlockerType.Crossing);
			if ((laneOverlap.m_Flags & overlapFlags) == 0)
			{
				if (inverse ? (@float.y >= origMinOffset) : (@float.x <= origMinOffset))
				{
					if (isRoundabout)
					{
						if (!m_TempBuffer.IsCreated)
						{
							m_TempBuffer = new NativeList<Entity>(16, Allocator.Temp);
						}
						m_TempBuffer.Add(in m_Lane);
					}
				}
				else
				{
					if (isRoundabout && m_TempBuffer.IsCreated)
					{
						int num7 = 0;
						while (num7 < m_TempBuffer.Length)
						{
							if (!(m_TempBuffer[num7] == m_Lane))
							{
								num7++;
								continue;
							}
							goto IL_051f;
						}
					}
					int num8 = yieldOverride;
					if (m_LaneSignalData.TryGetComponent(m_Lane, out var componentData2))
					{
						switch (componentData2.m_Signal)
						{
						case LaneSignalType.Stop:
							num8++;
							break;
						case LaneSignalType.Yield:
							num8--;
							break;
						}
					}
					int num9 = math.select(laneOverlap.m_PriorityDelta, num8, num8 != 0);
					num9 = math.select(num9, 0, requestSpace && num9 > 0);
					num6 -= num9;
					if (m_LaneReservationData.TryGetComponent(m_Lane, out componentData))
					{
						float offset = componentData.GetOffset();
						float num10 = math.select(math.max(num3 + @float.z, m_CurveOffset.x), 0f, inverse);
						int priority2 = componentData.GetPriority();
						if (offset > num10 || priority2 > num6)
						{
							float maxBrakingSpeed = VehicleUtils.GetMaxBrakingSpeed(m_PrefabCar, num5, m_SafeTimeStep);
							maxBrakingSpeed = MathUtils.Clamp(maxBrakingSpeed, m_SpeedRange);
							if (maxBrakingSpeed < m_MaxSpeed)
							{
								m_MaxSpeed = maxBrakingSpeed;
								m_Blocker = Entity.Null;
								m_BlockerType = blockerType;
							}
						}
						else if (math.select(math.select(laneOverlap.m_PriorityDelta, yieldOverride, num8 != 0), 1, (laneOverlap.m_Flags & OverlapFlags.Slow) != 0) > 0)
						{
							float maxBrakingSpeed2 = VehicleUtils.GetMaxBrakingSpeed(m_PrefabCar, num5, m_SafeTimeStep);
							if (maxBrakingSpeed2 >= speedLimit * 0.5f && maxBrakingSpeed2 < m_MaxSpeed)
							{
								m_MaxSpeed = maxBrakingSpeed2;
								m_Blocker = Entity.Null;
								m_BlockerType = blockerType;
							}
						}
						if (flag && !requestSpace && priority2 == 96 && !CheckOverlapSpace(lane, curveOffset, m_NextLane, m_NextOffset, @float.xy, out var blocker))
						{
							float maxBrakingSpeed3 = VehicleUtils.GetMaxBrakingSpeed(m_PrefabCar, num5, m_SafeTimeStep);
							if (maxBrakingSpeed3 < m_MaxSpeed)
							{
								m_MaxSpeed = maxBrakingSpeed3;
								m_Blocker = blocker;
								m_BlockerType = blockerType;
							}
						}
					}
				}
			}
			goto IL_051f;
			IL_051f:
			if (!m_LaneObjectData.TryGetBuffer(m_Lane, out var bufferData2) || bufferData2.Length == 0)
			{
				continue;
			}
			int num11 = 100;
			bool giveSpace = flag && num11 > num6;
			for (int j = 0; j < bufferData2.Length; j++)
			{
				LaneObject laneObject = bufferData2[j];
				if (laneObject.m_LaneObject == m_Entity)
				{
					continue;
				}
				Entity entity = laneObject.m_LaneObject;
				if (m_ControllerData.TryGetComponent(laneObject.m_LaneObject, out var componentData3))
				{
					if (componentData3.m_Controller == m_Entity)
					{
						continue;
					}
					entity = componentData3.m_Controller;
				}
				if (m_CreatureData.HasComponent(laneObject.m_LaneObject))
				{
					CheckPedestrian(overlapLine, laneObject.m_LaneObject, laneObject.m_CurvePosition.y, num5, giveSpace, inverse);
					continue;
				}
				float2 curvePosition = laneObject.m_CurvePosition;
				bool flag2 = curvePosition.y < curvePosition.x;
				float objectSpeed = GetObjectSpeed(laneObject.m_LaneObject, curvePosition.x);
				if ((laneOverlap.m_Flags & overlapFlags) == 0 && ((inverse ? (@float.y <= origMinOffset) : (@float.x >= origMinOffset)) | (flag2 ? (curvePosition.y < @float.w) : (curvePosition.y > @float.z))))
				{
					int num12;
					if (m_CarData.TryGetComponent(entity, out var componentData4))
					{
						num12 = VehicleUtils.GetPriority(componentData4);
					}
					else if (m_TrainData.HasComponent(laneObject.m_LaneObject))
					{
						PrefabRef prefabRef = m_PrefabRefData[laneObject.m_LaneObject];
						num12 = VehicleUtils.GetPriority(m_PrefabTrainData[prefabRef.m_Prefab]);
					}
					else
					{
						num12 = 0;
					}
					if (num12 - num6 > 0)
					{
						curvePosition.y += objectSpeed * 2f / math.max(1f, m_Curve.m_Length);
					}
				}
				if (flag2)
				{
					if (curvePosition.y >= @float.w - num3)
					{
						continue;
					}
				}
				else if (curvePosition.y <= @float.z + num3)
				{
					continue;
				}
				objectSpeed = math.select(objectSpeed, 0f - objectSpeed, inverse);
				float3 currentPos = MathUtils.Position(m_Curve.m_Bezier, math.select(m_CurveOffset.x, m_CurveOffset.y, flag2 != inverse));
				UpdateMaxSpeed(laneObject.m_LaneObject, blockerType, objectSpeed, curvePosition.x, distanceFactor, num5, num4, laneOffset, laneObject.m_LaneObject == m_Ignore, inverse, flag2, inverseLaneOffset, currentPos);
			}
		}
	}

	private float GetObjectSpeed(Entity obj, float curveOffset)
	{
		if (!m_MovingData.TryGetComponent(obj, out var componentData))
		{
			return 0f;
		}
		float3 y = math.normalizesafe(MathUtils.Tangent(m_Curve.m_Bezier, curveOffset));
		return math.dot(componentData.m_Velocity, y);
	}

	private void CheckPedestrian(Line3.Segment overlapLine, Entity obj, float targetOffset, float distanceOffset, bool giveSpace, bool inverse)
	{
		float2 @float = math.select(m_CurveOffset, m_CurveOffset.yx, inverse);
		if ((targetOffset <= @float.x) | (targetOffset >= @float.y))
		{
			PrefabRef prefabRef = m_PrefabRefData[obj];
			Transform transform = m_TransformData[obj];
			float num = m_PrefabObjectGeometry.m_Size.x * 0.5f;
			if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
			{
				num += componentData.m_Size.z * 0.5f;
			}
			float t;
			float num2 = MathUtils.Distance(overlapLine.xz, transform.m_Position.xz, out t);
			float num3 = math.dot(math.forward(transform.m_Rotation).xz, math.normalizesafe(MathUtils.Position(overlapLine, t).xz - transform.m_Position.xz));
			if (num2 - math.select(math.min(0f - num3, 0f), math.max(num3, 0f), giveSpace) >= num)
			{
				return;
			}
		}
		float position = ((m_PushBlockers || !m_MovingData.TryGetComponent(obj, out var componentData2) || !(math.lengthsq(componentData2.m_Velocity) >= 0.01f)) ? math.max(3f, VehicleUtils.GetMaxBrakingSpeed(m_PrefabCar, distanceOffset, 3f, m_SafeTimeStep)) : VehicleUtils.GetMaxBrakingSpeed(m_PrefabCar, distanceOffset, m_SafeTimeStep));
		position = MathUtils.Clamp(position, m_SpeedRange);
		if (position < m_MaxSpeed)
		{
			m_MaxSpeed = position;
			m_Blocker = obj;
			m_BlockerType = BlockerType.Temporary;
		}
	}

	private void UpdateMaxSpeed(Entity obj, BlockerType blockerType, float objectSpeed, float curvePosition, float distanceFactor, float distanceOffset, float overlapOffset, float laneOffset, bool ignore, bool inverse1, bool inverse2, bool inverseLaneOffset, float3 currentPos)
	{
		PrefabRef prefabRef = m_PrefabRefData[obj];
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		bool flag = false;
		ObjectGeometryData componentData2;
		if (m_PrefabTrainData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
		{
			Train train = m_TrainData[obj];
			float2 @float = componentData.m_AttachOffsets - componentData.m_BogieOffsets;
			num = math.select(@float.y, @float.x, (train.m_Flags & Game.Vehicles.TrainFlags.Reversed) != 0);
			flag = true;
		}
		else if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out componentData2))
		{
			num = 0f - componentData2.m_Bounds.min.z;
			num2 = componentData2.m_Size.x;
			num3 = componentData2.m_Bounds.max.z - componentData2.m_Bounds.min.z;
		}
		float2 float2 = math.select(m_CurveOffset, m_CurveOffset.yx, inverse1 != inverse2);
		float2 = math.select(curvePosition - float2, float2 - curvePosition, inverse2);
		if (float2.y * m_Curve.m_Length >= num)
		{
			return;
		}
		float2.x = math.min(0f, float2.x);
		Transform transform = m_TransformData[obj];
		float t = curvePosition + math.select(0f - float2.x, float2.x, inverse2);
		float3 x = MathUtils.Position(m_Curve.m_Bezier, t);
		float num4 = math.distance(x, currentPos);
		num4 += float2.x * m_Curve.m_Length;
		num4 = ((!(math.dot(transform.m_Position - currentPos, currentPos - m_PrevPosition) < 0f)) ? math.min(num4, math.distance(transform.m_Position, currentPos)) : math.min(num4, math.distance(transform.m_Position, m_PrevPosition) + m_PrevDistance - m_Distance - math.min(0f, overlapOffset)));
		num4 -= num;
		num4 *= distanceFactor;
		num4 += distanceOffset;
		if (m_IsBicycle && m_BicycleData.HasComponent(obj))
		{
			float num5 = math.dot(MathUtils.Right(math.normalizesafe(MathUtils.Tangent(m_Curve.m_Bezier, t).xz)), transform.m_Position.xz - x.xz);
			float num6 = math.abs(num5 - laneOffset) * distanceFactor;
			float num7 = math.max(m_PrefabObjectGeometry.m_Size.x + num2, 1f) * 0.5f;
			float num8 = math.lengthsq(num6 / num7) * 2f;
			if (num8 > num3)
			{
				return;
			}
			float2 float3 = new float2(laneOffset - num5, 1f);
			float3.x = math.select(float3.x, -0.1f, float3.x < 0f && float3.x > -0.1f);
			float3.x = math.select(float3.x, 0.1f, float3.x >= 0f && float3.x < 0.1f);
			float3.x = math.select(float3.x, 0f - float3.x, inverseLaneOffset);
			float3.x = num7 * 0.1f / float3.x;
			float3.x = math.abs(float3.x) * float3.x;
			m_LaneOffsetPush += float3 * (1f / math.lengthsq(math.max(0f, num4) + 0.5f));
			num4 += num8;
		}
		float maxBrakingSpeed;
		if (objectSpeed > 0.001f && m_PrefabCarData.TryGetComponent(prefabRef.m_Prefab, out var componentData3))
		{
			objectSpeed = math.max(0f, objectSpeed - componentData3.m_Braking * m_TimeStep * 2f) * distanceFactor;
			if (m_PrefabCar.m_Braking >= componentData3.m_Braking)
			{
				num4 += objectSpeed * m_SafeTimeStep;
				maxBrakingSpeed = VehicleUtils.GetMaxBrakingSpeed(m_PrefabCar, num4, objectSpeed, m_SafeTimeStep);
			}
			else
			{
				num4 += VehicleUtils.GetBrakingDistance(componentData3, objectSpeed, m_SafeTimeStep);
				maxBrakingSpeed = VehicleUtils.GetMaxBrakingSpeed(m_PrefabCar, num4, m_SafeTimeStep);
			}
		}
		else if (objectSpeed > 0.001f && flag)
		{
			objectSpeed = math.max(0f, objectSpeed - componentData.m_Braking * m_TimeStep * 2f) * distanceFactor;
			if (m_PrefabCar.m_Braking >= componentData.m_Braking)
			{
				num4 += objectSpeed * m_SafeTimeStep;
				maxBrakingSpeed = VehicleUtils.GetMaxBrakingSpeed(m_PrefabCar, num4, objectSpeed, m_SafeTimeStep);
			}
			else
			{
				num4 += VehicleUtils.GetBrakingDistance(componentData, objectSpeed, m_SafeTimeStep);
				maxBrakingSpeed = VehicleUtils.GetMaxBrakingSpeed(m_PrefabCar, num4, m_SafeTimeStep);
			}
		}
		else
		{
			maxBrakingSpeed = VehicleUtils.GetMaxBrakingSpeed(m_PrefabCar, num4, m_SafeTimeStep);
		}
		if (blockerType == BlockerType.Oncoming)
		{
			float y = 2f - maxBrakingSpeed * (1f / 6f);
			m_Oncoming = math.max(m_Oncoming, y);
			maxBrakingSpeed = math.max(maxBrakingSpeed, 3f);
			maxBrakingSpeed = MathUtils.Clamp(maxBrakingSpeed, m_SpeedRange);
			if (maxBrakingSpeed < m_MaxSpeed)
			{
				m_MaxSpeed = maxBrakingSpeed;
				m_Blocker = Entity.Null;
				m_BlockerType = blockerType;
			}
		}
		else
		{
			maxBrakingSpeed = math.select(maxBrakingSpeed, 3f, ignore && maxBrakingSpeed < 3f);
			maxBrakingSpeed = MathUtils.Clamp(maxBrakingSpeed, m_SpeedRange);
			if (maxBrakingSpeed < m_MaxSpeed)
			{
				m_MaxSpeed = maxBrakingSpeed;
				m_Blocker = obj;
				m_BlockerType = blockerType;
			}
		}
	}
}
