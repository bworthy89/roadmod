using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class AudioSettingsPrefab : PrefabBase
{
	public EffectPrefab[] m_Effects;

	public float m_MinHeight;

	public float m_MaxHeight;

	[Range(0f, 1f)]
	public float m_OverlapRatio = 0.8f;

	[Range(0f, 1f)]
	public float m_MinDistanceRatio = 0.5f;

	[Header("Culling")]
	public int m_FireCullMaxAmount;

	public float m_FireCullMaxDistance;

	public int m_CarEngineCullMaxAmount;

	public float m_CarEngineCullMaxDistance;

	public int m_PublicTransCullMaxAmount;

	public float m_PublicTransCullMaxDistance;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<AmbientAudioSettingsData>());
		components.Add(ComponentType.ReadWrite<AmbientAudioEffect>());
		components.Add(ComponentType.ReadWrite<CullingAudioSettingsData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		DynamicBuffer<AmbientAudioEffect> buffer = entityManager.GetBuffer<AmbientAudioEffect>(entity);
		PrefabSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		for (int i = 0; i < m_Effects.Length; i++)
		{
			buffer.Add(new AmbientAudioEffect
			{
				m_Effect = orCreateSystemManaged.GetEntity(m_Effects[i])
			});
		}
		AmbientAudioSettingsData componentData = new AmbientAudioSettingsData
		{
			m_MaxHeight = m_MaxHeight,
			m_MinDistanceRatio = m_MinDistanceRatio,
			m_MinHeight = m_MinHeight,
			m_OverlapRatio = m_OverlapRatio
		};
		entityManager.SetComponentData(entity, componentData);
		CullingAudioSettingsData componentData2 = new CullingAudioSettingsData
		{
			m_FireCullMaxAmount = m_FireCullMaxAmount,
			m_FireCullMaxDistance = m_FireCullMaxDistance,
			m_CarEngineCullMaxAmount = m_CarEngineCullMaxAmount,
			m_CarEngineCullMaxDistance = m_CarEngineCullMaxDistance,
			m_PublicTransCullMaxAmount = m_PublicTransCullMaxAmount,
			m_PublicTransCullMaxDistance = m_PublicTransCullMaxDistance
		};
		entityManager.SetComponentData(entity, componentData2);
	}
}
