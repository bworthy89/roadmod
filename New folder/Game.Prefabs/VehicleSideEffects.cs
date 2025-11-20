using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Vehicles/", new Type[] { typeof(VehiclePrefab) })]
public class VehicleSideEffects : ComponentBase
{
	public float2 m_RoadWear = new float2(0.5f, 1f);

	public float2 m_NoisePollution = new float2(0.5f, 1f);

	public float2 m_AirPollution = new float2(0.5f, 1f);

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<VehicleSideEffectData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		VehicleSideEffectData componentData = default(VehicleSideEffectData);
		componentData.m_Min = new float3(m_RoadWear.x, m_NoisePollution.x, m_AirPollution.x);
		componentData.m_Max = new float3(m_RoadWear.y, m_NoisePollution.y, m_AirPollution.y);
		entityManager.SetComponentData(entity, componentData);
	}
}
