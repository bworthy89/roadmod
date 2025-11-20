using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Buildings;

public static class ValidationHelpers
{
	public static void ValidateBuilding(Entity entity, Building building, Transform transform, PrefabRef prefabRef, ValidationSystem.EntityData data, NativeArray<GroundWater> groundWaterMap, NativeQueue<ErrorData>.ParallelWriter errorQueue)
	{
		if (building.m_RoadEdge == Entity.Null)
		{
			BuildingData buildingData = data.m_PrefabBuilding[prefabRef.m_Prefab];
			if ((buildingData.m_Flags & Game.Prefabs.BuildingFlags.RequireRoad) != 0)
			{
				float3 position = BuildingUtils.CalculateFrontPosition(transform, buildingData.m_LotSize.y);
				bool num = (buildingData.m_Flags & (Game.Prefabs.BuildingFlags.CanBeOnRoad | Game.Prefabs.BuildingFlags.CanBeOnRoadArea)) != 0;
				bool flag = (buildingData.m_Flags & Game.Prefabs.BuildingFlags.CanBeRoadSide) != 0;
				if (num && !flag)
				{
					position = transform.m_Position;
				}
				errorQueue.Enqueue(new ErrorData
				{
					m_ErrorSeverity = ErrorSeverity.Warning,
					m_ErrorType = ErrorType.NoRoadAccess,
					m_TempEntity = entity,
					m_Position = position
				});
			}
		}
		if (((data.m_WaterPumpingStationData.TryGetComponent(prefabRef.m_Prefab, out var componentData) && (componentData.m_Types & AllowedWaterTypes.Groundwater) != AllowedWaterTypes.None) || data.m_GroundWaterPoweredData.HasComponent(prefabRef.m_Prefab)) && GroundWaterSystem.GetGroundWater(transform.m_Position, groundWaterMap).m_Max <= 500)
		{
			errorQueue.Enqueue(new ErrorData
			{
				m_ErrorSeverity = ErrorSeverity.Error,
				m_ErrorType = ErrorType.NoGroundWater,
				m_TempEntity = entity,
				m_Position = transform.m_Position
			});
		}
	}

	public static void ValidateUpgrade(Entity entity, Owner owner, PrefabRef prefabRef, ValidationSystem.EntityData data, NativeQueue<ErrorData>.ParallelWriter errorQueue)
	{
		if ((data.m_PrefabBuilding.HasComponent(prefabRef.m_Prefab) && (!data.m_ServiceUpgradeData.TryGetComponent(prefabRef.m_Prefab, out var componentData) || !componentData.m_ForbidMultiple)) || !data.m_Upgrades.TryGetBuffer(owner.m_Owner, out var bufferData))
		{
			return;
		}
		for (int i = 0; i < bufferData.Length; i++)
		{
			Entity upgrade = bufferData[i].m_Upgrade;
			if (upgrade != entity && data.m_PrefabRef[upgrade].m_Prefab == prefabRef.m_Prefab)
			{
				errorQueue.Enqueue(new ErrorData
				{
					m_ErrorSeverity = ErrorSeverity.Error,
					m_ErrorType = ErrorType.AlreadyUpgraded,
					m_TempEntity = entity,
					m_PermanentEntity = owner.m_Owner,
					m_Position = data.m_Transform[owner.m_Owner].m_Position
				});
			}
		}
	}
}
