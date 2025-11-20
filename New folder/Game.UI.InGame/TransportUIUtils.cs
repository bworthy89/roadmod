using Colossal.Entities;
using Game.Economy;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;

namespace Game.UI.InGame;

public static class TransportUIUtils
{
	public static int CountLines(NativeArray<UITransportLineData> lines, TransportType type, bool cargo = false)
	{
		int num = 0;
		for (int i = 0; i < lines.Length; i++)
		{
			if (lines[i].type == type && lines[i].isCargo == cargo)
			{
				num++;
			}
		}
		return num;
	}

	public static NativeArray<UITransportLineData> GetSortedLines(EntityQuery query, EntityManager entityManager, PrefabSystem prefabSystem)
	{
		NativeArray<Entity> nativeArray = query.ToEntityArray(Allocator.TempJob);
		int length = nativeArray.Length;
		NativeArray<UITransportLineData> nativeArray2 = new NativeArray<UITransportLineData>(length, Allocator.Temp);
		for (int i = 0; i < length; i++)
		{
			nativeArray2[i] = BuildTransportLine(nativeArray[i], entityManager, prefabSystem);
		}
		nativeArray2.Sort();
		nativeArray.Dispose();
		return nativeArray2;
	}

	public static UITransportLineData BuildTransportLine(Entity entity, EntityManager entityManager, PrefabSystem m_PrefabSystem)
	{
		Route componentData = entityManager.GetComponentData<Route>(entity);
		TransportLineData componentData2 = entityManager.GetComponentData<TransportLineData>(entityManager.GetComponentData<PrefabRef>(entity).m_Prefab);
		bool visible = !entityManager.HasComponent<HiddenRoute>(entity);
		Color componentData3 = entityManager.GetComponentData<Color>(entity);
		int cargo = 0;
		int capacity = 0;
		int stopCount = GetStopCount(entityManager, entity);
		int routeVehiclesCount = GetRouteVehiclesCount(entityManager, entity, ref cargo, ref capacity);
		float routeLength = GetRouteLength(entityManager, entity);
		float usage = ((capacity > 0) ? ((float)cargo / (float)capacity) : 0f);
		RouteSchedule schedule = ((!RouteUtils.CheckOption(componentData, RouteOption.Day)) ? (RouteUtils.CheckOption(componentData, RouteOption.Night) ? RouteSchedule.Night : RouteSchedule.DayAndNight) : RouteSchedule.Day);
		bool active = !RouteUtils.CheckOption(componentData, RouteOption.Inactive);
		return new UITransportLineData(entity, active, visible, componentData2.m_CargoTransport, componentData3, schedule, componentData2.m_TransportType, routeLength, stopCount, routeVehiclesCount, cargo, usage);
	}

	public static int GetStopCount(EntityManager entityManager, Entity entity)
	{
		DynamicBuffer<RouteWaypoint> buffer = entityManager.GetBuffer<RouteWaypoint>(entity, isReadOnly: true);
		int num = 0;
		for (int i = 0; i < buffer.Length; i++)
		{
			if (entityManager.TryGetComponent<Connected>(buffer[i].m_Waypoint, out var component) && (entityManager.HasComponent<Game.Routes.TransportStop>(component.m_Connected) || entityManager.HasComponent<Game.Routes.WorkStop>(component.m_Connected)) && !entityManager.HasComponent<TaxiStand>(component.m_Connected))
			{
				num++;
			}
		}
		return num;
	}

	public static float GetRouteLength(EntityManager entityManager, Entity entity)
	{
		DynamicBuffer<RouteSegment> buffer = entityManager.GetBuffer<RouteSegment>(entity, isReadOnly: true);
		float num = 0f;
		for (int i = 0; i < buffer.Length; i++)
		{
			if (entityManager.TryGetComponent<PathInformation>(buffer[i].m_Segment, out var component))
			{
				num += component.m_Distance;
			}
		}
		return num;
	}

	public static int GetRouteVehiclesCount(EntityManager entityManager, Entity entity, ref int cargo, ref int capacity)
	{
		if (entityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<RouteVehicle> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				AddCargo(entityManager, buffer[i].m_Vehicle, ref cargo, ref capacity);
			}
			return buffer.Length;
		}
		return 0;
	}

	private static void AddCargo(EntityManager entityManager, Entity entity, ref int cargo, ref int capacity)
	{
		if (entityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<LayoutElement> buffer))
		{
			for (int i = 0; i < buffer.Length; i++)
			{
				AddVehicleCargo(entityManager, buffer[i].m_Vehicle, ref cargo, ref capacity);
			}
		}
		else
		{
			AddVehicleCargo(entityManager, entity, ref cargo, ref capacity);
		}
	}

	private static void AddVehicleCargo(EntityManager entityManager, Entity entity, ref int cargo, ref int capacity)
	{
		if (!entityManager.TryGetComponent<PrefabRef>(entity, out var component))
		{
			return;
		}
		bool flag = false;
		CargoTransportVehicleData component3;
		WorkVehicleData component4;
		if (entityManager.TryGetComponent<PublicTransportVehicleData>(component.m_Prefab, out var component2))
		{
			if (entityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Passenger> buffer))
			{
				cargo += buffer.Length;
			}
			capacity += component2.m_PassengerCapacity;
		}
		else if (entityManager.TryGetComponent<CargoTransportVehicleData>(component.m_Prefab, out component3))
		{
			flag = true;
			capacity += component3.m_CargoCapacity;
		}
		else if (entityManager.TryGetComponent<WorkVehicleData>(component.m_Prefab, out component4))
		{
			flag = true;
			capacity += (int)component4.m_MaxWorkAmount;
		}
		if (flag && entityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<Resources> buffer2))
		{
			for (int i = 0; i < buffer2.Length; i++)
			{
				cargo += buffer2[i].m_Amount;
			}
		}
	}

	public static bool ShouldBindTransportType(EntityManager entityManager, PrefabSystem prefabSystem, TransportType transportType, NativeArray<Entity> lineDatas)
	{
		if (transportType == TransportType.Taxi)
		{
			return true;
		}
		for (int i = 0; i < lineDatas.Length; i++)
		{
			if (entityManager.TryGetComponent<PrefabData>(lineDatas[i], out var component) && prefabSystem.TryGetPrefab<TransportLinePrefab>(component, out var prefab) && prefab.m_TransportType == transportType)
			{
				return true;
			}
		}
		return false;
	}
}
