using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs.Effects;

[ComponentMenu("Effects/", new Type[] { typeof(EffectPrefab) })]
public class VehicleSFX : ComponentBase
{
	public float2 m_SpeedLimits;

	public float2 m_SpeedPitches;

	public float2 m_SpeedVolumes;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<VehicleAudioEffectData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		VehicleAudioEffectData componentData = new VehicleAudioEffectData
		{
			m_SpeedLimits = m_SpeedLimits,
			m_SpeedPitches = m_SpeedPitches,
			m_SpeedVolumes = m_SpeedVolumes
		};
		entityManager.SetComponentData(entity, componentData);
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}
}
