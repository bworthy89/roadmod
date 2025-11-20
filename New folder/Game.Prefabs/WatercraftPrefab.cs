using System;
using System.Collections.Generic;
using Game.Common;
using Game.Objects;
using Game.Pathfind;
using Game.Rendering;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Vehicles/", new Type[] { })]
public class WatercraftPrefab : VehiclePrefab
{
	public SizeClass m_SizeClass = SizeClass.Large;

	public EnergyTypes m_EnergyType = EnergyTypes.Fuel;

	public float m_MaxSpeed = 150f;

	public float m_Acceleration = 1f;

	public float m_Braking = 2f;

	public float2 m_Turning = new float2(30f, 5f);

	public float m_AngularAcceleration = 5f;

	public override IEnumerable<string> modTags
	{
		get
		{
			foreach (string modTag in base.modTags)
			{
				yield return modTag;
			}
			yield return "Ship";
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<WatercraftData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<Watercraft>());
		if (components.Contains(ComponentType.ReadWrite<Moving>()))
		{
			components.Add(ComponentType.ReadWrite<WatercraftNavigation>());
			components.Add(ComponentType.ReadWrite<WatercraftNavigationLane>());
			components.Add(ComponentType.ReadWrite<WatercraftCurrentLane>());
			components.Add(ComponentType.ReadWrite<PathOwner>());
			components.Add(ComponentType.ReadWrite<PathElement>());
			components.Add(ComponentType.ReadWrite<Target>());
			components.Add(ComponentType.ReadWrite<Blocker>());
			components.Add(ComponentType.ReadWrite<Swaying>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		WatercraftData componentData = default(WatercraftData);
		componentData.m_SizeClass = m_SizeClass;
		componentData.m_EnergyType = m_EnergyType;
		componentData.m_MaxSpeed = m_MaxSpeed / 3.6f;
		componentData.m_Acceleration = m_Acceleration;
		componentData.m_Braking = m_Braking;
		componentData.m_Turning = math.radians(m_Turning);
		componentData.m_AngularAcceleration = math.radians(m_AngularAcceleration);
		entityManager.SetComponentData(entity, componentData);
		entityManager.SetComponentData(entity, new UpdateFrameData(8));
	}
}
