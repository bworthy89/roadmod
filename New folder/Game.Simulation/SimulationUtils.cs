using Unity.Mathematics;

namespace Game.Simulation;

public static class SimulationUtils
{
	public const float FRAMES_PER_SECOND = 60f;

	public const int MOVING_OBJECT_UPDATE_GROUP_SHIFT = 4;

	public const int MOVING_OBJECT_UPDATE_GROUP_COUNT = 16;

	public const int NET_UPDATE_GROUP_COUNT = 16;

	public const int LANE_UPDATE_GROUP_COUNT = 16;

	public const int TREE_UPDATE_GROUP_COUNT = 16;

	public const int BUILDING_UPDATE_GROUP_COUNT = 16;

	public const int COMPANY_UPDATE_GROUP_COUNT = 16;

	public const int HOUSEHOLD_UPDATE_GROUP_COUNT = 16;

	public const int CITIZEN_UPDATE_GROUP_COUNT = 16;

	public const int HOUSEHOLDPET_UPDATE_GROUP_COUNT = 16;

	public const int GARBAGE_COLLECTION_DISPATCH_GROUP_COUNT = 32;

	public const int TRANSPORT_VEHICLE_DISPATCH_GROUP_COUNT = 8;

	public const int RANDOM_TRAFFIC_DISPATCH_GROUP_COUNT = 16;

	public const int FIRE_RESCUE_DISPATCH_GROUP_COUNT = 4;

	public const int POLICE_PATROL_DISPATCH_GROUP_COUNT = 32;

	public const int TAXI_DISPATCH_GROUP_COUNT = 16;

	public const int HEALTHCARE_DISPATCH_GROUP_COUNT = 16;

	public const int MAINTENANCE_DISPATCH_GROUP_COUNT = 32;

	public const int POST_VAN_DISPATCH_GROUP_COUNT = 32;

	public const int MAIL_TRANSFER_DISPATCH_GROUP_COUNT = 8;

	public const int POLICE_EMERGENCY_DISPATCH_GROUP_COUNT = 4;

	public const int EVACUATION_DISPATCH_GROUP_COUNT = 4;

	public const int PRISONER_TRANSPORT_DISPATCH_GROUP_COUNT = 16;

	public const int GARBAGE_TRANSFER_DISPATCH_GROUP_COUNT = 8;

	public const int GOODS_DELIVERY_DISPATCH_GROUP_COUNT = 32;

	public const int MOVING_OBJECT_INTERPOLATION_FRAME_COUNT = 4;

	public const int MOVING_EVENT_INTERPOLATION_FRAME_COUNT = 4;

	public const int AMBULANCE_UPDATE_GROUP = 0;

	public const int BUS_UPDATE_GROUP = 1;

	public const int GARBAGE_TRUCK_UPDATE_GROUP = 2;

	public const int TRAIN_UPDATE_GROUP = 3;

	public const int FIRE_ENGINE_UPDATE_GROUP = 4;

	public const int POLICE_CAR_UPDATE_GROUP = 5;

	public const int TAXI_UPDATE_GROUP = 6;

	public const int MAINTENANCE_VEHICLE_UPDATE_GROUP = 7;

	public const int WATERCRAFT_UPDATE_GROUP = 8;

	public const int POST_VAN_UPDATE_GROUP = 9;

	public const int AIRCRAFT_UPDATE_GROUP = 10;

	public const int HEARSE_UPDATE_GROUP = 11;

	public const int WORK_VEHICLE_UPDATE_GROUP = 12;

	public const int PET_UPDATE_GROUP = 5;

	public const int WILDLIFE_UPDATE_GROUP = 13;

	public const int DOMESTICATED_UPDATE_GROUP = 9;

	public const uint BUILDING_UPDATE_INTERVAL = 16u;

	public const uint DISPATCH_INTERVAL = 16u;

	public const uint DISPATCH_DELAY = 64u;

	public const uint MAX_PATHFIND_DELAY = 64u;

	public const uint CREATURE_SPAWN_INTERVAL = 16u;

	public const uint MOVING_EVENT_UPDATE_INTERVAL = 16u;

	public const uint MOVING_EVENT_UPDATE_GROUP = 0u;

	public const int TRANSPORT_STATION_UPDATE_GROUP = 0;

	public const int HOSPITAL_UPDATE_GROUP = 1;

	public const int TRANSPORT_DEPOT_UPDATE_GROUP = 2;

	public const int GARBAGE_FACILITY_UPDATE_GROUP = 5;

	public const int SCHOOL_UPDATE_GROUP = 6;

	public const int FIRE_STATION_UPDATE_GROUP = 7;

	public const int POLICE_STATION_UPDATE_GROUP = 8;

	public const int PARK_UPDATE_GROUP = 9;

	public const int MAINTENANCE_DEPOT_UPDATE_GROUP = 10;

	public const int POST_FACILITY_UPDATE_GROUP = 11;

	public const int PARKING_FACILITY_UPDATE_GROUP = 12;

	public const int TELECOM_FACILITY_UPDATE_GROUP = 13;

	public const int EXTRACTOR_FACILITY_UPDATE_GROUP = 14;

	public const int EMERGENCY_SHELTER_UPDATE_GROUP = 15;

	public const int DISASTER_FACILITY_UPDATE_GROUP = 0;

	public const int FIREWATCH_TOWER_UPDATE_GROUP = 1;

	public const int DEATHCARE_FACILITY_UPDATE_GROUP = 2;

	public const int PRISON_UPDATE_GROUP = 3;

	public const int ADMIN_BUILDING_UPDATE_GROUP = 4;

	public const int WELFARE_OFFICE_UPDATE_GROUP = 5;

	public const int RESEARCH_FACILITY_UPDATE_GROUP = 6;

	public const int GOODS_DELIVERY_FACILITY_UPDATE_GROUP = 7;

	public static uint GetUpdateFrameWithInterval(uint frame, uint interval, int groupCount)
	{
		return (uint)((frame / interval) & (groupCount - 1));
	}

	public static uint GetUpdateFrame(uint frame, int updatesPerDay, int groupCount)
	{
		return (uint)((frame / (262144 / (updatesPerDay * groupCount))) & (groupCount - 1));
	}

	public static uint GetUpdateFrameRare(uint frame, int daysPerUpdate, int groupCount)
	{
		return (uint)((frame / (daysPerUpdate * 262144 / groupCount)) & (groupCount - 1));
	}

	public static void ResetFailedRequest(ref ServiceRequest serviceRequest)
	{
		serviceRequest.m_FailCount = (byte)math.min(255, serviceRequest.m_FailCount + 1);
		serviceRequest.m_Cooldown = (byte)((1 << math.min(8, serviceRequest.m_FailCount)) - 1);
	}

	public static void ResetReverseRequest(ref ServiceRequest serviceRequest)
	{
		serviceRequest.m_FailCount = (byte)math.min(255, serviceRequest.m_FailCount + 1);
		serviceRequest.m_Cooldown = (byte)math.max(4, (1 << math.min(8, serviceRequest.m_FailCount)) - 1);
	}

	public static bool TickServiceRequest(ref ServiceRequest serviceRequest)
	{
		bool result = (serviceRequest.m_Cooldown == 0) | ((serviceRequest.m_Flags & ServiceRequestFlags.SkipCooldown) != 0);
		serviceRequest.m_Cooldown = (byte)math.max(0, serviceRequest.m_Cooldown - 1);
		serviceRequest.m_Flags &= ~ServiceRequestFlags.SkipCooldown;
		return result;
	}
}
