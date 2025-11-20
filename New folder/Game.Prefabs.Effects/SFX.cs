using System;
using System.Collections.Generic;
using Game.Audio;
using Game.Effects;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Prefabs.Effects;

[ComponentMenu("Effects/", new Type[] { typeof(EffectPrefab) })]
public class SFX : ComponentBase
{
	public AudioClip m_AudioClip;

	[Range(0f, 1f)]
	public float m_Volume = 1f;

	[Range(-3f, 3f)]
	public float m_Pitch = 1f;

	[Range(0f, 1f)]
	public float m_SpatialBlend = 1f;

	[Range(0f, 1f)]
	public float m_Doppler = 1f;

	public float m_Spread;

	public AudioRolloffMode m_RolloffMode = AudioRolloffMode.Linear;

	public float2 m_MinMaxDistance = new float2(1f, 200f);

	public bool m_Loop;

	public MixerGroup m_MixerGroup;

	public byte m_Priority = 128;

	public AnimationCurve m_RolloffCurve;

	public float3 m_SourceSize;

	public float2 m_FadeTimes;

	public bool m_RandomStartTime;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<AudioEffectData>());
		components.Add(ComponentType.ReadWrite<AudioSourceData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		AudioManager existingSystemManaged = entityManager.World.GetExistingSystemManaged<AudioManager>();
		entityManager.SetComponentData(entity, new AudioEffectData
		{
			m_AudioClipId = existingSystemManaged.RegisterSFX(this),
			m_MaxDistance = m_MinMaxDistance.y,
			m_SourceSize = m_SourceSize,
			m_FadeTimes = m_FadeTimes
		});
		DynamicBuffer<AudioSourceData> buffer = entityManager.GetBuffer<AudioSourceData>(entity);
		buffer.ResizeUninitialized(1);
		buffer[0] = new AudioSourceData
		{
			m_SFXEntity = entity
		};
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}
}
