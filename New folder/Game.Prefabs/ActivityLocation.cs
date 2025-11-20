using System;
using System.Collections.Generic;
using Game.Net;
using Game.Objects;
using Game.Rendering;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[]
{
	typeof(StaticObjectPrefab),
	typeof(MarkerObjectPrefab),
	typeof(VehiclePrefab)
})]
public class ActivityLocation : ComponentBase
{
	[Serializable]
	public class LocationInfo
	{
		public ActivityLocationPrefab m_Activity;

		public float3 m_Position = float3.zero;

		public quaternion m_Rotation = quaternion.identity;
	}

	public LocationInfo[] m_Locations;

	public NetInvertMode m_InvertWhen;

	public string m_AnimatedPropName;

	public bool m_RequireAuthorization;

	public override bool ignoreUnlockDependencies => true;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_Locations != null)
		{
			for (int i = 0; i < m_Locations.Length; i++)
			{
				prefabs.Add(m_Locations[i].m_Activity);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		if (!(base.prefab is VehiclePrefab) && !(base.prefab is BuildingPrefab))
		{
			components.Add(ComponentType.ReadWrite<SpawnLocationData>());
		}
		components.Add(ComponentType.ReadWrite<ActivityLocationElement>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (!(base.prefab is VehiclePrefab) && !(base.prefab is BuildingPrefab))
		{
			components.Add(ComponentType.ReadWrite<Game.Objects.SpawnLocation>());
			components.Add(ComponentType.ReadWrite<Game.Objects.ActivityLocation>());
		}
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		SpawnLocationData componentData = default(SpawnLocationData);
		componentData.m_ConnectionType = RouteConnectionType.Pedestrian;
		componentData.m_ActivityMask = default(ActivityMask);
		componentData.m_RoadTypes = RoadTypes.None;
		componentData.m_TrackTypes = TrackTypes.None;
		componentData.m_RequireAuthorization = m_RequireAuthorization;
		componentData.m_HangaroundOnLane = false;
		if (m_Locations != null && m_Locations.Length != 0)
		{
			DynamicBuffer<ActivityLocationElement> buffer = entityManager.GetBuffer<ActivityLocationElement>(entity);
			buffer.ResizeUninitialized(m_Locations.Length);
			PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
			AnimatedPropID propID = entityManager.World.GetExistingSystemManaged<AnimatedSystem>().GetPropID(m_AnimatedPropName);
			for (int i = 0; i < m_Locations.Length; i++)
			{
				LocationInfo locationInfo = m_Locations[i];
				ActivityLocationElement value = new ActivityLocationElement
				{
					m_Prefab = existingSystemManaged.GetEntity(locationInfo.m_Activity),
					m_Position = locationInfo.m_Position,
					m_Rotation = locationInfo.m_Rotation,
					m_PropID = propID
				};
				switch (m_InvertWhen)
				{
				case NetInvertMode.LefthandTraffic:
					value.m_ActivityFlags |= ActivityFlags.InvertLefthandTraffic;
					break;
				case NetInvertMode.RighthandTraffic:
					value.m_ActivityFlags |= ActivityFlags.InvertRighthandTraffic;
					break;
				case NetInvertMode.Always:
					value.m_ActivityFlags |= ActivityFlags.InvertLefthandTraffic | ActivityFlags.InvertRighthandTraffic;
					break;
				}
				ActivityLocationData componentData2 = entityManager.GetComponentData<ActivityLocationData>(value.m_Prefab);
				value.m_ActivityMask = componentData2.m_ActivityMask;
				componentData.m_ActivityMask.m_Mask |= componentData2.m_ActivityMask.m_Mask;
				buffer[i] = value;
			}
		}
		else
		{
			ComponentBase.baseLog.ErrorFormat(base.prefab, "Empty activity location array: {0}", base.prefab.name);
		}
		if (!(base.prefab is VehiclePrefab) && !(base.prefab is BuildingPrefab))
		{
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
