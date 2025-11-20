using System;
using System.Collections.Generic;
using Game.PSI;
using Game.Vehicles;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ExcludeGeneratedModTag]
[ComponentMenu("Vehicles/", new Type[] { })]
public class HelicopterPrefab : AircraftPrefab
{
	public float m_FlyingMaxSpeed = 250f;

	public float m_FlyingAcceleration = 10f;

	public float m_FlyingAngularAcceleration = 10f;

	public float m_AccelerationSwayFactor = 0.5f;

	public float m_VelocitySwayFactor = 0.7f;

	public override IEnumerable<string> modTags
	{
		get
		{
			foreach (string modTag in base.modTags)
			{
				yield return modTag;
			}
			yield return GetHelicopterType().ToString();
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<HelicopterData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<Helicopter>());
		if (GetHelicopterType() == HelicopterType.Rocket)
		{
			components.Add(ComponentType.ReadWrite<Rocket>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		HelicopterData componentData = default(HelicopterData);
		componentData.m_HelicopterType = GetHelicopterType();
		componentData.m_FlyingMaxSpeed = m_FlyingMaxSpeed / 3.6f;
		componentData.m_FlyingAcceleration = m_FlyingAcceleration;
		componentData.m_FlyingAngularAcceleration = math.radians(m_FlyingAngularAcceleration);
		componentData.m_AccelerationSwayFactor = m_AccelerationSwayFactor;
		componentData.m_VelocitySwayFactor = m_VelocitySwayFactor / componentData.m_FlyingMaxSpeed;
		entityManager.SetComponentData(entity, componentData);
	}

	protected virtual HelicopterType GetHelicopterType()
	{
		return HelicopterType.Helicopter;
	}
}
